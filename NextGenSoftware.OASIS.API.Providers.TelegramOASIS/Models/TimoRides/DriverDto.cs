using System;
using System.Collections.Generic;

namespace NextGenSoftware.OASIS.API.Providers.TelegramOASIS.Models.TimoRides
{
    public class DriverDto
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public double Rating { get; set; }
        public int TotalRides { get; set; }
        public int KarmaScore { get; set; }
        public string PhotoUrl { get; set; }
        public string PhoneNumber { get; set; }
        
        // Vehicle Info
        public string VehicleModel { get; set; }
        public string VehicleColor { get; set; }
        public string VehiclePlate { get; set; }
        public string VehicleType { get; set; } // sedan, suv, luxury
        
        // Location
        public double CurrentLatitude { get; set; }
        public double CurrentLongitude { get; set; }
        public double DistanceFromPickupKm { get; set; }
        public int ETAMinutes { get; set; }
        
        // Pricing
        public decimal EstimatedFare { get; set; }
        public string Currency { get; set; }
        
        // Features
        public List<string> Languages { get; set; } = new List<string>();
        public List<string> Amenities { get; set; } = new List<string>(); // ac, wifi, music, child_seat
        
        // Availability
        public bool IsAvailable { get; set; }
        public bool IsOnline { get; set; }
        
        public string GetFullName() => $"{FirstName} {LastName}";
        
        public string GetVehicleDescription() => 
            $"{VehicleColor} {VehicleModel} ({VehiclePlate})";
        
        public string GetKarmaBadge()
        {
            if (KarmaScore >= 1000) return "Legend";
            if (KarmaScore >= 750) return "Trusted";
            if (KarmaScore >= 500) return "Good";
            if (KarmaScore >= 250) return "Rising";
            return "New";
        }
    }
    
    public class DriversResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public List<DriverDto> Drivers { get; set; } = new List<DriverDto>();
        public int TotalCount { get; set; }
    }
}



