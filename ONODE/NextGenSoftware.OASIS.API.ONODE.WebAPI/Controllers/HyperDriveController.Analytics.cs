using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using NextGenSoftware.OASIS.API.Core.Configuration;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Helpers;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers
{    public partial class HyperDriveController
    {
        /// Gets HyperDrive status and health
        /// </summary>
        [HttpGet("status")]
        public ActionResult<OASISResult<HyperDriveStatus>> GetStatus()
        {
            try
            {
                var config = _configManager.GetConfiguration();
                var status = new HyperDriveStatus
                {
                    IsEnabled = config.IsEnabled,
                    AutoFailoverEnabled = config.AutoFailoverEnabled,
                    AutoReplicationEnabled = config.AutoReplicationEnabled,
                    AutoLoadBalancingEnabled = config.AutoLoadBalancingEnabled,
                    DefaultStrategy = Enum.TryParse<LoadBalancingStrategy>(config.DefaultStrategy, out var defaultStrategy) ? defaultStrategy : LoadBalancingStrategy.RoundRobin,
                    EnabledProviders = config.EnabledProviders.Select(p => Enum.TryParse<ProviderType>(p, out var providerType) ? providerType : ProviderType.None).Where(p => p != ProviderType.None).ToList(),
                    LoadBalancingProviders = config.LoadBalancingProviders.Select(p => Enum.TryParse<ProviderType>(p, out var providerType) ? providerType : ProviderType.None).Where(p => p != ProviderType.None).ToList(),
                    TotalProviders = config.EnabledProviders.Count,
                    ActiveProviders = config.EnabledProviders.Count, // Simplified for now
                    LastHealthCheck = DateTime.UtcNow
                };

                return Ok(new OASISResult<HyperDriveStatus>
                {
                    Result = status,

                    Message = "HyperDrive status retrieved successfully."
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<HyperDriveStatus>
                {
                    IsError = true,
                    Message = $"Error retrieving status: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// Gets AI-powered optimization recommendations
        /// </summary>
        [HttpGet("ai/recommendations")]
        public ActionResult<OASISResult<List<OptimizationRecommendation>>> GetAIRecommendations()
        {
            try
            {
                var recommendations = _aiEngine.GetSmartRecommendationsAsync().Result;
                return Ok(new OASISResult<List<OptimizationRecommendation>>
                {
                    Result = recommendations,
                    IsError = false,
                    Message = "AI optimization recommendations retrieved successfully."
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<List<OptimizationRecommendation>>
                {
                    IsError = true,
                    Message = $"Error retrieving AI recommendations: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// Gets predictive analytics
        /// </summary>
        [HttpGet("analytics/predictive/{providerType}")]
        public ActionResult<OASISResult<PredictiveAnalytics>> GetPredictiveAnalytics(ProviderType providerType, [FromQuery] int forecastDays = 7)
        {
            try
            {
                var analytics = _analyticsEngine.GetPredictiveAnalyticsAsync(providerType, forecastDays).Result;
                return Ok(new OASISResult<PredictiveAnalytics>
                {
                    Result = analytics,

                    Message = $"Predictive analytics for {providerType} retrieved successfully."
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<PredictiveAnalytics>
                {
                    IsError = true,
                    Message = $"Error retrieving predictive analytics: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// Gets comprehensive analytics report
        /// </summary>
        [HttpGet("analytics/report")]
        public ActionResult<OASISResult<AnalyticsReport>> GetAnalyticsReport([FromQuery] ProviderType? providerType = null, [FromQuery] TimeRange timeRange = TimeRange.Last24Hours)
        {
            try
            {
                var report = _analyticsEngine.GetAnalyticsReportAsync(providerType, timeRange).Result;
                return Ok(new OASISResult<AnalyticsReport>
                {
                    Result = report,

                    Message = "Analytics report generated successfully."
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<AnalyticsReport>
                {
                    IsError = true,
                    Message = $"Error generating analytics report: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// Gets real-time dashboard data
        /// </summary>
        [HttpGet("dashboard")]
        public ActionResult<OASISResult<DashboardData>> GetDashboardData()
        {
            try
            {
                var dashboard = _analyticsEngine.GetDashboardDataAsync().Result;
                return Ok(new OASISResult<DashboardData>
                {
                    Result = dashboard,

                    Message = "Dashboard data retrieved successfully."
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<DashboardData>
                {
                    IsError = true,
                    Message = $"Error retrieving dashboard data: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// Gets failure predictions
        /// </summary>
        [HttpGet("failover/predictions")]
        public ActionResult<OASISResult<FailoverPrediction>> GetFailurePredictions()
        {
            try
            {
                var predictions = _failoverEngine.PredictAndPreventFailuresAsync().Result;
                return Ok(new OASISResult<FailoverPrediction>
                {
                    Result = predictions,

                    Message = "Failure predictions retrieved successfully."
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<FailoverPrediction>
                {
                    IsError = true,
                    Message = $"Error retrieving failure predictions: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// Records analytics data
        /// </summary>
        [HttpPost("analytics/record")]
        public ActionResult<OASISResult<bool>> RecordAnalyticsData([FromBody] AnalyticsDataPoint dataPoint)
        {
            if (dataPoint == null)
                return BadRequest(new OASISResult<bool> { IsError = true, Message = "The request body is required. Please provide a valid AnalyticsDataPoint (ProviderType and data)." });
            try
            {
                _analyticsEngine.RecordAnalyticsData(dataPoint.ProviderType, dataPoint);
                return Ok(new OASISResult<bool>
                {
                    Result = true,

                    Message = "Analytics data recorded successfully."
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<bool>
                {
                    IsError = true,
                    Message = $"Error recording analytics data: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// Records performance data for AI training
        /// </summary>
        [HttpPost("ai/record-performance")]
        public ActionResult<OASISResult<bool>> RecordPerformanceData([FromBody] PerformanceDataPoint dataPoint)
        {
            if (dataPoint == null)
                return BadRequest(new OASISResult<bool> { IsError = true, Message = "The request body is required. Please provide a valid PerformanceDataPoint (ProviderType and data)." });
            try
            {
                _aiEngine.RecordPerformanceData(dataPoint.ProviderType, dataPoint);
                return Ok(new OASISResult<bool>
                {
                    Result = true,

                    Message = "Performance data recorded successfully."
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<bool>
                {
                    IsError = true,
                    Message = $"Error recording performance data: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// Records failure event
        /// </summary>
        [HttpPost("failover/record-failure")]
        public ActionResult<OASISResult<bool>> RecordFailureEvent([FromBody] FailureEvent failureEvent)
        {
            if (failureEvent == null)
                return BadRequest(new OASISResult<bool> { IsError = true, Message = "The request body is required. Please provide a valid FailureEvent (ProviderType and event details)." });
            try
            {
                _failoverEngine.RecordFailureEvent(failureEvent.ProviderType, failureEvent);
                return Ok(new OASISResult<bool>
                {
                    Result = true,

                    Message = "Failure event recorded successfully."
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<bool>
                {
                    IsError = true,
                    Message = $"Error recording failure event: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// Gets cost optimization recommendations
        /// </summary>
        [HttpGet("analytics/cost-optimization")]
        public ActionResult<OASISResult<List<CostOptimizationRecommendation>>> GetCostOptimizationRecommendations()
        {
            try
            {
                var recommendations = _analyticsEngine.GetCostOptimizationRecommendationsAsync().Result;
                return Ok(new OASISResult<List<CostOptimizationRecommendation>>
                {
                    Result = recommendations,

                    Message = "Cost optimization recommendations retrieved successfully."
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<List<CostOptimizationRecommendation>>
                {
                    IsError = true,
                    Message = $"Error retrieving cost optimization recommendations: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// Gets performance optimization recommendations
        /// </summary>
        [HttpGet("analytics/performance-optimization")]
        public ActionResult<OASISResult<List<PerformanceOptimizationRecommendation>>> GetPerformanceOptimizationRecommendations()
        {
            try
            {
                var recommendations = _analyticsEngine.GetPerformanceOptimizationRecommendationsAsync().Result;
                return Ok(new OASISResult<List<PerformanceOptimizationRecommendation>>
                {
                    Result = recommendations,

                    Message = "Performance optimization recommendations retrieved successfully."
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<List<PerformanceOptimizationRecommendation>>
                {
                    IsError = true,
                    Message = $"Error retrieving performance optimization recommendations: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// Initiates preventive failover
        /// </summary>
        [HttpPost("failover/preventive")]
        public ActionResult<OASISResult<bool>> InitiatePreventiveFailover([FromBody] List<ProviderType> highRiskProviders)
        {
            if (highRiskProviders == null)
                return BadRequest(new OASISResult<bool> { IsError = true, Message = "The request body is required. Please provide a valid JSON array of ProviderType (high-risk providers)." });
            try
            {
                var result = _failoverEngine.InitiatePreventiveFailoverAsync(highRiskProviders).Result;
                return Ok(new OASISResult<bool>
                {
                    Result = result,

                    Message = "Preventive failover initiated successfully."
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<bool>
                {
                    IsError = true,
                    Message = $"Error initiating preventive failover: {ex.Message}"
                });
            }
        }

    // -------------------------
    // Replication Management
    // -------------------------
    }
}