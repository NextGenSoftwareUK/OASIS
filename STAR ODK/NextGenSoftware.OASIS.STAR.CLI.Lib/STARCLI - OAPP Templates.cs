using System.Diagnostics;
using NextGenSoftware.CLI.Engine;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.ONode.Core.Interfaces;
using NextGenSoftware.OASIS.API.ONode.Core.Interfaces.Holons;
using NextGenSoftware.OASIS.API.ONode.Core.Enums;
using NextGenSoftware.OASIS.API.ONODE.Core.Events;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.ONode.Core.Holons;

namespace NextGenSoftware.OASIS.STAR.CLI.Lib
{
    public static partial class STARCLI
    {
        public static async Task CreateOAPPTemplateAsync(object createParams, ProviderType providerType = ProviderType.Default)
        {
            CLIEngine.ShowDivider();
            CLIEngine.ShowMessage("Welcome to the OAPP Template Wizard");
            CLIEngine.ShowDivider();
            Console.WriteLine();
            CLIEngine.ShowMessage("This wizard will allow you create an OAPP Template which can be used to create a OAPP from.", false);
            CLIEngine.ShowMessage("The OAPP Template can be created from anything you like such as a website, javascript template, game, app, service, etc in any language, platform or os.");
            CLIEngine.ShowMessage("You simply need to add specefic STAR ODK OAPP Template reserved tags where dynamic data will be injected in from the OAPP meta data.");
            CLIEngine.ShowMessage("The wizard will create an empty folder with a OAPPTemplateDNA.json file in it. You then simply place any files/folders you need into this folder.");
            CLIEngine.ShowMessage("Finally you run the sub-command 'oapp template publish' to convert the folder containing the OAPP Template (can contain any number of files and sub-folders) into a OAPP Template file (.oapptemplate).");
            CLIEngine.ShowMessage("You can then share the .oapptemplate file with others from which you can create OAPP's from. You can also optionally choose to upload the .oapptemplate file to the STARNET store so others can search, download and install the OAPP Template. They can then create OAPP's from the template.");
            CLIEngine.ShowDivider();

            string OAPPTemplateName = CLIEngine.GetValidInput("What is the name of the OAPP Template?");

            if (OAPPTemplateName == "exit")
                return;

            string OAPPTemplateDesc = CLIEngine.GetValidInput("What is the description of the OAPP Template?");

            if (OAPPTemplateDesc == "exit")
                return;

            object value = CLIEngine.GetValidInputForEnum("What type of OAPP Template do you wish to create?", typeof(OAPPTemplateType));

            if (value != null)
            {
                if (value.ToString() == "exit")
                    return;

                OAPPTemplateType OAPPTemplateType = (OAPPTemplateType)value;
                string oappTemplatePath = "";

                if (!string.IsNullOrEmpty(STAR.STARDNA.BasePath))
                    oappTemplatePath = Path.Combine(STAR.STARDNA.BasePath, STAR.STARDNA.DefaultOAPPTemplatesSourcePath);
                else
                    oappTemplatePath = STAR.STARDNA.DefaultOAPPTemplatesSourcePath;

                if (!CLIEngine.GetConfirmation($"Do you wish to create the OAPP Template in the default path defined in the STARDNA as 'DefaultOAPPTemplatesSourcePath'? The current path points to: {oappTemplatePath}"))
                    oappTemplatePath = CLIEngine.GetValidFolder("Where do you wish to create the OAPP Template?");

                oappTemplatePath = Path.Combine(oappTemplatePath, OAPPTemplateName);

                Console.WriteLine("");
                CLIEngine.ShowWorkingMessage("Generating OAPP Template...");
                OASISResult<IOAPPTemplate> oappTemplateResult = await STAR.OASISAPI.OAPPTemplates.CreateOAPPTemplateAsync(OAPPTemplateName, OAPPTemplateDesc, OAPPTemplateType, STAR.BeamedInAvatar.Id, oappTemplatePath, providerType);

                if (oappTemplateResult != null)
                {
                    if (!oappTemplateResult.IsError && oappTemplateResult.Result != null)
                    {
                        CLIEngine.ShowSuccessMessage($"OAPP Template Successfully Generated. ({oappTemplateResult.Message})");
                        ShowOAPPTemplate(oappTemplateResult.Result);
                        Console.WriteLine("");

                        if (CLIEngine.GetConfirmation("Do you wish to open the OAPP Template folder now?"))
                            Process.Start("explorer.exe", oappTemplatePath);

                        Console.WriteLine("");
                    }
                    //else
                    //    CLIEngine.ShowErrorMessage($"Error Occured: {oappTemplateResult.Message}"); //Redundant error message.
                }
                else
                    CLIEngine.ShowErrorMessage($"Unknown Error Occured.");
            }
        }

        public static async Task EditOAPPTemplateAsync(string idOrName = "", object editParams = null, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IOAPPTemplate> loadResult = await LoadOAPPTemplateAsync(idOrName, "edit", providerType);

            if (loadResult != null && loadResult.Result != null && !loadResult.IsError)
            {
                ShowOAPPTemplate(loadResult.Result);

                //TODO: Comeback to this.
                loadResult.Result.Name = CLIEngine.GetValidInput("What is the name of the OAPP Template?");
                loadResult.Result.Description = CLIEngine.GetValidInput("What is the description of the OAPP Template?");

                OASISResult<IOAPPTemplate> result = await STAR.OASISAPI.OAPPTemplates.SaveOAPPTemplateAsync(loadResult.Result, STAR.BeamedInAvatar.Id, providerType);
                CLIEngine.ShowWorkingMessage("Saving OAPP Template...");

                if (result != null && !result.IsError && result.Result != null)
                {
                    CLIEngine.ShowSuccessMessage("OAPP Template Successfully Updated.");
                    ShowOAPPTemplate(result.Result);
                }
                else
                    CLIEngine.ShowErrorMessage($"An error occured updating the OAPP Template. Reason: {result.Message}");
            }
            else
                CLIEngine.ShowErrorMessage($"An error occured loading the OAPP Template. Reason: {loadResult.Message}");
        }

        public static async Task DeleteOAPPTemplateAsync(string idOrName = "", bool softDelete = true, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IOAPPTemplate> result = await LoadOAPPTemplateAsync(idOrName, "delete", providerType);

            if (result != null && !result.IsError && result.Result != null)
            {
                ShowOAPPTemplate(result.Result);

                if (CLIEngine.GetConfirmation("Are you sure you wish to delete the OAPP Template?"))
                {
                    CLIEngine.ShowWorkingMessage("Deleting OAPP Template...");
                    result = await STAR.OASISAPI.OAPPTemplates.DeleteOAPPTemplateAsync(result.Result, true, providerType);

                    if (result != null && !result.IsError && result.Result != null)
                    {
                        CLIEngine.ShowSuccessMessage("OAPP Template Successfully Deleted.");
                        ShowOAPPTemplate(result.Result);
                    }
                    else
                        CLIEngine.ShowErrorMessage($"An error occured deleting the OAPP Template. Reason: {result.Message}");
                }
            }
            else
                CLIEngine.ShowErrorMessage($"An error occured loading the OAPP Template. Reason: {result.Message}");
        }

