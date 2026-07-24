using System;
using System.Text.Json;
using System.Threading.Tasks;
using Grpc.Core;
using NextGenSoftware.OASIS.API.Native.EndPoint;
using NextGenSoftware.OASIS.STAR.DNA;
using NextGenSoftware.OASIS.STAR.WebAPI.Grpc;
using NextGenSoftware.OASIS.API.ONODE.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Enums;

namespace NextGenSoftware.OASIS.STAR.WebAPI.GrpcServices
{
    // ── CelestialBodyMetaData ─────────────────────────────────────────────────
    public class CelestialBodyMetaDataGrpcService : CelestialBodyMetaDataService.CelestialBodyMetaDataServiceBase
    {
        private static readonly STARAPI _api = new STARAPI(new STARDNA());

        public override async Task<MetaListResponse> GetAll(MetaEmptyMsg request, ServerCallContext context)
        {
            try
            {
                var avatarId = GrpcHelpers.GetAvatarId(context);
                var result = await _api.CelestialBodiesMetaDataDNA.LoadAllAsync(avatarId, null);
                if (result.IsError) return new MetaListResponse { IsError = true, Message = result.Message };
                var resp = new MetaListResponse();
                if (result.Result != null)
                    foreach (var item in result.Result)
                        resp.Items.Add(ToMsg(item));
                return resp;
            }
            catch (Exception ex) { return new MetaListResponse { IsError = true, Message = ex.Message }; }
        }

        public override async Task<MetaResponse> Get(MetaIdMsg request, ServerCallContext context)
        {
            try
            {
                if (!Guid.TryParse(request.Id, out var id)) return new MetaResponse { IsError = true, Message = "Invalid ID." };
                var avatarId = GrpcHelpers.GetAvatarId(context);
                var result = await _api.CelestialBodiesMetaDataDNA.LoadAsync(avatarId, id, 0);
                return result.IsError ? new MetaResponse { IsError = true, Message = result.Message } : new MetaResponse { Result = ToMsg(result.Result) };
            }
            catch (Exception ex) { return new MetaResponse { IsError = true, Message = ex.Message }; }
        }

        public override async Task<MetaResponse> Create(MetaDataMsg request, ServerCallContext context)
        {
            try
            {
                var avatarId = GrpcHelpers.GetAvatarId(context);
                var result = await _api.CelestialBodiesMetaDataDNA.CreateAsync(avatarId, request.Name, request.Description, default, string.Empty, null);
                return result.IsError ? new MetaResponse { IsError = true, Message = result.Message } : new MetaResponse { Result = ToMsg(result.Result) };
            }
            catch (Exception ex) { return new MetaResponse { IsError = true, Message = ex.Message }; }
        }

        public override async Task<MetaResponse> Update(MetaDataMsg request, ServerCallContext context)
        {
            try
            {
                if (!Guid.TryParse(request.Id, out var id)) return new MetaResponse { IsError = true, Message = "Invalid ID." };
                var avatarId = GrpcHelpers.GetAvatarId(context);
                var item = new CelestialBodyMetaDataDNA { Id = id, Name = request.Name, Description = request.Description };
                var result = await _api.CelestialBodiesMetaDataDNA.UpdateAsync(avatarId, item);
                return result.IsError ? new MetaResponse { IsError = true, Message = result.Message } : new MetaResponse { Result = ToMsg(result.Result) };
            }
            catch (Exception ex) { return new MetaResponse { IsError = true, Message = ex.Message }; }
        }

        public override async Task<MetaBoolMsg> Delete(MetaIdMsg request, ServerCallContext context)
        {
            try
            {
                if (!Guid.TryParse(request.Id, out var id)) return new MetaBoolMsg { IsError = true, Message = "Invalid ID." };
                var avatarId = GrpcHelpers.GetAvatarId(context);
                var result = await _api.CelestialBodiesMetaDataDNA.DeleteAsync(avatarId, id, 0);
                return result.IsError ? new MetaBoolMsg { IsError = true, Message = result.Message } : new MetaBoolMsg { Value = true };
            }
            catch (Exception ex) { return new MetaBoolMsg { IsError = true, Message = ex.Message }; }
        }

