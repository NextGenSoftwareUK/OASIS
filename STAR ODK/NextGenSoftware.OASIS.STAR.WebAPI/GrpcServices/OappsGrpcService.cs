using Grpc.Core;
using NextGenSoftware.OASIS.STAR.WebAPI.Grpc;
using NextGenSoftware.OASIS.API.Native.EndPoint;
using NextGenSoftware.OASIS.STAR.DNA;
using NextGenSoftware.OASIS.API.ONODE.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Enums;

namespace NextGenSoftware.OASIS.STAR.WebAPI.GrpcServices
{
    public class OappsGrpcService : OappsService.OappsServiceBase
    {
        private static readonly STARAPI _starAPI = new STARAPI(new STARDNA());

        // ── OAPPs ─────────────────────────────────────────────────────────────

        public override async Task<OappListResponse> GetAllOapps(OappEmptyMsg request, ServerCallContext context)
        {
            try
            {
                var avatarId = GrpcHelpers.GetAvatarId(context);
                var result = await _starAPI.OAPPs.LoadAllAsync(avatarId, null);
                if (result.IsError)
                    return new OappListResponse { IsError = true, Message = result.Message };
                var resp = new OappListResponse { IsError = false, Message = result.Message ?? "OK" };
                if (result.Result != null)
                    foreach (var o in result.Result)
                        resp.Items.Add(new OappMessage { Id = o.Id.ToString(), Name = o.Name ?? string.Empty, Description = o.Description ?? string.Empty });
                return resp;
            }
            catch (Exception ex) { return new OappListResponse { IsError = true, Message = ex.Message }; }
        }

        public override async Task<OappResponse> GetOapp(OappIdMsg request, ServerCallContext context)
        {
            try
            {
                if (!Guid.TryParse(request.Id, out var id))
                    return new OappResponse { IsError = true, Message = "Invalid id format." };
                var avatarId = GrpcHelpers.GetAvatarId(context);
                var result = await _starAPI.OAPPs.LoadAsync(avatarId, id, 0);
                if (result.IsError)
                    return new OappResponse { IsError = true, Message = result.Message };
                var o = result.Result;
                return new OappResponse { IsError = false, Message = result.Message ?? "OK", Result = o == null ? null : new OappMessage { Id = o.Id.ToString(), Name = o.Name ?? string.Empty } };
            }
            catch (Exception ex) { return new OappResponse { IsError = true, Message = ex.Message }; }
        }

        public override async Task<OappResponse> CreateOapp(OappMessage request, ServerCallContext context)
        {
            try
            {
                var avatarId = GrpcHelpers.GetAvatarId(context);
                var result = await _starAPI.OAPPs.CreateAsync(avatarId, request.Name, request.Description, default, string.Empty, null);
                if (result.IsError)
                    return new OappResponse { IsError = true, Message = result.Message };
                var o = result.Result;
                return new OappResponse { IsError = false, Message = result.Message ?? "OK", Result = o == null ? null : new OappMessage { Id = o.Id.ToString(), Name = o.Name ?? string.Empty } };
            }
            catch (Exception ex) { return new OappResponse { IsError = true, Message = ex.Message }; }
        }

        public override async Task<OappResponse> UpdateOapp(OappMessage request, ServerCallContext context)
        {
            try
            {
                if (!Guid.TryParse(request.Id, out var id))
                    return new OappResponse { IsError = true, Message = "Invalid id format." };
                var avatarId = GrpcHelpers.GetAvatarId(context);
                var oapp = new OAPP { Id = id, Name = request.Name, Description = request.Description };
                var result = await _starAPI.OAPPs.UpdateAsync(avatarId, oapp);
                if (result.IsError)
                    return new OappResponse { IsError = true, Message = result.Message };
                var o = result.Result;
                return new OappResponse { IsError = false, Message = result.Message ?? "OK", Result = o == null ? null : new OappMessage { Id = o.Id.ToString(), Name = o.Name ?? string.Empty } };
            }
            catch (Exception ex) { return new OappResponse { IsError = true, Message = ex.Message }; }
        }

        public override async Task<OappBoolMsg> DeleteOapp(OappIdMsg request, ServerCallContext context)
        {
            try
            {
                if (!Guid.TryParse(request.Id, out var id))
                    return new OappBoolMsg { IsError = true, Message = "Invalid id format." };
                var avatarId = GrpcHelpers.GetAvatarId(context);
                var result = await _starAPI.OAPPs.DeleteAsync(avatarId, id, 0);
                return new OappBoolMsg { IsError = result.IsError, Message = result.Message, Result = !result.IsError };
            }
            catch (Exception ex) { return new OappBoolMsg { IsError = true, Message = ex.Message }; }
        }

