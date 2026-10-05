using System;
using System.Collections.Generic;

namespace NextGenSoftware.OASIS.API.Providers.TelegramOASIS.Models.TimoRides
{
    public enum RideBookingState
    {
        Idle,
        WaitingPickupLocation,
        WaitingDropoffLocation,
        SelectingDriver,
        ViewingDriverProfile,
        SelectingPayment,
        ConfirmingBooking,
        TrackingRide,
        RatingRide,
        AddingReview,
        DriverAwaitingLocation
    }
    
    public class RideBookingData
    {
        public RideBookingState State { get; set; }
        
        // Location data
        public LocationDto PickupLocation { get; set; }
        public LocationDto DropoffLocation { get; set; }
        
        // Selected driver
        public string SelectedDriverId { get; set; }
        public DriverDto SelectedDriver { get; set; }
        
        // Payment
        public string PaymentMethod { get; set; }
        public decimal EstimatedFare { get; set; }
        
        // Active booking
        public string BookingId { get; set; }
        public BookingDto CurrentBooking { get; set; }
        
        // Rating
        public int? PendingRatingStars { get; set; }

        // Driver signal audit trail
        public List<DriverSignalAuditEntry> DriverSignalAudit { get; set; } = new();

        // Pending driver location requests
        public DriverLocationContext PendingDriverLocation { get; set; }
        
        // Timestamps
        public DateTime StartedAt { get; set; }
        public DateTime? LastUpdatedAt { get; set; }
    }

    public class DriverSignalAuditEntry
    {
        public DateTime Timestamp { get; set; }
        public string Action { get; set; }
        public string BookingId { get; set; }
        public string PayloadJson { get; set; }
        public string TraceId { get; set; }
    }

    public class DriverLocationContext
    {
        public string DriverId { get; set; }
        public string BookingId { get; set; }
        public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
        public DateTime ExpiresAt { get; set; } = DateTime.UtcNow.AddMinutes(5);
    }
}





