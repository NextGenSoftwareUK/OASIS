// TimoRides Telegram Bot - Command Handlers
// Add these methods to TelegramBotService.cs

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using NextGenSoftware.OASIS.API.Providers.TelegramOASIS.Models.TimoRides;
using NextGenSoftware.OASIS.API.Providers.TelegramOASIS.Services;

namespace NextGenSoftware.OASIS.API.Providers.TelegramOASIS
{
    public partial class TelegramBotService
    {
        // Add these as private fields to TelegramBotService class
        private readonly TimoRidesApiService _timoRidesService;
        private readonly RideBookingStateManager _stateManager;
        private readonly GoogleMapsService _mapsService;
        private readonly IConfiguration _configuration;
        
        // =========================================
        // MAIN COMMAND ROUTER
        // =========================================
        
        /// <summary>
        /// Route ride-related commands to appropriate handlers
        /// Call this from your existing HandleCommandAsync method
        /// </summary>
        private async Task HandleRideCommandAsync(Message message, IAvatar avatar)
        {
            var command = message.Text?.Split(' ')[0].ToLower();
            var userId = message.From.Id;
            
            _logger.LogInformation($"Handling ride command: {command} from user {userId}");
            
            try
            {
                switch (command)
                {
                    case "/bookride":
                        await StartRideBookingFlowAsync(message, avatar);
                        break;
                        
                    case "/quickbook":
                        await QuickBookRideAsync(message, avatar);
                        break;
                        
                    case "/myrides":
                        await ShowRideHistoryAsync(message, avatar);
                        break;
                        
                    case "/track":
                        await TrackActiveRideAsync(message, avatar);
                        break;
                        
                    case "/rate":
                        await HandleRateCommandAsync(message, avatar);
                        break;
                        
                    case "/cancel":
                        await CancelRideAsync(message, avatar);
                        break;
                        
                    case "/wallet":
                        await ShowWalletBalanceAsync(message, avatar);
                        break;
                        
                    case "/topup":
                        await InitiateWalletTopupAsync(message, avatar);
                        break;
                        
                    case "/favorites":
                        await ShowFavoriteDriversAsync(message, avatar);
                        break;
                        
                    case "/receipt":
                        await SendRideReceiptAsync(message, avatar);
                        break;
                        
                    default:
                        await _botClient.SendTextMessageAsync(
                            chatId: message.Chat.Id,
                            text: "❌ Unknown ride command. Type /help to see available commands.");
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error handling ride command: {command}");
                await _botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: "❌ Sorry, something went wrong. Please try again.");
            }
        }
        
        // =========================================
        // BOOKING FLOW - STEP 1: INITIATE
        // =========================================
        
        private async Task StartRideBookingFlowAsync(Message message, IAvatar avatar)
        {
            var userId = message.From.Id;
            
            // Check if user already has active booking
            if (await _stateManager.HasActiveBookingAsync(userId))
            {
                var bookingId = await _stateManager.GetActiveBookingIdAsync(userId);
                await _botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: $"⚠️ You already have an active ride!\n\n" +
                          $"🆔 Booking: {bookingId}\n\n" +
                          $"Track your ride: /track {bookingId}\n" +
                          $"Cancel ride: /cancel {bookingId}");
                return;
            }
            
            // Clear any old state
            await _stateManager.ClearStateAsync(userId);
            
            // Request pickup location
            var keyboard = new ReplyKeyboardMarkup(new[]
            {
                new KeyboardButton[]
                {
                    KeyboardButton.WithRequestLocation("📍 Share My Current Location")
                }
            })
            {
                ResizeKeyboard = true,
                OneTimeKeyboard = true
            };
            
            await _botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: "🚖 <b>Book a TimoRides Ride</b>\n\n" +
                      "📍 <b>Step 1: Share your pickup location</b>\n\n" +
                      "• Tap the button below to share your current location\n" +
                      "• Or type an address (e.g., 'uShaka Beach, Durban')",
                parseMode: ParseMode.Html,
                replyMarkup: keyboard);
            
