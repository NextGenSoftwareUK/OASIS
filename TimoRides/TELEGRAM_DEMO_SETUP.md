# TimoRides Telegram Bot - Demo Setup (No Backend Required)

**Perfect for presentations and testing the UX flow!**

## 🎯 What This Demo Shows

- Full booking conversation flow
- Location sharing UX
- Driver selection cards with inline buttons
- Payment method selection
- Booking confirmation
- All with **mocked data** (no backend connection needed)

---

## 🚀 Quick Setup (5 Minutes)

### Step 1: Create Mock API Service

Create this file to replace the real API calls with fake data:

**File: `TimoRides/MockTimoRidesApiService.cs`**

```csharp
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Providers.TelegramOASIS.Models.TimoRides;

namespace NextGenSoftware.OASIS.API.Providers.TelegramOASIS.Services
{
    /// <summary>
    /// Mock TimoRides API Service for DEMO purposes
    /// Returns fake data so you can demo the bot without backend connection
    /// </summary>
    public class MockTimoRidesApiService
    {
        public async Task<List<DriverDto>> GetNearbyDriversAsync(
            double latitude, 
            double longitude, 
            int radiusKm = 10)
        {
            // Simulate API delay
            await Task.Delay(1000);
            
            // Return fake drivers
            return new List<DriverDto>
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
                    PhoneNumber = "+27821234567"
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
                    PhoneNumber = "+27821234568"
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
                    PhoneNumber = "+27821234569"
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
                    PhoneNumber = "+27821234570"
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
                    PhoneNumber = "+27821234571"
                }
            };
        }
        
        public async Task<DriverDto> GetDriverDetailsAsync(string driverId)
        {
            // Simulate API delay
            await Task.Delay(500);
            
            // Return the same fake drivers
            var drivers = await GetNearbyDriversAsync(0, 0);
            return drivers.Find(d => d.Id == driverId) ?? drivers[0];
        }
        
        public async Task<BookingResponse> CreateBookingAsync(BookingRequest request)
        {
            // Simulate API delay
            await Task.Delay(1500);
            
            // Generate fake booking ID
            var bookingId = $"BK-{DateTime.Now:yyyyMMdd}-{Guid.NewGuid().ToString().Substring(0, 4).ToUpper()}";
            
            // Get driver details
            var driver = await GetDriverDetailsAsync(request.DriverId);
            
            return new BookingResponse
            {
                Success = true,
                Booking = new BookingDto
                {
                    BookingId = bookingId,
                    UserId = request.UserId,
                    DriverId = request.DriverId,
                    DriverName = driver.GetFullName(),
                    VehicleDetails = $"{driver.VehicleColor} {driver.VehicleModel} ({driver.VehiclePlateNumber})",
                    DriverPhone = driver.PhoneNumber,
                    DriverETAMinutes = driver.ETAMinutes,
                    Status = "pending",
                    PickupAddress = request.PickupAddress,
                    DropoffAddress = request.DropoffAddress,
                    EstimatedFare = driver.EstimatedFare,
                    FinalFare = driver.EstimatedFare,
                    PaymentMethod = request.PaymentMethod,
                    Currency = request.Currency,
                    CreatedAt = DateTime.UtcNow
                }
            };
        }
        
        public async Task<RideStatus> GetRideStatusAsync(string bookingId)
        {
            // Simulate API delay
            await Task.Delay(500);
            
            // Return fake status
            return new RideStatus
            {
                BookingId = bookingId,
                Status = "driver_en_route_pickup",
                DriverLatitude = -29.8587,
                DriverLongitude = 31.0218,
                ETAMinutes = 5,
                DistanceKm = 2.3,
                VehicleDescription = "Black Toyota Corolla",
                DriverPhone = "+27821234567",
                ETAToDestination = 0,
                FinalDistanceKm = 0,
                DurationMinutes = 0,
                FinalFare = 0,
                PaymentStatus = "pending"
            };
        }
        
        public async Task SubmitRatingAsync(string bookingId, int stars, string review)
        {
            // Simulate API delay
            await Task.Delay(500);
            
            // In demo mode, we just log it
            Console.WriteLine($"Rating submitted: {bookingId} - {stars} stars - {review}");
        }
    }
}
```

---

### Step 2: Configure for Demo Mode

Update your configuration to use demo mode:

**File: `OASIS_DNA.json` (or your config file)**

```json
{
  "TelegramOASIS": {
    "BotToken": "YOUR_BOT_TOKEN_HERE",
    "WebhookUrl": "",
    "MongoConnectionString": "mongodb://localhost:27017",
    
    "TimoRides": {
      "Enabled": true,
      "DemoMode": true,  // ← ADD THIS!
      "BackendUrl": "http://localhost:4205/api",
      "GoogleMapsApiKey": "YOUR_KEY_OR_LEAVE_EMPTY_FOR_DEMO",
      "DefaultCurrency": "ZAR",
      "DefaultCurrencySymbol": "R",
      "DefaultRadius": 10,
      "MaxDriversToShow": 5,
      "KarmaPerRide": 20,
      "TokensPerRide": 2.0,
      "RideTrackingIntervalSeconds": 10
    }
  }
}
```

