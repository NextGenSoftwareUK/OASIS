using NextGenSoftware.Utilities;
using NextGenSoftware.CLI.Engine;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.ONODE.Core.Holons;
using NextGenSoftware.OASIS.API.ONODE.Core.Managers;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Holons;
using NextGenSoftware.OASIS.API.ONODE.Core.Objects;

namespace NextGenSoftware.OASIS.STAR.CLI.Lib
{
    public class Runtimes : STARNETUIBase<Runtime, DownloadedRuntime, InstalledRuntime, RuntimeDNA>
    {
        public Runtimes(Guid avatarId) : base(new RuntimeManager(avatarId),
            "Welcome to the Runtime Wizard", new List<string> 
            {
                "This wizard will allow you create an Runtime which can be used to create a OAPP from (along with a OAPP Template).",
                "The runtime can be created from anything you like from any stack, platform, OS etc.",
                "The STAR & OASIS runtimes can only be created by an admin/wizard.",
                "The wizard will create an empty folder with a RuntimeDNA.json file in it. You then simply place any files/folders you need into this folder.",
                "Finally you run the sub-command 'runtime publish' to convert the folder containing the runtime (can contain any number of files and sub-folders) into a OASIS Runtime file (.oruntime) as well as optionally upload to STARNET.",
                "You can then share the .oruntime file with others across any platform or OS from which they can create OAPP's from (along with a OAPP Template, you can even use the same OAPP Template for different runtimes). They can install the Runtime from the file using the sub-command 'runtime install'.",
                "You can also optionally choose to upload the .oruntime file to the STARNET store so others can search, download and install the runtime. They can then create OAPP's from the runtime."
            },
            STAR.STARDNA.DefaultRuntimesSourcePath, "DefaultRuntimesSourcePath",
            STAR.STARDNA.DefaultRuntimesPublishedPath, "DefaultRuntimesPublishedPath",
            STAR.STARDNA.DefaultRuntimesDownloadedPath, "DefaultRuntimesDownloadedPath",
            STAR.STARDNA.DefaultRuntimesInstalledPath, "DefaultRuntimesInstalledPath")
        { }


        public async Task<OASISResult<bool>> InstallDependentRuntimesAsync(ISTARNETDNA STARNETDNA, string OAPPFolder, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<bool> result = new OASISResult<bool>();
            string errorMessage = "An error occured in InstallDependentRuntimes. Reason:";
            string downloadPath = "";
            string installPath = "";
            string OASISRunTimePath = STAR.STARDNA.DefaultRuntimesInstalledOASISPath;
            string STARRunTimePath = STAR.STARDNA.DefaultRuntimesInstalledSTARPath;

            if (!string.IsNullOrEmpty(STAR.STARDNA.BaseSTARNETPath))
            {
                OASISRunTimePath = Path.Combine(STAR.STARDNA.BaseSTARNETPath, STAR.STARDNA.DefaultRuntimesInstalledOASISPath);
                STARRunTimePath = Path.Combine(STAR.STARDNA.BaseSTARNETPath, STAR.STARDNA.DefaultRuntimesInstalledSTARPath);
            }

            OASISRunTimePath = Path.Combine(OASISRunTimePath, string.Concat("OASIS Runtime_v", STARNETDNA.OASISRuntimeVersion));
            STARRunTimePath = Path.Combine(STARRunTimePath, string.Concat("STAR Runtime_v", STARNETDNA.STARRuntimeVersion));

            //If the OASIS Runtime folder does not exist in the OAPP folder, then we need to copy it from the installed runtimes folder.
            if (!Directory.Exists(Path.Combine(OAPPFolder, "Dependencies", "STARNET", "Runtimes", "OASIS Runtime")))
            {
                //Copy the correct runtimes to the OAPP folder.
                if (Directory.Exists(OASISRunTimePath))
                    DirectoryHelper.CopyFilesRecursively(OASISRunTimePath, Path.Combine(OAPPFolder, "Dependencies", "STARNET", "Runtimes", "OASIS Runtime"));
                else
                {
                    CLIEngine.ShowWarningMessage($"The target OASIS Runtime {STARNETDNA.OASISRuntimeVersion} is not installed!");

                    if (CLIEngine.GetConfirmation("Do you wish to download & install now?"))
                    {
                        if (Path.IsPathRooted(STAR.STARDNA.DefaultRuntimesDownloadedPath) || string.IsNullOrEmpty(STAR.STARDNA.BaseSTARNETPath))
                            downloadPath = STAR.STARDNA.DefaultRuntimesDownloadedPath;
                        else
                            downloadPath = Path.Combine(STAR.STARDNA.BaseSTARNETPath, STAR.STARDNA.DefaultRuntimesDownloadedPath);


                        if (Path.IsPathRooted(STAR.STARDNA.DefaultRuntimesInstalledOASISPath) || string.IsNullOrEmpty(STAR.STARDNA.BaseSTARNETPath))
                            installPath = STAR.STARDNA.DefaultRuntimesInstalledOASISPath;
                        else
                            installPath = Path.Combine(STAR.STARDNA.BaseSTARNETPath, STAR.STARDNA.DefaultRuntimesInstalledOASISPath);

                        Console.WriteLine("");
                        //STAR.STARAPI.Runtimes.OnDownloadStatusChanged += Runtimes_OnDownloadStatusChanged;
                        //STAR.STARAPI.Runtimes.OnInstallStatusChanged += Runtimes_OnInstallStatusChanged;
                        OASISResult<IInstalledRuntime> installResult = await ((RuntimeManager)STARNETManager).DownloadAndInstallOASISRuntimeAsync(STAR.BeamedInAvatar.Id, STARNETDNA.OASISRuntimeVersion, downloadPath, installPath, providerType);
                        //STAR.STARAPI.Runtimes.OnDownloadStatusChanged -= Runtimes_OnDownloadStatusChanged;
                        //STAR.STARAPI.Runtimes.OnInstallStatusChanged -= Runtimes_OnInstallStatusChanged;

                        if (installResult != null && installResult.Result != null && !installResult.IsError)
                        {
                            CLIEngine.ShowWorkingMessage("Copying OASIS Runtime files to OAPP folder...");
                            DirectoryHelper.CopyFilesRecursively(OASISRunTimePath, Path.Combine(OAPPFolder, "Runtimes", "OASIS Runtime"));
                        }
                        else
                        {
                            OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured downloading & installing the OASIS Runtime {STARNETDNA.OASISRuntimeVersion}. Reason: {installResult.Message}");
                            return result;
                        }
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} The target OASIS Runtime {STARNETDNA.OASISRuntimeVersion} is not installed!");
                        return result;
                    }
                }
            }

