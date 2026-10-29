using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

public enum HolonMigrationBarrierObservationState { Unobserved, Absent, Held, Cancelled, Unknown }

/// <summary>Host-selected provider for one exact physical store. Native operations
/// still verify that binding; this declaration cannot override native identity.</summary>
public sealed record HolonMigrationBarrierParticipant(CosmicStorageIdentity Storage, IHolonMigrationFenceStorageProvider Provider);

/// <summary>Redacted progress only. Native owner tokens remain inside the trusted
/// coordinator. A held barrier does not prove quiescence or authorize cutover.</summary>
public sealed record HolonMigrationBarrierStoreProgress(string StorageFingerprint, string ScopeFingerprint,
    HolonMigrationBarrierObservationState State);

public sealed class HolonMigrationBarrierProgress
{
    public Guid MigrationId { get; }
    public string AuthorizationContextDigest { get; }
    public IReadOnlyList<HolonMigrationBarrierStoreProgress> Stores { get; }
    public bool AllBarriersHeld => Stores.Count > 0 && Stores.All(s => s.State == HolonMigrationBarrierObservationState.Held);
    internal HolonMigrationBarrierProgress(HolonReplicationMigrationAuthorizationContext context,
        IEnumerable<HolonMigrationBarrierStoreProgress> stores)
    {
        MigrationId = context.MigrationId; AuthorizationContextDigest = context.ContextDigest;
        Stores = Array.AsReadOnly(stores.ToArray());
    }
}

