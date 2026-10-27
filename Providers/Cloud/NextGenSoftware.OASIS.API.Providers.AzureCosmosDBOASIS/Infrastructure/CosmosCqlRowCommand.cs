using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Azure.Cosmos;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS.Infrastructure;

/// <summary>Pure re-admission and bounded native row command construction.</summary>
internal static class CosmosCqlRowCommand
{
    // Shared physical admission profile for the public native row executor.
    internal static CosmicQueryCapabilities Capabilities
    {
        get
        {
            var equality = new[] { CosmicQueryComparisonOperator.Equal, CosmicQueryComparisonOperator.NotEqual };
            var fields = new List<CosmicQueryFieldCapability>();
            foreach (var name in new[] { "Name", "Description" })
                fields.Add(new(name, typeof(string), equality));
            foreach (var name in new[] { "IsActive", "IsPublic" })
                fields.Add(new(name, typeof(bool), equality));
            foreach (var name in new[] { "Id", "ParentHolonId", "VersionId", "PreviousVersionId", "CreatedByAvatarId", "ModifiedByAvatarId" })
                fields.Add(new(name, typeof(Guid), equality, Array.Empty<CosmicQueryStringOperator>(), supportsOrdering: name == "Id"));
            fields.Add(new("Version", typeof(int), Enum.GetValues<CosmicQueryComparisonOperator>(),
                Array.Empty<CosmicQueryStringOperator>(), supportsOrdering: true));
            return new CosmicQueryCapabilities(CosmicQueryFeatures.And | CosmicQueryFeatures.Or |
                CosmicQueryFeatures.Not | CosmicQueryFeatures.NullLiteral | CosmicQueryFeatures.BooleanConstant |
                CosmicQueryFeatures.Ordering | CosmicQueryFeatures.Paging | CosmicQueryFeatures.Metadata |
                CosmicQueryFeatures.MetadataExists | CosmicQueryFeatures.MetadataNull, fields.ToArray(),
                new[] { typeof(string), typeof(bool), typeof(bool?) }
                    .Select(type => new CosmicQueryMetadataCapability(type, equality)).ToArray());
        }
    }

    internal static QueryDefinition Compile<T>(CosmicQueryPlan<T> plan, int maximumResults) where T : IHolon
        => Compile(plan, maximumResults, _ => "*");

    internal static QueryDefinition Compile<T>(CosmicQueryPlan<T> plan, int maximumResults,
        Func<CosmosCqlPredicateSql, string> selection) where T : IHolon
    {
        ArgumentNullException.ThrowIfNull(plan);
        if (maximumResults < 1 || maximumResults > 10000)
            throw new ArgumentOutOfRangeException(nameof(maximumResults));
        if (plan.Targets.Count != 1 || plan.Targets[0].Provider != ProviderType.AzureCosmosDBOASIS)
            throw new NotSupportedException("Cosmos CQL requires exactly its own admitted native target.");
        // A plan can originate with a more permissive adapter profile. Re-admit
        // against this physical executor's contract before constructing SQL.
        var admitted = Capabilities.AdmitPlan(plan);
        if (admitted.IsError) throw new NotSupportedException(admitted.Message);
        var query = plan.Query;
        var translator = new CosmosCqlPredicateSql();
        var predicate = query.Predicates.Count == 0 ? "true" :
            string.Join(" AND ", query.Predicates.Select(translator.Translate));
        var ordering = query.Orderings.Select(order => "c[\"" + order.Path.Segments.Single() +
            "\"] " + (order.Descending ? "DESC" : "ASC")).ToList();
        if (!query.Orderings.Any(order => order.Path.Segments.Count == 1 && order.Path.Segments[0] == "Id"))
            ordering.Add("c[\"Id\"] ASC");
        // One extra row exposes overflow instead of silently truncating a source
        // whose caller did not request a small enough window.
        var take = Math.Min(query.Limit ?? maximumResults + 1, maximumResults + 1);
        var sql = "SELECT " + selection(translator) + " FROM c WHERE " + predicate + " ORDER BY " + string.Join(", ", ordering) +
            " OFFSET @cqlOffset LIMIT @cqlLimit";
        return translator.BuildCommand(sql).WithParameter("@cqlOffset", query.Offset).WithParameter("@cqlLimit", take);
    }
}
