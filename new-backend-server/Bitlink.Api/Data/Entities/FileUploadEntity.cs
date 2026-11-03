using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Bitlink.Api.Data.Entities;

/// <summary>
/// Represents a pending or completed file upload token.
/// Replaces both the MongoDB "fileUploads" collection and the Redis upload token cache.
/// </summary>
[Table("file_uploads")]
public class FileUploadEntity
{
    [Key]
    [Column("upload_id")]
    [MaxLength(100)]
    public string UploadId { get; set; } = string.Empty;

    [Required]
    [Column("user_id")]
    [MaxLength(255)]
    public string UserId { get; set; } = string.Empty;

    [Required]
    [Column("file_name")]
    [MaxLength(255)]
    public string FileName { get; set; } = string.Empty;

    [Required]
    [Column("mime_type")]
    [MaxLength(255)]
    public string MimeType { get; set; } = string.Empty;

    [Column("size_bytes")]
    public long SizeBytes { get; set; }

    [Required]
    [Column("file_id")]
    [MaxLength(100)]
    public string FileId { get; set; } = string.Empty;

    [Column("parent_id")]
    [MaxLength(100)]
    public string? ParentId { get; set; }

    [Required]
    [Column("status")]
    [MaxLength(20)]
    public string Status { get; set; } = "pending";

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("expires_at")]
    public DateTime ExpiresAt { get; set; }
}
