using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Neo4j.Driver;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using INode = Neo4j.Driver.INode;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS;

internal sealed class NeoMigrationFenceException(string code, string message) : InvalidOperationException(message)
{ internal string Code { get; } = code; }

internal static class NeoHolonMigrationFence
{
    internal const string SchemaId = "cosmic-migration-record-gate-schema-v1";

    internal static async Task RequireSchemaAsync(IAsyncTransaction tx)
    {
        var rows = await (await tx.RunAsync("MATCH (g:CosmicMigrationRecordGate {HolonId:$schema}) RETURN g.Format AS format",
            new { schema = SchemaId }).ConfigureAwait(false)).ToListAsync().ConfigureAwait(false);
        if (rows.Count != 1 || rows[0]["format"] is not long format || format != 1)
            throw new InvalidDataException("Native migration admission schema is missing or invalid; it was not initialized.");
    }

    private static async Task<INode> LockRecordAsync(IAsyncTransaction tx, Guid id, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        var row = await (await tx.RunAsync("MERGE (g:CosmicMigrationRecordGate {HolonId:$id}) "
            + "ON CREATE SET g.FenceId='',g.FenceHash='',g.Epoch=0 "
            + "SET g.Epoch=g.Epoch+1 RETURN g", new { id = id.ToString("D") }).ConfigureAwait(false)).SingleAsync().ConfigureAwait(false);
        var gate = row["g"].As<INode>();
        if (!gate.Properties.TryGetValue("Epoch", out var epoch) || epoch is not long number || number < 1
            || !gate.Properties.TryGetValue("FenceId", out var fence) || fence is not string fid
            || !gate.Properties.TryGetValue("FenceHash", out var hash) || hash is not string digest
            || (fid.Length == 0) != (digest.Length == 0)) throw new InvalidDataException("Native record admission metadata is invalid.");
        return gate;
    }

    internal static async Task AssertUnfencedAsync(IAsyncTransaction tx, IEnumerable<Guid> ids, CancellationToken token)
    {
        await RequireSchemaAsync(tx).ConfigureAwait(false);
        foreach (var id in ids.Distinct().OrderBy(id => id.ToString("D"), StringComparer.Ordinal))
            if (((await LockRecordAsync(tx, id, token).ConfigureAwait(false))["FenceId"]).As<string>().Length != 0)
                throw new NeoMigrationFenceException("COSMIC_MIGRATION_RECORD_FENCED", "An affected record is held by a durable migration barrier.");
    }

    private static HolonMigrationFenceObservation Read(INode node, HolonMigrationFenceRequest request)
    {
        if (node == null) return new(request, Guid.Empty, HolonMigrationFenceState.Absent);
        string Text(string name) => node.Properties.TryGetValue(name, out var value) && value is string text
            ? text : throw new InvalidDataException("Native migration fence receipt is incomplete.");
        var original = HolonMigrationFenceRequest.ReadCanonical(Text("RequestJson"), request.ExpectedStorage);
        if (Text("MigrationId") != original.MigrationId.ToString("D") || Text("RequestHash") != original.Fingerprint
            || !Guid.TryParseExact(Text("OwnerToken"), "D", out var owner) || owner == Guid.Empty
            || Text("FenceState") is not ("Held" or "Cancelled" or "MutationStarted")) throw new InvalidDataException("Native fence receipt integrity is invalid.");
        if (original.Fingerprint != request.Fingerprint)
            throw new NeoMigrationFenceException("COSMIC_MIGRATION_FENCE_SCOPE_MISMATCH", "The migration identity belongs to another exact operator or review scope.");
        var state = Text("FenceState");
        var phase = state == "Held" ? HolonMigrationFenceState.Held : state == "Cancelled" ? HolonMigrationFenceState.Cancelled : HolonMigrationFenceState.MutationStarted;
        var digest = node.Properties.TryGetValue("MutationDigest", out var value) ? value as string
            ?? throw new InvalidDataException("Native mutation intent is not canonical.") : null;
        return new(request, owner, phase, digest);
    }

