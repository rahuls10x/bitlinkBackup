using System.Security.Cryptography;

namespace Bitlink.Api.Services;

/// <summary>
/// Cryptographic utility generating unique entity identifiers with designated prefixes.
/// Matches the implementation in the Express CryptoService.
/// </summary>
public static class CryptoService
{
    /// <summary>
    /// Generates a random session identifier with prefix "session_" followed by 32 random hex bytes (64 hex characters).
    /// </summary>
    public static string GenerateSessionId()
    {
        return "session_" + Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();
    }

    /// <summary>
    /// Generates a random lock identifier with prefix "lock_" followed by 32 random hex bytes.
    /// </summary>
    public static string GenerateLockId()
    {
        return "lock_" + Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();
    }

    /// <summary>
    /// Generates a random upload identifier with prefix "upload_" followed by 16 random hex bytes (32 hex characters).
    /// Total length: 7 + 32 = 39 characters.
    /// </summary>
    public static string GenerateUploadId()
    {
        return "upload_" + Convert.ToHexString(RandomNumberGenerator.GetBytes(16)).ToLowerInvariant();
    }

    /// <summary>
    /// Generates a random file identifier with prefix "file_" followed by 32 random hex bytes (64 hex characters).
    /// Total length: 5 + 64 = 69 characters.
    /// </summary>
    public static string GenerateFileId()
    {
        return "file_" + Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();
    }
}
