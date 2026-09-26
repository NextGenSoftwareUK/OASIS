using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive.Synchronization;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services
{
    public interface IHyperDriveOfflineSessionGrantIssuer
    {
        Task<OASISResult<HyperDriveOfflineSessionGrant>> IssueAsync(Guid authenticatedAvatarId,
            IssueHyperDriveOfflineSessionGrantRequest request, CancellationToken cancellationToken);
        Task<OASISResult<Guid>> ValidateAsync(HyperDriveOfflineSessionGrant grant, string requiredScope,
            Guid requestDeviceId, CancellationToken cancellationToken);
    }

    /// <summary>Issues narrowly scoped offline authorization. Construction validates all signing configuration so an enabled host fails at startup.</summary>
    public sealed class HyperDriveOfflineSessionGrantIssuer : IHyperDriveOfflineSessionGrantIssuer
    {
        private readonly byte[] _privateKey;
        private readonly byte[] _publicKey;
        private readonly HashSet<string> _allowedScopes;
        private readonly int _maximumLifetimeMinutes;

        public HyperDriveOfflineSessionGrantIssuer(OfflineSessionGrantSettings settings, Func<string, string> environmentReader = null)
        {
            if (settings == null || !settings.Enabled)
                throw new InvalidOperationException("Offline-session grant issuance is not enabled.");
            if (settings.MaximumLifetimeMinutes <= 0)
                throw new InvalidOperationException("OfflineSessionGrants.MaximumLifetimeMinutes must be greater than zero.");
            if (string.IsNullOrWhiteSpace(settings.SigningPrivateKeyEnvironmentVariable))
                throw new InvalidOperationException("OfflineSessionGrants.SigningPrivateKeyEnvironmentVariable is required.");
            string encodedPublicKey = string.IsNullOrWhiteSpace(settings.SigningPublicKeyEnvironmentVariable)
                ? settings.SigningPublicKey
                : (environmentReader ?? Environment.GetEnvironmentVariable)(settings.SigningPublicKeyEnvironmentVariable);
            if (string.IsNullOrWhiteSpace(encodedPublicKey))
                throw new InvalidOperationException("OfflineSessionGrants.SigningPublicKey is required so Edge releases can pin the grant issuer identity.");

            _allowedScopes = new HashSet<string>((settings.AllowedScopes ?? new List<string>())
                .Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()), StringComparer.Ordinal);
            if (_allowedScopes.Count == 0)
                throw new InvalidOperationException("OfflineSessionGrants.AllowedScopes must contain at least one explicit scope.");

            string encodedKey = (environmentReader ?? Environment.GetEnvironmentVariable)(settings.SigningPrivateKeyEnvironmentVariable);
            if (string.IsNullOrWhiteSpace(encodedKey))
                throw new InvalidOperationException($"Environment variable '{settings.SigningPrivateKeyEnvironmentVariable}' must contain the offline-grant signing key.");
            try
            {
                _privateKey = Convert.FromBase64String(encodedKey);
                using var key = ECDsa.Create();
                key.ImportPkcs8PrivateKey(_privateKey, out int bytesRead);
                if (bytesRead != _privateKey.Length || key.KeySize != 256)
                    throw new InvalidOperationException("The offline-grant signing key must be one PKCS#8 ECDSA P-256 key.");
                byte[] configuredPublicKey = Convert.FromBase64String(encodedPublicKey);
                byte[] derivedPublicKey = key.ExportSubjectPublicKeyInfo();
                if (!CryptographicOperations.FixedTimeEquals(configuredPublicKey, derivedPublicKey))
                    throw new InvalidOperationException("OfflineSessionGrants.SigningPublicKey does not match the configured private signing key.");
                _publicKey = configuredPublicKey;
            }
            catch (FormatException ex) { throw new InvalidOperationException("The offline-grant signing private or public key is not valid base64.", ex); }
            catch (CryptographicException ex) { throw new InvalidOperationException("The offline-grant signing key is not a valid PKCS#8 ECDSA P-256 key.", ex); }

            _maximumLifetimeMinutes = settings.MaximumLifetimeMinutes;
        }

        public Task<OASISResult<HyperDriveOfflineSessionGrant>> IssueAsync(Guid authenticatedAvatarId,
            IssueHyperDriveOfflineSessionGrantRequest request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (authenticatedAvatarId == Guid.Empty)
                return Task.FromResult(Error("OFFLINE_GRANT_AUTH_REQUIRED", "An authenticated avatar is required."));
            if (request == null || request.DeviceId == Guid.Empty)
                return Task.FromResult(Error("OFFLINE_GRANT_DEVICE_REQUIRED", "A non-empty DeviceId is required."));
            if (request.RequestedLifetimeMinutes <= 0)
                return Task.FromResult(Error("OFFLINE_GRANT_LIFETIME_INVALID", "RequestedLifetimeMinutes must be greater than zero."));

            string[] scopes = (request.RequestedScopes ?? Array.Empty<string>())
                .Select(x => x?.Trim()).Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.Ordinal).OrderBy(x => x, StringComparer.Ordinal).ToArray();
            if (scopes.Length == 0)
                return Task.FromResult(Error("OFFLINE_GRANT_SCOPES_REQUIRED", "At least one offline scope is required."));
            string[] forbidden = scopes.Where(x => !_allowedScopes.Contains(x)).ToArray();
            if (forbidden.Length != 0)
                return Task.FromResult(Error("OFFLINE_GRANT_SCOPE_FORBIDDEN", $"The following scopes are not authorized for offline use: {string.Join(", ", forbidden)}."));

            DateTime issuedUtc = DateTime.UtcNow;
            var grant = new HyperDriveOfflineSessionGrant
            {
                GrantId = Guid.NewGuid().ToString("N"),
                AvatarId = authenticatedAvatarId,
                DeviceId = request.DeviceId,
                IssuedUtc = issuedUtc,
                ExpiresUtc = issuedUtc.AddMinutes(Math.Min(request.RequestedLifetimeMinutes, _maximumLifetimeMinutes)),
                Scopes = scopes
            };
            grant.Signature = HyperDriveOfflineSessionGrantProof.Sign(grant, _privateKey);
            return Task.FromResult(new OASISResult<HyperDriveOfflineSessionGrant>(grant));
        }

        public Task<OASISResult<Guid>> ValidateAsync(HyperDriveOfflineSessionGrant grant, string requiredScope,
            Guid requestDeviceId, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (grant == null || grant.AvatarId == Guid.Empty || grant.DeviceId == Guid.Empty ||
                string.IsNullOrWhiteSpace(grant.GrantId) || string.IsNullOrWhiteSpace(grant.Signature))
                return Task.FromResult(ValidationError("OFFLINE_GRANT_INVALID", "A complete signed offline-session grant is required."));
            if (requestDeviceId == Guid.Empty || grant.DeviceId != requestDeviceId)
                return Task.FromResult(ValidationError("OFFLINE_GRANT_DEVICE_MISMATCH", "The offline-session grant is not bound to this device."));
            if (grant.IssuedUtc.Kind != DateTimeKind.Utc || grant.ExpiresUtc.Kind != DateTimeKind.Utc ||
                grant.ExpiresUtc <= grant.IssuedUtc || grant.ExpiresUtc <= DateTime.UtcNow)
                return Task.FromResult(ValidationError("OFFLINE_GRANT_EXPIRED", "The offline-session grant has expired or has invalid UTC validity times."));
            if (string.IsNullOrWhiteSpace(requiredScope) ||
                !(grant.Scopes ?? Array.Empty<string>()).Contains(requiredScope, StringComparer.Ordinal))
                return Task.FromResult(ValidationError("OFFLINE_GRANT_SCOPE_FORBIDDEN", $"The offline-session grant does not authorize '{requiredScope}'."));
            if (!HyperDriveOfflineSessionGrantProof.Verify(grant, _publicKey))
                return Task.FromResult(ValidationError("OFFLINE_GRANT_SIGNATURE_INVALID", "The offline-session grant signature is invalid."));
            return Task.FromResult(new OASISResult<Guid>(grant.AvatarId));
        }

        private static OASISResult<HyperDriveOfflineSessionGrant> Error(string code, string message) => new()
        {
            IsError = true, ErrorCount = 1, ErrorCode = code, Message = message
        };

        private static OASISResult<Guid> ValidationError(string code, string message) => new()
        {
            IsError = true, ErrorCount = 1, ErrorCode = code, Message = message
        };
    }
}
