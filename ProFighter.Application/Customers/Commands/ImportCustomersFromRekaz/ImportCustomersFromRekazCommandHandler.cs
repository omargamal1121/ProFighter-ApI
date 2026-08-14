using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ProFighter.Application.Common.Interfaces;
using ProFighter.Application.Common.Models;
using ProFighter.Domain.Enums;

namespace ProFighter.Application.Customers.Commands.ImportCustomersFromRekaz;

public class ImportCustomersFromRekazCommandHandler : IRequestHandler<ImportCustomersFromRekazCommand, ImportCustomersResult>
{
    private readonly IRekazCustomersClient _rekazCustomersClient;
    private readonly IApplicationDbContext _context;
    private readonly ICustomerProvisioningService _provisioningService;
    private readonly ILogger<ImportCustomersFromRekazCommandHandler> _logger;

    public ImportCustomersFromRekazCommandHandler(
        IRekazCustomersClient rekazCustomersClient,
        IApplicationDbContext context,
        ICustomerProvisioningService provisioningService,
        ILogger<ImportCustomersFromRekazCommandHandler> logger)
    {
        _rekazCustomersClient = rekazCustomersClient;
        _context = context;
        _provisioningService = provisioningService;
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

        const int safetyLimit = 200; // 200 iterations * 100 = 20,000 customers safety cap
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
                var query = new RekazCustomersQuery(
                    SkipCount: skipCount,
                    MaxResultCount: maxResultCount
                );

                var response = await _rekazCustomersClient.GetCustomersAsync(query, cancellationToken);
                if (response == null || response.Items == null || response.Items.Count == 0)
                {
                    break;
                }

                totalFetched += response.Items.Count;

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

                        await _provisioningService.ProvisionLocalCustomerAsync(
                            customerDto.Id,
                            customerDto.Name,
                            customerDto.MobileNumber,
                            customerDto.Email,
                            CustomerSource.LegacyRekazImport,
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

        if (imported > 0)
        {
            try
            {
                await _context.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Fatal error saving imported customer entities to the database.");
                errors.Add($"Critical DB Save failure: {ex.Message}");
                // Adjust counts since the commit failed
                failed += imported;
                imported = 0;
            }
        }

        return new ImportCustomersResult(totalFetched, imported, skipped, failed, errors);
    }
}
