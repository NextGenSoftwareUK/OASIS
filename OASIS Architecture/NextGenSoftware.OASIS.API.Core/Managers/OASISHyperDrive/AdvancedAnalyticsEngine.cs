using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive;

namespace NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive
{
    /// <summary>
    /// Priority levels for analytics recommendations
    /// </summary>
    public enum Priority
    {
        Low,
        Medium,
        High,
        Critical
    }
}

namespace NextGenSoftware.OASIS.API.Core.Managers
{
    /// <summary>
    /// Advanced analytics engine for OASIS HyperDrive
    /// </summary>
    public class AdvancedAnalyticsEngine
    {
        private static AdvancedAnalyticsEngine _instance;
        private readonly PerformanceMonitor _performanceMonitor;
        private readonly Dictionary<ProviderType, List<AnalyticsDataPoint>> _analyticsData;
        private readonly object _lockObject = new object();

        public static AdvancedAnalyticsEngine Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new AdvancedAnalyticsEngine();
                return _instance;
            }
        }

        private AdvancedAnalyticsEngine()
        {
            _performanceMonitor = PerformanceMonitor.Instance;
            _analyticsData = new Dictionary<ProviderType, List<AnalyticsDataPoint>>();
        }

        /// <summary>
        /// Records analytics data point
        /// </summary>
        public void RecordAnalyticsData(ProviderType providerType, AnalyticsDataPoint dataPoint)
        {
            lock (_lockObject)
            {
                if (!_analyticsData.ContainsKey(providerType))
                {
                    _analyticsData[providerType] = new List<AnalyticsDataPoint>();
                }

                _analyticsData[providerType].Add(dataPoint);

                // Keep only last 10000 data points to prevent memory issues
                if (_analyticsData[providerType].Count > 10000)
                {
                    _analyticsData[providerType] = _analyticsData[providerType]
                        .Skip(_analyticsData[providerType].Count - 10000)
                        .ToList();
                }
            }
        }

        /// <summary>
        /// Gets current costs for all providers
        /// </summary>
        public async Task<Dictionary<string, decimal>> GetCurrentCostsAsync()
        {
            var costs = new Dictionary<string, decimal>();
            
            foreach (var provider in _analyticsData.Keys)
            {
                var providerCosts = _performanceMonitor.GetProviderCosts(provider);
                costs[provider.ToString()] = providerCosts;
            }
            
            return await Task.FromResult(costs);
        }

        /// <summary>
        /// Gets cost history for all providers
        /// </summary>
        public async Task<Dictionary<string, List<decimal>>> GetCostHistoryAsync(string timeRange = "Last30Days")
        {
            var history = new Dictionary<string, List<decimal>>();
            
            var days = timeRange switch
            {
                "Last7Days" => 7,
                "Last30Days" => 30,
                "Last90Days" => 90,
                _ => 30
            };
            
            foreach (var provider in _analyticsData.Keys)
            {
                var providerHistory = _performanceMonitor.GetProviderCostHistory(provider, days);
                history[provider.ToString()] = providerHistory;
            }
            
            return await Task.FromResult(history);
        }

        /// <summary>
        /// Gets cost projections for all providers
        /// </summary>
        public async Task<Dictionary<string, decimal>> GetCostProjectionsAsync()
        {
            var projections = new Dictionary<string, decimal>();
            
            foreach (var provider in _analyticsData.Keys)
            {
                var growthRate = _performanceMonitor.GetProviderCostGrowthRate(provider);
                var currentCost = _performanceMonitor.GetProviderCosts(provider);
                var projectedCost = currentCost * (1 + growthRate);
                projections[provider.ToString()] = projectedCost;
            }
            
            return await Task.FromResult(projections);
        }

        /// <summary>
        /// Sets cost limits for providers
        /// </summary>
        public void SetCostLimits(Dictionary<string, decimal> limits)
        {
            foreach (var limit in limits)
            {
                if (Enum.TryParse<ProviderType>(limit.Key, out var providerType))
                {
                    _performanceMonitor.SetProviderCostLimit(providerType, limit.Value);
                }
            }
        }

        /// <summary>
        /// Gets security recommendations
        /// </summary>
        public async Task<Dictionary<string, object>> GetSecurityRecommendationsAsync()
        {
            var recommendations = new Dictionary<string, object>
            {
                { "security_recommendations", new List<string>
                    {
                        "Enable multi-factor authentication for all providers",
                        "Implement encryption at rest for sensitive data",
                        "Regular security audits and penetration testing",
                        "Monitor for unusual access patterns",
                        "Keep all provider SDKs and dependencies updated"
                    }
                }
            };
            
            return await Task.FromResult(recommendations);
        }

        /// <summary>
        /// Gets comprehensive analytics report
        /// </summary>
        public async Task<AnalyticsReport> GetAnalyticsReportAsync(ProviderType? providerType = null, TimeRange timeRange = TimeRange.Last24Hours)
        {
            var report = new AnalyticsReport
            {
                GeneratedAt = DateTime.UtcNow,
                TimeRange = timeRange,
                ProviderType = providerType
            };

            var providers = providerType.HasValue ? new[] { providerType.Value } : _analyticsData.Keys.ToArray();

            foreach (var provider in providers)
            {
                var providerData = GetProviderData(provider, timeRange);
                if (providerData.Any())
                {
                    var providerAnalytics = new ProviderAnalytics
                    {
                        ProviderType = provider,
                        TotalRequests = providerData.Count,
                        SuccessfulRequests = providerData.Count(d => d.Success),
                        FailedRequests = providerData.Count(d => !d.Success),
                        AverageResponseTime = providerData.Average(d => d.ResponseTimeMs),
                        MinResponseTime = providerData.Min(d => d.ResponseTimeMs),
                        MaxResponseTime = providerData.Max(d => d.ResponseTimeMs),
                        AverageThroughput = providerData.Average(d => d.ThroughputMbps),
                        TotalCost = providerData.Sum(d => d.Cost),
                        AverageCost = providerData.Average(d => d.Cost),
                        ErrorRate = providerData.Count(d => !d.Success) / (double)providerData.Count,
                        UptimePercentage = CalculateUptimePercentage(providerData),
                        PeakUsageTime = FindPeakUsageTime(providerData),
                        GeographicDistribution = CalculateGeographicDistribution(providerData),
                        CostTrend = CalculateCostTrend(providerData),
                        PerformanceTrend = CalculatePerformanceTrend(providerData),
                        ReliabilityTrend = CalculateReliabilityTrend(providerData),
                        Anomalies = DetectAnomalies(providerData),
                        Recommendations = GenerateRecommendations(providerData)
                    };

                    report.ProviderAnalytics.Add(providerAnalytics);
                }
            }

            // Calculate overall system metrics
            report.SystemMetrics = CalculateSystemMetrics(report.ProviderAnalytics);
            report.TopPerformers = GetTopPerformers(report.ProviderAnalytics);
            report.Underperformers = GetUnderperformers(report.ProviderAnalytics);
            report.CostOptimization = GetCostOptimization(report.ProviderAnalytics);
            report.ReliabilityInsights = GetReliabilityInsights(report.ProviderAnalytics);

            return report;
        }

        /// <summary>
        /// Gets real-time analytics dashboard data
        /// </summary>
        public async Task<DashboardData> GetDashboardDataAsync()
        {
            var dashboard = new DashboardData
            {
                Timestamp = DateTime.UtcNow,
                ActiveProviders = _analyticsData.Keys.Count,
                TotalRequests = _analyticsData.Values.SelectMany(x => x).Count(),
                SystemHealth = CalculateSystemHealth(),
                PerformanceMetrics = GetPerformanceMetrics(),
                CostMetrics = GetCostMetrics(),
                GeographicMetrics = GetGeographicMetrics(),
                Alerts = GetActiveAlerts(),
                Trends = GetTrends()
            };

            return dashboard;
        }

        /// <summary>
        /// Gets predictive analytics
        /// </summary>
        public async Task<PredictiveAnalytics> GetPredictiveAnalyticsAsync(ProviderType providerType, int forecastDays = 7)
        {
            var historicalData = GetProviderData(providerType, TimeRange.Last30Days);
            
            if (historicalData.Count < 10)
            {
                return new PredictiveAnalytics
                {
                    ProviderType = providerType,
                    ForecastDays = forecastDays,
                    Confidence = 0.1,
                    Message = "Insufficient data for accurate predictions"
                };
            }

            var predictions = new PredictiveAnalytics
            {
                ProviderType = providerType,
                ForecastDays = forecastDays,
                Confidence = CalculatePredictionConfidence(historicalData),
                PredictedPerformance = PredictPerformance(historicalData, forecastDays),
                PredictedCost = PredictCost(historicalData, forecastDays),
                PredictedReliability = PredictReliability(historicalData, forecastDays),
                RiskFactors = IdentifyRiskFactors(historicalData),
                Recommendations = GeneratePredictiveRecommendations(historicalData)
            };

            return predictions;
        }

        /// <summary>
        /// Gets cost optimization recommendations
        /// </summary>
        public async Task<List<CostOptimizationRecommendation>> GetCostOptimizationRecommendationsAsync()
        {
            var recommendations = new List<CostOptimizationRecommendation>();

            foreach (var provider in _analyticsData.Keys)
            {
                var data = GetProviderData(provider, TimeRange.Last7Days);
                if (data.Count < 5) continue;

                var avgCost = data.Average(d => d.Cost);
                var costTrend = CalculateCostTrend(data);
                var performance = data.Average(d => d.ResponseTimeMs);

                if (costTrend > 0.1) // Cost increasing
                {
                    recommendations.Add(new CostOptimizationRecommendation
                    {
                        ProviderType = provider,
                        CurrentCost = avgCost,
                        CostTrend = costTrend,
                        PotentialSavings = CalculatePotentialSavings(data),
                        RecommendedActions = new List<string>
                        {
                            "Consider load balancing to cheaper providers",
                            "Optimize request patterns",
                            "Review cost thresholds",
                            "Consider alternative providers"
                        },
                        Priority = costTrend > 0.2 ? Priority.High : Priority.Medium
                    });
                }
            }

            return recommendations.OrderByDescending(r => r.Priority).ToList();
        }

        /// <summary>
        /// Gets performance optimization recommendations
        /// </summary>
        public async Task<List<PerformanceOptimizationRecommendation>> GetPerformanceOptimizationRecommendationsAsync()
        {
            var recommendations = new List<PerformanceOptimizationRecommendation>();

            foreach (var provider in _analyticsData.Keys)
            {
                var data = GetProviderData(provider, TimeRange.Last7Days);
                if (data.Count < 5) continue;

                var avgResponseTime = data.Average(d => d.ResponseTimeMs);
                var performanceTrend = CalculatePerformanceTrend(data);
                var errorRate = data.Count(d => !d.Success) / (double)data.Count;

                if (performanceTrend < -0.1 || avgResponseTime > 1000 || errorRate > 0.05)
                {
                    recommendations.Add(new PerformanceOptimizationRecommendation
                    {
                        ProviderType = provider,
                        CurrentPerformance = avgResponseTime,
                        PerformanceTrend = performanceTrend,
                        ErrorRate = errorRate,
                        RecommendedActions = new List<string>
                        {
                            "Optimize request patterns",
                            "Increase connection pooling",
                            "Review timeout settings",
                            "Consider failover preparation"
                        },
                        Priority = errorRate > 0.1 ? Priority.Critical : Priority.High
                    });
                }
            }

            return recommendations.OrderByDescending(r => r.Priority).ToList();
        }

        /// <summary>
        /// Gets provider data for specified time range
        /// </summary>
        private List<AnalyticsDataPoint> GetProviderData(ProviderType providerType, TimeRange timeRange)
        {
            lock (_lockObject)
            {
                if (!_analyticsData.ContainsKey(providerType))
                    return new List<AnalyticsDataPoint>();

                var cutoffTime = GetCutoffTime(timeRange);
                return _analyticsData[providerType]
                    .Where(d => d.Timestamp >= cutoffTime)
                    .ToList();
            }
        }

        /// <summary>
        /// Gets cutoff time for time range
        /// </summary>
        private DateTime GetCutoffTime(TimeRange timeRange)
        {
            return timeRange switch
            {
                TimeRange.LastHour => DateTime.UtcNow.AddHours(-1),
                TimeRange.Last24Hours => DateTime.UtcNow.AddDays(-1),
                TimeRange.Last7Days => DateTime.UtcNow.AddDays(-7),
                TimeRange.Last30Days => DateTime.UtcNow.AddDays(-30),
                _ => DateTime.UtcNow.AddDays(-1)
            };
        }

        /// <summary>
        /// Calculates uptime percentage
        /// </summary>
        private double CalculateUptimePercentage(List<AnalyticsDataPoint> data)
        {
            if (!data.Any()) return 0;
            
            var totalTime = (data.Max(d => d.Timestamp) - data.Min(d => d.Timestamp)).TotalMinutes;
            var downtime = data.Count(d => !d.Success) * 1.0; // Assume 1 minute per failure
            return Math.Max(0, 100 - (downtime / totalTime * 100));
        }

        /// <summary>
        /// Finds peak usage time
        /// </summary>
        private DateTime FindPeakUsageTime(List<AnalyticsDataPoint> data)
        {
            if (!data.Any()) return DateTime.UtcNow;

            var hourlyGroups = data
                .GroupBy(d => d.Timestamp.Hour)
                .OrderByDescending(g => g.Count())
                .FirstOrDefault();

            return hourlyGroups?.First().Timestamp ?? DateTime.UtcNow;
        }

        /// <summary>
        /// Calculates geographic distribution
        /// </summary>
        private Dictionary<string, int> CalculateGeographicDistribution(List<AnalyticsDataPoint> data)
        {
            return data
                .Where(d => !string.IsNullOrEmpty(d.Region))
                .GroupBy(d => d.Region)
                .ToDictionary(g => g.Key, g => g.Count());
        }

        /// <summary>
        /// Calculates cost trend
        /// </summary>
        private double CalculateCostTrend(List<AnalyticsDataPoint> data)
        {
            if (data.Count < 5) return 0;

            var recent = data.TakeLast(5).Average(d => d.Cost);
            var older = data.SkipLast(5).TakeLast(5).Average(d => d.Cost);
            
            return (recent - older) / older;
        }

        /// <summary>
        /// Calculates performance trend
        /// </summary>
        private double CalculatePerformanceTrend(List<AnalyticsDataPoint> data)
        {
            if (data.Count < 5) return 0;

            var recent = data.TakeLast(5).Average(d => d.ResponseTimeMs);
            var older = data.SkipLast(5).TakeLast(5).Average(d => d.ResponseTimeMs);
            
            return (older - recent) / older; // Positive means improvement
        }

        /// <summary>
        /// Calculates reliability trend
        /// </summary>
        private double CalculateReliabilityTrend(List<AnalyticsDataPoint> data)
        {
            if (data.Count < 5) return 0;

            var recent = data.TakeLast(5).Count(d => d.Success) / (double)data.TakeLast(5).Count();
            var older = data.SkipLast(5).TakeLast(5).Count(d => d.Success) / (double)data.SkipLast(5).TakeLast(5).Count();
            
            return (recent - older) / older;
        }

        /// <summary>
        /// Detects anomalies in data
        /// </summary>
        private List<Anomaly> DetectAnomalies(List<AnalyticsDataPoint> data)
        {
            var anomalies = new List<Anomaly>();
            
            if (data.Count < 10) return anomalies;

            var avgResponseTime = data.Average(d => d.ResponseTimeMs);
            var stdDev = Math.Sqrt(data.Average(d => Math.Pow(d.ResponseTimeMs - avgResponseTime, 2)));

            foreach (var point in data)
            {
                if (Math.Abs(point.ResponseTimeMs - avgResponseTime) > 2 * stdDev)
                {
                    anomalies.Add(new Anomaly
                    {
                        Timestamp = point.Timestamp,
                        Type = AnomalyType.Performance,
                        Severity = point.ResponseTimeMs > avgResponseTime + 2 * stdDev ? Severity.High : Severity.Medium,
                        Description = $"Response time anomaly: {point.ResponseTimeMs}ms (avg: {avgResponseTime:F1}ms)"
                    });
                }
            }

            return anomalies;
        }

        /// <summary>
        /// Generates recommendations based on data
        /// </summary>
        private List<string> GenerateRecommendations(List<AnalyticsDataPoint> data)
        {
            var recommendations = new List<string>();

            var avgResponseTime = data.Average(d => d.ResponseTimeMs);
            var errorRate = data.Count(d => !d.Success) / (double)data.Count;
            var avgCost = data.Average(d => d.Cost);

            if (avgResponseTime > 1000)
                recommendations.Add("Consider performance optimization or provider switch");

            if (errorRate > 0.05)
                recommendations.Add("High error rate detected - review provider health");

            if (avgCost > 0.1)
                recommendations.Add("High cost detected - consider cost optimization");

            return recommendations;
        }

        /// <summary>
        /// Calculates system health
        /// </summary>
        private double CalculateSystemHealth()
        {
            var allData = _analyticsData.Values.SelectMany(x => x).ToList();
            if (!allData.Any()) return 100;

            var successRate = allData.Count(d => d.Success) / (double)allData.Count;
            var avgResponseTime = allData.Average(d => d.ResponseTimeMs);
            var responseTimeScore = Math.Max(0, 100 - (avgResponseTime / 10));

            return (successRate * 0.6 + (responseTimeScore / 100) * 0.4) * 100;
        }

        /// <summary>
        /// Gets performance metrics
        /// </summary>
        private PerformanceMetrics GetPerformanceMetrics()
        {
            var allData = _analyticsData.Values.SelectMany(x => x).ToList();
            return new PerformanceMetrics
            {
                TotalRequests = allData.Count,
                SuccessfulRequests = allData.Count(d => d.Success),
                AverageResponseTime = allData.Average(d => d.ResponseTimeMs),
                Throughput = allData.Sum(d => d.ThroughputMbps)
            };
        }

        /// <summary>
        /// Gets cost metrics
        /// </summary>
        private CostMetrics GetCostMetrics()
        {
            var allData = _analyticsData.Values.SelectMany(x => x).ToList();
            return new CostMetrics
            {
                TotalCost = allData.Sum(d => d.Cost),
                AverageCost = allData.Average(d => d.Cost),
                CostPerRequest = allData.Sum(d => d.Cost) / allData.Count
            };
        }

        /// <summary>
        /// Gets geographic metrics
        /// </summary>
        private GeographicMetrics GetGeographicMetrics()
        {
            var allData = _analyticsData.Values.SelectMany(x => x).ToList();
            return new GeographicMetrics
            {
                TotalRegions = allData.Select(d => d.Region).Distinct().Count(),
                TopRegion = allData.GroupBy(d => d.Region).OrderByDescending(g => g.Count()).FirstOrDefault()?.Key ?? "Unknown",
                GeographicDistribution = allData.GroupBy(d => d.Region).ToDictionary(g => g.Key, g => g.Count())
            };
        }

        /// <summary>
        /// Gets active alerts
        /// </summary>
        private List<Alert> GetActiveAlerts()
        {
            var alerts = new List<Alert>();

            foreach (var provider in _analyticsData.Keys)
            {
                var data = GetProviderData(provider, TimeRange.LastHour);
                if (data.Count < 5) continue;

                var errorRate = data.Count(d => !d.Success) / (double)data.Count;
                var avgResponseTime = data.Average(d => d.ResponseTimeMs);

                if (errorRate > 0.1)
                {
                    alerts.Add(new Alert
                    {
                        ProviderType = provider,
                        Type = AlertType.HighErrorRate,
                        Severity = Severity.Critical,
                        Message = $"High error rate detected: {errorRate:P1}",
                        Timestamp = DateTime.UtcNow
                    });
                }

                if (avgResponseTime > 2000)
                {
                    alerts.Add(new Alert
                    {
                        ProviderType = provider,
                        Type = AlertType.SlowResponse,
                        Severity = Severity.High,
                        Message = $"Slow response time: {avgResponseTime:F1}ms",
                        Timestamp = DateTime.UtcNow
                    });
                }
            }

            return alerts;
        }

        /// <summary>
        /// Gets trends
        /// </summary>
        private List<Trend> GetTrends()
        {
            var trends = new List<Trend>();

            foreach (var provider in _analyticsData.Keys)
            {
                var data = GetProviderData(provider, TimeRange.Last7Days);
                if (data.Count < 10) continue;

                var performanceTrend = CalculatePerformanceTrend(data);
                var costTrend = CalculateCostTrend(data);
                var reliabilityTrend = CalculateReliabilityTrend(data);

                trends.Add(new Trend
                {
                    ProviderType = provider,
                    PerformanceTrend = performanceTrend,
                    CostTrend = costTrend,
                    ReliabilityTrend = reliabilityTrend
                });
            }

            return trends;
        }

        // Additional helper methods for predictions and calculations...
        private double CalculatePredictionConfidence(List<AnalyticsDataPoint> data) => Math.Min(0.9, data.Count / 100.0);
        private double PredictPerformance(List<AnalyticsDataPoint> data, int days) => data.Average(d => d.ResponseTimeMs);
        private double PredictCost(List<AnalyticsDataPoint> data, int days) => data.Average(d => d.Cost);
        private double PredictReliability(List<AnalyticsDataPoint> data, int days) => data.Count(d => d.Success) / (double)data.Count;
        private List<string> IdentifyRiskFactors(List<AnalyticsDataPoint> data) => new List<string> { "Insufficient data for risk analysis" };
        private List<string> GeneratePredictiveRecommendations(List<AnalyticsDataPoint> data) => new List<string> { "Continue monitoring" };
        private double CalculatePotentialSavings(List<AnalyticsDataPoint> data) => data.Average(d => d.Cost) * 0.2;
        private SystemMetrics CalculateSystemMetrics(List<ProviderAnalytics> analytics) => new SystemMetrics();
        private List<ProviderAnalytics> GetTopPerformers(List<ProviderAnalytics> analytics) => analytics.OrderByDescending(a => a.UptimePercentage).Take(3).ToList();
        private List<ProviderAnalytics> GetUnderperformers(List<ProviderAnalytics> analytics) => analytics.OrderBy(a => a.UptimePercentage).Take(3).ToList();
        private List<CostOptimization> GetCostOptimization(List<ProviderAnalytics> analytics) => new List<CostOptimization>();
        private List<ReliabilityInsight> GetReliabilityInsights(List<ProviderAnalytics> analytics) => new List<ReliabilityInsight>();
    }

    // Data models for analytics
    public class AnalyticsDataPoint
    {
        public DateTime Timestamp { get; set; }
        public ProviderType ProviderType { get; set; }
        public bool Success { get; set; }
        public double ResponseTimeMs { get; set; }
        public double ThroughputMbps { get; set; }
        public double Cost { get; set; }
        public string Region { get; set; }
        public string UserId { get; set; }
        public string Operation { get; set; }
    }

    public class AnalyticsReport
    {
        public DateTime GeneratedAt { get; set; }
        public TimeRange TimeRange { get; set; }
        public ProviderType? ProviderType { get; set; }
        public List<ProviderAnalytics> ProviderAnalytics { get; set; } = new List<ProviderAnalytics>();
        public SystemMetrics SystemMetrics { get; set; }
        public List<ProviderAnalytics> TopPerformers { get; set; } = new List<ProviderAnalytics>();
        public List<ProviderAnalytics> Underperformers { get; set; } = new List<ProviderAnalytics>();
        public List<CostOptimization> CostOptimization { get; set; } = new List<CostOptimization>();
        public List<ReliabilityInsight> ReliabilityInsights { get; set; } = new List<ReliabilityInsight>();
    }

    public class ProviderAnalytics
    {
        public ProviderType ProviderType { get; set; }
        public int TotalRequests { get; set; }
        public int SuccessfulRequests { get; set; }
        public int FailedRequests { get; set; }
        public double AverageResponseTime { get; set; }
        public double MinResponseTime { get; set; }
        public double MaxResponseTime { get; set; }
        public double AverageThroughput { get; set; }
        public double TotalCost { get; set; }
        public double AverageCost { get; set; }
        public double ErrorRate { get; set; }
        public double UptimePercentage { get; set; }
        public DateTime PeakUsageTime { get; set; }
        public Dictionary<string, int> GeographicDistribution { get; set; } = new Dictionary<string, int>();
        public double CostTrend { get; set; }
        public double PerformanceTrend { get; set; }
        public double ReliabilityTrend { get; set; }
        public List<Anomaly> Anomalies { get; set; } = new List<Anomaly>();
        public List<string> Recommendations { get; set; } = new List<string>();
    }

    public class DashboardData
    {
        public DateTime Timestamp { get; set; }
        public int ActiveProviders { get; set; }
        public int TotalRequests { get; set; }
        public double SystemHealth { get; set; }
        public PerformanceMetrics PerformanceMetrics { get; set; }
        public CostMetrics CostMetrics { get; set; }
        public GeographicMetrics GeographicMetrics { get; set; }
        public List<Alert> Alerts { get; set; } = new List<Alert>();
        public List<Trend> Trends { get; set; } = new List<Trend>();
    }

    public class PredictiveAnalytics
    {
        public ProviderType ProviderType { get; set; }
        public int ForecastDays { get; set; }
        public double Confidence { get; set; }
        public string Message { get; set; }
        public double PredictedPerformance { get; set; }
        public double PredictedCost { get; set; }
        public double PredictedReliability { get; set; }
        public List<string> RiskFactors { get; set; } = new List<string>();
        public List<string> Recommendations { get; set; } = new List<string>();
    }

    public class CostOptimizationRecommendation
    {
        public ProviderType ProviderType { get; set; }
        public double CurrentCost { get; set; }
        public double CostTrend { get; set; }
        public double PotentialSavings { get; set; }
        public List<string> RecommendedActions { get; set; } = new List<string>();
        public Priority Priority { get; set; }
    }

    public class PerformanceOptimizationRecommendation
    {
        public ProviderType ProviderType { get; set; }
        public double CurrentPerformance { get; set; }
        public double PerformanceTrend { get; set; }
        public double ErrorRate { get; set; }
        public List<string> RecommendedActions { get; set; } = new List<string>();
        public Priority Priority { get; set; }
    }

    public class Anomaly
    {
        public DateTime Timestamp { get; set; }
        public AnomalyType Type { get; set; }
        public Severity Severity { get; set; }
        public string Description { get; set; }
    }

    public class Alert
    {
        public ProviderType ProviderType { get; set; }
        public AlertType Type { get; set; }
        public Severity Severity { get; set; }
        public string Message { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class Trend
    {
        public ProviderType ProviderType { get; set; }
        public double PerformanceTrend { get; set; }
        public double CostTrend { get; set; }
        public double ReliabilityTrend { get; set; }
    }

    public class PerformanceMetrics
    {
        public int TotalRequests { get; set; }
        public int SuccessfulRequests { get; set; }
        public double AverageResponseTime { get; set; }
        public double Throughput { get; set; }
    }

    public class CostMetrics
    {
        public double TotalCost { get; set; }
        public double AverageCost { get; set; }
        public double CostPerRequest { get; set; }
    }

    public class GeographicMetrics
    {
        public int TotalRegions { get; set; }
        public string TopRegion { get; set; }
        public Dictionary<string, int> GeographicDistribution { get; set; } = new Dictionary<string, int>();
    }

    public class SystemMetrics
    {
        public double OverallHealth { get; set; }
        public double AveragePerformance { get; set; }
        public double TotalCost { get; set; }
        public int TotalProviders { get; set; }
    }

    public class CostOptimization
    {
        public string Description { get; set; }
        public double PotentialSavings { get; set; }
        public List<string> Actions { get; set; } = new List<string>();
    }

    public class ReliabilityInsight
    {
        public string Description { get; set; }
        public double ReliabilityScore { get; set; }
        public List<string> Recommendations { get; set; } = new List<string>();
    }

    public enum TimeRange
    {
        LastHour,
        Last24Hours,
        Last7Days,
        Last30Days
    }

    public enum AnomalyType
    {
        Performance,
        Cost,
        Reliability,
        Security
    }

    public enum AlertType
    {
        HighErrorRate,
        SlowResponse,
        HighCost,
        LowUptime,
        SecurityBreach
    }

    public enum Severity
    {
        Low,
        Medium,
        High,
        Critical
    }
}
