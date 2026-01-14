using System.CommandLine;
using System.Text.Json;
using PaymentProcessor.Core.Models;
using PaymentProcessor.Core.Services;

namespace PaymentProcessor.Cli;

class Program
{
    static int Main(string[] args)
    {
        Option<FileInfo> inputOption = new("--input")
        {
            Description = "Input JSON file containing transactions",
            Required = true
        };

        Option<FileInfo> outputOption = new("--output")
        {
            Description = "Output JSON file for the report",
            Required = true
        };

        RootCommand rootCommand = new("Payment Transaction Processor");
        rootCommand.Options.Add(inputOption);
        rootCommand.Options.Add(outputOption);

        rootCommand.SetAction(parseResult =>
        {
            var input = parseResult.GetValue(inputOption);
            var output = parseResult.GetValue(outputOption);
            ProcessTransactions(input!, output!);
        });

        return rootCommand.Parse(args).Invoke();
    }

    private static void ProcessTransactions(FileInfo input, FileInfo output)
    {
        var jsonContent = File.ReadAllText(input.FullName);

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };

        var transactions = JsonSerializer.Deserialize<List<TransactionDto>>(jsonContent, options) ?? new List<TransactionDto>();

        var processor = new TransactionProcessorService();
        var report = processor.Process(transactions);

        var outputOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };

        var reportJson = JsonSerializer.Serialize(report, outputOptions);
        File.WriteAllText(output.FullName, reportJson);

        Console.WriteLine($"Report generated at {output.FullName}");
    }
}
