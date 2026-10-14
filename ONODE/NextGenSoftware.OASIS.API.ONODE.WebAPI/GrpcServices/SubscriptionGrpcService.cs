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
        private readonly SubscriptionPlanCatalogue _catalogue;

        public SubscriptionGrpcService(ISubscriptionService subscriptionService, SubscriptionPlanCatalogue catalogue)
        {
            _subscriptionService = subscriptionService;
            _catalogue = catalogue;
        }

        public override async Task<JsonResponse> GetPlans(SubEmptyRequest request, ServerCallContext context)
        {
            try { return new JsonResponse { Json = JsonSerializer.Serialize(await _catalogue.GetAllAsync(context.CancellationToken)) }; }
            catch (Exception ex) { return new JsonResponse { IsError = true, Message = ex.Message }; }
        }

        public override async Task<JsonResponse> GetMySubscriptions(SubByUserRequest request, ServerCallContext context)
        {
            try
            {
                var record = await _subscriptionService.GetSubscriptionAsync(SubscriptionServiceIdentity.RequireSubscriber(context.GetHttpContext(), request.UserId));
                return new JsonResponse { Json = JsonSerializer.Serialize(record) };
            }
            catch (Exception ex) { return new JsonResponse { IsError = true, Message = ex.Message }; }
        }

        public override async Task<JsonResponse> GetMyOrders(SubByUserRequest request, ServerCallContext context)
        {
            try
            {
                var orders = await _subscriptionService.GetOrdersAsync(SubscriptionServiceIdentity.RequireSubscriber(context.GetHttpContext(), request.UserId));
                return new JsonResponse { Json = JsonSerializer.Serialize(orders) };
            }
            catch (Exception ex) { return new JsonResponse { IsError = true, Message = ex.Message }; }
        }

        public override async Task<JsonResponse> GetUsage(SubByUserRequest request, ServerCallContext context)
        {
            try
            {
                var now = DateTime.UtcNow;
                var usage = await _subscriptionService.GetUsageAsync(SubscriptionServiceIdentity.RequireSubscriber(context.GetHttpContext(), request.UserId), now.Year, now.Month);
                return new JsonResponse { Json = JsonSerializer.Serialize(usage) };
            }
            catch (Exception ex) { return new JsonResponse { IsError = true, Message = ex.Message }; }
        }

        public override Task<JsonResponse> GetHyperDriveUsage(SubByUserRequest request, ServerCallContext context)
        {
            return Task.FromResult(new JsonResponse { IsError = true, Message = "HyperDrive usage is available through the authenticated REST subscription API." });
        }

        public override Task<JsonResponse> CheckHyperDriveQuota(CheckQuotaRequest request, ServerCallContext context)
        {
            return Task.FromResult(new JsonResponse { IsError = true, Message = "HyperDrive quota checks require the authenticated REST subscription API." });
        }

        public override Task<JsonResponse> CreateCheckoutSession(CreateCheckoutRequest request, ServerCallContext context)
        {
            return Task.FromResult(new JsonResponse { IsError = true, Message = "CreateCheckoutSession must be called via REST API for Stripe integration." });
        }

        public override async Task<OASISGrpcResponse> TogglePayAsYouGo(TogglePayAsYouGoGrpcRequest request, ServerCallContext context)
        {
            try
            {
                await _subscriptionService.SetPayAsYouGoAsync(SubscriptionServiceIdentity.RequireSubscriber(context.GetHttpContext(), request.UserId), request.Enabled);
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
