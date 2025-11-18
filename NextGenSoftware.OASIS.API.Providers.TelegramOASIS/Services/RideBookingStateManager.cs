using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NextGenSoftware.OASIS.API.Providers.TelegramOASIS.Models.TimoRides;

namespace NextGenSoftware.OASIS.API.Providers.TelegramOASIS.Services
{
    public class RideBookingStateManager
    {
        private readonly ConcurrentDictionary<long, RideBookingData> _userStates;
        private const int MaxDriverAuditEntries = 25;
        private readonly ILogger<RideBookingStateManager> _logger;
        
        public RideBookingStateManager(ILogger<RideBookingStateManager> logger)
        {
            _userStates = new ConcurrentDictionary<long, RideBookingData>();
            _logger = logger;
        }
        
        /// <summary>
        /// Get user's current booking state
        /// </summary>
        public Task<RideBookingData> GetStateAsync(long telegramUserId)
        {
            if (_userStates.TryGetValue(telegramUserId, out var state))
            {
                return Task.FromResult(state);
            }
            
            // Create new state
            var newState = new RideBookingData
            {
                State = RideBookingState.Idle,
                StartedAt = DateTime.UtcNow
            };
            
            _userStates[telegramUserId] = newState;
            return Task.FromResult(newState);
        }
        
        /// <summary>
        /// Update user's booking state
        /// </summary>
        public Task SetStateAsync(long telegramUserId, RideBookingState state)
        {
            var data = _userStates.GetOrAdd(telegramUserId, new RideBookingData());
            data.State = state;
            data.LastUpdatedAt = DateTime.UtcNow;
            
            _logger.LogInformation($"User {telegramUserId} state updated to: {state}");
            
            return Task.CompletedTask;
        }
        
        /// <summary>
        /// Store pickup location
        /// </summary>
        public Task StorePickupLocationAsync(
            long telegramUserId,
            double latitude,
            double longitude,
            string address)
        {
            var data = _userStates.GetOrAdd(telegramUserId, new RideBookingData());
            data.PickupLocation = new LocationDto
            {
                Latitude = latitude,
                Longitude = longitude,
                Address = address
            };
            data.LastUpdatedAt = DateTime.UtcNow;
            
            _logger.LogInformation($"Pickup location stored for user {telegramUserId}: {address}");
            
            return Task.CompletedTask;
        }
        
        /// <summary>
        /// Store dropoff location
        /// </summary>
        public Task StoreDropoffLocationAsync(
            long telegramUserId,
            double latitude,
            double longitude,
            string address)
        {
            var data = _userStates.GetOrAdd(telegramUserId, new RideBookingData());
            data.DropoffLocation = new LocationDto
            {
                Latitude = latitude,
                Longitude = longitude,
                Address = address
            };
            data.LastUpdatedAt = DateTime.UtcNow;
            
            _logger.LogInformation($"Dropoff location stored for user {telegramUserId}: {address}");
            
            return Task.CompletedTask;
        }
        
        /// <summary>
        /// Store selected driver
        /// </summary>
        public Task StoreSelectedDriverAsync(long telegramUserId, DriverDto driver)
        {
            var data = _userStates.GetOrAdd(telegramUserId, new RideBookingData());
            data.SelectedDriverId = driver.Id;
            data.SelectedDriver = driver;
            data.EstimatedFare = driver.EstimatedFare;
            data.LastUpdatedAt = DateTime.UtcNow;
            
            _logger.LogInformation(
                $"Driver {driver.Name} selected by user {telegramUserId}");
            
            return Task.CompletedTask;
        }
        
        /// <summary>
        /// Store payment method
        /// </summary>
        public Task StorePaymentMethodAsync(long telegramUserId, string paymentMethod)
        {
            var data = _userStates.GetOrAdd(telegramUserId, new RideBookingData());
            data.PaymentMethod = paymentMethod;
            data.LastUpdatedAt = DateTime.UtcNow;
            
            _logger.LogInformation(
                $"Payment method {paymentMethod} selected by user {telegramUserId}");
            
            return Task.CompletedTask;
        }
        
