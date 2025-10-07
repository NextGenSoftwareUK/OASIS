using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Configuration;
using NextGenSoftware.Utilities;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.Core.Managers
{
    /// <summary>
    /// Handles all provider selection algorithms and load balancing strategies
    /// </summary>
    public class ProviderSelector
    {
        private static ProviderSelector _instance;
        private static readonly object _lock = new object();

        public static ProviderSelector Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                            _instance = new ProviderSelector();
                    }
                }
                return _instance;
            }
        }

        private ProviderSelector() { }

        private readonly ProviderRegistry _registry = ProviderRegistry.Instance;
        private readonly PerformanceMonitor _performanceMonitor = PerformanceMonitor.Instance;
        private readonly OASISHyperDriveConfigManager _configManager = OASISHyperDriveConfigManager.Instance;

        // Round-robin state
        private int _roundRobinIndex = 0;
        private readonly object _roundRobinLock = new object();

        /// <summary>
        /// Selects optimal provider for load balancing based on strategy
        /// </summary>
        public EnumValue<ProviderType> SelectOptimalProviderForLoadBalancing(LoadBalancingStrategy strategy = LoadBalancingStrategy.Auto)
        {
            var availableProviders = _registry.ProviderAutoLoadBalanceList;
            if (availableProviders.Count == 0)
                return _registry.CurrentStorageProviderType;

            // Use configuration-based strategy if Auto is selected
            if (strategy == LoadBalancingStrategy.Auto)
            {
                var config = _configManager.GetConfiguration();
                var configured = config?.DefaultStrategy;
                if (!string.IsNullOrWhiteSpace(configured) && Enum.TryParse(configured, true, out LoadBalancingStrategy parsed))
                    strategy = parsed;
            }

            return strategy switch
            {
                LoadBalancingStrategy.RoundRobin => SelectRoundRobinProvider(availableProviders),
                LoadBalancingStrategy.WeightedRoundRobin => SelectWeightedRoundRobinProvider(availableProviders),
                LoadBalancingStrategy.LeastConnections => SelectLeastConnectionsProvider(availableProviders),
                LoadBalancingStrategy.Geographic => SelectGeographicProvider(availableProviders),
                LoadBalancingStrategy.CostBased => SelectCostBasedProvider(availableProviders),
                LoadBalancingStrategy.Performance => SelectPerformanceBasedProvider(availableProviders),
                LoadBalancingStrategy.Intelligent => SelectIntelligentProvider(availableProviders),
                _ => _registry.CurrentStorageProviderType
            };
        }

        /// <summary>
        /// Round-robin provider selection
        /// </summary>
        private EnumValue<ProviderType> SelectRoundRobinProvider(List<EnumValue<ProviderType>> providers)
        {
            if (!providers.Any()) return _registry.CurrentStorageProviderType;

            lock (_roundRobinLock)
            {
                var selectedProvider = providers[_roundRobinIndex % providers.Count];
                _roundRobinIndex++;
                return selectedProvider;
            }
        }

        /// <summary>
        /// Weighted round-robin provider selection
        /// </summary>
        private EnumValue<ProviderType> SelectWeightedRoundRobinProvider(List<EnumValue<ProviderType>> providers)
        {
            if (!providers.Any()) return _registry.CurrentStorageProviderType;

            var weights = providers.ToDictionary(p => p, p => GetProviderWeight(p));
            var totalWeight = weights.Values.Sum();
            var random = new Random().NextDouble() * totalWeight;
            
            var currentWeight = 0.0;
            foreach (var provider in providers)
            {
                currentWeight += weights[provider];
                if (random <= currentWeight)
                    return provider;
            }
            
            return providers.First();
        }

        /// <summary>
        /// Least connections provider selection
        /// </summary>
        private EnumValue<ProviderType> SelectLeastConnectionsProvider(List<EnumValue<ProviderType>> providers)
        {
            if (!providers.Any()) return _registry.CurrentStorageProviderType;

            return providers
                .OrderBy(p => _performanceMonitor.GetMetrics(p.Value)?.ActiveConnections ?? 0)
                .First();
        }

        /// <summary>
        /// Geographic provider selection
        /// </summary>
        private EnumValue<ProviderType> SelectGeographicProvider(List<EnumValue<ProviderType>> providers)
        {
            if (!providers.Any()) return _registry.CurrentStorageProviderType;

            // This would use geographic routing logic
            // For now, return first available provider
            return providers.First();
        }

        /// <summary>
        /// Cost-based provider selection
        /// </summary>
        private EnumValue<ProviderType> SelectCostBasedProvider(List<EnumValue<ProviderType>> providers)
        {
            if (!providers.Any()) return _registry.CurrentStorageProviderType;

            // Select provider with lowest cost
            return providers
                .OrderBy(p => GetProviderCost(p))
                .First();
        }

        /// <summary>
        /// Performance-based provider selection
        /// </summary>
        internal EnumValue<ProviderType> SelectPerformanceBasedProvider(List<EnumValue<ProviderType>> providers)
        {
            if (!providers.Any()) return _registry.CurrentStorageProviderType;

            var config = _configManager.GetConfiguration();
            var providerScores = new Dictionary<EnumValue<ProviderType>, double>();

            foreach (var provider in providers)
            {
                var metrics = _performanceMonitor.GetMetrics(provider.Value);
                if (metrics == null) continue;

                // Calculate performance score with lag/ping as primary criteria
                var latencyScore = CalculateLatencyScore(metrics);
                var performanceScore = CalculatePerformanceScore(metrics);
                var reliabilityScore = CalculateReliabilityScore(metrics);

                var score = (latencyScore * config.LatencyWeight) + 
                           (performanceScore * config.ThroughputWeight) + 
                           (reliabilityScore * config.ReliabilityWeight);
                
                providerScores[provider] = score;
            }

            return providerScores.OrderByDescending(x => x.Value).First().Key;
        }

        /// <summary>
        /// Intelligent provider selection using AI
        /// </summary>
        private EnumValue<ProviderType> SelectIntelligentProvider(List<EnumValue<ProviderType>> providers)
        {
            if (!providers.Any()) return _registry.CurrentStorageProviderType;

            // Use AI optimization engine for intelligent selection
            var aiEngine = AIOptimizationEngine.Instance;
            var recommendations = aiEngine.GetProviderRecommendationsAsync(new StorageOperationRequest(), providers.Select(p => p.Value).ToList()).Result;
            var selectedProvider = recommendations.FirstOrDefault()?.Value ?? providers.First().Value;
            
            return new EnumValue<ProviderType>(selectedProvider);
        }

        /// <summary>
        /// Calculates latency score (lower latency = higher score)
        /// </summary>
        private double CalculateLatencyScore(ProviderPerformanceMetrics metrics)
        {
            return Math.Max(0, 1000 - metrics.ResponseTimeMs) / 1000.0;
        }

        /// <summary>
        /// Calculates performance score
        /// </summary>
        private double CalculatePerformanceScore(ProviderPerformanceMetrics metrics)
        {
            return (metrics.ThroughputMbps / 100.0) * (metrics.UptimePercentage / 100.0);
        }

        /// <summary>
        /// Calculates reliability score
        /// </summary>
        private double CalculateReliabilityScore(ProviderPerformanceMetrics metrics)
        {
            return (metrics.UptimePercentage / 100.0) * (1.0 - metrics.ErrorRate);
        }

        /// <summary>
        /// Gets provider weight for weighted selection
        /// </summary>
        private double GetProviderWeight(EnumValue<ProviderType> provider)
        {
            var metrics = _performanceMonitor.GetMetrics(provider.Value);
            if (metrics == null) return 1.0;
            
            return (metrics.UptimePercentage / 100.0) * (1.0 - metrics.ErrorRate);
        }

        /// <summary>
        /// Gets provider cost (simplified implementation)
        /// </summary>
        private decimal GetProviderCost(EnumValue<ProviderType> provider)
        {
            // This would integrate with actual cost data
            // For now, return a simple cost based on provider type
            return provider.Value switch
            {
                ProviderType.EthereumOASIS => 0.05m,
                ProviderType.TRONOASIS => 0.03m,
                ProviderType.ChainLinkOASIS => 0.02m,
                ProviderType.IPFSOASIS => 0.01m,
                ProviderType.SQLLiteDBOASIS => 0.00m,
                ProviderType.MongoDBOASIS => 0.00m,
                _ => 0.01m
            };
        }

        /// <summary>
        /// Selects provider for auto-failover
        /// </summary>
        public EnumValue<ProviderType> SelectFailoverProvider(EnumValue<ProviderType> currentProvider)
        {
            var failoverProviders = _registry.ProviderAutoFailOverList
                .Where(p => p.Value != currentProvider.Value)
                .ToList();

            if (!failoverProviders.Any())
                return _registry.CurrentStorageProviderType;

            // Select based on performance and reliability
            return SelectPerformanceBasedProvider(failoverProviders);
        }

        /// <summary>
        /// Selects provider for auto-replication
        /// </summary>
        public EnumValue<ProviderType> SelectReplicationProvider(EnumValue<ProviderType> currentProvider)
        {
            var replicationProviders = _registry.ProviderAutoReplicationList
                .Where(p => p.Value != currentProvider.Value)
                .ToList();

            if (!replicationProviders.Any())
                return _registry.CurrentStorageProviderType;

            // Select based on cost and performance
            return SelectCostBasedProvider(replicationProviders);
        }
    }
}
