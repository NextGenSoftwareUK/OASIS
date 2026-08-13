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
        public virtual async Task<OASISResult<T1>> CreateAsync(ISTARNETCreateOptions<T1, T4> createOptions = null, object holonSubType = null, bool showHeaderAndInro = true, bool addDependencies = true, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = new OASISResult<T1>();

            if (TryReadScriptedNonInteractiveCreate(createOptions, out string scriptedName, out string scriptedDesc, out string scriptedSubType, out string scriptedParentFolder))
            {
                if (showHeaderAndInro)
                    ShowHeader();

                return await CreateAsyncScriptedNonInteractiveAsync(result, createOptions, scriptedName, scriptedDesc ?? "", scriptedSubType, scriptedParentFolder, showHeaderAndInro, addDependencies, providerType);
            }

            if (showHeaderAndInro)
                ShowHeader();

            string holonName = CLIEngine.GetValidInput($"What is the name of the {STARNETManager.STARNETHolonUIName}?");

            if (holonName == "exit")
            {
                result.Message = "User Exited";
                return result;
            }


            string holonDesc = CLIEngine.GetValidInput($"What is the description of the {STARNETManager.STARNETHolonUIName}?");

            if (holonDesc == "exit")
            {
                result.Message = "User Exited";
                return result;
            }

            if (holonSubType == null)
                holonSubType = CLIEngine.GetValidInputForEnum($"What type of {STARNETManager.STARNETHolonUIName} do you wish to create?", STARNETManager.STARNETCategory);

            if (holonSubType != null)
            {
                if (holonSubType.ToString() == "exit")
                {
                    result.Message = "User Exited";
                    return result;
                }

                //Type STARNETHolonType = (Type)value;
                string holonPath = "";

                if (Path.IsPathRooted(SourcePath) || string.IsNullOrEmpty(STAR.STARDNA.STARNETBasePath))
                    holonPath = SourcePath;
                else
                    holonPath = Path.Combine(STAR.STARDNA.STARNETBasePath, SourcePath);

                (result, holonPath) = GetValidFolder(result, holonPath, STARNETManager.STARNETHolonUIName, SourceSTARDNAKey, true, holonName);

                if (result.IsError)
                    return result;

                //if (!CLIEngine.GetConfirmation($"Do you wish to create the {STARNETManager.STARNETHolonUIName} in the default path defined in the STARDNA as '{SourceSTARDNAKey}'? The current path points to: {holonPath}"))
                //    holonPath = CLIEngine.GetValidFolder($"Where do you wish to create the {STARNETManager.STARNETHolonUIName}?");

                //holonPath = Path.Combine(holonPath, holonName);

                //if (Directory.Exists(holonPath) && checkIfSourcePathExists)
                //{
                //    if (CLIEngine.GetConfirmation($"The directory {holonPath} already exists! Would you like to delete it?"))
                //    {
                //        Console.WriteLine("");
                //        Directory.Delete(holonPath, true);
                //    }
                //    else
                //    {
                //        Console.WriteLine("");
                //        OASISErrorHandling.HandleError(ref result, $"The directory {holonPath} already exists! Please either delete it or choose a different name.");
                //        return result;
                //    }
                //}

                //await AddLibsRuntimesAndTemplatesAsync(createResult.Result.STARNETDNA, "OAPP Template", providerType);

                Console.WriteLine("");
                CLIEngine.ShowWorkingMessage($"Generating {STARNETManager.STARNETHolonUIName}...");
                //OASISResult<T1> starHolonResult = await STARNETManager.CreateAsync(STAR.BeamedInAvatar.Id, holonName, holonDesc, Type, holonPath, providerType);
                //result = await STARNETManager.CreateAsync(STAR.BeamedInAvatar.Id, holonName, holonDesc, holonSubType, holonPath, newHolon: newHolon, checkIfSourcePathExists: checkIfSourcePathExists, metaData: metaData, STARNETDNA: STARNETDNA, providerType: providerType);
                //result = await STARNETManager.CreateAsync(STAR.BeamedInAvatar.Id, holonName, holonDesc, holonSubType, holonPath, newHolon: newHolon, checkIfSourcePathExists: checkIfSourcePathExists, STARNETDNA: STARNETDNA, providerType: providerType);
                result = await STARNETManager.CreateAsync(STAR.BeamedInAvatar.Id, holonName, holonDesc, holonSubType, holonPath, createOptions: createOptions, providerType: providerType);

                if (result != null)
                {
                    if (!result.IsError && result.Result != null)
                    {
                        CLIEngine.ShowSuccessMessage($"{STARNETManager.STARNETHolonUIName} Successfully Generated.");
                        await ShowAsync(result.Result);
                        Console.WriteLine("");

                        if (addDependencies)
                            await AddDependenciesAsync(result.Result.STARNETDNA, providerType);

                        if (CLIEngine.GetConfirmation($"Do you wish to open the {STARNETManager.STARNETHolonUIName} folder now?"))
                            Process.Start("explorer.exe", holonPath);

                        Console.WriteLine("");
                    }
                }
                else
                    CLIEngine.ShowErrorMessage($"Unknown Error Occured.");
            }
            else
                OASISErrorHandling.HandleError(ref result, "holonSubType is null!");

            return result;
        }

        /// <summary>Reads scripted-create fields from <c>CustomCreateParams</c> (<see cref="StarCliNonInteractiveCreateKeys"/>), populated by <see cref="StarnetUiScriptedCreateCli"/> from argv.</summary>
        protected static bool TryReadScriptedNonInteractiveCreate(ISTARNETCreateOptions<T1, T4> createOptions, out string scriptedName, out string scriptedDesc, out string scriptedSubType, out string scriptedParentFolder)
        {
            scriptedName = null;
            scriptedDesc = null;
            scriptedSubType = null;
            scriptedParentFolder = null;

            if (createOptions?.CustomCreateParams == null)
                return false;

            Dictionary<string, object> p = createOptions.CustomCreateParams;
            if (!p.TryGetValue(StarCliNonInteractiveCreateKeys.Scripted, out object flagObj) || flagObj is not bool scripted || !scripted)
                return false;

            if (!p.TryGetValue(StarCliNonInteractiveCreateKeys.Name, out object n) || n == null || string.IsNullOrWhiteSpace(n.ToString()))
                return false;

            if (!p.TryGetValue(StarCliNonInteractiveCreateKeys.SubType, out object st) || st == null || string.IsNullOrWhiteSpace(st.ToString()))
                return false;

            p.TryGetValue(StarCliNonInteractiveCreateKeys.Description, out object d);
            scriptedName = n.ToString().Trim();
            scriptedDesc = d?.ToString() ?? "";
            scriptedSubType = st.ToString().Trim();

            if (p.TryGetValue(StarCliNonInteractiveCreateKeys.ParentFolder, out object pf) && pf != null && !string.IsNullOrWhiteSpace(pf.ToString()))
                scriptedParentFolder = pf.ToString().Trim();

            return true;
        }

        private async Task<OASISResult<T1>> CreateAsyncScriptedNonInteractiveAsync(
            OASISResult<T1> result,
            ISTARNETCreateOptions<T1, T4> createOptions,
            string holonName,
            string holonDesc,
            string subTypeToken,
            string parentFolderOpt,
            bool showUiDetails,
            bool addDependencies,
            ProviderType providerType)
        {
            object parsedSubType;
            try
            {
                parsedSubType = Enum.Parse(STARNETManager.STARNETCategory.GetType(), subTypeToken, ignoreCase: true);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Invalid category '{subTypeToken}' for {STARNETManager.STARNETHolonUIName}. Expected a {STARNETManager.STARNETCategory.GetType().Name} value. {ex.Message}");
                return result;
            }

            string holonPath;
            if (!string.IsNullOrWhiteSpace(parentFolderOpt))
                holonPath = Path.Combine(Path.GetFullPath(parentFolderOpt), holonName);
            else
            {
                string baseFolder;
                if (Path.IsPathRooted(SourcePath) || string.IsNullOrEmpty(STAR.STARDNA.STARNETBasePath))
                    baseFolder = SourcePath;
                else
                    baseFolder = Path.Combine(STAR.STARDNA.STARNETBasePath, SourcePath);

                holonPath = Path.Combine(baseFolder, holonName);
            }

            if (Directory.Exists(holonPath))
            {
                OASISErrorHandling.HandleError(ref result, $"Directory already exists: {holonPath}. Remove it or choose a different name.");
                return result;
            }

            Console.WriteLine("");
            CLIEngine.ShowWorkingMessage($"Generating {STARNETManager.STARNETHolonUIName}...");
            result = await STARNETManager.CreateAsync(STAR.BeamedInAvatar.Id, holonName, holonDesc, parsedSubType, holonPath, createOptions: createOptions, providerType: providerType);

            if (result != null && !result.IsError && result.Result != null)
            {
                CLIEngine.ShowSuccessMessage($"{STARNETManager.STARNETHolonUIName} Successfully Generated.");

                if (showUiDetails)
                {
                    await ShowAsync(result.Result);
                    Console.WriteLine("");
                }

                if (addDependencies)
                    await AddDependenciesAsync(result.Result.STARNETDNA, providerType);

                Console.WriteLine("");
            }
            else if (result != null && result.IsError)
            {
                CLIEngine.ShowErrorMessage(result.Message);
            }
            else
                CLIEngine.ShowErrorMessage("Unknown Error Occured.");

            return result;
        }
    }
}
