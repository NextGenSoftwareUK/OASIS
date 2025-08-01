//using System.Diagnostics;
//using NextGenSoftware.CLI.Engine;
//using NextGenSoftware.OASIS.Common;
//using NextGenSoftware.OASIS.API.Core;
//using NextGenSoftware.OASIS.API.Core.Enums;
//using NextGenSoftware.OASIS.API.ONODE.Core.Enums;
//using NextGenSoftware.OASIS.API.ONODE.Core.Events;
//using NextGenSoftware.OASIS.API.ONODE.Core.Holons;
//using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Holons;
//using NextGenSoftware.OASIS.STAR.OASISAPIManager;
//using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Holons;
//using NextGenSoftware.OASIS.STAR.CLI.Lib.Enums;
//using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces;

//namespace NextGenSoftware.OASIS.STAR.CLI.Lib
//{
//    public static partial class STARCLI
//    {
//        public static async Task CreateRuntimeAsync(object createParams, ProviderType providerType = ProviderType.Default)
//        {
//            RuntimeType runtimeType = RuntimeType.STAR;
//            bool runtimeValid = false;
//            string runtimePath = "";
//            string runtimeName = "";

//            CLIEngine.ShowDivider();
//            CLIEngine.ShowMessage("Welcome to the Runtime Wizard");
//            CLIEngine.ShowDivider();
//            Console.WriteLine();
//            CLIEngine.ShowMessage("This wizard will allow you create an Runtime which can be used to create a OAPP from (along with a OAPP Template).", false);
//            CLIEngine.ShowMessage("The runtime can be created from anything you like from any stack, platform, os etc.");
//            CLIEngine.ShowMessage("The STAR & OASIS runtimes can only be created by an admin/wizard.");
//            CLIEngine.ShowMessage("The wizard will create an empty folder with a RuntimeDNA.json file in it. You then simply place any files/folders you need into this folder.");
//            CLIEngine.ShowMessage("Finally you run the sub-command 'runtime publish' to convert the folder containing the runtime (can contain any number of files and sub-folders) into a OASIS Runtime file (.oruntime).");
//            CLIEngine.ShowMessage("You can then share the .oruntime file with others from which you can create OAPP's from (along with a OAPP Template, you can even use the same OAPP Template for different runtimes).");
//            CLIEngine.ShowMessage("You can also optionally choose to upload the .oruntime file to the STARNET store so others can search, download and install the runtime. They can then create OAPP's from the runtime.");
//            CLIEngine.ShowDivider();

//            do
//            {
//                object value = CLIEngine.GetValidInputForEnum("What type of Runtime do you wish to create?", typeof(RuntimeType));

//                if (value != null)
//                {
//                    if (value.ToString() == "exit")
//                        return;

//                    if ((value.ToString().ToUpper() == "OASIS" || value.ToString().ToUpper() == "STAR") && STAR.BeamedInAvatar.AvatarType.Name != "Wizard")
//                        CLIEngine.ShowWarningMessage("Only wizards (admins) can create OASIS or STAR runtimes!");
//                    else
//                    {
//                        runtimeType = (RuntimeType)value;
//                        runtimeValid = true;
//                    }
//                }
//            } while (!runtimeValid);

//            string version = CLIEngine.GetValidInput("What is the version of the Runtime?");

//            if (version == "exit")
//                return;

//            if (runtimeType == RuntimeType.Other)
//            {
//                runtimeName = CLIEngine.GetValidInput("What is the name of the Runtime?");

//                if (runtimeName == "exit")
//                    return;
//            }
//            else
//            {
//                runtimeName = string.Concat(Enum.GetName(typeof(RuntimeType), runtimeType), " ", version);

//                if (!CLIEngine.GetConfirmation($"Do you wish to use the following default name: {runtimeName}?"))
//                    runtimeName = CLIEngine.GetValidInput("What is the name of the Runtime?");
//            }

//            string runtimeDesc = CLIEngine.GetValidInput("What is the description of the Runtime?");

//            if (runtimeDesc == "exit")
//                return; 

//            if (!string.IsNullOrEmpty(STAR.STARDNA.BasePath))
//                runtimePath = Path.Combine(STAR.STARDNA.BasePath, STAR.STARDNA.DefaultRuntimesSourcePath);
//            else
//                runtimePath = STAR.STARDNA.DefaultRuntimesSourcePath;

//            if (!CLIEngine.GetConfirmation($"Do you wish to create the Runtime in the default path defined in the STARDNA as 'DefaultRuntimesSourcePath'? The current path points to: {runtimePath}"))
//                runtimePath = CLIEngine.GetValidFolder("Where do you wish to create the Runtime?");

//            runtimePath = Path.Combine(runtimePath, runtimeName);

//            Console.WriteLine("");
//            CLIEngine.ShowWorkingMessage("Generating Runtime...");
//            OASISResult<IRuntimeDNA> runtimeResult = await STAR.OASISAPI.Runtimes.CreateRuntimeAsync(runtimeName, runtimeDesc, runtimeType, version, STAR.BeamedInAvatar.Id, runtimePath, providerType);

//            if (runtimeResult != null)
//            {
//                if (!runtimeResult.IsError && runtimeResult.Result != null)
//                {
//                    CLIEngine.ShowSuccessMessage($"Runtime Successfully Generated. ({runtimeResult.Message})");
//                    ShowRuntime(runtimeResult.Result);
//                    Console.WriteLine("");

//                    if (CLIEngine.GetConfirmation("Do you wish to open the Runtime folder now?"))
//                        Process.Start("explorer.exe", runtimePath);

//                    Console.WriteLine("");
//                }
//                //else
//                //    CLIEngine.ShowErrorMessage($"Error Occured: {RuntimeResult.Message}"); //Redundant error message.
//            }
//            else
//                CLIEngine.ShowErrorMessage($"Unknown Error Occured.");
//        }

//        public static async Task EditRuntimeAsync(string idOrName = "", object editParams = null, ProviderType providerType = ProviderType.Default)
//        {
//            OASISResult<IRuntime> loadResult = await LoadRuntimeAsync(idOrName, "edit", providerType);

