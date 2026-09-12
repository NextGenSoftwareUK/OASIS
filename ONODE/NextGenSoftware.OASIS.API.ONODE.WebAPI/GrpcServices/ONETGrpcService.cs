using System;
using System.Text.Json;
using System.Threading.Tasks;
using Grpc.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.ONODE.Core.Managers;
using NextGenSoftware.OASIS.API.ONODE.Core.Network;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Grpc;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.GrpcServices
{
    public class ONETGrpcService : ONETService.ONETServiceBase
    {
        private static ONETManager CreateONETManager()
        {
            var result = System.Threading.Tasks.Task.Run(
                OASISBootLoader.OASISBootLoader.GetAndActivateDefaultStorageProviderAsync).Result;
            return new ONETManager(result.Result, OASISBootLoader.OASISBootLoader.OASISDNA);
        }

        public override async Task<JsonResponse> GetOASISDNA(ONETEmptyRequest request, ServerCallContext context)
        {
            try
            {
                var result = await CreateONETManager().GetOASISDNAAsync();
                return result.IsError
                    ? new JsonResponse { IsError = true, Message = result.Message }
                    : new JsonResponse { Json = JsonSerializer.Serialize(result.Result) };
            }
            catch (Exception ex) { return new JsonResponse { IsError = true, Message = ex.Message }; }
        }

        public override async Task<JsonResponse> GetNetworkStatus(ONETEmptyRequest request, ServerCallContext context)
        {
            try
            {
                var result = await CreateONETManager().GetNetworkStatusAsync();
                return result.IsError
                    ? new JsonResponse { IsError = true, Message = result.Message }
                    : new JsonResponse { Json = JsonSerializer.Serialize(result.Result) };
            }
            catch (Exception ex) { return new JsonResponse { IsError = true, Message = ex.Message }; }
        }

        public override async Task<JsonResponse> GetNetworkNodes(ONETEmptyRequest request, ServerCallContext context)
        {
            try
            {
                var result = await CreateONETManager().GetConnectedNodesAsync();
                return result.IsError
                    ? new JsonResponse { IsError = true, Message = result.Message }
                    : new JsonResponse { Json = JsonSerializer.Serialize(result.Result) };
            }
            catch (Exception ex) { return new JsonResponse { IsError = true, Message = ex.Message }; }
        }

        public override async Task<JsonResponse> GetNetworkStats(ONETEmptyRequest request, ServerCallContext context)
        {
            try
            {
                var result = await CreateONETManager().GetNetworkStatsAsync();
                return result.IsError
                    ? new JsonResponse { IsError = true, Message = result.Message }
                    : new JsonResponse { Json = JsonSerializer.Serialize(result.Result) };
            }
            catch (Exception ex) { return new JsonResponse { IsError = true, Message = ex.Message }; }
        }

        public override async Task<OASISGrpcResponse> UpdateOASISDNA(UpdateOASISDNARequest request, ServerCallContext context)
        {
            try
            {
                var oasisdna = JsonSerializer.Deserialize<OASISDNA>(request.OasisdnaJson);
                if (oasisdna == null)
                    return new OASISGrpcResponse { IsError = true, Message = "Failed to deserialize OASISDNA." };
                var result = await CreateONETManager().UpdateOASISDNAAsync(oasisdna);
                return result.IsError ? new OASISGrpcResponse { IsError = true, Message = result.Message } : new OASISGrpcResponse();
            }
            catch (Exception ex) { return new OASISGrpcResponse { IsError = true, Message = ex.Message }; }
        }

        public override async Task<OASISGrpcResponse> ConnectToNode(ConnectToNodeRequest request, ServerCallContext context)
        {
            try
            {
                var result = await CreateONETManager().ConnectToNodeAsync(request.NodeId, request.NodeAddress);
                return result.IsError ? new OASISGrpcResponse { IsError = true, Message = result.Message } : new OASISGrpcResponse();
            }
            catch (Exception ex) { return new OASISGrpcResponse { IsError = true, Message = ex.Message }; }
        }

        public override async Task<OASISGrpcResponse> DisconnectFromNode(DisconnectFromNodeRequest request, ServerCallContext context)
        {
            try
            {
                var result = await CreateONETManager().DisconnectFromNodeAsync(request.NodeId);
                return result.IsError ? new OASISGrpcResponse { IsError = true, Message = result.Message } : new OASISGrpcResponse();
            }
            catch (Exception ex) { return new OASISGrpcResponse { IsError = true, Message = ex.Message }; }
        }

        public override async Task<OASISGrpcResponse> StartNetwork(ONETEmptyRequest request, ServerCallContext context)
        {
            try
            {
                var result = await CreateONETManager().StartNetworkAsync();
                return result.IsError ? new OASISGrpcResponse { IsError = true, Message = result.Message } : new OASISGrpcResponse();
            }
            catch (Exception ex) { return new OASISGrpcResponse { IsError = true, Message = ex.Message }; }
        }

        public override async Task<OASISGrpcResponse> StopNetwork(ONETEmptyRequest request, ServerCallContext context)
        {
            try
            {
                var result = await CreateONETManager().StopNetworkAsync();
                return result.IsError ? new OASISGrpcResponse { IsError = true, Message = result.Message } : new OASISGrpcResponse();
            }
            catch (Exception ex) { return new OASISGrpcResponse { IsError = true, Message = ex.Message }; }
        }

        public override async Task<OASISGrpcResponse> BroadcastNetworkMessage(BroadcastNetworkMessageRequest request, ServerCallContext context)
        {
            try
            {
                var result = await CreateONETManager().BroadcastMessageAsync(request.Message, request.MessageType);
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
