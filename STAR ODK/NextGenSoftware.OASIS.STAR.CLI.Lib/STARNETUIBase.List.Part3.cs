using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Drawing.Text;
using System.Linq;
using ADRaffy.ENSNormalize;
using NextGenSoftware.CLI.Engine;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.ONODE.Core.Enums.STARNETHolon;
using NextGenSoftware.OASIS.API.ONODE.Core.Events.STARNETHolon;
using NextGenSoftware.OASIS.API.ONODE.Core.Holons;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Holons;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Managers;
using NextGenSoftware.OASIS.API.ONODE.Core.Managers;
using NextGenSoftware.OASIS.API.ONODE.Core.Objects;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.STAR.CelestialSpace;
using NextGenSoftware.OASIS.STAR.CLI.Lib.Enums;
using NextGenSoftware.OASIS.STAR.CLI.Lib.Objects;
using Org.BouncyCastle.Utilities;

namespace NextGenSoftware.OASIS.STAR.CLI.Lib
{
    public partial class STARNETUIBase<T1, T2, T3, T4>
    {

        public virtual async Task UninstallAsync(string idOrName = "", ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = await FindAsync("uninstall", idOrName, default, true, providerType: providerType);

            if (result != null && !result.IsError && result.Result != null)
            {
                OASISResult<T3> uninstallResult = await STARNETManager.UninstallAsync(STAR.BeamedInAvatar.Id, result.Result.Id, result.Result.STARNETDNA.Version, providerType);

                if (uninstallResult != null)
                {
                    if (!uninstallResult.IsError && uninstallResult.Result != null)
                    {
                        CLIEngine.ShowSuccessMessage($"{STARNETManager.STARNETHolonUIName} Successfully Uninstalled.");
                        await ShowAsync(result.Result);
                    }
                    else
                        CLIEngine.ShowErrorMessage($"Error installing {STARNETManager.STARNETHolonUIName}. Reason: {uninstallResult.Message}");
                }
                else
                    CLIEngine.ShowErrorMessage($"Error uninstalling {STARNETManager.STARNETHolonUIName}. Reason: Unknown error occured!");
            }
            else
                CLIEngine.ShowErrorMessage($"An error occured loading the {STARNETManager.STARNETHolonUIName}. Reason: {result.Message}");
        }

        public virtual void Uninstall(string idOrName = "", ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = Find("uninstall", idOrName, true, providerType: providerType);

            if (result != null && !result.IsError && result.Result != null)
            {
                OASISResult<T3> uninstallResult = STARNETManager.Uninstall(STAR.BeamedInAvatar.Id, result.Result.Id, result.Result.STARNETDNA.Version, providerType);

                if (uninstallResult != null)
                {
                    if (!uninstallResult.IsError && uninstallResult.Result != null)
                    {
                        CLIEngine.ShowSuccessMessage($"{STARNETManager.STARNETHolonUIName} Successfully Uninstalled.");
                        ShowAsync(result.Result);
                    }
                    else
                        CLIEngine.ShowErrorMessage($"Error installing {STARNETManager.STARNETHolonUIName}. Reason: {uninstallResult.Message}");
                }
                else
                    CLIEngine.ShowErrorMessage($"Error uninstalling {STARNETManager.STARNETHolonUIName}. Reason: Unknown error occured!");
            }
            else
                CLIEngine.ShowErrorMessage($"An error occured loading the {STARNETManager.STARNETHolonUIName}. Reason: {result.Message}");
        }

        public virtual async Task<OASISResult<IEnumerable<T1>>> ListAllAsync(bool showAllVersions = false, bool showDetailedInfo = false, int version = 0, ProviderType providerType = ProviderType.Default)
        {
            Console.WriteLine("");
            CLIEngine.ShowWorkingMessage($"Loading {STARNETManager.STARNETHolonUIName}'s...");
            return ListStarHolons(await STARNETManager.LoadAllAsync(STAR.BeamedInAvatar.Id, null, true, showAllVersions, version, providerType: providerType), showDetailedInfo: showDetailedInfo);
        }

        public virtual OASISResult<IEnumerable<T1>> ListAll(bool showAllVersions = false, bool showDetailedInfo = false, int version = 0, ProviderType providerType = ProviderType.Default)
        {
            Console.WriteLine("");
            CLIEngine.ShowWorkingMessage($"Loading {STARNETManager.STARNETHolonUIName}'s...");
            return ListStarHolons(STARNETManager.LoadAll(STAR.BeamedInAvatar.Id, null, true, showAllVersions, version, providerType: providerType), showDetailedInfo: showDetailedInfo);
        }

        public virtual async Task ListAllCreatedByBeamedInAvatarAsync(bool showAllVersions = false, bool showDetailedInfo = false, ProviderType providerType = ProviderType.Default)
        {
            if (STAR.BeamedInAvatar != null)
            {
                Console.WriteLine("");
                CLIEngine.ShowWorkingMessage($"Loading {STARNETManager.STARNETHolonUIName}'s...");
                ListStarHolons(await STARNETManager.LoadAllForAvatarAsync(STAR.BeamedInAvatar.AvatarId), showDetailedInfo: showDetailedInfo);
            }
            else
                CLIEngine.ShowErrorMessage("No Avatar Is Beamed In. Please Beam In First!");
        }

