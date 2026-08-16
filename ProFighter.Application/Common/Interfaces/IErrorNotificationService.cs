namespace ProFighter.Application.Common.Interfaces;

public interface IErrorNotificationService
{
    Task SendErrorNotificationAsync(string errorMessage, string? stackTrace = null);
}
