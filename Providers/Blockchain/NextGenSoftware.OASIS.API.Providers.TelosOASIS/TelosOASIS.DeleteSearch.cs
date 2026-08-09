using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Globalization;
using EOSNewYork.EOSCore.Response.API;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Providers.EOSIOOASIS.Entities.DTOs.GetAccount;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.GeoSpatialNFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.GeoSpatialNFT.Request;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Response;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets.Response;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Response;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses;
using System.Threading;

namespace NextGenSoftware.OASIS.API.Providers.TelosOASIS
{
    public partial class TelosOASIS
    {
        public override OASISResult<IHolon> DeleteHolon(Guid id)
        {
            return DeleteHolonAsync(id).Result;
        }

        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(Guid id)
        {
            var result = new OASISResult<IHolon>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Telos provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Load holon first to return it
                var holonResult = await LoadHolonAsync(id);
                if (holonResult.IsError || holonResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"Holon not found: {id}");
                    return result;
                }

                // Send delete transaction to Telos smart contract
                var deleteUrl = $"{TELOS_API_BASE_URL}/v1/chain/push_transaction";
                var deleteData = new
                {
                    actions = new[]
                    {
                        new
                        {
                            account = "orgs.seeds",
                            name = "deleteholon",
                            authorization = new[]
                            {
                                new { actor = "orgs.seeds", permission = "active" }
                            },
                            data = new
                            {
                                id = id.ToString()
                            }
                        }
                    }
                };

                var content = new StringContent(JsonSerializer.Serialize(deleteData), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(deleteUrl, content);

                if (response.IsSuccessStatusCode)
                {
                    result.Result = holonResult.Result;
                    result.IsError = false;
                    result.Message = $"Holon {id} deleted from Telos blockchain successfully";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to delete holon from Telos blockchain: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error deleting holon from Telos: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IHolon> DeleteHolon(string providerKey)
        {
            return DeleteHolonAsync(providerKey).Result;
        }

        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(string providerKey)
        {
            var result = new OASISResult<IHolon>();
            try
            {
                // Load holon by provider key first
                var holonResult = await LoadHolonAsync(providerKey);
                if (holonResult.IsError || holonResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"Holon not found: {providerKey}");
                    return result;
                }

                // Delegate to DeleteHolonAsync(Guid)
                return await DeleteHolonAsync(holonResult.Result.Id);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error deleting holon by provider key from Telos: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<IEnumerable<IAvatar>> GetAvatarsNearMe(long geoLat, long geoLong, int radiusInMeters)
        {
            return GetAvatarsNearMeAsync(geoLat, geoLong, radiusInMeters).Result;
        }

        public async Task<OASISResult<IEnumerable<IAvatar>>> GetAvatarsNearMeAsync(long geoLat, long geoLong, int radiusInMeters)
        {
            var result = new OASISResult<IEnumerable<IAvatar>>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Telos provider: {activateResult.Message}");
                        return result;
                    }
                }

                var avatarsResult = await LoadAllAvatarsAsync(0);
                if (avatarsResult.IsError || avatarsResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error loading avatars: {avatarsResult.Message}");
                    return result;
                }

                var centerLat = geoLat / 1e6d;
                var centerLng = geoLong / 1e6d;
                var nearby = new List<IAvatar>();

                foreach (var avatar in avatarsResult.Result)
                {
                    if (avatar.MetaData != null &&
                        avatar.MetaData.TryGetValue("Latitude", out var latObj) &&
                        avatar.MetaData.TryGetValue("Longitude", out var lngObj) &&
                        double.TryParse(latObj?.ToString(), out var lat) &&
                        double.TryParse(lngObj?.ToString(), out var lng))
                    {
                        var distance = GeoHelper.CalculateDistance(centerLat, centerLng, lat, lng);
                        if (distance <= radiusInMeters)
                            nearby.Add(avatar);
                    }
                }

                result.Result = nearby;
                result.IsError = false;
                result.Message = $"Found {nearby.Count} avatars within {radiusInMeters}m";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting avatars near me from Telos: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<IEnumerable<IHolon>> GetHolonsNearMe(long geoLat, long geoLong, int radiusInMeters, HolonType Type)
        {
            return GetHolonsNearMeAsync(geoLat, geoLong, radiusInMeters, Type).Result;
        }

        public async Task<OASISResult<IEnumerable<IHolon>>> GetHolonsNearMeAsync(long geoLat, long geoLong, int radiusInMeters, HolonType Type)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Telos provider: {activateResult.Message}");
                        return result;
                    }
                }

                var holonsResult = await LoadAllHolonsAsync(Type, true, true, 0, 0, true, false, 0);
                if (holonsResult.IsError || holonsResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error loading holons: {holonsResult.Message}");
                    return result;
                }

                var centerLat = geoLat / 1e6d;
                var centerLng = geoLong / 1e6d;
                var nearby = new List<IHolon>();

