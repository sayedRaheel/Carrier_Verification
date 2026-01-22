# Quick Start Guide

Get up and running with the Carrier Verification System in minutes!

## Python Quick Start

```bash
# 1. Navigate to Python directory
cd python

# 2. Install dependencies
pip install -r requirements.txt

# 3. Set up your API key
cp .env.example .env
# Then edit .env and replace "YourApiKey" with your actual API key

# 4. Run tests for USDOT 44
python test_carrier_verification.py
```

**Example .env file:**
```
API_KEY=your_actual_api_key_here
API_BASE_URL=https://saferwebapi.com/v2/usdot/snapshot
TEST_USDOT=44
```

## C# Quick Start

```bash
# 1. Navigate to C# directory
cd csharp

# 2. Restore NuGet packages
dotnet restore

# 3. Set up your API key
cp .env.example .env
# Then edit .env and replace "YourApiKey" with your actual API key

# 4. Run tests for USDOT 44
dotnet run
```

**Example .env file:**
```
API_KEY=your_actual_api_key_here
API_BASE_URL=https://saferwebapi.com/v2/usdot/snapshot
TEST_USDOT=44
```

## Expected Output

When you run the tests, you'll see:

1. **API Connectivity Test** - Verifies connection to SaferWebAPI
2. **Authority Status** - Checks if carrier is authorized
3. **Data Freshness** - Validates MCS-150 form date
4. **Vehicle OOS Rate** - Analyzes vehicle out-of-service percentage
5. **Driver OOS Rate** - Analyzes driver out-of-service percentage
6. **Crash History** - Reviews crash records
7. **Inspection Volume** - Checks inspection data quality
8. **Fleet Size** - Evaluates fleet concentration risk
9. **Safety Rating** - Reviews FMCSA safety rating
10. **Full Verification** - Complete workflow with final decision

## Testing with Different USDOT Numbers

To test with a different USDOT number, simply update the `TEST_USDOT` value in your `.env` file:

```
TEST_USDOT=123456
```

## API Key Information

- **Free Trial:** 15 days
- **After Trial:** $30/month
- Get your API key from: https://saferwebapi.com

## Troubleshooting

**Error: API_KEY not found**
- Make sure you created `.env` file from `.env.example`
- Verify your API key is set correctly (no quotes needed)

**Error: Failed to fetch carrier data**
- Check your API key is valid
- Verify your internet connection
- Ensure you haven't exceeded API rate limits

**Error: Module not found (Python)**
- Run `pip install -r requirements.txt` again
- Make sure you're in the `python/` directory

**Error: Package restore failed (C#)**
- Run `dotnet restore` again
- Check your internet connection for NuGet package downloads

