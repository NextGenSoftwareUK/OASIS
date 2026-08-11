using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ADRaffy.ENSNormalize;
using Newtonsoft.Json;
using NextGenSoftware.CLI.Engine;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.GeoSpatialNFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.GeoSpatialNFT.Request;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Request;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using NextGenSoftware.OASIS.API.ONODE.Core.Holons;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces;
using NextGenSoftware.OASIS.API.ONODE.Core.Managers;
using NextGenSoftware.OASIS.API.ONODE.Core.Network;
using NextGenSoftware.OASIS.API.ONODE.Core.Objects;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.STAR.CLI.Lib.Objects;
using NextGenSoftware.OASIS.STAR.DNA;

namespace NextGenSoftware.OASIS.STAR.CLI.Lib
{
    public partial class GeoNFTs : STARNETUIBase<STARGeoNFT, DownloadedGeoNFT, InstalledGeoNFT, STARNETDNA>
    {
        public async Task<OASISResult<IWeb4GeoSpatialNFT>> ConvertGeoNFTAsync(object mintParams = null)
        {
            OASISResult<IWeb4GeoSpatialNFT> result = new OASISResult<IWeb4GeoSpatialNFT>();
            OASISErrorHandling.HandleError(ref result, "WEB4 GeoNFT convert is not available: the STAR CLI has no wired ONODE/NFTManager convert API yet (use remint, wrap/create, or interactive flows where applicable).");
            return await Task.FromResult(result);
        }

        public virtual async Task<OASISResult<IEnumerable<IWeb4GeoSpatialNFT>>> ListAllWeb4GeoNFTsAsync(ProviderType providerType = ProviderType.Default)
        {
            Console.WriteLine("");
            CLIEngine.ShowWorkingMessage($"Loading WEB4 Geo-NFT's...");
            return ListWeb4GeoNFTs(await NFTCommon.NFTManager.LoadAllWeb4GeoNFTsAsync(providerType));
        }

        public virtual OASISResult<IEnumerable<IWeb4GeoSpatialNFT>> ListAllWeb4GeoNFTs(ProviderType providerType = ProviderType.Default)
        {
            Console.WriteLine("");
            CLIEngine.ShowWorkingMessage($"Loading WEB4 Geo-NFT's...");
            return ListWeb4GeoNFTs(NFTCommon.NFTManager.LoadAllWeb4GeoNFTs(providerType));
        }

        //public virtual async Task<OASISResult<IEnumerable<IOASISGeoSpatialNFT>>> ListAllWeb4GeoNFTForAvatarsAsync(bool showAllVersions = false, bool showDetailedInfo = false, int version = 0, ProviderType providerType = ProviderType.Default)
        public virtual async Task<OASISResult<IEnumerable<IWeb4GeoSpatialNFT>>> ListAllWeb4GeoNFTForAvatarsAsync(ProviderType providerType = ProviderType.Default)
        {
            Console.WriteLine("");
            CLIEngine.ShowWorkingMessage($"Loading WEB4 Geo-NFT's...");
            return ListWeb4GeoNFTs(await NFTCommon.NFTManager.LoadAllWeb4GeoNFTsForAvatarAsync(STAR.BeamedInAvatar.Id, providerType));
        }

        //public virtual OASISResult<IEnumerable<IOASISGeoSpatialNFT>> ListAllWeb4GeoNFTsForAvatar(bool showAllVersions = false, bool showDetailedInfo = false, int version = 0, ProviderType providerType = ProviderType.Default)
        public virtual OASISResult<IEnumerable<IWeb4GeoSpatialNFT>> ListAllWeb4GeoNFTsForAvatar(ProviderType providerType = ProviderType.Default)
        {
            Console.WriteLine("");
            CLIEngine.ShowWorkingMessage($"Loading WEB4 Geo-NFT's...");
            return ListWeb4GeoNFTs(NFTCommon.NFTManager.LoadAllWeb4GeoNFTsForAvatar(STAR.BeamedInAvatar.Id, providerType));
        }

