using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NSubstitute;
using ProFighter.Application.Common.Models;
using ProFighter.Domain.Entities;
using ProFighter.Domain.Enums;
using ProFighter.Infrastructure.Identity;
using ProFighter.Infrastructure.Persistence;
using Xunit;

namespace ProFighter.Infrastructure.Tests.Identity;

public class CustomerProvisioningServiceRaceTests
{
    private static AppDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private static UserManager<ApplicationUser> CreateMockUserManager()
    {
        var store = Substitute.For<IUserStore<ApplicationUser>>();
        var mgr = Substitute.For<UserManager<ApplicationUser>>(
            store, null, null, null, null, null, null, null, null);
        return mgr;
    }

    [Fact]
    public async Task ProvisionLocalCustomerAsync_WhenIdentityUserCreationFailsWithDuplicateUserName_RecoversExistingUserAndCustomer()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var userManager = CreateMockUserManager();
        var configuration = Substitute.For<IConfiguration>();
        configuration["Identity:DefaultLegacyPassword"].Returns("DefaultPass123!");
        var logger = Substitute.For<ILogger<CustomerProvisioningService>>();

        var rekazCustomerId = Guid.NewGuid();
        var mobileNumber = "+966500000000";
        var expectedUserName = "966500000000_0";

        // CreateAsync throws InvalidOperationException with "already taken"
        userManager.CreateAsync(Arg.Any<ApplicationUser>(), Arg.Any<string>())
            .Returns(Task.FromResult(IdentityResult.Failed(new IdentityError
            {
                Code = "DuplicateUserName",
                Description = $"Username '{expectedUserName}' is already taken."
            })));

        var existingUser = new ApplicationUser { Id = Guid.NewGuid(), UserName = expectedUserName, PhoneNumber = mobileNumber };
        
        int callCount = 0;
        userManager.FindByNameAsync(expectedUserName).Returns(_ => 
        {
            callCount++;
            return callCount == 1 ? null : existingUser;
        });

        var existingCustomer = new Customer(existingUser.Id, "John Parallel", mobileNumber, CustomerSource.LegacyRekazImport, rekazCustomerId: rekazCustomerId);
        context.Customers.Add(existingCustomer);
        await context.SaveChangesAsync();

        var service = new CustomerProvisioningService(userManager, context, configuration, logger);

        // Act
        var customer = await service.ProvisionLocalCustomerAsync(rekazCustomerId, "John Parallel", mobileNumber, "john@example.com", CustomerSource.LegacyRekazImport, GymType.ProFighter);

        // Assert
        Assert.NotNull(customer);
        Assert.Equal(existingUser.Id, customer.Id);
        Assert.Equal("John Parallel", customer.Name);
    }
}
