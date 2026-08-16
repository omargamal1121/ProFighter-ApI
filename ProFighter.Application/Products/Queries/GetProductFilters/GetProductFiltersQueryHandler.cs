using MediatR;
using ProFighter.Application.Common.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ProFighter.Application.Products.Queries.GetProductFilters;

// Orchestrates: Returns a static list of product categories available for filtering.
public class GetProductFiltersQueryHandler : IRequestHandler<GetProductFiltersQuery, IReadOnlyList<ProductCategoryDto>>
{
    private static readonly IReadOnlyList<ProductCategoryDto> _categories =
    [
        new("boxing",    "بوكس"),
        new("kickboxing","كيك بوكس"),
        new("muay-thai", "ماي تاي"),
        new("mma",       "MMA"),
        new("jiu-jitsu", "جوجيتسو"),
        new("karate",    "كراتيه"),
        new("swimming",  "سباحة"),
        new("bootcamp",  "بود كامب"),
    ];

    public Task<IReadOnlyList<ProductCategoryDto>> Handle(GetProductFiltersQuery request, CancellationToken cancellationToken)
    {
        return Task.FromResult(_categories);
    }
}
