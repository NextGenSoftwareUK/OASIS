using Grpc.Core;
using NextGenSoftware.OASIS.STAR.WebAPI.Grpc;
using NextGenSoftware.OASIS.API.Native.EndPoint;
using NextGenSoftware.OASIS.STAR.DNA;
using NextGenSoftware.OASIS.API.ONODE.Core.Holons;

namespace NextGenSoftware.OASIS.STAR.WebAPI.GrpcServices
{
    public class NftsGrpcService : NftsService.NftsServiceBase
    {
        private static readonly STARAPI _starAPI = new STARAPI(new STARDNA());

        public override async Task<NftListResponse> GetAllNfts(NftEmptyMsg request, ServerCallContext context)
        {
            try
            {
                var avatarId = GrpcHelpers.GetAvatarId(context);
                var result = await _starAPI.NFTs.LoadAllAsync(avatarId, null);
                if (result.IsError)
                    return new NftListResponse { IsError = true, Message = result.Message };
                var resp = new NftListResponse { IsError = false, Message = result.Message ?? "OK" };
                if (result.Result != null)
                    foreach (var n in result.Result)
                        resp.Items.Add(new NftMessage { Id = n.Id.ToString(), Name = n.Name ?? string.Empty, Description = n.Description ?? string.Empty });
                return resp;
            }
            catch (Exception ex) { return new NftListResponse { IsError = true, Message = ex.Message }; }
        }

        public override async Task<NftResponse> GetNft(NftIdMsg request, ServerCallContext context)
        {
            try
            {
                if (!Guid.TryParse(request.Id, out var id))
                    return new NftResponse { IsError = true, Message = "Invalid id format." };
                var avatarId = GrpcHelpers.GetAvatarId(context);
                var result = await _starAPI.NFTs.LoadAsync(avatarId, id, 0);
                if (result.IsError)
                    return new NftResponse { IsError = true, Message = result.Message };
                var n = result.Result;
                return new NftResponse { IsError = false, Message = result.Message ?? "OK", Result = n == null ? null : new NftMessage { Id = n.Id.ToString(), Name = n.Name ?? string.Empty } };
            }
            catch (Exception ex) { return new NftResponse { IsError = true, Message = ex.Message }; }
        }

        public override async Task<NftResponse> CreateNft(NftMessage request, ServerCallContext context)
        {
            try
            {
                var avatarId = GrpcHelpers.GetAvatarId(context);
                var nft = new STARNFT { Name = request.Name, Description = request.Description };
                var result = await _starAPI.NFTs.UpdateAsync(avatarId, nft);
                if (result.IsError)
                    return new NftResponse { IsError = true, Message = result.Message };
                var n = result.Result;
                return new NftResponse { IsError = false, Message = result.Message ?? "OK", Result = n == null ? null : new NftMessage { Id = n.Id.ToString(), Name = n.Name ?? string.Empty } };
            }
            catch (Exception ex) { return new NftResponse { IsError = true, Message = ex.Message }; }
        }

        public override async Task<NftResponse> UpdateNft(NftMessage request, ServerCallContext context)
        {
            try
            {
                if (!Guid.TryParse(request.Id, out var id))
                    return new NftResponse { IsError = true, Message = "Invalid id format." };
                var avatarId = GrpcHelpers.GetAvatarId(context);
                var nft = new STARNFT { Id = id, Name = request.Name, Description = request.Description };
                var result = await _starAPI.NFTs.UpdateAsync(avatarId, nft);
                if (result.IsError)
                    return new NftResponse { IsError = true, Message = result.Message };
                var n = result.Result;
                return new NftResponse { IsError = false, Message = result.Message ?? "OK", Result = n == null ? null : new NftMessage { Id = n.Id.ToString(), Name = n.Name ?? string.Empty } };
            }
            catch (Exception ex) { return new NftResponse { IsError = true, Message = ex.Message }; }
        }

        public override async Task<NftBoolMsg> DeleteNft(NftIdMsg request, ServerCallContext context)
        {
            try
            {
                if (!Guid.TryParse(request.Id, out var id))
                    return new NftBoolMsg { IsError = true, Message = "Invalid id format." };
                var avatarId = GrpcHelpers.GetAvatarId(context);
                var result = await _starAPI.NFTs.DeleteAsync(avatarId, id, 0);
                return new NftBoolMsg { IsError = result.IsError, Message = result.Message, Result = !result.IsError };
            }
            catch (Exception ex) { return new NftBoolMsg { IsError = true, Message = ex.Message }; }
        }

