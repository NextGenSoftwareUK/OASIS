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

    }
}