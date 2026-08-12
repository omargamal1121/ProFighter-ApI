using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ProFighter.Application.Common.Exceptions;
using ProFighter.Application.Common.Interfaces;
using ProFighter.Application.Common.Models;
using ProFighter.Infrastructure.ExternalServices.Rekaz.Dtos;
using System.Net.Http.Json;
using System.Text.Json;

namespace ProFighter.Infrastructure.ExternalServices.Rekaz;

/// <summary>
/// Typed HttpClient implementation of <see cref="IRekazProductsClient"/>.
/// Registered via <c>AddHttpClient&lt;IRekazProductsClient, RekazProductsClient&gt;()</c>.
/// </summary>
public sealed class RekazProductsClient : IRekazProductsClient
{
    private const string ProductsEndpoint = "/api/public/products";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _httpClient;
    private readonly RekazOptions _options;
    private readonly ILogger<RekazProductsClient> _logger;

    public RekazProductsClient(
        HttpClient httpClient,
        IOptions<RekazOptions> options,
        ILogger<RekazProductsClient> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<RekazProductsResult> GetProductsAsync(
        RekazProductsQuery query,
        CancellationToken ct = default)
    {
        var qs = BuildQueryString(query);
        var requestUri = $"{ProductsEndpoint}{qs}";

        _logger.LogInformation(
            "Rekaz GetProducts → {Endpoint}{Query}",
            ProductsEndpoint, qs);

        using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);

        // Authorization: Basic <value used as-is — not re-encoded>
        request.Headers.TryAddWithoutValidation("Authorization", _options.ApiKeyBase64);
        request.Headers.TryAddWithoutValidation("__tenant", _options.TenantId);
        request.Headers.TryAddWithoutValidation("Accept", "application/json");

        using var response = await _httpClient.SendAsync(request, ct);

        _logger.LogInformation(
            "Rekaz GetProducts ← {StatusCode} ({StatusCodeInt})",
            response.StatusCode, (int)response.StatusCode);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new RekazApiException(response.StatusCode, body);
        }

        var dto = await response.Content.ReadFromJsonAsync<RekazProductsResponse>(JsonOptions, ct)
            ?? throw new RekazApiException(response.StatusCode, "Empty response body");

        return MapToResult(dto);
    }

    /// <summary>
    /// Builds the query string from <paramref name="q"/>.
    /// Only non-null / non-default-sentinel fields are included.
    /// </summary>
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

    // -----------------------------------------------------------------------
    // Mapping — Infrastructure DTOs → Application models
    // -----------------------------------------------------------------------

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
            ProductType: (RekazProductType)p.Type,   // 0=Reservation, 1=Subscription, 2=Merchandise
            TypeString: p.TypeString,
            Prices: p.Pricing.Select(MapPrice).ToList()
        );

    private static RekazPriceSummary MapPrice(RekazPricingDto pr) =>
        new(
            Id: pr.Id,
            Name: pr.Name,
            Amount: pr.Amount,
            DiscountedAmount: pr.DiscountedAmount,
            IsRecurring: pr.Type == 2   // PriceType: 2 = Recurring
        );
}
