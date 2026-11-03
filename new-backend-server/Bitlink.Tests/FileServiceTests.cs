using Bitlink.Api.Data.Entities;
using Bitlink.Api.Models;
using Bitlink.Api.Repositories;
using Bitlink.Api.Services;
using Bitlink.Api.Utils;
using Moq;
using Xunit;

namespace Bitlink.Tests;

public class FileServiceTests
{
    private readonly Mock<IFileRepository> _fileRepoMock;
    private readonly Mock<IBucketRepository> _bucketRepoMock;
    private readonly FileService _service;

    public FileServiceTests()
    {
        _fileRepoMock = new Mock<IFileRepository>();
        _bucketRepoMock = new Mock<IBucketRepository>();
        _service = new FileService(_fileRepoMock.Object, _bucketRepoMock.Object);
    }

    [Fact]
    public async Task ViewFiles_ReturnsListOfFilesForUser()
    {
        var input = new ViewFilesInput { ParentId = null, Limit = 10, Offset = 0 };
        var sampleFiles = new List<FileEntity>
        {
            new() { FileId = "file_1", FileName = "doc1.pdf", UserId = "user_1", SizeBytes = 1024, MimeType = "application/pdf" },
            new() { FileId = "file_2", FileName = "image.png", UserId = "user_1", SizeBytes = 2048, MimeType = "image/png" }
        };

        _fileRepoMock
            .Setup(r => r.ViewFilesAsync("user_1", null, 10, 0))
            .ReturnsAsync(sampleFiles);

        var result = await _service.ViewFilesAsync(input, "user_1");

        Assert.Equal(2, result.Count);
        Assert.Equal("doc1.pdf", result[0].FileName);
    }

    [Fact]
    public async Task RequestingToDownload_WhenFileBelongsToUserAndExists_ReturnsSignedUrl()
    {
        const string fileId = "file_1234567890";
        const string userId = "user_1";

        var file = new FileEntity
        {
            FileId = fileId,
            FileName = "test.txt",
            UserId = userId,
            MimeType = "text/plain",
            SizeBytes = 100
        };

        _fileRepoMock.Setup(r => r.FetchFileDataAsync(fileId)).ReturnsAsync(file);
        _bucketRepoMock.Setup(b => b.FileExistsAsync(fileId)).ReturnsAsync(true);
        _bucketRepoMock.Setup(b => b.GetDownloadSignedUrlAsync(fileId, 60)).ReturnsAsync("http://minio/signed-download-url");

        var result = await _service.RequestingToDownloadAsync(fileId, userId);

        Assert.NotNull(result);
        Assert.Equal("http://minio/signed-download-url", result.SignedUrl);
        Assert.True(result.ExpiresAt > DateTime.UtcNow);
    }

    [Fact]
    public async Task RequestingToDownload_WhenFileBelongsToOtherUser_Throws403()
    {
        const string fileId = "file_1234567890";

        var file = new FileEntity
        {
            FileId = fileId,
            FileName = "secret.txt",
            UserId = "other_user",
            MimeType = "text/plain",
            SizeBytes = 100
        };

        _fileRepoMock.Setup(r => r.FetchFileDataAsync(fileId)).ReturnsAsync(file);

        var ex = await Assert.ThrowsAsync<BitlinkException>(() => _service.RequestingToDownloadAsync(fileId, "user_1"));
        Assert.Equal(403, ex.StatusCode);
        Assert.Equal("File does not belong to user", ex.Message);
    }

    [Fact]
    public async Task RenameFile_WhenFileBelongsToUserAndExists_UpdatesFileName()
    {
        const string fileId = "file_rename_123";
        const string userId = "user_1";

        var file = new FileEntity
        {
            FileId = fileId,
            FileName = "old_name.txt",
            UserId = userId,
            MimeType = "text/plain"
        };

        _fileRepoMock.Setup(r => r.FetchFileDataAsync(fileId)).ReturnsAsync(file);
        _bucketRepoMock.Setup(b => b.FileExistsAsync(fileId)).ReturnsAsync(true);
        _fileRepoMock
            .Setup(r => r.RenameFileAsync(fileId, "new_name.txt", It.IsAny<DateTime>()))
            .ReturnsAsync(true);

        await _service.RenameFileAsync(new RenameFileInput { FileId = fileId, NewFileName = "new_name.txt" }, userId);

        _fileRepoMock.Verify(r => r.RenameFileAsync(fileId, "new_name.txt", It.IsAny<DateTime>()), Times.Once);
    }

    [Fact]
    public async Task DeleteFile_WhenFileBelongsToUserAndExists_DeletesFromDbAndBucket()
    {
        const string fileId = "file_delete_123";
        const string userId = "user_1";

        var file = new FileEntity
        {
            FileId = fileId,
            FileName = "trash.txt",
            UserId = userId,
            MimeType = "text/plain"
        };

        _fileRepoMock.Setup(r => r.FetchFileDataAsync(fileId)).ReturnsAsync(file);
        _bucketRepoMock.Setup(b => b.FileExistsAsync(fileId)).ReturnsAsync(true);
        _fileRepoMock.Setup(r => r.DeleteFileAsync(fileId)).ReturnsAsync(true);
        _bucketRepoMock.Setup(b => b.DeleteFileAsync(fileId)).ReturnsAsync(true);

        await _service.DeleteFileAsync(new DeleteFileInput { FileId = fileId }, userId);

        _fileRepoMock.Verify(r => r.DeleteFileAsync(fileId), Times.Once);
        _bucketRepoMock.Verify(b => b.DeleteFileAsync(fileId), Times.Once);
    }
}
