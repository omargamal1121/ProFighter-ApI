using MediatR;

namespace ProFighter.Application.Customers.Commands.SyncCustomers;

public record SyncCustomersCommand : IRequest<SyncCustomersResult>;

public record SyncCustomersResult(int TotalProcessed, int Created, int Updated, int Skipped, int Errors);
