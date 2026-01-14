using System.Buffers;
using System.Globalization;
using FluentValidation;
using PaymentProcessor.Core.Enums;
using PaymentProcessor.Core.Models;

namespace PaymentProcessor.Core.Validators;

public class TransactionDtoValidator : AbstractValidator<TransactionDto>
{
    private static readonly SearchValues<char> UpperCaseLetters = SearchValues.Create("ABCDEFGHIJKLMNOPQRSTUVWXYZ");

    public TransactionDtoValidator()
    {
        RuleFor(x => x.TransactionId)
            .NotEmpty()
            .WithState(_ => InvalidReason.MISSING_FIELDS);

        RuleFor(x => x.MerchantRef)
            .NotEmpty()
            .WithState(_ => InvalidReason.MISSING_FIELDS);

        RuleFor(x => x.Amount)
            .NotNull()
            .WithState(_ => InvalidReason.MISSING_FIELDS);

        RuleFor(x => x.Currency)
            .NotEmpty()
            .WithState(_ => InvalidReason.MISSING_FIELDS);

        RuleFor(x => x.Status)
            .NotEmpty()
            .WithState(_ => InvalidReason.MISSING_FIELDS);

        RuleFor(x => x.CreatedAtUtc)
            .NotEmpty()
            .WithState(_ => InvalidReason.MISSING_FIELDS);

        RuleFor(x => x.Amount)
            .GreaterThan(0)
            .When(x => x.Amount.HasValue)
            .WithState(_ => InvalidReason.INVALID_AMOUNT);

        RuleFor(x => x.Currency)
            .Must(BeValidCurrency)
            .When(x => !string.IsNullOrWhiteSpace(x.Currency))
            .WithState(_ => InvalidReason.INVALID_CURRENCY);

        RuleFor(x => x.Status)
            .Must(BeValidStatus)
            .When(x => !string.IsNullOrWhiteSpace(x.Status))
            .WithState(_ => InvalidReason.INVALID_STATUS);

        RuleFor(x => x.CreatedAtUtc)
            .Must(BeValidUtcTimestamp)
            .When(x => !string.IsNullOrWhiteSpace(x.CreatedAtUtc))
            .WithState(_ => InvalidReason.INVALID_TIMESTAMP);
    }

    private static bool BeValidCurrency(string? currency)
    {
        return currency != null && currency.Length == 3 && currency.AsSpan().IndexOfAnyExcept(UpperCaseLetters) == -1;
    }

    private static bool BeValidStatus(string? status)
    {
        return status != null && Enum.TryParse<TransactionStatus>(status, out _);
    }

    private static bool BeValidUtcTimestamp(string? timestamp)
    {
        if (timestamp == null) return false;

        return DateTime.TryParse(timestamp, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var result)
            && result.Kind == DateTimeKind.Utc;
    }
}
