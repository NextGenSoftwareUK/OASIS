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
        public int DisplayFieldLength { get; set; } = DEFAULT_FIELD_LENGTH;

        public STARNETUIBase(ISTARNETManagerBase<T1, T2, T3, T4> starManager, string createHeader, List<string> createIntroParagraphs, string sourcePath = "", string sourceSTARDNAKey = "", string publishedPath = "", string publishedSTARDNAKey = "", string downloadedPath = "", string downloadSTARDNAKey = "", string installedPath = "", string installedSTARDNAKey = "", int displayFieldLength = DEFAULT_FIELD_LENGTH)
        {
            starManager.OnDownloadStatusChanged += OnDownloadStatusChanged;
            starManager.OnInstallStatusChanged += OnInstallStatusChanged;
            starManager.OnPublishStatusChanged += OnPublishStatusChanged;
            starManager.OnUploadStatusChanged += OnUploadStatusChanged;

            CreateHeader = createHeader;
            CreateIntroParagraphs = createIntroParagraphs;
            IsInit = true;
            STARNETManager = starManager;
            SourcePath = sourcePath;
            SourceSTARDNAKey = sourceSTARDNAKey;
            PublishedPath = publishedPath;
            PublishedSTARDNAKey = publishedSTARDNAKey;
            DownloadedPath = downloadedPath;
            DownloadSTARDNAKey = downloadSTARDNAKey;
            InstalledPath = installedPath;
            InstalledSTARDNAKey = installedSTARDNAKey;
            DisplayFieldLength = displayFieldLength;
        }

        public virtual void Dispose()
        {
            STARNETManager.OnDownloadStatusChanged -= OnDownloadStatusChanged;
            STARNETManager.OnInstallStatusChanged -= OnInstallStatusChanged;
            STARNETManager.OnPublishStatusChanged -= OnPublishStatusChanged;
            STARNETManager.OnUploadStatusChanged -= OnUploadStatusChanged;
        }

        //public virtual async Task<OASISResult<T1>> CreateAsync(object createParams, T1 newHolon = default, bool showHeaderAndInro = true, bool checkIfSourcePathExists = true, object holonSubType = null, Dictionary<string, object> metaData = null, T4 STARNETDNA = default, ProviderType providerType = ProviderType.Default)
        //public virtual async Task<OASISResult<T1>> CreateAsync(object createParams, T1 newHolon = default, T4 STARNETDNA = default, object holonSubType = null, bool showHeaderAndInro = true, bool checkIfSourcePathExists = true, ProviderType providerType = ProviderType.Default)
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

        public (OASISResult<T>, string) GetValidFolder<T>(OASISResult<T> result, string defaultPath, string pathDisplayName, string SourceSTARDNAKey, bool checkIfExists = true, string holonName = "")
        {
            if (!CLIEngine.GetConfirmation($"Do you wish to create the {pathDisplayName} in the default path defined in the STARDNA as '{SourceSTARDNAKey}' (recommended)? The current path points to: {defaultPath}"))
                defaultPath = CLIEngine.GetValidFolder($"Where do you wish to create the {pathDisplayName}?");

            if (!string.IsNullOrEmpty(holonName))
                defaultPath = Path.Combine(defaultPath, holonName);

            if (Directory.Exists(defaultPath) && checkIfExists)
            {
                Console.WriteLine("");
                if (CLIEngine.GetConfirmation($"The directory {defaultPath} already exists! Would you like to delete it?"))
                {
                    Console.WriteLine("");
                    Directory.Delete(defaultPath, true);
                }
                else
                {
                    Console.WriteLine("");
                    OASISErrorHandling.HandleError(ref result, $"The directory {defaultPath} already exists! Please either delete it or choose a different name.");
                    return (result, defaultPath);
                }
            }

            result.IsSaved = true;
            return (result, defaultPath);
        }

        public virtual async Task UpdateAsync(string idOrName = "", object editParams = null, bool editLaunchTarget = true, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> loadResult = await FindAsync("update", idOrName, default, true, providerType: providerType);
            bool changesMade = false;

            if (loadResult != null && loadResult.Result != null && !loadResult.IsError)
            {
                if (CLIEngine.GetConfirmation($"Do you wish to update the {STARNETManager.STARNETHolonUIName} Name? (currently is {loadResult.Result.Name})."))
                {
                    Console.WriteLine("");
                    loadResult.Result.STARNETDNA.Name = CLIEngine.GetValidInput($"What is the new name of the {STARNETManager.STARNETHolonUIName}?");
                    changesMade = true;
                }
                else
                    Console.WriteLine("");

                if (CLIEngine.GetConfirmation($"Do you wish to update the {STARNETManager.STARNETHolonUIName} Description? (currently is {loadResult.Result.Description}.)"))
                {
                    Console.WriteLine("");
                    loadResult.Result.STARNETDNA.Description = CLIEngine.GetValidInput($"What is the new description of the {STARNETManager.STARNETHolonUIName}?");
                    changesMade = true;
                }
                else
                    Console.WriteLine("");

                int icat = 0;
                string cat = "";

                if (int.TryParse(loadResult.Result.STARNETDNA.STARNETCategory.ToString(), out icat))
                    cat = Enum.GetName(STARNETManager.STARNETCategory.GetType(), Convert.ToInt32(loadResult.Result.STARNETDNA.STARNETCategory));

                else if (!string.IsNullOrEmpty(loadResult.Result.STARNETDNA.STARNETCategory.ToString()))
                    cat = loadResult.Result.STARNETDNA.STARNETCategory.ToString();

                else
                    cat = Enum.GetName(STARNETManager.STARNETCategory.GetType(), Convert.ToInt32(loadResult.Result.STARNETDNA.STARNETCategory));

                if (string.IsNullOrEmpty(cat))
                    cat = "Not set";

                if (CLIEngine.GetConfirmation($"Do you wish to update the {STARNETManager.STARNETHolonUIName} Category? (currently is {cat})."))
                //if (CLIEngine.GetConfirmation($"Do you wish to update the {STARNETManager.STARNETHolonUIName} Category? (currently is {Enum.Parse( loadResult.Result.STARNETDNA.STARNETCategory})."))
                {
                    Console.WriteLine("");
                    object holonSubType = CLIEngine.GetValidInputForEnum($"What is the new category of the {STARNETManager.STARNETHolonUIName}?", STARNETManager.STARNETCategory);

                    if (holonSubType != null)
                    {
                        if (holonSubType.ToString() == "exit")
                            return;

                        loadResult.Result.STARNETDNA.STARNETCategory = holonSubType;
                        changesMade = true;
                    }
                }
                else
                    Console.WriteLine("");

                // Update Language (STARNETSubCategory) for libraries
                if (STARNETManager.STARNETHolonType == HolonType.Library)
                {
                    string currentLanguage = loadResult.Result.STARNETDNA.STARNETSubCategory?.ToString() ?? "Not set";
                    if (CLIEngine.GetConfirmation($"Do you wish to update the Language? (currently is {currentLanguage})."))
                    {
                        Console.WriteLine("");
                        object language = CLIEngine.GetValidInputForEnum($"What is the new Language of the {STARNETManager.STARNETHolonUIName}?", typeof(Languages));

                        if (language != null)
                        {
                            if (language.ToString() == "exit")
                                return;

                            loadResult.Result.STARNETDNA.STARNETSubCategory = language;
                            changesMade = true;
                        }
                    }
                    else
                        Console.WriteLine("");
                }

                if (editLaunchTarget && CLIEngine.GetConfirmation(string.Concat("Do you wish to update the launch target? (currently is ", !string.IsNullOrEmpty(loadResult.Result.STARNETDNA.LaunchTarget) ? loadResult.Result.STARNETDNA.LaunchTarget : "None", ".)")))
                {
                    Console.WriteLine("");
                    loadResult.Result.STARNETDNA.LaunchTarget = CLIEngine.GetValidInput($"What is the new launch target of the {STARNETManager.STARNETHolonUIName}?");
                    changesMade = true;
                }
                else
                    Console.WriteLine("");

                await OnExtraUpdateFieldsAsync(loadResult, ref changesMade, providerType);

                if (changesMade)
                {
                    OASISResult<T1> result = await STARNETManager.EditAsync(STAR.BeamedInAvatar.Id, loadResult.Result, (T4)loadResult.Result.STARNETDNA, providerType);
                    Console.WriteLine("");
                    CLIEngine.ShowWorkingMessage($"Saving {STARNETManager.STARNETHolonUIName}...");

                    if (result != null && !result.IsError && result.Result != null)
                    {
                        (result, bool saveResult) = ErrorHandling.HandleResponse(result, await STARNETManager.WriteDNAAsync(result.Result.STARNETDNA, result.Result.STARNETDNA.SourcePath), "Error occured saving the STARNETDNA. Reason: ", $"{STARNETManager.STARNETHolonUIName} Successfully Updated.");

                        if (saveResult)
                            await ShowAsync(result.Result);
                    }
                    else
                        CLIEngine.ShowErrorMessage($"An error occured updating the {STARNETManager.STARNETHolonUIName}. Reason: {result.Message}");
                }

                if (loadResult.Result.STARNETDNA.PublishedOn != DateTime.MinValue && CLIEngine.GetConfirmation($"Do you wish to upload any changes you have made in the Source folder ({loadResult.Result.STARNETDNA.SourcePath})? The version number will remain the same ({loadResult.Result.STARNETDNA.Version})."))
                    await PublishAsync(loadResult.Result.STARNETDNA.SourcePath, true, DefaultLaunchMode.Optional, providerType: providerType);
                else
                    Console.WriteLine("");
            }
            else
            {
                Console.WriteLine("");
                CLIEngine.ShowErrorMessage($"An error occured loading the {STARNETManager.STARNETHolonUIName}. Reason: {loadResult.Message}");
            }
        }

        /// <summary>Override in derived types (e.g. <see cref="Quests"/>) to edit holon-specific fields after name/description/category/launch prompts.</summary>
        protected virtual Task OnExtraUpdateFieldsAsync(OASISResult<T1> loadResult, ref bool changesMade, ProviderType providerType)
        {
            return Task.CompletedTask;
        }

        public virtual async Task<OASISResult<T1>> AddDependencyAsync(string idOrNameOfParent = "", ISTARNETDNA parentSTARNETDNA = null, string idOrNameOfDependency = "", string dependencyType = "", ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            bool depSelected = false;
            DependencyType dependencyTypeEnum = DependencyType.Quest;

            if (!string.IsNullOrEmpty(dependencyType))
            {
                object depObj = Enum.Parse(typeof(DependencyType), dependencyType);

                if (depObj != "exit")
                {
                    dependencyTypeEnum = (DependencyType)depObj;
                    depSelected = true;
                }
            }

            if (!depSelected)
            {
                object depType = CLIEngine.GetValidInputForEnum("What type of dependency do you wish to add?", typeof(DependencyType));
                if (depType != null)
                {
                    if (depType.ToString() == "exit" || depType.ToString() == "None")
                    {
                        result.Message = "User Exited";
                        return result;
                    }
                    dependencyTypeEnum = (DependencyType)depType;
                }
            }

            string dependencyDisplayName = Enum.GetName(typeof(DependencyType), dependencyTypeEnum) ?? "Dependency";
            string dependenciesDisplayName = $"{dependencyDisplayName}s";

            if (dependencyTypeEnum == DependencyType.Library)
                dependenciesDisplayName = "libraries";

            if (parentSTARNETDNA == null)
            {
                OASISResult<T1> parentResult = await FindAsync("use", idOrNameOfParent, default, true, providerType: providerType);

                if (parentResult != null && !parentResult.IsError && parentResult.Result != null)
                    parentSTARNETDNA = parentResult.Result.STARNETDNA;
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"An error occured loading the {STARNETManager.STARNETHolonUIName} for id/name {idOrNameOfParent}. Reason: {parentResult.Message}");
                    return result;
                }
            }

            if (parentSTARNETDNA != null)
            {
                OASISResult<InstalledSTARNETHolon> installedDependency = new OASISResult<InstalledSTARNETHolon>();
                Type installedDependencyType;

                switch (dependencyTypeEnum)
                {
                    case DependencyType.OAPP:
                        {
                            OASISResult<InstalledOAPP> installedTemplate = await STARCLI.OAPPs.FindAndInstallIfNotInstalledAsync("use", idOrNameOfDependency, providerType: providerType);
                            installedDependency.Result = installedTemplate.Result;
                            OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(installedTemplate, installedDependency);
                            installedDependencyType = typeof(OAPP);
                        }
                        break;

                    case DependencyType.Runtime:
                        {
                            OASISResult<InstalledRuntime> installedHolon = await STARCLI.Runtimes.FindAndInstallIfNotInstalledAsync("use", idOrNameOfDependency, providerType: providerType);
                            installedDependency.Result = installedHolon.Result;
                            OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(installedHolon, installedDependency);
                        }
                        break;

                    case DependencyType.Library:
                        {
                            OASISResult<InstalledLibrary> installedHolon = await STARCLI.Libs.FindAndInstallIfNotInstalledAsync("use", idOrNameOfDependency, providerType: providerType);
                            installedDependency.Result = installedHolon.Result;
                            OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(installedHolon, installedDependency);
                        }
                        break;

                    case DependencyType.Template:
                        {
                            OASISResult<InstalledOAPPTemplate> installedHolon = await STARCLI.OAPPTemplates.FindAndInstallIfNotInstalledAsync("use", idOrNameOfDependency, providerType: providerType);
                            installedDependency.Result = installedHolon.Result;
                            OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(installedHolon, installedDependency);
                        }
                        break;

                    case DependencyType.NFT:
                        {
                            OASISResult<InstalledNFT> installedHolon = await STARCLI.NFTs.FindAndInstallIfNotInstalledAsync("use", idOrNameOfDependency, providerType: providerType);
                            installedDependency.Result = installedHolon.Result;
                            OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(installedHolon, installedDependency);
                        }
                        break;

                    case DependencyType.GeoNFT:
                        {
                            OASISResult<InstalledGeoNFT> installedHolon = await STARCLI.GeoNFTs.FindAndInstallIfNotInstalledAsync("use", idOrNameOfDependency, providerType: providerType);
                            installedDependency.Result = installedHolon.Result;
                            OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(installedHolon, installedDependency);
                        }
                        break;

                    case DependencyType.NFTCollection:
                        {
                            OASISResult<InstalledNFTCollection> installedHolon = await STARCLI.NFTCollections.FindAndInstallIfNotInstalledAsync("use", idOrNameOfDependency, providerType: providerType);
                            installedDependency.Result = installedHolon.Result;
                            OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(installedHolon, installedDependency);
                        }
                        break;

                    case DependencyType.GeoNFTCollection:
                        {
                            OASISResult<InstalledGeoNFTCollection> installedHolon = await STARCLI.GeoNFTCollections.FindAndInstallIfNotInstalledAsync("use", idOrNameOfDependency, providerType: providerType);
                            installedDependency.Result = installedHolon.Result;
                            OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(installedHolon, installedDependency);
                        }
                        break;

                    case DependencyType.GeoHotSpot:
                        {
                            OASISResult<InstalledGeoHotSpot> installedHolon = await STARCLI.GeoHotSpots.FindAndInstallIfNotInstalledAsync("use", idOrNameOfDependency, providerType: providerType);
                            installedDependency.Result = installedHolon.Result;
                            OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(installedHolon, installedDependency);
                        }
                        break;

                    case DependencyType.Quest:
                        {
                            OASISResult<InstalledQuest> installedHolon = await STARCLI.Quests.FindAndInstallIfNotInstalledAsync("use", idOrNameOfDependency, providerType: providerType);
                            installedDependency.Result = installedHolon.Result;
                            OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(installedHolon, installedDependency);
                        }
                        break;

                    case DependencyType.Mission:
                        {
                            OASISResult<InstalledMission> installedHolon = await STARCLI.Missions.FindAndInstallIfNotInstalledAsync("use", idOrNameOfDependency, providerType: providerType);
                            installedDependency.Result = installedHolon.Result;
                            OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(installedHolon, installedDependency);
                        }
                        break;

                    case DependencyType.Chapter:
                        {
                            OASISResult<InstalledChapter> installedHolon = await STARCLI.Chapters.FindAndInstallIfNotInstalledAsync("use", idOrNameOfDependency, providerType: providerType);
                            installedDependency.Result = installedHolon.Result;
                            OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(installedHolon, installedDependency);
                        }
                        break;

                    case DependencyType.InventoryItem:
                        {
                            OASISResult<InstalledInventoryItem> installedHolon = await STARCLI.InventoryItems.FindAndInstallIfNotInstalledAsync("use", idOrNameOfDependency, providerType: providerType);
                            installedDependency.Result = installedHolon.Result;
                            OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(installedHolon, installedDependency);
                        }
                        break;

                    case DependencyType.CelestialSpace:
                        {
                            OASISResult<InstalledCelestialSpace> installedHolon = await STARCLI.CelestialSpaces.FindAndInstallIfNotInstalledAsync("use", idOrNameOfDependency, providerType: providerType);
                            installedDependency.Result = installedHolon.Result;
                            OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(installedHolon, installedDependency);
                        }
                        break;

                    case DependencyType.CelestialBody:
                        {
                            OASISResult<InstalledCelestialBody> installedHolon = await STARCLI.CelestialBodies.FindAndInstallIfNotInstalledAsync("use", idOrNameOfDependency, providerType: providerType);
                            installedDependency.Result = installedHolon.Result;
                            OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(installedHolon, installedDependency);
                        }
                        break;

                    case DependencyType.Zome:
                        {
                            OASISResult<InstalledZome> installedHolon = await STARCLI.Zomes.FindAndInstallIfNotInstalledAsync("use", idOrNameOfDependency, providerType: providerType);
                            installedDependency.Result = installedHolon.Result;
                            OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(installedHolon, installedDependency);
                        }
                        break;

                    case DependencyType.Holon:
                        {
                            OASISResult<InstalledHolon> installedHolon = await STARCLI.Holons.FindAndInstallIfNotInstalledAsync("use", idOrNameOfDependency, providerType: providerType);
                            installedDependency.Result = installedHolon.Result;
                            OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(installedHolon, installedDependency);
                        }
                        break;

                    case DependencyType.CelestialBodyMetaDataDNA:
                        {
                            OASISResult<InstalledCelestialBodyMetaDataDNA> installedHolon = await STARCLI.CelestialBodiesMetaDataDNA.FindAndInstallIfNotInstalledAsync("use", idOrNameOfDependency, providerType: providerType);
                            installedDependency.Result = installedHolon.Result;
                            OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(installedHolon, installedDependency);
                        }
                        break;

                    case DependencyType.ZomeMetaDataDNA:
                        {
                            OASISResult<InstalledZomeMetaDataDNA> installedHolon = await STARCLI.ZomesMetaDataDNA.FindAndInstallIfNotInstalledAsync("use", idOrNameOfDependency, providerType: providerType);
                            installedDependency.Result = installedHolon.Result;
                            OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(installedHolon, installedDependency);
                        }
                        break;

                    case DependencyType.HolonMetaDataDNA:
                        {
                            OASISResult<InstalledHolonMetaDataDNA> installedHolon = await STARCLI.HolonsMetaDataDNA.FindAndInstallIfNotInstalledAsync("use", idOrNameOfDependency, providerType: providerType);
                            installedDependency.Result = installedHolon.Result;
                            OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(installedHolon, installedDependency);
                        }
                        break;
                }

                if (installedDependency != null && installedDependency.Result != null && !installedDependency.IsError)
                {
                    if (CLIEngine.GetConfirmation($"Do you wish to install the {dependencyDisplayName} '{installedDependency.Result.STARNETDNA.Name}' (v{installedDependency.Result.STARNETDNA.Version}) into the {STARNETManager.STARNETHolonUIName} '{parentSTARNETDNA.Name}'?"))
                    {
                        Console.WriteLine("");
                        DependencyInstallMode dependencyInstallMode = DependencyInstallMode.Nested;
                        object dependencyInstallModeObj = CLIEngine.GetValidInputForEnum($"Do you wish to install the dependency in the root of the {STARNETManager.STARNETHolonUIName}, in the Dependencies (Smartbricks) sub-folder (Nested)? (Recommended) or would you like to flatten the dependencies so all sub-dependencies are placed in the same level?", typeof(DependencyInstallMode));

                        if (dependencyInstallModeObj != null)
                            dependencyInstallMode = (DependencyInstallMode)dependencyInstallModeObj;

                        if (dependencyInstallMode != DependencyInstallMode.Nested)
                        {
                            CLIEngine.ShowWarningMessage("This feature is not yet fully implemented, please let us know if you would find this feature useful in future! Thank you! Defaulting to Nested...");
                            dependencyInstallMode = DependencyInstallMode.Nested;
                        }

                        bool installNow = CLIEngine.GetConfirmation($"Do you wish to install the {dependencyDisplayName} now? (recommended) (Selecting 'No' will just add it as a dependency in the STARNETDNA and you can install it later)");

                        if (!installNow)
                        {
                            Console.WriteLine("");
                            CLIEngine.ShowWarningMessage("This feature is not yet fully implemented, please let us know if you would find this feature useful in future! Thank you! Installing now...");
                            installNow = true;
                        }

                        Console.WriteLine("");
                        CLIEngine.ShowWorkingMessage($"Installing {dependencyDisplayName} '{installedDependency.Result.STARNETDNA.Name}' Into {STARNETManager.STARNETHolonUIName} '{parentSTARNETDNA.Name}'...");

                        switch (dependencyTypeEnum)
                        {
                            case DependencyType.OAPP:
                                result = await STARNETManager.AddDependencyAsync<InstalledOAPP>(STAR.BeamedInAvatar.Id, parentSTARNETDNA.Id, parentSTARNETDNA.Version, installedDependency.Result.STARNETDNA.Id, installedDependency.Result.STARNETDNA.Version, HolonType.InstalledOAPP, dependencyTypeEnum, installNow, dependencyInstallMode, providerType);
                                break;

                            case DependencyType.Runtime:
                                result = await STARNETManager.AddDependencyAsync<InstalledRuntime>(STAR.BeamedInAvatar.Id, parentSTARNETDNA.Id, parentSTARNETDNA.Version, installedDependency.Result.STARNETDNA.Id, installedDependency.Result.STARNETDNA.Version, HolonType.InstalledRuntime, dependencyTypeEnum, installNow, dependencyInstallMode, providerType);
                                break;

                            case DependencyType.Library:
                                result = await STARNETManager.AddDependencyAsync<InstalledLibrary>(STAR.BeamedInAvatar.Id, parentSTARNETDNA.Id, parentSTARNETDNA.Version, installedDependency.Result.STARNETDNA.Id, installedDependency.Result.STARNETDNA.Version, HolonType.InstalledLibrary, dependencyTypeEnum, installNow, dependencyInstallMode, providerType);
                                break;

                            case DependencyType.Template:
                                result = await STARNETManager.AddDependencyAsync<InstalledOAPPTemplate>(STAR.BeamedInAvatar.Id, parentSTARNETDNA.Id, parentSTARNETDNA.Version, installedDependency.Result.STARNETDNA.Id, installedDependency.Result.STARNETDNA.Version, HolonType.InstalledOAPPTemplate, dependencyTypeEnum, installNow, dependencyInstallMode, providerType);
                                break;

                            case DependencyType.NFT:
                                result = await STARNETManager.AddDependencyAsync<InstalledNFT>(STAR.BeamedInAvatar.Id, parentSTARNETDNA.Id, parentSTARNETDNA.Version, installedDependency.Result.STARNETDNA.Id, installedDependency.Result.STARNETDNA.Version, HolonType.InstalledNFT, dependencyTypeEnum, installNow, dependencyInstallMode, providerType);
                                break;

                            case DependencyType.GeoNFTCollection:
                                result = await STARNETManager.AddDependencyAsync<InstalledGeoNFT>(STAR.BeamedInAvatar.Id, parentSTARNETDNA.Id, parentSTARNETDNA.Version, installedDependency.Result.STARNETDNA.Id, installedDependency.Result.STARNETDNA.Version, HolonType.InstalledGeoNFTCollection, dependencyTypeEnum, installNow, dependencyInstallMode, providerType);
                                break;

                            case DependencyType.NFTCollection:
                                result = await STARNETManager.AddDependencyAsync<InstalledNFT>(STAR.BeamedInAvatar.Id, parentSTARNETDNA.Id, parentSTARNETDNA.Version, installedDependency.Result.STARNETDNA.Id, installedDependency.Result.STARNETDNA.Version, HolonType.InstalledNFTCollection, dependencyTypeEnum, installNow, dependencyInstallMode, providerType);
                                break;

                            case DependencyType.GeoNFT:
                                result = await STARNETManager.AddDependencyAsync<InstalledGeoNFT>(STAR.BeamedInAvatar.Id, parentSTARNETDNA.Id, parentSTARNETDNA.Version, installedDependency.Result.STARNETDNA.Id, installedDependency.Result.STARNETDNA.Version, HolonType.InstalledGeoNFT, dependencyTypeEnum, installNow, dependencyInstallMode, providerType);
                                break;

                            case DependencyType.GeoHotSpot:
                                result = await STARNETManager.AddDependencyAsync<InstalledGeoHotSpot>(STAR.BeamedInAvatar.Id, parentSTARNETDNA.Id, parentSTARNETDNA.Version, installedDependency.Result.STARNETDNA.Id, installedDependency.Result.STARNETDNA.Version, HolonType.InstalledGeoHotSpot, dependencyTypeEnum, installNow, dependencyInstallMode, providerType);
                                break;

                            case DependencyType.Quest:
                                result = await STARNETManager.AddDependencyAsync<InstalledQuest>(STAR.BeamedInAvatar.Id, parentSTARNETDNA.Id, parentSTARNETDNA.Version, installedDependency.Result.STARNETDNA.Id, installedDependency.Result.STARNETDNA.Version, HolonType.InstalledQuest, dependencyTypeEnum, installNow, dependencyInstallMode, providerType);
                                break;

                            case DependencyType.Mission:
                                result = await STARNETManager.AddDependencyAsync<InstalledMission>(STAR.BeamedInAvatar.Id, parentSTARNETDNA.Id, parentSTARNETDNA.Version, installedDependency.Result.STARNETDNA.Id, installedDependency.Result.STARNETDNA.Version, HolonType.InstalledMission, dependencyTypeEnum, installNow, dependencyInstallMode, providerType);
                                break;

                            case DependencyType.Chapter:
                                result = await STARNETManager.AddDependencyAsync<InstalledChapter>(STAR.BeamedInAvatar.Id, parentSTARNETDNA.Id, parentSTARNETDNA.Version, installedDependency.Result.STARNETDNA.Id, installedDependency.Result.STARNETDNA.Version, HolonType.InstalledChapter, dependencyTypeEnum, installNow, dependencyInstallMode, providerType);
                                break;

                            case DependencyType.InventoryItem:
                                result = await STARNETManager.AddDependencyAsync<InstalledInventoryItem>(STAR.BeamedInAvatar.Id, parentSTARNETDNA.Id, parentSTARNETDNA.Version, installedDependency.Result.STARNETDNA.Id, installedDependency.Result.STARNETDNA.Version, HolonType.InstalledInventoryItem, dependencyTypeEnum, installNow, dependencyInstallMode, providerType);
                                break;

                            case DependencyType.CelestialSpace:
                                result = await STARNETManager.AddDependencyAsync<InstalledCelestialSpace>(STAR.BeamedInAvatar.Id, parentSTARNETDNA.Id, parentSTARNETDNA.Version, installedDependency.Result.STARNETDNA.Id, installedDependency.Result.STARNETDNA.Version, HolonType.InstalledCelestialSpace, dependencyTypeEnum, installNow, dependencyInstallMode, providerType);
                                break;

                            case DependencyType.CelestialBody:
                                result = await STARNETManager.AddDependencyAsync<InstalledCelestialBody>(STAR.BeamedInAvatar.Id, parentSTARNETDNA.Id, parentSTARNETDNA.Version, installedDependency.Result.STARNETDNA.Id, installedDependency.Result.STARNETDNA.Version, HolonType.InstalledCelestialBody, dependencyTypeEnum, installNow, dependencyInstallMode, providerType);
                                break;

                            case DependencyType.Zome:
                                result = await STARNETManager.AddDependencyAsync<InstalledZome>(STAR.BeamedInAvatar.Id, parentSTARNETDNA.Id, parentSTARNETDNA.Version, installedDependency.Result.STARNETDNA.Id, installedDependency.Result.STARNETDNA.Version, HolonType.InstalledZome, dependencyTypeEnum, installNow, dependencyInstallMode, providerType);
                                break;

                            case DependencyType.Holon:
                                result = await STARNETManager.AddDependencyAsync<InstalledHolon>(STAR.BeamedInAvatar.Id, parentSTARNETDNA.Id, parentSTARNETDNA.Version, installedDependency.Result.STARNETDNA.Id, installedDependency.Result.STARNETDNA.Version, HolonType.InstalledHolon, dependencyTypeEnum, installNow, dependencyInstallMode, providerType);
                                break;

                            case DependencyType.CelestialBodyMetaDataDNA:
                                result = await STARNETManager.AddDependencyAsync<InstalledCelestialBodyMetaDataDNA>(STAR.BeamedInAvatar.Id, parentSTARNETDNA.Id, parentSTARNETDNA.Version, installedDependency.Result.STARNETDNA.Id, installedDependency.Result.STARNETDNA.Version, HolonType.InstalledCelestialBodyMetaDataDNA, dependencyTypeEnum, installNow, dependencyInstallMode, providerType);
                                break;

                            case DependencyType.ZomeMetaDataDNA:
                                result = await STARNETManager.AddDependencyAsync<InstalledZomeMetaDataDNA>(STAR.BeamedInAvatar.Id, parentSTARNETDNA.Id, parentSTARNETDNA.Version, installedDependency.Result.STARNETDNA.Id, installedDependency.Result.STARNETDNA.Version, HolonType.InstalledZomeMetaDataDNA, dependencyTypeEnum, installNow, dependencyInstallMode, providerType);
                                break;

                            case DependencyType.HolonMetaDataDNA:
                                result = await STARNETManager.AddDependencyAsync<InstalledHolonMetaDataDNA>(STAR.BeamedInAvatar.Id, parentSTARNETDNA.Id, parentSTARNETDNA.Version, installedDependency.Result.STARNETDNA.Id, installedDependency.Result.STARNETDNA.Version, HolonType.InstalledHolonMetaDataDNA, dependencyTypeEnum, installNow, dependencyInstallMode, providerType);
                                break;
                        }

                        if (result != null && result.Result != null && !result.IsError)
                            CLIEngine.ShowSuccessMessage($"{dependencyDisplayName} '{installedDependency.Result.STARNETDNA.Name}' added to {STARNETManager.STARNETHolonUIName} '{parentSTARNETDNA.Name}'.");
                        else
                            CLIEngine.ShowErrorMessage($"Failed to add {dependencyDisplayName} '{installedDependency.Result.STARNETDNA.Name}' to {STARNETManager.STARNETHolonUIName} '{parentSTARNETDNA.Name}'. Error: {result.Message}");
                    }
                }
                else
                    CLIEngine.ShowErrorMessage($"Failed to add {dependencyDisplayName} to {STARNETManager.STARNETHolonUIName} '{parentSTARNETDNA.Name}'. Error: {installedDependency.Message}");
            }

            return result;
        }

        public virtual async Task<OASISResult<T1>> RemoveDependencyAsync(string idOrNameOfParent = "", string idOrNameOfDependency = "", string dependencyType = "", ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            bool depSelected = false;
            DependencyType dependencyTypeEnum = DependencyType.Quest;
            List<STARNETDependency> dependencies = new List<STARNETDependency>();

            if (!string.IsNullOrEmpty(dependencyType))
            {
                object depObj = Enum.Parse(typeof(DependencyType), dependencyType);

                if (depObj != "exit")
                {
                    dependencyTypeEnum = (DependencyType)depObj;
                    depSelected = true;
                }
            }


            if (!depSelected)
            {
                object depType = CLIEngine.GetValidInputForEnum("What type of dependency (Smartbrick) do you wish to remove?", typeof(DependencyType));
                if (depType != null)
                {
                    if (depType.ToString() == "exit" || depType.ToString() == "None")
                    {
                        result.Message = "User Exited";
                        return result;
                    }
                    dependencyTypeEnum = (DependencyType)depType;
                }
            }

            string dependencyDisplayName = Enum.GetName(typeof(DependencyType), dependencyTypeEnum) ?? "Dependency";
            string dependenciesDisplayName = $"{dependencyDisplayName}s";

            if (dependencyTypeEnum == DependencyType.Library)
                dependenciesDisplayName = "libraries";

            OASISResult<T1> parentResult = await FindAsync("use", idOrNameOfParent, default, true, providerType: providerType);

            if (parentResult != null && !parentResult.IsError && parentResult.Result != null)
            {
                ISTARNETDependency selectedLib = null;

                do
                {
                    if (string.IsNullOrEmpty(idOrNameOfDependency))
                    {
                        switch (dependencyTypeEnum)
                        {
                            case DependencyType.OAPP:
                                dependencies = parentResult.Result.STARNETDNA.Dependencies.OAPPs;
                                break;

                            case DependencyType.Runtime:
                                dependencies = parentResult.Result.STARNETDNA.Dependencies.Runtimes;
                                break;

                            case DependencyType.Library:
                                dependencies = parentResult.Result.STARNETDNA.Dependencies.Libraries;
                                break;

                            case DependencyType.Template:
                                dependencies = parentResult.Result.STARNETDNA.Dependencies.Templates;
                                break;

                            case DependencyType.NFT:
                                dependencies = parentResult.Result.STARNETDNA.Dependencies.NFTs;
                                break;

                            case DependencyType.GeoNFT:
                                dependencies = parentResult.Result.STARNETDNA.Dependencies.GeoNFTs;
                                break;

                            case DependencyType.NFTCollection:
                                dependencies = parentResult.Result.STARNETDNA.Dependencies.NFTCollections;
                                break;

                            case DependencyType.GeoNFTCollection:
                                dependencies = parentResult.Result.STARNETDNA.Dependencies.GeoNFTCollections;
                                break;

                            case DependencyType.GeoHotSpot:
                                dependencies = parentResult.Result.STARNETDNA.Dependencies.GeoHotSpots;
                                break;

                            case DependencyType.Quest:
                                dependencies = parentResult.Result.STARNETDNA.Dependencies.Quests;
                                break;

                            case DependencyType.Mission:
                                dependencies = parentResult.Result.STARNETDNA.Dependencies.Missions;
                                break;

                            case DependencyType.Chapter:
                                dependencies = parentResult.Result.STARNETDNA.Dependencies.Chapters;
                                break;

                            case DependencyType.InventoryItem:
                                dependencies = parentResult.Result.STARNETDNA.Dependencies.InventoryItems;
                                break;

                            case DependencyType.CelestialSpace:
                                dependencies = parentResult.Result.STARNETDNA.Dependencies.CelestialSpaces;
                                break;

                            case DependencyType.CelestialBody:
                                dependencies = parentResult.Result.STARNETDNA.Dependencies.CelestialBodies;
                                break;

                            case DependencyType.Zome:
                                dependencies = parentResult.Result.STARNETDNA.Dependencies.Zomes;
                                break;

                            case DependencyType.Holon:
                                dependencies = parentResult.Result.STARNETDNA.Dependencies.Holons;
                                break;

                            case DependencyType.CelestialBodyMetaDataDNA:
                                dependencies = parentResult.Result.STARNETDNA.Dependencies.CelestialBodiesMetaDataDNA;
                                break;

                            case DependencyType.ZomeMetaDataDNA:
                                dependencies = parentResult.Result.STARNETDNA.Dependencies.ZomesMetaDataDNA;
                                break;

                            case DependencyType.HolonMetaDataDNA:
                                dependencies = parentResult.Result.STARNETDNA.Dependencies.HolonsMetaDataDNA;
                                break;
                        }

                        CLIEngine.ShowMessage($"{dependencies.Count} {dependencyDisplayName}(s) Found:");

                        foreach (ISTARNETDependency metaData in dependencies)
                        {
                            ShowDependency(metaData, DisplayFieldLength);
                            CLIEngine.ShowDivider();
                        }

                        idOrNameOfDependency = CLIEngine.GetValidInput($"What is the ID/Name of the {dependencyDisplayName} you wish to remove from the {parentResult.Result.STARNETDNA.STARNETHolonType} with title '{parentResult.Result.STARNETDNA.Name}'? (or type 'exit' to cancel)");
                    }

                    if (Guid.TryParse(idOrNameOfDependency, out Guid runtimeId))
                        selectedLib = dependencies.FirstOrDefault(x => x.STARNETHolonId == runtimeId);
                    else
                    {
                        selectedLib = dependencies.FirstOrDefault(x => x.Name == idOrNameOfDependency);

                        if (selectedLib == null)
                        {
                            IEnumerable<ISTARNETDependency> results = dependencies.Where(x => x.Name.ToLower().Contains(idOrNameOfDependency.ToLower()));

                            if (results != null && results.Count() > 0)
                            {
                                CLIEngine.ShowWarningMessage($"No exact match was found for that name, but the {dependenciesDisplayName}(s) below are similar:");

                                foreach (ISTARNETDependency lib in results)
                                {
                                    ShowDependency(lib, DisplayFieldLength);
                                    CLIEngine.ShowDivider();
                                }


                                idOrNameOfDependency = CLIEngine.GetValidInput("Please make sure you enter the EXACT name (case sensitive) and try again!");
                                selectedLib = dependencies.FirstOrDefault(x => x.Name == idOrNameOfDependency);
                            }
                            //else
                            //    CLIEngine.ShowWarningMessage("No match was found, please try again!");
                        }
                    }

                    if (selectedLib != null)
                    {
                        ShowDependency(selectedLib, DisplayFieldLength);

                        if (!CLIEngine.GetConfirmation($"Please confirm you wish to remove the '{selectedLib.Name}' {dependencyDisplayName.ToLower()} from the {STARNETManager.STARNETHolonUIName} '{parentResult.Result.STARNETDNA.Name}'?", ConsoleColor.Magenta))
                            selectedLib = null;

                        Console.WriteLine("");
                    }
                    else
                        CLIEngine.ShowErrorMessage($"{dependencyDisplayName} was not found, please try again!");

                    idOrNameOfDependency = "";

                } while (selectedLib == null && idOrNameOfDependency.ToLower() != "exit");

                //Im super happy Im super happy Im super happy Im super happy! :) ;) :) :) :) :)
                CLIEngine.ShowWorkingMessage($"Removing {dependencyDisplayName} '{selectedLib.Name}' From {STARNETManager.STARNETHolonUIName} '{parentResult.Result.STARNETDNA.Name}'...");

                switch (dependencyTypeEnum)
                {
                    case DependencyType.OAPP:
                        result = await STARNETManager.RemoveDependencyAsync<InstalledOAPP>(STAR.BeamedInAvatar.Id, parentResult.Result.STARNETDNA.Id, parentResult.Result.STARNETDNA.Version, selectedLib.STARNETHolonId, selectedLib.Version, HolonType.InstalledRuntime, dependencyTypeEnum, providerType);
                        break;

                    case DependencyType.Runtime:
                        result = await STARNETManager.RemoveDependencyAsync<InstalledRuntime>(STAR.BeamedInAvatar.Id, parentResult.Result.STARNETDNA.Id, parentResult.Result.STARNETDNA.Version, selectedLib.STARNETHolonId, selectedLib.Version, HolonType.InstalledRuntime, dependencyTypeEnum, providerType);
                        break;

                    case DependencyType.Library:
                        result = await STARNETManager.RemoveDependencyAsync<InstalledLibrary>(STAR.BeamedInAvatar.Id, parentResult.Result.STARNETDNA.Id, parentResult.Result.STARNETDNA.Version, selectedLib.STARNETHolonId, selectedLib.Version, HolonType.InstalledLibrary, dependencyTypeEnum, providerType);
                        break;

                    case DependencyType.Template:
                        result = await STARNETManager.RemoveDependencyAsync<InstalledOAPPTemplate>(STAR.BeamedInAvatar.Id, parentResult.Result.STARNETDNA.Id, parentResult.Result.STARNETDNA.Version, selectedLib.STARNETHolonId, selectedLib.Version, HolonType.InstalledLibrary, dependencyTypeEnum, providerType);
                        break;

                    case DependencyType.NFT:
                        result = await STARNETManager.RemoveDependencyAsync<InstalledNFT>(STAR.BeamedInAvatar.Id, parentResult.Result.STARNETDNA.Id, parentResult.Result.STARNETDNA.Version, selectedLib.STARNETHolonId, selectedLib.Version, HolonType.InstalledNFT, dependencyTypeEnum, providerType);
                        break;

                    case DependencyType.GeoNFT:
                        result = await STARNETManager.RemoveDependencyAsync<InstalledGeoNFT>(STAR.BeamedInAvatar.Id, parentResult.Result.STARNETDNA.Id, parentResult.Result.STARNETDNA.Version, selectedLib.STARNETHolonId, selectedLib.Version, HolonType.InstalledGeoNFT, dependencyTypeEnum, providerType);
                        break;

                    case DependencyType.NFTCollection:
                        result = await STARNETManager.RemoveDependencyAsync<InstalledNFTCollection>(STAR.BeamedInAvatar.Id, parentResult.Result.STARNETDNA.Id, parentResult.Result.STARNETDNA.Version, selectedLib.STARNETHolonId, selectedLib.Version, HolonType.InstalledNFTCollection, dependencyTypeEnum, providerType);
                        break;

                    case DependencyType.GeoNFTCollection:
                        result = await STARNETManager.RemoveDependencyAsync<InstalledGeoNFTCollection>(STAR.BeamedInAvatar.Id, parentResult.Result.STARNETDNA.Id, parentResult.Result.STARNETDNA.Version, selectedLib.STARNETHolonId, selectedLib.Version, HolonType.InstalledGeoNFTCollection, dependencyTypeEnum, providerType);
                        break;

                    case DependencyType.GeoHotSpot:
                        result = await STARNETManager.RemoveDependencyAsync<InstalledGeoHotSpot>(STAR.BeamedInAvatar.Id, parentResult.Result.STARNETDNA.Id, parentResult.Result.STARNETDNA.Version, selectedLib.STARNETHolonId, selectedLib.Version, HolonType.InstalledGeoHotSpot, dependencyTypeEnum, providerType);
                        break;

                    case DependencyType.Quest:
                        result = await STARNETManager.RemoveDependencyAsync<InstalledQuest>(STAR.BeamedInAvatar.Id, parentResult.Result.STARNETDNA.Id, parentResult.Result.STARNETDNA.Version, selectedLib.STARNETHolonId, selectedLib.Version, HolonType.InstalledQuest, dependencyTypeEnum, providerType);
                        break;

                    case DependencyType.Mission:
                        result = await STARNETManager.RemoveDependencyAsync<InstalledMission>(STAR.BeamedInAvatar.Id, parentResult.Result.STARNETDNA.Id, parentResult.Result.STARNETDNA.Version, selectedLib.STARNETHolonId, selectedLib.Version, HolonType.InstalledMission, dependencyTypeEnum, providerType);
                        break;

                    case DependencyType.Chapter:
                        result = await STARNETManager.RemoveDependencyAsync<InstalledChapter>(STAR.BeamedInAvatar.Id, parentResult.Result.STARNETDNA.Id, parentResult.Result.STARNETDNA.Version, selectedLib.STARNETHolonId, selectedLib.Version, HolonType.InstalledChapter, dependencyTypeEnum, providerType);
                        break;

                    case DependencyType.InventoryItem:
                        result = await STARNETManager.RemoveDependencyAsync<InstalledInventoryItem>(STAR.BeamedInAvatar.Id, parentResult.Result.STARNETDNA.Id, parentResult.Result.STARNETDNA.Version, selectedLib.STARNETHolonId, selectedLib.Version, HolonType.InstalledInventoryItem, dependencyTypeEnum, providerType);
                        break;

                    case DependencyType.CelestialSpace:
                        result = await STARNETManager.RemoveDependencyAsync<InstalledCelestialSpace>(STAR.BeamedInAvatar.Id, parentResult.Result.STARNETDNA.Id, parentResult.Result.STARNETDNA.Version, selectedLib.STARNETHolonId, selectedLib.Version, HolonType.InstalledCelestialSpace, dependencyTypeEnum, providerType);
                        break;

                    case DependencyType.CelestialBody:
                        result = await STARNETManager.RemoveDependencyAsync<InstalledCelestialBody>(STAR.BeamedInAvatar.Id, parentResult.Result.STARNETDNA.Id, parentResult.Result.STARNETDNA.Version, selectedLib.STARNETHolonId, selectedLib.Version, HolonType.InstalledCelestialBody, dependencyTypeEnum, providerType);
                        break;

                    case DependencyType.Zome:
                        result = await STARNETManager.RemoveDependencyAsync<InstalledZome>(STAR.BeamedInAvatar.Id, parentResult.Result.STARNETDNA.Id, parentResult.Result.STARNETDNA.Version, selectedLib.STARNETHolonId, selectedLib.Version, HolonType.InstalledZome, dependencyTypeEnum, providerType);
                        break;

                    case DependencyType.Holon:
                        result = await STARNETManager.RemoveDependencyAsync<InstalledHolon>(STAR.BeamedInAvatar.Id, parentResult.Result.STARNETDNA.Id, parentResult.Result.STARNETDNA.Version, selectedLib.STARNETHolonId, selectedLib.Version, HolonType.InstalledHolon, dependencyTypeEnum, providerType);
                        break;

                    case DependencyType.CelestialBodyMetaDataDNA:
                        result = await STARNETManager.RemoveDependencyAsync<InstalledCelestialBodyMetaDataDNA>(STAR.BeamedInAvatar.Id, parentResult.Result.STARNETDNA.Id, parentResult.Result.STARNETDNA.Version, selectedLib.STARNETHolonId, selectedLib.Version, HolonType.InstalledCelestialBodyMetaDataDNA, dependencyTypeEnum, providerType);
                        break;

                    case DependencyType.ZomeMetaDataDNA:
                        result = await STARNETManager.RemoveDependencyAsync<InstalledZomeMetaDataDNA>(STAR.BeamedInAvatar.Id, parentResult.Result.STARNETDNA.Id, parentResult.Result.STARNETDNA.Version, selectedLib.STARNETHolonId, selectedLib.Version, HolonType.InstalledZomeMetaDataDNA, dependencyTypeEnum, providerType);
                        break;

                    case DependencyType.HolonMetaDataDNA:
                        result = await STARNETManager.RemoveDependencyAsync<InstalledHolonMetaDataDNA>(STAR.BeamedInAvatar.Id, parentResult.Result.STARNETDNA.Id, parentResult.Result.STARNETDNA.Version, selectedLib.STARNETHolonId, selectedLib.Version, HolonType.InstalledHolonMetaDataDNA, dependencyTypeEnum, providerType);
                        break;
                }

                if (result != null && result.Result != null && !result.IsError)
                    CLIEngine.ShowSuccessMessage($"{dependencyDisplayName} '{selectedLib.Name}' removed from {STARNETManager.STARNETHolonUIName} '{parentResult.Result.STARNETDNA.Name}'.");
                else
                    CLIEngine.ShowErrorMessage($"Failed to remove {dependencyDisplayName.ToLower()} '{selectedLib.Name}' from {STARNETManager.STARNETHolonUIName} '{parentResult.Result.STARNETDNA.Name}'. Error: {result.Message}");
            }
            else
            {
                Console.WriteLine("");
                CLIEngine.ShowErrorMessage($"An error occured loading the {STARNETManager.STARNETHolonUIName} for id/name {idOrNameOfParent}. Reason: {parentResult.Message}");
            }

            return result;
        }

        //public virtual async Task RemoveTemplateAsync(string idOrNameOfParent = "", string idOrNameOfTemplate = "", ProviderType providerType = ProviderType.Default)
        //{
        //    OASISResult<T1> result = await FindAsync("use", idOrNameOfParent, true, providerType: providerType);

        //    if (result != null && !result.IsError && result.Result != null)
        //    {
        //        bool validTemplateSelected = false;
        //        IOAPPTemplate selectedTemplate = null;

        //        do
        //        {
        //            if (string.IsNullOrEmpty(idOrNameOfTemplate))
        //            {
        //                //TODO: Need to list all installed templates for the given parent here and allow user to select one.
        //                foreach (ISTARNETDependency metaData in result.Result.STARNETDNA.Dependencies.Templates)
        //                {
        //                    ShowDependency(metaData, DisplayFieldLength);
        //                    CLIEngine.ShowDivider();
        //                }

        //                idOrNameOfTemplate = CLIEngine.GetValidInput("What ID/Name of the Template do you wish to remove from STARNET? (or type 'exit' to cancel)");
        //            }

        //            if (Guid.TryParse(idOrNameOfTemplate, out Guid templateId))
        //            {
        //                OASISResult<OAPPTemplate> templateResult = await STAR.STARAPI.OAPPTemplates.LoadAsync(STAR.BeamedInAvatar.Id, templateId, providerType: providerType);

        //                if (templateResult != null && templateResult.Result != null && !templateResult.IsError)
        //                    selectedTemplate = templateResult.Result;
        //                else
        //                    CLIEngine.ShowErrorMessage($"Failed to load template with ID '{templateId}'. Error: {templateResult.Message}");
        //            }
        //            else
        //                CLIEngine.ShowErrorMessage($"Invalid Template ID '{idOrNameOfTemplate}'. Please provide a valid GUID.");

        //        } while (selectedTemplate == null && idOrNameOfTemplate.ToLower() != "exit");

        //        //Im super happy Im super happy Im super happy Im super happy! :) ;) :) :) :) :)
        //        CLIEngine.ShowWorkingMessage($"Removing Template '{selectedTemplate.STARNETDNA.Name}' From {STARNETManager.STARNETHolonUIName} '{result.Result.STARNETDNA.Name}'...");
        //        OASISResult<OAPPTemplate> removeResult = await STAR.STARAPI.OAPPTemplates.RemoveLibraryAsync(STAR.BeamedInAvatar.Id, result.Result.STARNETDNA.Id, result.Result.STARNETDNA.Version, selectedTemplate.STARNETDNA.Id, selectedTemplate.STARNETDNA.Version, providerType);

        //        if (removeResult != null && removeResult.Result != null && !removeResult.IsError)
        //            CLIEngine.ShowSuccessMessage($"Template '{selectedTemplate.STARNETDNA.Name}' removed from {STARNETManager.STARNETHolonUIName} '{result.Result.STARNETDNA.Name}'.");
        //        else
        //            CLIEngine.ShowErrorMessage($"Failed to remove template '{selectedTemplate.STARNETDNA.Name}' from {STARNETManager.STARNETHolonUIName} '{result.Result.STARNETDNA.Name}'. Error: {removeResult.Message}");
        //    }
        //    else
        //    {
        //        Console.WriteLine("");
        //        CLIEngine.ShowErrorMessage($"An error occured loading the {STARNETManager.STARNETHolonUIName} for id/name {idOrNameOfParent}. Reason: {result.Message}");
        //    }
        //}


        //public virtual async Task RemoveDependencyAsync<T>(string idOrNameOfParent = "", string idOrNameOfRuntime = "", ProviderType providerType = ProviderType.Default) where T : ISTARNETHolon
        //{
        //    OASISResult<T1> result = await FindAsync("use", idOrNameOfParent, true, providerType: providerType);

        //    if (result != null && !result.IsError && result.Result != null)
        //    {
        //        bool validRuntimeSelected = false;
        //        IRuntime selectedRuntime = null;

        //        do
        //        {
        //            if (string.IsNullOrEmpty(idOrNameOfRuntime))
        //            {
        //                //TODO: Need to list all installed runtimes for the given parent here and allow user to select one.
        //                foreach (ISTARNETDependency metaData in result.Result.STARNETDNA.LibrariesMetaData)
        //                {
        //                    ShowSTARNETHolonMetaData(metaData, DisplayFieldLength);
        //                    CLIEngine.ShowDivider();
        //                }

        //                idOrNameOfRuntime = CLIEngine.GetValidInput("What ID/Name of the Runtime do you wish to remove from the STARNET? (or type 'exit' to cancel)");
        //            }

        //            if (Guid.TryParse(idOrNameOfRuntime, out Guid runtimeId))
        //            {
        //                OASISResult<T> runtimeResult = await STAR.STARAPI.Runtimes.LoadAsync(STAR.BeamedInAvatar.Id, runtimeId, providerType: providerType);

        //                if (runtimeResult != null && runtimeResult.Result != null && !runtimeResult.IsError)
        //                    selectedRuntime = runtimeResult.Result;
        //                else
        //                    CLIEngine.ShowErrorMessage($"Failed to load runtime with ID '{runtimeId}'. Error: {runtimeResult.Message}");
        //            }
        //            else
        //                CLIEngine.ShowErrorMessage($"Invalid Runtime ID '{idOrNameOfRuntime}'. Please provide a valid GUID.");

        //        } while (selectedRuntime == null && idOrNameOfRuntime.ToLower() != "exit");

        //        //Im super happy Im super happy Im super happy Im super happy! :) ;) :) :) :) :)
        //        OASISResult<Runtime> removeResult = await STAR.STARAPI.Runtimes.RemoveLibraryAsync(STAR.BeamedInAvatar.Id, result.Result.STARNETDNA.Id, result.Result.STARNETDNA.Version, selectedRuntime.STARNETDNA.Id, selectedRuntime.STARNETDNA.Version, providerType);

        //        if (removeResult != null && removeResult.Result != null && !removeResult.IsError)
        //            CLIEngine.ShowSuccessMessage($"Runtime '{selectedRuntime.Name}' removed from {STARNETManager.STARNETHolonUIName} '{result.Result.STARNETDNA.Name}'.");
        //        else
        //            CLIEngine.ShowErrorMessage($"Failed to remove runtime '{selectedRuntime.Name}' from {STARNETManager.STARNETHolonUIName} '{result.Result.STARNETDNA.Name}'. Error: {removeResult.Message}");
        //    }
        //    else
        //    {
        //        Console.WriteLine("");
        //        CLIEngine.ShowErrorMessage($"An error occured loading the {STARNETManager.STARNETHolonUIName} for id/name {idOrNameOfParent}. Reason: {result.Message}");
        //    }
        //}

    }
}
