using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Devices;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Devices;
using MediaBrowser.Model.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Nathan.Plugin.OptimizedVersions.Configuration;
using Nathan.Plugin.OptimizedVersions.Data;

namespace Nathan.Plugin.OptimizedVersions.Services;

/// <summary>
/// Service for handling video transcoding operations.
/// </summary>
public class TranscodingService : ITranscodingService, IHostedService, IDisposable
{
    private const string DefaultVideoCodec = "libx264";
    private const string DefaultAudioCodec = "aac";
    private const string DefaultPreset = "medium";
    private const string DefaultVideoSize = "1920x1080";
    private const string DefaultVideoBitrate = "2500k";
    private const string DefaultAudioBitrate = "160k";

    private readonly ILogger<TranscodingService> _logger;
    private readonly IDbContextFactory<OptimizedVersionsDbContext> _dbContextFactory;
    private readonly ILibraryManager _libraryManager;
    private readonly IApplicationPaths _appPaths;
    private readonly ILogger<OptimizedVersionsDbService> _dbServiceLogger;
    private readonly string _outputPath;
    private readonly ConcurrentDictionary<string, Process> _activeProcesses;
    private OptimizedVersionsDbService? _dbService;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="TranscodingService"/> class.
    /// </summary>
    /// <param name="logger">Instance of the logger.</param>
    /// <param name="dbContextFactory">Instance of the database context factory.</param>
    /// <param name="libraryManager">Instance of the library manager.</param>
    /// <param name="appPaths">Instance of the application paths.</param>
    /// <param name="dbServiceLogger">Instance of the database service logger.</param>
    public TranscodingService(
        ILogger<TranscodingService> logger,
        IDbContextFactory<OptimizedVersionsDbContext> dbContextFactory,
        ILibraryManager libraryManager,
        IApplicationPaths appPaths,
        ILogger<OptimizedVersionsDbService> dbServiceLogger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _dbContextFactory = dbContextFactory ?? throw new ArgumentNullException(nameof(dbContextFactory));
        _libraryManager = libraryManager ?? throw new ArgumentNullException(nameof(libraryManager));
        _appPaths = appPaths ?? throw new ArgumentNullException(nameof(appPaths));
        _dbServiceLogger = dbServiceLogger ?? throw new ArgumentNullException(nameof(dbServiceLogger));

        _activeProcesses = new ConcurrentDictionary<string, Process>();
        _outputPath = Path.Combine(_appPaths.DataPath, "OptimizedVersions");

        if (!Directory.Exists(_outputPath))
        {
            Directory.CreateDirectory(_outputPath);
        }
    }

    private OptimizedVersionsDbService DbService
    {
        get
        {
            if (_dbService == null)
            {
                throw new InvalidOperationException("TranscodingService not initialized. Ensure StartAsync has been called.");
            }

            return _dbService;
        }
    }

    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        // Create DbService on startup
        using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        _dbService = new OptimizedVersionsDbService(context, _dbServiceLogger);

        await context.Database.EnsureCreatedAsync(cancellationToken).ConfigureAwait(false);

        // Load existing jobs
        var pendingJobs = await DbService.GetPendingJobsAsync().ConfigureAwait(false);
        foreach (var job in pendingJobs)
        {
            _logger.LogInformation("Resuming job {JobId} on startup", job.JobId);
            job.Status = TranscodeStatus.Pending;
            _ = TranscodeAsync(job, cancellationToken);
        }
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Start a new transcode job.
    /// </summary>
    /// <param name="item">The media item to transcode.</param>
    /// <param name="deviceId">Optional device ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The job ID.</returns>
    public async Task<string> StartTranscodeJob(
        BaseItem item,
        string? deviceId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(item);

        var jobId = Guid.NewGuid().ToString("N");
        var itemFolder = Path.Combine(_outputPath, item.Id.ToString("N"));

        if (!Directory.Exists(itemFolder))
        {
            Directory.CreateDirectory(itemFolder);
        }

        var outputPath = Path.Combine(
            itemFolder,
            $"{item.Name}_{DateTime.UtcNow:yyyyMMddHHmmss}.mp4");

        var job = new OptimizedVersionJob
        {
            JobId = jobId,
            ItemId = item.Id,
            Status = TranscodeStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            SourcePath = item.Path,
            OutputPath = outputPath,
            DeviceId = deviceId
        };

        await DbService.CreateJobAsync(job).ConfigureAwait(false);
        _ = TranscodeAsync(job, cancellationToken);

        return jobId;
    }

