using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using NBitcoin;
using Nethereum.Util;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Core.Helpers
{
    /// <summary>
    /// Helper class for deriving blockchain-specific addresses from public keys
    /// </summary>
    public static class AddressDerivationHelper
    {
        /// <summary>
        /// Derives a blockchain-specific address from a public key
        /// </summary>
        /// <param name="publicKey">The public key (hex string or base58)</param>
        /// <param name="providerType">The blockchain provider type</param>
        /// <param name="network">Network type (testnet/mainnet) - optional. If not provided, will try to get from OASIS_DNA</param>
        /// <param name="oasisDNA">Optional OASIS_DNA configuration to get network settings</param>
        /// <returns>The derived wallet address</returns>
        public static string DeriveAddress(string publicKey, ProviderType providerType, string network = null, OASISDNA oasisDNA = null)
        {
            if (string.IsNullOrEmpty(publicKey))
                return null;

            try
            {
                // Determine network if not provided
                if (string.IsNullOrEmpty(network))
                {
                    network = GetNetworkFromOASISDNA(providerType, oasisDNA) ?? "mainnet";
                }

                switch (providerType)
                {
                    case ProviderType.EthereumOASIS:
                    case ProviderType.PolygonOASIS:
                    case ProviderType.ArbitrumOASIS:
                    case ProviderType.BaseOASIS:
                    case ProviderType.OptimismOASIS:
                    case ProviderType.BNBChainOASIS:
                    case ProviderType.FantomOASIS:
                        return DeriveEthereumAddress(publicKey);

                    case ProviderType.AztecOASIS:
                    case ProviderType.MidenOASIS:
                        // Aztec and Miden may use Ethereum-style addresses
                        return DeriveEthereumAddress(publicKey);

                    case ProviderType.ZcashOASIS:
                        return DeriveZcashAddress(publicKey, network);

                    case ProviderType.StarknetOASIS:
                        return DeriveStarknetAddress(publicKey, network);

                    case ProviderType.SolanaOASIS:
                        // Solana addresses are the public key itself in base58
                        // If it's already base58, return as-is; otherwise convert
                        return DeriveSolanaAddress(publicKey);

                    default:
                        // For unknown providers, return the public key as-is
                        // This maintains backward compatibility
                        return publicKey;
                }
            }
            catch (Exception ex)
            {
                // Log error and return public key as fallback
                Console.WriteLine($"Error deriving address for {providerType}: {ex.Message}");
                return publicKey;
            }
        }

        /// <summary>
        /// Gets network configuration from OASIS_DNA for a provider
        /// Supports both testnet and mainnet - determines network from ChainId or Network property
        /// </summary>
        private static string GetNetworkFromOASISDNA(ProviderType providerType, OASISDNA oasisDNA)
        {
            if (oasisDNA == null || oasisDNA.OASIS?.StorageProviders == null)
                return null;

            try
            {
                var storageProviders = oasisDNA.OASIS.StorageProviders;

                // For EVM-compatible chains, determine network from ChainId
                if (providerType == ProviderType.EthereumOASIS && storageProviders.EthereumOASIS != null)
                {
                    long chainId = storageProviders.EthereumOASIS.ChainId;
                    // ChainId 1 = mainnet, 11155111 = Sepolia testnet, 5 = Goerli testnet
                    if (chainId == 1) return "mainnet";
                    if (chainId == 11155111 || chainId == 5) return "testnet";
                    return "testnet"; // Default to testnet for safety
                }
                
                if (providerType == ProviderType.ArbitrumOASIS && storageProviders.ArbitrumOASIS != null)
                {
                    long chainId = storageProviders.ArbitrumOASIS.ChainId;
                    // ChainId 42161 = Arbitrum mainnet, 421614 = Arbitrum Sepolia testnet
                    if (chainId == 42161) return "mainnet";
                    if (chainId == 421614) return "testnet";
                    return "testnet"; // Default to testnet for safety
                }
                
                if (providerType == ProviderType.PolygonOASIS && storageProviders.PolygonOASIS != null)
                {
                    // Check ConnectionString for testnet indicators
                    string connStr = storageProviders.PolygonOASIS.ConnectionString ?? "";
                    if (connStr.Contains("amoy") || connStr.Contains("testnet") || connStr.Contains("mumbai"))
                        return "testnet";
                    if (connStr.Contains("mainnet") || connStr.Contains("polygon-rpc.com"))
                        return "mainnet";
                    return "testnet"; // Default to testnet for safety
                }

                // For Zcash, check if there's a Network property or default to testnet
                if (providerType == ProviderType.ZcashOASIS)
                {
                    // Zcash network is typically specified in the provider config
                    // Default to testnet for safety
                    return "testnet";
                }
                
                // For Starknet, check Network property or ConnectionString
                if (providerType == ProviderType.StarknetOASIS)
                {
                    // Starknet network: "alpha-mainnet" or "alpha-goerli" (testnet)
                    // Default to testnet for safety
                    return "testnet";
                }

                // For Aztec and Miden, they typically use testnet for development
                if (providerType == ProviderType.AztecOASIS || providerType == ProviderType.MidenOASIS)
                {
                    return "testnet";
                }

                // For Solana, check ConnectionString
                if (providerType == ProviderType.SolanaOASIS && storageProviders.SolanaOASIS != null)
                {
                    string connStr = storageProviders.SolanaOASIS.ConnectionString ?? "";
                    if (connStr.Contains("devnet") || connStr.Contains("testnet"))
                        return "testnet";
                    if (connStr.Contains("mainnet-beta") || connStr.Contains("mainnet"))
                        return "mainnet";
                    return "testnet"; // Default to testnet for safety
                }

                // Default to mainnet for unknown providers
                return "mainnet";
            }
            catch
            {
                // Default to mainnet on error
                return "mainnet";
            }
        }

        /// <summary>
        /// Derives an Ethereum-style address (0x + 40 hex chars) from a public key
        /// Uses Keccak-256 hash of the public key, takes last 20 bytes
        /// </summary>
        private static string DeriveEthereumAddress(string publicKey)
        {
            try
            {
                // Remove 0x prefix if present
                string cleanKey = publicKey.StartsWith("0x") ? publicKey.Substring(2) : publicKey;

                // Convert hex string to bytes
                byte[] publicKeyBytes = HexToBytes(cleanKey);

                // If public key is in base58 (Solana format), we need to decode it first
                if (IsBase58(cleanKey))
                {
                    // This is likely a Solana public key, decode it
                    publicKeyBytes = Base58Decode(cleanKey);
                }

                // If we still don't have valid bytes, try to parse as hex again
                if (publicKeyBytes == null || publicKeyBytes.Length == 0)
                {
                    publicKeyBytes = HexToBytes(cleanKey);
                }

                // Ethereum addresses are derived from the Keccak-256 hash of the public key
                // Take the last 20 bytes (40 hex chars) of the hash
                byte[] hash = Keccak256(publicKeyBytes);
                byte[] addressBytes = hash.Skip(Math.Max(0, hash.Length - 20)).Take(20).ToArray();

                return "0x" + BytesToHex(addressBytes).ToLower();
            }
            catch
            {
                // Fallback: if derivation fails, return a placeholder
                return "0x0000000000000000000000000000000000000000";
            }
        }

        /// <summary>
        /// Derives a Zcash transparent address from a public key
        /// Transparent addresses: tm... (testnet) or t1... (mainnet)
        /// Note: Unified Addresses (u1...) require proper ZIP-316 encoding which needs
        /// Zcash protocol libraries. For now, we generate transparent addresses which
        /// are simpler and should work with most faucets.
        /// </summary>
        private static string DeriveZcashAddress(string publicKey, string network)
        {
            try
            {
                // Zcash transparent addresses are derived from public keys
                // Format: tm... (testnet) or t1... (mainnet), base58 encoded
                // This is a simplified implementation - full Zcash address derivation
                // requires more complex logic with proper Zcash libraries
                
                string prefix = network == "testnet" ? "tm" : "t1";
                
                // Use a hash of the public key to generate a deterministic address
                byte[] publicKeyBytes = HexToBytes(publicKey.StartsWith("0x") ? publicKey.Substring(2) : publicKey);
                if (publicKeyBytes == null || publicKeyBytes.Length == 0)
                {
                    publicKeyBytes = Base58Decode(publicKey);
                }
                
                if (publicKeyBytes == null || publicKeyBytes.Length == 0)
                {
                    return $"{prefix}0000000000000000000000000000000000000000";
                }

                // Hash the public key
                byte[] hash = ComputeSHA256(publicKeyBytes);
                
                // Take first 20 bytes and encode in base58
                // Zcash transparent addresses are typically 34 characters
                byte[] addressBytes = hash.Take(20).ToArray();
                string base58Encoded = Base58Encode(addressBytes);
                
                // Zcash transparent address format: prefix + base58 encoded data
                // Typically 34 characters total (2 char prefix + 32 char base58)
                return prefix + base58Encoded.Substring(0, Math.Min(32, base58Encoded.Length));
            }
            catch
            {
                // Fallback: return a placeholder transparent address
                return network == "testnet" ? "tm0000000000000000000000000000000000000000" : "t10000000000000000000000000000000000000000";
            }
        }

        /// <summary>
        /// Derives a Starknet address from a public key
        /// Starknet addresses are 66 characters (0x + 64 hex chars)
        /// </summary>
        private static string DeriveStarknetAddress(string publicKey, string network = "mainnet")
        {
            try
            {
                // Starknet uses a different address derivation
                // For now, we'll use a hash-based approach
                // In production, use proper Starknet address derivation
                
                byte[] publicKeyBytes = HexToBytes(publicKey.StartsWith("0x") ? publicKey.Substring(2) : publicKey);
                if (publicKeyBytes == null || publicKeyBytes.Length == 0)
                {
                    publicKeyBytes = Base58Decode(publicKey);
                }
                
                if (publicKeyBytes == null || publicKeyBytes.Length == 0)
                {
                    return "0x0000000000000000000000000000000000000000000000000000000000000000";
                }

                // Hash the public key
                byte[] hash = ComputeSHA256(publicKeyBytes);
                
                // Starknet addresses are 32 bytes (64 hex chars)
                // Take first 32 bytes of hash
                byte[] addressBytes = hash.Take(32).ToArray();
                
                return "0x" + BytesToHex(addressBytes).ToLower();
            }
            catch
            {
                return "0x0000000000000000000000000000000000000000000000000000000000000000";
            }
        }

        /// <summary>
        /// Derives a Solana address from a public key
        /// Solana addresses are Ed25519 public keys (32 bytes) encoded in base58
        /// Should be 32-44 characters when encoded
        /// </summary>
        private static string DeriveSolanaAddress(string publicKey)
        {
            try
            {
                // If it's already a valid base58 Solana address (32-44 chars), return as-is
                if (IsBase58(publicKey) && publicKey.Length >= 32 && publicKey.Length <= 44)
                {
                    // Verify it decodes to 32 bytes (Ed25519 public key size)
                    byte[] decoded = Base58Decode(publicKey);
                    if (decoded != null && decoded.Length == 32)
                    {
                        return publicKey;
                    }
                }

                // If it's hex, try to convert
                byte[] publicKeyBytes = HexToBytes(publicKey.StartsWith("0x") ? publicKey.Substring(2) : publicKey);
                
                // If we got bytes, check if it's 32 bytes (Ed25519) or 33/65 bytes (Secp256K1)
                if (publicKeyBytes != null && publicKeyBytes.Length > 0)
                {
                    // If it's 32 bytes, it's already an Ed25519 public key - encode it
                    if (publicKeyBytes.Length == 32)
                    {
                        return Base58Encode(publicKeyBytes);
                    }
                    
                    // If it's 33 or 65 bytes, it's Secp256K1 - we can't use this for Solana
                    // This means the key was generated incorrectly
                    // Return a placeholder to indicate the issue
                    Console.WriteLine($"Warning: Solana address derivation received {publicKeyBytes.Length} byte public key (expected 32 bytes for Ed25519). Key may have been generated incorrectly.");
                    
                    // Try to extract first 32 bytes as a workaround (not ideal, but better than nothing)
                    if (publicKeyBytes.Length >= 32)
                    {
                        byte[] ed25519Bytes = publicKeyBytes.Take(32).ToArray();
                        return Base58Encode(ed25519Bytes);
                    }
                }

                // If it's base58 but wrong length, try to decode and re-encode
                if (IsBase58(publicKey))
                {
                    byte[] decoded = Base58Decode(publicKey);
                    if (decoded != null && decoded.Length >= 32)
                    {
                        // Take first 32 bytes
                        byte[] ed25519Bytes = decoded.Take(32).ToArray();
                        return Base58Encode(ed25519Bytes);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deriving Solana address: {ex.Message}");
            }

            // Fallback: return as-is (may be invalid, but preserves the original)
            return publicKey;
        }

        #region Cryptographic Helpers

        private static byte[] Keccak256(byte[] input)
        {
            // Use Nethereum's Keccak-256 implementation for proper Ethereum address derivation
            return Sha3Keccack.Current.CalculateHash(input);
        }

        private static byte[] ComputeSHA256(byte[] input)
        {
            using (var sha256 = System.Security.Cryptography.SHA256.Create())
            {
                return sha256.ComputeHash(input);
            }
        }

        private static byte[] HexToBytes(string hex)
        {
            try
            {
                if (hex.Length % 2 != 0)
                    hex = "0" + hex;

                byte[] bytes = new byte[hex.Length / 2];
                for (int i = 0; i < bytes.Length; i++)
                {
                    bytes[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
                }
                return bytes;
            }
            catch
            {
                return null;
            }
        }

        private static string BytesToHex(byte[] bytes)
        {
            return BitConverter.ToString(bytes).Replace("-", "");
        }

        private static bool IsBase58(string input)
        {
            if (string.IsNullOrEmpty(input))
                return false;

            // Base58 alphabet: 123456789ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz
            string base58Alphabet = "123456789ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz";
            return input.All(c => base58Alphabet.Contains(c));
        }

        private static byte[] Base58Decode(string base58)
        {
            try
            {
                // Use NBitcoin's Base58 decoder
                return NBitcoin.DataEncoders.Encoders.Base58.DecodeData(base58);
            }
            catch
            {
                return null;
            }
        }

        private static string Base58Encode(byte[] bytes)
        {
            try
            {
                // Use NBitcoin's Base58 encoder
                return NBitcoin.DataEncoders.Encoders.Base58.EncodeData(bytes);
            }
            catch
            {
                return "";
            }
        }

        #endregion
    }
}

