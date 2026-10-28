using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.Common;

namespace CosmicCqlDemo;

/// <summary>
/// Read a chain through an explicitly configured durable query index. This is
/// shared COSMIC execution, not native chain SQL or an automatic provider fallback.
/// The native Solana acceptance test runs this exact sample against SQLite.
/// </summary>
public static class ChainIndexedWalkthrough
{
    public static async Task<(CosmicIndexedQueryExecution<Holon> Rows, CosmicIndexedQueryExecution<Holon> Empty)> RunAsync(
        HolonManager manager,
        ICosmicChainQueryIndexReader index,
        ICosmicChainQuerySourceObserver sourceObserver,
        CosmicChainQueryReadRequirement freshness,
        CosmicQueryReadAccess serverReadAccess,
        Guid selectedId,
        TextWriter output,
        CancellationToken cancellationToken = default)
    {
        // The trusted host owns the source/catalogue/index binding, authenticated
        // actor policy and already-published index. The sample never signs a write.
        var scope = Value(CosmicQueryScope.Named(freshness.Source.SourceProvider));
        var source = CosmicQuery<Holon>.Create(scope);
        source = Value(source.WithIndexedSource(freshness));
        source = Value(source.WithReadAccess(serverReadAccess));

        var query = Value(CosmicLinq.CreateIndexed(manager, index, sourceObserver, source));

        // Build ordinary LINQ over the shared CQL binder. Capture scalar query
        // values first; arbitrary client property getters/methods are not invoked.
        var selected = query
            .Where(holon => holon.Id == selectedId)
            .OrderBy(holon => holon.Name)
            .Take(1);

        // Use the indexed page terminal to retain the checkpoint, freshly observed
        // finalized head and each row's authoritative source commit reference.
        var rows = Value(await selected.ToCosmicIndexedPageAsync(cancellationToken));

        await output.WriteLineAsync("\n1. Explicit chain query through its durable index\n");
        await output.WriteLineAsync($"   Source:    {freshness.Source.SourceProvider}");
        await output.WriteLineAsync($"   Execution: {freshness.Source.ExecutionProvider}");
        await output.WriteLineAsync($"   Coverage starts at slot: {rows.Checkpoint.CompleteFromSlot}");
        await output.WriteLineAsync($"   Index checkpoint:       {rows.Checkpoint.Position.Slot}");
        await output.WriteLineAsync($"   Fresh finalized head:   {rows.Head.Position.Slot}\n");

        for (var i = 0; i < rows.Rows.Count; i++)
        {
            var holon = rows.Rows[i];
            var receipt = rows.Receipts[i];

            await output.WriteLineAsync($"   {holon.Name} (version {holon.Version})");
            await output.WriteLineAsync($"   Source commit: {receipt.CommitReference}");
            await output.WriteLineAsync($"   Source slot:   {receipt.Position.Slot}\n");
        }

        // An empty result still needs complete index coverage and fresh source
        // evidence. It cannot hide a missing transaction, wrong network or block.
        var empty = Value(await query
            .Where(holon => holon.Name == "does-not-exist")
            .Take(1)
            .ToCosmicIndexedPageAsync(cancellationToken));

        await output.WriteLineAsync($"\n2. Successful empty query: {empty.Rows.Count} rows\n");
        await output.WriteLineAsync("   Assurance: trusted source observation; not a cryptographic inclusion proof.\n");

        return (rows, empty);
    }

    private static T Value<T>(OASISResult<T> result)
    {
        if (result.IsError)
            throw new InvalidOperationException(result.ErrorCode + ": " + result.Message, result.Exception);

        return result.Result;
    }
}
