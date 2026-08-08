using Ipfs;
using Newtonsoft.Json;
using NextGenSoftware.CLI.Engine;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.GeoSpatialNFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.GeoSpatialNFT.Request;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.GeoSpatialNFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Request;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.ONODE.Core.Enums;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Managers;
using NextGenSoftware.OASIS.API.ONODE.Core.Managers.Base;
using NextGenSoftware.OASIS.API.Providers.IPFSOASIS;
using NextGenSoftware.OASIS.API.Providers.PinataOASIS;
using NextGenSoftware.Logging;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;
namespace NextGenSoftware.OASIS.API.ONODE.Core.Managers
{
    public partial class NFTManager
    {
        public OASISResult<bool> IsNFTStandardTypeValid(IMintWeb4NFTRequest request, string errorMessage = "")
        {
            return IsNFTStandardTypeValid(request.NFTStandardType.Value, request.OnChainProvider.Value, errorMessage);
        }

        public OASISResult<bool> IsNFTStandardTypeValid(NFTStandardType NFTStandardType, ProviderType onChainProvider, string errorMessage = "")
        {
            OASISResult<bool> result = new OASISResult<bool>();

            if (NFTStandardType == NFTStandardType.SPL && onChainProvider != ProviderType.SolanaOASIS)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} NFTStandardType is set to SPL but OnChainProvider is not set to SolanaOASIS! Please make sure you set the OnChainProvider to SolanaOASIS when minting SPL NFTs.");
                return result;
            }

