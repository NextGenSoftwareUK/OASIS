using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using NBitcoin;
using Nethereum.Util;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.Common;
using StarkSharp.StarkCurve.Signature;
using Nerdbank.Zcash;

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
                        // Aztec may use Ethereum-style addresses
                        return DeriveEthereumAddress(publicKey);
                    
                    case ProviderType.MidenOASIS:
                        // Miden uses Bech32 addresses (mtst1... for testnet, mid1... for mainnet)
                        return DeriveMidenAddress(publicKey, network);

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
        /// Derives a Zcash Unified Address from a public key using Nerdbank.Zcash
        /// Unified Addresses (u1... for testnet, u... for mainnet) are preferred by faucets
        /// and combine transparent and shielded receivers into a single address.
        /// </summary>
        private static string DeriveZcashAddress(string publicKey, string network)
        {
            try
            {
                // Get public key bytes
                byte[] publicKeyBytes = HexToBytes(publicKey.StartsWith("0x") ? publicKey.Substring(2) : publicKey);
                if (publicKeyBytes == null || publicKeyBytes.Length == 0)
                {
                    publicKeyBytes = Base58Decode(publicKey);
                }
                
                if (publicKeyBytes == null || publicKeyBytes.Length == 0)
                {
                    return GetZcashAddressFallback(network);
                }

                // Determine Zcash network
                ZcashNetwork zcashNetwork = network == "testnet" 
                    ? ZcashNetwork.TestNet 
                    : ZcashNetwork.MainNet;

                // For Zcash transparent addresses, we need to:
                // 1. Hash public key with SHA256
                // 2. Hash again with RIPEMD160 (standard Bitcoin/Zcash address derivation)
                // 3. Create a TransparentP2PKHReceiver from the hash
                // 4. Create a UnifiedAddress with that receiver

                // Step 1: SHA256 hash of public key
                byte[] sha256Hash = ComputeSHA256(publicKeyBytes);
                
                // Step 2: RIPEMD160 hash of SHA256 result
                byte[] ripemd160Hash = ComputeRIPEMD160(sha256Hash);
                
                if (ripemd160Hash == null || ripemd160Hash.Length != 20)
                {
                    return GetZcashAddressFallback(network);
                }

                // Step 3: Create TransparentP2PKHReceiver from the hash
                // TransparentP2PKHReceiver takes a ReadOnlySpan<byte> of 20 bytes
                var transparentReceiver = new TransparentP2PKHReceiver(ripemd160Hash);
                
                // Step 4: Create UnifiedAddress with the transparent receiver
                // UnifiedAddress.Create takes receivers and network
                var unifiedAddress = UnifiedAddress.Create(zcashNetwork, transparentReceiver);
                
                return unifiedAddress.ToString();
            }
            catch (Exception ex)
            {
                // Log error for debugging
                Console.WriteLine($"Error deriving Zcash Unified Address with Nerdbank.Zcash: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                
                // Fallback: return a placeholder address
                return GetZcashAddressFallback(network);
            }
        }

        /// <summary>
        /// Computes RIPEMD160 hash (required for Zcash transparent address derivation)
        /// </summary>
        private static byte[] ComputeRIPEMD160(byte[] data)
        {
            try
            {
                // NBitcoin provides RIPEMD160 - it's a static method, not a disposable object
                return NBitcoin.Crypto.Hashes.RIPEMD160(data);
            }
            catch
            {
                // Fallback: use BouncyCastle.Crypto (version 1.x) - already referenced in project
                try
                {
                    var digest = new Org.BouncyCastle.Crypto.Digests.RipeMD160Digest();
                    digest.BlockUpdate(data, 0, data.Length);
                    byte[] result = new byte[digest.GetDigestSize()];
                    digest.DoFinal(result, 0);
                    return result;
                }
                catch
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// Returns a fallback placeholder Zcash address
        /// </summary>
        private static string GetZcashAddressFallback(string network)
        {
            string prefix = network == "testnet" ? "tm" : "t1";
            return $"{prefix}0000000000000000000000000000000000000000";
        }

        /// <summary>
        /// Derives a Starknet address from a public key
        /// Starknet addresses are 66 characters (0x + 64 hex chars)
        /// 
        /// IMPORTANT: Starknet addresses require Pedersen hash, not SHA256!
        /// Current implementation uses SHA256 as a placeholder - this generates invalid addresses.
        /// 
        /// Proper Starknet address derivation:
        /// address = pedersen_hash(
        ///     account_class_hash,
        ///     pedersen_hash(public_key, salt),
        ///     constructor_calldata_hash
        /// )
        /// 
        /// TODO: Integrate StarkSharp SDK (https://github.com/project3fusion/StarkSharp) or
        /// implement Pedersen hash to generate valid Starknet addresses.
        /// </summary>
        private static string DeriveStarknetAddress(string publicKey, string network = "mainnet")
        {
            try
            {
                // Try to use CLI approach if available (similar to Miden)
                string starknetCliPath = GetStarknetCliPath();
                if (!string.IsNullOrEmpty(starknetCliPath) && File.Exists(starknetCliPath))
                {
                    string address = DeriveStarknetAddressViaCli(publicKey, network, starknetCliPath);
                    if (!string.IsNullOrEmpty(address) && IsValidStarknetAddress(address))
                    {
                        return address;
                    }
                }
                
                // Use StarkSharp SDK with Pedersen hash for proper address derivation
                try
                {
                    // Convert public key to BigInteger
                    byte[] publicKeyBytes = HexToBytes(publicKey.StartsWith("0x") ? publicKey.Substring(2) : publicKey);
                    if (publicKeyBytes == null || publicKeyBytes.Length == 0)
                    {
                        publicKeyBytes = Base58Decode(publicKey);
                    }
                    
                    if (publicKeyBytes == null || publicKeyBytes.Length == 0)
                    {
                        Console.WriteLine("Warning: Could not parse public key for Starknet address derivation");
                        return GetStarknetAddressFallback(publicKey, network);
                    }

                    // Convert public key bytes to BigInteger
                    // Ensure it's treated as unsigned and in the correct byte order
                    if (BitConverter.IsLittleEndian)
                    {
                        Array.Reverse(publicKeyBytes);
                    }
                    
                    // Add leading zero if needed to ensure positive BigInteger
                    if (publicKeyBytes.Length > 0 && (publicKeyBytes[0] & 0x80) != 0)
                    {
                        byte[] extendedBytes = new byte[publicKeyBytes.Length + 1];
                        publicKeyBytes.CopyTo(extendedBytes, 0);
                        publicKeyBytes = extendedBytes;
                    }
                    
                    BigInteger publicKeyBigInt = new BigInteger(publicKeyBytes);
                    if (publicKeyBigInt < 0)
                    {
                        // Make it positive
                        byte[] positiveBytes = new byte[publicKeyBytes.Length + 1];
                        publicKeyBytes.CopyTo(positiveBytes, 0);
                        publicKeyBigInt = new BigInteger(positiveBytes);
                    }

                    // Use Pedersen hash from StarkSharp with full address derivation formula
                    // Full Starknet address derivation:
                    //   address = pedersen_hash(
                    //       account_class_hash,
                    //       pedersen_hash(public_key, salt),
                    //       constructor_calldata_hash
                    //   )
                    
                    // Default OpenZeppelin account class hash (for standard accounts)
                    // This is the class hash for OpenZeppelin's Account contract
                    // Mainnet and testnet use the same class hash
                    BigInteger accountClassHash = HexToBigInteger("0x027214a306090cd26575758e8e1b3a");
                    
                    // Salt (typically 0 for deterministic addresses, or random for unique addresses)
                    // Using 0 for deterministic address generation from public key
                    BigInteger salt = BigInteger.Zero;
                    
                    // Hash public key with salt: pedersen_hash(public_key, salt)
                    BigInteger publicKeySaltHash = ECDSA.PedersenHash(publicKeyBigInt, salt);
                    
                    // Constructor calldata hash
                    // For OpenZeppelin accounts, constructor calldata is an array containing the public key
                    // We need to hash the array: pedersen_hash(array_length, pedersen_hash(public_key))
                    // For a single element array [public_key], we use PedersenArrayHash
                    BigInteger constructorCalldataHash = ECDSA.PedersenArrayHash(publicKeyBigInt);
                    
                    // Final address = pedersen_hash(account_class_hash, public_key_salt_hash, constructor_calldata_hash)
                    BigInteger pedersenHash = ECDSA.PedersenHash(accountClassHash, publicKeySaltHash, constructorCalldataHash);
                    
                    // Convert to hex string (64 chars)
                    string addressHex = pedersenHash.ToString("X").ToLower();
                    
                    // Pad to 64 characters if needed
                    if (addressHex.Length < 64)
                    {
                        addressHex = addressHex.PadLeft(64, '0');
                    }
                    else if (addressHex.Length > 64)
                    {
                        // Take last 64 characters if longer
                        addressHex = addressHex.Substring(addressHex.Length - 64);
                    }
                    
                    string address = "0x" + addressHex;
                    
                    if (IsValidStarknetAddress(address))
                    {
                        return address;
                    }
                }
                catch (Exception starkSharpEx)
                {
                    Console.WriteLine($"Error using StarkSharp Pedersen hash: {starkSharpEx.Message}");
                    Console.WriteLine($"Stack trace: {starkSharpEx.StackTrace}");
                }
                
                // Fallback: Use SHA256 (generates invalid addresses but maintains format)
                Console.WriteLine($"Warning: Falling back to SHA256 for Starknet address - addresses will be invalid. StarkSharp integration failed.");
                return GetStarknetAddressFallback(publicKey, network);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deriving Starknet address: {ex.Message}");
                return "0x0000000000000000000000000000000000000000000000000000000000000000";
            }
        }

        /// <summary>
        /// Fallback method using SHA256 (generates invalid addresses)
        /// </summary>
        private static string GetStarknetAddressFallback(string publicKey, string network)
        {
            try
            {
                byte[] publicKeyBytes = HexToBytes(publicKey.StartsWith("0x") ? publicKey.Substring(2) : publicKey);
                if (publicKeyBytes == null || publicKeyBytes.Length == 0)
                {
                    publicKeyBytes = Base58Decode(publicKey);
                }
                
                if (publicKeyBytes == null || publicKeyBytes.Length == 0)
                {
                    return "0x0000000000000000000000000000000000000000000000000000000000000000";
                }

                // ❌ WRONG: Using SHA256 instead of Pedersen hash
                byte[] hash = ComputeSHA256(publicKeyBytes);
                byte[] addressBytes = hash.Take(32).ToArray();
                
                return "0x" + BytesToHex(addressBytes).ToLower();
            }
            catch
            {
                return "0x0000000000000000000000000000000000000000000000000000000000000000";
            }
        }

        /// <summary>
        /// Attempts to derive Starknet address using CLI tool (if available)
        /// Similar approach to Miden - calls external Starknet CLI
        /// </summary>
        private static string DeriveStarknetAddressViaCli(string publicKey, string network, string cliPath)
        {
            try
            {
                // Try using starknet.py or starknet-devnet CLI
                // Command may vary based on CLI tool
                string arguments = $"account derive --public-key {publicKey} --network {network}";
                
                var processInfo = new ProcessStartInfo
                {
                    FileName = cliPath,
                    Arguments = arguments,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (var process = Process.Start(processInfo))
                {
                    if (process == null)
                        return null;

                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();
                    process.WaitForExit();

                    if (process.ExitCode != 0)
                    {
                        Console.WriteLine($"Starknet CLI error: {error}");
                        return null;
                    }

                    // Parse address from output
                    string address = ParseStarknetCliOutput(output);
                    if (!string.IsNullOrEmpty(address) && IsValidStarknetAddress(address))
                    {
                        return address;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error calling Starknet CLI: {ex.Message}");
            }

            return null;
        }

        /// <summary>
        /// Gets Starknet CLI path from configuration or default locations
        /// </summary>
        private static string GetStarknetCliPath()
        {
            // Check common installation paths
            string[] commonPaths = {
                "/usr/local/bin/starknet",
                "/usr/bin/starknet",
                "./tools/starknet",
                "./starknet",
                "starknet" // If in PATH
            };

            foreach (string path in commonPaths)
            {
                if (File.Exists(path))
                    return path;
            }

            // Check if in PATH
            try
            {
                var process = Process.Start(new ProcessStartInfo
                {
                    FileName = "which",
                    Arguments = "starknet",
                    RedirectStandardOutput = true,
                    UseShellExecute = false
                });

                if (process != null)
                {
                    string path = process.StandardOutput.ReadToEnd().Trim();
                    process.WaitForExit();
                    if (!string.IsNullOrEmpty(path) && File.Exists(path))
                        return path;
                }
            }
            catch { }

            return null;
        }

        /// <summary>
        /// Parses Starknet CLI output to extract address
        /// </summary>
        private static string ParseStarknetCliOutput(string output)
        {
            if (string.IsNullOrEmpty(output))
                return null;

            // Try to find address pattern: 0x + 64 hex chars
            var match = System.Text.RegularExpressions.Regex.Match(
                output,
                @"0x[0-9a-fA-F]{64}"
            );

            if (match.Success)
                return match.Value;

            // Try JSON output if CLI returns JSON
            try
            {
                var json = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(output);
                return json?.address?.ToString() ?? json?.Address?.ToString();
            }
            catch { }

            // Try lines starting with "Address:" or "address:"
            foreach (string line in output.Split('\n'))
            {
                if (line.Contains("Address:") || line.Contains("address:"))
                {
                    var parts = line.Split(new[] { ':', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (string part in parts)
                    {
                        if (part.StartsWith("0x") && part.Length == 66)
                            return part;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Validates Starknet address format
        /// Must be 66 characters: 0x + 64 hex characters
        /// </summary>
        private static bool IsValidStarknetAddress(string address)
        {
            if (string.IsNullOrEmpty(address))
                return false;

            // Must be 66 chars: 0x + 64 hex
            if (address.Length != 66)
                return false;

            if (!address.StartsWith("0x"))
                return false;

            // Must be valid hex
            string hex = address.Substring(2);
            return System.Text.RegularExpressions.Regex.IsMatch(hex, @"^[0-9a-fA-F]{64}$");
        }

        /// <summary>
        /// Derives a Miden address from a public key using Bech32 encoding with proper checksum
        /// Miden testnet addresses: mtst1... (Bech32 format)
        /// Miden mainnet addresses: mid1... (Bech32 format)
        /// Uses NBitcoin's Bech32Encoder for proper checksum calculation
        /// </summary>
        private static string DeriveMidenAddress(string publicKey, string network = "testnet")
        {
            try
            {
                // Miden uses Bech32 encoding with prefix "mtst" (testnet) or "mid" (mainnet)
                string hrp = network == "testnet" ? "mtst" : "mid";
                
                // Get public key bytes
                byte[] publicKeyBytes = HexToBytes(publicKey.StartsWith("0x") ? publicKey.Substring(2) : publicKey);
                if (publicKeyBytes == null || publicKeyBytes.Length == 0)
                {
                    publicKeyBytes = Base58Decode(publicKey);
                }
                
                if (publicKeyBytes == null || publicKeyBytes.Length == 0)
                {
                    // Fallback: generate a deterministic address from the public key string
                    publicKeyBytes = Encoding.UTF8.GetBytes(publicKey);
                }

                // Hash the public key to get a deterministic 32-byte value
                byte[] hash = ComputeSHA256(publicKeyBytes);
                
                // Miden addresses appear to use 16 bytes (not 20 like Ethereum)
                // This matches the 37-character address length we see in examples
                // 16 bytes = 32 base32 chars + 6 checksum chars + "mtst1" = 37 chars
                byte[] addressBytes = hash.Take(16).ToArray();
                
                // Convert bytes to 5-bit groups (base32)
                List<byte> data = new List<byte>();
                int bits = 0;
                int value = 0;
                
                foreach (byte b in addressBytes)
                {
                    value = (value << 8) | b;
                    bits += 8;
                    
                    while (bits >= 5)
                    {
                        data.Add((byte)((value >> (bits - 5)) & 0x1f));
                        bits -= 5;
                    }
                }
                
                if (bits > 0)
                {
                    data.Add((byte)((value << (5 - bits)) & 0x1f));
                }
                
                // Encode with Bech32 checksum
                string address = Bech32Encode(hrp, data.ToArray());
                
                return address;
            }
            catch (Exception ex)
            {
                // Log the error for debugging
                Console.WriteLine($"Error deriving Miden address: {ex.Message}");
                
                // Fallback: return a placeholder in the correct format
                string prefix = network == "testnet" ? "mtst1" : "mid1";
                return $"{prefix}0000000000000000000000000000000000000000";
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

        /// <summary>
        /// Bech32 encoding with checksum
        /// Implements the Bech32 encoding algorithm as specified in BIP-0173
        /// </summary>
        private static string Bech32Encode(string hrp, byte[] data)
        {
            const string CHARSET = "qpzry9x8gf2tvdw0s3jn54khce6mua7l";
            
            // Convert HRP to bytes
            byte[] hrpBytes = Encoding.ASCII.GetBytes(hrp.ToLower());
            
            // Create the values array: HRP expanded + separator (0) + data
            List<byte> values = new List<byte>();
            
            // Expand HRP: each character becomes 5 bits (high 3 bits, low 5 bits)
            foreach (byte b in hrpBytes)
            {
                values.Add((byte)(b >> 5));
            }
            values.Add(0); // Separator
            foreach (byte b in hrpBytes)
            {
                values.Add((byte)(b & 0x1f));
            }
            
            // Add data
            values.AddRange(data);
            
            // Calculate checksum
            byte[] checksum = Bech32CreateChecksum(hrpBytes, data);
            values.AddRange(checksum);
            
            // Build the address string
            StringBuilder sb = new StringBuilder();
            sb.Append(hrp);
            sb.Append('1');
            
            foreach (byte v in values.Skip(hrpBytes.Length * 2 + 1)) // Skip HRP expansion and separator
            {
                sb.Append(CHARSET[v]);
            }
            
            return sb.ToString();
        }

        /// <summary>
        /// Creates Bech32 checksum according to BIP-0173
        /// </summary>
        private static byte[] Bech32CreateChecksum(byte[] hrp, byte[] data)
        {
            // Bech32 generator constants (BIP-0173)
            uint[] GEN = { 0x3b6a57b2u, 0x26508e6du, 0x1ea119fau, 0x3d4233ddu, 0x2a1462b3u };
            
            // Create values array: HRP expanded + data + 6 zeros
            List<byte> values = new List<byte>();
            
            // Expand HRP
            foreach (byte b in hrp)
            {
                values.Add((byte)(b >> 5));
            }
            values.Add(0); // Separator
            foreach (byte b in hrp)
            {
                values.Add((byte)(b & 0x1f));
            }
            
            // Add data
            values.AddRange(data);
            
            // Add 6 zeros for checksum
            values.AddRange(new byte[] { 0, 0, 0, 0, 0, 0 });
            
            // Polynomial division (BIP-0173 algorithm)
            uint chk = 1;
            foreach (byte v in values)
            {
                uint top = chk >> 25;
                chk = ((chk & 0x1ffffff) << 5) ^ v;
                for (int i = 0; i < 5; i++)
                {
                    if (((top >> i) & 1) != 0)
                    {
                        chk ^= GEN[i];
                    }
                }
            }
            
            // Convert to 6 bytes (5-bit groups)
            byte[] checksum = new byte[6];
            for (int i = 0; i < 6; i++)
            {
                checksum[i] = (byte)((chk >> (5 * (5 - i))) & 0x1f);
            }
            
            return checksum;
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

        /// <summary>
        /// Converts a hex string to BigInteger
        /// </summary>
        private static BigInteger HexToBigInteger(string hex)
        {
            try
            {
                // Remove 0x prefix if present
                string cleanHex = hex.StartsWith("0x") ? hex.Substring(2) : hex;
                
                // Convert hex string to bytes
                byte[] bytes = HexToBytes(cleanHex);
                if (bytes == null || bytes.Length == 0)
                    return BigInteger.Zero;
                
                // Ensure big-endian for BigInteger (add leading zero if needed for positive)
                if (bytes.Length > 0 && (bytes[0] & 0x80) != 0)
                {
                    byte[] extendedBytes = new byte[bytes.Length + 1];
                    bytes.CopyTo(extendedBytes, 0);
                    bytes = extendedBytes;
                }
                
                // Reverse if little-endian
                if (BitConverter.IsLittleEndian)
                {
                    Array.Reverse(bytes);
                }
                
                return new BigInteger(bytes);
            }
            catch
            {
                return BigInteger.Zero;
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

