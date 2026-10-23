using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS;

internal static class SqliteCqlContract
{
    internal static readonly IReadOnlyDictionary<string, CosmicQueryFieldCapability> Fields;
    internal static readonly CosmicQueryCapabilities Capabilities;
    internal static readonly IReadOnlyDictionary<Type, CosmicQueryMetadataCapability> MetadataTypes;
    static SqliteCqlContract()
    {
        var fields = new Dictionary<string, CosmicQueryFieldCapability>(StringComparer.Ordinal);
        var equality = new[] { CosmicQueryComparisonOperator.Equal, CosmicQueryComparisonOperator.NotEqual };
        var ordered = System.Enum.GetValues<CosmicQueryComparisonOperator>();
        foreach (var name in new[] { "Name", "Description" }) fields.Add(name, new(name, typeof(string), equality, System.Enum.GetValues<CosmicQueryStringOperator>()));
        fields.Add("Version", new("Version", typeof(int), ordered));
        fields.Add("HolonType", new("HolonType", typeof(HolonType), ordered));
        foreach (var name in new[] { "IsActive", "IsPublic" }) fields.Add(name, new(name, typeof(bool), equality));
        foreach (var name in new[] { "Id", "ParentHolonId", "VersionId", "PreviousVersionId", "CreatedByAvatarId", "ModifiedByAvatarId" })
            fields.Add(name, new(name, typeof(Guid), equality));
        fields=fields.ToDictionary(pair=>pair.Key,pair=>new CosmicQueryFieldCapability(pair.Value.Name,pair.Value.ValueType,
            pair.Value.Comparisons.ToArray(),pair.Value.StringOperations.ToArray(),supportsOrdering:true),StringComparer.Ordinal);
        Fields = new ReadOnlyDictionary<string, CosmicQueryFieldCapability>(fields);
        var metadata = new[] {
            new CosmicQueryMetadataCapability(typeof(string), equality, System.Enum.GetValues<CosmicQueryStringOperator>(), supportsMembership: true),
            new CosmicQueryMetadataCapability(typeof(bool), equality, supportsMembership: true),
            new CosmicQueryMetadataCapability(typeof(bool?), equality, supportsMembership: true),
            new CosmicQueryMetadataCapability(typeof(Guid), equality, supportsMembership: true),
            new CosmicQueryMetadataCapability(typeof(Guid?), equality, supportsMembership: true),
            new CosmicQueryMetadataCapability(typeof(DateTime), ordered, supportsMembership: true),
            new CosmicQueryMetadataCapability(typeof(DateTime?), ordered, supportsMembership: true),
            new CosmicQueryMetadataCapability(typeof(int), ordered, supportsMembership: true),
            new CosmicQueryMetadataCapability(typeof(int?), ordered, supportsMembership: true),
            new CosmicQueryMetadataCapability(typeof(long), ordered, supportsMembership: true),
            new CosmicQueryMetadataCapability(typeof(long?), ordered, supportsMembership: true)
        };
        metadata=metadata.Concat(new[]{typeof(byte),typeof(sbyte),typeof(short),typeof(ushort),typeof(uint),typeof(HolonType),typeof(ProviderType)}
            .SelectMany(type=>new[]{type,typeof(Nullable<>).MakeGenericType(type)})
            .Select(type=>new CosmicQueryMetadataCapability(type,ordered,supportsMembership:true))).ToArray();
        metadata=metadata.Select(m=>new CosmicQueryMetadataCapability(m.ValueType,m.Comparisons.ToArray(),m.StringOperations.ToArray(),
            m.SupportsMembership,m.SupportsMixedNumericComparison,supportsOrdering:true)).ToArray();
        MetadataTypes = new ReadOnlyDictionary<Type, CosmicQueryMetadataCapability>(metadata.ToDictionary(m => m.ValueType));
        Capabilities = new CosmicQueryCapabilities(CosmicQueryFeatures.And | CosmicQueryFeatures.Or | CosmicQueryFeatures.Not |
            CosmicQueryFeatures.Metadata | CosmicQueryFeatures.StringContains | CosmicQueryFeatures.StringStartsWith | CosmicQueryFeatures.StringEndsWith | CosmicQueryFeatures.NullLiteral | CosmicQueryFeatures.BooleanConstant | CosmicQueryFeatures.MetadataExists | CosmicQueryFeatures.MetadataNull | CosmicQueryFeatures.Membership | CosmicQueryFeatures.MixedNumericComparison | CosmicQueryFeatures.FieldComparison | CosmicQueryFeatures.Ordering | CosmicQueryFeatures.Paging | CosmicQueryFeatures.GraphExpansion|CosmicQueryFeatures.GraphReadAccess|CosmicQueryFeatures.ChildExists|CosmicQueryFeatures.GraphSnapshot, fields.Values.ToArray(), metadata);
    }
}
