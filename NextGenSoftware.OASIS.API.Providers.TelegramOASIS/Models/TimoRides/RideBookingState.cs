using System;

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
        AddingReview
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
        
        // Timestamps
        public DateTime StartedAt { get; set; }
        public DateTime? LastUpdatedAt { get; set; }
    }
}





