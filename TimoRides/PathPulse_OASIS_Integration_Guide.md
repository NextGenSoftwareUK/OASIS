# PathPulse.ai × TimoRides/OASIS Integration Guide

**Date:** October 20, 2025  
**For:** TimoRides - PathPulse.ai Conversations  
**Version:** 1.0

---

## 📋 Executive Summary

This document outlines how **PathPulse.ai** would integrate with the **TimoRides/OASIS** platform. PathPulse.ai can be integrated as a **routing and optimization provider** within the OASIS architecture, offering TimoRides advanced routing, ETA calculations, and pathfinding capabilities as an alternative or complement to existing solutions (Google Maps Distance Matrix API, DistanceMatrix.ai).

### Key Benefits of Integration

- **Provider Abstraction**: Switch between PathPulse.ai, Google Maps, Mapbox, or other routing providers seamlessly
- **Auto-Failover**: OASIS HyperDrive automatically fails over to backup providers if PathPulse.ai is unavailable
- **Cost Optimization**: Mix and match routing providers based on cost, performance, and features
- **Enhanced Routing**: Access PathPulse.ai's advanced routing algorithms and optimization capabilities
- **Driver Data Monetization**: Drivers earn passive income by sharing anonymized driving data with PathPulse
- **Data Ownership**: Drivers maintain control over their data through OASIS Avatar system
- **Future-Proof**: Easy to add additional providers or upgrade PathPulse.ai features without refactoring TimoRides

---

## 💰 Driver Data Revenue Sharing Model

### Overview: Turning Drivers into Data Contributors

One of the most innovative aspects of the PathPulse × TimoRides integration is the ability for **drivers to earn additional revenue** by sharing their driving data with PathPulse.ai. This creates a **Web3-native data marketplace** where drivers maintain ownership and control of their data while earning passive income.

### How It Works

```
┌──────────────────────────────────────────────────────────────┐
│                    Driver Data Flow                          │
└──────────────────────────────────────────────────────────────┘

1. Driver opts-in to data sharing
   ├─ Accepts data sharing terms
   ├─ Sets data sharing preferences
   └─ Links wallet for micropayments

2. Driver's device (mobile app) collects data during rides
   ├─ GPS coordinates (anonymized)
   ├─ Speed, acceleration, braking patterns
   ├─ Route choices and traffic conditions
   ├─ Road quality, potholes, obstacles
   └─ Time of day, weather conditions

3. Data stored on Driver's OASIS Avatar
   ├─ Encrypted and attached to driver's Avatar
   ├─ Driver maintains ownership
   ├─ Stored across multiple OASIS providers (MongoDB, IPFS, Holochain)
   └─ Versioned and immutable (blockchain-backed)

4. PathPulse requests data access
   ├─ PathPulse sends data request to OASIS API
   ├─ Driver receives notification
   ├─ Driver approves/rejects request (one-time or recurring)
   └─ Smart contract governs data access terms

5. Data shared with PathPulse
   ├─ Anonymized and aggregated
   ├─ PathPulse improves routing algorithms
   ├─ Benefits all TimoRides users with better routes
   └─ PathPulse pays for data access

6. Driver gets paid
   ├─ Micropayments to driver's wallet (Avatar-linked)
   ├─ Paid in local currency, USDC, or mobile money
   ├─ Automatic monthly/weekly payouts
   └─ Driver earns karma for contributing to ecosystem
```

### Data Types Drivers Can Share

#### Core Driving Data
- **Route History**: Actual routes taken (anonymized GPS trails)
- **Traffic Patterns**: Real-time traffic observations
- **Speed Data**: Average speeds on different road segments
- **Road Conditions**: Potholes, construction, road quality
- **Parking Availability**: Where parking was found
- **Wait Times**: Pickup wait times, drop-off times

#### Advanced Data (Optional, Higher Value)
- **Fuel Efficiency**: Fuel consumption per route
- **Weather Impact**: How weather affects route times
- **Event-Based Traffic**: Traffic patterns during events
- **Safety Data**: Hard braking, near-misses (for safety AI)
- **Rider Preferences**: Preferred routes (with rider consent)

### Revenue Model

#### Option A: Pay-Per-Data-Point
```
Data Type                     Value per Point    Monthly Potential
────────────────────────────────────────────────────────────────
GPS coordinate (per km)       $0.0001           $5-15 (avg driver)
Traffic observation           $0.001            $10-30
Road condition report         $0.01             $20-60
Complete ride data            $0.10             $100-300 (active driver)
────────────────────────────────────────────────────────────────
```

#### Option B: Revenue Share Model
- PathPulse pays **2-5% of revenue** generated from improved routing
- Drivers collectively earn percentage of PathPulse's TimoRides earnings
- More equitable for high-contribution drivers
- Incentivizes quality data sharing

#### Option C: Hybrid Model (Recommended)
- **Base Rate**: $0.10 per complete ride data shared
- **Bonus**: 2% revenue share from PathPulse improvements
- **Karma Rewards**: Earn 10 karma points per ride shared
- **Unlocks**: Premium driver features at 1000 karma points

### Example Driver Earnings

