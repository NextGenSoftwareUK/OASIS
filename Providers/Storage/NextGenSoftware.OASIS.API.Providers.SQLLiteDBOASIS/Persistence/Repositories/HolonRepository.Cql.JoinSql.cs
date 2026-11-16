using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Data.Sqlite;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Repositories;

public partial class HolonRepository
{
    // All three statements must execute under the same serializable read owner.
    // Keeping source proofs separate avoids duplicating large selected cells for
    // every pair. Pair membership is produced exclusively by SQLite's JOIN.
    private sealed class JoinSql
    {
        internal string Left, Right, Pairs;
        internal string LeftPredicate, RightPredicate;
        internal bool HasRelationship;
        internal List<SqliteParameter> Parameters;
    }

    private static JoinSql CompileJoinSql<TLeft, TRight, TKey, TResult>(
        CosmicJoinSelection<TLeft, TRight, TKey, TResult> selection, CosmicJoinExecutionLimits limits)
        where TLeft : IHolon where TRight : IHolon
    {
        var parameters = new List<SqliteParameter>();
        var left = new CqlSqlTranslator("Holon", parameters, selection.Join.Left.ReadAccess);
        var right = new CqlSqlTranslator("Holon", parameters, selection.Join.Right.ReadAccess);
        var lp = Predicate(left, selection.Join.Left);
        var rp = Predicate(right, selection.Join.Right);
        var ls = Source(left, selection.Join.Left, true, limits.MaximumLeftRows, lp);
        if (selection.Join.RestrictsRightToLeftParents)
        {
            var parent = new CqlSqlTranslator("joinParent", parameters, selection.Join.Left.ReadAccess);
            rp = "(" + rp + ") AND " + right.ScopedJoinChildPredicate(parent, ls);
        }
        var rs = Source(right, selection.Join.Right, false, limits.MaximumRightRows, rp);
        var equality = new List<string>();
        for (var index = 0; index < selection.Join.Keys.Count; index++)
        {
            // Null/missing components never match, including null == null.
            var type = Nullable.GetUnderlyingType(selection.Join.Keys[index].Left.ValueType)
                ?? selection.Join.Keys[index].Left.ValueType;
            var collation = type == typeof(Guid) ? " COLLATE NOCASE" : " COLLATE BINARY";
            equality.Add($"l.k{index}s=2 AND r.k{index}s=2 AND l.k{index}v{collation}=r.k{index}v{collation}");
        }
        var join = selection.Join.Kind switch
        {
            CosmicJoinKind.Left => " LEFT JOIN r ON " + string.Join(" AND ", equality.Select(value => "(" + value + ")")),
            CosmicJoinKind.Inner => " INNER JOIN r ON " + string.Join(" AND ", equality.Select(value => "(" + value + ")")),
            CosmicJoinKind.Full => " FULL OUTER JOIN r ON " + string.Join(" AND ", equality.Select(value => "(" + value + ")")),
            CosmicJoinKind.Cross => " CROSS JOIN r",
            _ => throw new NotSupportedException("Unknown native SQLite join kind.")
        };
        var maximumPairs = selection.OutputLimit.HasValue
            ? limits.MaximumPairs
            : Math.Min(limits.MaximumPairs, (long)limits.MaximumResults);
        var cap = maximumPairs + 1;
        var pairLimit = left.Bind(cap);
        var orderTerms = new List<string>();
        var orderProjection = "";
        foreach (var order in selection.OutputOrdering)
        {
            var column = selection.Columns[order.Column];
            var source = column.Source == CosmicJoinColumnSource.Left ? "l" : "r";
            var index = selection.Columns.Take(order.Column).Count(item => item.Source == column.Source);
            var cell = source + ".v" + index;
            var alias = "orderValue" + orderTerms.Count;
            orderProjection += ",CASE WHEN " + cell + "s=2 THEN " + cell + "v END AS " + alias;
            var collation = (Nullable.GetUnderlyingType(column.ValueType) ?? column.ValueType) == typeof(Guid)
                ? " COLLATE NOCASE" : " COLLATE BINARY";
            orderTerms.Add(alias + collation + (order.Descending ? " DESC NULLS LAST" : " ASC NULLS FIRST"));
        }
        var pairOrder = string.Join(",", orderTerms.Concat(new[] { "lp NULLS LAST", "rp" }));
        var pairs = "SELECT l.position AS lp,r.position AS rp" + orderProjection + " FROM l" + join;
        var sources = "WITH l AS MATERIALIZED (" + ls + "),r AS MATERIALIZED (" + rs + ") ";
        string output;
        if (selection.OutputLimit is int requestedLimit)
        {
            var limit = left.Bind(requestedLimit);
            var offset = left.Bind(selection.OutputOffset);
            output = sources + ",pairs AS MATERIALIZED (" + pairs + " LIMIT " + pairLimit
                + "),page AS MATERIALIZED (SELECT *,1 AS present FROM pairs ORDER BY " + pairOrder
                + " LIMIT " + limit + " OFFSET " + offset + ")"
                + " SELECT lp,rp,nativeCount,present FROM (SELECT count(*) AS nativeCount FROM pairs)"
                + " LEFT JOIN page ON 1=1 ORDER BY " + pairOrder;
        }
        else
        {
            output = "SELECT lp,rp,count(*) OVER () AS nativeCount,1 AS present FROM (" + sources
                + pairs + " ORDER BY " + pairOrder + " LIMIT " + pairLimit + ") ORDER BY " + pairOrder;
        }

        return new JoinSql
        {
            Left = ls, Right = rs, Parameters = parameters,
            LeftPredicate = lp, RightPredicate = rp,
            HasRelationship = left.HasRelationship || right.HasRelationship,
            Pairs = output
        };

        static string Predicate<T>(CqlSqlTranslator translator, CosmicQuery<T> query) where T : IHolon
            => translator.TranslateSource(query.Predicates);

        string Source<T>(CqlSqlTranslator translator, CosmicQuery<T> query, bool isLeft, int maximumRows, string predicate)
            where T : IHolon
        {
            var ordering = translator.TranslateOrdering(query.Orderings);
            var cells = new List<string>
            {
                "row_number() OVER (ORDER BY " + ordering + ") AS position",
                "\"Holon\".\"Id\" AS id", "\"Holon\".\"IsPublic\" AS public",
                "\"Holon\".\"CreatedByAvatarId\" AS creator"
            };
            for (var index = 0; index < selection.Join.Keys.Count; index++)
                Cell(isLeft ? selection.Join.Keys[index].Left : selection.Join.Keys[index].Right, "k" + index);
            var selected = selection.Columns.Where(column => column.Source ==
                (isLeft ? CosmicJoinColumnSource.Left : CosmicJoinColumnSource.Right)).ToArray();
            for (var index = 0; index < selected.Length; index++) Cell(selected[index].Path, "v" + index);
            for (var index = 0; index < query.Orderings.Count; index++) Cell(query.Orderings[index].Path, "o" + index);
            var take = translator.Bind(Math.Min(query.Limit.HasValue ? (long)query.Limit.Value : long.MaxValue, (long)maximumRows + 1));
            var offset = translator.Bind((long)query.Offset);
            return "SELECT " + string.Join(",", cells) + " FROM Holon WHERE " + predicate
                + " ORDER BY " + ordering + " LIMIT " + take + " OFFSET " + offset;

            void Cell(CosmicQueryPath path, string alias)
            {
                var operand = translator.ProjectionOperand(path, "join" + (isLeft ? "L" : "R") + alias, comparable: true);
                cells.Add(operand.State + " AS " + alias + "s");
                cells.Add(operand.Value + " AS " + alias + "v");
            }
        }
    }
}
