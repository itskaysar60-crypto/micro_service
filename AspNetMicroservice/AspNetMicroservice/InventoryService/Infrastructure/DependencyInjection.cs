using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using InventoryService.Domain.Interfaces;
using InventoryService.Infrastructure.Messaging;
using InventoryService.Infrastructure.Persistence;
using InventoryService.Infrastructure.Repositories;

namespace InventoryService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        // ── Database ──
        services.AddDbContext<InventoryDbContext>(options =>
            options.UseSqlServer(config.GetConnectionString("InventoryDb")));

        // ── Repositories ──
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IProcessedEventRepository, ProcessedEventRepository>();

        // ── RabbitMQ Consumer ──
        var rmqSettings = new RabbitMqSettings
        {
            Host        = config["RabbitMq:Host"]        ?? "localhost",
            Port        = int.TryParse(config["RabbitMq:Port"], out var p) ? p : 5672,
            Username    = config["RabbitMq:Username"]    ?? "guest",
            Password    = config["RabbitMq:Password"]    ?? "guest",
            VirtualHost = config["RabbitMq:VirtualHost"] ?? "/"
        };
        services.AddSingleton(rmqSettings);
        services.AddHostedService<RabbitMqOrderConsumer>();

        return services;
    }
}

