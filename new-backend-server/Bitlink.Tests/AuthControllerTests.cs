using Bitlink.Api.Controllers;
using Bitlink.Api.Middleware;
using Bitlink.Api.Models;
using Bitlink.Api.Services;
using Bitlink.Api.Utils;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace Bitlink.Tests;

public class AuthControllerTests
{
    private readonly Mock<IAuthService> _authServiceMock;
    private readonly Mock<IWebHostEnvironment> _environmentMock;
    private readonly AuthController _controller;
    private readonly DefaultHttpContext _httpContext;

    public AuthControllerTests()
    {
        _authServiceMock = new Mock<IAuthService>();
        _environmentMock = new Mock<IWebHostEnvironment>();
        _environmentMock.Setup(e => e.EnvironmentName).Returns("Development");

        _controller = new AuthController(_authServiceMock.Object, _environmentMock.Object);

        _httpContext = new DefaultHttpContext();
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = _httpContext
        };
    }

    [Fact]
    public async Task ExchangeToken_WithValidCodeAndVerifier_SetsCookieAndReturnsUserData()
    {
        // Arrange
        var request = new TokenizeRequest
        {
            Code = "auth_valid_code_1234567890",
            CodeVerifier = "valid_code_verifier_long_enough_to_satisfy_constraints_123456"
        };

        var mockResult = new UserExchangeResult
        {
            SessionId = "session_1234567890abcdef1234567890abcdef",
            Name = "Test User",
            Email = "test@example.com"
        };

        _authServiceMock
            .Setup(s => s.ExchangeCodeAsync(request.Code, request.CodeVerifier))
            .ReturnsAsync(mockResult);

        // Act
        var result = await _controller.ExchangeToken(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<StandardResponse>(okResult.Value);
        Assert.True(response.Success);
        Assert.Equal("Authentication successful", response.Message);

        var data = Assert.IsType<TokenExchangeResponseData>(response.Data);
        Assert.Equal("Test User", data.Name);
        Assert.Equal("test@example.com", data.Email);

        // Verify Set-Cookie header contains sessionToken
        var cookies = _httpContext.Response.Headers.SetCookie.ToString();
        Assert.Contains("sessionToken=session_1234567890abcdef1234567890abcdef", cookies);
    }

    [Fact]
    public async Task ExchangeToken_WhenCodeMissing_ThrowsBitlinkException400()
    {
        var request = new TokenizeRequest
        {
            Code = null,
            CodeVerifier = "valid_code_verifier_long_enough_to_satisfy_constraints_123456"
        };

        var ex = await Assert.ThrowsAsync<BitlinkException>(() => _controller.ExchangeToken(request));
        Assert.Equal(400, ex.StatusCode);
        Assert.Equal("Authorization code is required", ex.Message);
    }

    [Fact]
    public async Task ExchangeToken_WhenCodeDoesNotStartWithAuth_ThrowsBitlinkException400()
    {
        var request = new TokenizeRequest
        {
            Code = "invalid_prefix_code",
            CodeVerifier = "valid_code_verifier_long_enough_to_satisfy_constraints_123456"
        };

        var ex = await Assert.ThrowsAsync<BitlinkException>(() => _controller.ExchangeToken(request));
        Assert.Equal(400, ex.StatusCode);
        Assert.Contains("Authorization Code must be a valid code starting with auth_", ex.Message);
    }

    [Fact]
    public async Task ExchangeToken_WhenCodeVerifierTooShort_ThrowsBitlinkException400()
    {
        var request = new TokenizeRequest
        {
            Code = "auth_valid_code_1234567890",
            CodeVerifier = "short_verifier"
        };

        var ex = await Assert.ThrowsAsync<BitlinkException>(() => _controller.ExchangeToken(request));
        Assert.Equal(400, ex.StatusCode);
        Assert.Contains("Invalid Code Verifier", ex.Message);
    }

    [Fact]
    public async Task Me_WhenAccessTokenPresent_ReturnsUserProfile()
    {
        _httpContext.Items[SessionAuthAttribute.AccessTokenKey] = "valid_access_token";

        var mockUser = new UserResource
        {
            UserId = "user_123",
            Name = "Test User",
            Email = "test@example.com"
        };

        _authServiceMock
            .Setup(s => s.FetchUserResourceAsync("valid_access_token"))
            .ReturnsAsync(mockUser);

        var result = await _controller.Me();

        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<StandardResponse>(okResult.Value);
        Assert.True(response.Success);
        Assert.Equal("Authentication successful", response.Message);
        Assert.Equal(mockUser, response.Data);
    }

    [Fact]
    public async Task Logout_WhenSessionTokenPresent_RevokesSessionAndClearsCookie()
    {
        _httpContext.Request.Headers.Append("Cookie", "sessionToken=session_test_token_123");

        _authServiceMock
            .Setup(s => s.LogoutAsync("session_test_token_123"))
            .Returns(Task.CompletedTask);

        var result = await _controller.Logout();

        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<StandardResponse>(okResult.Value);
        Assert.True(response.Success);
        Assert.Equal("Logout successful", response.Message);

        _authServiceMock.Verify(s => s.LogoutAsync("session_test_token_123"), Times.Once);
    }

    [Fact]
    public async Task Logout_WhenSessionTokenMissing_ThrowsBitlinkException500()
    {
        var ex = await Assert.ThrowsAsync<BitlinkException>(() => _controller.Logout());
        Assert.Equal(500, ex.StatusCode);
        Assert.Equal("Session token is not found", ex.Message);
    }
}
