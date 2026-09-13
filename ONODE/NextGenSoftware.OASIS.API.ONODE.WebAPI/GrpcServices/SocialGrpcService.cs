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
    public class SocialGrpcService : SocialService.SocialServiceBase
    {
        public override async Task<JsonResponse> GetSocialFeed(GetSocialFeedRequest request, ServerCallContext context)
        {
            try
            {
                if (!Guid.TryParse(request.AvatarId, out var avatarId))
                    return new JsonResponse { IsError = true, Message = "Invalid avatar ID." };
                var result = await SocialManager.Instance.GetSocialFeedAsync(avatarId);
                return result.IsError
                    ? new JsonResponse { IsError = true, Message = result.Message }
                    : new JsonResponse { Json = JsonSerializer.Serialize(result.Result) };
            }
            catch (Exception ex) { return new JsonResponse { IsError = true, Message = ex.Message }; }
        }

        public override async Task<JsonResponse> GetRegisteredSocialProviders(GetRegisteredSocialProvidersRequest request, ServerCallContext context)
        {
            try
            {
                if (!Guid.TryParse(request.AvatarId, out var avatarId))
                    return new JsonResponse { IsError = true, Message = "Invalid avatar ID." };
                var result = await SocialManager.Instance.GetRegisteredProvidersAsync(avatarId);
                return result.IsError
                    ? new JsonResponse { IsError = true, Message = result.Message }
                    : new JsonResponse { Json = JsonSerializer.Serialize(result.Result) };
            }
            catch (Exception ex) { return new JsonResponse { IsError = true, Message = ex.Message }; }
        }

        public override async Task<OASISGrpcResponse> RegisterSocialProvider(RegisterSocialProviderRequest request, ServerCallContext context)
        {
            try
            {
                if (!Guid.TryParse(request.AvatarId, out var avatarId))
                    return new OASISGrpcResponse { IsError = true, Message = "Invalid avatar ID." };
                var result = await SocialManager.Instance.RegisterSocialProviderAsync(avatarId, request.ProviderName, request.AccessToken);
                return result.IsError
                    ? new OASISGrpcResponse { IsError = true, Message = result.Message }
                    : new OASISGrpcResponse { IsError = false };
            }
            catch (Exception ex) { return new OASISGrpcResponse { IsError = true, Message = ex.Message }; }
        }

        public override async Task<OASISGrpcResponse> ShareHolon(ShareHolonRequest request, ServerCallContext context)
        {
            try
            {
                if (!Guid.TryParse(request.AvatarId, out var avatarId))
                    return new OASISGrpcResponse { IsError = true, Message = "Invalid avatar ID." };
                if (!Guid.TryParse(request.HolonId, out var holonId))
                    return new OASISGrpcResponse { IsError = true, Message = "Invalid holon ID." };
                var result = await SocialManager.Instance.ShareHolonAsync(avatarId, holonId, request.Message);
                return result.IsError
                    ? new OASISGrpcResponse { IsError = true, Message = result.Message }
                    : new OASISGrpcResponse { IsError = false };
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
