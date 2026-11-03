using Bitlink.Api.Data.Entities;
using Bitlink.Api.Repositories;
using Bitlink.Api.Services;
using Bitlink.Api.Utils;
using Moq;
using Xunit;

namespace Bitlink.Tests;

public class SessionServiceTests
{
    private readonly Mock<ISessionRepository> _sessionRepoMock;
    private readonly Mock<IAuthService> _authServiceMock;
    private readonly SessionService _service;

    public SessionServiceTests()
    {
        _sessionRepoMock = new Mock<ISessionRepository>();
        _authServiceMock = new Mock<IAuthService>();
        _service = new SessionService(_sessionRepoMock.Object, _authServiceMock.Object);
    }

    [Fact]
    public async Task ValidateSessionToken_WithValidActiveSession_ReturnsSession()
    {
        const string sessionId = "session_1234567890abcdef1234567890abcdef";
        var activeSession = new SessionEntity
        {
            SessionId = sessionId,
            UserId = "user_1",
            IsRevoked = false,
            AccessToken = "at_valid",
            RefreshToken = "rt_valid",
            AccessTokenExpiry = DateTime.UtcNow.AddMinutes(15),
            RefreshTokenExpiry = DateTime.UtcNow.AddDays(30)
        };

        _sessionRepoMock
            .Setup(r => r.FindActiveSessionByIdAsync(sessionId))
            .ReturnsAsync(activeSession);

        var result = await _service.ValidateSessionTokenAsync(sessionId);

        Assert.NotNull(result);
        Assert.Equal(sessionId, result.SessionId);
        _authServiceMock.Verify(a => a.RefreshTokensAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ValidateSessionToken_WhenRefreshTokenExpired_RevokesSessionAndThrows401()
    {
        const string sessionId = "session_expired_1234567890abcdef";
        var expiredSession = new SessionEntity
        {
            SessionId = sessionId,
            UserId = "user_1",
            IsRevoked = false,
            AccessToken = "at_expired",
            RefreshToken = "rt_expired",
            AccessTokenExpiry = DateTime.UtcNow.AddMinutes(-30),
            RefreshTokenExpiry = DateTime.UtcNow.AddMinutes(-10) // expired
        };

        _sessionRepoMock
            .Setup(r => r.FindActiveSessionByIdAsync(sessionId))
            .ReturnsAsync(expiredSession);

        var ex = await Assert.ThrowsAsync<BitlinkException>(() => _service.ValidateSessionTokenAsync(sessionId));
        Assert.Equal(401, ex.StatusCode);
        Assert.Equal("Session expired", ex.Message);

        _sessionRepoMock.Verify(r => r.RevokeSessionByIdAsync(sessionId), Times.Once);
    }

    [Fact]
    public async Task ValidateSessionToken_WhenAccessTokenNearExpiry_RotatesTokenViaAuthService()
    {
        const string sessionId = "session_rotate_1234567890abcdef";
        var nearExpirySession = new SessionEntity
        {
            SessionId = sessionId,
            UserId = "user_1",
            IsRevoked = false,
            AccessToken = "at_expiring_soon",
            RefreshToken = "rt_valid",
            AccessTokenExpiry = DateTime.UtcNow.AddSeconds(5), // within 10-second buffer
            RefreshTokenExpiry = DateTime.UtcNow.AddDays(30)
        };

        var refreshedSession = new SessionEntity
        {
            SessionId = sessionId,
            UserId = "user_1",
            IsRevoked = false,
            AccessToken = "at_refreshed_new",
            RefreshToken = "rt_refreshed_new",
            AccessTokenExpiry = DateTime.UtcNow.AddMinutes(15),
            RefreshTokenExpiry = DateTime.UtcNow.AddDays(30)
        };

        _sessionRepoMock
            .Setup(r => r.FindActiveSessionByIdAsync(sessionId))
            .ReturnsAsync(nearExpirySession);

        _authServiceMock
            .Setup(a => a.RefreshTokensAsync(sessionId, "rt_valid"))
            .ReturnsAsync(refreshedSession);

        var result = await _service.ValidateSessionTokenAsync(sessionId);

        Assert.NotNull(result);
        Assert.Equal("at_refreshed_new", result.AccessToken);
        _authServiceMock.Verify(a => a.RefreshTokensAsync(sessionId, "rt_valid"), Times.Once);
    }
}
