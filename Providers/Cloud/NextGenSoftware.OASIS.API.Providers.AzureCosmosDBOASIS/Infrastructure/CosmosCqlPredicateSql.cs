using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Azure.Cosmos;
using Newtonsoft.Json;
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
            CosmicQueryMetadataTest test => MetadataTest(test),
            _ => throw new NotSupportedException("Cosmos CQL predicate has not been admitted.")
        };
    }

    private string Compare(CosmicQueryComparison comparison)
    {
        var path = comparison.Path;
        if (path.IsMetadata) return MetadataCompare(comparison);
        if (path.Segments.Count != 1)
            throw new NotSupportedException("Cosmos CQL metadata/path semantics require separate admission.");

        var name = path.Segments[0];
        var expected = name switch
        {
            "Name" or "Description" => typeof(string),
            "IsActive" or "IsPublic" => typeof(bool),
            "Version" => typeof(int),
            "Id" or "ParentHolonId" or "VersionId" or "PreviousVersionId" or
                "CreatedByAvatarId" or "ModifiedByAvatarId" => typeof(Guid),
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
        _parameters.Add((parameter, expected == typeof(Guid)
            ? ((Guid)comparison.Literal.Value).ToString("D") : comparison.Literal.Value));
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
        var guard = expected == typeof(string) || expected == typeof(Guid) ? "IS_STRING" : expected == typeof(bool) ? "IS_BOOL" : "IS_NUMBER";
        var normalized = "IIF(" + guard + "(" + field + "), " + field + " " + symbol + " " + parameter + ", false)";
        return operation == CosmicQueryComparisonOperator.NotEqual ? "(NOT " + normalized + ")" : normalized;
    }

    private string Bind(object value)
    {
        var name = "@cql" + _parameters.Count;
        _parameters.Add((name, value));
        return name;
    }

    private static (string Field, string Exists) MetadataPath(IReadOnlyList<string> segments)
    {
        if (segments.Count < 1 || segments.Count > 16) throw new NotSupportedException("Bounded metadata path required.");
        var field = "c[\"MetaData\"]";
        var ancestors = new List<string> { "IS_OBJECT(" + field + ")" };
        for (var i = 0; i < segments.Count; i++)
        {
            field += "[" + JsonConvert.SerializeObject(segments[i]) + "]";
            if (i + 1 < segments.Count) ancestors.Add("IS_OBJECT(" + field + ")");
        }
        ancestors.Add("IS_DEFINED(" + field + ")");
        return (field, "(" + string.Join(" AND ", ancestors) + ")");
    }

    private string MetadataTest(CosmicQueryMetadataTest test)
    {
        var (field, exists) = MetadataPath(test.Segments);
        return test.Kind == CosmicQueryMetadataTestKind.Exists ? exists : "(" + exists + " AND IS_NULL(" + field + "))";
    }

    private string MetadataCompare(CosmicQueryComparison comparison)
    {
        var path = comparison.Path;
        var type = Nullable.GetUnderlyingType(path.ValueType) ?? path.ValueType;
        if (type != typeof(string) && type != typeof(bool) ||
            comparison.Operator is not CosmicQueryComparisonOperator.Equal and not CosmicQueryComparisonOperator.NotEqual)
            throw new NotSupportedException("Cosmos metadata comparison domain is not admitted.");
        var (field, exists) = MetadataPath(path.Segments);
        var nullable = type == typeof(string) || Nullable.GetUnderlyingType(path.ValueType) != null;
        var guard = MetadataDomainGuard(path, field);
        var unequal = comparison.Operator == CosmicQueryComparisonOperator.NotEqual;
        if (comparison.Literal.Kind == CosmicQueryScalarKind.Null)
        {
            if (!nullable) throw new NotSupportedException("Non-nullable metadata cannot be compared with null.");
            return "(" + exists + " AND " + (unequal ? guard : "IS_NULL(" + field + ")") + ")";
        }
        if (comparison.Literal.Kind != path.ScalarKind)
            throw new NotSupportedException("Cosmos metadata literal type differs from its declared domain.");
        var valueTest = "IIF(" + guard + ", " + field + (unequal ? " != " : " = ") + Bind(comparison.Literal.Value) + ", false)";
        if (unequal && nullable) valueTest = "(" + valueTest + " OR IS_NULL(" + field + "))";
        return "(" + exists + " AND " + valueTest + ")";
    }

    private string MetadataDomainGuard(CosmicQueryPath path, string field)
    {
        var type = Nullable.GetUnderlyingType(path.ValueType) ?? path.ValueType;
        var guard = (type == typeof(string) ? "IS_STRING(" : "IS_BOOL(") + field + ")";
        if (type == typeof(string))
        {
            // Exact numeric literals are native strings on the wire. Exclude
            // their framed structural paths from the canonical string domain.
            var steps = new[] { "MetaData" }.Concat(path.Segments).ToArray();
            var matches = new List<string> { "ARRAY_LENGTH(cqlNumber[\"path\"]) = " + steps.Length };
            for (var i = 0; i < steps.Length; i++)
                matches.Add("cqlNumber[\"path\"][" + i + "] = " + Bind(steps[i]));
            guard = "(" + guard + " AND NOT EXISTS(SELECT VALUE cqlNumber FROM cqlNumber IN c[\"_oasis_exact_numbers\"][\"values\"] WHERE " + string.Join(" AND ", matches) + "))";
        }
        return guard;
    }

    internal string ProjectionCell(CosmicQueryPath path)
    {
        string field, exists, guard;
        if (path.IsMetadata)
        {
            (field, exists) = MetadataPath(path.Segments);
            guard = MetadataDomainGuard(path, field);
        }
        else
        {
            // Projection admission owns the exact canonical whitelist/type.
            field = "c[" + JsonConvert.SerializeObject(path.Segments.Single()) + "]";
            exists = "IS_DEFINED(" + field + ")";
            guard = path.ValueType == typeof(bool) ? "IS_BOOL(" + field + ")"
                : path.ValueType == typeof(int) ? "IS_NUMBER(" + field + ")" : "IS_STRING(" + field + ")";
        }
        var state = "IIF(NOT " + exists + ", 0, IIF(IS_NULL(" + field + "), 1, IIF(" + guard + ", 2, 3)))";
        return "{\"state\": " + state + ", \"value\": " + field + "}";
    }
}
