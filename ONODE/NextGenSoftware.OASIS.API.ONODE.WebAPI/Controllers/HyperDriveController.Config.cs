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
            if (mode == null)
                return BadRequest(new OASISResult<bool> { IsError = true, Message = "The request body is required. Please provide the HyperDrive mode value." });
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
            if (config == null)
                return BadRequest(new OASISResult<bool> { IsError = true, Message = "The request body is required. Please provide a valid HyperDrive configuration object." });
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
            if (request == null)
                return BadRequest(new OASISResult<bool> { IsError = true, Message = "The request body is required. Please provide a valid record request (ProviderType, Success, ResponseTimeMs, Cost)." });
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
            if (request == null)
                return BadRequest(new OASISResult<bool> { IsError = true, Message = "The request body is required. Please provide a valid record connection (ProviderType, IsConnecting)." });
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
            if (geoInfo == null)
                return BadRequest(new OASISResult<bool> { IsError = true, Message = "The request body is required. Please provide a valid GeographicInfo object." });
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
            if (costAnalysis == null)
                return BadRequest(new OASISResult<bool> { IsError = true, Message = "The request body is required. Please provide a valid CostAnalysis object." });
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

    }
}