using Bitlink.Api.Data.Entities;
using Bitlink.Api.Models;
using Bitlink.Api.Services;
using Bitlink.Api.Utils;
using Microsoft.AspNetCore.SignalR;

namespace Bitlink.Api.Hubs;

/// <summary>
/// Real-time SignalR hub providing file management events and multi-client synchronization.
/// Replaces Socket.IO SocketRoutes and JoinSocketRooms.
/// </summary>
public class FileHub : Hub
{
    private readonly ISessionService _sessionService;
    private readonly IFileService _fileService;
    private readonly IUploadService _uploadService;
    private readonly ILogger<FileHub> _logger;

    public FileHub(
        ISessionService sessionService,
        IFileService fileService,
        IUploadService uploadService,
        ILogger<FileHub> logger)
    {
        _sessionService = sessionService;
        _fileService = fileService;
        _uploadService = uploadService;
        _logger = logger;
    }

    private string GetUserId()
    {
        if (Context.Items.TryGetValue("UserId", out var uid) && uid is string userId)
        {
            return userId;
        }
        throw new BitlinkException(401, "User is not authenticated on this connection");
    }

    private string GetRoomId() => $"room_{GetUserId()}";

    public override async Task OnConnectedAsync()
    {
        var httpContext = Context.GetHttpContext();
        var token = httpContext?.Request.Cookies["sessionToken"];

        if (string.IsNullOrWhiteSpace(token))
        {
            _logger.LogWarning("[SignalR] Connection rejected: No sessionToken cookie provided");
            Context.Abort();
            return;
        }

        try
        {
            var session = await _sessionService.ValidateSessionTokenAsync(token);
            Context.Items["SessionId"] = session.SessionId;
            Context.Items["UserId"] = session.UserId;
            Context.Items["AccessToken"] = session.AccessToken;

            var roomId = $"room_{session.UserId}";
            await Groups.AddToGroupAsync(Context.ConnectionId, roomId);

            _logger.LogInformation("[SignalR] Client connected: {ConnectionId} (Session: {SessionId}) (User: {UserId})",
                Context.ConnectionId, session.SessionId, session.UserId);

            await base.OnConnectedAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning("[SignalR] Connection rejected: {Message}", ex.Message);
            Context.Abort();
        }
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("[SignalR] Client disconnected: {ConnectionId}", Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }

    public async Task<StandardResponse<List<ServerFile>>> ViewFiles(ViewFilesInput data)
    {
        try
        {
            var userId = GetUserId();
            var files = await _fileService.ViewFilesAsync(data, userId);

            var result = files.Select(MapToFileModel).ToList();
            return StandardResponse<List<ServerFile>>.Ok("Files has been fetched successfully", result);
        }
        catch (Exception ex)
        {
            return StandardResponse<List<ServerFile>>.Fail(ex.Message);
        }
    }

    public async Task<StandardResponse<UploadResponseData>> RequestingToUpload(RequestUploadInput data)
    {
        try
        {
            ValidationHelpers.StrictCheck(!string.IsNullOrWhiteSpace(data.FileName), 400, "File name is required");
            ValidationHelpers.StrictCheck(!string.IsNullOrWhiteSpace(data.MimeType), 400, "MIME type is required");
            ValidationHelpers.StrictCheck(data.SizeBytes > 0, 400, "File size must be greater than zero");

            var userId = GetUserId();
            var uploadInfo = await _uploadService.RequestingToUploadAsync(data, userId);

            return StandardResponse<UploadResponseData>.Ok("Permission to Upload is granted", uploadInfo);
        }
        catch (Exception ex)
        {
            return StandardResponse<UploadResponseData>.Fail(ex.Message);
        }
    }

    public async Task<StandardResponse<ServerFile>> UploadSuccess(UploadAcknowledgementInput data)
    {
        try
        {
            ValidationHelpers.StrictCheck(!string.IsNullOrWhiteSpace(data.UploadId), 400, "Upload ID is required");

            var file = await _uploadService.UploadSuccessAsync(data.UploadId);
            var serverFile = MapToFileModel(file);

            // Broadcast to other sessions of this user
            await Clients.OthersInGroup(GetRoomId()).SendAsync("fileUpdated", new
            {
                operation = "fileAdded",
                data = serverFile
            });

            return StandardResponse<ServerFile>.Ok("File has been uploaded successfully", serverFile);
        }
        catch (Exception ex)
        {
            return StandardResponse<ServerFile>.Fail(ex.Message);
        }
    }

    public async Task<StandardResponse> UploadFailed(UploadAcknowledgementInput data)
    {
        try
        {
            ValidationHelpers.StrictCheck(!string.IsNullOrWhiteSpace(data.UploadId), 400, "Upload ID is required");

            await _uploadService.UploadFailedAsync(data.UploadId);
            return StandardResponse.Ok("Upload has been cancelled successfully");
        }
        catch (Exception ex)
        {
            return StandardResponse.Fail(ex.Message);
        }
    }

    public async Task<StandardResponse<DownloadResponseData>> RequestingToDownload(RequestDownloadInput data)
    {
        try
        {
            ValidationHelpers.StrictCheck(!string.IsNullOrWhiteSpace(data.FileId), 400, "File ID is required");

            var userId = GetUserId();
            var downloadInfo = await _fileService.RequestingToDownloadAsync(data.FileId, userId);

            return StandardResponse<DownloadResponseData>.Ok("Permission to Download is granted", downloadInfo);
        }
        catch (Exception ex)
        {
            return StandardResponse<DownloadResponseData>.Fail(ex.Message);
        }
    }

    public async Task<StandardResponse> RenameFile(RenameFileInput data)
    {
        try
        {
            ValidationHelpers.StrictCheck(!string.IsNullOrWhiteSpace(data.FileId), 400, "File ID is required");
            ValidationHelpers.StrictCheck(!string.IsNullOrWhiteSpace(data.NewFileName), 400, "New file name is required");

            var userId = GetUserId();
            await _fileService.RenameFileAsync(data, userId);

            // Broadcast rename to other active sessions of this user
            await Clients.OthersInGroup(GetRoomId()).SendAsync("fileUpdated", new
            {
                operation = "fileRenamed",
                data
            });

            return StandardResponse.Ok("File has been renamed successfully");
        }
        catch (Exception ex)
        {
            return StandardResponse.Fail(ex.Message);
        }
    }

    public async Task<StandardResponse> DeleteFile(DeleteFileInput data)
    {
        try
        {
            ValidationHelpers.StrictCheck(!string.IsNullOrWhiteSpace(data.FileId), 400, "File ID is required");

            var userId = GetUserId();
            await _fileService.DeleteFileAsync(data, userId);

            // Broadcast deletion to other active sessions of this user
            await Clients.OthersInGroup(GetRoomId()).SendAsync("fileUpdated", new
            {
                operation = "fileDeleted",
                data
            });

            return StandardResponse.Ok("File has been deleted successfully");
        }
        catch (Exception ex)
        {
            return StandardResponse.Fail(ex.Message);
        }
    }

    private static ServerFile MapToFileModel(FileEntity entity) => new()
    {
        FileId = entity.FileId,
        FileName = entity.FileName,
        UserId = entity.UserId,
        MimeType = entity.MimeType,
        SizeBytes = entity.SizeBytes,
        ParentId = entity.ParentId,
        CreatedAt = entity.CreatedAt,
        ModifiedAt = entity.ModifiedAt
    };
}
