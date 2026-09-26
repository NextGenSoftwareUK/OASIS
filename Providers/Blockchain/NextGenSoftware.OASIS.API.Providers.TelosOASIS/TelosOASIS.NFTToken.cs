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
        public async Task<OASISResult<BridgeTransactionResponse>> WithdrawNFTAsync(string nftTokenAddress, string tokenId, string senderAccountAddress, string senderPrivateKey)
        {
            var result = new OASISResult<BridgeTransactionResponse>();
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

                if (string.IsNullOrWhiteSpace(nftTokenAddress) || string.IsNullOrWhiteSpace(tokenId) || 
                    string.IsNullOrWhiteSpace(senderAccountAddress) || string.IsNullOrWhiteSpace(senderPrivateKey))
                {
                    OASISErrorHandling.HandleError(ref result, "NFT token address, token ID, sender address, and private key are required");
                    return result;
                }

                var lockRequest = new LockWeb3NFTRequest
                {
                    NFTTokenAddress = nftTokenAddress,
                    Web3NFTId = Guid.TryParse(tokenId, out var guid) ? guid : Guid.NewGuid(),
                    LockedByAvatarId = Guid.Empty
                };

                var lockResult = await LockNFTAsync(lockRequest);
                if (lockResult.IsError || lockResult.Result == null)
                {
                    result.Result = new BridgeTransactionResponse
                    {
                        TransactionId = string.Empty,
                        IsSuccessful = false,
                        ErrorMessage = lockResult.Message,
                        Status = BridgeTransactionStatus.Canceled
                    };
                    OASISErrorHandling.HandleError(ref result, $"Failed to lock NFT: {lockResult.Message}");
                    return result;
                }

                result.Result = new BridgeTransactionResponse
                {
                    TransactionId = lockResult.Result.TransactionResult ?? string.Empty,
                    IsSuccessful = !lockResult.IsError,
                    Status = BridgeTransactionStatus.Pending
                };
                result.IsError = false;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error withdrawing NFT: {ex.Message}", ex);
                result.Result = new BridgeTransactionResponse
                {
                    TransactionId = string.Empty,
                    IsSuccessful = false,
                    ErrorMessage = ex.Message,
                    Status = BridgeTransactionStatus.Canceled
                };
            }
            return result;
        }

        public async Task<OASISResult<BridgeTransactionResponse>> DepositNFTAsync(string nftTokenAddress, string tokenId, string receiverAccountAddress, string sourceTransactionHash = null)
        {
            var result = new OASISResult<BridgeTransactionResponse>();
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

                if (string.IsNullOrWhiteSpace(nftTokenAddress) || string.IsNullOrWhiteSpace(receiverAccountAddress))
                {
                    OASISErrorHandling.HandleError(ref result, "NFT token address and receiver address are required");
                    return result;
                }

                var mintRequest = new MintWeb3NFTRequest
                {
                    SendToAddressAfterMinting = receiverAccountAddress,
                };

                var mintResult = await MintNFTAsync(mintRequest);
                if (mintResult.IsError || mintResult.Result == null)
                {
                    result.Result = new BridgeTransactionResponse
                    {
                        TransactionId = string.Empty,
                        IsSuccessful = false,
                        ErrorMessage = mintResult.Message,
                        Status = BridgeTransactionStatus.Canceled
                    };
                    OASISErrorHandling.HandleError(ref result, $"Failed to deposit/mint NFT: {mintResult.Message}");
                    return result;
                }

                result.Result = new BridgeTransactionResponse
                {
                    TransactionId = mintResult.Result.TransactionResult ?? string.Empty,
                    IsSuccessful = !mintResult.IsError,
                    Status = BridgeTransactionStatus.Pending
                };
                result.IsError = false;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error depositing NFT: {ex.Message}", ex);
                result.Result = new BridgeTransactionResponse
                {
                    TransactionId = string.Empty,
                    IsSuccessful = false,
                    ErrorMessage = ex.Message,
                    Status = BridgeTransactionStatus.Canceled
                };
            }
            return result;
        }

        public OASISResult<IWeb3NFT> LoadOnChainNFTData(string nftTokenAddress)
        {
            return LoadOnChainNFTDataAsync(nftTokenAddress).Result;
        }

        public async Task<OASISResult<IWeb3NFT>> LoadOnChainNFTDataAsync(string nftTokenAddress)
        {
            var result = new OASISResult<IWeb3NFT>();
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
                var loadResult = await _eosioOASIS?.LoadOnChainNFTDataAsync(nftTokenAddress);
                if (loadResult != null && !loadResult.IsError)
                {
                    result.Result = loadResult.Result;
                    result.IsError = false;
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to load NFT data: {loadResult?.Message}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading NFT data: {ex.Message}", ex);
            }
            return result;
        }

        public async Task<OASISResult<ITransactionResponse>> SendTokenAsync(ISendWeb3TokenRequest request)
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

                // Delegate to EOSIOOASIS for token transfer
                var eosResult = await _eosioOASIS?.SendTokenAsync(request);
                if (eosResult != null && !eosResult.IsError)
                {
                    result.Result = eosResult.Result;
                    result.IsError = false;
                    result.Message = "Token sent successfully on Telos";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to send token: {eosResult?.Message ?? "Unknown error"}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error sending token on Telos: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<ITransactionResponse> SendToken(ISendWeb3TokenRequest request)
        {
            return SendTokenAsync(request).Result;
        }

        public async Task<OASISResult<ITransactionResponse>> MintTokenAsync(IMintWeb3TokenRequest request)
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

                // Delegate to EOSIOOASIS for token minting
                var eosResult = await _eosioOASIS?.MintTokenAsync(request);
                if (eosResult != null && !eosResult.IsError)
                {
                    result.Result = eosResult.Result;
                    result.IsError = false;
                    result.Message = "Token minted successfully on Telos";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to mint token: {eosResult?.Message ?? "Unknown error"}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error minting token on Telos: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<ITransactionResponse> MintToken(IMintWeb3TokenRequest request)
        {
            return MintTokenAsync(request).Result;
        }

        public async Task<OASISResult<ITransactionResponse>> BurnTokenAsync(IBurnWeb3TokenRequest request)
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

                // Delegate to EOSIOOASIS for token burning
                var eosResult = await _eosioOASIS?.BurnTokenAsync(request);
                if (eosResult != null && !eosResult.IsError)
                {
                    result.Result = eosResult.Result;
                    result.IsError = false;
                    result.Message = "Token burned successfully on Telos";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to burn token: {eosResult?.Message ?? "Unknown error"}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error burning token on Telos: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<ITransactionResponse> BurnToken(IBurnWeb3TokenRequest request)
        {
            return BurnTokenAsync(request).Result;
        }

        public async Task<OASISResult<ITransactionResponse>> LockTokenAsync(ILockWeb3TokenRequest request)
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

                // Lock token by transferring to bridge pool account on Telos
                var bridgePoolAccount = "oasispool";
                var sendRequest = new SendWeb3TokenRequest
                {
                    FromWalletAddress = request.FromWalletAddress,
                    ToWalletAddress = bridgePoolAccount,
                    FromTokenAddress = request.TokenAddress,
                    Amount = 0 // ILockWeb3TokenRequest doesn't have Amount property
                };

                var sendResult = await SendTokenAsync(sendRequest);
                if (sendResult.IsError || sendResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to lock token: {sendResult.Message}", sendResult.Exception);
                    return result;
                }

                result.Result = sendResult.Result;
                result.IsError = false;
                result.Message = "Token locked successfully on Telos";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error locking token on Telos: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<ITransactionResponse> LockToken(ILockWeb3TokenRequest request)
        {
            return LockTokenAsync(request).Result;
        }

        public async Task<OASISResult<ITransactionResponse>> UnlockTokenAsync(IUnlockWeb3TokenRequest request)
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

                // Unlock token by transferring from bridge pool account on Telos
                var bridgePoolAccount = "oasispool";
                var sendRequest = new SendWeb3TokenRequest
                {
                    FromWalletAddress = bridgePoolAccount,
                    ToWalletAddress = "", // IUnlockWeb3TokenRequest doesn't have ToWalletAddress property
                    FromTokenAddress = request.TokenAddress,
                    Amount = 0 // IUnlockWeb3TokenRequest doesn't have Amount property
                };

                var sendResult = await SendTokenAsync(sendRequest);
                if (sendResult.IsError || sendResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to unlock token: {sendResult.Message}", sendResult.Exception);
                    return result;
                }

                result.Result = sendResult.Result;
                result.IsError = false;
                result.Message = "Token unlocked successfully on Telos";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error unlocking token on Telos: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<ITransactionResponse> UnlockToken(IUnlockWeb3TokenRequest request)
        {
            return UnlockTokenAsync(request).Result;
        }

        public async Task<OASISResult<double>> GetBalanceAsync(IGetWeb3WalletBalanceRequest request)
        {
            var result = new OASISResult<double>();
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

                // Query Telos account balance using EOSIO API
                var accountAddress = request.WalletAddress;
                if (string.IsNullOrEmpty(accountAddress))
                {
                    OASISErrorHandling.HandleError(ref result, "Wallet address is required");
                    return result;
                }

                // Use Telos API to get account balance
                var balanceUrl = $"{TELOS_API_BASE_URL}/v1/chain/get_account";
                var accountData = new { account_name = accountAddress };
                var content = new StringContent(JsonSerializer.Serialize(accountData), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(balanceUrl, content);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var accountInfo = JsonSerializer.Deserialize<JsonElement>(responseContent);

                    // Extract balance from EOS account info
                    if (accountInfo.TryGetProperty("core_liquid_balance", out var balance))
                    {
                        var balanceStr = balance.GetString() ?? "0.0000 TLOS";
                        var balanceValue = balanceStr.Split(' ')[0];
                        if (double.TryParse(balanceValue, out var balanceAmount))
                        {
                            result.Result = balanceAmount;
                            result.IsError = false;
                            result.Message = "Balance retrieved successfully from Telos";
                        }
                        else
                        {
                            OASISErrorHandling.HandleError(ref result, "Failed to parse balance from Telos response");
                        }
                    }
                    else
                    {
                        result.Result = 0.0;
                        result.IsError = false;
                        result.Message = "Account found but no balance information available";
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Telos balance query failed: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting balance from Telos: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<double> GetBalance(IGetWeb3WalletBalanceRequest request)
        {
            return GetBalanceAsync(request).Result;
        }

        public async Task<OASISResult<IList<IWalletTransaction>>> GetTransactionsAsync(IGetWeb3TransactionsRequest request)
        {
            var result = new OASISResult<IList<IWalletTransaction>>();
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

                var accountAddress = request.WalletAddress;
                if (string.IsNullOrEmpty(accountAddress))
                {
                    OASISErrorHandling.HandleError(ref result, "Wallet address is required");
                    return result;
                }

                // Query Telos transaction history
                var historyUrl = $"{TELOS_API_BASE_URL}/v1/history/get_actions";
                var historyData = new { account_name = accountAddress, pos = -1, offset = -100 }; // IGetWeb3TransactionsRequest doesn't have Limit property
                var content = new StringContent(JsonSerializer.Serialize(historyData), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(historyUrl, content);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var historyInfo = JsonSerializer.Deserialize<JsonElement>(responseContent);

                    var transactions = new List<IWalletTransaction>();
                    if (historyInfo.TryGetProperty("actions", out var actions))
                    {
                        foreach (var action in actions.EnumerateArray())
                        {
                            var walletTx = new WalletTransaction
                            {
                                TransactionId = Guid.NewGuid(),
                                FromWalletAddress = action.TryGetProperty("data", out var data) && data.TryGetProperty("from", out var from) ? from.GetString() : "",
                                ToWalletAddress = action.TryGetProperty("data", out var data2) && data2.TryGetProperty("to", out var to) ? to.GetString() : "",
                                Amount = action.TryGetProperty("data", out var data3) && data3.TryGetProperty("quantity", out var qty) ? 
                                    double.TryParse(qty.GetString()?.Split(' ')[0] ?? "0", out var amt) ? amt : 0 : 0
                            };
                            transactions.Add(walletTx);
                        }
                    }

                    result.Result = transactions;
                    result.IsError = false;
                    result.Message = $"Retrieved {transactions.Count} transactions from Telos";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Telos transactions query failed: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting transactions from Telos: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<IList<IWalletTransaction>> GetTransactions(IGetWeb3TransactionsRequest request)
        {
            return GetTransactionsAsync(request).Result;
        }

    }
}
