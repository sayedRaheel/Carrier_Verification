# Python Carrier Verification Tests

## Setup

1. Install dependencies:
```bash
pip install -r requirements.txt
```

2. Configure API key:
```bash
cp .env.example .env
# Edit .env and add your API key:
# API_KEY=your_actual_api_key_here
```

## Running Tests

```bash
python test_carrier_verification.py
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

