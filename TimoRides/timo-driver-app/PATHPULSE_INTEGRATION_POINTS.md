# PathPulse Integration Points in Driver App

**Date:** January 2025  
**Status:** Integration Planning  
**Related:** PathPulse × TimoRides Integration Plan

---

## 🎯 Overview

This document shows **where and how** PathPulse.ai routing integration will appear in the React Native driver app. PathPulse provides intelligent routing, traffic-aware ETAs, and route optimization that enhances the driver experience.

---

## 📍 Integration Points Map

### 1. **Home Screen - Location Tracking & Route Display**

**Location:** `src/screens/Home/HomeScreen.js`

**Integration Points:**
- ✅ **Location Updates**: Driver location sent to backend → forwarded to PathPulse
- ✅ **Map Display**: Route visualization from PathPulse (via backend API)
- ✅ **Traffic Indicators**: Real-time traffic data displayed on map

**Current Implementation:**
```javascript
// Location updates sent every 5 seconds
useEffect(() => {
  if (isOnline && driverId) {
    locationService.startTracking(driverId, (location) => {
      // Location sent to backend
      // Backend forwards to PathPulse for route optimization
    });
  }
}, [isOnline, driverId]);
```

**PathPulse Enhancement:**
```javascript
// Enhanced with PathPulse route data
useEffect(() => {
  if (isOnline && driverId) {
    locationService.startTracking(driverId, async (location) => {
      // Send location to backend
      await driversService.updateLocation(driverId, location);
      
      // Backend calls PathPulse API for:
      // - Traffic-aware route updates
      // - Alternative route suggestions
      // - Real-time ETA adjustments
    });
  }
}, [isOnline, driverId]);
```

**Visual Changes:**
- 🟢 **Traffic Overlay**: Color-coded traffic conditions on map
- 🛣️ **Route Lines**: PathPulse-optimized route displayed
- ⏱️ **Live ETA**: Updates based on PathPulse traffic data

---

### 2. **Ride Request Screen - Route Preview & ETA**

**Location:** `src/screens/Rides/RideRequestScreen.js`

**Integration Points:**
- ✅ **Pickup ETA**: PathPulse calculates driver ETA to pickup location
- ✅ **Route Preview**: Show optimized route before accepting
- ✅ **Traffic Information**: Display traffic conditions for the route

**Current Implementation:**
```javascript
// Basic ride request display
<Text variant="bodyMedium">
  Distance: {booking.tripDistance || 'N/A'}
</Text>
<Text variant="bodyMedium">
  Duration: {booking.tripDuration || 'N/A'}
</Text>
```

**PathPulse Enhancement:**
```javascript
// Enhanced with PathPulse data
const [routeData, setRouteData] = useState(null);

useEffect(() => {
  if (booking) {
    // Fetch PathPulse route data
    fetchPathPulseRoute({
      origin: currentLocation,
      destination: booking.sourceLocation,
    }).then((data) => {
      setRouteData(data);
      // data includes:
      // - optimizedRoute
      // - trafficConditions
      // - alternativeRoutes
      // - realTimeETA
    });
  }
}, [booking]);

// Display PathPulse-enhanced information
<View style={styles.routePreview}>
  <Text variant="titleSmall">Route Preview</Text>
  <Text variant="bodyMedium">
    ⏱️ ETA to Pickup: {routeData?.realTimeETA || 'Calculating...'}
  </Text>
  <Text variant="bodySmall">
    🚦 Traffic: {routeData?.trafficConditions || 'Unknown'}
  </Text>
  {routeData?.alternativeRoutes && (
    <Text variant="bodySmall">
      ✨ {routeData.alternativeRoutes.length} alternative routes available
    </Text>
  )}
</View>
```

**Visual Changes:**
- 📊 **Route Preview Card**: Shows PathPulse-optimized route
- 🚦 **Traffic Status**: Color-coded traffic conditions
- 🛣️ **Alternative Routes**: Button to view route options

---

### 3. **Active Ride Screen - Turn-by-Turn Navigation**

**Location:** `src/screens/Rides/ActiveRideScreen.js`

**Integration Points:**
- ✅ **Navigation Integration**: PathPulse provides turn-by-turn directions
- ✅ **Live Route Updates**: Route adjusts based on real-time traffic
- ✅ **ETA Accuracy**: Precise arrival times using PathPulse traffic data

**Current Implementation:**
```javascript
// Basic navigation button
<Button
  mode="outlined"
  icon="navigation"
  onPress={() => {
    // Open Google Maps navigation
  }}
>
  Navigate to Pickup
</Button>
```

