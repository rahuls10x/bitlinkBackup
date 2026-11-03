using Bitlink.Api.Data.Entities;

namespace Bitlink.Api.Services;

/// <summary>
/// Service contract for session validation and lifecycle management.
/// </summary>
public interface ISessionService
{
    Task<SessionEntity> ValidateSessionTokenAsync(string? token);
}
