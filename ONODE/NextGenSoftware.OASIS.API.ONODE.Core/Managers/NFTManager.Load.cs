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
                    long topLeftLat = latLocation - radius;
                    long topLeftLong = longLocation - radius;
                    long topRightLat = latLocation - radius;
                    long topRightLong = longLocation + radius;
                    long bottomRightLat = latLocation - radius;
                    long bottomRightLong = longLocation + radius;
                    long bottomLeftLat = latLocation - radius;
                    long bottomLeftLong = longLocation - radius;

                    if (topLeftLat < 0) topLeftLat = 0;
                    if (topLeftLong < 0) topLeftLong = 0;
                    if (topRightLat < 0) topRightLat = 0;
                    if (topRightLong < 0) topRightLong = 0;
                    if (bottomRightLat < 0) bottomRightLat = 0;
                    if (bottomRightLong < 0) bottomRightLong = 0;
                    if (bottomLeftLat < 0) bottomLeftLat = 0;
                    if (bottomLeftLong < 0) bottomLeftLong = 0;

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
                    long topLeftLat = latLocation - radius;
                    long topLeftLong = longLocation - radius;
                    long topRightLat = latLocation - radius;
                    long topRightLong = longLocation + radius;
                    long bottomRightLat = latLocation - radius;
                    long bottomRightLong = longLocation + radius;
                    long bottomLeftLat = latLocation - radius;
                    long bottomLeftLong = longLocation - radius;

                    if (topLeftLat < 0) topLeftLat = 0;
                    if (topLeftLong < 0) topLeftLong = 0;
                    if (topRightLat < 0) topRightLat = 0;
                    if (topRightLong < 0) topRightLong = 0;
                    if (bottomRightLat < 0) bottomRightLat = 0;
                    if (bottomRightLong < 0) bottomRightLong = 0;
                    if (bottomLeftLat < 0) bottomLeftLat = 0;
                    if (bottomLeftLong < 0) bottomLeftLong = 0;

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

        public async Task<OASISResult<IWeb4GeoSpatialNFT>> PlaceWeb4GeoNFTAsync(IPlaceWeb4GeoSpatialNFTRequest request, ResponseFormatType responseFormatType = ResponseFormatType.FormattedText)
        {
            OASISResult<IWeb4GeoSpatialNFT> result = new OASISResult<IWeb4GeoSpatialNFT>();
            if (request == null)
            {
                result.IsError = true;
                result.Message = "The request is required. Please provide a valid IPlaceWeb4GeoSpatialNFTRequest.";
                return result;
            }
            string errorMessage = "Error occured in PlaceWeb4GeoNFTAsync in NFTManager. Reason:";

            try
            {
                OASISResult<IWeb4NFT> loadNftResult = await LoadWeb4NftAsync(request.OriginalWeb4OASISNFTId, request.OriginalWeb4OASISNFTOffChainProvider.Value);

                if (loadNftResult != null && !loadNftResult.IsError && loadNftResult.Result != null)
                {
                    result.Result = CreateWeb4GeoSpatialNFT(request, loadNftResult.Result);
                    OASISResult<IHolon> saveHolonResult = Data.SaveHolon(CreateWeb4GeoSpatialNFTMetaDataHolon(result.Result), request.PlacedByAvatarId, true, true, 0, true, false, request.GeoNFTMetaDataProvider.Value);

                    if ((saveHolonResult != null && (saveHolonResult.IsError || saveHolonResult.Result == null)) || saveHolonResult == null)
                    {
                        result.Result = null;
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured saving metadata holon to the GeoNFTMetaDataProvider {Enum.GetName(typeof(ProviderType), request.GeoNFTMetaDataProvider)}. Reason: {saveHolonResult.Message}");
                    }
                    {
                        List<IWeb3NFT> web3NFTs = new List<IWeb3NFT>();

                        foreach (IWeb3NFT webNFT in loadNftResult.Result.Web3NFTs)
                            web3NFTs.Add(webNFT);

                        result.Message = FormatSuccessMessage(result, web3NFTs, responseFormatType: responseFormatType);
                    }
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured loading original OASIS NFT with id {request.OriginalWeb4OASISNFTId}. Reason: {loadNftResult.Message}");
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
            }

            return result;
        }

        public OASISResult<IWeb4GeoSpatialNFT> PlaceWeb4GeoNFT(IPlaceWeb4GeoSpatialNFTRequest request, ResponseFormatType responseFormatType = ResponseFormatType.FormattedText)
        {
            OASISResult<IWeb4GeoSpatialNFT> result = new OASISResult<IWeb4GeoSpatialNFT>();
            if (request == null)
            {
                result.IsError = true;
                result.Message = "The request is required. Please provide a valid IPlaceWeb4GeoSpatialNFTRequest.";
                return result;
            }
            string errorMessage = "Error occured in PlaceWeb4GeoNFT in NFTManager. Reason:";

            try
            {
                OASISResult<IWeb4NFT> loadNftResult = LoadWeb4Nft(request.OriginalWeb4OASISNFTId, request.OriginalWeb4OASISNFTOffChainProvider.Value);

                if (loadNftResult != null && !loadNftResult.IsError && loadNftResult.Result != null)
                {
                    result.Result = CreateWeb4GeoSpatialNFT(request, loadNftResult.Result);
                    OASISResult<IHolon> saveHolonResult = Data.SaveHolon(CreateWeb4GeoSpatialNFTMetaDataHolon(result.Result), request.PlacedByAvatarId, true, true, 0, true, false, request.GeoNFTMetaDataProvider.Value);

                    if ((saveHolonResult != null && (saveHolonResult.IsError || saveHolonResult.Result == null)) || saveHolonResult == null)
                    {
                        result.Result = null;
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured saving metadata holon to the GeoNFTMetaDataProvider {Enum.GetName(typeof(ProviderType), request.GeoNFTMetaDataProvider)}. Reason: {saveHolonResult.Message}");
                    }
                    else
                    {
                        List<IWeb3NFT> web3NFTs = new List<IWeb3NFT>();

                        foreach (IWeb3NFT webNFT in loadNftResult.Result.Web3NFTs)
                            web3NFTs.Add(webNFT);

                        result.Message = FormatSuccessMessage(result, web3NFTs, responseFormatType: responseFormatType);
                    }
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured loading original OASIS NFT with id {request.OriginalWeb4OASISNFTId}. Reason: {loadNftResult.Message}");
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
            }

            return result;
        }

        public async Task<OASISResult<IWeb4GeoSpatialNFT>> MintAndPlaceWeb4GeoNFTAsync(IMintAndPlaceWeb4GeoSpatialNFTRequest request, ResponseFormatType responseFormatType = ResponseFormatType.FormattedText)
        {
            OASISResult<IWeb4GeoSpatialNFT> result = new OASISResult<IWeb4GeoSpatialNFT>();
            if (request == null)
            {
                result.IsError = true;
                result.Message = "The request is required. Please provide a valid IMintAndPlaceWeb4GeoSpatialNFTRequest.";
                return result;
            }
            string errorMessage = "Error occured in MintAndPlaceGeoNFTAsync in NFTManager. Reason:";

            try
            {
                OASISResult<IWeb4NFT> mintNftResult = await MintNftAsync(CreateMintWeb4NFTTransactionRequest(request), true);

                if (mintNftResult != null && mintNftResult.Result != null && !mintNftResult.IsError)
                {
                    PlaceWeb4GeoSpatialNFTRequest placeGeoSpatialNFTRequest = new PlaceWeb4GeoSpatialNFTRequest()
                    {
                        OriginalWeb4OASISNFTId = mintNftResult.Result.Id,
                        OriginalWeb4OASISNFTOffChainProvider = request.OffChainProvider != null ? request.OffChainProvider : new EnumValue<ProviderType>(ProviderType.None),
                        GeoNFTMetaDataProvider = request.GeoNFTMetaDataProvider,
                        PlacedByAvatarId = request.MintedByAvatarId,
                        Lat = request.Lat,
                        Long = request.Long,
                        AllowOtherPlayersToAlsoCollect = request.AllowOtherPlayersToAlsoCollect,
                        PermSpawn = request.PermSpawn,
                        GlobalSpawnQuantity = request.GlobalSpawnQuantity,
                        PlayerSpawnQuantity = request.PlayerSpawnQuantity,
                        RespawnDurationInSeconds = request.RespawnDurationInSeconds,
                        Nft2DSprite = request.Nft2DSprite,
                        Nft2DSpriteURI = request.Nft2DSpriteURI,
                        Nft3DObject = request.Nft3DObject,
                        Nft3DObjectURI = request.Nft3DObjectURI
                    };

                    result.Result = CreateWeb4GeoSpatialNFT(placeGeoSpatialNFTRequest, mintNftResult.Result);
                    OASISResult<IHolon> saveHolonResult = await Data.SaveHolonAsync(CreateWeb4GeoSpatialNFTMetaDataHolon(result.Result), request.MintedByAvatarId, true, true, 0, true, false, request.GeoNFTMetaDataProvider.Value);

                    if (saveHolonResult != null && (saveHolonResult.IsError || saveHolonResult.Result == null) || saveHolonResult == null)
                    {
                        result.Result = null;
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured saving metadata holon to the OffChainProvider {Enum.GetName(typeof(ProviderType), request.OffChainProvider.Value)}. Reason: {saveHolonResult.Message}");
                    }
                    else
                    {
                        result.SavedCount = mintNftResult.SavedCount;
                        result.Message = FormatSuccessMessage(request, result, mintNftResult.Result.NewlyMintedWeb3NFTs, responseFormatType);
                    }
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured minting the GEONFT in function MintNftAsync. Reason: {mintNftResult.Message}");
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
            }

            return result;
        }

        public async Task<OASISResult<IWeb4NFT>> UpdateWeb4NFTAsync(IUpdateWeb4NFTRequest request, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IWeb4NFT> result = new();
            string errorMessage = "Error occured in UpdateWeb4NFTAsync in NFTManager. Reason:";

            try
            {
                if (request == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Request is null");
                    return result;
                }

                OASISResult<IHolon> nftHolonResult = await Data.LoadHolonAsync(request.Id, providerType: providerType);

                if (nftHolonResult != null && nftHolonResult.Result != null && !nftHolonResult.IsError)
                {
                    OASISResult<IWeb4NFT> nftResult = DecodeNFTMetaData(nftHolonResult, result, errorMessage);

                    if (nftResult != null && nftResult.Result != null && !nftResult.IsError)
                    {
                        nftResult.Result.Title = !string.IsNullOrEmpty(request.Title) ? request.Title : nftResult.Result.Title;
                        nftResult.Result.Description = !string.IsNullOrEmpty(request.Description) ? request.Description : nftResult.Result.Description;
                        nftResult.Result.MintedByAvatarId = request.MintedByAvatarId != Guid.Empty ? request.MintedByAvatarId : nftResult.Result.MintedByAvatarId;
                        nftResult.Result.ModifiedByAvatarId = request.ModifiedByAvatarId != Guid.Empty ? request.ModifiedByAvatarId : nftResult.Result.ModifiedByAvatarId;
                        nftResult.Result.ModifiedOn = DateTime.Now;
                        nftResult.Result.ImageUrl = !string.IsNullOrEmpty(request.ImageUrl) ? request.ImageUrl : nftResult.Result.ImageUrl;
                        nftResult.Result.Image = request.Image != null ? request.Image : nftResult.Result.Image;
                        nftResult.Result.ThumbnailUrl = !string.IsNullOrEmpty(request.ThumbnailUrl) ? request.ThumbnailUrl : nftResult.Result.ThumbnailUrl;
                        nftResult.Result.Thumbnail = request.Thumbnail != null ? request.Thumbnail : nftResult.Result.Thumbnail;
                        nftResult.Result.MetaData = request.MetaData != null ? request.MetaData : nftResult.Result.MetaData;
                        nftResult.Result.Tags = request.Tags ?? nftResult.Result.Tags;
                        nftResult.Result.Price = request.Price.HasValue ? request.Price.Value : nftResult.Result.Price;
                        nftResult.Result.Discount = request.Discount.HasValue ? request.Discount.Value : nftResult.Result.Discount;
                        nftResult.Result.IsForSale = request.IsForSale.HasValue ? request.IsForSale.Value : nftResult.Result.IsForSale;
                        nftResult.Result.SalesHistory = request.SalesHistory ?? nftResult.Result.SalesHistory;
                        nftResult.Result.RoyaltyPercentage = request.RoyaltyPercentage.HasValue ? request.RoyaltyPercentage.Value : nftResult.Result.RoyaltyPercentage;
                        nftResult.Result.CurrentOwnerAvatarId = request.CurrentOwnerAvatarId != Guid.Empty ? request.CurrentOwnerAvatarId : nftResult.Result.CurrentOwnerAvatarId;
                        nftResult.Result.PreviousOwnerAvatarId = request.PreviousOwnerAvatarId != Guid.Empty ? request.PreviousOwnerAvatarId : nftResult.Result.PreviousOwnerAvatarId;
                        nftResult.Result.LastPurchasedByAvatarId = request.LastPurchasedByAvatarId != Guid.Empty ? request.LastPurchasedByAvatarId : nftResult.Result.LastPurchasedByAvatarId;
                        nftResult.Result.LastSaleAmount = request.LastSaleAmount.HasValue ? request.LastSaleAmount.Value : nftResult.Result.LastSaleAmount;
                        nftResult.Result.LastSaleDate = request.LastSaleDate != DateTime.MinValue ? request.LastSaleDate : nftResult.Result.LastSaleDate;
                        nftResult.Result.LastSaleDiscount = request.LastSaleDiscount.HasValue ? request.LastSaleDiscount.Value : nftResult.Result.LastSaleDiscount;
                        nftResult.Result.LastSalePrice = request.LastSalePrice.HasValue ? request.LastSalePrice.Value : nftResult.Result.LastSalePrice;
                        nftResult.Result.LastSaleQuantity = request.LastSaleQuantity.HasValue ? request.LastSaleQuantity.Value : nftResult.Result.LastSaleQuantity;
                        nftResult.Result.LastSaleTax = request.LastSaleTax.HasValue ? request.LastSaleTax.Value : nftResult.Result.LastSaleTax;
                        nftResult.Result.LastSaleTransactionHash = !string.IsNullOrEmpty(request.LastSaleTransactionHash) ? request.LastSaleTransactionHash : nftResult.Result.LastSaleTransactionHash;
                        nftResult.Result.LastSoldByAvatarId = request.LastSoldByAvatarId != Guid.Empty ? request.LastSoldByAvatarId : nftResult.Result.LastSoldByAvatarId;
                        nftResult.Result.RoyaltyPercentage = request.RoyaltyPercentage.HasValue ? request.RoyaltyPercentage.Value : nftResult.Result.RoyaltyPercentage;
                        nftResult.Result.SaleEndDate = request.SaleEndDate.HasValue ? request.SaleEndDate.Value : nftResult.Result.SaleEndDate;
                        nftResult.Result.SaleStartDate = request.SaleStartDate.HasValue ? request.SaleStartDate.Value : nftResult.Result.SaleStartDate;
                        nftResult.Result.TotalNumberOfSales = request.TotalNumberOfSales.HasValue ? request.TotalNumberOfSales.Value : nftResult.Result.TotalNumberOfSales;

                        if (request.UpdateChildWebNFTIds == null)
                            request.UpdateChildWebNFTIds = new List<string>();

                        if (request.UpdateAllChildWeb3NFTs)
                        {
                            foreach (Web3NFT web3NFT in nftResult.Result.Web3NFTs)
                                request.UpdateChildWebNFTIds.Add(web3NFT.Id.ToString());
                        }

                        foreach (Web3NFT web3NFT in nftResult.Result.Web3NFTs)
                        {
                            if (request.UpdateChildWebNFTIds.Contains(web3NFT.Id.ToString()))
                                UpdateWeb3NFT(web3NFT, request);
                        }

                        OASISResult<IHolon> saveHolonResult = await Data.SaveHolonAsync(UpdateWeb4NFTMetaDataHolon(nftHolonResult.Result, nftResult.Result), request.ModifiedByAvatarId, providerType: providerType);

                        if (saveHolonResult != null && saveHolonResult.Result != null && !saveHolonResult.IsError)
                        {
                            OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(nftResult, result);
                            result.Result = nftResult.Result;

                            OASISResult<bool> updateWeb3NFTHolonsResult = await UpdateWeb3NFTHolonsAsync(request, providerType);

                            if (updateWeb3NFTHolonsResult != null && updateWeb3NFTHolonsResult.Result && !updateWeb3NFTHolonsResult.IsError)
                                result.Message = "Web4 OASIS NFT Updated Successfully.";
                            else
                                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured updating the child WEB3 NFT Holons. Reason: {updateWeb3NFTHolonsResult?.Message}");
                        }
                        else
                            OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured saving Web4 OASIS NFT. Reason: {saveHolonResult?.Message}");
                    }
                    else
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured loading Web4 OASIS NFT. Reason: {nftResult?.Message}");
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured loading Web4 OASIS NFT Holon. Reason: {nftHolonResult?.Message}");
                    return result;
                }
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
            }

            return result;
        }

        public async Task<OASISResult<IWeb4GeoSpatialNFT>> UpdateWeb4GeoNFTAsync(IUpdateWeb4GeoNFTRequest request, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IWeb4GeoSpatialNFT> result = new();
            string errorMessage = "Error occured in UpdateWeb4GeoNFTAsync in NFTManager. Reason:";

            try
            {
                if (request == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Request is null");
                    return result;
                }

                OASISResult<IHolon> nftHolonResult = await Data.LoadHolonAsync(request.Id, providerType: providerType);

                if (nftHolonResult != null && nftHolonResult.Result != null && !nftHolonResult.IsError)
                {
                    OASISResult<IWeb4GeoSpatialNFT> nftResult = DecodeGeoNFTMetaData(nftHolonResult, result, errorMessage);

                    if (nftResult != null && nftResult.Result != null && !nftResult.IsError)
                    {
                        nftResult.Result.Title = !string.IsNullOrEmpty(request.Title) ? request.Title : nftResult.Result.Title;
                        nftResult.Result.Description = !string.IsNullOrEmpty(request.Description) ? request.Description : nftResult.Result.Description;
                        nftResult.Result.ModifiedByAvatarId = request.ModifiedByAvatarId != Guid.Empty ? request.ModifiedByAvatarId : nftResult.Result.ModifiedByAvatarId;
                        nftResult.Result.ModifiedOn = DateTime.Now;
                        nftResult.Result.ImageUrl = !string.IsNullOrEmpty(request.ImageUrl) ? request.ImageUrl : nftResult.Result.ImageUrl;
                        nftResult.Result.Image = request.Image != null ? request.Image : nftResult.Result.Image;
                        nftResult.Result.ThumbnailUrl = !string.IsNullOrEmpty(request.ThumbnailUrl) ? request.ThumbnailUrl : nftResult.Result.ThumbnailUrl;
                        nftResult.Result.Thumbnail = request.Thumbnail != null ? request.Thumbnail : nftResult.Result.Thumbnail;
                        nftResult.Result.MetaData = request.MetaData != null ? request.MetaData : nftResult.Result.MetaData;
                        nftResult.Result.Tags = request.Tags ?? nftResult.Result.Tags;
                        nftResult.Result.Lat = request.Lat.HasValue ? request.Lat.Value : nftResult.Result.Lat;
                        nftResult.Result.Long = request.Long.HasValue ? request.Long.Value : nftResult.Result.Long;
                        nftResult.Result.AllowOtherPlayersToAlsoCollect = request.AllowOtherPlayersToAlsoCollect.HasValue ? request.AllowOtherPlayersToAlsoCollect.Value : nftResult.Result.AllowOtherPlayersToAlsoCollect;
                        nftResult.Result.PermSpawn = request.PermSpawn.HasValue ? request.PermSpawn.Value : nftResult.Result.PermSpawn;
                        nftResult.Result.GlobalSpawnQuantity = request.GlobalSpawnQuantity.HasValue ? request.GlobalSpawnQuantity.Value : nftResult.Result.GlobalSpawnQuantity;
                        nftResult.Result.PlayerSpawnQuantity = request.PlayerSpawnQuantity.HasValue ? request.PlayerSpawnQuantity.Value : nftResult.Result.PlayerSpawnQuantity;
                        nftResult.Result.RespawnDurationInSeconds = request.RespawnDurationInSeconds.HasValue ? request.RespawnDurationInSeconds.Value : nftResult.Result.RespawnDurationInSeconds;
                        nftResult.Result.Nft2DSprite = request.Nft2DSprite != null ? request.Nft2DSprite : nftResult.Result.Nft2DSprite;
                        nftResult.Result.Nft2DSpriteURI = !string.IsNullOrEmpty(request.Nft2DSpriteURI) ? request.Nft2DSpriteURI : nftResult.Result.Nft2DSpriteURI;
                        nftResult.Result.Nft3DObject = request.Nft3DObject != null ? request.Nft3DObject : nftResult.Result.Nft3DObject;
                        nftResult.Result.Nft3DObjectURI = !string.IsNullOrEmpty(request.Nft3DObjectURI) ? request.Nft3DObjectURI : nftResult.Result.Nft3DObjectURI;

                        if (request.UpdateChildWebNFTIds == null)
                            request.UpdateChildWebNFTIds = new List<string>();

                        if (request.UpdateAllChildWeb3NFTs)
                        {
                            foreach (Web3NFT web3NFT in nftResult.Result.Web3NFTs)
                                request.UpdateChildWebNFTIds.Add(web3NFT.Id.ToString());
                        }

                        foreach (Web3NFT web3NFT in nftResult.Result.Web3NFTs)
                        {
                            if (request.UpdateChildWebNFTIds.Contains(web3NFT.Id.ToString()))
                                UpdateWeb3NFT(web3NFT, request);
                        }

                        OASISResult<IHolon> saveHolonResult = await Data.SaveHolonAsync(UpdateWeb4GeoNFTMetaDataHolon(nftHolonResult.Result, nftResult.Result), request.ModifiedByAvatarId, providerType: providerType);

                        if (saveHolonResult != null && saveHolonResult.Result != null && !saveHolonResult.IsError)
                        {
                            OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(nftResult, result);
                            result.Result = nftResult.Result;

                            OASISResult<bool> updateWeb3NFTHolonsResult = await UpdateWeb3NFTHolonsAsync(request, providerType);

                            if (updateWeb3NFTHolonsResult != null && updateWeb3NFTHolonsResult.Result && !updateWeb3NFTHolonsResult.IsError)
                                result.Message = "Web4 OASIS Geo-NFT Updated Successfully.";
                            else
                                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured updating the child WEB3 NFT Holons. Reason: {updateWeb3NFTHolonsResult?.Message}");
                        }
                        else
                            OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured saving Web4 OASIS Geo-NFT. Reason: {saveHolonResult?.Message}");
                    }
                    else
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured loading Web4 OASIS Geo-NFT. Reason: {nftResult?.Message}");
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured loading Web4 OASIS Geo-NFT Holon. Reason: {nftHolonResult?.Message}");
                    return result;
                }
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
            }

            return result;
        }

        private async Task<OASISResult<bool>> UpdateWeb3NFTHolonsAsync(IUpdateWeb4NFTRequest request, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<bool> result = new OASISResult<bool>();
            OASISResult<IEnumerable<IWeb3NFT>> web3NFTs = await LoadAllWeb3NFTsAsync(request.Id, providerType);
            string errorMessage = "Error occured in NFTManager.UpdateWeb3NFTHolonsAsync. Reason: ";

            if (web3NFTs != null && web3NFTs.Result != null && !web3NFTs.IsError)
            {
                foreach (IWeb3NFT web3NFT in web3NFTs.Result)
                {
                    if (request.UpdateChildWebNFTIds.Contains(web3NFT.Id.ToString()))
                    {
                        IWeb3NFT updatedWeb3NFT = UpdateWeb3NFT(web3NFT, request);

                        OASISResult<IHolon> web3NftHolonResult = await Data.LoadHolonAsync(web3NFT.Id, providerType: providerType);

                        if (web3NftHolonResult != null && web3NftHolonResult.Result != null && !web3NftHolonResult.IsError)
                        {
                            OASISResult<IHolon> saveHolonResult = await Data.SaveHolonAsync(UpdateWeb3NFTMetaDataHolon(web3NftHolonResult.Result, updatedWeb3NFT, request.Id), request.ModifiedByAvatarId, providerType: providerType);

                            if (!(saveHolonResult != null && saveHolonResult.Result != null && !saveHolonResult.IsError))
                                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured saving the Web3 NFT Holon for id {updatedWeb3NFT.Id} and title '{updatedWeb3NFT.Title}'. Reason: {saveHolonResult.Message}");
                        }
                        else
                            OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured loading the Web3 OASIS NFT Holon. Reason: {web3NftHolonResult?.Message}");
                    }
                }
            }

            if (!result.IsError)
                result.Result = true;

            return result;
        }
    }
}
