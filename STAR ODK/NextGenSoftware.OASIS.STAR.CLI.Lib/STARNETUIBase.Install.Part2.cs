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
    }
}