---

### Step 3: Use Mock Service in Startup

**In your `Startup.cs` or `Program.cs`:**

```csharp
// Check if demo mode is enabled
var demoMode = Configuration.GetValue<bool>("TelegramOASIS:TimoRides:DemoMode", false);

if (demoMode)
{
    // Use mock service for demo
    services.AddSingleton<TimoRidesApiService>(sp => 
        new MockTimoRidesApiService() as TimoRidesApiService);
    
    Console.WriteLine("⚠️ TimoRides running in DEMO MODE with mock data");
}
else
{
    // Use real service
    services.AddSingleton<TimoRidesApiService>();
}

// Always register these
services.AddSingleton<RideBookingStateManager>();
services.AddSingleton<GoogleMapsService>();
```

**Or simpler approach - just swap the service:**

```csharp
// Comment out real service:
// services.AddSingleton<TimoRidesApiService>();

// Add mock service:
services.AddSingleton<TimoRidesApiService, MockTimoRidesApiService>();
```

---

## 🎬 Demo Script

### What to Show in Your Presentation

#### 1. **Start the Bot**
```
User: /start

Bot: 🎉 Welcome to OASIS!
     Your account has been created and linked to @john_doe.
     
     You can now:
     • Join accountability groups (/creategroup)
     • Track achievements (/setgoal)
     • 🚖 Book rides with TimoRides (/bookride)
     
     Type /help to see all commands.
```

---

#### 2. **Begin Booking Flow**
```
User: /bookride

Bot: 🚖 Book a TimoRides Ride

     📍 Step 1: Share your pickup location
     
     • Tap the button below to share your current location
     • Or type an address
     
     [📍 Share My Current Location] (button)
```

**Demo Tip:** You can either:
- Share your actual location (Telegram will send real GPS coords)
- Type "uShaka Beach, Durban" to test address input

---

#### 3. **Share Pickup Location**
```
User: [Shares location via Telegram]

Bot: 🔍 Processing location...

Bot: ✅ Pickup location set:
     uShaka Marine World, Durban
     
     📍 Step 2: Share your destination
     
     • Tap the button to share destination location
     • Or type a destination address
     
     [📍 Share Destination] (button)
```

---

#### 4. **Share Destination**
```
User: [Shares destination]

Bot: 🔍 Processing destination...

Bot: ✅ Destination set:
     King Shaka Airport, Durban
     
     📏 Distance: 34.2 km
     ⏱️ Estimated time: 28 minutes
     
     🔍 Searching for available drivers...
```

---

#### 5. **Driver Cards Appear**
```
Bot: 🚗 5 Drivers Available
     
     Tap a driver to see details and book:

[Driver Card 1 with photo]
┌─────────────────────────────────────┐
│ 👤 Sipho Mkhize ⭐⭐⭐⭐⭐ 4.9 (287 rides)
│ 🚗 Toyota Corolla • Black
│ 💵 R 420
│ 🏆 Karma: 850 (Trusted)
│ 🗣️ English, Zulu
│ ❄️ AC • 📱 WiFi • 🎵 Music
│ ⏱️ 5 min away
│
│ [🚖 Select Driver]  [👤 View Profile]
└─────────────────────────────────────┘

[Driver Card 2]
┌─────────────────────────────────────┐
│ 👤 Thandi Nkosi ⭐⭐⭐⭐½ 4.8 (156 rides)
│ 🚗 VW Polo • White
│ 💵 R 380 (Best Price!)
│ 🏆 Karma: 620 (Good)
│ 🗣️ English, Xhosa
│ ❄️ AC
│ ⏱️ 8 min away
│
│ [🚖 Select Driver]  [👤 View Profile]
└─────────────────────────────────────┘

[3 more driver cards...]
```

**Demo Tip:** The photos are from pravatar.cc, so they'll show different avatars!

---

#### 6. **Select Driver**
```
User: [Taps "🚖 Select Driver" on Sipho's card]

Bot: 💳 Select Payment Method
     
     👤 Driver: Sipho Mkhize
     💵 Fare: R 420
     
     Choose how you'd like to pay:
     
     [💳 OASIS Wallet (R 500) ✅]
     [💵 Cash on Delivery ✅]
     [📱 Mobile Money (M-Pesa)]
     [💳 Card (Flutterwave)]
```

---

#### 7. **Select Payment**
```
User: [Taps "💵 Cash on Delivery"]

Bot: 📋 Booking Summary
     
     👤 Driver: Sipho Mkhize ⭐ 4.9
     🚗 Vehicle: Toyota Corolla (Black)
     
     📍 Pickup:
     uShaka Marine World, Durban
     
     📍 Dropoff:
     King Shaka Airport, Durban
     
     💵 Fare: R 420
     💳 Payment: Cash on Delivery
     ⏱️ ETA: 5 minutes
     
     Confirm to book this ride?
     
     [✅ Confirm Booking]  [❌ Cancel]
```

