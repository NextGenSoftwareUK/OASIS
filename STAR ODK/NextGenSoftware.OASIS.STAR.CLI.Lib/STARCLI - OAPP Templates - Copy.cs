//using System.Diagnostics;
//using NextGenSoftware.CLI.Engine;
//using NextGenSoftware.OASIS.Common;
//using NextGenSoftware.OASIS.API.Core.Enums;
//using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
//using NextGenSoftware.OASIS.STAR.CLI.Lib.Enums;
//using NextGenSoftware.OASIS.API.ONODE.Core.Enums;
//using NextGenSoftware.OASIS.API.ONODE.Core.Events;
//using NextGenSoftware.OASIS.API.ONODE.Core.Holons;
//using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces;
//using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Holons;

//namespace NextGenSoftware.OASIS.STAR.CLI.Lib
//{
//    public static partial class STARCLI
//    {
//        private static bool _init = false;

//        public static void Init()
//        {
//            STAR.OASISAPI.OAPPTemplates.OnDownloadStatusChanged += OAPPTemplates_OnDownloadStatusChanged;
//            STAR.OASISAPI.OAPPTemplates.OnInstallStatusChanged += OAPPTemplates_OnInstallStatusChanged;
//            STAR.OASISAPI.OAPPTemplates.OnPublishStatusChanged += OAPPTemplates_OnPublishStatusChanged;
//            STAR.OASISAPI.OAPPTemplates.OnUploadStatusChanged += OAPPTemplates_OnUploadStatusChanged;
//            _init = true;
//        }

//        public static void Dispose()
//        {
//            STAR.OASISAPI.OAPPTemplates.OnDownloadStatusChanged -= OAPPTemplates_OnDownloadStatusChanged;
//            STAR.OASISAPI.OAPPTemplates.OnInstallStatusChanged -= OAPPTemplates_OnInstallStatusChanged;
//            STAR.OASISAPI.OAPPTemplates.OnPublishStatusChanged -= OAPPTemplates_OnPublishStatusChanged;
//            STAR.OASISAPI.OAPPTemplates.OnUploadStatusChanged -= OAPPTemplates_OnUploadStatusChanged;
//        }

//        public static async Task CreateOAPPTemplateAsync(object createParams, ProviderType providerType = ProviderType.Default)
//        {
//            CLIEngine.ShowDivider();
//            CLIEngine.ShowMessage("Welcome to the OAPP Template Wizard");
//            CLIEngine.ShowDivider();
//            Console.WriteLine();
//            CLIEngine.ShowMessage("This wizard will allow you create an OAPP Template which can be used to create a OAPP from.", false);
//            CLIEngine.ShowMessage("The OAPP Template can be created from anything you like such as a website, javascript template, game, app, service, etc in any language, platform or os.");
//            CLIEngine.ShowMessage("You simply need to add specefic STAR ODK OAPP Template reserved tags where dynamic data will be injected in from the OAPP meta data.");
//            CLIEngine.ShowMessage("The wizard will create an empty folder with a OAPPSystemHolonDNA.json file in it. You then simply place any files/folders you need into this folder.");
//            CLIEngine.ShowMessage("Finally you run the sub-command 'oapp template publish' to convert the folder containing the OAPP Template (can contain any number of files and sub-folders) into a OAPP Template file (.oapptemplate).");
//            CLIEngine.ShowMessage("You can then share the .oapptemplate file with others from which you can create OAPP's from. You can also optionally choose to upload the .oapptemplate file to the STARNET store so others can search, download and install the OAPP Template. They can then create OAPP's from the template.");
//            CLIEngine.ShowDivider();

//            string OAPPTemplateName = CLIEngine.GetValidInput("What is the name of the OAPP Template?");

//            if (OAPPTemplateName == "exit")
//                return;

//            string OAPPTemplateDesc = CLIEngine.GetValidInput("What is the description of the OAPP Template?");

//            if (OAPPTemplateDesc == "exit")
//                return;

//            object value = CLIEngine.GetValidInputForEnum("What type of OAPP Template do you wish to create?", typeof(OAPPTemplateType));

//            if (value != null)
//            {
//                if (value.ToString() == "exit")
//                    return;

//                OAPPTemplateType OAPPTemplateType = (OAPPTemplateType)value;
//                string oappTemplatePath = "";

//                if (!string.IsNullOrEmpty(STAR.STARDNA.BasePath))
//                    oappTemplatePath = Path.Combine(STAR.STARDNA.BasePath, STAR.STARDNA.DefaultOAPPTemplatesSourcePath);
//                else
//                    oappTemplatePath = STAR.STARDNA.DefaultOAPPTemplatesSourcePath;

//                if (!CLIEngine.GetConfirmation($"Do you wish to create the OAPP Template in the default path defined in the STARDNA as 'DefaultOAPPTemplatesSourcePath'? The current path points to: {oappTemplatePath}"))
//                    oappTemplatePath = CLIEngine.GetValidFolder("Where do you wish to create the OAPP Template?");

//                oappTemplatePath = Path.Combine(oappTemplatePath, OAPPTemplateName);

//                Console.WriteLine("");
//                CLIEngine.ShowWorkingMessage("Generating OAPP Template...");
//                OASISResult<IOAPPTemplate> oappTemplateResult = await STAR.OASISAPI.OAPPTemplates.CreateOAPPTemplateAsync(STAR.BeamedInAvatar.Id, OAPPTemplateName, OAPPTemplateDesc, OAPPTemplateType, oappTemplatePath, providerType);

//                if (oappTemplateResult != null)
//                {
//                    if (!oappTemplateResult.IsError && oappTemplateResult.Result != null)
//                    {
//                        CLIEngine.ShowSuccessMessage($"OAPP Template Successfully Generated.");
//                        ShowOAPPTemplate(oappTemplateResult.Result);
//                        Console.WriteLine("");

//                        if (CLIEngine.GetConfirmation("Do you wish to open the OAPP Template folder now?"))
//                            Process.Start("explorer.exe", oappTemplatePath);

//                        Console.WriteLine("");
//                    }
//                }
//                else
//                    CLIEngine.ShowErrorMessage($"Unknown Error Occured.");
//            }
//        }

//        public static async Task EditOAPPTemplateAsync(string idOrName = "", object editParams = null, ProviderType providerType = ProviderType.Default)
//        {
//            OASISResult<IOAPPTemplate> loadResult = await LoadOAPPTemplateAsync(idOrName, "edit", true, providerType);
//            bool changesMade = false;

//            if (loadResult != null && loadResult.Result != null && !loadResult.IsError)
//            {
//                if (CLIEngine.GetConfirmation("Do you wish to edit the OAPP Template Name?"))
//                {
//                    Console.WriteLine("");
//                    loadResult.Result.OAPPSystemHolonDNA.Name = CLIEngine.GetValidInput("What is the new name of the OAPP Template?");
//                    changesMade = true;
//                }
//                else
//                    Console.WriteLine("");

//                if (CLIEngine.GetConfirmation("Do you wish to edit the OAPP Template Description?"))
//                {
//                    Console.WriteLine("");
//                    loadResult.Result.OAPPSystemHolonDNA.Description = CLIEngine.GetValidInput("What is the new description of the OAPP Template?");
//                    changesMade = true;
//                }
//                else
//                    Console.WriteLine("");

//                if (CLIEngine.GetConfirmation("Do you wish to edit the OAPP Template Type?"))
//                {
//                    Console.WriteLine("");
//                    object value = CLIEngine.GetValidInputForEnum("What is the new type of the OAPP Template?", typeof(OAPPTemplateType));

//                    if (value != null)
//                    {
//                        if (value.ToString() == "exit")
//                            return;

//                        loadResult.Result.OAPPSystemHolonDNA.OAPPSystemHolonType = (OAPPTemplateType)value;
//                        changesMade = true;
//                    }
//                }
//                else
//                    Console.WriteLine("");

//                if (CLIEngine.GetConfirmation("Do you wish to edit the launch target?"))
//                {
//                    Console.WriteLine("");
//                    loadResult.Result.OAPPSystemHolonDNA.LaunchTarget = CLIEngine.GetValidInput("What is the new launch target of the OAPP Template?");
//                    changesMade = true;
//                }
//                else
//                    Console.WriteLine("");

//                if (changesMade)
//                {
//                    OASISResult<IOAPPTemplate> result = await STAR.OASISAPI.OAPPTemplates.EditOAPPTemplateAsync(loadResult.Result, loadResult.Result.OAPPSystemHolonDNA, STAR.BeamedInAvatar.Id, providerType);
//                    Console.WriteLine("");
//                    CLIEngine.ShowWorkingMessage("Saving OAPP Template...");

//                    if (result != null && !result.IsError && result.Result != null)
//                    {
//                        (result, bool saveResult) = ErrorHandling.HandleResponse(result, await STAR.OASISAPI.OAPPTemplates.WriteDNAAsync(result.Result.OAPPSystemHolonDNA, result.Result.OAPPSystemHolonDNA.SourcePath), "Error occured saving the OAPPSystemHolonDNA. Reason: ", "OAPP Template Successfully Updated.");

//                        if (saveResult)
//                            ShowOAPPTemplate(result.Result);
//                    }
//                    else
//                        CLIEngine.ShowErrorMessage($"An error occured updating the OAPP Template. Reason: {result.Message}");
//                }

//                if (loadResult.Result.OAPPSystemHolonDNA.PublishedOn != DateTime.MinValue && CLIEngine.GetConfirmation($"Do you wish to upload any changes you have made in the Source folder ({loadResult.Result.OAPPSystemHolonDNA.SourcePath})? The version number will remain the same ({loadResult.Result.OAPPSystemHolonDNA.Version})."))
//                    await PublishOAPPTemplateAsync(loadResult.Result.OAPPSystemHolonDNA.SourcePath, true, providerType);
//                else
//                    Console.WriteLine("");
//            }
//            else
//            {
//                Console.WriteLine("");
//                CLIEngine.ShowErrorMessage($"An error occured loading the OAPP Template. Reason: {loadResult.Message}");
//            }
//        }

//        public static async Task DeleteOAPPTemplateAsync(string idOrName = "", bool softDelete = true, ProviderType providerType = ProviderType.Default)
//        {
//            OASISResult<IOAPPTemplate> result = await LoadOAPPTemplateAsync(idOrName, "delete", true, providerType);

//            if (result != null && !result.IsError && result.Result != null)
//            {
//                if (CLIEngine.GetConfirmation("Are you sure you wish to delete this OAPP Template? This will also delete the OAPP Template from the Source and Published folders and remove it from the STARNET Store (if you have already published it)"))
//                {
//                    Console.WriteLine("");
//                    bool deleteDownload = CLIEngine.GetConfirmation("Do you wish to also delete the correponding downloaded OAPP Template? (if there is any)");

//                    Console.WriteLine("");
//                    bool deleteInstall = CLIEngine.GetConfirmation("Do you wish to also delete the correponding installed OAPP Template? (if there is any). This is different to uninstalling because uninstalled OAPP Templates are still visible with the 'list uninstalled' sub-command and have the option to re-install. Whereas once it is deleted it is gone forever!");

//                    Console.WriteLine("");
//                    if (CLIEngine.GetConfirmation("ARE YOU SURE YOU WITH TO PERMANENTLY DELETE THE OAPP TEMPLATE? IT WILL NOT BE POSSIBLE TO RECOVER AFTRWARDS!", ConsoleColor.Red))
//                    {
//                        Console.WriteLine("");
//                        CLIEngine.ShowWorkingMessage("Deleting OAPP Template...");
//                        result = await STAR.OASISAPI.OAPPTemplates.DeleteOAPPTemplateAsync(STAR.BeamedInAvatar.Id, result.Result, result.Result.OAPPSystemHolonDNA.VersionSequence, true, deleteDownload, deleteInstall, providerType);

//                        if (result != null && !result.IsError && result.Result != null)
//                            CLIEngine.ShowSuccessMessage("OAPP Template Successfully Deleted.");
//                        else
//                            CLIEngine.ShowErrorMessage($"An error occured deleting the OAPP Template. Reason: {result.Message}");
//                    }
//                }
//            }
//            else
//                CLIEngine.ShowErrorMessage($"An error occured loading the OAPP Template. Reason: {result.Message}");
//        }