//            if (loadResult != null && loadResult.Result != null && !loadResult.IsError)
//            {
//                ShowRuntime(loadResult.Result.RuntimeDNA);

//                //TODO: Comeback to this.
//                loadResult.Result.Name = CLIEngine.GetValidInput("What is the name of the Runtime?");
//                loadResult.Result.Description = CLIEngine.GetValidInput("What is the description of the Runtime?");

//                OASISResult<IRuntime> result = await STAR.OASISAPI.Runtimes.SaveRuntimeAsync(loadResult.Result, STAR.BeamedInAvatar.Id, providerType);
//                CLIEngine.ShowWorkingMessage("Saving Runtime...");

//                if (result != null && !result.IsError && result.Result != null)
//                {
//                    CLIEngine.ShowSuccessMessage("Runtime Successfully Updated.");
//                    ShowRuntime(result.Result.RuntimeDNA);
//                }
//                else
//                    CLIEngine.ShowErrorMessage($"An error occured updating the Runtime. Reason: {result.Message}");
//            }
//            else
//                CLIEngine.ShowErrorMessage($"An error occured loading the Runtime. Reason: {loadResult.Message}");
//        }

//        public static async Task DeleteRuntimeAsync(string idOrName = "", bool softDelete = true, ProviderType providerType = ProviderType.Default)
//        {
//            OASISResult<IRuntime> result = await LoadRuntimeAsync(idOrName, "delete", providerType);

//            if (result != null && !result.IsError && result.Result != null)
//            {
//                ShowRuntime(result.Result.RuntimeDNA);

//                if (CLIEngine.GetConfirmation("Are you sure you wish to delete the Runtime?"))
//                {
//                    CLIEngine.ShowWorkingMessage("Deleting Runtime...");
//                    result = await STAR.OASISAPI.Runtimes.DeleteRuntimeAsync(result.Result, STAR.BeamedInAvatar.Id, true, providerType);

//                    if (result != null && !result.IsError && result.Result != null)
//                    {
//                        CLIEngine.ShowSuccessMessage("Runtime Successfully Deleted.");
//                        ShowRuntime(result.Result.RuntimeDNA);
//                    }
//                    else
//                        CLIEngine.ShowErrorMessage($"An error occured deleting the Runtime. Reason: {result.Message}");
//                }
//            }
//            else
//                CLIEngine.ShowErrorMessage($"An error occured loading the Runtime. Reason: {result.Message}");
//        }

//        public static async Task PublishRuntimeAsync(string RuntimePath = "", ProviderType providerType = ProviderType.Default)
//        {
//            bool generateRuntime = true;
//            bool uploadRuntimeToCloud = false;
//            ProviderType runtimeBinaryProviderType = ProviderType.IPFSOASIS;
//            string publishPath = "";

//            if (string.IsNullOrEmpty(RuntimePath))
//            {
//                string OAPPPathQuestion = "What is the full path to the Runtime directory?";
//                RuntimePath = CLIEngine.GetValidFolder(OAPPPathQuestion, false);
//            }

//            OASISResult<IRuntimeDNA> RuntimeDNAResult = await STAR.OASISAPI.Runtimes.ReadRuntimeDNAAsync(RuntimePath);

//            if (RuntimeDNAResult != null && RuntimeDNAResult.Result != null && !RuntimeDNAResult.IsError)
//            {
//                Console.WriteLine("");
//                bool registerOnSTARNET = CLIEngine.GetConfirmation("Do you wish to publish to STARNET? If you select 'Y' to this question then your Runtime will be published to STARNET where others will be able to find, download and install. If you select 'N' then only the .Runtime install file will be generated on your local device, which you can distribute as you please. This file will also be generated even if you publish to STARNET.");
//                Console.WriteLine("");

//                if (registerOnSTARNET)
//                {
//                    CLIEngine.ShowMessage("Do you wish to publish/upload the .Runtime file to an OASIS Provider or to the cloud or both? Depending on which OASIS Provider is chosen such as IPFSOASIS there may issues such as speed, relialbility etc for such a large file. If you choose to upload to the cloud this could be faster and more reliable (but there is a limit of 5 OAPPs on the free plan and you will need to upgrade to upload more than 5 OAPPs). You may want to choose to use both to add an extra layer of redundancy (recommended).");

//                    if (CLIEngine.GetConfirmation("Do you wish to upload to the cloud?"))
//                        uploadRuntimeToCloud = true;

//                    if (CLIEngine.GetConfirmation("Do you wish to upload to an OASIS Provider? Make sure you select a provider that can handle large files such as IPFSOASIS, HoloOASIS etc. Also remember the OASIS Hyperdrive will only be able to auto-replicate to other providers that also support large files and are free or cost effective. By default it will NOT auto-replicate large files, you will need to manually configure this in your OASIS Profile settings."))
//                    {
//                        object largeProviderTypeObject = CLIEngine.GetValidInputForEnum("What provider do you wish to publish the Runtime to? (The default is IPFSOASIS)", typeof(ProviderType));

//                        if (largeProviderTypeObject != null)
//                            runtimeBinaryProviderType = (ProviderType)largeProviderTypeObject;
//                    }
//                }

//                if (Path.IsPathRooted(STAR.STARDNA.DefaultRuntimesPublishedPath))
//                    publishPath = STAR.STARDNA.DefaultRuntimesPublishedPath;
//                else
//                    publishPath = Path.Combine(STAR.STARDNA.BasePath, STAR.STARDNA.DefaultRuntimesPublishedPath);

//                if (!CLIEngine.GetConfirmation($"Do you wish to publish the Runtime to the default publish folder defined in the STARDNA as DefaultRuntimesPublishedPath : {publishPath}?"))
//                {
//                    if (CLIEngine.GetConfirmation($"Do you wish to publish the Runtime to: {Path.Combine(RuntimePath, "Published")}?"))
//                        publishPath = Path.Combine(RuntimePath, "Published");
//                    else
//                        publishPath = CLIEngine.GetValidFolder("Where do you wish to publish the Runtime?", true);
//                }

