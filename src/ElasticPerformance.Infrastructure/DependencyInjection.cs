using ElasticPerformance.Application.Interfaces;
using ElasticPerformance.Infrastructure.Configuration;
using ElasticPerformance.Infrastructure.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ElasticPerformance.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<ElasticsearchSettings>(
            configuration.GetSection("ElasticsearchSettings"));

        services.AddSingleton<ICuentaBancariaRepository, CuentaBancariaRepository>();

        return services;
    }
}
