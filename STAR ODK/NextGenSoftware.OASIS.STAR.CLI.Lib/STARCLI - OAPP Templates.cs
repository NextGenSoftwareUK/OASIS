using System.Diagnostics;
using NextGenSoftware.CLI.Engine;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.ONode.Core.Holons;
using NextGenSoftware.OASIS.API.ONode.Core.Interfaces;
using NextGenSoftware.OASIS.API.ONode.Core.Interfaces.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;

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

        public static async Task InstallOAPPTemplateAsync(string idOrName = "", ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IInstalledOAPPTemplate> installResult = null;
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
                        STAR.OASISAPI.OAPPs.OnOAPPDownloadStatusChanged += OAPPs_OnOAPPDownloadStatusChanged;
                        STAR.OASISAPI.OAPPs.OnOAPPInstallStatusChanged += OAPPs_OnOAPPInstallStatusChanged;
                        CLIEngine.ShowWorkingMessage("Installing OAPP Template...");
                        installResult = await STAR.OASISAPI.OAPPTemplates.InstallOAPPTemplateAsync(STAR.BeamedInAvatar.Id, result.Result, installPath, true, providerType);
                        STAR.OASISAPI.OAPPs.OnOAPPDownloadStatusChanged -= OAPPs_OnOAPPDownloadStatusChanged;
                        STAR.OASISAPI.OAPPs.OnOAPPInstallStatusChanged -= OAPPs_OnOAPPInstallStatusChanged;
                    }
                }
            }
            else
            {
                Console.WriteLine("");
                if (CLIEngine.GetConfirmation("Do you wish to install the OAPP Template from a local .oapptemplate file or from STARNET? Press 'Y' for local .oapptemplate or 'N' for STARNET."))
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
        }

        public static void InstallOAPPTemplate(string idOrName = "", ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IInstalledOAPP> installResult = null;
            string installPath = "";

            if (Path.IsPathRooted(STAR.STARDNA.DefaultInstalledOAPPsPath))
                installPath = STAR.STARDNA.DefaultInstalledOAPPsPath;
            else
                installPath = Path.Combine(STAR.STARDNA.BasePath, STAR.STARDNA.DefaultInstalledOAPPsPath);

            if (!CLIEngine.GetConfirmation($"Do you wish to install the OAPP Template to the default install folder defined in the STARDNA as DefaultInstalledOAPPsPath : {installPath}?"))
                installPath = CLIEngine.GetValidFolder("What is the full path to where you wish to install the OAPP?", true);

            if (!string.IsNullOrEmpty(idOrName))
            {
                OASISResult<IOAPP> result = LoadOAPP(idOrName, "install", providerType);

                if (result != null && result.Result != null && !result.IsError)
                    installResult = STAR.OASISAPI.OAPPs.InstallOAPP(STAR.BeamedInAvatar.Id, result.Result, installPath, true, providerType);

                //installResult = await STAR.OASISAPI.OAPPs.InstallOAPPAsync(STAR.BeamedInAvatar.Id, id, installPath, providerType);
            }
            else
            {
                Console.WriteLine("");
                if (CLIEngine.GetConfirmation("Do you wish to install the OAPP from a local .oapp file or from STARNET? Press 'Y' for local .oapp or 'N' for STARNET."))
                {
                    Console.WriteLine("");
                    string oappPath = CLIEngine.GetValidFile("What is the full path to the .oapp file?");

                    CLIEngine.ShowWorkingMessage("Installing OAPP...");
                    installResult = STAR.OASISAPI.OAPPs.InstallOAPP(STAR.BeamedInAvatar.Id, oappPath, installPath, true, providerType);
                }
                else
                    LaunchSTARNETAsync(true);
            }

            if (installResult != null)
            {
                if (!installResult.IsError && installResult.Result != null)
                {
                    CLIEngine.ShowSuccessMessage("OAPP Successfully Installed.");
                    ShowInstalledOAPP(installResult.Result);

                    if (CLIEngine.GetConfirmation("Do you wish to launch the OAPP now?"))
                    {
                        string oappTarget = Path.Combine(installPath, installResult.Result.OAPPDNA.LaunchTarget);
                        //Process.Start("explorer.exe", Path.Combine(installPath, installResult.Result.OAPPDNA.LaunchTarget));
                        Process.Start("dotnet.exe", Path.Combine(installPath, installResult.Result.OAPPDNA.LaunchTarget));
                    }
                }
                else
                    CLIEngine.ShowErrorMessage($"Error installing OAPP. Reason: {installResult.Message}");
            }
            else
                CLIEngine.ShowErrorMessage($"Error installing OAPP. Reason: Unknown error occured!");
        }

        public static async Task UnInstallOAPPTemplateAsync(string idOrName = "", ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IOAPP> result = await LoadOAPPAsync(idOrName, "uninstall", providerType);

            if (result != null && !result.IsError && result.Result != null)
            {
                OASISResult<IOAPPDNA> uninstallResult = await STAR.OASISAPI.OAPPs.UnInstallOAPPAsync(result.Result.OAPPDNA, STAR.BeamedInAvatar.Id, providerType);

                if (uninstallResult != null)
                {
                    if (!uninstallResult.IsError && uninstallResult.Result != null)
                    {
                        CLIEngine.ShowSuccessMessage("OAPP Successfully Uninstalled.");
                        ShowOAPP(uninstallResult.Result);
                    }
                    else
                        CLIEngine.ShowErrorMessage($"Error installing OAPP. Reason: {uninstallResult.Message}");
                }
                else
                    CLIEngine.ShowErrorMessage($"Error uninstalling OAPP. Reason: Unknown error occured!");
            }
            else
                CLIEngine.ShowErrorMessage($"An error occured loading the OAPP. Reason: {result.Message}");
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

        //public static async Task SearchOAPPTemplatesAsync(ProviderType providerType = ProviderType.Default)
        //{
        //    //await STAR.OASISAPI.OAPPs.
        //}

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

                        foreach (IOAPP oapp in oappTemplates.Result)
                            ShowOAPP(oapp);

                        ShowOAPPListFooter();
                    }
                    else
                        CLIEngine.ShowWarningMessage("No OAPP Templates Found.");
                }
                else
                    CLIEngine.ShowErrorMessage($"Error occured loading OAPP Temmplates. Reason: {oappTemplates.Message}");
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
    }
}