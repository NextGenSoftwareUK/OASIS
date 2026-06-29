using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Configuration;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive;
using NextGenSoftware.Logging;

namespace NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive
{
    /// <summary>
    /// OASIS HyperDrive - The intelligent routing engine that provides 100% uptime
    /// through auto-failover, auto-load balancing, and auto-replication across all providers
    /// </summary>
    public class OASISHyperDrive
    {
        /// <summary>
        /// Real in-process usage counters for quota enforcement, keyed by "{operationType}:{yyyy-MM}" so usage
        /// rolls over monthly (matching SubscriptionConfig.BillingCycle = "Monthly"). Shared across all
        /// OASISHyperDrive instances (the class is constructed fresh per request by callers like AvatarManager/
        /// HolonManager) via this static field, so quota actually accumulates instead of always reading 0.
        /// Loaded from disk on first use and persisted after every increment so monthly caps survive restarts.
        /// </summary>
        private static readonly ConcurrentDictionary<string, int> _usageCounters = new ConcurrentDictionary<string, int>();
        private static bool _usageCountersLoaded = false;
        private static readonly object _usageLoadLock = new object();

        /// <summary>
        /// Directory used for persisting runtime state (quota counters).
        /// Defaults to "oasis-data"; can be overridden before the first OASISHyperDrive instance is created.
        /// </summary>
        public static string DataDirectory { get; set; } = "oasis-data";
        private const string QuotaFileName = "hyperdrive-quota.json";

        private readonly ProviderManager _providerManager;
        private readonly PerformanceMonitor _performanceMonitor;
        private readonly OASISHyperDriveConfigManager _configManager;
        private readonly AIOptimizationEngine _aiEngine;
        private readonly AdvancedAnalyticsEngine _analyticsEngine;
        private readonly PredictiveFailoverEngine _failoverEngine;

        public OASISHyperDrive()
        {
            _providerManager = ProviderManager.Instance;
            _performanceMonitor = PerformanceMonitor.Instance;
            _configManager = OASISHyperDriveConfigManager.Instance;
            _aiEngine = AIOptimizationEngine.Instance;
            _analyticsEngine = AdvancedAnalyticsEngine.Instance;
            _failoverEngine = PredictiveFailoverEngine.Instance;
        }

        /// <summary>
        /// Routes requests intelligently based on current conditions and strategy
        /// </summary>
        public async Task<OASISResult<T>> RouteRequestAsync<T>(
            IRequest request, 
            LoadBalancingStrategy strategy = LoadBalancingStrategy.Auto)
        {
            try
            {
                // 1. Check subscription quotas before proceeding
                var quotaCheck = await CheckQuotaBeforeOperationAsync(request);
                if (quotaCheck.IsError)
                {
                    return new OASISResult<T>
                    {
                        IsError = true,
                        Message = quotaCheck.Message
                    };
                }

                // 2. Select optimal provider using HyperDrive logic
                var providerType = await SelectOptimalProviderAsync(request, strategy);
                
                // 3. Route request to provider
                var result = await RouteToProviderAsync<T>(request, providerType);
                
                // 4. Handle failover if needed
                if (result.IsError)
                {
                    result = await HandleFailoverAsync<T>(request, providerType);
                }
                
                // 5. Optionally update performance metrics (not available in current PerformanceMonitor API)
                
                return result;
            }
            catch (Exception ex)
            {
                return new OASISResult<T>
                {
                    IsError = true,
                    Message = $"Request routing failed: {ex.Message}",
                    Exception = ex
                };
            }
        }

