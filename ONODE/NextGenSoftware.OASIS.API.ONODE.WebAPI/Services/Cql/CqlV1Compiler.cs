using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text.Json;
using System.Globalization;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;

public static partial class CqlV1Compiler
{
    private static readonly string[] UtcDateFormats = Enumerable.Range(0, 8)
        .Select(digits => "yyyy-MM-dd'T'HH:mm:ss" + (digits == 0 ? "" : "." + new string('f', digits)) + "'Z'").ToArray();

    // One wire owner for source and selected-result dates. Do not infer a
    // timezone or round input wider than DateTime's100ns resolution.
    private static bool TryUInt64(JsonElement json, out ulong value)
    {
        value = 0;
        if (json.ValueKind != JsonValueKind.String) return false;
        var text = json.GetString();
        return text.Length is >= 1 and <= 20 && text.All(c => c is >= '0' and <= '9')
            && ulong.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out value);
    }

    private static bool TryUtcDate(JsonElement json, out DateTime value)
    {
        value = default;
        if (json.ValueKind != JsonValueKind.String) return false;
        var text = json.GetString();
        return text.Length is >= 20 and <= 28 && DateTime.TryParseExact(text, UtcDateFormats,
            CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out value);
    }
    // Share the exact source/group literal contract. Decimal.TryParse alone can
    // round excess precision; re-encoding must reproduce the original string.
    private static bool TryDecimal(JsonElement json, out decimal value)
    {
        value = 0;
        if (json.ValueKind != JsonValueKind.String) return false;
        var text = json.GetString();
        return text.Length is >= 1 and <= 31
            && decimal.TryParse(text, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
                CultureInfo.InvariantCulture, out value)
            && value.ToString(CultureInfo.InvariantCulture) == text;
    }
    // Require the exact finite binary64 round-trip spelling. This prevents
    // silently rounding an over-precise string or accepting underflow as zero.
    private static bool TryDouble(JsonElement json, out double value)
    {
        value = 0d;
        if (json.ValueKind != JsonValueKind.String) return false;
        var text = json.GetString();
        if (text.Length is < 1 or > 24 || !double.TryParse(text,
            NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint | NumberStyles.AllowExponent,
            CultureInfo.InvariantCulture, out value) || double.IsNaN(value) || double.IsInfinity(value))
            return false;
        if (value == 0d) value = 0d;
        return value.ToString("R", CultureInfo.InvariantCulture) == text;
    }
    // Canonical binary32 strings prevent silent rounding of over-precise input.
    private static bool TrySingle(JsonElement json, out float value)
    {
        value = 0f;
        if (json.ValueKind != JsonValueKind.String) return false;
        var text = json.GetString();
        if (text.Length is < 1 or > 24 || !float.TryParse(text,
            NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint | NumberStyles.AllowExponent,
            CultureInfo.InvariantCulture, out value) || float.IsNaN(value) || float.IsInfinity(value))
            return false;
        if (value == 0f) value = 0f;
        return value.ToString("R", CultureInfo.InvariantCulture) == text;
    }
    public static bool Supported(ProviderType type) => type is ProviderType.MongoDBOASIS or ProviderType.SQLLiteDBOASIS or ProviderType.Neo4jOASIS;
    // Read profiles retain their exact native capabilities. Bounded federation
    // admits the complete selected read set; composition keeps its separate scope.
    internal static IReadOnlyList<ProviderType> ReadProfiles { get; } = Array.AsReadOnly(new[]
    {
        ProviderType.MongoDBOASIS, ProviderType.SQLLiteDBOASIS, ProviderType.Neo4jOASIS,
        ProviderType.AzureCosmosDBOASIS, ProviderType.ActivityPubOASIS, ProviderType.IPFSOASIS, ProviderType.SOLIDOASIS,
        ProviderType.AWSS3OASIS, ProviderType.OracleDBOASIS, ProviderType.SolanaOASIS, ProviderType.GoogleCloudOASIS, ProviderType.EthereumOASIS
    });
    public static bool SupportedReadProfile(ProviderType type) => ReadProfiles.Contains(type);

    public static ProviderType? RequestedProvider(string name, bool includeCloudReadProfile = false)
    {
        if (name == null) return null;
        if (name.Length > 40 || !Enum.TryParse<ProviderType>(name, false, out var type) ||
            type.ToString() != name || !(includeCloudReadProfile ? SupportedReadProfile(type) : Supported(type)))
            throw new ArgumentException(includeCloudReadProfile
                ? "Select an enrolled MongoDB, SQLite, Neo4j, Cosmos DB, ActivityPub, IPFS, SOLID or AWS S3 read profile, or omit Provider for the current default."
                : "Select MongoDBOASIS, SQLLiteDBOASIS or Neo4jOASIS, or omit Provider for the current default.");
        return type;
    }

    public static CosmicQuery<Holon> Compile(CqlV1Request request, Guid actor, ProviderType? provider)
        =>Compile(request,actor,provider,null);

    public static CosmicQuery<Holon> Compile(CqlV1Request request, Guid actor, ProviderType? provider,
        CosmicQueryReadAccess access)
    {
        if (request == null || request.Take is < 1 or > 100)
            throw new ArgumentException("Row queries require Take 1–100.");
        return CompileSource(actor, provider, request.Filters, request.AnyOf, request.AnyChildren,
            request.AnyDescendants, request.Graph, request.OrderBy, request.Descending, request.Skip, request.Take,access,includeCloudReadProfile:true);
    }

    public static CosmicQuery<Holon> CompileScalarSource(CqlScalarV1Request request, Guid actor, ProviderType? provider)
        =>CompileScalarSource(request,actor,provider,null);

    public static CosmicQuery<Holon> CompileScalarSource(CqlScalarV1Request request, Guid actor, ProviderType? provider,
        CosmicQueryReadAccess access)
    {
        if (request == null) throw new ArgumentException("A scalar query description is required.");
        return CompileSource(actor, provider, request.Filters, request.AnyOf, request.AnyChildren,
            request.AnyDescendants, request.Graph, request.OrderBy, request.Descending, request.Skip, request.Take,access,includeCloudReadProfile:true);
    }

    internal static CosmicQuery<Holon> CompileIndexedScalarSource(CqlScalarV1Request request, Guid actor,
        CosmicChainQueryReadRequirement requirement, CosmicQueryReadAccess access)
    {
        if (request == null || request.Provider != null || request.Graph != null || request.Selection != null
            || request.Operation is not ("Count" or "LongCount" or "Any"))
            throw new ArgumentException("Indexed cardinality requires an alias-owned source and Count, LongCount or Any.");
        var source = CompileSource(actor, requirement.Source.SourceProvider, request.Filters, request.AnyOf,
            request.AnyChildren, request.AnyDescendants, null, request.OrderBy, request.Descending,
            request.Skip, request.Take, access, includeCloudReadProfile: true, indexedSource: true);
        var indexed = source.WithIndexedSource(requirement);
        if (indexed.IsError) throw new ArgumentException(indexed.Message);
        return indexed.Result;
    }

    internal static CosmicQuery<Holon> CompileIndexedSource(CqlV1Request request, Guid actor,
        CosmicChainQueryReadRequirement requirement, CosmicQueryReadAccess access)
    {
        if (request == null || request.Provider != null || request.Graph != null || request.Take is < 1 or > 100)
            throw new ArgumentException("Indexed rows require Take1-100 with alias-owned provider and no graph.");
        var source = CompileSource(actor, requirement.Source.SourceProvider, request.Filters, request.AnyOf,
            request.AnyChildren, request.AnyDescendants, null, request.OrderBy, request.Descending,
            request.Skip, request.Take, access, includeCloudReadProfile:true, indexedSource:true);
        var indexed = source.WithIndexedSource(requirement);
        if(indexed.IsError)throw new ArgumentException(indexed.Message);
        return indexed.Result;
    }

    // Only the federation planner may bind this unbound business intent to its
    // complete captured target policies before any native execution.
    internal static CosmicQuery<Holon> CompileFederationSource(CqlV1Request request, Guid actor)
    {
        if (request == null || request.Provider != null || request.Graph != null || request.Take is < 1 or > 100)
            throw new ArgumentException("Federation source requires Take1-100 and no duplicate provider or graph selector.");
        return CompileSource(actor, null, request.Filters, request.AnyOf, request.AnyChildren, request.AnyDescendants,
            null, request.OrderBy, request.Descending, request.Skip, request.Take,
            bindAccess: false, unorderedFederation: true);
    }

    private static CosmicQuery<Holon> CompileSource(Guid actor, ProviderType? provider, CqlV1Filter[] filters,
        CqlV1Filter[] anyOf, CqlV1ChildExists[] anyChildren, CqlV1DescendantExists[] anyDescendants,
        CqlV1Graph graphDescription, string orderBy, bool descending, int skip, int? take,CosmicQueryReadAccess access=null,
        bool bindAccess=true, bool includeCloudReadProfile=false, bool indexedSource=false, bool unorderedFederation=false)
    {
        if(access!=null)
        {
            if(access.ActorId!=actor || access.Authority.HasValue && provider.HasValue && access.Authority!=provider)
                throw new ArgumentException("Trusted access must bind the same authenticated actor and concrete provider.");
            provider=access.Authority??provider;
        }
        if (actor == Guid.Empty || provider.HasValue && !(indexedSource ? provider.Value==ProviderType.SolanaOASIS : includeCloudReadProfile ? SupportedReadProfile(provider.Value) : Supported(provider.Value)))
            throw new ArgumentException("Authenticated actor and concrete v1 provider required.");
        if (filters == null || anyOf == null || anyChildren == null || anyDescendants == null ||
            anyChildren.Length + (long)anyDescendants.Length > 4 ||
            filters.Length + anyOf.Length > 12 || skip is < 0 or > 1000 || take is < 0 or > 10000)
            throw new ArgumentException("Use at most 12 filters, Skip 0–1000 and optional Take 0–10000.");
        var filterCount = filters.Length + anyOf.Length;
        void CountGroup(CqlV1Filter[] filters,CqlV1Filter[] anyOf)
        {
            if(filters==null || anyOf==null || filters.Length+(long)anyOf.Length is <1 or >12)
                throw new ArgumentException("Each relationship group requires one to twelve bounded filters.");
            filterCount+=filters.Length+anyOf.Length;
            if(filterCount>12)throw new ArgumentException("Use at most twelve filters across roots and relationship groups.");
        }
        foreach(var group in anyChildren)
        {
            if(group==null)throw new ArgumentException("Child-existence groups cannot be null.");
            CountGroup(group.Filters,group.AnyOf);
        }
        foreach(var group in anyDescendants)
        {
            if(group==null)throw new ArgumentException("Descendant-existence groups cannot be null.");
            CountGroup(group.Filters,group.AnyOf);
            var limits=CosmicQueryTraversalOptions.Create(group.MaximumDepth,group.MaximumNodes);
            if(limits.IsError)throw new ArgumentException(limits.Message);
        }

        // Mandatory access restriction enters the SAME native plan before filters, ordering
        // and paging. Shared-index metadata cannot grant access; Wizard does not bypass this.
        var query = CosmicQuery<Holon>.Create(provider.HasValue ? CosmicQueryScope.Named(provider.Value).Result : CosmicQueryScope.CurrentDefault);
        if (bindAccess) query = Add(query, actor, access, provider);
        if(graphDescription!=null)
        {
            var graph=graphDescription;
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
        foreach (var filter in filters)
            query = Add(query, parameter, Predicate(filter, parameter));

        Expression alternatives = null;
        foreach (var filter in anyOf)
        {
            var condition = Predicate(filter, parameter);
            alternatives = alternatives == null ? condition : Expression.OrElse(alternatives, condition);
        }
        if (alternatives != null) query = Add(query, parameter, alternatives);

        foreach(var group in anyChildren)
        {
            var related=query.WhereAnyChild(RelationshipPredicate(group.Filters,group.AnyOf));
            if(related.IsError)throw new ArgumentException(related.Message);
            query=related.Result;
        }
        foreach(var group in anyDescendants)
        {
            var limits=CosmicQueryTraversalOptions.Create(group.MaximumDepth,group.MaximumNodes);
            if(limits.IsError)throw new ArgumentException(limits.Message);
            var related=query.WhereAnyDescendant(RelationshipPredicate(group.Filters,group.AnyOf),limits.Result);
            if(related.IsError)throw new ArgumentException(related.Message);
            query=related.Result;
        }

        if (orderBy == "None")
        {
            if (descending || !unorderedFederation && (!includeCloudReadProfile
                || provider is not (ProviderType.ActivityPubOASIS or ProviderType.IPFSOASIS or ProviderType.SOLIDOASIS or ProviderType.AWSS3OASIS or ProviderType.OracleDBOASIS or ProviderType.SolanaOASIS or ProviderType.GoogleCloudOASIS or ProviderType.EthereumOASIS)))
                throw new ArgumentException("OrderBy None requires an unsorted protocol read profile or an explicitly unordered federation, without Descending.");
            var pointPage = query.Skip(skip);
            if (take.HasValue) pointPage = pointPage.Take(take.Value);
            if (pointPage.IsError) throw new ArgumentException(pointPage.Message);
            return pointPage.Result;
        }
        var ordered = orderBy switch
        {
            "Name" => descending ? query.OrderByDescending(h => h.Name) : query.OrderBy(h => h.Name),
            "Version" => descending ? query.OrderByDescending(h => h.Version) : query.OrderBy(h => h.Version),
            "Id" => descending ? query.OrderByDescending(h => h.Id) : query.OrderBy(h => h.Id),
            _ => throw new ArgumentException("OrderBy accepts Name, Version or Id.")
        };
        if (ordered.IsError) throw new ArgumentException(ordered.Message);
        var page = ordered.ThenBy(h => h.Id).Skip(skip);
        if (take.HasValue) page = page.Take(take.Value);
        if (page.IsError) throw new ArgumentException(page.Message);
        return page.Result;
    }

    private static Expression<Func<IHolon,bool>> RelationshipPredicate(CqlV1Filter[] filters,CqlV1Filter[] anyOf)
    {
        var child=Expression.Parameter(typeof(IHolon),"child");
        Expression condition=null;
        foreach(var filter in filters)
        {
            var next=Predicate(filter,child);
            condition=condition==null?next:Expression.AndAlso(condition,next);
        }
        Expression alternatives=null;
        foreach(var filter in anyOf)
        {
            var next=Predicate(filter,child);
            alternatives=alternatives==null?next:Expression.OrElse(alternatives,next);
        }
        if(alternatives!=null)condition=condition==null?alternatives:Expression.AndAlso(condition,alternatives);
        return Expression.Lambda<Func<IHolon,bool>>(condition,child);
    }

    /// <summary>
    /// What a caller sees by default: their own and public holons, and (when the host provides this) holons their owner shared
    /// with them, a group they are in, or everyone. Null keeps the plain owner/public rule. Set once at start-up.
    /// </summary>
    public static ICqlSharedReads SharedReads { get; set; }

    internal static NextGenSoftware.OASIS.Common.OASISResult<CosmicQueryReadAccess> DefaultReadAccess(Guid actor, ProviderType? provider)
        =>DefaultReadAccess(actor,provider,SharedReads);

    internal static NextGenSoftware.OASIS.Common.OASISResult<CosmicQueryReadAccess> DefaultReadAccess(Guid actor, ProviderType? provider,
        ICqlSharedReads shared)
    {
        if(shared==null||!shared.SharedReads)return CosmicQueryReadAccess.OwnerOrPublic(actor);
        // Inherited reads need the provider to walk up the parent chain natively; one that cannot keeps the plain shared rule.
        var inherited=shared.InheritedReads&&shared.ProviderWalksAncestors(provider);
        return CosmicQueryReadAccess.OwnerPublicOrShared(actor,shared.GroupsOf(actor),inherited);
    }

    private static CosmicQuery<Holon> Add(CosmicQuery<Holon> query, Guid actor,CosmicQueryReadAccess trusted,ProviderType? provider=null)
    {
        var access=trusted==null?DefaultReadAccess(actor,provider):new NextGenSoftware.OASIS.Common.OASISResult<CosmicQueryReadAccess>(trusted);
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
                CqlV1Scalar.UInt64 => typeof(ulong), CqlV1Scalar.Decimal => typeof(decimal), CqlV1Scalar.Double => typeof(double), CqlV1Scalar.Single => typeof(float),
                CqlV1Scalar.Boolean => typeof(bool), CqlV1Scalar.DateTime => typeof(DateTime), _ => typeof(Guid)
            };
            path = Expression.Call(typeof(CosmicQueryMetadata), "Value", new[] { type }, parameter, segments);
        }
        else
        {
            if (filter.Field is not ("Id" or "Name" or "Description" or "Version" or "IsActive" or "ParentHolonId"))
                throw new ArgumentException("Unsupported canonical query field.");
            if (filter.Operator is CqlV1Operator.Exists or CqlV1Operator.IsNull) throw new ArgumentException("Existence/null tests require MetadataPath.");
            // Reflection on an interface does not include inherited interfaces.
            // Bind the one declared contract member (e.g. IHolonBase.Name).
            var property = parameter.Type.IsInterface
                ? parameter.Type.GetInterfaces().Append(parameter.Type).SelectMany(type => type.GetProperties())
                    .Where(member => member.Name == filter.Field).Distinct().Single()
                : parameter.Type.GetProperty(filter.Field);
            path = Expression.Property(parameter, property);
        }

        if (filter.Operator == CqlV1Operator.In)
        {
            if (filter.Value.ValueKind != JsonValueKind.Array || filter.Value.GetArrayLength() is < 1 or > 100)
                throw new ArgumentException("In requires an array of1–100 exact bounded literals.");
            var values = Expression.NewArrayInit(path.Type,
                filter.Value.EnumerateArray().Select(json => Literal(json, path.Type)));
            // One membership AST. Never expand this into point queries or in-memory filtering.
            return Expression.Call(typeof(Enumerable), nameof(Enumerable.Contains), new[] { path.Type }, values, path);
        }

        var literal = Literal(filter.Value, path.Type);
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

    // Scalar comparisons and membership share the same precision/type boundary.
    private static ConstantExpression Literal(JsonElement json, Type type)
    {
        object value;
        if (type == typeof(string) && json.ValueKind == JsonValueKind.String && json.GetString().Length <= 512) value = json.GetString();
        else if (type == typeof(bool) && json.ValueKind is JsonValueKind.True or JsonValueKind.False) value = json.GetBoolean();
        else if (type == typeof(long) && json.ValueKind == JsonValueKind.Number && json.TryGetInt64(out var number)) value = number;
        else if (type == typeof(int) && json.ValueKind == JsonValueKind.Number && json.TryGetInt32(out var integer)) value = integer;
        else if (type == typeof(Guid) && json.ValueKind == JsonValueKind.String && Guid.TryParseExact(json.GetString(), "D", out var guid)) value = guid;
        else if (type == typeof(ulong) && TryUInt64(json, out var unsigned)) value = unsigned;
        else if (type == typeof(float) && TrySingle(json, out var finiteSingle)) value = finiteSingle;
        else if (type == typeof(double) && TryDouble(json, out var finiteDouble)) value = finiteDouble;
        else if (type == typeof(decimal) && TryDecimal(json, out var exactDecimal)) value = exactDecimal;
        else if (type == typeof(DateTime) && TryUtcDate(json, out var utc)) value = utc;
        else throw new ArgumentException("Value must match the exact bounded scalar type.");
        return Expression.Constant(value, type);
    }
}

/// <summary>
/// The host's answer to "what has been shared with this caller": whether shared reads are on, the caller's groups, and
/// whether the provider about to run the query can walk up the parent chain natively.
/// </summary>
public interface ICqlSharedReads
{
    /// <summary>Queries also return holons shared with the caller, their groups, or everyone.</summary>
    bool SharedReads { get; }
    /// <summary>Also return holons below one that granted read to everything beneath it, where the provider can.</summary>
    bool InheritedReads { get; }
    /// <summary>The groups the caller belongs to, including groups of groups. A failed lookup returns none (shares less, never more).</summary>
    System.Collections.Generic.IReadOnlyCollection<Guid> GroupsOf(Guid actor);
    /// <summary>The provider (null: the current default) declares native ancestor support.</summary>
    bool ProviderWalksAncestors(ProviderType? provider);
}

