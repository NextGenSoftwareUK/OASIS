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
    /// Derives a private key from a mnemonic phrase using Radix BIP44 path m/44'/1022'/0'/0/0
    /// </summary>
    public static PrivateKey GetPrivateKey(Mnemonic mnemonic)
    {
        // Derive using Radix BIP44 path: m/44'/1022'/0'/0/0
        var extKey = mnemonic.DeriveExtKey()
            .Derive(new KeyPath("m/44'/1022'/0'/0/0"));
        var keyBytes = extKey.PrivateKey.ToBytes();

        var privateKeyBytes = new byte[32];
        Array.Copy(keyBytes, 0, privateKeyBytes, 0, Math.Min(keyBytes.Length, 32));

        return PrivateKey.NewEd25519(privateKeyBytes);
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

