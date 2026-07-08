using ElasticPerformance.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace ElasticPerformance.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<CuentaBancariaService>();
        return services;
    }
}
