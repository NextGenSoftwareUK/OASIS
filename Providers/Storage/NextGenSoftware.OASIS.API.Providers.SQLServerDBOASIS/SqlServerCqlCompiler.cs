using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Microsoft.Data.SqlClient;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.SQLServerDBOASIS;

/// <summary>Bind shared bounded-key algebra to this exact storage authority.</summary>
public static class SqlServerCqlCompiler
{
    public static CosmicQueryCapabilities Capabilities { get; } = new(
        CosmicQueryFeatures.BooleanConstant | CosmicQueryFeatures.Membership | CosmicQueryFeatures.And |
        CosmicQueryFeatures.Or | CosmicQueryFeatures.Paging | CosmicQueryFeatures.Metadata | CosmicQueryFeatures.StringContains,
        new[] {
            new CosmicQueryFieldCapability("Id", typeof(Guid), CosmicQueryComparisonOperator.Equal),
            new CosmicQueryFieldCapability("Name", typeof(string), new[] { CosmicQueryComparisonOperator.Equal }, new[] { CosmicQueryStringOperator.Contains }),
            new CosmicQueryFieldCapability("Description", typeof(string), new[] { CosmicQueryComparisonOperator.Equal }, new[] { CosmicQueryStringOperator.Contains })
        }, new[] { new CosmicQueryMetadataCapability(typeof(string), new[] { CosmicQueryComparisonOperator.Equal }) });

    public static OASISResult<IReadOnlyList<Guid>> Compile<T>(CosmicQuery<T> query, ProviderType authority) where T : IHolon
        => BoundedIdentityCqlCompiler.Compile(query, authority, ProviderType.SQLServerDBOASIS);
    public sealed record NativeCommand(SqlCommand Command, int SourceCount);