**PathPulse Enhancement:**
```javascript
// Enhanced navigation with PathPulse
const [navigationData, setNavigationData] = useState(null);

useEffect(() => {
  if (activeBooking) {
    // Get PathPulse navigation data
    fetchPathPulseNavigation({
      origin: currentLocation,
      destination: activeBooking.destinationLocation,
      waypoints: activeBooking.waypoints || [],
    }).then((data) => {
      setNavigationData(data);
      // data includes:
      // - turnByTurnDirections
      // - optimizedRoute
      // - liveTrafficUpdates
      // - alternativeRoutes
    });
  }
}, [activeBooking]);

// Display PathPulse navigation
<View style={styles.navigationCard}>
  <Text variant="titleMedium">Navigation</Text>
  
  {/* Current Step */}
  <View style={styles.currentStep}>
    <Text variant="bodyLarge">
      {navigationData?.currentStep?.instruction}
    </Text>
    <Text variant="bodySmall">
      {navigationData?.currentStep?.distance} away
    </Text>
  </View>
  
  {/* Next Steps */}
  {navigationData?.upcomingSteps?.slice(0, 3).map((step, index) => (
    <View key={index} style={styles.nextStep}>
      <Text variant="bodySmall">{step.instruction}</Text>
    </View>
  ))}
  
  {/* Traffic Alert */}
  {navigationData?.trafficAlert && (
    <View style={styles.trafficAlert}>
      <Text variant="bodySmall">
        ⚠️ {navigationData.trafficAlert.message}
      </Text>
      <Button
        mode="text"
        onPress={() => {
          // Switch to alternative route
          switchToAlternativeRoute(navigationData.alternativeRoute);
        }}
      >
        Use Alternative Route
      </Button>
    </View>
  )}
</View>
```

**Visual Changes:**
- 🧭 **Turn-by-Turn Display**: PathPulse navigation instructions
- 🚦 **Traffic Alerts**: Real-time traffic warnings
- 🛣️ **Route Switching**: Quick switch to alternative routes

---

### 4. **Location Service - PathPulse Data Collection**

**Location:** `src/services/location.js`

**Integration Points:**
- ✅ **Location Streaming**: Continuous location updates to PathPulse
- ✅ **Metadata Collection**: Road conditions, speed, bearing
- ✅ **Webhook Integration**: PathPulse webhooks for route updates

**Current Implementation:**
```javascript
// Basic location tracking
export const locationService = {
  startTracking: (driverId, onLocationUpdate) => {
    // Update location every 5 seconds
    locationUpdateInterval = setInterval(async () => {
      const location = await Location.getCurrentPositionAsync();
      await driversService.updateLocation(driverId, {
        latitude: location.coords.latitude,
        longitude: location.coords.longitude,
        bearing: location.coords.heading || 0,
        speed: location.coords.speed || 0,
      });
    }, LOCATION_UPDATE_INTERVAL);
  },
};
```

**PathPulse Enhancement:**
```javascript
// Enhanced with PathPulse metadata
export const locationService = {
  startTracking: (driverId, onLocationUpdate) => {
    locationUpdateInterval = setInterval(async () => {
      const location = await Location.getCurrentPositionAsync();
      
      // Enhanced location data for PathPulse
      const pathPulseData = {
        latitude: location.coords.latitude,
        longitude: location.coords.longitude,
        bearing: location.coords.heading || 0,
        speed: location.coords.speed || 0,
        accuracy: location.coords.accuracy,
        altitude: location.coords.altitude,
        timestamp: new Date().toISOString(),
        // PathPulse-specific metadata
        metadata: {
          roadType: detectRoadType(location), // highway, city, residential
          roadCondition: detectRoadCondition(location), // smooth, rough, construction
          trafficDensity: estimateTrafficDensity(location),
        },
      };
      
      // Send to backend → forwards to PathPulse
      await driversService.updateLocation(driverId, pathPulseData);
      
      // Listen for PathPulse webhook updates
      subscribeToPathPulseUpdates((update) => {
        // Update route if PathPulse suggests alternative
        if (update.suggestedRoute) {
          onLocationUpdate({
            ...location,
            suggestedRoute: update.suggestedRoute,
          });
        }
      });
    }, LOCATION_UPDATE_INTERVAL);
  },
};
```

**Backend Integration:**
- Backend receives location → calls PathPulse API
- PathPulse processes data → sends webhook to backend
- Backend forwards updates to driver app via WebSocket/polling

---

### 5. **API Service Layer - PathPulse Endpoints**

**Location:** `src/services/api/pathpulse.js` (NEW FILE)

