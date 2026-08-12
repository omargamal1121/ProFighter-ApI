namespace ProFighter.Domain.ValueObjects;

public class OrderSnapshot
{
    public decimal Subtotal { get; private set; }
    public decimal DiscountAmount { get; private set; }
    public decimal TaxAmount { get; private set; }
    public decimal TotalAmount { get; private set; }
    public decimal PaidAmount { get; private set; }
    public decimal RemainingAmount { get; private set; }
    public string Currency { get; private set; }
    public string OrderStatus { get; private set; }
    public string OrderPaymentStatus { get; private set; }

    // EF Core deserialization constructor
    private OrderSnapshot()
    {
        Currency = string.Empty;
        OrderStatus = string.Empty;
        OrderPaymentStatus = string.Empty;
    }

    public OrderSnapshot(
        decimal subtotal,
        decimal discountAmount,
        decimal taxAmount,
        decimal totalAmount,
        decimal paidAmount,
        decimal remainingAmount,
        string currency,
        string orderStatus,
        string orderPaymentStatus)
    {
        Subtotal = subtotal;
        DiscountAmount = discountAmount;
        TaxAmount = taxAmount;
        TotalAmount = totalAmount;
        PaidAmount = paidAmount;
        RemainingAmount = remainingAmount;
        Currency = currency;
        OrderStatus = orderStatus;
        OrderPaymentStatus = orderPaymentStatus;
    }
}
