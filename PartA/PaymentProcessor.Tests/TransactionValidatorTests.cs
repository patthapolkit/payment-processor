using PaymentProcessor.Core.Enums;
using PaymentProcessor.Core.Models;
using PaymentProcessor.Core.Validators;
using Shouldly;

namespace PaymentProcessor.Tests;

[TestFixture]
public class TransactionValidatorTests
{
    private TransactionValidator _validator = null!;

    [SetUp]
    public void Setup()
    {
        _validator = new TransactionValidator();
    }

    private static TransactionDto CreateValidDto() => new()
    {
        TransactionId = "tx-001",
        MerchantRef = "m-1",
        Amount = 100.00m,
        Currency = "USD",
        Status = "SUCCESS",
        CreatedAtUtc = "2025-01-12T10:00:00Z"
    };

    [Test]
    public void Validate_ValidTransaction_ReturnsSuccess()
    {
        var transaction = CreateValidDto();

        var result = _validator.Validate(transaction);

        result.IsValid.ShouldBeTrue();
        result.Transaction.ShouldNotBeNull();
        result.Transaction.TransactionId.ShouldBe("tx-001");
    }

    [TestCase(nameof(TransactionDto.TransactionId))]
    [TestCase(nameof(TransactionDto.MerchantRef))]
    [TestCase(nameof(TransactionDto.Currency))]
    [TestCase(nameof(TransactionDto.Status))]
    [TestCase(nameof(TransactionDto.CreatedAtUtc))]
    public void Validate_MissingField_ReturnsMissingFields(string fieldName)
    {
        var transaction = CreateValidDto();
        var property = typeof(TransactionDto).GetProperty(fieldName)!;
        property.SetValue(transaction, "");

        var result = _validator.Validate(transaction);

        result.IsValid.ShouldBeFalse();
        result.Reason.ShouldBe(InvalidReason.MISSING_FIELDS);
    }

    [Test]
    public void Validate_MissingAmount_ReturnsMissingFields()
    {
        var transaction = CreateValidDto();
        transaction.Amount = null;
        var result = _validator.Validate(transaction);

        result.IsValid.ShouldBeFalse();
        result.Reason.ShouldBe(InvalidReason.MISSING_FIELDS);
    }

    [TestCase(0)]
    [TestCase(-10)]
    [TestCase(-0.01)]
    public void Validate_InvalidAmount_ReturnsInvalidAmount(decimal amount)
    {
        var transaction = CreateValidDto();
        transaction.Amount = amount;

        var result = _validator.Validate(transaction);

        result.IsValid.ShouldBeFalse();
        result.Reason.ShouldBe(InvalidReason.INVALID_AMOUNT);
    }

    [TestCase("US")]
    [TestCase("USDD")]
    [TestCase("usd")]
    [TestCase("US1")]
    [TestCase("123")]
    public void Validate_InvalidCurrency_ReturnsInvalidCurrency(string currency)
    {
        var transaction = CreateValidDto();
        transaction.Currency = currency;

        var result = _validator.Validate(transaction);

        result.IsValid.ShouldBeFalse();
        result.Reason.ShouldBe(InvalidReason.INVALID_CURRENCY);
    }

    [TestCase("COMPLETED")]
    [TestCase("success")]
    [TestCase("failed")]
    [TestCase("INVALID")]
    public void Validate_InvalidStatus_ReturnsInvalidStatus(string status)
    {
        var transaction = CreateValidDto();
        transaction.Status = status;

        var result = _validator.Validate(transaction);

        result.IsValid.ShouldBeFalse();
        result.Reason.ShouldBe(InvalidReason.INVALID_STATUS);
    }

    [TestCase("not-a-date")]
    [TestCase("2025-01-12T10:00:00")]
    [TestCase("2025-01-12T10:00:00+07:00")]
    [TestCase("2025-01-12")]
    public void Validate_InvalidTimestamp_ReturnsInvalidTimestamp(string timestamp)
    {
        var transaction = CreateValidDto();
        transaction.CreatedAtUtc = timestamp;

        var result = _validator.Validate(transaction);

        result.IsValid.ShouldBeFalse();
        result.Reason.ShouldBe(InvalidReason.INVALID_TIMESTAMP);
    }

    [TestCase("SUCCESS")]
    [TestCase("FAILED")]
    [TestCase("PENDING")]
    public void Validate_ValidStatus_ReturnsSuccess(string status)
    {
        var transaction = CreateValidDto();
        transaction.Status = status;

        var result = _validator.Validate(transaction);

        result.IsValid.ShouldBeTrue();
    }
}
