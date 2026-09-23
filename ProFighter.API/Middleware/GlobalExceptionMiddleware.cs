using ProFighter.Application.Common;
using System.Net;
using System.Text.Json;

namespace ProFighter.API.Middleware;

public class GlobalExceptionMiddleware : IMiddleware
{
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(ILogger<GlobalExceptionMiddleware> logger)
    {
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        try
        {
            await next(context);
        }
        catch (UnauthorizedAccessException ex)
        {
            var formattedError = ProFighter.Infrastructure.Logging.ExceptionLogFormatter.ToOneLine(ex);
            _logger.LogWarning("Authentication/authorization failure: {Error}", formattedError);
            await HandleExceptionAsync(context, ex, _logger);
        }
        catch (ArgumentException ex)
        {
            var formattedError = ProFighter.Infrastructure.Logging.ExceptionLogFormatter.ToOneLine(ex);
            _logger.LogWarning("Validation error: {Error}", formattedError);
            await HandleExceptionAsync(context, ex, _logger);
        }
        catch (InvalidOperationException ex)
        {
            var formattedError = ProFighter.Infrastructure.Logging.ExceptionLogFormatter.ToOneLine(ex);
            _logger.LogWarning("Invalid operation: {Error}", formattedError);
            await HandleExceptionAsync(context, ex, _logger);
        }
        catch (KeyNotFoundException ex)
        {
            var formattedError = ProFighter.Infrastructure.Logging.ExceptionLogFormatter.ToOneLine(ex);
            _logger.LogWarning("Resource not found: {Error}", formattedError);
            await HandleExceptionAsync(context, ex, _logger);
        }
        catch (Exception ex)
        {
            var formattedError = ProFighter.Infrastructure.Logging.ExceptionLogFormatter.ToOneLine(ex);
            _logger.LogError("Unhandled exception: {Error}", formattedError);
            await HandleExceptionAsync(context, ex, _logger);
        }
    }

    private static async Task HandleExceptionAsync(
        HttpContext context,
        Exception exception,
        ILogger logger)
    {
        if (context.Response.HasStarted)
        {
            var formattedError = ProFighter.Infrastructure.Logging.ExceptionLogFormatter.ToOneLine(exception);
            logger.LogError(
                "Exception thrown after response started: {Error} | Path: {Path} | Method: {Method} | StatusCode: {StatusCode}",
                formattedError,
                context.Request.Path,
                context.Request.Method,
                context.Response.StatusCode);

            throw exception; // rethrow original — do NOT wrap
        }

        // Response has NOT started — safe to clear and write our error response.
        context.Response.Clear();
        context.Response.ContentType = "application/json";

        var response = exception switch
        {
            UnauthorizedAccessException => CreateErrorResponse(
                "Unauthorized",
                !string.IsNullOrWhiteSpace(exception.Message) && !exception.Message.StartsWith("Attempted to perform an unauthorized operation", StringComparison.OrdinalIgnoreCase)
                    ? exception.Message
                    : "Authentication is required to access this resource.",
                HttpStatusCode.Unauthorized),

            ArgumentException or InvalidOperationException => CreateErrorResponse(
                "Validation Error",
                exception.Message,
                HttpStatusCode.BadRequest),

            KeyNotFoundException => CreateErrorResponse(
                "Not Found",
                "The requested resource was not found.",
                HttpStatusCode.NotFound),

            Microsoft.EntityFrameworkCore.DbUpdateException => CreateErrorResponse(
                "Database Error",
                "An error occurred while processing your request. Please try again later.",
                HttpStatusCode.InternalServerError),

            TimeoutException => CreateErrorResponse(
                "Timeout",
                "The request took too long to process. Please try again.",
                HttpStatusCode.RequestTimeout),

            _ => CreateErrorResponse(
                "Internal Server Error",
                "An unexpected error occurred. Please try again later.",
                HttpStatusCode.InternalServerError)
        };

        context.Response.StatusCode = (int)response.StatusCode;

        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        await context.Response.WriteAsync(
            JsonSerializer.Serialize(response.Response, jsonOptions));
    }

    private static (ApiResponse<object> Response, HttpStatusCode StatusCode) CreateErrorResponse(
        string errorType,
        string message,
        HttpStatusCode statusCode)
    {
        var apiResponse = ApiResponse<object>.CreateErrorResponse(
            message,
            new ErrorResponse(errorType, message),
            (int)statusCode);

        return (apiResponse, statusCode);
    }
}
