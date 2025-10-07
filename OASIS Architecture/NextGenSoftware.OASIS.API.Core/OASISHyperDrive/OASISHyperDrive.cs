using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.DNA;

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
        private readonly LoadBalancer _loadBalancer;
        private readonly ReplicationEngine _replicationEngine;
        private readonly FailoverManager _failoverManager;
        private readonly GeographicRouter _geographicRouter;
        private readonly CostOptimizer _costOptimizer;
        private readonly OASISHyperDriveConfigManager _configManager;
        private readonly SubscriptionManager _subscriptionManager;
        private readonly DataPermissionsManager _dataPermissionsManager;
        private readonly IntelligentModeManager _intelligentModeManager;

        public OASISHyperDrive()
        {
            _providerManager = new ProviderManager();
            _performanceMonitor = new PerformanceMonitor();
            _loadBalancer = new LoadBalancer(_performanceMonitor);
            _replicationEngine = new ReplicationEngine();
            _failoverManager = new FailoverManager(_performanceMonitor);
            _geographicRouter = new GeographicRouter();
            _costOptimizer = new CostOptimizer();
            _configManager = new OASISHyperDriveConfigManager();
            _subscriptionManager = new SubscriptionManager();
            _dataPermissionsManager = new DataPermissionsManager();
            _intelligentModeManager = new IntelligentModeManager();
        }

        /// <summary>
        /// Routes requests intelligently based on current conditions and strategy
        /// </summary>
        public async Task<OASISResult<T>> RouteRequestAsync<T>(
            IRequest request, 
            RoutingStrategy strategy = RoutingStrategy.Auto)
        {
            try
            {
                // 1. Analyze current network conditions
                var networkConditions = await _performanceMonitor.AnalyzeNetworkConditionsAsync();
                
                // 2. Select optimal provider
                var provider = await SelectOptimalProviderAsync(request, networkConditions, strategy);
                
                // 3. Route request to provider
                var result = await RouteToProviderAsync<T>(request, provider);
                
                // 4. Handle failover if needed
                if (result.IsError)
                {
                    result = await HandleFailoverAsync<T>(request, provider);
                }
                
                // 5. Update performance metrics
                await _performanceMonitor.UpdateMetricsAsync(provider, result);
                
                return result;
            }
            catch (Exception ex)
            {
                return new OASISResult<T>
                {
                    IsError = true,
                    Message = $"HyperDrive routing failed: {ex.Message}",
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
                var availableProviders = await _providerManager.GetAvailableProvidersAsync();
                var selectedProvider = await _loadBalancer.SelectProviderAsync(request, availableProviders, strategy);
                
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
        /// Auto-failover - switches to backup providers when issues are detected
        /// </summary>
        public async Task<OASISResult<T>> FailoverRequestAsync<T>(
            IRequest request,
            IOASISStorageProvider failedProvider)
        {
            try
            {
                return await _failoverManager.HandleFailoverAsync<T>(request, failedProvider);
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
        public async Task<OASISResult<bool>> ReplicateDataAsync<T>(
            T data,
            ReplicationStrategy strategy = ReplicationStrategy.Smart)
        {
            try
            {
                return await _replicationEngine.ReplicateDataAsync(data, strategy);
            }
            catch (Exception ex)
            {
                return new OASISResult<bool>
                {
                    IsError = true,
                    Message = $"Replication failed: {ex.Message}",
                    Exception = ex
                };
            }
        }

        /// <summary>
        /// Geographic routing - routes to nearest provider based on location
        /// </summary>
        public async Task<OASISResult<T>> RouteGeographicallyAsync<T>(
            IRequest request,
            GeographicLocation userLocation)
        {
            try
            {
                var availableProviders = await _providerManager.GetAvailableProvidersAsync();
                var nearestProvider = await _geographicRouter.SelectNearestProviderAsync(availableProviders, userLocation);
                
                return await RouteToProviderAsync<T>(request, nearestProvider);
            }
            catch (Exception ex)
            {
                return new OASISResult<T>
                {
                    IsError = true,
                    Message = $"Geographic routing failed: {ex.Message}",
                    Exception = ex
                };
            }
        }

        /// <summary>
        /// Cost-optimized routing - routes to most cost-effective provider
        /// </summary>
        public async Task<OASISResult<T>> RouteCostOptimizedAsync<T>(
            IRequest request,
            CostOptimizationStrategy strategy = CostOptimizationStrategy.Balanced)
        {
            try
            {
                var availableProviders = await _providerManager.GetAvailableProvidersAsync();
                var optimizedProvider = await _costOptimizer.SelectOptimalProviderAsync(availableProviders, request, strategy);
                
                return await RouteToProviderAsync<T>(request, optimizedProvider);
            }
            catch (Exception ex)
            {
                return new OASISResult<T>
                {
                    IsError = true,
                    Message = $"Cost optimization failed: {ex.Message}",
                    Exception = ex
                };
            }
        }

        private async Task<IOASISStorageProvider> SelectOptimalProviderAsync(
            IRequest request, 
            NetworkConditions conditions, 
            RoutingStrategy strategy)
        {
            var availableProviders = await _providerManager.GetAvailableProvidersAsync();
            
            return strategy switch
            {
                RoutingStrategy.Performance => await SelectByPerformanceAsync(availableProviders, conditions),
                RoutingStrategy.Cost => await SelectByCostAsync(availableProviders, conditions),
                RoutingStrategy.Geographic => await SelectByGeographyAsync(availableProviders, conditions),
                RoutingStrategy.Auto => await SelectAutomaticallyAsync(availableProviders, conditions),
                _ => await SelectAutomaticallyAsync(availableProviders, conditions)
            };
        }

        private async Task<IOASISStorageProvider> SelectByPerformanceAsync(
            List<IOASISStorageProvider> providers, 
            NetworkConditions conditions)
        {
            var performanceMetrics = await _performanceMonitor.GetProviderPerformanceAsync(providers);
            return performanceMetrics
                .OrderByDescending(p => p.ResponseTime)
                .ThenByDescending(p => p.Throughput)
                .First().Provider;
        }

        private async Task<IOASISStorageProvider> SelectByCostAsync(
            List<IOASISStorageProvider> providers, 
            NetworkConditions conditions)
        {
            var costMetrics = await _costOptimizer.GetProviderCostsAsync(providers);
            return costMetrics
                .OrderBy(p => p.CostPerRequest)
                .First().Provider;
        }

        private async Task<IOASISStorageProvider> SelectByGeographyAsync(
            List<IOASISStorageProvider> providers, 
            NetworkConditions conditions)
        {
            return await _geographicRouter.SelectNearestProviderAsync(providers, conditions.UserLocation);
        }

        private async Task<IOASISStorageProvider> SelectAutomaticallyAsync(
            List<IOASISStorageProvider> providers, 
            NetworkConditions conditions)
        {
            // Intelligent selection based on multiple factors
            var performanceMetrics = await _performanceMonitor.GetProviderPerformanceAsync(providers);
            var costMetrics = await _costOptimizer.GetProviderCostsAsync(providers);
            
            // Weighted scoring system
            var scoredProviders = providers.Select(provider =>
            {
                var perf = performanceMetrics.FirstOrDefault(p => p.Provider == provider);
                var cost = costMetrics.FirstOrDefault(c => c.Provider == provider);
                
                var score = (perf?.ResponseTime ?? 1.0) * 0.4 +
                           (perf?.Throughput ?? 1.0) * 0.3 +
                           (cost?.CostPerRequest ?? 1.0) * 0.3;
                
                return new { Provider = provider, Score = score };
            }).OrderBy(p => p.Score);
            
            return scoredProviders.First().Provider;
        }

        private async Task<OASISResult<T>> RouteToProviderAsync<T>(IRequest request, IOASISStorageProvider provider)
        {
            try
            {
                // Route the request to the selected provider
                // This would integrate with the existing OASIS provider system
                var result = new OASISResult<T>();
                
                // Implement actual routing logic based on request type
                result = await RouteRequestAsync<T>(request, provider);
                
                return result;
            }
            catch (Exception ex)
            {
                return new OASISResult<T>
                {
                    IsError = true,
                    Message = $"Provider routing failed: {ex.Message}",
                    Exception = ex
                };
            }
        }

        private async Task<OASISResult<T>> RouteRequestAsync<T>(IRequest request, IOASISStorageProvider provider)
        {
            try
            {
                // Route based on request type
                switch (request)
                {
                    case SaveHolonRequest saveRequest:
                        return await provider.SaveHolonAsync<T>(saveRequest.Holon) as OASISResult<T>;
                    
                    case LoadHolonRequest loadRequest:
                        return await provider.LoadHolonAsync<T>(loadRequest.Id) as OASISResult<T>;
                    
                    case LoadHolonsRequest loadHolonsRequest:
                        return await provider.LoadHolonsAsync<T>(loadHolonsRequest.ParentId) as OASISResult<T>;
                    
                    case DeleteHolonRequest deleteRequest:
                        return await provider.DeleteHolonAsync(deleteRequest.Id) as OASISResult<T>;
                    
                    case SaveAvatarRequest saveAvatarRequest:
                        return await provider.SaveAvatarAsync(saveAvatarRequest.Avatar) as OASISResult<T>;
                    
                    case LoadAvatarRequest loadAvatarRequest:
                        return await provider.LoadAvatarAsync(loadAvatarRequest.Id) as OASISResult<T>;
                    
                    case LoadAvatarByEmailRequest loadByEmailRequest:
                        return await provider.LoadAvatarByEmailAsync(loadByEmailRequest.Email) as OASISResult<T>;
                    
                    case LoadAvatarByUsernameRequest loadByUsernameRequest:
                        return await provider.LoadAvatarByUsernameAsync(loadByUsernameRequest.Username) as OASISResult<T>;
                    
                    case DeleteAvatarRequest deleteAvatarRequest:
                        return await provider.DeleteAvatarAsync(deleteAvatarRequest.Id) as OASISResult<T>;
                    
                    default:
                        return new OASISResult<T>
                        {
                            IsError = true,
                            Message = $"Unsupported request type: {request.GetType().Name}"
                        };
                }
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

        private async Task<OASISResult<T>> HandleFailoverAsync<T>(IRequest request, IOASISStorageProvider failedProvider)
        {
            return await _failoverManager.HandleFailoverAsync<T>(request, failedProvider);
        }
    }

    /// <summary>
    /// Routing strategies for HyperDrive
    /// </summary>
    public enum RoutingStrategy
    {
        Auto,
        Performance,
        Cost,
        Geographic,
        LoadBalanced
    }

    /// <summary>
    /// Load balancing strategies
    /// </summary>
    public enum LoadBalancingStrategy
    {
        Auto,
        RoundRobin,
        WeightedRoundRobin,
        LeastConnections,
        Geographic,
        CostBased
    }

    /// <summary>
    /// Replication strategies
    /// </summary>
    public enum ReplicationStrategy
    {
        Synchronous,
        Asynchronous,
        Smart,
        Critical
    }

    /// <summary>
    /// Cost optimization strategies
    /// </summary>
    public enum CostOptimizationStrategy
    {
        Balanced,
        CostFirst,
        PerformanceFirst,
        Geographic
    }

    /// <summary>
    /// Network conditions for routing decisions
    /// </summary>
    public class NetworkConditions
    {
        public GeographicLocation UserLocation { get; set; }
        public double NetworkLatency { get; set; }
        public double NetworkBandwidth { get; set; }
        public bool IsOffline { get; set; }
        public List<string> AvailableRegions { get; set; } = new List<string>();
    }

    /// <summary>
    /// Geographic location for routing
    /// </summary>
    public class GeographicLocation
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string Country { get; set; }
        public string Region { get; set; }
        public string City { get; set; }
    }

    // Enhanced HyperDrive Management Classes

    /// <summary>
    /// Manages subscription-based HyperDrive configuration
    /// </summary>
    public class SubscriptionManager
    {
        public async Task<OASISResult<SubscriptionConfig>> GetSubscriptionConfigAsync()
        {
            try
            {
                var dna = OASISDNAManager.Instance.OASISDNA;
                return new OASISResult<SubscriptionConfig>
                {
                    Result = dna?.SubscriptionConfig ?? new SubscriptionConfig(),
                    IsSuccess = true
                };
            }
            catch (Exception ex)
            {
                return new OASISResult<SubscriptionConfig>
                {
                    IsError = true,
                    Message = $"Failed to get subscription config: {ex.Message}",
                    Exception = ex
                };
            }
        }

        public async Task<OASISResult<bool>> UpdateSubscriptionConfigAsync(SubscriptionConfig config)
        {
            try
            {
                var dna = OASISDNAManager.Instance.OASISDNA;
                if (dna != null)
                {
                    dna.SubscriptionConfig = config;
                    await OASISDNAManager.Instance.SaveOASISDNAAsync();
                }
                
                return new OASISResult<bool>
                {
                    Result = true,
                    IsSuccess = true
                };
            }
            catch (Exception ex)
            {
                return new OASISResult<bool>
                {
                    IsError = true,
                    Message = $"Failed to update subscription config: {ex.Message}",
                    Exception = ex
                };
            }
        }

        public async Task<OASISResult<bool>> CheckQuotaStatusAsync(string quotaType, int currentUsage)
        {
            try
            {
                var config = await GetSubscriptionConfigAsync();
                if (config.IsError) return new OASISResult<bool> { IsError = true, Message = config.Message };

                var limits = GetQuotaLimits(config.Result);
                var limit = limits.ContainsKey(quotaType) ? limits[quotaType] : 0;
                
                return new OASISResult<bool>
                {
                    Result = currentUsage < limit,
                    IsSuccess = true
                };
            }
            catch (Exception ex)
            {
                return new OASISResult<bool>
                {
                    IsError = true,
                    Message = $"Failed to check quota status: {ex.Message}",
                    Exception = ex
                };
            }
        }

        private Dictionary<string, int> GetQuotaLimits(SubscriptionConfig config)
        {
            return new Dictionary<string, int>
            {
                { "Replications", config.MaxReplicationsPerMonth },
                { "Failovers", config.MaxFailoversPerMonth },
                { "Storage", config.MaxStorageGB },
                { "Requests", GetRequestLimit(config.PlanType) }
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
    }

    /// <summary>
    /// Manages data permissions for HyperDrive
    /// </summary>
    public class DataPermissionsManager
    {
        public async Task<OASISResult<DataPermissionsConfig>> GetDataPermissionsAsync()
        {
            try
            {
                var dna = OASISDNAManager.Instance.OASISDNA;
                return new OASISResult<DataPermissionsConfig>
                {
                    Result = dna?.DataPermissions ?? new DataPermissionsConfig(),
                    IsSuccess = true
                };
            }
            catch (Exception ex)
            {
                return new OASISResult<DataPermissionsConfig>
                {
                    IsError = true,
                    Message = $"Failed to get data permissions: {ex.Message}",
                    Exception = ex
                };
            }
        }

        public async Task<OASISResult<bool>> UpdateDataPermissionsAsync(DataPermissionsConfig permissions)
        {
            try
            {
                var dna = OASISDNAManager.Instance.OASISDNA;
                if (dna != null)
                {
                    dna.DataPermissions = permissions;
                    await OASISDNAManager.Instance.SaveOASISDNAAsync();
                }
                
                return new OASISResult<bool>
                {
                    Result = true,
                    IsSuccess = true
                };
            }
            catch (Exception ex)
            {
                return new OASISResult<bool>
                {
                    IsError = true,
                    Message = $"Failed to update data permissions: {ex.Message}",
                    Exception = ex
                };
            }
        }

        public async Task<OASISResult<bool>> CheckFieldPermissionAsync(string fieldName, string providerType, string dataType)
        {
            try
            {
                var permissions = await GetDataPermissionsAsync();
                if (permissions.IsError) return new OASISResult<bool> { IsError = true, Message = permissions.Message };

                // Check if field is allowed for this provider and data type
                var isAllowed = CheckFieldAccess(permissions.Result, fieldName, providerType, dataType);
                
                return new OASISResult<bool>
                {
                    Result = isAllowed,
                    IsSuccess = true
                };
            }
            catch (Exception ex)
            {
                return new OASISResult<bool>
                {
                    IsError = true,
                    Message = $"Failed to check field permission: {ex.Message}",
                    Exception = ex
                };
            }
        }

        private bool CheckFieldAccess(DataPermissionsConfig permissions, string fieldName, string providerType, string dataType)
        {
            // Check avatar field permissions
            if (dataType == "Avatar")
            {
                var fieldPermission = permissions.AvatarPermissions.Fields.FirstOrDefault(f => f.FieldName == fieldName);
                if (fieldPermission != null)
                {
                    return fieldPermission.ProviderTypes.Contains(providerType);
                }
            }

            // Check holon field permissions
            var holonPermission = permissions.HolonPermissions.HolonTypes.FirstOrDefault(h => h.HolonType == dataType);
            if (holonPermission != null)
            {
                return holonPermission.ProviderTypes.Contains(providerType);
            }

            return false;
        }
    }

    /// <summary>
    /// Manages intelligent mode for HyperDrive
    /// </summary>
    public class IntelligentModeManager
    {
        public async Task<OASISResult<IntelligentModeConfig>> GetIntelligentModeAsync()
        {
            try
            {
                var dna = OASISDNAManager.Instance.OASISDNA;
                return new OASISResult<IntelligentModeConfig>
                {
                    Result = dna?.IntelligentMode ?? new IntelligentModeConfig(),
                    IsSuccess = true
                };
            }
            catch (Exception ex)
            {
                return new OASISResult<IntelligentModeConfig>
                {
                    IsError = true,
                    Message = $"Failed to get intelligent mode: {ex.Message}",
                    Exception = ex
                };
            }
        }

        public async Task<OASISResult<bool>> UpdateIntelligentModeAsync(IntelligentModeConfig mode)
        {
            try
            {
                var dna = OASISDNAManager.Instance.OASISDNA;
                if (dna != null)
                {
                    dna.IntelligentMode = mode;
                    await OASISDNAManager.Instance.SaveOASISDNAAsync();
                }
                
                return new OASISResult<bool>
                {
                    Result = true,
                    IsSuccess = true
                };
            }
            catch (Exception ex)
            {
                return new OASISResult<bool>
                {
                    IsError = true,
                    Message = $"Failed to update intelligent mode: {ex.Message}",
                    Exception = ex
                };
            }
        }

        public async Task<OASISResult<bool>> IsIntelligentModeEnabledAsync()
        {
            try
            {
                var mode = await GetIntelligentModeAsync();
                if (mode.IsError) return new OASISResult<bool> { IsError = true, Message = mode.Message };
                
                return new OASISResult<bool>
                {
                    Result = mode.Result.IsEnabled,
                    IsSuccess = true
                };
            }
            catch (Exception ex)
            {
                return new OASISResult<bool>
                {
                    IsError = true,
                    Message = $"Failed to check intelligent mode: {ex.Message}",
                    Exception = ex
                };
            }
        }
    }
}