/// <summary>Acquire-only, pre-mutation coordinator. The host must retain the exact
/// reviewed preview/context for recovery. Native immutable receipts are the durable
/// per-store journal: a new invocation reads all stores before acquiring any absent
/// scope. No automatic retry, compensation, expiry, data mutation or HTTP execution.
/// Cancellation/failure preserves held and unknown outcomes for explicit recovery.</summary>
public static class HolonReplicationMigrationBarrierCoordinator
{
    public static async Task<OASISResult<HolonMigrationBarrierProgress>> AcquireAsync(
        HolonReplicationMigrationPreview preview, HolonReplicationMigrationAuthorizationContext context,
        IHolonReplicationMigrationAuthorizationPolicy policy, IEnumerable<HolonMigrationBarrierParticipant> participants,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(preview); ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(policy); ArgumentNullException.ThrowIfNull(participants);
        var result = new OASISResult<HolonMigrationBarrierProgress>();
        var progress = new List<HolonMigrationBarrierStoreProgress>();
        OASISResult<HolonMigrationBarrierProgress> Fail(string code, string message, Exception cause = null)
        {
            result.IsError = true; result.ErrorCount = 1; result.ErrorCode = code; result.Message = message; result.Exception = cause ?? result.Exception;
            result.Result = new(context, progress); return result;
        }
        var expectedContext = preview.AuthorizationContext(context.MigrationId, context.OperatorId, context.AuthorizationPolicyBinding);
        if (context.ContextDigest != expectedContext.ContextDigest || policy.BindingIdentity != context.AuthorizationPolicyBinding)
            return Fail("COSMIC_MIGRATION_BARRIER_SCOPE_MISMATCH", "The host grant must describe this exact reviewed preview.");
        if (preview.Findings.Any(f => f is not (HolonReplicationMigrationFinding.NativeWriteAndDeliveryFencesRequired
            or HolonReplicationMigrationFinding.ReplacementTargetEvidenceUnverified or HolonReplicationMigrationFinding.DeliveryInFlight)))
            return Fail("COSMIC_MIGRATION_BARRIER_PREVIEW_UNVERIFIED", "Original graph, transport and historical receiver proof are required before barrier acquisition.");
        var ids = preview.Receipts.SelectMany(r => r.Records).Select(r => r.Original.HolonId).Distinct().ToArray();
        if (ids.Length == 0 || ids.Length > 4096)
            return Fail("COSMIC_MIGRATION_BARRIER_SCOPE_INVALID", "A complete bounded original graph is required.");
        var stores = new[] { context.SourceStorage, context.OriginalTargetStorage, context.ReplacementRoute.TargetStorage }
            .GroupBy(s => s.BindingFingerprint, StringComparer.Ordinal).Select(g => g.First())
            .OrderBy(s => s.BindingFingerprint, StringComparer.Ordinal).ToArray();
        var owners = participants.Take(4).ToArray();
        if (owners.Length != stores.Length || owners.Any(p => p?.Storage == null || p.Provider == null)
            || owners.Select(p => p.Storage.BindingFingerprint).Distinct(StringComparer.Ordinal).Count() != owners.Length
            || stores.Any(s => !owners.Any(p => p.Storage.BindingFingerprint == s.BindingFingerprint)))
            return Fail("COSMIC_MIGRATION_BARRIER_CAPABILITY_REQUIRED", "Supply one explicit native barrier provider for every distinct physical participant.");
        var scopes = stores.Select(s => new HolonMigrationFenceRequest(context.MigrationId, context.OperatorId,
            s, preview.PreviewDigest, context.ContextDigest, ids)).ToArray();
        progress.AddRange(scopes.Select(s => new HolonMigrationBarrierStoreProgress(s.ExpectedStorage.BindingFingerprint,
            s.Fingerprint, HolonMigrationBarrierObservationState.Unobserved)));
        var active = -1;
        async Task<bool> Authorized()
        {
            cancellationToken.ThrowIfCancellationRequested();
            var permission = await policy.AuthorizeAsync(context, cancellationToken).ConfigureAwait(false);
            if (permission == null || permission.IsError || !permission.Result)
            {
                Fail(permission?.ErrorCode ?? "COSMIC_MIGRATION_AUTHORIZATION_DENIED",
                    "The exact current administrative grant does not admit barrier acquisition.", permission?.Exception);
                return false;
            }
            return true;
        }
        bool Accept(OASISResult<HolonMigrationFenceObservation> observation, int index, bool acquiring)
        {
            if (observation == null || observation.IsError)
            {
                progress[index] = progress[index] with { State = HolonMigrationBarrierObservationState.Unknown };
                Fail(observation?.ErrorCode ?? "COSMIC_MIGRATION_BARRIER_OUTCOME_UNKNOWN",
                    "A native barrier outcome requires reconciliation; no held scope was released.", observation?.Exception);
                return false;
            }
            var known = observation.Result;
            if (known == null || known.Request.Fingerprint != scopes[index].Fingerprint
                || (acquiring && known.State != HolonMigrationFenceState.Held))
            {
                progress[index] = progress[index] with { State = HolonMigrationBarrierObservationState.Unknown };
                Fail("COSMIC_MIGRATION_BARRIER_OBSERVATION_INVALID", "The native observation does not prove this exact scope.");
                return false;
            }
            progress[index] = progress[index] with { State = known.State switch {
                HolonMigrationFenceState.Held => HolonMigrationBarrierObservationState.Held,
                HolonMigrationFenceState.Cancelled => HolonMigrationBarrierObservationState.Cancelled,
                _ => HolonMigrationBarrierObservationState.Absent } };
            if (known.State == HolonMigrationFenceState.Cancelled)
            {
                Fail("COSMIC_MIGRATION_BARRIER_TERMINAL", "A cancelled migration identity cannot acquire more barriers."); return false;
            }
            // Native warnings survive successful observations; they cannot erase held state.
            if (observation.IsWarning) { result.IsWarning = true; result.WarningCount += Math.Max(1, observation.WarningCount); result.Exception ??= observation.Exception; }
            if (acquiring && observation.IsSaved) { result.IsSaved = true; result.SavedCount++; }
            return true;
        }
        try
        {
            if (!await Authorized().ConfigureAwait(false)) return result;
            // Read the entire durable participant set first. A missing/failed read
            // never causes a blind acquire on later stores.
            for (var i = 0; i < scopes.Length; i++)
            {
                active = i; cancellationToken.ThrowIfCancellationRequested();
                var provider = owners.Single(p => p.Storage.BindingFingerprint == stores[i].BindingFingerprint).Provider;
                var observed = await provider.ReadHolonMigrationFenceAsync(scopes[i], cancellationToken).ConfigureAwait(false);
                active = -1;
                if (!Accept(observed, i, false)) return result;
            }
            for (var i = 0; i < scopes.Length; i++)
            {
                if (progress[i].State == HolonMigrationBarrierObservationState.Held) continue;
                if (!await Authorized().ConfigureAwait(false)) return result;
                active = i;
                var provider = owners.Single(p => p.Storage.BindingFingerprint == stores[i].BindingFingerprint).Provider;
                var acquired = await provider.AcquireHolonMigrationFenceAsync(scopes[i], cancellationToken).ConfigureAwait(false);
                active = -1;
                if (!Accept(acquired, i, true)) return result;
            }
            if (!await Authorized().ConfigureAwait(false)) return result;
            result.Result = new(context, progress); result.IsLoaded = true;
            result.Message = "All native record barriers are held. In-flight reconciliation and cutover admission remain separate.";
            return result;
        }
        catch (Exception error)
        {
            if (active >= 0) progress[active] = progress[active] with { State = HolonMigrationBarrierObservationState.Unknown };
            return Fail(error is OperationCanceledException ? "COSMIC_MIGRATION_BARRIER_CANCELLED" : "COSMIC_MIGRATION_BARRIER_OUTCOME_UNKNOWN",
                "Preserve the exact reviewed context and reconcile native scopes; no compensation was attempted.", error);
        }
    }
}