**Scenario: Active Driver (200 rides/month)**
```
Base Data Sharing:     200 rides × $0.10    = $20.00
Traffic Reports:       150 reports × $0.001 = $0.15
Road Conditions:       10 reports × $0.01   = $0.10
Revenue Share:         2% × $50 earned      = $1.00
────────────────────────────────────────────────────
Total Monthly:                                $21.25
Annual:                                       $255.00

Plus: 2,000 karma points/month
```

**Scenario: Part-Time Driver (50 rides/month)**
```
Base Data Sharing:     50 rides × $0.10     = $5.00
Traffic Reports:       30 reports × $0.001  = $0.03
Revenue Share:         2% × $10 earned      = $0.20
────────────────────────────────────────────────────
Total Monthly:                                $5.23
Annual:                                       $62.76

Plus: 500 karma points/month
```

### Privacy & Control

#### Driver Data Rights
1. **Ownership**: Driver owns all data generated
2. **Opt-In**: Must explicitly consent to sharing
3. **Granular Control**: Choose what data to share
4. **Revoke Anytime**: Can stop sharing at any moment
5. **Anonymization**: All personal data anonymized
6. **Transparency**: See exactly what's being shared

#### Privacy Protection
- **No Personal Information**: Names, addresses, rider info never shared
- **GPS Anonymization**: Location data anonymized to 100m radius
- **Aggregation**: Data combined with other drivers before PathPulse access
- **Differential Privacy**: Mathematical privacy guarantees
- **GDPR/POPIA Compliant**: Meets all data protection regulations

---

## 🏗️ OASIS Provider Architecture Overview

### What is an OASIS Provider?

OASIS uses a **provider architecture** that allows multiple service providers (blockchain, storage, mapping, routing) to be plugged in, configured, and switched dynamically. Each provider implements standard interfaces that the OASIS API can call.

### Provider Types

1. **Storage Providers**: MongoDB, SQLite, IPFS, Neo4j, AzureCosmosDB
2. **Blockchain Providers**: Ethereum, Solana, Arbitrum, Polygon, Base
3. **Map Providers**: Mapbox, GOMap, Google Maps (existing)
4. **Routing/Navigation Providers**: **← PathPulse.ai would fit here**

### Key Provider Interfaces

All OASIS providers implement the `IOASISProvider` interface:

```csharp
public interface IOASISProvider
{
    string ProviderName { get; set; }
    string ProviderDescription { get; set; }
    EnumValue<ProviderCategory> ProviderCategory { get; set; }
    EnumValue<ProviderType> ProviderType { get; set; }
    bool IsProviderActivated { get; set; }
    
    OASISResult<bool> ActivateProvider();
    Task<OASISResult<bool>> ActivateProviderAsync();
    OASISResult<bool> DeActivateProvider();
    Task<OASISResult<bool>> DeActivateProviderAsync();
}
```

For mapping/routing specifically, providers implement `IOASISMapProvider`:

```csharp
public interface IOASISMapProvider
{
    MapProviderType MapProviderType { get; set; }
    string MapProviderName { get; set; }
    string MapProviderDescription { get; set; }
    
    // Routing Methods
    bool CreateAndDrawRouteOnMapBetweenHolons(IHolon fromHolon, IHolon toHolon);
    bool CreateAndDrawRouteOnMapBeweenPoints(MapPoints points);
    bool DrawRouteOnMap(float startX, float startY, float endX, float endY);
    
    // Map Interaction Methods
    bool Draw2DSpriteOnHUD(object sprite, float x, float y);
    bool Draw2DSpriteOnMap(object sprite, float x, float y);
    bool Draw3DObjectOnMap(object obj, float x, float y);
    
    // Navigation Methods
    bool PanMapDown(float value);
    bool PanMapUp(float value);
    bool PanMapLeft(float value);
    bool PanMapRight(float value);
    bool ZoomMapIn(float value);
    bool ZoomMapOut(float value);
    bool ZoomToHolonOnMap(IHolon holon);
    
    // Selection Methods
    bool SelectHolonOnMap(IHolon holon);
    bool SelectBuildingOnMap(IBuilding building);
    bool HighlightBuildingOnMap(IBuilding building);
}
```

---

## 🔌 PathPulse Integration Approaches

There are **two primary approaches** for integrating PathPulse.ai:

### Approach 1: Full OASIS Provider (Recommended for Long-Term)

Create a dedicated `PathPulseOASIS` provider that fully integrates with the OASIS ecosystem.

**Benefits:**
- Native OASIS integration
- Works with OASIS HyperDrive (auto-failover)
- Usable across all OASIS-based projects (not just TimoRides)
- Standardized provider registration and configuration

**Implementation Location:**
```
/Volumes/Storage/OASIS_CLEAN/Providers/
  └── NextGenSoftware.OASIS.API.Providers.PathPulseOASIS/
      ├── PathPulseOASIS.cs
      ├── Services/
      │   ├── IPathPulseRoutingService.cs
      │   ├── PathPulseRoutingService.cs
      │   ├── IPathPulseOptimizationService.cs
      │   └── PathPulseOptimizationService.cs
      ├── Models/
      │   ├── PathPulseRoute.cs
      │   ├── PathPulseETA.cs
      │   └── PathPulseOptimizationRequest.cs
      ├── Enums/
      │   ├── PathPulseRoutingProfile.cs
      │   └── PathPulseOptimizationType.cs
      ├── NextGenSoftware.OASIS.API.Providers.PathPulseOASIS.csproj
      └── README.md
```