//                Console.WriteLine("");
//                CLIEngine.ShowWorkingMessage("Publishing Runtime...");

//                STAR.OASISAPI.Runtimes.OnRuntimePublishStatusChanged += Runtimes_OnRuntimePublishStatusChanged;
//                STAR.OASISAPI.Runtimes.OnRuntimeUploadStatusChanged += Runtimes_OnRuntimeUploadStatusChanged;
//                OASISResult<IRuntimeDNA> publishResult = await STAR.OASISAPI.Runtimes.PublishRuntimeAsync(RuntimePath, STAR.BeamedInAvatar.Id, publishPath, registerOnSTARNET, generateRuntime, uploadRuntimeToCloud, providerType, runtimeBinaryProviderType);
//                STAR.OASISAPI.Runtimes.OnRuntimeUploadStatusChanged -= Runtimes_OnRuntimeUploadStatusChanged;
//                STAR.OASISAPI.Runtimes.OnRuntimePublishStatusChanged -= Runtimes_OnRuntimePublishStatusChanged;

//                if (publishResult != null && !publishResult.IsError && publishResult.Result != null)
//                {
//                    CLIEngine.ShowSuccessMessage("Runtime Successfully Published.");
//                    ShowRuntime(publishResult.Result);

//                    if (CLIEngine.GetConfirmation("Do you wish to install the Runtime now?"))
//                        await InstallRuntimeAsync(publishResult.Result.Id.ToString());

//                    Console.WriteLine("");
//                }
//                else
//                    CLIEngine.ShowErrorMessage($"An error occured publishing the Runtime. Reason: {publishResult.Message}");
//            }
//            else
//                CLIEngine.ShowErrorMessage("The RuntimeDNA.json file could not be found! Please ensure it is in the folder you specified.");
//        }

//        public static async Task UnPublishRuntimeAsync(string idOrName = "", ProviderType providerType = ProviderType.Default)
//        {
//            OASISResult<IRuntime> result = await LoadRuntimeAsync(idOrName, "unpublish", providerType);

//            if (result != null && !result.IsError && result.Result != null)
//            {
//                OASISResult<IRuntimeDNA> unpublishResult = await STAR.OASISAPI.Runtimes.UnPublishRuntimeAsync(result.Result, STAR.BeamedInAvatar.Id, providerType);

//                if (unpublishResult != null && !unpublishResult.IsError && unpublishResult.Result != null)
//                {
//                    CLIEngine.ShowSuccessMessage("Runtime Successfully Unpublished.");
//                    ShowRuntime(unpublishResult.Result);
//                }
//                else
//                    CLIEngine.ShowErrorMessage($"An error occured unpublishing the Runtime. Reason: {unpublishResult.Message}");
//            }
//        }

//        public static async Task<OASISResult<IInstalledRuntime>> InstallRuntimeAsync(string idOrName = "", InstallMode installMode = InstallMode.DownloadAndInstall, ProviderType providerType = ProviderType.Default)
//        {
//            OASISResult<IInstalledRuntime> installResult = new OASISResult<IInstalledRuntime>();
//            string installPath = "";

//            if (Path.IsPathRooted(STAR.STARDNA.DefaultRuntimesInstalledPath))
//                installPath = STAR.STARDNA.DefaultRuntimesInstalledPath;
//            else
//                installPath = Path.Combine(STAR.STARDNA.BasePath, STAR.STARDNA.DefaultRuntimesInstalledPath);

//            Console.WriteLine("");

//            if (!CLIEngine.GetConfirmation($"Do you wish to install the Runtime to the default install folder defined in the STARDNA as DefaultRuntimesInstalledPath : {installPath}?"))
//                installPath = CLIEngine.GetValidFolder("What is the full path to where you wish to install the Runtime?", true);

//            if (!string.IsNullOrEmpty(idOrName))
//            {
//                Console.WriteLine("");
//                ProviderType largeFileProviderType = ProviderType.IPFSOASIS;
//                object largeProviderTypeObject = CLIEngine.GetValidInputForEnum("What provider do you wish to install the Runtime from? (The default is IPFSOASIS)", typeof(ProviderType));

//                if (largeProviderTypeObject != null)
//                {
//                    largeFileProviderType = (ProviderType)largeProviderTypeObject;
//                    OASISResult<IRuntime> result = await LoadRuntimeAsync(idOrName, "install", largeFileProviderType, false);

//                    if (result != null && result.Result != null && !result.IsError)
//                    {
//                        STAR.OASISAPI.Runtimes.OnRuntimeDownloadStatusChanged += Runtimes_OnRuntimeDownloadStatusChanged;
//                        STAR.OASISAPI.Runtimes.OnRuntimeInstallStatusChanged += Runtimes_OnRuntimeInstallStatusChanged;
//                        CLIEngine.ShowWorkingMessage("Installing Runtime...");
//                        STAR.OASISAPI.Runtimes.OnRuntimeDownloadStatusChanged -= Runtimes_OnRuntimeDownloadStatusChanged;
//                        STAR.OASISAPI.Runtimes.OnRuntimeInstallStatusChanged -= Runtimes_OnRuntimeInstallStatusChanged;
//                    }
//                }
//            }
//            else
//            {
//                Console.WriteLine("");
//                if (CLIEngine.GetConfirmation("Do you wish to install the Runtime from a local .oruntime file or from STARNET? Press 'Y' for local .oruntime file or 'N' for STARNET."))
//                {
//                    Console.WriteLine("");
//                    string runtimePath = CLIEngine.GetValidFile("What is the full path to the .oruntime file?");

//                    CLIEngine.ShowWorkingMessage("Installing Runtime...");
//                    installResult = await STAR.OASISAPI.Runtimes.InstallRuntimeAsync(STAR.BeamedInAvatar.Id, runtimePath, installPath, providerType);
//                }
//                else
//                {
//                    //await LaunchSTARNETAsync(true);

//                    OASISResult<IEnumerable<IRuntime>> oappRuntimesResult = await ListAllRuntimesAsync();

