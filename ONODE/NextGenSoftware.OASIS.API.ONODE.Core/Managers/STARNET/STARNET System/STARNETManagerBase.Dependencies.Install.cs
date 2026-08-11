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
    public abstract partial class STARNETManagerBase<T1, T2, T3, T4>
    {
        public OASISResult<T> InstallDependency<T>(Guid avatarId, STARNETDependency dependency, string defaultDownloadPath, string defaultInstallPath, string dependencyDisplayName, ProviderType providerType = ProviderType.Default) where T : IInstalledSTARNETHolon
        {
            OASISResult<T> result = new OASISResult<T>();
            string downloadPath = "";
            string installPath = "";

            if (Path.IsPathRooted(defaultDownloadPath) || string.IsNullOrEmpty(STARDNA.STARNETBasePath))
                downloadPath = defaultDownloadPath;
            else
                downloadPath = Path.Combine(STARDNA.STARNETBasePath, defaultDownloadPath);

            if (Path.IsPathRooted(defaultInstallPath) || string.IsNullOrEmpty(STARDNA.STARNETBasePath))
                installPath = defaultInstallPath;
            else
                installPath = Path.Combine(STARDNA.STARNETBasePath, defaultInstallPath);

            switch (dependency.Type)
            {
                case DependencyType.Runtime:
                    {
                        RuntimeManager runtimeManager = new RuntimeManager(avatarId, STARDNA, OASISDNA);
                        runtimeManager.OnDownloadStatusChanged += RuntimeManager_OnDownloadStatusChanged;
                        runtimeManager.OnInstallStatusChanged += RuntimeManager_OnInstallStatusChanged;
                        OASISResult<InstalledRuntime> installResult = runtimeManager.DownloadAndInstall(avatarId, dependency.STARNETHolonId, dependency.Version, installPath, downloadPath, providerType: providerType);
                        runtimeManager.OnDownloadStatusChanged -= RuntimeManager_OnDownloadStatusChanged;
                        runtimeManager.OnInstallStatusChanged -= RuntimeManager_OnInstallStatusChanged;
                        result.Result = (T)(IInstalledSTARNETHolon)installResult.Result;
                        OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(installResult, result);
                        runtimeManager = null;
                    }
                    break;

                case DependencyType.Library:
                    {
                        LibraryManager libManager = new LibraryManager(avatarId, STARDNA, OASISDNA);
                        libManager.OnDownloadStatusChanged += LibManager_OnDownloadStatusChanged;
                        libManager.OnInstallStatusChanged += LibManager_OnInstallStatusChanged;
                        OASISResult<InstalledLibrary> installResult = libManager.DownloadAndInstall(avatarId, dependency.STARNETHolonId, dependency.Version, installPath, downloadPath, providerType: providerType);
                        libManager.OnDownloadStatusChanged -= LibManager_OnDownloadStatusChanged;
                        libManager.OnInstallStatusChanged -= LibManager_OnInstallStatusChanged;
                        result.Result = (T)(IInstalledSTARNETHolon)installResult.Result;
                        OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(installResult, result);
                        libManager = null;
                    }
                    break;

                case DependencyType.Template:
                    {
                        OAPPTemplateManager templateManager = new OAPPTemplateManager(avatarId, STARDNA, OASISDNA);
                        templateManager.OnDownloadStatusChanged += TemplateManager_OnDownloadStatusChanged;
                        templateManager.OnInstallStatusChanged += TemplateManager_OnInstallStatusChanged;
                        OASISResult<InstalledOAPPTemplate> installResult = templateManager.DownloadAndInstall(avatarId, dependency.STARNETHolonId, dependency.Version, installPath, downloadPath, providerType: providerType);
                        templateManager.OnDownloadStatusChanged -= TemplateManager_OnDownloadStatusChanged;
                        templateManager.OnInstallStatusChanged -= TemplateManager_OnInstallStatusChanged;
                        result.Result = (T)(IInstalledSTARNETHolon)installResult.Result;
                        OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(installResult, result);
                        templateManager = null;
                    }
                    break;

                case DependencyType.OAPP:
                    {
                        OAPPManager OAPPManager = new OAPPManager(avatarId, STARDNA, OASISDNA);
                        OAPPManager.OnDownloadStatusChanged += OAPPManager_OnDownloadStatusChanged;
                        OAPPManager.OnInstallStatusChanged += OAPPManager_OnInstallStatusChanged;
                        OASISResult<InstalledOAPP> installResult = OAPPManager.DownloadAndInstall(avatarId, dependency.STARNETHolonId, dependency.Version, installPath, downloadPath, providerType: providerType);
                        OAPPManager.OnDownloadStatusChanged -= OAPPManager_OnDownloadStatusChanged;
                        OAPPManager.OnInstallStatusChanged -= OAPPManager_OnInstallStatusChanged;
                        result.Result = (T)(IInstalledSTARNETHolon)installResult.Result;
                        OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(installResult, result);
                        OAPPManager = null;
                    }
                    break;

                case DependencyType.Quest:
                    {
                        QuestManager QuestManager = new QuestManager(avatarId, STARDNA, OASISDNA);
                        QuestManager.OnDownloadStatusChanged += QuestManager_OnDownloadStatusChanged;
                        QuestManager.OnInstallStatusChanged += QuestManager_OnInstallStatusChanged;
                        OASISResult<InstalledQuest> installResult = QuestManager.DownloadAndInstall(avatarId, dependency.STARNETHolonId, dependency.Version, installPath, downloadPath, providerType: providerType);
                        QuestManager.OnDownloadStatusChanged -= QuestManager_OnDownloadStatusChanged;
                        QuestManager.OnInstallStatusChanged -= QuestManager_OnInstallStatusChanged;
                        result.Result = (T)(IInstalledSTARNETHolon)installResult.Result;
                        OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(installResult, result);
                        QuestManager = null;
                    }
                    break;

                case DependencyType.Mission:
                    {
                        MissionManager MissionManager = new MissionManager(avatarId, STARDNA, OASISDNA);
                        MissionManager.OnDownloadStatusChanged += MissionManager_OnDownloadStatusChanged;
                        MissionManager.OnInstallStatusChanged += MissionManager_OnInstallStatusChanged;
                        OASISResult<InstalledMission> installResult = MissionManager.DownloadAndInstall(avatarId, dependency.STARNETHolonId, dependency.Version, installPath, downloadPath, providerType: providerType);
                        MissionManager.OnDownloadStatusChanged -= MissionManager_OnDownloadStatusChanged;
                        MissionManager.OnInstallStatusChanged -= MissionManager_OnInstallStatusChanged;
                        result.Result = (T)(IInstalledSTARNETHolon)installResult.Result;
                        OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(installResult, result);
                        MissionManager = null;
                    }
                    break;

                case DependencyType.Chapter:
                    {
                        ChapterManager ChapterManager = new ChapterManager(avatarId, STARDNA, OASISDNA);
                        ChapterManager.OnDownloadStatusChanged += ChapterManager_OnDownloadStatusChanged;
                        ChapterManager.OnInstallStatusChanged += ChapterManager_OnInstallStatusChanged;
                        OASISResult<InstalledChapter> installResult = ChapterManager.DownloadAndInstall(avatarId, dependency.STARNETHolonId, dependency.Version, installPath, downloadPath, providerType: providerType);
                        ChapterManager.OnDownloadStatusChanged -= ChapterManager_OnDownloadStatusChanged;
                        ChapterManager.OnInstallStatusChanged -= ChapterManager_OnInstallStatusChanged;
                        result.Result = (T)(IInstalledSTARNETHolon)installResult.Result;
                        OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(installResult, result);
                        ChapterManager = null;
                    }
                    break;

                case DependencyType.NFT:
                    {
                        STARNFTManager STARNFTManager = new STARNFTManager(avatarId, STARDNA, OASISDNA);
                        STARNFTManager.OnDownloadStatusChanged += NFTManager_OnDownloadStatusChanged;
                        STARNFTManager.OnInstallStatusChanged += NFTManager_OnInstallStatusChanged;
                        OASISResult<InstalledNFT> installResult = STARNFTManager.DownloadAndInstall(avatarId, dependency.STARNETHolonId, dependency.Version, installPath, downloadPath, providerType: providerType);
                        STARNFTManager.OnDownloadStatusChanged -= NFTManager_OnDownloadStatusChanged;
                        STARNFTManager.OnInstallStatusChanged -= NFTManager_OnInstallStatusChanged;
                        result.Result = (T)(IInstalledSTARNETHolon)installResult.Result;
                        OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(installResult, result);
                        STARNFTManager = null;
                    }
                    break;

                case DependencyType.GeoNFT:
                    {
                        STARGeoNFTManager STARGeoNFTManager = new STARGeoNFTManager(avatarId, STARDNA, OASISDNA);
                        STARGeoNFTManager.OnDownloadStatusChanged += GeoNFTManager_OnDownloadStatusChanged;
                        STARGeoNFTManager.OnInstallStatusChanged += GeoNFTManager_OnInstallStatusChanged;
                        OASISResult<InstalledGeoNFT> installResult = STARGeoNFTManager.DownloadAndInstall(avatarId, dependency.STARNETHolonId, dependency.Version, installPath, downloadPath, providerType: providerType);
                        STARGeoNFTManager.OnDownloadStatusChanged -= GeoNFTManager_OnDownloadStatusChanged;
                        STARGeoNFTManager.OnInstallStatusChanged -= GeoNFTManager_OnInstallStatusChanged;
                        result.Result = (T)(IInstalledSTARNETHolon)installResult.Result;
                        OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(installResult, result);
                        STARGeoNFTManager = null;
                    }
                    break;

                case DependencyType.NFTCollection:
                    {
                        STARNFTCollectionManager STARNFTCollectionManager = new STARNFTCollectionManager(avatarId, STARDNA, OASISDNA);
                        STARNFTCollectionManager.OnDownloadStatusChanged += NFTCollectionManager_OnDownloadStatusChanged;
                        STARNFTCollectionManager.OnInstallStatusChanged += NFTCollectionManager_OnInstallStatusChanged;
                        OASISResult<InstalledNFTCollection> installResult = STARNFTCollectionManager.DownloadAndInstall(avatarId, dependency.STARNETHolonId, dependency.Version, installPath, downloadPath, providerType: providerType);
                        STARNFTCollectionManager.OnDownloadStatusChanged -= NFTCollectionManager_OnDownloadStatusChanged;
                        STARNFTCollectionManager.OnInstallStatusChanged -= NFTCollectionManager_OnInstallStatusChanged;
                        result.Result = (T)(IInstalledSTARNETHolon)installResult.Result;
                        OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(installResult, result);
                        STARNFTCollectionManager = null;
                    }
                    break;

                case DependencyType.GeoNFTCollection:
                    {
                        STARGeoNFTCollectionManager STARGeoNFTCollectionManager = new STARGeoNFTCollectionManager(avatarId, STARDNA, OASISDNA);
                        STARGeoNFTCollectionManager.OnDownloadStatusChanged += GeoNFTCollectionManager_OnDownloadStatusChanged;
                        STARGeoNFTCollectionManager.OnInstallStatusChanged += GeoNFTCollectionManager_OnInstallStatusChanged;
                        OASISResult<InstalledGeoNFTCollection> installResult = STARGeoNFTCollectionManager.DownloadAndInstall(avatarId, dependency.STARNETHolonId, dependency.Version, installPath, downloadPath, providerType: providerType);
                        STARGeoNFTCollectionManager.OnDownloadStatusChanged -= GeoNFTCollectionManager_OnDownloadStatusChanged;
                        STARGeoNFTCollectionManager.OnInstallStatusChanged -= GeoNFTCollectionManager_OnInstallStatusChanged;
                        result.Result = (T)(IInstalledSTARNETHolon)installResult.Result;
                        OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(installResult, result);
                        STARGeoNFTCollectionManager = null;
                    }
                    break;

                case DependencyType.GeoHotSpot:
                    {
                        GeoHotSpotManager GeoHotSpotManager = new GeoHotSpotManager(avatarId, STARDNA, OASISDNA);
                        GeoHotSpotManager.OnDownloadStatusChanged += GeoHotSpotManager_OnDownloadStatusChanged;
                        GeoHotSpotManager.OnInstallStatusChanged += GeoHotSpotManager_OnInstallStatusChanged;
                        OASISResult<InstalledGeoHotSpot> installResult = GeoHotSpotManager.DownloadAndInstall(avatarId, dependency.STARNETHolonId, dependency.Version, installPath, downloadPath, providerType: providerType);
                        GeoHotSpotManager.OnDownloadStatusChanged -= GeoHotSpotManager_OnDownloadStatusChanged;
                        GeoHotSpotManager.OnInstallStatusChanged -= GeoHotSpotManager_OnInstallStatusChanged;
                        result.Result = (T)(IInstalledSTARNETHolon)installResult.Result;
                        OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(installResult, result);
                        GeoHotSpotManager = null;
                    }
                    break;

                case DependencyType.CelestialSpace:
                    {
                        CelestialSpaceManager CelestialSpaceManager = new CelestialSpaceManager(avatarId, STARDNA, OASISDNA);
                        CelestialSpaceManager.OnDownloadStatusChanged += CelestialSpaceManager_OnDownloadStatusChanged;
                        CelestialSpaceManager.OnInstallStatusChanged += CelestialSpaceManager_OnInstallStatusChanged;
                        OASISResult<InstalledCelestialSpace> installResult = CelestialSpaceManager.DownloadAndInstall(avatarId, dependency.STARNETHolonId, dependency.Version, installPath, downloadPath, providerType: providerType);
                        CelestialSpaceManager.OnDownloadStatusChanged -= CelestialSpaceManager_OnDownloadStatusChanged;
                        CelestialSpaceManager.OnInstallStatusChanged -= CelestialSpaceManager_OnInstallStatusChanged;
                        result.Result = (T)(IInstalledSTARNETHolon)installResult.Result;
                        OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(installResult, result);
                        CelestialSpaceManager = null;
                    }
                    break;

                case DependencyType.CelestialBody:
                    {
                        CelestialBodyManager CelestialBodyManager = new CelestialBodyManager(avatarId, STARDNA, OASISDNA);
                        CelestialBodyManager.OnDownloadStatusChanged += CelestialBodyManager_OnDownloadStatusChanged;
                        CelestialBodyManager.OnInstallStatusChanged += CelestialBodyManager_OnInstallStatusChanged;
                        OASISResult<InstalledCelestialBody> installResult = CelestialBodyManager.DownloadAndInstall(avatarId, dependency.STARNETHolonId, dependency.Version, installPath, downloadPath, providerType: providerType);
                        CelestialBodyManager.OnDownloadStatusChanged -= CelestialBodyManager_OnDownloadStatusChanged;
                        CelestialBodyManager.OnInstallStatusChanged -= CelestialBodyManager_OnInstallStatusChanged;
                        result.Result = (T)(IInstalledSTARNETHolon)installResult.Result;
                        OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(installResult, result);
                        CelestialBodyManager = null;
                    }
                    break;

                case DependencyType.Zome:
                    {
                        STARZomeManager STARZomeManager = new STARZomeManager(avatarId, STARDNA, OASISDNA);
                        STARZomeManager.OnDownloadStatusChanged += ZomeManager_OnDownloadStatusChanged;
                        STARZomeManager.OnInstallStatusChanged += ZomeManager_OnInstallStatusChanged;
                        OASISResult<InstalledZome> installResult = STARZomeManager.DownloadAndInstall(avatarId, dependency.STARNETHolonId, dependency.Version, installPath, downloadPath, providerType: providerType);
                        STARZomeManager.OnDownloadStatusChanged -= ZomeManager_OnDownloadStatusChanged;
                        STARZomeManager.OnInstallStatusChanged -= ZomeManager_OnInstallStatusChanged;
                        result.Result = (T)(IInstalledSTARNETHolon)installResult.Result;
                        OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(installResult, result);
                        STARZomeManager = null;
                    }
                    break;

                case DependencyType.Holon:
                    {
                        STARHolonManager STARHolonManager = new STARHolonManager(avatarId, STARDNA, OASISDNA);
                        STARHolonManager.OnDownloadStatusChanged += HolonManager_OnDownloadStatusChanged;
                        STARHolonManager.OnInstallStatusChanged += HolonManager_OnInstallStatusChanged;
                        OASISResult<InstalledHolon> installResult = STARHolonManager.DownloadAndInstall(avatarId, dependency.STARNETHolonId, dependency.Version, installPath, downloadPath, providerType: providerType);
                        STARHolonManager.OnDownloadStatusChanged -= HolonManager_OnDownloadStatusChanged;
                        STARHolonManager.OnInstallStatusChanged -= HolonManager_OnInstallStatusChanged;
                        result.Result = (T)(IInstalledSTARNETHolon)installResult.Result;
                        OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(installResult, result);
                        STARHolonManager = null;
                    }
                    break;

                case DependencyType.InventoryItem:
                    {
                        InventoryItemManager InventoryItemManager = new InventoryItemManager(avatarId, STARDNA, OASISDNA);
                        InventoryItemManager.OnDownloadStatusChanged += InventoryItemManager_OnDownloadStatusChanged;
                        InventoryItemManager.OnInstallStatusChanged += InventoryItemManager_OnInstallStatusChanged;
                        OASISResult<InstalledInventoryItem> installResult = InventoryItemManager.DownloadAndInstall(avatarId, dependency.STARNETHolonId, dependency.Version, installPath, downloadPath, providerType: providerType);
                        InventoryItemManager.OnDownloadStatusChanged -= InventoryItemManager_OnDownloadStatusChanged;
                        InventoryItemManager.OnInstallStatusChanged -= InventoryItemManager_OnInstallStatusChanged;
                        result.Result = (T)(IInstalledSTARNETHolon)installResult.Result;
                        OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(installResult, result);
                        InventoryItemManager = null;
                    }
                    break;

                case DependencyType.CelestialBodyMetaDataDNA:
                    {
                        CelestialBodyMetaDataDNAManager CelestialBodyMetaDataDNAManager = new CelestialBodyMetaDataDNAManager(avatarId, STARDNA, OASISDNA);
                        CelestialBodyMetaDataDNAManager.OnDownloadStatusChanged += CelestialBodyMetaDataDNAManager_OnDownloadStatusChanged;
                        CelestialBodyMetaDataDNAManager.OnInstallStatusChanged += CelestialBodyMetaDataDNAManager_OnInstallStatusChanged;
                        OASISResult<InstalledCelestialBodyMetaDataDNA> installResult = CelestialBodyMetaDataDNAManager.DownloadAndInstall(avatarId, dependency.STARNETHolonId, dependency.Version, installPath, downloadPath, providerType: providerType);
                        CelestialBodyMetaDataDNAManager.OnDownloadStatusChanged -= CelestialBodyMetaDataDNAManager_OnDownloadStatusChanged;
                        CelestialBodyMetaDataDNAManager.OnInstallStatusChanged -= CelestialBodyMetaDataDNAManager_OnInstallStatusChanged;
                        result.Result = (T)(IInstalledSTARNETHolon)installResult.Result;
                        OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(installResult, result);
                        CelestialBodyMetaDataDNAManager = null;
                    }
                    break;

                case DependencyType.ZomeMetaDataDNA:
                    {
                        ZomeMetaDataDNAManager ZomeMetaDataDNAManager = new ZomeMetaDataDNAManager(avatarId, STARDNA, OASISDNA);
                        ZomeMetaDataDNAManager.OnDownloadStatusChanged += ZomeMetaDataDNAManager_OnDownloadStatusChanged;
                        ZomeMetaDataDNAManager.OnInstallStatusChanged += ZomeMetaDataDNAManager_OnInstallStatusChanged;
                        OASISResult<InstalledZomeMetaDataDNA> installResult = ZomeMetaDataDNAManager.DownloadAndInstall(avatarId, dependency.STARNETHolonId, dependency.Version, installPath, downloadPath, providerType: providerType);
                        ZomeMetaDataDNAManager.OnDownloadStatusChanged -= ZomeMetaDataDNAManager_OnDownloadStatusChanged;
                        ZomeMetaDataDNAManager.OnInstallStatusChanged -= ZomeMetaDataDNAManager_OnInstallStatusChanged;
                        result.Result = (T)(IInstalledSTARNETHolon)installResult.Result;
                        OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(installResult, result);
                        ZomeMetaDataDNAManager = null;
                    }
                    break;

                case DependencyType.HolonMetaDataDNA:
                    {
                        HolonMetaDataDNAManager HolonMetaDataDNAManager = new HolonMetaDataDNAManager(avatarId, STARDNA, OASISDNA);
                        HolonMetaDataDNAManager.OnDownloadStatusChanged += HolonMetaDataDNAManager_OnDownloadStatusChanged;
                        HolonMetaDataDNAManager.OnInstallStatusChanged += HolonMetaDataDNAManager_OnInstallStatusChanged;
                        OASISResult<InstalledHolonMetaDataDNA> installResult = HolonMetaDataDNAManager.DownloadAndInstall(avatarId, dependency.STARNETHolonId, dependency.Version, installPath, downloadPath, providerType: providerType);
                        HolonMetaDataDNAManager.OnDownloadStatusChanged -= HolonMetaDataDNAManager_OnDownloadStatusChanged;
                        HolonMetaDataDNAManager.OnInstallStatusChanged -= HolonMetaDataDNAManager_OnInstallStatusChanged;
                        result.Result = (T)(IInstalledSTARNETHolon)installResult.Result;
                        OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(installResult, result);
                        HolonMetaDataDNAManager = null;
                    }
                    break;

                default:
                    {
                        OASISErrorHandling.HandleError(ref result, $"Unsupported dependency type: {dependency.Type} for dependency {dependency.Name}.");
                    }
                    break;
            }

            return result;
        }

        public bool IsThereDependencies(ISTARNETDNA STARNETDNA)
        {
            return ListAllDependencies(STARNETDNA).Count > 0;
        }

        public int GetNumberOfDependendies(ISTARNETDNA STARNETDNA)
        {
            return ListAllDependencies(STARNETDNA).Count;
        }

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
    }
}
