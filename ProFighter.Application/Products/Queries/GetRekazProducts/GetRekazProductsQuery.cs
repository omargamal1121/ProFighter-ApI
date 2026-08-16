using MediatR;
using ProFighter.Application.Common.Models;
using System;

namespace ProFighter.Application.Products.Queries.GetRekazProducts;

public record GetRekazProductsQuery(
    int SkipCount = 0,
    int MaxResultCount = 20,
    string? Keyword = null,
    RekazProductType? Type = null,
    Guid? BranchId = null,
    string? Sorting = null) : IRequest<RekazProductsResult>;
