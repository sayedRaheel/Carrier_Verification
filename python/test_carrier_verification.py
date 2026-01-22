"""
Test Cases for Carrier Verification System
Tests using USDOT 44 as an example carrier
"""

import os
import sys
from dotenv import load_dotenv
from carrier_verifier import CarrierVerifier


def load_config():
    """Load configuration from .env file"""
    # Load .env file
    load_dotenv()
    
    api_key = os.getenv("API_KEY", "").strip()  # Strip whitespace
    api_base_url = os.getenv("API_BASE_URL", "https://saferwebapi.com/v2/usdot/snapshot").strip()
    test_usdot = os.getenv("TEST_USDOT", "44").strip()
    
    if not api_key:
        print("ERROR: API_KEY not found in .env file!")
        print("Please copy .env.example to .env and add your API key.")
        sys.exit(1)
    
    return {
        "api_key": api_key,
        "api_base_url": api_base_url,
        "test_usdot": test_usdot
    }


def test_api_connectivity(verifier: CarrierVerifier, usdot: str):
    """Test 1: Verify API connectivity and data retrieval"""
    print("\n" + "="*60)
    print("TEST 1: API Connectivity and Data Retrieval")
    print("="*60)
    
    try:
        data = verifier.fetch_carrier_data(usdot)
        print(f"✓ Successfully fetched data for USDOT {usdot}")
        print(f"  Carrier Name: {data.get('legal_name', 'N/A')}")
        print(f"  DBA Name: {data.get('dba_name', 'N/A')}")
        print(f"  Operating Status: {data.get('operating_status', 'N/A')}")
        print(f"  Power Units: {data.get('power_units', 'N/A')}")
        print(f"  Drivers: {data.get('drivers', 'N/A')}")
        return True, data
    except Exception as e:
        print(f"✗ Failed to fetch data: {str(e)}")
        return False, None


def test_authority_status(verifier: CarrierVerifier, data: dict):
    """Test 2: Authority Status Verification"""
    print("\n" + "="*60)
    print("TEST 2: Authority Status Verification")
    print("="*60)
    
    result = verifier.verify_authority_status(data)
    operating_status = data.get("operating_status", "N/A")
    
    print(f"  Operating Status: {operating_status}")
    print(f"  Verification Result: {result}")
    
    if result == "ACCEPT":
        print("  ✓ Carrier has active authority")
    elif result == "DENY":
        print("  ✗ Carrier authority is inactive/revoked")
    else:
        print("  ⚠ Carrier authority requires review")
    
    return result


def test_data_freshness(verifier: CarrierVerifier, data: dict):
    """Test 3: Data Freshness Verification"""
    print("\n" + "="*60)
    print("TEST 3: Data Freshness (MCS-150 Update)")
    print("="*60)
    
    mcs_150_date = data.get("mcs_150_form_date", "N/A")
    result = verifier.verify_data_freshness(data)
    
    print(f"  MCS-150 Form Date: {mcs_150_date}")
    print(f"  Verification Result: {result}")
    
    if result == "ACCEPT":
        print("  ✓ Data is fresh (≤12 months)")
    elif result == "REVIEW":
        print("  ⚠ Data is moderately stale (13-24 months)")
    else:
        print("  ✗ Data is too stale (>24 months)")
    
    return result


def test_vehicle_oos_rate(verifier: CarrierVerifier, data: dict):
    """Test 4: Vehicle Out-of-Service Rate"""
    print("\n" + "="*60)
    print("TEST 4: Vehicle Out-of-Service Rate")
    print("="*60)
    
    us_inspections = data.get("us_inspections", {})
    vehicle = us_inspections.get("vehicle", {})
    inspections = int(vehicle.get("inspections", 0))
    out_of_service = int(vehicle.get("out_of_service", 0))
    
    if inspections > 0:
        oos_rate = (out_of_service / inspections) * 100
        print(f"  Inspections: {inspections}")
        print(f"  Out of Service: {out_of_service}")
        print(f"  OOS Rate: {oos_rate:.2f}%")
        print(f"  National Average: 20.87%")
    else:
        print(f"  Inspections: {inspections} (No data)")
        oos_rate = 0
    
    result = verifier.verify_vehicle_oos_rate(data)
    print(f"  Verification Result: {result}")
    
    if result == "ACCEPT":
        print("  ✓ Vehicle OOS rate is acceptable (≤25%)")
    elif result == "REVIEW":
        print("  ⚠ Vehicle OOS rate requires review (26-30%)")
    else:
        print("  ✗ Vehicle OOS rate is too high (>30%)")
    
    return result


