using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Grpc.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.ONODE.Core.Managers;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Grpc;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.GrpcServices
{
    public class ONODEGrpcService : ONODEService.ONODEServiceBase
    {
        private static ONODEManager CreateONODEManager()
        {
            var result = System.Threading.Tasks.Task.Run(
                OASISBootLoader.OASISBootLoader.GetAndActivateDefaultStorageProviderAsync).Result;
            return new ONODEManager(result.Result, OASISBootLoader.OASISBootLoader.OASISDNA);
        }

        public override async Task<JsonResponse> GetNodeStatus(ONODEEmptyRequest request, ServerCallContext context)
        {
            try
            {
                var result = await CreateONODEManager().GetNodeStatusAsync();
                return result.IsError
                    ? new JsonResponse { IsError = true, Message = result.Message }
                    : new JsonResponse { Json = JsonSerializer.Serialize(result.Result) };
            }
            catch (Exception ex) { return new JsonResponse { IsError = true, Message = ex.Message }; }
        }

        public override async Task<JsonResponse> GetNodeInfo(ONODEEmptyRequest request, ServerCallContext context)
        {
            try
            {
                var result = await CreateONODEManager().GetNodeInfoAsync();
                return result.IsError
                    ? new JsonResponse { IsError = true, Message = result.Message }
                    : new JsonResponse { Json = JsonSerializer.Serialize(result.Result) };
            }
            catch (Exception ex) { return new JsonResponse { IsError = true, Message = ex.Message }; }
        }

        public override async Task<JsonResponse> GetNodeMetrics(ONODEEmptyRequest request, ServerCallContext context)
        {
            try
            {
                var result = await CreateONODEManager().GetNodeMetricsAsync();
                return result.IsError
                    ? new JsonResponse { IsError = true, Message = result.Message }
                    : new JsonResponse { Json = JsonSerializer.Serialize(result.Result) };
            }
            catch (Exception ex) { return new JsonResponse { IsError = true, Message = ex.Message }; }
        }

        public override async Task<JsonResponse> GetNodeLogs(GetNodeLogsRequest request, ServerCallContext context)
        {
            try
            {
                var result = await CreateONODEManager().GetNodeLogsAsync(request.Lines);
                return result.IsError
                    ? new JsonResponse { IsError = true, Message = result.Message }
                    : new JsonResponse { Json = JsonSerializer.Serialize(result.Result) };
            }
            catch (Exception ex) { return new JsonResponse { IsError = true, Message = ex.Message }; }
        }

        public override async Task<JsonResponse> GetNodeConfig(ONODEEmptyRequest request, ServerCallContext context)
        {
            try
            {
                var result = await CreateONODEManager().GetNodeConfigAsync();
                return result.IsError
                    ? new JsonResponse { IsError = true, Message = result.Message }
                    : new JsonResponse { Json = JsonSerializer.Serialize(result.Result) };
            }
            catch (Exception ex) { return new JsonResponse { IsError = true, Message = ex.Message }; }
        }

        public override async Task<JsonResponse> GetNodePeers(ONODEEmptyRequest request, ServerCallContext context)
        {
            try
            {
                var result = await CreateONODEManager().GetConnectedPeersAsync();
                return result.IsError
                    ? new JsonResponse { IsError = true, Message = result.Message }
                    : new JsonResponse { Json = JsonSerializer.Serialize(result.Result) };
            }
            catch (Exception ex) { return new JsonResponse { IsError = true, Message = ex.Message }; }
        }

        public override async Task<JsonResponse> GetNodeStats(ONODEEmptyRequest request, ServerCallContext context)
        {
            try
            {
                var result = await CreateONODEManager().GetNodeStatsAsync();
                return result.IsError
                    ? new JsonResponse { IsError = true, Message = result.Message }
                    : new JsonResponse { Json = JsonSerializer.Serialize(result.Result) };
            }
            catch (Exception ex) { return new JsonResponse { IsError = true, Message = ex.Message }; }
        }

        public override async Task<OASISGrpcResponse> StartNode(ONODEEmptyRequest request, ServerCallContext context)
        {
            try
            {
                var result = await CreateONODEManager().StartNodeAsync();
                return result.IsError ? new OASISGrpcResponse { IsError = true, Message = result.Message } : new OASISGrpcResponse();
            }
            catch (Exception ex) { return new OASISGrpcResponse { IsError = true, Message = ex.Message }; }
        }

        public override async Task<OASISGrpcResponse> StopNode(ONODEEmptyRequest request, ServerCallContext context)
        {
            try
            {
                var result = await CreateONODEManager().StopNodeAsync();
                return result.IsError ? new OASISGrpcResponse { IsError = true, Message = result.Message } : new OASISGrpcResponse();
            }
            catch (Exception ex) { return new OASISGrpcResponse { IsError = true, Message = ex.Message }; }
        }

        public override async Task<OASISGrpcResponse> RestartNode(ONODEEmptyRequest request, ServerCallContext context)
        {
            try
            {
                var result = await CreateONODEManager().RestartNodeAsync();
                return result.IsError ? new OASISGrpcResponse { IsError = true, Message = result.Message } : new OASISGrpcResponse();
            }
            catch (Exception ex) { return new OASISGrpcResponse { IsError = true, Message = ex.Message }; }
        }

        public override async Task<OASISGrpcResponse> UpdateNodeConfig(UpdateNodeConfigRequest request, ServerCallContext context)
        {
            try
            {
                var config = JsonSerializer.Deserialize<Dictionary<string, object>>(request.ConfigJson) ?? new Dictionary<string, object>();
                var result = await CreateONODEManager().UpdateNodeConfigAsync(config);
                return result.IsError ? new OASISGrpcResponse { IsError = true, Message = result.Message } : new OASISGrpcResponse();
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
