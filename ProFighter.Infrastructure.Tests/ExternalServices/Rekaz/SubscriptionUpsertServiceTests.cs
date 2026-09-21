using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;
using ProFighter.Application.Common.Interfaces;
using ProFighter.Application.Common.Models;
using ProFighter.Application.Subscriptions.Services;
using ProFighter.Domain.Entities;
using ProFighter.Domain.Enums;
using ProFighter.Infrastructure.Persistence;
using Xunit;

namespace ProFighter.Infrastructure.Tests.ExternalServices.Rekaz;

public class SubscriptionUpsertServiceTests
{
    private static AppDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task UpsertSubscriptionAsync_WhenTotalAmountNegative_ReturnsSkipped()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var customerSync = Substitute.For<IRekazCustomerSyncService>();
        var logger = Substitute.For<ILogger<SubscriptionUpsertService>>();

        var service = new SubscriptionUpsertService(context, customerSync, logger);

        var rekazSub = new RekazSubscriptionResult(
            Id: Guid.NewGuid(),
            SubscriptionCode: "SUB-NEG",
            CustomerId: Guid.NewGuid(),
            StartAt: DateTime.UtcNow,
            EndAt: DateTime.UtcNow.AddMonths(1),
            Status: "Active",
            PaidAmount: -50m,
            TotalAmount: -50m,
            RemainingAmount: 0m,
            IsPaused: false,
            PausedAt: null,
            ResumeAt: null
        );

        // Act
        var result = await service.UpsertSubscriptionAsync(rekazSub, GymType.ProFighter);

