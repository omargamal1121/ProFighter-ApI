using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ProFighter.Application.Common.Interfaces;
using ProFighter.Application.Common.Models;
using ProFighter.Domain.Entities;
using ProFighter.Domain.Enums;

namespace ProFighter.Application.Subscriptions.Commands.SyncSubscriptions;

public class SyncSubscriptionsCommandHandler : IRequestHandler<SyncSubscriptionsCommand, SyncSubscriptionsResult>
{
    private readonly IApplicationDbContext _context;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IRekazSubscriptionsClient _subscriptionsClient;
    private readonly IRekazCustomersClient _customersClient;
    private readonly ICustomerProvisioningService _provisioningService;
    private readonly ILogger<SyncSubscriptionsCommandHandler> _logger;

    public SyncSubscriptionsCommandHandler(
        IApplicationDbContext context,
        IUnitOfWork unitOfWork,
        IRekazSubscriptionsClient subscriptionsClient,
        IRekazCustomersClient customersClient,
        ICustomerProvisioningService provisioningService,
        ILogger<SyncSubscriptionsCommandHandler> logger)
    {
        _context = context;
        _unitOfWork = unitOfWork;
        _subscriptionsClient = subscriptionsClient;
        _customersClient = customersClient;
        _provisioningService = provisioningService;
        _logger = logger;
    }

    public async Task<SyncSubscriptionsResult> Handle(SyncSubscriptionsCommand request, CancellationToken ct)
    {
        var totalProcessed = 0;
        var created = 0;
        var updated = 0;
        var skipped = 0;

        _logger.LogInformation("Starting subscription synchronization from Rekaz");

        try
        {
            // Fetch all subscriptions from Rekaz (using pagination)
            var skipCount = 0;
            const int maxResultCount = 100;

            while (true)
            {
                var query = new RekazSubscriptionsQuery(
                    MaxResultCount: maxResultCount,
                    SkipCount: skipCount);

                var rekazResult = await _subscriptionsClient.GetSubscriptionsAsync(query, ct);

                if (rekazResult.Items.Count == 0)
                {
                    _logger.LogInformation("No more subscriptions to sync. Total processed: {Total}", totalProcessed);
                    break;
                }

                _logger.LogInformation("Processing batch of {Count} subscriptions (starting from {Skip})", 
                    rekazResult.Items.Count, skipCount);

                foreach (var rekazSubscription in rekazResult.Items)
                {
                    try
                    {
                        var result = await SyncSingleSubscriptionAsync(rekazSubscription, ct);
                        totalProcessed++;

                        if (result == SyncResult.Created)
                            created++;
                        else if (result == SyncResult.Updated)
                            updated++;
                        else
                            skipped++;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to sync subscription {SubscriptionId}", rekazSubscription.Id);
                        skipped++;
                    }
                }

                skipCount += maxResultCount;

                // Break if we've processed all available items
                if (rekazResult.Items.Count < maxResultCount)
                {
                    _logger.LogInformation("Completed subscription synchronization. Total: {Total}, Created: {Created}, Updated: {Updated}, Skipped: {Skipped}",
                        totalProcessed, created, updated, skipped);
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fatal error during subscription synchronization");
            throw;
        }

        return new SyncSubscriptionsResult(totalProcessed, created, updated, skipped);
    }

    private async Task<SyncResult> SyncSingleSubscriptionAsync(RekazSubscriptionResult rekazSubscription, CancellationToken ct)
    {
        // Check if customer exists locally
        var customer = await _context.Customers
            .FirstOrDefaultAsync(c => c.RekazCustomerId == rekazSubscription.CustomerId, ct);

        if (customer is null)
        {
            // Customer doesn't exist locally - fetch from Rekaz and provision
            var rekazCustomer = await _customersClient.GetCustomerByIdAsync(rekazSubscription.CustomerId, ct);
            if (rekazCustomer is null)
            {
                _logger.LogWarning("Customer {CustomerId} not found in Rekaz for subscription {SubscriptionId}", 
                    rekazSubscription.CustomerId, rekazSubscription.Id);
                return SyncResult.Skipped;
            }

            customer = await _provisioningService.ProvisionLocalCustomerAsync(
                rekazCustomer.Id, rekazCustomer.Name, rekazCustomer.MobileNumber, rekazCustomer.Email,
                CustomerSource.LegacyRekazImport, ct);
        }

      
        var existingSubscription = await _context.Subscriptions
            .FirstOrDefaultAsync(s => s.RekazSubscriptionId == rekazSubscription.Id, ct);

        if (existingSubscription is null)
        {
              var newSubscription = new Subscription(
                id: Guid.NewGuid(),
                customerId: customer.Id,
                rekazSubscriptionId: rekazSubscription.Id,
                type: SubscriptionType.MartialArts, // TODO: real priceId→Type mapping
                startDate: rekazSubscription.StartAt,
                price: rekazSubscription.TotalAmount);
            newSubscription.SyncFromRekaz(rekazSubscription.Status, rekazSubscription.StartAt, rekazSubscription.EndAt, rekazSubscription.TotalAmount);
            _context.Subscriptions.Add(newSubscription);
            await _context.SaveChangesAsync(ct);

            _logger.LogDebug("Created local subscription for Rekaz subscription {SubscriptionId}", rekazSubscription.Id);
            return SyncResult.Created;
        }
        else
        {
            // Update existing subscription
            existingSubscription.SyncFromRekaz(rekazSubscription.Status, rekazSubscription.StartAt, rekazSubscription.EndAt, rekazSubscription.TotalAmount);
            await _context.SaveChangesAsync(ct);

            _logger.LogDebug("Updated local subscription for Rekaz subscription {SubscriptionId}", rekazSubscription.Id);
            return SyncResult.Updated;
        }
    }

    private enum SyncResult
    {
        Created,
        Updated,
        Skipped
    }
}
