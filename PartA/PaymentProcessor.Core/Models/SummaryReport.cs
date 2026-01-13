namespace PaymentProcessor.Core.Models;

public class SummaryReport
{
    public int TotalTransactions { get; init; }
    public int ValidTransactions { get; set; }
    public int InvalidTransactions { get; set; }

    public Dictionary<string, int> InvalidReasons { get; set; } = new()
    {
        ["MISSING_FIELDS"] = 0,
        ["INVALID_AMOUNT"] = 0,
        ["INVALID_CURRENCY"] = 0,
        ["INVALID_STATUS"] = 0,
        ["INVALID_TIMESTAMP"] = 0
    };

    public Dictionary<string, int> StatusCounts { get; set; } = new()
    {
        ["SUCCESS"] = 0,
        ["FAILED"] = 0,
        ["PENDING"] = 0
    };

    public AmountStats SuccessAmountStats { get; set; } = new();
    public List<DuplicateGroup> DuplicateGroups { get; set; } = [];
}

public class AmountStats
{
    public decimal Min { get; init; }
    public decimal Max { get; init; }
    public decimal Avg { get; init; }
}

public class DuplicateGroup
{
    public required string Rule { get; init; }
    public required List<Transaction> Transactions { get; init; }
}
