using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

/// <summary>Coordinates existing wallet lookups, not signing, authorization or address syntax validation.
/// The minter address is used only when no recipient selector was supplied, never after lookup failure.</summary>
public static class NftMintRecipient
{
    public static async Task<OASISResult<string>> ResolveAsync(IMintWeb3NFTRequest request,string minterAddress,
        Func<Guid,Task<OASISResult<IProviderWallet>>> byId,
        Func<string,Task<OASISResult<IProviderWallet>>> byUsername,
        Func<string,Task<OASISResult<IProviderWallet>>> byEmail,
        Func<string,string,bool> sameAddress=null)
    {
        var result=new OASISResult<string>();
        try {
            ArgumentNullException.ThrowIfNull(request);
            // Own all selectors before the first wallet await.
            var address=request.SendToAddressAfterMinting;var id=request.SendToAvatarAfterMintingId;
            var username=request.SendToAvatarAfterMintingUsername;var email=request.SendToAvatarAfterMintingEmail;
            var addresses=new List<string>();
            if(!string.IsNullOrWhiteSpace(address))addresses.Add(address);
            var lookups=new List<Func<Task<OASISResult<IProviderWallet>>>>();
            if(id!=Guid.Empty)lookups.Add(()=>byId(id));
            if(!string.IsNullOrWhiteSpace(username))lookups.Add(()=>byUsername(username));
            if(!string.IsNullOrWhiteSpace(email))lookups.Add(()=>byEmail(email));
            foreach(var lookup in lookups){
                var wallet=await lookup();
                if(wallet==null)throw new InvalidOperationException("Recipient wallet lookup returned no result envelope.");
                if(wallet.IsError)return OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<IProviderWallet,string>(wallet);
                if(wallet.Result==null||string.IsNullOrWhiteSpace(wallet.Result.WalletAddress))
                    throw new InvalidOperationException("Recipient wallet lookup returned no wallet address.");
                addresses.Add(wallet.Result.WalletAddress);
            }
            sameAddress??=(left,right)=>StringComparer.Ordinal.Equals(left,right);
            foreach(var candidate in addresses)
                if(!sameAddress(addresses[0],candidate))throw new ArgumentException("Recipient selectors resolve to different wallets.");
            var selected=addresses.Count==0?minterAddress:addresses[0];
            if(string.IsNullOrWhiteSpace(selected))throw new InvalidOperationException("No recipient or minter wallet address is available.");
            result.Result=selected;
        }catch(Exception error){OASISErrorHandling.HandleError(ref result,"NFT recipient resolution failed.",error);}
        return result;
    }
}
