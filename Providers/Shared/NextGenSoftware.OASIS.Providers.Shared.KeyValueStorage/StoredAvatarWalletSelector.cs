using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

/// <summary>Stored credential selection; never decrypts, logs or exposes the selector.</summary>
public static class StoredAvatarWalletSelector
{
    public static Task<OASISResult<IAvatar>> SelectAsync(string key,bool privateKey,int version,
        Func<int,Task<OASISResult<IEnumerable<IAvatar>>>> load)
        => SelectMatchingAsync(key,version,load,avatar=>avatar.ProviderWallets?.Values.Any(wallets=>
            wallets?.Any(wallet=>wallet!=null && string.Equals(privateKey ? wallet.PrivateKey:wallet.PublicKey,key,StringComparison.Ordinal))==true)==true);

    public static async Task<OASISResult<IAvatar>> SelectMatchingAsync(string key,int version,
        Func<int,Task<OASISResult<IEnumerable<IAvatar>>>> load,Func<IAvatar,bool> matchesSelector)
    {
        var result=new OASISResult<IAvatar>();
        try
        {
            if(string.IsNullOrWhiteSpace(key)) throw new ArgumentException("A nonempty avatar credential selector is required.",nameof(key));
            ArgumentNullException.ThrowIfNull(matchesSelector);
            if(version!=0) throw new ArgumentOutOfRangeException(nameof(version),"Credential selectors require latest version zero; archived records are not authentication authority.");
            var source=await load(version);
            if(source==null) throw new InvalidDataException("Avatar collection result is missing.");
            if(source.IsError) return OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<IEnumerable<IAvatar>,IAvatar>(source);
            if(source.Result==null) throw new InvalidDataException("Avatar collection payload is missing.");
            var matches=source.Result.Select(avatar=>avatar ?? throw new InvalidDataException("Avatar collection contains a null record."))
                .Where(avatar=>!avatar.IsDeleted && matchesSelector(avatar))
                .Take(2).ToList();
            if(matches.Count>1) throw new StorageIdentityAmbiguousException();
            if(matches.Count==0)
            {
                result.IsError=true;result.ErrorCode=OASISResultErrorCodes.RecordNotFound;result.Message="No avatar matches the credential selector.";
            }
            else {result.Result=matches[0];result.IsLoaded=true;}
        }
        catch(Exception cause)
        {
            OASISErrorHandling.HandleError(ref result,"Avatar credential lookup failed.",cause);
            if(cause is StorageIdentityAmbiguousException) result.ErrorCode=OASISResultErrorCodes.AmbiguousIdentity;
            else if(cause is ArgumentException || cause is NotSupportedException) result.ErrorCode="HYPERDRIVE_PAYLOAD_INVALID";
        }
        return result;
    }
}
