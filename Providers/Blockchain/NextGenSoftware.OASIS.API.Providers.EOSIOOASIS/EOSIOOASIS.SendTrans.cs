using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EOSNewYork.EOSCore;
using Newtonsoft.Json;
using NextGenSoftware.Logging;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.Avatar;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets.Response;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Utilities;
using NextGenSoftware.OASIS.API.Providers.EOSIOOASIS.Entities.DTOs.CurrencyBalance;
using NextGenSoftware.OASIS.API.Providers.EOSIOOASIS.Entities.DTOs.GetAccount;
using NextGenSoftware.OASIS.API.Providers.EOSIOOASIS.Entities.Models;
using NextGenSoftware.OASIS.API.Providers.EOSIOOASIS.Infrastructure.EOSClient;
using NextGenSoftware.OASIS.API.Providers.EOSIOOASIS.Infrastructure.Persistence;
using NextGenSoftware.OASIS.API.Providers.EOSIOOASIS.Infrastructure.Repository;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using NextGenSoftware.Utilities.ExtentionMethods;
using NextGenSoftware.OASIS.API.Core.Objects;
using Nethereum.Signer;
using Nethereum.Hex.HexConvertors.Extensions;
using System.IO;
using System.Text.Json;

namespace NextGenSoftware.OASIS.API.Providers.EOSIOOASIS
{
    public partial class EOSIOOASIS
    {
        public OASISResult<IWeb3NFTTransactionResponse> MintNFT(IMintWeb3NFTRequest transation)
        {
            return MintNFTAsync(transation).Result;
        }

        public async Task<OASISResult<IWeb3NFTTransactionResponse>> MintNFTAsync(IMintWeb3NFTRequest transation)
        {
            var result = new OASISResult<IWeb3NFTTransactionResponse>();
            string errorMessage = "Error in MintNFT method in EOSIOOASIS Provider. Reason: ";

            try
            {
                if (transation == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Transaction request is null");
                    return result;
                }

                // Get wallet address for the avatar
                var walletResult = await WalletHelper.GetWalletAddressForAvatarAsync(WalletManager, ProviderType.Value, transation.MintedByAvatarId);
                if (walletResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Failed to get wallet address: {walletResult.Message}");
                    return result;
                }

                // Create EOSIO NFT mint transaction
                var mintTransaction = new
                {
                    from = walletResult.Result,
                    to = transation.SendToAddressAfterMinting,
                    title = transation.Title,
                    description = transation.Description,
                    imageUrl = transation.ImageUrl,
                    jsonMetaData = transation.JSONMetaData,
                    memo = $"OASIS NFT mint transaction for {transation.MintedByAvatarId}"
                };

                // Implement NFT minting using EOSIO transfer repository
                if (_transferRepository != null && !string.IsNullOrWhiteSpace(transation.SendToAddressAfterMinting))
                {
                    // Mint NFT by transferring from zero address (minting)
                    var mintResult = await _transferRepository.TransferEosNft(walletResult.Result, transation.SendToAddressAfterMinting, 0, transation.Symbol ?? "SYS");

                    if (mintResult != null && !mintResult.IsError && mintResult.Result != null)
                    {
                        result.Result = new Web3NFTTransactionResponse
                        {
                            TransactionResult = mintResult.Result.TransactionResult ?? "NFT minted successfully",
                            Web3NFT = new Web3NFT
                            {
                                Title = transation.Title ?? "EOSIO NFT",
                                Description = transation.Description ?? "NFT minted via OASIS",
                                ImageUrl = transation.ImageUrl ?? "",
                                NFTTokenAddress = "" // Will be set after minting
                            }
                        };
                        result.IsError = false;
                        result.IsSaved = true;
                        result.Message = "EOSIO NFT minted successfully";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} Failed to mint NFT: {mintResult?.Message ?? "Unknown error"}");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Transfer repository not available or send address not provided");
                }

                // Legacy commented code for reference:
                //var transactionResult = _transferRepository.TransferEosNft(walletResult.Result, transation.SendToAddressAfterMinting, 0).Result;

                //if (transactionResult != null)
                //{
                //    result.Result = new NextGenSoftware.OASIS.API.Core.Objects.Wallets.Response.Web4NFTTransactionRespone
                //    {
                //        TransactionResult = transactionResult.
                //        OASISNFT = null // Will be populated after NFT creation
                //    };
                //    result.IsError = false;
                //    result.IsSaved = true;
                //}
                //else
                //{
                //    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Failed to create NFT transaction");
                //}
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} {ex.Message}", ex);
            }

