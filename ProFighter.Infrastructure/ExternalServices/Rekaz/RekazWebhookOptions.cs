namespace ProFighter.Infrastructure.ExternalServices.Rekaz;

public class RekazWebhookOptions
{
    public const string SectionName = "RekazWebhook";
    public string ReceiverPath { get; set; } = null!; // long, unguessable path segment for ProFighter
    public string ProGymReceiverPath { get; set; } = null!; // long, unguessable path segment for ProGym
}
