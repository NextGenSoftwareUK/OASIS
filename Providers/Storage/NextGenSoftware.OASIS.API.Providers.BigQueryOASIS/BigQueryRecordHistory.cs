using System;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.BigQueryOASIS;

public partial class BigQueryOASIS
{
    private static readonly EmbeddedRecordHistory RecordHistory=new();

    private static async Task<T?> DecodeRecordVersionAsync<T>(string raw,int version)where T:class,IHolonBase
    {
        if(raw.Trim()=="null")throw new InvalidDataException("BigQuery stored record document must not be null.");
        var document=OasisJson.ParseStoredObject(raw);
        var payload=RecordHistory.CanonicalPayload(document);
        var current=Core.Serialization.StoredRecordJson.DeserializeStored<T>(payload.ToString(Newtonsoft.Json.Formatting.None));
        if(current==null||current.Version<0)throw new InvalidDataException("BigQuery canonical record or version is invalid.");
        if(document["id"]!=null&&document["id"]!.Value<string>()!=current.Id.ToString())throw new InvalidDataException("BigQuery history envelope identity differs from its canonical record.");
        document["id"]=current.Id.ToString();
        if(document.Property(EmbeddedRecordHistory.PropertyName)==null)return version==0||current.Version==version?current:null;
        return await RecordHistory.ReadAsync(current.Id,document,version,Core.Serialization.StoredRecordJson.DeserializeStored<T>);
    }

    private static string? IndexedRecordValue(IHolonBase record,string field)=>field switch
    {
        "Username"=>record is IAvatar avatar?avatar.Username:record is IAvatarDetail detail?detail.Username:null,
        "Email"=>record is IAvatar avatar?avatar.Email:record is IAvatarDetail detail?detail.Email:null,
        "ParentHolonId"=>record is IHolon holon?holon.ParentHolonId.ToString():null,
        _=>throw new ArgumentException("Unsupported BigQuery indexed field.",nameof(field))
    };
}
