using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Grpc.Core;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Grpc;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.GrpcServices
{
    public class ShareGrpcService : ShareService.ShareServiceBase
    {
        private static HolonManager CreateHolonManager()
        {
            var result = Task.Run(OASISBootLoader.OASISBootLoader.GetAndActivateDefaultStorageProviderAsync).Result;
            return new HolonManager(result.Result);
        }

        public override async Task<OASISGrpcResponse> ShareHolon(ShareSingleRequest request, ServerCallContext context)
        {
            try
            {
                if (!Guid.TryParse(request.HolonId, out var holonId) || !Guid.TryParse(request.AvatarId, out var avatarId))
                    return new OASISGrpcResponse { IsError = true, Message = "Invalid holon or avatar ID." };
                var result = await ShareHolonInternalAsync(holonId, new[] { avatarId });
                return result.IsError ? new OASISGrpcResponse { IsError = true, Message = result.Message } : new OASISGrpcResponse();
            }
            catch (Exception ex) { return new OASISGrpcResponse { IsError = true, Message = ex.Message }; }
        }

        public override async Task<OASISGrpcResponse> ShareHolonMany(ShareManyRequest request, ServerCallContext context)
        {
            try
            {
                if (!Guid.TryParse(request.HolonId, out var holonId))
                    return new OASISGrpcResponse { IsError = true, Message = "Invalid holon ID." };
                var avatarIds = request.AvatarIdsCsv
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Select(s => Guid.TryParse(s, out var g) ? g : Guid.Empty)
                    .Where(g => g != Guid.Empty).ToArray();
                if (avatarIds.Length == 0)
                    return new OASISGrpcResponse { IsError = true, Message = "No valid avatar IDs provided." };
                var result = await ShareHolonInternalAsync(holonId, avatarIds);
                return result.IsError ? new OASISGrpcResponse { IsError = true, Message = result.Message } : new OASISGrpcResponse();
            }
            catch (Exception ex) { return new OASISGrpcResponse { IsError = true, Message = ex.Message }; }
        }

        private async Task<NextGenSoftware.OASIS.Common.OASISResult<bool>> ShareHolonInternalAsync(Guid holonId, IEnumerable<Guid> avatarIds)
        {
            var result = new NextGenSoftware.OASIS.Common.OASISResult<bool>();
            var manager = CreateHolonManager();
            var holonResult = await manager.LoadHolonAsync(holonId);
            if (holonResult == null || holonResult.IsError || holonResult.Result == null)
            {
                result.IsError = true; result.Message = $"Unable to load holon {holonId}. Reason: {holonResult?.Message}";
                return result;
            }
            if (holonResult.Result.MetaData == null)
                holonResult.Result.MetaData = new Dictionary<string, object>();
            var ids = new HashSet<Guid>(avatarIds.Where(g => g != Guid.Empty));
            holonResult.Result.MetaData["SHARED_AVATAR_IDS"] = System.Text.Json.JsonSerializer.Serialize(ids);
            var saveResult = await manager.SaveHolonAsync(holonResult.Result, Guid.Empty);
            if (saveResult == null || saveResult.IsError)
            {
                result.IsError = true; result.Message = $"Unable to save holon. Reason: {saveResult?.Message}";
                return result;
            }
            result.Result = true;
            return result;
        }
    }
}
