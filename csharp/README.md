# C# Carrier Verification Tests

## Prerequisites

- .NET 6.0 SDK or later
- Visual Studio, Visual Studio Code, or any .NET IDE

## Setup

1. Restore NuGet packages:
```bash
dotnet restore
```

2. Configure API key:
```bash
cp .env.example .env
# Edit .env and add your API key:
# API_KEY=your_actual_api_key_here
```

## Running Tests

```bash
dotnet run
```

Or build and run:
```bash
dotnet build
dotnet run
```

## Test Coverage

The test suite includes:
- API connectivity and data retrieval
- Authority status verification
- Data freshness checks
- Vehicle OOS rate analysis
- Driver OOS rate analysis
- Crash history evaluation
- Inspection volume assessment
- Fleet size evaluation
- Safety rating verification
- Full verification workflow

All tests use USDOT 44 as the example carrier.

## Project Structure

- `Program.cs` - Main entry point and test execution
- `CarrierVerifier.cs` - Core verification logic
- `CarrierData.cs` - Data models for API responses
- `.env` - Configuration file (create from .env.example)