//        public static async Task PublishOAPPTemplateAsync(string oappTemplatePath = "", bool edit = false, ProviderType providerType = ProviderType.Default)
//        {
//            bool generateOAPP = true;
//            bool uploadOAPPToCloud = true;
//            ProviderType OAPPBinaryProviderType = ProviderType.None;  
//            string launchTarget = "";
//            string publishPath = "";
//            string launchTargetQuestion = "";
//            bool simpleWizard = false;

//            if (!_init)
//                Init();

//            if (CLIEngine.GetConfirmation("Do you wish to launch the Simple or Advanced Wizard? The Simple Wizard will use defaults (recommended) but the Advanced Wizard will allow greater control and customisation. Press 'Y' for Simple or 'N' for Advanced."))
//                simpleWizard = true;

//            if (string.IsNullOrEmpty(oappTemplatePath))
//            {
//                Console.WriteLine("");
//                string OAPPPathQuestion = "What is the full path to the OAPP Template directory?";
//                launchTargetQuestion = "What is the relative path (from the root of the path given above, e.g bin\\launch.exe) to the launch target for the OAPP Template? (This could be the exe or batch file for a desktop or console app, or the index.html page for a website, etc)";
//                oappTemplatePath = CLIEngine.GetValidFolder(OAPPPathQuestion, false);
//            }

//            OASISResult<IOAPPSystemHolonDNA> OAPPTemplateDNAResult = await STAR.OASISAPI.OAPPTemplates.ReadDNAFromSourceOrInstallFolderAsync<IOAPPSystemHolonDNA>(oappTemplatePath);

//            if (OAPPTemplateDNAResult != null && OAPPTemplateDNAResult.Result != null && !OAPPTemplateDNAResult.IsError)
//            {
//                OASISResult<IOAPPTemplate> OAPPTemplateResult = await STAR.OASISAPI.OAPPTemplates.LoadOAPPTemplateAsync(STAR.BeamedInAvatar.Id, OAPPTemplateDNAResult.Result.Id, 0, providerType);

//                if (OAPPTemplateResult != null && OAPPTemplateResult.Result != null && !OAPPTemplateResult.IsError)
//                {
//                    OAPPTemplateResult.Result.OAPPSystemHolonDNA.Version = OAPPTemplateDNAResult.Result.Version; //Update the version from the JSON file.
//                    ShowOAPPTemplate(OAPPTemplateResult.Result);

//                    if (!CLIEngine.GetConfirmation("Is this the correct OAPP Template you wish to publish?"))
//                    {
//                        Console.WriteLine("");
//                        return;
//                    }

//                    Console.WriteLine("");

//                    object templateType = Enum.Parse(typeof(OAPPTemplateType), OAPPTemplateDNAResult.Result.OAPPSystemHolonType.ToString());
//                    OAPPTemplateType OAPPTemplateType = (OAPPTemplateType)templateType;

//                    switch (OAPPTemplateType)
//                    {
//                        case OAPPTemplateType.Console:
//                        case OAPPTemplateType.WPF:
//                        case OAPPTemplateType.WinForms:
//                            launchTarget = $"{OAPPTemplateDNAResult.Result.Name}.exe"; //TODO: For this line to work need to remove the namespace question so it just uses the OAPPName as the namespace. //TODO: Eventually this will be set in the OAPPTemplate and/or can also be set when I add the command line dotnet publish integration.
//                            break;

//                        case OAPPTemplateType.Blazor:
//                        case OAPPTemplateType.MAUI:
//                        case OAPPTemplateType.WebMVC:
//                            launchTarget = $"index.html";
//                            break;
//                    }

//                    if (!string.IsNullOrEmpty(launchTarget))
//                    {
//                        if (!CLIEngine.GetConfirmation($"{launchTargetQuestion} Do you wish to use the following default launch target: {launchTarget}?"))
//                        {
//                            Console.WriteLine("");
//                            launchTarget = CLIEngine.GetValidFile("What launch target do you wish to use? ", oappTemplatePath);
//                        }
//                        else
//                            launchTarget = Path.Combine(oappTemplatePath, launchTarget);
//                    }
//                    else
//                        launchTarget = CLIEngine.GetValidFile(launchTargetQuestion, oappTemplatePath);

//                    Console.WriteLine("");
//                    bool registerOnSTARNET = CLIEngine.GetConfirmation("Do you wish to publish to STARNET? If you select 'Y' to this question then your OAPP Template will be published to STARNET where others will be able to find, download and install. If you select 'N' then only the .oapptemplate install file will be generated on your local device, which you can distribute as you please. This file will also be generated even if you publish to STARNET.");
//                    Console.WriteLine("");

//                    if (registerOnSTARNET && !simpleWizard)
//                    {
//                        CLIEngine.ShowMessage("Do you wish to publish/upload the .oapptemplate file to an OASIS Provider or to the cloud or both? Depending on which OASIS Provider is chosen such as IPFSOASIS there may issues such as speed, relialbility etc for such a large file. If you choose to upload to the cloud this could be faster and more reliable (but there is a limit of 5 OAPPs on the free plan and you will need to upgrade to upload more than 5 OAPPs). You may want to choose to use both to add an extra layer of redundancy (recommended).");

//                        if (!CLIEngine.GetConfirmation("Do you wish to upload to the cloud?"))
//                            uploadOAPPToCloud = false;

//                        Console.WriteLine("");
//                        if (CLIEngine.GetConfirmation("Do you wish to upload to an OASIS Provider? Make sure you select a provider that can handle large files such as IPFSOASIS, HoloOASIS etc. Also remember the OASIS Hyperdrive will only be able to auto-replicate to other providers that also support large files and are free or cost effective. By default it will NOT auto-replicate large files, you will need to manually configure this in your OASIS Profile settings."))
//                        {
//                            Console.WriteLine("");
//                            object largeProviderTypeObject = CLIEngine.GetValidInputForEnum("What provider do you wish to publish the OAPP to? (The default is IPFSOASIS)", typeof(ProviderType));

//                            if (largeProviderTypeObject != null)
//                                OAPPBinaryProviderType = (ProviderType)largeProviderTypeObject;
//                        }
//                        else
//                            Console.WriteLine("");
//                    }

//                    if (Path.IsPathRooted(STAR.STARDNA.DefaultOAPPTemplatesPublishedPath))
//                        publishPath = STAR.STARDNA.DefaultOAPPTemplatesPublishedPath;
//                    else
//                        publishPath = Path.Combine(STAR.STARDNA.BasePath, STAR.STARDNA.DefaultOAPPTemplatesPublishedPath);

//                    if (!simpleWizard)
//                    {
//                        if (!CLIEngine.GetConfirmation($"Do you wish to publish the OAPP Template to the default publish folder defined in the STARDNA as DefaultOAPPTemplatesPublishedPath : {publishPath}?"))
//                        {
//                            Console.WriteLine("");

//                            if (CLIEngine.GetConfirmation($"Do you wish to publish the OAPP Template to: {Path.Combine(oappTemplatePath, "Published")}?"))
//                                publishPath = Path.Combine(oappTemplatePath, "Published");
//                            else
//                            {
//                                Console.WriteLine("");
//                                publishPath = CLIEngine.GetValidFolder("Where do you wish to publish the OAPP Template?", true);
//                            }
//                        }
//                    }

//                    publishPath = new DirectoryInfo(publishPath).FullName;

//                    Console.WriteLine("");
//                    CLIEngine.ShowWorkingMessage("Publishing OAPP Template...");
//                    OASISResult<IOAPPTemplate> publishResult = await STAR.OASISAPI.OAPPTemplates.PublishOAPPTemplateAsync(STAR.BeamedInAvatar.Id, oappTemplatePath, launchTarget, publishPath, registerOnSTARNET, generateOAPP, uploadOAPPToCloud, edit, providerType, OAPPBinaryProviderType);

//                    if (publishResult != null && !publishResult.IsError && publishResult.Result != null)
//                    {
//                        ShowOAPPTemplate(publishResult.Result);

//                        if (CLIEngine.GetConfirmation("Do you wish to install the OAPP Template now?"))
//                            await DownloadAndInstallOAPPTemplateAsync(publishResult.Result.OAPPSystemHolonDNA.Id.ToString(), InstallMode.DownloadAndInstall, providerType);

//                        Console.WriteLine("");
//                    }
//                    else
//                    {
//                        if (publishResult.Message.Contains("Please make sure you increment the version"))
//                        {
//                            if (CLIEngine.GetConfirmation("Do you wish to open the OAPPTemplateDNA.json file now?"))
//                                Process.Start("explorer.exe", Path.Combine(OAPPTemplateDNAResult.Result.SourcePath, "OAPPTemplateDNA.json"));
//                        }
//                        else
//                            CLIEngine.ShowErrorMessage($"An error occured publishing the OAPP Template. Reason: {publishResult.Message}");

//                        Console.WriteLine("");
//                    }
//                }
//                else
//                    CLIEngine.ShowErrorMessage($"The OAPPTemplate could not be found for id {OAPPTemplateDNAResult.Result.Id} found in the OAPPTemplateDNA.json file. It could be corrupt, the id could be wrong or you may not have permission, please check and try again, or create a new OAPP Template.");
//            }
//            else
//                CLIEngine.ShowErrorMessage("The OAPPTemplateDNA.json file could not be found! Please ensure it is in the folder you specified.");
//        }

//        public static async Task UnpublishOAPPTemplateAsync(string idOrName = "", ProviderType providerType = ProviderType.Default)
//        {
//            OASISResult<IOAPPTemplate> result = await LoadOAPPTemplateAsync(idOrName, "unpublish", true, providerType);

//            if (result != null && !result.IsError && result.Result != null)
//            {
//                OASISResult<IOAPPTemplate> unpublishResult = await STAR.OASISAPI.OAPPTemplates.UnpublishOAPPTemplateAsync(STAR.BeamedInAvatar.Id, result.Result, providerType);

//                if (unpublishResult != null && !unpublishResult.IsError && unpublishResult.Result != null)
//                {
//                    CLIEngine.ShowSuccessMessage("OAPP Template Successfully Unpublished.");
//                    ShowOAPPTemplate(result.Result);
//                }
//                else
//                    CLIEngine.ShowErrorMessage($"An error occured unpublishing the OAPP Template. Reason: {unpublishResult.Message}");
//            }
//        }

//        public static async Task RepublishOAPPTemplateAsync(string idOrName = "", ProviderType providerType = ProviderType.Default)
//        {
//            OASISResult<IOAPPTemplate> result = await LoadOAPPTemplateAsync(idOrName, "republish", true, providerType);

//            if (result != null && !result.IsError && result.Result != null)
//            {
//                OASISResult<IOAPPTemplate> republishResult = await STAR.OASISAPI.OAPPTemplates.RepublishOAPPTemplateAsync(STAR.BeamedInAvatar.Id, result.Result, providerType);

//                if (republishResult != null && !republishResult.IsError && republishResult.Result != null)
//                {
//                    CLIEngine.ShowSuccessMessage("OAPP Template Successfully Republished.");
//                    ShowOAPPTemplate(result.Result);
//                }
//                else
//                    CLIEngine.ShowErrorMessage($"An error occured unpublishing the OAPP Template. Reason: {republishResult.Message}");
//            }
//        }

//        public static async Task ActivateOAPPTemplateAsync(string idOrName = "", ProviderType providerType = ProviderType.Default)
//        {
//            OASISResult<IOAPPTemplate> result = await LoadOAPPTemplateAsync(idOrName, "activate", true, providerType);

//            if (result != null && !result.IsError && result.Result != null)
//            {
//                if (result.MetaData != null && result.MetaData.ContainsKey("Active") && result.MetaData["Active"] != null && result.MetaData["Active"] == "1")
//                {
//                    OASISResult<IOAPPTemplate> activateResult = await STAR.OASISAPI.OAPPTemplates.ActivateOAPPTemplateAsync(STAR.BeamedInAvatar.Id, result.Result, providerType);