        /// <summary>
        /// Auto-load balancing - distributes load across multiple providers
        /// </summary>
        public async Task<OASISResult<T>> LoadBalanceRequestAsync<T>(
            IRequest request,
            LoadBalancingStrategy strategy = LoadBalancingStrategy.Auto)
        {
            try
            {
                var availableProviders = _providerManager.GetProviderAutoLoadBalanceList();
                var selectedProvider = await SelectOptimalProviderAsync(request, strategy);
                
                return await RouteToProviderAsync<T>(request, selectedProvider);
            }
            catch (Exception ex)
            {
                return new OASISResult<T>
                {
                    IsError = true,
                    Message = $"Load balancing failed: {ex.Message}",
                    Exception = ex
                };
            }
        }

        /// <summary>
        /// Auto-failover - switches to backup providers when primary fails
        /// </summary>
        public async Task<OASISResult<T>> FailoverRequestAsync<T>(IRequest request)
        {
            if (request == null)
                return new OASISResult<T> { IsError = true, Message = "The request is required. Please provide a valid IRequest." };
            try
            {
                var failoverProviders = _providerManager.GetProviderAutoFailOverList();
                
                foreach (var provider in failoverProviders)
                {
                    var result = await RouteToProviderAsync<T>(request, provider);
                    if (!result.IsError)
                    {
                        return result;
                    }
                }
                
                return new OASISResult<T>
                {
                    IsError = true,
                    Message = "All failover providers failed"
                };
            }
            catch (Exception ex)
            {
                return new OASISResult<T>
                {
                    IsError = true,
                    Message = $"Failover failed: {ex.Message}",
                    Exception = ex
                };
            }
        }

        /// <summary>
        /// Auto-replication - replicates data across multiple providers
        /// </summary>
        public async Task<OASISResult<List<T>>> ReplicateRequestAsync<T>(IRequest request)
        {
            if (request == null)
                return new OASISResult<List<T>> { IsError = true, Message = "The request is required. Please provide a valid IRequest." };
            try
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
                
                return new OASISResult<List<T>>
                {
                    Result = results,
                    Message = $"Replicated to {results.Count} providers"
                };
            }
            catch (Exception ex)
            {
                return new OASISResult<List<T>>
                {
                    IsError = true,
                    Message = $"Replication failed: {ex.Message}",
                    Exception = ex
                };
            }
        }

        // HyperDrive-specific methods (following SOLID principles)

        /// <summary>
        /// Selects optimal provider based on HyperDrive configuration and strategy
        /// </summary>
        private async Task<EnumValue<ProviderType>> SelectOptimalProviderAsync(IRequest request, LoadBalancingStrategy strategy)
        {
            try
            {
                // Get current configuration
                var config = _configManager.GetConfiguration();
                var subscriptionConfig = GetSubscriptionConfig();
                
                // Get available providers from ProviderManager
                var availableProviders = _providerManager.GetProviderAutoLoadBalanceList();
                
                // Apply subscription-based filtering
                var filteredProviders = await FilterProvidersBySubscriptionAsync(availableProviders, subscriptionConfig);
                
                // Use ProviderManager's selection logic for standard strategies
                if (strategy != LoadBalancingStrategy.Auto && strategy != LoadBalancingStrategy.CostBased)
                {
                    return _providerManager.SelectOptimalProviderForLoadBalancing(strategy);
                }
                
                        // Handle HyperDrive-specific strategies
                        return strategy switch
                        {
                            LoadBalancingStrategy.CostBased => SelectCostBasedProvider(filteredProviders, subscriptionConfig),
                            LoadBalancingStrategy.Auto => await SelectIntelligentProviderAsync(filteredProviders, request, subscriptionConfig),
                            _ => _providerManager.SelectOptimalProviderForLoadBalancing(strategy)
                        };
            }
            catch (Exception ex)
            {
                LoggingManager.Log($"OASISHyperDrive.SelectOptimalProviderAsync failed, falling back to the current storage provider. Reason: {ex.Message}", LogType.Warning);
                return _providerManager.CurrentStorageProviderType;
            }
        }

