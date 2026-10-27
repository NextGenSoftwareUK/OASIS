using System.Linq.Expressions;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.Common;

namespace CosmicCqlDemo;

/// <summary>Demo acceptance for metadata, comparisons, membership, strings and ordering.</summary>
public static class V1OperatorQueries
{
    // Literal punctuation must never become SQL LIKE or regular-expression syntax.
    // Composed é, a supplementary Unicode character and ordinal case are deliberate.
    public const string OperatorText = "Café ☕ 🐉 %_[]'.*";

    public static async Task<int> RunAsync(HolonManager manager,
        ICosmicQueryStorageProvider adapter, CosmicQuery<Holon> visible, TextWriter output)
    {
        var completed = 0;
        var all = new[] { "B-owned", "C-public", "D-owned" };

        // Presence and null are independent: a present null exists; an absent
        // property is missing, and must not satisfy IsNull.
        await Check("Present null exists", h => CosmicQueryMetadata.Exists(h, "nullable"), all);
        await Check("Present null is null", h => CosmicQueryMetadata.IsNull(h, "nullable"), all);
        await Check("Missing property can be selected", h => !CosmicQueryMetadata.Exists(h, "missing"), all);
        await Check("Missing is not present null", h => CosmicQueryMetadata.IsNull(h, "missing"));
        await Check("Present non-null value", h => CosmicQueryMetadata.Exists(h, "stock")
            && !CosmicQueryMetadata.IsNull(h, "stock"), all);

        // Both operands are server-side paths. The comparison is not evaluated
        // by loading records into the client; exact Int64 values stay exact.
        await Check("Field comparison / true", h => CosmicQueryMetadata.Value<long>(h, "stock")
            < CosmicQueryMetadata.Value<long>(h, "precision"), all);
        await Check("Field comparison / false", h => CosmicQueryMetadata.Value<long>(h, "precision")
            < CosmicQueryMetadata.Value<long>(h, "stock"));
        await Check("Canonical-to-metadata Guid comparison", h => h.Id
            == CosmicQueryMetadata.Value<Guid>(h, "guid"), all);
        await Check("Canonical field equality", h => h.Version == h.Version, all);

        // Contains on a bounded closed collection means membership, not string
        // matching. Duplicate constants must not duplicate returned entities.
        var names = new[] { "B-owned", "D-owned" };
        await Check("Name membership", h => names.Contains(h.Name), "B-owned", "D-owned");

        var empty = Array.Empty<string>();
        await Check("Empty membership set", h => empty.Contains(h.Name));

        var duplicateNames = new[] { "B-owned", "B-owned" };
        await Check("Duplicate membership constants", h => duplicateNames.Contains(h.Name), "B-owned");

        var exactValues = new[] { 9007199254740992L, 9007199254740993L };
        await Check("Exact Int64 membership", h => exactValues.Contains(
            CosmicQueryMetadata.Value<long>(h, "precision")), all);

        // Explicit ordinal comparison gives the same case/literal contract on
        // every database. These cases exercise actual matching Unicode data,
        // empty patterns, combining characters and pattern metacharacters.
        foreach (var (pattern, matches) in new[]
        {
            ("Café", true), ("🐉", true), ("%_", true), ("[]'.*", true),
            ("", true), ("café", false), ("Cafe\u0301", false)
        })
        {
            await Check("Ordinal Contains / " + pattern,
                h => CosmicQueryMetadata.Value<string>(h, "operatorText")
                    .Contains(pattern, StringComparison.Ordinal), matches ? all : Array.Empty<string>());
        }

        foreach (var (pattern, matches) in new[]
        {
            ("Café", true), ("", true), ("café", false), ("%", false), ("Cafe\u0301", false)
        })
        {
            await Check("Ordinal StartsWith / " + pattern,
                h => CosmicQueryMetadata.Value<string>(h, "operatorText")
                    .StartsWith(pattern, StringComparison.Ordinal), matches ? all : Array.Empty<string>());
        }

        foreach (var (pattern, matches) in new[]
        {
            ("[]'.*", true), ("", true), (".*", true), (".*$", false), (".+", false)
        })
        {
            await Check("Ordinal EndsWith / " + pattern,
                h => CosmicQueryMetadata.Value<string>(h, "operatorText")
                    .EndsWith(pattern, StringComparison.Ordinal), matches ? all : Array.Empty<string>());
        }

        // All visible roots share stock=7, so the secondary ordering actually
        // determines the result. Unique first keys would not prove ThenBy.
        await Show("Tied primary key / ThenBy", Value(visible
            .OrderBy(h => CosmicQueryMetadata.Value<long>(h, "stock"))
            .ThenBy(h => h.Name)), all);

        await Show("Tied primary key / ThenByDescending", Value(visible
            .OrderBy(h => CosmicQueryMetadata.Value<long>(h, "stock"))
            .ThenByDescending(h => h.Name)), "D-owned", "C-public", "B-owned");

        return completed;

        Task Check(string title, Expression<Func<Holon, bool>> predicate, params string[] expected)
            => Show(title, Value(visible.Where(predicate).OrderBy(h => h.Name)), expected);

        async Task Show(string title, CosmicQuery<Holon> query, params string[] expected)
        {
            var plan = Value(CosmicQueryPlanner.Plan(query, adapter.QueryProviderType,
                new[] { new CosmicQueryTarget(adapter.QueryProviderType, adapter.QueryCapabilities) }));
            var execution = Value(await manager.ExecuteQueryAsync(plan, new[] { adapter }, maximumResults: 10));
            var actual = execution.Providers.Single().Rows.Select(h => h.Name).ToArray();

            if (!actual.SequenceEqual(expected))
                throw new InvalidOperationException(title + ": expected [" + string.Join(", ", expected)
                    + "], got [" + string.Join(", ", actual) + "] on " + adapter.QueryProviderType);

            completed++;
            await output.WriteLineAsync($"\n{58 + completed}. {title}\n\n  {string.Join(", ", actual)}\n");
        }
    }

    private static T Value<T>(OASISResult<T> result)
    {
        if (result.IsError)
            throw new InvalidOperationException(result.ErrorCode + ": " + result.Message, result.Exception);

        return result.Result;
    }
}
