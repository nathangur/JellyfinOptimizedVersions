using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities;
using Nathan.Plugin.OptimizedVersions.Configuration;
using Nathan.Plugin.OptimizedVersions.Data;

namespace Nathan.Plugin.OptimizedVersions.Services;

/// <summary>
/// Interface for transcoding service operations.
/// </summary>
public interface ITranscodingService
{
    /// <summary>
    /// Starts a new transcode job.
    /// </summary>
    /// <param name="item">The media item to transcode.</param>
    /// <param name="deviceId">Optional device ID. Can be null.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The job ID.</returns>
    Task<string> StartTranscodeJob(BaseItem item, string? deviceId, CancellationToken cancellationToken);

    /// <summary>
    /// Cancels a transcode job.
    /// </summary>
    /// <param name="jobId">The job ID to cancel.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task CancelTranscodeJob(string jobId);

    /// <summary>
    /// Gets the output path for a job.
    /// </summary>
    /// <param name="job">The job.</param>
    /// <returns>The output path.</returns>
    Task<string> GetOutputPathAsync(OptimizedVersionJob job);

    /// <summary>
    /// Gets the status of a transcode job.
    /// </summary>
    /// <param name="jobId">The job ID to check.</param>
    /// <returns>The current status of the job.</returns>
    Task<TranscodeStatus> GetJobStatus(string jobId);

    /// <summary>
    /// Starts the transcoding service.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task StartAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Stops the transcoding service.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task StopAsync(CancellationToken cancellationToken);
}
