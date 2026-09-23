using System.Net.Http.Json;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ProFighter.Infrastructure.Services;

public class EmailSender : IEmailSender
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailSender> _logger;

    public EmailSender(HttpClient httpClient, IConfiguration configuration, ILogger<EmailSender> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        var apiKey = _configuration["Brevo:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            _logger.LogError("Brevo API key missing in configuration ('Brevo:ApiKey').");
            throw new InvalidOperationException("Brevo API key missing in configuration ('Brevo:ApiKey').");
        }

        var senderEmail = _configuration["Brevo:SenderEmail"] ?? _configuration["Email:Address"] ?? "no-reply@profighter.com";
        var senderName = _configuration["Brevo:SenderName"] ?? "ProFighter Gym";

        var payload = new
        {
            sender = new { email = senderEmail, name = senderName },
            to = new[] { new { email } },
            subject,
            htmlContent = htmlMessage.StartsWith("<html>", StringComparison.OrdinalIgnoreCase)
                ? htmlMessage
                : $"<html><body>{htmlMessage}</body></html>"
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.brevo.com/v3/smtp/email")
        {
            Content = JsonContent.Create(payload)
        };
        request.Headers.Add("api-key", apiKey);

        try
        {
            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogError("Brevo email failed with status {StatusCode}: {Error}", response.StatusCode, error);
                throw new InvalidOperationException($"Brevo email failed: {error}");
            }

            _logger.LogInformation("Email sent successfully via Brevo to {Email}", email);
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            var formattedError = ProFighter.Infrastructure.Logging.ExceptionLogFormatter.ToOneLine(ex);
            _logger.LogError("Unexpected error occurred while sending email via Brevo to {Email}: {Error}", email, formattedError);
            throw new InvalidOperationException($"Failed to send email via Brevo to {email}", ex);
        }
    }
}
