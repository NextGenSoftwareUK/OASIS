using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS;

internal sealed class NeoCqlCompositionCommand
{
    internal string Text { get; }
    internal Dictionary<string, object> Parameters { get; }
    internal NeoCqlCompositionCommand(string text, Dictionary<string, object> parameters)
    { Text = text; Parameters = parameters; }
}

public partial class Neo4jOASIS
{
    // Every stage uses captured scalar cells, never a repeated MATCH against
    // mutable source records. This does not grant repeatable-snapshot reads.
    internal static NeoCqlCompositionCommand CompileCqlComposition<T, TResult>(
        CosmicProjectionCompositionPlan<T, TResult> plan, CosmicProjectionCompositionExecutionLimits limits) where T : IHolon
    {
        if (plan == null || limits == null || limits.Admit(plan.Options).IsError
            || plan.Targets.Count != 1 || plan.Targets[0].Provider != Core.Enums.ProviderType.Neo4jOASIS
            || plan.Query.ResultStages.Count == 0)
            throw new ArgumentException("Composition requires a matching single-provider plan and admitted remaining limits.");
        return CompileCqlSelected(plan.Query, limits);
    }

    internal static NeoCqlCompositionCommand CompileCqlProjectedAll<T, TResult>(
        CosmicProjectedAllPlan<T, TResult> plan, CosmicProjectionCompositionExecutionLimits limits) where T : IHolon
    {
        if (plan == null || limits == null || limits.Admit(plan.Options).IsError
            || plan.Targets.Count != 1 || plan.Targets[0].Provider != Core.Enums.ProviderType.Neo4jOASIS)
            throw new ArgumentException("Projected All requires one matching plan and remaining work.");
        return CompileCqlSelected(plan.Query.Projection, limits, plan.Query.Predicate);
    }

