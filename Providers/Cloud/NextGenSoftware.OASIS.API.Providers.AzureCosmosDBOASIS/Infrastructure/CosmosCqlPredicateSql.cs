using System;
using System.Collections.Generic;
using Microsoft.Azure.Cosmos;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS.Infrastructure;

/// <summary>Canonical predicate translation; provider enrollment requires the remaining read gates.</summary>
internal sealed class CosmosCqlPredicateSql
{
    private readonly List<(string Name, object Value)> _parameters = new();

    internal QueryDefinition BuildCommand(string sql)
    {
        var command = new QueryDefinition(sql);
        foreach (var parameter in _parameters)
            command.WithParameter(parameter.Name, parameter.Value);
        return command;
    }

    internal string Translate(CosmicQueryPredicate predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        return predicate switch
        {
            CosmicQueryBoolean boolean => boolean.Value ? "true" : "false",
            CosmicQueryLogical logical => "(" + Translate(logical.Left) +
                (logical.IsAnd ? " AND " : " OR ") + Translate(logical.Right) + ")",
            CosmicQueryNot not => "(NOT " + Translate(not.Operand) + ")",
            CosmicQueryComparison comparison => Compare(comparison),
            _ => throw new NotSupportedException("Cosmos CQL predicate has not been admitted.")
        };
    }

    private string Compare(CosmicQueryComparison comparison)
    {
        var path = comparison.Path;
        if (path.IsMetadata || path.Segments.Count != 1)
            throw new NotSupportedException("Cosmos CQL metadata/path semantics require separate admission.");

        var name = path.Segments[0];
        var expected = name switch
        {
            "Name" or "Description" => typeof(string),
            "IsActive" or "IsPublic" => typeof(bool),
            "Version" => typeof(int),
            _ => throw new NotSupportedException("Cosmos CQL canonical field has not been admitted: " + name)
        };
        if (path.ValueType != expected)
            throw new NotSupportedException("Cosmos CQL canonical field type differs from its stored contract.");

        var operation = comparison.Operator;
        var equality = operation is CosmicQueryComparisonOperator.Equal or CosmicQueryComparisonOperator.NotEqual;
        if (!equality && expected != typeof(int))
            throw new NotSupportedException("Cosmos CQL ordering comparison has not been admitted for this field.");
        var field = "c[\"" + name + "\"]"; // Fixed whitelist, never caller property text.
        if (comparison.Literal.Kind == CosmicQueryScalarKind.Null)
        {
            if (!equality || expected != typeof(string))
                throw new NotSupportedException("Cosmos CQL null comparison requires a nullable canonical string.");
            var nullSql = "(NOT IS_DEFINED(" + field + ") OR IS_NULL(" + field + "))";
            return operation == CosmicQueryComparisonOperator.NotEqual ? "(NOT " + nullSql + ")" : nullSql;
        }
        if (comparison.Literal.Kind != path.ScalarKind)
            throw new NotSupportedException("Cosmos CQL mixed numeric semantics have not been admitted.");

        var parameter = "@cql" + _parameters.Count;
        _parameters.Add((parameter, comparison.Literal.Value));
        var symbol = operation switch
        {
            CosmicQueryComparisonOperator.Equal or CosmicQueryComparisonOperator.NotEqual => "=",
            CosmicQueryComparisonOperator.GreaterThan => ">",
            CosmicQueryComparisonOperator.GreaterThanOrEqual => ">=",
            CosmicQueryComparisonOperator.LessThan => "<",
            CosmicQueryComparisonOperator.LessThanOrEqual => "<=",
            _ => throw new NotSupportedException("Unknown Cosmos CQL comparison.")
        };
        // Cosmos undefined must become false before Boolean composition. String
        // inequality is the complement of equality, including absent/null values.
        var guard = expected == typeof(string) ? "IS_STRING" : expected == typeof(bool) ? "IS_BOOL" : "IS_NUMBER";
        var normalized = "IIF(" + guard + "(" + field + "), " + field + " " + symbol + " " + parameter + ", false)";
        return operation == CosmicQueryComparisonOperator.NotEqual ? "(NOT " + normalized + ")" : normalized;
    }
}