        public virtual async Task<OASISResult<IEnumerable<T3>>> ListAllInstalledForBeamedInAvatarAsync(ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<T3>> result = new OASISResult<IEnumerable<T3>>();

            if (STAR.BeamedInAvatar != null)
            {
                Console.WriteLine("");
                CLIEngine.ShowWorkingMessage($"Loading Installed {STARNETManager.STARNETHolonUIName}'s...");
                result = await STARNETManager.ListInstalledAsync(STAR.BeamedInAvatar.AvatarId);
                ListStarHolonsInstalled(result);
            }
            else
                OASISErrorHandling.HandleError(ref result, "No Avatar Is Beamed In. Please Beam In First!");

            return result;
        }

        public virtual async Task<OASISResult<IEnumerable<T3>>> ListAllUninstalledForBeamedInAvatarAsync(ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<T3>> result = new OASISResult<IEnumerable<T3>>();

            if (STAR.BeamedInAvatar != null)
            {
                Console.WriteLine("");
                CLIEngine.ShowWorkingMessage($"Loading Uninstalled {STARNETManager.STARNETHolonUIName}s...");
                result = await STARNETManager.ListUninstalledAsync(STAR.BeamedInAvatar.AvatarId);
                ListStarHolonsInstalled(result, true, true);

                if (result != null && !result.IsError && result.Result != null && result.Result.Count() > 0 && CLIEngine.GetConfirmation("Would you like to re-install any of the above?"))
                {
                    int number = 0;

                    do
                    {
                        Console.WriteLine("");
                        number = CLIEngine.GetValidInputForInt("What number do you wish to re-install? (It will be downloaded and installed to the previous paths)");

                        if (number < 0 || number > result.Result.Count())
                            CLIEngine.ShowErrorMessage($"Invalid number, it needs to be between 1 and {result.Result.Count()}");
                    }
                    while (number < 0 || number > result.Result.Count());

                    if (number > 0)
                    {
                        T3 template = result.Result.ElementAt(number - 1);

                        if (template != null)
                        {
                            OASISResult<T3> installResult = await DownloadAndInstallAsync(template.STARNETDNA.Id.ToString(), InstallMode.DownloadAndReInstall, providerType);

                            if (installResult != null && !installResult.IsError && installResult.Result != null)
                            {
                                ShowInstalled(installResult.Result);
                                CLIEngine.ShowSuccessMessage($"{STARNETManager.STARNETHolonUIName} Successfully Re-Installed");
                            }
                            else
                                CLIEngine.ShowErrorMessage($"An error occured re-installing the {STARNETManager.STARNETHolonUIName}. Reason: {installResult.Message}");
                        }
                        else
                            CLIEngine.ShowErrorMessage($"An error occured re-installing the {STARNETManager.STARNETHolonUIName}. Reason: {STARNETManager.STARNETHolonIdName} not found in the metadata!");
                    }
                }
                else
                    Console.WriteLine("");
            }
            else
                OASISErrorHandling.HandleError(ref result, "No Avatar Is Beamed In. Please Beam In First!");

            return result;
        }

        public virtual async Task<OASISResult<IEnumerable<T1>>> ListAllUnpublishedForBeamedInAvatarAsync(ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<T1>> result = new OASISResult<IEnumerable<T1>>();

            if (STAR.BeamedInAvatar != null)
            {
                Console.WriteLine("");
                CLIEngine.ShowWorkingMessage($"Loading Unpublished {STARNETManager.STARNETHolonUIName}'s...");
                result = await STARNETManager.ListUnpublishedAsync(STAR.BeamedInAvatar.AvatarId);
                ListStarHolons(result, true);

                if (result != null && !result.IsError && result.Result != null && result.Result.Count() > 0 && CLIEngine.GetConfirmation("Would you like to republish any of the above?"))
                {
                    int number = 0;

                    do
                    {
                        Console.WriteLine("");
                        number = CLIEngine.GetValidInputForInt("What number do you wish to republish?");

                        if (number < 0 || number > result.Result.Count())
                            CLIEngine.ShowErrorMessage($"Invalid number, it needs to be between 1 and {result.Result.Count()}");
                    }
                    while (number < 0 || number > result.Result.Count());

                    if (number > 0)
                    {
                        T1 template = result.Result.ElementAt(number - 1);
                        Guid id = Guid.Empty;

                        if (template != null)
                        {
                            OASISResult<T1> republishResult = await STARNETManager.RepublishAsync(STAR.BeamedInAvatar.Id, template, providerType);

                            if (republishResult != null && !republishResult.IsError && republishResult.Result != null)
                            {
                                await ShowAsync(republishResult.Result);
                                CLIEngine.ShowSuccessMessage($"{STARNETManager.STARNETHolonUIName} Successfully Republished");
                            }
                            else
                                CLIEngine.ShowErrorMessage($"An error occured republishing the {STARNETManager.STARNETHolonUIName}. Reason: {republishResult.Message}");
                        }
                    }
                }
                else
                    Console.WriteLine("");
            }
            else
                OASISErrorHandling.HandleError(ref result, "No Avatar Is Beamed In. Please Beam In First!");

            return result;
        }

    }
}
