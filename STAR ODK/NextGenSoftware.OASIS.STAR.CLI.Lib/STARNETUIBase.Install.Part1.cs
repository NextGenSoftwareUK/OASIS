using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Drawing.Text;
using System.Linq;
using ADRaffy.ENSNormalize;
using NextGenSoftware.CLI.Engine;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.ONODE.Core.Enums.STARNETHolon;
using NextGenSoftware.OASIS.API.ONODE.Core.Events.STARNETHolon;
using NextGenSoftware.OASIS.API.ONODE.Core.Holons;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Holons;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Managers;
using NextGenSoftware.OASIS.API.ONODE.Core.Managers;
using NextGenSoftware.OASIS.API.ONODE.Core.Objects;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.STAR.CelestialSpace;
using NextGenSoftware.OASIS.STAR.CLI.Lib.Enums;
using NextGenSoftware.OASIS.STAR.CLI.Lib.Objects;
using Org.BouncyCastle.Utilities;

namespace NextGenSoftware.OASIS.STAR.CLI.Lib
{
    public partial class STARNETUIBase<T1, T2, T3, T4>
    {
        public async Task<OASISResult<T3>> FindAndInstallIfNotInstalledAsync(string operationName, string idOrName = "", bool showOnlyForCurrentAvatar = true, string STARNETHolonUIName = "Default", ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T3> result = new OASISResult<T3>();

            if (STARNETHolonUIName == "Default")
                STARNETHolonUIName = STARNETManager.STARNETHolonUIName;

            OASISResult<T1> findResult = await FindAsync(operationName, idOrName, default, showOnlyForCurrentAvatar, STARNETHolonUIName: STARNETHolonUIName, providerType: providerType);

            if (findResult != null && findResult.Result != null && !findResult.IsError)
            {
                OASISResult<bool> celestialBodyDNAInstalledResult = await STARNETManager.IsInstalledAsync(STAR.BeamedInAvatar.Id, findResult.Result.STARNETDNA.Id, findResult.Result.STARNETDNA.VersionSequence, providerType);

                if (celestialBodyDNAInstalledResult != null && !celestialBodyDNAInstalledResult.IsError)
                {
                    if (!celestialBodyDNAInstalledResult.Result)
                    {
                        if (CLIEngine.GetConfirmation($"The selected {STARNETHolonUIName} is not currently installed. Do you wish to install it now?"))
                        {
                            OASISResult<T3> installResult = await DownloadAndInstallAsync(findResult.Result.STARNETDNA.Id.ToString(), InstallMode.DownloadAndInstall, providerType);

                            if (installResult.Result != null && !installResult.IsError)
                                result = installResult;
                            else
                                OASISErrorHandling.HandleError(ref result, $"Error occured installing the {STARNETHolonUIName}. Reason: {installResult.Message}");
                        }
                    }
                    else
                    {
                        OASISResult<T3> loadResult = await STARNETManager.LoadInstalledAsync(STAR.BeamedInAvatar.Id, findResult.Result.STARNETDNA.Id, findResult.Result.STARNETDNA.VersionSequence, providerType);

                        if (loadResult != null && loadResult.Result != null && !loadResult.IsError)
                            result = loadResult;
                        else
                            OASISErrorHandling.HandleError(ref result, $"Error occured loading the {STARNETHolonUIName}. Reason: {loadResult.Message}");
                    }
                }
                else
                    OASISErrorHandling.HandleError(ref result, ($"Error occured checking if {STARNETHolonUIName} is installed. Reason: {celestialBodyDNAInstalledResult.Message}"));
            }
            else
                OASISErrorHandling.HandleError(ref result, ($"Error occured finding {STARNETHolonUIName}. Reason: {findResult.Message}"));

            return result;
        }

        //TODO: Finish implementing later!
        //public OASISResult<T3> FindAndInstallIfNotInstalled(string operationName, string idOrName = "", bool showOnlyForCurrentAvatar = true, string STARNETHolonUIName = "", ProviderType providerType = ProviderType.Default)
        //{
        //    OASISResult<T3> result = new OASISResult<T3>();
        //    OASISResult<T1> downloadedCelestialBodyDNA = STARCLI.CelestialBodiesMetaDataDNA.Find<T1>(operationName, idOrName, showOnlyForCurrentAvatar, STARNETHolonUIName: STARNETHolonUIName, providerType: providerType);

