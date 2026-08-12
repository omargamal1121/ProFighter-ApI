namespace ProFighter.Application.Common;

/// <summary>
/// Typed result wrapper for application layer operations.
/// Services always return Result&lt;T&gt; or Result — never throw business exceptions.
/// HTTP status codes are carried here to keep controllers strictly thin.
/// </summary>
public sealed class Result<T>
{
    public bool IsSuccess { get; private init; }
    public T? Data { get; private init; }
    public string? Message { get; private init; }
    public List<string>? Errors { get; private init; }
    public int Status { get; private init; }

    public static Result<T> Success(T data, string? message = null, int status = 200) =>
        new() { IsSuccess = true, Data = data, Message = message, Status = status };

    public static Result<T> Failure(string error, int status = 400) =>
        new() { IsSuccess = false, Errors = [error], Status = status };

    public static Result<T> Failure(List<string> errors, int status = 400) =>
        new() { IsSuccess = false, Errors = errors, Status = status };
}

/// <summary>Non-generic variant for operations that return no payload.</summary>
public sealed class Result
{
    public bool IsSuccess { get; private init; }
    public string? Message { get; private init; }
    public List<string>? Errors { get; private init; }
    public int Status { get; private init; }

    public static Result Success(string? message = null, int status = 200) =>
        new() { IsSuccess = true, Message = message, Status = status };

    public static Result Failure(string error, int status = 400) =>
        new() { IsSuccess = false, Message = error, Errors = [error], Status = status };

    public static Result Failure(List<string> errors, int status = 400) =>
        new() { IsSuccess = false, Message = errors.FirstOrDefault(), Errors = errors, Status = status };
}
