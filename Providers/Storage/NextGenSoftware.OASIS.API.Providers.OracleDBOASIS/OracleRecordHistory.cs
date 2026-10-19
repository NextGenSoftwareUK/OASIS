using System;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.OracleDBOASIS;

public partial class OracleDBOASIS
{
    private static readonly EmbeddedRecordHistory RecordHistory=new();

    private async Task<T?> ReadRecordVersionAsync<T>(Guid identity,string json,int version) where T:class,IHolonBase
    {
        if(version<0)throw new ArgumentOutOfRangeException(nameof(version));
        var current=Deserialize<T>(json);
        if(current.Id!=identity)throw new InvalidDataException("Oracle canonical record identity differs from its native key.");
        var document=JObject.Parse(json);
        if(document["id"]!=null&&document["id"]!.Value<string>()!=identity.ToString())throw new InvalidDataException("Oracle embedded history identity differs from native key.");
        if(document.Property(EmbeddedRecordHistory.PropertyName)==null)return version==0||current.Version==version?current:null;
        document["id"]=identity.ToString();
        return await RecordHistory.ReadAsync(identity,document,version,value=>Deserialize<T>(value));
    }

    // One existing canonical history format; native SQL owner must lock or CAS the original.
    private async Task<string> StageRecordHistoryAsync<T>(T proposed,string? original) where T:class,IHolonBase
    {
        // Native optimistic revisions advance on ALL holon writes, independently of logical history.
        if(proposed is IHolon){
            proposed=Core.Serialization.StoredRecordJson.Project<T>(proposed);
            Core.Holons.CosmicConcurrency.SetProviderRevisionId((IHolon)proposed,Core.Enums.ProviderType.OracleDBOASIS,Guid.NewGuid());
        }
        JObject? previous=null;
        if(original!=null){
            var stored=Deserialize<T>(original!);
            if(stored.Id!=proposed.Id)throw new InvalidDataException("Oracle stored record identity differs from proposed native key.");
            previous=JObject.Parse(original);
        }
        if(!IsVersionControlEnabled&&previous?.Property(EmbeddedRecordHistory.PropertyName)==null)return Serialize(proposed);
        if(previous!=null){
            if(previous["id"]!=null&&previous["id"]!.Value<string>()!=proposed.Id.ToString())throw new InvalidDataException("Oracle embedded history key differs from native identity.");
            previous["id"]=proposed.Id.ToString();
        }
        var next=await RecordHistory.StageAsync(proposed,previous,json=>Deserialize<T>(json),row=>Serialize(row));
        return next.ToString(Newtonsoft.Json.Formatting.None);
    }
}
