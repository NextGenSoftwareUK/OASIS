# TimoRides Telegram Bot - Quick Start Guide

**Ready to implement? Follow these steps to get started in < 30 minutes.**

---

## ⚡ Quick Implementation (15-30 minutes)

### Step 1: Create Feature Branch (2 min)

```bash
cd /Volumes/Storage/OASIS_CLEAN
git checkout -b feature/telegram-ride-booking
```

---

### Step 2: Add Configuration (3 min)

**File:** `NextGenSoftware.OASIS.API.ONODE.WebAPI/OASIS_DNA.json`

Add this section to your existing `TelegramOASIS` configuration:

```json
"TimoRides": {
  "Enabled": true,
  "BackendUrl": "http://localhost:3000/api",
  "GoogleMapsApiKey": "YOUR_KEY_HERE",
  "DefaultCurrency": "ZAR",
  "DefaultCurrencySymbol": "R",
  "DefaultRadius": 10,
  "KarmaPerRide": 20,
  "TokensPerRide": 2.0,
  "RideTrackingIntervalSeconds": 10,
  "MaxDriversToShow": 5
}
```

---

### Step 3: Copy Implementation Files (5 min)

**1. Create Models Directory:**
```bash
mkdir -p NextGenSoftware.OASIS.API.Providers.TelegramOASIS/Models/TimoRides
```

**2. Copy the data models from `TIMO_TELEGRAM_PHASE1_IMPLEMENTATION.md`:**
- `DriverDto.cs`
- `BookingDto.cs`
- `RideBookingState.cs`

---

**3. Create Services Directory:**
```bash
mkdir -p NextGenSoftware.OASIS.API.Providers.TelegramOASIS/Services
```

**4. Copy the services:**
- `TimoRidesApiService.cs` (from Phase 1 guide)
- `RideBookingStateManager.cs` (from Phase 1 guide)
- `GoogleMapsService.cs` (from TIMO_TELEGRAM_HELPERS.cs)

---

### Step 4: Update TelegramBotService (10 min)

**File:** `NextGenSoftware.OASIS.API.Providers.TelegramOASIS/TelegramBotService.cs`

**A. Add private fields** (top of class):

```csharp
private readonly TimoRidesApiService _timoRidesService;
private readonly RideBookingStateManager _rideStateManager;
private readonly GoogleMapsService _mapsService;
```

**B. Update constructor** to inject services:

```csharp
public TelegramBotService(
    string botToken,
    TelegramOASIS provider,
    IAvatarManager avatarManager,
    ILogger<TelegramBotService> logger,
    INFTService nftService,
    IPinataService pinataService,
    TimoRidesApiService timoRidesService,          // ADD
    RideBookingStateManager rideStateManager,     // ADD
    GoogleMapsService mapsService,                // ADD
    IConfiguration configuration)
{
    // ... existing code ...
    _timoRidesService = timoRidesService;
    _rideStateManager = rideStateManager;
    _mapsService = mapsService;
}
```

**C. Update HandleCommandAsync** to route ride commands:

```csharp
private async Task HandleCommandAsync(Message message)
{
    var command = message.Text?.Split(' ')[0].ToLower();
    
    // Existing commands...
    if (command.StartsWith("/start")) { ... }
    else if (command.StartsWith("/creategroup")) { ... }
    // ... other existing commands ...
    
    // ADD RIDE COMMANDS ROUTING:
    else if (IsRideCommand(command))
    {
        var avatar = await GetOrCreateAvatarAsync(message.From.Id);
        await HandleRideCommandAsync(message, avatar);
    }
    else
    {
        await ShowHelpAsync(message);
    }
}

// ADD THIS HELPER:
private bool IsRideCommand(string command)
{
    return command == "/bookride" || 
           command == "/quickbook" ||
           command == "/myrides" ||
           command == "/track" ||
           command == "/rate" ||
           command == "/cancel" ||
           command == "/wallet" ||
           command == "/topup" ||
           command == "/favorites" ||
           command == "/receipt";
}
```

**D. Add location message handler:**

Update your `HandleUpdate` method to detect location messages:

```csharp
if (update.Type == UpdateType.Message)
{
    var message = update.Message;
    
    if (message.Location != null)
    {
        // ADD THIS:
        var avatar = await GetOrCreateAvatarAsync(message.From.Id);
        await HandleLocationMessageAsync(message, avatar);
        return;
    }
    
    if (message.Text != null && message.Text.StartsWith("/"))
    {
        await HandleCommandAsync(message);
    }
    // ... rest of existing code ...
}
```

**E. Add callback query handler:**

Update your callback query handler to include ride callbacks:

