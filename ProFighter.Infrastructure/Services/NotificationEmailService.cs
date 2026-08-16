using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ProFighter.Application.Common.Interfaces;
using ProFighter.Domain.Entities;

namespace ProFighter.Infrastructure.Services;

public class NotificationEmailService : INotificationEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<NotificationEmailService> _logger;

    public NotificationEmailService(IConfiguration configuration, ILogger<NotificationEmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public Task SendSyncFailureAlertAsync(CustomerSyncFailure failure, CancellationToken ct = default)
    {
        var adminEmail = _configuration["Notifications:AdminEmail"];
        var smtpHost = _configuration["Smtp:Host"];

        _logger.LogInformation("Sending sync failure alert to admin email: {AdminEmail} for Rekaz customer ID: {RekazCustomerId}", 
            adminEmail, failure.RekazCustomerId);
        return Task.CompletedTask;
    }
}
