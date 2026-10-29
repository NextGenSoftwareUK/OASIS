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
        HolonReplicaHeadReadConsistency consistency = HolonReplicaHeadReadConsistency.NativeSnapshot,
        IBoundHolonReplicationOutboxSource migrationSource = null)
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
        if (migrationSource != null)
        {
            // Global discovery must retain every still-pending target, including
            // targets after a supplementary-Unicode cursor. Newer operations may
            // also contribute a head for an already completed original target.
            var required = rows.Where(row => row.State != HolonDeliveryState.Completed).Select(row => row.TargetId).ToHashSet(StringComparer.Ordinal);
            string routeAfter = null; var routePages = 0;
            do
            {
                var route = await NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.HolonReplicationMigrationAudit.ReadPageAsync(
                    migrationSource, source, 1, routeAfter);
                if (route.IsError || route.Result == null) throw new InvalidOperationException(route.Message, route.Exception);
                foreach (var head in route.Result.Heads) required.Remove(head.TargetId);
                routeAfter = route.Result.NextAfterTargetId;
                if (++routePages > 4096) throw new InvalidOperationException("Global fixture inventory exceeded its explicit read budget.");
            } while (routeAfter != null);
            if (required.Count != 0) throw new InvalidOperationException("Global route discovery omitted an unfinished original target.");
        }
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

public static class CosmicLineageInventoryProbe
{
    public static async Task VerifyPreviewAsync(IHolonReplicationRouteInventoryOutbox outbox,
        CosmicStorageIdentity source, CosmicStorageIdentity originalTarget, string sourceId,
        Guid operation, int originalCount,
        Func<Task<NextGenSoftware.OASIS.Common.OASISResult<NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.HolonReplicationReceiptPreflightReport>>> readReceipt,
        HolonReplicaHeadReadConsistency consistency = HolonReplicaHeadReadConsistency.NativeSnapshot)
    {
        // Proposed replacement identity is not contacted or admitted here. The
        // same-authority Mongo fixture needs a distinct-authority proposal only.
        var candidate = originalTarget.Authority == source.Authority
            ? new CosmicStorageIdentity(NextGenSoftware.OASIS.API.Core.Enums.ProviderType.SQLLiteDBOASIS,
                new string('F', 64), "preview-only-not-contacted") : originalTarget;
        var route = new NextGenSoftware.OASIS.API.Core.Configuration.HolonReplicationRouteBinding(source, candidate, "preview-only-policy");
        async Task<NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.HolonReplicationMigrationPreview> Read()
        {
            var result = await NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.HolonReplicationMigrationPreviewReader.ReadAsync(
                (IHolonReplicationLineageInventoryOutbox)outbox, source, sourceId, "target", originalTarget, route,
                (row, token) => row.OperationId == operation ? readReceipt() : throw new InvalidOperationException("Unexpected fixture operation."),
                maxObligations: originalCount, pageSize: 1, sourceConsistency: consistency);
            if (result.IsError || result.Result == null) throw new InvalidOperationException(result.Message, result.Exception);
            return result.Result;
        }
        var preview = await Read();
        if (preview.Obligations.Count != originalCount || preview.Receipts.Count != 1
            || preview.Obligations.All(row => row.State != HolonDeliveryState.Completed)
            || preview.PreviewDigest != (await Read()).PreviewDigest
            || !preview.Findings.Contains(NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.HolonReplicationMigrationFinding.NativeWriteAndDeliveryFencesRequired)
            || !preview.Findings.Contains(NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.HolonReplicationMigrationFinding.ReplacementTargetEvidenceUnverified))
            throw new InvalidOperationException("Native preview omitted evidence, changed without writes, or concealed unresolved execution gates.");
    }