**Integration Points:**
- ✅ **Route Calculation**: Get optimized routes from PathPulse
- ✅ **Traffic Data**: Fetch real-time traffic conditions
- ✅ **Route Optimization**: Multi-stop route optimization

**New Service File:**
```javascript
// src/services/api/pathpulse.js
import apiClient from './client';
import { API_ENDPOINTS } from '../../utils/constants';

export const pathPulseService = {
  // Calculate route with traffic
  calculateRoute: async (origin, destination, options = {}) => {
    const response = await apiClient.post('/api/pathpulse/route', {
      origin,
      destination,
      ...options,
    });
    return response.data;
  },

  // Get traffic-aware ETA
  getTrafficETA: async (origin, destination, departureTime) => {
    const response = await apiClient.post('/api/pathpulse/eta', {
      origin,
      destination,
      departureTime,
    });
    return response.data;
  },

  // Optimize multi-stop route
  optimizeRoute: async (waypoints) => {
    const response = await apiClient.post('/api/pathpulse/optimize', {
      waypoints,
    });
    return response.data;
  },

  // Get alternative routes
  getAlternativeRoutes: async (origin, destination) => {
    const response = await apiClient.post('/api/pathpulse/alternatives', {
      origin,
      destination,
    });
    return response.data;
  },

  // Subscribe to route updates
  subscribeToUpdates: (bookingId, callback) => {
    // WebSocket or polling for real-time updates
    const ws = new WebSocket(`${API_BASE_URL}/api/pathpulse/updates/${bookingId}`);
    ws.onmessage = (event) => {
      const update = JSON.parse(event.data);
      callback(update);
    };
    return ws;
  },
};
```

**Backend Endpoints (to be implemented):**
- `POST /api/pathpulse/route` - Calculate route
- `POST /api/pathpulse/eta` - Get traffic-aware ETA
- `POST /api/pathpulse/optimize` - Optimize multi-stop route
- `POST /api/pathpulse/alternatives` - Get alternative routes
- `WebSocket /api/pathpulse/updates/:bookingId` - Real-time route updates

---

### 6. **Redux Store - PathPulse State Management**

**Location:** `src/store/slices/pathpulseSlice.js` (NEW FILE)

**Integration Points:**
- ✅ **Route State**: Store current route data
- ✅ **Traffic State**: Store traffic conditions
- ✅ **Navigation State**: Store turn-by-turn directions

**New Redux Slice:**
```javascript
// src/store/slices/pathpulseSlice.js
import { createSlice, createAsyncThunk } from '@reduxjs/toolkit';
import { pathPulseService } from '../../services/api/pathpulse';

export const fetchRoute = createAsyncThunk(
  'pathpulse/fetchRoute',
  async ({ origin, destination }, { rejectWithValue }) => {
    try {
      const data = await pathPulseService.calculateRoute(origin, destination);
      return data;
    } catch (error) {
      return rejectWithValue(error.message);
    }
  }
);

export const fetchTrafficETA = createAsyncThunk(
  'pathpulse/fetchTrafficETA',
  async ({ origin, destination, departureTime }, { rejectWithValue }) => {
    try {
      const data = await pathPulseService.getTrafficETA(
        origin,
        destination,
        departureTime
      );
      return data;
    } catch (error) {
      return rejectWithValue(error.message);
    }
  }
);

const pathPulseSlice = createSlice({
  name: 'pathpulse',
  initialState: {
    currentRoute: null,
    trafficETA: null,
    alternativeRoutes: [],
    trafficConditions: null,
    navigationSteps: [],
    isLoading: false,
    error: null,
  },
  reducers: {
    setCurrentRoute: (state, action) => {
      state.currentRoute = action.payload;
    },
    clearRoute: (state) => {
      state.currentRoute = null;
      state.navigationSteps = [];
    },
  },
  extraReducers: (builder) => {
    builder
      .addCase(fetchRoute.pending, (state) => {
        state.isLoading = true;
      })
      .addCase(fetchRoute.fulfilled, (state, action) => {
        state.isLoading = false;
        state.currentRoute = action.payload.route;
        state.navigationSteps = action.payload.steps;
        state.alternativeRoutes = action.payload.alternatives || [];
      })
      .addCase(fetchRoute.rejected, (state, action) => {
        state.isLoading = false;
        state.error = action.payload;
      });
  },
});

export const { setCurrentRoute, clearRoute } = pathPulseSlice.actions;
export default pathPulseSlice.reducer;
```

---

## 🎨 UI/UX Enhancements

