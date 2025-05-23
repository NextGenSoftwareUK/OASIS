using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.ONode.Core.Interfaces;
using NextGenSoftware.OASIS.API.ONode.Core.Holons;
using NextGenSoftware.OASIS.API.ONODE.Core.Events;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.ONode.Core.Interfaces.Holons;

namespace NextGenSoftware.OASIS.API.ONode.Core.Managers
{
    public class OAPPTemplateManager : OAPPSystemManagerBase<OAPPTemplate, DownloadedOAPPTemplate, InstalledOAPPTemplate>
    {
        private int _progress = 0;
        private long _fileLength = 0;

        public OAPPTemplateManager(Guid avatarId, OASISDNA OASISDNA = null) : base(avatarId, OASISDNA, 
            HolonType.OAPPTemplate, HolonType.InstalledOAPPTemplate, 
            "OAPP Template", 
            "OAPPTemplateId", 
            "OAPPTemplateName", 
            "OAPPTemplateType", 
            "oapptemplate", 
            "oasis_oapptemplates", 
            "OAPPTemplateDNA.json") { }

        public OAPPTemplateManager(IOASISStorageProvider OASISStorageProvider, Guid avatarId, OASISDNA OASISDNA = null) : base(OASISStorageProvider, avatarId, OASISDNA,
            HolonType.OAPPTemplate, 
            HolonType.InstalledOAPPTemplate, 
            "OAPP Template", 
            "OAPPTemplateId", 
            "OAPPTemplateName", 
            "OAPPTemplateType", 
            "oapptemplate", 
            "oasis_oapptemplates", 
            "OAPPTemplateDNA.json") { }

        //public delegate void OAPPTemplatePublishStatusChanged(object sender, OAPPTemplatePublishStatusEventArgs e);
        //public delegate void OAPPTemplateInstallStatusChanged(object sender, OAPPTemplateInstallStatusEventArgs e);
        //public delegate void OAPPTemplateUploadStatusChanged(object sender, OAPPTemplateUploadProgressEventArgs e);
        //public delegate void OAPPTemplateDownloadStatusChanged(object sender, OAPPTemplateDownloadProgressEventArgs e);

        ///// <summary>
        ///// Fired when there is a change in the OAPP Template publish status.
        ///// </summary>
        //public event OAPPTemplatePublishStatusChanged OnOAPPTemplatePublishStatusChanged;

        ///// <summary>
        ///// Fired when there is a change to the OAPP Template Install status.
        ///// </summary>
        //public event OAPPTemplateInstallStatusChanged OnOAPPTemplateInstallStatusChanged;

        ///// <summary>
        ///// Fired when there is a change in the OAPP Template upload status.
        ///// </summary>
        //public event OAPPTemplateUploadStatusChanged OnOAPPTemplateUploadStatusChanged;

        ///// <summary>
        ///// Fired when there is a change in the OAPP Template download status.
        ///// </summary>
        //public event OAPPTemplateDownloadStatusChanged OnOAPPTemplateDownloadStatusChanged;

        public async Task<OASISResult<IOAPPTemplate>> CreateOAPPTemplateAsync(string name, string description, OAPPTemplateType OAPPTemplateType, Guid avatarId, string fullPathToOAPPTemplate, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.CreateAsync(name, description, OAPPTemplateType, avatarId, fullPathToOAPPTemplate, providerType));
        }

