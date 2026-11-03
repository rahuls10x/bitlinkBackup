using Bitlink.Api.Services;
using Bitlink.Api.Utils;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Bitlink.Api.Middleware;

/// <summary>
/// Action filter attribute requiring a valid sessionToken cookie.
/// Injects SessionId, AccessToken, and UserId into HttpContext.Items.
/// Corresponds to TokenAuthMiddleware.checkToken in Express.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class SessionAuthAttribute : Attribute, IAsyncActionFilter
{
    public const string SessionIdKey = "SessionId";
    public const string AccessTokenKey = "AccessToken";
    public const string UserIdKey = "UserId";

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var sessionService = context.HttpContext.RequestServices.GetRequiredService<ISessionService>();

        var token = context.HttpContext.Request.Cookies["sessionToken"];
        ValidationHelpers.StrictCheck(!string.IsNullOrWhiteSpace(token), 401, "Session token is not found");

        var session = await sessionService.ValidateSessionTokenAsync(token);

        context.HttpContext.Items[SessionIdKey] = session.SessionId;
        context.HttpContext.Items[AccessTokenKey] = session.AccessToken;
        context.HttpContext.Items[UserIdKey] = session.UserId;

        await next();
    }
}
