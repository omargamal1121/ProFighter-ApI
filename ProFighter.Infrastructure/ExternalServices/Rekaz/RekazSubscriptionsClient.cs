using Microsoft.Extensions.Logging;
using ProFighter.Application.Common.Exceptions;
using ProFighter.Application.Common.Interfaces;
using ProFighter.Application.Common.Models;
using ProFighter.Infrastructure.ExternalServices.Rekaz.Dtos;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace ProFighter.Infrastructure.ExternalServices.Rekaz;

/// <summary>
/// Typed HttpClient implementation of <see cref="IRekazSubscriptionsClient"/>.
/// Uses the shared "RekazClient" named HttpClient.
/// </summary>
public sealed class RekazSubscriptionsClient : IRekazSubscriptionsClient
{
    private const string SubscriptionsEndpoint = "/api/public/subscriptions";
    private const int FixedCustomerType = 12;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _httpClient;
    private readonly ILogger<RekazSubscriptionsClient> _logger;

    public RekazSubscriptionsClient(HttpClient httpClient, ILogger<RekazSubscriptionsClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<RekazSubscriptionCreatedResult> CreateSubscriptionAsync(
        CreateRekazSubscriptionRequest request,
        CancellationToken ct = default)
    {
        // 1. Validation
        if (request.CustomerId.HasValue && request.NewCustomerDetails != null)
            throw new ArgumentException("Cannot specify both CustomerId and NewCustomerDetails. Choose one.", nameof(request));

        if (!request.CustomerId.HasValue && request.NewCustomerDetails == null)
            throw new ArgumentException("Either CustomerId or NewCustomerDetails must be specified.", nameof(request));

        if (request.Items == null || request.Items.Count == 0)
            throw new ArgumentException("Subscription must contain at least 1 item.", nameof(request));

        _logger.LogInformation("Rekaz CreateSubscription → POST {Endpoint}", SubscriptionsEndpoint);

        // 2. Build Payload
        object? customerDetails = null;
        if (request.NewCustomerDetails != null)
        {
            customerDetails = new
            {
                name = request.NewCustomerDetails.Name,
                mobileNumber = request.NewCustomerDetails.MobileNumber,
                email = request.NewCustomerDetails.Email,
                type = FixedCustomerType,
                companyName = request.NewCustomerDetails.CompanyName
            };
        }

        var body = new
        {
            customerId = request.CustomerId,
            customerDetails = customerDetails,
            startAt = request.StartAt,
            discount = request.Discount != null ? new { type = request.Discount.Type, value = request.Discount.Value } : null,
            branchId = request.BranchId,
            items = request.Items.Select(i => new
            {
                priceId = i.PriceId,
                quantity = i.Quantity,
                loyaltyRewardId = i.LoyaltyRewardId
            }).ToList(),
            occurenceDays = request.OccurenceDays
        };

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, SubscriptionsEndpoint)
        {
            Content = JsonContent.Create(body, options: JsonOptions)
        };
        httpRequest.Headers.TryAddWithoutValidation("Accept", "application/json");

        using var response = await _httpClient.SendAsync(httpRequest, ct);

        _logger.LogInformation(
            "Rekaz CreateSubscription ← {StatusCode} ({StatusCodeInt})",
            response.StatusCode, (int)response.StatusCode);

        if (!response.IsSuccessStatusCode)
        {
            var bodyText = await response.Content.ReadAsStringAsync(ct);
            throw new RekazApiException(response.StatusCode, bodyText);
        }

        var dto = await response.Content.ReadFromJsonAsync<RekazSubscriptionCreatedDto>(JsonOptions, ct)
            ?? throw new RekazApiException(response.StatusCode, "Empty response body on subscription creation.");

        return new RekazSubscriptionCreatedResult(dto.InvoiceId, dto.PaymentLink);
    }

    /// <inheritdoc/>
    public async Task<RekazSubscriptionsListResult> GetSubscriptionsAsync(
        RekazSubscriptionsQuery query,
        CancellationToken ct = default)
    {
        var qs = BuildListQueryString(query);
        var requestUri = $"{SubscriptionsEndpoint}{qs}";

        _logger.LogInformation("Rekaz GetSubscriptions → GET {Endpoint}{Query}", SubscriptionsEndpoint, qs);

        using var httpRequest = new HttpRequestMessage(HttpMethod.Get, requestUri);
        httpRequest.Headers.TryAddWithoutValidation("Accept", "application/json");

        using var response = await _httpClient.SendAsync(httpRequest, ct);

        _logger.LogInformation(
            "Rekaz GetSubscriptions ← {StatusCode} ({StatusCodeInt})",
            response.StatusCode, (int)response.StatusCode);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(ct);
            throw new RekazApiException(response.StatusCode, errorBody);
        }

