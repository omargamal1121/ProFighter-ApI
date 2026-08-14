using ProFighter.Domain.Entities;

namespace ProFighter.Application.Common.Interfaces;

public interface INotificationEmailService
{
    Task SendSyncFailureAlertAsync(CustomerSyncFailure failure, CancellationToken ct = default);
}
