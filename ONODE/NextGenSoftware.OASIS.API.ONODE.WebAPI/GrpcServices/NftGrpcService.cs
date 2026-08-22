using System;
using System.Text.Json;
using System.Threading.Tasks;
using Grpc.Core;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Request;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using NextGenSoftware.OASIS.API.ONODE.Core.Managers;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Grpc;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.GrpcServices
{
    public class NftGrpcService : Grpc.NftService.NftServiceBase
    {
        private static NFTManager CreateNFTManager() => new NFTManager(Guid.Empty);

        public override async Task<JsonResponse> CollectNFT(NftJsonRequest request, ServerCallContext context)
        {
            try
            {
                var req = JsonSerializer.Deserialize<CollectGeoNFTRequest>(request.Json);
                if (req == null) return new JsonResponse { IsError = true, Message = "Invalid request." };
                var result = await CreateNFTManager().CollectNFTAsync(req);
                return result.IsError ? new JsonResponse { IsError = true, Message = result.Message } : new JsonResponse { Json = JsonSerializer.Serialize(result.Result) };
            }
            catch (Exception ex) { return new JsonResponse { IsError = true, Message = ex.Message }; }
        }

        public override async Task<JsonResponse> SendNFT(NftJsonRequest request, ServerCallContext context)
        {
            try
            {
                var req = JsonSerializer.Deserialize<SendWeb4NFTRequest>(request.Json);
                if (req == null) return new JsonResponse { IsError = true, Message = "Invalid request." };
                var result = await CreateNFTManager().SendNFTAsync(Guid.Empty, req);
                return result.IsError ? new JsonResponse { IsError = true, Message = result.Message } : new JsonResponse { Json = JsonSerializer.Serialize(result.Result) };
            }
            catch (Exception ex) { return new JsonResponse { IsError = true, Message = ex.Message }; }
        }

        public override async Task<JsonResponse> MintNft(NftJsonRequest request, ServerCallContext context)
        {
            try
            {
                var req = JsonSerializer.Deserialize<MintWeb4NFTRequest>(request.Json);
                if (req == null) return new JsonResponse { IsError = true, Message = "Invalid request." };
                var result = await CreateNFTManager().MintNftAsync(req);
                return result.IsError ? new JsonResponse { IsError = true, Message = result.Message } : new JsonResponse { Json = JsonSerializer.Serialize(result.Result) };
            }
            catch (Exception ex) { return new JsonResponse { IsError = true, Message = ex.Message }; }
        }

        public override async Task<JsonResponse> RemintNft(NftJsonRequest request, ServerCallContext context)
        {
            try
            {
                var req = JsonSerializer.Deserialize<RemintWeb4NFTRequest>(request.Json);
                if (req == null) return new JsonResponse { IsError = true, Message = "Invalid request." };
                var result = await CreateNFTManager().RemintNftAsync(req);
                return result.IsError ? new JsonResponse { IsError = true, Message = result.Message } : new JsonResponse { Json = JsonSerializer.Serialize(result.Result) };
            }
            catch (Exception ex) { return new JsonResponse { IsError = true, Message = ex.Message }; }
        }

        public override Task<JsonResponse> PlaceGeoNFT(NftJsonRequest request, ServerCallContext context)
        {
            return Task.FromResult(new JsonResponse { IsError = true, Message = "PlaceGeoNFT not yet implemented in NFTManager." });
        }

        public override Task<JsonResponse> MintAndPlaceGeoNFT(NftJsonRequest request, ServerCallContext context)
        {
            return Task.FromResult(new JsonResponse { IsError = true, Message = "MintAndPlaceGeoNFT not yet implemented in NFTManager." });
        }

        public override async Task<JsonResponse> LoadWeb4NftById(NftByIdRequest request, ServerCallContext context)
        {
            try
            {
                if (!Guid.TryParse(request.Id, out var id)) return new JsonResponse { IsError = true, Message = "Invalid ID." };
                var result = await CreateNFTManager().LoadWeb4NftAsync(id);
                return result.IsError ? new JsonResponse { IsError = true, Message = result.Message } : new JsonResponse { Json = JsonSerializer.Serialize(result.Result) };
            }
            catch (Exception ex) { return new JsonResponse { IsError = true, Message = ex.Message }; }
        }

        public override async Task<JsonResponse> LoadWeb4NftByHash(NftByHashRequest request, ServerCallContext context)
        {
            try
            {
                var result = await CreateNFTManager().LoadWeb4NftAsync(request.Hash);
                return result.IsError ? new JsonResponse { IsError = true, Message = result.Message } : new JsonResponse { Json = JsonSerializer.Serialize(result.Result) };
            }
            catch (Exception ex) { return new JsonResponse { IsError = true, Message = ex.Message }; }
        }

        public override Task<JsonResponse> LoadAllWeb4NFTsForAvatar(NftByAvatarRequest request, ServerCallContext context)
        {
            return Task.FromResult(new JsonResponse { IsError = true, Message = "LoadAllWeb4NFTsForAvatar not yet implemented." });
        }

        public override Task<JsonResponse> LoadAllWeb4GeoNFTsForAvatar(NftByAvatarRequest request, ServerCallContext context)
        {
            return Task.FromResult(new JsonResponse { IsError = true, Message = "LoadAllWeb4GeoNFTsForAvatar not yet implemented." });
        }

        public override Task<JsonResponse> LoadAllGeoNFTs(NftEmptyRequest request, ServerCallContext context)
        {
            return Task.FromResult(new JsonResponse { IsError = true, Message = "LoadAllGeoNFTs not yet implemented." });
        }

        public override async Task<JsonResponse> LoadWeb3NftById(NftByIdRequest request, ServerCallContext context)
        {
            try
            {
                if (!Guid.TryParse(request.Id, out var id)) return new JsonResponse { IsError = true, Message = "Invalid ID." };
                var result = await CreateNFTManager().LoadWeb3NftAsync(id);
                return result.IsError ? new JsonResponse { IsError = true, Message = result.Message } : new JsonResponse { Json = JsonSerializer.Serialize(result.Result) };
            }
            catch (Exception ex) { return new JsonResponse { IsError = true, Message = ex.Message }; }
        }

        public override async Task<JsonResponse> LoadWeb3NftByHash(NftByHashRequest request, ServerCallContext context)
        {
            try
            {
                var result = await CreateNFTManager().LoadWeb3NftAsync(request.Hash);
                return result.IsError ? new JsonResponse { IsError = true, Message = result.Message } : new JsonResponse { Json = JsonSerializer.Serialize(result.Result) };
            }
            catch (Exception ex) { return new JsonResponse { IsError = true, Message = ex.Message }; }
        }

        public override async Task<JsonResponse> ImportWeb3NFT(NftJsonRequest request, ServerCallContext context)
        {
            try
            {
                var req = JsonSerializer.Deserialize<ImportWeb3NFTRequest>(request.Json);
                if (req == null) return new JsonResponse { IsError = true, Message = "Invalid request." };
                var result = await CreateNFTManager().ImportWeb3NFTAsync(req);
                return result.IsError ? new JsonResponse { IsError = true, Message = result.Message } : new JsonResponse { Json = JsonSerializer.Serialize(result.Result) };
            }
            catch (Exception ex) { return new JsonResponse { IsError = true, Message = ex.Message }; }
        }

        public override Task<JsonResponse> CreateWeb4NFTCollection(NftJsonRequest request, ServerCallContext context)
        {
            return Task.FromResult(new JsonResponse { IsError = true, Message = "CreateWeb4NFTCollection not yet implemented." });
        }

        public override Task<JsonResponse> SearchWeb4NFTs(SearchNftRequest request, ServerCallContext context)
        {
            return Task.FromResult(new JsonResponse { IsError = true, Message = "SearchWeb4NFTs not yet implemented." });
        }

        public override Task<JsonResponse> LoadAllWeb3NFTsForAvatar(NftByAvatarRequest request, ServerCallContext context)
        {
            return Task.FromResult(new JsonResponse { IsError = true, Message = "LoadAllWeb3NFTsForAvatar not yet implemented." });
        }

        public override Task<JsonResponse> LoadAllWeb3NFTsForMintAddress(NftByAddressRequest request, ServerCallContext context)
        {
            return Task.FromResult(new JsonResponse { IsError = true, Message = "Not yet implemented." });
        }

        public override Task<JsonResponse> LoadAllWeb3NFTs(NftEmptyRequest request, ServerCallContext context)
        {
            return Task.FromResult(new JsonResponse { IsError = true, Message = "Not yet implemented." });
        }

        public override Task<JsonResponse> LoadAllWeb4NFTsForMintAddress(NftByAddressRequest request, ServerCallContext context)
        {
            return Task.FromResult(new JsonResponse { IsError = true, Message = "Not yet implemented." });
        }

        public override Task<JsonResponse> LoadAllGeoNFTsForMintAddress(NftByAddressRequest request, ServerCallContext context)
        {
            return Task.FromResult(new JsonResponse { IsError = true, Message = "Not yet implemented." });
        }

        public override Task<JsonResponse> LoadAllWeb4NFTs(NftEmptyRequest request, ServerCallContext context)
        {
            return Task.FromResult(new JsonResponse { IsError = true, Message = "Not yet implemented." });
        }

        public override Task<JsonResponse> UpdateWeb4Nft(NftJsonRequest request, ServerCallContext context)
        {
            return Task.FromResult(new JsonResponse { IsError = true, Message = "Not yet implemented." });
        }

        public override Task<JsonResponse> ImportWeb4NFTFromFile(ImportNftFromFileRequest request, ServerCallContext context)
        {
            return Task.FromResult(new JsonResponse { IsError = true, Message = "Not yet implemented." });
        }

        public override Task<JsonResponse> ImportWeb4NFT(ImportNftByAvatarRequest request, ServerCallContext context)
        {
            return Task.FromResult(new JsonResponse { IsError = true, Message = "Not yet implemented." });
        }

        public override Task<JsonResponse> ExportWeb4NFTToFile(ExportNftRequest request, ServerCallContext context)
        {
            return Task.FromResult(new JsonResponse { IsError = true, Message = "Not yet implemented." });
        }

        public override Task<JsonResponse> ExportWeb4NFT(ExportNftObjectRequest request, ServerCallContext context)
        {
            return Task.FromResult(new JsonResponse { IsError = true, Message = "Not yet implemented." });
        }

        public override Task<JsonResponse> SearchWeb4GeoNFTs(SearchNftRequest request, ServerCallContext context)
        {
            return Task.FromResult(new JsonResponse { IsError = true, Message = "Not yet implemented." });
        }

        public override Task<JsonResponse> SearchWeb4NFTCollections(SearchNftRequest request, ServerCallContext context)
        {
            return Task.FromResult(new JsonResponse { IsError = true, Message = "Not yet implemented." });
        }
    }
}
