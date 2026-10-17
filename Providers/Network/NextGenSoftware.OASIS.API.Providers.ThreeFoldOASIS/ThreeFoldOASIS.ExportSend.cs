using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Helpers;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Linq;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets.Response;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Response;
using System.Threading;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Helpers;

namespace NextGenSoftware.OASIS.API.Providers.ThreeFoldOASIS
{
    public partial class ThreeFoldOASIS
    {
        public OASISResult<ITransactionResponse> SendTokenByEmail(string fromAvatarEmail, string toAvatarEmail, decimal amount)
        {
            return SendTokenByEmailAsync(fromAvatarEmail, toAvatarEmail, amount).Result;
        }

        public async Task<OASISResult<ITransactionResponse>> SendTokenByEmailAsync(string fromAvatarEmail, string toAvatarEmail, decimal amount, string token)
        {
            var result = new OASISResult<ITransactionResponse>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate ThreeFold provider: {activateResult.Message}");
                        return result;
                    }
                }

                var transactionRequest = new
                {
                    fromAvatarEmail = fromAvatarEmail,
                    toAvatarEmail = toAvatarEmail,
                    amount = amount,
                    token = token
                };

                var jsonContent = JsonSerializer.Serialize(transactionRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync($"{_apiBaseUrl}/transactions/send-by-email-token", content);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var transactionResponse = JsonSerializer.Deserialize<TransactionResponse>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (transactionResponse != null)
                    {
                        result.Result = transactionResponse;
                        result.IsError = false;
                        result.Message = "Transaction sent successfully by email with token to ThreeFold Grid";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref result, "Failed to deserialize transaction response from ThreeFold Grid API");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"ThreeFold Grid API error: {response.StatusCode} - {response.ReasonPhrase}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error sending transaction by email with token to ThreeFold Grid: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<ITransactionResponse> SendTokenByEmail(string fromAvatarEmail, string toAvatarEmail, decimal amount, string token)
        {
            return SendTokenByEmailAsync(fromAvatarEmail, toAvatarEmail, amount, token).Result;
        }

        // IOASISBlockchainStorageProvider interface methods
        public OASISResult<ITransactionResponse> SendToken(string fromWalletAddress, string toWalletAddress, decimal amount, string memoText)
        {
            return SendTokenAsync(fromWalletAddress, toWalletAddress, amount, memoText).Result;
        }

        public async Task<OASISResult<ITransactionResponse>> SendTokenAsync(string fromWalletAddress, string toWalletAddress, decimal amount, string memoText)
        {
            var result = new OASISResult<ITransactionResponse>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate ThreeFold provider: {activateResult.Message}");
                        return result;
                    }
                }

                // ThreeFold transaction implementation
                var transactionRequest = new
                {
                    from = fromWalletAddress,
                    to = toWalletAddress,
                    amount = amount,
                    memo = memoText
                };

