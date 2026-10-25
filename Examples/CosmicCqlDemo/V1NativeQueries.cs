using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.Common;

namespace CosmicCqlDemo;

/// <summary>
/// LINQ-style CQL over an already-authorized native provider. The host calls the
/// SAME method for MongoDB, SQLite and Neo4j; no provider-specific query code.
/// For untrusted callers, use WEB4 v1: its server enforces identity and access.
/// </summary>
public static class V1NativeQueries
{
    public static async Task<int> RunAsync(
        HolonManager manager,
        ICosmicQueryStorageProvider adapter,
        Guid actor,
        string fixtureTag,
        TextWriter output)
    {
        var completedQueries = 0;
        var provider = adapter.QueryProviderType;
        var scope = Value(CosmicQueryScope.Named(provider));

        // Local SDK queries run inside the host's trust boundary. We supply the
        // access policy explicitly here; HTTP v1 always adds it on the server.
        var access = Value(CosmicQueryReadAccess.OwnerOrPublic(actor));

        var visible = Value(CosmicQuery<Holon>.Create(scope)
            .WithReadAccess(access)
            .Where(holon => CosmicQueryMetadata.Value<string>(holon, "fixture") == fixtureTag));

        await output.WriteLineAsync($"\n===== C# / LINQ-style CQL: {provider} =====\n");

        // 1. Compose filters and a deterministic native page.
        // Expected: C-public, because visibility is applied before Skip/Take.
        var page = Value(visible
            .Where(holon => CosmicQueryMetadata.Value<long>(holon, "stock") >= 7)
            .OrderBy(holon => holon.Name)
            .ThenBy(holon => holon.Id)
            .Skip(1)
            .Take(1));

        await ShowAsync("1. Native filter / sort / page", page, "C-public");

        // 2. Nested paths, OR and explicit ordinal string operations.
        // A-private remains excluded by the earlier visible query predicate.
        var nested = Value(visible
            .Where(holon => CosmicQueryMetadata.Value<string>(holon, "location", "city") == "London")
            .Where(holon => holon.Name == "A-private" || holon.Name.StartsWith("D-", StringComparison.Ordinal))
            .OrderBy(holon => holon.Name)
            .Take(10));

        await ShowAsync("2. Nested metadata / protected OR", nested, "D-owned");

        // 3. long literals retain their exact value beyond double precision.
        var precise = Value(visible
            .Where(holon => CosmicQueryMetadata.Value<long>(holon, "precision") == 9007199254740993L)
            .OrderBy(holon => holon.Name)
            .Take(10));

        await ShowAsync("3. Exact Int64 metadata", precise, "B-owned", "C-public", "D-owned");

        // 4. The SAME policy applies to every selected child before native limits.
        // A plain root Where(...) cannot express this child-access guarantee.
        var shape = Value(CosmicQueryGraphOptions.Create(
            recursive: true,
            maxChildDepth: 2,
            maximumNodes: 8));

        var graphQuery = Value(visible
            .Where(holon => holon.Name == "C-public")
            .WithChildren(shape));

        var graphPlan = Value(CosmicQueryPlanner.Plan(
            graphQuery,
            provider,
            new[] { new CosmicQueryTarget(provider, adapter.QueryCapabilities) }));

        var graph = Value(await manager.ExecuteQueryAsync(graphPlan, new[] { adapter }));
        var root = graph.Providers.Single().Rows.Single();
        var children = root.Children.Select(child => child.Name).ToArray();

        if (!children.SequenceEqual(new[] { "public child", "owned child" }))
            throw new InvalidOperationException("Child visibility or persisted membership ordering changed.");

        completedQueries++;

        await output.WriteLineAsync("4. Access-bound graph: public root / selected children");
        await output.WriteLineAsync("\n  " + root.Name + "\n    " + string.Join("\n    ", children) + "\n");

        // This is a selected view. For a root-only edit use saveChildren: false;
        // AcceptChanges() cannot authorize replacement of hidden children.
        // For public JSON use WEB4's graph DTOs, which omit private store state.

        // 5. Match parents by a VISIBLE direct child without loading a graph.
        // Each WhereAnyChild is a separate native EXISTS condition. Two clauses
        // may match different siblings; the same actor policy protects both.
        var parentsWithChildren = Value(visible
            .WhereAnyChild(child => child.Name == "public child")
            .WhereAnyChild(child => child.Name == "owned child")
            .OrderBy(parent => parent.Name)
            .Take(1));

        await ShowAsync("5. Native child existence / independent siblings", parentsWithChildren, "C-public");

        // 6. Search a bounded descendant scope without loading child output.
        // A private intermediate prunes its entire branch, even a public leaf.
        // MaximumNodes counts all visible canonical descendants, not just matches.
        var traversal = Value(CosmicQueryTraversalOptions.Create(
            maximumDepth: 2,
            maximumNodes: 2));

        var parentsWithDescendants = Value(visible
            .WhereAnyDescendant(child => child.Name == "owned child", traversal)
            .OrderBy(parent => parent.Name)
            .Take(1));

        await ShowAsync("6. Bounded native descendant existence", parentsWithDescendants, "C-public");

        // 7. A terminal builds an immutable intent; it does not run a row query
        // or hydrate entities. Omitted Take counts the complete visible source.
        await ShowScalarAsync("7. Native LongCount / complete visible source",
            Value(visible.LongCount()), 3L);

        // 8. Window algebra belongs to the source: Skip/Take happen before Count.
        await ShowScalarAsync("8. Count after an explicit source window",
            Value(visible.Skip(1).Take(1).Count()), 1);

        // 9. Access is applied before Any, too. An inaccessible match is false.
        await ShowScalarAsync("9. Any cannot reveal a private root",
            Value(visible.Where(holon => holon.Name == "A-private").Any()), false);

        // 10. Select only the values this screen needs. Source visibility and
        // Skip/Take run natively before selection; no holon or child is hydrated.
        // These detached DTO values carry no entity save/change-tracking state.
        if (adapter is not ICosmicQueryProjectionStorageProvider projectionAdapter)
            throw new InvalidOperationException("This provider has no selected-column contract.");

        var selected = Value(visible
            .OrderBy(holon => holon.Name)
            .ThenBy(holon => holon.Id)
            .Skip(1)
            .Take(1)
            .Select(holon => new
            {
                Label = holon.Name,
                Precision = CosmicQueryMetadata.Value<long>(holon, "precision"),
                Optional = CosmicQueryMetadata.Value<long?>(holon, "nullable"),
                City = CosmicQueryMetadata.Value<string>(holon, "location", "city")
            }));

        var selectedPlan = Value(CosmicProjectionPlanner.Plan(
            selected,
            provider,
            new[]
            {
                new CosmicProjectionTarget(
                    provider,
                    adapter.QueryCapabilities,
                    projectionAdapter.ProjectionCapabilities)
            }));

        var projected = Value(await manager.ExecuteProjectionQueryAsync(
            selectedPlan,
            new[] { adapter },
            maximumResults: 10));

        var summary = projected.Providers.Single().Rows.Single();

        if (summary.Label != "C-public" || summary.Precision != 9007199254740993L
            || summary.Optional != null || summary.City != "London")
            throw new InvalidOperationException("Native selected-column values changed.");

        completedQueries++;

        await output.WriteLineAsync("10. Native Select / detached typed values");
        await output.WriteLineAsync($"\n  {summary.Label}\n  Exact Int64: {summary.Precision}\n  Optional: null\n  City: {summary.City}\n");

        // 11. Min reads every visible value natively. Hidden roots deliberately
        // contain stronger extrema, so this also demonstrates actor isolation.
        await ShowScalarAsync("11. Native Min / exact signed Int64",
            Value(visible.Min(h => CosmicQueryMetadata.Value<long>(h, "extremum"))),
            -9007199254740991L);

        // 12. Max uses the same typed terminal and capability/read owner.
        // No entity is loaded and the manager does not calculate a client maximum.
        await ShowScalarAsync("12. Native Max / exact signed Int64",
            Value(visible.Max(h => CosmicQueryMetadata.Value<long>(h, "extremum"))),
            -9007199254740989L);

        // 13. Sum returns an exact Int64 total. The selected inputs exceed
        // JavaScript's safe-integer range, so the HTTP form returns text.
        await ShowScalarAsync("13. Native Sum / exact signed total",
            Value(visible.Sum(h => CosmicQueryMetadata.Value<long>(h, "extremum"))),
            -27021597764222970L);

        // 14. Apply the ordered window before Average. Integer input produces
        // Decimal, preserving the half-unit beyond binary64 integer precision.
        var firstTwo = Value(visible.OrderBy(h => h.Name).Take(2));

        await ShowScalarAsync("14. Native Average / exact Decimal fraction",
            Value(firstTwo.Average(h => CosmicQueryMetadata.Value<long>(h, "extremum"))),
            -9007199254740990.5m);

        // 15. Reduce a selected source window into native groups. The key and
        // aggregates are detached values; no holon is hydrated or tracked.
        var grouped = Value(firstTwo
            .GroupBy(h => CosmicQueryMetadata.Value<long>(h, "stock"))
            .Select(group => new
            {
                Stock = group.Key,
                Items = group.LongCount(),

                Total = group.Sum(h => CosmicQueryMetadata.Value<long>(h, "extremum")),
                Mean = group.Average(h => CosmicQueryMetadata.Value<long>(h, "extremum"))
            }));

        if (adapter is not ICosmicQueryGroupedStorageProvider groupedAdapter)
            throw new InvalidOperationException("This provider has no grouped contract.");

        var groupedPlan = Value(CosmicGroupedPlanner.Plan(
            grouped,
            provider,
            new[]
            {
                new CosmicGroupedTarget(provider,
                    adapter.QueryCapabilities, groupedAdapter.GroupedCapabilities)
            }));

        var groupedExecution = Value(await manager.ExecuteGroupedQueryAsync(groupedPlan, new[] { adapter }));
        var groupSummary = groupedExecution.Providers.Single().Rows.Single();

        if (groupSummary.Stock != 7 || groupSummary.Items != 2
            || groupSummary.Total != -18014398509481981L
            || groupSummary.Mean != -9007199254740990.5m)
            throw new InvalidOperationException("Grouped source-window reduction returned unexpected values.");

        completedQueries++;
        await output.WriteLineAsync("15. Native GroupBy / exact multi-aggregate reduction");
        await output.WriteLineAsync($"\n  Stock={groupSummary.Stock}, Items={groupSummary.Items}\n  Total={groupSummary.Total}\n  Mean={groupSummary.Mean}\n");

        // 16. Sort selected aggregate outputs after grouping. The sums differ
        // by one near the binary64 integer boundary; retain exact Int64 values.
        // ThenBy supplies the next user sort; complete group identity breaks ties.
        var orderedGroups = Value(visible
            .GroupBy(h => h.Name)
            .Select(group => new
            {
                Name = group.Key,
                Total = group.Sum(h => CosmicQueryMetadata.Value<long>(h, "extremum"))
            })
            .OrderByDescending(group => group.Total)
            .ThenBy(group => group.Name));

        var orderedPlan = Value(CosmicGroupedPlanner.Plan(
            orderedGroups,
            provider,
            new[] { new CosmicGroupedTarget(provider,
                adapter.QueryCapabilities, groupedAdapter.GroupedCapabilities) }));

        var orderedExecution = Value(await manager.ExecuteGroupedQueryAsync(orderedPlan, new[] { adapter }));
        var orderedRows = orderedExecution.Providers.Single().Rows;

        if (!orderedRows.Select(row => row.Name).SequenceEqual(new[] { "D-owned", "C-public", "B-owned" })
            || !orderedRows.Select(row => row.Total).SequenceEqual(new[] { -9007199254740989L, -9007199254740990L, -9007199254740991L }))
            throw new InvalidOperationException("Grouped aggregate ordering lost exact values or result order.");

        completedQueries++;
        await output.WriteLineAsync("16. Native grouped ordering / exact adjacent Int64 sums\n");

        foreach (var row in orderedRows)
            await output.WriteLineAsync($"  {row.Name}: {row.Total}");

        await output.WriteLineAsync();

        // 17. Page the ordered aggregate results, independently of source rows.
        // Complete bounded proof remains validated, including the skipped groups.
        var groupPage = Value(orderedGroups.Skip(1).Take(1));

        var pagePlan = Value(CosmicGroupedPlanner.Plan(
            groupPage,
            provider,
            new[] { new CosmicGroupedTarget(provider,
                adapter.QueryCapabilities, groupedAdapter.GroupedCapabilities) }));

        var pageExecution = Value(await manager.ExecuteGroupedQueryAsync(pagePlan, new[] { adapter }));
        var pageRow = pageExecution.Providers.Single().Rows.Single();

        if (pageRow.Name != "C-public" || pageRow.Total != -9007199254740990L)
            throw new InvalidOperationException("Grouped result page returned the wrong exact aggregate.");

        completedQueries++;
        await output.WriteLineAsync("17. Native grouped page / Skip(1).Take(1)\n");
        await output.WriteLineAsync($"  {pageRow.Name}: {pageRow.Total}\n");

        // 18. Filter the reduced totals, then page the matching groups.
        // B-owned is excluded by its aggregate value. Skip(1) skips D-owned,
        // so C-public wins. Filtering source rows would have different semantics.
        // Keep the Int64 literal exact, including its sign and adjacent units.
        var filteredGroups = Value(orderedGroups
            .Where(group => group.Total > -9007199254740991L)
            .Skip(1)
            .Take(1));

        var filteredPlan = Value(CosmicGroupedPlanner.Plan(
            filteredGroups,
            provider,
            new[] { new CosmicGroupedTarget(provider,
                adapter.QueryCapabilities, groupedAdapter.GroupedCapabilities) }));

        var filteredExecution = Value(await manager.ExecuteGroupedQueryAsync(filteredPlan, new[] { adapter }));
        var filteredRow = filteredExecution.Providers.Single().Rows.Single();

        if (filteredRow.Name != "C-public" || filteredRow.Total != -9007199254740990L)
            throw new InvalidOperationException("Grouped filtering or matching-group paging lost exact semantics.");

        completedQueries++;
        await output.WriteLineAsync("18. Filter grouped totals / Where().Skip(1).Take(1)\n");
        await output.WriteLineAsync($"  {filteredRow.Name}: {filteredRow.Total}\n");

        // 19. Filter and order the normalized Decimal Average, then take a page.
        // The two source rows were selected before grouping (example15).
        // At this magnitude, a binary64 value loses the half-unit fraction.
        // Every provider compares the same rounded value that it publishes.
        var averagePage = Value(grouped
            .Where(row => row.Mean >= -9007199254740990.5m
                && row.Mean < -9007199254740990m)
            .OrderBy(row => row.Mean)
            .Take(1));

        var averagePlan = Value(CosmicGroupedPlanner.Plan(
            averagePage,
            provider,
            new[] { new CosmicGroupedTarget(provider,
                adapter.QueryCapabilities, groupedAdapter.GroupedCapabilities) }));

        var averageExecution = Value(await manager.ExecuteGroupedQueryAsync(averagePlan, new[] { adapter }));
        var averageRow = averageExecution.Providers.Single().Rows.Single();

        if (averageRow.Stock != 7 || averageRow.Items != 2 || averageRow.Mean != -9007199254740990.5m)
            throw new InvalidOperationException("Normalized Average filtering/order/page lost exact Decimal semantics.");

        completedQueries++;
        await output.WriteLineAsync("19. Filter and order Decimal means / Select().Where().OrderBy()\n");
        await output.WriteLineAsync($"  Stock={averageRow.Stock}, Items={averageRow.Items}, Mean={averageRow.Mean}\n");

        // 20. Declare UTC bounds outside the expression. Date methods are not
        // arbitrary executable query code. These two instants differ by100ns.
        var dateStart = new DateTime(2026, 10, 24, 0, 0, 0, DateTimeKind.Utc).AddTicks(2);
        var dateEnd = dateStart.AddTicks(2);

        var dated = Value(visible
            .Where(row => CosmicQueryMetadata.Value<DateTime>(row, "observedAt") >= dateStart
                && CosmicQueryMetadata.Value<DateTime>(row, "observedAt") < dateEnd)
            .OrderBy(row => CosmicQueryMetadata.Value<DateTime>(row, "observedAt"))
            .Take(2)
            .Select(row => new
            {
                row.Name,
                ObservedAt = CosmicQueryMetadata.Value<DateTime>(row, "observedAt")
            }));

        var datePlan = Value(CosmicProjectionPlanner.Plan(dated, provider,
            new[] { new CosmicProjectionTarget(provider, adapter.QueryCapabilities,
                projectionAdapter.ProjectionCapabilities) }));

        var dateExecution = Value(await manager.ExecuteProjectionQueryAsync(datePlan, new[] { adapter }));
        var dateRows = dateExecution.Providers.Single().Rows;

        if (!dateRows.Select(row => row.Name).SequenceEqual(new[] { "B-owned", "C-public" })
            || !dateRows.Select(row => row.ObservedAt).SequenceEqual(new[] { dateStart, dateStart.AddTicks(1) })
            || dateRows.Any(row => row.ObservedAt.Kind != DateTimeKind.Utc))
            throw new InvalidOperationException("UTC date selection lost tick precision, kind or visibility.");

        completedQueries++;
        await output.WriteLineAsync("20. UTC date range / exact detached timestamps\n");
        foreach (var row in dateRows)
            await output.WriteLineAsync($"  {row.Name}: {row.ObservedAt:O}");
        await output.WriteLineAsync();

        // 21. The same UTC literals can filter selected group keys. All source
        // groups still participate in proof validation before result publication.
        var dateGroups = Value(visible
            .GroupBy(row => CosmicQueryMetadata.Value<DateTime>(row, "observedAt"))
            .Select(group => new { ObservedAt = group.Key, Items = group.LongCount() })
            .Where(row => row.ObservedAt >= dateStart && row.ObservedAt < dateEnd)
            .OrderBy(row => row.ObservedAt)
            .Take(2));

        var dateGroupPlan = Value(CosmicGroupedPlanner.Plan(dateGroups, provider,
            new[] { new CosmicGroupedTarget(provider, adapter.QueryCapabilities, groupedAdapter.GroupedCapabilities) }));
        var dateGroupExecution = Value(await manager.ExecuteGroupedQueryAsync(dateGroupPlan, new[] { adapter }));
        var dateGroupRows = dateGroupExecution.Providers.Single().Rows;

        if (!dateGroupRows.Select(row => row.ObservedAt).SequenceEqual(new[] { dateStart, dateStart.AddTicks(1) })
            || dateGroupRows.Any(row => row.Items != 1 || row.ObservedAt.Kind != DateTimeKind.Utc))
            throw new InvalidOperationException("UTC grouped filters/order/page lost exact date identity.");

        completedQueries++;
        await output.WriteLineAsync("21. UTC grouped keys / selected date filters and ordering\n");
        foreach (var row in dateGroupRows)
            await output.WriteLineAsync($"  {row.ObservedAt:O}: {row.Items} item\n");


        // 22. UInt64 counters exceed both JavaScript's exact integer range and
        // signed Int64. Keep ulong literals and let the provider order the values.
        var counterStart = ulong.MaxValue - 3UL;
        var counterEnd = ulong.MaxValue - 1UL;

        var unsignedSelection = Value(visible
            .Where(row => CosmicQueryMetadata.Value<ulong>(row, "counter") >= counterStart
                && CosmicQueryMetadata.Value<ulong>(row, "counter") < counterEnd)
            .OrderBy(row => CosmicQueryMetadata.Value<ulong>(row, "counter"))
            .Select(row => new
            {
                row.Name,
                Counter = CosmicQueryMetadata.Value<ulong>(row, "counter")
            }));

        var unsignedPlan = Value(CosmicProjectionPlanner.Plan(unsignedSelection, provider,
            new[] { new CosmicProjectionTarget(provider, adapter.QueryCapabilities,
                projectionAdapter.ProjectionCapabilities) }));

        var unsignedResult = Value(await manager.ExecuteProjectionQueryAsync(unsignedPlan, new[] { adapter }));
        var unsignedRows = unsignedResult.Providers.Single().Rows;

        if (!unsignedRows.Select(row => row.Name).SequenceEqual(new[] { "C-public", "B-owned" })
            || !unsignedRows.Select(row => row.Counter).SequenceEqual(new[] { counterStart, counterStart + 1UL }))
            throw new InvalidOperationException("UInt64 source ordering lost precision or visibility.");

        completedQueries++;
        await output.WriteLineAsync("22. UInt64 source range / exact unsigned ordering\n");
        foreach (var row in unsignedRows)
            await output.WriteLineAsync($"  {row.Name}: {row.Counter}");
        await output.WriteLineAsync();

        // 23. Filter selected unsigned group keys before the result window.
        // Native reduction and complete proof validation still include all groups.
        var unsignedGroups = Value(visible
            .GroupBy(row => CosmicQueryMetadata.Value<ulong>(row, "counter"))
            .Select(group => new { Counter = group.Key, Items = group.LongCount() })
            .Where(row => row.Counter >= counterStart && row.Counter < counterEnd)
            .OrderBy(row => row.Counter)
            .Take(2));

        var unsignedGroupPlan = Value(CosmicGroupedPlanner.Plan(unsignedGroups, provider,
            new[] { new CosmicGroupedTarget(provider, adapter.QueryCapabilities, groupedAdapter.GroupedCapabilities) }));

        var unsignedGroupResult = Value(await manager.ExecuteGroupedQueryAsync(unsignedGroupPlan, new[] { adapter }));
        var unsignedGroupRows = unsignedGroupResult.Providers.Single().Rows;

        if (!unsignedGroupRows.Select(row => row.Counter).SequenceEqual(new[] { counterStart, counterStart + 1UL })
            || unsignedGroupRows.Any(row => row.Items != 1))
            throw new InvalidOperationException("UInt64 grouped result filtering/order lost exact key identity.");

        completedQueries++;
        await output.WriteLineAsync("23. UInt64 group keys / exact selected-result filters\n");
        foreach (var row in unsignedGroupRows)
            await output.WriteLineAsync($"  {row.Counter}: {row.Items} item\n");

        // 24. These prices differ by 10^-28. Use m literals so C# retains
        // every digit, then let the provider filter/order before projection.
        var priceStart = 1.0000000000000000000000000002m;
        var priceEnd = 1.0000000000000000000000000004m;

        var decimalSelection = Value(visible
            .Where(row => CosmicQueryMetadata.Value<decimal>(row, "price") >= priceStart
                && CosmicQueryMetadata.Value<decimal>(row, "price") < priceEnd)
            .OrderBy(row => CosmicQueryMetadata.Value<decimal>(row, "price"))
            .Select(row => new
            {
                row.Name,
                Price = CosmicQueryMetadata.Value<decimal>(row, "price")
            }));

        var decimalPlan = Value(CosmicProjectionPlanner.Plan(decimalSelection, provider,
            new[] { new CosmicProjectionTarget(provider, adapter.QueryCapabilities,
                projectionAdapter.ProjectionCapabilities) }));

        var decimalResult = Value(await manager.ExecuteProjectionQueryAsync(decimalPlan, new[] { adapter }));
        var decimalRows = decimalResult.Providers.Single().Rows;

        if (!decimalRows.Select(row => row.Name).SequenceEqual(new[] { "B-owned", "C-public" })
            || !decimalRows.Select(row => row.Price).SequenceEqual(new[] { priceStart, priceStart + 0.0000000000000000000000000001m }))
            throw new InvalidOperationException("Decimal source ordering lost precision or visibility.");

        completedQueries++;
        await output.WriteLineAsync("24. Decimal price range / exact price ordering\n");
        foreach (var row in decimalRows)
            await output.WriteLineAsync($"  {row.Name}: {row.Price}");
        await output.WriteLineAsync();

        // 25. Group by exact prices, then filter/order the selected Price alias.
        // Native reduction and complete proof validation still include all groups.
        var decimalGroups = Value(visible
            .GroupBy(row => CosmicQueryMetadata.Value<decimal>(row, "price"))
            .Select(group => new { Price = group.Key, Items = group.LongCount() })
            .Where(row => row.Price >= priceStart && row.Price < priceEnd)
            .OrderBy(row => row.Price)
            .Take(2));

        var decimalGroupPlan = Value(CosmicGroupedPlanner.Plan(decimalGroups, provider,
            new[] { new CosmicGroupedTarget(provider, adapter.QueryCapabilities, groupedAdapter.GroupedCapabilities) }));

        var decimalGroupResult = Value(await manager.ExecuteGroupedQueryAsync(decimalGroupPlan, new[] { adapter }));
        var decimalGroupRows = decimalGroupResult.Providers.Single().Rows;

        if (!decimalGroupRows.Select(row => row.Price).SequenceEqual(new[] { priceStart, priceStart + 0.0000000000000000000000000001m })
            || decimalGroupRows.Any(row => row.Items != 1))
            throw new InvalidOperationException("Decimal grouped result filtering/order lost exact key identity.");

        completedQueries++;
        await output.WriteLineAsync("25. Decimal group keys / exact selected-result filters\n");
        foreach (var row in decimalGroupRows)
            await output.WriteLineAsync($"  {row.Price}: {row.Items} item\n");

        // 26. Adjacent binary64 readings remain distinct. Use d literals for
        // Double; use Decimal instead when the domain requires exact prices.
        var readingStart = 1.0000000000000004d;
        var readingEnd = 1.0000000000000009d;

        var doubleSelection = Value(visible
            .Where(row => CosmicQueryMetadata.Value<double>(row, "reading") >= readingStart
                && CosmicQueryMetadata.Value<double>(row, "reading") < readingEnd)
            .OrderBy(row => CosmicQueryMetadata.Value<double>(row, "reading"))
            .Select(row => new
            {
                row.Name,
                Reading = CosmicQueryMetadata.Value<double>(row, "reading")
            }));

        var doublePlan = Value(CosmicProjectionPlanner.Plan(doubleSelection, provider,
            new[] { new CosmicProjectionTarget(provider, adapter.QueryCapabilities,
                projectionAdapter.ProjectionCapabilities) }));

        var doubleResult = Value(await manager.ExecuteProjectionQueryAsync(doublePlan, new[] { adapter }));
        var doubleRows = doubleResult.Providers.Single().Rows;

        if (!doubleRows.Select(row => row.Name).SequenceEqual(new[] { "B-owned", "C-public" })
            || !doubleRows.Select(row => row.Reading).SequenceEqual(new[] { readingStart, readingStart + 2.220446049250313e-16d }))
            throw new InvalidOperationException("Double source ordering lost precision or visibility.");

        completedQueries++;
        await output.WriteLineAsync("26. Double reading range / binary64 reading ordering\n");
        foreach (var row in doubleRows)
            await output.WriteLineAsync($"  {row.Name}: {row.Reading}");
        await output.WriteLineAsync();

        // 27. Group by binary64 readings, then filter/order the selected Reading alias.
        // Native reduction and complete proof validation still include all groups.
        var doubleGroups = Value(visible
            .GroupBy(row => CosmicQueryMetadata.Value<double>(row, "reading"))
            .Select(group => new { Reading = group.Key, Items = group.LongCount() })
            .Where(row => row.Reading >= readingStart && row.Reading < readingEnd)
            .OrderBy(row => row.Reading)
            .Take(2));

        var doubleGroupPlan = Value(CosmicGroupedPlanner.Plan(doubleGroups, provider,
            new[] { new CosmicGroupedTarget(provider, adapter.QueryCapabilities, groupedAdapter.GroupedCapabilities) }));

        var doubleGroupResult = Value(await manager.ExecuteGroupedQueryAsync(doubleGroupPlan, new[] { adapter }));
        var doubleGroupRows = doubleGroupResult.Providers.Single().Rows;

        if (!doubleGroupRows.Select(row => row.Reading).SequenceEqual(new[] { readingStart, readingStart + 2.220446049250313e-16d })
            || doubleGroupRows.Any(row => row.Items != 1))
            throw new InvalidOperationException("Double grouped result filtering/order lost exact key identity.");

        completedQueries++;
        await output.WriteLineAsync("27. Double group keys / typed selected-result filters\n");
        foreach (var row in doubleGroupRows)
            await output.WriteLineAsync($"  {row.Reading}: {row.Items} item\n");

        // 28. Adjacent binary32 readings remain distinct. Use f literals for
        // Single; use Decimal instead when the domain requires exact prices.
        var singleStart = 1.0000002f;
        var singleEnd = 1.0000005f;

        var singleSelection = Value(visible
            .Where(row => CosmicQueryMetadata.Value<float>(row, "singleReading") >= singleStart
                && CosmicQueryMetadata.Value<float>(row, "singleReading") < singleEnd)
            .OrderBy(row => CosmicQueryMetadata.Value<float>(row, "singleReading"))
            .Select(row => new
            {
                row.Name,
                Reading = CosmicQueryMetadata.Value<float>(row, "singleReading")
            }));

        var singlePlan = Value(CosmicProjectionPlanner.Plan(singleSelection, provider,
            new[] { new CosmicProjectionTarget(provider, adapter.QueryCapabilities,
                projectionAdapter.ProjectionCapabilities) }));

        var singleResult = Value(await manager.ExecuteProjectionQueryAsync(singlePlan, new[] { adapter }));
        var singleRows = singleResult.Providers.Single().Rows;

        if (!singleRows.Select(row => row.Name).SequenceEqual(new[] { "B-owned", "C-public" })
            || !singleRows.Select(row => row.Reading).SequenceEqual(new[] { singleStart, singleStart + 1.1920929e-7f }))
            throw new InvalidOperationException("Single source ordering lost precision or visibility.");

        completedQueries++;
        await output.WriteLineAsync("28. Single reading range / binary32 reading ordering\n");
        foreach (var row in singleRows)
            await output.WriteLineAsync($"  {row.Name}: {row.Reading}");
        await output.WriteLineAsync();

        // 29. Group by binary32 readings, then filter/order the selected Reading alias.
        // Native reduction and complete proof validation still include all groups.
        var singleGroups = Value(visible
            .GroupBy(row => CosmicQueryMetadata.Value<float>(row, "singleReading"))
            .Select(group => new { Reading = group.Key, Items = group.LongCount() })
            .Where(row => row.Reading >= singleStart && row.Reading < singleEnd)
            .OrderBy(row => row.Reading)
            .Take(2));

        var singleGroupPlan = Value(CosmicGroupedPlanner.Plan(singleGroups, provider,
            new[] { new CosmicGroupedTarget(provider, adapter.QueryCapabilities, groupedAdapter.GroupedCapabilities) }));

        var singleGroupResult = Value(await manager.ExecuteGroupedQueryAsync(singleGroupPlan, new[] { adapter }));
        var singleGroupRows = singleGroupResult.Providers.Single().Rows;

        if (!singleGroupRows.Select(row => row.Reading).SequenceEqual(new[] { singleStart, singleStart + 1.1920929e-7f })
            || singleGroupRows.Any(row => row.Items != 1))
            throw new InvalidOperationException("Single grouped result filtering/order lost exact key identity.");

        completedQueries++;
        await output.WriteLineAsync("29. Single group keys / typed selected-result filters\n");
        foreach (var row in singleGroupRows)
            await output.WriteLineAsync($"  {row.Reading}: {row.Items} item\n");

        // 30. A left join preserves every visible left row. The independently
        // filtered right input contains only C-public, so B-owned/D-owned have
        // no partner. Nullable output represents absence without losing Int64.
        var joinLeft = Value(visible.OrderBy(holon => holon.Name).ThenBy(holon => holon.Id));
        var joinRight = Value(visible
            .Where(holon => holon.Name == "C-public")
            .OrderBy(holon => holon.Name)
            .ThenBy(holon => holon.Id));

        var leftSelection = Value(joinLeft
            .LeftJoin(joinRight, left => left.Name, right => right.Name)
            .Select((left, right) => new JoinDemoRow
            {
                Name = left.Name,
                Precision = (long?)CosmicQueryMetadata.Value<long>(right, "precision"),
                Present = right != null
            }));

        await ShowJoinAsync("30. Left join / unmatched rows and exact values", leftSelection,
            "B-owned", "C-public", "D-owned");

        // 31. Changing only the join kind removes unmatched left rows. Both
        // inputs retain their actor policy, provider scope and source windows.
        var innerSelection = Value(joinLeft
            .Join(joinRight, left => left.Name, right => right.Name)
            .Select((left, right) => new JoinDemoRow
            {
                Name = left.Name,
                Precision = (long?)CosmicQueryMetadata.Value<long>(right, "precision"),
                Present = right != null
            }));

        await ShowJoinAsync("31. Inner join / matching pairs only", innerSelection, "C-public");

        // 32. The SOURCE page contains C-public and D-owned. Select captures
        // detached scalar cells; this Where runs AFTER that source window.
        // Every source cell is validated even if a later stage removes its row.
        var selectedWindow = Value(visible
            .OrderBy(holon => holon.Name)
            .Skip(1)
            .Take(2)
            .Select(holon => new CompositionDemoRow
            {
                Name = holon.Name,
                Precision = CosmicQueryMetadata.Value<long>(holon, "precision")
            }));

        var filteredWindow = Value(selectedWindow
            .Where(row => row.Name != "C-public")
            .OrderByDescending(row => row.Precision)
            .ThenBy(row => row.Name)
            .Take(1));

        await ShowCompositionAsync("32. Composition / filter the selected source page", filteredWindow, "D-owned");

        // 33. Moving Take BEFORE Where changes the answer. C-public is selected
        // first, then removed. The native provider must preserve this order;
        // moving the predicate into the source would incorrectly return D-owned.
        var pageBeforeFilter = Value(selectedWindow
            .Take(1)
            .Where(row => row.Name != "C-public"));

        await ShowCompositionAsync("33. Composition / page before filter returns no rows", pageBeforeFilter);

        // 34. Int64 selected comparisons retain all digits. This second result
        // ordering starts a new stage, then Skip/Take selects C-public.
        var exactSelected = Value(selectedWindow
            .Where(row => row.Precision == 9007199254740993L)
            .OrderByDescending(row => row.Name)
            .Skip(1)
            .Take(1));

        await ShowCompositionAsync("34. Composition / exact values and result paging", exactSelected, "C-public");

        // 35. Sum keeps the full UInt64 domain. Select one source row so the
        // total is representable; summing several near-maximum counters must fail.
        var unsignedFirst = Value(visible.OrderBy(holon => holon.Name).Take(1));

        await ShowScalarAsync("35. UInt64 Sum / exact unsigned total",
            Value(unsignedFirst.Sum(holon =>
                CosmicQueryMetadata.Value<ulong>(holon, "counter"))),
            ulong.MaxValue - 2UL);

        // 36. Average may use a numerator larger than UInt64. Its Decimal
        // result retains the half-unit that a JavaScript Number would lose.
        var unsignedWindow = Value(visible.OrderBy(holon => holon.Name).Take(2));

        await ShowScalarAsync("36. UInt64 Average / exact Decimal fraction",
            Value(unsignedWindow.Average(holon =>
                CosmicQueryMetadata.Value<ulong>(holon, "counter"))),
            18446744073709551612.5m);

        // 37. Source Take selects participation before grouping. Filter and
        // order the published Decimal mean, then select a group result page.
        var unsignedMeans = Value(unsignedWindow
            .GroupBy(holon => CosmicQueryMetadata.Value<long>(holon, "stock"))
            .Select(group => new
            {
                Mean = group.Average(holon =>
                    CosmicQueryMetadata.Value<ulong>(holon, "counter"))
            })
            .Where(group => group.Mean == 18446744073709551612.5m)
            .OrderByDescending(group => group.Mean)
            .Take(1));

        var unsignedMeanPlan = Value(CosmicGroupedPlanner.Plan(
            unsignedMeans,
            provider,
            new[] { new CosmicGroupedTarget(provider,
                adapter.QueryCapabilities, groupedAdapter.GroupedCapabilities) }));

        var unsignedMeanExecution = Value(await manager.ExecuteGroupedQueryAsync(
            unsignedMeanPlan, new[] { adapter }));
        var unsignedMeanRow = unsignedMeanExecution.Providers.Single().Rows.Single();

        if (unsignedMeanRow.Mean != 18446744073709551612.5m)
            throw new InvalidOperationException("Unsigned grouped mean lost its exact fraction.");

        completedQueries++;
        await output.WriteLineAsync("37. UInt64 groups / filter and order exact means\n");
        await output.WriteLineAsync($"  Mean: {unsignedMeanRow.Mean}\n");

        return completedQueries;

        async Task ShowCompositionAsync(string title,
            CosmicProjectionQuery<Holon, CompositionDemoRow> query, params string[] expected)
        {
            if (adapter is not ICosmicQueryProjectionCompositionStorageProvider native)
                throw new InvalidOperationException("This provider has no native composition contract.");

            var plan = Value(CosmicProjectionCompositionPlanner.Plan(
                query,
                provider,
                new[] { new CosmicProjectionCompositionTarget(provider, native.QueryCapabilities,
                    native.ProjectionCapabilities, native.CompositionCapabilities) }));

            var execution = Value(await manager.ExecuteProjectionCompositionQueryAsync(plan, new[] { adapter }));
            var rows = execution.Providers.Single().Rows;

            if (!rows.Select(row => row.Name).SequenceEqual(expected)
                || rows.Any(row => row.Precision != 9007199254740993L))
                throw new InvalidOperationException(title + " lost stage order, exact cells or visibility.");

            completedQueries++;
            await output.WriteLineAsync(title + "\n");
            if (rows.Count == 0) await output.WriteLineAsync("  (no rows)\n");
            foreach (var row in rows)
                await output.WriteLineAsync($"  {row.Name}: precision={row.Precision}\n");
        }

        async Task ShowJoinAsync(
            string title,
            CosmicJoinSelection<Holon, Holon, string, JoinDemoRow> selection,
            params string[] expectedNames)
        {
            // Join support is an explicit adapter capability. Planning and
            // execution use the same generic Core/HolonManager on every store.
            if (adapter is not ICosmicQueryJoinStorageProvider joinAdapter)
                throw new InvalidOperationException("This provider has no native join contract.");

            var plan = Value(CosmicJoinPlanner.Plan(
                selection,
                provider,
                new[] { new CosmicJoinTarget(provider, adapter.QueryCapabilities, joinAdapter.JoinCapabilities) }));

            var execution = Value(await manager.ExecuteJoinQueryAsync(plan, new[] { adapter }));
            var rows = execution.Providers.Single().Rows;

            if (!rows.Select(row => row.Value.Name).SequenceEqual(expectedNames)
                || rows.Any(row => row.HasRight != row.Value.Present
                    || row.Value.Precision != (row.HasRight ? 9007199254740993L : (long?)null)))
                throw new InvalidOperationException(title + " lost join identity, presence or exact Int64 values.");

            completedQueries++;
            await output.WriteLineAsync(title + "\n");

            foreach (var row in rows)
                await output.WriteLineAsync($"  {row.Value.Name}: hasRight={row.HasRight}, precision={row.Value.Precision?.ToString() ?? "(absent)"}\n");
        }

        async Task ShowScalarAsync<TResult>(
            string title,
            CosmicScalarQuery<Holon, TResult> query,
            TResult expected)
        {
            // Scalar capability is additive; row support alone does not grant it.
            if (adapter is not ICosmicQueryScalarStorageProvider scalarAdapter)
                throw new InvalidOperationException("This provider has no scalar contract.");

            var plan = Value(CosmicScalarPlanner.Plan(
                query,
                provider,
                new[] { new CosmicScalarTarget(provider,
                    adapter.QueryCapabilities, scalarAdapter.ScalarCapabilities) }));

            var execution = Value(await manager.ExecuteScalarQueryAsync(plan, new[] { adapter }));
            var result = execution.Providers.Single().Value;

            if (!EqualityComparer<TResult>.Default.Equals(result, expected))
                throw new InvalidOperationException(title + " returned an unexpected scalar.");

            completedQueries++;

            await output.WriteLineAsync(title);
            await output.WriteLineAsync($"\n  {typeof(TResult).Name}: {result}\n");
        }

        async Task ShowAsync(string title, CosmicQuery<Holon> query, params string[] expected)
        {
            // Planning checks this concrete provider's actual capabilities
            // before execution. Unsupported expressions fail; no client scan.
            var plan = Value(CosmicQueryPlanner.Plan(
                query,
                provider,
                new[] { new CosmicQueryTarget(provider, adapter.QueryCapabilities) }));

            var execution = Value(await manager.ExecuteQueryAsync(
                plan,
                new[] { adapter },
                maximumResults: 10));

            var names = execution.Providers.Single().Rows.Select(holon => holon.Name).ToArray();

            if (!names.SequenceEqual(expected))
                throw new InvalidOperationException(title + " returned unexpected native rows.");

            completedQueries++;

            await output.WriteLineAsync(title);
            await output.WriteLineAsync("\n  " + string.Join("\n  ", names) + "\n");
        }
    }

    // These DTOs are constructed only after the complete native proof passes.
    public sealed class CompositionDemoRow
    {
        public string Name { get; set; } = string.Empty;
        public long Precision { get; set; }
    }

    // A detached application DTO, constructed only after all native proofs pass.
    public sealed class JoinDemoRow
    {
        public string Name { get; set; } = string.Empty;
        public long? Precision { get; set; }
        public bool Present { get; set; }
    }

    // Application code should handle OASISResult errors at its own boundary.
    // The demo fails loudly so a broken query cannot look like an empty result.
    private static T Value<T>(OASISResult<T> result)
    {
        if (result.IsError)
            throw new InvalidOperationException(result.Message, result.Exception);

        return result.Result;
    }
}
