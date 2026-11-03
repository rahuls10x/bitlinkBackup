using Bitlink.Api.Data.Entities;
using Bitlink.Api.Models;

namespace Bitlink.Api.Services;

/// <summary>
/// Service contract for OAuth authentication and token lifecycle operations.
/// </summary>
public interface IAuthService
{
    Task<UserExchangeResult> ExchangeCodeAsync(string code, string codeVerifier);
    Task<UserResource> FetchUserResourceAsync(string accessToken);
    Task<SessionEntity> RefreshTokensAsync(string sessionId, string refreshToken);
    Task LogoutAsync(string sessionId);
}
