using MediatR;
using Microsoft.AspNetCore.Mvc;
using ProFighter.Application.Common;
using ProFighter.Application.Common.Models;
using ProFighter.Application.Products.Queries.GetProductFilters;
using ProFighter.Application.Products.Queries.GetRekazProducts;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ProFighter.API.Controllers;

[Route("api/rekaz/products")]
public class RekazProductsController : BaseController
{
    private readonly IMediator _mediator;

    public RekazProductsController(IMediator mediator)
    {
        _mediator = mediator;
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
        var query = new GetRekazProductsQuery(
            SkipCount: skipCount,
            MaxResultCount: maxResultCount,
            Keyword: keyword,
            Type: type,
            BranchId: branchId,
            Sorting: sorting
        );

        var data = await _mediator.Send(query, ct);
        return HandleResult(Result<RekazProductsResult>.Success(data, "Products fetched successfully."));
    }

    /// <summary>
    /// Returns the ProFighter sport category filter list.
    /// These are static options that map to keyword filters on the products endpoint.
    /// </summary>
    [HttpGet("filters")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<ProductCategoryDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ProductCategoryDto>>>> GetFilters(CancellationToken ct = default)
    {
        var data = await _mediator.Send(new GetProductFiltersQuery(), ct);
        return HandleResult(Result<IReadOnlyList<ProductCategoryDto>>.Success(data, "Filters fetched successfully."));
    }
}
