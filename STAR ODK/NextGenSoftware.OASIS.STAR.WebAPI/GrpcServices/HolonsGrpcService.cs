using Grpc.Core;
using NextGenSoftware.OASIS.STAR.WebAPI.Grpc;
using NextGenSoftware.OASIS.API.Native.EndPoint;
using NextGenSoftware.OASIS.STAR.DNA;
using NextGenSoftware.OASIS.API.ONODE.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Managers;

namespace NextGenSoftware.OASIS.STAR.WebAPI.GrpcServices
{
    public class HolonsGrpcService : HolonsService.HolonsServiceBase
    {
        private static readonly STARAPI _starAPI = new STARAPI(new STARDNA());

        public override async Task<HolonListResponse> GetAllHolons(HolonEmptyMsg request, ServerCallContext context)
        {
            try
            {
                var avatarId = GrpcHelpers.GetAvatarId(context);
                var result = await _starAPI.Holons.LoadAllAsync(avatarId, 0);
                if (result.IsError)
                    return new HolonListResponse { IsError = true, Message = result.Message };
                var resp = new HolonListResponse { IsError = false, Message = result.Message ?? "OK" };
                if (result.Result != null)
                    foreach (var h in result.Result)
                        resp.Items.Add(new HolonMessage { Id = h.Id.ToString(), Name = h.Name ?? string.Empty, Description = h.Description ?? string.Empty });
                return resp;
            }
            catch (Exception ex) { return new HolonListResponse { IsError = true, Message = ex.Message }; }
        }

        public override async Task<HolonResponse> GetHolon(HolonIdMsg request, ServerCallContext context)
        {
            try
            {
                if (!Guid.TryParse(request.Id, out var id))
                    return new HolonResponse { IsError = true, Message = "Invalid id format." };
                var avatarId = GrpcHelpers.GetAvatarId(context);
                var result = await _starAPI.Holons.LoadAsync(avatarId, id, 0);
                if (result.IsError)
                    return new HolonResponse { IsError = true, Message = result.Message };
                var h = result.Result;
                return new HolonResponse { IsError = false, Message = result.Message ?? "OK", Result = h == null ? null : new HolonMessage { Id = h.Id.ToString(), Name = h.Name ?? string.Empty, Description = h.Description ?? string.Empty } };
            }
            catch (Exception ex) { return new HolonResponse { IsError = true, Message = ex.Message }; }
        }

        public override async Task<HolonResponse> CreateHolon(HolonMessage request, ServerCallContext context)
        {
            try
            {
                var avatarId = GrpcHelpers.GetAvatarId(context);
                var holon = new STARHolon { Name = request.Name, Description = request.Description };
                var result = await _starAPI.Holons.UpdateAsync(avatarId, holon);
                if (result.IsError)
                    return new HolonResponse { IsError = true, Message = result.Message };
                var h = result.Result;
                return new HolonResponse { IsError = false, Message = result.Message ?? "OK", Result = h == null ? null : new HolonMessage { Id = h.Id.ToString(), Name = h.Name ?? string.Empty } };
            }
            catch (Exception ex) { return new HolonResponse { IsError = true, Message = ex.Message }; }
        }

        public override async Task<HolonResponse> UpdateHolon(HolonMessage request, ServerCallContext context)
        {
            try
            {
                if (!Guid.TryParse(request.Id, out var id))
                    return new HolonResponse { IsError = true, Message = "Invalid id format." };
                var avatarId = GrpcHelpers.GetAvatarId(context);
                var holon = new STARHolon { Id = id, Name = request.Name, Description = request.Description };
                var result = await _starAPI.Holons.UpdateAsync(avatarId, holon);
                if (result.IsError)
                    return new HolonResponse { IsError = true, Message = result.Message };
                var h = result.Result;
                return new HolonResponse { IsError = false, Message = result.Message ?? "OK", Result = h == null ? null : new HolonMessage { Id = h.Id.ToString(), Name = h.Name ?? string.Empty } };
            }
            catch (Exception ex) { return new HolonResponse { IsError = true, Message = ex.Message }; }
        }

        public override async Task<HolonBoolMsg> DeleteHolon(HolonIdMsg request, ServerCallContext context)
        {
            try
            {
                if (!Guid.TryParse(request.Id, out var id))
                    return new HolonBoolMsg { IsError = true, Message = "Invalid id format." };
                var avatarId = GrpcHelpers.GetAvatarId(context);
                var result = await _starAPI.Holons.DeleteAsync(avatarId, id, 0);
                return new HolonBoolMsg { IsError = result.IsError, Message = result.Message, Result = !result.IsError };
            }
            catch (Exception ex) { return new HolonBoolMsg { IsError = true, Message = ex.Message }; }
        }

