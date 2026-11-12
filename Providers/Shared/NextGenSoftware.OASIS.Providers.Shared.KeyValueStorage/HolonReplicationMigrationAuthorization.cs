using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Configuration;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

/// <summary>An exact migration reviewed by an operator. Construct the actor from
/// the authenticated host context, never from an HTTP body's claimed identity.
/// The preview digest binds reviewed evidence; this object does not verify that evidence.</summary>
public sealed class HolonReplicationMigrationAuthorizationContext
{
    public Guid MigrationId { get; }
    public Guid OperatorId { get; }
    public CosmicStorageIdentity SourceStorage { get; }
    public CosmicStorageIdentity OriginalTargetStorage { get; }
    public string OriginalSourceId { get; }
    public string OriginalTargetId { get; }
    public HolonReplicationRouteBinding ReplacementRoute { get; }
    public string PreviewDigest { get; }
    public string AuthorizationPolicyBinding { get; }
    public string ContextDigest { get; }

    public HolonReplicationMigrationAuthorizationContext(Guid migrationId, Guid authenticatedOperator,
        CosmicStorageIdentity source, CosmicStorageIdentity originalTarget, string originalSourceId,
        string originalTargetId, HolonReplicationRouteBinding replacementRoute, string previewDigest,
        string authorizationPolicyBinding)
    {
        if (migrationId == Guid.Empty || authenticatedOperator == Guid.Empty)
            throw new ArgumentException("An exact migration and authenticated operator are required.");
        SourceStorage = source ?? throw new ArgumentNullException(nameof(source));
        OriginalTargetStorage = originalTarget ?? throw new ArgumentNullException(nameof(originalTarget));
        ReplacementRoute = replacementRoute ?? throw new ArgumentNullException(nameof(replacementRoute));
        ValidateToken(originalSourceId); ValidateToken(originalTargetId); ValidateToken(authorizationPolicyBinding);
        if (previewDigest == null || previewDigest.Length != 64
            || previewDigest.Any(c => !(c >= '0' && c <= '9' || c >= 'A' && c <= 'F')))
            throw new ArgumentException("The exact canonical SHA256 preview digest is required.", nameof(previewDigest));
        if (source.BindingFingerprint != replacementRoute.SourceStorage.BindingFingerprint)
            throw new ArgumentException("Lineage rebinding must retain the explicitly reviewed physical source.");
        MigrationId = migrationId; OperatorId = authenticatedOperator;
        OriginalSourceId = originalSourceId; OriginalTargetId = originalTargetId;
        PreviewDigest = previewDigest; AuthorizationPolicyBinding = authorizationPolicyBinding;
        var bytes = JsonSerializer.SerializeToUtf8Bytes(new {
            format = "cosmic-migration-authorization-v1", MigrationId, OperatorId,
            source = SourceStorage.BindingFingerprint, originalTarget = OriginalTargetStorage.BindingFingerprint,
            OriginalSourceId, OriginalTargetId, replacementSource = ReplacementRoute.SourceId,
            replacementTarget = ReplacementRoute.TargetId, replacementTargetStore = ReplacementRoute.TargetStorage.BindingFingerprint,
            replacementPolicy = ReplacementRoute.PolicyBinding, PreviewDigest, AuthorizationPolicyBinding });
        ContextDigest = Convert.ToHexString(SHA256.HashData(bytes));
    }

    internal static void ValidateToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token) || token.Length > 256 || token != token.Trim()
            || token.Any(char.IsControl)) throw new ArgumentException("An exact bounded identity is required.");
        // Reject malformed Unicode rather than accepting two tokens with the same encoded bytes.
        _ = new UTF8Encoding(false, true).GetBytes(token);
    }
}

/// <summary>Administrative migration admission, separate from ordinary record
/// replication. Hosts must explicitly register their operator/tenant policy.</summary>
public interface IHolonReplicationMigrationAuthorizationPolicy
{
    string BindingIdentity { get; }
    Task<OASISResult<bool>> AuthorizeAsync(HolonReplicationMigrationAuthorizationContext context,
        CancellationToken cancellationToken = default);
}

/// <summary>Host-created, time-limited admission for one exact reviewed context.
/// It is not a client token, native fence, replica receipt or migration commit.</summary>
public sealed class HolonReplicationMigrationGrant
{
    public HolonReplicationMigrationAuthorizationContext Context { get; }
    public DateTimeOffset NotBefore { get; }
    public DateTimeOffset ExpiresAt { get; }
    public HolonReplicationMigrationGrant(HolonReplicationMigrationAuthorizationContext context,
        DateTimeOffset notBefore, DateTimeOffset expiresAt)
    {
        Context = context ?? throw new ArgumentNullException(nameof(context));
        if (expiresAt <= notBefore) throw new ArgumentException("An explicit nonempty authorization interval is required.");
        NotBefore = notBefore; ExpiresAt = expiresAt;
    }
}

/// <summary>Immutable host-owned allowlist. Empty configuration denies all;
/// matching a replication policy or owning a source record grants no admission.
/// Rebuild/replace the registered policy to revoke grants; execution must ask again.</summary>
public sealed class ScopedHolonReplicationMigrationPolicy : IHolonReplicationMigrationAuthorizationPolicy
{
    private readonly IReadOnlyDictionary<string, HolonReplicationMigrationGrant> grants;
    private readonly TimeProvider clock;
    public string BindingIdentity { get; }
    public ScopedHolonReplicationMigrationPolicy(string bindingIdentity,
        IEnumerable<HolonReplicationMigrationGrant> hostGrants, TimeProvider clock = null)
    {
        if (string.IsNullOrWhiteSpace(bindingIdentity) || bindingIdentity.Length > 256
            || bindingIdentity != bindingIdentity.Trim() || bindingIdentity.Any(char.IsControl))
            throw new ArgumentException("An explicit administrative policy binding is required.");
        var rows = hostGrants?.Take(257).ToArray() ?? throw new ArgumentNullException(nameof(hostGrants));
        if (rows.Length > 256 || rows.Any(g => g == null || g.Context.AuthorizationPolicyBinding != bindingIdentity))
            throw new ArgumentException("Admit at most 256 exact grants belonging to this policy.");
        BindingIdentity = bindingIdentity;
        grants = rows.ToDictionary(g => g.Context.ContextDigest, StringComparer.Ordinal);
        this.clock = clock ?? TimeProvider.System;
    }

    public Task<OASISResult<bool>> AuthorizeAsync(HolonReplicationMigrationAuthorizationContext context,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var now = clock.GetUtcNow();
        var allowed = context != null && context.AuthorizationPolicyBinding == BindingIdentity
            && grants.TryGetValue(context.ContextDigest, out var grant)
            && now >= grant.NotBefore && now < grant.ExpiresAt;
        return Task.FromResult(allowed ? new OASISResult<bool>(true)
            : new OASISResult<bool>(false) { IsError = true, ErrorCount = 1,
                ErrorCode = "COSMIC_MIGRATION_AUTHORIZATION_DENIED",
                Message = "No current host grant admits this operator and exact reviewed migration context." });
    }
}