                foreach (var holon in holonsResult.Result)
                {
                    if (holon.MetaData != null &&
                        holon.MetaData.TryGetValue("Latitude", out var latObj) &&
                        holon.MetaData.TryGetValue("Longitude", out var lngObj) &&
                        double.TryParse(latObj?.ToString(), out var lat) &&
                        double.TryParse(lngObj?.ToString(), out var lng))
                    {
                        var distance = GeoHelper.CalculateDistance(centerLat, centerLng, lat, lng);
                        if (distance <= radiusInMeters)
                            nearby.Add(holon);
                    }
                }

                result.Result = nearby;
                result.IsError = false;
                result.Message = $"Found {nearby.Count} holons within {radiusInMeters}m";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting holons near me from Telos: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<ITransactionResponse> SendTransaction(string fromWalletAddress, string toWalletAddress, decimal amount, string memoText)
        {
            return SendTransactionAsync(fromWalletAddress, toWalletAddress, amount, memoText).Result;
        }

        public async Task<OASISResult<ITransactionResponse>> SendTransactionAsync(string fromWalletAddress, string toWalletAddress, decimal amount, string memoText)
        {
            var result = new OASISResult<ITransactionResponse>();

            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Telos provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Convert decimal amount to TLOS (1 TLOS = 10^4 precision)
                var amountInTLOS = (long)(amount * 10000);

                // Prepare account request to get balances
                var accountRequest = new
                {
                    account_name = fromWalletAddress
                };

                var accountJson = JsonSerializer.Serialize(accountRequest);
                var accountContent = new StringContent(accountJson, Encoding.UTF8, "application/json");
                var accountResult = await _httpClient.PostAsync("/v1/chain/get_account", accountContent);

                if (!accountResult.IsSuccessStatusCode)
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to get account info for Telos address {fromWalletAddress}: {accountResult.StatusCode}");
                    return result;
                }

                var accountData = JsonSerializer.Deserialize<JsonElement>(await accountResult.Content.ReadAsStringAsync());

                // Parse core_liquid_balance safely
                decimal balanceDecimal = 0m;
                if (accountData.TryGetProperty("core_liquid_balance", out var coreBal) && coreBal.ValueKind == JsonValueKind.String)
                {
                    var balanceStr = coreBal.GetString();
                    if (!string.IsNullOrEmpty(balanceStr))
                    {
                        // balance typically like "1.2345 TLOS"
                        var parts = balanceStr.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length > 0)
                        {
                            Decimal.TryParse(parts[0], NumberStyles.Number, CultureInfo.InvariantCulture, out balanceDecimal);
                        }
                    }
                }

                var balanceValue = (long)(balanceDecimal * 10000m);

                if (balanceValue < amountInTLOS)
                {
                    OASISErrorHandling.HandleError(ref result, $"Insufficient balance. Available: {balanceDecimal} TLOS, Required: {(decimal)amountInTLOS / 10000m} TLOS");
                    return result;
                }

                // Create Telos transfer transaction
                var transferRequest = new
                {
                    actions = new[]
                    {
                        new
                        {
                            account = "eosio.token",
                            name = "transfer",
                            authorization = new[]
                            {
                                new
                                {
                                    actor = fromWalletAddress,
                                    permission = "active"
                                }
                            },
                            data = new
                            {
                                from = fromWalletAddress,
                                to = toWalletAddress,
                                quantity = string.Format(CultureInfo.InvariantCulture, "{0:F4} TLOS", amount),
                                memo = memoText
                            }
                        }
                    }
                };

                // Submit transaction to Telos network
                var jsonContent = JsonSerializer.Serialize(transferRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var submitResponse = await _httpClient.PostAsync("/v1/chain/push_transaction", content);
                if (submitResponse.IsSuccessStatusCode)
                {
                    var responseContent = await submitResponse.Content.ReadAsStringAsync();
                    var responseData = JsonSerializer.Deserialize<JsonElement>(responseContent);

                    string txId = responseData.TryGetProperty("transaction_id", out var txProp) ? txProp.GetString() : null;

                    result.Result = new TransactionResponse
                    {
                        TransactionResult = txId
                    };
                    result.IsError = false;
                    result.Message = $"Telos transaction sent successfully. TX ID: {result.Result.TransactionResult}";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to submit Telos transaction: {submitResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result, $"Error sending Telos transaction: {ex.Message}");
            }

            return result;
        }


        public OASISResult<IWeb3NFTTransactionResponse> SendNFT(ISendWeb3NFTRequest transaction)
        {
            return SendNFTAsync(transaction).Result;
        }

