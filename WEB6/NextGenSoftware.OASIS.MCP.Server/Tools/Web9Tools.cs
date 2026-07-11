using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;
using NextGenSoftware.OASIS.Web9.Core.Managers;

namespace NextGenSoftware.OASIS.MCP.Server.Tools
{
    /// <summary>WEB9 - the Singularity Layer made literal: live unified status aggregation across WEB4-WEB8.</summary>
    [McpServerToolType]
    public static class Web9Tools
    {
        private static readonly SingularityAggregationManager _manager = new SingularityAggregationManager();

        [McpServerTool(Name = "web9_get_unified_status"), Description("WEB9: probes WEB4-WEB8 in parallel and returns one unified status report - \"the network observing itself\" implemented as real cross-service health aggregation and live metric collection.")]
        public static async Task<string> GetUnifiedStatus()
        {
            var result = await _manager.GetUnifiedStatusAsync();
            return JsonSerializer.Serialize(result);
        }
    }
}
