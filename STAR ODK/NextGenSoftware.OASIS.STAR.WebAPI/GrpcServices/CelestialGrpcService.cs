using Grpc.Core;
using NextGenSoftware.OASIS.STAR.WebAPI.Grpc;
using NextGenSoftware.OASIS.API.Native.EndPoint;
using NextGenSoftware.OASIS.STAR.DNA;

namespace NextGenSoftware.OASIS.STAR.WebAPI.GrpcServices
{
    public class CelestialGrpcService : CelestialService.CelestialServiceBase
    {
        private static readonly STARAPI _starAPI = new STARAPI(new STARDNA());

        public override async Task<CelestialBodyListResponse> GetAllCelestialBodies(EmptyMsg request, ServerCallContext context)
        {
            try
            {
                var avatarId = GrpcHelpers.GetAvatarId(context);
                var result = await _starAPI.CelestialBodies.LoadAllAsync(avatarId, null);
                if (result.IsError)
                    return new CelestialBodyListResponse { IsError = true, Message = result.Message };
                var resp = new CelestialBodyListResponse { IsError = false, Message = result.Message ?? "OK" };
                if (result.Result != null)
                    foreach (var item in result.Result)
                        resp.Items.Add(new CelestialBodyMessage { Id = item.Id.ToString(), Name = item.Name ?? string.Empty, Description = item.Description ?? string.Empty });
                return resp;
            }
            catch (Exception ex) { return new CelestialBodyListResponse { IsError = true, Message = ex.Message }; }
        }

        public override async Task<CelestialBodyResponse> GetCelestialBody(IdMsg request, ServerCallContext context)
        {
            try
            {
                if (!Guid.TryParse(request.Id, out var id))
                    return new CelestialBodyResponse { IsError = true, Message = "Invalid id format." };
                var avatarId = GrpcHelpers.GetAvatarId(context);
                var result = await _starAPI.CelestialBodies.LoadAsync(avatarId, id, 0);
                if (result.IsError)
                    return new CelestialBodyResponse { IsError = true, Message = result.Message };
                var item = result.Result;
                return new CelestialBodyResponse
                {
                    IsError = false,
                    Message = result.Message ?? "OK",
                    Result = item == null ? null : new CelestialBodyMessage { Id = item.Id.ToString(), Name = item.Name ?? string.Empty, Description = item.Description ?? string.Empty }
                };
            }
            catch (Exception ex) { return new CelestialBodyResponse { IsError = true, Message = ex.Message }; }
        }

        public override async Task<CelestialBodyResponse> CreateCelestialBody(CelestialBodyMessage request, ServerCallContext context)
        {
            try
            {
                var avatarId = GrpcHelpers.GetAvatarId(context);
                var result = await _starAPI.CelestialBodies.CreateAsync(avatarId, request.Name, request.Description, default, string.Empty, null);
                if (result.IsError)
                    return new CelestialBodyResponse { IsError = true, Message = result.Message };
                var item = result.Result;
                return new CelestialBodyResponse
                {
                    IsError = false,
                    Message = result.Message ?? "OK",
                    Result = item == null ? null : new CelestialBodyMessage { Id = item.Id.ToString(), Name = item.Name ?? string.Empty, Description = item.Description ?? string.Empty }
                };
            }
            catch (Exception ex) { return new CelestialBodyResponse { IsError = true, Message = ex.Message }; }
        }

        public override async Task<CelestialBodyResponse> UpdateCelestialBody(CelestialBodyMessage request, ServerCallContext context)
        {
            try
            {
                if (!Guid.TryParse(request.Id, out var id))
                    return new CelestialBodyResponse { IsError = true, Message = "Invalid id format." };
                var avatarId = GrpcHelpers.GetAvatarId(context);
                var body = new NextGenSoftware.OASIS.API.ONODE.Core.Holons.STARCelestialBody { Id = id, Name = request.Name, Description = request.Description };
                var result = await _starAPI.CelestialBodies.UpdateAsync(avatarId, body);
                if (result.IsError)
                    return new CelestialBodyResponse { IsError = true, Message = result.Message };
                var item = result.Result;
                return new CelestialBodyResponse
                {
                    IsError = false,
                    Message = result.Message ?? "OK",
                    Result = item == null ? null : new CelestialBodyMessage { Id = item.Id.ToString(), Name = item.Name ?? string.Empty, Description = item.Description ?? string.Empty }
                };
            }
            catch (Exception ex) { return new CelestialBodyResponse { IsError = true, Message = ex.Message }; }
        }