### Approach 2: TimoRides Service Layer (Faster MVP)

Create a service layer within TimoRides that abstracts PathPulse.ai and can be upgraded to a full OASIS provider later.

**Benefits:**
- Faster initial implementation
- Works immediately with existing Node.js backend
- Can be extracted into OASIS provider later

**Implementation Location:**
```
/Volumes/Storage/OASIS_CLEAN/TimoRides/
  └── ride-scheduler-be/
      └── services/
          ├── pathPulseService.js
          └── routingService.js (abstraction layer)
```

---

## 📐 Technical Implementation

### Option 1: PathPulseOASIS Provider (C#/.NET)

#### Provider Class Structure

```csharp
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.PathPulseOASIS
{
    public class PathPulseOASIS : IOASISMapProvider, IOASISProvider
    {
        // IOASISProvider Properties
        public string ProviderName { get; set; } = "PathPulse.ai";
        public string ProviderDescription { get; set; } = "PathPulse.ai routing and optimization provider";
        public EnumValue<ProviderCategory> ProviderCategory { get; set; } = new EnumValue<ProviderCategory>(Enums.ProviderCategory.Mapping);
        public EnumValue<ProviderType> ProviderType { get; set; } = new EnumValue<ProviderType>(Enums.ProviderType.PathPulseOASIS);
        public bool IsProviderActivated { get; set; }
        
        // IOASISMapProvider Properties
        public MapProviderType MapProviderType { get; set; } = MapProviderType.PathPulse;
        public string MapProviderName { get; set; } = "PathPulse";
        public string MapProviderDescription { get; set; } = "PathPulse.ai intelligent routing provider";
        
        // PathPulse Configuration
        private string _apiKey;
        private string _baseUrl;
        private HttpClient _httpClient;
        
        public PathPulseOASIS(string apiKey, string baseUrl = "https://api.pathpulse.ai")
        {
            _apiKey = apiKey;
            _baseUrl = baseUrl;
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");
        }
        
        // Activation Methods
        public OASISResult<bool> ActivateProvider()
        {
            try
            {
                // Test API connection
                var testResult = TestConnection();
                if (testResult.IsSuccess)
                {
                    IsProviderActivated = true;
                    return new OASISResult<bool>(true);
                }
                return new OASISResult<bool>(false) { Message = "Failed to activate PathPulse provider" };
            }
            catch (Exception ex)
            {
                return new OASISResult<bool>(false) { Message = ex.Message };
            }
        }
        
        public async Task<OASISResult<bool>> ActivateProviderAsync()
        {
            try
            {
                var testResult = await TestConnectionAsync();
                if (testResult.IsSuccess)
                {
                    IsProviderActivated = true;
                    return new OASISResult<bool>(true);
                }
                return new OASISResult<bool>(false) { Message = "Failed to activate PathPulse provider" };
            }
            catch (Exception ex)
            {
                return new OASISResult<bool>(false) { Message = ex.Message };
            }
        }
        
        public OASISResult<bool> DeActivateProvider()
        {
            IsProviderActivated = false;
            return new OASISResult<bool>(true);
        }
        
        public async Task<OASISResult<bool>> DeActivateProviderAsync()
        {
            IsProviderActivated = false;
            return await Task.FromResult(new OASISResult<bool>(true));
        }
        
        // Core Routing Methods
        public async Task<PathPulseRoute> CalculateRouteAsync(double startLat, double startLng, double endLat, double endLng, PathPulseRoutingProfile profile = PathPulseRoutingProfile.Driving)
        {
            if (!IsProviderActivated)
                throw new InvalidOperationException("Provider not activated");
                
            var request = new
            {
                origin = new { lat = startLat, lng = startLng },
                destination = new { lat = endLat, lng = endLng },
                profile = profile.ToString().ToLower(),
                alternatives = true,
                steps = true,
                overview = "full"
            };
            
            var response = await _httpClient.PostAsJsonAsync($"{_baseUrl}/v1/route", request);
            response.EnsureSuccessStatusCode();
            
            return await response.Content.ReadFromJsonAsync<PathPulseRoute>();
        }
        
        public async Task<PathPulseETA> CalculateETAAsync(double startLat, double startLng, double endLat, double endLng)
        {
            if (!IsProviderActivated)
                throw new InvalidOperationException("Provider not activated");
                
            var request = new
            {
                origin = new { lat = startLat, lng = startLng },
                destination = new { lat = endLat, lng = endLng },
                departure_time = "now"
            };
            
            var response = await _httpClient.PostAsJsonAsync($"{_baseUrl}/v1/eta", request);
            response.EnsureSuccessStatusCode();
            
            return await response.Content.ReadFromJsonAsync<PathPulseETA>();
        }
        
        public async Task<double> CalculateDistanceAsync(double startLat, double startLng, double endLat, double endLng)
        {
            var route = await CalculateRouteAsync(startLat, startLng, endLat, endLng);
            return route.Distance; // in kilometers
        }
        
        // Multi-Stop Optimization
        public async Task<PathPulseOptimizedRoute> OptimizeMultiStopRouteAsync(List<Coordinate> waypoints, PathPulseOptimizationType optimizationType = PathPulseOptimizationType.Time)
        {
            if (!IsProviderActivated)
                throw new InvalidOperationException("Provider not activated");
                
            var request = new
            {
                waypoints = waypoints.Select(w => new { lat = w.Latitude, lng = w.Longitude }),
                optimization = optimizationType.ToString().ToLower(),
                return_to_start = false
            };
            
            var response = await _httpClient.PostAsJsonAsync($"{_baseUrl}/v1/optimize", request);
            response.EnsureSuccessStatusCode();
            
            return await response.Content.ReadFromJsonAsync<PathPulseOptimizedRoute>();
        }
        
        // IOASISMapProvider Implementation
        public bool DrawRouteOnMap(float startX, float startY, float endX, float endY)
        {
            try
            {
                var route = CalculateRouteAsync(startX, startY, endX, endY).Result;
                // Draw route on map (implementation depends on frontend)
                return route != null;
            }
            catch
            {
                return false;
            }
        }
        
        public bool CreateAndDrawRouteOnMapBeweenPoints(MapPoints points)
        {
            try
            {
                // Convert MapPoints to PathPulse waypoints and optimize
                var waypoints = points.Points.Select(p => new Coordinate { Latitude = p.Lat, Longitude = p.Lng }).ToList();
                var optimizedRoute = OptimizeMultiStopRouteAsync(waypoints).Result;
                return optimizedRoute != null;
            }
            catch
            {
                return false;
            }
        }
        
        // Helper Methods
        private OASISResult<bool> TestConnection()
        {
            try
            {
                var response = _httpClient.GetAsync($"{_baseUrl}/health").Result;
                return new OASISResult<bool>(response.IsSuccessStatusCode);
            }
            catch (Exception ex)
            {
                return new OASISResult<bool>(false) { Message = ex.Message };
            }
        }
        
        private async Task<OASISResult<bool>> TestConnectionAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/health");
                return new OASISResult<bool>(response.IsSuccessStatusCode);
            }
            catch (Exception ex)
            {
                return new OASISResult<bool>(false) { Message = ex.Message };
            }
        }
    }
}
```

