using System;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive.Synchronization;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.Edge.NearbySync
{
    /// <summary>
    /// HyperDrive sync transport over the nearby mesh. Plug it into <c>OASISEdgeRuntime</c> exactly as the HTTP
    /// transport is: the runtime's outbox, conflict handling and cursors are unchanged; only the wire differs.
    /// The peer on the other end is a device that can answer exchanges (a gateway with internet, or another
    /// Edge device acting as a store).
    /// </summary>
    public sealed class NearbyPeerSyncTransport : IHyperDriveSyncTransport
    {
        private readonly NearbyNode _node;
        private readonly string _peerNodeId;
        private readonly TimeSpan? _timeout;

        public NearbyPeerSyncTransport(NearbyNode node, string peerNodeId, TimeSpan? timeout = null)
        {
            _node = node ?? throw new ArgumentNullException(nameof(node));
            _peerNodeId = string.IsNullOrWhiteSpace(peerNodeId) ? throw new ArgumentException("A peer node id is required.", nameof(peerNodeId)) : peerNodeId;
            _timeout = timeout;
        }

        public async Task<OASISResult<SyncExchangeResponse>> ExchangeAsync(SyncExchangeRequest request, CancellationToken cancellationToken)
        {
            var result = new OASISResult<SyncExchangeResponse>();
            if (request == null)
                return Fail(result, "NEARBY_EXCHANGE_REQUEST_REQUIRED", "A sync exchange request is required.", null);
            try
            {
                var payload = Encoding.UTF8.GetBytes(HyperDriveJson.Serialize(request));
                var responseBytes = await _node.RequestAsync(_peerNodeId, NearbyChannels.HyperDriveSync, payload, _timeout, cancellationToken).ConfigureAwait(false);
                var serverResult = HyperDriveJson.Deserialize<OASISResult<SyncExchangeResponse>>(Encoding.UTF8.GetString(responseBytes));
                if (serverResult == null || serverResult.IsError || serverResult.Result == null)
                    return Fail(result, serverResult?.ErrorCode ?? "NEARBY_PEER_ERROR",
                        serverResult?.Message ?? "The nearby peer returned no result.", serverResult?.Exception);
                return serverResult;
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                return Fail(result, "NEARBY_PEER_TIMEOUT", "The nearby peer did not answer in time.", null);
            }
            catch (InvalidOperationException ex)
            {
                return Fail(result, "NEARBY_NO_PEERS", ex.Message, ex);
            }
            catch (AggregateException ex)
            {
                return Fail(result, "NEARBY_LINK_FAILED", "Every nearby link failed to send.", ex);
            }
            catch (JsonException ex)
            {
                return Fail(result, "NEARBY_INVALID_RESPONSE", "The nearby peer's response was not valid OASIS JSON.", ex);
            }
        }

        private static OASISResult<SyncExchangeResponse> Fail(OASISResult<SyncExchangeResponse> result, string code, string message, Exception ex)
        {
            result.IsError = true;
            result.ErrorCode = code;
            result.Message = message;
            result.Exception = ex;
            return result;
        }
    }
}
