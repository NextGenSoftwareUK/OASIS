using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.Common;

namespace CosmicCqlDemo;

/// <summary>Native universal scalar and direct-child predicates after the composed source window.</summary>
public static class V1AllQueries
{
    public static async Task<int> RunAsync(HolonManager manager, ICosmicQueryStorageProvider adapter,
        CosmicQuery<Holon> visible, TextWriter output, string fixtureTag = null)
    {
        var completed = 0;
        var scalar = adapter as ICosmicQueryScalarStorageProvider
            ?? throw new InvalidOperationException("The native provider must declare scalar capability.");
        var ordered = Value(visible.OrderBy(row => row.Name));

        await Show(
            "All / every visible stock qualifies",
            visible.All(row => CosmicQueryMetadata.Value<long>(row, "stock") >= 7),
            true);

        await Show(
            "All / no visible stock qualifies",
            visible.All(row => CosmicQueryMetadata.Value<long>(row, "stock") > 7),
            false);

        await Show(
            "All / one counterexample",
            visible.All(row => row.Name != "C-public"),
            false);


        // Evaluate AFTER the source window. Filtering counterexamples before
        // Take/Skip would change the sequence and give incorrect answers here.
        await Show(
            "Take(2).All / D is outside the window",
            ordered.Take(2).All(row => row.Name != "D-owned"),
            true);

        await Show(
            "Skip(1).Take(1).All / C qualifies",
            ordered.Skip(1).Take(1).All(row => row.Name == "C-public"),
            true);

        await Show(
            "Skip(1).Take(1).All / C is not B",
            ordered.Skip(1).Take(1).All(row => row.Name == "B-owned"),
            false);


        // Universal quantification is true over an empty sequence, including
        // when the predicate is false. Native errors must still stay errors.
        await Show(
            "Empty native source / All(false)",
            visible.Where(row => row.Name == "no such menu item").All(row => false),
            true);

        await Show(
            "Take(0).All(false)",
            ordered.Take(0).All(row => false),
            true);

        await Show(
            "All(true)",
            visible.All(row => true),
            true);

        await Show(
            "All(false)",
            visible.All(row => false),
            false);


        // Inaccessible roots do not participate in the universal predicate.
        await Show(
            "Hidden root is excluded",
            visible.All(row => row.Name != "A-private"),
            true);


        await Show(
            "Present null exists",
            visible.All(row => CosmicQueryMetadata.Exists(row, "nullable")),
            true);

        await Show(
            "Present null is null",
            visible.All(row => CosmicQueryMetadata.IsNull(row, "nullable")),
            true);

        await Show(
            "Missing is not null",
            visible.All(row => CosmicQueryMetadata.IsNull(row, "missing")),
            false);

        await Show(
            "Missing property absent",
            visible.All(row => !CosmicQueryMetadata.Exists(row, "missing")),
            true);

        await Show("Exact field comparison", visible.All(row => CosmicQueryMetadata.Value<long>(row, "stock")
            < CosmicQueryMetadata.Value<long>(row, "precision")), true);

        var names = new[] { "B-owned", "C-public", "D-owned" };
        await Show(
            "Bounded membership",
            visible.All(row => names.Contains(row.Name)),
            true);

        await Show("Ordinal Unicode matching", visible.All(row => CosmicQueryMetadata.Value<string>(row, "operatorText")
            .Contains("🐉", StringComparison.Ordinal)), true);
        await Show("Combined Boolean predicate", visible.All(row => CosmicQueryMetadata.Value<bool>(row, "enabled")
            && names.Contains(row.Name)), true);

        // Graph predicates may define the SOURCE before All. Direct-child
        // predicates inside All are demonstrated below; recursive traversal
        // inside the terminal remains a separate red capability.
        var parents = Value(visible.WhereAnyChild(child => child.Name == "public child").OrderBy(row => row.Name).Take(1));
        await Show(
            "Child-filtered source",
            parents.All(row => row.Name == "C-public"),
            true);

        var descendants = Value(visible.WhereAnyDescendant(child => child.Name == "owned child",
            Value(CosmicQueryTraversalOptions.Create(maximumDepth: 2, maximumNodes: 2))).OrderBy(row => row.Name).Take(1));
        await Show(
            "Descendant-filtered source",
            descendants.All(row => row.Name == "C-public"),
            true);

        var absentParents = Value(visible.WhereAnyChild(child => child.Name == "no such child"));
        await Show(
            "Empty child-filtered source",
            absentParents.All(row => false),
            true);


        // Child predicates execute after the source page and use the SAME
        // visibility/membership policy as source relationship filters.
        await Show(
            "Every visible root has a public child",
            visible.All(row => row.Children.Any(child => child.Name == "public child")),
            false);

        var publicParent = Value(visible.Where(row => row.Name == "C-public"));
        await Show(
            "C has a public child",
            publicParent.All(row => row.Children.Any(child => child.Name == "public child")),
            true);

        await Show(
            "C has an owned child",
            publicParent.All(row => row.Children.Any(child => child.Name == "owned child")),
            true);

        await Show(
            "C's private child is invisible",
            publicParent.All(row => row.Children.Any(child => child.Name == "private child")),
            false);

        await Show(
            "Grandchildren are not direct children",
            publicParent.All(row => row.Children.Any(child => child.Name == "public behind private")),
            false);

        await Show(
            "Take(1) keeps B before testing children",
            ordered.Take(1).All(row => row.Children.Any(child => child.Name == "public child")),
            false);

        await Show(
            "Skip(1) selects C before testing children",
            ordered.Skip(1).Take(1).All(row => row.Children.Any(child => child.Name == "public child")),
            true);

        await Show(
            "Empty child terminal is true",
            ordered.Take(0).All(row => row.Children.Any(child => child.Name == "no child")),
            true);

        await Show(
            "B has no public child",
            ordered.Take(1).All(row => !row.Children.Any(child => child.Name == "public child")),
            true);

        await Show(
            "Scalar OR child test",
            ordered.Take(2).All(row => row.Name == "B-owned" || row.Children.Any(child => child.Name == "public child")),
            true);

        await Show(
            "Scalar AND child test",
            publicParent.All(row => row.Name == "C-public" && row.Children.Any(child => child.Name == "owned child")),
            true);

        await Show("Two independent child predicates", publicParent.All(row => row.Children.Any(child => child.Name == "public child")
            && row.Children.Any(child => child.Name == "owned child")), true);
        await Show("Dollar-prefixed string stays literal", publicParent.All(row => row.Name != "$row.Name"
            && row.Children.Any(child => child.Name != "$parent")), true);
        await Show("Missing metadata plus child predicate", publicParent.All(row => !CosmicQueryMetadata.Exists(row, "missing")
            && row.Children.Any(child => child.Name == "public child")), true);
        await Show(
            "Source traversal plus terminal child",
            descendants.All(row => row.Children.Any(child => child.Name == "public child")),
            true);

        await Show(
            "Empty graph-filtered source plus terminal child",
            absentParents.All(row => row.Children.Any(child => child.Name == "public child")),
            true);


        // Nested child navigation is still a bounded native expression, not a
        // recursive enumeration of loaded Children collections. The trusted
        // host-only query below omits access filtering explicitly to prove the
        // positive path through the fixture's otherwise hidden parent.
        if (fixtureTag != null)
        {
            var trustedParent = Value(CosmicQuery<Holon>.Create(visible.Scope)
                .Where(row => CosmicQueryMetadata.Value<string>(row, "fixture") == fixtureTag
                    && row.Name == "C-public"));

            await Show(
                "Nested positive path without visibility filtering",
                trustedParent.All(row => row.Children.Any(child => child.Children.Any(
                    grandchild => grandchild.Name == "public behind private"))),
                true);

            await Show(
                "Nested path respects the source actor's hidden parent",
                publicParent.All(row => row.Children.Any(child => child.Children.Any(
                    grandchild => grandchild.Name == "public behind private"))),
                false);

            await Show(
                "Nested predicate with child scalar guard",
                trustedParent.All(row => row.Children.Any(child => child.Name == "private child"
                    && child.Children.Any(grandchild => grandchild.Name == "public behind private"))),
                true);

            await Show(
                "Nested missing child remains false",
                trustedParent.All(row => row.Children.Any(child => child.Children.Any(
                    grandchild => grandchild.Name == "does not exist"))),
                false);

            await Show(
                "Negated nested predicate remains native",
                publicParent.All(row => !row.Children.Any(child => child.Children.Any(
                    grandchild => grandchild.Name == "public behind private"))),
                true);

            await Show(
                "Nested OR combines child scalar and navigation predicates",
                trustedParent.All(row => row.Children.Any(child => child.Name == "missing"
                    || child.Children.Any(grandchild => grandchild.Name == "public behind private"))),
                true);

            await Show(
                "Nested literal is not a native field reference",
                trustedParent.All(row => row.Children.Any(child => child.Children.Any(
                    grandchild => grandchild.Name != "$row.Name"))),
                true);

            await Show(
                "Three child levels do not invent a fourth node",
                trustedParent.All(row => row.Children.Any(child => child.Children.Any(grandchild =>
                    grandchild.Children.Any(greatGrandchild => greatGrandchild.Name == "public behind private")))),
                false);
            // Descendant markers require explicit immutable traversal bounds.
            // Errors are observable even when another Boolean branch decides
            // the match; terminal traversal applies only to the source page.
            var visibleBounds = Value(CosmicQueryTraversalOptions.Create(2, 2));
            var shallowBounds = Value(CosmicQueryTraversalOptions.Create(1, 4));
            var trustedBounds = Value(CosmicQueryTraversalOptions.Create(2, 4));
            var tightBounds = Value(CosmicQueryTraversalOptions.Create(2, 1));
            var threeNodes = Value(CosmicQueryTraversalOptions.Create(2, 3));

            await Show("Descendant / visible owned child",
                publicParent.All(row => CosmicQueryRelations.AnyDescendant(row, child => child.Name == "owned child", visibleBounds)), true);
            await Show("Descendant / hidden intermediate blocks grandchild",
                publicParent.All(row => CosmicQueryRelations.AnyDescendant(row, child => child.Name == "public behind private", visibleBounds)), false);
            await Show("Descendant / depth1 excludes grandchild",
                trustedParent.All(row => CosmicQueryRelations.AnyDescendant(row, child => child.Name == "public behind private", shallowBounds)), false);
            await Show("Descendant / depth2 includes grandchild",
                trustedParent.All(row => CosmicQueryRelations.AnyDescendant(row, child => child.Name == "public behind private", trustedBounds)), true);
            await Show("Descendant / root never matches itself",
                trustedParent.All(row => CosmicQueryRelations.AnyDescendant(row, child => child.Name == "C-public", trustedBounds)), false);
            await Show("Descendant / empty terminal is true",
                ordered.Take(0).All(row => CosmicQueryRelations.AnyDescendant(row, child => false, tightBounds)), true);
            await Show("Descendant / page keeps B before traversing",
                ordered.Take(1).All(row => CosmicQueryRelations.AnyDescendant(row, child => child.Name == "public child", tightBounds)), false);
            await Show("Descendant / negated missing match",
                publicParent.All(row => !CosmicQueryRelations.AnyDescendant(row, child => child.Name == "no such child", visibleBounds)), true);
            await Show("Descendant / scalar OR traversal",
                ordered.Take(2).All(row => row.Name == "B-owned" || CosmicQueryRelations.AnyDescendant(row, child => child.Name == "owned child", visibleBounds)), true);
            await Show("Descendant / scalar AND traversal",
                publicParent.All(row => row.Name == "C-public" && CosmicQueryRelations.AnyDescendant(row, child => child.Name == "owned child", visibleBounds)), true);
            await Show("Descendant / two independent predicates",
                publicParent.All(row => CosmicQueryRelations.AnyDescendant(row, child => child.Name == "owned child", visibleBounds)
                    && CosmicQueryRelations.AnyDescendant(row, child => child.Name == "public child", visibleBounds)), true);
            await Show("Descendant / source and terminal traversal scopes",
                descendants.All(row => CosmicQueryRelations.AnyDescendant(row, child => child.Name == "owned child", visibleBounds)), true);

            await ShowError("Descendant / an early match cannot hide node overflow",
                publicParent.All(row => CosmicQueryRelations.AnyDescendant(row, child => child.Name == "public child", tightBounds)), "CQL_QUERY_LIMIT");
            await ShowError("Descendant / false AND cannot hide node overflow",
                publicParent.All(row => row.Name == "not C" && CosmicQueryRelations.AnyDescendant(row, child => child.Name == "public child", tightBounds)), "CQL_QUERY_LIMIT");
            await ShowError("Descendant / true OR cannot hide node overflow",
                publicParent.All(row => row.Name == "C-public" || CosmicQueryRelations.AnyDescendant(row, child => child.Name == "public child", tightBounds)), "CQL_QUERY_LIMIT");
            await ShowError("Descendant / exact cumulative node budget",
                trustedParent.All(row => CosmicQueryRelations.AnyDescendant(row, child => child.Name == "public behind private", threeNodes)), "CQL_QUERY_LIMIT");

            var invalidSource = Value(visible.WhereAnyDescendant(child => child.Name == "no such child", tightBounds).Take(0));
            await ShowError("Descendant / source error survives an empty source window",
                invalidSource.All(row => CosmicQueryRelations.AnyDescendant(row, child => child.Name == "owned child", visibleBounds)), "CQL_QUERY_LIMIT");
            await Show("Descendant / empty filtered source",
                absentParents.All(row => CosmicQueryRelations.AnyDescendant(row, child => false, visibleBounds)), true);
            await Show("Descendant / false OR true traversal",
                publicParent.All(row => row.Name == "not C" || CosmicQueryRelations.AnyDescendant(row, child => child.Name == "owned child", visibleBounds)), true);
            await Show("Descendant / true AND false traversal",
                publicParent.All(row => row.Name == "C-public" && CosmicQueryRelations.AnyDescendant(row, child => child.Name == "no such child", visibleBounds)), false);
            // Descendant scope belongs to EACH visible direct child. The
            // parent Any result and protected errors are reduced separately.
            await Show("Child -> descendant / trusted positive path",
                trustedParent.All(row => row.Children.Any(child => CosmicQueryRelations.AnyDescendant(
                    child, descendant => descendant.Name == "public behind private", visibleBounds))), true);
            await Show("Child -> descendant / hidden parent is excluded",
                publicParent.All(row => row.Children.Any(child => CosmicQueryRelations.AnyDescendant(
                    child, descendant => descendant.Name == "public behind private", visibleBounds))), false);
            await Show("Child -> descendant / scalar AND in child scope",
                trustedParent.All(row => row.Children.Any(child => child.Name == "private child" && CosmicQueryRelations.AnyDescendant(
                    child, descendant => descendant.Name == "public behind private", visibleBounds))), true);
            await Show("Child -> descendant / scalar OR in child scope",
                trustedParent.All(row => row.Children.Any(child => child.Name == "owned child" || CosmicQueryRelations.AnyDescendant(
                    child, descendant => descendant.Name == "public behind private", visibleBounds))), true);
            await Show("Child -> descendant / negation within child scope",
                trustedParent.All(row => row.Children.Any(child => !CosmicQueryRelations.AnyDescendant(
                    child, descendant => descendant.Name == "public behind private", visibleBounds))), true);
            await Show("Child -> descendant / negated parent result",
                publicParent.All(row => !row.Children.Any(child => CosmicQueryRelations.AnyDescendant(
                    child, descendant => descendant.Name == "public behind private", visibleBounds))), true);
            await Show("Child -> descendant / source Take selects B first",
                ordered.Take(1).All(row => row.Children.Any(child => CosmicQueryRelations.AnyDescendant(
                    child, descendant => true, visibleBounds))), false);
            await Show("Child -> descendant / source Skip selects secured C",
                ordered.Skip(1).Take(1).All(row => row.Children.Any(child => CosmicQueryRelations.AnyDescendant(
                    child, descendant => true, visibleBounds))), false);
            await Show("Child -> descendant / empty source window",
                ordered.Take(0).All(row => row.Children.Any(child => CosmicQueryRelations.AnyDescendant(
                    child, descendant => true, visibleBounds))), true);
            await Show("Child -> descendant / empty filtered source",
                absentParents.All(row => row.Children.Any(child => CosmicQueryRelations.AnyDescendant(
                    child, descendant => true, visibleBounds))), true);
            await Show("Child -> descendant / false child scalar remains false",
                trustedParent.All(row => row.Children.Any(child => child.Name == "missing" && CosmicQueryRelations.AnyDescendant(
                    child, descendant => true, visibleBounds))), false);
            await Show("Child -> descendant / independent root traversal",
                trustedParent.All(row => row.Children.Any(child => CosmicQueryRelations.AnyDescendant(
                    child, descendant => descendant.Name == "public behind private", visibleBounds))
                    && CosmicQueryRelations.AnyDescendant(row, descendant => descendant.Name == "owned child", trustedBounds)), true);
        }

        return completed;

        async Task ShowError(string title, OASISResult<CosmicScalarQuery<Holon, bool>> query, string expectedCode)
        {
            var plan = Value(CosmicScalarPlanner.Plan(Value(query), adapter.QueryProviderType,
                new[] { new CosmicScalarTarget(adapter.QueryProviderType, adapter.QueryCapabilities, scalar.ScalarCapabilities) }));
            var result = await manager.ExecuteScalarQueryAsync(plan, new[] { adapter });
            if (!result.IsError || result.ErrorCode != expectedCode)
                throw new InvalidOperationException(title + ": expected " + expectedCode + ", got " + result.ErrorCode + ": " + result.Message);
            completed++;
            await output.WriteLineAsync($"\n{135 + completed}. {title}\n\n  {result.ErrorCode}\n");
        }

        async Task Show(string title, OASISResult<CosmicScalarQuery<Holon, bool>> query, bool expected)
        {
            var plan = Value(CosmicScalarPlanner.Plan(Value(query), adapter.QueryProviderType,
                new[] { new CosmicScalarTarget(adapter.QueryProviderType, adapter.QueryCapabilities, scalar.ScalarCapabilities) }));
            var result = Value(await manager.ExecuteScalarQueryAsync(plan, new[] { adapter }));
            var actual = result.Providers.Single().Value;
            if (actual != expected) throw new InvalidOperationException(title + ": expected " + expected + ", got " + actual);
            completed++;
            await output.WriteLineAsync($"\n{135 + completed}. {title}\n\n  {actual}\n");
        }
    }

    private static T Value<T>(OASISResult<T> result)
    {
        if (result.IsError) throw new InvalidOperationException(result.ErrorCode + ": " + result.Message, result.Exception);
        return result.Result;
    }
}
