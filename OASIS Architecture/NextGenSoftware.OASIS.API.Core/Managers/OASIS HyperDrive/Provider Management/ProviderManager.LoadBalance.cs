using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using NextGenSoftware.OASIS.API.Core.Configuration;

namespace NextGenSoftware.OASIS.API.Core.Managers
{
    public partial class ProviderManager
    {
        public string GetProviderAutoFailOverListAsString()
        {
            return GetProviderListAsString(GetProviderAutoFailOverList());
        }

        public string GetProviderAutoFailOverListForAvatarLoginAsString()
        {
            return GetProviderListAsString(GetProviderAutoFailOverListForAvatarLogin());
        }

        public string GetProviderAutoFailOverListForCheckIfEmailAlreadyInUseAsString()
        {
            return GetProviderListAsString(GetProviderAutoFailOverListForCheckIfEmailAlreadyInUse());
        }

        public string GetProviderAutoFailOverListForCheckIfUsernameAlreadyInUseAsString()
        {
            return GetProviderListAsString(GetProviderAutoFailOverListForCheckIfUsernameAlreadyInUse());
        }

        public string GetProvidersThatAreAutoReplicatingAsString()
        {
            return GetProviderListAsString(GetProvidersThatAreAutoReplicating());
        }

        public string GetProviderAutoLoadBalanceListAsString()
        {
            return GetProviderListAsString(GetProviderAutoLoadBalanceList());
        }

        public string GetProviderAutoFailOverLocalListAsString()
        {
            return GetProviderListAsString(GetProviderAutoFailOverLocalList());
        }

        public string GetProviderListAsString(List<ProviderType> providerList)
        {
            return GetProviderListAsString(EnumHelper.ConvertToEnumValueList(providerList).ToList());
        }

        public string GetProviderListAsString(List<EnumValue<ProviderType>> providerList)
        {
            string list = "";

            for (int i = 0; i < providerList.Count(); i++)
            {
                list = string.Concat(list, providerList[i].Name);

                if (i < providerList.Count - 2)
                    list = string.Concat(list, ", ");

                else if (i == providerList.Count() - 2)
                    list = string.Concat(list, " & ");
            }

            return list;
        }

        //public List<EnumValue<ProviderType>> GetProvidersThatAreAutoReplicating()
        //{
        //    return _providersThatAreAutoReplicating;
        //}

        public List<EnumValue<ProviderType>> GetProvidersThatAreAutoReplicating()
        {
            //TODO: Handle OASISResult properly and make all methods return OASISResult ASAP!
            //string providerListCache = GetProviderListAsString(_providersThatAreAutoReplicating);
            //return GetProvidersFromListAsEnumList("AutoReplicate", providerListCache).Result.ToList();

            return _providersThatAreAutoReplicating;
        }

        private bool SetProviderList(bool add, IEnumerable<ProviderType> providers, List<EnumValue<ProviderType>> listToAddTo)
        {
            foreach (ProviderType providerType in providers)
            {
                if (add && !listToAddTo.Any(x => x.Value == providerType))
                    listToAddTo.Add(new EnumValue<ProviderType>(providerType));

                else if (!add)
                {
                    foreach (EnumValue<ProviderType> type in listToAddTo)
                    {
                        if (type.Value == providerType)
                        {
                            listToAddTo.Remove(type);
                            break;
                        }
                    }
                }
            }

            return true;
        }


        /// <summary>
        /// Gets the list of providers configured for auto-load balancing
        /// </summary>
        // Duplicate removed: method already defined earlier

        /// <summary>
        /// Gets the auto-load balance list as a string for logging/debugging
        /// </summary>
        // Duplicate removed: method already defined earlier

        /// <summary>
        /// Adds providers to the auto-load balance list
        /// </summary>
        public bool AddProvidersToAutoLoadBalanceList(IEnumerable<ProviderType> providers)
        {
            return SetProviderList(true, providers, _providerAutoLoadBalanceList);
        }

