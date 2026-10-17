using System;
using System.Text.Json;
using System.Threading.Tasks;
using Grpc.Core;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Entities.DTOs.Requests;
using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Infrastructure.Services.Solana;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Grpc;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.GrpcServices
{
    public class SolanaGrpcService : Grpc.SolanaService.SolanaServiceBase
    {
        private readonly ISolanaService _solanaService;

        public SolanaGrpcService(ISolanaService solanaService)
        {
            _solanaService = solanaService;
        }

        public override async Task<JsonResponse> MintSolanaNft(MintSolanaNftRequest request, ServerCallContext context)
        {
            try
            {
                var mintRequest = JsonSerializer.Deserialize<MintWeb3NFTRequest>(request.RequestJson);
                if (mintRequest == null) return new JsonResponse { IsError = true, Message = "Invalid request JSON." };
                var result = await _solanaService.MintNftAsync(mintRequest);
                return result.IsError ? new JsonResponse { IsError = true, Message = result.Message } : new JsonResponse { Json = JsonSerializer.Serialize(result.Result) };
            }
            catch (Exception ex) { return new JsonResponse { IsError = true, Message = ex.Message }; }
        }

        public override async Task<JsonResponse> SendSolanaTransaction(SendSolanaTransactionRequest request, ServerCallContext context)
        {
            try
            {
                var sendRequest = new SendTransactionRequest
                {
                    FromAccount = new NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Entities.DTOs.Common.BaseAccountRequest { PublicKey = request.FromAccount },
                    ToAccount = new NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Entities.DTOs.Common.BaseAccountRequest { PublicKey = request.ToAccount },
                    Amount = (ulong)request.Lamports
                };
                var result = await _solanaService.SendTransaction(sendRequest);
                return result.IsError ? new JsonResponse { IsError = true, Message = result.Message } : new JsonResponse { Json = JsonSerializer.Serialize(result.Result) };
            }
            catch (Exception ex) { return new JsonResponse { IsError = true, Message = ex.Message }; }
        }
    }
}