//                    if (activateResult != null && !activateResult.IsError && activateResult.Result != null)
//                    {
//                        CLIEngine.ShowSuccessMessage("OAPP Template Successfully Activated.");
//                        ShowOAPPTemplate(result.Result);
//                    }
//                    else
//                        CLIEngine.ShowErrorMessage($"An error occured activating the OAPP Template. Reason: {activateResult.Message}");
//                }
//                else
//                    CLIEngine.ShowErrorMessage($"The OAPP Template is already activated!");
//            }   
//        }

//        public static async Task DeactivateOAPPTemplateAsync(string idOrName = "", ProviderType providerType = ProviderType.Default)
//        {
//            OASISResult<IOAPPTemplate> result = await LoadOAPPTemplateAsync(idOrName, "deactivate", true, providerType);

//            if (result != null && !result.IsError && result.Result != null)
//            {
//                if (result.MetaData != null && result.MetaData.ContainsKey("Active") && result.MetaData["Active"] != null && result.MetaData["Active"] == "0")
//                {
//                    OASISResult<IOAPPTemplate> deactivateResult = await STAR.OASISAPI.OAPPTemplates.DeactivateOAPPTemplateAsync(STAR.BeamedInAvatar.Id, result.Result, providerType);

//                    if (deactivateResult != null && !deactivateResult.IsError && deactivateResult.Result != null)
//                    {
//                        CLIEngine.ShowSuccessMessage("OAPP Template Successfully Deactivated.");
//                        ShowOAPPTemplate(result.Result);
//                    }
//                    else
//                        CLIEngine.ShowErrorMessage($"An error occured deactivating the OAPP Template. Reason: {deactivateResult.Message}");
//                }
//                else
//                    CLIEngine.ShowErrorMessage($"The OAPP Template is already deactivated!");
//            }
//        }

//        public static async Task<OASISResult<IInstalledOAPPTemplate>> DownloadAndInstallOAPPTemplateAsync(string idOrName = "", InstallMode installMode = InstallMode.DownloadAndInstall, ProviderType providerType = ProviderType.Default)
//        {
//            OASISResult<IInstalledOAPPTemplate> installResult = new OASISResult<IInstalledOAPPTemplate>();
//            string downloadPath = "";
//            string installPath = "";
//            bool simpleWizard = false;
//            string operation = "install";

//            if (installMode == InstallMode.DownloadOnly)
//                operation = "download";

//            if (!_init)
//                Init();

//            if (Path.IsPathRooted(STAR.STARDNA.DefaultOAPPTemplatesDownloadedPath))
//                downloadPath = STAR.STARDNA.DefaultOAPPTemplatesDownloadedPath;
//            else
//                downloadPath = Path.Combine(STAR.STARDNA.BasePath, STAR.STARDNA.DefaultOAPPTemplatesDownloadedPath);

//            if (Path.IsPathRooted(STAR.STARDNA.DefaultOAPPTemplatesInstalledPath))
//                installPath = STAR.STARDNA.DefaultOAPPTemplatesInstalledPath;
//            else
//                installPath = Path.Combine(STAR.STARDNA.BasePath, STAR.STARDNA.DefaultOAPPTemplatesInstalledPath);

//            Console.WriteLine("");

//            if (CLIEngine.GetConfirmation("Do you wish to launch the Simple or Advanced Wizard? The Simple Wizard will use defaults (recommended) but the Advanced Wizard will allow greater control and customisation. Press 'Y' for Simple or 'N' for Advanced."))
//                simpleWizard = true;

//            if (!simpleWizard)
//            {
//                Console.WriteLine("");

//                if (!CLIEngine.GetConfirmation($"Do you wish to download the OAPP Template to the default download folder defined in the STARDNA as DefaultOAPPTemplatesDownloadedPath : {downloadPath}?"))
//                {
//                    Console.WriteLine("");
//                    downloadPath = CLIEngine.GetValidFolder("What is the full path to where you wish to download the OAPP Template?", true);
//                }

//                downloadPath = new DirectoryInfo(downloadPath).FullName;

//                if (installMode != InstallMode.DownloadAndInstall)
//                {
//                    Console.WriteLine("");

//                    if (!CLIEngine.GetConfirmation($"Do you wish to install the OAPP Template to the default install folder defined in the STARDNA as DefaultOAPPTemplatesInstalledPath : {installPath}?"))
//                    {
//                        Console.WriteLine("");
//                        installPath = CLIEngine.GetValidFolder("What is the full path to where you wish to install the OAPP Template?", true);
//                    }

//                    installPath = new DirectoryInfo(installPath).FullName;
//                }
//            }

//            if (!string.IsNullOrEmpty(idOrName))
//            {
//                Console.WriteLine("");
//                OASISResult<IOAPPTemplate> result = await LoadOAPPTemplateForProviderAsync(idOrName, operation, false, providerType, false);

//                if (result != null && result.Result != null && !result.IsError)
//                {
//                    if (result.MetaData != null && result.MetaData.ContainsKey("Reinstall") && !string.IsNullOrEmpty(result.MetaData["Reinstall"]) && result.MetaData["Reinstall"] == "1" && installMode == InstallMode.DownloadAndInstall)
//                        installMode = InstallMode.DownloadAndReInstall;
                    
//                    installResult = await CheckIfInstalledAndInstallOAPPTemplateAsync(result.Result, downloadPath, installPath, installMode, "", providerType);
//                }
//            }
//            else
//            {
//                Console.WriteLine("");
//                if (installMode != InstallMode.DownloadOnly && CLIEngine.GetConfirmation("Do you wish to install the OAPP Template from a local .oapptemplate file or from STARNET? Press 'Y' for local .oapptemplate file or 'N' for STARNET."))
//                {
//                    Console.WriteLine("");
//                    string oappPath = CLIEngine.GetValidFile("What is the full path to the .oapptemplate file?");

//                    if (oappPath == "exit")
//                        return installResult;

//                    OASISResult<IOAPPSystemHolonDNA> oappTemplateDNAResult = await STAR.OASISAPI.OAPPTemplates.ReadDNAFromPublishedFileAsync<IOAPPSystemHolonDNA>(oappPath);

//                    if (oappTemplateDNAResult != null && oappTemplateDNAResult.Result != null && !oappTemplateDNAResult.IsError)
//                    {
//                        OASISResult<IOAPPTemplate> oappTemplateResult = await STAR.OASISAPI.OAPPTemplates.LoadOAPPTemplateAsync(STAR.BeamedInAvatar.Id, oappTemplateDNAResult.Result.Id, 0, providerType);

//                        if (oappTemplateResult != null && oappTemplateResult.Result != null && !oappTemplateResult.IsError)
//                        {
//                            installMode = InstallMode.InstallOnly;

//                            if (oappTemplateResult.MetaData != null && oappTemplateResult.MetaData.ContainsKey("Reinstall") && !string.IsNullOrEmpty(oappTemplateResult.MetaData["Reinstall"]) && oappTemplateResult.MetaData["Reinstall"] == "1")
//                                installMode = InstallMode.ReInstall;

//                            installResult = await CheckIfInstalledAndInstallOAPPTemplateAsync(oappTemplateResult.Result, downloadPath, installPath, installMode, oappPath, providerType);
//                        }
//                        else
//                            CLIEngine.ShowErrorMessage($"The OAPPTemplate could not be found for id {oappTemplateDNAResult.Result.Id} found in the OAPPSystemHolonDNA.json file. It could be corrupt or the id could be wrong, please check and try again, or create a new OAPP Template.");
//                    }
//                    else
//                        CLIEngine.ShowErrorMessage($"The OAPP Template could not be found or is not valid! Please ensure it is in the folder you specified.");
//                }
//                else
//                {
//                    Console.WriteLine("");
//                    OASISResult<IOAPPTemplate> result = await LoadOAPPTemplateForProviderAsync("", operation, false, providerType, false);

//                    if (result != null && result.Result != null && !result.IsError)
//                    {
//                        if (result.MetaData != null && result.MetaData.ContainsKey("Reinstall") && !string.IsNullOrEmpty(result.MetaData["Reinstall"]) && result.MetaData["Reinstall"] == "1" && installMode == InstallMode.DownloadAndInstall)
//                            installMode = InstallMode.DownloadAndReInstall;

//                        installResult = await CheckIfInstalledAndInstallOAPPTemplateAsync(result.Result, downloadPath, installPath, installMode, "", providerType);
//                    }
//                    else
//                    {
//                        installResult.Message = result.Message;
//                        installResult.IsError = true;
//                    }
//                }
//            }

//            if (installResult != null)
//            {
//                if (!installResult.IsError && installResult.Result != null)
//                {
//                    ShowInstalledOAPPTemplate(installResult.Result);

//                    if (CLIEngine.GetConfirmation("Do you wish to open the folder to the OAPP Template now?"))
//                        STAR.OASISAPI.OAPPTemplates.OpenOAPPTemplateFolder(STAR.BeamedInAvatar.Id, installResult.Result);
//                }
//                else
//                    CLIEngine.ShowErrorMessage($"Error {operation}ing OAPP Template. Reason: {installResult.Message}");
//            }
//            else
//                CLIEngine.ShowErrorMessage($"Error {operation}ing OAPP Template. Reason: Unknown error occured!");

//            Console.WriteLine("");
//            return installResult;
//        }

//        public static OASISResult<IInstalledOAPPTemplate> DownloadAndInstallOAPPTemplate(string idOrName = "", ProviderType providerType = ProviderType.Default)
//        {
//            OASISResult<IInstalledOAPPTemplate> installResult = new OASISResult<IInstalledOAPPTemplate>();
//            string downloadPath = "";
//            string installPath = "";

//            if (Path.IsPathRooted(STAR.STARDNA.DefaultOAPPTemplatesDownloadedPath))
//                downloadPath = STAR.STARDNA.DefaultOAPPTemplatesDownloadedPath;
//            else
//                downloadPath = Path.Combine(STAR.STARDNA.BasePath, STAR.STARDNA.DefaultOAPPTemplatesDownloadedPath);

//            if (Path.IsPathRooted(STAR.STARDNA.DefaultOAPPTemplatesInstalledPath))
//                installPath = STAR.STARDNA.DefaultOAPPTemplatesInstalledPath;
//            else
//                installPath = Path.Combine(STAR.STARDNA.BasePath, STAR.STARDNA.DefaultOAPPTemplatesInstalledPath);

//            Console.WriteLine("");

//            if (!CLIEngine.GetConfirmation($"Do you wish to download the OAPP Template to the default download folder defined in the STARDNA as DefaultOAPPTemplatesDownloadedPath : {downloadPath}?"))
//            {
//                Console.WriteLine("");
//                downloadPath = CLIEngine.GetValidFolder("What is the full path to where you wish to download the OAPP Template?", true);
//            }

//            downloadPath = new DirectoryInfo(downloadPath).FullName;

//            Console.WriteLine("");

//            if (!CLIEngine.GetConfirmation($"Do you wish to install the OAPP Template to the default install folder defined in the STARDNA as DefaultOAPPTemplatesInstalledPath : {installPath}?"))
//            {
//                Console.WriteLine("");
//                installPath = CLIEngine.GetValidFolder("What is the full path to where you wish to install the OAPP Template?", true);
//            }

//            installPath = new DirectoryInfo(installPath).FullName;

//            if (!string.IsNullOrEmpty(idOrName))
//            {
//                Console.WriteLine("");
//                OASISResult<IOAPPTemplate> result = LoadOAPPTemplateForProvider(idOrName, "install", false, providerType, false);

//                if (result != null && result.Result != null && !result.IsError)
//                    installResult = STAR.OASISAPI.OAPPTemplates.DownloadAndInstallOAPPTemplate(STAR.BeamedInAvatar.Id, result.Result, installPath, downloadPath, true, false, providerType);
//            }
//            else
//            {
//                Console.WriteLine("");
//                if (CLIEngine.GetConfirmation("Do you wish to install the OAPP Template from a local .oapptemplate file or from STARNET? Press 'Y' for local .oapptemplate file or 'N' for STARNET."))
//                {
//                    Console.WriteLine("");
//                    string oappPath = CLIEngine.GetValidFile("What is the full path to the .oapptemplate file?");

//                    if (oappPath == "exit")
//                        return installResult;

