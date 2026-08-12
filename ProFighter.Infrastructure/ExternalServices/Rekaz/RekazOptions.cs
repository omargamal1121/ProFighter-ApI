namespace ProFighter.Infrastructure.ExternalServices.Rekaz;

public class RekazOptions
{
    public const string SectionName = "Rekaz";

    public string BaseUrl { get; set; } = "https://platform.rekaz.io";

   
    public string ApiKeyBase64 { get; set; } = null!;

   
    public string TenantId { get; set; } = null!;
}
