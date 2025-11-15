# TimoRides × Telegram Bot Integration Design

**Date:** October 28, 2025  
**Integration Type:** OASIS Telegram Bot Extension  
**Status:** Design Proposal

---

## Executive Summary

Leverage your existing **OASIS Telegram Bot** infrastructure to enable ride booking through Telegram. Users can book TimoRides directly in Telegram without downloading a separate app, with payments via OASIS Wallet, and reputation/trust via OASIS Karma system.

### Why This is Perfect

1. **You already built most of it!** The TelegramOASIS provider + authentication system exists
2. **South African market fit:** Telegram is popular, data-light alternative to web/native apps
3. **Unified identity:** Same OASIS Avatar for accountability groups AND ride bookings
4. **Built-in payments:** OASIS Wallet already integrated
5. **Trust system:** Leverage Karma scores for driver/rider reputation
6. **Low friction:** No app download, works on any device with Telegram

---

## Architecture Overview

### Current OASIS Telegram Bot Stack

```
┌──────────────────────────────────────────────────┐
│         Telegram Users (Existing)                │
│    • Accountability groups                       │
│    • Achievement tracking                        │
│    • NFT minting                                 │
└────────────────┬─────────────────────────────────┘
                 │ Bot commands
                 ▼
┌──────────────────────────────────────────────────┐
│      TelegramBotService (Existing)               │
│  • Handles /start, /creategroup, /checkin, etc   │
│  • Routes to command handlers                    │
└────────────────┬─────────────────────────────────┘
                 │
                 ▼
┌──────────────────────────────────────────────────┐
│      TelegramOASIS Provider (Existing)           │
│  • Links Telegram ID → OASIS Avatar              │
│  • MongoDB storage                               │
│  • Karma/Token rewards                           │
└────────────────┬─────────────────────────────────┘
                 │
        ┌────────┴────────┐
        ▼                 ▼
  ┌──────────┐      ┌──────────┐
  │  OASIS   │      │  Solana  │
  │  Core    │      │  (NFTs)  │
  └──────────┘      └──────────┘
```

### Extended Architecture (+ TimoRides)

```
┌──────────────────────────────────────────────────┐
│         Telegram Users (Extended)                │
│    • Accountability groups (existing)            │
│    • Achievement tracking (existing)             │
│    • NFT minting (existing)                      │
│    • 🚖 BOOK TIMORIDES (NEW!)                    │
└────────────────┬─────────────────────────────────┘
                 │ Bot commands
                 ▼
┌──────────────────────────────────────────────────┐
│      TelegramBotService (EXTENDED)               │
│  • Existing: /start, /creategroup, /checkin      │
│  • NEW: /bookride, /myrides, /track, /rate       │
└────────────────┬─────────────────────────────────┘
                 │
        ┌────────┴────────┐
        ▼                 ▼
┌──────────────┐    ┌──────────────────┐
│ TelegramOASIS│    │ TimoRides        │
│ Provider     │    │ Integration      │
│ (Existing)   │    │ (NEW Module)     │
└──────┬───────┘    └────────┬─────────┘
       │                     │
       │                     ▼
       │            ┌──────────────────┐
       │            │ TimoRides Backend│
       │            │ (ride-scheduler) │
       │            │  • Drivers       │
       │            │  • Bookings      │
       │            │  • Locations     │
       │            └──────────────────┘
       │
       ▼
┌──────────────┐
│  OASIS Core  │
│  • Avatar    │
│  • Karma     │
│  • Wallet    │
└──────────────┘
```

---

## User Experience Flow

### 🚀 **Flow 1: First-Time User Books Ride**

#### Step 1: Initial Setup (One-Time)
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

**Technical:**
- Creates OASIS Avatar (existing functionality)
- Links Telegram ID to Avatar ID
- Initializes OASIS Wallet (0 balance)
- Records user timezone from Telegram profile

---

#### Step 2: Request Ride
```
User: /bookride

Bot: 🚖 Book a TimoRides Ride
     
     📍 Share your pickup location:
     • Tap the 📎 attachment icon
     • Select "Location"
     • Choose "Send My Current Location"
     
     Or type an address:
     Example: 123 Main Street, Durban

User: [Shares location via Telegram's location feature]
     📍 Location: -29.8587, 31.0218 (uShaka Marine World, Durban)

Bot: ✅ Pickup location set: uShaka Marine World, Durban
     
     📍 Now share your destination:
     (Same process - send location or type address)

User: [Shares destination]
     📍 Location: -29.9247, 30.9707 (Durban King Shaka Airport)

Bot: ✅ Destination set: Durban Airport
     
     📏 Distance: 34 km
     ⏱️ Estimated time: 28 minutes
     
     🔍 Searching for available drivers...
```

**Technical:**
- Use Telegram's native location sharing (sends lat/lng)
- Reverse geocode via Google Maps API
- Calculate distance/time via TimoRides backend
- Query `/api/drivers/nearby` with pickup location

---

#### Step 3: Driver Selection (Marketplace Model)
```
Bot: 🚗 5 Drivers Available:

     [Driver Card 1]
     ┌─────────────────────────────────────┐
     │ 👤 Sipho M. ⭐ 4.9 (287 rides)      │
     │ 🚗 Toyota Corolla • Black           │
     │ 💵 R 420                            │
     │ 🏆 Karma: 850 (Trusted)             │
     │ 🗣️ English, Zulu                    │
     │ ❄️ AC • 📱 WiFi • 🎵 Music          │
     │ ⏱️ 5 min away                       │
     │                                      │
     │ [Select Driver 🚖]                   │
     └─────────────────────────────────────┘
     
     [Driver Card 2]
     ┌─────────────────────────────────────┐
     │ 👤 Thandi N. ⭐ 4.8 (156 rides)     │
     │ 🚗 VW Polo • White                  │
     │ 💵 R 380 (Best Price!)              │
     │ 🏆 Karma: 620 (Good)                │
     │ 🗣️ English, Xhosa                   │
     │ ❄️ AC                               │
     │ ⏱️ 8 min away                       │
     │                                      │
     │ [Select Driver 🚖]                   │
     └─────────────────────────────────────┘
     
     [3 more drivers...]
     
     Tap "Select Driver" to book your ride.

User: [Taps "Select Driver 🚖" on Sipho's card]
```