        public OASISResult<IOAPPTemplate> CreateOAPPTemplate(string name, string description, OAPPTemplateType OAPPTemplateType, Guid avatarId, string fullPathToOAPPTemplate, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.Create(name, description, OAPPTemplateType, avatarId, fullPathToOAPPTemplate, providerType));
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
            return ProcessResults(await base.LoadAllAsync(avatarId, OAPPTemplateType, OAPPTemplateType == OAPPTemplateType.All, showAllVersions, version, providerType));
        }

        public OASISResult<IEnumerable<IOAPPTemplate>> LoadAllOAPPTemplates(Guid avatarId, OAPPTemplateType OAPPTemplateType = OAPPTemplateType.All, bool showAllVersions = false, int version = 0, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResults(base.LoadAll(avatarId, OAPPTemplateType, OAPPTemplateType == OAPPTemplateType.All, showAllVersions, version, providerType));
        }

        public async Task<OASISResult<IEnumerable<IOAPPTemplate>>> LoadAllOAPPTemplatesForAvatarAsync(Guid avatarId, bool showAllVersions = false, int version = 0, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResults(await base.LoadAllForAvatarAsync(avatarId, showAllVersions, version, providerType));
        }

        public OASISResult<IEnumerable<IOAPPTemplate>> LoadAllOAPPTemplatesForAvatar(Guid avatarId, bool showAllVersions = false, int version = 0, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResults(base.LoadAllForAvatar(avatarId, showAllVersions, version, providerType));
        }

        public async Task<OASISResult<IEnumerable<IOAPPTemplate>>> SearchOAPPTemplatesAsync(string searchTerm, Guid avatarId, bool searchOnlyForCurrentAvatar = true, bool showAllVersions = false, int version = 0, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResults(await base.SearchAsync(searchTerm, avatarId, HolonType.OAPPTemplate, searchOnlyForCurrentAvatar, showAllVersions, version, providerType));
        }

        public OASISResult<IEnumerable<IOAPPTemplate>> SearchOAPPTemplates(string searchTerm, Guid avatarId, bool searchOnlyForCurrentAvatar = true, bool showAllVersions = false, int version = 0, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResults(base.Search(searchTerm, avatarId, HolonType.OAPPTemplate, searchOnlyForCurrentAvatar, showAllVersions, version, providerType));
        }

        public async Task<OASISResult<IOAPPTemplate>> DeleteOAPPTemplateAsync(Guid oappTemplateId, Guid avatarId, int version, bool softDelete = true, bool deleteDownload = true, bool deleteInstall = true, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.DeleteAsync(oappTemplateId, avatarId, version, softDelete, deleteDownload, deleteInstall, providerType));
        }

        public OASISResult<IOAPPTemplate> DeleteOAPPTemplate(Guid oappTemplateId, Guid avatarId, int version, bool softDelete = true, bool deleteDownload = true, bool deleteInstall = true, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.Delete(oappTemplateId, avatarId, version, softDelete, deleteDownload, deleteInstall, providerType));
        }

        public async Task<OASISResult<IOAPPTemplate>> DeleteOAPPTemplateAsync(Guid avatarId, IOAPPTemplate oappTemplate, int version, bool softDelete = true, bool deleteDownload = true, bool deleteInstall = true, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.DeleteAsync(oappTemplate, avatarId, version, softDelete, deleteDownload, deleteInstall, providerType));
        }

        public OASISResult<IOAPPTemplate> DeleteOAPPTemplate(Guid avatarId, IOAPPTemplate oappTemplate, int version, bool softDelete = true, bool deleteDownload = true, bool deleteInstall = true, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.Delete(oappTemplate, avatarId, version, softDelete, deleteDownload, deleteInstall, providerType));
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

        public async Task<OASISResult<IOAPPTemplate>> EditOAPPTemplateAsync(Guid OAPPTemplateId, IOAPPTemplateDNA newOAPPTemplateDNA, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.EditAsync(OAPPTemplateId, newOAPPTemplateDNA, avatarId, providerType));
        }

        public async Task<OASISResult<IOAPPTemplate>> EditOAPPTemplateAsync(IOAPPTemplate OAPPTemplate, IOAPPTemplateDNA newOAPPTemplateDNA, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.EditAsync((OAPPTemplate)OAPPTemplate, newOAPPTemplateDNA, avatarId, providerType));
        }

        public async Task<OASISResult<IOAPPTemplate>> PublishOAPPTemplateAsync(Guid avatarId, string fullPathToOAPPTemplate, string launchTarget, string fullPathToPublishTo = "", bool registerOnSTARNET = true, bool generateOAPPTemplateBinary = true, bool uploadOAPPTemplateToCloud = false, bool edit = false, ProviderType providerType = ProviderType.Default, ProviderType oappBinaryProviderType = ProviderType.IPFSOASIS)
        {
            return ProcessResult(await base.PublishAsync(avatarId, fullPathToOAPPTemplate, launchTarget, fullPathToPublishTo, registerOnSTARNET, generateOAPPTemplateBinary, uploadOAPPTemplateToCloud, edit, providerType));
        }

        public async Task<OASISResult<IOAPPTemplate>> PublishOAPPTemplate(Guid avatarId, string fullPathToOAPPTemplate, string launchTarget, string fullPathToPublishTo = "", bool registerOnSTARNET = true, bool generateOAPPTemplateBinary = true, bool uploadOAPPTemplateToCloud = false, bool edit = false, ProviderType providerType = ProviderType.Default, ProviderType oappBinaryProviderType = ProviderType.IPFSOASIS)
        {
            return ProcessResult(base.Publish(avatarId, fullPathToOAPPTemplate, launchTarget, fullPathToPublishTo, registerOnSTARNET, generateOAPPTemplateBinary, uploadOAPPTemplateToCloud, edit, providerType));
        }

        public async Task<OASISResult<IOAPPTemplate>> UnpublishOAPPTemplateAsync(IOAPPTemplate OAPPTemplate, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.UnpublishAsync((OAPPTemplate)OAPPTemplate, avatarId, providerType));
        }

        public OASISResult<IOAPPTemplate> UnpublishOAPPTemplate(IOAPPTemplate OAPPTemplate, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.Unpublish((OAPPTemplate)OAPPTemplate, avatarId, providerType));
        }

        public async Task<OASISResult<IOAPPTemplate>> UnpublishOAPPTemplateAsync(Guid OAPPTemplateId, int version, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.UnpublishAsync(OAPPTemplateId, version, avatarId, providerType));
        }

        public OASISResult<IOAPPTemplate> UnpublishOAPPTemplate(Guid OAPPTemplateId, int version, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.Unpublish(OAPPTemplateId, version, avatarId, providerType));
        }

        public async Task<OASISResult<IOAPPTemplate>> UnpublishOAPPTemplateAsync(IOAPPTemplateDNA OAPPTemplateDNA, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.UnpublishAsync(OAPPTemplateDNA, avatarId, providerType));
        }

        public OASISResult<IOAPPTemplate> UnpublishOAPPTemplate(IOAPPTemplateDNA OAPPTemplateDNA, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.Unpublish(OAPPTemplateDNA, avatarId, providerType));
        }

        public async Task<OASISResult<IOAPPTemplate>> RepublishOAPPTemplateAsync(IOAPPTemplate OAPPTemplate, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.RepublishAsync((OAPPTemplate)OAPPTemplate, avatarId, providerType));
        }

        public OASISResult<IOAPPTemplate> RepublishOAPPTemplate(IOAPPTemplate OAPPTemplate, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.Republish((OAPPTemplate)OAPPTemplate, avatarId, providerType));
        }

        public async Task<OASISResult<IOAPPTemplate>> RepublishOAPPTemplateAsync(IOAPPTemplateDNA OAPPTemplateDNA, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.RepublishAsync(OAPPTemplateDNA, avatarId, providerType));
        }

        public OASISResult<IOAPPTemplate> RepublishOAPPTemplate(IOAPPTemplateDNA OAPPTemplateDNA, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.Republish(OAPPTemplateDNA, avatarId, providerType));
        }

        public async Task<OASISResult<IOAPPTemplate>> RepublishOAPPTemplateAsync(Guid OAPPTemplateId, int version, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.RepublishAsync(OAPPTemplateId, version, avatarId, providerType));
        }

        public OASISResult<IOAPPTemplate> RepublishOAPPTemplate(Guid OAPPTemplateId, int version, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.Republish(OAPPTemplateId, version, avatarId, providerType));
        }

        public async Task<OASISResult<IOAPPTemplate>> DeactivateOAPPTemplateAsync(IOAPPTemplate OAPPTemplate, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.DeactivateAsync((OAPPTemplate)OAPPTemplate, avatarId, providerType));
        }

        public OASISResult<IOAPPTemplate> DeactivateOAPPTemplate(IOAPPTemplate OAPPTemplate, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.Deactivate((OAPPTemplate)OAPPTemplate, avatarId, providerType));
        }

        public async Task<OASISResult<IOAPPTemplate>> DeactivateOAPPTemplateAsync(Guid OAPPTemplateId, int version, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.DeactivateAsync(OAPPTemplateId, version, avatarId, providerType));
        }

        public OASISResult<IOAPPTemplate> DeactivateOAPPTemplate(Guid OAPPTemplateId, int version, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.Deactivate(OAPPTemplateId, version, avatarId, providerType));
        }

        public async Task<OASISResult<IOAPPTemplate>> DeactivateOAPPTemplateAsync(IOAPPTemplateDNA OAPPTemplateDNA, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.DeactivateAsync(OAPPTemplateDNA, avatarId, providerType));
        }

        public OASISResult<IOAPPTemplate> DeactivateOAPPTemplate(IOAPPTemplateDNA OAPPTemplateDNA, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.Deactivate(OAPPTemplateDNA, avatarId, providerType));
        }

        public async Task<OASISResult<IOAPPTemplate>> ActivateOAPPTemplateAsync(IOAPPTemplate OAPPTemplate, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.ActivateAsync((OAPPTemplate)OAPPTemplate, avatarId, providerType));
        }

        public OASISResult<IOAPPTemplate> ActivateOAPPTemplate(IOAPPTemplate OAPPTemplate, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.Activate((OAPPTemplate)OAPPTemplate, avatarId, providerType));
        }

        public async Task<OASISResult<IOAPPTemplate>> ActivateOAPPTemplateAsync(IOAPPTemplateDNA OAPPTemplateDNA, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.ActivateAsync(OAPPTemplateDNA, avatarId, providerType));
        }

        public OASISResult<IOAPPTemplate> ActivateOAPPTemplate(IOAPPTemplateDNA OAPPTemplateDNA, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.Activate(OAPPTemplateDNA, avatarId, providerType));
        }

        public async Task<OASISResult<IOAPPTemplate>> ActivateOAPPTemplateAsync(Guid OAPPTemplateId, int version, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.ActivateAsync(OAPPTemplateId, version, avatarId, providerType));
        }

        public OASISResult<IOAPPTemplate> ActivateOAPPTemplate(Guid OAPPTemplateId, int version, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.Activate(OAPPTemplateId, version, avatarId, providerType));
        }

        public async Task<OASISResult<IDownloadedOAPPTemplate>> DownloadOAPPTemplateAsync(Guid avatarId, IOAPPTemplate OAPPTemplate, string fullDownloadPath, bool reInstall = false, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.DownloadAsync(avatarId, (OAPPTemplate)OAPPTemplate, fullDownloadPath, reInstall, providerType));
        }

        public async Task<OASISResult<IDownloadedOAPPTemplate>> DownloadOAPPTemplate(Guid avatarId, IOAPPTemplate OAPPTemplate, string fullDownloadPath, bool reInstall = false, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.DownloadAsync(avatarId, (OAPPTemplate)OAPPTemplate, fullDownloadPath, reInstall, providerType));
        }

        public async Task<OASISResult<IInstalledOAPPTemplate>> DownloadAndInstallOAPPTemplateAsync(Guid avatarId, IOAPPTemplate OAPPTemplate, string fullInstallPath, string fullDownloadPath = "", bool createOAPPTemplateDirectory = true, bool reInstall = false, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.DownloadAndInstallAsync(avatarId, (OAPPTemplate)OAPPTemplate, fullDownloadPath, fullDownloadPath, createOAPPTemplateDirectory, reInstall, providerType));
        }

        public async Task<OASISResult<IInstalledOAPPTemplate>> DownloadAndInstallOAPPTemplate(Guid avatarId, IOAPPTemplate OAPPTemplate, string fullInstallPath, string fullDownloadPath = "", bool createOAPPTemplateDirectory = true, bool reInstall = false, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.DownloadAndInstall(avatarId, (OAPPTemplate)OAPPTemplate, fullDownloadPath, fullDownloadPath, createOAPPTemplateDirectory, reInstall, providerType));
        }

        public async Task<OASISResult<IInstalledOAPPTemplate>> InstallOAPPTemplateAsync(Guid avatarId, string fullPathToPublishedOAPPTemplateFile, string fullInstallPath, bool createOAPPTemplateDirectory = true, IDownloadedOAPPTemplate downloadedOAPPTemplate = null, bool reInstall = false, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.InstallAsync(avatarId, fullPathToPublishedOAPPTemplateFile, fullInstallPath, createOAPPTemplateDirectory, downloadedOAPPTemplate, reInstall, providerType));
        }

        public OASISResult<IInstalledOAPPTemplate> InstallOAPPTemplate(Guid avatarId, string fullPathToPublishedOAPPTemplateFile, string fullInstallPath, bool createOAPPTemplateDirectory = true, IDownloadedOAPPTemplate downloadedOAPPTemplate = null, bool reInstall = false, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.Install(avatarId, fullPathToPublishedOAPPTemplateFile, fullInstallPath, createOAPPTemplateDirectory, downloadedOAPPTemplate, reInstall, providerType));
        }

        public async Task<OASISResult<IInstalledOAPPTemplate>> InstallOAPPTemplateAsync(Guid avatarId, Guid OAPPTemplateId, int version, string fullInstallPath, string fullDownloadPath = "", bool createOAPPTemplateDirectory = true, bool reInstall = false, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.InstallAsync(avatarId, OAPPTemplateId, version, fullInstallPath, fullDownloadPath, createOAPPTemplateDirectory, reInstall, providerType));
        }

        public OASISResult<IInstalledOAPPTemplate> InstallOAPPTemplate(Guid avatarId, Guid OAPPTemplateId, int version, string fullInstallPath, string fullDownloadPath = "", bool createOAPPTemplateDirectory = true, bool reInstall = false, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.Install(avatarId, OAPPTemplateId, version, fullInstallPath, fullDownloadPath, createOAPPTemplateDirectory, reInstall, providerType));
        }

        public async Task<OASISResult<IInstalledOAPPTemplate>> UninstallOAPPTemplateAsync(IInstalledOAPPTemplate installedOAPPTemplate, Guid avatarId, string errorMessage, ProviderType providerType)
        {
            return ProcessResult(await base.UninstallAsync((InstalledOAPPTemplate)installedOAPPTemplate, avatarId, errorMessage, providerType));
        }

        public OASISResult<IInstalledOAPPTemplate> UninstallOAPPTemplate(IInstalledOAPPTemplate installedOAPPTemplate, Guid avatarId, string errorMessage, ProviderType providerType)
        {
            return ProcessResult(base.Uninstall((InstalledOAPPTemplate)installedOAPPTemplate, avatarId, errorMessage, providerType));
        }

        public async Task<OASISResult<IInstalledOAPPTemplate>> UninstallOAPPTemplateAsync(Guid OAPPTemplateId, int versionSequence, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.UninstallAsync(OAPPTemplateId, versionSequence, avatarId, providerType));
        }

        public OASISResult<IInstalledOAPPTemplate> UninstallOAPPTemplate(Guid OAPPTemplateId, int versionSequence, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.Uninstall(OAPPTemplateId, versionSequence, avatarId, providerType));
        }

        public async Task<OASISResult<IInstalledOAPPTemplate>> UninstallOAPPTemplateAsync(Guid OAPPTemplateId, string version, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.UninstallAsync(OAPPTemplateId, version, avatarId, providerType));
        }

        public OASISResult<IInstalledOAPPTemplate> UninstallOAPPTemplate(Guid OAPPTemplateId, string version, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.Uninstall(OAPPTemplateId, version, avatarId, providerType));
        }

        public async Task<OASISResult<IInstalledOAPPTemplate>> UninstallOAPPTemplateAsync(string OAPPTemplateName, int versionSequence, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.UninstallAsync(OAPPTemplateName, versionSequence, avatarId, providerType));
        }

        public OASISResult<IInstalledOAPPTemplate> UninstallOAPPTemplate(string OAPPTemplateName, int versionSequence, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.Uninstall(OAPPTemplateName, versionSequence, avatarId, providerType));
        }

        public async Task<OASISResult<IInstalledOAPPTemplate>> UninstallOAPPTemplateAsync(string OAPPTemplateName, string version, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.UninstallAsync(OAPPTemplateName, version, avatarId, providerType));
        }

        public OASISResult<IInstalledOAPPTemplate> UninstallOAPPTemplate(string OAPPTemplateName, string version, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.Uninstall(OAPPTemplateName, version, avatarId, providerType));
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

        public async Task<OASISResult<bool>> IsOAPPTemplateInstalledAsync(Guid OAPPTemplateId, Guid avatarId, int versionSequence, ProviderType providerType = ProviderType.Default)
        {
            return await base.IsInstalledAsync(OAPPTemplateId, avatarId, versionSequence, providerType);
        }

        public OASISResult<bool> IsOAPPTemplateInstalled(Guid avatarId, Guid OAPPTemplateId, int versionSequence, ProviderType providerType = ProviderType.Default)
        {
            return base.IsInstalled(OAPPTemplateId, avatarId, versionSequence, providerType);
        }

        public async Task<OASISResult<bool>> IsOAPPTemplateInstalledAsync(Guid avatarId, Guid OAPPTemplateId, string version, ProviderType providerType = ProviderType.Default)
        {
            return await base.IsInstalledAsync(OAPPTemplateId, avatarId, version, providerType);
        }

        public OASISResult<bool> IsOAPPTemplateInstalled(Guid avatarId, Guid OAPPTemplateId, string version, ProviderType providerType = ProviderType.Default)
        {
            return base.IsInstalled(OAPPTemplateId, avatarId, version, providerType);
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

        public async Task<OASISResult<bool>> WriteOAPPTemplateDNAAsync(IOAPPTemplateDNA OAPPTemplateDNA, string fullPathToOAPPTemplate)
        {
            return await base.WriteOAPPSystemHolonDNAAsync(OAPPTemplateDNA, fullPathToOAPPTemplate);
        }

        public OASISResult<bool> WriteOAPPTemplateDNA(IOAPPTemplateDNA OAPPTemplateDNA, string fullPathToOAPPTemplate)
        {
            return base.WriteOAPPSystemHolonDNA(OAPPTemplateDNA, fullPathToOAPPTemplate);
        }

        public async Task<OASISResult<IOAPPTemplateDNA>> ReadOAPPTemplateDNAAsync(string fullPathToOAPPTemplate)
        {
            return ProcessResult(await base.ReadOAPPSystemHolonDNAAsync(fullPathToOAPPTemplate));
        }

        public OASISResult<IOAPPTemplateDNA> ReadOAPPTemplateDNA(string fullPathToOAPPTemplate)
        {
            return ProcessResult(base.ReadOAPPSystemHolonDNA(fullPathToOAPPTemplate));
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

        private OASISResult<IOAPPTemplateDNA> ProcessResult(OASISResult<IOAPPSystemHolonDNA> operationResult)
        {
            OASISResult<IOAPPTemplateDNA> result = new OASISResult<IOAPPTemplateDNA>();
            result.Result = (IOAPPTemplateDNA)operationResult.Result;
            OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(operationResult, result);
            return result;
        }
    }
}