//                    if (oappRuntimesResult != null && oappRuntimesResult.Result != null && !oappRuntimesResult.IsError && oappRuntimesResult.Result.Count() > 0)
//                    {
//                        //Guid OAPPID = CLIEngine.GetValidInputForGuid("What is the GUID/ID of the OAPP you wish to install?");

//                        ProviderType largeFileProviderType = ProviderType.IPFSOASIS;
//                        object largeProviderTypeObject = CLIEngine.GetValidInputForEnum("What provider do you wish to install the Runtime from? (The default is IPFSOASIS)", typeof(ProviderType));

//                        if (largeProviderTypeObject != null)
//                        {
//                            largeFileProviderType = (ProviderType)largeProviderTypeObject;
//                            OASISResult<IRuntime> result = await LoadRuntimeAsync("", "install", largeFileProviderType);

//                            if (result != null && result.Result != null && !result.IsError)
//                                await InstallRuntimeAsync(result.Result.Id.ToString());
//                        }
//                    }
//                    else
//                    {
//                        installResult.Message = "No Runtime found to install.";
//                        installResult.IsError = true;
//                    }
//                }
//            }

//            if (installResult != null)
//            {
//                if (!installResult.IsError && installResult.Result != null)
//                {
//                    CLIEngine.ShowSuccessMessage("Runtime Successfully Installed.");
//                    ShowInstalledRuntime(installResult.Result);

//                    if (CLIEngine.GetConfirmation("Do you wish to open the folder to the Runtime now?"))
//                        STAR.OASISAPI.Runtimes.OpenRuntimeFolder(STAR.BeamedInAvatar.Id, installResult.Result);
//                }
//                else
//                    CLIEngine.ShowErrorMessage($"Error installing Runtime. Reason: {installResult.Message}");
//            }
//            else
//                CLIEngine.ShowErrorMessage($"Error installing Runtime. Reason: Unknown error occured!");

//            return installResult;
//        }

//        public static OASISResult<IInstalledRuntime> InstallRuntime(string idOrName = "", ProviderType providerType = ProviderType.Default)
//        {
//            OASISResult<IInstalledRuntime> installResult = new OASISResult<IInstalledRuntime>();
//            string installPath = "";

//            if (Path.IsPathRooted(STAR.STARDNA.DefaultRuntimesInstalledPath))
//                installPath = STAR.STARDNA.DefaultRuntimesInstalledPath;
//            else
//                installPath = Path.Combine(STAR.STARDNA.BasePath, STAR.STARDNA.DefaultRuntimesInstalledPath);

//            if (!CLIEngine.GetConfirmation($"Do you wish to install the Runtime to the default install folder defined in the STARDNA as DefaultRuntimesInstalledPath : {installPath}?"))
//                installPath = CLIEngine.GetValidFolder("What is the full path to where you wish to install the Runtime?", true);

//            if (!string.IsNullOrEmpty(idOrName))
//            {
//                OASISResult<IRuntime> result = LoadRuntime(idOrName, "install", providerType);

//                if (result != null && result.Result != null && !result.IsError)
//                    installResult = STAR.OASISAPI.Runtimes.InstallRuntime(STAR.BeamedInAvatar.Id, result.Result, installPath, providerType);

//                //installResult = await STAR.OASISAPI.OAPPs.InstallOAPPAsync(STAR.BeamedInAvatar.Id, id, installPath, providerType);
//            }
//            else
//            {
//                Console.WriteLine("");
//                if (CLIEngine.GetConfirmation("Do you wish to install the Runtime from a local .oruntime file or from STARNET? Press 'Y' for local .oruntime file or 'N' for STARNET."))
//                {
//                    Console.WriteLine("");
//                    string oappPath = CLIEngine.GetValidFile("What is the full path to the .oruntime file?");

//                    CLIEngine.ShowWorkingMessage("Installing Runtime...");
//                    installResult = STAR.OASISAPI.Runtimes.InstallRuntime(STAR.BeamedInAvatar.Id, oappPath, installPath, providerType);
//                }
//                else
//                {
//                    //await LaunchSTARNETAsync(true);

//                    OASISResult<IEnumerable<IRuntime>> oappRuntimesResult = ListAllRuntimes();

//                    if (oappRuntimesResult != null && oappRuntimesResult.Result != null && !oappRuntimesResult.IsError && oappRuntimesResult.Result.Count() > 0)
//                    {
//                        //Guid OAPPID = CLIEngine.GetValidInputForGuid("What is the GUID/ID of the OAPP you wish to install?");

//                        ProviderType largeFileProviderType = ProviderType.IPFSOASIS;
//                        object largeProviderTypeObject = CLIEngine.GetValidInputForEnum("What provider do you wish to install the Runtime from? (The default is IPFSOASIS)", typeof(ProviderType));

//                        if (largeProviderTypeObject != null)
//                        {
//                            largeFileProviderType = (ProviderType)largeProviderTypeObject;
//                            OASISResult<IRuntime> result = LoadRuntime("", "install", largeFileProviderType);

//                            if (result != null && result.Result != null && !result.IsError)
//                                InstallRuntime(result.Result.Id.ToString());
//                        }
//                    }
//                    else
//                    {
//                        installResult.Message = "No Runtime found to install.";
//                        installResult.IsError = true;
//                    }
//                }
//            }

//            if (installResult != null)
//            {
//                if (!installResult.IsError && installResult.Result != null)
//                {
//                    CLIEngine.ShowSuccessMessage("Runtime Successfully Installed.");
//                    ShowInstalledRuntime(installResult.Result);

//                    if (CLIEngine.GetConfirmation("Do you wish to open the folder to the Runtime now?"))
//                        STAR.OASISAPI.Runtimes.OpenRuntimeFolder(STAR.BeamedInAvatar.Id, installResult.Result);
//                }
//                else
//                    CLIEngine.ShowErrorMessage($"Error installing Runtime. Reason: {installResult.Message}");
//            }
//            else
//                CLIEngine.ShowErrorMessage($"Error installing Runtime. Reason: Unknown error occured!");

