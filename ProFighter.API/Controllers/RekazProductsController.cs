using Microsoft.AspNetCore.Mvc;
using ProFighter.Application.Common;
using ProFighter.Application.Common.Interfaces;
using ProFighter.Application.Common.Models;

namespace ProFighter.API.Controllers;

[Route("api/rekaz/products")]
public class RekazProductsController : BaseController
{
    private readonly IRekazProductsClient _rekazProducts;

    // ProFighter sport categories used to filter the products listing.
    // Key → sent as the Keyword query param to Rekaz.
    // Name → Arabic display label shown in the UI.
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

    public RekazProductsController(IRekazProductsClient rekazProducts)
    {
        _rekazProducts = rekazProducts;
    }

    /// <summary>
    /// Fetches products from the Rekaz platform.
    /// Proxies the full set of Rekaz filter/pagination parameters.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<RekazProductsResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status502BadGateway)]
    public async Task<ActionResult<ApiResponse<RekazProductsResult>>> GetProducts(
        [FromQuery] int skipCount = 0,
        [FromQuery] int maxResultCount = 20,
        [FromQuery] string? keyword = null,
        [FromQuery] RekazProductType? type = null,
        [FromQuery] Guid? branchId = null,
        [FromQuery] string? sorting = null,
        CancellationToken ct = default)
    {
        var query = new RekazProductsQuery(
            SkipCount: skipCount,
            MaxResultCount: maxResultCount,
            Keyword: keyword,
            Type: type,
            BranchId: branchId,
            Sorting: sorting
        );

        var data = await _rekazProducts.GetProductsAsync(query, ct);
        return HandleResult(Result<RekazProductsResult>.Success(data, "Products fetched successfully."));
    }

    /// <summary>
    /// Returns the ProFighter sport category filter list.
    /// These are static options that map to keyword filters on the products endpoint.
    /// </summary>
    [HttpGet("filters")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<ProductCategoryDto>>), StatusCodes.Status200OK)]
    public ActionResult<ApiResponse<IReadOnlyList<ProductCategoryDto>>> GetFilters() =>
        HandleResult(Result<IReadOnlyList<ProductCategoryDto>>.Success(_categories, "Filters fetched successfully."));
}
