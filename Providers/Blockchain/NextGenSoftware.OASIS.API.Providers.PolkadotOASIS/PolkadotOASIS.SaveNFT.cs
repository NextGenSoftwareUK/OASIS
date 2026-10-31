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

        public Task<OASISResult<IWeb3NFTTransactionResponse>> SendNFTAsync(ISendWeb3NFTRequest request)
            =>SendOwnedNftAsync(request);

        public OASISResult<IWeb3NFTTransactionResponse> MintNFT(IMintWeb3NFTRequest request)
        {
            return MintNFTAsync(request).Result;
        }

        public Task<OASISResult<IWeb3NFTTransactionResponse>> MintNFTAsync(IMintWeb3NFTRequest request)
            =>MintOwnedNftAsync(request);

        public OASISResult<IWeb3NFTTransactionResponse> BurnNFT(IBurnWeb3NFTRequest request)
        {
            return BurnNFTAsync(request).Result;
        }

        public Task<OASISResult<IWeb3NFTTransactionResponse>> BurnNFTAsync(IBurnWeb3NFTRequest request)
            =>BurnOwnedNftAsync(request);

        public OASISResult<IWeb3NFT> LoadOnChainNFTData(string nftTokenAddress)
        {
            return LoadOnChainNFTDataAsync(nftTokenAddress).Result;
        }

        public async Task<OASISResult<IWeb3NFT>> LoadOnChainNFTDataAsync(string nftTokenAddress)
        {
            var response=new OASISResult<IWeb3NFT>();
            try {
                if(!IsGenesisHash(nftTokenAddress))throw new ArgumentException("A canonical32byte PSP34 collection contract address is required.",nameof(nftTokenAddress));
                if(_nftConfiguration==null)throw new InvalidOperationException("Configure the reviewed PSP34 code hash and explicit query origin before NFT reads.");
                if(!_isActivated) {
                    var activation=await ActivateProviderAsync();
                    if(activation.IsError)return OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<bool,IWeb3NFT>(activation);
                }
                var address=nftTokenAddress.ToLowerInvariant();
                var observed=await InvokePolkadotSdkAsync(new {operation="psp34ReadCollection",endpoint=_rpcEndpoint,
                    genesisHash=ReportedGenesisHash,contractAddress=address,contractCodeHash=_nftConfiguration.CodeHash,
                    origin=_nftConfiguration.QueryOrigin});
                response.Result=ParsePsp34Collection(observed,address);
                response.IsLoaded=true;response.Message="PSP34 collection metadata loaded from finalized state.";
            } catch(Exception cause) {
                OASISErrorHandling.HandleError(ref response,$"Error loading PSP34 collection: {cause.Message}",cause);
            }
            return response;
        }

        private IWeb3NFT ParsePsp34Collection(JsonElement observed,string address)
        {
            if(_nftConfiguration==null||observed.GetProperty("contractAddress").GetString()!=address
                ||observed.GetProperty("codeHash").GetString()!=_nftConfiguration.CodeHash
                ||!IsGenesisHash(observed.GetProperty("blockHash").GetString())
                ||observed.GetProperty("collectionId").ValueKind!=JsonValueKind.Object)
                throw new System.IO.InvalidDataException("PSP34 collection response differs from the reviewed contract identity.");
            var attributes=observed.GetProperty("attributes");
            string Attribute(string key) {
                var value=attributes.GetProperty(key);
                return value.ValueKind==JsonValueKind.Null?null:value.ValueKind==JsonValueKind.String?value.GetString():
                    throw new System.IO.InvalidDataException("PSP34 metadata attribute must be text or absent.");
            }
            return new Web3NFT {
                NFTTokenAddress=address,Title=Attribute("name")??string.Empty,Symbol=Attribute("symbol")??string.Empty,
                Description=Attribute("description")??string.Empty,ImageUrl=Attribute("image")??string.Empty,
                OnChainProvider=new EnumValue<ProviderType>(Core.Enums.ProviderType.PolkadotOASIS),
                NFTStandardType=new EnumValue<NFTStandardType>(NFTStandardType.PSP34),
                MetaData=new Dictionary<string,string> {
                    [NativeNftChainKey]=ReportedGenesisHash,
                    ["polkadot:collection-id"]=observed.GetProperty("collectionId").GetRawText(),
                    ["polkadot:block-hash"]=observed.GetProperty("blockHash").GetString(),
                    ["polkadot:code-hash"]=_nftConfiguration.CodeHash
                }
            };
        }
    }
}
