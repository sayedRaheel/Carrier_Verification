using RestSharp;
using Newtonsoft.Json;
using System.Text.RegularExpressions;

namespace CarrierVerification;

public class CarrierVerifier
{
    private readonly string _apiKey;
    private readonly string _apiBaseUrl;

    public CarrierVerifier(string apiKey, string apiBaseUrl = "https://saferwebapi.com/v2/usdot/snapshot")
    {
        _apiKey = apiKey;
        _apiBaseUrl = apiBaseUrl;
    }

    public CarrierData FetchCarrierData(string usdot)
    {
        var client = new RestClient(_apiBaseUrl);
        var request = new RestRequest($"/{usdot}", Method.Get);
        request.AddHeader("x-api-key", _apiKey);

        var response = client.Execute(request);
        
        if (!response.IsSuccessful)
        {
            throw new Exception($"Failed to fetch carrier data: {response.ErrorMessage ?? response.StatusCode.ToString()}");
        }

        return JsonConvert.DeserializeObject<CarrierData>(response.Content!) 
            ?? throw new Exception("Failed to deserialize carrier data");
    }

    public string VerifyAuthorityStatus(CarrierData data)
    {
        var operatingStatus = data.OperatingStatus?.ToUpper() ?? "";

        if (operatingStatus.Contains("ACTIVE") || operatingStatus.Contains("AUTHORIZED"))
        {
            return "ACCEPT";
        }
        else if (operatingStatus.Contains("NOT AUTHORIZED") || 
                 operatingStatus.Contains("INACTIVE") || 
                 operatingStatus.Contains("OUT OF SERVICE"))
        {
            return "DENY";
        }
        else
        {
            return "REVIEW";
        }
    }

    public string VerifyDataFreshness(CarrierData data)
    {
        if (string.IsNullOrEmpty(data.Mcs150FormDate))
        {
            return "DENY";
        }

        try
        {
            if (DateTime.TryParse(data.Mcs150FormDate, out var mcs150Date))
            {
                var monthsOld = (DateTime.Now - mcs150Date).TotalDays / 30.0;

                if (monthsOld <= 12)
                {
                    return "ACCEPT";
                }
                else if (monthsOld <= 24)
                {
                    return "REVIEW";
                }
                else
                {
                    return "DENY";
                }
            }
        }
        catch
        {
            // Fall through to DENY
        }

        return "DENY";
    }

    public string VerifyVehicleOosRate(CarrierData data)
    {
        var vehicle = data.UsInspections?.Vehicle;
        if (vehicle == null)
        {
            return "REVIEW";
        }

        var inspections = int.TryParse(vehicle.Inspections, out var insp) ? insp : 0;
        var outOfService = int.TryParse(vehicle.OutOfService, out var oos) ? oos : 0;

        if (inspections == 0)
        {
            return "REVIEW";
        }

        var oosRate = (outOfService / (double)inspections) * 100;

        if (oosRate <= 25)
        {
            return "ACCEPT";
        }
        else if (oosRate <= 30)
        {
            return "REVIEW";
        }
        else
        {
            return "DENY";
        }
    }

    public string VerifyDriverOosRate(CarrierData data)
    {
        var driver = data.UsInspections?.Driver;
        if (driver == null)
        {
            return "REVIEW";
        }

        var inspections = int.TryParse(driver.Inspections, out var insp) ? insp : 0;
        var outOfService = int.TryParse(driver.OutOfService, out var oos) ? oos : 0;

        if (inspections == 0)
        {
            return "REVIEW";
        }

        var oosRate = (outOfService / (double)inspections) * 100;

        if (oosRate <= 7)
        {
            return "ACCEPT";
        }
        else if (oosRate <= 10)
        {
            return "REVIEW";
        }
        else
        {
            return "DENY";
        }
    }

    public string VerifyCrashHistory(CarrierData data)
    {
        var crashes = data.UnitedStatesCrashes;
        if (crashes == null)
        {
            return "ACCEPT";
        }

        var fatal = crashes.Fatal;
        var injury = crashes.Injury;
        var tow = crashes.Tow;

        if (fatal > 0 || injury >= 2)
        {
            return "DENY";
        }
        else if (injury == 1)
        {
            return "REVIEW";
        }
        else if (tow <= 1)
        {
            return "ACCEPT";
        }
        else
        {
            return "REVIEW";
        }
    }

