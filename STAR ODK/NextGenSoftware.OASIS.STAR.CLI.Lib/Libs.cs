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
    public class Libs : STARNETUIBase<Library, DownloadedLibrary, InstalledLibrary, LibraryDNA>
    {
        public Libs(Guid avatarId) : base(new LibsManager(avatarId),
            "Welcome to the Library Wizard", new List<string> 
            {
                "This wizard will allow you create a Library which can be used in a OAPP from (along with a OAPP Template & Runtime).",
                "The library can be created from anything you like from any stack, platform, OS etc.",
                "You can even link an existing library from any site or package store to this one. When you publish you will be given an option to do so.",
                "The wizard will create an empty folder with a LibraryDNA.json file in it. You then simply place any files/folders you need into this folder (if you are linking to an existing library then you can skip this step).",
                "Finally you run the sub-command 'library publish' to convert the folder containing the library (can contain any number of files and sub-folders) into a OASIS Library file (.olibrary) as well as optionally upload to STARNET.",
                "You can then share the .olibrary file with others across any platform or OS from which they can include in OAPP's. They can install the Library from the file using the sub-command 'library install'.",
                "You can also optionally choose to upload the .olibrary file to the STARNET store so others can search, download and install the library. They can then create OAPP's from the library."
            },
            STAR.STARDNA.DefaultLibsSourcePath, "DefaultLibsSourcePath",
            STAR.STARDNA.DefaultLibsPublishedPath, "DefaultLibsPublishedPath",
            STAR.STARDNA.DefaultLibsDownloadedPath, "DefaultLibsDownloadedPath",
            STAR.STARDNA.DefaultLibsInstalledPath, "DefaultLibsInstalledPath")
        { }


        public async Task<OASISResult<bool>> InstallDependentLibsAsync(ISTARNETDNA STARNETDNA, string OAPPFolder, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<bool> result = new OASISResult<bool>();
            string errorMessage = "An error occured in InstallDependentLibs. Reason:";
            string downloadPath = "";
            string installPath = "";
            string OASISRunTimePath = STAR.STARDNA.DefaultLibsInstalledOASISPath;
            string STARRunTimePath = STAR.STARDNA.DefaultLibsInstalledSTARPath;

            if (!string.IsNullOrEmpty(STAR.STARDNA.BaseSTARNETPath))
            {
                OASISRunTimePath = Path.Combine(STAR.STARDNA.BaseSTARNETPath, STAR.STARDNA.DefaultLibsInstalledOASISPath);
                STARRunTimePath = Path.Combine(STAR.STARDNA.BaseSTARNETPath, STAR.STARDNA.DefaultLibsInstalledSTARPath);
            }

            OASISRunTimePath = Path.Combine(OASISRunTimePath, string.Concat("OASIS Library_v", STARNETDNA.OASISLibraryVersion));
            STARRunTimePath = Path.Combine(STARRunTimePath, string.Concat("STAR Library_v", STARNETDNA.STARLibraryVersion));

            //If the OASIS Library folder does not exist in the OAPP folder, then we need to copy it from the installed librarys folder.
            if (!Directory.Exists(Path.Combine(OAPPFolder, "Libs", "OASIS Library")))
            {
                //Copy the correct librarys to the OAPP folder.
                if (Directory.Exists(OASISRunTimePath))
                    DirectoryHelper.CopyFilesRecursively(OASISRunTimePath, Path.Combine(OAPPFolder, "Libs", "OASIS Library"));
                else
                {
                    CLIEngine.ShowWarningMessage($"The target OASIS Library {STARNETDNA.OASISLibraryVersion} is not installed!");

                    if (CLIEngine.GetConfirmation("Do you wish to download & install now?"))
                    {
                        if (Path.IsPathRooted(STAR.STARDNA.DefaultLibsDownloadedPath) || string.IsNullOrEmpty(STAR.STARDNA.BaseSTARNETPath))
                            downloadPath = STAR.STARDNA.DefaultLibsDownloadedPath;
                        else
                            downloadPath = Path.Combine(STAR.STARDNA.BaseSTARNETPath, STAR.STARDNA.DefaultLibsDownloadedPath);


                        if (Path.IsPathRooted(STAR.STARDNA.DefaultLibsInstalledOASISPath) || string.IsNullOrEmpty(STAR.STARDNA.BaseSTARNETPath))
                            installPath = STAR.STARDNA.DefaultLibsInstalledOASISPath;
                        else
                            installPath = Path.Combine(STAR.STARDNA.BaseSTARNETPath, STAR.STARDNA.DefaultLibsInstalledOASISPath);

                        Console.WriteLine("");
                        //STAR.STARAPI.Libs.OnDownloadStatusChanged += Libs_OnDownloadStatusChanged;
                        //STAR.STARAPI.Libs.OnInstallStatusChanged += Libs_OnInstallStatusChanged;
                        OASISResult<IInstalledLibrary> installResult = await ((LibraryManager)STARNETManager).DownloadAndInstallOASISLibraryAsync(STAR.BeamedInAvatar.Id, STARNETDNA.OASISLibraryVersion, downloadPath, installPath, providerType);
                        //STAR.STARAPI.Libs.OnDownloadStatusChanged -= Libs_OnDownloadStatusChanged;
                        //STAR.STARAPI.Libs.OnInstallStatusChanged -= Libs_OnInstallStatusChanged;

                        if (installResult != null && installResult.Result != null && !installResult.IsError)
                        {
                            CLIEngine.ShowWorkingMessage("Copying OASIS Library files to OAPP folder...");
                            DirectoryHelper.CopyFilesRecursively(OASISRunTimePath, Path.Combine(OAPPFolder, "Libs", "OASIS Library"));
                        }
                        else
                        {
                            OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured downloading & installing the OASIS Library {STARNETDNA.OASISLibraryVersion}. Reason: {installResult.Message}");
                            return result;
                        }
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} The target OASIS Library {STARNETDNA.OASISLibraryVersion} is not installed!");
                        return result;
                    }
                }
            }

            //If the STAR Library folder does not exist in the OAPP folder, then we need to copy it from the installed librarys folder.
            if (!Directory.Exists(Path.Combine(OAPPFolder, "Libs", "STAR Library")))
            {
                if (Directory.Exists(STARRunTimePath))
                    DirectoryHelper.CopyFilesRecursively(STARRunTimePath, Path.Combine(OAPPFolder, "Libs", "STAR Library"));
                else
                {
                    CLIEngine.ShowWarningMessage($"The target STAR Library {STARNETDNA.STARLibraryVersion} is not installed!");

                    if (CLIEngine.GetConfirmation("Do you wish to download & install now?"))
                    {
                        if (Path.IsPathRooted(STAR.STARDNA.DefaultLibsInstalledSTARPath) || string.IsNullOrEmpty(STAR.STARDNA.BaseSTARNETPath))
                            installPath = STAR.STARDNA.DefaultLibsInstalledSTARPath;
                        else
                            installPath = Path.Combine(STAR.STARDNA.BaseSTARNETPath, STAR.STARDNA.DefaultLibsInstalledOASISPath);

                        Console.WriteLine("");
                        //STAR.STARAPI.Libs.OnDownloadStatusChanged += Libs_OnDownloadStatusChanged;
                        //STAR.STARAPI.Libs.OnInstallStatusChanged += Libs_OnInstallStatusChanged;
                        //OASISResult<IInstalledLibrary> installResult = await STAR.STARAPI.Libs.DownloadAndInstallSTARLibraryAsync(BeamedInAvatar.Id, installedOAPPTemplateResult.Result.STARNETDNA.STARLibraryVersion, downloadPath, installPath, providerType);
                        OASISResult<IInstalledLibrary> installResult = await ((LibraryManager)STARNETManager).DownloadAndInstallSTARLibraryAsync(STAR.BeamedInAvatar.Id, STARNETDNA.STARLibraryVersion, downloadPath, installPath, providerType);
                        //STAR.STARAPI.Libs.OnDownloadStatusChanged -= Libs_OnDownloadStatusChanged;
                        //STAR.STARAPI.Libs.OnInstallStatusChanged -= Libs_OnInstallStatusChanged;

                        if (installResult != null && installResult.Result != null && !installResult.IsError)
                        {
                            CLIEngine.ShowWorkingMessage("Copying STAR Library files to OAPP folder...");
                            DirectoryHelper.CopyFilesRecursively(STARRunTimePath, Path.Combine(OAPPFolder, "Libs", "STAR Library"));
                        }
                        else
                        {
                            OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured downloading & installing the STAR Library {STARNETDNA.OASISLibraryVersion}. Reason: {installResult.Message}");
                            return result;
                        }
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} The target STAR Library {STARNETDNA.STARLibraryVersion} is not installed!");
                        return result;
                    }
                }
            }

            result.Result = true;
            return result;
        }
    }
}