using MediatR;

namespace ProFighter.Application.Customers.Commands.RegisterCustomer;

public record RegisterCustomerCommand(
    string Name,
    string MobileNumber,
    string Email,
    string Password
) : IRequest<RegisterCustomerResult>;

public record RegisterCustomerResult(Guid CustomerId, bool EmailConfirmationSent);
