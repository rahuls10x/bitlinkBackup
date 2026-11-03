using Bitlink.Api.Data.Entities;
using Bitlink.Api.Models;

namespace Bitlink.Api.Services;

/// <summary>
/// Business logic contract for managing file upload lifecycles.
/// </summary>
public interface IUploadService
{
    Task<UploadResponseData> RequestingToUploadAsync(RequestUploadInput data, string userId);
    Task UploadFailedAsync(string uploadId);
    Task<FileEntity> UploadSuccessAsync(string uploadId);
}
