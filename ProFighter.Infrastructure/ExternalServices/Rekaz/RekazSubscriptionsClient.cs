using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using ProFighter.Application.Common.Exceptions;
using ProFighter.Application.Common.Interfaces;
using ProFighter.Application.Common.Models;
using ProFighter.Infrastructure.ExternalServices.Rekaz.Dtos;

namespace ProFighter.Infrastructure.ExternalServices.Rekaz;

public sealed class RekazSubscriptionsClient : IRekazSubscriptionsClient
{
    private const string SubscriptionsEndpoint = "/api/public/subscriptions";
    private const int FixedCustomerType = 12;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Skip,
        Converters = { new RekazDiscountTypeConverter() }
    };

    private readonly HttpClient _httpClient;
    private readonly ILogger<RekazSubscriptionsClient> _logger;

    public RekazSubscriptionsClient(HttpClient httpClient, ILogger<RekazSubscriptionsClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<RekazSubscriptionCreatedResult> CreateSubscriptionAsync(
        CreateRekazSubscriptionRequest request,
        CancellationToken ct = default)
    {
        if (request.CustomerId.HasValue && request.NewCustomerDetails != null)
            throw new ArgumentException("Cannot specify both CustomerId and NewCustomerDetails. Choose one.", nameof(request));

        if (!request.CustomerId.HasValue && request.NewCustomerDetails == null)
            throw new ArgumentException("Either CustomerId or NewCustomerDetails must be specified.", nameof(request));

        if (request.Items == null || request.Items.Count == 0)
            throw new ArgumentException("Subscription must contain at least 1 item.", nameof(request));

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

        var sw = Stopwatch.StartNew();
        using var response = await _httpClient.SendAsync(httpRequest, ct);
        sw.Stop();

        LogHttpCall("POST", SubscriptionsEndpoint, response.StatusCode, sw.ElapsedMilliseconds);

        if (!response.IsSuccessStatusCode)
        {
            var bodyText = await response.Content.ReadAsStringAsync(ct);
            throw new RekazApiException(response.StatusCode, bodyText);
        }

        var dto = await response.Content.ReadFromJsonAsync<RekazSubscriptionCreatedDto>(JsonOptions, ct)
            ?? throw new RekazApiException(response.StatusCode, "Empty response body on subscription creation.");

        return new RekazSubscriptionCreatedResult(dto.InvoiceId, dto.PaymentLink);
    }

    public async Task<RekazSubscriptionsListResult> GetSubscriptionsAsync(
        RekazSubscriptionsQuery query,
        CancellationToken ct = default)
    {
        var qs = BuildListQueryString(query);
        var requestUri = $"{SubscriptionsEndpoint}{qs}";

        using var httpRequest = new HttpRequestMessage(HttpMethod.Get, requestUri);
        httpRequest.Headers.TryAddWithoutValidation("Accept", "application/json");

        var sw = Stopwatch.StartNew();
        using var response = await _httpClient.SendAsync(httpRequest, ct);
        sw.Stop();

        LogHttpCall("GET", requestUri, response.StatusCode, sw.ElapsedMilliseconds);

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

    public async Task<RekazSubscriptionResult?> GetSubscriptionByIdAsync(
        Guid id,
        CancellationToken ct = default)
    {
        var requestUri = $"{SubscriptionsEndpoint}/{id}";

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

        var dto = await response.Content.ReadFromJsonAsync<RekazSubscriptionDto>(JsonOptions, ct)
            ?? throw new RekazApiException(response.StatusCode, "Empty response body on subscription fetch.");

        return MapSubscription(dto);
    }

    public async Task<List<RekazSubscriptionResult>> GetSubscriptionsByCustomerAsync(
        Guid customerId,
        CancellationToken ct = default)
    {
        var allItems = new List<RekazSubscriptionResult>();
        var skipCount = 0;
        const int pageSize = 100;

        while (true)
        {
            var query = new RekazSubscriptionsQuery(
                CustomerId: customerId,
                MaxResultCount: pageSize,
                SkipCount: skipCount);

            RekazSubscriptionsListResult result;
            try
            {
                result = await GetSubscriptionsAsync(query, ct);
            }
            catch (RekazApiException ex) when (ex.StatusCode == HttpStatusCode.TooManyRequests)
            {
                _logger.LogWarning("Rekaz rate limit hit (429) for CustomerId={CustomerId} at SkipCount={SkipCount}. Retrying after delay.", customerId, skipCount);
                await Task.Delay(TimeSpan.FromMilliseconds(200), ct);
                result = await GetSubscriptionsAsync(query, ct);
            }

            if (result.Items == null || result.Items.Count == 0)
            {
                if (skipCount == 0) break;
            }

            if (result.Items != null && result.Items.Count > 0)
            {
                var mismatched = result.Items.FirstOrDefault(i => i.CustomerId != customerId);
                if (mismatched != null)
                {
                    _logger.LogWarning(
                        "CustomerId filter ignored by Rekaz. Requested CustomerId: {RequestedCustomerId}, but API returned item with CustomerId: {MismatchedCustomerId}",
                        customerId, mismatched.CustomerId);
                    throw new RekazCustomerFilterNotSupportedException(customerId, mismatched.CustomerId);
                }

                allItems.AddRange(result.Items);
            }

            skipCount += pageSize;
            if (skipCount >= result.TotalCount)
                break;

            await Task.Delay(TimeSpan.FromMilliseconds(50), ct);
        }

        return allItems;
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
            parts.AddRange(q.Statuses.Select(s => $"Statuses={Uri.EscapeDataString(s)}"));
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

    internal static RekazSubscriptionResult MapSubscription(RekazSubscriptionDto dto)
    {
        string? name = null;
        var firstItem = dto.Items?.FirstOrDefault();
        if (firstItem?.LocalizedProductName?.OtherLanguages != null)
        {
            if (firstItem.LocalizedProductName.OtherLanguages.TryGetValue("ar", out var arName)
                || firstItem.LocalizedProductName.OtherLanguages.TryGetValue("Ar", out arName))
            {
                name = arName;
            }
        }

        return new(
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
            ResumeAt: dto.ResumeAt,
            Name: name,
            PriceId: firstItem?.PriceId,
            ProductId: firstItem?.ProductId,
            CreationTime: dto.CreationTime != default ? new DateTimeOffset(dto.CreationTime, TimeSpan.Zero) : null,
            LastModificationTime: dto.LastModificationTime.HasValue ? new DateTimeOffset(dto.LastModificationTime.Value, TimeSpan.Zero) : null
        );
    }
}
