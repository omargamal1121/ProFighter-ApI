using System;
using System.Threading;
using System.Threading.Tasks;

namespace ProFighter.Application.Subscriptions.Services;

public interface IRekazTransactionEventHandler
{
    Task HandleAsync(Guid transactionId, string eventName, CancellationToken ct);
}
