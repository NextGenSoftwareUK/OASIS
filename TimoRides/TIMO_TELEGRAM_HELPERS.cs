// TimoRides Telegram Bot - Helper Services
// Additional services needed for the integration

using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NextGenSoftware.OASIS.API.Providers.TelegramOASIS.Models.TimoRides;

namespace NextGenSoftware.OASIS.API.Providers.TelegramOASIS.Services
{
    // =========================================
    // GOOGLE MAPS SERVICE
    // =========================================
    
    public class GoogleMapsService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly ILogger<GoogleMapsService> _logger;
        
        public GoogleMapsService(
            IConfiguration configuration,
            ILogger<GoogleMapsService> logger)
        {
            _apiKey = configuration["TelegramOASIS:TimoRides:GoogleMapsApiKey"];
            _logger = logger;
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri("https://maps.googleapis.com/maps/api/")
            };
        }
        
        /// <summary>
        /// Convert lat/lng to human-readable address
        /// </summary>
        public async Task<string> ReverseGeocodeAsync(double latitude, double longitude)
        {
            try
            {
                _logger.LogInformation($"Reverse geocoding: {latitude}, {longitude}");
                
                var url = $"geocode/json?latlng={latitude},{longitude}&key={_apiKey}";
                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();
                
                var result = await response.Content.ReadFromJsonAsync<GeocodeResponse>();
                
                if (result?.Status == "OK" && result.Results?.Count > 0)
                {
                    var address = result.Results[0].FormattedAddress;
                    _logger.LogInformation($"Reverse geocoded to: {address}");
                    return address;
                }
                
                _logger.LogWarning($"Reverse geocoding failed: {result?.Status}");
                return $"{latitude:F4}, {longitude:F4}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reverse geocoding");
                return $"{latitude:F4}, {longitude:F4}";
            }
        }
        
        /// <summary>
        /// Convert address to lat/lng
        /// </summary>
        public async Task<(double Latitude, double Longitude)?> GeocodeAsync(string address)
        {
            try
            {
                _logger.LogInformation($"Geocoding address: {address}");
                
                var url = $"geocode/json?address={Uri.EscapeDataString(address)}&key={_apiKey}";
                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();
                
                var result = await response.Content.ReadFromJsonAsync<GeocodeResponse>();
                
                if (result?.Status == "OK" && result.Results?.Count > 0)
                {
                    var location = result.Results[0].Geometry.Location;
                    _logger.LogInformation(
                        $"Geocoded to: {location.Lat}, {location.Lng}");
                    return (location.Lat, location.Lng);
                }
                
                _logger.LogWarning($"Geocoding failed: {result?.Status}");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error geocoding address");
                return null;
            }
        }
        
        /// <summary>
        /// Get distance and duration between two points
        /// </summary>
        public async Task<(double DistanceKm, int DurationMinutes)> GetDistanceMatrixAsync(
            double originLat,
            double originLng,
            double destLat,
            double destLng)
        {
            try
            {
                var url = $"distancematrix/json?origins={originLat},{originLng}" +
                         $"&destinations={destLat},{destLng}&key={_apiKey}";
                
                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();
                
                var result = await response.Content.ReadFromJsonAsync<DistanceMatrixResponse>();
                
                if (result?.Status == "OK" && 
                    result.Rows?.Count > 0 && 
                    result.Rows[0].Elements?.Count > 0)
                {
                    var element = result.Rows[0].Elements[0];
                    if (element.Status == "OK")
                    {
                        var distanceKm = element.Distance.Value / 1000.0;
                        var durationMin = (int)Math.Ceiling(element.Duration.Value / 60.0);
                        
                        return (distanceKm, durationMin);
                    }
                }
                
                // Fallback to simple calculation
                var distance = CalculateHaversineDistance(originLat, originLng, destLat, destLng);
                var duration = (int)(distance / 40 * 60); // Assume 40 km/h
                
                return (distance, duration);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting distance matrix");
                
                // Fallback
                var distance = CalculateHaversineDistance(originLat, originLng, destLat, destLng);
                var duration = (int)(distance / 40 * 60);
                
                return (distance, duration);
            }
        }
        
        private double CalculateHaversineDistance(
            double lat1, double lon1, double lat2, double lon2)
        {
            const double R = 6371; // Earth radius in km
            
            var dLat = ToRadians(lat2 - lat1);
            var dLon = ToRadians(lon2 - lon1);
            
            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                   Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                   Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            
            return R * c;
        }
        
        private double ToRadians(double degrees) => degrees * Math.PI / 180;
    }
    
    // Google Maps API DTOs
    public class GeocodeResponse
    {
        public string Status { get; set; }
        public List<GeocodeResult> Results { get; set; }
    }
    
    public class GeocodeResult
    {
        public string FormattedAddress { get; set; }
        public Geometry Geometry { get; set; }
    }
    
    public class Geometry
    {
        public LatLng Location { get; set; }
    }
    
    public class LatLng
    {
        public double Lat { get; set; }
        public double Lng { get; set; }
    }
    
    public class DistanceMatrixResponse
    {
        public string Status { get; set; }
        public List<DistanceMatrixRow> Rows { get; set; }
    }
    
    public class DistanceMatrixRow
    {
        public List<DistanceMatrixElement> Elements { get; set; }
    }
    
    public class DistanceMatrixElement
    {
        public string Status { get; set; }
        public DistanceValue Distance { get; set; }
        public DurationValue Duration { get; set; }
    }
    
    public class DistanceValue
    {
        public int Value { get; set; } // in meters
        public string Text { get; set; }
    }
    
    public class DurationValue
    {
        public int Value { get; set; } // in seconds
        public string Text { get; set; }
    }
    
    // =========================================
    // BOOKING CONFIRMATION SERVICE
    // =========================================
    
    public static class BookingConfirmationService
    {
        /// <summary>
        /// Process payment selection and show booking summary
        /// </summary>
        public static async Task HandlePaymentSelectionAsync(
            CallbackQuery query,
            IAvatar avatar,
            ITelegramBotClient botClient,
            RideBookingStateManager stateManager,
            ILogger logger)
        {
            var paymentMethod = query.Data.Split(':')[1];
            var userId = query.From.Id;
            
            if (paymentMethod == "wallet_insufficient")
            {
                await botClient.AnswerCallbackQueryAsync(
                    callbackQueryId: query.Id,
                    text: "❌ Insufficient funds in wallet",
                    showAlert: true);
                return;
            }
            
            var bookingData = await stateManager.GetStateAsync(userId);
            
            // Store payment method
            await stateManager.StorePaymentMethodAsync(userId, paymentMethod);
            
            // Build booking summary
            var currency = "R";
            var driver = bookingData.SelectedDriver;
            
            var summaryText = $"📋 <b>Booking Summary</b>\n\n" +
                             $"👤 <b>Driver:</b> {driver.GetFullName()} {GetStarRating(driver.Rating)}\n" +
                             $"🚗 <b>Vehicle:</b> {driver.VehicleModel} ({driver.VehicleColor})\n\n" +
                             $"📍 <b>Pickup:</b>\n{bookingData.PickupLocation.Address}\n\n" +
                             $"📍 <b>Dropoff:</b>\n{bookingData.DropoffLocation.Address}\n\n" +
                             $"💵 <b>Fare:</b> {currency} {driver.EstimatedFare:F0}\n" +
                             $"💳 <b>Payment:</b> {GetPaymentMethodLabel(paymentMethod)}\n" +
                             $"⏱️ <b>ETA:</b> {driver.ETAMinutes} minutes\n\n" +
                             $"<i>Confirm to book this ride?</i>";
            
            var keyboard = new InlineKeyboardMarkup(new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("✅ Confirm Booking", "confirm_booking"),
                    InlineKeyboardButton.WithCallbackData("❌ Cancel", "cancel_booking")
                }
            });
            
            await botClient.SendTextMessageAsync(
                chatId: query.Message.Chat.Id,
                text: summaryText,
                parseMode: ParseMode.Html,
                replyMarkup: keyboard);
            
            await stateManager.SetStateAsync(userId, RideBookingState.ConfirmingBooking);
            
            await botClient.AnswerCallbackQueryAsync(
                callbackQueryId: query.Id,
                text: "✅ Payment method selected");
        }
        
        /// <summary>
        /// Finalize booking and create in backend
        /// </summary>
        public static async Task ConfirmBookingAsync(
            CallbackQuery query,
            IAvatar avatar,
            ITelegramBotClient botClient,
            RideBookingStateManager stateManager,
            TimoRidesApiService timoRidesService,
            IConfiguration configuration,
            ILogger logger)
        {
            var userId = query.From.Id;
            var bookingData = await stateManager.GetStateAsync(userId);
            
            // Show processing
            await botClient.AnswerCallbackQueryAsync(
                callbackQueryId: query.Id,
                text: "Creating booking...");
            
            var processingMsg = await botClient.SendTextMessageAsync(
                chatId: query.Message.Chat.Id,
                text: "🔄 Creating your booking...");
            
            try
            {
                // Deduct from wallet if wallet payment
                if (bookingData.PaymentMethod == "wallet")
                {
                    // TODO: Implement OASIS Wallet deduction
                    logger.LogInformation(
                        $"Would deduct {bookingData.EstimatedFare} from user {avatar.Id} wallet");
                }
                
                // Create booking request
                var request = new BookingRequest
                {
                    UserId = avatar.Id.ToString(),
                    UserTelegramId = userId,
                    DriverId = bookingData.SelectedDriverId,
                    PickupLatitude = bookingData.PickupLocation.Latitude,
                    PickupLongitude = bookingData.PickupLocation.Longitude,
                    PickupAddress = bookingData.PickupLocation.Address,
                    DropoffLatitude = bookingData.DropoffLocation.Latitude,
                    DropoffLongitude = bookingData.DropoffLocation.Longitude,
                    DropoffAddress = bookingData.DropoffLocation.Address,
                    PaymentMethod = bookingData.PaymentMethod,
                    EstimatedFare = bookingData.EstimatedFare,
                    Currency = configuration["TelegramOASIS:TimoRides:DefaultCurrency"] ?? "ZAR",
                    VehicleType = bookingData.SelectedDriver.VehicleType ?? "sedan"
                };
                
                // Create booking via API
                var booking = await timoRidesService.CreateBookingAsync(request);
                
                // Store booking
                await stateManager.StoreBookingAsync(userId, booking);
                
                // Delete processing message
                await botClient.DeleteMessageAsync(
                    chatId: query.Message.Chat.Id,
                    messageId: processingMsg.MessageId);
                
                // Send confirmation
                var confirmationText = $"✅ <b>Booking Confirmed!</b> 🎉\n\n" +
                                      $"🆔 <b>Booking ID:</b> <code>{booking.BookingId}</code>\n\n" +
                                      $"👤 <b>Driver:</b> {booking.DriverName}\n" +
                                      $"🚗 <b>Vehicle:</b> {booking.VehicleDetails}\n" +
                                      $"📱 <b>Driver Phone:</b> <code>{booking.DriverPhone}</code>\n\n" +
                                      $"📍 <b>Driver is {booking.DriverETAMinutes} minutes away</b>\n\n" +
                                      $"<b>Track your ride:</b>\n" +
                                      $"/track {booking.BookingId}\n\n" +
                                      $"<i>You'll receive updates as your driver approaches.</i>";
                
                await botClient.SendTextMessageAsync(
                    chatId: query.Message.Chat.Id,
                    text: confirmationText,
                    parseMode: ParseMode.Html);
                
                // Start background tracking
                _ = Task.Run(async () =>
                {
                    await RideTrackingService.StartTrackingAsync(
                        userId,
                        booking.BookingId,
                        botClient,
                        timoRidesService,
                        logger);
                });
                
                logger.LogInformation(
                    $"Booking {booking.BookingId} created for user {userId}");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error confirming booking");
                
                await botClient.EditMessageTextAsync(
                    chatId: query.Message.Chat.Id,
                    messageId: processingMsg.MessageId,
                    text: "❌ Failed to create booking. Please try again.\n\n" +
                          "/bookride - Try again");
                
                await stateManager.ClearStateAsync(userId);
            }
        }
        
        private static string GetStarRating(double rating)
        {
            return $"⭐ {rating:F1}";
        }
        
        private static string GetPaymentMethodLabel(string method)
        {
            return method switch
            {
                "wallet" => "OASIS Wallet",
                "cash" => "Cash on Delivery",
                "mpesa" => "M-Pesa",
                "card" => "Card (Flutterwave)",
                _ => method
            };
        }
    }
    
    // =========================================
    // RIDE TRACKING SERVICE
    // =========================================
    
    public static class RideTrackingService
    {
        /// <summary>
        /// Background service that polls for ride updates
        /// </summary>
        public static async Task StartTrackingAsync(
            long userId,
            string bookingId,
            ITelegramBotClient botClient,
            TimoRidesApiService timoRidesService,
            ILogger logger)
        {
            logger.LogInformation($"Starting ride tracking for booking {bookingId}");
            
            var notificationsSent = new HashSet<string>();
            
            while (true)
            {
                try
                {
                    await Task.Delay(10000); // Poll every 10 seconds
                    
                    var status = await timoRidesService.GetRideStatusAsync(bookingId);
                    
                    // Send notifications based on status
                    if (status.Status == "driver_en_route_pickup" && 
                        !notificationsSent.Contains("en_route"))
                    {
                        await botClient.SendTextMessageAsync(
                            chatId: userId,
                            text: $"🚗 <b>Driver is on the way!</b>\n\n" +
                                  $"⏱️ ETA: <b>{status.ETAMinutes} minutes</b>\n" +
                                  $"📍 Distance: {status.DistanceKm:F1} km away\n\n" +
                                  $"/track {bookingId}",
                            parseMode: ParseMode.Html);
                        
                        notificationsSent.Add("en_route");
                    }
                    else if (status.Status == "driver_arrived" && 
                             !notificationsSent.Contains("arrived"))
                    {
                        await botClient.SendTextMessageAsync(
                            chatId: userId,
                            text: $"🎉 <b>Driver has arrived!</b>\n\n" +
                                  $"Please proceed to the pickup point.\n" +
                                  $"Your driver is waiting in a {status.VehicleDescription}.\n\n" +
                                  $"📱 Call driver: <code>{status.DriverPhone}</code>",
                            parseMode: ParseMode.Html);
                        
                        notificationsSent.Add("arrived");
                    }
                    else if (status.Status == "ride_started" && 
                             !notificationsSent.Contains("started"))
                    {
                        await botClient.SendTextMessageAsync(
                            chatId: userId,
                            text: $"🚗 <b>Ride Started!</b>\n\n" +
                                  $"📍 En route to destination\n" +
                                  $"⏱️ Estimated arrival: {status.ETAToDestination} minutes\n\n" +
                                  $"Have a safe journey! 🛣️",
                            parseMode: ParseMode.Html);
                        
                        notificationsSent.Add("started");
                    }
                    else if (status.Status == "completed")
                    {
                        await botClient.SendTextMessageAsync(
                            chatId: userId,
                            text: $"✅ <b>Ride Completed!</b>\n\n" +
                                  $"📋 <b>Trip Summary:</b>\n" +
                                  $"• Distance: {status.FinalDistanceKm:F1} km\n" +
                                  $"• Duration: {status.DurationMinutes} minutes\n" +
                                  $"• Fare: R {status.FinalFare:F0}\n\n" +
                                  $"💳 Payment: {status.PaymentStatus}\n\n" +
                                  $"⭐ <b>Rate your ride:</b>\n" +
                                  $"/rate {bookingId}",
                            parseMode: ParseMode.Html);
                        
                        logger.LogInformation($"Ride {bookingId} completed. Stopping tracking.");
                        break; // Exit tracking loop
                    }
                    else if (status.Status == "cancelled")
                    {
                        await botClient.SendTextMessageAsync(
                            chatId: userId,
                            text: $"❌ <b>Ride Cancelled</b>\n\n" +
                                  $"Your ride has been cancelled.\n" +
                                  $"Book a new ride: /bookride",
                            parseMode: ParseMode.Html);
                        
                        logger.LogInformation($"Ride {bookingId} cancelled. Stopping tracking.");
                        break;
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, $"Error tracking ride {bookingId}");
                    // Continue tracking despite errors
                    await Task.Delay(10000);
                }
            }
        }
    }
    
    // =========================================
    // MESSAGE FORMATTERS
    // =========================================
    
    public static class MessageFormatters
    {
        public static string FormatRideHistory(List<BookingDto> bookings, string currency = "R")
        {
            if (!bookings.Any())
            {
                return "📋 <b>No ride history</b>\n\n" +
                       "You haven't taken any rides yet.\n\n" +
                       "/bookride - Book your first ride!";
            }
            
            var message = $"📋 <b>Your Recent Rides</b>\n\n";
            
            foreach (var booking in bookings.Take(10))
            {
                var statusEmoji = booking.Status switch
                {
                    "completed" => "✅",
                    "cancelled" => "❌",
                    "active" => "🚗",
                    _ => "⏳"
                };
                
                message += $"{statusEmoji} <b>{booking.BookingId}</b>\n" +
                          $"📅 {booking.CreatedAt:MMM dd, yyyy}\n" +
                          $"👤 {booking.DriverName}\n" +
                          $"💵 {currency} {booking.FinalFare:F0}\n\n";
            }
            
            return message;
        }
        
        public static string FormatWalletBalance(decimal balance, string currency = "R")
        {
            return $"💳 <b>OASIS Wallet</b>\n\n" +
                   $"💰 <b>Balance:</b> {currency} {balance:F2}\n\n" +
                   $"<b>Top up options:</b>\n" +
                   $"/topup 100\n" +
                   $"/topup 500\n" +
                   $"/topup 1000\n\n" +
                   $"<i>Or specify a custom amount</i>";
        }
    }
}




