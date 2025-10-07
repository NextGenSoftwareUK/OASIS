using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Core.Managers;

namespace NextGenSoftware.OASIS.API.Core
{
    /// <summary>
    /// OASIS HyperDrive - The intelligent routing engine that provides 100% uptime
    /// through auto-failover, auto-load balancing, and auto-replication across all providers
    /// </summary>
    public class OASISHyperDrive
    {
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
                if (!quotaCheck.IsSuccess)
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
                
                // 5. Update performance metrics
                await _performanceMonitor.UpdateMetricsAsync(providerType, result);
                
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
                var availableProviders = _providerManager.GetAvailableProviders();
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
            try
            {
                var failoverProviders = _providerManager.GetFailoverProviders();
                
                foreach (var provider in failoverProviders)
                {
                    var result = await RouteToProviderAsync<T>(request, provider);
                    if (result.IsSuccess)
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
            try
            {
                var replicationProviders = _providerManager.GetReplicationProviders();
                var results = new List<T>();
                
                foreach (var provider in replicationProviders)
                {
                    var result = await RouteToProviderAsync<T>(request, provider);
                    if (result.IsSuccess && result.Result != null)
                    {
                        results.Add(result.Result);
                    }
                }
                
                return new OASISResult<List<T>>
                {
                    Result = results,
                    IsSuccess = true,
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
                var availableProviders = _providerManager.GetAvailableProviders();
                
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
                // Fallback to current provider
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
                var recommendations = await _aiEngine.GetProviderRecommendationsAsync(request, providers);
                
                // Apply subscription constraints
                var filteredRecommendations = recommendations
                    .Where(r => IsProviderAllowedForSubscriptionAsync(r.ProviderType, subscriptionConfig).Result)
                    .OrderByDescending(r => r.Score)
                    .ToList();
                
                return filteredRecommendations.FirstOrDefault()?.ProviderType ?? providers.First();
            }
            catch (Exception)
            {
                // Fallback to ProviderManager's performance-based selection
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
                        // Allow with pay-as-you-go
                        return new OASISResult<bool> { Result = true, IsSuccess = true };
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
                
                return new OASISResult<bool> { Result = true, IsSuccess = true };
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
            var dna = OASISDNAManager.Instance.OASISDNA;
            return dna?.SubscriptionConfig ?? new SubscriptionConfig();
        }

        private bool IsFreeProvider(EnumValue<ProviderType> provider)
        {
            var freeProviders = new[] { 
                ProviderType.MongoOASIS, ProviderType.IPFSOASIS, ProviderType.SEEDSOASIS, 
                ProviderType.ScuttlebuttOASIS, ProviderType.ThreeFoldOASIS, ProviderType.HoloOASIS,
                ProviderType.PLANOASIS, ProviderType.SOLIDOASIS, ProviderType.BlockStackOASIS
            };
            return freeProviders.Contains(provider.Value);
        }

        private bool IsHighCostProvider(EnumValue<ProviderType> provider)
        {
            var highCostProviders = new[] { 
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

        private async Task<int> GetCurrentUsageAsync(string operationType)
        {
            // This would typically come from a usage tracking service
            return operationType switch
            {
                "Replications" => 45,
                "Failovers" => 3,
                "Storage" => 2,
                "Requests" => 1250,
                _ => 0
            };
        }

        private int GetQuotaLimit(string operationType, SubscriptionConfig config)
        {
            return operationType switch
            {
                "Replications" => config.MaxReplicationsPerMonth,
                "Failovers" => config.MaxFailoversPerMonth,
                "Storage" => config.MaxStorageGB,
                "Requests" => GetRequestLimit(config.PlanType),
                _ => 0
            };
        }

        private int GetRequestLimit(string planType)
        {
            return planType switch
            {
                "Free" => 1000,
                "Basic" => 10000,
                "Pro" => 100000,
                "Enterprise" => int.MaxValue,
                _ => 1000
            };
        }

        // Placeholder methods that would be implemented with actual provider routing
        private async Task<OASISResult<T>> RouteToProviderAsync<T>(IRequest request, EnumValue<ProviderType> providerType)
        {
            // This would route the request to the specific provider
            // For now, return a placeholder
            return new OASISResult<T>
            {
                IsError = true,
                Message = "Provider routing not implemented"
            };
        }

        private async Task<OASISResult<T>> HandleFailoverAsync<T>(IRequest request, EnumValue<ProviderType> originalProvider)
        {
            // This would handle failover to backup providers
            // For now, return a placeholder
            return new OASISResult<T>
            {
                IsError = true,
                Message = "Failover handling not implemented"
            };
        }
    }

    // Supporting classes and enums
    public enum LoadBalancingStrategy
    {
        Auto,
        RoundRobin,
        WeightedRoundRobin,
        LeastConnections,
        Geographic,
        CostBased,
        Performance
    }

    public class ProviderPerformanceMetrics
    {
        public double ResponseTimeMs { get; set; }
        public double ThroughputMbps { get; set; }
        public double UptimePercentage { get; set; }
        public double ErrorRate { get; set; }
        public int ActiveConnections { get; set; }
    }

    public class GeographicLocation
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string Country { get; set; }
        public string Region { get; set; }
        public string City { get; set; }
    }
}