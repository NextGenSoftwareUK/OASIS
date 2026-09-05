using System;
using System.Text.Json;
using System.Threading.Tasks;
using Grpc.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Grpc;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.GrpcServices
{
    public class WalletGrpcService : WalletService.WalletServiceBase
    {
        private static WalletManager CreateWalletManager()
        {
            var result = System.Threading.Tasks.Task.Run(
                OASISBootLoader.OASISBootLoader.GetAndActivateDefaultStorageProviderAsync).Result;
            return new WalletManager(result.Result);
        }

        public override async Task<JsonResponse> GetWalletsForAvatarById(GetWalletsForAvatarByIdRequest request, ServerCallContext context)
        {
            try
            {
                if (!Guid.TryParse(request.AvatarId, out var avatarId))
                    return new JsonResponse { IsError = true, Message = "Invalid avatar ID." };
                var manager = CreateWalletManager();
                var result = await manager.LoadProviderWalletsForAvatarByIdAsync(avatarId, request.ShowOnlyDefault);
                return result.IsError
                    ? new JsonResponse { IsError = true, Message = result.Message }
                    : new JsonResponse { Json = JsonSerializer.Serialize(result.Result) };
            }
            catch (Exception ex) { return new JsonResponse { IsError = true, Message = ex.Message }; }
        }

        public override async Task<JsonResponse> GetWalletsForAvatarByUsername(GetWalletsForAvatarByUsernameRequest request, ServerCallContext context)
        {
            try
            {
                var manager = CreateWalletManager();
                var result = await manager.LoadProviderWalletsForAvatarByUsernameAsync(request.Username, request.ShowOnlyDefault);
                return result.IsError
                    ? new JsonResponse { IsError = true, Message = result.Message }
                    : new JsonResponse { Json = JsonSerializer.Serialize(result.Result) };
            }
            catch (Exception ex) { return new JsonResponse { IsError = true, Message = ex.Message }; }
        }

        public override async Task<JsonResponse> GetWalletsForAvatarByEmail(GetWalletsForAvatarByEmailRequest request, ServerCallContext context)
        {
            try
            {
                var manager = CreateWalletManager();
                var result = await manager.LoadProviderWalletsForAvatarByEmailAsync(request.Email);
                return result.IsError
                    ? new JsonResponse { IsError = true, Message = result.Message }
                    : new JsonResponse { Json = JsonSerializer.Serialize(result.Result) };
            }
            catch (Exception ex) { return new JsonResponse { IsError = true, Message = ex.Message }; }
        }

        public override async Task<JsonResponse> CreateWallet(CreateWalletRequest request, ServerCallContext context)
        {
            try
            {
                if (!Guid.TryParse(request.AvatarId, out var avatarId))
                    return new JsonResponse { IsError = true, Message = "Invalid avatar ID." };
                var pt = ParseEnum(request.WalletProviderType, ProviderType.Default);
                var manager = CreateWalletManager();
                var result = await manager.CreateWalletForAvatarByIdAsync(avatarId, request.Name, request.Description, pt);
                return result.IsError
                    ? new JsonResponse { IsError = true, Message = result.Message }
                    : new JsonResponse { Json = JsonSerializer.Serialize(result.Result) };
            }
            catch (Exception ex) { return new JsonResponse { IsError = true, Message = ex.Message }; }
        }

        public override async Task<JsonResponse> CreateWalletByUsername(CreateWalletByUsernameRequest request, ServerCallContext context)
        {
            try
            {
                var pt = ParseEnum(request.WalletProviderType, ProviderType.Default);
                var manager = CreateWalletManager();
                var result = await manager.CreateWalletForAvatarByUsernameAsync(request.Username, request.Name, request.Description, pt);
                return result.IsError
                    ? new JsonResponse { IsError = true, Message = result.Message }
                    : new JsonResponse { Json = JsonSerializer.Serialize(result.Result) };
            }
            catch (Exception ex) { return new JsonResponse { IsError = true, Message = ex.Message }; }
        }

        public override async Task<JsonResponse> CreateWalletByEmail(CreateWalletByEmailRequest request, ServerCallContext context)
        {
            try
            {
                var pt = ParseEnum(request.WalletProviderType, ProviderType.Default);
                var manager = CreateWalletManager();
                var result = await manager.CreateWalletForAvatarByEmailAsync(request.Email, request.Name, request.Description, pt);
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
