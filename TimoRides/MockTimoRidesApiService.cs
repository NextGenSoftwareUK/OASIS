// Mock TimoRides API Service for DEMO purposes
// Copy this to: NextGenSoftware.OASIS.API.Providers.TelegramOASIS/Services/
// This allows you to demo the Telegram bot without backend connection

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Providers.TelegramOASIS.Models.TimoRides;
using Microsoft.Extensions.Logging;

namespace NextGenSoftware.OASIS.API.Providers.TelegramOASIS.Services
{
    /// <summary>
    /// Mock implementation of TimoRides API for demonstrations
    /// Returns realistic fake data so you can demo the bot flow
    /// </summary>
    public class MockTimoRidesApiService : TimoRidesApiService
    {
        private readonly ILogger<MockTimoRidesApiService> _logger;
        private readonly Random _random = new Random();
        
        public MockTimoRidesApiService(ILogger<MockTimoRidesApiService> logger)
        {
            _logger = logger;
            _logger.LogWarning("⚠️ Using MOCK TimoRides API - For DEMO purposes only!");
        }
        
        /// <summary>
        /// Returns fake list of drivers with realistic data
        /// </summary>
        public override async Task<List<DriverDto>> GetNearbyDriversAsync(
            double latitude, 
            double longitude, 
            int radiusKm = 10)
        {
            _logger.LogInformation($"[MOCK] Getting nearby drivers for {latitude}, {longitude}");
            
            // Simulate API delay
            await Task.Delay(1000);
            
            // Return fake drivers with South African names and realistic data
            var drivers = new List<DriverDto>
            {
                new DriverDto
                {
                    Id = "driver_001",
                    FirstName = "Sipho",
                    LastName = "Mkhize",
                    Rating = 4.9,
                    TotalRides = 287,
                    VehicleModel = "Toyota Corolla",
                    VehicleColor = "Black",
                    VehicleType = "sedan",
                    VehiclePlateNumber = "CA 123 GP",
                    EstimatedFare = 420,
                    KarmaScore = 850,
                    Languages = new List<string> { "English", "Zulu" },
                    Amenities = new List<string> { "ac", "wifi", "music" },
                    ETAMinutes = 5,
                    PhotoUrl = "https://i.pravatar.cc/150?u=sipho",
                    PhoneNumber = "+27821234567",
                    Bio = "Professional driver with 5 years experience. Always on time!"
                },
                new DriverDto
                {
                    Id = "driver_002",
                    FirstName = "Thandi",
                    LastName = "Nkosi",
                    Rating = 4.8,
                    TotalRides = 156,
                    VehicleModel = "VW Polo",
                    VehicleColor = "White",
                    VehicleType = "sedan",
                    VehiclePlateNumber = "CA 456 GP",
                    EstimatedFare = 380,
                    KarmaScore = 620,
                    Languages = new List<string> { "English", "Xhosa" },
                    Amenities = new List<string> { "ac" },
                    ETAMinutes = 8,
                    PhotoUrl = "https://i.pravatar.cc/150?u=thandi",
                    PhoneNumber = "+27821234568",
                    Bio = "Friendly and safe driver. Best prices in town!"
                },
                new DriverDto
                {
                    Id = "driver_003",
                    FirstName = "Mandla",
                    LastName = "Dlamini",
                    Rating = 4.95,
                    TotalRides = 423,
                    VehicleModel = "Mercedes C-Class",
                    VehicleColor = "Silver",
                    VehicleType = "premium",
                    VehiclePlateNumber = "CA 789 GP",
                    EstimatedFare = 650,
                    KarmaScore = 950,
                    Languages = new List<string> { "English", "Zulu", "Afrikaans" },
                    Amenities = new List<string> { "ac", "wifi", "music", "child_seat" },
                    ETAMinutes = 3,
                    PhotoUrl = "https://i.pravatar.cc/150?u=mandla",
                    PhoneNumber = "+27821234569",
                    Bio = "Premium service with luxury vehicle. Water and snacks provided."
                },
                new DriverDto
                {
                    Id = "driver_004",
                    FirstName = "Zanele",
                    LastName = "Khumalo",
                    Rating = 4.7,
                    TotalRides = 98,
                    VehicleModel = "Honda Civic",
                    VehicleColor = "Blue",
                    VehicleType = "sedan",
                    VehiclePlateNumber = "CA 321 GP",
                    EstimatedFare = 400,
                    KarmaScore = 580,
                    Languages = new List<string> { "English", "Zulu" },
                    Amenities = new List<string> { "ac", "music" },
                    ETAMinutes = 7,
                    PhotoUrl = "https://i.pravatar.cc/150?u=zanele",
                    PhoneNumber = "+27821234570",
                    Bio = "Reliable and punctual. Great music selection!"
                },
                new DriverDto
                {
                    Id = "driver_005",
                    FirstName = "Bongani",
                    LastName = "Mahlangu",
                    Rating = 4.85,
                    TotalRides = 312,
                    VehicleModel = "BMW 3 Series",
                    VehicleColor = "Black",
                    VehicleType = "premium",
                    VehiclePlateNumber = "CA 654 GP",
                    EstimatedFare = 680,
                    KarmaScore = 780,
                    Languages = new List<string> { "English", "Sotho" },
                    Amenities = new List<string> { "ac", "wifi", "music", "pet_friendly" },
                    ETAMinutes = 6,
                    PhotoUrl = "https://i.pravatar.cc/150?u=bongani",
                    PhoneNumber = "+27821234571",
                    Bio = "Pet-friendly rides! Your furry friends are welcome."
                }
            };
            
            // Randomize order and ETA slightly
            drivers = drivers.OrderBy(x => _random.Next()).ToList();
            foreach (var driver in drivers)
            {
                driver.ETAMinutes += _random.Next(-2, 3); // Vary by ±2 minutes
                if (driver.ETAMinutes < 1) driver.ETAMinutes = 1;
            }
            
            _logger.LogInformation($"[MOCK] Returning {drivers.Count} drivers");
            return drivers;
        }
        