        /// <summary>
        /// Filters providers based on subscription plan and cost constraints
        /// </summary>
        private async Task<List<EnumValue<ProviderType>>> FilterProvidersBySubscriptionAsync(
            List<EnumValue<ProviderType>> providers, 
            SubscriptionConfig subscriptionConfig)
        {
            var filteredProviders = new List<EnumValue<ProviderType>>();
            
            foreach (var provider in providers)
            {
                // Check if provider is allowed for this subscription plan
                if (await IsProviderAllowedForSubscriptionAsync(provider, subscriptionConfig))
                {
                    filteredProviders.Add(provider);
                }
            }
            
            return filteredProviders.Any() ? filteredProviders : providers;
        }

        /// <summary>
        /// Checks if provider is allowed for current subscription
        /// </summary>
        private async Task<bool> IsProviderAllowedForSubscriptionAsync(EnumValue<ProviderType> provider, SubscriptionConfig config)
        {
            // Free plan - only allow free providers
            if (config.PlanType == "Free")
            {
                return IsFreeProvider(provider);
            }
            
            // Paid plans - check cost constraints
            if (config.PlanType == "Basic" && IsHighCostProvider(provider))
            {
                return false;
            }
            
            return true;
        }

        /// <summary>
        /// Intelligent provider selection using AI optimization
        /// </summary>
        private async Task<EnumValue<ProviderType>> SelectIntelligentProviderAsync(
            List<EnumValue<ProviderType>> providers, 
            IRequest request, 
            SubscriptionConfig subscriptionConfig)
        {
            try
            {
                // Get AI recommendations
                var recommendations = await _aiEngine.GetProviderRecommendationsAsync(request, providers.Select(p => p.Value).ToList());

                // Apply subscription constraints (awaited properly - no sync-over-async .Result, which risked
                // deadlocking callers running on a captured synchronization context).
                var filteredRecommendations = new List<ProviderRecommendation>();
                foreach (var recommendation in recommendations)
                {
                    if (await IsProviderAllowedForSubscriptionAsync(new EnumValue<ProviderType>(recommendation.ProviderType), subscriptionConfig))
                        filteredRecommendations.Add(recommendation);
                }
                filteredRecommendations = filteredRecommendations.OrderByDescending(r => r.Score).ToList();

                return new EnumValue<ProviderType>(filteredRecommendations.FirstOrDefault()?.ProviderType ?? providers.First().Value);
            }
            catch (Exception ex)
            {
                LoggingManager.Log($"OASISHyperDrive.SelectIntelligentProviderAsync AI recommendation failed, falling back to performance-based selection. Reason: {ex.Message}", LogType.Warning);
                return _providerManager.SelectOptimalProviderForLoadBalancing(LoadBalancingStrategy.Performance);
            }
        }


        /// <summary>
        /// Cost-based provider selection
        /// </summary>
        private EnumValue<ProviderType> SelectCostBasedProvider(List<EnumValue<ProviderType>> providers, SubscriptionConfig config)
        {
            if (!providers.Any()) return _providerManager.CurrentStorageProviderType;

            // Prioritize free providers for cost-conscious plans
            if (config.PlanType == "Free" || config.PlanType == "Basic")
            {
                var freeProviders = providers.Where(IsFreeProvider).ToList();
                if (freeProviders.Any()) return freeProviders.First();
            }

            // For paid plans, select based on cost efficiency
            return providers.FirstOrDefault(p => !IsHighCostProvider(p)) ?? providers.First();
        }


