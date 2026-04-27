using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OrderService.Domain.Interfaces;

namespace OrderService.Infrastructure.Sync;

/// <summary>
/// Background service that polls OutboxEvents and POSTs them to InventoryService.
/// This is the core of the offline-first sync strategy.
/// </summary>
public class HttpOutboxRelay : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<HttpOutboxRelay> _logger;

    public HttpOutboxRelay(
        IServiceScopeFactory scopeFactory,
        IHttpClientFactory httpClientFactory,
        ILogger<HttpOutboxRelay> logger)
    {
        _scopeFactory = scopeFactory;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("HttpOutboxRelay started. Polling every 10 seconds...");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await TrySyncPendingEventsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in OutboxRelay cycle.");
            }

            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
        }
    }

    private async Task TrySyncPendingEventsAsync(CancellationToken ct)
    {
        // Create a new scope because BackgroundService is Singleton
        // but DbContext/Repositories are Scoped
        using var scope = _scopeFactory.CreateScope();
        var outboxRepo = scope.ServiceProvider.GetRequiredService<IOutboxRepository>();
        var orderRepo = scope.ServiceProvider.GetRequiredService<IOrderRepository>();

        var pending = await outboxRepo.GetUnpublishedAsync();

        if (pending.Count == 0) return;

        _logger.LogInformation("Found {Count} unpublished events. Syncing...", pending.Count);

        var client = _httpClientFactory.CreateClient("InventoryService");

        foreach (var evt in pending)
        {
            if (ct.IsCancellationRequested) break;

            try
            {
                var content = new StringContent(evt.Payload, Encoding.UTF8, "application/json");
                var response = await client.PostAsync("/api/sync/orders", content, ct);

                if (response.IsSuccessStatusCode)
                {
                    await outboxRepo.MarkPublishedAsync(evt.Id);
                    await orderRepo.MarkAsSyncedAsync(evt.OrderId);
                    await orderRepo.SaveChangesAsync();

                    _logger.LogInformation("Event {EventId} synced successfully.", evt.Id);
                }
                else
                {
                    var error = $"HTTP {(int)response.StatusCode}: {response.ReasonPhrase}";
                    await outboxRepo.IncrementRetryAsync(evt.Id, error);
                    _logger.LogWarning("Sync failed for event {EventId}: {Error}", evt.Id, error);
                }
            }
            catch (HttpRequestException ex)
            {
                await outboxRepo.IncrementRetryAsync(evt.Id, ex.Message);
                _logger.LogWarning("InventoryService unreachable. Will retry. Error: {Msg}", ex.Message);
                break; // If service is down, no point trying rest
            }
        }
    }
}
