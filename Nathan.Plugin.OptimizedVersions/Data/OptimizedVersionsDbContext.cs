using System;
using System.IO;
using MediaBrowser.Common.Configuration;
using Microsoft.EntityFrameworkCore;

namespace Nathan.Plugin.OptimizedVersions.Data
{
    /// <summary>
    /// Database context for optimized versions.
    /// </summary>
    public class OptimizedVersionsDbContext : DbContext
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="OptimizedVersionsDbContext"/> class.
        /// </summary>
        /// <param name="options">The options to be used by a <see cref="DbContext"/>.</param>
        public OptimizedVersionsDbContext(DbContextOptions<OptimizedVersionsDbContext> options)
            : base(options)
        {
        }

        /// <summary>
        /// Gets or sets the jobs table.
        /// </summary>
        public DbSet<OptimizedVersionJob> Jobs { get; set; } = null!;

        /// <summary>
        /// Gets or sets the files table.
        /// </summary>
        public DbSet<OptimizedVersionFile> Files { get; set; } = null!;

        /// <inheritdoc />
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            ArgumentNullException.ThrowIfNull(modelBuilder);

            modelBuilder.Entity<OptimizedVersionJob>(entity =>
            {
                entity.HasKey(e => e.JobId);
                entity.Property(e => e.Status).IsRequired();
            });

            modelBuilder.Entity<OptimizedVersionFile>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.FilePath).IsRequired();
                entity.HasOne(e => e.Job)
                    .WithMany()
                    .HasForeignKey(e => e.JobId)
                    .OnDelete(DeleteBehavior.SetNull);
            });
        }
    }
}
