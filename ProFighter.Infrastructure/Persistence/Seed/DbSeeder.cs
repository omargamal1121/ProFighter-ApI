using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ProFighter.Domain.Entities;
using ProFighter.Domain.Enums;
using ProFighter.Infrastructure.Identity;

namespace ProFighter.Infrastructure.Persistence.Seed;

public static class DbSeeder
{
    public static async Task SeedAdminUserAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var services = scope.ServiceProvider;

        var logger = services.GetRequiredService<ILogger<AppDbContext>>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = services.GetRequiredService<RoleManager<ApplicationRole>>();
        var dbContext = services.GetRequiredService<AppDbContext>();
        var configuration = services.GetRequiredService<IConfiguration>();

        try
        {
            // 1. Seed Admin Role
            const string adminRoleName = "Admin";
            if (!await roleManager.RoleExistsAsync(adminRoleName))
            {
                var roleResult = await roleManager.CreateAsync(new ApplicationRole { Name = adminRoleName });
                if (roleResult.Succeeded)
                {
                    logger.LogInformation("Role '{RoleName}' created successfully.", adminRoleName);
                }
                else
                {
                    logger.LogError("Failed to create role '{RoleName}': {Errors}", 
                        adminRoleName, string.Join("; ", roleResult.Errors.Select(e => e.Description)));
                }
            }

            // 2. Read Seed Configuration from seed.json if present, otherwise defaults
            string adminMobile = "+966549338795";
            string adminName = "System Admin";
            string adminEmail = "admin@profighter.com";

            var seedFilePath = Path.Combine(AppContext.BaseDirectory, "Persistence", "Seed", "seed.json");
            if (!File.Exists(seedFilePath))
            {
                // Fallback to source root path
                seedFilePath = Path.Combine(Directory.GetCurrentDirectory(), "ProFighter.Infrastructure", "Persistence", "Seed", "seed.json");
            }

            if (File.Exists(seedFilePath))
            {
                try
                {
                    var jsonString = await File.ReadAllTextAsync(seedFilePath);
                    using var doc = JsonDocument.Parse(jsonString);
                    if (doc.RootElement.TryGetProperty("AdminUser", out var adminProp))
                    {
                        if (adminProp.TryGetProperty("MobileNumber", out var mob) && !string.IsNullOrWhiteSpace(mob.GetString()))
                            adminMobile = mob.GetString()!;
                        if (adminProp.TryGetProperty("Name", out var nm) && !string.IsNullOrWhiteSpace(nm.GetString()))
                            adminName = nm.GetString()!;
                        if (adminProp.TryGetProperty("Email", out var em) && !string.IsNullOrWhiteSpace(em.GetString()))
                            adminEmail = em.GetString()!;
                    }
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Could not parse seed.json. Using fallback default values.");
                }
            }

            // 3. Check if Admin Customer or Identity User already exists
            var existingCustomer = await dbContext.Customers
                .FirstOrDefaultAsync(c => c.MobileNumber == adminMobile);

            var sanitizedDigits = new string(adminMobile.Where(char.IsDigit).ToArray());
            var username = $"{sanitizedDigits}_0"; // GymType.ProFighter = 0

            var existingUser = await userManager.FindByNameAsync(username)
                ?? await userManager.FindByEmailAsync(adminEmail);

            if (existingCustomer == null && existingUser == null)
            {
                var defaultPassword = configuration["Identity:DefaultLegacyPassword"] ?? "Rekaz@Default123!";

                var newAdminUser = new ApplicationUser
                {
                    UserName = username,
                    PhoneNumber = adminMobile,
                    Email = adminEmail,
                    MustChangePassword = true
                };

                var createResult = await userManager.CreateAsync(newAdminUser, defaultPassword);
                if (!createResult.Succeeded)
                {
                    logger.LogError("Failed to create admin Identity user: {Errors}",
                        string.Join("; ", createResult.Errors.Select(e => e.Description)));
                    return;
                }

                await userManager.AddToRoleAsync(newAdminUser, adminRoleName);

                var customer = new Customer(
                    id: newAdminUser.Id,
                    name: adminName,
                    mobileNumber: adminMobile,
                    source: CustomerSource.AdminAdded,
                    email: adminEmail,
                    rekazCustomerId: null,
                    isFirstLogin: true,
                    gymType: GymType.ProFighter);

                dbContext.Customers.Add(customer);
                await dbContext.SaveChangesAsync();

                logger.LogInformation("Admin user {MobileNumber} seeded successfully.", adminMobile);
            }
            else
            {
                // Ensure existing user has Admin role
                var targetUser = existingUser ?? await userManager.FindByIdAsync(existingCustomer!.Id.ToString());
                if (targetUser != null)
                {
                    if (!await userManager.IsInRoleAsync(targetUser, adminRoleName))
                    {
                        await userManager.AddToRoleAsync(targetUser, adminRoleName);
                        logger.LogInformation("Added role '{RoleName}' to existing user {UserId}.", adminRoleName, targetUser.Id);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while seeding admin user.");
        }
    }
}