**Technical:**
- Display drivers as Telegram **Inline Keyboards** (interactive buttons)
- Each card is a callback button with `driver_id` payload
- Sort by: proximity, price, karma score (customizable)
- Include driver photo (Telegram supports images in messages)
- Real-time availability from TimoRides backend

---

#### Step 4: Booking Confirmation
```
Bot: 📋 Booking Summary
     
     👤 Driver: Sipho M. ⭐ 4.9
     📍 Pickup: uShaka Marine World
     📍 Dropoff: Durban Airport
     💵 Fare: R 420
     ⏱️ ETA: 5 minutes
     
     💳 Payment Method:
     [OASIS Wallet (R 0)] 💀 Insufficient funds
     [Cash on Delivery] ✅
     [Mobile Money (M-Pesa)]
     [Card (Flutterwave)]
     
     Select payment method:

User: [Taps "Cash on Delivery"]

Bot: ✅ Booking Confirmed! 🎉
     
     🆔 Booking ID: BK-20251028-A7X9
     👤 Driver: Sipho M.
     🚗 Vehicle: Black Toyota Corolla (CA 123 XYZ)
     📱 Driver Phone: +27 82 XXX XXXX [Reveal] 🔓
     
     📍 Driver is 5 minutes away
     You can track the ride in real-time:
     /track BK-20251028-A7X9
     
     💬 Need to chat with your driver?
     /chat BK-20251028-A7X9
```

**Technical:**
- Create booking via `POST /api/bookings`
- Store booking in TimoRides MongoDB
- Link booking to Telegram user's OASIS Avatar
- Check OASIS Wallet balance for payment
- Send booking confirmation to driver (via driver app or Telegram if driver uses bot)

---

#### Step 5: Real-Time Tracking
```
User: /track BK-20251028-A7X9

Bot: 🚖 Live Ride Tracking
     
     Status: Driver En Route to Pickup
     
     📍 Driver Location: [Live Map Preview]
     (Map shows driver marker + route to pickup)
     
     ⏱️ Estimated arrival: 3 minutes
     🚗 Driver is 1.2 km away
     
     [Refresh Location 🔄]
     [Call Driver 📞]
     [Cancel Ride ❌]

[Auto-update every 10 seconds]

Bot: 📍 Location Updated (auto)
     ⏱️ Driver arriving in 1 minute!
     
Bot: 🎉 Driver has arrived!
     Please proceed to the pickup point.
     
     Your driver is waiting in a Black Toyota Corolla.
     
     [Confirm Pickup ✅]
     [Driver Not Here ❓]
```

**Technical:**
- Poll `/api/bookings/{id}/status` every 10 seconds
- Or use WebSocket for real-time updates (more efficient)
- Send Telegram location message with driver's current position
- Use Telegram's **Live Location** feature for continuous tracking
- Update message inline (edit previous message, not spam new ones)

---

#### Step 6: Ride In Progress
```
Bot: 🚗 Ride Started!
     
     Status: En Route to Destination
     ⏱️ Estimated arrival: 26 minutes
     
     [Track Live 📍]
     [Contact Driver 💬]
     
[Auto-updates as ride progresses]

Bot: 📍 You're almost there! (5 minutes away)

Bot: ✅ Ride Completed!
     
     📋 Trip Summary:
     • Distance: 34.2 km
     • Duration: 29 minutes
     • Fare: R 420 (paid in cash)
     
     💳 Payment Status: Completed ✅
     
     ⭐ Rate Your Ride:
     How was your experience with Sipho?
     
     [⭐⭐⭐⭐⭐ Excellent]
     [⭐⭐⭐⭐ Good]
     [⭐⭐⭐ Average]
     [⭐⭐ Poor]
     [⭐ Very Poor]
```

**Technical:**
- Backend marks ride as "completed" when driver ends ride
- Calculate final fare (may differ from estimate due to traffic)
- Process payment if prepaid (OASIS Wallet, card)
- If cash, mark as "paid on completion"
- Prompt for rating

---

#### Step 7: Rating & Rewards
```
User: [Taps "⭐⭐⭐⭐⭐ Excellent"]

Bot: Thank you for your rating! ⭐⭐⭐⭐⭐
     
     📝 Optional: Add a review (helps other riders)
     Type your review or tap Skip.
     
User: Great driver! Very friendly and professional. 👍

Bot: ✅ Review submitted!
     
     🎁 Ride completed! You earned:
     • 20 Karma points
     • 2 REWARD tokens
     
     ⭐ Total Karma: 20
     🪙 Total Tokens: 2.0
     
     🏆 Keep riding to unlock achievements:
     • 🥉 Bronze Rider (10 rides) - 50 karma
     • 🥈 Silver Rider (50 rides) - 200 karma
     • 🥇 Gold Rider (100 rides) - 500 karma + NFT Badge
```

**Technical:**
- Submit rating to `POST /api/bookings/{id}/rate`
- Update driver's rating in TimoRides backend
- Award karma via OASIS Karma system (existing)
- Award tokens (configurable per ride)
- Track achievement progress
- Mint NFT for milestones (reuse existing NFT service)

---

### 🔄 **Flow 2: Returning User (Faster)**

```
User: /bookride

Bot: 🚖 Quick Book
     
     📍 Pickup from your current location?
     [Yes, use my location 📍]
     [No, I'll enter manually ✍️]
     
User: [Taps "Yes"]

Bot: ✅ Pickup: [Your Current Location]
     
     📍 Choose destination:
     
     🕐 Recent Destinations:
     • Durban Airport (used 3 times)
     • Gateway Mall
     • uShaka Beach
     
     Or send a new location/address.

User: [Taps "Durban Airport"]

Bot: [Jumps straight to driver selection]
     🚗 5 Drivers Available...
```

**Technical:**
- Store ride history (pickup/dropoff pairs)
- Suggest frequent destinations
- One-tap booking for repeat routes
- Pre-fill payment method (last used)

