using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
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

    public async Task<Guid> ProvisionFromRekazAsync(
        Guid rekazCustomerId,
        string name,
        string mobileNumber,
        string? email,
        CancellationToken ct = default)
    {
        var normalizedUserName = mobileNumber.StartsWith("+") 
            ? mobileNumber.Substring(1) 
            : mobileNumber;

        var user = await _userManager.FindByNameAsync(normalizedUserName);
        if (user == null)
        {
            var defaultPassword = _configuration["Identity:DefaultLegacyPassword"] 
                ?? throw new InvalidOperationException("Default legacy password 'Identity:DefaultLegacyPassword' is not configured.");

            user = await CreateIdentityUserAsync(normalizedUserName, mobileNumber, email, defaultPassword, mustChangePassword: true, ct);
        }

        var customerExists = await _context.Customers.AnyAsync(c => c.Id == user.Id, ct);
        if (customerExists)
        {
            return user.Id;
        }
    
        var customer = new Customer(user.Id, name, mobileNumber, CustomerSource.LegacyRekazImport, email, rekazCustomerId, isFirstLogin: true);
        _context.Customers.Add(customer);

        return customer.Id;
    }

    public async Task<Guid> ProvisionLocalCustomerWithPasswordAsync(
        Guid rekazCustomerId, string name, string mobileNumber, string? email,
        string password, CustomerSource source, CancellationToken ct = default)
    {
        var normalizedUserName = mobileNumber.StartsWith("+")
            ? mobileNumber.Substring(1)
            : mobileNumber;

        var user = await _userManager.FindByNameAsync(normalizedUserName);
        if (user != null)
        {
            throw new InvalidOperationException($"User with mobile number {mobileNumber} already exists.");
        }

        user = await CreateIdentityUserAsync(normalizedUserName, mobileNumber, email, password, mustChangePassword: false, ct);

        var customer = new Customer(user.Id, name, mobileNumber, email, rekazCustomerId); // EmailRegistration users set their own password, so no first login required
        _context.Customers.Add(customer);

        return customer.Id;
    }

    public async Task<Guid> ProvisionLocalCustomerAsync(
        Guid rekazCustomerId, string name, string mobileNumber, string? email,
        CustomerSource source, CancellationToken ct = default)
    {
        var normalizedUserName = mobileNumber.StartsWith("+")
            ? mobileNumber.Substring(1)
            : mobileNumber;

        var user = await _userManager.FindByNameAsync(normalizedUserName);
        if (user == null)
        {
            var defaultPassword = _configuration["Identity:DefaultLegacyPassword"]
                ?? throw new InvalidOperationException("Default legacy password 'Identity:DefaultLegacyPassword' is not configured.");

            user = await CreateIdentityUserAsync(normalizedUserName, mobileNumber, email, defaultPassword, mustChangePassword: true, ct);
        }

        var customerExists = await _context.Customers.AnyAsync(c => c.Id == user.Id, ct);
        if (customerExists)
        {
            return user.Id;
        }

        var customer = new Customer(user.Id, name, mobileNumber, source, email, rekazCustomerId, isFirstLogin: true);
        _context.Customers.Add(customer);

        return customer.Id;
    }

    private async Task<ApplicationUser> CreateIdentityUserAsync(
        string username,
        string mobileNumber,
        string? email,
        string password,
        bool mustChangePassword,
        CancellationToken ct)
    {
        var user = new ApplicationUser
        {
            UserName = username,
            PhoneNumber = mobileNumber,
            Email = email,
            MustChangePassword = mustChangePassword
        };

        var createResult = await _userManager.CreateAsync(user, password);
        if (!createResult.Succeeded)
        {
            var errors = string.Join("; ", createResult.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Failed to create identity user: {errors}");
        }

        return user;
    }
}
