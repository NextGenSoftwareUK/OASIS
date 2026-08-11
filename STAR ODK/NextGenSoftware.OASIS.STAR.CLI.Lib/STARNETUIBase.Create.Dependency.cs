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
