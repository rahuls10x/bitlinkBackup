using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Bitlink.Api.Data.Entities;

/// <summary>
/// Represents a user file stored in the system.
/// Metadata is stored in PostgreSQL and binary content is stored in S3/MinIO.
/// Replaces the MongoDB "files" collection.
/// </summary>
[Table("files")]
public class FileEntity
{
    [Key]
    [Column("file_id")]
    [MaxLength(100)]
    public string FileId { get; set; } = string.Empty;

    [Required]
    [Column("file_name")]
    [MaxLength(255)]
    public string FileName { get; set; } = string.Empty;

    [Required]
    [Column("user_id")]
    [MaxLength(255)]
    public string UserId { get; set; } = string.Empty;

    [Required]
    [Column("mime_type")]
    [MaxLength(255)]
    public string MimeType { get; set; } = string.Empty;

    [Column("size_bytes")]
    public long SizeBytes { get; set; }

    [Column("parent_id")]
    [MaxLength(100)]
    public string? ParentId { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("modified_at")]
    public DateTime ModifiedAt { get; set; } = DateTime.UtcNow;
}
