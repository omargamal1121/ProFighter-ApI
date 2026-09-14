using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ProFighter.Application.Common.Interfaces;
using ProFighter.Application.Common.Models;
using ProFighter.Domain.Entities;
using ProFighter.Domain.Enums;

namespace ProFighter.Application.Customers.Commands.SyncCustomers;

public class SyncCustomersCommandHandler : IRequestHandler<SyncCustomersCommand, SyncCustomersResult>
{
    private readonly IApplicationDbContext _context;
    private readonly IRekazCustomersClient _customersClient;
    private readonly ICustomerProvisioningService _provisioningService;
    private readonly ILogger<SyncCustomersCommandHandler> _logger;

    public SyncCustomersCommandHandler(
        IApplicationDbContext context,
        IRekazCustomersClient customersClient,
        ICustomerProvisioningService provisioningService,
        ILogger<SyncCustomersCommandHandler> logger)
    {
        _context = context;
        _customersClient = customersClient;
        _provisioningService = provisioningService;
        _logger = logger;
    }

    public async Task<SyncCustomersResult> Handle(SyncCustomersCommand request, CancellationToken ct)
    {
        var totalProcessed = 0;
        var created = 0;
        var updated = 0;
        var skipped = 0;
        var errors = 0;

        _logger.LogInformation("Starting customer synchronization from Rekaz");

        var skipCount = 0;
        const int maxResultCount = 100;

        while (true)
        {
            RekazCustomersListResult rekazResult;
            try
            {
                var query = new RekazCustomersQuery(
                    MaxResultCount: maxResultCount,
                    SkipCount: skipCount);

                rekazResult = await _customersClient.GetCustomersAsync(query, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch customer batch from Rekaz at SkipCount {SkipCount}", skipCount);
                throw;
            }

            if (rekazResult.Items.Count == 0)
            {
                break;
            }

            _logger.LogInformation("Processing customer batch of {Count} (starting at offset {SkipCount})", rekazResult.Items.Count, skipCount);

            foreach (var rekazCustomer in rekazResult.Items)
            {
                totalProcessed++;
                try
                {
                    var syncResult = await SyncSingleCustomerAsync(rekazCustomer, ct);
                    if (syncResult == CustomerSyncItemResult.Created)
                        created++;
                    else if (syncResult == CustomerSyncItemResult.Updated)
                        updated++;
                    else
                        skipped++;
                }
                catch (Exception ex)
                {
                    errors++;
                    _logger.LogError(ex, "Error syncing Rekaz customer {RekazCustomerId} ({Name}, Mobile: {MobileNumber})",
                        rekazCustomer.Id, rekazCustomer.Name, rekazCustomer.MobileNumber);
                }
            }

            skipCount += maxResultCount;
            if (rekazResult.Items.Count < maxResultCount)
            {
                break;
            }
        }

        _logger.LogInformation(
            "Completed customer synchronization. Total: {Total}, Created: {Created}, Updated: {Updated}, Skipped: {Skipped}, Errors: {Errors}",
            totalProcessed, created, updated, skipped, errors);

        return new SyncCustomersResult(totalProcessed, created, updated, skipped, errors);
    }

    private async Task<CustomerSyncItemResult> SyncSingleCustomerAsync(RekazCustomerResult rekazCustomer, CancellationToken ct)
    {
        var existingCustomer = await _context.Customers
            .FirstOrDefaultAsync(c => c.RekazCustomerId == rekazCustomer.Id, ct);

        if (existingCustomer == null)
        {
            var normalizedUserName = rekazCustomer.MobileNumber.StartsWith("+")
                ? rekazCustomer.MobileNumber[1..]
                : rekazCustomer.MobileNumber;

            existingCustomer = await _context.Customers
                .FirstOrDefaultAsync(c => c.MobileNumber == rekazCustomer.MobileNumber || c.MobileNumber == normalizedUserName, ct);
        }

        if (existingCustomer == null)
        {
            await _provisioningService.ProvisionLocalCustomerAsync(
                rekazCustomer.Id, rekazCustomer.Name, rekazCustomer.MobileNumber, rekazCustomer.Email,
                CustomerSource.LegacyRekazImport, ct: ct);
            await _context.SaveChangesAsync(ct);
            return CustomerSyncItemResult.Created;
        }

        var profileChanged = false;
        if (existingCustomer.Name != rekazCustomer.Name || existingCustomer.MobileNumber != rekazCustomer.MobileNumber || existingCustomer.Email != rekazCustomer.Email)
        {
            existingCustomer.UpdateProfile(rekazCustomer.Name, rekazCustomer.MobileNumber, rekazCustomer.Email);
            profileChanged = true;
        }

        if (existingCustomer.RekazCustomerId != rekazCustomer.Id)
        {
            existingCustomer.SyncRekazId(rekazCustomer.Id);
            profileChanged = true;
        }

        if (profileChanged)
        {
            await _context.SaveChangesAsync(ct);
            return CustomerSyncItemResult.Updated;
        }

        return CustomerSyncItemResult.Skipped;
    }

    private enum CustomerSyncItemResult
    {
        Created,
        Updated,
        Skipped
    }
}
