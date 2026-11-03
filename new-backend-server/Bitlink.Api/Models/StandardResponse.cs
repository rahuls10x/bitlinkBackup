using System.Text.Json.Serialization;

namespace Bitlink.Api.Models;

/// <summary>
/// Standard API response model returned across all endpoints, matching the Express backend format.
/// </summary>
public class StandardResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("message")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Message { get; set; }

    [JsonPropertyName("error")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Error { get; set; }

    [JsonPropertyName("data")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object? Data { get; set; }

    public static StandardResponse Ok(string message, object? data = null) =>
        new() { Success = true, Message = message, Data = data };

    public static StandardResponse Fail(string error) =>
        new() { Success = false, Error = error };
}

/// <summary>
/// Strongly-typed generic standard response.
/// </summary>
public class StandardResponse<T>
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("message")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Message { get; set; }

    [JsonPropertyName("error")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Error { get; set; }

    [JsonPropertyName("data")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public T? Data { get; set; }

    public static StandardResponse<T> Ok(string message, T? data = default) =>
        new() { Success = true, Message = message, Data = data };

    public static StandardResponse<T> Fail(string error) =>
        new() { Success = false, Error = error };
}
