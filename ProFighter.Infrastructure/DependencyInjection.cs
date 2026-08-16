using Microsoft.AspNetCore.Identity;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ProFighter.Application.Common.Interfaces;
using ProFighter.Application.Common.Interfaces.Auth;
using ProFighter.Infrastructure.Auth.Services;
using ProFighter.Infrastructure.Caching;
using ProFighter.Infrastructure.ExternalServices.Rekaz;
using ProFighter.Infrastructure.Identity;
using ProFighter.Infrastructure.Persistence;
using Hangfire.MySql;

namespace ProFighter.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<RekazOptions>(
            configuration.GetSection(RekazOptions.SectionName));

        services.Configure<RekazWebhookOptions>(
            configuration.GetSection(RekazWebhookOptions.SectionName));

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

        // MySQL DbContext & Identity
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

        services.AddDbContext<AppDbContext>(options =>
            options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString),
                mysqlOptions => mysqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(30),
                    errorNumbersToAdd: null)));

        services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
        {
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireNonAlphanumeric = true;
            options.Password.RequiredLength = 8;
            options.User.RequireUniqueEmail = false;
        })
        .AddEntityFrameworkStores<AppDbContext>()
        .AddDefaultTokenProviders();

		// Hangfire with MySQL storage
		services.AddHangfire(config => config
	  .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
	  .UseSimpleAssemblyNameTypeSerializer()
	  .UseRecommendedSerializerSettings()
	  .UseStorage(new MySqlStorage(connectionString, new MySqlStorageOptions
	  {
		  QueuePollInterval = TimeSpan.FromSeconds(15),
		  JobExpirationCheckInterval = TimeSpan.FromHours(1),
		  CountersAggregateInterval = TimeSpan.FromMinutes(5),
		  PrepareSchemaIfNecessary = true,
		  DashboardJobListLimit = 500,
		
	  })));

		services.AddHangfireServer(options =>
        {
            options.WorkerCount = Environment.ProcessorCount * 2;
            options.Queues = new[] { "default", "critical", "email" };
            options.ServerName = $"ProFighter-{Environment.MachineName}";
        });

        // Memory Cache for first-login tokens
        services.AddMemoryCache();

        // DB Context & Transactions
        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<AppDbContext>());
        services.AddScoped<IUnitOfWork, Persistence.UnitOfWork>();

        // Provisioning & Email Services
        services.AddScoped<ICustomerProvisioningService, Identity.CustomerProvisioningService>();
        services.AddScoped<IEmailConfirmationService, Identity.EmailConfirmationService>();
        services.AddScoped<IPasswordResetService, Identity.PasswordResetService>();
        services.AddScoped<INotificationEmailService, Services.NotificationEmailService>();
        services.AddScoped<IAccountEmailService, Services.AccountEmailService>();
        services.AddScoped<IErrorNotificationService, Services.ErrorNotificationService>();
        services.AddScoped<Microsoft.AspNetCore.Identity.UI.Services.IEmailSender, Services.EmailSender>();

        // Auth Token Services
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IRefreshTokenService, RefreshTokenService>();
        services.AddScoped<IIdentityService, IdentityService>();

        // Authentication Services
        services.AddScoped<IAuthenticationService, Identity.AuthenticationService>();
        services.AddSingleton<IFirstLoginTokenService, Caching.FirstLoginTokenService>();
        services.AddSingleton<IPasswordResetOtpService, Caching.PasswordResetOtpService>();
        services.AddSingleton<IEmailConfirmationOtpService, Caching.EmailConfirmationOtpService>();

        // Webhook Processing
        services.AddScoped<IRekazWebhookProcessor, ProFighter.Application.Subscriptions.Services.RekazWebhookProcessor>();

        return services;
    }
}