---

### Option 2: TimoRides Node.js Service Layer

#### Service Implementation

**File:** `/TimoRides/ride-scheduler-be/services/pathPulseService.js`

```javascript
const axios = require('axios');

class PathPulseService {
  constructor() {
    this.apiKey = process.env.PATHPULSE_API_KEY;
    this.baseUrl = process.env.PATHPULSE_BASE_URL || 'https://api.pathpulse.ai';
    this.client = axios.create({
      baseURL: this.baseUrl,
      headers: {
        'Authorization': `Bearer ${this.apiKey}`,
        'Content-Type': 'application/json'
      }
    });
  }

  /**
   * Calculate route between two points
   * @param {Object} origin - { lat, lng }
   * @param {Object} destination - { lat, lng }
   * @param {string} profile - 'driving' | 'walking' | 'cycling'
   * @returns {Promise<Object>} Route details with distance, duration, and path
   */
  async calculateRoute(origin, destination, profile = 'driving') {
    try {
      const response = await this.client.post('/v1/route', {
        origin,
        destination,
        profile,
        alternatives: true,
        steps: true,
        overview: 'full'
      });
      
      return {
        distance: response.data.distance, // in km
        duration: response.data.duration, // in seconds
        durationText: this.formatDuration(response.data.duration),
        distanceText: `${response.data.distance} km`,
        path: response.data.geometry, // GeoJSON or polyline
        alternatives: response.data.alternatives || []
      };
    } catch (error) {
      console.error('PathPulse route calculation error:', error);
      throw error;
    }
  }

  /**
   * Calculate ETA between two points with traffic
   * @param {Object} origin - { lat, lng }
   * @param {Object} destination - { lat, lng }
   * @returns {Promise<Object>} ETA details
   */
  async calculateETA(origin, destination) {
    try {
      const response = await this.client.post('/v1/eta', {
        origin,
        destination,
        departure_time: 'now'
      });
      
      return {
        distance: response.data.distance,
        duration: response.data.duration,
        durationInTraffic: response.data.duration_in_traffic,
        durationText: this.formatDuration(response.data.duration),
        trafficCondition: response.data.traffic_condition // 'light' | 'moderate' | 'heavy'
      };
    } catch (error) {
      console.error('PathPulse ETA calculation error:', error);
      throw error;
    }
  }

  /**
   * Calculate distance between two points
   * @param {Object} origin - { lat, lng }
   * @param {Object} destination - { lat, lng }
   * @returns {Promise<number>} Distance in kilometers
   */
  async calculateDistance(origin, destination) {
    const route = await this.calculateRoute(origin, destination);
    return route.distance;
  }

  /**
   * Optimize multi-stop route
   * @param {Array<Object>} waypoints - Array of { lat, lng }
   * @param {string} optimizationType - 'time' | 'distance' | 'balanced'
   * @returns {Promise<Object>} Optimized route
   */
  async optimizeMultiStopRoute(waypoints, optimizationType = 'time') {
    try {
      const response = await this.client.post('/v1/optimize', {
        waypoints,
        optimization: optimizationType,
        return_to_start: false
      });
      
      return {
        optimizedOrder: response.data.optimized_order,
        totalDistance: response.data.total_distance,
        totalDuration: response.data.total_duration,
        routes: response.data.routes,
        savings: {
          distanceSaved: response.data.savings?.distance_saved || 0,
          timeSaved: response.data.savings?.time_saved || 0
        }
      };
    } catch (error) {
      console.error('PathPulse optimization error:', error);
      throw error;
    }
  }

  /**
   * Format duration in seconds to human-readable text
   * @param {number} seconds 
   * @returns {string}
   */
  formatDuration(seconds) {
    const hours = Math.floor(seconds / 3600);
    const minutes = Math.floor((seconds % 3600) / 60);
    
    if (hours > 0) {
      return `${hours} hour${hours > 1 ? 's' : ''} ${minutes} min${minutes !== 1 ? 's' : ''}`;
    }
    return `${minutes} min${minutes !== 1 ? 's' : ''}`;
  }

  /**
   * Test API connection
   * @returns {Promise<boolean>}
   */
  async testConnection() {
    try {
      await this.client.get('/health');
      return true;
    } catch (error) {
      console.error('PathPulse connection test failed:', error);
      return false;
    }
  }
}

module.exports = PathPulseService;
```

