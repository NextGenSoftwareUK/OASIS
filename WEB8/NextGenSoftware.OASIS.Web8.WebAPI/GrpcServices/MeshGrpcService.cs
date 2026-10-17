using System;
using System.Linq;
using System.Threading.Tasks;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using NextGenSoftware.OASIS.Web8.Core.Enums;
using NextGenSoftware.OASIS.Web8.Core.Managers;
using NextGenSoftware.OASIS.Web8.Core.Models;
using NextGenSoftware.OASIS.Web8.WebAPI.Grpc;

namespace NextGenSoftware.OASIS.Web8.WebAPI.GrpcServices
{
    public class MeshGrpcService : MeshService.MeshServiceBase
    {
        public override async Task<RegisterNodeResponse> RegisterNode(RegisterNodeRequest request, ServerCallContext context)
        {
            try
            {
                Guid avatarId = Guid.TryParse(request.AvatarId, out Guid aid) ? aid : Guid.Empty;
                GalacticNode node = ProtoMapper.ToNode(request.Node);
                var manager = new GalacticMeshManager(avatarId);
                var result = await manager.RegisterNodeAsync(node);
                if (result.IsError)
                    return new RegisterNodeResponse { IsError = true, Message = result.Message ?? string.Empty };
                return new RegisterNodeResponse { Node = ProtoMapper.FromNode(result.Result) };
            }
            catch (Exception ex)
            {
                return new RegisterNodeResponse { IsError = true, Message = ex.Message };
            }
        }

        public override async Task<GetNodesResponse> GetNodes(GetNodesRequest request, ServerCallContext context)
        {
            try
            {
                Guid avatarId = Guid.TryParse(request.AvatarId, out Guid aid) ? aid : Guid.Empty;
                var manager = new GalacticMeshManager(avatarId);
                var result = await manager.GetNodesAsync();
                if (result.IsError)
                    return new GetNodesResponse { IsError = true, Message = result.Message ?? string.Empty };
                var response = new GetNodesResponse();
                if (result.Result != null)
                    response.Nodes.AddRange(result.Result.Select(ProtoMapper.FromNode));
                return response;
            }
            catch (Exception ex)
            {
                return new GetNodesResponse { IsError = true, Message = ex.Message };
            }
        }

        public override async Task<AddLinkResponse> AddLink(AddLinkRequest request, ServerCallContext context)
        {
            try
            {
                Guid avatarId = Guid.TryParse(request.AvatarId, out Guid aid) ? aid : Guid.Empty;
                Guid nodeAId = Guid.Parse(request.NodeAId);
                Guid nodeBId = Guid.Parse(request.NodeBId);
                var manager = new GalacticMeshManager(avatarId);
                var result = await manager.AddLinkAsync(nodeAId, nodeBId, request.LatencyMs);
                if (result.IsError)
                    return new AddLinkResponse { IsError = true, Message = result.Message ?? string.Empty };
                return new AddLinkResponse { Result = result.Result };
            }
            catch (Exception ex)
            {
                return new AddLinkResponse { IsError = true, Message = ex.Message };
            }
        }

        public override async Task<HeartbeatResponse> Heartbeat(HeartbeatRequest request, ServerCallContext context)
        {
            try
            {
                Guid avatarId = Guid.TryParse(request.AvatarId, out Guid aid) ? aid : Guid.Empty;
                Guid nodeId = Guid.Parse(request.NodeId);
                var manager = new GalacticMeshManager(avatarId);
                var result = await manager.HeartbeatAsync(nodeId);
                if (result.IsError)
                    return new HeartbeatResponse { IsError = true, Message = result.Message ?? string.Empty };
                return new HeartbeatResponse { Result = result.Result };
            }
            catch (Exception ex)
            {
                return new HeartbeatResponse { IsError = true, Message = ex.Message };
            }
        }

        public override async Task<ComputeRouteResponse> ComputeRoute(ComputeRouteRequest request, ServerCallContext context)
        {
            try
            {
                Guid avatarId = Guid.TryParse(request.AvatarId, out Guid aid) ? aid : Guid.Empty;
                Guid sourceNodeId = Guid.Parse(request.SourceNodeId);
                Guid destinationNodeId = Guid.Parse(request.DestinationNodeId);
                var manager = new GalacticMeshManager(avatarId);
                var result = await manager.ComputeRouteAsync(sourceNodeId, destinationNodeId);
                if (result.IsError)
                    return new ComputeRouteResponse { IsError = true, Message = result.Message ?? string.Empty };
                var response = new ComputeRouteResponse();
                if (result.Result != null)
                    response.NodeIds.AddRange(result.Result.Select(g => g.ToString()));
                return response;
            }
            catch (Exception ex)
            {
                return new ComputeRouteResponse { IsError = true, Message = ex.Message };
            }
        }

        public override async Task<SendMeshMessageResponse> SendMeshMessage(SendMeshMessageRequest request, ServerCallContext context)
        {
            try
            {
                Guid avatarId = Guid.TryParse(request.AvatarId, out Guid aid) ? aid : Guid.Empty;
                MeshMessage message = ProtoMapper.ToMessage(request.MeshMessage);
                var manager = new GalacticMeshManager(avatarId);
                var result = await manager.SendMessageAsync(message);
                if (result.IsError)
                    return new SendMeshMessageResponse { IsError = true, Message = result.Message ?? string.Empty };
                return new SendMeshMessageResponse { Result = ProtoMapper.FromRouteResult(result.Result) };
            }
            catch (Exception ex)
            {
                return new SendMeshMessageResponse { IsError = true, Message = ex.Message };
            }
        }
    }
}
