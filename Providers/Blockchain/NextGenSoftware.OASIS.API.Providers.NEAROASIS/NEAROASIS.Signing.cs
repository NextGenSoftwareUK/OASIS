using System;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.Providers.NEAROASIS;

public partial class NEAROASIS
{
    private static void ValidateNearSigningInputs(string seed)
    {
        if(!string.IsNullOrWhiteSpace(seed))throw new NotSupportedException("NEAR signing currently requires an explicit Ed25519 secret key, not an ignored seed phrase.");
    }

    private async Task<(string Account,string Key)> ResolveNearSignerAsync(string owner,string privateKey,string publicKey,NearKeyPair knownKey=null)
    {
        if(string.IsNullOrWhiteSpace(owner))throw new ArgumentException("A native owner account is required.");
        var key=privateKey;
        if(string.IsNullOrWhiteSpace(key))
        {
            if(!string.Equals(owner,_accountId,StringComparison.Ordinal))
                throw new ArgumentException("An independent owner requires its own signing key; service credentials cannot substitute for it.");
            key=_privateKey;
        }
        if(string.IsNullOrWhiteSpace(key))throw new ArgumentException("A NEAR Ed25519 signing key is required.");
        if(!string.IsNullOrWhiteSpace(privateKey)||!string.IsNullOrWhiteSpace(publicKey))
        {
            if(knownKey!=null&&knownKey.PrivateKey!=key)throw new ArgumentException("Pre-derived NEAR signing identity differs from the selected key.");
            var derived=knownKey??await _sdk.DerivePublicKeyAsync(key);
            if(!string.IsNullOrWhiteSpace(publicKey)&&!string.Equals(publicKey,derived.PublicKey,StringComparison.Ordinal))
                throw new ArgumentException("The supplied NEAR public key does not match its signing key.");
        }
        // Use an explicit account scope or an identity resolved from native state/SDK;
        // never guess a named account from a public key. The chain checks key authority.
        return(owner,key);
    }
}
