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
        
        // RadixEngineToolkit PrivateKey uses Ed25519 - create from 32-byte seed
        // Ensure we have exactly 32 bytes for Ed25519 private key
        var privateKeyBytes = new byte[32];
        if (seed.Length >= 32)
        {
            Array.Copy(seed, 0, privateKeyBytes, 0, 32);
        }
        else
        {
            // If seed is shorter, pad with zeros (shouldn't happen with NBitcoin)
            Array.Copy(seed, 0, privateKeyBytes, 0, seed.Length);
        }
        
        // Create PrivateKey from bytes using RadixEngineToolkit
        // RadixEngineToolkit PrivateKey constructor requires PrivateKeySafeHandle, not byte[]
        // The proper way to create PrivateKey from bytes requires using RadixEngineToolkit's internal API
        // which uses PrivateKeySafeHandle. Since this is not directly accessible, we need to use
        // RadixEngineToolkit's key generation API or a workaround.
        // TODO: Implement proper PrivateKey creation using RadixEngineToolkit PrivateKeySafeHandle API
        // For now, generate a new PrivateKey using RadixEngineToolkit's key generation
        // Note: This doesn't restore the exact key from mnemonic, but creates a new one
        // Proper implementation would use RadixEngineToolkit's key derivation from seed
        throw new NotImplementedException("PrivateKey creation from mnemonic bytes requires RadixEngineToolkit PrivateKeySafeHandle API which is not directly accessible. Use RadixEngineToolkit's key generation API instead.");
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
        // RadixEngineToolkit PrivateKey constructor requires PrivateKeySafeHandle, not byte[]
        // The proper way to create PrivateKey from hex requires using RadixEngineToolkit's internal API
        // which uses PrivateKeySafeHandle. Since this is not directly accessible, we need to use
        // RadixEngineToolkit's key restoration API.
        // TODO: Implement proper PrivateKey creation from hex using RadixEngineToolkit PrivateKeySafeHandle API
        throw new NotImplementedException("PrivateKey creation from hex requires RadixEngineToolkit PrivateKeySafeHandle API which is not directly accessible. Use RadixEngineToolkit's key restoration API instead.");
    }
}

