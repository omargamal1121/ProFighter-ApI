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

public sealed class RekazCustomersClient : IRekazCustomersClient
{
    private const string CustomersEndpoint = "/api/public/customers";
    private const int FixedCustomerType = 1;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _httpClient;
    private readonly ILogger<RekazCustomersClient> _logger;

    public RekazCustomersClient(HttpClient httpClient, ILogger<RekazCustomersClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<Guid> CreateCustomerAsync(
        CreateRekazCustomerRequest request,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new ArgumentException("Customer name is required.", nameof(request));

        if (string.IsNullOrWhiteSpace(request.MobileNumber))
            throw new ArgumentException("Customer mobile number is required.", nameof(request));

        var body = new
        {
            name         = request.Name,
            mobileNumber = request.MobileNumber,
            email        = request.Email,
            address      = request.Address,
            type         = FixedCustomerType,
            vatNumber    = request.VatNumber,
            branchId     = request.BranchId,
            companyName  = request.CompanyName,
            customFields = request.CustomFields,
            birthDate    = request.BirthDate?.ToString("yyyy-MM-dd"),
        };

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, CustomersEndpoint)
        {
            Content = JsonContent.Create(body, options: JsonOptions)
        };
        httpRequest.Headers.TryAddWithoutValidation("Accept", "application/json");

        var sw = Stopwatch.StartNew();
        using var response = await _httpClient.SendAsync(httpRequest, ct);
        sw.Stop();

        LogHttpCall("POST", CustomersEndpoint, response.StatusCode, sw.ElapsedMilliseconds);

        if (!response.IsSuccessStatusCode)
        {
            var body2 = await response.Content.ReadAsStringAsync(ct);
            throw new RekazApiException(response.StatusCode, body2);
        }

        var dto = await response.Content.ReadFromJsonAsync<RekazCustomerCreatedDto>(JsonOptions, ct)
            ?? throw new RekazApiException(response.StatusCode, "Empty response body on customer creation.");

        return dto.CustomerId;
    }

    public async Task<RekazCustomersListResult> GetCustomersAsync(
        RekazCustomersQuery query,
        CancellationToken ct = default)
    {
        var qs = BuildListQueryString(query);
        var requestUri = $"{CustomersEndpoint}{qs}";

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

        var dto = await response.Content.ReadFromJsonAsync<RekazCustomersListResponse>(JsonOptions, ct)
            ?? throw new RekazApiException(response.StatusCode, "Empty response body on customer list.");

        return new RekazCustomersListResult(
            Items: dto.Items.Select(MapCustomer).ToList(),
            TotalCount: dto.TotalCount
        );
    }

    public async Task<RekazCustomerResult?> GetCustomerByIdAsync(
        Guid id,
        CancellationToken ct = default)
    {
        var requestUri = $"{CustomersEndpoint}/{id}";

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

        var dto = await response.Content.ReadFromJsonAsync<RekazCustomerDto>(JsonOptions, ct)
            ?? throw new RekazApiException(response.StatusCode, "Empty response body on customer fetch.");

        return MapCustomer(dto);
    }

    public async Task<RekazCustomerResult?> GetCustomerByMobileNumberAsync(
        string mobileNumber,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(mobileNumber))
            throw new ArgumentException("Mobile number is required.", nameof(mobileNumber));

        var list = await GetCustomersAsync(new RekazCustomersQuery(
            SkipCount: 0,
            MaxResultCount: 1,
            MobileNumber: mobileNumber
        ), ct);

        return list.Items.FirstOrDefault();
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

    private static string BuildListQueryString(RekazCustomersQuery q)
    {
        var clampedMax = Math.Clamp(q.MaxResultCount, 1, 100);

        var parts = new List<string>
        {
            $"SkipCount={q.SkipCount}",
            $"MaxResultCount={clampedMax}",
        };

        if (!string.IsNullOrWhiteSpace(q.MobileNumber))
            parts.Add($"MobileNumber={Uri.EscapeDataString(q.MobileNumber)}");

        if (!string.IsNullOrWhiteSpace(q.Sorting))
            parts.Add($"Sorting={Uri.EscapeDataString(q.Sorting)}");

        return "?" + string.Join("&", parts);
    }

    private static RekazCustomerResult MapCustomer(RekazCustomerDto dto) =>
        new(
            Id:             dto.Id,
            Name:           dto.Name,
            CustomerNumber: dto.CustomerNumber,
            MobileNumber:   dto.MobileNumber,
            Email:          dto.Email,
            Address:        dto.Address,
            CompanyName:    dto.CompanyName,
            IsBlocked:      dto.IsBlocked
        );
}
