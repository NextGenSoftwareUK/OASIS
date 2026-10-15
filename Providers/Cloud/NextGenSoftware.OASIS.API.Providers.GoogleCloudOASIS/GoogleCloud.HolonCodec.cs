using System;
using System.Collections.Generic;
using System.IO;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.GoogleCloudOASIS;

public partial class GoogleCloudOASIS
{
    private async Task<OASISResult<IHolon>> ReadCanonicalHolonByIdAsync(Guid id,bool children,bool recursive,int depth,bool fromProvider,int version)
    {
        var result=new OASISResult<IHolon>();
        try {
            if(id==Guid.Empty) throw new ArgumentException("A nonempty holon identity is required.");
            if(depth<0) throw new ArgumentOutOfRangeException(nameof(depth));
            var ready=await EnsureDetailStoreAsync(version);
            if(ready.IsError) return OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<bool,IHolon>(ready);
            var document=await _firestoreDb.Collection("holons").Document(id.ToString()).GetSnapshotAsync();
            var holon=document.Exists ? DecodeHolon(document.ConvertTo<Dictionary<string,object>>(),document.Id):null;
            if(holon==null || !holon.IsActive) {result.IsError=true;result.ErrorCode=OASISResultErrorCodes.RecordNotFound;result.Message="Firestore holon not found.";}
            else
            {
                IEnumerable<IHolon> records = new[] { holon };
                if (children && fromProvider)
                {
                    var loaded = await LoadAllHolonsAsync(loadChildren:false,version:version);
                    if (loaded.IsError)
                        return OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<IEnumerable<IHolon>,IHolon>(loaded);
                    records = loaded.Result;
                }
                var project = HolonGraphProjection.Create(records,children,recursive,depth,0,fromProvider,
                    source => OasisJson.Deserialize<Holon>(OasisJson.Serialize(source)));
                result.Result=project(holon);
                result.IsLoaded=true;
            }
        }
        catch(Exception cause) {OASISErrorHandling.HandleError(ref result,cause.Message,cause);}
        return result;
    }

    private static Holon DecodeHolon(Dictionary<string,object> fields,string documentId)
    {
        if(!fields.TryGetValue("data_json",out var value) || value is not string json)
            throw new InvalidDataException("Firestore holon lacks canonical data_json; old mappings require explicit review.");
        var holon=OasisJson.Deserialize<Holon>(json) ?? throw new InvalidDataException("Firestore holon payload is null.");
        if(holon.Id==Guid.Empty || holon.Id.ToString()!=documentId || !fields.TryGetValue("id",out var id) || !Equals(id,documentId))
            throw new InvalidDataException("Firestore holon identity does not match document key.");
        return holon;
    }
}