//                    installResult = STAR.OASISAPI.OAPPTemplates.InstallOAPPTemplate(STAR.BeamedInAvatar.Id, oappPath, installPath, true, null, false, providerType);
//                }
//                else
//                {
//                    Console.WriteLine("");
//                    CLIEngine.ShowWorkingMessage("Loading OAPP Templates...");
//                    OASISResult<IEnumerable<IOAPPTemplate>> oappTemplatesResult = ListAllOAPPTemplates();

//                    if (oappTemplatesResult != null && oappTemplatesResult.Result != null && !oappTemplatesResult.IsError && oappTemplatesResult.Result.Count() > 0)
//                    {
//                        OASISResult<IOAPPTemplate> result = LoadOAPPTemplateForProvider("", "install", false, providerType, false);

//                        if (result != null && result.Result != null && !result.IsError)
//                            installResult = STAR.OASISAPI.OAPPTemplates.DownloadAndInstallOAPPTemplate(STAR.BeamedInAvatar.Id, result.Result, installPath, downloadPath, true, false, providerType);
//                        else
//                        {
//                            installResult.Message = result.Message;
//                            installResult.IsError = true;
//                        }
//                    }
//                    else
//                    {
//                        installResult.Message = "No OAPP Templates found to install.";
//                        installResult.IsError = true;
//                    }
//                }
//            }

//            if (installResult != null)
//            {
//                if (!installResult.IsError && installResult.Result != null)
//                {
//                    ShowInstalledOAPPTemplate(installResult.Result);

//                    if (CLIEngine.GetConfirmation("Do you wish to open the folder to the OAPP Template now?"))
//                        STAR.OASISAPI.OAPPTemplates.OpenOAPPTemplateFolder(STAR.BeamedInAvatar.Id, installResult.Result);
//                }
//                else
//                    CLIEngine.ShowErrorMessage($"Error installing OAPP Template. Reason: {installResult.Message}");
//            }
//            else
//                CLIEngine.ShowErrorMessage($"Error installing OAPP Template. Reason: Unknown error occured!");

//            Console.WriteLine("");
//            return installResult;
//        }

//        public static async Task UninstallOAPPTemplateAsync(string idOrName = "", ProviderType providerType = ProviderType.Default)
//        {
//            OASISResult<IOAPPTemplate> result = await LoadOAPPTemplateAsync(idOrName, "uninstall", true, providerType);

//            if (result != null && !result.IsError && result.Result != null)
//            {
//                OASISResult<IInstalledOAPPTemplate> uninstallResult = await STAR.OASISAPI.OAPPTemplates.UninstallOAPPTemplateAsync(STAR.BeamedInAvatar.Id, result.Result.Id, result.Result.OAPPSystemHolonDNA.Version, providerType);

//                if (uninstallResult != null)
//                {
//                    if (!uninstallResult.IsError && uninstallResult.Result != null)
//                    {
//                        CLIEngine.ShowSuccessMessage("OAPP Template Successfully Uninstalled.");
//                        ShowOAPPTemplate(result.Result);
//                    }
//                    else
//                        CLIEngine.ShowErrorMessage($"Error installing OAPP Template. Reason: {uninstallResult.Message}");
//                }
//                else
//                    CLIEngine.ShowErrorMessage($"Error uninstalling OAPP Template. Reason: Unknown error occured!");
//            }
//            else
//                CLIEngine.ShowErrorMessage($"An error occured loading the OAPP Template. Reason: {result.Message}");
//        }

//        public static void UninstallOAPPTemplate(string idOrName = "", ProviderType providerType = ProviderType.Default)
//        {
//            OASISResult<IOAPPTemplate> result = LoadOAPPTemplate(idOrName, "uninstall", true, providerType);

//            if (result != null && !result.IsError && result.Result != null)
//            {
//                OASISResult<IInstalledOAPPTemplate> uninstallResult =  STAR.OASISAPI.OAPPTemplates.UninstallOAPPTemplate(STAR.BeamedInAvatar.Id, result.Result.Id, result.Result.OAPPSystemHolonDNA.Version, providerType);

//                if (uninstallResult != null)
//                {
//                    if (!uninstallResult.IsError && uninstallResult.Result != null)
//                    {
//                        CLIEngine.ShowSuccessMessage("OAPP Template Successfully Uninstalled.");
//                        ShowOAPPTemplate(result.Result);
//                    }
//                    else
//                        CLIEngine.ShowErrorMessage($"Error installing OAPP Template. Reason: {uninstallResult.Message}");
//                }
//                else
//                    CLIEngine.ShowErrorMessage($"Error uninstalling OAPP Template. Reason: Unknown error occured!");
//            }
//            else
//                CLIEngine.ShowErrorMessage($"An error occured loading the OAPP Template. Reason: {result.Message}");
//        }

//        public static async Task<OASISResult<IEnumerable<IOAPPTemplate>>> ListAllOAPPTemplatesAsync(bool showAllVersions = false, ProviderType providerType = ProviderType.Default)
//        {
//            Console.WriteLine("");
//            CLIEngine.ShowWorkingMessage("Loading OAPP Templates...");
//            return ListOAPPTemplates(await STAR.OASISAPI.OAPPTemplates.LoadAllOAPPTemplatesAsync(STAR.BeamedInAvatar.Id, OAPPTemplateType.All, showAllVersions, 0, providerType));
//        }

//        public static OASISResult<IEnumerable<IOAPPTemplate>> ListAllOAPPTemplates(bool showAllVersions = false, int version = 0, ProviderType providerType = ProviderType.Default)
//        {
//            Console.WriteLine("");
//            CLIEngine.ShowWorkingMessage("Loading OAPP Templates...");
//            return  ListOAPPTemplates(STAR.OASISAPI.OAPPTemplates.LoadAllOAPPTemplates(STAR.BeamedInAvatar.Id, OAPPTemplateType.All, showAllVersions, version, providerType));
//        }

//        public static async Task ListOAPPTemplatesCreatedByBeamedInAvatarAsync(bool showAllVersions = false, ProviderType providerType = ProviderType.Default)
//        {
//            if (STAR.BeamedInAvatar != null)
//            {
//                Console.WriteLine("");
//                CLIEngine.ShowWorkingMessage("Loading OAPP Templates...");
//                ListOAPPTemplates(await STAR.OASISAPI.OAPPTemplates.LoadAllOAPPTemplatesForAvatarAsync(STAR.BeamedInAvatar.AvatarId));
//            }
//            else
//                CLIEngine.ShowErrorMessage("No Avatar Is Beamed In. Please Beam In First!");
//        }

//        public static async Task<OASISResult<IEnumerable<IInstalledOAPPTemplate>>> ListOAPPTemplatesInstalledForBeamedInAvatarAsync(ProviderType providerType = ProviderType.Default)
//        {
//            OASISResult<IEnumerable<IInstalledOAPPTemplate>> result = new OASISResult<IEnumerable<IInstalledOAPPTemplate>>();

//            if (STAR.BeamedInAvatar != null)
//            {
//                Console.WriteLine("");
//                CLIEngine.ShowWorkingMessage("Loading Installed OAPP Templates...");
//                result = await STAR.OASISAPI.OAPPTemplates.ListInstalledOAPPTemplatesAsync(STAR.BeamedInAvatar.AvatarId);
//                ListInstalledOAPPTemplates(result);
//            }
//            else
//                OASISErrorHandling.HandleError(ref result, "No Avatar Is Beamed In. Please Beam In First!");

//            return result;
//        }

//        public static async Task<OASISResult<IEnumerable<IInstalledOAPPTemplate>>> ListOAPPTemplatesUninstalledForBeamedInAvatarAsync(ProviderType providerType = ProviderType.Default)
//        {
//            OASISResult<IEnumerable<IInstalledOAPPTemplate>> result = new OASISResult<IEnumerable<IInstalledOAPPTemplate>>();

//            if (STAR.BeamedInAvatar != null)
//            {
//                Console.WriteLine("");
//                CLIEngine.ShowWorkingMessage("Loading Uninstalled OAPP Templates...");
//                result = await STAR.OASISAPI.OAPPTemplates.ListUnInstalledOAPPTemplatesAsync(STAR.BeamedInAvatar.AvatarId);
//                ListInstalledOAPPTemplates(result, true, true);

//                if (result != null && !result.IsError && result.Result != null && result.Result.Count() > 0 && CLIEngine.GetConfirmation("Would you like to re-install any of the above?"))
//                {
//                    int number = 0;

//                    do
//                    {
//                        Console.WriteLine("");
//                        number = CLIEngine.GetValidInputForInt("What number do you wish to re-install? (It will be downloaded and installed to the previous paths)");

//                        if (number < 0 || number > result.Result.Count())
//                            CLIEngine.ShowErrorMessage($"Invalid number, it needs to be between 1 and {result.Result.Count()}");
//                    }
//                    while (number < 0 || number > result.Result.Count());

//                    if (number > 0)
//                    {
//                        IInstalledOAPPTemplate template = result.Result.ElementAt(number - 1);
    
//                        //if (template != null && template.MetaData != null && template.MetaData.ContainsKey("OAPPTemplateId") && template.MetaData.ContainsKey("OAPPTemplateId") != null && Guid.TryParse(template.MetaData.ContainsKey("OAPPTemplateId").ToString(), out id))
//                        if (template != null)
//                        {
//                            if (!_init)
//                                Init();

//                            OASISResult<IInstalledOAPPTemplate> installResult = await DownloadAndInstallOAPPTemplateAsync(template.OAPPSystemHolonDNA.Id.ToString(), InstallMode.DownloadAndReInstall, providerType);

//                            if (installResult != null && !installResult.IsError && installResult.Result != null)
//                            {
//                                ShowInstalledOAPPTemplate(installResult.Result);
//                                CLIEngine.ShowSuccessMessage("OAPP Template Successfully Re-Installed");
//                            }
//                            else
//                                CLIEngine.ShowErrorMessage($"An error occured re-installing the OAPP Template. Reason: {installResult.Message}");
//                        }
//                        else
//                            CLIEngine.ShowErrorMessage($"An error occured re-installing the OAPP Template. Reason: OAPPTemplateId not found in the metadata!");
//                    }
//                }
//                else
//                    Console.WriteLine("");
//            }
//            else
//                OASISErrorHandling.HandleError(ref result, "No Avatar Is Beamed In. Please Beam In First!");

//            return result;
//        }

//        public static async Task<OASISResult<IEnumerable<IOAPPTemplate>>> ListOAPPTemplatesUnpublishedForBeamedInAvatarAsync(ProviderType providerType = ProviderType.Default)
//        {
//            OASISResult<IEnumerable<IOAPPTemplate>> result = new OASISResult<IEnumerable<IOAPPTemplate>>();

//            if (STAR.BeamedInAvatar != null)
//            {
//                Console.WriteLine("");
//                CLIEngine.ShowWorkingMessage("Loading Unpublished OAPP Templates...");
//                result = await STAR.OASISAPI.OAPPTemplates.ListUnpublishedOAPPTemplatesAsync(STAR.BeamedInAvatar.AvatarId);
//                ListOAPPTemplates(result, true);

//                if (result != null && !result.IsError && result.Result != null && result.Result.Count() > 0 && CLIEngine.GetConfirmation("Would you like to republish any of the above?"))
//                {
//                    int number = 0;

//                    do
//                    {
//                        Console.WriteLine("");
//                        number = CLIEngine.GetValidInputForInt("What number do you wish to republish?");

//                        if (number < 0 || number > result.Result.Count())
//                            CLIEngine.ShowErrorMessage($"Invalid number, it needs to be between 1 and {result.Result.Count()}");
//                    }
//                    while (number < 0 || number > result.Result.Count());

//                    if (number > 0)
//                    {
//                        IOAPPTemplate template = result.Result.ElementAt(number - 1);
//                        Guid id = Guid.Empty;

//                        if (template != null)
//                        {
//                            OASISResult<IOAPPTemplate> republishResult = await STAR.OASISAPI.OAPPTemplates.RepublishOAPPTemplateAsync(STAR.BeamedInAvatar.Id, template, providerType);

//                            if (republishResult != null && !republishResult.IsError && republishResult.Result != null)
//                            {
//                                ShowOAPPTemplate(republishResult.Result);
//                                CLIEngine.ShowSuccessMessage("OAPP Template Successfully Republished");
//                            }
//                            else
//                                CLIEngine.ShowErrorMessage($"An error occured republishing the OAPP Template. Reason: {republishResult.Message}");
//                        }
//                    }
//                }
//                else
//                    Console.WriteLine("");
//            }
//            else
//                OASISErrorHandling.HandleError(ref result, "No Avatar Is Beamed In. Please Beam In First!");

