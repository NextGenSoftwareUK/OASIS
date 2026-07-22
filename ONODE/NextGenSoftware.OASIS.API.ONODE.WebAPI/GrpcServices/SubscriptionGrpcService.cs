using System;
using System.Text.Json;
using System.Threading.Tasks;
using Grpc.Core;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Grpc;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Subscription;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.GrpcServices
{
    public class SubscriptionGrpcService : Grpc.SubscriptionService.SubscriptionServiceBase
    {
        private readonly ISubscriptionService _subscriptionService;

        public SubscriptionGrpcService(ISubscriptionService subscriptionService)
        {
            _subscriptionService = subscriptionService;
        }

        public override Task<JsonResponse> GetPlans(SubEmptyRequest request, ServerCallContext context)
        {
            // Plans are hardcoded; return a stub JSON array
            var plans = new[] {
                new { id = "free", name = "Free", priceMonthly = 0 },
                new { id = "bronze", name = "Bronze", priceMonthly = 9 },
                new { id = "silver", name = "Silver", priceMonthly = 29 },
                new { id = "gold", name = "Gold", priceMonthly = 99 }
            };
            return Task.FromResult(new JsonResponse { Json = JsonSerializer.Serialize(plans) });
        }

        public override async Task<JsonResponse> GetMySubscriptions(SubByUserRequest request, ServerCallContext context)
        {
            try
            {
                var record = await _subscriptionService.GetSubscriptionAsync(request.UserId);
                return new JsonResponse { Json = JsonSerializer.Serialize(record) };
            }
            catch (Exception ex) { return new JsonResponse { IsError = true, Message = ex.Message }; }
        }

        public override async Task<JsonResponse> GetMyOrders(SubByUserRequest request, ServerCallContext context)
        {
            try
            {
                var orders = await _subscriptionService.GetOrdersAsync(request.UserId);
                return new JsonResponse { Json = JsonSerializer.Serialize(orders) };
            }
            catch (Exception ex) { return new JsonResponse { IsError = true, Message = ex.Message }; }
        }

        public override async Task<JsonResponse> GetUsage(SubByUserRequest request, ServerCallContext context)
        {
            try
            {
                var now = DateTime.UtcNow;
                var usage = await _subscriptionService.GetUsageAsync(request.UserId, now.Year, now.Month);
                return new JsonResponse { Json = JsonSerializer.Serialize(usage) };
            }
            catch (Exception ex) { return new JsonResponse { IsError = true, Message = ex.Message }; }
        }

        public override Task<JsonResponse> GetHyperDriveUsage(SubByUserRequest request, ServerCallContext context)
        {
            return Task.FromResult(new JsonResponse { Json = "{}" });
        }

        public override Task<JsonResponse> CheckHyperDriveQuota(CheckQuotaRequest request, ServerCallContext context)
        {
            return Task.FromResult(new JsonResponse { Json = "true" });
        }

        public override Task<JsonResponse> CreateCheckoutSession(CreateCheckoutRequest request, ServerCallContext context)
        {
            return Task.FromResult(new JsonResponse { IsError = true, Message = "CreateCheckoutSession must be called via REST API for Stripe integration." });
        }

        public override async Task<OASISGrpcResponse> TogglePayAsYouGo(TogglePayAsYouGoGrpcRequest request, ServerCallContext context)
        {
            try
            {
                await _subscriptionService.SetPayAsYouGoAsync(request.UserId, request.Enabled);
                return new OASISGrpcResponse();
            }
            catch (Exception ex) { return new OASISGrpcResponse { IsError = true, Message = ex.Message }; }
        }

        public override Task<OASISGrpcResponse> UpdateSubscriptionHyperDriveConfig(UpdateSubHyperDriveRequest request, ServerCallContext context)
        {
            return Task.FromResult(new OASISGrpcResponse { IsError = true, Message = "UpdateHyperDriveConfig not yet implemented via gRPC." });
        }
    }
}
