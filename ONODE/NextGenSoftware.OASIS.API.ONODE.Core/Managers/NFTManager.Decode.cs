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
        private OASISResult<IWeb3NFT> DecodeNFTMetaData(OASISResult<IHolon> holonResult, OASISResult<IWeb3NFT> result, string errorMessage)
        {
            if (holonResult != null && !holonResult.IsError && holonResult.Result != null)
                result.Result = (IWeb3NFT)System.Text.Json.JsonSerializer.Deserialize(holonResult.Result.MetaData["NFT.WEB3NFT"].ToString(), typeof(Web3NFT));
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured loading holon metadata. Reason: {holonResult.Message}");

            return result;
        }

        private OASISResult<IWeb4NFT> DecodeNFTMetaData(OASISResult<IHolon> holonResult, OASISResult<IWeb4NFT> result, string errorMessage)
        {
            if (holonResult != null && !holonResult.IsError && holonResult.Result != null)
                result.Result = (IWeb4NFT)System.Text.Json.JsonSerializer.Deserialize(holonResult.Result.MetaData["NFT.WEB4NFT"].ToString(), typeof(Web4NFT));
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured loading holon metadata. Reason: {holonResult.Message}");

            return result;
        }

        private OASISResult<IWeb4GeoSpatialNFT> DecodeGeoNFTMetaData(OASISResult<IHolon> holonResult, OASISResult<IWeb4GeoSpatialNFT> result, string errorMessage)
        {
            if (holonResult != null && !holonResult.IsError && holonResult.Result != null)
                result.Result = (Web4OASISGeoSpatialNFT)System.Text.Json.JsonSerializer.Deserialize(holonResult.Result.MetaData["GEONFT.WEB4GEONFT"].ToString(), typeof(Web4OASISGeoSpatialNFT));
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured loading holon metadata. Reason: {holonResult.Message}");

            return result;
        }

        private OASISResult<IEnumerable<IWeb3NFT>> DecodeNFTMetaData(OASISResult<IEnumerable<IHolon>> holonsResult, OASISResult<IEnumerable<IWeb3NFT>> result, string errorMessage)
        {
            List<IWeb3NFT> nfts = new List<IWeb3NFT>();

            if (holonsResult != null && !holonsResult.IsError && holonsResult.Result != null)
            {
                if (holonsResult.Result.Count() > 0)
                {
                    foreach (IHolon holon in holonsResult.Result)
                        nfts.Add((IWeb3NFT)System.Text.Json.JsonSerializer.Deserialize(holon.MetaData["NFT.WEB3NFT"].ToString(), typeof(Web3NFT)));

                    result.Result = nfts;
                }
                else
                    result.Message = "No NFT's Found.";
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured loading holon metadata. Reason: {holonsResult.Message}");

            return result;
        }

        private OASISResult<IEnumerable<IWeb4NFT>> DecodeNFTMetaData(OASISResult<IEnumerable<IHolon>> holonsResult, OASISResult<IEnumerable<IWeb4NFT>> result, string errorMessage)
        {
            List<IWeb4NFT> nfts = new List<IWeb4NFT>();

            if (holonsResult != null && !holonsResult.IsError && holonsResult.Result != null)
            {
                if (holonsResult.Result.Count() > 0)
                {
                    foreach (IHolon holon in holonsResult.Result)
                        nfts.Add((IWeb4NFT)System.Text.Json.JsonSerializer.Deserialize(holon.MetaData["NFT.WEB4NFT"].ToString(), typeof(Web4NFT)));

                    result.Result = nfts;
                }
                else
                    result.Message = "No NFT's Found.";
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured loading holon metadata. Reason: {holonsResult.Message}");

            return result;
        }

        private OASISResult<IEnumerable<IWeb4GeoSpatialNFT>> DecodeGeoNFTMetaData(OASISResult<IEnumerable<IHolon>> holonsResult, OASISResult<IEnumerable<IWeb4GeoSpatialNFT>> result, string errorMessage)
        {
            List<IWeb4GeoSpatialNFT> nfts = new List<IWeb4GeoSpatialNFT>();

            if (holonsResult != null && !holonsResult.IsError && holonsResult.Result != null)
            {
                if (holonsResult.Result.Count() > 0)
                {
                    foreach (IHolon holon in holonsResult.Result)
                        nfts.Add((IWeb4GeoSpatialNFT)System.Text.Json.JsonSerializer.Deserialize(holon.MetaData["GEONFT.WEB4GEONFT"].ToString(), typeof(Web4OASISGeoSpatialNFT)));

                    result.Result = nfts;
                }
                else
                    result.Message = "No GeoNFT's Found.";
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured loading holon metadata. Reason: {holonsResult.Message}");

            return result;
        }

        private IWeb3NFT UpdateWeb3NFT(IWeb3NFT web3NFT, IUpdateWeb4NFTRequest request)
        {
            web3NFT.Title = !string.IsNullOrEmpty(request.Title) ? request.Title : web3NFT.Title;
            web3NFT.Description = !string.IsNullOrEmpty(request.Description) ? request.Description : web3NFT.Description;
            web3NFT.ModifiedByAvatarId = request.ModifiedByAvatarId != Guid.Empty ? request.ModifiedByAvatarId : web3NFT.ModifiedByAvatarId;
            web3NFT.ModifiedOn = DateTime.Now;
            web3NFT.ImageUrl = !string.IsNullOrEmpty(request.ImageUrl) ? request.ImageUrl : web3NFT.ImageUrl;
            web3NFT.Image = request.Image != null ? request.Image : web3NFT.Image;
            web3NFT.ThumbnailUrl = !string.IsNullOrEmpty(request.ThumbnailUrl) ? request.ThumbnailUrl : web3NFT.ThumbnailUrl;
            web3NFT.Thumbnail = request.Thumbnail != null ? request.Thumbnail : web3NFT.Thumbnail;
            web3NFT.MetaData = request.MetaData != null ? request.MetaData : web3NFT.MetaData;
            web3NFT.Tags = request.Tags ?? web3NFT.Tags;
            web3NFT.Price = request.Price.HasValue ? request.Price.Value : web3NFT.Price;
            web3NFT.Discount = request.Discount.HasValue ? request.Discount.Value : web3NFT.Discount;
            web3NFT.IsForSale = request.IsForSale.HasValue ? request.IsForSale.Value : web3NFT.IsForSale;
            web3NFT.SalesHistory = request.SalesHistory ?? web3NFT.SalesHistory;
            web3NFT.RoyaltyPercentage = request.RoyaltyPercentage.HasValue ? request.RoyaltyPercentage.Value : web3NFT.RoyaltyPercentage;
            web3NFT.CurrentOwnerAvatarId = request.CurrentOwnerAvatarId != Guid.Empty ? request.CurrentOwnerAvatarId : web3NFT.CurrentOwnerAvatarId;
            web3NFT.PreviousOwnerAvatarId = request.PreviousOwnerAvatarId != Guid.Empty ? request.PreviousOwnerAvatarId : web3NFT.PreviousOwnerAvatarId;
            web3NFT.LastPurchasedByAvatarId = request.LastPurchasedByAvatarId != Guid.Empty ? request.LastPurchasedByAvatarId : web3NFT.LastPurchasedByAvatarId;
            web3NFT.LastSaleAmount = request.LastSaleAmount.HasValue ? request.LastSaleAmount.Value : web3NFT.LastSaleAmount;
            web3NFT.LastSaleDate = request.LastSaleDate != DateTime.MinValue ? request.LastSaleDate : web3NFT.LastSaleDate;
            web3NFT.LastSaleDiscount = request.LastSaleDiscount.HasValue ? request.LastSaleDiscount.Value : web3NFT.LastSaleDiscount;
            web3NFT.LastSalePrice = request.LastSalePrice.HasValue ? request.LastSalePrice.Value : web3NFT.LastSalePrice;
            web3NFT.LastSaleQuantity = request.LastSaleQuantity.HasValue ? request.LastSaleQuantity.Value : web3NFT.LastSaleQuantity;
            web3NFT.LastSaleTax = request.LastSaleTax.HasValue ? request.LastSaleTax.Value : web3NFT.LastSaleTax;
            web3NFT.LastSaleTransactionHash = !string.IsNullOrEmpty(request.LastSaleTransactionHash) ? request.LastSaleTransactionHash : web3NFT.LastSaleTransactionHash;
            web3NFT.LastSoldByAvatarId = request.LastSoldByAvatarId != Guid.Empty ? request.LastSoldByAvatarId : web3NFT.LastSoldByAvatarId;
            web3NFT.RoyaltyPercentage = request.RoyaltyPercentage.HasValue ? request.RoyaltyPercentage.Value : web3NFT.RoyaltyPercentage;
            web3NFT.SaleEndDate = request.SaleEndDate.HasValue ? request.SaleEndDate.Value : web3NFT.SaleEndDate;
            web3NFT.SaleStartDate = request.SaleStartDate.HasValue ? request.SaleStartDate.Value : web3NFT.SaleStartDate;
            web3NFT.TotalNumberOfSales = request.TotalNumberOfSales.HasValue ? request.TotalNumberOfSales.Value : web3NFT.TotalNumberOfSales;

            return web3NFT;
        }

        //TODO: Lots more coming soon! ;-)
    }
}