# HyperDrive - OASIS Intelligent Routing System

**Last Updated:** December 2025  
**Source of Truth:** `OASIS Architecture/NextGenSoftware.OASIS.API.Core/Managers/OASIS HyperDrive/OASISHyperDrive.cs`

---

## What is HyperDrive?

**OASIS HyperDrive** is the intelligent routing engine that provides **100% uptime** through automatic failover, replication, and load balancing across all providers. It sits between managers and providers, intelligently routing requests to the optimal provider based on current conditions.

**Core Promise:** Your application keeps working even if individual providers fail.

---

## Architecture

```
Client Request
      ↓
Manager (AvatarManager, HolonManager, etc.)
      ↓
OASIS HyperDrive
      ↓
Provider Selection (Intelligent)
      ↓
Provider (Ethereum, Solana, MongoDB, etc.)
      ↓
Response
```

HyperDrive intercepts requests and routes them to the best available provider automatically.

---

## Core Components

### 1. OASISHyperDrive

Main routing engine that orchestrates all HyperDrive features.

**Key Classes:**
- `OASISHyperDrive` - Main routing engine
- `ProviderManager` - Provider registration and selection
- `PerformanceMonitor` - Tracks provider performance
- `OASISHyperDriveConfigManager` - Configuration management
- `AIOptimizationEngine` - AI-powered provider selection
- `AdvancedAnalyticsEngine` - Analytics and reporting
- `PredictiveFailoverEngine` - Predictive failover logic

---

## Three Core Features

### 1. Auto-Failover

**Purpose:** Automatically switches to backup providers when primary fails.

**How It Works:**
1. Request routed to primary provider
2. If provider fails (timeout, error, etc.)
3. HyperDrive tries next provider in failover list
4. Continues until success or all providers exhausted
5. Returns result from first successful provider

**Configuration:**
```json
"AutoFailOverEnabled": true,
"AutoFailOverProviders": "MongoDBOASIS, ArbitrumOASIS, EthereumOASIS"
```

**Code Flow:**
```csharp
// From OASISHyperDrive.cs
public async Task<OASISResult<T>> FailoverRequestAsync<T>(IRequest request)
{
    var failoverProviders = _providerManager.GetProviderAutoFailOverList();
    
    foreach (var provider in failoverProviders)
    {
        var result = await RouteToProviderAsync<T>(request, provider);
        if (!result.IsError)
        {
            return result; // Success!
        }
    }
    
    // All providers failed
    return new OASISResult<T> { IsError = true, Message = "All failover providers failed" };
}
```

**Result:** 100% uptime - your app keeps working even if MongoDB goes down.

---

### 2. Auto-Replication

**Purpose:** Automatically replicates data across multiple providers for redundancy.

**How It Works:**
1. Data saved to primary provider
2. HyperDrive replicates to all providers in replication list
3. Data now stored on multiple providers simultaneously
4. Provides redundancy and geographic distribution

**Configuration:**
```json
"AutoReplicationEnabled": false,  // Currently disabled in default config
"AutoReplicationProviders": "MongoDBOASIS"
```

**Code Flow:**
```csharp
// From OASISHyperDrive.cs
public async Task<OASISResult<List<T>>> ReplicateRequestAsync<T>(IRequest request)
{
    var replicationProviders = _providerManager.GetProvidersThatAreAutoReplicating();
    var results = new List<T>();
    
    foreach (var provider in replicationProviders)
    {
        var result = await RouteToProviderAsync<T>(request, provider);
        if (!result.IsError && result.Result != null)
        {
            results.Add(result.Result);
        }
    }
    
    return new OASISResult<List<T>> { Result = results };
}
```

**Use Cases:**
- Critical data that must survive provider failures
- Geographic redundancy
- Compliance requirements (multiple storage locations)

---

### 3. Auto-Load Balancing

**Purpose:** Distributes requests across multiple providers for optimal performance.

**How It Works:**
1. HyperDrive maintains list of load-balanced providers
2. Selects optimal provider based on strategy
3. Routes request to selected provider
4. Monitors performance and adjusts selection

**Load Balancing Strategies:**

```csharp
public enum LoadBalancingStrategy
{
    Auto,              // Intelligent selection using AI
    RoundRobin,        // Round-robin distribution
    Performance,       // Best performance (latency)
    CostBased,         // Lowest cost
    Geographic,        // Nearest geographic location
    LeastConnections   // Least busy provider
}
```

**Configuration:**
```json
"AutoLoadBalanceEnabled": true,
"AutoLoadBalanceProviders": "MongoDBOASIS"
```

