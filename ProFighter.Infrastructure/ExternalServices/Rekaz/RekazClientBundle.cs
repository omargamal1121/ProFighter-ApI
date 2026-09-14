using Microsoft.Extensions.Logging;
using ProFighter.Application.Common.Interfaces;

namespace ProFighter.Infrastructure.ExternalServices.Rekaz;

/// <summary>
/// Internal implementation of <see cref="IRekazClient"/> — bundles a single
/// <see cref="HttpClient"/> (pre-configured with per-gym auth) into typed sub-clients.
/// The lifetime of this object is scoped to one <see cref="IRekazClientFactory.GetClient"/>
/// call; the underlying HttpClient is returned to the pool when the factory's scope ends.
/// </summary>
internal sealed class RekazClientBundle : IRekazClient
{
    public IRekazCustomersClient Customers { get; }
    public IRekazSubscriptionsClient Subscriptions { get; }
    public IRekazProductsClient Products { get; }
    public IRekazTransactionsClient Transactions { get; }

    internal RekazClientBundle(
        IRekazCustomersClient customers,
        IRekazSubscriptionsClient subscriptions,
        IRekazProductsClient products,
        IRekazTransactionsClient transactions)
    {
        Customers = customers;
        Subscriptions = subscriptions;
        Products = products;
        Transactions = transactions;
    }
}