//            return result;
//        }

//        public static async Task<OASISResult<IEnumerable<IOAPPTemplate>>> ListOAPPTemplatesDeactivatedForBeamedInAvatarAsync(ProviderType providerType = ProviderType.Default)
//        {
//            OASISResult<IEnumerable<IOAPPTemplate>> result = new OASISResult<IEnumerable<IOAPPTemplate>>();

//            if (STAR.BeamedInAvatar != null)
//            {
//                Console.WriteLine("");
//                CLIEngine.ShowWorkingMessage("Loading Deactivated OAPP Templates...");
//                result = await STAR.OASISAPI.OAPPTemplates.ListDeactivatedOAPPTemplatesAsync(STAR.BeamedInAvatar.AvatarId);
//                ListOAPPTemplates(result, true);

//                if (result != null && !result.IsError && result.Result != null && result.Result.Count() > 0 && CLIEngine.GetConfirmation("Would you like to reactivate any of the above?"))
//                {
//                    int number = 0;

//                    do
//                    {
//                        Console.WriteLine("");
//                        number = CLIEngine.GetValidInputForInt("What number do you wish to reactivate?");

//                        if (number < 0 || number > result.Result.Count())
//                            CLIEngine.ShowErrorMessage($"Invalid number, it needs to be between 1 and {result.Result.Count()}");
//                    }
//                    while (number < 0 || number > result.Result.Count());

//                    if (number > 0)
//                    {
//                        IOAPPTemplate template = result.Result.ElementAt(number - 1);
//                        Guid id = Guid.Empty;

//                        if (template != null)
//                        {
//                            OASISResult<IOAPPTemplate> activateResult = await STAR.OASISAPI.OAPPTemplates.ActivateOAPPTemplateAsync(STAR.BeamedInAvatar.Id, template, providerType);

//                            if (activateResult != null && !activateResult.IsError && activateResult.Result != null)
//                            {
//                                ShowOAPPTemplate(activateResult.Result);
//                                CLIEngine.ShowSuccessMessage("OAPP Template Successfully Reactivated");
//                            }
//                            else
//                                CLIEngine.ShowErrorMessage($"An error occured reactivating the OAPP Template. Reason: {activateResult.Message}");
//                        }
//                    }
//                }
//                else
//                    Console.WriteLine("");
//            }
//            else
//                OASISErrorHandling.HandleError(ref result, "No Avatar Is Beamed In. Please Beam In First!");

//            return result;
//        }

//        public static async Task SearchOAPPTemplatesAsync(string searchTerm = "", bool showAllVersions = false, bool showForAllAvatars = true, ProviderType providerType = ProviderType.Default)
//        {            
//            if (string.IsNullOrEmpty(searchTerm) || searchTerm == "forallavatars" || searchTerm == "forallavatars")
//            { 
//                //Console.WriteLine("");
//                searchTerm = CLIEngine.GetValidInput("What is the name of the OAPP Template you wish to search for?");
//            }

//            Console.WriteLine("");
//            CLIEngine.ShowWorkingMessage("Searching OAPP Templates...");
//            ListOAPPTemplates(await STAR.OASISAPI.OAPPTemplates.SearchOAPPTemplatesAsync(STAR.BeamedInAvatar.Id, searchTerm, !showForAllAvatars, showAllVersions, 0, providerType));
//        }

//        public static async Task ShowOAPPTemplateAsync(string idOrName = "", ProviderType providerType = ProviderType.Default)
//        {
//            OASISResult<IOAPPTemplate> result = await LoadOAPPTemplateAsync(idOrName, "view", true, providerType);

//            if (result != null && !result.IsError && result.Result != null)
//                ShowOAPPTemplate(result.Result);
//            else
//                CLIEngine.ShowErrorMessage($"An error occured loading the OAPP Template. Reason: {result.Message}");
//        }

//        public static void ShowOAPPTemplate(IOAPPTemplate oappTemplate, bool showHeader = true, bool showFooter = true, bool showNumbers = false, int number = 0, bool showDetailedInfo = false)
//        {
//            if (showHeader)
//                CLIEngine.ShowDivider();

//            if (showNumbers)
//                CLIEngine.ShowMessage(string.Concat("Number:                                     ", number));

//            CLIEngine.ShowMessage(string.Concat($"Id:                                         ", oappTemplate.OAPPSystemHolonDNA.Id != Guid.Empty ? oappTemplate.OAPPSystemHolonDNA.Id : "None"));
//            CLIEngine.ShowMessage(string.Concat($"Name:                                       ", !string.IsNullOrEmpty(oappTemplate.OAPPSystemHolonDNA.Name) ? oappTemplate.OAPPSystemHolonDNA.Name : "None"));
//            CLIEngine.ShowMessage(string.Concat($"Description:                                ", !string.IsNullOrEmpty(oappTemplate.OAPPSystemHolonDNA.Description) ? oappTemplate.OAPPSystemHolonDNA.Description : "None"));
//            CLIEngine.ShowMessage(string.Concat($"OAPP Template Type:                         ", oappTemplate.OAPPSystemHolonDNA.OAPPSystemHolonType));
//            CLIEngine.ShowMessage(string.Concat($"Created On:                                 ", oappTemplate.OAPPSystemHolonDNA.CreatedOn != DateTime.MinValue ? oappTemplate.OAPPSystemHolonDNA.CreatedOn.ToString() : "None"));
//            CLIEngine.ShowMessage(string.Concat($"Created By:                                 ", oappTemplate.OAPPSystemHolonDNA.CreatedByAvatarId != Guid.Empty ? string.Concat(oappTemplate.OAPPSystemHolonDNA.CreatedByAvatarUsername, " (", oappTemplate.OAPPSystemHolonDNA.CreatedByAvatarId.ToString(), ")") : "None"));
//            CLIEngine.ShowMessage(string.Concat($"Modified On:                                ", oappTemplate.OAPPSystemHolonDNA.ModifiedOn != DateTime.MinValue ? oappTemplate.OAPPSystemHolonDNA.CreatedOn.ToString() : "None"));
//            CLIEngine.ShowMessage(string.Concat($"Modified By:                                ", oappTemplate.OAPPSystemHolonDNA.ModifiedByAvatarId != Guid.Empty ? string.Concat(oappTemplate.OAPPSystemHolonDNA.ModifiedByAvatarUsername, " (", oappTemplate.OAPPSystemHolonDNA.ModifiedByAvatarId.ToString(), ")") : "None"));
//            CLIEngine.ShowMessage(string.Concat($"OAPP Template Path:                         ", !string.IsNullOrEmpty(oappTemplate.OAPPSystemHolonDNA.SourcePath) ? oappTemplate.OAPPSystemHolonDNA.SourcePath : "None"));
//            CLIEngine.ShowMessage(string.Concat($"Published On:                               ", oappTemplate.OAPPSystemHolonDNA.PublishedOn != DateTime.MinValue ? oappTemplate.OAPPSystemHolonDNA.PublishedOn.ToString() : "None"));
//            CLIEngine.ShowMessage(string.Concat($"Published By:                               ", oappTemplate.OAPPSystemHolonDNA.PublishedByAvatarId != Guid.Empty ? string.Concat(oappTemplate.OAPPSystemHolonDNA.PublishedByAvatarUsername, " (", oappTemplate.OAPPSystemHolonDNA.PublishedByAvatarId.ToString(), ")") : "None"));
//            CLIEngine.ShowMessage(string.Concat($"OAPP Template Published Path:               ", !string.IsNullOrEmpty(oappTemplate.OAPPSystemHolonDNA.PublishedPath) ? oappTemplate.OAPPSystemHolonDNA.PublishedPath : "None"));
//            CLIEngine.ShowMessage(string.Concat($"OAPP Template Filesize:                     ", oappTemplate.OAPPSystemHolonDNA.FileSize.ToString()));
//            CLIEngine.ShowMessage(string.Concat($"OAPP Template Published On STARNET:         ", oappTemplate.OAPPSystemHolonDNA.PublishedOnSTARNET ? "True" : "False"));
//            CLIEngine.ShowMessage(string.Concat($"OAPP Template Published To Cloud:           ", oappTemplate.OAPPSystemHolonDNA.PublishedToCloud ? "True" : "False"));
//            CLIEngine.ShowMessage(string.Concat($"OAPP Template Published To OASIS Provider:  ", Enum.GetName(typeof(ProviderType), oappTemplate.OAPPSystemHolonDNA.PublishedProviderType)));
//            CLIEngine.ShowMessage(string.Concat($"Launch Target:                              ", oappTemplate.OAPPSystemHolonDNA.LaunchTarget));
//            CLIEngine.ShowMessage(string.Concat($"OAPP Template Version:                      ", oappTemplate.OAPPSystemHolonDNA.Version));
//            CLIEngine.ShowMessage(string.Concat($"Version Sequence:                           ", oappTemplate.OAPPSystemHolonDNA.VersionSequence));
//            CLIEngine.ShowMessage(string.Concat($"Number Of Versions:                         ", oappTemplate.OAPPSystemHolonDNA.NumberOfVersions));
//            //CLIEngine.ShowMessage(string.Concat($"OASIS Holon Version:                        ", oappTemplate.Version));
//            //CLIEngine.ShowMessage(string.Concat($"OASIS Holon VersionId:                      ", oappTemplate.VersionId));
//            //CLIEngine.ShowMessage(string.Concat($"OASIS Holon PreviousVersionId:              ", oappTemplate.PreviousVersionId));
//            CLIEngine.ShowMessage(string.Concat($"Downloads:                                  ", oappTemplate.OAPPSystemHolonDNA.Downloads));
//            CLIEngine.ShowMessage(string.Concat($"Installs:                                   ", oappTemplate.OAPPSystemHolonDNA.Installs));
//            CLIEngine.ShowMessage(string.Concat($"Total Downloads:                            ", oappTemplate.OAPPSystemHolonDNA.TotalDownloads));
//            CLIEngine.ShowMessage(string.Concat($"Total Installs:                             ", oappTemplate.OAPPSystemHolonDNA.TotalInstalls));
//            CLIEngine.ShowMessage(string.Concat($"Active:                                     ", oappTemplate.MetaData != null && oappTemplate.MetaData.ContainsKey("Active") && oappTemplate.MetaData["Active"] != null && oappTemplate.MetaData["Active"].ToString() == "1" ? "True" : "False"));

//            if (showDetailedInfo)
//            {
//                //Show base holon info.
//            }

//            if (showFooter)
//                CLIEngine.ShowDivider();
//        }

//        public static void ShowInstalledOAPPTemplate(IInstalledOAPPTemplate oappTemplate, bool showHeader = true, bool showFooter = true, bool showNumbers = false, int number = 0, bool showUninstallInfo = false, bool showDetailedInfo = false)
//        {
//            ShowOAPPTemplate(oappTemplate, showHeader, false, showNumbers, number, showDetailedInfo);
//            CLIEngine.ShowMessage(string.Concat($"Downloaded On:                              ", oappTemplate.DownloadedOn != DateTime.MinValue ? oappTemplate.DownloadedOn.ToString() : "None"));
//            CLIEngine.ShowMessage(string.Concat($"Downloaded By:                              ", oappTemplate.DownloadedBy != Guid.Empty ? string.Concat(oappTemplate.DownloadedByAvatarUsername, " (", oappTemplate.DownloadedBy.ToString(), ")") : "None"));
//            CLIEngine.ShowMessage(string.Concat($"Downloaded Path:                            ", !string.IsNullOrEmpty(oappTemplate.DownloadedPath) ? oappTemplate.DownloadedPath : "None"));
//            CLIEngine.ShowMessage(string.Concat($"Installed On:                               ", oappTemplate.InstalledOn != DateTime.MinValue ? oappTemplate.InstalledOn.ToString() : "None"));
//            CLIEngine.ShowMessage(string.Concat($"Installed By:                               ", oappTemplate.InstalledBy != Guid.Empty ? string.Concat(oappTemplate.InstalledByAvatarUsername, " (", oappTemplate.InstalledBy.ToString(), ")") : "None"));
//            CLIEngine.ShowMessage(string.Concat($"Installed Path:                             ", !string.IsNullOrEmpty(oappTemplate.InstalledPath) ? oappTemplate.InstalledPath : "None"));

