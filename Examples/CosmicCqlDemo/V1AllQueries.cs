using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.Common;

namespace CosmicCqlDemo;

/// <summary>Native universal scalar predicates over the complete composed source.</summary>
public static class V1AllQueries
{
    public static async Task<int> RunAsync(HolonManager manager, ICosmicQueryStorageProvider adapter,
        CosmicQuery<Holon> visible, TextWriter output)
    {
        var completed = 0;
        var scalar = adapter as ICosmicQueryScalarStorageProvider
            ?? throw new InvalidOperationException("The native provider must declare scalar capability.");
        var ordered = Value(visible.OrderBy(row => row.Name));

        await Show("All / every visible stock qualifies", visible.All(row => CosmicQueryMetadata.Value<long>(row, "stock") >= 7), true);
        await Show("All / no visible stock qualifies", visible.All(row => CosmicQueryMetadata.Value<long>(row, "stock") > 7), false);
        await Show("All / one counterexample", visible.All(row => row.Name != "C-public"), false);

        // Evaluate AFTER the source window. Filtering counterexamples before
        // Take/Skip would change the sequence and give incorrect answers here.
        await Show("Take(2).All / D is outside the window", ordered.Take(2).All(row => row.Name != "D-owned"), true);
        await Show("Skip(1).Take(1).All / C qualifies", ordered.Skip(1).Take(1).All(row => row.Name == "C-public"), true);
        await Show("Skip(1).Take(1).All / C is not B", ordered.Skip(1).Take(1).All(row => row.Name == "B-owned"), false);

        // Universal quantification is true over an empty sequence, including
        // when the predicate is false. Native errors must still stay errors.
        await Show("Empty native source / All(false)", visible.Where(row => row.Name == "no such menu item").All(row => false), true);
        await Show("Take(0).All(false)", ordered.Take(0).All(row => false), true);
        await Show("All(true)", visible.All(row => true), true);
        await Show("All(false)", visible.All(row => false), false);

        // Inaccessible roots do not participate in the universal predicate.
        await Show("Hidden root is excluded", visible.All(row => row.Name != "A-private"), true);

        await Show("Present null exists", visible.All(row => CosmicQueryMetadata.Exists(row, "nullable")), true);
        await Show("Present null is null", visible.All(row => CosmicQueryMetadata.IsNull(row, "nullable")), true);
        await Show("Missing is not null", visible.All(row => CosmicQueryMetadata.IsNull(row, "missing")), false);
        await Show("Missing property absent", visible.All(row => !CosmicQueryMetadata.Exists(row, "missing")), true);
        await Show("Exact field comparison", visible.All(row => CosmicQueryMetadata.Value<long>(row, "stock")
            < CosmicQueryMetadata.Value<long>(row, "precision")), true);

        var names = new[] { "B-owned", "C-public", "D-owned" };
        await Show("Bounded membership", visible.All(row => names.Contains(row.Name)), true);
        await Show("Ordinal Unicode matching", visible.All(row => CosmicQueryMetadata.Value<string>(row, "operatorText")
            .Contains("🐉", StringComparison.Ordinal)), true);
        await Show("Combined Boolean predicate", visible.All(row => CosmicQueryMetadata.Value<bool>(row, "enabled")
            && names.Contains(row.Name)), true);

        // Existing graph predicates may define the SOURCE before All. Traversal
        // inside the All predicate itself remains a separate red capability.
        var parents = Value(visible.WhereAnyChild(child => child.Name == "public child").OrderBy(row => row.Name).Take(1));
        await Show("Child-filtered source", parents.All(row => row.Name == "C-public"), true);
        var descendants = Value(visible.WhereAnyDescendant(child => child.Name == "owned child",
            Value(CosmicQueryTraversalOptions.Create(maximumDepth: 2, maximumNodes: 2))).OrderBy(row => row.Name).Take(1));
        await Show("Descendant-filtered source", descendants.All(row => row.Name == "C-public"), true);
        var absentParents = Value(visible.WhereAnyChild(child => child.Name == "no such child"));
        await Show("Empty child-filtered source", absentParents.All(row => false), true);

        return completed;

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