        public override async Task<MetaResponse> Clone(MetaCloneMsg request, ServerCallContext context)
        {
            try
            {
                if (!Guid.TryParse(request.Id, out var id)) return new MetaResponse { IsError = true, Message = "Invalid ID." };
                var avatarId = GrpcHelpers.GetAvatarId(context);
                var result = await _api.CelestialBodiesMetaDataDNA.CloneAsync(avatarId, id, request.NewName);
                return result.IsError ? new MetaResponse { IsError = true, Message = result.Message } : new MetaResponse { Result = ToMsg(result.Result) };
            }
            catch (Exception ex) { return new MetaResponse { IsError = true, Message = ex.Message }; }
        }

        public override async Task<MetaResponse> Publish(MetaPublishMsg request, ServerCallContext context)
        {
            try
            {
                if (!Guid.TryParse(request.Id, out var id)) return new MetaResponse { IsError = true, Message = "Invalid ID." };
                var avatarId = GrpcHelpers.GetAvatarId(context);
                var result = await _api.CelestialBodiesMetaDataDNA.PublishAsync(avatarId, request.PublishPath, string.Empty, string.Empty, false, request.RegisterOnStarnet);
                return result.IsError ? new MetaResponse { IsError = true, Message = result.Message } : new MetaResponse { Result = ToMsg(result.Result) };
            }
            catch (Exception ex) { return new MetaResponse { IsError = true, Message = ex.Message }; }
        }

        public override async Task<MetaListResponse> Search(MetaSearchMsg request, ServerCallContext context)
        {
            try
            {
                var avatarId = GrpcHelpers.GetAvatarId(context);
                var result = await _api.CelestialBodiesMetaDataDNA.SearchAsync<CelestialBodyMetaDataDNA>(avatarId, request.SearchTerm, default, null, MetaKeyValuePairMatchMode.All, true, request.ShowAllVersions, request.Version);
                if (result.IsError) return new MetaListResponse { IsError = true, Message = result.Message };
                var resp = new MetaListResponse();
                if (result.Result != null)
                    foreach (var item in result.Result)
                        resp.Items.Add(ToMsg(item));
                return resp;
            }
            catch (Exception ex) { return new MetaListResponse { IsError = true, Message = ex.Message }; }
        }

        public override async Task<MetaListResponse> GetVersions(MetaIdMsg request, ServerCallContext context)
        {
            try
            {
                if (!Guid.TryParse(request.Id, out var id)) return new MetaListResponse { IsError = true, Message = "Invalid ID." };
                var avatarId = GrpcHelpers.GetAvatarId(context);
                var result = await _api.CelestialBodiesMetaDataDNA.LoadVersionsAsync(id);
                if (result.IsError) return new MetaListResponse { IsError = true, Message = result.Message };
                var resp = new MetaListResponse();
                if (result.Result != null)
                    foreach (var item in result.Result)
                        resp.Items.Add(ToMsg(item));
                return resp;
            }
            catch (Exception ex) { return new MetaListResponse { IsError = true, Message = ex.Message }; }
        }

        private static MetaDataMsg ToMsg(CelestialBodyMetaDataDNA item)
        {
            if (item == null) return new MetaDataMsg();
            return new MetaDataMsg { Id = item.Id.ToString(), Name = item.Name ?? string.Empty, Description = item.Description ?? string.Empty };
        }
    }

    // ── HolonMetaData ─────────────────────────────────────────────────────────
    public class HolonMetaDataGrpcService : HolonMetaDataService.HolonMetaDataServiceBase
    {
        private static readonly STARAPI _api = new STARAPI(new STARDNA());

        public override async Task<MetaListResponse> GetAll(MetaEmptyMsg request, ServerCallContext context)
        {
            try
            {
                var avatarId = GrpcHelpers.GetAvatarId(context);
                var result = await _api.HolonsMetaDataDNA.LoadAllAsync(avatarId, null);
                if (result.IsError) return new MetaListResponse { IsError = true, Message = result.Message };
                var resp = new MetaListResponse();
                if (result.Result != null)
                    foreach (var item in result.Result)
                        resp.Items.Add(ToMsg(item));
                return resp;
            }
            catch (Exception ex) { return new MetaListResponse { IsError = true, Message = ex.Message }; }
        }

