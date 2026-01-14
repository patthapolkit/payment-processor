using PaymentProcessor.Core.Models;

namespace PaymentProcessor.Core.Services;

public interface ITransactionProcessorService
{
    SummaryReport Process(List<TransactionDto> transactions);
}
