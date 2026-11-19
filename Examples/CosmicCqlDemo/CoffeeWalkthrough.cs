using System.Text.Json;
using NextGenSoftware.OASIS.API.Core.CustomAttrbiutes;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.Common;

namespace CosmicCqlDemo;

// A normal typed holon: these annotated properties are persisted by COSMIC ORM.
// CQL expressions can refer to them directly, as shown in examples 1-3 below.
public sealed class Coffee : Holon
{
    [CustomOASISProperty]
    public string Category { get; set; } = "";

    [CustomOASISProperty]
    public int Rating { get; set; }

    [CustomOASISProperty]
    public int PriceInPence { get; set; }

    [CustomOASISProperty]
    public int Stock { get; set; }

    [CustomOASISProperty]
    public int Reserved { get; set; }
}

public sealed record DemoRow(string Name, int Rating, int PriceInPence, int Stock);
public sealed record DemoStep(string Name, string Query, IReadOnlyList<DemoRow> Rows);
public sealed record DemoReport(string Status, string Provider, int SeededRows, IReadOnlyList<DemoStep> Steps,
    bool NoOpSkipped, string ConflictCode, JsonElement CommittedDelta, int FinalVersion);

// The walkthrough owns no database or provider-specific query implementation.
// Hosts supply their already-authorized manager and native adapter instances.
public static class CoffeeWalkthrough
{
    public static async Task<DemoReport> RunAsync(HolonManager manager, HolonManager otherManager,
        ICosmicQueryStorageProvider adapter, ICosmicQueryStorageProvider otherAdapter, TextWriter output,
        CancellationToken cancellationToken = default, Action<Guid, IReadOnlyList<Guid>>? seeding = null)
    {
        // SETUP - select the provider scope and create five synthetic holons.
        // Named scope targets this provider. The host supplies the authorized adapters;
        // constructing a scope does not grant access to additional providers.
        var actor = Guid.NewGuid();
        var provider = adapter.QueryProviderType;
        var scope = Value(CosmicQueryScope.Named(provider));
        var steps = new List<DemoStep>();

        Coffee Row(string name,
            string category,
            int rating,
            int price,
            int stock,
            int reserved,
            string city) => new()
            {
                Id = Guid.NewGuid(),
                Name = name,
                Category = category,
                Rating = rating,
                PriceInPence = price,
                Stock = stock,
                Reserved = reserved,
                MetaData = new() { { "location", new Dictionary<string, object> { { "city", city } } } }
            };

        Coffee[] seed =
        {
            Row("Espresso",    "coffee",  9, 250, 20, 2, "London"),
            Row("Flat White",  "coffee",  8, 350,  8, 1, "London"),
            Row("Latte",       "coffee",  8, 400,  0, 0, "London"),
            Row("Green Tea",   "tea",     7, 200, 10, 0, "London"),
            Row("Iced Coffee", "coffee", 10, 450, 10, 0, "Paris")
        };

        seeding?.Invoke(actor, seed.Select(row => row.Id).ToArray());

        // Preassigned IDs need an explicit create-only expectation: null means that
        // no provider revision may exist yet. Each save has its own operation ID.
        foreach (var row in seed)
        {
            var saved = await manager.SaveHolonAsync(row,
                actor,
                new CosmicWriteOptions(Guid.NewGuid(),
                new Dictionary<Guid, Guid?> { { row.Id, null } }),
                saveChildren: false,
                providerType: provider);
            Success(saved);
            Check(saved.IsSaved, "Every seed must be committed.");
        }

        await output.WriteLineAsync($"Saved {seed.Length} synthetic holons through HolonManager / Hyperdrive V2 ({provider}).");

        // EXECUTION PIPELINE - build an expression, plan against native capabilities,
        // then execute through HolonManager. Building an expression performs no I/O.
        CosmicQuery<Coffee> Query() => CosmicQuery<Coffee>.Create(scope);

        async Task<IReadOnlyList<Coffee>> Read(CosmicQuery<Coffee> query,
            HolonManager owner,
            ICosmicQueryStorageProvider target)
        {
            var plan = Value(CosmicQueryPlanner.Plan(query,
                target.QueryProviderType,
                new[] { new CosmicQueryTarget(target.QueryProviderType, target.QueryCapabilities) }));
            var read = Value(await owner.ExecuteQueryAsync(plan,
                new[] { target },
                maximumResults: 20,
                cancellationToken: cancellationToken));
            Check(read.Providers.Count == 1 && read.Providers[0].Provider == provider,
                "Results must preserve selected provider provenance.");
            return read.Providers[0].Rows;
        }

        // Demo-only presentation and assertions. This verifies expected results;
        // filtering, sorting and paging have already happened in the provider.
        async Task<IReadOnlyList<Coffee>> Show(string name,
            string source,
            CosmicQuery<Coffee> query,
            params string[] expected)
        {
            var rows = await Read(query, manager, adapter);
            Check(rows.Select(r => r.Name).SequenceEqual(expected), name + " returned unexpected rows/order.");
            Check(rows.All(r => !HolonChangeTracking.HasChanges(r)),
                "Hydrated typed query results must be clean.");
            steps.Add(new(name,
                source,
                rows.Select(r => new DemoRow(r.Name,
                r.Rating,
                r.PriceInPence,
                r.Stock)).ToArray()));
            await output.WriteLineAsync("\n" + name + "\n  " + source);
            foreach (var row in rows)
                await output.WriteLineAsync($"  {row.Name,-14} rating={row.Rating}, price={row.PriceInPence}p, stock={row.Stock}");
            return rows;
        }

        // EXAMPLE 1 - typed filters, field-to-field comparison and ranked results.
        // Stock > Reserved compares two persisted properties in the native query.
        // Expected: Iced Coffee, Espresso, Flat White.
        var ranked = Value(Query()
            .Where(c => c.Category == "coffee" && c.Stock > c.Reserved)
            .OrderByDescending(c => c.Rating)
            .ThenBy(c => c.PriceInPence)
            .Take(3));

        var rankedRows = await Show("1. Typed properties + field comparison + ordering",
            "Where(c => c.Category == \"coffee\" && c.Stock > c.Reserved).OrderByDescending(c => c.Rating).ThenBy(c => c.PriceInPence).Take(3)",
            ranked,
            "Iced Coffee",
            "Espresso",
            "Flat White");

        // EXAMPLE 2 - collection membership and a nested metadata path.
        // Value<string>(c, "location", "city") addresses metadata location.city.
        // Capture plain values such as categories rather than invoking app callbacks.
        // Expected: Espresso, Flat White, Green Tea.
        var categories = new[] { "coffee", "tea" };
        var london = Value(Query()
            .Where(c =>
                categories.Contains(c.Category) &&
                c.Rating >= 7 &&
                c.Stock > c.Reserved &&
                CosmicQueryMetadata.Value<string>(c, "location", "city") == "London")
            .OrderByDescending(c => c.Rating));

        await Show("2. Nested metadata + membership",
            "Where(c => categories.Contains(c.Category) && c.Rating >= 7 && c.Stock > c.Reserved && Metadata.Value<string>(c, \"location\", \"city\") == \"London\").OrderByDescending(c => c.Rating)",
            london,
            "Espresso",
            "Flat White",
            "Green Tea");

        // EXAMPLE 3 - sort before paging; Skip/Take run in the native provider.
        // Include sold-out coffee here so the second page contains Flat White, Latte.
        var page = Value(Query()
            .Where(c => c.Category == "coffee")
            .OrderBy(c => c.PriceInPence)
            .Skip(1)
            .Take(2));

        await Show("3. Native paging",
            "Where(c => c.Category == \"coffee\").OrderBy(c => c.PriceInPence).Skip(1).Take(2)",
            page,
            "Flat White",
            "Latte");

        // CONCURRENCY SETUP - two independent clients read the same saved revision.
        // The second instance becomes stale after the first client's later update.
        var winner = rankedRows.Single(r => r.Name == "Espresso");
        var id = winner.Id;
        var sameId = Value(Query().Where(c => c.Id == id));
        var stale = (await Read(sameId, otherManager, otherAdapter)).Single();

        var originalVersion = winner.Version;
        var originalRevision = CosmicConcurrency.GetProviderRevisionId(winner, provider);

        // EXAMPLE 4 - saving a clean query result is a no-op.
        // Loading establishes the accepted tracking baseline; no edits means no write.
        var noOp = await manager.SaveHolonAsync(winner, actor, saveChildren: false, providerType: provider);

        Success(noOp);

        Check(!noOp.IsSaved && winner.Version == originalVersion, "No-op save must not write a new revision.");

        await output.WriteLineAsync("\n4. No-op Save(): no write, revision unchanged.");

        // EXAMPLE 5 - edit ordinary properties, then save with an operation identity.
        // The accepted provider revision is used for the conditional write.
        winner.Rating = 10;
        winner.Stock--;
        var operation = Guid.NewGuid();

        var update = await manager.SaveHolonAsync(winner,
            actor,
            new CosmicWriteOptions(operation),
            saveChildren: false,
            providerType: provider);

        Success(update);

        Check(
            update.IsSaved &&
            winner.Version == originalVersion + 1 &&
            !HolonChangeTracking.HasChanges(winner),
            "Write-back must publish a clean acknowledged revision.");

        Check(CosmicConcurrency.GetProviderRevisionId(winner, provider) != originalRevision,
            "Conditional save must advance the provider revision.");

        Check(update.MetaData != null && update.MetaData.ContainsKey("COSMIC.CommittedDelta"),
            "Save must return its committed delta summary.");

        // The acknowledged save returns its committed delta in result metadata.
        // Check the operation ID so the delta is tied to this particular save.
        using var delta = JsonDocument.Parse(update.MetaData!["COSMIC.CommittedDelta"]);

        Check(delta.RootElement.GetProperty("operationId").GetString() == operation.ToString("D"),
            "Delta must belong to the acknowledged operation.");

        await output.WriteLineAsync($"5. Save(): Espresso rating=10, stock={winner.Stock}, version={winner.Version}; committed delta returned.");

        // EXAMPLE 6 - a stale client's conditional write must fail explicitly.
        // Local edits remain dirty; the accepted winner stays unchanged in storage.
        stale.Rating = 1;

        var conflict = await otherManager.SaveHolonAsync(stale,
            actor,
            new CosmicWriteOptions(Guid.NewGuid()),
            saveChildren: false,
            providerType: provider);

        Check(
            conflict.IsError &&
            conflict.ErrorCode == CosmicConcurrencyException.Code &&
            !conflict.IsSaved &&
            HolonChangeTracking.HasChanges(stale),
            "A stale client must receive an explicit conflict without losing local edits.");

        // Re-query through the second client to confirm what was actually committed.
        var fresh = (await Read(sameId, otherManager, otherAdapter)).Single();

        Check(fresh.Rating == 10 && fresh.Stock == 19 && fresh.Version == winner.Version,
            "Fresh query must observe the winner, not the rejected stale edit.");

        await output.WriteLineAsync("6. Second client Save(): " + conflict.ErrorCode + "; winner preserved, stale edits remain local.");

        // EXAMPLE 7 - capability validation happens before native execution.
        // This adapter does not support exact decimal metadata filtering; the planner
        // reports that limitation instead of evaluating the predicate in memory.
        var unsupported = Value(Query().Where(c => CosmicQueryMetadata.Value<decimal>(c,
            "unsupportedDecimal") > 1m));
        var rejected = CosmicQueryPlanner.Plan(unsupported,
            provider,
            new[] { new CosmicQueryTarget(provider,
            adapter.QueryCapabilities) });

        Check(rejected.IsError && rejected.ErrorCode == "CQL_CAPABILITY_UNSUPPORTED",
            "Unsupported scalar semantics must reject at planning.");

        await output.WriteLineAsync("7. Unsupported decimal filter: rejected at planning before native query execution.");

        return new("passed",
            provider.ToString(),
            seed.Length,
            steps,
            true,
            conflict.ErrorCode!,
            delta.RootElement.Clone(),
            winner.Version);
    }

    // Demo assertions unwrap OASISResult and stop the walkthrough on a real error.
    // Application callers can inspect IsError, ErrorCode and Message themselves.
    private static T Value<T>(OASISResult<T> result)
    {
        Success(result);
        if (result.Result is null)
            throw new InvalidOperationException("Expected a result payload.");
        return result.Result;
    }

    private static void Success<T>(OASISResult<T> result)
    {
        if (result.IsError)
            throw new InvalidOperationException(result.ErrorCode + ": " + result.Message, result.Exception);
    }

    private static void Check(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }
}
