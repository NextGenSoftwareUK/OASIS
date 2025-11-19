# PathPulse Integration Plan for Driver App

**Date:** January 2025  
**Status:** Planning Phase

---

## 🎯 Overview

PathPulse.ai provides intelligent routing and optimization for the TimoRides driver app. This document outlines where and how PathPulse will be integrated.

---

## 📍 Integration Points

### 1. **Home Screen - Real-time Location Tracking**

**Location:** `src/screens/Home/HomeScreen.jsx`  
**Service:** `src/services/location.js` (NEW)

**What PathPulse Does:**
- Receives driver location updates via webhook
- Tracks driver position in real-time
- Updates backend with current location

**Implementation:**
```javascript
// src/services/location.js
- Background location service
- Sends location to backend every 10 seconds
- Backend forwards to PathPulse webhook
```

**Backend Endpoint:**
- `POST /api/driver-signals/driver-location`
- Backend forwards to PathPulse webhook

**PathPulse Webhook:**
- `POST /api/driver-signals/driver-webhooks/pathpulse`
- Receives location updates from PathPulse

---

### 2. **Ride Request Screen - Route Preview**

**Location:** `src/screens/Rides/RideRequestScreen.jsx`

**What PathPulse Does:**
- Calculates optimal route from driver → pickup → destination
- Provides ETA estimates with traffic
- Shows route on map preview
- Calculates fare estimate based on route

**Features:**
- Route polyline visualization
- Distance calculation
- ETA to pickup
- ETA to destination
- Traffic-aware routing
- Multi-stop optimization (if multiple pickups)

**API Flow:**
```
1. Driver receives ride request
2. App calls backend: GET /api/bookings/:id
3. Backend calls PathPulse: Calculate route
   - From: Driver current location
   - To: Pickup location
   - Then: Destination location
4. PathPulse returns:
   - Route polyline
   - Distance
   - ETA (with traffic)
   - Estimated fare
5. App displays route preview on map
```

**UI Elements:**
- Map with route polyline
- "X min to pickup" badge
- "Y km total distance" badge
- Route preview card

---

### 3. **Active Ride Screen - Turn-by-turn Navigation**

**Location:** `src/screens/Rides/ActiveRideScreen.jsx` (NEW)

**What PathPulse Does:**
- Provides real-time navigation instructions
- Updates route based on traffic
- Recalculates ETA dynamically
- Optimizes route in real-time

**Features:**
- Turn-by-turn directions
- Voice navigation (future)
- Traffic alerts
- Route recalculation
- ETA updates
- Distance remaining

**API Flow:**
```
1. Driver accepts ride
2. App calls PathPulse: Start navigation
   - From: Driver location
   - To: Pickup (then destination)
3. PathPulse streams navigation updates:
   - Current step
   - Next turn
   - Distance to next turn
   - ETA updates
4. App displays navigation UI
5. On arrival, app confirms with PathPulse
```

**UI Elements:**
- Navigation card (top of screen)
- Current instruction
- Next turn indicator
- Distance/ETA display
- Map with route highlighting
- "Arrived" button

---

### 4. **Location Service - Background Updates**

**Location:** `src/services/location.js` (NEW)

**What PathPulse Does:**
- Receives continuous location updates
- Tracks driver movement
- Updates driver position in system
- Enables real-time matching

**Implementation:**
```javascript
// src/services/location.js
import * as Location from 'expo-location';

class LocationService {
  // Start tracking when driver goes online
  startTracking() {
    // Get location every 10 seconds
    // Send to backend
    // Backend forwards to PathPulse
  }

  // Stop tracking when driver goes offline
  stopTracking() {
    // Clear interval
  }

  // Send location update
  async updateLocation(location) {
    // POST /api/driver-signals/driver-location
    // Backend forwards to PathPulse webhook
  }
}
```

**Backend Integration:**
- `POST /api/driver-signals/driver-location`
- Backend receives location
- Backend forwards to PathPulse webhook
- PathPulse updates driver position

---

### 5. **Backend Webhook Handler**

**Location:** `ride-scheduler-be/routes/driverSignalRoutes.js`  
**Controller:** `ride-scheduler-be/controllers/driverSignalController.js`

**What PathPulse Does:**
- Sends webhook events to backend
- Location updates
- Route completion
- Traffic alerts
- ETA updates

**Webhook Endpoint:**
```
POST /api/driver-signals/driver-webhooks/pathpulse
```

**Webhook Events:**
```javascript
{
  "event": "location_update" | "route_completed" | "traffic_alert" | "eta_update",
  "driverId": "driver-id",
  "data": {
    // Event-specific data
  },
  "signature": "pathpulse-signature"
}
```

**Security:**
- Signature verification
- Rate limiting
- Event validation

---

## 🔄 Data Flow

### Location Update Flow

```
Driver App
  ↓ (every 10 seconds)
Location Service
  ↓ POST /api/driver-signals/driver-location
Backend API
  ↓ Forward to PathPulse
PathPulse Webhook
  ↓ Process location
PathPulse System
  ↓ (optional) Send updates back
Backend Webhook Handler
  ↓ POST /api/driver-signals/driver-webhooks/pathpulse
Backend API
  ↓ (optional) Notify driver app
Driver App
```

