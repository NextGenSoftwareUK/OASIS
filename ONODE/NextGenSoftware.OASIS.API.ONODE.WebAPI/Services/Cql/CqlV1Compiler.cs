using System;
using System.Linq;
using System.Linq.Expressions;
using System.Text.Json;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;

public static class CqlV1Compiler
{
    public static bool Supported(ProviderType type) => type is ProviderType.MongoDBOASIS or ProviderType.SQLLiteDBOASIS or ProviderType.Neo4jOASIS;

    public static ProviderType? RequestedProvider(string name)
    {
        if (name == null) return null;
        if (name.Length > 40 || !Enum.TryParse<ProviderType>(name, false, out var type) ||
            type.ToString() != name || !Supported(type))
            throw new ArgumentException("Select MongoDBOASIS, SQLLiteDBOASIS or Neo4jOASIS, or omit Provider for the current default.");
        return type;
    }

    public static CosmicQuery<Holon> Compile(CqlV1Request request, Guid actor, ProviderType? provider)
    {
        if (actor == Guid.Empty || provider.HasValue && !Supported(provider.Value)) throw new ArgumentException("Authenticated actor and concrete v1 provider required.");
        if (request == null || request.Filters == null || request.AnyOf == null ||
            request.Filters.Length + request.AnyOf.Length > 12 || request.Skip is < 0 or > 1000 || request.Take is < 1 or > 100)
            throw new ArgumentException("Use at most 12 filters, Skip 0–1000 and Take 1–100.");

        // Mandatory access restriction enters the SAME native plan before filters, ordering
        // and paging. Shared-index metadata cannot grant access; Wizard does not bypass this.
        var query = Add(CosmicQuery<Holon>.Create(provider.HasValue ? CosmicQueryScope.Named(provider.Value).Result : CosmicQueryScope.CurrentDefault),
            actor);
        if(request.Graph!=null)
        {
            var graph=request.Graph;
            if(graph.MaxChildDepth is <1 or >CqlV1Projection.MaximumDepth ||
                graph.MaximumNodes is <1 or >1000 || graph.MaximumPayloadBytes is <1 or >CqlV1Endpoint.MaximumResponseBytes ||
                graph.ChildType==null || graph.ChildType.Length>64 ||
                !Enum.TryParse<HolonType>(graph.ChildType,false,out var childType) || childType.ToString()!=graph.ChildType || !Enum.IsDefined(childType) ||
                graph.Consistency==null || graph.Consistency.Length>32 ||
                !Enum.TryParse<CosmicQueryGraphConsistency>(graph.Consistency,false,out var consistency) ||
                consistency.ToString()!=graph.Consistency || !Enum.IsDefined(consistency))
                throw new ArgumentException("Graph requires depth1–8, nodes1–1000, payload1–1048576 and canonical child type/consistency names.");
            var options=CosmicQueryGraphOptions.Create(consistency,graph.Recursive,graph.MaxChildDepth,
                childType,graph.MaximumNodes,graph.MaximumPayloadBytes);
            if(options.IsError)throw new ArgumentException(options.Message);
            var expanded=query.WithChildren(options.Result);
            if(expanded.IsError)throw new ArgumentException(expanded.Message);
            query=expanded.Result;
        }
        var parameter = Expression.Parameter(typeof(Holon), "h");
        foreach (var filter in request.Filters)
            query = Add(query, parameter, Predicate(filter, parameter));

        Expression alternatives = null;
        foreach (var filter in request.AnyOf)
        {
            var condition = Predicate(filter, parameter);
            alternatives = alternatives == null ? condition : Expression.OrElse(alternatives, condition);
        }
        if (alternatives != null) query = Add(query, parameter, alternatives);

        var ordered = request.OrderBy switch
        {
            "Name" => request.Descending ? query.OrderByDescending(h => h.Name) : query.OrderBy(h => h.Name),
            "Version" => request.Descending ? query.OrderByDescending(h => h.Version) : query.OrderBy(h => h.Version),
            "Id" => request.Descending ? query.OrderByDescending(h => h.Id) : query.OrderBy(h => h.Id),
            _ => throw new ArgumentException("OrderBy accepts Name, Version or Id.")
        };
        if (ordered.IsError) throw new ArgumentException(ordered.Message);
        var page = ordered.ThenBy(h => h.Id).Skip(request.Skip).Take(request.Take);
        if (page.IsError) throw new ArgumentException(page.Message);
        return page.Result;
    }

    private static CosmicQuery<Holon> Add(CosmicQuery<Holon> query, Guid actor)
    {
        var access=CosmicQueryReadAccess.OwnerOrPublic(actor);
        if(access.IsError)throw new ArgumentException(access.Message);
        var secured = query.WithReadAccess(access.Result);
        if (secured.IsError) throw new ArgumentException(secured.Message);
        return secured.Result;
    }

