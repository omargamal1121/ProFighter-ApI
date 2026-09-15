using Microsoft.AspNetCore.Identity;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ProFighter.Application.Common.Interfaces;
using ProFighter.Application.Common.Interfaces.Auth;
using ProFighter.Application.Subscriptions.Services;
using ProFighter.Infrastructure.Auth.Services;
using ProFighter.Infrastructure.Caching;
using ProFighter.Infrastructure.ExternalServices.Rekaz;
using ProFighter.Infrastructure.Identity;
using ProFighter.Infrastructure.Persistence;
using Hangfire.MySql;
using Google.Apis.Auth.OAuth2;

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

        // ── Per-gym multi-tenant options ──────────────────────────────────────────
        // Bound to the same "Rekaz" section.
        // Keys must be set via environment variables or user-secrets, e.g.:
        //   Rekaz__ProFighter__ApiKeyBase64   (never in appsettings.json)
        //   Rekaz__ProFighter__TenantId
        //   Rekaz__ProGym__ApiKeyBase64
        //   Rekaz__ProGym__TenantId
        services.Configure<RekazMultiGymOptions>(
            configuration.GetSection(RekazOptions.SectionName));

        // ── Existing named client (ProFighter key) — kept for backwards compat ────
        // Used by webhook handlers, subscription creation, etc. that are still
        // implicitly ProFighter-only.
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
        services.AddHttpClient<IRekazTransactionsClient, RekazTransactionsClient>("RekazClient");

        // ── Base client for factory (base URL only, no auth — factory stamps per-gym headers) ──
        services.AddHttpClient("RekazClientBase", (sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<RekazOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl);
        });

        // IRekazClientFactory — resolves gym-specific Rekaz clients at runtime.
        services.AddScoped<IRekazClientFactory, RekazClientFactory>();

        // IGymSettingsService — provides per-gym config (e.g. BranchId) to Application layer.
        services.AddScoped<IGymSettingsService, GymSettingsService>();

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
		  TransactionTimeout = TimeSpan.FromMinutes(1),
		  QueuePollInterval = TimeSpan.FromSeconds(30),
		  JobExpirationCheckInterval = TimeSpan.FromHours(1),
		  CountersAggregateInterval = TimeSpan.FromMinutes(5),
		  PrepareSchemaIfNecessary = true,
		  DashboardJobListLimit = 500,
		
	  })));

		services.AddHangfireServer(options =>
        {
            options.WorkerCount = 4;
            options.Queues = new[] { "critical", "email", "default" };
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

        // AI Services
        services.AddScoped<IAiPlanService, Services.AiPlanService>();

        // Firebase Push Notifications
        var firebaseCredentialsFileName = configuration["Firebase:CredentialsFileName"];
        if (!string.IsNullOrEmpty(firebaseCredentialsFileName))
        {
            var path = Path.Combine(Directory.GetCurrentDirectory(), "Secrets", firebaseCredentialsFileName);
            if (File.Exists(path) && FirebaseAdmin.FirebaseApp.DefaultInstance == null)
            {
                FirebaseAdmin.FirebaseApp.Create(new FirebaseAdmin.AppOptions()
                {
					Credential = Google.Apis.Auth.OAuth2.GoogleCredential.FromFile(path)

				});
            }
        }
        services.AddScoped<INotificationService, Services.FirebaseNotificationService>();

        // Webhook Processing
        services.AddScoped<IRekazSubscriptionEventHandler, ProFighter.Application.Subscriptions.Services.RekazSubscriptionEventHandler>();
        services.AddScoped<IRekazTransactionEventHandler, RekazTransactionEventHandler>();
        services.AddScoped<IRekazWebhookProcessor, ProFighter.Application.Subscriptions.Services.RekazWebhookProcessor>();

        // Cloudinary Image Storage
        services.Configure<ExternalServices.Cloudinary.CloudinarySettings>(
            configuration.GetSection(ExternalServices.Cloudinary.CloudinarySettings.SectionName));

        services.AddSingleton(sp =>
        {
            var options = sp.GetRequiredService<IOptions<ExternalServices.Cloudinary.CloudinarySettings>>().Value;
            var account = new CloudinaryDotNet.Account(options.CloudName, options.ApiKey, options.ApiSecret);
            return new CloudinaryDotNet.Cloudinary(account);
        });

        services.AddScoped<IImageService, ExternalServices.Cloudinary.CloudinaryImageService>();

        return services;
    }
}
