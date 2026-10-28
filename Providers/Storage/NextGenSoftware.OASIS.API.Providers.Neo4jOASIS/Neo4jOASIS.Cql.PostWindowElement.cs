using System;
using System.Linq;
using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS;

public partial class Neo4jOASIS
{
    internal static (string Text, IReadOnlyDictionary<string, object> Parameters) CompileCqlPostWindowElement<T>(CosmicQueryPlan<T> plan) where T : IHolon
    {
        var source = CompileCqlSource(plan.Query);
        return (CompileCqlPostWindowElement(source, plan), source.Translator.Parameters);
    }

    private static string CompileCqlPostWindowElement<T>(NeoCqlSource compiled, CosmicQueryPlan<T> plan) where T : IHolon
    {
        if (plan.PostWindowPredicate == null || plan.PostWindowLimit is not (1 or 2))
            throw new ArgumentException("An admitted bounded post-window element is required.");
        var source = compiled.Translator;
        var terminal = new NeoCqlTranslator("h", "p", source.Parameters, plan.Query.ReadAccess);
        var predicate = string.Join(" AND ", plan.PostWindowPredicate.Predicates.Select(terminal.Translate));
        var cycle = terminal.TraversalCycles.Count == 0 ? "false" : "(" + string.Join(" OR ", terminal.TraversalCycles) + ")";
        var limited = terminal.TraversalLimits.Count == 0 ? "false" : "(" + string.Join(" OR ", terminal.TraversalLimits) + ")";
        var keys = source.TranslateOrdering(plan.Query.Orderings);
        var aliases = keys.Select((_, index) => "elementSort" + index).ToArray();
        var projected = string.Concat(keys.Select((key, index) => "," + key.Expression + " AS " + aliases[index]));
        var carried = string.Concat(aliases.Select(alias => "," + alias));
        var order = string.Join(",", keys.Select((key, index) => aliases[index] + (key.Descending ? " DESC" : " ASC"))
            .Concat(new[] { "p.f_Id ASC" }));
        var maximumWork = CosmicProjectionResults.MaximumResultLimit;
        source.Parameters.Add("elementOffset", plan.Query.Offset);
        source.Parameters.Add("elementTake", Math.Min(plan.Query.Limit ?? maximumWork + 1, maximumWork + 1));
        source.Parameters.Add("elementLimit", plan.PostWindowLimit.Value);
        source.Parameters.Add("elementMaximumWork", maximumWork);
        const string fields = "id,revision,valid,snapshot,traversalCycle,traversalLimit";
        const string invalid = "null AS id,null AS revision,false AS valid,null AS snapshot,";
        var sourceError = compiled.Translator.TraversalClauses.Count == 0 ? "" : " UNION ALL " + compiled.EligibleSource
            + "WHERE " + compiled.Cycle + " OR " + compiled.TraversalLimit
            + " RETURN " + invalid + compiled.Cycle + " AS traversalCycle," + compiled.TraversalLimit + " AS traversalLimit LIMIT 1";
        var winners = plan.TailIndex.HasValue ? "reverse(matches)" : "matches";
        // Capture references and terminal outcomes, not unbounded snapshot strings.
        // An extra source row is a work-limit sentinel, never a truncated success.
        return "CALL { " + compiled.Source + "WHERE NOT valid RETURN " + invalid
            + "false AS traversalCycle,false AS traversalLimit LIMIT 1" + sourceError
            + " UNION ALL " + compiled.EligibleSource + "WHERE (" + compiled.DescendantPredicate + ") AND NOT("
            + compiled.Cycle + " OR " + compiled.TraversalLimit + ") "
            + "WITH h,p" + projected + " ORDER BY " + order + " SKIP $elementOffset LIMIT $elementTake "
            + string.Concat(terminal.TraversalClauses)
            + "WITH h,p" + carried + string.Concat(terminal.TraversalStates.Select(state => "," + state)) + " ORDER BY " + order + " "
            + "WITH collect({h:h,p:p,matched:coalesce((" + predicate + "),false)}) AS candidates,"
            + "max(CASE WHEN " + cycle + " THEN 1 ELSE 0 END) AS cycle,"
            + "max(CASE WHEN " + limited + " THEN 1 ELSE 0 END) AS limited,count(*) AS work "
            + "WITH candidates,coalesce(cycle,0) AS cycle,coalesce(limited,0) AS limited,work "
            + "CALL { WITH candidates,cycle,limited,work WITH candidates,cycle,limited,work "
            + "WHERE cycle>0 OR limited>0 OR work>$elementMaximumWork RETURN " + invalid
            + "cycle>0 AS traversalCycle,limited>0 OR work>$elementMaximumWork AS traversalLimit "
            + "UNION ALL WITH candidates,cycle,limited,work WITH candidates,cycle,limited,work "
            + "WHERE cycle=0 AND limited=0 AND work<=$elementMaximumWork "
            + "WITH [candidate IN candidates WHERE candidate.matched] AS matches "
            + "UNWIND (" + winners + ")[0..$elementLimit] AS winner "
            + "RETURN winner.h.Id AS id,winner.h.ProviderRevisionId AS revision,true AS valid,"
            + "CASE WHEN size(winner.h.SnapshotJson)<=2097152 THEN winner.h.SnapshotJson ELSE null END AS snapshot,"
            + "false AS traversalCycle,false AS traversalLimit } RETURN " + fields + " } RETURN " + fields;
    }
}
