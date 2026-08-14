using ProFighter.Infrastructure;
using ProFighter.Application;

namespace ProFighter.API
{
	public class Program
	{
		public static void Main(string[] args)
		{
			var builder = WebApplication.CreateBuilder(args);

			// Add services to the container.

			builder.Services.AddControllers();
			// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
			builder.Services.AddEndpointsApiExplorer();
			builder.Services.AddSwaggerGen();

			builder.Services.AddApplication();
			builder.Services.AddInfrastructure(builder.Configuration);

			var app = builder.Build();

			
		
			app.UseSwagger();
			app.UseSwaggerUI();
			
			app.UseHttpsRedirection();

			app.UseAuthorization();


			app.MapControllers();

			app.Run();
		}
	}
}