    /// <summary>
    /// Cancel an active transcode job.
    /// </summary>
    /// <param name="jobId">The job ID to cancel.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task CancelTranscodeJob(string jobId)
    {
        ArgumentNullException.ThrowIfNull(jobId);

        if (_activeProcesses.TryRemove(jobId, out var process))
        {
            try
            {
                if (!process.HasExited)
                {
                    process.Kill();
                }
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Error killing process for job {JobId}", jobId);
            }
            catch (NotSupportedException ex)
            {
                _logger.LogError(ex, "Kill operation not supported for job {JobId}", jobId);
            }
            finally
            {
                process.Dispose();
            }
        }

        var job = await DbService.GetJobAsync(jobId).ConfigureAwait(false);
        if (job != null)
        {
            job.Status = TranscodeStatus.Canceled;
            job.CompletedAt = DateTime.UtcNow;
            await DbService.UpdateJobAsync(job).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public async Task<TranscodeStatus> GetJobStatus(string jobId)
    {
        var job = await DbService.GetJobAsync(jobId).ConfigureAwait(false);
        if (job == null)
        {
            return TranscodeStatus.NotFound;
        }

        return job.Status;
    }

    /// <summary>
    /// Gets the output path for the specified job.
    /// </summary>
    /// <param name="job">The job to get the output path for.</param>
    /// <returns>The output path.</returns>
    public Task<string> GetOutputPathAsync(OptimizedVersionJob job)
    {
        return Task.Run(() =>
        {
            ArgumentNullException.ThrowIfNull(job);

            if (string.IsNullOrEmpty(job.OutputPath))
            {
                throw new InvalidOperationException("Job output path is not set");
            }

            var path = Path.GetFullPath(job.OutputPath);
            var allowedPath = Path.GetFullPath(_outputPath);

            if (!path.StartsWith(allowedPath, StringComparison.OrdinalIgnoreCase))
            {
                throw new UnauthorizedAccessException("Invalid output path location");
            }

            return path;
        });
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Disposes the managed and unmanaged resources used by the TranscodingService.
    /// </summary>
    /// <param name="disposing">True to dispose managed resources.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing)
        {
            _dbService?.Dispose();
            foreach (var process in _activeProcesses.Values)
            {
                try
                {
                    if (!process.HasExited)
                    {
                        process.Kill();
                    }

                    process.Dispose();
                }
                catch (ObjectDisposedException ex)
                {
                    _logger.LogError(ex, "Error disposing process - object already disposed");
                }
                catch (InvalidOperationException ex)
                {
                    _logger.LogError(ex, "Error disposing process - invalid operation");
                }
            }

            _activeProcesses.Clear();
        }

        _disposed = true;
    }

    private static string BuildFfmpegArguments(string inputPath, string outputPath)
    {
        ArgumentNullException.ThrowIfNull(inputPath);
        ArgumentException.ThrowIfNullOrEmpty(outputPath);

        return $"-i \"{inputPath}\" -c:v {DefaultVideoCodec} -preset {DefaultPreset} " +
               $"-s {DefaultVideoSize} -b:v {DefaultVideoBitrate} " +
               $"-c:a {DefaultAudioCodec} -b:a {DefaultAudioBitrate} " +
               $"-y \"{outputPath}\"";
    }

    private static void ValidateJobParameters(OptimizedVersionJob job)
    {
        ArgumentNullException.ThrowIfNull(job);

        if (string.IsNullOrEmpty(job.DeviceId))
        {
            throw new ArgumentException("DeviceId cannot be null or empty", nameof(job));
        }

        if (job.ItemId == Guid.Empty)
        {
            throw new ArgumentException("ItemId cannot be empty", nameof(job));
        }
    }

    private async Task TranscodeAsync(
        OptimizedVersionJob job,
        CancellationToken cancellationToken)
    {
        Process? process = null;
        try
        {
            job.Status = TranscodeStatus.Processing;
            await DbService.UpdateJobAsync(job).ConfigureAwait(false);

            if (string.IsNullOrEmpty(job.OutputPath))
            {
                throw new InvalidOperationException("Job output path is not set");
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = "ffmpeg",
                Arguments = BuildFfmpegArguments(job.SourcePath, job.OutputPath),
                UseShellExecute = false,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            process = new Process { StartInfo = startInfo };
            if (!_activeProcesses.TryAdd(job.JobId, process))
            {
                throw new InvalidOperationException($"Job {job.JobId} is already running");
            }

            process.Start();
            await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);

            if (process.ExitCode == 0)
            {
                var fileInfo = new FileInfo(job.OutputPath);
                job.Status = TranscodeStatus.Completed;
                job.Progress = 100;
                job.FileSize = fileInfo.Length;
                job.CompletedAt = DateTime.UtcNow;

                // Create the file record
                var file = new OptimizedVersionFile
                {
                    Id = Guid.NewGuid(),
                    ItemId = job.ItemId,
                    JobId = job.JobId,
                    FilePath = job.OutputPath,
                    CreatedAt = DateTime.UtcNow,
                    FileSize = fileInfo.Length
                };

                await DbService.CreateFileAsync(file).ConfigureAwait(false);
            }
            else
            {
                job.Status = TranscodeStatus.Failed;
                job.ErrorMessage = $"FFmpeg process exited with code {process.ExitCode}";
                job.CompletedAt = DateTime.UtcNow;

                if (File.Exists(job.OutputPath))
                {
                    File.Delete(job.OutputPath);
                }
            }
        }
        catch (OperationCanceledException)
        {
            job.Status = TranscodeStatus.Canceled;
            job.CompletedAt = DateTime.UtcNow;
            throw;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            job.Status = TranscodeStatus.Failed;
            job.ErrorMessage = ex.Message;
            job.CompletedAt = DateTime.UtcNow;

            if (!string.IsNullOrEmpty(job.OutputPath) && File.Exists(job.OutputPath))
            {
                File.Delete(job.OutputPath);
            }

            _logger.LogError(ex, "Error processing transcode job {JobId}: {Message}", job.JobId, ex.Message);
        }
        finally
        {
            if (process != null)
            {
                _activeProcesses.TryRemove(job.JobId, out _);
                process.Dispose();
            }

            await DbService.UpdateJobAsync(job).ConfigureAwait(false);
        }
    }
}
