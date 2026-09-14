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
        GymType gymType = GymType.ProFighter,
        CancellationToken ct = default)
    {
        var generatedUserName = GenerateIdentityUsername(mobileNumber, rekazCustomerId, gymType);
        var user = await _userManager.FindByNameAsync(generatedUserName);

        if (user == null)
        {
            // Fallback for existing legacy users
            var oldUserName = mobileNumber.StartsWith("+") ? mobileNumber.Substring(1) : mobileNumber;
            user = await _userManager.FindByNameAsync(oldUserName);
        }

        if (user == null)
        {
            var defaultPassword = _configuration["Identity:DefaultLegacyPassword"] 
                ?? throw new InvalidOperationException("Default legacy password 'Identity:DefaultLegacyPassword' is not configured.");

            user = await CreateIdentityUserAsync(generatedUserName, mobileNumber, email, defaultPassword, mustChangePassword: true, ct);
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
        string password, CustomerSource source, GymType gymType = GymType.ProFighter, CancellationToken ct = default)
    {
        var generatedUserName = GenerateIdentityUsername(mobileNumber, rekazCustomerId, gymType);
        var user = await _userManager.FindByNameAsync(generatedUserName);

        if (user == null)
        {
            var oldUserName = mobileNumber.StartsWith("+") ? mobileNumber.Substring(1) : mobileNumber;
            user = await _userManager.FindByNameAsync(oldUserName);
        }

        if (user != null)
        {
            throw new InvalidOperationException($"User with mobile number {mobileNumber} already exists.");
        }

        try
        {
            user = await CreateIdentityUserAsync(generatedUserName, mobileNumber, email, password, mustChangePassword: false, ct);
        }
        catch (DbUpdateException ex) when (IsDuplicateUserNameError(ex))
        {
            throw new InvalidOperationException($"User with mobile number {mobileNumber} already exists.", ex);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("already taken") || ex.Message.Contains("DuplicateUserName"))
        {
            throw new InvalidOperationException($"User with mobile number {mobileNumber} already exists.", ex);
        }

        var customer = new Customer(user.Id, name, mobileNumber, email, rekazCustomerId); // EmailRegistration users set their own password, so no first login required
        _context.Customers.Add(customer);

        return customer.Id;
    }

    public async Task<Customer> ProvisionLocalCustomerAsync(
        Guid rekazCustomerId, string name, string mobileNumber, string? email,
        CustomerSource source, GymType gymType = GymType.ProFighter, CancellationToken ct = default)
    {
        var generatedUserName = GenerateIdentityUsername(mobileNumber, rekazCustomerId, gymType);
        var user = await _userManager.FindByNameAsync(generatedUserName);

        if (user == null)
        {
            var oldUserName = mobileNumber.StartsWith("+") ? mobileNumber.Substring(1) : mobileNumber;
            user = await _userManager.FindByNameAsync(oldUserName);
        }

        if (user == null)
        {
            try
            {
                var defaultPassword = _configuration["Identity:DefaultLegacyPassword"]
                    ?? throw new InvalidOperationException("Default legacy password 'Identity:DefaultLegacyPassword' is not configured.");

                user = await CreateIdentityUserAsync(generatedUserName, mobileNumber, email, defaultPassword, mustChangePassword: true, ct);
            }
            catch (DbUpdateException ex) when (IsDuplicateUserNameError(ex))
            {
                user = await _userManager.FindByNameAsync(generatedUserName)
                    ?? throw new InvalidOperationException(
                        $"Duplicate username conflict for {generatedUserName}, but user not found on re-lookup.", ex);
            }
        }

        var existingCustomer = await _context.Customers.FirstOrDefaultAsync(c => c.Id == user.Id, ct);
        if (existingCustomer != null)
        {
            return existingCustomer;
        }

        var customer = new Customer(user.Id, name, mobileNumber, source, email, rekazCustomerId, isFirstLogin: true, gymType: gymType);
        _context.Customers.Add(customer);

        return customer;
    }

    private bool IsDuplicateUserNameError(DbUpdateException ex)
    {
        if (ex.InnerException is MySqlConnector.MySqlException mySqlEx)
        {
            return mySqlEx.Number == 1062;
        }
        return false;
    }

    private string SanitizeMobileNumberForUsername(string mobileNumber)
    {
        if (string.IsNullOrWhiteSpace(mobileNumber)) return string.Empty;
        return new string(mobileNumber.Where(char.IsDigit).ToArray());
    }

    private string GenerateIdentityUsername(string mobileNumber, Guid rekazCustomerId, GymType gymType)
    {
        var sanitized = SanitizeMobileNumberForUsername(mobileNumber);
        var baseUserName = string.IsNullOrWhiteSpace(sanitized) 
            ? $"customer_{rekazCustomerId:N}" 
            : sanitized;

        return $"{baseUserName}_{(int)gymType}";
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