    /// <summary>One native command, always restricted to the trusted catalogue.
    /// OPENJSON preserves full strings instead of JSON_VALUE's4000-character limit.</summary>
    public static OASISResult<NativeCommand> CompileNative<T>(CosmicQuery<T> query, ProviderType authority) where T : IHolon
    {
        OASISResult<NativeCommand> Error(string code, string message) => new() {
            IsError = true, ErrorCount = 1, ErrorCode = code, Message = message };
        if (query == null) return Error("CQL_CAPABILITY_UNSUPPORTED", "A captured query is required.");
        var admitted = Capabilities.Admit(query);
        if (admitted.IsError) return Error(admitted.ErrorCode, admitted.Message);
        if (authority != ProviderType.SQLServerDBOASIS || query.ReadAccess?.Authority != authority)
            return Error("CQL_ACCESS_INVALID", "An explicit SQL Server-authorized catalogue is required.");
        var source = CosmicQuery<T>.Create(query.Scope).WithReadAccess(query.ReadAccess);
        if (source.IsError) return Error(source.ErrorCode, source.Message);
        var keys = BoundedIdentityCqlCompiler.Compile(source.Result, authority, ProviderType.SQLServerDBOASIS);
        if (keys.IsError) return Error(keys.ErrorCode, keys.Message);
        var command = new SqlCommand();
        try {
            var number = 0;
            string Parameter(object value, SqlDbType type) {
                if (++number > 1024) throw new ArgumentException("The native parameter budget is exceeded.");
                var name = "@cql" + number;
                var parameter = command.Parameters.Add(name, type);
                if (type == SqlDbType.NVarChar) parameter.Size = -1;
                parameter.Value = value;
                return name;
            }
            const string binary = " COLLATE Latin1_General_100_BIN2";
            string Equal(string left, string right) => "(" + left + binary + "=" + right + binary +
                " AND DATALENGTH(" + left + ")=DATALENGTH(" + right + "))";
            string TextTest(CosmicQueryPath path, string value, bool contains) {
                if (value.Contains('\0') || contains && value.Length > 8000)
                    throw new ArgumentException("SQL Server Contains requires at most8000 UTF16 units and strings cannot contain NUL.");
                // The canonical SQL Server codec uses camelCase fields but preserves
                // dictionary keys. Each metadata parent must be an object, not an array.
                var segments = path.IsMetadata ? new[] { "metaData" }.Concat(path.Segments).ToArray()
                    : new[] { path.Segments.Single() == "Name" ? "name" : "description" };
                if (segments.Any(segment => segment.Contains('\0')))
                    throw new ArgumentException("SQL Server metadata keys cannot contain NUL.");
                var alias = "j" + number + "_";
                var from = "OPENJSON(DataJson) AS " + alias + "0";
                var conditions = new List<string>();
                for (var index = 0; index < segments.Length; index++) {
                    var current = alias + index;
                    if (index != 0) {
                        var parent = alias + (index - 1);
                        from += " CROSS APPLY OPENJSON(CASE WHEN " + parent + ".[type]=5 THEN " + parent + ".[value] ELSE N'{}' END) AS " + current;
                    }
                    conditions.Add(Equal(current + ".[key]", Parameter(segments[index], SqlDbType.NVarChar)));
                }
                var last = alias + (segments.Length - 1);
                conditions.Add(last + ".[type]=1");
                var argument = Parameter(value, SqlDbType.NVarChar);
                conditions.Add(contains ? "CHARINDEX(" + argument + binary + "," + last + ".[value]" + binary + ")>0"
                    : Equal(last + ".[value]", argument));
                return "EXISTS (SELECT 1 FROM " + from + " WHERE " + string.Join(" AND ", conditions) + ")";
            }
            string Predicate(CosmicQueryPredicate predicate) => predicate switch {
                CosmicQueryBoolean boolean => boolean.Value ? "1=1" : "1=0",
                CosmicQueryLogical logical => "(" + Predicate(logical.Left) + (logical.IsAnd ? " AND " : " OR ") + Predicate(logical.Right) + ")",
                CosmicQueryComparison comparison when comparison.Path.ValueType == typeof(Guid) && comparison.Path.Segments.Single() == "Id"
                    => "Id=" + Parameter(comparison.Literal.Value, SqlDbType.UniqueIdentifier),
                CosmicQueryComparison comparison when comparison.Path.ValueType == typeof(string)
                    => TextTest(comparison.Path, (string)comparison.Literal.Value, false),
                CosmicQueryStringMatch match => TextTest(match.Path, match.Value, true),
                CosmicQueryMembership membership when !membership.Path.IsMetadata && membership.Path.Segments.Single() == "Id"
                    => membership.Values.Count == 0 ? "1=0" : "Id IN (" + string.Join(",", membership.Values.Select(value => Parameter(value.Value, SqlDbType.UniqueIdentifier))) + ")",
                _ => throw new ArgumentException("Only admitted SQL Server native content predicates are supported.")
            };
            var conditions = query.BusinessPredicates.Select(Predicate).ToArray();
            var names = keys.Result.Select((id, index) => "@id" + index).ToArray();
            var catalogue = names.Length == 0 ? "1=0" : "Id IN (" + string.Join(",", names) + ")";
            command.CommandText = "SELECT Id,DataJson,IsDeleted FROM OASISHolons WHERE (" + catalogue + ") AND IsDeleted=0" +
                (conditions.Length == 0 ? "" : " AND (" + string.Join(" AND ", conditions) + ")") +
                " ORDER BY CONVERT(char(36),Id) COLLATE Latin1_General_100_BIN2 OFFSET @skip ROWS FETCH NEXT @take ROWS ONLY";
            for (var index = 0; index < names.Length; index++) command.Parameters.Add(names[index], SqlDbType.UniqueIdentifier).Value = keys.Result[index];
            command.Parameters.Add("@take", SqlDbType.Int).Value = query.Limit ?? keys.Result.Count;
            command.Parameters.Add("@skip", SqlDbType.Int).Value = query.Offset;
            return new(new NativeCommand(command, keys.Result.Count));
        }
        catch (ArgumentException error) {
            command.Dispose();
            return Error("CQL_CAPABILITY_UNSUPPORTED", error.Message);
        }
    }

}
