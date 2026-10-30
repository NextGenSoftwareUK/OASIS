using System;
using System.Collections.Generic;
using System.Linq;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.DynamoDBOASIS;

/// <summary>Compile bounded identity algebra into one native BatchGetItem key set.
/// No source rows are scanned or evaluated by a managed predicate.</summary>
public static class DynamoCqlCompiler
{
    public static CosmicQueryCapabilities Capabilities { get; } = new(
        CosmicQueryFeatures.BooleanConstant | CosmicQueryFeatures.Membership | CosmicQueryFeatures.And
            | CosmicQueryFeatures.Or | CosmicQueryFeatures.Paging,
        new[] { new CosmicQueryFieldCapability("Id", typeof(Guid), CosmicQueryComparisonOperator.Equal) });

    public static OASISResult<IReadOnlyList<Guid>> Compile<T>(CosmicQuery<T> query, ProviderType authority) where T : IHolon
    {
        try
        {
            if (query == null) throw new ArgumentException("A captured query is required.");
            var admitted = Capabilities.Admit(query);
            if (admitted.IsError) return new() { IsError = true, ErrorCount = 1, ErrorCode = admitted.ErrorCode, Message = admitted.Message };
            if (authority is not (ProviderType.AWSOASIS or ProviderType.DynamoDBOASIS) || query.ReadAccess?.Authority != authority)
                return new() { IsError = true, ErrorCount = 1, ErrorCode = "CQL_ACCESS_INVALID",
                    Message = "A complete server-authorized record catalogue for this exact Dynamo/AWS authority is required." };
            Guid Identity(CosmicQueryLiteral literal) => literal.Kind == CosmicQueryScalarKind.Guid
                && literal.Value is Guid id && id != Guid.Empty ? id : throw new ArgumentException("Nonempty GUID identities are required.");
            HashSet<Guid>? Combine(HashSet<Guid>? left, HashSet<Guid>? right, bool and)
            {
                // null denotes the universal constant, never an unbounded database request.
                if (and) { if (left == null) return right; if (right == null) return left; left.IntersectWith(right); return left; }
                if (left == null || right == null) return null;
                left.UnionWith(right); return left;
            }
            HashSet<Guid>? Select(CosmicQueryPredicate predicate) => predicate switch {
                CosmicQueryBoolean boolean => boolean.Value ? null : new(),
                CosmicQueryLogical logical => Combine(Select(logical.Left), Select(logical.Right), logical.IsAnd),
                CosmicQueryComparison comparison => new() { Identity(comparison.Literal) },
                CosmicQueryMembership membership => membership.Values.Select(Identity).ToHashSet(),
                _ => throw new ArgumentException("Only admitted identity predicates can select native keys.") };
            HashSet<Guid>? keys = null;
            foreach (var predicate in query.Predicates) keys = Combine(keys, Select(predicate), true);
            if (keys == null || keys.Count > 100) throw new ArgumentException("A complete bounded native source of at most 100 keys is required.");
            return new(Array.AsReadOnly(keys.OrderBy(id => id.ToString("D"), StringComparer.Ordinal).ToArray()));
        }
        catch (ArgumentException error)
        { return new() { IsError = true, ErrorCount = 1, ErrorCode = "CQL_CAPABILITY_UNSUPPORTED", Message = error.Message, Exception = error }; }
    }
}
