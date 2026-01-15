# Payment Transaction Processor

A .NET console application that processes payment transactions, validates data, detects duplicates, and generates summary reports.

## Table of Contents

- [Features](#features)
- [How to Run](#how-to-run)
- [Assumptions Made](#assumptions-made)
- [Trade-offs and Design Decisions](#trade-offs-and-design-decisions)
- [AI Usage](#ai-usage)

## Features

- **Transaction Validation**: Validates all required fields with business rules (amount > 0, 3-letter currency codes, valid status and timestamps)
- **Duplicate Detection**: Identifies duplicates using two rules:
  - **TXID Rule**: Same transaction ID appearing multiple times
  - **MERCHANT_AMOUNT_DAY Rule**: Same merchant, amount, and currency on the same UTC day
- **Summary Report**: Generates comprehensive statistics including:
  - Total, valid, and invalid transaction counts
  - Invalid reason breakdown
  - Status distribution
  - Amount statistics (min/max/avg) for successful transactions
  - Duplicate groups with detailed transaction lists
- **Idempotent Processing**: Handles duplicate transaction IDs by counting each unique ID only once in totals

## How to Run

### Requirements

- .NET 9 SDK

### Build the Project

```bash
cd PartA
dotnet build
```

### Run the Application

```bash
dotnet run --project PaymentProcessor.Cli -- --input transactions.json --output report.json
```

### Run Tests

```bash
dotnet test
```

## Assumptions Made

1. **Validation Priority**: Validation happens first, then duplicate detection on valid records only
2. **Rule 2 Duplicates**: All transactions in a MERCHANT_AMOUNT_DAY group are flagged as duplicates (including the first occurrence)
3. **Idempotent Logic**: For transactions with the same `transactionId`:
   - Count only once in status totals
   - Use the latest transaction by `createdAtUtc` for amount calculations
4. **Date Comparison**: "Same UTC day" means same calendar date in UTC (ignores time component)
5. **Decimal Precision**: Financial amounts use `decimal` type (not `double`) for accuracy
6. **Case Sensitivity**: Currency codes are case-sensitive and must be uppercase
7. **Empty File**: An empty transactions array is valid and produces zero counts

## Trade-offs and Design Decisions

- **Project Structure**: Kept it simple with just three layers (CLI, Core, and Tests) maintaining separation of concerns without overcomplicating things
- **No Infrastructure Layer**: The app only does file I/O, so adding another project would add unnecessary complexity. If this grows to include database access or other infrastructure concerns, then it would make sense to split it out
- **No Generic Host or DI Container**: Since this is a straightforward CLI app with minimal dependencies, manual instantiation is clearer and easier to understand than setting up a full DI container
- **Fluent Validation**: Chose this over built-in Data Annotations because it's clearer to read, flexible, and easier to test in isolation
- **System.Text.Json**: Used the built-in JSON library instead of external alternatives like Newtonsoft.Json to reduce external dependencies

## AI Usage

- **GitHub Copilot & Gemini CLI**: Used for planning, code completion and generation, architecture decisions, and problem-solving during development
- **ChatGPT & Gemini Web**: Used for Q&A tasks, exploring design patterns and practices, and validation of architectural designs
