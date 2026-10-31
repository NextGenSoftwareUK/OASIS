using Solnet.Wallet;

namespace NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Infrastructure;

internal static class SolanaSigningIdentity
{
    internal static Account FromPrivateKey(string key,string address)
    {
        if(string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(address))
            throw new ArgumentException("An explicit signing key and its public address are required.");
        return Validate(new Account(key,address),address);
    }

    internal static Account ResolveNftOwner(string key,string address,string seed,Account serviceOwner)
    {
        if(!string.IsNullOrWhiteSpace(seed))
        {
            if(!string.IsNullOrWhiteSpace(key)) throw new ArgumentException("Supply one signing-key source, not both a seed phrase and private key.");
            return Validate(new Wallet(seed).GetAccount(0),address);
        }
        if(!string.IsNullOrWhiteSpace(key)) return FromPrivateKey(key,address);
        return Validate(serviceOwner,address);
    }

    private static Account Validate(Account signer,string address)
    {
        ArgumentNullException.ThrowIfNull(signer);
        if(!string.IsNullOrWhiteSpace(address))
        {
            var expected=new PublicKey(address);
            if(expected.KeyBytes.Length!=32 || expected.Key!=signer.PublicKey.Key)
                throw new ArgumentException("Solana signer does not match requested owner.");
        }
        var challenge=System.Text.Encoding.UTF8.GetBytes("oasis/solana/signing-identity/v1");
        if(!signer.Verify(challenge,signer.Sign(challenge))) throw new ArgumentException("Signing key does not match its public address.");
        return signer;
    }
}
