using MediaBrowser.Controller;
using Microsoft.Extensions.DependencyInjection;
using Nathan.Plugin.OptimizedVersions.Data;
using Nathan.Plugin.OptimizedVersions.Services;
using MediaBrowser.Controller.Plugins;
using Microsoft.EntityFrameworkCore;

namespace Nathan.Plugin.OptimizedVersions;

/// <summary>
/// Provides service registration for the OptimizedVersions plugin.
/// </summary>
public class ServiceRegistrator : IPluginServiceRegistrator
{
    /// <inheritdoc />
    public void RegisterServices(IServiceCollection serviceCollection, IServerApplicationHost applicationHost)
    {
        serviceCollection.AddDbContextFactory<OptimizedVersionsDbContext>(options =>
            options.UseSqlite("Data Source=jellyfin.db"),
            ServiceLifetime.Singleton
        );
        serviceCollection.AddScoped<IOptimizedVersionsDbService, OptimizedVersionsDbService>();
        serviceCollection.AddSingleton<TranscodingService>();
        serviceCollection.AddSingleton<ITranscodingService>(sp => sp.GetRequiredService<TranscodingService>());
        serviceCollection.AddHostedService(sp => sp.GetRequiredService<TranscodingService>());
    }
}