---

## Command Reference (New TimoRides Commands)

### Ride Booking Commands

| Command | Description | Example |
|---------|-------------|---------|
| `/bookride` | Start ride booking flow | `/bookride` |
| `/quickbook [destination]` | Quick book to saved location | `/quickbook airport` |
| `/myrides` | View ride history | `/myrides` |
| `/track [booking_id]` | Track active ride | `/track BK-20251028-A7X9` |
| `/cancel [booking_id]` | Cancel upcoming ride | `/cancel BK-20251028-A7X9` |
| `/rate [booking_id] [1-5]` | Rate completed ride | `/rate BK-20251028-A7X9 5` |
| `/receipt [booking_id]` | Get ride receipt | `/receipt BK-20251028-A7X9` |
| `/favorites` | Manage favorite drivers | `/favorites` |
| `/wallet` | Check OASIS Wallet balance | `/wallet` |
| `/topup [amount]` | Add funds to wallet | `/topup 500` |

### Driver Commands (If Driver Uses Telegram Bot)

| Command | Description | Example |
|---------|-------------|---------|
| `/gooffline` | Stop receiving ride requests | `/gooffline` |
| `/goonline` | Start receiving requests | `/goonline` |
| `/earnings` | View today's earnings | `/earnings` |
| `/accept [booking_id]` | Accept ride request | `/accept BK-123` |
| `/arrived` | Mark as arrived at pickup | `/arrived` |
| `/startride` | Start the ride | `/startride` |
| `/completeride` | Complete the ride | `/completeride` |

---

## Technical Implementation

### 1. Extend TelegramBotService

**Add to `TelegramBotService.cs`:**

```csharp
// New handler for ride booking commands
private async Task HandleRideCommandAsync(Message message)
{
    var command = message.Text?.Split(' ')[0].ToLower();
    var userId = message.From.Id;
    
    // Get or create OASIS Avatar (existing functionality)
    var avatar = await GetOrCreateAvatarAsync(userId);
    
    switch (command)
    {
        case "/bookride":
            await StartRideBookingFlowAsync(message, avatar);
            break;
            
        case "/myrides":
            await ShowRideHistoryAsync(message, avatar);
            break;
            
        case "/track":
            await TrackActiveRideAsync(message, avatar);
            break;
            
        case "/wallet":
            await ShowWalletBalanceAsync(message, avatar);
            break;
            
        // ... more commands
    }
}

private async Task StartRideBookingFlowAsync(Message message, IAvatar avatar)
{
    // Step 1: Request pickup location
    await _botClient.SendTextMessageAsync(
        chatId: message.Chat.Id,
        text: "🚖 Book a TimoRides Ride\n\n" +
              "📍 Share your pickup location:\n" +
              "• Tap 📎 → Location → Send My Current Location\n" +
              "• Or type an address",
        replyMarkup: new ReplyKeyboardMarkup(new[]
        {
            KeyboardButton.WithRequestLocation("📍 Share My Location")
        })
        {
            ResizeKeyboard = true,
            OneTimeKeyboard = true
        }
    );
    
    // Store state: waiting for pickup location
    await _stateManager.SetUserStateAsync(
        userId: avatar.Id,
        state: "WAITING_PICKUP_LOCATION"
    );
}
```

---

### 2. Create TimoRidesIntegration Service

**New file: `Services/TimoRidesIntegration.cs`**

```csharp
public class TimoRidesIntegration
{
    private readonly HttpClient _httpClient;
    private readonly string _timoBackendUrl;
    
    public TimoRidesIntegration(IConfiguration config)
    {
        _timoBackendUrl = config["TimoRides:BackendUrl"]; // http://your-backend:3000/api
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(_timoBackendUrl)
        };
    }
    
    // Get nearby drivers
    public async Task<List<DriverDto>> GetNearbyDriversAsync(
        double latitude, 
        double longitude, 
        int radiusKm = 10
    )
    {
        var response = await _httpClient.GetAsync(
            $"/drivers/nearby?lat={latitude}&lng={longitude}&radius={radiusKm}"
        );
        
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<DriversResponse>();
        return result.Drivers;
    }
    
    // Create booking
    public async Task<BookingDto> CreateBookingAsync(BookingRequest request)
    {
        var response = await _httpClient.PostAsJsonAsync("/bookings", request);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<BookingResponse>();
        return result.Booking;
    }
    
    // Track ride
    public async Task<RideStatus> GetRideStatusAsync(string bookingId)
    {
        var response = await _httpClient.GetAsync($"/bookings/{bookingId}/status");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<RideStatus>();
    }
    
    // Submit rating
    public async Task SubmitRatingAsync(string bookingId, int stars, string review)
    {
        await _httpClient.PostAsJsonAsync($"/bookings/{bookingId}/rate", new
        {
            Rating = stars,
            Review = review
        });
    }
}
```

---

### 3. State Management for Multi-Step Flows

**User flow states:**

```csharp
public enum RideBookingState
{
    IDLE,
    WAITING_PICKUP_LOCATION,
    WAITING_DROPOFF_LOCATION,
    SELECTING_DRIVER,
    SELECTING_PAYMENT,
    CONFIRMING_BOOKING,
    TRACKING_RIDE,
    RATING_RIDE
}

public class UserStateManager
{
    private readonly Dictionary<long, RideBookingState> _userStates = new();
    private readonly Dictionary<long, RideBookingData> _userBookingData = new();
    
    public async Task SetUserStateAsync(long telegramUserId, RideBookingState state)
    {
        _userStates[telegramUserId] = state;
    }
    
    public async Task<RideBookingState> GetUserStateAsync(long telegramUserId)
    {
        return _userStates.GetValueOrDefault(telegramUserId, RideBookingState.IDLE);
    }
    
    public async Task StorePickupLocationAsync(long telegramUserId, Location location)
    {
        if (!_userBookingData.ContainsKey(telegramUserId))
        {
            _userBookingData[telegramUserId] = new RideBookingData();
        }
        _userBookingData[telegramUserId].PickupLocation = location;
    }
    
    // ... similar methods for dropoff, selected driver, etc.
}
```

