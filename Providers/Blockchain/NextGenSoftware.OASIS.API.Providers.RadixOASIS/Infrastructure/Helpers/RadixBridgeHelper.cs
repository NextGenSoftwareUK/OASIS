using System.Security.Cryptography;

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
        var seed = mnemonic.ToSeed();
        return new PrivateKey.Ed25519(seed[..32]);
    }

    /// <summary>
    /// Generates a random nonce for transactions
    /// </summary>
    public static uint RandomNonce()
    {
        return (uint)RandomNumberGenerator.GetInt32(0, int.MaxValue);
    }
}

