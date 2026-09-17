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
    // Replication Management
    // -------------------------
    [HttpPost("replication/triggers")]
        public ActionResult<OASISResult<ReplicationTriggerConfig>> CreateReplicationTrigger([FromBody] ReplicationTriggerConfig trigger)
        {
            if (trigger == null)
                return BadRequest(new OASISResult<ReplicationTriggerConfig> { IsError = true, Message = "The request body is required. Please provide a valid ReplicationTriggerConfig." });
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
            if (trigger == null)
                return BadRequest(new OASISResult<ReplicationTriggerConfig> { IsError = true, Message = "The request body is required. Please provide a valid ReplicationTriggerConfig." });
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
            if (rule == null)
                return BadRequest(new OASISResult<ProviderReplicationRuleConfig> { IsError = true, Message = "The request body is required. Please provide a valid ProviderReplicationRuleConfig." });
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
            if (rule == null)
                return BadRequest(new OASISResult<DataTypeReplicationRuleConfig> { IsError = true, Message = "The request body is required. Please provide a valid DataTypeReplicationRuleConfig." });
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
            if (rule == null)
                return BadRequest(new OASISResult<ScheduleRuleConfig> { IsError = true, Message = "The request body is required. Please provide a valid ScheduleRuleConfig." });
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
            if (rule == null)
                return BadRequest(new OASISResult<CostOptimizationRuleConfig> { IsError = true, Message = "The request body is required. Please provide a valid CostOptimizationRuleConfig." });
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
    }
}
