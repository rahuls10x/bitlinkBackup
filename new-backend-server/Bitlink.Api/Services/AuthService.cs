using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Bitlink.Api.Configuration;
using Bitlink.Api.Data.Entities;
using Bitlink.Api.Models;
using Bitlink.Api.Repositories;
using Bitlink.Api.Utils;
using Microsoft.Extensions.Options;

namespace Bitlink.Api.Services;

/// <summary>
/// Business logic layer executing OAuth code exchange, user resource fetching,
/// token refresh rotation with PostgreSQL advisory lock synchronization, and logout.
/// </summary>
public class AuthService : IAuthService
{
    private readonly HttpClient _httpClient;
    private readonly ISessionRepository _sessionRepository;
    private readonly AuthServerOptions _authOptions;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        HttpClient httpClient,
        ISessionRepository sessionRepository,
        IOptions<AuthServerOptions> authOptions,
        ILogger<AuthService> logger)
    {
        _httpClient = httpClient;
        _sessionRepository = sessionRepository;
        _authOptions = authOptions.Value;
        _logger = logger;
    }

    /// <summary>
    /// Fetches the authenticated user's profile from Truth Resource Server.
    /// </summary>
    public async Task<UserResource> FetchUserResourceAsync(string accessToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"{_authOptions.ResourceServerUrl}/resource");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var response = await _httpClient.SendAsync(request);
        var content = await response.Content.ReadAsStringAsync();

        var result = JsonSerializer.Deserialize<TruthApiResponse<UserResource>>(content);

        ValidationHelpers.StrictCheck(
            response.IsSuccessStatusCode || (result != null && (result.Success || result.Data != null)),
            (int)response.StatusCode,
            result?.Error ?? "Failed to fetch user resource");

        var userResource = ValidationHelpers.StrictCheck(result?.Data, 500, "User resource data is missing");
        return userResource;
    }

    /// <summary>
    /// Exchanges authorization code and PKCE code_verifier for Access and Refresh tokens.
    /// </summary>
    public async Task<UserExchangeResult> ExchangeCodeAsync(string code, string codeVerifier)
    {
        var payload = new
        {
            client_id = _authOptions.ClientId,
            client_secret = _authOptions.ClientSecret,
            code,
            code_verifier = codeVerifier
        };

        var jsonContent = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync($"{_authOptions.AuthServerUrl}/tokenize", jsonContent);
        var content = await response.Content.ReadAsStringAsync();

        var result = JsonSerializer.Deserialize<TruthApiResponse<AuthTokens>>(content);

        ValidationHelpers.StrictCheck(
            response.IsSuccessStatusCode || (result != null && (result.Success || result.Data != null)),
            (int)response.StatusCode,
            result?.Error ?? "Failed to exchange authorization code");

        var tokenData = ValidationHelpers.StrictCheck(result?.Data, 500, "Token response data is missing");

        var sessionId = CryptoService.GenerateSessionId();
        var now = DateTime.UtcNow;

        var sessionObject = new SessionEntity
        {
            SessionId = sessionId,
            UserId = tokenData.UserId,
            IsRevoked = false,
            AccessToken = tokenData.AccessToken,
            RefreshToken = tokenData.RefreshToken ?? string.Empty,
            TokenType = tokenData.TokenType,
            CreatedAt = now,
            ExpiresAt = now.AddMinutes(4320),
            AccessTokenExpiry = now.AddMilliseconds(tokenData.ExpiresIn),
            RefreshTokenExpiry = now.AddDays(30)
        };

        var created = await _sessionRepository.CreateSessionAsync(sessionObject);
        ValidationHelpers.StrictCheck(created, 500, "Failed to create session");

        var userResource = await FetchUserResourceAsync(tokenData.AccessToken);

        return new UserExchangeResult
        {
            SessionId = sessionId,
            Name = userResource.Name,
            Email = userResource.Email,
            Avatar = userResource.Avatar
        };
    }

    /// <summary>
    /// Refreshes expired access tokens using the refresh token, synchronized with PostgreSQL advisory locks.
    /// </summary>
    public async Task<SessionEntity> RefreshTokensAsync(string sessionId, string refreshToken)
    {
        await using var transaction = await _sessionRepository.BeginTransactionAsync();

        try
        {
            // Acquire PostgreSQL transaction-scoped advisory lock for the session
            await _sessionRepository.AcquireAdvisoryXactLockAsync($"refresh:{sessionId}");

            // Re-read session to check if another thread completed the refresh while waiting for the lock
            var existingSession = await _sessionRepository.FindActiveSessionByIdAsync(sessionId);
            if (existingSession != null && existingSession.AccessTokenExpiry.Subtract(TimeSpan.FromSeconds(10)) > DateTime.UtcNow)
            {
                await transaction.CommitAsync();
                return existingSession;
            }

            var payload = new
            {
                refresh_token = refreshToken,
                client_id = _authOptions.ClientId,
                client_secret = _authOptions.ClientSecret
            };

            var jsonContent = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync($"{_authOptions.AuthServerUrl}/refresh", jsonContent);
            var content = await response.Content.ReadAsStringAsync();

            var result = JsonSerializer.Deserialize<TruthApiResponse<AuthTokens>>(content);

            ValidationHelpers.StrictCheck(
                response.IsSuccessStatusCode || (result != null && (result.Success || result.Data != null)),
                (int)response.StatusCode,
                result?.Error ?? "Failed to refresh token");

            var tokenData = ValidationHelpers.StrictCheck(result?.Data, 500, "Refreshed token data is missing");

            var now = DateTime.UtcNow;
            var updated = await _sessionRepository.UpdateSessionByIdAsync(
                sessionId,
                tokenData.AccessToken,
                tokenData.RefreshToken ?? string.Empty,
                tokenData.TokenType,
                now.AddMilliseconds(tokenData.ExpiresIn),
                now.AddDays(30));

            ValidationHelpers.StrictCheck(updated, 500, "Failed to update session");

            await transaction.CommitAsync();

            var refreshedSession = await _sessionRepository.FindActiveSessionByIdAsync(sessionId);
            return ValidationHelpers.StrictCheck(refreshedSession, 401, "Failed to retrieve updated session");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing tokens for session {SessionId}", sessionId);
            await transaction.RollbackAsync();
            throw;
        }
    }

    /// <summary>
    /// Revokes an existing session in PostgreSQL upon logout.
    /// </summary>
    public async Task LogoutAsync(string sessionId)
    {
        var revoked = await _sessionRepository.RevokeSessionByIdAsync(sessionId);
        ValidationHelpers.StrictCheck(revoked, 500, "Failed to revoke session");
    }
}


