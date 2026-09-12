using System;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
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
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Response;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Objects.Avatar;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Response;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets.Response;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using System.Security.Cryptography;

namespace NextGenSoftware.OASIS.API.Providers.NEAROASIS
{
    public partial class NEAROASIS
    {
        public OASISResult<IEnumerable<IAvatar>> GetAvatarsNearMe(long x, long y, int radius)
        {
            var response = new OASISResult<IEnumerable<IAvatar>>();
            try
            {
                if (!_isActivated)
                {
                    var activateResult = ActivateProviderAsync().GetAwaiter().GetResult();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref response, $"Failed to activate NEAR provider: {activateResult.Message}");
                        return response;
                    }
                }

                // Get avatars near me from NEAR blockchain
                var rpcRequest = new
                {
                    jsonrpc = "2.0",
                    id = "dontcare",
                    method = "query",
                    @params = new
                    {
                        request_type = "call_function",
                        finality = "final",
                        account_id = "oasis.near",
                        method_name = "get_avatars_near_me",
                        args_base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{{\"x\":{x},\"y\":{y},\"radius\":{radius}}}"))
                    }
                };

                var jsonContent = JsonSerializer.Serialize(rpcRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var httpResponse = _httpClient.PostAsync("", content).Result;

                if (httpResponse.IsSuccessStatusCode)
                {
                    var jsonResponse = httpResponse.Content.ReadAsStringAsync().Result;
                    var result = JsonSerializer.Deserialize<JsonElement>(jsonResponse);
                    
                    if (result.TryGetProperty("result", out var resultElement))
                    {
                        var avatars = new List<IAvatar>();
                        if (resultElement.TryGetProperty("result", out var dataElement))
                        {
                            var dataString = dataElement.GetString();
                            if (!string.IsNullOrEmpty(dataString))
                            {
                                var avatarData = JsonSerializer.Deserialize<List<JsonElement>>(dataString);
                                foreach (var avatarJson in avatarData)
                                {
                                    var avatar = new Avatar
                                    {
                                        Id = Guid.Parse(avatarJson.GetProperty("id").GetString() ?? Guid.Empty.ToString()),
                                        Username = avatarJson.GetProperty("username").GetString() ?? "",
                                        Email = avatarJson.GetProperty("email").GetString() ?? "",
                                        Version = 0
                                    };
                                    avatars.Add(avatar);
                                }
                            }
                        }
                        response.Result = avatars;
                        response.IsError = false;
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref response, "No avatars found");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to get avatars from NEAR: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error getting avatars from NEAR: {ex.Message}");
            }
            return response;
        }

