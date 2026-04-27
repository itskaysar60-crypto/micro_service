using Microsoft.Extensions.DependencyInjection;
using InventoryService.Application.Services;

namespace InventoryService.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IInventoryService, InventoryAppService>();
        return services;
    }
}
