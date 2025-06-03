using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces;
using NextGenSoftware.OASIS.API.ONODE.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Holons;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Managers
{
    public class OAPPManager : OAPPSystemManagerBase<OAPP, DownloadedOAPP, InstalledOAPP>
    {
        public OAPPManager(Guid avatarId, OASISDNA OASISDNA = null) : base(avatarId, OASISDNA,
            HolonType.OAPP,
            HolonType.InstalledOAPP,
            "OAPP",
            "OAPPId",
            "OAPPName",
            "OAPPType",
            "oapp",
            "oasis_oapps",
            "OAPPDNA.json",
            "OAPPDNAJSON")
        { }

        public OAPPManager(IOASISStorageProvider OASISStorageProvider, Guid avatarId, OASISDNA OASISDNA = null) : base(OASISStorageProvider, avatarId, OASISDNA,
            HolonType.OAPP,
            HolonType.InstalledOAPP,
            "OAPP",
            "OAPPId",
            "OAPPName",
            "OAPPType",
            "oapp",
            "oasis_oapps",
            "OAPPDNA.json",
            "OAPPDNAJSON")
        { }

        public async Task<OASISResult<IOAPP>> CreateOAPPAsync(Guid avatarId, string name, string description, OAPPType OAPPType, OAPPTemplateType OAPPTemplateType, Guid OAPPId, GenesisType genesisType, string fullPathToOAPP, ICelestialBody celestialBody = null, IEnumerable<IZome> zomes = null, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.CreateAsync(avatarId, name, description, OAPPType, fullPathToOAPP, new Dictionary<string, object>()
            { 
                { "OAPPTemplateType", OAPPTemplateType },
                { "OAPPId", OAPPId.ToString() },
                { "GenesisType", genesisType.ToString() },
                { "CelestialBody", celestialBody },
                { "Zomes", zomes }
            }, 
            providerType));
        }

        public OASISResult<IOAPP> CreateOAPP(Guid avatarId, string name, string description, OAPPType OAPPType, OAPPTemplateType OAPPTemplateType, Guid OAPPId, GenesisType genesisType, string fullPathToOAPP, ICelestialBody celestialBody = null, IEnumerable<IZome> zomes = null, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.Create(avatarId, name, description, OAPPType, fullPathToOAPP, new Dictionary<string, object>()
            {
                { "OAPPTemplateType", OAPPTemplateType },
                { "OAPPId", OAPPId.ToString() },
                { "GenesisType", genesisType.ToString() },
                { "CelestialBody", celestialBody },
                { "Zomes", zomes }
            },
            providerType));
        }

        #region COSMICManagerBase
        public async Task<OASISResult<IOAPP>> SaveOAPPAsync(Guid avatarId, IOAPP oappTemplate, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.SaveAsync(avatarId, (OAPP)oappTemplate, providerType));
        }

        public OASISResult<IOAPP> SaveOAPP(Guid avatarId, IOAPP oappTemplate, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.Save(avatarId, (OAPP)oappTemplate, providerType));
        }

        public async Task<OASISResult<IOAPP>> LoadOAPPAsync(Guid avatarId, Guid OAPPId, int version = 0, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.LoadAsync(avatarId, OAPPId, version, providerType));
        }

        public OASISResult<IOAPP> LoadOAPP(Guid avatarId, Guid OAPPId, int version = 0, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.Load(avatarId, OAPPId, version, providerType));
        }

        public async Task<OASISResult<IEnumerable<IOAPP>>> LoadAllOAPPsAsync(Guid avatarId, OAPPTemplateType OAPPTemplateType = OAPPTemplateType.All, bool showAllVersions = false, int version = 0, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResults(await base.LoadAllAsync(avatarId, OAPPTemplateType, OAPPTemplateType == OAPPTemplateType.All, showAllVersions, version, HolonType.OAPPTemplate, "OAPPTemplate", providerType));
        }

        public OASISResult<IEnumerable<IOAPP>> LoadAllOAPPs(Guid avatarId, OAPPTemplateType OAPPTemplateType = OAPPTemplateType.All, bool showAllVersions = false, int version = 0, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResults(base.LoadAll(avatarId, OAPPTemplateType, OAPPTemplateType == OAPPTemplateType.All, showAllVersions, version, HolonType.OAPPTemplate, "OAPPTemplate", providerType));
        }

        public async Task<OASISResult<IEnumerable<IOAPP>>> LoadAllOAPPsForAvatarAsync(Guid avatarId, bool showAllVersions = false, int version = 0, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResults(await base.LoadAllForAvatarAsync(avatarId, showAllVersions, version, providerType));
        }

        public OASISResult<IEnumerable<IOAPP>> LoadAllOAPPsForAvatar(Guid avatarId, bool showAllVersions = false, int version = 0, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResults(base.LoadAllForAvatar(avatarId, showAllVersions, version, providerType));
        }

        public async Task<OASISResult<IEnumerable<IOAPP>>> SearchOAPPsAsync(Guid avatarId, string searchTerm, bool searchOnlyForCurrentAvatar = true, bool showAllVersions = false, int version = 0, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResults(await base.SearchAsync(avatarId, searchTerm, HolonType.OAPP, searchOnlyForCurrentAvatar, showAllVersions, version, providerType));
        }

        public OASISResult<IEnumerable<IOAPP>> SearchOAPPs(Guid avatarId, string searchTerm, bool searchOnlyForCurrentAvatar = true, bool showAllVersions = false, int version = 0, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResults(base.Search(avatarId, searchTerm, HolonType.OAPP, searchOnlyForCurrentAvatar, showAllVersions, version, providerType));
        }

        public async Task<OASISResult<IOAPP>> DeleteOAPPAsync(Guid avatarId, Guid oappTemplateId, int version, bool softDelete = true, bool deleteDownload = true, bool deleteInstall = true, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.DeleteAsync(avatarId, oappTemplateId, version, softDelete, deleteDownload, deleteInstall, providerType));
        }

        public OASISResult<IOAPP> DeleteOAPP(Guid avatarId, Guid oappTemplateId, int version, bool softDelete = true, bool deleteDownload = true, bool deleteInstall = true, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.Delete(avatarId, oappTemplateId, version, softDelete, deleteDownload, deleteInstall, providerType));
        }

        public async Task<OASISResult<IOAPP>> DeleteOAPPAsync(Guid avatarId, IOAPP oappTemplate, int version, bool softDelete = true, bool deleteDownload = true, bool deleteInstall = true, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.DeleteAsync(avatarId, oappTemplate, version, softDelete, deleteDownload, deleteInstall, providerType));
        }

        public OASISResult<IOAPP> DeleteOAPP(Guid avatarId, IOAPP oappTemplate, int version, bool softDelete = true, bool deleteDownload = true, bool deleteInstall = true, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.Delete(avatarId, oappTemplate, version, softDelete, deleteDownload, deleteInstall, providerType));
        }
        #endregion

        public async Task<OASISResult<IEnumerable<IOAPP>>> LoadOAPPVersionsAsync(Guid OAPPId, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResults(await base.LoadVersionsAsync(OAPPId, providerType));
        }

        public OASISResult<IEnumerable<IOAPP>> LoadOAPPVersions(Guid OAPPId, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResults(base.LoadVersions(OAPPId, providerType));
        }

        public async Task<OASISResult<IOAPP>> LoadOAPPVersionAsync(Guid OAPPId, string version, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.LoadVersionAsync(OAPPId, version, providerType));
        }

        public OASISResult<IOAPP> LoadOAPPVersion(Guid OAPPId, string version, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.LoadVersion(OAPPId, version, providerType));
        }

        public async Task<OASISResult<IOAPP>> EditOAPPAsync(Guid OAPPId, IOAPPSystemHolonDNA newOAPPDNA, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.EditAsync(OAPPId, newOAPPDNA, avatarId, providerType));
        }

        public async Task<OASISResult<IOAPP>> EditOAPPAsync(IOAPP OAPP, IOAPPSystemHolonDNA newOAPPDNA, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.EditAsync(avatarId, (OAPP)OAPP, newOAPPDNA, providerType));
        }

        public async Task<OASISResult<IOAPP>> PublishOAPPAsync(Guid avatarId, string fullPathToOAPP, string launchTarget, string fullPathToPublishTo = "", bool registerOnSTARNET = true, bool generateOAPPBinary = true, bool uploadOAPPToCloud = false, bool edit = false, ProviderType providerType = ProviderType.Default, ProviderType oappBinaryProviderType = ProviderType.IPFSOASIS)
        {
            return ProcessResult(await base.PublishAsync(avatarId, fullPathToOAPP, launchTarget, fullPathToPublishTo, registerOnSTARNET, generateOAPPBinary, uploadOAPPToCloud, edit, providerType, oappBinaryProviderType));
        }

        public async Task<OASISResult<IOAPP>> PublishOAPP(Guid avatarId, string fullPathToOAPP, string launchTarget, string fullPathToPublishTo = "", bool registerOnSTARNET = true, bool generateOAPPBinary = true, bool uploadOAPPToCloud = false, bool edit = false, ProviderType providerType = ProviderType.Default, ProviderType oappBinaryProviderType = ProviderType.IPFSOASIS)
        {
            return ProcessResult(base.Publish(avatarId, fullPathToOAPP, launchTarget, fullPathToPublishTo, registerOnSTARNET, generateOAPPBinary, uploadOAPPToCloud, edit, providerType, oappBinaryProviderType));
        }

        public async Task<OASISResult<IOAPP>> UnpublishOAPPAsync(Guid avatarId, IOAPP OAPP, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.UnpublishAsync(avatarId, (OAPP)OAPP, providerType));
        }

        public OASISResult<IOAPP> UnpublishOAPP(Guid avatarId, IOAPP OAPP, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.Unpublish(avatarId, (OAPP)OAPP, providerType));
        }

        public async Task<OASISResult<IOAPP>> UnpublishOAPPAsync(Guid avatarId, Guid OAPPId, int version, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.UnpublishAsync(avatarId, OAPPId, version, providerType));
        }

        public OASISResult<IOAPP> UnpublishOAPP(Guid avatarId, Guid OAPPId, int version, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.Unpublish(avatarId, OAPPId, version, providerType));
        }

        public async Task<OASISResult<IOAPP>> UnpublishOAPPAsync(Guid avatarId, IOAPPSystemHolonDNA OAPPDNA, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.UnpublishAsync(avatarId, OAPPDNA, providerType));
        }

        public OASISResult<IOAPP> UnpublishOAPP(Guid avatarId, IOAPPSystemHolonDNA OAPPDNA, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.Unpublish(avatarId, OAPPDNA, providerType));
        }

        public async Task<OASISResult<IOAPP>> RepublishOAPPAsync(Guid avatarId, IOAPP OAPP, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.RepublishAsync(avatarId, (OAPP)OAPP, providerType));
        }

        public OASISResult<IOAPP> RepublishOAPP(Guid avatarId, IOAPP OAPP, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.Republish(avatarId, (OAPP)OAPP, providerType));
        }

        public async Task<OASISResult<IOAPP>> RepublishOAPPAsync(Guid avatarId, IOAPPSystemHolonDNA OAPPDNA, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.RepublishAsync(avatarId, OAPPDNA, providerType));
        }

        public OASISResult<IOAPP> RepublishOAPP(Guid avatarId, IOAPPSystemHolonDNA OAPPDNA, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.Republish(avatarId, OAPPDNA, providerType));
        }

        public async Task<OASISResult<IOAPP>> RepublishOAPPAsync(Guid OAPPId, int version, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.RepublishAsync(avatarId, OAPPId, version, providerType));
        }

        public OASISResult<IOAPP> RepublishOAPP(Guid OAPPId, int version, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.Republish(avatarId, OAPPId, version, providerType));
        }

        public async Task<OASISResult<IOAPP>> DeactivateOAPPAsync(Guid avatarId, IOAPP OAPP, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.DeactivateAsync(avatarId, (OAPP)OAPP, providerType));
        }

        public OASISResult<IOAPP> DeactivateOAPP(Guid avatarId, IOAPP OAPP, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.Deactivate(avatarId, (OAPP)OAPP, providerType));
        }

        public async Task<OASISResult<IOAPP>> DeactivateOAPPAsync(Guid OAPPId, int version, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.DeactivateAsync(avatarId, OAPPId, version, providerType));
        }

        public OASISResult<IOAPP> DeactivateOAPP(Guid OAPPId, int version, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.Deactivate(avatarId, OAPPId, version, providerType));
        }

        public async Task<OASISResult<IOAPP>> DeactivateOAPPAsync(Guid avatarId, IOAPPSystemHolonDNA OAPPDNA, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.DeactivateAsync(avatarId, OAPPDNA, providerType));
        }

        public OASISResult<IOAPP> DeactivateOAPP(Guid avatarId, IOAPPSystemHolonDNA OAPPDNA, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.Deactivate(avatarId, OAPPDNA, providerType));
        }

        public async Task<OASISResult<IOAPP>> ActivateOAPPAsync(Guid avatarId, IOAPP OAPP, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.ActivateAsync((OAPP)OAPP, avatarId, providerType));
        }

        public OASISResult<IOAPP> ActivateOAPP(Guid avatarId, IOAPP OAPP, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.Activate((OAPP)OAPP, avatarId, providerType));
        }

        public async Task<OASISResult<IOAPP>> ActivateOAPPAsync(Guid avatarId, IOAPPSystemHolonDNA OAPPDNA, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.ActivateAsync(OAPPDNA, avatarId, providerType));
        }

        public OASISResult<IOAPP> ActivateOAPP(Guid avatarId, IOAPPSystemHolonDNA OAPPDNA, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.Activate(avatarId, OAPPDNA, providerType));
        }

        public async Task<OASISResult<IOAPP>> ActivateOAPPAsync(Guid avatarId, Guid OAPPId, int version, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.ActivateAsync(avatarId, OAPPId, version, providerType));
        }

        public OASISResult<IOAPP> ActivateOAPP(Guid avatarId, Guid OAPPId, int version, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.Activate(avatarId, OAPPId, version, providerType));
        }

        public async Task<OASISResult<IDownloadedOAPP>> DownloadOAPPAsync(Guid avatarId, IOAPP OAPP, string fullDownloadPath, bool reInstall = false, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.DownloadAsync(avatarId, (OAPP)OAPP, fullDownloadPath, reInstall, providerType));
        }

        public OASISResult<IDownloadedOAPP> DownloadOAPP(Guid avatarId, IOAPP OAPP, string fullDownloadPath, bool reInstall = false, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.Download(avatarId, (OAPP)OAPP, fullDownloadPath, reInstall, providerType));
        }

        public async Task<OASISResult<IInstalledOAPP>> DownloadAndInstallOAPPAsync(Guid avatarId, IOAPP OAPP, string fullInstallPath, string fullDownloadPath = "", bool createOAPPDirectory = true, bool reInstall = false, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.DownloadAndInstallAsync(avatarId, (OAPP)OAPP, fullInstallPath, fullDownloadPath, createOAPPDirectory, reInstall, providerType));
        }

        public OASISResult<IInstalledOAPP> DownloadAndInstallOAPP(Guid avatarId, IOAPP OAPP, string fullInstallPath, string fullDownloadPath = "", bool createOAPPDirectory = true, bool reInstall = false, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.DownloadAndInstall(avatarId, (OAPP)OAPP, fullInstallPath, fullDownloadPath, createOAPPDirectory, reInstall, providerType));
        }

        public async Task<OASISResult<IInstalledOAPP>> DownloadAndInstallOAPPAsync(Guid avatarId, Guid OAPPId, int version, string fullInstallPath, string fullDownloadPath = "", bool createOAPPDirectory = true, bool reInstall = false, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.DownloadAndInstallAsync(avatarId, OAPPId, version, fullInstallPath, fullDownloadPath, createOAPPDirectory, reInstall, providerType));
        }

        public async Task<OASISResult<IInstalledOAPP>> DownloadAndInstallOAPP(Guid avatarId, Guid OAPPId, int version, string fullInstallPath, string fullDownloadPath = "", bool createOAPPDirectory = true, bool reInstall = false, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.DownloadAndInstall(avatarId, OAPPId, version, fullInstallPath, fullDownloadPath, createOAPPDirectory, reInstall, providerType));
        }


        public async Task<OASISResult<IInstalledOAPP>> InstallOAPPAsync(Guid avatarId, string fullPathToPublishedOAPPFile, string fullInstallPath, bool createOAPPDirectory = true, IDownloadedOAPP downloadedOAPP = null, bool reInstall = false, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.InstallAsync(avatarId, fullPathToPublishedOAPPFile, fullInstallPath, createOAPPDirectory, downloadedOAPP, reInstall, providerType));
        }

        public OASISResult<IInstalledOAPP> InstallOAPP(Guid avatarId, string fullPathToPublishedOAPPFile, string fullInstallPath, bool createOAPPDirectory = true, IDownloadedOAPP downloadedOAPP = null, bool reInstall = false, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.Install(avatarId, fullPathToPublishedOAPPFile, fullInstallPath, createOAPPDirectory, downloadedOAPP, reInstall, providerType));
        }

        public async Task<OASISResult<IInstalledOAPP>> UninstallOAPPAsync(IInstalledOAPP installedOAPP, Guid avatarId, string errorMessage, ProviderType providerType)
        {
            return ProcessResult(await base.UninstallAsync(avatarId, (InstalledOAPP)installedOAPP, errorMessage, providerType));
        }

        public OASISResult<IInstalledOAPP> UninstallOAPP(Guid avatarId, IInstalledOAPP installedOAPP, string errorMessage, ProviderType providerType)
        {
            return ProcessResult(base.Uninstall(avatarId, (InstalledOAPP)installedOAPP, errorMessage, providerType));
        }

        public async Task<OASISResult<IInstalledOAPP>> UninstallOAPPAsync(Guid avatarId, Guid OAPPId, int versionSequence, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.UninstallAsync(avatarId, OAPPId, versionSequence, providerType));
        }

        public OASISResult<IInstalledOAPP> UninstallOAPP(Guid avatarId, Guid OAPPId, int versionSequence, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.Uninstall(avatarId, OAPPId, versionSequence, providerType));
        }

        public async Task<OASISResult<IInstalledOAPP>> UninstallOAPPAsync(Guid avatarId, Guid OAPPId, string version, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.UninstallAsync(avatarId, OAPPId, version, providerType));
        }

        public OASISResult<IInstalledOAPP> UninstallOAPP(Guid avatarId, Guid OAPPId, string version, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.Uninstall(avatarId, OAPPId, version, providerType));
        }

        public async Task<OASISResult<IInstalledOAPP>> UninstallOAPPAsync(Guid avatarId, string OAPPName, int versionSequence, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.UninstallAsync(avatarId, OAPPName, versionSequence, providerType));
        }

        public OASISResult<IInstalledOAPP> UninstallOAPP(Guid avatarId, string OAPPName, int versionSequence, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.Uninstall(avatarId, OAPPName, versionSequence, providerType));
        }

        public async Task<OASISResult<IInstalledOAPP>> UninstallOAPPAsync(Guid avatarId, string OAPPName, string version, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.UninstallAsync(avatarId, OAPPName, version, providerType));
        }

        public OASISResult<IInstalledOAPP> UninstallOAPP(Guid avatarId, string OAPPName, string version, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.Uninstall(avatarId, OAPPName, version, providerType));
        }

        public async Task<OASISResult<IEnumerable<IInstalledOAPP>>> ListInstalledOAPPsAsync(Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResults(await base.ListInstalledAsync(avatarId, providerType));
        }

        public OASISResult<IEnumerable<IInstalledOAPP>> ListInstalledOAPPs(Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResults(base.ListInstalled(avatarId, providerType));
        }

        public async Task<OASISResult<IEnumerable<IInstalledOAPP>>> ListUnInstalledOAPPsAsync(Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResults(await base.ListUninstalledAsync(avatarId, providerType));
        }

        public OASISResult<IEnumerable<IInstalledOAPP>> ListUnInstalledOAPPs(Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResults(base.ListUninstalled(avatarId, providerType));
        }

        public async Task<OASISResult<IEnumerable<IOAPP>>> ListUnpublishedOAPPsAsync(Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResults(await base.ListUnpublishedAsync(avatarId, providerType));
        }

        public OASISResult<IEnumerable<IOAPP>> ListUnpublishedOAPPs(Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResults(base.ListUnpublished(avatarId, providerType));
        }

        public async Task<OASISResult<IEnumerable<IOAPP>>> ListDeactivatedOAPPsAsync(Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResults(await base.ListDeactivatedAsync(avatarId, providerType));
        }

        public OASISResult<IEnumerable<IOAPP>> ListDeactivatedOAPPs(Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResults(base.ListDeactivated(avatarId, providerType));
        }

        public async Task<OASISResult<bool>> IsOAPPInstalledAsync(Guid avatarId, Guid OAPPId, int versionSequence, ProviderType providerType = ProviderType.Default)
        {
            return await base.IsInstalledAsync(avatarId, OAPPId, versionSequence, providerType);
        }

        public OASISResult<bool> IsOAPPInstalled(Guid avatarId, Guid OAPPId, int versionSequence, ProviderType providerType = ProviderType.Default)
        {
            return base.IsInstalled(avatarId, OAPPId, versionSequence, providerType);
        }

        public async Task<OASISResult<bool>> IsOAPPInstalledAsync(Guid avatarId, Guid OAPPId, string version, ProviderType providerType = ProviderType.Default)
        {
            return await base.IsInstalledAsync(avatarId, OAPPId, version, providerType);
        }

        public OASISResult<bool> IsOAPPInstalled(Guid avatarId, Guid OAPPId, string version, ProviderType providerType = ProviderType.Default)
        {
            return base.IsInstalled(avatarId, OAPPId, version, providerType);
        }

        public async Task<OASISResult<bool>> IsOAPPInstalledAsync(Guid avatarId, string OAPPName, int versionSequence, ProviderType providerType = ProviderType.Default)
        {
            return await base.IsInstalledAsync(avatarId, OAPPName, versionSequence, providerType);
        }

        public OASISResult<bool> IsOAPPInstalled(Guid avatarId, string OAPPName, int versionSequence, ProviderType providerType = ProviderType.Default)
        {
            return base.IsInstalled(avatarId, OAPPName, versionSequence, providerType);
        }

        public async Task<OASISResult<bool>> IsOAPPInstalledAsync(Guid avatarId, string OAPPName, string version, ProviderType providerType = ProviderType.Default)
        {
            return await base.IsInstalledAsync(avatarId, OAPPName, version, providerType);
        }

        public OASISResult<bool> IsOAPPInstalled(Guid avatarId, string OAPPName, string version, ProviderType providerType = ProviderType.Default)
        {
            return base.IsInstalled(avatarId, OAPPName, version, providerType);
        }

        public async Task<OASISResult<IInstalledOAPP>> LoadInstalledOAPPAsync(Guid avatarId, Guid OAPPId, int versionSequence = 0, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.LoadInstalledAsync(avatarId, OAPPId, versionSequence, providerType));
        }

        public OASISResult<IInstalledOAPP> LoadInstalledOAPP(Guid avatarId, Guid OAPPId, int versionSequence = 0, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.LoadInstalled(avatarId, OAPPId, versionSequence, providerType));
        }

        public async Task<OASISResult<IInstalledOAPP>> LoadInstalledOAPPAsync(Guid avatarId, string OAPPName, int versionSequence = 0, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.LoadInstalledAsync(avatarId, OAPPName, versionSequence, providerType));
        }

        public OASISResult<IInstalledOAPP> LoadInstalledOAPP(Guid avatarId, string OAPPName, int versionSequence = 0, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.LoadInstalled(avatarId, OAPPName, versionSequence, providerType));
        }

        public async Task<OASISResult<IInstalledOAPP>> LoadInstalledOAPPAsync(Guid avatarId, Guid OAPPId, string version, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.LoadInstalledAsync(avatarId, OAPPId, version, providerType));
        }

        public OASISResult<IInstalledOAPP> LoadInstalledOAPP(Guid avatarId, Guid OAPPId, string version, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.LoadInstalled(avatarId, OAPPId, version, providerType));
        }

        public async Task<OASISResult<IInstalledOAPP>> LoadInstalledOAPPAsync(Guid avatarId, string OAPPName, string version, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.LoadInstalledAsync(avatarId, OAPPName, version, providerType));
        }

        public OASISResult<IInstalledOAPP> LoadInstalledOAPP(Guid avatarId, string OAPPName, string version, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.LoadInstalled(avatarId, OAPPName, version, providerType));
        }

        public async Task<OASISResult<IInstalledOAPP>> LoadInstalledOAPPAsync(Guid avatarId, Guid OAPPId, bool active, int versionSequence = 0, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.LoadInstalledAsync(avatarId, OAPPId, active, versionSequence, providerType));
        }

        public OASISResult<IInstalledOAPP> LoadInstalledOAPP(Guid avatarId, Guid OAPPId, bool active, int versionSequence = 0, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.LoadInstalled(avatarId, OAPPId, active, versionSequence, providerType));
        }

        public async Task<OASISResult<IInstalledOAPP>> LoadInstalledOAPPAsync(Guid avatarId, string OAPPName, bool active, int versionSequence = 0, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.LoadInstalledAsync(avatarId, OAPPName, active, versionSequence, providerType));
        }

        public OASISResult<IInstalledOAPP> LoadInstalledOAPP(Guid avatarId, string OAPPName, bool active, int versionSequence = 0, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.LoadInstalled(avatarId, OAPPName, active, versionSequence, providerType));
        }

        public async Task<OASISResult<IInstalledOAPP>> LoadInstalledOAPPAsync(Guid avatarId, Guid OAPPId, string version, bool active, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.LoadInstalledAsync(avatarId, OAPPId, version, active, providerType));
        }

        public OASISResult<IInstalledOAPP> LoadInstalledOAPP(Guid avatarId, Guid OAPPId, string version, bool active, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.LoadInstalled(avatarId, OAPPId, version, active, providerType));
        }

        public async Task<OASISResult<IInstalledOAPP>> LoadInstalledOAPPAsync(Guid avatarId, string OAPPName, string version, bool active, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.LoadInstalledAsync(avatarId, OAPPName, version, active, providerType));
        }

        public OASISResult<IInstalledOAPP> LoadInstalledOAPP(Guid avatarId, string OAPPName, string version, bool active, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.LoadInstalled(avatarId, OAPPName, version, active, providerType));
        }

        public OASISResult<IInstalledOAPP> OpenOAPPFolder(Guid avatarId, IInstalledOAPP OAPP)
        {
            return ProcessResult(base.OpenOAPPSystemHolonFolder(avatarId, (InstalledOAPP)OAPP));
        }

        public async Task<OASISResult<IInstalledOAPP>> OpenOAPPFolderAsync(Guid avatarId, Guid OAPPId, int versionSequence, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.OpenOAPPSystemHolonFolderAsync(avatarId, OAPPId, versionSequence, providerType));
        }

        public OASISResult<IInstalledOAPP> OpenOAPPFolder(Guid avatarId, Guid OAPPId, int versionSequence, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.OpenOAPPSystemHolonFolder(avatarId, OAPPId, versionSequence, providerType));
        }

        public async Task<OASISResult<IInstalledOAPP>> OpenOAPPFolderAsync(Guid avatarId, Guid OAPPId, string version, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.OpenOAPPSystemHolonFolderAsync(avatarId, OAPPId, version, providerType));
        }

        public OASISResult<IInstalledOAPP> OpenOAPPFolder(Guid avatarId, Guid OAPPId, string version, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.OpenOAPPSystemHolonFolder(avatarId, OAPPId, version, providerType));
        }

        public async Task<OASISResult<bool>> WriteOAPPDNAAsync(IOAPPSystemHolonDNA OAPPDNA, string fullPathToOAPP)
        {
            return await base.WriteOAPPSystemHolonDNAAsync(OAPPDNA, fullPathToOAPP);
        }

        public OASISResult<bool> WriteOAPPDNA(IOAPPSystemHolonDNA OAPPDNA, string fullPathToOAPP)
        {
            return base.WriteOAPPSystemHolonDNA(OAPPDNA, fullPathToOAPP);
        }

        public async Task<OASISResult<IOAPPSystemHolonDNA>> ReadOAPPDNAFromSourceOrInstalledFolderAsync(string fullPathToOAPPFolder)
        {
            return await base.ReadOAPPSystemHolonDNAFromSourceOrInstallFolderAsync(fullPathToOAPPFolder);
        }

        public OASISResult<IOAPPSystemHolonDNA> ReadOAPPDNAFromSourceOrInstalledFolder(string fullPathToOAPPFolder)
        {
            return base.ReadOAPPSystemHolonDNAFromSourceOrInstallFolder(fullPathToOAPPFolder);
        }

        public async Task<OASISResult<IOAPPSystemHolonDNA>> ReadOAPPDNAFromPublishedOAPPFileAsync(string fullPathToOAPPFolder)
        {
            return await base.ReadOAPPSystemHolonDNAFromPublishedFileAsync(fullPathToOAPPFolder);
        }

        public OASISResult<IOAPPSystemHolonDNA> ReadOAPPDNAFromPublishedOAPPFile(string fullPathToOAPPFolder)
        {
            return base.ReadOAPPSystemHolonDNAFromPublishedFile(fullPathToOAPPFolder);
        }

        private OASISResult<IEnumerable<IOAPP>> ProcessResults(OASISResult<IEnumerable<OAPP>> operationResult)
        {
            OASISResult<IEnumerable<IOAPP>> result = new OASISResult<IEnumerable<IOAPP>>();

            if (operationResult != null && operationResult.Result != null && !operationResult.IsError && operationResult.Result.Count() > 0)
            {
                List<IOAPP> oappTemplates = new List<IOAPP>();

                foreach (IOAPP template in operationResult.Result)
                    oappTemplates.Add(template);

                result.Result = oappTemplates;
            }

            OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(operationResult, result);
            return result;
        }

        private OASISResult<IEnumerable<IInstalledOAPP>> ProcessResults(OASISResult<IEnumerable<InstalledOAPP>> operationResult)
        {
            OASISResult<IEnumerable<IInstalledOAPP>> result = new OASISResult<IEnumerable<IInstalledOAPP>>();

            if (operationResult != null && operationResult.Result != null && !operationResult.IsError && operationResult.Result.Count() > 0)
            {
                List<IInstalledOAPP> oappTemplates = new List<IInstalledOAPP>();

                foreach (IInstalledOAPP template in operationResult.Result)
                    oappTemplates.Add(template);

                result.Result = oappTemplates;
            }

            OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(operationResult, result);
            return result;
        }

        private OASISResult<IOAPP> ProcessResult(OASISResult<OAPP> operationResult)
        {
            OASISResult<IOAPP> result = new OASISResult<IOAPP>();
            result.Result = operationResult.Result;
            OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(operationResult, result);
            return result;
        }

        private OASISResult<IDownloadedOAPP> ProcessResult(OASISResult<DownloadedOAPP> operationResult)
        {
            OASISResult<IDownloadedOAPP> result = new OASISResult<IDownloadedOAPP>();
            result.Result = operationResult.Result;
            OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(operationResult, result);
            return result;
        }

        private OASISResult<IInstalledOAPP> ProcessResult(OASISResult<InstalledOAPP> operationResult)
        {
            OASISResult<IInstalledOAPP> result = new OASISResult<IInstalledOAPP>();
            result.Result = operationResult.Result;
            OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(operationResult, result);
            return result;
        }
    }
}