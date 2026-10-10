using System;
using System.Threading.Tasks;
using Grpc.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Grpc;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.GrpcServices
{
    public class KeysGrpcService : KeysService.KeysServiceBase
    {
        private static KeyManager CreateKeyManager()
        {
            var result = System.Threading.Tasks.Task.Run(
                OASISBootLoader.OASISBootLoader.GetAndActivateDefaultStorageProviderAsync).Result;
            return new KeyManager(result.Result);
        }

        public override Task<StringResponse> GetProviderUniqueStorageKey(GetProviderUniqueStorageKeyRequest request, ServerCallContext context)
        {
            try
            {
                if (!Guid.TryParse(request.AvatarId, out var avatarId))
                    return Task.FromResult(new StringResponse { IsError = true, Message = "Invalid avatar ID." });
                var pt = ParseEnum(request.ProviderType, ProviderType.Default);
                var manager = CreateKeyManager();
                var result = manager.GetProviderUniqueStorageKeyForAvatarById(avatarId, pt);
                return Task.FromResult(result.IsError
                    ? new StringResponse { IsError = true, Message = result.Message }
                    : new StringResponse { Value = result.Result ?? string.Empty });
            }
            catch (Exception ex) { return Task.FromResult(new StringResponse { IsError = true, Message = ex.Message }); }
        }

        public override Task<StringListResponse> GetProviderPublicKeys(GetProviderPublicKeysRequest request, ServerCallContext context)
        {
            try
            {
                if (!Guid.TryParse(request.AvatarId, out var avatarId))
                    return Task.FromResult(new StringListResponse { IsError = true, Message = "Invalid avatar ID." });
                var pt = ParseEnum(request.ProviderType, ProviderType.Default);
                var manager = CreateKeyManager();
                var result = manager.GetProviderPublicKeysForAvatarById(avatarId, pt);
                var resp = new StringListResponse { IsError = result.IsError, Message = result.Message ?? string.Empty };
                if (!result.IsError && result.Result != null)
                    resp.Values.AddRange(result.Result);
                return Task.FromResult(resp);
            }
            catch (Exception ex) { return Task.FromResult(new StringListResponse { IsError = true, Message = ex.Message }); }
        }

        public override Task<OASISGrpcResponse> ClearKeyCache(KeysEmptyRequest request, ServerCallContext context)
        {
            try
            {
                var manager = CreateKeyManager();
                var result = manager.ClearCache();
                return Task.FromResult(result.IsError ? new OASISGrpcResponse { IsError = true, Message = result.Message } : new OASISGrpcResponse());
            }
            catch (Exception ex) { return Task.FromResult(new OASISGrpcResponse { IsError = true, Message = ex.Message }); }
        }

        public override Task<OASISGrpcResponse> LinkProviderPublicKey(LinkProviderPublicKeyRequest request, ServerCallContext context)
        {
            try
            {
                if (!Guid.TryParse(request.WalletId, out var walletId)) return Task.FromResult(new OASISGrpcResponse { IsError = true, Message = "Invalid wallet ID." });
                if (!Guid.TryParse(request.AvatarId, out var avatarId)) return Task.FromResult(new OASISGrpcResponse { IsError = true, Message = "Invalid avatar ID." });
                var pt = ParseEnum(request.ProviderType, ProviderType.Default);
                var manager = CreateKeyManager();
                var result = manager.LinkProviderPublicKeyToAvatarById(walletId, avatarId, pt, request.ProviderKey, request.WalletAddress);
                return Task.FromResult(result.IsError ? new OASISGrpcResponse { IsError = true, Message = result.Message } : new OASISGrpcResponse());
            }
            catch (Exception ex) { return Task.FromResult(new OASISGrpcResponse { IsError = true, Message = ex.Message }); }
        }

        public override Task<OASISGrpcResponse> LinkProviderPrivateKey(LinkProviderPrivateKeyRequest request, ServerCallContext context)
        {
            try
            {
                if (!Guid.TryParse(request.WalletId, out var walletId)) return Task.FromResult(new OASISGrpcResponse { IsError = true, Message = "Invalid wallet ID." });
                if (!Guid.TryParse(request.AvatarId, out var avatarId)) return Task.FromResult(new OASISGrpcResponse { IsError = true, Message = "Invalid avatar ID." });
                var pt = ParseEnum(request.ProviderType, ProviderType.Default);
                var manager = CreateKeyManager();
                var result = manager.LinkProviderPrivateKeyToAvatarById(walletId, avatarId, pt, request.ProviderPrivateKey);
                return Task.FromResult(result.IsError ? new OASISGrpcResponse { IsError = true, Message = result.Message } : new OASISGrpcResponse());
            }
            catch (Exception ex) { return Task.FromResult(new OASISGrpcResponse { IsError = true, Message = ex.Message }); }
        }

        public override Task<OASISGrpcResponse> GenerateKeyPairAndLink(GenerateKeyPairAndLinkRequest request, ServerCallContext context)
        {
            try
            {
                if (!Guid.TryParse(request.AvatarId, out var avatarId)) return Task.FromResult(new OASISGrpcResponse { IsError = true, Message = "Invalid avatar ID." });
                var pt = ParseEnum(request.ProviderType, ProviderType.Default);
                var manager = CreateKeyManager();
                var result = manager.GenerateKeyPairWithWalletAddressAndLinkProviderKeysToAvatarById(avatarId, pt);
                return Task.FromResult(result.IsError ? new OASISGrpcResponse { IsError = true, Message = result.Message } : new OASISGrpcResponse());
            }
            catch (Exception ex) { return Task.FromResult(new OASISGrpcResponse { IsError = true, Message = ex.Message }); }
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
