using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ProFighter.Application.Common.Interfaces;
using ProFighter.Application.Common.Models;

namespace ProFighter.Application.Customers.Commands.ImportCustomersFromRekaz;

// Orchestrates: External API pagination loop, existence check, and local provisioning in batches.
public class ImportCustomersFromRekazCommandHandler : IRequestHandler<ImportCustomersFromRekazCommand, ImportCustomersResult>
{
    private readonly IRekazCustomersClient _rekazCustomersClient;
    private readonly ICustomerProvisioningService _provisioningService;
    private readonly IApplicationDbContext _context;
    private readonly ILogger<ImportCustomersFromRekazCommandHandler> _logger;

    public ImportCustomersFromRekazCommandHandler(
        IRekazCustomersClient rekazCustomersClient,
        ICustomerProvisioningService provisioningService,
        IApplicationDbContext context,
        ILogger<ImportCustomersFromRekazCommandHandler> logger)
    {
        _rekazCustomersClient = rekazCustomersClient;
        _provisioningService = provisioningService;
        _context = context;
        _logger = logger;
    }

    public async Task<ImportCustomersResult> Handle(ImportCustomersFromRekazCommand request, CancellationToken cancellationToken)
    {
        int skipCount = 0;
        const int maxResultCount = 100;
        int totalFetched = 0;
        int imported = 0;
        int skipped = 0;
        int failed = 0;
        var errors = new List<string>();

        const int safetyLimit = 200;
        int iterations = 0;
        bool hasMore = true;

        while (hasMore)
        {
            iterations++;
            if (iterations > safetyLimit)
            {
                _logger.LogWarning("Safety limit of {SafetyLimit} iterations reached during Rekaz customer import. Terminating loop.", safetyLimit);
                break;
            }

            try
            {
                // 1. Fetch page from external API
                var query = new RekazCustomersQuery(SkipCount: skipCount, MaxResultCount: maxResultCount);
                var response = await _rekazCustomersClient.GetCustomersAsync(query, cancellationToken);
                
                if (response == null || response.Items == null || response.Items.Count == 0)
                {
                    break;
                }

                totalFetched += response.Items.Count;

                // 2. Process each customer
                foreach (var customerDto in response.Items)
                {
                    try
                    {
                        var exists = await _context.Customers.AnyAsync(c => c.RekazCustomerId == customerDto.Id, cancellationToken);
                        if (exists)
                        {
                            skipped++;
                            continue;
                        }

                        // Provision locally (adds to DbContext tracked entities)
                        await _provisioningService.ProvisionFromRekazAsync(
                            customerDto.Id,
                            customerDto.Name,
                            customerDto.MobileNumber,
                            customerDto.Email,
                            cancellationToken
                        );

                        imported++;
                    }
                    catch (Exception ex)
                    {
                        failed++;
                        _logger.LogError(ex, "Failed to import Rekaz customer with ID {RekazCustomerId}", customerDto.Id);
                        errors.Add($"Failed to import customer {customerDto.Name} (Mobile: {customerDto.MobileNumber}): {ex.Message}");
                    }
                }

                // 3. Persist the batch of successfully provisioned customers
                if (imported > 0)
                {
                    await _context.SaveChangesAsync(cancellationToken);
                }

                skipCount += maxResultCount;
                hasMore = response.Items.Count == maxResultCount && skipCount < response.TotalCount;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch page of customers from Rekaz at SkipCount {SkipCount}", skipCount);
                errors.Add($"Failed to fetch batch from Rekaz: {ex.Message}");
                break; // Stop paginating if the external service fails completely
            }
        }

        return new ImportCustomersResult(totalFetched, imported, skipped, failed, errors);
    }
}
