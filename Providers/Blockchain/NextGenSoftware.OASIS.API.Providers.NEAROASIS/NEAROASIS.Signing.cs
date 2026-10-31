using System;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.NEAROASIS;

public partial class NEAROASIS
{
    protected virtual Task<OASISResult<IProviderWallet>> LoadSigningWalletAsync(Guid actor)
        =>WalletManager.GetAvatarDefaultWalletByIdAsync(actor,Core.Enums.ProviderType.NEAROASIS,showPrivateKeys:true);

    private async Task<(string Account,string Key)> ResolveLockSignerAsync(Guid actor,string requestedFrom,string suppliedKey)
    {
        if(!string.IsNullOrWhiteSpace(suppliedKey)){
            if(string.IsNullOrWhiteSpace(requestedFrom))throw new ArgumentException("An explicit private key requires its actual NEAR account address.");
            var known=await ResolveNearExplicitKeyAsync(suppliedKey,null);
            return await ResolveNearSignerAsync(requestedFrom,suppliedKey,null,known);
        }
        if(actor!=Guid.Empty){
            var loaded=await LoadSigningWalletAsync(actor);
            if(loaded==null||loaded.IsError||loaded.IsWarning||loaded.Result==null||loaded.Result.AvatarId!=actor
                ||loaded.Result.ProviderType!=Core.Enums.ProviderType.NEAROASIS||string.IsNullOrWhiteSpace(loaded.Result.PrivateKey)
                ||string.IsNullOrWhiteSpace(loaded.Result.WalletAddress))
                throw new InvalidOperationException("The exact requesting avatar's NEAR signing wallet is required.",loaded?.Exception);
            if(!string.IsNullOrWhiteSpace(requestedFrom)&&requestedFrom!=loaded.Result.WalletAddress)
                throw new ArgumentException("Requested source account differs from the avatar's signing wallet.");
            return await ResolveNearSignerAsync(loaded.Result.WalletAddress,loaded.Result.PrivateKey,loaded.Result.PublicKey);
        }
        if(!string.IsNullOrWhiteSpace(requestedFrom)&&requestedFrom!=_accountId)
            throw new ArgumentException("An independent NEAR account requires its own signing wallet/key.");
        return await ResolveNearSignerAsync(_accountId,null,null);
    }


    private async Task<NearKeyPair> ResolveNearExplicitKeyAsync(string privateKey,string seed)
    {
        if(!string.IsNullOrWhiteSpace(seed)){
            if(!string.IsNullOrWhiteSpace(privateKey))throw new ArgumentException("Choose a NEAR mnemonic or explicit secret key, not both.");
            if(seed.Length>512 || seed.Split((char[])null,StringSplitOptions.RemoveEmptyEntries).Length is not (12 or 15 or 18 or 21 or 24))
                throw new ArgumentException("A bounded BIP39 mnemonic is required; checksum validation is owned by the NEAR SDK.");
            return await _sdk.RestoreMnemonicAsync(seed);
        }
        return string.IsNullOrWhiteSpace(privateKey)?null:await _sdk.DerivePublicKeyAsync(privateKey);
    }

    private async Task<(string Account,string Key)> ResolveNearSignerAsync(string owner,string privateKey,string publicKey,NearKeyPair knownKey=null)
    {
        if(string.IsNullOrWhiteSpace(owner))throw new ArgumentException("A native owner account is required.");
        if(knownKey!=null&&!string.IsNullOrWhiteSpace(privateKey)&&knownKey.PrivateKey!=privateKey)
            throw new ArgumentException("Pre-derived NEAR signing identity differs from the supplied key.");
        var key=knownKey?.PrivateKey??privateKey;
        if(string.IsNullOrWhiteSpace(key))
        {
            if(!string.Equals(owner,_accountId,StringComparison.Ordinal))
                throw new ArgumentException("An independent owner requires its own signing key; service credentials cannot substitute for it.");
            key=_privateKey;
        }
        if(string.IsNullOrWhiteSpace(key))throw new ArgumentException("A NEAR Ed25519 signing key is required.");
        if(knownKey!=null||!string.IsNullOrWhiteSpace(privateKey)||!string.IsNullOrWhiteSpace(publicKey))
        {
            var derived=knownKey??await _sdk.DerivePublicKeyAsync(key);
            if(!string.IsNullOrWhiteSpace(publicKey)&&!string.Equals(publicKey,derived.PublicKey,StringComparison.Ordinal))
                throw new ArgumentException("The supplied NEAR public key does not match its signing key.");
        }
        // Use an explicit account scope or an identity resolved from native state/SDK;
        // never guess a named account from a public key. The chain checks key authority.
        return(owner,key);
    }
}