        public override async Task<MetaResponse> Get(MetaIdMsg request, ServerCallContext context)
        {
            try
            {
                if (!Guid.TryParse(request.Id, out var id)) return new MetaResponse { IsError = true, Message = "Invalid ID." };
                var avatarId = GrpcHelpers.GetAvatarId(context);
                var result = await _api.HolonsMetaDataDNA.LoadAsync(avatarId, id, 0);
                return result.IsError ? new MetaResponse { IsError = true, Message = result.Message } : new MetaResponse { Result = ToMsg(result.Result) };
            }
            catch (Exception ex) { return new MetaResponse { IsError = true, Message = ex.Message }; }
        }

        public override async Task<MetaResponse> Create(MetaDataMsg request, ServerCallContext context)
        {
            try
            {
                var avatarId = GrpcHelpers.GetAvatarId(context);
                var result = await _api.HolonsMetaDataDNA.CreateAsync(avatarId, request.Name, request.Description, default, string.Empty, null);
                return result.IsError ? new MetaResponse { IsError = true, Message = result.Message } : new MetaResponse { Result = ToMsg(result.Result) };
            }
            catch (Exception ex) { return new MetaResponse { IsError = true, Message = ex.Message }; }
        }

        public override async Task<MetaResponse> Update(MetaDataMsg request, ServerCallContext context)
        {
            try
            {
                if (!Guid.TryParse(request.Id, out var id)) return new MetaResponse { IsError = true, Message = "Invalid ID." };
                var avatarId = GrpcHelpers.GetAvatarId(context);
                var item = new HolonMetaDataDNA { Id = id, Name = request.Name, Description = request.Description };
                var result = await _api.HolonsMetaDataDNA.UpdateAsync(avatarId, item);
                return result.IsError ? new MetaResponse { IsError = true, Message = result.Message } : new MetaResponse { Result = ToMsg(result.Result) };
            }
            catch (Exception ex) { return new MetaResponse { IsError = true, Message = ex.Message }; }
        }

        public override async Task<MetaBoolMsg> Delete(MetaIdMsg request, ServerCallContext context)
        {
            try
            {
                if (!Guid.TryParse(request.Id, out var id)) return new MetaBoolMsg { IsError = true, Message = "Invalid ID." };
                var avatarId = GrpcHelpers.GetAvatarId(context);
                var result = await _api.HolonsMetaDataDNA.DeleteAsync(avatarId, id, 0);
                return result.IsError ? new MetaBoolMsg { IsError = true, Message = result.Message } : new MetaBoolMsg { Value = true };
            }
            catch (Exception ex) { return new MetaBoolMsg { IsError = true, Message = ex.Message }; }
        }

        public override async Task<MetaResponse> Clone(MetaCloneMsg request, ServerCallContext context)
        {
            try
            {
                if (!Guid.TryParse(request.Id, out var id)) return new MetaResponse { IsError = true, Message = "Invalid ID." };
                var avatarId = GrpcHelpers.GetAvatarId(context);
                var result = await _api.HolonsMetaDataDNA.CloneAsync(avatarId, id, request.NewName);
                return result.IsError ? new MetaResponse { IsError = true, Message = result.Message } : new MetaResponse { Result = ToMsg(result.Result) };
            }
            catch (Exception ex) { return new MetaResponse { IsError = true, Message = ex.Message }; }
        }

        public override async Task<MetaResponse> Publish(MetaPublishMsg request, ServerCallContext context)
        {
            try
            {
                if (!Guid.TryParse(request.Id, out var id)) return new MetaResponse { IsError = true, Message = "Invalid ID." };
                var avatarId = GrpcHelpers.GetAvatarId(context);
                var result = await _api.HolonsMetaDataDNA.PublishAsync(avatarId, request.PublishPath, string.Empty, string.Empty, false, request.RegisterOnStarnet);
                return result.IsError ? new MetaResponse { IsError = true, Message = result.Message } : new MetaResponse { Result = ToMsg(result.Result) };
            }
            catch (Exception ex) { return new MetaResponse { IsError = true, Message = ex.Message }; }
        }

