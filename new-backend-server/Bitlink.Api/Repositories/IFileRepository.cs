using Bitlink.Api.Data.Entities;

namespace Bitlink.Api.Repositories;

/// <summary>
/// Data access contract for files stored in PostgreSQL.
/// </summary>
public interface IFileRepository
{
    Task<bool> CreateNewFileAsync(FileEntity file);
    Task<List<FileEntity>> ViewFilesAsync(string userId, string? parentId, int limit, int offset);
    Task<FileEntity?> FetchFileDataAsync(string fileId);
    Task<bool> RenameFileAsync(string fileId, string newFileName, DateTime modifiedAt);
    Task<bool> DeleteFileAsync(string fileId);
}
