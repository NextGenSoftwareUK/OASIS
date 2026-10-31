using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

/// <summary>Unique profile lookup over a provider-owned versioned collection, not credential authentication.</summary>
public static class StoredRecordIdentityLookup
{
    public static async Task<OASISResult<T>> SelectAsync<T>(string query,int version,
        Func<int,Task<OASISResult<IEnumerable<T>>>> load,Func<T,string> identity)where T:class,IHolonBase
    {
        var result=new OASISResult<T>();
        try{
            if(string.IsNullOrWhiteSpace(query))throw new ArgumentException("A nonblank profile identity is required.",nameof(query));
            if(version<0)throw new ArgumentOutOfRangeException(nameof(version));
            ArgumentNullException.ThrowIfNull(load);ArgumentNullException.ThrowIfNull(identity);
            var selected=query.Trim();var source=await load(version);
            if(source==null)throw new InvalidDataException("Profile collection result is missing.");
            if(source.IsError)return OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<IEnumerable<T>,T>(source);
            if(source.Result==null)throw new InvalidDataException("Profile collection payload is missing.");
            // Version and deleted-record visibility belong to the provider's collection owner.
            var matches=source.Result.Select(row=>row??throw new InvalidDataException("Profile collection contains a null record."))
                .Where(row=>string.Equals(identity(row)?.Trim(),selected,StringComparison.OrdinalIgnoreCase)).Take(2).ToArray();
            if(matches.Length>1)throw new StorageIdentityAmbiguousException();
            if(matches.Length==0){result.IsError=true;result.ErrorCode=OASISResultErrorCodes.RecordNotFound;result.Message="Profile identity was not found.";}
            else{result.Result=matches[0];result.IsLoaded=true;result.LoadedCount=1;}
        }catch(Exception cause){
            OASISErrorHandling.HandleError(ref result,"Profile identity lookup failed.",cause);
            if(cause is StorageIdentityAmbiguousException)result.ErrorCode=OASISResultErrorCodes.AmbiguousIdentity;
            else if(cause is ArgumentException)result.ErrorCode="HYPERDRIVE_PAYLOAD_INVALID";
        }
        return result;
    }
}