//            return installResult;
//        }

//        public static async Task UnInstallRuntimeAsync(string idOrName = "", ProviderType providerType = ProviderType.Default)
//        {
//            OASISResult<IRuntime> result = await LoadRuntimeAsync(idOrName, "uninstall", providerType);

//            if (result != null && !result.IsError && result.Result != null)
//            {
//                OASISResult<IRuntimeDNA> uninstallResult = await STAR.OASISAPI.Runtimes.UnInstallRuntimeAsync(result.Result.RuntimeDNA, STAR.BeamedInAvatar.Id, providerType);

//                if (uninstallResult != null)
//                {
//                    if (!uninstallResult.IsError && uninstallResult.Result != null)
//                    {
//                        CLIEngine.ShowSuccessMessage("Runtime Successfully Uninstalled.");
//                        ShowRuntime(uninstallResult.Result);
//                    }
//                    else
//                        CLIEngine.ShowErrorMessage($"Error installing Runtime. Reason: {uninstallResult.Message}");
//                }
//                else
//                    CLIEngine.ShowErrorMessage($"Error uninstalling Runtime. Reason: Unknown error occured!");
//            }
//            else
//                CLIEngine.ShowErrorMessage($"An error occured loading the Runtime. Reason: {result.Message}");
//        }

//        public static async Task<OASISResult<IEnumerable<IRuntime>>> ListAllRuntimesAsync(bool showAllVersions = false, ProviderType providerType = ProviderType.Default)
//        {
//            return ListRuntimes(await STAR.OASISAPI.Runtimes.LoadAllRuntimesAsync(providerType));
//        }

//        public static OASISResult<IEnumerable<IRuntime>> ListAllRuntimes(ProviderType providerType = ProviderType.Default)
//        {
//            return ListRuntimes(STAR.OASISAPI.Runtimes.LoadAllRuntimes(providerType));
//        }

//        public static async Task ListRuntimesCreatedByBeamedInAvatarAsync(bool showAllVersions = false, ProviderType providerType = ProviderType.Default)
//        {
//            if (STAR.BeamedInAvatar != null)
//                ListRuntimes(await STAR.OASISAPI.Runtimes.LoadAllRuntimesForAvatarAsync(STAR.BeamedInAvatar.AvatarId));
//            else
//                CLIEngine.ShowErrorMessage("No Avatar Is Beamed In. Please Beam In First!");
//        }

//        public static async Task<OASISResult<IEnumerable<IInstalledRuntime>>> ListRuntimesInstalledForBeamedInAvatarAsync(ProviderType providerType = ProviderType.Default)
//        {
//            OASISResult<IEnumerable<IInstalledRuntime>> result = new OASISResult<IEnumerable<IInstalledRuntime>>();

//            if (STAR.BeamedInAvatar != null)
//            {
//                result = await STAR.OASISAPI.Runtimes.ListInstalledRuntimesAsync(STAR.BeamedInAvatar.AvatarId);
//                ListInstalledRuntimes(result);
//            }
//            else
//                OASISErrorHandling.HandleError(ref result, "No Avatar Is Beamed In. Please Beam In First!");
//            //CLIEngine.ShowErrorMessage("No Avatar Is Beamed In. Please Beam In First!");

//            return result;
//        }

//        public static async Task SearchRuntimesAsync(string searchTerm, bool showAllVersions, bool showForAllAvatars, ProviderType providerType = ProviderType.Default)
//        {
//            ListRuntimes(await STAR.OASISAPI.Runtimes.SearchRuntimesAsync(searchTerm, STAR.BeamedInAvatar.Id, !showForAllAvatars, providerType));
//        }

//        public static async Task ShowRuntimeAsync(string idOrName = "", ProviderType providerType = ProviderType.Default)
//        {
//            OASISResult<IRuntime> result = await LoadRuntimeAsync(idOrName, "view", providerType);

//            if (result != null && !result.IsError && result.Result != null)
//                ShowRuntime(result.Result.RuntimeDNA);
//            else
//                CLIEngine.ShowErrorMessage($"An error occured loading the Runtime. Reason: {result.Message}");
//        }

