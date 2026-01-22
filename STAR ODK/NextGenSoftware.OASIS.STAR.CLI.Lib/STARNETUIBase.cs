using System;
using System.Diagnostics;
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
    public class STARNETUIBase<T1, T2, T3, T4>
        where T1 : ISTARNETHolon, new()
        where T2 : IDownloadedSTARNETHolon, new()
        where T3 : IInstalledSTARNETHolon, new()
        where T4 : ISTARNETDNA, new()
    {
        protected const int DEFAULT_FIELD_LENGTH = 35;

        public virtual ISTARNETManagerBase<T1, T2, T3, T4> STARNETManager { get; set; }
        public virtual bool IsInit { get; set; }
        public virtual string CreateHeader { get; set; }
        public virtual List<string> CreateIntroParagraphs { get; set; }
        public virtual string SourcePath { get; set; }
        public virtual string SourceSTARDNAKey { get; set; }
        public virtual string PublishedPath { get; set; }
        public virtual string PublishedSTARDNAKey { get; set; }
        public virtual string DownloadedPath { get; set; }
        public virtual string DownloadSTARDNAKey { get; set; }
        public virtual string InstalledPath { get; set; }
        public virtual string InstalledSTARDNAKey { get; set; }

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

                if (Path.IsPathRooted(SourcePath) || string.IsNullOrEmpty(STAR.STARDNA.BaseSTARNETPath))
                    holonPath = SourcePath;
                else
                    holonPath = Path.Combine(STAR.STARDNA.BaseSTARNETPath, SourcePath);

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

                if (CLIEngine.GetConfirmation($"Do you wish to update the {STARNETManager.STARNETHolonUIName} Category? (currently is {Enum.GetName(STARNETManager.STARNETCategory.GetType(), Convert.ToInt32(loadResult.Result.STARNETDNA.STARNETCategory))})."))
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

                if (editLaunchTarget && CLIEngine.GetConfirmation(string.Concat("Do you wish to update the launch target? (currently is ", string.IsNullOrEmpty(loadResult.Result.STARNETDNA.LaunchTarget) ? loadResult.Result.STARNETDNA.LaunchTarget : "None", ".)")))
                {
                    Console.WriteLine("");
                    loadResult.Result.STARNETDNA.LaunchTarget = CLIEngine.GetValidInput($"What is the new launch target of the {STARNETManager.STARNETHolonUIName}?");
                    changesMade = true;
                }
                else
                    Console.WriteLine("");

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
                        object dependencyInstallModeObj = CLIEngine.GetValidInputForEnum($"Do you wish to install the dependency in the root of the {STARNETManager.STARNETHolonUIName}, in the Dependencies sub-folder (Nested)? (Recommended) or would you like to flatten the dependencies so all sub-dependencies are placed in the same level?", typeof(DependencyInstallMode));

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
                object depType = CLIEngine.GetValidInputForEnum("What type of dependency do you wish to remove?", typeof(DependencyType));
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

        public async Task<OASISResult<ISTARNETDNA>> AddDependenciesAsync(ISTARNETDNA STARNETDNA, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<ISTARNETDNA> result = new OASISResult<ISTARNETDNA>();
            DependencyType dependencyTypeEnum = DependencyType.OAPP;

            //Console.WriteLine("");

            if (CLIEngine.GetConfirmation($"Do you wish to add any dependencies to the {STARNETDNA.STARNETHolonType} with name '{STARNETDNA.Name}'? (you do not need to add the OASIS or STAR runtimes, they are added automatically)"))
            {
                do
                {
                    Console.WriteLine("");
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


                    Guid dependencyId = Guid.Empty;
                    //Console.WriteLine("");
                    if (!CLIEngine.GetConfirmation($"Does the {Enum.GetName(typeof(DependencyType), dependencyTypeEnum)} already exist?"))
                    {
                        Console.WriteLine("");

                        switch (dependencyTypeEnum)
                        {
                            case DependencyType.OAPP:
                                {
                                    OASISResult<OAPP> createResult = await STARCLI.OAPPs.CreateAsync(null, providerType: providerType);

                                    if (createResult != null && createResult.Result != null && !createResult.IsError)
                                        dependencyId = createResult.Result.Id;
                                }
                                break;

                            case DependencyType.Template:
                                {
                                    OASISResult<OAPPTemplate> createResult = await STARCLI.OAPPTemplates.CreateAsync(null, providerType: providerType);

                                    if (createResult != null && createResult.Result != null && !createResult.IsError)
                                        dependencyId = createResult.Result.Id;
                                }
                                break;

                            case DependencyType.Zome:
                                {
                                    OASISResult<STARZome> createResult = await STARCLI.Zomes.CreateAsync(null, providerType: providerType);

                                    if (createResult != null && createResult.Result != null && !createResult.IsError)
                                        dependencyId = createResult.Result.Id;
                                }
                                break;

                            //TODO: Implement same pattern above to the other dependency types below.
                            case DependencyType.NFT:
                                {
                                    OASISResult<STARNFT> createResult = await STARCLI.NFTs.CreateAsync(null, providerType: providerType);
                                    if (createResult != null && createResult.Result != null && !createResult.IsError)
                                        dependencyId = createResult.Result.Id;
                                }
                                break;

                            case DependencyType.GeoNFT:
                                {
                                    OASISResult<STARGeoNFT> createResult = await STARCLI.GeoNFTs.CreateAsync(null, providerType: providerType);
                                    if (createResult != null && createResult.Result != null && !createResult.IsError)
                                        dependencyId = createResult.Result.Id;
                                }
                                break;

                            case DependencyType.NFTCollection:
                                {
                                    OASISResult<STARNFTCollection> createResult = await STARCLI.NFTCollections.CreateAsync(null, providerType: providerType);
                                    if (createResult != null && createResult.Result != null && !createResult.IsError)
                                        dependencyId = createResult.Result.Id;
                                }
                                break;

                            case DependencyType.GeoNFTCollection:
                                {
                                    OASISResult<STARGeoNFTCollection> createResult = await STARCLI.GeoNFTCollections.CreateAsync(null, providerType: providerType);
                                    if (createResult != null && createResult.Result != null && !createResult.IsError)
                                        dependencyId = createResult.Result.Id;
                                }
                                break;

                            case DependencyType.Chapter:
                                {
                                    OASISResult<Chapter> createResult = await STARCLI.Chapters.CreateAsync(null, providerType: providerType);
                                    if (createResult != null && createResult.Result != null && !createResult.IsError)
                                        dependencyId = createResult.Result.Id;
                                }
                                break;

                            case DependencyType.CelestialBody:
                                {
                                    OASISResult<STARCelestialBody> createResult = await STARCLI.CelestialBodies.CreateAsync(null, providerType: providerType);
                                    if (createResult != null && createResult.Result != null && !createResult.IsError)
                                        dependencyId = createResult.Result.Id;
                                }
                                break;

                            case DependencyType.CelestialBodyMetaDataDNA:
                                {
                                    OASISResult<CelestialBodyMetaDataDNA> createResult = await STARCLI.CelestialBodiesMetaDataDNA.CreateAsync(null, providerType: providerType);
                                    if (createResult != null && createResult.Result != null && !createResult.IsError)
                                        dependencyId = createResult.Result.Id;
                                }
                                break;

                            case DependencyType.CelestialSpace:
                                {
                                    OASISResult<STARCelestialSpace> createResult = await STARCLI.CelestialSpaces.CreateAsync(null, providerType: providerType);
                                    if (createResult != null && createResult.Result != null && !createResult.IsError)
                                        dependencyId = createResult.Result.Id;
                                }
                                break;

                            case DependencyType.Holon:
                                {
                                    OASISResult<STARHolon> createResult = await STARCLI.Holons.CreateAsync(null, providerType: providerType);
                                    if (createResult != null && createResult.Result != null && !createResult.IsError)
                                        dependencyId = createResult.Result.Id;
                                }
                                break;

                            case DependencyType.HolonMetaDataDNA:
                                {
                                    OASISResult<HolonMetaDataDNA> createResult = await STARCLI.HolonsMetaDataDNA.CreateAsync(null, providerType: providerType);
                                    if (createResult != null && createResult.Result != null && !createResult.IsError)
                                        dependencyId = createResult.Result.Id;
                                }
                                break;

                            case DependencyType.InventoryItem:
                                {
                                    OASISResult<InventoryItem> createResult = await STARCLI.InventoryItems.CreateAsync(null, providerType: providerType);
                                    if (createResult != null && createResult.Result != null && !createResult.IsError)
                                        dependencyId = createResult.Result.Id;
                                }
                                break;

                            case DependencyType.Library:
                                {
                                    OASISResult<Library> createResult = await STARCLI.Libs.CreateAsync(null, providerType: providerType);
                                    if (createResult != null && createResult.Result != null && !createResult.IsError)
                                        dependencyId = createResult.Result.Id;
                                }
                                break;

                            case DependencyType.Mission:
                                {
                                    OASISResult<Mission> createResult = await STARCLI.Missions.CreateAsync(null, providerType: providerType);
                                    if (createResult != null && createResult.Result != null && !createResult.IsError)
                                        dependencyId = createResult.Result.Id;
                                }
                                break;

                            case DependencyType.Quest:
                                {
                                    OASISResult<Quest> createResult = await STARCLI.Quests.CreateAsync(null, providerType: providerType);
                                    if (createResult != null && createResult.Result != null && !createResult.IsError)
                                        dependencyId = createResult.Result.Id;
                                }
                                break;

                            case DependencyType.Runtime:
                                {
                                    OASISResult<Runtime> createResult = await STARCLI.Runtimes.CreateAsync(null, providerType: providerType);
                                    if (createResult != null && createResult.Result != null && !createResult.IsError)
                                        dependencyId = createResult.Result.Id;
                                }
                                break;

                            case DependencyType.ZomeMetaDataDNA:
                                {
                                    OASISResult<ZomeMetaDataDNA> createResult = await STARCLI.ZomesMetaDataDNA.CreateAsync(null, providerType: providerType);
                                    if (createResult != null && createResult.Result != null && !createResult.IsError)
                                        dependencyId = createResult.Result.Id;
                                }
                                break;
                        }
                    }
                    //else
                    //{
                        Console.WriteLine("");
                        OASISResult<T1> addResult = await AddDependencyAsync(idOrNameOfDependency: dependencyId.ToString(), dependencyType: Enum.GetName(typeof(DependencyType), dependencyTypeEnum), parentSTARNETDNA: STARNETDNA, providerType: providerType);

                        if (addResult != null && addResult.Result != null && !addResult.IsError)
                        {
                            result.Result = STARNETDNA;
                            OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(addResult, result);
                        }
                        else
                        {
                            result.IsError = true;
                            result.Message = addResult.Message;
                            //return result;
                        }
                    //} 
                }
                while (CLIEngine.GetConfirmation($"Do you wish to add another dependency to the {STARNETDNA.STARNETHolonType} with name '{STARNETDNA.Name}'?"));
            }

            Console.WriteLine("");
            CLIEngine.ShowDivider();
            return result;
        }

        public virtual async Task<OASISResult<T1>> DeleteAsync(string idOrName = "", bool softDelete = true, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = await FindAsync("delete", idOrName, default, true, providerType: providerType);

            if (result != null && !result.IsError && result.Result != null)
            {
                if (CLIEngine.GetConfirmation($"Are you sure you wish to delete this {STARNETManager.STARNETHolonUIName}? This will also delete the {STARNETManager.STARNETHolonUIName} from the Source and Published folders and remove it from the STARNET Store (if you have already published it)"))
                {
                    Console.WriteLine("");
                    bool deleteDownload = CLIEngine.GetConfirmation($"Do you wish to also delete the correponding downloaded {STARNETManager.STARNETHolonUIName}? (if there is any)");

                    Console.WriteLine("");
                    bool deleteInstall = CLIEngine.GetConfirmation($"Do you wish to also delete the correponding installed {STARNETManager.STARNETHolonUIName}? (if there is any). This is different to uninstalling because uninstalled {STARNETManager.STARNETHolonUIName}s are still visible with the 'list uninstalled' sub-command and have the option to re-install. Whereas once it is deleted it is gone forever!");

                    Console.WriteLine("");
                    if (CLIEngine.GetConfirmation($"ARE YOU SURE YOU WITH TO PERMANENTLY DELETE THE {STARNETManager.STARNETHolonUIName}? IT WILL NOT BE POSSIBLE TO RECOVER AFTRWARDS!", ConsoleColor.Red))
                    {
                        Console.WriteLine("");
                        CLIEngine.ShowWorkingMessage($"Deleting {STARNETManager.STARNETHolonUIName}...");
                        result = await STARNETManager.DeleteAsync(STAR.BeamedInAvatar.Id, result.Result, result.Result.STARNETDNA.VersionSequence, true, deleteDownload, deleteInstall, providerType);

                        if (result != null && !result.IsError && result.Result != null)
                        {
                            result.IsDeleted = true;
                            CLIEngine.ShowSuccessMessage($"{STARNETManager.STARNETHolonUIName} Successfully Deleted.");
                        }
                        else
                            OASISErrorHandling.HandleError(ref result, $"An error occured deleting the {STARNETManager.STARNETHolonUIName}. Reason: {result.Message}");
                    }
                }
            }
            else
                OASISErrorHandling.HandleError(ref result, $"An error occured loading the {STARNETManager.STARNETHolonUIName}. Reason: {result.Message}");

            return result;
        }

        //public virtual async Task<OASISResult<T1>> PublishAsync(string sourcePath = "", bool edit = false, DefaultLaunchMode defaultLaunchMode = DefaultLaunchMode.Optional, bool askToInstallAtEnd = true, ProviderType providerType = ProviderType.Default)
        public virtual async Task<OASISResult<T1>> PublishAsync(string sourcePath = "", bool edit = false, DefaultLaunchMode defaultLaunchMode = DefaultLaunchMode.None, bool askToInstallAtEnd = true, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> publishResult = new OASISResult<T1>();
            bool generateOAPP = true;
            bool uploadOAPPToCloud = true;
            ProviderType OAPPBinaryProviderType = ProviderType.None;
            // string publishPath = "";

            OASISResult<BeginPublishResult> beginPublishResult = await BeginPublishingAsync(sourcePath, defaultLaunchMode, providerType);

            if (beginPublishResult != null && !beginPublishResult.IsError && beginPublishResult.Result != null)
            {
                Console.WriteLine("");
                bool registerOnSTARNET = CLIEngine.GetConfirmation($"Do you wish to publish to STARNET? If you select 'Y' to this question then your {STARNETManager.STARNETHolonUIName} will be published to STARNET where others will be able to find, download and install. If you select 'N' then only the .{STARNETManager.STARNETHolonFileExtention} install file will be generated on your local device, which you can distribute as you please. This file will also be generated even if you publish to STARNET.");
                Console.WriteLine("");

                if (registerOnSTARNET && !beginPublishResult.Result.SimpleWizard)
                {
                    CLIEngine.ShowMessage($"Do you wish to publish/upload the .{STARNETManager.STARNETHolonFileExtention} file to an OASIS Provider or to the cloud or both? Depending on which OASIS Provider is chosen such as IPFSOASIS there may issues such as speed, relialbility etc for such a large file. If you choose to upload to the cloud this could be faster and more reliable (but there is a limit of 5 OAPPs on the free plan and you will need to upgrade to upload more than 5 OAPPs). You may want to choose to use both to add an extra layer of redundancy (recommended).");

                    if (!CLIEngine.GetConfirmation("Do you wish to upload to the cloud?"))
                        uploadOAPPToCloud = false;

                    Console.WriteLine("");
                    if (CLIEngine.GetConfirmation("Do you wish to upload to an OASIS Provider? Make sure you select a provider that can handle large files such as IPFSOASIS, HoloOASIS etc. Also remember the OASIS Hyperdrive will only be able to auto-replicate to other providers that also support large files and are free or cost effective. By default it will NOT auto-replicate large files, you will need to manually configure this in your OASIS Profile settings."))
                    {
                        Console.WriteLine("");
                        object largeProviderTypeObject = CLIEngine.GetValidInputForEnum("What provider do you wish to publish the OAPP to? (The default is IPFSOASIS)", typeof(ProviderType));

                        if (largeProviderTypeObject != null)
                            OAPPBinaryProviderType = (ProviderType)largeProviderTypeObject;
                    }
                    else
                        Console.WriteLine("");
                }

                publishResult = await FininaliazePublishingAsync(beginPublishResult.Result, edit, registerOnSTARNET, generateOAPP, uploadOAPPToCloud, askToInstallAtEnd, providerType, OAPPBinaryProviderType);
            }
            else
                CLIEngine.ShowErrorMessage($"Error Occured: {beginPublishResult.Message}");

            return publishResult;
        }

        //protected async Task<OASISResult<BeginPublishResult>> BeginPublishingAsync(string sourcePath, DefaultLaunchMode defaultLaunchMode = DefaultLaunchMode.Optional, ProviderType providerType = ProviderType.Default)
        protected async Task<OASISResult<BeginPublishResult>> BeginPublishingAsync(string sourcePath, DefaultLaunchMode defaultLaunchMode = DefaultLaunchMode.None, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<BeginPublishResult> result = new OASISResult<BeginPublishResult>(new BeginPublishResult());
            bool generateOAPP = true;
            bool uploadOAPPToCloud = true;
            ProviderType OAPPBinaryProviderType = ProviderType.None;
            string launchTargetQuestion = $"What is the relative path (from the root of the path given above, e.g bin\\launch.exe) to the launch target for the {STARNETManager.STARNETHolonUIName}? (This could be the exe or batch file for a desktop or console app, or the index.html page for a website, etc)";
            result.Result.SimpleWizard = CLIEngine.GetConfirmation("Do you wish to launch the Simple or Advanced Wizard? The Simple Wizard will use defaults (recommended) but the Advanced Wizard will allow greater control and customisation. Press 'Y' for Simple or 'N' for Advanced.");

            if (string.IsNullOrEmpty(sourcePath))
            {
                Console.WriteLine("");
                //launchTargetQuestion = $"What is the relative path (from the root of the path given above, e.g bin\\launch.exe) to the launch target for the {STARNETManager.STARNETHolonUIName}? (This could be the exe or batch file for a desktop or console app, or the index.html page for a website, etc)";
                sourcePath = CLIEngine.GetValidFolder($"What is the full path to the {STARNETManager.STARNETHolonUIName} directory?", false);
            }

            result.Result.SourcePath = sourcePath;
            OASISResult<STARNETDNA> DNAResult = await STARNETManager.ReadDNAFromSourceOrInstallFolderAsync<STARNETDNA>(sourcePath);

            if (DNAResult != null && DNAResult.Result != null && !DNAResult.IsError)
            {
                OASISResult<T1> loadResult = await STARNETManager.LoadAsync(STAR.BeamedInAvatar.Id, DNAResult.Result.Id, 0, providerType: providerType);

                if (loadResult != null && loadResult.Result != null && !loadResult.IsError)
                {
                    loadResult.Result.STARNETDNA.Version = DNAResult.Result.Version; //Update the version from the JSON file.
                    await ShowAsync(loadResult.Result);

                    if (!CLIEngine.GetConfirmation($"Is this the correct {STARNETManager.STARNETHolonUIName} you wish to publish?"))
                    {
                        Console.WriteLine("");
                        result.Message = "User Exited";
                        result.IsError = true;
                        return result;
                    }

                    result.Result.LaunchTarget = loadResult.Result.STARNETDNA.LaunchTarget;
                    Console.WriteLine("");

                    //object templateType = Enum.Parse(STARNETManager.STARNETHolonSubType, DNAResult.Result.STARNETHolonType.ToString());
                    //Type Type = (Type)templateType;

                    //switch (Type)
                    //{
                    //    case Type.Console:
                    //    case Type.WPF:
                    //    case Type.WinForms:
                    //        launchTarget = $"{DNAResult.Result.Name}.exe"; //TODO: For this line to work need to remove the namespace question so it just uses the OAPPName as the namespace. //TODO: Eventually this will be set in the  and/or can also be set when I add the command line dotnet publish integration.
                    //        break;

                    //    case Type.Blazor:
                    //    case Type.MAUI:
                    //    case Type.WebMVC:
                    //        launchTarget = $"index.html";
                    //        break;
                    //}

                    if (defaultLaunchMode != DefaultLaunchMode.None)
                    {
                        bool hasDefaultLaunchTarget = false;

                        if (defaultLaunchMode == DefaultLaunchMode.Optional)
                            hasDefaultLaunchTarget = CLIEngine.GetConfirmation($"Do you wish to set a default launch target?");

                        else if (defaultLaunchMode == DefaultLaunchMode.Mandatory)
                            hasDefaultLaunchTarget = true;

                        if (hasDefaultLaunchTarget)
                        {
                            Console.WriteLine("");
                            if (!string.IsNullOrEmpty(result.Result.LaunchTarget))
                            {
                                if (!CLIEngine.GetConfirmation($"{launchTargetQuestion} Do you wish to use the following default launch target: {result.Result.LaunchTarget}?"))
                                {
                                    Console.WriteLine("");
                                    result.Result.LaunchTarget = CLIEngine.GetValidFile("What launch target do you wish to use? ", sourcePath);
                                }
                                else
                                    result.Result.LaunchTarget = Path.Combine(sourcePath, result.Result.LaunchTarget);
                            }
                            else
                                result.Result.LaunchTarget = CLIEngine.GetValidFile(launchTargetQuestion, sourcePath);
                        }
                    }

                    if (!result.Result.SimpleWizard && STARNETManager.GetNumberOfDependendies(DNAResult.Result) > 0 && CLIEngine.GetConfirmation($"Do you wish to embed any of the dependencies? It is not recommended because will increase the storage space/cost & upload/download time. If you choose 'N' then they will be automatically downloaded and installed when someone installs your {STARNETManager.STARNETHolonUIName}. Only choose 'Y' if you want them embedded in case there is an issue downloading/installing them seperatley later (unlikely) or if you want the {STARNETManager.STARNETHolonUIName} to be fully self-contained with no external dependencies (useful if you wish to install it offline from the {STARNETManager.STARNETHolonFileExtention} file)."))
                    {
                        if (DNAResult.Result.Dependencies.Templates.Count > 0)
                            result.Result.EmbedTemplates = CLIEngine.GetConfirmation("Do you wish to embed the sub-templates?");

                        if (DNAResult.Result.Dependencies.Runtimes.Count > 0)
                            result.Result.EmbedRuntimes = CLIEngine.GetConfirmation("Do you wish to embed the runtimes?");

                        if (DNAResult.Result.Dependencies.Libraries.Count > 0)
                            result.Result.EmbedLibs = CLIEngine.GetConfirmation("Do you wish to embed the libraries?");

                        //TODO: Add rest here!
                    }
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"The {STARNETManager.STARNETHolonUIName} could not be found for id {DNAResult.Result.Id} found in the {STARNETManager.STARNETDNAFileName} file. It could be corrupt, the id could be wrong or you may not have permission, please check and try again, or create a new {STARNETManager.STARNETHolonUIName}.");
            }
            else
                OASISErrorHandling.HandleError(ref result, $"The {STARNETManager.STARNETDNAFileName} file could not be found! Please ensure it is in the folder you specified.");

            return result;
        }

        protected async Task<OASISResult<T1>> FininaliazePublishingAsync(BeginPublishResult pubParams, bool edit, bool registerOnSTARNET, bool generateOAPP, bool uploadOAPPToCloud, bool askToInstallAtEnd = true, ProviderType providerType = ProviderType.Default, ProviderType OAPPBinaryProviderType = ProviderType.Default)
        {
            OASISResult<T1> publishResult = new OASISResult<T1>();
            //OASISResult<string> pubPathResult = await GetPublishPathAsync(pubParams, edit, registerOnSTARNET, generateOAPP, uploadOAPPToCloud, providerType, OAPPBinaryProviderType);
            OASISResult<string> pubPathResult = await GetPublishPathAsync(pubParams.SourcePath, pubParams.SimpleWizard, edit, registerOnSTARNET, generateOAPP, uploadOAPPToCloud, providerType, OAPPBinaryProviderType);

            if (pubPathResult != null && !string.IsNullOrEmpty(pubPathResult.Result) && !pubPathResult.IsError)
            {
                publishResult = await STARNETManager.PublishAsync(STAR.BeamedInAvatar.Id, pubParams.SourcePath, pubParams.LaunchTarget, pubPathResult.Result, edit, registerOnSTARNET, generateOAPP, uploadOAPPToCloud, providerType, OAPPBinaryProviderType, pubParams.EmbedRuntimes, pubParams.EmbedLibs, pubParams.EmbedTemplates);
                await PostFininaliazePublishingAsync(publishResult, pubParams.SourcePath, askToInstallAtEnd, providerType);
            }
            else
                OASISErrorHandling.HandleError(ref publishResult, $"Error occured in STARNETUIBase.FininaliazePublishingAsync calling PreFininaliazePublishingAsync. Reason: {pubPathResult.Message}");

            return publishResult;
        }

        //protected async Task<OASISResult<string>> GetPublishPathAsync(BeginPublishResult pubParams, bool edit, bool registerOnSTARNET, bool generateOAPP, bool uploadOAPPToCloud, ProviderType providerType, ProviderType OAPPBinaryProviderType)
        protected async Task<OASISResult<string>> GetPublishPathAsync(string sourcePath, bool simpleWizard, bool edit, bool registerOnSTARNET, bool generateOAPP, bool uploadOAPPToCloud, ProviderType providerType, ProviderType OAPPBinaryProviderType)
        {
            OASISResult<string> result = new OASISResult<string>();
            string publishPath = "";

            if (Path.IsPathRooted(PublishedPath) || string.IsNullOrEmpty(STAR.STARDNA.BaseSTARNETPath))
                publishPath = PublishedPath;
            else
                publishPath = Path.Combine(STAR.STARDNA.BaseSTARNETPath, PublishedPath);

            if (!simpleWizard)
            {
                if (!CLIEngine.GetConfirmation($"Do you wish to publish the {STARNETManager.STARNETHolonUIName} to the default publish folder defined in the STARDNA as {PublishedSTARDNAKey} : {publishPath}?"))
                {
                    Console.WriteLine("");

                    if (CLIEngine.GetConfirmation($"Do you wish to publish the {STARNETManager.STARNETHolonUIName} to: {Path.Combine(sourcePath, "Published")}?"))
                        publishPath = Path.Combine(sourcePath, "Published");
                    else
                    {
                        Console.WriteLine("");
                        publishPath = CLIEngine.GetValidFolder($"Where do you wish to publish the {STARNETManager.STARNETHolonUIName}?", true);
                    }
                }
            }

            publishPath = new DirectoryInfo(publishPath).FullName;

            //Console.WriteLine("");
            CLIEngine.ShowWorkingMessage($"Publishing {STARNETManager.STARNETHolonUIName}...");
            result.Result = publishPath;
            return result;
        }

        protected async Task<OASISResult<T1>> PostFininaliazePublishingAsync(OASISResult<T1> publishResult, string sourcePath, bool askToInstallAtEnd = true, ProviderType providerType = ProviderType.Default)
        {

            if (publishResult != null && !publishResult.IsError && publishResult.Result != null)
            {
                await ShowAsync(publishResult.Result);

                if (askToInstallAtEnd && CLIEngine.GetConfirmation($"Do you wish to install the {STARNETManager.STARNETHolonUIName} now?"))
                    await DownloadAndInstallAsync(publishResult.Result.STARNETDNA.Id.ToString(), InstallMode.DownloadAndInstall, providerType);

                Console.WriteLine("");
            }
            else
            {
                //if (publishResult.Message.Contains("Please make sure you increment the version"))
                if (publishResult.Message.Contains(STARNETManager.STARNETDNAFileName))
                {
                    if (CLIEngine.GetConfirmation($"Do you wish to open the {STARNETManager.STARNETDNAFileName} file now?"))
                        Process.Start("explorer.exe", Path.Combine(sourcePath, STARNETManager.STARNETDNAFileName));
                }
                else
                    CLIEngine.ShowErrorMessage($"An error occured publishing the {STARNETManager.STARNETHolonUIName}. Reason: {publishResult.Message}");

                Console.WriteLine("");
            }

            return publishResult;
        }


        public virtual async Task UnpublishAsync(string idOrName = "", ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = await FindAsync("unpublish", idOrName, default, true, providerType: providerType);

            if (result != null && !result.IsError && result.Result != null)
            {
                OASISResult<T1> unpublishResult = await STARNETManager.UnpublishAsync(STAR.BeamedInAvatar.Id, result.Result, providerType);

                if (unpublishResult != null && !unpublishResult.IsError && unpublishResult.Result != null)
                {
                    CLIEngine.ShowSuccessMessage($"{STARNETManager.STARNETHolonUIName} Successfully Unpublished.");
                    await ShowAsync(result.Result);
                }
                else
                    CLIEngine.ShowErrorMessage($"An error occured unpublishing the {STARNETManager.STARNETHolonUIName}. Reason: {unpublishResult.Message}");
            }
        }

        public virtual async Task RepublishAsync(string idOrName = "", ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = await FindAsync("republish", idOrName, default, true, providerType: providerType);

            if (result != null && !result.IsError && result.Result != null)
            {
                OASISResult<T1> republishResult = await STARNETManager.RepublishAsync(STAR.BeamedInAvatar.Id, result.Result, providerType);

                if (republishResult != null && !republishResult.IsError && republishResult.Result != null)
                {
                    CLIEngine.ShowSuccessMessage($"{STARNETManager.STARNETHolonUIName} Successfully Republished.");
                    await ShowAsync(result.Result);
                }
                else
                    CLIEngine.ShowErrorMessage($"An error occured unpublishing the {STARNETManager.STARNETHolonUIName}. Reason: {republishResult.Message}");
            }
        }

        public virtual async Task ActivateAsync(string idOrName = "", ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = await FindAsync("activate", idOrName, default, true, providerType: providerType);

            if (result != null && !result.IsError && result.Result != null)
            {
                if (result.MetaData != null && result.MetaData.ContainsKey("Active") && result.MetaData["Active"] != null && result.MetaData["Active"] == "1")
                {
                    OASISResult<T1> activateResult = await STARNETManager.ActivateAsync(STAR.BeamedInAvatar.Id, result.Result, providerType);

                    if (activateResult != null && !activateResult.IsError && activateResult.Result != null)
                    {
                        CLIEngine.ShowSuccessMessage($"{STARNETManager.STARNETHolonUIName} Successfully Activated.");
                        await ShowAsync(result.Result);
                    }
                    else
                        CLIEngine.ShowErrorMessage($"An error occured activating the {STARNETManager.STARNETHolonUIName}. Reason: {activateResult.Message}");
                }
                else
                    CLIEngine.ShowErrorMessage($"The {STARNETManager.STARNETHolonUIName} is already activated!");
            }
        }

        public virtual async Task DeactivateAsync(string idOrName = "", ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = await FindAsync("deactivate", idOrName, default, true, providerType: providerType);

            if (result != null && !result.IsError && result.Result != null)
            {
                if (result.MetaData != null && result.MetaData.ContainsKey("Active") && result.MetaData["Active"] != null && result.MetaData["Active"] == "0")
                {
                    OASISResult<T1> deactivateResult = await STARNETManager.DeactivateAsync(STAR.BeamedInAvatar.Id, result.Result, providerType);

                    if (deactivateResult != null && !deactivateResult.IsError && deactivateResult.Result != null)
                    {
                        CLIEngine.ShowSuccessMessage($"{STARNETManager.STARNETHolonUIName} Successfully Deactivated.");
                        await ShowAsync(result.Result);
                    }
                    else
                        CLIEngine.ShowErrorMessage($"An error occured deactivating the {STARNETManager.STARNETHolonUIName}. Reason: {deactivateResult.Message}");
                }
                else
                    CLIEngine.ShowErrorMessage($"The {STARNETManager.STARNETHolonUIName} is already deactivated!");
            }
        }

        public virtual async Task<OASISResult<T3>> DownloadAndInstallAsync(string idOrName = "", InstallMode installMode = InstallMode.DownloadAndInstall, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T3> installResult = new OASISResult<T3>();
            string downloadPath = "";
            string installPath = "";
            bool simpleWizard = false;
            string operation = "install";

            if (installMode == InstallMode.DownloadOnly)
                operation = "download";

            if (Path.IsPathRooted(DownloadedPath) || string.IsNullOrEmpty(STAR.STARDNA.BaseSTARNETPath))
                downloadPath = DownloadedPath;
            else
                downloadPath = Path.Combine(STAR.STARDNA.BaseSTARNETPath, DownloadedPath);


            if (Path.IsPathRooted(InstalledPath) || string.IsNullOrEmpty(STAR.STARDNA.BaseSTARNETPath))
                installPath = InstalledPath;
            else
                installPath = Path.Combine(STAR.STARDNA.BaseSTARNETPath, InstalledPath);

            Console.WriteLine("");

            if (CLIEngine.GetConfirmation("Do you wish to launch the Simple or Advanced Wizard? The Simple Wizard will use defaults (recommended) but the Advanced Wizard will allow greater control and customisation. Press 'Y' for Simple or 'N' for Advanced."))
                simpleWizard = true;

            if (!simpleWizard)
            {
                Console.WriteLine("");

                if (!CLIEngine.GetConfirmation($"Do you wish to download the {STARNETManager.STARNETHolonUIName} to the default download folder defined in the STARDNA as {DownloadSTARDNAKey} : {downloadPath}?"))
                {
                    Console.WriteLine("");
                    downloadPath = CLIEngine.GetValidFolder($"What is the full path to where you wish to download the {STARNETManager.STARNETHolonUIName}?", true);
                }

                downloadPath = new DirectoryInfo(downloadPath).FullName;

                if (installMode != InstallMode.DownloadAndInstall)
                {
                    Console.WriteLine("");

                    if (!CLIEngine.GetConfirmation($"Do you wish to install the {STARNETManager.STARNETHolonUIName} to the default install folder defined in the STARDNA as {InstalledSTARDNAKey} : {installPath}?"))
                    {
                        Console.WriteLine("");
                        installPath = CLIEngine.GetValidFolder($"What is the full path to where you wish to install the {STARNETManager.STARNETHolonUIName}?", true);
                    }

                    installPath = new DirectoryInfo(installPath).FullName;
                }
            }

            if (!string.IsNullOrEmpty(idOrName))
            {
                Console.WriteLine("");
                OASISResult<T1> result = await FindForProviderAsync(operation, idOrName, false, false, true, providerType);

                if (result != null && result.Result != null && !result.IsError)
                {
                    if (result.MetaData != null && result.MetaData.ContainsKey("Reinstall") && !string.IsNullOrEmpty(result.MetaData["Reinstall"]) && result.MetaData["Reinstall"] == "1" && installMode == InstallMode.DownloadAndInstall)
                        installMode = InstallMode.DownloadAndReInstall;

                    installResult = await CheckIfInstalledAndInstallAsync(result.Result, downloadPath, installPath, installMode, "", providerType);
                }
            }
            else
            {
                Console.WriteLine("");
                if (installMode != InstallMode.DownloadOnly && CLIEngine.GetConfirmation($"Do you wish to install the {STARNETManager.STARNETHolonUIName} from a local .{STARNETManager.STARNETDNAFileName} file or from STARNET? Press 'Y' for local .{STARNETManager.STARNETDNAFileName} file or 'N' for STARNET."))
                {
                    Console.WriteLine("");
                    string oappPath = CLIEngine.GetValidFile($"What is the full path to the .{STARNETManager.STARNETDNAFileName} file?");

                    if (oappPath == "exit")
                        return installResult;

                    OASISResult<ISTARNETDNA> starHolonDNAResult = await STARNETManager.ReadDNAFromPublishedFileAsync<ISTARNETDNA>(oappPath);

                    if (starHolonDNAResult != null && starHolonDNAResult.Result != null && !starHolonDNAResult.IsError)
                    {
                        OASISResult<T1> starHolonResult = await STARNETManager.LoadAsync(STAR.BeamedInAvatar.Id, starHolonDNAResult.Result.Id, 0, providerType: providerType);

                        if (starHolonResult != null && starHolonResult.Result != null && !starHolonResult.IsError)
                        {
                            installMode = InstallMode.InstallOnly;

                            if (starHolonResult.MetaData != null && starHolonResult.MetaData.ContainsKey("Reinstall") && !string.IsNullOrEmpty(starHolonResult.MetaData["Reinstall"]) && starHolonResult.MetaData["Reinstall"] == "1")
                                installMode = InstallMode.ReInstall;

                            installResult = await CheckIfInstalledAndInstallAsync(starHolonResult.Result, downloadPath, installPath, installMode, oappPath, providerType);
                        }
                        else
                            CLIEngine.ShowErrorMessage($"The {STARNETManager.STARNETHolonUIName} could not be found for id {starHolonDNAResult.Result.Id} found in the STARNETDNA.json file. It could be corrupt or the id could be wrong, please check and try again, or create a new {STARNETManager.STARNETHolonUIName}.");
                    }
                    else
                        CLIEngine.ShowErrorMessage($"The {STARNETManager.STARNETHolonUIName} could not be found or is not valid! Please ensure it is in the folder you specified.");
                }
                else
                {
                    Console.WriteLine("");
                    OASISResult<T1> result = await FindForProviderAsync(operation, "", false, false, true, providerType);

                    if (result != null && result.Result != null && !result.IsError)
                    {
                        if (result.MetaData != null && result.MetaData.ContainsKey("Reinstall") && !string.IsNullOrEmpty(result.MetaData["Reinstall"]) && result.MetaData["Reinstall"] == "1" && installMode == InstallMode.DownloadAndInstall)
                            installMode = InstallMode.DownloadAndReInstall;

                        installResult = await CheckIfInstalledAndInstallAsync(result.Result, downloadPath, installPath, installMode, "", providerType);
                    }
                    else
                    {
                        installResult.Message = result.Message;
                        installResult.IsError = true;
                    }
                }
            }

            if (installResult != null)
            {
                if (!installResult.IsError && installResult.Result != null)
                {
                    ShowInstalled(installResult.Result);

                    if (CLIEngine.GetConfirmation($"Do you wish to open the folder to the {STARNETManager.STARNETHolonUIName} now?"))
                        STARNETManager.OpenSTARNETHolonFolder(STAR.BeamedInAvatar.Id, installResult.Result);
                    //await STARNETManager.OpenSTARNETHolonFolderAsync(STAR.BeamedInAvatar.Id, installResult.Result.STARNETDNA.Id, installResult.Result.STARNETDNA.Version);
                }
                else
                    CLIEngine.ShowErrorMessage($"Error {operation}ing {STARNETManager.STARNETHolonUIName}. Reason: {installResult.Message}");
            }
            else
                CLIEngine.ShowErrorMessage($"Error {operation}ing {STARNETManager.STARNETHolonUIName}. Reason: Unknown error occured!");

            Console.WriteLine("");
            return installResult;
        }

        public virtual OASISResult<T3> DownloadAndInstall(string idOrName = "", ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T3> installResult = new OASISResult<T3>();
            string downloadPath = "";
            string installPath = "";

            if (Path.IsPathRooted(DownloadedPath) || string.IsNullOrEmpty(STAR.STARDNA.BaseSTARNETPath))
                downloadPath = SourcePath;
            else
                downloadPath = Path.Combine(STAR.STARDNA.BaseSTARNETPath, DownloadedPath);


            if (Path.IsPathRooted(InstalledPath) || string.IsNullOrEmpty(STAR.STARDNA.BaseSTARNETPath))
                installPath = SourcePath;
            else
                installPath = Path.Combine(STAR.STARDNA.BaseSTARNETPath, InstalledPath);

            Console.WriteLine("");

            if (!CLIEngine.GetConfirmation($"Do you wish to download the {STARNETManager.STARNETHolonUIName} to the default download folder defined in the STARDNA as {DownloadSTARDNAKey} : {downloadPath}?"))
            {
                Console.WriteLine("");
                downloadPath = CLIEngine.GetValidFolder($"What is the full path to where you wish to download the {STARNETManager.STARNETHolonUIName}?", true);
            }

            downloadPath = new DirectoryInfo(downloadPath).FullName;

            Console.WriteLine("");

            if (!CLIEngine.GetConfirmation($"Do you wish to install the {STARNETManager.STARNETHolonUIName} to the default install folder defined in the STARDNA as {DownloadSTARDNAKey} : {installPath}?"))
            {
                Console.WriteLine("");
                installPath = CLIEngine.GetValidFolder($"What is the full path to where you wish to install the {STARNETManager.STARNETHolonUIName}?", true);
            }

            installPath = new DirectoryInfo(installPath).FullName;

            if (!string.IsNullOrEmpty(idOrName))
            {
                Console.WriteLine("");
                OASISResult<T1> result = FindForProvider("install", idOrName, false, false, true, providerType);

                if (result != null && result.Result != null && !result.IsError)
                    installResult = STARNETManager.DownloadAndInstall(STAR.BeamedInAvatar.Id, result.Result, installPath, downloadPath, true, false, providerType);
            }
            else
            {
                Console.WriteLine("");
                if (CLIEngine.GetConfirmation($"Do you wish to install the {STARNETManager.STARNETHolonUIName} from a local .{STARNETManager.STARNETDNAFileName} file or from STARNET? Press 'Y' for local .{STARNETManager.STARNETDNAFileName} file or 'N' for STARNET."))
                {
                    Console.WriteLine("");
                    string oappPath = CLIEngine.GetValidFile($"What is the full path to the {STARNETManager.STARNETDNAFileName} file?");

                    if (oappPath == "exit")
                        return installResult;

                    installResult = STARNETManager.Install(STAR.BeamedInAvatar.Id, oappPath, installPath, true, null, false, providerType);
                }
                else
                {
                    Console.WriteLine("");
                    CLIEngine.ShowWorkingMessage($"Loading {STARNETManager.STARNETHolonUIName}s...");
                    OASISResult<IEnumerable<T1>> starHolonsResult = ListAll();

                    if (starHolonsResult != null && starHolonsResult.Result != null && !starHolonsResult.IsError && starHolonsResult.Result.Count() > 0)
                    {
                        OASISResult<T1> result = FindForProvider("", "install", false, false, true, providerType);

                        if (result != null && result.Result != null && !result.IsError)
                            installResult = STARNETManager.DownloadAndInstall(STAR.BeamedInAvatar.Id, result.Result, installPath, downloadPath, true, false, providerType);
                        else
                        {
                            installResult.Message = result.Message;
                            installResult.IsError = true;
                        }
                    }
                    else
                    {
                        installResult.Message = $"No {STARNETManager.STARNETHolonUIName}s found to install.";
                        installResult.IsError = true;
                    }
                }
            }

            if (installResult != null)
            {
                if (!installResult.IsError && installResult.Result != null)
                {
                    ShowInstalled(installResult.Result);

                    if (CLIEngine.GetConfirmation($"Do you wish to open the folder to the {STARNETManager.STARNETHolonUIName} now?"))
                        STARNETManager.OpenSTARNETHolonFolder(STAR.BeamedInAvatar.Id, installResult.Result);
                }
                else
                    CLIEngine.ShowErrorMessage($"Error installing {STARNETManager.STARNETHolonUIName}. Reason: {installResult.Message}");
            }
            else
                CLIEngine.ShowErrorMessage($"Error installing {STARNETManager.STARNETHolonUIName}. Reason: Unknown error occured!");

            Console.WriteLine("");
            return installResult;
        }

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

        public virtual async Task<OASISResult<IEnumerable<T1>>> ListAllDeactivatedForBeamedInAvatarAsync(ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<T1>> result = new OASISResult<IEnumerable<T1>>();

            if (STAR.BeamedInAvatar != null)
            {
                Console.WriteLine("");
                CLIEngine.ShowWorkingMessage($"Loading Deactivated {STARNETManager.STARNETHolonUIName}'s...");
                result = await STARNETManager.ListDeactivatedAsync(STAR.BeamedInAvatar.AvatarId);
                ListStarHolons(result, true);

                if (result != null && !result.IsError && result.Result != null && result.Result.Count() > 0 && CLIEngine.GetConfirmation("Would you like to reactivate any of the above?"))
                {
                    int number = 0;

                    do
                    {
                        Console.WriteLine("");
                        number = CLIEngine.GetValidInputForInt("What number do you wish to reactivate?");

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
                            OASISResult<T1> activateResult = await STARNETManager.ActivateAsync(STAR.BeamedInAvatar.Id, template, providerType);

                            if (activateResult != null && !activateResult.IsError && activateResult.Result != null)
                            {
                                await ShowAsync(activateResult.Result);
                                CLIEngine.ShowSuccessMessage($"{STARNETManager.STARNETHolonUIName} Successfully Reactivated");
                            }
                            else
                                CLIEngine.ShowErrorMessage($"An error occured reactivating the {STARNETManager.STARNETHolonUIName}. Reason: {activateResult.Message}");
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

        public virtual async Task SearchAsync(string searchTerm = "", Guid parentId = default, bool showAllVersions = false, bool showForAllAvatars = true, ProviderType providerType = ProviderType.Default)
        {
            if (string.IsNullOrEmpty(searchTerm) || searchTerm == "forallavatars" || searchTerm == "forallavatars")
            {
                //Console.WriteLine("");
                searchTerm = CLIEngine.GetValidInput($"What is the name of the {STARNETManager.STARNETHolonUIName} you wish to search for?");
            }

            Console.WriteLine("");
            CLIEngine.ShowWorkingMessage($"Searching {STARNETManager.STARNETHolonUIName}'s...");
            ListStarHolons(await STARNETManager.SearchAsync<T1>(STAR.BeamedInAvatar.Id, searchTerm, parentId, null, MetaKeyValuePairMatchMode.All, !showForAllAvatars, showAllVersions, 0, providerType));
        }

        public virtual async Task ShowAsync(string idOrName = "", bool showDetailed = false, ProviderType providerType = ProviderType.Default)
        {
            if (idOrName.ToLower() == "detailed")
            {
                idOrName = "";
                showDetailed = true;
            }

            OASISResult<T1> result = await FindAsync("view", idOrName, default, true, providerType: providerType);

            //if (result != null && !result.IsError && result.Result != null)
            //    Show(result.Result, showDetailedInfo: showDetailed);
            //else
            //    CLIEngine.ShowErrorMessage($"An error occured loading the {STARNETManager.STARNETHolonUIName}. Reason: {result.Message}");
        }

        public virtual async Task ShowAsync<T>(T starHolon, bool showHeader = true, bool showFooter = true, bool showNumbers = false, int number = 0, bool showDetailedInfo = false, int displayFieldLength = DEFAULT_FIELD_LENGTH, object customData = null) where T : ISTARNETHolon
        {
            if (DisplayFieldLength > displayFieldLength)
                displayFieldLength = DisplayFieldLength;

            if (showHeader)
                CLIEngine.ShowDivider();

            Console.WriteLine("");

            if (showNumbers)
                CLIEngine.ShowMessage(string.Concat("Number:".PadRight(displayFieldLength), number), false);

            CLIEngine.ShowMessage(string.Concat($"Id:".PadRight(displayFieldLength), starHolon.STARNETDNA.Id != Guid.Empty ? starHolon.STARNETDNA.Id : "None"), false);
            CLIEngine.ShowMessage(string.Concat($"Name:".PadRight(displayFieldLength), !string.IsNullOrEmpty(starHolon.STARNETDNA.Name) ? starHolon.STARNETDNA.Name : "None"), false);
            CLIEngine.ShowMessage(string.Concat($"Description:".PadRight(displayFieldLength), !string.IsNullOrEmpty(starHolon.STARNETDNA.Description) ? starHolon.STARNETDNA.Description : "None"), false);
            CLIEngine.ShowMessage(string.Concat($"Type:".PadRight(displayFieldLength), starHolon.STARNETDNA.STARNETHolonType), false);
            CLIEngine.ShowMessage(string.Concat($"Category:".PadRight(displayFieldLength), starHolon.STARNETDNA.STARNETCategory), false);
            
            // Display Language (STARNETSubCategory) for libraries
            if (starHolon.STARNETDNA.STARNETSubCategory != null)
            {
                CLIEngine.ShowMessage(string.Concat($"Language:".PadRight(displayFieldLength), starHolon.STARNETDNA.STARNETSubCategory), false);
            }
            CLIEngine.ShowMessage(string.Concat($"Created On:".PadRight(displayFieldLength), starHolon.STARNETDNA.CreatedOn != DateTime.MinValue ? starHolon.STARNETDNA.CreatedOn.ToString() : "None"), false);
            CLIEngine.ShowMessage(string.Concat($"Created By:".PadRight(displayFieldLength), starHolon.STARNETDNA.CreatedByAvatarId != Guid.Empty ? string.Concat(starHolon.STARNETDNA.CreatedByAvatarUsername, " (", starHolon.STARNETDNA.CreatedByAvatarId.ToString(), ")") : "None"), false);
            CLIEngine.ShowMessage(string.Concat($"Modified On:".PadRight(displayFieldLength), starHolon.STARNETDNA.ModifiedOn != DateTime.MinValue ? starHolon.STARNETDNA.CreatedOn.ToString() : "None"), false);
            CLIEngine.ShowMessage(string.Concat($"Modified By:".PadRight(displayFieldLength), starHolon.STARNETDNA.ModifiedByAvatarId != Guid.Empty ? string.Concat(starHolon.STARNETDNA.ModifiedByAvatarUsername, " (", starHolon.STARNETDNA.ModifiedByAvatarId.ToString(), ")") : "None"), false);
            CLIEngine.ShowMessage(string.Concat($"Source Path:".PadRight(displayFieldLength), !string.IsNullOrEmpty(starHolon.STARNETDNA.SourcePath) ? starHolon.STARNETDNA.SourcePath : "None"), false);
            CLIEngine.ShowMessage(string.Concat($"Published On:".PadRight(displayFieldLength), starHolon.STARNETDNA.PublishedOn != DateTime.MinValue ? starHolon.STARNETDNA.PublishedOn.ToString() : "None"), false);
            CLIEngine.ShowMessage(string.Concat($"Published By:".PadRight(displayFieldLength), starHolon.STARNETDNA.PublishedByAvatarId != Guid.Empty ? string.Concat(starHolon.STARNETDNA.PublishedByAvatarUsername, " (", starHolon.STARNETDNA.PublishedByAvatarId.ToString(), ")") : "None"), false);
            CLIEngine.ShowMessage(string.Concat($"Published Path:".PadRight(displayFieldLength), !string.IsNullOrEmpty(starHolon.STARNETDNA.PublishedPath) ? starHolon.STARNETDNA.PublishedPath : "None"), false);
            CLIEngine.ShowMessage(string.Concat($"Filesize:".PadRight(displayFieldLength), starHolon.STARNETDNA.FileSize.ToString()), false);
            CLIEngine.ShowMessage(string.Concat($"Published On STARNET:".PadRight(displayFieldLength), starHolon.STARNETDNA.PublishedOnSTARNET ? "True" : "False"), false);
            CLIEngine.ShowMessage(string.Concat($"Published To Cloud:".PadRight(displayFieldLength), starHolon.STARNETDNA.PublishedToCloud ? "True" : "False"), false);
            CLIEngine.ShowMessage(string.Concat($"Published To OASIS Provider:".PadRight(displayFieldLength), starHolon.STARNETDNA.PublishedProviderType), false);
            CLIEngine.ShowMessage(string.Concat($"Launch Target:".PadRight(displayFieldLength), !string.IsNullOrEmpty(starHolon.STARNETDNA.LaunchTarget) ? starHolon.STARNETDNA.LaunchTarget : "None"), false);
            CLIEngine.ShowMessage(string.Concat($"Version:".PadRight(displayFieldLength), starHolon.STARNETDNA.Version), false);
            CLIEngine.ShowMessage(string.Concat($"Version Sequence:".PadRight(displayFieldLength), starHolon.STARNETDNA.VersionSequence), false);
            CLIEngine.ShowMessage(string.Concat($"Number Of Versions:".PadRight(displayFieldLength), starHolon.STARNETDNA.NumberOfVersions), false);
            CLIEngine.ShowMessage(string.Concat($"Downloads:".PadRight(displayFieldLength), starHolon.STARNETDNA.Downloads), false);
            CLIEngine.ShowMessage(string.Concat($"Installs:".PadRight(displayFieldLength), starHolon.STARNETDNA.Installs), false);
            CLIEngine.ShowMessage(string.Concat($"Total Downloads:".PadRight(displayFieldLength), starHolon.STARNETDNA.TotalDownloads), false);
            CLIEngine.ShowMessage(string.Concat($"Total Installs:".PadRight(displayFieldLength), starHolon.STARNETDNA.TotalInstalls), false);
            CLIEngine.ShowMessage(string.Concat($"Active:".PadRight(displayFieldLength), starHolon.MetaData != null && starHolon.MetaData.ContainsKey("Active") && starHolon.MetaData["Active"] != null && starHolon.MetaData["Active"].ToString() == "1" ? "True" : "False"), false);
            CLIEngine.ShowMessage(string.Concat($"OASIS Runtime Version:".PadRight(displayFieldLength), starHolon.STARNETDNA.OASISRuntimeVersion), false);
            CLIEngine.ShowMessage(string.Concat($"OASIS API Version:".PadRight(displayFieldLength), starHolon.STARNETDNA.OASISAPIVersion), false);
            CLIEngine.ShowMessage(string.Concat($"COSMIC Version:".PadRight(displayFieldLength), starHolon.STARNETDNA.COSMICVersion), false);
            CLIEngine.ShowMessage(string.Concat($"STAR Runtime Version:".PadRight(displayFieldLength), starHolon.STARNETDNA.STARRuntimeVersion), false);
            CLIEngine.ShowMessage(string.Concat($"STAR ODK Version:".PadRight(displayFieldLength), starHolon.STARNETDNA.STARODKVersion), false);
            CLIEngine.ShowMessage(string.Concat($"STARNET Version:".PadRight(displayFieldLength), starHolon.STARNETDNA.STARNETVersion), false);
            CLIEngine.ShowMessage(string.Concat($"STAR API Version:".PadRight(displayFieldLength), starHolon.STARNETDNA.STARAPIVersion), false);
            CLIEngine.ShowMessage(string.Concat($".NET Version:".PadRight(displayFieldLength), starHolon.STARNETDNA.DotNetVersion), false);
            Console.WriteLine("");

            if (starHolon.STARNETDNA.MetaTagMappings != null)
                ShowHolonMetaTagMappings(starHolon.STARNETDNA.MetaTagMappings.MetaHolonTags, showDetailedInfo, displayFieldLength);
            else
                DisplayProperty("Holon Meta Tag Mappings", "None", displayFieldLength);

            if (starHolon.STARNETDNA.MetaTagMappings != null)
                ShowMetaTagMappings(starHolon.STARNETDNA.MetaTagMappings.MetaTags, showDetailedInfo, displayFieldLength);
            else
                DisplayProperty("Meta Tag Mappings", "None", displayFieldLength);

            ShowAllDependencies(starHolon, showDetailedInfo, displayFieldLength);
            //Console.WriteLine("");
            //ShowHolonMetaTagMappings(starHolon.STARNETDNA.MetaHolonTagMappings, showDetailedInfo, displayFieldLength);
            //ShowMetaTagMappings(starHolon.STARNETDNA.MetaTagMappings, showDetailedInfo, displayFieldLength);

            if (showFooter)
                CLIEngine.ShowDivider();
        }

        public virtual void ShowInstalled(T3 starHolon, bool showHeader = true, bool showFooter = true, bool showNumbers = false, int number = 0, bool showUninstallInfo = false, bool showDetailedInfo = false, int displayFieldLength = DEFAULT_FIELD_LENGTH)
        {
            if (DisplayFieldLength > displayFieldLength)
                displayFieldLength = DisplayFieldLength;

            //Show((T1)starHolon, showHeader, false, showNumbers, number, showDetailedInfo);
            ShowAsync(ConvertFromT3ToT1(starHolon), showHeader, false, showNumbers, number, showDetailedInfo);

            Console.WriteLine("");
            CLIEngine.ShowMessage(string.Concat($"Downloaded On:".PadRight(displayFieldLength), starHolon.DownloadedOn != DateTime.MinValue ? starHolon.DownloadedOn.ToString() : "None"), false);
            CLIEngine.ShowMessage(string.Concat($"Downloaded By:".PadRight(displayFieldLength), starHolon.DownloadedBy != Guid.Empty ? string.Concat(starHolon.DownloadedByAvatarUsername, " (", starHolon.DownloadedBy.ToString(), ")") : "None"), false);
            CLIEngine.ShowMessage(string.Concat($"Downloaded Path:".PadRight(displayFieldLength), !string.IsNullOrEmpty(starHolon.DownloadedPath) ? starHolon.DownloadedPath : "None"), false);
            CLIEngine.ShowMessage(string.Concat($"Installed On:".PadRight(displayFieldLength), starHolon.InstalledOn != DateTime.MinValue ? starHolon.InstalledOn.ToString() : "None"), false);
            CLIEngine.ShowMessage(string.Concat($"Installed By:".PadRight(displayFieldLength), starHolon.InstalledBy != Guid.Empty ? string.Concat(starHolon.InstalledByAvatarUsername, " (", starHolon.InstalledBy.ToString(), ")") : "None"), false);
            CLIEngine.ShowMessage(string.Concat($"Installed Path:".PadRight(displayFieldLength), !string.IsNullOrEmpty(starHolon.InstalledPath) ? starHolon.InstalledPath : "None"), false);

            if (showUninstallInfo)
            {
                CLIEngine.ShowMessage(string.Concat($"Uninstalled On:".PadRight(displayFieldLength), starHolon.UninstalledOn != DateTime.MinValue ? starHolon.UninstalledOn.ToString() : "None"), false);
                CLIEngine.ShowMessage(string.Concat($"Uninstalled By:".PadRight(displayFieldLength), starHolon.UninstalledBy != Guid.Empty ? string.Concat(starHolon.UninstalledByAvatarUsername, " (", starHolon.UninstalledBy.ToString(), ")") : "None"), false);
            }

            if (showFooter)
                CLIEngine.ShowDivider();
        }

        public void ShowHeader()
        {
            CLIEngine.ShowDivider();
            CLIEngine.ShowMessage(CreateHeader);
            CLIEngine.ShowDivider();
            Console.WriteLine();

            for (int i = 0; i < CreateIntroParagraphs.Count; i++)
                CLIEngine.ShowMessage(CreateIntroParagraphs[i]);

            CLIEngine.ShowDivider();
        }

        protected void ShowDependency(ISTARNETDependency metaData, int displayFieldLength)
        {
            Console.WriteLine("");
            DisplayProperty("Id", metaData.STARNETHolonId.ToString(), displayFieldLength);
            DisplayProperty("Name", metaData.Name, displayFieldLength);
            DisplayProperty("Description", metaData.Description, displayFieldLength);
            DisplayProperty("Version", metaData.Version, displayFieldLength);
            DisplayProperty("Version Sequence", metaData.VersionSequence.ToString(), displayFieldLength);
            DisplayProperty("Installed From", metaData.InstalledFrom, displayFieldLength);
            DisplayProperty("Installed To", metaData.InstalledTo, displayFieldLength);
            //Console.WriteLine("");
        }

        protected void ShowDependenices(IList<STARNETDependency> dependencies, int displayFieldLength)
        {
            if (dependencies.Count > 0)
            {
                foreach (ISTARNETDependency dependency in dependencies)
                    ShowDependency(dependency, displayFieldLength);

                Console.WriteLine("");
            }
            //else
            //    CLIEngine.ShowMessage("None", false);
        }

        protected void ShowAllDependencies(ISTARNETHolon starHolon, bool showDetailed, int displayFieldLength)
        {
            string tip = "";

            //if (!showDetailed)
            //    tip = "(use show/list detailed to view)";

            Console.WriteLine("");
            DisplayProperty("DEPENDENCIES", "", displayFieldLength, false);
            Console.WriteLine("");
            DisplayDependencyType("OAPPs", starHolon.STARNETDNA.Dependencies.OAPPs, tip, showDetailed, displayFieldLength);
            DisplayDependencyType("Runtimes", starHolon.STARNETDNA.Dependencies.Runtimes, tip, showDetailed, displayFieldLength);
            DisplayDependencyType("Libs", starHolon.STARNETDNA.Dependencies.Libraries, tip, showDetailed, displayFieldLength);
            DisplayDependencyType("Templates", starHolon.STARNETDNA.Dependencies.Templates, tip, showDetailed, displayFieldLength);
            DisplayDependencyType("Holons", starHolon.STARNETDNA.Dependencies.Holons, tip, showDetailed, displayFieldLength);
            DisplayDependencyType("Zomes", starHolon.STARNETDNA.Dependencies.Zomes, tip, showDetailed, displayFieldLength);
            DisplayDependencyType("CelestialSpaces", starHolon.STARNETDNA.Dependencies.CelestialSpaces, tip, showDetailed, displayFieldLength);
            DisplayDependencyType("CelestialBodies", starHolon.STARNETDNA.Dependencies.CelestialBodies, tip, showDetailed, displayFieldLength);
            DisplayDependencyType("CelestialBodiesMetaDataDNA", starHolon.STARNETDNA.Dependencies.CelestialBodiesMetaDataDNA, tip, showDetailed, displayFieldLength);
            DisplayDependencyType("ZomesMetaDataDNA", starHolon.STARNETDNA.Dependencies.ZomesMetaDataDNA, tip, showDetailed, displayFieldLength);
            DisplayDependencyType("HolonsMetaDataDNA", starHolon.STARNETDNA.Dependencies.HolonsMetaDataDNA, tip, showDetailed, displayFieldLength);
            DisplayDependencyType("NFTs", starHolon.STARNETDNA.Dependencies.NFTs, tip, showDetailed, displayFieldLength);
            DisplayDependencyType("NFTCollections", starHolon.STARNETDNA.Dependencies.NFTCollections, tip, showDetailed, displayFieldLength);
            DisplayDependencyType("GeoNFTs", starHolon.STARNETDNA.Dependencies.GeoNFTs, tip, showDetailed, displayFieldLength);
            DisplayDependencyType("GeoNFTCollections", starHolon.STARNETDNA.Dependencies.GeoNFTCollections, tip, showDetailed, displayFieldLength);
            DisplayDependencyType("GeoHotSpots", starHolon.STARNETDNA.Dependencies.GeoHotSpots, tip, showDetailed, displayFieldLength);
            DisplayDependencyType("Chapters", starHolon.STARNETDNA.Dependencies.Chapters, tip, showDetailed, displayFieldLength);
            DisplayDependencyType("Missions", starHolon.STARNETDNA.Dependencies.Missions, tip, showDetailed, displayFieldLength);
            DisplayDependencyType("Quests", starHolon.STARNETDNA.Dependencies.Quests, tip, showDetailed, displayFieldLength);
            DisplayDependencyType("InventoryItems", starHolon.STARNETDNA.Dependencies.InventoryItems, tip, showDetailed, displayFieldLength);

            //DisplayDependencyType("OAPPS", starHolon.STARNETDNA.Dependencies.OAPPs, tip, showDetailed, displayFieldLength);
            //DisplayDependencyType("RUNTIMES", starHolon.STARNETDNA.Dependencies.Runtimes, tip, showDetailed, displayFieldLength);
            //DisplayDependencyType("LIBS", starHolon.STARNETDNA.Dependencies.Libraries, tip, showDetailed, displayFieldLength);
            //DisplayDependencyType("TEMPLATES", starHolon.STARNETDNA.Dependencies.Templates, tip, showDetailed, displayFieldLength);
            //DisplayDependencyType("HOLONS", starHolon.STARNETDNA.Dependencies.Holons, tip, showDetailed, displayFieldLength);
            //DisplayDependencyType("ZOMES", starHolon.STARNETDNA.Dependencies.Zomes, tip, showDetailed, displayFieldLength);
            //DisplayDependencyType("CELESTIALSPACES", starHolon.STARNETDNA.Dependencies.CelestialSpaces, tip, showDetailed, displayFieldLength);
            //DisplayDependencyType("CELESTIALBODIES", starHolon.STARNETDNA.Dependencies.CelestialBodies, tip, showDetailed, displayFieldLength);
            //DisplayDependencyType("CELESTIALBODYMETADATA", starHolon.STARNETDNA.Dependencies.CelestialBodiesMetaDataDNA, tip, showDetailed, displayFieldLength);
            //DisplayDependencyType("ZOMESMETADATA", starHolon.STARNETDNA.Dependencies.ZomesMetaDataDNA, tip, showDetailed, displayFieldLength);
            //DisplayDependencyType("HOLONSMETADATA", starHolon.STARNETDNA.Dependencies.HolonsMetaDataDNA, tip, showDetailed, displayFieldLength);
            //DisplayDependencyType("NFTS", starHolon.STARNETDNA.Dependencies.NFTs, tip, showDetailed, displayFieldLength);
            //DisplayDependencyType("NFTCOLLECTIONS", starHolon.STARNETDNA.Dependencies.NFTCollections, tip, showDetailed, displayFieldLength);
            //DisplayDependencyType("GEONFTS", starHolon.STARNETDNA.Dependencies.GeoNFTs, tip, showDetailed, displayFieldLength);
            //DisplayDependencyType("GEONFTCOLLECTIONS", starHolon.STARNETDNA.Dependencies.GeoNFTCollections, tip, showDetailed, displayFieldLength);
            //DisplayDependencyType("GEOHOTSPOTS", starHolon.STARNETDNA.Dependencies.GeoHotSpots, tip, showDetailed, displayFieldLength);
            //DisplayDependencyType("CHAPTERS", starHolon.STARNETDNA.Dependencies.Chapters, tip, showDetailed, displayFieldLength);
            //DisplayDependencyType("MISSIONS", starHolon.STARNETDNA.Dependencies.Missions, tip, showDetailed, displayFieldLength);
            //DisplayDependencyType("QUESTS", starHolon.STARNETDNA.Dependencies.Quests, tip, showDetailed, displayFieldLength);
            //DisplayDependencyType("INVENTORYITEMS", starHolon.STARNETDNA.Dependencies.InventoryItems, tip, showDetailed, displayFieldLength);

            if (!showDetailed)
            {
                Console.WriteLine("");
                DisplayProperty("Use 'show/list detailed' command to view dependency details.", "", displayFieldLength, false);
            }
        }

        private void DisplayDependencyType(string dependencyType, List<STARNETDependency> dependencies, string tip, bool showDetailed, int displayFieldLength)
        {
            if (showDetailed)
                Console.WriteLine("");
            
            DisplayProperty(string.Concat(dependencyType, " (", dependencies.Count, ")"), "", displayFieldLength, false);

            if (showDetailed)
                ShowDependenices(dependencies, displayFieldLength);

            //CLIEngine.ShowMessage(string.Concat($"{dependencies.Count} Found.", dependencies.Count > 0 ? tip : ""), false);
        }

        //protected void ShowHolonMetaTagMappings(Dictionary<string, (string, string)> metaHolonTagMappings, bool showDetailedInfo, int displayFieldLength = DEFAULT_FIELD_LENGTH)
        protected void ShowHolonMetaTagMappings(List<MetaHolonTag> metaHolonTagMappings, bool showDetailedInfo, int displayFieldLength = DEFAULT_FIELD_LENGTH)
        {
            if (showDetailedInfo)
            {
                if (metaHolonTagMappings != null && metaHolonTagMappings.Count > 0)
                {
                    int colWidth = 20;
                    Console.WriteLine("");
                    CLIEngine.ShowMessage(string.Concat("Holon Meta Tag Mappings", " (", metaHolonTagMappings.Count.ToString(), "):"), false);
                    Console.WriteLine("");
                    CLIEngine.ShowMessage(string.Concat("TAG".PadRight(20), "HOLON".PadRight(colWidth), "NODE".PadRight(colWidth), "TYPE".PadRight(colWidth)), false);
                    //CLIEngine.ShowMessage(string.Concat("TAG".PadRight(22), "HOLON".PadRight(22), "HOLON TYPE".PadRight(22), "NODE".PadRight(22), "NODE TYPE".PadRight(22)), false);
                    Console.WriteLine("");

                    foreach (MetaHolonTag metaHolonTag in metaHolonTagMappings)
                        CLIEngine.ShowMessage(string.Concat(metaHolonTag.MetaTag.PadRight(colWidth), metaHolonTag.HolonName.PadRight(colWidth), metaHolonTag.NodeName.PadRight(colWidth), metaHolonTag.NodeType.PadRight(colWidth)), false);
                    //CLIEngine.ShowMessage(string.Concat(metaHolonTag.MetaTag.PadRight(22), metaHolonTag.HolonName.PadRight(22), metaHolonTag.NodeName.PadRight(22), metaHolonTag.NodeType.Name.PadRight(22)), false);
                    //CLIEngine.ShowMessage(string.Concat(metaHolonTag.MetaTag.PadRight(22), metaHolonTag.HolonName.PadRight(22), metaHolonTag.NodeName, Enum.GetName(typeof(NodeType), metaHolonTag.NodeType).PadRight(22)), false);

                    Console.WriteLine("");
                }
                else
                    DisplayProperty("Holon Meta Tag Mappings", "None", displayFieldLength);
            }
            else
                DisplayProperty("Holon Meta Tag Mappings", string.Concat(metaHolonTagMappings != null && metaHolonTagMappings.Count > 0 ? metaHolonTagMappings.Count.ToString() : "None", metaHolonTagMappings != null && metaHolonTagMappings.Count > 0 ? " (use show/list detailed to view)" : ""), displayFieldLength);
        }

        protected void ShowMetaTagMappings(Dictionary<string, string> metaTagMappings, bool showDetailedInfo, int displayFieldLength = DEFAULT_FIELD_LENGTH)
        {
            if (showDetailedInfo)
            {
                if (metaTagMappings != null && metaTagMappings.Count > 0)
                {
                    int colWidth = 20;
                    Console.WriteLine("");
                    CLIEngine.ShowMessage(string.Concat("Meta Tag Mappings", " (", metaTagMappings.Count.ToString(), "):"), false);
                    Console.WriteLine("");
                    CLIEngine.ShowMessage(string.Concat("TAG".PadRight(colWidth), "META DATA".PadRight(colWidth)), false);
                    Console.WriteLine("");

                    foreach (string key in metaTagMappings.Keys)
                        CLIEngine.ShowMessage(string.Concat(key.PadRight(colWidth), metaTagMappings[key].PadRight(colWidth)), false);

                    Console.WriteLine("");
                }
                else
                    DisplayProperty("Meta Tag Mappings", "None", displayFieldLength);
            }
            else
                DisplayProperty("Meta Tag Mappings", string.Concat(metaTagMappings != null && metaTagMappings.Count > 0 ? metaTagMappings.Count.ToString() : "None", metaTagMappings != null && metaTagMappings.Count > 0 ? " (use show/list detailed to view)" : ""), displayFieldLength);
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

                        if (operationName != "view")
                            await ShowAsync(result.Result);
                    }
                }

                if (idOrName == "exit")
                    break;

                if (result.Result != null && operationName != "view")
                {
                    if (CLIEngine.GetConfirmation($"Please confirm you wish to {operationName} this {STARNETHolonUIName}?"))
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

                        if (operationName != "view")
                            ShowAsync(result.Result);
                    }
                }

                if (idOrName == "exit")
                    break;

                if (result.Result != null && operationName != "view")
                {
                    if (CLIEngine.GetConfirmation($"Please confirm you wish to {operationName} this {STARNETManager.STARNETHolonUIName}?"))
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

        public async Task<OASISResult<T1>> CloneAsync(object options = null)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            
            //TODO: Implement ASAP! ;-)

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


        protected string ParseMetaData(Dictionary<string, object> metaData, string key, string notFoundDefaultValue = "None")
        {
            return metaData != null && metaData.ContainsKey(key) && metaData[key] != null && !string.IsNullOrEmpty(metaData[key].ToString()) ? metaData[key].ToString() : notFoundDefaultValue;
        }

        protected string ParseMetaDataForEnum(Dictionary<string, object> metaData, string key, Type enumType, string notFoundDefaultValue = "None")
        {
            return metaData != null && metaData.ContainsKey(key) && metaData[key] != null ? Enum.GetName(enumType, metaData[key]) : notFoundDefaultValue;
        }

        protected string ParseMetaDataForByteArray(Dictionary<string, object> metaData, string key, string foundDefaultValue = "Yes", string notFoundDefaultValue = "No")
        {
            return metaData != null && metaData.ContainsKey(key) && metaData[key] != null ? foundDefaultValue : notFoundDefaultValue;
        }

        protected string ParseMetaDataForPositiveNumber(Dictionary<string, object> metaData, string key)
        {
            int number;

            if (metaData != null && metaData.ContainsKey(key) && metaData[key] != null)
            {
                if (int.TryParse(metaData[key].ToString(), out number))
                {
                    if (number > 0)
                        return number.ToString();
                }
            }

            return "None";
        }

        protected string ParseMetaDataForLatLong(Dictionary<string, object> metaData, string latKey, string longKey)
        {
            string latReturn = ParseMetaDataForPositiveNumber(metaData, latKey);
            string longReturn = ParseMetaDataForPositiveNumber(metaData, longKey);

            if (latReturn != "None" && longReturn != "None")
                return $"{latReturn}/{longReturn}";

            return "None";
        }

        protected string ParseMetaDataForBinaryUploadAndURI(Dictionary<string, object> metaData, string binaryUploadKey, string URIKey)
        {
            return metaData != null && metaData.ContainsKey(binaryUploadKey) && metaData[binaryUploadKey] != null ? "BINARY UPLOADED" : metaData != null && metaData.ContainsKey(URIKey) && metaData[URIKey] != null ? metaData[URIKey].ToString() : "None";
        }

        protected void DisplayProperty(string heading, string value, int displayFieldLength, bool displayColon = true)
        {
            CLIEngine.DisplayProperty(heading, value, displayFieldLength, displayColon);
        }

        //protected void ShowNFTDetails(INFTBase nft, IWeb4OASISNFT web4NFT, int displayFieldLength, bool displayTags = true, bool displayMetaData = true)
        //{
        //    DisplayProperty("NFT Id", nft.Id.ToString(), displayFieldLength);

        //    if ((web4NFT != null && nft.Title != web4NFT.Title) || web4NFT == null)
        //        DisplayProperty("Title", nft.Title, displayFieldLength);

        //    if ((web4NFT != null && nft.Description != web4NFT.Description) || web4NFT == null)
        //        DisplayProperty("Description", nft.Description, displayFieldLength);

        //    if ((web4NFT != null && nft.Price != web4NFT.Price) || web4NFT == null)
        //        DisplayProperty("Price", nft.Price.ToString(), displayFieldLength);

        //    if ((web4NFT != null && nft.Discount != web4NFT.Discount) || web4NFT == null)
        //        DisplayProperty("Discount", nft.Discount.ToString(), displayFieldLength);

        //    if ((web4NFT != null && nft.RoyaltyPercentage != web4NFT.RoyaltyPercentage) || web4NFT == null)
        //        DisplayProperty("Royalty Percentage", nft.RoyaltyPercentage.ToString(), displayFieldLength);

        //    if ((web4NFT != null && nft.IsForSale != web4NFT.IsForSale) || web4NFT == null)
        //        DisplayProperty("For Sale", nft.IsForSale ? string.Concat("Yes (StartDate: ", nft.SaleStartDate.HasValue ? nft.SaleStartDate.Value.ToShortDateString() : "Not Set", nft.SaleEndDate.HasValue ? nft.SaleEndDate.Value.ToShortDateString() : "Not Set") : "No", displayFieldLength);

        //    if ((web4NFT != null && nft.MintedByAvatarId != web4NFT.MintedByAvatarId) || web4NFT == null)
        //        DisplayProperty("Minted By Avatar Id", nft.MintedByAvatarId.ToString(), displayFieldLength);

        //    if ((web4NFT != null && nft.MintedOn != web4NFT.MintedOn) || web4NFT == null)
        //        DisplayProperty("Minted On", nft.MintedOn.ToString(), displayFieldLength);

        //    if ((web4NFT != null && nft.OnChainProvider.Name != web4NFT.OnChainProvider.Name) || web4NFT == null)
        //        DisplayProperty("OnChain Provider", nft.OnChainProvider.Name, displayFieldLength);

        //    if ((web4NFT != null && nft.OffChainProvider.Name != web4NFT.OffChainProvider.Name) || web4NFT == null)
        //        DisplayProperty("OffChain Provider", nft.OffChainProvider.Name, displayFieldLength);

        //    if ((web4NFT != null && nft.StoreNFTMetaDataOnChain != web4NFT.StoreNFTMetaDataOnChain) || web4NFT == null)
        //        DisplayProperty("Store NFT Meta Data OnChain", nft.StoreNFTMetaDataOnChain.ToString(), displayFieldLength);

        //    if ((web4NFT != null && nft.NFTOffChainMetaType.Name != web4NFT.NFTOffChainMetaType.Name) || web4NFT == null)
        //        DisplayProperty("NFT OffChain Meta Type", nft.NFTOffChainMetaType.Name, displayFieldLength);

        //    if ((web4NFT != null && nft.NFTStandardType.Name != web4NFT.NFTStandardType.Name) || web4NFT == null)
        //        DisplayProperty("NFT Standard Type", nft.NFTStandardType.Name, displayFieldLength);

        //    if ((web4NFT != null && nft.Symbol != web4NFT.Symbol) || web4NFT == null)
        //        DisplayProperty("Symbol", nft.Symbol, displayFieldLength);

        //    if ((web4NFT != null && nft.Image != web4NFT.Image) || web4NFT == null)
        //        DisplayProperty("Image", nft.Image != null ? "Yes" : "None", displayFieldLength);

        //    if ((web4NFT != null && nft.ImageUrl != web4NFT.ImageUrl) || web4NFT == null)
        //        DisplayProperty("Image Url", nft.ImageUrl, displayFieldLength);

        //    if ((web4NFT != null && nft.Thumbnail != web4NFT.Thumbnail) || web4NFT == null)
        //        DisplayProperty("Thumbnail", nft.Thumbnail != null ? "Yes" : "None", displayFieldLength);

        //    if ((web4NFT != null && nft.ThumbnailUrl != web4NFT.ThumbnailUrl) || web4NFT == null)
        //        DisplayProperty("Thumbnail Url", !string.IsNullOrEmpty(nft.ThumbnailUrl) ? nft.ThumbnailUrl : "None", displayFieldLength);

        //    if ((web4NFT != null && nft.JSONMetaDataURL != web4NFT.JSONMetaDataURL) || web4NFT == null)
        //        DisplayProperty("JSON MetaData URL", nft.JSONMetaDataURL, displayFieldLength);

        //    if ((web4NFT != null && nft.JSONMetaDataURLHolonId != web4NFT.JSONMetaDataURLHolonId) || web4NFT == null)
        //        DisplayProperty("JSON MetaData URL Holon Id", nft.JSONMetaDataURLHolonId != Guid.Empty ? nft.JSONMetaDataURLHolonId.ToString() : "None", displayFieldLength);

        //    if ((web4NFT != null && nft.SellerFeeBasisPoints != web4NFT.SellerFeeBasisPoints) || web4NFT == null)
        //        DisplayProperty("Seller Fee Basis Points", nft.SellerFeeBasisPoints.ToString(), displayFieldLength);

        //    if ((web4NFT != null && nft.SendToAddressAfterMinting != web4NFT.SendToAddressAfterMinting) || web4NFT == null)
        //        DisplayProperty("Send To Address After Minting", nft.SendToAddressAfterMinting, displayFieldLength);

        //    if ((web4NFT != null && nft.SendToAvatarAfterMintingId != web4NFT.SendToAvatarAfterMintingId) || web4NFT == null)
        //        DisplayProperty("Send To Avatar After Minting Id", nft.SendToAvatarAfterMintingId != Guid.Empty ? nft.SendToAvatarAfterMintingId.ToString() : "None", displayFieldLength);

        //    if ((web4NFT != null && nft.SendToAvatarAfterMintingUsername != web4NFT.SendToAvatarAfterMintingUsername) || web4NFT == null)
        //        DisplayProperty("Send To Avatar After Minting Username", !string.IsNullOrEmpty(nft.SendToAvatarAfterMintingUsername) ? nft.SendToAvatarAfterMintingUsername : "None", displayFieldLength);

        //    if ((web4NFT != null && displayTags && TagHelper.GetTags(nft.Tags) != TagHelper.GetTags(web4NFT.Tags)) || web4NFT == null)
        //        TagHelper.ShowTags(nft.Tags, displayFieldLength);

        //    if ((web4NFT != null && displayMetaData && MetaDataHelper.GetMetaData(nft.MetaData) != MetaDataHelper.GetMetaData(web4NFT.MetaData)) || web4NFT == null)
        //        MetaDataHelper.ShowMetaData(nft.MetaData, displayFieldLength);

        //    //CLIEngine.ShowDivider();
        //}


        protected async Task<OASISResult<ImageObjectResult>> ProcessImageOrObjectAsync(string holonType)
        {
            OASISResult<ImageObjectResult> result = new OASISResult<ImageObjectResult>(new ImageObjectResult());

            if (CLIEngine.GetConfirmation($"Would you rather use a 3D object or a 2D sprite/image to represent your {holonType} in Our World and other UI's? Press Y for 3D or N for 2D."))
            {
                Console.WriteLine("");

                if (CLIEngine.GetConfirmation("Would you like to upload a local 3D object from your device or input a URI to an online object? (Press Y for local or N for online)"))
                {
                    Console.WriteLine("");
                    string objPath = CLIEngine.GetValidFile("What is the full path to the local 3D object? (Press Enter if you wish to skip and use a default 3D object instead. You can always change this later.)");

                    if (objPath == "exit")
                    {
                        result.Message = "User Exited";
                        return result;
                    }

                    result.Result.Object3D = File.ReadAllBytes(objPath);

                }
                else
                {
                    Console.WriteLine("");
                    result.Result.Object3DURI = await CLIEngine.GetValidURIAsync("What is the URI to the 3D object? (Press Enter if you wish to skip and use a default 3D object instead. You can always change this later.)");

                    if (result.Result.Object3DURI == null)
                    {
                        result.Message = "User Exited";
                        return result;
                    }
                }
            }
            else
            {
                Console.WriteLine("");

                if (CLIEngine.GetConfirmation("Would you like to upload a local 2D sprite/image from your device or input a URI to an online sprite/image? (Press Y for local or N for online)"))
                {
                    Console.WriteLine("");
                    string imgPath = CLIEngine.GetValidFile("What is the full path to the local 2d sprite/image? (Press Enter if you wish to skip and use the default image instead. You can always change this later.)");

                    if (imgPath == "exit")
                    {
                        result.Message = "User Exited";
                        return result;
                    }

                    result.Result.Image2D = File.ReadAllBytes(imgPath);
                }
                else
                {
                    Console.WriteLine("");
                    result.Result.Image2DURI = await CLIEngine.GetValidURIAsync("What is the URI to the 2D sprite/image? (Press Enter if you wish to skip and use the default image instead. You can always change this later.)");

                    if (result.Result.Image2DURI == null)
                    {
                        result.Message = "User Exited";
                        return result;
                    }
                }
            }

            return result;
        }

        private OASISResult<IEnumerable<T>> ListStarHolons<T>(OASISResult<IEnumerable<T>> starHolons, bool showNumbers = false, bool showDetailedInfo = false) where T : ISTARNETHolon, new()
        {
            if (starHolons != null)
            {
                if (!starHolons.IsError)
                {
                    if (starHolons.Result != null && starHolons.Result.Count() > 0)
                    {
                        Console.WriteLine();

                        if (starHolons.Result.Count() == 1)
                            CLIEngine.ShowMessage($"{starHolons.Result.Count()} {STARNETManager.STARNETHolonUIName} Found:");
                        else
                            CLIEngine.ShowMessage($"{starHolons.Result.Count()} {STARNETManager.STARNETHolonUIName}s Found:");

                        for (int i = 0; i < starHolons.Result.Count(); i++)
                            ShowAsync(starHolons.Result.ElementAt(i), i == 0, true, showNumbers, i + 1, showDetailedInfo);
                    }
                    else
                        CLIEngine.ShowWarningMessage($"No {STARNETManager.STARNETHolonUIName}'s Found.");
                }
                else
                    CLIEngine.ShowErrorMessage($"Error occured loading {STARNETManager.STARNETHolonUIName}'s. Reason: {starHolons.Message}");
            }
            else
                CLIEngine.ShowErrorMessage($"Unknown error occured loading {STARNETManager.STARNETHolonUIName}'s.");

            return starHolons;
        }

        private void ListStarHolonsInstalled(OASISResult<IEnumerable<T3>> starHolons, bool showNumbers = false, bool showUninstallInfo = false)
        {
            if (starHolons != null)
            {
                if (!starHolons.IsError)
                {
                    if (starHolons.Result != null && starHolons.Result.Count() > 0)
                    {
                        Console.WriteLine();

                        if (starHolons.Result.Count() == 1)
                            CLIEngine.ShowMessage($"{starHolons.Result.Count()} {STARNETManager.STARNETHolonUIName} Found:");
                        else
                            CLIEngine.ShowMessage($"{starHolons.Result.Count()} {STARNETManager.STARNETHolonUIName}s Found:");

                        for (int i = 0; i < starHolons.Result.Count(); i++)
                            ShowInstalled(starHolons.Result.ElementAt(i), i == 0, true, showNumbers, i + 1, showUninstallInfo);
                    }
                    else
                        CLIEngine.ShowWarningMessage($"No {STARNETManager.STARNETHolonUIName}s Found.");
                }
                else
                    CLIEngine.ShowErrorMessage($"Error occured loading {STARNETManager.STARNETHolonUIName}'s. Reason: {starHolons.Message}");
            }
            else
                CLIEngine.ShowErrorMessage($"Unknown error occured loading {STARNETManager.STARNETHolonUIName}'s.");
        }

        private async Task<OASISResult<T>> CheckIfAlreadyInstalledAsync<T>(T holon, ProviderType providerType = ProviderType.Default) where T : ISTARNETHolon, new()
        {
            OASISResult<T> result = new OASISResult<T>();
            OASISResult<bool> oappInstalledResult = await STARNETManager.IsInstalledAsync(STAR.BeamedInAvatar.Id, holon.STARNETDNA.Id, holon.STARNETDNA.Version, providerType);

            if (oappInstalledResult != null && !oappInstalledResult.IsError)
            {
                if (oappInstalledResult.Result)
                {
                    Console.WriteLine("");
                    CLIEngine.ShowWarningMessage($"You have already installed this version (v{holon.STARNETDNA.Version}). Please uninstall before attempting to re-install.");

                    if (CLIEngine.GetConfirmation($"Do you wish to uninstall the {STARNETManager.STARNETHolonUIName} now? Press 'Y' for Yes or 'N' for No."))
                    {
                        Console.WriteLine("");
                        CLIEngine.ShowWorkingMessage($"Uninstalling {STARNETManager.STARNETHolonUIName}...");
                        OASISResult<T3> uninstallResult = await STARNETManager.UninstallAsync(STAR.BeamedInAvatar.Id, holon.STARNETDNA.Id, holon.STARNETDNA.Version, providerType);

                        if (uninstallResult != null && uninstallResult.Result != null && !uninstallResult.IsError)
                        {
                            CLIEngine.ShowSuccessMessage($"{STARNETManager.STARNETHolonUIName} Successfully Uninstalled.");
                            result.MetaData["Reinstall"] = "1";
                        }
                        else
                            OASISErrorHandling.HandleError(ref result, $"Error occured uninstalling the {STARNETManager.STARNETHolonUIName}! Reason: {uninstallResult.Message}");
                    }
                    else
                    {
                        result.IsError = true;
                        result.Message = "User Denied Uninstall";
                        Console.WriteLine("");
                    }
                }
            }
            else
                OASISErrorHandling.HandleError(ref result, ($"Error occured checking if the {STARNETManager.STARNETHolonUIName} is already installed! Reason: {oappInstalledResult.Message}"));

            return result;
        }

        private OASISResult<T1> CheckIfAlreadyInstalled(T1 holon, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            OASISResult<bool> oappInstalledResult = STARNETManager.IsInstalled(STAR.BeamedInAvatar.Id, holon.STARNETDNA.Id, holon.STARNETDNA.Version, providerType);

            if (oappInstalledResult != null && !oappInstalledResult.IsError)
            {
                if (oappInstalledResult.Result)
                {
                    Console.WriteLine("");
                    CLIEngine.ShowWarningMessage($"You have already installed this version (v{holon.STARNETDNA.Version}). Please uninstall before attempting to re-install.");

                    if (CLIEngine.GetConfirmation($"Do you wish to uninstall the {STARNETManager.STARNETHolonUIName} now? Press 'Y' for Yes or 'N' for No."))
                    {
                        Console.WriteLine("");
                        CLIEngine.ShowWorkingMessage($"Uninstalling {STARNETManager.STARNETHolonUIName}...");
                        OASISResult<T3> uninstallResult = STARNETManager.Uninstall(STAR.BeamedInAvatar.Id, result.Result.STARNETDNA.Id, result.Result.STARNETDNA.Version, providerType);

                        if (uninstallResult != null && uninstallResult.Result != null && !uninstallResult.IsError)
                        {
                            CLIEngine.ShowSuccessMessage($"{STARNETManager.STARNETHolonUIName} Successfully Uninstalled.");
                            result.MetaData["Reinstall"] = "1";
                        }
                        else
                            OASISErrorHandling.HandleError(ref result, $"Error occured uninstalling the {STARNETManager.STARNETHolonUIName}! Reason: {uninstallResult.Message}");
                    }
                    else
                    {
                        result.IsError = true;
                        result.Message = "User Denied Uninstall";
                        Console.WriteLine("");
                    }
                }
            }
            else
                OASISErrorHandling.HandleError(ref result, ($"Error occured checking if the {STARNETManager.STARNETHolonUIName} is already installed! Reason: {oappInstalledResult.Message}"));

            return result;
        }

        private async Task<OASISResult<T3>> CheckIfInstalledAndInstallAsync(T1 holon, string downloadPath, string installPath, InstallMode installMode, string fullPathToPublishedFile = "", ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T3> installResult = new OASISResult<T3>();
            bool continueInstall = false;

            if (holon != null)
            {
                if (installMode != InstallMode.DownloadOnly)
                {
                    OASISResult<T1> checkResult = await CheckIfAlreadyInstalledAsync(holon, providerType);

                    if (checkResult != null && !checkResult.IsError)
                        continueInstall = true;
                    else
                        CLIEngine.ShowErrorMessage($"Error checking if the {STARNETManager.STARNETHolonUIName} is already installed! Reason: {checkResult.MetaData}");
                }
            }

            if (continueInstall)
                installResult = await InstallAsync(holon, downloadPath, installPath, installMode, fullPathToPublishedFile, providerType);

            if (installResult != null && installResult.IsError && installResult.Message.Contains("is not published"))
            {
                if (holon.STARNETDNA.CreatedByAvatarId == STAR.BeamedInAvatar.Id)
                {
                    if (CLIEngine.GetConfirmation("Would you like to publish it now?"))
                    {
                        Console.WriteLine("");
                        //OASISResult<bool> publishResult = await STARNETManager.PublishAsync(STAR.BeamedInAvatar.Id, holon.STARNETDNA.Id, holon.STARNETDNA.VersionSequence, providerType);
                        OASISResult<T1> publishResult = await PublishAsync(holon.STARNETDNA.SourcePath, defaultLaunchMode: DefaultLaunchMode.Optional, askToInstallAtEnd: false, providerType: providerType);

                        if (!(publishResult != null && !publishResult.IsError && publishResult.Result != null))
                            CLIEngine.ShowErrorMessage($"Error publishing the {STARNETManager.STARNETHolonUIName} before installing it! Reason: {publishResult.Message}");
                        else
                        {
                            installResult.IsError = false;
                            installResult.Message = "";
                        }
                        //The publish routine automatically installs at the end(if the user agrees) so no need to install again here.
                        if (publishResult != null && !publishResult.IsError && publishResult.Result != null)
                            installResult = await InstallAsync(holon, downloadPath, installPath, installMode, fullPathToPublishedFile, providerType);
                        else
                            CLIEngine.ShowErrorMessage($"Error publishing the {STARNETManager.STARNETHolonUIName} before installing it! Reason: {publishResult.Message}");
                    }
                    else
                        Console.WriteLine("");
                }
            }

            return installResult;
        }

        private OASISResult<T3> CheckIfInstalledAndInstall(T1 holon, string downloadPath, string installPath, InstallMode installMode, string fullPathToPublishedFile = "", ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T3> installResult = new OASISResult<T3>();
            bool continueInstall = false;

            if (holon != null)
            {
                if (installMode != InstallMode.DownloadOnly)
                {
                    OASISResult<T1> checkResult = CheckIfAlreadyInstalled(holon, providerType);

                    if (checkResult != null && !checkResult.IsError)
                        continueInstall = true;
                    else
                        CLIEngine.ShowErrorMessage($"Error checking if the {STARNETManager.STARNETHolonUIName} is already installed! Reason: {checkResult.MetaData}");
                }
            }

            if (continueInstall)
                installResult = Install(holon, downloadPath, installPath, installMode, fullPathToPublishedFile, providerType);

            return installResult;
        }

        protected async Task<OASISResult<T3>> InstallAsync(T1 starHolon, string downloadPath, string installPath, InstallMode installMode, string fullPathToPublishedFile = "", ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T3> result = new OASISResult<T3>();
            //OASISResult<bool> publishResult = await STARNETManager.IsPublishedAsync(STAR.BeamedInAvatar.Id, starHolon.STARNETDNA.Id, starHolon.STARNETDNA.VersionSequence, providerType);
            //OASISResult<bool> publishResult = await STARNETManager.IsPublishedAsync(STAR.BeamedInAvatar.Id, starHolon.STARNETDNA.Id, starHolon.MetaData["Version"].ToString(), providerType);
            OASISResult<bool> publishResult = await STARNETManager.IsPublishedAsync(STAR.BeamedInAvatar.Id, starHolon.STARNETDNA.Id, starHolon.STARNETDNA.Version, providerType);

            if (publishResult != null && !publishResult.IsError)
            {
                if (!publishResult.Result)
                {
                    OASISErrorHandling.HandleError(ref result, $"The {STARNETManager.STARNETHolonUIName} is not published and cannot be installed. Please publish it first.");
                    return result;
                }
            }
            else
            {
                OASISErrorHandling.HandleError(ref result, $"Error checking if {STARNETManager.STARNETHolonUIName} is published. Reason: {publishResult.Message}");
                return result;
            }

            switch (installMode)
            {
                case InstallMode.DownloadAndInstall:
                    result = await STARNETManager.DownloadAndInstallAsync(STAR.BeamedInAvatar.Id, starHolon, installPath, downloadPath, true, false, providerType);
                    break;

                case InstallMode.DownloadOnly:
                    {
                        OASISResult<T2> downloadResult = await STARNETManager.DownloadAsync(STAR.BeamedInAvatar.Id, starHolon, downloadPath, false, providerType);

                        if (downloadResult != null && downloadResult.Result != null && !downloadResult.IsError)
                        {
                            result.Result = new T3() { STARNETDNA = downloadResult.Result.STARNETDNA };
                            result.Result.DownloadedOn = downloadResult.Result.DownloadedOn;
                            result.Result.DownloadedBy = downloadResult.Result.DownloadedBy;
                            result.Result.DownloadedByAvatarUsername = downloadResult.Result.DownloadedByAvatarUsername;
                            result.Result.DownloadedPath = downloadResult.Result.DownloadedPath;
                        }
                        else
                        {
                            result.Message = downloadResult.Message;
                            result.IsError = true;
                        }
                    }
                    break;

                case InstallMode.InstallOnly:
                    result = await STARNETManager.InstallAsync(STAR.BeamedInAvatar.Id, fullPathToPublishedFile, installPath, true, null, false, providerType);
                    break;

                case InstallMode.DownloadAndReInstall:
                    result = await STARNETManager.DownloadAndInstallAsync(STAR.BeamedInAvatar.Id, starHolon, installPath, downloadPath, true, true, providerType);
                    break;

                case InstallMode.ReInstall:
                    result = await STARNETManager.InstallAsync(STAR.BeamedInAvatar.Id, fullPathToPublishedFile, installPath, true, null, true, providerType);
                    break;
            }

            return result;
        }

        //protected void ShowMetaData(Dictionary<string, object> metaData)
        //{
        //    if (metaData != null)
        //    {
        //        CLIEngine.ShowMessage($"MetaData:");

        //        foreach (string key in metaData.Keys)
        //            CLIEngine.ShowMessage(string.Concat("          key = ", GetMetaValue(metaData[key])), false);
        //    }
        //    else
        //        CLIEngine.ShowMessage($"MetaData: None");
        //}

        //private string GetMetaValue(object value)
        //{
        //    return value != null ? IsBinary(value) ? "<binary>" : value.ToString() : "None";
        //}

        //protected bool IsBinary(object data)
        //{
        //    if (data == null)
        //        return false;

        //    if (data is byte[])
        //        return true;

        //    try
        //    {
        //        byte[] binaryData = Convert.FromBase64String(data.ToString());

        //        for (int i = 0; i < binaryData.Length; i++)
        //        {
        //            if (binaryData[i] > 127)
        //                return true;
        //        }
        //    }
        //    catch { }

        //    return false;
        //}

        //protected Dictionary<string, object> AddMetaData(string holonName)
        //{
        //    Dictionary<string, object> metaData = new Dictionary<string, object>();

        //    if (CLIEngine.GetConfirmation($"Do you wish to add any metadata to this {holonName}?"))
        //    {
        //        metaData = AddItemToMetaData(metaData);
        //        bool metaDataDone = false;

        //        do
        //        {
        //            if (CLIEngine.GetConfirmation("Do you wish to add more metadata?"))
        //                metaData = AddItemToMetaData(metaData);
        //            else
        //                metaDataDone = true;
        //        }
        //        while (!metaDataDone);
        //    }

        //    return metaData;
        //}

        //protected Dictionary<string, object> AddItemToMetaData(Dictionary<string, object> metaData)
        //{
        //    Console.WriteLine("");
        //    string key = CLIEngine.GetValidInput("What is the key?");
        //    string value = "";
        //    byte[] metaFile = null;

        //    if (CLIEngine.GetConfirmation("Is the value a file?"))
        //    {
        //        Console.WriteLine("");
        //        string metaPath = CLIEngine.GetValidFile("What is the full path to the file?");
        //        metaFile = File.ReadAllBytes(metaPath);
        //    }
        //    else
        //    {
        //        Console.WriteLine("");
        //        value = CLIEngine.GetValidInput("What is the value?");
        //    }

        //    if (metaFile != null)
        //        metaData[key] = metaFile;
        //    else
        //        metaData[key] = value;

        //    return metaData;
        //}

        //protected Dictionary<string, object> ManageMetaData(Dictionary<string, object> metaData, string itemName)
        //{
        //    if (metaData == null)
        //        metaData = new Dictionary<string, object>();

        //    bool done = false;

        //    while (!done)
        //    {
        //        Console.WriteLine("");
        //        CLIEngine.ShowMessage($"Current {itemName} metadata:", false);

        //        if (metaData.Count == 0)
        //            CLIEngine.ShowMessage("  None", false);
        //        else
        //        {
        //            int i = 1;
        //            foreach (var kv in metaData)
        //            {
        //                CLIEngine.ShowMessage($"  {i}. {kv.Key} = {GetMetaValue(kv.Value)}", false);
        //                i++;
        //            }
        //        }

        //        Console.WriteLine("");
        //        CLIEngine.ShowMessage("Choose an action: (A)dd, (E)dit, (D)elete, (Q)uit", false);
        //        string choice = CLIEngine.GetValidInput("Enter A, E, D or Q:").ToUpper();

        //        switch (choice)
        //        {
        //            case "A":
        //                metaData = AddItemToMetaData(metaData);
        //                break;

        //            case "E":
        //                if (metaData.Count == 0)
        //                {
        //                    CLIEngine.ShowErrorMessage("No metadata to edit.");
        //                    break;
        //                }

        //                int editIndex = CLIEngine.GetValidInputForInt("Enter the number of the metadata entry to edit:", true, 1, metaData.Count);
        //                string editKey = metaData.Keys.ElementAt(editIndex - 1);
        //                object currentValue = metaData[editKey];

        //                if (currentValue is byte[])
        //                {
        //                    if (CLIEngine.GetConfirmation("This value is binary. Do you want to replace it with a file? (Y) or replace with text (N)?"))
        //                    {
        //                        string metaPath = CLIEngine.GetValidFile("What is the full path to the file?");
        //                        metaData[editKey] = File.ReadAllBytes(metaPath);
        //                    }
        //                    else
        //                    {
        //                        string newValue = CLIEngine.GetValidInput("Enter the new text value (or type 'clear' to remove):", addLineBefore: true);
        //                        if (newValue.ToLower() == "clear")
        //                            metaData.Remove(editKey);
        //                        else
        //                            metaData[editKey] = newValue;
        //                    }
        //                }
        //                else
        //                {
        //                    if (CLIEngine.GetConfirmation("Do you want to set this value from a file? (Y) or enter text value (N)?"))
        //                    {
        //                        string metaPath = CLIEngine.GetValidFile("What is the full path to the file?");
        //                        metaData[editKey] = File.ReadAllBytes(metaPath);
        //                    }
        //                    else
        //                    {
        //                        string newValue = CLIEngine.GetValidInput("Enter the new text value (or type 'clear' to remove):");
        //                        if (newValue.ToLower() == "clear")
        //                            metaData.Remove(editKey);
        //                        else
        //                            metaData[editKey] = newValue;
        //                    }
        //                }

        //                break;

        //            case "D":
        //                if (metaData.Count == 0)
        //                {
        //                    CLIEngine.ShowErrorMessage("No metadata to delete.");
        //                    break;
        //                }

        //                int delIndex = CLIEngine.GetValidInputForInt("Enter the number of the metadata entry to delete:", true, 1, metaData.Count);
        //                string delKey = metaData.Keys.ElementAt(delIndex - 1);

        //                if (CLIEngine.GetConfirmation($"Are you sure you want to delete metadata '{delKey}'?"))
        //                {
        //                    metaData.Remove(delKey);
        //                    CLIEngine.ShowSuccessMessage($"Metadata '{delKey}' deleted.", addLineBefore: true);
        //                }
        //                else
        //                    Console.WriteLine("");

        //                break;

        //            case "Q":
        //                done = true;
        //                break;

        //            default:
        //                CLIEngine.ShowErrorMessage("Invalid choice. Please enter A, E, D or Q.");
        //                break;
        //        }
        //    }

        //    return metaData;
        //}

        //protected void DisplayMetaData(Dictionary<string, object> metaData)
        //{
        //    foreach (string key in metaData.Keys)
        //        CLIEngine.ShowMessage(string.Concat("          key = ", metaData[key] is byte[]? "<binary>" : metaData[key]), false);
        //}

        private OASISResult<T3> Install(T1 starHolon, string downloadPath, string installPath, InstallMode installMode, string fullPathToPublishedFile = "", ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T3> result = new OASISResult<T3>();
            OASISResult<bool> publishResult = STARNETManager.IsPublished(STAR.BeamedInAvatar.Id, starHolon.STARNETDNA.Id, starHolon.STARNETDNA.VersionSequence, providerType);

            if (publishResult != null && !publishResult.IsError)
            {
                if (!publishResult.Result)
                {
                    OASISErrorHandling.HandleError(ref result, $"The {STARNETManager.STARNETHolonUIName} is not published and cannot be installed. Please publish it first.");
                    return result;
                }
            }
            else
            {
                OASISErrorHandling.HandleError(ref result, $"Error checking if {STARNETManager.STARNETHolonUIName} is published. Reason: {publishResult.Message}");
                return result;
            }

            switch (installMode)
            {
                case InstallMode.DownloadAndInstall:
                    result = STARNETManager.DownloadAndInstall(STAR.BeamedInAvatar.Id, starHolon, installPath, downloadPath, true, false, providerType);
                    break;

                case InstallMode.DownloadOnly:
                    {
                        OASISResult<T2> downloadResult = STARNETManager.Download(STAR.BeamedInAvatar.Id, starHolon, downloadPath, false, providerType);

                        if (downloadResult != null && downloadResult.Result != null && !downloadResult.IsError)
                        {
                            result.Result = new T3() { STARNETDNA = downloadResult.Result.STARNETDNA };
                            result.Result.DownloadedOn = downloadResult.Result.DownloadedOn;
                            result.Result.DownloadedBy = downloadResult.Result.DownloadedBy;
                            result.Result.DownloadedByAvatarUsername = downloadResult.Result.DownloadedByAvatarUsername;
                            result.Result.DownloadedPath = downloadResult.Result.DownloadedPath;
                        }
                        else
                        {
                            result.Message = downloadResult.Message;
                            result.IsError = true;
                        }
                    }
                    break;

                case InstallMode.InstallOnly:
                    result = STARNETManager.Install(STAR.BeamedInAvatar.Id, fullPathToPublishedFile, installPath, true, null, false, providerType);
                    break;

                case InstallMode.DownloadAndReInstall:
                    result = STARNETManager.DownloadAndInstall(STAR.BeamedInAvatar.Id, starHolon, installPath, downloadPath, true, true, providerType);
                    break;

                case InstallMode.ReInstall:
                    result = STARNETManager.Install(STAR.BeamedInAvatar.Id, fullPathToPublishedFile, installPath, true, null, true, providerType);
                    break;
            }

            return result;
        }

        private T1 ConvertFromT3ToT1(T3 holon)
        {
            T1 newHolon = new T1();
            newHolon.STARNETDNA = holon.STARNETDNA;
            newHolon.MetaData = holon.MetaData;
            return newHolon;
        }

        private void OnPublishStatusChanged(object sender, STARNETHolonPublishStatusEventArgs e)
        {
            switch (e.Status)
            {
                case STARNETHolonPublishStatus.DotNetPublishing:
                    CLIEngine.ShowWorkingMessage("DotNet Publishing...");
                    break;

                case STARNETHolonPublishStatus.Uploading:
                    CLIEngine.ShowMessage("Uploading...");
                    Console.WriteLine("");
                    break;

                case STARNETHolonPublishStatus.Published:
                    CLIEngine.ShowSuccessMessage($"{STARNETManager.STARNETHolonUIName} Published Successfully");
                    break;

                case STARNETHolonPublishStatus.Error:
                    CLIEngine.ShowErrorMessage(e.ErrorMessage);
                    break;

                default:
                    CLIEngine.ShowWorkingMessage($"{Enum.GetName(typeof(STARNETHolonPublishStatus), e.Status)}...");
                    break;
            }
        }

        private void OnUploadStatusChanged(object sender, STARNETHolonUploadProgressEventArgs e)
        {
            CLIEngine.ShowProgressBar((double)e.Progress / (double)100);
        }

        private void OnInstallStatusChanged(object sender, STARNETHolonInstallStatusEventArgs e)
        {
            switch (e.Status)
            {
                case STARNETHolonInstallStatus.Downloading:
                    CLIEngine.ShowMessage($"Downloading {e.STARNETDNA.Name} v{e.STARNETDNA.Version}...");
                    Console.WriteLine("");
                    break;

                case STARNETHolonInstallStatus.Installing:
                    CLIEngine.ShowWorkingMessage($"Installing {e.STARNETDNA.Name} v{e.STARNETDNA.Version}...");
                    break;

                case STARNETHolonInstallStatus.InstallingDependencies:
                    CLIEngine.ShowWorkingMessage("Installing Dependencies...");
                    break;

                case STARNETHolonInstallStatus.InstallingRuntimes:
                    CLIEngine.ShowWorkingMessage("Installing Runtimes...");
                    break;

                case STARNETHolonInstallStatus.InstallingLibs:
                    CLIEngine.ShowWorkingMessage("Installing Libs...");
                    break;

                case STARNETHolonInstallStatus.InstallingTemplates:
                    CLIEngine.ShowWorkingMessage("Installing Templates...");
                    break;

                case STARNETHolonInstallStatus.Installed:
                    CLIEngine.ShowSuccessMessage($"{e.STARNETDNA.Name} v{e.STARNETDNA.Version} Installed Successfully");
                    break;

                case STARNETHolonInstallStatus.Error:
                    CLIEngine.ShowErrorMessage(e.ErrorMessage);
                    break;

                default:
                    CLIEngine.ShowWorkingMessage($"{Enum.GetName(typeof(STARNETHolonInstallStatus), e.Status)}...");
                    break;
            }
        }

        private void OnDownloadStatusChanged(object sender, STARNETHolonDownloadProgressEventArgs e)
        {
            CLIEngine.ShowProgressBar((double)e.Progress / (double)100);
        }
    }
}