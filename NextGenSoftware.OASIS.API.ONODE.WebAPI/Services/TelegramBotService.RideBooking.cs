using System;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using NextGenSoftware.OASIS.API.Providers.TelegramOASIS.Services;
using NextGenSoftware.OASIS.API.Providers.TelegramOASIS.Models.TimoRides;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services
{
    /// <summary>
    /// Partial class containing TimoRides booking functionality
    /// TO INTEGRATE: Add these methods to the main TelegramBotService class
    /// </summary>
    public partial class TelegramBotServiceRideBookingExtension
    {
        // NOTE: These fields should be added to the main TelegramBotService class
        // private readonly TimoRidesApiService _timoRidesService;
        // private readonly RideBookingStateManager _rideStateManager;
        // private readonly GoogleMapsService _mapsService;
        
        /// <summary>
        /// Handle ride-related commands
        /// ADD TO: HandleCommandAsync switch statement
        /// </summary>
        private async Task HandleRideCommandAsync(Message message, CancellationToken cancellationToken)
        {
            var messageText = message.Text;
            var chatId = message.Chat.Id;
            var userId = message.From.Id;
            
            var parts = messageText.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var command = parts[0].ToLower();
            
            // Get or ensure user is linked
            var userLink = await _telegramProvider.GetTelegramAvatarByTelegramIdAsync(userId);
            
            if (userLink.Result == null && command != "/start")
            {
                await _botClient.SendTextMessageAsync(
                    chatId, 
                    "❌ Please use /start first to link your account.", 
                    cancellationToken: cancellationToken);
                return;
            }
            
            switch (command)
            {
                case "/bookride":
                    await StartRideBookingFlowAsync(message, cancellationToken);
                    break;
                    
                case "/myrides":
                    await ShowRideHistoryAsync(message, userLink.Result, cancellationToken);
                    break;
                    
                case "/track":
                    if (parts.Length < 2)
                    {
                        await _botClient.SendTextMessageAsync(
                            chatId, 
                            "Usage: /track <booking_id>", 
                            cancellationToken: cancellationToken);
                        return;
                    }
                    await TrackRideAsync(message, parts[1], cancellationToken);
                    break;
                    
                case "/rate":
                    if (parts.Length < 2)
                    {
                        await _botClient.SendTextMessageAsync(
                            chatId, 
                            "Usage: /rate <booking_id>", 
                            cancellationToken: cancellationToken);
                        return;
                    }
                    await ShowRatingOptionsAsync(message, parts[1], cancellationToken);
                    break;
                    
                case "/cancel":
                    if (parts.Length < 2)
                    {
                        await _botClient.SendTextMessageAsync(
                            chatId, 
                            "Usage: /cancel <booking_id>", 
                            cancellationToken: cancellationToken);
                        return;
                    }
                    await CancelRideAsync(message, parts[1], cancellationToken);
                    break;
                    
                default:
                    await _botClient.SendTextMessageAsync(
                        chatId, 
                        "Unknown ride command. Available commands:\n" +
                        "/bookride - Book a new ride\n" +
                        "/myrides - View ride history\n" +
                        "/track <id> - Track active ride\n" +
                        "/rate <id> - Rate completed ride\n" +
                        "/cancel <id> - Cancel booking", 
                        cancellationToken: cancellationToken);
                    break;
            }
        }
        
        /// <summary>
        /// Start ride booking flow
        /// </summary>
        private async Task StartRideBookingFlowAsync(Message message, CancellationToken cancellationToken)
        {
            var chatId = message.Chat.Id;
            var userId = message.From.Id;
            
            // Check if user already has active booking
            var hasActive = await _rideStateManager.HasActiveBookingAsync(userId);
            if (hasActive)
            {
                var activeBookingId = await _rideStateManager.GetActiveBookingIdAsync(userId);
                await _botClient.SendTextMessageAsync(
                    chatId,
                    $"⚠️ You already have an active booking: {activeBookingId}\n\n" +
                    $"Please complete or cancel it first:\n" +
                    $"/track {activeBookingId}\n" +
                    $"/cancel {activeBookingId}",
                    cancellationToken: cancellationToken);
                return;
            }
            
            // Set state to waiting for pickup location
            await _rideStateManager.SetStateAsync(userId, RideBookingState.WaitingPickupLocation);
            
            // Create keyboard with location button
            var keyboard = new ReplyKeyboardMarkup(new[]
            {
                new KeyboardButton[] { KeyboardButton.WithRequestLocation("📍 Share My Location") }
            })
            {
                ResizeKeyboard = true,
                OneTimeKeyboard = true
            };
            
            await _botClient.SendTextMessageAsync(
                chatId,
                "🚖 *Book a TimoRides Ride*\n\n" +
                "📍 Please share your *pickup location*:\n\n" +
                "• Tap the button below to share your current location\n" +
                "• Or send any location from the map\n\n" +
                "_Note: You can type an address, but location sharing is more accurate_",
                parseMode: ParseMode.Markdown,
                replyMarkup: keyboard,
                cancellationToken: cancellationToken);
        }
        
        /// <summary>
        /// Handle location messages
        /// ADD TO: HandleUpdateAsync to detect location messages
        /// </summary>
        private async Task HandleLocationMessageAsync(Message message, CancellationToken cancellationToken)
        {
            var chatId = message.Chat.Id;
            var userId = message.From.Id;
            var location = message.Location;
            
            if (location == null)
                return;
            
            // Get user's current state
            var state = await _rideStateManager.GetStateAsync(userId);
            
            if (state.State == RideBookingState.WaitingPickupLocation)
            {
                await ProcessPickupLocationAsync(message, location, cancellationToken);
            }
            else if (state.State == RideBookingState.WaitingDropoffLocation)
            {
                await ProcessDropoffLocationAsync(message, location, cancellationToken);
            }
            else
            {
                await _botClient.SendTextMessageAsync(
                    chatId,
                    "I'm not expecting a location right now. Use /bookride to start a booking.",
                    cancellationToken: cancellationToken);
            }
        }
        
        /// <summary>
        /// Process pickup location
        /// </summary>
        private async Task ProcessPickupLocationAsync(Message message, Location location, CancellationToken cancellationToken)
        {
            var chatId = message.Chat.Id;
            var userId = message.From.Id;
            
            // Geocode the location
            var address = await _mapsService.ReverseGeocodeAsync(location.Latitude, location.Longitude);
            
            // Store pickup location
            await _rideStateManager.StorePickupLocationAsync(
                userId,
                location.Latitude,
                location.Longitude,
                address);
            
            // Update state
            await _rideStateManager.SetStateAsync(userId, RideBookingState.WaitingDropoffLocation);
            
            // Request dropoff location
            var keyboard = new ReplyKeyboardMarkup(new[]
            {
                new KeyboardButton[] { KeyboardButton.WithRequestLocation("📍 Share Destination") }
            })
            {
                ResizeKeyboard = true,
                OneTimeKeyboard = true
            };
            
            await _botClient.SendTextMessageAsync(
                chatId,
                $"✅ *Pickup location set*\n\n" +
                $"📍 {address}\n\n" +
                $"Now, please share your *destination* location:",
                parseMode: ParseMode.Markdown,
                replyMarkup: keyboard,
                cancellationToken: cancellationToken);
        }
        
        /// <summary>
        /// Process dropoff location and search for drivers
        /// </summary>
        private async Task ProcessDropoffLocationAsync(Message message, Location location, CancellationToken cancellationToken)
        {
            var chatId = message.Chat.Id;
            var userId = message.From.Id;
            
            // Geocode the location
            var address = await _mapsService.ReverseGeocodeAsync(location.Latitude, location.Longitude);
            
            // Store dropoff location
            await _rideStateManager.StoreDropoffLocationAsync(
                userId,
                location.Latitude,
                location.Longitude,
                address);
            
            // Get booking data
            var bookingData = await _rideStateManager.GetStateAsync(userId);
            
            // Calculate distance and duration
            var distanceInfo = await _mapsService.CalculateDistanceAsync(
                bookingData.PickupLocation.Latitude,
                bookingData.PickupLocation.Longitude,
                bookingData.DropoffLocation.Latitude,
                bookingData.DropoffLocation.Longitude);
            
            await _botClient.SendTextMessageAsync(
                chatId,
                $"✅ *Destination set*\n\n" +
                $"📍 {address}\n\n" +
                $"📏 Distance: {distanceInfo.DistanceKm:F1} km\n" +
                $"⏱️ Estimated time: {distanceInfo.DurationMinutes} minutes\n\n" +
                $"🔍 Searching for available drivers...",
                parseMode: ParseMode.Markdown,
                replyMarkup: new ReplyKeyboardRemove(),
                cancellationToken: cancellationToken);
            
            // Search for nearby drivers
            await ShowAvailableDriversAsync(message, cancellationToken);
        }
        
        /// <summary>
        /// Display available drivers
        /// </summary>
        private async Task ShowAvailableDriversAsync(Message message, CancellationToken cancellationToken)
        {
            var chatId = message.Chat.Id;
            var userId = message.From.Id;
            
            try
            {
                // Get booking data
                var bookingData = await _rideStateManager.GetStateAsync(userId);
                
                // Get nearby drivers
                var drivers = await _timoRidesService.GetNearbyDriversAsync(
                    bookingData.PickupLocation.Latitude,
                    bookingData.PickupLocation.Longitude,
                    10); // 10km radius
                
                if (!drivers.Any())
                {
                    await _botClient.SendTextMessageAsync(
                        chatId,
                        "😕 No drivers available in your area right now.\n\n" +
                        "Please try again in a few minutes, or contact support if this persists.",
                        cancellationToken: cancellationToken);
                    
                    await _rideStateManager.ClearStateAsync(userId);
                    return;
                }
                
                // Update state
                await _rideStateManager.SetStateAsync(userId, RideBookingState.SelectingDriver);
                
                // Send driver cards
                await _botClient.SendTextMessageAsync(
                    chatId,
                    $"🚗 *{drivers.Count} Drivers Available*\n\n" +
                    "Select a driver to continue:",
                    parseMode: ParseMode.Markdown,
                    cancellationToken: cancellationToken);
                
                // Send each driver as a card
                foreach (var driver in drivers.Take(5))
                {
                    await SendDriverCardAsync(chatId, driver, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                await _botClient.SendTextMessageAsync(
                    chatId,
                    $"❌ Error searching for drivers: {ex.Message}\n\n" +
                    "Please try again later.",
                    cancellationToken: cancellationToken);
                
                await _rideStateManager.ClearStateAsync(userId);
            }
        }
        
        /// <summary>
        /// Send driver card with inline keyboard
        /// </summary>
        private async Task SendDriverCardAsync(long chatId, DriverDto driver, CancellationToken cancellationToken)
        {
            var amenities = driver.Amenities.Any() 
                ? string.Join(" • ", driver.Amenities.Select(a => GetAmenityEmoji(a)))
                : "Standard features";
            
            var cardText = 
                $"👤 *{driver.GetFullName()}* ⭐ {driver.Rating:F1} ({driver.TotalRides} rides)\n" +
                $"🚗 {driver.VehicleModel} • {driver.VehicleColor}\n" +
                $"💵 R {driver.EstimatedFare}\n" +
                $"🏆 Karma: {driver.KarmaScore} ({driver.GetKarmaBadge()})\n" +
                $"🗣️ {string.Join(", ", driver.Languages)}\n" +
                $"{amenities}\n" +
                $"⏱️ {driver.ETAMinutes} min away";
            
            var keyboard = new InlineKeyboardMarkup(new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("Select Driver 🚖", $"select_driver:{driver.Id}"),
                    InlineKeyboardButton.WithCallbackData("View Profile 👤", $"driver_profile:{driver.Id}")
                }
            });
            
            await _botClient.SendTextMessageAsync(
                chatId,
                cardText,
                parseMode: ParseMode.Markdown,
                replyMarkup: keyboard,
                cancellationToken: cancellationToken);
        }
        
        private string GetAmenityEmoji(string amenity)
        {
            return amenity.ToLower() switch
            {
                "ac" => "❄️ AC",
                "wifi" => "📱 WiFi",
                "music" => "🎵 Music",
                "child_seat" => "👶 Child Seat",
                _ => amenity
            };
        }
        
        /// <summary>
        /// Handle callback queries (inline button clicks)
        /// ADD TO: HandleUpdateAsync for UpdateType.CallbackQuery
        /// </summary>
        private async Task HandleRideCallbackQueryAsync(CallbackQuery query, CancellationToken cancellationToken)
        {
            var data = query.Data;
            var chatId = query.Message.Chat.Id;
            var userId = query.From.Id;
            
            try
            {
                if (data.StartsWith("select_driver:"))
                {
                    var driverId = data.Split(':')[1];
                    await HandleDriverSelectionAsync(query, driverId, cancellationToken);
                }
                else if (data.StartsWith("driver_profile:"))
                {
                    var driverId = data.Split(':')[1];
                    await ShowDriverProfileAsync(query, driverId, cancellationToken);
                }
                else if (data.StartsWith("payment:"))
                {
                    var paymentMethod = data.Split(':')[1];
                    await HandlePaymentSelectionAsync(query, paymentMethod, cancellationToken);
                }
                else if (data == "confirm_booking")
                {
                    await ConfirmBookingAsync(query, cancellationToken);
                }
                else if (data == "cancel_booking")
                {
                    await _rideStateManager.ClearStateAsync(userId);
                    await _botClient.SendTextMessageAsync(
                        chatId,
                        "❌ Booking cancelled.",
                        cancellationToken: cancellationToken);
                }
                else if (data.StartsWith("rate:"))
                {
                    var parts = data.Split(':');
                    var bookingId = parts[1];
                    var stars = int.Parse(parts[2]);
                    await HandleRatingSubmissionAsync(query, bookingId, stars, cancellationToken);
                }
                
                // Answer callback to remove loading state
                await _botClient.AnswerCallbackQueryAsync(
                    query.Id,
                    cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                await _botClient.AnswerCallbackQueryAsync(
                    query.Id,
                    text: $"Error: {ex.Message}",
                    showAlert: true,
                    cancellationToken: cancellationToken);
            }
        }
        
        /// <summary>
        /// Handle driver selection
        /// </summary>
        private async Task HandleDriverSelectionAsync(CallbackQuery query, string driverId, CancellationToken cancellationToken)
        {
            var chatId = query.Message.Chat.Id;
            var userId = query.From.Id;
            
            // Get driver details
            var driver = await _timoRidesService.GetDriverDetailsAsync(driverId);
            
            // Store selected driver
            await _rideStateManager.StoreSelectedDriverAsync(userId, driver);
            await _rideStateManager.SetStateAsync(userId, RideBookingState.SelectingPayment);
            
            // Show payment options
            await ShowPaymentOptionsAsync(chatId, driver.EstimatedFare, cancellationToken);
        }
        
        /// <summary>
        /// Show driver profile
        /// </summary>
        private async Task ShowDriverProfileAsync(CallbackQuery query, string driverId, CancellationToken cancellationToken)
        {
            var chatId = query.Message.Chat.Id;
            
            var driver = await _timoRidesService.GetDriverDetailsAsync(driverId);
            
            var profileText = 
                $"👤 *Driver Profile*\n\n" +
                $"*Name:* {driver.GetFullName()}\n" +
                $"*Rating:* ⭐ {driver.Rating:F1}/5.0 ({driver.TotalRides} rides)\n" +
                $"*Karma Score:* 🏆 {driver.KarmaScore} ({driver.GetKarmaBadge()})\n\n" +
                $"*Vehicle:* 🚗 {driver.GetVehicleDescription()}\n" +
                $"*Type:* {driver.VehicleType}\n" +
                $"*Languages:* 🗣️ {string.Join(", ", driver.Languages)}\n" +
                $"*Features:* {string.Join(", ", driver.Amenities)}\n\n" +
                $"*Estimated Fare:* 💵 R {driver.EstimatedFare}\n" +
                $"*ETA to you:* ⏱️ {driver.ETAMinutes} minutes";
            
            var keyboard = new InlineKeyboardMarkup(new[]
            {
                new[] { InlineKeyboardButton.WithCallbackData("Select This Driver 🚖", $"select_driver:{driver.Id}") }
            });
            
            await _botClient.SendTextMessageAsync(
                chatId,
                profileText,
                parseMode: ParseMode.Markdown,
                replyMarkup: keyboard,
                cancellationToken: cancellationToken);
        }
        
        /// <summary>
        /// Show payment method options
        /// </summary>
        private async Task ShowPaymentOptionsAsync(long chatId, decimal fare, CancellationToken cancellationToken)
        {
            var keyboard = new InlineKeyboardMarkup(new[]
            {
                new[] { InlineKeyboardButton.WithCallbackData("💵 Cash on Delivery", "payment:cash") },
                new[] { InlineKeyboardButton.WithCallbackData("🪙 OASIS Wallet", "payment:wallet") },
                new[] { InlineKeyboardButton.WithCallbackData("📱 Mobile Money", "payment:mpesa") },
                new[] { InlineKeyboardButton.WithCallbackData("💳 Card", "payment:card") }
            });
            
            await _botClient.SendTextMessageAsync(
                chatId,
                $"💳 *Select Payment Method*\n\n" +
                $"Fare: *R {fare}*\n\n" +
                "Choose how you'd like to pay:",
                parseMode: ParseMode.Markdown,
                replyMarkup: keyboard,
                cancellationToken: cancellationToken);
        }
        
        /// <summary>
        /// Handle payment method selection
        /// </summary>
        private async Task HandlePaymentSelectionAsync(CallbackQuery query, string paymentMethod, CancellationToken cancellationToken)
        {
            var chatId = query.Message.Chat.Id;
            var userId = query.From.Id;
            
            // Store payment method
            await _rideStateManager.StorePaymentMethodAsync(userId, paymentMethod);
            await _rideStateManager.SetStateAsync(userId, RideBookingState.ConfirmingBooking);
            
            // Show booking summary
            var bookingData = await _rideStateManager.GetStateAsync(userId);
            
            var summaryText = 
                $"📋 *Booking Summary*\n\n" +
                $"👤 Driver: {bookingData.SelectedDriver.GetFullName()}\n" +
                $"📍 Pickup: {bookingData.PickupLocation.Address}\n" +
                $"📍 Dropoff: {bookingData.DropoffLocation.Address}\n" +
                $"💵 Fare: R {bookingData.EstimatedFare}\n" +
                $"💳 Payment: {GetPaymentMethodLabel(paymentMethod)}\n" +
                $"⏱️ ETA: {bookingData.SelectedDriver.ETAMinutes} minutes";
            
            var keyboard = new InlineKeyboardMarkup(new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("✅ Confirm Booking", "confirm_booking"),
                    InlineKeyboardButton.WithCallbackData("❌ Cancel", "cancel_booking")
                }
            });
            
            await _botClient.SendTextMessageAsync(
                chatId,
                summaryText,
                parseMode: ParseMode.Markdown,
                replyMarkup: keyboard,
                cancellationToken: cancellationToken);
        }
        
        private string GetPaymentMethodLabel(string method)
        {
            return method switch
            {
                "cash" => "💵 Cash on Delivery",
                "wallet" => "🪙 OASIS Wallet",
                "mpesa" => "📱 Mobile Money",
                "card" => "💳 Card",
                _ => method
            };
        }
        
        /// <summary>
        /// Confirm and create the booking
        /// </summary>
        private async Task ConfirmBookingAsync(CallbackQuery query, CancellationToken cancellationToken)
        {
            var chatId = query.Message.Chat.Id;
            var userId = query.From.Id;
            
            try
            {
                // Get booking data
                var bookingData = await _rideStateManager.GetStateAsync(userId);
                var userLink = await _telegramProvider.GetTelegramAvatarByTelegramIdAsync(userId);
                
                // Create booking request
                var bookingRequest = new BookingRequest
                {
                    UserId = userLink.Result.OasisAvatarId.ToString(),
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
                    Currency = "ZAR"
                };
                
                // Create booking
                var booking = await _timoRidesService.CreateBookingAsync(bookingRequest);
                
                // Store booking
                await _rideStateManager.StoreBookingAsync(userId, booking);
                
                // Send confirmation
                await _botClient.SendTextMessageAsync(
                    chatId,
                    $"✅ *Booking Confirmed!* 🎉\n\n" +
                    $"🆔 Booking ID: `{booking.BookingId}`\n" +
                    $"👤 Driver: {booking.DriverName}\n" +
                    $"🚗 Vehicle: {booking.VehicleDetails}\n" +
                    $"📱 Driver Phone: {booking.DriverPhone}\n\n" +
                    $"📍 Driver is {booking.DriverETAMinutes} minutes away\n\n" +
                    $"Track your ride: /track {booking.BookingId}\n\n" +
                    $"_You'll receive updates as your driver approaches_",
                    parseMode: ParseMode.Markdown,
                    cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                await _botClient.SendTextMessageAsync(
                    chatId,
                    $"❌ Error creating booking: {ex.Message}\n\n" +
                    "Please try again or contact support.",
                    cancellationToken: cancellationToken);
                
                await _rideStateManager.ClearStateAsync(userId);
            }
        }
        
        // Additional helper methods for /track, /rate, /myrides, etc.
        // Would be implemented similarly...
        
        private async Task ShowRideHistoryAsync(Message message, TelegramAvatar user, CancellationToken cancellationToken)
        {
            // Implementation for showing ride history
            await _botClient.SendTextMessageAsync(
                message.Chat.Id,
                "📋 Your recent rides:\n\nFeature coming soon!",
                cancellationToken: cancellationToken);
        }
        
        private async Task TrackRideAsync(Message message, string bookingId, CancellationToken cancellationToken)
        {
            // Implementation for tracking ride
            await _botClient.SendTextMessageAsync(
                message.Chat.Id,
                $"🚖 Tracking ride: {bookingId}\n\nFeature coming soon!",
                cancellationToken: cancellationToken);
        }
        
        private async Task ShowRatingOptionsAsync(Message message, string bookingId, CancellationToken cancellationToken)
        {
            var keyboard = new InlineKeyboardMarkup(new[]
            {
                new[] { InlineKeyboardButton.WithCallbackData("⭐⭐⭐⭐⭐ Excellent", $"rate:{bookingId}:5") },
                new[] { InlineKeyboardButton.WithCallbackData("⭐⭐⭐⭐ Good", $"rate:{bookingId}:4") },
                new[] { InlineKeyboardButton.WithCallbackData("⭐⭐⭐ Average", $"rate:{bookingId}:3") },
                new[] { InlineKeyboardButton.WithCallbackData("⭐⭐ Poor", $"rate:{bookingId}:2") },
                new[] { InlineKeyboardButton.WithCallbackData("⭐ Very Poor", $"rate:{bookingId}:1") }
            });
            
            await _botClient.SendTextMessageAsync(
                message.Chat.Id,
                "⭐ Rate Your Ride\n\nHow was your experience?",
                replyMarkup: keyboard,
                cancellationToken: cancellationToken);
        }
        
        private async Task HandleRatingSubmissionAsync(CallbackQuery query, string bookingId, int stars, CancellationToken cancellationToken)
        {
            var chatId = query.Message.Chat.Id;
            var userId = query.From.Id;
            
            try
            {
                // Submit rating
                await _timoRidesService.SubmitRatingAsync(bookingId, stars, null);
                
                // Award karma
                await _achievementManager.AwardKarmaAsync(userId, 20); // 20 karma per ride
                
                await _botClient.SendTextMessageAsync(
                    chatId,
                    $"✅ Thank you for your rating! ⭐ x{stars}\n\n" +
                    "🎁 You earned 20 Karma points!\n\n" +
                    "📝 Add a review? Reply with your feedback or type /skip",
                    cancellationToken: cancellationToken);
                
                // Clear booking state
                await _rideStateManager.ClearStateAsync(userId);
            }
            catch (Exception ex)
            {
                await _botClient.SendTextMessageAsync(
                    chatId,
                    $"❌ Error submitting rating: {ex.Message}",
                    cancellationToken: cancellationToken);
            }
        }
        
        private async Task CancelRideAsync(Message message, string bookingId, CancellationToken cancellationToken)
        {
            await _botClient.SendTextMessageAsync(
                message.Chat.Id,
                $"Cancelling ride: {bookingId}\n\nFeature coming soon!",
                cancellationToken: cancellationToken);
        }
    }
}



