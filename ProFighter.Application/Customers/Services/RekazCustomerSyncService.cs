using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ProFighter.Application.Common.Interfaces;
using ProFighter.Domain.Entities;
using ProFighter.Domain.Enums;

namespace ProFighter.Application.Customers.Services;

public class RekazCustomerSyncService : IRekazCustomerSyncService
{
    private readonly IApplicationDbContext _context;
    private readonly IRekazClientFactory _clientFactory;
    private readonly ICustomerProvisioningService _provisioningService;

    public RekazCustomerSyncService(
        IApplicationDbContext context,
        IRekazClientFactory clientFactory,
        ICustomerProvisioningService provisioningService)
    {
        _context = context;
        _clientFactory = clientFactory;
        _provisioningService = provisioningService;
    }

    public Task<Customer> EnsureLocalCustomerAsync(Guid rekazCustomerId, CancellationToken ct)
    {
        return EnsureLocalCustomerAsync(rekazCustomerId, GymType.ProFighter, ct);
    }

    public async Task<Customer> EnsureLocalCustomerAsync(Guid rekazCustomerId, GymType gymType, CancellationToken ct = default)
    {
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
        var rekazCustomer = await rekazClient.Customers.GetCustomerByIdAsync(rekazCustomerId, ct)
            ?? throw new InvalidOperationException($"Rekaz customer {rekazCustomerId} could not be found via Rekaz API for {gymType}.");

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
