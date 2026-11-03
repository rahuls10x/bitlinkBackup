using Bitlink.Api.Data.Entities;
using Bitlink.Api.Models;
using Bitlink.Api.Repositories;
using Bitlink.Api.Utils;

namespace Bitlink.Api.Services;

/// <summary>
/// Business logic service for querying, downloading, renaming, and deleting files.
/// </summary>
public class FileService : IFileService
{
    private readonly IFileRepository _fileRepository;
    private readonly IBucketRepository _bucketRepository;

    public FileService(IFileRepository fileRepository, IBucketRepository bucketRepository)
    {
        _fileRepository = fileRepository;
        _bucketRepository = bucketRepository;
    }

    /// <summary>
    /// Views files matching the provided parentId, limit, and offset for the user.
    ///
    /// Inorder Flow:
    /// - Find files in PostgreSQL files table by userId and parentId with limit and offset
    /// - Return files
    /// </summary>
    public async Task<List<FileEntity>> ViewFilesAsync(ViewFilesInput query, string userId)
    {
        var files = await _fileRepository.ViewFilesAsync(userId, query.ParentId, query.Limit, query.Offset);
        ValidationHelpers.StrictCheck(files, 500, "Failed to fetch files");
        return files;
    }

    /// <summary>
    /// Requests a presigned download URL for a file after checking user ownership and S3 existence.
    ///
    /// Inorder Flow:
    /// - Find file in files table by fileId and check its ownership
    /// - Check if file exists in S3 bucket
    /// - Create download signed url with 60s expiry
    /// - Return download signed url and expiry
    /// </summary>
    public async Task<DownloadResponseData> RequestingToDownloadAsync(string fileId, string userId)
    {
        var file = await _fileRepository.FetchFileDataAsync(fileId);
        ValidationHelpers.StrictCheck(file, 500, "Failed to fetch file");
        ValidationHelpers.StrictCheck(file!.UserId == userId, 403, "File does not belong to user");

        var exists = await _bucketRepository.FileExistsAsync(file.FileId);
        ValidationHelpers.StrictCheck(exists, 500, "File does not exist");

        const int expirySeconds = 60; // 1 minute
        var downloadUrl = await _bucketRepository.GetDownloadSignedUrlAsync(file.FileId, expirySeconds);
        ValidationHelpers.StrictCheck(!string.IsNullOrEmpty(downloadUrl), 500, "Failed to get download signed url");

        return new DownloadResponseData
        {
            SignedUrl = downloadUrl,
            ExpiresAt = DateTime.UtcNow.AddSeconds(expirySeconds)
        };
    }

    /// <summary>
    /// Renames a file after verifying user ownership and S3 object existence.
    ///
    /// Inorder Flow:
    /// - Find file in files table by fileId and check its ownership
    /// - Check if file exists in S3 bucket
    /// - Rename file in database
    /// </summary>
    public async Task RenameFileAsync(RenameFileInput data, string userId)
    {
        var file = await _fileRepository.FetchFileDataAsync(data.FileId);
        ValidationHelpers.StrictCheck(file, 500, "Failed to fetch file");
        ValidationHelpers.StrictCheck(file!.UserId == userId, 403, "File does not belong to user");

        var exists = await _bucketRepository.FileExistsAsync(file.FileId);
        ValidationHelpers.StrictCheck(exists, 500, "File does not exist");

        var modifiedTime = DateTime.UtcNow;
        var renamed = await _fileRepository.RenameFileAsync(file.FileId, data.NewFileName, modifiedTime);
        ValidationHelpers.StrictCheck(renamed, 500, "Failed to rename file");
    }

    /// <summary>
    /// Deletes a file record from PostgreSQL and removes the binary from the S3 bucket.
    ///
    /// Inorder Flow:
    /// - Find file in files table by fileId and check its ownership
    /// - Check if file exists in S3 bucket
    /// - Delete file from database and then bucket
    /// </summary>
    public async Task DeleteFileAsync(DeleteFileInput data, string userId)
    {
        var file = await _fileRepository.FetchFileDataAsync(data.FileId);
        ValidationHelpers.StrictCheck(file, 500, "Failed to fetch file");
        ValidationHelpers.StrictCheck(file!.UserId == userId, 403, "File does not belong to user");

        var exists = await _bucketRepository.FileExistsAsync(file.FileId);
        ValidationHelpers.StrictCheck(exists, 500, "File does not exist");

        var deletedFromDb = await _fileRepository.DeleteFileAsync(file.FileId);
        ValidationHelpers.StrictCheck(deletedFromDb, 500, "Failed to delete file");

        var deletedFromBucket = await _bucketRepository.DeleteFileAsync(file.FileId);
        ValidationHelpers.StrictCheck(deletedFromBucket, 500, "Failed to delete file from bucket");
    }
}
