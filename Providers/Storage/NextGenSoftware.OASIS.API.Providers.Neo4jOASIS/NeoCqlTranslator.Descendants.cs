using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS;

internal sealed partial class NeoCqlTranslator
{
    internal readonly List<string> TraversalClauses = new();
    internal readonly List<string> TraversalStates = new();
    internal readonly List<string> TraversalCycles = new();
    internal readonly List<string> TraversalLimits = new();

    private string TranslateDescendant(CosmicQueryAnyDescendant descendant)
    {
        var suffix = Parameters.Count.ToString(CultureInfo.InvariantCulture);
        var state = "desc" + suffix;
        var path = "path" + suffix;
        var nodes = "nodes" + suffix;
        var projections = "projections" + suffix;
        var projectionNode = "projectionNode" + suffix;
        var projectionEdge = "projectionEdge" + suffix;
        var index = "index" + suffix;
        var depth = descendant.Traversal.MaximumDepth.ToString(CultureInfo.InvariantCulture);
        var budget = Bind(descendant.Traversal.MaximumNodes);
        var fetch = Bind(descendant.Traversal.MaximumNodes + 1);
        var parentNode = "hopParent" + suffix;
        var childNode = "hopChild" + suffix;
        var parent = "hopParentProjection" + suffix;
        var child = "hopChildProjection" + suffix;
        var visible = "true";
        if (_access != null)
        {
            var secured = CosmicQuery<Core.Holons.Holon>.Create().WithReadAccess(_access);
            if (secured.IsError) throw new System.NotSupportedException(secured.Message);
            visible = new NeoCqlTranslator(childNode, child, Parameters, _access).Translate(secured.Result.Predicates.Single());
        }
        var matched = new NeoCqlTranslator("last(" + nodes + ")", "last(" + projections + ")", Parameters, _access)
            .Translate(descendant.Predicate);
        var edge = "EXISTS { MATCH ("+parentNode+")-["+parent+":HAS_CQL_PROJECTION]->("+parentNode+") MATCH ("+childNode+")-["+child+":HAS_CQL_PROJECTION]->("+childNode+") WHERE "+child + ".f_ParentHolonId=" + parent + ".f_Id AND (NOT " + parent
            + ".f_HasChildManifest OR " + child + ".f_Id IN " + parent + ".f_ChildIds) AND (" + visible + ") }";
        // Prefixes are simple paths. A repeated final identity is retained as a
        // cycle outcome, never expanded into arbitrary further cyclic paths.
        TraversalClauses.Add("CALL { WITH " + _node + "," + _projection
            + " MATCH " + path + "=(" + _node + ") (("+parentNode+":Holon)-[:HAS_CHILD]->("+childNode+":Holon) WHERE "+edge+"){1,"+depth+"}(descendant:Holon)"
            + " WITH descendant,nodes(" + path + ") AS " + nodes + ", [" + projectionNode + " IN nodes(" + path
            + ") | head([(" + projectionNode + ")-[" + projectionEdge + ":HAS_CQL_PROJECTION]->(" + projectionNode + ") | " + projectionEdge + "])] AS " + projections
            + " WHERE all(" + index + " IN range(0,size(" + nodes + ")-2) WHERE single(prefixNode IN " + nodes
            + "[0..-1] WHERE prefixNode=" + nodes + "[" + index + "]))"
            + " WITH DISTINCT descendant,any(prefixNode IN " + nodes + "[0..-1] WHERE prefixNode=last(" + nodes + ")) AS cycle,"
            + matched + " AS matched LIMIT " + fetch
            + " WITH collect({cycle:cycle,matched:matched}) AS states RETURN {cycle:any(s IN states WHERE s.cycle),"
            + "overBudget:size(states)>" + budget + ",matched:any(s IN states WHERE s.matched)} AS " + state + " } ");
        TraversalCycles.Add(state + ".cycle");
        TraversalLimits.Add(state + ".overBudget");
        TraversalStates.Add(state);
        return state + ".matched";
    }
}