        //    if (downloadedCelestialBodyDNA != null && downloadedCelestialBodyDNA.Result != null && !downloadedCelestialBodyDNA.IsError)
        //    {
        //        OASISResult<bool> celestialBodyDNAInstalledResult = STAR.STARAPI.CelestialBodiesMetaDataDNA.IsInstalled(STAR.BeamedInAvatar.Id, downloadedCelestialBodyDNA.Result.STARNETDNA.Id, downloadedCelestialBodyDNA.Result.STARNETDNA.VersionSequence, providerType);

        //        if (celestialBodyDNAInstalledResult != null && !celestialBodyDNAInstalledResult.IsError)
        //        {
        //            if (!celestialBodyDNAInstalledResult.Result)
        //            {
        //                if (CLIEngine.GetConfirmation($"The selected {STARNETHolonUIName} is not currently installed. Do you wish to install it now?"))
        //                {
        //                    OASISResult<T3> installResult = DownloadAndInstall(downloadedCelestialBodyDNA.Result.STARNETDNA.Id.ToString(), InstallMode.DownloadAndInstall, providerType);

        //                    if (installResult.Result != null && !installResult.IsError)
        //                        result = installResult;
        //                    else
        //                        OASISErrorHandling.HandleError(ref result, $"Error occured installing the {STARNETHolonUIName}. Reason: {installResult.Message}");
        //                }
        //            }
        //            else
        //            {
        //                OASISResult<T3> loadResult = STARNETManager.LoadInstalled(STAR.BeamedInAvatar.Id, downloadedCelestialBodyDNA.Result.STARNETDNA.Id, downloadedCelestialBodyDNA.Result.STARNETDNA.VersionSequence, providerType);

        //                if (loadResult != null && loadResult.Result != null && !loadResult.IsError)
        //                    result = loadResult;
        //                else
        //                    OASISErrorHandling.HandleError(ref result, $"Error occured loading the {STARNETHolonUIName}. Reason: {loadResult.Message}");
        //            }
        //        }
        //        else
        //            CLIEngine.ShowErrorMessage($"Error occured checking if {STARNETHolonUIName} is installed. Reason: {celestialBodyDNAInstalledResult.Message}");
        //    }
        //    else
        //        CLIEngine.ShowErrorMessage($"Error occured finding {STARNETHolonUIName}. Reason: {downloadedCelestialBodyDNA.Message}");

        //    return result;
        //}


        protected string ParseMetaData(Dictionary<string, object> metaData, string key, string notFoundDefaultValue = "None")
        {
            return metaData != null && metaData.ContainsKey(key) && metaData[key] != null && !string.IsNullOrEmpty(metaData[key].ToString()) ? metaData[key].ToString() : notFoundDefaultValue;
        }

        protected string ParseMetaDataForEnum(Dictionary<string, object> metaData, string key, Type enumType, string notFoundDefaultValue = "None")
        {
            return metaData != null && metaData.ContainsKey(key) && metaData[key] != null ? Enum.GetName(enumType, metaData[key]) : notFoundDefaultValue;
        }

        protected string ParseMetaDataForByteArray(Dictionary<string, object> metaData, string key, string foundDefaultValue = "Yes", string notFoundDefaultValue = "No")
        {
            return metaData != null && metaData.ContainsKey(key) && metaData[key] != null ? foundDefaultValue : notFoundDefaultValue;
        }

        protected string ParseMetaDataForPositiveNumber(Dictionary<string, object> metaData, string key)
        {
            int number;

            if (metaData != null && metaData.ContainsKey(key) && metaData[key] != null)
            {
                if (int.TryParse(metaData[key].ToString(), out number))
                {
                    if (number > 0)
                        return number.ToString();
                }
            }

            return "None";
        }

        protected string ParseMetaDataForLatLong(Dictionary<string, object> metaData, string latKey, string longKey)
        {
            string latReturn = ParseMetaDataForPositiveNumber(metaData, latKey);
            string longReturn = ParseMetaDataForPositiveNumber(metaData, longKey);

            if (latReturn != "None" && longReturn != "None")
                return $"{latReturn}/{longReturn}";

            return "None";
        }

