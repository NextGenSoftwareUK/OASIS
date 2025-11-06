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
            _backendUrl = configuration["OASIS:StorageProviders:TelegramOASIS:TimoRides:BackendUrl"];
            _currency = configuration["OASIS:StorageProviders:TelegramOASIS:TimoRides:DefaultCurrency"] ?? "ZAR";
            
            if (string.IsNullOrEmpty(_backendUrl))
            {
                _logger.LogWarning("TimoRides backend URL not configured");
                _backendUrl = "http://localhost:4205/api";
            }
            
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



