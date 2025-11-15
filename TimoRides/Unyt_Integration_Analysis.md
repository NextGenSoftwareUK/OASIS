# Unyt × TimoRides Integration Analysis
**Date:** October 24, 2025  
**Integration Bridge:** HoloNET (Holochain .NET Client)

---

## Executive Summary

**Unyt** and **TimoRides** are a perfect match - both are rideshare platforms with complementary strengths:

- **Unyt**: Decentralized P2P payment/credit system on Holochain with Smart Agreements
- **TimoRides**: Premium rideshare platform with marketplace UX, offline support, and mobile money integration
- **Bridge**: **HoloNET** - the .NET client for Holochain that OASIS already uses

### Key Integration Opportunity

Unyt's rideshare demo shows **multi-regional coordination with custom pricing** - exactly what TimoRides needs for expansion across multiple South African cities (Durban, Cape Town, Johannesburg, etc.) with different pricing and taxi association partnerships.

---

## 1. TimoRides Architecture Analysis

### Current Tech Stack

| Component | Technology | Purpose |
|-----------|-----------|---------|
| **Backend API** | Node.js + Express | REST API, booking logic, payments |
| **Database** | MongoDB | User profiles, bookings, vehicles |
| **Frontend Web** | Angular 15 + NgRx | Admin dashboard, web booking |
| **Mobile App** | Android (Java/Kotlin) | Rider/driver mobile app |
| **Payments** | Flutterwave (expensive) | Card payments, wallet topups |
| **Maps** | Google Maps API | Routing, distance calculation |
| **SMS/Email** | Twilio, SendGrid | Notifications |

### Core Business Models

```
┌─────────────────────────────────────────────┐
│         TimoRides Data Models               │
├─────────────────────────────────────────────┤
│  • User (rider, driver, admin)              │
│  • Car (vehicle profiles, features)         │
│  • Booking (ride requests, status tracking) │
│  • Wallet (balance, topups, payments)       │
│  • PaymentRequest (invoices, receipts)      │
│  • OTP (phone verification)                 │
│  • GlobalSettings (pricing config)          │
└─────────────────────────────────────────────┘
```

### Integration Points Identified

#### 🎯 High-Priority Integration Points

1. **Payment Processing** (`walletTopupModel.js`, `paymentRequestModel.js`)
   - Current: Flutterwave with high merchant fees
   - **Unyt Solution**: P2P payment settlements with Smart Agreements
   - **Benefit**: Eliminate 3-5% merchant fees

2. **Pricing Configuration** (`globalSettingsModal.js`, fare calculation in booking controller)
   - Current: Simple MongoDB pricing config
   - **Unyt Solution**: Multi-unit accounting (KM, Minutes → ZAR/USDC)
   - **Benefit**: Regional pricing customization per city/taxi association

3. **Offline Sync** (Currently not implemented)
   - Current: App breaks without internet
   - **Unyt Solution**: Holochain DHT + local storage
   - **Benefit**: MVP Priority #4 - Offline functionality

4. **Driver Wallet/Earnings** (`utils/wallet.js`, driver earnings tracking)
   - Current: Centralized wallet with Flutterwave payouts
   - **Unyt Solution**: P2P microtransactions, instant settlements
   - **Benefit**: Drivers get paid instantly, no withdrawal delays

#### 🔄 Medium-Priority Integration Points

5. **Booking/Ride Coordination** (`bookingModal.js`, `bookingController.js`)
   - Current: REST API polling for ride status updates
   - **Unyt Solution**: DHT-based coordination with event streams
   - **Benefit**: Real-time updates without WebSocket infrastructure

6. **Trust/Reputation** (Not fully implemented)
   - Current: Basic ratings in User model
   - **Unyt Solution**: Karma API + decentralized reputation
   - **Benefit**: MVP Priority #5 - Trust System

