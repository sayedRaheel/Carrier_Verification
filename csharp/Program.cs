using DotNetEnv;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace CarrierVerification;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("=".PadRight(60, '='));
        Console.WriteLine("CARRIER VERIFICATION SYSTEM - TEST SUITE");
        Console.WriteLine("Using USDOT 44 as test example");
        Console.WriteLine("=".PadRight(60, '='));

        // Load .env file
        Env.Load();

        // Load configuration from environment variables
        var apiKey = Environment.GetEnvironmentVariable("API_KEY");
        var apiBaseUrl = Environment.GetEnvironmentVariable("API_BASE_URL") 
            ?? "https://saferwebapi.com/v2/usdot/snapshot";
        var testUsdot = Environment.GetEnvironmentVariable("TEST_USDOT") ?? "44";

        if (string.IsNullOrEmpty(apiKey) || apiKey == "YourApiKey")
        {
            Console.WriteLine("\nERROR: Please set your API key in .env file");
            Console.WriteLine("Copy .env.example to .env and add your API key.");
            return;
        }

        // Initialize verifier
        var verifier = new CarrierVerifier(apiKey, apiBaseUrl);

        // Run individual tests
        CarrierData? data = null;
        try
        {
            data = TestApiConnectivity(verifier, testUsdot);
            if (data == null)
            {
                Console.WriteLine("\nCannot proceed with other tests - API connection failed");
                return;
            }

            TestAuthorityStatus(verifier, data);
            TestDataFreshness(verifier, data);
            TestVehicleOosRate(verifier, data);
            TestDriverOosRate(verifier, data);
            TestCrashHistory(verifier, data);
            TestInspectionVolume(verifier, data);
            TestFleetSize(verifier, data);
            TestSafetyRating(verifier, data);

            // Run full verification workflow
            TestFullVerification(verifier, testUsdot);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\nERROR: {ex.Message}");
        }

        Console.WriteLine("\n" + "=".PadRight(60, '='));
        Console.WriteLine("ALL TESTS COMPLETED");
        Console.WriteLine("=".PadRight(60, '=') + "\n");
    }

    static CarrierData? TestApiConnectivity(CarrierVerifier verifier, string usdot)
    {
        Console.WriteLine("\n" + "=".PadRight(60, '='));
        Console.WriteLine("TEST 1: API Connectivity and Data Retrieval");
        Console.WriteLine("=".PadRight(60, '='));

        try
        {
            var data = verifier.FetchCarrierData(usdot);
            Console.WriteLine($"✓ Successfully fetched data for USDOT {usdot}");
            Console.WriteLine($"  Carrier Name: {data.LegalName ?? "N/A"}");
            Console.WriteLine($"  DBA Name: {data.DbaName ?? "N/A"}");
            Console.WriteLine($"  Operating Status: {data.OperatingStatus ?? "N/A"}");
            Console.WriteLine($"  Power Units: {data.PowerUnits}");
            Console.WriteLine($"  Drivers: {data.Drivers}");
            return data;
        }
        catch (Exception e)
        {
            Console.WriteLine($"✗ Failed to fetch data: {e.Message}");
            return null;
        }
    }

    static void TestAuthorityStatus(CarrierVerifier verifier, CarrierData data)
    {
        Console.WriteLine("\n" + "=".PadRight(60, '='));
        Console.WriteLine("TEST 2: Authority Status Verification");
        Console.WriteLine("=".PadRight(60, '='));

        var result = verifier.VerifyAuthorityStatus(data);
        Console.WriteLine($"  Operating Status: {data.OperatingStatus ?? "N/A"}");
        Console.WriteLine($"  Verification Result: {result}");

        if (result == "ACCEPT")
            Console.WriteLine("  ✓ Carrier has active authority");
        else if (result == "DENY")
            Console.WriteLine("  ✗ Carrier authority is inactive/revoked");
        else
            Console.WriteLine("  ⚠ Carrier authority requires review");
    }

    static void TestDataFreshness(CarrierVerifier verifier, CarrierData data)
    {
        Console.WriteLine("\n" + "=".PadRight(60, '='));
        Console.WriteLine("TEST 3: Data Freshness (MCS-150 Update)");
        Console.WriteLine("=".PadRight(60, '='));

        var mcs150Date = data.Mcs150FormDate ?? "N/A";
        var result = verifier.VerifyDataFreshness(data);
        Console.WriteLine($"  MCS-150 Form Date: {mcs150Date}");
        Console.WriteLine($"  Verification Result: {result}");

        if (result == "ACCEPT")
            Console.WriteLine("  ✓ Data is fresh (≤12 months)");
        else if (result == "REVIEW")
            Console.WriteLine("  ⚠ Data is moderately stale (13-24 months)");
        else
            Console.WriteLine("  ✗ Data is too stale (>24 months)");
    }

    static void TestVehicleOosRate(CarrierVerifier verifier, CarrierData data)
    {
        Console.WriteLine("\n" + "=".PadRight(60, '='));
        Console.WriteLine("TEST 4: Vehicle Out-of-Service Rate");
        Console.WriteLine("=".PadRight(60, '='));

        var vehicle = data.UsInspections?.Vehicle;
        var inspections = int.TryParse(vehicle?.Inspections, out var insp) ? insp : 0;
        var outOfService = int.TryParse(vehicle?.OutOfService, out var oos) ? oos : 0;

        if (inspections > 0)
        {
            var oosRate = (outOfService / (double)inspections) * 100;
            Console.WriteLine($"  Inspections: {inspections}");
            Console.WriteLine($"  Out of Service: {outOfService}");
            Console.WriteLine($"  OOS Rate: {oosRate:F2}%");
            Console.WriteLine($"  National Average: 20.87%");
        }
        else
        {
            Console.WriteLine($"  Inspections: {inspections} (No data)");
        }

        var result = verifier.VerifyVehicleOosRate(data);
        Console.WriteLine($"  Verification Result: {result}");

        if (result == "ACCEPT")
            Console.WriteLine("  ✓ Vehicle OOS rate is acceptable (≤25%)");
        else if (result == "REVIEW")
            Console.WriteLine("  ⚠ Vehicle OOS rate requires review (26-30%)");
        else
            Console.WriteLine("  ✗ Vehicle OOS rate is too high (>30%)");
    }

    static void TestDriverOosRate(CarrierVerifier verifier, CarrierData data)
    {
        Console.WriteLine("\n" + "=".PadRight(60, '='));
        Console.WriteLine("TEST 5: Driver Out-of-Service Rate");
        Console.WriteLine("=".PadRight(60, '='));

        var driver = data.UsInspections?.Driver;
        var inspections = int.TryParse(driver?.Inspections, out var insp) ? insp : 0;
        var outOfService = int.TryParse(driver?.OutOfService, out var oos) ? oos : 0;

        if (inspections > 0)
        {
            var oosRate = (outOfService / (double)inspections) * 100;
            Console.WriteLine($"  Inspections: {inspections}");
            Console.WriteLine($"  Out of Service: {outOfService}");
            Console.WriteLine($"  OOS Rate: {oosRate:F2}%");
            Console.WriteLine($"  National Average: 5.51%");
        }
        else
        {
            Console.WriteLine($"  Inspections: {inspections} (No data)");
        }

        var result = verifier.VerifyDriverOosRate(data);
        Console.WriteLine($"  Verification Result: {result}");

        if (result == "ACCEPT")
            Console.WriteLine("  ✓ Driver OOS rate is acceptable (≤7%)");
        else if (result == "REVIEW")
            Console.WriteLine("  ⚠ Driver OOS rate requires review (8-10%)");
        else
            Console.WriteLine("  ✗ Driver OOS rate is too high (>10%)");
    }

    static void TestCrashHistory(CarrierVerifier verifier, CarrierData data)
    {
        Console.WriteLine("\n" + "=".PadRight(60, '='));
        Console.WriteLine("TEST 6: Crash History");
        Console.WriteLine("=".PadRight(60, '='));

        var crashes = data.UnitedStatesCrashes;
        var total = crashes?.Total ?? 0;
        var fatal = crashes?.Fatal ?? 0;
        var injury = crashes?.Injury ?? 0;
        var tow = crashes?.Tow ?? 0;

        Console.WriteLine($"  Total Crashes: {total}");
        Console.WriteLine($"  Fatal: {fatal}");
        Console.WriteLine($"  Injury: {injury}");
        Console.WriteLine($"  Tow: {tow}");

        var result = verifier.VerifyCrashHistory(data);
        Console.WriteLine($"  Verification Result: {result}");

        if (result == "ACCEPT")
            Console.WriteLine("  ✓ Crash history is acceptable");
        else if (result == "REVIEW")
            Console.WriteLine("  ⚠ Crash history requires review");
        else
            Console.WriteLine("  ✗ Crash history indicates high risk");
    }

    static void TestInspectionVolume(CarrierVerifier verifier, CarrierData data)
    {
        Console.WriteLine("\n" + "=".PadRight(60, '='));
        Console.WriteLine("TEST 7: Inspection Volume (Signal Quality)");
        Console.WriteLine("=".PadRight(60, '='));

        var inspections = data.UsInspections;
        var vehicleInspections = int.TryParse(inspections?.Vehicle?.Inspections, out var v) ? v : 0;
        var driverInspections = int.TryParse(inspections?.Driver?.Inspections, out var d) ? d : 0;
        var hazmatInspections = int.TryParse(inspections?.Hazmat?.Inspections, out var h) ? h : 0;
        var total = vehicleInspections + driverInspections + hazmatInspections;

        Console.WriteLine($"  Vehicle Inspections: {vehicleInspections}");
        Console.WriteLine($"  Driver Inspections: {driverInspections}");
        Console.WriteLine($"  Hazmat Inspections: {hazmatInspections}");
        Console.WriteLine($"  Total Inspections: {total}");

        var result = verifier.VerifyInspectionVolume(data);
        Console.WriteLine($"  Verification Result: {result}");

        if (result == "ACCEPT")
            Console.WriteLine("  ✓ Sufficient inspection data (≥10)");
        else
            Console.WriteLine("  ⚠ Low inspection volume (<10) - insufficient signal");
    }

    static void TestFleetSize(CarrierVerifier verifier, CarrierData data)
    {
        Console.WriteLine("\n" + "=".PadRight(60, '='));
        Console.WriteLine("TEST 8: Fleet Size");
        Console.WriteLine("=".PadRight(60, '='));

        var powerUnits = data.PowerUnits;
        var drivers = data.Drivers;

        Console.WriteLine($"  Power Units: {powerUnits}");
        Console.WriteLine($"  Drivers: {drivers}");

        var result = verifier.VerifyFleetSize(data);
        Console.WriteLine($"  Verification Result: {result}");

        if (result == "ACCEPT")
            Console.WriteLine("  ✓ Fleet size is acceptable (≥5 units)");
        else if (result == "REVIEW")
            Console.WriteLine("  ⚠ Small fleet (2-4 units) - requires review");
        else
            Console.WriteLine("  ✗ Single-truck carrier - high risk");
    }

    static void TestSafetyRating(CarrierVerifier verifier, CarrierData data)
    {
        Console.WriteLine("\n" + "=".PadRight(60, '='));
        Console.WriteLine("TEST 10: Safety Rating");
        Console.WriteLine("=".PadRight(60, '='));

        var safetyRating = data.SafetyRating ?? "NOT RATED";
        var safetyRatingDate = data.SafetyRatingDate ?? "N/A";

        Console.WriteLine($"  Safety Rating: {safetyRating}");
        Console.WriteLine($"  Rating Date: {safetyRatingDate}");

        var result = verifier.VerifySafetyRating(data);
        Console.WriteLine($"  Verification Result: {result}");

        if (result == "ACCEPT")
            Console.WriteLine("  ✓ Safety rating is satisfactory");
        else if (result == "REVIEW")
            Console.WriteLine("  ⚠ Safety rating not available - requires review");
        else
            Console.WriteLine("  ✗ Safety rating is conditional/unsatisfactory");
    }

    static void TestFullVerification(CarrierVerifier verifier, string usdot)
    {
        Console.WriteLine("\n" + "=".PadRight(60, '='));
        Console.WriteLine("FULL VERIFICATION WORKFLOW");
        Console.WriteLine("=".PadRight(60, '='));

        try
        {
            Console.WriteLine("\nRunning verification without phone number...");
            var results = verifier.VerifyCarrier(usdot);

            Console.WriteLine($"\nCarrier: {results.CarrierName}");
            Console.WriteLine($"USDOT: {results.Usdot}");
            Console.WriteLine("\nVerification Checks:");
            foreach (var check in results.Checks)
            {
                var checkName = string.Join(" ", check.Key.Split('_').Select(s => 
                    char.ToUpper(s[0]) + s.Substring(1).ToLower()));
                Console.WriteLine($"  {checkName}: {check.Value}");
            }

            Console.WriteLine($"\nFinal Decision: {results.FinalDecision}");
            Console.WriteLine($"Reason: {results.Reason}");

            // Test with phone verification
            if (results.RawData?.Phone != null)
            {
                Console.WriteLine($"\n\nRunning verification with phone number: {results.RawData.Phone}");
                var resultsWithPhone = verifier.VerifyCarrier(usdot, results.RawData.Phone);
                Console.WriteLine($"Identity Verification: {resultsWithPhone.Checks["identity_verification"]}");
            }
        }
        catch (Exception e)
        {
            Console.WriteLine($"✗ Verification failed: {e.Message}");
        }
    }
}

