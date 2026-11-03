using Bitlink.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Bitlink.Api.Data;

/// <summary>
/// Entity Framework Core DbContext for Bitlink using PostgreSQL.
/// Manages sessions, files, and file uploads tables, and provides helper
/// methods for transaction-scoped advisory locking.
/// </summary>
public class BitlinkDbContext : DbContext
{
    public BitlinkDbContext(DbContextOptions<BitlinkDbContext> options) : base(options)
    {
    }

    public DbSet<SessionEntity> Sessions => Set<SessionEntity>();
    public DbSet<FileEntity> Files => Set<FileEntity>();
    public DbSet<FileUploadEntity> FileUploads => Set<FileUploadEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // SessionEntity configuration & indexes
        modelBuilder.Entity<SessionEntity>(entity =>
        {
            entity.HasKey(e => e.SessionId);
            entity.HasIndex(e => e.SessionId)
                .HasFilter("is_revoked = false")
                .HasDatabaseName("idx_sessions_active");
            entity.HasIndex(e => e.UserId)
                .HasDatabaseName("idx_sessions_user_id");
        });

        // FileEntity configuration & indexes
        modelBuilder.Entity<FileEntity>(entity =>
        {
            entity.HasKey(e => e.FileId);
            entity.HasIndex(e => new { e.UserId, e.ParentId })
                .HasDatabaseName("idx_files_user_parent");
        });

        // FileUploadEntity configuration & indexes
        modelBuilder.Entity<FileUploadEntity>(entity =>
        {
            entity.HasKey(e => e.UploadId);
            entity.HasIndex(e => e.ExpiresAt)
                .HasDatabaseName("idx_file_uploads_expires");
            entity.HasIndex(e => e.FileId)
                .HasDatabaseName("idx_file_uploads_file_id");
        });
    }

    /// <summary>
    /// Acquires a PostgreSQL transaction-scoped advisory lock on a resource name.
    /// Uses hashtext() to generate a consistent 32-bit hash of the resource key.
    /// The lock is scoped to the current database transaction and is automatically
    /// released when the transaction commits or rolls back.
    /// </summary>
    /// <param name="resourceKey">Unique identifier string to lock (e.g. "refresh:session_xxx")</param>
    public async Task AcquireAdvisoryXactLockAsync(string resourceKey)
    {
        await Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(hashtext({0}))", resourceKey);
    }
}
