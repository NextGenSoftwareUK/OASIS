using System;
using System.Linq;
using System.Threading.Tasks;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using NextGenSoftware.OASIS.Web7.Core.Enums;
using NextGenSoftware.OASIS.Web7.Core.Managers;
using NextGenSoftware.OASIS.Web7.Core.Models;

namespace NextGenSoftware.OASIS.Web7.WebAPI.Grpc
{
    public sealed class SymbiosisGrpcService : SymbiosisService.SymbiosisServiceBase
    {
        public override async Task<StartSessionResponse> StartSession(StartSessionRequest request, ServerCallContext context)
        {
            try
            {
                if (!Guid.TryParse(request.AvatarId, out Guid avatarId))
                    return new StartSessionResponse { IsError = true, Message = "Invalid avatar_id GUID." };

                var retention = RetentionMode.Ephemeral;
                if (!string.IsNullOrWhiteSpace(request.Retention))
                    System.Enum.TryParse<RetentionMode>(request.Retention, true, out retention);

                var manager = new SymbiosisSessionManager(avatarId);
                var result  = await manager.StartSessionAsync(avatarId, request.ConsentGranted, retention);

                if (result.IsError)
                    return new StartSessionResponse { IsError = true, Message = result.Message };

                return new StartSessionResponse
                {
                    IsError = false,
                    Message = result.Message ?? string.Empty,
                    Session = MapSession(result.Result)
                };
            }
            catch (Exception ex)
            {
                return new StartSessionResponse { IsError = true, Message = ex.Message };
            }
        }

        public override async Task<GetSessionResponse> GetSession(GetSessionRequest request, ServerCallContext context)
        {
            try
            {
                if (!Guid.TryParse(request.AvatarId,  out Guid avatarId)  ||
                    !Guid.TryParse(request.SessionId, out Guid sessionId))
                    return new GetSessionResponse { IsError = true, Message = "One or more GUIDs are invalid." };

                var manager = new SymbiosisSessionManager(avatarId);
                var result  = await manager.GetSessionAsync(sessionId);

                if (result.IsError)
                    return new GetSessionResponse { IsError = true, Message = result.Message };

                return new GetSessionResponse
                {
                    IsError = false,
                    Message = result.Message ?? string.Empty,
                    Session = MapSession(result.Result)
                };
            }
            catch (Exception ex)
            {
                return new GetSessionResponse { IsError = true, Message = ex.Message };
            }
        }

        public override async Task<SubmitSignalsResponse> SubmitSignals(SubmitSignalsRequest request, ServerCallContext context)
        {
            try
            {
                if (!Guid.TryParse(request.AvatarId,  out Guid avatarId)  ||
                    !Guid.TryParse(request.SessionId, out Guid sessionId))
                    return new SubmitSignalsResponse { IsError = true, Message = "One or more GUIDs are invalid." };

                var samples = request.Samples.Select(s =>
                {
                    BioSignalType signalType = BioSignalType.EEG;
                    if (!string.IsNullOrWhiteSpace(s.SignalType))
                        System.Enum.TryParse<BioSignalType>(s.SignalType, true, out signalType);

                    return new BioSignalSample
                    {
                        SignalType   = signalType,
                        Channel      = s.Channel,
                        SampleRateHz = s.SampleRateHz,
                        Values       = s.Values.ToList(),
                        CapturedUtc  = s.CapturedUtc?.ToDateTime() ?? DateTime.UtcNow
                    };
                }).ToList();

                var manager = new SymbiosisSessionManager(avatarId);
                var result  = await manager.SubmitSignalsAsync(sessionId, samples);

                if (result.IsError)
                    return new SubmitSignalsResponse { IsError = true, Message = result.Message };

                return new SubmitSignalsResponse
                {
                    IsError        = false,
                    Message        = result.Message ?? string.Empty,
                    IntentionState = CollectiveConsciousnessGrpcService.MapIntentionState(result.Result)
                };
            }
            catch (Exception ex)
            {
                return new SubmitSignalsResponse { IsError = true, Message = ex.Message };
            }
        }

        public override async Task<EndSessionResponse> EndSession(EndSessionRequest request, ServerCallContext context)
        {
            try
            {
                if (!Guid.TryParse(request.AvatarId,  out Guid avatarId)  ||
                    !Guid.TryParse(request.SessionId, out Guid sessionId))
                    return new EndSessionResponse { IsError = true, Message = "One or more GUIDs are invalid." };

                var manager = new SymbiosisSessionManager(avatarId);
                var result  = await manager.EndSessionAsync(sessionId);

                if (result.IsError)
                    return new EndSessionResponse { IsError = true, Message = result.Message };

                return new EndSessionResponse
                {
                    IsError = false,
                    Message = result.Message ?? string.Empty,
                    Result  = result.Result
                };
            }
            catch (Exception ex)
            {
                return new EndSessionResponse { IsError = true, Message = ex.Message };
            }
        }

        // ── helpers ──────────────────────────────────────────────────────────

        private static SymbiosisSessionProto MapSession(SymbiosisSession src)
        {
            if (src == null) return null;

            var proto = new SymbiosisSessionProto
            {
                Id             = src.Id.ToString(),
                AvatarId       = src.AvatarId.ToString(),
                ConsentGranted = src.ConsentGranted,
                IsActive       = src.IsActive,
                Retention      = src.Retention.ToString(),
                StartedUtc     = Timestamp.FromDateTime(DateTime.SpecifyKind(src.StartedUtc, DateTimeKind.Utc))
            };

            if (src.EndedUtc.HasValue)
                proto.EndedUtc = Timestamp.FromDateTime(DateTime.SpecifyKind(src.EndedUtc.Value, DateTimeKind.Utc));

            if (src.LastIntentionState != null)
                proto.LastIntentionState = CollectiveConsciousnessGrpcService.MapIntentionState(src.LastIntentionState);

            if (src.AuditLog != null)
                proto.AuditLog.AddRange(src.AuditLog);

            return proto;
        }
    }
}
