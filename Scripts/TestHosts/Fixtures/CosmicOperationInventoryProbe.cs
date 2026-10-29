using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace CosmicNativeAcceptance;

public static class CosmicOperationInventoryProbe
{
    public static async Task VerifyAsync(IHolonReplicationRouteInventoryOutbox outbox, CosmicStorageIdentity source,
        Guid operation, string sourceId, Guid actor, IEnumerable<string> expectedTargets, bool requireCompleted,
        HolonReplicaHeadReadConsistency consistency = HolonReplicaHeadReadConsistency.NativeSnapshot)
    {
        var reader = outbox as IHolonReplicationOperationInventoryOutbox
            ?? throw new InvalidOperationException("The native source must expose complete operation inventory.");
        if (consistency == HolonReplicaHeadReadConsistency.ReadCommittedValidated)
        {
            var strict = await reader.ReadOperationObligationsAsync(new(source, operation));
            if (!strict.IsError || strict.Result != null || strict.ErrorCode != "COSMIC_OPERATION_INVENTORY_CONSISTENCY_UNSUPPORTED")
                throw new InvalidOperationException("A weaker native reader must explicitly refuse strict inventory.");
        }
        string after = null; var rows = new List<HolonReplicationOperationObligation>(); var pages = 0;
        do
        {
            var request = new HolonReplicationOperationReadRequest(source, operation, 1, after, consistency);
            var observed = await reader.ReadOperationObligationsAsync(request);
            if (observed.IsError || observed.Result == null) throw new InvalidOperationException(observed.Message, observed.Exception);
            if (observed.IsSaved || observed.Result.Consistency != consistency) throw new InvalidOperationException("Inventory must be read-only and report actual consistency.");
            var repeated = await reader.ReadOperationObligationsAsync(request);
            if (repeated.IsError || repeated.Result.ObservationDigest != observed.Result.ObservationDigest)
                throw new InvalidOperationException("Unchanged operation observations must have a stable digest.");
            rows.AddRange(observed.Result.Obligations); after = observed.Result.NextAfterTargetId;
            if (++pages > 256) throw new InvalidOperationException("Native pagination did not terminate.");
        } while (after != null);
        if (!rows.Select(row => row.TargetId).SequenceEqual(expectedTargets.OrderBy(value => Convert.ToHexString(System.Text.Encoding.UTF8.GetBytes(value)), StringComparer.Ordinal))
            || rows.Any(row => row.OperationId != operation || row.SourceId != sourceId || row.ActorId != actor)
            || requireCompleted && !rows.Any(row => row.State == HolonDeliveryState.Completed))
            throw new InvalidOperationException("Complete operation inventory omitted or rebound an original obligation.");
        var missing = await reader.ReadOperationObligationsAsync(new(source, Guid.NewGuid(), requiredConsistency: consistency));
        if (missing.IsError || missing.Result.Obligations.Count != 0 || missing.Result.NextAfterTargetId != null)
            throw new InvalidOperationException("Unknown operation inventory must be an explicit empty observation.");
        var wrong = new CosmicStorageIdentity(source.Authority, source.EndpointFingerprint, Guid.NewGuid());
        var rejected = await reader.ReadOperationObligationsAsync(new(wrong, operation, requiredConsistency: consistency));
        if (!rejected.IsError || rejected.Result != null) throw new InvalidOperationException("Wrong physical binding must be rejected.");
        using var cancelled = new CancellationTokenSource(); cancelled.Cancel();
        try { await reader.ReadOperationObligationsAsync(new(source, operation, requiredConsistency: consistency), cancelled.Token); }
        catch (OperationCanceledException) { return; }
        throw new InvalidOperationException("Cancelled inventory must not access native state.");
    }

    // The caller has already applied and acknowledged this exact target mutation.
    public static async Task CompleteAlreadyAppliedTargetAsync(IHolonReplicationRouteInventoryOutbox outbox,
        CosmicStorageIdentity source, Guid operation, string target,
        HolonReplicaHeadReadConsistency consistency = HolonReplicaHeadReadConsistency.NativeSnapshot)
    {
        var reader = (IHolonReplicationOperationInventoryOutbox)outbox;
        var claim = await outbox.ClaimNextAsync(target, "owned-operation-inventory", TimeSpan.FromMinutes(1));
        if (claim.IsError || claim.Result?.OperationId != operation) throw new InvalidOperationException("The applied target's exact obligation must be owned.", claim.Exception);
        var request = new HolonReplicationOperationReadRequest(source, operation, requiredConsistency: consistency);
        var before = await reader.ReadOperationObligationsAsync(request);
        var renewed = await outbox.RenewAsync(claim.Result, TimeSpan.FromMinutes(2));
        if (before.IsError || renewed.IsError || !renewed.Result) throw new InvalidOperationException("Fixture lease renewal failed.");
        var after = await reader.ReadOperationObligationsAsync(request);
        if (after.IsError || before.Result.Obligations.Single(row => row.TargetId == target).StateDigest
            == after.Result.Obligations.Single(row => row.TargetId == target).StateDigest)
            throw new InvalidOperationException("Native renewal must change the hidden-state digest.");
        var finished = await outbox.FinishAsync(claim.Result, HolonDeliveryOutcome.Completed);
        if (finished.IsError || !finished.Result) throw new InvalidOperationException("Acknowledged fixture delivery could not be completed.", finished.Exception);
    }
}