        public async Task<OASISResult<IWeb4GeoSpatialNFT>> UpdateWeb4GeoNFTAsync(string idOrName = "", ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IWeb4GeoSpatialNFT> result = new OASISResult<IWeb4GeoSpatialNFT>();
            UpdateWeb4GeoNFTRequest request = new UpdateWeb4GeoNFTRequest();

            OASISResult<IWeb4GeoSpatialNFT> nftResult = await FindWeb4GeoNFTAsync("update", idOrName, providerType: providerType);

            if (nftResult != null && nftResult.Result != null && !nftResult.IsError)
            {
                // Prefill request with existing values so unchanged fields are preserved
                var existing = nftResult.Result;

                request.Id = existing.Id;
                request.Title = existing.Title;
                request.Description = existing.Description;
                request.Image = existing.Image;
                request.ImageUrl = existing.ImageUrl;
                request.Thumbnail = existing.Thumbnail;
                request.ThumbnailUrl = existing.ThumbnailUrl;
                request.Price = existing.Price;
                request.Discount = existing.Discount;
                request.Tags = existing.Tags != null ? new List<string>(existing.Tags) : null;
                request.MetaData = existing.MetaData != null ? new Dictionary<string, string>(existing.MetaData) : new Dictionary<string, string>();

                // Geo specific
                request.Lat = (long?)existing.Lat;
                request.Long = (long?)existing.Long;
                request.AllowOtherPlayersToAlsoCollect = existing.AllowOtherPlayersToAlsoCollect;
                request.PermSpawn = existing.PermSpawn;
                request.GlobalSpawnQuantity = existing.GlobalSpawnQuantity;
                request.PlayerSpawnQuantity = existing.PlayerSpawnQuantity;
                request.RespawnDurationInSeconds = existing.RespawnDurationInSeconds;
                request.Nft3DObject = existing.Nft3DObject;
                request.Nft3DObjectURI = existing.Nft3DObjectURI;
                request.Nft2DSprite = existing.Nft2DSprite;
                request.Nft2DSpriteURI = existing.Nft2DSpriteURI;

                OASISResult<IUpdateWeb4NFTRequest> updateResult = await NFTCommon.UpdateWeb4NFTAsync(request, nftResult.Result, "WEB4 GeoNFT", false, false);

                if (updateResult != null && updateResult.Result != null && !updateResult.IsError)
                {
                    request = (UpdateWeb4GeoNFTRequest)updateResult.Result;

                    // Geo specific edits
                    if (CLIEngine.GetConfirmation("Do you wish to edit the Lat/Long location?"))
                    {
                        request.Lat = CLIEngine.GetValidInputForLong("Please enter the new Lat location:", addLineBefore: true);
                        request.Long = CLIEngine.GetValidInputForLong("Please enter the new Long location:", addLineBefore: true);
                    }
                    else
                        Console.WriteLine("");

                    if (CLIEngine.GetConfirmation("Do you wish to edit spawn settings (PermSpawn / AllowOtherPlayersToAlsoCollect)?"))
                    {
                        Console.WriteLine("");
                        request.PermSpawn = CLIEngine.GetConfirmation("Will the NFT be permanently spawned? Press 'Y' for Yes or 'N' for No.");

                        if (!request.PermSpawn.Value)
                        {
                            Console.WriteLine("");
                            request.AllowOtherPlayersToAlsoCollect = CLIEngine.GetConfirmation("Once the NFT has been collected by a given player/avatar, do you want it to also still be collectable by other players/avatars? (Press Y for Yes or N for No)");

                            if (request.AllowOtherPlayersToAlsoCollect.Value)
                            {
                                Console.WriteLine("");
                                request.GlobalSpawnQuantity = CLIEngine.GetValidInputForInt("How many times can the NFT re-spawn once it has been collected?");
                                request.RespawnDurationInSeconds = CLIEngine.GetValidInputForInt("How long will it take (in seconds) for the NFT to re-spawn once it has been collected?");
                                request.PlayerSpawnQuantity = CLIEngine.GetValidInputForInt("How many times can the NFT re-spawn once it has been collected for a given player/avatar? (If you want to enforce that players/avatars can only collect each NFT once then set this to 0.)");
                            }
                        }
                    }
                    else
                        Console.WriteLine("");

                    if (CLIEngine.GetConfirmation("Do you wish to update the 2D sprite or 3D object assets?", addLineBefore: true))
                    {
                        Console.WriteLine("");
                        OASISResult<ImageObjectResult> imageObjectResult = await ProcessImageOrObjectAsync("WEB4 Geo-NFT");

                        if (imageObjectResult != null && imageObjectResult.Result != null && !imageObjectResult.IsError)
                        {
                            request.Nft3DObject = imageObjectResult.Result.Object3D;
                            request.Nft3DObjectURI = imageObjectResult.Result.Object3DURI != null ? imageObjectResult.Result.Object3DURI.AbsoluteUri : request.Nft3DObjectURI;
                            request.Nft2DSprite = imageObjectResult.Result.Image2D;
                            request.Nft2DSpriteURI = imageObjectResult.Result.Image2DURI != null ? imageObjectResult.Result.Image2DURI.AbsoluteUri : request.Nft2DSpriteURI;
                        }
                        else
                        {
                            string msg = imageObjectResult != null ? imageObjectResult.Message : "";
                            OASISErrorHandling.HandleError(ref result, $"Error Occured Processing 2D/3D assets for WEB4 Geo-NFT: {msg}");
                            return result;
                        }
                    }
                    else
                        Console.WriteLine("");

                    request.Tags = TagHelper.ManageTags(nftResult.Result.Tags);
                    request.MetaData = MetaDataHelper.ManageMetaData(nftResult.Result.MetaData, "WEB4 Geo-NFT");

                    CLIEngine.ShowWorkingMessage("Saving WEB4 Geo-NFT...");
                    result = await NFTCommon.NFTManager.UpdateWeb4GeoNFTAsync(request, providerType);

                    if (result != null && result.Result != null && !result.IsError)
                    {
                        CLIEngine.ShowSuccessMessage("WEB4 OASIS GeoNFT Successfully Updated.");

                        if (result != null && result.Result != null && !result.IsError && result.Result.ParentWeb5NFTIds != null && result.Result.ParentWeb5NFTIds.Count > 0)
                        {
                            foreach (Guid id in result.Result.ParentWeb5NFTIds)
                            {
                                result = await NFTCommon.UpdateSTARNETHolonAsync(id, "GeoNFT", STARNETManager, result, providerType);

                                var starNFTResult = await STARNETManager.LoadAsync(STAR.BeamedInAvatar.Id, id, providerType: providerType);

                                if (starNFTResult != null && starNFTResult.Result != null && !starNFTResult.IsError)
                                    UpdateWeb4AndWeb3GeoNFTJSONFiles(result.Result, starNFTResult.Result.STARNETDNA.SourcePath);
                            }
                        }
                    }
                    else
                    {
                        string msg = result != null ? result.Message : "";
                        OASISErrorHandling.HandleError(ref result, $"Error Occured Updating WEB4 GeoNFT in UpdateWeb4GeoNFTAsync method. Reason: {msg}");
                    }
                }
                
                //if (CLIEngine.GetConfirmation("Do you wish to edit the Title?"))
                //    request.Title = CLIEngine.GetValidInput("Please enter the new title: ", addLineBefore: true);
                //else
                //    Console.WriteLine("");

                //if (CLIEngine.GetConfirmation("Do you wish to edit the Description?"))
                //    request.Description = CLIEngine.GetValidInput("Please enter the new description: ", addLineBefore: true);
                //else
                //    Console.WriteLine("");

                //request.ModifiedByAvatarId = STAR.BeamedInAvatar.Id;

                //if (CLIEngine.GetConfirmation("Do you wish to update the Image and Thumbnail?"))
                //{
                //    Console.WriteLine("");
                //    OASISResult<ImageAndThumbnail> imageAndThumbnailResult = NFTCommon.ProcessImageAndThumbnail("WEB4 Geo-NFT");

                //    if (imageAndThumbnailResult != null && imageAndThumbnailResult.Result != null && !imageAndThumbnailResult.IsError)
                //    {
                //        request.Image = imageAndThumbnailResult.Result.Image;
                //        request.ImageUrl = imageAndThumbnailResult.Result.ImageUrl;
                //        request.Thumbnail = imageAndThumbnailResult.Result.Thumbnail;
                //        request.ThumbnailUrl = imageAndThumbnailResult.Result.ThumbnailUrl;
                //    }
                //    else
                //    {
                //        string msg = imageAndThumbnailResult != null ? imageAndThumbnailResult.Message : "";
                //        OASISErrorHandling.HandleError(ref result, $"Error Occured Processing Image and Thumbnail for WEB4 Geo-NFT: {msg}");
                //        return result;
                //    }
                //}
                //else
                //    Console.WriteLine("");

                //if (CLIEngine.GetConfirmation("Do you wish to edit the Price?"))
                //{
                //    Console.WriteLine("");
                //    request.Price = CLIEngine.GetValidInputForDecimal("Please enter the new Price: ");
                //}
                //else
                //    Console.WriteLine("");

                //if (CLIEngine.GetConfirmation("Do you wish to edit the Discount?"))
                //{
                //    Console.WriteLine("");
                //    request.Discount = CLIEngine.GetValidInputForDecimal("Please enter the new Discount: ");
                //}
                //else
                //    Console.WriteLine("");

                //if (CLIEngine.GetConfirmation("Do you wish to edit the Royalty Percentage?"))
                //    request.RoyaltyPercentage = CLIEngine.GetValidInputForInt("Please enter the Royalty Percentage (integer): ", false);
                //else
                //    Console.WriteLine("");

                //if (CLIEngine.GetConfirmation("Do you wish to change the sale status (Is For Sale)?"))
                //    request.IsForSale = CLIEngine.GetConfirmation("Is the NFT for sale? Press 'Y' for Yes or 'N' for No.");
                //else
                //    Console.WriteLine("");

                //if (request.IsForSale.HasValue && request.IsForSale.Value)
                //{
                //    string existingSaleStartDate = collectionResult.Result.SaleStartDate.HasValue ? collectionResult.Result.SaleStartDate.Value == DateTime.MinValue ? "None" : collectionResult.Result.SaleStartDate.Value.ToShortDateString() : "None";

                //    if (CLIEngine.GetConfirmation($"Do you wish to edit the Sale Start Date? (currently is: {existingSaleStartDate})", addLineBefore: true))
                //        request.SaleStartDate = CLIEngine.GetValidInputForDate("Please enter the Sale Start Date (YYYY-MM-DD) or 'none' to clear:", addLineBefore: true);
                //    else
                //        Console.WriteLine("");

                //    if (request.SaleStartDate.HasValue)
                //    {
                //        string existingSaleEndDate = collectionResult.Result.SaleEndDate.HasValue ? collectionResult.Result.SaleEndDate.Value == DateTime.MinValue ? "None" : collectionResult.Result.SaleEndDate.Value.ToShortDateString() : "None";

                //        if (CLIEngine.GetConfirmation($"Do you wish to edit Sale End Date? (currently is: {existingSaleEndDate})"))
                //        {
                //            do
                //            {
                //                request.SaleEndDate = CLIEngine.GetValidInputForDate("Please enter the Sale End Date (YYYY-MM-DD) or 'none' to clear:", addLineBefore: true);

                //                if (request.SaleEndDate.HasValue && request.SaleEndDate.Value <= request.SaleEndDate.Value)
                //                    CLIEngine.ShowWarningMessage("The end date must be after the start date!");
                //            }
                //            while (request.SaleEndDate.HasValue && request.SaleEndDate.Value <= request.SaleStartDate.Value);
                //        }
                //        else
                //            Console.WriteLine("");
                //    }
                //    else
                //        request.SaleEndDate = null;
                //}
            }
            else
            {
                string msg = nftResult != null ? nftResult.Message : "";
                OASISErrorHandling.HandleError(ref result, $"Error Occured Finding WEB4 Geo-NFT to update: {msg}");
            }

            return result;
        }

