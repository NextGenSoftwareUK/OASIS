using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

/// <summary>Host-owned approval of exact mutation evidence. Administrative
/// barrier admission alone never authorizes starting a data mutation.</summary>
public interface IHolonMigrationMutationAuthorizationPolicy
{
    string BindingIdentity { get; }
    Task<OASISResult<bool>> AuthorizeAsync(HolonMigrationMutationPreparation preparation,
        CancellationToken cancellationToken = default);
}

public sealed class HolonMigrationMutationGrant
{
    public HolonMigrationMutationPreparation Preparation { get; }
    public DateTimeOffset NotBefore { get; }
    public DateTimeOffset ExpiresAt { get; }

    public HolonMigrationMutationGrant(HolonMigrationMutationPreparation preparation,
        DateTimeOffset notBefore, DateTimeOffset expiresAt)
    {
        Preparation = preparation ?? throw new ArgumentNullException(nameof(preparation));
        if (expiresAt <= notBefore)
            throw new ArgumentException("An explicit nonempty mutation approval interval is required.");
        NotBefore = notBefore;
        ExpiresAt = expiresAt;
    }
}

/// <summary>Immutable bounded host allowlist. Replace the registered policy to
/// revoke grants. Every execution/recovery must recheck the current policy;
/// persisted preparation or native started markers never extend an approval.</summary>
public sealed class ScopedHolonMigrationMutationPolicy : IHolonMigrationMutationAuthorizationPolicy
{
    private readonly IReadOnlyDictionary<string, HolonMigrationMutationGrant> grants;
    private readonly TimeProvider clock;
    public string BindingIdentity { get; }

    public ScopedHolonMigrationMutationPolicy(string bindingIdentity,
        IEnumerable<HolonMigrationMutationGrant> hostGrants, TimeProvider clock = null)
    {
        HolonReplicationMigrationAuthorizationContext.ValidateToken(bindingIdentity);
        var rows = hostGrants?.Take(257).ToArray() ?? throw new ArgumentNullException(nameof(hostGrants));
        if (rows.Length > 256 || rows.Any(g => g == null || g.Preparation.MutationPolicyBinding != bindingIdentity))
            throw new ArgumentException("Admit at most 256 exact mutation grants belonging to this policy.");
        BindingIdentity = bindingIdentity;
        grants = rows.ToDictionary(g => g.Preparation.Fingerprint, StringComparer.Ordinal);
        this.clock = clock ?? TimeProvider.System;
    }

    public Task<OASISResult<bool>> AuthorizeAsync(HolonMigrationMutationPreparation preparation,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var now = clock.GetUtcNow();
        var allowed = preparation != null && preparation.MutationPolicyBinding == BindingIdentity
            && grants.TryGetValue(preparation.Fingerprint, out var grant)
            && now >= grant.NotBefore && now < grant.ExpiresAt;
        return Task.FromResult(allowed ? new OASISResult<bool>(true)
            : new OASISResult<bool>(false) { IsError = true, ErrorCount = 1,
                ErrorCode = "COSMIC_MIGRATION_MUTATION_AUTHORIZATION_DENIED",
                Message = "No current host grant approves this exact prepared mutation." });
    }
}
