using System;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS.Infrastructure;

/// <summary>Cosmos native-field adapter; shared codec owns the canonical history format.
/// Cosmos SDK writer still owns observed-ETag persistence and native item-size rejection.</summary>
public static class CosmosEmbeddedHistory
{
    public const string PropertyName=EmbeddedRecordHistory.PropertyName;
    private static readonly EmbeddedRecordHistory Codec=new("id","_etag","_rid","_self","_attachments","_ts");
    public static JObject CanonicalPayload(JObject document)=>Codec.CanonicalPayload(document);
    public static Task<JObject> StageAsync<T>(T proposed,JObject previousDocument,Func<string,T> decode) where T:class,IHolonBase
        =>Codec.StageAsync(proposed,previousDocument,decode);
    public static Task<T> ReadAsync<T>(Guid identity,JObject document,int version,Func<string,T> decode) where T:class,IHolonBase
        =>Codec.ReadAsync(identity,document,version,decode);
}
