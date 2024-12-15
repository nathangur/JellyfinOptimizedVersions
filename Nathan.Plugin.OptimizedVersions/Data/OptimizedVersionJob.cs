using System;

namespace Nathan.Plugin.OptimizedVersions.Data
{
    /// <summary>
    /// Represents the status of a transcode job.
    /// </summary>
    public enum TranscodeStatus
    {
        /// <summary>
        /// Job not found.
        /// </summary>
        NotFound,

        /// <summary>
        /// Job is pending.
        /// </summary>
        Pending,

        /// <summary>
        /// Job is processing.
        /// </summary>
        Processing,

        /// <summary>
        /// Job completed successfully.
        /// </summary>
        Completed,

        /// <summary>
        /// Job failed.
        /// </summary>
        Failed,

        /// <summary>
        /// Job was canceled.
        /// </summary>
        Canceled,
    }

    /// <summary>
    /// Represents a job for creating an optimized version.
    /// </summary>
    public class OptimizedVersionJob
    {
        /// <summary>
        /// Gets or sets the job ID.
        /// </summary>
        public string JobId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the item ID.
        /// </summary>
        public Guid ItemId { get; set; }

        /// <summary>
        /// Gets or sets the job status.
        /// </summary>
        public TranscodeStatus Status { get; set; }

        /// <summary>
        /// Gets or sets the progress percentage.
        /// </summary>
        public float? Progress { get; set; }

        /// <summary>
        /// Gets or sets the creation time.
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Gets or sets the completion time.
        /// </summary>
        public DateTime? CompletedAt { get; set; }

        /// <summary>
        /// Gets or sets the source file path.
        /// </summary>
        public string SourcePath { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the output file path.
        /// </summary>
        public string? OutputPath { get; set; }

        /// <summary>
        /// Gets or sets the output file size in bytes.
        /// </summary>
        public long? FileSize { get; set; }

        /// <summary>
        /// Gets or sets the device ID.
        /// </summary>
        public string? DeviceId { get; set; }

        /// <summary>
        /// Gets or sets the error message if any.
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Gets or sets the current FPS during transcoding.
        /// </summary>
        public double? CurrentFps { get; set; }

        /// <summary>
        /// Gets or sets the current bitrate during transcoding.
        /// </summary>
        public double? CurrentBitrate { get; set; }

        /// <summary>
        /// Gets or sets the estimated time remaining for transcoding.
        /// </summary>
        public TimeSpan? TimeRemaining { get; set; }
    }
}
