using Bitlink.Api.Data.Entities;
using Bitlink.Api.Models;
using Bitlink.Api.Repositories;
using Bitlink.Api.Utils;

namespace Bitlink.Api.Services;

/// <summary>
/// Business logic service coordinating file upload authorizations, presigned S3 URLs,
/// cancellation rollbacks, and finalized file creation.
/// </summary>
public class UploadService : IUploadService
{
    private readonly IBucketRepository _bucketRepository;
    private readonly IUploadRepository _uploadRepository;
    private readonly IFileRepository _fileRepository;

    public UploadService(
        IBucketRepository bucketRepository,
        IUploadRepository uploadRepository,
        IFileRepository fileRepository)
    {
        _bucketRepository = bucketRepository;
        _uploadRepository = uploadRepository;
        _fileRepository = fileRepository;
    }

    /// <summary>
    /// Requests upload authorization, generating a unique file key, upload token, and S3 PUT signed URL.
    ///
    /// Inorder Flow:
    /// - Create uploadId, fileId, expirySeconds and get Signed Upload Url with 5 min expiry
    /// - Create Upload entity
    /// - Store in database
    /// - Return the upload information
    /// </summary>
    public async Task<UploadResponseData> RequestingToUploadAsync(RequestUploadInput data, string userId)
    {
        var uploadId = CryptoService.GenerateUploadId();
        var fileId = CryptoService.GenerateFileId();
        const int expirySeconds = 5 * 60;

        var signedUrl = await _bucketRepository.GetUploadSignedUrlAsync(fileId, data.MimeType, data.SizeBytes, expirySeconds);
        ValidationHelpers.StrictCheck(!string.IsNullOrEmpty(signedUrl), 500, "Failed to get upload signed url");

        var now = DateTime.UtcNow;
        var uploadObject = new FileUploadEntity
        {
            UploadId = uploadId,
            UserId = userId,
            FileName = data.FileName,
            MimeType = data.MimeType,
            SizeBytes = data.SizeBytes,
            FileId = fileId,
            ParentId = data.ParentId,
            Status = "pending",
            CreatedAt = now,
            ExpiresAt = now.AddSeconds(expirySeconds)
        };

        var created = await _uploadRepository.CreateFileUploadTokenAsync(uploadObject);
        ValidationHelpers.StrictCheck(created, 500, "Failed to create file upload record");

        return new UploadResponseData
        {
            SignedUrl = signedUrl,
            UploadId = uploadId,
            ExpiresAt = uploadObject.ExpiresAt
        };
    }

    /// <summary>
    /// Cancels a failed upload and deletes any orphaned binary objects from S3.
    ///
    /// Inorder Flow:
    /// - Retrieve and delete file upload token from database
    /// - If file exists in bucket, delete it
    /// </summary>
    public async Task UploadFailedAsync(string uploadId)
    {
        var uploadObject = await _uploadRepository.RetrieveAndDeleteFileUploadTokenAsync(uploadId);
        if (uploadObject is null)
        {
            return;
        }

        if (await _bucketRepository.FileExistsAsync(uploadObject.FileId))
        {
            var deleted = await _bucketRepository.DeleteFileAsync(uploadObject.FileId);
            ValidationHelpers.StrictCheck(deleted, 500, "Failed to delete file from bucket");
        }
    }

    /// <summary>
    /// Finalizes an upload, verifies the binary exists in S3, and records file metadata in PostgreSQL.
    ///
    /// Inorder Flow:
    /// - Retrieve and delete file upload token from database
    /// - Check file exists in bucket
    /// - Create file entity and store in database
    /// - Return file entity
    /// </summary>
    public async Task<FileEntity> UploadSuccessAsync(string uploadId)
    {
        var uploadObject = await _uploadRepository.RetrieveAndDeleteFileUploadTokenAsync(uploadId);
        ValidationHelpers.StrictCheck(uploadObject, 500, "Failed to retrieve file upload token");

        var exists = await _bucketRepository.FileExistsAsync(uploadObject.FileId);
        ValidationHelpers.StrictCheck(exists, 500, "File does not exist");

        var now = DateTime.UtcNow;
        var fileObject = new FileEntity
        {
            FileId = uploadObject.FileId,
            FileName = uploadObject.FileName,
            UserId = uploadObject.UserId,
            MimeType = uploadObject.MimeType,
            SizeBytes = uploadObject.SizeBytes,
            ParentId = uploadObject.ParentId,
            CreatedAt = now,
            ModifiedAt = now
        };

        var created = await _fileRepository.CreateNewFileAsync(fileObject);
        ValidationHelpers.StrictCheck(created, 500, "Failed to create file");

        return fileObject;
    }
}