        public static async Task PublishOAPPTemplateAsync(string oappTemplatePath = "", ProviderType providerType = ProviderType.Default)
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

            if (string.IsNullOrEmpty(oappTemplatePath))
            {
                Console.WriteLine("");
                string OAPPPathQuestion = "What is the full path to the OAPP Template directory?";
                launchTargetQuestion = "What is the relative path (from the root of the path given above, e.g bin\\launch.exe) to the launch target for the OAPP Template? (This could be the exe or batch file for a desktop or console app, or the index.html page for a website, etc)";
                oappTemplatePath = CLIEngine.GetValidFolder(OAPPPathQuestion, false);
            }

            OASISResult<IOAPPTemplateDNA> OAPPTemplateDNAResult = await STAR.OASISAPI.OAPPTemplates.ReadOAPPTemplateDNAAsync(oappTemplatePath);

            if (OAPPTemplateDNAResult != null && OAPPTemplateDNAResult.Result != null && !OAPPTemplateDNAResult.IsError)
            {
                switch (OAPPTemplateDNAResult.Result.OAPPTemplateType)
                {
                    case OAPPTemplateType.Console:
                    case OAPPTemplateType.WPF:
                    case OAPPTemplateType.WinForms:
                        launchTarget = $"{OAPPTemplateDNAResult.Result.Name}.exe"; //TODO: For this line to work need to remove the namespace question so it just uses the OAPPName as the namespace. //TODO: Eventually this will be set in the OAPPTemplate and/or can also be set when I add the command line dotnet publish integration.
                        break;

                    case OAPPTemplateType.Blazor:
                    case OAPPTemplateType.MAUI:
                    case OAPPTemplateType.WebMVC:
                        launchTarget = $"index.html";
                        break;
                }

                if (!string.IsNullOrEmpty(launchTarget))
                {
                    if (!CLIEngine.GetConfirmation($"{launchTargetQuestion} Do you wish to use the following default launch target: {launchTarget}?"))
                    {
                        Console.WriteLine("");
                        launchTarget = CLIEngine.GetValidFile("What launch target do you wish to use? ", oappTemplatePath);
                    }
                    else
                        launchTarget = Path.Combine(oappTemplatePath, launchTarget);
                }
                else
                    launchTarget = CLIEngine.GetValidFile(launchTargetQuestion, oappTemplatePath);

                Console.WriteLine("");
                bool registerOnSTARNET = CLIEngine.GetConfirmation("Do you wish to publish to STARNET? If you select 'Y' to this question then your OAPP Template will be published to STARNET where others will be able to find, download and install. If you select 'N' then only the .oapptemplate install file will be generated on your local device, which you can distribute as you please. This file will also be generated even if you publish to STARNET.");
                Console.WriteLine("");

                if (registerOnSTARNET && !simpleWizard)
                {
                    CLIEngine.ShowMessage("Do you wish to publish/upload the .oapptemplate file to an OASIS Provider or to the cloud or both? Depending on which OASIS Provider is chosen such as IPFSOASIS there may issues such as speed, relialbility etc for such a large file. If you choose to upload to the cloud this could be faster and more reliable (but there is a limit of 5 OAPPs on the free plan and you will need to upgrade to upload more than 5 OAPPs). You may want to choose to use both to add an extra layer of redundancy (recommended).");

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

                if (Path.IsPathRooted(STAR.STARDNA.DefaultOAPPTemplatesPublishedPath))
                    publishPath = STAR.STARDNA.DefaultOAPPTemplatesPublishedPath;
                else
                    publishPath = Path.Combine(STAR.STARDNA.BasePath, STAR.STARDNA.DefaultOAPPTemplatesPublishedPath);

                if (!simpleWizard)
                {
                    if (!CLIEngine.GetConfirmation($"Do you wish to publish the OAPP Template to the default publish folder defined in the STARDNA as DefaultOAPPTemplatesPublishedPath : {publishPath}?"))
                    {
                        Console.WriteLine("");

                        if (CLIEngine.GetConfirmation($"Do you wish to publish the OAPP Template to: {Path.Combine(oappTemplatePath, "Published")}?"))
                            publishPath = Path.Combine(oappTemplatePath, "Published");
                        else
                        {
                            Console.WriteLine("");
                            publishPath = CLIEngine.GetValidFolder("Where do you wish to publish the OAPP Template?", true);
                        }
                    }
                }

                publishPath = new DirectoryInfo(publishPath).FullName;

                Console.WriteLine("");
                CLIEngine.ShowWorkingMessage("Publishing OAPP Template...");

                STAR.OASISAPI.OAPPTemplates.OnOAPPTemplatePublishStatusChanged += OAPPTemplates_OnOAPPTemplatePublishStatusChanged;
                STAR.OASISAPI.OAPPTemplates.OnOAPPTemplateUploadStatusChanged += OAPPTemplates_OnOAPPTemplateUploadStatusChanged;
                OASISResult<IOAPPTemplate> publishResult = await STAR.OASISAPI.OAPPTemplates.PublishOAPPTemplateAsync(oappTemplatePath, launchTarget, STAR.BeamedInAvatar.Id, publishPath, registerOnSTARNET, generateOAPP, uploadOAPPToCloud, providerType, OAPPBinaryProviderType);
                STAR.OASISAPI.OAPPTemplates.OnOAPPTemplateUploadStatusChanged -= OAPPTemplates_OnOAPPTemplateUploadStatusChanged;
                STAR.OASISAPI.OAPPTemplates.OnOAPPTemplatePublishStatusChanged -= OAPPTemplates_OnOAPPTemplatePublishStatusChanged;

                if (publishResult != null && !publishResult.IsError && publishResult.Result != null)
                {
                    ShowOAPPTemplate(publishResult.Result);

                    if (CLIEngine.GetConfirmation("Do you wish to install the OAPP Template now?"))
                        await DownloadAndInstallOAPPTemplateAsync(publishResult.Result.OAPPTemplateDNA.Id.ToString(), true, providerType);

                    Console.WriteLine("");
                }
                else
                    CLIEngine.ShowErrorMessage($"An error occured publishing the OAPP Template. Reason: {publishResult.Message}");
            }
            else
                CLIEngine.ShowErrorMessage("The OAPPTemplateDNA.json file could not be found! Please ensure it is in the folder you specified.");
        }