                var jsonContent = JsonSerializer.Serialize(transactionRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync($"{_apiBaseUrl}/transactions", content);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var transactionResponse = JsonSerializer.Deserialize<TransactionResponse>(responseContent);
                    result.Result = transactionResponse;
                    result.IsError = false;
                    result.Message = "Transaction sent successfully";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Transaction failed: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error sending transaction: {ex.Message}", ex);
            }
            return result;
        }

        public async Task<OASISResult<ITransactionResponse>> SendTokenByDefaultWalletAsync(Guid fromAvatarId, Guid toAvatarId, decimal amount)
        {
            var result = new OASISResult<ITransactionResponse>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate ThreeFold provider: {activateResult.Message}");
                        return result;
                    }
                }

                var transactionRequest = new
                {
                    fromAvatarId = fromAvatarId,
                    toAvatarId = toAvatarId,
                    amount = amount
                };

                var jsonContent = JsonSerializer.Serialize(transactionRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync($"{_apiBaseUrl}/transactions/send-by-default-wallet", content);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var transactionResponse = JsonSerializer.Deserialize<TransactionResponse>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (transactionResponse != null)
                    {
                        result.Result = transactionResponse;
                        result.IsError = false;
                        result.Message = "Transaction sent successfully by default wallet to ThreeFold Grid";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref result, "Failed to deserialize transaction response from ThreeFold Grid API");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"ThreeFold Grid API error: {response.StatusCode} - {response.ReasonPhrase}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error sending transaction by default wallet to ThreeFold Grid: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<ITransactionResponse> SendTokenByDefaultWallet(Guid fromAvatarId, Guid toAvatarId, decimal amount)
        {
            return SendTokenByDefaultWalletAsync(fromAvatarId, toAvatarId, amount).Result;
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
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate ThreeFold provider: {activateResult.Message}");
                        return result;
                    }
                }

                if (transation == null)
                {
                    OASISErrorHandling.HandleError(ref result, "NFT transaction request cannot be null");
                    return result;
                }

                var nftRequest = new
                {
                    fromAddress = transation.FromWalletAddress,
                    toAddress = transation.ToWalletAddress,
                    nftId = transation.TokenId,
                    tokenId = transation.TokenId,
                    contractAddress = transation.TokenAddress
                };

                var jsonContent = JsonSerializer.Serialize(nftRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync($"{_apiBaseUrl}/nft/send", content);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var nftResponse = JsonSerializer.Deserialize<IWeb3NFTTransactionResponse>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (nftResponse != null)
                    {
                        result.Result = nftResponse;
                        result.IsError = false;
                        result.Message = "NFT sent successfully to ThreeFold Grid";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref result, "Failed to deserialize NFT transaction response from ThreeFold Grid API");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"ThreeFold Grid API error: {response.StatusCode} - {response.ReasonPhrase}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error sending NFT to ThreeFold Grid: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<IWeb3NFTTransactionResponse> SendNFT(ISendWeb3NFTRequest transation)
        {
            return SendNFTAsync(transation).Result;
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
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate ThreeFold provider: {activateResult.Message}");
                        return result;
                    }
                }

                if (transation == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Mint NFT transaction request cannot be null");
                    return result;
                }

                var mintRequest = new
                {
                    toAddress = transation.SendToAddressAfterMinting,
                    tokenId = transation.Title,
                    contractAddress = transation.OnChainProvider.ToString(),
                    metadata = transation.MetaData,
                    name = transation.Title,
                    description = transation.Description,
                    imageUrl = transation.ImageUrl
                };

                var jsonContent = JsonSerializer.Serialize(mintRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync($"{_apiBaseUrl}/nft/mint", content);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var nftResponse = JsonSerializer.Deserialize<IWeb3NFTTransactionResponse>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (nftResponse != null)
                    {
                        result.Result = nftResponse;
                        result.IsError = false;
                        result.Message = "NFT minted successfully on ThreeFold Grid";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref result, "Failed to deserialize NFT mint response from ThreeFold Grid API");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"ThreeFold Grid API error: {response.StatusCode} - {response.ReasonPhrase}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error minting NFT on ThreeFold Grid: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<IWeb3NFTTransactionResponse> MintNFT(IMintWeb3NFTRequest transation)
        {
            return MintNFTAsync(transation).Result;
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
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate ThreeFold provider: {activateResult.Message}");
                        return result;
                    }
                }

                if (string.IsNullOrEmpty(nftTokenAddress))
                {
                    OASISErrorHandling.HandleError(ref result, "NFT token address cannot be null or empty");
                    return result;
                }

                var response = await _httpClient.GetAsync($"{_apiBaseUrl}/nft/on-chain/{Uri.EscapeDataString(nftTokenAddress)}");

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var nft = JsonSerializer.Deserialize<Web3NFT>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (nft != null)
                    {
                        result.Result = nft;
                        result.IsError = false;
                        result.Message = "NFT data loaded successfully from ThreeFold Grid";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref result, "Failed to deserialize NFT data from ThreeFold Grid API");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"ThreeFold Grid API error: {response.StatusCode} - {response.ReasonPhrase}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading NFT data from ThreeFold Grid: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<IWeb3NFT> LoadOnChainNFTData(string nftTokenAddress)
        {
            return LoadOnChainNFTDataAsync(nftTokenAddress).Result;
        }

        public async Task<OASISResult<IWeb3NFTTransactionResponse>> BurnNFTAsync(IBurnWeb3NFTRequest request)
        {
            var result = new OASISResult<IWeb3NFTTransactionResponse>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate ThreeFold provider: {activateResult.Message}");
                        return result;
                    }
                }

                var burnRequest = new
                {
                    nftTokenAddress = request.NFTTokenAddress,
                    burntByAvatarId = request.BurntByAvatarId,
                    web3NFTId = request.Web3NFTId
                };

                var jsonContent = JsonSerializer.Serialize(burnRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync($"{_apiBaseUrl}/nft/burn", content);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var nftResponse = JsonSerializer.Deserialize<Web3NFTTransactionResponse>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (nftResponse != null)
                    {
                        result.Result = nftResponse;
                        result.IsError = false;
                        result.Message = "NFT burned successfully on ThreeFold Grid";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref result, "Failed to deserialize NFT burn response from ThreeFold Grid API");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"ThreeFold Grid API error: {response.StatusCode} - {response.ReasonPhrase}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error burning NFT on ThreeFold Grid: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<IWeb3NFTTransactionResponse> BurnNFT(IBurnWeb3NFTRequest request)
        {
            return BurnNFTAsync(request).Result;
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
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate ThreeFold provider: {activateResult.Message}");
                        return result;
                    }
                }

                var mintRequest = new
                {
                    tokenAddress = request.Symbol ?? string.Empty,
                    mintedByAvatarId = request.MintedByAvatarId,
                    amount = 1m,
                    symbol = request.Symbol ?? "TFT"
                };

                var jsonContent = JsonSerializer.Serialize(mintRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync($"{_apiBaseUrl}/tokens/mint", content);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var txResponse = JsonSerializer.Deserialize<TransactionResponse>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (txResponse != null)
                    {
                        result.Result = txResponse;
                        result.IsError = false;
                        result.Message = "Token minted successfully on ThreeFold Grid";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref result, "Failed to deserialize token mint response from ThreeFold Grid API");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"ThreeFold Grid API error: {response.StatusCode} - {response.ReasonPhrase}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error minting token on ThreeFold Grid: {ex.Message}", ex);
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
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate ThreeFold provider: {activateResult.Message}");
                        return result;
                    }
                }

                var burnRequest = new
                {
                    tokenAddress = request.TokenAddress,
                    burntByAvatarId = request.BurntByAvatarId,
                    amount = 1m
                };

                var jsonContent = JsonSerializer.Serialize(burnRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync($"{_apiBaseUrl}/tokens/burn", content);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var txResponse = JsonSerializer.Deserialize<TransactionResponse>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (txResponse != null)
                    {
                        result.Result = txResponse;
                        result.IsError = false;
                        result.Message = "Token burned successfully on ThreeFold Grid";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref result, "Failed to deserialize token burn response from ThreeFold Grid API");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"ThreeFold Grid API error: {response.StatusCode} - {response.ReasonPhrase}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error burning token on ThreeFold Grid: {ex.Message}", ex);
            }
            return result;
        }

    }
}
