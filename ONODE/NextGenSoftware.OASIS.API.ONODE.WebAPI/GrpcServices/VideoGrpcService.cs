using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Grpc.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.ONODE.Core.Managers;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Grpc;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.GrpcServices
{
    public class VideoGrpcService : VideoService.VideoServiceBase
    {
        public override async Task<JsonResponse> GetActiveCalls(GetActiveCallsRequest request, ServerCallContext context)
        {
            try
            {
                if (!Guid.TryParse(request.AvatarId, out var avatarId))
                    return new JsonResponse { IsError = true, Message = "Invalid avatar ID." };
                var result = await VideoManager.Instance.GetActiveCallsAsync(avatarId);
                return result.IsError
                    ? new JsonResponse { IsError = true, Message = result.Message }
                    : new JsonResponse { Json = JsonSerializer.Serialize(result.Result) };
            }
            catch (Exception ex) { return new JsonResponse { IsError = true, Message = ex.Message }; }
        }

        public override async Task<JsonResponse> GetCallParticipants(GetCallParticipantsRequest request, ServerCallContext context)
        {
            try
            {
                var result = await VideoManager.Instance.GetCallParticipantsAsync(request.CallId);
                return result.IsError
                    ? new JsonResponse { IsError = true, Message = result.Message }
                    : new JsonResponse { Json = JsonSerializer.Serialize(result.Result) };
            }
            catch (Exception ex) { return new JsonResponse { IsError = true, Message = ex.Message }; }
        }

        public override async Task<StringResponse> StartVideoCall(StartVideoCallRequest request, ServerCallContext context)
        {
            try
            {
                if (!Guid.TryParse(request.InitiatorId, out var initiatorId))
                    return new StringResponse { IsError = true, Message = "Invalid initiator ID." };
                var participantIds = new List<Guid>();
                foreach (var idStr in request.ParticipantIds)
                    if (Guid.TryParse(idStr, out var g)) participantIds.Add(g);
                var callName = string.IsNullOrWhiteSpace(request.CallName) ? null : request.CallName;
                var result = await VideoManager.Instance.StartVideoCallAsync(initiatorId, participantIds, callName: callName);
                return result.IsError
                    ? new StringResponse { IsError = true, Message = result.Message }
                    : new StringResponse { Value = result.Result ?? string.Empty };
            }
            catch (Exception ex) { return new StringResponse { IsError = true, Message = ex.Message }; }
        }

        public override async Task<OASISGrpcResponse> JoinVideoCall(JoinVideoCallRequest request, ServerCallContext context)
        {
            try
            {
                if (!Guid.TryParse(request.ParticipantId, out var participantId))
                    return new OASISGrpcResponse { IsError = true, Message = "Invalid participant ID." };
                var result = await VideoManager.Instance.JoinVideoCallAsync(request.CallId, participantId);
                return result.IsError ? new OASISGrpcResponse { IsError = true, Message = result.Message } : new OASISGrpcResponse();
            }
            catch (Exception ex) { return new OASISGrpcResponse { IsError = true, Message = ex.Message }; }
        }

        public override async Task<OASISGrpcResponse> EndVideoCall(EndVideoCallRequest request, ServerCallContext context)
        {
            try
            {
                if (!Guid.TryParse(request.EndedById, out var endedById))
                    return new OASISGrpcResponse { IsError = true, Message = "Invalid ended-by ID." };
                var result = await VideoManager.Instance.EndVideoCallAsync(request.CallId, endedById);
                return result.IsError ? new OASISGrpcResponse { IsError = true, Message = result.Message } : new OASISGrpcResponse();
            }
            catch (Exception ex) { return new OASISGrpcResponse { IsError = true, Message = ex.Message }; }
        }

        public override async Task<OASISGrpcResponse> LeaveVideoCall(LeaveVideoCallRequest request, ServerCallContext context)
        {
            try
            {
                if (!Guid.TryParse(request.ParticipantId, out var participantId))
                    return new OASISGrpcResponse { IsError = true, Message = "Invalid participant ID." };
                var result = await VideoManager.Instance.LeaveVideoCallAsync(request.CallId, participantId);
                return result.IsError ? new OASISGrpcResponse { IsError = true, Message = result.Message } : new OASISGrpcResponse();
            }
            catch (Exception ex) { return new OASISGrpcResponse { IsError = true, Message = ex.Message }; }
        }

        private static async Task ActivateProviderIfSpecifiedAsync(string providerType, bool setGlobally)
        {
            if (string.IsNullOrWhiteSpace(providerType)) return;
            if (!System.Enum.TryParse<ProviderType>(providerType, true, out var pt)) return;
            if (pt == ProviderType.Default || pt == ProviderType.None) return;
            await OASISBootLoader.OASISBootLoader.GetAndActivateStorageProviderAsync(pt, null, false, setGlobally);
        }

        private static T ParseEnum<T>(string value, T defaultValue) where T : struct, System.Enum
        {
            if (string.IsNullOrWhiteSpace(value)) return defaultValue;
            return System.Enum.TryParse<T>(value, true, out var result) ? result : defaultValue;
        }
    }
}