        /// <summary>
        /// Removes providers from the auto-load balance list
        /// </summary>
        public bool RemoveProvidersFromAutoLoadBalanceList(IEnumerable<ProviderType> providers)
        {
            return SetProviderList(false, providers, _providerAutoLoadBalanceList);
        }

        /// <summary>
        /// Clears the auto-load balance list
        /// </summary>
        public void ClearAutoLoadBalanceList()
        {
            _providerAutoLoadBalanceList.Clear();
        }

        /// <summary>
        /// Selects the optimal provider for load balancing based on current conditions
        /// </summary>
        public EnumValue<ProviderType> SelectOptimalProviderForLoadBalancing(LoadBalancingStrategy strategy = LoadBalancingStrategy.Auto)
        {
            if (!IsAutoLoadBalanceEnabled || _providerAutoLoadBalanceList.Count == 0)
                return CurrentStorageProviderType;

            // Use configuration-based strategy if Auto is selected
            if (strategy == LoadBalancingStrategy.Auto)
            {
                var config = OASISHyperDriveConfigManager.Instance.GetConfiguration();
                var configured = config?.DefaultStrategy;
                if (!string.IsNullOrWhiteSpace(configured) && Enum.TryParse(configured, true, out LoadBalancingStrategy parsed))
                    strategy = parsed;
                else
                    strategy = LoadBalancingStrategy.Auto;
            }

            return strategy switch
            {
                LoadBalancingStrategy.RoundRobin => SelectRoundRobinProvider(),
                LoadBalancingStrategy.WeightedRoundRobin => SelectWeightedRoundRobinProvider(),
                LoadBalancingStrategy.LeastConnections => SelectLeastConnectionsProvider(),
                LoadBalancingStrategy.Geographic => SelectGeographicProvider(),
                LoadBalancingStrategy.CostBased => SelectCostBasedProvider(),
                LoadBalancingStrategy.Performance => SelectPerformanceBasedProvider(),
                _ => SelectAutoProvider()
            };
        }

        /// <summary>
        /// Performance-based provider selection with lag/ping as primary criteria
        /// </summary>
        private EnumValue<ProviderType> SelectPerformanceBasedProvider()
        {
            if (_providerAutoLoadBalanceList.Count == 0)
                return CurrentStorageProviderType;

            var performanceMonitor = PerformanceMonitor.Instance;
            var config = OASISHyperDriveConfigManager.Instance.GetConfiguration();
            var providerScores = new Dictionary<EnumValue<ProviderType>, double>();

            // Calculate performance scores with lag/ping as primary criteria
            foreach (var provider in _providerAutoLoadBalanceList)
            {
                var metrics = performanceMonitor.GetMetrics(provider.Value);
                var geoInfo = performanceMonitor.GetGeographicInfo(provider.Value);
                
                if (metrics == null)
                {
                    providerScores[provider] = 0;
                    continue;
                }

                // Primary criteria: Latency/Ping (50% weight from config)
                var latencyScore = CalculateLatencyScore(metrics, geoInfo);
                
                // Secondary criteria: Performance (30% weight from config)
                var performanceScore = CalculatePerformanceScore(metrics);
                
                // Tertiary criteria: Reliability (20% weight from config)
                var reliabilityScore = CalculateReliabilityScore(metrics);

                // Weighted combination with lag/ping as primary
                var score = (latencyScore * config.LatencyWeight) + 
                           (performanceScore * config.ThroughputWeight) + 
                           (reliabilityScore * config.ReliabilityWeight);
                
                providerScores[provider] = score;
            }

            // Select provider with highest score
            var bestScore = providerScores.Values.Max();
            var bestProviders = providerScores
                .Where(x => x.Value == bestScore)
                .Select(x => x.Key)
                .ToList();

            // If multiple providers have same score, use latency as tie-breaker
            if (bestProviders.Count > 1)
            {
                var lowestLatency = double.MaxValue;
                EnumValue<ProviderType> bestProvider = bestProviders.First();

                foreach (var provider in bestProviders)
                {
                    var metrics = performanceMonitor.GetMetrics(provider.Value);
                    var latency = metrics?.ResponseTimeMs ?? 1000; // Default high latency
                    
                    if (latency < lowestLatency)
                    {
                        lowestLatency = latency;
                        bestProvider = provider;
                    }
                }

                return bestProvider;
            }

            return bestProviders.First();
        }

