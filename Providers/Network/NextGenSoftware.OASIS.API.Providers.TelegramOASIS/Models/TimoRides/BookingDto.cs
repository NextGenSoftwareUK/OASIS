using System;

namespace NextGenSoftware.OASIS.API.Providers.TelegramOASIS.Models.TimoRides
{
    public class BookingRequest
    {
        public string UserId { get; set; }
        public long UserTelegramId { get; set; }
        public string DriverId { get; set; }
        
        public double PickupLatitude { get; set; }
        public double PickupLongitude { get; set; }
        public string PickupAddress { get; set; }
        
        public double DropoffLatitude { get; set; }
        public double DropoffLongitude { get; set; }
        public string DropoffAddress { get; set; }
        
        public string PaymentMethod { get; set; } // wallet, cash, mpesa, card
        public decimal EstimatedFare { get; set; }
        public string Currency { get; set; }
        
        public string VehicleType { get; set; }
        public string Notes { get; set; }
    }
    
    public class BookingDto
    {
        public string Id { get; set; }
        public string BookingId { get; set; } // Human-readable: BK-20251028-A7X9
        public string UserId { get; set; }
        public string DriverId { get; set; }
        
        public string DriverName { get; set; }
        public string DriverPhone { get; set; }
        public string VehicleDetails { get; set; }
        
        public LocationDto Pickup { get; set; }
        public LocationDto Dropoff { get; set; }
        
        public decimal EstimatedFare { get; set; }
        public decimal FinalFare { get; set; }
        public string Currency { get; set; }
        
        public string PaymentMethod { get; set; }
        public string PaymentStatus { get; set; } // pending, completed, failed
        
        public string Status { get; set; } // pending, accepted, driver_en_route, arrived, started, completed, cancelled
        
        public int DriverETAMinutes { get; set; }
        public double DistanceKm { get; set; }
        public int EstimatedDurationMinutes { get; set; }
        
        public DateTime CreatedAt { get; set; }
        public DateTime? AcceptedAt { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
    }
    
    public class BookingResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public BookingDto Booking { get; set; }
    }
    
    public class LocationDto
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string Address { get; set; }
    }
    
    public class RideStatus
    {
        public string BookingId { get; set; }
        public string Status { get; set; }
        
        public double DriverLatitude { get; set; }
        public double DriverLongitude { get; set; }
        public double DistanceKm { get; set; }
        public int ETAMinutes { get; set; }
        public int ETAToDestination { get; set; }
        
        public string VehicleDescription { get; set; }
        public string DriverPhone { get; set; }
        
        public decimal FinalFare { get; set; }
        public int DurationMinutes { get; set; }
        public double FinalDistanceKm { get; set; }
        
        public string PaymentStatus { get; set; }
        
        // Notification tracking
        public bool NotifiedEnRoute { get; set; }
        public bool NotifiedArrived { get; set; }
        public bool NotifiedStarted { get; set; }
        public bool NotifiedCompleted { get; set; }
    }
    
    public class RatingRequest
    {
        public string BookingId { get; set; }
        public int Stars { get; set; } // 1-5
        public string Review { get; set; }
        public long TelegramUserId { get; set; }
    }
}





