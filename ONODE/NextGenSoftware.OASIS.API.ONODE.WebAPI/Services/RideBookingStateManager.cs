//using System;
//using System.Collections.Concurrent;
//using System.Collections.Generic;
//using System.Linq;
//using Microsoft.Extensions.Logging;

//namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services
//{
//    /// <summary>
//    /// Tracks per-chat booking progress for Telegram initiated rides.
//    /// </summary>
//    public class RideBookingStateManager
//    {
//        private readonly ConcurrentDictionary<long, RideBookingState> _states = new();
//        private readonly ILogger<RideBookingStateManager> _logger;

//        public RideBookingStateManager(ILogger<RideBookingStateManager> logger)
//        {
//            _logger = logger;
//        }

//        public RideBookingState Reset(long chatId, string initialState = null)
//        {
//            var state = new RideBookingState
//            {
//                ConversationState = initialState ?? RideConversationStates.None
//            };

//            _states.AddOrUpdate(chatId, state, (_, __) => state);
//            _logger?.LogDebug("Ride state reset for chat {ChatId}", chatId);
//            return state;
//        }

//        public void Clear(long chatId)
//        {
//            _states.TryRemove(chatId, out _);
//            _logger?.LogDebug("Ride state cleared for chat {ChatId}", chatId);
//        }

//        public RideBookingState GetOrCreate(long chatId)
//        {
//            return _states.GetOrAdd(chatId, _ =>
//            {
//                _logger?.LogDebug("Creating ride state for chat {ChatId}", chatId);
//                return new RideBookingState();
//            });
//        }

//        public bool TryGet(long chatId, out RideBookingState state) => _states.TryGetValue(chatId, out state);

//        public void SetConversationState(long chatId, string state)
//        {
//            var rideState = GetOrCreate(chatId);
//            rideState.ConversationState = state;
//            rideState.Touch();
//        }

//        public bool TryGetConversationState(long chatId, out string state)
//        {
//            if (_states.TryGetValue(chatId, out var rideState) && !string.IsNullOrEmpty(rideState.ConversationState))
//            {
//                state = rideState.ConversationState;
//                return true;
//            }

//            state = null;
//            return false;
//        }

//        public void SetPickup(long chatId, RideLocation location)
//        {
//            var rideState = GetOrCreate(chatId);
//            rideState.Pickup = location;
//            rideState.Touch();
//        }

//        public void SetDestination(long chatId, RideLocation location)
//        {
//            var rideState = GetOrCreate(chatId);
//            rideState.Destination = location;
//            rideState.Touch();
//        }

//        public bool TryGetPickup(long chatId, out RideLocation location)
//        {
//            location = null;
//            if (_states.TryGetValue(chatId, out var rideState) && rideState.Pickup != null)
//            {
//                location = rideState.Pickup;
//                return true;
//            }

//            return false;
//        }

//        public bool TryGetDestination(long chatId, out RideLocation location)
//        {
//            location = null;
//            if (_states.TryGetValue(chatId, out var rideState) && rideState.Destination != null)
//            {
//                location = rideState.Destination;
//                return true;
//            }

//            return false;
//        }

//        public RideLocation GetOrCreatePickup(long chatId)
//        {
//            var rideState = GetOrCreate(chatId);
//            if (rideState.Pickup == null)
//                rideState.Pickup = new RideLocation();

//            return rideState.Pickup;
//        }

//        public RideLocation GetOrCreateDestination(long chatId)
//        {
//            var rideState = GetOrCreate(chatId);
//            if (rideState.Destination == null)
//                rideState.Destination = new RideLocation();

//            return rideState.Destination;
//        }

//        public void SetVehiclePreference(long chatId, string preference)
//        {
//            var rideState = GetOrCreate(chatId);
//            rideState.VehiclePreference = preference;
//            rideState.Touch();
//        }

//        public string GetVehiclePreference(long chatId)
//        {
//            return _states.TryGetValue(chatId, out var rideState)
//                ? rideState.VehiclePreference
//                : null;
//        }

//        public void SetScheduledTime(long chatId, DateTime? scheduledTime)
//        {
//            var rideState = GetOrCreate(chatId);
//            rideState.ScheduledTime = scheduledTime;
//            rideState.Touch();
//        }

//        public DateTime? GetScheduledTime(long chatId)
//        {
//            return _states.TryGetValue(chatId, out var rideState)
//                ? rideState.ScheduledTime
//                : null;
//        }

