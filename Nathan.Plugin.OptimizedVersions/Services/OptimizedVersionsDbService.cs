using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Nathan.Plugin.OptimizedVersions.Data;

namespace Nathan.Plugin.OptimizedVersions.Services;

/// <summary>
/// Service for managing optimized version database operations.
/// </summary>
public class OptimizedVersionsDbService : IOptimizedVersionsDbService, IDisposable
{
    private readonly OptimizedVersionsDbContext _dbContext;
    private readonly ILogger<OptimizedVersionsDbService> _logger;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="OptimizedVersionsDbService"/> class.
    /// </summary>
    /// <param name="dbContext">Database context instance.</param>
    /// <param name="logger">Logger instance.</param>
    public OptimizedVersionsDbService(
        OptimizedVersionsDbContext dbContext,
        ILogger<OptimizedVersionsDbService> logger)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets all pending jobs.
    /// </summary>
    /// <returns>List of pending jobs.</returns>
    public async Task<List<OptimizedVersionJob>> GetPendingJobsAsync()
    {
        return await _dbContext.Jobs
            .Where(j => j.Status == TranscodeStatus.Pending || j.Status == TranscodeStatus.Processing)
            .ToListAsync()
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Creates a new job in the database.
    /// </summary>
    /// <param name="job">The job to create.</param>
    /// <returns>The created job.</returns>
    public async Task<OptimizedVersionJob> CreateJobAsync(OptimizedVersionJob job)
    {
        _dbContext.Jobs.Add(job);
        await _dbContext.SaveChangesAsync().ConfigureAwait(false);
        return job;
    }

    /// <summary>
    /// Updates an existing job in the database.
    /// </summary>
    /// <param name="job">The job to update.</param>
    /// <returns>The updated job.</returns>
    public async Task<OptimizedVersionJob> UpdateJobAsync(OptimizedVersionJob job)
    {
        _dbContext.Jobs.Update(job);
        await _dbContext.SaveChangesAsync().ConfigureAwait(false);
        return job;
    }

    /// <summary>
    /// Gets a job by its ID.
    /// </summary>
    /// <param name="jobId">The job ID.</param>
    /// <returns>The job if found, null otherwise.</returns>
    public async Task<OptimizedVersionJob?> GetJobAsync(string jobId)
    {
        return await _dbContext.Jobs.FindAsync(jobId).ConfigureAwait(false);
    }

    /// <summary>
    /// Gets all jobs, optionally filtered by device ID.
    /// </summary>
    /// <param name="deviceId">Optional device ID to filter by.</param>
    /// <returns>List of jobs.</returns>
    public async Task<List<OptimizedVersionJob>> GetAllJobsAsync(string? deviceId = null)
    {
        var query = _dbContext.Jobs.AsQueryable();
        if (!string.IsNullOrEmpty(deviceId))
        {
            query = query.Where(j => j.DeviceId == deviceId);
        }

        return await query.ToListAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Creates a new file record in the database.
    /// </summary>
    /// <param name="file">The file record to create.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task CreateFileAsync(OptimizedVersionFile file)
    {
        _dbContext.Files.Add(file);
        await _dbContext.SaveChangesAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Clears the database cache.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task ClearCacheAsync()
    {
        _dbContext.Jobs.RemoveRange(_dbContext.Jobs);
        _dbContext.Files.RemoveRange(_dbContext.Files);
        await _dbContext.SaveChangesAsync().ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Disposes the managed and unmanaged resources used by the OptimizedVersionsDbService.
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
            _dbContext.Dispose();
        }

        _disposed = true;
    }
}
