using System;
using System.Text.Json;
using System.Threading.Tasks;
using Grpc.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Grpc;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.GrpcServices
{
    public class BridgeGrpcService : BridgeService.BridgeServiceBase
    {
        public override async Task<DoubleResponse> GetExchangeRate(GetExchangeRateRequest request, ServerCallContext context)
        {
            try
            {
                var result = await BridgeManager.Instance.GetExchangeRateAsync(request.FromToken, request.ToToken);
                return result.IsError
                    ? new DoubleResponse { IsError = true, Message = result.Message }
                    : new DoubleResponse { Value = (double)result.Result };
            }
            catch (Exception ex) { return new DoubleResponse { IsError = true, Message = ex.Message }; }
        }

        public override async Task<JsonResponse> GetBridgeOrderBalance(GetBridgeOrderBalanceRequest request, ServerCallContext context)
        {
            try
            {
                if (!Guid.TryParse(request.OrderId, out var orderId))
                    return new JsonResponse { IsError = true, Message = "Invalid order ID." };
                var result = await BridgeManager.Instance.CheckOrderBalanceAsync(orderId);
                return result.IsError
                    ? new JsonResponse { IsError = true, Message = result.Message }
                    : new JsonResponse { Json = JsonSerializer.Serialize(result.Result) };
            }
            catch (Exception ex) { return new JsonResponse { IsError = true, Message = ex.Message }; }
        }

        public override async Task<JsonResponse> CreateBridgeOrder(CreateBridgeOrderRequest request, ServerCallContext context)
        {
            try
            {
                var req = new NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs.CreateBridgeOrderRequest
                {
                    FromToken = request.FromToken,
                    ToToken = request.ToToken,
                    Amount = (decimal)request.Amount
                };
                var result = await BridgeManager.Instance.CreateBridgeOrderAsync(req);
                return result.IsError
                    ? new JsonResponse { IsError = true, Message = result.Message }
                    : new JsonResponse { Json = JsonSerializer.Serialize(result.Result) };
            }
            catch (Exception ex) { return new JsonResponse { IsError = true, Message = ex.Message }; }
        }

        public override async Task<JsonResponse> CreatePrivateBridgeOrder(CreateBridgeOrderRequest request, ServerCallContext context)
        {
            try
            {
                var req = new NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs.CreateBridgeOrderRequest
                {
                    FromToken = request.FromToken,
                    ToToken = request.ToToken,
                    Amount = (decimal)request.Amount,
                    EnableViewingKeyAudit = true,
                    RequireProofVerification = true
                };
                var result = await BridgeManager.Instance.CreateBridgeOrderAsync(req);
                return result.IsError
                    ? new JsonResponse { IsError = true, Message = result.Message }
                    : new JsonResponse { Json = JsonSerializer.Serialize(result.Result) };
            }
            catch (Exception ex) { return new JsonResponse { IsError = true, Message = ex.Message }; }
        }

        public override async Task<OASISGrpcResponse> VerifyBridgeProof(VerifyBridgeProofRequest request, ServerCallContext context)
        {
            try
            {
                var result = await BridgeManager.Instance.VerifyProofAsync(request.ProofPayload, request.ProofType);
                return result.IsError || !result.Result
                    ? new OASISGrpcResponse { IsError = true, Message = result.IsError ? result.Message : "Proof verification failed." }
                    : new OASISGrpcResponse();
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
