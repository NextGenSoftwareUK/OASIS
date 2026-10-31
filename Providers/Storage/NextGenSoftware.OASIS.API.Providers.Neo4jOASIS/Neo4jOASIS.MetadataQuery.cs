using System;
using System.Collections.Generic;
using System.Linq;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS;

public partial class Neo4jOASIS
{
    // Exact ordinal string projections, matching the existing shared provider's MetaMatches semantics.
    // These index entries are JSON-encoded lookup values, NOT encryption or a public-data safety boundary.
    private static string MetadataLookupToken(string key, string value) => OasisJson.Serialize(new[] { key, value });

    internal static List<string> MetadataLookupTokens(IHolon holon)
        => holon.MetaData?.Select(pair => MetadataLookupToken(pair.Key, StoredRecordSearch.MetadataValueText(pair.Value))).ToList() ?? new List<string>();

    internal static (string Query, Dictionary<string, object> Parameters) MetadataLookupQuery(
        Dictionary<string, string> pairs, MetaKeyValuePairMatchMode mode, HolonType type)
    {
        if (pairs == null || pairs.Count == 0 || pairs.Keys.Any(string.IsNullOrWhiteSpace))
            throw new ArgumentException("At least one nonblank metadata key is required.");
        if (!Enum.IsDefined(typeof(MetaKeyValuePairMatchMode), mode) || !Enum.IsDefined(typeof(HolonType), type))
            throw new ArgumentException("Defined metadata match mode and holon type are required.");
        var predicate = mode == MetaKeyValuePairMatchMode.All ? "all" : "any";
        var query = "MATCH (h:Holon) WHERE ($type = 'All' OR h.HolonType = $type) AND "
            + predicate + "(token IN $tokens WHERE token IN coalesce(h.MetaLookupTokens, [])) RETURN h";
        return (query, new Dictionary<string, object> {
            ["type"] = type.ToString(), ["tokens"] = pairs.Select(pair => MetadataLookupToken(pair.Key, pair.Value)).ToList()
        });
    }
}