        /// <summary>
        /// Checks quota before operation
        /// </summary>
        private async Task<OASISResult<bool>> CheckQuotaBeforeOperationAsync(IRequest request)
        {
            try
            {
                var subscriptionConfig = GetSubscriptionConfig();
                var operationType = GetOperationType(request);
                
                // Check if operation would exceed quota
                var currentUsage = await GetCurrentUsageAsync(operationType);
                var limit = GetQuotaLimit(operationType, subscriptionConfig);
                
                if (currentUsage >= limit)
                {
                    if (subscriptionConfig.PayAsYouGoEnabled)
                    {
                        // Allow with pay-as-you-go - still record the usage below so cost/overage is tracked.
                        IncrementUsage(operationType);
                        return new OASISResult<bool> { Result = true };
                    }
                    else
                    {
                        return new OASISResult<bool>
                        {
                            IsError = true,
                            Message = $"Quota exceeded for {operationType}. Current usage: {currentUsage}/{limit}"
                        };
                    }
                }

                // Operation is within quota - record it now so the next check sees accurate usage.
                IncrementUsage(operationType);
                return new OASISResult<bool> { Result = true };
            }
            catch (Exception ex)
            {
                return new OASISResult<bool>
                {
                    IsError = true,
                    Message = $"Failed to check quota: {ex.Message}",
                    Exception = ex
                };
            }
        }

        // Helper methods

        private SubscriptionConfig GetSubscriptionConfig()
        {
            var dna = OASISDNAManager.OASISDNA;
            return dna?.OASIS?.SubscriptionConfig ?? new SubscriptionConfig();
        }

        private bool IsFreeProvider(EnumValue<ProviderType> provider)
        {
            var freeProviders = new List<ProviderType> {
                ProviderType.IPFSOASIS, ProviderType.SEEDSOASIS,
                ProviderType.ScuttlebuttOASIS, ProviderType.ThreeFoldOASIS, ProviderType.HoloOASIS,
                ProviderType.PLANOASIS, ProviderType.SOLIDOASIS, ProviderType.BlockStackOASIS
            };
            return freeProviders.Contains(provider.Value);
        }

        private bool IsHighCostProvider(EnumValue<ProviderType> provider)
        {
            var highCostProviders = new List<ProviderType> {
                ProviderType.EthereumOASIS, ProviderType.TRONOASIS, ProviderType.ChainLinkOASIS
            };
            return highCostProviders.Contains(provider.Value);
        }


        private string GetOperationType(IRequest request)
        {
            return request switch
            {
                SaveHolonRequest _ => "Replications",
                LoadHolonRequest _ => "Requests",
                SaveAvatarRequest _ => "Replications",
                LoadAvatarRequest _ => "Requests",
                _ => "Requests"
            };
        }

        /// <summary>Builds the rolling-monthly usage counter key for an operation type.</summary>
        private static string GetUsageCounterKey(string operationType)
        {
            return $"{operationType}:{DateTime.UtcNow:yyyy-MM}";
        }

        private static void EnsureUsageCountersLoaded()
        {
            if (_usageCountersLoaded) return;
            lock (_usageLoadLock)
            {
                if (_usageCountersLoaded) return;
                // Synchronous load on first access (only runs once per process).
                var loaded = OASISPersistence.LoadAsync<Dictionary<string, int>>(DataDirectory, QuotaFileName)
                    .GetAwaiter().GetResult();
                if (loaded != null)
                    foreach (var kv in loaded)
                        _usageCounters[kv.Key] = kv.Value;
                _usageCountersLoaded = true;
            }
        }

        /// <summary>Records one unit of usage against the current monthly window for the given operation type.</summary>
        private static void IncrementUsage(string operationType)
        {
            EnsureUsageCountersLoaded();
            _usageCounters.AddOrUpdate(GetUsageCounterKey(operationType), 1, (_, count) => count + 1);
            // Fire-and-forget best-effort persist so monthly caps survive restarts.
            _ = OASISPersistence.SaveAsync(DataDirectory, QuotaFileName, new Dictionary<string, int>(_usageCounters));
        }

        private Task<int> GetCurrentUsageAsync(string operationType)
        {
            EnsureUsageCountersLoaded();
            return Task.FromResult(_usageCounters.TryGetValue(GetUsageCounterKey(operationType), out var count) ? count : 0);
        }

