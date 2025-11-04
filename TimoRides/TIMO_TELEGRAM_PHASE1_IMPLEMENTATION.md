# TimoRides Telegram Bot - Phase 1 Implementation Guide

**Target:** Working ride booking MVP in 4-6 weeks  
**Date:** October 28, 2025

---

## Week-by-Week Breakdown

### Week 1: Foundation & Setup

#### Day 1-2: Project Setup
- [ ] Create feature branch: `feature/telegram-ride-booking`
- [ ] Add TimoRides configuration to `OASIS_DNA.json`
- [ ] Create new project structure in TelegramOASIS
- [ ] Set up HttpClient for TimoRides backend API
- [ ] Create data models (DTOs)

#### Day 3-4: Basic Command Routing
- [ ] Extend `TelegramBotService` with ride commands
- [ ] Add `/bookride` command handler
- [ ] Implement state management service
- [ ] Test basic command recognition

#### Day 5: Testing & Documentation
- [ ] Test command routing
- [ ] Document API endpoints needed from backend
- [ ] Set up local testing environment
- [ ] End of Week 1 Review

---

### Week 2: Booking Flow (Part 1)

#### Day 6-7: Location Handling
- [ ] Implement location message handler
- [ ] Add Google Maps reverse geocoding
- [ ] Store pickup location in state
- [ ] Request dropoff location

#### Day 8-9: Driver Search Integration
- [ ] Create `TimoRidesApiService`
- [ ] Implement `GetNearbyDrivers()` API call
- [ ] Parse and display driver list
- [ ] Test with mock drivers

#### Day 10: Testing
- [ ] End-to-end test: location → drivers
- [ ] Error handling for API failures
- [ ] Handle "no drivers available" case

---

### Week 3: Booking Flow (Part 2)

#### Day 11-12: Driver Selection
- [ ] Create inline keyboard with driver cards
- [ ] Handle callback queries for driver selection
- [ ] Display driver details with photos
- [ ] Store selected driver in state

#### Day 13-14: Payment & Confirmation
- [ ] Create payment method selection UI
- [ ] Check OASIS Wallet balance
- [ ] Display booking summary
- [ ] Implement confirmation callback

#### Day 15: Booking Creation
- [ ] Call TimoRides `POST /api/bookings` API
- [ ] Handle booking response
- [ ] Send confirmation message
- [ ] Store booking in database

---

### Week 4: Tracking & Completion

#### Day 16-17: Real-Time Tracking
- [ ] Implement `/track` command
- [ ] Create background polling service
- [ ] Send status update messages
- [ ] Display driver location

#### Day 18-19: Rating System
- [ ] Implement `/rate` command
- [ ] Create rating inline keyboard
- [ ] Submit rating to backend
- [ ] Award karma + tokens

#### Day 20: Polish & Testing
- [ ] Add `/myrides` command (ride history)
- [ ] Error handling improvements
- [ ] User experience refinements
- [ ] End-to-end testing

---

### Week 5: Integration & Testing

#### Day 21-23: Backend Integration
- [ ] Work with backend team to ensure API compatibility
- [ ] Test with real TimoRides backend
- [ ] Fix integration issues
- [ ] Add logging and monitoring

#### Day 24-25: Internal Testing
- [ ] Test with team members
- [ ] Create test scenarios document
- [ ] Bug fixes
- [ ] Performance optimization

---

### Week 6: Beta & Launch Prep

#### Day 26-28: Beta Testing
- [ ] Recruit 10-20 beta testers
- [ ] Deploy to staging environment
- [ ] Monitor usage and gather feedback
- [ ] Iterate on feedback

#### Day 29-30: Launch Preparation
- [ ] Final bug fixes
- [ ] Create user documentation
- [ ] Set up production monitoring
- [ ] Deploy to production
- [ ] Announce launch! 🚀

---

## Detailed Implementation Steps

### Step 1: Update OASIS_DNA.json

**Location:** `/NextGenSoftware.OASIS.API.ONODE.WebAPI/OASIS_DNA.json`

```json
{
  "TelegramOASIS": {
    "BotToken": "YOUR_BOT_TOKEN",
    "WebhookUrl": "https://api.oasisweb4.one/api/telegram/webhook",
    "MongoConnectionString": "mongodb+srv://...",
    "TreasuryWalletAddress": "YOUR_SOLANA_WALLET",
    "RewardTokenMintAddress": "TOKEN_MINT_ADDRESS",
    "RewardTokenSymbol": "REWARD",
    "SolanaCluster": "devnet",
    
    "TimoRides": {
      "Enabled": true,
      "BackendUrl": "http://localhost:3000/api",
      "GoogleMapsApiKey": "YOUR_GOOGLE_MAPS_KEY",
      "DefaultCurrency": "ZAR",
      "DefaultCurrencySymbol": "R",
      "DefaultRadius": 10,
      "KarmaPerRide": 20,
      "TokensPerRide": 2.0,
      "RideTrackingIntervalSeconds": 10,
      "MaxDriversToShow": 5,
      "BookingTimeoutMinutes": 15,
      
      "Achievements": {
        "BronzeRider": {
          "RequiredRides": 10,
          "KarmaReward": 50,
          "TokenReward": 10
        },
        "SilverRider": {
          "RequiredRides": 50,
          "KarmaReward": 200,
          "TokenReward": 50
        },
        "GoldRider": {
          "RequiredRides": 100,
          "KarmaReward": 500,
          "TokenReward": 100,
          "MintNFT": true
        }
      }
    }
  }
}
```