//            if (showUninstallInfo)
//            {
//                CLIEngine.ShowMessage(string.Concat($"Uninstalled On:                             ", oappTemplate.UninstalledOn != DateTime.MinValue ? oappTemplate.UninstalledOn.ToString() : "None"));
//                CLIEngine.ShowMessage(string.Concat($"Uninstalled By:                             ", oappTemplate.UninstalledBy != Guid.Empty ? string.Concat(oappTemplate.UninstalledByAvatarUsername, " (", oappTemplate.UninstalledBy.ToString(), ")") : "None"));
//            }

//            if (showFooter)
//                CLIEngine.ShowDivider();
//        }

//        private static OASISResult<IEnumerable<IOAPPTemplate>> ListOAPPTemplates(OASISResult<IEnumerable<IOAPPTemplate>> oappTemplates, bool showNumbers = false)
//        {
//            if (oappTemplates != null)
//            {
//                if (!oappTemplates.IsError)
//                {
//                    if (oappTemplates.Result != null && oappTemplates.Result.Count() > 0)
//                    {
//                        Console.WriteLine();

//                        if (oappTemplates.Result.Count() == 1)
//                            CLIEngine.ShowMessage($"{oappTemplates.Result.Count()} OAPP Template Found:");
//                        else
//                            CLIEngine.ShowMessage($"{oappTemplates.Result.Count()} OAPP Templates Found:");

//                        for (int i = 0; i < oappTemplates.Result.Count(); i++)
//                            ShowOAPPTemplate(oappTemplates.Result.ElementAt(i), i==0, true, showNumbers, i + 1);
//                    }
//                    else
//                        CLIEngine.ShowWarningMessage("No OAPP Templates Found.");
//                }
//                else
//                    CLIEngine.ShowErrorMessage($"Error occured loading OAPP Templates. Reason: {oappTemplates.Message}");
//            }
//            else
//                CLIEngine.ShowErrorMessage($"Unknown error occured loading OAPP Templates.");

//            return oappTemplates;
//        }

//        private static void ListInstalledOAPPTemplates(OASISResult<IEnumerable<IInstalledOAPPTemplate>> oappTemplates, bool showNumbers = false, bool showUninstallInfo = false)
//        {
//            if (oappTemplates != null)
//            {
//                if (!oappTemplates.IsError)
//                {
//                    if (oappTemplates.Result != null && oappTemplates.Result.Count() > 0)
//                    {
//                        Console.WriteLine();

//                        if (oappTemplates.Result.Count() == 1)
//                            CLIEngine.ShowMessage($"{oappTemplates.Result.Count()} OAPP Template Found:");
//                        else
//                            CLIEngine.ShowMessage($"{oappTemplates.Result.Count()} OAPP Templates Found:");

//                        for (int i = 0; i < oappTemplates.Result.Count(); i++)
//                            ShowInstalledOAPPTemplate(oappTemplates.Result.ElementAt(i), i == 0, true, showNumbers, i + 1, showUninstallInfo);
//                    }
//                    else
//                        CLIEngine.ShowWarningMessage("No OAPP Templates Found.");
//                }
//                else
//                    CLIEngine.ShowErrorMessage($"Error occured loading OAPP Templates. Reason: {oappTemplates.Message}");
//            }
//            else
//                CLIEngine.ShowErrorMessage($"Unknown error occured loading OAPP Templates.");
//        }

//        private static async Task<OASISResult<IOAPPTemplate>> LoadOAPPTemplateForProviderAsync(string idOrName, string operationName, bool showOnlyForCurrentAvatar, ProviderType providerType, bool addSpace = true, bool simpleWizard = true)
//        {
//            OASISResult<IOAPPTemplate> result = new OASISResult<IOAPPTemplate>();
//            ProviderType largeFileProviderType = ProviderType.IPFSOASIS;

//            if (!simpleWizard)
//            {
//                if (!CLIEngine.GetConfirmation("Do you wish to download from the cloud or from the OASIS? Press 'Y' for the cloud or N' for the OASIS."))
//                {
//                    Console.WriteLine("");
//                    object largeProviderTypeObject = CLIEngine.GetValidInputForEnum("What OASIS provider do you wish to install the OAPP Template from? (The default is IPFSOASIS)", typeof(ProviderType));

//                    if (largeProviderTypeObject != null)
//                    {
//                        largeFileProviderType = (ProviderType)largeProviderTypeObject;
//                        result = await LoadOAPPTemplateAsync(idOrName, operationName, showOnlyForCurrentAvatar, largeFileProviderType, addSpace);
//                    }
//                    else
//                        OASISErrorHandling.HandleError(ref result, "Error occured in LoadOAPPTemplateForProviderAsync, reason: largeProviderTypeObject is null!");
//                }
//                else
//                {
//                    Console.WriteLine("");
//                    result = await LoadOAPPTemplateAsync(idOrName, operationName, showOnlyForCurrentAvatar, providerType, addSpace);
//                }
//            }
//            else
//                result = await LoadOAPPTemplateAsync(idOrName, operationName, showOnlyForCurrentAvatar, providerType, addSpace);

//            return result;
//        }

//        private static OASISResult<IOAPPTemplate> LoadOAPPTemplateForProvider(string idOrName, string operationName, bool showOnlyForCurrentAvatar, ProviderType providerType, bool addSpace = true, bool simpleWizard = true)
//        {
//            OASISResult<IOAPPTemplate> result = new OASISResult<IOAPPTemplate>();
//            ProviderType largeFileProviderType = ProviderType.IPFSOASIS;

//            if (!simpleWizard)
//            {
//                if (!CLIEngine.GetConfirmation("Do you wish to download from the cloud or from the OASIS? Press 'Y' for the cloud or N' for the OASIS."))
//                {
//                    Console.WriteLine("");
//                    object largeProviderTypeObject = CLIEngine.GetValidInputForEnum("What OASIS provider do you wish to install the OAPP Template from? (The default is IPFSOASIS)", typeof(ProviderType));

//                    if (largeProviderTypeObject != null)
//                    {
//                        largeFileProviderType = (ProviderType)largeProviderTypeObject;
//                        result = LoadOAPPTemplate(idOrName, operationName, showOnlyForCurrentAvatar, largeFileProviderType, addSpace);
//                    }
//                    else
//                        OASISErrorHandling.HandleError(ref result, "Error occured in LoadOAPPTemplateForProvider, reason: largeProviderTypeObject is null!");
//                }
//                else
//                {
//                    Console.WriteLine("");
//                    result = LoadOAPPTemplate(idOrName, operationName, showOnlyForCurrentAvatar, providerType, addSpace);
//                }
//            }
//            else
//                result = LoadOAPPTemplate(idOrName, operationName, showOnlyForCurrentAvatar, providerType, addSpace);

//            return result;
//        }

//        private static async Task<OASISResult<IOAPPTemplate>> LoadOAPPTemplateAsync(string idOrName, string operationName, bool showOnlyForCurrentAvatar, ProviderType providerType = ProviderType.Default, bool addSpace = true)
//        {
//            OASISResult<IOAPPTemplate> result = new OASISResult<IOAPPTemplate>();
//            Guid id = Guid.Empty;

//            do
//            {
//                if (string.IsNullOrEmpty(idOrName))
//                {
//                    if (!CLIEngine.GetConfirmation($"Do you know the GUID/ID or Name of the OAPP Template you wish to {operationName}? Press 'Y' for Yes or 'N' for No."))
//                    {
//                        Console.WriteLine("");
//                        CLIEngine.ShowWorkingMessage("Loading OAPP Templates...");
                        
//                        if (showOnlyForCurrentAvatar)
//                            ListOAPPTemplates(await STAR.OASISAPI.OAPPTemplates.LoadAllOAPPTemplatesForAvatarAsync(STAR.BeamedInAvatar.AvatarId));
//                        else
//                            ListOAPPTemplates(await STAR.OASISAPI.OAPPTemplates.LoadAllOAPPTemplatesAsync(STAR.BeamedInAvatar.AvatarId, OAPPTemplateType.All, false, 0, providerType));
//                    }
//                    else
//                        Console.WriteLine("");

//                    idOrName = CLIEngine.GetValidInput($"What is the GUID/ID or Name of the OAPP Template you wish to {operationName}?");

//                    if (idOrName == "exit")
//                        break;
//                }

//                if (addSpace)
//                    Console.WriteLine("");

//                if (Guid.TryParse(idOrName, out id))
//                {
//                    CLIEngine.ShowWorkingMessage("Loading OAPP Template...");
//                    result = await STAR.OASISAPI.OAPPTemplates.LoadOAPPTemplateAsync(STAR.BeamedInAvatar.AvatarId, id, 0, providerType);

//                    if (result != null && result.Result != null && !result.IsError && showOnlyForCurrentAvatar && result.Result.OAPPSystemHolonDNA.CreatedByAvatarId != STAR.BeamedInAvatar.AvatarId)
//                    {
//                        CLIEngine.ShowErrorMessage($"You do not have permission to {operationName} this OAPP Template. It was created by another avatar.");
//                        result.Result = null;
//                    }
//                }
//                else
//                {
//                    CLIEngine.ShowWorkingMessage("Searching OAPP Templates...");
//                    OASISResult<IEnumerable<IOAPPTemplate>> searchResults = await STAR.OASISAPI.OAPPTemplates.SearchOAPPTemplatesAsync(STAR.BeamedInAvatar.Id, idOrName, showOnlyForCurrentAvatar, false, 0, providerType);

//                    if (searchResults != null && searchResults.Result != null && !searchResults.IsError)
//                    {
//                        if (searchResults.Result.Count() > 1)
//                        {
//                            ListOAPPTemplates(searchResults, true);

//                            do
//                            {
//                                int number = CLIEngine.GetValidInputForInt($"What is the number of the OAPP Template you wish to {operationName}?");

//                                if (number > 0 && number <= searchResults.Result.Count())
//                                    result.Result = searchResults.Result.ElementAt(number - 1);                                 
//                                else
//                                    CLIEngine.ShowErrorMessage("Invalid number entered. Please try again.");

//                            } while (result.Result == null || result.IsError);
//                        }
//                        else if (searchResults.Result.Count() == 1)
//                            result.Result = searchResults.Result.FirstOrDefault();
//                        else
//                        {
//                            idOrName = "";
//                            CLIEngine.ShowWarningMessage("No OAPP Template Found!");
//                        }
//                    }
//                    else
//                        CLIEngine.ShowErrorMessage($"An error occured calling STAR.OASISAPI.OAPPTemplates.SearchOAPPTemplatesAsync. Reason: {searchResults.Message}");
//                }

//                if (result.Result != null && result.Result.OAPPSystemHolonDNA != null)
//                {
//                    ShowOAPPTemplate(result.Result);

//                    if (result.Result.OAPPSystemHolonDNA.NumberOfVersions > 1)
//                    {
//                        if ((operationName == "view" && CLIEngine.GetConfirmation($"{result.Result.OAPPSystemHolonDNA.NumberOfVersions} versions were found. Do you wish to view the other versions?")) ||
//                            (!CLIEngine.GetConfirmation($"{result.Result.OAPPSystemHolonDNA.NumberOfVersions} versions were found. Do you wish to {operationName} the latest version ({result.Result.OAPPSystemHolonDNA.Version})?")))
//                        {
//                            Console.WriteLine("");
//                            CLIEngine.ShowWorkingMessage("Loading OAPP Template Versions...");
//                            OASISResult<IEnumerable<IOAPPTemplate>> versionsResult = await STAR.OASISAPI.OAPPTemplates.LoadOAPPTemplateVersionsAsync(result.Result.OAPPSystemHolonDNA.Id, providerType);
//                            ListOAPPTemplates(versionsResult);

//                            if (operationName != "view" && versionsResult != null && versionsResult.Result != null && !versionsResult.IsError && versionsResult.Result.Count() > 0)
//                            {
//                                bool versionSelected = false;

