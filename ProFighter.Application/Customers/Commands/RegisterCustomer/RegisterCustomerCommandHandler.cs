using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ProFighter.Application.Common.Exceptions;
using ProFighter.Application.Common.Interfaces;
using ProFighter.Application.Common.Models;
using ProFighter.Domain.Entities;
using ProFighter.Domain.Enums;
using System.Net;
using System.Text.Json;

namespace ProFighter.Application.Customers.Commands.RegisterCustomer;

// Orchestrates: Rekaz customer creation first (required — Rekaz rejects subscriptions
// for customers it doesn't already know about), then local persistence (ApplicationUser +
// Customer, shared primary key) in a transaction, then best-effort email confirmation.
public class RegisterCustomerCommandHandler : IRequestHandler<RegisterCustomerCommand, RegisterCustomerResult>
{
    private readonly IRekazClientFactory _rekazClientFactory;
    private readonly ICurrentGymContext _gymContext;
    private readonly ICustomerProvisioningService _provisioningService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IApplicationDbContext _context;
    private readonly IEmailConfirmationService _emailConfirmationService;
    private readonly ILogger<RegisterCustomerCommandHandler> _logger;

    public RegisterCustomerCommandHandler(
        IRekazClientFactory rekazClientFactory,
        ICurrentGymContext gymContext,
        ICustomerProvisioningService provisioningService,
        IUnitOfWork unitOfWork,
        IApplicationDbContext context,
        IEmailConfirmationService emailConfirmationService,
        ILogger<RegisterCustomerCommandHandler> logger)
    {
        _rekazClientFactory = rekazClientFactory;
        _gymContext = gymContext;
        _provisioningService = provisioningService;
        _unitOfWork = unitOfWork;
        _context = context;
        _emailConfirmationService = emailConfirmationService;
        _logger = logger;
    }

    public async Task<RegisterCustomerResult> Handle(RegisterCustomerCommand request, CancellationToken ct)
    {
        var currentGym = _gymContext.CurrentGymType;

        // Check for existing mobile number or email in our database for the current gym before calling Rekaz
        var existingMobile = await _context.Customers
            .AnyAsync(c => c.GymType == currentGym && c.MobileNumber == request.MobileNumber, ct);
        if (existingMobile)
        {
            throw new InvalidOperationException($"A customer with mobile number '{request.MobileNumber}' already exists for this gym.");
        }

        var rekazClient = _rekazClientFactory.GetClient(currentGym);
        var existingRekazCustomer = await rekazClient.Customers.GetCustomerByMobileNumberAsync(request.MobileNumber, ct);
        if (existingRekazCustomer != null)
        {
            await EnsureLocalCustomerExistsAsync(existingRekazCustomer, currentGym, ct);
            throw new InvalidOperationException($"A customer with mobile number '{request.MobileNumber}' already exists for this gym.");
        }

        if (!string.IsNullOrWhiteSpace(request.Email))
        {
            var existingEmail = await _context.Customers
                .AnyAsync(c => c.GymType == currentGym && c.Email == request.Email, ct);
            if (existingEmail)
            {
                throw new InvalidOperationException($"A customer with email '{request.Email}' already exists for this gym.");
            }
        }

        Guid rekazCustomerId;
        try
        {
            rekazCustomerId = await rekazClient.Customers.CreateCustomerAsync(
                new CreateRekazCustomerRequest(request.Name, request.MobileNumber, request.Email), ct);
        }
        catch (RekazApiException ex) when (IsRekazMobileNumberAlreadyExists(ex))
        {
            existingRekazCustomer = await rekazClient.Customers.GetCustomerByMobileNumberAsync(request.MobileNumber, ct);
            if (existingRekazCustomer != null)
            {
                await EnsureLocalCustomerExistsAsync(existingRekazCustomer, currentGym, ct);
                throw new InvalidOperationException($"A customer with mobile number '{request.MobileNumber}' already exists for this gym.");
            }

            throw;
        }

        Guid customerId;
        try
        {
            customerId = await _unitOfWork.ExecuteInTransactionAsync(async innerCt =>
            {
                var id = await _provisioningService.ProvisionLocalCustomerWithPasswordAsync(
                    rekazCustomerId, request.Name, request.MobileNumber, request.Email,
                    request.Password, CustomerSource.EmailRegistration, currentGym, ct: innerCt);
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

    private async Task EnsureLocalCustomerExistsAsync(
        RekazCustomerResult rekazCustomer,
        GymType gymType,
        CancellationToken ct)
    {
        var existingCustomer = await _context.Customers
            .AnyAsync(c => c.GymType == gymType && c.RekazCustomerId == rekazCustomer.Id, ct);

        if (existingCustomer)
            return;

        var email = !string.IsNullOrWhiteSpace(rekazCustomer.Email)
            ? rekazCustomer.Email
            : null;

        await _unitOfWork.ExecuteInTransactionAsync(async innerCt =>
        {
            await _provisioningService.ProvisionLocalCustomerAsync(
                rekazCustomer.Id,
                rekazCustomer.Name,
                rekazCustomer.MobileNumber,
                email,
                CustomerSource.LegacyRekazImport,
                gymType: gymType,
                ct: innerCt);

            await _context.SaveChangesAsync(innerCt);
            return true;
        }, ct);
    }

    private static bool IsRekazMobileNumberAlreadyExists(RekazApiException ex)
    {
        if (ex.StatusCode != HttpStatusCode.Forbidden)
            return false;

        try
        {
            using var doc = JsonDocument.Parse(ex.ResponseBody);
            if (!doc.RootElement.TryGetProperty("error", out var error))
                return false;

            if (!error.TryGetProperty("code", out var code))
                return false;

            return string.Equals(code.GetString(), "SP:Customer:MobileNumberAlreadyExists", StringComparison.Ordinal);
        }
        catch
        {
            return false;
        }
    }
}
