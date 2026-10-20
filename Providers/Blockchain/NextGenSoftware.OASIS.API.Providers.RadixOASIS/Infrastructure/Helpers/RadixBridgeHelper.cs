using System.Security.Cryptography;
using NBitcoin;
using NextGenSoftware.OASIS.API.Core.Helpers.Crypto;
using RadixEngineToolkit;

namespace NextGenSoftware.OASIS.API.Providers.RadixOASIS.Infrastructure.Helpers;

/// <summary>
/// Helper class for Radix bridge operations
/// </summary>
public static class RadixBridgeHelper
{
    public const string MainNet = "mainnet";
    public const string StokeNet = "stokenet";
    public const string MainNetXrdAddress = "resource_rdx1tknxxxxxxxxxradxrdxxxxxxxxx009923554798xxxxxxxxxradxrd";
    public const string StokeNetXrdAddress = "resource_tdx_2_1tknxxxxxxxxxradxrdxxxxxxxxx009923554798xxxxxxxxxtfd2jc";

    /// <summary>
    /// Derives the Ed25519 account transaction-signing key the Radix wallet uses for a BIP-39 mnemonic:
    /// SLIP-10 on the CAP-26 path m/44'/1022'/{networkId}'/525'/1460'/{index}' (525 = account, 1460 = transaction signing).
    /// The previous version ran secp256k1 BIP-32 derivation and reinterpreted its bytes as an Ed25519 key, so phrases
    /// did not restore the accounts the Radix wallet shows.
    /// </summary>
    public static PrivateKey GetPrivateKey(Mnemonic mnemonic, byte networkId, uint index = 0)
    {
        ArgumentNullException.ThrowIfNull(mnemonic);
        var seed = mnemonic.DeriveSeed();
        return PrivateKey.NewEd25519(Slip10Ed25519.DerivePrivateKey(seed, 44, 1022, networkId, 525, 1460, index));
    }

    /// <summary>The Radix virtual (preallocated) account address controlled by an Ed25519 key on a network.</summary>
    public static string AccountAddress(PrivateKey privateKey, byte networkId)
    {
        ArgumentNullException.ThrowIfNull(privateKey);
        using var address = Address.PreallocatedAccountAddressFromPublicKey(privateKey.PublicKey(), networkId);
        return address.AddressString();
    }

    /// <summary>
    /// Generates a random nonce for transactions
    /// </summary>
    public static uint RandomNonce()
    {
        return (uint)RandomNumberGenerator.GetInt32(0, int.MaxValue);
    }

    /// <summary>
    /// Restores a private key from a 64-character hex string (Ed25519)
    /// </summary>
    public static PrivateKey GetPrivateKeyFromHex(string hexPrivateKey)
    {
        if (string.IsNullOrWhiteSpace(hexPrivateKey))
            throw new ArgumentException("Hex private key must not be null or empty.", nameof(hexPrivateKey));

        var bytes = new byte[hexPrivateKey.Length / 2];
        for (int i = 0; i < bytes.Length; i++)
            bytes[i] = Convert.ToByte(hexPrivateKey.Substring(i * 2, 2), 16);

        return PrivateKey.NewEd25519(bytes);
    }
}

