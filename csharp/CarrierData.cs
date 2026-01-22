using Newtonsoft.Json;

namespace CarrierVerification;

public class CarrierData
{
    [JsonProperty("entity_type")]
    public string? EntityType { get; set; }

    [JsonProperty("legal_name")]
    public string? LegalName { get; set; }

    [JsonProperty("dba_name")]
    public string? DbaName { get; set; }

    [JsonProperty("physical_address")]
    public string? PhysicalAddress { get; set; }

    [JsonProperty("phone")]
    public string? Phone { get; set; }

    [JsonProperty("mailing_address")]
    public string? MailingAddress { get; set; }

    [JsonProperty("usdot")]
    public string? Usdot { get; set; }

    [JsonProperty("state_carrier_id")]
    public string? StateCarrierId { get; set; }

    [JsonProperty("mc_mx_ff_numbers")]
    public string? McMxFfNumbers { get; set; }

    [JsonProperty("duns_number")]
    public string? DunsNumber { get; set; }

    [JsonProperty("power_units")]
    public int PowerUnits { get; set; }

    [JsonProperty("drivers")]
    public int Drivers { get; set; }

    [JsonProperty("mcs_150_form_date")]
    public string? Mcs150FormDate { get; set; }

    [JsonProperty("operating_status")]
    public string? OperatingStatus { get; set; }

    [JsonProperty("us_inspections")]
    public UsInspections? UsInspections { get; set; }

    [JsonProperty("united_states_crashes")]
    public UnitedStatesCrashes? UnitedStatesCrashes { get; set; }

    [JsonProperty("safety_rating")]
    public string? SafetyRating { get; set; }

    [JsonProperty("safety_rating_date")]
    public string? SafetyRatingDate { get; set; }

    [JsonProperty("latest_update")]
    public string? LatestUpdate { get; set; }
}

public class UsInspections
{
    [JsonProperty("vehicle")]
    public InspectionData? Vehicle { get; set; }

    [JsonProperty("driver")]
    public InspectionData? Driver { get; set; }

    [JsonProperty("hazmat")]
    public InspectionData? Hazmat { get; set; }

    [JsonProperty("iep")]
    public InspectionData? Iep { get; set; }
}

public class InspectionData
{
    [JsonProperty("inspections")]
    public string? Inspections { get; set; }

    [JsonProperty("out_of_service")]
    public string? OutOfService { get; set; }

    [JsonProperty("out_of_service_percent")]
    public string? OutOfServicePercent { get; set; }

    [JsonProperty("national_average")]
    public string? NationalAverage { get; set; }
}

public class UnitedStatesCrashes
{
    [JsonProperty("tow")]
    public int Tow { get; set; }

    [JsonProperty("fatal")]
    public int Fatal { get; set; }

    [JsonProperty("injury")]
    public int Injury { get; set; }

    [JsonProperty("total")]
    public int Total { get; set; }
}

public class VerificationResult
{
    public string Usdot { get; set; } = string.Empty;
    public string CarrierName { get; set; } = string.Empty;
    public string DbaName { get; set; } = string.Empty;
    public Dictionary<string, string> Checks { get; set; } = new();
    public string? FinalDecision { get; set; }
    public string Reason { get; set; } = string.Empty;
    public CarrierData? RawData { get; set; }
}

