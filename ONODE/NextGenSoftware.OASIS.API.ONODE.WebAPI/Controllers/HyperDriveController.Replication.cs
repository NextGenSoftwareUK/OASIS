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

        // -------------------------
        // Failover Management
        // -------------------------
        [HttpPost("failover/triggers")]
        public ActionResult<OASISResult<FailoverTriggerConfig>> CreateFailoverTrigger([FromBody] FailoverTriggerConfig trigger)
        {
            if (trigger == null)
                return BadRequest(new OASISResult<FailoverTriggerConfig> { IsError = true, Message = "The request body is required. Please provide a valid FailoverTriggerConfig." });
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
            if (trigger == null)
                return BadRequest(new OASISResult<FailoverTriggerConfig> { IsError = true, Message = "The request body is required. Please provide a valid FailoverTriggerConfig." });
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
            if (rule == null)
                return BadRequest(new OASISResult<ProviderFailoverRuleConfig> { IsError = true, Message = "The request body is required. Please provide a valid ProviderFailoverRuleConfig." });
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
            if (rule == null)
                return BadRequest(new OASISResult<EscalationRuleConfig> { IsError = true, Message = "The request body is required. Please provide a valid EscalationRuleConfig." });
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
            if (alert == null)
                return BadRequest(new OASISResult<UsageAlertConfig> { IsError = true, Message = "The request body is required. Please provide a valid UsageAlertConfig." });
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
            if (alert == null)
                return BadRequest(new OASISResult<UsageAlertConfig> { IsError = true, Message = "The request body is required. Please provide a valid UsageAlertConfig." });
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
            if (notification == null)
                return BadRequest(new OASISResult<QuotaNotificationConfig> { IsError = true, Message = "The request body is required. Please provide a valid QuotaNotificationConfig." });
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
            if (notification == null)
                return BadRequest(new OASISResult<QuotaNotificationConfig> { IsError = true, Message = "The request body is required. Please provide a valid QuotaNotificationConfig." });
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
    }
}