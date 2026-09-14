using ProFighter.Application.Common.Interfaces;

namespace ProFighter.Application.Common.Interfaces;

/// <summary>
/// A per-gym Rekaz client bundle exposing customers and subscriptions sub-clients,
/// each pre-configured with the correct API key and tenant for that gym.
/// </summary>
public interface IRekazClient
{
    IRekazCustomersClient Customers { get; }
    IRekazSubscriptionsClient Subscriptions { get; }
    IRekazProductsClient Products { get; }
    IRekazTransactionsClient Transactions { get; }
}