        /// <summary>
        /// Returns detailed info for a specific driver
        /// </summary>
        public override async Task<DriverDto> GetDriverDetailsAsync(string driverId)
        {
            _logger.LogInformation($"[MOCK] Getting driver details for {driverId}");
            
            // Simulate API delay
            await Task.Delay(500);
            
            // Return from our fake list
            var drivers = await GetNearbyDriversAsync(0, 0);
            var driver = drivers.FirstOrDefault(d => d.Id == driverId);
            
            if (driver == null)
            {
                _logger.LogWarning($"[MOCK] Driver {driverId} not found, returning first driver");
                return drivers[0];
            }
            
            return driver;
        }
        
        /// <summary>
        /// Creates a fake booking and returns a realistic booking ID
        /// </summary>
        public override async Task<BookingResponse> CreateBookingAsync(BookingRequest request)
        {
            _logger.LogInformation($"[MOCK] Creating booking for driver {request.DriverId}");
            
            // Simulate API delay (creating booking takes a bit longer)
            await Task.Delay(1500);
            
            // Generate realistic booking ID
            var bookingId = $"BK-{DateTime.Now:yyyyMMdd}-{Guid.NewGuid().ToString().Substring(0, 4).ToUpper()}";
            
            // Get driver details
            var driver = await GetDriverDetailsAsync(request.DriverId);
            
            var booking = new BookingDto
            {
                BookingId = bookingId,
                UserId = request.UserId,
                UserTelegramId = request.UserTelegramId,
                DriverId = request.DriverId,
                DriverName = driver.GetFullName(),
                VehicleDetails = $"{driver.VehicleColor} {driver.VehicleModel} ({driver.VehiclePlateNumber})",
                DriverPhone = driver.PhoneNumber,
                DriverETAMinutes = driver.ETAMinutes,
                Status = "pending",
                PickupLatitude = request.PickupLatitude,
                PickupLongitude = request.PickupLongitude,
                PickupAddress = request.PickupAddress,
                DropoffLatitude = request.DropoffLatitude,
                DropoffLongitude = request.DropoffLongitude,
                DropoffAddress = request.DropoffAddress,
                EstimatedFare = driver.EstimatedFare,
                FinalFare = driver.EstimatedFare,
                PaymentMethod = request.PaymentMethod,
                PaymentStatus = request.PaymentMethod == "wallet" ? "paid" : "pending",
                Currency = request.Currency,
                CreatedAt = DateTime.UtcNow
            };
            
            _logger.LogInformation($"[MOCK] Booking created: {bookingId}");
            
            return new BookingResponse
            {
                Success = true,
                Message = "Booking created successfully",
                Booking = booking
            };
        }
        
