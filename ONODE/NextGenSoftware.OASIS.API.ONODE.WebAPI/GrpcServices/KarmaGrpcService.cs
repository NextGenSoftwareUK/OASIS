using System;
using System.Threading.Tasks;
using Grpc.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Grpc;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.GrpcServices
{
    public class KarmaGrpcService : KarmaService.KarmaServiceBase
    {
        private static AvatarManager AvatarManager => Program.AvatarManager;

        public override async Task<KarmaResponse> GetKarmaForAvatar(GetKarmaRequest request, ServerCallContext context)
        {
            try
            {
                await ActivateProviderIfSpecifiedAsync(request.ProviderType, request.SetGlobally);

                if (!Guid.TryParse(request.AvatarId, out var avatarId))
                    return new KarmaResponse { IsError = true, Message = "Invalid avatar ID format." };

                // Karma lives on AvatarDetail, not IAvatar
                var result = await AvatarManager.LoadAvatarDetailAsync(avatarId);

                return result.IsError
                    ? new KarmaResponse { IsError = true, Message = result.Message }
                    : new KarmaResponse { KarmaTotal = (int)(result.Result?.Karma ?? 0) };
            }
            catch (Exception ex)
            {
                return new KarmaResponse { IsError = true, Message = ex.Message };
            }
        }

        public override async Task<KarmaResponse> AddKarmaToAvatar(KarmaOperationRequest request, ServerCallContext context)
        {
            try
            {
                await ActivateProviderIfSpecifiedAsync(request.ProviderType, request.SetGlobally);

                if (!Guid.TryParse(request.AvatarId, out var avatarId))
                    return new KarmaResponse { IsError = true, Message = "Invalid avatar ID format." };

                var karmaTypePositive = ParseEnum(request.KarmaTypePositive, KarmaTypePositive.None);
                var karmaSource = ParseEnum(request.KarmaSource, KarmaSourceType.API);

                // KarmaManager.Instance wraps the result in OASISResult<KarmaAkashicRecord>
                var result = await KarmaManager.Instance.AddKarmaToAvatarAsync(
                    avatarId, karmaTypePositive, karmaSource,
                    request.KarmaSourceTitle, request.KarmaSourceDesc);

                return result.IsError
                    ? new KarmaResponse { IsError = true, Message = result.Message }
                    : new KarmaResponse { KarmaTotal = (int)(result.Result?.TotalKarma ?? 0) };
            }
            catch (Exception ex)
            {
                return new KarmaResponse { IsError = true, Message = ex.Message };
            }
        }

        public override async Task<KarmaResponse> RemoveKarmaFromAvatar(KarmaOperationRequest request, ServerCallContext context)
        {
            try
            {
                await ActivateProviderIfSpecifiedAsync(request.ProviderType, request.SetGlobally);

                if (!Guid.TryParse(request.AvatarId, out var avatarId))
                    return new KarmaResponse { IsError = true, Message = "Invalid avatar ID format." };

                var karmaTypeNegative = ParseEnum(request.KarmaTypeNegative, KarmaTypeNegative.None);
                var karmaSource = ParseEnum(request.KarmaSource, KarmaSourceType.API);

                var result = await KarmaManager.Instance.RemoveKarmaFromAvatarAsync(
                    avatarId, karmaTypeNegative, karmaSource,
                    request.KarmaSourceTitle, request.KarmaSourceDesc);

                return result.IsError
                    ? new KarmaResponse { IsError = true, Message = result.Message }
                    : new KarmaResponse { KarmaTotal = (int)(result.Result?.TotalKarma ?? 0) };
            }
            catch (Exception ex)
            {
                return new KarmaResponse { IsError = true, Message = ex.Message };
            }
        }

        // ── Helpers ─────────────────────────────────────────────────────────────

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
