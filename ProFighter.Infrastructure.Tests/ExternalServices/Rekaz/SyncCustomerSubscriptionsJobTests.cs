using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;
using ProFighter.Application.Common.Exceptions;
using ProFighter.Application.Common.Interfaces;
using ProFighter.Application.Common.Models;
using ProFighter.Application.Subscriptions.Jobs;
using ProFighter.Domain.Entities;
using ProFighter.Domain.Enums;
using ProFighter.Infrastructure.Persistence;
using Xunit;

namespace ProFighter.Infrastructure.Tests.ExternalServices.Rekaz;

public class SyncCustomerSubscriptionsJobTests
{
    private static AppDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task ExecuteAsync_WhenMatchingSubscriptionFound_SucceedsWithoutThrowing()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var clientFactory = Substitute.For<IRekazClientFactory>();
        var rekazClient = Substitute.For<IRekazClient>();
        var subscriptionsClient = Substitute.For<IRekazSubscriptionsClient>();
        var customerSync = Substitute.For<IRekazCustomerSyncService>();
        var upsertService = Substitute.For<ISubscriptionUpsertService>();
        var logger = Substitute.For<ILogger<SyncCustomerSubscriptionsJob>>();

        clientFactory.GetClient(GymType.ProFighter).Returns(rekazClient);
        rekazClient.Subscriptions.Returns(subscriptionsClient);

        var rekazCustomerId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var createdAt = DateTimeOffset.UtcNow;

        var subResult = new RekazSubscriptionResult(
            Id: Guid.NewGuid(),
            SubscriptionCode: "SUB-1",
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
            ProductId: productId,
            CreationTime: createdAt
        );

        subscriptionsClient.GetSubscriptionsByCustomerAsync(rekazCustomerId, Arg.Any<System.Threading.CancellationToken>())
            .Returns(Task.FromResult(new List<RekazSubscriptionResult> { subResult }));

        upsertService.UpsertSubscriptionAsync(subResult, GymType.ProFighter, Arg.Any<System.Threading.CancellationToken>())
            .Returns(Task.FromResult(SubscriptionUpsertResult.Created));

        var job = new SyncCustomerSubscriptionsJob(clientFactory, context, customerSync, upsertService, logger);

        // Act & Assert (should not throw)
        await job.ExecuteAsync(GymType.ProFighter, rekazCustomerId, createdAt, new List<Guid> { productId }, performContext: null);

        await customerSync.Received(1).EnsureLocalCustomerAsync(rekazCustomerId, GymType.ProFighter, Arg.Any<System.Threading.CancellationToken>());
        await upsertService.Received(1).UpsertSubscriptionAsync(subResult, GymType.ProFighter, Arg.Any<System.Threading.CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WhenNoMatchingSubscriptionAndNotFinalRetry_ThrowsSubscriptionNotYetVisibleException()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var clientFactory = Substitute.For<IRekazClientFactory>();
        var rekazClient = Substitute.For<IRekazClient>();
        var subscriptionsClient = Substitute.For<IRekazSubscriptionsClient>();
        var customerSync = Substitute.For<IRekazCustomerSyncService>();
        var upsertService = Substitute.For<ISubscriptionUpsertService>();
        var logger = Substitute.For<ILogger<SyncCustomerSubscriptionsJob>>();

        clientFactory.GetClient(GymType.ProFighter).Returns(rekazClient);
        rekazClient.Subscriptions.Returns(subscriptionsClient);

        var rekazCustomerId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var createdAt = DateTimeOffset.UtcNow;

        // Returns empty subscriptions list
        subscriptionsClient.GetSubscriptionsByCustomerAsync(rekazCustomerId, Arg.Any<System.Threading.CancellationToken>())
            .Returns(Task.FromResult(new List<RekazSubscriptionResult>()));

        var job = new SyncCustomerSubscriptionsJob(clientFactory, context, customerSync, upsertService, logger);

        // Act & Assert
        await Assert.ThrowsAsync<SubscriptionNotYetVisibleException>(() =>
            job.ExecuteAsync(GymType.ProFighter, rekazCustomerId, createdAt, new List<Guid> { productId }, performContext: null));
    }
}
