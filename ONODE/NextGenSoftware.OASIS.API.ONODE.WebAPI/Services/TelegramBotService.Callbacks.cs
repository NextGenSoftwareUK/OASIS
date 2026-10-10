using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services
{
    public partial class TelegramBotService
    {
        private async Task HandleCallbackQueryAsync(CallbackQuery callbackQuery, CancellationToken cancellationToken)
        {
            var data = callbackQuery.Data;
            var userId = callbackQuery.From.Id;
            var chatId = callbackQuery.Message.Chat.Id;

            if (data.StartsWith("pickup_"))
            {
                var confirmedPickup = data.Replace("pickup_", "");
                StorePickup(userId, 0, 0, confirmedPickup);
                await _botClient.AnswerCallbackQueryAsync(callbackQuery.Id, "✅", cancellationToken: cancellationToken);

                if (TryGetDestination(userId, out var dest))
                {
                    var destOptions = GetLocationOptions(dest.addr);
                    if (destOptions.Count > 1)
                    {
                        var keyboard = new InlineKeyboardMarkup(
                            destOptions.Select(loc => new[] { InlineKeyboardButton.WithCallbackData(loc, $"dest_{loc}") })
                        );
                        await _botClient.SendTextMessageAsync(
                            chatId,
                            $"*Pickup confirmed*\n{confirmedPickup}\n\n" +
                            $"*Which destination?*\nI found multiple matches for '{dest.addr}':",
                            parseMode: ParseMode.Markdown,
                            replyMarkup: keyboard,
                            cancellationToken: cancellationToken
                        );
                        SetRideConversationState(userId, RideConversationStates.ConfirmingDestination);
                        return;
                    }
                    else
                    {
                        StoreDestination(userId, 0, 0, destOptions[0]);
                        await ConfirmBookingDetailsAsync(chatId, userId, confirmedPickup, destOptions[0], cancellationToken);
                        return;
                    }
                }
                return;
            }

            if (data.StartsWith("dest_"))
            {
                var confirmedDest = data.Replace("dest_", "");
                StoreDestination(userId, 0, 0, confirmedDest);
                await _botClient.AnswerCallbackQueryAsync(callbackQuery.Id, "✅", cancellationToken: cancellationToken);

                if (TryGetPickup(userId, out var pickup))
                {
                    await ConfirmBookingDetailsAsync(chatId, userId, pickup.addr, confirmedDest, cancellationToken);
                    return;
                }
                return;
            }

            if (data.StartsWith("select_"))
            {
                var driverId = data.Replace("select_", "");
                if (!TryGetDriverSummary(userId, driverId, out var driverSummary))
                {
                    await _botClient.AnswerCallbackQueryAsync(callbackQuery.Id, "Driver no longer available", cancellationToken: cancellationToken);
                    await _botClient.SendTextMessageAsync(chatId, "⚠️ That driver is no longer available. Please choose another option.", cancellationToken: cancellationToken);
                    return;
                }

                if (!TryGetPickupLocation(userId, out var pickup) || !TryGetDestinationLocation(userId, out var destination))
                {
                    await _botClient.SendTextMessageAsync(chatId, "⚠️ Missing pickup or destination details. Use /bookride to start again.", cancellationToken: cancellationToken);
                    return;
                }

                var riderIdentity = ResolveRiderIdentity(callbackQuery.From);
                var passengers = ResolvePassengerCount(userId);

                var bookingRequest = new CreateBookingRequest
                {
                    Car = driverSummary.CarId,
                    TripAmount = driverSummary.RideAmount > 0 ? driverSummary.RideAmount : 75,
                    IsCash = true,
                    DepartureTime = GetScheduledTime(userId) ?? DateTime.UtcNow.AddMinutes(5),
                    PhoneNumber = riderIdentity.phone,
                    Email = riderIdentity.email,
                    FullName = riderIdentity.fullName,
                    BookingType = "passengers",
                    Passengers = passengers,
                    State = (_timoOptions.DefaultState ?? "KwaZuluNatal").ToLowerInvariant(),
                    SourceLocation = new LocationPayload
                    {
                        Address = pickup.Address,
                        Latitude = pickup.Latitude,
                        Longitude = pickup.Longitude
                    },
                    DestinationLocation = new LocationPayload
                    {
                        Address = destination.Address,
                        Latitude = destination.Latitude,
                        Longitude = destination.Longitude
                    }
                };

                await _botClient.AnswerCallbackQueryAsync(callbackQuery.Id, "Processing booking...", cancellationToken: cancellationToken);

                var bookingResult = await _timoRidesApiService.CreateBookingAsync(bookingRequest, cancellationToken);
                if (bookingResult.IsError || string.IsNullOrWhiteSpace(bookingResult.Result?.Id))
                {
                    await _botClient.SendTextMessageAsync(chatId, $"❌ Booking failed: {bookingResult.Message ?? "Unknown error"}", cancellationToken: cancellationToken);
                    return;
                }

                var bookingId = bookingResult.Result.Id;
                StoreSelectedDriver(userId, driverId);
                StoreLastBookingId(userId, bookingId);
                ResetRideState(userId);
                StoreLastBookingId(userId, bookingId);

                await _botClient.SendTextMessageAsync(
                    chatId,
                    $"*Booking Confirmed*\n\n" +
                    $"Booking ID: `{bookingId}`\n" +
                    $"Driver: {driverSummary.DriverName}\n" +
                    $"Vehicle: {driverSummary.VehicleMake} {driverSummary.VehicleModel} ({driverSummary.VehicleColor})\n" +
                    $"Fare: R {bookingRequest.TripAmount:F0}\n\n" +
                    $"You'll receive updates as your driver approaches.",
                    parseMode: ParseMode.Markdown,
                    cancellationToken: cancellationToken);

                _ = Task.Run(async () => await SimulateDriverTrackingAsync(chatId, driverSummary.DriverName ?? "Timo Driver", bookingId));
                return;
            }

            if (data == "confirm_yes")
            {
                await _botClient.AnswerCallbackQueryAsync(callbackQuery.Id, "✅ Confirmed", cancellationToken: cancellationToken);
                if (TryGetPickup(userId, out var pickup) && TryGetDestination(userId, out var dest))
                    await ProceedWithConfirmedBooking(chatId, userId, pickup.addr, dest.addr, cancellationToken);
                return;
            }

            if (data == "confirm_cancel")
            {
                await _botClient.AnswerCallbackQueryAsync(callbackQuery.Id, "Cancelled", cancellationToken: cancellationToken);
                ResetRideState(userId);
                await _botClient.SendTextMessageAsync(chatId, "Booking cancelled.\n\nUse /bookride when you're ready to try again.", cancellationToken: cancellationToken);
                return;
            }

            if (data == "trip_confirm")
            {
                await _botClient.AnswerCallbackQueryAsync(callbackQuery.Id, "✅ Have a safe trip!", cancellationToken: cancellationToken);
                await _botClient.SendTextMessageAsync(chatId, "*Safety confirmed*\n\nEnjoy your ride!", parseMode: ParseMode.Markdown, cancellationToken: cancellationToken);
                return;
            }

            if (data == "trip_cancel")
            {
                await _botClient.AnswerCallbackQueryAsync(callbackQuery.Id, "Contact support immediately", cancellationToken: cancellationToken);
                await _botClient.SendTextMessageAsync(
                    chatId,
                    "*⚠️ Safety Alert*\n\n" +
                    "Do NOT enter the vehicle.\n\n" +
                    "If this is not your driver:\n" +
                    "• Contact TimoRides support immediately\n" +
                    "• Stay in a safe location\n" +
                    "• Report the incorrect vehicle\n\n" +
                    "Support: +27 XX XXX XXXX",
                    parseMode: ParseMode.Markdown,
                    cancellationToken: cancellationToken
                );
                return;
            }

            if (data.StartsWith("rate_"))
            {
                var rating = data.Replace("rate_", "");
                var stars = new string('⭐', int.Parse(rating));
                await _botClient.AnswerCallbackQueryAsync(callbackQuery.Id, "✅ Thank you for your feedback!", cancellationToken: cancellationToken);
                await _botClient.SendTextMessageAsync(
                    chatId,
                    $"*Rating submitted*\n\n" +
                    $"You rated: {stars}\n" +
                    $"+10 Karma points earned\n\n" +
                    $"Thank you for riding with TimoRides.\n" +
                    $"Use /bookride for your next trip.",
                    parseMode: ParseMode.Markdown,
                    cancellationToken: cancellationToken
                );
                return;
            }

            await _botClient.AnswerCallbackQueryAsync(callbackQuery.Id, "Processing...", cancellationToken: cancellationToken);
        }

        private async Task SimulateDriverTrackingAsync(long chatId, string driverName, string bookingId)
        {
            try
            {
                var firstName = driverName.Split(' ')[0];

                await Task.Delay(TimeSpan.FromSeconds(5));
                await _botClient.SendTextMessageAsync(chatId, $"*{firstName} accepted your ride*\n\n4.2 km away • ETA 5 minutes", parseMode: ParseMode.Markdown);

                await Task.Delay(TimeSpan.FromSeconds(8));
                await _botClient.SendTextMessageAsync(chatId, $"*{firstName} is on the way*\n\n2.8 km away • ETA 3 minutes", parseMode: ParseMode.Markdown);

                await Task.Delay(TimeSpan.FromSeconds(8));
                await _botClient.SendTextMessageAsync(chatId, $"*{firstName} is nearby*\n\n800 meters away • ETA 1 minute", parseMode: ParseMode.Markdown);

                await Task.Delay(TimeSpan.FromSeconds(10));
                await _botClient.SendAnimationAsync(
                    chatId,
                    animation: InputFile.FromUri("https://media.giphy.com/media/l0HlBO7eyXzSZkJri/giphy.gif"),
                    caption: $"*{firstName} has arrived*\n\nYour driver is waiting at the pickup location.",
                    parseMode: ParseMode.Markdown
                );

                await Task.Delay(TimeSpan.FromSeconds(3));
                var vehicleDetails = GetVehicleDetails(driverName);
                var safetyKeyboard = new InlineKeyboardMarkup(new[]
                {
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData("✅ Confirmed, get in", "trip_confirm"),
                        InlineKeyboardButton.WithCallbackData("❌ Not my ride", "trip_cancel")
                    }
                });

                await _botClient.SendTextMessageAsync(
                    chatId,
                    $"*Safety Check*\n\n" +
                    $"Please confirm the vehicle details:\n\n" +
                    $"Driver: {driverName}\n" +
                    $"Vehicle: {vehicleDetails.car} ({vehicleDetails.color})\n" +
                    $"Plate: {vehicleDetails.plate}\n\n" +
                    $"Does everything match?",
                    parseMode: ParseMode.Markdown,
                    replyMarkup: safetyKeyboard
                );

                await Task.Delay(TimeSpan.FromSeconds(10));
                await _botClient.SendTextMessageAsync(chatId, $"*Trip started*\n\nTracking: `{bookingId}`", parseMode: ParseMode.Markdown);

                await Task.Delay(TimeSpan.FromSeconds(15));
                await _botClient.SendTextMessageAsync(chatId, $"*Approaching destination*\n\n500 meters • Arriving in 1 minute", parseMode: ParseMode.Markdown);

                await Task.Delay(TimeSpan.FromSeconds(10));
                await _botClient.SendAnimationAsync(
                    chatId,
                    animation: InputFile.FromUri("https://media.giphy.com/media/l0MYt5jPR6QX5pnqM/giphy.gif"),
                    caption: $"*Trip completed*\n\nFare: R 420\nDuration: 18 minutes\nDistance: 12.5 km\n\nRate your experience with {firstName}:",
                    parseMode: ParseMode.Markdown,
                    replyMarkup: new InlineKeyboardMarkup(new[]
                    {
                        new[]
                        {
                            InlineKeyboardButton.WithCallbackData("⭐⭐⭐⭐⭐", "rate_5"),
                            InlineKeyboardButton.WithCallbackData("⭐⭐⭐⭐", "rate_4"),
                            InlineKeyboardButton.WithCallbackData("⭐⭐⭐", "rate_3")
                        }
                    })
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in driver tracking simulation");
            }
        }
    }
}