//                                do
//                                {
//                                    int version = CLIEngine.GetValidInputForInt($"Which version do you wish to {operationName}? (Enter the Version Sequence that corresponds to the relevant template)");

//                                    if (version > 0 && version <= versionsResult.Result.Count())
//                                    {
//                                        versionSelected = true;
//                                        result.Result = versionsResult.Result.ElementAt(version - 1);
//                                    }
//                                    else
//                                        CLIEngine.ShowErrorMessage("Invalid version entered. Please try again.");

//                                    if (version == 0)
//                                        break;

//                                } while (!versionSelected);
//                            }
//                        }
//                        else
//                            Console.WriteLine("");

//                        if (operationName != "view")
//                            ShowOAPPTemplate(result.Result);
//                    }
//                }

//                if (idOrName == "exit")
//                    break;

//                if (result.Result != null && operationName != "view")
//                {
//                    if (CLIEngine.GetConfirmation($"Please confirm you wish to {operationName} this OAPP Template?"))
//                    {
//                        if (operationName == "install")
//                        {
//                            if (result != null && result.Result != null)
//                            {
//                                OASISResult<IOAPPTemplate> checkResult = await CheckIfAlreadyInstalledAsync(result.Result, providerType);

//                                if (checkResult != null && checkResult.Result != null && !checkResult.IsError)
//                                {
//                                    if (result.MetaData != null && result.MetaData.ContainsKey("Reinstall"))
//                                        result.MetaData["Reinstall"] = checkResult.MetaData["Reinstall"];
//                                }
//                                else
//                                    result.Result = null;
//                            }
//                            else
//                            {
//                                CLIEngine.ShowErrorMessage($"Error occured checking if the OAPP Template is already installed! Reason: OAPPTemplateId was not found in the metadata!");
//                                result.Result = null;
//                            }
//                        }
//                    }
//                    else
//                    {
//                        Console.WriteLine("");
//                        result.Result = null;

//                        if (!CLIEngine.GetConfirmation("Do you wish to search for another OAPP Template?"))
//                        {
//                            idOrName = "exit";
//                            break;
//                        }
//                    }

//                    Console.WriteLine("");
//                }
//            }
//            while (result.Result == null || result.IsError);

//            if (idOrName == "exit")
//            {
//                result.IsError = true;
//                result.Message = "User Exited";
//            }

//            return result;
//        }

//        private static OASISResult<IOAPPTemplate> LoadOAPPTemplate(string idOrName, string operationName, bool showOnlyForCurrentAvatar, ProviderType providerType = ProviderType.Default, bool addSpace = true)
//        {
//            OASISResult<IOAPPTemplate> result = new OASISResult<IOAPPTemplate>();
//            Guid id = Guid.Empty;
//            bool reInstall = false;

//            do
//            {
//                if (string.IsNullOrEmpty(idOrName))
//                {
//                    if (!CLIEngine.GetConfirmation($"Do you know the GUID/ID or Name of the OAPP Template you wish to {operationName}? Press 'Y' for Yes or 'N' for No."))
//                    {
//                        Console.WriteLine("");
//                        CLIEngine.ShowWorkingMessage("Loading OAPP Templates...");

//                        if (showOnlyForCurrentAvatar)
//                            ListOAPPTemplates(STAR.OASISAPI.OAPPTemplates.LoadAllOAPPTemplatesForAvatar(STAR.BeamedInAvatar.AvatarId));
//                        else
//                            ListOAPPTemplates(STAR.OASISAPI.OAPPTemplates.LoadAllOAPPTemplates(STAR.BeamedInAvatar.AvatarId, OAPPTemplateType.All, false, 0, providerType));
//                    }
//                    else
//                        Console.WriteLine("");

//                    idOrName = CLIEngine.GetValidInput($"What is the GUID/ID or Name of the OAPP Template you wish to {operationName}?");

//                    if (idOrName == "exit")
//                        break;
//                }

//                if (addSpace)
//                    Console.WriteLine("");

//                if (Guid.TryParse(idOrName, out id))
//                {
//                    CLIEngine.ShowWorkingMessage("Loading OAPP Template...");
//                    result = STAR.OASISAPI.OAPPTemplates.LoadOAPPTemplate(STAR.BeamedInAvatar.Id, id, 0, providerType);

//                    if (result != null && result.Result != null && !result.IsError && showOnlyForCurrentAvatar && result.Result.OAPPSystemHolonDNA.CreatedByAvatarId != STAR.BeamedInAvatar.AvatarId)
//                    {
//                        CLIEngine.ShowErrorMessage($"You do not have permission to {operationName} this OAPP Template. It was created by another avatar.");
//                        result.Result = null;
//                    }
//                }
//                else
//                {
//                    CLIEngine.ShowWorkingMessage("Searching OAPP Templates...");
//                    OASISResult<IEnumerable<IOAPPTemplate>> searchResults = STAR.OASISAPI.OAPPTemplates.SearchOAPPTemplates(STAR.BeamedInAvatar.Id, idOrName, showOnlyForCurrentAvatar, false, 0, providerType);

//                    if (searchResults != null && searchResults.Result != null && !searchResults.IsError)
//                    {
//                        if (searchResults.Result.Count() > 1)
//                        {
//                            ListOAPPTemplates(searchResults, true);

//                            do
//                            {
//                                int number = CLIEngine.GetValidInputForInt($"What is the number of the OAPP Template you wish to {operationName}?");

//                                if (number > 0 && number <= searchResults.Result.Count())
//                                    result.Result = searchResults.Result.ElementAt(number - 1);
//                                else
//                                    CLIEngine.ShowErrorMessage("Invalid number entered. Please try again.");

//                            } while (result.Result == null || result.IsError);
//                        }
//                        else if (searchResults.Result.Count() == 1)
//                            result.Result = searchResults.Result.FirstOrDefault();
//                        else
//                        {
//                            idOrName = "";
//                            CLIEngine.ShowWarningMessage("No OAPP Template Found!");
//                        }
//                    }
//                    else
//                        CLIEngine.ShowErrorMessage($"An error occured calling STAR.OASISAPI.OAPPTemplates.SearchOAPPTemplatesAsync. Reason: {searchResults.Message}");
//                }

//                if (result.Result != null && result.Result.OAPPSystemHolonDNA != null)
//                {
//                    ShowOAPPTemplate(result.Result);

//                    if (result.Result.OAPPSystemHolonDNA.NumberOfVersions > 1)
//                    {
//                        if ((operationName == "view" && CLIEngine.GetConfirmation($"{result.Result.OAPPSystemHolonDNA.NumberOfVersions} versions were found. Do you wish to view the other versions?")) ||
//                            (!CLIEngine.GetConfirmation($"{result.Result.OAPPSystemHolonDNA.NumberOfVersions} versions were found. Do you wish to {operationName} the latest version ({result.Result.OAPPSystemHolonDNA.Version})?")))
//                        {
//                            Console.WriteLine("");
//                            CLIEngine.ShowWorkingMessage("Loading OAPP Template Versions...");
//                            OASISResult<IEnumerable<IOAPPTemplate>> versionsResult = STAR.OASISAPI.OAPPTemplates.LoadOAPPTemplateVersions(result.Result.OAPPSystemHolonDNA.Id, providerType);
//                            ListOAPPTemplates(versionsResult);

//                            if (operationName != "view" && versionsResult != null && versionsResult.Result != null && !versionsResult.IsError && versionsResult.Result.Count() > 0)
//                            {
//                                bool versionSelected = false;

//                                do
//                                {
//                                    int version = CLIEngine.GetValidInputForInt($"Which version do you wish to {operationName}? (Enter the Version Sequence that corresponds to the relevant template)");

//                                    if (version > 0 && version <= versionsResult.Result.Count())
//                                    {
//                                        versionSelected = true;
//                                        result.Result = versionsResult.Result.ElementAt(version - 1);
//                                    }
//                                    else
//                                        CLIEngine.ShowErrorMessage("Invalid version entered. Please try again.");

//                                    if (version == 0)
//                                        break;

//                                } while (!versionSelected);
//                            }
//                        }
//                        else
//                            Console.WriteLine("");

//                        if (operationName != "view")
//                            ShowOAPPTemplate(result.Result);
//                    }
//                }

//                if (idOrName == "exit")
//                    break;

//                if (result.Result != null && operationName != "view")
//                {
//                    if (CLIEngine.GetConfirmation($"Please confirm you wish to {operationName} this OAPP Template?"))
//                    {
//                        if (operationName == "install")
//                        {
//                            if (result != null && result.Result != null)
//                            {
//                                OASISResult<IOAPPTemplate> checkResult = CheckIfAlreadyInstalled(result.Result, providerType);

//                                if (checkResult != null && checkResult.Result != null && !checkResult.IsError)
//                                {
//                                    if (result.MetaData != null && result.MetaData.ContainsKey("Reinstall"))
//                                        result.MetaData["Reinstall"] = checkResult.MetaData["Reinstall"];
//                                }
//                                else
//                                    result.Result = null;
//                            }
//                            else
//                            {
//                                CLIEngine.ShowErrorMessage($"Error occured checking if the OAPP Template is already installed! Reason: OAPPTemplateId was not found in the metadata!");
//                                result.Result = null;
//                            }
//                        }

//                    }
//                    else
//                    {
//                        if (CLIEngine.GetConfirmation("Do you wish to search for another OAPP Template?"))
//                            result.Result = null;
//                        else
//                            break;
//                    }

//                    Console.WriteLine("");
//                }

//            }
//            while (result.Result == null || result.IsError);

//            if (idOrName == "exit")
//            {
//                result.IsError = true;
//                result.Message = "User Exited";
//            }

//            return result;
//        }

//        private static async Task<OASISResult<IOAPPTemplate>> CheckIfAlreadyInstalledAsync(IOAPPTemplate OAPPTemplate, ProviderType providerType = ProviderType.Default)
//        {
//            OASISResult<IOAPPTemplate> result = new OASISResult<IOAPPTemplate>(OAPPTemplate);
//            OASISResult<bool> oappInstalledResult = await STAR.OASISAPI.OAPPTemplates.IsOAPPTemplateInstalledAsync(STAR.BeamedInAvatar.Id, OAPPTemplate.OAPPSystemHolonDNA.Id, OAPPTemplate.OAPPSystemHolonDNA.Version, providerType);

//            if (oappInstalledResult != null && !oappInstalledResult.IsError)
//            {
//                if (oappInstalledResult.Result)
//                {
//                    Console.WriteLine("");
//                    CLIEngine.ShowWarningMessage($"You have already installed this version (v{OAPPTemplate.OAPPSystemHolonDNA.Version}). Please uninstall before attempting to re-install.");

//                    if (CLIEngine.GetConfirmation("Do you wish to uninstall the OAPP Template now? Press 'Y' for Yes or 'N' for No."))
//                    {
//                        Console.WriteLine("");
//                        CLIEngine.ShowWorkingMessage("Uninstalling OAPP Template...");
//                        OASISResult<IInstalledOAPPTemplate> uninstallResult = await STAR.OASISAPI.OAPPTemplates.UninstallOAPPTemplateAsync(STAR.BeamedInAvatar.Id, result.Result.OAPPSystemHolonDNA.Id, result.Result.OAPPSystemHolonDNA.Version, providerType);

//                        if (uninstallResult != null && uninstallResult.Result != null && !uninstallResult.IsError)
//                        {
//                            CLIEngine.ShowSuccessMessage("OAPP Template Successfully Uninstalled.");
//                            result.MetaData["Reinstall"] = "1";
//                        }
//                        else
//                            OASISErrorHandling.HandleError(ref result, $"Error occured uninstalling the OAPP Template! Reason: {uninstallResult.Message}");
//                    }
//                    else
//                    {
//                        result.IsError = true;
//                        result.Message = "User Denied Uninstall";
//                        Console.WriteLine("");
//                    }
//                }
//            }
//            else
//                OASISErrorHandling.HandleError(ref result, ($"Error occured checking if the OAPP Template is already installed! Reason: {oappInstalledResult.Message}"));

//            return result;
//        }

