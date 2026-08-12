namespace ProFighter.Application.Common;

/// <summary>
/// Unified API response envelope returned from all endpoints.
/// All responses — success or failure — share the same shape, enabling consistent client handling.
/// </summary>
public sealed class ApiResponse<T>
{
    public bool Success { get; private init; }
    public string Message { get; private init; } = string.Empty;
    public T? Data { get; private init; }
    public int StatusCode { get; private init; }
    public ErrorResponse? Error { get; private init; }
    public IEnumerable<string>? Warnings { get; private init; }

    public static ApiResponse<T> CreateSuccessResponse(
        string message,
        T? data,
        int statusCode,
        IEnumerable<string>? warnings = null) =>
        new()
        {
            Success    = true,
            Message    = message,
            Data       = data,
            StatusCode = statusCode,
            Warnings   = warnings
        };

    public static ApiResponse<T> CreateErrorResponse(
        string message,
        ErrorResponse error,
        int statusCode,
        IEnumerable<string>? warnings = null) =>
        new()
        {
            Success    = false,
            Message    = message,
            Error      = error,
            StatusCode = statusCode,
            Warnings   = warnings
        };
}

/// <summary>Structured error details embedded in ApiResponse on failure.</summary>
public sealed class ErrorResponse
{
    public string Type { get; init; }
    public List<string> Errors { get; init; }

    public ErrorResponse(string type, List<string> errors)
    {
        Type   = type;
        Errors = errors;
    }

    public ErrorResponse(string type, string error)
    {
        Type   = type;
        Errors = [error];
    }
}
