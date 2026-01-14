using PaymentProcessor.Core.Models;
using PaymentProcessor.Core.Services;
using Shouldly;

namespace PaymentProcessor.Tests;

[TestFixture]
public class DuplicateDetectionTests
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
    public void Process_DuplicateTransactionId_DetectsTxidDuplicate()
    {
        var transactions = new List<TransactionDto>
        {
            CreateDto("tx-001", "m-1", 100, "USD", "SUCCESS", "2025-01-12T01:00:00Z"),
            CreateDto("tx-001", "m-1", 100, "USD", "SUCCESS", "2025-01-12T02:00:00Z")
        };

        var report = _service.Process(transactions);

        report.DuplicateGroups.Count.ShouldBe(1);
        report.DuplicateGroups[0].Rule.ShouldBe("TXID");
        report.DuplicateGroups[0].Transactions.Count.ShouldBe(2);
    }

    [Test]
    public void Process_SameMerchantAmountCurrencySameDay_DetectsMerchantAmountDayDuplicate()
    {
        var transactions = new List<TransactionDto>
        {
            CreateDto("tx-001", "m-1", 100, "USD", "SUCCESS", "2025-01-12T01:00:00Z"),
            CreateDto("tx-002", "m-1", 100, "USD", "FAILED", "2025-01-12T23:00:00Z")
        };

        var report = _service.Process(transactions);

        report.DuplicateGroups.Count.ShouldBe(1);
        report.DuplicateGroups[0].Rule.ShouldBe("MERCHANT_AMOUNT_DAY");
        report.DuplicateGroups[0].Transactions.Count.ShouldBe(2);
    }

    [Test]
    public void Process_SameMerchantAmountCurrencyDifferentDay_NoDuplicate()
    {
        var transactions = new List<TransactionDto>
        {
            CreateDto("tx-001", "m-1", 100, "USD", "SUCCESS", "2025-01-12T23:00:00Z"),
            CreateDto("tx-002", "m-1", 100, "USD", "SUCCESS", "2025-01-13T01:00:00Z")
        };

        var report = _service.Process(transactions);

        report.DuplicateGroups.Count.ShouldBe(0);
    }

    [Test]
    public void Process_DifferentMerchantSameAmountSameDay_NoDuplicate()
    {
        var transactions = new List<TransactionDto>
        {
            CreateDto("tx-001", "m-1", 100, "USD", "SUCCESS", "2025-01-12T01:00:00Z"),
            CreateDto("tx-002", "m-2", 100, "USD", "SUCCESS", "2025-01-12T02:00:00Z")
        };

        var report = _service.Process(transactions);

        report.DuplicateGroups.Count.ShouldBe(0);
    }

    [Test]
    public void Process_SameMerchantDifferentAmountSameDay_NoDuplicate()
    {
        var transactions = new List<TransactionDto>
        {
            CreateDto("tx-001", "m-1", 100, "USD", "SUCCESS", "2025-01-12T01:00:00Z"),
            CreateDto("tx-002", "m-1", 200, "USD", "SUCCESS", "2025-01-12T02:00:00Z")
        };

        var report = _service.Process(transactions);

        report.DuplicateGroups.Count.ShouldBe(0);
    }

    [Test]
    public void Process_SameMerchantSameAmountDifferentCurrency_NoDuplicate()
    {
        var transactions = new List<TransactionDto>
        {
            CreateDto("tx-001", "m-1", 100, "USD", "SUCCESS", "2025-01-12T01:00:00Z"),
            CreateDto("tx-002", "m-1", 100, "EUR", "SUCCESS", "2025-01-12T02:00:00Z")
        };

        var report = _service.Process(transactions);

        report.DuplicateGroups.Count.ShouldBe(0);
    }

    [Test]
    public void Process_BothDuplicateRulesMatch_CreatesSeparateGroups()
    {
        var transactions = new List<TransactionDto>
        {
            CreateDto("tx-001", "m-1", 100, "USD", "SUCCESS", "2025-01-12T01:00:00Z"),
            CreateDto("tx-001", "m-1", 100, "USD", "SUCCESS", "2025-01-12T02:00:00Z"),
            CreateDto("tx-002", "m-2", 200, "EUR", "FAILED", "2025-01-12T03:00:00Z"),
            CreateDto("tx-003", "m-2", 200, "EUR", "PENDING", "2025-01-12T04:00:00Z")
        };

        var report = _service.Process(transactions);

        report.DuplicateGroups.Count.ShouldBe(2);
        report.DuplicateGroups.ShouldContain(g => g.Rule == "TXID");
        report.DuplicateGroups.ShouldContain(g => g.Rule == "MERCHANT_AMOUNT_DAY");
    }

    [Test]
    public void Process_TxidDuplicatesWithSameMerchantAmountDay_OnlyTxidGroup()
    {
        var transactions = new List<TransactionDto>
        {
            CreateDto("tx-001", "m-1", 100, "USD", "SUCCESS", "2025-01-12T01:00:00Z"),
            CreateDto("tx-001", "m-1", 100, "USD", "SUCCESS", "2025-01-12T02:00:00Z")
        };

        var report = _service.Process(transactions);

        report.DuplicateGroups.Count.ShouldBe(1);
        report.DuplicateGroups[0].Rule.ShouldBe("TXID");
    }

    [Test]
    public void Process_MultipleMerchantAmountDayGroups_DetectsAll()
    {
        var transactions = new List<TransactionDto>
        {
            CreateDto("tx-001", "m-1", 100, "USD", "SUCCESS", "2025-01-12T01:00:00Z"),
            CreateDto("tx-002", "m-1", 100, "USD", "SUCCESS", "2025-01-12T02:00:00Z"),
            CreateDto("tx-003", "m-2", 200, "EUR", "SUCCESS", "2025-01-13T01:00:00Z"),
            CreateDto("tx-004", "m-2", 200, "EUR", "SUCCESS", "2025-01-13T02:00:00Z")
        };

        var report = _service.Process(transactions);

        var merchantGroups = report.DuplicateGroups.Where(g => g.Rule == "MERCHANT_AMOUNT_DAY").ToList();
        merchantGroups.Count.ShouldBe(2);
    }
}
