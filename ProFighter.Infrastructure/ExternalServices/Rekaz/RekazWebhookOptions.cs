namespace ProFighter.Infrastructure.ExternalServices.Rekaz;

public class RekazWebhookOptions
{
    public const string SectionName = "RekazWebhook";
    public string ReceiverPath { get; set; } = null!; // long, unguessable path segment (e.g. a GUID string), read from config/User Secrets — never hardcoded, logged, or exposed in API docs
}