7. **Multi-Regional Pricing** (Not yet implemented)
   - Current: Single pricing model for Durban only
   - **Unyt Solution**: Regional Smart Agreements (like Unyt's rideshare demo)
   - **Benefit**: Scale to multiple cities with different pricing

---

## 2. HoloNET - The Integration Bridge

### What is HoloNET?

**HoloNET** is the official .NET client library for Holochain, created by NextGenSoftware (makers of OASIS). It allows any .NET application to:

1. Connect to Holochain conductors (local or remote)
2. Call Holochain zome functions
3. Subscribe to Holochain signals (events)
4. Manage local DHT data
5. Handle P2P messaging

### HoloNET Capabilities

```
┌──────────────────────────────────────────────────────┐
│              HoloNET Architecture                     │
├──────────────────────────────────────────────────────┤
│                                                       │
│  .NET/C# Application                                 │
│  (TimoRides Backend, OASIS API)                      │
│           │                                           │
│           ▼                                           │
│  ┌──────────────────────────┐                        │
│  │   HoloNET Client         │                        │
│  │  - Connect()             │                        │
│  │  - CallZomeFunction()    │                        │
│  │  - SendHoloNETRequest()  │                        │
│  │  - OnSignalCallBack      │                        │
│  └──────────────────────────┘                        │
│           │                                           │
│           ▼ (WebSocket)                               │
│  ┌──────────────────────────┐                        │
│  │  Holochain Conductor     │                        │
│  │  - Local or Remote       │                        │
│  │  - Manages DHT           │                        │
│  │  - Runs hApps (DNAs)     │                        │
│  └──────────────────────────┘                        │
│           │                                           │
│           ▼                                           │
│  ┌──────────────────────────┐                        │
│  │  Holochain DHT Network   │                        │
│  │  - Distributed Data      │                        │
│  │  - P2P Communication     │                        │
│  │  - Validation Rules      │                        │
│  └──────────────────────────┘                        │
│           │                                           │
│           ▼                                           │
│  ┌──────────────────────────┐                        │
│  │  Unyt Application        │                        │
│  │  - Smart Agreements      │                        │
│  │  - Multi-unit Accounting │                        │
│  │  - P2P Payments          │                        │
│  └──────────────────────────┘                        │
└──────────────────────────────────────────────────────┘
```

### HoloNET Features for TimoRides Integration

#### 1. **Async/Await Support**
```csharp
// HoloNET uses modern async patterns
var result = await holoNETClient.CallZomeFunctionAsync(
    zomeName: "payment_processor",
    fnName: "settle_ride_payment",
    paramsObject: new { ride_id, amount, currency }
);
```

#### 2. **Event-Driven Architecture**
```csharp
holoNETClient.OnZomeFunctionCallBack += (sender, e) => {
    // Handle payment settlement confirmation
    ProcessPaymentComplete(e.ZomeReturnData);
};

holoNETClient.OnSignalCallBack += (sender, e) => {
    // Real-time ride status updates from Unyt DHT
    UpdateRideStatus(e.Signal);
};
```

#### 3. **OASIS Integration**
- HoloNET is already used by OASIS via `HoloOASIS` provider
- TimoRides can leverage OASIS's existing Holochain integration
- Unified Avatar system for riders/drivers across both platforms

---

## 3. Integration Architecture

### Proposed Hybrid Architecture

```
┌───────────────────────────────────────────────────────────────┐
│                    TimoRides × Unyt Stack                      │
├───────────────────────────────────────────────────────────────┤
│                                                                │
│  ┌──────────────────────────────────────────────────────────┐ │
│  │         Frontend Layer                                    │ │
│  │  • Angular Web App (existing)                             │ │
│  │  • Android App (existing)                                 │ │
│  │  • Unyt Desktop App (optional - for driver onboarding)   │ │
│  └──────────────────────────────────────────────────────────┘ │
│                           │                                    │
│                           ▼                                    │
│  ┌──────────────────────────────────────────────────────────┐ │
│  │         TimoRides Backend (Node.js + Express)             │ │
│  │  • Booking API                                            │ │
│  │  • User Management                                        │ │
│  │  • Vehicle Management                                     │ │
│  │  • MongoDB (legacy data storage)                          │ │
│  └──────────────────────────────────────────────────────────┘ │
│                           │                                    │
│           ┌───────────────┴───────────────┐                   │
│           │                               │                   │
│           ▼                               ▼                   │
│  ┌─────────────────┐           ┌─────────────────────────┐   │
│  │  Flutterwave    │           │   OASIS API Layer       │   │
│  │  (Legacy)       │           │   (.NET Core)           │   │
│  │  - Card         │           │   • Avatar API          │   │
│  │    payments     │           │   • Karma API           │   │
│  │  - Fallback     │           │   • Wallet API          │   │
│  └─────────────────┘           │   • HoloOASIS Provider  │   │
│                                └─────────────────────────┘   │
│                                           │                   │
│                                           ▼                   │
│                                ┌─────────────────────────┐   │
│                                │   HoloNET Client        │   │
│                                │   (.NET Library)        │   │
│                                │   • Connect to          │   │
│                                │     Holochain           │   │
│                                │   • Call Unyt zomes     │   │
│                                │   • Subscribe to        │   │
│                                │     signals             │   │
│                                └─────────────────────────┘   │
│                                           │                   │
│                                           ▼                   │
│                                ┌─────────────────────────┐   │
│                                │  Holochain Conductor    │   │
│                                │  (Local/Remote)         │   │
│                                │   • Manages DHT         │   │
│                                │   • Offline storage     │   │
│                                │   • Sync when online    │   │
│                                └─────────────────────────┘   │
│                                           │                   │
│                                           ▼                   │
│                                ┌─────────────────────────┐   │
│                                │   Unyt Application      │   │
│                                │   (Holochain hApp)      │   │
│                                │   • Smart Agreements    │   │
│                                │   • Multi-unit          │   │
│                                │     Accounting          │   │
│                                │   • P2P Payments        │   │
│                                │   • Regional Pricing    │   │
│                                └─────────────────────────┘   │
└───────────────────────────────────────────────────────────────┘
```

### Data Flow Examples

#### Example 1: Ride Payment Settlement

```javascript
// Current TimoRides Flow (Flutterwave)
1. Rider books ride → booking created in MongoDB
2. Ride completes → fare calculated
3. Backend calls Flutterwave API → charge card (3-5% fee)
4. Flutterwave settles to driver wallet (24-48 hour delay)
5. Driver withdraws to bank (additional fee + delay)

// With Unyt Integration
1. Rider books ride → booking created in MongoDB + Holochain DHT
2. Ride completes → fare calculated
3. Backend calls OASIS Wallet API → triggers Unyt Smart Agreement
4. Smart Agreement:
   - Converts service units (10 KM, 15 min) → ZAR amount
   - Debits rider's wallet (USDC/ZAR/mobile money)
   - Credits driver instantly (P2P, no intermediary)
   - Fee: 0.1% platform fee (vs 3-5% Flutterwave)
5. Driver receives payment instantly in their wallet
```

#### Example 2: Regional Pricing Configuration

```javascript
// Current: Single pricing for all rides
{
  "baseKmRate": 12.50,     // ZAR per KM
  "baseMinuteRate": 2.00,  // ZAR per minute
  "baseFare": 25.00        // ZAR minimum fare
}

// With Unyt: Regional Smart Agreements
{
  "durban_premium": {
    "serviceUnits": {
      "kilometers": { "rate": 15.00, "currency": "ZAR" },
      "minutes": { "rate": 2.50, "currency": "ZAR" },
      "surge": { "multiplier": 1.5, "conditions": "peak_hours" }
    },
    "paymentUnits": ["ZAR", "USDC", "MTN_Mobile_Money"],
    "feeStructure": {
      "platformFee": 0.1,
      "taxiAssociationFee": 0.05  // Shared with local association
    }
  },
  "cape_town_standard": {
    "serviceUnits": {
      "kilometers": { "rate": 12.00, "currency": "ZAR" },
      "minutes": { "rate": 1.80, "currency": "ZAR" }
    },
    "paymentUnits": ["ZAR", "USDC"]
  }
}
```

---

## 4. Specific Integration Scenarios

### Scenario A: Payments & Wallet (Phase 1 - Highest ROI)

**Problem:** Flutterwave fees are 3-5%, eating into driver earnings.

**Solution:** Integrate Unyt P2P payments via OASIS Wallet API + HoloNET

#### Implementation Steps

1. **Install HoloNET in OASIS Backend**
   ```bash
   dotnet add package NextGenSoftware.Holochain.HoloNET.Client
   ```

2. **Create HoloOASIS Payment Provider**
   ```csharp
   public class UnytPaymentProvider : IOASISPaymentProvider
   {
       private HoloNETClient _holoNET;
       
       public async Task<PaymentResult> SettleRidePayment(
           string rideId, 
           decimal amount, 
           string currency)
       {
           var result = await _holoNET.CallZomeFunctionAsync(
               zomeName: "payment_processor",
               fnName: "settle_payment",
               paramsObject: new {
                   ride_id = rideId,
                   amount = amount,
                   currency = currency,
                   service_units = new {
                       kilometers = ride.Distance,
                       minutes = ride.Duration
                   }
               }
           );
           
           return MapToPaymentResult(result);
       }
   }
   ```

3. **Update TimoRides Node.js Backend**
   ```javascript
   // New payment route
   router.post('/bookings/:id/settle-payment', async (req, res) => {
       const booking = await Booking.findById(req.params.id);
       
       // Try Unyt first (cheaper, faster)
       try {
           const result = await oasisAPI.wallet.settlePayment({
               rideId: booking._id,
               amount: booking.fare,
               riderId: booking.user,
               driverId: booking.car.driver,
               paymentMethod: 'unyt_p2p'  // or 'mobile_money', 'usdc'
           });
           
           booking.paymentStatus = 'settled';
           booking.paymentProvider = 'unyt';
           await booking.save();
           
           return res.json({ success: true, txHash: result.txHash });
       } catch (unytError) {
           // Fallback to Flutterwave if Unyt fails
           const fallback = await flutterwaveSettlement(booking);
           return res.json({ success: true, fallback: true });
       }
   });
   ```

4. **Mobile App Integration**
   ```kotlin
   // Android: Use WebSocket or REST API to OASIS
   class PaymentManager {
       fun settleRidePayment(rideId: String, amount: Double) {
           val request = PaymentSettlementRequest(
               rideId = rideId,
               amount = amount,
               paymentMethod = "unyt_p2p"
           )
           
           oasisAPI.wallet.settlePayment(request)
               .observeOn(AndroidSchedulers.mainThread())
               .subscribe { result ->
                   // Payment settled instantly via Unyt
                   showPaymentSuccess(result.txHash)
               }
       }
   }
   ```

**ROI Calculation:**
```
Monthly Ride Volume: 10,000 rides
Average Fare: R150 (ZAR)
Monthly GMV: R1,500,000

Current Flutterwave Fees (3.5%):  R52,500/month
With Unyt Fees (0.1%):            R1,500/month
Monthly Savings:                  R51,000/month
Annual Savings:                   R612,000/year (~$33,000 USD)
```

---

### Scenario B: Offline Functionality (Phase 1 - MVP Priority)

**Problem:** App breaks without internet connection.

**Solution:** Holochain DHT + local storage via HoloNET

#### Implementation Architecture

```
┌────────────────────────────────────────────────┐
│         Offline-First Architecture              │
├────────────────────────────────────────────────┤
│                                                 │
│  Driver/Rider Mobile App (Android)             │
│  ┌───────────────────────────────────────────┐ │
│  │  Local SQLite Database                    │ │
│  │   • Cached rides                          │ │
│  │   • Driver profiles                       │ │
│  │   • Pending transactions                  │ │
│  └───────────────────────────────────────────┘ │
│           │                                     │
│           ▼                                     │
│  ┌───────────────────────────────────────────┐ │
│  │  Holochain Conductor (Mobile)             │ │
│  │   • Runs on device                        │ │
│  │   • Stores DHT data locally               │ │
│  │   • Queues writes until online            │ │
│  └───────────────────────────────────────────┘ │
│           │                                     │
│           ▼ (When Online)                      │
│  ┌───────────────────────────────────────────┐ │
│  │  Sync Manager                             │ │
│  │   • Detects connectivity                  │ │
│  │   • Syncs pending transactions            │ │
│  │   • Updates local DHT                     │ │
│  └───────────────────────────────────────────┘ │
└────────────────────────────────────────────────┘
```

#### Mobile Implementation

**Android: Holochain Conductor Integration**

```kotlin
// Use Holochain Rust mobile library via JNI
class HolochainManager(context: Context) {
    private external fun startConductor(configPath: String): Boolean
    private external fun callZome(
        dnaHash: String, 
        zomeName: String, 
        fnName: String, 
        payload: String
    ): String
    
    init {
        System.loadLibrary("holochain_conductor")
    }
    
    fun startOfflineConductor() {
        // Start local conductor for offline storage
        val config = ConductorConfig(
            appId = "timorides",
            networkSeed = getDeviceId(),
            bootstrapService = null  // Offline mode
        )
        
        startConductor(config.toJson())
    }
    
    fun createRideOffline(ride: Ride): String {
        // Store ride in local DHT
        val payload = Json.encodeToString(ride)
        
        return callZome(
            dnaHash = "timorides_dna",
            zomeName = "rides",
            fnName = "create_ride",
            payload = payload
        )
    }
}

// Sync Manager
class OfflineSyncManager(
    private val holochain: HolochainManager,
    private val networkMonitor: NetworkMonitor
) {
    fun startMonitoring() {
        networkMonitor.onConnected {
            // Device came online - sync pending transactions
            syncPendingRides()
            syncPendingPayments()
        }
    }
    
    private suspend fun syncPendingRides() {
        val pendingRides = localDB.getPendingRides()
        
        for (ride in pendingRides) {
            try {
                // Push to Holochain DHT
                val result = holochain.callZome(
                    dnaHash = "timorides_dna",
                    zomeName = "rides",
                    fnName = "sync_ride",
                    payload = ride.toJson()
                )
                
                // Update backend via REST API
                oasisAPI.rides.create(ride)
                
                // Mark as synced
                localDB.markSynced(ride.id)
            } catch (e: Exception) {
                // Will retry on next connection
                Log.e("Sync", "Failed to sync ride ${ride.id}", e)
            }
        }
    }
}
```

**Usage in Ride Booking Flow:**

```kotlin
// Driver accepts ride (offline)
fun acceptRide(rideId: String) {
    if (!networkMonitor.isOnline()) {
        // Store acceptance locally
        localDB.insertPendingAcceptance(
            rideId = rideId,
            driverId = currentDriver.id,
            timestamp = System.currentTimeMillis()
        )
        
        // Also store in Holochain DHT (local)
        holochainManager.createRideOffline(
            Ride(
                id = rideId,
                status = RideStatus.ACCEPTED,
                driver = currentDriver,
                acceptedAt = Date()
            )
        )
        
        // Show offline mode indicator to user
        showOfflineNotification("Ride acceptance will sync when online")
    } else {
        // Normal online flow
        oasisAPI.rides.acceptRide(rideId)
    }
}
```

---

### Scenario C: Multi-Regional Pricing (Phase 2 - Expansion)

**Problem:** Need different pricing for Durban, Cape Town, Johannesburg, and taxi associations.

**Solution:** Unyt Smart Agreements with regional configuration

#### Smart Agreement Example

```javascript
// Durban Premium (Timo Direct)
{
  "region": "durban_premium",
  "smartAgreement": {
    "parties": {
      "platform": "timorides_durban",
      "drivers": ["driver_collective_durban"],
      "riders": ["all_verified_riders"]
    },
    "serviceUnits": {
      "distance": {
        "unit": "kilometers",
        "rate": 15.00,
        "currency": "ZAR",
        "rateModifiers": [
          { "condition": "peak_hours", "multiplier": 1.5 },
          { "condition": "night_time", "multiplier": 1.3 },
          { "condition": "surge_demand", "multiplier": 2.0 }
        ]
      },
      "time": {
        "unit": "minutes",
        "rate": 2.50,
        "currency": "ZAR"
      },
      "premium_features": {
        "child_seat": { "rate": 20.00, "currency": "ZAR" },
        "wifi": { "rate": 0.00, "included": true },
        "water": { "rate": 0.00, "included": true }
      }
    },
    "paymentUnits": [
      "ZAR",
      "USDC",
      "MTN_Mobile_Money",
      "Vodacom_M-Pesa"
    ],
    "feeStructure": {
      "platformFee": {
        "percentage": 10.0,
        "recipient": "timorides_platform"
      },
      "transactionFee": {
        "percentage": 0.1,
        "recipient": "unyt_network"
      }
    },
    "settlementTerms": {
      "frequency": "immediate",  // Instant driver payouts
      "method": "p2p_transfer",
      "minimumBalance": 0
    }
  }
}

// Cape Town - Taxi Association Partnership
{
  "region": "cape_town_taxi_association",
  "smartAgreement": {
    "parties": {
      "platform": "timorides_platform",
      "taxiAssociation": "cata_cape_town",  // Cape Amalgamated Taxi Association
      "drivers": ["cata_registered_drivers"],
      "riders": ["all_verified_riders"]
    },
    "serviceUnits": {
      "distance": {
        "unit": "kilometers",
        "rate": 12.00,
        "currency": "ZAR"
      },
      "time": {
        "unit": "minutes",
        "rate": 1.80,
        "currency": "ZAR"
      }
    },
    "paymentUnits": ["ZAR", "Mobile_Money"],
    "feeStructure": {
      "platformFee": {
        "percentage": 8.0,
        "recipient": "timorides_platform"
      },
      "associationFee": {
        "percentage": 5.0,
        "recipient": "cata_cape_town"  // Revenue share with taxi association
      },
      "transactionFee": {
        "percentage": 0.1,
        "recipient": "unyt_network"
      }
    },
    "specialRules": {
      "local_route_priority": true,  // CATA drivers get first priority on local routes
      "peak_time_bonuses": {
        "enabled": true,
        "bonus_percentage": 20.0,
        "paid_by": "timorides_platform"
      }
    }
  }
}
```

#### Backend Implementation

```csharp
// OASIS API - Smart Agreement Manager
public class SmartAgreementService
{
    private readonly HoloNETClient _holoNET;
    
    public async Task<SmartAgreement> LoadRegionalAgreement(string region)
    {
        var result = await _holoNET.CallZomeFunctionAsync(
            zomeName: "smart_agreements",
            fnName: "get_agreement_by_region",
            paramsObject: new { region = region }
        );
        
        return JsonSerializer.Deserialize<SmartAgreement>(result.ZomeReturnData);
    }
    
    public async Task<FareCalculation> CalculateFare(
        string region,
        double distanceKm,
        int durationMinutes,
        Dictionary<string, object> rideContext)
    {
        var agreement = await LoadRegionalAgreement(region);
        
        var result = await _holoNET.CallZomeFunctionAsync(
            zomeName: "fare_calculator",
            fnName: "calculate_fare",
            paramsObject: new {
                agreement_id = agreement.Id,
                service_units = new {
                    distance_km = distanceKm,
                    duration_minutes = durationMinutes
                },
                context = rideContext  // peak_hours, surge, etc.
            }
        );
        
        return JsonSerializer.Deserialize<FareCalculation>(result.ZomeReturnData);
    }
}
```

```javascript
// TimoRides Node.js Backend - Use OASIS Smart Agreement Service
router.post('/bookings/estimate-fare', async (req, res) => {
    const { pickup, destination, region, vehicleType } = req.body;
    
    // Calculate distance/time using Google Maps
    const route = await googleMapsService.getRoute(pickup, destination);
    
    // Get fare from Unyt Smart Agreement
    const fareEstimate = await oasisAPI.smartAgreements.calculateFare({
        region: region || 'durban_premium',
        distanceKm: route.distance / 1000,
        durationMinutes: route.duration / 60,
        context: {
            time_of_day: new Date().getHours(),
            day_of_week: new Date().getDay(),
            vehicle_type: vehicleType,
            surge_multiplier: await getSurgeMultiplier(pickup)
        }
    });
    
    res.json({
        estimatedFare: fareEstimate.total,
        breakdown: fareEstimate.breakdown,
        currency: fareEstimate.currency,
        paymentOptions: fareEstimate.acceptedPaymentMethods
    });
});
```

---

## 5. Implementation Roadmap

### Phase 1: Foundation (Weeks 1-8)

#### Week 1-2: Infrastructure Setup
- [ ] Set up OASIS API .NET backend service
- [ ] Install HoloNET NuGet packages
- [ ] Deploy Holochain conductor (dev environment)
- [ ] Connect HoloNET to Unyt Tx5 hApp

#### Week 3-4: Payment Integration (Highest ROI)
- [ ] Implement `UnytPaymentProvider` in OASIS
- [ ] Create REST API wrapper for TimoRides Node.js backend
- [ ] Test P2P payment flow (rider → driver)
- [ ] Implement Flutterwave fallback
- [ ] Add wallet balance tracking via Unyt

#### Week 5-6: Mobile Offline Support
- [ ] Integrate Holochain conductor in Android app (via JNI)
- [ ] Implement local SQLite cache
- [ ] Build sync manager for pending transactions
- [ ] Test offline ride booking → online sync

#### Week 7-8: Testing & Rollout
- [ ] End-to-end testing (payment + offline)
- [ ] Beta test with 50 drivers in Durban
- [ ] Monitor transaction costs vs Flutterwave
- [ ] Gather user feedback

**Expected Outcomes:**
- 90% reduction in payment fees
- Offline ride booking capability
- Instant driver payouts

---

### Phase 2: Multi-Regional Expansion (Weeks 9-16)

#### Week 9-10: Smart Agreements for Regional Pricing
- [ ] Create Durban Premium Smart Agreement
- [ ] Create Cape Town Taxi Association Agreement
- [ ] Implement `SmartAgreementService` in OASIS
- [ ] Test multi-unit accounting (KM, minutes → ZAR)

#### Week 11-12: Mobile Money Integration
- [ ] Integrate MTN Mobile Money via Unyt
- [ ] Integrate Vodacom M-Pesa via Unyt
- [ ] Add payment method selection in mobile app
- [ ] Test cross-currency settlements (ZAR ↔ USDC)

#### Week 13-14: Cape Town Launch
- [ ] Onboard CATA (Cape Amalgamated Taxi Association)
- [ ] Deploy Cape Town Smart Agreement
- [ ] Launch marketing campaign
- [ ] Monitor driver adoption

#### Week 15-16: Analytics & Optimization
- [ ] Build admin dashboard for multi-region analytics
- [ ] Monitor settlement times and costs
- [ ] Optimize DHT sync performance
- [ ] Prepare for Johannesburg launch

**Expected Outcomes:**
- 2 cities operational (Durban, Cape Town)
- Partnership with 1 taxi association
- 3 payment methods supported

---

### Phase 3: Advanced Features (Weeks 17-24)

#### Trust & Reputation (Weeks 17-20)
- [ ] Integrate OASIS Karma API
- [ ] Connect driver ratings to Karma score
- [ ] Display Karma badges in marketplace UI
- [ ] Implement trust-based ride matching

#### Data Monetization (Weeks 21-24)
- [ ] Implement PathPulse integration (GPS data sharing)
- [ ] Driver opt-in for data sharing
- [ ] Micropayments to drivers for data contribution
- [ ] Dashboard showing driver data earnings

---

## 6. Technical Integration Details

### Node.js to OASIS API Bridge

Since TimoRides backend is Node.js and OASIS/HoloNET is .NET, we need a bridge.

#### Option A: REST API Wrapper (Recommended)

```
┌─────────────────────────────────────────────────┐
│   TimoRides Node.js Backend                     │
│   (Express REST API)                            │
└───────────────┬─────────────────────────────────┘
                │ HTTP/REST
                ▼
┌─────────────────────────────────────────────────┐
│   OASIS API Gateway                             │
│   (.NET Core Web API)                           │
│   • /api/wallet/settle-payment                  │
│   • /api/smart-agreements/calculate-fare        │
│   • /api/karma/get-score                        │
└───────────────┬─────────────────────────────────┘
                │ HoloNET
                ▼
┌─────────────────────────────────────────────────┐
│   HoloNET Client                                │
│   (Holochain .NET Client)                       │
└───────────────┬─────────────────────────────────┘
                │ WebSocket
                ▼
┌─────────────────────────────────────────────────┐
│   Holochain Conductor                           │
│   (Local or Remote)                             │
└───────────────┬─────────────────────────────────┘
                │ DHT
                ▼
┌─────────────────────────────────────────────────┐
│   Unyt Application                              │
│   (Holochain hApp)                              │
└─────────────────────────────────────────────────┘
```

**Implementation:**

```csharp
// OASIS API Gateway - Startup.cs
public void ConfigureServices(IServiceCollection services)
{
    services.AddControllers();
    services.AddSingleton<IHoloNETClient>(sp => 
    {
        var client = new HoloNETClient("ws://localhost:8888");
        client.Connect();
        return client;
    });
    services.AddScoped<IWalletService, UnytWalletService>();
    services.AddScoped<ISmartAgreementService, SmartAgreementService>();
}

// WalletController.cs
[ApiController]
[Route("api/wallet")]
public class WalletController : ControllerBase
{
    private readonly IWalletService _walletService;
    
    [HttpPost("settle-payment")]
    public async Task<IActionResult> SettlePayment([FromBody] PaymentRequest request)
    {
        var result = await _walletService.SettleRidePayment(
            rideId: request.RideId,
            amount: request.Amount,
            riderId: request.RiderId,
            driverId: request.DriverId
        );
        
        return Ok(new { 
            success = true, 
            txHash = result.TransactionHash,
            settledAt = result.Timestamp
        });
    }
}
```

```javascript
// TimoRides Node.js - Call OASIS API
const axios = require('axios');

class OASISClient {
    constructor(baseURL) {
        this.api = axios.create({
            baseURL: baseURL || 'http://localhost:5000',
            timeout: 10000
        });
    }
    
    async settlePayment(rideId, amount, riderId, driverId) {
        try {
            const response = await this.api.post('/api/wallet/settle-payment', {
                rideId,
                amount,
                riderId,
                driverId
            });
            
            return response.data;
        } catch (error) {
            console.error('OASIS payment failed:', error);
            throw error;
        }
    }
    
    async calculateFare(region, distanceKm, durationMinutes, context) {
        const response = await this.api.post('/api/smart-agreements/calculate-fare', {
            region,
            distanceKm,
            durationMinutes,
            context
        });
        
        return response.data;
    }
}

// Usage in booking controller
const oasisClient = new OASISClient(process.env.OASIS_API_URL);

router.post('/bookings/:id/complete', async (req, res) => {
    const booking = await Booking.findById(req.params.id)
        .populate('user')
        .populate({ path: 'car', populate: { path: 'driver' } });
    
    // Settle payment via Unyt
    try {
        const paymentResult = await oasisClient.settlePayment(
            booking._id,
            booking.fare,
            booking.user._id,
            booking.car.driver._id
        );
        
        booking.paymentStatus = 'settled';
        booking.paymentTxHash = paymentResult.txHash;
        booking.paymentProvider = 'unyt';
        await booking.save();
        
        res.json({ success: true, payment: paymentResult });
    } catch (error) {
        // Fallback to Flutterwave
        console.error('Unyt payment failed, using fallback:', error);
        const fallbackResult = await flutterwavePayment(booking);
        res.json({ success: true, fallback: true });
    }
});
```

---

#### Option B: gRPC Bridge (Higher Performance)

For production scale, consider gRPC for better performance:

```protobuf
// oasis_wallet.proto
syntax = "proto3";

service WalletService {
    rpc SettlePayment (PaymentRequest) returns (PaymentResponse);
    rpc GetBalance (BalanceRequest) returns (BalanceResponse);
}

message PaymentRequest {
    string ride_id = 1;
    double amount = 2;
    string rider_id = 3;
    string driver_id = 4;
    string currency = 5;
}

message PaymentResponse {
    bool success = 1;
    string tx_hash = 2;
    int64 timestamp = 3;
}
```

---

### Mobile App Integration

#### Android: Direct Holochain Integration via JNI

For offline support, embed Holochain conductor directly in Android app.

**Build Steps:**

1. **Compile Holochain for Android**
   ```bash
   # Cross-compile Holochain Rust library to Android
   cd holochain
   cargo install cross
   cross build --target aarch64-linux-android --release
   cross build --target armv7-linux-androideabi --release
   ```

2. **Create JNI Wrapper**
   ```kotlin
   // HolochainJNI.kt
   class HolochainConductor {
       external fun startConductor(configPath: String): Boolean
       external fun callZome(
           dnaHash: ByteArray,
           zomeName: String,
           fnName: String,
           payload: ByteArray
       ): ByteArray
       external fun shutdown(): Boolean
       
       companion object {
           init {
               System.loadLibrary("holochain_jni")
           }
       }
   }
   ```

3. **Use in Android App**
   ```kotlin
   // RideBookingViewModel.kt
   class RideBookingViewModel(
       private val holochain: HolochainConductor,
       private val networkMonitor: NetworkMonitor
   ) : ViewModel() {
       
       fun bookRide(pickup: LatLng, destination: LatLng, driverId: String) {
           val ride = Ride(
               id = UUID.randomUUID().toString(),
               pickup = pickup,
               destination = destination,
               driverId = driverId,
               riderId = currentUserId,
               status = RideStatus.REQUESTED
           )
           
           if (networkMonitor.isOnline()) {
               // Online: Use REST API + Holochain DHT
               bookRideOnline(ride)
           } else {
               // Offline: Store in local Holochain DHT
               bookRideOffline(ride)
           }
       }
       
       private fun bookRideOffline(ride: Ride) {
           val payload = Json.encodeToString(ride).toByteArray()
           
           holochain.callZome(
               dnaHash = "timorides_dna".encodeToByteArray(),
               zomeName = "rides",
               fnName = "create_ride",
               payload = payload
           )
           
           // Queue for sync when online
           syncQueue.add(SyncTask(
               type = SyncType.CREATE_RIDE,
               data = ride
           ))
           
           _rideStatus.value = RideStatus.PENDING_SYNC
       }
   }
   ```

---

## 7. Cost-Benefit Analysis

### Current Costs (Flutterwave)

| Item | Cost | Annual (10k rides/month) |
|------|------|--------------------------|
| Transaction fees (3.5%) | R52,500/month | R630,000 |
| Payout delays | Opportunity cost | ~R50,000 |
| Customer support | Integration complexity | R100,000 |
| **Total** | | **R780,000/year** |

### With Unyt Integration

| Item | Cost | Annual |
|------|------|--------|
| Transaction fees (0.1%) | R1,500/month | R18,000 |
| Holochain hosting | R5,000/month | R60,000 |
| OASIS API maintenance | R10,000/month | R120,000 |
| **Total** | | **R198,000/year** |

**Net Savings: R582,000/year (~$32,000 USD)**

**ROI: 294% in Year 1**

---

## 8. Risks & Mitigation

### Technical Risks

| Risk | Impact | Mitigation |
|------|--------|------------|
| Holochain network instability | High | Implement Flutterwave fallback |
| Mobile Holochain performance | Medium | Use Zero-Arc architecture, local caching |
| .NET ↔ Node.js complexity | Medium | Use REST API gateway, well-documented |
| Offline sync conflicts | Medium | Implement conflict resolution in Smart Agreements |

### Business Risks

| Risk | Impact | Mitigation |
|------|--------|------------|
| Driver adoption resistance | High | Phase rollout, incentivize early adopters |
| Regulatory compliance | Medium | Partner with licensed taxi associations |
| Unyt platform maturity | Medium | Test extensively, maintain fallbacks |
| Mobile data costs | Low | Offline-first architecture reduces data usage |

---

## 9. Key Integration Points Summary

### Immediate Integration Opportunities (Phase 1)

1. **Payment Settlement** ✅ HIGHEST PRIORITY
   - Use: HoloNET → Unyt Smart Agreements
   - Benefit: 90% fee reduction, instant driver payouts
   - ROI: R582k/year savings

2. **Offline Functionality** ✅ MVP PRIORITY #4
   - Use: Holochain DHT + local storage
   - Benefit: Reliable service in low-connectivity areas
   - Differentiator: Uber/Bolt don't have this

3. **Mobile Money Integration** ✅ MVP PRIORITY #3
   - Use: Unyt payment rails + OASIS Wallet API
   - Benefit: Reduce Flutterwave dependency
   - Target: 70% of users prefer mobile money

### Medium-Term Integration (Phase 2-3)

4. **Multi-Regional Pricing**
   - Use: Unyt Smart Agreements (like their rideshare demo)
   - Benefit: Expand to multiple cities with custom pricing
   - Target: 3 cities by end of Phase 2

5. **Trust/Reputation System**
   - Use: OASIS Karma API + Holochain verification
   - Benefit: Safety, driver quality, marketplace trust
   - Target: MVP Priority #5

6. **Data Monetization**
   - Use: PathPulse integration (already documented)
   - Benefit: Drivers earn passive income from GPS data
   - Target: R50-200/month per driver

---

## 10. Next Steps & Recommendations

### Immediate Actions (This Week)

1. **Test Unyt Tx5 Application**
   - Download Unyt Tx5 v0.42.0
   - Create test accounts (rider, driver)
   - Test the rideshare demo
   - Document Smart Agreement configuration process

2. **Set Up HoloNET Development Environment**
   - Clone OASIS repository
   - Install HoloNET NuGet packages
   - Review HoloOASIS provider code
   - Connect to local Holochain conductor

3. **Design Payment Flow**
   - Map current Flutterwave flow
   - Design Unyt payment flow
   - Identify integration points in TimoRides backend
   - Create fallback strategy

### Short-Term (Next 2 Weeks)

4. **Proof of Concept**
   - Build simple OASIS API gateway
   - Implement one Unyt payment via HoloNET
   - Test from Node.js backend
   - Measure transaction time and cost

5. **Mobile Offline Prototype**
   - Test Holochain on Android emulator
   - Implement basic offline ride creation
   - Test sync when connection restored
   - Document performance metrics

### Medium-Term (Next 1-2 Months)

6. **Phase 1 Implementation**
   - Full payment integration (with fallback)
   - Mobile offline support (Android)
   - Beta test with 50 Durban drivers
   - Measure cost savings vs Flutterwave

7. **Multi-Regional Planning**
   - Create Durban Smart Agreement
   - Design Cape Town Taxi Association agreement
   - Plan mobile money integration
   - Prepare for Phase 2 launch

---

## 11. Technical Resources

### Required Tools & Libraries

| Component | Tool/Library | Version | Purpose |
|-----------|-------------|---------|---------|
| .NET Runtime | .NET 6.0+ | Latest | OASIS API backend |
| HoloNET Client | NextGenSoftware.Holochain.HoloNET.Client | Latest | Holochain integration |
| Holochain Conductor | holochain | 0.3.x | Local/remote DHT |
| Unyt Tx5 | Unyt application | v0.42.0 | Smart Agreements |
| Node.js | Node.js | 18+ | TimoRides backend |
| Android NDK | Android NDK | Latest | Mobile Holochain JNI |

### Development Setup

```bash
# 1. Clone OASIS repository
git clone https://github.com/NextGenSoftwareUK/OASIS.git
cd OASIS

# 2. Install HoloNET
cd NextGenSoftware.OASIS.API.Providers.HoloOASIS
dotnet restore

# 3. Download Unyt Tx5
# Visit: https://github.com/unytco/unyt-sandbox-tx5/releases
# Download: Unyt-tx5_0.42.0 for your platform

# 4. Start Holochain conductor (if running locally)
holochain -c conductor-config.yaml

# 5. Test HoloNET connection
cd /path/to/OASIS/NextGenSoftware.Holochain.HoloNET.Client.TestHarness
dotnet run
```

### Documentation Links

- **HoloNET Documentation**: `/holochain-client-csharp/README.md` (in this repo)
- **OASIS API Documentation**: `/README.md` (in this repo)
- **Unyt Repository**: https://github.com/unytco/unyt-sandbox-tx5
- **Unyt Discord**: https://discord.com/channels/919686143581253632/1425157240972902430
- **Holochain Dev Discord**: https://discord.gg/k55DS5dmPH

---

## 12. Conclusion

### Why This Integration Makes Sense

1. **Perfect Use Case Alignment**
   - Unyt's demo is literally a rideshare platform
   - Multi-regional pricing matches TimoRides expansion needs
   - P2P payments solve the Flutterwave cost problem

2. **Technology Stack Compatibility**
   - OASIS already has HoloNET integration (HoloOASIS provider)
   - HoloNET bridges .NET ↔ Holochain seamlessly
   - Node.js can call OASIS REST API easily

3. **MVP Priority Alignment**
   - Offline support: MVP Priority #4 ✅
   - Mobile money: MVP Priority #3 ✅
   - Lower costs: MVP Priority #3 ✅
   - Trust system: MVP Priority #5 ✅

4. **Immediate ROI**
   - R582k/year savings on transaction fees
   - Instant driver payouts (competitive advantage)
   - Offline functionality (unique differentiator)

### Recommended Approach

**Start Small, Scale Fast:**

1. **Week 1-2**: Test Unyt, set up HoloNET environment
2. **Week 3-6**: Build payment integration POC
3. **Week 7-8**: Beta test with 50 drivers
4. **Week 9-16**: Full Phase 1 rollout + multi-regional expansion

**Key Success Factors:**
- Maintain Flutterwave as fallback during transition
- Train drivers on new payment system
- Monitor transaction success rates closely
- Partner with taxi associations for multi-regional expansion

---

## Contact & Support

**OASIS/HoloNET:**
- Discord: https://discord.gg/q9gMKU6
- GitHub: https://github.com/NextGenSoftwareUK/OASIS

**Unyt:**
- Discord: https://discord.com/channels/919686143581253632/1425157240972902430
- GitHub: https://github.com/unytco/unyt-sandbox-tx5

**Holochain:**
- Discord: https://discord.gg/k55DS5dmPH
- Forum: https://forum.holochain.org

---

**Document Version:** 1.0  
**Last Updated:** October 24, 2025  
**Author:** AI Analysis for TimoRides Integration Team











