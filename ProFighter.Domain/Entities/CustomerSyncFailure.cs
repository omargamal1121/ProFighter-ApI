using ProFighter.Domain.Common;

namespace ProFighter.Domain.Entities;

public class CustomerSyncFailure : BaseEntity
{
    public Guid RekazCustomerId { get; private set; }
    public string PayloadJson { get; private set; }
    public string ErrorMessage { get; private set; }
    public string Status { get; private set; } // e.g. "Pending"

    // EF Core Constructor
    private CustomerSyncFailure() : base()
    {
        PayloadJson = null!;
        ErrorMessage = null!;
        Status = "Pending";
    }

    public CustomerSyncFailure(Guid rekazCustomerId, string payloadJson, string errorMessage, string status = "Pending") : base()
    {
        RekazCustomerId = rekazCustomerId;
        PayloadJson = payloadJson;
        ErrorMessage = errorMessage;
        Status = status;
        CreatedAt = DateTime.UtcNow;
    }
}