        /// <summary>
        /// Returns fake ride status with realistic updates
        /// </summary>
        public override async Task<RideStatus> GetRideStatusAsync(string bookingId)
        {
            _logger.LogInformation($"[MOCK] Getting ride status for {bookingId}");
            
            // Simulate API delay
            await Task.Delay(500);
            
            // Simulate different statuses based on time
            var minutesSinceCreation = _random.Next(0, 30);
            
            string status;
            int etaMinutes;
            double distanceKm;
            
            if (minutesSinceCreation < 5)
            {
                status = "driver_en_route_pickup";
                etaMinutes = 5 - minutesSinceCreation;
                distanceKm = 2.3 - (minutesSinceCreation * 0.4);
            }
            else if (minutesSinceCreation < 7)
            {
                status = "driver_arrived";
                etaMinutes = 0;
                distanceKm = 0;
            }
            else if (minutesSinceCreation < 25)
            {
                status = "ride_started";
                etaMinutes = 25 - minutesSinceCreation;
                distanceKm = 34.2 - ((minutesSinceCreation - 7) * 1.9);
            }
            else
            {
                status = "completed";
                etaMinutes = 0;
                distanceKm = 0;
            }
            
            return new RideStatus
            {
                BookingId = bookingId,
                Status = status,
                DriverLatitude = -29.8587 + (_random.NextDouble() * 0.01),
                DriverLongitude = 31.0218 + (_random.NextDouble() * 0.01),
                ETAMinutes = etaMinutes,
                DistanceKm = Math.Max(0, distanceKm),
                VehicleDescription = "Black Toyota Corolla",
                DriverPhone = "+27821234567",
                ETAToDestination = status == "ride_started" ? etaMinutes : 0,
                FinalDistanceKm = status == "completed" ? 34.2 : 0,
                DurationMinutes = status == "completed" ? 28 : 0,
                FinalFare = status == "completed" ? 420 : 0,
                PaymentStatus = status == "completed" ? "completed" : "pending"
            };
        }
        
        /// <summary>
        /// Simulates submitting a rating
        /// </summary>
        public override async Task SubmitRatingAsync(string bookingId, int stars, string review)
        {
            _logger.LogInformation($"[MOCK] Submitting rating for {bookingId}: {stars} stars");
            
            // Simulate API delay
            await Task.Delay(500);
            
            _logger.LogInformation($"[MOCK] Rating submitted - Review: {review ?? "(no review)"}");
            
            // In demo mode, we just log it
            // In real mode, this would update the driver's rating in the database
        }
        
        /// <summary>
        /// Returns fake ride history
        /// </summary>
        public override async Task<List<BookingDto>> GetRideHistoryAsync(string userId, int limit = 10)
        {
            _logger.LogInformation($"[MOCK] Getting ride history for user {userId}");
            
            // Simulate API delay
            await Task.Delay(500);
            
            // Generate some fake historical rides
            var history = new List<BookingDto>();
            
            var driverNames = new[] { "Sipho Mkhize", "Thandi Nkosi", "Mandla Dlamini" };
            var statuses = new[] { "completed", "completed", "completed", "cancelled" };
            
            for (int i = 0; i < Math.Min(limit, 5); i++)
            {
                var date = DateTime.UtcNow.AddDays(-i * 3);
                var status = i < 3 ? "completed" : statuses[_random.Next(statuses.Length)];
                
                history.Add(new BookingDto
                {
                    BookingId = $"BK-{date:yyyyMMdd}-{Guid.NewGuid().ToString().Substring(0, 4).ToUpper()}",
                    UserId = userId,
                    DriverName = driverNames[_random.Next(driverNames.Length)],
                    VehicleDetails = "Black Toyota Corolla (CA 123 GP)",
                    Status = status,
                    PickupAddress = "uShaka Marine World, Durban",
                    DropoffAddress = "Gateway Mall, Umhlanga",
                    FinalFare = 300 + _random.Next(100, 400),
                    Currency = "ZAR",
                    PaymentMethod = "cash",
                    PaymentStatus = status == "completed" ? "completed" : "refunded",
                    CreatedAt = date,
                    CompletedAt = status == "completed" ? date.AddMinutes(25) : null
                });
            }
            
            _logger.LogInformation($"[MOCK] Returning {history.Count} historical rides");
            return history;
        }
    }
}


