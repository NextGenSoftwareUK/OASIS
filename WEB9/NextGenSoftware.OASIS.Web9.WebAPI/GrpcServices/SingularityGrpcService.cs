using System;
using System.Threading.Tasks;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using NextGenSoftware.OASIS.Web9.Core.Managers;
using NextGenSoftware.OASIS.Web9.Core.Models;
using NextGenSoftware.OASIS.Web9.WebAPI.Grpc;
using CoreLayerStatus = NextGenSoftware.OASIS.Web9.Core.Models.LayerStatus;

namespace NextGenSoftware.OASIS.Web9.WebAPI.GrpcServices
{
    public class SingularityGrpcService : SingularityService.SingularityServiceBase
    {
        private static readonly SingularityAggregationManager _manager = new SingularityAggregationManager();

        public override async Task<GetUnifiedStatusResponse> GetUnifiedStatus(
            GetUnifiedStatusRequest request,
            ServerCallContext context)
        {
            try
            {
                UnifiedStatusReport report = await _manager.GetUnifiedStatusAsync();

                var grpcReport = new Grpc.UnifiedStatusReport
                {
                    AllLayersHealthy = report.AllLayersHealthy,
                    HealthyLayerCount = report.HealthyLayerCount,
                    TotalLayerCount = report.TotalLayerCount,
                    GeneratedUtc = Timestamp.FromDateTime(
                        DateTime.SpecifyKind(report.GeneratedUtc, DateTimeKind.Utc))
                };

                foreach (CoreLayerStatus layer in report.Layers)
                {
                    var grpcLayer = new Grpc.LayerStatus
                    {
                        LayerName = layer.LayerName ?? string.Empty,
                        BaseUrl = layer.BaseUrl ?? string.Empty,
                        IsReachable = layer.IsReachable,
                        ResponseTimeMs = layer.ResponseTimeMs,
                        Error = layer.Error ?? string.Empty,
                        CheckedUtc = Timestamp.FromDateTime(
                            DateTime.SpecifyKind(layer.CheckedUtc, DateTimeKind.Utc))
                    };

                    if (layer.Metrics != null)
                    {
                        foreach (var kvp in layer.Metrics)
                            grpcLayer.Metrics[kvp.Key] = kvp.Value ?? string.Empty;
                    }

                    grpcReport.Layers.Add(grpcLayer);
                }

                return new GetUnifiedStatusResponse
                {
                    IsError = false,
                    Message = string.Empty,
                    Report = grpcReport
                };
            }
            catch (Exception ex)
            {
                return new GetUnifiedStatusResponse
                {
                    IsError = true,
                    Message = ex.Message
                };
            }
        }
    }
}
