using System;
using System.Text.Json;
using System.Threading.Tasks;
using Grpc.Core;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.ONODE.Core.Managers;
using NextGenSoftware.OASIS.API.ONODE.Core.Objects;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Grpc;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Interfaces;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.GrpcServices
{
    public class OLandGrpcService : OLandService.OLandServiceBase
    {
        private readonly IOlandService _olandService;

        public OLandGrpcService(IOlandService olandService)
        {
            _olandService = olandService;
        }

        private static OLandManager CreateOLandManager()
            => new OLandManager(new NFTManager(Guid.Empty), Guid.Empty);

        public override async Task<JsonResponse> GetOlandPrice(GetOlandPriceRequest request, ServerCallContext context)
        {
            try
            {
                var result = await CreateOLandManager().GetOlandPriceAsync(request.Count, request.CouponCode);
                return result.IsError ? new JsonResponse { IsError = true, Message = result.Message } : new JsonResponse { Json = result.Result.ToString() };
            }
            catch (Exception ex) { return new JsonResponse { IsError = true, Message = ex.Message }; }
        }

        public override async Task<JsonResponse> PurchaseOland(OlandJsonBodyRequest request, ServerCallContext context)
        {
            try
            {
                var req = JsonSerializer.Deserialize<PurchaseOlandRequest>(request.Json);
                if (req == null) return new JsonResponse { IsError = true, Message = "Invalid request JSON." };
                var result = await CreateOLandManager().PurchaseOlandAsync(req);
                return result.IsError ? new JsonResponse { IsError = true, Message = result.Message } : new JsonResponse { Json = JsonSerializer.Serialize(result.Result) };
            }
            catch (Exception ex) { return new JsonResponse { IsError = true, Message = ex.Message }; }
        }

        public override async Task<JsonResponse> LoadAllOlands(OLandEmptyRequest request, ServerCallContext context)
        {
            try
            {
                var result = await CreateOLandManager().LoadAllOlandsAsync();
                return result.IsError ? new JsonResponse { IsError = true, Message = result.Message } : new JsonResponse { Json = JsonSerializer.Serialize(result.Result) };
            }
            catch (Exception ex) { return new JsonResponse { IsError = true, Message = ex.Message }; }
        }

        public override async Task<JsonResponse> LoadOland(OlandByIdRequest request, ServerCallContext context)
        {
            try
            {
                if (!Guid.TryParse(request.Id, out var id)) return new JsonResponse { IsError = true, Message = "Invalid ID." };
                var result = await CreateOLandManager().LoadOlandAsync(id);
                return result.IsError ? new JsonResponse { IsError = true, Message = result.Message } : new JsonResponse { Json = JsonSerializer.Serialize(result.Result) };
            }
            catch (Exception ex) { return new JsonResponse { IsError = true, Message = ex.Message }; }
        }

        public override async Task<OASISGrpcResponse> DeleteOland(DeleteOlandRequest request, ServerCallContext context)
        {
            try
            {
                if (!Guid.TryParse(request.OlandId, out var olandId) || !Guid.TryParse(request.AvatarId, out var avatarId))
                    return new OASISGrpcResponse { IsError = true, Message = "Invalid IDs." };
                var result = await CreateOLandManager().DeleteOlandAsync(olandId, avatarId);
                return result.IsError ? new OASISGrpcResponse { IsError = true, Message = result.Message } : new OASISGrpcResponse();
            }
            catch (Exception ex) { return new OASISGrpcResponse { IsError = true, Message = ex.Message }; }
        }

        public override async Task<OASISGrpcResponse> SaveOland(OlandJsonBodyRequest request, ServerCallContext context)
        {
            try
            {
                var oland = JsonSerializer.Deserialize<IOLand>(request.Json);
                if (oland == null) return new OASISGrpcResponse { IsError = true, Message = "Invalid OLand JSON." };
                var result = await CreateOLandManager().SaveOlandAsync(oland);
                return result.IsError ? new OASISGrpcResponse { IsError = true, Message = result.Message } : new OASISGrpcResponse();
            }
            catch (Exception ex) { return new OASISGrpcResponse { IsError = true, Message = ex.Message }; }
        }

        public override async Task<OASISGrpcResponse> UpdateOland(OlandJsonBodyRequest request, ServerCallContext context)
        {
            try
            {
                var oland = JsonSerializer.Deserialize<IOLand>(request.Json);
                if (oland == null) return new OASISGrpcResponse { IsError = true, Message = "Invalid OLand JSON." };
                var result = await CreateOLandManager().UpdateOlandAsync(oland);
                return result.IsError ? new OASISGrpcResponse { IsError = true, Message = result.Message } : new OASISGrpcResponse();
            }
            catch (Exception ex) { return new OASISGrpcResponse { IsError = true, Message = ex.Message }; }
        }

        public override async Task<StringResponse> CreateOlandUnit(OlandJsonBodyRequest request, ServerCallContext context)
        {
            try
            {
                var req = JsonSerializer.Deserialize<NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.ManageOlandUnitRequestDto>(request.Json);
                if (req == null) return new StringResponse { IsError = true, Message = "Invalid request JSON." };
                var result = await _olandService.CreateOland(req);
                return result.IsError ? new StringResponse { IsError = true, Message = result.Message } : new StringResponse { Value = result.Result ?? "" };
            }
            catch (Exception ex) { return new StringResponse { IsError = true, Message = ex.Message }; }
        }

        public override async Task<StringResponse> UpdateOlandUnit(UpdateOlandUnitRequest request, ServerCallContext context)
        {
            try
            {
                if (!Guid.TryParse(request.Id, out var id)) return new StringResponse { IsError = true, Message = "Invalid ID." };
                var req = JsonSerializer.Deserialize<NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.ManageOlandUnitRequestDto>(request.Json);
                if (req == null) return new StringResponse { IsError = true, Message = "Invalid request JSON." };
                var result = await _olandService.UpdateOland(req, id);
                return result.IsError ? new StringResponse { IsError = true, Message = result.Message } : new StringResponse { Value = result.Result ?? "" };
            }
            catch (Exception ex) { return new StringResponse { IsError = true, Message = ex.Message }; }
        }

        public override async Task<OASISGrpcResponse> DeleteOlandUnit(OlandByIdRequest request, ServerCallContext context)
        {
            try
            {
                if (!Guid.TryParse(request.Id, out var id)) return new OASISGrpcResponse { IsError = true, Message = "Invalid ID." };
                var result = await _olandService.DeleteOland(id);
                return result.IsError ? new OASISGrpcResponse { IsError = true, Message = result.Message } : new OASISGrpcResponse();
            }
            catch (Exception ex) { return new OASISGrpcResponse { IsError = true, Message = ex.Message }; }
        }

        public override async Task<JsonResponse> GetOlandUnit(OlandByIdRequest request, ServerCallContext context)
        {
            try
            {
                if (!Guid.TryParse(request.Id, out var id)) return new JsonResponse { IsError = true, Message = "Invalid ID." };
                var result = await _olandService.GetOland(id);
                return result.IsError ? new JsonResponse { IsError = true, Message = result.Message } : new JsonResponse { Json = JsonSerializer.Serialize(result.Result) };
            }
            catch (Exception ex) { return new JsonResponse { IsError = true, Message = ex.Message }; }
        }

        public override async Task<JsonResponse> GetAllOlandUnits(OLandEmptyRequest request, ServerCallContext context)
        {
            try
            {
                var result = await _olandService.GetAllOlands();
                return result.IsError ? new JsonResponse { IsError = true, Message = result.Message } : new JsonResponse { Json = JsonSerializer.Serialize(result.Result) };
            }
            catch (Exception ex) { return new JsonResponse { IsError = true, Message = ex.Message }; }
        }
    }
}
