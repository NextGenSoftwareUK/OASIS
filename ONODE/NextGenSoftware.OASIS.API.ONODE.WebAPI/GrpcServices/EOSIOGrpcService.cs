using System;
using System.Text.Json;
using System.Threading.Tasks;
using Grpc.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Providers.EOSIOOASIS;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Grpc;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.GrpcServices
{
    public class EOSIOGrpcService : EOSIOService.EOSIOServiceBase
    {
        private static KeyManager CreateKeyManager()
        {
            var result = Task.Run(OASISBootLoader.OASISBootLoader.GetAndActivateDefaultStorageProviderAsync).Result;
            return new KeyManager(result.Result);
        }

        private static EOSIOOASIS CreateEOSIOOASIS()
        {
            var result = Task.Run(async () => await OASISBootLoader.OASISBootLoader.GetAndActivateStorageProviderAsync(ProviderType.EOSIOOASIS)).Result;
            if (result.IsError) throw new Exception(result.Message);
            return (EOSIOOASIS)result.Result;
        }

        public override Task<StringListResponse> GetEOSIOAccountNamesForAvatar(EOSIOByAvatarRequest request, ServerCallContext context)
        {
            try
            {
                if (!Guid.TryParse(request.AvatarId, out var avatarId))
                    return Task.FromResult(new StringListResponse { IsError = true, Message = "Invalid avatar ID." });
                var result = CreateKeyManager().GetProviderPublicKeysForAvatarById(avatarId, ProviderType.EOSIOOASIS);
                var response = new StringListResponse();
                if (result != null && !result.IsError && result.Result != null)
                    response.Values.AddRange(result.Result);
                return Task.FromResult(response);
            }
            catch (Exception ex) { return Task.FromResult(new StringListResponse { IsError = true, Message = ex.Message }); }
        }

        public override Task<StringResponse> GetEOSIOAccountPrivateKeyForAvatar(EOSIOByAvatarRequest request, ServerCallContext context)
        {
            try
            {
                if (!Guid.TryParse(request.AvatarId, out var avatarId))
                    return Task.FromResult(new StringResponse { IsError = true, Message = "Invalid avatar ID." });
                var eosio = CreateEOSIOOASIS();
                var key = eosio.GetEOSIOAccountPrivateKeyForAvatar(avatarId);
                return Task.FromResult(new StringResponse { Value = key ?? "" });
            }
            catch (Exception ex) { return Task.FromResult(new StringResponse { IsError = true, Message = ex.Message }); }
        }

        public override Task<JsonResponse> GetEOSIOAccount(EOSIOByAccountNameRequest request, ServerCallContext context)
        {
            try
            {
                var eosio = CreateEOSIOOASIS();
                var account = eosio.GetEOSIOAccount(request.AccountName);
                return Task.FromResult(new JsonResponse { Json = JsonSerializer.Serialize(account) });
            }
            catch (Exception ex) { return Task.FromResult(new JsonResponse { IsError = true, Message = ex.Message }); }
        }

        public override Task<JsonResponse> GetEOSIOAccountForAvatar(EOSIOByAvatarRequest request, ServerCallContext context)
        {
            try
            {
                if (!Guid.TryParse(request.AvatarId, out var avatarId))
                    return Task.FromResult(new JsonResponse { IsError = true, Message = "Invalid avatar ID." });
                var eosio = CreateEOSIOOASIS();
                var account = eosio.GetEOSIOAccountForAvatar(avatarId);
                return Task.FromResult(new JsonResponse { Json = JsonSerializer.Serialize(account) });
            }
            catch (Exception ex) { return Task.FromResult(new JsonResponse { IsError = true, Message = ex.Message }); }
        }

        public override Task<StringResponse> GetAvatarIdForEOSIOAccountName(EOSIOByAccountNameRequest request, ServerCallContext context)
        {
            try
            {
                var eosio = CreateEOSIOOASIS();
                var avatarId = eosio.GetAvatarIdForEOSIOAccountName(request.AccountName);
                return Task.FromResult(new StringResponse { Value = avatarId.ToString() });
            }
            catch (Exception ex) { return Task.FromResult(new StringResponse { IsError = true, Message = ex.Message }); }
        }

        public override Task<JsonResponse> GetAvatarForEOSIOAccountName(EOSIOByAccountNameRequest request, ServerCallContext context)
        {
            try
            {
                var eosio = CreateEOSIOOASIS();
                var avatar = eosio.GetAvatarForEOSIOAccountName(request.AccountName);
                return Task.FromResult(new JsonResponse { Json = JsonSerializer.Serialize(avatar) });
            }
            catch (Exception ex) { return Task.FromResult(new JsonResponse { IsError = true, Message = ex.Message }); }
        }

        public override Task<StringResponse> GetBalanceForEOSIOAccount(GetEOSIOBalanceRequest request, ServerCallContext context)
        {
            try
            {
                var eosio = CreateEOSIOOASIS();
                var balance = eosio.GetBalanceForEOSIOAccount(request.AccountName, request.Code, request.Symbol);
                return Task.FromResult(new StringResponse { Value = balance ?? "" });
            }
            catch (Exception ex) { return Task.FromResult(new StringResponse { IsError = true, Message = ex.Message }); }
        }

        public override Task<StringResponse> GetBalanceForAvatarEOSIO(GetEOSIOAvatarBalanceRequest request, ServerCallContext context)
        {
            try
            {
                if (!Guid.TryParse(request.AvatarId, out var avatarId))
                    return Task.FromResult(new StringResponse { IsError = true, Message = "Invalid avatar ID." });
                var eosio = CreateEOSIOOASIS();
                var balance = eosio.GetBalanceForAvatar(avatarId, request.Code, request.Symbol);
                return Task.FromResult(new StringResponse { Value = balance ?? "" });
            }
            catch (Exception ex) { return Task.FromResult(new StringResponse { IsError = true, Message = ex.Message }); }
        }

        public override Task<JsonResponse> LinkEOSIOAccountToAvatar(LinkEOSIOAccountRequest request, ServerCallContext context)
        {
            try
            {
                if (!Guid.TryParse(request.AvatarId, out var avatarId) || !Guid.TryParse(request.WalletId, out var walletId))
                    return Task.FromResult(new JsonResponse { IsError = true, Message = "Invalid avatar or wallet ID." });
                var result = CreateKeyManager().LinkProviderPublicKeyToAvatarById(walletId, avatarId, ProviderType.EOSIOOASIS, request.AccountName, request.WalletAddress);
                return Task.FromResult(result.IsError
                    ? new JsonResponse { IsError = true, Message = result.Message }
                    : new JsonResponse { Json = JsonSerializer.Serialize(result.Result) });
            }
            catch (Exception ex) { return Task.FromResult(new JsonResponse { IsError = true, Message = ex.Message }); }
        }
    }
}
