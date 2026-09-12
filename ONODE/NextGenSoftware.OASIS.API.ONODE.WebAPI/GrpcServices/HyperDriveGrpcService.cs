using System;
using System.Text.Json;
using System.Threading.Tasks;
using Grpc.Core;
using NextGenSoftware.OASIS.API.Core.Configuration;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Grpc;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.GrpcServices
{
    public class HyperDriveGrpcService : HyperDriveService.HyperDriveServiceBase
    {
        public override async Task<JsonResponse> GetHyperDriveAIRecommendations(HyperDriveEmptyRequest request, ServerCallContext context)
        {
            try
            {
                var result = await AIOptimizationEngine.Instance.GetSmartRecommendationsAsync();
                return new JsonResponse { Json = JsonSerializer.Serialize(result) };
            }
            catch (Exception ex) { return new JsonResponse { IsError = true, Message = ex.Message }; }
        }

        public override async Task<JsonResponse> GetHyperDriveAnalyticsReport(GetHyperDriveAnalyticsReportRequest request, ServerCallContext context)
        {
            try
            {
                ProviderType? pt = !string.IsNullOrWhiteSpace(request.ProviderType) && System.Enum.TryParse<ProviderType>(request.ProviderType, true, out var parsed) ? parsed : (ProviderType?)null;
                var tr = System.Enum.TryParse<TimeRange>(request.TimeRange, true, out var tr2) ? tr2 : TimeRange.Last24Hours;
                var result = await AdvancedAnalyticsEngine.Instance.GetAnalyticsReportAsync(pt, tr);
                return new JsonResponse { Json = JsonSerializer.Serialize(result) };
            }
            catch (Exception ex) { return new JsonResponse { IsError = true, Message = ex.Message }; }
        }

        public override async Task<JsonResponse> GetHyperDriveDashboard(HyperDriveEmptyRequest request, ServerCallContext context)
        {
            try
            {
                var result = await AdvancedAnalyticsEngine.Instance.GetDashboardDataAsync();
                return new JsonResponse { Json = JsonSerializer.Serialize(result) };
            }
            catch (Exception ex) { return new JsonResponse { IsError = true, Message = ex.Message }; }
        }

        public override async Task<JsonResponse> GetHyperDriveFailoverPredictions(HyperDriveEmptyRequest request, ServerCallContext context)
        {
            try
            {
                var result = await PredictiveFailoverEngine.Instance.PredictAndPreventFailuresAsync();
                return new JsonResponse { Json = JsonSerializer.Serialize(result) };
            }
            catch (Exception ex) { return new JsonResponse { IsError = true, Message = ex.Message }; }
        }

        public override async Task<JsonResponse> GetHyperDriveCostOptimizationRecommendations(HyperDriveEmptyRequest request, ServerCallContext context)
        {
            try
            {
                var result = await AdvancedAnalyticsEngine.Instance.GetCostOptimizationRecommendationsAsync();
                return new JsonResponse { Json = JsonSerializer.Serialize(result) };
            }
            catch (Exception ex) { return new JsonResponse { IsError = true, Message = ex.Message }; }
        }

        public override async Task<JsonResponse> GetHyperDriveSecurityRecommendations(HyperDriveEmptyRequest request, ServerCallContext context)
        {
            try
            {
                var result = await AdvancedAnalyticsEngine.Instance.GetSecurityRecommendationsAsync();
                return new JsonResponse { Json = JsonSerializer.Serialize(result) };
            }
            catch (Exception ex) { return new JsonResponse { IsError = true, Message = ex.Message }; }
        }

        public override Task<OASISGrpcResponse> UpdateHyperDriveConfig(UpdateHyperDriveConfigRequest request, ServerCallContext context)
        {
            try
            {
                var config = JsonSerializer.Deserialize<OASISHyperDriveConfig>(request.ConfigJson);
                if (config == null)
                    return Task.FromResult(new OASISGrpcResponse { IsError = true, Message = "Failed to deserialize config." });
                var result = OASISHyperDriveConfigManager.Instance.UpdateConfiguration(config);
                return Task.FromResult(result.IsError ? new OASISGrpcResponse { IsError = true, Message = result.Message } : new OASISGrpcResponse());
            }
            catch (Exception ex) { return Task.FromResult(new OASISGrpcResponse { IsError = true, Message = ex.Message }); }
        }

        public override Task<OASISGrpcResponse> ValidateHyperDriveConfig(HyperDriveEmptyRequest request, ServerCallContext context)
        {
            try
            {
                var result = OASISHyperDriveConfigManager.Instance.ValidateConfiguration();
                return Task.FromResult(result.IsError ? new OASISGrpcResponse { IsError = true, Message = result.Message } : new OASISGrpcResponse());
            }
            catch (Exception ex) { return Task.FromResult(new OASISGrpcResponse { IsError = true, Message = ex.Message }); }
        }

        public override Task<OASISGrpcResponse> ResetHyperDriveConfig(HyperDriveEmptyRequest request, ServerCallContext context)
        {
            try
            {
                var result = OASISHyperDriveConfigManager.Instance.ResetToDefaults();
                return Task.FromResult(result.IsError ? new OASISGrpcResponse { IsError = true, Message = result.Message } : new OASISGrpcResponse());
            }
            catch (Exception ex) { return Task.FromResult(new OASISGrpcResponse { IsError = true, Message = ex.Message }); }
        }

        public override Task<OASISGrpcResponse> ResetHyperDriveProviderMetrics(ResetHyperDriveProviderMetricsRequest request, ServerCallContext context)
        {
            try
            {
                var pt = ParseEnum(request.ProviderType, ProviderType.Default);
                PerformanceMonitor.Instance.ResetMetrics(pt);
                return Task.FromResult(new OASISGrpcResponse());
            }
            catch (Exception ex) { return Task.FromResult(new OASISGrpcResponse { IsError = true, Message = ex.Message }); }
        }

        public override Task<OASISGrpcResponse> ResetAllHyperDriveMetrics(HyperDriveEmptyRequest request, ServerCallContext context)
        {
            try
            {
                PerformanceMonitor.Instance.ResetAllMetrics();
                return Task.FromResult(new OASISGrpcResponse());
            }
            catch (Exception ex) { return Task.FromResult(new OASISGrpcResponse { IsError = true, Message = ex.Message }); }
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
