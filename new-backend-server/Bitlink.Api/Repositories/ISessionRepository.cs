using Bitlink.Api.Data.Entities;
using Microsoft.EntityFrameworkCore.Storage;

namespace Bitlink.Api.Repositories;

/// <summary>
/// Interface for Session data access operations.
/// </summary>
public interface ISessionRepository
{
    Task<bool> CreateSessionAsync(SessionEntity session);
    Task<SessionEntity?> FindActiveSessionByIdAsync(string sessionId);
    Task<bool> RevokeSessionByIdAsync(string sessionId);
    Task<bool> UpdateSessionByIdAsync(string sessionId, string accessToken, string refreshToken, string tokenType, DateTime accessTokenExpiry, DateTime refreshTokenExpiry);
    Task<IDbContextTransaction> BeginTransactionAsync();
    Task AcquireAdvisoryXactLockAsync(string resourceKey);
}
