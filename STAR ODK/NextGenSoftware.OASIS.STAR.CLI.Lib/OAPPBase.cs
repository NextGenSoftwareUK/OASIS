using NextGenSoftware.Utilities;
using NextGenSoftware.CLI.Engine;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.ONODE.Core.Holons;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Holons;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Managers;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.ONODE.Core.Managers;

namespace NextGenSoftware.OASIS.STAR.CLI.Lib
{
    public class OAPPBase<T1, T2, T3, T4> : STARNETUIBase<T1, T2, T3, T4>
        where T1 : IOAPPBase, new()
        where T2 : IDownloadedSTARNETHolon, new()
        where T3 : IInstalledSTARNETHolon, new()
        where T4 : ISTARNETDNA, new()
    {
        public OAPPBase(ISTARNETManagerBase<T1, T2, T3, T4> starManager, string createHeader, List<string> createIntroParagraphs, string sourcePath = "", string sourceSTARDNAKey = "", string publishedPath = "", string publishedSTARDNAKey = "", string downloadedPath = "", string downloadSTARDNAKey = "", string installedPath = "", string installedSTARDNAKey = "", int displayFieldLength = DEFAULT_FIELD_LENGTH) : base(starManager, createHeader, createIntroParagraphs, sourcePath, sourceSTARDNAKey, publishedPath, publishedSTARDNAKey, downloadedPath, downloadSTARDNAKey, installedPath, installedSTARDNAKey, displayFieldLength)
        {
            
        }

        protected async Task AddLibsRuntimesAndTemplatesAsync(ISTARNETDNA STARNETDNA, string holonTypeToAddTo, ProviderType providerType = ProviderType.Default)
        {
            if (CLIEngine.GetConfirmation("Do you wish to add any custom runtimes now? (you do not need to add the OASIS or STAR runtimes, they are added automatically)."))
            {
                do
                {
                    OASISResult<InstalledRuntime> installedRuntime = await STARCLI.Runtimes.FindAndInstallIfNotInstalledAsync("use", providerType: providerType);

                    if (installedRuntime != null && installedRuntime.Result != null && !installedRuntime.IsError)
                    {
                        CLIEngine.ShowWorkingMessage($"Installing Runtime Into {holonTypeToAddTo}...");
                        OASISResult<T1> addRuntimeResult = await ((IOAPPManagerBase<T1, T2, T3, T4>)STARNETManager).AddRuntimeAsync(STAR.BeamedInAvatar.Id, STARNETDNA.Id, STARNETDNA.Version, installedRuntime.Result.STARNETDNA.Id, installedRuntime.Result.STARNETDNA.Version, providerType);

                        if (addRuntimeResult != null && addRuntimeResult.Result != null && !addRuntimeResult.IsError)
                        {
                            DirectoryHelper.CopyFilesRecursively(installedRuntime.Result.InstalledPath, Path.Combine(STARNETDNA.SourcePath, "Dependencies", "STARNET", "Runtimes"));
                            //CLIEngine.ShowMessage($"You can now use the runtime in your OAPP Template '{createResult.Result.Name}' by using the reserved tags in your OAPP Template files. For more information on how to do this, please refer to the documentation for the runtime you just added.");
                            CLIEngine.ShowSuccessMessage($"Runtime '{installedRuntime.Result.Name}' added to {holonTypeToAddTo} '{STARNETDNA.Name}'.");
                        }
                        else
                            CLIEngine.ShowErrorMessage($"Failed to add runtime '{installedRuntime.Result.Name}' to {holonTypeToAddTo} '{STARNETDNA.Name}'. Error: {addRuntimeResult.Message}");
                    }
                    else
                        CLIEngine.ShowErrorMessage($"Failed to add runtime to {holonTypeToAddTo} '{STARNETDNA.Name}'. Error: {installedRuntime.Message}");
                }
                while (CLIEngine.GetConfirmation("Do you wish to add another custom runtime?"));
            }

            if (CLIEngine.GetConfirmation("Do you wish to add any libraries now?"))
            {
                do
                {
                    OASISResult<InstalledLibrary> installedLib = await STARCLI.Libs.FindAndInstallIfNotInstalledAsync("use", providerType: providerType);

                    if (installedLib != null && installedLib.Result != null && !installedLib.IsError)
                    {
                        CLIEngine.ShowWorkingMessage($"Installing Library Into {holonTypeToAddTo}...");
                        OASISResult<T1> addLibResult = await ((IOAPPManagerBase<T1, T2, T3, T4>)STARNETManager).AddLibraryAsync(STAR.BeamedInAvatar.Id, STARNETDNA.Id, STARNETDNA.Version, installedLib.Result.STARNETDNA.Id, installedLib.Result.STARNETDNA.Version, providerType);

                        if (addLibResult != null && addLibResult.Result != null && !addLibResult.IsError)
                        {
                            DirectoryHelper.CopyFilesRecursively(installedLib.Result.InstalledPath, Path.Combine(STARNETDNA.SourcePath, "Dependencies", "STARNET", "Libs"));
                            CLIEngine.ShowSuccessMessage($"Library '{installedLib.Result.Name}' added to {holonTypeToAddTo} '{STARNETDNA.Name}'.");
                            //CLIEngine.ShowMessage($"You can now use the library in your OAPP Template '{createResult.Result.Name}' by using the reserved tags in your OAPP Template files. For more information on how to do this, please refer to the documentation for the library you just added.");
                        }
                        else
                            CLIEngine.ShowErrorMessage($"Failed to add library '{installedLib.Result.Name}' to {holonTypeToAddTo} '{STARNETDNA.Name}'. Error: {addLibResult.Message}");
                    }
                    else
                        CLIEngine.ShowErrorMessage($"Failed to add library to {holonTypeToAddTo} '{STARNETDNA.Name}'. Error: {installedLib.Message}");
                }
                while (CLIEngine.GetConfirmation("Do you wish to add another library?"));
            }

            if (CLIEngine.GetConfirmation("Do you wish to add any sub-templates now?"))
            {
                do
                {
                    OASISResult<InstalledOAPPTemplate> installedTemplate = await STARCLI.OAPPTemplates.FindAndInstallIfNotInstalledAsync("use", providerType: providerType);

                    if (installedTemplate != null && installedTemplate.Result != null && !installedTemplate.IsError)
                    {
                        CLIEngine.ShowWorkingMessage($"Installing Library Into {holonTypeToAddTo}...");
                        OASISResult<T1> addTemplateResult = await ((IOAPPManagerBase<T1, T2, T3, T4>)STARNETManager).AddLibraryAsync(STAR.BeamedInAvatar.Id, STARNETDNA.Id, STARNETDNA.Version, installedTemplate.Result.STARNETDNA.Id, installedTemplate.Result.STARNETDNA.Version, providerType);

                        if (addTemplateResult != null && addTemplateResult.Result != null && !addTemplateResult.IsError)
                        {
                            DirectoryHelper.CopyFilesRecursively(installedTemplate.Result.InstalledPath, Path.Combine(STARNETDNA.SourcePath, "Dependencies", "STARNET", "Templates"));
                            CLIEngine.ShowSuccessMessage($"Template '{installedTemplate.Result.Name}' added to {holonTypeToAddTo} '{STARNETDNA.Name}'.");
                            //CLIEngine.ShowMessage($"You can now use the library in your OAPP Template '{createResult.Result.Name}' by using the reserved tags in your OAPP Template files. For more information on how to do this, please refer to the documentation for the library you just added.");
                        }
                        else
                            CLIEngine.ShowErrorMessage($"Failed to add template '{installedTemplate.Result.Name}' to {holonTypeToAddTo} '{STARNETDNA.Name}'. Error: {addTemplateResult.Message}");
                    }
                    else
                        CLIEngine.ShowErrorMessage($"Failed to add template to {holonTypeToAddTo} '{STARNETDNA.Name}'. Error: {installedTemplate.Message}");
                }
                while (CLIEngine.GetConfirmation("Do you wish to add another template?"));
            }
        }
    }
}