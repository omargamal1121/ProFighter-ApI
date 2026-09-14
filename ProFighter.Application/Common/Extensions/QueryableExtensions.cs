using Microsoft.EntityFrameworkCore;
using ProFighter.Application.Common.Interfaces;
using ProFighter.Domain.Enums;
using System.Linq;

namespace ProFighter.Application.Common.Extensions;

public static class QueryableExtensions
{
    public static IQueryable<T> ForCurrentGym<T>(this IQueryable<T> query, ICurrentGymContext context) where T : class
    {
        return query.Where(e => EF.Property<GymType>(e, "GymType") == context.CurrentGymType);
    }
}