//        private static OASISResult<IOAPPTemplate> CheckIfAlreadyInstalled(IOAPPTemplate OAPPTemplate, ProviderType providerType = ProviderType.Default)
//        {
//            OASISResult<IOAPPTemplate> result = new OASISResult<IOAPPTemplate>(OAPPTemplate);
//            OASISResult<bool> oappInstalledResult = STAR.OASISAPI.OAPPTemplates.IsOAPPTemplateInstalled(STAR.BeamedInAvatar.Id, OAPPTemplate.OAPPSystemHolonDNA.Id, OAPPTemplate.OAPPSystemHolonDNA.Version, providerType);

//            if (oappInstalledResult != null && !oappInstalledResult.IsError)
//            {
//                if (oappInstalledResult.Result)
//                {
//                    Console.WriteLine("");
//                    CLIEngine.ShowWarningMessage($"You have already installed this version (v{OAPPTemplate.OAPPSystemHolonDNA.Version}). Please uninstall before attempting to re-install.");

//                    if (CLIEngine.GetConfirmation("Do you wish to uninstall the OAPP Template now? Press 'Y' for Yes or 'N' for No."))
//                    {
//                        Console.WriteLine("");
//                        CLIEngine.ShowWorkingMessage("Uninstalling OAPP Template...");
//                        OASISResult<IInstalledOAPPTemplate> uninstallResult = STAR.OASISAPI.OAPPTemplates.UninstallOAPPTemplate(STAR.BeamedInAvatar.Id, result.Result.OAPPSystemHolonDNA.Id, result.Result.OAPPSystemHolonDNA.Version, providerType);

//                        if (uninstallResult != null && uninstallResult.Result != null && !uninstallResult.IsError)
//                        {
//                            CLIEngine.ShowSuccessMessage("OAPP Template Successfully Uninstalled.");
//                            result.MetaData["Reinstall"] = "1";
//                        }
//                        else
//                            OASISErrorHandling.HandleError(ref result, $"Error occured uninstalling the OAPP Template! Reason: {uninstallResult.Message}");
//                    }
//                    else
//                    {
//                        result.IsError = true;
//                        result.Message = "User Denied Uninstall";
//                        Console.WriteLine("");
//                    }
//                }
//            }
//            else
//                OASISErrorHandling.HandleError(ref result, ($"Error occured checking if the OAPP Template is already installed! Reason: {oappInstalledResult.Message}"));

//            return result;
//        }

//        private static async Task<OASISResult<IInstalledOAPPTemplate>> CheckIfInstalledAndInstallOAPPTemplateAsync(IOAPPTemplate OAPPTemplate, string downloadPath, string installPath, InstallMode installMode, string fullPathToPublishedOAPPTemplateFile = "", ProviderType providerType = ProviderType.Default)
//        {
//            OASISResult<IInstalledOAPPTemplate> installResult = new OASISResult<IInstalledOAPPTemplate>();
//            bool continueInstall = false;

//            if (OAPPTemplate != null)
//            {
//                if (installMode != InstallMode.DownloadOnly)
//                {
//                    OASISResult<IOAPPTemplate> checkResult = await CheckIfAlreadyInstalledAsync(OAPPTemplate, providerType);

//                    if (checkResult != null && checkResult.Result != null && !checkResult.IsError)
//                        continueInstall = true;
//                    else
//                        CLIEngine.ShowErrorMessage($"Error checking if the OAPP Template is already installed! Reason: {checkResult.MetaData}");
//                }
//            }

//            if (continueInstall)
//                installResult = await InstallOAPPTemplateAsync(OAPPTemplate, downloadPath, installPath, installMode, fullPathToPublishedOAPPTemplateFile, providerType);

//            return installResult;
//        }

//        private static OASISResult<IInstalledOAPPTemplate> CheckIfInstalledAndInstallOAPPTemplate(IOAPPTemplate OAPPTemplate, string downloadPath, string installPath, InstallMode installMode, string fullPathToPublishedOAPPTemplateFile = "", ProviderType providerType = ProviderType.Default)
//        {
//            OASISResult<IInstalledOAPPTemplate> installResult = new OASISResult<IInstalledOAPPTemplate>();
//            bool continueInstall = false;

//            if (OAPPTemplate != null)
//            {
//                if (installMode != InstallMode.DownloadOnly)
//                {
//                    OASISResult<IOAPPTemplate> checkResult = CheckIfAlreadyInstalled(OAPPTemplate, providerType);

//                    if (checkResult != null && checkResult.Result != null && !checkResult.IsError)
//                        continueInstall = true;
//                    else
//                        CLIEngine.ShowErrorMessage($"Error checking if the OAPP Template is already installed! Reason: {checkResult.MetaData}");
//                }
//            }

//            if (continueInstall)
//                installResult = InstallOAPPTemplate(OAPPTemplate, downloadPath, installPath, installMode, fullPathToPublishedOAPPTemplateFile, providerType);

//            return installResult;
//        }

//        private static async Task<OASISResult<IInstalledOAPPTemplate>> InstallOAPPTemplateAsync(IOAPPTemplate oappTemplate, string downloadPath, string installPath, InstallMode installMode, string fullPathToPublishedOAPPTemplateFile = "", ProviderType providerType = ProviderType.Default)
//        {
//            OASISResult<IInstalledOAPPTemplate> result = new OASISResult<IInstalledOAPPTemplate>();

//            switch (installMode)
//            {
//                case InstallMode.DownloadAndInstall:
//                    result = await STAR.OASISAPI.OAPPTemplates.DownloadAndInstallOAPPTemplateAsync(STAR.BeamedInAvatar.Id, oappTemplate, installPath, downloadPath, true, false, providerType);
//                    break;

//                case InstallMode.DownloadOnly:
//                    {
//                        OASISResult<IDownloadedOAPPTemplate> downloadResult = await STAR.OASISAPI.OAPPTemplates.DownloadOAPPTemplateAsync(STAR.BeamedInAvatar.Id, oappTemplate, downloadPath, false, providerType);

//                        if (downloadResult != null && downloadResult.Result != null && !downloadResult.IsError)
//                        {
//                            result.Result = new InstalledOAPPTemplate() { OAPPSystemHolonDNA = downloadResult.Result.OAPPSystemHolonDNA };
//                            result.Result.DownloadedOn = downloadResult.Result.DownloadedOn;
//                            result.Result.DownloadedBy = downloadResult.Result.DownloadedBy;
//                            result.Result.DownloadedByAvatarUsername = downloadResult.Result.DownloadedByAvatarUsername;
//                            result.Result.DownloadedPath = downloadResult.Result.DownloadedPath;
//                        }
//                        else
//                        {
//                            result.Message = downloadResult.Message;
//                            result.IsError = true;
//                        }
//                    }
//                    break;

//                case InstallMode.InstallOnly:
//                    result = await STAR.OASISAPI.OAPPTemplates.InstallOAPPTemplateAsync(STAR.BeamedInAvatar.Id, fullPathToPublishedOAPPTemplateFile, installPath, true, null, false, providerType);
//                    break;

//                case InstallMode.DownloadAndReInstall:
//                    result = await STAR.OASISAPI.OAPPTemplates.DownloadAndInstallOAPPTemplateAsync(STAR.BeamedInAvatar.Id, oappTemplate, installPath, downloadPath, true, true, providerType);
//                    break;

//                case InstallMode.ReInstall:
//                    result = await STAR.OASISAPI.OAPPTemplates.InstallOAPPTemplateAsync(STAR.BeamedInAvatar.Id, fullPathToPublishedOAPPTemplateFile, installPath, true, null, true, providerType);
//                    break;
//            }

//            return result;
//        }

//        private static OASISResult<IInstalledOAPPTemplate> InstallOAPPTemplate(IOAPPTemplate oappTemplate, string downloadPath, string installPath, InstallMode installMode, string fullPathToPublishedOAPPTemplateFile = "", ProviderType providerType = ProviderType.Default)
//        {
//            OASISResult<IInstalledOAPPTemplate> result = new OASISResult<IInstalledOAPPTemplate>();

//            switch (installMode)
//            {
//                case InstallMode.DownloadAndInstall:
//                    result = STAR.OASISAPI.OAPPTemplates.DownloadAndInstallOAPPTemplate(STAR.BeamedInAvatar.Id, oappTemplate, installPath, downloadPath, true, false, providerType);
//                    break;

//                case InstallMode.DownloadOnly:
//                    {
//                        OASISResult<IDownloadedOAPPTemplate> downloadResult = STAR.OASISAPI.OAPPTemplates.DownloadOAPPTemplate(STAR.BeamedInAvatar.Id, oappTemplate, downloadPath, false, providerType);

//                        if (downloadResult != null && downloadResult.Result != null && !downloadResult.IsError)
//                        {
//                            result.Result = new InstalledOAPPTemplate() { OAPPSystemHolonDNA = downloadResult.Result.OAPPSystemHolonDNA };
//                            result.Result.DownloadedOn = downloadResult.Result.DownloadedOn;
//                            result.Result.DownloadedBy = downloadResult.Result.DownloadedBy;
//                            result.Result.DownloadedByAvatarUsername = downloadResult.Result.DownloadedByAvatarUsername;
//                            result.Result.DownloadedPath = downloadResult.Result.DownloadedPath;
//                        }
//                        else
//                        {
//                            result.Message = downloadResult.Message;
//                            result.IsError = true;
//                        }
//                    }
//                    break;

//                case InstallMode.InstallOnly:
//                    result = STAR.OASISAPI.OAPPTemplates.InstallOAPPTemplate(STAR.BeamedInAvatar.Id, fullPathToPublishedOAPPTemplateFile, installPath, true, null, false, providerType);
//                    break;

//                case InstallMode.DownloadAndReInstall:
//                    result = STAR.OASISAPI.OAPPTemplates.DownloadAndInstallOAPPTemplate(STAR.BeamedInAvatar.Id, oappTemplate, installPath, downloadPath, true, true, providerType);
//                    break;

//                case InstallMode.ReInstall:
//                    result = STAR.OASISAPI.OAPPTemplates.InstallOAPPTemplate(STAR.BeamedInAvatar.Id, fullPathToPublishedOAPPTemplateFile, installPath, true, null, true, providerType);
//                    break;
//            }

//            return result;
//        }

//        private static void OAPPTemplates_OnPublishStatusChanged(object sender, OAPPSystemHolonPublishStatusEventArgs e)
//        {
//            switch (e.Status)
//            {
//                case OAPPSystemHolonPublishStatus.Uploading:
//                    CLIEngine.ShowMessage("Uploading...");
//                    Console.WriteLine("");
//                    break;

//                case OAPPSystemHolonPublishStatus.Published:
//                    CLIEngine.ShowSuccessMessage("OAPP Template Published Successfully");
//                    break;

//                case OAPPSystemHolonPublishStatus.Error:
//                    CLIEngine.ShowErrorMessage(e.ErrorMessage);
//                    break;

//                default:
//                    CLIEngine.ShowWorkingMessage($"{Enum.GetName(typeof(OAPPTemplatePublishStatus), e.Status)}...");
//                    break;
//            }
//        }

//        private static void OAPPTemplates_OnUploadStatusChanged(object sender, OAPPSystemHolonUploadProgressEventArgs e)
//        {
//            CLIEngine.ShowProgressBar((double)e.Progress / (double)100);
//        }

//        private static void OAPPTemplates_OnInstallStatusChanged(object sender, OAPPSystemHolonInstallStatusEventArgs e)
//        {
//            switch (e.Status)
//            {
//                case OAPPSystemHolonInstallStatus.Downloading:
//                    CLIEngine.ShowMessage("Downloading...");
//                    Console.WriteLine("");
//                    break;

//                case OAPPSystemHolonInstallStatus.Installed:
//                    CLIEngine.ShowSuccessMessage("OAPP Template Installed Successfully");
//                    break;

//                case OAPPSystemHolonInstallStatus.Error:
//                    CLIEngine.ShowErrorMessage(e.ErrorMessage);
//                    break;

//                default:
//                    CLIEngine.ShowWorkingMessage($"{Enum.GetName(typeof(OAPPInstallStatus), e.Status)}...");
//                    break;
//            }
//        }

//        private static void OAPPTemplates_OnDownloadStatusChanged(object sender, OAPPSystemHolonDownloadProgressEventArgs e)
//        {
//            CLIEngine.ShowProgressBar((double)e.Progress / (double)100);
//        }
//    }
//}