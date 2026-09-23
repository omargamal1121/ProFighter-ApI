using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ProFighter.Application.Common.Interfaces;
using ProFighter.Application.Common.Models;
using ProFighter.Application.Subscriptions.Jobs;
using ProFighter.Domain.Enums;

namespace ProFighter.Application.Subscriptions.Services;

public class RekazTransactionEventHandler : IRekazTransactionEventHandler
{
    private readonly IApplicationDbContext _context;
    private readonly IRekazClientFactory _rekazClientFactory;
    private readonly IRekazCustomerSyncService _customerSyncService;
    private readonly INotificationService _notificationService;
    private readonly IBackgroundJobClient _backgroundJobs;
    private readonly IExceptionLogFormatter _logFormatter;
    private readonly ILogger<RekazTransactionEventHandler> _logger;

    public RekazTransactionEventHandler(
        IApplicationDbContext context,
        IRekazClientFactory rekazClientFactory,
        IRekazCustomerSyncService customerSyncService,
        INotificationService notificationService,
        IBackgroundJobClient backgroundJobs,
        IExceptionLogFormatter logFormatter,
        ILogger<RekazTransactionEventHandler> logger)
    {
        _context = context;
        _rekazClientFactory = rekazClientFactory;
        _customerSyncService = customerSyncService;
        _notificationService = notificationService;
        _backgroundJobs = backgroundJobs;
        _logFormatter = logFormatter;
        _logger = logger;
    }

    public async Task HandleAsync(Guid transactionId, string eventName, GymType gymType = GymType.ProFighter, CancellationToken ct = default)
    {
        var fetchResult = await FetchTransactionAsync(transactionId, gymType, ct);
        if (fetchResult is null)
        {
            _logger.LogWarning("Transaction {TransactionId} not found on re-fetch — allowing Hangfire retry", transactionId);
            throw new InvalidOperationException($"Transaction {transactionId} not found in Rekaz - allowing retry mechanism to handle");
        }

        var (fetched, resolvedGymType) = fetchResult.Value;

        // Ensure customer exists locally using explicit GymType
        var customer = await _customerSyncService.EnsureLocalCustomerAsync(fetched.CustomerId, resolvedGymType, ct);
        if (customer is null)
        {
            _logger.LogInformation("Rekaz webhook event {EventName} for {GymType} processed: TransactionId={TransactionId}, Result=Skipped (CustomerMissing)", eventName, resolvedGymType, transactionId);
            return;
        }

        // Check if transaction contains at least one item of type "Subscription" (or with product/price ID)
        var subscriptionItems = fetched.Items?
            .Where(i => string.Equals(i.Type, "Subscription", StringComparison.OrdinalIgnoreCase) ||
                        (string.IsNullOrWhiteSpace(i.Type) && (i.ProductId.HasValue || i.PriceId.HasValue)))
            .ToList();

        // Dedup marker: check if we already processed this specific transaction ID for notifications
        var alreadyProcessed = await _context.RekazWebhookInboxEntries
            .AnyAsync(e => e.Id == fetched.Id && e.EventName == "TransactionPaidNotification", ct);

        if (alreadyProcessed)
        {
            _logger.LogInformation("Rekaz webhook event {EventName} for {GymType} processed: TransactionId={TransactionId}, Result=Skipped (AlreadyProcessed)", eventName, resolvedGymType, transactionId);
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
                "{}",
                resolvedGymType
            );
            marker.MarkProcessed();
            try
            {
                _context.RekazWebhookInboxEntries.Add(marker);
                await _context.SaveChangesAsync(ct);
            }
            catch (DbUpdateException ex)
            {
                var formattedError = _logFormatter.ToOneLine(ex);
                _logger.LogWarning("DbUpdateException on SaveChangesAsync for transaction {TransactionId} webhook event {EventName}: {Error}. Clearing ChangeTracker and allowing Hangfire retry.", transactionId, eventName, formattedError);
                foreach (var entry in ((DbContext)_context).ChangeTracker.Entries().ToList())
                {
                    entry.State = EntityState.Detached;
                }
                throw;
            }

            _logger.LogInformation("Rekaz webhook event {EventName} for {GymType} processed: TransactionId={TransactionId}, Result=PaidNotificationSent", eventName, resolvedGymType, transactionId);
        }
        else
        {
            _logger.LogInformation("Rekaz webhook event {EventName} for {GymType} processed: TransactionId={TransactionId}, Result=Unchanged (PaymentStatus={PaymentStatus})", eventName, resolvedGymType, transactionId, fetched.PaymentStatus);
        }
    }

    private async Task<(RekazTransactionResult Transaction, GymType GymType)?> FetchTransactionAsync(Guid transactionId, GymType preferredGymType, CancellationToken ct)
    {
        try
        {
            var client = _rekazClientFactory.GetClient(preferredGymType);
            var fetched = await client.Transactions.GetTransactionByIdAsync(transactionId, ct);
            if (fetched is not null)
            {
                return (fetched, preferredGymType);
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to fetch transaction {TransactionId} for GymType {GymType}", transactionId, preferredGymType);
        }

        Exception? lastException = null;
        foreach (var fallbackGym in Enum.GetValues<GymType>())
        {
            if (fallbackGym == preferredGymType) continue;
            try
            {
                var client = _rekazClientFactory.GetClient(fallbackGym);
                var fetched = await client.Transactions.GetTransactionByIdAsync(transactionId, ct);
                if (fetched is not null)
                {
                    return (fetched, fallbackGym);
                }
            }
            catch (Exception ex)
            {
                lastException = ex;
                _logger.LogDebug(ex, "Failed fallback fetch for transaction {TransactionId} on GymType {GymType}", transactionId, fallbackGym);
            }
        }

        if (lastException is not null && lastException is not ProFighter.Application.Common.Exceptions.RekazApiException { StatusCode: System.Net.HttpStatusCode.NotFound })
        {
            var formattedError = _logFormatter.ToOneLine(lastException);
            _logger.LogWarning("Exception fetching transaction {TransactionId} across all gyms: {Error}", transactionId, formattedError);
        }

        return null;
    }
}
