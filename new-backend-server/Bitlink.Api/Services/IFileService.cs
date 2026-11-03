using Bitlink.Api.Data.Entities;
using Bitlink.Api.Models;

namespace Bitlink.Api.Services;

/// <summary>
/// Business logic contract for user file management.
/// </summary>
public interface IFileService
{
    Task<List<FileEntity>> ViewFilesAsync(ViewFilesInput query, string userId);
    Task<DownloadResponseData> RequestingToDownloadAsync(string fileId, string userId);
    Task RenameFileAsync(RenameFileInput data, string userId);
    Task DeleteFileAsync(DeleteFileInput data, string userId);
}
