using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Data.Sqlite;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Repositories;

public partial class HolonRepository
{
    // Both statements must run under one serializable read owner. Read and
    // validate the entire source before any selected filter/window can hide it.
    private sealed class CompositionSql
    {
        internal string Source, Proof, Predicate, Reduction, Cardinality, Numeric;
        internal bool HasRelationship;
        internal List<SqliteParameter> Parameters;
    }

    private static CompositionSql CompileCompositionSql<T, TResult>(
        CosmicProjectionCompositionPlan<T, TResult> plan, CosmicProjectionCompositionExecutionLimits limits) where T : IHolon
    {
        if (plan == null || limits == null || plan.Targets.Count != 1 || plan.Targets[0].Provider != ProviderType.SQLLiteDBOASIS)
            throw new ArgumentException("SQLite composition requires one matching plan and remaining limits.");
        return CompileSelectedSql(plan.Query, plan.Options, limits, cardinality: plan.RequestsCardinality, numeric: plan.NumericReduction);
    }

    // One compiler owns both the selected pipeline and its terminal reduction.
    private static CompositionSql CompileSelectedSql<T, TResult>(CosmicProjectionQuery<T, TResult> query,
        CosmicProjectionCompositionOptions options, CosmicProjectionCompositionExecutionLimits limits,
        CosmicQueryPredicate terminal = null, bool cardinality = false, CosmicProjectedNumericIntent numeric = null) where T : IHolon
    {
        var bounded = limits.Admit(options);
        if (bounded.IsError) throw new ArgumentException(bounded.Message);
        if (query.ResultStages.Count == 0 && terminal == null && !cardinality && numeric == null) throw new ArgumentException("A selected-result pipeline is required.");
        if (query.Source.GraphOptions != null) throw new NotSupportedException("Composed graph sources require separate native enrollment.");
        var parameters = new List<SqliteParameter>();
        var translator = new CqlSqlTranslator("Holon", parameters, query.Source.ReadAccess);
        var predicate = translator.TranslateSource(query.Source.Predicates);
        var sourceOrder = translator.TranslateOrdering(query.Source.Orderings);
        var cells = new List<string>
        {
            "row_number() OVER (ORDER BY " + sourceOrder + ") AS position",
            "\"Holon\".\"Id\" AS id", "\"Holon\".\"IsPublic\" AS public", "\"Holon\".\"CreatedByAvatarId\" AS creator"
        };
        for (var index = 0; index < query.Columns.Count; index++) Cell(query.Columns[index].Path, "v" + index);
        for (var index = 0; index < query.Source.Orderings.Count; index++) Cell(query.Source.Orderings[index].Path, "o" + index);
        var inputLimit = translator.Bind(Math.Min(query.Source.Limit.HasValue ? (long)query.Source.Limit.Value : long.MaxValue,
            (long)limits.MaximumInputRows + 1));
        var inputOffset = translator.Bind((long)query.Source.Offset);
        var source = "SELECT " + string.Join(",", cells) + " FROM Holon WHERE " + predicate
            + " ORDER BY " + sourceOrder + " LIMIT " + inputLimit + " OFFSET " + inputOffset;
        var stages = new List<string> { "c0 AS MATERIALIZED (" + source + ")" };
        var proof = new List<string>();
        var work = new List<string>();
        var previousRank = "position";
        for (var index = 0; index < query.ResultStages.Count; index++)
        {
            var from = "c" + index; var name = "c" + (index + 1); var rank = "r" + (index + 1);
            var tail = ""; var ranking = previousRank;
            var stage = query.ResultStages[index];
            if (stage is CosmicSelectedFilterStage filter)
                tail = " WHERE " + translator.TranslateCompositionPredicate(query, filter.Predicate);
            else if (stage is CosmicSelectedOrderStage ordering)
            {
                var keys = ordering.Orderings.Select(item => "v" + item.ColumnIndex + "v"
                    + CqlSqlTranslator.SelectedCollation(query.Columns[item.ColumnIndex].Path.ValueType)
                    + (item.Descending ? " DESC" : " ASC")).ToList();
                keys.Add(previousRank + " ASC");
                ranking = "row_number() OVER (ORDER BY " + string.Join(",", keys) + ")";
            }
            else if (stage is CosmicSelectedReverseStage)
                ranking = "row_number() OVER (ORDER BY " + previousRank + " DESC)";
            else if (stage is CosmicSelectedWindowStage window)
                tail = " ORDER BY " + previousRank + " LIMIT " + translator.Bind(window.Kind == CosmicSelectedWindowKind.Take ? (long)window.Count : -1L)
                    + " OFFSET " + translator.Bind(window.Kind == CosmicSelectedWindowKind.Skip ? (long)window.Count : 0L);
            else throw new NotSupportedException("Unknown selected composition stage.");
            stages.Add(name + " AS MATERIALIZED (SELECT *," + ranking + " AS " + rank + " FROM " + from + tail + ")");
            proof.Add("SELECT " + index + " AS phase,position," + rank + " AS ordinal,0 AS sourceCount,0 AS stageRows FROM " + name);
            work.Add("(SELECT count(*) FROM " + from + ")");
            previousRank = rank;
        }
        var header = "SELECT -1 AS phase,0 AS position,0 AS ordinal,(SELECT count(*) FROM c0) AS sourceCount,"
            + (work.Count == 0 ? "0" : string.Join("+", work)) + " AS stageRows";
        var common = "WITH " + string.Join(",", stages) + " ";
        var reduction = terminal == null ? null : common + "SELECT count(*) FROM c" + query.ResultStages.Count
            + " WHERE NOT (" + translator.TranslateCompositionPredicate(query, terminal) + ")";
        return new CompositionSql
        {
            Source = source, Predicate = predicate, HasRelationship = translator.HasRelationship, Parameters = parameters,
            Proof = common + header + (proof.Count == 0 ? "" : " UNION ALL " + string.Join(" UNION ALL ", proof))
                + " ORDER BY phase,ordinal,position", Reduction = reduction,
            Cardinality = common + "SELECT count(*) FROM c" + query.ResultStages.Count,
            Numeric = numeric == null ? null : common + CqlSqlTranslator.SelectedNumericReduction(numeric, "c" + query.ResultStages.Count, previousRank)
        };

        void Cell(CosmicQueryPath path, string alias)
        {
            var operand = translator.ProjectionOperand(path, "composed" + alias, comparable: true);
            cells.Add(operand.State + " AS " + alias + "s"); cells.Add(operand.Value + " AS " + alias + "v");
        }
    }

    private sealed partial class CqlSqlTranslator
    {
        internal static string SelectedCollation(Type declared) => GroupCollation(declared);
        internal string TranslateCompositionPredicate<T, TResult>(CosmicProjectionQuery<T, TResult> query,
            CosmicQueryPredicate predicate) where T : IHolon
            => TranslateSelectedPredicate(predicate, path =>
            {
                var index = !path.IsMetadata && path.Segments.Count == 1
                    ? query.Columns.ToList().FindIndex(column => column.Alias == path.Segments[0] && column.Path.ValueType == path.ValueType) : -1;
                if (index < 0) throw new NotSupportedException("Composition filter requires an exact selected alias.");
                var type = Nullable.GetUnderlyingType(path.ValueType) ?? path.ValueType;
                return new GroupFilterCell
                {
                    Type = path.ValueType, Value = "v" + index + "v" + GroupCollation(path.ValueType),
                    NormalizedNumericKey = type == typeof(decimal) || type == typeof(double) || type == typeof(float)
                };
            });
    }
}
