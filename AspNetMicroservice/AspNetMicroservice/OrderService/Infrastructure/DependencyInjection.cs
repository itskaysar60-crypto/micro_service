using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OrderService.Domain.Interfaces;
using OrderService.Infrastructure.Persistence;
using OrderService.Infrastructure.Repositories;
using OrderService.Infrastructure.Sync;

namespace OrderService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration config)
    {
        // ── Database ──
        services.AddDbContext<OrderDbContext>(options =>
            options.UseSqlServer(config.GetConnectionString("OrderDb")));

        // ── Repositories (Scoped — one per HTTP request) ──
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IOutboxRepository, OutboxRepository>();

        // ── HTTP Client for InventoryService ──
        services.AddHttpClient("InventoryService", client =>
        {
            client.BaseAddress = new Uri(config["InventoryService:BaseUrl"]!);
            client.DefaultRequestHeaders.Add("Accept", "application/json");
        });

        // ── Background Sync Service ──
        services.AddHostedService<HttpOutboxRelay>();

        return services;
    }
}
