using Bitlink.Api.Data;
using Bitlink.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Bitlink.Api.Repositories;

/// <summary>
/// PostgreSQL repository for upload token tracking using Entity Framework Core.
/// Replaces the MongoDB collection and Redis upload cache.
/// </summary>
public class UploadRepository : IUploadRepository
{
    private readonly BitlinkDbContext _dbContext;

    public UploadRepository(BitlinkDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Creates a file upload record in the PostgreSQL database.
    /// </summary>
    public async Task<bool> CreateFileUploadTokenAsync(FileUploadEntity data)
    {
        await _dbContext.FileUploads.AddAsync(data);
        var affected = await _dbContext.SaveChangesAsync();
        return affected > 0;
    }

    /// <summary>
    /// Atomically finds and deletes a file upload token from PostgreSQL.
    /// </summary>
    public async Task<FileUploadEntity?> RetrieveAndDeleteFileUploadTokenAsync(string uploadId)
    {
        var record = await _dbContext.FileUploads.FirstOrDefaultAsync(u => u.UploadId == uploadId);
        if (record is null)
        {
            return null;
        }

        _dbContext.FileUploads.Remove(record);
        await _dbContext.SaveChangesAsync();
        return record;
    }

    /// <summary>
    /// Deletes a file upload token from PostgreSQL.
    /// </summary>
    public async Task<bool> DeleteFileUploadTokenAsync(string uploadId)
    {
        var record = await _dbContext.FileUploads.FirstOrDefaultAsync(u => u.UploadId == uploadId);
        if (record is null)
        {
            return false;
        }

        _dbContext.FileUploads.Remove(record);
        var affected = await _dbContext.SaveChangesAsync();
        return affected > 0;
    }
}
