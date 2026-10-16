using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

/// <summary>Exact stored-wallet-key lookup; never decrypts, logs or exposes the selector.</summary>
public static class StoredAvatarWalletSelector
{
    public static async Task<OASISResult<IAvatar>> SelectAsync(string key,bool privateKey,int version,
        Func<int,Task<OASISResult<IEnumerable<IAvatar>>>> load)
    {
        var result=new OASISResult<IAvatar>();
        try
        {
            if(string.IsNullOrWhiteSpace(key)) throw new ArgumentException("A nonempty wallet key selector is required.",nameof(key));
            if(version<0) throw new ArgumentOutOfRangeException(nameof(version));
            var source=await load(version);
            if(source==null) throw new InvalidDataException("Avatar collection result is missing.");
            if(source.IsError) return OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<IEnumerable<IAvatar>,IAvatar>(source);
            if(source.Result==null) throw new InvalidDataException("Avatar collection payload is missing.");
            var matches=source.Result.Select(avatar=>avatar ?? throw new InvalidDataException("Avatar collection contains a null record."))
                .Where(avatar=>!avatar.IsDeleted && avatar.ProviderWallets?.Values.Any(wallets=>
                wallets?.Any(wallet=>wallet!=null && string.Equals(privateKey ? wallet.PrivateKey:wallet.PublicKey,key,StringComparison.Ordinal))==true)==true)
                .Take(2).ToList();
            if(matches.Count>1) throw new StorageIdentityAmbiguousException();
            if(matches.Count==0)
            {
                result.IsError=true;result.ErrorCode=OASISResultErrorCodes.RecordNotFound;result.Message="No avatar matches the wallet key selector.";
            }
            else {result.Result=matches[0];result.IsLoaded=true;}
        }
        catch(Exception cause)
        {
            OASISErrorHandling.HandleError(ref result,"Avatar wallet key lookup failed.",cause);
            if(cause is StorageIdentityAmbiguousException) result.ErrorCode=OASISResultErrorCodes.AmbiguousIdentity;
            else if(cause is ArgumentException || cause is NotSupportedException) result.ErrorCode="HYPERDRIVE_PAYLOAD_INVALID";
        }
        return result;
    }
}
