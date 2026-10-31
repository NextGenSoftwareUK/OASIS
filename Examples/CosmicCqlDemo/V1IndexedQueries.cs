using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.Common;

namespace CosmicCqlDemo;

/// <summary>Zero-based entity indexing through the shared native paging planner.</summary>
public static class V1IndexedQueries
{
    public static async Task<int> RunAsync(HolonManager manager,
        ICosmicQueryStorageProvider adapter, CosmicQuery<Holon> visible, TextWriter output)
    {
        var completed = 0;
        var ordered = Value(visible.OrderBy(row => row.Name));
        var absent = Value(visible.Where(row => row.Name == "no such menu item"));

        // Index zero means the first VISIBLE element, after the source filters.
        // One native row is requested at the composed source offset + index.
        await Show("ElementAt(0)", ordered.ElementAt(0), "B-owned");
        await Show("ElementAt(1)", ordered.ElementAt(1), "C-public");
        await Show("ElementAt(2)", ordered.ElementAt(2), "D-owned");

        // The strict terminal reports an index error; OrDefault returns a
        // successful null when a successful query has no such element.
        await Show("ElementAt / outside native input", ordered.ElementAt(3), error: "CQL_INDEX_OUT_OF_RANGE");
        await Show("ElementAtOrDefault / outside native input", ordered.ElementAtOrDefault(3));

        // A negative OrDefault index is known empty, with no provider read.
        // Cancellation/inherited actor checks still apply to this pure result.
        await Show("ElementAtOrDefault(-1) / no I/O", ordered.ElementAtOrDefault(-1));

        // Indexing is relative to the composed window. Skip(1)/Take(2) leaves
        // C-public,D-owned; its element one is D-owned, not C-public.
        await Show("Window-relative index", ordered.Skip(1).Take(2).ElementAt(1), "D-owned");

        // An explicit Take bounds the sequence. Never read the next database
        // row outside that window just to satisfy an out-of-range index.
        await Show("Index outside Take / strict", ordered.Skip(1).Take(1).ElementAt(1), error: "CQL_INDEX_OUT_OF_RANGE");
        await Show("Index outside Take / default", ordered.Take(1).ElementAtOrDefault(1));
        await Show("Take(0) / strict", ordered.Take(0).ElementAt(0), error: "CQL_INDEX_OUT_OF_RANGE");
        await Show("Take(0) / default", ordered.Take(0).ElementAtOrDefault(0));

        // A filter can produce an empty sequence whose size was not known
        // during planning. Provider failures still remain explicit errors.
        await Show("Empty native filter / default", absent.ElementAtOrDefault(0));
        await Show("Empty native filter / strict", absent.ElementAt(0), error: "CQL_INDEX_OUT_OF_RANGE");

        await Show("Earlier Skip is preserved", ordered.Skip(1).ElementAt(0), "C-public");
        await Show("ElementAtOrDefault / existing element", ordered.ElementAtOrDefault(1), "C-public");

        return completed;

        async Task Show(string title, OASISResult<CosmicElementQuery<Holon>> query,
            string? expectedName = null, string? error = null)
        {
            var plan = Value(CosmicElementPlanner.Plan(Value(query), adapter.QueryProviderType,
                new[] { new CosmicQueryTarget(adapter.QueryProviderType, adapter.QueryCapabilities) }));
            var result = await manager.ExecuteElementQueryAsync(plan, new[] { adapter });

            if (error != null)
            {
                if (!result.IsError || result.ErrorCode != error || result.IsLoaded || result.Result != null)
                    throw new InvalidOperationException(title + ": expected " + error);
            }
            else if (result.IsError || !result.IsLoaded || result.Result?.Name != expectedName)
            {
                throw new InvalidOperationException(title + ": " + result.Message);
            }

            completed++;
            await output.WriteLineAsync($"\n{90 + completed}. {title}\n\n  {error ?? result.Result?.Name ?? "null (successful empty result)"}\n");
        }
    }

    private static T Value<T>(OASISResult<T> result)
    {
        if (result.IsError) throw new InvalidOperationException(result.ErrorCode + ": " + result.Message, result.Exception);
        return result.Result;
    }
}
