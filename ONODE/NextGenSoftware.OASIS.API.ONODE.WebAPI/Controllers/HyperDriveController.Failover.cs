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
    }
}
