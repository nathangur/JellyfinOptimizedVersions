using System.Collections.Generic;
using System.Collections.ObjectModel;
using MediaBrowser.Model.Plugins;

namespace Nathan.Plugin.OptimizedVersions.Configuration
{
    /// <summary>
    /// Plugin configuration settings.
    /// </summary>
    public class PluginConfiguration : BasePluginConfiguration
    {
        /// <summary>
        /// Gets or sets the output path.
        /// </summary>
        public string OutputPath { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the maximum number of concurrent jobs.
        /// </summary>
        public int MaxConcurrentJobs { get; set; }

        /// <summary>
        /// Gets or sets the default preset.
        /// </summary>
        public string DefaultPreset { get; set; } = "medium";

        /// <summary>
        /// Gets or sets the default CRF value.
        /// </summary>
        public int DefaultCrf { get; set; } = 23;

        /// <summary>
        /// Gets or sets the maximum storage in GB.
        /// </summary>
        public int MaxStorageGB { get; set; } = 100;

        /// <summary>
        /// Gets or sets the retention days.
        /// </summary>
        public int RetentionDays { get; set; } = 30;

        /// <summary>
        /// Gets or sets the default container format.
        /// </summary>
        public string DefaultContainer { get; set; } = "mkv";

        /// <summary>
        /// Gets or sets the hardware acceleration method.
        /// </summary>
        public string HardwareAcceleration { get; set; } = "none";
    }
}
