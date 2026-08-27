using System;
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
    private readonly IRekazCustomersClient _customersClient;
    private readonly ICustomerProvisioningService _provisioningService;

    public RekazCustomerSyncService(
        IApplicationDbContext context,
        IRekazCustomersClient customersClient,
        ICustomerProvisioningService provisioningService)
    {
        _context = context;
        _customersClient = customersClient;
        _provisioningService = provisioningService;
    }

    public async Task<Customer> EnsureLocalCustomerAsync(Guid rekazCustomerId, CancellationToken ct)
    {
        var existingCustomer = await _context.Customers
            .FirstOrDefaultAsync(c => c.RekazCustomerId == rekazCustomerId, ct);

        if (existingCustomer != null)
        {
            return existingCustomer;
        }

        var rekazCustomer = await _customersClient.GetCustomerByIdAsync(rekazCustomerId, ct)
            ?? throw new InvalidOperationException($"Rekaz customer {rekazCustomerId} could not be found via Rekaz API.");

        var customer = await _provisioningService.ProvisionLocalCustomerAsync(
            rekazCustomer.Id, rekazCustomer.Name, rekazCustomer.MobileNumber, rekazCustomer.Email,
            CustomerSource.LegacyRekazImport, ct);

        // ProvisionLocalCustomerAsync already adds the customer to the change tracker.
        // Returning the tracked (unsaved) entity.
        return customer;
    }
}