        public async Task<OASISResult<IWeb3NFTTransactionResponse>> SendNFTAsync(ISendWeb3NFTRequest transaction)
        {
            var result = new OASISResult<IWeb3NFTTransactionResponse>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Telos provider: {activateResult.Message}");
                        return result;
                    }
                }
                // Delegate to EOSIOOASIS for NFT operations
                var sendResult = await _eosioOASIS?.SendNFTAsync(transaction);
                if (sendResult != null && sendResult.Result != null && !sendResult.IsError)
                {
                    result.Result.TransactionResult = sendResult.Result.TransactionResult;
                    result.IsError = false;
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to send NFT: {sendResult?.Message}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error sending NFT: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<IWeb3NFTTransactionResponse> MintNFT(IMintWeb3NFTRequest transaction)
        {
            return MintNFTAsync(transaction).Result;
        }

        public async Task<OASISResult<IWeb3NFTTransactionResponse>> MintNFTAsync(IMintWeb3NFTRequest transaction)
        {
            var result = new OASISResult<IWeb3NFTTransactionResponse>();
            result.Result = new Web3NFTTransactionResponse();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Telos provider: {activateResult.Message}");
                        return result;
                    }
                }
                var mintResult = await _eosioOASIS?.MintNFTAsync(transaction);
                if (mintResult != null && mintResult.Result != null && !mintResult.IsError)
                {
                    result.Result.TransactionResult = mintResult.Result.TransactionResult;
                    result.IsError = false;
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to mint NFT: {mintResult?.Message}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error minting NFT: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<IWeb3NFTTransactionResponse> BurnNFT(IBurnWeb3NFTRequest request)
        {
            return BurnNFTAsync(request).Result;
        }

        public async Task<OASISResult<IWeb3NFTTransactionResponse>> BurnNFTAsync(IBurnWeb3NFTRequest request)
        {
            var result = new OASISResult<IWeb3NFTTransactionResponse>();
            result.Result = new Web3NFTTransactionResponse();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Telos provider: {activateResult.Message}");
                        return result;
                    }
                }
                var burnResult = await _eosioOASIS?.BurnNFTAsync(request);
                if (burnResult != null && burnResult.Result != null && !burnResult.IsError)
                {
                    result.Result.TransactionResult = burnResult.Result.TransactionResult;
                    result.IsError = false;
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to burn NFT: {burnResult?.Message}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error burning NFT: {ex.Message}", ex);
            }
            return result;
        }

        // NFT-specific lock/unlock methods
        public OASISResult<IWeb3NFTTransactionResponse> LockNFT(ILockWeb3NFTRequest request)
        {
            return LockNFTAsync(request).Result;
        }

        public async Task<OASISResult<IWeb3NFTTransactionResponse>> LockNFTAsync(ILockWeb3NFTRequest request)
        {
            var result = new OASISResult<IWeb3NFTTransactionResponse>();
            result.Result = new Web3NFTTransactionResponse();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Telos provider: {activateResult.Message}");
                        return result;
                    }
                }

                var bridgePoolAccount = "oasisbridge";
                var sendRequest = new SendWeb3NFTRequest
                {
                    FromNFTTokenAddress = request.NFTTokenAddress,
                    FromWalletAddress = string.Empty,
                    ToWalletAddress = bridgePoolAccount,
                    TokenAddress = request.NFTTokenAddress,
                    TokenId = request.Web3NFTId.ToString(),
                    Amount = 1
                };

                var sendResult = await SendNFTAsync(sendRequest);
                if (sendResult.IsError || sendResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to lock NFT: {sendResult.Message}", sendResult.Exception);
                    return result;
                }

                result.IsError = false;
                result.Result.TransactionResult = sendResult.Result.TransactionResult;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error locking NFT: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<IWeb3NFTTransactionResponse> UnlockNFT(IUnlockWeb3NFTRequest request)
        {
            return UnlockNFTAsync(request).Result;
        }

        public async Task<OASISResult<IWeb3NFTTransactionResponse>> UnlockNFTAsync(IUnlockWeb3NFTRequest request)
        {
            var result = new OASISResult<IWeb3NFTTransactionResponse>();
            result.Result = new Web3NFTTransactionResponse();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Telos provider: {activateResult.Message}");
                        return result;
                    }
                }

                var bridgePoolAccount = "oasisbridge";
                var sendRequest = new SendWeb3NFTRequest
                {
                    FromNFTTokenAddress = request.NFTTokenAddress,
                    FromWalletAddress = bridgePoolAccount,
                    ToWalletAddress = string.Empty,
                    TokenAddress = request.NFTTokenAddress,
                    TokenId = request.Web3NFTId.ToString(),
                    Amount = 1
                };

                var sendResult = await SendNFTAsync(sendRequest);
                if (sendResult.IsError || sendResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to unlock NFT: {sendResult.Message}", sendResult.Exception);
                    return result;
                }

                result.IsError = false;
                result.Result.TransactionResult = sendResult.Result.TransactionResult;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error unlocking NFT: {ex.Message}", ex);
            }
            return result;
        }

        // NFT Bridge Methods
    }
}
