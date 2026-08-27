using System;
using System.Threading;
using System.Threading.Tasks;

namespace ProFighter.Application.Common.Interfaces;

public interface IRekazSubscriptionEventHandler
{
    Task HandleAsync(Guid rekazSubscriptionId, string eventName, CancellationToken ct);
}