        public override async Task<MetaListResponse> Search(MetaSearchMsg request, ServerCallContext context)
        {
            try
            {
                var avatarId = GrpcHelpers.GetAvatarId(context);
                var result = await _api.HolonsMetaDataDNA.SearchAsync<HolonMetaDataDNA>(avatarId, request.SearchTerm, default, null, MetaKeyValuePairMatchMode.All, true, request.ShowAllVersions, request.Version);
                if (result.IsError) return new MetaListResponse { IsError = true, Message = result.Message };
                var resp = new MetaListResponse();
                if (result.Result != null)
                    foreach (var item in result.Result)
                        resp.Items.Add(ToMsg(item));
                return resp;
            }
            catch (Exception ex) { return new MetaListResponse { IsError = true, Message = ex.Message }; }
        }

        public override async Task<MetaListResponse> GetVersions(MetaIdMsg request, ServerCallContext context)
        {
            try
            {
                if (!Guid.TryParse(request.Id, out var id)) return new MetaListResponse { IsError = true, Message = "Invalid ID." };
                var avatarId = GrpcHelpers.GetAvatarId(context);
                var result = await _api.HolonsMetaDataDNA.LoadVersionsAsync(id);
                if (result.IsError) return new MetaListResponse { IsError = true, Message = result.Message };
                var resp = new MetaListResponse();
                if (result.Result != null)
                    foreach (var item in result.Result)
                        resp.Items.Add(ToMsg(item));
                return resp;
            }
            catch (Exception ex) { return new MetaListResponse { IsError = true, Message = ex.Message }; }
        }

        private static MetaDataMsg ToMsg(HolonMetaDataDNA item)
        {
            if (item == null) return new MetaDataMsg();
            return new MetaDataMsg { Id = item.Id.ToString(), Name = item.Name ?? string.Empty, Description = item.Description ?? string.Empty };
        }
    }

    // ── ZomeMetaData ──────────────────────────────────────────────────────────
    public class ZomeMetaDataGrpcService : ZomeMetaDataService.ZomeMetaDataServiceBase
    {
        private static readonly STARAPI _api = new STARAPI(new STARDNA());

        public override async Task<MetaListResponse> GetAll(MetaEmptyMsg request, ServerCallContext context)
        {
            try
            {
                var avatarId = GrpcHelpers.GetAvatarId(context);
                var result = await _api.ZomesMetaDataDNA.LoadAllAsync(avatarId, null);
                if (result.IsError) return new MetaListResponse { IsError = true, Message = result.Message };
                var resp = new MetaListResponse();
                if (result.Result != null)
                    foreach (var item in result.Result)
                        resp.Items.Add(ToMsg(item));
                return resp;
            }
            catch (Exception ex) { return new MetaListResponse { IsError = true, Message = ex.Message }; }
        }

        public override async Task<MetaResponse> Get(MetaIdMsg request, ServerCallContext context)
        {
            try
            {
                if (!Guid.TryParse(request.Id, out var id)) return new MetaResponse { IsError = true, Message = "Invalid ID." };
                var avatarId = GrpcHelpers.GetAvatarId(context);
                var result = await _api.ZomesMetaDataDNA.LoadAsync(avatarId, id, 0);
                return result.IsError ? new MetaResponse { IsError = true, Message = result.Message } : new MetaResponse { Result = ToMsg(result.Result) };
            }
            catch (Exception ex) { return new MetaResponse { IsError = true, Message = ex.Message }; }
        }

        public override async Task<MetaResponse> Create(MetaDataMsg request, ServerCallContext context)
        {
            try
            {
                var avatarId = GrpcHelpers.GetAvatarId(context);
                var result = await _api.ZomesMetaDataDNA.CreateAsync(avatarId, request.Name, request.Description, default, string.Empty, null);
                return result.IsError ? new MetaResponse { IsError = true, Message = result.Message } : new MetaResponse { Result = ToMsg(result.Result) };
            }
            catch (Exception ex) { return new MetaResponse { IsError = true, Message = ex.Message }; }
        }

        public override async Task<MetaResponse> Update(MetaDataMsg request, ServerCallContext context)
        {
            try
            {
                if (!Guid.TryParse(request.Id, out var id)) return new MetaResponse { IsError = true, Message = "Invalid ID." };
                var avatarId = GrpcHelpers.GetAvatarId(context);
                var item = new ZomeMetaDataDNA { Id = id, Name = request.Name, Description = request.Description };
                var result = await _api.ZomesMetaDataDNA.UpdateAsync(avatarId, item);
                return result.IsError ? new MetaResponse { IsError = true, Message = result.Message } : new MetaResponse { Result = ToMsg(result.Result) };
            }
            catch (Exception ex) { return new MetaResponse { IsError = true, Message = ex.Message }; }
        }