        protected string ParseMetaDataForBinaryUploadAndURI(Dictionary<string, object> metaData, string binaryUploadKey, string URIKey)
        {
            return metaData != null && metaData.ContainsKey(binaryUploadKey) && metaData[binaryUploadKey] != null ? "BINARY UPLOADED" : metaData != null && metaData.ContainsKey(URIKey) && metaData[URIKey] != null ? metaData[URIKey].ToString() : "None";
        }

        protected void DisplayProperty(string heading, string value, int displayFieldLength, bool displayColon = true)
        {
            CLIEngine.DisplayProperty(heading, value, displayFieldLength, displayColon);
        }

        //protected void ShowNFTDetails(INFTBase nft, IWeb4OASISNFT web4NFT, int displayFieldLength, bool displayTags = true, bool displayMetaData = true)
        //{
        //    DisplayProperty("NFT Id", nft.Id.ToString(), displayFieldLength);

        //    if ((web4NFT != null && nft.Title != web4NFT.Title) || web4NFT == null)
        //        DisplayProperty("Title", nft.Title, displayFieldLength);

        //    if ((web4NFT != null && nft.Description != web4NFT.Description) || web4NFT == null)
        //        DisplayProperty("Description", nft.Description, displayFieldLength);

        //    if ((web4NFT != null && nft.Price != web4NFT.Price) || web4NFT == null)
        //        DisplayProperty("Price", nft.Price.ToString(), displayFieldLength);

        //    if ((web4NFT != null && nft.Discount != web4NFT.Discount) || web4NFT == null)
        //        DisplayProperty("Discount", nft.Discount.ToString(), displayFieldLength);

        //    if ((web4NFT != null && nft.RoyaltyPercentage != web4NFT.RoyaltyPercentage) || web4NFT == null)
        //        DisplayProperty("Royalty Percentage", nft.RoyaltyPercentage.ToString(), displayFieldLength);

        //    if ((web4NFT != null && nft.IsForSale != web4NFT.IsForSale) || web4NFT == null)
        //        DisplayProperty("For Sale", nft.IsForSale ? string.Concat("Yes (StartDate: ", nft.SaleStartDate.HasValue ? nft.SaleStartDate.Value.ToShortDateString() : "Not Set", nft.SaleEndDate.HasValue ? nft.SaleEndDate.Value.ToShortDateString() : "Not Set") : "No", displayFieldLength);

        //    if ((web4NFT != null && nft.MintedByAvatarId != web4NFT.MintedByAvatarId) || web4NFT == null)
        //        DisplayProperty("Minted By Avatar Id", nft.MintedByAvatarId.ToString(), displayFieldLength);

        //    if ((web4NFT != null && nft.MintedOn != web4NFT.MintedOn) || web4NFT == null)
        //        DisplayProperty("Minted On", nft.MintedOn.ToString(), displayFieldLength);

        //    if ((web4NFT != null && nft.OnChainProvider.Name != web4NFT.OnChainProvider.Name) || web4NFT == null)
        //        DisplayProperty("OnChain Provider", nft.OnChainProvider.Name, displayFieldLength);

        //    if ((web4NFT != null && nft.OffChainProvider.Name != web4NFT.OffChainProvider.Name) || web4NFT == null)
        //        DisplayProperty("OffChain Provider", nft.OffChainProvider.Name, displayFieldLength);

        //    if ((web4NFT != null && nft.StoreNFTMetaDataOnChain != web4NFT.StoreNFTMetaDataOnChain) || web4NFT == null)
        //        DisplayProperty("Store NFT Meta Data OnChain", nft.StoreNFTMetaDataOnChain.ToString(), displayFieldLength);

        //    if ((web4NFT != null && nft.NFTOffChainMetaType.Name != web4NFT.NFTOffChainMetaType.Name) || web4NFT == null)
        //        DisplayProperty("NFT OffChain Meta Type", nft.NFTOffChainMetaType.Name, displayFieldLength);

        //    if ((web4NFT != null && nft.NFTStandardType.Name != web4NFT.NFTStandardType.Name) || web4NFT == null)
        //        DisplayProperty("NFT Standard Type", nft.NFTStandardType.Name, displayFieldLength);

        //    if ((web4NFT != null && nft.Symbol != web4NFT.Symbol) || web4NFT == null)
        //        DisplayProperty("Symbol", nft.Symbol, displayFieldLength);

        //    if ((web4NFT != null && nft.Image != web4NFT.Image) || web4NFT == null)
        //        DisplayProperty("Image", nft.Image != null ? "Yes" : "None", displayFieldLength);

