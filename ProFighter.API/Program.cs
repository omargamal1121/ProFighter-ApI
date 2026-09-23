using Hangfire;
using ProFighter.Infrastructure;
using ProFighter.Infrastructure.Auth;
using ProFighter.Application;
using ProFighter.API.Middleware;
using Serilog;
using Microsoft.AspNetCore.DataProtection;

namespace ProFighter.API
{
	public class Program
	{
		public static async Task Main(string[] args)
		{
			var builder = WebApplication.CreateBuilder(args);

			Log.Logger = new LoggerConfiguration()
				.ReadFrom.Configuration(builder.Configuration)
				.WriteTo.Console()
				.WriteTo.File(
					path: "Logs/log-.txt",
					rollingInterval: RollingInterval.Day,
					retainedFileCountLimit: 14,
					outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
				.CreateLogger();

			builder.Host.UseSerilog();

			// Add services to the container.

			builder.Services.AddControllers();
			builder.Services.AddEndpointsApiExplorer();
			builder.Services.AddSwaggerGen();

			builder.Services.AddCors(options =>
			{
				options.AddPolicy("AllowAll", policy =>
				{
					policy.AllowAnyOrigin()
						  .AllowAnyHeader()
						  .AllowAnyMethod();
				});
			});

			builder.Services.AddApplication();
			builder.Services.AddInfrastructure(builder.Configuration);

			builder.Services.AddHttpContextAccessor();
			builder.Services.AddScoped<ProFighter.Application.Common.Interfaces.ICurrentGymContext, ProFighter.API.Services.CurrentGymContext>();

			// Configure Data Protection to persist keys to a durable file system location
			builder.Services.AddDataProtection()
				.PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(builder.Environment.ContentRootPath, "keys")))
				.SetApplicationName("ProFighter");

			// JWT Authentication (must come after AddInfrastructure so Identity is registered)
			builder.Services.AddJwtAuthentication(builder.Configuration);

			// Register Global Exception Middleware (required since it implements IMiddleware)
			builder.Services.AddScoped<GlobalExceptionMiddleware>();

			builder.Services.AddRateLimiter(options =>
			{
				// AI plan endpoint: 100 req/min per user or IP
				options.AddPolicy("AiPlanPolicy", context =>
				{
					var userId = context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
						?? context.Connection.RemoteIpAddress?.ToString()
						?? "anonymous";
					return System.Threading.RateLimiting.RateLimitPartition.GetFixedWindowLimiter(userId,
						partition => new System.Threading.RateLimiting.FixedWindowRateLimiterOptions
						{
							AutoReplenishment = true,
							PermitLimit = 100,
							QueueLimit = 0,
							Window = TimeSpan.FromMinutes(1)
						});
				});

				// Strict auth policy: 5 req/min per IP — OTP submission, forgot-password, confirm-email
				options.AddPolicy("AuthStrictPolicy", context =>
				{
					var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
					return System.Threading.RateLimiting.RateLimitPartition.GetFixedWindowLimiter(ip,
						partition => new System.Threading.RateLimiting.FixedWindowRateLimiterOptions
						{
							AutoReplenishment = true,
							PermitLimit = 5,
							QueueLimit = 0,
							Window = TimeSpan.FromMinutes(1)
						});
				});

				// General auth policy: 20 req/min per IP — login, register, request-email-confirmation
				options.AddPolicy("AuthGeneralPolicy", context =>
				{
					var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
					return System.Threading.RateLimiting.RateLimitPartition.GetFixedWindowLimiter(ip,
						partition => new System.Threading.RateLimiting.FixedWindowRateLimiterOptions
						{
							AutoReplenishment = true,
							PermitLimit = 20,
							QueueLimit = 0,
							Window = TimeSpan.FromMinutes(1)
						});
				});

				// General API policy: 120 req/min per user or IP — all other endpoints
				options.AddPolicy("GeneralApiPolicy", context =>
				{
					var userId = context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
						?? context.Connection.RemoteIpAddress?.ToString()
						?? "anonymous";
					return System.Threading.RateLimiting.RateLimitPartition.GetFixedWindowLimiter(userId,
						partition => new System.Threading.RateLimiting.FixedWindowRateLimiterOptions
						{
							AutoReplenishment = true,
							PermitLimit = 120,
							QueueLimit = 0,
							Window = TimeSpan.FromMinutes(1)
						});
				});

				options.RejectionStatusCode = 429;
			});

			var app = builder.Build();

			// Global Exception Middleware - must be first in the pipeline
			app.UseMiddleware<GlobalExceptionMiddleware>();

			app.UseSwagger();
			app.UseSwaggerUI();

			app.UseHttpsRedirection();

			app.UseCors("AllowAll");

			// Order matters: Authentication before Authorization
			app.UseAuthentication();
			app.UseAuthorization();
			app.UseMiddleware<GymTypeValidationMiddleware>();
			app.UseRateLimiter();

			// Hangfire Dashboard (configured for public access)
			app.UseHangfireDashboard("/hangfire", new DashboardOptions
			{
				Authorization = new[] { new PublicHangfireDashboardAuthorizationFilter() }
			});

			app.MapControllers();

			// Register recurring jobs
			RecurringJob.AddOrUpdate<ProFighter.Application.Subscriptions.Jobs.SubscriptionExpiryReminderJob>(
				"SubscriptionExpiryReminderJob",
				job => job.RunAsync(CancellationToken.None),
				Cron.Daily);

			
			var egyptTz = TimeZoneInfo.FindSystemTimeZoneById("Egypt Standard Time");

			RecurringJob.AddOrUpdate<ProFighter.Application.Sync.Jobs.GymDataSyncJob>(
				"gym-sync-profighter-customers",
				job => job.SyncProFighterCustomersAsync(CancellationToken.None),
				"0 2 * * *",
				new RecurringJobOptions { TimeZone = egyptTz });

			RecurringJob.AddOrUpdate<ProFighter.Application.Sync.Jobs.GymDataSyncJob>(
				"gym-sync-profighter-subscriptions",
				job => job.SyncProFighterSubscriptionsAsync(CancellationToken.None),
				"30 2 * * *",
				new RecurringJobOptions { TimeZone = egyptTz });

			RecurringJob.AddOrUpdate<ProFighter.Application.Sync.Jobs.GymDataSyncJob>(
				"gym-sync-progym-customers",
				job => job.SyncProGymCustomersAsync(CancellationToken.None),
				"0 3 * * *",
				new RecurringJobOptions { TimeZone = egyptTz });

			RecurringJob.AddOrUpdate<ProFighter.Application.Sync.Jobs.GymDataSyncJob>(
				"gym-sync-progym-subscriptions",
				job => job.SyncProGymSubscriptionsAsync(CancellationToken.None),
				"30 3 * * *",
				new RecurringJobOptions { TimeZone = egyptTz });

			
			using (var scope = app.Services.CreateScope())
			{
				try
				{
				
					await ProFighter.Infrastructure.Persistence.Seed.DbSeeder.SeedAdminUserAsync(scope.ServiceProvider);
				}
				catch (Exception ex)
				{
					Log.Error(ex, "Failed to apply database migrations on startup.");
				}
			}

			app.Run();
		}
	}
}

public class PublicHangfireDashboardAuthorizationFilter : Hangfire.Dashboard.IDashboardAuthorizationFilter
{
    public bool Authorize(Hangfire.Dashboard.DashboardContext context)
    {
        return true;
    }
}

