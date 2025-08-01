﻿using System.Diagnostics;
using NextGenSoftware.CLI.Engine;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.STAR.CLI.Lib.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Holons;
using NextGenSoftware.OASIS.API.ONODE.Core.Enums.STARNETHolon;
using NextGenSoftware.OASIS.API.ONODE.Core.Events.STARNETHolon;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Managers;

namespace NextGenSoftware.OASIS.STAR.CLI.Lib
{
    public class STARNETUIBase<T1, T2, T3, T4>
        where T1 : ISTARNETHolon, new()
        where T2 : IDownloadedSTARNETHolon, new()
        where T3 : IInstalledSTARNETHolon, new()
        where T4 : ISTARNETDNA, new()
    {
        protected const int DEFAULT_FIELD_LENGTH = 35;

        public virtual ISTARNETManagerBase<T1, T2, T3, T4> STARNETManager { get; set; }
        public virtual bool IsInit { get; set; }
        public virtual string CreateHeader { get; set; }
        public virtual List<string> CreateIntroParagraphs { get; set; }
        public virtual string SourcePath { get; set; }
        public virtual string SourceSTARDNAKey { get; set; }
        public virtual string PublishedPath { get; set; }
        public virtual string PublishedSTARDNAKey { get; set; }
        public virtual string DownloadedPath { get; set; }
        public virtual string DownloadSTARDNAKey { get; set; }
        public virtual string InstalledPath { get; set; }
        public virtual string InstalledSTARDNAKey { get; set; }

        public int DisplayFieldLength { get; set; } = DEFAULT_FIELD_LENGTH;
        

        public STARNETUIBase(ISTARNETManagerBase<T1, T2, T3, T4> starManager, string createHeader, List<string> createIntroParagraphs, string sourcePath = "", string sourceSTARDNAKey = "", string publishedPath = "", string publishedSTARDNAKey = "", string downloadedPath = "", string downloadSTARDNAKey = "", string installedPath = "", string installedSTARDNAKey = "", int displayFieldLength = DEFAULT_FIELD_LENGTH)
        {
            starManager.OnDownloadStatusChanged += OnDownloadStatusChanged;
            starManager.OnInstallStatusChanged += OnInstallStatusChanged;
            starManager.OnPublishStatusChanged += OnPublishStatusChanged;
            starManager.OnUploadStatusChanged += OnUploadStatusChanged;

            CreateHeader = createHeader;
            CreateIntroParagraphs = createIntroParagraphs;
            IsInit = true;
            STARNETManager = starManager;
            SourcePath = sourcePath;
            SourceSTARDNAKey = sourceSTARDNAKey;
            PublishedPath = publishedPath;
            PublishedSTARDNAKey = publishedSTARDNAKey;
            DownloadedPath = downloadedPath;
            DownloadSTARDNAKey = downloadSTARDNAKey;
            InstalledPath = installedPath;
            InstalledSTARDNAKey = installedSTARDNAKey;
            DisplayFieldLength = displayFieldLength;
        }

        public virtual void Dispose()
        {
            STARNETManager.OnDownloadStatusChanged -= OnDownloadStatusChanged;
            STARNETManager.OnInstallStatusChanged -= OnInstallStatusChanged;
            STARNETManager.OnPublishStatusChanged -= OnPublishStatusChanged;
            STARNETManager.OnUploadStatusChanged -= OnUploadStatusChanged;
        }

        public virtual async Task<OASISResult<T1>> CreateAsync(object createParams, T1 newHolon = default, bool showHeaderAndInro = true, bool checkIfSourcePathExists = true, object holonSubType = null, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            
            if (showHeaderAndInro)
                ShowHeader();

            string holonName = CLIEngine.GetValidInput($"What is the name of the {STARNETManager.STARNETHolonUIName}?");

            if (holonName == "exit")
            {
                result.Message = "User Exited";
                return result;
            }
               

            string holonDesc = CLIEngine.GetValidInput($"What is the description of the {STARNETManager.STARNETHolonUIName}?");

            if (holonDesc == "exit")
            {
                result.Message = "User Exited";
                return result;
            }

            if (holonSubType == null)
                holonSubType = CLIEngine.GetValidInputForEnum($"What type of {STARNETManager.STARNETHolonUIName} do you wish to create?", STARNETManager.STARNETHolonSubType);

            if (holonSubType != null)
            {
                if (holonSubType.ToString() == "exit")
                {
                    result.Message = "User Exited";
                    return result;
                }

                //Type STARNETHolonType = (Type)value;
                string holonPath = "";

                if (Path.IsPathRooted(SourcePath) || string.IsNullOrEmpty(STAR.STARDNA.BaseSTARNETPath))
                    holonPath = SourcePath;
                else
                    holonPath = Path.Combine(STAR.STARDNA.BaseSTARNETPath, SourcePath);

                (result, holonPath) = GetValidFolder(result, holonPath, STARNETManager.STARNETHolonUIName, SourceSTARDNAKey, true, holonName);

                if (result.IsError)
                    return result;

                //if (!CLIEngine.GetConfirmation($"Do you wish to create the {STARNETManager.STARNETHolonUIName} in the default path defined in the STARDNA as '{SourceSTARDNAKey}'? The current path points to: {holonPath}"))
                //    holonPath = CLIEngine.GetValidFolder($"Where do you wish to create the {STARNETManager.STARNETHolonUIName}?");

                //holonPath = Path.Combine(holonPath, holonName);

                //if (Directory.Exists(holonPath) && checkIfSourcePathExists)
                //{
                //    if (CLIEngine.GetConfirmation($"The directory {holonPath} already exists! Would you like to delete it?"))
                //    {
                //        Console.WriteLine("");
                //        Directory.Delete(holonPath, true);
                //    }
                //    else
                //    {
                //        Console.WriteLine("");
                //        OASISErrorHandling.HandleError(ref result, $"The directory {holonPath} already exists! Please either delete it or choose a different name.");
                //        return result;
                //    }
                //}

                Console.WriteLine("");
                CLIEngine.ShowWorkingMessage($"Generating {STARNETManager.STARNETHolonUIName}...");
                //OASISResult<T1> starHolonResult = await STARNETManager.CreateAsync(STAR.BeamedInAvatar.Id, holonName, holonDesc, Type, holonPath, providerType);
                result = await STARNETManager.CreateAsync(STAR.BeamedInAvatar.Id, holonName, holonDesc, holonSubType, holonPath, newHolon: newHolon, checkIfSourcePathExists: checkIfSourcePathExists, providerType: providerType);

                if (result != null)
                {
                    if (!result.IsError && result.Result != null)
                    {
                        CLIEngine.ShowSuccessMessage($"{STARNETManager.STARNETHolonUIName} Successfully Generated.");
                        Show(result.Result);
                        Console.WriteLine("");

                        if (CLIEngine.GetConfirmation($"Do you wish to open the {STARNETManager.STARNETHolonUIName} folder now?"))
                            Process.Start("explorer.exe", holonPath);

                        Console.WriteLine("");
                    }
                }
                else
                    CLIEngine.ShowErrorMessage($"Unknown Error Occured.");
            }
            else
                OASISErrorHandling.HandleError(ref result, "holonSubType is null!");

            return result;
        }

        public (OASISResult<T>, string) GetValidFolder<T>(OASISResult<T> result, string defaultPath, string pathDisplayName, string SourceSTARDNAKey, bool checkIfExists = true, string holonName = "")
        {
            if (!CLIEngine.GetConfirmation($"Do you wish to create the {pathDisplayName} in the default path defined in the STARDNA as '{SourceSTARDNAKey}' (recommended)? The current path points to: {defaultPath}"))
                defaultPath = CLIEngine.GetValidFolder($"Where do you wish to create the {pathDisplayName}?");

            if (!string.IsNullOrEmpty(holonName))
                defaultPath = Path.Combine(defaultPath, holonName);

            if (Directory.Exists(defaultPath) && checkIfExists)
            {
                Console.WriteLine("");
                if (CLIEngine.GetConfirmation($"The directory {defaultPath} already exists! Would you like to delete it?"))
                {
                    Console.WriteLine("");
                    Directory.Delete(defaultPath, true);
                }
                else
                {
                    Console.WriteLine("");
                    OASISErrorHandling.HandleError(ref result, $"The directory {defaultPath} already exists! Please either delete it or choose a different name.");
                    return (result, defaultPath);
                }
            }

            result.IsSaved = true;
            return (result, defaultPath);
        }

        public virtual async Task EditAsync(string idOrName = "", object editParams = null, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> loadResult = await FindAsync("edit", idOrName, true, providerType: providerType);
            bool changesMade = false;

            if (loadResult != null && loadResult.Result != null && !loadResult.IsError)
            {
                if (CLIEngine.GetConfirmation($"Do you wish to edit the {STARNETManager.STARNETHolonUIName} Name?"))
                {
                    Console.WriteLine("");
                    loadResult.Result.STARNETDNA.Name = CLIEngine.GetValidInput($"What is the new name of the {STARNETManager.STARNETHolonUIName}?");
                    changesMade = true;
                }
                else
                    Console.WriteLine("");

                if (CLIEngine.GetConfirmation($"Do you wish to edit the {STARNETManager.STARNETHolonUIName} Description?"))
                {
                    Console.WriteLine("");
                    loadResult.Result.STARNETDNA.Description = CLIEngine.GetValidInput($"What is the new description of the {STARNETManager.STARNETHolonUIName}?");
                    changesMade = true;
                }
                else
                    Console.WriteLine("");

                if (CLIEngine.GetConfirmation($"Do you wish to edit the {STARNETManager.STARNETHolonUIName} Type?"))
                {
                    Console.WriteLine("");
                    object holonSubType = CLIEngine.GetValidInputForEnum($"What is the new type of the {STARNETManager.STARNETHolonUIName}?", STARNETManager.STARNETHolonSubType);

                    if (holonSubType != null)
                    {
                        if (holonSubType.ToString() == "exit")
                            return;

                        loadResult.Result.STARNETDNA.STARNETHolonType = holonSubType;
                        changesMade = true;
                    }
                }
                else
                    Console.WriteLine("");

                if (CLIEngine.GetConfirmation("Do you wish to edit the launch target?"))
                {
                    Console.WriteLine("");
                    loadResult.Result.STARNETDNA.LaunchTarget = CLIEngine.GetValidInput($"What is the new launch target of the {STARNETManager.STARNETHolonUIName}?");
                    changesMade = true;
                }
                else
                    Console.WriteLine("");

                if (changesMade)
                {
                    OASISResult<T1> result = await STARNETManager.EditAsync(STAR.BeamedInAvatar.Id, loadResult.Result, (T4)loadResult.Result.STARNETDNA, providerType);
                    Console.WriteLine("");
                    CLIEngine.ShowWorkingMessage($"Saving {STARNETManager.STARNETHolonUIName}...");

                    if (result != null && !result.IsError && result.Result != null)
                    {
                        (result, bool saveResult) = ErrorHandling.HandleResponse(result, await STARNETManager.WriteDNAAsync(result.Result.STARNETDNA, result.Result.STARNETDNA.SourcePath), "Error occured saving the STARNETDNA. Reason: ", $"{STARNETManager.STARNETHolonUIName} Successfully Updated.");

                        if (saveResult)
                            Show(result.Result);
                    }
                    else
                        CLIEngine.ShowErrorMessage($"An error occured updating the {STARNETManager.STARNETHolonUIName}. Reason: {result.Message}");
                }

                if (loadResult.Result.STARNETDNA.PublishedOn != DateTime.MinValue && CLIEngine.GetConfirmation($"Do you wish to upload any changes you have made in the Source folder ({loadResult.Result.STARNETDNA.SourcePath})? The version number will remain the same ({loadResult.Result.STARNETDNA.Version})."))
                    await PublishAsync(loadResult.Result.STARNETDNA.SourcePath, true, DefaultLaunchMode.Optional, providerType);
                else
                    Console.WriteLine("");
            }
            else
            {
                Console.WriteLine("");
                CLIEngine.ShowErrorMessage($"An error occured loading the {STARNETManager.STARNETHolonUIName}. Reason: {loadResult.Message}");
            }
        }

        public virtual async Task DeleteAsync(string idOrName = "", bool softDelete = true, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = await FindAsync("delete", idOrName, true, providerType: providerType);

            if (result != null && !result.IsError && result.Result != null)
            {
                if (CLIEngine.GetConfirmation($"Are you sure you wish to delete this {STARNETManager.STARNETHolonUIName}? This will also delete the {STARNETManager.STARNETHolonUIName} from the Source and Published folders and remove it from the STARNET Store (if you have already published it)"))
                {
                    Console.WriteLine("");
                    bool deleteDownload = CLIEngine.GetConfirmation($"Do you wish to also delete the correponding downloaded {STARNETManager.STARNETHolonUIName}? (if there is any)");

                    Console.WriteLine("");
                    bool deleteInstall = CLIEngine.GetConfirmation($"Do you wish to also delete the correponding installed {STARNETManager.STARNETHolonUIName}? (if there is any). This is different to uninstalling because uninstalled {STARNETManager.STARNETHolonUIName}s are still visible with the 'list uninstalled' sub-command and have the option to re-install. Whereas once it is deleted it is gone forever!");

                    Console.WriteLine("");
                    if (CLIEngine.GetConfirmation("ARE YOU SURE YOU WITH TO PERMANENTLY DELETE THE OAPP TEMPLATE? IT WILL NOT BE POSSIBLE TO RECOVER AFTRWARDS!", ConsoleColor.Red))
                    {
                        Console.WriteLine("");
                        CLIEngine.ShowWorkingMessage($"Deleting {STARNETManager.STARNETHolonUIName}...");
                        result = await STARNETManager.DeleteAsync(STAR.BeamedInAvatar.Id, result.Result, result.Result.STARNETDNA.VersionSequence, true, deleteDownload, deleteInstall, providerType);

                        if (result != null && !result.IsError && result.Result != null)
                            CLIEngine.ShowSuccessMessage($"{STARNETManager.STARNETHolonUIName} Successfully Deleted.");
                        else
                            CLIEngine.ShowErrorMessage($"An error occured deleting the {STARNETManager.STARNETHolonUIName}. Reason: {result.Message}");
                    }
                }
            }
            else
                CLIEngine.ShowErrorMessage($"An error occured loading the {STARNETManager.STARNETHolonUIName}. Reason: {result.Message}");
        }

        public virtual async Task<OASISResult<T1>> PublishAsync(string sourcePath = "", bool edit = false, DefaultLaunchMode defaultLaunchMode = DefaultLaunchMode.Optional, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> publishResult = new OASISResult<T1>();
            bool generateOAPP = true;
            bool uploadOAPPToCloud = true;
            ProviderType OAPPBinaryProviderType = ProviderType.None;  
           // string publishPath = "";

            OASISResult<BeginPublishResult> beginPublishResult = await BeginPublishingAsync(sourcePath, defaultLaunchMode, providerType);

            if (beginPublishResult != null && !beginPublishResult.IsError && beginPublishResult.Result != null)
            {
                Console.WriteLine("");
                bool registerOnSTARNET = CLIEngine.GetConfirmation($"Do you wish to publish to STARNET? If you select 'Y' to this question then your {STARNETManager.STARNETHolonUIName} will be published to STARNET where others will be able to find, download and install. If you select 'N' then only the .{STARNETManager.STARNETHolonFileExtention} install file will be generated on your local device, which you can distribute as you please. This file will also be generated even if you publish to STARNET.");
                Console.WriteLine("");

                if (registerOnSTARNET && !beginPublishResult.Result.SimpleWizard)
                {
                    CLIEngine.ShowMessage($"Do you wish to publish/upload the .{STARNETManager.STARNETHolonFileExtention} file to an OASIS Provider or to the cloud or both? Depending on which OASIS Provider is chosen such as IPFSOASIS there may issues such as speed, relialbility etc for such a large file. If you choose to upload to the cloud this could be faster and more reliable (but there is a limit of 5 OAPPs on the free plan and you will need to upgrade to upload more than 5 OAPPs). You may want to choose to use both to add an extra layer of redundancy (recommended).");

                    if (!CLIEngine.GetConfirmation("Do you wish to upload to the cloud?"))
                        uploadOAPPToCloud = false;

                    Console.WriteLine("");
                    if (CLIEngine.GetConfirmation("Do you wish to upload to an OASIS Provider? Make sure you select a provider that can handle large files such as IPFSOASIS, HoloOASIS etc. Also remember the OASIS Hyperdrive will only be able to auto-replicate to other providers that also support large files and are free or cost effective. By default it will NOT auto-replicate large files, you will need to manually configure this in your OASIS Profile settings."))
                    {
                        Console.WriteLine("");
                        object largeProviderTypeObject = CLIEngine.GetValidInputForEnum("What provider do you wish to publish the OAPP to? (The default is IPFSOASIS)", typeof(ProviderType));

                        if (largeProviderTypeObject != null)
                            OAPPBinaryProviderType = (ProviderType)largeProviderTypeObject;
                    }
                    else
                        Console.WriteLine("");
                }

                publishResult = await FininaliazePublishingAsync(beginPublishResult.Result.SimpleWizard, beginPublishResult.Result.SourcePath, beginPublishResult.Result.LaunchTarget, edit, registerOnSTARNET, generateOAPP, uploadOAPPToCloud, providerType, OAPPBinaryProviderType);
            }
            else
                CLIEngine.ShowErrorMessage($"Error Occured: {beginPublishResult.Message}");

            return publishResult;
        }

        protected async Task<OASISResult<BeginPublishResult>> BeginPublishingAsync(string sourcePath, DefaultLaunchMode defaultLaunchMode = DefaultLaunchMode.Optional, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<BeginPublishResult> result = new OASISResult<BeginPublishResult>();
            bool generateOAPP = true;
            bool uploadOAPPToCloud = true;
            ProviderType OAPPBinaryProviderType = ProviderType.None;
            string launchTarget = "";
            string launchTargetQuestion = $"What is the relative path (from the root of the path given above, e.g bin\\launch.exe) to the launch target for the {STARNETManager.STARNETHolonUIName}? (This could be the exe or batch file for a desktop or console app, or the index.html page for a website, etc)";
            bool simpleWizard = false;

            if (CLIEngine.GetConfirmation("Do you wish to launch the Simple or Advanced Wizard? The Simple Wizard will use defaults (recommended) but the Advanced Wizard will allow greater control and customisation. Press 'Y' for Simple or 'N' for Advanced."))
                simpleWizard = true;

            if (string.IsNullOrEmpty(sourcePath))
            {
                Console.WriteLine("");
                //launchTargetQuestion = $"What is the relative path (from the root of the path given above, e.g bin\\launch.exe) to the launch target for the {STARNETManager.STARNETHolonUIName}? (This could be the exe or batch file for a desktop or console app, or the index.html page for a website, etc)";
                sourcePath = CLIEngine.GetValidFolder($"What is the full path to the {STARNETManager.STARNETHolonUIName} directory?", false);
            }

            OASISResult<STARNETDNA> DNAResult = await STARNETManager.ReadDNAFromSourceOrInstallFolderAsync<STARNETDNA>(sourcePath);

            if (DNAResult != null && DNAResult.Result != null && !DNAResult.IsError)
            {
                OASISResult<T1> loadResult = await STARNETManager.LoadAsync(STAR.BeamedInAvatar.Id, DNAResult.Result.Id, 0, providerType);

                if (loadResult != null && loadResult.Result != null && !loadResult.IsError)
                {
                    loadResult.Result.STARNETDNA.Version = DNAResult.Result.Version; //Update the version from the JSON file.
                    Show(loadResult.Result);

                    if (!CLIEngine.GetConfirmation($"Is this the correct {STARNETManager.STARNETHolonUIName} you wish to publish?"))
                    {
                        Console.WriteLine("");
                        result.Message = "User Exited";
                        result.IsError = true;
                        return result;
                    }

                    launchTarget = loadResult.Result.STARNETDNA.LaunchTarget;
                    Console.WriteLine("");

                    //object templateType = Enum.Parse(STARNETManager.STARNETHolonSubType, DNAResult.Result.STARNETHolonType.ToString());
                    //Type Type = (Type)templateType;

                    //switch (Type)
                    //{
                    //    case Type.Console:
                    //    case Type.WPF:
                    //    case Type.WinForms:
                    //        launchTarget = $"{DNAResult.Result.Name}.exe"; //TODO: For this line to work need to remove the namespace question so it just uses the OAPPName as the namespace. //TODO: Eventually this will be set in the  and/or can also be set when I add the command line dotnet publish integration.
                    //        break;

                    //    case Type.Blazor:
                    //    case Type.MAUI:
                    //    case Type.WebMVC:
                    //        launchTarget = $"index.html";
                    //        break;
                    //}

                    if (defaultLaunchMode != DefaultLaunchMode.None)
                    {
                        bool hasDefaultLaunchTarget = false;

                        if (defaultLaunchMode == DefaultLaunchMode.Optional)
                            hasDefaultLaunchTarget = CLIEngine.GetConfirmation($"Do you wish to set a default launch target?");

                        else if (defaultLaunchMode == DefaultLaunchMode.Mandatory)
                            hasDefaultLaunchTarget = true;

                        if (hasDefaultLaunchTarget)
                        {
                            if (!string.IsNullOrEmpty(launchTarget))
                            {
                                if (!CLIEngine.GetConfirmation($"{launchTargetQuestion} Do you wish to use the following default launch target: {launchTarget}?"))
                                {
                                    Console.WriteLine("");
                                    launchTarget = CLIEngine.GetValidFile("What launch target do you wish to use? ", sourcePath);
                                }
                                else
                                    launchTarget = Path.Combine(sourcePath, launchTarget);
                            }
                            else
                                launchTarget = CLIEngine.GetValidFile(launchTargetQuestion, sourcePath);
                        }
                    }
                }
                else
                    CLIEngine.ShowErrorMessage($"The {STARNETManager.STARNETHolonUIName} could not be found for id {DNAResult.Result.Id} found in the {STARNETManager.STARNETDNAFileName} file. It could be corrupt, the id could be wrong or you may not have permission, please check and try again, or create a new {STARNETManager.STARNETHolonUIName}.");
            }
            else
                CLIEngine.ShowErrorMessage($"The {STARNETManager.STARNETDNAFileName} file could not be found! Please ensure it is in the folder you specified.");

            result.Result = new BeginPublishResult() { SourcePath = sourcePath, LaunchTarget = launchTarget, SimpleWizard = simpleWizard };
            return result;
        }

        protected async Task<OASISResult<T1>> FininaliazePublishingAsync(bool simpleWizard, string sourcePath, string launchTarget, bool edit, bool registerOnSTARNET, bool generateOAPP, bool uploadOAPPToCloud, ProviderType providerType, ProviderType OAPPBinaryProviderType)
        {
            OASISResult<T1> publishResult = new OASISResult<T1>();
            OASISResult<string> prePubResult = await PreFininaliazePublishingAsync(simpleWizard, sourcePath, launchTarget, edit, registerOnSTARNET, generateOAPP, uploadOAPPToCloud, providerType, OAPPBinaryProviderType);
            bool embedLibs = false;
            bool embedRuntimes = false;
            bool embedTemplates = false;

            if (prePubResult != null && !string.IsNullOrEmpty(prePubResult.Result) && !prePubResult.IsError)
            {
                if (STARNETManager.STARNETHolonType == HolonType.OAPPTemplate && CLIEngine.GetConfirmation("Do you wish to embed the libraries, runtimes & sub-templates in the template (say 'Y' if you only want to enbed one of these)? It is not recommended because will increase the storage space/cost & upload/download time. If you choose 'N' then they will be automatically downloaded and installed when someone installs your template. Only choose 'Y' if you want them embedded in case there is an issue downloading/installing them seperatley later (unlikely) or if you want the template to be fully self-contained with no external dependencies (useful if you wish to install it offline from the .oapptemplate file)."))
                {
                    embedTemplates = CLIEngine.GetConfirmation("Do you wish to embed the sub-templates?");
                    embedRuntimes = CLIEngine.GetConfirmation("Do you wish to embed the runtimes?");
                    embedLibs = CLIEngine.GetConfirmation("Do you wish to embed the libraries?");
                }

                publishResult = await STARNETManager.PublishAsync(STAR.BeamedInAvatar.Id, sourcePath, launchTarget, prePubResult.Result, edit, registerOnSTARNET, generateOAPP, uploadOAPPToCloud, providerType, OAPPBinaryProviderType, embedRuntimes, embedLibs, embedTemplates);
                await PostFininaliazePublishingAsync(publishResult, sourcePath, providerType);
            }
            else
                OASISErrorHandling.HandleError(ref publishResult, $"Error occured in STARNETUIBase.FininaliazePublishingAsync calling PreFininaliazePublishingAsync. Reason: {prePubResult.Message}");

            return publishResult;
        }

        protected async Task<OASISResult<string>> PreFininaliazePublishingAsync(bool simpleWizard, string sourcePath, string launchTarget, bool edit, bool registerOnSTARNET, bool generateOAPP, bool uploadOAPPToCloud, ProviderType providerType, ProviderType OAPPBinaryProviderType)
        {
            OASISResult<string> result = new OASISResult<string>();
            string publishPath = "";

            if (Path.IsPathRooted(PublishedPath) || string.IsNullOrEmpty(STAR.STARDNA.BaseSTARNETPath))
                publishPath = PublishedPath;
            else
                publishPath = Path.Combine(STAR.STARDNA.BaseSTARNETPath, PublishedPath);

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

            Console.WriteLine("");
            CLIEngine.ShowWorkingMessage($"Publishing {STARNETManager.STARNETHolonUIName}...");
            result.Result = publishPath;
            return result;
        }

        protected async Task<OASISResult<T1>> PostFininaliazePublishingAsync(OASISResult<T1> publishResult, string sourcePath, ProviderType providerType)
        {

            if (publishResult != null && !publishResult.IsError && publishResult.Result != null)
            {
                Show(publishResult.Result);

                if (CLIEngine.GetConfirmation($"Do you wish to install the {STARNETManager.STARNETHolonUIName} now?"))
                    await DownloadAndInstallAsync(publishResult.Result.STARNETDNA.Id.ToString(), InstallMode.DownloadAndInstall, providerType);

                Console.WriteLine("");
            }
            else
            {
                if (publishResult.Message.Contains("Please make sure you increment the version"))
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
            OASISResult<T1> result = await FindAsync("unpublish", idOrName, true, providerType: providerType);

            if (result != null && !result.IsError && result.Result != null)
            {
                OASISResult<T1> unpublishResult = await STARNETManager.UnpublishAsync(STAR.BeamedInAvatar.Id, result.Result, providerType);

                if (unpublishResult != null && !unpublishResult.IsError && unpublishResult.Result != null)
                {
                    CLIEngine.ShowSuccessMessage($"{STARNETManager.STARNETHolonUIName} Successfully Unpublished.");
                    Show(result.Result);
                }
                else
                    CLIEngine.ShowErrorMessage($"An error occured unpublishing the {STARNETManager.STARNETHolonUIName}. Reason: {unpublishResult.Message}");
            }
        }

        public virtual async Task RepublishAsync(string idOrName = "", ProviderType providerType = ProviderType.Default)
            {
                OASISResult<T1> result = await FindAsync("republish", idOrName, true, providerType: providerType);

            if (result != null && !result.IsError && result.Result != null)
            {
                OASISResult<T1> republishResult = await STARNETManager.RepublishAsync(STAR.BeamedInAvatar.Id, result.Result, providerType);

                if (republishResult != null && !republishResult.IsError && republishResult.Result != null)
                {
                    CLIEngine.ShowSuccessMessage($"{STARNETManager.STARNETHolonUIName} Successfully Republished.");
                    Show(result.Result);
                }
                else
                    CLIEngine.ShowErrorMessage($"An error occured unpublishing the {STARNETManager.STARNETHolonUIName}. Reason: {republishResult.Message}");
            }
        }

        public virtual async Task ActivateAsync(string idOrName = "", ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = await FindAsync("activate", idOrName, true, providerType: providerType);

            if (result != null && !result.IsError && result.Result != null)
            {
                if (result.MetaData != null && result.MetaData.ContainsKey("Active") && result.MetaData["Active"] != null && result.MetaData["Active"] == "1")
                {
                    OASISResult<T1> activateResult = await STARNETManager.ActivateAsync(STAR.BeamedInAvatar.Id, result.Result, providerType);

                    if (activateResult != null && !activateResult.IsError && activateResult.Result != null)
                    {
                        CLIEngine.ShowSuccessMessage($"{STARNETManager.STARNETHolonUIName} Successfully Activated.");
                        Show(result.Result);
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
            OASISResult<T1> result = await FindAsync("deactivate", idOrName, true, providerType: providerType);

            if (result != null && !result.IsError && result.Result != null)
            {
                if (result.MetaData != null && result.MetaData.ContainsKey("Active") && result.MetaData["Active"] != null && result.MetaData["Active"] == "0")
                {
                    OASISResult<T1> deactivateResult = await STARNETManager.DeactivateAsync(STAR.BeamedInAvatar.Id, result.Result, providerType);

                    if (deactivateResult != null && !deactivateResult.IsError && deactivateResult.Result != null)
                    {
                        CLIEngine.ShowSuccessMessage($"{STARNETManager.STARNETHolonUIName} Successfully Deactivated.");
                        Show(result.Result);
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

            if (Path.IsPathRooted(DownloadedPath) || string.IsNullOrEmpty(STAR.STARDNA.BaseSTARNETPath))
                downloadPath = DownloadedPath;
            else
                downloadPath = Path.Combine(STAR.STARDNA.BaseSTARNETPath, DownloadedPath);


            if (Path.IsPathRooted(InstalledPath) || string.IsNullOrEmpty(STAR.STARDNA.BaseSTARNETPath))
                installPath = InstalledPath;
            else
                installPath = Path.Combine(STAR.STARDNA.BaseSTARNETPath, InstalledPath);

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
                        OASISResult<T1> starHolonResult = await STARNETManager.LoadAsync(STAR.BeamedInAvatar.Id, starHolonDNAResult.Result.Id, 0, providerType);

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

            if (Path.IsPathRooted(DownloadedPath) || string.IsNullOrEmpty(STAR.STARDNA.BaseSTARNETPath))
                downloadPath = SourcePath;
            else
                downloadPath = Path.Combine(STAR.STARDNA.BaseSTARNETPath, DownloadedPath);


            if (Path.IsPathRooted(InstalledPath) || string.IsNullOrEmpty(STAR.STARDNA.BaseSTARNETPath))
                installPath = SourcePath;
            else
                installPath = Path.Combine(STAR.STARDNA.BaseSTARNETPath, InstalledPath);

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

        public virtual async Task UninstallAsync(string idOrName = "", ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = await FindAsync("uninstall", idOrName, true, providerType: providerType);

            if (result != null && !result.IsError && result.Result != null)
            {
                OASISResult<T3> uninstallResult = await STARNETManager.UninstallAsync(STAR.BeamedInAvatar.Id, result.Result.Id, result.Result.STARNETDNA.Version, providerType);

                if (uninstallResult != null)
                {
                    if (!uninstallResult.IsError && uninstallResult.Result != null)
                    {
                        CLIEngine.ShowSuccessMessage($"{STARNETManager.STARNETHolonUIName} Successfully Uninstalled.");
                        Show(result.Result);
                    }
                    else
                        CLIEngine.ShowErrorMessage($"Error installing {STARNETManager.STARNETHolonUIName}. Reason: {uninstallResult.Message}");
                }
                else
                    CLIEngine.ShowErrorMessage($"Error uninstalling {STARNETManager.STARNETHolonUIName}. Reason: Unknown error occured!");
            }
            else
                CLIEngine.ShowErrorMessage($"An error occured loading the {STARNETManager.STARNETHolonUIName}. Reason: {result.Message}");
        }

        public virtual void Uninstall(string idOrName = "", ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = Find("uninstall", idOrName, true, providerType: providerType);

            if (result != null && !result.IsError && result.Result != null)
            {
                OASISResult<T3> uninstallResult = STARNETManager.Uninstall(STAR.BeamedInAvatar.Id, result.Result.Id, result.Result.STARNETDNA.Version, providerType);

                if (uninstallResult != null)
                {
                    if (!uninstallResult.IsError && uninstallResult.Result != null)
                    {
                        CLIEngine.ShowSuccessMessage($"{STARNETManager.STARNETHolonUIName} Successfully Uninstalled.");
                        Show(result.Result);
                    }
                    else
                        CLIEngine.ShowErrorMessage($"Error installing {STARNETManager.STARNETHolonUIName}. Reason: {uninstallResult.Message}");
                }
                else
                    CLIEngine.ShowErrorMessage($"Error uninstalling {STARNETManager.STARNETHolonUIName}. Reason: Unknown error occured!");
            }
            else
                CLIEngine.ShowErrorMessage($"An error occured loading the {STARNETManager.STARNETHolonUIName}. Reason: {result.Message}");
        }

        public virtual async Task<OASISResult<IEnumerable<T1>>> ListAllAsync(bool showAllVersions = false, ProviderType providerType = ProviderType.Default)
        {
            Console.WriteLine("");
            CLIEngine.ShowWorkingMessage($"Loading {STARNETManager.STARNETHolonUIName}'s...");
            return ListStarHolons(await STARNETManager.LoadAllAsync(STAR.BeamedInAvatar.Id, null, true, showAllVersions, 0, providerType: providerType));
        }

        public virtual OASISResult<IEnumerable<T1>> ListAll(bool showAllVersions = false, int version = 0, ProviderType providerType = ProviderType.Default)
        {
            Console.WriteLine("");
            CLIEngine.ShowWorkingMessage($"Loading {STARNETManager.STARNETHolonUIName}'s...");
            return  ListStarHolons(STARNETManager.LoadAll(STAR.BeamedInAvatar.Id, null, true, showAllVersions, version, providerType: providerType));
        }

        public virtual async Task ListAllCreatedByBeamedInAvatarAsync(bool showAllVersions = false, ProviderType providerType = ProviderType.Default)
        {
            if (STAR.BeamedInAvatar != null)
            {
                Console.WriteLine("");
                CLIEngine.ShowWorkingMessage($"Loading {STARNETManager.STARNETHolonUIName}'s...");
                ListStarHolons(await STARNETManager.LoadAllForAvatarAsync(STAR.BeamedInAvatar.AvatarId));
            }
            else
                CLIEngine.ShowErrorMessage("No Avatar Is Beamed In. Please Beam In First!");
        }

        public virtual async Task<OASISResult<IEnumerable<T3>>> ListAllInstalledForBeamedInAvatarAsync(ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<T3>> result = new OASISResult<IEnumerable<T3>>();

            if (STAR.BeamedInAvatar != null)
            {
                Console.WriteLine("");
                CLIEngine.ShowWorkingMessage($"Loading Installed {STARNETManager.STARNETHolonUIName}'s...");
                result = await STARNETManager.ListInstalledAsync(STAR.BeamedInAvatar.AvatarId);
                ListStarHolonsInstalled(result);
            }
            else
                OASISErrorHandling.HandleError(ref result, "No Avatar Is Beamed In. Please Beam In First!");

            return result;
        }

        public virtual async Task<OASISResult<IEnumerable<T3>>> ListAllUninstalledForBeamedInAvatarAsync(ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<T3>> result = new OASISResult<IEnumerable<T3>>();

            if (STAR.BeamedInAvatar != null)
            {
                Console.WriteLine("");
                CLIEngine.ShowWorkingMessage($"Loading Uninstalled {STARNETManager.STARNETHolonUIName}s...");
                result = await STARNETManager.ListUninstalledAsync(STAR.BeamedInAvatar.AvatarId);
                ListStarHolonsInstalled(result, true, true);

                if (result != null && !result.IsError && result.Result != null && result.Result.Count() > 0 && CLIEngine.GetConfirmation("Would you like to re-install any of the above?"))
                {
                    int number = 0;

                    do
                    {
                        Console.WriteLine("");
                        number = CLIEngine.GetValidInputForInt("What number do you wish to re-install? (It will be downloaded and installed to the previous paths)");

                        if (number < 0 || number > result.Result.Count())
                            CLIEngine.ShowErrorMessage($"Invalid number, it needs to be between 1 and {result.Result.Count()}");
                    }
                    while (number < 0 || number > result.Result.Count());

                    if (number > 0)
                    {
                        T3 template = result.Result.ElementAt(number - 1);
    
                        if (template != null)
                        {
                            OASISResult<T3> installResult = await DownloadAndInstallAsync(template.STARNETDNA.Id.ToString(), InstallMode.DownloadAndReInstall, providerType);

                            if (installResult != null && !installResult.IsError && installResult.Result != null)
                            {
                                ShowInstalled(installResult.Result);
                                CLIEngine.ShowSuccessMessage($"{STARNETManager.STARNETHolonUIName} Successfully Re-Installed");
                            }
                            else
                                CLIEngine.ShowErrorMessage($"An error occured re-installing the {STARNETManager.STARNETHolonUIName}. Reason: {installResult.Message}");
                        }
                        else
                            CLIEngine.ShowErrorMessage($"An error occured re-installing the {STARNETManager.STARNETHolonUIName}. Reason: {STARNETManager.STARNETHolonIdName} not found in the metadata!");
                    }
                }
                else
                    Console.WriteLine("");
            }
            else
                OASISErrorHandling.HandleError(ref result, "No Avatar Is Beamed In. Please Beam In First!");

            return result;
        }

        public virtual async Task<OASISResult<IEnumerable<T1>>> ListAllUnpublishedForBeamedInAvatarAsync(ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<T1>> result = new OASISResult<IEnumerable<T1>>();

            if (STAR.BeamedInAvatar != null)
            {
                Console.WriteLine("");
                CLIEngine.ShowWorkingMessage($"Loading Unpublished {STARNETManager.STARNETHolonUIName}'s...");
                result = await STARNETManager.ListUnpublishedAsync(STAR.BeamedInAvatar.AvatarId);
                ListStarHolons(result, true);

                if (result != null && !result.IsError && result.Result != null && result.Result.Count() > 0 && CLIEngine.GetConfirmation("Would you like to republish any of the above?"))
                {
                    int number = 0;

                    do
                    {
                        Console.WriteLine("");
                        number = CLIEngine.GetValidInputForInt("What number do you wish to republish?");

                        if (number < 0 || number > result.Result.Count())
                            CLIEngine.ShowErrorMessage($"Invalid number, it needs to be between 1 and {result.Result.Count()}");
                    }
                    while (number < 0 || number > result.Result.Count());

                    if (number > 0)
                    {
                        T1 template = result.Result.ElementAt(number - 1);
                        Guid id = Guid.Empty;

                        if (template != null)
                        {
                            OASISResult<T1> republishResult = await STARNETManager.RepublishAsync(STAR.BeamedInAvatar.Id, template, providerType);

                            if (republishResult != null && !republishResult.IsError && republishResult.Result != null)
                            {
                                Show(republishResult.Result);
                                CLIEngine.ShowSuccessMessage($"{STARNETManager.STARNETHolonUIName} Successfully Republished");
                            }
                            else
                                CLIEngine.ShowErrorMessage($"An error occured republishing the {STARNETManager.STARNETHolonUIName}. Reason: {republishResult.Message}");
                        }
                    }
                }
                else
                    Console.WriteLine("");
            }
            else
                OASISErrorHandling.HandleError(ref result, "No Avatar Is Beamed In. Please Beam In First!");

            return result;
        }

        public virtual async Task<OASISResult<IEnumerable<T1>>> ListAllDeactivatedForBeamedInAvatarAsync(ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<T1>> result = new OASISResult<IEnumerable<T1>>();

            if (STAR.BeamedInAvatar != null)
            {
                Console.WriteLine("");
                CLIEngine.ShowWorkingMessage($"Loading Deactivated {STARNETManager.STARNETHolonUIName}'s...");
                result = await STARNETManager.ListDeactivatedAsync(STAR.BeamedInAvatar.AvatarId);
                ListStarHolons(result, true);

                if (result != null && !result.IsError && result.Result != null && result.Result.Count() > 0 && CLIEngine.GetConfirmation("Would you like to reactivate any of the above?"))
                {
                    int number = 0;

                    do
                    {
                        Console.WriteLine("");
                        number = CLIEngine.GetValidInputForInt("What number do you wish to reactivate?");

                        if (number < 0 || number > result.Result.Count())
                            CLIEngine.ShowErrorMessage($"Invalid number, it needs to be between 1 and {result.Result.Count()}");
                    }
                    while (number < 0 || number > result.Result.Count());

                    if (number > 0)
                    {
                        T1 template = result.Result.ElementAt(number - 1);
                        Guid id = Guid.Empty;

                        if (template != null)
                        {
                            OASISResult<T1> activateResult = await STARNETManager.ActivateAsync(STAR.BeamedInAvatar.Id, template, providerType);

                            if (activateResult != null && !activateResult.IsError && activateResult.Result != null)
                            {
                                Show(activateResult.Result);
                                CLIEngine.ShowSuccessMessage($"{STARNETManager.STARNETHolonUIName} Successfully Reactivated");
                            }
                            else
                                CLIEngine.ShowErrorMessage($"An error occured reactivating the {STARNETManager.STARNETHolonUIName}. Reason: {activateResult.Message}");
                        }
                    }
                }
                else
                    Console.WriteLine("");
            }
            else
                OASISErrorHandling.HandleError(ref result, "No Avatar Is Beamed In. Please Beam In First!");

            return result;
        }

        public virtual async Task SearchsAsync(string searchTerm = "", bool showAllVersions = false, bool showForAllAvatars = true, ProviderType providerType = ProviderType.Default)
        {            
            if (string.IsNullOrEmpty(searchTerm) || searchTerm == "forallavatars" || searchTerm == "forallavatars")
            { 
                //Console.WriteLine("");
                searchTerm = CLIEngine.GetValidInput($"What is the name of the {STARNETManager.STARNETHolonUIName} you wish to search for?");
            }

            Console.WriteLine("");
            CLIEngine.ShowWorkingMessage($"Searching {STARNETManager.STARNETHolonUIName}'s...");
            ListStarHolons(await STARNETManager.SearchAsync<T1>(STAR.BeamedInAvatar.Id, searchTerm, !showForAllAvatars, showAllVersions, 0, providerType));
        }

        public virtual async Task ShowAsync(string idOrName = "", ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = await FindAsync("view", idOrName, true, providerType: providerType);

            if (result != null && !result.IsError && result.Result != null)
                Show(result.Result);
            else
                CLIEngine.ShowErrorMessage($"An error occured loading the {STARNETManager.STARNETHolonUIName}. Reason: {result.Message}");
        }

        public virtual void Show<T>(T starHolon, bool showHeader = true, bool showFooter = true, bool showNumbers = false, int number = 0, bool showDetailedInfo = false, int displayFieldLength = DEFAULT_FIELD_LENGTH, object customData = null) where T: ISTARNETHolon
        {
            if (DisplayFieldLength > displayFieldLength)
                displayFieldLength = DisplayFieldLength;

            if (showHeader)
                CLIEngine.ShowDivider();

            Console.WriteLine("");

            if (showNumbers)
                CLIEngine.ShowMessage(string.Concat("Number:".PadRight(displayFieldLength), number), false);

            CLIEngine.ShowMessage(string.Concat($"Id:".PadRight(displayFieldLength), starHolon.STARNETDNA.Id != Guid.Empty ? starHolon.STARNETDNA.Id : "None"), false);
            CLIEngine.ShowMessage(string.Concat($"Name:".PadRight(displayFieldLength), !string.IsNullOrEmpty(starHolon.STARNETDNA.Name) ? starHolon.STARNETDNA.Name : "None"), false);
            CLIEngine.ShowMessage(string.Concat($"Description:".PadRight(displayFieldLength), !string.IsNullOrEmpty(starHolon.STARNETDNA.Description) ? starHolon.STARNETDNA.Description : "None"), false);
            CLIEngine.ShowMessage(string.Concat($"Type:".PadRight(displayFieldLength), starHolon.STARNETDNA.STARNETHolonType), false);
            CLIEngine.ShowMessage(string.Concat($"Created On:".PadRight(displayFieldLength), starHolon.STARNETDNA.CreatedOn != DateTime.MinValue ? starHolon.STARNETDNA.CreatedOn.ToString() : "None"), false);
            CLIEngine.ShowMessage(string.Concat($"Created By:".PadRight(displayFieldLength), starHolon.STARNETDNA.CreatedByAvatarId != Guid.Empty ? string.Concat(starHolon.STARNETDNA.CreatedByAvatarUsername, " (", starHolon.STARNETDNA.CreatedByAvatarId.ToString(), ")") : "None"), false);
            CLIEngine.ShowMessage(string.Concat($"Modified On:".PadRight(displayFieldLength), starHolon.STARNETDNA.ModifiedOn != DateTime.MinValue ? starHolon.STARNETDNA.CreatedOn.ToString() : "None"), false);
            CLIEngine.ShowMessage(string.Concat($"Modified By:".PadRight(displayFieldLength), starHolon.STARNETDNA.ModifiedByAvatarId != Guid.Empty ? string.Concat(starHolon.STARNETDNA.ModifiedByAvatarUsername, " (", starHolon.STARNETDNA.ModifiedByAvatarId.ToString(), ")") : "None"), false);
            CLIEngine.ShowMessage(string.Concat($"Source Path:".PadRight(displayFieldLength), !string.IsNullOrEmpty(starHolon.STARNETDNA.SourcePath) ? starHolon.STARNETDNA.SourcePath : "None"), false);
            CLIEngine.ShowMessage(string.Concat($"Published On:".PadRight(displayFieldLength), starHolon.STARNETDNA.PublishedOn != DateTime.MinValue ? starHolon.STARNETDNA.PublishedOn.ToString() : "None"), false);
            CLIEngine.ShowMessage(string.Concat($"Published By:".PadRight(displayFieldLength), starHolon.STARNETDNA.PublishedByAvatarId != Guid.Empty ? string.Concat(starHolon.STARNETDNA.PublishedByAvatarUsername, " (", starHolon.STARNETDNA.PublishedByAvatarId.ToString(), ")") : "None"), false);
            CLIEngine.ShowMessage(string.Concat($"Published Path:".PadRight(displayFieldLength), !string.IsNullOrEmpty(starHolon.STARNETDNA.PublishedPath) ? starHolon.STARNETDNA.PublishedPath : "None"), false);
            CLIEngine.ShowMessage(string.Concat($"Filesize:".PadRight(displayFieldLength), starHolon.STARNETDNA.FileSize.ToString()), false);
            CLIEngine.ShowMessage(string.Concat($"Published On STARNET:".PadRight(displayFieldLength), starHolon.STARNETDNA.PublishedOnSTARNET ? "True" : "False"), false);
            CLIEngine.ShowMessage(string.Concat($"Published To Cloud:".PadRight(displayFieldLength), starHolon.STARNETDNA.PublishedToCloud ? "True" : "False"), false);
            CLIEngine.ShowMessage(string.Concat($"Published To OASIS Provider:".PadRight(displayFieldLength), Enum.GetName(typeof(ProviderType), starHolon.STARNETDNA.PublishedProviderType)), false);
            CLIEngine.ShowMessage(string.Concat($"Launch Target:".PadRight(displayFieldLength), !string.IsNullOrEmpty(starHolon.STARNETDNA.LaunchTarget) ? starHolon.STARNETDNA.LaunchTarget : "None"), false);
            CLIEngine.ShowMessage(string.Concat($"Version:".PadRight(displayFieldLength), starHolon.STARNETDNA.Version), false);
            CLIEngine.ShowMessage(string.Concat($"Version Sequence:".PadRight(displayFieldLength), starHolon.STARNETDNA.VersionSequence), false);
            CLIEngine.ShowMessage(string.Concat($"Number Of Versions:".PadRight(displayFieldLength), starHolon.STARNETDNA.NumberOfVersions), false);
            //CLIEngine.ShowMessage(string.Concat($"OASIS Holon Version:                        ", starHolon.Version), false);
            //CLIEngine.ShowMessage(string.Concat($"OASIS Holon VersionId:                      ", starHolon.VersionId), false);
            //CLIEngine.ShowMessage(string.Concat($"OASIS Holon PreviousVersionId:              ", starHolon.PreviousVersionId), false);
            CLIEngine.ShowMessage(string.Concat($"Downloads:".PadRight(displayFieldLength), starHolon.STARNETDNA.Downloads), false);
            CLIEngine.ShowMessage(string.Concat($"Installs:".PadRight(displayFieldLength), starHolon.STARNETDNA.Installs), false);
            CLIEngine.ShowMessage(string.Concat($"Total Downloads:".PadRight(displayFieldLength), starHolon.STARNETDNA.TotalDownloads), false);
            CLIEngine.ShowMessage(string.Concat($"Total Installs:".PadRight(displayFieldLength), starHolon.STARNETDNA.TotalInstalls), false);
            CLIEngine.ShowMessage(string.Concat($"Active:".PadRight(displayFieldLength), starHolon.MetaData != null && starHolon.MetaData.ContainsKey("Active") && starHolon.MetaData["Active"] != null && starHolon.MetaData["Active"].ToString() == "1" ? "True" : "False"), false);
            CLIEngine.ShowMessage(string.Concat($"OASIS Runtime Version:".PadRight(displayFieldLength), starHolon.STARNETDNA.OASISRuntimeVersion), false);
            CLIEngine.ShowMessage(string.Concat($"OASIS API Version:".PadRight(displayFieldLength), starHolon.STARNETDNA.OASISAPIVersion), false);
            CLIEngine.ShowMessage(string.Concat($"COSMIC Version:".PadRight(displayFieldLength), starHolon.STARNETDNA.COSMICVersion), false);
            CLIEngine.ShowMessage(string.Concat($"STAR Runtime Version:".PadRight(displayFieldLength), starHolon.STARNETDNA.STARRuntimeVersion), false);
            CLIEngine.ShowMessage(string.Concat($"STAR ODK Version:".PadRight(displayFieldLength), starHolon.STARNETDNA.STARODKVersion), false);
            CLIEngine.ShowMessage(string.Concat($"STARNET Version:".PadRight(displayFieldLength), starHolon.STARNETDNA.STARNETVersion), false);
            CLIEngine.ShowMessage(string.Concat($"STAR API Version:".PadRight(displayFieldLength), starHolon.STARNETDNA.STARAPIVersion), false);
            CLIEngine.ShowMessage(string.Concat($".NET Version:".PadRight(displayFieldLength), starHolon.STARNETDNA.DotNetVersion), false);



            //CLIEngine.ShowMessage(string.Concat($"Id:".PadRight(displayFieldLength), starHolon.STARNETDNA.Id != Guid.Empty ? starHolon.STARNETDNA.Id : "None"), false);
            //CLIEngine.ShowMessage(string.Concat($"Name:".PadRight(displayFieldLength), !string.IsNullOrEmpty(starHolon.STARNETDNA.Name) ? starHolon.STARNETDNA.Name : "None"), false);
            //CLIEngine.ShowMessage(string.Concat($"Description:".PadRight(displayFieldLength), !string.IsNullOrEmpty(starHolon.STARNETDNA.Description) ? starHolon.STARNETDNA.Description : "None"), false);
            //CLIEngine.ShowMessage(string.Concat($"{STARNETManager.STARNETHolonUIName} Type:".PadRight(displayFieldLength), starHolon.STARNETDNA.STARNETHolonType), false);
            //CLIEngine.ShowMessage(string.Concat($"Created On:".PadRight(displayFieldLength), starHolon.STARNETDNA.CreatedOn != DateTime.MinValue ? starHolon.STARNETDNA.CreatedOn.ToString() : "None"), false);
            //CLIEngine.ShowMessage(string.Concat($"Created By:".PadRight(displayFieldLength), starHolon.STARNETDNA.CreatedByAvatarId != Guid.Empty ? string.Concat(starHolon.STARNETDNA.CreatedByAvatarUsername, " (", starHolon.STARNETDNA.CreatedByAvatarId.ToString(), ")") : "None"), false);
            //CLIEngine.ShowMessage(string.Concat($"Modified On:".PadRight(displayFieldLength), starHolon.STARNETDNA.ModifiedOn != DateTime.MinValue ? starHolon.STARNETDNA.CreatedOn.ToString() : "None"), false);
            //CLIEngine.ShowMessage(string.Concat($"Modified By:".PadRight(displayFieldLength), starHolon.STARNETDNA.ModifiedByAvatarId != Guid.Empty ? string.Concat(starHolon.STARNETDNA.ModifiedByAvatarUsername, " (", starHolon.STARNETDNA.ModifiedByAvatarId.ToString(), ")") : "None"), false);
            //CLIEngine.ShowMessage(string.Concat($"{STARNETManager.STARNETHolonUIName} Path:".PadRight(displayFieldLength), !string.IsNullOrEmpty(starHolon.STARNETDNA.SourcePath) ? starHolon.STARNETDNA.SourcePath : "None"), false);
            //CLIEngine.ShowMessage(string.Concat($"Published On:".PadRight(displayFieldLength), starHolon.STARNETDNA.PublishedOn != DateTime.MinValue ? starHolon.STARNETDNA.PublishedOn.ToString() : "None"), false);
            //CLIEngine.ShowMessage(string.Concat($"Published By:".PadRight(displayFieldLength), starHolon.STARNETDNA.PublishedByAvatarId != Guid.Empty ? string.Concat(starHolon.STARNETDNA.PublishedByAvatarUsername, " (", starHolon.STARNETDNA.PublishedByAvatarId.ToString(), ")") : "None"), false);
            //CLIEngine.ShowMessage(string.Concat($"{STARNETManager.STARNETHolonUIName} Published Path:".PadRight(displayFieldLength), !string.IsNullOrEmpty(starHolon.STARNETDNA.PublishedPath) ? starHolon.STARNETDNA.PublishedPath : "None"), false);
            //CLIEngine.ShowMessage(string.Concat($"{STARNETManager.STARNETHolonUIName} Filesize:".PadRight(displayFieldLength), starHolon.STARNETDNA.FileSize.ToString()), false);
            //CLIEngine.ShowMessage(string.Concat($"{STARNETManager.STARNETHolonUIName} Published On STARNET:".PadRight(displayFieldLength), starHolon.STARNETDNA.PublishedOnSTARNET ? "True" : "False"), false);
            //CLIEngine.ShowMessage(string.Concat($"{STARNETManager.STARNETHolonUIName} Published To Cloud:".PadRight(displayFieldLength), starHolon.STARNETDNA.PublishedToCloud ? "True" : "False"), false);
            //CLIEngine.ShowMessage(string.Concat($"{STARNETManager.STARNETHolonUIName} Published To OASIS Provider:".PadRight(displayFieldLength), Enum.GetName(typeof(ProviderType), starHolon.STARNETDNA.PublishedProviderType)), false);
            //CLIEngine.ShowMessage(string.Concat($"Launch Target:".PadRight(displayFieldLength), starHolon.STARNETDNA.LaunchTarget), false);
            //CLIEngine.ShowMessage(string.Concat($"{STARNETManager.STARNETHolonUIName} Version:".PadRight(displayFieldLength), starHolon.STARNETDNA.Version), false);
            //CLIEngine.ShowMessage(string.Concat($"Version Sequence:".PadRight(displayFieldLength), starHolon.STARNETDNA.VersionSequence), false);
            //CLIEngine.ShowMessage(string.Concat($"Number Of Versions:".PadRight(displayFieldLength), starHolon.STARNETDNA.NumberOfVersions), false);
            ////CLIEngine.ShowMessage(string.Concat($"OASIS Holon Version:                        ", starHolon.Version), false);
            ////CLIEngine.ShowMessage(string.Concat($"OASIS Holon VersionId:                      ", starHolon.VersionId), false);
            ////CLIEngine.ShowMessage(string.Concat($"OASIS Holon PreviousVersionId:              ", starHolon.PreviousVersionId), false);
            //CLIEngine.ShowMessage(string.Concat($"Downloads:".PadRight(displayFieldLength), starHolon.STARNETDNA.Downloads), false);
            //CLIEngine.ShowMessage(string.Concat($"Installs:".PadRight(displayFieldLength), starHolon.STARNETDNA.Installs), false);
            //CLIEngine.ShowMessage(string.Concat($"Total Downloads:".PadRight(displayFieldLength), starHolon.STARNETDNA.TotalDownloads), false);
            //CLIEngine.ShowMessage(string.Concat($"Total Installs:".PadRight(displayFieldLength), starHolon.STARNETDNA.TotalInstalls), false);
            //CLIEngine.ShowMessage(string.Concat($"Active:".PadRight(displayFieldLength), starHolon.MetaData != null && starHolon.MetaData.ContainsKey("Active") && starHolon.MetaData["Active"] != null && starHolon.MetaData["Active"].ToString() == "1" ? "True" : "False"), false);
            //CLIEngine.ShowMessage(string.Concat($"OASIS Runtime Version:".PadRight(displayFieldLength), starHolon.STARNETDNA.OASISRuntimeVersion), false);
            //CLIEngine.ShowMessage(string.Concat($"OASIS API Version:".PadRight(displayFieldLength), starHolon.STARNETDNA.OASISAPIVersion), false);
            //CLIEngine.ShowMessage(string.Concat($"COSMIC Version:".PadRight(displayFieldLength), starHolon.STARNETDNA.COSMICVersion), false);
            //CLIEngine.ShowMessage(string.Concat($"STAR Runtime Version:".PadRight(displayFieldLength), starHolon.STARNETDNA.STARRuntimeVersion), false);
            //CLIEngine.ShowMessage(string.Concat($"STAR ODK Version:".PadRight(displayFieldLength), starHolon.STARNETDNA.STARODKVersion), false);
            //CLIEngine.ShowMessage(string.Concat($"STARNET Version:".PadRight(displayFieldLength), starHolon.STARNETDNA.STARNETVersion), false);
            //CLIEngine.ShowMessage(string.Concat($"STAR API Version:".PadRight(displayFieldLength), starHolon.STARNETDNA.STARAPIVersion), false);
            //CLIEngine.ShowMessage(string.Concat($".NET Version:".PadRight(displayFieldLength), starHolon.STARNETDNA.DotNetVersion), false);


            //if (showNumbers)
            //    CLIEngine.ShowMessage(string.Concat("Number:                                     ", number), false);

            //CLIEngine.ShowMessage(string.Concat($"Id:                                         ", starHolon.STARNETDNA.Id != Guid.Empty ? starHolon.STARNETDNA.Id : "None"), false);
            //CLIEngine.ShowMessage(string.Concat($"Name:                                       ", !string.IsNullOrEmpty(starHolon.STARNETDNA.Name) ? starHolon.STARNETDNA.Name : "None"), false);
            //CLIEngine.ShowMessage(string.Concat($"Description:                                ", !string.IsNullOrEmpty(starHolon.STARNETDNA.Description) ? starHolon.STARNETDNA.Description : "None"), false);
            //CLIEngine.ShowMessage(string.Concat($"{STARNETManager.STARNETHolonUIName} Type:                         ", starHolon.STARNETDNA.STARNETHolonType), false);
            //CLIEngine.ShowMessage(string.Concat($"Created On:                                 ", starHolon.STARNETDNA.CreatedOn != DateTime.MinValue ? starHolon.STARNETDNA.CreatedOn.ToString() : "None"), false);
            //CLIEngine.ShowMessage(string.Concat($"Created By:                                 ", starHolon.STARNETDNA.CreatedByAvatarId != Guid.Empty ? string.Concat(starHolon.STARNETDNA.CreatedByAvatarUsername, " (", starHolon.STARNETDNA.CreatedByAvatarId.ToString(), ")") : "None"), false);
            //CLIEngine.ShowMessage(string.Concat($"Modified On:                                ", starHolon.STARNETDNA.ModifiedOn != DateTime.MinValue ? starHolon.STARNETDNA.CreatedOn.ToString() : "None"), false);
            //CLIEngine.ShowMessage(string.Concat($"Modified By:                                ", starHolon.STARNETDNA.ModifiedByAvatarId != Guid.Empty ? string.Concat(starHolon.STARNETDNA.ModifiedByAvatarUsername, " (", starHolon.STARNETDNA.ModifiedByAvatarId.ToString(), ")") : "None"), false);
            //CLIEngine.ShowMessage(string.Concat($"{STARNETManager.STARNETHolonUIName} Path:                         ", !string.IsNullOrEmpty(starHolon.STARNETDNA.SourcePath) ? starHolon.STARNETDNA.SourcePath : "None"), false);
            //CLIEngine.ShowMessage(string.Concat($"Published On:                               ", starHolon.STARNETDNA.PublishedOn != DateTime.MinValue ? starHolon.STARNETDNA.PublishedOn.ToString() : "None"), false);
            //CLIEngine.ShowMessage(string.Concat($"Published By:                               ", starHolon.STARNETDNA.PublishedByAvatarId != Guid.Empty ? string.Concat(starHolon.STARNETDNA.PublishedByAvatarUsername, " (", starHolon.STARNETDNA.PublishedByAvatarId.ToString(), ")") : "None"), false);
            //CLIEngine.ShowMessage(string.Concat($"{STARNETManager.STARNETHolonUIName} Published Path:               ", !string.IsNullOrEmpty(starHolon.STARNETDNA.PublishedPath) ? starHolon.STARNETDNA.PublishedPath : "None"), false);
            //CLIEngine.ShowMessage(string.Concat($"{STARNETManager.STARNETHolonUIName} Filesize:                     ", starHolon.STARNETDNA.FileSize.ToString()), false);
            //CLIEngine.ShowMessage(string.Concat($"{STARNETManager.STARNETHolonUIName} Published On STARNET:         ", starHolon.STARNETDNA.PublishedOnSTARNET ? "True" : "False"), false);
            //CLIEngine.ShowMessage(string.Concat($"{STARNETManager.STARNETHolonUIName} Published To Cloud:           ", starHolon.STARNETDNA.PublishedToCloud ? "True" : "False"), false);
            //CLIEngine.ShowMessage(string.Concat($"{STARNETManager.STARNETHolonUIName} Published To OASIS Provider:  ", Enum.GetName(typeof(ProviderType), starHolon.STARNETDNA.PublishedProviderType)), false);
            //CLIEngine.ShowMessage(string.Concat($"Launch Target:                              ", starHolon.STARNETDNA.LaunchTarget), false);
            //CLIEngine.ShowMessage(string.Concat($"{STARNETManager.STARNETHolonUIName} Version:                      ", starHolon.STARNETDNA.Version), false);
            //CLIEngine.ShowMessage(string.Concat($"Version Sequence:                           ", starHolon.STARNETDNA.VersionSequence), false);
            //CLIEngine.ShowMessage(string.Concat($"Number Of Versions:                         ", starHolon.STARNETDNA.NumberOfVersions), false);
            ////CLIEngine.ShowMessage(string.Concat($"OASIS Holon Version:                        ", starHolon.Version), false);
            ////CLIEngine.ShowMessage(string.Concat($"OASIS Holon VersionId:                      ", starHolon.VersionId), false);
            ////CLIEngine.ShowMessage(string.Concat($"OASIS Holon PreviousVersionId:              ", starHolon.PreviousVersionId), false);
            //CLIEngine.ShowMessage(string.Concat($"Downloads:                                  ", starHolon.STARNETDNA.Downloads), false);
            //CLIEngine.ShowMessage(string.Concat($"Installs:                                   ", starHolon.STARNETDNA.Installs), false);
            //CLIEngine.ShowMessage(string.Concat($"Total Downloads:                            ", starHolon.STARNETDNA.TotalDownloads), false);
            //CLIEngine.ShowMessage(string.Concat($"Total Installs:                             ", starHolon.STARNETDNA.TotalInstalls), false);
            //CLIEngine.ShowMessage(string.Concat($"Active:                                     ", starHolon.MetaData != null && starHolon.MetaData.ContainsKey("Active") && starHolon.MetaData["Active"] != null && starHolon.MetaData["Active"].ToString() == "1" ? "True" : "False"), false);

            if (showDetailedInfo)
            {
                //Show base holon info.
            }

            if (showFooter)
                CLIEngine.ShowDivider();
        }

        public virtual void ShowInstalled(T3 starHolon, bool showHeader = true, bool showFooter = true, bool showNumbers = false, int number = 0, bool showUninstallInfo = false, bool showDetailedInfo = false, int displayFieldLength = DEFAULT_FIELD_LENGTH)
        {
            if (DisplayFieldLength > displayFieldLength)
                displayFieldLength = DisplayFieldLength;

            //Show((T1)starHolon, showHeader, false, showNumbers, number, showDetailedInfo);
            Show(ConvertFromT3ToT1(starHolon), showHeader, false, showNumbers, number, showDetailedInfo);

            Console.WriteLine("");
            CLIEngine.ShowMessage(string.Concat($"Downloaded On:".PadRight(displayFieldLength), starHolon.DownloadedOn != DateTime.MinValue ? starHolon.DownloadedOn.ToString() : "None"), false);
            CLIEngine.ShowMessage(string.Concat($"Downloaded By:".PadRight(displayFieldLength), starHolon.DownloadedBy != Guid.Empty ? string.Concat(starHolon.DownloadedByAvatarUsername, " (", starHolon.DownloadedBy.ToString(), ")") : "None"), false);
            CLIEngine.ShowMessage(string.Concat($"Downloaded Path:".PadRight(displayFieldLength), !string.IsNullOrEmpty(starHolon.DownloadedPath) ? starHolon.DownloadedPath : "None"), false);
            CLIEngine.ShowMessage(string.Concat($"Installed On:".PadRight(displayFieldLength), starHolon.InstalledOn != DateTime.MinValue ? starHolon.InstalledOn.ToString() : "None"), false);
            CLIEngine.ShowMessage(string.Concat($"Installed By:".PadRight(displayFieldLength), starHolon.InstalledBy != Guid.Empty ? string.Concat(starHolon.InstalledByAvatarUsername, " (", starHolon.InstalledBy.ToString(), ")") : "None"), false);
            CLIEngine.ShowMessage(string.Concat($"Installed Path:".PadRight(displayFieldLength), !string.IsNullOrEmpty(starHolon.InstalledPath) ? starHolon.InstalledPath : "None"), false);

            if (showUninstallInfo)
            {
                CLIEngine.ShowMessage(string.Concat($"Uninstalled On:".PadRight(displayFieldLength), starHolon.UninstalledOn != DateTime.MinValue ? starHolon.UninstalledOn.ToString() : "None"), false);
                CLIEngine.ShowMessage(string.Concat($"Uninstalled By:".PadRight(displayFieldLength), starHolon.UninstalledBy != Guid.Empty ? string.Concat(starHolon.UninstalledByAvatarUsername, " (", starHolon.UninstalledBy.ToString(), ")") : "None"), false);
            }

            if (showFooter)
                CLIEngine.ShowDivider();
        }

        public void ShowHeader()
        {
            CLIEngine.ShowDivider();
            CLIEngine.ShowMessage(CreateHeader);
            CLIEngine.ShowDivider();
            Console.WriteLine();

            for (int i = 0; i < CreateIntroParagraphs.Count; i++)
                CLIEngine.ShowMessage(CreateIntroParagraphs[i]);

            CLIEngine.ShowDivider();
        }

        public async Task<OASISResult<T1>> FindAsync(string operationName, string idOrName = "", bool showOnlyForCurrentAvatar = false, bool addSpace = true, string STARNETHolonUIName = "Default", ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            Guid id = Guid.Empty;

            if (STARNETHolonUIName == "Default")
                STARNETHolonUIName = STARNETManager.STARNETHolonUIName;

            do
            {
                if (string.IsNullOrEmpty(idOrName))
                {
                    if (!CLIEngine.GetConfirmation($"Do you know the GUID/ID or Name of the {STARNETHolonUIName} you wish to {operationName}? Press 'Y' for Yes or 'N' for No."))
                    {
                        Console.WriteLine("");
                        CLIEngine.ShowWorkingMessage($"Loading {STARNETHolonUIName}'s...");
                        
                        if (showOnlyForCurrentAvatar)
                            ListStarHolons(await STARNETManager.LoadAllForAvatarAsync(STAR.BeamedInAvatar.AvatarId));
                        else
                            ListStarHolons(await STARNETManager.LoadAllAsync(STAR.BeamedInAvatar.AvatarId, null, true, false, 0, providerType: providerType));
                    }
                    else
                        Console.WriteLine("");

                    idOrName = CLIEngine.GetValidInput($"What is the GUID/ID or Name of the {STARNETHolonUIName} you wish to {operationName}?");

                    if (idOrName == "exit")
                        break;
                }

                if (addSpace)
                    Console.WriteLine("");

                if (Guid.TryParse(idOrName, out id))
                {
                    CLIEngine.ShowWorkingMessage($"Loading {STARNETHolonUIName}...");
                    result = await STARNETManager.LoadAsync(STAR.BeamedInAvatar.AvatarId, id, 0, providerType);

                    if (result != null && result.Result != null && !result.IsError && showOnlyForCurrentAvatar && result.Result.STARNETDNA.CreatedByAvatarId != STAR.BeamedInAvatar.AvatarId)
                    {
                        CLIEngine.ShowErrorMessage($"You do not have permission to {operationName} this {STARNETHolonUIName}. It was created by another avatar.");
                        result.Result = default;
                    }
                }
                else
                {
                    CLIEngine.ShowWorkingMessage($"Searching {STARNETHolonUIName}s...");
                    OASISResult<IEnumerable<T1>> searchResults = await STARNETManager.SearchAsync<T1>(STAR.BeamedInAvatar.Id, idOrName, showOnlyForCurrentAvatar, false, 0, providerType);

                    if (searchResults != null && searchResults.Result != null && !searchResults.IsError)
                    {
                        if (searchResults.Result.Count() > 1)
                        {
                            ListStarHolons(searchResults, true);

                            do
                            {
                                int number = CLIEngine.GetValidInputForInt($"What is the number of the {STARNETHolonUIName} you wish to {operationName}?");

                                if (number > 0 && number <= searchResults.Result.Count())
                                    result.Result = searchResults.Result.ElementAt(number - 1);                                 
                                else
                                    CLIEngine.ShowErrorMessage("Invalid number entered. Please try again.");

                            } while (result.Result == null || result.IsError);
                        }
                        else if (searchResults.Result.Count() == 1)
                            result.Result = searchResults.Result.FirstOrDefault();
                        else
                        {
                            idOrName = "";
                            CLIEngine.ShowWarningMessage($"No {STARNETHolonUIName} Found!");
                        }
                    }
                    else
                        CLIEngine.ShowErrorMessage($"An error occured calling STARNETManager.SearchsAsync. Reason: {searchResults.Message}");
                }

                if (result.Result != null && result.Result.STARNETDNA != null)
                {
                    Show(result.Result);

                    if (result.Result.STARNETDNA.NumberOfVersions > 1)
                    {
                        //if (((operationName == "view" || operationName == "use") && CLIEngine.GetConfirmation($"{result.Result.STARNETDNA.NumberOfVersions} versions were found. Do you wish to view the other versions?")) ||
                        //    (!CLIEngine.GetConfirmation($"{result.Result.STARNETDNA.NumberOfVersions} versions were found. Do you wish to {operationName} the latest version ({result.Result.STARNETDNA.Version})?")))
                        if (!CLIEngine.GetConfirmation($"{result.Result.STARNETDNA.NumberOfVersions} versions were found. Do you wish to {operationName} the latest version ({result.Result.STARNETDNA.Version}) or do you wish to view all the versions? Press 'Y' for latest version or 'N' for all versions."))
                        {
                            Console.WriteLine("");
                            CLIEngine.ShowWorkingMessage($"Loading {STARNETHolonUIName} Versions...");
                            OASISResult<IEnumerable<T1>> versionsResult = await STARNETManager.LoadVersionsAsync(result.Result.STARNETDNA.Id, providerType);
                            ListStarHolons(versionsResult);

                            if (operationName != "view" && versionsResult != null && versionsResult.Result != null && !versionsResult.IsError && versionsResult.Result.Count() > 0)
                            {
                                bool versionSelected = false;

                                do
                                {
                                    int version = CLIEngine.GetValidInputForInt($"Which version do you wish to {operationName}? (Enter the Version Sequence that corresponds to the relevant template)");

                                    if (version > 0 && version <= versionsResult.Result.Count())
                                    {
                                        versionSelected = true;
                                        result.Result = versionsResult.Result.ElementAt(version - 1);
                                    }
                                    else
                                        CLIEngine.ShowErrorMessage("Invalid version entered. Please try again.");

                                    if (version == 0)
                                        break;

                                } while (!versionSelected);
                            }
                        }
                        else
                            Console.WriteLine("");

                        if (operationName != "view")
                            Show(result.Result);
                    }
                }

                if (idOrName == "exit")
                    break;

                if (result.Result != null && operationName != "view")
                {
                    if (CLIEngine.GetConfirmation($"Please confirm you wish to {operationName} this {STARNETHolonUIName}?"))
                    {
                        if (operationName == "install")
                        {
                            if (result != null && result.Result != null)
                            {
                                OASISResult<T1> checkResult = await CheckIfAlreadyInstalledAsync(result.Result, providerType);

                                if (checkResult != null && checkResult.Result != null && !checkResult.IsError)
                                {
                                    if (result.MetaData != null && result.MetaData.ContainsKey("Reinstall"))
                                        result.MetaData["Reinstall"] = checkResult.MetaData["Reinstall"];
                                }
                                else if (checkResult.IsError)
                                    result.Result = default;
                            }
                            else
                            {
                                CLIEngine.ShowErrorMessage($"Error occured checking if the {STARNETHolonUIName} is already installed! Reason: Id was not found in the metadata!");
                                result.Result = default;
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine("");
                        result.Result = default;
                        idOrName = "";

                        if (!CLIEngine.GetConfirmation($"Do you wish to search for another {STARNETHolonUIName}?"))
                        {
                            idOrName = "exit";
                            break;
                        }
                    }

                    Console.WriteLine("");
                }
            }
            while (result.Result == null || result.IsError);

            if (idOrName == "exit")
            {
                result.IsError = true;
                result.Message = "User Exited";
            }

            return result;
        }

        public OASISResult<T1> Find(string operationName, string idOrName = "", bool showOnlyForCurrentAvatar = true, bool addSpace = true, string STARNETHolonUIName = "Default", ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            Guid id = Guid.Empty;
            bool reInstall = false;

            do
            {
                if (string.IsNullOrEmpty(idOrName))
                {
                    if (!CLIEngine.GetConfirmation($"Do you know the GUID/ID or Name of the {STARNETManager.STARNETHolonUIName} you wish to {operationName}? Press 'Y' for Yes or 'N' for No."))
                    {
                        Console.WriteLine("");
                        CLIEngine.ShowWorkingMessage($"Loading {STARNETManager.STARNETHolonUIName}s...");

                        if (showOnlyForCurrentAvatar)
                            ListStarHolons(STARNETManager.LoadAllForAvatar(STAR.BeamedInAvatar.AvatarId));
                        else
                            ListStarHolons(STARNETManager.LoadAll(STAR.BeamedInAvatar.AvatarId, null, true, false, 0, providerType: providerType));
                    }
                    else
                        Console.WriteLine("");

                    idOrName = CLIEngine.GetValidInput($"What is the GUID/ID or Name of the {STARNETManager.STARNETHolonUIName} you wish to {operationName}?");

                    if (idOrName == "exit")
                        break;
                }

                if (addSpace)
                    Console.WriteLine("");

                if (Guid.TryParse(idOrName, out id))
                {
                    CLIEngine.ShowWorkingMessage($"Loading {STARNETManager.STARNETHolonUIName}...");
                    result = STARNETManager.Load(STAR.BeamedInAvatar.Id, id, 0, providerType);

                    if (result != null && result.Result != null && !result.IsError && showOnlyForCurrentAvatar && result.Result.STARNETDNA.CreatedByAvatarId != STAR.BeamedInAvatar.AvatarId)
                    {
                        CLIEngine.ShowErrorMessage($"You do not have permission to {operationName} this {STARNETManager.STARNETHolonUIName}. It was created by another avatar.");
                        result.Result = default;
                    }
                }
                else
                {
                    CLIEngine.ShowWorkingMessage($"Searching {STARNETManager.STARNETHolonUIName}'s...");
                    OASISResult<IEnumerable<T1>> searchResults = STARNETManager.Search(STAR.BeamedInAvatar.Id, idOrName, showOnlyForCurrentAvatar, false, 0, providerType);

                    if (searchResults != null && searchResults.Result != null && !searchResults.IsError)
                    {
                        if (searchResults.Result.Count() > 1)
                        {
                            ListStarHolons(searchResults, true);

                            do
                            {
                                int number = CLIEngine.GetValidInputForInt($"What is the number of the {STARNETManager.STARNETHolonUIName} you wish to {operationName}?");

                                if (number > 0 && number <= searchResults.Result.Count())
                                    result.Result = searchResults.Result.ElementAt(number - 1);
                                else
                                    CLIEngine.ShowErrorMessage("Invalid number entered. Please try again.");

                            } while (result.Result == null || result.IsError);
                        }
                        else if (searchResults.Result.Count() == 1)
                            result.Result = searchResults.Result.FirstOrDefault();
                        else
                        {
                            idOrName = "";
                            CLIEngine.ShowWarningMessage($"No {STARNETManager.STARNETHolonUIName} Found!");
                        }
                    }
                    else
                        CLIEngine.ShowErrorMessage($"An error occured calling STARNETManager.SearchsAsync. Reason: {searchResults.Message}");
                }

                if (result.Result != null && result.Result.STARNETDNA != null)
                {
                    Show(result.Result);

                    if (result.Result.STARNETDNA.NumberOfVersions > 1)
                    {
                        if ((operationName == "view" && CLIEngine.GetConfirmation($"{result.Result.STARNETDNA.NumberOfVersions} versions were found. Do you wish to view the other versions?")) ||
                            (!CLIEngine.GetConfirmation($"{result.Result.STARNETDNA.NumberOfVersions} versions were found. Do you wish to {operationName} the latest version ({result.Result.STARNETDNA.Version})?")))
                        {
                            Console.WriteLine("");
                            CLIEngine.ShowWorkingMessage($"Loading {STARNETManager.STARNETHolonUIName} Versions...");
                            OASISResult<IEnumerable<T1>> versionsResult = STARNETManager.LoadVersions(result.Result.STARNETDNA.Id, providerType);
                            ListStarHolons(versionsResult);

                            if (operationName != "view" && versionsResult != null && versionsResult.Result != null && !versionsResult.IsError && versionsResult.Result.Count() > 0)
                            {
                                bool versionSelected = false;

                                do
                                {
                                    int version = CLIEngine.GetValidInputForInt($"Which version do you wish to {operationName}? (Enter the Version Sequence that corresponds to the relevant template)");

                                    if (version > 0 && version <= versionsResult.Result.Count())
                                    {
                                        versionSelected = true;
                                        result.Result = versionsResult.Result.ElementAt(version - 1);
                                    }
                                    else
                                        CLIEngine.ShowErrorMessage("Invalid version entered. Please try again.");

                                    if (version == 0)
                                        break;

                                } while (!versionSelected);
                            }
                        }
                        else
                            Console.WriteLine("");

                        if (operationName != "view")
                            Show(result.Result);
                    }
                }

                if (idOrName == "exit")
                    break;

                if (result.Result != null && operationName != "view")
                {
                    if (CLIEngine.GetConfirmation($"Please confirm you wish to {operationName} this {STARNETManager.STARNETHolonUIName}?"))
                    {
                        if (operationName == "install")
                        {
                            if (result != null && result.Result != null)
                            {
                                OASISResult<T1> checkResult = CheckIfAlreadyInstalled(result.Result, providerType);

                                if (checkResult != null && checkResult.Result != null && !checkResult.IsError)
                                {
                                    if (result.MetaData != null && result.MetaData.ContainsKey("Reinstall"))
                                        result.MetaData["Reinstall"] = checkResult.MetaData["Reinstall"];
                                }
                                else if (checkResult.IsError)
                                    result.Result = default;
                            }
                            else
                            {
                                CLIEngine.ShowErrorMessage($"Error occured checking if the {STARNETManager.STARNETHolonUIName} is already installed! Reason: Id was not found in the metadata!");
                                result.Result = default;
                            }
                        }

                    }
                    else
                    {
                        if (CLIEngine.GetConfirmation($"Do you wish to search for another {STARNETManager.STARNETHolonUIName}?"))
                            result.Result = default;
                        else
                            break;
                    }

                    Console.WriteLine("");
                }

            }
            while (result.Result == null || result.IsError);

            if (idOrName == "exit")
            {
                result.IsError = true;
                result.Message = "User Exited";
            }

            return result;
        }

        private async Task<OASISResult<T1>> FindForProviderAsync(string operationName, string idOrName = "", bool showOnlyForCurrentAvatar = true, bool addSpace = true, bool simpleWizard = true, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            ProviderType largeFileProviderType = ProviderType.IPFSOASIS;

            if (!simpleWizard)
            {
                if (!CLIEngine.GetConfirmation("Do you wish to download from the cloud or from the OASIS? Press 'Y' for the cloud or N' for the OASIS."))
                {
                    Console.WriteLine("");
                    object largeProviderTypeObject = CLIEngine.GetValidInputForEnum($"What OASIS provider do you wish to install the {STARNETManager.STARNETHolonUIName} from? (The default is IPFSOASIS)", typeof(ProviderType));

                    if (largeProviderTypeObject != null)
                    {
                        largeFileProviderType = (ProviderType)largeProviderTypeObject;
                        result = await FindAsync(operationName, idOrName, showOnlyForCurrentAvatar, addSpace, providerType: largeFileProviderType);
                    }
                    else
                        OASISErrorHandling.HandleError(ref result, "Error occured in FindForProviderAsync, reason: largeProviderTypeObject is null!");
                }
                else
                {
                    Console.WriteLine("");
                    result = await FindAsync(operationName, idOrName, showOnlyForCurrentAvatar, addSpace, providerType: providerType);
                }
            }
            else
                result = await FindAsync(operationName, idOrName, showOnlyForCurrentAvatar, addSpace, providerType: providerType);

            return result;
        }

        private OASISResult<T1> FindForProvider(string operationName, string idOrName = "", bool showOnlyForCurrentAvatar = true, bool addSpace = true, bool simpleWizard = true, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            ProviderType largeFileProviderType = ProviderType.IPFSOASIS;

            if (!simpleWizard)
            {
                if (!CLIEngine.GetConfirmation("Do you wish to download from the cloud or from the OASIS? Press 'Y' for the cloud or N' for the OASIS."))
                {
                    Console.WriteLine("");
                    object largeProviderTypeObject = CLIEngine.GetValidInputForEnum($"What OASIS provider do you wish to install the {STARNETManager.STARNETHolonUIName} from? (The default is IPFSOASIS)", typeof(ProviderType));

                    if (largeProviderTypeObject != null)
                    {
                        largeFileProviderType = (ProviderType)largeProviderTypeObject;
                        result = Find(operationName, idOrName, showOnlyForCurrentAvatar, addSpace, providerType: largeFileProviderType);
                    }
                    else
                        OASISErrorHandling.HandleError(ref result, "Error occured in FindForProvider, reason: largeProviderTypeObject is null!");
                }
                else
                {
                    Console.WriteLine("");
                    result = Find(operationName, idOrName, showOnlyForCurrentAvatar, addSpace, providerType: largeFileProviderType);
                }
            }
            else
                result = Find(operationName, idOrName, showOnlyForCurrentAvatar, addSpace, providerType: largeFileProviderType);

            return result;
        }


        //public async Task<OASISResult<T3>> FindForProviderAndInstallIfNotInstalledAsync(string operationName, string idOrName = "", bool showOnlyForCurrentAvatar = true, string STARNETHolonUIName = "", ProviderType providerType = ProviderType.Default)
        public async Task<OASISResult<T3>> FindForProviderAndInstallIfNotInstalledAsync(string operationName, string idOrName = "", bool showOnlyForCurrentAvatar = true, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T3> result = new OASISResult<T3>();
            OASISResult<T1> findResult = await FindForProviderAsync(operationName, idOrName, showOnlyForCurrentAvatar, providerType: providerType);

            if (findResult != null && findResult.Result != null && !findResult.IsError)
            {
                //OASISResult<bool> installedResult = await STARNETManager.IsInstalledAsync(STAR.BeamedInAvatar.Id, findResult.Result.STARNETDNA.Id, findResult.Result.STARNETDNA.VersionSequence, providerType);
                OASISResult<bool> installedResult = await STARNETManager.IsInstalledAsync(STAR.BeamedInAvatar.Id, findResult.Result.STARNETDNA.Id, findResult.Result.STARNETDNA.Version, providerType);

                if (installedResult != null && !installedResult.IsError)
                {
                    if (!installedResult.Result)
                    {
                        if (CLIEngine.GetConfirmation($"The selected {STARNETManager.STARNETHolonUIName} is not currently installed. Do you wish to install it now?"))
                        {
                            result = await DownloadAndInstallAsync(findResult.Result.STARNETDNA.Id.ToString(), InstallMode.DownloadAndInstall, providerType);

                            if (!(result != null && result.Result != null && !result.IsError))
                                OASISErrorHandling.HandleError(ref result, $"Error occured installing the {STARNETManager.STARNETHolonUIName}. Reason: {result.Message}");
                        }
                        else
                        {
                            Console.WriteLine("");
                            result.Message = "User Declined Installation";
                            result.IsError = true;
                        }
                    }
                    else
                    {
                        result = await STARNETManager.LoadInstalledAsync(STAR.BeamedInAvatar.Id, findResult.Result.STARNETDNA.Id, findResult.Result.STARNETDNA.VersionSequence, providerType);

                        if (!(result != null && result.Result != null && !result.IsError))
                            OASISErrorHandling.HandleError(ref result, $"Error occured loading the {STARNETManager.STARNETHolonUIName}. Reason: {result.Message}");
                    }
                }
                else
                    CLIEngine.ShowErrorMessage($"Error occured checking if {STARNETManager.STARNETHolonUIName} is installed. Reason: {installedResult.Message}");
            }
            else
            {
                Console.WriteLine("");
                CLIEngine.ShowErrorMessage($"Error occured finding {STARNETManager.STARNETHolonUIName}. Reason: {findResult.Message}");
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(findResult, result);
            }

            return result;
        }

        //TODO: Finish implementing later!
        //public OASISResult<T3> FindForProviderAndInstallIfNotInstalled(string operationName, string idOrName = "", bool showOnlyForCurrentAvatar = true, string STARNETHolonUIName = "", ProviderType providerType = ProviderType.Default)
        //{
        //    OASISResult<T3> result = new OASISResult<T3>();
        //    OASISResult<T1> downloadedCelestialBodyDNA = STARCLI.CelestialBodiesMetaDataDNA.FindForProvider(operationName, idOrName, showOnlyForCurrentAvatar, STARNETHolonUIName: STARNETHolonUIName, providerType: providerType);

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


        //private async Task<OASISResult<T1>> FindForProviderAndInstallAsync(string operationName, string downloadPath, string installPath, string idOrName = "", bool showOnlyForCurrentAvatar = true, bool addSpace = true, bool simpleWizard = true, InstallMode installMode = InstallMode.DownloadAndInstall, ProviderType providerType = ProviderType.Default)
        //{
        //    OASISResult<T1> result = new OASISResult<T1>();
        //    ProviderType largeFileProviderType = ProviderType.IPFSOASIS;


        //    //OASISResult<T1> result = await FindForProviderAsync(operation, idOrName, false, false, true, providerType);

        //    //if (result != null && result.Result != null && !result.IsError)
        //    //{
        //    //    if (result.MetaData != null && result.MetaData.ContainsKey("Reinstall") && !string.IsNullOrEmpty(result.MetaData["Reinstall"]) && result.MetaData["Reinstall"] == "1" && installMode == InstallMode.DownloadAndInstall)
        //    //        installMode = InstallMode.DownloadAndReInstall;

        //    //    installResult = await CheckIfInstalledAndInstallAsync(result.Result, downloadPath, installPath, installMode, "", providerType);
        //    //}

        //    OASISResult<T1> templateResult = await FindForProviderAsync(operationName, idOrName, showOnlyForCurrentAvatar,addSpace, simpleWizard, providerType: providerType);

        //    if (templateResult != null && templateResult.Result != null && !templateResult.IsError)
        //    {
        //        if (result.MetaData != null && result.MetaData.ContainsKey("Reinstall") && !string.IsNullOrEmpty(result.MetaData["Reinstall"]) && result.MetaData["Reinstall"] == "1" && installMode == InstallMode.DownloadAndInstall)
        //            installMode = InstallMode.DownloadAndReInstall;

        //        DownloadAndInstallAsync(idOrName, downloadPath, installPath, templateResult.Result, installMode, providerType);

        //        //OASISResult<bool> oappTemplateInstalledResult = await CheckIfInstalledAndInstallAsync(templateResult.Result, downloadPath, installPath, installMode, )

        //        //if (oappTemplateInstalledResult != null && !oappTemplateInstalledResult.IsError)
        //        //{
        //        //    if (!oappTemplateInstalledResult.Result)
        //        //    {
        //        //        if (CLIEngine.GetConfirmation($"The selected OAPP Template is not currently installed. Do you wish to install it now?"))
        //        //        {
        //        //            OASISResult<InstalledOAPPTemplate> installResult = await STARCLI.OAPPTemplates.DownloadAndInstallAsync(templateResult.Result.STARNETDNA.Id.ToString(), InstallMode.DownloadAndInstall, providerType);

        //        //            if (installResult.Result != null && !installResult.IsError)
        //        //            {
        //        //                templateInstalled = true;
        //        //                OAPPTemplate = installResult.Result;
        //        //            }
        //        //        }
        //        //    }
        //        //    else
        //        //    {
        //        //        templateInstalled = true;
        //        //        OAPPTemplate = templateResult.Result;
        //        //    }
        //        //}
        //        //else
        //        //    CLIEngine.ShowErrorMessage($"Error occured checking if OAPP Template is installed. Reason: {oappTemplateInstalledResult.Message}");
        //    }
        //    else
        //        CLIEngine.ShowErrorMessage($"Error occured finding OAPP Template. Reason: {templateResult.Message}");


        //    return result;
        //}


        public async Task<OASISResult<T3>> FindAndInstallIfNotInstalledAsync(string operationName, string idOrName = "", bool showOnlyForCurrentAvatar = true, string STARNETHolonUIName = "", ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T3> result = new OASISResult<T3>();
            OASISResult<T1> downloadedCelestialBodyDNA = await FindAsync(operationName, idOrName, showOnlyForCurrentAvatar, STARNETHolonUIName: STARNETHolonUIName, providerType: providerType);

            if (downloadedCelestialBodyDNA != null && downloadedCelestialBodyDNA.Result != null && !downloadedCelestialBodyDNA.IsError)
            {
                OASISResult<bool> celestialBodyDNAInstalledResult = await STAR.STARAPI.CelestialBodiesMetaDataDNA.IsInstalledAsync(STAR.BeamedInAvatar.Id, downloadedCelestialBodyDNA.Result.STARNETDNA.Id, downloadedCelestialBodyDNA.Result.STARNETDNA.VersionSequence, providerType);

                if (celestialBodyDNAInstalledResult != null && !celestialBodyDNAInstalledResult.IsError)
                {
                    if (!celestialBodyDNAInstalledResult.Result)
                    {
                        if (CLIEngine.GetConfirmation($"The selected {STARNETHolonUIName} is not currently installed. Do you wish to install it now?"))
                        {
                            OASISResult<T3> installResult = await DownloadAndInstallAsync(downloadedCelestialBodyDNA.Result.STARNETDNA.Id.ToString(), InstallMode.DownloadAndInstall, providerType);

                            if (installResult.Result != null && !installResult.IsError)
                                result = installResult;
                            else
                                OASISErrorHandling.HandleError(ref result, $"Error occured installing the {STARNETHolonUIName}. Reason: {installResult.Message}");
                        }
                    }
                    else
                    {
                        OASISResult<T3> loadResult = await STARNETManager.LoadInstalledAsync(STAR.BeamedInAvatar.Id, downloadedCelestialBodyDNA.Result.STARNETDNA.Id, downloadedCelestialBodyDNA.Result.STARNETDNA.VersionSequence, providerType);

                        if (loadResult != null && loadResult.Result != null && !loadResult.IsError)
                            result = loadResult;
                        else
                            OASISErrorHandling.HandleError(ref result, $"Error occured loading the {STARNETHolonUIName}. Reason: {loadResult.Message}");
                    }
                }
                else
                    CLIEngine.ShowErrorMessage($"Error occured checking if {STARNETHolonUIName} is installed. Reason: {celestialBodyDNAInstalledResult.Message}");
            }
            else
                CLIEngine.ShowErrorMessage($"Error occured finding {STARNETHolonUIName}. Reason: {downloadedCelestialBodyDNA.Message}");

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
            string colon = ":";

            if (!displayColon)
                colon = "";

            CLIEngine.ShowMessage(string.Concat($"{heading}{colon}".PadRight(displayFieldLength), value), false);
        }



        private OASISResult<IEnumerable<T>> ListStarHolons<T>(OASISResult<IEnumerable<T>> starHolons, bool showNumbers = false) where T : ISTARNETHolon, new()
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
                            Show(starHolons.Result.ElementAt(i), i == 0, true, showNumbers, i + 1);
                    }
                    else
                        CLIEngine.ShowWarningMessage($"No {STARNETManager.STARNETHolonUIName}s Found.");
                }
                else
                    CLIEngine.ShowErrorMessage($"Error occured loading {STARNETManager.STARNETHolonUIName}s. Reason: {starHolons.Message}");
            }
            else
                CLIEngine.ShowErrorMessage($"Unknown error occured loading {STARNETManager.STARNETHolonUIName}s.");

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
                        OASISResult<T3> uninstallResult = await STARNETManager.UninstallAsync(STAR.BeamedInAvatar.Id, result.Result.STARNETDNA.Id, result.Result.STARNETDNA.Version, providerType);

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
                        OASISResult<T1> publishResult = await PublishAsync(holon.STARNETDNA.SourcePath, defaultLaunchMode: DefaultLaunchMode.Optional, providerType: providerType);

                        if (!(publishResult != null && !publishResult.IsError && publishResult.Result != null))
                            CLIEngine.ShowErrorMessage($"Error publishing the {STARNETManager.STARNETHolonUIName} before installing it! Reason: {publishResult.Message}");

                        //The publish routine automatically installs at the end (if the user agrees) so no need to install again here.
                        //if (publishResult != null && !publishResult.IsError && publishResult.Result != null)
                        //    installResult = await InstallAsync(holon, downloadPath, installPath, installMode, fullPathToPublishedFile, providerType);
                        //else
                        //    CLIEngine.ShowErrorMessage($"Error publishing the {STARNETManager.STARNETHolonUIName} before installing it! Reason: {publishResult.Message}");
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
                    CLIEngine.ShowMessage("Downloading...");
                    Console.WriteLine("");
                    break;

                case STARNETHolonInstallStatus.Installed:
                    CLIEngine.ShowSuccessMessage($"{STARNETManager.STARNETHolonUIName} Installed Successfully");
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