---

### 4. Location Handling

**Process Telegram location messages:**

```csharp
private async Task HandleLocationMessageAsync(Message message)
{
    var location = message.Location;
    var userId = message.From.Id;
    var state = await _stateManager.GetUserStateAsync(userId);
    
    if (state == RideBookingState.WAITING_PICKUP_LOCATION)
    {
        // Store pickup location
        await _stateManager.StorePickupLocationAsync(userId, location);
        
        // Reverse geocode to get address
        var address = await _geocodingService.ReverseGeocodeAsync(
            location.Latitude, 
            location.Longitude
        );
        
        await _botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: $"✅ Pickup location set: {address}\n\n" +
                  "📍 Now share your destination:"
        );
        
        // Update state
        await _stateManager.SetUserStateAsync(userId, RideBookingState.WAITING_DROPOFF_LOCATION);
    }
    else if (state == RideBookingState.WAITING_DROPOFF_LOCATION)
    {
        // Store dropoff location
        await _stateManager.StoreDropoffLocationAsync(userId, location);
        
        // Get address
        var address = await _geocodingService.ReverseGeocodeAsync(
            location.Latitude, 
            location.Longitude
        );
        
        await _botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: $"✅ Destination set: {address}\n\n" +
                  "🔍 Searching for available drivers..."
        );
        
        // Fetch nearby drivers
        await ShowAvailableDriversAsync(message, userId);
    }
}
```

---

### 5. Driver Selection with Inline Keyboards

**Display drivers as interactive cards:**

```csharp
private async Task ShowAvailableDriversAsync(Message message, long userId)
{
    var bookingData = await _stateManager.GetBookingDataAsync(userId);
    var pickupLocation = bookingData.PickupLocation;
    
    // Get nearby drivers from TimoRides backend
    var drivers = await _timoRidesService.GetNearbyDriversAsync(
        pickupLocation.Latitude,
        pickupLocation.Longitude,
        radiusKm: 10
    );
    
    if (!drivers.Any())
    {
        await _botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: "😕 No drivers available in your area right now.\n" +
                  "Please try again in a few minutes."
        );
        return;
    }
    
    // Send each driver as a card with inline button
    foreach (var driver in drivers.Take(5))
    {
        var cardText = $"👤 {driver.Name} ⭐ {driver.Rating} ({driver.TotalRides} rides)\n" +
                       $"🚗 {driver.VehicleModel} • {driver.VehicleColor}\n" +
                       $"💵 R {driver.EstimatedFare}\n" +
                       $"🏆 Karma: {driver.KarmaScore} ({GetKarmaBadge(driver.KarmaScore)})\n" +
                       $"🗣️ {string.Join(", ", driver.Languages)}\n" +
                       $"{GetAmenitiesString(driver.Amenities)}\n" +
                       $"⏱️ {driver.ETAMinutes} min away";
        
        var keyboard = new InlineKeyboardMarkup(new[]
        {
            InlineKeyboardButton.WithCallbackData(
                "Select Driver 🚖", 
                $"select_driver:{driver.Id}"
            ),
            InlineKeyboardButton.WithCallbackData(
                "View Profile 👤", 
                $"driver_profile:{driver.Id}"
            )
        });
        
        // Send driver photo if available
        if (!string.IsNullOrEmpty(driver.PhotoUrl))
        {
            await _botClient.SendPhotoAsync(
                chatId: message.Chat.Id,
                photo: new InputOnlineFile(driver.PhotoUrl),
                caption: cardText,
                replyMarkup: keyboard
            );
        }
        else
        {
            await _botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: cardText,
                replyMarkup: keyboard
            );
        }
    }
    
    // Update state
    await _stateManager.SetUserStateAsync(userId, RideBookingState.SELECTING_DRIVER);
}
```

---

### 6. Handle Driver Selection (Callback Queries)

**Process inline button clicks:**

```csharp
private async Task HandleCallbackQueryAsync(CallbackQuery query)
{
    var data = query.Data; // e.g., "select_driver:driver123"
    var userId = query.From.Id;
    
    if (data.StartsWith("select_driver:"))
    {
        var driverId = data.Split(':')[1];
        
        // Store selected driver
        await _stateManager.StoreSelectedDriverAsync(userId, driverId);
        
        // Show payment options
        await ShowPaymentOptionsAsync(query.Message, userId);
        
        // Answer callback to remove loading state
        await _botClient.AnswerCallbackQueryAsync(
            callbackQueryId: query.Id,
            text: "Driver selected! ✅"
        );
    }
    else if (data.StartsWith("payment:"))
    {
        var paymentMethod = data.Split(':')[1]; // "cash", "wallet", "mpesa", "card"
        
        await ProcessPaymentSelectionAsync(query.Message, userId, paymentMethod);
        
        await _botClient.AnswerCallbackQueryAsync(
            callbackQueryId: query.Id,
            text: "Payment method selected! ✅"
        );
    }
}

private async Task ShowPaymentOptionsAsync(Message message, long userId)
{
    var bookingData = await _stateManager.GetBookingDataAsync(userId);
    var fare = bookingData.EstimatedFare;
    
    // Check OASIS Wallet balance
    var avatar = await GetAvatarAsync(userId);
    var walletBalance = await GetWalletBalanceAsync(avatar.Id);
    
    var keyboard = new InlineKeyboardMarkup(new[]
    {
        new[]
        {
            InlineKeyboardButton.WithCallbackData(
                $"OASIS Wallet (R {walletBalance}) {(walletBalance >= fare ? "✅" : "❌")}",
                "payment:wallet"
            )
        },
        new[]
        {
            InlineKeyboardButton.WithCallbackData("Cash on Delivery ✅", "payment:cash")
        },
        new[]
        {
            InlineKeyboardButton.WithCallbackData("Mobile Money (M-Pesa)", "payment:mpesa")
        },
        new[]
        {
            InlineKeyboardButton.WithCallbackData("Card (Flutterwave)", "payment:card")
        }
    });
    
    await _botClient.SendTextMessageAsync(
        chatId: message.Chat.Id,
        text: $"💳 Select Payment Method\n\nFare: R {fare}",
        replyMarkup: keyboard
    );
    
    await _stateManager.SetUserStateAsync(userId, RideBookingState.SELECTING_PAYMENT);
}
```

