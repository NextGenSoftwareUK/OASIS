using System;
using System.Collections.Generic;
using System.Linq;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.Core.Enums;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS;

internal static class NeoCqlContract
{
    internal static readonly CosmicQueryCapabilities Capabilities = Create();
    private static CosmicQueryCapabilities Create()
    {
        var equality = new[] { CosmicQueryComparisonOperator.Equal, CosmicQueryComparisonOperator.NotEqual };
        var ordered = Enum.GetValues<CosmicQueryComparisonOperator>();
        var strings = Enum.GetValues<CosmicQueryStringOperator>();
        var fields = new List<CosmicQueryFieldCapability>();
        foreach (var name in new[] { "Name", "Description" }) fields.Add(new(name, typeof(string), equality, strings));
        fields.Add(new("Version", typeof(int), ordered));
        fields.Add(new("HolonType",typeof(HolonType),ordered));
        foreach (var name in new[] { "IsActive", "IsPublic" }) fields.Add(new(name, typeof(bool), equality));
        foreach (var name in new[] { "Id", "ParentHolonId", "VersionId", "PreviousVersionId", "CreatedByAvatarId", "ModifiedByAvatarId" })
            fields.Add(new(name, typeof(Guid), equality));
        var metadata = new List<CosmicQueryMetadataCapability> { new(typeof(string), equality, strings, supportsMembership: true) };
        foreach (var type in new[] { typeof(bool), typeof(bool?), typeof(int), typeof(int?), typeof(long), typeof(long?) })
            metadata.Add(new(type, type == typeof(bool) || type == typeof(bool?) ? equality : ordered, supportsMembership: true));
        foreach(var type in new[]{typeof(byte),typeof(sbyte),typeof(short),typeof(ushort),typeof(uint),typeof(HolonType),typeof(ProviderType)})
            foreach(var declared in new[]{type,typeof(Nullable<>).MakeGenericType(type)})
                metadata.Add(new(declared,ordered,supportsMembership:true));
        foreach(var type in new[]{typeof(Guid),typeof(Guid?)}) metadata.Add(new(type,equality,supportsMembership:true));
        return new(CosmicQueryFeatures.And | CosmicQueryFeatures.Or | CosmicQueryFeatures.Not | CosmicQueryFeatures.Metadata |
            CosmicQueryFeatures.NullLiteral | CosmicQueryFeatures.BooleanConstant | CosmicQueryFeatures.MetadataExists |
            CosmicQueryFeatures.MetadataNull | CosmicQueryFeatures.StringContains | CosmicQueryFeatures.StringStartsWith |
            CosmicQueryFeatures.StringEndsWith | CosmicQueryFeatures.Membership | CosmicQueryFeatures.FieldComparison |
            CosmicQueryFeatures.Ordering | CosmicQueryFeatures.Paging | CosmicQueryFeatures.GraphExpansion,
            fields.Select(field=>new CosmicQueryFieldCapability(field.Name,field.ValueType,field.Comparisons.ToArray(),
                field.StringOperations.ToArray(),supportsOrdering:true)).ToArray(),
            metadata.Select(type=>new CosmicQueryMetadataCapability(type.ValueType,type.Comparisons.ToArray(),
                type.StringOperations.ToArray(),type.SupportsMembership,type.SupportsMixedNumericComparison,supportsOrdering:true)).ToArray());
    }
}