        public override async Task<HolonListResponse> GetHolonsByType(HolonTypeMsg request, ServerCallContext context)
        {
            try
            {
                if (!System.Enum.TryParse<HolonType>(request.HolonType, true, out var holonType))
                    return new HolonListResponse { IsError = true, Message = $"Unknown holon type '{request.HolonType}'." };
                var result = await HolonManager.Instance.LoadAllHolonsAsync(holonType);
                if (result.IsError)
                    return new HolonListResponse { IsError = true, Message = result.Message };
                var resp = new HolonListResponse { IsError = false, Message = result.Message ?? "OK" };
                if (result.Result != null)
                    foreach (var h in result.Result)
                        resp.Items.Add(new HolonMessage { Id = h.Id.ToString(), Name = h.Name ?? string.Empty });
                return resp;
            }
            catch (Exception ex) { return new HolonListResponse { IsError = true, Message = ex.Message }; }
        }

        public override async Task<HolonListResponse> GetHolonsByParent(HolonIdMsg request, ServerCallContext context)
        {
            try
            {
                if (!Guid.TryParse(request.Id, out var parentId))
                    return new HolonListResponse { IsError = true, Message = "Invalid id format." };
                var result = await HolonManager.Instance.LoadHolonsForParentAsync(parentId);
                if (result.IsError)
                    return new HolonListResponse { IsError = true, Message = result.Message };
                var resp = new HolonListResponse { IsError = false, Message = result.Message ?? "OK" };
                if (result.Result != null)
                    foreach (var h in result.Result)
                        resp.Items.Add(new HolonMessage { Id = h.Id.ToString(), Name = h.Name ?? string.Empty });
                return resp;
            }
            catch (Exception ex) { return new HolonListResponse { IsError = true, Message = ex.Message }; }
        }

        public override async Task<HolonListResponse> GetHolonsByMetadata(MetadataQuery request, ServerCallContext context)
        {
            try
            {
                var result = await HolonManager.Instance.LoadHolonsByMetaDataAsync(request.Key, request.Value);
                if (result.IsError)
                    return new HolonListResponse { IsError = true, Message = result.Message };
                var resp = new HolonListResponse { IsError = false, Message = result.Message ?? "OK" };
                if (result.Result != null)
                    foreach (var h in result.Result)
                        resp.Items.Add(new HolonMessage { Id = h.Id.ToString(), Name = h.Name ?? string.Empty });
                return resp;
            }
            catch (Exception ex) { return new HolonListResponse { IsError = true, Message = ex.Message }; }
        }

        public override async Task<HolonListResponse> SearchHolons(HolonSearchMsg request, ServerCallContext context)
        {
            try
            {
                var avatarId = GrpcHelpers.GetAvatarId(context);
                var result = await _starAPI.Holons.LoadAllAsync(avatarId, 0);
                if (result.IsError)
                    return new HolonListResponse { IsError = true, Message = result.Message };
                var resp = new HolonListResponse { IsError = false, Message = "OK" };
                if (result.Result != null)
                    foreach (var h in result.Result.Where(h => (h.Name?.Contains(request.Query, StringComparison.OrdinalIgnoreCase) == true) || (h.Description?.Contains(request.Query, StringComparison.OrdinalIgnoreCase) == true)))
                        resp.Items.Add(new HolonMessage { Id = h.Id.ToString(), Name = h.Name ?? string.Empty });
                return resp;
            }
            catch (Exception ex) { return new HolonListResponse { IsError = true, Message = ex.Message }; }
        }

        public override async Task<HolonListResponse> LoadAllHolonsForAvatar(HolonEmptyMsg request, ServerCallContext context)
        {
            try
            {
                var avatarId = GrpcHelpers.GetAvatarId(context);
                var result = await _starAPI.Holons.LoadAllForAvatarAsync(avatarId, false, 0);
                if (result.IsError)
                    return new HolonListResponse { IsError = true, Message = result.Message };
                var resp = new HolonListResponse { IsError = false, Message = result.Message ?? "OK" };
                if (result.Result != null)
                    foreach (var h in result.Result)
                        resp.Items.Add(new HolonMessage { Id = h.Id.ToString(), Name = h.Name ?? string.Empty });
                return resp;
            }
            catch (Exception ex) { return new HolonListResponse { IsError = true, Message = ex.Message }; }
        }

        public override async Task<HolonResponse> PublishHolon(HolonPublishMsg request, ServerCallContext context)
        {
            try
            {
                var avatarId = GrpcHelpers.GetAvatarId(context);
                var result = await _starAPI.Holons.PublishAsync(avatarId, request.SourcePath, request.LaunchTarget, request.PublishPath, false, request.RegisterOnStarnet, false, false);
                if (result.IsError)
                    return new HolonResponse { IsError = true, Message = result.Message };
                var h = result.Result;
                return new HolonResponse { IsError = false, Message = result.Message ?? "OK", Result = h == null ? null : new HolonMessage { Id = h.Id.ToString(), Name = h.Name ?? string.Empty } };
            }
            catch (Exception ex) { return new HolonResponse { IsError = true, Message = ex.Message }; }
        }

