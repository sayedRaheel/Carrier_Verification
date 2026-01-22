"""
Carrier Verification Module
Implements the FMCSA-Based Decision Framework for carrier onboarding
"""

import json
import requests
from datetime import datetime
from dateutil import parser
from typing import Dict, List, Tuple, Optional


class CarrierVerifier:
    """Main class for carrier verification using FMCSA/SAFER data"""
    
    def __init__(self, api_key: str, api_base_url: str = "https://saferwebapi.com/v2/usdot/snapshot"):
        self.api_key = api_key
        self.api_base_url = api_base_url
        self.headers = {"x-api-key": api_key}
    
    def fetch_carrier_data(self, usdot: str) -> Dict:
        """Fetch carrier snapshot data from SaferWebAPI"""
        url = f"{self.api_base_url}/{usdot}"
        try:
            response = requests.get(url, headers=self.headers)
            response.raise_for_status()
            return response.json()
        except requests.exceptions.HTTPError as e:
            if response.status_code == 401:
                raise Exception(f"Unauthorized (401): Invalid or expired API key. Please check your API key in .env file.")
            else:
                raise Exception(f"HTTP Error {response.status_code}: {str(e)}")
        except requests.exceptions.RequestException as e:
            raise Exception(f"Failed to fetch carrier data: {str(e)}")
    
    def verify_authority_status(self, data: Dict) -> str:
        """Rule 2: Authority Status"""
        operating_status = data.get("operating_status", "").upper()
        
        if operating_status in ["ACTIVE", "AUTHORIZED"]:
            return "ACCEPT"
        elif operating_status in ["NOT AUTHORIZED", "INACTIVE", "OUT OF SERVICE"]:
            return "DENY"
        else:
            return "REVIEW"
    
    def verify_data_freshness(self, data: Dict) -> str:
        """Rule 3: Data Freshness (MCS-150 Update)"""
        mcs_150_date_str = data.get("mcs_150_form_date")
        if not mcs_150_date_str:
            return "DENY"
        
        try:
            mcs_150_date = parser.parse(mcs_150_date_str)
            months_old = (datetime.now() - mcs_150_date).days / 30.0
            
            if months_old <= 12:
                return "ACCEPT"
            elif months_old <= 24:
                return "REVIEW"
            else:
                return "DENY"
        except Exception:
            return "DENY"
    
    def verify_vehicle_oos_rate(self, data: Dict) -> str:
        """Rule 4: Vehicle Out-of-Service Rate"""
        us_inspections = data.get("us_inspections", {})
        vehicle = us_inspections.get("vehicle", {})
        
        inspections = int(vehicle.get("inspections", 0))
        out_of_service = int(vehicle.get("out_of_service", 0))
        
        if inspections == 0:
            return "REVIEW"  # No data
        
        oos_rate = (out_of_service / inspections) * 100
        
        if oos_rate <= 25:
            return "ACCEPT"
        elif oos_rate <= 30:
            return "REVIEW"
        else:
            return "DENY"
    
    def verify_driver_oos_rate(self, data: Dict) -> str:
        """Rule 5: Driver Out-of-Service Rate"""
        us_inspections = data.get("us_inspections", {})
        driver = us_inspections.get("driver", {})
        
        inspections = int(driver.get("inspections", 0))
        out_of_service = int(driver.get("out_of_service", 0))
        
        if inspections == 0:
            return "REVIEW"  # No data
        
        oos_rate = (out_of_service / inspections) * 100
        
        if oos_rate <= 7:
            return "ACCEPT"
        elif oos_rate <= 10:
            return "REVIEW"
        else:
            return "DENY"
    
    def verify_crash_history(self, data: Dict) -> str:
        """Rule 6: Crash History"""
        us_crashes = data.get("united_states_crashes", {})
        
        tow = int(us_crashes.get("tow", 0))
        injury = int(us_crashes.get("injury", 0))
        fatal = int(us_crashes.get("fatal", 0))
        
        if fatal > 0 or injury >= 2:
            return "DENY"
        elif injury == 1:
            return "REVIEW"
        elif tow <= 1:
            return "ACCEPT"
        else:
            return "REVIEW"
    
    def verify_inspection_volume(self, data: Dict) -> str:
        """Rule 7: Inspection Volume"""
        us_inspections = data.get("us_inspections", {})
        
        vehicle_inspections = int(us_inspections.get("vehicle", {}).get("inspections", 0))
        driver_inspections = int(us_inspections.get("driver", {}).get("inspections", 0))
        hazmat_inspections = int(us_inspections.get("hazmat", {}).get("inspections", 0))
        
        total_inspections = vehicle_inspections + driver_inspections + hazmat_inspections
        
        if total_inspections < 10:
            return "REVIEW"
        else:
            return "ACCEPT"
    
    def verify_fleet_size(self, data: Dict) -> str:
        """Rule 8: Fleet Size"""
        power_units = int(data.get("power_units", 0))
        
        if power_units >= 5:
            return "ACCEPT"
        elif power_units >= 2:
            return "REVIEW"
        else:
            return "DENY"
    
    def verify_safety_rating(self, data: Dict) -> str:
        """Rule 10: Safety Rating"""
        safety_rating = data.get("safety_rating", "").upper() if data.get("safety_rating") else "NOT RATED"
        
        if safety_rating == "SATISFACTORY":
            return "ACCEPT"
        elif safety_rating in ["CONDITIONAL", "UNSATISFACTORY"]:
            return "DENY"
        else:  # NOT RATED or None
            return "REVIEW"
    
    def verify_carrier(self, usdot: str, phone_number: Optional[str] = None) -> Dict:
        """
        Main verification method
        
        Args:
            usdot: USDOT number to verify
            phone_number: Optional phone number for identity verification (Rule 1)
        
        Returns:
            Dictionary with verification results and final decision
        """
        # Fetch carrier data
        data = self.fetch_carrier_data(usdot)
        
        results = {
            "usdot": usdot,
            "carrier_name": data.get("legal_name", "Unknown"),
            "dba_name": data.get("dba_name", ""),
            "checks": {},
            "final_decision": None,
            "reason": ""
        }
        
        # Rule 1: Identity & Ownership Verification (if phone provided)
        if phone_number:
            fmcsa_phone = data.get("phone", "").replace(" ", "").replace("-", "").replace("(", "").replace(")", "")
            provided_phone = phone_number.replace(" ", "").replace("-", "").replace("(", "").replace(")", "")
            if fmcsa_phone == provided_phone:
                results["checks"]["identity_verification"] = "PASS"
            else:
                results["checks"]["identity_verification"] = "FAIL"
                results["final_decision"] = "AUTO-REJECT"
                results["reason"] = "Phone number mismatch"
                return results
        else:
            results["checks"]["identity_verification"] = "SKIPPED"
        
        # Rule 2: Authority Status
        results["checks"]["authority_status"] = self.verify_authority_status(data)
        
        # Rule 3: Data Freshness
        results["checks"]["data_freshness"] = self.verify_data_freshness(data)
        
        # Rule 4: Vehicle OOS Rate
        results["checks"]["vehicle_oos_rate"] = self.verify_vehicle_oos_rate(data)
        
        # Rule 5: Driver OOS Rate
        results["checks"]["driver_oos_rate"] = self.verify_driver_oos_rate(data)
        
        # Rule 6: Crash History
        results["checks"]["crash_history"] = self.verify_crash_history(data)
        
        # Rule 7: Inspection Volume
        results["checks"]["inspection_volume"] = self.verify_inspection_volume(data)
        
        # Rule 8: Fleet Size
        results["checks"]["fleet_size"] = self.verify_fleet_size(data)
        
        # Rule 10: Safety Rating
        results["checks"]["safety_rating"] = self.verify_safety_rating(data)
        
        # Final Decision Logic
        check_results = list(results["checks"].values())
        deny_count = check_results.count("DENY")
        review_count = check_results.count("REVIEW")
        
        if deny_count > 0:
            results["final_decision"] = "AUTO-REJECT"
            results["reason"] = f"{deny_count} DENY flag(s) found"
        elif review_count >= 2:
            results["final_decision"] = "CONDITIONAL_APPROVAL"
            results["reason"] = f"{review_count} REVIEW flag(s) - requires increased monitoring"
        else:
            results["final_decision"] = "FULL_APPROVAL"
            results["reason"] = "All checks passed"
        
        results["raw_data"] = data
        return results