---

### 7. Confirm and Create Booking

**Final confirmation and API call:**

```csharp
private async Task ProcessPaymentSelectionAsync(
    Message message, 
    long userId, 
    string paymentMethod
)
{
    var bookingData = await _stateManager.GetBookingDataAsync(userId);
    bookingData.PaymentMethod = paymentMethod;
    
    // Validate OASIS Wallet if selected
    if (paymentMethod == "wallet")
    {
        var avatar = await GetAvatarAsync(userId);
        var balance = await GetWalletBalanceAsync(avatar.Id);
        
        if (balance < bookingData.EstimatedFare)
        {
            await _botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: "❌ Insufficient funds in OASIS Wallet.\n" +
                      $"Balance: R {balance}\n" +
                      $"Required: R {bookingData.EstimatedFare}\n\n" +
                      "Please select another payment method or top up your wallet:\n" +
                      "/topup 500"
            );
            return;
        }
    }
    
    // Show booking summary
    var summaryText = $"📋 Booking Summary\n\n" +
                      $"👤 Driver: {bookingData.DriverName}\n" +
                      $"📍 Pickup: {bookingData.PickupAddress}\n" +
                      $"📍 Dropoff: {bookingData.DropoffAddress}\n" +
                      $"💵 Fare: R {bookingData.EstimatedFare}\n" +
                      $"💳 Payment: {GetPaymentMethodLabel(paymentMethod)}\n" +
                      $"⏱️ ETA: {bookingData.DriverETA} minutes";
    
    var keyboard = new InlineKeyboardMarkup(new[]
    {
        new[]
        {
            InlineKeyboardButton.WithCallbackData("✅ Confirm Booking", "confirm_booking"),
            InlineKeyboardButton.WithCallbackData("❌ Cancel", "cancel_booking")
        }
    });
    
    await _botClient.SendTextMessageAsync(
        chatId: message.Chat.Id,
        text: summaryText,
        replyMarkup: keyboard
    );
}

private async Task ConfirmBookingAsync(Message message, long userId)
{
    var avatar = await GetAvatarAsync(userId);
    var bookingData = await _stateManager.GetBookingDataAsync(userId);
    
    // Create booking via TimoRides backend
    var booking = await _timoRidesService.CreateBookingAsync(new BookingRequest
    {
        UserId = avatar.Id,
        UserTelegramId = userId,
        DriverId = bookingData.SelectedDriverId,
        PickupLatitude = bookingData.PickupLocation.Latitude,
        PickupLongitude = bookingData.PickupLocation.Longitude,
        PickupAddress = bookingData.PickupAddress,
        DropoffLatitude = bookingData.DropoffLocation.Latitude,
        DropoffLongitude = bookingData.DropoffLocation.Longitude,
        DropoffAddress = bookingData.DropoffAddress,
        PaymentMethod = bookingData.PaymentMethod,
        EstimatedFare = bookingData.EstimatedFare
    });
    
    // Process payment if wallet
    if (bookingData.PaymentMethod == "wallet")
    {
        await DeductFromWalletAsync(avatar.Id, bookingData.EstimatedFare);
    }
    
    // Send confirmation
    await _botClient.SendTextMessageAsync(
        chatId: message.Chat.Id,
        text: $"✅ Booking Confirmed! 🎉\n\n" +
              $"🆔 Booking ID: {booking.Id}\n" +
              $"👤 Driver: {booking.DriverName}\n" +
              $"🚗 Vehicle: {booking.VehicleDetails}\n" +
              $"📱 Driver Phone: {booking.DriverPhone}\n\n" +
              $"📍 Driver is {booking.DriverETAMinutes} minutes away\n\n" +
              $"Track your ride: /track {booking.Id}\n" +
              $"Chat with driver: /chat {booking.Id}"
    );
    
    // Start tracking updates (background task)
    _ = StartRideTrackingAsync(userId, booking.Id);
    
    // Clear booking state
    await _stateManager.ClearBookingDataAsync(userId);
    await _stateManager.SetUserStateAsync(userId, RideBookingState.TRACKING_RIDE);
}
```

---

### 8. Real-Time Ride Tracking

**Background service that polls for updates:**

```csharp
private async Task StartRideTrackingAsync(long userId, string bookingId)
{
    var chatId = userId; // Telegram chat ID same as user ID for DMs
    
    while (true)
    {
        try
        {
            // Get ride status from backend
            var status = await _timoRidesService.GetRideStatusAsync(bookingId);
            
            // Send updates based on status changes
            if (status.Status == "driver_en_route_pickup" && !status.NotifiedEnRoute)
            {
                await _botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: $"🚗 Driver is on the way!\n\n" +
                          $"⏱️ ETA: {status.ETAMinutes} minutes\n" +
                          $"📍 Distance: {status.DistanceKm} km away\n\n" +
                          $"/track {bookingId}"
                );
                
                // Mark as notified
                await _timoRidesService.MarkNotifiedAsync(bookingId, "en_route");
            }
            else if (status.Status == "driver_arrived" && !status.NotifiedArrived)
            {
                await _botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: $"🎉 Driver has arrived!\n\n" +
                          $"Please proceed to the pickup point.\n" +
                          $"Your driver is waiting in a {status.VehicleDescription}."
                );
                
                await _timoRidesService.MarkNotifiedAsync(bookingId, "arrived");
            }
            else if (status.Status == "ride_started" && !status.NotifiedStarted)
            {
                await _botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: $"🚗 Ride Started!\n\n" +
                          $"📍 En route to destination\n" +
                          $"⏱️ Estimated arrival: {status.ETAToDestination} minutes\n\n" +
                          $"/track {bookingId}"
                );
                
                await _timoRidesService.MarkNotifiedAsync(bookingId, "started");
            }
            else if (status.Status == "completed")
            {
                await _botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: $"✅ Ride Completed!\n\n" +
                          $"📋 Trip Summary:\n" +
                          $"• Distance: {status.FinalDistanceKm} km\n" +
                          $"• Duration: {status.DurationMinutes} minutes\n" +
                          $"• Fare: R {status.FinalFare}\n\n" +
                          $"💳 Payment Status: {status.PaymentStatus}\n\n" +
                          $"⭐ Rate your ride: /rate {bookingId}"
                );
                
                // Exit tracking loop
                break;
            }
            
            // Wait 10 seconds before next check
            await Task.Delay(10000);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error tracking ride {bookingId}: {ex.Message}");
            await Task.Delay(10000);
        }
    }
}
```