    private static CosmicQuery<Holon> Add(CosmicQuery<Holon> query, ParameterExpression parameter, Expression predicate)
    {
        var result = query.Where(Expression.Lambda<Func<Holon, bool>>(predicate, parameter));
        if (result.IsError) throw new ArgumentException(result.Message);
        return result.Result;
    }

    private static Expression Predicate(CqlV1Filter filter, ParameterExpression parameter)
    {
        if (filter == null || !Enum.IsDefined(filter.Operator) || !Enum.IsDefined(filter.Scalar))
            throw new ArgumentException("Defined filter operator and scalar required.");
        Expression path;
        if (filter.MetadataPath != null)
        {
            if (filter.Field != null || filter.MetadataPath.Length is < 1 or > 4 ||
                filter.MetadataPath.Any(s => string.IsNullOrEmpty(s) || s.Length > 64 || s.Any(char.IsControl)))
                throw new ArgumentException("Use a metadata path of one to four bounded segments, without Field.");
            var segments = Expression.NewArrayInit(typeof(string), filter.MetadataPath.Select(s => Expression.Constant(s)));
            if (filter.Operator is CqlV1Operator.Exists or CqlV1Operator.IsNull)
            {
                if (filter.Value.ValueKind != JsonValueKind.Undefined) throw new ArgumentException("Existence/null tests do not accept Value.");
                return Expression.Call(typeof(CosmicQueryMetadata), filter.Operator == CqlV1Operator.Exists ? "Exists" : "IsNull",
                    null, parameter, segments);
            }
            var type = filter.Scalar switch
            {
                CqlV1Scalar.String => typeof(string), CqlV1Scalar.Integer => typeof(long),
                CqlV1Scalar.Boolean => typeof(bool), _ => typeof(Guid)
            };
            path = Expression.Call(typeof(CosmicQueryMetadata), "Value", new[] { type }, parameter, segments);
        }
        else
        {
            if (filter.Field is not ("Id" or "Name" or "Description" or "Version" or "IsActive" or "ParentHolonId"))
                throw new ArgumentException("Unsupported canonical query field.");
            if (filter.Operator is CqlV1Operator.Exists or CqlV1Operator.IsNull) throw new ArgumentException("Existence/null tests require MetadataPath.");
            path = Expression.Property(parameter, filter.Field);
        }

        object value;
        var json = filter.Value;
        if (path.Type == typeof(string) && json.ValueKind == JsonValueKind.String && json.GetString().Length <= 512) value = json.GetString();
        else if (path.Type == typeof(bool) && json.ValueKind is JsonValueKind.True or JsonValueKind.False) value = json.GetBoolean();
        else if (path.Type == typeof(long) && json.ValueKind == JsonValueKind.Number && json.TryGetInt64(out var number)) value = number;
        else if (path.Type == typeof(int) && json.ValueKind == JsonValueKind.Number && json.TryGetInt32(out var integer)) value = integer;
        else if (path.Type == typeof(Guid) && json.ValueKind == JsonValueKind.String && Guid.TryParseExact(json.GetString(), "D", out var guid)) value = guid;
        else throw new ArgumentException("Value must match the exact bounded scalar type.");

        var literal = Expression.Constant(value, path.Type);
        if (filter.Operator is CqlV1Operator.Contains or CqlV1Operator.StartsWith or CqlV1Operator.EndsWith)
        {
            if (path.Type != typeof(string)) throw new ArgumentException("String operation requires a string field.");
            // Prefix/suffix single-argument overloads use the current culture. CQL's
            // portable native contract requires ordinal semantics explicitly.
            var method = typeof(string).GetMethod(filter.Operator.ToString(), new[] { typeof(string), typeof(StringComparison) });
            return Expression.Call(path, method, literal, Expression.Constant(StringComparison.Ordinal));
        }
        var node = filter.Operator switch
        {
            CqlV1Operator.Equal => ExpressionType.Equal, CqlV1Operator.NotEqual => ExpressionType.NotEqual,
            CqlV1Operator.GreaterThan => ExpressionType.GreaterThan, CqlV1Operator.GreaterThanOrEqual => ExpressionType.GreaterThanOrEqual,
            CqlV1Operator.LessThan => ExpressionType.LessThan, CqlV1Operator.LessThanOrEqual => ExpressionType.LessThanOrEqual,
            _ => throw new ArgumentException("Unsupported filter operation.")
        };
        return Expression.MakeBinary(node, path, literal);
    }
}
