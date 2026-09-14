using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ProFighter.Application.Common.Interfaces;
using ProFighter.Domain.Enums;

namespace ProFighter.Infrastructure.ExternalServices.Rekaz;


public sealed class RekazClientFactory : IRekazClientFactory
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly RekazMultiGymOptions _options;
    private readonly ILoggerFactory _loggerFactory;

    public RekazClientFactory(
        IHttpClientFactory httpClientFactory,
        IOptions<RekazMultiGymOptions> options,
        ILoggerFactory loggerFactory)
    {
        _httpClientFactory = httpClientFactory;
        _options           = options.Value;
        _loggerFactory     = loggerFactory;
    }

    /// <inheritdoc/>
    public IRekazClient GetClient(GymType gymType)
    {
        var credentials = gymType switch
        {
            GymType.ProFighter => _options.ProFighter
                ?? throw new InvalidOperationException(
                    "Rekaz API key for GymType.ProFighter is not configured. " +
                    "Set Rekaz:ProFighter:ApiKeyBase64 and Rekaz:ProFighter:TenantId " +
                    "via environment variables or user-secrets."),

            GymType.ProGym => _options.ProGym
                ?? throw new InvalidOperationException(
                    "Rekaz API key for GymType.ProGym is not configured. " +
                    "Set Rekaz:ProGym:ApiKeyBase64 and Rekaz:ProGym:TenantId " +
                    "via environment variables or user-secrets."),

            _ => throw new ArgumentOutOfRangeException(nameof(gymType),
                    $"No Rekaz credentials are configured for GymType '{gymType}'.")
        };

        if (string.IsNullOrWhiteSpace(credentials.ApiKeyBase64))
            throw new InvalidOperationException(
                $"Rekaz:{{gymType}}:ApiKeyBase64 is empty for GymType '{gymType}'. " +
                "Set it via an environment variable or user-secret.");

        if (string.IsNullOrWhiteSpace(credentials.TenantId))
            throw new InvalidOperationException(
                $"Rekaz:{{gymType}}:TenantId is empty for GymType '{gymType}'. " +
                "Set it via an environment variable or user-secret.");

  var httpClient = _httpClientFactory.CreateClient("RekazClientBase");

         httpClient.DefaultRequestHeaders.Remove("Authorization");
        httpClient.DefaultRequestHeaders.Remove("__tenant");
        httpClient.DefaultRequestHeaders.Add("Authorization", $"Basic {credentials.ApiKeyBase64}");
        httpClient.DefaultRequestHeaders.Add("__tenant", credentials.TenantId);

        var customersClient = new RekazCustomersClient(
            httpClient,
            _loggerFactory.CreateLogger<RekazCustomersClient>());

        var subscriptionsClient = new RekazSubscriptionsClient(
            httpClient,
            _loggerFactory.CreateLogger<RekazSubscriptionsClient>());

        var productsClient = new RekazProductsClient(
            httpClient,
            _loggerFactory.CreateLogger<RekazProductsClient>());

        var transactionsClient = new RekazTransactionsClient(
            httpClient,
            _loggerFactory.CreateLogger<RekazTransactionsClient>());

        return new RekazClientBundle(customersClient, subscriptionsClient, productsClient, transactionsClient);
    }
}
