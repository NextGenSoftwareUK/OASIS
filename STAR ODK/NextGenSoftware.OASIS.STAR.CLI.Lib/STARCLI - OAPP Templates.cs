using System.Diagnostics;
using NextGenSoftware.CLI.Engine;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.ONode.Core.Interfaces;
using NextGenSoftware.OASIS.API.ONode.Core.Interfaces.Holons;
using NextGenSoftware.OASIS.API.ONode.Core.Enums;
using NextGenSoftware.OASIS.API.ONODE.Core.Events;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;

namespace NextGenSoftware.OASIS.STAR.CLI.Lib
{
    public static partial class STARCLI
    {
        public static async Task CreateOAPPTemplateAsync(ProviderType providerType = ProviderType.Default)
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
                    oappTemplatePath = Path.Combine(STAR.STARDNA.BasePath, STAR.STARDNA.DefaultOAPPTemplatePath);
                else
                    oappTemplatePath = STAR.STARDNA.DefaultOAPPTemplatePath;

                if (!CLIEngine.GetConfirmation($"Do you wish to create the OAPP Template in the default path defined in the STARDNA as 'OAPPDNATemplatePath'? The current path points to: {oappTemplatePath}"))
                    oappTemplatePath = CLIEngine.GetValidFolder("Where do you wish to create the OAPP Template?");

                oappTemplatePath = Path.Combine(oappTemplatePath, OAPPTemplateName);

                Console.WriteLine("");
                CLIEngine.ShowWorkingMessage("Generating OAPP Template...");
                OASISResult<IOAPPTemplateDNA> oappTemplateResult = await STAR.OASISAPI.OAPPTemplates.CreateOAPPTemplateAsync(OAPPTemplateName, OAPPTemplateDesc, OAPPTemplateType, STAR.BeamedInAvatar.Id, oappTemplatePath, providerType);

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