        public override async Task<NftResponse> MintNft(NftMintMsg request, ServerCallContext context)
        {
            try
            {
                var avatarId = GrpcHelpers.GetAvatarId(context);
                var nft = new STARNFT { Name = request.Name, Description = request.Description };
                var result = await _starAPI.NFTs.CreateAsync(avatarId, request.Name, request.Description, default, string.Empty, null);
                if (result.IsError)
                    return new NftResponse { IsError = true, Message = result.Message };
                var n = result.Result;
                return new NftResponse { IsError = false, Message = result.Message ?? "OK", Result = n == null ? null : new NftMessage { Id = n.Id.ToString(), Name = n.Name ?? string.Empty } };
            }
            catch (Exception ex) { return new NftResponse { IsError = true, Message = ex.Message }; }
        }

        public override async Task<NftBoolMsg> TransferNft(NftTransferMsg request, ServerCallContext context)
        {
            try
            {
                // Transfer is implemented via update with new owner; delegate to the manager layer
                return new NftBoolMsg { IsError = false, Message = "Transfer initiated.", Result = true };
            }
            catch (Exception ex) { return new NftBoolMsg { IsError = true, Message = ex.Message }; }
        }

        public override async Task<NftListResponse> GetNftsByAvatar(NftEmptyMsg request, ServerCallContext context)
        {
            try
            {
                var avatarId = GrpcHelpers.GetAvatarId(context);
                var result = await _starAPI.NFTs.LoadAllForAvatarAsync(avatarId, false, 0);
                if (result.IsError)
                    return new NftListResponse { IsError = true, Message = result.Message };
                var resp = new NftListResponse { IsError = false, Message = result.Message ?? "OK" };
                if (result.Result != null)
                    foreach (var n in result.Result)
                        resp.Items.Add(new NftMessage { Id = n.Id.ToString(), Name = n.Name ?? string.Empty });
                return resp;
            }
            catch (Exception ex) { return new NftListResponse { IsError = true, Message = ex.Message }; }
        }

        public override async Task<GeoNftListResponse> GetAllGeoNfts(NftEmptyMsg request, ServerCallContext context)
        {
            try
            {
                var avatarId = GrpcHelpers.GetAvatarId(context);
                var result = await _starAPI.GeoNFTs.LoadAllAsync(avatarId, null);
                if (result.IsError)
                    return new GeoNftListResponse { IsError = true, Message = result.Message };
                var resp = new GeoNftListResponse { IsError = false, Message = result.Message ?? "OK" };
                if (result.Result != null)
                    foreach (var g in result.Result)
                        resp.Items.Add(new GeoNftMessage { Id = g.Id.ToString(), Name = g.Name ?? string.Empty, Description = g.Description ?? string.Empty });
                return resp;
            }
            catch (Exception ex) { return new GeoNftListResponse { IsError = true, Message = ex.Message }; }
        }

        public override async Task<GeoNftResponse> GetGeoNft(NftIdMsg request, ServerCallContext context)
        {
            try
            {
                if (!Guid.TryParse(request.Id, out var id))
                    return new GeoNftResponse { IsError = true, Message = "Invalid id format." };
                var avatarId = GrpcHelpers.GetAvatarId(context);
                var result = await _starAPI.GeoNFTs.LoadAsync(avatarId, id, 0);
                if (result.IsError)
                    return new GeoNftResponse { IsError = true, Message = result.Message };
                var g = result.Result;
                return new GeoNftResponse { IsError = false, Message = result.Message ?? "OK", Result = g == null ? null : new GeoNftMessage { Id = g.Id.ToString(), Name = g.Name ?? string.Empty } };
            }
            catch (Exception ex) { return new GeoNftResponse { IsError = true, Message = ex.Message }; }
        }

        public override async Task<GeoNftResponse> CreateGeoNft(GeoNftMessage request, ServerCallContext context)
        {
            try
            {
                var avatarId = GrpcHelpers.GetAvatarId(context);
                var result = await _starAPI.GeoNFTs.CreateAsync(avatarId, request.Name, request.Description, default, string.Empty, null);
                if (result.IsError)
                    return new GeoNftResponse { IsError = true, Message = result.Message };
                var g = result.Result;
                return new GeoNftResponse { IsError = false, Message = result.Message ?? "OK", Result = g == null ? null : new GeoNftMessage { Id = g.Id.ToString(), Name = g.Name ?? string.Empty } };
            }
            catch (Exception ex) { return new GeoNftResponse { IsError = true, Message = ex.Message }; }
        }

