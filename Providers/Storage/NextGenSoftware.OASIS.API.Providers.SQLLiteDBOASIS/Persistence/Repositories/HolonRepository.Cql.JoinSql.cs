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
            var operation = selection.Join.Keys[index].Comparison;
            var type = Nullable.GetUnderlyingType(selection.Join.Keys[index].Left.ValueType)
                ?? selection.Join.Keys[index].Left.ValueType;
            var collation = type == typeof(Guid) ? " COLLATE NOCASE" : " COLLATE BINARY";
            if (operation is CosmicJoinComparison.NullSafeEqual or CosmicJoinComparison.NullSafeNotEqual)
            {
                var nullableOperator = operation == CosmicJoinComparison.NullSafeEqual ? "IS" : "IS NOT";
                equality.Add($"(CASE WHEN l.k{index}s=2 THEN l.k{index}v END){collation} {nullableOperator} "
                    + $"(CASE WHEN r.k{index}s=2 THEN r.k{index}v END)");
                continue;
            }
            var comparison = operation switch
            {
                CosmicJoinComparison.Equal => "=",
                CosmicJoinComparison.NotEqual => "<>",
                CosmicJoinComparison.LessThan => "<",
                CosmicJoinComparison.LessThanOrEqual => "<=",
                CosmicJoinComparison.GreaterThan => ">",
                CosmicJoinComparison.GreaterThanOrEqual => ">=",
                _ => throw new NotSupportedException("Unknown native join comparison.")
            };
            equality.Add($"l.k{index}s=2 AND r.k{index}s=2 AND l.k{index}v{collation}{comparison}r.k{index}v{collation}");
        }
        var keyPredicate = selection.Join.CombineKeyPredicates(equality.Select(value => "(" + value + ")").ToArray(),
            group => "(" + string.Join(" AND ", group) + ")", groups => "(" + string.Join(" OR ", groups) + ")");
        var join = selection.Join.Kind switch
        {
            CosmicJoinKind.Left => " LEFT JOIN r ON " + keyPredicate,
            CosmicJoinKind.Inner => " INNER JOIN r ON " + keyPredicate,
            CosmicJoinKind.Full => " FULL OUTER JOIN r ON " + keyPredicate,
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
            if (selection.OutputFromEnd > 0)
            {
                const string count = "(SELECT count(*) FROM pairs)";
                var end = selection.OutputTailWindowLimit is int window
                    ? "min(" + count + "," + left.Bind((long)selection.OutputOffset + window) + ")" : count;
                var index = "(" + end + "-" + left.Bind(selection.OutputFromEnd) + ")";
                limit = "CASE WHEN " + index + ">=" + offset + " THEN 1 ELSE 0 END";
                offset = "max(0," + index + ")";
            }
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

        string Source<T>(CqlSqlTranslator translator,CosmicQuery<T> query,bool isLeft,int maximumRows,string predicate)
            where T:IHolon
            =>CompileKeyedSourceSql(translator,query,
                selection.Join.Keys.Select(key=>isLeft?key.Left:key.Right).ToArray(),
                selection.Columns.Where(column=>column.Source==(isLeft?CosmicJoinColumnSource.Left:CosmicJoinColumnSource.Right))
                    .Select(column=>column.Path).ToArray(),maximumRows,predicate,isLeft?"joinL":"joinR");
    }
}
