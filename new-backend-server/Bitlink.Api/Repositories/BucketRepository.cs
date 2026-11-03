using System.Net;
using Amazon.S3;
using Amazon.S3.Model;
using Bitlink.Api.Configuration;
using Microsoft.Extensions.Options;

namespace Bitlink.Api.Repositories;

/// <summary>
/// Data access repository wrapping AWSSDK.S3 client for MinIO / S3 object storage operations.
/// Generates presigned upload and download URLs, checks file existence, and deletes objects.
/// </summary>
public class BucketRepository : IBucketRepository
{
    private readonly IAmazonS3 _s3Client;
    private readonly S3Options _s3Options;
    private readonly string _bucketName;

    public BucketRepository(IAmazonS3 s3Client, IOptions<S3Options> s3Options)
    {
        _s3Client = s3Client;
        _s3Options = s3Options.Value;
        _bucketName = _s3Options.BucketName;
    }

    private Protocol DetermineProtocol() =>
        _s3Options.Endpoint.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
            ? Protocol.HTTPS
            : Protocol.HTTP;

    /// <summary>
    /// Generates a presigned PUT URL for uploading an object directly to S3/MinIO.
    /// </summary>
    public Task<string> GetUploadSignedUrlAsync(string key, string contentType, long contentLength, int expiresIn = 3600)
    {
        var request = new GetPreSignedUrlRequest
        {
            BucketName = _bucketName,
            Key = key,
            Verb = HttpVerb.PUT,
            Expires = DateTime.UtcNow.AddSeconds(expiresIn),
            ContentType = contentType,
            Protocol = DetermineProtocol()
        };

        var url = _s3Client.GetPreSignedURL(request);
        return Task.FromResult(url);
    }

    /// <summary>
    /// Generates a presigned GET URL for downloading an object directly from S3/MinIO.
    /// </summary>
    public Task<string> GetDownloadSignedUrlAsync(string key, int expiresIn = 3600)
    {
        var request = new GetPreSignedUrlRequest
        {
            BucketName = _bucketName,
            Key = key,
            Verb = HttpVerb.GET,
            Expires = DateTime.UtcNow.AddSeconds(expiresIn),
            Protocol = DetermineProtocol()
        };

        var url = _s3Client.GetPreSignedURL(request);
        return Task.FromResult(url);
    }

    /// <summary>
    /// Checks if a file exists in the S3/MinIO bucket via GetObjectMetadataAsync (HEAD object).
    /// </summary>
    public async Task<bool> FileExistsAsync(string key)
    {
        try
        {
            await _s3Client.GetObjectMetadataAsync(_bucketName, key);
            return true;
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound || ex.ErrorCode == "NotFound")
        {
            return false;
        }
    }

    /// <summary>
    /// Permanently deletes an object from the S3/MinIO bucket.
    /// </summary>
    public async Task<bool> DeleteFileAsync(string key)
    {
        await _s3Client.DeleteObjectAsync(_bucketName, key);
        return true;
    }
}
