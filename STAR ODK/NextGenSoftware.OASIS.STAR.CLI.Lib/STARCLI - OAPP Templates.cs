using System.Diagnostics;
using NextGenSoftware.CLI.Engine;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.ONode.Core.Interfaces;
using NextGenSoftware.OASIS.API.ONode.Core.Interfaces.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.ONode.Core.Enums;

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
            CLIEngine.ShowMessage("You then simply run this wizard to convert the folder containing the template (can contain any number of files and sub-folders) into a OAPP Template file (.oapptemplate).");
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
                    oappTemplatePath = Path.Combine(STAR.STARDNA.BasePath, STAR.STARDNA.OAPPDNATemplatePath);
                else
                    oappTemplatePath = STAR.STARDNA.OAPPDNATemplatePath;

                if (!CLIEngine.GetConfirmation($"Do you wish to create the OAPP Template in the default path defined in the STARDNA as 'OAPPDNATemplatePath'? The current path points to: {oappTemplatePath}"))
                    oappTemplatePath = CLIEngine.GetValidFolder("Where do you wish to create the OAPP Template?");

                oappTemplatePath = Path.Combine(oappTemplatePath, OAPPTemplateName);

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
                    else
                        CLIEngine.ShowErrorMessage($"Error Occured: {oappTemplateResult.Message}");
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

        public static async Task SearchOAPPTemplatesAsync(ProviderType providerType = ProviderType.Default)
        {

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
            CLIEngine.ShowMessage(string.Concat($"OAPP Template Published Path:                        ", !string.IsNullOrEmpty(oappTemplateDNA.OAPPTemplatePublishedPath) ? oappTemplateDNA.OAPPTemplatePublishedPath : "None"));
            CLIEngine.ShowMessage(string.Concat($"OAPP Template Filesize:                     ", oappTemplateDNA.OAPPTemplateFileSize.ToString()));
            //CLIEngine.ShowMessage(string.Concat($"OAPP Template Self Contained Published Path:                   ", !string.IsNullOrEmpty(oappTemplateDNA.OAPPSelfContainedPublishedPath) ? oappTemplateDNA.OAPPPublishedPath : "None"));
            //CLIEngine.ShowMessage(string.Concat($"OAPP Self Contained Filesize:                         ", oappTemplateDNA.OAPPSelfContainedFileSize.ToString()));
            //CLIEngine.ShowMessage(string.Concat($"OAPP Self Contained Full Published Path:              ", !string.IsNullOrEmpty(oappTemplateDNA.OAPPSelfContainedFullPublishedPath) ? oappTemplateDNA.OAPPPublishedPath : "None"));
            //CLIEngine.ShowMessage(string.Concat($"OAPP Self Contained Full Filesize:                    ", oappTemplateDNA.OAPPSelfContainedFullFileSize.ToString()));
            CLIEngine.ShowMessage(string.Concat($"OAPP Template Published On STARNET:                            ", oappTemplateDNA.OAPPTemplatePublishedOnSTARNET ? "True" : "False"));
            CLIEngine.ShowMessage(string.Concat($"OAPP Template Published To Cloud:           ", oappTemplateDNA.OAPPTemplatePublishedToCloud ? "True" : "False"));
            CLIEngine.ShowMessage(string.Concat($"OAPP Template Published To OASIS Provider:  ", Enum.GetName(typeof(ProviderType), oappTemplateDNA.OAPPTemplatePublishedProviderType)));
            //CLIEngine.ShowMessage(string.Concat($"OAPP Self Contained Published To Cloud:               ", oappTemplateDNA.OAPPSelfContainedPublishedToCloud ? "True" : "False"));
            //CLIEngine.ShowMessage(string.Concat($"OAPP Self Contained Published To OASIS Provider:      ", Enum.GetName(typeof(ProviderType), oappTemplateDNA.OAPPSelfContainedPublishedProviderType)));
            //CLIEngine.ShowMessage(string.Concat($"OAPP Self Contained Full Published To Cloud:          ", oappTemplateDNA.OAPPSelfContainedFullPublishedToCloud ? "True" : "False"));
            //CLIEngine.ShowMessage(string.Concat($"OAPP Self Contained Full Published To OASIS Provider: ", Enum.GetName(typeof(ProviderType), oappTemplateDNA.OAPPSelfContainedFullPublishedProviderType)));{}90
            CLIEngine.ShowMessage(string.Concat($"Version:                                              ", oappTemplateDNA.Version));
            CLIEngine.ShowMessage(string.Concat($"Versions:                                              ", oappTemplateDNA.Versions));
            CLIEngine.ShowMessage(string.Concat($"Downloads:                                              ", oappTemplateDNA.Downloads));
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
                string OAPPPathQuestion = "What is the full path to the (dotnet) published output for the OAPP you wish to publish?";
                launchTargetQuestion = "What is the relative path (from the root of the path given above, e.g bin\\launch.exe) to the launch target for the OAPP? (This could be the exe or batch file for a desktop or console app, or the index.html page for a website, etc)";
                oappTemplatePath = CLIEngine.GetValidFolder(OAPPPathQuestion, false);
            }

            OASISResult<IOAPPDNA> OAPPDNAResult = await STAR.OASISAPI.OAPPTemplates.ReadOAPPTemplateDNAAsync(oappTemplatePath);

            if (OAPPDNAResult != null && OAPPDNAResult.Result != null && !OAPPDNAResult.IsError)
            {
                switch (OAPPDNAResult.Result.OAPPTemplateType)
                {
                    case OAPPTemplateType.Console:
                    case OAPPTemplateType.WPF:
                    case OAPPTemplateType.WinForms:
                        launchTarget = $"{OAPPDNAResult.Result.OAPPName}.exe"; //TODO: For this line to work need to remove the namespace question so it just uses the OAPPName as the namespace. //TODO: Eventually this will be set in the OAPPTemplate and/or can also be set when I add the command line dotnet publish integration.
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
                        launchTarget = CLIEngine.GetValidFile("What launch target do you wish to use? ", oappPath);
                    else
                        launchTarget = Path.Combine(oappPath, launchTarget);
                }
                else
                    launchTarget = CLIEngine.GetValidFile(launchTargetQuestion, oappPath);

                Console.WriteLine("");
                if (CLIEngine.GetConfirmation("Do you wish to generate the standard .oapp file? (Recommended). This file contains only the built & published OAPP source code. NOTE: You will need to make sure the target machine that runs this OAPP has both the appropriate OASIS & STAR ODK Runtimes installed along with the appropriate .NET Runtime."))
                {
                    generateOAPP = true;

                    if (CLIEngine.GetConfirmation("Do you wish to upload/publish the .oapp file to STARNET?"))
                    {
                        if (CLIEngine.GetConfirmation("Do you wish to upload/publish the .oapp file to cloud storage?"))
                            uploadOAPPToCloud = true;

                        object OAPPBinaryProviderTypeObject = CLIEngine.GetValidInputForEnum("Do you wish to upload/publish the .oapp file to The OASIS? If so what provider do you wish to upload to? If you do not wish to then enter 'None'.", typeof(ProviderType));

                        if (OAPPBinaryProviderTypeObject != null)
                        {
                            if (OAPPBinaryProviderTypeObject.ToString() == "exit")
                                return;
                            else
                                OAPPBinaryProviderType = (ProviderType)OAPPBinaryProviderTypeObject;
                        }
                    }
                }

                Console.WriteLine("");
                if (CLIEngine.GetConfirmation("Do you wish to generate the self-contained .oapp file? This file contains the built & published OAPP source code along with the OASIS & STAR ODK Runtimes. NOTE: You will need to make sure the target machine that runs this OAPP has the appropriate .NET runtime installed. The file will be a minimum of 250 MB."))
                {
                    generateOAPPSelfContained = true;

                    if (CLIEngine.GetConfirmation("Do you wish to upload/publish the self-contained .oapp file to STARNET?"))
                    {
                        if (CLIEngine.GetConfirmation("Do you wish to upload/publish the .oapp file to cloud storage?"))
                            uploadOAPPToCloud = true;

                        object OAPPBinaryProviderTypeObject = CLIEngine.GetValidInputForEnum("Do you wish to upload/publish the .oapp file to The OASIS? If so what provider do you wish to upload to? If you do not wish to then enter 'None'.", typeof(ProviderType));

                        if (OAPPBinaryProviderTypeObject != null)
                        {
                            if (OAPPBinaryProviderTypeObject.ToString() == "exit")
                                return;
                            else
                                OAPPBinaryProviderType = (ProviderType)OAPPBinaryProviderTypeObject;
                        }
                    }
                }

                Console.WriteLine("");
                if (CLIEngine.GetConfirmation("Do you wish to generate the self-contained (full) .oapp file? This file contains the built & published OAPP source code along with the OASIS, STAR ODK & .NET Runtimes. NOTE: The file will be a minimum of 500 MB."))
                {
                    generateOAPPSelfContained = true;

                    if (CLIEngine.GetConfirmation("Do you wish to upload/publish the self-contained (full) .oapp file to STARNET?"))
                    {
                        if (CLIEngine.GetConfirmation("Do you wish to upload/publish the .oapp file to cloud storage?"))
                            uploadOAPPToCloud = true;

                        object OAPPBinaryProviderTypeObject = CLIEngine.GetValidInputForEnum("Do you wish to upload/publish the .oapp file to The OASIS? If so what provider do you wish to upload to? If you do not wish to then enter 'None'.", typeof(ProviderType));

                        if (OAPPBinaryProviderTypeObject != null)
                        {
                            if (OAPPBinaryProviderTypeObject.ToString() == "exit")
                                return;
                            else
                                OAPPBinaryProviderType = (ProviderType)OAPPBinaryProviderTypeObject;
                        }
                    }
                }

                if (!uploadOAPPToCloud && OAPPBinaryProviderType == ProviderType.None)
                    CLIEngine.ShowMessage("Since you did not select to upload to the cloud or OASIS storage the oapp will not be published to STARNET.");

                Console.WriteLine("");
                if (CLIEngine.GetConfirmation("Do you wish to generate a .oappsource file? This file will contain only the source files with no dependencies such as the OASIS or STAR runtimes (around 203MB). These will automatically be restored via nuget when the OAPP is built and/or published. You can optionally choose to upload this .oappsource file to STARNET from which others can download and then build to install and run your OAPP. NOTE: The full .oapp file is pre-built and published and is around 250MB minimum. You can optionally choose to also upload this file to STARNET (but you MUST upload either the .oappsource and make public or the full .oapp file if you want people to to be able to download and install your OAPP.) The advantage of the full .oapp file is that the OAPP is pre-built with all dependencies and so is guaranteed to install and run without any issues. It can also verify the launch target exists in the pre-built OAPP. If an OAPP is installed from the smaller .oappsource file (if you choose to upload and make public) there may be problems with restoring all dependencies etc so there are pros and cons to both approaches with the oapp taking longer to publish/upload and download over the .oappsource (as well as taking up more storage space) but has the advantage of being fully self contained and guaranteed to install & run fine."))
                {
                    generateOAPPSource = true;

                    if (CLIEngine.GetConfirmation("Do you wish to upload the .oappsource file to STARNET? The next question will ask if you wish to make this public. You may choose to upload and keep private as an extra backup for your code for example."))
                    {
                        uploadOAPPSource = true;

                        if (CLIEngine.GetConfirmation("Do you wish to make the .oappsource public? People will be able to view your code so only do this if you are happy with this. NOTE: If you select 'N' to this question then people will not be able to download, build, publish and install your OAPP from your .oappsource file. You will need to upload the full pre-built & published .oapp file below if you want people to be able to download and install your OAPP from STARNET. If you wish people to be able to download and install from your .oappsource file then select 'Y' to this question and the next."))
                            makeOAPPSourcePublic = true;
                    }
                }

                Console.WriteLine("");
                bool registerOnSTARNET = CLIEngine.GetConfirmation("Do you wish to publish to STARNET? If you select 'Y' to this question then your OAPP will be published to STARNET where others will be able to find, download and install. If you select 'N' then only the .oapp install file will be generated on your local device, which you can distribute as you please. This file will also be generated even if you publish to STARNET.");

                if (registerOnSTARNET)
                {
                    Console.WriteLine("");
                    bool uploadOAPP = true;

                    if (makeOAPPSourcePublic)
                    {
                        if (!(CLIEngine.GetConfirmation("You have selected to generate, upload and make public your .oappsource file from which people can download, build, publish & install your OAPP (as long as the dependencies are restored fine and the launch target is found). You can also choose to upload the full .oapp file to STARNET giving people the option of which download and install process they prefer as well as an extra layer of redundancy. Do you wish to upload the .oapp file now?")))
                            uploadOAPP = false;
                    }

                    if (uploadOAPP)
                    {
                        CLIEngine.ShowMessage("Do you wish to publish/upload the.oapp file to an OASIS Provider or to the cloud or both? Depending on which OASIS Provider is chosen such as IPFSOASIS there may issues such as speed, relialbility etc for such a large file. If you choose to upload to the cloud this could be faster and more reliable (but there is a limit of 5 OAPPs on the free plan and you will need to upgrade to upload more than 5 OAPPs). You may want to choose to use both to add an extra layer of redundancy (recommended).");

                        //if (CLIEngine.GetConfirmation("Do you wish to upload to the cloud?"))
                        //    uploadToCloud = true;

                        //if (CLIEngine.GetConfirmation("Do you wish to upload to an OASIS Provider? Make sure you select a provider that can handle large files such as IPFSOASIS, HoloOASIS etc. Also remember the OASIS Hyperdrive will only be able to auto-replicate to other providers that also support large files and are free or cost effective. By default it will NOT auto-replicate large files, you will need to manually configure this in your OASIS Profile settings."))
                        //{
                        //    object largeProviderTypeObject = CLIEngine.GetValidInputForEnum("What provider do you wish to publish the OAPP to? (The default is IPFSOASIS)", typeof(ProviderType));

                        //    if (largeProviderTypeObject != null)
                        //        largeFileProviderType = (ProviderType)largeProviderTypeObject;
                        //}
                    }
                }
                else
                    Console.WriteLine("");

                if (Path.IsPathRooted(STAR.STARDNA.DefaultPublishedOAPPsPath))
                    publishPath = STAR.STARDNA.DefaultPublishedOAPPsPath;
                else
                    publishPath = Path.Combine(STAR.STARDNA.BasePath, STAR.STARDNA.DefaultPublishedOAPPsPath);

                //Console.WriteLine("");
                if (!CLIEngine.GetConfirmation($"Do you wish to publish the OAPP to the default publish folder defined in the STARDNA as DefaultPublishedOAPPsPath : {publishPath}?"))
                {
                    if (CLIEngine.GetConfirmation($"Do you wish to publish the OAPP to: {Path.Combine(oappPath, "Published")}?"))
                        publishPath = Path.Combine(oappPath, "Published");
                    else
                        publishPath = CLIEngine.GetValidFolder("Where do you wish to publish the OAPP?", true);
                }

                Console.WriteLine("");
                CLIEngine.ShowWorkingMessage("Publishing OAPP...");

                STAR.OASISAPI.OAPPs.OnOAPPPublishStatusChanged += OAPPs_OnOAPPPublishStatusChanged;
                STAR.OASISAPI.OAPPs.OnOAPPUploadStatusChanged += OAPPs_OnOAPPUploadStatusChanged;
                OASISResult<IOAPPDNA> publishResult = await STAR.OASISAPI.OAPPs.PublishOAPPAsync(oappPath, launchTarget, STAR.BeamedInAvatar.Id, publishDotNot, publishPath, registerOnSTARNET, generateOAPPSource, uploadOAPPSource, makeOAPPSourcePublic, generateOAPP, generateOAPPSelfContained, generateOAPPSelfContainedFull, uploadOAPPToCloud, uploadOAPPSelfContainedToCloud, uploadOAPPSelfContainedFullToCloud, providerType, OAPPBinaryProviderType, OAPPSelfContainedBinaryProviderType, OAPPSelfContainedFullBinaryProviderType);
                STAR.OASISAPI.OAPPs.OnOAPPUploadStatusChanged -= OAPPs_OnOAPPUploadStatusChanged;
                STAR.OASISAPI.OAPPs.OnOAPPPublishStatusChanged -= OAPPs_OnOAPPPublishStatusChanged;

                if (publishResult != null && !publishResult.IsError && publishResult.Result != null)
                {
                    CLIEngine.ShowSuccessMessage("OAPP Successfully Published.");
                    ShowOAPP(publishResult.Result);

                    if (CLIEngine.GetConfirmation("Do you wish to install the OAPP now?"))
                        await InstallOAPPAsync(publishResult.Result.OAPPId.ToString());

                    Console.WriteLine("");
                }
                else
                    CLIEngine.ShowErrorMessage($"An error occured publishing the OAPP. Reason: {publishResult.Message}");
            }
            else
                CLIEngine.ShowErrorMessage("The OAPPDNA.json file could not be found! Please ensure it is in the folder you specified.");
        }

        public static async Task UnPublishOAPPTemplateAsync(string idOrName = "", ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IOAPP> result = await LoadOAPPAsync(idOrName, "unpublish", providerType);

            if (result != null && !result.IsError && result.Result != null)
            {
                OASISResult<IOAPPDNA> unpublishResult = await STAR.OASISAPI.OAPPs.UnPublishOAPPAsync(result.Result, providerType);

                if (unpublishResult != null && !unpublishResult.IsError && unpublishResult.Result != null)
                {
                    CLIEngine.ShowSuccessMessage("OAPP Successfully Unpublished.");
                    ShowOAPP(unpublishResult.Result);
                }
                else
                    CLIEngine.ShowErrorMessage($"An error occured unpublishing the OAPP. Reason: {unpublishResult.Message}");
            }
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

        private static void OAPPTemplates_OnOAPPTemplateInstallStatusChanged(object sender, API.ONODE.Core.Events.OAPPTemplateInstallStatusEventArgs e)
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

        private static void OAPPTemplates_OnOAPPTemplateDownloadStatusChanged(object sender, API.ONODE.Core.Events.OAPPTemplateDownloadProgressEventArgs e)
        {
            CLIEngine.UpdateWorkingMessage($"Downloading... {e.Progress}%");
        }
    }
}