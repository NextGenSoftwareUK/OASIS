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
    public class FilesGrpcService : FilesService.FilesServiceBase
    {
        public override async Task<JsonResponse> GetAllFiles(GetAllFilesRequest request, ServerCallContext context)
        {
            try
            {
                if (!Guid.TryParse(request.AvatarId, out var avatarId))
                    return new JsonResponse { IsError = true, Message = "Invalid avatar ID." };
                var result = await FilesManager.Instance.GetAllFilesForAvatarAsync(avatarId);
                return result.IsError
                    ? new JsonResponse { IsError = true, Message = result.Message }
                    : new JsonResponse { Json = JsonSerializer.Serialize(result.Result) };
            }
            catch (Exception ex) { return new JsonResponse { IsError = true, Message = ex.Message }; }
        }

        public override async Task<JsonResponse> GetFileMetadata(GetFileMetadataRequest request, ServerCallContext context)
        {
            try
            {
                if (!Guid.TryParse(request.AvatarId, out var avatarId)) return new JsonResponse { IsError = true, Message = "Invalid avatar ID." };
                if (!Guid.TryParse(request.FileId, out var fileId)) return new JsonResponse { IsError = true, Message = "Invalid file ID." };
                var result = await FilesManager.Instance.GetFileMetadataAsync(avatarId, fileId);
                return result.IsError
                    ? new JsonResponse { IsError = true, Message = result.Message }
                    : new JsonResponse { Json = JsonSerializer.Serialize(result.Result) };
            }
            catch (Exception ex) { return new JsonResponse { IsError = true, Message = ex.Message }; }
        }

        public override async Task<OASISGrpcResponse> DeleteFile(DeleteFileRequest request, ServerCallContext context)
        {
            try
            {
                if (!Guid.TryParse(request.AvatarId, out var avatarId)) return new OASISGrpcResponse { IsError = true, Message = "Invalid avatar ID." };
                if (!Guid.TryParse(request.FileId, out var fileId)) return new OASISGrpcResponse { IsError = true, Message = "Invalid file ID." };
                var result = await FilesManager.Instance.DeleteFileAsync(avatarId, fileId);
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