        public override async Task<OappResponse> PublishOapp(OappPublishMsg request, ServerCallContext context)
        {
            try
            {
                var avatarId = GrpcHelpers.GetAvatarId(context);
                var result = await _starAPI.OAPPs.PublishAsync(avatarId, request.SourcePath, request.LaunchTarget, request.PublishPath, false, request.RegisterOnStarnet, false, false);
                if (result.IsError)
                    return new OappResponse { IsError = true, Message = result.Message };
                var o = result.Result;
                return new OappResponse { IsError = false, Message = result.Message ?? "OK", Result = o == null ? null : new OappMessage { Id = o.Id.ToString(), Name = o.Name ?? string.Empty } };
            }
            catch (Exception ex) { return new OappResponse { IsError = true, Message = ex.Message }; }
        }

        public override async Task<OappResponse> DownloadOapp(OappDownloadMsg request, ServerCallContext context)
        {
            try
            {
                if (!Guid.TryParse(request.Id, out var id))
                    return new OappResponse { IsError = true, Message = "Invalid id format." };
                var avatarId = GrpcHelpers.GetAvatarId(context);
                var result = await _starAPI.OAPPs.DownloadAsync(avatarId, id, 0, request.DestinationPath, request.Overwrite);
                if (result.IsError)
                    return new OappResponse { IsError = true, Message = result.Message };
                var o = result.Result;
                return new OappResponse { IsError = false, Message = result.Message ?? "OK", Result = o == null ? null : new OappMessage { Id = o.Id.ToString(), Name = o.Name ?? string.Empty } };
            }
            catch (Exception ex) { return new OappResponse { IsError = true, Message = ex.Message }; }
        }

        public override async Task<OappResponse> ActivateOapp(OappVersionedIdMsg request, ServerCallContext context)
        {
            try
            {
                if (!Guid.TryParse(request.Id, out var id))
                    return new OappResponse { IsError = true, Message = "Invalid id format." };
                var avatarId = GrpcHelpers.GetAvatarId(context);
                var result = await _starAPI.OAPPs.ActivateAsync(avatarId, id, request.Version);
                if (result.IsError)
                    return new OappResponse { IsError = true, Message = result.Message };
                var o = result.Result;
                return new OappResponse { IsError = false, Message = result.Message ?? "OK", Result = o == null ? null : new OappMessage { Id = o.Id.ToString(), Name = o.Name ?? string.Empty } };
            }
            catch (Exception ex) { return new OappResponse { IsError = true, Message = ex.Message }; }
        }

        public override async Task<OappResponse> DeactivateOapp(OappVersionedIdMsg request, ServerCallContext context)
        {
            try
            {
                if (!Guid.TryParse(request.Id, out var id))
                    return new OappResponse { IsError = true, Message = "Invalid id format." };
                var avatarId = GrpcHelpers.GetAvatarId(context);
                var result = await _starAPI.OAPPs.DeactivateAsync(avatarId, id, request.Version);
                if (result.IsError)
                    return new OappResponse { IsError = true, Message = result.Message };
                var o = result.Result;
                return new OappResponse { IsError = false, Message = result.Message ?? "OK", Result = o == null ? null : new OappMessage { Id = o.Id.ToString(), Name = o.Name ?? string.Empty } };
            }
            catch (Exception ex) { return new OappResponse { IsError = true, Message = ex.Message }; }
        }

        public override async Task<OappListResponse> SearchOapps(OappSearchMsg request, ServerCallContext context)
        {
            try
            {
                var avatarId = GrpcHelpers.GetAvatarId(context);
                var result = await _starAPI.OAPPs.SearchAsync<OAPP>(avatarId, request.SearchTerm, default, null, NextGenSoftware.OASIS.API.Core.Enums.MetaKeyValuePairMatchMode.All, true, request.ShowAllVersions, 0);
                if (result.IsError)
                    return new OappListResponse { IsError = true, Message = result.Message };
                var resp = new OappListResponse { IsError = false, Message = result.Message ?? "OK" };
                if (result.Result != null)
                    foreach (var o in result.Result)
                        resp.Items.Add(new OappMessage { Id = o.Id.ToString(), Name = o.Name ?? string.Empty });
                return resp;
            }
            catch (Exception ex) { return new OappListResponse { IsError = true, Message = ex.Message }; }
        }