        public static async Task UnPublishOAPPTemplateAsync(string idOrName = "", ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IOAPPTemplate> result = await LoadOAPPTemplateAsync(idOrName, "unpublish", providerType);

            if (result != null && !result.IsError && result.Result != null)
            {
                OASISResult<IOAPPTemplate> unpublishResult = await STAR.OASISAPI.OAPPTemplates.UnPublishOAPPTemplateAsync(result.Result, STAR.BeamedInAvatar.Id, providerType);

                if (unpublishResult != null && !unpublishResult.IsError && unpublishResult.Result != null)
                {
                    CLIEngine.ShowSuccessMessage("OAPP Template Successfully Unpublished.");
                    ShowOAPPTemplate(result.Result);
                }
                else
                    CLIEngine.ShowErrorMessage($"An error occured unpublishing the OAPP Template. Reason: {unpublishResult.Message}");
            }
        }
        public static async Task<OASISResult<IInstalledOAPPTemplate>> DownloadAndInstallOAPPTemplateAsync(string idOrName = "", bool install = true, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IInstalledOAPPTemplate> installResult = new OASISResult<IInstalledOAPPTemplate>();
            string downloadPath = "";
            string installPath = "";
            bool simpleWizard = false;
            string operation = "install";

            if (!install)
                operation = "download"; 

            STAR.OASISAPI.OAPPTemplates.OnOAPPTemplateDownloadStatusChanged += OAPPTemplates_OnOAPPTemplateDownloadStatusChanged;
            STAR.OASISAPI.OAPPTemplates.OnOAPPTemplateInstallStatusChanged += OAPPTemplates_OnOAPPTemplateInstallStatusChanged;

            if (Path.IsPathRooted(STAR.STARDNA.DefaultOAPPTemplatesDownloadedPath))
                downloadPath = STAR.STARDNA.DefaultOAPPTemplatesDownloadedPath;
            else
                downloadPath = Path.Combine(STAR.STARDNA.BasePath, STAR.STARDNA.DefaultOAPPTemplatesDownloadedPath);

            if (Path.IsPathRooted(STAR.STARDNA.DefaultOAPPTemplatesInstalledPath))
                installPath = STAR.STARDNA.DefaultOAPPTemplatesInstalledPath;
            else
                installPath = Path.Combine(STAR.STARDNA.BasePath, STAR.STARDNA.DefaultOAPPTemplatesInstalledPath);

            Console.WriteLine("");

            if (CLIEngine.GetConfirmation("Do you wish to launch the Simple or Advanced Wizard? The Simple Wizard will use defaults (recommended) but the Advanced Wizard will allow greater control and customisation. Press 'Y' for Simple or 'N' for Advanced."))
                simpleWizard = true;

            if (!simpleWizard)
            {
                Console.WriteLine("");

                if (!CLIEngine.GetConfirmation($"Do you wish to download the OAPP Template to the default download folder defined in the STARDNA as DefaultOAPPTemplatesDownloadedPath : {downloadPath}?"))
                {
                    Console.WriteLine("");
                    downloadPath = CLIEngine.GetValidFolder("What is the full path to where you wish to download the OAPP Template?", true);
                }

                downloadPath = new DirectoryInfo(downloadPath).FullName;

                if (install)
                {
                    Console.WriteLine("");

                    if (!CLIEngine.GetConfirmation($"Do you wish to install the OAPP Template to the default install folder defined in the STARDNA as DefaultOAPPTemplatesInstalledPath : {installPath}?"))
                    {
                        Console.WriteLine("");
                        installPath = CLIEngine.GetValidFolder("What is the full path to where you wish to install the OAPP Template?", true);
                    }

                    installPath = new DirectoryInfo(installPath).FullName;
                }
            }

            if (!string.IsNullOrEmpty(idOrName))
            {
                Console.WriteLine("");
                OASISResult<IOAPPTemplate> result = await LoadOAPPTemplateForProviderAsync(idOrName, operation, providerType, false);

                if (result != null && result.Result != null && !result.IsError)
                    installResult = await InstallOAPPTemplateAsync(result.Result, downloadPath, installPath, install, providerType);
            }
            else
            {
                Console.WriteLine("");
                if (install && CLIEngine.GetConfirmation("Do you wish to install the OAPP Template from a local .oapptemplate file or from STARNET? Press 'Y' for local .oapptemplate file or 'N' for STARNET."))
                {
                    Console.WriteLine("");
                    string oappPath = CLIEngine.GetValidFile("What is the full path to the .oapptemplate file?");

                    if (oappPath == "exit")
                        return installResult;
                    
                    installResult = await STAR.OASISAPI.OAPPTemplates.InstallOAPPTemplateAsync(STAR.BeamedInAvatar.Id, oappPath, installPath, true, null, providerType);
                }
                else
                {
                    Console.WriteLine("");
                    OASISResult<IOAPPTemplate> result = await LoadOAPPTemplateForProviderAsync("", operation, providerType, false);

                    if (result != null && result.Result != null && !result.IsError)
                        installResult = await InstallOAPPTemplateAsync(result.Result, downloadPath, installPath, install, providerType);
                    else
                    {
                        installResult.Message = result.Message;
                        installResult.IsError = true;
                    }
                    
                    

                    //CLIEngine.ShowWorkingMessage("Loading OAPP Templates...");
                    //OASISResult<IEnumerable<IOAPPTemplate>> oappTemplatesResult = await ListAllOAPPTemplatesAsync();

                    //if (oappTemplatesResult != null && oappTemplatesResult.Result != null && !oappTemplatesResult.IsError && oappTemplatesResult.Result.Count() > 0)
                    //{
                    //    OASISResult<IOAPPTemplate> result = await LoadOAPPTemplateForProviderAsync("", operation, providerType, false);

                    //    if (result != null && result.Result != null && !result.IsError)
                    //        installResult = await InstallOAPPTemplateAsync(result.Result, downloadPath, installPath, install, providerType);
                    //    else
                    //    {
                    //        installResult.Message = result.Message;
                    //        installResult.IsError = true;
                    //    }
                    //}
                    //else
                    //{
                    //    installResult.Message = $"No OAPP Templates found to {operation}.";
                    //    installResult.IsError = true;
                    //}
                }
            }

            if (installResult != null)
            {
                if (!installResult.IsError && installResult.Result != null)
                {
                    ShowInstalledOAPPTemplate(installResult.Result);

                    if (CLIEngine.GetConfirmation("Do you wish to open the folder to the OAPP Template now?"))
                        STAR.OASISAPI.OAPPTemplates.OpenOAPPTemplateFolder(STAR.BeamedInAvatar.Id, installResult.Result);
                }
                else
                    CLIEngine.ShowErrorMessage($"Error {operation}ing OAPP Template. Reason: {installResult.Message}");
            }
            else
                CLIEngine.ShowErrorMessage($"Error {operation}ing OAPP Template. Reason: Unknown error occured!");

            STAR.OASISAPI.OAPPTemplates.OnOAPPTemplateDownloadStatusChanged -= OAPPTemplates_OnOAPPTemplateDownloadStatusChanged;
            STAR.OASISAPI.OAPPTemplates.OnOAPPTemplateInstallStatusChanged -= OAPPTemplates_OnOAPPTemplateInstallStatusChanged;

            Console.WriteLine("");
            return installResult;
        }

