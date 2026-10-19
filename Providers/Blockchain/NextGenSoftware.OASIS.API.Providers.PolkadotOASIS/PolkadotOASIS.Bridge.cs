using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Response;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Response;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets.Response;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using System.Text.Json.Serialization;

namespace NextGenSoftware.OASIS.API.Providers.PolkadotOASIS
{
    public partial class PolkadotOASIS
    {
        public OASISResult<IKeyPairAndWallet> GenerateKeyPair()
        {
            return Task.Run(() => GenerateKeyPairAsync()).GetAwaiter().GetResult();
        }

        public Task<OASISResult<IKeyPairAndWallet>> GenerateKeyPairAsync()
        {
            return GenerateKeyPairAsync(null);
        }

        public OASISResult<IKeyPairAndWallet> GenerateKeyPair(IGetWeb3WalletBalanceRequest request)
        {
            return Task.Run(() => GenerateKeyPairAsync(request)).GetAwaiter().GetResult();
        }

        public async Task<OASISResult<IKeyPairAndWallet>> GenerateKeyPairAsync(IGetWeb3WalletBalanceRequest request)
        {
            var result = new OASISResult<IKeyPairAndWallet>();
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Polkadot provider is not activated");
                    return result;
                }

                if (!string.IsNullOrWhiteSpace(request?.WalletAddress))
                    throw new ArgumentException("New key generation does not recover or replace an existing wallet address.");
                var generated = await InvokePolkadotSdkAsync(new { operation = "generateKeyPair", endpoint = _rpcEndpoint,
                    genesisHash = ReportedGenesisHash }).ConfigureAwait(false);
                result.Result = ParseSdkGeneratedWallet(generated);
                result.Message = "New SDK sr25519 seed/public key and authoritative-network SS58 address generated; no wallet stored, funded or submitted.";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error generating key pair: {ex.Message}", ex);
            }
            return result;
        }

        private IKeyPairAndWallet ParseSdkGeneratedWallet(JsonElement generated)
        {
                var seed = generated.GetProperty("privateKey").GetString();
                var publicKey = generated.GetProperty("publicKey").GetString();
                var address = generated.GetProperty("address").GetString();
                var prefix = generated.GetProperty("ss58Format").GetInt32();
                if (generated.GetProperty("chainId").GetString() != ReportedGenesisHash
                    || generated.GetProperty("cryptoType").GetString() != "sr25519"
                    || !IsGenesisHash(seed) || !IsGenesisHash(publicKey)
                    || prefix < 0 || prefix > 16383 || prefix is 46 or 47
                    || address == null || address.Length < 35 || address.Length > 50
                    || !System.Text.RegularExpressions.Regex.IsMatch(address, "^[1-9A-HJ-NP-Za-km-z]+$"))
                    throw new System.IO.InvalidDataException("SDK key generation response is invalid; key values withheld.");
                return new KeyHelper.KeyPairAndWallet { PrivateKey = seed, PublicKey = publicKey,
                    WalletAddressLegacy = address, WalletAddressSegwitP2SH = null };
        }



        public async Task<OASISResult<decimal>> GetAccountBalanceAsync(string accountAddress, CancellationToken token = default)
        {
            var result = new OASISResult<decimal>();
            try
            {
                if (!_isActivated || _httpClient == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Polkadot provider is not activated");
                    return result;
                }

                if (string.IsNullOrWhiteSpace(accountAddress))
                {
                    OASISErrorHandling.HandleError(ref result, "Account address is required");
                    return result;
                }

                var balance = await InvokePolkadotSdkAsync(new { operation = "balance", endpoint = _rpcEndpoint,
                    genesisHash = ReportedGenesisHash, address = accountAddress }, token).ConfigureAwait(false);
                result.Result = ParseNativeDecimalBalance(balance);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting Polkadot account balance: {ex.Message}", ex);
            }
            return result;
        }

        public async Task<OASISResult<(string PublicKey, string PrivateKey, string SeedPhrase)>> CreateAccountAsync(CancellationToken token = default)
        {
            var result = new OASISResult<(string PublicKey, string PrivateKey, string SeedPhrase)>();
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Polkadot provider is not activated");
                    return result;
                }

                var generated = await InvokePolkadotSdkAsync(new { operation = "createAccount", endpoint = _rpcEndpoint,
                    genesisHash = ReportedGenesisHash }, token).ConfigureAwait(false);
                var wallet = ParseSdkGeneratedWallet(generated);
                var phrase = generated.GetProperty("seedPhrase").GetString();
                if (string.IsNullOrWhiteSpace(phrase) || phrase.Split(' ').Length != 24)
                    throw new System.IO.InvalidDataException("SDK mnemonic response is invalid; values withheld.");
                result.Result = (wallet.PublicKey, wallet.PrivateKey, phrase);
                result.IsError = false;
                result.Message = "SDK sr25519 account material created; not stored, funded or submitted.";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error creating Polkadot account: {ex.Message}", ex);
            }
            return result;
        }

        public async Task<OASISResult<(string PublicKey, string PrivateKey)>> RestoreAccountAsync(string seedPhrase, CancellationToken token = default)
        {
            var result = new OASISResult<(string PublicKey, string PrivateKey)>();
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Polkadot provider is not activated");
                    return result;
                }

                if (string.IsNullOrWhiteSpace(seedPhrase))
                {
                    OASISErrorHandling.HandleError(ref result, "Seed phrase cannot be null or empty");
                    return result;
                }

                var restored = await InvokePolkadotSdkAsync(new { operation = "restoreAccount", endpoint = _rpcEndpoint,
                    genesisHash = ReportedGenesisHash, recoveryMaterial = seedPhrase }, token).ConfigureAwait(false);
                var wallet = ParseSdkGeneratedWallet(restored);
                result.Result = (wallet.PublicKey, wallet.PrivateKey);
                result.IsError = false;
                result.Message = "SDK sr25519 identity restored from explicit material; no stored wallet replaced.";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error restoring Polkadot account: {ex.Message}", ex);
            }
            return result;
        }

        public Task<OASISResult<BridgeTransactionResponse>> WithdrawAsync(decimal amount, string senderAccountAddress, string senderPrivateKey)
            => ExecuteBridgeTransferAsync(amount, senderAccountAddress, _bridgePoolAddress, senderPrivateKey);

        public Task<OASISResult<BridgeTransactionResponse>> DepositAsync(decimal amount, string receiverAccountAddress)
            => ExecuteBridgeTransferAsync(amount, _bridgePoolAddress, receiverAccountAddress, _privateKey);

        private async Task<OASISResult<BridgeTransactionResponse>> ExecuteBridgeTransferAsync(
            decimal amount, string sender, string recipient, string signingKey)
        {
            var result = new OASISResult<BridgeTransactionResponse>();
            try
            {
                if (!_isActivated) throw new InvalidOperationException("Polkadot provider is not activated.");
                if (string.IsNullOrWhiteSpace(_bridgePoolAddress))
                    throw new InvalidOperationException("An explicit bridge pool address is required; no guessed destination is permitted.");
                if (amount <= 0 || string.IsNullOrWhiteSpace(sender) || string.IsNullOrWhiteSpace(recipient)
                    || string.IsNullOrWhiteSpace(signingKey))
                    throw new ArgumentException("A positive amount, explicit sender/recipient and signing key are required.");
                var transfer = await SendOwnedNativeTransferAsync(new NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests.SendWeb3TokenRequest {
                    FromWalletAddress = sender, ToWalletAddress = recipient, Amount = amount, OwnerPrivateKey = signingKey
                }).ConfigureAwait(false);
                if (transfer.IsError) throw transfer.Exception ?? new InvalidOperationException(transfer.Message);
                var original = transfer.Result?.TransactionResult;
                if (!IsGenesisHash(original))
                    throw new System.IO.InvalidDataException("Finalized native transfer returned no valid original transaction identity.");
                result.Result = new BridgeTransactionResponse {
                    TransactionId = original, IsSuccessful = true, Status = BridgeTransactionStatus.Completed
                };
                result.Message = "Native pool transfer finalized; cross-chain authorization/release and billing settlement are separate.";
            }
            catch (Exception failure)
            {
                OASISErrorHandling.HandleError(ref result, failure.Message, failure);
                var uncertain = failure as INativeMutationReconciliationRequired;
                result.Result = new BridgeTransactionResponse {
                    TransactionId = uncertain?.OriginalTransactionId ?? string.Empty,
                    IsSuccessful = false, ErrorMessage = failure.Message,
                    Status = uncertain != null ? BridgeTransactionStatus.Pending : BridgeTransactionStatus.Canceled
                };
            }
            return result;
        }

        public async Task<OASISResult<BridgeTransactionStatus>> GetTransactionStatusAsync(string transactionHash, CancellationToken token = default)
        {
            var result = new OASISResult<BridgeTransactionStatus>();
            try
            {
                if (!_isActivated || _httpClient == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Polkadot provider is not activated");
                    return result;
                }

                if (string.IsNullOrWhiteSpace(transactionHash))
                {
                    OASISErrorHandling.HandleError(ref result, "Transaction hash is required");
                    return result;
                }

                // Query Polkadot RPC for transaction status
                var rpcRequest = new
                {
                    jsonrpc = "2.0",
                    id = 1,
                    method = "chain_getBlock",
                    @params = new object[] { transactionHash }
                };

                var response = await _httpClient.PostAsJsonAsync("", rpcRequest, token);
                var content = await response.Content.ReadAsStringAsync(token);
                var jsonDoc = JsonDocument.Parse(content);

                if (jsonDoc.RootElement.TryGetProperty("result", out var resultElement))
                {
                    result.Result = BridgeTransactionStatus.Completed;
                    result.IsError = false;
                }
                else
                {
                    result.Result = BridgeTransactionStatus.NotFound;
                    result.IsError = true;
                    result.Message = "Transaction not found";
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting Polkadot transaction status: {ex.Message}", ex);
                result.Result = BridgeTransactionStatus.NotFound;
            }
            return result;
        }



        /// <summary>
        /// Parse Polkadot blockchain response to Avatar collection
        /// </summary>
        private IEnumerable<IAvatar> ParsePolkadotToAvatars(string polkadotJson)
        {
            try
            {
                var jsonDoc = JsonDocument.Parse(polkadotJson);
                var root = jsonDoc.RootElement;
                var avatars = new List<IAvatar>();

                if (root.ValueKind == JsonValueKind.Array)
                {
                    foreach (var element in root.EnumerateArray())
                    {
                        var avatar = ParsePolkadotToAvatar(element.GetRawText());
                        if (avatar != null)
                            avatars.Add(avatar);
                    }
                }
                else if (root.TryGetProperty("avatars", out var avatarsArray) && avatarsArray.ValueKind == JsonValueKind.Array)
                {
                    foreach (var element in avatarsArray.EnumerateArray())
                    {
                        var avatar = ParsePolkadotToAvatar(element.GetRawText());
                        if (avatar != null)
                            avatars.Add(avatar);
                    }
                }

                return avatars;
            }
            catch
            {
                return new List<IAvatar>();
            }
        }

        /// <summary>
        /// Parse Polkadot blockchain response to Holon collection
        /// </summary>
        private IEnumerable<IHolon> ParsePolkadotToHolons(string polkadotJson)
        {
            try
            {
                var jsonDoc = JsonDocument.Parse(polkadotJson);
                var root = jsonDoc.RootElement;
                var holons = new List<IHolon>();

                if (root.ValueKind == JsonValueKind.Array)
                {
                    foreach (var element in root.EnumerateArray())
                    {
                        var holon = ParsePolkadotToHolon(element.GetRawText());
                        if (holon != null)
                            holons.Add(holon);
                    }
                }
                else if (root.TryGetProperty("holons", out var holonsArray) && holonsArray.ValueKind == JsonValueKind.Array)
                {
                    foreach (var element in holonsArray.EnumerateArray())
                    {
                        var holon = ParsePolkadotToHolon(element.GetRawText());
                        if (holon != null)
                            holons.Add(holon);
                    }
                }

                return holons;
            }
            catch
            {
                return new List<IHolon>();
            }
        }

        /// <summary>
        /// Parse Polkadot chain storage (hex-encoded) to Holon object
        /// </summary>
        private IHolon ParsePolkadotStorageToHolon(string hexStorageData)
        {
            try
            {
                // Decode hex-encoded storage data
                if (string.IsNullOrEmpty(hexStorageData) || !hexStorageData.StartsWith("0x"))
                {
                    return null;
                }

                // Remove "0x" prefix and decode
                var hexBytes = hexStorageData.Substring(2);
                var bytes = new byte[hexBytes.Length / 2];
                for (int i = 0; i < bytes.Length; i++)
                {
                    bytes[i] = Convert.ToByte(hexBytes.Substring(i * 2, 2), 16);
                }

                // Try to decode as UTF-8 JSON string
                var jsonString = Encoding.UTF8.GetString(bytes).Trim('\0');
                if (string.IsNullOrWhiteSpace(jsonString))
                {
                    return null;
                }

                // Parse as JSON and create holon
                return ParsePolkadotToHolon(jsonString);
            }
            catch
            {
                // If parsing fails, return null
                return null;
            }
        }

        /// <summary>
        /// Parse Polkadot blockchain response to Holon object
        /// </summary>
        private IHolon ParsePolkadotToHolon(string polkadotJson)
        {
            try
            {
                var jsonDoc = JsonDocument.Parse(polkadotJson);
                var root = jsonDoc.RootElement;

                var holon = new Holon
                {
                    Id = root.TryGetProperty("id", out var idElement) && idElement.GetString() != null ? Guid.Parse(idElement.GetString()) : CreateDeterministicGuid($"{ProviderType.Value}:holon:{root.GetRawText()}"),
                    Name = root.TryGetProperty("name", out var nameElement) ? nameElement.GetString() : "Polkadot Holon",
                    Description = root.TryGetProperty("description", out var descElement) ? descElement.GetString() : "Holon from Polkadot blockchain",
                    ProviderUniqueStorageKey = new Dictionary<ProviderType, string>
                    {
                        [Core.Enums.ProviderType.PolkadotOASIS] = root.TryGetProperty("polkadotId", out var polkadotIdElement) ? polkadotIdElement.GetString() ?? CreateDeterministicGuid($"{ProviderType.Value}:holon:{root.GetRawText()}").ToString() : CreateDeterministicGuid($"{ProviderType.Value}:holon:{root.GetRawText()}").ToString()
                    },
                    IsActive = root.TryGetProperty("isActive", out var activeElement) ? activeElement.GetBoolean() : true,
                    CreatedDate = root.TryGetProperty("createdDate", out var createdElement) && DateTime.TryParse(createdElement.GetString(), out var createdDate) ? createdDate : DateTime.UtcNow,
                    ModifiedDate = root.TryGetProperty("modifiedDate", out var modifiedElement) && DateTime.TryParse(modifiedElement.GetString(), out var modifiedDate) ? modifiedDate : DateTime.UtcNow
                };

                return holon;
            }
            catch
            {
                return new Holon
                {
                    Id = CreateDeterministicGuid($"{ProviderType.Value}:holon:error"),
                    Name = "Polkadot Holon",
                    ProviderUniqueStorageKey = new Dictionary<ProviderType, string>
                    {
                        [Core.Enums.ProviderType.PolkadotOASIS] = CreateDeterministicGuid($"{ProviderType.Value}:holon:error").ToString()
                    }
                };
            }
        }

        /// <summary>
        /// Creates a deterministic GUID from input string using SHA-256 hash
        /// </summary>
        private static Guid CreateDeterministicGuid(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return Guid.Empty;

            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
            return new Guid(bytes.Take(16).ToArray());
        }

    }
}