            if (NFTStandardType != NFTStandardType.SPL && onChainProvider == ProviderType.SolanaOASIS)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} OnChainProvider is set to SolanaOASIS but NFTStandardType is not set to SPL! Please make sure you set the NFTStandardType to SPL when minting NFTs on SolanaOASIS.");
                return result;
            }

            if ((NFTStandardType == NFTStandardType.ERC721 || NFTStandardType == NFTStandardType.ERC1155) && !ProviderManager.Instance.IsProviderEVMBlockchain(onChainProvider))
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} When selecting NFTStandardType ERC721 or ERC1155 then the OnChainProvider needs to be set to a supported EVM chain such as ArbitrumOASIS, EthereumOASIS, PolygonOASIS & BaseOASIS.");
                return result;
            }

            return result;
        }

        public async Task<OASISResult<IWeb3NFT>> LoadWeb3NftAsync(Guid id, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IWeb3NFT> result = new OASISResult<IWeb3NFT>();
            string errorMessage = "Error occured in LoadWeb3NftAsync in NFTManager. Reason:";

            try
            {
                result = DecodeNFTMetaData(await Data.LoadHolonAsync(id, true, true, 0, true, false, HolonType.All, 0, providerType), result, errorMessage);
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
            }

            return result;
        }

        public OASISResult<IWeb3NFT> LoadWeb3Nft(Guid id, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IWeb3NFT> result = new OASISResult<IWeb3NFT>();
            string errorMessage = "Error occured in LoadWeb3Nft in NFTManager. Reason:";

            try
            {
                result = DecodeNFTMetaData(Data.LoadHolon(id, true, true, 0, true, false, HolonType.All, 0, providerType), result, errorMessage);
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
            }

            return result;
        }

        public async Task<OASISResult<IWeb3NFT>> LoadWeb3NftAsync(string onChainNftHash, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IWeb3NFT> result = new OASISResult<IWeb3NFT>();
            string errorMessage = "Error occured in LoadWeb3NftAsync in NFTManager. Reason:";

            try
            {
                result = DecodeNFTMetaData(await Data.LoadHolonByMetaDataAsync("NFT.Hash", onChainNftHash, HolonType.Web3NFT, true, true, 0, true, false, HolonType.All, 0, providerType), result, errorMessage);

            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
            }

            return result;
        }

        public OASISResult<IWeb3NFT> LoadWeb3Nft(string onChainNftHash, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IWeb3NFT> result = new OASISResult<IWeb3NFT>();
            string errorMessage = "Error occured in LoadWeb3Nft in NFTManager. Reason:";

            try
            {
                //result = DecodeNFTMetaData(Data.LoadHolonByCustomKey(onChainNftHash, true, true, 0, true, false, HolonType.All, 0, providerType), result, errorMessage);
                result = DecodeNFTMetaData(Data.LoadHolonByMetaData("NFT.Hash", onChainNftHash, HolonType.Web3NFT, true, true, 0, true, false, HolonType.All, 0, providerType), result, errorMessage);
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
            }

            return result;
        }

        public async Task<OASISResult<IWeb4NFT>> LoadWeb4NftAsync(Guid id, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IWeb4NFT> result = new OASISResult<IWeb4NFT>();
            string errorMessage = "Error occured in LoadWeb4NftAsync in NFTManager. Reason:";

            try
            {
                result = DecodeNFTMetaData(await Data.LoadHolonAsync(id, true, true, 0, true, false, HolonType.All, 0, providerType), result, errorMessage);
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
            }

            return result;
        }

        public OASISResult<IWeb4NFT> LoadWeb4Nft(Guid id, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IWeb4NFT> result = new OASISResult<IWeb4NFT>();
            string errorMessage = "Error occured in LoadWeb4Nft in NFTManager. Reason:";

            try
            {
                result = DecodeNFTMetaData(Data.LoadHolon(id, true, true, 0, true, false, HolonType.All, 0, providerType), result, errorMessage);
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
            }

            return result;
        }

        //TODO: Need to refactor this because it needs to check all the child web nfts to find the matching hash...
        public async Task<OASISResult<IWeb4NFT>> LoadWeb4NftAsync(string onChainNftHash, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IWeb4NFT> result = new OASISResult<IWeb4NFT>();
            string errorMessage = "Error occured in LoadWeb4NftAsync in NFTManager. Reason:";

            try
            {
                result = DecodeNFTMetaData(await Data.LoadHolonByMetaDataAsync("NFT.Hash", onChainNftHash, HolonType.Web4NFT, true, true, 0, true, false, HolonType.All, 0, providerType), result, errorMessage);

            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
            }

            return result;
        }

        //TODO: Need to refactor this because it needs to check all the child web nfts to find the matching hash...
        public OASISResult<IWeb4NFT> LoadWeb4Nft(string onChainNftHash, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IWeb4NFT> result = new OASISResult<IWeb4NFT>();
            string errorMessage = "Error occured in LoadWeb4Nft in NFTManager. Reason:";

            try
            {
                //result = DecodeNFTMetaData(Data.LoadHolonByCustomKey(onChainNftHash, true, true, 0, true, false, HolonType.All, 0, providerType), result, errorMessage);
                result = DecodeNFTMetaData(Data.LoadHolonByMetaData("NFT.Hash", onChainNftHash, HolonType.Web4NFT, true, true, 0, true, false, HolonType.All, 0, providerType), result, errorMessage);
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
            }

            return result;
        }

        public async Task<OASISResult<IWeb4GeoSpatialNFT>> LoadWeb4GeoNftAsync(Guid id, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IWeb4GeoSpatialNFT> result = new OASISResult<IWeb4GeoSpatialNFT>();
            string errorMessage = "Error occured in LoadWeb4GeoNftAsync in NFTManager. Reason:";

            try
            {
                result = DecodeGeoNFTMetaData(await Data.LoadHolonAsync(id, true, true, 0, true, false, HolonType.All, 0, providerType), result, errorMessage);
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
            }

            return result;
        }

        public OASISResult<IWeb4GeoSpatialNFT> LoadWeb4GeoNft(Guid id, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IWeb4GeoSpatialNFT> result = new OASISResult<IWeb4GeoSpatialNFT>();
            string errorMessage = "Error occured in LoadWeb4GeoNft in NFTManager. Reason:";

            try
            {
                result = DecodeGeoNFTMetaData(Data.LoadHolon(id, true, true, 0, true, false, HolonType.All, 0, providerType), result, errorMessage);
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
            }

            return result;
        }

        //TODO: Need to refactor this because it needs to check all the child web nfts to find the matching hash...
        public async Task<OASISResult<IWeb4GeoSpatialNFT>> LoadWeb4GeoNftAsync(string onChainNftHash, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IWeb4GeoSpatialNFT> result = new OASISResult<IWeb4GeoSpatialNFT>();
            string errorMessage = "Error occured in LoadWeb4GeoNftAsync in NFTManager. Reason:";

            try
            {
                //result = DecodeGeoNFTMetaData(await Data.LoadHolonByCustomKeyAsync(onChainNftHash, true, true, 0, true, false, HolonType.All, 0, providerType), result, errorMessage);
                result = DecodeGeoNFTMetaData(await Data.LoadHolonByMetaDataAsync("NFT.Hash", onChainNftHash, HolonType.Web4GeoNFT, true, true, 0, true, false, HolonType.All, 0, providerType), result, errorMessage);
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
            }

            return result;
        }

        //TODO: Need to refactor this because it needs to check all the child web nfts to find the matching hash...
        public OASISResult<IWeb4GeoSpatialNFT> LoadWeb4GeoNft(string onChainNftHash, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IWeb4GeoSpatialNFT> result = new OASISResult<IWeb4GeoSpatialNFT>();
            string errorMessage = "Error occured in LoadWeb4GeoNft in NFTManager. Reason:";

            try
            {
                //result = DecodeGeoNFTMetaData(Data.LoadHolonByCustomKey(onChainNftHash, true, true, 0, true, false, HolonType.All, 0, providerType), result, errorMessage);
                result = DecodeGeoNFTMetaData(Data.LoadHolonByMetaData("NFT.Hash", onChainNftHash, HolonType.Web4GeoNFT, true, true, 0, true, false, HolonType.All, 0, providerType), result, errorMessage);
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
            }

            return result;
        }

        public async Task<OASISResult<IEnumerable<IWeb3NFT>>> LoadAllWeb3NFTsForAvatarAsync(Guid avatarId, Guid parentWeb4NFTId = default, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<IWeb3NFT>> result = new OASISResult<IEnumerable<IWeb3NFT>>();
            string errorMessage = "Error occured in LoadAllWeb3NFTsForAvatarAsync in NFTManager. Reason:";

            try
            {
                if (parentWeb4NFTId != Guid.Empty)
                {
                    result = DecodeNFTMetaData(await Data.LoadHolonsByMetaDataAsync(new Dictionary<string, string>()
                    {
                        { "NFT.MintedByAvatarId", avatarId.ToString() },
                        { "NFT.ParentWeb4NFTId", parentWeb4NFTId.ToString() }
                    }, MetaKeyValuePairMatchMode.All, HolonType.Web3NFT, providerType: providerType), result, errorMessage);
                }
                else
                    result = DecodeNFTMetaData(await Data.LoadHolonsByMetaDataAsync("NFT.MintedByAvatarId", avatarId.ToString(), HolonType.Web3NFT, true, true, 0, true, false, 0, HolonType.All, 0, providerType), result, errorMessage);
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
            }

            return result;
        }

        public OASISResult<IEnumerable<IWeb3NFT>> LoadAllWeb3NFTsForAvatar(Guid avatarId, Guid parentWeb4NFTId = default, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<IWeb3NFT>> result = new OASISResult<IEnumerable<IWeb3NFT>>();
            string errorMessage = "Error occured in LoadAllWeb3NFTsForAvatar in NFTManager. Reason:";

            try
            {
                if (parentWeb4NFTId != Guid.Empty)
                {
                    result = DecodeNFTMetaData(Data.LoadHolonsByMetaData(new Dictionary<string, string>()
                    {
                        { "NFT.MintedByAvatarId", avatarId.ToString() },
                        { "NFT.ParentWeb4NFTId", parentWeb4NFTId.ToString() }
                    }, MetaKeyValuePairMatchMode.All, HolonType.Web3NFT, providerType: providerType), result, errorMessage);
                }
                else
                    result = DecodeNFTMetaData(Data.LoadHolonsByMetaData("NFT.MintedByAvatarId", avatarId.ToString(), HolonType.Web3NFT, true, true, 0, true, false, 0, HolonType.All, 0, providerType), result, errorMessage);
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
            }

            return result;
        }

        public async Task<OASISResult<IEnumerable<IWeb3NFT>>> LoadAllWeb3NFTsForMintAddressAsync(string mintWalletAddress, Guid parentWeb4NFTId = default, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<IWeb3NFT>> result = new OASISResult<IEnumerable<IWeb3NFT>>();
            string errorMessage = "Error occured in LoadAllWeb3NFTsForMintAddressAsync in NFTManager. Reason:";

            try
            {
                if (parentWeb4NFTId != Guid.Empty)
                {
                    result = DecodeNFTMetaData(await Data.LoadHolonsByMetaDataAsync(new Dictionary<string, string>()
                    {
                        { "NFT.MintWalletAddress", mintWalletAddress },
                        { "NFT.ParentWeb4NFTId", parentWeb4NFTId.ToString() }
                    }, MetaKeyValuePairMatchMode.All, HolonType.Web3NFT, providerType: providerType), result, errorMessage);
                }
                else
                    result = DecodeNFTMetaData(await Data.LoadHolonsByMetaDataAsync("NFT.MintWalletAddress", mintWalletAddress, HolonType.Web3NFT, true, true, 0, true, false, 0, HolonType.All, 0, providerType), result, errorMessage);
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
            }

            return result;
        }

        public OASISResult<IEnumerable<IWeb3NFT>> LoadAllWeb3NFTsForMintAddress(string mintWalletAddress, Guid parentWeb4NFTId = default, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<IWeb3NFT>> result = new OASISResult<IEnumerable<IWeb3NFT>>();
            string errorMessage = "Error occured in LoadAllNFTsForMintAddress in NFTManager. Reason:";

            try
            {
                if (parentWeb4NFTId != Guid.Empty)
                {
                    result = DecodeNFTMetaData(Data.LoadHolonsByMetaData(new Dictionary<string, string>()
                    {
                        { "NFT.MintWalletAddress", mintWalletAddress },
                        { "NFT.ParentWeb4NFTId", parentWeb4NFTId.ToString() }
                    }, MetaKeyValuePairMatchMode.All, HolonType.Web3NFT, providerType: providerType), result, errorMessage);
                }
                else
                    result = DecodeNFTMetaData(Data.LoadHolonsByMetaData("NFT.MintWalletAddress", mintWalletAddress, HolonType.Web3NFT, true, true, 0, true, false, 0, HolonType.All, 0, providerType), result, errorMessage);
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
            }

            return result;
        }

        public async Task<OASISResult<IEnumerable<IWeb4NFT>>> LoadAllWeb4NFTsForAvatarAsync(Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<IWeb4NFT>> result = new OASISResult<IEnumerable<IWeb4NFT>>();
            string errorMessage = "Error occured in LoadAllWeb4NFTsForAvatarAsync in NFTManager. Reason:";

            try
            {
                result = DecodeNFTMetaData(await Data.LoadHolonsByMetaDataAsync("NFT.MintedByAvatarId", avatarId.ToString(), HolonType.Web4NFT, true, true, 0, true, false, 0, HolonType.All, 0, providerType), result, errorMessage);

                //TODO: Want to add new LoadHolonsForAvatar methods to HolonManager eventually, which we would use here instead. It would load all Holons that had CreatedByAvatarId = avatarId. But for now we can just set the ParentId on the holons to the AvatarId.
                // OASISResult<IEnumerable<IHolon>> holonsResult = await Data.LoadHolonsForParentAsync(avatarId, HolonType.Web4NFT, true, true, 0, true, 0, providerType); //This line would also work because by default all holons created have their parent set to the avatar that created them in the HolonManger.
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
            }

            return result;
        }

        public OASISResult<IEnumerable<IWeb4NFT>> LoadAllWeb4NFTsForAvatar(Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<IWeb4NFT>> result = new OASISResult<IEnumerable<IWeb4NFT>>();
            string errorMessage = "Error occured in LoadAllWeb4NFTsForAvatar in NFTManager. Reason:";

            try
            {
                result = DecodeNFTMetaData(Data.LoadHolonsByMetaData("NFT.MintedByAvatarId", avatarId.ToString(), HolonType.Web4NFT, true, true, 0, true, false, 0, HolonType.All, 0, providerType), result, errorMessage);
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
            }

            return result;
        }

        public async Task<OASISResult<IEnumerable<IWeb4NFT>>> LoadAllWeb4NFTsForMintAddressAsync(string mintWalletAddress, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<IWeb4NFT>> result = new OASISResult<IEnumerable<IWeb4NFT>>();
            string errorMessage = "Error occured in LoadAllWeb4NFTsForMintAddressAsync in NFTManager. Reason:";

            try
            {
                result = DecodeNFTMetaData(await Data.LoadHolonsByMetaDataAsync("NFT.MintWalletAddress", mintWalletAddress, HolonType.Web4NFT, true, true, 0, true, false, 0, HolonType.All, 0, providerType), result, errorMessage);

                //TODO: We could possibly add a CustomKey2 property to Holons to load by but not sure how far we go with this? I think eventually we may have 3 custom keys you can load by but for now Search will do... ;-)
                //OASISResult<ISearchResults> searchResult = await SearchManager.Instance.SearchAsync(new SearchParams()
                //{
                //    SearchGroups = new List<ISearchGroupBase>()
                //    {
                //        new SearchTextGroup()
                //        {
                //            SearchQuery = mintWalletAddress,
                //            SearchHolons = true,
                //            HolonSearchParams = new SearchHolonParams()
                //            {
                //                 MetaData = true,
                //                 MetaDataKey = "NFT.MintWalletAddress"
                //            }
                //        },
                //        new SearchTextGroup()
                //        {
                //            PreviousSearchGroupOperator = SearchParamGroupOperator.And, //This wll currently not work in MongoDBOASIS Provider because it currently only supports OR and not AND...
                //            SearchQuery = "NFT",
                //            SearchHolons = true,
                //            HolonSearchParams = new SearchHolonParams()
                //            {
                //                Name = true
                //            }
                //        }
                //    }
                //});

                //if (searchResult != null && !searchResult.IsError && searchResult.Result != null)
                //{
                //    foreach (IHolon holon in searchResult.Result.SearchResultHolons)
                //        result.Result.Add((IWeb4OASISNFT)JsonSerializer.Deserialize(holon.MetaData["OASISNFT"].ToString(), typeof(IWeb4OASISNFT)));
                //}
                //else
                //    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured loading/searching the holon metadata. Reason: {searchResult.Message}");
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
            }

            return result;
        }

        public OASISResult<IEnumerable<IWeb4NFT>> LoadAllWeb4NFTsForMintAddress(string mintWalletAddress, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<IWeb4NFT>> result = new OASISResult<IEnumerable<IWeb4NFT>>();
            string errorMessage = "Error occured in LoadAllWeb4NFTsForMintAddress in NFTManager. Reason:";

            try
            {
                result = DecodeNFTMetaData(Data.LoadHolonsByMetaData("NFT.MintedByAvatarId", mintWalletAddress, HolonType.Web4NFT, true, true, 0, true, false, 0, HolonType.All, 0, providerType), result, errorMessage);
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
            }

            return result;
        }

        public async Task<OASISResult<IEnumerable<IWeb4GeoSpatialNFT>>> LoadAllWeb4GeoNFTsForAvatarAsync(Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<IWeb4GeoSpatialNFT>> result = new OASISResult<IEnumerable<IWeb4GeoSpatialNFT>>();
            string errorMessage = "Error occured in LoadAllWeb4GeoNFTsForAvatarAsync in NFTManager. Reason:";

            try
            {
                result = DecodeGeoNFTMetaData(await Data.LoadHolonsByMetaDataAsync("GEONFT.PlacedByAvatarId", avatarId.ToString(), HolonType.Web4GeoNFT, true, true, 0, true, false, 0, HolonType.All, 0, providerType), result, errorMessage);
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
            }

            return result;
        }

        public OASISResult<IEnumerable<IWeb4GeoSpatialNFT>> LoadAllWeb4GeoNFTsForAvatar(Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<IWeb4GeoSpatialNFT>> result = new OASISResult<IEnumerable<IWeb4GeoSpatialNFT>>();
            string errorMessage = "Error occured in LoadAllWeb4GeoNFTsForAvatar in NFTManager. Reason:";

            try
            {
                result = DecodeGeoNFTMetaData(Data.LoadHolonsByMetaData("GEONFT.MintedByAvatarId", avatarId.ToString(), HolonType.Web4GeoNFT, true, true, 0, true, false, 0, HolonType.All, 0, providerType), result, errorMessage);
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
            }

            return result;
        }

        public async Task<OASISResult<IEnumerable<IWeb4GeoSpatialNFT>>> LoadAllWeb4GeoNFTsForMintAddressAsync(string mintWalletAddress, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<IWeb4GeoSpatialNFT>> result = new OASISResult<IEnumerable<IWeb4GeoSpatialNFT>>();
            string errorMessage = "Error occured in LoadAllWeb4GeoNFTsForMintAddressAsync in NFTManager. Reason:";

            try
            {
                result = DecodeGeoNFTMetaData(await Data.LoadHolonsByMetaDataAsync("GEONFT.OriginalOASISNFT.MintWalletAddress", mintWalletAddress, HolonType.Web4GeoNFT, true, true, 0, true, false, 0, HolonType.All, 0, providerType), result, errorMessage);
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
            }

            return result;
        }

        public OASISResult<IEnumerable<IWeb4GeoSpatialNFT>> LoadAllWeb4GeoNFTsForMintAddress(string mintWalletAddress, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<IWeb4GeoSpatialNFT>> result = new OASISResult<IEnumerable<IWeb4GeoSpatialNFT>>();
            string errorMessage = "Error occured in LoadAllWeb4GeoNFTsForMintAddress in NFTManager. Reason:";

            try
            {
                result = DecodeGeoNFTMetaData(Data.LoadHolonsByMetaData("GEONFT.OriginalOASISNFT.MintWalletAddress", mintWalletAddress, HolonType.Web4GeoNFT, true, true, 0, true, false, 0, HolonType.All, 0, providerType), result, errorMessage);
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
            }

            return result;
        }

        public async Task<OASISResult<IEnumerable<IWeb4GeoSpatialNFT>>> LoadAllWeb4GeoNFTsForAvatarLocationAsync(long latLocation, long longLocation, int radius, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<IWeb4GeoSpatialNFT>> result = new OASISResult<IEnumerable<IWeb4GeoSpatialNFT>>();
            string errorMessage = "Error occured in LoadAllWeb4GeoNFTsForAvatarLocationAsync in NFTManager. Reason:";

            try
            {
                if (radius > 0)
                {
                    //Create a bounding box.
                    long topLeftLat = latLocation - radius;
                    long topLeftLong = longLocation - radius;
                    long topRightLat = latLocation - radius;
                    long topRightLong = longLocation + radius;
                    long bottomRightLat = latLocation - radius;
                    long bottomRightLong = longLocation + radius;
                    long bottomLeftLat = latLocation - radius;
                    long bottomLeftLong = longLocation - radius;

                    if (topLeftLat < 0)
                        topLeftLat = 0;

                    if (topLeftLong < 0)
                        topLeftLong = 0;

                    if (topRightLat < 0)
                        topRightLat = 0;

                    if (topRightLong < 0)
                        topRightLong = 0;

                    if (bottomRightLat < 0)
                        bottomRightLat = 0;

                    if (bottomRightLong < 0)
                        bottomRightLong = 0;

                    if (bottomLeftLat < 0)
                        bottomLeftLat = 0;

                    if (bottomLeftLong < 0)
                        bottomLeftLong = 0;

                    //TODO: Eventually we want to be able to load only the NFTs we need rather than having to load them all into memory! We need to run the geo-spatial query on the provider itself! ;-)
                    OASISResult<IEnumerable<IWeb4GeoSpatialNFT>> geoNfts = await LoadAllWeb4GeoNFTsAsync(providerType);

                    if (geoNfts != null && !geoNfts.IsError && geoNfts.Result != null)
                    {
                        List<IWeb4GeoSpatialNFT> matchedGeoNFTs = new List<IWeb4GeoSpatialNFT>();

                        foreach (IWeb4GeoSpatialNFT geoSpatialNFT in geoNfts.Result)
                        {
                            if (geoSpatialNFT.Lat >= bottomLeftLat && geoSpatialNFT.Long >= bottomLeftLong
                                && geoSpatialNFT.Lat <= topLeftLat && geoSpatialNFT.Long >= topLeftLong
                                && geoSpatialNFT.Lat <= topRightLat && geoSpatialNFT.Long <= topRightLong
                                && geoSpatialNFT.Lat >= bottomRightLat && geoSpatialNFT.Long <= bottomRightLong)
                                matchedGeoNFTs.Add(geoSpatialNFT);
                        }

                        result.Result = matchedGeoNFTs;
                    }
                }
                else
                    result = DecodeGeoNFTMetaData(await Data.LoadHolonsByMetaDataAsync("GEONFT.LatLong", string.Concat(latLocation.ToString(), ":", longLocation.ToString()), HolonType.Web4GeoNFT, true, true, 0, true, false, 0, HolonType.All, 0, providerType), result, errorMessage);

            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
            }

            return result;
        }

        public OASISResult<IEnumerable<IWeb4GeoSpatialNFT>> LoadAllWeb4GeoNFTsForAvatarLocation(long latLocation, long longLocation, int radius, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<IWeb4GeoSpatialNFT>> result = new OASISResult<IEnumerable<IWeb4GeoSpatialNFT>>();
            string errorMessage = "Error occured in LoadAllWeb4GeoNFTsForAvatarLocationAsync in NFTManager. Reason:";

            try
            {
                if (radius > 0)
                {
                    //Create a bounding box.
                    long topLeftLat = latLocation - radius;
                    long topLeftLong = longLocation - radius;
                    long topRightLat = latLocation - radius;
                    long topRightLong = longLocation + radius;
                    long bottomRightLat = latLocation - radius;
                    long bottomRightLong = longLocation + radius;
                    long bottomLeftLat = latLocation - radius;
                    long bottomLeftLong = longLocation - radius;

                    if (topLeftLat < 0)
                        topLeftLat = 0;

                    if (topLeftLong < 0)
                        topLeftLong = 0;

                    if (topRightLat < 0)
                        topRightLat = 0;

                    if (topRightLong < 0)
                        topRightLong = 0;

                    if (bottomRightLat < 0)
                        bottomRightLat = 0;

                    if (bottomRightLong < 0)
                        bottomRightLong = 0;

                    if (bottomLeftLat < 0)
                        bottomLeftLat = 0;

                    if (bottomLeftLong < 0)
                        bottomLeftLong = 0;

                    //TODO: Eventually we want to be able to load only the NFTs we need rather than having to load them all into memory! We need to run the geo-spatial query on the provider itself! ;-)
                    OASISResult<IEnumerable<IWeb4GeoSpatialNFT>> geoNfts = LoadAllWeb4GeoNFTs(providerType);

                    if (geoNfts != null && !geoNfts.IsError && geoNfts.Result != null)
                    {
                        List<IWeb4GeoSpatialNFT> matchedGeoNFTs = new List<IWeb4GeoSpatialNFT>();

                        foreach (IWeb4GeoSpatialNFT geoSpatialNFT in geoNfts.Result)
                        {
                            if (geoSpatialNFT.Lat >= bottomLeftLat && geoSpatialNFT.Long >= bottomLeftLong
                                && geoSpatialNFT.Lat <= topLeftLat && geoSpatialNFT.Long >= topLeftLong
                                && geoSpatialNFT.Lat <= topRightLat && geoSpatialNFT.Long <= topRightLong
                                && geoSpatialNFT.Lat >= bottomRightLat && geoSpatialNFT.Long <= bottomRightLong)
                                matchedGeoNFTs.Add(geoSpatialNFT);
                        }

                        result.Result = matchedGeoNFTs;
                    }
                }
                else
                    result = DecodeGeoNFTMetaData(Data.LoadHolonsByMetaData("GEONFT.LatLong", string.Concat(latLocation.ToString(), ":", longLocation.ToString()), HolonType.Web4GeoNFT, true, true, 0, true, false, 0, HolonType.All, 0, providerType), result, errorMessage);

            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
            }

            return result;
        }

        public async Task<OASISResult<IEnumerable<IWeb3NFT>>> LoadAllWeb3NFTsAsync(Guid parentWeb4NFTId = default, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<IWeb3NFT>> result = new OASISResult<IEnumerable<IWeb3NFT>>();
            string errorMessage = "Error occured in LoadAllWeb3NFTsAsync in NFTManager. Reason:";

            try
            {
                if (parentWeb4NFTId != Guid.Empty)
                    result = DecodeNFTMetaData(await Data.LoadHolonsByMetaDataAsync("NFT.ParentWeb4NFTId", parentWeb4NFTId.ToString(), HolonType.Web3NFT, true, true, 0, true, false, 0, HolonType.All, 0, providerType), result, errorMessage);
                else
                    result = DecodeNFTMetaData(await Data.LoadAllHolonsAsync(HolonType.Web3NFT, true, true, 0, true, false, HolonType.All, 0, providerType), result, errorMessage);
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
            }

            return result;
        }

        public OASISResult<IEnumerable<IWeb3NFT>> LoadAllWeb3NFTs(Guid parentWeb4NFTId = default, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<IWeb3NFT>> result = new OASISResult<IEnumerable<IWeb3NFT>>();
            string errorMessage = "Error occured in LoadAllWeb3NFTs in NFTManager. Reason:";

            try
            {
                if (parentWeb4NFTId != Guid.Empty)
                    result = DecodeNFTMetaData(Data.LoadHolonsByMetaData("NFT.ParentWeb4NFTId", parentWeb4NFTId.ToString(), HolonType.Web3NFT, true, true, 0, true, false, 0, HolonType.All, 0, providerType), result, errorMessage);
                else
                    result = DecodeNFTMetaData(Data.LoadAllHolons(HolonType.Web3NFT, true, true, 0, true, false, HolonType.All, 0, providerType), result, errorMessage);
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
            }

            return result;
        }

        public async Task<OASISResult<IEnumerable<IWeb4NFT>>> LoadAllWeb4NFTsAsync(ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<IWeb4NFT>> result = new OASISResult<IEnumerable<IWeb4NFT>>();
            string errorMessage = "Error occured in LoadAllWeb4NFTsAsync in NFTManager. Reason:";

            try
            {
                result = DecodeNFTMetaData(await Data.LoadAllHolonsAsync(HolonType.Web4NFT, true, true, 0, true, false, HolonType.All, 0, providerType), result, errorMessage);
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
            }

            return result;
        }

        public OASISResult<IEnumerable<IWeb4NFT>> LoadAllWeb4NFTs(ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<IWeb4NFT>> result = new OASISResult<IEnumerable<IWeb4NFT>>();
            string errorMessage = "Error occured in LoadAllWeb4NFTs in NFTManager. Reason:";

            try
            {
                result = DecodeNFTMetaData(Data.LoadAllHolons(HolonType.Web4NFT, true, true, 0, true, false, HolonType.All, 0, providerType), result, errorMessage);
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
            }

            return result;
        }

        public async Task<OASISResult<IEnumerable<IWeb4GeoSpatialNFT>>> LoadAllWeb4GeoNFTsAsync(ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<IWeb4GeoSpatialNFT>> result = new OASISResult<IEnumerable<IWeb4GeoSpatialNFT>>();
            string errorMessage = "Error occured in LoadAllWeb4GeoNFTsAsync in NFTManager. Reason:";

            try
            {
                result = DecodeGeoNFTMetaData(await Data.LoadAllHolonsAsync(HolonType.Web4GeoNFT, true, true, 0, true, false, HolonType.All, 0, providerType), result, errorMessage);
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
            }

            return result;
        }

        public OASISResult<IEnumerable<IWeb4GeoSpatialNFT>> LoadAllWeb4GeoNFTs(ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<IWeb4GeoSpatialNFT>> result = new OASISResult<IEnumerable<IWeb4GeoSpatialNFT>>();
            string errorMessage = "Error occured in LoadAllWeb4GeoNFTs in NFTManager. Reason:";

            try
            {
                result = DecodeGeoNFTMetaData(Data.LoadAllHolons(HolonType.Web4GeoNFT, true, true, 0, true, false, HolonType.All, 0, providerType), result, errorMessage);
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
            }

            return result;
        }

    }
}