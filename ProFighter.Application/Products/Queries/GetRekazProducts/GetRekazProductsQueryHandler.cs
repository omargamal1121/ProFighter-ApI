using MediatR;
using ProFighter.Application.Common.Interfaces;
using ProFighter.Application.Common.Models;
using System.Threading;
using System.Threading.Tasks;

namespace ProFighter.Application.Products.Queries.GetRekazProducts;

// Orchestrates: Fetches products from Rekaz API using the provided query parameters.
public class GetRekazProductsQueryHandler : IRequestHandler<GetRekazProductsQuery, RekazProductsResult>
{
    private readonly IRekazClientFactory _clientFactory;
    private readonly ICurrentGymContext _gymContext;

    public GetRekazProductsQueryHandler(
        IRekazClientFactory clientFactory,
        ICurrentGymContext gymContext)
    {
        _clientFactory = clientFactory;
        _gymContext = gymContext;
    }

    public async Task<RekazProductsResult> Handle(GetRekazProductsQuery request, CancellationToken cancellationToken)
    {
        var rekazQuery = new RekazProductsQuery(
            SkipCount: request.SkipCount,
            MaxResultCount: request.MaxResultCount,
            Keyword: request.Keyword,
            Type: request.Type,
            BranchId: request.BranchId,
            Sorting: request.Sorting
        );

        var rekazClient = _clientFactory.GetClient(_gymContext.CurrentGymType);
        return await rekazClient.Products.GetProductsAsync(rekazQuery, cancellationToken);
    }
}
