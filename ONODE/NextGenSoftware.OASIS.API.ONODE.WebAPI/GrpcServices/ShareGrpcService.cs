using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Grpc.Core;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Helpers;
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
                var result = await HolonAccess.ShareAsync(CreateHolonManager(), holonId, new[] { avatarId }, Caller(context));
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
                var result = await HolonAccess.ShareAsync(CreateHolonManager(), holonId, avatarIds, Caller(context));
                return result.IsError ? new OASISGrpcResponse { IsError = true, Message = result.Message } : new OASISGrpcResponse();
            }
            catch (Exception ex) { return new OASISGrpcResponse { IsError = true, Message = ex.Message }; }
        }

        // JwtMiddleware runs for gRPC requests too and stores the signed-in avatar on the HttpContext.
        private static IAvatar Caller(ServerCallContext context) =>
            context.GetHttpContext()?.Items["Avatar"] as IAvatar;
    }
}