### Route Calculation Flow

```
Driver receives ride request
  ↓
Ride Request Screen
  ↓ GET /api/bookings/:id
Backend API
  ↓ Call PathPulse API
PathPulse Routing Service
  ↓ Return route data
Backend API
  ↓ Return to app
Ride Request Screen
  ↓ Display route on map
```

### Navigation Flow

```
Driver accepts ride
  ↓
Active Ride Screen
  ↓ Start navigation
PathPulse Navigation API
  ↓ Stream navigation updates
Active Ride Screen
  ↓ Display turn-by-turn
Driver navigates
  ↓ Location updates
PathPulse updates route
  ↓ Send updated ETA
Active Ride Screen
  ↓ Update UI
```

---

## 📋 Implementation Checklist

### Phase 1: Location Tracking
- [ ] Create `src/services/location.js`
- [ ] Implement background location tracking
- [ ] Connect to backend location endpoint
- [ ] Test location updates
- [ ] Verify PathPulse webhook receives updates

### Phase 2: Route Preview
- [ ] Enhance Ride Request Screen
- [ ] Add map component
- [ ] Integrate PathPulse route API
- [ ] Display route polyline
- [ ] Show ETA and distance
- [ ] Calculate fare estimate

### Phase 3: Navigation
- [ ] Create Active Ride Screen
- [ ] Integrate PathPulse navigation API
- [ ] Display turn-by-turn directions
- [ ] Show route on map
- [ ] Update ETA in real-time
- [ ] Handle route recalculation

### Phase 4: Backend Integration
- [ ] Verify webhook endpoint exists
- [ ] Test webhook signature verification
- [ ] Handle PathPulse events
- [ ] Forward location updates
- [ ] Process route completions

---

## 🔧 Technical Details

### PathPulse API Endpoints (via Backend)

**Route Calculation:**
```
POST /api/pathpulse/calculate-route
Body: {
  origin: { lat, lng },
  destination: { lat, lng },
  waypoints?: [{ lat, lng }],
  optimize?: boolean
}
Response: {
  route: { polyline, distance, duration, eta },
  fare: { estimate }
}
```

**Navigation Start:**
```
POST /api/pathpulse/start-navigation
Body: {
  driverId: string,
  routeId: string,
  bookingId: string
}
Response: {
  navigationId: string,
  instructions: [...]
}
```

**Location Update:**
```
POST /api/driver-signals/driver-location
Body: {
  driverId: string,
  location: { lat, lng, heading, speed },
  timestamp: number
}
```

### Webhook Events

**Location Update:**
```json
{
  "event": "location_update",
  "driverId": "driver-123",
  "data": {
    "location": { "lat": -29.8587, "lng": 31.0218 },
    "heading": 45,
    "speed": 60
  }
}
```

**Route Completed:**
```json
{
  "event": "route_completed",
  "driverId": "driver-123",
  "bookingId": "booking-456",
  "data": {
    "routeId": "route-789",
    "completedAt": "2025-01-15T10:30:00Z"
  }
}
```

**Traffic Alert:**
```json
{
  "event": "traffic_alert",
  "driverId": "driver-123",
  "data": {
    "alert": "Heavy traffic ahead",
    "delay": 5,
    "location": { "lat": -29.8587, "lng": 31.0218 }
  }
}
```

---

## 🎨 UI/UX Considerations

### Route Preview (Ride Request Screen)
- Map with route polyline (blue line)
- Pickup marker (green)
- Destination marker (red)
- Driver marker (blue car icon)
- ETA badge (top right)
- Distance badge
- "View Route" button

### Navigation (Active Ride Screen)
- Large navigation card (top)
- Current instruction text
- Next turn icon
- Distance to next turn
- ETA to destination
- Map with highlighted route
- "Arrived" button (when at pickup/destination)

### Location Tracking
- No visible UI (background service)
- Status indicator in header (online/offline)
- Location accuracy indicator (optional)

---

## 🚀 Next Steps

1. **Review PathPulse API Documentation**
   - Understand available endpoints
   - Review webhook format
   - Check authentication requirements

2. **Create Location Service**
   - Background tracking
   - API integration
   - Error handling

3. **Enhance Ride Request Screen**
   - Add map component
   - Integrate route API
   - Display route preview

4. **Create Active Ride Screen**
   - Navigation UI
   - Turn-by-turn display
   - Route visualization

5. **Test Integration**
   - Location updates
   - Route calculation
   - Navigation flow
   - Webhook events

---

## 📚 Related Documentation

- PathPulse Architecture: `/TimoRides/PathPulse_Architecture_Diagram.md`
- PathPulse Executive Summary: `/TimoRides/PathPulse_Integration_Executive_Summary.md`
- Backend API: `/TimoRides/ride-scheduler-be/README.md`
- Driver App Plan: `/TimoRides/DRIVER_APP_DEVELOPMENT_PLAN.md`

---

**Status:** Ready for Implementation  
**Priority:** High - Core functionality for driver app


