using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive;

namespace NextGenSoftware.OASIS.API.Core.Managers
{
    /// <summary>
    /// Predictive failover engine for OASIS HyperDrive
    /// </summary>
    public class PredictiveFailoverEngine
    {
        private static PredictiveFailoverEngine _instance;
        private readonly PerformanceMonitor _performanceMonitor;
        private readonly AIOptimizationEngine _aiEngine;
        private readonly Dictionary<ProviderType, FailurePredictionModel> _failureModels;
        private readonly Dictionary<ProviderType, List<FailureEvent>> _failureHistory;
        private readonly List<PreventiveAction> _preventiveActions = new List<PreventiveAction>();
        private readonly object _lockObject = new object();

        /// <summary>
        /// Per-provider override recommendations written by the background prediction loop and read by
        /// RouteToProviderAsync on each incoming request. Keyed by the at-risk provider; value is the
        /// recommended replacement to use instead. This replaces the previous implementation which called
        /// ProviderManager.SetAndActivateCurrentStorageProviderAsync globally from the background loop,
        /// racing with concurrent in-flight storage operations.
        /// </summary>
        private readonly System.Collections.Concurrent.ConcurrentDictionary<ProviderType, ProviderType>
            _pendingFailoverOverrides = new();

        /// <summary>
        /// Returns the override provider for <paramref name="provider"/> if the background loop has flagged
        /// it as high-risk, or <see cref="ProviderType.Default"/> when no override is active.
        /// Called by OASISHyperDrive.RouteToProviderAsync on the hot path.
        /// </summary>
        public ProviderType GetFailoverOverride(ProviderType provider)
            => _pendingFailoverOverrides.TryGetValue(provider, out var alt) ? alt : ProviderType.Default;