        // Assert
        Assert.Equal(SubscriptionUpsertResult.Skipped, result);
    }

    [Fact]
    public async Task UpsertSubscriptionAsync_WhenSubscriptionDoesNotExist_CreatesNewSubscription()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var customerSync = Substitute.For<IRekazCustomerSyncService>();
        var logger = Substitute.For<ILogger<SubscriptionUpsertService>>();

        var rekazCustomerId = Guid.NewGuid();
        var localCustomer = new Customer(Guid.NewGuid(), "John Doe", "123456789", CustomerSource.LegacyRekazImport, rekazCustomerId: rekazCustomerId, gymType: GymType.ProFighter);

        customerSync.EnsureLocalCustomerAsync(rekazCustomerId, GymType.ProFighter, Arg.Any<ISet<Guid>?>(), Arg.Any<System.Threading.CancellationToken>())
            .Returns(Task.FromResult<Customer?>(localCustomer));

        var productId = Guid.NewGuid();
        var service = new SubscriptionUpsertService(context, customerSync, logger);

        var rekazSubId = Guid.NewGuid();
        var rekazSub = new RekazSubscriptionResult(
            Id: rekazSubId,
            SubscriptionCode: "SUB-NEW",
            CustomerId: rekazCustomerId,
            StartAt: DateTime.UtcNow,
            EndAt: DateTime.UtcNow.AddMonths(1),
            Status: "Pending",
            PaidAmount: 0m,
            TotalAmount: 100m,
            RemainingAmount: 100m,
            IsPaused: false,
            PausedAt: null,
            ResumeAt: null,
            Name: "سباحة شهر",
            ProductId: productId
        );

        // Act
        var result = await service.UpsertSubscriptionAsync(rekazSub, GymType.ProFighter);
        await context.SaveChangesAsync();

        // Assert
        Assert.Equal(SubscriptionUpsertResult.Created, result);
        var created = await context.Subscriptions.FirstOrDefaultAsync(s => s.RekazSubscriptionId == rekazSubId && s.GymType == GymType.ProFighter);
        Assert.NotNull(created);
        Assert.Null(created.Type);
        Assert.Equal("Pending", created.Status);
        Assert.Equal("سباحة شهر", created.Name);
    }

    [Fact]
    public async Task UpsertSubscriptionAsync_WhenSubscriptionExists_UpdatesExistingSubscriptionAndName()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var customerSync = Substitute.For<IRekazCustomerSyncService>();
        var logger = Substitute.For<ILogger<SubscriptionUpsertService>>();

        var rekazCustomerId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var rekazSubId = Guid.NewGuid();

        var existingSub = new Subscription(
            id: Guid.NewGuid(),
            customerId: customerId,
            rekazSubscriptionId: rekazSubId,
            type: null,
            startDate: DateTime.UtcNow.AddDays(-5),
            price: 100m,
            name: "old name",
            gymType: GymType.ProGym);

        context.Subscriptions.Add(existingSub);
        await context.SaveChangesAsync();

        var localCustomer = new Customer(customerId, "Jane Doe", "987654321", CustomerSource.LegacyRekazImport, rekazCustomerId: rekazCustomerId, gymType: GymType.ProGym);
        customerSync.EnsureLocalCustomerAsync(rekazCustomerId, GymType.ProGym, Arg.Any<ISet<Guid>?>(), Arg.Any<System.Threading.CancellationToken>())
            .Returns(Task.FromResult<Customer?>(localCustomer));

        var productId = Guid.NewGuid();
        var service = new SubscriptionUpsertService(context, customerSync, logger);

        var rekazSub = new RekazSubscriptionResult(
            Id: rekazSubId,
            SubscriptionCode: "SUB-EXISTING",
            CustomerId: rekazCustomerId,
            StartAt: DateTime.UtcNow.AddDays(-5),
            EndAt: DateTime.UtcNow.AddDays(25),
            Status: "Active",
            PaidAmount: 100m,
            TotalAmount: 100m,
            RemainingAmount: 0m,
            IsPaused: false,
            PausedAt: null,
            ResumeAt: null,
            Name: "سباحة شهر جديدة",
            ProductId: productId
        );

        // Act
        var result = await service.UpsertSubscriptionAsync(rekazSub, GymType.ProGym);

        // Assert
        Assert.Equal(SubscriptionUpsertResult.Updated, result);
        var updated = await context.Subscriptions.FirstOrDefaultAsync(s => s.RekazSubscriptionId == rekazSubId && s.GymType == GymType.ProGym);
        Assert.NotNull(updated);
        Assert.Equal("Active", updated.Status);
        Assert.Equal("سباحة شهر جديدة", updated.Name);
    }

    [Fact]
    public async Task ConcurrentHandlers_ForSameCustomerAndSubscription_HandlesConcurrencySuccessfully()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var customerSync = Substitute.For<IRekazCustomerSyncService>();
        var logger = Substitute.For<ILogger<SubscriptionUpsertService>>();

        var rekazCustomerId = Guid.NewGuid();
        var localCustomer = new Customer(Guid.NewGuid(), "Concurrent Customer", "123456789", CustomerSource.LegacyRekazImport, rekazCustomerId: rekazCustomerId, gymType: GymType.ProFighter);

        customerSync.EnsureLocalCustomerAsync(rekazCustomerId, GymType.ProFighter, Arg.Any<ISet<Guid>?>(), Arg.Any<System.Threading.CancellationToken>())
            .Returns(Task.FromResult<Customer?>(localCustomer));

        var service = new SubscriptionUpsertService(context, customerSync, logger);

        var rekazSubId = Guid.NewGuid();
        var rekazSub = new RekazSubscriptionResult(
            Id: rekazSubId,
            SubscriptionCode: "SUB-CONCURRENT",
            CustomerId: rekazCustomerId,
            StartAt: DateTime.UtcNow,
            EndAt: DateTime.UtcNow.AddMonths(1),
            Status: "Active",
            PaidAmount: 100m,
            TotalAmount: 100m,
            RemainingAmount: 0m,
            IsPaused: false,
            PausedAt: null,
            ResumeAt: null,
            Name: "اشتراك عام"
        );

        // Act - Simulate 2 concurrent tasks processing the same subscription
        var task1 = service.UpsertSubscriptionAsync(rekazSub, GymType.ProFighter);
        var task2 = service.UpsertSubscriptionAsync(rekazSub, GymType.ProFighter);

        var results = await Task.WhenAll(task1, task2);
        await context.SaveChangesAsync();

        // Assert
        Assert.Contains(SubscriptionUpsertResult.Created, results);
        var count = await context.Subscriptions.CountAsync(s => s.RekazSubscriptionId == rekazSubId && s.GymType == GymType.ProFighter);
        Assert.Equal(1, count);
    }
}
