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
        where T1 : ISTARNETHolon, new()
        where T2 : IDownloadedSTARNETHolon, new()
        where T3 : IInstalledSTARNETHolon, new()
        where T4 : ISTARNETDNA, new()
    {
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

        private async Task<OASISResult<T>> CheckIfAlreadyInstalledAsync<T>(T holon, ProviderType providerType = ProviderType.Default) where T : ISTARNETHolon, new()
        {
            OASISResult<T> result = new OASISResult<T>();
            OASISResult<bool> oappInstalledResult = await STARNETManager.IsInstalledAsync(STAR.BeamedInAvatar.Id, holon.STARNETDNA.Id, holon.STARNETDNA.Version, providerType);

            if (oappInstalledResult != null && !oappInstalledResult.IsError)
            {
                if (oappInstalledResult.Result)
                {
                    Console.WriteLine("");
                    CLIEngine.ShowWarningMessage($"You have already installed this version (v{holon.STARNETDNA.Version}). Please uninstall before attempting to re-install.");

                    if (CLIEngine.GetConfirmation($"Do you wish to uninstall the {STARNETManager.STARNETHolonUIName} now? Press 'Y' for Yes or 'N' for No."))
                    {
                        Console.WriteLine("");
                        CLIEngine.ShowWorkingMessage($"Uninstalling {STARNETManager.STARNETHolonUIName}...");
                        OASISResult<T3> uninstallResult = await STARNETManager.UninstallAsync(STAR.BeamedInAvatar.Id, holon.STARNETDNA.Id, holon.STARNETDNA.Version, providerType);

                        if (uninstallResult != null && uninstallResult.Result != null && !uninstallResult.IsError)
                        {
                            CLIEngine.ShowSuccessMessage($"{STARNETManager.STARNETHolonUIName} Successfully Uninstalled.");
                            result.MetaData["Reinstall"] = "1";
                        }
                        else
                            OASISErrorHandling.HandleError(ref result, $"Error occured uninstalling the {STARNETManager.STARNETHolonUIName}! Reason: {uninstallResult.Message}");
                    }
                    else
                    {
                        result.IsError = true;
                        result.Message = "User Denied Uninstall";
                        Console.WriteLine("");
                    }
                }
            }
            else
                OASISErrorHandling.HandleError(ref result, ($"Error occured checking if the {STARNETManager.STARNETHolonUIName} is already installed! Reason: {oappInstalledResult.Message}"));

            return result;
        }

        private OASISResult<T1> CheckIfAlreadyInstalled(T1 holon, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            OASISResult<bool> oappInstalledResult = STARNETManager.IsInstalled(STAR.BeamedInAvatar.Id, holon.STARNETDNA.Id, holon.STARNETDNA.Version, providerType);

            if (oappInstalledResult != null && !oappInstalledResult.IsError)
            {
                if (oappInstalledResult.Result)
                {
                    Console.WriteLine("");
                    CLIEngine.ShowWarningMessage($"You have already installed this version (v{holon.STARNETDNA.Version}). Please uninstall before attempting to re-install.");

                    if (CLIEngine.GetConfirmation($"Do you wish to uninstall the {STARNETManager.STARNETHolonUIName} now? Press 'Y' for Yes or 'N' for No."))
                    {
                        Console.WriteLine("");
                        CLIEngine.ShowWorkingMessage($"Uninstalling {STARNETManager.STARNETHolonUIName}...");
                        OASISResult<T3> uninstallResult = STARNETManager.Uninstall(STAR.BeamedInAvatar.Id, result.Result.STARNETDNA.Id, result.Result.STARNETDNA.Version, providerType);

                        if (uninstallResult != null && uninstallResult.Result != null && !uninstallResult.IsError)
                        {
                            CLIEngine.ShowSuccessMessage($"{STARNETManager.STARNETHolonUIName} Successfully Uninstalled.");
                            result.MetaData["Reinstall"] = "1";
                        }
                        else
                            OASISErrorHandling.HandleError(ref result, $"Error occured uninstalling the {STARNETManager.STARNETHolonUIName}! Reason: {uninstallResult.Message}");
                    }
                    else
                    {
                        result.IsError = true;
                        result.Message = "User Denied Uninstall";
                        Console.WriteLine("");
                    }
                }
            }
            else
                OASISErrorHandling.HandleError(ref result, ($"Error occured checking if the {STARNETManager.STARNETHolonUIName} is already installed! Reason: {oappInstalledResult.Message}"));

            return result;
        }

        private async Task<OASISResult<T3>> CheckIfInstalledAndInstallAsync(T1 holon, string downloadPath, string installPath, InstallMode installMode, string fullPathToPublishedFile = "", ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T3> installResult = new OASISResult<T3>();
            bool continueInstall = false;

            if (holon != null)
            {
                if (installMode != InstallMode.DownloadOnly)
                {
                    OASISResult<T1> checkResult = await CheckIfAlreadyInstalledAsync(holon, providerType);

                    if (checkResult != null && !checkResult.IsError)
                        continueInstall = true;
                    else
                        CLIEngine.ShowErrorMessage($"Error checking if the {STARNETManager.STARNETHolonUIName} is already installed! Reason: {checkResult.MetaData}");
                }
            }

            if (continueInstall)
                installResult = await InstallAsync(holon, downloadPath, installPath, installMode, fullPathToPublishedFile, providerType);

            if (installResult != null && installResult.IsError && installResult.Message.Contains("is not published"))
            {
                if (holon.STARNETDNA.CreatedByAvatarId == STAR.BeamedInAvatar.Id)
                {
                    if (CLIEngine.GetConfirmation("Would you like to publish it now?"))
                    {
                        Console.WriteLine("");
                        //OASISResult<bool> publishResult = await STARNETManager.PublishAsync(STAR.BeamedInAvatar.Id, holon.STARNETDNA.Id, holon.STARNETDNA.VersionSequence, providerType);
                        OASISResult<T1> publishResult = await PublishAsync(holon.STARNETDNA.SourcePath, defaultLaunchMode: DefaultLaunchMode.Optional, askToInstallAtEnd: false, providerType: providerType);

                        if (!(publishResult != null && !publishResult.IsError && publishResult.Result != null))
                            CLIEngine.ShowErrorMessage($"Error publishing the {STARNETManager.STARNETHolonUIName} before installing it! Reason: {publishResult.Message}");
                        else
                        {
                            installResult.IsError = false;
                            installResult.Message = "";
                        }
                        //The publish routine automatically installs at the end(if the user agrees) so no need to install again here.
                        if (publishResult != null && !publishResult.IsError && publishResult.Result != null)
                            installResult = await InstallAsync(holon, downloadPath, installPath, installMode, fullPathToPublishedFile, providerType);
                        else
                            CLIEngine.ShowErrorMessage($"Error publishing the {STARNETManager.STARNETHolonUIName} before installing it! Reason: {publishResult.Message}");
                    }
                    else
                        Console.WriteLine("");
                }
            }

            return installResult;
        }

        private OASISResult<T3> CheckIfInstalledAndInstall(T1 holon, string downloadPath, string installPath, InstallMode installMode, string fullPathToPublishedFile = "", ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T3> installResult = new OASISResult<T3>();
            bool continueInstall = false;

            if (holon != null)
            {
                if (installMode != InstallMode.DownloadOnly)
                {
                    OASISResult<T1> checkResult = CheckIfAlreadyInstalled(holon, providerType);

                    if (checkResult != null && !checkResult.IsError)
                        continueInstall = true;
                    else
                        CLIEngine.ShowErrorMessage($"Error checking if the {STARNETManager.STARNETHolonUIName} is already installed! Reason: {checkResult.MetaData}");
                }
            }

            if (continueInstall)
                installResult = Install(holon, downloadPath, installPath, installMode, fullPathToPublishedFile, providerType);

            return installResult;
        }

        protected async Task<OASISResult<T3>> InstallAsync(T1 starHolon, string downloadPath, string installPath, InstallMode installMode, string fullPathToPublishedFile = "", ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T3> result = new OASISResult<T3>();
            //OASISResult<bool> publishResult = await STARNETManager.IsPublishedAsync(STAR.BeamedInAvatar.Id, starHolon.STARNETDNA.Id, starHolon.STARNETDNA.VersionSequence, providerType);
            //OASISResult<bool> publishResult = await STARNETManager.IsPublishedAsync(STAR.BeamedInAvatar.Id, starHolon.STARNETDNA.Id, starHolon.MetaData["Version"].ToString(), providerType);
            OASISResult<bool> publishResult = await STARNETManager.IsPublishedAsync(STAR.BeamedInAvatar.Id, starHolon.STARNETDNA.Id, starHolon.STARNETDNA.Version, providerType);

            if (publishResult != null && !publishResult.IsError)
            {
                if (!publishResult.Result)
                {
                    OASISErrorHandling.HandleError(ref result, $"The {STARNETManager.STARNETHolonUIName} is not published and cannot be installed. Please publish it first.");
                    return result;
                }
            }
            else
            {
                OASISErrorHandling.HandleError(ref result, $"Error checking if {STARNETManager.STARNETHolonUIName} is published. Reason: {publishResult.Message}");
                return result;
            }

            switch (installMode)
            {
                case InstallMode.DownloadAndInstall:
                    result = await STARNETManager.DownloadAndInstallAsync(STAR.BeamedInAvatar.Id, starHolon, installPath, downloadPath, true, false, providerType);
                    break;

                case InstallMode.DownloadOnly:
                    {
                        OASISResult<T2> downloadResult = await STARNETManager.DownloadAsync(STAR.BeamedInAvatar.Id, starHolon, downloadPath, false, providerType);

                        if (downloadResult != null && downloadResult.Result != null && !downloadResult.IsError)
                        {
                            result.Result = new T3() { STARNETDNA = downloadResult.Result.STARNETDNA };
                            result.Result.DownloadedOn = downloadResult.Result.DownloadedOn;
                            result.Result.DownloadedBy = downloadResult.Result.DownloadedBy;
                            result.Result.DownloadedByAvatarUsername = downloadResult.Result.DownloadedByAvatarUsername;
                            result.Result.DownloadedPath = downloadResult.Result.DownloadedPath;
                        }
                        else
                        {
                            result.Message = downloadResult.Message;
                            result.IsError = true;
                        }
                    }
                    break;

                case InstallMode.InstallOnly:
                    result = await STARNETManager.InstallAsync(STAR.BeamedInAvatar.Id, fullPathToPublishedFile, installPath, true, null, false, providerType);
                    break;

                case InstallMode.DownloadAndReInstall:
                    result = await STARNETManager.DownloadAndInstallAsync(STAR.BeamedInAvatar.Id, starHolon, installPath, downloadPath, true, true, providerType);
                    break;

                case InstallMode.ReInstall:
                    result = await STARNETManager.InstallAsync(STAR.BeamedInAvatar.Id, fullPathToPublishedFile, installPath, true, null, true, providerType);
                    break;
            }

            return result;
        }

        //protected void ShowMetaData(Dictionary<string, object> metaData)
        //{
        //    if (metaData != null)
        //    {
        //        CLIEngine.ShowMessage($"MetaData:");

        //        foreach (string key in metaData.Keys)
        //            CLIEngine.ShowMessage(string.Concat("          key = ", GetMetaValue(metaData[key])), false);
        //    }
        //    else
        //        CLIEngine.ShowMessage($"MetaData: None");
        //}

        //private string GetMetaValue(object value)
        //{
        //    return value != null ? IsBinary(value) ? "<binary>" : value.ToString() : "None";
        //}

        //protected bool IsBinary(object data)
        //{
        //    if (data == null)
        //        return false;

        //    if (data is byte[])
        //        return true;

        //    try
        //    {
        //        byte[] binaryData = Convert.FromBase64String(data.ToString());

        //        for (int i = 0; i < binaryData.Length; i++)
        //        {
        //            if (binaryData[i] > 127)
        //                return true;
        //        }
        //    }
        //    catch { }

        //    return false;
        //}

        //protected Dictionary<string, object> AddMetaData(string holonName)
        //{
        //    Dictionary<string, object> metaData = new Dictionary<string, object>();

        //    if (CLIEngine.GetConfirmation($"Do you wish to add any metadata to this {holonName}?"))
        //    {
        //        metaData = AddItemToMetaData(metaData);
        //        bool metaDataDone = false;

        //        do
        //        {
        //            if (CLIEngine.GetConfirmation("Do you wish to add more metadata?"))
        //                metaData = AddItemToMetaData(metaData);
        //            else
        //                metaDataDone = true;
        //        }
        //        while (!metaDataDone);
        //    }

        //    return metaData;
        //}

        //protected Dictionary<string, object> AddItemToMetaData(Dictionary<string, object> metaData)
        //{
        //    Console.WriteLine("");
        //    string key = CLIEngine.GetValidInput("What is the key?");
        //    string value = "";
        //    byte[] metaFile = null;

        //    if (CLIEngine.GetConfirmation("Is the value a file?"))
        //    {
        //        Console.WriteLine("");
        //        string metaPath = CLIEngine.GetValidFile("What is the full path to the file?");
        //        metaFile = File.ReadAllBytes(metaPath);
        //    }
        //    else
        //    {
        //        Console.WriteLine("");
        //        value = CLIEngine.GetValidInput("What is the value?");
        //    }

        //    if (metaFile != null)
        //        metaData[key] = metaFile;
        //    else
        //        metaData[key] = value;

        //    return metaData;
        //}

        //protected Dictionary<string, object> ManageMetaData(Dictionary<string, object> metaData, string itemName)
        //{
        //    if (metaData == null)
        //        metaData = new Dictionary<string, object>();

        //    bool done = false;

        //    while (!done)
        //    {
        //        Console.WriteLine("");
        //        CLIEngine.ShowMessage($"Current {itemName} metadata:", false);

        //        if (metaData.Count == 0)
        //            CLIEngine.ShowMessage("  None", false);
        //        else
        //        {
        //            int i = 1;
        //            foreach (var kv in metaData)
        //            {
        //                CLIEngine.ShowMessage($"  {i}. {kv.Key} = {GetMetaValue(kv.Value)}", false);
        //                i++;
        //            }
        //        }

        //        Console.WriteLine("");
        //        CLIEngine.ShowMessage("Choose an action: (A)dd, (E)dit, (D)elete, (Q)uit", false);
        //        string choice = CLIEngine.GetValidInput("Enter A, E, D or Q:").ToUpper();

        //        switch (choice)
        //        {
        //            case "A":
        //                metaData = AddItemToMetaData(metaData);
        //                break;

        //            case "E":
        //                if (metaData.Count == 0)
        //                {
        //                    CLIEngine.ShowErrorMessage("No metadata to edit.");
        //                    break;
        //                }

        //                int editIndex = CLIEngine.GetValidInputForInt("Enter the number of the metadata entry to edit:", true, 1, metaData.Count);
        //                string editKey = metaData.Keys.ElementAt(editIndex - 1);
        //                object currentValue = metaData[editKey];

        //                if (currentValue is byte[])
        //                {
        //                    if (CLIEngine.GetConfirmation("This value is binary. Do you want to replace it with a file? (Y) or replace with text (N)?"))
        //                    {
        //                        string metaPath = CLIEngine.GetValidFile("What is the full path to the file?");
        //                        metaData[editKey] = File.ReadAllBytes(metaPath);
        //                    }
        //                    else
        //                    {
        //                        string newValue = CLIEngine.GetValidInput("Enter the new text value (or type 'clear' to remove):", addLineBefore: true);
        //                        if (newValue.ToLower() == "clear")
        //                            metaData.Remove(editKey);
        //                        else
        //                            metaData[editKey] = newValue;
        //                    }
        //                }
        //                else
        //                {
        //                    if (CLIEngine.GetConfirmation("Do you want to set this value from a file? (Y) or enter text value (N)?"))
        //                    {
        //                        string metaPath = CLIEngine.GetValidFile("What is the full path to the file?");
        //                        metaData[editKey] = File.ReadAllBytes(metaPath);
        //                    }
        //                    else
        //                    {
        //                        string newValue = CLIEngine.GetValidInput("Enter the new text value (or type 'clear' to remove):");
        //                        if (newValue.ToLower() == "clear")
        //                            metaData.Remove(editKey);
        //                        else
        //                            metaData[editKey] = newValue;
        //                    }
        //                }

        //                break;

        //            case "D":
        //                if (metaData.Count == 0)
        //                {
        //                    CLIEngine.ShowErrorMessage("No metadata to delete.");
        //                    break;
        //                }

        //                int delIndex = CLIEngine.GetValidInputForInt("Enter the number of the metadata entry to delete:", true, 1, metaData.Count);
        //                string delKey = metaData.Keys.ElementAt(delIndex - 1);

        //                if (CLIEngine.GetConfirmation($"Are you sure you want to delete metadata '{delKey}'?"))
        //                {
        //                    metaData.Remove(delKey);
        //                    CLIEngine.ShowSuccessMessage($"Metadata '{delKey}' deleted.", addLineBefore: true);
        //                }
        //                else
        //                    Console.WriteLine("");

        //                break;

        //            case "Q":
        //                done = true;
        //                break;

        //            default:
        //                CLIEngine.ShowErrorMessage("Invalid choice. Please enter A, E, D or Q.");
        //                break;
        //        }
        //    }

        //    return metaData;
        //}

        //protected void DisplayMetaData(Dictionary<string, object> metaData)
        //{
        //    foreach (string key in metaData.Keys)
        //        CLIEngine.ShowMessage(string.Concat("          key = ", metaData[key] is byte[]? "<binary>" : metaData[key]), false);
        //}

        private OASISResult<T3> Install(T1 starHolon, string downloadPath, string installPath, InstallMode installMode, string fullPathToPublishedFile = "", ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T3> result = new OASISResult<T3>();
            OASISResult<bool> publishResult = STARNETManager.IsPublished(STAR.BeamedInAvatar.Id, starHolon.STARNETDNA.Id, starHolon.STARNETDNA.VersionSequence, providerType);

            if (publishResult != null && !publishResult.IsError)
            {
                if (!publishResult.Result)
                {
                    OASISErrorHandling.HandleError(ref result, $"The {STARNETManager.STARNETHolonUIName} is not published and cannot be installed. Please publish it first.");
                    return result;
                }
            }
            else
            {
                OASISErrorHandling.HandleError(ref result, $"Error checking if {STARNETManager.STARNETHolonUIName} is published. Reason: {publishResult.Message}");
                return result;
            }

            switch (installMode)
            {
                case InstallMode.DownloadAndInstall:
                    result = STARNETManager.DownloadAndInstall(STAR.BeamedInAvatar.Id, starHolon, installPath, downloadPath, true, false, providerType);
                    break;

                case InstallMode.DownloadOnly:
                    {
                        OASISResult<T2> downloadResult = STARNETManager.Download(STAR.BeamedInAvatar.Id, starHolon, downloadPath, false, providerType);

                        if (downloadResult != null && downloadResult.Result != null && !downloadResult.IsError)
                        {
                            result.Result = new T3() { STARNETDNA = downloadResult.Result.STARNETDNA };
                            result.Result.DownloadedOn = downloadResult.Result.DownloadedOn;
                            result.Result.DownloadedBy = downloadResult.Result.DownloadedBy;
                            result.Result.DownloadedByAvatarUsername = downloadResult.Result.DownloadedByAvatarUsername;
                            result.Result.DownloadedPath = downloadResult.Result.DownloadedPath;
                        }
                        else
                        {
                            result.Message = downloadResult.Message;
                            result.IsError = true;
                        }
                    }
                    break;

                case InstallMode.InstallOnly:
                    result = STARNETManager.Install(STAR.BeamedInAvatar.Id, fullPathToPublishedFile, installPath, true, null, false, providerType);
                    break;

                case InstallMode.DownloadAndReInstall:
                    result = STARNETManager.DownloadAndInstall(STAR.BeamedInAvatar.Id, starHolon, installPath, downloadPath, true, true, providerType);
                    break;

                case InstallMode.ReInstall:
                    result = STARNETManager.Install(STAR.BeamedInAvatar.Id, fullPathToPublishedFile, installPath, true, null, true, providerType);
                    break;
            }

            return result;
        }
        
        /// <summary>STARNET DNA category can deserialize as <see cref="JsonElement"/> or other CLR types; avoid printing <c>ValueKind=...</c> to the user.</summary>
        private static string FormatStarnetDnaCategoryForDisplay(object? raw)
        {
            if (raw == null) return "None";
            if (raw is JsonElement je)
            {
                switch (je.ValueKind)
                {
                    case JsonValueKind.String:
                        return je.GetString() ?? "None";
                    case JsonValueKind.Number:
                        if (je.TryGetInt32(out var i32))
                            return i32.ToString(CultureInfo.InvariantCulture);
                        if (je.TryGetInt64(out var i64))
                            return i64.ToString(CultureInfo.InvariantCulture);
                        return je.GetRawText();
                    case JsonValueKind.Null:
                    case JsonValueKind.Undefined:
                        return "None";
                    default:
                        return je.GetRawText();
                }
            }

            var s = raw.ToString();
            if (string.IsNullOrEmpty(s))
                return "None";

            // Some JSON deserialization/serialization paths end up stringifying JsonElement
            // as an object like `{ "ValueKind": 3 }` (losing the actual value).
            // Convert this into a readable JsonValueKind name instead of printing the blob.
            try
            {
                if (s.Contains("\"ValueKind\"", StringComparison.Ordinal) && s.TrimStart().StartsWith("{"))
                {
                    using var doc = JsonDocument.Parse(s);
                    if (doc.RootElement.ValueKind == JsonValueKind.Object &&
                        doc.RootElement.TryGetProperty("ValueKind", out JsonElement vk))
                    {
                        if (vk.ValueKind == JsonValueKind.Number && vk.TryGetInt32(out int i))
                            return ((JsonValueKind)i).ToString();
                        if (vk.ValueKind == JsonValueKind.String)
                            return vk.GetString() ?? "None";
                    }
                }
            }
            catch
            {
                // Fall through to raw string output.
            }

            return s;
        }

        private T1 ConvertFromT3ToT1(T3 holon)
        {
            T1 newHolon = new T1();
            newHolon.STARNETDNA = holon.STARNETDNA;
            newHolon.MetaData = holon.MetaData;
            return newHolon;
        }

        private void OnPublishStatusChanged(object sender, STARNETHolonPublishStatusEventArgs e)
        {
            switch (e.Status)
            {
                case STARNETHolonPublishStatus.DotNetPublishing:
                    CLIEngine.ShowWorkingMessage("DotNet Publishing...");
                    break;

                case STARNETHolonPublishStatus.Uploading:
                    CLIEngine.ShowMessage("Uploading...");
                    Console.WriteLine("");
                    break;

                case STARNETHolonPublishStatus.Published:
                    CLIEngine.ShowSuccessMessage($"{STARNETManager.STARNETHolonUIName} Published Successfully");
                    break;

                case STARNETHolonPublishStatus.Error:
                    CLIEngine.ShowErrorMessage(e.ErrorMessage);
                    break;

                default:
                    CLIEngine.ShowWorkingMessage($"{Enum.GetName(typeof(STARNETHolonPublishStatus), e.Status)}...");
                    break;
            }
        }

        private void OnUploadStatusChanged(object sender, STARNETHolonUploadProgressEventArgs e)
        {
            CLIEngine.ShowProgressBar((double)e.Progress / (double)100);
        }

        private void OnInstallStatusChanged(object sender, STARNETHolonInstallStatusEventArgs e)
        {
            switch (e.Status)
            {
                case STARNETHolonInstallStatus.Downloading:
                    CLIEngine.ShowMessage($"Downloading {e.STARNETDNA.Name} v{e.STARNETDNA.Version}...");
                    Console.WriteLine("");
                    break;

                case STARNETHolonInstallStatus.Installing:
                    CLIEngine.ShowWorkingMessage($"Installing {e.STARNETDNA.Name} v{e.STARNETDNA.Version}...");
                    break;

                case STARNETHolonInstallStatus.InstallingDependencies:
                    CLIEngine.ShowWorkingMessage("Installing Dependencies (Smartbricks)...");
                    break;

                case STARNETHolonInstallStatus.InstallingRuntimes:
                    CLIEngine.ShowWorkingMessage("Installing Runtimes...");
                    break;

                case STARNETHolonInstallStatus.InstallingLibs:
                    CLIEngine.ShowWorkingMessage("Installing Libs...");
                    break;

                case STARNETHolonInstallStatus.InstallingTemplates:
                    CLIEngine.ShowWorkingMessage("Installing Templates...");
                    break;

                case STARNETHolonInstallStatus.Installed:
                    CLIEngine.ShowSuccessMessage($"{e.STARNETDNA.Name} v{e.STARNETDNA.Version} Installed Successfully");
                    break;

                case STARNETHolonInstallStatus.Error:
                    CLIEngine.ShowErrorMessage(e.ErrorMessage);
                    break;

                default:
                    CLIEngine.ShowWorkingMessage($"{Enum.GetName(typeof(STARNETHolonInstallStatus), e.Status)}...");
                    break;
            }
        }

        private void OnDownloadStatusChanged(object sender, STARNETHolonDownloadProgressEventArgs e)
        {
            CLIEngine.ShowProgressBar((double)e.Progress / (double)100);
        }
    }
}