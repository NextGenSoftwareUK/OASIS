using System;
using System.Threading.Tasks;
using Grpc.Core;
using NextGenSoftware.OASIS.Web8.Core.Enums;
using NextGenSoftware.OASIS.Web8.Core.Managers;
using NextGenSoftware.OASIS.Web8.Core.Models;
using NextGenSoftware.OASIS.Web8.WebAPI.Grpc;

namespace NextGenSoftware.OASIS.Web8.WebAPI.GrpcServices
{
    public class ProtocolBridgeGrpcService : ProtocolBridgeService.ProtocolBridgeServiceBase
    {
        private static readonly ProtocolBridgeManager _bridge = new ProtocolBridgeManager();

        public override Task<TranslateInboundResponse> TranslateInbound(TranslateInboundRequest request, ServerCallContext context)
        {
            try
            {
                if (!System.Enum.TryParse<BridgeFormat>(request.Format, true, out var format))
                    return Task.FromResult(new TranslateInboundResponse { IsError = true, Message = $"Unknown BridgeFormat: {request.Format}" });

                Guid sourceNodeId = Guid.Parse(request.SourceNodeId);
                Guid destinationNodeId = Guid.Parse(request.DestinationNodeId);

                MeshMessage message = _bridge.TranslateInbound(request.RawPayload, format, sourceNodeId, destinationNodeId);
                return Task.FromResult(new TranslateInboundResponse { MeshMessage = ProtoMapper.FromMessage(message) });
            }
            catch (Exception ex)
            {
                return Task.FromResult(new TranslateInboundResponse { IsError = true, Message = ex.Message });
            }
        }

        public override Task<TranslateOutboundResponse> TranslateOutbound(TranslateOutboundRequest request, ServerCallContext context)
        {
            try
            {
                if (!System.Enum.TryParse<BridgeFormat>(request.TargetFormat, true, out var targetFormat))
                    return Task.FromResult(new TranslateOutboundResponse { IsError = true, Message = $"Unknown BridgeFormat: {request.TargetFormat}" });

                MeshMessage message = ProtoMapper.ToMessage(request.MeshMessage);
                string translated = _bridge.TranslateOutbound(message, targetFormat);
                return Task.FromResult(new TranslateOutboundResponse { Translated = translated });
            }
            catch (Exception ex)
            {
                return Task.FromResult(new TranslateOutboundResponse { IsError = true, Message = ex.Message });
            }
        }
    }
}
