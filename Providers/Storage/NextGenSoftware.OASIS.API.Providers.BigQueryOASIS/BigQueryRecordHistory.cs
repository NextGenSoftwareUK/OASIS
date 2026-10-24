using System;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.BigQueryOASIS;

public partial class BigQueryOASIS
{
    private static readonly EmbeddedRecordHistory RecordHistory=new();

    private static void ValidateRecordWrite(IHolonBase source)
    {
        ArgumentNullException.ThrowIfNull(source);
        if(source.Version<0)throw new ArgumentOutOfRangeException(nameof(source.Version));
    }

    private static async Task<T?> DecodeRecordVersionAsync<T>(string raw,int version)where T:class,IHolonBase
    {
        if(raw.Trim()=="null")throw new InvalidDataException("BigQuery stored record document must not be null.");
        var document=OasisJson.ParseStoredObject(raw);
        var payload=RecordHistory.CanonicalPayload(document);
        var current=DecodeCanonical<T>(payload.ToString(Newtonsoft.Json.Formatting.None));
        if(current==null||current.Version<0)throw new InvalidDataException("BigQuery canonical record or version is invalid.");
        if(document["id"]!=null&&document["id"]!.Value<string>()!=current.Id.ToString())throw new InvalidDataException("BigQuery history envelope identity differs from its canonical record.");
        document["id"]=current.Id.ToString();
        if(document.Property(EmbeddedRecordHistory.PropertyName)==null)return version==0||current.Version==version?current:null;
        return await RecordHistory.ReadAsync(current.Id,document,version,DecodeCanonical<T>);
    }

    private static T DecodeCanonical<T>(string json)where T:class,IHolonBase
    {
        if(typeof(T)==typeof(IAvatar))return (T)(object)Core.Serialization.StoredRecordJson.DeserializeStored<Avatar>(json);
        if(typeof(T)==typeof(IAvatarDetail))return (T)(object)Core.Serialization.StoredRecordJson.DeserializeStored<AvatarDetail>(json);
        if(typeof(T)==typeof(IHolon))return (T)(object)Core.Serialization.StoredRecordJson.DeserializeStored<Holon>(json);
        return Core.Serialization.StoredRecordJson.DeserializeStored<T>(json);
    }

    private async Task<(T? Record,string? Raw)> ObserveRecordAsync<T>(string table,Guid id)where T:class,IHolonBase
    {
        string? original=null;
        var current=UniqueRecord(await ReadRowsAsync<T>($"SELECT doc FROM {Table(table)} WHERE id = @id LIMIT 2",new[]{StringParameter("id",id.ToString())},(_,raw)=>original=raw));
        if(current!=null&&current.Id!=id)throw new InvalidDataException("BigQuery native identity differs from its canonical record.");
        return(current,original);
    }

    private async Task<(T Record,string Document)> StageRecordHistoryAsync<T>(T proposed,string? original)where T:class,IHolonBase
    {
        JObject? previous=null;
        if(original!=null)
        {
            var stored=await DecodeRecordVersionAsync<T>(original,0);
            if(stored==null||stored.Id!=proposed.Id)throw new InvalidDataException("BigQuery stored history identity differs from proposed record.");
            if(stored.IsDeleted&&!proposed.IsDeleted)throw new InvalidOperationException("An active save cannot implicitly resurrect a BigQuery tombstone.");
            previous=OasisJson.ParseStoredObject(original);previous["id"]=proposed.Id.ToString();
        }
        if(!IsVersionControlEnabled&&previous?.Property(EmbeddedRecordHistory.PropertyName)==null)
            return(proposed,Core.Serialization.StoredRecordJson.Serialize(proposed));
        var next=await RecordHistory.StageAsync(proposed,previous,DecodeCanonical<T>,row=>Core.Serialization.StoredRecordJson.Serialize(row));
        return(DecodeCanonical<T>(RecordHistory.CanonicalPayload(next).ToString(Newtonsoft.Json.Formatting.None)),next.ToString(Newtonsoft.Json.Formatting.None));
    }

    private static void PublishRecordVersion(IHolonBase source,IHolonBase stored)
    {
        source.Version=stored.Version;source.VersionId=stored.VersionId;source.PreviousVersionId=stored.PreviousVersionId;
    }

    private async Task SoftDeleteRecordAsync<T>(string table,Guid id)where T:class,IHolonBase
    {
        var observed=await ObserveRecordAsync<T>(table,id);
        if(observed.Record==null)throw new System.Collections.Generic.KeyNotFoundException("BigQuery record to delete was not found.");
        observed.Record.DeletedDate=DateTime.UtcNow;
        var written=await SaveAcknowledgedRecordAsync(observed.Record,table,observationProvided:true,observedRaw:observed.Raw);
        if(written.IsError||!written.IsSaved)throw written.Exception??new InvalidDataException("BigQuery soft delete did not receive a complete save acknowledgement.");
    }

    private static string? IndexedRecordValue(IHolonBase record,string field)=>field switch
    {
        "Username"=>record is IAvatar avatar?avatar.Username:record is IAvatarDetail detail?detail.Username:null,
        "Email"=>record is IAvatar avatar?avatar.Email:record is IAvatarDetail detail?detail.Email:null,
        "ParentHolonId"=>record is IHolon holon?holon.ParentHolonId.ToString():null,
        _=>throw new ArgumentException("Unsupported BigQuery indexed field.",nameof(field))
    };
}
