using System;
using System.Threading.Tasks;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using NextGenSoftware.OASIS.Web10.Core.Managers;
using NextGenSoftware.OASIS.Web10.Core.Models;
using NextGenSoftware.OASIS.Web10.WebAPI.Grpc;
using Web9Models = NextGenSoftware.OASIS.Web9.Core.Models;

namespace NextGenSoftware.OASIS.Web10.WebAPI.GrpcServices
{
    public class SourceGrpcService : SourceService.SourceServiceBase
    {
        private static readonly SourceManager _manager = new SourceManager();

        public override async Task<GetSourceResponse> GetSource(
            GetSourceRequest request,
            ServerCallContext context)
        {
            try
            {
                NextGenSoftware.OASIS.Web10.Core.Models.SourceReport report = await _manager.GetSourceAsync();

                var grpcUnified = MapUnifiedStatus(report.UnifiedStatus);

                var grpcReport = new Grpc.SourceReport
                {
                    OasisRuntimeVersion = report.OasisRuntimeVersion ?? string.Empty,
                    OasisApiVersion = report.OasisApiVersion ?? string.Empty,
                    StarApiVersion = report.StarApiVersion ?? string.Empty,
                    UnifiedStatus = grpcUnified,
                    GeneratedUtc = Timestamp.FromDateTime(
                        DateTime.SpecifyKind(report.GeneratedUtc, DateTimeKind.Utc))
                };

                return new GetSourceResponse
                {
                    IsError = false,
                    Message = string.Empty,
                    Report = grpcReport
                };
            }
            catch (Exception ex)
            {
                return new GetSourceResponse
                {
                    IsError = true,
                    Message = ex.Message
                };
            }
        }

        private static Grpc.UnifiedStatusReport MapUnifiedStatus(Web9Models.UnifiedStatusReport report)
        {
            if (report == null)
                return new Grpc.UnifiedStatusReport();

            var grpcReport = new Grpc.UnifiedStatusReport
            {
                AllLayersHealthy = report.AllLayersHealthy,
                HealthyLayerCount = report.HealthyLayerCount,
                TotalLayerCount = report.TotalLayerCount,
                GeneratedUtc = Timestamp.FromDateTime(
                    DateTime.SpecifyKind(report.GeneratedUtc, DateTimeKind.Utc))
            };

            if (report.Layers != null)
            {
                foreach (Web9Models.LayerStatus layer in report.Layers)
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
            }

            return grpcReport;
        }
    }
}
