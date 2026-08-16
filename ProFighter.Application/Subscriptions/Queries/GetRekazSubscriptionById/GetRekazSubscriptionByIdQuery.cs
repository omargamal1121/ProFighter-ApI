using MediatR;
using ProFighter.Application.Common.Models;
using System;

namespace ProFighter.Application.Subscriptions.Queries.GetRekazSubscriptionById;

public record GetRekazSubscriptionByIdQuery(Guid Id) : IRequest<RekazSubscriptionResult?>;
