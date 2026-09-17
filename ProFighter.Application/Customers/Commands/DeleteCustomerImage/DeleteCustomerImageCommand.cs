using System;
using MediatR;
using ProFighter.Application.Common;

namespace ProFighter.Application.Customers.Commands.DeleteCustomerImage;

public record DeleteCustomerImageCommand(
    Guid CustomerId,
    Guid ImageId
) : IRequest<Result<bool>>;
