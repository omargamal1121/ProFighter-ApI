using MediatR;

namespace ProFighter.Application.Subscriptions.Commands.SyncSubscriptions;

public record SyncSubscriptionsCommand : IRequest<SyncSubscriptionsResult>;

public record SyncSubscriptionsResult(int TotalProcessed, int Created, int Updated, int Skipped);
