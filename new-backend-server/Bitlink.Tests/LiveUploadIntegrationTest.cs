using System.Net.Http.Headers;
using System.Text;
using Amazon.Runtime;
using Amazon.S3;
using Bitlink.Api.Configuration;
using Bitlink.Api.Repositories;
using Microsoft.Extensions.Options;
using Xunit;

namespace Bitlink.Tests;

public class LiveUploadIntegrationTest
{
    [Fact]
    public async Task PresignedUploadUrl_CanSuccessfullyUploadToLiveMinio()
    {
        var s3Config = new AmazonS3Config
        {
            ServiceURL = "http://localhost:9000",
            ForcePathStyle = true,
            UseHttp = true
        };

        var credentials = new BasicAWSCredentials("root", "rootpass");
        using var s3Client = new AmazonS3Client(credentials, s3Config);

        var options = Options.Create(new S3Options
        {
            Endpoint = "http://localhost:9000",
            BucketName = "bitlink-bucket",
            AccessKey = "root",
            SecretKey = "rootpass"
        });

        var repo = new BucketRepository(s3Client, options);

        var testFileId = "file_test_upload_" + Guid.NewGuid().ToString("N");
        const string contentType = "text/plain";
        var contentBytes = Encoding.UTF8.GetBytes("Hello Bitlink S3 Upload!");

        // 1. Generate presigned upload URL
        var signedUrl = await repo.GetUploadSignedUrlAsync(testFileId, contentType, contentBytes.Length, 300);

        // Assert: Protocol is http://, not https://
        Assert.StartsWith("http://localhost:9000/", signedUrl);

        // 2. Perform the PUT upload using HttpClient (just like browser XHR)
        using var httpClient = new HttpClient();
        using var content = new ByteArrayContent(contentBytes);
        content.Headers.ContentType = new MediaTypeHeaderValue(contentType);

        var uploadResponse = await httpClient.PutAsync(signedUrl, content);

        // Assert: MinIO accepted the upload with HTTP 200 OK!
        Assert.True(uploadResponse.IsSuccessStatusCode, $"Upload failed with status {uploadResponse.StatusCode}: {await uploadResponse.Content.ReadAsStringAsync()}");

        // 3. Verify file exists in bucket
        var exists = await repo.FileExistsAsync(testFileId);
        Assert.True(exists, "Uploaded file should exist in MinIO");

        // 4. Generate download URL and verify download works
        var downloadUrl = await repo.GetDownloadSignedUrlAsync(testFileId, 60);
        Assert.StartsWith("http://localhost:9000/", downloadUrl);

        var downloadResponse = await httpClient.GetAsync(downloadUrl);
        Assert.True(downloadResponse.IsSuccessStatusCode);
        var downloadedText = await downloadResponse.Content.ReadAsStringAsync();
        Assert.Equal("Hello Bitlink S3 Upload!", downloadedText);

        // 5. Cleanup test file
        await repo.DeleteFileAsync(testFileId);
        var existsAfterDelete = await repo.FileExistsAsync(testFileId);
        Assert.False(existsAfterDelete, "File should be deleted");
    }
}