        public static OASISResult<IInstalledOAPPTemplate> DownloadAndInstallOAPPTemplate(string idOrName = "", ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IInstalledOAPPTemplate> installResult = new OASISResult<IInstalledOAPPTemplate>();
            string downloadPath = "";
            string installPath = "";

            STAR.OASISAPI.OAPPTemplates.OnOAPPTemplateDownloadStatusChanged += OAPPTemplates_OnOAPPTemplateDownloadStatusChanged;
            STAR.OASISAPI.OAPPTemplates.OnOAPPTemplateInstallStatusChanged += OAPPTemplates_OnOAPPTemplateInstallStatusChanged;

            if (Path.IsPathRooted(STAR.STARDNA.DefaultOAPPTemplatesDownloadedPath))
                downloadPath = STAR.STARDNA.DefaultOAPPTemplatesDownloadedPath;
            else
                downloadPath = Path.Combine(STAR.STARDNA.BasePath, STAR.STARDNA.DefaultOAPPTemplatesDownloadedPath);

            if (Path.IsPathRooted(STAR.STARDNA.DefaultOAPPTemplatesInstalledPath))
                installPath = STAR.STARDNA.DefaultOAPPTemplatesInstalledPath;
            else
                installPath = Path.Combine(STAR.STARDNA.BasePath, STAR.STARDNA.DefaultOAPPTemplatesInstalledPath);

            Console.WriteLine("");

            if (!CLIEngine.GetConfirmation($"Do you wish to download the OAPP Template to the default download folder defined in the STARDNA as DefaultOAPPTemplatesDownloadedPath : {downloadPath}?"))
            {
                Console.WriteLine("");
                downloadPath = CLIEngine.GetValidFolder("What is the full path to where you wish to download the OAPP Template?", true);
            }

            downloadPath = new DirectoryInfo(downloadPath).FullName;

            Console.WriteLine("");

            if (!CLIEngine.GetConfirmation($"Do you wish to install the OAPP Template to the default install folder defined in the STARDNA as DefaultOAPPTemplatesInstalledPath : {installPath}?"))
            {
                Console.WriteLine("");
                installPath = CLIEngine.GetValidFolder("What is the full path to where you wish to install the OAPP Template?", true);
            }

            installPath = new DirectoryInfo(installPath).FullName;

            if (!string.IsNullOrEmpty(idOrName))
            {
                Console.WriteLine("");
                OASISResult<IOAPPTemplate> result = LoadOAPPTemplateForProvider(idOrName, "install", providerType, false);

                if (result != null && result.Result != null && !result.IsError)
                    installResult = STAR.OASISAPI.OAPPTemplates.InstallOAPPTemplate(STAR.BeamedInAvatar.Id, result.Result, installPath, downloadPath, true, providerType);
            }
            else
            {
                Console.WriteLine("");
                if (CLIEngine.GetConfirmation("Do you wish to install the OAPP Template from a local .oapptemplate file or from STARNET? Press 'Y' for local .oapptemplate file or 'N' for STARNET."))
                {
                    Console.WriteLine("");
                    string oappPath = CLIEngine.GetValidFile("What is the full path to the .oapptemplate file?");

                    if (oappPath == "exit")
                        return installResult;

                    installResult = STAR.OASISAPI.OAPPTemplates.InstallOAPPTemplate(STAR.BeamedInAvatar.Id, oappPath, installPath, true, providerType);
                }
                else
                {
                    Console.WriteLine("");
                    CLIEngine.ShowWorkingMessage("Loading OAPP Templates...");
                    OASISResult<IEnumerable<IOAPPTemplate>> oappTemplatesResult = ListAllOAPPTemplates();

                    if (oappTemplatesResult != null && oappTemplatesResult.Result != null && !oappTemplatesResult.IsError && oappTemplatesResult.Result.Count() > 0)
                    {
                        OASISResult<IOAPPTemplate> result = LoadOAPPTemplateForProvider("", "install", providerType, false);

                        if (result != null && result.Result != null && !result.IsError)
                            installResult = STAR.OASISAPI.OAPPTemplates.InstallOAPPTemplate(STAR.BeamedInAvatar.Id, result.Result, installPath, downloadPath, true, providerType);
                        else
                        {
                            installResult.Message = result.Message;
                            installResult.IsError = true;
                        }
                    }
                    else
                    {
                        installResult.Message = "No OAPP Templates found to install.";
                        installResult.IsError = true;
                    }
                }
            }

            if (installResult != null)
            {
                if (!installResult.IsError && installResult.Result != null)
                {
                    ShowInstalledOAPPTemplate(installResult.Result);

                    if (CLIEngine.GetConfirmation("Do you wish to open the folder to the OAPP Template now?"))
                        STAR.OASISAPI.OAPPTemplates.OpenOAPPTemplateFolder(STAR.BeamedInAvatar.Id, installResult.Result);
                }
                else
                    CLIEngine.ShowErrorMessage($"Error installing OAPP Template. Reason: {installResult.Message}");
            }
            else
                CLIEngine.ShowErrorMessage($"Error installing OAPP Template. Reason: Unknown error occured!");

            STAR.OASISAPI.OAPPTemplates.OnOAPPTemplateDownloadStatusChanged -= OAPPTemplates_OnOAPPTemplateDownloadStatusChanged;
            STAR.OASISAPI.OAPPTemplates.OnOAPPTemplateInstallStatusChanged -= OAPPTemplates_OnOAPPTemplateInstallStatusChanged;

            Console.WriteLine("");
            return installResult;
        }

        //public static async Task<OASISResult<IDownloadedOAPPTemplate>> DownloadOAPPTemplateAsync(string idOrName = "", ProviderType providerType = ProviderType.Default)
        //{
        //    OASISResult<IDownloadedOAPPTemplate> result = new OASISResult<IDownloadedOAPPTemplate>();

        //    return result;
        //}

        public static async Task UnInstallOAPPTemplateAsync(string idOrName = "", ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IOAPPTemplate> result = await LoadOAPPTemplateAsync(idOrName, "uninstall", providerType);

            if (result != null && !result.IsError && result.Result != null)
            {
                OASISResult<IOAPPTemplate> uninstallResult = await STAR.OASISAPI.OAPPTemplates.UnInstallOAPPTemplateAsync(result.Result.OAPPTemplateDNA, STAR.BeamedInAvatar.Id, providerType);

                if (uninstallResult != null)
                {
                    if (!uninstallResult.IsError && uninstallResult.Result != null)
                    {
                        CLIEngine.ShowSuccessMessage("OAPP Template Successfully Uninstalled.");
                        ShowOAPPTemplate(result.Result);
                    }
                    else
                        CLIEngine.ShowErrorMessage($"Error installing OAPP Template. Reason: {uninstallResult.Message}");
                }
                else
                    CLIEngine.ShowErrorMessage($"Error uninstalling OAPP Template. Reason: Unknown error occured!");
            }
            else
                CLIEngine.ShowErrorMessage($"An error occured loading the OAPP Template. Reason: {result.Message}");
        }