def test_driver_oos_rate(verifier: CarrierVerifier, data: dict):
    """Test 5: Driver Out-of-Service Rate"""
    print("\n" + "="*60)
    print("TEST 5: Driver Out-of-Service Rate")
    print("="*60)
    
    us_inspections = data.get("us_inspections", {})
    driver = us_inspections.get("driver", {})
    inspections = int(driver.get("inspections", 0))
    out_of_service = int(driver.get("out_of_service", 0))
    
    if inspections > 0:
        oos_rate = (out_of_service / inspections) * 100
        print(f"  Inspections: {inspections}")
        print(f"  Out of Service: {out_of_service}")
        print(f"  OOS Rate: {oos_rate:.2f}%")
        print(f"  National Average: 5.51%")
    else:
        print(f"  Inspections: {inspections} (No data)")
        oos_rate = 0
    
    result = verifier.verify_driver_oos_rate(data)
    print(f"  Verification Result: {result}")
    
    if result == "ACCEPT":
        print("  ✓ Driver OOS rate is acceptable (≤7%)")
    elif result == "REVIEW":
        print("  ⚠ Driver OOS rate requires review (8-10%)")
    else:
        print("  ✗ Driver OOS rate is too high (>10%)")
    
    return result


def test_crash_history(verifier: CarrierVerifier, data: dict):
    """Test 6: Crash History"""
    print("\n" + "="*60)
    print("TEST 6: Crash History")
    print("="*60)
    
    us_crashes = data.get("united_states_crashes", {})
    tow = int(us_crashes.get("tow", 0))
    injury = int(us_crashes.get("injury", 0))
    fatal = int(us_crashes.get("fatal", 0))
    total = int(us_crashes.get("total", 0))
    
    print(f"  Total Crashes: {total}")
    print(f"  Fatal: {fatal}")
    print(f"  Injury: {injury}")
    print(f"  Tow: {tow}")
    
    result = verifier.verify_crash_history(data)
    print(f"  Verification Result: {result}")
    
    if result == "ACCEPT":
        print("  ✓ Crash history is acceptable")
    elif result == "REVIEW":
        print("  ⚠ Crash history requires review")
    else:
        print("  ✗ Crash history indicates high risk")
    
    return result


def test_inspection_volume(verifier: CarrierVerifier, data: dict):
    """Test 7: Inspection Volume"""
    print("\n" + "="*60)
    print("TEST 7: Inspection Volume (Signal Quality)")
    print("="*60)
    
    us_inspections = data.get("us_inspections", {})
    vehicle_inspections = int(us_inspections.get("vehicle", {}).get("inspections", 0))
    driver_inspections = int(us_inspections.get("driver", {}).get("inspections", 0))
    hazmat_inspections = int(us_inspections.get("hazmat", {}).get("inspections", 0))
    total = vehicle_inspections + driver_inspections + hazmat_inspections
    
    print(f"  Vehicle Inspections: {vehicle_inspections}")
    print(f"  Driver Inspections: {driver_inspections}")
    print(f"  Hazmat Inspections: {hazmat_inspections}")
    print(f"  Total Inspections: {total}")
    
    result = verifier.verify_inspection_volume(data)
    print(f"  Verification Result: {result}")
    
    if result == "ACCEPT":
        print("  ✓ Sufficient inspection data (≥10)")
    else:
        print("  ⚠ Low inspection volume (<10) - insufficient signal")
    
    return result


def test_fleet_size(verifier: CarrierVerifier, data: dict):
    """Test 8: Fleet Size"""
    print("\n" + "="*60)
    print("TEST 8: Fleet Size")
    print("="*60)
    
    power_units = int(data.get("power_units", 0))
    drivers = int(data.get("drivers", 0))
    
    print(f"  Power Units: {power_units}")
    print(f"  Drivers: {drivers}")
    
    result = verifier.verify_fleet_size(data)
    print(f"  Verification Result: {result}")
    
    if result == "ACCEPT":
        print("  ✓ Fleet size is acceptable (≥5 units)")
    elif result == "REVIEW":
        print("  ⚠ Small fleet (2-4 units) - requires review")
    else:
        print("  ✗ Single-truck carrier - high risk")
    
    return result


