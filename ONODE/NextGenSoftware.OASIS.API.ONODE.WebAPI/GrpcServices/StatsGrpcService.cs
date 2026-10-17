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
    public class StatsGrpcService : StatsService.StatsServiceBase
    {
        public override async Task<JsonResponse> GetAvatarStats(GetAvatarStatsRequest request, ServerCallContext context)
        {
            try
            {
                if (!Guid.TryParse(request.AvatarId, out var avatarId))
                    return new JsonResponse { IsError = true, Message = "Invalid avatar ID." };
                var result = await StatsManager.Instance.GetAvatarStatsAsync(avatarId);
                return result.IsError
                    ? new JsonResponse { IsError = true, Message = result.Message }
                    : new JsonResponse { Json = JsonSerializer.Serialize(result.Result) };
            }
            catch (Exception ex) { return new JsonResponse { IsError = true, Message = ex.Message }; }
        }

        public override async Task<JsonResponse> GetLeaderboardStats(GetLeaderboardStatsRequest request, ServerCallContext context)
        {
            try
            {
                if (!Guid.TryParse(request.AvatarId, out var avatarId))
                    return new JsonResponse { IsError = true, Message = "Invalid avatar ID." };
                var result = await StatsManager.Instance.GetLeaderboardStatsAsync(avatarId);
                return result.IsError
                    ? new JsonResponse { IsError = true, Message = result.Message }
                    : new JsonResponse { Json = JsonSerializer.Serialize(result.Result) };
            }
            catch (Exception ex) { return new JsonResponse { IsError = true, Message = ex.Message }; }
        }

        public override async Task<JsonResponse> GetKeyStats(GetKeyStatsRequest request, ServerCallContext context)
        {
            try
            {
                if (!Guid.TryParse(request.AvatarId, out var avatarId))
                    return new JsonResponse { IsError = true, Message = "Invalid avatar ID." };
                var result = await StatsManager.Instance.GetKeyStatsAsync(avatarId);
                return result.IsError
                    ? new JsonResponse { IsError = true, Message = result.Message }
                    : new JsonResponse { Json = JsonSerializer.Serialize(result.Result) };
            }
            catch (Exception ex) { return new JsonResponse { IsError = true, Message = ex.Message }; }
        }

        public override async Task<JsonResponse> GetSystemStats(GetSystemStatsRequest request, ServerCallContext context)
        {
            try
            {
                var result = await StatsManager.Instance.GetSystemStatsAsync();
                return result.IsError
                    ? new JsonResponse { IsError = true, Message = result.Message }
                    : new JsonResponse { Json = JsonSerializer.Serialize(result.Result) };
            }
            catch (Exception ex) { return new JsonResponse { IsError = true, Message = ex.Message }; }
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