---

#### Routing Abstraction Layer

**File:** `/TimoRides/ride-scheduler-be/services/routingService.js`

```javascript
const PathPulseService = require('./pathPulseService');
const { getTravelTime, distanceCalculateMetrix } = require('../utils/distanceCalculation');

class RoutingService {
  constructor() {
    this.primaryProvider = process.env.ROUTING_PROVIDER || 'pathpulse'; // 'pathpulse' | 'google' | 'distancematrix'
    this.fallbackProvider = process.env.ROUTING_FALLBACK_PROVIDER || 'google';
    
    // Initialize providers
    this.pathPulse = new PathPulseService();
  }

  /**
   * Calculate distance and duration between two points with auto-fallback
   * @param {Object} origin - { lat, lng }
   * @param {Object} destination - { lat, lng }
   * @returns {Promise<Object>} { distance, duration, distanceNotation }
   */
  async calculateRoute(origin, destination) {
    try {
      // Try primary provider
      if (this.primaryProvider === 'pathpulse') {
        return await this._calculateRoutePathPulse(origin, destination);
      } else if (this.primaryProvider === 'google') {
        return await this._calculateRouteGoogle(origin, destination);
      } else if (this.primaryProvider === 'distancematrix') {
        return await this._calculateRouteDistanceMatrix(origin, destination);
      }
    } catch (error) {
      console.error(`Primary routing provider (${this.primaryProvider}) failed:`, error);
      
      // Fallback to secondary provider
      try {
        console.log(`Falling back to ${this.fallbackProvider}`);
        if (this.fallbackProvider === 'google') {
          return await this._calculateRouteGoogle(origin, destination);
        } else if (this.fallbackProvider === 'distancematrix') {
          return await this._calculateRouteDistanceMatrix(origin, destination);
        }
      } catch (fallbackError) {
        console.error(`Fallback routing provider (${this.fallbackProvider}) failed:`, fallbackError);
        throw new Error('All routing providers failed');
      }
    }
  }

  async _calculateRoutePathPulse(origin, destination) {
    const route = await this.pathPulse.calculateRoute(origin, destination);
    return {
      distance: route.distance,
      duration: route.durationText,
      distanceNotation: 'km',
      provider: 'pathpulse',
      path: route.path // For drawing route on map
    };
  }

  async _calculateRouteGoogle(origin, destination) {
    const result = await getTravelTime(origin, destination);
    return {
      distance: result.distance,
      duration: result.duration,
      distanceNotation: result.distanceNotation,
      provider: 'google'
    };
  }

  async _calculateRouteDistanceMatrix(origin, destination) {
    const result = await distanceCalculateMetrix(origin, destination);
    return {
      distance: result.distance,
      duration: result.duration,
      distanceNotation: 'km',
      provider: 'distancematrix'
    };
  }

  /**
   * Calculate ETA with traffic
   * @param {Object} origin 
   * @param {Object} destination 
   * @returns {Promise<Object>}
   */
  async calculateETA(origin, destination) {
    if (this.primaryProvider === 'pathpulse') {
      return await this.pathPulse.calculateETA(origin, destination);
    }
    // Fallback to regular route calculation
    return await this.calculateRoute(origin, destination);
  }

  /**
   * Optimize route for multiple stops (e.g., driver picking up multiple riders)
   * @param {Array<Object>} waypoints 
   * @returns {Promise<Object>}
   */
  async optimizeMultiStopRoute(waypoints) {
    if (this.primaryProvider === 'pathpulse') {
      return await this.pathPulse.optimizeMultiStopRoute(waypoints);
    }
    // For other providers, return waypoints in original order
    return {
      optimizedOrder: waypoints.map((_, i) => i),
      totalDistance: null,
      totalDuration: null
    };
  }
}

module.exports = new RoutingService();
```

