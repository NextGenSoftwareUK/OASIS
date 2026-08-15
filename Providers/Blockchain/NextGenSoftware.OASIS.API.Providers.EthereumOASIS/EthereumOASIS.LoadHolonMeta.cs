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
        public async Task<OASISResult<IHolon>> LoadHolonByCustomKeyAsync(string customKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IHolon>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Ethereum provider: {activateResult.Message}");
                        return result;
                    }
                }

                if (string.IsNullOrWhiteSpace(customKey))
                {
                    OASISErrorHandling.HandleError(ref result, "Custom key cannot be null or empty");
                    return result;
                }

                // Load holon by custom key from Ethereum smart contract
                // Try loading by provider key first (custom key might be stored as provider key)
                var holonResult = await LoadHolonAsync(customKey, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version);
                if (!holonResult.IsError && holonResult.Result != null)
                {
                    result.Result = holonResult.Result;
                    result.IsError = false;
                    result.Message = "Holon loaded successfully from Ethereum by custom key";
                }
                else
                {
                    // If not found by provider key, try searching by metadata
                    // Custom key might be stored in metadata
                    if (_nextGenSoftwareOasisService != null)
                    {
                        try
                        {
                            // Search for holons with custom key in metadata
                            var searchParams = new SearchParams
                            {
                                FilterByMetaData = new Dictionary<string, string> { ["CustomKey"] = customKey },
                                MetaKeyValuePairMatchMode = MetaKeyValuePairMatchMode.All
                            };
                            
                            var searchResult = await SearchAsync(searchParams);
                            var holonList = searchResult.Result?.SearchResultHolons ?? new List<IHolon>();
                            if (!searchResult.IsError && searchResult.Result != null && holonList.Any())
                            {
                                // Find holon where custom key matches
                                var matchingHolon = holonList.FirstOrDefault(h => 
                                    h.MetaData != null && 
                                    h.MetaData.ContainsKey("CustomKey") && 
                                    h.MetaData["CustomKey"]?.ToString() == customKey);
                                
                                if (matchingHolon != null)
                                {
                                    result.Result = matchingHolon;
                                    result.IsError = false;
                                    result.Message = "Holon loaded successfully from Ethereum by custom key (via metadata search)";
                                }
                                else
                                {
                                    OASISErrorHandling.HandleError(ref result, "Holon not found with that custom key on Ethereum");
                                }
                            }
                            else
                            {
                                OASISErrorHandling.HandleError(ref result, "Holon not found with that custom key on Ethereum");
                            }
                        }
                        catch (Exception searchEx)
                        {
                            OASISErrorHandling.HandleError(ref result, $"Failed to search for holon by custom key: {searchEx.Message}");
                        }
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref result, "Ethereum service is not initialized");
                    }
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading holon by custom key from Ethereum: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<IHolon> LoadHolonByCustomKey(string customKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadHolonByCustomKeyAsync(customKey, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }

        public async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentByCustomKeyAsync(string customKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Ethereum provider: {activateResult.Message}");
                        return result;
                    }
                }

                if (string.IsNullOrWhiteSpace(customKey))
                {
                    OASISErrorHandling.HandleError(ref result, "Custom key cannot be null or empty");
                    return result;
                }

                // First load the parent holon by custom key
                var parentResult = await LoadHolonByCustomKeyAsync(customKey, false, false, 0, continueOnError, loadChildrenFromProvider, version);
                
                if (parentResult.IsError || parentResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"Parent holon not found: {parentResult.Message}");
                    return result;
                }

                // Then load children for the parent
                var childrenResult = await LoadHolonsForParentAsync(parentResult.Result.Id, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version);
                
                result.Result = childrenResult.Result;
                result.IsError = childrenResult.IsError;
                result.Message = childrenResult.Message;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading holons for parent by custom key from Ethereum: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<IEnumerable<IHolon>> LoadHolonsForParentByCustomKey(string customKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadHolonsForParentByCustomKeyAsync(customKey, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }

        //public override Task<OASISResult<IHolon>> LoadHolonByMetaDataAsync(string metaKey, string metaValue, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        //{
        //    throw new NotImplementedException();
        //}

        //public override OASISResult<IHolon> LoadHolonByMetaData(string metaKey, string metaValue, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        //{
        //    throw new NotImplementedException();
        //}

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
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

                var response = await _httpClient.GetAsync($"{_apiBaseUrl}/holons/search?metaKey={Uri.EscapeDataString(metaKey)}&metaValue={Uri.EscapeDataString(metaValue)}&type={type}&version={version}");

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var holons = System.Text.Json.JsonSerializer.Deserialize<List<Holon>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    
                    if (holons != null)
                    {
                        result.Result = holons.Cast<IHolon>();
                        result.IsError = false;
                        result.Message = $"Successfully loaded {holons.Count} holons by metadata from Ethereum";
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
                OASISErrorHandling.HandleError(ref result, $"Error loading holons by metadata from Ethereum: {ex.Message}", ex);
            }
            return result;
        }

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

    }
}