        //    if ((web4NFT != null && nft.ImageUrl != web4NFT.ImageUrl) || web4NFT == null)
        //        DisplayProperty("Image Url", nft.ImageUrl, displayFieldLength);

        //    if ((web4NFT != null && nft.Thumbnail != web4NFT.Thumbnail) || web4NFT == null)
        //        DisplayProperty("Thumbnail", nft.Thumbnail != null ? "Yes" : "None", displayFieldLength);

        //    if ((web4NFT != null && nft.ThumbnailUrl != web4NFT.ThumbnailUrl) || web4NFT == null)
        //        DisplayProperty("Thumbnail Url", !string.IsNullOrEmpty(nft.ThumbnailUrl) ? nft.ThumbnailUrl : "None", displayFieldLength);

        //    if ((web4NFT != null && nft.JSONMetaDataURL != web4NFT.JSONMetaDataURL) || web4NFT == null)
        //        DisplayProperty("JSON MetaData URL", nft.JSONMetaDataURL, displayFieldLength);

        //    if ((web4NFT != null && nft.JSONMetaDataURLHolonId != web4NFT.JSONMetaDataURLHolonId) || web4NFT == null)
        //        DisplayProperty("JSON MetaData URL Holon Id", nft.JSONMetaDataURLHolonId != Guid.Empty ? nft.JSONMetaDataURLHolonId.ToString() : "None", displayFieldLength);

        //    if ((web4NFT != null && nft.SellerFeeBasisPoints != web4NFT.SellerFeeBasisPoints) || web4NFT == null)
        //        DisplayProperty("Seller Fee Basis Points", nft.SellerFeeBasisPoints.ToString(), displayFieldLength);

        //    if ((web4NFT != null && nft.SendToAddressAfterMinting != web4NFT.SendToAddressAfterMinting) || web4NFT == null)
        //        DisplayProperty("Send To Address After Minting", nft.SendToAddressAfterMinting, displayFieldLength);

        //    if ((web4NFT != null && nft.SendToAvatarAfterMintingId != web4NFT.SendToAvatarAfterMintingId) || web4NFT == null)
        //        DisplayProperty("Send To Avatar After Minting Id", nft.SendToAvatarAfterMintingId != Guid.Empty ? nft.SendToAvatarAfterMintingId.ToString() : "None", displayFieldLength);

        //    if ((web4NFT != null && nft.SendToAvatarAfterMintingUsername != web4NFT.SendToAvatarAfterMintingUsername) || web4NFT == null)
        //        DisplayProperty("Send To Avatar After Minting Username", !string.IsNullOrEmpty(nft.SendToAvatarAfterMintingUsername) ? nft.SendToAvatarAfterMintingUsername : "None", displayFieldLength);

        //    if ((web4NFT != null && displayTags && TagHelper.GetTags(nft.Tags) != TagHelper.GetTags(web4NFT.Tags)) || web4NFT == null)
        //        TagHelper.ShowTags(nft.Tags, displayFieldLength);

        //    if ((web4NFT != null && displayMetaData && MetaDataHelper.GetMetaData(nft.MetaData) != MetaDataHelper.GetMetaData(web4NFT.MetaData)) || web4NFT == null)
        //        MetaDataHelper.ShowMetaData(nft.MetaData, displayFieldLength);

        //    //CLIEngine.ShowDivider();
        //}


