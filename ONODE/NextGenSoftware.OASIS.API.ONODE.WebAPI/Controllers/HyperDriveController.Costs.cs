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
    public partial class HyperDriveController
    {

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
            if (limits == null)
                return BadRequest(new OASISResult<bool> { IsError = true, Message = "The request body is required. Please provide a valid JSON object with cost limits." });
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
            if (rules == null)
                return BadRequest(new OASISResult<bool> { IsError = true, Message = "The request body is required. Please provide a valid ReplicationRulesConfig." });
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
            if (rules == null)
                return BadRequest(new OASISResult<bool> { IsError = true, Message = "The request body is required. Please provide a valid FailoverRulesConfig." });
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
            if (config == null)
                return BadRequest(new OASISResult<bool> { IsError = true, Message = "The request body is required. Please provide a valid SubscriptionConfig." });
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
            if (permissions == null)
                return BadRequest(new OASISResult<bool> { IsError = true, Message = "The request body is required. Please provide a valid DataPermissionsConfig." });
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
            if (mode == null)
                return BadRequest(new OASISResult<bool> { IsError = true, Message = "The request body is required. Please provide a valid IntelligentModeConfig." });
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