    internal enum Action { Acquire, Read, Cancel, BeginMutation }
    internal static async Task<(HolonMigrationFenceObservation Result, bool Changed)> ApplyAsync(IAsyncTransaction tx,
        HolonMigrationFenceRequest request, Action action, Guid owner, CancellationToken token, string mutationDigest = null)
    {
        await RequireSchemaAsync(tx).ConfigureAwait(false); token.ThrowIfCancellationRequested();
        var claim = Guid.NewGuid().ToString("D"); INode node;
        if (action == Action.Read)
        {
            var rows = await (await tx.RunAsync("MATCH (f:CosmicMigrationFence {MigrationId:$id}) RETURN f LIMIT 2",
                new { id = request.MigrationId.ToString("D") }).ConfigureAwait(false)).ToListAsync().ConfigureAwait(false);
            if (rows.Count > 1) throw new InvalidDataException("Native fence identity is not unique.");
            node = rows.Count == 0 ? null : rows[0]["f"].As<INode>();
        }
        else
        {
            var locked = await (await tx.RunAsync("MERGE (f:CosmicMigrationFence {MigrationId:$id}) "
                + "ON CREATE SET f.__cosmicClaim=$claim,f.LockEpoch=0 SET f.LockEpoch=coalesce(f.LockEpoch,0)+1 RETURN f",
                new { id = request.MigrationId.ToString("D"), claim }).ConfigureAwait(false)).SingleAsync().ConfigureAwait(false);
            node = locked["f"].As<INode>();
        }
        var fresh = node != null && node.Properties.TryGetValue("__cosmicClaim", out var marker) && marker is string text && text == claim;
        var current = fresh ? new HolonMigrationFenceObservation(request, Guid.Empty, HolonMigrationFenceState.Absent) : Read(node, request);
        if (current.State == HolonMigrationFenceState.Absent)
        {
            var orphan = await (await tx.RunAsync("MATCH (g:CosmicMigrationRecordGate {FenceId:$id}) RETURN count(g) AS count",
                new { id = request.MigrationId.ToString("D") }).ConfigureAwait(false)).SingleAsync().ConfigureAwait(false);
            if (orphan["count"].As<long>() != 0) throw new InvalidDataException("Native record ownership exists without its original fence receipt.");
        }
        if (action == Action.Read) return (current, false);
        if (action == Action.Acquire)
        {
            if (current.State == HolonMigrationFenceState.Cancelled)
                throw new NeoMigrationFenceException("COSMIC_MIGRATION_FENCE_TERMINAL", "A cancelled migration identity cannot be reacquired.");
            if (current.State is HolonMigrationFenceState.Held or HolonMigrationFenceState.MutationStarted) return (current, false);
            foreach (var id in request.HolonIds)
            {
                var gate = await LockRecordAsync(tx, id, token).ConfigureAwait(false);
                if (gate["FenceId"].As<string>().Length != 0)
                    throw new NeoMigrationFenceException("COSMIC_MIGRATION_FENCE_CONFLICT", "Another migration holds an overlapping record scope.");
                await (await tx.RunAsync("MATCH (g:CosmicMigrationRecordGate {HolonId:$id}) SET g.FenceId=$migration,g.FenceHash=$hash",
                    new { id = id.ToString("D"), migration = request.MigrationId.ToString("D"), hash = request.Fingerprint }).ConfigureAwait(false)).ConsumeAsync().ConfigureAwait(false);
            }
            owner = Guid.NewGuid();
            await (await tx.RunAsync("MATCH (f:CosmicMigrationFence {MigrationId:$id,__cosmicClaim:$claim}) "
                + "SET f.RequestJson=$json,f.RequestHash=$hash,f.OwnerToken=$owner,f.FenceState='Held' REMOVE f.__cosmicClaim",
                new { id = request.MigrationId.ToString("D"), claim, json = request.CanonicalJson, hash = request.Fingerprint, owner = owner.ToString("D") }).ConfigureAwait(false)).ConsumeAsync().ConfigureAwait(false);
            return (new(request, owner, HolonMigrationFenceState.Held), true);
        }
        if (action == Action.BeginMutation)
        {
            _ = new HolonMigrationMutationStartRequest(request, owner, mutationDigest);
            if (current.State == HolonMigrationFenceState.Absent || current.OwnerToken != owner)
                throw new NeoMigrationFenceException("COSMIC_MIGRATION_FENCE_OWNER_MISMATCH", "Only the exact held native owner may record mutation start.");
            if (current.State == HolonMigrationFenceState.Cancelled)
                throw new NeoMigrationFenceException("COSMIC_MIGRATION_FENCE_TERMINAL", "A cancelled migration identity cannot begin mutation.");
            if (current.State == HolonMigrationFenceState.MutationStarted)
            {
                if (current.MutationDigest != mutationDigest) throw new NeoMigrationFenceException("COSMIC_MIGRATION_MUTATION_INTENT_REUSED", "The durable mutation intent cannot be replaced.");
                return (current, false);
            }
            foreach (var id in request.HolonIds)
            {
                var gate = await LockRecordAsync(tx, id, token).ConfigureAwait(false);
                if (gate["FenceId"].As<string>() != request.MigrationId.ToString("D") || gate["FenceHash"].As<string>() != request.Fingerprint)
                    throw new InvalidDataException("Native record membership differs from the held mutation scope.");
            }
            var changed = await (await tx.RunAsync("MATCH (f:CosmicMigrationFence {MigrationId:$id,OwnerToken:$owner,RequestHash:$hash,FenceState:'Held'}) "
                + "WHERE f.MutationDigest IS NULL SET f.FenceState='MutationStarted',f.MutationDigest=$digest RETURN count(f) AS count",
                new { id = request.MigrationId.ToString("D"), owner = owner.ToString("D"), hash = request.Fingerprint, digest = mutationDigest }).ConfigureAwait(false)).SingleAsync().ConfigureAwait(false);
            if (changed["count"].As<long>() != 1) throw new InvalidDataException("Native phase compare-and-swap did not acknowledge the original owner.");
            return (new(request, owner, HolonMigrationFenceState.MutationStarted, mutationDigest), true);
        }
        if (current.State == HolonMigrationFenceState.Absent || current.OwnerToken != owner)
            throw new NeoMigrationFenceException("COSMIC_MIGRATION_FENCE_OWNER_MISMATCH", "Only the exact pre-mutation fence owner may cancel it.");
        if (current.State == HolonMigrationFenceState.Cancelled) return (current, false);
        if (current.State == HolonMigrationFenceState.MutationStarted)
            throw new NeoMigrationFenceException("COSMIC_MIGRATION_MUTATION_ALREADY_STARTED", "Started or uncertain mutation cannot use pre-mutation cancellation.");
        foreach (var id in request.HolonIds)
        {
            var gate = await LockRecordAsync(tx, id, token).ConfigureAwait(false);
            if (gate["FenceId"].As<string>() != request.MigrationId.ToString("D") || gate["FenceHash"].As<string>() != request.Fingerprint)
                throw new InvalidDataException("Native fence membership differs; no cancellation was committed.");
            await (await tx.RunAsync("MATCH (g:CosmicMigrationRecordGate {HolonId:$id}) SET g.FenceId='',g.FenceHash=''",
                new { id = id.ToString("D") }).ConfigureAwait(false)).ConsumeAsync().ConfigureAwait(false);
        }
        await (await tx.RunAsync("MATCH (f:CosmicMigrationFence {MigrationId:$id}) SET f.FenceState='Cancelled'",
            new { id = request.MigrationId.ToString("D") }).ConfigureAwait(false)).ConsumeAsync().ConfigureAwait(false);
        return (new(request, owner, HolonMigrationFenceState.Cancelled), true);
    }
}
