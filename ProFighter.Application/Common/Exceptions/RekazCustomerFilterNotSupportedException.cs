using System;

namespace ProFighter.Application.Common.Exceptions;

/// <summary>
/// Thrown when Rekaz API ignores the CustomerId query filter and returns items belonging to other customers.
/// </summary>
public class RekazCustomerFilterNotSupportedException : Exception
{
    public Guid RequestedCustomerId { get; }
    public Guid MismatchedCustomerId { get; }

    public RekazCustomerFilterNotSupportedException(Guid requestedCustomerId, Guid mismatchedCustomerId)
        : base($"Rekaz API ignored the CustomerId query filter. Requested CustomerId: {requestedCustomerId}, but response contained item for CustomerId: {mismatchedCustomerId}.")
    {
        RequestedCustomerId = requestedCustomerId;
        MismatchedCustomerId = mismatchedCustomerId;
    }
}
