using System;
using System.Text.Json;
using System.Threading.Tasks;
using Grpc.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.ONODE.Core.Managers;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Grpc;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.GrpcServices
{
    public class GiftsGrpcService : GiftsService.GiftsServiceBase
    {
        public override async Task<JsonResponse> GetAllGifts(GetAllGiftsRequest request, ServerCallContext context)
        {
            try
            {
                if (!Guid.TryParse(request.AvatarId, out var avatarId))
                    return new JsonResponse { IsError = true, Message = "Invalid avatar ID." };
                var result = await GiftsManager.Instance.GetAllGiftsAsync(avatarId);
                return result.IsError
                    ? new JsonResponse { IsError = true, Message = result.Message }
                    : new JsonResponse { Json = JsonSerializer.Serialize(result.Result) };
            }
            catch (Exception ex) { return new JsonResponse { IsError = true, Message = ex.Message }; }
        }

        public override async Task<JsonResponse> GetGiftHistory(GetGiftHistoryRequest request, ServerCallContext context)
        {
            try
            {
                if (!Guid.TryParse(request.AvatarId, out var avatarId))
                    return new JsonResponse { IsError = true, Message = "Invalid avatar ID." };
                var result = await GiftsManager.Instance.GetGiftHistoryAsync(avatarId, request.Limit, request.Offset);
                return result.IsError
                    ? new JsonResponse { IsError = true, Message = result.Message }
                    : new JsonResponse { Json = JsonSerializer.Serialize(result.Result) };
            }
            catch (Exception ex) { return new JsonResponse { IsError = true, Message = ex.Message }; }
        }

        public override async Task<JsonResponse> GetGiftStats(GetGiftStatsRequest request, ServerCallContext context)
        {
            try
            {
                if (!Guid.TryParse(request.AvatarId, out var avatarId))
                    return new JsonResponse { IsError = true, Message = "Invalid avatar ID." };
                var result = await GiftsManager.Instance.GetGiftStatsAsync(avatarId);
                return result.IsError
                    ? new JsonResponse { IsError = true, Message = result.Message }
                    : new JsonResponse { Json = JsonSerializer.Serialize(result.Result) };
            }
            catch (Exception ex) { return new JsonResponse { IsError = true, Message = ex.Message }; }
        }

        public override async Task<JsonResponse> SendGift(SendGiftRequest request, ServerCallContext context)
        {
            try
            {
                if (!Guid.TryParse(request.FromAvatarId, out var fromId)) return new JsonResponse { IsError = true, Message = "Invalid from avatar ID." };
                if (!Guid.TryParse(request.ToAvatarId, out var toId)) return new JsonResponse { IsError = true, Message = "Invalid to avatar ID." };
                var giftType = ParseEnum(request.GiftType, GiftType.Karma);
                var message = string.IsNullOrWhiteSpace(request.Message) ? null : request.Message;
                var result = await GiftsManager.Instance.SendGiftAsync(fromId, toId, giftType, message);
                return result.IsError
                    ? new JsonResponse { IsError = true, Message = result.Message }
                    : new JsonResponse { Json = JsonSerializer.Serialize(result.Result) };
            }
            catch (Exception ex) { return new JsonResponse { IsError = true, Message = ex.Message }; }
        }

        public override async Task<OASISGrpcResponse> ReceiveGift(ReceiveGiftRequest request, ServerCallContext context)
        {
            try
            {
                if (!Guid.TryParse(request.AvatarId, out var avatarId)) return new OASISGrpcResponse { IsError = true, Message = "Invalid avatar ID." };
                if (!Guid.TryParse(request.GiftId, out var giftId)) return new OASISGrpcResponse { IsError = true, Message = "Invalid gift ID." };
                var result = await GiftsManager.Instance.ReceiveGiftAsync(avatarId, giftId);
                return result.IsError ? new OASISGrpcResponse { IsError = true, Message = result.Message } : new OASISGrpcResponse();
            }
            catch (Exception ex) { return new OASISGrpcResponse { IsError = true, Message = ex.Message }; }
        }

        public override async Task<OASISGrpcResponse> OpenGift(OpenGiftRequest request, ServerCallContext context)
        {
            try
            {
                if (!Guid.TryParse(request.AvatarId, out var avatarId)) return new OASISGrpcResponse { IsError = true, Message = "Invalid avatar ID." };
                if (!Guid.TryParse(request.GiftId, out var giftId)) return new OASISGrpcResponse { IsError = true, Message = "Invalid gift ID." };
                var result = await GiftsManager.Instance.OpenGiftAsync(avatarId, giftId);
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