---

### 9. Manual Tracking Command

**User requests current status:**

```csharp
private async Task TrackActiveRideAsync(Message message, IAvatar avatar)
{
    var parts = message.Text.Split(' ');
    if (parts.Length < 2)
    {
        await _botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: "❌ Please provide booking ID:\n/track BK-20251028-A7X9"
        );
        return;
    }
    
    var bookingId = parts[1];
    
    // Verify booking belongs to this user
    var booking = await _timoRidesService.GetBookingDetailsAsync(bookingId);
    if (booking.UserTelegramId != message.From.Id)
    {
        await _botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: "❌ Booking not found or doesn't belong to you."
        );
        return;
    }
    
    // Get current status
    var status = await _timoRidesService.GetRideStatusAsync(bookingId);
    
    // Send live location if driver en route
    if (status.Status == "driver_en_route_pickup" || status.Status == "ride_started")
    {
        // Send driver's current location
        var locationMessage = await _botClient.SendLocationAsync(
            chatId: message.Chat.Id,
            latitude: (float)status.DriverLatitude,
            longitude: (float)status.DriverLongitude,
            livePeriod: 900 // Update for 15 minutes
        );
        
        // Note: You'd need driver app to send location updates via webhook
        // for Telegram's live location to work properly
    }
    
    var statusText = status.Status switch
    {
        "driver_en_route_pickup" => $"🚗 Driver En Route to Pickup\n" +
                                     $"⏱️ ETA: {status.ETAMinutes} min\n" +
                                     $"📍 {status.DistanceKm} km away",
        "driver_arrived" => "🎉 Driver has arrived at pickup!",
        "ride_started" => $"🚗 Ride In Progress\n" +
                          $"⏱️ ETA to destination: {status.ETAToDestination} min",
        "completed" => "✅ Ride completed!\n/rate {bookingId}",
        _ => $"Status: {status.Status}"
    };
    
    var keyboard = new InlineKeyboardMarkup(new[]
    {
        new[]
        {
            InlineKeyboardButton.WithCallbackData("🔄 Refresh", $"refresh_tracking:{bookingId}"),
            InlineKeyboardButton.WithCallbackData("📞 Call Driver", $"call_driver:{bookingId}")
        },
        new[]
        {
            InlineKeyboardButton.WithCallbackData("❌ Cancel Ride", $"cancel_ride:{bookingId}")
        }
    });
    
    await _botClient.SendTextMessageAsync(
        chatId: message.Chat.Id,
        text: $"🚖 Live Ride Tracking\n\n{statusText}",
        replyMarkup: keyboard
    );
}
```

---

### 10. Rating System with Karma Rewards

**Submit rating after ride:**

```csharp
private async Task HandleRateCommandAsync(Message message, IAvatar avatar)
{
    var parts = message.Text.Split(' ');
    if (parts.Length < 3)
    {
        // Show rating UI
        await ShowRatingOptionsAsync(message, parts.Length > 1 ? parts[1] : null);
        return;
    }
    
    var bookingId = parts[1];
    var rating = int.Parse(parts[2]);
    
    if (rating < 1 || rating > 5)
    {
        await _botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: "❌ Rating must be between 1 and 5 stars."
        );
        return;
    }
    
    // Submit rating to backend
    await _timoRidesService.SubmitRatingAsync(bookingId, rating, null);
    
    // Award karma for completing ride
    var karmaAwarded = 20;
    var tokensAwarded = 2.0m;
    
    await AwardKarmaAsync(avatar.Id, karmaAwarded);
    await AwardTokensAsync(avatar.Id, tokensAwarded);
    
    await _botClient.SendTextMessageAsync(
        chatId: message.Chat.Id,
        text: $"✅ Thank you for your rating! ⭐ x{rating}\n\n" +
              $"🎁 You earned:\n" +
              $"• {karmaAwarded} Karma points\n" +
              $"• {tokensAwarded} REWARD tokens\n\n" +
              $"⭐ Total Karma: {await GetTotalKarmaAsync(avatar.Id)}\n" +
              $"🪙 Total Tokens: {await GetTotalTokensAsync(avatar.Id)}\n\n" +
              $"📝 Add a review? (helps other riders)\n" +
              $"Reply with your review or /skip"
    );
    
    // Wait for review text
    await _stateManager.SetUserStateAsync(message.From.Id, RideBookingState.RATING_RIDE);
    await _stateManager.StoreBookingIdForReviewAsync(message.From.Id, bookingId);
}

private async Task ShowRatingOptionsAsync(Message message, string bookingId)
{
    if (string.IsNullOrEmpty(bookingId))
    {
        await _botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: "❌ Please provide booking ID:\n/rate BK-20251028-A7X9"
        );
        return;
    }
    
    var keyboard = new InlineKeyboardMarkup(new[]
    {
        new[]
        {
            InlineKeyboardButton.WithCallbackData("⭐⭐⭐⭐⭐ Excellent", $"rate:{bookingId}:5")
        },
        new[]
        {
            InlineKeyboardButton.WithCallbackData("⭐⭐⭐⭐ Good", $"rate:{bookingId}:4")
        },
        new[]
        {
            InlineKeyboardButton.WithCallbackData("⭐⭐⭐ Average", $"rate:{bookingId}:3")
        },
        new[]
        {
            InlineKeyboardButton.WithCallbackData("⭐⭐ Poor", $"rate:{bookingId}:2")
        },
        new[]
        {
            InlineKeyboardButton.WithCallbackData("⭐ Very Poor", $"rate:{bookingId}:1")
        }
    });
    
    await _botClient.SendTextMessageAsync(
        chatId: message.Chat.Id,
        text: "⭐ Rate Your Ride\n\nHow was your experience?",
        replyMarkup: keyboard
    );
}
```

