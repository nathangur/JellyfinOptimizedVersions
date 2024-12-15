using System;
using MediaBrowser.Controller;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Nathan.Plugin.OptimizedVersions.Data;
using Nathan.Plugin.OptimizedVersions.Services;

namespace Nathan.Plugin.OptimizedVersions;

/// <summary>
/// Provides service registration for the OptimizedVersions plugin.
/// </summary>
public static class ServiceRegistrator
{
    /// <summary>
    /// Registers required services for the OptimizedVersions plugin.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection RegisterOptimizedVersionsServices(this IServiceCollection services)
    {
        services.AddDbContextFactory<OptimizedVersionsDbContext>();
        services.AddScoped<IOptimizedVersionsDbService, OptimizedVersionsDbService>();
        services.AddSingleton<TranscodingService>();
        services.AddSingleton<ITranscodingService>(sp => sp.GetRequiredService<TranscodingService>());
        services.AddHostedService(sp => sp.GetRequiredService<TranscodingService>());

        return services;
    }
}
