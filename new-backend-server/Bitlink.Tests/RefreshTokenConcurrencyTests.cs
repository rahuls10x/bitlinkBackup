using System.Net;
using Bitlink.Api.Configuration;
using Bitlink.Api.Data.Entities;
using Bitlink.Api.Repositories;
using Bitlink.Api.Services;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using Xunit;

namespace Bitlink.Tests;

public class RefreshTokenConcurrencyTests
{
    private readonly Mock<ISessionRepository> _sessionRepoMock;
    private readonly Mock<IDbContextTransaction> _transactionMock;
    private readonly Mock<HttpMessageHandler> _httpHandlerMock;
    private readonly HttpClient _httpClient;
    private readonly IOptions<AuthServerOptions> _authOptions;
    private readonly Mock<ILogger<AuthService>> _loggerMock;
    private readonly AuthService _authService;

    public RefreshTokenConcurrencyTests()
    {
        _sessionRepoMock = new Mock<ISessionRepository>();
        _transactionMock = new Mock<IDbContextTransaction>();
        _sessionRepoMock.Setup(r => r.BeginTransactionAsync()).ReturnsAsync(_transactionMock.Object);

        _httpHandlerMock = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_httpHandlerMock.Object);

        _authOptions = Options.Create(new AuthServerOptions
        {
            AuthServerUrl = "http://mock-auth:4000",
            ResourceServerUrl = "http://mock-auth:5000",
            ClientId = "client_test",
            ClientSecret = "secret_test"
        });

        _loggerMock = new Mock<ILogger<AuthService>>();

        _authService = new AuthService(
            _httpClient,
            _sessionRepoMock.Object,
            _authOptions,
            _loggerMock.Object);
    }

    [Fact]
    public async Task RefreshTokens_AcquiresAdvisoryLock_AndReturnsImmediatelyIfAlreadyRefreshedByConcurrentRequest()
    {
        const string sessionId = "session_concurrency_test_123";
        const string refreshToken = "rt_concurrency_test_123";

        // Session was already refreshed by another thread while waiting for lock
        var freshSessionAfterLock = new SessionEntity
        {
            SessionId = sessionId,
            UserId = "user_1",
            AccessToken = "at_already_refreshed_by_another_thread",
            RefreshToken = "rt_already_refreshed",
            AccessTokenExpiry = DateTime.UtcNow.AddMinutes(15), // fresh!
            RefreshTokenExpiry = DateTime.UtcNow.AddDays(30)
        };

        _sessionRepoMock
            .Setup(r => r.FindActiveSessionByIdAsync(sessionId))
            .ReturnsAsync(freshSessionAfterLock);

        // Act
        var result = await _authService.RefreshTokensAsync(sessionId, refreshToken);

        // Assert: Advisory lock was acquired for the specific session
        _sessionRepoMock.Verify(r => r.AcquireAdvisoryXactLockAsync($"refresh:{sessionId}"), Times.Once);

        // Assert: Transaction was committed without calling external Truth refresh endpoint
        _transactionMock.Verify(t => t.CommitAsync(default), Times.Once);
        Assert.Equal("at_already_refreshed_by_another_thread", result.AccessToken);

        // Verify HTTP handler was NEVER called
        _httpHandlerMock.Protected().Verify(
            "SendAsync",
            Times.Never(),
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task RefreshTokens_AcquiresAdvisoryLock_RotatesToken_AndUpdatesDatabaseWhenNeeded()
    {
        const string sessionId = "session_concurrency_test_rotate";
        const string refreshToken = "rt_concurrency_test_rotate";

        var expiredSessionAfterLock = new SessionEntity
        {
            SessionId = sessionId,
            UserId = "user_1",
            AccessToken = "at_expired",
            RefreshToken = refreshToken,
            AccessTokenExpiry = DateTime.UtcNow.AddSeconds(-5),
            RefreshTokenExpiry = DateTime.UtcNow.AddDays(30)
        };

        var updatedSessionInDb = new SessionEntity
        {
            SessionId = sessionId,
            UserId = "user_1",
            AccessToken = "at_newly_issued_by_truth",
            RefreshToken = "rt_newly_issued",
            AccessTokenExpiry = DateTime.UtcNow.AddMinutes(15),
            RefreshTokenExpiry = DateTime.UtcNow.AddDays(30)
        };

        _sessionRepoMock
            .SetupSequence(r => r.FindActiveSessionByIdAsync(sessionId))
            .ReturnsAsync(expiredSessionAfterLock)
            .ReturnsAsync(updatedSessionInDb);

        _sessionRepoMock
            .Setup(r => r.UpdateSessionByIdAsync(
                sessionId,
                "at_newly_issued_by_truth",
                "rt_newly_issued",
                It.IsAny<string>(),
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>()))
            .ReturnsAsync(true);

        var truthResponseJson = @"{
            ""success"": true,
            ""data"": {
                ""access_token"": ""at_newly_issued_by_truth"",
                ""token_type"": ""Bearer"",
                ""user_id"": ""user_1"",
                ""expires_in"": 900000,
                ""refresh_token"": ""rt_newly_issued"",
                ""scopes"": [""name"", ""email""]
            }
        }";

        _httpHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(truthResponseJson)
            });

        // Act
        var result = await _authService.RefreshTokensAsync(sessionId, refreshToken);

        // Assert: Advisory lock acquired, database updated, transaction committed
        _sessionRepoMock.Verify(r => r.AcquireAdvisoryXactLockAsync($"refresh:{sessionId}"), Times.Once);
        _sessionRepoMock.Verify(r => r.UpdateSessionByIdAsync(
            sessionId,
            "at_newly_issued_by_truth",
            "rt_newly_issued",
            It.IsAny<string>(),
            It.IsAny<DateTime>(),
            It.IsAny<DateTime>()), Times.Once);
        _transactionMock.Verify(t => t.CommitAsync(default), Times.Once);

        Assert.Equal("at_newly_issued_by_truth", result.AccessToken);
    }

    [Fact]
    public async Task RefreshTokens_WhenExceptionOccurs_RollsBackTransaction()
    {
        const string sessionId = "session_fail_123";
        const string refreshToken = "rt_fail_123";

        var expiredSessionAfterLock = new SessionEntity
        {
            SessionId = sessionId,
            UserId = "user_1",
            AccessToken = "at_expired",
            RefreshToken = refreshToken,
            AccessTokenExpiry = DateTime.UtcNow.AddSeconds(-5),
            RefreshTokenExpiry = DateTime.UtcNow.AddDays(30)
        };

        _sessionRepoMock
            .Setup(r => r.FindActiveSessionByIdAsync(sessionId))
            .ReturnsAsync(expiredSessionAfterLock);

        _httpHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Network failure connecting to Truth Auth Server"));

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(() => _authService.RefreshTokensAsync(sessionId, refreshToken));

        // Verify transaction rolled back
        _transactionMock.Verify(t => t.RollbackAsync(default), Times.Once);
    }
}