//        public static void ShowRuntime(IRuntimeDNA RuntimeDNA)
//        {
//            CLIEngine.ShowMessage(string.Concat($"Id:                                         ", RuntimeDNA.Id != Guid.Empty ? RuntimeDNA.Id : "None"));
//            CLIEngine.ShowMessage(string.Concat($"Name:                                       ", !string.IsNullOrEmpty(RuntimeDNA.Name) ? RuntimeDNA.Name : "None"));
//            CLIEngine.ShowMessage(string.Concat($"Description:                                ", !string.IsNullOrEmpty(RuntimeDNA.Description) ? RuntimeDNA.Description : "None"));
//            CLIEngine.ShowMessage(string.Concat($"Runtime Type:                               ", Enum.GetName(typeof(RuntimeType), RuntimeDNA.RuntimeType)));
//            CLIEngine.ShowMessage(string.Concat($"Created On:                                 ", RuntimeDNA.CreatedOn != DateTime.MinValue ? RuntimeDNA.CreatedOn.ToString() : "None"));
//            CLIEngine.ShowMessage(string.Concat($"Created By:                                 ", RuntimeDNA.CreatedByAvatarId != Guid.Empty ? string.Concat(RuntimeDNA.CreatedByAvatarUsername, " (", RuntimeDNA.CreatedByAvatarId.ToString(), ")") : "None"));
//            CLIEngine.ShowMessage(string.Concat($"Published On:                               ", RuntimeDNA.PublishedOn != DateTime.MinValue ? RuntimeDNA.PublishedOn.ToString() : "None"));
//            CLIEngine.ShowMessage(string.Concat($"Published By:                               ", RuntimeDNA.PublishedByAvatarId != Guid.Empty ? string.Concat(RuntimeDNA.PublishedByAvatarUsername, " (", RuntimeDNA.PublishedByAvatarId.ToString(), ")") : "None"));
//            CLIEngine.ShowMessage(string.Concat($"Runtime Published Path:                     ", !string.IsNullOrEmpty(RuntimeDNA.PublishedPath) ? RuntimeDNA.PublishedPath : "None"));
//            CLIEngine.ShowMessage(string.Concat($"Runtime Filesize:                           ", RuntimeDNA.RuntimeFileSize.ToString()));
//            //CLIEngine.ShowMessage(string.Concat($"Runtime Self Contained Published Path:                   ", !string.IsNullOrEmpty(RuntimeDNA.OAPPSelfContainedPublishedPath) ? RuntimeDNA.OAPPPublishedPath : "None"));
//            //CLIEngine.ShowMessage(string.Concat($"OAPP Self Contained Filesize:                         ", RuntimeDNA.OAPPSelfContainedFileSize.ToString()));
//            //CLIEngine.ShowMessage(string.Concat($"OAPP Self Contained Full Published Path:              ", !string.IsNullOrEmpty(RuntimeDNA.OAPPSelfContainedFullPublishedPath) ? RuntimeDNA.OAPPPublishedPath : "None"));
//            //CLIEngine.ShowMessage(string.Concat($"OAPP Self Contained Full Filesize:                    ", RuntimeDNA.OAPPSelfContainedFullFileSize.ToString()));
//            CLIEngine.ShowMessage(string.Concat($"Runtime Published On STARNET:               ", RuntimeDNA.RuntimePublishedOnSTARNET ? "True" : "False"));
//            CLIEngine.ShowMessage(string.Concat($"Runtime Published To Cloud:                 ", RuntimeDNA.RuntimePublishedToCloud ? "True" : "False"));
//            CLIEngine.ShowMessage(string.Concat($"Runtime Published To OASIS Provider:        ", Enum.GetName(typeof(ProviderType), RuntimeDNA.RuntimePublishedProviderType)));
//            //CLIEngine.ShowMessage(string.Concat($"OAPP Self Contained Published To Cloud:               ", RuntimeDNA.OAPPSelfContainedPublishedToCloud ? "True" : "False"));
//            //CLIEngine.ShowMessage(string.Concat($"OAPP Self Contained Published To OASIS Provider:      ", Enum.GetName(typeof(ProviderType), RuntimeDNA.OAPPSelfContainedPublishedProviderType)));
//            //CLIEngine.ShowMessage(string.Concat($"OAPP Self Contained Full Published To Cloud:          ", RuntimeDNA.OAPPSelfContainedFullPublishedToCloud ? "True" : "False"));
//            //CLIEngine.ShowMessage(string.Concat($"OAPP Self Contained Full Published To OASIS Provider: ", Enum.GetName(typeof(ProviderType), RuntimeDNA.OAPPSelfContainedFullPublishedProviderType)));{}90
//            CLIEngine.ShowMessage(string.Concat($"Version:                                    ", RuntimeDNA.Version));
//            CLIEngine.ShowMessage(string.Concat($"Versions:                                   ", RuntimeDNA.Versions));
//            CLIEngine.ShowMessage(string.Concat($"Downloads:                                  ", RuntimeDNA.Downloads));
//            //CLIEngine.ShowMessage(string.Concat($"STAR ODK Version:                                     ", RuntimeDNA.STARODKVersion));
//            //CLIEngine.ShowMessage(string.Concat($"OASIS Version:                                        ", RuntimeDNA.OASISVersion));
//            //CLIEngine.ShowMessage(string.Concat($"COSMIC Version:                                       ", RuntimeDNA.COSMICVersion));
//            //CLIEngine.ShowMessage(string.Concat($".NET Version:                                         ", RuntimeDNA.DotNetVersion));

//            CLIEngine.ShowDivider();
//        }

//        public static void ShowInstalledRuntime(IInstalledRuntime Runtime)
//        {
//            ShowRuntime(Runtime.RuntimeDNA);
//            CLIEngine.ShowMessage(string.Concat($"Installed On:                                ", Runtime.InstalledOn != DateTime.MinValue ? Runtime.InstalledOn.ToString() : "None"));
//            CLIEngine.ShowMessage(string.Concat($"Installed By:                                ", Runtime.InstalledBy != Guid.Empty ? string.Concat(Runtime.InstalledByAvatarUsername, " (", Runtime.InstalledBy.ToString(), ")") : "None"));
//            CLIEngine.ShowMessage(string.Concat($"Installed Path:                              ", Runtime.InstalledPath));
//            CLIEngine.ShowDivider();
//        }

//        public static async Task RepublishRuntimeAsync(string idOrName = "", ProviderType providerType = ProviderType.Default)
//        {
//            OASISResult<IOAPPTemplate> result = await LoadOAPPTemplateAsync(idOrName, "republish", true, providerType);

//            if (result != null && !result.IsError && result.Result != null)
//            {
//                OASISResult<IOAPPTemplate> unpublishResult = await STAR.OASISAPI.OAPPTemplates.RepublishOAPPTemplateAsync(STAR.BeamedInAvatar.Id, result.Result, providerType);

//                if (unpublishResult != null && !unpublishResult.IsError && unpublishResult.Result != null)
//                {
//                    CLIEngine.ShowSuccessMessage("OAPP Template Successfully Republished.");
//                    ShowOAPPTemplate(result.Result);
//                }
//                else
//                    CLIEngine.ShowErrorMessage($"An error occured unpublishing the OAPP Template. Reason: {unpublishResult.Message}");
//            }
//        }

//        public static async Task ActivateRuntimeAsync(string idOrName = "", ProviderType providerType = ProviderType.Default)
//        {
//            OASISResult<IOAPPTemplate> result = await LoadOAPPTemplateAsync(idOrName, "activate", true, providerType);

//            if (result != null && !result.IsError && result.Result != null)
//            {
//                OASISResult<IOAPPTemplate> unpublishResult = await STAR.OASISAPI.OAPPTemplates.UnpublishOAPPTemplateAsync(STAR.BeamedInAvatar.Id, result.Result, providerType);