    public static async Task VerifyWindowAsync(IHolonReplicationRouteInventoryOutbox outbox, CosmicStorageIdentity source,
        string sourceId, Guid originalOperation, int originalCount, Guid? appendedOperation = null, Func<Task> append = null,
        HolonReplicaHeadReadConsistency consistency = HolonReplicaHeadReadConsistency.NativeSnapshot)
    {
        var reader = outbox as IHolonReplicationLineageInventoryOutbox
            ?? throw new InvalidOperationException("Source-wide lineage inventory capability is required.");
        if (consistency == HolonReplicaHeadReadConsistency.ReadCommittedValidated)
        {
            var strict = await reader.ReadLineageObligationsAsync(new(source, sourceId));
            if (!strict.IsError || strict.ErrorCode != "COSMIC_LINEAGE_INVENTORY_CONSISTENCY_UNSUPPORTED")
                throw new InvalidOperationException("A weak source cannot silently satisfy strict lineage reads.");
        }
        var first = await reader.ReadLineageObligationsAsync(new(source, sourceId, 1, requiredConsistency: consistency));
        if (first.IsError || first.Result == null || first.Result.Obligations.Count != 1)
            throw new InvalidOperationException(first.Message, first.Exception);
        var through = first.Result.ThroughSequence;
        if (append != null) await append();
        async Task<List<HolonReplicationOperationObligation>> Remaining(HolonReplicationLineagePage start)
        {
            var rows = new List<HolonReplicationOperationObligation>(start.Obligations); var cursor = start.NextAfterSequence; var pages = 0;
            while (cursor.HasValue)
            {
                var next = await reader.ReadLineageObligationsAsync(new(source, sourceId, 1, cursor.Value, start.ThroughSequence, consistency));
                if (next.IsError || next.Result == null || next.Result.ThroughSequence != start.ThroughSequence || next.IsSaved)
                    throw new InvalidOperationException("Lineage pagination changed its captured scope.", next.Exception);
                rows.AddRange(next.Result.Obligations); cursor = next.Result.NextAfterSequence;
                if (++pages > 4096) throw new InvalidOperationException("Lineage fixture exceeded its explicit read budget.");
            }
            return rows;
        }
        var captured = await Remaining(first.Result);
        if (captured.Count != originalCount || captured.Any(row => row.OperationId != originalOperation || row.Sequence > through)
            || !captured.Any(row => row.State == HolonDeliveryState.Completed))
            throw new InvalidOperationException("A captured window omitted original work or included a later append.");
        var fresh = await reader.ReadLineageObligationsAsync(new(source, sourceId, 1, requiredConsistency: consistency));
        if (fresh.IsError || fresh.Result == null) throw new InvalidOperationException(fresh.Message, fresh.Exception);
        var current = await Remaining(fresh.Result);
        if (append != null && (current.Count != originalCount * 2 || fresh.Result.ThroughSequence <= through
            || !current.Any(row => row.OperationId == appendedOperation)))
            throw new InvalidOperationException("A new window must observe the later original operation.");
        var absent = await reader.ReadLineageObligationsAsync(new(source, "unknown-" + Guid.NewGuid(), requiredConsistency: consistency));
        if (absent.IsError || absent.Result.Obligations.Count != 0 || absent.Result.ThroughSequence != 0)
            throw new InvalidOperationException("Unknown lineage must remain an explicit empty observation.");
        var wrong = new CosmicStorageIdentity(source.Authority, source.EndpointFingerprint, Guid.NewGuid());
        var rejected = await reader.ReadLineageObligationsAsync(new(wrong, sourceId, requiredConsistency: consistency));
        if (!rejected.IsError || rejected.Result != null) throw new InvalidOperationException("Wrong lineage source binding must be rejected.");
        using var cancelled = new CancellationTokenSource(); cancelled.Cancel();
        try { await reader.ReadLineageObligationsAsync(new(source, sourceId, requiredConsistency: consistency), cancelled.Token); }
        catch (OperationCanceledException) { return; }
        throw new InvalidOperationException("Cancelled lineage inventory accessed native state.");
    }
}
