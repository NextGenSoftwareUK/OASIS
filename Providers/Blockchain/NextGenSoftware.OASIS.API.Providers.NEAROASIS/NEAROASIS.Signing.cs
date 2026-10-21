using System;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.Providers.NEAROASIS;

public partial class NEAROASIS
{
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
