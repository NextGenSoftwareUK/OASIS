using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive.Synchronization;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.Edge.Runtime
{
    /// <summary>
    /// Server-authorized, device-bound offline session assertion. SignedGrant is opaque to storage;
    /// an IEdgeOfflineGrantValidator must cryptographically validate it before every cache or resume.
    /// </summary>
    /// <summary>
    /// Platform adapter backed by Keychain, Android Keystore, Windows Credential Locker, or an
    /// equivalent protected store. Plain files, PlayerPrefs and application configuration are invalid implementations.
    /// </summary>
    public interface IEdgeSecureSessionStore
    {
        Task<OASISResult<bool>> SaveAsync(HyperDriveOfflineSessionGrant grant, CancellationToken cancellationToken);
        Task<OASISResult<HyperDriveOfflineSessionGrant>> LoadAsync(CancellationToken cancellationToken);
        Task<OASISResult<bool>> DeleteAsync(CancellationToken cancellationToken);
    }

    /// <summary>Validates the hosted ONODE signature and immutable claims in an offline grant.</summary>
    public interface IEdgeOfflineGrantValidator
    {
        Task<OASISResult<bool>> ValidateAsync(HyperDriveOfflineSessionGrant grant, CancellationToken cancellationToken);
    }

    public sealed class EcdsaEdgeOfflineGrantValidator : IEdgeOfflineGrantValidator
    {
        private readonly byte[] _hostPublicKey;

        public EcdsaEdgeOfflineGrantValidator(string base64SubjectPublicKeyInfo)
        {
            if (string.IsNullOrWhiteSpace(base64SubjectPublicKeyInfo))
                throw new ArgumentException("A pinned hosted ONODE public key is required.", nameof(base64SubjectPublicKeyInfo));
            try { _hostPublicKey = Convert.FromBase64String(base64SubjectPublicKeyInfo); }
            catch (FormatException ex) { throw new ArgumentException("The hosted ONODE public key must be base64 encoded.", nameof(base64SubjectPublicKeyInfo), ex); }
        }

        public Task<OASISResult<bool>> ValidateAsync(HyperDriveOfflineSessionGrant grant,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            bool valid = HyperDriveOfflineSessionGrantProof.Verify(grant, _hostPublicKey);
            return Task.FromResult(valid
                ? new OASISResult<bool>(true)
                : new OASISResult<bool> { IsError = true, ErrorCount = 1,
                    ErrorCode = "EDGE_OFFLINE_GRANT_SIGNATURE_INVALID",
                    Message = "The cached offline-session grant was not signed by the pinned hosted ONODE key." });
        }
    }

    public sealed class EdgeOfflineSessionManager
    {
        private readonly Guid _avatarId;
        private readonly Guid _deviceId;
        private readonly IEdgeSecureSessionStore _store;
        private readonly IEdgeOfflineGrantValidator _validator;
        private readonly IHyperDriveClock _clock;

        public EdgeOfflineSessionManager(Guid avatarId, Guid deviceId, IEdgeSecureSessionStore store,
            IEdgeOfflineGrantValidator validator, IHyperDriveClock clock)
        {
            if (avatarId == Guid.Empty) throw new ArgumentException("An avatar id is required.", nameof(avatarId));
            if (deviceId == Guid.Empty) throw new ArgumentException("A device id is required.", nameof(deviceId));
            _avatarId = avatarId;
            _deviceId = deviceId;
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        }

        public async Task<OASISResult<bool>> CacheAuthenticatedSessionAsync(HyperDriveOfflineSessionGrant grant,
            CancellationToken cancellationToken = default)
        {
            var claims = ValidateClaims(grant, Array.Empty<string>());
            if (claims != null) return Error<bool>("EDGE_OFFLINE_GRANT_INVALID", claims);
            var signature = await _validator.ValidateAsync(grant, cancellationToken).ConfigureAwait(false);
            if (signature == null || signature.IsError || !signature.Result)
                return Error<bool>(signature?.ErrorCode ?? "EDGE_OFFLINE_GRANT_SIGNATURE_INVALID",
                    signature?.Message ?? "The hosted offline-session grant signature is invalid.");
            var saved = await _store.SaveAsync(grant, cancellationToken).ConfigureAwait(false);
            if (saved == null || saved.IsError || !saved.Result)
                return Error<bool>(saved?.ErrorCode ?? "EDGE_SECURE_SESSION_SAVE_FAILED",
                    saved?.Message ?? "The validated offline session was not stored securely.");
            return saved;
        }

        public async Task<OASISResult<HyperDriveOfflineSessionGrant>> ResumeAsync(IEnumerable<string> requiredScopes,
            CancellationToken cancellationToken = default)
        {
            var scopes = (requiredScopes ?? Array.Empty<string>()).Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.Ordinal).ToArray();
            var loaded = await _store.LoadAsync(cancellationToken).ConfigureAwait(false);
            if (loaded == null || loaded.IsError)
                return Error<HyperDriveOfflineSessionGrant>(loaded?.ErrorCode ?? "EDGE_SECURE_SESSION_LOAD_FAILED",
                    loaded?.Message ?? "The secure offline session could not be loaded.");
            if (loaded.Result == null)
                return Error<HyperDriveOfflineSessionGrant>("EDGE_OFFLINE_SESSION_NOT_FOUND", "No secure offline session is available.");

            var claims = ValidateClaims(loaded.Result, scopes);
            if (claims != null)
                return Error<HyperDriveOfflineSessionGrant>("EDGE_OFFLINE_SESSION_REJECTED", claims);
            var signature = await _validator.ValidateAsync(loaded.Result, cancellationToken).ConfigureAwait(false);
            if (signature == null || signature.IsError || !signature.Result)
                return Error<HyperDriveOfflineSessionGrant>(signature?.ErrorCode ?? "EDGE_OFFLINE_GRANT_SIGNATURE_INVALID",
                    signature?.Message ?? "The cached offline-session grant signature is invalid.");

            return new OASISResult<HyperDriveOfflineSessionGrant>(loaded.Result)
            { IsLoaded = true, Message = "The server-authorized offline session was resumed." };
        }

        public Task<OASISResult<bool>> ClearAsync(CancellationToken cancellationToken = default) =>
            _store.DeleteAsync(cancellationToken);

        private string ValidateClaims(HyperDriveOfflineSessionGrant grant, IReadOnlyCollection<string> requiredScopes)
        {
            if (grant == null || string.IsNullOrWhiteSpace(grant.GrantId) || string.IsNullOrWhiteSpace(grant.Signature))
                return "A complete signed offline-session grant is required.";
            if (grant.AvatarId != _avatarId || grant.DeviceId != _deviceId)
                return "The offline-session grant is not bound to this avatar and device.";
            if (grant.IssuedUtc.Kind != DateTimeKind.Utc || grant.ExpiresUtc.Kind != DateTimeKind.Utc ||
                grant.ExpiresUtc <= grant.IssuedUtc || grant.ExpiresUtc <= _clock.UtcNow)
                return "The offline-session grant has expired or contains invalid UTC validity times.";
            var granted = new HashSet<string>(grant.Scopes ?? Array.Empty<string>(), StringComparer.Ordinal);
            var missing = requiredScopes.Where(x => !granted.Contains(x)).ToArray();
            return missing.Length == 0 ? null : $"The offline-session grant does not authorize: {string.Join(", ", missing)}.";
        }

        private static OASISResult<T> Error<T>(string code, string message) => new OASISResult<T>
        { IsError = true, ErrorCount = 1, ErrorCode = code, Message = message };
    }
}
