using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OrderService.Domain.Interfaces;
using OrderService.Infrastructure.Messaging;
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
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // ── RabbitMQ ──
        var rmqSettings = new RabbitMqSettings
        {
            Host        = config["RabbitMq:Host"]        ?? "localhost",
            Port        = int.TryParse(config["RabbitMq:Port"], out var p) ? p : 5672,
            Username    = config["RabbitMq:Username"]    ?? "guest",
            Password    = config["RabbitMq:Password"]    ?? "guest",
            VirtualHost = config["RabbitMq:VirtualHost"] ?? "/"
        };
        services.AddSingleton(rmqSettings);            // bind settings POCO once
        services.AddSingleton<RabbitMqPublisher>();    // one connection for the whole process

        // ── Background relay: polls OutboxEvents → publishes to RabbitMQ ──
        services.AddHostedService<RabbitMqOutboxRelay>();

        return services;
    }
}