```csharp
if (update.Type == UpdateType.CallbackQuery)
{
    var query = update.CallbackQuery;
    
    // Existing callback handling...
    
    // ADD RIDE CALLBACKS:
    if (IsRideCallback(query.Data))
    {
        var avatar = await GetOrCreateAvatarAsync(query.From.Id);
        await HandleRideCallbackQueryAsync(query, avatar);
        return;
    }
    
    // ... rest of existing code ...
}

// ADD THIS HELPER:
private bool IsRideCallback(string data)
{
    return data.StartsWith("select_driver:") ||
           data.StartsWith("driver_profile:") ||
           data.StartsWith("payment:") ||
           data == "confirm_booking" ||
           data == "cancel_booking" ||
           data.StartsWith("rate:") ||
           data.StartsWith("refresh_tracking:") ||
           data.StartsWith("call_driver:");
}
```

**F. Copy handler methods:**

Now copy all the handler methods from `TIMO_TELEGRAM_HANDLERS.cs` into your `TelegramBotService` class:

- `HandleRideCommandAsync`
- `StartRideBookingFlowAsync`
- `HandleLocationMessageAsync`
- `ProcessPickupLocationAsync`
- `ProcessDropoffLocationAsync`
- `ShowAvailableDriversAsync`
- `SendDriverCardAsync`
- `HandleRideCallbackQueryAsync`
- `HandleDriverSelectionAsync`
- `ShowPaymentOptionsAsync`
- Plus all the helper methods

---

### Step 5: Register Services (2 min)

**File:** `NextGenSoftware.OASIS.API.ONODE.WebAPI/Startup.cs` or `Program.cs`

Add service registrations:

```csharp
// In ConfigureServices or where you register services:

// TimoRides services
services.AddSingleton<TimoRidesApiService>();
services.AddSingleton<RideBookingStateManager>();
services.AddSingleton<GoogleMapsService>();
```

---

### Step 6: Start TimoRides Backend (2 min)

```bash
cd /Volumes/Storage/OASIS_CLEAN/TimoRides/ride-scheduler-be
npm install
node server.js
```

Backend should start on `http://localhost:3000`

---

### Step 7: Test! (5 min)

**1. Start your OASIS API:**
```bash
cd /Volumes/Storage/OASIS_CLEAN
dotnet run --project NextGenSoftware.OASIS.API.ONODE.WebAPI
```

**2. Open Telegram and message your bot:**
```
/start
/bookride
```

**3. Share your location when prompted**

**4. Select a driver and complete booking**

---

## 🧪 Testing Checklist

### Basic Flow
- [ ] `/start` - Bot responds with welcome message
- [ ] `/bookride` - Requests pickup location
- [ ] Share location - Bot acknowledges and requests dropoff
- [ ] Share dropoff - Bot fetches nearby drivers
- [ ] Drivers displayed with photos and details
- [ ] Click "Select Driver" - Shows payment options
- [ ] Select payment method - Shows booking summary
- [ ] Click "Confirm" - Creates booking successfully
- [ ] Receive confirmation with booking ID

### Error Handling
- [ ] No drivers available - Shows appropriate message
- [ ] Backend offline - Shows error message gracefully
- [ ] Invalid location - Handles without crashing
- [ ] Wallet insufficient funds - Prevents wallet payment
- [ ] Google Maps API key invalid - Falls back to lat/lng

### Commands
- [ ] `/myrides` - Shows ride history
- [ ] `/track [booking_id]` - Shows ride status
- [ ] `/rate [booking_id]` - Shows rating options
- [ ] `/wallet` - Shows wallet balance
- [ ] `/help` - Shows updated help with ride commands

---

## 🐛 Common Issues & Fixes

### Issue: "No drivers found"

**Cause:** Backend not running or no drivers in database

**Fix:**
```bash
# Make sure backend is running
cd TimoRides/ride-scheduler-be
node server.js

# Check MongoDB has drivers
mongo
> use timorides
> db.drivers.find()
```

---

### Issue: "Failed to process location"

**Cause:** Google Maps API key missing or invalid

**Fix:**
1. Get API key from Google Cloud Console
2. Enable these APIs:
   - Geocoding API
   - Distance Matrix API
   - Maps JavaScript API
3. Add billing account (required even for free tier)
4. Update `OASIS_DNA.json` with your key

---

### Issue: "Service registration failed"

**Cause:** Services not properly registered in DI container

**Fix:**
```csharp
// In Startup.cs/Program.cs, make sure these are BEFORE TelegramBotService:
services.AddSingleton<TimoRidesApiService>();
services.AddSingleton<RideBookingStateManager>();
services.AddSingleton<GoogleMapsService>();

// Then TelegramBotService can inject them:
services.AddSingleton<TelegramBotService>();
```

---

### Issue: "Cannot connect to TimoRides backend"

**Cause:** Backend URL misconfigured

**Fix:**
```json
// In OASIS_DNA.json:
"BackendUrl": "http://localhost:3000/api"  // For local testing

// If backend is on different machine:
"BackendUrl": "http://192.168.1.100:3000/api"

// If using ngrok (for device testing):
"BackendUrl": "https://abc123.ngrok.io/api"
```

---

### Issue: "Callback queries not working"

**Cause:** Callback handler not properly integrated

**Fix:**
Make sure you added the `IsRideCallback()` check in your callback handler:

