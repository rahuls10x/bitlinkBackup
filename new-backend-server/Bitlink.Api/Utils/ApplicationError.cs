using System.Diagnostics.CodeAnalysis;

namespace Bitlink.Api.Utils;

/// <summary>
/// Custom application exception representing business logic errors with HTTP status codes.
/// Corresponds to ApplicationError in the Express backend.
/// </summary>
public class BitlinkException : Exception
{
    public int StatusCode { get; }

    public BitlinkException(int statusCode, string message) : base(message)
    {
        StatusCode = statusCode;
    }
}

/// <summary>
/// Assertion and validation helpers matching the Express strictCheck utility.
/// Enforces non-nullability and valid conditions, throwing BitlinkException on failure.
/// </summary>
public static class ValidationHelpers
{
    /// <summary>
    /// Validates that a reference or nullable value is non-null.
    /// If null, throws an BitlinkException with the given statusCode and message.
    /// </summary>
    public static T StrictCheck<T>([NotNull] T? value, int statusCode, string message) where T : class
    {
        if (value is null)
        {
            throw new BitlinkException(statusCode, message);
        }
        return value;
    }

    /// <summary>
    /// Validates that a nullable value type has a value.
    /// If null, throws an BitlinkException with the given statusCode and message.
    /// </summary>
    public static T StrictCheck<T>([NotNull] T? value, int statusCode, string message) where T : struct
    {
        if (!value.HasValue)
        {
            throw new BitlinkException(statusCode, message);
        }
        return value.Value;
    }

    /// <summary>
    /// Validates that a string is neither null nor empty nor whitespace.
    /// If invalid, throws an BitlinkException with the given statusCode and message.
    /// </summary>
    public static string StrictCheckString([NotNull] string? value, int statusCode, string message)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new BitlinkException(statusCode, message);
        }
        return value;
    }

    /// <summary>
    /// Validates a boolean condition. If false, throws an BitlinkException with the given statusCode and message.
    /// </summary>
    public static void StrictCheck(bool condition, int statusCode, string message)
    {
        if (!condition)
        {
            throw new BitlinkException(statusCode, message);
        }
    }
}

