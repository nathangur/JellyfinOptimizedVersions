using System;

namespace Nathan.Plugin.OptimizedVersions.Data
{
    /// <summary>
    /// Represents a stored optimized version file.
    /// </summary>
    public class OptimizedVersionFile
    {
        /// <summary>
        /// Gets or sets the file ID.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the original item ID.
        /// </summary>
        public Guid ItemId { get; set; }

        /// <summary>
        /// Gets or sets the job ID that created this file.
        /// </summary>
        public string? JobId { get; set; }

        /// <summary>
        /// Gets or sets the file path.
        /// </summary>
        public string FilePath { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the profile name used.
        /// </summary>
        public string ProfileName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the creation time.
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Gets or sets the file size in bytes.
        /// </summary>
        public long FileSize { get; set; }

        /// <summary>
        /// Gets or sets the associated job.
        /// </summary>
        public virtual OptimizedVersionJob? Job { get; set; }
    }
}
