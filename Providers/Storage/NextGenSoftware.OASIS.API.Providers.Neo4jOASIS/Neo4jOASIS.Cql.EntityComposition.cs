using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS;

public partial class Neo4jOASIS : ICosmicQueryEntityCompositionStorageProvider
{
    public CosmicEntityCompositionCapabilities EntityCompositionCapabilities => new(
        CosmicEntityStageFeatures.Filter | CosmicEntityStageFeatures.Ordering | CosmicEntityStageFeatures.Paging, CqlCapabilities);

    public Task<OASISResult<IReadOnlyList<IHolon>>> ExecuteCosmicEntityCompositionAsync<T>(CosmicEntityCompositionPlan<T> plan,
        int maximumResults=100, CancellationToken cancellationToken=default) where T:IHolon
        => ExecuteCqlRowsAsync(plan?.Source, maximumResults, cancellationToken, plan);

    internal static (string Text, IReadOnlyDictionary<string, object> Parameters) CompileCqlEntityComposition<T>(
        CosmicEntityCompositionPlan<T> plan, int maximumResults) where T:IHolon
    {
        var compiled = CompileCqlSource(plan.Query.Source);
        return (CompileCqlEntityComposition(compiled, plan, maximumResults), compiled.Translator.Parameters);
    }

    private static string CompileCqlEntityComposition<T>(NeoCqlSource compiled, CosmicEntityCompositionPlan<T> plan,
        int maximumResults) where T:IHolon
    {
        var translator = compiled.Translator;
        var parameters = translator.Parameters;
        parameters.Add("entityOffset", plan.Query.Source.Offset);
        parameters.Add("entityTake", Math.Min(plan.Query.Source.Limit ?? plan.Options.MaximumInputRows + 1, plan.Options.MaximumInputRows + 1));
        parameters.Add("entityInputBound", plan.Options.MaximumInputRows);
        parameters.Add("entityWorkBound", plan.Options.MaximumStageRows);
        parameters.Add("entityOutputTake", maximumResults + 1);
        const string invalid = "null AS id,null AS revision,false AS valid,null AS snapshot,";
        const string fields = "id,revision,valid,snapshot,traversalCycle,traversalLimit,entityInput";
        var keys = translator.TranslateOrdering(plan.Query.Source.Orderings);
        var projected = string.Concat(keys.Select((key,index) => "," + key.Expression + " AS entitySort" + index));
        var ordering = string.Join(",", keys.Select((key,index) => "entitySort" + index + (key.Descending ? " DESC" : " ASC"))
            .Concat(new[] { "p.f_Id ASC" }));
        var text = new StringBuilder(compiled.EligibleSource + "WHERE (" + compiled.DescendantPredicate + ") AND NOT("
            + compiled.Cycle + " OR " + compiled.TraversalLimit + ") WITH h,p" + projected + " ORDER BY " + ordering
            + " SKIP $entityOffset LIMIT $entityTake WITH collect({h:h,p:p}) AS candidates "
            + "WITH candidates AS entitySource,candidates AS selected,false AS entityCycle,"
            + "size(candidates)>$entityInputBound AS entityLimit,0 AS entityWork ");
        var index = 0;
        foreach (var stage in plan.Query.Stages)
        {
            // Work and failure state survive every later window, even Take(0).
            text.Append("WITH entitySource,selected,entityCycle,entityLimit,entityWork+size(selected) AS entityWork ")
                .Append("WITH entitySource,selected,entityCycle,entityLimit OR entityWork>$entityWorkBound AS entityLimit,entityWork ");
            if (stage is CosmicEntityFilterStage<T> filter)
            {
                var native = new NeoCqlTranslator("h", "p", parameters, plan.Query.Source.ReadAccess);
                var predicate = string.Join(" AND ", filter.Predicate.Predicates.Select(native.Translate));
                var cycle = native.TraversalCycles.Count == 0 ? "false" : "(" + string.Join(" OR ", native.TraversalCycles) + ")";
                var limit = native.TraversalLimits.Count == 0 ? "false" : "(" + string.Join(" OR ", native.TraversalLimits) + ")";
                text.Append("CALL { WITH selected UNWIND CASE WHEN size(selected)=0 THEN [] ELSE range(0,size(selected)-1) END AS entityPosition WITH selected[entityPosition] AS item,entityPosition WITH item.h AS h,item.p AS p,entityPosition ")
                    .Append(string.Concat(native.TraversalClauses))
                    .Append("WITH h,p,entityPosition" + string.Concat(native.TraversalStates.Select(value => "," + value)) + " ORDER BY entityPosition ")
                    .Append("RETURN collect({h:h,p:p,matched:coalesce((" + predicate + "),false)}) AS evaluated,"
                        + "coalesce(max(CASE WHEN " + cycle + " THEN 1 ELSE 0 END),0)>0 AS stageCycle,"
                        + "coalesce(max(CASE WHEN " + limit + " THEN 1 ELSE 0 END),0)>0 AS stageLimit } ")
                    .Append("WITH entitySource,[item IN evaluated WHERE item.matched | {h:item.h,p:item.p}] AS selected,"
                        + "entityCycle OR stageCycle AS entityCycle,entityLimit OR stageLimit AS entityLimit,entityWork ");
            }
            else if (stage is CosmicEntityOrderStage order)
            {
                var sorted = translator.TranslateOrdering(order.Orderings);
                var project = string.Concat(sorted.Select((key,keyIndex) => "," + key.Expression + " AS stageSort" + keyIndex));
                var sort = string.Join(",", sorted.Select((key,keyIndex) => "stageSort" + keyIndex + (key.Descending ? " DESC" : " ASC"))
                    .Concat(new[] { "p.f_Id ASC" }));
                text.Append("CALL { WITH selected UNWIND selected AS item WITH item.h AS h,item.p AS p WITH h,p" + project
                    + " ORDER BY " + sort + " RETURN collect({h:h,p:p}) AS sorted } ")
                    .Append("WITH entitySource,sorted AS selected,entityCycle,entityLimit,entityWork ");
            }
            else if (stage is CosmicEntityWindowStage window)
            {
                var name = "entityWindow" + index; parameters.Add(name, window.Count);
                var slice = window.Kind == CosmicSelectedWindowKind.Take ? "[..$" + name + "]" : "[$" + name + "..]";
                text.Append("WITH entitySource,selected" + slice + " AS selected,entityCycle,entityLimit,entityWork ");
            }
            else throw new NotSupportedException("Unknown native entity stage.");
            index++;
        }
        text.Append("UNWIND CASE WHEN entityCycle OR entityLimit THEN [{h:null,p:null,input:false}] "
            + "ELSE [item IN entitySource | {h:item.h,p:item.p,input:true}] + "
            + "[item IN selected[..$entityOutputTake] | {h:item.h,p:item.p,input:false}] END AS entry ")
            .Append("RETURN entry.h.Id AS id,entry.h.ProviderRevisionId AS revision,NOT(entityCycle OR entityLimit) AS valid,"
                + "CASE WHEN size(entry.h.SnapshotJson)<=2097152 THEN entry.h.SnapshotJson ELSE null END AS snapshot,"
                + "entityCycle AS traversalCycle,entityLimit AS traversalLimit,entry.input AS entityInput ");
        var sourceErrors = compiled.Translator.TraversalClauses.Count == 0 ? "" : " UNION ALL " + compiled.EligibleSource
            + "WHERE " + compiled.Cycle + " OR " + compiled.TraversalLimit + " RETURN " + invalid
            + compiled.Cycle + " AS traversalCycle," + compiled.TraversalLimit + " AS traversalLimit,false AS entityInput LIMIT 1";
        return "CALL { " + compiled.Source + "WHERE NOT valid RETURN " + invalid
            + "false AS traversalCycle,false AS traversalLimit,false AS entityInput LIMIT 1" + sourceErrors
            + " UNION ALL " + text + " } RETURN " + fields;
    }
}