        var dto = await response.Content.ReadFromJsonAsync<RekazSubscriptionsListResponse>(JsonOptions, ct)
            ?? throw new RekazApiException(response.StatusCode, "Empty response body on subscription list.");

        return new RekazSubscriptionsListResult(
            Items: dto.Items.Select(MapSubscription).ToList(),
            TotalCount: dto.TotalCount
        );
    }

    /// <inheritdoc/>
    public async Task<RekazSubscriptionResult?> GetSubscriptionByIdAsync(
        Guid id,
        CancellationToken ct = default)
    {
        var requestUri = $"{SubscriptionsEndpoint}/{id}";

        _logger.LogInformation("Rekaz GetSubscriptionById → GET {Endpoint}", requestUri);

        using var httpRequest = new HttpRequestMessage(HttpMethod.Get, requestUri);
        httpRequest.Headers.TryAddWithoutValidation("Accept", "application/json");

        using var response = await _httpClient.SendAsync(httpRequest, ct);

        _logger.LogInformation(
            "Rekaz GetSubscriptionById ← {StatusCode} ({StatusCodeInt})",
            response.StatusCode, (int)response.StatusCode);

        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(ct);
            throw new RekazApiException(response.StatusCode, errorBody);
        }

        var dto = await response.Content.ReadFromJsonAsync<RekazSubscriptionDto>(JsonOptions, ct)
            ?? throw new RekazApiException(response.StatusCode, "Empty response body on subscription fetch.");

        return MapSubscription(dto);
    }

    private static string BuildListQueryString(RekazSubscriptionsQuery q)
    {
        var clampedMax = Math.Clamp(q.MaxResultCount, 1, 100);

        var parts = new List<string>
        {
            $"SkipCount={q.SkipCount}",
            $"MaxResultCount={clampedMax}",
        };

        if (q.CustomerId.HasValue)
            parts.Add($"CustomerId={q.CustomerId.Value}");

        if (q.StartAtMin.HasValue)
            parts.Add($"StartAtMin={q.StartAtMin.Value:O}");

        if (q.StartAtMax.HasValue)
            parts.Add($"StartAtMax={q.StartAtMax.Value:O}");

        if (q.NextBillingAtMin.HasValue)
            parts.Add($"NextBillingAtMin={q.NextBillingAtMin.Value:O}");

        if (q.NextBillingAtMax.HasValue)
            parts.Add($"NextBillingAtMax={q.NextBillingAtMax.Value:O}");

        if (q.Statuses != null && q.Statuses.Count > 0)
        {
            // Verified standard ASP.NET model binding array query representation
            parts.AddRange(q.Statuses.Select(s => $"Statuses={s}"));
        }

        if (!string.IsNullOrWhiteSpace(q.CustomerMobile))
            parts.Add($"CustomerMobile={Uri.EscapeDataString(q.CustomerMobile)}");

        if (!string.IsNullOrWhiteSpace(q.Keyword))
            parts.Add($"Keyword={Uri.EscapeDataString(q.Keyword)}");

        if (q.PriceIds != null && q.PriceIds.Count > 0)
        {
            parts.AddRange(q.PriceIds.Select(p => $"PriceIds={p}"));
        }

        if (q.BranchId.HasValue)
            parts.Add($"BranchId={q.BranchId.Value}");

        if (!string.IsNullOrWhiteSpace(q.Sorting))
            parts.Add($"Sorting={Uri.EscapeDataString(q.Sorting)}");

        return "?" + string.Join("&", parts);
    }

    private static RekazSubscriptionResult MapSubscription(RekazSubscriptionDto dto) =>
        new(
            Id: dto.Id,
            SubscriptionCode: dto.SubscriptionCode,
            CustomerId: dto.CustomerId,
            StartAt: dto.StartAt,
            EndAt: dto.EndAt,
            Status: dto.Status,
            PaidAmount: dto.PaidAmount,
            TotalAmount: dto.TotalAmount,
            RemainingAmount: dto.RemainingAmount,
            IsPaused: dto.IsPaused,
            PausedAt: dto.PausedAt,
            ResumeAt: dto.ResumeAt
        );
}
