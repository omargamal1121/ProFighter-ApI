using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ProFighter.Application.Common.Models;
using ProFighter.Domain.Enums;

namespace ProFighter.Application.Common.Interfaces;

public enum SubscriptionUpsertResult
{
    Created,
    Updated,
    Unchanged,
    Skipped
}

/// <summary>
/// Shared service responsible for upserting Rekaz subscriptions into the local database.
/// Does NOT call SaveChangesAsync (caller manages unit of work boundary).
/// Does NOT send push notifications on upsert.
/// </summary>
public interface ISubscriptionUpsertService
{
    /// <summary>
    /// Upserts a subscription returned from Rekaz.
    /// Matches on RekazSubscriptionId + GymType.
    /// Ensures local customer exists first via IRekazCustomerSyncService.
    /// Dynamically maps SubscriptionType via ISubscriptionTypeMapper.
    /// </summary>
    Task<SubscriptionUpsertResult> UpsertSubscriptionAsync(
        RekazSubscriptionResult rekazSub,
        GymType gymType,
        ISet<Guid>? negativeCache = null,
        ISet<Guid>? unmappedProductTracker = null,
        CancellationToken ct = default);
}
