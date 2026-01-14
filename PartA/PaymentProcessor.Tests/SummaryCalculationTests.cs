using PaymentProcessor.Core.Models;
using PaymentProcessor.Core.Services;
using Shouldly;

namespace PaymentProcessor.Tests;

[TestFixture]
public class SummaryCalculationTests
{
    private TransactionProcessorService _service = null!;

    [SetUp]
    public void Setup()
    {
        _service = new TransactionProcessorService();
    }

    private static TransactionDto CreateDto(string txId, string merchantRef, decimal amount, string currency, string status, string timestamp) => new()
    {
        TransactionId = txId,
        MerchantRef = merchantRef,
        Amount = amount,
        Currency = currency,
        Status = status,
        CreatedAtUtc = timestamp
    };

    [Test]
    public void Process_EmptyList_ReturnsZeroCounts()
    {
        var transactions = new List<TransactionDto>();

        var report = _service.Process(transactions);

        report.TotalTransactions.ShouldBe(0);
        report.ValidTransactions.ShouldBe(0);
        report.InvalidTransactions.ShouldBe(0);
    }

    [Test]
    public void Process_CountsTransactionsCorrectly()
    {
        var transactions = new List<TransactionDto>
        {
            CreateDto("tx-001", "m-1", 100, "USD", "SUCCESS", "2025-01-12T01:00:00Z"),
            CreateDto("tx-002", "m-2", 200, "EUR", "FAILED", "2025-01-12T02:00:00Z"),
            CreateDto("tx-003", "m-3", -50, "GBP", "SUCCESS", "2025-01-12T03:00:00Z")
        };

        var report = _service.Process(transactions);

        report.TotalTransactions.ShouldBe(3);
        report.ValidTransactions.ShouldBe(2);
        report.InvalidTransactions.ShouldBe(1);
    }

    [Test]
    public void Process_CountsStatusCorrectly()
    {
        var transactions = new List<TransactionDto>
        {
            CreateDto("tx-001", "m-1", 100, "USD", "SUCCESS", "2025-01-12T01:00:00Z"),
            CreateDto("tx-002", "m-2", 200, "EUR", "SUCCESS", "2025-01-12T02:00:00Z"),
            CreateDto("tx-003", "m-3", 150, "GBP", "FAILED", "2025-01-12T03:00:00Z"),
            CreateDto("tx-004", "m-4", 50, "USD", "PENDING", "2025-01-12T04:00:00Z")
        };

        var report = _service.Process(transactions);

        report.StatusCounts["SUCCESS"].ShouldBe(2);
        report.StatusCounts["FAILED"].ShouldBe(1);
        report.StatusCounts["PENDING"].ShouldBe(1);
    }

    [Test]
    public void Process_CalculatesSuccessAmountStats()
    {
        var transactions = new List<TransactionDto>
        {
            CreateDto("tx-001", "m-1", 100, "USD", "SUCCESS", "2025-01-12T01:00:00Z"),
            CreateDto("tx-002", "m-2", 200, "EUR", "SUCCESS", "2025-01-12T02:00:00Z"),
            CreateDto("tx-003", "m-3", 300, "GBP", "SUCCESS", "2025-01-12T03:00:00Z"),
            CreateDto("tx-004", "m-4", 500, "USD", "FAILED", "2025-01-12T04:00:00Z")
        };

        var report = _service.Process(transactions);

        report.SuccessAmountStats.Min.ShouldBe(100);
        report.SuccessAmountStats.Max.ShouldBe(300);
        report.SuccessAmountStats.Avg.ShouldBe(200);
    }

    [Test]
    public void Process_NoSuccessTransactions_ZeroStats()
    {
        var transactions = new List<TransactionDto>
        {
            CreateDto("tx-001", "m-1", 100, "USD", "FAILED", "2025-01-12T01:00:00Z"),
            CreateDto("tx-002", "m-2", 200, "EUR", "PENDING", "2025-01-12T02:00:00Z")
        };

        var report = _service.Process(transactions);

        report.SuccessAmountStats.Min.ShouldBe(0);
        report.SuccessAmountStats.Max.ShouldBe(0);
        report.SuccessAmountStats.Avg.ShouldBe(0);
    }

