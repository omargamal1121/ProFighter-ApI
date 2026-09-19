using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ProFighter.Application.Common.Interfaces;
using ProFighter.Domain.Enums;

namespace ProFighter.Infrastructure.ExternalServices.Rekaz;

public class SubscriptionTypeMapper : ISubscriptionTypeMapper
{
    private readonly RekazSubscriptionTypeOptions _options;
    private readonly ILogger<SubscriptionTypeMapper> _logger;

    public SubscriptionTypeMapper(
        IOptions<RekazSubscriptionTypeOptions> options,
        ILogger<SubscriptionTypeMapper> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public SubscriptionType MapSubscriptionType(GymType gymType, Guid? productId)
    {
        if (!productId.HasValue || productId.Value == Guid.Empty)
        {
            _logger.LogWarning("Missing or empty ProductId for {GymType}. Defaulting SubscriptionType to MartialArts.", gymType);
            return SubscriptionType.MartialArts;
        }

        var dictionary = gymType switch
        {
            GymType.ProFighter => _options.ProFighter,
            GymType.ProGym => _options.ProGym,
            _ => null
        };

        var productIdStr = productId.Value.ToString();

        if (dictionary != null && dictionary.TryGetValue(productIdStr, out var rawType))
        {
            if (Enum.TryParse<SubscriptionType>(rawType, true, out var parsedType))
            {
                return parsedType;
            }
            _logger.LogWarning("Unrecognized SubscriptionType '{RawType}' configured for ProductId {ProductId} in {GymType}. Defaulting to MartialArts.", rawType, productId.Value, gymType);
        }
        else
        {
            _logger.LogWarning("Unmapped ProductId {ProductId} for {GymType}. Defaulting SubscriptionType to MartialArts.", productId.Value, gymType);
        }

        return SubscriptionType.MartialArts;
    }
}