def test_safety_rating(verifier: CarrierVerifier, data: dict):
    """Test 10: Safety Rating"""
    print("\n" + "="*60)
    print("TEST 10: Safety Rating")
    print("="*60)
    
    safety_rating = data.get("safety_rating", "NOT RATED")
    safety_rating_date = data.get("safety_rating_date", "N/A")
    
    print(f"  Safety Rating: {safety_rating if safety_rating else 'NOT RATED'}")
    print(f"  Rating Date: {safety_rating_date if safety_rating_date else 'N/A'}")
    
    result = verifier.verify_safety_rating(data)
    print(f"  Verification Result: {result}")
    
    if result == "ACCEPT":
        print("  ✓ Safety rating is satisfactory")
    elif result == "REVIEW":
        print("  ⚠ Safety rating not available - requires review")
    else:
        print("  ✗ Safety rating is conditional/unsatisfactory")
    
    return result


def test_full_verification(verifier: CarrierVerifier, usdot: str):
    """Test: Full Verification Workflow"""
    print("\n" + "="*60)
    print("FULL VERIFICATION WORKFLOW")
    print("="*60)
    
    try:
        # Test without phone verification
        print("\nRunning verification without phone number...")
        results = verifier.verify_carrier(usdot)
        
        print(f"\nCarrier: {results['carrier_name']}")
        print(f"USDOT: {results['usdot']}")
        print(f"\nVerification Checks:")
        for check, result in results['checks'].items():
            print(f"  {check.replace('_', ' ').title()}: {result}")
        
        print(f"\nFinal Decision: {results['final_decision']}")
        print(f"Reason: {results['reason']}")
        
        # Test with phone verification (using example phone from data)
        if 'raw_data' in results:
            fmcsa_phone = results['raw_data'].get('phone', '')
            if fmcsa_phone:
                print(f"\n\nRunning verification with phone number: {fmcsa_phone}")
                results_with_phone = verifier.verify_carrier(usdot, phone_number=fmcsa_phone)
                print(f"Identity Verification: {results_with_phone['checks']['identity_verification']}")
        
        return results
    except Exception as e:
        print(f"✗ Verification failed: {str(e)}")
        return None


def main():
    """Run all test cases"""
    print("\n" + "="*60)
    print("CARRIER VERIFICATION SYSTEM - TEST SUITE")
    print("Using USDOT 44 as test example")
    print("="*60)
    
    # Load configuration
    config = load_config()
    api_key = config.get("api_key")
    test_usdot = config.get("test_usdot", "44")
    
    if not api_key or api_key == "YourApiKey":
        print("\nERROR: Please set your API key in .env file")
        sys.exit(1)
    
    # Debug: Show API key length (first 4 and last 4 chars for security)
    if api_key:
        masked_key = f"{api_key[:4]}...{api_key[-4:]}" if len(api_key) > 8 else "***"
        print(f"\nAPI Key loaded: {masked_key} (length: {len(api_key)})")
    
    # Initialize verifier
    verifier = CarrierVerifier(api_key, config.get("api_base_url"))
    
    # Run individual tests
    success, data = test_api_connectivity(verifier, test_usdot)
    if not success:
        print("\nCannot proceed with other tests - API connection failed")
        sys.exit(1)
    
    test_authority_status(verifier, data)
    test_data_freshness(verifier, data)
    test_vehicle_oos_rate(verifier, data)
    test_driver_oos_rate(verifier, data)
    test_crash_history(verifier, data)
    test_inspection_volume(verifier, data)
    test_fleet_size(verifier, data)
    test_safety_rating(verifier, data)
    
    # Run full verification workflow
    test_full_verification(verifier, test_usdot)
    
    print("\n" + "="*60)
    print("ALL TESTS COMPLETED")
    print("="*60 + "\n")


if __name__ == "__main__":
    main()