    public string VerifyInspectionVolume(CarrierData data)
    {
        var inspections = data.UsInspections;
        if (inspections == null)
        {
            return "REVIEW";
        }

        var vehicleInspections = int.TryParse(inspections.Vehicle?.Inspections, out var v) ? v : 0;
        var driverInspections = int.TryParse(inspections.Driver?.Inspections, out var d) ? d : 0;
        var hazmatInspections = int.TryParse(inspections.Hazmat?.Inspections, out var h) ? h : 0;

        var totalInspections = vehicleInspections + driverInspections + hazmatInspections;

        if (totalInspections < 10)
        {
            return "REVIEW";
        }
        else
        {
            return "ACCEPT";
        }
    }

    public string VerifyFleetSize(CarrierData data)
    {
        var powerUnits = data.PowerUnits;

        if (powerUnits >= 5)
        {
            return "ACCEPT";
        }
        else if (powerUnits >= 2)
        {
            return "REVIEW";
        }
        else
        {
            return "DENY";
        }
    }

    public string VerifySafetyRating(CarrierData data)
    {
        var safetyRating = data.SafetyRating?.ToUpper() ?? "NOT RATED";

        if (safetyRating == "SATISFACTORY")
        {
            return "ACCEPT";
        }
        else if (safetyRating == "CONDITIONAL" || safetyRating == "UNSATISFACTORY")
        {
            return "DENY";
        }
        else
        {
            return "REVIEW";
        }
    }

    public VerificationResult VerifyCarrier(string usdot, string? phoneNumber = null)
    {
        var data = FetchCarrierData(usdot);
        var result = new VerificationResult
        {
            Usdot = usdot,
            CarrierName = data.LegalName ?? "Unknown",
            DbaName = data.DbaName ?? "",
            RawData = data
        };

        // Rule 1: Identity & Ownership Verification
        if (!string.IsNullOrEmpty(phoneNumber))
        {
            var fmcsaPhone = NormalizePhone(data.Phone ?? "");
            var providedPhone = NormalizePhone(phoneNumber);
            
            if (fmcsaPhone == providedPhone)
            {
                result.Checks["identity_verification"] = "PASS";
            }
            else
            {
                result.Checks["identity_verification"] = "FAIL";
                result.FinalDecision = "AUTO-REJECT";
                result.Reason = "Phone number mismatch";
                return result;
            }
        }
        else
        {
            result.Checks["identity_verification"] = "SKIPPED";
        }

        // Rule 2: Authority Status
        result.Checks["authority_status"] = VerifyAuthorityStatus(data);

        // Rule 3: Data Freshness
        result.Checks["data_freshness"] = VerifyDataFreshness(data);

        // Rule 4: Vehicle OOS Rate
        result.Checks["vehicle_oos_rate"] = VerifyVehicleOosRate(data);

        // Rule 5: Driver OOS Rate
        result.Checks["driver_oos_rate"] = VerifyDriverOosRate(data);

        // Rule 6: Crash History
        result.Checks["crash_history"] = VerifyCrashHistory(data);

        // Rule 7: Inspection Volume
        result.Checks["inspection_volume"] = VerifyInspectionVolume(data);

        // Rule 8: Fleet Size
        result.Checks["fleet_size"] = VerifyFleetSize(data);

        // Rule 10: Safety Rating
        result.Checks["safety_rating"] = VerifySafetyRating(data);

        // Final Decision Logic
        var denyCount = result.Checks.Values.Count(v => v == "DENY");
        var reviewCount = result.Checks.Values.Count(v => v == "REVIEW");

        if (denyCount > 0)
        {
            result.FinalDecision = "AUTO-REJECT";
            result.Reason = $"{denyCount} DENY flag(s) found";
        }
        else if (reviewCount >= 2)
        {
            result.FinalDecision = "CONDITIONAL_APPROVAL";
            result.Reason = $"{reviewCount} REVIEW flag(s) - requires increased monitoring";
        }
        else
        {
            result.FinalDecision = "FULL_APPROVAL";
            result.Reason = "All checks passed";
        }

        return result;
    }

    private string NormalizePhone(string phone)
    {
        return Regex.Replace(phone, @"[\s\-\(\)]", "");
    }
}

