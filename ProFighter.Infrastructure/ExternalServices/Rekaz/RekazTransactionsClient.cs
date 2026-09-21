using System;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ProFighter.Application.Common.Exceptions;
using ProFighter.Application.Common.Interfaces;

namespace ProFighter.Infrastructure.ExternalServices.Rekaz;

public class RekazTransactionsClient : IRekazTransactionsClient
{
    private const string TransactionsEndpoint = "/api/public/transactions";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Skip
    };

    private readonly HttpClient _httpClient;
    private readonly ILogger<RekazTransactionsClient> _logger;

    public RekazTransactionsClient(HttpClient httpClient, ILogger<RekazTransactionsClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<RekazTransactionResult?> GetTransactionByIdAsync(Guid id, CancellationToken ct = default)
    {
        var requestUri = $"{TransactionsEndpoint}/{id}";

        using var httpRequest = new HttpRequestMessage(HttpMethod.Get, requestUri);
        httpRequest.Headers.TryAddWithoutValidation("Accept", "application/json");

        var sw = Stopwatch.StartNew();
        using var response = await _httpClient.SendAsync(httpRequest, ct);
        sw.Stop();

        LogHttpCall("GET", requestUri, response.StatusCode, sw.ElapsedMilliseconds, isByIdLookup: true);

        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(ct);
            throw new RekazApiException(response.StatusCode, errorBody);
        }

        var dto = await response.Content.ReadFromJsonAsync<RekazTransactionResult>(JsonOptions, ct)
            ?? throw new RekazApiException(response.StatusCode, "Empty response body on transaction fetch.");

        return dto;
    }

    private void LogHttpCall(string method, string path, HttpStatusCode statusCode, long elapsedMs, bool isByIdLookup = false)
    {
        var isExpected = (int)statusCode >= 200 && (int)statusCode <= 299 || (isByIdLookup && statusCode == HttpStatusCode.NotFound);
        if (isExpected)
        {
            _logger.LogDebug("Rekaz HTTP {Method} {Path} → {StatusCode} ({ElapsedMs} ms)", method, path, (int)statusCode, elapsedMs);
        }
        else
        {
            _logger.LogWarning("Rekaz HTTP {Method} {Path} → {StatusCode} ({ElapsedMs} ms)", method, path, (int)statusCode, elapsedMs);
        }
    }
}
