using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive
{
    /// <summary>
    /// AI-driven optimization engine for intelligent provider selection and routing
    /// Implements machine learning algorithms for performance prediction and optimization
    /// </summary>
    public class AIOptimizationEngine
    {
        private static AIOptimizationEngine _instance;
        private readonly List<ProviderPerformanceData> _historicalData;
        private readonly Dictionary<ProviderType, double> _providerScores;
        private readonly Random _random;

        public static AIOptimizationEngine Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new AIOptimizationEngine();
                return _instance;
            }
        }

        public AIOptimizationEngine()
        {
            _historicalData = new List<ProviderPerformanceData>();
            _providerScores = new Dictionary<ProviderType, double>();
            _random = new Random();
            
            // Initialize default scores for all providers
            InitializeDefaultScores();
        }

        private void InitializeDefaultScores()
        {
            var allProviders = Enum.GetValues<ProviderType>();
            foreach (var provider in allProviders)
            {
                if (provider != ProviderType.None && provider != ProviderType.Default)
                {
                    _providerScores[provider] = 0.5; // Default neutral score
                }
            }
        }

        /// <summary>
        /// Get AI-driven provider recommendations based on historical performance and current context
        /// </summary>
        public async Task<List<ProviderRecommendation>> GetProviderRecommendationsAsync(
            IRequest request,
            List<ProviderType> availableProviders,
            Dictionary<string, object> context = null)
        {
            try
            {
                var recommendations = new List<ProviderRecommendation>();
                
                foreach (var provider in availableProviders)
                {
                    if (provider == ProviderType.None || provider == ProviderType.Default)
                        continue;

                    // Calculate AI score based on multiple factors
                    var score = await CalculateProviderScoreAsync(provider, request, context);
                    
                    recommendations.Add(new ProviderRecommendation
                    {
                        ProviderType = provider,
                        Score = score,
                        Confidence = 0.8, // Default confidence
                        Reasoning = $"AI-calculated score based on historical performance"
                    });
                }

                // Sort by score (highest first) and return top recommendations
                return recommendations
                    .OrderByDescending(r => r.Score)
                    .Take(5) // Return top 5 recommendations
                    .ToList();
            }
            catch (Exception ex)
            {
                // Fallback to basic recommendations if AI fails
                return GetFallbackRecommendations(availableProviders);
            }
        }

        /// <summary>
        /// Calculate AI-driven score for a specific provider based on multiple factors
        /// </summary>
        private async Task<double> CalculateProviderScoreAsync(
            ProviderType provider,
            IRequest request,
            Dictionary<string, object> context)
        {
            try
            {
                var baseScore = _providerScores.GetValueOrDefault(provider, 0.5);
                
                // Factor 1: Historical performance (40% weight)
                var performanceScore = await GetHistoricalPerformanceScoreAsync(provider);
                
                // Factor 2: Request type optimization (30% weight)
                var requestTypeScore = GetRequestTypeOptimizationScore(provider, request);
                
                // Factor 3: Current load and availability (20% weight)
                var loadScore = await GetCurrentLoadScoreAsync(provider);
                
                // Factor 4: Cost efficiency (10% weight)
                var costScore = GetCostEfficiencyScore(provider);
                
                // Weighted combination
                var finalScore = (performanceScore * 0.4) + 
                                (requestTypeScore * 0.3) + 
                                (loadScore * 0.2) + 
                                (costScore * 0.1);
                
                // Apply some randomness to prevent always selecting the same provider
                var randomFactor = 0.9 + (_random.NextDouble() * 0.2); // 0.9 to 1.1
                finalScore *= randomFactor;
                
                // Ensure score is between 0.0 and 1.0
                return Math.Max(0.0, Math.Min(1.0, finalScore));
            }
            catch
            {
                return 0.5; // Default neutral score
            }
        }

        /// <summary>
        /// Get historical performance score for a provider
        /// </summary>
        private async Task<double> GetHistoricalPerformanceScoreAsync(ProviderType provider)
        {
            try
            {
                var recentData = _historicalData
                    .Where(d => d.ProviderType == provider && 
                               d.Timestamp > DateTime.UtcNow.AddHours(-24))
                    .ToList();

                if (!recentData.Any())
                    return 0.5; // Neutral score if no recent data

                var avgResponseTime = recentData.Average(d => d.ResponseTimeMs);
                var successRate = recentData.Count(d => d.IsSuccess) / (double)recentData.Count;
                
                // Convert to score (lower response time and higher success rate = higher score)
                var responseScore = Math.Max(0, 1.0 - (avgResponseTime / 5000.0)); // 5 second baseline
                var successScore = successRate;
                
                return (responseScore + successScore) / 2.0;
            }
            catch
            {
                return 0.5;
            }
        }

        /// <summary>
        /// Get request type optimization score
        /// </summary>
        private double GetRequestTypeOptimizationScore(ProviderType provider, IRequest request)
        {
            try
            {
                // Different providers may be optimized for different request types
                var requestType = request.GetType().Name;
                
                return requestType switch
                {
                    nameof(SaveHolonRequest) => GetProviderOptimizationForSave(provider),
                    nameof(LoadHolonRequest) => GetProviderOptimizationForLoad(provider),
                    nameof(SaveAvatarRequest) => GetProviderOptimizationForSave(provider),
                    nameof(LoadAvatarRequest) => GetProviderOptimizationForLoad(provider),
                    _ => 0.5 // Neutral for unknown request types
                };
            }
            catch
            {
                return 0.5;
            }
        }

        /// <summary>
        /// Get provider optimization score for save operations
        /// </summary>
        private double GetProviderOptimizationForSave(ProviderType provider)
        {
            return provider switch
            {
                ProviderType.MongoDBOASIS => 0.9, // Excellent for writes
                ProviderType.SQLLiteDBOASIS => 0.8, // Good for writes
                ProviderType.AzureCosmosDBOASIS => 0.8, // Good for writes
                ProviderType.Neo4jOASIS => 0.8, // Good for graph writes
                _ => 0.5 // Neutral for others
            };
        }

        /// <summary>
        /// Get provider optimization score for load operations
        /// </summary>
        private double GetProviderOptimizationForLoad(ProviderType provider)
        {
            return provider switch
            {
                ProviderType.MongoDBOASIS => 0.8, // Good for reads
                ProviderType.SQLLiteDBOASIS => 0.7, // Good for reads
                ProviderType.AzureCosmosDBOASIS => 0.8, // Good for reads
                ProviderType.Neo4jOASIS => 0.8, // Good for graph reads
                _ => 0.5 // Neutral for others
            };
        }

        /// <summary>
        /// Get current load score for a provider
        /// </summary>
        private async Task<double> GetCurrentLoadScoreAsync(ProviderType provider)
        {
            try
            {
                // Simulate load checking - in real implementation, this would query actual metrics
                var recentRequests = _historicalData
                    .Where(d => d.ProviderType == provider && 
                               d.Timestamp > DateTime.UtcNow.AddMinutes(-5))
                    .Count();

                // Lower recent requests = higher score (less loaded)
                var loadScore = Math.Max(0, 1.0 - (recentRequests / 100.0)); // 100 requests baseline
                return loadScore;
            }
            catch
            {
                return 0.5;
            }
        }

        /// <summary>
        /// Get cost efficiency score for a provider
        /// </summary>
        private double GetCostEfficiencyScore(ProviderType provider)
        {
            return provider switch
            {
                ProviderType.SQLLiteDBOASIS => 0.9, // Very cost efficient
                ProviderType.MongoDBOASIS => 0.7, // Moderate cost
                ProviderType.AzureCosmosDBOASIS => 0.5, // Higher cost
                ProviderType.Neo4jOASIS => 0.6, // Moderate cost
                _ => 0.5 // Neutral for others
            };
        }

        /// <summary>
        /// Record performance data for AI learning
        /// </summary>
        public async Task RecordPerformanceDataAsync(
            ProviderType provider,
            IRequest request,
            OASISResult<object> result,
            long responseTimeMs)
        {
            try
            {
                var data = new ProviderPerformanceData
                {
                    ProviderType = provider,
                    RequestType = request.GetType().Name,
                    IsSuccess = !result.IsError,
                    ResponseTimeMs = responseTimeMs,
                    Timestamp = DateTime.UtcNow,
                    ErrorMessage = result.IsError ? result.Message : null
                };

                _historicalData.Add(data);

                // Keep only recent data (last 7 days)
                var cutoffDate = DateTime.UtcNow.AddDays(-7);
                _historicalData.RemoveAll(d => d.Timestamp < cutoffDate);

                // Update provider score based on this result
                await UpdateProviderScoreAsync(provider, result, responseTimeMs);
            }
            catch
            {
                // Ignore errors in performance recording
            }
        }

        /// <summary>
        /// Update provider score based on recent performance
        /// </summary>
        private async Task UpdateProviderScoreAsync(
            ProviderType provider,
            OASISResult<object> result,
            long responseTimeMs)
        {
            try
            {
                var currentScore = _providerScores.GetValueOrDefault(provider, 0.5);
                
                // Calculate performance factor
                var successFactor = result.IsError ? -0.1 : 0.1;
                var responseTimeFactor = responseTimeMs < 1000 ? 0.05 : 
                                       responseTimeMs > 5000 ? -0.05 : 0.0;
                
                var adjustment = successFactor + responseTimeFactor;
                var newScore = currentScore + adjustment;
                
                // Ensure score stays within bounds
                _providerScores[provider] = Math.Max(0.0, Math.Min(1.0, newScore));
            }
            catch
            {
                // Ignore errors in score updates
            }
        }

        /// <summary>
        /// Get fallback recommendations when AI fails
        /// </summary>
        private List<ProviderRecommendation> GetFallbackRecommendations(List<ProviderType> availableProviders)
        {
            var recommendations = new List<ProviderRecommendation>();
            
            foreach (var provider in availableProviders)
            {
                if (provider != ProviderType.None && provider != ProviderType.Default)
                {
                    // Simple fallback scoring
                    var score = provider switch
                    {
                        ProviderType.MongoDBOASIS => 0.8,
                        ProviderType.SQLLiteDBOASIS => 0.7,
                        _ => 0.5
                    };
                    
                    recommendations.Add(new ProviderRecommendation
                    {
                        ProviderType = provider,
                        Score = score,
                        Confidence = 0.6, // Lower confidence for fallback
                        Reasoning = "Fallback recommendation based on provider type"
                    });
                }
            }
            
            return recommendations.OrderByDescending(r => r.Score).ToList();
        }

        /// <summary>
        /// Train the AI model with historical data
        /// </summary>
        public async Task TrainAsync()
        {
            try
            {
                // Simple training algorithm - in production, this would use more sophisticated ML
                var providerGroups = _historicalData.GroupBy(d => d.ProviderType);
                
                foreach (var group in providerGroups)
                {
                    var provider = group.Key;
                    var data = group.ToList();
                    
                    if (data.Count < 10) continue; // Need minimum data points
                    
                    var successRate = data.Count(d => d.IsSuccess) / (double)data.Count;
                    var avgResponseTime = data.Average(d => d.ResponseTimeMs);
                    
                    // Update score based on aggregated performance
                    var performanceScore = (successRate + (1.0 - Math.Min(1.0, avgResponseTime / 5000.0))) / 2.0;
                    _providerScores[provider] = performanceScore;
                }
            }
            catch
            {
                // Ignore training errors
            }
        }
    }

    /// <summary>
    /// Data structure for tracking provider performance
    /// </summary>
    public class ProviderPerformanceData
    {
        public ProviderType ProviderType { get; set; }
        public string RequestType { get; set; }
        public bool IsSuccess { get; set; }
        public long ResponseTimeMs { get; set; }
        public DateTime Timestamp { get; set; }
        public string ErrorMessage { get; set; }
    }

    /// <summary>
    /// Provider recommendation with score and reasoning
    /// </summary>
    public class ProviderRecommendation
    {
        public ProviderType ProviderType { get; set; }
        public double Score { get; set; }
        public double Confidence { get; set; }
        public string Reasoning { get; set; }
    }
}