using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using ProFighter.Application.Common.Interfaces;
using ProFighter.Application.Common.Models;
using ProFighter.Domain.Entities;

namespace ProFighter.Application.Customers.Commands.CreateCustomerByAdmin;

// Orchestrates: Rekaz creation first, then local persistence in a transaction, with failure-recovery logging on local sync failure
public class CreateCustomerByAdminCommandHandler : IRequestHandler<CreateCustomerByAdminCommand, CreateCustomerByAdminResult>
{
	private readonly IRekazCustomersClient _rekazCustomersClient;
	private readonly ICustomerProvisioningService _provisioningService;
	private readonly INotificationEmailService _emailService;
	private readonly IUnitOfWork _unitOfWork;
	private readonly IApplicationDbContext _context;
	private readonly ILogger<CreateCustomerByAdminCommandHandler> _logger;

	public CreateCustomerByAdminCommandHandler(
		IRekazCustomersClient rekazCustomersClient,
		ICustomerProvisioningService provisioningService,
		INotificationEmailService emailService,
		IUnitOfWork unitOfWork,
		IApplicationDbContext context,
		ILogger<CreateCustomerByAdminCommandHandler> logger)
	{
		_rekazCustomersClient = rekazCustomersClient;
		_provisioningService = provisioningService;
		_emailService = emailService;
		_unitOfWork = unitOfWork;
		_context = context;
		_logger = logger;
	}

	public async Task<CreateCustomerByAdminResult> Handle(CreateCustomerByAdminCommand request, CancellationToken cancellationToken)
	{
		var createRequest = new CreateRekazCustomerRequest(
			Name: request.Name,
			MobileNumber: request.MobileNumber,
			Email: request.Email
		);

		
		var rekazCustomerId = await _rekazCustomersClient.CreateCustomerAsync(createRequest, cancellationToken);

	
		try
		{
			var localCustomerId = await _unitOfWork.ExecuteInTransactionAsync(async ct =>
			{
				var id = await _provisioningService.ProvisionFromRekazAsync(
					rekazCustomerId,
					request.Name,
					request.MobileNumber,
					request.Email,
					ct
				);
				await _context.SaveChangesAsync(ct);
				return id;
			}, cancellationToken);

			return new CreateCustomerByAdminResult(localCustomerId, LocalSyncSucceeded: true);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Local synchronization failed for Rekaz customer ID: {RekazCustomerId}", rekazCustomerId);

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

			try
			{
				await _emailService.SendSyncFailureAlertAsync(failure, cancellationToken);
			}
			catch (Exception emailEx)
			{
				_logger.LogError(emailEx, "Failed to send Sync Failure alert email for Rekaz Customer: {RekazCustomerId}", rekazCustomerId);
			}

			return new CreateCustomerByAdminResult(rekazCustomerId, LocalSyncSucceeded: false);
		}
	}
}