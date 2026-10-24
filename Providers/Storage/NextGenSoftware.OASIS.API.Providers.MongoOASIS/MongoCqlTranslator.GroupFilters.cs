using System;
using System.Linq;
using MongoDB.Bson;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS;

internal sealed partial class MongoCqlTranslator
{
    private static BsonValue GroupedSumValue(int index)
        => Op("$add", Op("$multiply", "$a" + index + "high",
            Literal(new BsonDecimal128(new Decimal128(CosmicNumericScalarResults.LimbBase)))), "$a" + index + "low");

    private static BsonValue GroupFilterValue<T, TKey, TResult>(CosmicGroupedSelection<T, TKey, TResult> query,
        CosmicQueryPath path) where T : IHolon
    {
        var index = !path.IsMetadata && path.Segments.Count == 1
            ? query.Columns.ToList().FindIndex(column => column.Alias == path.Segments[0] && column.ValueType == path.ValueType) : -1;
        if (index < 0) throw new NotSupportedException("Grouped filter must bind an exact selected scalar.");
        var column = query.Columns[index];
        BsonValue value;
        if (column.KeyIndex >= 0) value = "$_id.k" + column.KeyIndex;
        else if (column.Operation is CosmicScalarOperation.Count or CosmicScalarOperation.LongCount) value = "$inputs";
        else if (column.Operation is CosmicScalarOperation.Min or CosmicScalarOperation.Max) value = "$a" + index + ".k";
        else if (column.Operation == CosmicScalarOperation.Sum) value = GroupedSumValue(index);
        else if (column.Operation == CosmicScalarOperation.Average) value = "$cqlGroupAverage" + index;
        else throw new NotSupportedException("Exact normalized Average filtering is not enrolled.");
        return Op("$ifNull", value, Literal(BsonNull.Value));
    }
    private static BsonValue GroupFilterConstant(CosmicQueryLiteral literal)
        => literal.Kind == CosmicQueryScalarKind.Guid ? ComparableGuid(Constant(literal))
            : literal.Kind == CosmicQueryScalarKind.Decimal ? Literal(new BsonDecimal128(new Decimal128((decimal)literal.Value)))
            : literal.Kind == CosmicQueryScalarKind.Double ? Literal(new BsonDouble((double)literal.Value)) : Constant(literal);

    private static BsonValue GroupFilterComparison(BsonValue left, BsonValue right, CosmicQueryComparisonOperator operation)
    {
        var symbol = operation switch
        {
            CosmicQueryComparisonOperator.Equal => "$eq", CosmicQueryComparisonOperator.NotEqual => "$ne",
            CosmicQueryComparisonOperator.GreaterThan => "$gt", CosmicQueryComparisonOperator.GreaterThanOrEqual => "$gte",
            CosmicQueryComparisonOperator.LessThan => "$lt", CosmicQueryComparisonOperator.LessThanOrEqual => "$lte",
            _ => throw new NotSupportedException("Unknown grouped comparison operator.")
        };
        var raw = Op(symbol, left, right);
        return operation is CosmicQueryComparisonOperator.Equal or CosmicQueryComparisonOperator.NotEqual ? raw
            : Choose(Op("$and", Op("$ne", left, Literal(BsonNull.Value)), Op("$ne", right, Literal(BsonNull.Value))), raw, Literal(false));
    }
    private static BsonValue GroupFilterPredicate<T, TKey, TResult>(CosmicGroupedSelection<T, TKey, TResult> query,
        CosmicQueryPredicate predicate) where T : IHolon
    {
        if (predicate is CosmicQueryBoolean boolean) return Literal(boolean.Value);
        if (predicate is CosmicQueryLogical logical)
            return Op(logical.IsAnd ? "$and" : "$or", GroupFilterPredicate(query, logical.Left), GroupFilterPredicate(query, logical.Right));
        if (predicate is CosmicQueryNot not) return Op("$not", GroupFilterPredicate(query, not.Operand));
        if (predicate is CosmicQueryComparison comparison)
            return GroupFilterComparison(GroupFilterValue(query, comparison.Path), GroupFilterConstant(comparison.Literal), comparison.Operator);
        if (predicate is CosmicQueryFieldComparison fields)
            return GroupFilterComparison(GroupFilterValue(query, fields.Left), GroupFilterValue(query, fields.Right), fields.Operator);
        if (predicate is CosmicQueryMembership membership)
            return membership.Values.Count == 0 ? Literal(false) : Op("$or", membership.Values.Select(literal =>
                GroupFilterComparison(GroupFilterValue(query, membership.Path), GroupFilterConstant(literal), CosmicQueryComparisonOperator.Equal)).ToArray());
        if (predicate is CosmicQueryStringMatch match)
        {
            var value = GroupFilterValue(query, match.Path); var text = Literal(match.Value);
            BsonValue test;
            if (match.Operator is CosmicQueryStringOperator.Contains or CosmicQueryStringOperator.StartsWith)
                test = Op(match.Operator == CosmicQueryStringOperator.Contains ? "$gte" : "$eq", Op("$indexOfCP", value, text), Literal(0));
            else if (match.Operator == CosmicQueryStringOperator.EndsWith)
            {
                var length = new BsonDocument("$strLenCP", value);
                var suffix = new BsonDocument("$strLenCP", text);
                test = Choose(Op("$gte", length, suffix), Op("$eq", Op("$substrCP", value, Op("$subtract", length, suffix), suffix), text), Literal(false));
            }
            else throw new NotSupportedException("Unknown grouped string operation.");
            return Choose(Op("$ne", value, Literal(BsonNull.Value)), test, Literal(false));
        }
        throw new NotSupportedException("Unsupported grouped predicate.");
    }
}