//                if (unpublishResult != null && !unpublishResult.IsError && unpublishResult.Result != null)
//                {
//                    CLIEngine.ShowSuccessMessage("OAPP Template Successfully Activated.");
//                    ShowOAPPTemplate(result.Result);
//                }
//                else
//                    CLIEngine.ShowErrorMessage($"An error occured activating the OAPP Template. Reason: {unpublishResult.Message}");
//            }
//        }

//        public static async Task DeactivateRuntimeAsync(string idOrName = "", ProviderType providerType = ProviderType.Default)
//        {
//            OASISResult<IOAPPTemplate> result = await LoadOAPPTemplateAsync(idOrName, "deactivate", true, providerType);

//            if (result != null && !result.IsError && result.Result != null)
//            {
//                OASISResult<IOAPPTemplate> unpublishResult = await STAR.OASISAPI.OAPPTemplates.RepublishOAPPTemplateAsync(STAR.BeamedInAvatar.Id, result.Result, providerType);

//                if (unpublishResult != null && !unpublishResult.IsError && unpublishResult.Result != null)
//                {
//                    CLIEngine.ShowSuccessMessage("OAPP Template Successfully Deactivated.");
//                    ShowOAPPTemplate(result.Result);
//                }
//                else
//                    CLIEngine.ShowErrorMessage($"An error occured deactivating the OAPP Template. Reason: {unpublishResult.Message}");
//            }
//        }

//        public static async Task<OASISResult<IEnumerable<IInstalledOAPPTemplate>>> ListRuntimesUninstalledForBeamedInAvatarAsync(ProviderType providerType = ProviderType.Default)
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
//                        //Guid id = Guid.Empty;

//                        //if (template != null && template.MetaData != null && template.MetaData.ContainsKey("OAPPTemplateId") && template.MetaData.ContainsKey("OAPPTemplateId") != null && Guid.TryParse(template.MetaData.ContainsKey("OAPPTemplateId").ToString(), out id))
//                        if (template != null)
//                        {
//                            //OASISResult<IInstalledOAPPTemplate> installResult = await STAR.OASISAPI.OAPPTemplates.InstallOAPPTemplateAsync(STAR.BeamedInAvatar.Id, template.OAPPTemplateDNA.Id, template.OAPPTemplateDNA.VersionSequence, template.InstalledPath, template.DownloadedPath, true, true, providerType);
//                            OASISResult<IInstalledOAPPTemplate> installResult = await STAR.OASISAPI.OAPPTemplates.DownloadAndInstallOAPPTemplateAsync(STAR.BeamedInAvatar.Id, template, template.InstalledPath, template.DownloadedPath, true, true, providerType);
//                            //OASISResult<IInstalledOAPPTemplate> installResult = await DownloadAndInstallOAPPTemplateAsync(result.Result.ElementAt(number - 1).Id.ToString(), InstallMode.DownloadAndReInstall, providerType);

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

//        public static async Task<OASISResult<IEnumerable<IOAPPTemplate>>> ListRuntimesUnpublishedForBeamedInAvatarAsync(ProviderType providerType = ProviderType.Default)
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
//                        //Guid id = Guid.Empty;

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

//        public static async Task<OASISResult<IEnumerable<IOAPPTemplate>>> ListRuntimesDeactivatedForBeamedInAvatarAsync(ProviderType providerType = ProviderType.Default)
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
//                        //Guid id = Guid.Empty;

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

//        private static OASISResult<IEnumerable<IRuntime>> ListRuntimes(OASISResult<IEnumerable<IRuntime>> runtimes)
//        {
//            if (runtimes != null)
//            {
//                if (!runtimes.IsError)
//                {
//                    if (runtimes.Result != null && runtimes.Result.Count() > 0)
//                    {
//                        Console.WriteLine();

//                        if (runtimes.Result.Count() == 1)
//                            CLIEngine.ShowMessage($"{runtimes.Result.Count()} Runtime's Found:");
//                        else
//                            CLIEngine.ShowMessage($"{runtimes.Result.Count()} Runtime's Found:");

//                        CLIEngine.ShowDivider();

//                        foreach (IRuntime runtime in runtimes.Result)
//                            ShowRuntime(runtime.RuntimeDNA);

//                        //ShowRuntimeListFooter();
//                    }
//                    else
//                        CLIEngine.ShowWarningMessage("No Runtime's Found.");
//                }
//                else
//                    CLIEngine.ShowErrorMessage($"Error occured loading Runtime's. Reason: {runtimes.Message}");
//            }
//            else
//                CLIEngine.ShowErrorMessage($"Unknown error occured loading Runtime's.");

//            return runtimes;
//        }

//        private static void ListInstalledRuntimes(OASISResult<IEnumerable<IInstalledRuntime>> installedRuntimes)
//        {
//            if (installedRuntimes != null)
//            {
//                if (!installedRuntimes.IsError)
//                {
//                    if (installedRuntimes.Result != null && installedRuntimes.Result.Count() > 0)
//                    {
//                        Console.WriteLine();

//                        if (installedRuntimes.Result.Count() == 1)
//                            CLIEngine.ShowMessage($"{installedRuntimes.Result.Count()} Runtime's Found:");
//                        else
//                            CLIEngine.ShowMessage($"{installedRuntimes.Result.Count()} Runtime's Found:");

//                        CLIEngine.ShowDivider();

//                        foreach (IInstalledRuntime runtime in installedRuntimes.Result)
//                            ShowInstalledRuntime(runtime);

//                        //ShowRuntimeListFooter();
//                    }
//                    else
//                        CLIEngine.ShowWarningMessage("No Runtime's Found.");
//                }
//                else
//                    CLIEngine.ShowErrorMessage($"Error occured loading Runtime's. Reason: {installedRuntimes.Message}");
//            }
//            else
//                CLIEngine.ShowErrorMessage($"Unknown error occured loading Runtime's.");
//        }

//        private static async Task<OASISResult<IRuntime>> LoadRuntimeAsync(string idOrName, string operationName, ProviderType providerType = ProviderType.Default, bool addSpace = true)
//        {
//            OASISResult<IRuntime> result = new OASISResult<IRuntime>();
//            Guid id = Guid.Empty;

