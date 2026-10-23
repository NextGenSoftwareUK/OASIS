using System;
using System.Collections.Generic;
using System.Linq;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS;

internal static class MongoCqlContract
{
    internal static readonly CosmicQueryCapabilities Capabilities=Create();
    private static CosmicQueryCapabilities Create()
    {
        var equal=new[]{CosmicQueryComparisonOperator.Equal,CosmicQueryComparisonOperator.NotEqual};
        var ordered=Enum.GetValues<CosmicQueryComparisonOperator>();
        var strings=Enum.GetValues<CosmicQueryStringOperator>();
        var fields=new List<CosmicQueryFieldCapability>();
        foreach(var name in new[]{"Name","Description"})fields.Add(new(name,typeof(string),equal,strings));
        fields.Add(new("Version",typeof(int),ordered));fields.Add(new("HolonType",typeof(HolonType),ordered));
        foreach(var name in new[]{"IsActive","IsPublic"})fields.Add(new(name,typeof(bool),equal));
        foreach(var name in new[]{"Id","ParentHolonId","VersionId","PreviousVersionId","CreatedByAvatarId","ModifiedByAvatarId"})
            fields.Add(new(name,typeof(Guid),equal));
        var metadata=new[]{
            new CosmicQueryMetadataCapability(typeof(string),equal,strings,supportsMembership:true),
            new CosmicQueryMetadataCapability(typeof(bool),equal,supportsMembership:true),
            new CosmicQueryMetadataCapability(typeof(bool?),equal,supportsMembership:true),
            new CosmicQueryMetadataCapability(typeof(Guid),equal,supportsMembership:true),
            new CosmicQueryMetadataCapability(typeof(Guid?),equal,supportsMembership:true),
            new CosmicQueryMetadataCapability(typeof(DateTime),ordered,supportsMembership:true),
            new CosmicQueryMetadataCapability(typeof(DateTime?),ordered,supportsMembership:true),
            new CosmicQueryMetadataCapability(typeof(int),ordered,supportsMembership:true),
            new CosmicQueryMetadataCapability(typeof(int?),ordered,supportsMembership:true),
            new CosmicQueryMetadataCapability(typeof(long),ordered,supportsMembership:true),
            new CosmicQueryMetadataCapability(typeof(long?),ordered,supportsMembership:true)
        };
        metadata=metadata.Concat(new[]{typeof(byte),typeof(sbyte),typeof(short),typeof(ushort),typeof(uint),typeof(HolonType),typeof(ProviderType)}
            .SelectMany(type=>new[]{type,typeof(Nullable<>).MakeGenericType(type)})
            .Select(type=>new CosmicQueryMetadataCapability(type,ordered,supportsMembership:true))).ToArray();
        fields=fields.Select(f=>new CosmicQueryFieldCapability(f.Name,f.ValueType,f.Comparisons.ToArray(),f.StringOperations.ToArray(),supportsOrdering:true)).ToList();
        metadata=metadata.Select(m=>new CosmicQueryMetadataCapability(m.ValueType,m.Comparisons.ToArray(),m.StringOperations.ToArray(),
            m.SupportsMembership,m.SupportsMixedNumericComparison,supportsOrdering:true)).ToArray();
        return new CosmicQueryCapabilities(CosmicQueryFeatures.And|CosmicQueryFeatures.Or|CosmicQueryFeatures.Not
            |CosmicQueryFeatures.Metadata|CosmicQueryFeatures.NullLiteral|CosmicQueryFeatures.BooleanConstant
            |CosmicQueryFeatures.MetadataExists|CosmicQueryFeatures.MetadataNull|CosmicQueryFeatures.Membership
            |CosmicQueryFeatures.Ordering|CosmicQueryFeatures.Paging|CosmicQueryFeatures.FieldComparison|CosmicQueryFeatures.StringContains|CosmicQueryFeatures.StringStartsWith|CosmicQueryFeatures.StringEndsWith|CosmicQueryFeatures.GraphExpansion,
            fields.ToArray(),metadata);
    }
}