---

## 🔧 Configuration

### OASIS DNA Configuration (for Full Provider)

**File:** `/NextGenSoftware.OASIS.API.ONODE.WebAPI/OASIS_DNA.json`

```json
{
  "StorageProviders": {
    "PathPulseOASIS": {
      "ApiKey": "your_pathpulse_api_key_here",
      "BaseUrl": "https://api.pathpulse.ai",
      "DefaultProfile": "driving",
      "EnableTrafficData": true,
      "EnableOptimization": true,
      "MaxWaypointsPerRequest": 25,
      "RequestTimeout": 30,
      "RetryAttempts": 3
    }
  }
}
```

### TimoRides Environment Variables

**File:** `/TimoRides/ride-scheduler-be/.env`

```bash
# Routing Configuration
ROUTING_PROVIDER=pathpulse  # 'pathpulse' | 'google' | 'distancematrix'
ROUTING_FALLBACK_PROVIDER=google

# PathPulse.ai Configuration
PATHPULSE_API_KEY=your_pathpulse_api_key
PATHPULSE_BASE_URL=https://api.pathpulse.ai
PATHPULSE_ENABLE_TRAFFIC=true
PATHPULSE_ENABLE_OPTIMIZATION=true

# Existing providers (keep as fallback)
DISTANCE_METRIX=your_distancematrix_key
GOOGLE_CLOUD=your_google_maps_key
```

---

## 📊 Integration with TimoRides Features

### 1. Ride Distance Calculation

**Current Implementation:**
```javascript
// TimoRides/ride-scheduler-be/utils/distanceCalculation.js
async function getTravelTime(origin, destination, res) {
  const url = `https://maps.googleapis.com/maps/api/distancematrix/json?...`;
  const response = await axios.get(url);
  // ...
}
```

**With PathPulse Integration:**
```javascript
// TimoRides/ride-scheduler-be/controllers/distantController.js
const routingService = require('../services/routingService');

async function calculateDistance(req, res) {
  const { origin, destination } = req.body;
  
  try {
    const route = await routingService.calculateRoute(origin, destination);
    res.json({
      distance: route.distance,
      duration: route.duration,
      distanceNotation: route.distanceNotation,
      provider: route.provider
    });
  } catch (error) {
    res.status(500).json({ message: 'Distance calculation failed', error: error.message });
  }
}
```

### 2. ETA Calculation for Live Rides

**Use Case:** Show riders how long until driver arrives

```javascript
// TimoRides/ride-scheduler-be/controllers/bookingController.js
const routingService = require('../services/routingService');

async function calculateDriverETA(req, res) {
  const { driverLocation, riderLocation } = req.body;
  
  try {
    const eta = await routingService.calculateETA(driverLocation, riderLocation);
    res.json({
      eta: eta.durationInTraffic || eta.duration,
      distance: eta.distance,
      trafficCondition: eta.trafficCondition
    });
  } catch (error) {
    res.status(500).json({ message: 'ETA calculation failed', error: error.message });
  }
}
```

### 3. Multi-Stop Route Optimization

**Use Case:** Driver picks up multiple riders or makes multiple stops

```javascript
const routingService = require('../services/routingService');

