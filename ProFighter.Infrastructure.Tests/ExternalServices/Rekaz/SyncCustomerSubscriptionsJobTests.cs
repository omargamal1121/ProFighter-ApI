using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NSubstitute;
using ProFighter.Application.Subscriptions.Jobs;
using ProFighter.Domain.Enums;
using Xunit;

namespace ProFighter.Infrastructure.Tests.ExternalServices.Rekaz;

public class SyncCustomerSubscriptionsJobTests
{
    [Fact]
    public async Task ExecuteAsync_IsObsoleteNoOp_CompletesWithoutThrowing()
    {
        // Arrange
        var logger = Substitute.For<ILogger<SyncCustomerSubscriptionsJob>>();
        var job = new SyncCustomerSubscriptionsJob(logger);

        var rekazCustomerId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var createdAt = DateTimeOffset.UtcNow;

        // Act & Assert (should complete as no-op without throwing)
        await job.ExecuteAsync(GymType.ProFighter, rekazCustomerId, createdAt, new List<Guid> { productId }, performContext: null);
    }
}
