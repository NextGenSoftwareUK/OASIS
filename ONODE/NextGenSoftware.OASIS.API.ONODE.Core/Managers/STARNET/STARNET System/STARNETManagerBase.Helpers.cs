using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Google.Cloud.Storage.V1;
using Microsoft.Extensions.DependencyModel;
using Newtonsoft.Json;
using NextGenSoftware.CLI.Engine;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.ONODE.Core.Enums.STARNETHolon;
using NextGenSoftware.OASIS.API.ONODE.Core.Events.STARNETHolon;
using NextGenSoftware.OASIS.API.ONODE.Core.Holons;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Holons;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Managers;
using NextGenSoftware.OASIS.API.ONODE.Core.Objects;
using NextGenSoftware.OASIS.API.ONODE.Core.Objects.STARNET;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.STAR.DNA;
using NextGenSoftware.Utilities;
using NextGenSoftware.OASIS.API.ONODE.Core.Managers.Interop;
using NextGenSoftware.OASIS.API.ONODE.Core.Enums;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Managers.Base
{
    public abstract partial class STARNETManagerBase<T1, T2, T3, T4> where T1 : ISTARNETHolon, new()
        where T2 : IDownloadedSTARNETHolon, new()
        where T3 : IInstalledSTARNETHolon, new()
        where T4 : ISTARNETDNA, new()
    {
        public List<ISTARNETDependency> ListAllDependencies(ISTARNETDNA STARNETDNA)
        {
            List<ISTARNETDependency> dependencies = new List<ISTARNETDependency>();
            dependencies.AddRange(STARNETDNA.Dependencies.CelestialBodies);
            dependencies.AddRange(STARNETDNA.Dependencies.CelestialBodiesMetaDataDNA);
            dependencies.AddRange(STARNETDNA.Dependencies.CelestialSpaces);
            dependencies.AddRange(STARNETDNA.Dependencies.Chapters);
            dependencies.AddRange(STARNETDNA.Dependencies.GeoHotSpots);
            dependencies.AddRange(STARNETDNA.Dependencies.GeoNFTs);
            dependencies.AddRange(STARNETDNA.Dependencies.GeoNFTCollections);
            dependencies.AddRange(STARNETDNA.Dependencies.Holons);
            dependencies.AddRange(STARNETDNA.Dependencies.HolonsMetaDataDNA);
            dependencies.AddRange(STARNETDNA.Dependencies.InventoryItems);
            dependencies.AddRange(STARNETDNA.Dependencies.Libraries);
            dependencies.AddRange(STARNETDNA.Dependencies.Missions);
            dependencies.AddRange(STARNETDNA.Dependencies.NFTs);
            dependencies.AddRange(STARNETDNA.Dependencies.NFTCollections);
            dependencies.AddRange(STARNETDNA.Dependencies.OAPPs);
            dependencies.AddRange(STARNETDNA.Dependencies.Quests);
            dependencies.AddRange(STARNETDNA.Dependencies.Runtimes);
            dependencies.AddRange(STARNETDNA.Dependencies.Templates);
            dependencies.AddRange(STARNETDNA.Dependencies.Zomes);
            dependencies.AddRange(STARNETDNA.Dependencies.ZomesMetaDataDNA);
            return dependencies;
        }

        protected void RaisePublishStatusChangedEvent(T4 STARNETDNA, STARNETHolonPublishStatus status, string errorMesssage = "")
        {
            OnPublishStatusChanged?.Invoke(this, new STARNETHolonPublishStatusEventArgs() { STARNETDNA = STARNETDNA, Status = status, ErrorMessage = errorMesssage });
        }

        protected void RaiseInstallStatusChangedEvent(T4 STARNETDNA, STARNETHolonInstallStatus status, string errorMesssage = "")
        {
            OnInstallStatusChanged?.Invoke(this, new STARNETHolonInstallStatusEventArgs() { STARNETDNA = STARNETDNA, Status = status, ErrorMessage = errorMesssage });
        }

        protected void RaiseUploadStatusChangedEvent(T4 STARNETDNA, STARNETHolonUploadStatus status, string errorMesssage = "")
        {
            OnUploadStatusChanged?.Invoke(this, new STARNETHolonUploadProgressEventArgs() { STARNETDNA = STARNETDNA, Status = status, ErrorMessage = errorMesssage });
        }

        protected void RaiseDownloadStatusChangedEvent(T4 STARNETDNA, STARNETHolonDownloadStatus status, string errorMesssage = "")
        {
            OnDownloadStatusChanged?.Invoke(this, new STARNETHolonDownloadProgressEventArgs() { STARNETDNA = STARNETDNA, Status = status, ErrorMessage = errorMesssage });
        }

        private OASISResult<T> CheckForVersionMismatches<T>(T4 STARNETDNA, ref OASISResult<T> result)
        {
            string message = "The {0} Version ({1}) does not match the current version ({1}). This may lead to issues, it is recommended to make sure the versions match.";

            if (STARNETDNA.STARODKVersion != OASISBootLoader.OASISBootLoader.STARODKVersion)
                OASISErrorHandling.HandleWarning(ref result, string.Format(message, "STARODK", STARNETDNA.STARODKVersion, OASISBootLoader.OASISBootLoader.STARODKVersion));

            if (STARNETDNA.STARRuntimeVersion != OASISBootLoader.OASISBootLoader.STARRuntimeVersion)
                OASISErrorHandling.HandleWarning(ref result, string.Format(message, "STAR Runtime", STARNETDNA.STARRuntimeVersion, OASISBootLoader.OASISBootLoader.STARRuntimeVersion));

            if (STARNETDNA.STARNETVersion != OASISBootLoader.OASISBootLoader.STARNETVersion)
                OASISErrorHandling.HandleWarning(ref result, string.Format(message, "STARNET", STARNETDNA.STARNETVersion, OASISBootLoader.OASISBootLoader.STARNETVersion));

            if (STARNETDNA.STARAPIVersion != OASISBootLoader.OASISBootLoader.STARAPIVersion)
                OASISErrorHandling.HandleWarning(ref result, string.Format(message, "STAR API", STARNETDNA.STARAPIVersion, OASISBootLoader.OASISBootLoader.STARAPIVersion));

            if (STARNETDNA.OASISAPIVersion != OASISBootLoader.OASISBootLoader.OASISAPIVersion)
                OASISErrorHandling.HandleWarning(ref result, string.Format(message, "OASIS Runtime", STARNETDNA.OASISRuntimeVersion, OASISBootLoader.OASISBootLoader.OASISRuntimeVersion));

            if (STARNETDNA.OASISAPIVersion != OASISBootLoader.OASISBootLoader.OASISAPIVersion)
                OASISErrorHandling.HandleWarning(ref result, string.Format(message, "OASIS API", STARNETDNA.OASISAPIVersion, OASISBootLoader.OASISBootLoader.OASISAPIVersion));

            if (STARNETDNA.COSMICVersion != OASISBootLoader.OASISBootLoader.COSMICVersion)
                OASISErrorHandling.HandleWarning(ref result, string.Format(message, "COSMIC", STARNETDNA.COSMICVersion, OASISBootLoader.OASISBootLoader.STARODKVersion));

            if (STARNETDNA.DotNetVersion != OASISBootLoader.OASISBootLoader.DotNetVersion)
                OASISErrorHandling.HandleWarning(ref result, string.Format(message, ".NET", STARNETDNA.DotNetVersion, OASISBootLoader.OASISBootLoader.DotNetVersion));

            return result;
        }

        private async Task<OASISResult<T3>> UninstallAsync(Guid avatarId, OASISResult<T3> installedSTARNETHolonResult, string errorMessage, ProviderType providerType)
        {
            OASISResult<T3> result = new OASISResult<T3>();

            if (installedSTARNETHolonResult != null && !installedSTARNETHolonResult.IsError && installedSTARNETHolonResult.Result != null)
                result = await UninstallAsync(avatarId, installedSTARNETHolonResult.Result, errorMessage, providerType);
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonByMetaDataAsync. Reason: {installedSTARNETHolonResult.Message}");

            return result;
        }

        private OASISResult<T3> Uninstall(Guid avatarId, OASISResult<T3> installedSTARNETHolonResult, string errorMessage, ProviderType providerType)
        {
            OASISResult<T3> result = new OASISResult<T3>();

            if (installedSTARNETHolonResult != null && !installedSTARNETHolonResult.IsError && installedSTARNETHolonResult.Result != null)
                result = Uninstall(avatarId, installedSTARNETHolonResult.Result, errorMessage, providerType);
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonByMetaData. Reason: {installedSTARNETHolonResult.Message}");

            return result;
        }

        private async Task<OASISResult<T3>> InstallDependenciesAsync(Guid avatarId, T1 STARNETHolon, string fullInstallPath, string errorMessage, OASISResult<T3> result, ProviderType providerType = ProviderType.Default)
        {
            result = await InstallDependenciesAsync<InstalledLibrary>(avatarId, STARNETHolon.STARNETDNA.Dependencies.Libraries, "Libs", HolonType.InstalledLibrary, fullInstallPath, STARDNA.DefaultLibsDownloadedPath, STARDNA.DefaultLibsInstalledPath, "Library", errorMessage, result, providerType);
            result = await InstallDependenciesAsync<InstalledRuntime>(avatarId, STARNETHolon.STARNETDNA.Dependencies.Runtimes, "Runtimes", HolonType.InstalledRuntime, fullInstallPath, STARDNA.DefaultRuntimesDownloadedPath, STARDNA.DefaultRuntimesInstalledPath, "Runtime", errorMessage, result, providerType);
            result = await InstallDependenciesAsync<InstalledOAPPTemplate>(avatarId, STARNETHolon.STARNETDNA.Dependencies.Templates, "Templates", HolonType.InstalledOAPPTemplate, fullInstallPath, STARDNA.DefaultOAPPTemplatesDownloadedPath, STARDNA.DefaultOAPPTemplatesInstalledPath, "Template", errorMessage, result, providerType);
            result = await InstallDependenciesAsync<InstalledOAPP>(avatarId, STARNETHolon.STARNETDNA.Dependencies.OAPPs, "OAPPs", HolonType.InstalledOAPP, fullInstallPath, STARDNA.DefaultOAPPsDownloadedPath, STARDNA.DefaultOAPPsInstalledPath, "OAPP", errorMessage, result, providerType);
            result = await InstallDependenciesAsync<InstalledQuest>(avatarId, STARNETHolon.STARNETDNA.Dependencies.Quests, "Quests", HolonType.InstalledQuest, fullInstallPath, STARDNA.DefaultQuestsDownloadedPath, STARDNA.DefaultQuestsInstalledPath, "Quest", errorMessage, result, providerType);
            result = await InstallDependenciesAsync<InstalledMission>(avatarId, STARNETHolon.STARNETDNA.Dependencies.Missions, "Missions", HolonType.InstalledMission, fullInstallPath, STARDNA.DefaultMissionsDownloadedPath, STARDNA.DefaultMissionsInstalledPath, "Mission", errorMessage, result, providerType);
            result = await InstallDependenciesAsync<InstalledChapter>(avatarId, STARNETHolon.STARNETDNA.Dependencies.Chapters, "Chapters", HolonType.InstalledChapter, fullInstallPath, STARDNA.DefaultChaptersDownloadedPath, STARDNA.DefaultChaptersInstalledPath, "Chapter", errorMessage, result, providerType);
            result = await InstallDependenciesAsync<InstalledNFT>(avatarId, STARNETHolon.STARNETDNA.Dependencies.NFTs, "NFTs", HolonType.InstalledNFT, fullInstallPath, STARDNA.DefaultNFTsDownloadedPath, STARDNA.DefaultNFTsInstalledPath, "NFT", errorMessage, result, providerType);
            result = await InstallDependenciesAsync<InstalledGeoNFT>(avatarId, STARNETHolon.STARNETDNA.Dependencies.GeoNFTs, "GeoNFTs", HolonType.InstalledGeoNFT, fullInstallPath, STARDNA.DefaultGeoNFTsDownloadedPath, STARDNA.DefaultGeoNFTsInstalledPath, "GeoNFT", errorMessage, result, providerType);
            result = await InstallDependenciesAsync<InstalledGeoNFTCollection>(avatarId, STARNETHolon.STARNETDNA.Dependencies.GeoNFTCollections, "GeoNFTCollections", HolonType.InstalledGeoNFTCollection, fullInstallPath, STARDNA.DefaultGeoNFTCollectionsDownloadedPath, STARDNA.DefaultGeoNFTCollectionsInstalledPath, "GeoNFTCollection", errorMessage, result, providerType);
            result = await InstallDependenciesAsync<InstalledGeoHotSpot>(avatarId, STARNETHolon.STARNETDNA.Dependencies.GeoHotSpots, "GeoHotSpots", HolonType.InstalledGeoHotSpot, fullInstallPath, STARDNA.DefaultGeoHotSpotsDownloadedPath, STARDNA.DefaultGeoHotSpotsInstalledPath, "GeoHotSpot", errorMessage, result, providerType);
            result = await InstallDependenciesAsync<InstalledCelestialSpace>(avatarId, STARNETHolon.STARNETDNA.Dependencies.CelestialSpaces, "CelestialSpaces", HolonType.InstalledCelestialSpace, fullInstallPath, STARDNA.DefaultCelestialSpacesDownloadedPath, STARDNA.DefaultCelestialSpacesInstalledPath, "CelestialSpace", errorMessage, result, providerType);
            result = await InstallDependenciesAsync<InstalledCelestialBody>(avatarId, STARNETHolon.STARNETDNA.Dependencies.CelestialBodies, "CelestialBodies", HolonType.InstalledCelestialBody, fullInstallPath, STARDNA.DefaultCelestialBodiesDownloadedPath, STARDNA.DefaultCelestialBodiesInstalledPath, "CelestialBody", errorMessage, result, providerType);
            result = await InstallDependenciesAsync<InstalledZome>(avatarId, STARNETHolon.STARNETDNA.Dependencies.Zomes, "Zomes", HolonType.InstalledZome, fullInstallPath, STARDNA.DefaultZomesDownloadedPath, STARDNA.DefaultZomesInstalledPath, "Zome", errorMessage, result, providerType);
            result = await InstallDependenciesAsync<InstalledHolon>(avatarId, STARNETHolon.STARNETDNA.Dependencies.Holons, "Holons", HolonType.InstalledHolon, fullInstallPath, STARDNA.DefaultHolonsDownloadedPath, STARDNA.DefaultHolonsInstalledPath, "Holon", errorMessage, result, providerType);
            result = await InstallDependenciesAsync<InstalledInventoryItem>(avatarId, STARNETHolon.STARNETDNA.Dependencies.InventoryItems, "InventoryItems", HolonType.InstalledInventoryItem, fullInstallPath, STARDNA.DefaultInventoryItemsDownloadedPath, STARDNA.DefaultInventoryItemsInstalledPath, "InventoryItem", errorMessage, result, providerType);
            result = await InstallDependenciesAsync<InstalledCelestialBodyMetaDataDNA>(avatarId, STARNETHolon.STARNETDNA.Dependencies.CelestialBodiesMetaDataDNA, "CelestialBodiesMetaDataDNA", HolonType.InstalledCelestialBodyMetaDataDNA, fullInstallPath, STARDNA.DefaultCelestialBodiesMetaDataDNADownloadedPath, STARDNA.DefaultCelestialBodiesMetaDataDNAInstalledPath, "CelestialBodyMetaDataDNA", errorMessage, result, providerType);
            result = await InstallDependenciesAsync<InstalledZomeMetaDataDNA>(avatarId, STARNETHolon.STARNETDNA.Dependencies.ZomesMetaDataDNA, "ZomesMetaDataDNA", HolonType.InstalledZomeMetaDataDNA, fullInstallPath, STARDNA.DefaultZomesMetaDataDNADownloadedPath, STARDNA.DefaultZomesMetaDataDNAInstalledPath, "ZomeMetaDataDNA", errorMessage, result, providerType);
            result = await InstallDependenciesAsync<InstalledHolonMetaDataDNA>(avatarId, STARNETHolon.STARNETDNA.Dependencies.HolonsMetaDataDNA, "HolonsMetaDataDNA", HolonType.InstalledHolonMetaDataDNA, fullInstallPath, STARDNA.DefaultHolonsMetaDataDNADownloadedPath, STARDNA.DefaultHolonsMetaDataDNAInstalledPath, "HolonMetaDataDNA", errorMessage, result, providerType);

            return result;
        }

        private OASISResult<T3> InstallDependencies(Guid avatarId, T1 STARNETHolon, string fullInstallPath, string errorMessage, OASISResult<T3> result, ProviderType providerType = ProviderType.Default)
        {
            result = InstallDependencies<InstalledLibrary>(avatarId, STARNETHolon.STARNETDNA.Dependencies.Libraries, "Libs", HolonType.InstalledLibrary, fullInstallPath, STARDNA.DefaultLibsDownloadedPath, STARDNA.DefaultLibsInstalledPath, "Library", errorMessage, result, providerType);
            result = InstallDependencies<InstalledRuntime>(avatarId, STARNETHolon.STARNETDNA.Dependencies.Runtimes, "Runtimes", HolonType.InstalledRuntime, fullInstallPath, STARDNA.DefaultRuntimesDownloadedPath, STARDNA.DefaultRuntimesInstalledPath, "Runtime", errorMessage, result, providerType);
            result = InstallDependencies<InstalledOAPPTemplate>(avatarId, STARNETHolon.STARNETDNA.Dependencies.Templates, "Templates", HolonType.InstalledOAPPTemplate, fullInstallPath, STARDNA.DefaultOAPPTemplatesDownloadedPath, STARDNA.DefaultOAPPTemplatesInstalledPath, "Template", errorMessage, result, providerType);
            result = InstallDependencies<InstalledOAPP>(avatarId, STARNETHolon.STARNETDNA.Dependencies.OAPPs, "OAPPs", HolonType.InstalledOAPP, fullInstallPath, STARDNA.DefaultOAPPsDownloadedPath, STARDNA.DefaultOAPPsInstalledPath, "OAPP", errorMessage, result, providerType);
            result = InstallDependencies<InstalledQuest>(avatarId, STARNETHolon.STARNETDNA.Dependencies.Quests, "Quests", HolonType.InstalledQuest, fullInstallPath, STARDNA.DefaultQuestsDownloadedPath, STARDNA.DefaultQuestsInstalledPath, "Quest", errorMessage, result, providerType);
            result = InstallDependencies<InstalledMission>(avatarId, STARNETHolon.STARNETDNA.Dependencies.Missions, "Missions", HolonType.InstalledMission, fullInstallPath, STARDNA.DefaultMissionsDownloadedPath, STARDNA.DefaultMissionsInstalledPath, "Mission", errorMessage, result, providerType);
            result = InstallDependencies<InstalledChapter>(avatarId, STARNETHolon.STARNETDNA.Dependencies.Chapters, "Chapters", HolonType.InstalledChapter, fullInstallPath, STARDNA.DefaultChaptersDownloadedPath, STARDNA.DefaultChaptersInstalledPath, "Chapter", errorMessage, result, providerType);
            result = InstallDependencies<InstalledNFT>(avatarId, STARNETHolon.STARNETDNA.Dependencies.NFTs, "NFTs", HolonType.InstalledNFT, fullInstallPath, STARDNA.DefaultNFTsDownloadedPath, STARDNA.DefaultNFTsInstalledPath, "NFT", errorMessage, result, providerType);
            result = InstallDependencies<InstalledGeoNFT>(avatarId, STARNETHolon.STARNETDNA.Dependencies.GeoNFTs, "GeoNFTs", HolonType.InstalledGeoNFT, fullInstallPath, STARDNA.DefaultGeoNFTsDownloadedPath, STARDNA.DefaultGeoNFTsInstalledPath, "GeoNFT", errorMessage, result, providerType);
            result = InstallDependencies<InstalledGeoNFTCollection>(avatarId, STARNETHolon.STARNETDNA.Dependencies.GeoNFTCollections, "GeoNFTCollections", HolonType.InstalledGeoNFTCollection, fullInstallPath, STARDNA.DefaultGeoNFTCollectionsDownloadedPath, STARDNA.DefaultGeoNFTCollectionsInstalledPath, "GeoNFTCollection", errorMessage, result, providerType);
            result = InstallDependencies<InstalledGeoHotSpot>(avatarId, STARNETHolon.STARNETDNA.Dependencies.GeoHotSpots, "GeoHotSpots", HolonType.InstalledGeoHotSpot, fullInstallPath, STARDNA.DefaultGeoHotSpotsDownloadedPath, STARDNA.DefaultGeoHotSpotsInstalledPath, "GeoHotSpot", errorMessage, result, providerType);
            result = InstallDependencies<InstalledCelestialSpace>(avatarId, STARNETHolon.STARNETDNA.Dependencies.CelestialSpaces, "CelestialSpaces", HolonType.InstalledCelestialSpace, fullInstallPath, STARDNA.DefaultCelestialSpacesDownloadedPath, STARDNA.DefaultCelestialSpacesInstalledPath, "CelestialSpace", errorMessage, result, providerType);
            result = InstallDependencies<InstalledCelestialBody>(avatarId, STARNETHolon.STARNETDNA.Dependencies.CelestialBodies, "CelestialBodies", HolonType.InstalledCelestialBody, fullInstallPath, STARDNA.DefaultCelestialBodiesDownloadedPath, STARDNA.DefaultCelestialBodiesInstalledPath, "CelestialBody", errorMessage, result, providerType);
            result = InstallDependencies<InstalledZome>(avatarId, STARNETHolon.STARNETDNA.Dependencies.Zomes, "Zomes", HolonType.InstalledZome, fullInstallPath, STARDNA.DefaultZomesDownloadedPath, STARDNA.DefaultZomesInstalledPath, "Zome", errorMessage, result, providerType);
            result = InstallDependencies<InstalledHolon>(avatarId, STARNETHolon.STARNETDNA.Dependencies.Holons, "Holons", HolonType.InstalledHolon, fullInstallPath, STARDNA.DefaultHolonsDownloadedPath, STARDNA.DefaultHolonsInstalledPath, "Holon", errorMessage, result, providerType);
            result = InstallDependencies<InstalledInventoryItem>(avatarId, STARNETHolon.STARNETDNA.Dependencies.InventoryItems, "InventoryItems", HolonType.InstalledInventoryItem, fullInstallPath, STARDNA.DefaultInventoryItemsDownloadedPath, STARDNA.DefaultInventoryItemsInstalledPath, "InventoryItem", errorMessage, result, providerType);
            result = InstallDependencies<InstalledCelestialBodyMetaDataDNA>(avatarId, STARNETHolon.STARNETDNA.Dependencies.CelestialBodiesMetaDataDNA, "CelestialBodiesMetaDataDNA", HolonType.InstalledCelestialBodyMetaDataDNA, fullInstallPath, STARDNA.DefaultCelestialBodiesMetaDataDNADownloadedPath, STARDNA.DefaultCelestialBodiesMetaDataDNAInstalledPath, "CelestialBodyMetaDataDNA", errorMessage, result, providerType);
            result = InstallDependencies<InstalledZomeMetaDataDNA>(avatarId, STARNETHolon.STARNETDNA.Dependencies.ZomesMetaDataDNA, "ZomesMetaDataDNA", HolonType.InstalledZomeMetaDataDNA, fullInstallPath, STARDNA.DefaultZomesMetaDataDNADownloadedPath, STARDNA.DefaultZomesMetaDataDNAInstalledPath, "ZomeMetaDataDNA", errorMessage, result, providerType);
            result = InstallDependencies<InstalledHolonMetaDataDNA>(avatarId, STARNETHolon.STARNETDNA.Dependencies.HolonsMetaDataDNA, "HolonsMetaDataDNA", HolonType.InstalledHolonMetaDataDNA, fullInstallPath, STARDNA.DefaultHolonsMetaDataDNADownloadedPath, STARDNA.DefaultHolonsMetaDataDNAInstalledPath, "HolonMetaDataDNA", errorMessage, result, providerType);

            return result;
        }

        private async Task<OASISResult<T3>> InstallDependenciesAsync<T>(Guid avatarId, List<STARNETDependency> dependencies, string dependencyFolder, HolonType installedHolonType, string fullInstallPath, string defaultDownloadPath, string defaultInstallPath, string dependencyDisplayName, string errorMessage, OASISResult<T3> result, ProviderType providerType = ProviderType.Default) where T : IInstalledSTARNETHolon, new()
        {
            foreach (STARNETDependency dependency in dependencies)
            {
                string installPath = Path.Combine(fullInstallPath, "Dependencies", "STARNET", dependencyFolder, string.Concat(dependency.Name, "_v", dependency.Version));

                if (dependency.InstallMode == DependencyInstallMode.Root)
                    installPath = fullInstallPath;

                string dependencyDNAFilePath = Path.Combine(installPath, string.Concat(Enum.GetName(typeof(DependencyType), dependency.Type), "_", dependency.Name, "_v", dependency.Version));
                bool install = false;

                //TODO: When the DNA files are switched over to use OAPPTemplate_SampleTemplate_v1.0.0.json format un-comment this line! :-)
                //if (!File.Exists(dependencyDNAFilePath) && dependency.Install)
                if (!Directory.Exists(installPath) && dependency.Install) //Currently ONLY supports Nested! above line supports both! ;-)
                {
                    if (Directory.Exists(dependency.InstalledFrom))
                        DirectoryHelper.CopyFilesRecursively(dependency.InstalledFrom, installPath);
                    else
                    {
                        OASISResult<T> installedLibResult = await Data.LoadHolonByMetaDataAsync<T>(new Dictionary<string, string>()
                                                        {
                                                            { "Id", dependency.STARNETHolonId.ToString() },
                                                            { "Version", dependency.Version },
                                                            { "Active", "1" }
                                                        }, metaKeyValuePairMatchMode: MetaKeyValuePairMatchMode.All, installedHolonType, providerType: providerType);

                        if (installedLibResult != null && installedLibResult.Result != null && !installedLibResult.IsError)
                        {
                            if (Directory.Exists(installedLibResult.Result.InstalledPath))
                                DirectoryHelper.CopyFilesRecursively(installedLibResult.Result.InstalledPath, installPath);
                            else
                                install = true;
                        }
                        else
                            install = true;

                        if (install)
                        {
                            OASISResult<T> installResult = await InstallDependencyAsync<T>(avatarId, dependency, defaultDownloadPath, defaultInstallPath, dependencyDisplayName, providerType);

                            if (installResult != null && installResult.Result != null && !installResult.IsError)
                            {
                                if (Directory.Exists(installResult.Result.InstalledPath))
                                    DirectoryHelper.CopyFilesRecursively(installResult.Result.InstalledPath, installPath);
                                else
                                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured installing the {dependencyDisplayName} dependency {dependency.Name}. Reason: {installResult.Message}");
                            }
                        }
                    }
                }
            }

            return result;
        }

        private OASISResult<T3> InstallDependencies<T>(Guid avatarId, List<STARNETDependency> dependencies, string dependencyFolder, HolonType installedHolonType, string fullInstallPath, string defaultDownloadPath, string defaultInstallPath, string dependencyDisplayName, string errorMessage, OASISResult<T3> result, ProviderType providerType = ProviderType.Default) where T : IInstalledSTARNETHolon, new()
        {
            foreach (STARNETDependency dependency in dependencies)
            {
                string installPath = Path.Combine(fullInstallPath, "Dependencies", "STARNET", dependencyFolder, string.Concat(dependency.Name, "_v", dependency.Version));

                if (dependency.InstallMode == DependencyInstallMode.Root)
                    installPath = fullInstallPath;

                string dependencyDNAFilePath = Path.Combine(installPath, string.Concat(Enum.GetName(typeof(DependencyType), dependency.Type), "_", dependency.Name, "_v", dependency.Version));
                bool install = false;

                //TODO: When the DNA files are switched over to use OAPPTemplate_SampleTemplate_v1.0.0.json format un-comment this line! :-)
                //if (!File.Exists(dependencyDNAFilePath) && dependency.Install)
                if (!Directory.Exists(installPath) && dependency.Install) //Currently ONLY supports Nested! above line supports both! ;-)
                {
                    if (Directory.Exists(dependency.InstalledFrom))
                        DirectoryHelper.CopyFilesRecursively(dependency.InstalledFrom, installPath);
                    else
                    {
                        OASISResult<T> installedLibResult = Data.LoadHolonByMetaData<T>(new Dictionary<string, string>()
                                                        {
                                                            { "Id", dependency.STARNETHolonId.ToString() },
                                                            { "Version", dependency.Version },
                                                            { "Active", "1" }
                                                        }, metaKeyValuePairMatchMode: MetaKeyValuePairMatchMode.All, installedHolonType, providerType: providerType);

                        if (installedLibResult != null && installedLibResult.Result != null && !installedLibResult.IsError)
                        {
                            if (Directory.Exists(installedLibResult.Result.InstalledPath))
                                DirectoryHelper.CopyFilesRecursively(installedLibResult.Result.InstalledPath, installPath);
                            else
                                install = true;
                        }
                        else
                            install = true;

                        if (install)
                        {
                            OASISResult<T> installResult = InstallDependency<T>(avatarId, dependency, defaultDownloadPath, defaultInstallPath, dependencyDisplayName, providerType);

                            if (installResult != null && installResult.Result != null && !installResult.IsError)
                            {
                                if (Directory.Exists(installResult.Result.InstalledPath))
                                    DirectoryHelper.CopyFilesRecursively(installResult.Result.InstalledPath, installPath);
                                else
                                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured installing the {dependencyDisplayName} dependency {dependency.Name}. Reason: {installResult.Message}");
                            }
                        }
                    }
                }
            }

            return result;
        }

        private OASISResult<IEnumerable<T>> FilterResultsForVersion<T>(Guid avatarId, OASISResult<IEnumerable<T>> results, bool showAllVersions = false, int version = 0) where T : ISTARNETHolon, new()
        {
            OASISResult<IEnumerable<T>> result = new OASISResult<IEnumerable<T>>();
            List<T> holons = new List<T>();

            if (results == null)
                return OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(results, result);
            if (results.IsError || results.Result == null)
            {
                result.Result = results.Result ?? Enumerable.Empty<T>();
                return OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(results, result);
            }

            if (!showAllVersions)
            {
                if (results.Result != null && !results.IsError)
                {
                    if (version == 0) //latest version
                    {
                        Dictionary<string, T> latestVersions = new Dictionary<string, T>();
                        string dependencyId = "";
                        int latestVersion = 0;
                        int currentVersion = 0;

                        foreach (T oappSystemHolon in results.Result)
                        {
                            if (oappSystemHolon == null)
                                continue;
                            if (oappSystemHolon.MetaData != null && oappSystemHolon.MetaData.ContainsKey(STARNETHolonIdName) && oappSystemHolon.MetaData[STARNETHolonIdName] != null)
                                dependencyId = oappSystemHolon.MetaData[STARNETHolonIdName].ToString();

                            latestVersion = latestVersions.ContainsKey(dependencyId) && latestVersions[dependencyId]?.STARNETDNA?.Version != null
                                ? Convert.ToInt32(latestVersions[dependencyId].STARNETDNA.Version.Replace(".", ""))
                                : 0;
                            currentVersion = (oappSystemHolon.STARNETDNA != null && !string.IsNullOrEmpty(oappSystemHolon.STARNETDNA.Version))
                                ? Convert.ToInt32(oappSystemHolon.STARNETDNA.Version.Replace(".", ""))
                                : 0;

                            if (latestVersions.ContainsKey(dependencyId) &&
                                currentVersion > latestVersion
                                || !latestVersions.ContainsKey(dependencyId))
                                latestVersions[dependencyId] = oappSystemHolon;
                        }

                        result.Result = latestVersions.Values.ToList();
                    }
                    else
                    {
                        List<T> filteredList = new List<T>();
                        //filteredList = results.Result.ToList();

                        foreach (T oappSystemHolon in results.Result)
                        {
                            if (oappSystemHolon?.MetaData != null && oappSystemHolon.MetaData.ContainsKey("VersionSequence") && oappSystemHolon.MetaData["VersionSequence"]?.ToString() == version.ToString())
                                filteredList.Add(oappSystemHolon);
                        }

                        result.Result = filteredList;
                    }
                }
            }
            else
                result.Result = results.Result;

            //Filter out any items that are not created by the avatar or published on STARNET.
            if (results.Result != null && result.Result != null)
            {
                holons = result.Result.ToList();

                foreach (T oappSystemHolon in result.Result)
                {
                    if (oappSystemHolon?.STARNETDNA == null)
                        continue;
                    if (oappSystemHolon.STARNETDNA.CreatedByAvatarId != avatarId)
                    {
                        if (oappSystemHolon.STARNETDNA.PublishedOn == DateTime.MinValue)
                            holons.Remove(oappSystemHolon);
                    }
                }
            }

            result.Result = holons;
            result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(results, result);
            return result;
        }

        private OASISResult<T1> Update(Guid avatarId, T1 holon, OASISResult<T1> result, string errorMessage, bool updateDNAJSONFile = false, string STARNETDNAJSONName = "Default", ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> questResult = Update(avatarId, holon, updateDNAJSONFile, STARNETDNAJSONName, providerType);
            OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(questResult, result);

            if (questResult != null && questResult.Result != null && !questResult.IsError)
                result.Result = questResult.Result;
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured saving the {STARNETHolonUIName} with OAPPManagerBase.Update. Reason: {questResult.Message}");

            return result;
        }

        private async Task<OASISResult<T1>> UpdateAsync(Guid avatarId, T1 holon, OASISResult<T1> result, string errorMessage, bool updateDNAJSONFile = false, string STARNETDNAJSONName = "Default", ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> questResult = await UpdateAsync(avatarId, holon, updateDNAJSONFile, STARNETDNAJSONName, providerType);
            OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(questResult, result);

            if (questResult != null && questResult.Result != null && !questResult.IsError)
                result.Result = questResult.Result;
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured saving the {STARNETHolonUIName} with OAPPManagerBase.Update. Reason: {questResult.Message}");

            return result;
        }

        private OASISResult<(T1, string)> AddDependency<T>(T1 parent, T installedDependency, DependencyType dependencyType, string errorMessage, bool installDependency = true, DependencyInstallMode dependencyInstallMode = DependencyInstallMode.Nested) where T : IInstalledSTARNETHolon, new()
        {
            OASISResult<(T1, string)> result = new OASISResult<(T1, string)>();
            string dependencyFolderName = Enum.GetName(typeof(DependencyType), dependencyType);

            switch (dependencyType)
            {
                case DependencyType.CelestialBodyMetaDataDNA:
                    dependencyFolderName = "CelestialBodiesMetaDataDNA";
                    break;

                case DependencyType.ZomeMetaDataDNA:
                    dependencyFolderName = "ZomesMetaDataDNA";
                    break;

                case DependencyType.HolonMetaDataDNA:
                    dependencyFolderName = "HolonsMetaDataDNA";
                    break;

                case DependencyType.Library:
                    dependencyFolderName = "Libs";
                    break;

                case DependencyType.CelestialBody:
                    dependencyFolderName = "CelestialBodies";
                    break;

                default:
                    dependencyFolderName = string.Concat(dependencyFolderName, "s");
                    break;
            }

            string installPath = Path.Combine(parent.STARNETDNA.SourcePath, "Dependencies", "STARNET", dependencyFolderName, string.Concat(installedDependency.STARNETDNA.Name, "_v", installedDependency.STARNETDNA.Version));

            //TODO: Need to change the DNA files to use the name and version so instead of OAPPTemplate.DNA it would be OAPPTemplate_SampleTemplate_v1.0.0.json.
            if (dependencyInstallMode == DependencyInstallMode.Root)
                installPath = parent.STARNETDNA.SourcePath;

            bool found = false;

            switch (dependencyType)
            {
                case DependencyType.CelestialBodyMetaDataDNA:
                    found = parent.STARNETDNA.Dependencies.CelestialBodiesMetaDataDNA.Any(x => x.HolonId == installedDependency.Id);
                    break;

                case DependencyType.ZomeMetaDataDNA:
                    found = parent.STARNETDNA.Dependencies.ZomesMetaDataDNA.Any(x => x.HolonId == installedDependency.Id);
                    break;

                case DependencyType.HolonMetaDataDNA:
                    found = parent.STARNETDNA.Dependencies.HolonsMetaDataDNA.Any(x => x.HolonId == installedDependency.Id);
                    break;

                case DependencyType.Runtime:
                    found = parent.STARNETDNA.Dependencies.Runtimes.Any(x => x.HolonId == installedDependency.Id);
                    break;

                case DependencyType.Library:
                    found = parent.STARNETDNA.Dependencies.Libraries.Any(x => x.HolonId == installedDependency.Id);
                    break;

                case DependencyType.Template:
                    found = parent.STARNETDNA.Dependencies.Templates.Any(x => x.HolonId == installedDependency.Id);
                    break;

                case DependencyType.OAPP:
                    found = parent.STARNETDNA.Dependencies.OAPPs.Any(x => x.HolonId == installedDependency.Id);
                    break;

                case DependencyType.Zome:
                    found = parent.STARNETDNA.Dependencies.Zomes.Any(x => x.HolonId == installedDependency.Id);
                    break;

                case DependencyType.Holon:
                    found = parent.STARNETDNA.Dependencies.Holons.Any(x => x.HolonId == installedDependency.Id);
                    break;

                case DependencyType.CelestialBody:
                    found = parent.STARNETDNA.Dependencies.CelestialBodies.Any(x => x.HolonId == installedDependency.Id);
                    break;

                case DependencyType.CelestialSpace:
                    found = parent.STARNETDNA.Dependencies.CelestialSpaces.Any(x => x.HolonId == installedDependency.Id);
                    break;

                case DependencyType.Quest:
                    found = parent.STARNETDNA.Dependencies.Quests.Any(x => x.HolonId == installedDependency.Id);
                    break;

                case DependencyType.Mission:
                    found = parent.STARNETDNA.Dependencies.Missions.Any(x => x.HolonId == installedDependency.Id);
                    break;

                case DependencyType.Chapter:
                    found = parent.STARNETDNA.Dependencies.Chapters.Any(x => x.HolonId == installedDependency.Id);
                    break;

                case DependencyType.NFT:
                    found = parent.STARNETDNA.Dependencies.NFTs.Any(x => x.HolonId == installedDependency.Id);
                    break;

                case DependencyType.GeoNFT:
                    found = parent.STARNETDNA.Dependencies.GeoNFTs.Any(x => x.HolonId == installedDependency.Id);
                    break;

                case DependencyType.NFTCollection:
                    found = parent.STARNETDNA.Dependencies.NFTCollections.Any(x => x.HolonId == installedDependency.Id);
                    break;

                case DependencyType.GeoNFTCollection:
                    found = parent.STARNETDNA.Dependencies.GeoNFTCollections.Any(x => x.HolonId == installedDependency.Id);
                    break;

                case DependencyType.GeoHotSpot:
                    found = parent.STARNETDNA.Dependencies.GeoHotSpots.Any(x => x.HolonId == installedDependency.Id);
                    break;

                case DependencyType.InventoryItem:
                    found = parent.STARNETDNA.Dependencies.InventoryItems.Any(x => x.HolonId == installedDependency.Id);
                    break;
            }

            if (!found)
            {
                STARNETDependency dependency = new STARNETDependency()
                {
                    HolonId = installedDependency.Id,
                    STARNETHolonId = installedDependency.STARNETDNA.Id,
                    Name = installedDependency.STARNETDNA.Name,
                    Description = installedDependency.STARNETDNA.Description,
                    VersionSequence = installedDependency.STARNETDNA.VersionSequence,
                    Version = installedDependency.STARNETDNA.Version,
                    InstalledFrom = installedDependency.InstalledPath,
                    InstalledTo = installPath,
                    Install = installDependency,
                    InstallMode = dependencyInstallMode
                };

                switch (dependencyType)
                {
                    case DependencyType.CelestialBodyMetaDataDNA:
                        parent.STARNETDNA.Dependencies.CelestialBodiesMetaDataDNA.Add(dependency);
                        break;

                    case DependencyType.ZomeMetaDataDNA:
                        parent.STARNETDNA.Dependencies.ZomesMetaDataDNA.Add(dependency);
                        break;

                    case DependencyType.HolonMetaDataDNA:
                        parent.STARNETDNA.Dependencies.HolonsMetaDataDNA.Add(dependency);
                        break;

                    case DependencyType.Runtime:
                        parent.STARNETDNA.Dependencies.Runtimes.Add(dependency);
                        break;

                    case DependencyType.Library:
                        parent.STARNETDNA.Dependencies.Libraries.Add(dependency);
                        break;

                    case DependencyType.Template:
                        parent.STARNETDNA.Dependencies.Templates.Add(dependency);
                        break;

                    case DependencyType.OAPP:
                        parent.STARNETDNA.Dependencies.OAPPs.Add(dependency);
                        break;

                    case DependencyType.Zome:
                        parent.STARNETDNA.Dependencies.Zomes.Add(dependency);
                        break;

                    case DependencyType.Holon:
                        parent.STARNETDNA.Dependencies.Holons.Add(dependency);
                        break;

                    case DependencyType.CelestialBody:
                        parent.STARNETDNA.Dependencies.CelestialBodies.Add(dependency);
                        break;

                    case DependencyType.CelestialSpace:
                        parent.STARNETDNA.Dependencies.CelestialSpaces.Add(dependency);
                        break;

                    case DependencyType.Quest:
                        parent.STARNETDNA.Dependencies.Quests.Add(dependency);
                        break;

                    case DependencyType.Mission:
                        parent.STARNETDNA.Dependencies.Missions.Add(dependency);
                        break;

                    case DependencyType.Chapter:
                        parent.STARNETDNA.Dependencies.Chapters.Add(dependency);
                        break;

                    case DependencyType.NFT:
                        parent.STARNETDNA.Dependencies.NFTs.Add(dependency);
                        break;

                    case DependencyType.GeoNFT:
                        parent.STARNETDNA.Dependencies.GeoNFTs.Add(dependency);
                        break;

                    case DependencyType.NFTCollection:
                        parent.STARNETDNA.Dependencies.NFTCollections.Add(dependency);
                        break;

                    case DependencyType.GeoNFTCollection:
                        parent.STARNETDNA.Dependencies.GeoNFTCollections.Add(dependency);
                        break;

                    case DependencyType.GeoHotSpot:
                        parent.STARNETDNA.Dependencies.GeoHotSpots.Add(dependency);
                        break;

                    case DependencyType.InventoryItem:
                        parent.STARNETDNA.Dependencies.InventoryItems.Add(dependency);
                        break;
                }
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} The {Enum.GetName(typeof(DependencyType), dependencyType)} {installedDependency.STARNETDNA.Name} has already been added to {parent.STARNETDNA.Name}.");

            result.Result = (parent, installPath);
            return result;
        }

        private OASISResult<STARNETDependency> RemoveDependency<T>(T1 parent, T installedDependency, DependencyType dependencyType, string errorMessage) where T : IInstalledSTARNETHolon, new()
        {
            OASISResult<STARNETDependency> result = new OASISResult<STARNETDependency>();
            STARNETDependency STARNETDependency = null;

            switch (dependencyType)
            {
                case DependencyType.CelestialBodyMetaDataDNA:
                    STARNETDependency = parent.STARNETDNA.Dependencies.CelestialBodiesMetaDataDNA.FirstOrDefault(x => x.HolonId == installedDependency.Id);
                    break;

                case DependencyType.ZomeMetaDataDNA:
                    STARNETDependency = parent.STARNETDNA.Dependencies.ZomesMetaDataDNA.FirstOrDefault(x => x.HolonId == installedDependency.Id);
                    break;

                case DependencyType.HolonMetaDataDNA:
                    STARNETDependency = parent.STARNETDNA.Dependencies.HolonsMetaDataDNA.FirstOrDefault(x => x.HolonId == installedDependency.Id);
                    break;

                case DependencyType.Runtime:
                    STARNETDependency = parent.STARNETDNA.Dependencies.Runtimes.FirstOrDefault(x => x.HolonId == installedDependency.Id);
                    break;

                case DependencyType.Library:
                    STARNETDependency = parent.STARNETDNA.Dependencies.Libraries.FirstOrDefault(x => x.HolonId == installedDependency.Id);
                    break;

                case DependencyType.Template:
                    STARNETDependency = parent.STARNETDNA.Dependencies.Templates.FirstOrDefault(x => x.HolonId == installedDependency.Id);
                    break;

                case DependencyType.OAPP:
                    STARNETDependency = parent.STARNETDNA.Dependencies.OAPPs.FirstOrDefault(x => x.HolonId == installedDependency.Id);
                    break;

                case DependencyType.Zome:
                    STARNETDependency = parent.STARNETDNA.Dependencies.Zomes.FirstOrDefault(x => x.HolonId == installedDependency.Id);
                    break;

                case DependencyType.Holon:
                    STARNETDependency = parent.STARNETDNA.Dependencies.Holons.FirstOrDefault(x => x.HolonId == installedDependency.Id);
                    break;

                case DependencyType.CelestialBody:
                    STARNETDependency = parent.STARNETDNA.Dependencies.CelestialBodies.FirstOrDefault(x => x.HolonId == installedDependency.Id);
                    break;

                case DependencyType.CelestialSpace:
                    STARNETDependency = parent.STARNETDNA.Dependencies.CelestialSpaces.FirstOrDefault(x => x.HolonId == installedDependency.Id);
                    break;

                case DependencyType.Quest:
                    STARNETDependency = parent.STARNETDNA.Dependencies.Quests.FirstOrDefault(x => x.HolonId == installedDependency.Id);
                    break;

                case DependencyType.Mission:
                    STARNETDependency = parent.STARNETDNA.Dependencies.Missions.FirstOrDefault(x => x.HolonId == installedDependency.Id);
                    break;

                case DependencyType.Chapter:
                    STARNETDependency = parent.STARNETDNA.Dependencies.Chapters.FirstOrDefault(x => x.HolonId == installedDependency.Id);
                    break;

                case DependencyType.NFT:
                    STARNETDependency = parent.STARNETDNA.Dependencies.NFTs.FirstOrDefault(x => x.HolonId == installedDependency.Id);
                    break;

                case DependencyType.GeoNFT:
                    STARNETDependency = parent.STARNETDNA.Dependencies.GeoNFTs.FirstOrDefault(x => x.HolonId == installedDependency.Id);
                    break;

                case DependencyType.NFTCollection:
                    STARNETDependency = parent.STARNETDNA.Dependencies.NFTCollections.FirstOrDefault(x => x.HolonId == installedDependency.Id);
                    break;

                case DependencyType.GeoNFTCollection:
                    STARNETDependency = parent.STARNETDNA.Dependencies.GeoNFTCollections.FirstOrDefault(x => x.HolonId == installedDependency.Id);
                    break;

                case DependencyType.GeoHotSpot:
                    STARNETDependency = parent.STARNETDNA.Dependencies.GeoHotSpots.FirstOrDefault(x => x.HolonId == installedDependency.Id);
                    break;

                case DependencyType.InventoryItem:
                    STARNETDependency = parent.STARNETDNA.Dependencies.InventoryItems.FirstOrDefault(x => x.HolonId == installedDependency.Id);
                    break;
            }

            if (STARNETDependency != null)
            {
                switch (dependencyType)
                {
                    case DependencyType.CelestialBodyMetaDataDNA:
                        parent.STARNETDNA.Dependencies.CelestialBodiesMetaDataDNA.Remove(STARNETDependency);
                        break;

                    case DependencyType.ZomeMetaDataDNA:
                        parent.STARNETDNA.Dependencies.ZomesMetaDataDNA.Remove(STARNETDependency);
                        break;

                    case DependencyType.HolonMetaDataDNA:
                        parent.STARNETDNA.Dependencies.HolonsMetaDataDNA.Remove(STARNETDependency);
                        break;

                    case DependencyType.Runtime:
                        parent.STARNETDNA.Dependencies.Runtimes.Remove(STARNETDependency);
                        break;

                    case DependencyType.Library:
                        parent.STARNETDNA.Dependencies.Libraries.Remove(STARNETDependency);
                        break;

                    case DependencyType.Template:
                        parent.STARNETDNA.Dependencies.Templates.Remove(STARNETDependency);
                        break;

                    case DependencyType.OAPP:
                        parent.STARNETDNA.Dependencies.OAPPs.Remove(STARNETDependency);
                        break;

                    case DependencyType.Zome:
                        parent.STARNETDNA.Dependencies.Zomes.Remove(STARNETDependency);
                        break;

                    case DependencyType.Holon:
                        parent.STARNETDNA.Dependencies.Holons.Remove(STARNETDependency);
                        break;

                    case DependencyType.CelestialBody:
                        parent.STARNETDNA.Dependencies.CelestialBodies.Remove(STARNETDependency);
                        break;

                    case DependencyType.CelestialSpace:
                        parent.STARNETDNA.Dependencies.CelestialSpaces.Remove(STARNETDependency);
                        break;

                    case DependencyType.Quest:
                        parent.STARNETDNA.Dependencies.Quests.Remove(STARNETDependency);
                        break;

                    case DependencyType.Mission:
                        parent.STARNETDNA.Dependencies.Missions.Remove(STARNETDependency);
                        break;

                    case DependencyType.Chapter:
                        parent.STARNETDNA.Dependencies.Chapters.Remove(STARNETDependency);
                        break;

                    case DependencyType.NFT:
                        parent.STARNETDNA.Dependencies.NFTs.Remove(STARNETDependency);
                        break;

                    case DependencyType.GeoNFT:
                        parent.STARNETDNA.Dependencies.GeoNFTs.Remove(STARNETDependency);
                        break;

                    case DependencyType.NFTCollection:
                        parent.STARNETDNA.Dependencies.NFTCollections.Remove(STARNETDependency);
                        break;

                    case DependencyType.GeoNFTCollection:
                        parent.STARNETDNA.Dependencies.GeoNFTCollections.Remove(STARNETDependency);
                        break;

                    case DependencyType.GeoHotSpot:
                        parent.STARNETDNA.Dependencies.GeoHotSpots.Remove(STARNETDependency);
                        break;

                    case DependencyType.InventoryItem:
                        parent.STARNETDNA.Dependencies.InventoryItems.Remove(STARNETDependency);
                        break;
                }
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} The {Enum.GetName(typeof(DependencyType), dependencyType)} {installedDependency.STARNETDNA.Name} was not found installed for {parent.STARNETDNA.Name}.");

            result.Result = STARNETDependency;
            return result;
        }

    }
}