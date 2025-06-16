using System.Diagnostics;
using NextGenSoftware.CLI.Engine;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.STAR.CLI.Lib.Enums;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces;
using NextGenSoftware.OASIS.API.ONODE.Core.Enums.STARHolon;
using NextGenSoftware.OASIS.API.ONODE.Core.Events.STARHolon;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Holons;


namespace NextGenSoftware.OASIS.STAR.CLI.Lib
{
    public class STARUIBase<T1, T2, T3>
        where T1 : ISTARNETHolon, new()
        where T2 : IDownloadedSTARHolon, new()
        where T3 : IInstalledSTARNETHolon, new()
    {

        public ISTARManagerBase<T1, T2, T3> STARManager { get; set; }
        public bool IsInit { get; set; }
        public string CreateHeader { get; set; }
        public List<string> CreateIntroParagraphs { get; set; }
        public string SourcePath { get; set; }
        public string SourceSTARDNAKey { get; set; }
        public string PublishedPath { get; set; }
        public string PublishedSTARDNAKey { get; set; }
        public string DownloadedPath { get; set; }
        public string DownloadSTARDNAKey { get; set; }
        public string InstalledPath { get; set; }
        public string InstalledSTARDNAKey { get; set; }
        

        public STARUIBase(ISTARManagerBase<T1, T2, T3> starManager, string createHeader, List<string> createIntroParagraphs, string sourcePath = "", string sourceSTARDNAKey = "", string publishedPath = "", string publishedSTARDNAKey = "", string downloadedPath = "", string downloadSTARDNAKey = "", string installedPath = "", string installedSTARDNAKey = "")
        {
            starManager.OnDownloadStatusChanged += OnDownloadStatusChanged;
            starManager.OnInstallStatusChanged += OnInstallStatusChanged;
            starManager.OnPublishStatusChanged += OnPublishStatusChanged;
            starManager.OnUploadStatusChanged += OnUploadStatusChanged;

            CreateHeader = createHeader;
            CreateIntroParagraphs = createIntroParagraphs;
            IsInit = true;
            STARManager = starManager;
            SourcePath = sourcePath;
            SourceSTARDNAKey = sourceSTARDNAKey;
            PublishedPath = publishedPath;
            PublishedSTARDNAKey = publishedSTARDNAKey;
            DownloadedPath = downloadedPath;
            DownloadSTARDNAKey = downloadSTARDNAKey;
            InstalledPath = installedPath;
            InstalledSTARDNAKey = installedSTARDNAKey;
        }

        public void Dispose()
        {
            STARManager.OnDownloadStatusChanged -= OnDownloadStatusChanged;
            STARManager.OnInstallStatusChanged -= OnInstallStatusChanged;
            STARManager.OnPublishStatusChanged -= OnPublishStatusChanged;
            STARManager.OnUploadStatusChanged -= OnUploadStatusChanged;
        }

        public async Task CreateAsync(object createParams, ProviderType providerType = ProviderType.Default)
        {
            ShowHeader();

            string holonName = CLIEngine.GetValidInput($"What is the name of the {STARManager.STARHolonUIName}?");

            if (holonName == "exit")
                return;

            string holonDesc = CLIEngine.GetValidInput($"What is the description of the {STARManager.STARHolonUIName}?");

            if (holonDesc == "exit")
                return;

            object holonSubType = CLIEngine.GetValidInputForEnum($"What type of {STARManager.STARHolonUIName} do you wish to create?", STARManager.STARHolonSubType);

            if (holonSubType != null)
            {
                if (holonSubType.ToString() == "exit")
                    return;

                //Type STARHolonType = (Type)value;
                string holonPath = "";

                if (Path.IsPathRooted(SourcePath) || string.IsNullOrEmpty(STAR.STARDNA.BasePath))
                    holonPath = SourcePath;
                else
                    holonPath = Path.Combine(STAR.STARDNA.BasePath, SourcePath);


                if (!CLIEngine.GetConfirmation($"Do you wish to create the {STARManager.STARHolonUIName} in the default path defined in the STARDNA as '{SourceSTARDNAKey}'? The current path points to: {holonPath}"))
                    holonPath = CLIEngine.GetValidFolder($"Where do you wish to create the {STARManager.STARHolonUIName}?");

                holonPath = Path.Combine(holonPath, holonName);

                Console.WriteLine("");
                CLIEngine.ShowWorkingMessage($"Generating {STARManager.STARHolonUIName}...");
                //OASISResult<T1> starHolonResult = await STARManager.CreateAsync(STAR.BeamedInAvatar.Id, holonName, holonDesc, Type, holonPath, providerType);
                OASISResult<T1> starHolonResult =  await STARManager.CreateAsync(STAR.BeamedInAvatar.Id, holonName, holonDesc, holonSubType, holonPath, null, providerType);

                if (starHolonResult != null)
                {
                    if (!starHolonResult.IsError && starHolonResult.Result != null)
                    {
                        CLIEngine.ShowSuccessMessage($"{STARManager.STARHolonUIName} Successfully Generated.");
                        Show(starHolonResult.Result);
                        Console.WriteLine("");

                        if (CLIEngine.GetConfirmation($"Do you wish to open the {STARManager.STARHolonUIName} folder now?"))
                            Process.Start("explorer.exe", holonPath);

                        Console.WriteLine("");
                    }
                }
                else
                    CLIEngine.ShowErrorMessage($"Unknown Error Occured.");
            }
        }

        public async Task EditAsync(string idOrName = "", object editParams = null, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> loadResult = await LoadAsync(idOrName, "edit", true, providerType);
            bool changesMade = false;

            if (loadResult != null && loadResult.Result != null && !loadResult.IsError)
            {
                if (CLIEngine.GetConfirmation($"Do you wish to edit the {STARManager.STARHolonUIName} Name?"))
                {
                    Console.WriteLine("");
                    loadResult.Result.STARHolonDNA.Name = CLIEngine.GetValidInput($"What is the new name of the {STARManager.STARHolonUIName}?");
                    changesMade = true;
                }
                else
                    Console.WriteLine("");

                if (CLIEngine.GetConfirmation($"Do you wish to edit the {STARManager.STARHolonUIName} Description?"))
                {
                    Console.WriteLine("");
                    loadResult.Result.STARHolonDNA.Description = CLIEngine.GetValidInput($"What is the new description of the {STARManager.STARHolonUIName}?");
                    changesMade = true;
                }
                else
                    Console.WriteLine("");

                if (CLIEngine.GetConfirmation($"Do you wish to edit the {STARManager.STARHolonUIName} Type?"))
                {
                    Console.WriteLine("");
                    object holonSubType = CLIEngine.GetValidInputForEnum($"What is the new type of the {STARManager.STARHolonUIName}?", STARManager.STARHolonSubType);

                    if (holonSubType != null)
                    {
                        if (holonSubType.ToString() == "exit")
                            return;

                        loadResult.Result.STARHolonDNA.STARHolonType = holonSubType;
                        changesMade = true;
                    }
                }
                else
                    Console.WriteLine("");

                if (CLIEngine.GetConfirmation("Do you wish to edit the launch target?"))
                {
                    Console.WriteLine("");
                    loadResult.Result.STARHolonDNA.LaunchTarget = CLIEngine.GetValidInput($"What is the new launch target of the {STARManager.STARHolonUIName}?");
                    changesMade = true;
                }
                else
                    Console.WriteLine("");

                if (changesMade)
                {
                    OASISResult<T1> result = await STARManager.EditAsync(STAR.BeamedInAvatar.Id, loadResult.Result, loadResult.Result.STARHolonDNA, providerType);
                    Console.WriteLine("");
                    CLIEngine.ShowWorkingMessage($"Saving {STARManager.STARHolonUIName}...");

                    if (result != null && !result.IsError && result.Result != null)
                    {
                        (result, bool saveResult) = ErrorHandling.HandleResponse(result, await STARManager.WriteDNAAsync(result.Result.STARHolonDNA, result.Result.STARHolonDNA.SourcePath), "Error occured saving the STARHolonDNA. Reason: ", $"{STARManager.STARHolonUIName} Successfully Updated.");

                        if (saveResult)
                            Show(result.Result);
                    }
                    else
                        CLIEngine.ShowErrorMessage($"An error occured updating the {STARManager.STARHolonUIName}. Reason: {result.Message}");
                }

                if (loadResult.Result.STARHolonDNA.PublishedOn != DateTime.MinValue && CLIEngine.GetConfirmation($"Do you wish to upload any changes you have made in the Source folder ({loadResult.Result.STARHolonDNA.SourcePath})? The version number will remain the same ({loadResult.Result.STARHolonDNA.Version})."))
                    await PublishAsync(loadResult.Result.STARHolonDNA.SourcePath, true, providerType);
                else
                    Console.WriteLine("");
            }
            else
            {
                Console.WriteLine("");
                CLIEngine.ShowErrorMessage($"An error occured loading the {STARManager.STARHolonUIName}. Reason: {loadResult.Message}");
            }
        }

        public async Task DeleteAsync(string idOrName = "", bool softDelete = true, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = await LoadAsync(idOrName, "delete", true, providerType);

            if (result != null && !result.IsError && result.Result != null)
            {
                if (CLIEngine.GetConfirmation($"Are you sure you wish to delete this {STARManager.STARHolonUIName}? This will also delete the {STARManager.STARHolonUIName} from the Source and Published folders and remove it from the STARNET Store (if you have already published it)"))
                {
                    Console.WriteLine("");
                    bool deleteDownload = CLIEngine.GetConfirmation($"Do you wish to also delete the correponding downloaded {STARManager.STARHolonUIName}? (if there is any)");

                    Console.WriteLine("");
                    bool deleteInstall = CLIEngine.GetConfirmation($"Do you wish to also delete the correponding installed {STARManager.STARHolonUIName}? (if there is any). This is different to uninstalling because uninstalled {STARManager.STARHolonUIName}s are still visible with the 'list uninstalled' sub-command and have the option to re-install. Whereas once it is deleted it is gone forever!");

                    Console.WriteLine("");
                    if (CLIEngine.GetConfirmation("ARE YOU SURE YOU WITH TO PERMANENTLY DELETE THE OAPP TEMPLATE? IT WILL NOT BE POSSIBLE TO RECOVER AFTRWARDS!", ConsoleColor.Red))
                    {
                        Console.WriteLine("");
                        CLIEngine.ShowWorkingMessage($"Deleting {STARManager.STARHolonUIName}...");
                        result = await STARManager.DeleteAsync(STAR.BeamedInAvatar.Id, result.Result, result.Result.STARHolonDNA.VersionSequence, true, deleteDownload, deleteInstall, providerType);

                        if (result != null && !result.IsError && result.Result != null)
                            CLIEngine.ShowSuccessMessage($"{STARManager.STARHolonUIName} Successfully Deleted.");
                        else
                            CLIEngine.ShowErrorMessage($"An error occured deleting the {STARManager.STARHolonUIName}. Reason: {result.Message}");
                    }
                }
            }
            else
                CLIEngine.ShowErrorMessage($"An error occured loading the {STARManager.STARHolonUIName}. Reason: {result.Message}");
        }

        public async Task PublishAsync(string sourcePath = "", bool edit = false, ProviderType providerType = ProviderType.Default)
        {
            bool generateOAPP = true;
            bool uploadOAPPToCloud = true;
            ProviderType OAPPBinaryProviderType = ProviderType.None;  
            string launchTarget = "";
            string publishPath = "";
            string launchTargetQuestion = "";
            bool simpleWizard = false;

            if (CLIEngine.GetConfirmation("Do you wish to launch the Simple or Advanced Wizard? The Simple Wizard will use defaults (recommended) but the Advanced Wizard will allow greater control and customisation. Press 'Y' for Simple or 'N' for Advanced."))
                simpleWizard = true;

            if (string.IsNullOrEmpty(sourcePath))
            {
                Console.WriteLine("");
                launchTargetQuestion = $"What is the relative path (from the root of the path given above, e.g bin\\launch.exe) to the launch target for the {STARManager.STARHolonUIName}? (This could be the exe or batch file for a desktop or console app, or the index.html page for a website, etc)";
                sourcePath = CLIEngine.GetValidFolder($"What is the full path to the {STARManager.STARHolonUIName} directory?", false);
            }

            OASISResult<ISTARHolonDNA> DNAResult = await STARManager.ReadDNAFromSourceOrInstallFolderAsync<ISTARHolonDNA>(sourcePath);

            if (DNAResult != null && DNAResult.Result != null && !DNAResult.IsError)
            {
                OASISResult<T1> loadResult = await STARManager.LoadAsync(STAR.BeamedInAvatar.Id, DNAResult.Result.Id, 0, providerType);

                if (loadResult != null && loadResult.Result != null && !loadResult.IsError)
                {
                    loadResult.Result.STARHolonDNA.Version = DNAResult.Result.Version; //Update the version from the JSON file.
                    Show(loadResult.Result);

                    if (!CLIEngine.GetConfirmation($"Is this the correct {STARManager.STARHolonUIName} you wish to publish?"))
                    {
                        Console.WriteLine("");
                        return;
                    }

                    launchTarget = loadResult.Result.STARHolonDNA.LaunchTarget;
                    Console.WriteLine("");

                    //object templateType = Enum.Parse(STARManager.STARHolonSubType, DNAResult.Result.STARHolonType.ToString());
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

                    Console.WriteLine("");
                    bool registerOnSTARNET = CLIEngine.GetConfirmation($"Do you wish to publish to STARNET? If you select 'Y' to this question then your {STARManager.STARHolonUIName} will be published to STARNET where others will be able to find, download and install. If you select 'N' then only the .{STARManager.STARHolonFileExtention} install file will be generated on your local device, which you can distribute as you please. This file will also be generated even if you publish to STARNET.");
                    Console.WriteLine("");

                    if (registerOnSTARNET && !simpleWizard)
                    {
                        CLIEngine.ShowMessage($"Do you wish to publish/upload the .{STARManager.STARHolonFileExtention} file to an OASIS Provider or to the cloud or both? Depending on which OASIS Provider is chosen such as IPFSOASIS there may issues such as speed, relialbility etc for such a large file. If you choose to upload to the cloud this could be faster and more reliable (but there is a limit of 5 OAPPs on the free plan and you will need to upgrade to upload more than 5 OAPPs). You may want to choose to use both to add an extra layer of redundancy (recommended).");

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

                    if (Path.IsPathRooted(PublishedPath) || string.IsNullOrEmpty(STAR.STARDNA.BasePath))
                        publishPath = PublishedPath;
                    else
                        publishPath = Path.Combine(STAR.STARDNA.BasePath, PublishedPath);

                    if (!simpleWizard)
                    {
                        if (!CLIEngine.GetConfirmation($"Do you wish to publish the {STARManager.STARHolonUIName} to the default publish folder defined in the STARDNA as {PublishedSTARDNAKey} : {publishPath}?"))
                        {
                            Console.WriteLine("");

                            if (CLIEngine.GetConfirmation($"Do you wish to publish the {STARManager.STARHolonUIName} to: {Path.Combine(sourcePath, "Published")}?"))
                                publishPath = Path.Combine(sourcePath, "Published");
                            else
                            {
                                Console.WriteLine("");
                                publishPath = CLIEngine.GetValidFolder($"Where do you wish to publish the {STARManager.STARHolonUIName}?", true);
                            }
                        }
                    }

                    publishPath = new DirectoryInfo(publishPath).FullName;

                    Console.WriteLine("");
                    CLIEngine.ShowWorkingMessage($"Publishing {STARManager.STARHolonUIName}...");
                    OASISResult<T1> publishResult = await STARManager.PublishAsync(STAR.BeamedInAvatar.Id, sourcePath, launchTarget, publishPath, registerOnSTARNET, generateOAPP, uploadOAPPToCloud, edit, providerType, OAPPBinaryProviderType);

                    if (publishResult != null && !publishResult.IsError && publishResult.Result != null)
                    {
                        Show(publishResult.Result);

                        if (CLIEngine.GetConfirmation($"Do you wish to install the {STARManager.STARHolonUIName} now?"))
                            await DownloadAndInstallAsync(publishResult.Result.STARHolonDNA.Id.ToString(), InstallMode.DownloadAndInstall, providerType);

                        Console.WriteLine("");
                    }
                    else
                    {
                        if (publishResult.Message.Contains("Please make sure you increment the version"))
                        {
                            if (CLIEngine.GetConfirmation($"Do you wish to open the {STARManager.STARHolonDNAFileName} file now?"))
                                Process.Start("explorer.exe", Path.Combine(DNAResult.Result.SourcePath, STARManager.STARHolonDNAFileName));
                        }
                        else
                            CLIEngine.ShowErrorMessage($"An error occured publishing the {STARManager.STARHolonUIName}. Reason: {publishResult.Message}");

                        Console.WriteLine("");
                    }
                }
                else
                    CLIEngine.ShowErrorMessage($"The {STARManager.STARHolonUIName} could not be found for id {DNAResult.Result.Id} found in the {STARManager.STARHolonDNAFileName} file. It could be corrupt, the id could be wrong or you may not have permission, please check and try again, or create a new {STARManager.STARHolonUIName}.");
            }
            else
                CLIEngine.ShowErrorMessage($"The {STARManager.STARHolonDNAFileName} file could not be found! Please ensure it is in the folder you specified.");
        }

        public async Task UnpublishAsync(string idOrName = "", ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = await LoadAsync(idOrName, "unpublish", true, providerType);

            if (result != null && !result.IsError && result.Result != null)
            {
                OASISResult<T1> unpublishResult = await STARManager.UnpublishAsync(STAR.BeamedInAvatar.Id, result.Result, providerType);

                if (unpublishResult != null && !unpublishResult.IsError && unpublishResult.Result != null)
                {
                    CLIEngine.ShowSuccessMessage($"{STARManager.STARHolonUIName} Successfully Unpublished.");
                    Show(result.Result);
                }
                else
                    CLIEngine.ShowErrorMessage($"An error occured unpublishing the {STARManager.STARHolonUIName}. Reason: {unpublishResult.Message}");
            }
        }

        public async Task RepublishAsync(string idOrName = "", ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = await LoadAsync(idOrName, "republish", true, providerType);

            if (result != null && !result.IsError && result.Result != null)
            {
                OASISResult<T1> republishResult = await STARManager.RepublishAsync(STAR.BeamedInAvatar.Id, result.Result, providerType);

                if (republishResult != null && !republishResult.IsError && republishResult.Result != null)
                {
                    CLIEngine.ShowSuccessMessage($"{STARManager.STARHolonUIName} Successfully Republished.");
                    Show(result.Result);
                }
                else
                    CLIEngine.ShowErrorMessage($"An error occured unpublishing the {STARManager.STARHolonUIName}. Reason: {republishResult.Message}");
            }
        }

        public async Task ActivateAsync(string idOrName = "", ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = await LoadAsync(idOrName, "activate", true, providerType);

            if (result != null && !result.IsError && result.Result != null)
            {
                if (result.MetaData != null && result.MetaData.ContainsKey("Active") && result.MetaData["Active"] != null && result.MetaData["Active"] == "1")
                {
                    OASISResult<T1> activateResult = await STARManager.ActivateAsync(STAR.BeamedInAvatar.Id, result.Result, providerType);

                    if (activateResult != null && !activateResult.IsError && activateResult.Result != null)
                    {
                        CLIEngine.ShowSuccessMessage($"{STARManager.STARHolonUIName} Successfully Activated.");
                        Show(result.Result);
                    }
                    else
                        CLIEngine.ShowErrorMessage($"An error occured activating the {STARManager.STARHolonUIName}. Reason: {activateResult.Message}");
                }
                else
                    CLIEngine.ShowErrorMessage($"The {STARManager.STARHolonUIName} is already activated!");
            }   
        }

        public async Task DeactivateAsync(string idOrName = "", ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = await LoadAsync(idOrName, "deactivate", true, providerType);

            if (result != null && !result.IsError && result.Result != null)
            {
                if (result.MetaData != null && result.MetaData.ContainsKey("Active") && result.MetaData["Active"] != null && result.MetaData["Active"] == "0")
                {
                    OASISResult<T1> deactivateResult = await STARManager.DeactivateAsync(STAR.BeamedInAvatar.Id, result.Result, providerType);

                    if (deactivateResult != null && !deactivateResult.IsError && deactivateResult.Result != null)
                    {
                        CLIEngine.ShowSuccessMessage($"{STARManager.STARHolonUIName} Successfully Deactivated.");
                        Show(result.Result);
                    }
                    else
                        CLIEngine.ShowErrorMessage($"An error occured deactivating the {STARManager.STARHolonUIName}. Reason: {deactivateResult.Message}");
                }
                else
                    CLIEngine.ShowErrorMessage($"The {STARManager.STARHolonUIName} is already deactivated!");
            }
        }

        public async Task<OASISResult<T3>> DownloadAndInstallAsync(string idOrName = "", InstallMode installMode = InstallMode.DownloadAndInstall, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T3> installResult = new OASISResult<T3>();
            string downloadPath = "";
            string installPath = "";
            bool simpleWizard = false;
            string operation = "install";

            if (installMode == InstallMode.DownloadOnly)
                operation = "download";

            if (Path.IsPathRooted(DownloadedPath) || string.IsNullOrEmpty(STAR.STARDNA.BasePath))
                downloadPath = SourcePath;
            else
                downloadPath = Path.Combine(STAR.STARDNA.BasePath, DownloadedPath);


            if (Path.IsPathRooted(InstalledPath) || string.IsNullOrEmpty(STAR.STARDNA.BasePath))
                installPath = SourcePath;
            else
                installPath = Path.Combine(STAR.STARDNA.BasePath, InstalledPath);

            Console.WriteLine("");

            if (CLIEngine.GetConfirmation("Do you wish to launch the Simple or Advanced Wizard? The Simple Wizard will use defaults (recommended) but the Advanced Wizard will allow greater control and customisation. Press 'Y' for Simple or 'N' for Advanced."))
                simpleWizard = true;

            if (!simpleWizard)
            {
                Console.WriteLine("");

                if (!CLIEngine.GetConfirmation($"Do you wish to download the {STARManager.STARHolonUIName} to the default download folder defined in the STARDNA as {DownloadSTARDNAKey} : {downloadPath}?"))
                {
                    Console.WriteLine("");
                    downloadPath = CLIEngine.GetValidFolder($"What is the full path to where you wish to download the {STARManager.STARHolonUIName}?", true);
                }

                downloadPath = new DirectoryInfo(downloadPath).FullName;

                if (installMode != InstallMode.DownloadAndInstall)
                {
                    Console.WriteLine("");

                    if (!CLIEngine.GetConfirmation($"Do you wish to install the {STARManager.STARHolonUIName} to the default install folder defined in the STARDNA as {InstalledSTARDNAKey} : {installPath}?"))
                    {
                        Console.WriteLine("");
                        installPath = CLIEngine.GetValidFolder("What is the full path to where you wish to install the {STARManager.STARHolonUIName}?", true);
                    }

                    installPath = new DirectoryInfo(installPath).FullName;
                }
            }

            if (!string.IsNullOrEmpty(idOrName))
            {
                Console.WriteLine("");
                OASISResult<T1> result = await LoadForProviderAsync(idOrName, operation, false, providerType, false);

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
                if (installMode != InstallMode.DownloadOnly && CLIEngine.GetConfirmation($"Do you wish to install the {STARManager.STARHolonUIName} from a local .{STARManager.STARHolonDNAFileName} file or from STARNET? Press 'Y' for local .{STARManager.STARHolonDNAFileName} file or 'N' for STARNET."))
                {
                    Console.WriteLine("");
                    string oappPath = CLIEngine.GetValidFile($"What is the full path to the .{STARManager.STARHolonDNAFileName} file?");

                    if (oappPath == "exit")
                        return installResult;

                    OASISResult<ISTARHolonDNA> starHolonDNAResult = await STARManager.ReadDNAFromPublishedFileAsync<ISTARHolonDNA>(oappPath);

                    if (starHolonDNAResult != null && starHolonDNAResult.Result != null && !starHolonDNAResult.IsError)
                    {
                        OASISResult<T1> starHolonResult = await STARManager.LoadAsync(STAR.BeamedInAvatar.Id, starHolonDNAResult.Result.Id, 0, providerType);

                        if (starHolonResult != null && starHolonResult.Result != null && !starHolonResult.IsError)
                        {
                            installMode = InstallMode.InstallOnly;

                            if (starHolonResult.MetaData != null && starHolonResult.MetaData.ContainsKey("Reinstall") && !string.IsNullOrEmpty(starHolonResult.MetaData["Reinstall"]) && starHolonResult.MetaData["Reinstall"] == "1")
                                installMode = InstallMode.ReInstall;

                            installResult = await CheckIfInstalledAndInstallAsync(starHolonResult.Result, downloadPath, installPath, installMode, oappPath, providerType);
                        }
                        else
                            CLIEngine.ShowErrorMessage($"The  could not be found for id {starHolonDNAResult.Result.Id} found in the STARHolonDNA.json file. It could be corrupt or the id could be wrong, please check and try again, or create a new {STARManager.STARHolonUIName}.");
                    }
                    else
                        CLIEngine.ShowErrorMessage($"The {STARManager.STARHolonUIName} could not be found or is not valid! Please ensure it is in the folder you specified.");
                }
                else
                {
                    Console.WriteLine("");
                    OASISResult<T1> result = await LoadForProviderAsync("", operation, false, providerType, false);

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

                    if (CLIEngine.GetConfirmation($"Do you wish to open the folder to the {STARManager.STARHolonUIName} now?"))
                        STARManager.OpenSTARHolonFolder(STAR.BeamedInAvatar.Id, installResult.Result);
                        //await STARManager.OpenSTARHolonFolderAsync(STAR.BeamedInAvatar.Id, installResult.Result.STARHolonDNA.Id, installResult.Result.STARHolonDNA.Version);
                }
                else
                    CLIEngine.ShowErrorMessage($"Error {operation}ing {STARManager.STARHolonUIName}. Reason: {installResult.Message}");
            }
            else
                CLIEngine.ShowErrorMessage($"Error {operation}ing {STARManager.STARHolonUIName}. Reason: Unknown error occured!");

            Console.WriteLine("");
            return installResult;
        }

        public OASISResult<T3> DownloadAndInstall(string idOrName = "", ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T3> installResult = new OASISResult<T3>();
            string downloadPath = "";
            string installPath = "";

            if (Path.IsPathRooted(DownloadedPath) || string.IsNullOrEmpty(STAR.STARDNA.BasePath))
                downloadPath = SourcePath;
            else
                downloadPath = Path.Combine(STAR.STARDNA.BasePath, DownloadedPath);


            if (Path.IsPathRooted(InstalledPath) || string.IsNullOrEmpty(STAR.STARDNA.BasePath))
                installPath = SourcePath;
            else
                installPath = Path.Combine(STAR.STARDNA.BasePath, InstalledPath);

            Console.WriteLine("");

            if (!CLIEngine.GetConfirmation($"Do you wish to download the {STARManager.STARHolonUIName} to the default download folder defined in the STARDNA as {DownloadSTARDNAKey} : {downloadPath}?"))
            {
                Console.WriteLine("");
                downloadPath = CLIEngine.GetValidFolder($"What is the full path to where you wish to download the {STARManager.STARHolonUIName}?", true);
            }

            downloadPath = new DirectoryInfo(downloadPath).FullName;

            Console.WriteLine("");

            if (!CLIEngine.GetConfirmation($"Do you wish to install the {STARManager.STARHolonUIName} to the default install folder defined in the STARDNA as {DownloadSTARDNAKey} : {installPath}?"))
            {
                Console.WriteLine("");
                installPath = CLIEngine.GetValidFolder($"What is the full path to where you wish to install the {STARManager.STARHolonUIName}?", true);
            }

            installPath = new DirectoryInfo(installPath).FullName;

            if (!string.IsNullOrEmpty(idOrName))
            {
                Console.WriteLine("");
                OASISResult<T1> result = LoadForProvider(idOrName, "install", false, providerType, false);

                if (result != null && result.Result != null && !result.IsError)
                    installResult = STARManager.DownloadAndInstall(STAR.BeamedInAvatar.Id, result.Result, installPath, downloadPath, true, false, providerType);
            }
            else
            {
                Console.WriteLine("");
                if (CLIEngine.GetConfirmation($"Do you wish to install the {STARManager.STARHolonUIName} from a local .{STARManager.STARHolonDNAFileName} file or from STARNET? Press 'Y' for local .{STARManager.STARHolonDNAFileName} file or 'N' for STARNET."))
                {
                    Console.WriteLine("");
                    string oappPath = CLIEngine.GetValidFile($"What is the full path to the {STARManager.STARHolonDNAFileName} file?");

                    if (oappPath == "exit")
                        return installResult;

                    installResult = STARManager.Install(STAR.BeamedInAvatar.Id, oappPath, installPath, true, null, false, providerType);
                }
                else
                {
                    Console.WriteLine("");
                    CLIEngine.ShowWorkingMessage($"Loading {STARManager.STARHolonUIName}s...");
                    OASISResult<IEnumerable<T1>> starHolonsResult = ListAll();

                    if (starHolonsResult != null && starHolonsResult.Result != null && !starHolonsResult.IsError && starHolonsResult.Result.Count() > 0)
                    {
                        OASISResult<T1> result = LoadForProvider("", "install", false, providerType, false);

                        if (result != null && result.Result != null && !result.IsError)
                            installResult = STARManager.DownloadAndInstall(STAR.BeamedInAvatar.Id, result.Result, installPath, downloadPath, true, false, providerType);
                        else
                        {
                            installResult.Message = result.Message;
                            installResult.IsError = true;
                        }
                    }
                    else
                    {
                        installResult.Message = $"No {STARManager.STARHolonUIName}s found to install.";
                        installResult.IsError = true;
                    }
                }
            }

            if (installResult != null)
            {
                if (!installResult.IsError && installResult.Result != null)
                {
                    ShowInstalled(installResult.Result);

                    if (CLIEngine.GetConfirmation($"Do you wish to open the folder to the {STARManager.STARHolonUIName} now?"))
                        STARManager.OpenSTARHolonFolder(STAR.BeamedInAvatar.Id, installResult.Result);
                }
                else
                    CLIEngine.ShowErrorMessage($"Error installing {STARManager.STARHolonUIName}. Reason: {installResult.Message}");
            }
            else
                CLIEngine.ShowErrorMessage($"Error installing {STARManager.STARHolonUIName}. Reason: Unknown error occured!");

            Console.WriteLine("");
            return installResult;
        }

        public async Task UninstallAsync(string idOrName = "", ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = await LoadAsync(idOrName, "uninstall", true, providerType);

            if (result != null && !result.IsError && result.Result != null)
            {
                OASISResult<T3> uninstallResult = await STARManager.UninstallAsync(STAR.BeamedInAvatar.Id, result.Result.Id, result.Result.STARHolonDNA.Version, providerType);

                if (uninstallResult != null)
                {
                    if (!uninstallResult.IsError && uninstallResult.Result != null)
                    {
                        CLIEngine.ShowSuccessMessage($"{STARManager.STARHolonUIName} Successfully Uninstalled.");
                        Show(result.Result);
                    }
                    else
                        CLIEngine.ShowErrorMessage($"Error installing {STARManager.STARHolonUIName}. Reason: {uninstallResult.Message}");
                }
                else
                    CLIEngine.ShowErrorMessage($"Error uninstalling {STARManager.STARHolonUIName}. Reason: Unknown error occured!");
            }
            else
                CLIEngine.ShowErrorMessage($"An error occured loading the {STARManager.STARHolonUIName}. Reason: {result.Message}");
        }

        public void Uninstall(string idOrName = "", ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = Load(idOrName, "uninstall", true, providerType);

            if (result != null && !result.IsError && result.Result != null)
            {
                OASISResult<T3> uninstallResult = STARManager.Uninstall(STAR.BeamedInAvatar.Id, result.Result.Id, result.Result.STARHolonDNA.Version, providerType);

                if (uninstallResult != null)
                {
                    if (!uninstallResult.IsError && uninstallResult.Result != null)
                    {
                        CLIEngine.ShowSuccessMessage($"{STARManager.STARHolonUIName} Successfully Uninstalled.");
                        Show(result.Result);
                    }
                    else
                        CLIEngine.ShowErrorMessage($"Error installing {STARManager.STARHolonUIName}. Reason: {uninstallResult.Message}");
                }
                else
                    CLIEngine.ShowErrorMessage($"Error uninstalling {STARManager.STARHolonUIName}. Reason: Unknown error occured!");
            }
            else
                CLIEngine.ShowErrorMessage($"An error occured loading the {STARManager.STARHolonUIName}. Reason: {result.Message}");
        }

        public async Task<OASISResult<IEnumerable<T1>>> ListAllsAsync(bool showAllVersions = false, ProviderType providerType = ProviderType.Default)
        {
            Console.WriteLine("");
            CLIEngine.ShowWorkingMessage($"Loading {STARManager.STARHolonUIName}'s...");
            return ListStarHolons(await STARManager.LoadAllAsync(STAR.BeamedInAvatar.Id, null, true, showAllVersions, 0, providerType: providerType));
        }

        public OASISResult<IEnumerable<T1>> ListAll(bool showAllVersions = false, int version = 0, ProviderType providerType = ProviderType.Default)
        {
            Console.WriteLine("");
            CLIEngine.ShowWorkingMessage($"Loading {STARManager.STARHolonUIName}'s...");
            return  ListStarHolons(STARManager.LoadAll(STAR.BeamedInAvatar.Id, null, true, showAllVersions, version, providerType: providerType));
        }

        public async Task ListAllCreatedByBeamedInAvatarAsync(bool showAllVersions = false, ProviderType providerType = ProviderType.Default)
        {
            if (STAR.BeamedInAvatar != null)
            {
                Console.WriteLine("");
                CLIEngine.ShowWorkingMessage($"Loading {STARManager.STARHolonUIName}'s...");
                ListStarHolons(await STARManager.LoadAllForAvatarAsync(STAR.BeamedInAvatar.AvatarId));
            }
            else
                CLIEngine.ShowErrorMessage("No Avatar Is Beamed In. Please Beam In First!");
        }

        public async Task<OASISResult<IEnumerable<T3>>> ListAllInstalledForBeamedInAvatarAsync(ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<T3>> result = new OASISResult<IEnumerable<T3>>();

            if (STAR.BeamedInAvatar != null)
            {
                Console.WriteLine("");
                CLIEngine.ShowWorkingMessage($"Loading Installed {STARManager.STARHolonUIName}'s...");
                result = await STARManager.ListInstalledAsync(STAR.BeamedInAvatar.AvatarId);
                ListStarHolonsInstalled(result);
            }
            else
                OASISErrorHandling.HandleError(ref result, "No Avatar Is Beamed In. Please Beam In First!");

            return result;
        }

        public async Task<OASISResult<IEnumerable<T3>>> ListAllUninstalledForBeamedInAvatarAsync(ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<T3>> result = new OASISResult<IEnumerable<T3>>();

            if (STAR.BeamedInAvatar != null)
            {
                Console.WriteLine("");
                CLIEngine.ShowWorkingMessage($"Loading Uninstalled {STARManager.STARHolonUIName}s...");
                result = await STARManager.ListUninstalledAsync(STAR.BeamedInAvatar.AvatarId);
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
                            OASISResult<T3> installResult = await DownloadAndInstallAsync(template.STARHolonDNA.Id.ToString(), InstallMode.DownloadAndReInstall, providerType);

                            if (installResult != null && !installResult.IsError && installResult.Result != null)
                            {
                                ShowInstalled(installResult.Result);
                                CLIEngine.ShowSuccessMessage($"{STARManager.STARHolonUIName} Successfully Re-Installed");
                            }
                            else
                                CLIEngine.ShowErrorMessage($"An error occured re-installing the {STARManager.STARHolonUIName}. Reason: {installResult.Message}");
                        }
                        else
                            CLIEngine.ShowErrorMessage($"An error occured re-installing the {STARManager.STARHolonUIName}. Reason: {STARManager.STARHolonIdName} not found in the metadata!");
                    }
                }
                else
                    Console.WriteLine("");
            }
            else
                OASISErrorHandling.HandleError(ref result, "No Avatar Is Beamed In. Please Beam In First!");