        private int GetQuotaLimit(string operationType, SubscriptionConfig config)
        {
            return operationType switch
            {
                "Replications" => config.MaxReplicationsPerMonth,
                "Failovers" => config.MaxFailoversPerMonth,
                // Requests now reads a real, DNA-configurable SubscriptionConfig.MaxRequestsPerMonth field
                // instead of a hardcoded 1000 - operators can now actually tune this per subscription plan.
                _ => config.MaxRequestsPerMonth
            };
        }

                // Route requests to specific providers
                private async Task<OASISResult<T>> RouteToProviderAsync<T>(IRequest request, EnumValue<ProviderType> providerType)
                {
                    // Apply any preventive failover override recorded by the background prediction loop.
                    // This is the safe alternative to PredictiveFailoverEngine calling
                    // SetAndActivateCurrentStorageProviderAsync globally (which would race with concurrent requests).
                    var override_ = PredictiveFailoverEngine.Instance.GetFailoverOverride(providerType.Value);
                    if (override_ != ProviderType.Default)
                    {
                        LoggingManager.Log(
                            $"HyperDrive: applying predictive failover override {providerType.Value} -> {override_}",
                            Logging.LogType.Info);
                        providerType = new EnumValue<ProviderType>(override_);
                    }

                    var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                    OASISResult<T> routeResult;

                    try
                    {
                        // Switch to the target provider
                        await ProviderManager.Instance.SetAndActivateCurrentStorageProviderAsync(providerType.Value);

                        // Route based on request type
                        routeResult = request switch
                        {
                            SaveHolonRequest saveHolon => await RouteSaveHolonAsync<T>(saveHolon),
                            LoadHolonRequest loadHolon => await RouteLoadHolonAsync<T>(loadHolon),
                            SaveAvatarRequest saveAvatar => await RouteSaveAvatarAsync<T>(saveAvatar),
                            LoadAvatarRequest loadAvatar => await RouteLoadAvatarAsync<T>(loadAvatar),
                            _ => new OASISResult<T>
                            {
                                IsError = true,
                                Message = $"Unknown request type: {request.GetType().Name}"
                            }
                        };
                    }
                    catch (Exception ex)
                    {
                        routeResult = new OASISResult<T>
                        {
                            IsError = true,
                            Message = $"Provider routing failed: {ex.Message}",
                            Exception = ex
                        };
                    }

                    stopwatch.Stop();

                    // If this provider succeeded after a failover override was active, clear it so traffic
                    // gradually returns to the primary once it has recovered.
                    if (!routeResult.IsError)
                        PredictiveFailoverEngine.Instance.ClearFailoverOverride(providerType.Value);

                    // Feed the AI optimization engine real outcomes from every routed request, instead of it
                    // always degrading to a neutral 0.5 score because nothing in the live path ever called this.
                    await _aiEngine.RecordPerformanceDataAsync(
                        providerType.Value,
                        request,
                        new OASISResult<object> { IsError = routeResult.IsError, Message = routeResult.Message },
                        stopwatch.ElapsedMilliseconds);

                    // Feed AdvancedAnalyticsEngine real outcomes too - RecordAnalyticsData was never called
                    // from anywhere in the live path, so its cost/performance optimization recommendations
                    // and predictive analytics always operated on zero recorded data points regardless of
                    // real traffic.
                    _analyticsEngine.RecordAnalyticsData(providerType.Value, new AnalyticsDataPoint
                    {
                        Timestamp = DateTime.UtcNow,
                        ProviderType = providerType.Value,
                        Success = !routeResult.IsError,
                        ResponseTimeMs = stopwatch.ElapsedMilliseconds,
                        Cost = (double)PerformanceMonitor.Instance.GetProviderCosts(providerType.Value),
                        Operation = request.GetType().Name
                    });

                    return routeResult;
                }

