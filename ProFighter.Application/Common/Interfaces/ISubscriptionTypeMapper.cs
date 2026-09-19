using System;
using ProFighter.Domain.Enums;

namespace ProFighter.Application.Common.Interfaces;

/// <summary>
/// Service responsible for mapping Rekaz product/price IDs to local <see cref="SubscriptionType"/>.
/// Keyed by (GymType, ProductId/PriceId).
/// </summary>
public interface ISubscriptionTypeMapper
{
    /// <summary>
    /// Maps a Rekaz productId (or priceId) and GymType to a <see cref="SubscriptionType"/>.
    /// If unmapped, logs a warning and returns default (<see cref="SubscriptionType.MartialArts"/>).
    /// </summary>
    SubscriptionType MapSubscriptionType(GymType gymType, Guid? productId);
}