        /// <summary>
        /// Calculates reliability score
        /// </summary>
        private double CalculateReliabilityScore(ProviderPerformanceMetrics metrics)
        {
            var uptimeScore = metrics.UptimePercentage;
            var errorRateScore = Math.Max(0, 100 - (metrics.ErrorRate * 100));
            
            return (uptimeScore * 0.6) + (errorRateScore * 0.4);
        }

        /// <summary>
        /// Round Robin load balancing - distributes requests evenly
        /// </summary>
        private EnumValue<ProviderType> SelectRoundRobinProvider()
        {
            if (_providerAutoLoadBalanceList.Count == 0)
                return CurrentStorageProviderType;

            // Simple round robin implementation
            var index = DateTime.Now.Millisecond % _providerAutoLoadBalanceList.Count;
            return _providerAutoLoadBalanceList[index];
        }

        /// <summary>
        /// Weighted Round Robin - weights providers based on performance
        /// </summary>
        private EnumValue<ProviderType> SelectWeightedRoundRobinProvider()
        {
            if (_providerAutoLoadBalanceList.Count == 0)
                return CurrentStorageProviderType;

            var performanceMonitor = PerformanceMonitor.Instance;
            var providerWeights = new Dictionary<EnumValue<ProviderType>, double>();

            // Calculate weights based on performance metrics
            foreach (var provider in _providerAutoLoadBalanceList)
            {
                var metrics = performanceMonitor.GetMetrics(provider.Value);
                var weight = metrics?.OverallScore ?? 50; // Default weight if no metrics
                providerWeights[provider] = Math.Max(1, weight); // Ensure minimum weight of 1
            }

            // Select provider based on weighted random selection
            var totalWeight = providerWeights.Values.Sum();
            var random = new Random().NextDouble() * totalWeight;
            var currentWeight = 0.0;

            foreach (var provider in _providerAutoLoadBalanceList)
            {
                currentWeight += providerWeights[provider];
                if (random <= currentWeight)
                    return provider;
            }

            return _providerAutoLoadBalanceList.First();
        }

        /// <summary>
        /// Least Connections - routes to provider with fewest active connections
        /// </summary>
        private EnumValue<ProviderType> SelectLeastConnectionsProvider()
        {
            if (_providerAutoLoadBalanceList.Count == 0)
                return CurrentStorageProviderType;

            var performanceMonitor = PerformanceMonitor.Instance;
            var providerConnections = new Dictionary<EnumValue<ProviderType>, int>();

            // Get connection counts for all providers
            foreach (var provider in _providerAutoLoadBalanceList)
            {
                var connections = performanceMonitor.GetActiveConnections(provider.Value);
                providerConnections[provider] = connections;
            }

            // Select provider with least connections
            var leastConnections = providerConnections.Values.Min();
            var leastConnectedProviders = providerConnections
                .Where(x => x.Value == leastConnections)
                .Select(x => x.Key)
                .ToList();

            // If multiple providers have same connection count, use round robin among them
            if (leastConnectedProviders.Count > 1)
            {
                var index = DateTime.Now.Millisecond % leastConnectedProviders.Count;
                return leastConnectedProviders[index];
            }

            return leastConnectedProviders.First();
        }

