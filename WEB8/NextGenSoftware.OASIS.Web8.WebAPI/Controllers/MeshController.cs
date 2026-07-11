using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NextGenSoftware.OASIS.Web8.Core.Managers;
using NextGenSoftware.OASIS.Web8.Core.Models;

namespace NextGenSoftware.OASIS.Web8.WebAPI.Controllers
{
    /// <summary>The WEB8 fractal holonic mesh - register nodes, declare links, compute routes and relay messages with self-healing failover.</summary>
    [ApiController]
    [Route("v1/mesh")]
    public class MeshController : Web8ControllerBase
    {
        [HttpPost("nodes")]
        [ProducesResponseType(typeof(GalacticNode), StatusCodes.Status200OK)]
        public async Task<IActionResult> RegisterNode([FromBody] GalacticNode node)
        {
            GalacticMeshManager manager = new GalacticMeshManager(AvatarId);
            var result = await manager.RegisterNodeAsync(node);
            return result.IsError ? BadRequest(result) : Ok(result);
        }

        [HttpGet("nodes")]
        [ProducesResponseType(typeof(System.Collections.Generic.List<GalacticNode>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetNodes()
        {
            GalacticMeshManager manager = new GalacticMeshManager(AvatarId);
            var result = await manager.GetNodesAsync();
            return result.IsError ? BadRequest(result) : Ok(result);
        }

        [HttpPost("links")]
        [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
        public async Task<IActionResult> AddLink([FromQuery] Guid nodeAId, [FromQuery] Guid nodeBId, [FromQuery] double latencyMs = 50)
        {
            GalacticMeshManager manager = new GalacticMeshManager(AvatarId);
            var result = await manager.AddLinkAsync(nodeAId, nodeBId, latencyMs);
            return result.IsError ? BadRequest(result) : Ok(result);
        }

        [HttpPost("nodes/{nodeId}/heartbeat")]
        [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
        public async Task<IActionResult> Heartbeat(Guid nodeId)
        {
            GalacticMeshManager manager = new GalacticMeshManager(AvatarId);
            var result = await manager.HeartbeatAsync(nodeId);
            return result.IsError ? BadRequest(result) : Ok(result);
        }

        [HttpGet("route")]
        [ProducesResponseType(typeof(System.Collections.Generic.List<Guid>), StatusCodes.Status200OK)]
        public async Task<IActionResult> ComputeRoute([FromQuery] Guid sourceNodeId, [FromQuery] Guid destinationNodeId)
        {
            GalacticMeshManager manager = new GalacticMeshManager(AvatarId);
            var result = await manager.ComputeRouteAsync(sourceNodeId, destinationNodeId);
            return result.IsError ? BadRequest(result) : Ok(result);
        }

        /// <summary>Routes and relays a message hop-by-hop to its destination, self-healing around any failed/stale node.</summary>
        [HttpPost("send")]
        [ProducesResponseType(typeof(MeshRouteResult), StatusCodes.Status200OK)]
        public async Task<IActionResult> SendMessage([FromBody] MeshMessage message)
        {
            GalacticMeshManager manager = new GalacticMeshManager(AvatarId);
            var result = await manager.SendMessageAsync(message);
            return result.IsError ? BadRequest(result) : Ok(result);
        }
    }
}
