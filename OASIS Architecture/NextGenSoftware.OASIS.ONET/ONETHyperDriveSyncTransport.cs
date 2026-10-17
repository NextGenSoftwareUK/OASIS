using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive.Synchronization;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.ONET
{
    public sealed class ONETHyperDriveSyncTransport : IHyperDriveSyncTransport
    {
        public const string OperationName = "oasis.hyperdrive.sync.exchange.v3";
        private readonly ONETRequestResponseEndpoint _endpoint;
        private readonly string _hostNodeId;
        private readonly IONETCapabilityDirectory _directory;
        private readonly ONETCapabilityQuery _hostQuery;
        private readonly JsonSerializerOptions _jsonOptions;

        public ONETHyperDriveSyncTransport(ONETRequestResponseEndpoint endpoint, string hostNodeId,
            JsonSerializerOptions jsonOptions = null)
        {
            _endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));
            _hostNodeId = !string.IsNullOrWhiteSpace(hostNodeId) ? hostNodeId :
                throw new ArgumentException("A hosted ONODE identifier is required.", nameof(hostNodeId));
            _jsonOptions = jsonOptions ?? new JsonSerializerOptions(JsonSerializerDefaults.Web);
        }

        public ONETHyperDriveSyncTransport(ONETRequestResponseEndpoint endpoint,
            IONETCapabilityDirectory directory, ONETCapabilityQuery hostQuery,
            JsonSerializerOptions jsonOptions = null)
        {
            _endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));
            _directory = directory ?? throw new ArgumentNullException(nameof(directory));
            _hostQuery = hostQuery ?? throw new ArgumentNullException(nameof(hostQuery));
            _jsonOptions = jsonOptions ?? new JsonSerializerOptions(JsonSerializerDefaults.Web);
        }

        public async Task<OASISResult<SyncExchangeResponse>> ExchangeAsync(SyncExchangeRequest request,
            CancellationToken cancellationToken)
        {
            var result = new OASISResult<SyncExchangeResponse>();
            if (request == null) return Failure(result, "ONET_SYNC_REQUEST_REQUIRED", "A sync request is required.");
            string targetNodeId = _hostNodeId;
            if (_directory != null)
            {
                var selected = await _directory.SelectNodeAsync(_hostQuery, cancellationToken).ConfigureAwait(false);
                if (selected == null || selected.IsError || selected.Result == null)
                    return Failure(result, selected?.ErrorCode ?? "ONET_SYNC_HOST_NOT_FOUND",
                        selected?.Message ?? "No verified hosted ONODE is available for synchronization.");
                targetNodeId = selected.Result.NodeId;
            }
            var response = await _endpoint.RequestAsync(targetNodeId, OperationName,
                JsonSerializer.Serialize(request, _jsonOptions), cancellationToken).ConfigureAwait(false);
            if (response == null || response.IsError)
                return Failure(result, response?.ErrorCode ?? "ONET_SYNC_EXCHANGE_FAILED",
                    response?.Message ?? "The ONET synchronization request returned no result.");
            try
            {
                result.Result = JsonSerializer.Deserialize<SyncExchangeResponse>(response.Result, _jsonOptions);
                return result.Result == null
                    ? Failure(result, "ONET_SYNC_RESPONSE_INVALID", "The ONET synchronization response was empty.") : result;
            }
            catch (JsonException ex)
            {
                result.Exception = ex;
                return Failure(result, "ONET_SYNC_RESPONSE_INVALID", "The ONET synchronization response was not valid JSON.");
            }
        }

        private static OASISResult<SyncExchangeResponse> Failure(OASISResult<SyncExchangeResponse> result,
            string code, string message)
        { result.IsError = true; result.ErrorCount = 1; result.ErrorCode = code; result.Message = message; return result; }
    }
}