        /// <summary>
        /// Geographic routing - routes to nearest provider
        /// </summary>
        private EnumValue<ProviderType> SelectGeographicProvider()
        {
            if (_providerAutoLoadBalanceList.Count == 0)
                return CurrentStorageProviderType;

            var performanceMonitor = PerformanceMonitor.Instance;
            var providerScores = new Dictionary<EnumValue<ProviderType>, double>();

            // Calculate geographic scores for all providers
            foreach (var provider in _providerAutoLoadBalanceList)
            {
                var geoInfo = performanceMonitor.GetGeographicInfo(provider.Value);
                var score = geoInfo?.NetworkLatency ?? 50; // Default score if no geo info
                providerScores[provider] = score;
            }

            // Select provider with best geographic score (lowest latency)
            var bestScore = providerScores.Values.Min();
            var bestProviders = providerScores
                .Where(x => x.Value == bestScore)
                .Select(x => x.Key)
                .ToList();

            // If multiple providers have same score, use round robin among them
            if (bestProviders.Count > 1)
            {
                var index = DateTime.Now.Millisecond % bestProviders.Count;
                return bestProviders[index];
            }

            return bestProviders.First();
        }

        /// <summary>
        /// Cost-based routing - routes to most cost-effective provider
        /// </summary>
        private EnumValue<ProviderType> SelectCostBasedProvider()
        {
            if (_providerAutoLoadBalanceList.Count == 0)
                return CurrentStorageProviderType;

            var performanceMonitor = PerformanceMonitor.Instance;
            var providerCosts = new Dictionary<EnumValue<ProviderType>, double>();

            // Calculate cost scores for all providers
            foreach (var provider in _providerAutoLoadBalanceList)
            {
                var costAnalysis = performanceMonitor.GetCostAnalysis(provider.Value);
                var metrics = performanceMonitor.GetMetrics(provider.Value);
                
                // Use cost analysis if available, otherwise use metrics cost
                var cost = costAnalysis?.TotalCost ?? metrics?.CostPerOperation ?? 0.01; // Default cost
                providerCosts[provider] = cost;
            }

            // Select provider with lowest cost
            var lowestCost = providerCosts.Values.Min();
            var lowestCostProviders = providerCosts
                .Where(x => x.Value == lowestCost)
                .Select(x => x.Key)
                .ToList();

            // If multiple providers have same cost, use performance metrics to break tie
            if (lowestCostProviders.Count > 1)
            {
                var bestPerformance = double.MaxValue;
                EnumValue<ProviderType> bestProvider = lowestCostProviders.First();

                foreach (var provider in lowestCostProviders)
                {
                    var metrics = performanceMonitor.GetMetrics(provider.Value);
                    var performanceScore = metrics?.ResponseTimeMs ?? 100; // Default response time
                    
                    if (performanceScore < bestPerformance)
                    {
                        bestPerformance = performanceScore;
                        bestProvider = provider;
                    }
                }

                return bestProvider;
            }

            return lowestCostProviders.First();
        }

        /// <summary>
        /// Auto selection - intelligently selects based on multiple factors
        /// </summary>
        private EnumValue<ProviderType> SelectAutoProvider()
        {
            if (_providerAutoLoadBalanceList.Count == 0)
                return CurrentStorageProviderType;

            var performanceMonitor = PerformanceMonitor.Instance;
            var providerScores = new Dictionary<EnumValue<ProviderType>, double>();

            // Calculate comprehensive scores for all providers
            foreach (var provider in _providerAutoLoadBalanceList)
            {
                var metrics = performanceMonitor.GetMetrics(provider.Value);
                var geoInfo = performanceMonitor.GetGeographicInfo(provider.Value);
                var costAnalysis = performanceMonitor.GetCostAnalysis(provider.Value);

                // Calculate weighted score based on multiple factors
                var score = CalculateIntelligentScore(metrics, geoInfo, costAnalysis, provider.Value);
                providerScores[provider] = score;
            }

            // Select provider with highest score
            var bestScore = providerScores.Values.Max();
            var bestProviders = providerScores
                .Where(x => x.Value == bestScore)
                .Select(x => x.Key)
                .ToList();

            // If multiple providers have same score, use latency as tie-breaker
            if (bestProviders.Count > 1)
            {
                var lowestLatency = double.MaxValue;
                EnumValue<ProviderType> bestProvider = bestProviders.First();

                foreach (var provider in bestProviders)
                {
                    var metrics = performanceMonitor.GetMetrics(provider.Value);
                    var latency = metrics?.ResponseTimeMs ?? 1000; // Default high latency
                    
                    if (latency < lowestLatency)
                    {
                        lowestLatency = latency;
                        bestProvider = provider;
                    }
                }

                return bestProvider;
            }

            return bestProviders.First();
        }

