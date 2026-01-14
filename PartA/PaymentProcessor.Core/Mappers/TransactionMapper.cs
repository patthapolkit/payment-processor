using System.Globalization;
using PaymentProcessor.Core.Enums;
using PaymentProcessor.Core.Models;

namespace PaymentProcessor.Core.Mappers;

public static class TransactionMapper
{
    public static Transaction ToDomain(this TransactionDto dto)
    {
        return new Transaction
        {
            TransactionId = dto.TransactionId!,
            MerchantRef = dto.MerchantRef!,
            Amount = dto.Amount!.Value,
            Currency = dto.Currency!,
            Status = Enum.Parse<TransactionStatus>(dto.Status!),
            CreatedAtUtc = DateTime.Parse(dto.CreatedAtUtc!, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind)
        };
    }
}
