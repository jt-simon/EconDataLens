using EconDataLens.Application.Cpi;
using Microsoft.Extensions.DependencyInjection;

namespace EconDataLens.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<ICpiIngestionService, CpiIngestionService>();
        services.AddScoped<ICpiEtlService, CpiEtlService>();
        return services;
    }
}
