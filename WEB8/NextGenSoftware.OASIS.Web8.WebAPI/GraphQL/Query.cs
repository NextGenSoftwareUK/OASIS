using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.Web8.Core.Managers;
using NextGenSoftware.OASIS.Web8.Core.Models;

namespace NextGenSoftware.OASIS.Web8.WebAPI.GraphQL
{
    /// <summary>Root Query type for WEB8 mesh networking GraphQL queries.</summary>
    public class Query
    {
        /// <summary>Returns all registered nodes in the galactic mesh.</summary>
        public async Task<List<GalacticNode>> GetNodesAsync()
        {
            var manager = new GalacticMeshManager(Guid.Empty);
            var result = await manager.GetNodesAsync();
            return result.IsError ? new List<GalacticNode>() : result.Result ?? new List<GalacticNode>();
        }

        /// <summary>Computes the shortest-path route between two mesh nodes using Dijkstra with self-healing failover.</summary>
        public async Task<List<Guid>> GetRouteAsync(Guid sourceNodeId, Guid destinationNodeId)
        {
            var manager = new GalacticMeshManager(Guid.Empty);
            var result = await manager.ComputeRouteAsync(sourceNodeId, destinationNodeId);
            return result.IsError ? new List<Guid>() : result.Result ?? new List<Guid>();
        }

        /// <summary>Returns the current mesh topology — all nodes and their connectivity metadata.</summary>
        public async Task<MeshTopology> GetMeshTopologyAsync()
        {
            var manager = new GalacticMeshManager(Guid.Empty);
            var nodesResult = await manager.GetNodesAsync();
            return new MeshTopology
            {
                Nodes = nodesResult.IsError ? new List<GalacticNode>() : nodesResult.Result ?? new List<GalacticNode>(),
                NodeCount = nodesResult.IsError ? 0 : nodesResult.Result?.Count ?? 0
            };
        }
    }

    /// <summary>Snapshot of mesh topology returned by the GetMeshTopology query.</summary>
    public class MeshTopology
    {
        public List<GalacticNode> Nodes { get; set; } = new List<GalacticNode>();
        public int NodeCount { get; set; }
    }
}
