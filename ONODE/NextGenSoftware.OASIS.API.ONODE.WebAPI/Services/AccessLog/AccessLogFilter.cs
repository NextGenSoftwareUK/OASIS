using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.AccessLog
{
    /// <summary>
    /// Records successful holon reads on the Data API (actions named Load* or Search*). The first thing it does is read the
    /// DNA master switch; with AccessLog.Enabled false it returns immediately. It never alters the response and never throws.
    /// </summary>
    public class AccessLogFilter : IActionFilter
    {
        private readonly AccessLogService _service;
        public AccessLogFilter(AccessLogService service) { _service = service; }

        public void OnActionExecuting(ActionExecutingContext context) { }

        public void OnActionExecuted(ActionExecutedContext context)
        {
            try
            {
                if (!_service.IsEnabled || context.Exception != null) return;
                var action = context.ActionDescriptor.RouteValues.TryGetValue("action", out var a) ? a : null;
                if (action == null || !(action.StartsWith("Load", StringComparison.Ordinal) || action.StartsWith("Search", StringComparison.Ordinal))) return;
                var value = (context.Result as ObjectResult)?.Value;
                var holons = Extract(value);
                if (holons.Count == 0) return;
                var http = context.HttpContext;
                var avatar = http.Items.TryGetValue("Avatar", out var av) ? av as IAvatar : null;
                if (avatar == null) return;
                var appId = http.Request.Headers.TryGetValue("X-OASIS-App-Id", out var h) ? h.ToString() : null;
                _service.Record(avatar.Id, appId, action, holons, http.Connection.RemoteIpAddress?.ToString());
            }
            catch (Exception) { /* a read is never affected by logging */ }
        }

        /// <summary>Finds holons inside OASISHttpResponseMessage&lt;T&gt;.Result.Result without knowing T.</summary>
        internal static List<(Guid Id, string Type)> Extract(object response)
        {
            var found = new List<(Guid, string)>();
            if (response == null) return found;
            var oasisResult = response.GetType().GetProperty("Result")?.GetValue(response);
            var payload = oasisResult?.GetType().GetProperty("Result")?.GetValue(oasisResult);
            if (payload is IHolon one) found.Add(Describe(one));
            else if (payload is IEnumerable many && !(payload is string))
                foreach (var item in many) if (item is IHolon holon) found.Add(Describe(holon));
            return found;
        }

        private static (Guid, string) Describe(IHolon h)
            => (h.Id, string.IsNullOrWhiteSpace(h.CustomHolonType) ? h.HolonType.ToString() : h.CustomHolonType);
    }
}
