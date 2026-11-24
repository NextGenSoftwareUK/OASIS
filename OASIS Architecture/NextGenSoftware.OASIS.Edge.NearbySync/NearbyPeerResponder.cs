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
    /// Answers sync exchanges from trusted nearby peers. The request's claimed avatar must equal the avatar
    /// bound to the peer's verified key, so a trusted peer cannot sync as someone else.
    /// </summary>
    public sealed class NearbyPeerResponder
    {
        private readonly INearbyExchangeHandler _handler;

        public NearbyPeerResponder(NearbyNode node, INearbyExchangeHandler handler)
        {
            if (node == null) throw new ArgumentNullException(nameof(node));
            _handler = handler ?? throw new ArgumentNullException(nameof(handler));
            node.RegisterHandler(NearbyChannels.HyperDriveSync, HandleAsync);
        }

        private async Task<byte[]> HandleAsync(string senderNodeId, Guid senderAvatarId, byte[] payload, CancellationToken cancellationToken)
        {
            var result = new OASISResult<SyncExchangeResponse>();
            try
            {
                var request = HyperDriveJson.Deserialize<SyncExchangeRequest>(Encoding.UTF8.GetString(payload));
                if (request == null)
                    result = Error("NEARBY_EXCHANGE_REQUEST_REQUIRED", "A sync exchange request is required.");
                else if (request.AvatarId != senderAvatarId)
                    result = Error("NEARBY_AVATAR_MISMATCH", "The request avatar does not match the avatar bound to the peer's key.");
                else
                    result = await _handler.HandleAsync(request, cancellationToken).ConfigureAwait(false);
            }
            catch (JsonException)
            {
                result = Error("NEARBY_INVALID_REQUEST", "The request was not valid OASIS JSON.");
            }
            return Encoding.UTF8.GetBytes(HyperDriveJson.Serialize(result));
        }

        private static OASISResult<SyncExchangeResponse> Error(string code, string message) =>
            new OASISResult<SyncExchangeResponse> { IsError = true, ErrorCode = code, Message = message };
    }
}
