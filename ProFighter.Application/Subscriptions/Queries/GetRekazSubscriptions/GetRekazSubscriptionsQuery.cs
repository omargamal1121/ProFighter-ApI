using MediatR;
using ProFighter.Application.Common.Models;
using System;
using System.Collections.Generic;

namespace ProFighter.Application.Subscriptions.Queries.GetRekazSubscriptions;

public record GetRekazSubscriptionsQuery(
    int MaxResultCount = 20,
    Guid? CustomerId = null,
    DateTime? StartAtMin = null,
    DateTime? StartAtMax = null,
    DateTime? NextBillingAtMin = null,
    DateTime? NextBillingAtMax = null,
    List<string>? Statuses = null,
    string? CustomerMobile = null,
    string? Keyword = null,
    List<Guid>? PriceIds = null,
    Guid? BranchId = null,
    string? Sorting = null,
    int SkipCount = 0) : IRequest<RekazSubscriptionsListResult>;