        public OASISResult<IEnumerable<IHolon>> GetHolonsNearMe(long x, long y, int radius, HolonType holonType)
        {
            var response = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!_isActivated)
                {
                    var activateResult = ActivateProviderAsync().GetAwaiter().GetResult();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref response, $"Failed to activate NEAR provider: {activateResult.Message}");
                        return response;
                    }
                }

                // Get holons near me from NEAR blockchain
                var rpcRequest = new
                {
                    jsonrpc = "2.0",
                    id = "dontcare",
                    method = "query",
                    @params = new
                    {
                        request_type = "call_function",
                        finality = "final",
                        account_id = "oasis.near",
                        method_name = "get_holons_near_me",
                        args_base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{{\"x\":{x},\"y\":{y},\"radius\":{radius},\"holon_type\":\"{holonType}\"}}"))
                    }
                };

                var jsonContent = JsonSerializer.Serialize(rpcRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var httpResponse = _httpClient.PostAsync("", content).Result;

                if (httpResponse.IsSuccessStatusCode)
                {
                    var jsonResponse = httpResponse.Content.ReadAsStringAsync().Result;
                    var result = JsonSerializer.Deserialize<JsonElement>(jsonResponse);
                    
                    if (result.TryGetProperty("result", out var resultElement))
                    {
                        var holons = new List<IHolon>();
                        if (resultElement.TryGetProperty("result", out var dataElement))
                        {
                            var dataString = dataElement.GetString();
                            if (!string.IsNullOrEmpty(dataString))
                            {
                                var holonData = JsonSerializer.Deserialize<List<JsonElement>>(dataString);
                                foreach (var holonJson in holonData)
                                {
                                    var holon = new Holon
                                    {
                                        Id = Guid.Parse(holonJson.GetProperty("id").GetString() ?? Guid.Empty.ToString()),
                                        Name = holonJson.GetProperty("name").GetString() ?? "",
                                        Description = holonJson.GetProperty("description").GetString() ?? "",
                                        Version = 0
                                    };
                                    holons.Add(holon);
                                }
                            }
                        }
                        response.Result = holons;
                        response.IsError = false;
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref response, "No holons found");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to get holons from NEAR: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error getting holons from NEAR: {ex.Message}");
            }
            return response;
        }

        // Missing IOASISNFTProvider methods

        public OASISResult<IWeb3NFTTransactionResponse> SendNFT(ISendWeb3NFTRequest request)
        {
            return SendNFTAsync(request).Result;
        }

        public async Task<OASISResult<IWeb3NFTTransactionResponse>> SendNFTAsync(ISendWeb3NFTRequest request)
        {
            var response = new OASISResult<IWeb3NFTTransactionResponse>();
            try
            {
                if (!_isActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref response, $"Failed to activate NEAR provider: {activateResult.Message}");
                        return response;
                    }
                }

                // Send NFT on NEAR blockchain
                var rpcRequest = new
                {
                    jsonrpc = "2.0",
                    id = "dontcare",
                    method = "query",
                    @params = new
                    {
                        request_type = "call_function",
                        finality = "final",
                        account_id = "oasis.near",
                        method_name = "send_nft",
                        args_base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(request)))
                    }
                };

                var jsonContent = JsonSerializer.Serialize(rpcRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var httpResponse = await _httpClient.PostAsync("", content);

                if (httpResponse.IsSuccessStatusCode)
                {
                    var jsonResponse = await httpResponse.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<JsonElement>(jsonResponse);
                    
                    if (result.TryGetProperty("result", out var resultElement))
                    {
                        var nftTransactionResponse = new Web3NFTTransactionResponse
                        {
                            TransactionResult = resultElement.GetProperty("transaction_result").GetString() ?? "",
                            Web3NFT = new Web3NFT
                            {
                                Id = Guid.Parse(resultElement.GetProperty("nft_id").GetString() ?? Guid.Empty.ToString()),
                                Title = resultElement.GetProperty("nft_name").GetString() ?? "",
                                Description = resultElement.GetProperty("nft_description").GetString() ?? ""
                            },
                            SendNFTTransactionResult = resultElement.GetProperty("message").GetString() ?? ""
                        };
                        response.Result = nftTransactionResponse;
                        response.IsError = false;
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref response, "NFT send failed");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to send NFT on NEAR: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error sending NFT on NEAR: {ex.Message}");
            }
            return response;
        }

        public OASISResult<IWeb3NFTTransactionResponse> MintNFT(IMintWeb3NFTRequest request)
        {
            return MintNFTAsync(request).Result;
        }

        public async Task<OASISResult<IWeb3NFTTransactionResponse>> MintNFTAsync(IMintWeb3NFTRequest request)
        {
            var response = new OASISResult<IWeb3NFTTransactionResponse>();
            try
            {
                if (!_isActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref response, $"Failed to activate NEAR provider: {activateResult.Message}");
                        return response;
                    }
                }

                // Mint NFT on NEAR blockchain
                var rpcRequest = new
                {
                    jsonrpc = "2.0",
                    id = "dontcare",
                    method = "query",
                    @params = new
                    {
                        request_type = "call_function",
                        finality = "final",
                        account_id = "oasis.near",
                        method_name = "mint_nft",
                        args_base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(request)))
                    }
                };

                var jsonContent = JsonSerializer.Serialize(rpcRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var httpResponse = await _httpClient.PostAsync("", content);

                if (httpResponse.IsSuccessStatusCode)
                {
                    var jsonResponse = await httpResponse.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<JsonElement>(jsonResponse);
                    
                    if (result.TryGetProperty("result", out var resultElement))
                    {
                        var nftTransactionResponse = new Web3NFTTransactionResponse
                        {
                            TransactionResult = resultElement.GetProperty("transaction_result").GetString() ?? "",
                            Web3NFT = new Web3NFT
                            {
                                Id = Guid.Parse(resultElement.GetProperty("nft_id").GetString() ?? Guid.Empty.ToString()),
                                Title = resultElement.GetProperty("nft_name").GetString() ?? "",
                                Description = resultElement.GetProperty("nft_description").GetString() ?? ""
                            },
                            SendNFTTransactionResult = resultElement.GetProperty("message").GetString() ?? ""
                        };
                        response.Result = nftTransactionResponse;
                        response.IsError = false;
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref response, "NFT mint failed");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to mint NFT on NEAR: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error: {ex.Message}");
            }
            return response;
        }

        // Missing abstract method implementations
        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(string key, string value, HolonType holonType, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int version = 0, bool continueOnError = true, bool loadChildrenRecursiveDepth = true, int loadChildrenRecursiveDepthInt = 0)
        {
            var response = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!_isActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref response, $"Failed to activate NEAR provider: {activateResult.Message}");
                        return response;
                    }
                }

                // Query holons by metadata from NEAR smart contract
                var rpcRequest = new
                {
                    jsonrpc = "2.0",
                    id = "dontcare",
                    method = "query",
                    @params = new
                    {
                        request_type = "call_function",
                        finality = "final",
                        account_id = "oasis.near",
                        method_name = "get_holons_by_metadata",
                        args_base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{{\"key\":\"{key}\",\"value\":\"{value}\",\"holon_type\":\"{holonType}\"}}"))
                    }
                };

                var jsonContent = JsonSerializer.Serialize(rpcRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var httpResponse = await _httpClient.PostAsync("", content);

                if (httpResponse.IsSuccessStatusCode)
                {
                    var jsonResponse = await httpResponse.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<JsonElement>(jsonResponse);
                    
                    if (result.TryGetProperty("result", out var resultElement))
                    {
                        var holons = new List<IHolon>();
                        if (resultElement.TryGetProperty("result", out var dataElement))
                        {
                            var dataString = dataElement.GetString();
                            if (!string.IsNullOrEmpty(dataString))
                            {
                                var holonData = JsonSerializer.Deserialize<List<JsonElement>>(dataString);
                                foreach (var holonJson in holonData)
                                {
                                    var holon = new Holon
                                    {
                                        Id = Guid.Parse(holonJson.GetProperty("id").GetString() ?? Guid.Empty.ToString()),
                                        Name = holonJson.GetProperty("name").GetString() ?? "",
                                        Description = holonJson.GetProperty("description").GetString() ?? "",
                                        Version = version
                                    };
                                    holons.Add(holon);
                                }
                            }
                        }
                        response.Result = holons;
                        response.IsError = false;
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref response, "No holons found");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to load holons from NEAR: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error loading holons from NEAR: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(string key, string value, HolonType holonType, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int version = 0, bool continueOnError = true, bool loadChildrenRecursiveDepth = true, int loadChildrenRecursiveDepthInt = 0)
        {
            return LoadHolonsByMetaDataAsync(key, value, holonType, loadChildren, recursive, maxChildDepth, version, continueOnError, loadChildrenRecursiveDepth, loadChildrenRecursiveDepthInt).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(Dictionary<string, string> metaData, MetaKeyValuePairMatchMode matchMode, HolonType holonType, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int version = 0, bool continueOnError = true, bool loadChildrenRecursiveDepth = true, int loadChildrenRecursiveDepthInt = 0)
        {
            var response = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!_isActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref response, $"Failed to activate NEAR provider: {activateResult.Message}");
                        return response;
                    }
                }

                // Query holons by metadata dictionary from NEAR smart contract
                var rpcRequest = new
                {
                    jsonrpc = "2.0",
                    id = "dontcare",
                    method = "query",
                    @params = new
                    {
                        request_type = "call_function",
                        finality = "final",
                        account_id = "oasis.near",
                        method_name = "get_holons_by_metadata_dict",
                        args_base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{{\"metadata\":{JsonSerializer.Serialize(metaData)},\"match_mode\":\"{matchMode}\",\"holon_type\":\"{holonType}\"}}"))
                    }
                };

                var jsonContent = JsonSerializer.Serialize(rpcRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var httpResponse = await _httpClient.PostAsync("", content);

                if (httpResponse.IsSuccessStatusCode)
                {
                    var jsonResponse = await httpResponse.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<JsonElement>(jsonResponse);
                    
                    if (result.TryGetProperty("result", out var resultElement))
                    {
                        var holons = new List<IHolon>();
                        if (resultElement.TryGetProperty("result", out var dataElement))
                        {
                            var dataString = dataElement.GetString();
                            if (!string.IsNullOrEmpty(dataString))
                            {
                                var holonData = JsonSerializer.Deserialize<List<JsonElement>>(dataString);
                                foreach (var holonJson in holonData)
                                {
                                    var holon = new Holon
                                    {
                                        Id = Guid.Parse(holonJson.GetProperty("id").GetString() ?? Guid.Empty.ToString()),
                                        Name = holonJson.GetProperty("name").GetString() ?? "",
                                        Description = holonJson.GetProperty("description").GetString() ?? "",
                                        Version = version
                                    };
                                    holons.Add(holon);
                                }
                            }
                        }
                        response.Result = holons;
                        response.IsError = false;
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref response, "No holons found");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to load holons from NEAR: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error loading holons from NEAR: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(Dictionary<string, string> metaData, MetaKeyValuePairMatchMode matchMode, HolonType holonType, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int version = 0, bool continueOnError = true, bool loadChildrenRecursiveDepth = true, int loadChildrenRecursiveDepthInt = 0)
        {
            return LoadHolonsByMetaDataAsync(metaData, matchMode, holonType, loadChildren, recursive, maxChildDepth, version, continueOnError, loadChildrenRecursiveDepth, loadChildrenRecursiveDepthInt).Result;
        }



        public OASISResult<IEnumerable<IPlayer>> GetPlayersNearMe()
        {
            var response = new OASISResult<IEnumerable<IPlayer>>();

            try
            {
                if (!_isActivated)
                {
                    var activateResult = ActivateProviderAsync().GetAwaiter().GetResult();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref response, $"Failed to activate NEAR provider: {activateResult.Message}");
                        return response;
                    }
                }

                // Get players near me from NEAR blockchain
                var queryUrl = "/accounts/nearby";

                var httpResponse = _httpClient.GetAsync(queryUrl).Result;
                if (httpResponse.IsSuccessStatusCode)
                {
                    var content = httpResponse.Content.ReadAsStringAsync().Result;
                    // Parse NEAR JSON and create Player collection
                    var players = new List<IPlayer>();
                    var avatar = ParseNEARToAvatar(content);
                    if (avatar != null)
                    {
                        players.Add(avatar as IPlayer);
                        response.Result = players;
                        response.Message = "Players loaded from NEAR successfully";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref response, "Failed to parse NEAR JSON response");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to get players near me from NEAR blockchain: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error getting players near me from NEAR: {ex.Message}");
            }

            return response;
        }




    }
}