        //public static async Task<OASISResult<IEnumerable<IOAPPTemplate>>> ListAllOAPPTemplatesAsync(int version = 0, ProviderType providerType = ProviderType.Default)
        public static async Task<OASISResult<IEnumerable<IOAPPTemplate>>> ListAllOAPPTemplatesAsync(ProviderType providerType = ProviderType.Default)
        {
            return ListOAPPTemplates(await STAR.OASISAPI.OAPPTemplates.LoadAllOAPPTemplatesAsync(OAPPTemplateType.All, 0, providerType));
        }

        public static OASISResult<IEnumerable<IOAPPTemplate>> ListAllOAPPTemplates(int version = 0, ProviderType providerType = ProviderType.Default)
        {
            return  ListOAPPTemplates(STAR.OASISAPI.OAPPTemplates.LoadAllOAPPTemplates(OAPPTemplateType.All, version, providerType));
        }

        public static async Task ListOAPPTemplatesCreatedByBeamedInAvatarAsync(ProviderType providerType = ProviderType.Default)
        {
            if (STAR.BeamedInAvatar != null)
                ListOAPPTemplates(await STAR.OASISAPI.OAPPTemplates.LoadAllOAPPTemplatesForAvatarAsync(STAR.BeamedInAvatar.AvatarId));
            else
                CLIEngine.ShowErrorMessage("No Avatar Is Beamed In. Please Beam In First!");
        }

        public static async Task<OASISResult<IEnumerable<IInstalledOAPPTemplate>>> ListOAPPTemplatesInstalledForBeamedInAvatarAsync(ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<IInstalledOAPPTemplate>> result = new OASISResult<IEnumerable<IInstalledOAPPTemplate>>();

            if (STAR.BeamedInAvatar != null)
            {
                result = await STAR.OASISAPI.OAPPTemplates.ListInstalledOAPPTemplatesAsync(STAR.BeamedInAvatar.AvatarId);
                ListInstalledOAPPTemplates(result);
            }
            else
                OASISErrorHandling.HandleError(ref result, "No Avatar Is Beamed In. Please Beam In First!");

            return result;
        }

        //public static async Task SearchOAPPTemplatesAsync(string searchTerm, int version = 0, ProviderType providerType = ProviderType.Default)
        public static async Task SearchOAPPTemplatesAsync(string searchTerm, ProviderType providerType = ProviderType.Default)
        {
            ListOAPPTemplates(await STAR.OASISAPI.OAPPTemplates.SearchOAPPTemplatesAsync(searchTerm, 0, providerType));
        }

        public static async Task ShowOAPPTemplateAsync(string idOrName = "", ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IOAPPTemplate> result = await LoadOAPPTemplateAsync(idOrName, "view", providerType);

            if (result != null && !result.IsError && result.Result != null)
                ShowOAPPTemplate(result.Result);
            else
                CLIEngine.ShowErrorMessage($"An error occured loading the OAPP Template. Reason: {result.Message}");
        }

        public static void ShowOAPPTemplate(IOAPPTemplate oappTemplate, bool showDivider = true, int number = 0, bool showDetailedInfo = false)
        {
            if (number > 0)
                CLIEngine.ShowMessage(string.Concat("Number:                                      ", number));  

            CLIEngine.ShowMessage(string.Concat($"Id:                                         ", oappTemplate.OAPPTemplateDNA.Id != Guid.Empty ? oappTemplate.OAPPTemplateDNA.Id : "None"));
            CLIEngine.ShowMessage(string.Concat($"Name:                                       ", !string.IsNullOrEmpty(oappTemplate.OAPPTemplateDNA.Name) ? oappTemplate.OAPPTemplateDNA.Name : "None"));
            CLIEngine.ShowMessage(string.Concat($"Description:                                ", !string.IsNullOrEmpty(oappTemplate.OAPPTemplateDNA.Description) ? oappTemplate.OAPPTemplateDNA.Description : "None"));
            CLIEngine.ShowMessage(string.Concat($"OAPP Template Type:                         ", Enum.GetName(typeof(OAPPTemplateType), oappTemplate.OAPPTemplateDNA.OAPPTemplateType)));
            CLIEngine.ShowMessage(string.Concat($"Created On:                                 ", oappTemplate.OAPPTemplateDNA.CreatedOn != DateTime.MinValue ? oappTemplate.OAPPTemplateDNA.CreatedOn.ToString() : "None"));
            CLIEngine.ShowMessage(string.Concat($"Created By:                                 ", oappTemplate.OAPPTemplateDNA.CreatedByAvatarId != Guid.Empty ? string.Concat(oappTemplate.OAPPTemplateDNA.CreatedByAvatarUsername, " (", oappTemplate.OAPPTemplateDNA.CreatedByAvatarId.ToString(), ")") : "None"));
            CLIEngine.ShowMessage(string.Concat($"OAPP Template Path:                         ", !string.IsNullOrEmpty(oappTemplate.OAPPTemplateDNA.OAPPTemplatePath) ? oappTemplate.OAPPTemplateDNA.OAPPTemplatePath : "None"));
            CLIEngine.ShowMessage(string.Concat($"Published On:                               ", oappTemplate.OAPPTemplateDNA.PublishedOn != DateTime.MinValue ? oappTemplate.OAPPTemplateDNA.PublishedOn.ToString() : "None"));
            CLIEngine.ShowMessage(string.Concat($"Published By:                               ", oappTemplate.OAPPTemplateDNA.PublishedByAvatarId != Guid.Empty ? string.Concat(oappTemplate.OAPPTemplateDNA.PublishedByAvatarUsername, " (", oappTemplate.OAPPTemplateDNA.PublishedByAvatarId.ToString(), ")") : "None"));
            CLIEngine.ShowMessage(string.Concat($"OAPP Template Published Path:               ", !string.IsNullOrEmpty(oappTemplate.OAPPTemplateDNA.OAPPTemplatePublishedPath) ? oappTemplate.OAPPTemplateDNA.OAPPTemplatePublishedPath : "None"));
            CLIEngine.ShowMessage(string.Concat($"OAPP Template Filesize:                     ", oappTemplate.OAPPTemplateDNA.OAPPTemplateFileSize.ToString()));
            CLIEngine.ShowMessage(string.Concat($"OAPP Template Published On STARNET:         ", oappTemplate.OAPPTemplateDNA.OAPPTemplatePublishedOnSTARNET ? "True" : "False"));
            CLIEngine.ShowMessage(string.Concat($"OAPP Template Published To Cloud:           ", oappTemplate.OAPPTemplateDNA.OAPPTemplatePublishedToCloud ? "True" : "False"));
            CLIEngine.ShowMessage(string.Concat($"OAPP Template Published To OASIS Provider:  ", Enum.GetName(typeof(ProviderType), oappTemplate.OAPPTemplateDNA.OAPPTemplatePublishedProviderType)));
            CLIEngine.ShowMessage(string.Concat($"OAPP Template Version:                      ", oappTemplate.OAPPTemplateDNA.Version));
            CLIEngine.ShowMessage(string.Concat($"OASIS Holon Version:                        ", oappTemplate.Version));
            CLIEngine.ShowMessage(string.Concat($"OASIS Holon VersionId:                      ", oappTemplate.VersionId));
            CLIEngine.ShowMessage(string.Concat($"OASIS Holon PreviousVersionId:              ", oappTemplate.PreviousVersionId));
            CLIEngine.ShowMessage(string.Concat($"Versions:                                   ", oappTemplate.OAPPTemplateDNA.Versions));
            CLIEngine.ShowMessage(string.Concat($"Downloads:                                  ", oappTemplate.OAPPTemplateDNA.Downloads));
            CLIEngine.ShowMessage(string.Concat($"Installs:                                   ", oappTemplate.OAPPTemplateDNA.Installs));

            if (showDetailedInfo)
            {
                //Show base holon info.
            }

            if (showDivider)
                CLIEngine.ShowDivider();
        }

