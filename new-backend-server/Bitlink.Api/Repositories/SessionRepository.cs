using Bitlink.Api.Data;
using Bitlink.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Bitlink.Api.Repositories;

/// <summary>
/// PostgreSQL repository for user session management using Entity Framework Core.
/// Replaces the MongoDB SessionRepository.
/// </summary>
public class SessionRepository : ISessionRepository
{
    private readonly BitlinkDbContext _dbContext;

    public SessionRepository(BitlinkDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Inserts a new session record into the PostgreSQL database.
    /// </summary>
    public async Task<bool> CreateSessionAsync(SessionEntity session)
    {
        await _dbContext.Sessions.AddAsync(session);
        var affected = await _dbContext.SaveChangesAsync();
        return affected > 0;
    }

    /// <summary>
    /// Finds an active (unrevoked) session by its session identifier.
    /// </summary>
    public async Task<SessionEntity?> FindActiveSessionByIdAsync(string sessionId)
    {
        return await _dbContext.Sessions
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.SessionId == sessionId && !s.IsRevoked);
    }

    /// <summary>
    /// Revokes an existing session by marking IsRevoked = true.
    /// </summary>
    public async Task<bool> RevokeSessionByIdAsync(string sessionId)
    {
        var session = await _dbContext.Sessions.FirstOrDefaultAsync(s => s.SessionId == sessionId);
        if (session is null)
        {
            return false;
        }

        session.IsRevoked = true;
        var affected = await _dbContext.SaveChangesAsync();
        return affected > 0;
    }

    /// <summary>
    /// Updates tokens and expiry dates for an active session.
    /// </summary>
    public async Task<bool> UpdateSessionByIdAsync(
        string sessionId,
        string accessToken,
        string refreshToken,
        string tokenType,
        DateTime accessTokenExpiry,
        DateTime refreshTokenExpiry)
    {
        var session = await _dbContext.Sessions.FirstOrDefaultAsync(s => s.SessionId == sessionId);
        if (session is null)
        {
            return false;
        }

        session.AccessToken = accessToken;
        if (!string.IsNullOrEmpty(refreshToken))
        {
            session.RefreshToken = refreshToken;
        }
        session.TokenType = tokenType;
        session.AccessTokenExpiry = accessTokenExpiry;
        session.RefreshTokenExpiry = refreshTokenExpiry;

        var affected = await _dbContext.SaveChangesAsync();
        return affected > 0;
    }

    /// <summary>
    /// Begins an explicit database transaction for atomic multi-step operations.
    /// </summary>
    public async Task<IDbContextTransaction> BeginTransactionAsync()
    {
        return await _dbContext.Database.BeginTransactionAsync();
    }

    /// <summary>
    /// Acquires a transaction-scoped advisory lock in PostgreSQL.
    /// The lock is automatically released upon transaction commit or rollback.
    /// </summary>
    public async Task AcquireAdvisoryXactLockAsync(string resourceKey)
    {
        await _dbContext.AcquireAdvisoryXactLockAsync(resourceKey);
    }
}