        /// <summary>
        /// Calculates intelligent score based on multiple factors with lag/ping as primary criteria
        /// </summary>
        private double CalculateIntelligentScore(ProviderPerformanceMetrics metrics, GeographicInfo geoInfo, CostAnalysis costAnalysis, ProviderType providerType)
        {
            if (metrics == null)
                return 0;

            // Primary criteria: Latency/Ping (40% weight)
            var latencyScore = CalculateLatencyScore(metrics, geoInfo);
            
            // Secondary criteria: Performance (25% weight)
            var performanceScore = CalculatePerformanceScore(metrics);
            
            // Tertiary criteria: Cost (15% weight)
            var costScore = CalculateCostScore(metrics, costAnalysis);
            
            // Quaternary criteria: Geographic (10% weight)
            var geographicScore = CalculateGeographicScore(geoInfo);
            
            // Quinary criteria: Availability (10% weight)
            var availabilityScore = CalculateAvailabilityScore(metrics);

            // Weighted combination with lag/ping as primary
            return (latencyScore * 0.40) + 
                   (performanceScore * 0.25) + 
                   (costScore * 0.15) + 
                   (geographicScore * 0.10) + 
                   (availabilityScore * 0.10);
        }

        /// <summary>
        /// Calculates latency score (primary criteria)
        /// </summary>
        private double CalculateLatencyScore(ProviderPerformanceMetrics metrics, GeographicInfo geoInfo)
        {
            var latency = metrics.ResponseTimeMs;
            var networkLatency = geoInfo?.NetworkLatency ?? latency;
            var totalLatency = Math.Min(latency, networkLatency);

            // Lower latency = higher score (inverted)
            if (totalLatency <= 50) return 100;      // Excellent
            if (totalLatency <= 100) return 90;      // Very Good
            if (totalLatency <= 200) return 80;       // Good
            if (totalLatency <= 500) return 60;       // Fair
            if (totalLatency <= 1000) return 40;      // Poor
            return 20; // Very Poor
        }

        /// <summary>
        /// Calculates performance score
        /// </summary>
        private double CalculatePerformanceScore(ProviderPerformanceMetrics metrics)
        {
            var throughputScore = Math.Min(100, metrics.ThroughputMbps * 10);
            var errorRateScore = Math.Max(0, 100 - (metrics.ErrorRate * 100));
            var uptimeScore = metrics.UptimePercentage;

            return (throughputScore * 0.4) + (errorRateScore * 0.3) + (uptimeScore * 0.3);
        }

        /// <summary>
        /// Calculates cost score
        /// </summary>
        private double CalculateCostScore(ProviderPerformanceMetrics metrics, CostAnalysis costAnalysis)
        {
            var cost = costAnalysis?.TotalCost ?? metrics.CostPerOperation;
            
            // Lower cost = higher score (inverted)
            if (cost <= 0.01) return 100;      // Excellent
            if (cost <= 0.05) return 90;       // Very Good
            if (cost <= 0.10) return 80;       // Good
            if (cost <= 0.25) return 60;        // Fair
            if (cost <= 0.50) return 40;        // Poor
            return 20; // Very Poor
        }

        /// <summary>
        /// Calculates geographic score
        /// </summary>
        private double CalculateGeographicScore(GeographicInfo geoInfo)
        {
            if (geoInfo == null) return 50; // Neutral score

            var latencyScore = Math.Max(0, 100 - (geoInfo.NetworkLatency * 10));
            var hopsScore = Math.Max(0, 100 - (geoInfo.NetworkHops * 5));

            return (latencyScore * 0.7) + (hopsScore * 0.3);
        }

        /// <summary>
        /// Calculates availability score
        /// </summary>
        private double CalculateAvailabilityScore(ProviderPerformanceMetrics metrics)
        {
            return metrics.UptimePercentage;
        }

    }
}