            // Update state
            await _stateManager.SetStateAsync(userId, RideBookingState.WaitingPickupLocation);
        }
        
        // =========================================
        // LOCATION HANDLING
        // =========================================
        
        /// <summary>
        /// Handle location messages from users
        /// Call this from your existing message handler when message.Location != null
        /// </summary>
        private async Task HandleLocationMessageAsync(Message message, IAvatar avatar)
        {
            var location = message.Location;
            var userId = message.From.Id;
            var state = await _stateManager.GetStateAsync(userId);
            
            _logger.LogInformation(
                $"Received location from user {userId}: {location.Latitude}, {location.Longitude}");
            
            if (state.State == RideBookingState.WaitingPickupLocation)
            {
                await ProcessPickupLocationAsync(message, avatar, location);
            }
            else if (state.State == RideBookingState.WaitingDropoffLocation)
            {
                await ProcessDropoffLocationAsync(message, avatar, location);
            }
            else
            {
                await _botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: "❓ I'm not sure what to do with this location.\n" +
                          "Use /bookride to start a new ride booking.");
            }
        }
        
        private async Task ProcessPickupLocationAsync(
            Message message,
            IAvatar avatar,
            Location location)
        {
            var userId = message.From.Id;
            
            // Show processing message
            var processingMsg = await _botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: "🔍 Processing location...");
            
            try
            {
                // Reverse geocode to get address
                var address = await _mapsService.ReverseGeocodeAsync(
                    location.Latitude,
                    location.Longitude);
                
                // Store pickup location
                await _stateManager.StorePickupLocationAsync(
                    userId,
                    location.Latitude,
                    location.Longitude,
                    address);
                
                // Delete processing message
                await _botClient.DeleteMessageAsync(
                    chatId: message.Chat.Id,
                    messageId: processingMsg.MessageId);
                
                // Request dropoff location
                var keyboard = new ReplyKeyboardMarkup(new[]
                {
                    new KeyboardButton[]
                    {
                        KeyboardButton.WithRequestLocation("📍 Share Destination")
                    }
                })
                {
                    ResizeKeyboard = true,
                    OneTimeKeyboard = true
                };
                
                await _botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: $"✅ <b>Pickup location set:</b>\n{address}\n\n" +
                          $"📍 <b>Step 2: Share your destination</b>\n\n" +
                          "• Tap the button to share destination location\n" +
                          "• Or type a destination address",
                    parseMode: ParseMode.Html,
                    replyMarkup: keyboard);
                
                // Update state
                await _stateManager.SetStateAsync(userId, RideBookingState.WaitingDropoffLocation);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing pickup location");
                
                await _botClient.EditMessageTextAsync(
                    chatId: message.Chat.Id,
                    messageId: processingMsg.MessageId,
                    text: "❌ Failed to process location. Please try again.");
                
                await _stateManager.ClearStateAsync(userId);
            }
        }
        
        private async Task ProcessDropoffLocationAsync(
            Message message,
            IAvatar avatar,
            Location location)
        {
            var userId = message.From.Id;
            
            // Show processing message
            var processingMsg = await _botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: "🔍 Processing destination...",
                replyMarkup: new ReplyKeyboardRemove()); // Remove location keyboard
            
            try
            {
                // Reverse geocode
                var address = await _mapsService.ReverseGeocodeAsync(
                    location.Latitude,
                    location.Longitude);
                
                // Store dropoff location
                await _stateManager.StoreDropoffLocationAsync(
                    userId,
                    location.Latitude,
                    location.Longitude,
                    address);
                
                // Get booking data
                var bookingData = await _stateManager.GetStateAsync(userId);
                
                // Calculate distance and fare
                var distance = CalculateDistance(
                    bookingData.PickupLocation.Latitude,
                    bookingData.PickupLocation.Longitude,
                    bookingData.DropoffLocation.Latitude,
                    bookingData.DropoffLocation.Longitude);
                
                var estimatedTime = (int)(distance / 40 * 60); // Assuming 40 km/h avg speed
                
                // Delete processing message
                await _botClient.DeleteMessageAsync(
                    chatId: message.Chat.Id,
                    messageId: processingMsg.MessageId);
                
                // Show summary
                await _botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: $"✅ <b>Destination set:</b>\n{address}\n\n" +
                          $"📏 Distance: <b>{distance:F1} km</b>\n" +
                          $"⏱️ Estimated time: <b>{estimatedTime} minutes</b>\n\n" +
                          $"🔍 Searching for available drivers...",
                    parseMode: ParseMode.Html);
                
                // Fetch and show drivers
                await ShowAvailableDriversAsync(message, avatar);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing dropoff location");
                
                await _botClient.EditMessageTextAsync(
                    chatId: message.Chat.Id,
                    messageId: processingMsg.MessageId,
                    text: "❌ Failed to process destination. Please try again.");
                
                await _stateManager.ClearStateAsync(userId);
            }
        }
        
        // =========================================
        // DRIVER SELECTION
        // =========================================
        
        private async Task ShowAvailableDriversAsync(Message message, IAvatar avatar)
        {
            var userId = message.From.Id;
            var bookingData = await _stateManager.GetStateAsync(userId);
            var pickupLocation = bookingData.PickupLocation;
            
            try
            {
                // Fetch nearby drivers
                var drivers = await _timoRidesService.GetNearbyDriversAsync(
                    pickupLocation.Latitude,
                    pickupLocation.Longitude,
                    radiusKm: 10);
                
                if (!drivers.Any())
                {
                    await _botClient.SendTextMessageAsync(
                        chatId: message.Chat.Id,
                        text: "😕 <b>No drivers available</b>\n\n" +
                              "There are no drivers in your area right now.\n" +
                              "Please try again in a few minutes.\n\n" +
                              "/bookride - Try again",
                        parseMode: ParseMode.Html);
                    
                    await _stateManager.ClearStateAsync(userId);
                    return;
                }
                
                // Limit to top 5 drivers
                var maxDrivers = int.Parse(_configuration["TelegramOASIS:TimoRides:MaxDriversToShow"] ?? "5");
                var topDrivers = drivers.Take(maxDrivers).ToList();
                
                await _botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: $"🚗 <b>{topDrivers.Count} Drivers Available</b>\n\n" +
                          "Tap a driver to see details and book:",
                    parseMode: ParseMode.Html);
                
                // Send each driver as a card
                foreach (var driver in topDrivers)
                {
                    await SendDriverCardAsync(message.Chat.Id, driver, bookingData);
                }
                
                // Update state
                await _stateManager.SetStateAsync(userId, RideBookingState.SelectingDriver);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching available drivers");
                
                await _botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: "❌ Failed to fetch available drivers.\n" +
                          "Please check your internet connection and try again.\n\n" +
                          "/bookride - Try again");
                
                await _stateManager.ClearStateAsync(userId);
            }
        }
        
        private async Task SendDriverCardAsync(
            long chatId,
            DriverDto driver,
            RideBookingData bookingData)
        {
            var currency = _configuration["TelegramOASIS:TimoRides:DefaultCurrencySymbol"] ?? "R";
            
            // Build card text
            var cardText = $"👤 <b>{driver.GetFullName()}</b> {GetStarRating(driver.Rating)} " +
                          $"<i>({driver.TotalRides} rides)</i>\n" +
                          $"🚗 {driver.VehicleModel} • {driver.VehicleColor}\n" +
                          $"💵 <b>{currency} {driver.EstimatedFare:F0}</b>\n" +
                          $"🏆 Karma: <b>{driver.KarmaScore}</b> ({driver.GetKarmaBadge()})\n" +
                          $"🗣️ {string.Join(", ", driver.Languages ?? new List<string> { "English" })}\n";
            
            // Add amenities if available
            if (driver.Amenities?.Any() == true)
            {
                cardText += $"{GetAmenitiesString(driver.Amenities)}\n";
            }
            
            cardText += $"⏱️ <b>{driver.ETAMinutes} min away</b>";
            
            // Create inline keyboard
            var keyboard = new InlineKeyboardMarkup(new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData(
                        "🚖 Select Driver",
                        $"select_driver:{driver.Id}")
                },
                new[]
                {
                    InlineKeyboardButton.WithCallbackData(
                        "👤 View Profile",
                        $"driver_profile:{driver.Id}")
                }
            });
            
            // Send with photo if available
            if (!string.IsNullOrEmpty(driver.PhotoUrl))
            {
                try
                {
                    await _botClient.SendPhotoAsync(
                        chatId: chatId,
                        photo: new InputOnlineFile(driver.PhotoUrl),
                        caption: cardText,
                        parseMode: ParseMode.Html,
                        replyMarkup: keyboard);
                    return;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, $"Failed to send driver photo for {driver.Id}");
                    // Fall through to text-only message
                }
            }
            
            // Send text-only if no photo or photo failed
            await _botClient.SendTextMessageAsync(
                chatId: chatId,
                text: cardText,
                parseMode: ParseMode.Html,
                replyMarkup: keyboard);
        }
        
        // =========================================
        // CALLBACK QUERY HANDLERS
        // =========================================
        
        /// <summary>
        /// Handle inline button clicks
        /// Call this from your existing callback query handler
        /// </summary>
        private async Task HandleRideCallbackQueryAsync(CallbackQuery query, IAvatar avatar)
        {
            var data = query.Data;
            var userId = query.From.Id;
            
            _logger.LogInformation($"Handling ride callback: {data} from user {userId}");
            
            try
            {
                if (data.StartsWith("select_driver:"))
                {
                    await HandleDriverSelectionAsync(query, avatar);
                }
                else if (data.StartsWith("driver_profile:"))
                {
                    await ShowDriverProfileAsync(query, avatar);
                }
                else if (data.StartsWith("payment:"))
                {
                    await HandlePaymentSelectionAsync(query, avatar);
                }
                else if (data == "confirm_booking")
                {
                    await ConfirmBookingAsync(query, avatar);
                }
                else if (data == "cancel_booking")
                {
                    await CancelBookingFlowAsync(query, avatar);
                }
                else if (data.StartsWith("rate:"))
                {
                    await HandleRatingCallbackAsync(query, avatar);
                }
                else if (data.StartsWith("refresh_tracking:"))
                {
                    await RefreshTrackingAsync(query, avatar);
                }
                else if (data.StartsWith("call_driver:"))
                {
                    await ShowDriverContactAsync(query, avatar);
                }
                
                // Answer callback to remove loading state
                await _botClient.AnswerCallbackQueryAsync(
                    callbackQueryId: query.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error handling callback: {data}");
                
                await _botClient.AnswerCallbackQueryAsync(
                    callbackQueryId: query.Id,
                    text: "❌ Something went wrong. Please try again.");
            }
        }
        
        private async Task HandleDriverSelectionAsync(CallbackQuery query, IAvatar avatar)
        {
            var driverId = query.Data.Split(':')[1];
            var userId = query.From.Id;
            
            // Show loading
            await _botClient.AnswerCallbackQueryAsync(
                callbackQueryId: query.Id,
                text: "Loading driver...");
            
            // Fetch full driver details
            var driver = await _timoRidesService.GetDriverDetailsAsync(driverId);
            
            // Store selected driver
            await _stateManager.StoreSelectedDriverAsync(userId, driver);
            
            // Show payment options
            await ShowPaymentOptionsAsync(query.Message, avatar);
        }
        
        // =========================================
        // PAYMENT HANDLING
        // =========================================
        
        private async Task ShowPaymentOptionsAsync(Message message, IAvatar avatar)
        {
            var userId = message.Chat.From?.Id ?? message.From.Id;
            var bookingData = await _stateManager.GetStateAsync(userId);
            var driver = bookingData.SelectedDriver;
            var currency = _configuration["TelegramOASIS:TimoRides:DefaultCurrencySymbol"] ?? "R";
            
            // Check OASIS Wallet balance
            var walletBalance = await GetWalletBalanceAsync(avatar.Id);
            var hasEnoughBalance = walletBalance >= driver.EstimatedFare;
            
            var keyboardButtons = new List<InlineKeyboardButton[]>();
            
            // OASIS Wallet option
            keyboardButtons.Add(new[]
            {
                InlineKeyboardButton.WithCallbackData(
                    $"💳 OASIS Wallet ({currency} {walletBalance:F0}) {(hasEnoughBalance ? "✅" : "❌ Insufficient")}",
                    hasEnoughBalance ? "payment:wallet" : "payment:wallet_insufficient")
            });
            
            // Cash option
            keyboardButtons.Add(new[]
            {
                InlineKeyboardButton.WithCallbackData(
                    "💵 Cash on Delivery ✅",
                    "payment:cash")
            });
            
            // Mobile Money options
            keyboardButtons.Add(new[]
            {
                InlineKeyboardButton.WithCallbackData(
                    "📱 Mobile Money (M-Pesa)",
                    "payment:mpesa")
            });
            
            // Card option
            keyboardButtons.Add(new[]
            {
                InlineKeyboardButton.WithCallbackData(
                    "💳 Card (Flutterwave)",
                    "payment:card")
            });
            
            var keyboard = new InlineKeyboardMarkup(keyboardButtons);
            
            await _botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: $"💳 <b>Select Payment Method</b>\n\n" +
                      $"👤 Driver: {driver.GetFullName()}\n" +
                      $"💵 Fare: <b>{currency} {driver.EstimatedFare:F0}</b>\n\n" +
                      $"Choose how you'd like to pay:",
                parseMode: ParseMode.Html,
                replyMarkup: keyboard);
            
            await _stateManager.SetStateAsync(userId, RideBookingState.SelectingPayment);
        }
        
        // =========================================
        // HELPER METHODS
        // =========================================
        
        private string GetStarRating(double rating)
        {
            var fullStars = (int)Math.Floor(rating);
            var hasHalfStar = (rating - fullStars) >= 0.5;
            
            var stars = new string('⭐', fullStars);
            if (hasHalfStar) stars += "½";
            
            return $"{stars} {rating:F1}";
        }
        
        private string GetAmenitiesString(List<string> amenities)
        {
            var icons = new Dictionary<string, string>
            {
                { "ac", "❄️ AC" },
                { "wifi", "📱 WiFi" },
                { "music", "🎵 Music" },
                { "child_seat", "👶 Child Seat" },
                { "wheelchair", "♿ Wheelchair Accessible" },
                { "pet_friendly", "🐕 Pet Friendly" }
            };
            
            var result = amenities
                .Where(a => icons.ContainsKey(a.ToLower()))
                .Select(a => icons[a.ToLower()]);
            
            return string.Join(" • ", result);
        }
        
        private double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
        {
            // Haversine formula for distance calculation
            const double R = 6371; // Earth's radius in km
            
            var dLat = ToRadians(lat2 - lat1);
            var dLon = ToRadians(lon2 - lon1);
            
            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                   Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                   Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            
            return R * c;
        }
        
        private double ToRadians(double degrees)
        {
            return degrees * Math.PI / 180;
        }
        
        private async Task<decimal> GetWalletBalanceAsync(Guid avatarId)
        {
            // TODO: Integrate with actual OASIS Wallet service
            // For now, return mock balance
            return 500m;
        }
    }
}



