using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS;

public sealed partial class SqliteCosmicChainIndexStore
{
    private CosmicQueryCapabilities MetadataRowCapabilities()
    {
        var types=new[]{typeof(string),typeof(bool),typeof(bool?),typeof(int),typeof(int?),typeof(long),typeof(long?)};
        var metadata=types.Select(type=>new CosmicQueryMetadataCapability(type,
            type==typeof(bool)||type==typeof(bool?)?Equality:(CosmicQueryComparisonOperator[])Enum.GetValues(typeof(CosmicQueryComparisonOperator)),
            supportsMembership:true,supportsOrdering:true)).ToArray();
        return new CosmicQueryCapabilities(CanonicalRowCapabilities.Features|CosmicQueryFeatures.Metadata|
            CosmicQueryFeatures.MetadataExists|CosmicQueryFeatures.MetadataNull|CosmicQueryFeatures.NullLiteral,
            CanonicalRowCapabilities.CanonicalFields.ToArray(),metadata).WithMetadataRoots(metadataPolicy.Keys);
    }

    private string MetadataContext(string id,string versionId)
        =>context+"/metadata/"+metadataPolicy.ProfileHash+"/"+id+"/"+versionId;

    private string QueryableMetadataJson(Holon row)
    {
        // Export only selected roots already exempt from encryption. Never decrypt
        // or copy the entire metadata dictionary to make a query possible.
        var selected=new Dictionary<string,object>(StringComparer.Ordinal);
        if(row.MetaData!=null)foreach(var pair in row.MetaData)
            if(metadataPolicy.Keys.Contains(pair.Key,StringComparer.Ordinal))selected.Add(pair.Key,pair.Value);
        var json=OasisJson.Serialize(selected);
        if(Encoding.UTF8.GetByteCount(json)>4*1024*1024)throw new System.IO.InvalidDataException("Queryable metadata exceeds its authenticated codec budget.");
        return json;
    }
}
