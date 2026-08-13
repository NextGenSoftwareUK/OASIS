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

        /// <summary>Non-interactive find by name matched multiple holons; list GUID — name rows (all by default; cap with CLIEngine.MaxHolonSearchResults / --search-limit).</summary>
        private string BuildNonInteractiveMultipleMatchDetails(IEnumerable<T1> matches, string idOrName, string holonUiName)
        {
            if (matches == null)
                return $"Multiple matches for '{idOrName}' ({holonUiName}). Use a GUID in non-interactive mode.";

            IList<T1> list = matches as IList<T1> ?? matches.ToList();
            int total = list.Count;
            int maxList = CLIEngine.MaxHolonSearchResults > 0 ? CLIEngine.MaxHolonSearchResults : total;
            var rows = new List<string>(Math.Min(maxList, total));
            for (int i = 0; i < list.Count && i < maxList; i++)
            {
                T1 h = list[i];
                ISTARNETDNA dna = h?.STARNETDNA;
                string nm = string.IsNullOrWhiteSpace(dna?.Name) ? "(unnamed)" : dna.Name;
                Guid gid = dna != null ? dna.Id : Guid.Empty;
                rows.Add($"  {gid} — {nm}");
            }

            string more = total > maxList ? $"{Environment.NewLine}  ... and {total - maxList} more." : "";
            return $"Multiple matches ({total}) for '{idOrName}' ({holonUiName}). Use a GUID in non-interactive mode.{Environment.NewLine}Candidates:{Environment.NewLine}{string.Join(Environment.NewLine, rows)}{more}";
        }

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
    }
}