        public static void ShowInstalledOAPPTemplate(IInstalledOAPPTemplate oappTemplate)
        {
            ShowOAPPTemplate(oappTemplate, false);
            CLIEngine.ShowMessage(string.Concat($"Downloaded On:                              ", oappTemplate.DownloadedOAPPTemplate != null && oappTemplate.DownloadedOAPPTemplate.DownloadedOn != DateTime.MinValue ? oappTemplate.DownloadedOAPPTemplate.DownloadedOn.ToString() : "None"));
            CLIEngine.ShowMessage(string.Concat($"Downloaded By:                              ", oappTemplate.DownloadedOAPPTemplate != null && oappTemplate.DownloadedOAPPTemplate.DownloadedBy != Guid.Empty ? string.Concat(oappTemplate.DownloadedOAPPTemplate.DownloadedByAvatarUsername, " (", oappTemplate.DownloadedOAPPTemplate.DownloadedBy.ToString(), ")") : "None"));
            CLIEngine.ShowMessage(string.Concat($"Downloaded Path:                            ", oappTemplate.DownloadedOAPPTemplate != null ? oappTemplate.DownloadedOAPPTemplate.DownloadedPath : "None"));
            CLIEngine.ShowMessage(string.Concat($"Installed On:                               ", oappTemplate.InstalledOn != DateTime.MinValue ? oappTemplate.InstalledOn.ToString() : "None"));
            CLIEngine.ShowMessage(string.Concat($"Installed By:                               ", oappTemplate.InstalledBy != Guid.Empty ? string.Concat(oappTemplate.InstalledByAvatarUsername, " (", oappTemplate.InstalledBy.ToString(), ")") : "None"));
            CLIEngine.ShowMessage(string.Concat($"Installed Path:                             ", !string.IsNullOrEmpty(oappTemplate.InstalledPath) ? oappTemplate.InstalledPath : "None"));
            CLIEngine.ShowDivider();
        }

        
        private static OASISResult<IEnumerable<IOAPPTemplate>> ListOAPPTemplates(OASISResult<IEnumerable<IOAPPTemplate>> oappTemplates, bool showNumbers = false)
        {
            if (oappTemplates != null)
            {
                if (!oappTemplates.IsError)
                {
                    if (oappTemplates.Result != null && oappTemplates.Result.Count() > 0)
                    {
                        Console.WriteLine();

                        if (oappTemplates.Result.Count() == 1)
                            CLIEngine.ShowMessage($"{oappTemplates.Result.Count()} OAPP Template Found:");
                        else
                            CLIEngine.ShowMessage($"{oappTemplates.Result.Count()} OAPP Templates Found:");

                        CLIEngine.ShowDivider();

                        int number = 0;
                        foreach (IOAPPTemplate oappTemplate in oappTemplates.Result)
                        {
                            if (showNumbers)
                                number++;

                            ShowOAPPTemplate(oappTemplate, true, number);
                        }

                        //ShowOAPPTemplateListFooter();
                    }
                    else
                        CLIEngine.ShowWarningMessage("No OAPP Templates Found.");
                }
                else
                    CLIEngine.ShowErrorMessage($"Error occured loading OAPP Templates. Reason: {oappTemplates.Message}");
            }
            else
                CLIEngine.ShowErrorMessage($"Unknown error occured loading OAPP Templates.");

            return oappTemplates;
        }

        //private static OASISResult<IEnumerable<IOAPPTemplate>> ListOAPPTemplates(OASISResult<IEnumerable<IOAPPTemplate>> oappTemplates)
        //{
        //    if (oappTemplates != null)
        //    {
        //        if (!oappTemplates.IsError)
        //        {
        //            if (oappTemplates.Result != null && oappTemplates.Result.Count() > 0)
        //            {
        //                Console.WriteLine();

        //                if (oappTemplates.Result.Count() == 1)
        //                    CLIEngine.ShowMessage($"{oappTemplates.Result.Count()} OAPP Template Found:");
        //                else
        //                    CLIEngine.ShowMessage($"{oappTemplates.Result.Count()} OAPP Templates Found:");

        //                CLIEngine.ShowDivider();

        //                foreach (IOAPPTemplate oappTemplate in oappTemplates.Result)
        //                    ShowOAPPTemplate(oappTemplate.OAPPTemplateDNA);

        //                //ShowOAPPTemplateListFooter();
        //            }
        //            else
        //                CLIEngine.ShowWarningMessage("No OAPP Templates Found.");
        //        }
        //        else
        //            CLIEngine.ShowErrorMessage($"Error occured loading OAPP Templates. Reason: {oappTemplates.Message}");
        //    }
        //    else
        //        CLIEngine.ShowErrorMessage($"Unknown error occured loading OAPP Templates.");

        //    return oappTemplates;
        //}

        private static void ListInstalledOAPPTemplates(OASISResult<IEnumerable<IInstalledOAPPTemplate>> oappTemplates)
        {
            if (oappTemplates != null)
            {
                if (!oappTemplates.IsError)
                {
                    if (oappTemplates.Result != null && oappTemplates.Result.Count() > 0)
                    {
                        Console.WriteLine();

                        if (oappTemplates.Result.Count() == 1)
                            CLIEngine.ShowMessage($"{oappTemplates.Result.Count()} OAPP Template Found:");
                        else
                            CLIEngine.ShowMessage($"{oappTemplates.Result.Count()} OAPP Templates Found:");

                        CLIEngine.ShowDivider();

                        foreach (IInstalledOAPPTemplate oappTemplate in oappTemplates.Result)
                            ShowInstalledOAPPTemplate(oappTemplate);

                        //ShowOAPPTemplateListFooter();
                    }
                    else
                        CLIEngine.ShowWarningMessage("No OAPP Templates Found.");
                }
                else
                    CLIEngine.ShowErrorMessage($"Error occured loading OAPP Templates. Reason: {oappTemplates.Message}");
            }
            else
                CLIEngine.ShowErrorMessage($"Unknown error occured loading OAPP Templates.");
        }

