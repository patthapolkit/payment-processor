using System.Globalization;
using FluentValidation;
using PaymentProcessor.Core.Enums;
using PaymentProcessor.Core.Models;

namespace PaymentProcessor.Core.Validators;

public class TransactionDtoValidator : AbstractValidator<TransactionDto>
{
    public TransactionDtoValidator()
    {
        RuleFor(x => x.TransactionId)
            .NotEmpty()
            .WithErrorCode(nameof(InvalidReason.MISSING_FIELDS));

        RuleFor(x => x.MerchantRef)
            .NotEmpty()
            .WithErrorCode(nameof(InvalidReason.MISSING_FIELDS));

        RuleFor(x => x.Amount)
            .NotNull()
            .WithErrorCode(nameof(InvalidReason.MISSING_FIELDS));

        RuleFor(x => x.Currency)
            .NotEmpty()
            .WithErrorCode(nameof(InvalidReason.MISSING_FIELDS));

        RuleFor(x => x.Status)
            .NotEmpty()
            .WithErrorCode(nameof(InvalidReason.MISSING_FIELDS));

        RuleFor(x => x.CreatedAtUtc)
            .NotEmpty()
            .WithErrorCode(nameof(InvalidReason.MISSING_FIELDS));

        RuleFor(x => x.Amount)
            .GreaterThan(0)
            .When(x => x.Amount.HasValue)
            .WithErrorCode(nameof(InvalidReason.INVALID_AMOUNT));

        RuleFor(x => x.Currency)
            .Must(BeValidCurrency)
            .When(x => !string.IsNullOrWhiteSpace(x.Currency))
            .WithErrorCode(nameof(InvalidReason.INVALID_CURRENCY));

        RuleFor(x => x.Status)
            .Must(BeValidStatus)
            .When(x => !string.IsNullOrWhiteSpace(x.Status))
            .WithErrorCode(nameof(InvalidReason.INVALID_STATUS));

        RuleFor(x => x.CreatedAtUtc)
            .Must(BeValidUtcTimestamp)
            .When(x => !string.IsNullOrWhiteSpace(x.CreatedAtUtc))
            .WithErrorCode(nameof(InvalidReason.INVALID_TIMESTAMP));
    }

    private static bool BeValidCurrency(string? currency)
    {
        return currency != null && currency.Length == 3 && currency.All(char.IsLetter) && currency.All(char.IsUpper);
    }

    private static bool BeValidStatus(string? status)
    {
        return status != null && Enum.TryParse<TransactionStatus>(status, out _);
    }

    private static bool BeValidUtcTimestamp(string? timestamp)
    {
        if (timestamp == null) return false;

        // Accept both with and without fractional seconds, but must end with Z
        if (!timestamp.EndsWith("Z", StringComparison.Ordinal))
            return false;

        return DateTime.TryParse(timestamp, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var result)
            && result.Kind == DateTimeKind.Utc;
    }
}

public class TransactionValidator
{
    private readonly TransactionDtoValidator _fluentValidator = new();

    public ValidationResult Validate(TransactionDto dto)
    {
        var fluentResult = _fluentValidator.Validate(dto);

        if (!fluentResult.IsValid)
        {
            var errorCode = fluentResult.Errors.First().ErrorCode;
            var reason = Enum.Parse<InvalidReason>(errorCode);
            return ValidationResult.Failure(reason);
        }

        var transaction = new Transaction
        {
            TransactionId = dto.TransactionId!,
            MerchantRef = dto.MerchantRef!,
            Amount = dto.Amount!.Value,
            Currency = dto.Currency!,
            Status = Enum.Parse<TransactionStatus>(dto.Status!),
            CreatedAtUtc = DateTime.Parse(dto.CreatedAtUtc!, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind)
        };

        return ValidationResult.Success(transaction);
    }
}
