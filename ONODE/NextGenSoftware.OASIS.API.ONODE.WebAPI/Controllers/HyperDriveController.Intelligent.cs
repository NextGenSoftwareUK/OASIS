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