        /// <summary>
        /// Store created booking
        /// </summary>
        public Task StoreBookingAsync(long telegramUserId, BookingDto booking)
        {
            var data = _userStates.GetOrAdd(telegramUserId, new RideBookingData());
            data.BookingId = booking.BookingId;
            data.CurrentBooking = booking;
            data.State = RideBookingState.TrackingRide;
            data.LastUpdatedAt = DateTime.UtcNow;
            
            _logger.LogInformation(
                $"Booking {booking.BookingId} stored for user {telegramUserId}");
            
            return Task.CompletedTask;
        }
        
        /// <summary>
        /// Store pending rating
        /// </summary>
        public Task StorePendingRatingAsync(long telegramUserId, int stars)
        {
            var data = _userStates.GetOrAdd(telegramUserId, new RideBookingData());
            data.PendingRatingStars = stars;
            data.State = RideBookingState.AddingReview;
            data.LastUpdatedAt = DateTime.UtcNow;
            
            return Task.CompletedTask;
        }
        
        /// <summary>
        /// Clear user's booking data
        /// </summary>
        public Task ClearStateAsync(long telegramUserId)
        {
            if (_userStates.TryRemove(telegramUserId, out _))
            {
                _logger.LogInformation($"Cleared booking state for user {telegramUserId}");
            }
            
            return Task.CompletedTask;
        }

        public Task RecordDriverSignalAsync(long telegramUserId, DriverSignalAuditEntry entry)
        {
            var data = _userStates.GetOrAdd(telegramUserId, new RideBookingData());
            data.DriverSignalAudit ??= new List<DriverSignalAuditEntry>();
            data.DriverSignalAudit.Insert(0, entry);

            if (data.DriverSignalAudit.Count > MaxDriverAuditEntries)
            {
                data.DriverSignalAudit.RemoveAt(data.DriverSignalAudit.Count - 1);
            }

            data.LastUpdatedAt = DateTime.UtcNow;

            _logger.LogInformation(
                $"Driver signal recorded for user {telegramUserId}: {entry.Action} / {entry.BookingId}");

            return Task.CompletedTask;
        }

        public Task SetPendingDriverLocationAsync(
            long telegramUserId,
            string driverId,
            string bookingId,
            TimeSpan? ttl = null)
        {
            var data = _userStates.GetOrAdd(telegramUserId, new RideBookingData());
            data.PendingDriverLocation = new DriverLocationContext
            {
                DriverId = driverId,
                BookingId = bookingId,
                RequestedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.Add(ttl ?? TimeSpan.FromMinutes(5))
            };
            data.State = RideBookingState.DriverAwaitingLocation;
            data.LastUpdatedAt = DateTime.UtcNow;

            _logger.LogInformation(
                $"Awaiting driver location for user {telegramUserId} booking {bookingId}");

            return Task.CompletedTask;
        }

        public Task ClearPendingDriverLocationAsync(long telegramUserId)
        {
            if (_userStates.TryGetValue(telegramUserId, out var data))
            {
                data.PendingDriverLocation = null;
                data.State = RideBookingState.Idle;
                data.LastUpdatedAt = DateTime.UtcNow;
            }

            return Task.CompletedTask;
        }
        
        /// <summary>
        /// Check if user has active booking
        /// </summary>
        public Task<bool> HasActiveBookingAsync(long telegramUserId)
        {
            if (_userStates.TryGetValue(telegramUserId, out var data))
            {
                var hasBooking = !string.IsNullOrEmpty(data.BookingId) && 
                                 data.State == RideBookingState.TrackingRide;
                return Task.FromResult(hasBooking);
            }
            
            return Task.FromResult(false);
        }
        
        /// <summary>
        /// Get active booking ID
        /// </summary>
        public Task<string> GetActiveBookingIdAsync(long telegramUserId)
        {
            if (_userStates.TryGetValue(telegramUserId, out var data))
            {
                return Task.FromResult(data.BookingId);
            }
            
            return Task.FromResult<string>(null);
        }
    }
}





