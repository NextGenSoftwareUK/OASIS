using System;
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
    public class ClanGrpcService : ClanService.ClanServiceBase
    {
        public override async Task<JsonResponse> GetClan(GetClanRequest request, ServerCallContext context)
        {
            try
            {
                if (!Guid.TryParse(request.ClanId, out var clanId))
                    return new JsonResponse { IsError = true, Message = "Invalid clan ID." };
                var result = await ClanManager.Instance.LoadClanAsync(clanId);
                return result.IsError
                    ? new JsonResponse { IsError = true, Message = result.Message }
                    : new JsonResponse { Json = JsonSerializer.Serialize(result.Result) };
            }
            catch (Exception ex) { return new JsonResponse { IsError = true, Message = ex.Message }; }
        }

        public override async Task<JsonResponse> GetClanByName(GetClanByNameRequest request, ServerCallContext context)
        {
            try
            {
                var result = await ClanManager.Instance.LoadClanByNameAsync(request.Name);
                return result.IsError
                    ? new JsonResponse { IsError = true, Message = result.Message }
                    : new JsonResponse { Json = JsonSerializer.Serialize(result.Result) };
            }
            catch (Exception ex) { return new JsonResponse { IsError = true, Message = ex.Message }; }
        }

        public override async Task<JsonResponse> GetAllClans(GetAllClansRequest request, ServerCallContext context)
        {
            try
            {
                Guid? ownerAvatarId = null;
                if (!string.IsNullOrWhiteSpace(request.OwnerAvatarId) && Guid.TryParse(request.OwnerAvatarId, out var g))
                    ownerAvatarId = g;
                var result = await ClanManager.Instance.ListClansAsync(ownerAvatarId);
                return result.IsError
                    ? new JsonResponse { IsError = true, Message = result.Message }
                    : new JsonResponse { Json = JsonSerializer.Serialize(result.Result) };
            }
            catch (Exception ex) { return new JsonResponse { IsError = true, Message = ex.Message }; }
        }

        public override async Task<StringListResponse> GetClanMembers(GetClanMembersRequest request, ServerCallContext context)
        {
            try
            {
                if (!Guid.TryParse(request.ClanId, out var clanId))
                    return new StringListResponse { IsError = true, Message = "Invalid clan ID." };
                var result = await ClanManager.Instance.GetClanMembersAsync(clanId);
                var resp = new StringListResponse { IsError = result.IsError, Message = result.Message ?? string.Empty };
                if (!result.IsError && result.Result != null)
                    resp.Values.AddRange(result.Result.Select(g => g.ToString()));
                return resp;
            }
            catch (Exception ex) { return new StringListResponse { IsError = true, Message = ex.Message }; }
        }

        public override async Task<JsonResponse> CreateClan(CreateClanRequest request, ServerCallContext context)
        {
            try
            {
                if (!Guid.TryParse(request.OwnerAvatarId, out var ownerId))
                    return new JsonResponse { IsError = true, Message = "Invalid owner avatar ID." };
                var result = await ClanManager.Instance.CreateClanAsync(ownerId, request.Name, string.IsNullOrWhiteSpace(request.Description) ? null : request.Description);
                return result.IsError
                    ? new JsonResponse { IsError = true, Message = result.Message }
                    : new JsonResponse { Json = JsonSerializer.Serialize(result.Result) };
            }
            catch (Exception ex) { return new JsonResponse { IsError = true, Message = ex.Message }; }
        }

        public override async Task<OASISGrpcResponse> DeleteClan(DeleteClanRequest request, ServerCallContext context)
        {
            try
            {
                if (!Guid.TryParse(request.ClanId, out var clanId))
                    return new OASISGrpcResponse { IsError = true, Message = "Invalid clan ID." };
                var result = await ClanManager.Instance.DeleteClanAsync(clanId);
                return result.IsError ? new OASISGrpcResponse { IsError = true, Message = result.Message } : new OASISGrpcResponse();
            }
            catch (Exception ex) { return new OASISGrpcResponse { IsError = true, Message = ex.Message }; }
        }

        public override async Task<OASISGrpcResponse> AddAvatarToClan(AddAvatarToClanRequest request, ServerCallContext context)
        {
            try
            {
                if (!Guid.TryParse(request.ClanId, out var clanId)) return new OASISGrpcResponse { IsError = true, Message = "Invalid clan ID." };
                if (!Guid.TryParse(request.AvatarId, out var avatarId)) return new OASISGrpcResponse { IsError = true, Message = "Invalid avatar ID." };
                var result = await ClanManager.Instance.AddAvatarToClanAsync(clanId, avatarId);
                return result.IsError ? new OASISGrpcResponse { IsError = true, Message = result.Message } : new OASISGrpcResponse();
            }
            catch (Exception ex) { return new OASISGrpcResponse { IsError = true, Message = ex.Message }; }
        }

        public override async Task<OASISGrpcResponse> RemoveAvatarFromClan(RemoveAvatarFromClanRequest request, ServerCallContext context)
        {
            try
            {
                if (!Guid.TryParse(request.ClanId, out var clanId)) return new OASISGrpcResponse { IsError = true, Message = "Invalid clan ID." };
                if (!Guid.TryParse(request.AvatarId, out var avatarId)) return new OASISGrpcResponse { IsError = true, Message = "Invalid avatar ID." };
                var result = await ClanManager.Instance.RemoveAvatarFromClanAsync(clanId, avatarId);
                return result.IsError ? new OASISGrpcResponse { IsError = true, Message = result.Message } : new OASISGrpcResponse();
            }
            catch (Exception ex) { return new OASISGrpcResponse { IsError = true, Message = ex.Message }; }
        }

        public override async Task<OASISGrpcResponse> SendItemToClan(SendItemToClanRequest request, ServerCallContext context)
        {
            try
            {
                if (!Guid.TryParse(request.SenderAvatarId, out var senderId)) return new OASISGrpcResponse { IsError = true, Message = "Invalid sender avatar ID." };
                if (!Guid.TryParse(request.ClanId, out var clanId)) return new OASISGrpcResponse { IsError = true, Message = "Invalid clan ID." };
                var result = await ClanManager.Instance.SendItemToClanAsync(senderId, clanId, request.ItemName, request.Quantity);
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
