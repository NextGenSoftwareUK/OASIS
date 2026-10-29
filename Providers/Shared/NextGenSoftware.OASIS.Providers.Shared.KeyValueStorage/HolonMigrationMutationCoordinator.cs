using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

public sealed record HolonMigrationMutationProgress(string PreparationFingerprint,
    HolonMigrationBarrierProgress Barriers)
{
    public bool AllParticipantsStarted => Barriers.Stores.Count > 0
        && Barriers.Stores.All(s => s.State == HolonMigrationBarrierObservationState.MutationStarted);
}

/// <summary>Starts or resumes only the exact durably prepared native mutation
/// markers. Never moves business data, cancels barriers, retries uncertain writes,
/// grants cutover permission or substitutes a new preparation during recovery.</summary>
public static class HolonMigrationMutationCoordinator
{
    public static async Task<OASISResult<HolonMigrationMutationProgress>> StartPersistedAsync(
        HolonReplicationMigrationBarrierPlan plan, string approvedFingerprint,
        FileHolonMigrationMutationPreparationStore store,
        IHolonReplicationMigrationAuthorizationPolicy administrativePolicy,
        IHolonMigrationMutationAuthorizationPolicy mutationPolicy,
        IEnumerable<HolonMigrationBarrierParticipant> participants,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(plan); ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(administrativePolicy); ArgumentNullException.ThrowIfNull(mutationPolicy);
        var progress = plan.Scopes.Select(s => new HolonMigrationBarrierStoreProgress(
            s.ExpectedStorage.BindingFingerprint, s.Fingerprint, HolonMigrationBarrierObservationState.Unobserved)).ToArray();
        var result = new OASISResult<HolonMigrationMutationProgress>();
        HolonMigrationMutationPreparation preparation = null;
        int active = -1;
        OASISResult<HolonMigrationMutationProgress> Fail(string code, Exception error = null)
        {
            result.IsError = true; result.ErrorCount = 1; result.ErrorCode = code; result.Exception = error ?? result.Exception;
            result.Message = "Preserve original preparation and native outcomes; no cancellation or compensation was attempted.";
            result.Result = new(approvedFingerprint, new(plan.Context, progress)); return result;
        }
        async Task<bool> Authorized()
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (administrativePolicy.BindingIdentity != plan.Context.AuthorizationPolicyBinding)
            { Fail("COSMIC_MIGRATION_AUTHORIZATION_DENIED"); return false; }
            var admin = await administrativePolicy.AuthorizeAsync(plan.Context, cancellationToken).ConfigureAwait(false);
            if (admin == null || admin.IsError || !admin.Result)
            { Fail(admin?.ErrorCode ?? "COSMIC_MIGRATION_AUTHORIZATION_DENIED", admin?.Exception); return false; }
            if (preparation == null) return true;
            if (mutationPolicy.BindingIdentity != preparation.MutationPolicyBinding)
            { Fail("COSMIC_MIGRATION_MUTATION_AUTHORIZATION_DENIED"); return false; }
            var permission = await mutationPolicy.AuthorizeAsync(preparation, cancellationToken).ConfigureAwait(false);
            if (permission == null || permission.IsError || !permission.Result)
            { Fail(permission?.ErrorCode ?? "COSMIC_MIGRATION_MUTATION_AUTHORIZATION_DENIED", permission?.Exception); return false; }
            return true;
        }
        bool Accept(OASISResult<HolonMigrationFenceObservation> observed, int index, Guid? expectedOwner = null)
        {
            if (observed == null || observed.IsError || observed.Result == null)
            {
                progress[index] = progress[index] with { State = HolonMigrationBarrierObservationState.Unknown };
                Fail(observed?.ErrorCode ?? "COSMIC_MIGRATION_MUTATION_OUTCOME_UNKNOWN", observed?.Exception); return false;
            }
            var native = observed.Result;
            if (native.Request.Fingerprint != plan.Scopes[index].Fingerprint
                || expectedOwner.HasValue && native.OwnerToken != expectedOwner.Value
                || native.State is not (HolonMigrationFenceState.Held or HolonMigrationFenceState.MutationStarted)
                || expectedOwner.HasValue && native.State != HolonMigrationFenceState.MutationStarted
                || native.State == HolonMigrationFenceState.MutationStarted && native.MutationDigest != preparation.Fingerprint)
            {
                progress[index] = progress[index] with { State = HolonMigrationBarrierObservationState.Unknown };
                Fail("COSMIC_MIGRATION_MUTATION_SCOPE_OR_INTENT_MISMATCH"); return false;
            }
            progress[index] = progress[index] with { State = native.State == HolonMigrationFenceState.Held
                ? HolonMigrationBarrierObservationState.Held : HolonMigrationBarrierObservationState.MutationStarted };
            if (observed.IsWarning) { result.IsWarning = true; result.WarningCount += Math.Max(1, observed.WarningCount); result.Exception ??= observed.Exception; }
            if (expectedOwner.HasValue && observed.IsSaved) { result.IsSaved = true; result.SavedCount++; }
            return true;
        }
        try
        {
            if (!await Authorized().ConfigureAwait(false)) return result;
            var persisted = await store.ReadAsync(plan, approvedFingerprint, cancellationToken).ConfigureAwait(false);
            if (persisted.IsError || persisted.Result == null)
                return Fail(persisted.ErrorCode ?? "COSMIC_MIGRATION_MUTATION_PREPARATION_UNVERIFIED", persisted.Exception);
            preparation = persisted.Result;
            if (!await Authorized().ConfigureAwait(false)) return result;
            var owners = HolonMigrationBarrierParticipants.Bind(plan, participants);
            // Capability admission is complete before any native read or start.
            if (owners.Any(p => p.Provider is not IHolonMigrationMutationStorageProvider))
                return Fail("COSMIC_MIGRATION_MUTATION_CAPABILITY_REQUIRED");
            var observations = new HolonMigrationFenceObservation[owners.Length];
            for (var i = 0; i < owners.Length; i++)
            {
                active = i;
                var observed = await owners[i].Provider.ReadHolonMigrationFenceAsync(plan.Scopes[i], cancellationToken).ConfigureAwait(false);
                active = -1;
                if (!Accept(observed, i)) return result;
                observations[i] = observed.Result;
            }
            for (var i = 0; i < owners.Length; i++)
            {
                if (observations[i].State == HolonMigrationFenceState.MutationStarted) continue;
                if (!await Authorized().ConfigureAwait(false)) return result;
                active = i;
                var started = await ((IHolonMigrationMutationStorageProvider)owners[i].Provider).BeginHolonMigrationMutationAsync(
                    new(plan.Scopes[i], observations[i].OwnerToken, preparation.Fingerprint), cancellationToken).ConfigureAwait(false);
                active = -1;
                if (!Accept(started, i, observations[i].OwnerToken)) return result;
            }
            if (!await Authorized().ConfigureAwait(false)) return result;
            result.Result = new(preparation.Fingerprint, new(plan.Context, progress)); result.IsLoaded = true;
            result.Message = "All exact native mutation-start markers acknowledged. Business-data CAS and cutover remain separate.";
            return result;
        }
        catch (Exception error)
        {
            if (active >= 0) progress[active] = progress[active] with { State = HolonMigrationBarrierObservationState.Unknown };
            return Fail(error is HolonMigrationBarrierPlanException scope ? scope.Code
                : error is OperationCanceledException ? "COSMIC_MIGRATION_MUTATION_CANCELLED" : "COSMIC_MIGRATION_MUTATION_OUTCOME_UNKNOWN", error);
        }
    }
}
