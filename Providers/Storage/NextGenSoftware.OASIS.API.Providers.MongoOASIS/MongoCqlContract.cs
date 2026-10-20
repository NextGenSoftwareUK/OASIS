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
        return new CosmicQueryCapabilities(CosmicQueryFeatures.And|CosmicQueryFeatures.Or|CosmicQueryFeatures.Not
            |CosmicQueryFeatures.Metadata|CosmicQueryFeatures.NullLiteral|CosmicQueryFeatures.BooleanConstant
            |CosmicQueryFeatures.MetadataExists|CosmicQueryFeatures.MetadataNull|CosmicQueryFeatures.Membership
            |CosmicQueryFeatures.StringContains|CosmicQueryFeatures.StringStartsWith|CosmicQueryFeatures.StringEndsWith,
            fields.ToArray(),metadata);
    }
}
