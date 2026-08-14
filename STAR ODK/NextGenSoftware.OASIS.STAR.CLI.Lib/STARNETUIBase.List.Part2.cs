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

        protected async Task<OASISResult<T1>> FininaliazePublishingAsync(BeginPublishResult pubParams, bool edit, bool registerOnSTARNET, bool generateOAPP, bool uploadOAPPToCloud, bool askToInstallAtEnd = true, ProviderType providerType = ProviderType.Default, ProviderType OAPPBinaryProviderType = ProviderType.Default)
        {
            OASISResult<T1> publishResult = new OASISResult<T1>();
            //OASISResult<string> pubPathResult = await GetPublishPathAsync(pubParams, edit, registerOnSTARNET, generateOAPP, uploadOAPPToCloud, providerType, OAPPBinaryProviderType);
            OASISResult<string> pubPathResult = await GetPublishPathAsync(pubParams.SourcePath, pubParams.SimpleWizard, edit, registerOnSTARNET, generateOAPP, uploadOAPPToCloud, providerType, OAPPBinaryProviderType);

            if (pubPathResult != null && !string.IsNullOrEmpty(pubPathResult.Result) && !pubPathResult.IsError)
            {
                publishResult = await STARNETManager.PublishAsync(STAR.BeamedInAvatar.Id, pubParams.SourcePath, pubParams.LaunchTarget, pubPathResult.Result, edit, registerOnSTARNET, generateOAPP, uploadOAPPToCloud, providerType, OAPPBinaryProviderType, pubParams.EmbedRuntimes, pubParams.EmbedLibs, pubParams.EmbedTemplates);
                await PostFininaliazePublishingAsync(publishResult, pubParams.SourcePath, askToInstallAtEnd, providerType);
            }
            else
                OASISErrorHandling.HandleError(ref publishResult, $"Error occured in STARNETUIBase.FininaliazePublishingAsync calling PreFininaliazePublishingAsync. Reason: {pubPathResult.Message}");

            return publishResult;
        }

        //protected async Task<OASISResult<string>> GetPublishPathAsync(BeginPublishResult pubParams, bool edit, bool registerOnSTARNET, bool generateOAPP, bool uploadOAPPToCloud, ProviderType providerType, ProviderType OAPPBinaryProviderType)
        protected async Task<OASISResult<string>> GetPublishPathAsync(string sourcePath, bool simpleWizard, bool edit, bool registerOnSTARNET, bool generateOAPP, bool uploadOAPPToCloud, ProviderType providerType, ProviderType OAPPBinaryProviderType)
        {
            OASISResult<string> result = new OASISResult<string>();
            string publishPath = "";

            if (Path.IsPathRooted(PublishedPath) || string.IsNullOrEmpty(STAR.STARDNA.STARNETBasePath))
                publishPath = PublishedPath;
            else
                publishPath = Path.Combine(STAR.STARDNA.STARNETBasePath, PublishedPath);

            if (!simpleWizard)
            {
                if (!CLIEngine.GetConfirmation($"Do you wish to publish the {STARNETManager.STARNETHolonUIName} to the default publish folder defined in the STARDNA as {PublishedSTARDNAKey} : {publishPath}?"))
                {
                    Console.WriteLine("");

                    if (CLIEngine.GetConfirmation($"Do you wish to publish the {STARNETManager.STARNETHolonUIName} to: {Path.Combine(sourcePath, "Published")}?"))
                        publishPath = Path.Combine(sourcePath, "Published");
                    else
                    {
                        Console.WriteLine("");
                        publishPath = CLIEngine.GetValidFolder($"Where do you wish to publish the {STARNETManager.STARNETHolonUIName}?", true);
                    }
                }
            }

            publishPath = new DirectoryInfo(publishPath).FullName;

            //Console.WriteLine("");
            CLIEngine.ShowWorkingMessage($"Publishing {STARNETManager.STARNETHolonUIName}...");
            result.Result = publishPath;
            return result;
        }

        protected async Task<OASISResult<T1>> PostFininaliazePublishingAsync(OASISResult<T1> publishResult, string sourcePath, bool askToInstallAtEnd = true, ProviderType providerType = ProviderType.Default)
        {

            if (publishResult != null && !publishResult.IsError && publishResult.Result != null)
            {
                await ShowAsync(publishResult.Result);

                if (askToInstallAtEnd && CLIEngine.GetConfirmation($"Do you wish to install the {STARNETManager.STARNETHolonUIName} now?"))
                    await DownloadAndInstallAsync(publishResult.Result.STARNETDNA.Id.ToString(), InstallMode.DownloadAndInstall, providerType);

                Console.WriteLine("");
            }
            else
            {
                //if (publishResult.Message.Contains("Please make sure you increment the version"))
                if (publishResult.Message.Contains(STARNETManager.STARNETDNAFileName))
                {
                    if (CLIEngine.GetConfirmation($"Do you wish to open the {STARNETManager.STARNETDNAFileName} file now?"))
                        Process.Start("explorer.exe", Path.Combine(sourcePath, STARNETManager.STARNETDNAFileName));
                }
                else
                    CLIEngine.ShowErrorMessage($"An error occured publishing the {STARNETManager.STARNETHolonUIName}. Reason: {publishResult.Message}");

                Console.WriteLine("");
            }

            return publishResult;
        }


        public virtual async Task UnpublishAsync(string idOrName = "", ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = await FindAsync("unpublish", idOrName, default, true, providerType: providerType);

            if (result != null && !result.IsError && result.Result != null)
            {
                OASISResult<T1> unpublishResult = await STARNETManager.UnpublishAsync(STAR.BeamedInAvatar.Id, result.Result, providerType);

                if (unpublishResult != null && !unpublishResult.IsError && unpublishResult.Result != null)
                {
                    CLIEngine.ShowSuccessMessage($"{STARNETManager.STARNETHolonUIName} Successfully Unpublished.");
                    await ShowAsync(result.Result);
                }
                else
                    CLIEngine.ShowErrorMessage($"An error occured unpublishing the {STARNETManager.STARNETHolonUIName}. Reason: {unpublishResult.Message}");
            }
        }

        public virtual async Task RepublishAsync(string idOrName = "", ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = await FindAsync("republish", idOrName, default, true, providerType: providerType);

            if (result != null && !result.IsError && result.Result != null)
            {
                OASISResult<T1> republishResult = await STARNETManager.RepublishAsync(STAR.BeamedInAvatar.Id, result.Result, providerType);

                if (republishResult != null && !republishResult.IsError && republishResult.Result != null)
                {
                    CLIEngine.ShowSuccessMessage($"{STARNETManager.STARNETHolonUIName} Successfully Republished.");
                    await ShowAsync(result.Result);
                }
                else
                    CLIEngine.ShowErrorMessage($"An error occured unpublishing the {STARNETManager.STARNETHolonUIName}. Reason: {republishResult.Message}");
            }
        }

        public virtual async Task ActivateAsync(string idOrName = "", ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = await FindAsync("activate", idOrName, default, true, providerType: providerType);

            if (result != null && !result.IsError && result.Result != null)
            {
                if (result.MetaData != null && result.MetaData.ContainsKey("Active") && result.MetaData["Active"] != null && result.MetaData["Active"] == "1")
                {
                    OASISResult<T1> activateResult = await STARNETManager.ActivateAsync(STAR.BeamedInAvatar.Id, result.Result, providerType);

                    if (activateResult != null && !activateResult.IsError && activateResult.Result != null)
                    {
                        CLIEngine.ShowSuccessMessage($"{STARNETManager.STARNETHolonUIName} Successfully Activated.");
                        await ShowAsync(result.Result);
                    }
                    else
                        CLIEngine.ShowErrorMessage($"An error occured activating the {STARNETManager.STARNETHolonUIName}. Reason: {activateResult.Message}");
                }
                else
                    CLIEngine.ShowErrorMessage($"The {STARNETManager.STARNETHolonUIName} is already activated!");
            }
        }

        public virtual async Task DeactivateAsync(string idOrName = "", ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = await FindAsync("deactivate", idOrName, default, true, providerType: providerType);

            if (result != null && !result.IsError && result.Result != null)
            {
                if (result.MetaData != null && result.MetaData.ContainsKey("Active") && result.MetaData["Active"] != null && result.MetaData["Active"] == "0")
                {
                    OASISResult<T1> deactivateResult = await STARNETManager.DeactivateAsync(STAR.BeamedInAvatar.Id, result.Result, providerType);

                    if (deactivateResult != null && !deactivateResult.IsError && deactivateResult.Result != null)
                    {
                        CLIEngine.ShowSuccessMessage($"{STARNETManager.STARNETHolonUIName} Successfully Deactivated.");
                        await ShowAsync(result.Result);
                    }
                    else
                        CLIEngine.ShowErrorMessage($"An error occured deactivating the {STARNETManager.STARNETHolonUIName}. Reason: {deactivateResult.Message}");
                }
                else
                    CLIEngine.ShowErrorMessage($"The {STARNETManager.STARNETHolonUIName} is already deactivated!");
            }
        }

        public virtual async Task<OASISResult<T3>> DownloadAndInstallAsync(string idOrName = "", InstallMode installMode = InstallMode.DownloadAndInstall, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T3> installResult = new OASISResult<T3>();
            string downloadPath = "";
            string installPath = "";
            bool simpleWizard = false;
            string operation = "install";

            if (installMode == InstallMode.DownloadOnly)
                operation = "download";

            if (Path.IsPathRooted(DownloadedPath) || string.IsNullOrEmpty(STAR.STARDNA.STARNETBasePath))
                downloadPath = DownloadedPath;
            else
                downloadPath = Path.Combine(STAR.STARDNA.STARNETBasePath, DownloadedPath);


            if (Path.IsPathRooted(InstalledPath) || string.IsNullOrEmpty(STAR.STARDNA.STARNETBasePath))
                installPath = InstalledPath;
            else
                installPath = Path.Combine(STAR.STARDNA.STARNETBasePath, InstalledPath);

            Console.WriteLine("");

            if (CLIEngine.GetConfirmation("Do you wish to launch the Simple or Advanced Wizard? The Simple Wizard will use defaults (recommended) but the Advanced Wizard will allow greater control and customisation. Press 'Y' for Simple or 'N' for Advanced."))
                simpleWizard = true;

            if (!simpleWizard)
            {
                Console.WriteLine("");

                if (!CLIEngine.GetConfirmation($"Do you wish to download the {STARNETManager.STARNETHolonUIName} to the default download folder defined in the STARDNA as {DownloadSTARDNAKey} : {downloadPath}?"))
                {
                    Console.WriteLine("");
                    downloadPath = CLIEngine.GetValidFolder($"What is the full path to where you wish to download the {STARNETManager.STARNETHolonUIName}?", true);
                }

                downloadPath = new DirectoryInfo(downloadPath).FullName;

                if (installMode != InstallMode.DownloadAndInstall)
                {
                    Console.WriteLine("");

                    if (!CLIEngine.GetConfirmation($"Do you wish to install the {STARNETManager.STARNETHolonUIName} to the default install folder defined in the STARDNA as {InstalledSTARDNAKey} : {installPath}?"))
                    {
                        Console.WriteLine("");
                        installPath = CLIEngine.GetValidFolder($"What is the full path to where you wish to install the {STARNETManager.STARNETHolonUIName}?", true);
                    }

                    installPath = new DirectoryInfo(installPath).FullName;
                }
            }

            if (!string.IsNullOrEmpty(idOrName))
            {
                Console.WriteLine("");
                OASISResult<T1> result = await FindForProviderAsync(operation, idOrName, false, false, true, providerType);

                if (result != null && result.Result != null && !result.IsError)
                {
                    if (result.MetaData != null && result.MetaData.ContainsKey("Reinstall") && !string.IsNullOrEmpty(result.MetaData["Reinstall"]) && result.MetaData["Reinstall"] == "1" && installMode == InstallMode.DownloadAndInstall)
                        installMode = InstallMode.DownloadAndReInstall;

                    installResult = await CheckIfInstalledAndInstallAsync(result.Result, downloadPath, installPath, installMode, "", providerType);
                }
            }
            else
            {
                Console.WriteLine("");
                if (installMode != InstallMode.DownloadOnly && CLIEngine.GetConfirmation($"Do you wish to install the {STARNETManager.STARNETHolonUIName} from a local .{STARNETManager.STARNETDNAFileName} file or from STARNET? Press 'Y' for local .{STARNETManager.STARNETDNAFileName} file or 'N' for STARNET."))
                {
                    Console.WriteLine("");
                    string oappPath = CLIEngine.GetValidFile($"What is the full path to the .{STARNETManager.STARNETDNAFileName} file?");

                    if (oappPath == "exit")
                        return installResult;

                    OASISResult<ISTARNETDNA> starHolonDNAResult = await STARNETManager.ReadDNAFromPublishedFileAsync<ISTARNETDNA>(oappPath);

                    if (starHolonDNAResult != null && starHolonDNAResult.Result != null && !starHolonDNAResult.IsError)
                    {
                        OASISResult<T1> starHolonResult = await STARNETManager.LoadAsync(STAR.BeamedInAvatar.Id, starHolonDNAResult.Result.Id, 0, providerType: providerType);

                        if (starHolonResult != null && starHolonResult.Result != null && !starHolonResult.IsError)
                        {
                            installMode = InstallMode.InstallOnly;

                            if (starHolonResult.MetaData != null && starHolonResult.MetaData.ContainsKey("Reinstall") && !string.IsNullOrEmpty(starHolonResult.MetaData["Reinstall"]) && starHolonResult.MetaData["Reinstall"] == "1")
                                installMode = InstallMode.ReInstall;

                            installResult = await CheckIfInstalledAndInstallAsync(starHolonResult.Result, downloadPath, installPath, installMode, oappPath, providerType);
                        }
                        else
                            CLIEngine.ShowErrorMessage($"The {STARNETManager.STARNETHolonUIName} could not be found for id {starHolonDNAResult.Result.Id} found in the STARNETDNA.json file. It could be corrupt or the id could be wrong, please check and try again, or create a new {STARNETManager.STARNETHolonUIName}.");
                    }
                    else
                        CLIEngine.ShowErrorMessage($"The {STARNETManager.STARNETHolonUIName} could not be found or is not valid! Please ensure it is in the folder you specified.");
                }
                else
                {
                    Console.WriteLine("");
                    OASISResult<T1> result = await FindForProviderAsync(operation, "", false, false, true, providerType);

                    if (result != null && result.Result != null && !result.IsError)
                    {
                        if (result.MetaData != null && result.MetaData.ContainsKey("Reinstall") && !string.IsNullOrEmpty(result.MetaData["Reinstall"]) && result.MetaData["Reinstall"] == "1" && installMode == InstallMode.DownloadAndInstall)
                            installMode = InstallMode.DownloadAndReInstall;

                        installResult = await CheckIfInstalledAndInstallAsync(result.Result, downloadPath, installPath, installMode, "", providerType);
                    }
                    else
                    {
                        installResult.Message = result.Message;
                        installResult.IsError = true;
                    }
                }
            }

            if (installResult != null)
            {
                if (!installResult.IsError && installResult.Result != null)
                {
                    ShowInstalled(installResult.Result);

                    if (CLIEngine.GetConfirmation($"Do you wish to open the folder to the {STARNETManager.STARNETHolonUIName} now?"))
                        STARNETManager.OpenSTARNETHolonFolder(STAR.BeamedInAvatar.Id, installResult.Result);
                    //await STARNETManager.OpenSTARNETHolonFolderAsync(STAR.BeamedInAvatar.Id, installResult.Result.STARNETDNA.Id, installResult.Result.STARNETDNA.Version);
                }
                else
                    CLIEngine.ShowErrorMessage($"Error {operation}ing {STARNETManager.STARNETHolonUIName}. Reason: {installResult.Message}");
            }
            else
                CLIEngine.ShowErrorMessage($"Error {operation}ing {STARNETManager.STARNETHolonUIName}. Reason: Unknown error occured!");

            Console.WriteLine("");
            return installResult;
        }

        public virtual OASISResult<T3> DownloadAndInstall(string idOrName = "", ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T3> installResult = new OASISResult<T3>();
            string downloadPath = "";
            string installPath = "";

            if (Path.IsPathRooted(DownloadedPath) || string.IsNullOrEmpty(STAR.STARDNA.STARNETBasePath))
                downloadPath = SourcePath;
            else
                downloadPath = Path.Combine(STAR.STARDNA.STARNETBasePath, DownloadedPath);


            if (Path.IsPathRooted(InstalledPath) || string.IsNullOrEmpty(STAR.STARDNA.STARNETBasePath))
                installPath = SourcePath;
            else
                installPath = Path.Combine(STAR.STARDNA.STARNETBasePath, InstalledPath);

            Console.WriteLine("");

            if (!CLIEngine.GetConfirmation($"Do you wish to download the {STARNETManager.STARNETHolonUIName} to the default download folder defined in the STARDNA as {DownloadSTARDNAKey} : {downloadPath}?"))
            {
                Console.WriteLine("");
                downloadPath = CLIEngine.GetValidFolder($"What is the full path to where you wish to download the {STARNETManager.STARNETHolonUIName}?", true);
            }

            downloadPath = new DirectoryInfo(downloadPath).FullName;

            Console.WriteLine("");

            if (!CLIEngine.GetConfirmation($"Do you wish to install the {STARNETManager.STARNETHolonUIName} to the default install folder defined in the STARDNA as {DownloadSTARDNAKey} : {installPath}?"))
            {
                Console.WriteLine("");
                installPath = CLIEngine.GetValidFolder($"What is the full path to where you wish to install the {STARNETManager.STARNETHolonUIName}?", true);
            }

            installPath = new DirectoryInfo(installPath).FullName;

            if (!string.IsNullOrEmpty(idOrName))
            {
                Console.WriteLine("");
                OASISResult<T1> result = FindForProvider("install", idOrName, false, false, true, providerType);

                if (result != null && result.Result != null && !result.IsError)
                    installResult = STARNETManager.DownloadAndInstall(STAR.BeamedInAvatar.Id, result.Result, installPath, downloadPath, true, false, providerType);
            }
            else
            {
                Console.WriteLine("");
                if (CLIEngine.GetConfirmation($"Do you wish to install the {STARNETManager.STARNETHolonUIName} from a local .{STARNETManager.STARNETDNAFileName} file or from STARNET? Press 'Y' for local .{STARNETManager.STARNETDNAFileName} file or 'N' for STARNET."))
                {
                    Console.WriteLine("");
                    string oappPath = CLIEngine.GetValidFile($"What is the full path to the {STARNETManager.STARNETDNAFileName} file?");

                    if (oappPath == "exit")
                        return installResult;

                    installResult = STARNETManager.Install(STAR.BeamedInAvatar.Id, oappPath, installPath, true, null, false, providerType);
                }
                else
                {
                    Console.WriteLine("");
                    CLIEngine.ShowWorkingMessage($"Loading {STARNETManager.STARNETHolonUIName}s...");
                    OASISResult<IEnumerable<T1>> starHolonsResult = ListAll();

                    if (starHolonsResult != null && starHolonsResult.Result != null && !starHolonsResult.IsError && starHolonsResult.Result.Count() > 0)
                    {
                        OASISResult<T1> result = FindForProvider("", "install", false, false, true, providerType);

                        if (result != null && result.Result != null && !result.IsError)
                            installResult = STARNETManager.DownloadAndInstall(STAR.BeamedInAvatar.Id, result.Result, installPath, downloadPath, true, false, providerType);
                        else
                        {
                            installResult.Message = result.Message;
                            installResult.IsError = true;
                        }
                    }
                    else
                    {
                        installResult.Message = $"No {STARNETManager.STARNETHolonUIName}s found to install.";
                        installResult.IsError = true;
                    }
                }
            }

            if (installResult != null)
            {
                if (!installResult.IsError && installResult.Result != null)
                {
                    ShowInstalled(installResult.Result);

                    if (CLIEngine.GetConfirmation($"Do you wish to open the folder to the {STARNETManager.STARNETHolonUIName} now?"))
                        STARNETManager.OpenSTARNETHolonFolder(STAR.BeamedInAvatar.Id, installResult.Result);
                }
                else
                    CLIEngine.ShowErrorMessage($"Error installing {STARNETManager.STARNETHolonUIName}. Reason: {installResult.Message}");
            }
            else
                CLIEngine.ShowErrorMessage($"Error installing {STARNETManager.STARNETHolonUIName}. Reason: Unknown error occured!");

            Console.WriteLine("");
            return installResult;
        }
    }
}