---

## Configuration Setup

### OASIS_DNA.json Updates

```json
{
  "TelegramOASIS": {
    "BotToken": "YOUR_BOT_TOKEN",
    "WebhookUrl": "https://your-domain.com/api/telegram/webhook",
    "MongoConnectionString": "mongodb+srv://...",
    "TreasuryWalletAddress": "YOUR_SOLANA_WALLET",
    "RewardTokenMintAddress": "TOKEN_MINT_ADDRESS",
    "RewardTokenSymbol": "REWARD",
    "SolanaCluster": "devnet",
    
    "TimoRides": {
      "Enabled": true,
      "BackendUrl": "http://timorides-backend:3000/api",
      "GoogleMapsApiKey": "YOUR_GOOGLE_MAPS_KEY",
      "DefaultCurrency": "ZAR",
      "DefaultRadius": 10,
      "KarmaPerRide": 20,
      "TokensPerRide": 2.0,
      "RideTrackingIntervalSeconds": 10
    }
  }
}
```

---

## Database Schema Extensions

### TelegramAvatar (Extend Existing)

```javascript
{
  id: "unique_id",
  telegramId: 123456789,
  telegramUsername: "@john_doe",
  firstName: "John",
  lastName: "Doe",
  oasisAvatarId: "uuid-of-oasis-avatar",
  groupIds: ["group1", "group2"],
  
  // NEW for TimoRides:
  timoRides: {
    totalRides: 15,
    totalSpent: 5280.50, // ZAR
    favoriteDriverIds: ["driver1", "driver2"],
    savedLocations: [
      {
        label: "Home",
        address: "123 Main St, Durban",
        latitude: -29.8587,
        longitude: 31.0218
      },
      {
        label: "Work",
        address: "Gateway Mall, Umhlanga",
        latitude: -29.7296,
        longitude: 31.0683
      }
    ],
    paymentMethods: [
      {
        type: "wallet", // oasis_wallet, cash, mpesa, card
        default: true
      }
    ],
    ridePreferences: {
      autoShareLocation: true,
      notifyOnDriverArrival: true,
      preferredLanguages: ["English", "Zulu"]
    }
  },
  
  lastInteractionAt: "2025-10-28T10:00:00Z"
}
```

### TelegramRideBooking (New Collection)

```javascript
{
  id: "booking_id",
  bookingId: "BK-20251028-A7X9", // From TimoRides backend
  telegramUserId: 123456789,
  oasisAvatarId: "uuid",
  driverId: "driver123",
  
  pickup: {
    latitude: -29.8587,
    longitude: 31.0218,
    address: "uShaka Marine World, Durban"
  },
  
  dropoff: {
    latitude: -29.9247,
    longitude: 30.9707,
    address: "King Shaka Airport, Durban"
  },
  
  fare: {
    estimated: 420,
    final: 435,
    currency: "ZAR"
  },
  
  paymentMethod: "cash", // wallet, cash, mpesa, card
  paymentStatus: "completed",
  
  status: "completed", // pending, driver_en_route_pickup, driver_arrived, ride_started, completed, cancelled
  
  timestamps: {
    requested: "2025-10-28T10:00:00Z",
    driverAccepted: "2025-10-28T10:01:30Z",
    driverArrived: "2025-10-28T10:05:00Z",
    rideStarted: "2025-10-28T10:07:00Z",
    rideCompleted: "2025-10-28T10:35:00Z"
  },
  
  rating: {
    stars: 5,
    review: "Great driver! Very professional.",
    ratedAt: "2025-10-28T10:40:00Z"
  },
  
  rewards: {
    karmaAwarded: 20,
    tokensAwarded: 2.0
  },
  
  notifications: {
    sentEnRoute: true,
    sentArrived: true,
    sentStarted: true,
    sentCompleted: true
  }
}
```

---

## Benefits of Telegram Integration

### 1. **Low Barrier to Entry** 🚀
- No app download required
- Works on any device with Telegram
- Lower data usage than native app
- Perfect for South African market (data costs)

### 2. **Unified Identity** 🔐
- Same OASIS Avatar for accountability groups AND rides
- Portable reputation across platforms
- Single login (Telegram account)

### 3. **Built-In Payments** 💰
- OASIS Wallet integration (crypto)
- Fallback to cash, mobile money, cards
- No need for separate payment setup

