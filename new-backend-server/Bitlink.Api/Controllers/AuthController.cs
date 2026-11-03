using Bitlink.Api.Middleware;
using Bitlink.Api.Models;
using Bitlink.Api.Services;
using Bitlink.Api.Utils;
using Microsoft.AspNetCore.Mvc;

namespace Bitlink.Api.Controllers;

[ApiController]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IWebHostEnvironment _environment;

    public AuthController(IAuthService authService, IWebHostEnvironment environment)
    {
        _authService = authService;
        _environment = environment;
    }

    /// <summary>
    /// Handles token exchange request from client callback.
    ///
    /// Inorder Flow:
    /// - Retrieve code and code_verifier from request body and validate constraints
    /// - Exchange code for access & refresh tokens and user resource
    /// - Set HTTP-only sessionToken cookie
    /// - Structure success response with User resources
    /// </summary>
    [HttpPost("api/auth/token")]
    public async Task<IActionResult> ExchangeToken([FromBody] TokenizeRequest request)
    {
        ValidationHelpers.StrictCheck(request != null, 400, "Request body is required");
        ValidationHelpers.StrictCheck(!string.IsNullOrWhiteSpace(request!.Code), 400, "Authorization code is required");
        ValidationHelpers.StrictCheck(!string.IsNullOrWhiteSpace(request.CodeVerifier), 400, "Code verifier is required");

        if (!request.Code!.StartsWith("auth_"))
        {
            throw new BitlinkException(400, "Authorization Code must be a valid code starting with auth_");
        }

        if (request.CodeVerifier!.Length < 43 || request.CodeVerifier.Length > 128)
        {
            throw new BitlinkException(400, "Invalid Code Verifier. Length must be between 43 and 128 characters.");
        }

        var userData = await _authService.ExchangeCodeAsync(request.Code, request.CodeVerifier);

        Response.Cookies.Append("sessionToken", userData.SessionId, new CookieOptions
        {
            HttpOnly = true,
            Secure = !_environment.IsDevelopment(),
            SameSite = SameSiteMode.Lax,
            MaxAge = TimeSpan.FromDays(30)
        });

        return Ok(StandardResponse.Ok("Authentication successful", new TokenExchangeResponseData
        {
            Name = userData.Name,
            Email = userData.Email
        }));
    }

    /// <summary>
    /// Handles me request returning user profile details.
    ///
    /// Inorder Flow:
    /// - Retrieve access token from request context (set by SessionAuth)
    /// - Fetch user resource from resource server
    /// - Structure success response with User resources
    /// </summary>
    [HttpGet("api/auth/me")]
    [SessionAuth]
    public async Task<IActionResult> Me()
    {
        var accessToken = HttpContext.Items[SessionAuthAttribute.AccessTokenKey] as string;
        ValidationHelpers.StrictCheck(!string.IsNullOrWhiteSpace(accessToken), 500, "Access token is not found");

        var user = await _authService.FetchUserResourceAsync(accessToken!);

        return Ok(StandardResponse.Ok("Authentication successful", user));
    }

    /// <summary>
    /// Handles logout request.
    ///
    /// Inorder Flow:
    /// - Retrieve session token from cookie
    /// - Invalidate Session Token in database
    /// - Clear session token cookie and send success response
    /// </summary>
    [HttpPost("api/auth/logout")]
    public async Task<IActionResult> Logout()
    {
        var token = Request.Cookies["sessionToken"];
        ValidationHelpers.StrictCheck(!string.IsNullOrWhiteSpace(token), 500, "Session token is not found");

        await _authService.LogoutAsync(token!);

        Response.Cookies.Delete("sessionToken");

        return Ok(StandardResponse.Ok("Logout successful"));
    }

    /// <summary>
    /// Sample protected endpoint verifying session validity.
    /// </summary>
    [HttpGet("api/example/protected")]
    [SessionAuth]
    public IActionResult ProtectedExample()
    {
        var sessionId = HttpContext.Items[SessionAuthAttribute.SessionIdKey] as string;
        var accessToken = HttpContext.Items[SessionAuthAttribute.AccessTokenKey] as string;

        return Ok(StandardResponse.Ok("Protected Bitlink endpoint accessed successfully", new
        {
            sessionId,
            accessToken,
            timestamp = DateTime.UtcNow.ToString("o")
        }));
    }

    /// <summary>
    /// Health check endpoint.
    /// </summary>
    [HttpGet("health")]
    public IActionResult Health()
    {
        return Ok(StandardResponse.Ok("Bitlink is healthy", new
        {
            timestamp = DateTime.UtcNow.ToString("o")
        }));
    }

    /// <summary>
    /// Root fallback.
    /// </summary>
    [HttpGet("/")]
    public IActionResult Root()
    {
        return Content("Bitlink Backend is Running!");
    }
}
