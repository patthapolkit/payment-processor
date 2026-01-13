namespace PaymentProcessor.Core.Models;

public class TransactionDto
{
    public string? TransactionId { get; set; }
    public string? MerchantRef { get; set; }
    public decimal? Amount { get; set; }
    public string? Currency { get; set; }
    public string? Status { get; set; }
    public string? CreatedAtUtc { get; set; }
}
