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
{


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
                var result = new OASISResult<OASISHyperDriveConfig>
                {
                    Result = config,
                    Message = "HyperDrive configuration retrieved successfully."
                };

                // Return test data if setting is enabled and result is null, has error, or result is null
                // Note: HyperDriveController doesn't inherit from OASISControllerBase, so we need to check config directly
                var configService = HttpContext.RequestServices.GetService(typeof(Microsoft.Extensions.Configuration.IConfiguration)) as Microsoft.Extensions.Configuration.IConfiguration;
                bool useTestData = configService?.GetValue<bool>("OASIS:UseTestDataWhenLiveDataNotAvailable", 
                    bool.Parse(Environment.GetEnvironmentVariable("USE_TEST_DATA_WHEN_LIVE_DATA_NOT_AVAILABLE") ?? "false")) ?? false;

                if (useTestData && (result == null || result.IsError || result.Result == null))
                {
                    return Ok(new OASISResult<OASISHyperDriveConfig>
                    {
                        Result = null,
                        IsError = false,
                        Message = "HyperDrive configuration retrieved successfully (using test data)."
                    });
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                // Return test data if setting is enabled, otherwise return error
                var configService = HttpContext.RequestServices.GetService(typeof(Microsoft.Extensions.Configuration.IConfiguration)) as Microsoft.Extensions.Configuration.IConfiguration;
                bool useTestData = configService?.GetValue<bool>("OASIS:UseTestDataWhenLiveDataNotAvailable", 
                    bool.Parse(Environment.GetEnvironmentVariable("USE_TEST_DATA_WHEN_LIVE_DATA_NOT_AVAILABLE") ?? "false")) ?? false;

                if (useTestData)
                {
                    return Ok(new OASISResult<OASISHyperDriveConfig>
                    {
                        Result = null,
                        IsError = false,
                        Message = "HyperDrive configuration retrieved successfully (using test data)."
                    });
                }
                return BadRequest(new OASISResult<OASISHyperDriveConfig>
                {
                    IsError = true,
                    Message = $"Error retrieving HyperDrive configuration: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Gets/sets HyperDrive mode (Legacy | OASISHyperDrive2)
        /// </summary>
        [HttpGet("mode")]
        public ActionResult<OASISResult<string>> GetHyperDriveMode()
        {
            try
            {
                var dna = OASISDNAManager.OASISDNA.OASIS;
                var mode = dna?.HyperDriveMode ?? "Legacy";
                return Ok(new OASISResult<string> { Result = mode, Message = "HyperDrive mode retrieved." });
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<string> { IsError = true, Message = ex.Message, Exception = ex });
            }
        }

        [HttpPut("mode")]
        public async Task<ActionResult<OASISResult<bool>>> SetHyperDriveMode([FromBody] string mode)
        {
            try
            {
                var dna = OASISDNAManager.OASISDNA.OASIS;
                if (dna != null)
                {
                    dna.HyperDriveMode = mode;
                    await OASISDNAManager.SaveDNAAsync();
                }
                return Ok(new OASISResult<bool> { Result = true, Message = "HyperDrive mode updated." });
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<bool> { IsError = true, Message = ex.Message, Exception = ex });
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
                if (result.IsError)
                {
                    return BadRequest(result);
                }
                else
                {
                    return Ok(result);
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
                    if (Enum.TryParse<ProviderType>(provider, out var providerType))
                    {
                        connections[providerType] = _performanceMonitor.GetActiveConnections(providerType);
                    }
                }

                return Ok(new OASISResult<Dictionary<ProviderType, int>>
                {
                    Result = connections,

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
                var availableProviders = config.LoadBalancingProviders?.Select(p => Enum.TryParse<ProviderType>(p, out var providerType) ? providerType : ProviderType.None).Where(p => p != ProviderType.None).ToList() ?? new List<ProviderType>();
                var selectedStrategy = strategy ?? (Enum.TryParse<LoadBalancingStrategy>(config.DefaultStrategy, out var defaultStrategy) ? defaultStrategy : LoadBalancingStrategy.RoundRobin);

                var bestProvider = _performanceMonitor.GetBestProvider(availableProviders, selectedStrategy);

                return Ok(new OASISResult<ProviderType>
                {
                    Result = bestProvider,

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
    [HttpPost("replication/triggers")]
        public ActionResult<OASISResult<ReplicationTriggerConfig>> CreateReplicationTrigger([FromBody] ReplicationTriggerConfig trigger)
        {
            try
            {
                var dna = OASISDNAManager.OASISDNA.OASIS;
                trigger.Id ??= Guid.NewGuid().ToString();
                dna.ReplicationRules.ReplicationTriggers.Add(trigger);
                OASISDNAManager.SaveDNA();

                return Ok(new OASISResult<ReplicationTriggerConfig>
                {
                    Result = trigger,

                    Message = "Replication trigger created successfully."
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<ReplicationTriggerConfig>
                {
                    IsError = true,
                    Message = $"Failed to create replication trigger: {ex.Message}",
                    Exception = ex
                });
            }
        }

        [HttpPut("replication/triggers/{id}")]
        public ActionResult<OASISResult<ReplicationTriggerConfig>> UpdateReplicationTrigger(string id, [FromBody] ReplicationTriggerConfig trigger)
        {
            try
            {
                var dna = OASISDNAManager.OASISDNA.OASIS;
                var list = dna.ReplicationRules.ReplicationTriggers;
                var idx = list.FindIndex(t => t.Id == id);
                if (idx < 0)
                {
                    return NotFound(new OASISResult<ReplicationTriggerConfig>
                    {
                        IsError = true,
                        Message = $"Replication trigger {id} not found."
                    });
                }
                trigger.Id = id;
                list[idx] = trigger;
                OASISDNAManager.SaveDNA();

                return Ok(new OASISResult<ReplicationTriggerConfig>
                {
                    Result = trigger,

                    Message = "Replication trigger updated successfully."
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<ReplicationTriggerConfig>
                {
                    IsError = true,
                    Message = $"Failed to update replication trigger: {ex.Message}",
                    Exception = ex
                });
            }
        }

        [HttpDelete("replication/triggers/{id}")]
        public ActionResult<OASISResult<bool>> DeleteReplicationTrigger(string id)
        {
            try
            {
                var dna = OASISDNAManager.OASISDNA.OASIS;
                var removed = dna.ReplicationRules.ReplicationTriggers.RemoveAll(t => t.Id == id) > 0;
                OASISDNAManager.SaveDNA();
                return Ok(new OASISResult<bool>
                {
                    Result = removed,
                    Message = removed ? "Replication trigger deleted successfully." : "Trigger not found."
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<bool>
                {
                    IsError = true,
                    Message = $"Failed to delete replication trigger: {ex.Message}",
                    Exception = ex
                });
            }
        }

        [HttpGet("replication/provider-rules")]
        public ActionResult<OASISResult<List<ProviderReplicationRuleConfig>>> GetProviderReplicationRules()
        {
            try
            {
                var rules = OASISDNAManager.OASISDNA.OASIS.ReplicationRules.ProviderRules;
                return Ok(new OASISResult<List<ProviderReplicationRuleConfig>> { Result = rules });
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<List<ProviderReplicationRuleConfig>> { IsError = true, Message = ex.Message, Exception = ex });
            }
        }

        [HttpPut("replication/provider-rules")]
        public ActionResult<OASISResult<ProviderReplicationRuleConfig>> UpdateProviderReplicationRule([FromBody] ProviderReplicationRuleConfig rule)
        {
            try
            {
                var dna = OASISDNAManager.OASISDNA.OASIS;
                var list = dna.ReplicationRules.ProviderRules;
                var idx = list.FindIndex(r => r.ProviderType == rule.ProviderType);
                if (idx >= 0) list[idx] = rule; else list.Add(rule);
                OASISDNAManager.SaveDNA();
                return Ok(new OASISResult<ProviderReplicationRuleConfig> { Result = rule });
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<ProviderReplicationRuleConfig> { IsError = true, Message = ex.Message, Exception = ex });
            }
        }

        [HttpGet("replication/data-type-rules")]
        public ActionResult<OASISResult<List<DataTypeReplicationRuleConfig>>> GetDataTypeReplicationRules()
        {
            try
            {
                var rules = OASISDNAManager.OASISDNA.OASIS.ReplicationRules.DataTypeRules;
                return Ok(new OASISResult<List<DataTypeReplicationRuleConfig>> { Result = rules });
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<List<DataTypeReplicationRuleConfig>> { IsError = true, Message = ex.Message, Exception = ex });
            }
        }

        [HttpPut("replication/data-type-rules")]
        public ActionResult<OASISResult<DataTypeReplicationRuleConfig>> UpdateDataTypeReplicationRule([FromBody] DataTypeReplicationRuleConfig rule)
        {
            try
            {
                var dna = OASISDNAManager.OASISDNA.OASIS;
                var list = dna.ReplicationRules.DataTypeRules;
                var idx = list.FindIndex(r => r.DataType == rule.DataType);
                if (idx >= 0) list[idx] = rule; else list.Add(rule);
                OASISDNAManager.SaveDNA();
                return Ok(new OASISResult<DataTypeReplicationRuleConfig> { Result = rule });
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<DataTypeReplicationRuleConfig> { IsError = true, Message = ex.Message, Exception = ex });
            }
        }

        [HttpGet("replication/schedule-rules")]
        public ActionResult<OASISResult<List<ScheduleRuleConfig>>> GetScheduleRules()
        {
            try
            {
                var rules = OASISDNAManager.OASISDNA.OASIS.ReplicationRules.ScheduleRules;
                return Ok(new OASISResult<List<ScheduleRuleConfig>> { Result = rules });
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<List<ScheduleRuleConfig>> { IsError = true, Message = ex.Message, Exception = ex });
            }
        }

        [HttpPut("replication/schedule-rules")]
        public ActionResult<OASISResult<ScheduleRuleConfig>> UpdateScheduleRule([FromBody] ScheduleRuleConfig rule)
        {
            try
            {
                var dna = OASISDNAManager.OASISDNA.OASIS;
                var list = dna.ReplicationRules.ScheduleRules;
                var idx = list.FindIndex(r => r.Name == rule.Name);
                if (string.IsNullOrEmpty(rule.Name)) rule.Name = Guid.NewGuid().ToString();
                if (idx >= 0) list[idx] = rule; else list.Add(rule);
                OASISDNAManager.SaveDNA();
                return Ok(new OASISResult<ScheduleRuleConfig> { Result = rule });
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<ScheduleRuleConfig> { IsError = true, Message = ex.Message, Exception = ex });
            }
        }

        [HttpGet("replication/cost-optimization")]
        public ActionResult<OASISResult<CostOptimizationRuleConfig>> GetCostOptimizationRule()
        {
            try
            {
                var rule = OASISDNAManager.OASISDNA.OASIS.ReplicationRules.CostOptimization;
                return Ok(new OASISResult<CostOptimizationRuleConfig> { Result = rule });
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<CostOptimizationRuleConfig> { IsError = true, Message = ex.Message, Exception = ex });
            }
        }

        [HttpPut("replication/cost-optimization")]
        public ActionResult<OASISResult<CostOptimizationRuleConfig>> UpdateCostOptimizationRule([FromBody] CostOptimizationRuleConfig rule)
        {
            try
            {
                var dna = OASISDNAManager.OASISDNA.OASIS;
                dna.ReplicationRules.CostOptimization = rule;
                OASISDNAManager.SaveDNA();
                return Ok(new OASISResult<CostOptimizationRuleConfig> { Result = rule });
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<CostOptimizationRuleConfig> { IsError = true, Message = ex.Message, Exception = ex });
            }
        }

        // -------------------------
        // Failover Management
        // -------------------------
        [HttpPost("failover/triggers")]
        public ActionResult<OASISResult<FailoverTriggerConfig>> CreateFailoverTrigger([FromBody] FailoverTriggerConfig trigger)
        {
            try
            {
                var dna = OASISDNAManager.OASISDNA.OASIS;
                trigger.Id ??= Guid.NewGuid().ToString();
                dna.FailoverRules.FailoverTriggers.Add(trigger);
                OASISDNAManager.SaveDNA();
                return Ok(new OASISResult<FailoverTriggerConfig> { Result = trigger, Message = "Failover trigger created successfully." });
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<FailoverTriggerConfig> { IsError = true, Message = ex.Message, Exception = ex });
            }
        }

        [HttpPut("failover/triggers/{id}")]
        public ActionResult<OASISResult<FailoverTriggerConfig>> UpdateFailoverTrigger(string id, [FromBody] FailoverTriggerConfig trigger)
        {
            try
            {
                var dna = OASISDNAManager.OASISDNA.OASIS;
                var list = dna.FailoverRules.FailoverTriggers;
                var idx = list.FindIndex(t => t.Id == id);
                if (idx < 0)
                {
                    return NotFound(new OASISResult<FailoverTriggerConfig> { IsError = true, Message = $"Failover trigger {id} not found." });
                }
                trigger.Id = id;
                list[idx] = trigger;
                OASISDNAManager.SaveDNA();
                return Ok(new OASISResult<FailoverTriggerConfig> { Result = trigger });
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<FailoverTriggerConfig> { IsError = true, Message = ex.Message, Exception = ex });
            }
        }

        [HttpDelete("failover/triggers/{id}")]
        public ActionResult<OASISResult<bool>> DeleteFailoverTrigger(string id)
        {
            try
            {
                var dna = OASISDNAManager.OASISDNA.OASIS;
                var removed = dna.FailoverRules.FailoverTriggers.RemoveAll(t => t.Id == id) > 0;
                OASISDNAManager.SaveDNA();
                return Ok(new OASISResult<bool> { Result = removed, Message = removed ? "Failover trigger deleted successfully." : "Trigger not found." });
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<bool> { IsError = true, Message = ex.Message, Exception = ex });
            }
        }

        [HttpGet("failover/provider-rules")]
        public ActionResult<OASISResult<List<ProviderFailoverRuleConfig>>> GetProviderFailoverRules()
        {
            try
            {
                var rules = OASISDNAManager.OASISDNA.OASIS.FailoverRules.ProviderRules;
                return Ok(new OASISResult<List<ProviderFailoverRuleConfig>> { Result = rules });
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<List<ProviderFailoverRuleConfig>> { IsError = true, Message = ex.Message, Exception = ex });
            }
        }

        [HttpPut("failover/provider-rules")]
        public ActionResult<OASISResult<ProviderFailoverRuleConfig>> UpdateProviderFailoverRule([FromBody] ProviderFailoverRuleConfig rule)
        {
            try
            {
                var dna = OASISDNAManager.OASISDNA.OASIS;
                var list = dna.FailoverRules.ProviderRules;
                var idx = list.FindIndex(r => r.ProviderType == rule.ProviderType);
                if (idx >= 0) list[idx] = rule; else list.Add(rule);
                OASISDNAManager.SaveDNA();
                return Ok(new OASISResult<ProviderFailoverRuleConfig> { Result = rule });
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<ProviderFailoverRuleConfig> { IsError = true, Message = ex.Message, Exception = ex });
            }
        }

        [HttpGet("failover/escalation-rules")]
        public ActionResult<OASISResult<List<EscalationRuleConfig>>> GetEscalationRules()
        {
            try
            {
                var rules = OASISDNAManager.OASISDNA.OASIS.FailoverRules.EscalationRules;
                return Ok(new OASISResult<List<EscalationRuleConfig>> { Result = rules });
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<List<EscalationRuleConfig>> { IsError = true, Message = ex.Message, Exception = ex });
            }
        }

        [HttpPut("failover/escalation-rules")]
        public ActionResult<OASISResult<EscalationRuleConfig>> UpdateEscalationRule([FromBody] EscalationRuleConfig rule)
        {
            try
            {
                var dna = OASISDNAManager.OASISDNA.OASIS;
                var list = dna.FailoverRules.EscalationRules;
                var idx = list.FindIndex(r => r.Name == rule.Name);
                if (string.IsNullOrEmpty(rule.Name)) rule.Name = Guid.NewGuid().ToString();
                if (idx >= 0) list[idx] = rule; else list.Add(rule);
                OASISDNAManager.SaveDNA();
                return Ok(new OASISResult<EscalationRuleConfig> { Result = rule });
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<EscalationRuleConfig> { IsError = true, Message = ex.Message, Exception = ex });
            }
        }

        // -------------------------
        // Subscription Alerts/Notifications
        // -------------------------
        [HttpGet("subscription/usage-alerts")]
        public ActionResult<OASISResult<List<UsageAlertConfig>>> GetUsageAlerts()
        {
            try
            {
                var list = OASISDNAManager.OASISDNA.OASIS.SubscriptionConfig.UsageAlerts;
                return Ok(new OASISResult<List<UsageAlertConfig>> { Result = list });
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<List<UsageAlertConfig>> { IsError = true, Message = ex.Message, Exception = ex });
            }
        }

        [HttpPost("subscription/usage-alerts")]
        public ActionResult<OASISResult<UsageAlertConfig>> CreateUsageAlert([FromBody] UsageAlertConfig alert)
        {
            try
            {
                var dna = OASISDNAManager.OASISDNA.OASIS;
                alert.Id ??= Guid.NewGuid().ToString();
                dna.SubscriptionConfig.UsageAlerts.Add(alert);
                OASISDNAManager.SaveDNA();
                return Ok(new OASISResult<UsageAlertConfig> { Result = alert });
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<UsageAlertConfig> { IsError = true, Message = ex.Message, Exception = ex });
            }
        }

        [HttpPut("subscription/usage-alerts/{id}")]
        public ActionResult<OASISResult<UsageAlertConfig>> UpdateUsageAlert(string id, [FromBody] UsageAlertConfig alert)
        {
            try
            {
                var dna = OASISDNAManager.OASISDNA.OASIS;
                var list = dna.SubscriptionConfig.UsageAlerts;
                var idx = list.FindIndex(a => a.Id == id);
                if (idx < 0) return NotFound(new OASISResult<UsageAlertConfig> { IsError = true, Message = "Alert not found" });
                alert.Id = id;
                list[idx] = alert;
                OASISDNAManager.SaveDNA();
                return Ok(new OASISResult<UsageAlertConfig> { Result = alert });
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<UsageAlertConfig> { IsError = true, Message = ex.Message, Exception = ex });
            }
        }

        [HttpDelete("subscription/usage-alerts/{id}")]
        public ActionResult<OASISResult<bool>> DeleteUsageAlert(string id)
        {
            try
            {
                var dna = OASISDNAManager.OASISDNA.OASIS;
                var removed = dna.SubscriptionConfig.UsageAlerts.RemoveAll(a => a.Id == id) > 0;
                OASISDNAManager.SaveDNA();
                return Ok(new OASISResult<bool> { Result = removed });
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<bool> { IsError = true, Message = ex.Message, Exception = ex });
            }
        }

        [HttpGet("subscription/quota-notifications")]
        public ActionResult<OASISResult<List<QuotaNotificationConfig>>> GetQuotaNotifications()
        {
            try
            {
                var list = OASISDNAManager.OASISDNA.OASIS.SubscriptionConfig.QuotaNotifications;
                return Ok(new OASISResult<List<QuotaNotificationConfig>> { Result = list });
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<List<QuotaNotificationConfig>> { IsError = true, Message = ex.Message, Exception = ex });
            }
        }

        [HttpPost("subscription/quota-notifications")]
        public ActionResult<OASISResult<QuotaNotificationConfig>> CreateQuotaNotification([FromBody] QuotaNotificationConfig notification)
        {
            try
            {
                var dna = OASISDNAManager.OASISDNA.OASIS;
                notification.Id ??= Guid.NewGuid().ToString();
                dna.SubscriptionConfig.QuotaNotifications.Add(notification);
                OASISDNAManager.SaveDNA();
                return Ok(new OASISResult<QuotaNotificationConfig> { Result = notification });
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<QuotaNotificationConfig> { IsError = true, Message = ex.Message, Exception = ex });
            }
        }

        [HttpPut("subscription/quota-notifications/{id}")]
        public ActionResult<OASISResult<QuotaNotificationConfig>> UpdateQuotaNotification(string id, [FromBody] QuotaNotificationConfig notification)
        {
            try
            {
                var dna = OASISDNAManager.OASISDNA.OASIS;
                var list = dna.SubscriptionConfig.QuotaNotifications;
                var idx = list.FindIndex(n => n.Id == id);
                if (idx < 0) return NotFound(new OASISResult<QuotaNotificationConfig> { IsError = true, Message = "Notification not found" });
                notification.Id = id;
                list[idx] = notification;
                OASISDNAManager.SaveDNA();
                return Ok(new OASISResult<QuotaNotificationConfig> { Result = notification });
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<QuotaNotificationConfig> { IsError = true, Message = ex.Message, Exception = ex });
            }
        }

        [HttpDelete("subscription/quota-notifications/{id}")]
        public ActionResult<OASISResult<bool>> DeleteQuotaNotification(string id)
        {
            try
            {
                var dna = OASISDNAManager.OASISDNA.OASIS;
                var removed = dna.SubscriptionConfig.QuotaNotifications.RemoveAll(n => n.Id == id) > 0;
                OASISDNAManager.SaveDNA();
                return Ok(new OASISResult<bool> { Result = removed });
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<bool> { IsError = true, Message = ex.Message, Exception = ex });
            }
        }

        // -------------------------
        // Cost Endpoints
        // -------------------------
        [HttpGet("costs/current")]
        public ActionResult<OASISResult<Dictionary<string, decimal>>> GetCurrentCosts()
        {
            try
            {
                var costs = AdvancedAnalyticsEngine.Instance.GetCurrentCostsAsync().Result;
                return Ok(new OASISResult<Dictionary<string, decimal>> { Result = costs });
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<Dictionary<string, decimal>> { IsError = true, Message = ex.Message, Exception = ex });
            }
        }

        [HttpGet("costs/history")]
        public ActionResult<OASISResult<Dictionary<string, List<decimal>>>> GetCostHistory([FromQuery] string timeRange = "Last30Days")
        {
            try
            {
                var history = AdvancedAnalyticsEngine.Instance.GetCostHistoryAsync(timeRange).Result;
                return Ok(new OASISResult<Dictionary<string, List<decimal>>> { Result = history });
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<Dictionary<string, List<decimal>>> { IsError = true, Message = ex.Message, Exception = ex });
            }
        }

        [HttpGet("costs/projections")]
        public ActionResult<OASISResult<Dictionary<string, decimal>>> GetCostProjections()
        {
            try
            {
                var projections = AdvancedAnalyticsEngine.Instance.GetCostProjectionsAsync().Result;
                return Ok(new OASISResult<Dictionary<string, decimal>> { Result = projections });
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<Dictionary<string, decimal>> { IsError = true, Message = ex.Message, Exception = ex });
            }
        }

        [HttpPut("costs/limits")]
        public ActionResult<OASISResult<bool>> SetCostLimits([FromBody] Dictionary<string, decimal> limits)
        {
            try
            {
                AdvancedAnalyticsEngine.Instance.SetCostLimits(limits);
                return Ok(new OASISResult<bool> { Result = true, Message = "Cost limits updated successfully." });
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<bool> { IsError = true, Message = ex.Message, Exception = ex });
            }
        }

        // -------------------------
        // Recommendations Endpoints
        // -------------------------
        [HttpGet("recommendations/smart")]
        public ActionResult<OASISResult<Dictionary<string, object>>> GetSmartRecommendations()
        {
            try
            {
                var recs = AIOptimizationEngine.Instance.GetSmartRecommendationsAsync().Result;
                var result = new Dictionary<string, object> { { "recommendations", recs } };
                return Ok(new OASISResult<Dictionary<string, object>> { Result = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<Dictionary<string, object>> { IsError = true, Message = ex.Message, Exception = ex });
            }
        }

        [HttpGet("recommendations/security")]
        public ActionResult<OASISResult<Dictionary<string, object>>> GetSecurityRecommendations()
        {
            try
            {
                var recs = AdvancedAnalyticsEngine.Instance.GetSecurityRecommendationsAsync().Result;
                return Ok(new OASISResult<Dictionary<string, object>> { Result = recs });
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<Dictionary<string, object>> { IsError = true, Message = ex.Message, Exception = ex });
            }
        }
    

        // Enhanced HyperDrive API Endpoints

        /// <summary>
        /// Gets replication rules configuration
        /// </summary>
        [HttpGet("replication/rules")]
        public ActionResult<OASISResult<ReplicationRulesConfig>> GetReplicationRules()
        {
            try
            {
                var dna = OASISDNAManager.OASISDNA.OASIS;
                var rules = dna?.ReplicationRules ?? new ReplicationRulesConfig();

                return Ok(new OASISResult<ReplicationRulesConfig>
                {
                    Result = rules,

                    Message = "Replication rules retrieved successfully."
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<ReplicationRulesConfig>
                {
                    IsError = true,
                    Message = $"Failed to get replication rules: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Updates replication rules configuration
        /// </summary>
        [HttpPut("replication/rules")]
        public async Task<ActionResult<OASISResult<bool>>> UpdateReplicationRules([FromBody] ReplicationRulesConfig rules)
        {
            try
            {
                var dna = OASISDNAManager.OASISDNA.OASIS;
                if (dna != null)
                {
                    dna.ReplicationRules = rules;
                    await OASISDNAManager.SaveDNAAsync();
                }

                return Ok(new OASISResult<bool>
                {
                    Result = true,

                    Message = "Replication rules updated successfully."
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<bool>
                {
                    IsError = true,
                    Message = $"Failed to update replication rules: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Gets failover rules configuration
        /// </summary>
        [HttpGet("failover/rules")]
        public ActionResult<OASISResult<FailoverRulesConfig>> GetFailoverRules()
        {
            try
            {
                var dna = OASISDNAManager.OASISDNA.OASIS;
                var rules = dna?.FailoverRules ?? new FailoverRulesConfig();

                return Ok(new OASISResult<FailoverRulesConfig>
                {
                    Result = rules,

                    Message = "Failover rules retrieved successfully."
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<FailoverRulesConfig>
                {
                    IsError = true,
                    Message = $"Failed to get failover rules: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Updates failover rules configuration
        /// </summary>
        [HttpPut("failover/rules")]
        public async Task<ActionResult<OASISResult<bool>>> UpdateFailoverRules([FromBody] FailoverRulesConfig rules)
        {
            try
            {
                var dna = OASISDNAManager.OASISDNA.OASIS;
                if (dna != null)
                {
                    dna.FailoverRules = rules;
                    await OASISDNAManager.SaveDNAAsync();
                }

                return Ok(new OASISResult<bool>
                {
                    Result = true,

                    Message = "Failover rules updated successfully."
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<bool>
                {
                    IsError = true,
                    Message = $"Failed to update failover rules: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Gets subscription configuration
        /// </summary>
        [HttpGet("subscription/config")]
        public ActionResult<OASISResult<SubscriptionConfig>> GetSubscriptionConfig()
        {
            try
            {
                var dna = OASISDNAManager.OASISDNA.OASIS;
                var config = dna?.SubscriptionConfig ?? new SubscriptionConfig();

                return Ok(new OASISResult<SubscriptionConfig>
                {
                    Result = config,

                    Message = "Subscription configuration retrieved successfully."
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<SubscriptionConfig>
                {
                    IsError = true,
                    Message = $"Failed to get subscription config: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Updates subscription configuration
        /// </summary>
        [HttpPut("subscription/config")]
        public async Task<ActionResult<OASISResult<bool>>> UpdateSubscriptionConfig([FromBody] SubscriptionConfig config)
        {
            try
            {
                var dna = OASISDNAManager.OASISDNA.OASIS;
                if (dna != null)
                {
                    dna.SubscriptionConfig = config;
                    await OASISDNAManager.SaveDNAAsync();
                }

                return Ok(new OASISResult<bool>
                {
                    Result = true,

                    Message = "Subscription configuration updated successfully."
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<bool>
                {
                    IsError = true,
                    Message = $"Failed to update subscription config: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Gets data permissions configuration
        /// </summary>
        [HttpGet("data-permissions")]
        public ActionResult<OASISResult<DataPermissionsConfig>> GetDataPermissions()
        {
            try
            {
                var dna = OASISDNAManager.OASISDNA.OASIS;
                var permissions = dna?.DataPermissions ?? new DataPermissionsConfig();

                return Ok(new OASISResult<DataPermissionsConfig>
                {
                    Result = permissions,

                    Message = "Data permissions retrieved successfully."
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<DataPermissionsConfig>
                {
                    IsError = true,
                    Message = $"Failed to get data permissions: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Updates data permissions configuration
        /// </summary>
        [HttpPut("data-permissions")]
        public async Task<ActionResult<OASISResult<bool>>> UpdateDataPermissions([FromBody] DataPermissionsConfig permissions)
        {
            try
            {
                var dna = OASISDNAManager.OASISDNA.OASIS;
                if (dna != null)
                {
                    dna.DataPermissions = permissions;
                    await OASISDNAManager.SaveDNAAsync();
                }

                return Ok(new OASISResult<bool>
                {
                    Result = true,

                    Message = "Data permissions updated successfully."
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<bool>
                {
                    IsError = true,
                    Message = $"Failed to update data permissions: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Gets intelligent mode configuration
        /// </summary>
        [HttpGet("intelligent-mode")]
        public ActionResult<OASISResult<IntelligentModeConfig>> GetIntelligentMode()
        {
            try
            {
                var dna = OASISDNAManager.OASISDNA.OASIS;
                var mode = dna?.IntelligentMode ?? new IntelligentModeConfig();

                return Ok(new OASISResult<IntelligentModeConfig>
                {
                    Result = mode,

                    Message = "Intelligent mode configuration retrieved successfully."
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<IntelligentModeConfig>
                {
                    IsError = true,
                    Message = $"Failed to get intelligent mode: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Updates intelligent mode configuration
        /// </summary>
        [HttpPut("intelligent-mode")]
        public async Task<ActionResult<OASISResult<bool>>> UpdateIntelligentMode([FromBody] IntelligentModeConfig mode)
        {
            try
            {
                var dna = OASISDNAManager.OASISDNA.OASIS;
                if (dna != null)
                {
                    dna.IntelligentMode = mode;
                    await OASISDNAManager.SaveDNAAsync();
                }

                return Ok(new OASISResult<bool>
                {
                    Result = true,

                    Message = "Intelligent mode configuration updated successfully."
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<bool>
                {
                    IsError = true,
                    Message = $"Failed to update intelligent mode: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Enables intelligent mode
        /// </summary>
        [HttpPost("intelligent-mode/enable")]
        public async Task<ActionResult<OASISResult<bool>>> EnableIntelligentMode()
        {
            try
            {
                var dna = OASISDNAManager.OASISDNA.OASIS;
                if (dna != null)
                {
                    dna.IntelligentMode.IsEnabled = true;
                    await OASISDNAManager.SaveDNAAsync();
                }

                return Ok(new OASISResult<bool>
                {
                    Result = true,

                    Message = "Intelligent mode enabled successfully."
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<bool>
                {
                    IsError = true,
                    Message = $"Failed to enable intelligent mode: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Disables intelligent mode
        /// </summary>
        [HttpPost("intelligent-mode/disable")]
        public async Task<ActionResult<OASISResult<bool>>> DisableIntelligentMode()
        {
            try
            {
                var dna = OASISDNAManager.OASISDNA.OASIS;
                if (dna != null)
                {
                    dna.IntelligentMode.IsEnabled = false;
                    await OASISDNAManager.SaveDNAAsync();
                }

                return Ok(new OASISResult<bool>
                {
                    Result = true,

                    Message = "Intelligent mode disabled successfully."
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<bool>
                {
                    IsError = true,
                    Message = $"Failed to disable intelligent mode: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Gets current usage statistics
        /// </summary>
        [HttpGet("quota/usage")]
        public ActionResult<OASISResult<Dictionary<string, int>>> GetCurrentUsage()
        {
            try
            {
                // This would typically come from a usage tracking service
                var usage = new Dictionary<string, int>
            {
                { "Replications", 45 },
                { "Failovers", 3 },
                { "Storage", 2 },
                { "Requests", 1250 }
            };

                return Ok(new OASISResult<Dictionary<string, int>>
                {
                    Result = usage,

                    Message = "Current usage retrieved successfully."
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<Dictionary<string, int>>
                {
                    IsError = true,
                    Message = $"Failed to get current usage: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Gets quota limits for current subscription
        /// </summary>
        [HttpGet("quota/limits")]
        public ActionResult<OASISResult<Dictionary<string, int>>> GetQuotaLimits()
        {
            try
            {
                var dna = OASISDNAManager.OASISDNA.OASIS;
                var config = dna?.SubscriptionConfig ?? new SubscriptionConfig();

                var limits = new Dictionary<string, int>
            {
                { "Replications", config.MaxReplicationsPerMonth },
                { "Failovers", config.MaxFailoversPerMonth },
                { "Storage", config.MaxStorageGB },
                { "Requests", GetRequestLimit(config.PlanType) }
            };

                return Ok(new OASISResult<Dictionary<string, int>>
                {
                    Result = limits,

                    Message = "Quota limits retrieved successfully."
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<Dictionary<string, int>>
                {
                    IsError = true,
                    Message = $"Failed to get quota limits: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Checks quota status for a specific type
        /// </summary>
        [HttpGet("quota/status")]
        public ActionResult<OASISResult<Dictionary<string, object>>> CheckQuotaStatus()
        {
            try
            {
                var dna = OASISDNAManager.OASISDNA.OASIS;
                var config = dna?.SubscriptionConfig ?? new SubscriptionConfig();

                var status = new Dictionary<string, object>
            {
                { "PlanType", config.PlanType },
                { "PayAsYouGoEnabled", config.PayAsYouGoEnabled },
                { "Usage", new Dictionary<string, int>
                    {
                        { "Replications", 45 },
                        { "Failovers", 3 },
                        { "Storage", 2 },
                        { "Requests", 1250 }
                    }
                },
                { "Limits", new Dictionary<string, int>
                    {
                        { "Replications", config.MaxReplicationsPerMonth },
                        { "Failovers", config.MaxFailoversPerMonth },
                        { "Storage", config.MaxStorageGB },
                        { "Requests", GetRequestLimit(config.PlanType) }
                    }
                }
            };

                return Ok(new OASISResult<Dictionary<string, object>>
                {
                    Result = status,

                    Message = "Quota status retrieved successfully."
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<Dictionary<string, object>>
                {
                    IsError = true,
                    Message = $"Failed to get quota status: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Gets free providers list
        /// </summary>
        [HttpGet("providers/free")]
        public ActionResult<OASISResult<List<string>>> GetFreeProviders()
        {
            try
            {
                var freeProviders = new List<string>
            {
                "MongoOASIS", "IPFSOASIS", "SEEDSOASIS", "ScuttlebuttOASIS",
                "ThreeFoldOASIS", "HoloOASIS", "PLANOASIS", "SOLIDOASIS",
                "BlockStackOASIS", "Web3CoreOASIS"
            };

                return Ok(new OASISResult<List<string>>
                {
                    Result = freeProviders,

                    Message = "Free providers retrieved successfully."
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<List<string>>
                {
                    IsError = true,
                    Message = $"Failed to get free providers: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Gets low-cost providers list
        /// </summary>
        [HttpGet("providers/low-cost")]
        public ActionResult<OASISResult<List<string>>> GetLowCostProviders()
        {
            try
            {
                var lowCostProviders = new List<string>
            {
                "PolygonOASIS", "FantomOASIS", "TelosOASIS", "ArbitrumOASIS"
            };

                return Ok(new OASISResult<List<string>>
                {
                    Result = lowCostProviders,

                    Message = "Low-cost providers retrieved successfully."
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<List<string>>
                {
                    IsError = true,
                    Message = $"Failed to get low-cost providers: {ex.Message}",
                    Exception = ex
                });
            }
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
}