                private async Task<OASISResult<T>> HandleFailoverAsync<T>(IRequest request, EnumValue<ProviderType> originalProvider)
                {
                    try
                    {
                        // Get failover providers from ProviderManager
                        var failoverProviders = ProviderManager.Instance.GetProviderAutoFailOverList();
                        
                        foreach (var provider in failoverProviders)
                        {
                            if (provider.Value == originalProvider.Value) continue; // Skip the failed provider
                            
                            var result = await RouteToProviderAsync<T>(request, provider);
                            if (!result.IsError)
                            {
                                return result;
                            }
                        }
                        
                        return new OASISResult<T>
                        {
                            IsError = true,
                            Message = "All failover providers failed"
                        };
                    }
                    catch (Exception ex)
                    {
                        return new OASISResult<T>
                        {
                            IsError = true,
                            Message = $"Failover handling failed: {ex.Message}",
                            Exception = ex
                        };
                    }
                }

        // Route methods for different request types
        private async Task<OASISResult<T>> RouteSaveHolonAsync<T>(SaveHolonRequest request)
        {
            // Route to HolonManager for saving
            var holonManager = HolonManager.Instance;
            var result = await holonManager.SaveHolonAsync(request.Holon as IHolon);
            return new OASISResult<T>
            {
                Result = result.Result != null ? (T)result.Result : default(T),
                IsError = result.IsError,
                Message = result.Message,
                Exception = result.Exception
            };
        }

        private async Task<OASISResult<T>> RouteLoadHolonAsync<T>(LoadHolonRequest request)
        {
            // Route to HolonManager for loading
            var holonManager = HolonManager.Instance;
            var result = await holonManager.LoadHolonAsync(request.HolonId);
            return new OASISResult<T>
            {
                Result = result.Result != null ? (T)result.Result : default(T),
                IsError = result.IsError,
                Message = result.Message,
                Exception = result.Exception
            };
        }

        private async Task<OASISResult<T>> RouteSaveAvatarAsync<T>(SaveAvatarRequest request)
        {
            // Route to AvatarManager for saving
            var avatarManager = AvatarManager.Instance;
            var result = await avatarManager.SaveAvatarAsync(request.Avatar as IAvatar);
            return new OASISResult<T>
            {
                Result = result.Result != null ? (T)result.Result : default(T),
                IsError = result.IsError,
                Message = result.Message,
                Exception = result.Exception
            };
        }

        private async Task<OASISResult<T>> RouteLoadAvatarAsync<T>(LoadAvatarRequest request)
        {
            // Route to AvatarManager for loading
            var avatarManager = AvatarManager.Instance;
            var result = await avatarManager.LoadAvatarAsync(request.AvatarId);
            return new OASISResult<T>
            {
                Result = result.Result != null ? (T)result.Result : default(T),
                IsError = result.IsError,
                Message = result.Message,
                Exception = result.Exception
            };
        }

        /// <summary>
        /// Get network topology information
        /// </summary>
        public async Task<OASISResult<NetworkTopology>> GetNetworkTopologyAsync()
        {
            var result = new OASISResult<NetworkTopology>();
            
            try
            {
                // Get current provider status and network topology
                var allProviders = _providerManager.GetAllRegisteredProviders();
                var activeProviderCount = allProviders.Count(p => p.IsProviderActivated);

                var topology = new NetworkTopology
                {
                    TotalProviders = allProviders.Count,
                    ActiveProviders = activeProviderCount,
                    // Real health = fraction of registered providers currently activated, not a hardcoded constant.
                    NetworkHealth = allProviders.Count == 0 ? 0.0 : Math.Round((double)activeProviderCount / allProviders.Count, 4),
                    LastUpdated = DateTime.UtcNow
                };
                
                result.Result = topology;
                result.IsError = false;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting network topology: {ex.Message}", ex);
            }
            
            return result;
        }
    }

    /// <summary>
    /// Network topology information
    /// </summary>
    public class NetworkTopology
    {
        public int TotalProviders { get; set; }
        public int ActiveProviders { get; set; }
        public double NetworkHealth { get; set; }
        public DateTime LastUpdated { get; set; }
    }

}