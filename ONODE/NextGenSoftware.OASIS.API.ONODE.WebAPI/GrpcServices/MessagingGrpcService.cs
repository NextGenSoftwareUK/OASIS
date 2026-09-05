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
    public class MessagingGrpcService : MessagingService.MessagingServiceBase
    {
        public override async Task<JsonResponse> GetMessages(GetMessagesRequest request, ServerCallContext context)
        {
            try
            {
                if (!Guid.TryParse(request.AvatarId, out var avatarId))
                    return new JsonResponse { IsError = true, Message = "Invalid avatar ID." };
                var result = await MessagingManager.Instance.GetMessagesAsync(avatarId, request.Limit, request.Offset);
                return result.IsError
                    ? new JsonResponse { IsError = true, Message = result.Message }
                    : new JsonResponse { Json = JsonSerializer.Serialize(result.Result) };
            }
            catch (Exception ex) { return new JsonResponse { IsError = true, Message = ex.Message }; }
        }

        public override async Task<JsonResponse> GetConversation(GetConversationRequest request, ServerCallContext context)
        {
            try
            {
                if (!Guid.TryParse(request.AvatarId, out var avatarId)) return new JsonResponse { IsError = true, Message = "Invalid avatar ID." };
                if (!Guid.TryParse(request.OtherAvatarId, out var otherAvatarId)) return new JsonResponse { IsError = true, Message = "Invalid other avatar ID." };
                var result = await MessagingManager.Instance.GetConversationAsync(avatarId, otherAvatarId, request.Limit, request.Offset);
                return result.IsError
                    ? new JsonResponse { IsError = true, Message = result.Message }
                    : new JsonResponse { Json = JsonSerializer.Serialize(result.Result) };
            }
            catch (Exception ex) { return new JsonResponse { IsError = true, Message = ex.Message }; }
        }

        public override async Task<JsonResponse> GetNotifications(GetNotificationsRequest request, ServerCallContext context)
        {
            try
            {
                if (!Guid.TryParse(request.AvatarId, out var avatarId))
                    return new JsonResponse { IsError = true, Message = "Invalid avatar ID." };
                var result = await MessagingManager.Instance.GetNotificationsAsync(avatarId, request.Limit, request.Offset);
                return result.IsError
                    ? new JsonResponse { IsError = true, Message = result.Message }
                    : new JsonResponse { Json = JsonSerializer.Serialize(result.Result) };
            }
            catch (Exception ex) { return new JsonResponse { IsError = true, Message = ex.Message }; }
        }

        public override async Task<OASISGrpcResponse> SendMessage(SendMessageRequest request, ServerCallContext context)
        {
            try
            {
                if (!Guid.TryParse(request.FromAvatarId, out var fromId)) return new OASISGrpcResponse { IsError = true, Message = "Invalid from avatar ID." };
                if (!Guid.TryParse(request.ToAvatarId, out var toId)) return new OASISGrpcResponse { IsError = true, Message = "Invalid to avatar ID." };
                var result = await MessagingManager.Instance.SendMessageToAvatarAsync(fromId, toId, request.Content);
                return result.IsError ? new OASISGrpcResponse { IsError = true, Message = result.Message } : new OASISGrpcResponse();
            }
            catch (Exception ex) { return new OASISGrpcResponse { IsError = true, Message = ex.Message }; }
        }

        public override async Task<OASISGrpcResponse> MarkMessagesAsRead(MarkMessagesAsReadRequest request, ServerCallContext context)
        {
            try
            {
                if (!Guid.TryParse(request.AvatarId, out var avatarId)) return new OASISGrpcResponse { IsError = true, Message = "Invalid avatar ID." };
                var messageIds = new List<Guid>();
                foreach (var idStr in request.MessageIds)
                    if (Guid.TryParse(idStr, out var g)) messageIds.Add(g);
                var result = await MessagingManager.Instance.MarkMessagesAsReadAsync(avatarId, messageIds);
                return result.IsError ? new OASISGrpcResponse { IsError = true, Message = result.Message } : new OASISGrpcResponse();
            }
            catch (Exception ex) { return new OASISGrpcResponse { IsError = true, Message = ex.Message }; }
        }

        public override async Task<OASISGrpcResponse> MarkNotificationsAsRead(MarkNotificationsAsReadRequest request, ServerCallContext context)
        {
            try
            {
                if (!Guid.TryParse(request.AvatarId, out var avatarId)) return new OASISGrpcResponse { IsError = true, Message = "Invalid avatar ID." };
                var notificationIds = new List<Guid>();
                foreach (var idStr in request.NotificationIds)
                    if (Guid.TryParse(idStr, out var g)) notificationIds.Add(g);
                var result = await MessagingManager.Instance.MarkNotificationsAsReadAsync(avatarId, notificationIds);
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
