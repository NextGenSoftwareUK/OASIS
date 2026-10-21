using System;
using System.Collections.Generic;
using System.Linq;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS;

internal sealed class NeoCqlTranslator
{
    internal readonly Dictionary<string, object> Parameters = new(StringComparer.Ordinal);
    private string Bind(object value)
    {
        if (Parameters.Count >= 900) throw new NotSupportedException("Neo4j CQL exceeds its 900-parameter budget.");
        var name = "cql" + Parameters.Count; Parameters.Add(name, value); return "$" + name;
    }
    private static bool NullablePath(CosmicQueryPath path)
        => !path.ValueType.IsValueType || Nullable.GetUnderlyingType(path.ValueType) != null;
    private (string Value, string Guard, string Null) Operand(CosmicQueryPath path)
    {
        if (!path.IsMetadata)
        {
            var name = path.Segments.Single();
            if (!new[] { "Id", "Name", "Description", "Version", "IsActive", "IsPublic", "ParentHolonId", "VersionId",
                "PreviousVersionId", "CreatedByAvatarId", "ModifiedByAvatarId" }.Contains(name, StringComparer.Ordinal))
                throw new NotSupportedException("Neo4j CQL canonical path is unsupported.");
            var column = "p[" + Bind("f_" + name) + "]";
            return (column, "true", column + " IS NULL");
        }
        var key = NeoCqlProjection.PathKey(path.Segments);
        var kind = "p[" + Bind(key + "_kind") + "]";
        var value = "p[" + Bind(key + "_value") + "]";
        var type = Nullable.GetUnderlyingType(path.ValueType) ?? path.ValueType;
        string guard;
        if (type == typeof(string)) guard = kind + "='string'";
        else if (type == typeof(bool)) guard = kind + "='boolean'";
        else if (type == typeof(int) || type == typeof(long))
        {
            guard = kind + "='integer'";
            if (type == typeof(int)) guard += " AND " + value + ">=-2147483648 AND " + value + "<=2147483647";
        }
        else throw new NotSupportedException("Neo4j CQL metadata scalar domain is unsupported.");
        return (value, "(" + guard + ")", kind + "='null'");
    }
    private static string Symbol(CosmicQueryComparisonOperator operation) => operation switch {
        CosmicQueryComparisonOperator.Equal => "=", CosmicQueryComparisonOperator.NotEqual => "<>",
        CosmicQueryComparisonOperator.GreaterThan => ">", CosmicQueryComparisonOperator.GreaterThanOrEqual => ">=",
        CosmicQueryComparisonOperator.LessThan => "<", CosmicQueryComparisonOperator.LessThanOrEqual => "<=",
        _ => throw new NotSupportedException("Unknown Neo4j CQL comparison.") };
    private static object Literal(CosmicQueryLiteral literal) => literal.Kind switch {
        CosmicQueryScalarKind.String or CosmicQueryScalarKind.Boolean or CosmicQueryScalarKind.Int64 => literal.Value,
        CosmicQueryScalarKind.Guid => ((Guid)literal.Value).ToString("D"),
        _ => throw new NotSupportedException("Neo4j CQL literal domain is unsupported.") };
    private static string Total(string predicate) => "coalesce((" + predicate + "),false)";

    internal string Translate(CosmicQueryPredicate predicate)
    {
        switch (predicate)
        {
            case CosmicQueryBoolean constant: return constant.Value ? "true" : "false";
            case CosmicQueryLogical logical:
                return "(" + Translate(logical.Left) + (logical.IsAnd ? " AND " : " OR ") + Translate(logical.Right) + ")";
            case CosmicQueryNot not: return "(NOT " + Translate(not.Operand) + ")";
            case CosmicQueryFieldComparison fields:
                var left = Operand(fields.Left); var right = Operand(fields.Right);
                var leftValid = NullablePath(fields.Left) ? "(" + left.Guard + " OR " + left.Null + ")" : left.Guard;
                var rightValid = NullablePath(fields.Right) ? "(" + right.Guard + " OR " + right.Null + ")" : right.Guard;
                string fieldTest;
                if (fields.Operator == CosmicQueryComparisonOperator.Equal || fields.Operator == CosmicQueryComparisonOperator.NotEqual)
                {
                    var equal = "(" + Total(left.Null + " AND " + right.Null) + " OR "
                        + Total(left.Value + "=" + right.Value) + ")";
                    fieldTest = fields.Operator == CosmicQueryComparisonOperator.Equal ? equal : "NOT " + equal;
                }
                else fieldTest = Total(left.Value + Symbol(fields.Operator) + right.Value);
                return Total(leftValid + " AND " + rightValid + " AND (" + fieldTest + ")");
            case CosmicQueryMetadataTest test:
                var kind = "p[" + Bind(NeoCqlProjection.PathKey(test.Segments) + "_kind") + "]";
                return test.Kind == CosmicQueryMetadataTestKind.Exists ? "(" + kind + " IS NOT NULL)" : Total(kind + "='null'");
            case CosmicQueryComparison comparison:
                var operand = Operand(comparison.Path);
                if (comparison.Literal.Kind == CosmicQueryScalarKind.Null)
                {
                    if (!NullablePath(comparison.Path)) throw new NotSupportedException("Nonnullable CQL path cannot compare with null.");
                    return comparison.Operator switch {
                        CosmicQueryComparisonOperator.Equal => Total(operand.Null),
                        CosmicQueryComparisonOperator.NotEqual => Total(operand.Guard + " AND NOT (" + operand.Null + ")"),
                        _ => throw new NotSupportedException("CQL null ordering is unsupported.") };
                }
                var compared = operand.Guard + " AND " + operand.Value + Symbol(comparison.Operator) + Bind(Literal(comparison.Literal));
                if (comparison.Operator == CosmicQueryComparisonOperator.NotEqual && NullablePath(comparison.Path))
                    compared = "(" + compared + ") OR (" + operand.Null + ")";
                return Total(compared);
            case CosmicQueryStringMatch match:
                var text = Operand(match.Path);
                var operation = match.Operator switch {
                    CosmicQueryStringOperator.Contains => " CONTAINS ", CosmicQueryStringOperator.StartsWith => " STARTS WITH ",
                    CosmicQueryStringOperator.EndsWith => " ENDS WITH ", _ => throw new NotSupportedException("Unknown CQL string operation.") };
                return Total(text.Guard + " AND " + text.Value + operation + Bind(match.Value));
            case CosmicQueryMembership membership:
                var member = Operand(membership.Path);
                var values = membership.Values.Where(value => value.Kind != CosmicQueryScalarKind.Null).Select(Literal).ToArray();
                var included = member.Guard + " AND " + member.Value + " IN " + Bind(values);
                if (NullablePath(membership.Path) && membership.Values.Any(value => value.Kind == CosmicQueryScalarKind.Null))
                    included = "(" + included + ") OR (" + member.Null + ")";
                return Total(included);
            default: throw new NotSupportedException("Neo4j CQL predicate has no admitted native translation.");
        }
    }
}
