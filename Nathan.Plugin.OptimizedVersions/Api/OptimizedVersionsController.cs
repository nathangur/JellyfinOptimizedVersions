using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Library;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Nathan.Plugin.OptimizedVersions.Configuration;
using Nathan.Plugin.OptimizedVersions.Data;
using Nathan.Plugin.OptimizedVersions.Services;

namespace Nathan.Plugin.OptimizedVersions.Api
{
    /// <summary>
    /// The optimized versions controller.
    /// </summary>
    [ApiController]
    [Authorize]
    [Route("OptimizedVersions")]
    public class OptimizedVersionsController : ControllerBase
    {
        private readonly ILogger<OptimizedVersionsController> _logger;
        private readonly ILibraryManager _libraryManager;
        private readonly ITranscodingService _transcodingService;
        private readonly IOptimizedVersionsDbService _dbService;
        private readonly IApplicationPaths _applicationPaths;

        /// <summary>
        /// Initializes a new instance of the <see cref="OptimizedVersionsController"/> class.
        /// </summary>
        /// <param name="logger">Logger instance.</param>
        /// <param name="libraryManager">Library manager instance.</param>
        /// <param name="transcodingService">Transcoding service instance.</param>
        /// <param name="dbService">Database service instance.</param>
        /// <param name="applicationPaths">Application paths instance.</param>
        public OptimizedVersionsController(
            ILogger<OptimizedVersionsController> logger,
            ILibraryManager libraryManager,
            ITranscodingService transcodingService,
            IOptimizedVersionsDbService dbService,
            IApplicationPaths applicationPaths)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _libraryManager = libraryManager ?? throw new ArgumentNullException(nameof(libraryManager));
            _transcodingService = transcodingService ?? throw new ArgumentNullException(nameof(transcodingService));
            _dbService = dbService ?? throw new ArgumentNullException(nameof(dbService));
            _applicationPaths = applicationPaths ?? throw new ArgumentNullException(nameof(applicationPaths));
        }

