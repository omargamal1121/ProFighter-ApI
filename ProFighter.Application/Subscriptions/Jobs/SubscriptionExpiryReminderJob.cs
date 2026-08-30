using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ProFighter.Application.Common.Interfaces;

namespace ProFighter.Application.Subscriptions.Jobs;

public class SubscriptionExpiryReminderJob
{
    private readonly IApplicationDbContext _context;
    private readonly INotificationService _notificationService;
    private readonly ILogger<SubscriptionExpiryReminderJob> _logger;

    public SubscriptionExpiryReminderJob(
        IApplicationDbContext context,
        INotificationService notificationService,
        ILogger<SubscriptionExpiryReminderJob> logger)
    {
        _context = context;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task RunAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Starting SubscriptionExpiryReminderJob");

        var reminderThreshold = DateTime.UtcNow.AddDays(3);
        var now = DateTime.UtcNow;

        var expiringSubscriptions = await _context.Subscriptions
            .Where(s => s.Status == "Active" && s.EndDate.HasValue && s.EndDate.Value > now && s.EndDate.Value <= reminderThreshold)
            .ToListAsync(ct);

        foreach (var sub in expiringSubscriptions)
        {
            var daysLeft = (sub.EndDate!.Value - now).Days;
            var body = daysLeft == 0 ? "Your subscription expires today!" : $"Your subscription expires in {daysLeft} days.";

            await _notificationService.SendToUserAsync(
                sub.CustomerId,
                "Subscription Expiring Soon",
                body,
                null,
                ct);
        }

        _logger.LogInformation("Finished SubscriptionExpiryReminderJob. Sent {Count} reminders.", expiringSubscriptions.Count);
    }
}
