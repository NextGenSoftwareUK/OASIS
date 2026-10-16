using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.IPFSOASIS;

public partial class IPFSOASIS
{
    private async Task<OASISResult<IEnumerable<IHolon>>> ReadIpfsExportAsync(Guid? owner,
        Func<HolonResume,bool> selector,int version,bool validSelector=true)
    {
        var result=new OASISResult<IEnumerable<IHolon>>();
        try
        {
            if(!validSelector || owner==Guid.Empty) throw new ArgumentException("A nonempty avatar export selector is required.");
            if(version<0) throw new ArgumentOutOfRangeException(nameof(version));
            var snapshot=await LoadLookupToJson();
            if(selector!=null)
            {
                var matches=snapshot.Where(row=>row.Value?.RecordKind=="avatar" && selector(row.Value)).Take(2).ToList();
                if(matches.Count>1) throw new StorageIdentityAmbiguousException();
                if(matches.Count==0) return new OASISResult<IEnumerable<IHolon>> {IsError=true,ErrorCount=1,
                    ErrorCode=OASISResultErrorCodes.RecordNotFound,Message="No IPFS avatar matches the export selector."};
                owner=matches[0].Value.Id;
            }
            var records=await ReadIndexSnapshotAsync<Holon>(snapshot,"holon",row=>true,version);
            result.Result=records.Where(row=>!owner.HasValue || row.CreatedByAvatarId==owner.Value || row.ParentHolonId==owner.Value).Cast<IHolon>().ToList();
            result.IsLoaded=true;
        }
        catch(Exception cause)
        {
            OASISErrorHandling.HandleError(ref result,"IPFS private export failed: "+cause.Message,cause);
            if(cause is StorageIdentityAmbiguousException) result.ErrorCode=OASISResultErrorCodes.AmbiguousIdentity;
        }
        return result;
    }
}
