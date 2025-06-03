using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.ONODE.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Holons;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Managers
{
    public class RuntimeManager : OAPPSystemManagerBase<Runtime, DownloadedRuntime, InstalledRuntime>
    {
        public RuntimeManager(Guid avatarId, OASISDNA OASISDNA = null) : base(avatarId, OASISDNA,
            HolonType.Runtime,
            HolonType.InstalledRuntime,
            "Runtime",
            "RuntimeId",
            "RuntimeName",
            "RuntimeType",
            "runtime",
            "oasis_runtimes",
            "RuntimeDNA.json",
            "RuntimeDNAJSON")
        { }

        public RuntimeManager(IOASISStorageProvider OASISStorageProvider, Guid avatarId, OASISDNA OASISDNA = null) : base(OASISStorageProvider, avatarId, OASISDNA,
            HolonType.Runtime,
            HolonType.InstalledRuntime,
            "Runtime",
            "RuntimeId",
            "RuntimeName",
            "RuntimeType",
            "oapp",
            "oasis_oapps",
            "RuntimeDNA.json",
            "RuntimeDNAJSON")
        { }

        public async Task<OASISResult<IRuntime>> CreateRuntimeTemplateAsync(string name, string description, RuntimeType runtimeType, Guid avatarId, string fullPathToRuntimeTemplate, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.CreateAsync(name, description, runtimeType, avatarId, fullPathToRuntimeTemplate, providerType));
        }

        public OASISResult<IRuntime> CreateRuntimeTemplate(string name, string description, RuntimeType runtimeType, Guid avatarId, string fullPathToRuntimeTemplate, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.Create(name, description, runtimeType, avatarId, fullPathToRuntimeTemplate, providerType));
        }

        #region COSMICManagerBase
        public async Task<OASISResult<IRuntime>> SaveRuntimeTemplateAsync(Guid avatarId, IRuntime oappTemplate, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.SaveAsync(avatarId, (Runtime)oappTemplate, providerType));
        }

        public OASISResult<IRuntime> SaveRuntimeTemplate(Guid avatarId, IRuntime oappTemplate, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.Save(avatarId, (Runtime)oappTemplate, providerType));
        }

        public async Task<OASISResult<IRuntime>> LoadRuntimeTemplateAsync(Guid avatarId, Guid RuntimeTemplateId, int version = 0, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.LoadAsync(avatarId, RuntimeTemplateId, version, providerType));
        }

        public OASISResult<IRuntime> LoadRuntimeTemplate(Guid avatarId, Guid RuntimeTemplateId, int version = 0, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.Load(avatarId, RuntimeTemplateId, version, providerType));
        }

        public async Task<OASISResult<IEnumerable<IRuntime>>> LoadAllRuntimeTemplatesAsync(Guid avatarId, RuntimeType runtimeType = RuntimeType.All, bool showAllVersions = false, int version = 0, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResults(await base.LoadAllAsync(avatarId, runtimeType, RuntimeType == RuntimeType.All, showAllVersions, version, providerType));
        }

        public OASISResult<IEnumerable<IRuntime>> LoadAllRuntimeTemplates(Guid avatarId, runtimeType RuntimeType = RuntimeType.All, bool showAllVersions = false, int version = 0, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResults(base.LoadAll(avatarId, RuntimeType, RuntimeType == RuntimeType.All, showAllVersions, version, providerType));
        }

        public async Task<OASISResult<IEnumerable<IRuntime>>> LoadAllRuntimeTemplatesForAvatarAsync(Guid avatarId, bool showAllVersions = false, int version = 0, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResults(await base.LoadAllForAvatarAsync(avatarId, showAllVersions, version, providerType));
        }

        public OASISResult<IEnumerable<IRuntime>> LoadAllRuntimeTemplatesForAvatar(Guid avatarId, bool showAllVersions = false, int version = 0, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResults(base.LoadAllForAvatar(avatarId, showAllVersions, version, providerType));
        }

        public async Task<OASISResult<IEnumerable<IRuntime>>> SearchRuntimeTemplatesAsync(Guid avatarId, string searchTerm, bool searchOnlyForCurrentAvatar = true, bool showAllVersions = false, int version = 0, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResults(await base.SearchAsync(avatarId, searchTerm, HolonType.Runtime, searchOnlyForCurrentAvatar, showAllVersions, version, providerType));
        }

        public OASISResult<IEnumerable<IRuntime>> SearchRuntimeTemplates(Guid avatarId, string searchTerm, bool searchOnlyForCurrentAvatar = true, bool showAllVersions = false, int version = 0, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResults(base.Search(avatarId, searchTerm, HolonType.Runtime, searchOnlyForCurrentAvatar, showAllVersions, version, providerType));
        }

        public async Task<OASISResult<IRuntime>> DeleteRuntimeTemplateAsync(Guid avatarId, Guid oappTemplateId, int version, bool softDelete = true, bool deleteDownload = true, bool deleteInstall = true, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.DeleteAsync(avatarId, oappTemplateId, version, softDelete, deleteDownload, deleteInstall, providerType));
        }

        public OASISResult<IRuntime> DeleteRuntimeTemplate(Guid avatarId, Guid oappTemplateId, int version, bool softDelete = true, bool deleteDownload = true, bool deleteInstall = true, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.Delete(avatarId, oappTemplateId, version, softDelete, deleteDownload, deleteInstall, providerType));
        }

        public async Task<OASISResult<IRuntime>> DeleteRuntimeTemplateAsync(Guid avatarId, IRuntime oappTemplate, int version, bool softDelete = true, bool deleteDownload = true, bool deleteInstall = true, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.DeleteAsync(avatarId, oappTemplate, version, softDelete, deleteDownload, deleteInstall, providerType));
        }

        public OASISResult<IRuntime> DeleteRuntimeTemplate(Guid avatarId, IRuntime oappTemplate, int version, bool softDelete = true, bool deleteDownload = true, bool deleteInstall = true, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.Delete(avatarId, oappTemplate, version, softDelete, deleteDownload, deleteInstall, providerType));
        }
        #endregion

        public async Task<OASISResult<IEnumerable<IRuntime>>> LoadRuntimeTemplateVersionsAsync(Guid RuntimeTemplateId, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResults(await base.LoadVersionsAsync(RuntimeTemplateId, providerType));
        }

        public OASISResult<IEnumerable<IRuntime>> LoadRuntimeTemplateVersions(Guid RuntimeTemplateId, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResults(base.LoadVersions(RuntimeTemplateId, providerType));
        }

        public async Task<OASISResult<IRuntime>> LoadRuntimeTemplateVersionAsync(Guid RuntimeTemplateId, string version, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.LoadVersionAsync(RuntimeTemplateId, version, providerType));
        }

        public OASISResult<IRuntime> LoadRuntimeTemplateVersion(Guid RuntimeTemplateId, string version, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.LoadVersion(RuntimeTemplateId, version, providerType));
        }

        public async Task<OASISResult<IRuntime>> EditRuntimeTemplateAsync(Guid RuntimeTemplateId, IOAPPSystemHolonDNA newRuntimeTemplateDNA, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.EditAsync(RuntimeTemplateId, newRuntimeTemplateDNA, avatarId, providerType));
        }

        public async Task<OASISResult<IRuntime>> EditRuntimeTemplateAsync(IRuntime Runtime, IOAPPSystemHolonDNA newRuntimeTemplateDNA, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.EditAsync(avatarId, (Runtime)Runtime, newRuntimeTemplateDNA, providerType));
        }

        public async Task<OASISResult<IRuntime>> PublishRuntimeTemplateAsync(Guid avatarId, string fullPathToRuntimeTemplate, string launchTarget, string fullPathToPublishTo = "", bool registerOnSTARNET = true, bool generateRuntimeTemplateBinary = true, bool uploadRuntimeTemplateToCloud = false, bool edit = false, ProviderType providerType = ProviderType.Default, ProviderType oappBinaryProviderType = ProviderType.IPFSOASIS)
        {
            return ProcessResult(await base.PublishAsync(avatarId, fullPathToRuntimeTemplate, launchTarget, fullPathToPublishTo, registerOnSTARNET, generateRuntimeTemplateBinary, uploadRuntimeTemplateToCloud, edit, providerType, oappBinaryProviderType));
        }

        public async Task<OASISResult<IRuntime>> PublishRuntimeTemplate(Guid avatarId, string fullPathToRuntimeTemplate, string launchTarget, string fullPathToPublishTo = "", bool registerOnSTARNET = true, bool generateRuntimeTemplateBinary = true, bool uploadRuntimeTemplateToCloud = false, bool edit = false, ProviderType providerType = ProviderType.Default, ProviderType oappBinaryProviderType = ProviderType.IPFSOASIS)
        {
            return ProcessResult(base.Publish(avatarId, fullPathToRuntimeTemplate, launchTarget, fullPathToPublishTo, registerOnSTARNET, generateRuntimeTemplateBinary, uploadRuntimeTemplateToCloud, edit, providerType, oappBinaryProviderType));
        }

        public async Task<OASISResult<IRuntime>> UnpublishRuntimeTemplateAsync(Guid avatarId, IRuntime Runtime, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.UnpublishAsync(avatarId, (Runtime)Runtime, providerType));
        }

        public OASISResult<IRuntime> UnpublishRuntimeTemplate(Guid avatarId, IRuntime Runtime, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.Unpublish(avatarId, (Runtime)Runtime, providerType));
        }

        public async Task<OASISResult<IRuntime>> UnpublishRuntimeTemplateAsync(Guid avatarId, Guid RuntimeTemplateId, int version, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.UnpublishAsync(avatarId, RuntimeTemplateId, version, providerType));
        }

        public OASISResult<IRuntime> UnpublishRuntimeTemplate(Guid avatarId, Guid RuntimeTemplateId, int version, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.Unpublish(avatarId, RuntimeTemplateId, version, providerType));
        }

        public async Task<OASISResult<IRuntime>> UnpublishRuntimeTemplateAsync(Guid avatarId, IOAPPSystemHolonDNA RuntimeTemplateDNA, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.UnpublishAsync(avatarId, RuntimeTemplateDNA, providerType));
        }

        public OASISResult<IRuntime> UnpublishRuntimeTemplate(Guid avatarId, IOAPPSystemHolonDNA RuntimeTemplateDNA, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.Unpublish(avatarId, RuntimeTemplateDNA, providerType));
        }

        public async Task<OASISResult<IRuntime>> RepublishRuntimeTemplateAsync(Guid avatarId, IRuntime Runtime, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.RepublishAsync(avatarId, (Runtime)Runtime, providerType));
        }

        public OASISResult<IRuntime> RepublishRuntimeTemplate(Guid avatarId, IRuntime Runtime, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.Republish(avatarId, (Runtime)Runtime, providerType));
        }

        public async Task<OASISResult<IRuntime>> RepublishRuntimeTemplateAsync(Guid avatarId, IOAPPSystemHolonDNA RuntimeTemplateDNA, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.RepublishAsync(avatarId, RuntimeTemplateDNA, providerType));
        }

        public OASISResult<IRuntime> RepublishRuntimeTemplate(Guid avatarId, IOAPPSystemHolonDNA RuntimeTemplateDNA, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.Republish(avatarId, RuntimeTemplateDNA, providerType));
        }

        public async Task<OASISResult<IRuntime>> RepublishRuntimeTemplateAsync(Guid RuntimeTemplateId, int version, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.RepublishAsync(avatarId, RuntimeTemplateId, version, providerType));
        }

        public OASISResult<IRuntime> RepublishRuntimeTemplate(Guid RuntimeTemplateId, int version, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.Republish(avatarId, RuntimeTemplateId, version, providerType));
        }

        public async Task<OASISResult<IRuntime>> DeactivateRuntimeTemplateAsync(Guid avatarId, IRuntime Runtime, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.DeactivateAsync(avatarId, (Runtime)Runtime, providerType));
        }

        public OASISResult<IRuntime> DeactivateRuntimeTemplate(Guid avatarId, IRuntime Runtime, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.Deactivate(avatarId, (Runtime)Runtime, providerType));
        }

        public async Task<OASISResult<IRuntime>> DeactivateRuntimeTemplateAsync(Guid RuntimeTemplateId, int version, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.DeactivateAsync(avatarId, RuntimeTemplateId, version, providerType));
        }

        public OASISResult<IRuntime> DeactivateRuntimeTemplate(Guid RuntimeTemplateId, int version, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.Deactivate(avatarId, RuntimeTemplateId, version, providerType));
        }

        public async Task<OASISResult<IRuntime>> DeactivateRuntimeTemplateAsync(Guid avatarId, IOAPPSystemHolonDNA RuntimeTemplateDNA, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.DeactivateAsync(avatarId, RuntimeTemplateDNA, providerType));
        }

        public OASISResult<IRuntime> DeactivateRuntimeTemplate(Guid avatarId, IOAPPSystemHolonDNA RuntimeTemplateDNA, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.Deactivate(avatarId, RuntimeTemplateDNA, providerType));
        }

        public async Task<OASISResult<IRuntime>> ActivateRuntimeTemplateAsync(Guid avatarId, IRuntime Runtime, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.ActivateAsync((Runtime)Runtime, avatarId, providerType));
        }

        public OASISResult<IRuntime> ActivateRuntimeTemplate(Guid avatarId, IRuntime Runtime, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.Activate((Runtime)Runtime, avatarId, providerType));
        }

        public async Task<OASISResult<IRuntime>> ActivateRuntimeTemplateAsync(Guid avatarId, IOAPPSystemHolonDNA RuntimeTemplateDNA, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.ActivateAsync(RuntimeTemplateDNA, avatarId, providerType));
        }

        public OASISResult<IRuntime> ActivateRuntimeTemplate(Guid avatarId, IOAPPSystemHolonDNA RuntimeTemplateDNA, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.Activate(avatarId, RuntimeTemplateDNA, providerType));
        }

        public async Task<OASISResult<IRuntime>> ActivateRuntimeTemplateAsync(Guid avatarId, Guid RuntimeTemplateId, int version, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.ActivateAsync(avatarId, RuntimeTemplateId, version, providerType));
        }

        public OASISResult<IRuntime> ActivateRuntimeTemplate(Guid avatarId, Guid RuntimeTemplateId, int version, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.Activate(avatarId, RuntimeTemplateId, version, providerType));
        }

        public async Task<OASISResult<IDownloadedRuntime>> DownloadRuntimeTemplateAsync(Guid avatarId, IRuntime Runtime, string fullDownloadPath, bool reInstall = false, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.DownloadAsync(avatarId, (Runtime)Runtime, fullDownloadPath, reInstall, providerType));
        }

        public OASISResult<IDownloadedRuntime> DownloadRuntimeTemplate(Guid avatarId, IRuntime Runtime, string fullDownloadPath, bool reInstall = false, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.Download(avatarId, (Runtime)Runtime, fullDownloadPath, reInstall, providerType));
        }

        public async Task<OASISResult<IInstalledRuntime>> DownloadAndInstallRuntimeTemplateAsync(Guid avatarId, IRuntime Runtime, string fullInstallPath, string fullDownloadPath = "", bool createRuntimeTemplateDirectory = true, bool reInstall = false, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.DownloadAndInstallAsync(avatarId, (Runtime)Runtime, fullInstallPath, fullDownloadPath, createRuntimeTemplateDirectory, reInstall, providerType));
        }

        public OASISResult<IInstalledRuntime> DownloadAndInstallRuntimeTemplate(Guid avatarId, IRuntime Runtime, string fullInstallPath, string fullDownloadPath = "", bool createRuntimeTemplateDirectory = true, bool reInstall = false, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.DownloadAndInstall(avatarId, (Runtime)Runtime, fullInstallPath, fullDownloadPath, createRuntimeTemplateDirectory, reInstall, providerType));
        }

        public async Task<OASISResult<IInstalledRuntime>> DownloadAndInstallRuntimeTemplateAsync(Guid avatarId, Guid RuntimeTemplateId, int version, string fullInstallPath, string fullDownloadPath = "", bool createRuntimeTemplateDirectory = true, bool reInstall = false, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.DownloadAndInstallAsync(avatarId, RuntimeTemplateId, version, fullInstallPath, fullDownloadPath, createRuntimeTemplateDirectory, reInstall, providerType));
        }

        public async Task<OASISResult<IInstalledRuntime>> DownloadAndInstallRuntimeTemplate(Guid avatarId, Guid RuntimeTemplateId, int version, string fullInstallPath, string fullDownloadPath = "", bool createRuntimeTemplateDirectory = true, bool reInstall = false, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.DownloadAndInstall(avatarId, RuntimeTemplateId, version, fullInstallPath, fullDownloadPath, createRuntimeTemplateDirectory, reInstall, providerType));
        }


        public async Task<OASISResult<IInstalledRuntime>> InstallRuntimeTemplateAsync(Guid avatarId, string fullPathToPublishedRuntimeTemplateFile, string fullInstallPath, bool createRuntimeTemplateDirectory = true, IDownloadedRuntime downloadedRuntimeTemplate = null, bool reInstall = false, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.InstallAsync(avatarId, fullPathToPublishedRuntimeTemplateFile, fullInstallPath, createRuntimeTemplateDirectory, downloadedRuntimeTemplate, reInstall, providerType));
        }

        public OASISResult<IInstalledRuntime> InstallRuntimeTemplate(Guid avatarId, string fullPathToPublishedRuntimeTemplateFile, string fullInstallPath, bool createRuntimeTemplateDirectory = true, IDownloadedRuntime downloadedRuntimeTemplate = null, bool reInstall = false, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.Install(avatarId, fullPathToPublishedRuntimeTemplateFile, fullInstallPath, createRuntimeTemplateDirectory, downloadedRuntimeTemplate, reInstall, providerType));
        }

        public async Task<OASISResult<IInstalledRuntime>> UninstallRuntimeTemplateAsync(IInstalledRuntime installedRuntimeTemplate, Guid avatarId, string errorMessage, ProviderType providerType)
        {
            return ProcessResult(await base.UninstallAsync(avatarId, (InstalledRuntime)installedRuntimeTemplate, errorMessage, providerType));
        }

        public OASISResult<IInstalledRuntime> UninstallRuntimeTemplate(Guid avatarId, IInstalledRuntime installedRuntimeTemplate, string errorMessage, ProviderType providerType)
        {
            return ProcessResult(base.Uninstall(avatarId, (InstalledRuntime)installedRuntimeTemplate, errorMessage, providerType));
        }

        public async Task<OASISResult<IInstalledRuntime>> UninstallRuntimeTemplateAsync(Guid avatarId, Guid RuntimeTemplateId, int versionSequence, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.UninstallAsync(avatarId, RuntimeTemplateId, versionSequence, providerType));
        }

        public OASISResult<IInstalledRuntime> UninstallRuntimeTemplate(Guid avatarId, Guid RuntimeTemplateId, int versionSequence, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.Uninstall(avatarId, RuntimeTemplateId, versionSequence, providerType));
        }

        public async Task<OASISResult<IInstalledRuntime>> UninstallRuntimeTemplateAsync(Guid avatarId, Guid RuntimeTemplateId, string version, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.UninstallAsync(avatarId, RuntimeTemplateId, version, providerType));
        }

        public OASISResult<IInstalledRuntime> UninstallRuntimeTemplate(Guid avatarId, Guid RuntimeTemplateId, string version, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.Uninstall(avatarId, RuntimeTemplateId, version, providerType));
        }

        public async Task<OASISResult<IInstalledRuntime>> UninstallRuntimeTemplateAsync(Guid avatarId, string RuntimeTemplateName, int versionSequence, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.UninstallAsync(avatarId, RuntimeTemplateName, versionSequence, providerType));
        }

        public OASISResult<IInstalledRuntime> UninstallRuntimeTemplate(Guid avatarId, string RuntimeTemplateName, int versionSequence, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.Uninstall(avatarId, RuntimeTemplateName, versionSequence, providerType));
        }

        public async Task<OASISResult<IInstalledRuntime>> UninstallRuntimeTemplateAsync(Guid avatarId, string RuntimeTemplateName, string version, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.UninstallAsync(avatarId, RuntimeTemplateName, version, providerType));
        }

        public OASISResult<IInstalledRuntime> UninstallRuntimeTemplate(Guid avatarId, string RuntimeTemplateName, string version, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.Uninstall(avatarId, RuntimeTemplateName, version, providerType));
        }

        public async Task<OASISResult<IEnumerable<IInstalledRuntime>>> ListInstalledRuntimeTemplatesAsync(Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResults(await base.ListInstalledAsync(avatarId, providerType));
        }

        public OASISResult<IEnumerable<IInstalledRuntime>> ListInstalledRuntimeTemplates(Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResults(base.ListInstalled(avatarId, providerType));
        }

        public async Task<OASISResult<IEnumerable<IInstalledRuntime>>> ListUnInstalledRuntimeTemplatesAsync(Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResults(await base.ListUninstalledAsync(avatarId, providerType));
        }

        public OASISResult<IEnumerable<IInstalledRuntime>> ListUnInstalledRuntimeTemplates(Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResults(base.ListUninstalled(avatarId, providerType));
        }

        public async Task<OASISResult<IEnumerable<IRuntime>>> ListUnpublishedRuntimeTemplatesAsync(Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResults(await base.ListUnpublishedAsync(avatarId, providerType));
        }

        public OASISResult<IEnumerable<IRuntime>> ListUnpublishedRuntimeTemplates(Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResults(base.ListUnpublished(avatarId, providerType));
        }

        public async Task<OASISResult<IEnumerable<IRuntime>>> ListDeactivatedRuntimeTemplatesAsync(Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResults(await base.ListDeactivatedAsync(avatarId, providerType));
        }

        public OASISResult<IEnumerable<IRuntime>> ListDeactivatedRuntimeTemplates(Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResults(base.ListDeactivated(avatarId, providerType));
        }

        public async Task<OASISResult<bool>> IsRuntimeTemplateInstalledAsync(Guid avatarId, Guid RuntimeTemplateId, int versionSequence, ProviderType providerType = ProviderType.Default)
        {
            return await base.IsInstalledAsync(avatarId, RuntimeTemplateId, versionSequence, providerType);
        }

        public OASISResult<bool> IsRuntimeTemplateInstalled(Guid avatarId, Guid RuntimeTemplateId, int versionSequence, ProviderType providerType = ProviderType.Default)
        {
            return base.IsInstalled(avatarId, RuntimeTemplateId, versionSequence, providerType);
        }

        public async Task<OASISResult<bool>> IsRuntimeTemplateInstalledAsync(Guid avatarId, Guid RuntimeTemplateId, string version, ProviderType providerType = ProviderType.Default)
        {
            return await base.IsInstalledAsync(avatarId, RuntimeTemplateId, version, providerType);
        }

        public OASISResult<bool> IsRuntimeTemplateInstalled(Guid avatarId, Guid RuntimeTemplateId, string version, ProviderType providerType = ProviderType.Default)
        {
            return base.IsInstalled(avatarId, RuntimeTemplateId, version, providerType);
        }

        public async Task<OASISResult<bool>> IsRuntimeTemplateInstalledAsync(Guid avatarId, string RuntimeTemplateName, int versionSequence, ProviderType providerType = ProviderType.Default)
        {
            return await base.IsInstalledAsync(avatarId, RuntimeTemplateName, versionSequence, providerType);
        }

        public OASISResult<bool> IsRuntimeTemplateInstalled(Guid avatarId, string RuntimeTemplateName, int versionSequence, ProviderType providerType = ProviderType.Default)
        {
            return base.IsInstalled(avatarId, RuntimeTemplateName, versionSequence, providerType);
        }

        public async Task<OASISResult<bool>> IsRuntimeTemplateInstalledAsync(Guid avatarId, string RuntimeTemplateName, string version, ProviderType providerType = ProviderType.Default)
        {
            return await base.IsInstalledAsync(avatarId, RuntimeTemplateName, version, providerType);
        }

        public OASISResult<bool> IsRuntimeTemplateInstalled(Guid avatarId, string RuntimeTemplateName, string version, ProviderType providerType = ProviderType.Default)
        {
            return base.IsInstalled(avatarId, RuntimeTemplateName, version, providerType);
        }

        public async Task<OASISResult<IInstalledRuntime>> LoadInstalledRuntimeTemplateAsync(Guid avatarId, Guid RuntimeTemplateId, int versionSequence = 0, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.LoadInstalledAsync(avatarId, RuntimeTemplateId, versionSequence, providerType));
        }

        public OASISResult<IInstalledRuntime> LoadInstalledRuntimeTemplate(Guid avatarId, Guid RuntimeTemplateId, int versionSequence = 0, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.LoadInstalled(avatarId, RuntimeTemplateId, versionSequence, providerType));
        }

        public async Task<OASISResult<IInstalledRuntime>> LoadInstalledRuntimeTemplateAsync(Guid avatarId, string RuntimeTemplateName, int versionSequence = 0, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.LoadInstalledAsync(avatarId, RuntimeTemplateName, versionSequence, providerType));
        }

        public OASISResult<IInstalledRuntime> LoadInstalledRuntimeTemplate(Guid avatarId, string RuntimeTemplateName, int versionSequence = 0, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.LoadInstalled(avatarId, RuntimeTemplateName, versionSequence, providerType));
        }

        public async Task<OASISResult<IInstalledRuntime>> LoadInstalledRuntimeTemplateAsync(Guid avatarId, Guid RuntimeTemplateId, string version, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.LoadInstalledAsync(avatarId, RuntimeTemplateId, version, providerType));
        }

        public OASISResult<IInstalledRuntime> LoadInstalledRuntimeTemplate(Guid avatarId, Guid RuntimeTemplateId, string version, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.LoadInstalled(avatarId, RuntimeTemplateId, version, providerType));
        }

        public async Task<OASISResult<IInstalledRuntime>> LoadInstalledRuntimeTemplateAsync(Guid avatarId, string RuntimeTemplateName, string version, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.LoadInstalledAsync(avatarId, RuntimeTemplateName, version, providerType));
        }

        public OASISResult<IInstalledRuntime> LoadInstalledRuntimeTemplate(Guid avatarId, string RuntimeTemplateName, string version, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.LoadInstalled(avatarId, RuntimeTemplateName, version, providerType));
        }

        public async Task<OASISResult<IInstalledRuntime>> LoadInstalledRuntimeTemplateAsync(Guid avatarId, Guid RuntimeTemplateId, bool active, int versionSequence = 0, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.LoadInstalledAsync(avatarId, RuntimeTemplateId, active, versionSequence, providerType));
        }

        public OASISResult<IInstalledRuntime> LoadInstalledRuntimeTemplate(Guid avatarId, Guid RuntimeTemplateId, bool active, int versionSequence = 0, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.LoadInstalled(avatarId, RuntimeTemplateId, active, versionSequence, providerType));
        }

        public async Task<OASISResult<IInstalledRuntime>> LoadInstalledRuntimeTemplateAsync(Guid avatarId, string RuntimeTemplateName, bool active, int versionSequence = 0, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.LoadInstalledAsync(avatarId, RuntimeTemplateName, active, versionSequence, providerType));
        }

        public OASISResult<IInstalledRuntime> LoadInstalledRuntimeTemplate(Guid avatarId, string RuntimeTemplateName, bool active, int versionSequence = 0, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.LoadInstalled(avatarId, RuntimeTemplateName, active, versionSequence, providerType));
        }

        public async Task<OASISResult<IInstalledRuntime>> LoadInstalledRuntimeTemplateAsync(Guid avatarId, Guid RuntimeTemplateId, string version, bool active, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.LoadInstalledAsync(avatarId, RuntimeTemplateId, version, active, providerType));
        }

        public OASISResult<IInstalledRuntime> LoadInstalledRuntimeTemplate(Guid avatarId, Guid RuntimeTemplateId, string version, bool active, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.LoadInstalled(avatarId, RuntimeTemplateId, version, active, providerType));
        }

        public async Task<OASISResult<IInstalledRuntime>> LoadInstalledRuntimeTemplateAsync(Guid avatarId, string RuntimeTemplateName, string version, bool active, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.LoadInstalledAsync(avatarId, RuntimeTemplateName, version, active, providerType));
        }

        public OASISResult<IInstalledRuntime> LoadInstalledRuntimeTemplate(Guid avatarId, string RuntimeTemplateName, string version, bool active, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.LoadInstalled(avatarId, RuntimeTemplateName, version, active, providerType));
        }

        public OASISResult<IInstalledRuntime> OpenRuntimeTemplateFolder(Guid avatarId, IInstalledRuntime Runtime)
        {
            return ProcessResult(base.OpenOAPPSystemHolonFolder(avatarId, (InstalledRuntime)Runtime));
        }

        public async Task<OASISResult<IInstalledRuntime>> OpenRuntimeTemplateFolderAsync(Guid avatarId, Guid RuntimeTemplateId, int versionSequence, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.OpenOAPPSystemHolonFolderAsync(avatarId, RuntimeTemplateId, versionSequence, providerType));
        }

        public OASISResult<IInstalledRuntime> OpenRuntimeTemplateFolder(Guid avatarId, Guid RuntimeTemplateId, int versionSequence, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.OpenOAPPSystemHolonFolder(avatarId, RuntimeTemplateId, versionSequence, providerType));
        }

        public async Task<OASISResult<IInstalledRuntime>> OpenRuntimeTemplateFolderAsync(Guid avatarId, Guid RuntimeTemplateId, string version, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.OpenOAPPSystemHolonFolderAsync(avatarId, RuntimeTemplateId, version, providerType));
        }

        public OASISResult<IInstalledRuntime> OpenRuntimeTemplateFolder(Guid avatarId, Guid RuntimeTemplateId, string version, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.OpenOAPPSystemHolonFolder(avatarId, RuntimeTemplateId, version, providerType));
        }

        public async Task<OASISResult<bool>> WriteRuntimeTemplateDNAAsync(IOAPPSystemHolonDNA RuntimeTemplateDNA, string fullPathToRuntimeTemplate)
        {
            return await base.WriteOAPPSystemHolonDNAAsync(RuntimeTemplateDNA, fullPathToRuntimeTemplate);
        }

        public OASISResult<bool> WriteRuntimeTemplateDNA(IOAPPSystemHolonDNA RuntimeTemplateDNA, string fullPathToRuntimeTemplate)
        {
            return base.WriteOAPPSystemHolonDNA(RuntimeTemplateDNA, fullPathToRuntimeTemplate);
        }

        public async Task<OASISResult<IOAPPSystemHolonDNA>> ReadRuntimeTemplateDNAFromSourceOrInstalledFolderAsync(string fullPathToRuntimeTemplateFolder)
        {
            return await base.ReadOAPPSystemHolonDNAFromSourceOrInstallFolderAsync(fullPathToRuntimeTemplateFolder);
        }

        public OASISResult<IOAPPSystemHolonDNA> ReadRuntimeTemplateDNAFromSourceOrInstalledFolder(string fullPathToRuntimeTemplateFolder)
        {
            return base.ReadOAPPSystemHolonDNAFromSourceOrInstallFolder(fullPathToRuntimeTemplateFolder);
        }

        public async Task<OASISResult<IOAPPSystemHolonDNA>> ReadRuntimeTemplateDNAFromPublishedRuntimeTemplateFileAsync(string fullPathToRuntimeTemplateFolder)
        {
            return await base.ReadOAPPSystemHolonDNAFromPublishedFileAsync(fullPathToRuntimeTemplateFolder);
        }

        public OASISResult<IOAPPSystemHolonDNA> ReadRuntimeTemplateDNAFromPublishedRuntimeTemplateFile(string fullPathToRuntimeTemplateFolder)
        {
            return base.ReadOAPPSystemHolonDNAFromPublishedFile(fullPathToRuntimeTemplateFolder);
        }

        private OASISResult<IEnumerable<IRuntime>> ProcessResults(OASISResult<IEnumerable<Runtime>> operationResult)
        {
            OASISResult<IEnumerable<IRuntime>> result = new OASISResult<IEnumerable<IRuntime>>();

            if (operationResult != null && operationResult.Result != null && !operationResult.IsError && operationResult.Result.Count() > 0)
            {
                List<IRuntime> oappTemplates = new List<IRuntime>();

                foreach (IRuntime template in operationResult.Result)
                    oappTemplates.Add(template);

                result.Result = oappTemplates;
            }

            OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(operationResult, result);
            return result;
        }

        private OASISResult<IEnumerable<IInstalledRuntime>> ProcessResults(OASISResult<IEnumerable<InstalledRuntime>> operationResult)
        {
            OASISResult<IEnumerable<IInstalledRuntime>> result = new OASISResult<IEnumerable<IInstalledRuntime>>();

            if (operationResult != null && operationResult.Result != null && !operationResult.IsError && operationResult.Result.Count() > 0)
            {
                List<IInstalledRuntime> oappTemplates = new List<IInstalledRuntime>();

                foreach (IInstalledRuntime template in operationResult.Result)
                    oappTemplates.Add(template);

                result.Result = oappTemplates;
            }

            OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(operationResult, result);
            return result;
        }

        private OASISResult<IRuntime> ProcessResult(OASISResult<Runtime> operationResult)
        {
            OASISResult<IRuntime> result = new OASISResult<IRuntime>();
            result.Result = operationResult.Result;
            OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(operationResult, result);
            return result;
        }

        private OASISResult<IDownloadedRuntime> ProcessResult(OASISResult<DownloadedRuntime> operationResult)
        {
            OASISResult<IDownloadedRuntime> result = new OASISResult<IDownloadedRuntime>();
            result.Result = operationResult.Result;
            OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(operationResult, result);
            return result;
        }

        private OASISResult<IInstalledRuntime> ProcessResult(OASISResult<InstalledRuntime> operationResult)
        {
            OASISResult<IInstalledRuntime> result = new OASISResult<IInstalledRuntime>();
            result.Result = operationResult.Result;
            OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(operationResult, result);
            return result;
        }
    }
}