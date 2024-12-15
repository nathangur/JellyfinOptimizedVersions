using System;
using System.Collections.Generic;
using System.IO;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Controller;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nathan.Plugin.OptimizedVersions.Configuration;
using Nathan.Plugin.OptimizedVersions.Data;
using Nathan.Plugin.OptimizedVersions.Services;

namespace Nathan.Plugin.OptimizedVersions
{
    /// <summary>
    /// The main plugin class for the plugin.
    /// </summary>
    public class OptimizedVersionsPlugin : BasePlugin<PluginConfiguration>, IHasWebPages
    {
        private readonly ILogger<OptimizedVersionsPlugin> _logger;
        private readonly IServerApplicationHost _applicationHost;

        /// <summary>
        /// Initializes a new instance of the <see cref="OptimizedVersionsPlugin"/> class.
        /// </summary>
        /// <param name="applicationPaths">Instance of the <see cref="IApplicationPaths"/> interface.</param>
        /// <param name="xmlSerializer">Instance of the <see cref="IXmlSerializer"/> interface.</param>
        /// <param name="logger">Instance of the <see cref="ILogger"/> interface.</param>
        /// <param name="applicationHost">Instance of the <see cref="IServerApplicationHost"/> interface.</param>
        public OptimizedVersionsPlugin(
            IApplicationPaths applicationPaths,
            IXmlSerializer xmlSerializer,
            ILogger<OptimizedVersionsPlugin> logger,
            IServerApplicationHost applicationHost)
            : base(applicationPaths, xmlSerializer)
        {
            ArgumentNullException.ThrowIfNull(applicationPaths);

            _logger = logger;
            _applicationHost = applicationHost;
            Instance = this;

            // Ensure data directory exists
            var dataPath = Path.Combine(applicationPaths.DataPath, "OptimizedVersions");
            Directory.CreateDirectory(dataPath);

            // Register services
            if (_applicationHost is IServiceProvider serviceProvider)
            {
                serviceProvider.GetService<IServiceCollection>()?.RegisterOptimizedVersionsServices();
            }
        }

        /// <summary>
        /// Gets the current instance of the plugin.
        /// </summary>
        public static OptimizedVersionsPlugin? Instance { get; private set; }

        /// <inheritdoc />
        public override string Name => "Optimized Versions";

        /// <inheritdoc />
        public override string Description => "Provides optimized versions of media files.";

        /// <inheritdoc />
        public override Guid Id => new Guid("d6853bbd-0b01-466b-bfb8-eeaa4de3bc80");

        /// <inheritdoc />
        public IEnumerable<PluginPageInfo> GetPages()
        {
            return new[]
            {
                new PluginPageInfo
                {
                    Name = "optimizedversions",
                    EmbeddedResourcePath = GetType().Namespace + ".Configuration.configPage.html",
                    MenuSection = "server",
                    MenuIcon = "smart_display",
                    DisplayName = "Optimized Versions"
                }
            };
        }
    }
}
