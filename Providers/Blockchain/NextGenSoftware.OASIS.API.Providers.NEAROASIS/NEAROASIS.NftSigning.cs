using System;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.Providers.NEAROASIS;

public partial class NEAROASIS
{
    private static void ValidateNftSigningInputs(string seed)
    {
        if(!string.IsNullOrWhiteSpace(seed))throw new NotSupportedException("NEAR NFT signing currently requires an explicit Ed25519 secret key, not an ignored seed phrase.");
    }

    private async Task<(string Account,string Key)> ResolveNftSignerAsync(string owner,string privateKey,string publicKey)
    {
        if(string.IsNullOrWhiteSpace(owner))throw new ArgumentException("A native NFT owner account is required.");
        var key=privateKey;
        if(string.IsNullOrWhiteSpace(key))
        {
            if(!string.Equals(owner,_accountId,StringComparison.Ordinal))
                throw new ArgumentException("An independent NFT owner requires its own signing key; service credentials cannot substitute for it.");
            key=_privateKey;
        }
        if(string.IsNullOrWhiteSpace(key))throw new ArgumentException("A NEAR Ed25519 signing key is required.");
        if(!string.IsNullOrWhiteSpace(privateKey)||!string.IsNullOrWhiteSpace(publicKey))
        {
            var derived=await _sdk.DerivePublicKeyAsync(key);
            if(!string.IsNullOrWhiteSpace(publicKey)&&!string.Equals(publicKey,derived.PublicKey,StringComparison.Ordinal))
                throw new ArgumentException("The supplied NEAR public key does not match its signing key.");
        }
        // The actual owner account is authoritative, including named accounts;
        // the native SDK/access-key check enforces account/key authorization.
        return(owner,key);
    }
}
