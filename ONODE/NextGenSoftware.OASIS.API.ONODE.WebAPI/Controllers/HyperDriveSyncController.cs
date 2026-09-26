using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System;
using Microsoft.AspNetCore.Mvc;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive.Synchronization;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Middleware;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers
{
    [ApiController]
    [Route("api/hyperdrive/sync")]
    public sealed class HyperDriveSyncController : OASISControllerBase
    {
        private readonly IHyperDriveOfflineSessionGrantIssuer _offlineGrantIssuer;

        public HyperDriveSyncController(IHyperDriveOfflineSessionGrantIssuer offlineGrantIssuer = null)
        {
            _offlineGrantIssuer = offlineGrantIssuer;
        }

        [Authorize]
        [HttpPost("offline-session-grant")]
        public async Task<IActionResult> IssueOfflineSessionGrant(
            [FromBody] IssueHyperDriveOfflineSessionGrantRequest request, CancellationToken cancellationToken)
        {
            if (_offlineGrantIssuer == null)
                return OfflineGrantError(HttpStatusCode.ServiceUnavailable, "OFFLINE_GRANT_ISSUANCE_DISABLED",
                    "This ONODE is not configured to issue offline-session grants.");
            var result = await _offlineGrantIssuer.IssueAsync(AvatarId, request, cancellationToken).ConfigureAwait(false);
            if (!result.IsError) return Ok(result);
            HttpStatusCode status = result.ErrorCode == "OFFLINE_GRANT_AUTH_REQUIRED" ? HttpStatusCode.Unauthorized :
                result.ErrorCode == "OFFLINE_GRANT_SCOPE_FORBIDDEN" ? HttpStatusCode.Forbidden : HttpStatusCode.BadRequest;
            return StatusCode((int)status, result);
        }
        [Authorize]
        [HttpPost("onet-peer-binding")]
        public async Task<IActionResult> BindOnetPeer([FromBody] BindHyperDrivePeerRequest request,
            CancellationToken cancellationToken)
        {
            if (!HostedSyncEnabled())
                return PeerBindingError(HttpStatusCode.ServiceUnavailable, "HOSTED_SYNC_DISABLED",
                    "Durable hosted HyperDrive synchronization is disabled on this ONODE.");
            if (request == null || request.DeviceId == Guid.Empty || string.IsNullOrWhiteSpace(request.NodeId) ||
                string.IsNullOrWhiteSpace(request.PublicKey) || string.IsNullOrWhiteSpace(request.Signature))
                return PeerBindingError(HttpStatusCode.BadRequest, "HOSTED_SYNC_PEER_BINDING_INVALID",
                    "DeviceId, NodeId, PublicKey and Signature are required.");

            byte[] publicKeyBytes;
            byte[] signatureBytes;
            try
            {
                publicKeyBytes = Convert.FromBase64String(request.PublicKey);
                signatureBytes = Convert.FromBase64String(request.Signature);
            }
            catch (FormatException)
            {
                return PeerBindingError(HttpStatusCode.BadRequest, "HOSTED_SYNC_PEER_BINDING_ENCODING_INVALID",
                    "PublicKey and Signature must be base64 encoded.");
            }

            if (!string.Equals(HyperDrivePeerBindingProof.DeriveNodeId(publicKeyBytes), request.NodeId, StringComparison.Ordinal))
                return PeerBindingError(HttpStatusCode.BadRequest, "HOSTED_SYNC_PEER_NODE_ID_INVALID",
                    "NodeId must be the lowercase SHA-256 fingerprint of PublicKey.");

            if (!HyperDrivePeerBindingProof.Verify(AvatarId, request.DeviceId, request.NodeId,
                publicKeyBytes, signatureBytes))
                return PeerBindingError(HttpStatusCode.Forbidden, "HOSTED_SYNC_PEER_PROOF_INVALID",
                    "The node binding signature is invalid.");

            var providerResult = await GetAndActivateDefaultStorageProviderAsync().ConfigureAwait(false);
            if (providerResult == null || providerResult.IsError || providerResult.Result == null)
                return PeerBindingError(HttpStatusCode.ServiceUnavailable, "HOSTED_SYNC_PROVIDER_UNAVAILABLE",
                    providerResult?.Message ?? "The configured OASIS storage provider is unavailable.");
            if (!(providerResult.Result is IHostedHyperDrivePeerBindingStore bindingStore))
                return PeerBindingError(HttpStatusCode.ServiceUnavailable, "HOSTED_SYNC_PEER_BINDING_UNSUPPORTED",
                    $"The configured provider '{providerResult.Result.ProviderName}' does not support durable ONET peer bindings.");

            var result = await bindingStore.BindPeerAsync(AvatarId, request.DeviceId, request.NodeId,
                request.PublicKey, cancellationToken).ConfigureAwait(false);
            if (!result.IsError) return Ok(result);
            return StatusCode(result.ErrorCode == "MONGO_PEER_ALREADY_BOUND" ? (int)HttpStatusCode.Conflict :
                (int)HttpStatusCode.ServiceUnavailable, result);
        }

        [HttpPost("exchange")]
        public async Task<IActionResult> Exchange([FromBody] SyncExchangeRequest request, CancellationToken cancellationToken)
        {
            if (!HostedSyncEnabled())
                return SyncError(HttpStatusCode.ServiceUnavailable, "HOSTED_SYNC_DISABLED",
                    "Durable hosted HyperDrive synchronization is disabled on this ONODE.");
            if (HttpContext.Items.ContainsKey(JwtMiddleware.AuthenticationErrorItemKey))
                return SyncError(HttpStatusCode.Unauthorized, "HOSTED_SYNC_BEARER_INVALID",
                    "The supplied bearer token is invalid. Remove it when authenticating with a signed offline-session grant, or log in again.");
            Guid authenticatedAvatarId = AvatarId;
            if (authenticatedAvatarId == Guid.Empty)
            {
                if (_offlineGrantIssuer == null)
                    return SyncError(HttpStatusCode.Unauthorized, "HOSTED_SYNC_AUTH_REQUIRED",
                        "A hosted session or signed offline-session grant is required.");
                var offlineIdentity = await _offlineGrantIssuer.ValidateAsync(request?.OfflineSessionGrant,
                    "hyperdrive.sync", request?.DeviceId ?? Guid.Empty, cancellationToken).ConfigureAwait(false);
                if (offlineIdentity == null || offlineIdentity.IsError || offlineIdentity.Result == Guid.Empty)
                    return SyncError(HttpStatusCode.Unauthorized,
                        offlineIdentity?.ErrorCode ?? "HOSTED_SYNC_AUTH_REQUIRED",
                        offlineIdentity?.Message ?? "The signed offline-session grant is invalid.");
                authenticatedAvatarId = offlineIdentity.Result;
            }
            else if (request?.OfflineSessionGrant != null && request.OfflineSessionGrant.AvatarId != authenticatedAvatarId)
                return SyncError(HttpStatusCode.Forbidden, "HOSTED_SYNC_OFFLINE_GRANT_AVATAR_MISMATCH",
                    "The offline-session grant does not belong to the authenticated avatar.");

            var providerResult = await GetAndActivateDefaultStorageProviderAsync().ConfigureAwait(false);
            if (providerResult == null || providerResult.IsError || providerResult.Result == null)
                return SyncError(HttpStatusCode.ServiceUnavailable, "HOSTED_SYNC_PROVIDER_UNAVAILABLE",
                    providerResult?.Message ?? "The configured OASIS storage provider is unavailable.");

            if (!(providerResult.Result is IHostedHyperDriveSyncStore syncStore))
                return SyncError(HttpStatusCode.ServiceUnavailable, "HOSTED_SYNC_PROVIDER_UNSUPPORTED",
                    $"The configured provider '{providerResult.Result.ProviderName}' does not implement durable hosted synchronization.");

            var result = await new HostedHyperDriveSyncProcessor(syncStore)
                .ExchangeAsync(authenticatedAvatarId, request, cancellationToken).ConfigureAwait(false);
            if (!result.IsError) return Ok(result);

            HttpStatusCode status = result.ErrorCode == "HOSTED_SYNC_AUTH_REQUIRED" ? HttpStatusCode.Unauthorized :
                result.ErrorCode == "HOSTED_SYNC_AVATAR_SCOPE_MISMATCH" ? HttpStatusCode.Forbidden :
                result.ErrorCode != null && result.ErrorCode.Contains("INVALID") || result.ErrorCode != null && result.ErrorCode.EndsWith("_REQUIRED")
                    ? HttpStatusCode.BadRequest : HttpStatusCode.ServiceUnavailable;
            return StatusCode((int)status, result);
        }

        private IActionResult SyncError(HttpStatusCode status, string code, string message) =>
            StatusCode((int)status, new OASISResult<SyncExchangeResponse>
            {
                IsError = true, ErrorCount = 1, ErrorCode = code, Message = message
            });

        private IActionResult PeerBindingError(HttpStatusCode status, string code, string message) =>
            StatusCode((int)status, new OASISResult<bool>
            {
                IsError = true, ErrorCount = 1, ErrorCode = code, Message = message
            });

        private IActionResult OfflineGrantError(HttpStatusCode status, string code, string message) =>
            StatusCode((int)status, new OASISResult<HyperDriveOfflineSessionGrant>
            {
                IsError = true, ErrorCount = 1, ErrorCode = code, Message = message
            });

        private static bool HostedSyncEnabled() =>
            OASISBootLoader.OASISBootLoader.OASISDNA?.OASIS?.OASISHyperDriveConfig?.EnableHostedSync == true;
    }
}