        private static async Task<OASISResult<IOAPPTemplate>> LoadOAPPTemplateForProviderAsync(string idOrName, string operationName, ProviderType providerType, bool addSpace = true, bool simpleWizard = true)
        {
            OASISResult<IOAPPTemplate> result = new OASISResult<IOAPPTemplate>();
            ProviderType largeFileProviderType = ProviderType.IPFSOASIS;

            if (!simpleWizard)
            {
                if (!CLIEngine.GetConfirmation("Do you wish to download from the cloud or from the OASIS? Press 'Y' for the cloud or N' for the OASIS."))
                {
                    Console.WriteLine("");
                    object largeProviderTypeObject = CLIEngine.GetValidInputForEnum("What OASIS provider do you wish to install the OAPP Template from? (The default is IPFSOASIS)", typeof(ProviderType));

                    if (largeProviderTypeObject != null)
                    {
                        largeFileProviderType = (ProviderType)largeProviderTypeObject;
                        result = await LoadOAPPTemplateAsync(idOrName, operationName, largeFileProviderType, addSpace);
                    }
                    else
                        OASISErrorHandling.HandleError(ref result, "Error occured in LoadOAPPTemplateForProviderAsync, reason: largeProviderTypeObject is null!");
                }
                else
                {
                    Console.WriteLine("");
                    result = await LoadOAPPTemplateAsync(idOrName, operationName, providerType, addSpace);
                }
            }
            else
                result = await LoadOAPPTemplateAsync(idOrName, operationName, providerType, addSpace);

            return result;
        }

        private static OASISResult<IOAPPTemplate> LoadOAPPTemplateForProvider(string idOrName, string operationName, ProviderType providerType, bool addSpace = true, bool simpleWizard = true)
        {
            OASISResult<IOAPPTemplate> result = new OASISResult<IOAPPTemplate>();
            ProviderType largeFileProviderType = ProviderType.IPFSOASIS;

            if (!simpleWizard)
            {
                if (!CLIEngine.GetConfirmation("Do you wish to download from the cloud or from the OASIS? Press 'Y' for the cloud or N' for the OASIS."))
                {
                    Console.WriteLine("");
                    object largeProviderTypeObject = CLIEngine.GetValidInputForEnum("What OASIS provider do you wish to install the OAPP Template from? (The default is IPFSOASIS)", typeof(ProviderType));

                    if (largeProviderTypeObject != null)
                    {
                        largeFileProviderType = (ProviderType)largeProviderTypeObject;
                        result = LoadOAPPTemplate(idOrName, operationName, largeFileProviderType, addSpace);
                    }
                    else
                        OASISErrorHandling.HandleError(ref result, "Error occured in LoadOAPPTemplateForProvider, reason: largeProviderTypeObject is null!");
                }
                else
                {
                    Console.WriteLine("");
                    result = LoadOAPPTemplate(idOrName, operationName, providerType, addSpace);
                }
            }
            else
                result = LoadOAPPTemplate(idOrName, operationName, providerType, addSpace);

            return result;
        }

