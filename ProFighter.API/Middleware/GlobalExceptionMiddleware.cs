using Microsoft.AspNetCore.Mvc;
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unhandled exception occurred: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        var response = exception switch
        {
            UnauthorizedAccessException => CreateErrorResponse(
                "Unauthorized",
                "Authentication is required to access this resource.",
                HttpStatusCode.Unauthorized),

            ArgumentException or InvalidOperationException => CreateErrorResponse(
                "Validation Error",
                exception.Message,
                HttpStatusCode.BadRequest),

            KeyNotFoundException => CreateErrorResponse(
                "Not Found",
                "The requested resource was not found.",
                HttpStatusCode.NotFound),

            // Handle database-related exceptions
            Microsoft.EntityFrameworkCore.DbUpdateException dbEx => CreateErrorResponse(
                "Database Error",
                "An error occurred while processing your request. Please try again later.",
                HttpStatusCode.InternalServerError),

            // Handle timeout exceptions
            TimeoutException => CreateErrorResponse(
                "Timeout",
                "The request took too long to process. Please try again.",
                HttpStatusCode.RequestTimeout),

            // Default handler for all other exceptions
            _ => CreateErrorResponse(
                "Internal Server Error",
                "An unexpected error occurred. Please try again later.",
                HttpStatusCode.InternalServerError)
        };

        context.Response.StatusCode = (int)response.StatusCode;
        await context.Response.WriteAsync(JsonSerializer.Serialize(response));
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
