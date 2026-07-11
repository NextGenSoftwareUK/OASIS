using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;
using NextGenSoftware.OASIS.Web10.Core.Managers;

namespace NextGenSoftware.OASIS.MCP.Server.Tools
{
    /// <summary>WEB10 - "The Source" / WEB0 made literal: the root source-of-truth endpoint for the whole OASIS stack.</summary>
    [McpServerToolType]
    public static class Web10Tools
    {
        private static readonly SourceManager _manager = new SourceManager();

        [McpServerTool(Name = "web10_get_source"), Description("WEB10: returns the foundational OASIS runtime/version identity (the Alpha) together with WEB9's live unified status across WEB4-WEB8 (the Omega) - \"WEB10 = WEB0\" as one real, queryable endpoint.")]
        public static async Task<string> GetSource()
        {
            var result = await _manager.GetSourceAsync();
            return JsonSerializer.Serialize(result);
        }
    }
}
