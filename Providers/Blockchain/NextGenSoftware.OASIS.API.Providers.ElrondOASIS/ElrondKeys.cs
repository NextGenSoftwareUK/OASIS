extern alias BouncyCastleCryptography;
using System;
using NBitcoin;
using NextGenSoftware.OASIS.API.Core.Helpers.Crypto;
using Ed25519PrivateKeyParameters = BouncyCastleCryptography::Org.BouncyCastle.Crypto.Parameters.Ed25519PrivateKeyParameters;

namespace NextGenSoftware.OASIS.API.Providers.ElrondOASIS;

/// <summary>
/// MultiversX (Elrond) key material: BIP-39 mnemonic, SLIP-10 Ed25519 on m/44'/508'/0'/0'/{index}', and
/// bech32 "erd1" addresses of the 32-byte Ed25519 public key. Secret keys are 32-byte hex, as MultiversX uses.
/// </summary>
public static class ElrondKeys
{
    public static string NewMnemonic() => new Mnemonic(Wordlist.English, WordCount.TwentyFour).ToString();

    public static (string SecretKeyHex, string PublicKeyHex, string Address) FromMnemonic(string mnemonic, uint index = 0)
    {
        if (string.IsNullOrWhiteSpace(mnemonic)) throw new ArgumentException("A BIP-39 mnemonic is required.", nameof(mnemonic));
        var phrase = new Mnemonic(mnemonic.Trim().ToLowerInvariant(), Wordlist.English);
        if (!phrase.IsValidChecksum) throw new ArgumentException("The mnemonic checksum is invalid.", nameof(mnemonic));
        return FromSecretKey(Slip10Ed25519.DerivePrivateKey(phrase.DeriveSeed(), 44, 508, 0, 0, index));
    }

    public static (string SecretKeyHex, string PublicKeyHex, string Address) FromSecretKey(byte[] secretKey)
    {
        if (secretKey == null || secretKey.Length != 32) throw new ArgumentException("An Ed25519 secret key is 32 bytes.", nameof(secretKey));
        var publicKey = new Ed25519PrivateKeyParameters(secretKey, 0).GeneratePublicKey().GetEncoded();
        return (Convert.ToHexString(secretKey).ToLowerInvariant(), Convert.ToHexString(publicKey).ToLowerInvariant(), Address(publicKey));
    }

    public static string Address(byte[] publicKey)
    {
        if (publicKey == null || publicKey.Length != 32) throw new ArgumentException("An Ed25519 public key is 32 bytes.", nameof(publicKey));
        return Bech32.Encode("erd", publicKey);
    }
}
