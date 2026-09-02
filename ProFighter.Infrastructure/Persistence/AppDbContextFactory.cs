using System;
using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace ProFighter.Infrastructure.Persistence;

public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        // 1. Try to read connection string from configuration or environment variables
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";

        var builder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile($"appsettings.{environment}.json", optional: true)
            .AddEnvironmentVariables();

        var configuration = builder.Build();

        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? Environment.GetEnvironmentVariable("DefaultConnection");

        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();

        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            // If a real/hosted connection string is provided, use it
            optionsBuilder.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
        }
        else
        {
            // Fallback dummy for offline design-time migrations scaffolding
            var serverVersion = new MySqlServerVersion(new Version(8, 0, 32));
            optionsBuilder.UseMySql("Server=localhost;Database=profighter;Uid=root;Pwd=;", serverVersion);
        }

        return new AppDbContext(optionsBuilder.Options);
    }
}