```csharp
if (update.Type == UpdateType.CallbackQuery)
{
    var query = update.CallbackQuery;
    
    if (IsRideCallback(query.Data))
    {
        await HandleRideCallbackQueryAsync(query, avatar);
        return;
    }
    
    // Existing callback handling...
}
```

---

## 📝 Next Steps After Testing

### Week 1: Polish Basic Flow
- [ ] Add loading states for long operations
- [ ] Improve error messages
- [ ] Add logging throughout
- [ ] Test with multiple users simultaneously

### Week 2: Add Tracking
- [ ] Implement real-time tracking updates
- [ ] Add driver arrival notifications
- [ ] Test tracking with actual rides

### Week 3: Rating & History
- [ ] Complete rating submission
- [ ] Award karma and tokens
- [ ] Display rich ride history
- [ ] Add favorite drivers feature

### Week 4: Payment Integration
- [ ] Connect OASIS Wallet
- [ ] Implement wallet deductions
- [ ] Add mobile money (M-Pesa)
- [ ] Test payment flows

---

## 🚀 Going to Production

### 1. Update Configuration

```json
{
  "TelegramOASIS": {
    "TimoRides": {
      "Enabled": true,
      "BackendUrl": "https://api.timorides.com/api",
      "GoogleMapsApiKey": "YOUR_PRODUCTION_KEY",
      // ... rest of config
    }
  }
}
```

### 2. Environment Variables

Set these in production:

```bash
export TIMORIDES_BACKEND_URL="https://api.timorides.com/api"
export GOOGLE_MAPS_API_KEY="your_production_key"
export TELEGRAM_BOT_TOKEN="your_production_bot_token"
```

### 3. Monitoring

Add monitoring for:
- Booking success rate
- API response times
- Error rates
- User engagement (bookings per day)

### 4. Scaling Considerations

- Use WebSockets instead of polling for tracking (more efficient)
- Cache frequent locations (reduce Google Maps API costs)
- Implement rate limiting per user
- Add Redis for distributed state management (if multiple bot instances)

---

## 📊 Success Metrics

Track these KPIs:

### Technical
- **API Response Time:** < 2 seconds avg
- **Booking Success Rate:** > 95%
- **Error Rate:** < 1%
- **Uptime:** > 99.5%

### User Engagement
- **Daily Active Users (DAU):** Target 100+ after 1 month
- **Bookings per User:** Target 3+ per month
- **Retention Rate:** > 60% month-over-month
- **Average Rating:** > 4.5 stars

### Business
- **Gross Booking Value:** Track total ride value
- **Payment Mix:** Cash vs. Wallet vs. Mobile Money
- **Driver Ratings:** Ensure quality drivers
- **Support Tickets:** Track issues and resolution time

---

## 🎓 Resources

### Documentation
- [Telegram Bot API](https://core.telegram.org/bots/api)
- [Google Maps API](https://developers.google.com/maps/documentation)
- [OASIS API Docs](../OASIS_API_COMPLETE_ENDPOINTS_SUMMARY.md)
- [TimoRides Backend](ride-scheduler-be/README.md)

### Code References
- **Main Design:** `TIMO_TELEGRAM_INTEGRATION_DESIGN.md`
- **Implementation Guide:** `TIMO_TELEGRAM_PHASE1_IMPLEMENTATION.md`
- **Handlers:** `TIMO_TELEGRAM_HANDLERS.cs`
- **Helper Services:** `TIMO_TELEGRAM_HELPERS.cs`

### Support
- **Technical Issues:** Check Common Issues section above
- **Backend API:** Coordinate with TimoRides backend team
- **OASIS Integration:** Review OASIS Telegram Bot docs

---

## ✅ Pre-Launch Checklist

Before going live:

### Technical
- [ ] All tests passing
- [ ] Error handling in place
- [ ] Logging configured
- [ ] Monitoring setup
- [ ] Backup/recovery plan

### Configuration
- [ ] Production API keys configured
- [ ] Backend URLs updated
- [ ] Environment variables set
- [ ] Rate limiting configured

### Documentation
- [ ] User guide created
- [ ] Help command updated
- [ ] FAQ prepared
- [ ] Support process defined

### Testing
- [ ] Load testing completed
- [ ] Security audit done
- [ ] Beta testing feedback incorporated
- [ ] Edge cases handled

### Legal/Compliance
- [ ] Privacy policy updated
- [ ] Terms of service published
- [ ] Data handling compliant
- [ ] Payment regulations followed

---

## 🎉 You're Ready!

You now have everything you need to implement TimoRides Telegram booking:

1. ✅ Complete architecture design
2. ✅ Full implementation code
3. ✅ Helper services and utilities
4. ✅ Testing checklist
5. ✅ Troubleshooting guide
6. ✅ Production deployment guide

**Estimated time to working PoC:** 4-6 weeks  
**Estimated time to production:** 8-12 weeks

**Questions?** Review the full design document or check the troubleshooting section.

**Ready to code?** Start with Step 1 and follow the checklist! 🚀







