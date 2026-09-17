using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ProFighter.Application.Common.Interfaces;

using ProFighter.Domain.Enums;
using ProFighter.Application.Common.Models;

namespace ProFighter.Application.Subscriptions.Services;

public class RekazTransactionEventHandler : IRekazTransactionEventHandler
{
    private readonly IApplicationDbContext _context;
    private readonly IRekazClientFactory _rekazClientFactory;
    private readonly IRekazCustomerSyncService _customerSyncService;
    private readonly INotificationService _notificationService;
    private readonly ILogger<RekazTransactionEventHandler> _logger;

    public RekazTransactionEventHandler(
        IApplicationDbContext context,
        IRekazClientFactory rekazClientFactory,
        IRekazCustomerSyncService customerSyncService,
        INotificationService notificationService,
        ILogger<RekazTransactionEventHandler> logger)
    {
        _context = context;
        _rekazClientFactory = rekazClientFactory;
        _customerSyncService = customerSyncService;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task HandleAsync(Guid transactionId, string eventName, CancellationToken ct)
    {
        var fetched = await FetchTransactionAsync(transactionId, ct);
        if (fetched is null)
        {
            _logger.LogWarning("Transaction {TransactionId} not found on re-fetch — allowing Hangfire retry", transactionId);
            throw new InvalidOperationException($"Transaction {transactionId} not found in Rekaz - allowing retry mechanism to handle");
        }

        // Ensure customer exists locally
        var customer = await _customerSyncService.EnsureLocalCustomerAsync(fetched.CustomerId, ct);

        // Dedup marker: check if we already processed this specific transaction ID for notifications
        var alreadyProcessed = await _context.RekazWebhookInboxEntries
            .AnyAsync(e => e.Id == fetched.Id && e.EventName == "TransactionPaidNotification", ct);

        if (alreadyProcessed)
        {
            _logger.LogInformation("Transaction {TransactionId} was already processed for notifications. Skipping.", fetched.Id);
            return;
        }

        // Processing rule: paymentStatus == "Paid", optionally status == "Confirmed", remainingAmount == 0
        if (fetched.PaymentStatus.Equals("Paid", StringComparison.OrdinalIgnoreCase) && 
            fetched.RemainingAmount == 0)
        {
            var dataPayload = new Dictionary<string, string>
            {
                { "transactionId", fetched.Id.ToString() },
                { "paidAmount", fetched.PaidAmount.ToString("F2") },
                { "currency", fetched.Currency }
            };

            var itemName = fetched.Items?.FirstOrDefault()?.NameAr ?? fetched.Items?.FirstOrDefault()?.NameEn ?? "الاشتراك";
            var body = $"تم دفع {fetched.PaidAmount} {fetched.Currency} لـ {itemName}";

            await _notificationService.SendToUserAsync(
                customer.Id, 
                "Payment Received / تم استلام الدفعة", 
                body, 
                dataPayload, 
                ct);

            // Insert dedup marker
            var marker = new ProFighter.Domain.Entities.RekazWebhookInboxEntry(
                fetched.Id, 
                "TransactionPaidNotification", 
                "{}"
            );
            marker.MarkProcessed();
            _context.RekazWebhookInboxEntries.Add(marker);
            await _context.SaveChangesAsync(ct);
        }
        
        _logger.LogInformation("Processed transaction {TransactionId} via webhook event {EventName}", transactionId, eventName);
    }

    private async Task<RekazTransactionResult?> FetchTransactionAsync(Guid transactionId, CancellationToken ct)
    {
        Exception? lastException = null;
        foreach (var gymType in Enum.GetValues<GymType>())
        {
            try
            {
                var client = _rekazClientFactory.GetClient(gymType);
                var fetched = await client.Transactions.GetTransactionByIdAsync(transactionId, ct);
                if (fetched is not null)
                {
                    return fetched;
                }
            }
            catch (Exception ex)
            {
                lastException = ex;
                _logger.LogDebug(ex, "Failed to fetch transaction {TransactionId} for GymType {GymType}", transactionId, gymType);
            }
        }

        if (lastException is not null && lastException is not ProFighter.Application.Common.Exceptions.RekazApiException { StatusCode: System.Net.HttpStatusCode.NotFound })
        {
            _logger.LogWarning(lastException, "Exception fetching transaction {TransactionId} across all gyms", transactionId);
        }

        return null;
    }
}
