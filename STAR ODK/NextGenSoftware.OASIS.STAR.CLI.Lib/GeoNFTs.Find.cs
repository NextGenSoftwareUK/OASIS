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
        public async Task<OASISResult<IWeb4GeoSpatialNFT>> FindWeb4GeoNFTAsync(string operationName, string idOrName = "", bool showOnlyForCurrentAvatar = true, bool addSpace = true, string UIName = "WEB4 GeoNFT", ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IWeb4GeoSpatialNFT> result = new OASISResult<IWeb4GeoSpatialNFT>();
            Guid id = Guid.Empty;

            if (idOrName == Guid.Empty.ToString())
                idOrName = "";

            do
            {
                if (string.IsNullOrEmpty(idOrName))
                {
                    if (CLIEngine.NonInteractive)
                        throw new CLIEngineNonInteractiveInputRequiredException(
                            $"Non-interactive mode requires a WEB4 GeoNFT id or name for '{operationName}'.");

                    bool cont = true;
                    OASISResult<IEnumerable<IWeb4GeoSpatialNFT>> starHolonsResult = null;

                    if (!CLIEngine.GetConfirmation($"Do you know the GUID/ID or Name of the {UIName} you wish to {operationName}? Press 'Y' for Yes or 'N' for No."))
                    {
                        Console.WriteLine("");
                        CLIEngine.ShowWorkingMessage($"Loading {UIName}'s...");

                        if (showOnlyForCurrentAvatar)
                            starHolonsResult = await NFTCommon.NFTManager.LoadAllWeb4GeoNFTsForAvatarAsync(STAR.BeamedInAvatar.AvatarId, providerType);
                        else
                            starHolonsResult = await NFTCommon.NFTManager.LoadAllWeb4GeoNFTsAsync(providerType);

                        ListWeb4GeoNFTs(starHolonsResult);

                        if (!(starHolonsResult != null && starHolonsResult.Result != null && !starHolonsResult.IsError && starHolonsResult.Result.Count() > 0))
                            cont = false;
                    }
                    else
                        Console.WriteLine("");

                    if (cont)
                        idOrName = CLIEngine.GetValidInput($"What is the GUID/ID or Name of the {UIName} you wish to {operationName}?");
                    else
                    {
                        idOrName = "nonefound";
                        break;
                    }

                    if (idOrName == "exit")
                        break;
                }

                if (addSpace)
                    Console.WriteLine("");

                if (Guid.TryParse(idOrName, out id))
                {
                    CLIEngine.ShowWorkingMessage($"Loading {UIName}...");
                    result = await NFTCommon.NFTManager.LoadWeb4GeoNftAsync(id, providerType);

                    if (result != null && result.Result != null && !result.IsError && showOnlyForCurrentAvatar && result.Result.MintedByAvatarId != STAR.BeamedInAvatar.AvatarId)
                    {
                        CLIEngine.ShowErrorMessage($"You do not have permission to {operationName} this {UIName}. It was minted by another avatar.");
                        result.Result = default;
                    }
                }
                else
                {
                    CLIEngine.ShowWorkingMessage($"Searching {UIName}s...");
                    OASISResult<IEnumerable<IWeb4GeoSpatialNFT>> searchResults = await NFTCommon.NFTManager.SearchWeb4GeoNFTsAsync(idOrName, STAR.BeamedInAvatar.Id, null, MetaKeyValuePairMatchMode.All, showOnlyForCurrentAvatar, providerType);

                    if (searchResults != null && searchResults.Result != null && !searchResults.IsError)
                    {
                        if (searchResults.Result.Count() > 1)
                        {
                            if (CLIEngine.NonInteractive)
                                throw new CLIEngineNonInteractiveInputRequiredException(
                                    $"Multiple WEB4 GeoNFT matches for '{idOrName}'. Use a GUID in non-interactive mode.");

                            ListWeb4GeoNFTs(searchResults, true);

                            if (CLIEngine.GetConfirmation("Are any of these correct?"))
                            {
                                Console.WriteLine("");

                                do
                                {
                                    int number = CLIEngine.GetValidInputForInt($"What is the number of the {UIName} you wish to {operationName}?");

                                    if (number > 0 && number <= searchResults.Result.Count())
                                        result.Result = searchResults.Result.ElementAt(number - 1);
                                    else
                                        CLIEngine.ShowErrorMessage("Invalid number entered. Please try again.");

                                } while (result.Result == null || result.IsError);
                            }
                            else
                            {
                                Console.WriteLine("");
                                idOrName = "";
                            }
                        }
                        else if (searchResults.Result.Count() == 1)
                            result.Result = searchResults.Result.FirstOrDefault();
                        else
                        {
                            idOrName = "";
                            CLIEngine.ShowWarningMessage($"No {UIName} Found!");
                        }
                    }
                    else
                        CLIEngine.ShowErrorMessage($"An error occured calling STARNETManager.SearchsAsync. Reason: {searchResults.Message}");
                }

                if (result.Result != null)
                    ShowGeoNFT(result.Result);

                if (idOrName == "exit")
                    break;

                if (result.Result != null && operationName != "view")
                {
                    if (CLIEngine.GetConfirmation($"Please confirm you wish to {operationName} this {UIName}?"))
                    {

                    }
                    else
                    {
                        Console.WriteLine("");
                        result.Result = default;
                        idOrName = "";

                        if (!CLIEngine.GetConfirmation($"Do you wish to search for another {UIName}?"))
                        {
                            idOrName = "exit";
                            break;
                        }
                    }

                    Console.WriteLine("");
                }

                idOrName = "";
            }
            while (result.Result == null || result.IsError);

            if (idOrName == "exit")
            {
                result.IsError = true;
                result.Message = "User Exited";
            }
            else if (idOrName == "nonefound")
            {
                result.IsError = true;
                result.Message = "None Found";
            }

            return result;
        }


        private OASISResult<IEnumerable<IWeb4GeoSpatialNFT>> ListWeb4GeoNFTs(OASISResult<IEnumerable<IWeb4GeoSpatialNFT>> geoNFTs, bool showNumbers = false, bool showDetailedInfo = false)
        {
            if (geoNFTs != null)
            {
                if (!geoNFTs.IsError)
                {
                    if (geoNFTs.Result != null && geoNFTs.Result.Count() > 0)
                    {
                        Console.WriteLine();

                        if (geoNFTs.Result.Count() == 1)
                            CLIEngine.ShowMessage($"{geoNFTs.Result.Count()} WEB4 GeoNFT Found:");
                        else
                            CLIEngine.ShowMessage($"{geoNFTs.Result.Count()} WEB4 GeoNFT's Found:");

                        for (int i = 0; i < geoNFTs.Result.Count(); i++)
                            ShowGeoNFT(geoNFTs.Result.ElementAt(i), i == 0, true, showNumbers, i + 1, showDetailedInfo);
                    }
                    else
                        CLIEngine.ShowWarningMessage($"No WEB4 GeoNFT's Found.");
                }
                else
                    CLIEngine.ShowErrorMessage($"Error occured loading WEB4 GeoNFT's. Reason: {geoNFTs.Message}");
            }
            else
                CLIEngine.ShowErrorMessage($"Unknown error occured loading WEB4 GeoNFT's.");

            return geoNFTs;
        }

        private void ShowGeoNFT(IWeb4GeoSpatialNFT web4GeoNFT, bool showHeader = true, bool showFooter = true, bool showNumbers = false, int number = 0, bool showDetailedInfo = false, int displayFieldLength = 39)
        {
            if (DisplayFieldLength > displayFieldLength)
                displayFieldLength = DisplayFieldLength;

            if (showHeader)
                CLIEngine.ShowDivider();

            Console.WriteLine("");

            if (showNumbers)
                CLIEngine.ShowMessage(string.Concat("Number:".PadRight(displayFieldLength), number), false);

            NFTCommon.ShowNFTDetails(web4GeoNFT, null, displayFieldLength, false, false);

            //DisplayProperty("Geo-NFT Id", geoNFT.Id.ToString(), displayFieldLength);
            //DisplayProperty("NFT Id", geoNFT.OriginalWeb4OASISNFTId.ToString(), displayFieldLength);
            //DisplayProperty("Title", geoNFT.Title, displayFieldLength);
            //DisplayProperty("Description", geoNFT.Description, displayFieldLength);
            //DisplayProperty("Price", geoNFT.Price.ToString(), displayFieldLength);
            //DisplayProperty("Discount", geoNFT.Discount.ToString(), displayFieldLength);
            //DisplayProperty("Royalty Percentage", geoNFT.RoyaltyPercentage.ToString(), displayFieldLength);
            //DisplayProperty("For Sale", geoNFT.IsForSale ? string.Concat("Yes (StartDate: ", geoNFT.SaleStartDate.HasValue ? geoNFT.SaleStartDate.Value.ToShortDateString() : "Not Set", geoNFT.SaleEndDate.HasValue ? geoNFT.SaleEndDate.Value.ToShortDateString() : "Not Set") : "No", displayFieldLength);
            //DisplayProperty("Minted By Avatar Id", geoNFT.MintedByAvatarId.ToString(), displayFieldLength);
            //DisplayProperty("Minted On", geoNFT.MintedOn.ToString(), displayFieldLength);
            //DisplayProperty("OnChain Provider", geoNFT.OnChainProvider.Name, displayFieldLength);
            //DisplayProperty("OffChain Provider", geoNFT.OffChainProvider.Name, displayFieldLength);
            //DisplayProperty("Store NFT Meta Data OnChain", geoNFT.StoreNFTMetaDataOnChain.ToString(), displayFieldLength);
            //DisplayProperty("NFT OffChain Meta Type", geoNFT.NFTOffChainMetaType.Name, displayFieldLength);
            //DisplayProperty("NFT Standard Type", geoNFT.NFTStandardType.Name, displayFieldLength);
            //DisplayProperty("Symbol", geoNFT.Symbol, displayFieldLength);
            //DisplayProperty("Image", geoNFT.Image != null ? "Yes" : "None", displayFieldLength);
            //DisplayProperty("Image Url", geoNFT.ImageUrl, displayFieldLength);
            //DisplayProperty("Thumbnail", geoNFT.Thumbnail != null ? "Yes" : "None", displayFieldLength);
            //DisplayProperty("Thumbnail Url", !string.IsNullOrEmpty(geoNFT.ThumbnailUrl) ? geoNFT.ThumbnailUrl : "None", displayFieldLength);
            //DisplayProperty("JSON MetaData URL", geoNFT.JSONMetaDataURL, displayFieldLength);
            //DisplayProperty("JSON MetaData URL Holon Id", geoNFT.JSONMetaDataURLHolonId != Guid.Empty ? geoNFT.JSONMetaDataURLHolonId.ToString() : "None", displayFieldLength);
            //DisplayProperty("Seller Fee Basis Points", geoNFT.SellerFeeBasisPoints.ToString(), displayFieldLength);
            //DisplayProperty("Send To Address After Minting", geoNFT.SendToAddressAfterMinting, displayFieldLength);
            //DisplayProperty("Send To Avatar After Minting Id", geoNFT.SendToAvatarAfterMintingId != Guid.Empty ? geoNFT.SendToAvatarAfterMintingId.ToString() : "None", displayFieldLength);
            //DisplayProperty("Send To Avatar After Minting Username", !string.IsNullOrEmpty(geoNFT.SendToAvatarAfterMintingUsername) ? geoNFT.SendToAvatarAfterMintingUsername : "None", displayFieldLength);
            DisplayProperty("Lat/Long", $"{web4GeoNFT.Lat}/{web4GeoNFT.Long}", displayFieldLength);
            DisplayProperty("Perm Spawn", web4GeoNFT.PermSpawn.ToString(), displayFieldLength);

            if (!web4GeoNFT.PermSpawn)
            {
                DisplayProperty("Allow Other Players To Also Collect", web4GeoNFT.AllowOtherPlayersToAlsoCollect.ToString(), displayFieldLength);

                if (web4GeoNFT.AllowOtherPlayersToAlsoCollect)
                {
                    DisplayProperty("Global Spawn Quantity", web4GeoNFT.GlobalSpawnQuantity.ToString(), displayFieldLength);
                    DisplayProperty("Player Spawn Quantity", web4GeoNFT.PlayerSpawnQuantity.ToString(), displayFieldLength);
                    DisplayProperty("Respawn Duration In Seconds", web4GeoNFT.RespawnDurationInSeconds.ToString(), displayFieldLength);
                }
                else
                {
                    DisplayProperty("Global Spawn Quantity", "N/A", displayFieldLength);
                    DisplayProperty("Player Spawn Quantity", "N/A", displayFieldLength);
                    DisplayProperty("Respawn Duration In Seconds", "N/A", displayFieldLength);
                }
            }
            else
            {
                DisplayProperty("Allow Other Players To Also Collect", "N/A", displayFieldLength);
                DisplayProperty("Global Spawn Quantity", "N/A", displayFieldLength);
                DisplayProperty("Player Spawn Quantity", "N/A", displayFieldLength);
                DisplayProperty("Respawn Duration In Seconds", "N/A", displayFieldLength);
            }

            DisplayProperty("2D Sprite", web4GeoNFT.Nft2DSprite != null ? "Yes" : "None", displayFieldLength);
            DisplayProperty("2D Sprite URL", !string.IsNullOrEmpty(web4GeoNFT.Nft2DSpriteURI) ? web4GeoNFT.Nft2DSpriteURI : "None", displayFieldLength);
            DisplayProperty("3D Object", web4GeoNFT.Nft2DSprite != null ? "Yes" : "None", displayFieldLength);
            DisplayProperty("3D Object URL", !string.IsNullOrEmpty(web4GeoNFT.Nft3DObjectURI) ? web4GeoNFT.Nft3DObjectURI : "None", displayFieldLength);

            TagHelper.ShowTags(web4GeoNFT.Tags, displayFieldLength);
            MetaDataHelper.ShowMetaData(web4GeoNFT.MetaData, displayFieldLength);

            Console.WriteLine("");
            DisplayProperty($"WEB3 NFT's ({web4GeoNFT.Web3NFTs.Count})", "", displayFieldLength);

            foreach (Web3NFT web3NFT in web4GeoNFT.Web3NFTs)
            {
                NFTCommon.ShowNFTDetails(web3NFT, web4GeoNFT, displayFieldLength);
                DisplayProperty("Send NFT Transaction Hash", web3NFT.SendNFTTransactionHash, displayFieldLength);
                DisplayProperty("OASIS MintWallet Address", web3NFT.OASISMintWalletAddress, displayFieldLength);
                DisplayProperty("Mint Transaction Hash", web3NFT.MintTransactionHash, displayFieldLength);
                DisplayProperty("NFT Token Address", web3NFT.NFTTokenAddress, displayFieldLength);
                DisplayProperty("Update Authority", web3NFT.UpdateAuthority, displayFieldLength);
            }

            if (showFooter)
                CLIEngine.ShowDivider();
        }

        private async Task<IPlaceWeb4GeoSpatialNFTRequest> GenerateGeoNFTRequestAsync(bool isExistingNFT)
        {
            PlaceWeb4GeoSpatialNFTRequest request = new PlaceWeb4GeoSpatialNFTRequest();
            request.PlacedByAvatarId = STAR.BeamedInAvatar.Id;

            if (isExistingNFT)
            {
                request.OriginalWeb4OASISNFTId = CLIEngine.GetValidInputForGuid("What is the original WEB4 OASIS NFT ID?");
                request.OriginalWeb4OASISNFTOffChainProvider = new Utilities.EnumValue<ProviderType>((ProviderType)CLIEngine.GetValidInputForEnum("What provider did you choose to store the off-chain metadata for the original OASIS NFT? (if you cannot remember, then enter 'All' and the OASIS HyperDrive will attempt to find it through auto-replication).", typeof(ProviderType)));
            }

            request.GeoNFTMetaDataProvider = new Utilities.EnumValue<ProviderType>((ProviderType)CLIEngine.GetValidInputForEnum("What provider would you like to store the Geo-NFT metadata on? (NOTE: It will automatically auto-replicate to other providers across the OASIS through the auto-replication feature in the OASIS HyperDrive)", typeof(ProviderType)));
            request.Lat = CLIEngine.GetValidInputForLong("What is the lat geo-location you wish for your NFT to appear in Our World/AR World?");
            request.Long = CLIEngine.GetValidInputForLong("What is the long geo-location you wish for your NFT to appear in Our World/AR World?");

            //if (CLIEngine.GetConfirmation("Would you like to use the NFT Thumbnail to represent the GeoNFT in Our World/OAPPs?")
            OASISResult<ImageObjectResult> imageObjectResult = await ProcessImageOrObjectAsync("Geo-NFT");

            if (imageObjectResult != null && imageObjectResult.Result != null && !imageObjectResult.IsError)
            {
                request.Nft3DObject = imageObjectResult.Result.Object3D;
                request.Nft3DObjectURI = imageObjectResult.Result.Object3DURI != null ? imageObjectResult.Result.Object3DURI.AbsoluteUri : "";
                request.Nft2DSprite = imageObjectResult.Result.Image2D;
                request.Nft2DSpriteURI = imageObjectResult.Result.Image2DURI != null ? imageObjectResult.Result.Image2DURI.AbsoluteUri : "";
            }

            request.PermSpawn = CLIEngine.GetConfirmation("Will the NFT be permantly spawned allowing infinite number of players to collect as many times as they wish? If you select Y to this then the NFT will always be available with zero re-spawn time.");
            Console.WriteLine("");

            if (!request.PermSpawn)
            {
                request.AllowOtherPlayersToAlsoCollect = CLIEngine.GetConfirmation("Once the NFT has been collected by a given player/avatar, do you want it to also still be collectable by other players/avatars?");

                if (request.AllowOtherPlayersToAlsoCollect)
                {
                    Console.WriteLine("");
                    request.GlobalSpawnQuantity = CLIEngine.GetValidInputForInt("How many times can the NFT re-spawn once it has been collected?");
                    request.RespawnDurationInSeconds = CLIEngine.GetValidInputForInt("How long will it take (in seconds) for the NFT to re-spawn once it has been collected?");
                    request.PlayerSpawnQuantity = CLIEngine.GetValidInputForInt("How many times can the NFT re-spawn once it has been collected for a given player/avatar? (If you want to enforce that players/avatars can only collect each NFT once then set this to 0.)");
                }
                else
                    Console.WriteLine("");
            }

            return request;
        }

        private OASISResult<bool> UpdateWeb4AndWeb3GeoNFTJSONFiles(IWeb4GeoSpatialNFT NFT, string path)
        {
            OASISResult<bool> result = new OASISResult<bool>();

            try
            {
                File.WriteAllText(Path.Combine(path, $"WEB4_GEONFT_{NFT.Id}.json"), JsonConvert.SerializeObject(NFT));

                if (!string.IsNullOrEmpty(NFT.JSONMetaData))
                    File.WriteAllText(Path.Combine(path, $"WEB4_JSONMetaData_{NFT.Id}.json"), NFT.JSONMetaData);

                foreach (IWeb3NFT web3Nft in NFT.Web3NFTs)
                {
                    File.WriteAllText(Path.Combine(path, $"WEB3_NFT_{web3Nft.Id}.json"), JsonConvert.SerializeObject(web3Nft));

                    if (!string.IsNullOrEmpty(web3Nft.JSONMetaData))
                        File.WriteAllText(Path.Combine(path, $"WEB3_JSONMetaData_{web3Nft.Id}.json"), web3Nft.JSONMetaData);
                }
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"Error occured updating WEB4 and WEB3 NFT JSON files. Reason: {e.Message}");
            }

            result.Result = true;
            return result;
        }
    }
}
