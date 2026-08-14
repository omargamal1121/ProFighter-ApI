using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using ProFighter.Application.Common.Interfaces;
using ProFighter.Domain.Entities;
using ProFighter.Domain.Enums;

namespace ProFighter.Infrastructure.Identity;

public class CustomerProvisioningService : ICustomerProvisioningService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IApplicationDbContext _context;
    private readonly IConfiguration _configuration;

    public CustomerProvisioningService(
        UserManager<ApplicationUser> userManager,
        IApplicationDbContext context,
        IConfiguration configuration)
    {
        _userManager = userManager;
        _context = context;
        _configuration = configuration;
    }

    public async Task<Guid> ProvisionLocalCustomerAsync(
        Guid rekazCustomerId,
        string name,
        string mobileNumber,
        string? email,
        CustomerSource source,
        CancellationToken ct = default)
    {
        // Normalize mobileNumber for UserName: strip leading "+" if present
        var normalizedUserName = mobileNumber.StartsWith("+") 
            ? mobileNumber.Substring(1) 
            : mobileNumber;

        var defaultPassword = _configuration["Identity:DefaultLegacyPassword"] 
            ?? throw new InvalidOperationException("Default legacy password 'Identity:DefaultLegacyPassword' is not configured.");

        // Check if user already exists
        var user = await _userManager.FindByNameAsync(normalizedUserName);
        if (user == null)
        {
            user = new ApplicationUser
            {
                UserName = normalizedUserName,
                PhoneNumber = mobileNumber,
                Email = email
            };

            var createResult = await _userManager.CreateAsync(user, defaultPassword);
            if (!createResult.Succeeded)
            {
                var errors = string.Join("; ", createResult.Errors.Select(e => e.Description));
                throw new InvalidOperationException($"Failed to create identity user: {errors}");
            }
        }

        // Shared primary key pattern: Customer.Id == ApplicationUser.Id
        var customer = new Customer(user.Id, name, mobileNumber, source, email, rekazCustomerId);

        _context.Customers.Add(customer);

        return customer.Id;
    }
}
