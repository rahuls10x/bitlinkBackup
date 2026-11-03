using Bitlink.Api.Data;
using Bitlink.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Bitlink.Api.Repositories;

/// <summary>
/// PostgreSQL repository for file management using Entity Framework Core.
/// Replaces the MongoDB FileRepository.
/// </summary>
public class FileRepository : IFileRepository
{
    private readonly BitlinkDbContext _dbContext;

    public FileRepository(BitlinkDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Inserts a newly finalized file into the database.
    /// </summary>
    public async Task<bool> CreateNewFileAsync(FileEntity file)
    {
        await _dbContext.Files.AddAsync(file);
        var affected = await _dbContext.SaveChangesAsync();
        return affected > 0;
    }

    /// <summary>
    /// Retrieves user files ordered by creation date descending with limit and offset.
    /// </summary>
    public async Task<List<FileEntity>> ViewFilesAsync(string userId, string? parentId, int limit, int offset)
    {
        return await _dbContext.Files
            .AsNoTracking()
            .Where(f => f.UserId == userId && f.ParentId == parentId)
            .OrderByDescending(f => f.CreatedAt)
            .Skip(offset)
            .Take(limit)
            .ToListAsync();
    }

    /// <summary>
    /// Fetches file metadata by file identifier.
    /// </summary>
    public async Task<FileEntity?> FetchFileDataAsync(string fileId)
    {
        return await _dbContext.Files
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.FileId == fileId);
    }

    /// <summary>
    /// Renames a file and updates the modifiedAt timestamp.
    /// </summary>
    public async Task<bool> RenameFileAsync(string fileId, string newFileName, DateTime modifiedAt)
    {
        var file = await _dbContext.Files.FirstOrDefaultAsync(f => f.FileId == fileId);
        if (file is null)
        {
            return false;
        }

        file.FileName = newFileName;
        file.ModifiedAt = modifiedAt;

        var affected = await _dbContext.SaveChangesAsync();
        return affected > 0;
    }

    /// <summary>
    /// Deletes a file record from the database.
    /// </summary>
    public async Task<bool> DeleteFileAsync(string fileId)
    {
        var file = await _dbContext.Files.FirstOrDefaultAsync(f => f.FileId == fileId);
        if (file is null)
        {
            return false;
        }

        _dbContext.Files.Remove(file);
        var affected = await _dbContext.SaveChangesAsync();
        return affected > 0;
    }
}
