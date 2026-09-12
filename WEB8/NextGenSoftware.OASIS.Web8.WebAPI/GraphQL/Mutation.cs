using System;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.Web8.Core.Enums;
using NextGenSoftware.OASIS.Web8.Core.Managers;
using NextGenSoftware.OASIS.Web8.Core.Models;

namespace NextGenSoftware.OASIS.Web8.WebAPI.GraphQL
{
    /// <summary>Root Mutation type for WEB8 mesh networking GraphQL mutations.</summary>
    public class Mutation
    {
        /// <summary>Registers a new node in the galactic mesh.</summary>
        public async Task<GalacticNode> RegisterNodeAsync(GalacticNode node)
        {
            var manager = new GalacticMeshManager(Guid.Empty);
            var result = await manager.RegisterNodeAsync(node);
            return result.IsError ? node : result.Result ?? node;
        }

        /// <summary>Declares a bidirectional link between two mesh nodes with an optional latency value.</summary>
        public async Task<bool> AddLinkAsync(Guid nodeAId, Guid nodeBId, double latencyMs = 50)
        {
            var manager = new GalacticMeshManager(Guid.Empty);
            var result = await manager.AddLinkAsync(nodeAId, nodeBId, latencyMs);
            return !result.IsError && result.Result;
        }

        /// <summary>Records a heartbeat for a node, keeping it active in the mesh routing table.</summary>
        public async Task<bool> HeartbeatAsync(Guid nodeId)
        {
            var manager = new GalacticMeshManager(Guid.Empty);
            var result = await manager.HeartbeatAsync(nodeId);
            return !result.IsError && result.Result;
        }

        /// <summary>Routes and relays a mesh message hop-by-hop to its destination, self-healing around failed nodes.</summary>
        public async Task<MeshRouteResult> SendMeshMessageAsync(MeshMessage message)
        {
            var manager = new GalacticMeshManager(Guid.Empty);
            var result = await manager.SendMessageAsync(message);
            return result.IsError
                ? new MeshRouteResult { MessageId = message.Id, Delivered = false }
                : result.Result ?? new MeshRouteResult { MessageId = message.Id, Delivered = false };
        }

        /// <summary>Translates a raw external payload into the unified WEB8 MeshMessage envelope.</summary>
        public MeshMessage TranslateInboundAsync(string rawPayload, BridgeFormat format, Guid sourceNodeId, Guid destinationNodeId)
        {
            var bridge = new ProtocolBridgeManager();
            return bridge.TranslateInbound(rawPayload, format, sourceNodeId, destinationNodeId);
        }

        /// <summary>Translates a unified WEB8 MeshMessage into a target protocol wire format.</summary>
        public string TranslateOutboundAsync(MeshMessage message, BridgeFormat targetFormat)
        {
            var bridge = new ProtocolBridgeManager();
            return bridge.TranslateOutbound(message, targetFormat);
        }
    }
}
