using System;
using System.Threading;
using System.Threading.Tasks;
using ProFighter.Domain.Enums;

namespace ProFighter.Application.Common.Interfaces;

public interface IRekazSubscriptionEventHandler
{
    Task HandleAsync(Guid rekazSubscriptionId, string eventName, GymType gymType = GymType.ProFighter, CancellationToken ct = default);
}