//        public void SetPassengerCount(long chatId, int? passengers)
//        {
//            var rideState = GetOrCreate(chatId);
//            rideState.PassengerCount = passengers;
//            rideState.Touch();
//        }

//        public void SetStops(long chatId, IEnumerable<string> stops)
//        {
//            var rideState = GetOrCreate(chatId);
//            rideState.MultiStops = stops?.Where(s => !string.IsNullOrWhiteSpace(s)).Distinct().ToList() ?? new List<string>();
//            rideState.Touch();
//        }

//        public IReadOnlyList<string> GetStops(long chatId)
//        {
//            if (_states.TryGetValue(chatId, out var rideState) && rideState.MultiStops != null)
//            {
//                return rideState.MultiStops.AsReadOnly();
//            }

//            return Array.Empty<string>();
//        }

//        public void SetAvailableDrivers(long chatId, IReadOnlyDictionary<string, TimoDriverSummary> drivers)
//        {
//            var rideState = GetOrCreate(chatId);
//            rideState.AvailableDrivers = drivers != null
//                ? new Dictionary<string, TimoDriverSummary>(drivers)
//                : new Dictionary<string, TimoDriverSummary>();
//            rideState.Touch();
//        }

//        public bool TryGetDriver(long chatId, string driverId, out TimoDriverSummary driver)
//        {
//            driver = null;
//            return _states.TryGetValue(chatId, out var rideState)
//                   && rideState.AvailableDrivers != null
//                   && rideState.AvailableDrivers.TryGetValue(driverId, out driver);
//        }

//        public void SetSelectedDriver(long chatId, string driverId)
//        {
//            var rideState = GetOrCreate(chatId);
//            rideState.SelectedDriverId = driverId;
//            rideState.Touch();
//        }

//        public void SetLastBookingId(long chatId, string bookingId)
//        {
//            var rideState = GetOrCreate(chatId);
//            rideState.LastBookingId = bookingId;
//            rideState.Touch();
//        }

//        public string GetLastBookingId(long chatId)
//        {
//            return _states.TryGetValue(chatId, out var rideState) ? rideState.LastBookingId : null;
//        }
//    }

//    public static class RideConversationStates
//    {
//        public const string None = "NONE";
//        public const string WaitingPickup = "WAITING_PICKUP";
//        public const string WaitingDropoff = "WAITING_DROPOFF";
//        public const string ConfirmingPickup = "CONFIRMING_PICKUP";
//        public const string ConfirmingDestination = "CONFIRMING_DESTINATION";
//        public const string AwaitingConfirmation = "AWAITING_CONFIRMATION";
//        public const string SelectingDriver = "SELECTING_DRIVER";
//    }

//    public class RideBookingState
//    {
//        public string ConversationState { get; set; } = RideConversationStates.None;
//        public RideLocation Pickup { get; set; }
//        public RideLocation Destination { get; set; }
//        public string SelectedDriverId { get; set; }
//        public DateTime? ScheduledTime { get; set; }
//        public string VehiclePreference { get; set; }
//        public int? PassengerCount { get; set; }
//        public List<string> MultiStops { get; set; } = new List<string>();
//        public Dictionary<string, TimoDriverSummary> AvailableDrivers { get; set; } = new Dictionary<string, TimoDriverSummary>();
//        public string LastBookingId { get; set; }
//        public DateTime LastUpdatedUtc { get; private set; } = DateTime.UtcNow;

//        public void Touch()
//        {
//            LastUpdatedUtc = DateTime.UtcNow;
//        }
//    }

//    public class RideLocation
//    {
//        public string Address { get; set; }
//        public double Latitude { get; set; }
//        public double Longitude { get; set; }

//        public bool HasValidCoordinates => !double.IsNaN(Latitude) && !double.IsNaN(Longitude);
//    }

//    public class TimoDriverSummary
//    {
//        public string CarId { get; set; }
//        public string DriverName { get; set; }
//        public string VehicleMake { get; set; }
//        public string VehicleModel { get; set; }
//        public string VehicleColor { get; set; }
//        public string VehicleRegNumber { get; set; }
//        public double Rating { get; set; }
//        public string DistanceAway { get; set; }
//        public string DurationAway { get; set; }
//        public string Distance { get; set; }
//        public string Duration { get; set; }
//        public decimal RideAmount { get; set; }
//        public string PhotoUrl { get; set; }
//    }
//}