        public async Task<OASISResult<IWeb4GeoSpatialNFT>> DeleteWeb4GeoNFTAsync(string idOrName, ProviderType providerType = ProviderType.Default)
        {
            return await DeleteWeb4GeoNFTAsync(idOrName, null, null, null, providerType);
        }

        public async Task<OASISResult<IWeb4GeoSpatialNFT>> DeleteWeb4GeoNFTAsync(string idOrName, bool? softDelete = true, bool? deleteChildWeb3NFTs = false, bool? burnChildWebNFTs = false, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IWeb4GeoSpatialNFT> geoNFT = await FindWeb4GeoNFTAsync("delete", idOrName, true);

            if (geoNFT == null || geoNFT.Result == null || geoNFT.IsError)
            {
                OASISErrorHandling.HandleError(ref geoNFT, $"Error occured finding WEB4 Geo-NFT to delete. Reason: {geoNFT.Message}");
                return geoNFT;
            }

            if (!softDelete.HasValue)
                softDelete = CLIEngine.GetConfirmation("Do you wish to permanently delete the Web4 Geo-NFT? (defaults to false)");

            if (!deleteChildWeb3NFTs.HasValue)
                deleteChildWeb3NFTs = CLIEngine.GetConfirmation("Do you wish to also delete the child Web3 NFTs? (the OASIS holon/metadata)(recommeneded/default)");

            if (!burnChildWebNFTs.HasValue)
                burnChildWebNFTs = CLIEngine.GetConfirmation("Do you wish to also burn the child Web3 NFTs? (permanently destroy the Web3 NFTs on-chain) (recommeneded/default)");

            CLIEngine.ShowWorkingMessage("Deleting WEB4 OASIS Geo-NFT...");
            OASISResult<bool> deleteResult = await NFTCommon.NFTManager.DeleteWeb4GeoNFTAsync(STAR.BeamedInAvatar.Id, geoNFT.Result.Id, softDelete.Value, deleteChildWeb3NFTs.Value, burnChildWebNFTs.Value, providerType: providerType);

            if (deleteResult != null && deleteResult.Result && !deleteResult.IsError)
            {
                CLIEngine.ShowSuccessMessage("WEB4 GeoNFT Successfully Deleted.");

                foreach (Guid id in geoNFT.Result.ParentWeb5NFTIds)
                    geoNFT = await NFTCommon.DeleteAllSTARNETVersionsAsync(id, STARNETManager, geoNFT, providerType);
            }
            else
            {
                string msg = deleteResult != null ? deleteResult.Message : "";
                OASISErrorHandling.HandleError(ref geoNFT, $"Error occured deleting WEB4 GeoNFT. Reason: {msg}");
            }

            return geoNFT;
        }

