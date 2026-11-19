using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive.Synchronization;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.Edge.NearbySync
{
    /// <summary>
    /// Store-and-forward gateway: relays a nearby peer's exchange to the hosted node while this device is online.
    /// The HttpClient must NOT carry this device's own bearer session. The hosted endpoint authenticates the
    /// request through the originating device's signed offline-session grant carried in the request itself,
    /// so the gateway holds no authority over the peer's avatar.
    /// </summary>
    public sealed class GrantForwardingExchangeHandler : INearbyExchangeHandler
    {
        private readonly HttpClient _anonymousClient;
        private readonly string _exchangePath;

        public GrantForwardingExchangeHandler(HttpClient anonymousClient, string exchangePath = "api/hyperdrive/sync/exchange")
        {
            _anonymousClient = anonymousClient ?? throw new ArgumentNullException(nameof(anonymousClient));
            if (_anonymousClient.BaseAddress == null) throw new ArgumentException("HttpClient.BaseAddress is required.", nameof(anonymousClient));
            _exchangePath = exchangePath;
        }

        public async Task<OASISResult<SyncExchangeResponse>> HandleAsync(SyncExchangeRequest request, CancellationToken cancellationToken)
        {
            var result = new OASISResult<SyncExchangeResponse>();
            if (request.OfflineSessionGrant == null)
            {
                result.IsError = true;
                result.ErrorCode = "NEARBY_GRANT_REQUIRED";
                result.Message = "A gateway can only forward exchanges that carry the originating device's offline-session grant.";
                return result;
            }
            try
            {
                using (var content = new StringContent(HyperDriveJson.Serialize(request), Encoding.UTF8, "application/json"))
                using (var response = await _anonymousClient.PostAsync(_exchangePath, content, cancellationToken).ConfigureAwait(false))
                {
                    var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    var hosted = string.IsNullOrWhiteSpace(json) ? null : HyperDriveJson.Deserialize<OASISResult<SyncExchangeResponse>>(json);
                    if (hosted != null)
                        return hosted;
                    result.IsError = true;
                    result.ErrorCode = "NEARBY_UPSTREAM_HTTP_ERROR";
                    result.Message = $"The hosted exchange endpoint returned HTTP {(int)response.StatusCode}.";
                    return result;
                }
            }
            catch (HttpRequestException ex)
            {
                result.IsError = true;
                result.ErrorCode = "NEARBY_UPSTREAM_UNAVAILABLE";
                result.Message = "The gateway cannot reach the hosted OASIS node.";
                result.Exception = ex;
                return result;
            }
        }
    }
}
