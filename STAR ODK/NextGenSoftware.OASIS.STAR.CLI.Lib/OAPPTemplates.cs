using NextGenSoftware.CLI.Engine;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.ONODE.Core.Holons;
using NextGenSoftware.OASIS.API.ONODE.Core.Objects;
using NextGenSoftware.OASIS.API.ONODE.Core.Managers;
using NextGenSoftware.OASIS.STAR.CLI.Lib.Enums;

namespace NextGenSoftware.OASIS.STAR.CLI.Lib
{
    public class OAPPTemplates : OAPPBase<OAPPTemplate, DownloadedOAPPTemplate, InstalledOAPPTemplate, OAPPTemplateDNA>
    {
        public OAPPTemplates(Guid avatarId) : base(new OAPPTemplateManager(avatarId),
            "Welcome to the OAPP Template Wizard", new List<string> 
            {
                "This wizard will allow you create an OAPP Template from which OAPP's can be created from.",
                "You can also plug in different Runtimes into your OAPP to allow it to run on different platforms, devices and operating systems. You can make unique cominations of different OAPP Template's and Runtimes's to create OAPP's from so you can re-use the same OAPP Tempalte with a different backend/runtime if you wish, the possibilityies are endless! :)",
                "The OAPP Template can be created from anything you like such as a website, javascript template, game, app, service, etc in any language, platform or OS.",
                "You simply need to add specefic STAR ODK OAPP Template reserved tags where dynamic data will be injected in from the OAPP meta data.",
                "The wizard will create an empty folder with a OAPPTemplateDNA.json file in it. You then simply place any files/folders you need into this folder.",
                "Finally you run the sub-command 'oapp template publish' to convert the folder containing the OAPP Template (can contain any number of files and sub-folders) into a OAPP Template file (.oapptemplate) as well as optionally upload to STARNET.",
                "You can then share the .oapptemplate file with others across any platform or OS, who can then install the OAPP Template from the file using the sub-command 'oapp template install'. You can also optionally choose to upload the .oapptemplate file to STARNET so others can search, download and install the OAPP Template. They can then create OAPP's from the template.",
                "You can also optionally choose to upload the .oapptemplate file to the STARNET store so others can search, download and install the quest."
            },
            STAR.STARDNA.DefaultOAPPTemplatesSourcePath, "DefaultOAPPTemplatesSourcePath",
            STAR.STARDNA.DefaultOAPPTemplatesPublishedPath, "DefaultOAPPTemplatesPublishedPath",
            STAR.STARDNA.DefaultOAPPTemplatesDownloadedPath, "DefaultOAPPTemplatesDownloadedPath",
            STAR.STARDNA.DefaultOAPPTemplatesInstalledPath, "DefaultOAPPTemplatesInstalledPath")
        { }

