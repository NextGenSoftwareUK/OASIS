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
        public async Task<OASISResult<IInventoryItem>> CollectGeoNFTAsync(ICollectGeoNFTRequest request, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IInventoryItem> result = new OASISResult<IInventoryItem>();
            string errorMessage = "Error occured in CollectGeoNFTAsync in NFTManager. Reason:";

            try
            {
                return await AvatarManager.Instance.AddItemToAvatarInventoryAsync(request.CollectedByAvatarId, new InventoryItem()
                {
                     Image2D = request.Image2D,
                     Image2DURI = request.Image2DURI,
                     Object3D = request.Object3D,
                     Object3DURI = request.Object3DURI,
                     Quantity = request.Quantity,
                     Stack = request.Stack,
                     GameSource = request.GameSource,
                     //ItemType = request.ItemType,
                     ItemType = InventoryItemType.GeoNFT,
                    NftId = request.GeoNFTId
                }, providerType);

            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
            }

            return result;
        }

        public OASISResult<IInventoryItem> CollectGeoNFT(ICollectGeoNFTRequest request, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IInventoryItem> result = new OASISResult<IInventoryItem>();
            string errorMessage = "Error occured in CollectGeoNFTAsync in NFTManager. Reason:";

            try
            {
                return AvatarManager.Instance.AddItemToAvatarInventory(request.CollectedByAvatarId, new InventoryItem()
                {
                    Image2D = request.Image2D,
                    Image2DURI = request.Image2DURI,
                    Object3D = request.Object3D,
                    Object3DURI = request.Object3DURI,
                    Quantity = request.Quantity,
                    Stack = request.Stack,
                    GameSource = request.GameSource,
                    //ItemType = request.ItemType,
                    ItemType = InventoryItemType.GeoNFT,
                    NftId = request.GeoNFTId
                }, providerType);

            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
            }

            return result;
        }

        public async Task<OASISResult<IInventoryItem>> CollectNFTAsync(ICollectGeoNFTRequest request, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IInventoryItem> result = new OASISResult<IInventoryItem>();
            string errorMessage = "Error occured in CollectNFTAsync in NFTManager. Reason:";

            try
            {
                return await AvatarManager.Instance.AddItemToAvatarInventoryAsync(request.CollectedByAvatarId, new InventoryItem()
                {
                    Image2D = request.Image2D,
                    Image2DURI = request.Image2DURI,
                    Object3D = request.Object3D,
                    Object3DURI = request.Object3DURI,
                    Quantity = request.Quantity,
                    Stack = request.Stack,
                    GameSource = request.GameSource,
                    //ItemType = request.ItemType,
                    ItemType = InventoryItemType.NFT,
                    NftId = request.GeoNFTId
                }, providerType);

            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
            }

            return result;
        }

        public OASISResult<IInventoryItem> CollectNFT(ICollectGeoNFTRequest request, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IInventoryItem> result = new OASISResult<IInventoryItem>();
            string errorMessage = "Error occured in CollectNFTAsync in NFTManager. Reason:";

            try
            {
                return AvatarManager.Instance.AddItemToAvatarInventory(request.CollectedByAvatarId, new InventoryItem()
                {
                    Image2D = request.Image2D,
                    Image2DURI = request.Image2DURI,
                    Object3D = request.Object3D,
                    Object3DURI = request.Object3DURI,
                    Quantity = request.Quantity,
                    Stack = request.Stack,
                    GameSource = request.GameSource,
                    //ItemType = request.ItemType,
                    ItemType = InventoryItemType.NFT,
                    NftId = request.GeoNFTId
                }, providerType);

            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
            }

            return result;
        }

    }
}