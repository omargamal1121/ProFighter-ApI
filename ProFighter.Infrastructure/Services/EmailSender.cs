using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace ProFighter.Infrastructure.Services;

public class EmailSender : IEmailSender
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailSender> _logger;

    public EmailSender(IConfiguration configuration, ILogger<EmailSender> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    private EmailSettings GetEmailSettings()
    {
        return new EmailSettings
        {
            Address = _configuration["Email:Address"] ?? throw new InvalidOperationException("Can't Find Email address"),
            Password = _configuration["Email:Password"] ?? throw new InvalidOperationException("Can't Find Email password"),
            Host = _configuration["Email:Host"] ?? throw new InvalidOperationException("Can't Find Email host"),
            Port = int.Parse(_configuration["Email:Port"] ?? throw new InvalidOperationException("Can't Find Email port"))
        };
    }

    public async Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        var settings = GetEmailSettings();

        var message = new MimeMessage();
        message.From.Add(MailboxAddress.Parse(settings.Address));
        message.To.Add(MailboxAddress.Parse(email));
        message.Subject = subject;
        message.Body = new TextPart("html") { Text = htmlMessage };

        using var client = new SmtpClient();
        try
        {
            await client.ConnectAsync(settings.Host, settings.Port, SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(settings.Address, settings.Password);
            await client.SendAsync(message);
            _logger.LogInformation("Email sent successfully to {Email} via MailKit", email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Email} via MailKit. Email={EmailAddress}, Host={Host}, Port={Port}",
                email, settings.Address, settings.Host, settings.Port);
            throw new InvalidOperationException(
                $"Failed to send email via MailKit. Email={settings.Address}, Host={settings.Host}, Port={settings.Port}",
                ex);
        }
        finally
        {
            await client.DisconnectAsync(true);
        }
    }

    private class EmailSettings
    {
        public string Address { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Host { get; set; } = string.Empty;
        public int Port { get; set; }
    }
}