            return result;
        }

        public async Task<OASISResult<IEnumerable<T1>>> ListAllUnpublishedForBeamedInAvatarAsync(ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<T1>> result = new OASISResult<IEnumerable<T1>>();

            if (STAR.BeamedInAvatar != null)
            {
                Console.WriteLine("");
                CLIEngine.ShowWorkingMessage($"Loading Unpublished {STARManager.STARHolonUIName}'s...");
                result = await STARManager.ListUnpublishedAsync(STAR.BeamedInAvatar.AvatarId);
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
                            OASISResult<T1> republishResult = await STARManager.RepublishAsync(STAR.BeamedInAvatar.Id, template, providerType);

                            if (republishResult != null && !republishResult.IsError && republishResult.Result != null)
                            {
                                Show(republishResult.Result);
                                CLIEngine.ShowSuccessMessage($"{STARManager.STARHolonUIName} Successfully Republished");
                            }
                            else
                                CLIEngine.ShowErrorMessage($"An error occured republishing the {STARManager.STARHolonUIName}. Reason: {republishResult.Message}");
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

        public async Task<OASISResult<IEnumerable<T1>>> ListAllDeactivatedForBeamedInAvatarAsync(ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<T1>> result = new OASISResult<IEnumerable<T1>>();

            if (STAR.BeamedInAvatar != null)
            {
                Console.WriteLine("");
                CLIEngine.ShowWorkingMessage($"Loading Deactivated {STARManager.STARHolonUIName}'s...");
                result = await STARManager.ListDeactivatedAsync(STAR.BeamedInAvatar.AvatarId);
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
                            OASISResult<T1> activateResult = await STARManager.ActivateAsync(STAR.BeamedInAvatar.Id, template, providerType);

                            if (activateResult != null && !activateResult.IsError && activateResult.Result != null)
                            {
                                Show(activateResult.Result);
                                CLIEngine.ShowSuccessMessage($"{STARManager.STARHolonUIName} Successfully Reactivated");
                            }
                            else
                                CLIEngine.ShowErrorMessage($"An error occured reactivating the {STARManager.STARHolonUIName}. Reason: {activateResult.Message}");
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

        public async Task SearchsAsync(string searchTerm = "", bool showAllVersions = false, bool showForAllAvatars = true, ProviderType providerType = ProviderType.Default)
        {            
            if (string.IsNullOrEmpty(searchTerm) || searchTerm == "forallavatars" || searchTerm == "forallavatars")
            { 
                //Console.WriteLine("");
                searchTerm = CLIEngine.GetValidInput($"What is the name of the {STARManager.STARHolonUIName} you wish to search for?");
            }

            Console.WriteLine("");
            CLIEngine.ShowWorkingMessage($"Searching {STARManager.STARHolonUIName}'s...");
            ListStarHolons(await STARManager.SearchAsync(STAR.BeamedInAvatar.Id, searchTerm, !showForAllAvatars, showAllVersions, 0, providerType));
        }

        public async Task ShowAsync(string idOrName = "", ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = await LoadAsync(idOrName, "view", true, providerType);

            if (result != null && !result.IsError && result.Result != null)
                Show(result.Result);
            else
                CLIEngine.ShowErrorMessage($"An error occured loading the {STARManager.STARHolonUIName}. Reason: {result.Message}");
        }

        public void Show(T1 starHolon, bool showHeader = true, bool showFooter = true, bool showNumbers = false, int number = 0, bool showDetailedInfo = false)
        {
            if (showHeader)
                CLIEngine.ShowDivider();

            if (showNumbers)
                CLIEngine.ShowMessage(string.Concat("Number:                                     ", number));

            CLIEngine.ShowMessage(string.Concat($"Id:                                         ", starHolon.STARHolonDNA.Id != Guid.Empty ? starHolon.STARHolonDNA.Id : "None"));
            CLIEngine.ShowMessage(string.Concat($"Name:                                       ", !string.IsNullOrEmpty(starHolon.STARHolonDNA.Name) ? starHolon.STARHolonDNA.Name : "None"));
            CLIEngine.ShowMessage(string.Concat($"Description:                                ", !string.IsNullOrEmpty(starHolon.STARHolonDNA.Description) ? starHolon.STARHolonDNA.Description : "None"));
            CLIEngine.ShowMessage(string.Concat($"{STARManager.STARHolonUIName} Type:                         ", starHolon.STARHolonDNA.STARHolonType));
            CLIEngine.ShowMessage(string.Concat($"Created On:                                 ", starHolon.STARHolonDNA.CreatedOn != DateTime.MinValue ? starHolon.STARHolonDNA.CreatedOn.ToString() : "None"));
            CLIEngine.ShowMessage(string.Concat($"Created By:                                 ", starHolon.STARHolonDNA.CreatedByAvatarId != Guid.Empty ? string.Concat(starHolon.STARHolonDNA.CreatedByAvatarUsername, " (", starHolon.STARHolonDNA.CreatedByAvatarId.ToString(), ")") : "None"));
            CLIEngine.ShowMessage(string.Concat($"Modified On:                                ", starHolon.STARHolonDNA.ModifiedOn != DateTime.MinValue ? starHolon.STARHolonDNA.CreatedOn.ToString() : "None"));
            CLIEngine.ShowMessage(string.Concat($"Modified By:                                ", starHolon.STARHolonDNA.ModifiedByAvatarId != Guid.Empty ? string.Concat(starHolon.STARHolonDNA.ModifiedByAvatarUsername, " (", starHolon.STARHolonDNA.ModifiedByAvatarId.ToString(), ")") : "None"));
            CLIEngine.ShowMessage(string.Concat($"{STARManager.STARHolonUIName} Path:                         ", !string.IsNullOrEmpty(starHolon.STARHolonDNA.SourcePath) ? starHolon.STARHolonDNA.SourcePath : "None"));
            CLIEngine.ShowMessage(string.Concat($"Published On:                               ", starHolon.STARHolonDNA.PublishedOn != DateTime.MinValue ? starHolon.STARHolonDNA.PublishedOn.ToString() : "None"));
            CLIEngine.ShowMessage(string.Concat($"Published By:                               ", starHolon.STARHolonDNA.PublishedByAvatarId != Guid.Empty ? string.Concat(starHolon.STARHolonDNA.PublishedByAvatarUsername, " (", starHolon.STARHolonDNA.PublishedByAvatarId.ToString(), ")") : "None"));
            CLIEngine.ShowMessage(string.Concat($"{STARManager.STARHolonUIName} Published Path:               ", !string.IsNullOrEmpty(starHolon.STARHolonDNA.PublishedPath) ? starHolon.STARHolonDNA.PublishedPath : "None"));
            CLIEngine.ShowMessage(string.Concat($"{STARManager.STARHolonUIName} Filesize:                     ", starHolon.STARHolonDNA.FileSize.ToString()));
            CLIEngine.ShowMessage(string.Concat($"{STARManager.STARHolonUIName} Published On STARNET:         ", starHolon.STARHolonDNA.PublishedOnSTARNET ? "True" : "False"));
            CLIEngine.ShowMessage(string.Concat($"{STARManager.STARHolonUIName} Published To Cloud:           ", starHolon.STARHolonDNA.PublishedToCloud ? "True" : "False"));
            CLIEngine.ShowMessage(string.Concat($"{STARManager.STARHolonUIName} Published To OASIS Provider:  ", Enum.GetName(typeof(ProviderType), starHolon.STARHolonDNA.PublishedProviderType)));
            CLIEngine.ShowMessage(string.Concat($"Launch Target:                              ", starHolon.STARHolonDNA.LaunchTarget));
            CLIEngine.ShowMessage(string.Concat($"{STARManager.STARHolonUIName} Version:                      ", starHolon.STARHolonDNA.Version));
            CLIEngine.ShowMessage(string.Concat($"Version Sequence:                           ", starHolon.STARHolonDNA.VersionSequence));
            CLIEngine.ShowMessage(string.Concat($"Number Of Versions:                         ", starHolon.STARHolonDNA.NumberOfVersions));
            //CLIEngine.ShowMessage(string.Concat($"OASIS Holon Version:                        ", starHolon.Version));
            //CLIEngine.ShowMessage(string.Concat($"OASIS Holon VersionId:                      ", starHolon.VersionId));
            //CLIEngine.ShowMessage(string.Concat($"OASIS Holon PreviousVersionId:              ", starHolon.PreviousVersionId));
            CLIEngine.ShowMessage(string.Concat($"Downloads:                                  ", starHolon.STARHolonDNA.Downloads));
            CLIEngine.ShowMessage(string.Concat($"Installs:                                   ", starHolon.STARHolonDNA.Installs));
            CLIEngine.ShowMessage(string.Concat($"Total Downloads:                            ", starHolon.STARHolonDNA.TotalDownloads));
            CLIEngine.ShowMessage(string.Concat($"Total Installs:                             ", starHolon.STARHolonDNA.TotalInstalls));
            CLIEngine.ShowMessage(string.Concat($"Active:                                     ", starHolon.MetaData != null && starHolon.MetaData.ContainsKey("Active") && starHolon.MetaData["Active"] != null && starHolon.MetaData["Active"].ToString() == "1" ? "True" : "False"));

            if (showDetailedInfo)
            {
                //Show base holon info.
            }

            if (showFooter)
                CLIEngine.ShowDivider();
        }

        public void ShowInstalled(T3 starHolon, bool showHeader = true, bool showFooter = true, bool showNumbers = false, int number = 0, bool showUninstallInfo = false, bool showDetailedInfo = false)
        {
            //Show((T1)starHolon, showHeader, false, showNumbers, number, showDetailedInfo);
            Show(ConvertFromT3ToT1(starHolon), showHeader, false, showNumbers, number, showDetailedInfo);
            CLIEngine.ShowMessage(string.Concat($"Downloaded On:                              ", starHolon.DownloadedOn != DateTime.MinValue ? starHolon.DownloadedOn.ToString() : "None"));
            CLIEngine.ShowMessage(string.Concat($"Downloaded By:                              ", starHolon.DownloadedBy != Guid.Empty ? string.Concat(starHolon.DownloadedByAvatarUsername, " (", starHolon.DownloadedBy.ToString(), ")") : "None"));
            CLIEngine.ShowMessage(string.Concat($"Downloaded Path:                            ", !string.IsNullOrEmpty(starHolon.DownloadedPath) ? starHolon.DownloadedPath : "None"));
            CLIEngine.ShowMessage(string.Concat($"Installed On:                               ", starHolon.InstalledOn != DateTime.MinValue ? starHolon.InstalledOn.ToString() : "None"));
            CLIEngine.ShowMessage(string.Concat($"Installed By:                               ", starHolon.InstalledBy != Guid.Empty ? string.Concat(starHolon.InstalledByAvatarUsername, " (", starHolon.InstalledBy.ToString(), ")") : "None"));
            CLIEngine.ShowMessage(string.Concat($"Installed Path:                             ", !string.IsNullOrEmpty(starHolon.InstalledPath) ? starHolon.InstalledPath : "None"));

            if (showUninstallInfo)
            {
                CLIEngine.ShowMessage(string.Concat($"Uninstalled On:                             ", starHolon.UninstalledOn != DateTime.MinValue ? starHolon.UninstalledOn.ToString() : "None"));
                CLIEngine.ShowMessage(string.Concat($"Uninstalled By:                             ", starHolon.UninstalledBy != Guid.Empty ? string.Concat(starHolon.UninstalledByAvatarUsername, " (", starHolon.UninstalledBy.ToString(), ")") : "None"));
            }

            if (showFooter)
                CLIEngine.ShowDivider();
        }

        private OASISResult<IEnumerable<T1>> ListStarHolons(OASISResult<IEnumerable<T1>> starHolons, bool showNumbers = false)
        {
            if (starHolons != null)
            {
                if (!starHolons.IsError)
                {
                    if (starHolons.Result != null && starHolons.Result.Count() > 0)
                    {
                        Console.WriteLine();

                        if (starHolons.Result.Count() == 1)
                            CLIEngine.ShowMessage($"{starHolons.Result.Count()} {STARManager.STARHolonUIName} Found:");
                        else
                            CLIEngine.ShowMessage($"{starHolons.Result.Count()} {STARManager.STARHolonUIName}s Found:");

                        for (int i = 0; i < starHolons.Result.Count(); i++)
                            Show(starHolons.Result.ElementAt(i), i==0, true, showNumbers, i + 1);
                    }
                    else
                        CLIEngine.ShowWarningMessage("No {STARManager.STARHolonUIName}s Found.");
                }
                else
                    CLIEngine.ShowErrorMessage($"Error occured loading {STARManager.STARHolonUIName}s. Reason: {starHolons.Message}");
            }
            else
                CLIEngine.ShowErrorMessage($"Unknown error occured loading {STARManager.STARHolonUIName}s.");

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
                            CLIEngine.ShowMessage($"{starHolons.Result.Count()} {STARManager.STARHolonUIName} Found:");
                        else
                            CLIEngine.ShowMessage($"{starHolons.Result.Count()} {STARManager.STARHolonUIName}s Found:");

                        for (int i = 0; i < starHolons.Result.Count(); i++)
                            ShowInstalled(starHolons.Result.ElementAt(i), i == 0, true, showNumbers, i + 1, showUninstallInfo);
                    }
                    else
                        CLIEngine.ShowWarningMessage($"No {STARManager.STARHolonUIName}s Found.");
                }
                else
                    CLIEngine.ShowErrorMessage($"Error occured loading {STARManager.STARHolonUIName}'s. Reason: {starHolons.Message}");
            }
            else
                CLIEngine.ShowErrorMessage($"Unknown error occured loading {STARManager.STARHolonUIName}'s.");
        }

        private async Task<OASISResult<T1>> LoadForProviderAsync(string idOrName, string operationName, bool showOnlyForCurrentAvatar, ProviderType providerType, bool addSpace = true, bool simpleWizard = true)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            ProviderType largeFileProviderType = ProviderType.IPFSOASIS;

            if (!simpleWizard)
            {
                if (!CLIEngine.GetConfirmation("Do you wish to download from the cloud or from the OASIS? Press 'Y' for the cloud or N' for the OASIS."))
                {
                    Console.WriteLine("");
                    object largeProviderTypeObject = CLIEngine.GetValidInputForEnum($"What OASIS provider do you wish to install the {STARManager.STARHolonUIName} from? (The default is IPFSOASIS)", typeof(ProviderType));

                    if (largeProviderTypeObject != null)
                    {
                        largeFileProviderType = (ProviderType)largeProviderTypeObject;
                        result = await LoadAsync(idOrName, operationName, showOnlyForCurrentAvatar, largeFileProviderType, addSpace);
                    }
                    else
                        OASISErrorHandling.HandleError(ref result, "Error occured in LoadForProviderAsync, reason: largeProviderTypeObject is null!");
                }
                else
                {
                    Console.WriteLine("");
                    result = await LoadAsync(idOrName, operationName, showOnlyForCurrentAvatar, providerType, addSpace);
                }
            }
            else
                result = await LoadAsync(idOrName, operationName, showOnlyForCurrentAvatar, providerType, addSpace);

            return result;
        }

        private OASISResult<T1> LoadForProvider(string idOrName, string operationName, bool showOnlyForCurrentAvatar, ProviderType providerType, bool addSpace = true, bool simpleWizard = true)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            ProviderType largeFileProviderType = ProviderType.IPFSOASIS;

            if (!simpleWizard)
            {
                if (!CLIEngine.GetConfirmation("Do you wish to download from the cloud or from the OASIS? Press 'Y' for the cloud or N' for the OASIS."))
                {
                    Console.WriteLine("");
                    object largeProviderTypeObject = CLIEngine.GetValidInputForEnum($"What OASIS provider do you wish to install the {STARManager.STARHolonUIName} from? (The default is IPFSOASIS)", typeof(ProviderType));

                    if (largeProviderTypeObject != null)
                    {
                        largeFileProviderType = (ProviderType)largeProviderTypeObject;
                        result = Load(idOrName, operationName, showOnlyForCurrentAvatar, largeFileProviderType, addSpace);
                    }
                    else
                        OASISErrorHandling.HandleError(ref result, "Error occured in LoadForProvider, reason: largeProviderTypeObject is null!");
                }
                else
                {
                    Console.WriteLine("");
                    result = Load(idOrName, operationName, showOnlyForCurrentAvatar, providerType, addSpace);
                }
            }
            else
                result = Load(idOrName, operationName, showOnlyForCurrentAvatar, providerType, addSpace);

            return result;
        }

        private async Task<OASISResult<T1>> LoadAsync(string idOrName, string operationName, bool showOnlyForCurrentAvatar, ProviderType providerType = ProviderType.Default, bool addSpace = true)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            Guid id = Guid.Empty;

            do
            {
                if (string.IsNullOrEmpty(idOrName))
                {
                    if (!CLIEngine.GetConfirmation($"Do you know the GUID/ID or Name of the {STARManager.STARHolonUIName} you wish to {operationName}? Press 'Y' for Yes or 'N' for No."))
                    {
                        Console.WriteLine("");
                        CLIEngine.ShowWorkingMessage($"Loading {STARManager.STARHolonUIName}'s...");
                        
                        if (showOnlyForCurrentAvatar)
                            ListStarHolons(await STARManager.LoadAllForAvatarAsync(STAR.BeamedInAvatar.AvatarId));
                        else
                            ListStarHolons(await STARManager.LoadAllAsync(STAR.BeamedInAvatar.AvatarId, null, true, false, 0, providerType: providerType));
                    }
                    else
                        Console.WriteLine("");

                    idOrName = CLIEngine.GetValidInput($"What is the GUID/ID or Name of the {STARManager.STARHolonUIName} you wish to {operationName}?");

                    if (idOrName == "exit")
                        break;
                }

                if (addSpace)
                    Console.WriteLine("");

                if (Guid.TryParse(idOrName, out id))
                {
                    CLIEngine.ShowWorkingMessage($"Loading {STARManager.STARHolonUIName}...");
                    result = await STARManager.LoadAsync(STAR.BeamedInAvatar.AvatarId, id, 0, providerType);

                    if (result != null && result.Result != null && !result.IsError && showOnlyForCurrentAvatar && result.Result.STARHolonDNA.CreatedByAvatarId != STAR.BeamedInAvatar.AvatarId)
                    {
                        CLIEngine.ShowErrorMessage($"You do not have permission to {operationName} this {STARManager.STARHolonUIName}. It was created by another avatar.");
                        result.Result = default;
                    }
                }
                else
                {
                    CLIEngine.ShowWorkingMessage($"Searching {STARManager.STARHolonUIName}s...");
                    OASISResult<IEnumerable<T1>> searchResults = await STARManager.SearchAsync(STAR.BeamedInAvatar.Id, idOrName, showOnlyForCurrentAvatar, false, 0, providerType);

                    if (searchResults != null && searchResults.Result != null && !searchResults.IsError)
                    {
                        if (searchResults.Result.Count() > 1)
                        {
                            ListStarHolons(searchResults, true);

                            do
                            {
                                int number = CLIEngine.GetValidInputForInt($"What is the number of the {STARManager.STARHolonUIName} you wish to {operationName}?");

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
                            CLIEngine.ShowWarningMessage($"No {STARManager.STARHolonUIName} Found!");
                        }
                    }
                    else
                        CLIEngine.ShowErrorMessage($"An error occured calling STARManager.SearchsAsync. Reason: {searchResults.Message}");
                }

                if (result.Result != null && result.Result.STARHolonDNA != null)
                {
                    Show(result.Result);

                    if (result.Result.STARHolonDNA.NumberOfVersions > 1)
                    {
                        if ((operationName == "view" && CLIEngine.GetConfirmation($"{result.Result.STARHolonDNA.NumberOfVersions} versions were found. Do you wish to view the other versions?")) ||
                            (!CLIEngine.GetConfirmation($"{result.Result.STARHolonDNA.NumberOfVersions} versions were found. Do you wish to {operationName} the latest version ({result.Result.STARHolonDNA.Version})?")))
                        {
                            Console.WriteLine("");
                            CLIEngine.ShowWorkingMessage($"Loading {STARManager.STARHolonUIName} Versions...");
                            OASISResult<IEnumerable<T1>> versionsResult = await STARManager.LoadVersionsAsync(result.Result.STARHolonDNA.Id, providerType);
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
                    if (CLIEngine.GetConfirmation($"Please confirm you wish to {operationName} this {STARManager.STARHolonUIName}?"))
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
                                else
                                    result.Result = default;
                            }
                            else
                            {
                                CLIEngine.ShowErrorMessage($"Error occured checking if the {STARManager.STARHolonUIName} is already installed! Reason: Id was not found in the metadata!");
                                result.Result = default;
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine("");
                        result.Result = default;

                        if (!CLIEngine.GetConfirmation($"Do you wish to search for another {STARManager.STARHolonUIName}?"))
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

        private OASISResult<T1> Load(string idOrName, string operationName, bool showOnlyForCurrentAvatar, ProviderType providerType = ProviderType.Default, bool addSpace = true)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            Guid id = Guid.Empty;
            bool reInstall = false;

            do
            {
                if (string.IsNullOrEmpty(idOrName))
                {
                    if (!CLIEngine.GetConfirmation($"Do you know the GUID/ID or Name of the {STARManager.STARHolonUIName} you wish to {operationName}? Press 'Y' for Yes or 'N' for No."))
                    {
                        Console.WriteLine("");
                        CLIEngine.ShowWorkingMessage("Loading {STARManager.STARHolonUIName}s...");

                        if (showOnlyForCurrentAvatar)
                            ListStarHolons(STARManager.LoadAllForAvatar(STAR.BeamedInAvatar.AvatarId));
                        else
                            ListStarHolons(STARManager.LoadAll(STAR.BeamedInAvatar.AvatarId, null, true, false, 0, providerType: providerType));
                    }
                    else
                        Console.WriteLine("");

                    idOrName = CLIEngine.GetValidInput($"What is the GUID/ID or Name of the {STARManager.STARHolonUIName} you wish to {operationName}?");

                    if (idOrName == "exit")
                        break;
                }

                if (addSpace)
                    Console.WriteLine("");

                if (Guid.TryParse(idOrName, out id))
                {
                    CLIEngine.ShowWorkingMessage($"Loading {STARManager.STARHolonUIName}...");
                    result = STARManager.Load(STAR.BeamedInAvatar.Id, id, 0, providerType);

                    if (result != null && result.Result != null && !result.IsError && showOnlyForCurrentAvatar && result.Result.STARHolonDNA.CreatedByAvatarId != STAR.BeamedInAvatar.AvatarId)
                    {
                        CLIEngine.ShowErrorMessage($"You do not have permission to {operationName} this {STARManager.STARHolonUIName}. It was created by another avatar.");
                        result.Result = default;
                    }
                }
                else
                {
                    CLIEngine.ShowWorkingMessage($"Searching {STARManager.STARHolonUIName}'s...");
                    OASISResult<IEnumerable<T1>> searchResults = STARManager.Search(STAR.BeamedInAvatar.Id, idOrName, showOnlyForCurrentAvatar, false, 0, providerType);

                    if (searchResults != null && searchResults.Result != null && !searchResults.IsError)
                    {
                        if (searchResults.Result.Count() > 1)
                        {
                            ListStarHolons(searchResults, true);

                            do
                            {
                                int number = CLIEngine.GetValidInputForInt($"What is the number of the {STARManager.STARHolonUIName} you wish to {operationName}?");

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
                            CLIEngine.ShowWarningMessage($"No {STARManager.STARHolonUIName} Found!");
                        }
                    }
                    else
                        CLIEngine.ShowErrorMessage($"An error occured calling STARManager.SearchsAsync. Reason: {searchResults.Message}");
                }

                if (result.Result != null && result.Result.STARHolonDNA != null)
                {
                    Show(result.Result);

                    if (result.Result.STARHolonDNA.NumberOfVersions > 1)
                    {
                        if ((operationName == "view" && CLIEngine.GetConfirmation($"{result.Result.STARHolonDNA.NumberOfVersions} versions were found. Do you wish to view the other versions?")) ||
                            (!CLIEngine.GetConfirmation($"{result.Result.STARHolonDNA.NumberOfVersions} versions were found. Do you wish to {operationName} the latest version ({result.Result.STARHolonDNA.Version})?")))
                        {
                            Console.WriteLine("");
                            CLIEngine.ShowWorkingMessage("Loading {STARManager.STARHolonUIName} Versions...");
                            OASISResult<IEnumerable<T1>> versionsResult = STARManager.LoadVersions(result.Result.STARHolonDNA.Id, providerType);
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
                    if (CLIEngine.GetConfirmation($"Please confirm you wish to {operationName} this {STARManager.STARHolonUIName}?"))
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
                                else
                                    result.Result = default;
                            }
                            else
                            {
                                CLIEngine.ShowErrorMessage($"Error occured checking if the {STARManager.STARHolonUIName} is already installed! Reason: Id was not found in the metadata!");
                                result.Result = default;
                            }
                        }

                    }
                    else
                    {
                        if (CLIEngine.GetConfirmation($"Do you wish to search for another {STARManager.STARHolonUIName}?"))
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

        private async Task<OASISResult<T1>> CheckIfAlreadyInstalledAsync(T1 holon, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            OASISResult<bool> oappInstalledResult = await STARManager.IsInstalledAsync(STAR.BeamedInAvatar.Id, holon.STARHolonDNA.Id, holon.STARHolonDNA.Version, providerType);

            if (oappInstalledResult != null && !oappInstalledResult.IsError)
            {
                if (oappInstalledResult.Result)
                {
                    Console.WriteLine("");
                    CLIEngine.ShowWarningMessage($"You have already installed this version (v{holon.STARHolonDNA.Version}). Please uninstall before attempting to re-install.");

                    if (CLIEngine.GetConfirmation($"Do you wish to uninstall the {STARManager.STARHolonUIName} now? Press 'Y' for Yes or 'N' for No."))
                    {
                        Console.WriteLine("");
                        CLIEngine.ShowWorkingMessage($"Uninstalling {STARManager.STARHolonUIName}...");
                        OASISResult<T3> uninstallResult = await STARManager.UninstallAsync(STAR.BeamedInAvatar.Id, result.Result.STARHolonDNA.Id, result.Result.STARHolonDNA.Version, providerType);

                        if (uninstallResult != null && uninstallResult.Result != null && !uninstallResult.IsError)
                        {
                            CLIEngine.ShowSuccessMessage($"{STARManager.STARHolonUIName} Successfully Uninstalled.");
                            result.MetaData["Reinstall"] = "1";
                        }
                        else
                            OASISErrorHandling.HandleError(ref result, $"Error occured uninstalling the {STARManager.STARHolonUIName}! Reason: {uninstallResult.Message}");
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
                OASISErrorHandling.HandleError(ref result, ($"Error occured checking if the {STARManager.STARHolonUIName} is already installed! Reason: {oappInstalledResult.Message}"));

            return result;
        }

        private OASISResult<T1> CheckIfAlreadyInstalled(T1 holon, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            OASISResult<bool> oappInstalledResult = STARManager.IsInstalled(STAR.BeamedInAvatar.Id, holon.STARHolonDNA.Id, holon.STARHolonDNA.Version, providerType);

            if (oappInstalledResult != null && !oappInstalledResult.IsError)
            {
                if (oappInstalledResult.Result)
                {
                    Console.WriteLine("");
                    CLIEngine.ShowWarningMessage($"You have already installed this version (v{holon.STARHolonDNA.Version}). Please uninstall before attempting to re-install.");

                    if (CLIEngine.GetConfirmation($"Do you wish to uninstall the {STARManager.STARHolonUIName} now? Press 'Y' for Yes or 'N' for No."))
                    {
                        Console.WriteLine("");
                        CLIEngine.ShowWorkingMessage($"Uninstalling {STARManager.STARHolonUIName}...");
                        OASISResult<T3> uninstallResult = STARManager.Uninstall(STAR.BeamedInAvatar.Id, result.Result.STARHolonDNA.Id, result.Result.STARHolonDNA.Version, providerType);

                        if (uninstallResult != null && uninstallResult.Result != null && !uninstallResult.IsError)
                        {
                            CLIEngine.ShowSuccessMessage($"{STARManager.STARHolonUIName} Successfully Uninstalled.");
                            result.MetaData["Reinstall"] = "1";
                        }
                        else
                            OASISErrorHandling.HandleError(ref result, $"Error occured uninstalling the {STARManager.STARHolonUIName}! Reason: {uninstallResult.Message}");
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
                OASISErrorHandling.HandleError(ref result, ($"Error occured checking if the {STARManager.STARHolonUIName} is already installed! Reason: {oappInstalledResult.Message}"));

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

                    if (checkResult != null && checkResult.Result != null && !checkResult.IsError)
                        continueInstall = true;
                    else
                        CLIEngine.ShowErrorMessage($"Error checking if the {STARManager.STARHolonUIName} is already installed! Reason: {checkResult.MetaData}");
                }
            }

            if (continueInstall)
                installResult = await InstallAsync(holon, downloadPath, installPath, installMode, fullPathToPublishedFile, providerType);

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

                    if (checkResult != null && checkResult.Result != null && !checkResult.IsError)
                        continueInstall = true;
                    else
                        CLIEngine.ShowErrorMessage($"Error checking if the {STARManager.STARHolonUIName} is already installed! Reason: {checkResult.MetaData}");
                }
            }

            if (continueInstall)
                installResult = Install(holon, downloadPath, installPath, installMode, fullPathToPublishedFile, providerType);

            return installResult;
        }

        private async Task<OASISResult<T3>> InstallAsync(T1 starHolon, string downloadPath, string installPath, InstallMode installMode, string fullPathToPublishedFile = "", ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T3> result = new OASISResult<T3>();

            switch (installMode)
            {
                case InstallMode.DownloadAndInstall:
                    result = await STARManager.DownloadAndInstallAsync(STAR.BeamedInAvatar.Id, starHolon, installPath, downloadPath, true, false, providerType);
                    break;

                case InstallMode.DownloadOnly:
                    {
                        OASISResult<T2> downloadResult = await STARManager.DownloadAsync(STAR.BeamedInAvatar.Id, starHolon, downloadPath, false, providerType);

                        if (downloadResult != null && downloadResult.Result != null && !downloadResult.IsError)
                        {
                            result.Result = new T3() { STARHolonDNA = downloadResult.Result.STARHolonDNA };
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
                    result = await STARManager.InstallAsync(STAR.BeamedInAvatar.Id, fullPathToPublishedFile, installPath, true, null, false, providerType);
                    break;

                case InstallMode.DownloadAndReInstall:
                    result = await STARManager.DownloadAndInstallAsync(STAR.BeamedInAvatar.Id, starHolon, installPath, downloadPath, true, true, providerType);
                    break;

                case InstallMode.ReInstall:
                    result = await STARManager.InstallAsync(STAR.BeamedInAvatar.Id, fullPathToPublishedFile, installPath, true, null, true, providerType);
                    break;
            }

            return result;
        }

        private OASISResult<T3> Install(T1 starHolon, string downloadPath, string installPath, InstallMode installMode, string fullPathToPublishedFile = "", ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T3> result = new OASISResult<T3>();

            switch (installMode)
            {
                case InstallMode.DownloadAndInstall:
                    result = STARManager.DownloadAndInstall(STAR.BeamedInAvatar.Id, starHolon, installPath, downloadPath, true, false, providerType);
                    break;

                case InstallMode.DownloadOnly:
                    {
                        OASISResult<T2> downloadResult = STARManager.Download(STAR.BeamedInAvatar.Id, starHolon, downloadPath, false, providerType);

                        if (downloadResult != null && downloadResult.Result != null && !downloadResult.IsError)
                        {
                            result.Result = new T3() { STARHolonDNA = downloadResult.Result.STARHolonDNA };
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
                    result = STARManager.Install(STAR.BeamedInAvatar.Id, fullPathToPublishedFile, installPath, true, null, false, providerType);
                    break;

                case InstallMode.DownloadAndReInstall:
                    result = STARManager.DownloadAndInstall(STAR.BeamedInAvatar.Id, starHolon, installPath, downloadPath, true, true, providerType);
                    break;

                case InstallMode.ReInstall:
                    result = STARManager.Install(STAR.BeamedInAvatar.Id, fullPathToPublishedFile, installPath, true, null, true, providerType);
                    break;
            }

            return result;
        }

        private void ShowHeader()
        {
            CLIEngine.ShowDivider();
            CLIEngine.ShowMessage(CreateHeader);
            CLIEngine.ShowDivider();
            Console.WriteLine();

            for (int i = 0; i < CreateIntroParagraphs.Count; i++)
                CLIEngine.ShowMessage(CreateIntroParagraphs[i]);

            CLIEngine.ShowDivider();
        }

        private T1 ConvertFromT3ToT1(T3 holon)
        {
            T1 newHolon = new T1();
            newHolon.STARHolonDNA = holon.STARHolonDNA;
            return newHolon;
        }

        private void OnPublishStatusChanged(object sender, STARHolonPublishStatusEventArgs e)
        {
            switch (e.Status)
            {
                case STARHolonPublishStatus.Uploading:
                    CLIEngine.ShowMessage("Uploading...");
                    Console.WriteLine("");
                    break;

                case STARHolonPublishStatus.Published:
                    CLIEngine.ShowSuccessMessage($"{STARManager.STARHolonUIName} Published Successfully");
                    break;

                case STARHolonPublishStatus.Error:
                    CLIEngine.ShowErrorMessage(e.ErrorMessage);
                    break;

                default:
                    CLIEngine.ShowWorkingMessage($"{Enum.GetName(typeof(STARHolonPublishStatus), e.Status)}...");
                    break;
            }
        }

        private void OnUploadStatusChanged(object sender, STARHolonUploadProgressEventArgs e)
        {
            CLIEngine.ShowProgressBar((double)e.Progress / (double)100);
        }

        private void OnInstallStatusChanged(object sender, STARHolonInstallStatusEventArgs e)
        {
            switch (e.Status)
            {
                case STARHolonInstallStatus.Downloading:
                    CLIEngine.ShowMessage("Downloading...");
                    Console.WriteLine("");
                    break;

                case STARHolonInstallStatus.Installed:
                    CLIEngine.ShowSuccessMessage($"{STARManager.STARHolonUIName} Installed Successfully");
                    break;

                case STARHolonInstallStatus.Error:
                    CLIEngine.ShowErrorMessage(e.ErrorMessage);
                    break;

                default:
                    CLIEngine.ShowWorkingMessage($"{Enum.GetName(typeof(STARHolonInstallStatus), e.Status)}...");
                    break;
            }
        }

        private void OnDownloadStatusChanged(object sender, STARHolonDownloadProgressEventArgs e)
        {
            CLIEngine.ShowProgressBar((double)e.Progress / (double)100);
        }
    }
}