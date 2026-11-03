using System.Net;
using System.Text.Json;
using Bitlink.Api.Models;
using Bitlink.Api.Utils;

namespace Bitlink.Api.Middleware;

/// <summary>
/// Global exception handling middleware that catches ApplicationException and unhandled errors,
/// returning standardized JSON responses matching the Express errorHandling helper.
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (BitlinkException appEx)
        {
            _logger.LogWarning("Application error [{StatusCode}]: {Message}", appEx.StatusCode, appEx.Message);
            await HandleExceptionAsync(context, appEx.StatusCode, appEx.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled server error: {Message}", ex.Message);
            await HandleExceptionAsync(context, (int)HttpStatusCode.InternalServerError, ex.Message);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, int statusCode, string message)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = statusCode;

        var response = StandardResponse.Fail(message);
        var json = JsonSerializer.Serialize(response);

        await context.Response.WriteAsync(json);
    }
}
