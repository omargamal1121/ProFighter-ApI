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
        var typeMapper = Substitute.For<ISubscriptionTypeMapper>();
        var logger = Substitute.For<ILogger<SubscriptionUpsertService>>();

        var service = new SubscriptionUpsertService(context, customerSync, typeMapper, logger);

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
        var typeMapper = Substitute.For<ISubscriptionTypeMapper>();
        var logger = Substitute.For<ILogger<SubscriptionUpsertService>>();

        var rekazCustomerId = Guid.NewGuid();
        var localCustomer = new Customer(Guid.NewGuid(), "John Doe", "123456789", CustomerSource.LegacyRekazImport, rekazCustomerId: rekazCustomerId, gymType: GymType.ProFighter);

        customerSync.EnsureLocalCustomerAsync(rekazCustomerId, GymType.ProFighter, Arg.Any<System.Threading.CancellationToken>())
            .Returns(Task.FromResult(localCustomer));

        var productId = Guid.NewGuid();
        typeMapper.MapSubscriptionType(GymType.ProFighter, productId).Returns(SubscriptionType.Swimming);

        var service = new SubscriptionUpsertService(context, customerSync, typeMapper, logger);

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
        Assert.Equal(SubscriptionType.Swimming, created.Type);
        Assert.Equal("Pending", created.Status);
        Assert.Equal("سباحة شهر", created.Name);
    }

    [Fact]
    public async Task UpsertSubscriptionAsync_WhenSubscriptionExists_UpdatesExistingSubscriptionAndType()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var customerSync = Substitute.For<IRekazCustomerSyncService>();
        var typeMapper = Substitute.For<ISubscriptionTypeMapper>();
        var logger = Substitute.For<ILogger<SubscriptionUpsertService>>();

        var rekazCustomerId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var rekazSubId = Guid.NewGuid();

        // Existing subscription was wrongly MartialArts
        var existingSub = new Subscription(
            id: Guid.NewGuid(),
            customerId: customerId,
            rekazSubscriptionId: rekazSubId,
            type: SubscriptionType.MartialArts,
            startDate: DateTime.UtcNow.AddDays(-5),
            price: 100m,
            gymType: GymType.ProGym);

        context.Subscriptions.Add(existingSub);
        await context.SaveChangesAsync();

        var productId = Guid.NewGuid();
        typeMapper.MapSubscriptionType(GymType.ProGym, productId).Returns(SubscriptionType.Swimming);

        var service = new SubscriptionUpsertService(context, customerSync, typeMapper, logger);

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
            Name: "سباحة شهر",
            ProductId: productId
        );

        // Act
        var result = await service.UpsertSubscriptionAsync(rekazSub, GymType.ProGym);

        // Assert
        Assert.Equal(SubscriptionUpsertResult.Updated, result);
        var updated = await context.Subscriptions.FirstOrDefaultAsync(s => s.RekazSubscriptionId == rekazSubId && s.GymType == GymType.ProGym);
        Assert.NotNull(updated);
        Assert.Equal(SubscriptionType.Swimming, updated.Type); // Type updated!
        Assert.Equal("Active", updated.Status);
    }
}
