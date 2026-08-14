using MediatR;

namespace ProFighter.Application.Customers.Commands.CreateCustomerByAdmin;

public record CreateCustomerByAdminCommand(
    string Name,
    string MobileNumber,
    string? Email) : IRequest<CreateCustomerByAdminResult>;

public record CreateCustomerByAdminResult(Guid CustomerId, bool LocalSyncSucceeded);
