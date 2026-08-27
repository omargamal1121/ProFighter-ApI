using Hangfire;
using ProFighter.Infrastructure;
using ProFighter.Infrastructure.Auth;
using ProFighter.Application;
using ProFighter.API.Middleware;
using Serilog;

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

			builder.Services.AddApplication();
			builder.Services.AddInfrastructure(builder.Configuration);

			// JWT Authentication (must come after AddInfrastructure so Identity is registered)
			builder.Services.AddJwtAuthentication(builder.Configuration);

			// Register Global Exception Middleware (required since it implements IMiddleware)
			builder.Services.AddScoped<GlobalExceptionMiddleware>();

			var app = builder.Build();

			// Global Exception Middleware - must be first in the pipeline
			app.UseMiddleware<GlobalExceptionMiddleware>();

			app.UseSwagger();
			app.UseSwaggerUI();

			app.UseHttpsRedirection();

			// Order matters: Authentication before Authorization
			app.UseAuthentication();
			app.UseAuthorization();

			// Hangfire Dashboard (requires authentication in production)
			app.UseHangfireDashboard();

			app.MapControllers();

			app.Run();
		}
	}
}
