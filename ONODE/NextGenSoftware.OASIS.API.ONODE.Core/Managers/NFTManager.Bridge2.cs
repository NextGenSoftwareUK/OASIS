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
        private string CreateMetaplexJson(IMintWeb4NFTRequest request)
        {
            var metadata = new
            {
                name = request.Title,
                symbol = request.Symbol,
                description = request.Description,
                seller_fee_basis_points = 500,
                image = request.ImageUrl,
                thumbnail = request.ThumbnailUrl,
                attributes = request.MetaData != null ? request.MetaData : new Dictionary<string, string>(),
                price = request.Price,
                discount = request.Discount,
                memo = request.MemoText
            };

            return System.Text.Json.JsonSerializer.Serialize(metadata, new JsonSerializerOptions { WriteIndented = true });
        }

        private string CreateERC721Json(IMintWeb4NFTRequest request)
        {
            var metadata = new
            {
                title = request.Title,
                description = request.Description,
                image = request.ImageUrl,
                thumbnail = request.ThumbnailUrl,
                attributes = request.MetaData != null ? request.MetaData : new Dictionary<string, string>(),
                price = request.Price,
                discount = request.Discount,
                memo = request.MemoText
            };

            return System.Text.Json.JsonSerializer.Serialize(metadata, new JsonSerializerOptions { WriteIndented = true });
        }

        private string CreateERC1155Json(IMintWeb4NFTRequest request)
        {
            var metadata = new
            {
                title = request.Title,
                description = request.Description,
                image = request.ImageUrl,
                thumbnail = request.ThumbnailUrl,
                copies = request.NumberToMint,
                attributes = request.MetaData != null ? request.MetaData : new Dictionary<string, string>(),
                price = request.Price,
                discount = request.Discount,
                memo = request.MemoText
            };

            return System.Text.Json.JsonSerializer.Serialize(metadata, new JsonSerializerOptions { WriteIndented = true });
        }

        private IWeb3NFT UpdateWeb3NFT(IWeb3NFT web3NFT, IMintWeb3NFTRequest request)
        {
            if (web3NFT.Id == Guid.Empty)
                web3NFT.Id = Guid.NewGuid();

            web3NFT.MintedByAvatarId = request.MintedByAvatarId;
            web3NFT.SendToAddressAfterMinting = request.SendToAddressAfterMinting;
            web3NFT.SendToAvatarAfterMintingId = request.SendToAvatarAfterMintingId;
            web3NFT.SendToAvatarAfterMintingUsername = request.SendToAvatarAfterMintingUsername;
            web3NFT.Title = request.Title;
            web3NFT.Description = request.Description;
            web3NFT.Price = request.Price.Value;
            web3NFT.Discount = request.Discount.Value;
            web3NFT.RoyaltyPercentage = request.RoyaltyPercentage.HasValue ? request.RoyaltyPercentage.Value : 0;
            web3NFT.Image = request.Image;
            web3NFT.ImageUrl = request.ImageUrl;
            web3NFT.Thumbnail = request.Thumbnail;
            web3NFT.ThumbnailUrl = request.ThumbnailUrl;
            web3NFT.OnChainProvider = new EnumValue<ProviderType>(request.OnChainProvider.Value);
            web3NFT.OffChainProvider = new EnumValue<ProviderType>(request.OffChainProvider.Value);
            web3NFT.StoreNFTMetaDataOnChain = request.StoreNFTMetaDataOnChain.HasValue ? request.StoreNFTMetaDataOnChain.Value : false;
            web3NFT.NFTOffChainMetaType = new EnumValue<NFTOffChainMetaType>(request.NFTOffChainMetaType.Value);
            web3NFT.NFTStandardType = new EnumValue<NFTStandardType>(request.NFTStandardType.Value);
            web3NFT.Symbol = request.Symbol;
            web3NFT.MintedOn = DateTime.Now;
            web3NFT.MemoText = request.MemoText;
            web3NFT.JSONMetaDataURL = request.JSONMetaDataURL;
            web3NFT.IsForSale = request.IsForSale.HasValue ? request.IsForSale.Value : false;
            web3NFT.SaleStartDate = request.SaleStartDate;
            web3NFT.SaleEndDate = request.SaleEndDate;
            web3NFT.Tags = request.Tags;
            web3NFT.MetaData = request.MetaData;
            //web3NFT.MetaData["{{{newnft}}}"] = "true";

            return web3NFT;
        }

        private Web4NFT CreateWeb4NFT(IMintWeb4NFTRequest request)
        {
            return new Web4NFT()
            {
                Id = Guid.NewGuid(),
                MetaData = request.MetaData,
                Tags = request.Tags,
                CollectionPublicKey = request.CollectionPublicKey,
                MintedByAvatarId = request.MintedByAvatarId,
                SendToAddressAfterMinting = request.SendToAddressAfterMinting,
                SendToAvatarAfterMintingId = request.SendToAvatarAfterMintingId,
                SendToAvatarAfterMintingUsername = request.SendToAvatarAfterMintingUsername,
                Title = request.Title,
                Description = request.Description,
                Price = request.Price,
                Discount = request.Discount,
                RoyaltyPercentage = request.RoyaltyPercentage.HasValue ? request.RoyaltyPercentage.Value : 0,
                Image = request.Image,
                ImageUrl = request.ImageUrl,
                Thumbnail = request.Thumbnail,
                ThumbnailUrl = request.ThumbnailUrl,
                OnChainProvider = request.OnChainProvider,
                OffChainProvider = request.OffChainProvider,
                StoreNFTMetaDataOnChain = request.StoreNFTMetaDataOnChain,
                NFTOffChainMetaType = request.NFTOffChainMetaType,
                NFTStandardType = request.NFTStandardType,
                Symbol = request.Symbol,
                MintedOn = DateTime.Now,
                MemoText = request.MemoText,
                JSONMetaDataURL = request.JSONMetaDataURL,
                IsForSale = request.IsForSale.HasValue ? request.IsForSale.Value : false,
                SaleStartDate = request.SaleStartDate,
                SaleEndDate = request.SaleEndDate
            };
        }

        private Web4NFT CreateWeb4NFT(IImportWeb3NFTRequest request)
        {
            return new Web4NFT()
            {
                Id = Guid.NewGuid(),
                MetaData = request.MetaData,
                Tags = request.Tags,
                ImportedByAvatarId = request.ImportedByAvatarId,
                ImportedOn = DateTime.Now,
                Title = request.Title,
                Description = request.Description,
                Price = request.Price,
                Discount = request.Discount,
                RoyaltyPercentage = request.RoyaltyPercentage,
                Image = request.Image,
                ImageUrl = request.ImageUrl,
                Thumbnail = request.Thumbnail,
                ThumbnailUrl = request.ThumbnailUrl,
                OnChainProvider = request.OnChainProvider,
                OffChainProvider = request.OffChainProvider,
                StoreNFTMetaDataOnChain = request.StoreNFTMetaDataOnChain,
                NFTOffChainMetaType = request.NFTOffChainMetaType,
                NFTStandardType = request.NFTStandardType,
                Symbol = request.Symbol,
                MemoText = request.MemoText,
                JSONMetaDataURL = request.JSONMetaDataURL,
                IsForSale = request.IsForSale.Value,
                SaleStartDate = request.SaleStartDate,
                SaleEndDate = request.SaleEndDate,
                Web3NFTs = new List<Web3NFT>() { new Web3NFT()
                {
                    MintTransactionHash = request.MintTransactionHash,
                    NFTMintedUsingWalletAddress = request.NFTMintedUsingWalletAddress,
                    UpdateAuthority = request.UpdateAuthority,
                    NFTTokenAddress = request.NFTTokenAddress
                } }
            };
        }

        private Web4OASISGeoSpatialNFT CreateWeb4GeoSpatialNFT(IPlaceWeb4GeoSpatialNFTRequest request, IWeb4NFT originalNftMetaData)
        {
            return new Web4OASISGeoSpatialNFT()
            {
                Id = Guid.NewGuid(),  //The NFT could be placed many times so we need a new ID for each time
                OriginalWeb4OASISNFTId = request.OriginalWeb4OASISNFTId, //We need to link back to the orignal NFT (but we copy across the NFT properties making it quicker and easier to get at the data). TODO: Do we want to copy the data across? Pros and Cons? Need to think about this... for now it's fine... ;-)
                GeoNFTMetaDataProvider = request.GeoNFTMetaDataProvider,
                JSONMetaDataURL = originalNftMetaData.JSONMetaDataURL,
                MintedByAvatarId = originalNftMetaData.MintedByAvatarId,
                SendToAddressAfterMinting = originalNftMetaData.SendToAddressAfterMinting,
                SendToAvatarAfterMintingId = originalNftMetaData.SendToAvatarAfterMintingId,
                SendToAvatarAfterMintingUsername = originalNftMetaData.SendToAvatarAfterMintingUsername,
                SellerFeeBasisPoints = originalNftMetaData.SellerFeeBasisPoints,
                Title = originalNftMetaData.Title,
                Description = originalNftMetaData.Description,
                Price = originalNftMetaData.Price,
                Discount = originalNftMetaData.Discount,
                RoyaltyPercentage = originalNftMetaData.RoyaltyPercentage,
                IsForSale = originalNftMetaData.IsForSale,
                SaleStartDate = originalNftMetaData.SaleStartDate,
                SaleEndDate = originalNftMetaData.SaleEndDate,
                Image = originalNftMetaData.Image,
                ImageUrl = originalNftMetaData.ImageUrl,
                Thumbnail = originalNftMetaData.Thumbnail,
                ThumbnailUrl = originalNftMetaData.ThumbnailUrl,
                MetaData = originalNftMetaData.MetaData,
                Tags = originalNftMetaData.Tags,
                OnChainProvider = originalNftMetaData.OnChainProvider,
                OffChainProvider = originalNftMetaData.OffChainProvider,
                StoreNFTMetaDataOnChain = originalNftMetaData.StoreNFTMetaDataOnChain,
                NFTOffChainMetaType = originalNftMetaData.NFTOffChainMetaType,
                NFTStandardType = originalNftMetaData.NFTStandardType,
                Symbol = originalNftMetaData.Symbol,
                MintedOn = originalNftMetaData.MintedOn,
                MemoText = originalNftMetaData.MemoText,
                PlacedByAvatarId = request.PlacedByAvatarId,
                Lat = request.Lat,
                Long = request.Long,
                PermSpawn = request.PermSpawn,
                PlayerSpawnQuantity = request.PlayerSpawnQuantity,
                AllowOtherPlayersToAlsoCollect = request.AllowOtherPlayersToAlsoCollect,
                GlobalSpawnQuantity = request.GlobalSpawnQuantity,
                RespawnDurationInSeconds = request.RespawnDurationInSeconds,
                PlacedOn = DateTime.Now,
                Nft2DSprite = request.Nft2DSprite,
                Nft3DObject = request.Nft3DObject,
                Nft3DObjectURI = request.Nft3DObjectURI,
                Nft2DSpriteURI = request.Nft2DSpriteURI,
                Web3NFTs = originalNftMetaData.Web3NFTs
            };
        }

        private IHolon CreateWeb4NFTMetaDataHolon(IWeb4NFT nftMetaData, IImportWeb3NFTRequest request)
        {
            IHolon holonNFT = new Holon(HolonType.Web4NFT);
            holonNFT.Id = nftMetaData.Id;
            holonNFT.Name = $"{nftMetaData.OnChainProvider.Name} WEB3 NFT Imported OnTo The OASIS with title {nftMetaData.Title}";
            holonNFT.Description = request.Description;
            holonNFT.MetaData["NFT.OASISNFT"] = System.Text.Json.JsonSerializer.Serialize(nftMetaData); //TODO: May remove this because its duplicated data. BUT we may need this for other purposes later such as exporting it to a file etc (but then we could just serialaize it there and then).
            holonNFT.MetaData["NFT.MintTransactionHash"] = request.MintTransactionHash;
            holonNFT.MetaData["NFT.Id"] = nftMetaData.Id;
            holonNFT.MetaData["NFT.MemoText"] = nftMetaData.MemoText;
            holonNFT.MetaData["NFT.Title"] = nftMetaData.Title;
            holonNFT.MetaData["NFT.Description"] = nftMetaData.Description;
            holonNFT.MetaData["NFT.Price"] = request.Price.ToString();
            holonNFT.MetaData["NFT.RoyaltyPercentage"] = nftMetaData.RoyaltyPercentage.ToString();
            holonNFT.MetaData["NFT.IsForSale"] = nftMetaData.IsForSale == true ? "Yes" : "No";
            holonNFT.MetaData["NFT.SaleStartDate"] = nftMetaData.SaleStartDate.HasValue ? nftMetaData.SaleStartDate.Value.ToShortDateString() : null;
            holonNFT.MetaData["NFT.SaleEndDate"] = nftMetaData.SaleEndDate.HasValue ? nftMetaData.SaleEndDate.Value.ToShortDateString() : null;
            holonNFT.MetaData["NFT.Discount"] = request.Discount.ToString();
            holonNFT.MetaData["NFT.OnChainProvider"] = nftMetaData.OnChainProvider.Name;
            holonNFT.MetaData["NFT.OffChainProvider"] = nftMetaData.OffChainProvider.Name;
            holonNFT.MetaData["NFT.StoreNFTMetaDataOnChain"] = request.StoreNFTMetaDataOnChain ? "True" : "False";
            holonNFT.MetaData["NFT.NFTOffChainMetaType"] = nftMetaData.NFTOffChainMetaType.Name;
            holonNFT.MetaData["NFT.NFTStandardType"] = request.NFTStandardType.Name;
            holonNFT.MetaData["NFT.Symbol"] = request.Symbol;
            holonNFT.MetaData["NFT.Image"] = request.Image;
            holonNFT.MetaData["NFT.ImageUrl"] = request.ImageUrl;
            holonNFT.MetaData["NFT.Thumbnail"] = request.Thumbnail;
            holonNFT.MetaData["NFT.ThumbnailUrl"] = request.ThumbnailUrl;
            holonNFT.MetaData["NFT.JSONMetaDataURL"] = request.JSONMetaDataURL;
            holonNFT.MetaData["NFT.SellerFeeBasisPoints"] = nftMetaData.SellerFeeBasisPoints;
            holonNFT.MetaData["NFT.MetaData"] = System.Text.Json.JsonSerializer.Serialize(request.MetaData);
            holonNFT.MetaData["NFT.Tags"] = System.Text.Json.JsonSerializer.Serialize(request.Tags);
            holonNFT.MetaData["NFT.ImportedByAvatarId"] = request.ImportedByAvatarId.ToString();
            holonNFT.MetaData["NFT.ImportedOn"] = DateTime.Now;
            holonNFT.ParentHolonId = nftMetaData.ImportedByAvatarId;

            if (nftMetaData.Web3NFTs.Count > 0)
            {
                holonNFT.MetaData["NFT.UpdateAuthority"] = nftMetaData.Web3NFTs[0].UpdateAuthority;
                holonNFT.MetaData["NFT.NFTMintedUsingWalletAddress"] = nftMetaData.Web3NFTs[0].NFTMintedUsingWalletAddress;
                holonNFT.MetaData["NFT.NFTTokenAddress"] = nftMetaData.Web3NFTs[0].NFTTokenAddress;
            }

            return holonNFT;
        }

        private IHolon CreateWeb4NFTMetaDataHolon(IWeb4NFT nftMetaData, IMintWeb4NFTRequest request = null)
        {
            return UpdateWeb4NFTMetaDataHolon(new Holon(HolonType.Web4NFT), nftMetaData, request);
        }

        private IHolon UpdateWeb4NFTMetaDataHolon(IHolon holonNFT, IWeb4NFT nftMetaData, IMintWeb4NFTRequest request = null)
        {
            holonNFT.Id = nftMetaData.Id;
            holonNFT.Name = $"{nftMetaData.OnChainProvider.Name} WEB4 NFT Minted On The OASIS with title {nftMetaData.Title}";
            holonNFT.Description = nftMetaData.MemoText;
            holonNFT.MetaData["NFT.WEB4NFT"] = System.Text.Json.JsonSerializer.Serialize(nftMetaData);
            holonNFT.MetaData["NFT.CollectionPublicKey"] = nftMetaData.CollectionPublicKey;
            //holonNFT.MetaData["NFT.VerifyCollectionTransactionHash"] = nftMetaData.VerifyCollectionTransactionHash;
            holonNFT.MetaData["NFT.Id"] = nftMetaData.Id;
            holonNFT.MetaData["NFT.MintedByAvatarId"] = nftMetaData.MintedByAvatarId.ToString();
            holonNFT.MetaData["NFT.SendToAvatarAfterMintingId"] = nftMetaData.SendToAvatarAfterMintingId.ToString();
            holonNFT.MetaData["NFT.SendToAvatarAfterMintingUsername"] = nftMetaData.SendToAvatarAfterMintingUsername;
            holonNFT.MetaData["NFT.SendToAddressAfterMinting"] = nftMetaData.SendToAddressAfterMinting;
            holonNFT.MetaData["NFT.MemoText"] = nftMetaData.MemoText;
            holonNFT.MetaData["NFT.Title"] = nftMetaData.Title;
            holonNFT.MetaData["NFT.Description"] = nftMetaData.Description;
            holonNFT.MetaData["NFT.Price"] = nftMetaData.Price.ToString();
            holonNFT.MetaData["NFT.Discount"] = nftMetaData.Discount.ToString();
            holonNFT.MetaData["NFT.RoyaltyPercentage"] = nftMetaData.RoyaltyPercentage.ToString();
            holonNFT.MetaData["NFT.IsForSale"] = nftMetaData.IsForSale == true ? "Yes" : "No";
            holonNFT.MetaData["NFT.SaleStartDate"] = nftMetaData.SaleStartDate.HasValue ? nftMetaData.SaleStartDate.Value.ToShortDateString() : null;
            holonNFT.MetaData["NFT.SaleEndDate"] = nftMetaData.SaleEndDate.HasValue ? nftMetaData.SaleEndDate.Value.ToShortDateString() : null;
            holonNFT.MetaData["NFT.NumberToMint"] = request != null ? request.NumberToMint.ToString() : "";
            holonNFT.MetaData["NFT.OnChainProvider"] = nftMetaData.OnChainProvider.Name;
            holonNFT.MetaData["NFT.OffChainProvider"] = nftMetaData.OffChainProvider.Name;
            holonNFT.MetaData["NFT.StoreNFTMetaDataOnChain"] = nftMetaData.StoreNFTMetaDataOnChain ? "True" : "False";
            holonNFT.MetaData["NFT.NFTOffChainMetaType"] = nftMetaData.NFTOffChainMetaType.Name;
            holonNFT.MetaData["NFT.NFTStandardType"] = nftMetaData.NFTStandardType.Name;
            holonNFT.MetaData["NFT.Symbol"] = nftMetaData.Symbol;
            holonNFT.MetaData["NFT.Image"] = nftMetaData.Image;
            holonNFT.MetaData["NFT.ImageUrl"] = nftMetaData.ImageUrl;
            holonNFT.MetaData["NFT.Thumbnail"] = nftMetaData.Thumbnail;
            holonNFT.MetaData["NFT.ThumbnailUrl"] = nftMetaData.ThumbnailUrl;
            holonNFT.MetaData["NFT.JSONMetaDataURL"] = nftMetaData.JSONMetaDataURL;
            holonNFT.MetaData["NFT.JSONMetaDataURLHolonId"] = nftMetaData.JSONMetaDataURLHolonId;
            holonNFT.MetaData["NFT.MintedOn"] = nftMetaData.MintedOn.ToShortDateString();
            holonNFT.MetaData["NFT.SellerFeeBasisPoints"] = nftMetaData.SellerFeeBasisPoints;
            holonNFT.MetaData["NFT.Tags"] = System.Text.Json.JsonSerializer.Serialize(nftMetaData.Tags);
            holonNFT.MetaData["NFT.MetaData"] = System.Text.Json.JsonSerializer.Serialize(nftMetaData.MetaData);
            //holonNFT.MetaData["NFT.MetaData"] = nftMetaData.MetaData; //TODO: Currently the line above works fine for normal metaData but for objects such as file uploads then it causes issues displaying the meta because it is displayed/stored as a string so there is no way to know if its a binary file.
            holonNFT.ParentHolonId = nftMetaData.MintedByAvatarId;

            //TODO: Not even sure if we need to record this anymore? Because these are not stored at the web4 level anymore, only at the web3 level.
            //if (nftMetaData.Web3NFTs.Count > 0)
            //{
            //    holonNFT.MetaData["NFT.MintTransactionHash"] = nftMetaData.Web3NFTs[0].MintTransactionHash;
            //    holonNFT.MetaData["NFT.OASISMintWalletAddress"] = nftMetaData.Web3NFTs[0].OASISMintWalletAddress;
            //    holonNFT.MetaData["NFT.SendNFTTransactionHash"] = nftMetaData.Web3NFTs[0].SendNFTTransactionHash;
            //    holonNFT.MetaData["NFT.NFTTokenAddress"] = nftMetaData.Web3NFTs[0].NFTTokenAddress;
            //    holonNFT.MetaData["NFT.UpdateAuthority"] = nftMetaData.Web3NFTs[0].UpdateAuthority;
            //}

            return holonNFT;
        }

        private IHolon CreateWeb3NFTMetaDataHolon(IWeb3NFT nftMetaData, Guid parentWeb4NFTId, IMintWeb3NFTRequest request = null)
        {
            return UpdateWeb3NFTMetaDataHolon(new Holon(HolonType.Web3NFT), nftMetaData, parentWeb4NFTId, request);
        }

        private IHolon UpdateWeb3NFTMetaDataHolon(IHolon holonNFT, IWeb3NFT nftMetaData, Guid parentWeb4NFTId, IMintWeb3NFTRequest request = null)
        {
            holonNFT.Id = nftMetaData.Id;
            holonNFT.Name = $"{nftMetaData.OnChainProvider.Name} WEB3 NFT Minted On The OASIS with title {nftMetaData.Title}";
            holonNFT.Description = nftMetaData.MemoText;
            holonNFT.MetaData["NFT.WEB3NFT"] = System.Text.Json.JsonSerializer.Serialize(nftMetaData);
            holonNFT.MetaData["NFT.Id"] = nftMetaData.Id;
            holonNFT.MetaData["NFT.CollectionPublicKey"] = nftMetaData.CollectionPublicKey;
            holonNFT.MetaData["NFT.VerifyCollectionTransactionHash"] = nftMetaData.VerifyCollectionTransactionHash;
            holonNFT.MetaData["NFT.ParentWeb4NFTId"] = parentWeb4NFTId.ToString();
            holonNFT.MetaData["NFT.MintedByAvatarId"] = nftMetaData.MintedByAvatarId.ToString();
            holonNFT.MetaData["NFT.SendToAvatarAfterMintingId"] = nftMetaData.SendToAvatarAfterMintingId.ToString();
            holonNFT.MetaData["NFT.SendToAvatarAfterMintingUsername"] = nftMetaData.SendToAvatarAfterMintingUsername;
            holonNFT.MetaData["NFT.SendToAddressAfterMinting"] = nftMetaData.SendToAddressAfterMinting;
            holonNFT.MetaData["NFT.MemoText"] = nftMetaData.MemoText;
            holonNFT.MetaData["NFT.Title"] = nftMetaData.Title;
            holonNFT.MetaData["NFT.Description"] = nftMetaData.Description;
            holonNFT.MetaData["NFT.Price"] = nftMetaData.Price.ToString();
            holonNFT.MetaData["NFT.Discount"] = nftMetaData.Discount.ToString();
            holonNFT.MetaData["NFT.RoyaltyPercentage"] = nftMetaData.RoyaltyPercentage.ToString();
            holonNFT.MetaData["NFT.IsForSale"] = nftMetaData.IsForSale == true ? "Yes" : "No";
            holonNFT.MetaData["NFT.SaleStartDate"] = nftMetaData.SaleStartDate.HasValue ? nftMetaData.SaleStartDate.Value.ToShortDateString() : null;
            holonNFT.MetaData["NFT.SaleEndDate"] = nftMetaData.SaleEndDate.HasValue ? nftMetaData.SaleEndDate.Value.ToShortDateString() : null;
            holonNFT.MetaData["NFT.NumberToMint"] = request != null ? request.NumberToMint.ToString() : "";
            holonNFT.MetaData["NFT.OnChainProvider"] = nftMetaData.OnChainProvider.Name;
            holonNFT.MetaData["NFT.OffChainProvider"] = nftMetaData.OffChainProvider.Name;
            holonNFT.MetaData["NFT.StoreNFTMetaDataOnChain"] = nftMetaData.StoreNFTMetaDataOnChain ? "True" : "False";
            holonNFT.MetaData["NFT.NFTOffChainMetaType"] = nftMetaData.NFTOffChainMetaType.Name;
            holonNFT.MetaData["NFT.NFTStandardType"] = nftMetaData.NFTStandardType.Name;
            holonNFT.MetaData["NFT.Symbol"] = nftMetaData.Symbol;
            holonNFT.MetaData["NFT.Image"] = nftMetaData.Image;
            holonNFT.MetaData["NFT.ImageUrl"] = nftMetaData.ImageUrl;
            holonNFT.MetaData["NFT.Thumbnail"] = nftMetaData.Thumbnail;
            holonNFT.MetaData["NFT.ThumbnailUrl"] = nftMetaData.ThumbnailUrl;
            holonNFT.MetaData["NFT.JSONMetaDataURL"] = nftMetaData.JSONMetaDataURL;
            holonNFT.MetaData["NFT.JSONMetaDataURLHolonId"] = nftMetaData.JSONMetaDataURLHolonId;
            holonNFT.MetaData["NFT.MintedOn"] = nftMetaData.MintedOn.ToShortDateString();
            holonNFT.MetaData["NFT.SellerFeeBasisPoints"] = nftMetaData.SellerFeeBasisPoints;
            holonNFT.MetaData["NFT.Tags"] = System.Text.Json.JsonSerializer.Serialize(nftMetaData.Tags);
            holonNFT.MetaData["NFT.MetaData"] = System.Text.Json.JsonSerializer.Serialize(nftMetaData.MetaData);
            //holonNFT.MetaData["NFT.MetaData"] = nftMetaData.MetaData; //TODO: Currently the line above works fine for normal metaData but for objects such as file uploads then it causes issues displaying the meta because it is displayed/stored as a string so there is no way to know if its a binary file.
            holonNFT.ParentHolonId = nftMetaData.MintedByAvatarId;

            return holonNFT;
        }

        private IHolon CreateWeb4GeoSpatialNFTMetaDataHolon(IWeb4GeoSpatialNFT geoNFTMetaData, IMintWeb4NFTRequest request = null)
        {
            return UpdateWeb4GeoNFTMetaDataHolon(new Holon(HolonType.Web4GeoNFT), geoNFTMetaData, request);
        }

        private IHolon UpdateWeb4GeoNFTMetaDataHolon(IHolon holonNFT, IWeb4GeoSpatialNFT geoNFTMetaData, IMintWeb4NFTRequest request = null)
        {
            holonNFT.Id = geoNFTMetaData.Id;
            holonNFT.Name = "WEB4 OASIS GEO NFT";
            holonNFT.Description = "WEB4 OASIS GEO NFT";
            holonNFT.MetaData["GEONFT.WEB4GEONFT"] = System.Text.Json.JsonSerializer.Serialize(geoNFTMetaData); //TODO: May remove this because its duplicated data.
            holonNFT.MetaData["GEONFT.Id"] = geoNFTMetaData.Id;
            holonNFT.MetaData["GEONFT.GeoNFTMetaDataProvider"] = geoNFTMetaData.GeoNFTMetaDataProvider.Name;
            holonNFT.MetaData["GEONFT.PlacedByAvatarId"] = geoNFTMetaData.PlacedByAvatarId.ToString();
            holonNFT.MetaData["GEONFT.PlacedOn"] = geoNFTMetaData.PlacedOn.ToShortDateString();
            holonNFT.MetaData["GEONFT.Lat"] = geoNFTMetaData.Lat;
            holonNFT.MetaData["GEONFT.Long"] = geoNFTMetaData.Long;
            holonNFT.MetaData["GEONFT.LatLong"] = string.Concat(geoNFTMetaData.Lat, ":", geoNFTMetaData.Long);
            holonNFT.MetaData["GEONFT.PermSpawn"] = geoNFTMetaData.PermSpawn;
            holonNFT.MetaData["GEONFT.PlayerSpawnQuantity"] = geoNFTMetaData.PlayerSpawnQuantity;
            holonNFT.MetaData["GEONFT.AllowOtherPlayersToAlsoCollect"] = geoNFTMetaData.AllowOtherPlayersToAlsoCollect;
            holonNFT.MetaData["GEONFT.GlobalSpawnQuantity"] = geoNFTMetaData.GlobalSpawnQuantity;
            holonNFT.MetaData["GEONFT.RespawnDurationInSeconds"] = geoNFTMetaData.RespawnDurationInSeconds;
            holonNFT.MetaData["GEONFT.Nft2DSprite"] = geoNFTMetaData.Nft2DSprite;
            holonNFT.MetaData["GEONFT.Nft2DSpriteURI"] = geoNFTMetaData.Nft2DSpriteURI;
            holonNFT.MetaData["GEONFT.Nft3DObject"] = geoNFTMetaData.Nft3DObject;
            holonNFT.MetaData["GEONFT.Nft3DObjectURI"] = geoNFTMetaData.Nft3DObjectURI;
            holonNFT.MetaData["GEONFT.OriginalOASISNFT.Id"] = geoNFTMetaData.OriginalWeb4OASISNFTId;
            holonNFT.MetaData["GEONFT.OriginalOASISNFT.MemoText"] = geoNFTMetaData.MemoText;
            holonNFT.MetaData["GEONFT.OriginalOASISNFT.Title"] = geoNFTMetaData.Title;
            holonNFT.MetaData["GEONFT.OriginalOASISNFT.Description"] = geoNFTMetaData.Description;
            holonNFT.MetaData["GEONFT.OriginalOASISNFT.MintedByAvatarId"] = geoNFTMetaData.MintedByAvatarId.ToString();
            holonNFT.MetaData["GEONFT.OriginalOASISNFT.SendToAvatarAfterMintingId"] = geoNFTMetaData.SendToAvatarAfterMintingId.ToString();
            holonNFT.MetaData["GEONFT.OriginalOASISNFT.SendToAvatarAfterMintingUsername"] = geoNFTMetaData.SendToAvatarAfterMintingUsername;
            holonNFT.MetaData["GEONFT.OriginalOASISNFT.SendToAddressAfterMinting"] = geoNFTMetaData.SendToAddressAfterMinting;
            holonNFT.MetaData["GEONFT.OriginalOASISNFT.Price"] = geoNFTMetaData.Price.ToString();
            holonNFT.MetaData["GEONFT.OriginalOASISNFT.Discount"] = geoNFTMetaData.Discount.ToString();
            holonNFT.MetaData["GEONFT.OriginalOASISNFT.RoyaltyPercentage"] = geoNFTMetaData.RoyaltyPercentage.ToString();
            holonNFT.MetaData["GEONFT.OriginalOASISNFT.IsForSale"] = geoNFTMetaData.IsForSale == true ? "Yes" : "No";
            holonNFT.MetaData["GEONFT.OriginalOASISNFT.SaleStartDate"] = geoNFTMetaData.SaleStartDate.HasValue ? geoNFTMetaData.SaleStartDate.Value.ToShortDateString() : null;
            holonNFT.MetaData["GEONFT.OriginalOASISNFT.SaleEndDate"] = geoNFTMetaData.SaleEndDate.HasValue ? geoNFTMetaData.SaleEndDate.Value.ToShortDateString() : null;
            holonNFT.MetaData["GEONFT.OriginalOASISNFT.OnChainProvider"] = geoNFTMetaData.OnChainProvider.Name;
            holonNFT.MetaData["GEONFT.OriginalOASISNFT.OffChainProvider"] = geoNFTMetaData.OffChainProvider.Name;
            holonNFT.MetaData["GEONFT.OriginalOASISNFT.StoreNFTMetaDataOnChain"] = geoNFTMetaData.StoreNFTMetaDataOnChain ? "True" : "False";
            holonNFT.MetaData["GEONFT.OriginalOASISNFT.NFTOffChainMetaType"] = geoNFTMetaData.NFTOffChainMetaType.Name;
            holonNFT.MetaData["GEONFT.OriginalOASISNFT.NFTStandardType"] = geoNFTMetaData.NFTStandardType.Name;
            holonNFT.MetaData["GEONFT.OriginalOASISNFT.Symbol"] = geoNFTMetaData.Symbol;
            holonNFT.MetaData["GEONFT.OriginalOASISNFT.Image"] = geoNFTMetaData.Image;
            holonNFT.MetaData["GEONFT.OriginalOASISNFT.ImageUrl"] = geoNFTMetaData.ImageUrl;
            holonNFT.MetaData["GEONFT.OriginalOASISNFT.Thumbnail"] = geoNFTMetaData.Thumbnail;
            holonNFT.MetaData["GEONFT.OriginalOASISNFT.ThumbnailUrl"] = geoNFTMetaData.ThumbnailUrl;
            holonNFT.MetaData["GEONFT.OriginalOASISNFT.JSONMetaDataURL"] = geoNFTMetaData.JSONMetaDataURL;
            holonNFT.MetaData["GEONFT.OriginalOASISNFT.JSONMetaDataURLHolonId"] = geoNFTMetaData.JSONMetaDataURLHolonId;
            holonNFT.MetaData["GEONFT.OriginalOASISNFT.MintedOn"] = geoNFTMetaData.MintedOn.ToShortDateString();
            holonNFT.MetaData["GEONFT.OriginalOASISNFT.SellerFeeBasisPoints"] = geoNFTMetaData.SellerFeeBasisPoints;
            holonNFT.MetaData["GEONFT.OriginalOASISNFT.MetaData"] = geoNFTMetaData.MetaData;
            holonNFT.MetaData["GEONFT.OriginalOASISNFT.Tags"] = geoNFTMetaData.Tags;

            //TODO: Not even sure if we need to record this anymore? Because these are not stored at the web4 level anymore, only at the web3 level.
            //if (geoNFTMetaData.Web3NFTs.Count > 0)
            //{
            //    holonNFT.MetaData["NFT.MintTransactionHash"] = geoNFTMetaData.Web3NFTs[0].MintTransactionHash;
            //    holonNFT.MetaData["NFT.OASISMintWalletAddress"] = geoNFTMetaData.Web3NFTs[0].OASISMintWalletAddress;
            //    holonNFT.MetaData["NFT.SendNFTTransactionHash"] = geoNFTMetaData.Web3NFTs[0].SendNFTTransactionHash;
            //    holonNFT.MetaData["NFT.NFTTokenAddress"] = geoNFTMetaData.Web3NFTs[0].NFTTokenAddress;
            //    holonNFT.MetaData["NFT.UpdateAuthority"] = geoNFTMetaData.Web3NFTs[0].UpdateAuthority;
            //}

            return holonNFT;
        }

        private IMintWeb4NFTRequest CreateMintWeb4NFTTransactionRequest(IMintAndPlaceWeb4GeoSpatialNFTRequest mintAndPlaceGeoSpatialNFTRequest)
        {
            return new MintWeb4NFTRequest()
            {
                //MintWalletAddress = mintAndPlaceGeoSpatialNFTRequest.MintWalletAddress,
                MintedByAvatarId = mintAndPlaceGeoSpatialNFTRequest.MintedByAvatarId,
                Title = mintAndPlaceGeoSpatialNFTRequest.Title,
                Description = mintAndPlaceGeoSpatialNFTRequest.Description,
                Image = mintAndPlaceGeoSpatialNFTRequest.Image,
                ImageUrl = mintAndPlaceGeoSpatialNFTRequest.ImageUrl,
                Thumbnail = mintAndPlaceGeoSpatialNFTRequest.Thumbnail,
                ThumbnailUrl = mintAndPlaceGeoSpatialNFTRequest.ThumbnailUrl,
                Price = mintAndPlaceGeoSpatialNFTRequest.Price,
                Discount = mintAndPlaceGeoSpatialNFTRequest.Discount,
                RoyaltyPercentage = mintAndPlaceGeoSpatialNFTRequest.RoyaltyPercentage,
                IsForSale = mintAndPlaceGeoSpatialNFTRequest.IsForSale,
                SaleStartDate = mintAndPlaceGeoSpatialNFTRequest.SaleStartDate,
                SaleEndDate = mintAndPlaceGeoSpatialNFTRequest.SaleEndDate,
                MemoText = mintAndPlaceGeoSpatialNFTRequest.MemoText,
                NumberToMint = mintAndPlaceGeoSpatialNFTRequest.NumberToMint,
                MetaData = mintAndPlaceGeoSpatialNFTRequest.MetaData,
                Tags = mintAndPlaceGeoSpatialNFTRequest.Tags,
                OffChainProvider = mintAndPlaceGeoSpatialNFTRequest.OffChainProvider,
                OnChainProvider = mintAndPlaceGeoSpatialNFTRequest.OnChainProvider,
                JSONMetaDataURL = mintAndPlaceGeoSpatialNFTRequest.JSONMetaDataURL,
                NFTOffChainMetaType = mintAndPlaceGeoSpatialNFTRequest.NFTOffChainMetaType,
                NFTStandardType = mintAndPlaceGeoSpatialNFTRequest.NFTStandardType,
                SendToAddressAfterMinting = mintAndPlaceGeoSpatialNFTRequest.SendToAddressAfterMinting,
                SendToAvatarAfterMintingId = mintAndPlaceGeoSpatialNFTRequest.SendToAvatarAfterMintingId,
                SendToAvatarAfterMintingUsername = mintAndPlaceGeoSpatialNFTRequest.SendToAvatarAfterMintingUsername,
                StoreNFTMetaDataOnChain = mintAndPlaceGeoSpatialNFTRequest.StoreNFTMetaDataOnChain,
                Symbol = mintAndPlaceGeoSpatialNFTRequest.Symbol,
                AttemptToMintEveryXSeconds = mintAndPlaceGeoSpatialNFTRequest.AttemptToMintEveryXSeconds,
                AttemptToSendEveryXSeconds = mintAndPlaceGeoSpatialNFTRequest.AttemptToSendEveryXSeconds,
                JSONMetaData = mintAndPlaceGeoSpatialNFTRequest.JSONMetaData,
                SendToAvatarAfterMintingEmail = mintAndPlaceGeoSpatialNFTRequest.SendToAvatarAfterMintingEmail,
                WaitForNFTToMintInSeconds = mintAndPlaceGeoSpatialNFTRequest.WaitForNFTToMintInSeconds,
                WaitForNFTToSendInSeconds = mintAndPlaceGeoSpatialNFTRequest.WaitForNFTToSendInSeconds,
                WaitTillNFTMinted = mintAndPlaceGeoSpatialNFTRequest.WaitTillNFTMinted,
                WaitTillNFTSent = mintAndPlaceGeoSpatialNFTRequest.WaitTillNFTSent,
                Web3NFTs = mintAndPlaceGeoSpatialNFTRequest.Web3NFTs
            };
        }

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
