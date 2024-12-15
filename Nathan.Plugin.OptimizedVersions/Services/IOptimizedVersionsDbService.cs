using System.Collections.Generic;
using System.Threading.Tasks;
using Nathan.Plugin.OptimizedVersions.Data;

namespace Nathan.Plugin.OptimizedVersions.Services
{
    /// <summary>
    /// Interface for the optimized versions database service.
    /// </summary>
    public interface IOptimizedVersionsDbService
    {
        /// <summary>
        /// Gets a job by its ID.
        /// </summary>
        /// <param name="jobId">The job ID.</param>
        /// <returns>The job if found, null otherwise.</returns>
        Task<OptimizedVersionJob?> GetJobAsync(string jobId);

        /// <summary>
        /// Gets all jobs, optionally filtered by device ID.
        /// </summary>
        /// <param name="deviceId">Optional device ID to filter by.</param>
        /// <returns>List of jobs.</returns>
        Task<List<OptimizedVersionJob>> GetAllJobsAsync(string? deviceId = null);

        /// <summary>
        /// Gets all pending jobs.
        /// </summary>
        /// <returns>List of pending jobs.</returns>
        Task<List<OptimizedVersionJob>> GetPendingJobsAsync();

        /// <summary>
        /// Creates a new job in the database.
        /// </summary>
        /// <param name="job">The job to create.</param>
        /// <returns>The created job.</returns>
        Task<OptimizedVersionJob> CreateJobAsync(OptimizedVersionJob job);

        /// <summary>
        /// Updates an existing job in the database.
        /// </summary>
        /// <param name="job">The job to update.</param>
        /// <returns>The updated job.</returns>
        Task<OptimizedVersionJob> UpdateJobAsync(OptimizedVersionJob job);

        /// <summary>
        /// Creates a new file record in the database.
        /// </summary>
        /// <param name="file">The file record to create.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task CreateFileAsync(OptimizedVersionFile file);

        /// <summary>
        /// Clears the database cache.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task ClearCacheAsync();
    }
}
