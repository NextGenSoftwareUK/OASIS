using System.Linq;
using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS;

public partial class Neo4jOASIS
{
    private sealed class NeoCqlSource
    {
        internal NeoCqlTranslator Translator = null!;
        internal string Source = null!;
        internal string EligibleSource = null!;
        internal string DescendantPredicate = null!;
        internal string Cycle = null!;
        internal string TraversalLimit = null!;
    }

    // Any relationship below a Boolean node may introduce traversal state.
    // Evaluate its complete scope before applying the compound business filter.
    private static bool HasSourceRelationship(CosmicQueryPredicate predicate)
        => CosmicScalarCapabilities.HasAllChildren(new[] { predicate })
            || CosmicScalarCapabilities.HasAllDescendants(new[] { predicate });

    // Row and scalar shapes share the same native ownership/access/membership
    // source. Result shaping must not copy or weaken those predicates.
    private static NeoCqlSource CompileCqlSource<T>(CosmicQuery<T> query, Dictionary<string,object> sharedParameters=null) where T : IHolon
    {
        var translator = sharedParameters==null ? new NeoCqlTranslator(query.ReadAccess)
            : new NeoCqlTranslator("h","p",sharedParameters,query.ReadAccess);
        var predicates = query.Predicates.Select(node => (Node: node, Sql: translator.Translate(node))).ToArray();
        var parentPredicate = predicates.Any(item => !HasSourceRelationship(item.Node))
            ? string.Join(" AND ", predicates.Where(item => !HasSourceRelationship(item.Node)).Select(item => item.Sql)) : "true";
        var descendantPredicate = predicates.Any(item => HasSourceRelationship(item.Node))
            ? string.Join(" AND ", predicates.Where(item => HasSourceRelationship(item.Node)).Select(item => item.Sql)) : "true";
        var source = "MATCH (h:Holon) OPTIONAL MATCH (h)-[p:HAS_CQL_PROJECTION]->(h) "
            + "WITH h,collect(p) AS projections WITH h,head(projections) AS p,size(projections)=1 AS uniqueProjection "
            + "WITH h,p,coalesce(uniqueProjection AND p.SchemaVersion=$schema AND p.f_Id=toLower(h.Id) AND p.f_HasChildManifest IN [true,false] AND p.f_ChildIds IS NOT NULL AND h.SnapshotJson IS NOT NULL "
            + "AND coalesce(p.ProviderRevisionId,'')=coalesce(h.ProviderRevisionId,''),false) AS valid ";
        if(!translator.Parameters.ContainsKey("schema"))translator.Parameters.Add("schema", NeoCqlProjection.SchemaVersion);
        return new NeoCqlSource
        {
            Translator = translator, Source = source, DescendantPredicate = descendantPredicate,
            EligibleSource = source + "WHERE valid AND (" + parentPredicate + ") WITH h,p " + string.Concat(translator.TraversalClauses)
                + "WITH h,p" + string.Concat(translator.TraversalStates.Select(state => "," + state)) + " ",
            Cycle = translator.TraversalCycles.Count == 0 ? "false" : "(" + string.Join(" OR ", translator.TraversalCycles) + ")",
            TraversalLimit = translator.TraversalLimits.Count == 0 ? "false" : "(" + string.Join(" OR ", translator.TraversalLimits) + ")"
        };
    }
}
