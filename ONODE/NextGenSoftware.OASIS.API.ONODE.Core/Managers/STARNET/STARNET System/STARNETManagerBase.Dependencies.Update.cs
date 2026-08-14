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
