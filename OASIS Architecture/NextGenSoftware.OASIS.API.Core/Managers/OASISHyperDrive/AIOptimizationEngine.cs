using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Core.Managers
{
    /// <summary>
    /// AI-powered optimization engine for OASIS HyperDrive
    /// </summary>
    public class AIOptimizationEngine
    {
        private static AIOptimizationEngine _instance;
        private readonly PerformanceMonitor _performanceMonitor;
        private readonly Dictionary<ProviderType, List<PerformanceDataPoint>> _historicalData;
        private readonly Dictionary<ProviderType, PredictiveModel> _predictiveModels;
        private readonly object _lockObject = new object();

        public static AIOptimizationEngine Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new AIOptimizationEngine();
                return _instance;
            }
        }

        private AIOptimizationEngine()
        {
            _performanceMonitor = PerformanceMonitor.Instance;
            _historicalData = new Dictionary<ProviderType, List<PerformanceDataPoint>>();
            _predictiveModels = new Dictionary<ProviderType, PredictiveModel>();
        }

        /// <summary>
        /// AI-powered provider selection with predictive analytics
        /// </summary>
        public async Task<ProviderType> SelectOptimalProviderAsync(List<ProviderType> availableProviders, LoadBalancingStrategy strategy = LoadBalancingStrategy.Auto)
        {
            if (!availableProviders.Any())
                return ProviderType.Default;

            var predictions = new Dictionary<ProviderType, double>();

            foreach (var provider in availableProviders)
            {
                var prediction = await PredictProviderPerformanceAsync(provider);
                predictions[provider] = prediction;
            }

            // Select provider with best predicted performance
            return predictions.OrderByDescending(x => x.Value).First().Key;
        }

        /// <summary>
        /// Predicts future performance for a provider
        /// </summary>
        public async Task<double> PredictProviderPerformanceAsync(ProviderType providerType)
        {
            lock (_lockObject)
            {
                if (!_predictiveModels.ContainsKey(providerType))
                {
                    _predictiveModels[providerType] = new PredictiveModel();
                }

                var model = _predictiveModels[providerType];
                var historicalData = GetHistoricalData(providerType);
                
                if (historicalData.Count < 10) // Need minimum data points
                {
                    return 50; // Default neutral score
                }

                // Use machine learning to predict performance
                var prediction = model.PredictPerformance(historicalData);
                return prediction;
            }
        }

        /// <summary>
        /// Records performance data for AI training
        /// </summary>
        public void RecordPerformanceData(ProviderType providerType, PerformanceDataPoint dataPoint)
        {
            lock (_lockObject)
            {
                if (!_historicalData.ContainsKey(providerType))
                {
                    _historicalData[providerType] = new List<PerformanceDataPoint>();
                }

                _historicalData[providerType].Add(dataPoint);

                // Keep only last 1000 data points to prevent memory issues
                if (_historicalData[providerType].Count > 1000)
                {
                    _historicalData[providerType] = _historicalData[providerType]
                        .Skip(_historicalData[providerType].Count - 1000)
                        .ToList();
                }

                // Update predictive model
                if (!_predictiveModels.ContainsKey(providerType))
                {
                    _predictiveModels[providerType] = new PredictiveModel();
                }

                _predictiveModels[providerType].Train(_historicalData[providerType]);
            }
        }

        /// <summary>
        /// Predicts potential failures before they happen
        /// </summary>
        public async Task<FailurePrediction> PredictFailureAsync(ProviderType providerType)
        {
            var historicalData = GetHistoricalData(providerType);
            var currentMetrics = _performanceMonitor.GetMetrics(providerType);

            if (historicalData.Count < 5)
            {
                return new FailurePrediction
                {
                    ProviderType = providerType,
                    FailureProbability = 0.1, // Default low probability
                    PredictedFailureTime = DateTime.UtcNow.AddHours(24),
                    Confidence = 0.1,
                    RecommendedActions = new List<string> { "Monitor closely" }
                };
            }

            // Analyze trends for failure prediction
            var recentData = historicalData.TakeLast(10).ToList();
            var failureProbability = CalculateFailureProbability(recentData, currentMetrics);
            var predictedFailureTime = CalculatePredictedFailureTime(recentData, failureProbability);
            var confidence = CalculateConfidence(recentData);
            var recommendedActions = GenerateRecommendedActions(failureProbability, currentMetrics);

            return new FailurePrediction
            {
                ProviderType = providerType,
                FailureProbability = failureProbability,
                PredictedFailureTime = predictedFailureTime,
                Confidence = confidence,
                RecommendedActions = recommendedActions
            };
        }

        /// <summary>
        /// Gets AI-powered optimization recommendations
        /// </summary>
        public async Task<List<OptimizationRecommendation>> GetOptimizationRecommendationsAsync()
        {
            var recommendations = new List<OptimizationRecommendation>();

            foreach (var provider in _historicalData.Keys)
            {
                var historicalData = GetHistoricalData(provider);
                var currentMetrics = _performanceMonitor.GetMetrics(provider);

                if (historicalData.Count < 5) continue;

                // Analyze performance trends
                var performanceTrend = AnalyzePerformanceTrend(historicalData);
                var costTrend = AnalyzeCostTrend(historicalData);
                var reliabilityTrend = AnalyzeReliabilityTrend(historicalData);

                // Generate recommendations based on trends
                if (performanceTrend < -0.1) // Performance declining
                {
                    recommendations.Add(new OptimizationRecommendation
                    {
                        ProviderType = provider,
                        Type = OptimizationType.Performance,
                        Priority = Priority.High,
                        Description = "Performance is declining. Consider load balancing adjustments.",
                        SuggestedActions = new List<string>
                        {
                            "Reduce weight in load balancing",
                            "Increase monitoring frequency",
                            "Consider failover preparation"
                        }
                    });
                }

                if (costTrend > 0.1) // Cost increasing
                {
                    recommendations.Add(new OptimizationRecommendation
                    {
                        ProviderType = provider,
                        Type = OptimizationType.Cost,
                        Priority = Priority.Medium,
                        Description = "Costs are increasing. Consider cost optimization.",
                        SuggestedActions = new List<string>
                        {
                            "Review cost thresholds",
                            "Consider alternative providers",
                            "Optimize request patterns"
                        }
                    });
                }

                if (reliabilityTrend < -0.05) // Reliability declining
                {
                    recommendations.Add(new OptimizationRecommendation
                    {
                        ProviderType = provider,
                        Type = OptimizationType.Reliability,
                        Priority = Priority.High,
                        Description = "Reliability is declining. Immediate attention required.",
                        SuggestedActions = new List<string>
                        {
                            "Prepare for failover",
                            "Increase health check frequency",
                            "Review provider configuration"
                        }
                    });
                }
            }

            return recommendations.OrderByDescending(r => r.Priority).ToList();
        }

        /// <summary>
        /// Gets historical data for a provider
        /// </summary>
        private List<PerformanceDataPoint> GetHistoricalData(ProviderType providerType)
        {
            lock (_lockObject)
            {
                return _historicalData.ContainsKey(providerType) 
                    ? _historicalData[providerType] 
                    : new List<PerformanceDataPoint>();
            }
        }

        /// <summary>
        /// Calculates failure probability based on historical data
        /// </summary>
        private double CalculateFailureProbability(List<PerformanceDataPoint> recentData, ProviderPerformanceMetrics currentMetrics)
        {
            if (recentData.Count < 3) return 0.1;

            var errorRate = recentData.Average(d => d.ErrorRate);
            var responseTime = recentData.Average(d => d.ResponseTimeMs);
            var uptime = recentData.Average(d => d.UptimePercentage);

            // Simple failure probability calculation
            var failureProbability = (errorRate * 0.4) + 
                                   ((responseTime > 1000 ? 1.0 : responseTime / 1000) * 0.3) + 
                                   ((100 - uptime) / 100 * 0.3);

            return Math.Min(1.0, Math.Max(0.0, failureProbability));
        }

        /// <summary>
        /// Calculates predicted failure time
        /// </summary>
        private DateTime CalculatePredictedFailureTime(List<PerformanceDataPoint> recentData, double failureProbability)
        {
            if (failureProbability < 0.3) return DateTime.UtcNow.AddDays(7);
            if (failureProbability < 0.6) return DateTime.UtcNow.AddHours(24);
            if (failureProbability < 0.8) return DateTime.UtcNow.AddHours(6);
            return DateTime.UtcNow.AddHours(1);
        }

        /// <summary>
        /// Calculates confidence in prediction
        /// </summary>
        private double CalculateConfidence(List<PerformanceDataPoint> recentData)
        {
            if (recentData.Count < 5) return 0.1;
            if (recentData.Count < 10) return 0.3;
            if (recentData.Count < 20) return 0.6;
            return 0.8;
        }

        /// <summary>
        /// Generates recommended actions based on failure probability
        /// </summary>
        private List<string> GenerateRecommendedActions(double failureProbability, ProviderPerformanceMetrics currentMetrics)
        {
            var actions = new List<string>();

            if (failureProbability > 0.8)
            {
                actions.Add("Immediate failover recommended");
                actions.Add("Increase monitoring to every 30 seconds");
                actions.Add("Prepare backup providers");
            }
            else if (failureProbability > 0.6)
            {
                actions.Add("Monitor closely");
                actions.Add("Prepare failover procedures");
                actions.Add("Review provider configuration");
            }
            else if (failureProbability > 0.3)
            {
                actions.Add("Continue monitoring");
                actions.Add("Review performance metrics");
            }
            else
            {
                actions.Add("Normal operation");
            }

            return actions;
        }

        /// <summary>
        /// Analyzes performance trend
        /// </summary>
        private double AnalyzePerformanceTrend(List<PerformanceDataPoint> historicalData)
        {
            if (historicalData.Count < 5) return 0;

            var recent = historicalData.TakeLast(5).Average(d => d.ResponseTimeMs);
            var older = historicalData.SkipLast(5).TakeLast(5).Average(d => d.ResponseTimeMs);
            
            return (older - recent) / older; // Positive means improvement
        }

        /// <summary>
        /// Analyzes cost trend
        /// </summary>
        private double AnalyzeCostTrend(List<PerformanceDataPoint> historicalData)
        {
            if (historicalData.Count < 5) return 0;

            var recent = historicalData.TakeLast(5).Average(d => d.CostPerOperation);
            var older = historicalData.SkipLast(5).TakeLast(5).Average(d => d.CostPerOperation);
            
            return (recent - older) / older; // Positive means cost increase
        }

        /// <summary>
        /// Analyzes reliability trend
        /// </summary>
        private double AnalyzeReliabilityTrend(List<PerformanceDataPoint> historicalData)
        {
            if (historicalData.Count < 5) return 0;

            var recent = historicalData.TakeLast(5).Average(d => d.UptimePercentage);
            var older = historicalData.SkipLast(5).TakeLast(5).Average(d => d.UptimePercentage);
            
            return (recent - older) / older; // Positive means improvement
        }
    }

    /// <summary>
    /// Performance data point for AI training
    /// </summary>
    public class PerformanceDataPoint
    {
        public DateTime Timestamp { get; set; }
        public double ResponseTimeMs { get; set; }
        public double ThroughputMbps { get; set; }
        public double ErrorRate { get; set; }
        public double UptimePercentage { get; set; }
        public double CostPerOperation { get; set; }
        public int ActiveConnections { get; set; }
        public double CpuUsage { get; set; }
        public double MemoryUsage { get; set; }
        public double NetworkLatency { get; set; }
    }

    /// <summary>
    /// Predictive model for AI optimization
    /// </summary>
    public class PredictiveModel
    {
        private List<double> _weights;
        private double _bias;

        public PredictiveModel()
        {
            _weights = new List<double> { 0.3, 0.2, 0.2, 0.1, 0.1, 0.1 }; // Response time, throughput, error rate, uptime, cost, connections
            _bias = 0.5;
        }

        public double PredictPerformance(List<PerformanceDataPoint> historicalData)
        {
            if (historicalData.Count < 5) return 50;

            var recent = historicalData.TakeLast(5).ToList();
            var features = ExtractFeatures(recent);
            
            var prediction = _bias;
            for (int i = 0; i < Math.Min(features.Count, _weights.Count); i++)
            {
                prediction += features[i] * _weights[i];
            }

            return Math.Max(0, Math.Min(100, prediction));
        }

        public void Train(List<PerformanceDataPoint> historicalData)
        {
            if (historicalData.Count < 10) return;

            // Simple linear regression training
            var features = ExtractFeatures(historicalData);
            var targets = historicalData.Select(d => CalculateTarget(d)).ToList();

            // Update weights using gradient descent (simplified)
            var learningRate = 0.01;
            for (int epoch = 0; epoch < 10; epoch++)
            {
                for (int i = 0; i < features.Count; i++)
                {
                    var prediction = PredictPerformance(historicalData);
                    var error = targets[i] - prediction;
                    
                    if (i < _weights.Count)
                    {
                        _weights[i] += learningRate * error * features[i];
                    }
                }
                _bias += learningRate * (targets.Average() - prediction);
            }
        }

        private List<double> ExtractFeatures(List<PerformanceDataPoint> data)
        {
            return new List<double>
            {
                data.Average(d => d.ResponseTimeMs) / 1000, // Normalize
                data.Average(d => d.ThroughputMbps) / 100,   // Normalize
                data.Average(d => d.ErrorRate),
                data.Average(d => d.UptimePercentage) / 100,  // Normalize
                data.Average(d => d.CostPerOperation) * 100, // Normalize
                data.Average(d => d.ActiveConnections) / 100 // Normalize
            };
        }

        private double CalculateTarget(PerformanceDataPoint data)
        {
            // Calculate performance score as target
            var responseScore = Math.Max(0, 100 - (data.ResponseTimeMs / 10));
            var throughputScore = Math.Min(100, data.ThroughputMbps * 10);
            var errorScore = Math.Max(0, 100 - (data.ErrorRate * 100));
            var uptimeScore = data.UptimePercentage;
            var costScore = Math.Max(0, 100 - (data.CostPerOperation * 100));

            return (responseScore * 0.3) + (throughputScore * 0.2) + (errorScore * 0.2) + (uptimeScore * 0.2) + (costScore * 0.1);
        }
    }

    /// <summary>
    /// Failure prediction result
    /// </summary>
    public class FailurePrediction
    {
        public ProviderType ProviderType { get; set; }
        public double FailureProbability { get; set; }
        public DateTime PredictedFailureTime { get; set; }
        public double Confidence { get; set; }
        public List<string> RecommendedActions { get; set; } = new List<string>();
    }

    /// <summary>
    /// Optimization recommendation
    /// </summary>
    public class OptimizationRecommendation
    {
        public ProviderType ProviderType { get; set; }
        public OptimizationType Type { get; set; }
        public Priority Priority { get; set; }
        public string Description { get; set; }
        public List<string> SuggestedActions { get; set; } = new List<string>();
    }

    /// <summary>
    /// Optimization types
    /// </summary>
    public enum OptimizationType
    {
        Performance,
        Cost,
        Reliability,
        Security,
        Geographic
    }

    /// <summary>
    /// Priority levels
    /// </summary>
    public enum Priority
    {
        Low,
        Medium,
        High,
        Critical
    }
}
