using Amazon.S3;
using Amazon.S3.Model;
using Bitlink.Api.Configuration;
using Bitlink.Api.Repositories;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Bitlink.Tests;

public class BucketRepositoryTests
{
    [Fact]
    public async Task GetUploadSignedUrl_WithHttpEndpoint_UsesHttpProtocol()
    {
        var s3Mock = new Mock<IAmazonS3>();
        GetPreSignedUrlRequest? capturedRequest = null;

        s3Mock.Setup(s => s.GetPreSignedURL(It.IsAny<GetPreSignedUrlRequest>()))
            .Callback<GetPreSignedUrlRequest>(r => capturedRequest = r)
            .Returns("http://localhost:9000/bitlink-bucket/file_123?sig=abc");

        var options = Options.Create(new S3Options
        {
            Endpoint = "http://localhost:9000",
            BucketName = "bitlink-bucket"
        });

        var repo = new BucketRepository(s3Mock.Object, options);

        var url = await repo.GetUploadSignedUrlAsync("file_123", "application/pdf", 1024, 300);

        Assert.NotNull(capturedRequest);
        Assert.Equal(Protocol.HTTP, capturedRequest.Protocol);
        Assert.Equal(HttpVerb.PUT, capturedRequest.Verb);
        Assert.Equal("application/pdf", capturedRequest.ContentType);
        Assert.StartsWith("http://", url);
    }

    [Fact]
    public async Task GetUploadSignedUrl_WithHttpsEndpoint_UsesHttpsProtocol()
    {
        var s3Mock = new Mock<IAmazonS3>();
        GetPreSignedUrlRequest? capturedRequest = null;

        s3Mock.Setup(s => s.GetPreSignedURL(It.IsAny<GetPreSignedUrlRequest>()))
            .Callback<GetPreSignedUrlRequest>(r => capturedRequest = r)
            .Returns("https://s3.amazonaws.com/bitlink-bucket/file_123?sig=abc");

        var options = Options.Create(new S3Options
        {
            Endpoint = "https://s3.amazonaws.com",
            BucketName = "bitlink-bucket"
        });

        var repo = new BucketRepository(s3Mock.Object, options);

        var url = await repo.GetUploadSignedUrlAsync("file_123", "application/pdf", 1024, 300);

        Assert.NotNull(capturedRequest);
        Assert.Equal(Protocol.HTTPS, capturedRequest.Protocol);
        Assert.StartsWith("https://", url);
    }

    [Fact]
    public async Task GetDownloadSignedUrl_WithHttpEndpoint_UsesHttpProtocol()
    {
        var s3Mock = new Mock<IAmazonS3>();
        GetPreSignedUrlRequest? capturedRequest = null;

        s3Mock.Setup(s => s.GetPreSignedURL(It.IsAny<GetPreSignedUrlRequest>()))
            .Callback<GetPreSignedUrlRequest>(r => capturedRequest = r)
            .Returns("http://localhost:9000/bitlink-bucket/file_123?sig=abc");

        var options = Options.Create(new S3Options
        {
            Endpoint = "http://localhost:9000",
            BucketName = "bitlink-bucket"
        });

        var repo = new BucketRepository(s3Mock.Object, options);

        var url = await repo.GetDownloadSignedUrlAsync("file_123", 60);

        Assert.NotNull(capturedRequest);
        Assert.Equal(Protocol.HTTP, capturedRequest.Protocol);
        Assert.Equal(HttpVerb.GET, capturedRequest.Verb);
        Assert.StartsWith("http://", url);
    }
}