            //If the STAR Runtime folder does not exist in the OAPP folder, then we need to copy it from the installed runtimes folder.
            if (!Directory.Exists(Path.Combine(OAPPFolder, "Dependencies", "STARNET", "Runtimes", "STAR Runtime")))
            {
                if (Directory.Exists(STARRunTimePath))
                    DirectoryHelper.CopyFilesRecursively(STARRunTimePath, Path.Combine(OAPPFolder, "Dependencies", "STARNET", "Runtimes", "STAR Runtime"));
                else
                {
                    CLIEngine.ShowWarningMessage($"The target STAR Runtime {STARNETDNA.STARRuntimeVersion} is not installed!");

                    if (CLIEngine.GetConfirmation("Do you wish to download & install now?"))
                    {
                        if (Path.IsPathRooted(STAR.STARDNA.DefaultRuntimesInstalledSTARPath) || string.IsNullOrEmpty(STAR.STARDNA.BaseSTARNETPath))
                            installPath = STAR.STARDNA.DefaultRuntimesInstalledSTARPath;
                        else
                            installPath = Path.Combine(STAR.STARDNA.BaseSTARNETPath, STAR.STARDNA.DefaultRuntimesInstalledOASISPath);

                        Console.WriteLine("");
                        //STAR.STARAPI.Runtimes.OnDownloadStatusChanged += Runtimes_OnDownloadStatusChanged;
                        //STAR.STARAPI.Runtimes.OnInstallStatusChanged += Runtimes_OnInstallStatusChanged;
                        //OASISResult<IInstalledRuntime> installResult = await STAR.STARAPI.Runtimes.DownloadAndInstallSTARRuntimeAsync(BeamedInAvatar.Id, installedOAPPTemplateResult.Result.STARNETDNA.STARRuntimeVersion, downloadPath, installPath, providerType);
                        OASISResult<IInstalledRuntime> installResult = await ((RuntimeManager)STARNETManager).DownloadAndInstallSTARRuntimeAsync(STAR.BeamedInAvatar.Id, STARNETDNA.STARRuntimeVersion, downloadPath, installPath, providerType);
                        //STAR.STARAPI.Runtimes.OnDownloadStatusChanged -= Runtimes_OnDownloadStatusChanged;
                        //STAR.STARAPI.Runtimes.OnInstallStatusChanged -= Runtimes_OnInstallStatusChanged;

                        if (installResult != null && installResult.Result != null && !installResult.IsError)
                        {
                            CLIEngine.ShowWorkingMessage("Copying STAR Runtime files to OAPP folder...");
                            DirectoryHelper.CopyFilesRecursively(STARRunTimePath, Path.Combine(OAPPFolder, "Runtimes", "STAR Runtime"));
                        }
                        else
                        {
                            OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured downloading & installing the STAR Runtime {STARNETDNA.OASISRuntimeVersion}. Reason: {installResult.Message}");
                            return result;
                        }
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} The target STAR Runtime {STARNETDNA.STARRuntimeVersion} is not installed!");
                        return result;
                    }
                }
            }

            result.Result = true;
            return result;
        }
    }
}