        public override async Task<OappListResponse> LoadAllOappsForAvatar(OappEmptyMsg request, ServerCallContext context)
        {
            try
            {
                var avatarId = GrpcHelpers.GetAvatarId(context);
                var result = await _starAPI.OAPPs.LoadAllForAvatarAsync(avatarId, false, 0);
                if (result.IsError)
                    return new OappListResponse { IsError = true, Message = result.Message };
                var resp = new OappListResponse { IsError = false, Message = result.Message ?? "OK" };
                if (result.Result != null)
                    foreach (var o in result.Result)
                        resp.Items.Add(new OappMessage { Id = o.Id.ToString(), Name = o.Name ?? string.Empty });
                return resp;
            }
            catch (Exception ex) { return new OappListResponse { IsError = true, Message = ex.Message }; }
        }

        // ── Plugins ───────────────────────────────────────────────────────────

        public override async Task<PluginListResponse> GetAllPlugins(OappEmptyMsg request, ServerCallContext context)
        {
            try
            {
                var avatarId = GrpcHelpers.GetAvatarId(context);
                var result = await _starAPI.Plugins.LoadAllAsync(avatarId, null);
                if (result.IsError)
                    return new PluginListResponse { IsError = true, Message = result.Message };
                var resp = new PluginListResponse { IsError = false, Message = result.Message ?? "OK" };
                if (result.Result != null)
                    foreach (var p in result.Result)
                        resp.Items.Add(new PluginMessage { Id = p.Id.ToString(), Name = p.Name ?? string.Empty, Description = p.Description ?? string.Empty });
                return resp;
            }
            catch (Exception ex) { return new PluginListResponse { IsError = true, Message = ex.Message }; }
        }

        public override async Task<PluginResponse> GetPlugin(OappIdMsg request, ServerCallContext context)
        {
            try
            {
                if (!Guid.TryParse(request.Id, out var id))
                    return new PluginResponse { IsError = true, Message = "Invalid id format." };
                var avatarId = GrpcHelpers.GetAvatarId(context);
                var result = await _starAPI.Plugins.LoadAsync(avatarId, id, 0);
                if (result.IsError)
                    return new PluginResponse { IsError = true, Message = result.Message };
                var p = result.Result;
                return new PluginResponse { IsError = false, Message = result.Message ?? "OK", Result = p == null ? null : new PluginMessage { Id = p.Id.ToString(), Name = p.Name ?? string.Empty } };
            }
            catch (Exception ex) { return new PluginResponse { IsError = true, Message = ex.Message }; }
        }

        public override async Task<PluginResponse> CreatePlugin(PluginMessage request, ServerCallContext context)
        {
            try
            {
                var avatarId = GrpcHelpers.GetAvatarId(context);
                var result = await _starAPI.Plugins.CreateAsync(avatarId, request.Name, request.Description, default, string.Empty, null);
                if (result.IsError)
                    return new PluginResponse { IsError = true, Message = result.Message };
                var p = result.Result;
                return new PluginResponse { IsError = false, Message = result.Message ?? "OK", Result = p == null ? null : new PluginMessage { Id = p.Id.ToString(), Name = p.Name ?? string.Empty } };
            }
            catch (Exception ex) { return new PluginResponse { IsError = true, Message = ex.Message }; }
        }

        public override async Task<OappBoolMsg> DeletePlugin(OappIdMsg request, ServerCallContext context)
        {
            try
            {
                if (!Guid.TryParse(request.Id, out var id))
                    return new OappBoolMsg { IsError = true, Message = "Invalid id format." };
                var avatarId = GrpcHelpers.GetAvatarId(context);
                var result = await _starAPI.Plugins.DeleteAsync(avatarId, id, 0);
                return new OappBoolMsg { IsError = result.IsError, Message = result.Message, Result = !result.IsError };
            }
            catch (Exception ex) { return new OappBoolMsg { IsError = true, Message = ex.Message }; }
        }

        // ── Runtimes ──────────────────────────────────────────────────────────

        public override async Task<RuntimeListResponse> GetAllRuntimes(OappEmptyMsg request, ServerCallContext context)
        {
            try
            {
                var avatarId = GrpcHelpers.GetAvatarId(context);
                var result = await _starAPI.Runtimes.LoadAllAsync(avatarId, null);
                if (result.IsError)
                    return new RuntimeListResponse { IsError = true, Message = result.Message };
                var resp = new RuntimeListResponse { IsError = false, Message = result.Message ?? "OK" };
                if (result.Result != null)
                    foreach (var r in result.Result)
                        resp.Items.Add(new RuntimeMessage { Id = r.Id.ToString(), Name = r.Name ?? string.Empty, Description = r.Description ?? string.Empty });
                return resp;
            }
            catch (Exception ex) { return new RuntimeListResponse { IsError = true, Message = ex.Message }; }
        }