async function optimizeMultiPickupRoute(req, res) {
  const { driverLocation, pickupLocations } = req.body;
  
  try {
    const waypoints = [driverLocation, ...pickupLocations];
    const optimizedRoute = await routingService.optimizeMultiStopRoute(waypoints);
    
    res.json({
      optimizedOrder: optimizedRoute.optimizedOrder,
      totalDistance: optimizedRoute.totalDistance,
      totalDuration: optimizedRoute.totalDuration,
      savings: optimizedRoute.savings
    });
  } catch (error) {
    res.status(500).json({ message: 'Route optimization failed', error: error.message });
  }
}
```

---

## 🚀 Implementation Roadmap

### Phase 1: Quick Integration (Week 1-2)

1. **Create PathPulse Service Layer**
   - Implement `pathPulseService.js`
   - Create `routingService.js` abstraction layer
   - Add environment variables

2. **Replace Distance Calculation**
   - Update `distantController.js` to use `routingService`
   - Test with PathPulse as primary, Google as fallback

3. **Testing**
   - Unit tests for PathPulse service
   - Integration tests for routing service
   - Load testing with real ride data

### Phase 2: Advanced Features (Week 3-4)

1. **ETA with Traffic**
   - Integrate PathPulse traffic-aware ETA
   - Display real-time traffic conditions to riders

2. **Route Optimization**
   - Implement multi-stop optimization
   - Add optimization to ride matching algorithm

3. **Frontend Integration**
   - Display optimized routes on map
   - Show alternative routes to riders

### Phase 3: Full OASIS Provider (Week 5-8)

1. **Create PathPulseOASIS Provider**
   - Implement in C#/.NET
   - Add to OASIS provider ecosystem

2. **OASIS Integration**
   - Register provider in OASIS DNA
   - Enable HyperDrive auto-failover

3. **Documentation & Testing**
   - Full provider documentation
   - TestHarness, UnitTests, IntegrationTests

---

## 💰 Cost Optimization Strategy

### Provider Cost Comparison Matrix

| Provider | Cost per 1K Requests | Traffic Data | Optimization | Coverage |
|----------|---------------------|--------------|--------------|----------|
| PathPulse.ai | **TBD** (negotiate) | ✅ Yes | ✅ Yes | Regional/Global |
| Google Maps | $5-$10 | ✅ Yes | ⚠️ Limited | Global |
| DistanceMatrix.ai | $1-$2 | ⚠️ Limited | ❌ No | Global |
| OSM/Self-Hosted | Free | ❌ No | ❌ No | Global |

### Intelligent Provider Selection

```javascript
class RoutingService {
  selectProvider(requestType, userTier) {
    // Premium users get PathPulse with traffic + optimization
    if (userTier === 'premium') {
      return 'pathpulse';
    }
    
    // Multi-stop optimization requires PathPulse
    if (requestType === 'multi-stop') {
      return 'pathpulse';
    }
    
    // Simple distance calculations use cheaper provider
    if (requestType === 'simple-distance') {
      return 'distancematrix';
    }
    
    return 'pathpulse'; // default
  }
}
```

---

## 🔐 Security & API Key Management

### Best Practices

1. **Environment Variables**: Store PathPulse API keys in `.env` files
2. **Key Rotation**: Implement API key rotation every 90 days
3. **Rate Limiting**: Configure rate limits to avoid unexpected costs
4. **Monitoring**: Track API usage and costs in real-time
5. **Fallback Keys**: Maintain backup API keys for failover

### OASIS Provider Security

```csharp
public class PathPulseOASIS : IOASISProvider
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<PathPulseOASIS> _logger;
    
    public PathPulseOASIS(IConfiguration configuration, ILogger<PathPulseOASIS> logger)
    {
        _configuration = configuration;
        _logger = logger;
        
        // Load API key from secure configuration
        _apiKey = _configuration["PathPulse:ApiKey"] ?? 
                  Environment.GetEnvironmentVariable("PATHPULSE_API_KEY");
        
        if (string.IsNullOrEmpty(_apiKey))
        {
            _logger.LogWarning("PathPulse API key not configured");
        }
    }
}
```

---

## 📈 Monitoring & Analytics

### Key Metrics to Track

1. **Provider Performance**
   - Average response time per provider
   - Success rate per provider
   - Fallback trigger frequency

2. **Cost Tracking**
   - Requests per provider
   - Cost per ride
   - Monthly API spend

3. **Quality Metrics**
   - Route accuracy
   - ETA accuracy (predicted vs actual)
   - User satisfaction with routes

### Implementation

```javascript
class RoutingService {
  async calculateRoute(origin, destination) {
    const startTime = Date.now();
    const provider = this.primaryProvider;
    
    try {
      const result = await this._calculateRoutePathPulse(origin, destination);
      
      // Log metrics
      this.logMetrics({
        provider,
        responseTime: Date.now() - startTime,
        success: true,
        distance: result.distance
      });
      
      return result;
    } catch (error) {
      this.logMetrics({
        provider,
        responseTime: Date.now() - startTime,
        success: false,
        error: error.message
      });
      throw error;
    }
  }
  
  logMetrics(data) {
    // Send to analytics service (e.g., Mixpanel, Segment, custom)
    console.log('Routing metrics:', data);
  }
}
```

---

## 🧪 Testing Strategy

### Unit Tests

```javascript
// tests/services/pathPulseService.test.js
const PathPulseService = require('../../services/pathPulseService');

describe('PathPulseService', () => {
  let service;
  
  beforeEach(() => {
    service = new PathPulseService();
  });
  
  test('should calculate route between two points', async () => {
    const origin = { lat: -29.8587, lng: 31.0218 }; // Durban
    const destination = { lat: -26.2041, lng: 28.0473 }; // Johannesburg
    
    const route = await service.calculateRoute(origin, destination);
    
    expect(route).toHaveProperty('distance');
    expect(route).toHaveProperty('duration');
    expect(route.distance).toBeGreaterThan(0);
  });
  
  test('should handle API errors gracefully', async () => {
    service.apiKey = 'invalid_key';
    
    await expect(
      service.calculateRoute({ lat: 0, lng: 0 }, { lat: 1, lng: 1 })
    ).rejects.toThrow();
  });
});
```

### Integration Tests

```javascript
// tests/integration/routingService.test.js
const routingService = require('../../services/routingService');

describe('RoutingService Integration', () => {
  test('should fallback to Google Maps when PathPulse fails', async () => {
    // Temporarily break PathPulse
    process.env.PATHPULSE_API_KEY = 'invalid';
    
    const origin = { lat: -29.8587, lng: 31.0218 };
    const destination = { lat: -26.2041, lng: 28.0473 };
    
    const route = await routingService.calculateRoute(origin, destination);
    
    expect(route.provider).toBe('google'); // Should fall back
    expect(route).toHaveProperty('distance');
  });
});
```

---

## 📚 API Documentation

### PathPulse API Endpoints (Example)

#### Calculate Route
```
POST https://api.pathpulse.ai/v1/route

