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
    public class OAPPTemplateManager : OAPPSystemManagerBase<OAPPTemplate, DownloadedOAPPTemplate, InstalledOAPPTemplate>
    {
        public OAPPTemplateManager(Guid avatarId, OASISDNA OASISDNA = null) : base(avatarId, OASISDNA, 
            HolonType.OAPPTemplate, 
            HolonType.InstalledOAPPTemplate, 
            "OAPP Template", 
            "OAPPTemplateId", 
            "OAPPTemplateName", 
            "OAPPTemplateType", 
            "oapptemplate", 
            "oasis_oapptemplates", 
            "OAPPTemplateDNA.json",
            "OAPPTemplateDNAJSON") { }

        public OAPPTemplateManager(IOASISStorageProvider OASISStorageProvider, Guid avatarId, OASISDNA OASISDNA = null) : base(OASISStorageProvider, avatarId, OASISDNA,
            HolonType.OAPPTemplate, 
            HolonType.InstalledOAPPTemplate,
            "OAPP Template",
            "OAPPTemplateId",
            "OAPPTemplateName",
            "OAPPTemplateType",
            "oapptemplate",
            "oasis_oapptemplates",
            "OAPPTemplateDNA.json",
            "OAPPTemplateDNAJSON")
        { }

        public async Task<OASISResult<IOAPPTemplate>> CreateOAPPTemplateAsync(Guid avatarId, string name, string description, OAPPTemplateType OAPPTemplateType, string fullPathToOAPPTemplate, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.CreateAsync(avatarId, name, description, OAPPTemplateType, fullPathToOAPPTemplate, null, providerType));
        }

        public OASISResult<IOAPPTemplate> CreateOAPPTemplate(Guid avatarId, string name, string description, OAPPTemplateType OAPPTemplateType, string fullPathToOAPPTemplate, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.Create(avatarId, name, description, OAPPTemplateType, fullPathToOAPPTemplate, null, providerType));
        }

        #region COSMICManagerBase
        public async Task<OASISResult<IOAPPTemplate>> SaveOAPPTemplateAsync(Guid avatarId, IOAPPTemplate oappTemplate, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.SaveAsync(avatarId, (OAPPTemplate)oappTemplate, providerType));
        }

        public OASISResult<IOAPPTemplate> SaveOAPPTemplate(Guid avatarId, IOAPPTemplate oappTemplate, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.Save(avatarId, (OAPPTemplate)oappTemplate, providerType));
        }

        public async Task<OASISResult<IOAPPTemplate>> LoadOAPPTemplateAsync(Guid avatarId, Guid OAPPTemplateId, int version = 0, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.LoadAsync(avatarId, OAPPTemplateId, version, providerType));
        }

        public OASISResult<IOAPPTemplate> LoadOAPPTemplate(Guid avatarId, Guid OAPPTemplateId, int version = 0, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.Load(avatarId, OAPPTemplateId, version, providerType));
        }

        public async Task<OASISResult<IEnumerable<IOAPPTemplate>>> LoadAllOAPPTemplatesAsync(Guid avatarId, OAPPTemplateType OAPPTemplateType = OAPPTemplateType.All, bool showAllVersions = false, int version = 0, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResults(await base.LoadAllAsync(avatarId, OAPPTemplateType, OAPPTemplateType == OAPPTemplateType.All, showAllVersions, version, HolonType.OAPPTemplate, "OAPPTemplate", providerType));
        }

        public OASISResult<IEnumerable<IOAPPTemplate>> LoadAllOAPPTemplates(Guid avatarId, OAPPTemplateType OAPPTemplateType = OAPPTemplateType.All, bool showAllVersions = false, int version = 0, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResults(base.LoadAll(avatarId, OAPPTemplateType, OAPPTemplateType == OAPPTemplateType.All, showAllVersions, version, HolonType.OAPPTemplate, "OAPPTemplate", providerType));
        }

        public async Task<OASISResult<IEnumerable<IOAPPTemplate>>> LoadAllOAPPTemplatesForAvatarAsync(Guid avatarId, bool showAllVersions = false, int version = 0, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResults(await base.LoadAllForAvatarAsync(avatarId, showAllVersions, version, providerType));
        }

        public OASISResult<IEnumerable<IOAPPTemplate>> LoadAllOAPPTemplatesForAvatar(Guid avatarId, bool showAllVersions = false, int version = 0, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResults(base.LoadAllForAvatar(avatarId, showAllVersions, version, providerType));
        }

        public async Task<OASISResult<IEnumerable<IOAPPTemplate>>> SearchOAPPTemplatesAsync(Guid avatarId, string searchTerm, bool searchOnlyForCurrentAvatar = true, bool showAllVersions = false, int version = 0, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResults(await base.SearchAsync(avatarId, searchTerm, HolonType.OAPPTemplate, searchOnlyForCurrentAvatar, showAllVersions, version, providerType));
        }

        public OASISResult<IEnumerable<IOAPPTemplate>> SearchOAPPTemplates(Guid avatarId, string searchTerm, bool searchOnlyForCurrentAvatar = true, bool showAllVersions = false, int version = 0, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResults(base.Search(avatarId, searchTerm, HolonType.OAPPTemplate, searchOnlyForCurrentAvatar, showAllVersions, version, providerType));
        }

        public async Task<OASISResult<IOAPPTemplate>> DeleteOAPPTemplateAsync(Guid avatarId, Guid oappTemplateId, int version, bool softDelete = true, bool deleteDownload = true, bool deleteInstall = true, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.DeleteAsync(avatarId, oappTemplateId, version, softDelete, deleteDownload, deleteInstall, providerType));
        }

        public OASISResult<IOAPPTemplate> DeleteOAPPTemplate(Guid avatarId, Guid oappTemplateId, int version, bool softDelete = true, bool deleteDownload = true, bool deleteInstall = true, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.Delete(avatarId, oappTemplateId, version, softDelete, deleteDownload, deleteInstall, providerType));
        }

        public async Task<OASISResult<IOAPPTemplate>> DeleteOAPPTemplateAsync(Guid avatarId, IOAPPTemplate oappTemplate, int version, bool softDelete = true, bool deleteDownload = true, bool deleteInstall = true, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.DeleteAsync(avatarId, oappTemplate, version, softDelete, deleteDownload, deleteInstall, providerType));
        }

        public OASISResult<IOAPPTemplate> DeleteOAPPTemplate(Guid avatarId, IOAPPTemplate oappTemplate, int version, bool softDelete = true, bool deleteDownload = true, bool deleteInstall = true, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.Delete(avatarId, oappTemplate, version, softDelete, deleteDownload, deleteInstall, providerType));
        }
        #endregion

        public async Task<OASISResult<IEnumerable<IOAPPTemplate>>> LoadOAPPTemplateVersionsAsync(Guid OAPPTemplateId, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResults(await base.LoadVersionsAsync(OAPPTemplateId, providerType));
        }

        public OASISResult<IEnumerable<IOAPPTemplate>> LoadOAPPTemplateVersions(Guid OAPPTemplateId, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResults(base.LoadVersions(OAPPTemplateId, providerType));
        }

        public async Task<OASISResult<IOAPPTemplate>> LoadOAPPTemplateVersionAsync(Guid OAPPTemplateId, string version, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.LoadVersionAsync(OAPPTemplateId, version, providerType));
        }

        public OASISResult<IOAPPTemplate> LoadOAPPTemplateVersion(Guid OAPPTemplateId, string version, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.LoadVersion(OAPPTemplateId, version, providerType));
        }

        public async Task<OASISResult<IOAPPTemplate>> EditOAPPTemplateAsync(Guid OAPPTemplateId, IOAPPSystemHolonDNA newOAPPTemplateDNA, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.EditAsync(OAPPTemplateId, newOAPPTemplateDNA, avatarId, providerType));
        }

        public async Task<OASISResult<IOAPPTemplate>> EditOAPPTemplateAsync(IOAPPTemplate OAPPTemplate, IOAPPSystemHolonDNA newOAPPTemplateDNA, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.EditAsync(avatarId, (OAPPTemplate)OAPPTemplate, newOAPPTemplateDNA, providerType));
        }

        public async Task<OASISResult<IOAPPTemplate>> PublishOAPPTemplateAsync(Guid avatarId, string fullPathToOAPP, string launchTarget, string fullPathToPublishTo = "", bool registerOnSTARNET = true, bool dotnetPublish = true, bool generateOAPPSource = true, bool uploadOAPPSourceToSTARNET = true, bool makeOAPPSourcePublic = false, bool generateOAPPBinary = true, bool generateOAPPSelfContainedBinary = false, bool generateOAPPSelfContainedFullBinary = false, bool uploadOAPPToCloud = false, bool uploadOAPPSelfContainedToCloud = false, bool uploadOAPPSelfContainedFullToCloud = false, ProviderType providerType = ProviderType.Default, ProviderType oappBinaryProviderType = ProviderType.IPFSOASIS, ProviderType oappSelfContainedBinaryProviderType = ProviderType.None, ProviderType oappSelfContainedFullBinaryProviderType = ProviderType.None)
        {
            return ProcessResult(await base.PublishAsync(avatarId, fullPathToOAPPTemplate, launchTarget, fullPathToPublishTo, registerOnSTARNET, generateOAPPTemplateBinary, uploadOAPPTemplateToCloud, edit, providerType, oappBinaryProviderType));
        }

        public OASISResult<IOAPPTemplate> PublishOAPPTemplate(Guid avatarId, string fullPathToOAPPTemplate, string launchTarget, string fullPathToPublishTo = "", bool registerOnSTARNET = true, bool generateOAPPTemplateBinary = true, bool uploadOAPPTemplateToCloud = false, bool edit = false, ProviderType providerType = ProviderType.Default, ProviderType oappBinaryProviderType = ProviderType.IPFSOASIS)
        {
            return ProcessResult(base.Publish(avatarId, fullPathToOAPPTemplate, launchTarget, fullPathToPublishTo, registerOnSTARNET, generateOAPPTemplateBinary, uploadOAPPTemplateToCloud, edit, providerType, oappBinaryProviderType));
        }

        public async Task<OASISResult<IOAPPTemplate>> UnpublishOAPPTemplateAsync(Guid avatarId, IOAPPTemplate OAPPTemplate, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.UnpublishAsync(avatarId, (OAPPTemplate)OAPPTemplate, providerType));
        }

        public OASISResult<IOAPPTemplate> UnpublishOAPPTemplate(Guid avatarId, IOAPPTemplate OAPPTemplate, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.Unpublish(avatarId, (OAPPTemplate)OAPPTemplate, providerType));
        }

        public async Task<OASISResult<IOAPPTemplate>> UnpublishOAPPTemplateAsync(Guid avatarId, Guid OAPPTemplateId, int version, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.UnpublishAsync(avatarId, OAPPTemplateId, version, providerType));
        }

        public OASISResult<IOAPPTemplate> UnpublishOAPPTemplate(Guid avatarId, Guid OAPPTemplateId, int version, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.Unpublish(avatarId, OAPPTemplateId, version, providerType));
        }

        public async Task<OASISResult<IOAPPTemplate>> UnpublishOAPPTemplateAsync(Guid avatarId, IOAPPSystemHolonDNA OAPPTemplateDNA    , ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.UnpublishAsync(avatarId, OAPPTemplateDNA, providerType));
        }

        public OASISResult<IOAPPTemplate> UnpublishOAPPTemplate(Guid avatarId, IOAPPSystemHolonDNA OAPPTemplateDNA, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.Unpublish(avatarId, OAPPTemplateDNA, providerType));
        }

        public async Task<OASISResult<IOAPPTemplate>> RepublishOAPPTemplateAsync(Guid avatarId, IOAPPTemplate OAPPTemplate, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.RepublishAsync(avatarId, (OAPPTemplate)OAPPTemplate, providerType));
        }

        public OASISResult<IOAPPTemplate> RepublishOAPPTemplate(Guid avatarId, IOAPPTemplate OAPPTemplate, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.Republish(avatarId, (OAPPTemplate)OAPPTemplate, providerType));
        }

        public async Task<OASISResult<IOAPPTemplate>> RepublishOAPPTemplateAsync(Guid avatarId, IOAPPSystemHolonDNA OAPPTemplateDNA, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.RepublishAsync(avatarId, OAPPTemplateDNA, providerType));
        }

        public OASISResult<IOAPPTemplate> RepublishOAPPTemplate(Guid avatarId, IOAPPSystemHolonDNA OAPPTemplateDNA, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.Republish(avatarId, OAPPTemplateDNA, providerType));
        }

        public async Task<OASISResult<IOAPPTemplate>> RepublishOAPPTemplateAsync(Guid OAPPTemplateId, int version, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.RepublishAsync(avatarId, OAPPTemplateId, version, providerType));
        }

        public OASISResult<IOAPPTemplate> RepublishOAPPTemplate(Guid OAPPTemplateId, int version, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.Republish(avatarId, OAPPTemplateId, version, providerType));
        }

        public async Task<OASISResult<IOAPPTemplate>> DeactivateOAPPTemplateAsync(Guid avatarId, IOAPPTemplate OAPPTemplate, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.DeactivateAsync(avatarId, (OAPPTemplate)OAPPTemplate, providerType));
        }

        public OASISResult<IOAPPTemplate> DeactivateOAPPTemplate(Guid avatarId, IOAPPTemplate OAPPTemplate, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.Deactivate(avatarId, (OAPPTemplate)OAPPTemplate, providerType));
        }

        public async Task<OASISResult<IOAPPTemplate>> DeactivateOAPPTemplateAsync(Guid OAPPTemplateId, int version, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.DeactivateAsync(avatarId, OAPPTemplateId, version, providerType));
        }

        public OASISResult<IOAPPTemplate> DeactivateOAPPTemplate(Guid OAPPTemplateId, int version, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.Deactivate(avatarId, OAPPTemplateId, version, providerType));
        }

        public async Task<OASISResult<IOAPPTemplate>> DeactivateOAPPTemplateAsync(Guid avatarId, IOAPPSystemHolonDNA OAPPTemplateDNA, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.DeactivateAsync(avatarId, OAPPTemplateDNA, providerType));
        }

        public OASISResult<IOAPPTemplate> DeactivateOAPPTemplate(Guid avatarId, IOAPPSystemHolonDNA OAPPTemplateDNA, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.Deactivate(avatarId, OAPPTemplateDNA, providerType));
        }

        public async Task<OASISResult<IOAPPTemplate>> ActivateOAPPTemplateAsync(Guid avatarId, IOAPPTemplate OAPPTemplate, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.ActivateAsync((OAPPTemplate)OAPPTemplate, avatarId, providerType));
        }

        public OASISResult<IOAPPTemplate> ActivateOAPPTemplate(Guid avatarId, IOAPPTemplate OAPPTemplate, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.Activate((OAPPTemplate)OAPPTemplate, avatarId, providerType));
        }

        public async Task<OASISResult<IOAPPTemplate>> ActivateOAPPTemplateAsync(Guid avatarId, IOAPPSystemHolonDNA OAPPTemplateDNA, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.ActivateAsync(OAPPTemplateDNA, avatarId, providerType));
        }

        public OASISResult<IOAPPTemplate> ActivateOAPPTemplate(Guid avatarId, IOAPPSystemHolonDNA OAPPTemplateDNA, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.Activate(avatarId, OAPPTemplateDNA, providerType));
        }

        public async Task<OASISResult<IOAPPTemplate>> ActivateOAPPTemplateAsync(Guid avatarId, Guid OAPPTemplateId, int version, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.ActivateAsync(avatarId, OAPPTemplateId, version, providerType));
        }

        public OASISResult<IOAPPTemplate> ActivateOAPPTemplate(Guid avatarId, Guid OAPPTemplateId, int version, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.Activate(avatarId, OAPPTemplateId, version, providerType));
        }

        public async Task<OASISResult<IDownloadedOAPPTemplate>> DownloadOAPPTemplateAsync(Guid avatarId, IOAPPTemplate OAPPTemplate, string fullDownloadPath, bool reInstall = false, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.DownloadAsync(avatarId, (OAPPTemplate)OAPPTemplate, fullDownloadPath, reInstall, providerType));
        }

        public OASISResult<IDownloadedOAPPTemplate> DownloadOAPPTemplate(Guid avatarId, IOAPPTemplate OAPPTemplate, string fullDownloadPath, bool reInstall = false, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.Download(avatarId, (OAPPTemplate)OAPPTemplate, fullDownloadPath, reInstall, providerType));
        }

        public async Task<OASISResult<IInstalledOAPPTemplate>> DownloadAndInstallOAPPTemplateAsync(Guid avatarId, IOAPPTemplate OAPPTemplate, string fullInstallPath, string fullDownloadPath = "", bool createOAPPTemplateDirectory = true, bool reInstall = false, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.DownloadAndInstallAsync(avatarId, (OAPPTemplate)OAPPTemplate, fullInstallPath, fullDownloadPath, createOAPPTemplateDirectory, reInstall, providerType));
        }

        public OASISResult<IInstalledOAPPTemplate> DownloadAndInstallOAPPTemplate(Guid avatarId, IOAPPTemplate OAPPTemplate, string fullInstallPath, string fullDownloadPath = "", bool createOAPPTemplateDirectory = true, bool reInstall = false, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.DownloadAndInstall(avatarId, (OAPPTemplate)OAPPTemplate, fullInstallPath, fullDownloadPath, createOAPPTemplateDirectory, reInstall, providerType));
        }

        public async Task<OASISResult<IInstalledOAPPTemplate>> DownloadAndInstallOAPPTemplateAsync(Guid avatarId, Guid OAPPTemplateId, int version, string fullInstallPath, string fullDownloadPath = "", bool createOAPPTemplateDirectory = true, bool reInstall = false, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.DownloadAndInstallAsync(avatarId, OAPPTemplateId, version, fullInstallPath, fullDownloadPath, createOAPPTemplateDirectory, reInstall, providerType));
        }

        public async Task<OASISResult<IInstalledOAPPTemplate>> DownloadAndInstallOAPPTemplate(Guid avatarId, Guid OAPPTemplateId, int version, string fullInstallPath, string fullDownloadPath = "", bool createOAPPTemplateDirectory = true, bool reInstall = false, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.DownloadAndInstall(avatarId, OAPPTemplateId, version, fullInstallPath, fullDownloadPath, createOAPPTemplateDirectory, reInstall, providerType));
        }


        public async Task<OASISResult<IInstalledOAPPTemplate>> InstallOAPPTemplateAsync(Guid avatarId, string fullPathToPublishedOAPPTemplateFile, string fullInstallPath, bool createOAPPTemplateDirectory = true, IDownloadedOAPPTemplate downloadedOAPPTemplate = null, bool reInstall = false, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.InstallAsync(avatarId, fullPathToPublishedOAPPTemplateFile, fullInstallPath, createOAPPTemplateDirectory, downloadedOAPPTemplate, reInstall, providerType));
        }

        public OASISResult<IInstalledOAPPTemplate> InstallOAPPTemplate(Guid avatarId, string fullPathToPublishedOAPPTemplateFile, string fullInstallPath, bool createOAPPTemplateDirectory = true, IDownloadedOAPPTemplate downloadedOAPPTemplate = null, bool reInstall = false, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.Install(avatarId, fullPathToPublishedOAPPTemplateFile, fullInstallPath, createOAPPTemplateDirectory, downloadedOAPPTemplate, reInstall, providerType));
        }

        public async Task<OASISResult<IInstalledOAPPTemplate>> UninstallOAPPTemplateAsync(IInstalledOAPPTemplate installedOAPPTemplate, Guid avatarId, string errorMessage, ProviderType providerType)
        {
            return ProcessResult(await base.UninstallAsync(avatarId, (InstalledOAPPTemplate)installedOAPPTemplate, errorMessage, providerType));
        }

        public OASISResult<IInstalledOAPPTemplate> UninstallOAPPTemplate(Guid avatarId, IInstalledOAPPTemplate installedOAPPTemplate, string errorMessage, ProviderType providerType)
        {
            return ProcessResult(base.Uninstall(avatarId, (InstalledOAPPTemplate)installedOAPPTemplate, errorMessage, providerType));
        }

        public async Task<OASISResult<IInstalledOAPPTemplate>> UninstallOAPPTemplateAsync(Guid avatarId, Guid OAPPTemplateId, int versionSequence, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.UninstallAsync(avatarId, OAPPTemplateId, versionSequence, providerType));
        }

        public OASISResult<IInstalledOAPPTemplate> UninstallOAPPTemplate(Guid avatarId, Guid OAPPTemplateId, int versionSequence, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.Uninstall(avatarId, OAPPTemplateId, versionSequence, providerType));
        }

        public async Task<OASISResult<IInstalledOAPPTemplate>> UninstallOAPPTemplateAsync(Guid avatarId, Guid OAPPTemplateId, string version, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.UninstallAsync(avatarId, OAPPTemplateId, version, providerType));
        }

        public OASISResult<IInstalledOAPPTemplate> UninstallOAPPTemplate(Guid avatarId, Guid OAPPTemplateId, string version, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.Uninstall(avatarId, OAPPTemplateId, version, providerType));
        }

        public async Task<OASISResult<IInstalledOAPPTemplate>> UninstallOAPPTemplateAsync(Guid avatarId, string OAPPTemplateName, int versionSequence, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.UninstallAsync(avatarId, OAPPTemplateName, versionSequence, providerType));
        }

        public OASISResult<IInstalledOAPPTemplate> UninstallOAPPTemplate(Guid avatarId, string OAPPTemplateName, int versionSequence, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.Uninstall(avatarId, OAPPTemplateName, versionSequence, providerType));
        }

        public async Task<OASISResult<IInstalledOAPPTemplate>> UninstallOAPPTemplateAsync(Guid avatarId, string OAPPTemplateName, string version, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.UninstallAsync(avatarId, OAPPTemplateName, version, providerType));
        }

        public OASISResult<IInstalledOAPPTemplate> UninstallOAPPTemplate(Guid avatarId, string OAPPTemplateName, string version, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.Uninstall(avatarId, OAPPTemplateName, version, providerType));
        }

        public async Task<OASISResult<IEnumerable<IInstalledOAPPTemplate>>> ListInstalledOAPPTemplatesAsync(Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResults(await base.ListInstalledAsync(avatarId, providerType));
        }

        public OASISResult<IEnumerable<IInstalledOAPPTemplate>> ListInstalledOAPPTemplates(Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResults(base.ListInstalled(avatarId, providerType));
        }

        public async Task<OASISResult<IEnumerable<IInstalledOAPPTemplate>>> ListUnInstalledOAPPTemplatesAsync(Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResults(await base.ListUninstalledAsync(avatarId, providerType));
        }

        public OASISResult<IEnumerable<IInstalledOAPPTemplate>> ListUnInstalledOAPPTemplates(Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResults(base.ListUninstalled(avatarId, providerType));
        }

        public async Task<OASISResult<IEnumerable<IOAPPTemplate>>> ListUnpublishedOAPPTemplatesAsync(Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResults(await base.ListUnpublishedAsync(avatarId, providerType));
        }

        public OASISResult<IEnumerable<IOAPPTemplate>> ListUnpublishedOAPPTemplates(Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResults(base.ListUnpublished(avatarId, providerType));
        }

        public async Task<OASISResult<IEnumerable<IOAPPTemplate>>> ListDeactivatedOAPPTemplatesAsync(Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResults(await base.ListDeactivatedAsync(avatarId, providerType));
        }

        public OASISResult<IEnumerable<IOAPPTemplate>> ListDeactivatedOAPPTemplates(Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResults(base.ListDeactivated(avatarId, providerType));
        }

        public async Task<OASISResult<bool>> IsOAPPTemplateInstalledAsync(Guid avatarId, Guid OAPPTemplateId, int versionSequence, ProviderType providerType = ProviderType.Default)
        {
            return await base.IsInstalledAsync(avatarId, OAPPTemplateId, versionSequence, providerType);
        }

        public OASISResult<bool> IsOAPPTemplateInstalled(Guid avatarId, Guid OAPPTemplateId, int versionSequence, ProviderType providerType = ProviderType.Default)
        {
            return base.IsInstalled(avatarId, OAPPTemplateId, versionSequence, providerType);
        }

        public async Task<OASISResult<bool>> IsOAPPTemplateInstalledAsync(Guid avatarId, Guid OAPPTemplateId, string version, ProviderType providerType = ProviderType.Default)
        {
            return await base.IsInstalledAsync(avatarId, OAPPTemplateId, version, providerType);
        }

        public OASISResult<bool> IsOAPPTemplateInstalled(Guid avatarId, Guid OAPPTemplateId, string version, ProviderType providerType = ProviderType.Default)
        {
            return base.IsInstalled(avatarId, OAPPTemplateId, version, providerType);
        }

        public async Task<OASISResult<bool>> IsOAPPTemplateInstalledAsync(Guid avatarId, string OAPPTemplateName, int versionSequence, ProviderType providerType = ProviderType.Default)
        {
            return await base.IsInstalledAsync(avatarId, OAPPTemplateName, versionSequence, providerType);
        }

        public OASISResult<bool> IsOAPPTemplateInstalled(Guid avatarId, string OAPPTemplateName, int versionSequence, ProviderType providerType = ProviderType.Default)
        {
            return base.IsInstalled(avatarId, OAPPTemplateName, versionSequence, providerType);
        }

        public async Task<OASISResult<bool>> IsOAPPTemplateInstalledAsync(Guid avatarId, string OAPPTemplateName, string version, ProviderType providerType = ProviderType.Default)
        {
            return await base.IsInstalledAsync(avatarId, OAPPTemplateName, version, providerType);
        }

        public OASISResult<bool> IsOAPPTemplateInstalled(Guid avatarId, string OAPPTemplateName, string version, ProviderType providerType = ProviderType.Default)
        {
            return base.IsInstalled(avatarId, OAPPTemplateName, version, providerType);
        }

        public async Task<OASISResult<IInstalledOAPPTemplate>> LoadInstalledOAPPTemplateAsync(Guid avatarId, Guid OAPPTemplateId, int versionSequence = 0, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.LoadInstalledAsync(avatarId, OAPPTemplateId, versionSequence, providerType));
        }

        public OASISResult<IInstalledOAPPTemplate> LoadInstalledOAPPTemplate(Guid avatarId, Guid OAPPTemplateId, int versionSequence = 0, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.LoadInstalled(avatarId, OAPPTemplateId, versionSequence, providerType));
        }

        public async Task<OASISResult<IInstalledOAPPTemplate>> LoadInstalledOAPPTemplateAsync(Guid avatarId, string OAPPTemplateName, int versionSequence = 0, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.LoadInstalledAsync(avatarId, OAPPTemplateName, versionSequence, providerType));
        }

        public OASISResult<IInstalledOAPPTemplate> LoadInstalledOAPPTemplate(Guid avatarId, string OAPPTemplateName, int versionSequence = 0, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.LoadInstalled(avatarId, OAPPTemplateName, versionSequence, providerType));
        }

        public async Task<OASISResult<IInstalledOAPPTemplate>> LoadInstalledOAPPTemplateAsync(Guid avatarId, Guid OAPPTemplateId, string version, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.LoadInstalledAsync(avatarId, OAPPTemplateId, version, providerType));
        }

        public OASISResult<IInstalledOAPPTemplate> LoadInstalledOAPPTemplate(Guid avatarId, Guid OAPPTemplateId, string version, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.LoadInstalled(avatarId, OAPPTemplateId, version, providerType));
        }

        public async Task<OASISResult<IInstalledOAPPTemplate>> LoadInstalledOAPPTemplateAsync(Guid avatarId, string OAPPTemplateName, string version, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.LoadInstalledAsync(avatarId, OAPPTemplateName, version, providerType));
        }

        public OASISResult<IInstalledOAPPTemplate> LoadInstalledOAPPTemplate(Guid avatarId, string OAPPTemplateName, string version, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.LoadInstalled(avatarId, OAPPTemplateName, version, providerType));
        }

        public async Task<OASISResult<IInstalledOAPPTemplate>> LoadInstalledOAPPTemplateAsync(Guid avatarId, Guid OAPPTemplateId, bool active, int versionSequence = 0, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.LoadInstalledAsync(avatarId, OAPPTemplateId, active, versionSequence, providerType));
        }

        public OASISResult<IInstalledOAPPTemplate> LoadInstalledOAPPTemplate(Guid avatarId, Guid OAPPTemplateId, bool active, int versionSequence = 0, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.LoadInstalled(avatarId, OAPPTemplateId, active, versionSequence, providerType));
        }

        public async Task<OASISResult<IInstalledOAPPTemplate>> LoadInstalledOAPPTemplateAsync(Guid avatarId, string OAPPTemplateName, bool active, int versionSequence = 0, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.LoadInstalledAsync(avatarId, OAPPTemplateName, active, versionSequence, providerType));
        }

        public OASISResult<IInstalledOAPPTemplate> LoadInstalledOAPPTemplate(Guid avatarId, string OAPPTemplateName, bool active, int versionSequence = 0, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.LoadInstalled(avatarId, OAPPTemplateName, active, versionSequence, providerType));
        }

        public async Task<OASISResult<IInstalledOAPPTemplate>> LoadInstalledOAPPTemplateAsync(Guid avatarId, Guid OAPPTemplateId, string version, bool active, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.LoadInstalledAsync(avatarId, OAPPTemplateId, version, active, providerType));
        }

        public OASISResult<IInstalledOAPPTemplate> LoadInstalledOAPPTemplate(Guid avatarId, Guid OAPPTemplateId, string version, bool active, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.LoadInstalled(avatarId, OAPPTemplateId, version, active, providerType));
        }

        public async Task<OASISResult<IInstalledOAPPTemplate>> LoadInstalledOAPPTemplateAsync(Guid avatarId, string OAPPTemplateName, string version, bool active, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.LoadInstalledAsync(avatarId, OAPPTemplateName, version, active, providerType));
        }

        public OASISResult<IInstalledOAPPTemplate> LoadInstalledOAPPTemplate(Guid avatarId, string OAPPTemplateName, string version, bool active, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.LoadInstalled(avatarId, OAPPTemplateName, version, active, providerType));
        }

        public OASISResult<IInstalledOAPPTemplate> OpenOAPPTemplateFolder(Guid avatarId, IInstalledOAPPTemplate OAPPTemplate)
        {
            return ProcessResult(base.OpenOAPPSystemHolonFolder(avatarId, (InstalledOAPPTemplate)OAPPTemplate));
        }

        public async Task<OASISResult<IInstalledOAPPTemplate>> OpenOAPPTemplateFolderAsync(Guid avatarId, Guid OAPPTemplateId, int versionSequence, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.OpenOAPPSystemHolonFolderAsync(avatarId, OAPPTemplateId, versionSequence, providerType));
        }

        public OASISResult<IInstalledOAPPTemplate> OpenOAPPTemplateFolder(Guid avatarId, Guid OAPPTemplateId, int versionSequence, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.OpenOAPPSystemHolonFolder(avatarId, OAPPTemplateId, versionSequence, providerType));
        }

        public async Task<OASISResult<IInstalledOAPPTemplate>> OpenOAPPTemplateFolderAsync(Guid avatarId, Guid OAPPTemplateId, string version, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.OpenOAPPSystemHolonFolderAsync(avatarId, OAPPTemplateId, version, providerType));
        }

        public OASISResult<IInstalledOAPPTemplate> OpenOAPPTemplateFolder(Guid avatarId, Guid OAPPTemplateId, string version, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.OpenOAPPSystemHolonFolder(avatarId, OAPPTemplateId, version, providerType));
        }

        public async Task<OASISResult<bool>> WriteOAPPTemplateDNAAsync(IOAPPSystemHolonDNA OAPPTemplateDNA, string fullPathToOAPPTemplate)
        {
            return await base.WriteOAPPSystemHolonDNAAsync(OAPPTemplateDNA, fullPathToOAPPTemplate);
        }

        public OASISResult<bool> WriteOAPPTemplateDNA(IOAPPSystemHolonDNA OAPPTemplateDNA, string fullPathToOAPPTemplate)
        {
            return base.WriteOAPPSystemHolonDNA(OAPPTemplateDNA, fullPathToOAPPTemplate);
        }

        public async Task<OASISResult<IOAPPSystemHolonDNA>> ReadOAPPTemplateDNAFromSourceOrInstalledFolderAsync(string fullPathToOAPPTemplateFolder)
        {
            return await base.ReadOAPPSystemHolonDNAFromSourceOrInstallFolderAsync(fullPathToOAPPTemplateFolder);
        }

        public OASISResult<IOAPPSystemHolonDNA> ReadOAPPTemplateDNAFromSourceOrInstalledFolder(string fullPathToOAPPTemplateFolder)
        {
            return base.ReadOAPPSystemHolonDNAFromSourceOrInstallFolder(fullPathToOAPPTemplateFolder);
        }

        public async Task<OASISResult<IOAPPSystemHolonDNA>> ReadOAPPTemplateDNAFromPublishedOAPPTemplateFileAsync(string fullPathToOAPPTemplateFolder)
        {
            return await base.ReadOAPPSystemHolonDNAFromPublishedFileAsync(fullPathToOAPPTemplateFolder);
        }

        public OASISResult<IOAPPSystemHolonDNA> ReadOAPPTemplateDNAFromPublishedOAPPTemplateFile(string fullPathToOAPPTemplateFolder)
        {
            return base.ReadOAPPSystemHolonDNAFromPublishedFile(fullPathToOAPPTemplateFolder);
        }

        private OASISResult<IEnumerable<IOAPPTemplate>> ProcessResults(OASISResult<IEnumerable<OAPPTemplate>> operationResult)
        {
            OASISResult<IEnumerable<IOAPPTemplate>> result = new OASISResult<IEnumerable<IOAPPTemplate>>();

            if (operationResult != null && operationResult.Result != null && !operationResult.IsError && operationResult.Result.Count() > 0)
            {
                List<IOAPPTemplate> oappTemplates = new List<IOAPPTemplate>();

                foreach (IOAPPTemplate template in operationResult.Result)
                    oappTemplates.Add(template);

                result.Result = oappTemplates;
            }

            OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(operationResult, result);
            return result;
        }

        private OASISResult<IEnumerable<IInstalledOAPPTemplate>> ProcessResults(OASISResult<IEnumerable<InstalledOAPPTemplate>> operationResult)
        {
            OASISResult<IEnumerable<IInstalledOAPPTemplate>> result = new OASISResult<IEnumerable<IInstalledOAPPTemplate>>();

            if (operationResult != null && operationResult.Result != null && !operationResult.IsError && operationResult.Result.Count() > 0)
            {
                List<IInstalledOAPPTemplate> oappTemplates = new List<IInstalledOAPPTemplate>();

                foreach (IInstalledOAPPTemplate template in operationResult.Result)
                    oappTemplates.Add(template);

                result.Result = oappTemplates;
            }

            OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(operationResult, result);
            return result;
        }

        private OASISResult<IOAPPTemplate> ProcessResult(OASISResult<OAPPTemplate> operationResult)
        {
            OASISResult<IOAPPTemplate> result = new OASISResult<IOAPPTemplate>();
            result.Result = operationResult.Result;
            OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(operationResult, result);
            return result;
        }

        private OASISResult<IDownloadedOAPPTemplate> ProcessResult(OASISResult<DownloadedOAPPTemplate> operationResult)
        {
            OASISResult<IDownloadedOAPPTemplate> result = new OASISResult<IDownloadedOAPPTemplate>();
            result.Result = operationResult.Result;
            OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(operationResult, result);
            return result;
        }

        private OASISResult<IInstalledOAPPTemplate> ProcessResult(OASISResult<InstalledOAPPTemplate> operationResult)
        {
            OASISResult<IInstalledOAPPTemplate> result = new OASISResult<IInstalledOAPPTemplate>();
            result.Result = operationResult.Result;
            OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(operationResult, result);
            return result;
        }
    }
}