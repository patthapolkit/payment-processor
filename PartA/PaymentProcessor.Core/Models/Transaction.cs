using System.Text.Json.Serialization;
using PaymentProcessor.Core.Enums;

namespace PaymentProcessor.Core.Models;

public record Transaction
{
    public required string TransactionId { get; init; }
    public required string MerchantRef { get; init; }
    public required decimal Amount { get; init; }
    public required string Currency { get; init; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public required TransactionStatus Status { get; init; }

    public required DateTime CreatedAtUtc { get; init; }

    [JsonIgnore]
    public DateOnly UtcDate => DateOnly.FromDateTime(CreatedAtUtc);
}
