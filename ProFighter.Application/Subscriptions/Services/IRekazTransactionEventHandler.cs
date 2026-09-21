using System;
using System.Threading;
using System.Threading.Tasks;
using ProFighter.Domain.Enums;

namespace ProFighter.Application.Subscriptions.Services;

public interface IRekazTransactionEventHandler
{
    Task HandleAsync(Guid transactionId, string eventName, GymType gymType = GymType.ProFighter, CancellationToken ct = default);
}
