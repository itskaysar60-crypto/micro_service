using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OrderService.Domain.Interfaces;
using OrderService.Infrastructure.Messaging;

namespace OrderService.Infrastructure.Sync;

/// <summary>
/// Background service that polls unpublished OutboxEvents and publishes them
/// to RabbitMQ (exchange: "order.created").
/// Replaces the old HttpOutboxRelay — same Outbox pattern, different transport.
/// </summary>
public sealed class RabbitMqOutboxRelay : BackgroundService
{
    private readonly IServiceScopeFactory      _scopeFactory;
    private readonly RabbitMqPublisher         _publisher;
    private readonly ILogger<RabbitMqOutboxRelay> _logger;

    private static readonly TimeSpan PollingInterval = TimeSpan.FromSeconds(10);

    public RabbitMqOutboxRelay(
        IServiceScopeFactory          scopeFactory,
        RabbitMqPublisher             publisher,
        ILogger<RabbitMqOutboxRelay>  logger)
    {
        _scopeFactory = scopeFactory;
        _publisher    = publisher;
        _logger       = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "RabbitMqOutboxRelay started. Polling every {Seconds}s...",
            PollingInterval.TotalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PublishPendingEventsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled error in RabbitMqOutboxRelay cycle.");
            }

            await Task.Delay(PollingInterval, stoppingToken);
        }
    }

    // ── Core publish loop ──────────────────────────────────────────────────

    private async Task PublishPendingEventsAsync(CancellationToken ct)
    {
        // BackgroundService is Singleton, repositories are Scoped — create a scope
        using var scope      = _scopeFactory.CreateScope();
        var outboxRepo       = scope.ServiceProvider.GetRequiredService<IOutboxRepository>();
        var orderRepo        = scope.ServiceProvider.GetRequiredService<IOrderRepository>();

        var pending = await outboxRepo.GetUnpublishedAsync();
        if (pending.Count == 0) return;

        _logger.LogInformation(
            "Found {Count} unpublished outbox event(s). Publishing to RabbitMQ...",
            pending.Count);

        foreach (var evt in pending)
        {
            if (ct.IsCancellationRequested) break;

            try
            {
                // Publish the pre-serialised JSON payload to RabbitMQ
                _publisher.Publish(evt.Payload);

                // Mark outbox event as published + order as synced (atomic SaveChanges)
                await outboxRepo.MarkPublishedAsync(evt.Id);
                await orderRepo.MarkAsSyncedAsync(evt.OrderId);
                await orderRepo.SaveChangesAsync();

                _logger.LogInformation(
                    "OutboxEvent {EventId} published and Order {OrderId} marked synced.",
                    evt.Id, evt.OrderId);
            }
            catch (RabbitMQ.Client.Exceptions.BrokerUnreachableException ex)
            {
                // Broker is down — increment retry, stop processing remaining events
                await outboxRepo.IncrementRetryAsync(evt.Id, ex.Message);
                _logger.LogWarning(
                    "RabbitMQ broker unreachable. Will retry on next cycle. Error: {Msg}",
                    ex.Message);
                break;
            }
            catch (Exception ex)
            {
                await outboxRepo.IncrementRetryAsync(evt.Id, ex.Message);
                _logger.LogError(ex,
                    "Failed to publish OutboxEvent {EventId}. Retry count incremented.", evt.Id);
            }
        }
    }
}
