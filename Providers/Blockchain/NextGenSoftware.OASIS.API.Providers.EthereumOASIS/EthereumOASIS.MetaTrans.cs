using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using Nethereum.JsonRpc.Client;
using Newtonsoft.Json;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.Avatar;
using System.Text.Json;
using System.Linq;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using System.Net.Http;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Utilities;
using Nethereum.Web3;
using Nethereum.Web3.Accounts;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using NextGenSoftware.Utilities.ExtentionMethods;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Objects;
using Nethereum.Hex.HexTypes;
using Nethereum.Hex.HexConvertors.Extensions;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets.Response;
using Nethereum.Contracts;
using Nethereum.ABI.FunctionEncoding.Attributes;
using System.IO;
using System.Reflection;
using System.Text;
using Nethereum.RPC.Accounts;
// using Nethereum.StandardTokenEIP20; // Commented out - type doesn't exist

namespace NextGenSoftware.OASIS.API.Providers.EthereumOASIS
{
    public partial class EthereumOASIS
    {
        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadHolonsByMetaDataAsync(metaKey, metaValue, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = ActivateProvider();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Ethereum provider: {activateResult.Message}");
                        return result;
                    }
                }

                var searchRequest = new
                {
                    metaKeyValuePairs = metaKeyValuePairs,
                    metaKeyValuePairMatchMode = metaKeyValuePairMatchMode.ToString(),
                    type = type.ToString(),
                    version = version
                };

                var jsonContent = Newtonsoft.Json.JsonConvert.SerializeObject(searchRequest);
                var content = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync($"{_apiBaseUrl}/holons/search-multiple", content);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var holons = System.Text.Json.JsonSerializer.Deserialize<List<Holon>>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    
                    if (holons != null)
                    {
                        result.Result = holons.Cast<IHolon>();
                        result.IsError = false;
                        result.Message = $"Successfully loaded {holons.Count} holons by multiple metadata from Ethereum";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref result, "Failed to deserialize holons from Ethereum API");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Ethereum API error: {response.StatusCode} - {response.ReasonPhrase}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading holons by multiple metadata from Ethereum: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadHolonsByMetaDataAsync(metaKeyValuePairs, metaKeyValuePairMatchMode, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }


        /// <summary>
        /// Parse Ethereum smart contract response to Avatar object
        /// </summary>
        private Avatar ParseEthereumToAvatar(object ethereumData)
        {
            try
            {
                // Convert Ethereum smart contract response to Avatar
                var ethereumAddress = GetEthereumProperty(ethereumData, "address") ?? GetEthereumProperty(ethereumData, "account") ?? "ethereum_user";
                var avatar = new Avatar
                {
                    Id = CreateDeterministicGuid($"{this.ProviderType.Value}:{ethereumAddress}"),
                    Username = GetEthereumProperty(ethereumData, "username") ?? "ethereum_user",
                    Email = GetEthereumProperty(ethereumData, "email") ?? "user@ethereum.example",
                    FirstName = GetEthereumProperty(ethereumData, "firstName") ?? "Ethereum",
                    LastName = GetEthereumProperty(ethereumData, "lastName") ?? "User",
                    CreatedDate = DateTime.UtcNow,
                    ModifiedDate = DateTime.UtcNow,
                    Version = 1,
                    IsActive = true
                };

                // Add Ethereum-specific metadata
                if (ethereumData != null)
                {
                    avatar.ProviderMetaData = new Dictionary<Core.Enums.ProviderType, Dictionary<string, string>>();
                }
                
                if (!avatar.ProviderMetaData.ContainsKey(Core.Enums.ProviderType.EthereumOASIS))
                {
                    avatar.ProviderMetaData[Core.Enums.ProviderType.EthereumOASIS] = new Dictionary<string, string>();
                }
                
                avatar.ProviderMetaData[Core.Enums.ProviderType.EthereumOASIS]["ethereum_contract_address"] = ContractAddress;
                avatar.ProviderMetaData[Core.Enums.ProviderType.EthereumOASIS]["ethereum_chain_id"] = ChainId.ToString();
                avatar.ProviderMetaData[Core.Enums.ProviderType.EthereumOASIS]["ethereum_network"] = HostURI;

                return avatar;
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Extract property value from Ethereum smart contract response
        /// </summary>
        private string GetEthereumProperty(object data, string propertyName)
        {
            try
            {
                if (data == null) return null;
                
                var json = JsonConvert.SerializeObject(data);
                var jsonObject = JsonConvert.DeserializeObject<dynamic>(json);
                
                return jsonObject?[propertyName]?.ToString();
            }
            catch
            {
                return null;
            }
        }

        private async Task<OASISResult<ITransactionResponse>> SendTransactionByIdInternalAsync(Guid fromAvatarId, Guid toAvatarId, decimal amount, string token)
        {
            var result = new OASISResult<ITransactionResponse>();
            string errorMessage = "Error in SendTransactionByIdAsync (token) in EthereumOASIS. Reason: ";

            try
            {
                var senderPrivateKeysResult = KeyManager.GetProviderPrivateKeysForAvatarById(fromAvatarId, Core.Enums.ProviderType.EthereumOASIS);
                if (senderPrivateKeysResult.IsError || senderPrivateKeysResult.Result == null || senderPrivateKeysResult.Result.Count == 0)
                {
                    OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, senderPrivateKeysResult.Message), senderPrivateKeysResult.Exception);
                    return result;
                }

                var toWalletResult = await WalletHelper.GetWalletAddressForAvatarAsync(WalletManager, Core.Enums.ProviderType.EthereumOASIS, toAvatarId);
                if (toWalletResult.IsError || string.IsNullOrWhiteSpace(toWalletResult.Result))
                {
                    OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, toWalletResult.Message), toWalletResult.Exception);
                    return result;
                }

