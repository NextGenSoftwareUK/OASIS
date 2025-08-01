using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.ONODE.Core.Managers;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Holons;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Managers
{
    public interface IOAPPManager
    {
        //event OAPPManager.OAPPDownloadStatusChanged OnOAPPDownloadStatusChanged;
        //event OAPPManager.OAPPInstallStatusChanged OnOAPPInstallStatusChanged;
        //event OAPPManager.OAPPPublishStatusChanged OnOAPPPublishStatusChanged;
        //event OAPPManager.OAPPUploadStatusChanged OnOAPPUploadStatusChanged;

        //OASISResult<IOAPP> CreateOAPP(Guid avatarId, string name, string description, OAPPType OAPPType, OAPPTemplateType OAPPTemplateType, Guid OAPPId, GenesisType genesisType, string fullPathToOAPP, ICelestialBody celestialBody = null, IEnumerable<IZome> zomes = null, bool checkIfSourcePathExists = true, ProviderType providerType = ProviderType.Default);
        //Task<OASISResult<IOAPP>> CreateOAPPAsync(Guid avatarId, string name, string description, OAPPType OAPPType, OAPPTemplateType OAPPTemplateType, Guid OAPPId, GenesisType genesisType, string fullPathToOAPP, ICelestialBody celestialBody = null, IEnumerable<IZome> zomes = null, bool checkIfSourcePathExists = true, ProviderType providerType = ProviderType.Default);
        OASISResult<IOAPP> PublishOAPP(Guid avatarId, string fullPathToSource, string launchTarget, string fullPathToPublishTo = "", bool edit = false, bool registerOnSTARNET = true, bool dotnetPublish = true, bool generateSource = true, bool uploadSourceToSTARNET = true, bool makeSourcePublic = false, bool generateBinary = true, bool generateSelfContainedBinary = false, bool generateSelfContainedFullBinary = false, bool uploadToCloud = false, bool uploadSelfContainedToCloud = false, bool uploadSelfContainedFullToCloud = false, ProviderType providerType = ProviderType.Default, ProviderType binaryProviderType = ProviderType.IPFSOASIS, ProviderType selfContainedBinaryProviderType = ProviderType.None, ProviderType selfContainedFullBinaryProviderType = ProviderType.None, bool embedRuntimes = false, bool embedLibs = false, bool embedTemplates = false);
        Task<OASISResult<IOAPP>> PublishOAPPAsync(Guid avatarId, string fullPathToSource, string launchTarget, string fullPathToPublishTo = "", bool edit = false, bool registerOnSTARNET = true, bool dotnetPublish = true, bool generateSource = true, bool uploadSourceToSTARNET = true, bool makeSourcePublic = false, bool generateBinary = true, bool generateSelfContainedBinary = false, bool generateSelfContainedFullBinary = false, bool uploadToCloud = false, bool uploadSelfContainedToCloud = false, bool uploadSelfContainedFullToCloud = false, ProviderType providerType = ProviderType.Default, ProviderType binaryProviderType = ProviderType.IPFSOASIS, ProviderType selfContainedBinaryProviderType = ProviderType.None, ProviderType selfContainedFullBinaryProviderType = ProviderType.None, bool embedRuntimes = false, bool embedLibs = false, bool embedTemplates = false);
    }
}