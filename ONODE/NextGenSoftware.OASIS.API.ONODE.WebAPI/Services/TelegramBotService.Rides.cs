using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
        #region TimoRides Ride Booking Handlers

        private async Task HandleBookRideCommand(long chatId, User user, CancellationToken cancellationToken)
        {
            var userId = user.Id;
            ResetRideState(userId);

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
                "*Book a ride*\n\n" +
                "Share your pickup location:\n" +
                "• Tap the button below, or\n" +
                "• Type an address (e.g., 'uShaka Beach, Durban')",
                parseMode: ParseMode.Markdown,
                replyMarkup: keyboard,
                cancellationToken: cancellationToken
            );

            SetRideConversationState(userId, RideConversationStates.WaitingPickup);
        }

        private async Task HandleMyRidesCommand(long chatId, User user, CancellationToken cancellationToken)
        {
            var ridesResult = await _timoRidesApiService.GetRiderBookingsAsync(cancellationToken);
            if (ridesResult.IsError || ridesResult.Result == null || ridesResult.Result.Count == 0)
            {
                await _botClient.SendTextMessageAsync(
                    chatId,
                    $"📋 <b>Your Ride History</b>\n\n" +
                    $"{(ridesResult.IsError ? $"Unable to load rides: {ridesResult.Message}" : "No rides yet.")}\n\n" +
                    "Use /bookride to schedule your first trip.",
                    parseMode: ParseMode.Html,
                    cancellationToken: cancellationToken);
                return;
            }

            var builder = new StringBuilder();
            builder.AppendLine("📋 <b>Your Recent Rides</b>\n");
            var count = 0;
            foreach (var booking in ridesResult.Result)
            {
                builder.AppendLine(FormatBookingSummary(booking));
                builder.AppendLine();
                if (++count == 5) break;
            }

            await _botClient.SendTextMessageAsync(chatId, builder.ToString(), parseMode: ParseMode.Html, cancellationToken: cancellationToken);
        }

        private async Task HandleTrackRideCommand(long chatId, User user, string[] args, CancellationToken cancellationToken)
        {
            var bookingId = args.Length > 0 ? args[0] : GetLastBookingId(user.Id);
            if (string.IsNullOrWhiteSpace(bookingId))
            {
                await _botClient.SendTextMessageAsync(chatId, "❓ Please provide a booking ID\n\nExample: /track 674f2c8f5d1a4f1c1a2b3c4d", cancellationToken: cancellationToken);
                return;
            }

            var bookingResult = await _timoRidesApiService.GetBookingAsync(bookingId, cancellationToken);
            if (bookingResult.IsError || bookingResult.Result == null)
            {
                await _botClient.SendTextMessageAsync(chatId, $"❌ Could not load booking `{bookingId}`\n{bookingResult.Message}", parseMode: ParseMode.Markdown, cancellationToken: cancellationToken);
                return;
            }

            var booking = bookingResult.Result;
            var status = booking.Status?.ToUpperInvariant() ?? "PENDING";

            var message =
$@"🚖 *Ride Status*
Booking ID: `{bookingId}`
Status: *{status}*
From: {booking.SourceLocation?.Address}
To: {booking.DestinationLocation?.Address}
Departure: {booking.DepartureTime:MMM dd, h:mm tt}
Fare: {booking.TripAmount}

Use /cancel {bookingId} to cancel if needed.";

            await _botClient.SendTextMessageAsync(chatId, message, parseMode: ParseMode.Markdown, cancellationToken: cancellationToken);
        }

        private async Task HandleCancelRideCommand(long chatId, User user, string[] args, CancellationToken cancellationToken)
        {
            var bookingId = args.Length > 0 ? args[0] : GetLastBookingId(user.Id);
            if (string.IsNullOrWhiteSpace(bookingId))
            {
                await _botClient.SendTextMessageAsync(chatId, "❓ Provide the booking ID to cancel.\nExample: /cancel 674f2c8f5d1a4f1c1a2b3c4d", cancellationToken: cancellationToken);
                return;
            }

            var reason = args.Length > 1 ? string.Join(' ', args.Skip(1)) : "Cancelled via Telegram";
            var cancelResult = await _timoRidesApiService.CancelBookingAsync(bookingId, reason, cancellationToken);
            if (cancelResult.IsError)
            {
                await _botClient.SendTextMessageAsync(chatId, $"❌ Unable to cancel booking `{bookingId}`\n{cancelResult.Message}", parseMode: ParseMode.Markdown, cancellationToken: cancellationToken);
                return;
            }

            await _botClient.SendTextMessageAsync(chatId, $"✅ Booking `{bookingId}` cancelled.\nIf you need another ride, use /bookride.", parseMode: ParseMode.Markdown, cancellationToken: cancellationToken);
        }

        private static string FormatBookingSummary(TimoBooking booking)
        {
            var status = booking.Status ?? "pending";
            var source = booking.SourceLocation?.Address ?? "Unknown pickup";
            var destination = booking.DestinationLocation?.Address ?? "Unknown destination";
            var departure = booking.DepartureTime.ToLocalTime();
            return $"• <code>{booking.Id}</code> - <b>{status}</b>\n  {source} → {destination}\n  {departure:MMM dd, h:mm tt}";
        }

        private async Task HandleLocationAsync(Telegram.Bot.Types.Message message, CancellationToken cancellationToken)
        {
            var userId = message.From.Id;
            var chatId = message.Chat.Id;

            if (!TryGetRideConversationState(userId, out var state))
                return;

            var location = message.Location;
            var address = $"{location.Latitude:F4}, {location.Longitude:F4}";

            if (state == RideConversationStates.WaitingPickup)
            {
                StorePickup(userId, location.Latitude, location.Longitude, address);

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
                    $"*Pickup set*\n{address}\n\nNow share your destination:",
                    parseMode: ParseMode.Markdown,
                    replyMarkup: keyboard,
                    cancellationToken: cancellationToken
                );

                SetRideConversationState(userId, RideConversationStates.WaitingDropoff);
            }
            else if (state == RideConversationStates.WaitingDropoff)
            {
                StoreDestination(userId, location.Latitude, location.Longitude, address);

                await _botClient.SendTextMessageAsync(
                    chatId,
                    $"*Destination set*\n{address}\n\nFinding drivers...",
                    parseMode: ParseMode.Markdown,
                    replyMarkup: new ReplyKeyboardRemove(),
                    cancellationToken: cancellationToken
                );

                try
                {
                    await ShowAvailableDriversAsync(chatId, userId, cancellationToken);
                    SetRideConversationState(userId, RideConversationStates.SelectingDriver);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error showing drivers");
                    await _botClient.SendTextMessageAsync(chatId, $"❌ Error loading drivers: {ex.Message}", cancellationToken: cancellationToken);
                }
            }
        }

        private async Task HandleAddressTextAsync(Telegram.Bot.Types.Message message, CancellationToken cancellationToken)
        {
            var userId = message.From.Id;
            var chatId = message.Chat.Id;
            var address = message.Text;
            var state = GetRideConversationState(userId);

            var vaguePhrases = new[] { "my hotel", "the hotel", "my place", "home", "work", "office", "my house" };
            if (vaguePhrases.Any(p => address.ToLower().Contains(p)))
            {
                await _botClient.SendTextMessageAsync(
                    chatId,
                    $"*Which {address.ToLower().Replace("my ", "").Replace("the ", "")}?*\n\n" +
                    "Please provide the full name or address.\n\n" +
                    "Example:\n" +
                    "• 'Hilton Durban Hotel'\n" +
                    "• '123 Marine Parade, Durban'",
                    parseMode: ParseMode.Markdown,
                    cancellationToken: cancellationToken
                );
                return;
            }

            if (state == RideConversationStates.WaitingPickup)
            {
                if (!await ResolveAndStoreLocationAsync(userId, chatId, address, true, cancellationToken))
                    return;

                await _botClient.SendTextMessageAsync(
                    chatId,
                    $"*Pickup set*\n{address}\n\nNow, where would you like to go?",
                    parseMode: ParseMode.Markdown,
                    replyMarkup: new ReplyKeyboardMarkup(new[]
                    {
                        new KeyboardButton[] { KeyboardButton.WithRequestLocation("Share Destination") }
                    })
                    {
                        ResizeKeyboard = true,
                        OneTimeKeyboard = true
                    },
                    cancellationToken: cancellationToken
                );

                SetRideConversationState(userId, RideConversationStates.WaitingDropoff);
            }
            else if (state == RideConversationStates.WaitingDropoff)
            {
                if (!await ResolveAndStoreLocationAsync(userId, chatId, address, false, cancellationToken))
                    return;

                await _botClient.SendTextMessageAsync(
                    chatId,
                    $"*Destination set*\n{address}\n\nFinding drivers...",
                    parseMode: ParseMode.Markdown,
                    replyMarkup: new ReplyKeyboardRemove(),
                    cancellationToken: cancellationToken
                );

                try
                {
                    await ShowAvailableDriversAsync(chatId, userId, cancellationToken);
                    SetRideConversationState(userId, RideConversationStates.SelectingDriver);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error showing drivers");
                    await _botClient.SendTextMessageAsync(chatId, $"❌ Error: {ex.Message}", cancellationToken: cancellationToken);
                }
            }
        }

        #endregion
    }
}