        public virtual async Task<OASISResult<IWeb4GeoSpatialNFT>> ShowWeb4GeoNFTAsync(string idOrName = "", ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IWeb4GeoSpatialNFT> result = new OASISResult<IWeb4GeoSpatialNFT>();

            Console.WriteLine("");
            CLIEngine.ShowWorkingMessage($"Loading WEB4 Geo-NFT's...");

            result = await FindWeb4GeoNFTAsync("view", idOrName, true, providerType: providerType);

            //if (result != null && result.Result != null && !result.IsError)
            //    ShowGeoNFT(result.Result);
            //else
            //    OASISErrorHandling.HandleError(ref result, "No WEB4 Geo-NFT Found For That Id or Name!");

            return result;
        }

        public virtual async Task SearchWeb4GeoNFTAsync(string searchTerm = "", bool showForAllAvatars = true, ProviderType providerType = ProviderType.Default)
        {
            if (string.IsNullOrEmpty(searchTerm) || searchTerm == "forallavatars" || searchTerm == "forallavatars")
                searchTerm = CLIEngine.GetValidInput($"What is the name of the WEB4 Geo-NFT you wish to search for?");

            Console.WriteLine("");
            CLIEngine.ShowWorkingMessage($"Searching WEB4 Geo-NFT's...");
            ListWeb4GeoNFTs(await NFTCommon.NFTManager.SearchWeb4GeoNFTsAsync(searchTerm, STAR.BeamedInAvatar.Id, null, MetaKeyValuePairMatchMode.All, !showForAllAvatars, providerType));
        }
    }
}
