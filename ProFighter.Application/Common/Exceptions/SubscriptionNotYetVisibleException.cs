using System;
using System.Collections.Generic;
using ProFighter.Domain.Enums;

namespace ProFighter.Application.Common.Exceptions;

/// <summary>
/// Thrown when a newly purchased Rekaz subscription is not yet readable via GET /api/public/subscriptions.
/// Triggers Hangfire automatic retries.
/// </summary>
public class SubscriptionNotYetVisibleException : Exception
{
    public GymType GymType { get; }
    public Guid RekazCustomerId { get; }
    public DateTimeOffset TransactionCreatedAtUtc { get; }
    public IReadOnlyList<Guid> ProductIds { get; }

    public SubscriptionNotYetVisibleException(
        GymType gymType,
        Guid rekazCustomerId,
        DateTimeOffset transactionCreatedAtUtc,
        IReadOnlyList<Guid> productIds)
        : base($"Subscription for Rekaz customer {rekazCustomerId} ({gymType}) with product IDs [{string.Join(", ", productIds)}] created at {transactionCreatedAtUtc:O} is not yet visible in Rekaz API.")
    {
        GymType = gymType;
        RekazCustomerId = rekazCustomerId;
        TransactionCreatedAtUtc = transactionCreatedAtUtc;
        ProductIds = productIds;
    }
}
