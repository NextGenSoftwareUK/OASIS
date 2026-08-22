using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Grpc.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.ONODE.Core.Managers;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Grpc;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.GrpcServices
{
    public class ChatGrpcService : ChatService.ChatServiceBase
    {
        public override async Task<JsonResponse> GetActiveChatSessions(GetActiveChatSessionsRequest request, ServerCallContext context)
        {
            try
            {
                if (!Guid.TryParse(request.AvatarId, out var avatarId))
                    return new JsonResponse { IsError = true, Message = "Invalid avatar ID." };
                var result = await ChatManager.Instance.GetActiveSessionsAsync(avatarId);
                return result.IsError
                    ? new JsonResponse { IsError = true, Message = result.Message }
                    : new JsonResponse { Json = JsonSerializer.Serialize(result.Result) };
            }
            catch (Exception ex) { return new JsonResponse { IsError = true, Message = ex.Message }; }
        }

        public override async Task<JsonResponse> GetChatHistory(GetChatHistoryRequest request, ServerCallContext context)
        {
            try
            {
                var result = await ChatManager.Instance.GetChatHistoryAsync(request.SessionId, request.Limit, request.Offset);
                return result.IsError
                    ? new JsonResponse { IsError = true, Message = result.Message }
                    : new JsonResponse { Json = JsonSerializer.Serialize(result.Result) };
            }
            catch (Exception ex) { return new JsonResponse { IsError = true, Message = ex.Message }; }
        }

        public override async Task<JsonResponse> GetChatStats(GetChatStatsRequest request, ServerCallContext context)
        {
            try
            {
                if (!Guid.TryParse(request.AvatarId, out var avatarId))
                    return new JsonResponse { IsError = true, Message = "Invalid avatar ID." };
                var result = await ChatManager.Instance.GetChatStatsAsync(avatarId);
                return result.IsError
                    ? new JsonResponse { IsError = true, Message = result.Message }
                    : new JsonResponse { Json = JsonSerializer.Serialize(result.Result) };
            }
            catch (Exception ex) { return new JsonResponse { IsError = true, Message = ex.Message }; }
        }

        public override async Task<StringResponse> StartChatSession(StartChatSessionRequest request, ServerCallContext context)
        {
            try
            {
                var participantIds = new List<Guid>();
                foreach (var idStr in request.ParticipantIds)
                    if (Guid.TryParse(idStr, out var g)) participantIds.Add(g);
                var sessionName = string.IsNullOrWhiteSpace(request.SessionName) ? null : request.SessionName;
                var result = await ChatManager.Instance.StartNewChatSessionAsync(participantIds, sessionName);
                return result.IsError
                    ? new StringResponse { IsError = true, Message = result.Message }
                    : new StringResponse { Value = result.Result ?? string.Empty };
            }
            catch (Exception ex) { return new StringResponse { IsError = true, Message = ex.Message }; }
        }

        public override async Task<OASISGrpcResponse> SendChatMessage(SendChatMessageRequest request, ServerCallContext context)
        {
            try
            {
                if (!Guid.TryParse(request.SenderId, out var senderId))
                    return new OASISGrpcResponse { IsError = true, Message = "Invalid sender ID." };
                var result = await ChatManager.Instance.SendMessageAsync(request.SessionId, senderId, request.Message);
                return result.IsError ? new OASISGrpcResponse { IsError = true, Message = result.Message } : new OASISGrpcResponse();
            }
            catch (Exception ex) { return new OASISGrpcResponse { IsError = true, Message = ex.Message }; }
        }

        public override async Task<OASISGrpcResponse> EndChatSession(EndChatSessionRequest request, ServerCallContext context)
        {
            try
            {
                if (!Guid.TryParse(request.EndedById, out var endedById))
                    return new OASISGrpcResponse { IsError = true, Message = "Invalid ended-by ID." };
                var result = await ChatManager.Instance.EndChatSessionAsync(request.SessionId, endedById);
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
