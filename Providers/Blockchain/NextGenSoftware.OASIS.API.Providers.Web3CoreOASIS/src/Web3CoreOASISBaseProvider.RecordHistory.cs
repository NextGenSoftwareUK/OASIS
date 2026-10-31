using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS;

public partial class Web3CoreOASISBaseProvider
{
    private static readonly EmbeddedRecordHistory PrivateHistory=new();

    // Detached staging only. The existing checked native writer must own persistence and publication.
    internal async Task<(T Record,byte[] Info)> StagePrivateHistoryAsync<T>(string kind,T proposal,EntityOASIS previous,bool createHistory,
        PlannedHolonWrite plannedHolon=null) where T:class,IHolonBase
    {
        ArgumentNullException.ThrowIfNull(proposal);
        var expected=kind switch{"avatar"=>typeof(Avatar),"detail"=>typeof(AvatarDetail),"holon"=>typeof(Holon),_=>null};
        if(expected!=typeof(T))throw new ArgumentException("Web3Core record kind/type mismatch.");
        if(proposal.Id==Guid.Empty)throw new ArgumentException("An explicit staged identity is required.");
        var detached=OasisJson.Deserialize<T>(OasisJson.Serialize(proposal));
        if(detached.Version<0)throw new InvalidDataException("Web3Core proposed version must not be negative.");
        JObject prior=null;
        if(previous!=null && !IsPrivateRecordAbsent(previous)){
            ValidatePrivateRecordIdentity(proposal.Id,previous);
            var decoded=DecryptPrivateDocument<T>(kind,previous);
            if(decoded.Record.Id!=proposal.Id)throw new InvalidDataException("Web3Core prior record differs from proposed identity.");
            prior=decoded.Document;
        }
        if(expected==typeof(Holon)&&detached is Holon holon){
            if(plannedHolon!=null&&plannedHolon.Snapshot.Id!=holon.Id)
                throw new InvalidDataException("Web3Core captured plan differs from the staged holon identity.");
            var observed=prior==null?null:OasisJson.DeserializeStored<Holon>(PrivateHistory.CanonicalPayload(prior).ToString(Newtonsoft.Json.Formatting.None));
            holon.ChildRoleManifestJson=HolonChildRolePersistence.Resolve(plannedHolon??new PlannedHolonWrite(holon,holon),
                HolonChildRoleWriteKind.SourceSave,prior!=null,observed?.ChildRoleManifestJson);
        }
        var document=createHistory || prior?.Property(EmbeddedRecordHistory.PropertyName)!=null
            ?await PrivateHistory.StageAsync(detached,prior,OasisJson.Deserialize<T>):OasisJson.ParseStoredObject(OasisJson.Serialize(detached));
        var staged=OasisJson.Deserialize<T>(document.ToString(Newtonsoft.Json.Formatting.None));
        var context=PrivateContext(kind,(uint)Core.Utilities.HashUtility.GetNumericHash(staged.Id.ToString()),PrivateExternalId(staged.Id));
        using var codec=OpenPrivateCodec();
        return (staged,Encoding.UTF8.GetBytes(codec.Encrypt(context,document.ToString(Newtonsoft.Json.Formatting.None))));
    }
}