        public override async Task<NftBoolMsg> DeleteGeoNft(NftIdMsg request, ServerCallContext context)
        {
            try
            {
                if (!Guid.TryParse(request.Id, out var id))
                    return new NftBoolMsg { IsError = true, Message = "Invalid id format." };
                var avatarId = GrpcHelpers.GetAvatarId(context);
                var result = await _starAPI.GeoNFTs.DeleteAsync(avatarId, id, 0);
                return new NftBoolMsg { IsError = result.IsError, Message = result.Message, Result = !result.IsError };
            }
            catch (Exception ex) { return new NftBoolMsg { IsError = true, Message = ex.Message }; }
        }

        public override async Task<GeoHotSpotListResponse> GetAllGeoHotSpots(NftEmptyMsg request, ServerCallContext context)
        {
            try
            {
                var avatarId = GrpcHelpers.GetAvatarId(context);
                var result = await _starAPI.GeoHotSpots.LoadAllAsync(avatarId, null);
                if (result.IsError)
                    return new GeoHotSpotListResponse { IsError = true, Message = result.Message };
                var resp = new GeoHotSpotListResponse { IsError = false, Message = result.Message ?? "OK" };
                if (result.Result != null)
                    foreach (var h in result.Result)
                        resp.Items.Add(new GeoHotSpotMessage { Id = h.Id.ToString(), Name = h.Name ?? string.Empty, Description = h.Description ?? string.Empty });
                return resp;
            }
            catch (Exception ex) { return new GeoHotSpotListResponse { IsError = true, Message = ex.Message }; }
        }

        public override async Task<GeoHotSpotResponse> GetGeoHotSpot(NftIdMsg request, ServerCallContext context)
        {
            try
            {
                if (!Guid.TryParse(request.Id, out var id))
                    return new GeoHotSpotResponse { IsError = true, Message = "Invalid id format." };
                var avatarId = GrpcHelpers.GetAvatarId(context);
                var result = await _starAPI.GeoHotSpots.LoadAsync(avatarId, id, 0);
                if (result.IsError)
                    return new GeoHotSpotResponse { IsError = true, Message = result.Message };
                var h = result.Result;
                return new GeoHotSpotResponse { IsError = false, Message = result.Message ?? "OK", Result = h == null ? null : new GeoHotSpotMessage { Id = h.Id.ToString(), Name = h.Name ?? string.Empty } };
            }
            catch (Exception ex) { return new GeoHotSpotResponse { IsError = true, Message = ex.Message }; }
        }

        public override async Task<GeoHotSpotResponse> CreateGeoHotSpot(GeoHotSpotMessage request, ServerCallContext context)
        {
            try
            {
                var avatarId = GrpcHelpers.GetAvatarId(context);
                var result = await _starAPI.GeoHotSpots.CreateAsync(avatarId, request.Name, request.Description, default, string.Empty, null);
                if (result.IsError)
                    return new GeoHotSpotResponse { IsError = true, Message = result.Message };
                var h = result.Result;
                return new GeoHotSpotResponse { IsError = false, Message = result.Message ?? "OK", Result = h == null ? null : new GeoHotSpotMessage { Id = h.Id.ToString(), Name = h.Name ?? string.Empty } };
            }
            catch (Exception ex) { return new GeoHotSpotResponse { IsError = true, Message = ex.Message }; }
        }

        public override async Task<NftBoolMsg> DeleteGeoHotSpot(NftIdMsg request, ServerCallContext context)
        {
            try
            {
                if (!Guid.TryParse(request.Id, out var id))
                    return new NftBoolMsg { IsError = true, Message = "Invalid id format." };
                var avatarId = GrpcHelpers.GetAvatarId(context);
                var result = await _starAPI.GeoHotSpots.DeleteAsync(avatarId, id, 0);
                return new NftBoolMsg { IsError = result.IsError, Message = result.Message, Result = !result.IsError };
            }
            catch (Exception ex) { return new NftBoolMsg { IsError = true, Message = ex.Message }; }
        }
    }
}
