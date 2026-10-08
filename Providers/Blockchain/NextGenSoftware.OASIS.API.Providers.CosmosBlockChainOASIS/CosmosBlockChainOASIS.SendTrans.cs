using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
// using Microsoft.Azure.Cosmos;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets.Response;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Managers;

namespace NextGenSoftware.OASIS.API.Providers.CosmosBlockChainOASIS
{
    public partial class CosmosBlockChainOASIS
    {
        public OASISResult<IWeb3NFTTransactionResponse> SendNFT(ISendWeb3NFTRequest transation)
        {
            return SendNFTAsync(transation).Result;
        }

        public async Task<OASISResult<IWeb3NFTTransactionResponse>> SendNFTAsync(ISendWeb3NFTRequest transation)
        {
            var response = new OASISResult<IWeb3NFTTransactionResponse>(new Web3NFTTransactionResponse());
            try
            {
                if (!_isActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref response, $"Failed to activate Cosmos Blockchain provider: {activateResult.Message}");
                        return response;
                    }
                }

                // Create Cosmos NFT transfer transaction using REST API
                var txRequest = new
                {
                    tx = new
                    {
                        body = new
                        {
                            messages = new[]
                            {
                                new
                                {
                                    type = "cosmos.nft.v1beta1.MsgTransfer",
                                    value = new
                                    {
                                        class_id = transation.TokenAddress,
                                        id = transation.TokenId,
                                        sender = transation.FromWalletAddress,
                                        receiver = transation.ToWalletAddress
                                    }
                                }
                            },
                            memo = "OASIS NFT transfer",
                            timeout_height = "0"
                        },
                        auth_info = new
                        {
                            signer_infos = new[]
                            {
                                new
                                {
                                    public_key = new
                                    {
                                        type = "cosmos.crypto.secp256k1.PubKey",
                                        value = "..." // This would be the actual public key
                                    },
                                    mode_info = new
                                    {
                                        single = new
                                        {
                                            mode = "SIGN_MODE_DIRECT"
                                        }
                                    },
                                    sequence = "0"
                                }
                            },
                            fee = new
                            {
                                amount = new[]
                                {
                                    new
                                    {
                                        denom = "uatom",
                                        amount = "5000" // Gas fee
                                    }
                                },
                                gas_limit = "200000"
                            }
                        },
                        signatures = new[] { "..." } // This would be the actual signature
                    },
                    mode = "BROADCAST_MODE_SYNC"
                };

                var jsonContent = JsonSerializer.Serialize(txRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var httpResponse = await _httpClient.PostAsync("/cosmos/tx/v1beta1/txs", content);

                if (httpResponse.IsSuccessStatusCode)
                {
                    var responseContent = await httpResponse.Content.ReadAsStringAsync();
                    var txResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);
                    
                    var nftTransactionResponse = new Web3NFTTransactionResponse();
                    if (txResponse.TryGetProperty("tx_response", out var txResp))
                    {
                        if (txResp.TryGetProperty("txhash", out var hash))
                            nftTransactionResponse.TransactionResult = hash.GetString();
                    }

                    response.Result = nftTransactionResponse;
                    response.IsError = false;
                    response.Message = "NFT transfer sent to Cosmos blockchain successfully";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to send NFT to Cosmos: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error sending NFT to Cosmos: {ex.Message}");
            }
            return response;
        }

        public OASISResult<IWeb3NFTTransactionResponse> MintNFT(IMintWeb3NFTRequest transation)
        {
            return MintNFTAsync(transation).Result;
        }

    }
}
