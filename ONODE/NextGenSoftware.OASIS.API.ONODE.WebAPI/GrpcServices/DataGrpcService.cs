using System;
using System.Linq;
using System.Threading.Tasks;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Grpc;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.GrpcServices
{
    public class DataGrpcService : DataService.DataServiceBase
    {
        private HolonManager CreateHolonManager()
        {
            var result = System.Threading.Tasks.Task.Run(
                OASISBootLoader.OASISBootLoader.GetAndActivateDefaultStorageProviderAsync).Result;

            return new HolonManager(result.Result);
        }

        public override async Task<HolonResponse> SaveHolon(SaveHolonGrpcRequest request, ServerCallContext context)
        {
            try
            {
                await ActivateProviderIfSpecifiedAsync(request.ProviderType, request.SetGlobally);

                var holon = MapToHolon(request.Holon);
                var manager = CreateHolonManager();

                // SaveHolonAsync(IHolon holon, bool saveChildren, bool recursive, int maxChildDepth, bool continueOnError, bool saveChildrenOnProvider, ProviderType)
                var result = await manager.SaveHolonAsync(holon,
                    saveChildren: request.SaveChildren,
                    recursive: request.Recursive,
                    maxChildDepth: request.MaxChildDepth,
                    continueOnError: request.ContinueOnError,
                    saveChildrenOnProvider: request.SaveChildrenForAllProviders);

                return result.IsError
                    ? new HolonResponse { IsError = true, Message = result.Message }
                    : new HolonResponse { Holon = MapHolon(result.Result) };
            }
            catch (Exception ex)
            {
                return new HolonResponse { IsError = true, Message = ex.Message };
            }
        }

        public override async Task<HolonResponse> LoadHolon(LoadHolonGrpcRequest request, ServerCallContext context)
        {
            try
            {
                await ActivateProviderIfSpecifiedAsync(request.ProviderType, request.SetGlobally);

                if (!Guid.TryParse(request.Id, out var id))
                    return new HolonResponse { IsError = true, Message = "Invalid holon ID format." };

                var manager = CreateHolonManager();

                // LoadHolonAsync(Guid id, bool loadChildren, bool recursive, int maxChildDepth, bool continueOnError, bool loadChildrenFromProvider, HolonType childHolonType, int version, ProviderType)
                var result = await manager.LoadHolonAsync(id,
                    loadChildren: request.LoadChildren,
                    recursive: request.Recursive,
                    maxChildDepth: request.MaxChildDepth,
                    continueOnError: request.ContinueOnError,
                    version: request.Version);

                return result.IsError
                    ? new HolonResponse { IsError = true, Message = result.Message }
                    : new HolonResponse { Holon = MapHolon(result.Result) };
            }
            catch (Exception ex)
            {
                return new HolonResponse { IsError = true, Message = ex.Message };
            }
        }

        public override async Task<LoadAllHolonsResponse> LoadAllHolons(LoadAllHolonsGrpcRequest request, ServerCallContext context)
        {
            try
            {
                await ActivateProviderIfSpecifiedAsync(request.ProviderType, request.SetGlobally);

                var holonType = ParseEnum(request.HolonType, HolonType.All);
                var manager = CreateHolonManager();

                var result = await manager.LoadAllHolonsAsync(holonType,
                    loadChildren: request.LoadChildren,
                    recursive: request.Recursive,
                    maxChildDepth: request.MaxChildDepth,
                    continueOnError: request.ContinueOnError,
                    version: request.Version);

                if (result.IsError)
                    return new LoadAllHolonsResponse { IsError = true, Message = result.Message };

                var response = new LoadAllHolonsResponse();

                if (result.Result != null)
                    response.Holons.AddRange(result.Result.Select(MapHolon));

                return response;
            }
            catch (Exception ex)
            {
                return new LoadAllHolonsResponse { IsError = true, Message = ex.Message };
            }
        }

        public override async Task<OASISDataResponse> DeleteHolon(DeleteHolonGrpcRequest request, ServerCallContext context)
        {
            try
            {
                await ActivateProviderIfSpecifiedAsync(request.ProviderType, request.SetGlobally);

                if (!Guid.TryParse(request.Id, out var id))
                    return new OASISDataResponse { IsError = true, Message = "Invalid holon ID format." };

                var manager = CreateHolonManager();

                // DeleteHolonAsync(Guid id, Guid avatarId, bool softDelete, ProviderType)
                var result = await manager.DeleteHolonAsync(id, Guid.Empty, request.SoftDelete);

                return new OASISDataResponse { IsError = result.IsError, Message = result.Message };
            }
            catch (Exception ex)
            {
                return new OASISDataResponse { IsError = true, Message = ex.Message };
            }
        }

        public override Task<OASISDataResponse> SaveData(SaveDataGrpcRequest request, ServerCallContext context)
        {
            try
            {
                // AvatarManager.Instance.SaveData is a sync key-value store method
                var result = AvatarManager.Instance.SaveData(request.Key, request.Value, Guid.Empty);

                return Task.FromResult(new OASISDataResponse
                {
                    IsError = result.IsError,
                    Message = result.Message ?? string.Empty
                });
            }
            catch (Exception ex)
            {
                return Task.FromResult(new OASISDataResponse { IsError = true, Message = ex.Message });
            }
        }

        public override Task<LoadDataGrpcResponse> LoadData(LoadDataGrpcRequest request, ServerCallContext context)
        {
            try
            {
                var result = AvatarManager.Instance.LoadData(request.Key, Guid.Empty);

                return Task.FromResult(result.IsError
                    ? new LoadDataGrpcResponse { IsError = true, Message = result.Message }
                    : new LoadDataGrpcResponse { Value = result.Result ?? string.Empty });
            }
            catch (Exception ex)
            {
                return Task.FromResult(new LoadDataGrpcResponse { IsError = true, Message = ex.Message });
            }
        }

        // ── Helpers ─────────────────────────────────────────────────────────────

        private static HolonMessage MapHolon(IHolon holon)
        {
            if (holon == null) return new HolonMessage();

            return new HolonMessage
            {
                Id = holon.Id.ToString(),
                Name = holon.Name ?? string.Empty,
                Description = holon.Description ?? string.Empty,
                HolonType = holon.HolonType.ToString(),
                ParentHolonId = holon.ParentHolonId.ToString(),
                IsActive = holon.IsActive,
                CreatedDate = holon.CreatedDate != default
                    ? Timestamp.FromDateTime(holon.CreatedDate.ToUniversalTime()) : null,
                ModifiedDate = holon.ModifiedDate != default
                    ? Timestamp.FromDateTime(holon.ModifiedDate.ToUniversalTime()) : null
            };
        }

        private static Holon MapToHolon(HolonMessage msg)
        {
            if (msg == null) return new Holon();

            var holon = new Holon
            {
                Name = msg.Name,
                Description = msg.Description,
                IsActive = msg.IsActive
            };

            if (Guid.TryParse(msg.Id, out var id)) holon.Id = id;
            if (Guid.TryParse(msg.ParentHolonId, out var parentId)) holon.ParentHolonId = parentId;
            if (System.Enum.TryParse<HolonType>(msg.HolonType, true, out var ht)) holon.HolonType = ht;

            return holon;
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