### Visual Indicators for PathPulse Features

**1. Traffic Status Badge**
```javascript
// Component: TrafficStatusBadge.js
<TrafficStatusBadge
  condition={routeData?.trafficConditions}
  // Displays: 🟢 Light | 🟡 Moderate | 🔴 Heavy
/>
```

**2. Route Optimization Indicator**
```javascript
// Shows when PathPulse has optimized the route
<View style={styles.optimizationBadge}>
  <Icon name="sparkles" />
  <Text>PathPulse Optimized Route</Text>
  <Text variant="caption">
    Saves {optimizationData.savedTime} minutes
  </Text>
</View>
```

**3. Alternative Route Selector**
```javascript
// Component: AlternativeRoutes.js
{alternativeRoutes.map((route, index) => (
  <RouteOption
    key={index}
    route={route}
    onSelect={() => switchToRoute(route)}
  />
))}
```

---

## 🔄 Data Flow Diagram

```
Driver App                    Backend API              PathPulse.ai
─────────────────            ─────────────            ──────────────

1. Driver goes online
   │
   ├─► POST /api/drivers/:id/status
   │   { isOnline: true }
   │                           │
   │                           ├─► Start location tracking
   │                           │
   │                           └─► Register with PathPulse
   │
2. Location updates (every 5s)
   │
   ├─► PATCH /api/drivers/:id/location
   │   { lat, lng, speed, bearing }
   │                           │
   │                           ├─► Store in database
   │                           │
   │                           └─► POST PathPulse API
   │                               └─► PathPulse processes
   │                                   └─► Webhook → Backend
   │                                       └─► WebSocket → Driver App
   │
3. Ride request received
   │
   ├─► GET /api/bookings
   │                           │
   │                           ├─► Calculate route (PathPulse)
   │                           │   └─► GET PathPulse /route
   │                           │       └─► Return optimized route
   │                           │
   │                           └─► Return booking + route data
   │
4. Driver accepts ride
   │
   ├─► POST /api/bookings/confirm-acceptance
   │                           │
   │                           ├─► Update booking status
   │                           │
   │                           └─► POST PathPulse /driver-action
   │                               { action: 'accept', bookingId }
   │
5. Active ride navigation
   │
   ├─► GET /api/pathpulse/route
   │   { origin, destination }
   │                           │
   │                           ├─► POST PathPulse /route
   │                           │   └─► Return turn-by-turn
   │                           │
   │                           └─► Return route data
   │
6. Real-time route updates
   │
   ├─► WebSocket connection
   │   /api/pathpulse/updates/:bookingId
   │                           │
   │                           ├─► Listen for PathPulse webhooks
   │                           │   └─► Forward to driver app
   │                           │
   │                           └─► Route update received
   │                               └─► Update map display
```

---

## 📋 Implementation Checklist

### Phase 1: Basic Integration (Week 1-2)
- [ ] Create `pathpulseService.js` API client
- [ ] Create `pathpulseSlice.js` Redux slice
- [ ] Update `HomeScreen.js` to display PathPulse route data
- [ ] Update `RideRequestScreen.js` to show traffic-aware ETA
- [ ] Update `ActiveRideScreen.js` with PathPulse navigation

### Phase 2: Enhanced Features (Week 3-4)
- [ ] Add traffic overlay to map
- [ ] Implement alternative route selector
- [ ] Add route optimization indicators
- [ ] Create traffic status badges
- [ ] Implement WebSocket for real-time updates

### Phase 3: Advanced Features (Week 5-6)
- [ ] Multi-stop route optimization
- [ ] Historical traffic pattern display
- [ ] Route comparison (PathPulse vs Google Maps)
- [ ] Driver feedback on route quality
- [ ] Analytics dashboard for route performance

---

## 🔗 Related Documentation

- **Backend Integration:** `/ride-scheduler-be/routes/driverSignalRoutes.js`
- **PathPulse Executive Summary:** `/PathPulse_Integration_Executive_Summary.md`
- **PathPulse Architecture:** `/PathPulse_Architecture_Diagram.md`
- **Driver App Plan:** `/DRIVER_APP_DEVELOPMENT_PLAN.md`

---

## 📞 Next Steps

1. **Backend Implementation**: Implement PathPulse API endpoints in backend
2. **API Testing**: Test PathPulse integration with sandbox credentials
3. **UI Components**: Create PathPulse-specific UI components
4. **State Management**: Integrate PathPulse Redux slice
5. **Testing**: Test with real routes in Durban

---

**Status:** 📋 Planning Complete - Ready for Implementation  
**Last Updated:** January 2025

