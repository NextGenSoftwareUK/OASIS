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
    static SqliteCqlContract()
    {
        var fields = new Dictionary<string, CosmicQueryFieldCapability>(StringComparer.Ordinal);
        var equality = new[] { CosmicQueryComparisonOperator.Equal, CosmicQueryComparisonOperator.NotEqual };
        var ordered = System.Enum.GetValues<CosmicQueryComparisonOperator>();
        foreach (var name in new[] { "Name", "Description" }) fields.Add(name, new(name, typeof(string), equality));
        fields.Add("Version", new("Version", typeof(int), ordered));
        fields.Add("HolonType", new("HolonType", typeof(HolonType), ordered));
        foreach (var name in new[] { "IsActive", "IsPublic" }) fields.Add(name, new(name, typeof(bool), equality));
        foreach (var name in new[] { "Id", "ParentHolonId", "VersionId", "PreviousVersionId", "CreatedByAvatarId", "ModifiedByAvatarId" })
            fields.Add(name, new(name, typeof(Guid), equality));
        Fields = new ReadOnlyDictionary<string, CosmicQueryFieldCapability>(fields);
        Capabilities = new CosmicQueryCapabilities(CosmicQueryFeatures.And | CosmicQueryFeatures.Or | CosmicQueryFeatures.Not |
            CosmicQueryFeatures.NullLiteral | CosmicQueryFeatures.BooleanConstant, fields.Values.ToArray());
    }
}
