using System;
using System.Threading;
using System.Threading.Tasks;

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ProFighter.Application.Common.Interfaces;

public record RekazTransactionItem(
    [property: JsonPropertyName("nameAr")] string? NameAr,
    [property: JsonPropertyName("nameEn")] string? NameEn,
    [property: JsonPropertyName("priceId")] Guid? PriceId = null,
    [property: JsonPropertyName("productId")] Guid? ProductId = null,
    [property: JsonPropertyName("type")] string? Type = null
);

public record RekazTransactionResult(
    Guid Id, 
    Guid CustomerId, 
    string Status,
    string PaymentStatus,
    decimal PaidAmount, 
    decimal RemainingAmount,
    string Currency,
    List<RekazTransactionItem> Items,
    [property: JsonPropertyName("creationTime")] DateTimeOffset? CreationTime = null
);

public interface IRekazTransactionsClient
{
    Task<RekazTransactionResult?> GetTransactionByIdAsync(Guid id, CancellationToken ct = default);
}