    [Test]
    public void Process_InvalidReasonsBreakdown()
    {
        var transactions = new List<TransactionDto>
        {
            new() { TransactionId = null, MerchantRef = "m-1", Amount = 100, Currency = "USD", Status = "SUCCESS", CreatedAtUtc = "2025-01-12T01:00:00Z" },
            CreateDto("tx-002", "m-2", -10, "USD", "SUCCESS", "2025-01-12T02:00:00Z"),
            CreateDto("tx-003", "m-3", 100, "US", "SUCCESS", "2025-01-12T03:00:00Z"),
            CreateDto("tx-004", "m-4", 100, "USD", "INVALID", "2025-01-12T04:00:00Z"),
            CreateDto("tx-005", "m-5", 100, "USD", "SUCCESS", "not-a-date")
        };

        var report = _service.Process(transactions);

        report.InvalidReasons["MISSING_FIELDS"].ShouldBe(1);
        report.InvalidReasons["INVALID_AMOUNT"].ShouldBe(1);
        report.InvalidReasons["INVALID_CURRENCY"].ShouldBe(1);
        report.InvalidReasons["INVALID_STATUS"].ShouldBe(1);
        report.InvalidReasons["INVALID_TIMESTAMP"].ShouldBe(1);
    }

    [Test]
    public void Process_IdempotentCounting_DuplicateTxIdCountsOnce()
    {
        var transactions = new List<TransactionDto>
        {
            CreateDto("tx-001", "m-1", 100, "USD", "SUCCESS", "2025-01-12T01:00:00Z"),
            CreateDto("tx-001", "m-1", 100, "USD", "SUCCESS", "2025-01-12T02:00:00Z")
        };

        var report = _service.Process(transactions);

        report.ValidTransactions.ShouldBe(2);
        report.StatusCounts["SUCCESS"].ShouldBe(1);
    }

    [Test]
    public void Process_IdempotentCounting_UsesLatestForStats()
    {
        var transactions = new List<TransactionDto>
        {
            CreateDto("tx-001", "m-1", 100, "USD", "SUCCESS", "2025-01-12T01:00:00Z"),
            CreateDto("tx-001", "m-1", 200, "USD", "SUCCESS", "2025-01-12T02:00:00Z")
        };

        var report = _service.Process(transactions);

        report.SuccessAmountStats.Min.ShouldBe(200);
        report.SuccessAmountStats.Max.ShouldBe(200);
        report.SuccessAmountStats.Avg.ShouldBe(200);
    }

    [Test]
    public void Process_IdempotentCounting_MixedStatuses()
    {
        var transactions = new List<TransactionDto>
        {
            CreateDto("tx-001", "m-1", 100, "USD", "PENDING", "2025-01-12T01:00:00Z"),
            CreateDto("tx-001", "m-1", 100, "USD", "SUCCESS", "2025-01-12T02:00:00Z"),
            CreateDto("tx-002", "m-2", 200, "EUR", "SUCCESS", "2025-01-12T03:00:00Z"),
            CreateDto("tx-002", "m-2", 200, "EUR", "FAILED", "2025-01-12T04:00:00Z")
        };

        var report = _service.Process(transactions);

        report.StatusCounts["SUCCESS"].ShouldBe(1);
        report.StatusCounts["FAILED"].ShouldBe(1);
        report.StatusCounts["PENDING"].ShouldBe(0);
    }

    [Test]
    public void Process_AverageRoundsToTwoDecimals()
    {
        var transactions = new List<TransactionDto>
        {
            CreateDto("tx-001", "m-1", 10, "USD", "SUCCESS", "2025-01-12T01:00:00Z"),
            CreateDto("tx-002", "m-2", 20, "USD", "SUCCESS", "2025-01-12T02:00:00Z"),
            CreateDto("tx-003", "m-3", 30, "USD", "SUCCESS", "2025-01-12T03:00:00Z")
        };

        var report = _service.Process(transactions);

        report.SuccessAmountStats.Avg.ShouldBe(20);
    }
}
