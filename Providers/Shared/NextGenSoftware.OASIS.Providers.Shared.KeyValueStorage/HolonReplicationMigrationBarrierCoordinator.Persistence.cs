using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

public static partial class HolonReplicationMigrationBarrierCoordinator
{
    /// <summary>Persist/verify exact recovery evidence before any native barrier.
    /// The host owns a persistent folder and independently registered policy.</summary>
    public static Task<OASISResult<HolonMigrationBarrierProgress>> AcquirePersistedAsync(
        HolonReplicationMigrationBarrierPlan plan, FileHolonMigrationBarrierPlanStore store,
        IHolonReplicationMigrationAuthorizationPolicy policy, IEnumerable<HolonMigrationBarrierParticipant> participants,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(plan); ArgumentNullException.ThrowIfNull(store);
        return WithPersistedPlan(plan.Context, policy, participants,
            () => store.SaveAsync(plan, cancellationToken), cancellationToken);
    }
    /// <summary>Resume only the original persisted evidence. No fresh preview,
    /// provider fallback or silent reconstruction after evidence loss is allowed.</summary>
    public static Task<OASISResult<HolonMigrationBarrierProgress>> ResumePersistedAsync(
        HolonReplicationMigrationAuthorizationContext context, FileHolonMigrationBarrierPlanStore store,
        IHolonReplicationMigrationAuthorizationPolicy policy, IEnumerable<HolonMigrationBarrierParticipant> participants,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(store);
        return WithPersistedPlan(context, policy, participants, () => store.ReadAsync(context, cancellationToken), cancellationToken);
    }
    private static async Task<OASISResult<HolonMigrationBarrierProgress>> WithPersistedPlan(
        HolonReplicationMigrationAuthorizationContext context, IHolonReplicationMigrationAuthorizationPolicy policy,
        IEnumerable<HolonMigrationBarrierParticipant> participants,
        Func<Task<OASISResult<HolonReplicationMigrationBarrierPlan>>> persistOrRead, CancellationToken token)
    {
        ArgumentNullException.ThrowIfNull(context); ArgumentNullException.ThrowIfNull(policy);
        OASISResult<HolonMigrationBarrierProgress> Error(string code, Exception cause)
            => new() { IsError = true, ErrorCount = 1, ErrorCode = code, Exception = cause,
                Message = "The original approved recovery evidence was not admitted; native barriers were not accessed.",
                Result = new(context, Array.Empty<HolonMigrationBarrierStoreProgress>()) };
        try
        {
            token.ThrowIfCancellationRequested();
            if (policy.BindingIdentity != context.AuthorizationPolicyBinding)
                return Error("COSMIC_MIGRATION_BARRIER_SCOPE_MISMATCH", null);
            var permission = await policy.AuthorizeAsync(context, token).ConfigureAwait(false);
            if (permission == null || permission.IsError || !permission.Result)
                return Error("COSMIC_MIGRATION_AUTHORIZATION_DENIED", permission?.Exception);
            var original = await persistOrRead().ConfigureAwait(false);
            if (original == null || original.IsError || original.Result == null)
                return Error(original?.ErrorCode ?? "COSMIC_MIGRATION_BARRIER_PLAN_UNVERIFIED", original?.Exception);
            return await AcquireAsync(original.Result, policy, participants, token).ConfigureAwait(false);
        }
        catch (Exception error) { return Error("COSMIC_MIGRATION_BARRIER_PLAN_UNVERIFIED", error); }
    }
}
