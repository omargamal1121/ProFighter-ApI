using ProFighter.Domain.Common;
using ProFighter.Domain.Enums;

namespace ProFighter.Domain.Entities;

public class CustomerSyncFailure : BaseEntity
{
    public Guid RekazCustomerId { get; private set; }
    public string PayloadJson { get; private set; }
    public string ErrorMessage { get; private set; }
    public string Status { get; private set; } // e.g. "Pending"
    public GymType GymType { get; private set; } = GymType.ProFighter;

    // EF Core Constructor
    private CustomerSyncFailure() : base()
    {
        PayloadJson = null!;
        ErrorMessage = null!;
        Status = "Pending";
    }

    public CustomerSyncFailure(Guid rekazCustomerId, string payloadJson, string errorMessage, string status = "Pending", GymType gymType = GymType.ProFighter) : base()
    {
        RekazCustomerId = rekazCustomerId;
        PayloadJson = payloadJson;
        ErrorMessage = errorMessage;
        Status = status;
        GymType = gymType;
        CreatedAt = DateTime.UtcNow;
    }
}
