using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.Common;

namespace CosmicCqlDemo;

/// <summary>Native Last and System.Index operations after the complete source window.</summary>
public static class V1TailQueries
{
    public static async Task<int> RunAsync(HolonManager manager,
        ICosmicQueryStorageProvider adapter, CosmicQuery<Holon> visible, TextWriter output)
    {
        var completed = 0;
        var ordered = Value(visible.OrderBy(row => row.Name));
        var descending = Value(visible.OrderByDescending(row => row.Name));
        var absent = Value(visible.Where(row => row.Name == "no such menu item").OrderBy(row => row.Name));

        // Last selects AFTER source Skip/Take. Reversing the source first would
        // change these windows; counting then re-reading would introduce a race.
        await Show("Last / ascending", ordered.Last(), "D-owned");
        await Show("Last / descending", descending.Last(), "B-owned");
        await Show("Take(2).Last", ordered.Take(2).Last(), "C-public");
        await Show("Skip(1).Last", ordered.Skip(1).Last(), "D-owned");
        await Show("Skip(1).Take(1).Last", ordered.Skip(1).Take(1).Last(), "C-public");
        await Show("Take beyond actual size / Last", ordered.Take(10).Last(), "D-owned");

        // Equal primary keys force secondary ordering to determine the tail.
        await Show("Last / tied primary, descending secondary", visible
            .OrderBy(row => CosmicQueryMetadata.Value<long>(row, "stock"))
            .ThenByDescending(row => row.Name).Last(), "B-owned");

        await Show("Last / empty native filter", absent.Last(), error: "CQL_SEQUENCE_EMPTY");
        await Show("LastOrDefault / empty native filter", absent.LastOrDefault());
        await Show("Take(0).Last", ordered.Take(0).Last(), error: "CQL_SEQUENCE_EMPTY");
        await Show("Take(0).LastOrDefault", ordered.Take(0).LastOrDefault());

        // Last has an explicit ordering contract. The planner does not silently
        // supply an application order or pick an arbitrary provider.
        await Show("Last / explicit order required", visible.Last(), error: "CQL_ORDER_REQUIRED");

        // ^1 is the final element; ^0 lies just beyond the end. These are the
        // System.Index overloads, alongside the earlier zero-based int API.
        await Show("ElementAt(^1)", ordered.ElementAt(^1), "D-owned");
        await Show("ElementAt(^2)", ordered.ElementAt(^2), "C-public");
        await Show("ElementAt(^3)", ordered.ElementAt(^3), "B-owned");
        await Show("ElementAt(^4) / actual range error", ordered.ElementAt(^4), error: "CQL_INDEX_OUT_OF_RANGE");
        await Show("ElementAtOrDefault(^4)", ordered.ElementAtOrDefault(^4));
        await Show("ElementAt(^0) / construction error", ordered.ElementAt(^0), error: "CQL_INDEX_OUT_OF_RANGE");
        await Show("ElementAtOrDefault(^0) / no I/O", ordered.ElementAtOrDefault(^0));

        await Show("Window-relative ^2", ordered.Skip(1).Take(2).ElementAt(^2), "C-public");
        await Show("Descending source window / ^2", descending.Take(2).ElementAt(^2), "D-owned");
        await Show("^2 outside explicit Take(1)", ordered.Take(1).ElementAtOrDefault(^2));
        await Show("System.Index from start", ordered.ElementAt(new Index(1)), "C-public");
        await Show("System.Index from start / OrDefault", ordered.ElementAtOrDefault(new Index(1)), "C-public");

        // Tail root selection uses the same native read/graph owner as ordinary
        // rows. Selected children still obey the root's trusted read policy.
        var graph = Value(visible.Where(row => row.Name == "C-public")
            .OrderBy(row => row.Name)
            .WithChildren(Value(CosmicQueryGraphOptions.Create(recursive: true, maxChildDepth: 2, maximumNodes: 8))));
        await Show("Last / selected visible children", graph.Last(), "C-public", checkChildren: true);

        // The second sort reverses the full declared domain, including null
        // ranks and identity ties. It reuses each adapter's exact scalar keys.
        await Show("Last / null metadata rank", visible
            .OrderBy(row => CosmicQueryMetadata.Value<string>(row, "nullable"))
            .ThenBy(row => row.Name).Last(), "D-owned");
        await Show("Last / adjacent UTC ticks", visible
            .OrderBy(row => CosmicQueryMetadata.Value<DateTime>(row, "observedAt"))
            .Take(2).Last(), "C-public");
        await Show("Last / exact UInt64 order", visible
            .OrderBy(row => CosmicQueryMetadata.Value<ulong>(row, "counter"))
            .Take(2).Last(), "C-public");
        await Show("Last / exact binary64 order", visible
            .OrderBy(row => CosmicQueryMetadata.Value<double>(row, "reading"))
            .Take(2).Last(), "C-public");
        await Show("Last / exact Decimal order", visible
            .OrderBy(row => CosmicQueryMetadata.Value<decimal>(row, "price"))
            .Take(2).Last(), "C-public");

        return completed;

        async Task Show(string title, OASISResult<CosmicElementQuery<Holon>> query,
            string? expectedName = null, string? error = null, bool checkChildren = false)
        {
            // Expected rejection cases identify their exact error. An unexpected
            // construction, planning or execution failure always fails the demo.
            if (query.IsError) CheckError(query.ErrorCode, query.Message);
            else
            {
                var plan = CosmicElementPlanner.Plan(query.Result, adapter.QueryProviderType,
                    new[] { new CosmicQueryTarget(adapter.QueryProviderType, adapter.QueryCapabilities) });
                if (plan.IsError) CheckError(plan.ErrorCode, plan.Message);
                else
                {
                    var result = await manager.ExecuteElementQueryAsync(plan.Result, new[] { adapter });
                    if (result.IsError) CheckError(result.ErrorCode, result.Message);
                    else
                    {
                        if (error != null || !result.IsLoaded || result.Result?.Name != expectedName)
                            throw new InvalidOperationException(title + ": unexpected tail result.");
                        if (checkChildren && !result.Result!.Children.Select(child => child.Name)
                            .SequenceEqual(new[] { "public child", "owned child" }))
                            throw new InvalidOperationException(title + ": child visibility/membership changed.");
                    }
                }
            }

            completed++;
            await output.WriteLineAsync($"\n{105 + completed}. {title}\n\n  {error ?? expectedName ?? "null (successful empty result)"}\n");

            void CheckError(string code, string message)
            {
                if (error == null || code != error)
                    throw new InvalidOperationException(title + ": " + code + ": " + message);
            }
        }
    }

    private static T Value<T>(OASISResult<T> result)
    {
        if (result.IsError) throw new InvalidOperationException(result.ErrorCode + ": " + result.Message, result.Exception);
        return result.Result;
    }
}
