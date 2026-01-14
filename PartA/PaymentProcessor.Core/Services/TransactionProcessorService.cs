using PaymentProcessor.Core.Enums;
using PaymentProcessor.Core.Models;
using PaymentProcessor.Core.Validators;

namespace PaymentProcessor.Core.Services;

public class TransactionProcessorService : ITransactionProcessorService
{
    private readonly TransactionValidator _validator = new();

    public SummaryReport Process(List<TransactionDto> transactions)
    {
        var report = new SummaryReport
        {
            TotalTransactions = transactions.Count
        };

        var validTransactions = new List<Transaction>();
        foreach (var result in transactions.Select(_validator.Validate))
        {
            if (result.IsValid)
            {
                validTransactions.Add(result.Transaction!);
            }
            else
            {
                report.InvalidReasons[result.Reason.ToString()!]++;
            }
        }

        report.ValidTransactions = validTransactions.Count;
        report.InvalidTransactions = transactions.Count - validTransactions.Count;

        var duplicateGroups = DetectDuplicates(validTransactions);
        report.DuplicateGroups = duplicateGroups;

        var deduplicatedTransactions = DeduplicateByTransactionId(validTransactions);

        foreach (var tx in deduplicatedTransactions)
        {
            report.StatusCounts[tx.Status.ToString()]++;
        }

        var successTransactions = deduplicatedTransactions
            .Where(t => t.Status == TransactionStatus.SUCCESS)
            .ToList();

        if (successTransactions.Count > 0)
        {
            var amounts = successTransactions.Select(t => t.Amount).ToList();
            report.SuccessAmountStats = new AmountStats
            {
                Min = amounts.Min(),
                Max = amounts.Max(),
                Avg = Math.Round(amounts.Average(), 2)
            };
        }

        return report;
    }

    private static List<DuplicateGroup> DetectDuplicates(List<Transaction> transactions)
    {
        var duplicateGroups = new List<DuplicateGroup>();

        var txIdGroups = transactions
            .GroupBy(t => t.TransactionId)
            .Where(g => g.Count() > 1);

        foreach (var group in txIdGroups)
        {
            duplicateGroups.Add(new DuplicateGroup
            {
                Rule = "TXID",
                Transactions = group.ToList()
            });
        }

        var merchantDayGroups = transactions
            .GroupBy(t => new { t.MerchantRef, t.Amount, t.Currency, t.UtcDate })
            .Where(g => g.Count() > 1);

        foreach (var group in merchantDayGroups)
        {
            var groupTransactions = group.ToList();
            var allSameTransactionId = groupTransactions
                .Select(t => t.TransactionId)
                .Distinct()
                .Count() == 1;

            if (!allSameTransactionId)
            {
                duplicateGroups.Add(new DuplicateGroup
                {
                    Rule = "MERCHANT_AMOUNT_DAY",
                    Transactions = groupTransactions
                });
            }
        }

        return duplicateGroups;
    }

    private static List<Transaction> DeduplicateByTransactionId(List<Transaction> transactions)
    {
        return transactions
            .GroupBy(t => t.TransactionId)
            .Select(g => g.OrderByDescending(t => t.CreatedAtUtc).First())
            .ToList();
    }
}
