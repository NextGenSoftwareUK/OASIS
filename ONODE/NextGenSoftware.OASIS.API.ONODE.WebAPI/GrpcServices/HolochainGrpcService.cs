using System;
using System.Text.Json;
using System.Threading.Tasks;
using Grpc.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Grpc;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.GrpcServices
{
    public class HolochainGrpcService : HolochainService.HolochainServiceBase
    {
        private static KeyManager CreateKeyManager()
        {
            var result = Task.Run(OASISBootLoader.OASISBootLoader.GetAndActivateDefaultStorageProviderAsync).Result;
            return new KeyManager(result.Result);
        }

        public override Task<StringListResponse> GetHolochainAgentIdsForAvatar(HoloByAvatarRequest request, ServerCallContext context)
        {
            try
            {
                if (!Guid.TryParse(request.AvatarId, out var avatarId))
                    return Task.FromResult(new StringListResponse { IsError = true, Message = "Invalid avatar ID." });
                var result = CreateKeyManager().GetProviderPublicKeysForAvatarById(avatarId, ProviderType.HoloOASIS);
                var response = new StringListResponse();
                if (result != null && !result.IsError && result.Result != null)
                    response.Values.AddRange(result.Result);
                return Task.FromResult(response);
            }
            catch (Exception ex) { return Task.FromResult(new StringListResponse { IsError = true, Message = ex.Message }); }
        }

        public override Task<StringListResponse> GetHolochainAgentPrivateKeysForAvatar(HoloByAvatarRequest request, ServerCallContext context)
        {
            try
            {
                if (!Guid.TryParse(request.AvatarId, out var avatarId))
                    return Task.FromResult(new StringListResponse { IsError = true, Message = "Invalid avatar ID." });
                var result = CreateKeyManager().GetProviderPrivateKeysForAvatarById(avatarId, ProviderType.HoloOASIS);
                var response = new StringListResponse();
                if (result != null && !result.IsError && result.Result != null)
                    response.Values.AddRange(result.Result);
                return Task.FromResult(response);
            }
            catch (Exception ex) { return Task.FromResult(new StringListResponse { IsError = true, Message = ex.Message }); }
        }

        public override Task<StringResponse> GetAvatarIdForHolochainAgentId(HoloByAgentIdRequest request, ServerCallContext context)
        {
            try
            {
                var result = CreateKeyManager().GetAvatarIdForProviderPublicKey(request.AgentId, ProviderType.HoloOASIS);
                return Task.FromResult(result.IsError
                    ? new StringResponse { IsError = true, Message = result.Message }
                    : new StringResponse { Value = result.Result.ToString() });
            }
            catch (Exception ex) { return Task.FromResult(new StringResponse { IsError = true, Message = ex.Message }); }
        }

        public override Task<JsonResponse> GetAvatarForHolochainAgentId(HoloByAgentIdRequest request, ServerCallContext context)
        {
            try
            {
                var result = CreateKeyManager().GetAvatarForProviderPublicKey(request.AgentId, ProviderType.HoloOASIS);
                return Task.FromResult(result.IsError
                    ? new JsonResponse { IsError = true, Message = result.Message }
                    : new JsonResponse { Json = JsonSerializer.Serialize(result.Result) });
            }
            catch (Exception ex) { return Task.FromResult(new JsonResponse { IsError = true, Message = ex.Message }); }
        }

        public override Task<StringResponse> GetHoloFuelBalanceForAgentId(HoloByAgentIdRequest request, ServerCallContext context)
        {
            return Task.FromResult(new StringResponse { Value = "" });
        }

        public override Task<StringResponse> GetHoloFuelBalanceForAvatar(HoloByAvatarRequest request, ServerCallContext context)
        {
            return Task.FromResult(new StringResponse { Value = "" });
        }

        public override Task<JsonResponse> LinkHolochainAgentIdToAvatar(LinkHolochainAgentRequest request, ServerCallContext context)
        {
            try
            {
                if (!Guid.TryParse(request.AvatarId, out var avatarId) || !Guid.TryParse(request.WalletId, out var walletId))
                    return Task.FromResult(new JsonResponse { IsError = true, Message = "Invalid avatar or wallet ID." });
                ProviderType providerToLoad = ProviderType.Default;
                if (!string.IsNullOrWhiteSpace(request.ProviderType))
                    System.Enum.TryParse<ProviderType>(request.ProviderType, true, out providerToLoad);
                var result = CreateKeyManager().LinkProviderPublicKeyToAvatarById(walletId, avatarId, ProviderType.HoloOASIS, request.AgentId, null, providerToLoadAvatarFrom: providerToLoad);
                return Task.FromResult(result.IsError
                    ? new JsonResponse { IsError = true, Message = result.Message }
                    : new JsonResponse { Json = JsonSerializer.Serialize(result.Result) });
            }
            catch (Exception ex) { return Task.FromResult(new JsonResponse { IsError = true, Message = ex.Message }); }
        }
    }
}
