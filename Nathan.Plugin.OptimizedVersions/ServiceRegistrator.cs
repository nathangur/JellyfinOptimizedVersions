using MediaBrowser.Controller;
using Microsoft.Extensions.DependencyInjection;
using Nathan.Plugin.OptimizedVersions.Data;
using Nathan.Plugin.OptimizedVersions.Services;
using MediaBrowser.Controller.Plugins;
using Microsoft.EntityFrameworkCore;
using MediaBrowser.Common.Configuration;
using System.IO;

namespace Nathan.Plugin.OptimizedVersions;

/// <summary>
/// Provides service registration for the OptimizedVersions plugin.
/// </summary>
public class ServiceRegistrator : IPluginServiceRegistrator
{
    /// <inheritdoc />
    public void RegisterServices(IServiceCollection serviceCollection, IServerApplicationHost applicationHost)
    {
        serviceCollection.AddDbContextFactory<OptimizedVersionsDbContext>((serviceProvider, opt) =>
        {
            var applicationPaths = serviceProvider.GetRequiredService<IApplicationPaths>();
            opt.UseSqlite($"Filename={Path.Combine(applicationPaths.DataPath, "jobs.db")}");
        });
        serviceCollection.AddScoped<IOptimizedVersionsDbService, OptimizedVersionsDbService>();
        serviceCollection.AddSingleton<TranscodingService>();
        serviceCollection.AddSingleton<ITranscodingService>(sp => sp.GetRequiredService<TranscodingService>());
        serviceCollection.AddHostedService(sp => sp.GetRequiredService<TranscodingService>());
    }
}