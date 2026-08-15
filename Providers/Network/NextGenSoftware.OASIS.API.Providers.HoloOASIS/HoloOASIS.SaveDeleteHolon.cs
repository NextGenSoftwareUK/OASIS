using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.Holochain.HoloNET.Client;
using NextGenSoftware.Holochain.HoloNET.Client.Interfaces;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using System.IO;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Response;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets.Response;
using NextGenSoftware.OASIS.API.Providers.HoloOASIS.Repositories;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using DataHelper = NextGenSoftware.OASIS.API.Providers.HoloOASIS.Helpers.DataHelper;
using NextGenSoftware.Utilities.ExtentionMethods;
using NextGenSoftware.OASIS.API.DNA;

namespace NextGenSoftware.OASIS.API.Providers.HoloOASIS
{
    public partial class HoloOASIS
    {
        public OASISResult<ITransactionResponse> SendToken(string fromWalletAddress, string toWalletAddress, decimal amount, string memoText)
        {
            var request = new SendWeb3TokenRequest
            {
                FromWalletAddress = fromWalletAddress,
                ToWalletAddress = toWalletAddress,
                Amount = amount,
                MemoText = memoText
            };

            return SendTokenAsync(request).Result;
        }

        public async Task<OASISResult<ITransactionResponse>> SendTokenAsync(string fromWalletAddress, string toWalletAddress, decimal amount, string memoText)
        {
            var request = new SendWeb3TokenRequest
            {
                FromWalletAddress = fromWalletAddress,
                ToWalletAddress = toWalletAddress,
                Amount = amount,
                MemoText = memoText
            };

            return await SendTokenAsync(request);
        }

        public OASISResult<ITransactionResponse> SendToken(ISendWeb3TokenRequest transation)
        {
            return SendTokenAsync(transation).Result;
        }