//            if (string.IsNullOrEmpty(idOrName))
//                idOrName = CLIEngine.GetValidInput($"What is the GUID/ID or Name to the Runtime you wish to {operationName}?");

//            if (addSpace)
//                Console.WriteLine("");

//            CLIEngine.ShowWorkingMessage("Loading Runtime...");

//            if (Guid.TryParse(idOrName, out id))
//                result = await STAR.OASISAPI.Runtimes.LoadRuntimeAsync(id, providerType);
//            else
//            {
//                OASISResult<IEnumerable<IRuntime>> allOAPPsTemplatesResult = await STAR.OASISAPI.Runtimes.LoadAllRuntimesAsync();

//                if (allOAPPsTemplatesResult != null && allOAPPsTemplatesResult.Result != null && !allOAPPsTemplatesResult.IsError)
//                {
//                    result.Result = allOAPPsTemplatesResult.Result.FirstOrDefault(x => x.Name == idOrName); //TODO: In future will use Where instead so user can select which Runtime they want... (if more than one matches the given name).

//                    if (result.Result == null)
//                    {
//                        result.IsError = true;
//                        result.Message = "No Runtime Was Found!";
//                    }
//                }
//                else
//                    CLIEngine.ShowErrorMessage($"An error occured calling STAR.OASISAPI.Runtimes.LoadRuntimeAsync. Reason: {allOAPPsTemplatesResult.Message}");
//            }

//            return result;
//        }

//        private static OASISResult<IRuntime> LoadRuntime(string idOrName, string operationName, ProviderType providerType = ProviderType.Default, bool addSpace = true)
//        {
//            OASISResult<IRuntime> result = new OASISResult<IRuntime>();
//            Guid id = Guid.Empty;

//            if (string.IsNullOrEmpty(idOrName))
//                idOrName = CLIEngine.GetValidInput($"What is the GUID/ID or Name to the Runtime you wish to {operationName}?");

//            if (addSpace)
//                Console.WriteLine("");

//            CLIEngine.ShowWorkingMessage("Loading Runtime...");

//            if (Guid.TryParse(idOrName, out id))
//                result = STAR.OASISAPI.Runtimes.LoadRuntime(id, providerType);
//            else
//            {
//                OASISResult<IEnumerable<IRuntime>> allOAPPsTemplatesResult = STAR.OASISAPI.Runtimes.LoadAllRuntimes(providerType);

//                if (allOAPPsTemplatesResult != null && allOAPPsTemplatesResult.Result != null && !allOAPPsTemplatesResult.IsError)
//                {
//                    result.Result = allOAPPsTemplatesResult.Result.FirstOrDefault(x => x.Name == idOrName); //TODO: In future will use Where instead so user can select which Runtime they want... (if more than one matches the given name).

//                    if (result.Result == null)
//                    {
//                        result.IsError = true;
//                        result.Message = "No Runtime Was Found!";
//                    }
//                }
//                else
//                    CLIEngine.ShowErrorMessage($"An error occured calling STAR.OASISAPI.Runtimes.LoadAllRuntimes. Reason: {allOAPPsTemplatesResult.Message}");
//            }

//            return result;
//        }

//        private static void Runtimes_OnRuntimePublishStatusChanged(object sender, RuntimePublishStatusEventArgs e)
//        {
//            switch (e.Status)
//            {
//                //case OAPPPublishStatus.DotNetPublishing:
//                //    CLIEngine.ShowWorkingMessage($"Dotnet Publishing...");
//                //    break;

//                case RuntimePublishStatus.Uploading:
//                    CLIEngine.ShowMessage("Uploading...");
//                    Console.WriteLine("");
//                    //CLIEngine.ShowWorkingMessage("Uploading... 0%");
//                    //CLIEngine.BeginWorkingMessage("Uploading... 0%");
//                    //CLIEngine.ShowProgressBar(0);
//                    break;

//                case RuntimePublishStatus.Published:
//                    CLIEngine.ShowSuccessMessage("Runtime Published Successfully");
//                    break;

//                case RuntimePublishStatus.Error:
//                    CLIEngine.ShowErrorMessage(e.ErrorMessage);
//                    break;

//                default:
//                    CLIEngine.ShowWorkingMessage($"{Enum.GetName(typeof(RuntimePublishStatus), e.Status)}...");
//                    break;
//            }
//        }

//        private static void Runtimes_OnRuntimeUploadStatusChanged(object sender, RuntimeUploadProgressEventArgs e)
//        {
//            //CLIEngine.ShowProgressBar(e.Progress, true);
//            //CLIEngine.ShowProgressBar(e.Progress);
//            //CLIEngine.UpdateWorkingMessageWithPercent(e.Progress);
//            //CLIEngine.UpdateWorkingMessage($"Uploading... {e.Progress}%"); //was this one.
//            CLIEngine.ShowProgressBar((double)e.Progress / (double)100);
//        }

//        private static void Runtimes_OnRuntimeInstallStatusChanged(object sender, RuntimeInstallStatusEventArgs e)
//        {
//            switch (e.Status)
//            {
//                case RuntimeInstallStatus.Downloading:
//                    CLIEngine.BeginWorkingMessage("Downloading...");
//                    //CLIEngine.ShowProgressBar(0);
//                    break;

//                case RuntimeInstallStatus.Installed:
//                    CLIEngine.ShowSuccessMessage("Runtime Installed Successfully");
//                    break;

//                case RuntimeInstallStatus.Error:
//                    CLIEngine.ShowErrorMessage(e.ErrorMessage);
//                    break;

//                default:
//                    CLIEngine.ShowWorkingMessage($"{Enum.GetName(typeof(OAPPInstallStatus), e.Status)}...");
//                    break;
//            }
//        }

//        private static void Runtimes_OnRuntimeDownloadStatusChanged(object sender, RuntimeDownloadProgressEventArgs e)
//        {
//            CLIEngine.UpdateWorkingMessage($"Downloading... {e.Progress}%");
//        }
//    }
//}