    private static NeoCqlCompositionCommand CompileCqlSelected<T, TResult>(CosmicProjectionQuery<T, TResult> query,
        CosmicProjectionCompositionExecutionLimits limits, CosmicQueryPredicate terminal = null) where T : IHolon
    {
        if (query.Source.GraphOptions != null) throw new NotSupportedException("Graph-source composition requires separate enrollment.");
        var parameters = new Dictionary<string, object>(StringComparer.Ordinal);
        var source = CompileCqlDetachedSource(query.Source, parameters, Array.Empty<CosmicQueryPath>(),
            query.Columns.Select(column => column.Path).ToArray(), limits.MaximumInputRows, limits.MaximumInputPayloadBytes);
        var binder = new NeoCqlTranslator("h", "p", parameters, null);
        var maximumRows = binder.Bind(limits.MaximumInputRows);
        var maximumBytes = binder.Bind(limits.MaximumInputPayloadBytes);
        var maximumWork = binder.Bind(limits.MaximumStageRows);
        var maximumProof = binder.Bind(limits.MaximumProofPayloadBytes);
        var valid = "size(input)<=" + maximumRows
            + " AND all(x IN input WHERE x.valid AND x.domainValid AND NOT(x.cycle OR x.traversalLimit OR x.payloadLimit))"
            + " AND reduce(total=0,x IN input | total+x.bytes)<=" + maximumBytes;
        var text = new StringBuilder("CALL { CALL { " + source + " } RETURN collect(input) AS input } "
            + "WITH input,CASE WHEN " + valid + " THEN range(0,size(input)-1) ELSE [] END AS s0,0 AS work,0 AS proofBytes ");
        var aliases = query.Columns.Select((column, index) => new { column.Alias, Index = index })
            .ToDictionary(item => item.Alias, item => item.Index, StringComparer.Ordinal);
        var filters = new GroupFilterCompiler<T, object, object>(binder, path =>
        {
            if (path.IsMetadata || path.Segments.Count != 1 || !aliases.TryGetValue(path.Segments[0], out var index)
                || query.Columns[index].Path.ValueType != path.ValueType)
                throw new NotSupportedException("Composition filters require exact selected scalar bindings.");
            return "input[position].values[" + index + "].v";
        });
        var carried = "input,s0";
        for (var index = 0; index < query.ResultStages.Count; index++)
        {
            var previous = "s" + index; var next = "s" + (index + 1);
            text.Append("WITH ").Append(carried).Append(",work+size(").Append(previous).Append(") AS work,proofBytes ");
            // Preserve the exceeded work in the header, but avoid further native
            // stage work after admission bounds are exhausted. The decoder must
            // reject this outcome; an empty list cannot conceal exhaustion.
            text.Append("CALL { WITH input,").Append(previous).Append(",work,proofBytes ")
                .Append("WITH input,").Append(previous).Append(" WHERE work<=").Append(maximumWork)
                .Append(" AND proofBytes<=").Append(maximumProof)
                .Append(" UNWIND range(0,size(").Append(previous).Append(")-1) AS rank ")
                .Append("WITH input,rank,").Append(previous).Append("[rank] AS position ");
            var stage = query.ResultStages[index];
            if (stage is CosmicSelectedFilterStage filter)
                text.Append("WHERE ").Append(filters.Translate(filter.Predicate)).Append(" WITH position,rank ORDER BY rank ASC ");
            else if (stage is CosmicSelectedOrderStage sorted)
            {
                // Neo4j's default null order differs from CQL. Explicit null
                // ranks preserve null-first ascending and null-last descending.
                var exact = sorted.Orderings.SelectMany(item => new[]
                {
                    "CASE WHEN input[position].values[" + item.ColumnIndex + "].v IS NULL THEN 0 ELSE 1 END" + (item.Descending ? " DESC" : " ASC"),
                    "input[position].values[" + item.ColumnIndex + "].v" + (item.Descending ? " DESC" : " ASC")
                });
                text.Append("WITH input,position,rank ORDER BY ").Append(string.Join(",", exact.Concat(new[] { "rank ASC" }))).Append(' ');
            }
            else if (stage is CosmicSelectedWindowStage window)
            {
                text.Append("WITH position,rank ORDER BY rank ASC ")
                    .Append(window.Kind == CosmicSelectedWindowKind.Skip ? "SKIP " : "LIMIT ")
                    .Append(binder.Bind(window.Count)).Append(' ');
            }
            else throw new NotSupportedException("Unknown selected-result stage.");
            text.Append("RETURN collect(position) AS ").Append(next).Append(" } ");
            carried += "," + next;
            text.Append("WITH ").Append(carried).Append(",work,proofBytes+16*size(").Append(next).Append(") AS proofBytes ");
        }

        // Explicit headers make empty stages observable, rather than inferred
        // from absent rows. Source proofs precede all stage identities.
        var import = carried + ",work,proofBytes";
        text.Append("CALL { WITH ").Append(import)
            .Append(" RETURN -2 AS phase,-1 AS position,-1 AS sourcePosition,null AS proof,size(input) AS nativeCount,work AS stageWork ")
            .Append("UNION ALL WITH ").Append(import)
            .Append(" UNWIND range(0,size(input)-1) AS pos RETURN -1 AS phase,pos AS position,pos AS sourcePosition,input[pos] AS proof,size(input) AS nativeCount,work AS stageWork ");
        for (var index = 0; index < query.ResultStages.Count; index++)
        {
            var list = "s" + (index + 1);
            text.Append("UNION ALL WITH ").Append(import).Append(" RETURN ").Append(index)
                .Append(" AS phase,-1 AS position,-1 AS sourcePosition,null AS proof,size(").Append(list).Append(") AS nativeCount,work AS stageWork ")
                .Append("UNION ALL WITH ").Append(import).Append(" UNWIND range(0,size(").Append(list).Append(")-1) AS pos RETURN ").Append(index)
                .Append(" AS phase,pos AS position,").Append(list).Append("[pos] AS sourcePosition,null AS proof,size(").Append(list).Append(") AS nativeCount,work AS stageWork ");
        }
        if (terminal != null)
        {
            var final = "s" + query.ResultStages.Count;
            // Reduction consumes captured scalar cells, never a second MATCH.
            // A negative count is an explicit exhausted-work marker, not true.
            text.Append("UNION ALL WITH ").Append(import).Append(" RETURN ").Append(query.ResultStages.Count)
                .Append(" AS phase,-1 AS position,-1 AS sourcePosition,null AS proof,CASE WHEN work+size(")
                .Append(final).Append(")<=").Append(maximumWork).Append(" AND proofBytes<=").Append(maximumProof)
                .Append(" THEN size([position IN ").Append(final).Append(" WHERE NOT coalesce((")
                .Append(filters.Translate(terminal)).Append("),false)]) ELSE -1 END AS nativeCount,work AS stageWork ");
        }
        text.Append("} RETURN phase,position,sourcePosition,proof,nativeCount,stageWork ORDER BY phase ASC,position ASC");
        if (text.Length > 1024 * 1024) throw new NeoCqlLimitException("Native composition exceeds its statement-size budget.");
        return new NeoCqlCompositionCommand(text.ToString(), parameters);
    }
}
