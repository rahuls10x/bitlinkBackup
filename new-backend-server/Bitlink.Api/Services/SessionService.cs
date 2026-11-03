using Bitlink.Api.Data.Entities;
using Bitlink.Api.Repositories;
using Bitlink.Api.Utils;

namespace Bitlink.Api.Services;

/// <summary>
/// Session validation service. Checks token structure, session active state in PostgreSQL,
/// verifies refresh token TTL, and coordinates token rotation when the access token nears expiry.
/// </summary>
public class SessionService : ISessionService
{
    private readonly ISessionRepository _sessionRepository;
    private readonly IAuthService _authService;

    public SessionService(ISessionRepository sessionRepository, IAuthService authService)
    {
        _sessionRepository = sessionRepository;
        _authService = authService;
    }

    /// <summary>
    /// Validates a session token string, handles expiration and token refresh.
    ///
    /// Inorder Flow:
    /// - Check and validate session token prefix ("session_")
    /// - Retrieve active session from PostgreSQL
    /// - Check if refresh token is expired and revoke if necessary
    /// - Check if access token is expired and rotate if necessary
    /// - Return the valid session object
    /// </summary>
    public async Task<SessionEntity> ValidateSessionTokenAsync(string? token)
    {
        ValidationHelpers.StrictCheck(!string.IsNullOrWhiteSpace(token), 401, "Unauthorized");
        ValidationHelpers.StrictCheck(token!.StartsWith("session_"), 401, "Invalid session token");

        var session = await _sessionRepository.FindActiveSessionByIdAsync(token);
        ValidationHelpers.StrictCheck(session, 401, "Session does not exist");

        // Check refresh token expiration (30 days)
        if (session.RefreshTokenExpiry < DateTime.UtcNow)
        {
            await _sessionRepository.RevokeSessionByIdAsync(session.SessionId);
            throw new BitlinkException(401, "Session expired");
        }

        // Check access token expiration (with 10-second buffer)
        if (session.AccessTokenExpiry.Subtract(TimeSpan.FromSeconds(10)) < DateTime.UtcNow)
        {
            session = await _authService.RefreshTokensAsync(session.SessionId, session.RefreshToken);
        }

        return session;
    }
}
