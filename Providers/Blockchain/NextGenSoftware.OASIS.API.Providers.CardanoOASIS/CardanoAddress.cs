extern alias BouncyCastleCryptography;
using System;
using NextGenSoftware.OASIS.API.Core.Helpers.Crypto;
using Blake2bDigest = BouncyCastleCryptography::Org.BouncyCastle.Crypto.Digests.Blake2bDigest;

namespace NextGenSoftware.OASIS.API.Providers.CardanoOASIS;

/// <summary>
/// CIP-19 enterprise addresses (payment key hash, no stake part): header 0x6{network} followed by the
/// Blake2b-224 hash of the Ed25519 verification key, bech32 with "addr" (mainnet) or "addr_test".
/// </summary>
public static class CardanoAddress
{
    public static string Enterprise(byte[] verificationKey, bool mainnet)
    {
        if (verificationKey == null || verificationKey.Length != 32) throw new ArgumentException("A Cardano verification key is 32 bytes.", nameof(verificationKey));
        var payload = new byte[29];
        payload[0] = (byte)(0x60 | (mainnet ? 1 : 0));
        var digest = new Blake2bDigest(224);
        digest.BlockUpdate(verificationKey, 0, verificationKey.Length);
        digest.DoFinal(payload, 1);
        return Bech32.Encode(mainnet ? "addr" : "addr_test", payload);
    }
}