        public async Task<OASISResult<ITransactionResponse>> SendTokenAsync(ISendWeb3TokenRequest transation)
        {
            var result = new OASISResult<ITransactionResponse>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Holo provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Create Holochain transaction
                var transaction = new
                {
                    from = transation.FromWalletAddress,
                    to = transation.ToWalletAddress,
                    amount = transation.Amount.ToString(),
                    memo = transation.MemoText,
                    timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
                };

                var json = JsonSerializer.Serialize(transaction);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("/transactions", content);
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var responseData = JsonSerializer.Deserialize<Dictionary<string, object>>(responseContent);
                    
                    var transactionResponse = new TransactionResponse
                    {
                        TransactionResult = responseData?.GetValueOrDefault("hash")?.ToString() ?? "transaction-completed"
                    };
                    
                    result.Result = transactionResponse;
                    result.IsError = false;
                    result.Message = "Transaction sent successfully via Holochain";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to send transaction via Holochain: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error sending transaction via Holochain: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<ITransactionResponse> SendTransactionById(Guid fromAvatarId, Guid toAvatarId, decimal amount)
        {
            return SendTransactionByIdAsync(fromAvatarId, toAvatarId, amount).Result;
        }

        public async Task<OASISResult<ITransactionResponse>> SendTransactionByIdAsync(Guid fromAvatarId, Guid toAvatarId, decimal amount)
        {
            var result = new OASISResult<ITransactionResponse>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Holo provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Get wallet addresses for avatars
                var fromWalletResult = await WalletHelper.GetWalletAddressForAvatarAsync(WalletManager.Instance, Core.Enums.ProviderType.HoloOASIS, fromAvatarId, _httpClient);
                var toWalletResult = await WalletHelper.GetWalletAddressForAvatarAsync(WalletManager.Instance, Core.Enums.ProviderType.HoloOASIS, toAvatarId, _httpClient);

                if (fromWalletResult.IsError || toWalletResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error getting wallet addresses: {fromWalletResult.Message} {toWalletResult.Message}");
                    return result;
                }

                // Create transaction request
                var transactionRequest = new SendWeb3TokenRequest
                {
                    FromWalletAddress = fromWalletResult.Result,
                    ToWalletAddress = toWalletResult.Result,
                    Amount = amount,
                    MemoText = ""
                };

                return await SendTokenAsync(transactionRequest);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error sending transaction via Holochain: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<ITransactionResponse> SendTransactionById(Guid fromAvatarId, Guid toAvatarId, decimal amount, string token)
        {
            return SendTransactionByIdAsync(fromAvatarId, toAvatarId, amount, token).Result;
        }

        public async Task<OASISResult<ITransactionResponse>> SendTransactionByIdAsync(Guid fromAvatarId, Guid toAvatarId, decimal amount, string token)
        {
            var result = new OASISResult<ITransactionResponse>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Holo provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Get wallet addresses for avatars
                var fromWalletResult = await WalletHelper.GetWalletAddressForAvatarAsync(WalletManager.Instance, Core.Enums.ProviderType.HoloOASIS, fromAvatarId, _httpClient);
                var toWalletResult = await WalletHelper.GetWalletAddressForAvatarAsync(WalletManager.Instance, Core.Enums.ProviderType.HoloOASIS, toAvatarId, _httpClient);

                if (fromWalletResult.IsError || toWalletResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error getting wallet addresses: {fromWalletResult.Message} {toWalletResult.Message}");
                    return result;
                }

                // Create token transaction request
                var transactionRequest = new SendWeb3TokenRequest
                {
                    FromWalletAddress = fromWalletResult.Result,
                    ToWalletAddress = toWalletResult.Result,
                    Amount = amount,
                    MemoText = $"Token: {token}"
                };

                return await SendTokenAsync(transactionRequest);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error sending token transaction via Holochain: {ex.Message}", ex);
            }
            return result;
        }

        public async Task<OASISResult<ITransactionResponse>> SendTransactionByUsernameAsync(string fromAvatarUsername, string toAvatarUsername, decimal amount)
        {
            var result = new OASISResult<ITransactionResponse>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Holo provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Get wallet addresses for avatars by username
                var fromWalletResult = await WalletHelper.GetWalletAddressForAvatarByUsernameAsync(WalletManager.Instance, Core.Enums.ProviderType.HoloOASIS, fromAvatarUsername, _httpClient);
                var toWalletResult = await WalletHelper.GetWalletAddressForAvatarByUsernameAsync(WalletManager.Instance, Core.Enums.ProviderType.HoloOASIS, toAvatarUsername, _httpClient);

                if (fromWalletResult.IsError || toWalletResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error getting wallet addresses: {fromWalletResult.Message} {toWalletResult.Message}");
                    return result;
                }

                // Create transaction request
                var transactionRequest = new SendWeb3TokenRequest
                {
                    FromWalletAddress = fromWalletResult.Result,
                    ToWalletAddress = toWalletResult.Result,
                    Amount = amount,
                    MemoText = ""
                };

                return await SendTokenAsync(transactionRequest);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error sending transaction via Holochain: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<ITransactionResponse> SendTransactionByUsername(string fromAvatarUsername, string toAvatarUsername, decimal amount)
        {
            return SendTransactionByUsernameAsync(fromAvatarUsername, toAvatarUsername, amount).Result;
        }

        public async Task<OASISResult<ITransactionResponse>> SendTransactionByUsernameAsync(string fromAvatarUsername, string toAvatarUsername, decimal amount, string token)
        {
            var result = new OASISResult<ITransactionResponse>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Holo provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Get wallet addresses for avatars by username
                var fromWalletResult = await WalletHelper.GetWalletAddressForAvatarByUsernameAsync(WalletManager.Instance, Core.Enums.ProviderType.HoloOASIS, fromAvatarUsername, _httpClient);
                var toWalletResult = await WalletHelper.GetWalletAddressForAvatarByUsernameAsync(WalletManager.Instance, Core.Enums.ProviderType.HoloOASIS, toAvatarUsername, _httpClient);

                if (fromWalletResult.IsError || toWalletResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error getting wallet addresses: {fromWalletResult.Message} {toWalletResult.Message}");
                    return result;
                }

                // Create token transaction request
                var transactionRequest = new SendWeb3TokenRequest
                {
                    FromWalletAddress = fromWalletResult.Result,
                    ToWalletAddress = toWalletResult.Result,
                    Amount = amount,
                    MemoText = $"Token: {token}"
                };

                return await SendTokenAsync(transactionRequest);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error sending token transaction via Holochain: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<ITransactionResponse> SendTransactionByUsername(string fromAvatarUsername, string toAvatarUsername, decimal amount, string token)
        {
            return SendTransactionByUsernameAsync(fromAvatarUsername, toAvatarUsername, amount, token).Result;
        }

        public async Task<OASISResult<ITransactionResponse>> SendTransactionByEmailAsync(string fromAvatarEmail, string toAvatarEmail, decimal amount)
        {
            var result = new OASISResult<ITransactionResponse>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Holo provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Get wallet addresses for avatars by email
                var fromWalletResult = await WalletHelper.GetWalletAddressForAvatarByEmailAsync(WalletManager.Instance, Core.Enums.ProviderType.HoloOASIS, fromAvatarEmail, _httpClient);
                var toWalletResult = await WalletHelper.GetWalletAddressForAvatarByEmailAsync(WalletManager.Instance, Core.Enums.ProviderType.HoloOASIS, toAvatarEmail, _httpClient);

                if (fromWalletResult.IsError || toWalletResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error getting wallet addresses: {fromWalletResult.Message} {toWalletResult.Message}");
                    return result;
                }

                // Create transaction request
                var transactionRequest = new SendWeb3TokenRequest
                {
                    FromWalletAddress = fromWalletResult.Result,
                    ToWalletAddress = toWalletResult.Result,
                    Amount = amount,
                    MemoText = ""
                };

                return await SendTokenAsync(transactionRequest);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error sending transaction via Holochain: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<ITransactionResponse> SendTransactionByEmail(string fromAvatarEmail, string toAvatarEmail, decimal amount)
        {
            return SendTransactionByEmailAsync(fromAvatarEmail, toAvatarEmail, amount).Result;
        }

        public async Task<OASISResult<ITransactionResponse>> SendTransactionByEmailAsync(string fromAvatarEmail, string toAvatarEmail, decimal amount, string token)
        {
            var result = new OASISResult<ITransactionResponse>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Holo provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Get wallet addresses for avatars by email
                var fromWalletResult = await WalletHelper.GetWalletAddressForAvatarByEmailAsync(WalletManager.Instance, Core.Enums.ProviderType.HoloOASIS, fromAvatarEmail, _httpClient);
                var toWalletResult = await WalletHelper.GetWalletAddressForAvatarByEmailAsync(WalletManager.Instance, Core.Enums.ProviderType.HoloOASIS, toAvatarEmail, _httpClient);

                if (fromWalletResult.IsError || toWalletResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error getting wallet addresses: {fromWalletResult.Message} {toWalletResult.Message}");
                    return result;
                }

                // Create token transaction request
                var transactionRequest = new SendWeb3TokenRequest
                {
                    FromWalletAddress = fromWalletResult.Result,
                    ToWalletAddress = toWalletResult.Result,
                    Amount = amount,
                    MemoText = $"Token: {token}"
                };

                return await SendTokenAsync(transactionRequest);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error sending token transaction via Holochain: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<ITransactionResponse> SendTransactionByEmail(string fromAvatarEmail, string toAvatarEmail, decimal amount, string token)
        {
            return SendTransactionByEmailAsync(fromAvatarEmail, toAvatarEmail, amount, token).Result;
        }

        public OASISResult<ITransactionResponse> SendTransactionByDefaultWallet(Guid fromAvatarId, Guid toAvatarId, decimal amount)
        {
            return SendTransactionByDefaultWalletAsync(fromAvatarId, toAvatarId, amount).Result;
        }

        public async Task<OASISResult<ITransactionResponse>> SendTransactionByDefaultWalletAsync(Guid fromAvatarId, Guid toAvatarId, decimal amount)
        {
            // Use the default wallet for the avatar
            return await SendTransactionByIdAsync(fromAvatarId, toAvatarId, amount);
        }



        public OASISResult<IWeb3NFTTransactionResponse> SendNFT(ISendWeb3NFTRequest transation)
        {
            return SendNFTAsync(transation).Result;
        }

        public async Task<OASISResult<IWeb3NFTTransactionResponse>> SendNFTAsync(ISendWeb3NFTRequest transation)
        {
            var result = new OASISResult<IWeb3NFTTransactionResponse>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Holo provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Create Holochain NFT transfer transaction
                var nftTransfer = new
                {
                    from = transation.FromWalletAddress,
                    to = transation.ToWalletAddress,
                    nftId = transation.TokenId,
                    timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
                };

                var json = JsonSerializer.Serialize(nftTransfer);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("/nft-transfers", content);
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var responseData = JsonSerializer.Deserialize<Dictionary<string, object>>(responseContent);
                    
                    var nftTransactionResponse = new Web3NFTTransactionResponse
                    {
                        TransactionResult = responseData?.GetValueOrDefault("hash")?.ToString() ?? "nft-transfer-completed",
                    };
                    
                    result.Result = nftTransactionResponse;
                    result.IsError = false;
                    result.Message = "NFT transfer sent successfully via Holochain";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to send NFT transfer via Holochain: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error sending NFT transfer via Holochain: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<IWeb3NFTTransactionResponse> MintNFT(IMintWeb3NFTRequest transation)
        {
            return MintNFTAsync(transation).Result;
        }

        public async Task<OASISResult<IWeb3NFTTransactionResponse>> MintNFTAsync(IMintWeb3NFTRequest transation)
        {
            var result = new OASISResult<IWeb3NFTTransactionResponse>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Holo provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Create Holochain NFT mint transaction
                var nftMint = new
                {
                    to = transation.SendToAddressAfterMinting,
                    nftId = transation.Title, // Using Title as identifier
                    metadata = transation.MetaData,
                    timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
                };

                var json = JsonSerializer.Serialize(nftMint);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("/nft-mints", content);
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var responseData = JsonSerializer.Deserialize<Dictionary<string, object>>(responseContent);
                    
                    var nftTransactionResponse = new Web3NFTTransactionResponse
                    {
                        TransactionResult = responseData?.GetValueOrDefault("hash")?.ToString() ?? "nft-mint-completed",
                    };
                    
                    result.Result = nftTransactionResponse;
                    result.IsError = false;
                    result.Message = "NFT minted successfully via Holochain";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to mint NFT via Holochain: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error minting NFT via Holochain: {ex.Message}", ex);
            }
            return result;
        }



    }
}