        public static PredictiveFailoverEngine Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new PredictiveFailoverEngine();
                return _instance;
            }
        }

        private PredictiveFailoverEngine()
        {
            _performanceMonitor = PerformanceMonitor.Instance;
            _aiEngine = AIOptimizationEngine.Instance;
            _failureModels = new Dictionary<ProviderType, FailurePredictionModel>();
            _failureHistory = new Dictionary<ProviderType, List<FailureEvent>>();
        }

        /// <summary>
        /// Predicts potential failures and initiates preventive failover
        /// </summary>
        public async Task<FailoverPrediction> PredictAndPreventFailuresAsync()
        {
            var prediction = new FailoverPrediction
            {
                Timestamp = DateTime.UtcNow,
                Predictions = new List<ProviderFailurePrediction>(),
                RecommendedActions = new List<string>(),
                RiskLevel = RiskLevel.Low
            };

            var highRiskProviders = new List<ProviderType>();

            // Predict for every actually-registered provider, not just ones that already have a failure
            // model. _failureModels is only ever populated by RecordFailureEvent/UpdateFailureModel, so a
            // provider that's silently degrading (high latency/errors) but has never had a single recorded
            // FailureEvent would otherwise never be checked at all - the riskiest case (an undetected,
            // ongoing degradation) was exactly the one this method could never catch.
            var providersToCheck = ProviderManager.Instance.GetAllRegisteredProviders()
                .Select(p => p.ProviderType?.Value ?? ProviderType.Default)
                .Where(p => p != ProviderType.Default)
                .Union(_failureModels.Keys)
                .Distinct();

            foreach (var provider in providersToCheck)
            {
                var failurePrediction = await PredictProviderFailureAsync(provider);
                prediction.Predictions.Add(failurePrediction);

                if (failurePrediction.RiskLevel >= RiskLevel.High)
                {
                    highRiskProviders.Add(provider);
                }
            }

            // Determine overall risk level
            prediction.RiskLevel = DetermineOverallRiskLevel(prediction.Predictions);

            // Generate recommended actions
            prediction.RecommendedActions = GenerateFailoverRecommendations(prediction.Predictions);

            // Initiate preventive failover for high-risk providers
            if (highRiskProviders.Any())
            {
                await InitiatePreventiveFailoverAsync(highRiskProviders);
            }

            return prediction;
        }

        /// <summary>
        /// Predicts failure for a specific provider
        /// </summary>
        public async Task<ProviderFailurePrediction> PredictProviderFailureAsync(ProviderType providerType)
        {
            var metrics = _performanceMonitor.GetMetrics(providerType);
            var geoInfo = _performanceMonitor.GetGeographicInfo(providerType);
            var costAnalysis = _performanceMonitor.GetCostAnalysis(providerType);

            if (metrics == null)
            {
                return new ProviderFailurePrediction
                {
                    ProviderType = providerType,
                    RiskLevel = RiskLevel.Unknown,
                    FailureProbability = 0.1,
                    PredictedFailureTime = DateTime.UtcNow.AddHours(24),
                    Confidence = 0.1,
                    RiskFactors = new List<string> { "Insufficient data" },
                    RecommendedActions = new List<string> { "Monitor closely" }
                };
            }

            // Calculate failure probability using multiple factors
            var failureProbability = CalculateFailureProbability(metrics, geoInfo, costAnalysis);
            var riskLevel = DetermineRiskLevel(failureProbability);
            var predictedFailureTime = CalculatePredictedFailureTime(failureProbability, metrics);
            var confidence = CalculatePredictionConfidence(providerType);
            var riskFactors = IdentifyRiskFactors(metrics, geoInfo, costAnalysis);
            var recommendedActions = GenerateProviderRecommendations(failureProbability, riskFactors);

            return new ProviderFailurePrediction
            {
                ProviderType = providerType,
                RiskLevel = riskLevel,
                FailureProbability = failureProbability,
                PredictedFailureTime = predictedFailureTime,
                Confidence = confidence,
                RiskFactors = riskFactors,
                RecommendedActions = recommendedActions
            };
        }

        /// <summary>
        /// Records a failure event for learning
        /// </summary>
        public void RecordFailureEvent(ProviderType providerType, FailureEvent failureEvent)
        {
            lock (_lockObject)
            {
                if (!_failureHistory.ContainsKey(providerType))
                {
                    _failureHistory[providerType] = new List<FailureEvent>();
                }

                _failureHistory[providerType].Add(failureEvent);

                // Keep only last 1000 events to prevent memory issues
                if (_failureHistory[providerType].Count > 1000)
                {
                    _failureHistory[providerType] = _failureHistory[providerType]
                        .Skip(_failureHistory[providerType].Count - 1000)
                        .ToList();
                }

                // Update failure prediction model
                UpdateFailureModel(providerType, failureEvent);
            }
        }

        /// <summary>
        /// Initiates preventive failover for high-risk providers
        /// </summary>
        public async Task<bool> InitiatePreventiveFailoverAsync(List<ProviderType> highRiskProviders)
        {
            var success = true;

            foreach (var provider in highRiskProviders)
            {
                try
                {
                    // Find best alternative provider
                    var alternativeProvider = await FindBestAlternativeProviderAsync(provider);
                    
                    if (alternativeProvider != ProviderType.Default)
                    {
                        // Initiate failover
                        await ExecutePreventiveFailoverAsync(provider, alternativeProvider);
                        
                        // Record the preventive action
                        RecordPreventiveAction(provider, alternativeProvider);
                    }
                }
                catch (Exception ex)
                {
                    OASISErrorHandling.HandleError($"Error initiating preventive failover for {provider}: {ex.Message}", ex);
                    success = false;
                }
            }

            return success;
        }

        /// <summary>
        /// Gets failure prediction history
        /// </summary>
        public Dictionary<ProviderType, List<FailureEvent>> GetFailureHistory()
        {
            lock (_lockObject)
            {
                return new Dictionary<ProviderType, List<FailureEvent>>(_failureHistory);
            }
        }

        /// <summary>
        /// Gets failure prediction model for a provider
        /// </summary>
        public FailurePredictionModel GetFailureModel(ProviderType providerType)
        {
            lock (_lockObject)
            {
                return _failureModels.ContainsKey(providerType) 
                    ? _failureModels[providerType] 
                    : new FailurePredictionModel();
            }
        }

        /// <summary>
        /// Calculates failure probability based on multiple factors
        /// </summary>
        private double CalculateFailureProbability(ProviderPerformanceMetrics metrics, GeographicInfo geoInfo, CostAnalysis costAnalysis)
        {
            var factors = new List<double>();

            // Response time factor (higher response time = higher failure probability)
            var responseTimeFactor = Math.Min(1.0, metrics.ResponseTimeMs / 2000.0);
            factors.Add(responseTimeFactor * 0.3);

            // Error rate factor
            var errorRateFactor = metrics.ErrorRate;
            factors.Add(errorRateFactor * 0.25);

            // Uptime factor (lower uptime = higher failure probability)
            var uptimeFactor = (100 - metrics.UptimePercentage) / 100.0;
            factors.Add(uptimeFactor * 0.2);

            // Geographic factor (higher latency = higher failure probability)
            var geoFactor = geoInfo != null ? Math.Min(1.0, geoInfo.NetworkLatency / 500.0) : 0.1;
            factors.Add(geoFactor * 0.15);

            // Cost factor (unusual cost patterns might indicate issues)
            var costFactor = costAnalysis != null ? Math.Min(1.0, costAnalysis.TotalCost / 1.0) : 0.1;
            factors.Add(costFactor * 0.1);

            return Math.Min(1.0, factors.Sum());
        }

        /// <summary>
        /// Determines risk level based on failure probability
        /// </summary>
        private RiskLevel DetermineRiskLevel(double failureProbability)
        {
            if (failureProbability >= 0.8) return RiskLevel.Critical;
            if (failureProbability >= 0.6) return RiskLevel.High;
            if (failureProbability >= 0.4) return RiskLevel.Medium;
            if (failureProbability >= 0.2) return RiskLevel.Low;
            return RiskLevel.VeryLow;
        }

        /// <summary>
        /// Calculates predicted failure time
        /// </summary>
        private DateTime CalculatePredictedFailureTime(double failureProbability, ProviderPerformanceMetrics metrics)
        {
            var baseTime = DateTime.UtcNow;
            
            if (failureProbability >= 0.8) return baseTime.AddMinutes(30);
            if (failureProbability >= 0.6) return baseTime.AddHours(2);
            if (failureProbability >= 0.4) return baseTime.AddHours(12);
            if (failureProbability >= 0.2) return baseTime.AddDays(1);
            return baseTime.AddDays(7);
        }

        /// <summary>
        /// Calculates prediction confidence
        /// </summary>
        private double CalculatePredictionConfidence(ProviderType providerType)
        {
            lock (_lockObject)
            {
                if (!_failureHistory.ContainsKey(providerType) || _failureHistory[providerType].Count < 5)
                    return 0.1;

                var historyCount = _failureHistory[providerType].Count;
                var recentFailures = _failureHistory[providerType].Count(f => f.Timestamp > DateTime.UtcNow.AddDays(-7));

                // More data and recent failures increase confidence
                var dataConfidence = Math.Min(0.8, historyCount / 100.0);
                var recencyConfidence = Math.Min(0.2, recentFailures / 10.0);

                return dataConfidence + recencyConfidence;
            }
        }

        /// <summary>
        /// Identifies risk factors
        /// </summary>
        private List<string> IdentifyRiskFactors(ProviderPerformanceMetrics metrics, GeographicInfo geoInfo, CostAnalysis costAnalysis)
        {
            var riskFactors = new List<string>();

            if (metrics.ResponseTimeMs > 1000)
                riskFactors.Add("High response time");

            if (metrics.ErrorRate > 0.05)
                riskFactors.Add("High error rate");

            if (metrics.UptimePercentage < 99)
                riskFactors.Add("Low uptime");

            if (geoInfo != null && geoInfo.NetworkLatency > 200)
                riskFactors.Add("High network latency");

            if (costAnalysis != null && costAnalysis.TotalCost > 0.5)
                riskFactors.Add("High operational cost");

            if (metrics.ThroughputMbps < 10)
                riskFactors.Add("Low throughput");

            return riskFactors;
        }

        /// <summary>
        /// Generates provider-specific recommendations
        /// </summary>
        private List<string> GenerateProviderRecommendations(double failureProbability, List<string> riskFactors)
        {
            var recommendations = new List<string>();

            if (failureProbability > 0.8)
            {
                recommendations.Add("Immediate failover recommended");
                recommendations.Add("Prepare backup providers");
                recommendations.Add("Increase monitoring frequency");
            }
            else if (failureProbability > 0.6)
            {
                recommendations.Add("Monitor closely");
                recommendations.Add("Prepare failover procedures");
                recommendations.Add("Review provider configuration");
            }
            else if (failureProbability > 0.4)
            {
                recommendations.Add("Continue monitoring");
                recommendations.Add("Review performance metrics");
            }
            else
            {
                recommendations.Add("Normal operation");
            }

            // Add specific recommendations based on risk factors
            if (riskFactors.Contains("High response time"))
                recommendations.Add("Optimize request patterns");

            if (riskFactors.Contains("High error rate"))
                recommendations.Add("Review error handling");

            if (riskFactors.Contains("Low uptime"))
                recommendations.Add("Check provider health");

            return recommendations;
        }

        /// <summary>
        /// Determines overall risk level
        /// </summary>
        private RiskLevel DetermineOverallRiskLevel(List<ProviderFailurePrediction> predictions)
        {
            if (predictions.Any(p => p.RiskLevel == RiskLevel.Critical))
                return RiskLevel.Critical;

            if (predictions.Any(p => p.RiskLevel == RiskLevel.High))
                return RiskLevel.High;

            if (predictions.Any(p => p.RiskLevel == RiskLevel.Medium))
                return RiskLevel.Medium;

            return RiskLevel.Low;
        }

        /// <summary>
        /// Generates failover recommendations
        /// </summary>
        private List<string> GenerateFailoverRecommendations(List<ProviderFailurePrediction> predictions)
        {
            var recommendations = new List<string>();

            var criticalProviders = predictions.Where(p => p.RiskLevel == RiskLevel.Critical).ToList();
            var highRiskProviders = predictions.Where(p => p.RiskLevel == RiskLevel.High).ToList();

            if (criticalProviders.Any())
            {
                recommendations.Add("Immediate action required for critical providers");
                recommendations.Add("Initiate emergency failover procedures");
            }

            if (highRiskProviders.Any())
            {
                recommendations.Add("Prepare for potential failover");
                recommendations.Add("Review backup provider availability");
            }

            if (predictions.Any(p => p.FailureProbability > 0.5))
            {
                recommendations.Add("Increase monitoring frequency");
                recommendations.Add("Prepare failover resources");
            }

            return recommendations;
        }

        /// <summary>
        /// Finds best alternative provider
        /// </summary>
        private async Task<ProviderType> FindBestAlternativeProviderAsync(ProviderType currentProvider)
        {
            // Use AI optimization engine to find best alternative
            var availableProviders = new List<ProviderType>
            {
                ProviderType.MongoDBOASIS,
                ProviderType.SQLLiteDBOASIS,
                ProviderType.EthereumOASIS,
                ProviderType.IPFSOASIS
            };

            // Remove current provider from alternatives
            availableProviders.Remove(currentProvider);

            var recommendations = await _aiEngine.GetProviderRecommendationsAsync(new StorageOperationRequest(), availableProviders);
            return recommendations.FirstOrDefault()?.ProviderType ?? availableProviders.First();
        }

        /// <summary>
        /// Records a per-provider failover override that OASISHyperDrive.RouteToProviderAsync will apply
        /// on the next incoming request for <paramref name="fromProvider"/>.  Does NOT call
        /// ProviderManager.SetAndActivateCurrentStorageProviderAsync — that mutates global state from this
        /// background loop, racing with concurrent in-flight storage operations on every other goroutine.
        /// </summary>
        private Task ExecutePreventiveFailoverAsync(ProviderType fromProvider, ProviderType toProvider)
        {
            _pendingFailoverOverrides[fromProvider] = toProvider;
            LoggingManager.Log(
                $"Preventive failover queued: next request for {fromProvider} will be redirected to {toProvider}",
                Logging.LogType.Info);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Clear the failover override for a provider once it has recovered (called by
        /// OASISHyperDrive.RouteToProviderAsync after a successful real operation through that provider).
        /// </summary>
        public void ClearFailoverOverride(ProviderType provider)
            => _pendingFailoverOverrides.TryRemove(provider, out _);

        /// <summary>
        /// Records a preventive action in a real, queryable list (see GetPreventiveActions) instead of just
        /// Console.WriteLine-ing it with no way to retrieve it afterward.
        /// </summary>
        private void RecordPreventiveAction(ProviderType fromProvider, ProviderType toProvider)
        {
            var action = new PreventiveAction
            {
                Timestamp = DateTime.UtcNow,
                FromProvider = fromProvider,
                ToProvider = toProvider,
                ActionType = PreventiveActionType.Failover,
                Success = true
            };

            lock (_lockObject)
            {
                _preventiveActions.Add(action);
                if (_preventiveActions.Count > 1000)
                    _preventiveActions.RemoveRange(0, _preventiveActions.Count - 1000);
            }

            LoggingManager.Log($"Recorded preventive action: {fromProvider} -> {toProvider} at {action.Timestamp:O}", Logging.LogType.Info);
        }

        /// <summary>Gets the real history of preventive failover actions taken.</summary>
        public List<PreventiveAction> GetPreventiveActions()
        {
            lock (_lockObject)
            {
                return new List<PreventiveAction>(_preventiveActions);
            }
        }

        /// <summary>
        /// Updates failure prediction model
        /// </summary>
        private void UpdateFailureModel(ProviderType providerType, FailureEvent failureEvent)
        {
            if (!_failureModels.ContainsKey(providerType))
            {
                _failureModels[providerType] = new FailurePredictionModel();
            }

            _failureModels[providerType].UpdateModel(failureEvent);
        }
    }


    /// <summary>
    /// Risk levels
    /// </summary>
    public enum RiskLevel
    {
        VeryLow,
        Low,
        Medium,
        High,
        Critical,
        Unknown
    }

    /// <summary>
    /// Failure types
    /// </summary>
    public enum FailureType
    {
        Timeout,
        ConnectionError,
        AuthenticationError,
        RateLimitExceeded,
        ServiceUnavailable,
        DataCorruption,
        PerformanceDegradation,
        SecurityBreach
    }

    /// <summary>
    /// Preventive action types
    /// </summary>
    public enum PreventiveActionType
    {
        Failover,
        LoadReduction,
        MonitoringIncrease,
        ConfigurationChange,
        ResourceScaling
    }
}
