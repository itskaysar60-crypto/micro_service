using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using InventoryService.Domain.Interfaces;
using InventoryService.Infrastructure.Persistence;
using InventoryService.Infrastructure.Repositories;

namespace InventoryService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        services.AddDbContext<InventoryDbContext>(options => options.UseSqlServer(config.GetConnectionString("InventoryDb")));

        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IProcessedEventRepository, ProcessedEventRepository>();

        return services;
    }
}