        public static async Task EditOAPPTemplateAsync(string idOrName = "", ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IOAPPTemplate> loadResult = await LoadOAPPTemplateAsync(idOrName, "edit", providerType);

            if (loadResult != null && loadResult.Result != null && !loadResult.IsError)
            {
                ShowOAPPTemplate(loadResult.Result.OAPPTemplateDNA);

                //TODO: Comeback to this.
                loadResult.Result.Name = CLIEngine.GetValidInput("What is the name of the OAPP Template?");
                loadResult.Result.Description = CLIEngine.GetValidInput("What is the description of the OAPP Template?");

                OASISResult<IOAPPTemplate> result = await STAR.OASISAPI.OAPPTemplates.SaveOAPPTemplateAsync(loadResult.Result, STAR.BeamedInAvatar.Id, providerType);
                CLIEngine.ShowWorkingMessage("Saving OAPP Template...");

                if (result != null && !result.IsError && result.Result != null)
                {
                    CLIEngine.ShowSuccessMessage("OAPP Template Successfully Updated.");
                    ShowOAPPTemplate(result.Result.OAPPTemplateDNA);
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
                ShowOAPPTemplate(result.Result.OAPPTemplateDNA);

                if (CLIEngine.GetConfirmation("Are you sure you wish to delete the OAPP Template?"))
                {
                    CLIEngine.ShowWorkingMessage("Deleting OAPP Template...");
                    result = await STAR.OASISAPI.OAPPTemplates.DeleteOAPPTemplateAsync(result.Result, true, providerType);

                    if (result != null && !result.IsError && result.Result != null)
                    {
                        CLIEngine.ShowSuccessMessage("OAPP Template Successfully Deleted.");
                        ShowOAPPTemplate(result.Result.OAPPTemplateDNA);
                    }
                    else
                        CLIEngine.ShowErrorMessage($"An error occured deleting the OAPP Template. Reason: {result.Message}");
                }
            }
            else
                CLIEngine.ShowErrorMessage($"An error occured loading the OAPP Template. Reason: {result.Message}");
        }

        public static async Task PublishOAPPTemplateAsync(string oappTemplatePath = "", ProviderType providerType = ProviderType.Default)
        //public static async Task PublishOAPPAsync(string oappPath = "", ProviderType providerType = ProviderType.Default)
        {
            bool generateOAPPSource = false;
            bool uploadOAPPSource = false;
            bool generateOAPP = true;
            //bool uploadOAPP = true;
            bool uploadOAPPToCloud = false;
            bool generateOAPPSelfContained = false;
            //bool uploadOAPPSelfContained = false;
            bool uploadOAPPSelfContainedToCloud = false;
            bool generateOAPPSelfContainedFull = false;
            //bool uploadOAPPSelfContainedFull = false;
            bool uploadOAPPSelfContainedFullToCloud = false;
            bool makeOAPPSourcePublic = false;
            ProviderType OAPPBinaryProviderType = providerType; //ProviderType.IPFSOASIS;
            ProviderType OAPPSelfContainedBinaryProviderType = ProviderType.IPFSOASIS; //ProviderType.IPFSOASIS;
            ProviderType OAPPSelfContainedFullBinaryProviderType = ProviderType.IPFSOASIS; //ProviderType.IPFSOASIS;
            string launchTarget = "";
            string publishPath = "";
            string launchTargetQuestion = "";
            // bool publishDotNot = false;

            if (string.IsNullOrEmpty(oappTemplatePath))
            {
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
                        //launchTarget = $"bin\\Release\\net8.0\\{OAPPDNAResult.Result.OAPPName}.exe"; //TODO: For this line to work need to remove the namespace question so it just uses the OAPPName as the namespace. //TODO: Eventually this will be set in the OAPPTemplate and/or can also be set when I add the command line dotnet publish integration.
                        break;

                    case OAPPTemplateType.Blazor:
                    case OAPPTemplateType.MAUI:
                    case OAPPTemplateType.WebMVC:
                        //launchTarget = $"bin\\Release\\net8.0\\index.html"; 
                        launchTarget = $"index.html";
                        break;
                }

                if (!string.IsNullOrEmpty(launchTarget))
                {
                    if (!CLIEngine.GetConfirmation($"{launchTargetQuestion} Do you wish to use the following default launch target: {launchTarget}?"))
                        launchTarget = CLIEngine.GetValidFile("What launch target do you wish to use? ", oappTemplatePath);
                    else
                        launchTarget = Path.Combine(oappTemplatePath, launchTarget);
                }
                else
                    launchTarget = CLIEngine.GetValidFile(launchTargetQuestion, oappTemplatePath);

                //if (CLIEngine.GetConfirmation("Do you wish to upload/publish the .oapptemplate file to STARNET?"))
                //{
                //    if (CLIEngine.GetConfirmation("Do you wish to upload/publish the .oapptemplate file to cloud storage?"))
                //        uploadOAPPToCloud = true;

                //    object OAPPBinaryProviderTypeObject = CLIEngine.GetValidInputForEnum("Do you wish to upload/publish the .oapptemplate file to The OASIS? If so what provider do you wish to upload to? If you do not wish to then enter 'None'.", typeof(ProviderType));

                //    if (OAPPBinaryProviderTypeObject != null)
                //    {
                //        if (OAPPBinaryProviderTypeObject.ToString() == "exit")
                //            return;
                //        else
                //            OAPPBinaryProviderType = (ProviderType)OAPPBinaryProviderTypeObject;
                //    }
                //}

                //if (!uploadOAPPToCloud && OAPPBinaryProviderType == ProviderType.None)
                //    CLIEngine.ShowMessage("Since you did not select to upload to the cloud or OASIS storage the oapptemplate will NOT be published to STARNET.");

                Console.WriteLine("");
                bool registerOnSTARNET = CLIEngine.GetConfirmation("Do you wish to publish to STARNET? If you select 'Y' to this question then your OAPP Template will be published to STARNET where others will be able to find, download and install. If you select 'N' then only the .oapptemplate install file will be generated on your local device, which you can distribute as you please. This file will also be generated even if you publish to STARNET.");
                Console.WriteLine("");

                if (registerOnSTARNET)
                {
                    CLIEngine.ShowMessage("Do you wish to publish/upload the .oapptemplate file to an OASIS Provider or to the cloud or both? Depending on which OASIS Provider is chosen such as IPFSOASIS there may issues such as speed, relialbility etc for such a large file. If you choose to upload to the cloud this could be faster and more reliable (but there is a limit of 5 OAPPs on the free plan and you will need to upgrade to upload more than 5 OAPPs). You may want to choose to use both to add an extra layer of redundancy (recommended).");

                    if (CLIEngine.GetConfirmation("Do you wish to upload to the cloud?"))
                        uploadOAPPToCloud = true;

                    if (CLIEngine.GetConfirmation("Do you wish to upload to an OASIS Provider? Make sure you select a provider that can handle large files such as IPFSOASIS, HoloOASIS etc. Also remember the OASIS Hyperdrive will only be able to auto-replicate to other providers that also support large files and are free or cost effective. By default it will NOT auto-replicate large files, you will need to manually configure this in your OASIS Profile settings."))
                    {
                        object largeProviderTypeObject = CLIEngine.GetValidInputForEnum("What provider do you wish to publish the OAPP to? (The default is IPFSOASIS)", typeof(ProviderType));

                        if (largeProviderTypeObject != null)
                            OAPPBinaryProviderType = (ProviderType)largeProviderTypeObject;
                    }
                }

                if (Path.IsPathRooted(STAR.STARDNA.DefaultPublishedOAPPTemplatesPath))
                    publishPath = STAR.STARDNA.DefaultPublishedOAPPTemplatesPath;
                else
                    publishPath = Path.Combine(STAR.STARDNA.BasePath, STAR.STARDNA.DefaultPublishedOAPPTemplatesPath);

                //Console.WriteLine("");
                if (!CLIEngine.GetConfirmation($"Do you wish to publish the OAPP Template to the default publish folder defined in the STARDNA as DefaultPublishedOAPPTemplatesPath : {publishPath}?"))
                {
                    if (CLIEngine.GetConfirmation($"Do you wish to publish the OAPP Template to: {Path.Combine(oappTemplatePath, "Published")}?"))
                        publishPath = Path.Combine(oappTemplatePath, "Published");
                    else
                        publishPath = CLIEngine.GetValidFolder("Where do you wish to publish the OAPP Template?", true);
                }

                Console.WriteLine("");
                CLIEngine.ShowWorkingMessage("Publishing OAPP Template...");

                STAR.OASISAPI.OAPPTemplates.OnOAPPTemplatePublishStatusChanged += OAPPTemplates_OnOAPPTemplatePublishStatusChanged;
                STAR.OASISAPI.OAPPTemplates.OnOAPPTemplateUploadStatusChanged += OAPPTemplates_OnOAPPTemplateUploadStatusChanged;
                OASISResult<IOAPPTemplateDNA> publishResult = await STAR.OASISAPI.OAPPTemplates.PublishOAPPTemplateAsync(oappTemplatePath, launchTarget, STAR.BeamedInAvatar.Id, publishPath, registerOnSTARNET, generateOAPP, uploadOAPPToCloud, providerType, OAPPBinaryProviderType);
                STAR.OASISAPI.OAPPTemplates.OnOAPPTemplateUploadStatusChanged -= OAPPTemplates_OnOAPPTemplateUploadStatusChanged;
                STAR.OASISAPI.OAPPTemplates.OnOAPPTemplatePublishStatusChanged -= OAPPTemplates_OnOAPPTemplatePublishStatusChanged;

                if (publishResult != null && !publishResult.IsError && publishResult.Result != null)
                {
                    CLIEngine.ShowSuccessMessage("OAPP Template Successfully Published.");
                    ShowOAPPTemplate(publishResult.Result);

                    if (CLIEngine.GetConfirmation("Do you wish to install the OAPP Template now?"))
                        await InstallOAPPTemplateAsync(publishResult.Result.Id.ToString());

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
                OASISResult<IOAPPTemplateDNA> unpublishResult = await STAR.OASISAPI.OAPPTemplates.UnPublishOAPPTemplateAsync(result.Result, STAR.BeamedInAvatar.Id, providerType);

                if (unpublishResult != null && !unpublishResult.IsError && unpublishResult.Result != null)
                {
                    CLIEngine.ShowSuccessMessage("OAPP Template Successfully Unpublished.");
                    ShowOAPPTemplate(unpublishResult.Result);
                }
                else
                    CLIEngine.ShowErrorMessage($"An error occured unpublishing the OAPP Template. Reason: {unpublishResult.Message}");
            }
        }
        public static async Task<OASISResult<IInstalledOAPPTemplate>> InstallOAPPTemplateAsync(string idOrName = "", ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IInstalledOAPPTemplate> installResult = new OASISResult<IInstalledOAPPTemplate>();
            string installPath = "";

            if (Path.IsPathRooted(STAR.STARDNA.DefaultInstalledOAPPTemplatesPath))
                installPath = STAR.STARDNA.DefaultInstalledOAPPTemplatesPath;
            else
                installPath = Path.Combine(STAR.STARDNA.BasePath, STAR.STARDNA.DefaultInstalledOAPPTemplatesPath);

            Console.WriteLine("");

            if (!CLIEngine.GetConfirmation($"Do you wish to install the OAPP Template to the default install folder defined in the STARDNA as DefaultInstalledOAPPTemplatesPath : {installPath}?"))
                installPath = CLIEngine.GetValidFolder("What is the full path to where you wish to install the OAPP Template?", true);

            if (!string.IsNullOrEmpty(idOrName))
            {
                Console.WriteLine("");
                ProviderType largeFileProviderType = ProviderType.IPFSOASIS;
                object largeProviderTypeObject = CLIEngine.GetValidInputForEnum("What provider do you wish to install the OAPP Template from? (The default is IPFSOASIS)", typeof(ProviderType));

                if (largeProviderTypeObject != null)
                {
                    largeFileProviderType = (ProviderType)largeProviderTypeObject;
                    OASISResult<IOAPPTemplate> result = await LoadOAPPTemplateAsync(idOrName, "install", largeFileProviderType, false);

                    if (result != null && result.Result != null && !result.IsError)
                    {
                        STAR.OASISAPI.OAPPTemplates.OnOAPPTemplateDownloadStatusChanged += OAPPTemplates_OnOAPPTemplateDownloadStatusChanged;
                        STAR.OASISAPI.OAPPTemplates.OnOAPPTemplateInstallStatusChanged += OAPPTemplates_OnOAPPTemplateInstallStatusChanged;
                        CLIEngine.ShowWorkingMessage("Installing OAPP Template...");
                        STAR.OASISAPI.OAPPTemplates.OnOAPPTemplateDownloadStatusChanged -= OAPPTemplates_OnOAPPTemplateDownloadStatusChanged;
                        STAR.OASISAPI.OAPPTemplates.OnOAPPTemplateInstallStatusChanged -= OAPPTemplates_OnOAPPTemplateInstallStatusChanged;
                    }
                }
            }
            else
            {
                Console.WriteLine("");
                if (CLIEngine.GetConfirmation("Do you wish to install the OAPP Template from a local .oapptemplate file or from STARNET? Press 'Y' for local .oapptemplate file or 'N' for STARNET."))
                {
                    Console.WriteLine("");
                    string oappPath = CLIEngine.GetValidFile("What is the full path to the .oapptemplate file?");

                    CLIEngine.ShowWorkingMessage("Installing OAPP Template...");
                    installResult = await STAR.OASISAPI.OAPPTemplates.InstallOAPPTemplateAsync(STAR.BeamedInAvatar.Id, oappPath, installPath, true, providerType);
                }
                else
                    await LaunchSTARNETAsync(true);
            }

            if (installResult != null)
            {
                if (!installResult.IsError && installResult.Result != null)
                {
                    CLIEngine.ShowSuccessMessage("OAPP Template Successfully Installed.");
                    ShowInstalledOAPPTemplate(installResult.Result);

                    if (CLIEngine.GetConfirmation("Do you wish to open the folder to the OAPP Template now?"))
                        STAR.OASISAPI.OAPPTemplates.OpenOAPPTemplateFolder(STAR.BeamedInAvatar.Id, installResult.Result);
                }
                else
                    CLIEngine.ShowErrorMessage($"Error installing OAPP Template. Reason: {installResult.Message}");
            }
            else
                CLIEngine.ShowErrorMessage($"Error installing OAPP Template. Reason: Unknown error occured!");

            return installResult;
        }

        public static OASISResult<IInstalledOAPPTemplate> InstallOAPPTemplate(string idOrName = "", ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IInstalledOAPPTemplate> installResult = new OASISResult<IInstalledOAPPTemplate>();
            string installPath = "";

            if (Path.IsPathRooted(STAR.STARDNA.DefaultInstalledOAPPsPath))
                installPath = STAR.STARDNA.DefaultInstalledOAPPsPath;
            else
                installPath = Path.Combine(STAR.STARDNA.BasePath, STAR.STARDNA.DefaultInstalledOAPPsPath);

            if (!CLIEngine.GetConfirmation($"Do you wish to install the OAPP Template to the default install folder defined in the STARDNA as DefaultInstalledOAPPTemplatesPath : {installPath}?"))
                installPath = CLIEngine.GetValidFolder("What is the full path to where you wish to install the OAPP Template?", true);

            if (!string.IsNullOrEmpty(idOrName))
            {
                OASISResult<IOAPPTemplate> result = LoadOAPPTemplate(idOrName, "install", providerType);

                if (result != null && result.Result != null && !result.IsError)
                    installResult = STAR.OASISAPI.OAPPTemplates.InstallOAPPTemplate(STAR.BeamedInAvatar.Id, result.Result, installPath, true, providerType);

                //installResult = await STAR.OASISAPI.OAPPs.InstallOAPPAsync(STAR.BeamedInAvatar.Id, id, installPath, providerType);
            }
            else
            {
                Console.WriteLine("");
                if (CLIEngine.GetConfirmation("Do you wish to install the OAPP Template from a local .oapptemplate file or from STARNET? Press 'Y' for local .oapptemplate file or 'N' for STARNET."))
                {
                    Console.WriteLine("");
                    string oappPath = CLIEngine.GetValidFile("What is the full path to the .oapptemplate file?");

                    CLIEngine.ShowWorkingMessage("Installing OAPP Template...");
                    installResult = STAR.OASISAPI.OAPPTemplates.InstallOAPPTemplate(STAR.BeamedInAvatar.Id, oappPath, installPath, true, providerType);
                }
                else
                    LaunchSTARNETAsync(true);
            }

            if (installResult != null)
            {
                if (!installResult.IsError && installResult.Result != null)
                {
                    CLIEngine.ShowSuccessMessage("OAPP Template Successfully Installed.");
                    ShowInstalledOAPPTemplate(installResult.Result);

                    if (CLIEngine.GetConfirmation("Do you wish to open the folder to the OAPP Template now?"))
                        STAR.OASISAPI.OAPPTemplates.OpenOAPPTemplateFolder(STAR.BeamedInAvatar.Id, installResult.Result);
                }
                else
                    CLIEngine.ShowErrorMessage($"Error installing OAPP Template. Reason: {installResult.Message}");
            }
            else
                CLIEngine.ShowErrorMessage($"Error installing OAPP Template. Reason: Unknown error occured!");

            return installResult;
        }

        public static async Task UnInstallOAPPTemplateAsync(string idOrName = "", ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IOAPPTemplate> result = await LoadOAPPTemplateAsync(idOrName, "uninstall", providerType);

            if (result != null && !result.IsError && result.Result != null)
            {
                OASISResult<IOAPPTemplateDNA> uninstallResult = await STAR.OASISAPI.OAPPTemplates.UnInstallOAPPTemplateAsync(result.Result.OAPPTemplateDNA, STAR.BeamedInAvatar.Id, providerType);

                if (uninstallResult != null)
                {
                    if (!uninstallResult.IsError && uninstallResult.Result != null)
                    {
                        CLIEngine.ShowSuccessMessage("OAPP Template Successfully Uninstalled.");
                        ShowOAPPTemplate(uninstallResult.Result);
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

        public static async Task ListAllOAPPTemplatesAsync(ProviderType providerType = ProviderType.Default)
        {
            ListOAPPs(await STAR.OASISAPI.OAPPs.ListAllOAPPsAsync(providerType));
        }

        public static void ListAllOAPPTemplates(ProviderType providerType = ProviderType.Default)
        {
            ListOAPPs(STAR.OASISAPI.OAPPs.ListAllOAPPs(providerType));
        }

        public static async Task ListOAPPTemplatesCreatedByBeamedInAvatarAsync(ProviderType providerType = ProviderType.Default)
        {
            if (STAR.BeamedInAvatar != null)
                ListOAPPs(await STAR.OASISAPI.OAPPs.ListOAPPsCreatedByAvatarAsync(STAR.BeamedInAvatar.AvatarId));
            else
                CLIEngine.ShowErrorMessage("No Avatar Is Beamed In. Please Beam In First!");
        }

        public static async Task<OASISResult<IEnumerable<IInstalledOAPP>>> ListOAPPTemplatesInstalledForBeamedInAvatarAsync(ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<IInstalledOAPP>> result = new OASISResult<IEnumerable<IInstalledOAPP>>();

            if (STAR.BeamedInAvatar != null)
            {
                result = await STAR.OASISAPI.OAPPs.ListInstalledOAPPsAsync(STAR.BeamedInAvatar.AvatarId);
                ListInstalledOAPPs(result);
            }
            else
                OASISErrorHandling.HandleError(ref result, "No Avatar Is Beamed In. Please Beam In First!");
            //CLIEngine.ShowErrorMessage("No Avatar Is Beamed In. Please Beam In First!");

            return result;
        }

        public static async Task SearchOAPPTemplatesAsync(string searchTerm, ProviderType providerType = ProviderType.Default)
        {
            ListOAPPTemplates(await STAR.OASISAPI.OAPPTemplates.SearchOAPPTemplatesAsync(searchTerm, providerType));
        }

        public static async Task ShowOAPPTemplateAsync(string idOrName = "", ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IOAPPTemplate> result = await LoadOAPPTemplateAsync(idOrName, "view", providerType);

            if (result != null && !result.IsError && result.Result != null)
                ShowOAPPTemplate(result.Result.OAPPTemplateDNA);
            else
                CLIEngine.ShowErrorMessage($"An error occured loading the OAPP Template. Reason: {result.Message}");
        }

        public static void ShowOAPPTemplate(IOAPPTemplateDNA oappTemplateDNA)
        {
            CLIEngine.ShowMessage(string.Concat($"Id:                                         ", oappTemplateDNA.Id != Guid.Empty ? oappTemplateDNA.Id : "None"));
            CLIEngine.ShowMessage(string.Concat($"Name:                                       ", !string.IsNullOrEmpty(oappTemplateDNA.Name) ? oappTemplateDNA.Name : "None"));
            CLIEngine.ShowMessage(string.Concat($"Description:                                ", !string.IsNullOrEmpty(oappTemplateDNA.Description) ? oappTemplateDNA.Description : "None"));
            CLIEngine.ShowMessage(string.Concat($"OAPP Template Type:                         ", Enum.GetName(typeof(OAPPTemplateType), oappTemplateDNA.OAPPTemplateType)));
            CLIEngine.ShowMessage(string.Concat($"Created On:                                 ", oappTemplateDNA.CreatedOn != DateTime.MinValue ? oappTemplateDNA.CreatedOn.ToString() : "None"));
            CLIEngine.ShowMessage(string.Concat($"Created By:                                 ", oappTemplateDNA.CreatedByAvatarId != Guid.Empty ? string.Concat(oappTemplateDNA.CreatedByAvatarUsername, " (", oappTemplateDNA.CreatedByAvatarId.ToString(), ")") : "None"));
            CLIEngine.ShowMessage(string.Concat($"Published On:                               ", oappTemplateDNA.PublishedOn != DateTime.MinValue ? oappTemplateDNA.PublishedOn.ToString() : "None"));
            CLIEngine.ShowMessage(string.Concat($"Published By:                               ", oappTemplateDNA.PublishedByAvatarId != Guid.Empty ? string.Concat(oappTemplateDNA.PublishedByAvatarUsername, " (", oappTemplateDNA.PublishedByAvatarId.ToString(), ")") : "None"));
            CLIEngine.ShowMessage(string.Concat($"OAPP Template Published Path:               ", !string.IsNullOrEmpty(oappTemplateDNA.OAPPTemplatePublishedPath) ? oappTemplateDNA.OAPPTemplatePublishedPath : "None"));
            CLIEngine.ShowMessage(string.Concat($"OAPP Template Filesize:                     ", oappTemplateDNA.OAPPTemplateFileSize.ToString()));
            //CLIEngine.ShowMessage(string.Concat($"OAPP Template Self Contained Published Path:                   ", !string.IsNullOrEmpty(oappTemplateDNA.OAPPSelfContainedPublishedPath) ? oappTemplateDNA.OAPPPublishedPath : "None"));
            //CLIEngine.ShowMessage(string.Concat($"OAPP Self Contained Filesize:                         ", oappTemplateDNA.OAPPSelfContainedFileSize.ToString()));
            //CLIEngine.ShowMessage(string.Concat($"OAPP Self Contained Full Published Path:              ", !string.IsNullOrEmpty(oappTemplateDNA.OAPPSelfContainedFullPublishedPath) ? oappTemplateDNA.OAPPPublishedPath : "None"));
            //CLIEngine.ShowMessage(string.Concat($"OAPP Self Contained Full Filesize:                    ", oappTemplateDNA.OAPPSelfContainedFullFileSize.ToString()));
            CLIEngine.ShowMessage(string.Concat($"OAPP Template Published On STARNET:         ", oappTemplateDNA.OAPPTemplatePublishedOnSTARNET ? "True" : "False"));
            CLIEngine.ShowMessage(string.Concat($"OAPP Template Published To Cloud:           ", oappTemplateDNA.OAPPTemplatePublishedToCloud ? "True" : "False"));
            CLIEngine.ShowMessage(string.Concat($"OAPP Template Published To OASIS Provider:  ", Enum.GetName(typeof(ProviderType), oappTemplateDNA.OAPPTemplatePublishedProviderType)));
            //CLIEngine.ShowMessage(string.Concat($"OAPP Self Contained Published To Cloud:               ", oappTemplateDNA.OAPPSelfContainedPublishedToCloud ? "True" : "False"));
            //CLIEngine.ShowMessage(string.Concat($"OAPP Self Contained Published To OASIS Provider:      ", Enum.GetName(typeof(ProviderType), oappTemplateDNA.OAPPSelfContainedPublishedProviderType)));
            //CLIEngine.ShowMessage(string.Concat($"OAPP Self Contained Full Published To Cloud:          ", oappTemplateDNA.OAPPSelfContainedFullPublishedToCloud ? "True" : "False"));
            //CLIEngine.ShowMessage(string.Concat($"OAPP Self Contained Full Published To OASIS Provider: ", Enum.GetName(typeof(ProviderType), oappTemplateDNA.OAPPSelfContainedFullPublishedProviderType)));{}90
            CLIEngine.ShowMessage(string.Concat($"Version:                                    ", oappTemplateDNA.Version));
            CLIEngine.ShowMessage(string.Concat($"Versions:                                   ", oappTemplateDNA.Versions));
            CLIEngine.ShowMessage(string.Concat($"Downloads:                                  ", oappTemplateDNA.Downloads));
            //CLIEngine.ShowMessage(string.Concat($"STAR ODK Version:                                     ", oappTemplateDNA.STARODKVersion));
            //CLIEngine.ShowMessage(string.Concat($"OASIS Version:                                        ", oappTemplateDNA.OASISVersion));
            //CLIEngine.ShowMessage(string.Concat($"COSMIC Version:                                       ", oappTemplateDNA.COSMICVersion));
            //CLIEngine.ShowMessage(string.Concat($".NET Version:                                         ", oappTemplateDNA.DotNetVersion));

            CLIEngine.ShowDivider();
        }

        public static void ShowInstalledOAPPTemplate(IInstalledOAPPTemplate oappTemplate)
        {
            ShowOAPPTemplate(oappTemplate.OAPPTemplateDNA);
            CLIEngine.ShowMessage(string.Concat($"Installed On:                                         ", oappTemplate.InstalledOn != DateTime.MinValue ? oappTemplate.InstalledOn.ToString() : "None"));
            CLIEngine.ShowMessage(string.Concat($"Installed By:                                         ", oappTemplate.InstalledBy != Guid.Empty ? string.Concat(oappTemplate.InstalledByAvatarUsername, " (", oappTemplate.InstalledBy.ToString(), ")") : "None"));
            CLIEngine.ShowMessage(string.Concat($"Installed Path:                                       ", oappTemplate.InstalledPath));
            CLIEngine.ShowDivider();
        }

        
        private static void ListOAPPTemplates(OASISResult<IEnumerable<IOAPPTemplate>> oappTemplates)
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

                        foreach (IOAPPTemplate oappTemplate in oappTemplates.Result)
                            ShowOAPPTemplate(oappTemplate.OAPPTemplateDNA);

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

        private static async Task<OASISResult<IOAPPTemplate>> LoadOAPPTemplateAsync(string idOrName, string operationName, ProviderType providerType = ProviderType.Default, bool addSpace = true)
        {
            OASISResult<IOAPPTemplate> result = new OASISResult<IOAPPTemplate>();
            Guid id = Guid.Empty;

            if (string.IsNullOrEmpty(idOrName))
                idOrName = CLIEngine.GetValidInput($"What is the GUID/ID or Name to the OAPP Template you wish to {operationName}?");

            if (addSpace)
                Console.WriteLine("");

            CLIEngine.ShowWorkingMessage("Loading OAPP Template...");

            if (Guid.TryParse(idOrName, out id))
                result = await STAR.OASISAPI.OAPPTemplates.LoadOAPPTemplateAsync(id, providerType);
            else
            {
                OASISResult<IEnumerable<IOAPPTemplate>> allOAPPsTemplatesResult = await STAR.OASISAPI.OAPPTemplates.LoadAllOAPPTemplatesAsync();

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
                    CLIEngine.ShowErrorMessage($"An error occured calling STAR.OASISAPI.OAPPTemplates.LoadOAPPTemplateAsync. Reason: {allOAPPsTemplatesResult.Message}");
            }

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
                    CLIEngine.ShowErrorMessage($"An error occured calling STAR.OASISAPI.OAPPTemplates.LoadAllOAPPTemplates. Reason: {allOAPPsTemplatesResult.Message}");
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
                    CLIEngine.BeginWorkingMessage("Downloading...");
                    //CLIEngine.ShowProgressBar(0);
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
            CLIEngine.UpdateWorkingMessage($"Downloading... {e.Progress}%");
        }
    }
}