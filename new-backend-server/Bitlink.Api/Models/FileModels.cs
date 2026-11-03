using System.Text.Json.Serialization;

namespace Bitlink.Api.Models;

public class RequestUploadInput
{
    [JsonPropertyName("fileName")]
    public string FileName { get; set; } = string.Empty;

    [JsonPropertyName("mimeType")]
    public string MimeType { get; set; } = string.Empty;

    [JsonPropertyName("sizeBytes")]
    public long SizeBytes { get; set; }

    [JsonPropertyName("parentId")]
    public string? ParentId { get; set; }
}

public class UploadAcknowledgementInput
{
    [JsonPropertyName("uploadId")]
    public string UploadId { get; set; } = string.Empty;
}

public class ViewFilesInput
{
    [JsonPropertyName("parentId")]
    public string? ParentId { get; set; }

    [JsonPropertyName("offset")]
    public int Offset { get; set; } = 0;

    [JsonPropertyName("limit")]
    public int Limit { get; set; } = 10;
}

public class RequestDownloadInput
{
    [JsonPropertyName("fileId")]
    public string FileId { get; set; } = string.Empty;
}

public class RenameFileInput
{
    [JsonPropertyName("fileId")]
    public string FileId { get; set; } = string.Empty;

    [JsonPropertyName("newFileName")]
    public string NewFileName { get; set; } = string.Empty;
}

public class DeleteFileInput
{
    [JsonPropertyName("fileId")]
    public string FileId { get; set; } = string.Empty;
}

public class UploadResponseData
{
    [JsonPropertyName("uploadId")]
    public string UploadId { get; set; } = string.Empty;

    [JsonPropertyName("signedUrl")]
    public string SignedUrl { get; set; } = string.Empty;

    [JsonPropertyName("expiresAt")]
    public DateTime ExpiresAt { get; set; }
}

public class DownloadResponseData
{
    [JsonPropertyName("signedUrl")]
    public string SignedUrl { get; set; } = string.Empty;

    [JsonPropertyName("expiresAt")]
    public DateTime ExpiresAt { get; set; }
}

public class ServerFile
{
    [JsonPropertyName("fileId")]
    public string FileId { get; set; } = string.Empty;

    [JsonPropertyName("fileName")]
    public string FileName { get; set; } = string.Empty;

    [JsonPropertyName("userId")]
    public string UserId { get; set; } = string.Empty;

    [JsonPropertyName("mimeType")]
    public string MimeType { get; set; } = string.Empty;

    [JsonPropertyName("sizeBytes")]
    public long SizeBytes { get; set; }

    [JsonPropertyName("parentId")]
    public string? ParentId { get; set; }

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("modifiedAt")]
    public DateTime ModifiedAt { get; set; }
}
