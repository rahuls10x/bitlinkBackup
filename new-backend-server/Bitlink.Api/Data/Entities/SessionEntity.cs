using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Bitlink.Api.Data.Entities;

/// <summary>
/// Represents an authenticated user session persisted in PostgreSQL.
/// Replaces the MongoDB "sessions" collection.
/// </summary>
[Table("sessions")]
public class SessionEntity
{
    [Key]
    [Column("session_id")]
    [MaxLength(100)]
    public string SessionId { get; set; } = string.Empty;

    [Required]
    [Column("user_id")]
    [MaxLength(255)]
    public string UserId { get; set; } = string.Empty;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("expires_at")]
    public DateTime ExpiresAt { get; set; }

    [Column("is_revoked")]
    public bool IsRevoked { get; set; } = false;

    [Required]
    [Column("access_token")]
    public string AccessToken { get; set; } = string.Empty;

    [Required]
    [Column("refresh_token")]
    public string RefreshToken { get; set; } = string.Empty;

    [Required]
    [Column("token_type")]
    [MaxLength(50)]
    public string TokenType { get; set; } = "Bearer";

    [Column("access_token_expiry")]
    public DateTime AccessTokenExpiry { get; set; }

    [Column("refresh_token_expiry")]
    public DateTime RefreshTokenExpiry { get; set; }
}