        /// <summary>
        /// Request an optimized version of a media item.
        /// </summary>
        /// <param name="itemId">The item ID.</param>
        /// <param name="deviceId">Optional device ID.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <response code="200">Optimized version request accepted.</response>
        /// <response code="404">Item not found.</response>
        /// <returns>The job ID.</returns>
        [HttpPost("Request/{itemId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<string>> RequestOptimizedVersion(
            [Required] Guid itemId,
            [FromQuery] string? deviceId,
            CancellationToken cancellationToken)
        {
            var item = _libraryManager.GetItemById(itemId);
            if (item == null)
            {
                return NotFound("Item not found");
            }

            try
            {
                var jobId = await _transcodingService.StartTranscodeJob(item, deviceId, cancellationToken)
                    .ConfigureAwait(false);
                return Ok(jobId);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                _logger.LogError(ex, "Error starting transcode job for item {ItemId}: {Message}", itemId, ex.Message);
                return StatusCode(StatusCodes.Status500InternalServerError, "Error starting transcode job");
            }
        }

        /// <summary>
        /// Cancel an optimization job.
        /// </summary>
        /// <param name="jobId">The job ID to cancel.</param>
        /// <returns>Action result.</returns>
        [HttpDelete("jobs/{jobId}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CancelJob([Required] string jobId)
        {
            if (string.IsNullOrWhiteSpace(jobId))
            {
                return BadRequest("Job ID is required");
            }

            _logger.LogInformation("Cancellation request for job: {JobId}", jobId);

            try
            {
                await _transcodingService.CancelTranscodeJob(jobId).ConfigureAwait(false);
                return NoContent();
            }
            catch (InvalidOperationException)
            {
                return NotFound("Job not found or already completed");
            }
        }

        /// <summary>
        /// Get the status of an optimization job.
        /// </summary>
        /// <param name="jobId">The job ID.</param>
        /// <returns>The job status.</returns>
        [HttpGet("Status/{jobId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<OptimizedVersionJob>> GetJobStatus([Required] string jobId)
        {
            if (string.IsNullOrWhiteSpace(jobId))
            {
                return BadRequest("Job ID is required");
            }

            var job = await _dbService.GetJobAsync(jobId).ConfigureAwait(false);
            if (job == null)
            {
                return NotFound("Job not found");
            }

            return Ok(job);
        }

        /// <summary>
        /// Get all optimization jobs.
        /// </summary>
        /// <param name="deviceId">Optional device ID to filter by.</param>
        /// <returns>List of jobs.</returns>
        [HttpGet("Jobs")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<OptimizedVersionJob>>> GetAllJobs([FromQuery] string? deviceId = null)
        {
            var jobs = await _dbService.GetAllJobsAsync(deviceId).ConfigureAwait(false);
            return Ok(jobs);
        }

        /// <summary>
        /// Download an optimized version file.
        /// </summary>
        /// <param name="jobId">The job ID.</param>
        /// <returns>The file stream.</returns>
        [HttpGet("jobs/{jobId}/download")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DownloadOptimizedFile([Required] string jobId)
        {
            if (string.IsNullOrWhiteSpace(jobId) || !Guid.TryParse(jobId, out _))
            {
                return BadRequest("Invalid Job ID format");
            }

            try
            {
                var job = await _dbService.GetJobAsync(jobId).ConfigureAwait(false);
                if (job == null || job.Status != TranscodeStatus.Completed || string.IsNullOrEmpty(job.OutputPath))
                {
                    return NotFound("File not found or job not completed");
                }

                // Build the full path based on the known output directory
                var fullPath = Path.Combine(_applicationPaths.DataPath, "OptimizedVersions", job.OutputPath);

                // Validate the path is safe
                if (!IsPathSafe(fullPath))
                {
                    _logger.LogWarning("Attempted access to potentially unsafe path: {Path}", fullPath);
                    return StatusCode(StatusCodes.Status403Forbidden, "Access denied");
                }

                var contentType = GetContentType(Path.GetExtension(fullPath));
                var fileName = Path.GetFileName(fullPath);

                return PhysicalFile(fullPath, contentType, fileName);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogError(ex, "Unauthorized access attempt for job {JobId}", jobId);
                return StatusCode(StatusCodes.Status403Forbidden, "Access denied");
            }
            catch (Exception ex) when (ex is IOException or InvalidOperationException)
            {
                _logger.LogError(ex, "Error accessing file for job {JobId}: {Message}", jobId, ex.Message);
                return StatusCode(StatusCodes.Status500InternalServerError, "Error accessing file");
            }
        }

        /// <summary>
        /// Delete all cached files.
        /// </summary>
        /// <returns>Action result.</returns>
        [HttpDelete("Cache")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteCache()
        {
            try
            {
                await _dbService.ClearCacheAsync().ConfigureAwait(false);
                return NoContent();
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                _logger.LogError(ex, "Error deleting cache: {Message}", ex.Message);
                return StatusCode(StatusCodes.Status500InternalServerError, "Failed to delete cache");
            }
        }

        /// <summary>
        /// Creates an optimized version for the specified item.
        /// </summary>
        /// <param name="itemId">The item ID.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The created job.</returns>
        [HttpPost("Items/{itemId}/Optimize")]
        [ProducesResponseType(StatusCodes.Status202Accepted)]
        public async Task<ActionResult<OptimizedVersionJob>> CreateOptimizedVersion(
            [FromRoute] Guid itemId,
            CancellationToken cancellationToken)
        {
            var item = _libraryManager.GetItemById(itemId);
            if (item == null)
            {
                return NotFound();
            }

            var jobId = await _transcodingService.StartTranscodeJob(item, null, cancellationToken)
                .ConfigureAwait(false);

            var job = await _dbService.GetJobAsync(jobId).ConfigureAwait(false);
            if (job == null)
            {
                return StatusCode(500, "Failed to create job");
            }

            return Accepted(new Uri($"/OptimizedVersions/{job.JobId}", UriKind.Relative), job);
        }

        private static string GetContentType(string extension)
        {
            return extension.ToUpperInvariant() switch
            {
                ".MKV" => "video/x-matroska",
                ".MP4" => "video/mp4",
                ".AVI" => "video/x-msvideo",
                ".MOV" => "video/quicktime",
                ".WMV" => "video/x-ms-wmv",
                _ => "application/octet-stream"
            };
        }

        private static async Task<string?> GetFileHashAsync(string filePath)
        {
            if (!System.IO.File.Exists(filePath))
            {
                return null;
            }

            using var sha256 = SHA256.Create();
            using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            var hash = await sha256.ComputeHashAsync(stream).ConfigureAwait(false);
            return Convert.ToBase64String(hash);
        }

        private string ValidateAndCreatePath(Guid itemId)
        {
            var itemPath = Path.Combine(_applicationPaths.DataPath, "OptimizedVersions");
            var targetPath = Path.Combine(itemPath, itemId.ToString("N"));
            var fullPath = Path.GetFullPath(targetPath);

            // Ensure the path is within the allowed directory
            var baseFullPath = Path.GetFullPath(_applicationPaths.DataPath);
            if (!fullPath.StartsWith(baseFullPath, StringComparison.OrdinalIgnoreCase))
            {
                throw new SecurityException($"Invalid path detected: {fullPath}");
            }

            if (!Directory.Exists(fullPath))
            {
                Directory.CreateDirectory(fullPath);
            }

            return fullPath;
        }

        private string SanitizePath(string path)
        {
            ArgumentNullException.ThrowIfNull(path);

            try
            {
                // Convert to absolute path and normalize
                var fullPath = Path.GetFullPath(path);
                var allowedDirectory = Path.GetFullPath(_applicationPaths.DataPath);

                // Verify the path is within the allowed directory
                if (!fullPath.StartsWith(allowedDirectory, StringComparison.OrdinalIgnoreCase))
                {
                    throw new UnauthorizedAccessException("Access to the path is not allowed");
                }

                // Additional validation
                if (Path.GetFileName(fullPath).Contains("..", StringComparison.OrdinalIgnoreCase))
                {
                    throw new UnauthorizedAccessException("Invalid path");
                }

                return fullPath;
            }
            catch (Exception ex) when (ex is ArgumentException or PathTooLongException)
            {
                throw new UnauthorizedAccessException("Invalid path", ex);
            }
        }

        private bool IsPathSafe(string path)
        {
            try
            {
                // Validate input
                if (string.IsNullOrWhiteSpace(path))
                {
                    return false;
                }

                // Get the allowed base directory and normalize it
                var allowedDirectory = Path.GetFullPath(_applicationPaths.DataPath);

                // Normalize the provided path
                var normalizedPath = Path.GetFullPath(path);

                // Check if the normalized path starts with the allowed directory
                if (!normalizedPath.StartsWith(allowedDirectory + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                // Ensure there are no invalid characters in the path
                if (normalizedPath.IndexOfAny(Path.GetInvalidPathChars()) >= 0)
                {
                    return false;
                }

                // Ensure the path does not contain any directory traversal sequences
                if (normalizedPath.Contains(".." + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                return true;
            }
            catch (Exception ex) when (
                ex is ArgumentException
                || ex is PathTooLongException
                || ex is NotSupportedException
                || ex is SecurityException)
            {
                _logger.LogWarning(ex, "Path validation failed for path: {Path}", path);
                return false;
            }
        }
    }
}
