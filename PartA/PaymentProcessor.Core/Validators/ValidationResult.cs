using PaymentProcessor.Core.Enums;
using PaymentProcessor.Core.Models;

namespace PaymentProcessor.Core.Validators;

public class ValidationResult
{
    public bool IsValid { get; init; }
    public Transaction? Transaction { get; init; }
    public InvalidReason? Reason { get; init; }

    public static ValidationResult Success(Transaction transaction) => new()
    {
        IsValid = true,
        Transaction = transaction
    };

    public static ValidationResult Failure(InvalidReason reason) => new()
    {
        IsValid = false,
        Reason = reason
    };
}