        protected async Task<OASISResult<ImageObjectResult>> ProcessImageOrObjectAsync(string holonType)
        {
            OASISResult<ImageObjectResult> result = new OASISResult<ImageObjectResult>(new ImageObjectResult());

            if (CLIEngine.GetConfirmation($"Would you rather use a 3D object or a 2D sprite/image to represent your {holonType} in Our World and other UI's? Press Y for 3D or N for 2D."))
            {
                Console.WriteLine("");

                if (CLIEngine.GetConfirmation("Would you like to upload a local 3D object from your device or input a URI to an online object? (Press Y for local or N for online)"))
                {
                    Console.WriteLine("");
                    string objPath = CLIEngine.GetValidFile("What is the full path to the local 3D object? (Press Enter if you wish to skip and use a default 3D object instead. You can always change this later.)");

                    if (objPath == "exit")
                    {
                        result.Message = "User Exited";
                        return result;
                    }

                    result.Result.Object3D = File.ReadAllBytes(objPath);

                }
                else
                {
                    Console.WriteLine("");
                    result.Result.Object3DURI = await CLIEngine.GetValidURIAsync("What is the URI to the 3D object? (Press Enter if you wish to skip and use a default 3D object instead. You can always change this later.)");

                    if (result.Result.Object3DURI == null)
                    {
                        result.Message = "User Exited";
                        return result;
                    }
                }
            }
            else
            {
                Console.WriteLine("");

                if (CLIEngine.GetConfirmation("Would you like to upload a local 2D sprite/image from your device or input a URI to an online sprite/image? (Press Y for local or N for online)"))
                {
                    Console.WriteLine("");
                    string imgPath = CLIEngine.GetValidFile("What is the full path to the local 2d sprite/image? (Press Enter if you wish to skip and use the default image instead. You can always change this later.)");

                    if (imgPath == "exit")
                    {
                        result.Message = "User Exited";
                        return result;
                    }

                    result.Result.Image2D = File.ReadAllBytes(imgPath);
                }
                else
                {
                    Console.WriteLine("");
                    result.Result.Image2DURI = await CLIEngine.GetValidURIAsync("What is the URI to the 2D sprite/image? (Press Enter if you wish to skip and use the default image instead. You can always change this later.)");

                    if (result.Result.Image2DURI == null)
                    {
                        result.Message = "User Exited";
                        return result;
                    }
                }
            }

            return result;
        }

        private OASISResult<IEnumerable<T>> ListStarHolons<T>(OASISResult<IEnumerable<T>> starHolons, bool showNumbers = false, bool showDetailedInfo = false) where T : ISTARNETHolon, new()
        {
            if (starHolons != null)
            {
                if (!starHolons.IsError)
                {
                    if (starHolons.Result != null && starHolons.Result.Count() > 0)
                    {
                        Console.WriteLine();

                        if (starHolons.Result.Count() == 1)
                            CLIEngine.ShowMessage($"{starHolons.Result.Count()} {STARNETManager.STARNETHolonUIName} Found:");
                        else
                            CLIEngine.ShowMessage($"{starHolons.Result.Count()} {STARNETManager.STARNETHolonUIName}s Found:");

                        for (int i = 0; i < starHolons.Result.Count(); i++)
                            ShowAsync(starHolons.Result.ElementAt(i), i == 0, true, showNumbers, i + 1, showDetailedInfo);
                    }
                    else
                        CLIEngine.ShowWarningMessage($"No {STARNETManager.STARNETHolonUIName}'s Found.");
                }
                else
                    CLIEngine.ShowErrorMessage($"Error occured loading {STARNETManager.STARNETHolonUIName}'s. Reason: {starHolons.Message}");
            }
            else
                CLIEngine.ShowErrorMessage($"Unknown error occured loading {STARNETManager.STARNETHolonUIName}'s.");

            return starHolons;
        }

        private void ListStarHolonsInstalled(OASISResult<IEnumerable<T3>> starHolons, bool showNumbers = false, bool showUninstallInfo = false)
        {
            if (starHolons != null)
            {
                if (!starHolons.IsError)
                {
                    if (starHolons.Result != null && starHolons.Result.Count() > 0)
                    {
                        Console.WriteLine();

                        if (starHolons.Result.Count() == 1)
                            CLIEngine.ShowMessage($"{starHolons.Result.Count()} {STARNETManager.STARNETHolonUIName} Found:");
                        else
                            CLIEngine.ShowMessage($"{starHolons.Result.Count()} {STARNETManager.STARNETHolonUIName}s Found:");

                        for (int i = 0; i < starHolons.Result.Count(); i++)
                            ShowInstalled(starHolons.Result.ElementAt(i), i == 0, true, showNumbers, i + 1, showUninstallInfo);
                    }
                    else
                        CLIEngine.ShowWarningMessage($"No {STARNETManager.STARNETHolonUIName}s Found.");
                }
                else
                    CLIEngine.ShowErrorMessage($"Error occured loading {STARNETManager.STARNETHolonUIName}'s. Reason: {starHolons.Message}");
            }
            else
                CLIEngine.ShowErrorMessage($"Unknown error occured loading {STARNETManager.STARNETHolonUIName}'s.");
        }
    }
}
