namespace Bitlink.Api.Repositories;

/// <summary>
/// Data access contract for S3/MinIO bucket operations.
/// </summary>
public interface IBucketRepository
{
    Task<string> GetUploadSignedUrlAsync(string key, string contentType, long contentLength, int expiresIn = 3600);
    Task<string> GetDownloadSignedUrlAsync(string key, int expiresIn = 3600);
    Task<bool> DeleteFileAsync(string key);
    Task<bool> FileExistsAsync(string key);
}
