using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

internal sealed class HolonMigrationEvidenceReadException(string code, Exception cause) : Exception("Native migration evidence read failed.", cause)
{ internal string Code { get; } = code; }

/// <summary>One owner of administrative admission and before/after native fence
/// checks for read-only migration evidence. No token is exposed in reports.</summary>
internal sealed class HolonMigrationEvidenceReadScope
{
    private readonly HolonReplicationMigrationBarrierPlan plan;
    private readonly IHolonReplicationMigrationAuthorizationPolicy policy;
    private readonly CancellationToken token;
    private readonly Dictionary<string, (Guid Owner, HolonMigrationFenceState Phase, string Digest)> owners = new(StringComparer.Ordinal);
    private readonly List<Exception> warnings = new();
    private int warningCount;
    internal HolonMigrationBarrierParticipant[] Participants { get; }
    private HolonMigrationEvidenceReadScope(HolonReplicationMigrationBarrierPlan plan,
        IHolonReplicationMigrationAuthorizationPolicy policy, IEnumerable<HolonMigrationBarrierParticipant> participants, CancellationToken token)
    { this.plan = plan; this.policy = policy; this.token = token; Participants = HolonMigrationBarrierParticipants.Bind(plan, participants); }
    internal static async Task<HolonMigrationEvidenceReadScope> OpenAsync(HolonReplicationMigrationBarrierPlan plan,
        IHolonReplicationMigrationAuthorizationPolicy policy, IEnumerable<HolonMigrationBarrierParticipant> participants, CancellationToken token)
    {
        var scope = new HolonMigrationEvidenceReadScope(plan, policy, participants, token);
        await scope.Authorize().ConfigureAwait(false); await scope.CheckFences(true).ConfigureAwait(false); return scope;
    }
    private async Task Authorize()
    {
        token.ThrowIfCancellationRequested();
        if (policy.BindingIdentity != plan.Context.AuthorizationPolicyBinding)
            throw new HolonMigrationEvidenceReadException("COSMIC_MIGRATION_AUTHORIZATION_DENIED", null);
        var allowed = await policy.AuthorizeAsync(plan.Context, token).ConfigureAwait(false);
        if (allowed == null || allowed.IsError || !allowed.Result)
            throw new HolonMigrationEvidenceReadException(string.IsNullOrWhiteSpace(allowed?.ErrorCode)
                ? "COSMIC_MIGRATION_AUTHORIZATION_DENIED" : allowed.ErrorCode, allowed?.Exception);
        Observe(allowed);
    }
    private async Task CheckFences(bool first)
    {
        foreach (var request in plan.Scopes)
        {
            var native = Participants.Single(p => p.Storage.BindingFingerprint == request.ExpectedStorage.BindingFingerprint).Provider;
            var read = await native.ReadHolonMigrationFenceAsync(request, token).ConfigureAwait(false);
            if (read == null || read.IsError)
                throw new HolonMigrationEvidenceReadException(string.IsNullOrWhiteSpace(read?.ErrorCode)
                    ? "COSMIC_MIGRATION_EVIDENCE_BARRIER_UNVERIFIED" : read.ErrorCode, read?.Exception);
            Observe(read);
            if (read.Result == null || read.Result.Request.Fingerprint != request.Fingerprint || read.Result.State is not (HolonMigrationFenceState.Held or HolonMigrationFenceState.MutationStarted)
                || !first && owners[request.Fingerprint] != (read.Result.OwnerToken, read.Result.State, read.Result.MutationDigest))
                throw new InvalidDataException("Every original native record barrier must remain held by the same owner.");
            if (first) owners.Add(request.Fingerprint, (read.Result.OwnerToken, read.Result.State, read.Result.MutationDigest));
        }
    }
    internal async Task CompleteAsync()
    { await CheckFences(false).ConfigureAwait(false); await Authorize().ConfigureAwait(false); }
    internal void Observe<T>(OASISResult<T> observed)
    {
        if (!observed.IsWarning) return;
        warningCount += Math.Max(1, observed.WarningCount); if (observed.Exception != null) warnings.Add(observed.Exception);
    }
    internal OASISResult<T> ApplyWarnings<T>(OASISResult<T> result)
    {
        result.IsWarning = warningCount > 0; result.WarningCount = warningCount;
        result.Exception = warnings.Count == 0 ? null : warnings.Count == 1 ? warnings[0] : new AggregateException(warnings); return result;
    }
}