        private static async Task<OASISResult<IOAPPTemplate>> LoadOAPPTemplateAsync(string idOrName, string operationName, ProviderType providerType = ProviderType.Default, bool addSpace = true)
        {
            OASISResult<IOAPPTemplate> result = new OASISResult<IOAPPTemplate>();
            Guid id = Guid.Empty;

            do
            {
                if (string.IsNullOrEmpty(idOrName))
                {
                    if (!CLIEngine.GetConfirmation($"Do you know the GUID/ID or Name of the OAPP Template you wish to {operationName}? Press 'Y' for Yes or 'N' for No."))
                        ListOAPPTemplates(await STAR.OASISAPI.OAPPTemplates.LoadAllOAPPTemplatesAsync(OAPPTemplateType.All, 0, providerType));
                    else
                        Console.WriteLine("");

                    idOrName = CLIEngine.GetValidInput($"What is the GUID/ID or Name of the OAPP Template you wish to {operationName}?");
                }

                if (addSpace)
                    Console.WriteLine("");

                if (Guid.TryParse(idOrName, out id))
                {
                    CLIEngine.ShowWorkingMessage("Loading OAPP Template...");
                    result = await STAR.OASISAPI.OAPPTemplates.LoadOAPPTemplateAsync(id, providerType);
                }
                else
                {
                    CLIEngine.ShowWorkingMessage("Searching OAPP Templates...");
                    OASISResult<IEnumerable<IOAPPTemplate>> searchResults = await STAR.OASISAPI.OAPPTemplates.SearchOAPPTemplatesAsync(idOrName, 0, providerType);

                    if (searchResults != null && searchResults.Result != null && !searchResults.IsError)
                    {
                        if (searchResults.Result.Count() > 1)
                        {
                            ListOAPPTemplates(searchResults, true);

                            do
                            {
                                int number = CLIEngine.GetValidInputForInt($"What is the number of the OAPP Template you wish to {operationName}?");

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
                            CLIEngine.ShowWarningMessage("No OAPP Template Found!");
                        }
                    }
                    else
                        CLIEngine.ShowErrorMessage($"An error occured calling STAR.OASISAPI.OAPPTemplates.SearchOAPPTemplatesAsync. Reason: {searchResults.Message}");
                }

                if (result.Result != null && result.Result.OAPPTemplateDNA != null && result.Result.OAPPTemplateDNA.Versions > 1)
                {
                    if (!CLIEngine.GetConfirmation($"{result.Result.OAPPTemplateDNA.Versions} versions were found. Do you wish to {operationName} the latest version ({result.Result.OAPPTemplateDNA.Version})?"))
                    {
                        Console.WriteLine("");
                        CLIEngine.ShowWorkingMessage("Loading OAPP Template Versions...");
                        OASISResult<IEnumerable<IOAPPTemplate>> versionsResult = await STAR.OASISAPI.OAPPTemplates.LoadOAPPTemplateVersionsAsync(result.Result.OAPPTemplateDNA.Id, providerType);
                        ListOAPPTemplates(versionsResult, true);
                        bool versionSelected = false;

                        do
                        {
                            int version = CLIEngine.GetValidInputForInt("What version do you wish to install?");

                            if (version > 0 && version <= versionsResult.Result.Count())
                            {
                                CLIEngine.ShowWorkingMessage("Loading OAPP Template Version...");
                                OASISResult<IOAPPTemplate> versionLoadResult = await STAR.OASISAPI.OAPPTemplates.LoadOAPPTemplateVersionAsync(result.Result.OAPPTemplateDNA.Id, versionsResult.Result.ElementAt(version - 1).OAPPTemplateDNA.Version, providerType);

                                if (versionLoadResult != null && versionLoadResult.Result != null && !versionLoadResult.IsError)
                                {
                                    result.Result = versionLoadResult.Result;
                                    versionSelected = true;
                                }
                                else
                                    CLIEngine.ShowErrorMessage($"An error occured loading the OAPP Template Version. Reason: {versionLoadResult.Message}");
                            }
                            else
                                CLIEngine.ShowErrorMessage("Invalid version entered. Please try again.");

                        } while (!versionSelected);
                    }
                }

                if (idOrName == "exit")
                    break;
            }
            while (result.Result == null || result.IsError);

            return result;
        }

        private static OASISResult<IOAPPTemplate> LoadOAPPTemplate(string idOrName, string operationName, ProviderType providerType = ProviderType.Default, bool addSpace = true)
        {
            OASISResult<IOAPPTemplate> result = new OASISResult<IOAPPTemplate>();
            Guid id = Guid.Empty;

            if (string.IsNullOrEmpty(idOrName))
                idOrName = CLIEngine.GetValidInput($"What is the GUID/ID or Name to the OAPP Template you wish to {operationName}?");

            if (addSpace)
                Console.WriteLine("");

            CLIEngine.ShowWorkingMessage("Loading OAPP Template...");

            if (Guid.TryParse(idOrName, out id))
                result = STAR.OASISAPI.OAPPTemplates.LoadOAPPTemplate(id, providerType);
            else
            {
                OASISResult<IEnumerable<IOAPPTemplate>> searchResults = STAR.OASISAPI.OAPPTemplates.SearchOAPPTemplates(idOrName, 0, providerType);

                OASISResult<IEnumerable<IOAPPTemplate>> allOAPPsTemplatesResult = STAR.OASISAPI.OAPPTemplates.LoadAllOAPPTemplates();

                if (allOAPPsTemplatesResult != null && allOAPPsTemplatesResult.Result != null && !allOAPPsTemplatesResult.IsError)
                {
                    result.Result = allOAPPsTemplatesResult.Result.FirstOrDefault(x => x.Name == idOrName); //TODO: In future will use Where instead so user can select which OAPP Template they want... (if more than one matches the given name).

                    if (result.Result == null)
                    {
                        result.IsError = true;
                        result.Message = "No OAPP Template Was Found!";
                    }
                }
                else
                    CLIEngine.ShowErrorMessage($"An error occured calling STAR.OASISAPI.OAPPTemplates.LoadOAPPTemplate. Reason: {allOAPPsTemplatesResult.Message}");
            }

            return result;
        }

        private static async Task<OASISResult<IInstalledOAPPTemplate>> InstallOAPPTemplateAsync(IOAPPTemplate oappTemplate, string downloadPath, string installPath, bool install = true, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IInstalledOAPPTemplate> result = new OASISResult<IInstalledOAPPTemplate>();

            if (install)
                result = await STAR.OASISAPI.OAPPTemplates.DownloadAndInstallOAPPTemplateAsync(STAR.BeamedInAvatar.Id, oappTemplate, installPath, downloadPath, true, providerType);
            else
            {
                OASISResult<IDownloadedOAPPTemplate> downloadResult = await STAR.OASISAPI.OAPPTemplates.DownloadOAPPTemplateAsync(STAR.BeamedInAvatar.Id, oappTemplate, downloadPath, providerType);

                if (downloadResult != null && downloadResult.Result != null && !downloadResult.IsError)
                {
                    result.Result = new InstalledOAPPTemplate() { OAPPTemplateDNA = downloadResult.Result.OAPPTemplateDNA };
                    result.Result.DownloadedOAPPTemplate = downloadResult.Result;
                }
                else
                {
                    result.Message = downloadResult.Message;
                    result.IsError = true;
                }
            }

            return result;
        }

        private static void OAPPTemplates_OnOAPPTemplatePublishStatusChanged(object sender, OAPPTemplatePublishStatusEventArgs e)
        {
            switch (e.Status)
            {
                //case OAPPPublishStatus.DotNetPublishing:
                //    CLIEngine.ShowWorkingMessage($"Dotnet Publishing...");
                //    break;

                case OAPPTemplatePublishStatus.Uploading:
                    CLIEngine.ShowMessage("Uploading...");
                    Console.WriteLine("");
                    //CLIEngine.ShowWorkingMessage("Uploading... 0%");
                    //CLIEngine.BeginWorkingMessage("Uploading... 0%");
                    //CLIEngine.ShowProgressBar(0);
                    break;

                case OAPPTemplatePublishStatus.Published:
                    CLIEngine.ShowSuccessMessage("OAPP Template Published Successfully");
                    break;

                case OAPPTemplatePublishStatus.Error:
                    CLIEngine.ShowErrorMessage(e.ErrorMessage);
                    break;

                default:
                    CLIEngine.ShowWorkingMessage($"{Enum.GetName(typeof(OAPPTemplatePublishStatus), e.Status)}...");
                    break;
            }
        }

        private static void OAPPTemplates_OnOAPPTemplateUploadStatusChanged(object sender, OAPPTemplateUploadProgressEventArgs e)
        {
            //CLIEngine.ShowProgressBar(e.Progress, true);
            //CLIEngine.ShowProgressBar(e.Progress);
            //CLIEngine.UpdateWorkingMessageWithPercent(e.Progress);
            //CLIEngine.UpdateWorkingMessage($"Uploading... {e.Progress}%"); //was this one.
            CLIEngine.ShowProgressBar((double)e.Progress / (double)100);
        }

        private static void OAPPTemplates_OnOAPPTemplateInstallStatusChanged(object sender, OAPPTemplateInstallStatusEventArgs e)
        {
            switch (e.Status)
            {
                case OAPPTemplateInstallStatus.Downloading:
                    //CLIEngine.BeginWorkingMessage("Downloading...");
                    //Console.WriteLine("");
                    CLIEngine.ShowMessage("Downloading...");
                    Console.WriteLine("");
                    break;

                case OAPPTemplateInstallStatus.Installed:
                    CLIEngine.ShowSuccessMessage("OAPP Template Installed Successfully");
                    break;

                case OAPPTemplateInstallStatus.Error:
                    CLIEngine.ShowErrorMessage(e.ErrorMessage);
                    break;

                default:
                    CLIEngine.ShowWorkingMessage($"{Enum.GetName(typeof(OAPPInstallStatus), e.Status)}...");
                    break;
            }
        }

        private static void OAPPTemplates_OnOAPPTemplateDownloadStatusChanged(object sender, OAPPTemplateDownloadProgressEventArgs e)
        {
            //CLIEngine.UpdateWorkingMessage($"Downloading... {e.Progress}%");
            CLIEngine.ShowProgressBar((double)e.Progress / (double)100);
        }
    }
}