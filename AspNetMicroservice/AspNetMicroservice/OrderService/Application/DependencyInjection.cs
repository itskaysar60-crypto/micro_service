using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using OrderService.Application.Services;

namespace OrderService.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IOrderService, OrderAppService>();

        // ── FluentValidation — scan this assembly for all AbstractValidator<T> ──
        services.AddValidatorsFromAssemblyContaining<Validators.CreateOrderDtoValidator>();

        return services;
    }
}