### 4. **Trust & Safety** 🛡️
- Karma scores for riders AND drivers
- On-chain reputation (can't be faked)
- Transparent rating history

### 5. **Social Proof** 👥
- Share ride achievements in groups
- Refer friends via Telegram
- Group ride bookings (split fares)

### 6. **Rewards System** 🎁
- Earn karma + tokens for each ride
- Milestone NFTs (Bronze/Silver/Gold Rider)
- Gamification increases engagement

### 7. **Offline-First** 📶
- Book ride when online
- Receive updates even with poor connection
- Telegram's offline capabilities

### 8. **Driver Onboarding** 🚗
- Drivers can also use Telegram bot
- Accept rides, update status, get paid
- No separate driver app needed (initially)

---

## Roadmap

### Phase 1: MVP (4-6 weeks)
- [ ] Extend TelegramBotService with ride commands
- [ ] Integrate with TimoRides backend API
- [ ] Basic booking flow (pickup → destination → driver selection → confirm)
- [ ] Real-time tracking (polling-based)
- [ ] Rating system with karma rewards
- [ ] Cash-only payments

### Phase 2: Enhanced Features (4-6 weeks)
- [ ] OASIS Wallet payments
- [ ] Mobile money integration (M-Pesa)
- [ ] Saved locations (Home, Work, etc.)
- [ ] Favorite drivers
- [ ] Ride scheduling (book for later)
- [ ] Group rides (split fare among Telegram group)

### Phase 3: Advanced (6-8 weeks)
- [ ] WebSocket for real-time tracking (replace polling)
- [ ] Driver-side Telegram bot
- [ ] Live location sharing
- [ ] In-bot chat with driver
- [ ] NFT ride badges (milestone achievements)
- [ ] Referral system (invite friends via Telegram)
- [ ] Promo codes

### Phase 4: Scale (8+ weeks)
- [ ] Multi-city support
- [ ] Dynamic pricing
- [ ] Ride pooling (share ride with strangers)
- [ ] Corporate accounts
- [ ] Analytics dashboard
- [ ] Admin panel for support

---

## Cost & Effort Estimates

### Development Time (1 Full-Stack Developer)

| Phase | Features | Time | Cost @ $65/hr |
|-------|----------|------|---------------|
| Phase 1 | MVP ride booking | 4-6 weeks (160-240 hrs) | $10,400 - $15,600 |
| Phase 2 | Enhanced features | 4-6 weeks (160-240 hrs) | $10,400 - $15,600 |
| Phase 3 | Advanced features | 6-8 weeks (240-320 hrs) | $15,600 - $20,800 |
| Phase 4 | Scale & optimize | 8+ weeks (320+ hrs) | $20,800+ |
| **Total** | **Full system** | **22-28 weeks** | **$57,200 - $72,800** |

### Infrastructure Costs

| Service | Cost | Notes |
|---------|------|-------|
| Telegram Bot API | **FREE** | No cost |
| OASIS API Hosting | Existing | Already running |
| TimoRides Backend | Existing | Already built |
| MongoDB | $0-50/mo | Atlas free tier or small cluster |
| Google Maps API | ~$200/mo | 100K requests = $200 |
| SMS (OTP) | ~$0.01/SMS | For driver notifications |
| **Total** | **~$250-300/mo** | Very affordable! |

---

## Success Metrics

### User Adoption
- **Target:** 1,000 Telegram users in first 3 months
- **Metric:** Daily Active Users (DAU)
- **Goal:** 20% DAU/MAU ratio (200 daily users)

### Engagement
- **Target:** 5,000 rides booked in first 6 months
- **Metric:** Rides per user per month
- **Goal:** 5+ rides/user/month (engaged users)

### Retention
- **Target:** 60% month-over-month retention
- **Metric:** Users who book 2+ rides/month
- **Goal:** 60% return rate after first ride

### Revenue
- **Target:** R 500,000 Gross Booking Value (GBV) in 6 months
- **Metric:** Average ride value × total rides
- **Goal:** R 100 average × 5,000 rides = R 500K GBV

### Trust & Safety
- **Target:** 4.5+ average driver rating
- **Metric:** Star rating distribution
- **Goal:** 80%+ rides rated 4-5 stars

---

## Competitive Advantages

### vs. Uber/Bolt (Native Apps)

| Feature | Uber/Bolt | TimoRides Telegram | Winner |
|---------|-----------|-------------------|--------|
| **Download Required** | Yes | No ✅ | Timo |
| **Data Usage** | High | Low ✅ | Timo |
| **Works Offline** | No | Yes ✅ | Timo |
| **Payment Options** | Card required | Cash, mobile money ✅ | Timo |
| **Driver Choice** | Algorithmic | User chooses ✅ | Timo |
| **Trust System** | Basic ratings | Karma + on-chain ✅ | Timo |
| **Rewards** | Uber Cash | Crypto tokens ✅ | Timo |
| **Cross-Platform** | Separate apps | Single Telegram ✅ | Timo |

---

## Risk Mitigation

### Technical Risks

**Risk:** Telegram rate limits  
**Mitigation:** Queue messages, use webhooks instead of polling

**Risk:** Google Maps API costs  
**Mitigation:** Cache common routes, use open alternatives (OpenStreetMap)

**Risk:** Real-time tracking latency  
**Mitigation:** Use WebSockets, optimize polling intervals

### Business Risks

**Risk:** Low driver adoption  
**Mitigation:** Hybrid approach - drivers use Android app OR Telegram

**Risk:** Payment fraud  
**Mitigation:** Cash-first, OASIS Wallet has built-in fraud detection

**Risk:** User confusion (too many commands)  
**Mitigation:** Guided flows, inline keyboards, visual aids

---

## Next Steps

### Immediate (This Week)
1. Review this design with team
2. Prioritize Phase 1 features
3. Set up development environment
4. Create feature branch in OASIS repo

### Short-Term (Next 2 Weeks)
1. Extend TelegramBotService with `/bookride` command
2. Implement state management for multi-step flows
3. Connect to TimoRides backend API
4. Test basic booking flow (no driver integration yet)

### Medium-Term (Next Month)
1. Complete Phase 1 MVP
2. Internal testing with team members
3. Beta test with 10-20 early adopters
4. Iterate based on feedback

### Long-Term (3-6 Months)
1. Public launch in Durban
2. Onboard 50 drivers
3. Acquire 1,000 riders
4. Expand to Cape Town and Johannesburg

---

## Conclusion

**TimoRides + Telegram = Perfect Match**

You've already built the hardest parts:
- ✅ TelegramOASIS provider with authentication
- ✅ OASIS Avatar unified identity
- ✅ Karma and token reward system
- ✅ NFT minting on Solana
- ✅ MongoDB data storage
- ✅ TimoRides backend API

**All you need to do is connect them!**

This integration gives you:
1. **Zero download friction** - users already have Telegram
2. **Built-in payments** - OASIS Wallet + mobile money
3. **Trust system** - Karma scores + on-chain reputation
4. **Rewards** - Gamification increases engagement
5. **Low costs** - Telegram API is free, minimal infrastructure

**Estimated Time:** 4-6 weeks for working MVP  
**Estimated Cost:** $10-15K development + $300/mo infrastructure  
**Market Fit:** Excellent for South Africa (data costs, mobile money, Telegram popularity)

---

**Questions? Ready to build?** Let's start with Phase 1! 🚀







