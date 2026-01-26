using System.Security.Cryptography;
using NBitcoin;
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
    /// Derives a private key from a mnemonic phrase
    /// </summary>
    public static PrivateKey GetPrivateKey(Mnemonic mnemonic)
    {
        // NBitcoin Mnemonic doesn't have ToSeed() - use DeriveExtKey() instead
        var extKey = mnemonic.DeriveExtKey();
        var seed = extKey.PrivateKey.ToBytes();
        // RadixEngineToolkit PrivateKey may not have Ed25519 nested type
        // Use PrivateKey constructor directly with bytes
        return new PrivateKey(seed[..32]);
    }

    /// <summary>
    /// Generates a random nonce for transactions
    /// </summary>
    public static uint RandomNonce()
    {
        return (uint)RandomNumberGenerator.GetInt32(0, int.MaxValue);
    }

    /// <summary>
    /// Gets a private key from hex string
    /// </summary>
    public static PrivateKey GetPrivateKeyFromHex(string hexPrivateKey)
    {
        var privateKeyBytes = Convert.FromHexString(hexPrivateKey);
        return new PrivateKey.Ed25519(privateKeyBytes);
    }
}

