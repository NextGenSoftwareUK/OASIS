using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;
using NextGenSoftware.OASIS.Web8.Core.Enums;
using NextGenSoftware.OASIS.Web8.Core.Managers;
using NextGenSoftware.OASIS.Web8.Core.Models;

namespace NextGenSoftware.OASIS.MCP.Server.Tools
{
    /// <summary>
    /// WEB8 - the fractal holonic mesh-networking layer. In-process MCP tools wrapping every public method of
    /// GalacticMeshManager and ProtocolBridgeManager.
    /// </summary>
    [McpServerToolType]
    public static class Web8Tools
    {
        private static Guid ParseAvatarId(string? avatarId) => Guid.TryParse(avatarId, out Guid id) ? id : Guid.Empty;
        private static readonly ProtocolBridgeManager _bridge = new ProtocolBridgeManager();

        [McpServerTool(Name = "web8_register_node"), Description("WEB8: registers a node in the mesh - any externally-reachable system that can accept a relayed message at an HTTP endpoint.")]
        public static async Task<string> RegisterNode(GalacticNode node, string? avatarId = null)
        {
            GalacticMeshManager manager = new GalacticMeshManager(ParseAvatarId(avatarId));
            var result = await manager.RegisterNodeAsync(node);
            return JsonSerializer.Serialize(result);
        }

        [McpServerTool(Name = "web8_get_nodes"), Description("WEB8: lists every node currently registered in the mesh.")]
        public static async Task<string> GetNodes(string? avatarId = null)
        {
            GalacticMeshManager manager = new GalacticMeshManager(ParseAvatarId(avatarId));
            var result = await manager.GetNodesAsync();
            return JsonSerializer.Serialize(result);
        }

        [McpServerTool(Name = "web8_add_link"), Description("WEB8: declares a bidirectional weighted link (mesh edge) between two nodes, carrying its latency for shortest-path routing.")]
        public static async Task<string> AddLink(string nodeAId, string nodeBId, double latencyMs = 50, string? avatarId = null)
        {
            GalacticMeshManager manager = new GalacticMeshManager(ParseAvatarId(avatarId));
            var result = await manager.AddLinkAsync(Guid.Parse(nodeAId), Guid.Parse(nodeBId), latencyMs);
            return JsonSerializer.Serialize(result);
        }

        [McpServerTool(Name = "web8_heartbeat"), Description("WEB8: records a heartbeat for a node, keeping it inside the liveness window so routing continues to consider it healthy.")]
        public static async Task<string> Heartbeat(string nodeId, string? avatarId = null)
        {
            GalacticMeshManager manager = new GalacticMeshManager(ParseAvatarId(avatarId));
            var result = await manager.HeartbeatAsync(Guid.Parse(nodeId));
            return JsonSerializer.Serialize(result);
        }

        [McpServerTool(Name = "web8_compute_route"), Description("WEB8: computes the shortest (lowest cumulative latency) path between two nodes via Dijkstra's algorithm, excluding any node outside the liveness window (self-healing).")]
        public static async Task<string> ComputeRoute(string sourceNodeId, string destinationNodeId, string? avatarId = null)
        {
            GalacticMeshManager manager = new GalacticMeshManager(ParseAvatarId(avatarId));
            var result = await manager.ComputeRouteAsync(Guid.Parse(sourceNodeId), Guid.Parse(destinationNodeId));
            return JsonSerializer.Serialize(result);
        }

        [McpServerTool(Name = "web8_send_message"), Description("WEB8: routes and relays a message hop-by-hop to its destination via real HTTP forwarding, self-healing around any failed/stale node by excluding it and recomputing the route.")]
        public static async Task<string> SendMessage(MeshMessage message, string? avatarId = null)
        {
            GalacticMeshManager manager = new GalacticMeshManager(ParseAvatarId(avatarId));
            var result = await manager.SendMessageAsync(message);
            return JsonSerializer.Serialize(result);
        }

        [McpServerTool(Name = "web8_translate_inbound"), Description("WEB8 protocol bridge: translates an external system's raw payload (Json, FormUrlEncoded or PlainText) into the unified MeshMessage envelope.")]
        public static string TranslateInbound(string rawPayload, string format, string sourceNodeId, string destinationNodeId)
        {
            MeshMessage message = _bridge.TranslateInbound(rawPayload, Enum.Parse<BridgeFormat>(format, true), Guid.Parse(sourceNodeId), Guid.Parse(destinationNodeId));
            return JsonSerializer.Serialize(message);
        }

        [McpServerTool(Name = "web8_translate_outbound"), Description("WEB8 protocol bridge: translates a MeshMessage's payload back into a target external wire format (Json, FormUrlEncoded or PlainText).")]
        public static string TranslateOutbound(MeshMessage message, string targetFormat)
        {
            return _bridge.TranslateOutbound(message, Enum.Parse<BridgeFormat>(targetFormat, true));
        }
    }
}