        public override async Task<BoolMsg> DeleteCelestialBody(IdMsg request, ServerCallContext context)
        {
            try
            {
                if (!Guid.TryParse(request.Id, out var id))
                    return new BoolMsg { IsError = true, Message = "Invalid id format." };
                var avatarId = GrpcHelpers.GetAvatarId(context);
                var result = await _starAPI.CelestialBodies.DeleteAsync(avatarId, id, 0);
                return new BoolMsg { IsError = result.IsError, Message = result.Message, Result = !result.IsError };
            }
            catch (Exception ex) { return new BoolMsg { IsError = true, Message = ex.Message }; }
        }

        public override async Task<CelestialBodyResponse> PublishCelestialBody(PublishMsg request, ServerCallContext context)
        {
            try
            {
                var avatarId = GrpcHelpers.GetAvatarId(context);
                var result = await _starAPI.CelestialBodies.PublishAsync(avatarId, request.SourcePath, request.LaunchTarget, string.Empty, false, true, false, false);
                if (result.IsError)
                    return new CelestialBodyResponse { IsError = true, Message = result.Message };
                var item = result.Result;
                return new CelestialBodyResponse
                {
                    IsError = false,
                    Message = result.Message ?? "OK",
                    Result = item == null ? null : new CelestialBodyMessage { Id = item.Id.ToString(), Name = item.Name ?? string.Empty }
                };
            }
            catch (Exception ex) { return new CelestialBodyResponse { IsError = true, Message = ex.Message }; }
        }

        public override async Task<CelestialBodyResponse> DownloadCelestialBody(DownloadMsg request, ServerCallContext context)
        {
            try
            {
                if (!Guid.TryParse(request.Id, out var id))
                    return new CelestialBodyResponse { IsError = true, Message = "Invalid id format." };
                var avatarId = GrpcHelpers.GetAvatarId(context);
                var result = await _starAPI.CelestialBodies.DownloadAsync(avatarId, id, request.Version, request.DownloadPath, false);
                if (result.IsError)
                    return new CelestialBodyResponse { IsError = true, Message = result.Message };
                var item = result.Result;
                return new CelestialBodyResponse
                {
                    IsError = false,
                    Message = result.Message ?? "OK",
                    Result = item == null ? null : new CelestialBodyMessage { Id = item.Id.ToString(), Name = item.Name ?? string.Empty }
                };
            }
            catch (Exception ex) { return new CelestialBodyResponse { IsError = true, Message = ex.Message }; }
        }

        public override async Task<CelestialBodyResponse> ActivateCelestialBody(VersionedIdMsg request, ServerCallContext context)
        {
            try
            {
                if (!Guid.TryParse(request.Id, out var id))
                    return new CelestialBodyResponse { IsError = true, Message = "Invalid id format." };
                var avatarId = GrpcHelpers.GetAvatarId(context);
                var result = await _starAPI.CelestialBodies.ActivateAsync(avatarId, id, request.Version);
                if (result.IsError)
                    return new CelestialBodyResponse { IsError = true, Message = result.Message };
                var item = result.Result;
                return new CelestialBodyResponse { IsError = false, Message = result.Message ?? "OK", Result = item == null ? null : new CelestialBodyMessage { Id = item.Id.ToString(), Name = item.Name ?? string.Empty } };
            }
            catch (Exception ex) { return new CelestialBodyResponse { IsError = true, Message = ex.Message }; }
        }

        public override async Task<CelestialBodyResponse> DeactivateCelestialBody(VersionedIdMsg request, ServerCallContext context)
        {
            try
            {
                if (!Guid.TryParse(request.Id, out var id))
                    return new CelestialBodyResponse { IsError = true, Message = "Invalid id format." };
                var avatarId = GrpcHelpers.GetAvatarId(context);
                var result = await _starAPI.CelestialBodies.DeactivateAsync(avatarId, id, request.Version);
                if (result.IsError)
                    return new CelestialBodyResponse { IsError = true, Message = result.Message };
                var item = result.Result;
                return new CelestialBodyResponse { IsError = false, Message = result.Message ?? "OK", Result = item == null ? null : new CelestialBodyMessage { Id = item.Id.ToString(), Name = item.Name ?? string.Empty } };
            }
            catch (Exception ex) { return new CelestialBodyResponse { IsError = true, Message = ex.Message }; }
        }

