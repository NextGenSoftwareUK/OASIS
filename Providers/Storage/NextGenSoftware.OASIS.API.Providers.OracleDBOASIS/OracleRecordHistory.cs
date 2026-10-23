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
    private readonly record struct NativeIdentityProjection(string? Username,string? Email);

    private static NativeIdentityProjection ReadNativeIdentityProjection(System.Data.Common.DbDataReader reader,int offset)
        =>new(reader.IsDBNull(offset)?null:reader.GetString(offset),reader.IsDBNull(offset+1)?null:reader.GetString(offset+1));

    private static void ValidateNativeIdentityProjection(IHolonBase current,NativeIdentityProjection native)
    {
        var identity=current switch{
            IAvatar avatar=>(avatar.Username,avatar.Email),
            IAvatarDetail detail=>(detail.Username,detail.Email),
            _=>throw new ArgumentException("Native identity projection requires Avatar or AvatarDetail.")
        };
        if(!string.Equals(identity.Item1,native.Username,StringComparison.Ordinal)||!string.Equals(identity.Item2,native.Email,StringComparison.Ordinal))
            throw new InvalidDataException("Oracle native identity indexes differ from their current canonical record.");
    }

    private async Task<T?> ReadRecordVersionAsync<T>(Guid identity,string json,int version,int? nativeAvatarDeleted=null,NativeIdentityProjection? nativeIdentity=null,NativeHolonProjection? nativeHolon=null) where T:class,IHolonBase
    {
        if(version<0)throw new ArgumentOutOfRangeException(nameof(version));
        var current=Deserialize<T>(json);
        if(current.Id!=identity)throw new InvalidDataException("Oracle canonical record identity differs from its native key.");
        if(nativeAvatarDeleted.HasValue)ValidateNativeAvatarProjection(current,nativeAvatarDeleted.Value);
        if(nativeIdentity.HasValue)ValidateNativeIdentityProjection(current,nativeIdentity.Value);
        if(nativeHolon is {} projection){
            if(current is not IHolon holon)throw new ArgumentException("Native holon projection requires a canonical Holon.");
            ValidateNativeHolonProjection(holon,projection.Type,projection.Deleted,projection.Parent);
        }
        var document=JObject.Parse(json);
        if(document["id"]!=null&&document["id"]!.Value<string>()!=identity.ToString())throw new InvalidDataException("Oracle embedded history identity differs from native key.");
        if(document.Property(EmbeddedRecordHistory.PropertyName)==null)return version==0||current.Version==version?current:null;
        document["id"]=identity.ToString();
        return await RecordHistory.ReadAsync(identity,document,version,value=>Deserialize<T>(value));
    }

    private static void ValidateNativeAvatarProjection(IHolonBase current,int deleted)
    {
        if(current is not Core.Holons.Avatar)throw new ArgumentException("Native avatar lifecycle projection requires a canonical Avatar.");
        if(deleted!=(current.IsDeleted?1:0))throw new InvalidDataException("Oracle native avatar lifecycle projection differs from its canonical record.");
    }

    // One existing canonical history format; native SQL owner must lock or CAS the original.
    private async Task<string> StageRecordHistoryAsync<T>(T proposed,string? original,int? nativeAvatarDeleted=null,NativeIdentityProjection? nativeIdentity=null) where T:class,IHolonBase
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
            if(nativeAvatarDeleted.HasValue)ValidateNativeAvatarProjection(stored,nativeAvatarDeleted.Value);
            if(nativeIdentity.HasValue)ValidateNativeIdentityProjection(stored,nativeIdentity.Value);
            if(stored.IsDeleted&&!proposed.IsDeleted)
                throw new Core.Helpers.StorageLifecycleConflictException("Active save cannot overwrite a committed Oracle tombstone.");
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
