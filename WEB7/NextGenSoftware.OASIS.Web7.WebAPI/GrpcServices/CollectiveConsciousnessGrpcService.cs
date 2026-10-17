using System;
using System.Linq;
using System.Threading.Tasks;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using NextGenSoftware.OASIS.Web7.Core.Managers;
using NextGenSoftware.OASIS.Web7.Core.Models;

namespace NextGenSoftware.OASIS.Web7.WebAPI.Grpc
{
    public sealed class CollectiveConsciousnessGrpcService : CollectiveConsciousnessService.CollectiveConsciousnessServiceBase
    {
        public override async Task<CreateSpaceResponse> CreateSpace(CreateSpaceRequest request, ServerCallContext context)
        {
            try
            {
                if (!Guid.TryParse(request.AvatarId, out Guid avatarId))
                    return new CreateSpaceResponse { IsError = true, Message = "Invalid avatar_id GUID." };

                var manager = new CollectiveConsciousnessManager(avatarId);
                var result = await manager.CreateSpaceAsync(request.Name);

                if (result.IsError)
                    return new CreateSpaceResponse { IsError = true, Message = result.Message };

                return new CreateSpaceResponse
                {
                    IsError = false,
                    Message = result.Message ?? string.Empty,
                    Space   = MapSpace(result.Result)
                };
            }
            catch (Exception ex)
            {
                return new CreateSpaceResponse { IsError = true, Message = ex.Message };
            }
        }

        public override async Task<JoinSpaceResponse> JoinSpace(JoinSpaceRequest request, ServerCallContext context)
        {
            try
            {
                if (!Guid.TryParse(request.AvatarId,  out Guid avatarId)  ||
                    !Guid.TryParse(request.SpaceId,   out Guid spaceId)   ||
                    !Guid.TryParse(request.SessionId, out Guid sessionId))
                    return new JoinSpaceResponse { IsError = true, Message = "One or more GUIDs are invalid." };

                var manager = new CollectiveConsciousnessManager(avatarId);
                var result  = await manager.JoinSpaceAsync(spaceId, sessionId);

                if (result.IsError)
                    return new JoinSpaceResponse { IsError = true, Message = result.Message };

                return new JoinSpaceResponse
                {
                    IsError = false,
                    Message = result.Message ?? string.Empty,
                    Result  = result.Result
                };
            }
            catch (Exception ex)
            {
                return new JoinSpaceResponse { IsError = true, Message = ex.Message };
            }
        }

        public override async Task<GetAggregateFieldResponse> GetAggregateField(GetAggregateFieldRequest request, ServerCallContext context)
        {
            try
            {
                if (!Guid.TryParse(request.AvatarId, out Guid avatarId) ||
                    !Guid.TryParse(request.SpaceId,  out Guid spaceId))
                    return new GetAggregateFieldResponse { IsError = true, Message = "One or more GUIDs are invalid." };

                var manager = new CollectiveConsciousnessManager(avatarId);
                var result  = await manager.GetAggregateFieldAsync(spaceId);

                if (result.IsError)
                    return new GetAggregateFieldResponse { IsError = true, Message = result.Message };

                return new GetAggregateFieldResponse
                {
                    IsError = false,
                    Message = result.Message ?? string.Empty,
                    Space   = MapSpace(result.Result)
                };
            }
            catch (Exception ex)
            {
                return new GetAggregateFieldResponse { IsError = true, Message = ex.Message };
            }
        }

        // ── helpers ──────────────────────────────────────────────────────────

        private static CollectiveConsciousnessSpaceProto MapSpace(CollectiveConsciousnessSpace src)
        {
            if (src == null) return null;

            var proto = new CollectiveConsciousnessSpaceProto
            {
                Id         = src.Id.ToString(),
                Name       = src.Name ?? string.Empty,
                CreatedUtc = Timestamp.FromDateTime(DateTime.SpecifyKind(src.CreatedUtc, DateTimeKind.Utc))
            };

            if (src.ParticipantSessionIds != null)
                proto.ParticipantSessionIds.AddRange(src.ParticipantSessionIds.Select(g => g.ToString()));

            if (src.AggregateState != null)
                proto.AggregateState = MapIntentionState(src.AggregateState);

            return proto;
        }

        internal static IntentionStateProto MapIntentionState(NextGenSoftware.OASIS.Web7.Core.Models.IntentionState src)
        {
            if (src == null) return null;

            var proto = new IntentionStateProto
            {
                Focus            = src.Focus,
                EmotionalValence = src.EmotionalValence,
                Arousal          = src.Arousal,
                CognitiveLoad    = src.CognitiveLoad,
                ComputedUtc      = Timestamp.FromDateTime(DateTime.SpecifyKind(src.ComputedUtc, DateTimeKind.Utc))
            };

            if (src.Features != null)
                foreach (var kv in src.Features)
                    proto.Features[kv.Key] = kv.Value;

            return proto;
        }
    }
}
