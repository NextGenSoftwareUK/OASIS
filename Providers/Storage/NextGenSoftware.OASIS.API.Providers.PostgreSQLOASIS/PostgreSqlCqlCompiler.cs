using System;
using System.Collections.Generic;
using System.Linq;
using Npgsql;
using NpgsqlTypes;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.PostgreSQLOASIS;

/// <summary>Bind shared bounded-key algebra to this exact storage authority.</summary>
public static class PostgreSqlCqlCompiler
{
    public static CosmicQueryCapabilities Capabilities { get; } = new(
        CosmicQueryFeatures.BooleanConstant | CosmicQueryFeatures.Membership | CosmicQueryFeatures.And |
        CosmicQueryFeatures.Or | CosmicQueryFeatures.Paging | CosmicQueryFeatures.Metadata | CosmicQueryFeatures.StringContains,
        new[] {
            new CosmicQueryFieldCapability("Id", typeof(Guid), CosmicQueryComparisonOperator.Equal),
            new CosmicQueryFieldCapability("Name", typeof(string), new[] { CosmicQueryComparisonOperator.Equal }, new[] { CosmicQueryStringOperator.Contains }),
            new CosmicQueryFieldCapability("Description", typeof(string), new[] { CosmicQueryComparisonOperator.Equal }, new[] { CosmicQueryStringOperator.Contains })
        }, new[] { new CosmicQueryMetadataCapability(typeof(string), new[] { CosmicQueryComparisonOperator.Equal }) });

    // PostgreSQL-compatible products retain their previously admitted identity-only
    // scope. New content semantics require independent provider proof.
    public static CosmicQueryCapabilities CapabilitiesFor(ProviderType authority)
        => authority == ProviderType.PostgreSQLOASIS ? Capabilities : BoundedIdentityCqlCompiler.Capabilities;

    public static OASISResult<IReadOnlyList<Guid>> Compile<T>(CosmicQuery<T> query, ProviderType authority) where T : IHolon
        => BoundedIdentityCqlCompiler.Compile(query, authority, ProviderType.PostgreSQLOASIS);
    /// <summary>Compile native predicates over an explicit authorized catalogue.
    /// Values and JSON path segments are parameters; no stored rows are filtered in managed code.</summary>
    public static OASISResult<NpgsqlCommand> CompileNative<T>(CosmicQuery<T> query, ProviderType authority, ProviderType requiredAuthority = ProviderType.PostgreSQLOASIS) where T : IHolon
    {
        OASISResult<NpgsqlCommand> Error(string code, string message) => new() {
            IsError = true, ErrorCount = 1, ErrorCode = code, Message = message };
        if (query == null) return Error("CQL_CAPABILITY_UNSUPPORTED", "A captured query is required.");
        var admitted = CapabilitiesFor(requiredAuthority).Admit(query);
        if (admitted.IsError) return Error(admitted.ErrorCode, admitted.Message);
        if (authority != requiredAuthority || query.ReadAccess?.Authority != authority)
            return Error("CQL_ACCESS_INVALID", "An explicit PostgreSQL-authorized catalogue is required.");
        var source = CosmicQuery<T>.Create(query.Scope).WithReadAccess(query.ReadAccess);
        if (source.IsError) return Error(source.ErrorCode, source.Message);
        var keys = BoundedIdentityCqlCompiler.Compile(source.Result, authority, requiredAuthority);
        if (keys.IsError) return Error(keys.ErrorCode, keys.Message);
        var command = new NpgsqlCommand();
        try {
            var number = 0;
            string Parameter(object value, NpgsqlDbType type) {
                if (++number > 1024) throw new ArgumentException("The native parameter budget is exceeded.");
                var name = "cql" + number;
                command.Parameters.AddWithValue(name, type, value);
                return "@" + name;
            }
            string Json(CosmicQueryPath path) {
                var segments = path.IsMetadata ? new[] { "MetaData" }.Concat(path.Segments).ToArray() : path.Segments.ToArray();
                if (segments.Any(segment => segment.Contains('\0')))
                    throw new ArgumentException("PostgreSQL JSON paths cannot contain NUL.");
                var value = "(data_json::jsonb #> " + Parameter(segments, NpgsqlDbType.Array | NpgsqlDbType.Text) + ")";
                // A numeric dictionary key must never index a JSON array. Every
                // parent is an object, matching COSMIC metadata path semantics.
                var parents = new List<string> { "jsonb_typeof(data_json::jsonb)='object'" };
                for (var length = 1; length < segments.Length; length++)
                    parents.Add("jsonb_typeof(data_json::jsonb #> " +
                        Parameter(segments.Take(length).ToArray(), NpgsqlDbType.Array | NpgsqlDbType.Text) + ")='object'");
                return "(CASE WHEN " + string.Join(" AND ", parents) + " THEN " + value + " ELSE NULL END)";
            }
            string TextTest(CosmicQueryPath path, string value, bool contains) {
                if (value.Contains('\0')) throw new ArgumentException("PostgreSQL strings cannot contain NUL.");
                var json = Json(path);
                var text = "(" + json + " #>> '{}') COLLATE \"C\"";
                var parameter = Parameter(value, NpgsqlDbType.Text);
                var test = contains ? "strpos(" + text + "," + parameter + ")>0" : text + "=" + parameter + " COLLATE \"C\"";
                return "(jsonb_typeof(" + json + ")='string' AND " + test + ")";
            }
            string Predicate(CosmicQueryPredicate predicate) => predicate switch {
                CosmicQueryBoolean boolean => boolean.Value ? "TRUE" : "FALSE",
                CosmicQueryLogical logical => "(" + Predicate(logical.Left) + (logical.IsAnd ? " AND " : " OR ") + Predicate(logical.Right) + ")",
                CosmicQueryComparison comparison when comparison.Path.ValueType == typeof(Guid) && comparison.Path.Segments.Single() == "Id"
                    => "id=" + Parameter(comparison.Literal.Value, NpgsqlDbType.Uuid),
                CosmicQueryComparison comparison when comparison.Path.ValueType == typeof(string)
                    => TextTest(comparison.Path, (string)comparison.Literal.Value, false),
                CosmicQueryStringMatch match => TextTest(match.Path, match.Value, true),
                CosmicQueryMembership membership when !membership.Path.IsMetadata && membership.Path.Segments.Single() == "Id"
                    => "id=ANY(" + Parameter(membership.Values.Select(value => (Guid)value.Value).ToArray(), NpgsqlDbType.Array | NpgsqlDbType.Uuid) + ")",
                _ => throw new ArgumentException("Only admitted PostgreSQL native content predicates are supported.")
            };
            var conditions = query.BusinessPredicates.Select(Predicate).ToArray();
            command.CommandText = "SELECT id,data_json,is_deleted FROM oasis_holons WHERE id=ANY(@ids) AND is_deleted=FALSE" +
                (conditions.Length == 0 ? "" : " AND (" + string.Join(" AND ", conditions) + ")") +
                " ORDER BY id::text COLLATE \"C\" LIMIT @take OFFSET @skip";
            command.Parameters.AddWithValue("ids", NpgsqlDbType.Array | NpgsqlDbType.Uuid, keys.Result.ToArray());
            command.Parameters.AddWithValue("take", query.Limit ?? keys.Result.Count);
            command.Parameters.AddWithValue("skip", query.Offset);
            return new(command);
        }
        catch (ArgumentException error) {
            command.Dispose();
            return Error("CQL_CAPABILITY_UNSUPPORTED", error.Message);
        }
    }

}
