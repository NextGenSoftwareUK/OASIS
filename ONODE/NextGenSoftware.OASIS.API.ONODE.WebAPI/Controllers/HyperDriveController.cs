using Microsoft.AspNetCore.Mvc;
using NextGenSoftware.OASIS.API.Core.Configuration;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HyperDriveController : ControllerBase
    {
        private readonly OASISHyperDriveConfigManager _configManager;
        private readonly ProviderManager _providerManager;
        private readonly PerformanceMonitor _performanceMonitor;
        private readonly AIOptimizationEngine _aiEngine;
        private readonly AdvancedAnalyticsEngine _analyticsEngine;
        private readonly PredictiveFailoverEngine _failoverEngine;

        public HyperDriveController()
        {
            _configManager = OASISHyperDriveConfigManager.Instance;
            _providerManager = ProviderManager.Instance;
            _performanceMonitor = PerformanceMonitor.Instance;
            _aiEngine = AIOptimizationEngine.Instance;
            _analyticsEngine = AdvancedAnalyticsEngine.Instance;
            _failoverEngine = PredictiveFailoverEngine.Instance;
        }

        /// <summary>
        /// Gets the current HyperDrive configuration
        /// </summary>
        [HttpGet("config")]
        public ActionResult<OASISResult<OASISHyperDriveConfig>> GetConfiguration()
        {
            try
            {
                var config = _configManager.GetConfiguration();
                return Ok(new OASISResult<OASISHyperDriveConfig>
                {
                    Result = config,
                    IsSuccess = true,
                    Message = "HyperDrive configuration retrieved successfully."
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<OASISHyperDriveConfig>
                {
                    IsError = true,
                    Message = $"Error retrieving HyperDrive configuration: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// Updates the HyperDrive configuration
        /// </summary>
        [HttpPut("config")]
        public ActionResult<OASISResult<bool>> UpdateConfiguration([FromBody] OASISHyperDriveConfig config)
        {
            try
            {
                var result = _configManager.UpdateConfiguration(config);
                if (result.IsSuccess)
                {
                    return Ok(result);
                }
                else
                {
                    return BadRequest(result);
                }
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<bool>
                {
                    IsError = true,
                    Message = $"Error updating HyperDrive configuration: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// Validates the current configuration
        /// </summary>
        [HttpPost("config/validate")]
        public ActionResult<OASISResult<bool>> ValidateConfiguration()
        {
            try
            {
                var result = _configManager.ValidateConfiguration();
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<bool>
                {
                    IsError = true,
                    Message = $"Error validating configuration: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// Resets configuration to defaults
        /// </summary>
        [HttpPost("config/reset")]
        public ActionResult<OASISResult<bool>> ResetConfiguration()
        {
            try
            {
                var result = _configManager.ResetToDefaults();
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<bool>
                {
                    IsError = true,
                    Message = $"Error resetting configuration: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// Gets performance metrics for all providers
        /// </summary>
        [HttpGet("metrics")]
        public ActionResult<OASISResult<Dictionary<ProviderType, ProviderPerformanceMetrics>>> GetMetrics()
        {
            try
            {
                var metrics = _performanceMonitor.GetAllMetrics();
                return Ok(new OASISResult<Dictionary<ProviderType, ProviderPerformanceMetrics>>
                {
                    Result = metrics,
                    IsSuccess = true,
                    Message = "Performance metrics retrieved successfully."
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<Dictionary<ProviderType, ProviderPerformanceMetrics>>
                {
                    IsError = true,
                    Message = $"Error retrieving metrics: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// Gets performance metrics for a specific provider
        /// </summary>
        [HttpGet("metrics/{providerType}")]
        public ActionResult<OASISResult<ProviderPerformanceMetrics>> GetProviderMetrics(ProviderType providerType)
        {
            try
            {
                var metrics = _performanceMonitor.GetMetrics(providerType);
                if (metrics != null)
                {
                    return Ok(new OASISResult<ProviderPerformanceMetrics>
                    {
                        Result = metrics,
                        IsSuccess = true,
                        Message = $"Performance metrics for {providerType} retrieved successfully."
                    });
                }
                else
                {
                    return NotFound(new OASISResult<ProviderPerformanceMetrics>
                    {
                        IsError = true,
                        Message = $"No metrics found for provider {providerType}"
                    });
                }
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<ProviderPerformanceMetrics>
                {
                    IsError = true,
                    Message = $"Error retrieving metrics for {providerType}: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// Gets active connection counts for all providers
        /// </summary>
        [HttpGet("connections")]
        public ActionResult<OASISResult<Dictionary<ProviderType, int>>> GetConnectionCounts()
        {
            try
            {
                var connections = new Dictionary<ProviderType, int>();
                var config = _configManager.GetConfiguration();
                
                foreach (var provider in config.LoadBalancingProviders)
                {
                    connections[provider] = _performanceMonitor.GetActiveConnections(provider);
                }

                return Ok(new OASISResult<Dictionary<ProviderType, int>>
                {
                    Result = connections,
                    IsSuccess = true,
                    Message = "Connection counts retrieved successfully."
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<Dictionary<ProviderType, int>>
                {
                    IsError = true,
                    Message = $"Error retrieving connection counts: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// Gets the best provider based on current strategy
        /// </summary>
        [HttpGet("best-provider")]
        public ActionResult<OASISResult<ProviderType>> GetBestProvider([FromQuery] LoadBalancingStrategy? strategy = null)
        {
            try
            {
                var config = _configManager.GetConfiguration();
                var availableProviders = config.LoadBalancingProviders;
                var selectedStrategy = strategy ?? config.DefaultStrategy;

                var bestProvider = _performanceMonitor.GetBestProvider(availableProviders, selectedStrategy);
                
                return Ok(new OASISResult<ProviderType>
                {
                    Result = bestProvider,
                    IsSuccess = true,
                    Message = $"Best provider selected using {selectedStrategy} strategy."
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<ProviderType>
                {
                    IsError = true,
                    Message = $"Error selecting best provider: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// Records a request for performance tracking
        /// </summary>
        [HttpPost("record-request")]
        public ActionResult<OASISResult<bool>> RecordRequest([FromBody] RecordRequestModel request)
        {
            try
            {
                _performanceMonitor.RecordRequest(
                    request.ProviderType, 
                    request.Success, 
                    request.ResponseTimeMs, 
                    request.Cost
                );

                return Ok(new OASISResult<bool>
                {
                    Result = true,
                    IsSuccess = true,
                    Message = "Request recorded successfully."
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<bool>
                {
                    IsError = true,
                    Message = $"Error recording request: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// Records connection activity
        /// </summary>
        [HttpPost("record-connection")]
        public ActionResult<OASISResult<bool>> RecordConnection([FromBody] RecordConnectionModel request)
        {
            try
            {
                _performanceMonitor.RecordConnection(request.ProviderType, request.IsConnecting);

                return Ok(new OASISResult<bool>
                {
                    Result = true,
                    IsSuccess = true,
                    Message = "Connection activity recorded successfully."
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<bool>
                {
                    IsError = true,
                    Message = $"Error recording connection: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// Updates geographic information for a provider
        /// </summary>
        [HttpPut("geographic/{providerType}")]
        public ActionResult<OASISResult<bool>> UpdateGeographicInfo(ProviderType providerType, [FromBody] GeographicInfo geoInfo)
        {
            try
            {
                _performanceMonitor.UpdateGeographicInfo(providerType, geoInfo);

                return Ok(new OASISResult<bool>
                {
                    Result = true,
                    IsSuccess = true,
                    Message = $"Geographic information for {providerType} updated successfully."
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<bool>
                {
                    IsError = true,
                    Message = $"Error updating geographic info: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// Updates cost analysis for a provider
        /// </summary>
        [HttpPut("cost/{providerType}")]
        public ActionResult<OASISResult<bool>> UpdateCostAnalysis(ProviderType providerType, [FromBody] CostAnalysis costAnalysis)
        {
            try
            {
                _performanceMonitor.UpdateCostAnalysis(providerType, costAnalysis);

                return Ok(new OASISResult<bool>
                {
                    Result = true,
                    IsSuccess = true,
                    Message = $"Cost analysis for {providerType} updated successfully."
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<bool>
                {
                    IsError = true,
                    Message = $"Error updating cost analysis: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// Resets metrics for a specific provider
        /// </summary>
        [HttpPost("metrics/{providerType}/reset")]
        public ActionResult<OASISResult<bool>> ResetProviderMetrics(ProviderType providerType)
        {
            try
            {
                _performanceMonitor.ResetMetrics(providerType);

                return Ok(new OASISResult<bool>
                {
                    Result = true,
                    IsSuccess = true,
                    Message = $"Metrics for {providerType} reset successfully."
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<bool>
                {
                    IsError = true,
                    Message = $"Error resetting metrics: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// Resets all metrics
        /// </summary>
        [HttpPost("metrics/reset-all")]
        public ActionResult<OASISResult<bool>> ResetAllMetrics()
        {
            try
            {
                _performanceMonitor.ResetAllMetrics();

                return Ok(new OASISResult<bool>
                {
                    Result = true,
                    IsSuccess = true,
                    Message = "All metrics reset successfully."
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<bool>
                {
                    IsError = true,
                    Message = $"Error resetting all metrics: {ex.Message}"
                });
            }
        }

        /// <summary>
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
                    DefaultStrategy = config.DefaultStrategy,
                    EnabledProviders = config.EnabledProviders,
                    LoadBalancingProviders = config.LoadBalancingProviders,
                    TotalProviders = config.EnabledProviders.Count,
                    ActiveProviders = config.EnabledProviders.Count, // Simplified for now
                    LastHealthCheck = DateTime.UtcNow
                };

                return Ok(new OASISResult<HyperDriveStatus>
                {
                    Result = status,
                    IsSuccess = true,
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
                var recommendations = _aiEngine.GetOptimizationRecommendationsAsync().Result;
                return Ok(new OASISResult<List<OptimizationRecommendation>>
                {
                    Result = recommendations,
                    IsSuccess = true,
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
                    IsSuccess = true,
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
                    IsSuccess = true,
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
                    IsSuccess = true,
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
                    IsSuccess = true,
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
            try
            {
                _analyticsEngine.RecordAnalyticsData(dataPoint.ProviderType, dataPoint);
                return Ok(new OASISResult<bool>
                {
                    Result = true,
                    IsSuccess = true,
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
            try
            {
                _aiEngine.RecordPerformanceData(dataPoint.ProviderType, dataPoint);
                return Ok(new OASISResult<bool>
                {
                    Result = true,
                    IsSuccess = true,
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
            try
            {
                _failoverEngine.RecordFailureEvent(failureEvent.ProviderType, failureEvent);
                return Ok(new OASISResult<bool>
                {
                    Result = true,
                    IsSuccess = true,
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
                    IsSuccess = true,
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
                    IsSuccess = true,
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
            try
            {
                var result = _failoverEngine.InitiatePreventiveFailoverAsync(highRiskProviders).Result;
                return Ok(new OASISResult<bool>
                {
                    Result = result,
                    IsSuccess = true,
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
    }

    /// <summary>
    /// Model for recording requests
    /// </summary>
    public class RecordRequestModel
    {
        public ProviderType ProviderType { get; set; }
        public bool Success { get; set; }
        public double ResponseTimeMs { get; set; }
        public double Cost { get; set; }
    }

    /// <summary>
    /// Model for recording connections
    /// </summary>
    public class RecordConnectionModel
    {
        public ProviderType ProviderType { get; set; }
        public bool IsConnecting { get; set; }
    }

    /// <summary>
    /// HyperDrive status information
    /// </summary>
    public class HyperDriveStatus
    {
        public bool IsEnabled { get; set; }
        public bool AutoFailoverEnabled { get; set; }
        public bool AutoReplicationEnabled { get; set; }
        public bool AutoLoadBalancingEnabled { get; set; }
        public LoadBalancingStrategy DefaultStrategy { get; set; }
        public List<ProviderType> EnabledProviders { get; set; }
        public List<ProviderType> LoadBalancingProviders { get; set; }
        public int TotalProviders { get; set; }
        public int ActiveProviders { get; set; }
        public DateTime LastHealthCheck { get; set; }
    }
}
