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
/// Typed HttpClient implementation of <see cref="IRekazCustomersClient"/>.
/// Uses the shared "RekazClient" named HttpClient (base address + auth headers
/// pre-configured in DependencyInjection.cs).
/// </summary>
public sealed class RekazCustomersClient : IRekazCustomersClient
{
    private const string CustomersEndpoint = "/api/public/customers";

    /// <summary>
    /// The only customer type value allowed by the Rekaz public API.
    /// Not exposed to callers — hardcoded here per API docs.
    /// </summary>
    private const int FixedCustomerType = 12;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _httpClient;
    private readonly ILogger<RekazCustomersClient> _logger;

    public RekazCustomersClient(HttpClient httpClient, ILogger<RekazCustomersClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    // -------------------------------------------------------------------------
    // CreateCustomerAsync
    // -------------------------------------------------------------------------

    /// <inheritdoc/>
    public async Task<Guid> CreateCustomerAsync(
        CreateRekazCustomerRequest request,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new ArgumentException("Customer name is required.", nameof(request));

        if (string.IsNullOrWhiteSpace(request.MobileNumber))
            throw new ArgumentException("Customer mobile number is required.", nameof(request));

        _logger.LogInformation("Rekaz CreateCustomer → POST {Endpoint}", CustomersEndpoint);

        // Build the body — "type" is always 12, callers cannot override it.
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

        using var response = await _httpClient.SendAsync(httpRequest, ct);

        _logger.LogInformation(
            "Rekaz CreateCustomer ← {StatusCode} ({StatusCodeInt})",
            response.StatusCode, (int)response.StatusCode);

        if (!response.IsSuccessStatusCode)
        {
            var body2 = await response.Content.ReadAsStringAsync(ct);
            throw new RekazApiException(response.StatusCode, body2);
        }

        var dto = await response.Content.ReadFromJsonAsync<RekazCustomerCreatedDto>(JsonOptions, ct)
            ?? throw new RekazApiException(response.StatusCode, "Empty response body on customer creation.");

        return dto.CustomerId;
    }

    // -------------------------------------------------------------------------
    // GetCustomersAsync
    // -------------------------------------------------------------------------

    /// <inheritdoc/>
    public async Task<RekazCustomersListResult> GetCustomersAsync(
        RekazCustomersQuery query,
        CancellationToken ct = default)
    {
        var qs = BuildListQueryString(query);
        var requestUri = $"{CustomersEndpoint}{qs}";

        _logger.LogInformation("Rekaz GetCustomers → GET {Endpoint}{Query}", CustomersEndpoint, qs);

        using var httpRequest = new HttpRequestMessage(HttpMethod.Get, requestUri);
        httpRequest.Headers.TryAddWithoutValidation("Accept", "application/json");

        using var response = await _httpClient.SendAsync(httpRequest, ct);

        _logger.LogInformation(
            "Rekaz GetCustomers ← {StatusCode} ({StatusCodeInt})",
            response.StatusCode, (int)response.StatusCode);

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

    // -------------------------------------------------------------------------
    // GetCustomerByIdAsync
    // -------------------------------------------------------------------------

    /// <inheritdoc/>
    public async Task<RekazCustomerResult?> GetCustomerByIdAsync(
        Guid id,
        CancellationToken ct = default)
    {
        var requestUri = $"{CustomersEndpoint}/{id}";

        _logger.LogInformation("Rekaz GetCustomerById → GET {Endpoint}", requestUri);

        using var httpRequest = new HttpRequestMessage(HttpMethod.Get, requestUri);
        httpRequest.Headers.TryAddWithoutValidation("Accept", "application/json");

        using var response = await _httpClient.SendAsync(httpRequest, ct);

        _logger.LogInformation(
            "Rekaz GetCustomerById ← {StatusCode} ({StatusCodeInt})",
            response.StatusCode, (int)response.StatusCode);

        // 404 is an expected "not found" case — return null instead of throwing.
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

    // -------------------------------------------------------------------------
    // Private helpers
    // -------------------------------------------------------------------------

    /// <summary>
    /// Builds the query string for GET /api/public/customers.
    /// MaxResultCount is clamped to the valid range 1–100.
    /// </summary>
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

    // Mapping — Infrastructure DTOs → Application models

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