            return result;
        }


        public async Task<GetAccountResponseDto> GetEOSIOAccountAsync(string eosioAccountName)
        {
            var accountResult = new OASISResult<GetAccountResponseDto>();
            try
            {
                var accountResponseDto = await _eosClient.GetAccount(new GetAccountDtoRequest()
                {
                    AccountName = eosioAccountName
                });
                accountResult.Result = accountResponseDto;
            }
            catch (Exception e)
            {
                accountResult.Result = null;

                OASISErrorHandling.HandleError(ref accountResult, e.Message);
            }

            return accountResult.Result;
        }

        public GetAccountResponseDto GetEOSIOAccount(string eosioAccountName)
        {
            var accountResult = new OASISResult<GetAccountResponseDto>();
            try
            {
                var accountResponseDto = _eosClient.GetAccount(new GetAccountDtoRequest()
                {
                    AccountName = eosioAccountName
                }).Result;
                accountResult.Result = accountResponseDto;
            }
            catch (Exception e)
            {
                accountResult.Result = null;

                OASISErrorHandling.HandleError(ref accountResult, e.Message);
            }

            return accountResult.Result;
        }

        public async Task<string> GetBalanceAsync(string eosioAccountName, string code, string symbol)
        {
            var balanceResult = new OASISResult<string>();
            try
            {
                var currencyBalances = await _eosClient.GetCurrencyBalance(new GetCurrencyBalanceRequestDto()
                {
                    Account = eosioAccountName,
                    Code = code,
                    Symbol = symbol
                });
                balanceResult.Result = currencyBalances != null ? currencyBalances[0] : string.Empty;
            }
            catch (Exception e)
            {
                balanceResult.Result = string.Empty;

                OASISErrorHandling.HandleError(ref balanceResult, e.Message);
            }

            return balanceResult.Result;
        }

        public string GetBalanceForEOSIOAccount(string eosioAccountName, string code, string symbol)
        {
            var balanceResult = new OASISResult<string>();
            try
            {
                var currencyBalances = _eosClient.GetCurrencyBalance(new GetCurrencyBalanceRequestDto()
                {
                    Account = eosioAccountName,
                    Code = code,
                    Symbol = symbol
                }).Result;
                balanceResult.Result = currencyBalances != null ? currencyBalances[0] : string.Empty;
            }
            catch (Exception e)
            {
                balanceResult.Result = string.Empty;

                OASISErrorHandling.HandleError(ref balanceResult, e.Message);
            }

            return balanceResult.Result;
        }

        public string GetBalanceForAvatar(Guid avatarId, string code, string symbol)
        {
            //TODO: Add support for multiple accounts later.
            return GetBalanceForEOSIOAccount(GetEOSIOAccountNamesForAvatar(avatarId)[0], code, symbol);
        }

        public List<string> GetEOSIOAccountNamesForAvatar(Guid avatarId)
        {
            try
            {
                var result = KeyManager.GetProviderPublicKeysForAvatarById(avatarId, Core.Enums.ProviderType.EOSIOOASIS);
                if (result.IsError)
                {
                    LoggingManager.Log($"Error getting EOSIO account names for avatar {avatarId}: {result.Message}", NextGenSoftware.Logging.LogType.Error);
                    return new List<string>();
                }
                return result.Result;
            }
            catch (Exception ex)
            {
                LoggingManager.Log($"Exception getting EOSIO account names for avatar {avatarId}: {ex.Message}", NextGenSoftware.Logging.LogType.Error);
                return new List<string>();
            }
        }

        public string GetEOSIOAccountPrivateKeyForAvatar(Guid avatarId)
        {
            try
            {
                var result = KeyManager.GetProviderPrivateKeysForAvatarById(avatarId, Core.Enums.ProviderType.EOSIOOASIS);
                if (result.IsError || result.Result == null || !result.Result.Any())
                {
                    LoggingManager.Log($"Error getting EOSIO private key for avatar {avatarId}: {result.Message}", NextGenSoftware.Logging.LogType.Error);
                    return string.Empty;
                }
                return result.Result[0];
            }
            catch (Exception ex)
            {
                LoggingManager.Log($"Exception getting EOSIO private key for avatar {avatarId}: {ex.Message}", NextGenSoftware.Logging.LogType.Error);
                return string.Empty;
            }
        }

        public GetAccountResponseDto GetEOSIOAccountForAvatar(Guid avatarId)
        {
            try
            {
                // Check cache first for performance
                if (_avatarIdToEOSIOAccountLookup.ContainsKey(avatarId))
                    return _avatarIdToEOSIOAccountLookup[avatarId];

                // Get account names for avatar
                var accountNames = GetEOSIOAccountNamesForAvatar(avatarId);
                if (accountNames == null || !accountNames.Any())
                {
                    LoggingManager.Log($"No EOSIO account names found for avatar {avatarId}", NextGenSoftware.Logging.LogType.Warning);
                    return null;
                }

                // Get account details for the first account (support for multiple accounts can be added later)
                var accountResult = GetEOSIOAccountAsync(accountNames[0]).Result;
                if (accountResult != null)
                {
                    _avatarIdToEOSIOAccountLookup[avatarId] = accountResult;
                }

                return accountResult;
            }
            catch (Exception ex)
            {
                LoggingManager.Log($"Exception getting EOSIO account for avatar {avatarId}: {ex.Message}", NextGenSoftware.Logging.LogType.Error);
                return null;
            }
        }

        public Guid GetAvatarIdForEOSIOAccountName(string eosioAccountName)
        {
            try
            {
                var result = KeyManager.GetAvatarIdForProviderPublicKey(eosioAccountName, Core.Enums.ProviderType.EOSIOOASIS);
                if (result.IsError)
                {
                    LoggingManager.Log($"Error getting avatar ID for EOSIO account {eosioAccountName}: {result.Message}", NextGenSoftware.Logging.LogType.Error);
                    return Guid.Empty;
                }
                return result.Result;
            }
            catch (Exception ex)
            {
                LoggingManager.Log($"Exception getting avatar ID for EOSIO account {eosioAccountName}: {ex.Message}", NextGenSoftware.Logging.LogType.Error);
                return Guid.Empty;
            }
        }

        public IAvatar GetAvatarForEOSIOAccountName(string eosioAccountName)
        {
            try
            {
                var result = KeyManager.GetAvatarForProviderPublicKey(eosioAccountName, Core.Enums.ProviderType.EOSIOOASIS);
                if (result.IsError)
                {
                    LoggingManager.Log($"Error getting avatar for EOSIO account {eosioAccountName}: {result.Message}", LogType.Error);
                    return null;
                }
                return result.Result;
            }
            catch (Exception ex)
            {
                LoggingManager.Log($"Exception getting avatar for EOSIO account {eosioAccountName}: {ex.Message}", LogType.Error);
                return null;
            }
        }

        // Removed explicit interface implementations that don't exist in the interface

        public void Dispose()
        {
            try
            {
                // Dispose of EOSIO client and repositories
                _eosClient?.Dispose();
                _avatarRepository = null;
                _avatarDetailRepository = null;
                _holonRepository = null;
                _transferRepository = null;
                _avatarManager = null;
                _keyManager = null;
                _walletManager = null;
            }
            catch (Exception ex)
            {
                // Log disposal error but don't throw
                LoggingManager.Log($"Error disposing EOSIOOASIS provider: {ex.Message}", LogType.Error);
            }
        }

        //public override Task<OASISResult<IHolon>> LoadHolonByCustomKeyAsync(string customKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        //{
        //    throw new NotImplementedException();
        //}

        //public override OASISResult<IHolon> LoadHolonByCustomKey(string customKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        //{
        //    throw new NotImplementedException();
        //}

        //public override Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentByCustomKeyAsync(string customKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        //{
        //    throw new NotImplementedException();
        //}

        //public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParentByCustomKey(string customKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        //{
        //    throw new NotImplementedException();
        //}

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
            string errorMessage = "Error in LoadHolonsByMetaDataAsync method in EOSIOOASIS Provider. Reason: ";

            try
            {
                if (string.IsNullOrEmpty(metaKey) || string.IsNullOrEmpty(metaValue))
                {
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} MetaKey and MetaValue cannot be null or empty");
                    return result;
                }

                // Search for holons by metadata using EOSIO repository
                var holons = await _holonRepository.ReadAllByMetaData(metaKey, metaValue, type);

                if (holons != null && holons.Any())
                {
                    var holonList = new List<IHolon>();
                    foreach (var holonDto in holons)
                    {
                        var holon = JsonConvert.DeserializeObject<Holon>(holonDto.Info);
                        if (holon != null)
                        {
                            holonList.Add(holon);
                        }
                    }

                    result.Result = holonList;
                    result.IsError = false;
                }
                else
                {
                    result.Result = new List<IHolon>();
                    result.IsError = false;
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} {ex.Message}", ex);
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
            string errorMessage = "Error in LoadHolonsByMetaDataAsync method in EOSIOOASIS Provider. Reason: ";

            try
            {
                if (metaKeyValuePairs == null || !metaKeyValuePairs.Any())
                {
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} MetaKeyValuePairs cannot be null or empty");
                    return result;
                }

                // Search for holons by multiple metadata key-value pairs using EOSIO repository
                var holons = await _holonRepository.ReadAllByMetaData(metaKeyValuePairs, metaKeyValuePairMatchMode, type);

                if (holons != null && holons.Any())
                {
                    var holonList = new List<IHolon>();
                    foreach (var holonDto in holons)
                    {
                        var holon = JsonConvert.DeserializeObject<Holon>(holonDto.Info);
                        if (holon != null)
                        {
                            holonList.Add(holon);
                        }
                    }

                    result.Result = holonList;
                    result.IsError = false;
                }
                else
                {
                    result.Result = new List<IHolon>();
                    result.IsError = false;
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} {ex.Message}", ex);
            }

            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadHolonsByMetaDataAsync(metaKeyValuePairs, metaKeyValuePairMatchMode, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }

        public async Task<OASISResult<IWeb3NFT>> LoadOnChainNFTDataAsync(string nftTokenAddress)
        {
            var result = new OASISResult<IWeb3NFT>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = ActivateProvider();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate EOSIO provider: {activateResult.Message}");
                        return result;
                    }
                }

                if (string.IsNullOrWhiteSpace(nftTokenAddress))
                {
                    OASISErrorHandling.HandleError(ref result, "NFT token address is required");
                    return result;
                }

                // Query EOSIO NFT data from chain using HTTP API
                // EOSIO NFTs are typically stored in a table on the contract
                using (var httpClient = new System.Net.Http.HttpClient())
                {
                    var apiUrl = $"{HostURI}/v1/chain/get_table_rows";
                    var requestData = new
                    {
                        json = true,
                        code = nftTokenAddress,
                        scope = nftTokenAddress,
                        table = "nfts",
                        limit = 10,
                        lower_bound = ""
                    };

                    var content = new System.Net.Http.StringContent(Newtonsoft.Json.JsonConvert.SerializeObject(requestData), System.Text.Encoding.UTF8, "application/json");
                    var response = await httpClient.PostAsync(apiUrl, content);

                    if (response.IsSuccessStatusCode)
                    {
                        var responseContent = await response.Content.ReadAsStringAsync();
                        var nftData = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(responseContent);

                        if (nftData?.rows != null && nftData.rows.Count > 0)
                        {
                            // Parse first NFT from the table
                            var nftRow = nftData.rows[0];
                            var nft = new Web3NFT
                            {
                                NFTTokenAddress = nftTokenAddress,
                                Title = nftRow.name?.ToString() ?? "",
                                Description = nftRow.description?.ToString() ?? ""
                            };

                            result.Result = nft;
                            result.IsError = false;
                            result.Message = "NFT data loaded successfully from EOSIO chain";
                        }
                        else
                        {
                            OASISErrorHandling.HandleError(ref result, "NFT not found on EOSIO chain");
                        }
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to query EOSIO chain: {response.StatusCode}");
                    }
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading NFT data from EOSIO chain: {ex.Message}", ex);
            }
            return result;
        }

    }
}
