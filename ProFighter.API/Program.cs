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
		public static void Main(string[] args)
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
				options.AddPolicy("AiPlanPolicy", context =>
				{
					// Rate limit per authenticated user id, fallback to IP if not authenticated
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
			app.UseRateLimiter();

			// Hangfire Dashboard (requires authentication in production)
			app.UseHangfireDashboard();

			app.MapControllers();

			// Register recurring jobs
			RecurringJob.AddOrUpdate<ProFighter.Application.Subscriptions.Jobs.SubscriptionExpiryReminderJob>(
				"SubscriptionExpiryReminderJob",
				job => job.RunAsync(CancellationToken.None),
				Cron.Daily);

			// Automatically apply pending database migrations to the hosted database on startup
			using (var scope = app.Services.CreateScope())
			{
				try
				{
					var db = scope.ServiceProvider.GetRequiredService<ProFighter.Infrastructure.Persistence.AppDbContext>();
					Microsoft.EntityFrameworkCore.RelationalDatabaseFacadeExtensions.Migrate(db.Database);
					Log.Information("Database migrations applied successfully.");
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
