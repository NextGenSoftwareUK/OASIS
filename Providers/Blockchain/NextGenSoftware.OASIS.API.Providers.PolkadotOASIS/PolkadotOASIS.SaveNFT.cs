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



        OASISResult<IEnumerable<IAvatar>> IOASISNETProvider.GetAvatarsNearMe(long geoLat, long geoLong, int radiusInMeters)
            => ReadNearbyRecordsAsync(geoLat,geoLong,radiusInMeters,()=>LoadAllAvatarsAsync()).GetAwaiter().GetResult();

        OASISResult<IEnumerable<IHolon>> IOASISNETProvider.GetHolonsNearMe(long geoLat, long geoLong, int radiusInMeters, HolonType holonType)
            => ReadNearbyRecordsAsync(geoLat,geoLong,radiusInMeters,
                ()=>LoadAllHolonsAsync(HolonType.All,loadChildren:false),holonType).GetAwaiter().GetResult();

        private async Task<OASISResult<IEnumerable<T>>> ReadNearbyRecordsAsync<T>(long latitude,long longitude,int radius,
            Func<Task<OASISResult<IEnumerable<T>>>> load,HolonType type=HolonType.All) where T:IHolonBase
        {
            var result=new OASISResult<IEnumerable<T>>();
            try {
                var query=new NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.StoredRecordLocationQuery(latitude,longitude,radius);
                if(!System.Enum.IsDefined(typeof(HolonType),type))throw new ArgumentOutOfRangeException(nameof(type));
                if(!_isActivated) {
                    var activation=await ActivateProviderAsync();
                    if(activation.IsError)return OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<bool,IEnumerable<T>>(activation);
                }
                var loaded=await load();
                if(loaded.IsError)return loaded;
                result=OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<IEnumerable<T>,IEnumerable<T>>(loaded);
                result.Result=query.Select(loaded.Result,row=>row.IsActive&&!row.IsDeleted&&(type==HolonType.All||row.HolonType==type));
                result.IsLoaded=true;
            } catch(Exception cause) {
                result.Result=null;result.IsLoaded=false;
                OASISErrorHandling.HandleError(ref result,cause.Message,cause);
            }
            return result;
        }

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
                        OASISErrorHandling.HandleError(ref response, $"Failed to activate Polkadot provider: {activateResult.Message}");
                        return response;
                    }
                }
                // Polkadot uses Substrate framework for NFTs
                // Use Polkadot.js SDK or Substrate API for NFT transfers
                OASISErrorHandling.HandleError(ref response, "SendNFTAsync requires Polkadot.js SDK or Substrate API integration");
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error in SendNFTAsync: {ex.Message}");
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
                        OASISErrorHandling.HandleError(ref response, $"Failed to activate Polkadot provider: {activateResult.Message}");
                        return response;
                    }
                }
                // Polkadot uses Substrate framework for NFTs
                // Use Polkadot.js SDK or Substrate API for NFT minting
                OASISErrorHandling.HandleError(ref response, "MintNFTAsync requires Polkadot.js SDK or Substrate API integration");
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error in MintNFTAsync: {ex.Message}");
            }
            return response;
        }

        public OASISResult<IWeb3NFTTransactionResponse> BurnNFT(IBurnWeb3NFTRequest request)
        {
            return BurnNFTAsync(request).Result;
        }

        public async Task<OASISResult<IWeb3NFTTransactionResponse>> BurnNFTAsync(IBurnWeb3NFTRequest request)
        {
            var response = new OASISResult<IWeb3NFTTransactionResponse>();
            try
            {
                if (!_isActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref response, $"Failed to activate Polkadot provider: {activateResult.Message}");
                        return response;
                    }
                }
                // Polkadot uses Substrate framework for NFTs
                // Use Polkadot.js SDK or Substrate API for NFT burning
                OASISErrorHandling.HandleError(ref response, "BurnNFTAsync requires Polkadot.js SDK or Substrate API integration");
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error in BurnNFTAsync: {ex.Message}");
            }
            return response;
        }

        public OASISResult<IWeb3NFT> LoadOnChainNFTData(string nftTokenAddress)
        {
            return LoadOnChainNFTDataAsync(nftTokenAddress).Result;
        }

        public async Task<OASISResult<IWeb3NFT>> LoadOnChainNFTDataAsync(string nftTokenAddress)
        {
            var response = new OASISResult<IWeb3NFT>();
            try
            {
                if (!_isActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref response, $"Failed to activate Polkadot provider: {activateResult.Message}");
                        return response;
                    }
                }

                // Query NFT from Polkadot smart contract using state_call
                var rpcRequest = new
                {
                    jsonrpc = "2.0",
                    id = 1,
                    method = "state_call",
                    @params = new[]
                    {
                        "Oasis_getNFT",
                        Convert.ToBase64String(Encoding.UTF8.GetBytes($"{{\"token_address\":\"{nftTokenAddress}\"}}")),
                        null
                    }
                };

                var jsonContent = JsonSerializer.Serialize(rpcRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var httpResponse = await _httpClient.PostAsync("", content);

                if (httpResponse.IsSuccessStatusCode)
                {
                    var responseContent = await httpResponse.Content.ReadAsStringAsync();
                    var rpcResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);

                    if (rpcResponse.TryGetProperty("result", out var result))
                    {
                        var nftJson = Convert.FromBase64String(result.GetString());
                        var nftData = JsonSerializer.Deserialize<JsonElement>(Encoding.UTF8.GetString(nftJson));
                        
                        var imgStr = nftData.TryGetProperty("image", out var imgEl) ? imgEl.GetString() : null;
                        var nft = new Web3NFT
                        {
                            NFTTokenAddress = nftTokenAddress,
                            Title = nftData.TryGetProperty("name", out var name) ? name.GetString() : "",
                            Description = nftData.TryGetProperty("description", out var desc) ? desc.GetString() : "",
                            ImageUrl = imgStr ?? "",
                            Image = !string.IsNullOrEmpty(imgStr) ? System.Text.Encoding.UTF8.GetBytes(imgStr) : null
                        };
                        if (nft.MetaData == null) nft.MetaData = new Dictionary<string, string>();
                        if (nftData.TryGetProperty("external_url", out var extUrl))
                            nft.MetaData["external_url"] = extUrl.GetString() ?? "";

                        response.Result = nft;
                        response.IsError = false;
                        response.Message = "NFT loaded successfully from Polkadot blockchain";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref response, "NFT not found on Polkadot blockchain");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to load NFT from Polkadot: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error in LoadOnChainNFTDataAsync: {ex.Message}");
            }
            return response;
        }
    }
}
