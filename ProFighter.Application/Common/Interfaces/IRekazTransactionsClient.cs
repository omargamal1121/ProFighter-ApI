using System;
using System.Threading;
using System.Threading.Tasks;

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ProFighter.Application.Common.Interfaces;

public record RekazTransactionItem(
    [property: JsonPropertyName("nameAr")] string? NameAr,
    [property: JsonPropertyName("nameEn")] string? NameEn
);

public record RekazTransactionResult(
    Guid Id, 
    Guid CustomerId, 
    string Status,
    string PaymentStatus,
    decimal PaidAmount, 
    decimal RemainingAmount,
    string Currency,
    List<RekazTransactionItem> Items
);

public interface IRekazTransactionsClient
{
    Task<RekazTransactionResult?> GetTransactionByIdAsync(Guid id, CancellationToken ct = default);
}
