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
    }
}
