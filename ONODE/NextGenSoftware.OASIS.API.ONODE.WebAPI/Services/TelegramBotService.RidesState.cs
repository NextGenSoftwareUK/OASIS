using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services
{
    public partial class TelegramBotService
    {
        #region Ride State Helpers

        private void ResetRideState(long userId) => _rideState?.Clear(userId);
        private void SetRideConversationState(long userId, string state) => _rideState?.SetConversationState(userId, state);

        private bool TryGetRideConversationState(long userId, out string state)
        {
            state = null;
            return _rideState != null && _rideState.TryGetConversationState(userId, out state);
        }

        private string GetRideConversationState(long userId)
            => _rideState?.GetOrCreate(userId).ConversationState ?? RideConversationStates.None;

        private void StorePickup(long userId, double lat, double lon, string address)
            => _rideState?.SetPickup(userId, new RideLocation { Latitude = lat, Longitude = lon, Address = address });

        private bool TryGetPickup(long userId, out (double lat, double lon, string addr) pickup)
        {
            pickup = default;
            if (TryGetPickupLocation(userId, out var location))
            {
                pickup = (location.Latitude, location.Longitude, location.Address);
                return true;
            }
            return false;
        }

        private bool TryGetPickupLocation(long userId, out RideLocation location)
        {
            location = null;
            return _rideState != null && _rideState.TryGetPickup(userId, out location) && location != null;
        }

        private void StoreDestination(long userId, double lat, double lon, string address)
            => _rideState?.SetDestination(userId, new RideLocation { Latitude = lat, Longitude = lon, Address = address });

        private bool TryGetDestination(long userId, out (double lat, double lon, string addr) destination)
        {
            destination = default;
            if (TryGetDestinationLocation(userId, out var location))
            {
                destination = (location.Latitude, location.Longitude, location.Address);
                return true;
            }
            return false;
        }

        private bool TryGetDestinationLocation(long userId, out RideLocation location)
        {
            location = null;
            return _rideState != null && _rideState.TryGetDestination(userId, out location) && location != null;
        }

        private void StoreScheduledTime(long userId, DateTime? scheduledTime) => _rideState?.SetScheduledTime(userId, scheduledTime);
        private DateTime? GetScheduledTime(long userId) => _rideState?.GetScheduledTime(userId);
        private void StoreVehiclePreference(long userId, string preference) => _rideState?.SetVehiclePreference(userId, preference);
        private string GetVehiclePreference(long userId) => _rideState?.GetVehiclePreference(userId);
        private void StoreStops(long userId, IEnumerable<string> stops) => _rideState?.SetStops(userId, stops);
        private IReadOnlyList<string> GetStops(long userId) => _rideState?.GetStops(userId) ?? Array.Empty<string>();

        private void StoreAvailableDrivers(long userId, IReadOnlyList<TimoDriverSummary> drivers)
        {
            if (_rideState == null) return;
            var dict = new Dictionary<string, TimoDriverSummary>();
            if (drivers != null)
            {
                foreach (var driver in drivers)
                {
                    if (!string.IsNullOrEmpty(driver.CarId))
                        dict[driver.CarId] = driver;
                }
            }
            _rideState.SetAvailableDrivers(userId, dict);
        }

        private bool TryGetDriverSummary(long userId, string driverId, out TimoDriverSummary driverSummary)
        {
            driverSummary = null;
            return _rideState != null && _rideState.TryGetDriver(userId, driverId, out driverSummary);
        }

        private void StoreSelectedDriver(long userId, string driverId) => _rideState?.SetSelectedDriver(userId, driverId);
        private void StoreLastBookingId(long userId, string bookingId) => _rideState?.SetLastBookingId(userId, bookingId);
        private string GetLastBookingId(long userId) => _rideState?.GetLastBookingId(userId);

        private int ResolvePassengerCount(long userId)
        {
            var stored = _rideState?.GetOrCreate(userId).PassengerCount;
            if (stored.HasValue && stored.Value > 0)
                return stored.Value;
            return _timoOptions?.DemoRider?.DefaultPassengers > 0 ? _timoOptions.DemoRider.DefaultPassengers : 1;
        }

        private (string fullName, string email, string phone) ResolveRiderIdentity(User telegramUser)
        {
            var fallbackName = _timoOptions?.DemoRider?.FullName ?? "Timo Telegram Rider";
            var fullName = $"{telegramUser.FirstName} {telegramUser.LastName}".Trim();
            if (string.IsNullOrWhiteSpace(fullName))
                fullName = !string.IsNullOrWhiteSpace(telegramUser.Username) ? telegramUser.Username : fallbackName;

            var email = _timoOptions?.DemoRider?.Email;
            if (string.IsNullOrWhiteSpace(email))
            {
                var slug = string.IsNullOrWhiteSpace(telegramUser.Username) ? telegramUser.Id.ToString() : telegramUser.Username;
                email = $"telegram+{slug}@timorides.local";
            }

            var phone = _timoOptions?.DemoRider?.PhoneNumber;
            if (string.IsNullOrWhiteSpace(phone))
                phone = "+27700000000";

            return (fullName, email, phone);
        }

        private async Task<bool> ResolveAndStoreLocationAsync(long userId, long chatId, string address, bool isPickup, CancellationToken cancellationToken)
        {
            var lookup = await _mapsService.GeocodeAsync(address, cancellationToken);
            if (!lookup.IsSuccess)
            {
                await _botClient.SendTextMessageAsync(chatId, $"❌ Could not resolve '{address}'. {lookup.ErrorMessage}", cancellationToken: cancellationToken);
                return false;
            }

            var location = lookup.Location;
            if (isPickup)
                StorePickup(userId, location.Latitude, location.Longitude, location.Address);
            else
                StoreDestination(userId, location.Latitude, location.Longitude, location.Address);

            return true;
        }

        #endregion

        private async Task ConfirmBookingDetailsAsync(long chatId, long userId, string pickup, string destination, CancellationToken cancellationToken)
        {
            var summaryText = "*Please confirm your booking*\n\n" +
                $"Pickup: {pickup}\n" +
                $"Destination: {destination}\n";

            var scheduledTime = GetScheduledTime(userId);
            if (scheduledTime.HasValue)
                summaryText += $"Time: {scheduledTime.Value:h:mm tt on MMM dd}\n";

            var vehiclePref = GetVehiclePreference(userId);
            if (!string.IsNullOrEmpty(vehiclePref))
                summaryText += $"Vehicle preference: {vehiclePref}\n";

            var stops = GetStops(userId);
            if (stops.Count > 0)
                summaryText += $"Via: {string.Join(", ", stops)}\n";

            summaryText += "\n*Additional options:*\n";
            summaryText += "• Reply with passenger count (e.g., '4 people')\n";
            summaryText += "• Request vehicle type (e.g., 'luxury car')\n";
            summaryText += "• Or click 'Find drivers' to continue\n";

            var keyboard = new InlineKeyboardMarkup(new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("✅ Find drivers", "confirm_yes"),
                    InlineKeyboardButton.WithCallbackData("❌ Cancel", "confirm_cancel")
                }
            });

            await _botClient.SendTextMessageAsync(chatId, summaryText, parseMode: ParseMode.Markdown, replyMarkup: keyboard, cancellationToken: cancellationToken);
            SetRideConversationState(userId, RideConversationStates.AwaitingConfirmation);
        }

        private async Task ProceedWithConfirmedBooking(long chatId, long userId, string pickup, string destination, CancellationToken cancellationToken)
        {
            var confirmText = $"*Searching for drivers...*\n\nPickup: {pickup}\nDestination: {destination}";
            await _botClient.SendTextMessageAsync(chatId, confirmText, parseMode: ParseMode.Markdown, cancellationToken: cancellationToken);
            await ShowAvailableDriversAsync(chatId, userId, cancellationToken);
        }

        private (string car, string color, string plate) GetVehicleDetails(string driverName)
        {
            var firstName = driverName.Split(' ')[0].ToLower();
            return firstName switch
            {
                "jonathan" => ("Renault Kwid", "Red", "ND 862-688"),
                "eddison" => ("VW Polo", "Silver", "ND 923-856"),
                "sipho" => ("Toyota Corolla", "Black", "NKZ 234 GP"),
                _ => ("Unknown Vehicle", "Unknown", "XXX XXX")
            };
        }

        private (int? passengers, string recommendation) ExtractPassengerInfo(string message)
        {
            var lowerMessage = message.ToLower();
            var countMatch = System.Text.RegularExpressions.Regex.Match(
                message,
                @"(?:family of|group of|with|party of)?\s*(\d+)\s*(?:people|passengers|person|pax)?",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase
            );

            if (countMatch.Success)
            {
                var count = int.Parse(countMatch.Groups[1].Value);
                if (count >= 6)
                    return (count, "You'll need an SUV or XL vehicle for 6+ passengers");
                else if (count >= 4)
                    return (count, "Standard sedans fit up to 4 passengers comfortably");
            }

            if (lowerMessage.Contains("family") || lowerMessage.Contains("group"))
                return (null, "How many passengers? This helps me find the right vehicle size.");

            return (null, null);
        }

        private List<string> GetLocationOptions(string location)
        {
            var lowerLoc = location.ToLower();

            if (lowerLoc.Contains("airport"))
            {
                if (lowerLoc.Contains("king shaka") || lowerLoc.Contains("durban"))
                    return new List<string> { "King Shaka International Airport" };
                else
                    return new List<string>
                    {
                        "King Shaka International Airport (Durban)",
                        "Virginia Airport (Durban North)"
                    };
            }

            var hotelChains = new Dictionary<string, List<string>>
            {
                ["hilton"] = new List<string>
                {
                    "Hilton Durban (12-14 Walnut Rd, Durban)",
                    "DoubleTree by Hilton (Umhlanga)",
                    "Hilton Garden Inn (Umhlanga Ridge)"
                },
                ["sheraton"] = new List<string>
                {
                    "Sheraton Durban (Marine Parade)",
                    "Four Points by Sheraton (Durban)"
                },
                ["marriott"] = new List<string>
                {
                    "Marriott Hotel Durban",
                    "Protea Hotel by Marriott (Umhlanga)"
                },
                ["southern sun"] = new List<string>
                {
                    "Southern Sun Elangeni & Maharani (Marine Parade)",
                    "Southern Sun North Beach (Durban)"
                },
                ["protea"] = new List<string>
                {
                    "Protea Hotel Edward (Marine Parade)",
                    "Protea Hotel Umhlanga"
                }
            };

            foreach (var chain in hotelChains)
            {
                if (lowerLoc.Contains(chain.Key))
                    return chain.Value;
            }

            if (lowerLoc.Contains("hotel") && lowerLoc.Split(' ').Length <= 3)
            {
                return new List<string>
                {
                    "Hilton Durban (Marine Parade)",
                    "Sheraton Durban (Marine Parade)",
                    "Southern Sun Elangeni (Marine Parade)",
                    "Beverly Hills Hotel (Umhlanga)"
                };
            }

            if (lowerLoc.Contains("mall"))
            {
                if (lowerLoc.Contains("gateway"))
                    return new List<string> { "Gateway Theatre of Shopping (Umhlanga)" };
                else
                    return new List<string>
                    {
                        "Gateway Theatre of Shopping (Umhlanga)",
                        "Pavilion Shopping Centre (Westville)",
                        "The Workshop Shopping Centre (City Centre)"
                    };
            }

            if (lowerLoc.Contains("beach"))
            {
                if (lowerLoc.Contains("ushaka"))
                    return new List<string> { "uShaka Beach (Marine Parade)" };
                else
                    return new List<string>
                    {
                        "uShaka Beach (Marine Parade)",
                        "Durban Beachfront (Golden Mile)",
                        "Umhlanga Beach (Umhlanga Rocks)"
                    };
            }

            var hasStreetNumber = System.Text.RegularExpressions.Regex.IsMatch(location, @"\d+");
            if (!hasStreetNumber && lowerLoc.Split(' ').Length <= 3)
                return new List<string>();

            return new List<string> { location };
        }
    }
}
