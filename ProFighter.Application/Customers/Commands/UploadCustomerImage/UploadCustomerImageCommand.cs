using System;
using MediatR;
using Microsoft.AspNetCore.Http;
using ProFighter.Application.Common;
using ProFighter.Application.Customers.Common;
using ProFighter.Domain.Enums;

namespace ProFighter.Application.Customers.Commands.UploadCustomerImage;

public record UploadCustomerImageCommand(
    Guid CustomerId,
    IFormFile Image,
    MediaPurpose Purpose = MediaPurpose.ProfileImage,
    int DisplayOrder = 0
) : IRequest<Result<CustomerMediaDto>>;
