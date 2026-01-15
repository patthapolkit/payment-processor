using System.Text.Json;
using PaymentProcessor.Core.Models;
using PaymentProcessor.Core.Services;
using Shouldly;

namespace PaymentProcessor.Tests;

[TestFixture]
public class IntegrationTests
{
    private string _testDirectory = null!;

    [SetUp]
    public void Setup()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), $"PaymentProcessorTests_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testDirectory);
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, true);
        }
    }

    [Test]
    public void EndToEnd_ProcessesTransactionsAndGeneratesReport()
    {
        // Arrange
        var inputFile = Path.Combine(_testDirectory, "transactions.json");
        var outputFile = Path.Combine(_testDirectory, "report.json");

        var testTransactions = new List<TransactionDto>
        {
            new()
            {
                TransactionId = "tx-001",
                MerchantRef = "m-1",
                Amount = 100.00m,
                Currency = "USD",
                Status = "SUCCESS",
                CreatedAtUtc = "2025-01-12T10:00:00Z"
            },
            new()
            {
                TransactionId = "tx-002",
                MerchantRef = "m-2",
                Amount = 200.00m,
                Currency = "EUR",
                Status = "FAILED",
                CreatedAtUtc = "2025-01-12T11:00:00Z"
            },
            new()
            {
                TransactionId = "tx-003",
                MerchantRef = "m-3",
                Amount = 300.00m,
                Currency = "GBP",
                Status = "PENDING",
                CreatedAtUtc = "2025-01-12T12:00:00Z"
            },
            new()
            {
                TransactionId = "tx-004",
                MerchantRef = "m-4",
                Amount = -50.00m, // Invalid
                Currency = "USD",
                Status = "SUCCESS",
                CreatedAtUtc = "2025-01-12T13:00:00Z"
            }
        };

        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };

        File.WriteAllText(inputFile, JsonSerializer.Serialize(testTransactions, jsonOptions));

        // Act
        var jsonContent = File.ReadAllText(inputFile);
        var readOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };
        var transactions = JsonSerializer.Deserialize<List<TransactionDto>>(jsonContent, readOptions);

        var processor = new TransactionProcessorService();
        var report = processor.Process(transactions!);

        var outputOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };
        File.WriteAllText(outputFile, JsonSerializer.Serialize(report, outputOptions));

        // Assert
        File.Exists(outputFile).ShouldBeTrue();

        var reportJson = File.ReadAllText(outputFile);
        var savedReport = JsonSerializer.Deserialize<SummaryReport>(reportJson, readOptions);

        savedReport.ShouldNotBeNull();
        savedReport.TotalTransactions.ShouldBe(4);
        savedReport.ValidTransactions.ShouldBe(3);
        savedReport.InvalidTransactions.ShouldBe(1);
        savedReport.StatusCounts["SUCCESS"].ShouldBe(1);
        savedReport.StatusCounts["FAILED"].ShouldBe(1);
        savedReport.StatusCounts["PENDING"].ShouldBe(1);
        savedReport.SuccessAmountStats.Min.ShouldBe(100.00m);
        savedReport.SuccessAmountStats.Max.ShouldBe(100.00m);
        savedReport.SuccessAmountStats.Avg.ShouldBe(100.00m);
    }

    [Test]
    public void EndToEnd_DetectsDuplicatesInFullFlow()
    {
        // Arrange
        var inputFile = Path.Combine(_testDirectory, "duplicates.json");
        var outputFile = Path.Combine(_testDirectory, "duplicates-report.json");

        var testTransactions = new List<TransactionDto>
        {
            new()
            {
                TransactionId = "tx-001",
                MerchantRef = "m-1",
                Amount = 100.00m,
                Currency = "USD",
                Status = "SUCCESS",
                CreatedAtUtc = "2025-01-12T10:00:00Z"
            },
            new()
            {
                TransactionId = "tx-001", // Duplicate TXID
                MerchantRef = "m-1",
                Amount = 100.00m,
                Currency = "USD",
                Status = "SUCCESS",
                CreatedAtUtc = "2025-01-12T11:00:00Z"
            },
            new()
            {
                TransactionId = "tx-002",
                MerchantRef = "m-2",
                Amount = 200.00m,
                Currency = "EUR",
                Status = "FAILED",
                CreatedAtUtc = "2025-01-12T12:00:00Z"
            },
            new()
            {
                TransactionId = "tx-003",
                MerchantRef = "m-2",
                Amount = 200.00m,
                Currency = "EUR",
                Status = "SUCCESS",
                CreatedAtUtc = "2025-01-12T13:00:00Z" // Duplicate MERCHANT_AMOUNT_DAY
            }
        };

        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };

        File.WriteAllText(inputFile, JsonSerializer.Serialize(testTransactions, jsonOptions));

        // Act
        var jsonContent = File.ReadAllText(inputFile);
        var readOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };
        var transactions = JsonSerializer.Deserialize<List<TransactionDto>>(jsonContent, readOptions);

        var processor = new TransactionProcessorService();
        var report = processor.Process(transactions!);

        File.WriteAllText(outputFile, JsonSerializer.Serialize(report, jsonOptions));

        // Assert
        var reportJson = File.ReadAllText(outputFile);
        var savedReport = JsonSerializer.Deserialize<SummaryReport>(reportJson, readOptions);

        savedReport.ShouldNotBeNull();
        savedReport.DuplicateGroups.Count.ShouldBe(2);
        savedReport.DuplicateGroups.ShouldContain(g => g.Rule == "TXID");
        savedReport.DuplicateGroups.ShouldContain(g => g.Rule == "MERCHANT_AMOUNT_DAY");
    }

    [Test]
    public void EndToEnd_HandlesEmptyTransactionsFile()
    {
        // Arrange
        var inputFile = Path.Combine(_testDirectory, "empty.json");
        var outputFile = Path.Combine(_testDirectory, "empty-report.json");

        var testTransactions = new List<TransactionDto>();

        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };

        File.WriteAllText(inputFile, JsonSerializer.Serialize(testTransactions, jsonOptions));

        // Act
        var jsonContent = File.ReadAllText(inputFile);
        var readOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };
        var transactions = JsonSerializer.Deserialize<List<TransactionDto>>(jsonContent, readOptions);

        var processor = new TransactionProcessorService();
        var report = processor.Process(transactions!);

        File.WriteAllText(outputFile, JsonSerializer.Serialize(report, jsonOptions));

        // Assert
        var reportJson = File.ReadAllText(outputFile);
        var savedReport = JsonSerializer.Deserialize<SummaryReport>(reportJson, readOptions);

        savedReport.ShouldNotBeNull();
        savedReport.TotalTransactions.ShouldBe(0);
        savedReport.ValidTransactions.ShouldBe(0);
        savedReport.InvalidTransactions.ShouldBe(0);
        savedReport.DuplicateGroups.Count.ShouldBe(0);
    }
}