**Code Flow:**
```csharp
// From OASISHyperDrive.cs
public async Task<OASISResult<T>> LoadBalanceRequestAsync<T>(
    IRequest request,
    LoadBalancingStrategy strategy = LoadBalancingStrategy.Auto)
{
    var availableProviders = _providerManager.GetProviderAutoLoadBalanceList();
    var selectedProvider = await SelectOptimalProviderAsync(request, strategy);
    
    return await RouteToProviderAsync<T>(request, selectedProvider);
}
```

---

## Intelligent Provider Selection

HyperDrive uses intelligent selection when `LoadBalancingStrategy.Auto` is used:

### Selection Factors

1. **Performance Metrics**
   - Latency (response time)
   - Throughput (requests per second)
   - Error rate
   - Uptime percentage

2. **Cost Metrics**
   - Transaction costs (gas fees)
   - Storage costs
   - API costs

3. **Geographic Factors**
   - Distance to provider nodes
   - Network routing efficiency
   - Regional performance

4. **Subscription Constraints**
   - Provider allowed for subscription plan
   - Quota limits
   - Pay-as-you-go availability

5. **AI Optimization**
   - Machine learning recommendations
   - Predictive analysis
   - Historical performance patterns

### Selection Code

```csharp
// From OASISHyperDrive.cs
private async Task<EnumValue<ProviderType>> SelectIntelligentProviderAsync(
    List<EnumValue<ProviderType>> providers, 
    IRequest request, 
    SubscriptionConfig subscriptionConfig)
{
    // Get AI recommendations
    var recommendations = await _aiEngine.GetProviderRecommendationsAsync(
        request, 
        providers.Select(p => p.Value).ToList()
    );
    
    // Apply subscription constraints
    var filteredRecommendations = recommendations
        .Where(r => IsProviderAllowedForSubscriptionAsync(...).Result)
        .OrderByDescending(r => r.Score)
        .ToList();
    
    return new EnumValue<ProviderType>(
        filteredRecommendations.FirstOrDefault()?.ProviderType ?? providers.First().Value
    );
}
```

---

## Request Routing Flow

### Complete Request Flow

```
1. Request arrives at Manager
      ↓
2. Manager calls HyperDrive.RouteRequestAsync()
      ↓
3. HyperDrive checks quota (if subscription-based)
      ↓
4. HyperDrive selects optimal provider
      ↓
5. Route to provider
      ↓
6. If error → Handle failover
      ↓
7. Return result
```

### Failover Flow

```
1. Request routed to Provider A
      ↓
2. Provider A fails (timeout/error)
      ↓
3. HyperDrive tries Provider B (next in failover list)
      ↓
4. Provider B succeeds → Return result
   OR
   Provider B fails → Try Provider C
      ↓
5. Continue until success or all exhausted
```

---

## Configuration

### OASIS_DNA.json Configuration

```json
{
  "OASIS": {
    "StorageProviders": {
      "AutoReplicationEnabled": false,
      "AutoFailOverEnabled": true,
      "AutoLoadBalanceEnabled": true,
      "AutoReplicationProviders": "MongoDBOASIS",
      "AutoLoadBalanceProviders": "MongoDBOASIS",
      "AutoFailOverProviders": "MongoDBOASIS, ArbitrumOASIS, EthereumOASIS"
    },
    "OASISHyperDriveConfig": {
      "IsEnabled": true,
      "DefaultStrategy": "Auto",
      "AutoFailoverEnabled": true,
      "AutoReplicationEnabled": true,
      "AutoLoadBalancingEnabled": true,
      "MaxRetryAttempts": 3,
      "RequestTimeoutMs": 5000,
      "PerformanceWeight": 0.4,
      "CostWeight": 0.3,
      "GeographicWeight": 0.2,
      "AvailabilityWeight": 0.1
    }
  }
}
```

### Configuration Properties

**Basic Settings:**
- `IsEnabled` - Enable/disable HyperDrive
- `DefaultStrategy` - Default load balancing strategy
- `MaxRetryAttempts` - Maximum retry attempts before failover
- `RequestTimeoutMs` - Request timeout in milliseconds

**Feature Toggles:**
- `AutoFailoverEnabled` - Enable auto-failover
- `AutoReplicationEnabled` - Enable auto-replication
- `AutoLoadBalancingEnabled` - Enable load balancing

**Weights (for intelligent selection):**
- `PerformanceWeight` - Weight for performance metrics
- `CostWeight` - Weight for cost considerations
- `GeographicWeight` - Weight for geographic proximity
- `AvailabilityWeight` - Weight for provider availability
- `LatencyWeight` - Weight for latency
- `ThroughputWeight` - Weight for throughput
- `ReliabilityWeight` - Weight for reliability

---

## Subscription Integration

HyperDrive integrates with subscription system for:

1. **Provider Filtering**
   - Free plans: Only free providers (IPFS, SEEDS, etc.)
   - Paid plans: All providers