        public override async Task<MetaBoolMsg> Delete(MetaIdMsg request, ServerCallContext context)
        {
            try
            {
                if (!Guid.TryParse(request.Id, out var id)) return new MetaBoolMsg { IsError = true, Message = "Invalid ID." };
                var avatarId = GrpcHelpers.GetAvatarId(context);
                var result = await _api.ZomesMetaDataDNA.DeleteAsync(avatarId, id, 0);
                return result.IsError ? new MetaBoolMsg { IsError = true, Message = result.Message } : new MetaBoolMsg { Value = true };
            }
            catch (Exception ex) { return new MetaBoolMsg { IsError = true, Message = ex.Message }; }
        }

        public override async Task<MetaResponse> Clone(MetaCloneMsg request, ServerCallContext context)
        {
            try
            {
                if (!Guid.TryParse(request.Id, out var id)) return new MetaResponse { IsError = true, Message = "Invalid ID." };
                var avatarId = GrpcHelpers.GetAvatarId(context);
                var result = await _api.ZomesMetaDataDNA.CloneAsync(avatarId, id, request.NewName);
                return result.IsError ? new MetaResponse { IsError = true, Message = result.Message } : new MetaResponse { Result = ToMsg(result.Result) };
            }
            catch (Exception ex) { return new MetaResponse { IsError = true, Message = ex.Message }; }
        }

        public override async Task<MetaResponse> Publish(MetaPublishMsg request, ServerCallContext context)
        {
            try
            {
                if (!Guid.TryParse(request.Id, out var id)) return new MetaResponse { IsError = true, Message = "Invalid ID." };
                var avatarId = GrpcHelpers.GetAvatarId(context);
                var result = await _api.ZomesMetaDataDNA.PublishAsync(avatarId, request.PublishPath, string.Empty, string.Empty, false, request.RegisterOnStarnet);
                return result.IsError ? new MetaResponse { IsError = true, Message = result.Message } : new MetaResponse { Result = ToMsg(result.Result) };
            }
            catch (Exception ex) { return new MetaResponse { IsError = true, Message = ex.Message }; }
        }

        public override async Task<MetaListResponse> Search(MetaSearchMsg request, ServerCallContext context)
        {
            try
            {
                var avatarId = GrpcHelpers.GetAvatarId(context);
                var result = await _api.ZomesMetaDataDNA.SearchAsync<ZomeMetaDataDNA>(avatarId, request.SearchTerm, default, null, MetaKeyValuePairMatchMode.All, true, request.ShowAllVersions, request.Version);
                if (result.IsError) return new MetaListResponse { IsError = true, Message = result.Message };
                var resp = new MetaListResponse();
                if (result.Result != null)
                    foreach (var item in result.Result)
                        resp.Items.Add(ToMsg(item));
                return resp;
            }
            catch (Exception ex) { return new MetaListResponse { IsError = true, Message = ex.Message }; }
        }

        public override async Task<MetaListResponse> GetVersions(MetaIdMsg request, ServerCallContext context)
        {
            try
            {
                if (!Guid.TryParse(request.Id, out var id)) return new MetaListResponse { IsError = true, Message = "Invalid ID." };
                var avatarId = GrpcHelpers.GetAvatarId(context);
                var result = await _api.ZomesMetaDataDNA.LoadVersionsAsync(id);
                if (result.IsError) return new MetaListResponse { IsError = true, Message = result.Message };
                var resp = new MetaListResponse();
                if (result.Result != null)
                    foreach (var item in result.Result)
                        resp.Items.Add(ToMsg(item));
                return resp;
            }
            catch (Exception ex) { return new MetaListResponse { IsError = true, Message = ex.Message }; }
        }

        private static MetaDataMsg ToMsg(ZomeMetaDataDNA item)
        {
            if (item == null) return new MetaDataMsg();
            return new MetaDataMsg { Id = item.Id.ToString(), Name = item.Name ?? string.Empty, Description = item.Description ?? string.Empty };
        }
    }
}
