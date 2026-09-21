using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ProFighter.Application.Common.Interfaces;
using ProFighter.Domain.Entities;
using ProFighter.Domain.Enums;

namespace ProFighter.Application.Customers.Services;

public class RekazCustomerSyncService : IRekazCustomerSyncService
{
    private readonly IApplicationDbContext _context;
    private readonly IRekazClientFactory _clientFactory;
    private readonly ICustomerProvisioningService _provisioningService;
    private readonly ILogger<RekazCustomerSyncService> _logger;

    public RekazCustomerSyncService(
        IApplicationDbContext context,
        IRekazClientFactory clientFactory,
        ICustomerProvisioningService provisioningService,
        ILogger<RekazCustomerSyncService> logger)
    {
        _context = context;
        _clientFactory = clientFactory;
        _provisioningService = provisioningService;
        _logger = logger;
    }

    public Task<Customer?> EnsureLocalCustomerAsync(Guid rekazCustomerId, CancellationToken ct)
    {
        return EnsureLocalCustomerAsync(rekazCustomerId, GymType.ProFighter, null, ct);
    }

    public Task<Customer?> EnsureLocalCustomerAsync(Guid rekazCustomerId, GymType gymType, CancellationToken ct = default)
    {
        return EnsureLocalCustomerAsync(rekazCustomerId, gymType, null, ct);
    }

    public async Task<Customer?> EnsureLocalCustomerAsync(
        Guid rekazCustomerId,
        GymType gymType,
        ISet<Guid>? negativeCache,
        CancellationToken ct = default)
    {
        if (negativeCache != null && negativeCache.Contains(rekazCustomerId))
        {
            return null;
        }

        // 1. Check ChangeTracker.Local first for unsaved, already-tracked entities
        var trackedCustomer = _context.Customers.Local
            .FirstOrDefault(c => c.RekazCustomerId == rekazCustomerId && c.GymType == gymType);

        if (trackedCustomer != null)
        {
            return trackedCustomer;
        }

        // 2. Query database set
        var existingCustomer = await _context.Customers
            .FirstOrDefaultAsync(c => c.RekazCustomerId == rekazCustomerId && c.GymType == gymType, ct);

        if (existingCustomer != null)
        {
            return existingCustomer;
        }

        // 3. Fetch from Rekaz using gym-specific client and provision
        var rekazClient = _clientFactory.GetClient(gymType);
        var rekazCustomer = await rekazClient.Customers.GetCustomerByIdAsync(rekazCustomerId, ct);
        if (rekazCustomer is null)
        {
            _logger.LogWarning("Skipping RekazId={RekazId} for {GymType}: customer not found in Rekaz (404).", rekazCustomerId, gymType);
            negativeCache?.Add(rekazCustomerId);
            return null;
        }

        if (string.IsNullOrWhiteSpace(rekazCustomer.MobileNumber))
        {
            _logger.LogWarning("Skipping RekazId={RekazId} for {GymType}: missing or empty mobile number.", rekazCustomerId, gymType);
            negativeCache?.Add(rekazCustomerId);
            return null;
        }

        var customer = await _provisioningService.ProvisionLocalCustomerAsync(
            rekazCustomer.Id,
            rekazCustomer.Name,
            rekazCustomer.MobileNumber,
            rekazCustomer.Email,
            CustomerSource.LegacyRekazImport,
            gymType,
            ct: ct);

        // ProvisionLocalCustomerAsync adds the customer entity to the ChangeTracker (unsaved).
        // Return the tracked entity without calling SaveChanges.
        return customer;
    }
}
