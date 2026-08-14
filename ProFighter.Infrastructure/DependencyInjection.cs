using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ProFighter.Application.Common.Interfaces;
using ProFighter.Infrastructure.ExternalServices.Rekaz;
using ProFighter.Infrastructure.Persistence;

namespace ProFighter.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
		services.Configure<RekazOptions>(
			configuration.GetSection(RekazOptions.SectionName));

		services.AddHttpClient("RekazClient", (sp, client) =>
		{
			var options = sp.GetRequiredService<IOptions<RekazOptions>>().Value;
			client.BaseAddress = new Uri(options.BaseUrl);
			client.DefaultRequestHeaders.Add("Authorization", $"Basic {options.ApiKeyBase64}");
			client.DefaultRequestHeaders.Add("__tenant", options.TenantId);
		});

		services.AddHttpClient<IRekazProductsClient, RekazProductsClient>("RekazClient");
		services.AddHttpClient<IRekazCustomersClient, RekazCustomersClient>("RekazClient");
		services.AddHttpClient<IRekazSubscriptionsClient, RekazSubscriptionsClient>("RekazClient");

		// DB Context & Transactions
		services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<AppDbContext>());
		services.AddScoped<IUnitOfWork, Persistence.UnitOfWork>();

		// Provisioning & Email Services
		services.AddScoped<ICustomerProvisioningService, Identity.CustomerProvisioningService>();
		services.AddScoped<INotificationEmailService, Services.NotificationEmailService>();

		return services;
    }
}
