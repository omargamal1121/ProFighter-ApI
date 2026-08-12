using System.Net;

namespace ProFighter.Application.Common.Exceptions;

/// <summary>
/// Thrown when the Rekaz API returns a non-success HTTP status code.
/// Callers should catch this exception to handle Rekaz-specific errors
/// without depending on <see cref="HttpRequestException"/>.
/// </summary>
public class RekazApiException : Exception
{
    /// <summary>The HTTP status code returned by the Rekaz API.</summary>
    public HttpStatusCode StatusCode { get; }

    /// <summary>The raw response body returned by the Rekaz API.</summary>
    public string ResponseBody { get; }

    public RekazApiException(HttpStatusCode statusCode, string responseBody)
        : base($"Rekaz API returned {(int)statusCode} {statusCode}: {responseBody}")
    {
        StatusCode = statusCode;
        ResponseBody = responseBody;
    }
}