        public override async Task<CelestialSpaceListResponse> GetAllCelestialSpaces(EmptyMsg request, ServerCallContext context)
        {
            try
            {
                var avatarId = GrpcHelpers.GetAvatarId(context);
                var result = await _starAPI.CelestialSpaces.LoadAllAsync(avatarId, null);
                if (result.IsError)
                    return new CelestialSpaceListResponse { IsError = true, Message = result.Message };
                var resp = new CelestialSpaceListResponse { IsError = false, Message = result.Message ?? "OK" };
                if (result.Result != null)
                    foreach (var item in result.Result)
                        resp.Items.Add(new CelestialSpaceMessage { Id = item.Id.ToString(), Name = item.Name ?? string.Empty, Description = item.Description ?? string.Empty });
                return resp;
            }
            catch (Exception ex) { return new CelestialSpaceListResponse { IsError = true, Message = ex.Message }; }
        }

        public override async Task<CelestialSpaceResponse> GetCelestialSpace(IdMsg request, ServerCallContext context)
        {
            try
            {
                if (!Guid.TryParse(request.Id, out var id))
                    return new CelestialSpaceResponse { IsError = true, Message = "Invalid id format." };
                var avatarId = GrpcHelpers.GetAvatarId(context);
                var result = await _starAPI.CelestialSpaces.LoadAsync(avatarId, id, 0);
                if (result.IsError)
                    return new CelestialSpaceResponse { IsError = true, Message = result.Message };
                var item = result.Result;
                return new CelestialSpaceResponse { IsError = false, Message = result.Message ?? "OK", Result = item == null ? null : new CelestialSpaceMessage { Id = item.Id.ToString(), Name = item.Name ?? string.Empty } };
            }
            catch (Exception ex) { return new CelestialSpaceResponse { IsError = true, Message = ex.Message }; }
        }

        public override async Task<CelestialSpaceResponse> CreateCelestialSpace(CelestialSpaceMessage request, ServerCallContext context)
        {
            try
            {
                var avatarId = GrpcHelpers.GetAvatarId(context);
                var result = await _starAPI.CelestialSpaces.CreateAsync(avatarId, request.Name, request.Description, default, string.Empty, null);
                if (result.IsError)
                    return new CelestialSpaceResponse { IsError = true, Message = result.Message };
                var item = result.Result;
                return new CelestialSpaceResponse { IsError = false, Message = result.Message ?? "OK", Result = item == null ? null : new CelestialSpaceMessage { Id = item.Id.ToString(), Name = item.Name ?? string.Empty } };
            }
            catch (Exception ex) { return new CelestialSpaceResponse { IsError = true, Message = ex.Message }; }
        }

        public override async Task<CelestialSpaceResponse> UpdateCelestialSpace(CelestialSpaceMessage request, ServerCallContext context)
        {
            try
            {
                if (!Guid.TryParse(request.Id, out var id))
                    return new CelestialSpaceResponse { IsError = true, Message = "Invalid id format." };
                var avatarId = GrpcHelpers.GetAvatarId(context);
                var space = new NextGenSoftware.OASIS.API.ONODE.Core.Holons.STARCelestialSpace { Id = id, Name = request.Name, Description = request.Description };
                var result = await _starAPI.CelestialSpaces.UpdateAsync(avatarId, space);
                if (result.IsError)
                    return new CelestialSpaceResponse { IsError = true, Message = result.Message };
                var item = result.Result;
                return new CelestialSpaceResponse { IsError = false, Message = result.Message ?? "OK", Result = item == null ? null : new CelestialSpaceMessage { Id = item.Id.ToString(), Name = item.Name ?? string.Empty } };
            }
            catch (Exception ex) { return new CelestialSpaceResponse { IsError = true, Message = ex.Message }; }
        }

        public override async Task<BoolMsg> DeleteCelestialSpace(IdMsg request, ServerCallContext context)
        {
            try
            {
                if (!Guid.TryParse(request.Id, out var id))
                    return new BoolMsg { IsError = true, Message = "Invalid id format." };
                var avatarId = GrpcHelpers.GetAvatarId(context);
                var result = await _starAPI.CelestialSpaces.DeleteAsync(avatarId, id, 0);
                return new BoolMsg { IsError = result.IsError, Message = result.Message, Result = !result.IsError };
            }
            catch (Exception ex) { return new BoolMsg { IsError = true, Message = ex.Message }; }
        }

        public override Task<GenericMsg> GetCosmicStatus(EmptyMsg request, ServerCallContext context)
        {
            try
            {
                var isBooted = OASISBootLoader.OASISBootLoader.IsOASISBooted;
                return Task.FromResult(new GenericMsg { IsError = false, Message = isBooted ? "OASIS booted" : "OASIS not booted" });
            }
            catch (Exception ex) { return Task.FromResult(new GenericMsg { IsError = true, Message = ex.Message }); }
        }
    }
}
