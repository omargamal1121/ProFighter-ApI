using MediatR;
using Microsoft.Extensions.Logging;
using ProFighter.Application.Common.Interfaces;
using ProFighter.Application.Common.Models;
using ProFighter.Domain.Entities;
using ProFighter.Domain.Enums;

namespace ProFighter.Application.Customers.Commands.RegisterCustomer;

// Orchestrates: Rekaz customer creation first (required — Rekaz rejects subscriptions
// for customers it doesn't already know about), then local persistence (ApplicationUser +
// Customer, shared primary key) in a transaction, then best-effort email confirmation.
public class RegisterCustomerCommandHandler : IRequestHandler<RegisterCustomerCommand, RegisterCustomerResult>
{
    private readonly IRekazCustomersClient _rekazCustomersClient;
    private readonly ICustomerProvisioningService _provisioningService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IApplicationDbContext _context;
    private readonly IEmailConfirmationService _emailConfirmationService;
    private readonly ILogger<RegisterCustomerCommandHandler> _logger;

    public RegisterCustomerCommandHandler(
        IRekazCustomersClient rekazCustomersClient,
        ICustomerProvisioningService provisioningService,
        IUnitOfWork unitOfWork,
        IApplicationDbContext context,
        IEmailConfirmationService emailConfirmationService,
        ILogger<RegisterCustomerCommandHandler> logger)
    {
        _rekazCustomersClient = rekazCustomersClient;
        _provisioningService = provisioningService;
        _unitOfWork = unitOfWork;
        _context = context;
        _emailConfirmationService = emailConfirmationService;
        _logger = logger;
    }

    public async Task<RegisterCustomerResult> Handle(RegisterCustomerCommand request, CancellationToken ct)
    {
        var rekazCustomerId = await _rekazCustomersClient.CreateCustomerAsync(
            new CreateRekazCustomerRequest(request.Name, request.MobileNumber, request.Email), ct);

        Guid customerId;
        try
        {
            customerId = await _unitOfWork.ExecuteInTransactionAsync(async innerCt =>
            {
                var id = await _provisioningService.ProvisionLocalCustomerWithPasswordAsync(
                    rekazCustomerId, request.Name, request.MobileNumber, request.Email,
                    request.Password, CustomerSource.EmailRegistration, innerCt);
                await _context.SaveChangesAsync(innerCt);
                return id;
            }, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Local synchronization failed for self-registered Rekaz customer {RekazCustomerId}.", rekazCustomerId);

            var payload = System.Text.Json.JsonSerializer.Serialize(
                new { request.Name, request.MobileNumber, request.Email });
            var failure = new Domain.Entities.CustomerSyncFailure(rekazCustomerId, payload, ex.Message, "Pending");

            try
            {
                _context.CustomerSyncFailures.Add(failure);
                await _context.SaveChangesAsync(ct);
            }
            catch (Exception dbEx)
            {
                _logger.LogCritical(dbEx,
                    "Failed to persist CustomerSyncFailure for Rekaz customer {RekazCustomerId}.", rekazCustomerId);
            }

            

            return new RegisterCustomerResult(Guid.Empty, false);
        }

        var emailSent = false;
        if (!string.IsNullOrWhiteSpace(request.Email))
        {
            try
            {
                await _emailConfirmationService.SendConfirmationOtpAsync(customerId, ct);
                emailSent = true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email confirmation OTP for customer {CustomerId}.", customerId);
            }
        }

        return new RegisterCustomerResult(customerId, emailSent);
    }
}
