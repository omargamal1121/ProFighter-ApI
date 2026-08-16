using MediatR;
using ProFighter.Application.Common.Interfaces;
using ProFighter.Application.Common.Models;
using System.Threading;
using System.Threading.Tasks;

namespace ProFighter.Application.Products.Queries.GetRekazProducts;

// Orchestrates: Fetches products from Rekaz API using the provided query parameters.
public class GetRekazProductsQueryHandler : IRequestHandler<GetRekazProductsQuery, RekazProductsResult>
{
    private readonly IRekazProductsClient _rekazProductsClient;

    public GetRekazProductsQueryHandler(IRekazProductsClient rekazProductsClient)
    {
        _rekazProductsClient = rekazProductsClient;
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

        return await _rekazProductsClient.GetProductsAsync(rekazQuery, cancellationToken);
    }
}