        public override async Task<HolonResponse> DownloadHolon(HolonDownloadMsg request, ServerCallContext context)
        {
            try
            {
                if (!Guid.TryParse(request.Id, out var id))
                    return new HolonResponse { IsError = true, Message = "Invalid id format." };
                var avatarId = GrpcHelpers.GetAvatarId(context);
                var result = await _starAPI.Holons.DownloadAsync(avatarId, id, request.Version, request.DownloadPath, request.ReInstall);
                if (result.IsError)
                    return new HolonResponse { IsError = true, Message = result.Message };
                var h = result.Result;
                return new HolonResponse { IsError = false, Message = result.Message ?? "OK", Result = h == null ? null : new HolonMessage { Id = h.Id.ToString(), Name = h.Name ?? string.Empty } };
            }
            catch (Exception ex) { return new HolonResponse { IsError = true, Message = ex.Message }; }
        }

        public override async Task<HolonResponse> ActivateHolon(HolonVersionedIdMsg request, ServerCallContext context)
        {
            try
            {
                if (!Guid.TryParse(request.Id, out var id))
                    return new HolonResponse { IsError = true, Message = "Invalid id format." };
                var avatarId = GrpcHelpers.GetAvatarId(context);
                var result = await _starAPI.Holons.ActivateAsync(avatarId, id, request.Version);
                if (result.IsError)
                    return new HolonResponse { IsError = true, Message = result.Message };
                var h = result.Result;
                return new HolonResponse { IsError = false, Message = result.Message ?? "OK", Result = h == null ? null : new HolonMessage { Id = h.Id.ToString(), Name = h.Name ?? string.Empty } };
            }
            catch (Exception ex) { return new HolonResponse { IsError = true, Message = ex.Message }; }
        }

        public override async Task<HolonResponse> DeactivateHolon(HolonVersionedIdMsg request, ServerCallContext context)
        {
            try
            {
                if (!Guid.TryParse(request.Id, out var id))
                    return new HolonResponse { IsError = true, Message = "Invalid id format." };
                var avatarId = GrpcHelpers.GetAvatarId(context);
                var result = await _starAPI.Holons.DeactivateAsync(avatarId, id, request.Version);
                if (result.IsError)
                    return new HolonResponse { IsError = true, Message = result.Message };
                var h = result.Result;
                return new HolonResponse { IsError = false, Message = result.Message ?? "OK", Result = h == null ? null : new HolonMessage { Id = h.Id.ToString(), Name = h.Name ?? string.Empty } };
            }
            catch (Exception ex) { return new HolonResponse { IsError = true, Message = ex.Message }; }
        }

        public override async Task<HolonResponse> UnpublishHolon(HolonVersionedIdMsg request, ServerCallContext context)
        {
            try
            {
                if (!Guid.TryParse(request.Id, out var id))
                    return new HolonResponse { IsError = true, Message = "Invalid id format." };
                var avatarId = GrpcHelpers.GetAvatarId(context);
                var result = await _starAPI.Holons.UnpublishAsync(avatarId, id, request.Version);
                if (result.IsError)
                    return new HolonResponse { IsError = true, Message = result.Message };
                var h = result.Result;
                return new HolonResponse { IsError = false, Message = result.Message ?? "OK", Result = h == null ? null : new HolonMessage { Id = h.Id.ToString(), Name = h.Name ?? string.Empty } };
            }
            catch (Exception ex) { return new HolonResponse { IsError = true, Message = ex.Message }; }
        }

        public override async Task<HolonResponse> RepublishHolon(HolonVersionedIdMsg request, ServerCallContext context)
        {
            try
            {
                if (!Guid.TryParse(request.Id, out var id))
                    return new HolonResponse { IsError = true, Message = "Invalid id format." };
                var avatarId = GrpcHelpers.GetAvatarId(context);
                var result = await _starAPI.Holons.RepublishAsync(avatarId, id, request.Version);
                if (result.IsError)
                    return new HolonResponse { IsError = true, Message = result.Message };
                var h = result.Result;
                return new HolonResponse { IsError = false, Message = result.Message ?? "OK", Result = h == null ? null : new HolonMessage { Id = h.Id.ToString(), Name = h.Name ?? string.Empty } };
            }
            catch (Exception ex) { return new HolonResponse { IsError = true, Message = ex.Message }; }
        }