        public override async Task<RuntimeResponse> GetRuntime(OappIdMsg request, ServerCallContext context)
        {
            try
            {
                if (!Guid.TryParse(request.Id, out var id))
                    return new RuntimeResponse { IsError = true, Message = "Invalid id format." };
                var avatarId = GrpcHelpers.GetAvatarId(context);
                var result = await _starAPI.Runtimes.LoadAsync(avatarId, id, 0);
                if (result.IsError)
                    return new RuntimeResponse { IsError = true, Message = result.Message };
                var r = result.Result;
                return new RuntimeResponse { IsError = false, Message = result.Message ?? "OK", Result = r == null ? null : new RuntimeMessage { Id = r.Id.ToString(), Name = r.Name ?? string.Empty } };
            }
            catch (Exception ex) { return new RuntimeResponse { IsError = true, Message = ex.Message }; }
        }

        public override async Task<RuntimeResponse> CreateRuntime(RuntimeMessage request, ServerCallContext context)
        {
            try
            {
                var avatarId = GrpcHelpers.GetAvatarId(context);
                var result = await _starAPI.Runtimes.CreateAsync(avatarId, request.Name, request.Description, default, string.Empty, null);
                if (result.IsError)
                    return new RuntimeResponse { IsError = true, Message = result.Message };
                var r = result.Result;
                return new RuntimeResponse { IsError = false, Message = result.Message ?? "OK", Result = r == null ? null : new RuntimeMessage { Id = r.Id.ToString(), Name = r.Name ?? string.Empty } };
            }
            catch (Exception ex) { return new RuntimeResponse { IsError = true, Message = ex.Message }; }
        }

        public override async Task<OappBoolMsg> DeleteRuntime(OappIdMsg request, ServerCallContext context)
        {
            try
            {
                if (!Guid.TryParse(request.Id, out var id))
                    return new OappBoolMsg { IsError = true, Message = "Invalid id format." };
                var avatarId = GrpcHelpers.GetAvatarId(context);
                var result = await _starAPI.Runtimes.DeleteAsync(avatarId, id, 0);
                return new OappBoolMsg { IsError = result.IsError, Message = result.Message, Result = !result.IsError };
            }
            catch (Exception ex) { return new OappBoolMsg { IsError = true, Message = ex.Message }; }
        }

        // ── Templates ─────────────────────────────────────────────────────────
        // Templates API not yet exposed on STARAPI — stub until available.

        public override Task<TemplateListResponse> GetAllTemplates(OappEmptyMsg request, ServerCallContext context)
            => Task.FromResult(new TemplateListResponse { IsError = true, Message = "Templates API is not yet available via gRPC in this release." });

        public override Task<TemplateResponse> GetTemplate(OappIdMsg request, ServerCallContext context)
            => Task.FromResult(new TemplateResponse { IsError = true, Message = "Templates API is not yet available via gRPC in this release." });

        public override Task<TemplateResponse> CreateTemplate(TemplateMessage request, ServerCallContext context)
            => Task.FromResult(new TemplateResponse { IsError = true, Message = "Templates API is not yet available via gRPC in this release." });

        public override Task<OappBoolMsg> DeleteTemplate(OappIdMsg request, ServerCallContext context)
            => Task.FromResult(new OappBoolMsg { IsError = true, Message = "Templates API is not yet available via gRPC in this release." });

        // ── Libraries ─────────────────────────────────────────────────────────
        // Libraries API not yet exposed on STARAPI — stub until available.

        public override Task<LibraryListResponse> GetAllLibraries(OappEmptyMsg request, ServerCallContext context)
            => Task.FromResult(new LibraryListResponse { IsError = true, Message = "Libraries API is not yet available via gRPC in this release." });

        public override Task<LibraryResponse> GetLibrary(OappIdMsg request, ServerCallContext context)
            => Task.FromResult(new LibraryResponse { IsError = true, Message = "Libraries API is not yet available via gRPC in this release." });

        public override Task<LibraryResponse> CreateLibrary(LibraryMessage request, ServerCallContext context)
            => Task.FromResult(new LibraryResponse { IsError = true, Message = "Libraries API is not yet available via gRPC in this release." });

        public override Task<OappBoolMsg> DeleteLibrary(OappIdMsg request, ServerCallContext context)
            => Task.FromResult(new OappBoolMsg { IsError = true, Message = "Libraries API is not yet available via gRPC in this release." });
    }
}
