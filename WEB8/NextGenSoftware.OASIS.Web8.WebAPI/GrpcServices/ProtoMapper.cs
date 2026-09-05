using System;
using System.Linq;
using Google.Protobuf.WellKnownTypes;
using NextGenSoftware.OASIS.Web8.Core.Enums;
using NextGenSoftware.OASIS.Web8.Core.Models;
using NextGenSoftware.OASIS.Web8.WebAPI.Grpc;

namespace NextGenSoftware.OASIS.Web8.WebAPI.GrpcServices
{
    internal static class ProtoMapper
    {
        // ─── GalacticNode ────────────────────────────────────────────────────

        internal static GalacticNode ToNode(GalacticNodeProto p)
        {
            if (p == null) return null;
            System.Enum.TryParse<NextGenSoftware.OASIS.Web8.Core.Enums.NodeType>(p.Type, true, out var nodeType);
            return new GalacticNode
            {
                Id           = Guid.TryParse(p.Id, out var id) ? id : Guid.Empty,
                Name         = p.Name,
                Type         = nodeType,
                EndpointUrl  = p.EndpointUrl,
                IsSovereign  = p.IsSovereign,
                RegisteredUtc = p.RegisteredUtc?.ToDateTime() ?? DateTime.UtcNow,
                LastSeenUtc   = p.LastSeenUtc?.ToDateTime()   ?? DateTime.UtcNow,
            };
        }

        internal static GalacticNodeProto FromNode(GalacticNode n)
        {
            if (n == null) return null;
            return new GalacticNodeProto
            {
                Id            = n.Id.ToString(),
                Name          = n.Name ?? string.Empty,
                Type          = n.Type.ToString(),
                EndpointUrl   = n.EndpointUrl ?? string.Empty,
                IsSovereign   = n.IsSovereign,
                RegisteredUtc = Timestamp.FromDateTime(DateTime.SpecifyKind(n.RegisteredUtc, DateTimeKind.Utc)),
                LastSeenUtc   = Timestamp.FromDateTime(DateTime.SpecifyKind(n.LastSeenUtc, DateTimeKind.Utc)),
            };
        }

        // ─── MeshMessage ─────────────────────────────────────────────────────

        internal static MeshMessage ToMessage(MeshMessageProto p)
        {
            if (p == null) return null;
            return new MeshMessage
            {
                Id                = Guid.TryParse(p.Id, out var id) ? id : Guid.NewGuid(),
                SourceNodeId      = Guid.TryParse(p.SourceNodeId, out var src) ? src : Guid.Empty,
                DestinationNodeId = Guid.TryParse(p.DestinationNodeId, out var dst) ? dst : Guid.Empty,
                Payload           = p.Payload,
                Ttl               = p.Ttl,
            };
        }

        internal static MeshMessageProto FromMessage(MeshMessage m)
        {
            if (m == null) return null;
            return new MeshMessageProto
            {
                Id                = m.Id.ToString(),
                SourceNodeId      = m.SourceNodeId.ToString(),
                DestinationNodeId = m.DestinationNodeId.ToString(),
                Payload           = m.Payload ?? string.Empty,
                Ttl               = m.Ttl,
            };
        }

        // ─── MeshRouteResult ─────────────────────────────────────────────────

        internal static MeshRouteResultProto FromRouteResult(MeshRouteResult r)
        {
            if (r == null) return null;
            var proto = new MeshRouteResultProto
            {
                MessageId      = r.MessageId.ToString(),
                TotalLatencyMs = r.TotalLatencyMs,
                Delivered      = r.Delivered,
            };
            if (r.Path != null)
                proto.Path.AddRange(r.Path.Select(g => g.ToString()));
            if (r.RelayLog != null)
                proto.RelayLog.AddRange(r.RelayLog);
            return proto;
        }
    }
}
