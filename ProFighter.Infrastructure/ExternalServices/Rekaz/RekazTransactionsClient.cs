using System;
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

        _logger.LogInformation("Rekaz GetTransactionById → GET {Endpoint}", requestUri);

        using var httpRequest = new HttpRequestMessage(HttpMethod.Get, requestUri);
        httpRequest.Headers.TryAddWithoutValidation("Accept", "application/json");

        using var response = await _httpClient.SendAsync(httpRequest, ct);

        _logger.LogInformation(
            "Rekaz GetTransactionById ← {StatusCode} ({StatusCodeInt})",
            response.StatusCode, (int)response.StatusCode);

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
}
