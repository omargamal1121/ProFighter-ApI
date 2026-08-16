using MediatR;
using ProFighter.Application.Common.Models;
using System.Collections.Generic;

namespace ProFighter.Application.Products.Queries.GetProductFilters;

public record GetProductFiltersQuery : IRequest<IReadOnlyList<ProductCategoryDto>>;
