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

    /// <summary>
    /// "Any ancestor within the bound satisfies the predicate", as one inline boolean: a quantified path up the HAS_CHILD edges
    /// (each relationship is used once per path, so a loop cannot run on), then the predicate on that ancestor's projection.
    /// It needs none of the traversal clause plumbing because it never errors on size: the depth bound is the whole contract.
    /// The ancestor need not be readable by the caller; the right comes from the flag the predicate tests.
    /// </summary>
    private string TranslateAncestor(CosmicQueryAnyAncestor ancestor)
    {
        var suffix = Parameters.Count.ToString(CultureInfo.InvariantCulture);
        var hopChild = "upChild" + suffix;
        var hopParent = "upParent" + suffix;
        var candidate = "ancestor" + suffix;
        var candidateProjection = "ancestorProjection" + suffix;
        var depth = ancestor.Traversal.MaximumDepth.ToString(CultureInfo.InvariantCulture);
        var inner = new NeoCqlTranslator(candidate, candidateProjection, Parameters, _access, _relationshipDepth + 1);
        var matched = inner.Translate(ancestor.Predicate);
        return "EXISTS { MATCH (" + _node + ") ((" + hopChild + ":Holon)<-[:HAS_CHILD]-(" + hopParent + ":Holon)){1," + depth + "}(" + candidate + ":Holon) "
            + "MATCH (" + candidate + ")-[" + candidateProjection + ":HAS_CQL_PROJECTION]->(" + candidate + ") "
            + "WHERE coalesce((" + matched + "),false) }";
    }

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
        var candidate = "candidate" + suffix;
        var candidateProjection = "candidateProjection" + suffix;
        var inner = new NeoCqlTranslator(candidate, candidateProjection, Parameters, _access, _relationshipDepth + 1);
        var matched = inner.Translate(descendant.Predicate);
        var innerCycle = inner.TraversalCycles.Count == 0 ? "false" : "(" + string.Join(" OR ", inner.TraversalCycles) + ")";
        var innerLimit = inner.TraversalLimits.Count == 0 ? "false" : "(" + string.Join(" OR ", inner.TraversalLimits) + ")";
        var edge = "EXISTS { MATCH ("+parentNode+")-["+parent+":HAS_CQL_PROJECTION]->("+parentNode+") MATCH ("+childNode+")-["+child+":HAS_CQL_PROJECTION]->("+childNode+") WHERE "+child + ".f_ParentHolonId=" + parent + ".f_Id AND (NOT " + parent
            + ".f_HasChildManifest OR " + child + ".f_Id IN " + parent + ".f_ChildIds) AND (" + visible + ") }";
        // Prefixes are simple paths. A repeated final identity is retained as a
        // cycle outcome, never expanded into arbitrary further cyclic paths.
        TraversalClauses.Add("CALL { WITH " + _node + "," + _projection
            + " MATCH " + path + "=(" + _node + ") (("+parentNode+":Holon)-[:HAS_CHILD]->("+childNode+":Holon) WHERE "+edge+"){1,"+depth+"}(" + candidate + ":Holon)"
            + " WITH " + candidate + ",nodes(" + path + ") AS " + nodes + ", [" + projectionNode + " IN nodes(" + path
            + ") | head([(" + projectionNode + ")-[" + projectionEdge + ":HAS_CQL_PROJECTION]->(" + projectionNode + ") | " + projectionEdge + "])] AS " + projections
            + " WHERE all(" + index + " IN range(0,size(" + nodes + ")-2) WHERE single(prefixNode IN " + nodes
            + "[0..-1] WHERE prefixNode=" + nodes + "[" + index + "]))"
            + " WITH DISTINCT " + candidate + ",last(" + projections + ") AS " + candidateProjection + ","
            + "any(prefixNode IN " + nodes + "[0..-1] WHERE prefixNode=last(" + nodes + ")) AS pathCycle LIMIT " + fetch
            + " " + string.Concat(inner.TraversalClauses)
            + " WITH (pathCycle OR " + innerCycle + ") AS cycle," + innerLimit + " AS limited,coalesce((" + matched + "),false) AS matched"
            + " WITH collect({cycle:cycle,limited:limited,matched:matched}) AS states RETURN {cycle:any(s IN states WHERE s.cycle),"
            + "overBudget:size(states)>" + budget + " OR any(s IN states WHERE s.limited),matched:any(s IN states WHERE s.matched)} AS " + state + " } ");
        TraversalCycles.Add(state + ".cycle");
        TraversalLimits.Add(state + ".overBudget");
        TraversalStates.Add(state);
        return state + ".matched";
    }
}