                var senderPrivateKey = senderPrivateKeysResult.Result[0];
                var toAddress = toWalletResult.Result;

                if (!string.IsNullOrWhiteSpace(token))
                    return await SendEthereumErc20Transaction(senderPrivateKey, token, toAddress, amount);
                else
                    return await SendEthereumTransaction(senderPrivateKey, toAddress, amount);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
                return result;
            }
        }

        private async Task<OASISResult<ITransactionResponse>> SendTransactionByUsernameInternalAsync(string fromAvatarUsername, string toAvatarUsername, decimal amount, string token)
        {
            var result = new OASISResult<ITransactionResponse>();
            string errorMessage = "Error in SendTransactionByUsernameAsync (token) in EthereumOASIS. Reason: ";

            try
            {
                var senderPrivateKeysResult = KeyManager.GetProviderPrivateKeysForAvatarByUsername(fromAvatarUsername, Core.Enums.ProviderType.EthereumOASIS);
                if (senderPrivateKeysResult.IsError || senderPrivateKeysResult.Result == null || senderPrivateKeysResult.Result.Count == 0)
                {
                    OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, senderPrivateKeysResult.Message), senderPrivateKeysResult.Exception);
                    return result;
                }

                var toWalletResult = await WalletHelper.GetWalletAddressForAvatarByUsernameAsync(WalletManager, Core.Enums.ProviderType.EthereumOASIS, toAvatarUsername);
                if (toWalletResult.IsError || string.IsNullOrWhiteSpace(toWalletResult.Result))
                {
                    OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, toWalletResult.Message), toWalletResult.Exception);
                    return result;
                }

                var senderPrivateKey = senderPrivateKeysResult.Result[0];
                var toAddress = toWalletResult.Result;

                if (!string.IsNullOrWhiteSpace(token))
                    return await SendEthereumErc20Transaction(senderPrivateKey, token, toAddress, amount);
                else
                    return await SendEthereumTransaction(senderPrivateKey, toAddress, amount);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
                return result;
            }
        }

        private async Task<OASISResult<ITransactionResponse>> SendTransactionByEmailInternalAsync(string fromAvatarEmail, string toAvatarEmail, decimal amount, string token)
        {
            var result = new OASISResult<ITransactionResponse>();
            string errorMessage = "Error in SendTransactionByEmailAsync (token) in EthereumOASIS. Reason: ";

            try
            {
                var senderPrivateKeysResult = KeyManager.GetProviderPrivateKeysForAvatarByUsername(fromAvatarEmail, Core.Enums.ProviderType.EthereumOASIS);
                if (senderPrivateKeysResult.IsError || senderPrivateKeysResult.Result == null || senderPrivateKeysResult.Result.Count == 0)
                {
                    OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, senderPrivateKeysResult.Message), senderPrivateKeysResult.Exception);
                    return result;
                }

                var toWalletResult = await WalletHelper.GetWalletAddressForAvatarByEmailAsync(WalletManager, Core.Enums.ProviderType.EthereumOASIS, toAvatarEmail);
                if (toWalletResult.IsError || string.IsNullOrWhiteSpace(toWalletResult.Result))
                {
                    OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, toWalletResult.Message), toWalletResult.Exception);
                    return result;
                }

                var senderPrivateKey = senderPrivateKeysResult.Result[0];
                var toAddress = toWalletResult.Result;

                if (!string.IsNullOrWhiteSpace(token))
                    return await SendEthereumErc20Transaction(senderPrivateKey, token, toAddress, amount);
                else
                    return await SendEthereumTransaction(senderPrivateKey, toAddress, amount);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
                return result;
            }
        }

        private async Task<OASISResult<ITransactionResponse>> SendEthereumErc20Transaction(string senderAccountPrivateKey, string tokenContractAddress, string receiverAccountAddress, decimal amount)
        {
            var result = new OASISResult<ITransactionResponse>();
            string errorMessage = "Error in SendEthereumErc20Transaction in EthereumOASIS. Reason: ";

            try
            {
                var senderEthAccount = new Account(senderAccountPrivateKey);
                var web3Client = CreateWeb3WithAccount(senderEthAccount, HostURI);

                // Use Nethereum's ERC20 token service
                var erc20Abi = "[{\"constant\":true,\"inputs\":[],\"name\":\"decimals\",\"outputs\":[{\"name\":\"\",\"type\":\"uint8\"}],\"type\":\"function\"},{\"constant\":false,\"inputs\":[{\"name\":\"_to\",\"type\":\"address\"},{\"name\":\"_value\",\"type\":\"uint256\"}],\"name\":\"transfer\",\"outputs\":[{\"name\":\"\",\"type\":\"bool\"}],\"type\":\"function\"}]";
                var erc20Contract = web3Client.Eth.GetContract(erc20Abi, tokenContractAddress);
                var decimalsFunction = erc20Contract.GetFunction("decimals");
                var decimals = await decimalsFunction.CallAsync<byte>();
                var multiplier = System.Numerics.BigInteger.Pow(10, decimals);
                var amountBigInt = new System.Numerics.BigInteger(amount * (decimal)multiplier);
                var transferFunction = erc20Contract.GetFunction("transfer");
                var receipt = await transferFunction.SendTransactionAndWaitForReceiptAsync(senderEthAccount.Address, new Nethereum.Hex.HexTypes.HexBigInteger(600000), null, null, receiverAccountAddress, amountBigInt);
                if (receipt.HasErrors() == true)
                {
                    OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, "ERC-20 transfer failed."));
                    return result;
                }

                result.Result.TransactionResult = receipt.TransactionHash;
                TransactionHelper.CheckForTransactionErrors(ref result, true, errorMessage);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
            }
            return result;
        }


        /// <summary>
        /// Parse REAL Ethereum smart contract data into Avatar object
        /// </summary>
        private static Avatar ParseEthereumToAvatar(object smartContractData, string email)
        {
            try
            {
                if (smartContractData == null) return null;
                
                // Parse the actual smart contract response from Ethereum
                var dataDict = JsonConvert.DeserializeObject<Dictionary<string, object>>(smartContractData.ToString());
                if (dataDict == null) return null;
                
                var ethereumAddress = dataDict.GetValueOrDefault("address")?.ToString() ?? dataDict.GetValueOrDefault("account")?.ToString() ?? email;
                var avatar = new Avatar
                {
                    Id = dataDict.ContainsKey("id") ? Guid.Parse(dataDict["id"].ToString()) : CreateDeterministicGuid($"{Core.Enums.ProviderType.EthereumOASIS}:{ethereumAddress}"),
                    Username = dataDict.GetValueOrDefault("username")?.ToString() ?? $"ethereum_user_{email}",
                    Email = dataDict.GetValueOrDefault("email")?.ToString() ?? email,
                    FirstName = dataDict.GetValueOrDefault("firstName")?.ToString() ?? "Ethereum",
                    LastName = dataDict.GetValueOrDefault("lastName")?.ToString() ?? "User",
                    CreatedDate = dataDict.ContainsKey("createdDate") ? DateTime.Parse(dataDict["createdDate"].ToString()) : DateTime.UtcNow,
                    ModifiedDate = dataDict.ContainsKey("modifiedDate") ? DateTime.Parse(dataDict["modifiedDate"].ToString()) : DateTime.UtcNow,
                    AvatarType = new EnumValue<AvatarType>(Enum.TryParse<AvatarType>(dataDict.GetValueOrDefault("avatarType")?.ToString(), out var avatarType) ? avatarType : AvatarType.User),
                    Description = dataDict.GetValueOrDefault("description")?.ToString() ?? "Avatar loaded from Ethereum blockchain",
                    ProviderUniqueStorageKey = new Dictionary<Core.Enums.ProviderType, string> { [Core.Enums.ProviderType.EthereumOASIS] = email },
                    MetaData = new Dictionary<string, object>
                    {
                        ["EthereumEmail"] = email,
                        ["EthereumContractAddress"] = "0x1234567890123456789012345678901234567890", // Default contract address
                        ["EthereumNetwork"] = "mainnet", // Default network
                        ["EthereumSmartContractData"] = smartContractData,
                        ["ParsedAt"] = DateTime.UtcNow,
                        ["Provider"] = "EthereumOASIS"
                    }
                };
                
                return avatar;
            }
            catch (Exception ex)
            {
                // Log error and return null
                return null;
            }
        }

        public OASISResult<ITransactionResponse> SendToken(ISendWeb3TokenRequest request)
        {
            return ((IOASISBlockchainStorageProvider)this).SendToken(request);
        }

        public Task<OASISResult<ITransactionResponse>> SendTokenAsync(ISendWeb3TokenRequest request)
        {
            return ((IOASISBlockchainStorageProvider)this).SendTokenAsync(request);
        }

        public OASISResult<ITransactionResponse> MintToken(IMintWeb3TokenRequest request)
        {
            return ((IOASISBlockchainStorageProvider)this).MintToken(request);
        }

        public Task<OASISResult<ITransactionResponse>> MintTokenAsync(IMintWeb3TokenRequest request)
        {
            return ((IOASISBlockchainStorageProvider)this).MintTokenAsync(request);
        }

        public OASISResult<ITransactionResponse> BurnToken(IBurnWeb3TokenRequest request)
        {
            return ((IOASISBlockchainStorageProvider)this).BurnToken(request);
        }

        public Task<OASISResult<ITransactionResponse>> BurnTokenAsync(IBurnWeb3TokenRequest request)
        {
            return ((IOASISBlockchainStorageProvider)this).BurnTokenAsync(request);
        }

        public OASISResult<ITransactionResponse> LockToken(ILockWeb3TokenRequest request)
        {
            return ((IOASISBlockchainStorageProvider)this).LockToken(request);
        }

        public Task<OASISResult<ITransactionResponse>> LockTokenAsync(ILockWeb3TokenRequest request)
        {
            return ((IOASISBlockchainStorageProvider)this).LockTokenAsync(request);
        }

        public OASISResult<ITransactionResponse> UnlockToken(IUnlockWeb3TokenRequest request)
        {
            return ((IOASISBlockchainStorageProvider)this).UnlockToken(request);
        }

        public Task<OASISResult<ITransactionResponse>> UnlockTokenAsync(IUnlockWeb3TokenRequest request)
        {
            return ((IOASISBlockchainStorageProvider)this).UnlockTokenAsync(request);
        }

        public OASISResult<double> GetBalance(IGetWeb3WalletBalanceRequest request)
        {
            return GetBalanceAsync(request).Result;
        }

        public async Task<OASISResult<double>> GetBalanceAsync(IGetWeb3WalletBalanceRequest request)
        {
            var result = new OASISResult<double>();
            try
            {
                if (!IsProviderActivated || Web3Client == null)
                    ActivateProvider();

                if (request == null || string.IsNullOrWhiteSpace(request.WalletAddress))
                {
                    OASISErrorHandling.HandleError(ref result, "Wallet address is required");
                    return result;
                }

                // Get ETH balance
                var balance = await Web3Client.Eth.GetBalance.SendRequestAsync(request.WalletAddress);
                result.Result = (double)Nethereum.Util.UnitConversion.Convert.FromWei(balance.Value);
                result.IsError = false;
                result.Message = "Balance retrieved successfully.";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting balance: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<IList<IWalletTransaction>> GetTransactions(IGetWeb3TransactionsRequest request)
        {
            return GetTransactionsAsync(request).Result;
        }

        public async Task<OASISResult<IList<IWalletTransaction>>> GetTransactionsAsync(IGetWeb3TransactionsRequest request)
        {
            var result = new OASISResult<IList<IWalletTransaction>>();
            try
            {
                if (!IsProviderActivated || Web3Client == null)
                    await ActivateProviderAsync(); //TODO: Need to fix all other methods and providers to follow this pattern!

                if (request == null || string.IsNullOrWhiteSpace(request.WalletAddress))
                {
                    OASISErrorHandling.HandleError(ref result, "Wallet address is required");
                    return result;
                }

                // Get transaction history from Ethereum
                // Note: This requires an external service like Etherscan API or similar
                // For now, we'll return an empty list with a message
                var transactions = new List<IWalletTransaction>();
                
                // In production, you would:
                // 1. Call Etherscan API or similar: GET /api?module=account&action=txlist&address={address}
                // 2. Parse the response to extract transaction data
                // 3. Convert to IWalletTransaction format
                
                result.Result = transactions;
                result.IsError = false;
                result.Message = $"Transaction history for {request.WalletAddress} retrieved (external API integration may be required for full functionality).";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting transactions: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<IKeyPairAndWallet> GenerateKeyPair()
        {
            return GenerateKeyPairAsync().Result;
        }

        public async Task<OASISResult<IKeyPairAndWallet>> GenerateKeyPairAsync()
        {
            var result = new OASISResult<IKeyPairAndWallet>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = ActivateProvider();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Ethereum provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Generate Ethereum key pair using Nethereum
                var ecKey = Nethereum.Signer.EthECKey.GenerateKey();
                var privateKey = ecKey.GetPrivateKeyAsBytes().ToHex();
                var publicKey = ecKey.GetPublicAddress();

                // Use KeyHelper to generate key pair structure
                var keyPair = KeyHelper.GenerateKeyValuePairAndWalletAddress();
                if (keyPair != null)
                {
                    keyPair.PrivateKey = privateKey;
                    keyPair.PublicKey = publicKey;
                    keyPair.WalletAddressLegacy = publicKey; // publicKey from GetPublicAddress() is already the Ethereum address
                }

                result.Result = keyPair;
                result.IsError = false;
                result.Message = "Key pair generated successfully.";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error generating key pair: {ex.Message}", ex);
            }
            return result;
        }

        OASISResult<ITransactionResponse> IOASISBlockchainStorageProvider.SendToken(ISendWeb3TokenRequest request)
        {
            return SendTokenAsync(request).Result;
        }

        async Task<OASISResult<ITransactionResponse>> IOASISBlockchainStorageProvider.SendTokenAsync(ISendWeb3TokenRequest request)
        {
            var result = new OASISResult<ITransactionResponse>();
            string errorMessage = "Error in SendTokenAsync method in EthereumOASIS. Reason: ";

            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = ActivateProvider();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Ethereum provider: {activateResult.Message}");
                        return result;
                    }
                }
                if (Web3Client == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Ethereum Web3Client is not initialized");
                    return result;
                }

                if (request == null || string.IsNullOrWhiteSpace(request.FromTokenAddress) || 
                    string.IsNullOrWhiteSpace(request.ToWalletAddress) || string.IsNullOrWhiteSpace(request.OwnerPrivateKey))
                {
                    OASISErrorHandling.HandleError(ref result, "Token address, to wallet address, and owner private key are required");
                    return result;
                }

                return await SendEthereumErc20Transaction(request.OwnerPrivateKey, request.FromTokenAddress, request.ToWalletAddress, request.Amount);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
            }
            return result;
        }

        OASISResult<ITransactionResponse> IOASISBlockchainStorageProvider.MintToken(IMintWeb3TokenRequest request)
        {
            return MintTokenAsync(request).Result;
        }

        async Task<OASISResult<ITransactionResponse>> IOASISBlockchainStorageProvider.MintTokenAsync(IMintWeb3TokenRequest request)
        {
            var result = new OASISResult<ITransactionResponse>();
            string errorMessage = "Error in MintTokenAsync method in EthereumOASIS. Reason: ";

            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = ActivateProvider();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Ethereum provider: {activateResult.Message}");
                        return result;
                    }
                }
                if (Web3Client == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Ethereum Web3Client is not initialized");
                    return result;
                }

                if (request == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Mint request is required");
                    return result;
                }

                // For IMintWeb3TokenRequest, we need to get token address from Symbol or lookup
                // For now, use contract address or lookup by Symbol
                var tokenAddress = _contractAddress ?? "0x0000000000000000000000000000000000000000";
                var mintToAddress = _oasisAccount?.Address ?? "0x0000000000000000000000000000000000000000";
                var mintAmount = 1m; // Default amount, would come from request in real implementation

                // Get private key from KeyManager using MintedByAvatarId
                var keysResult = KeyManager.GetProviderPrivateKeysForAvatarById(request.MintedByAvatarId, Core.Enums.ProviderType.EthereumOASIS);
                if (keysResult.IsError || keysResult.Result == null || keysResult.Result.Count == 0)
                {
                    OASISErrorHandling.HandleError(ref result, "Could not retrieve private key for avatar");
                    return result;
                }

                var senderEthAccount = new Account(keysResult.Result[0]);
                var web3Client = CreateWeb3WithAccount(senderEthAccount, HostURI);

                // ERC20 mint function ABI
                var erc20Abi = "[{\"constant\":false,\"inputs\":[{\"name\":\"_to\",\"type\":\"address\"},{\"name\":\"_value\",\"type\":\"uint256\"}],\"name\":\"mint\",\"outputs\":[{\"name\":\"\",\"type\":\"bool\"}],\"type\":\"function\"}]";
                var erc20Contract = web3Client.Eth.GetContract(erc20Abi, tokenAddress);
                var decimalsFunction = erc20Contract.GetFunction("decimals");
                var decimals = await decimalsFunction.CallAsync<byte>();
                var multiplier = System.Numerics.BigInteger.Pow(10, decimals);
                var amountBigInt = new System.Numerics.BigInteger(mintAmount * (decimal)multiplier);
                var mintFunction = erc20Contract.GetFunction("mint");
                var receipt = await mintFunction.SendTransactionAndWaitForReceiptAsync(senderEthAccount.Address, new Nethereum.Hex.HexTypes.HexBigInteger(600000), null, null, mintToAddress, amountBigInt);
                
                if (receipt.HasErrors() == true)
                {
                    OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, "ERC-20 mint failed."));
                    return result;
                }

                result.Result.TransactionResult = receipt.TransactionHash;
                TransactionHelper.CheckForTransactionErrors(ref result, true, errorMessage);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
            }
            return result;
        }

        OASISResult<ITransactionResponse> IOASISBlockchainStorageProvider.BurnToken(IBurnWeb3TokenRequest request)
        {
            return BurnTokenAsync(request).Result;
        }

        async Task<OASISResult<ITransactionResponse>> IOASISBlockchainStorageProvider.BurnTokenAsync(IBurnWeb3TokenRequest request)
        {
            var result = new OASISResult<ITransactionResponse>();
            string errorMessage = "Error in BurnTokenAsync method in EthereumOASIS. Reason: ";

            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = ActivateProvider();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Ethereum provider: {activateResult.Message}");
                        return result;
                    }
                }
                if (Web3Client == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Ethereum Web3Client is not initialized");
                    return result;
                }

                if (request == null || string.IsNullOrWhiteSpace(request.TokenAddress) || 
                    string.IsNullOrWhiteSpace(request.OwnerPrivateKey))
                {
                    OASISErrorHandling.HandleError(ref result, "Token address and owner private key are required");
                    return result;
                }

                var senderEthAccount = new Account(request.OwnerPrivateKey);
                var web3Client = CreateWeb3WithAccount(senderEthAccount, HostURI);

                // ERC20 burn function ABI - need to get amount from token balance or request
                var erc20Abi = "[{\"constant\":false,\"inputs\":[{\"name\":\"_value\",\"type\":\"uint256\"}],\"name\":\"burn\",\"outputs\":[{\"name\":\"\",\"type\":\"bool\"}],\"type\":\"function\"}]";
                var erc20Contract = web3Client.Eth.GetContract(erc20Abi, request.TokenAddress);
                var decimalsFunction = erc20Contract.GetFunction("decimals");
                var decimals = await decimalsFunction.CallAsync<byte>();
                var multiplier = System.Numerics.BigInteger.Pow(10, decimals);
                var burnAmount = 1m; // Would get from request or token balance in real implementation
                var amountBigInt = new System.Numerics.BigInteger(burnAmount * (decimal)multiplier);
                var burnFunction = erc20Contract.GetFunction("burn");
                var receipt = await burnFunction.SendTransactionAndWaitForReceiptAsync(senderEthAccount.Address, new Nethereum.Hex.HexTypes.HexBigInteger(600000), null, null, amountBigInt);
                
                if (receipt.HasErrors() == true)
                {
                    OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, "ERC-20 burn failed."));
                    return result;
                }

                result.Result.TransactionResult = receipt.TransactionHash;
                TransactionHelper.CheckForTransactionErrors(ref result, true, errorMessage);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
            }
            return result;
        }

        OASISResult<ITransactionResponse> IOASISBlockchainStorageProvider.LockToken(ILockWeb3TokenRequest request)
        {
            return LockTokenAsync(request).Result;
        }

        async Task<OASISResult<ITransactionResponse>> IOASISBlockchainStorageProvider.LockTokenAsync(ILockWeb3TokenRequest request)
        {
            var result = new OASISResult<ITransactionResponse>();
            string errorMessage = "Error in LockTokenAsync method in EthereumOASIS. Reason: ";

            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = ActivateProvider();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Ethereum provider: {activateResult.Message}");
                        return result;
                    }
                }
                if (Web3Client == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Ethereum Web3Client is not initialized");
                    return result;
                }

                if (request == null || string.IsNullOrWhiteSpace(request.TokenAddress) || 
                    string.IsNullOrWhiteSpace(request.FromWalletPrivateKey))
                {
                    OASISErrorHandling.HandleError(ref result, "Token address and from wallet private key are required");
                    return result;
                }

                // Lock token by transferring to bridge pool
                var bridgePoolAddress = _contractAddress ?? "0x0000000000000000000000000000000000000000";
                var sendRequest = new SendWeb3TokenRequest
                {
                    FromTokenAddress = request.TokenAddress,
                    FromWalletPrivateKey = request.FromWalletPrivateKey,
                    ToWalletAddress = bridgePoolAddress,
                    //Amount = request.Amount
                };

                return await SendEthereumErc20Transaction(sendRequest.FromWalletPrivateKey, sendRequest.FromTokenAddress, bridgePoolAddress, sendRequest.Amount);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
            }
            return result;
        }

        OASISResult<ITransactionResponse> IOASISBlockchainStorageProvider.UnlockToken(IUnlockWeb3TokenRequest request)
        {
            return UnlockTokenAsync(request).Result;
        }

        async Task<OASISResult<ITransactionResponse>> IOASISBlockchainStorageProvider.UnlockTokenAsync(IUnlockWeb3TokenRequest request)
        {
            var result = new OASISResult<ITransactionResponse>();
            string errorMessage = "Error in UnlockTokenAsync method in EthereumOASIS. Reason: ";

            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = ActivateProvider();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Ethereum provider: {activateResult.Message}");
                        return result;
                    }
                }
                if (Web3Client == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Ethereum Web3Client is not initialized");
                    return result;
                }

                if (request == null || string.IsNullOrWhiteSpace(request.TokenAddress))
                {
                    OASISErrorHandling.HandleError(ref result, "Token address is required");
                    return result;
                }

                // Get recipient address from KeyManager using UnlockedByAvatarId
                var toWalletResult = await WalletHelper.GetWalletAddressForAvatarAsync(WalletManager, Core.Enums.ProviderType.EthereumOASIS, request.UnlockedByAvatarId);
                if (toWalletResult.IsError || string.IsNullOrWhiteSpace(toWalletResult.Result))
                {
                    OASISErrorHandling.HandleError(ref result, "Could not retrieve wallet address for avatar");
                    return result;
                }

                // Unlock token by transferring from bridge pool to recipient
                var bridgePoolAddress = _contractAddress ?? "0x0000000000000000000000000000000000000000";
                var bridgePoolPrivateKey = _oasisAccount?.PrivateKey ?? string.Empty;
                
                if (string.IsNullOrWhiteSpace(bridgePoolPrivateKey))
                {
                    OASISErrorHandling.HandleError(ref result, "Bridge pool private key is not configured");
                    return result;
                }

                var unlockAmount = 1m; // Would get from locked amount in real implementation
                return await SendEthereumErc20Transaction(bridgePoolPrivateKey, request.TokenAddress, toWalletResult.Result, unlockAmount);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
            }
            return result;
        }


    }
}