Request Body:
{
  "origin": { "lat": -29.8587, "lng": 31.0218 },
  "destination": { "lat": -26.2041, "lng": 28.0473 },
  "profile": "driving",
  "alternatives": true,
  "steps": true
}

Response:
{
  "distance": 568.5,
  "duration": 20340,
  "geometry": "...",
  "alternatives": [...]
}
```

#### Calculate ETA
```
POST https://api.pathpulse.ai/v1/eta

Request Body:
{
  "origin": { "lat": -29.8587, "lng": 31.0218 },
  "destination": { "lat": -26.2041, "lng": 28.0473 },
  "departure_time": "now"
}

Response:
{
  "distance": 568.5,
  "duration": 20340,
  "duration_in_traffic": 24600,
  "traffic_condition": "moderate"
}
```

#### Optimize Route
```
POST https://api.pathpulse.ai/v1/optimize

Request Body:
{
  "waypoints": [
    { "lat": -29.8587, "lng": 31.0218 },
    { "lat": -29.8500, "lng": 31.0300 },
    { "lat": -29.8400, "lng": 31.0400 }
  ],
  "optimization": "time",
  "return_to_start": false
}

Response:
{
  "optimized_order": [0, 2, 1],
  "total_distance": 25.3,
  "total_duration": 1800,
  "routes": [...],
  "savings": {
    "distance_saved": 5.2,
    "time_saved": 420
  }
}
```

---

## ✅ Benefits for TimoRides

### Immediate Benefits

1. **Cost Reduction**: PathPulse.ai may offer better pricing than Google Maps
2. **Enhanced Routing**: Access to advanced routing algorithms
3. **Traffic-Aware ETAs**: More accurate arrival time predictions
4. **Route Optimization**: Efficient multi-stop routes for drivers

### Long-Term Benefits

1. **Vendor Independence**: Not locked into single routing provider
2. **Auto-Failover**: OASIS HyperDrive ensures uptime
3. **Scalability**: Easy to add more providers as business grows
4. **Innovation**: Access to PathPulse.ai's latest features and improvements

### Competitive Advantages

1. **Premium Experience**: Best-in-class routing for premium rides
2. **Operational Efficiency**: Optimized routes reduce fuel costs and time
3. **Market Expansion**: PathPulse may have better regional coverage in South Africa
4. **Future-Proof**: OASIS architecture supports easy provider upgrades

---

## 🤝 Next Steps & Questions for PathPulse.ai

### Technical Discovery

1. **API Documentation**: Can you provide full API documentation?
2. **Coverage**: What geographic regions do you support? (South Africa? Durban specifically?)
3. **Traffic Data**: Do you provide real-time traffic data?
4. **Optimization**: What optimization algorithms do you support?
5. **Pricing**: What is your pricing model? (per request? per month? tiered?)
6. **Rate Limits**: What are your rate limits and fair use policies?
7. **SLA**: What uptime guarantees and SLAs do you provide?

### Integration Support

1. **SDK**: Do you provide Node.js/JavaScript SDK?
2. **Testing**: Do you offer sandbox/test API keys?
3. **Documentation**: Do you have integration guides or examples?
4. **Support**: What level of technical support is available?

### Business Terms

1. **Pricing**: What are the pricing tiers?
2. **Volume Discounts**: Do you offer discounts for high-volume usage?
3. **Contract Terms**: What are typical contract lengths?
4. **Trial Period**: Can TimoRides trial the API before committing?

---

## 📞 Contact & Support

### TimoRides Integration Team
- **Technical Lead**: [Name]
- **Email**: [Email]
- **GitHub**: https://github.com/NextGenSoftwareUK/OASIS

### OASIS Provider Development
- **Documentation**: https://github.com/NextGenSoftwareUK/OASIS
- **Provider Implementation Guide**: See `/Providers/PROVIDER_IMPLEMENTATION_STATUS.md`

---

## 📎 Appendix

### A. Existing Map Providers in OASIS

- **MapboxOASIS**: Full implementation with directions, geocoding, isochrone, matrix services
- **GOMapOASIS**: Unity-specific map provider for AR/VR applications
- **GoogleMapsOASIS**: (To be implemented)

### B. Related TimoRides Features

- **Ride Matching**: Could benefit from route optimization
- **Pricing**: Distance calculation feeds into fare estimation
- **Driver Earnings**: Accurate distance tracking for fair driver compensation
- **Offline Mode**: May need cached routes when offline

### C. OASIS HyperDrive

OASIS HyperDrive is the intelligent auto-failover system that:
- Monitors provider health
- Automatically switches to backup providers on failure
- Load balances across multiple providers
- Optimizes cost by selecting cheapest available provider

---

**Document Version:** 1.0  
**Last Updated:** October 20, 2025  
**Prepared For:** PathPulse.ai × TimoRides Integration Discussions


