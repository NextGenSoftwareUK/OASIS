using System;
using System.Collections.Generic;
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
        private async Task ShowAvailableDriversAsync(long chatId, long userId, CancellationToken cancellationToken)
        {
            if (!TryGetPickupLocation(userId, out var pickup) || !TryGetDestinationLocation(userId, out var destination))
            {
                await _botClient.SendTextMessageAsync(chatId, "⚠️ I need both pickup and destination before showing drivers.\nUse /bookride to restart.", cancellationToken: cancellationToken);
                return;
            }

            var searchResult = await _timoRidesApiService.GetNearbyDriversAsync(pickup, destination, GetScheduledTime(userId), cancellationToken);
            if (searchResult.IsError || searchResult.Result == null || searchResult.Result.Count == 0)
            {
                var errorMessage = searchResult.IsError ? searchResult.Message : "No nearby drivers are available right now.";
                await _botClient.SendTextMessageAsync(chatId, $"❌ {errorMessage}\n\nTry adjusting your pickup spot or try again in a moment.", cancellationToken: cancellationToken);
                return;
            }

            StoreAvailableDrivers(userId, searchResult.Result);

            await _botClient.SendAnimationAsync(
                chatId,
                animation: InputFile.FromUri("https://media.giphy.com/media/3oEjI6SIIHBdRxXI40/giphy.gif"),
                caption: $"*{searchResult.Result.Count} drivers found*\n\nSelect your driver:",
                parseMode: ParseMode.Markdown,
                cancellationToken: cancellationToken);

            foreach (var driver in searchResult.Result)
            {
                var fare = driver.RideAmount > 0 ? $"R {driver.RideAmount:F0}" : "Fare on request";
                var keyboard = new InlineKeyboardMarkup(new[]
                {
                    InlineKeyboardButton.WithCallbackData($"✅ Select {driver.DriverName?.Split(' ')[0] ?? "driver"}", $"select_{driver.CarId}")
                });

                var caption =
$@"*{driver.DriverName ?? "Timo Driver"}*
Rating: {driver.Rating:F1} ⭐
{driver.VehicleMake} {driver.VehicleModel} ({driver.VehicleColor})
Fare: {fare}
ETA: {driver.DurationAway ?? "—"} • Distance: {driver.DistanceAway ?? "—"}";

                try
                {
                    if (!string.IsNullOrWhiteSpace(driver.PhotoUrl))
                    {
                        await _botClient.SendPhotoAsync(
                            chatId: chatId,
                            photo: InputFile.FromUri(driver.PhotoUrl),
                            caption: caption,
                            parseMode: ParseMode.Markdown,
                            replyMarkup: keyboard,
                            cancellationToken: cancellationToken);
                    }
                    else
                    {
                        await _botClient.SendTextMessageAsync(chatId, caption, parseMode: ParseMode.Markdown, replyMarkup: keyboard, cancellationToken: cancellationToken);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to send driver card for {Driver}", driver.DriverName);
                    await _botClient.SendTextMessageAsync(chatId, caption, parseMode: ParseMode.Markdown, replyMarkup: keyboard, cancellationToken: cancellationToken);
                }

                await Task.Delay(300, cancellationToken);
            }

            SetRideConversationState(userId, RideConversationStates.SelectingDriver);
        }

        private async Task ProcessNaturalLanguageAsync(long chatId, User user, string userMessage, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation($"Processing natural language: '{userMessage}'");

                var passengerInfo = ExtractPassengerInfo(userMessage);
                if (passengerInfo.recommendation != null)
                {
                    await _botClient.SendTextMessageAsync(
                        chatId,
                        $"*{passengerInfo.recommendation}*\n\n" +
                        (passengerInfo.passengers.HasValue && passengerInfo.passengers.Value >= 6
                            ? "I'll show you SUV and larger vehicle options.\n\n"
                            : "") +
                        "Now, where are you going?",
                        parseMode: ParseMode.Markdown,
                        cancellationToken: cancellationToken
                    );

                    if (passengerInfo.passengers.HasValue && passengerInfo.passengers.Value >= 6)
                        StoreVehiclePreference(user.Id, "SUV");
                }

                if (!string.IsNullOrEmpty(_openAiApiKey))
                {
                    var aiResult = await ProcessWithOpenAI(userMessage);
                    if (aiResult != null)
                    {
                        await HandleAIExtractedBooking(chatId, user, aiResult, cancellationToken);
                        return;
                    }
                }

                var lowerMessage = userMessage.ToLower();
                var scheduledTime = ExtractScheduledTime(userMessage);
                var vehiclePreference = ExtractVehiclePreference(userMessage);
                var multiStops = ExtractMultiStops(userMessage);

                if (lowerMessage.Contains("ride") || lowerMessage.Contains("book") ||
                    lowerMessage.Contains("driver") || lowerMessage.Contains("taxi") ||
                    lowerMessage.Contains("take me") || lowerMessage.Contains("go to") ||
                    lowerMessage.Contains("need to get to"))
                {
                    var responseText = "*I can help you book a ride*\n\n";

                    if (scheduledTime.HasValue)
                    {
                        StoreScheduledTime(user.Id, scheduledTime.Value);
                        responseText += $"Scheduled for: {scheduledTime.Value:MMM dd, h:mm tt}\n";
                    }
                    if (!string.IsNullOrEmpty(vehiclePreference))
                    {
                        StoreVehiclePreference(user.Id, vehiclePreference);
                        responseText += $"Vehicle preference: {vehiclePreference}\n";
                    }
                    if (multiStops.Any())
                    {
                        StoreStops(user.Id, multiStops);
                        responseText += $"Stops: {string.Join(" → ", multiStops)}\n";
                    }

                    responseText += "\nUse /bookride to get started, or tell me:\n" +
                                   "• Where you're going (e.g., 'Gateway Mall to King Shaka Airport')\n" +
                                   "• Just your destination (e.g., 'Take me to uShaka Beach')";

                    await _botClient.SendTextMessageAsync(chatId, responseText, parseMode: ParseMode.Markdown, cancellationToken: cancellationToken);
                    return;
                }

                if (ContainsLocationKeywords(lowerMessage))
                {
                    var locations = ExtractLocations(userMessage);
                    if (locations.pickup != null && locations.destination != null)
                    {
                        var confirmationText = "*Got it!*\n\n" +
                            $"Pickup: {locations.pickup}\n" +
                            $"Destination: {locations.destination}\n";

                        if (scheduledTime.HasValue)
                        {
                            StoreScheduledTime(user.Id, scheduledTime.Value);
                            confirmationText += $"Time: {scheduledTime.Value:h:mm tt on MMM dd}\n";
                        }
                        if (!string.IsNullOrEmpty(vehiclePreference))
                        {
                            StoreVehiclePreference(user.Id, vehiclePreference);
                            confirmationText += $"Preference: {vehiclePreference}\n";
                        }
                        if (multiStops.Any())
                        {
                            StoreStops(user.Id, multiStops);
                            confirmationText += $"Via: {string.Join(", ", multiStops)}\n";
                        }

                        confirmationText += "\nFinding drivers...";

                        await _botClient.SendTextMessageAsync(chatId, confirmationText, parseMode: ParseMode.Markdown, cancellationToken: cancellationToken);

                        if (!await ResolveAndStoreLocationAsync(user.Id, chatId, locations.pickup, true, cancellationToken))
                            return;
                        if (!await ResolveAndStoreLocationAsync(user.Id, chatId, locations.destination, false, cancellationToken))
                            return;
                        await ShowAvailableDriversAsync(chatId, user.Id, cancellationToken);
                        return;
                    }
                }

                await _botClient.SendTextMessageAsync(
                    chatId,
                    "I'm TimoRides AI assistant. How can I help you today?\n\n" +
                    "Try saying:\n" +
                    "• 'I need a ride to the airport tomorrow at 3pm'\n" +
                    "• 'Book me a luxury car from Gateway to King Shaka'\n" +
                    "• 'Take me from Durban to Ballito via uShaka Beach'\n" +
                    "• Or use /bookride",
                    parseMode: ParseMode.Markdown,
                    cancellationToken: cancellationToken
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing natural language");
                await _botClient.SendTextMessageAsync(chatId, "I didn't quite catch that. Use /help to see available commands.", cancellationToken: cancellationToken);
            }
        }

        private async Task<AIBookingResult> ProcessWithOpenAI(string userMessage)
        {
            try
            {
                var systemPrompt = @"You are a ride-booking assistant. Extract booking details from user messages.
Respond ONLY with a JSON object in this exact format:
{
  ""intent"": ""book_ride"" or ""general"",
  ""pickup"": ""location or null"",
  ""destination"": ""location or null"",
  ""scheduledTime"": ""ISO datetime or null"",
  ""vehicleType"": ""luxury/economy/suv/sedan or null"",
  ""multiStops"": [""location1"", ""location2""] or []
}";

                var requestBody = new
                {
                    model = "gpt-4o-mini",
                    messages = new[]
                    {
                        new { role = "system", content = systemPrompt },
                        new { role = "user", content = userMessage }
                    },
                    temperature = 0.3,
                    max_tokens = 200
                };

                var json = System.Text.Json.JsonSerializer.Serialize(requestBody);
                var content = new System.Net.Http.StringContent(json, System.Text.Encoding.UTF8, "application/json");

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_openAiApiKey}");

                var response = await _httpClient.PostAsync("https://api.openai.com/v1/chat/completions", content);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning($"OpenAI API failed: {response.StatusCode}");
                    return null;
                }

                var responseText = await response.Content.ReadAsStringAsync();
                var responseJson = System.Text.Json.JsonDocument.Parse(responseText);
                var aiResponse = responseJson.RootElement
                    .GetProperty("choices")[0]
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString();

                _logger.LogInformation($"OpenAI response: {aiResponse}");
                return System.Text.Json.JsonSerializer.Deserialize<AIBookingResult>(aiResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OpenAI processing failed");
                return null;
            }
        }

        private async Task HandleAIExtractedBooking(long chatId, User user, AIBookingResult aiResult, CancellationToken cancellationToken)
        {
            if (aiResult.intent != "book_ride")
            {
                await _botClient.SendTextMessageAsync(
                    chatId,
                    "I'm here to help you book rides. Try saying:\n" +
                    "• 'I need a ride to the airport tomorrow at 3pm'\n" +
                    "• 'Book me a luxury car from Gateway to King Shaka'",
                    cancellationToken: cancellationToken
                );
                return;
            }

            if (!string.IsNullOrEmpty(aiResult.scheduledTime) && aiResult.scheduledTime != "null")
            {
                if (DateTime.TryParse(aiResult.scheduledTime, out var scheduledDt))
                    StoreScheduledTime(user.Id, scheduledDt);
            }

            if (!string.IsNullOrEmpty(aiResult.vehicleType) && aiResult.vehicleType != "null")
                StoreVehiclePreference(user.Id, aiResult.vehicleType);

            if (aiResult.multiStops != null && aiResult.multiStops.Any())
                StoreStops(user.Id, aiResult.multiStops);

            if (!string.IsNullOrEmpty(aiResult.destination) && aiResult.destination != "null")
                if (!await ResolveAndStoreLocationAsync(user.Id, chatId, aiResult.destination, false, cancellationToken))
                    return;

            var hasPickup = !string.IsNullOrEmpty(aiResult.pickup) && aiResult.pickup != "null";

            if (!hasPickup)
            {
                await _botClient.SendTextMessageAsync(
                    chatId,
                    "*Let's start with your pickup location*\n\n" +
                    "Where should I pick you up?\n\n" +
                    "Example: 'Gateway Mall' or 'King Shaka Airport'",
                    parseMode: ParseMode.Markdown,
                    cancellationToken: cancellationToken
                );
                SetRideConversationState(user.Id, RideConversationStates.WaitingPickup);
                return;
            }

            var pickupOptions = GetLocationOptions(aiResult.pickup);

            if (pickupOptions.Count == 0)
            {
                await _botClient.SendTextMessageAsync(
                    chatId,
                    $"*I need more details about '{aiResult.pickup}'*\n\n" +
                    "Please provide a more specific pickup location.\n\n" +
                    "Example: 'Gateway Mall, Umhlanga'",
                    parseMode: ParseMode.Markdown,
                    cancellationToken: cancellationToken
                );
                SetRideConversationState(user.Id, RideConversationStates.WaitingPickup);
                return;
            }

            if (pickupOptions.Count > 1)
            {
                var keyboard = new InlineKeyboardMarkup(
                    pickupOptions.Select(loc => new[] { InlineKeyboardButton.WithCallbackData(loc, $"pickup_{loc}") })
                );
                await _botClient.SendTextMessageAsync(
                    chatId,
                    $"*Confirm your pickup location*\n\nWhich one?",
                    parseMode: ParseMode.Markdown,
                    replyMarkup: keyboard,
                    cancellationToken: cancellationToken
                );
                SetRideConversationState(user.Id, RideConversationStates.ConfirmingPickup);
                return;
            }

            if (!await ResolveAndStoreLocationAsync(user.Id, chatId, pickupOptions[0], true, cancellationToken))
                return;

            var hasDestination = !string.IsNullOrEmpty(aiResult.destination) && aiResult.destination != "null";

            if (!hasDestination)
            {
                await _botClient.SendTextMessageAsync(
                    chatId,
                    $"*Pickup confirmed: {pickupOptions[0]}*\n\nWhere are you going?",
                    parseMode: ParseMode.Markdown,
                    cancellationToken: cancellationToken
                );
                SetRideConversationState(user.Id, RideConversationStates.WaitingDropoff);
                return;
            }

            var destOptions = GetLocationOptions(aiResult.destination);

            if (destOptions.Count == 0)
            {
                await _botClient.SendTextMessageAsync(
                    chatId,
                    $"*Pickup confirmed: {pickupOptions[0]}*\n\n" +
                    $"I need more details about '{aiResult.destination}'.\n\n" +
                    "Please provide the full name or address.\n\n" +
                    "Example: 'Sheraton Hotel, Marine Parade'",
                    parseMode: ParseMode.Markdown,
                    cancellationToken: cancellationToken
                );
                SetRideConversationState(user.Id, RideConversationStates.WaitingDropoff);
                return;
            }

            if (destOptions.Count > 1)
            {
                var keyboard = new InlineKeyboardMarkup(
                    destOptions.Select(loc => new[] { InlineKeyboardButton.WithCallbackData(loc, $"dest_{loc}") })
                );
                await _botClient.SendTextMessageAsync(
                    chatId,
                    $"*Pickup confirmed: {pickupOptions[0]}*\n\n*Which destination?*\n\nI found multiple options:",
                    parseMode: ParseMode.Markdown,
                    replyMarkup: keyboard,
                    cancellationToken: cancellationToken
                );
                SetRideConversationState(user.Id, RideConversationStates.ConfirmingDestination);
                return;
            }

            if (!await ResolveAndStoreLocationAsync(user.Id, chatId, destOptions[0], false, cancellationToken))
                return;

            await ConfirmBookingDetailsAsync(chatId, user.Id, pickupOptions[0], destOptions[0], cancellationToken);
        }

        private bool ContainsLocationKeywords(string message)
        {
            var keywords = new[] { "from", "to", "at", "near", "beach", "mall", "airport", "street", "road", "avenue" };
            return keywords.Any(k => message.Contains(k));
        }

        private (string pickup, string destination) ExtractLocations(string message)
        {
            var patterns = new[]
            {
                @"from\s+(.+?)\s+to\s+(.+)",
                @"(.+?)\s+to\s+(.+)",
                @"take me to\s+(.+)",
                @"go to\s+(.+)",
                @"going to\s+(.+)"
            };

            foreach (var pattern in patterns)
            {
                var match = System.Text.RegularExpressions.Regex.Match(message, pattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    if (match.Groups.Count == 3)
                        return (match.Groups[1].Value.Trim(), match.Groups[2].Value.Trim());
                    else if (match.Groups.Count == 2)
                        return (null, match.Groups[1].Value.Trim());
                }
            }

            return (null, null);
        }

        private DateTime? ExtractScheduledTime(string message)
        {
            var lowerMessage = message.ToLower();
            var now = DateTime.Now;

            if (lowerMessage.Contains("tomorrow"))
            {
                var time = ExtractTimeOfDay(message) ?? new TimeSpan(9, 0, 0);
                return now.Date.AddDays(1).Add(time);
            }

            if (lowerMessage.Contains("today") || lowerMessage.Contains("this evening") || lowerMessage.Contains("tonight"))
            {
                var time = ExtractTimeOfDay(message) ?? new TimeSpan(18, 0, 0);
                return now.Date.Add(time);
            }

            var timeMatch = System.Text.RegularExpressions.Regex.Match(
                message,
                @"at\s+(\d{1,2})(?::(\d{2}))?\s*(am|pm)?",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase
            );

            if (timeMatch.Success)
            {
                var hour = int.Parse(timeMatch.Groups[1].Value);
                var minute = timeMatch.Groups[2].Success ? int.Parse(timeMatch.Groups[2].Value) : 0;
                var isPm = timeMatch.Groups[3].Value.ToLower() == "pm";

                if (isPm && hour < 12) hour += 12;
                if (!isPm && hour == 12) hour = 0;

                var scheduledTime = now.Date.Add(new TimeSpan(hour, minute, 0));
                if (scheduledTime < now) scheduledTime = scheduledTime.AddDays(1);
                return scheduledTime;
            }

            var relativeMatch = System.Text.RegularExpressions.Regex.Match(
                message,
                @"in\s+(\d+)\s*(hour|minute|min)s?",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase
            );

            if (relativeMatch.Success)
            {
                var amount = int.Parse(relativeMatch.Groups[1].Value);
                var unit = relativeMatch.Groups[2].Value.ToLower();
                return unit.StartsWith("hour") ? now.AddHours(amount) : now.AddMinutes(amount);
            }

            return null;
        }

        private TimeSpan? ExtractTimeOfDay(string message)
        {
            var match = System.Text.RegularExpressions.Regex.Match(
                message,
                @"(\d{1,2})(?::(\d{2}))?\s*(am|pm)",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase
            );

            if (match.Success)
            {
                var hour = int.Parse(match.Groups[1].Value);
                var minute = match.Groups[2].Success ? int.Parse(match.Groups[2].Value) : 0;
                var isPm = match.Groups[3].Value.ToLower() == "pm";

                if (isPm && hour < 12) hour += 12;
                if (!isPm && hour == 12) hour = 0;

                return new TimeSpan(hour, minute, 0);
            }

            return null;
        }

        private string ExtractVehiclePreference(string message)
        {
            var lowerMessage = message.ToLower();
            if (lowerMessage.Contains("luxury") || lowerMessage.Contains("premium") || lowerMessage.Contains("merc"))
                return "Luxury";
            if (lowerMessage.Contains("economy") || lowerMessage.Contains("cheap") || lowerMessage.Contains("budget"))
                return "Economy";
            if (lowerMessage.Contains("suv") || lowerMessage.Contains("big car") || lowerMessage.Contains("large"))
                return "SUV";
            if (lowerMessage.Contains("sedan") || lowerMessage.Contains("standard"))
                return "Sedan";
            return null;
        }

        private List<string> ExtractMultiStops(string message)
        {
            var stops = new List<string>();
            var viaMatches = System.Text.RegularExpressions.Regex.Matches(
                message,
                @"(?:via|stop at|stopping at)\s+([^,\.]+)",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase
            );

            foreach (System.Text.RegularExpressions.Match match in viaMatches)
            {
                if (match.Success)
                    stops.Add(match.Groups[1].Value.Trim());
            }

            return stops;
        }

        private async Task HandleConfirmationUpdatesAsync(long chatId, long userId, string message, CancellationToken cancellationToken)
        {
            var passengerInfo = ExtractPassengerInfo(message);
            if (passengerInfo.passengers.HasValue)
            {
                _rideState?.SetPassengerCount(userId, passengerInfo.passengers.Value);
                var vehicleRecommendation = passengerInfo.passengers.Value >= 6 ? "SUV" :
                                           passengerInfo.passengers.Value >= 4 ? "Sedan" : "Standard";
                StoreVehiclePreference(userId, vehicleRecommendation);

                await _botClient.SendTextMessageAsync(
                    chatId,
                    $"*Updated: {passengerInfo.passengers.Value} passengers*\n\n" +
                    $"Recommended vehicle: {vehicleRecommendation}",
                    parseMode: ParseMode.Markdown,
                    cancellationToken: cancellationToken
                );
            }

            var vehiclePref = ExtractVehiclePreference(message);
            if (!string.IsNullOrEmpty(vehiclePref))
            {
                StoreVehiclePreference(userId, vehiclePref);
                await _botClient.SendTextMessageAsync(chatId, $"*Updated: {vehiclePref} vehicle*", parseMode: ParseMode.Markdown, cancellationToken: cancellationToken);
            }

            if (TryGetPickup(userId, out var pickup) && TryGetDestination(userId, out var dest))
            {
                await Task.Delay(500, cancellationToken);
                await ConfirmBookingDetailsAsync(chatId, userId, pickup.addr, dest.addr, cancellationToken);
            }
        }

        private class AIBookingResult
        {
            public string intent { get; set; }
            public string pickup { get; set; }
            public string destination { get; set; }
            public string scheduledTime { get; set; }
            public string vehicleType { get; set; }
            public string[] multiStops { get; set; }
        }
    }
}