        public override async Task<OASISResult<OAPPTemplate>> CreateAsync(object createParams, OAPPTemplate newHolon = null, bool showHeaderAndInro = true, bool checkIfSourcePathExists = true, object holonSubType = null, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<OAPPTemplate> createResult = await base.CreateAsync(createParams, newHolon, showHeaderAndInro, checkIfSourcePathExists, holonSubType, providerType);

            if (createResult != null && createResult.Result != null && !createResult.IsError)
            {
                 await AddLibsRuntimesAndTemplatesAsync(createResult.Result.STARNETDNA, "OAPP Template", providerType);

                //if (CLIEngine.GetConfirmation("Do you wish to add any custom runtimes now? (you do not need to add the OASIS or STAR runtimes, they are added automatically)."))
                //{
                //    do
                //    {
                //        OASISResult<InstalledRuntime> installedRuntime = await STARCLI.Runtimes.FindAndInstallIfNotInstalledAsync("use", providerType: providerType);

                //        if (installedRuntime != null && installedRuntime.Result != null && !installedRuntime.IsError)
                //        {
                //            CLIEngine.ShowWorkingMessage("Installing Runtime Into Template...");
                //            OASISResult<IOAPPTemplate> addRuntimeToTemplateResult = await ((OAPPTemplateManager)STARNETManager).AddRuntimeToOAPPTemplateAsync(STAR.BeamedInAvatar.Id, createResult.Result.STARNETDNA.Id, createResult.Result.STARNETDNA.Version, installedRuntime.Result.STARNETDNA.Id, installedRuntime.Result.STARNETDNA.Version, providerType);

                //            if (addRuntimeToTemplateResult != null && addRuntimeToTemplateResult.Result != null && !addRuntimeToTemplateResult.IsError)
                //            {
                //                DirectoryHelper.CopyFilesRecursively(installedRuntime.Result.InstalledPath, Path.Combine(createResult.Result.STARNETDNA.SourcePath, "Runtimes"));
                //                //CLIEngine.ShowMessage($"You can now use the runtime in your OAPP Template '{createResult.Result.Name}' by using the reserved tags in your OAPP Template files. For more information on how to do this, please refer to the documentation for the runtime you just added.");
                //                CLIEngine.ShowSuccessMessage($"Runtime '{installedRuntime.Result.Name}' added to OAPP Template '{createResult.Result.Name}'.");
                //            }
                //            else
                //                CLIEngine.ShowErrorMessage($"Failed to add runtime '{installedRuntime.Result.Name}' to OAPP Template '{createResult.Result.Name}'. Error: {addRuntimeToTemplateResult.Message}");
                //        }
                //        else
                //            CLIEngine.ShowErrorMessage($"Failed to add runtime to OAPP Template '{createResult.Result.Name}'. Error: {installedRuntime.Message}");
                //    }
                //    while (CLIEngine.GetConfirmation("Do you wish to add another custom runtime?"));
                //}

                //if (CLIEngine.GetConfirmation("Do you wish to add any libraries now?"))
                //{
                //    do
                //    {
                //        OASISResult<InstalledLibrary> installedLib = await STARCLI.Libs.FindAndInstallIfNotInstalledAsync("use", providerType: providerType);

                //        if (installedLib != null && installedLib.Result != null && !installedLib.IsError)
                //        {
                //            CLIEngine.ShowWorkingMessage("Installing Library Into Template...");
                //            OASISResult<IOAPPTemplate> addLibToTemplateResult = await ((OAPPTemplateManager)STARNETManager).AddLibraryToOAPPTemplateAsync(STAR.BeamedInAvatar.Id, createResult.Result.STARNETDNA.Id, createResult.Result.STARNETDNA.Version, installedLib.Result.STARNETDNA.Id, installedLib.Result.STARNETDNA.Version, providerType);

                //            if (addLibToTemplateResult != null && addLibToTemplateResult.Result != null && !addLibToTemplateResult.IsError)
                //            {
                //                DirectoryHelper.CopyFilesRecursively(installedLib.Result.InstalledPath, Path.Combine(createResult.Result.STARNETDNA.SourcePath, "Libs"));
                //                CLIEngine.ShowSuccessMessage($"Library '{installedLib.Result.Name}' added to OAPP Template '{createResult.Result.Name}'.");
                //                //CLIEngine.ShowMessage($"You can now use the library in your OAPP Template '{createResult.Result.Name}' by using the reserved tags in your OAPP Template files. For more information on how to do this, please refer to the documentation for the library you just added.");
                //            }
                //            else
                //                CLIEngine.ShowErrorMessage($"Failed to add library '{installedLib.Result.Name}' to OAPP Template '{createResult.Result.Name}'. Error: {addLibToTemplateResult.Message}");
                //        }
                //        else
                //            CLIEngine.ShowErrorMessage($"Failed to add library to OAPP Template '{createResult.Result.Name}'. Error: {installedLib.Message}");
                //    }
                //    while (CLIEngine.GetConfirmation("Do you wish to add another library?"));
                //}
            }

            return createResult;
        }

        public override async Task<OASISResult<OAPPTemplate>> PublishAsync(string sourcePath = "", bool edit = false, DefaultLaunchMode defaultLaunchMode = DefaultLaunchMode.Optional, ProviderType providerType = ProviderType.Default)
        {
            //if (!CLIEngine.GetConfirmation("Do you wish to embed the libraries, runtimes & sub-templates in the template? (It is not recommended because will increase the storage space/cost & upload/download time). If you choose 'N' then they will be automatically downloaded and installed when someone installs your template. Only choose 'Y' if you want them embedded in case there is an issue downloading/installing them seperatley later (unlikely) or if you want the template to be fully self-contained with no external dependencies (useful if you wish to install it offline from the .oapptemplate file)."))
            //{
            //    if (!CLIEngine.GetConfirmation("Do you wish to embed the runtimes?"))
            //    {
            //        if (Directory.Exists(Path.Combine(sourcePath, "Runtimes")))
            //            Directory.Delete(Path.Combine(sourcePath, "Runtimes"), true);
            //    }

            //    if (!CLIEngine.GetConfirmation("Do you wish to embed the libraries?"))
            //    {
            //        if (Directory.Exists(Path.Combine(sourcePath, "Libs")))
            //            Directory.Delete(Path.Combine(sourcePath, "Libs"), true);
            //    }

            //    if (!CLIEngine.GetConfirmation("Do you wish to embed the sub-templates?"))
            //    {
            //        if (Directory.Exists(Path.Combine(sourcePath, "Templates")))
            //            Directory.Delete(Path.Combine(sourcePath, "Templates"), true);
            //    }
            //}

            return await base.PublishAsync(sourcePath, edit, defaultLaunchMode, providerType);
        }
    }
}