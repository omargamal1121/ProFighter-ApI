namespace ProFighter.Application.Common.Models;

// ---- Create Request Models ----

public record CreateRekazSubscriptionRequest(
    Guid? CustomerId,
    RekazNewCustomerDetails? NewCustomerDetails,
    DateTime? StartAt,
    Guid? BranchId,
    List<RekazSubscriptionItemInput> Items,
    List<int>? OccurenceDays,
    RekazDiscountInput? Discount
);

public record RekazNewCustomerDetails(
    string Name,
    string MobileNumber,
    string? Email = null,
    string? CompanyName = null
);

public record RekazSubscriptionItemInput(
    Guid PriceId,
    int Quantity,
    Guid? LoyaltyRewardId = null
);

public record RekazDiscountInput(int Type, decimal Value);

public record RekazSubscriptionCreatedResult(Guid InvoiceId, string PaymentLink);

// ---- Get list / detail Models ----

public record RekazSubscriptionsQuery(
    int MaxResultCount = 20,
    Guid? CustomerId = null,
    DateTime? StartAtMin = null,
    DateTime? StartAtMax = null,
    DateTime? NextBillingAtMin = null,
    DateTime? NextBillingAtMax = null,
    List<string>? Statuses = null,
    string? CustomerMobile = null,
    string? Keyword = null,
    List<Guid>? PriceIds = null,
    Guid? BranchId = null,
    string? Sorting = null,
    int SkipCount = 0
);

public record RekazSubscriptionResult(
    Guid Id,
    string SubscriptionCode,
    Guid CustomerId,
    DateTime StartAt,
    DateTime? EndAt,
    string Status,
    decimal PaidAmount,
    decimal TotalAmount,
    decimal RemainingAmount,
    bool IsPaused,
    DateTime? PausedAt,
    DateTime? ResumeAt
)
{
    public bool IsFullyPaid => RemainingAmount <= 0;
}

public record RekazSubscriptionsListResult(List<RekazSubscriptionResult> Items, long TotalCount);