        public override async Task<HolonListResponse> GetHolonVersions(HolonIdMsg request, ServerCallContext context)
        {
            try
            {
                if (!Guid.TryParse(request.Id, out var id))
                    return new HolonListResponse { IsError = true, Message = "Invalid id format." };
                var result = await _starAPI.Holons.LoadVersionsAsync(id);
                if (result.IsError)
                    return new HolonListResponse { IsError = true, Message = result.Message };
                var resp = new HolonListResponse { IsError = false, Message = result.Message ?? "OK" };
                if (result.Result != null)
                    foreach (var h in result.Result)
                        resp.Items.Add(new HolonMessage { Id = h.Id.ToString(), Name = h.Name ?? string.Empty });
                return resp;
            }
            catch (Exception ex) { return new HolonListResponse { IsError = true, Message = ex.Message }; }
        }

        public override async Task<ZomeListResponse> GetAllZomes(HolonEmptyMsg request, ServerCallContext context)
        {
            try
            {
                var avatarId = GrpcHelpers.GetAvatarId(context);
                var result = await _starAPI.Zomes.LoadAllAsync(avatarId, null);
                if (result.IsError)
                    return new ZomeListResponse { IsError = true, Message = result.Message };
                var resp = new ZomeListResponse { IsError = false, Message = result.Message ?? "OK" };
                if (result.Result != null)
                    foreach (var z in result.Result)
                        resp.Items.Add(new ZomeMessage { Id = z.Id.ToString(), Name = z.Name ?? string.Empty, Description = z.Description ?? string.Empty });
                return resp;
            }
            catch (Exception ex) { return new ZomeListResponse { IsError = true, Message = ex.Message }; }
        }

        public override async Task<ZomeResponse> GetZome(HolonIdMsg request, ServerCallContext context)
        {
            try
            {
                if (!Guid.TryParse(request.Id, out var id))
                    return new ZomeResponse { IsError = true, Message = "Invalid id format." };
                var avatarId = GrpcHelpers.GetAvatarId(context);
                var result = await _starAPI.Zomes.LoadAsync(avatarId, id, 0);
                if (result.IsError)
                    return new ZomeResponse { IsError = true, Message = result.Message };
                var z = result.Result;
                return new ZomeResponse { IsError = false, Message = result.Message ?? "OK", Result = z == null ? null : new ZomeMessage { Id = z.Id.ToString(), Name = z.Name ?? string.Empty } };
            }
            catch (Exception ex) { return new ZomeResponse { IsError = true, Message = ex.Message }; }
        }

        public override async Task<ZomeResponse> CreateZome(ZomeMessage request, ServerCallContext context)
        {
            try
            {
                var avatarId = GrpcHelpers.GetAvatarId(context);
                var result = await _starAPI.Zomes.CreateAsync(avatarId, request.Name, request.Description, default, string.Empty, null);
                if (result.IsError)
                    return new ZomeResponse { IsError = true, Message = result.Message };
                var z = result.Result;
                return new ZomeResponse { IsError = false, Message = result.Message ?? "OK", Result = z == null ? null : new ZomeMessage { Id = z.Id.ToString(), Name = z.Name ?? string.Empty } };
            }
            catch (Exception ex) { return new ZomeResponse { IsError = true, Message = ex.Message }; }
        }

        public override async Task<ZomeResponse> UpdateZome(ZomeMessage request, ServerCallContext context)
        {
            try
            {
                if (!Guid.TryParse(request.Id, out var id))
                    return new ZomeResponse { IsError = true, Message = "Invalid id format." };
                var avatarId = GrpcHelpers.GetAvatarId(context);
                var zome = new STARZome { Id = id, Name = request.Name, Description = request.Description };
                var result = await _starAPI.Zomes.UpdateAsync(avatarId, zome);
                if (result.IsError)
                    return new ZomeResponse { IsError = true, Message = result.Message };
                var z = result.Result;
                return new ZomeResponse { IsError = false, Message = result.Message ?? "OK", Result = z == null ? null : new ZomeMessage { Id = z.Id.ToString(), Name = z.Name ?? string.Empty } };
            }
            catch (Exception ex) { return new ZomeResponse { IsError = true, Message = ex.Message }; }
        }

        public override async Task<HolonBoolMsg> DeleteZome(HolonIdMsg request, ServerCallContext context)
        {
            try
            {
                if (!Guid.TryParse(request.Id, out var id))
                    return new HolonBoolMsg { IsError = true, Message = "Invalid id format." };
                var avatarId = GrpcHelpers.GetAvatarId(context);
                var result = await _starAPI.Zomes.DeleteAsync(avatarId, id, 0);
                return new HolonBoolMsg { IsError = result.IsError, Message = result.Message, Result = !result.IsError };
            }
            catch (Exception ex) { return new HolonBoolMsg { IsError = true, Message = ex.Message }; }
        }
    }
}