---

### Step 2: Create Data Models

**New file:** `/NextGenSoftware.OASIS.API.Providers.TelegramOASIS/Models/TimoRides/`

#### `DriverDto.cs`
```csharp
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
        public List<string> Languages { get; set; }
        public List<string> Amenities { get; set; } // ac, wifi, music, child_seat
        
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
        public List<DriverDto> Drivers { get; set; }
        public int TotalCount { get; set; }
    }
}
```

#### `BookingDto.cs`
```csharp
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
```

#### `RideBookingState.cs`
```csharp
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
```

---

### Step 3: Create TimoRides API Service

**New file:** `/NextGenSoftware.OASIS.API.Providers.TelegramOASIS/Services/TimoRidesApiService.cs`

```csharp
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NextGenSoftware.OASIS.API.Providers.TelegramOASIS.Models.TimoRides;

namespace NextGenSoftware.OASIS.API.Providers.TelegramOASIS.Services
{
    public class TimoRidesApiService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<TimoRidesApiService> _logger;
        private readonly string _backendUrl;
        private readonly string _currency;
        
        public TimoRidesApiService(
            IConfiguration configuration,
            ILogger<TimoRidesApiService> logger)
        {
            _logger = logger;
            _backendUrl = configuration["TelegramOASIS:TimoRides:BackendUrl"];
            _currency = configuration["TelegramOASIS:TimoRides:DefaultCurrency"] ?? "ZAR";
            
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(_backendUrl),
                Timeout = TimeSpan.FromSeconds(30)
            };
            
            _logger.LogInformation($"TimoRidesApiService initialized with backend: {_backendUrl}");
        }
        
        /// <summary>
        /// Get nearby available drivers
        /// </summary>
        public async Task<List<DriverDto>> GetNearbyDriversAsync(
            double latitude,
            double longitude,
            int radiusKm = 10)
        {
            try
            {
                _logger.LogInformation(
                    $"Fetching nearby drivers: lat={latitude}, lng={longitude}, radius={radiusKm}km");
                
                var response = await _httpClient.GetAsync(
                    $"/drivers/nearby?lat={latitude}&lng={longitude}&radius={radiusKm}");
                
                response.EnsureSuccessStatusCode();
                
                var result = await response.Content.ReadFromJsonAsync<DriversResponse>();
                
                if (result?.Success == true && result.Drivers != null)
                {
                    _logger.LogInformation($"Found {result.Drivers.Count} nearby drivers");
                    return result.Drivers;
                }
                
                _logger.LogWarning("No drivers found or invalid response");
                return new List<DriverDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching nearby drivers");
                throw new Exception($"Failed to fetch nearby drivers: {ex.Message}", ex);
            }
        }
        
        /// <summary>
        /// Get specific driver details
        /// </summary>
        public async Task<DriverDto> GetDriverDetailsAsync(string driverId)
        {
            try
            {
                _logger.LogInformation($"Fetching driver details: {driverId}");
                
                var response = await _httpClient.GetAsync($"/drivers/{driverId}");
                response.EnsureSuccessStatusCode();
                
                var driver = await response.Content.ReadFromJsonAsync<DriverDto>();
                
                _logger.LogInformation($"Retrieved details for driver: {driver?.Name}");
                return driver;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error fetching driver details for {driverId}");
                throw new Exception($"Failed to fetch driver details: {ex.Message}", ex);
            }
        }
        
        /// <summary>
        /// Create a new ride booking
        /// </summary>
        public async Task<BookingDto> CreateBookingAsync(BookingRequest request)
        {
            try
            {
                _logger.LogInformation(
                    $"Creating booking: User {request.UserId}, Driver {request.DriverId}");
                
                request.Currency = _currency;
                
                var response = await _httpClient.PostAsJsonAsync("/bookings", request);
                response.EnsureSuccessStatusCode();
                
                var result = await response.Content.ReadFromJsonAsync<BookingResponse>();
                
                if (result?.Success == true && result.Booking != null)
                {
                    _logger.LogInformation(
                        $"Booking created successfully: {result.Booking.BookingId}");
                    return result.Booking;
                }
                
                throw new Exception(result?.Message ?? "Failed to create booking");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating booking");
                throw new Exception($"Failed to create booking: {ex.Message}", ex);
            }
        }
        
        /// <summary>
        /// Get booking details
        /// </summary>
        public async Task<BookingDto> GetBookingDetailsAsync(string bookingId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/bookings/{bookingId}");
                response.EnsureSuccessStatusCode();
                
                var booking = await response.Content.ReadFromJsonAsync<BookingDto>();
                return booking;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error fetching booking details for {bookingId}");
                throw;
            }
        }
        
        /// <summary>
        /// Get real-time ride status
        /// </summary>
        public async Task<RideStatus> GetRideStatusAsync(string bookingId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/bookings/{bookingId}/status");
                response.EnsureSuccessStatusCode();
                
                var status = await response.Content.ReadFromJsonAsync<RideStatus>();
                return status;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error fetching ride status for {bookingId}");
                throw;
            }
        }
        
        /// <summary>
        /// Submit rating for completed ride
        /// </summary>
        public async Task SubmitRatingAsync(string bookingId, int stars, string review)
        {
            try
            {
                _logger.LogInformation($"Submitting rating for booking {bookingId}: {stars} stars");
                
                var request = new RatingRequest
                {
                    BookingId = bookingId,
                    Stars = stars,
                    Review = review
                };
                
                var response = await _httpClient.PostAsJsonAsync(
                    $"/bookings/{bookingId}/rate", 
                    request);
                
                response.EnsureSuccessStatusCode();
                
                _logger.LogInformation($"Rating submitted successfully for {bookingId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error submitting rating for {bookingId}");
                throw;
            }
        }
        
        /// <summary>
        /// Cancel booking
        /// </summary>
        public async Task<bool> CancelBookingAsync(string bookingId, string reason)
        {
            try
            {
                _logger.LogInformation($"Cancelling booking {bookingId}: {reason}");
                
                var response = await _httpClient.PutAsJsonAsync(
                    $"/bookings/{bookingId}/cancel",
                    new { Reason = reason });
                
                response.EnsureSuccessStatusCode();
                
                _logger.LogInformation($"Booking {bookingId} cancelled successfully");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error cancelling booking {bookingId}");
                return false;
            }
        }
        
        /// <summary>
        /// Get user's ride history
        /// </summary>
        public async Task<List<BookingDto>> GetRideHistoryAsync(string userId, int limit = 10)
        {
            try
            {
                var response = await _httpClient.GetAsync(
                    $"/users/{userId}/bookings?limit={limit}");
                
                response.EnsureSuccessStatusCode();
                
                var bookings = await response.Content.ReadFromJsonAsync<List<BookingDto>>();
                return bookings ?? new List<BookingDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error fetching ride history for user {userId}");
                return new List<BookingDto>();
            }
        }
        
        /// <summary>
        /// Calculate fare estimate
        /// </summary>
        public async Task<decimal> CalculateFareAsync(
            double pickupLat,
            double pickupLng,
            double dropoffLat,
            double dropoffLng,
            string vehicleType = "sedan")
        {
            try
            {
                var response = await _httpClient.GetAsync(
                    $"/fare/estimate?pickup_lat={pickupLat}&pickup_lng={pickupLng}" +
                    $"&dropoff_lat={dropoffLat}&dropoff_lng={dropoffLng}&vehicle_type={vehicleType}");
                
                response.EnsureSuccessStatusCode();
                
                var result = await response.Content.ReadFromJsonAsync<FareEstimate>();
                return result?.Fare ?? 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating fare estimate");
                return 0;
            }
        }
    }
    
    public class FareEstimate
    {
        public decimal Fare { get; set; }
        public string Currency { get; set; }
        public double DistanceKm { get; set; }
        public int EstimatedMinutes { get; set; }
    }
}
```

---

### Step 4: Create State Management Service

**New file:** `/NextGenSoftware.OASIS.API.Providers.TelegramOASIS/Services/RideBookingStateManager.cs`

```csharp
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NextGenSoftware.OASIS.API.Providers.TelegramOASIS.Models.TimoRides;

namespace NextGenSoftware.OASIS.API.Providers.TelegramOASIS.Services
{
    public class RideBookingStateManager
    {
        private readonly ConcurrentDictionary<long, RideBookingData> _userStates;
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
```

---

## Next Steps

Now that you have the foundation, here's what to implement next:

### 1. **Extend TelegramBotService**
Add ride command handlers to the existing `TelegramBotService.cs`

### 2. **Add Google Maps Service**
For reverse geocoding (lat/lng → address)

### 3. **Create Message Formatters**
Helper methods to format driver cards, booking summaries, etc.

### 4. **Implement Callback Handlers**
Handle inline button clicks for driver selection, payment, etc.

### 5. **Add Background Tracking Service**
For real-time ride updates

Would you like me to continue with any of these specific components?



