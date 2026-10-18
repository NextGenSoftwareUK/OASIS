using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Providers.SOLANAOASIS;

public partial class SolanaOASIS
{
    private async Task<OASISResult<T>> FindStoredIdentityAsync<T>(bool valid,
        Func<Task<OASISResult<IEnumerable<T>>>> load,Func<T,bool> predicate)
    {
        var result=new OASISResult<T>();
        if(!valid) return new OASISResult<T> {IsError=true,ErrorCode="HYPERDRIVE_PAYLOAD_INVALID",Message="A nonempty Solana identity selector is required."};
        try
        {
            var collection=await load();
            if(collection.IsError)
            {
                OASISErrorHandling.HandleError(ref result,collection.Message,collection.Exception);
                result.ErrorCode=collection.ErrorCode;
                return result;
            }
            if(collection.Result == null) throw new InvalidOperationException("Solana collection returned no authenticated records.");
            var matches=collection.Result.Where(predicate).Take(2).ToList();
            if(matches.Count>1) throw new StorageIdentityAmbiguousException();
            if(matches.Count==0) throw new KeyNotFoundException("No stored Solana record matches the requested identity.");
            result.Result=matches[0]; result.IsLoaded=true;
        }
        catch(Exception cause)
        {
            OASISErrorHandling.HandleError(ref result,"Solana authenticated identity lookup failed: "+cause.Message,cause);
            if(cause is StorageIdentityAmbiguousException) result.ErrorCode=OASISResultErrorCodes.AmbiguousIdentity;
            else if(cause is KeyNotFoundException) result.ErrorCode=OASISResultErrorCodes.RecordNotFound;
        }
        return result;
    }
}
