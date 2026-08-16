using MediatR;
using ProFighter.Application.Common.Models;

namespace ProFighter.Application.Subscriptions.Commands.CreateRekazSubscription;

public record CreateRekazSubscriptionCommand(
    CreateRekazSubscriptionRequest Request) : IRequest<RekazSubscriptionCreatedResult>;
