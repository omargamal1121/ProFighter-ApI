using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ProFighter.Application.Common.Extensions;
using ProFighter.Application.Common.Interfaces;
using ProFighter.Application.Common.Models;

namespace ProFighter.Application.Subscriptions.Commands.CreateSubscription;

public class CreateSubscriptionCommandHandler : IRequestHandler<CreateSubscriptionCommand, CreateSubscriptionResult>
{
    private readonly IApplicationDbContext _context;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IRekazClientFactory _rekazClientFactory;
    private readonly ICurrentGymContext _gymContext;
    private readonly IGymSettingsService _gymSettings;
    private readonly ILogger<CreateSubscriptionCommandHandler> _logger;

    public CreateSubscriptionCommandHandler(
        IApplicationDbContext context,
        IUnitOfWork unitOfWork,
        IRekazClientFactory rekazClientFactory,
        ICurrentGymContext gymContext,
        IGymSettingsService gymSettings,
        ILogger<CreateSubscriptionCommandHandler> logger)
    {
        _context = context;
        _unitOfWork = unitOfWork;
        _rekazClientFactory = rekazClientFactory;
        _gymContext = gymContext;
        _gymSettings = gymSettings;
        _logger = logger;
    }

    public async Task<CreateSubscriptionResult> Handle(CreateSubscriptionCommand request, CancellationToken ct)
    {
        var gymType = _gymContext.CurrentGymType;

        var customer = await _context.Customers
            .ForCurrentGym(_gymContext)
            .FirstOrDefaultAsync(c => c.Id == request.CustomerId, ct)
            ?? throw new InvalidOperationException("Customer not found.");

        if (customer.RekazCustomerId is null)
            throw new InvalidOperationException("Customer is not yet synced with Rekaz — cannot create a subscription.");

        // Filter by plan Name (not Type) — each gym's plan names are distinct.
        var existing = await _context.Subscriptions
            .Where(s => s.CustomerId == customer.Id && s.Name == request.PlanName)
            .ForCurrentGym(_gymContext)
            .OrderByDescending(s => s.EndDate)
            .FirstOrDefaultAsync(ct);

        if (existing is not null && existing.Status == "Pending")
        {
            return new CreateSubscriptionResult(
                existing.RekazInvoiceId ?? Guid.Empty,
                existing.PaymentLink ?? string.Empty,
                IsRenewalQueued: false,
                EffectiveStartAt: existing.StartDate);
        }

        DateTime startAt;
        var isRenewal = false;
        if (existing is not null && existing.Status == "Active"
            && existing.EndDate.HasValue && existing.EndDate.Value > DateTime.UtcNow)
        {
            startAt = existing.EndDate.Value;
            isRenewal = true;
        }
        else
        {
            startAt = DateTime.UtcNow;
        }

        // Resolve BranchId from per-gym config (Rekaz:{GymTypeName}:BranchId in appsettings / env vars).
        var branchId = _gymSettings.GetBranchId(gymType);

        _logger.LogInformation(
            "Creating Rekaz subscription for Customer={CustomerId} GymType={GymType} PlanName={PlanName} BranchId={BranchId} IsRenewal={IsRenewal}",
            customer.Id, gymType, request.PlanName, branchId, isRenewal);

        var rekazSubscriptionsClient = _rekazClientFactory.GetClient(gymType).Subscriptions;
        var rekazResult = await rekazSubscriptionsClient.CreateSubscriptionAsync(new CreateRekazSubscriptionRequest(
            CustomerId: customer.RekazCustomerId,
            NewCustomerDetails: null,
            StartAt: startAt,
            BranchId: branchId,
            Items: new List<RekazSubscriptionItemInput> { new(request.PriceId, request.Quantity) },
            OccurenceDays: null,
            Discount: null
        ), ct);

        var fullPaymentLink = "https://platform.rekaz.io" + rekazResult.PaymentLink;

        return new CreateSubscriptionResult(rekazResult.InvoiceId, fullPaymentLink, isRenewal, startAt);
    }
}
