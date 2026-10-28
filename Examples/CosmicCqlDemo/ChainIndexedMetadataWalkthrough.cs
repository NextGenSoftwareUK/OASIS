using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.Common;

namespace CosmicCqlDemo;

/// <summary>
/// Metadata LINQ over a server-enrolled schema3 index. The host constructs the
/// SQLite store with HolonQueryableMetadataPolicy(new[] { "Status" }) and publishes
/// its authenticated projection before calling this sample. No source writes occur.
/// </summary>
public static class ChainIndexedMetadataWalkthrough
{
    public static async Task<(CosmicIndexedQueryExecution<Holon> Rows, long Count, bool Any, long EmptyWindow)> RunAsync(
        HolonManager manager,
        ICosmicChainQueryIndexScalarReader index,
        ICosmicChainQuerySourceObserver observer,
        CosmicChainQueryReadRequirement freshness,
        CosmicQueryReadAccess serverReadAccess,
        TextWriter output,
        CancellationToken cancellationToken = default)
    {
        var scope = Value(CosmicQueryScope.Named(freshness.Source.SourceProvider));
        var source = CosmicQuery<Holon>.Create(scope);
        source = Value(source.WithIndexedSource(freshness));
        source = Value(source.WithReadAccess(serverReadAccess));

        var query = Value(CosmicLinq.CreateIndexed(manager, index, observer, source));

        // Only the Status root is enrolled. The native SQL translator traverses
        // these exact nested keys; arbitrary private metadata cannot be queried.
        var coffees = query
            .Where(holon => CosmicQueryMetadata.Value<string>(holon, "Status", "category") == "coffee")
            .Where(holon => CosmicQueryMetadata.Value<long>(holon, "Status", "amount") > 9007199254740992L)
            .OrderBy(holon => CosmicQueryMetadata.Value<string>(holon, "Status", "category"));

        // A row page retains its checkpoint and independently observed source
        // receipts. Hydration/tracking belong to the shared V2 HolonManager.
        var rows = Value(await coffees.Take(10).ToCosmicIndexedPageAsync(cancellationToken));

        // These execute native scalar SQL, not a count of the fetched page.
        // The scalar assurance covers the authenticated index snapshot/head.
        var count = coffees.LongCount();
        var any = coffees.Any();
        var emptyWindow = coffees.Take(0).LongCount();

        await output.WriteLineAsync("\nQueryable metadata over an explicit chain index\n");
        await output.WriteLineAsync($"  Source: {freshness.Source.SourceProvider}; execution: {freshness.Source.ExecutionProvider}");
        await output.WriteLineAsync($"  Rows: {rows.Rows.Count}; LongCount: {count}; Any: {any}; zero window: {emptyWindow}");
        await output.WriteLineAsync($"  Acknowledged index slot: {rows.Checkpoint.Position.Slot}\n");

        return (rows, count, any, emptyWindow);
    }

    public sealed class MenuMetadata
    {
        public Guid Id { get; set; }
        public string Category { get; set; } = "";
        public long Amount { get; set; }
        public long? Missing { get; set; }
    }

    /// <summary>Native selected metadata, with source witnesses checked before DTO construction.</summary>
    public static async Task<CosmicIndexedProjectionExecution<MenuMetadata>> RunSelectionAsync(
        HolonManager manager,
        ICosmicChainQueryIndexProjectionReader index,
        ICosmicChainQuerySourceObserver observer,
        CosmicChainQueryReadRequirement freshness,
        CosmicQueryReadAccess serverReadAccess,
        CancellationToken cancellationToken = default)
    {
        var source = CosmicQuery<Holon>.Create(Value(CosmicQueryScope.Named(freshness.Source.SourceProvider)));
        source = Value(source.WithIndexedSource(freshness));
        source = Value(source.WithReadAccess(serverReadAccess));
        var query = Value(CosmicLinq.CreateIndexed(manager, index, observer, source));

        // Projection roots are enrolled independently from predicate roots.
        // Apply source filters/windows before selecting canonical and typed cells.
        var selected = query
            .Where(holon => CosmicQueryMetadata.Value<string>(holon, "Status", "category") == "coffee")
            .Take(10)
            .Select(holon => new MenuMetadata
            {
                Id = holon.Id,
                Category = CosmicQueryMetadata.Value<string>(holon, "Status", "category"),
                Amount = CosmicQueryMetadata.Value<long>(holon, "Status", "amount"),
                Missing = CosmicQueryMetadata.Value<long?>(holon, "Status", "absent")
            });

        return Value(await selected.ToCosmicIndexedProjectionPageAsync(cancellationToken));
    }

    private static T Value<T>(OASISResult<T> result)
    {
        if(result.IsError)throw new InvalidOperationException(result.ErrorCode+": "+result.Message,result.Exception);
        return result.Result;
    }
}
