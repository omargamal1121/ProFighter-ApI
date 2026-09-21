using System;
using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using ProFighter.Application.Common.Exceptions;
using ProFighter.Application.Common.Interfaces;
using ProFighter.Application.Common.Models;
using ProFighter.Infrastructure.ExternalServices.Rekaz.Dtos;

namespace ProFighter.Infrastructure.ExternalServices.Rekaz;

public sealed class RekazProductsClient : IRekazProductsClient
{
    private const string ProductsEndpoint = "/api/public/products";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _httpClient;
    private readonly ILogger<RekazProductsClient> _logger;

    public RekazProductsClient(
        HttpClient httpClient,
        ILogger<RekazProductsClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<RekazProductsResult> GetProductsAsync(
        RekazProductsQuery query,
        CancellationToken ct = default)
    {
        var qs = BuildQueryString(query);
        var requestUri = $"{ProductsEndpoint}{qs}";

        using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
        request.Headers.TryAddWithoutValidation("Accept", "application/json");

        var sw = Stopwatch.StartNew();
        using var response = await _httpClient.SendAsync(request, ct);
        sw.Stop();

        LogHttpCall("GET", requestUri, response.StatusCode, sw.ElapsedMilliseconds);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new RekazApiException(response.StatusCode, body);
        }

        var dto = await response.Content.ReadFromJsonAsync<RekazProductsResponse>(JsonOptions, ct)
            ?? throw new RekazApiException(response.StatusCode, "Empty response body");

        return MapToResult(dto);
    }

    private void LogHttpCall(string method, string path, HttpStatusCode statusCode, long elapsedMs)
    {
        var isExpected = (int)statusCode >= 200 && (int)statusCode <= 299;
        if (isExpected)
        {
            _logger.LogDebug("Rekaz HTTP {Method} {Path} → {StatusCode} ({ElapsedMs} ms)", method, path, (int)statusCode, elapsedMs);
        }
        else
        {
            _logger.LogWarning("Rekaz HTTP {Method} {Path} → {StatusCode} ({ElapsedMs} ms)", method, path, (int)statusCode, elapsedMs);
        }
    }

    private static string BuildQueryString(RekazProductsQuery q)
    {
        var parts = new List<string>
        {
            $"SkipCount={q.SkipCount}",
            $"MaxResultCount={q.MaxResultCount}",
        };

        if (!string.IsNullOrWhiteSpace(q.Keyword))
            parts.Add($"Keyword={Uri.EscapeDataString(q.Keyword)}");

        if (q.Type.HasValue)
            parts.Add($"Type={(int)q.Type.Value}");

        if (q.BranchId.HasValue)
            parts.Add($"BranchId={q.BranchId.Value}");

        if (!string.IsNullOrWhiteSpace(q.Sorting))
            parts.Add($"Sorting={Uri.EscapeDataString(q.Sorting)}");

        return "?" + string.Join("&", parts);
    }

    private static RekazProductsResult MapToResult(RekazProductsResponse dto) =>
        new(
            Items: dto.Items.Select(MapProduct).ToList(),
            TotalCount: dto.TotalCount
        );

    private static RekazProductSummary MapProduct(RekazProductDto p) =>
        new(
            Id: p.Id,
            Name: p.Name,
            Amount: p.Amount,
            ImageUrl: p.FeaturedImage ?? p.Images.FirstOrDefault(),
            IsOutOfStock: p.IsOutOfStock,
            StockQuantity: p.StockQuantity,
            ProductType: (RekazProductType)p.Type,
            TypeString: p.TypeString,
            Prices: p.Pricing.Select(MapPrice).ToList()
        );

    private static RekazPriceSummary MapPrice(RekazPricingDto pr) =>
        new(
            Id: pr.Id,
            Name: pr.Name,
            Amount: pr.Amount,
            DiscountedAmount: pr.DiscountedAmount,
            DiscountValidFrom: pr.DiscountValidFrom,
            DiscountValidUntil: pr.DiscountValidUntil,
            IsRecurring: pr.Type == 2
        );
}
