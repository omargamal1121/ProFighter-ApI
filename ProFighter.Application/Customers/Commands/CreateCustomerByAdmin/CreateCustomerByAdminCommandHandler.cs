using MediatR;
using Microsoft.Extensions.Logging;
using ProFighter.Application.Common.Interfaces;
using ProFighter.Application.Common.Models;
using ProFighter.Domain.Entities;
using ProFighter.Domain.Enums;
using System.Text.Json;

namespace ProFighter.Application.Customers.Commands.CreateCustomerByAdmin;

public class CreateCustomerByAdminCommandHandler : IRequestHandler<CreateCustomerByAdminCommand, CreateCustomerByAdminResult>
{
    private readonly IRekazCustomersClient _rekazCustomersClient;
    private readonly ICustomerProvisioningService _provisioningService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IApplicationDbContext _context;
    private readonly INotificationEmailService _emailService;
    private readonly ILogger<CreateCustomerByAdminCommandHandler> _logger;

    public CreateCustomerByAdminCommandHandler(
        IRekazCustomersClient rekazCustomersClient,
        ICustomerProvisioningService provisioningService,
        IUnitOfWork unitOfWork,
        IApplicationDbContext context,
        INotificationEmailService emailService,
        ILogger<CreateCustomerByAdminCommandHandler> logger)
    {
        _rekazCustomersClient = rekazCustomersClient;
        _provisioningService = provisioningService;
        _unitOfWork = unitOfWork;
        _context = context;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<CreateCustomerByAdminResult> Handle(CreateCustomerByAdminCommand request, CancellationToken cancellationToken)
    {
        // 1. Create on Rekaz first (unwrapped, let exceptions bubble up)
        var createRequest = new CreateRekazCustomerRequest(
            Name: request.Name,
            MobileNumber: request.MobileNumber,
            Email: request.Email
        );

        var rekazCustomerId = await _rekazCustomersClient.CreateCustomerAsync(createRequest, cancellationToken);

        // 2. Wrap local steps in Unit of Work transaction
        try
        {
            var customerId = await _unitOfWork.ExecuteInTransactionAsync(async innerCt =>
            {
                var id = await _provisioningService.ProvisionLocalCustomerAsync(
                    rekazCustomerId,
                    request.Name,
                    request.MobileNumber,
                    request.Email,
                    CustomerSource.AdminAdded,
                    innerCt
                );
                await _context.SaveChangesAsync(innerCt);
                return id;
            }, cancellationToken);

            return new CreateCustomerByAdminResult(customerId, LocalSyncSucceeded: true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Local synchronization failed for Rekaz customer ID: {RekazCustomerId}", rekazCustomerId);

            // Create CustomerSyncFailure entry
            var payload = JsonSerializer.Serialize(new { request.Name, request.MobileNumber, request.Email });
            var failure = new CustomerSyncFailure(rekazCustomerId, payload, ex.Message, "Pending");

            try
            {
                _context.CustomerSyncFailures.Add(failure);
                await _context.SaveChangesAsync(cancellationToken);
            }
            catch (Exception dbEx)
            {
                _logger.LogCritical(dbEx, "Failed to persist CustomerSyncFailure for Rekaz Customer: {RekazCustomerId}", rekazCustomerId);
            }

            // Send sync failure alert email
            try
            {
                await _emailService.SendSyncFailureAlertAsync(failure, cancellationToken);
            }
            catch (Exception emailEx)
            {
                _logger.LogError(emailEx, "Failed to send Sync Failure alert email for Rekaz Customer: {RekazCustomerId}", rekazCustomerId);
            }

            return new CreateCustomerByAdminResult(Guid.Empty, LocalSyncSucceeded: false);
        }
    }
}