2. **Quota Management**
   - Check quota before operations
   - Enforce limits
   - Allow pay-as-you-go if enabled

3. **Cost Optimization**
   - Select providers based on subscription plan
   - Avoid high-cost providers for basic plans

### Free Providers

HyperDrive identifies free providers:
- IPFSOASIS
- SEEDSOASIS
- ScuttlebuttOASIS
- ThreeFoldOASIS
- HoloOASIS
- PLANOASIS
- SOLIDOASIS
- BlockStackOASIS

### High-Cost Providers

HyperDrive identifies high-cost providers:
- EthereumOASIS (high gas fees)
- TRONOASIS
- ChainLinkOASIS

---

## Performance Monitoring

HyperDrive tracks provider performance:

1. **Metrics Collected:**
   - Response time (latency)
   - Success/failure rate
   - Throughput
   - Error types and frequencies

2. **Used For:**
   - Provider selection
   - Failover decisions
   - Load balancing
   - Predictive failover

3. **Storage:**
   - PerformanceMonitor class maintains metrics
   - Analytics engine processes historical data
   - AI engine uses for recommendations

---

## Request Types

HyperDrive handles different request types:

### Supported Request Types

1. **SaveHolonRequest** - Save holon data
2. **LoadHolonRequest** - Load holon by ID
3. **SaveAvatarRequest** - Save avatar data
4. **LoadAvatarRequest** - Load avatar by ID

### Routing Logic

```csharp
// From OASISHyperDrive.cs
private async Task<OASISResult<T>> RouteToProviderAsync<T>(
    IRequest request, 
    EnumValue<ProviderType> providerType)
{
    // Switch to target provider
    await ProviderManager.Instance.SetAndActivateCurrentStorageProviderAsync(
        providerType.Value
    );
    
    // Route based on request type
    return request switch
    {
        SaveHolonRequest saveHolon => await RouteSaveHolonAsync<T>(saveHolon),
        LoadHolonRequest loadHolon => await RouteLoadHolonAsync<T>(loadHolon),
        SaveAvatarRequest saveAvatar => await RouteSaveAvatarAsync<T>(saveAvatar),
        LoadAvatarRequest loadAvatar => await RouteLoadAvatarAsync<T>(loadAvatar),
        _ => new OASISResult<T> { IsError = true, Message = "Unknown request type" }
    };
}
```

---

## Error Handling

HyperDrive provides robust error handling:

1. **Provider Failures**
   - Catches exceptions
   - Logs errors
   - Triggers failover

2. **Quota Exceeded**
   - Checks quota before operations
   - Returns error if quota exceeded (unless pay-as-you-go)

3. **All Providers Failed**
   - Returns error after all failover attempts
   - Includes error details

4. **Timeout Handling**
   - Configurable timeout per request
   - Treats timeout as failure, triggers failover

---

## Best Practices

1. **Configure Failover Providers**
   - Always configure multiple providers in failover list
   - Order by reliability (most reliable first)

2. **Enable Auto-Replication for Critical Data**
   - Enable for data that must survive provider failures
   - Balance replication cost vs. redundancy needs

3. **Monitor Performance**
   - Review provider performance metrics
   - Adjust provider lists based on performance

4. **Cost Considerations**
   - Configure appropriate providers for subscription plans
   - Use free providers for basic plans

5. **Testing**
   - Test failover scenarios
   - Verify replication works correctly
   - Test load balancing distribution

---

## Limitations

1. **Replication Cost**
   - Replicating to multiple providers increases costs
   - Consider cost vs. redundancy trade-offs

2. **Consistency**
   - Replication is eventually consistent
   - Not all providers may have latest data immediately

3. **Provider-Specific Features**
   - Some features only available on specific providers
   - HyperDrive routes to appropriate provider automatically

---

## Related Documentation

- [Providers Concept](../concepts/PROVIDERS.md) - How providers work
- [Managers Concept](../concepts/MANAGERS.md) - How managers use HyperDrive
- [Provider Status](../reference/PROVIDERS/STATUS.md) - Provider availability
- [Configuration Reference](../reference/CONFIGURATION.md) - Configuration details

---

## Source Code References

**Main Implementation:**
- `OASIS Architecture/NextGenSoftware.OASIS.API.Core/Managers/OASIS HyperDrive/OASISHyperDrive.cs`

**Supporting Classes:**
- `PerformanceMonitor.cs` - Performance tracking
- `OASISHyperDriveConfigManager.cs` - Configuration management
- `AIOptimizationEngine.cs` - AI-powered selection
- `AdvancedAnalyticsEngine.cs` - Analytics
- `PredictiveFailoverEngine.cs` - Predictive failover

**Configuration:**
- `OASIS Architecture/NextGenSoftware.OASIS.API.DNA/Configuration/OASISHyperDriveConfig.cs`

