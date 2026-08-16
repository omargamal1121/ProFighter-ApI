using MediatR;
using ProFighter.Application.Common.Interfaces;
using ProFighter.Application.Common.Models;
using System.Threading;
using System.Threading.Tasks;

namespace ProFighter.Application.Subscriptions.Queries.GetRekazSubscriptions;

// Orchestrates: Fetches a list of subscriptions from Rekaz API using the provided query parameters.
public class GetRekazSubscriptionsQueryHandler : IRequestHandler<GetRekazSubscriptionsQuery, RekazSubscriptionsListResult>
{
    private readonly IRekazSubscriptionsClient _rekazSubscriptionsClient;

    public GetRekazSubscriptionsQueryHandler(IRekazSubscriptionsClient rekazSubscriptionsClient)
    {
        _rekazSubscriptionsClient = rekazSubscriptionsClient;
    }

    public async Task<RekazSubscriptionsListResult> Handle(GetRekazSubscriptionsQuery request, CancellationToken cancellationToken)
    {
        var rekazQuery = new RekazSubscriptionsQuery(
            MaxResultCount: request.MaxResultCount,
            CustomerId: request.CustomerId,
            StartAtMin: request.StartAtMin,
            StartAtMax: request.StartAtMax,
            NextBillingAtMin: request.NextBillingAtMin,
            NextBillingAtMax: request.NextBillingAtMax,
            Statuses: request.Statuses,
            CustomerMobile: request.CustomerMobile,
            Keyword: request.Keyword,
            PriceIds: request.PriceIds,
            BranchId: request.BranchId,
            Sorting: request.Sorting,
            SkipCount: request.SkipCount
        );

        return await _rekazSubscriptionsClient.GetSubscriptionsAsync(rekazQuery, cancellationToken);
    }
}
