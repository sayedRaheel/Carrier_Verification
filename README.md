# Carrier Verification System

A comprehensive system for verifying carrier legitimacy, safety, and operational risk using FMCSA/SAFER public data via the SaferWebAPI.com API.

## Overview

This system automates carrier onboarding using FMCSA/SAFER public data to assess:
- Carrier legitimacy and identity
- Safety ratings and compliance
- Operational risk factors
- Authority status and operational patterns

## API Information

### SaferWebAPI.com V2 API

**Base URL:** `https://saferwebapi.com/v2/usdot/snapshot/:USDotNumber`

**Authentication:** Requires `x-api-key` header

**API Validity:**
- **Free Trial:** 15 days
- **After Trial:** $30/month subscription

**Features:**
- The API gets smarter with usage - it tracks patterns and pre-caches frequently queried USDOT results
- Data is current with safer.fmcsa.dot.gov
- Returns valid JSON results with a simple GET request

### Example Request

**Python:**
```python
import requests

url = "https://saferwebapi.com/v2/usdot/snapshot/44"
headers = {"x-api-key": "YourApiKey"}
response = requests.get(url, headers=headers)
```

**C#:**
```csharp
var client = new RestClient("https://saferwebapi.com/v2/usdot/snapshot/44");
var request = new RestRequest(Method.GET);
request.AddHeader("x-api-key", "YourApiKey");
IRestResponse response = client.Execute(request);
```

## Verification Framework

### 1. Identity & Ownership Verification

**Purpose:** Ensure the person onboarding is the legitimate carrier operator.

**Rule:**
- The phone number listed in FMCSA must match the phone number used for OTP verification.

**Decision:**
- **PASS** → Numbers match → proceed
- **FAIL** → Numbers mismatch → AUTO-REJECT

**Rationale:** Prevents impersonation, double brokering, and fraudulent onboarding.

### 2. Authority Status

**Purpose:** Ensure the carrier is legally allowed to haul freight.

**Rule:**
- **ACCEPT** → Authority = Active
- **DENY** → Authority = Inactive / Revoked / Out-of-Service Order

**Decision Impact:** Any DENY → Immediate rejection

### 3. Data Freshness (MCS-150 Update)

**Purpose:** Avoid decisions based on stale or misleading data.

**Rule:**
- **ACCEPT** → Updated within ≤ 12 months
- **REVIEW** → 13–24 months
- **DENY** → > 24 months

**Rationale:** Old MCS-150 data = unknown fleet size, mileage, or operations → elevated risk.

### 4. Vehicle Out-of-Service (OOS) Rate

**Purpose:** Measure vehicle maintenance and compliance quality.

**Rule:**
- **ACCEPT** → ≤ 25%
- **REVIEW** → 26–30%
- **DENY** → > 30%

**Reference:** National average ≈ 22%

### 5. Driver Out-of-Service (OOS) Rate

**Purpose:** Measure driver compliance and training quality.

**Rule:**
- **ACCEPT** → ≤ 7%
- **REVIEW** → 8–10%
- **DENY** → > 10%

### 6. Crash History (Last 24–36 Months)

**Purpose:** Identify high-severity safety risk.

**Rule:**
- **ACCEPT** → 0–1 tow, no injuries
- **REVIEW** → 1 injury
- **DENY** → ≥ 2 injuries OR any fatal crash

### 7. Inspection Volume (Signal Quality Check)

**Purpose:** Detect "low-signal" carriers that appear clean due to lack of data.

**Rule:**
- **REVIEW** → < 10 total inspections

**Rationale:** Low inspection count ≠ safe carrier. It indicates insufficient enforcement signal.

### 8. Fleet Size / Concentration Risk

**Purpose:** Account for operational fragility.

**Rule:**
- **ACCEPT** → ≥ 5 power units
- **REVIEW** → 2–4 power units
- **LIMIT / DENY** → 1 power unit

**Rationale:** Single-truck carriers have higher failure, delay, and fraud risk.

### 9. Operating Pattern Match

**Purpose:** Ensure authority aligns with intended work.

**Rule:**
- **ACCEPT** → Lane + cargo match FMCSA authority
- **REVIEW** → New or uncommon lane / cargo
- **DENY** → Authority does not cover load type

### 10. Safety Rating

**Purpose:** Use FMCSA's highest-level enforcement outcome.

**Rule:**
- **ACCEPT** → Satisfactory
- **REVIEW** → Not Rated
- **DENY** → Conditional / Unsatisfactory

## Final Decision Logic

After all checks run:

- **ANY DENY** → AUTO-REJECT
- **2 or more REVIEW flags** → CONDITIONAL APPROVAL
  - Low-value loads
  - Short hauls
  - Increased monitoring
- **All ACCEPT** → FULL APPROVAL

## Important Notes

⚠️ **Insurance verification is NOT performed in this flow**
- Auto Liability & Cargo are validated via a separate system/process

## Why This Works

- Mirrors real broker SOPs
- Deterministic & explainable (no black box)
- Resistant to fraud + low-signal carriers
- Easy to convert into code, scoring, or APIs

## Setup Instructions

### Prerequisites

**Python:**
- Python 3.7+
- `requests` library

**C#:**
- .NET 6.0 or later
- RestSharp NuGet package

### Configuration

1. Clone this repository
2. Copy `.env.example` to `.env` in the `python/` or `csharp/` directory
3. Add your SaferWebAPI.com API key to the `.env` file:
   ```
   API_KEY=your_actual_api_key_here
   ```
4. Run the test cases

### Running Tests

**Python:**
```bash
cd python
pip install -r requirements.txt
cp .env.example .env
# Edit .env and add your API key
python test_carrier_verification.py
```

**C#:**
```bash
cd csharp
dotnet restore
cp .env.example .env
# Edit .env and add your API key
dotnet run
```

## Test Example

The test cases use **USDOT 44** as an example carrier to verify:
- API connectivity
- Data retrieval
- Verification rule application
- Decision logic

## Response Format

The API returns a JSON object containing:
- Entity information (legal name, DBA, addresses, phone)
- USDOT and MC numbers
- Fleet information (power units, drivers)
- Operating status and classifications
- Inspection data (vehicle, driver, hazmat, IEP)
- Crash history (US and Canada)
- Safety ratings and reviews
- Latest update timestamp

See the test files for complete response structure examples.

## Support

For API issues or feature requests, contact SaferWebAPI.com support.

## License

This verification framework is provided as-is for carrier onboarding automation.