---

#### 8. **Confirm Booking**
```
User: [Taps "✅ Confirm Booking"]

Bot: 🔄 Creating your booking...

Bot: ✅ Booking Confirmed! 🎉
     
     🆔 Booking ID: BK-20251105-A7X9
     
     👤 Driver: Sipho Mkhize
     🚗 Vehicle: Black Toyota Corolla (CA 123 GP)
     📱 Driver Phone: +27821234567
     
     📍 Driver is 5 minutes away
     
     Track your ride:
     /track BK-20251105-A7X9
     
     You'll receive updates as your driver approaches.
```

---

#### 9. **Automatic Updates (Optional)**
```
[10 seconds later, automatically]

Bot: 🚗 Driver is on the way!
     
     ⏱️ ETA: 5 minutes
     📍 Distance: 2.3 km away
     
     /track BK-20251105-A7X9

[After driver "arrives"]

Bot: 🎉 Driver has arrived!
     
     Please proceed to the pickup point.
     Your driver is waiting in a Black Toyota Corolla.
     
     📱 Call driver: +27821234567
```

---

#### 10. **Manual Tracking**
```
User: /track BK-20251105-A7X9

Bot: 🚖 Live Ride Tracking
     
     🚗 Driver En Route to Pickup
     ⏱️ ETA: 5 min
     📍 2.3 km away
     
     [🔄 Refresh]  [📞 Call Driver]
     [❌ Cancel Ride]
```

---

## 🎨 Presentation Tips

### 1. **Show the Flow, Not the Code**
- People want to see **what it looks like**, not how it works
- Focus on the UX and conversation experience
- Highlight the **choice-first** marketplace model

### 2. **Emphasize Key Features**
- ✨ **No app download** - works in Telegram
- ✨ **Driver selection** - users choose, not algorithm
- ✨ **Multiple payment options** - cash, wallet, mobile money
- ✨ **Real-time updates** - automated notifications
- ✨ **Low data usage** - perfect for South Africa

### 3. **Compare to Uber**
| Feature | Uber | TimoRides Telegram |
|---------|------|-------------------|
| Download app | ✅ Required | ❌ Not needed |
| Data usage | High | Low |
| Driver choice | Algorithm | User selects |
| Offline capability | No | Yes (partial) |
| Payment options | Card only | Cash, wallet, mobile money, card |

### 4. **Demo Pro Tips**
- Use your phone to actually share location (more impressive)
- Have the bot open on a projector/screen share
- Walk through 2-3 drivers to show variety
- Show the driver profile button (extra details)
- Mention the karma/rewards at the end

---

## 🧪 Testing Before Your Demo

Run through this checklist:

### Quick Test Flow (2 minutes)
```bash
# 1. Start your OASIS API
cd /Volumes/Storage/OASIS_CLEAN
dotnet run --project NextGenSoftware.OASIS.API.ONODE.WebAPI

# 2. Open Telegram on your phone
# 3. Search for your bot (@YourBotName)
# 4. Send: /start
# 5. Send: /bookride
# 6. Share location (any location)
# 7. Share destination (any location)
# 8. Select a driver
# 9. Select payment method
# 10. Confirm booking
```

### Expected Result
- All messages should appear within 1-2 seconds
- Driver cards should show with photos
- Booking confirmation should show a booking ID like `BK-20251105-A7X9`

---

## 🐛 Troubleshooting

### Bot doesn't respond
```bash
# Check if bot is running
curl http://localhost:5000/health

# Check logs
tail -f logs/oasis.log
```

### Drivers not showing
- Check that `DemoMode: true` in config
- Verify `MockTimoRidesApiService` is registered
- Look for errors in console output

### Location not processing
- Google Maps API key can be empty for demo (falls back to coordinates)
- Check that location permissions are enabled in Telegram

---

## 📊 What You CAN'T Demo (Without Backend)

These features require the actual backend:
- ❌ Real-time driver tracking (live location updates)
- ❌ Ride history from database
- ❌ Actual payment processing
- ❌ Driver app integration

But for a **UX demo**, you have everything you need! 🎉

---

## 💡 Next Steps After Demo

If people are impressed (and they will be!), you can show:

1. **The backend is already built** - point to `ride-scheduler-be/`
2. **Just need to connect** - remove `DemoMode` flag
3. **Timeline**: 1-2 days to integrate with real backend
4. **Android app** - show the beautiful UI (mention it needs API work)
5. **Roadmap** - PathPulse integration, OASIS features, expansion

---

## 🎉 You're Ready!

You now have a **fully demoable Telegram bot** that shows:
- ✅ Complete booking flow
- ✅ Beautiful driver selection cards  
- ✅ Payment options
- ✅ Real-time-looking updates
- ✅ Professional UX

**No backend connection required!**

Perfect for:
- Investor presentations
- Partner demos (like with Timo team)
- User testing
- Trade shows
- Internal reviews

---

**Questions?** Just ask! This demo setup took 30 minutes to prepare but will save you hours of setup and gives you something impressive to show right now! 🚀


