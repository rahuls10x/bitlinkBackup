using Bitlink.Api.Data.Entities;

namespace Bitlink.Api.Repositories;

/// <summary>
/// Data access contract for pending file upload tokens.
/// Replaces both the MongoDB UploadRepository and Redis upload cache.
/// </summary>
public interface IUploadRepository
{
    Task<bool> CreateFileUploadTokenAsync(FileUploadEntity data);
    Task<FileUploadEntity?> RetrieveAndDeleteFileUploadTokenAsync(string uploadId);
    Task<bool> DeleteFileUploadTokenAsync(string uploadId);
}
