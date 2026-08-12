using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ProFighter.Application.Common.Interfaces;
using ProFighter.Infrastructure.ExternalServices.Rekaz;

namespace ProFighter.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
    
        services.Configure<RekazOptions>(
            configuration.GetSection(RekazOptions.SectionName));

        var rekazBaseUrl = configuration[$"{RekazOptions.SectionName}:BaseUrl"]
                           ?? "https://platform.rekaz.io";

        services.AddHttpClient<IRekazProductsClient, RekazProductsClient>(client =>
        {
            client.BaseAddress = new Uri(rekazBaseUrl);
        });

        return services;
    }
}

