using System.Text.Json.Serialization;

namespace Bitlink.Api.Models;

/// <summary>
/// Incoming payload for POST /api/auth/token.
/// </summary>
public class TokenizeRequest
{
    [JsonPropertyName("code")]
    public string? Code { get; set; }

    [JsonPropertyName("code_verifier")]
    public string? CodeVerifier { get; set; }
}

/// <summary>
/// OAuth token response returned by Truth Auth Server /tokenize and /refresh endpoints.
/// </summary>
public class AuthTokens
{
    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; } = string.Empty;

    [JsonPropertyName("token_type")]
    public string TokenType { get; set; } = "Bearer";

    [JsonPropertyName("user_id")]
    public string UserId { get; set; } = string.Empty;

    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; set; }

    [JsonPropertyName("refresh_token")]
    public string? RefreshToken { get; set; }

    [JsonPropertyName("scopes")]
    public List<string>? Scopes { get; set; }
}

/// <summary>
/// Truth OAuth server generic JSON envelope: { success, data, error, message }.
/// </summary>
public class TruthApiResponse<T>
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("data")]
    public T? Data { get; set; }

    [JsonPropertyName("error")]
    public string? Error { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }
}

/// <summary>
/// User identity resource returned from Truth Resource Server /resource endpoint.
/// </summary>
public class UserResource
{
    [JsonPropertyName("userId")]
    public string? UserId { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("email")]
    public string? Email { get; set; }

    [JsonPropertyName("avatar")]
    public string? Avatar { get; set; }

    [JsonPropertyName("age")]
    public int? Age { get; set; }

    [JsonPropertyName("gender")]
    public string? Gender { get; set; }
}

/// <summary>
/// Internal result of code exchange combining session identifier and user profile.
/// </summary>
public class UserExchangeResult
{
    public string SessionId { get; set; } = string.Empty;
    public string? Name { get; set; }
    public string? Email { get; set; }
    public string? Avatar { get; set; }
}

/// <summary>
/// Data payload returned in POST /api/auth/token response.
/// </summary>
public class TokenExchangeResponseData
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("email")]
    public string? Email { get; set; }
}
