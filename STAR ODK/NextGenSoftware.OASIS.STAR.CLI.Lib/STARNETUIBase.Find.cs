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
        where T1 : ISTARNETHolon, new()
        where T2 : IDownloadedSTARNETHolon, new()
        where T3 : IInstalledSTARNETHolon, new()
        where T4 : ISTARNETDNA, new()
    {
        public async Task<OASISResult<T1>> FindAsync(string operationName, string idOrName = "", Guid parentId = default, bool showOnlyForCurrentAvatar = false, bool addSpace = true, string STARNETHolonUIName = "Default", ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            Guid id = Guid.Empty;

            if (STARNETHolonUIName == "Default")
                STARNETHolonUIName = STARNETManager.STARNETHolonUIName;

            if (idOrName == Guid.Empty.ToString())
                idOrName = "";

            do
            {
                if (string.IsNullOrEmpty(idOrName))
                {
                    if (CLIEngine.NonInteractive)
                        throw new CLIEngineNonInteractiveInputRequiredException(
                            $"Non-interactive mode requires a GUID or name for this operation. Example: '<command> {operationName} <idOrName>' (entity: {STARNETHolonUIName}).");

                    bool cont = true;
                    OASISResult<IEnumerable<T1>> starHolonsResult = null;

                    if (!CLIEngine.GetConfirmation($"Do you know the GUID/ID or Name of the {STARNETHolonUIName} you wish to {operationName}? Press 'Y' for Yes or 'N' for No."))
                    {
                        Console.WriteLine("");
                        CLIEngine.ShowWorkingMessage($"Loading {STARNETHolonUIName}'s...");

                        //TODO: Add parentId to load functions below... 
                        if (showOnlyForCurrentAvatar)
                            starHolonsResult = await STARNETManager.LoadAllForAvatarAsync(STAR.BeamedInAvatar.AvatarId);
                        else
                            starHolonsResult = await STARNETManager.LoadAllAsync(STAR.BeamedInAvatar.AvatarId, null, true, false, 0, providerType: providerType);

                        ListStarHolons(starHolonsResult);

                        if (!(starHolonsResult != null && starHolonsResult.Result != null && !starHolonsResult.IsError && starHolonsResult.Result.Count() > 0))
                            cont = false;
                    }
                    else
                        Console.WriteLine("");

                    if (cont)
                        idOrName = CLIEngine.GetValidInput($"What is the GUID/ID or Name of the {STARNETHolonUIName} you wish to {operationName}?");
                    else
                    {
                        idOrName = "nonefound";
                        break;
                    }

                    if (idOrName == "exit")
                        break;
                }

                if (addSpace)
                    Console.WriteLine("");

                if (Guid.TryParse(idOrName, out id))
                {
                    CLIEngine.ShowWorkingMessage($"Loading {STARNETHolonUIName}...");
                    result = await STARNETManager.LoadAsync(STAR.BeamedInAvatar.AvatarId, id, 0, providerType: providerType);

                    if (result != null && result.Result != null && !result.IsError && showOnlyForCurrentAvatar && result.Result.STARNETDNA.CreatedByAvatarId != STAR.BeamedInAvatar.AvatarId)
                    {
                        CLIEngine.ShowErrorMessage($"You do not have permission to {operationName} this {STARNETHolonUIName}. It was created by another avatar.");
                        result.Result = default;
                    }
                }
                else
                {
                    CLIEngine.ShowWorkingMessage($"Searching {STARNETHolonUIName}s...");
                    OASISResult<IEnumerable<T1>> searchResults = await STARNETManager.SearchAsync<T1>(STAR.BeamedInAvatar.Id, idOrName, parentId, null, MetaKeyValuePairMatchMode.All, showOnlyForCurrentAvatar, false, 0, providerType);

                    if (searchResults != null && searchResults.Result != null && !searchResults.IsError)
                    {
                        if (searchResults.Result.Count() > 1)
                        {
                            if (CLIEngine.NonInteractive)
                                throw new CLIEngineNonInteractiveInputRequiredException(
                                    BuildNonInteractiveMultipleMatchDetails(searchResults.Result, idOrName, STARNETHolonUIName));

                            ListStarHolons(searchResults, true);

                            if (CLIEngine.GetConfirmation("Are any of these correct?"))
                            {
                                Console.WriteLine("");

                                do
                                {
                                    int number = CLIEngine.GetValidInputForInt($"What is the number of the {STARNETHolonUIName} you wish to {operationName}?");

                                    if (number > 0 && number <= searchResults.Result.Count())
                                        result.Result = searchResults.Result.ElementAt(number - 1);
                                    else
                                        CLIEngine.ShowErrorMessage("Invalid number entered. Please try again.");

                                } while (result.Result == null || result.IsError);
                            }
                            else
                            {
                                Console.WriteLine("");
                                idOrName = "";
                            }
                        }
                        else if (searchResults.Result.Count() == 1)
                            result.Result = searchResults.Result.FirstOrDefault();
                        else
                        {
                            idOrName = "";
                            CLIEngine.ShowWarningMessage($"No {STARNETHolonUIName} Found!");
                        }
                    }
                    else
                        CLIEngine.ShowErrorMessage($"An error occured calling STARNETManager.SearchsAsync. Reason: {searchResults.Message}");
                }

                if (result.Result != null && result.Result.STARNETDNA != null)
                {
                    await ShowAsync(result.Result);

                    if (result.Result.STARNETDNA.NumberOfVersions > 1)
                    {
                        if (!CLIEngine.NonInteractive)
                        {
                            //if (((operationName == "view" || operationName == "use") && CLIEngine.GetConfirmation($"{result.Result.STARNETDNA.NumberOfVersions} versions were found. Do you wish to view the other versions?")) ||
                            //    (!CLIEngine.GetConfirmation($"{result.Result.STARNETDNA.NumberOfVersions} versions were found. Do you wish to {operationName} the latest version ({result.Result.STARNETDNA.Version})?")))
                            if (!CLIEngine.GetConfirmation($"{result.Result.STARNETDNA.NumberOfVersions} versions were found. Do you wish to {operationName} the latest version ({result.Result.STARNETDNA.Version}) or do you wish to view all the versions? Press 'Y' for latest version or 'N' for all versions."))
                            {
                                Console.WriteLine("");
                                CLIEngine.ShowWorkingMessage($"Loading {STARNETHolonUIName} Versions...");
                                OASISResult<IEnumerable<T1>> versionsResult = await STARNETManager.LoadVersionsAsync(result.Result.STARNETDNA.Id, providerType);
                                ListStarHolons(versionsResult);

                                if (operationName != "view" && versionsResult != null && versionsResult.Result != null && !versionsResult.IsError && versionsResult.Result.Count() > 0)
                                {
                                    bool versionSelected = false;

                                    do
                                    {
                                        int version = CLIEngine.GetValidInputForInt($"Which version do you wish to {operationName}? (Enter the Version Sequence that corresponds to the relevant template)");

                                        if (version > 0 && version <= versionsResult.Result.Count())
                                        {
                                            versionSelected = true;
                                            result.Result = versionsResult.Result.ElementAt(version - 1);
                                        }
                                        else
                                            CLIEngine.ShowErrorMessage("Invalid version entered. Please try again.");

                                        if (version == 0)
                                            break;

                                    } while (!versionSelected);
                                }
                            }
                            else
                                Console.WriteLine("");
                        }

                        if (operationName != "view")
                            await ShowAsync(result.Result);
                    }
                }

                if (idOrName == "exit")
                    break;

                if (result.Result != null && operationName != "view")
                {
                    if (CLIEngine.NonInteractive || CLIEngine.GetConfirmation($"Please confirm you wish to {operationName} this {STARNETHolonUIName}?"))
                    {
                        if (operationName == "install")
                        {
                            if (result != null && result.Result != null)
                            {
                                OASISResult<T1> checkResult = await CheckIfAlreadyInstalledAsync(result.Result, providerType);

                                if (checkResult != null && checkResult.Result != null && !checkResult.IsError)
                                {
                                    if (result.MetaData != null && result.MetaData.ContainsKey("Reinstall"))
                                        result.MetaData["Reinstall"] = checkResult.MetaData["Reinstall"];
                                }
                                else if (checkResult.IsError)
                                    result.Result = default;
                            }
                            else
                            {
                                CLIEngine.ShowErrorMessage($"Error occured checking if the {STARNETHolonUIName} is already installed! Reason: Id was not found in the metadata!");
                                result.Result = default;
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine("");
                        result.Result = default;
                        idOrName = "";

                        if (!CLIEngine.GetConfirmation($"Do you wish to search for another {STARNETHolonUIName}?"))
                        {
                            idOrName = "exit";
                            break;
                        }
                    }

                    Console.WriteLine("");
                }

                idOrName = "";
            }
            while (result.Result == null || result.IsError);

            if (idOrName == "exit")
            {
                result.IsError = true;
                result.Message = "User Exited";
            }
            else if (idOrName == "nonefound")
            {
                result.IsError = true;
                result.Message = "None Found";
            }

            return result;
        }

        public OASISResult<T1> Find(string operationName, string idOrName = "", bool showOnlyForCurrentAvatar = true, bool addSpace = true, string STARNETHolonUIName = "Default", ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            Guid id = Guid.Empty;
            bool reInstall = false;

            do
            {
                if (string.IsNullOrEmpty(idOrName))
                {
                    if (CLIEngine.NonInteractive)
                        throw new CLIEngineNonInteractiveInputRequiredException(
                            $"Non-interactive mode requires a GUID or name for this operation. Example: '<command> {operationName} <idOrName>' (entity: {STARNETManager.STARNETHolonUIName}).");

                    if (!CLIEngine.GetConfirmation($"Do you know the GUID/ID or Name of the {STARNETManager.STARNETHolonUIName} you wish to {operationName}? Press 'Y' for Yes or 'N' for No."))
                    {
                        Console.WriteLine("");
                        CLIEngine.ShowWorkingMessage($"Loading {STARNETManager.STARNETHolonUIName}s...");

                        if (showOnlyForCurrentAvatar)
                            ListStarHolons(STARNETManager.LoadAllForAvatar(STAR.BeamedInAvatar.AvatarId));
                        else
                            ListStarHolons(STARNETManager.LoadAll(STAR.BeamedInAvatar.AvatarId, null, true, false, 0, providerType: providerType));
                    }
                    else
                        Console.WriteLine("");

                    idOrName = CLIEngine.GetValidInput($"What is the GUID/ID or Name of the {STARNETManager.STARNETHolonUIName} you wish to {operationName}?");

                    if (idOrName == "exit")
                        break;
                }

                if (addSpace)
                    Console.WriteLine("");

                if (Guid.TryParse(idOrName, out id))
                {
                    CLIEngine.ShowWorkingMessage($"Loading {STARNETManager.STARNETHolonUIName}...");
                    result = STARNETManager.Load(STAR.BeamedInAvatar.Id, id, 0, providerType: providerType);

                    if (result != null && result.Result != null && !result.IsError && showOnlyForCurrentAvatar && result.Result.STARNETDNA.CreatedByAvatarId != STAR.BeamedInAvatar.AvatarId)
                    {
                        CLIEngine.ShowErrorMessage($"You do not have permission to {operationName} this {STARNETManager.STARNETHolonUIName}. It was created by another avatar.");
                        result.Result = default;
                    }
                }
                else
                {
                    CLIEngine.ShowWorkingMessage($"Searching {STARNETManager.STARNETHolonUIName}'s...");
                    OASISResult<IEnumerable<T1>> searchResults = STARNETManager.Search(STAR.BeamedInAvatar.Id, idOrName, default, null, MetaKeyValuePairMatchMode.All, showOnlyForCurrentAvatar, false, 0, providerType);

                    if (searchResults != null && searchResults.Result != null && !searchResults.IsError)
                    {
                        if (searchResults.Result.Count() > 1)
                        {
                            if (CLIEngine.NonInteractive)
                                throw new CLIEngineNonInteractiveInputRequiredException(
                                    BuildNonInteractiveMultipleMatchDetails(searchResults.Result, idOrName, STARNETManager.STARNETHolonUIName));

                            ListStarHolons(searchResults, true);

                            do
                            {
                                int number = CLIEngine.GetValidInputForInt($"What is the number of the {STARNETManager.STARNETHolonUIName} you wish to {operationName}?");

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
                            CLIEngine.ShowWarningMessage($"No {STARNETManager.STARNETHolonUIName} Found!");
                        }
                    }
                    else
                        CLIEngine.ShowErrorMessage($"An error occured calling STARNETManager.SearchsAsync. Reason: {searchResults.Message}");
                }

                if (result.Result != null && result.Result.STARNETDNA != null)
                {
                    ShowAsync(result.Result);

                    if (result.Result.STARNETDNA.NumberOfVersions > 1)
                    {
                        if (!CLIEngine.NonInteractive)
                        {
                            if ((operationName == "view" && CLIEngine.GetConfirmation($"{result.Result.STARNETDNA.NumberOfVersions} versions were found. Do you wish to view the other versions?")) ||
                                (!CLIEngine.GetConfirmation($"{result.Result.STARNETDNA.NumberOfVersions} versions were found. Do you wish to {operationName} the latest version ({result.Result.STARNETDNA.Version})?")))
                            {
                                Console.WriteLine("");
                                CLIEngine.ShowWorkingMessage($"Loading {STARNETManager.STARNETHolonUIName} Versions...");
                                OASISResult<IEnumerable<T1>> versionsResult = STARNETManager.LoadVersions(result.Result.STARNETDNA.Id, providerType);
                                ListStarHolons(versionsResult);

                                if (operationName != "view" && versionsResult != null && versionsResult.Result != null && !versionsResult.IsError && versionsResult.Result.Count() > 0)
                                {
                                    bool versionSelected = false;

                                    do
                                    {
                                        int version = CLIEngine.GetValidInputForInt($"Which version do you wish to {operationName}? (Enter the Version Sequence that corresponds to the relevant template)");

                                        if (version > 0 && version <= versionsResult.Result.Count())
                                        {
                                            versionSelected = true;
                                            result.Result = versionsResult.Result.ElementAt(version - 1);
                                        }
                                        else
                                            CLIEngine.ShowErrorMessage("Invalid version entered. Please try again.");

                                        if (version == 0)
                                            break;

                                    } while (!versionSelected);
                                }
                            }
                            else
                                Console.WriteLine("");
                        }

                        if (operationName != "view")
                            ShowAsync(result.Result);
                    }
                }

                if (idOrName == "exit")
                    break;

                if (result.Result != null && operationName != "view")
                {
                    if (CLIEngine.NonInteractive || CLIEngine.GetConfirmation($"Please confirm you wish to {operationName} this {STARNETManager.STARNETHolonUIName}?"))
                    {
                        if (operationName == "install")
                        {
                            if (result != null && result.Result != null)
                            {
                                OASISResult<T1> checkResult = CheckIfAlreadyInstalled(result.Result, providerType);

                                if (checkResult != null && checkResult.Result != null && !checkResult.IsError)
                                {
                                    if (result.MetaData != null && result.MetaData.ContainsKey("Reinstall"))
                                        result.MetaData["Reinstall"] = checkResult.MetaData["Reinstall"];
                                }
                                else if (checkResult.IsError)
                                    result.Result = default;
                            }
                            else
                            {
                                CLIEngine.ShowErrorMessage($"Error occured checking if the {STARNETManager.STARNETHolonUIName} is already installed! Reason: Id was not found in the metadata!");
                                result.Result = default;
                            }
                        }

                    }
                    else
                    {
                        if (CLIEngine.GetConfirmation($"Do you wish to search for another {STARNETManager.STARNETHolonUIName}?"))
                            result.Result = default;
                        else
                            break;
                    }

                    Console.WriteLine("");
                }

            }
            while (result.Result == null || result.IsError);

            if (idOrName == "exit")
            {
                result.IsError = true;
                result.Message = "User Exited";
            }

            return result;
        }

        private async Task<OASISResult<T1>> FindForProviderAsync(string operationName, string idOrName = "", bool showOnlyForCurrentAvatar = true, bool addSpace = true, bool simpleWizard = true, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            ProviderType largeFileProviderType = ProviderType.IPFSOASIS;

            if (!simpleWizard)
            {
                if (!CLIEngine.GetConfirmation("Do you wish to download from the cloud or from the OASIS? Press 'Y' for the cloud or N' for the OASIS."))
                {
                    Console.WriteLine("");
                    object largeProviderTypeObject = CLIEngine.GetValidInputForEnum($"What OASIS provider do you wish to install the {STARNETManager.STARNETHolonUIName} from? (The default is IPFSOASIS)", typeof(ProviderType));

                    if (largeProviderTypeObject != null)
                    {
                        largeFileProviderType = (ProviderType)largeProviderTypeObject;
                        result = await FindAsync(operationName, idOrName, default, showOnlyForCurrentAvatar, addSpace, providerType: largeFileProviderType);
                    }
                    else
                        OASISErrorHandling.HandleError(ref result, "Error occured in FindForProviderAsync, reason: largeProviderTypeObject is null!");
                }
                else
                {
                    Console.WriteLine("");
                    result = await FindAsync(operationName, idOrName, default, showOnlyForCurrentAvatar, addSpace, providerType: providerType);
                }
            }
            else
                result = await FindAsync(operationName, idOrName, default, showOnlyForCurrentAvatar, addSpace, providerType: providerType);

            return result;
        }

        private OASISResult<T1> FindForProvider(string operationName, string idOrName = "", bool showOnlyForCurrentAvatar = true, bool addSpace = true, bool simpleWizard = true, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            ProviderType largeFileProviderType = ProviderType.IPFSOASIS;

            if (!simpleWizard)
            {
                if (!CLIEngine.GetConfirmation("Do you wish to download from the cloud or from the OASIS? Press 'Y' for the cloud or N' for the OASIS."))
                {
                    Console.WriteLine("");
                    object largeProviderTypeObject = CLIEngine.GetValidInputForEnum($"What OASIS provider do you wish to install the {STARNETManager.STARNETHolonUIName} from? (The default is IPFSOASIS)", typeof(ProviderType));

                    if (largeProviderTypeObject != null)
                    {
                        largeFileProviderType = (ProviderType)largeProviderTypeObject;
                        result = Find(operationName, idOrName, showOnlyForCurrentAvatar, addSpace, providerType: largeFileProviderType);
                    }
                    else
                        OASISErrorHandling.HandleError(ref result, "Error occured in FindForProvider, reason: largeProviderTypeObject is null!");
                }
                else
                {
                    Console.WriteLine("");
                    result = Find(operationName, idOrName, showOnlyForCurrentAvatar, addSpace, providerType: largeFileProviderType);
                }
            }
            else
                result = Find(operationName, idOrName, showOnlyForCurrentAvatar, addSpace, providerType: largeFileProviderType);

            return result;
        }


        //public async Task<OASISResult<T3>> FindForProviderAndInstallIfNotInstalledAsync(string operationName, string idOrName = "", bool showOnlyForCurrentAvatar = true, string STARNETHolonUIName = "", ProviderType providerType = ProviderType.Default)
        public async Task<OASISResult<T3>> FindForProviderAndInstallIfNotInstalledAsync(string operationName, string idOrName = "", bool showOnlyForCurrentAvatar = true, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T3> result = new OASISResult<T3>();
            OASISResult<T1> findResult = await FindForProviderAsync(operationName, idOrName, showOnlyForCurrentAvatar, providerType: providerType);

            if (findResult != null && findResult.Result != null && !findResult.IsError)
            {
                //OASISResult<bool> installedResult = await STARNETManager.IsInstalledAsync(STAR.BeamedInAvatar.Id, findResult.Result.STARNETDNA.Id, findResult.Result.STARNETDNA.VersionSequence, providerType);
                OASISResult<bool> installedResult = await STARNETManager.IsInstalledAsync(STAR.BeamedInAvatar.Id, findResult.Result.STARNETDNA.Id, findResult.Result.STARNETDNA.Version, providerType);

                if (installedResult != null && !installedResult.IsError)
                {
                    if (!installedResult.Result)
                    {
                        if (CLIEngine.GetConfirmation($"The selected {STARNETManager.STARNETHolonUIName} is not currently installed. Do you wish to install it now?"))
                        {
                            result = await DownloadAndInstallAsync(findResult.Result.STARNETDNA.Id.ToString(), InstallMode.DownloadAndInstall, providerType);

                            if (!(result != null && result.Result != null && !result.IsError))
                                OASISErrorHandling.HandleError(ref result, $"Error occured installing the {STARNETManager.STARNETHolonUIName}. Reason: {result.Message}");
                        }
                        else
                        {
                            Console.WriteLine("");
                            result.Message = "User Declined Installation";
                            result.IsError = true;
                        }
                    }
                    else
                    {
                        result = await STARNETManager.LoadInstalledAsync(STAR.BeamedInAvatar.Id, findResult.Result.STARNETDNA.Id, findResult.Result.STARNETDNA.VersionSequence, providerType);

                        if (!(result != null && result.Result != null && !result.IsError))
                            OASISErrorHandling.HandleError(ref result, $"Error occured loading the {STARNETManager.STARNETHolonUIName}. Reason: {result.Message}");
                    }
                }
                else
                    CLIEngine.ShowErrorMessage($"Error occured checking if {STARNETManager.STARNETHolonUIName} is installed. Reason: {installedResult.Message}");
            }
            else
            {
                Console.WriteLine("");
                CLIEngine.ShowErrorMessage($"Error occured finding {STARNETManager.STARNETHolonUIName}. Reason: {findResult.Message}");
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(findResult, result);
            }

            return result;
        }

        /// <summary>
        /// Clones a STARNET holon via <see cref="ISTARNETManagerBase{T1,T2,T3,T4}.CloneAsync"/>.
        /// <paramref name="options"/> may be a source id or name string (non-interactive argv); when null/empty in interactive mode, <see cref="FindAsync"/> prompts.
        /// </summary>
        public async Task<OASISResult<T1>> CloneAsync(object options = null)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            string idOrName = options != null ? options.ToString()?.Trim() : null;
            if (string.IsNullOrEmpty(idOrName))
                idOrName = "";

            OASISResult<T1> findResult = await FindAsync("clone", idOrName, default, true, providerType: ProviderType.Default);
            if (findResult == null || findResult.IsError || findResult.Result == null)
            {
                OASISErrorHandling.HandleError(ref result, findResult?.Message ?? $"Could not find {STARNETManager.STARNETHolonUIName} to clone.");
                return result;
            }

            T1 source = findResult.Result;
            if (source.STARNETDNA == null || source.STARNETDNA.Id == Guid.Empty)
            {
                OASISErrorHandling.HandleError(ref result, "Source holon has no STARNET DNA id; cannot clone.");
                return result;
            }

            string newName = null;
            if (!CLIEngine.NonInteractive)
            {
                if (CLIEngine.GetConfirmation($"Do you wish to give the clone a custom name? (No = append \" - Clone\" to '{source.STARNETDNA.Name}')"))
                    newName = CLIEngine.GetValidInput($"New name for the cloned {STARNETManager.STARNETHolonUIName}:");
            }

            OASISResult<T1> cloneResult = await STARNETManager.CloneAsync(STAR.BeamedInAvatar.Id, source.STARNETDNA.Id, newName, ProviderType.Default);
            if (cloneResult != null && !cloneResult.IsError && cloneResult.Result != null)
            {
                if (!CLIEngine.JsonOutput)
                    CLIEngine.ShowSuccessMessage(cloneResult.Message ?? "Clone completed.");
                // JSON / non-interactive: avoid dumping the full holon UI to stdout (use follow-up show <id> if needed).
                if (!CLIEngine.NonInteractive && !CLIEngine.JsonOutput)
                    await ShowAsync(cloneResult.Result);
                return cloneResult;
            }

            if (cloneResult != null && !string.IsNullOrEmpty(cloneResult.Message))
                OASISErrorHandling.HandleError(ref result, cloneResult.Message);
            else
                OASISErrorHandling.HandleError(ref result, "Clone failed.");
            return result;
        }

        //TODO: Finish implementing later!
        //public OASISResult<T3> FindForProviderAndInstallIfNotInstalled(string operationName, string idOrName = "", bool showOnlyForCurrentAvatar = true, string STARNETHolonUIName = "", ProviderType providerType = ProviderType.Default)
        //{
        //    OASISResult<T3> result = new OASISResult<T3>();
        //    OASISResult<T1> downloadedCelestialBodyDNA = STARCLI.CelestialBodiesMetaDataDNA.FindForProvider(operationName, idOrName, showOnlyForCurrentAvatar, STARNETHolonUIName: STARNETHolonUIName, providerType: providerType);

        //    if (downloadedCelestialBodyDNA != null && downloadedCelestialBodyDNA.Result != null && !downloadedCelestialBodyDNA.IsError)
        //    {
        //        OASISResult<bool> celestialBodyDNAInstalledResult = STAR.STARAPI.CelestialBodiesMetaDataDNA.IsInstalled(STAR.BeamedInAvatar.Id, downloadedCelestialBodyDNA.Result.STARNETDNA.Id, downloadedCelestialBodyDNA.Result.STARNETDNA.VersionSequence, providerType);

        //        if (celestialBodyDNAInstalledResult != null && !celestialBodyDNAInstalledResult.IsError)
        //        {
        //            if (!celestialBodyDNAInstalledResult.Result)
        //            {
        //                if (CLIEngine.GetConfirmation($"The selected {STARNETHolonUIName} is not currently installed. Do you wish to install it now?"))
        //                {
        //                    OASISResult<T3> installResult = DownloadAndInstall(downloadedCelestialBodyDNA.Result.STARNETDNA.Id.ToString(), InstallMode.DownloadAndInstall, providerType);

        //                    if (installResult.Result != null && !installResult.IsError)
        //                        result = installResult;
        //                    else
        //                        OASISErrorHandling.HandleError(ref result, $"Error occured installing the {STARNETHolonUIName}. Reason: {installResult.Message}");
        //                }
        //            }
        //            else
        //            {
        //                OASISResult<T3> loadResult = STARNETManager.LoadInstalled(STAR.BeamedInAvatar.Id, downloadedCelestialBodyDNA.Result.STARNETDNA.Id, downloadedCelestialBodyDNA.Result.STARNETDNA.VersionSequence, providerType);

        //                if (loadResult != null && loadResult.Result != null && !loadResult.IsError)
        //                    result = loadResult;
        //                else
        //                    OASISErrorHandling.HandleError(ref result, $"Error occured loading the {STARNETHolonUIName}. Reason: {loadResult.Message}");
        //            }
        //        }
        //        else
        //            CLIEngine.ShowErrorMessage($"Error occured checking if {STARNETHolonUIName} is installed. Reason: {celestialBodyDNAInstalledResult.Message}");
        //    }
        //    else
        //        CLIEngine.ShowErrorMessage($"Error occured finding {STARNETHolonUIName}. Reason: {downloadedCelestialBodyDNA.Message}");

        //    return result;
        //}


        //private async Task<OASISResult<T1>> FindForProviderAndInstallAsync(string operationName, string downloadPath, string installPath, string idOrName = "", bool showOnlyForCurrentAvatar = true, bool addSpace = true, bool simpleWizard = true, InstallMode installMode = InstallMode.DownloadAndInstall, ProviderType providerType = ProviderType.Default)
        //{
        //    OASISResult<T1> result = new OASISResult<T1>();
        //    ProviderType largeFileProviderType = ProviderType.IPFSOASIS;


        //    //OASISResult<T1> result = await FindForProviderAsync(operation, idOrName, false, false, true, providerType);

        //    //if (result != null && result.Result != null && !result.IsError)
        //    //{
        //    //    if (result.MetaData != null && result.MetaData.ContainsKey("Reinstall") && !string.IsNullOrEmpty(result.MetaData["Reinstall"]) && result.MetaData["Reinstall"] == "1" && installMode == InstallMode.DownloadAndInstall)
        //    //        installMode = InstallMode.DownloadAndReInstall;

        //    //    installResult = await CheckIfInstalledAndInstallAsync(result.Result, downloadPath, installPath, installMode, "", providerType);
        //    //}

        //    OASISResult<T1> templateResult = await FindForProviderAsync(operationName, idOrName, showOnlyForCurrentAvatar,addSpace, simpleWizard, providerType: providerType);

        //    if (templateResult != null && templateResult.Result != null && !templateResult.IsError)
        //    {
        //        if (result.MetaData != null && result.MetaData.ContainsKey("Reinstall") && !string.IsNullOrEmpty(result.MetaData["Reinstall"]) && result.MetaData["Reinstall"] == "1" && installMode == InstallMode.DownloadAndInstall)
        //            installMode = InstallMode.DownloadAndReInstall;

        //        DownloadAndInstallAsync(idOrName, downloadPath, installPath, templateResult.Result, installMode, providerType);

        //        //OASISResult<bool> oappTemplateInstalledResult = await CheckIfInstalledAndInstallAsync(templateResult.Result, downloadPath, installPath, installMode, )

        //        //if (oappTemplateInstalledResult != null && !oappTemplateInstalledResult.IsError)
        //        //{
        //        //    if (!oappTemplateInstalledResult.Result)
        //        //    {
        //        //        if (CLIEngine.GetConfirmation($"The selected OAPP Template is not currently installed. Do you wish to install it now?"))
        //        //        {
        //        //            OASISResult<InstalledOAPPTemplate> installResult = await STARCLI.OAPPTemplates.DownloadAndInstallAsync(templateResult.Result.STARNETDNA.Id.ToString(), InstallMode.DownloadAndInstall, providerType);

        //        //            if (installResult.Result != null && !installResult.IsError)
        //        //            {
        //        //                templateInstalled = true;
        //        //                OAPPTemplate = installResult.Result;
        //        //            }
        //        //        }
        //        //    }
        //        //    else
        //        //    {
        //        //        templateInstalled = true;
        //        //        OAPPTemplate = templateResult.Result;
        //        //    }
        //        //}
        //        //else
        //        //    CLIEngine.ShowErrorMessage($"Error occured checking if OAPP Template is installed. Reason: {oappTemplateInstalledResult.Message}");
        //    }
        //    else
        //        CLIEngine.ShowErrorMessage($"Error occured finding OAPP Template. Reason: {templateResult.Message}");


        //    return result;
        //}


        public async Task<OASISResult<T3>> FindAndInstallIfNotInstalledAsync(string operationName, string idOrName = "", bool showOnlyForCurrentAvatar = true, string STARNETHolonUIName = "Default", ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T3> result = new OASISResult<T3>();

            if (STARNETHolonUIName == "Default")
                STARNETHolonUIName = STARNETManager.STARNETHolonUIName;

            OASISResult<T1> findResult = await FindAsync(operationName, idOrName, default, showOnlyForCurrentAvatar, STARNETHolonUIName: STARNETHolonUIName, providerType: providerType);

            if (findResult != null && findResult.Result != null && !findResult.IsError)
            {
                OASISResult<bool> celestialBodyDNAInstalledResult = await STARNETManager.IsInstalledAsync(STAR.BeamedInAvatar.Id, findResult.Result.STARNETDNA.Id, findResult.Result.STARNETDNA.VersionSequence, providerType);

                if (celestialBodyDNAInstalledResult != null && !celestialBodyDNAInstalledResult.IsError)
                {
                    if (!celestialBodyDNAInstalledResult.Result)
                    {
                        if (CLIEngine.GetConfirmation($"The selected {STARNETHolonUIName} is not currently installed. Do you wish to install it now?"))
                        {
                            OASISResult<T3> installResult = await DownloadAndInstallAsync(findResult.Result.STARNETDNA.Id.ToString(), InstallMode.DownloadAndInstall, providerType);

                            if (installResult.Result != null && !installResult.IsError)
                                result = installResult;
                            else
                                OASISErrorHandling.HandleError(ref result, $"Error occured installing the {STARNETHolonUIName}. Reason: {installResult.Message}");
                        }
                    }
                    else
                    {
                        OASISResult<T3> loadResult = await STARNETManager.LoadInstalledAsync(STAR.BeamedInAvatar.Id, findResult.Result.STARNETDNA.Id, findResult.Result.STARNETDNA.VersionSequence, providerType);

                        if (loadResult != null && loadResult.Result != null && !loadResult.IsError)
                            result = loadResult;
                        else
                            OASISErrorHandling.HandleError(ref result, $"Error occured loading the {STARNETHolonUIName}. Reason: {loadResult.Message}");
                    }
                }
                else
                    OASISErrorHandling.HandleError(ref result, ($"Error occured checking if {STARNETHolonUIName} is installed. Reason: {celestialBodyDNAInstalledResult.Message}"));
            }
            else
                OASISErrorHandling.HandleError(ref result, ($"Error occured finding {STARNETHolonUIName}. Reason: {findResult.Message}"));

            return result;
        }

        //TODO: Finish implementing later!
        //public OASISResult<T3> FindAndInstallIfNotInstalled(string operationName, string idOrName = "", bool showOnlyForCurrentAvatar = true, string STARNETHolonUIName = "", ProviderType providerType = ProviderType.Default)
        //{
        //    OASISResult<T3> result = new OASISResult<T3>();
        //    OASISResult<T1> downloadedCelestialBodyDNA = STARCLI.CelestialBodiesMetaDataDNA.Find<T1>(operationName, idOrName, showOnlyForCurrentAvatar, STARNETHolonUIName: STARNETHolonUIName, providerType: providerType);

        //    if (downloadedCelestialBodyDNA != null && downloadedCelestialBodyDNA.Result != null && !downloadedCelestialBodyDNA.IsError)
        //    {
        //        OASISResult<bool> celestialBodyDNAInstalledResult = STAR.STARAPI.CelestialBodiesMetaDataDNA.IsInstalled(STAR.BeamedInAvatar.Id, downloadedCelestialBodyDNA.Result.STARNETDNA.Id, downloadedCelestialBodyDNA.Result.STARNETDNA.VersionSequence, providerType);

        //        if (celestialBodyDNAInstalledResult != null && !celestialBodyDNAInstalledResult.IsError)
        //        {
        //            if (!celestialBodyDNAInstalledResult.Result)
        //            {
        //                if (CLIEngine.GetConfirmation($"The selected {STARNETHolonUIName} is not currently installed. Do you wish to install it now?"))
        //                {
        //                    OASISResult<T3> installResult = DownloadAndInstall(downloadedCelestialBodyDNA.Result.STARNETDNA.Id.ToString(), InstallMode.DownloadAndInstall, providerType);

        //                    if (installResult.Result != null && !installResult.IsError)
        //                        result = installResult;
        //                    else
        //                        OASISErrorHandling.HandleError(ref result, $"Error occured installing the {STARNETHolonUIName}. Reason: {installResult.Message}");
        //                }
        //            }
        //            else
        //            {
        //                OASISResult<T3> loadResult = STARNETManager.LoadInstalled(STAR.BeamedInAvatar.Id, downloadedCelestialBodyDNA.Result.STARNETDNA.Id, downloadedCelestialBodyDNA.Result.STARNETDNA.VersionSequence, providerType);

        //                if (loadResult != null && loadResult.Result != null && !loadResult.IsError)
        //                    result = loadResult;
        //                else
        //                    OASISErrorHandling.HandleError(ref result, $"Error occured loading the {STARNETHolonUIName}. Reason: {loadResult.Message}");
        //            }
        //        }
        //        else
        //            CLIEngine.ShowErrorMessage($"Error occured checking if {STARNETHolonUIName} is installed. Reason: {celestialBodyDNAInstalledResult.Message}");
        //    }
        //    else
        //        CLIEngine.ShowErrorMessage($"Error occured finding {STARNETHolonUIName}. Reason: {downloadedCelestialBodyDNA.Message}");

        //    return result;
        //}


    }
}