using System;
using System.IO;
using System.Numerics;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Utilities;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.EthereumOASIS;

public partial class EthereumOASIS
{
    private static readonly EmbeddedRecordHistory RecordHistory=new();

    // No persistence or caller publication: the checked writer must use this same observed ciphertext.
    private static async Task<(T Record,string Ciphertext)> StageStorageHistoryAsync<T>(string kind,T proposed,
        BigInteger observedEntityId,string observedIdentity,string observedCiphertext,bool createHistory) where T:class,IHolonBase
        =>await StageStorageHistoryWithPlanAsync(kind,proposed,observedEntityId,observedIdentity,observedCiphertext,createHistory,null);

    private static async Task<(T Record,string Ciphertext)> StageStorageHistoryWithPlanAsync<T>(string kind,T proposed,
        BigInteger observedEntityId,string observedIdentity,string observedCiphertext,bool createHistory,PlannedHolonWrite planned) where T:class,IHolonBase
    {
        ArgumentNullException.ThrowIfNull(proposed);
        var type=kind switch {"avatar"=>typeof(Avatar),"avatar-detail"=>typeof(AvatarDetail),"holon"=>typeof(Holon),_=>null};
        if(type!=typeof(T))throw new ArgumentException("Ethereum record kind/type mismatch.");
        if(proposed.Id==Guid.Empty || proposed.Version<0)throw new ArgumentException("Explicit identity and nonnegative version required.");
        var entityId=new BigInteger(HashUtility.GetNumericHash(proposed.Id.ToString()));
        JObject prior=null;
        using var codec=OpenPrivateStorageCodec();
        var absent=observedEntityId==0 && string.IsNullOrEmpty(observedIdentity) && string.IsNullOrEmpty(observedCiphertext);
        if(!absent){
            if(observedEntityId!=entityId || observedIdentity!=proposed.Id.ToString())throw new InvalidDataException("Ethereum observed history identity mismatch.");
            prior=DecryptStorageDocument<T>(kind,entityId,observedCiphertext,proposed.Id).Document;
        }
        var detached=OasisJson.DeserializeStored<T>(SerializeCanonicalEmbeddedRecord(proposed).ToString(Formatting.None));
        if(type==typeof(Holon)&&detached is Holon holon){
            if(planned!=null&&planned.Snapshot.Id!=holon.Id)throw new InvalidDataException("Ethereum captured plan differs from staged identity.");
            var observed=prior==null?null:OasisJson.DeserializeStored<Holon>(RecordHistory.CanonicalPayload(prior).ToString(Formatting.None));
            holon.ChildRoleManifestJson=HolonChildRolePersistence.Resolve(planned??new PlannedHolonWrite(holon,holon),
                HolonChildRoleWriteKind.SourceSave,prior!=null,observed?.ChildRoleManifestJson);
        }
        var document=createHistory || prior?.Property(EmbeddedRecordHistory.PropertyName)!=null
            ?await RecordHistory.StageAsync(detached,prior,OasisJson.DeserializeStored<T>,row=>SerializeCanonicalEmbeddedRecord(row).ToString(Formatting.None))
            :SerializeCanonicalEmbeddedRecord(detached);
        var record=OasisJson.DeserializeStored<T>(document.ToString(Formatting.None));
        return (record,codec.Encrypt(PrivateStorageContext(kind,entityId),document.ToString(Formatting.None)));
    }
}
