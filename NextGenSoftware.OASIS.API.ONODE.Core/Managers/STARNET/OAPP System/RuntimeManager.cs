using System;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.ONODE.Core.Holons;
using NextGenSoftware.OASIS.API.ONODE.Core.Managers.Base;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Holons;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Managers
{
    //public class RuntimeManager : STARNETManagerBase<Runtime, DownloadedRuntime, InstalledRuntime, RuntimeDNA>
    public class RuntimeManager : STARNETManagerBase<Runtime, DownloadedRuntime, InstalledRuntime, STARNETDNA>
    {
        public RuntimeManager(Guid avatarId, OASISDNA OASISDNA = null) : base(avatarId, 
            OASISDNA,
            typeof(RuntimeType),
            HolonType.Runtime,
            HolonType.InstalledRuntime,
            "Runtime",
            "RuntimeId",
            "RuntimeName",
            "RuntimeType",
            "oruntime",
            "oasis_runtimes",
            "RuntimeDNA.json",
            "RuntimeDNAJSON")
        { }

        public RuntimeManager(IOASISStorageProvider OASISStorageProvider, Guid avatarId, OASISDNA OASISDNA = null) : base(OASISStorageProvider, avatarId,
            OASISDNA,
            typeof(RuntimeType),
            HolonType.Runtime,
            HolonType.InstalledRuntime,
            "Runtime",
            "RuntimeId",
            "RuntimeName",
            "RuntimeType",
            "oruntime",
            "oasis_runtimes",
            "RuntimeDNA.json",
            "RuntimeDNAJSON")
        { }


        public async Task<OASISResult<IInstalledRuntime>> DownloadAndInstallOASISRuntimeAsync(Guid avatarId, string version, string downloadPath, string installPath, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.DownloadAndInstallAsync(avatarId, "OASIS Runtime", version, installPath, downloadPath, providerType: providerType));
        }

        public OASISResult<IInstalledRuntime> DownloadAndInstallOASISRuntime(Guid avatarId, string version, string downloadPath, string installPath, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.DownloadAndInstall(avatarId, "OASIS Runtime", version, installPath, downloadPath, providerType: providerType));
        }

        public async Task<OASISResult<IInstalledRuntime>> DownloadAndInstallSTARRuntimeAsync(Guid avatarId, string version, string downloadPath, string installPath, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.DownloadAndInstallAsync(avatarId, "STAR Runtime", version, installPath, downloadPath, providerType: providerType));
        }

        public OASISResult<IInstalledRuntime> DownloadAndInstallSTARRuntime(Guid avatarId, string version, string downloadPath, string installPath, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.DownloadAndInstall(avatarId, "STAR Runtime", version, installPath, downloadPath, providerType: providerType));
        }

        /*
        public async Task<OASISResult<IRuntime>> CreateRuntimeAsync(Guid avatarId, string name, string description, RuntimeType runtimeType, string fullPathToRuntime, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.CreateAsync(avatarId, name, description, runtimeType, fullPathToRuntime, null, providerType));
        }

        public OASISResult<IRuntime> CreateRuntime(Guid avatarId, string name, string description, RuntimeType runtimeType, string fullPathToRuntime, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.Create(avatarId, name, description, runtimeType, fullPathToRuntime, null, providerType));
        }

        #region COSMICManagerBase
        public async Task<OASISResult<IRuntime>> SaveRuntimeAsync(Guid avatarId, IRuntime oappTemplate, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.SaveAsync(avatarId, (Runtime)oappTemplate, providerType));
        }

        public OASISResult<IRuntime> SaveRuntime(Guid avatarId, IRuntime oappTemplate, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.Save(avatarId, (Runtime)oappTemplate, providerType));
        }

        public async Task<OASISResult<IRuntime>> LoadRuntimeAsync(Guid avatarId, Guid RuntimeId, int version = 0, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.LoadAsync(avatarId, RuntimeId, version, providerType));
        }

        public OASISResult<IRuntime> LoadRuntime(Guid avatarId, Guid RuntimeId, int version = 0, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.Load(avatarId, RuntimeId, version, providerType));
        }

        public async Task<OASISResult<IEnumerable<IRuntime>>> LoadAllRuntimesAsync(Guid avatarId, RuntimeType runtimeType = RuntimeType.STAR, bool loadAllTypes = true, bool showAllVersions = false, int version = 0, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResults(await base.LoadAllAsync(avatarId, runtimeType, loadAllTypes, showAllVersions, version, HolonType.Default, "Default", providerType));
        }

        public OASISResult<IEnumerable<IRuntime>> LoadAllRuntimes(Guid avatarId, RuntimeType runtimeType = RuntimeType.STAR, bool loadAllTypes = true, bool showAllVersions = false, int version = 0, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResults(base.LoadAll(avatarId, runtimeType, showAllVersions, showAllVersions, version, HolonType.Default, "Default", providerType));
        }

        public async Task<OASISResult<IEnumerable<IRuntime>>> LoadAllRuntimesForAvatarAsync(Guid avatarId, bool showAllVersions = false, int version = 0, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResults(await base.LoadAllForAvatarAsync(avatarId, showAllVersions, version, providerType));
        }

        public OASISResult<IEnumerable<IRuntime>> LoadAllRuntimesForAvatar(Guid avatarId, bool showAllVersions = false, int version = 0, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResults(base.LoadAllForAvatar(avatarId, showAllVersions, version, providerType));
        }

        public async Task<OASISResult<IEnumerable<IRuntime>>> SearchRuntimesAsync(Guid avatarId, string searchTerm, bool searchOnlyForCurrentAvatar = true, bool showAllVersions = false, int version = 0, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResults(await base.SearchAsync(avatarId, searchTerm, searchOnlyForCurrentAvatar, showAllVersions, version, providerType));
        }

        public OASISResult<IEnumerable<IRuntime>> SearchRuntimes(Guid avatarId, string searchTerm, bool searchOnlyForCurrentAvatar = true, bool showAllVersions = false, int version = 0, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResults(base.Search(avatarId, searchTerm, searchOnlyForCurrentAvatar, showAllVersions, version, providerType));
        }

        public async Task<OASISResult<IRuntime>> DeleteRuntimeAsync(Guid avatarId, Guid oappTemplateId, int version, bool softDelete = true, bool deleteDownload = true, bool deleteInstall = true, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.DeleteAsync(avatarId, oappTemplateId, version, softDelete, deleteDownload, deleteInstall, providerType));
        }

        public OASISResult<IRuntime> DeleteRuntime(Guid avatarId, Guid oappTemplateId, int version, bool softDelete = true, bool deleteDownload = true, bool deleteInstall = true, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.Delete(avatarId, oappTemplateId, version, softDelete, deleteDownload, deleteInstall, providerType));
        }

        public async Task<OASISResult<IRuntime>> DeleteRuntimeAsync(Guid avatarId, IRuntime oappTemplate, int version, bool softDelete = true, bool deleteDownload = true, bool deleteInstall = true, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.DeleteAsync(avatarId, oappTemplate, version, softDelete, deleteDownload, deleteInstall, providerType));
        }

        public OASISResult<IRuntime> DeleteRuntime(Guid avatarId, IRuntime oappTemplate, int version, bool softDelete = true, bool deleteDownload = true, bool deleteInstall = true, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.Delete(avatarId, oappTemplate, version, softDelete, deleteDownload, deleteInstall, providerType));
        }
        #endregion

        public async Task<OASISResult<IEnumerable<IRuntime>>> LoadRuntimeVersionsAsync(Guid RuntimeId, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResults(await base.LoadVersionsAsync(RuntimeId, providerType));
        }

        public OASISResult<IEnumerable<IRuntime>> LoadRuntimeVersions(Guid RuntimeId, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResults(base.LoadVersions(RuntimeId, providerType));
        }

        public async Task<OASISResult<IRuntime>> LoadRuntimeVersionAsync(Guid RuntimeId, string version, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.LoadVersionAsync(RuntimeId, version, providerType));
        }

        public OASISResult<IRuntime> LoadRuntimeVersion(Guid RuntimeId, string version, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.LoadVersion(RuntimeId, version, providerType));
        }

        public async Task<OASISResult<IRuntime>> EditRuntimeAsync(Guid RuntimeId, ISTARNETHolonDNA newRuntimeDNA, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.EditAsync(RuntimeId, newRuntimeDNA, avatarId, providerType));
        }

        public async Task<OASISResult<IRuntime>> EditRuntimeAsync(IRuntime Runtime, ISTARNETHolonDNA newRuntimeDNA, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.EditAsync(avatarId, (Runtime)Runtime, newRuntimeDNA, providerType));
        }

        public async Task<OASISResult<IRuntime>> PublishRuntimeAsync(Guid avatarId, string fullPathToRuntime, string launchTarget, string fullPathToPublishTo = "", bool registerOnSTARNET = true, bool generateRuntimeBinary = true, bool uploadRuntimeToCloud = false, bool edit = false, ProviderType providerType = ProviderType.Default, ProviderType oappBinaryProviderType = ProviderType.IPFSOASIS)
        {
            return ProcessResult(await base.PublishAsync(avatarId, fullPathToRuntime, launchTarget, fullPathToPublishTo, registerOnSTARNET, generateRuntimeBinary, uploadRuntimeToCloud, edit, providerType, oappBinaryProviderType));
        }

        public OASISResult<IRuntime> PublishRuntime(Guid avatarId, string fullPathToRuntime, string launchTarget, string fullPathToPublishTo = "", bool registerOnSTARNET = true, bool generateRuntimeBinary = true, bool uploadRuntimeToCloud = false, bool edit = false, ProviderType providerType = ProviderType.Default, ProviderType oappBinaryProviderType = ProviderType.IPFSOASIS)
        {
            return ProcessResult(base.Publish(avatarId, fullPathToRuntime, launchTarget, fullPathToPublishTo, registerOnSTARNET, generateRuntimeBinary, uploadRuntimeToCloud, edit, providerType, oappBinaryProviderType));
        }

        public async Task<OASISResult<IRuntime>> UnpublishRuntimeAsync(Guid avatarId, IRuntime Runtime, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.UnpublishAsync(avatarId, (Runtime)Runtime, providerType));
        }

        public OASISResult<IRuntime> UnpublishRuntime(Guid avatarId, IRuntime Runtime, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.Unpublish(avatarId, (Runtime)Runtime, providerType));
        }

        public async Task<OASISResult<IRuntime>> UnpublishRuntimeAsync(Guid avatarId, Guid RuntimeId, int version, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.UnpublishAsync(avatarId, RuntimeId, version, providerType));
        }

        public OASISResult<IRuntime> UnpublishRuntime(Guid avatarId, Guid RuntimeId, int version, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.Unpublish(avatarId, RuntimeId, version, providerType));
        }

        public async Task<OASISResult<IRuntime>> UnpublishRuntimeAsync(Guid avatarId, ISTARNETHolonDNA RuntimeDNA, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.UnpublishAsync(avatarId, RuntimeDNA, providerType));
        }

        public OASISResult<IRuntime> UnpublishRuntime(Guid avatarId, ISTARNETHolonDNA RuntimeDNA, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.Unpublish(avatarId, RuntimeDNA, providerType));
        }

        public async Task<OASISResult<IRuntime>> RepublishRuntimeAsync(Guid avatarId, IRuntime Runtime, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.RepublishAsync(avatarId, (Runtime)Runtime, providerType));
        }

        public OASISResult<IRuntime> RepublishRuntime(Guid avatarId, IRuntime Runtime, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.Republish(avatarId, (Runtime)Runtime, providerType));
        }

        public async Task<OASISResult<IRuntime>> RepublishRuntimeAsync(Guid avatarId, ISTARNETHolonDNA RuntimeDNA, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.RepublishAsync(avatarId, RuntimeDNA, providerType));
        }

        public OASISResult<IRuntime> RepublishRuntime(Guid avatarId, ISTARNETHolonDNA RuntimeDNA, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.Republish(avatarId, RuntimeDNA, providerType));
        }

        public async Task<OASISResult<IRuntime>> RepublishRuntimeAsync(Guid RuntimeId, int version, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.RepublishAsync(avatarId, RuntimeId, version, providerType));
        }

        public OASISResult<IRuntime> RepublishRuntime(Guid RuntimeId, int version, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.Republish(avatarId, RuntimeId, version, providerType));
        }

        public async Task<OASISResult<IRuntime>> DeactivateRuntimeAsync(Guid avatarId, IRuntime Runtime, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.DeactivateAsync(avatarId, (Runtime)Runtime, providerType));
        }

        public OASISResult<IRuntime> DeactivateRuntime(Guid avatarId, IRuntime Runtime, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.Deactivate(avatarId, (Runtime)Runtime, providerType));
        }

        public async Task<OASISResult<IRuntime>> DeactivateRuntimeAsync(Guid RuntimeId, int version, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.DeactivateAsync(avatarId, RuntimeId, version, providerType));
        }

        public OASISResult<IRuntime> DeactivateRuntime(Guid RuntimeId, int version, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.Deactivate(avatarId, RuntimeId, version, providerType));
        }

        public async Task<OASISResult<IRuntime>> DeactivateRuntimeAsync(Guid avatarId, ISTARNETHolonDNA RuntimeDNA, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.DeactivateAsync(avatarId, RuntimeDNA, providerType));
        }

        public OASISResult<IRuntime> DeactivateRuntime(Guid avatarId, ISTARNETHolonDNA RuntimeDNA, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.Deactivate(avatarId, RuntimeDNA, providerType));
        }

        public async Task<OASISResult<IRuntime>> ActivateRuntimeAsync(Guid avatarId, IRuntime Runtime, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.ActivateAsync(avatarId, (Runtime)Runtime, providerType));
        }

        public OASISResult<IRuntime> ActivateRuntime(Guid avatarId, IRuntime Runtime, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.Activate(avatarId, (Runtime)Runtime, providerType));
        }

        public async Task<OASISResult<IRuntime>> ActivateRuntimeAsync(Guid avatarId, ISTARNETHolonDNA RuntimeDNA, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.ActivateAsync(avatarId, RuntimeDNA, providerType));
        }

        public OASISResult<IRuntime> ActivateRuntime(Guid avatarId, ISTARNETHolonDNA RuntimeDNA, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.Activate(avatarId, RuntimeDNA, providerType));
        }

        public async Task<OASISResult<IRuntime>> ActivateRuntimeAsync(Guid avatarId, Guid RuntimeId, int version, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.ActivateAsync(avatarId, RuntimeId, version, providerType));
        }

        public OASISResult<IRuntime> ActivateRuntime(Guid avatarId, Guid RuntimeId, int version, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.Activate(avatarId, RuntimeId, version, providerType));
        }

        public async Task<OASISResult<IDownloadedRuntime>> DownloadRuntimeAsync(Guid avatarId, IRuntime Runtime, string fullDownloadPath, bool reInstall = false, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.DownloadAsync(avatarId, (Runtime)Runtime, fullDownloadPath, reInstall, providerType));
        }

        public OASISResult<IDownloadedRuntime> DownloadRuntime(Guid avatarId, IRuntime Runtime, string fullDownloadPath, bool reInstall = false, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.Download(avatarId, (Runtime)Runtime, fullDownloadPath, reInstall, providerType));
        }

        public async Task<OASISResult<IInstalledRuntime>> DownloadAndInstallRuntimeAsync(Guid avatarId, IRuntime Runtime, string fullInstallPath, string fullDownloadPath = "", bool createRuntimeDirectory = true, bool reInstall = false, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.DownloadAndInstallAsync(avatarId, (Runtime)Runtime, fullInstallPath, fullDownloadPath, createRuntimeDirectory, reInstall, providerType));
        }

        public OASISResult<IInstalledRuntime> DownloadAndInstallRuntime(Guid avatarId, IRuntime Runtime, string fullInstallPath, string fullDownloadPath = "", bool createRuntimeDirectory = true, bool reInstall = false, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.DownloadAndInstall(avatarId, (Runtime)Runtime, fullInstallPath, fullDownloadPath, createRuntimeDirectory, reInstall, providerType));
        }

        public async Task<OASISResult<IInstalledRuntime>> DownloadAndInstallRuntimeAsync(Guid avatarId, Guid RuntimeId, int version, string fullInstallPath, string fullDownloadPath = "", bool createRuntimeDirectory = true, bool reInstall = false, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.DownloadAndInstallAsync(avatarId, RuntimeId, version, fullInstallPath, fullDownloadPath, createRuntimeDirectory, reInstall, providerType));
        }

        public async Task<OASISResult<IInstalledRuntime>> DownloadAndInstallRuntime(Guid avatarId, Guid RuntimeId, int version, string fullInstallPath, string fullDownloadPath = "", bool createRuntimeDirectory = true, bool reInstall = false, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.DownloadAndInstall(avatarId, RuntimeId, version, fullInstallPath, fullDownloadPath, createRuntimeDirectory, reInstall, providerType));
        }


        public async Task<OASISResult<IInstalledRuntime>> InstallRuntimeAsync(Guid avatarId, string fullPathToPublishedRuntimeFile, string fullInstallPath, bool createRuntimeDirectory = true, IDownloadedRuntime downloadedRuntime = null, bool reInstall = false, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.InstallAsync(avatarId, fullPathToPublishedRuntimeFile, fullInstallPath, createRuntimeDirectory, downloadedRuntime, reInstall, providerType));
        }

        public OASISResult<IInstalledRuntime> InstallRuntime(Guid avatarId, string fullPathToPublishedRuntimeFile, string fullInstallPath, bool createRuntimeDirectory = true, IDownloadedRuntime downloadedRuntime = null, bool reInstall = false, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.Install(avatarId, fullPathToPublishedRuntimeFile, fullInstallPath, createRuntimeDirectory, downloadedRuntime, reInstall, providerType));
        }

        public async Task<OASISResult<IInstalledRuntime>> UninstallRuntimeAsync(IInstalledRuntime installedRuntime, Guid avatarId, string errorMessage, ProviderType providerType)
        {
            return ProcessResult(await base.UninstallAsync(avatarId, (InstalledRuntime)installedRuntime, errorMessage, providerType));
        }

        public OASISResult<IInstalledRuntime> UninstallRuntime(Guid avatarId, IInstalledRuntime installedRuntime, string errorMessage, ProviderType providerType)
        {
            return ProcessResult(base.Uninstall(avatarId, (InstalledRuntime)installedRuntime, errorMessage, providerType));
        }

        public async Task<OASISResult<IInstalledRuntime>> UninstallRuntimeAsync(Guid avatarId, Guid RuntimeId, int versionSequence, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.UninstallAsync(avatarId, RuntimeId, versionSequence, providerType));
        }

        public OASISResult<IInstalledRuntime> UninstallRuntime(Guid avatarId, Guid RuntimeId, int versionSequence, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.Uninstall(avatarId, RuntimeId, versionSequence, providerType));
        }

        public async Task<OASISResult<IInstalledRuntime>> UninstallRuntimeAsync(Guid avatarId, Guid RuntimeId, string version, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.UninstallAsync(avatarId, RuntimeId, version, providerType));
        }

        public OASISResult<IInstalledRuntime> UninstallRuntime(Guid avatarId, Guid RuntimeId, string version, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.Uninstall(avatarId, RuntimeId, version, providerType));
        }

        public async Task<OASISResult<IInstalledRuntime>> UninstallRuntimeAsync(Guid avatarId, string RuntimeName, int versionSequence, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.UninstallAsync(avatarId, RuntimeName, versionSequence, providerType));
        }

        public OASISResult<IInstalledRuntime> UninstallRuntime(Guid avatarId, string RuntimeName, int versionSequence, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.Uninstall(avatarId, RuntimeName, versionSequence, providerType));
        }

        public async Task<OASISResult<IInstalledRuntime>> UninstallRuntimeAsync(Guid avatarId, string RuntimeName, string version, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.UninstallAsync(avatarId, RuntimeName, version, providerType));
        }

        public OASISResult<IInstalledRuntime> UninstallRuntime(Guid avatarId, string RuntimeName, string version, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.Uninstall(avatarId, RuntimeName, version, providerType));
        }

        public async Task<OASISResult<IEnumerable<IInstalledRuntime>>> ListInstalledRuntimesAsync(Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResults(await base.ListInstalledAsync(avatarId, providerType));
        }

        public OASISResult<IEnumerable<IInstalledRuntime>> ListInstalledRuntimes(Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResults(base.ListInstalled(avatarId, providerType));
        }

        public async Task<OASISResult<IEnumerable<IInstalledRuntime>>> ListUnInstalledRuntimesAsync(Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResults(await base.ListUninstalledAsync(avatarId, providerType));
        }

        public OASISResult<IEnumerable<IInstalledRuntime>> ListUnInstalledRuntimes(Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResults(base.ListUninstalled(avatarId, providerType));
        }

        public async Task<OASISResult<IEnumerable<IRuntime>>> ListUnpublishedRuntimesAsync(Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResults(await base.ListUnpublishedAsync(avatarId, providerType));
        }

        public OASISResult<IEnumerable<IRuntime>> ListUnpublishedRuntimes(Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResults(base.ListUnpublished(avatarId, providerType));
        }

        public async Task<OASISResult<IEnumerable<IRuntime>>> ListDeactivatedRuntimesAsync(Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResults(await base.ListDeactivatedAsync(avatarId, providerType));
        }

        public OASISResult<IEnumerable<IRuntime>> ListDeactivatedRuntimes(Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResults(base.ListDeactivated(avatarId, providerType));
        }

        public async Task<OASISResult<bool>> IsRuntimeInstalledAsync(Guid avatarId, Guid RuntimeId, int versionSequence, ProviderType providerType = ProviderType.Default)
        {
            return await base.IsInstalledAsync(avatarId, RuntimeId, versionSequence, providerType);
        }

        public OASISResult<bool> IsRuntimeInstalled(Guid avatarId, Guid RuntimeId, int versionSequence, ProviderType providerType = ProviderType.Default)
        {
            return base.IsInstalled(avatarId, RuntimeId, versionSequence, providerType);
        }

        public async Task<OASISResult<bool>> IsRuntimeInstalledAsync(Guid avatarId, Guid RuntimeId, string version, ProviderType providerType = ProviderType.Default)
        {
            return await base.IsInstalledAsync(avatarId, RuntimeId, version, providerType);
        }

        public OASISResult<bool> IsRuntimeInstalled(Guid avatarId, Guid RuntimeId, string version, ProviderType providerType = ProviderType.Default)
        {
            return base.IsInstalled(avatarId, RuntimeId, version, providerType);
        }

        public async Task<OASISResult<bool>> IsRuntimeInstalledAsync(Guid avatarId, string RuntimeName, int versionSequence, ProviderType providerType = ProviderType.Default)
        {
            return await base.IsInstalledAsync(avatarId, RuntimeName, versionSequence, providerType);
        }

        public OASISResult<bool> IsRuntimeInstalled(Guid avatarId, string RuntimeName, int versionSequence, ProviderType providerType = ProviderType.Default)
        {
            return base.IsInstalled(avatarId, RuntimeName, versionSequence, providerType);
        }

        public async Task<OASISResult<bool>> IsRuntimeInstalledAsync(Guid avatarId, string RuntimeName, string version, ProviderType providerType = ProviderType.Default)
        {
            return await base.IsInstalledAsync(avatarId, RuntimeName, version, providerType);
        }

        public OASISResult<bool> IsRuntimeInstalled(Guid avatarId, string RuntimeName, string version, ProviderType providerType = ProviderType.Default)
        {
            return base.IsInstalled(avatarId, RuntimeName, version, providerType);
        }

        public async Task<OASISResult<IInstalledRuntime>> LoadInstalledRuntimeAsync(Guid avatarId, Guid RuntimeId, int versionSequence = 0, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.LoadInstalledAsync(avatarId, RuntimeId, versionSequence, providerType));
        }

        public OASISResult<IInstalledRuntime> LoadInstalledRuntime(Guid avatarId, Guid RuntimeId, int versionSequence = 0, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.LoadInstalled(avatarId, RuntimeId, versionSequence, providerType));
        }

        public async Task<OASISResult<IInstalledRuntime>> LoadInstalledRuntimeAsync(Guid avatarId, string RuntimeName, int versionSequence = 0, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.LoadInstalledAsync(avatarId, RuntimeName, versionSequence, providerType));
        }

        public OASISResult<IInstalledRuntime> LoadInstalledRuntime(Guid avatarId, string RuntimeName, int versionSequence = 0, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.LoadInstalled(avatarId, RuntimeName, versionSequence, providerType));
        }

        public async Task<OASISResult<IInstalledRuntime>> LoadInstalledRuntimeAsync(Guid avatarId, Guid RuntimeId, string version, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.LoadInstalledAsync(avatarId, RuntimeId, version, providerType));
        }

        public OASISResult<IInstalledRuntime> LoadInstalledRuntime(Guid avatarId, Guid RuntimeId, string version, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.LoadInstalled(avatarId, RuntimeId, version, providerType));
        }

        public async Task<OASISResult<IInstalledRuntime>> LoadInstalledRuntimeAsync(Guid avatarId, string RuntimeName, string version, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.LoadInstalledAsync(avatarId, RuntimeName, version, providerType));
        }

        public OASISResult<IInstalledRuntime> LoadInstalledRuntime(Guid avatarId, string RuntimeName, string version, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.LoadInstalled(avatarId, RuntimeName, version, providerType));
        }

        public async Task<OASISResult<IInstalledRuntime>> LoadInstalledRuntimeAsync(Guid avatarId, Guid RuntimeId, bool active, int versionSequence = 0, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.LoadInstalledAsync(avatarId, RuntimeId, active, versionSequence, providerType));
        }

        public OASISResult<IInstalledRuntime> LoadInstalledRuntime(Guid avatarId, Guid RuntimeId, bool active, int versionSequence = 0, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.LoadInstalled(avatarId, RuntimeId, active, versionSequence, providerType));
        }

        public async Task<OASISResult<IInstalledRuntime>> LoadInstalledRuntimeAsync(Guid avatarId, string RuntimeName, bool active, int versionSequence = 0, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.LoadInstalledAsync(avatarId, RuntimeName, active, versionSequence, providerType));
        }

        public OASISResult<IInstalledRuntime> LoadInstalledRuntime(Guid avatarId, string RuntimeName, bool active, int versionSequence = 0, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.LoadInstalled(avatarId, RuntimeName, active, versionSequence, providerType));
        }

        public async Task<OASISResult<IInstalledRuntime>> LoadInstalledRuntimeAsync(Guid avatarId, Guid RuntimeId, string version, bool active, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.LoadInstalledAsync(avatarId, RuntimeId, version, active, providerType));
        }

        public OASISResult<IInstalledRuntime> LoadInstalledRuntime(Guid avatarId, Guid RuntimeId, string version, bool active, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.LoadInstalled(avatarId, RuntimeId, version, active, providerType));
        }

        public async Task<OASISResult<IInstalledRuntime>> LoadInstalledRuntimeAsync(Guid avatarId, string RuntimeName, string version, bool active, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.LoadInstalledAsync(avatarId, RuntimeName, version, active, providerType));
        }

        public OASISResult<IInstalledRuntime> LoadInstalledRuntime(Guid avatarId, string RuntimeName, string version, bool active, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.LoadInstalled(avatarId, RuntimeName, version, active, providerType));
        }

        public OASISResult<IInstalledRuntime> OpenRuntimeFolder(Guid avatarId, IInstalledRuntime Runtime)
        {
            return ProcessResult(base.OpenSTARHolonFolder(avatarId, (InstalledRuntime)Runtime));
        }

        public async Task<OASISResult<IInstalledRuntime>> OpenRuntimeFolderAsync(Guid avatarId, Guid RuntimeId, int versionSequence, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.OpenSTARHolonFolderAsync(avatarId, RuntimeId, versionSequence, providerType));
        }

        public OASISResult<IInstalledRuntime> OpenRuntimeFolder(Guid avatarId, Guid RuntimeId, int versionSequence, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.OpenSTARHolonFolder(avatarId, RuntimeId, versionSequence, providerType));
        }

        public async Task<OASISResult<IInstalledRuntime>> OpenRuntimeFolderAsync(Guid avatarId, Guid RuntimeId, string version, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.OpenSTARHolonFolderAsync(avatarId, RuntimeId, version, providerType));
        }

        public OASISResult<IInstalledRuntime> OpenRuntimeFolder(Guid avatarId, Guid RuntimeId, string version, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.OpenSTARHolonFolder(avatarId, RuntimeId, version, providerType));
        }

        //public async Task<OASISResult<bool>> WriteRuntimeDNAAsync(ISTARNETHolonDNA RuntimeDNA, string fullPathToRuntime)
        //{
        //    return await base.WriteSTARHolonDNAAsync(RuntimeDNA, fullPathToRuntime);
        //}

        //public OASISResult<bool> WriteRuntimeDNA(ISTARNETHolonDNA RuntimeDNA, string fullPathToRuntime)
        //{
        //    return base.WriteSTARHolonDNA(RuntimeDNA, fullPathToRuntime);
        //}

        //public async Task<OASISResult<ISTARNETHolonDNA>> ReadRuntimeDNAFromSourceOrInstalledFolderAsync(string fullPathToRuntimeFolder)
        //{
        //    return await base.ReadSTARHolonDNAFromSourceOrInstallFolderAsync(fullPathToRuntimeFolder);
        //}

        //public OASISResult<ISTARNETHolonDNA> ReadRuntimeDNAFromSourceOrInstalledFolder(string fullPathToRuntimeFolder)
        //{
        //    return base.ReadSTARHolonDNAFromSourceOrInstallFolder(fullPathToRuntimeFolder);
        //}

        //public async Task<OASISResult<ISTARNETHolonDNA>> ReadRuntimeDNAFromPublishedRuntimeFileAsync(string fullPathToRuntimeFolder)
        //{
        //    return await base.ReadSTARHolonDNAFromPublishedFileAsync(fullPathToRuntimeFolder);
        //}

        //public OASISResult<ISTARNETHolonDNA> ReadRuntimeDNAFromPublishedRuntimeFile(string fullPathToRuntimeFolder)
        //{
        //    return base.ReadSTARHolonDNAFromPublishedFile(fullPathToRuntimeFolder);
        //}

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

        private OASISResult<IDownloadedRuntime> ProcessResult(OASISResult<DownloadedQuest> operationResult)
        {
            OASISResult<IDownloadedRuntime> result = new OASISResult<IDownloadedRuntime>();
            result.Result = operationResult.Result;
            OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(operationResult, result);
            return result;
        }
        */

        private OASISResult<IInstalledRuntime> ProcessResult(OASISResult<InstalledRuntime> operationResult)
        {
            OASISResult<IInstalledRuntime> result = new OASISResult<IInstalledRuntime>();
            result.Result = (IInstalledRuntime)operationResult.Result;
            OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(operationResult, result);
            return result;
        }
    }
}