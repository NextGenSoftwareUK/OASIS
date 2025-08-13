using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.ONODE.Core.Managers.Base;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Holons;
using NextGenSoftware.OASIS.API.ONODE.Core.Holons;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Managers
{
    public interface ISTARNETManagerBase<T1, T2, T3, T4>
        where T1 : ISTARNETHolon, new()
        where T2 : IDownloadedSTARNETHolon, new()
        where T3 : IInstalledSTARNETHolon, new()
        where T4 : ISTARNETDNA, new()
    {
        string STARNETDNAFileName { get; set; }
        string STARNETDNAJSONName { get; set; }
        string STARNETHolonFileExtention { get; set; }
        string STARNETHolonGoogleBucket { get; set; }
        string STARNETHolonIdName { get; set; }
        HolonType STARNETHolonInstalledHolonType { get; set; }
        string STARNETHolonNameName { get; set; }
        Type STARNETHolonSubType { get; set; }
        HolonType STARNETHolonType { get; set; }
        string STARNETHolonTypeName { get; set; }
        string STARNETHolonUIName { get; set; }

        event STARNETManagerBase<T1, T2, T3, T4>.DownloadStatusChanged OnDownloadStatusChanged;
        event STARNETManagerBase<T1, T2, T3, T4>.InstallStatusChanged OnInstallStatusChanged;
        event STARNETManagerBase<T1, T2, T3, T4>.PublishStatusChanged OnPublishStatusChanged;
        event STARNETManagerBase<T1, T2, T3, T4>.UploadStatusChanged OnUploadStatusChanged;

        OASISResult<T1> Activate(Guid avatarId, Guid id, int version, ProviderType providerType = ProviderType.Default);
        OASISResult<T1> Activate(Guid avatarId, T1 holon, ProviderType providerType = ProviderType.Default);
        OASISResult<T1> Activate(Guid avatarId, T4 STARNETDNA, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<T1>> ActivateAsync(Guid avatarId, Guid id, int version, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<T1>> ActivateAsync(Guid avatarId, T1 holon, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<T1>> ActivateAsync(Guid avatarId, T4 STARNETDNA, ProviderType providerType = ProviderType.Default);
        OASISResult<T1> AddLibrary(Guid avatarId, Guid parentId, int parentVersionSequence, Guid templateId, int libraryVersionSequence, ProviderType providerType = ProviderType.Default);
        OASISResult<T1> AddLibrary(Guid avatarId, Guid parentId, int parentVersionSequence, InstalledLibrary installedLibrary, ProviderType providerType = ProviderType.Default);
        OASISResult<T1> AddLibrary(Guid avatarId, Guid parentId, string parentVersion, Guid templateId, string libraryVersion, ProviderType providerType = ProviderType.Default);
        OASISResult<T1> AddLibrary(Guid avatarId, Guid parentId, string parentVersion, InstalledLibrary installedLibrary, ProviderType providerType = ProviderType.Default);
        OASISResult<T1> AddLibrary(Guid avatarId, T1 parent, InstalledLibrary installedLibrary, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<T1>> AddLibraryAsync(Guid avatarId, Guid parentId, int parentVersionSequence, Guid templateId, int libraryVersionSequence, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<T1>> AddLibraryAsync(Guid avatarId, Guid parentId, int parentVersionSequence, InstalledLibrary installedLibrary, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<T1>> AddLibraryAsync(Guid avatarId, Guid parentId, string parentVersion, Guid templateId, string libraryVersion, ProviderType providerType = ProviderType.Default);
        //Task<OASISResult<T1>> AddLibraryAsync(Guid avatarId, Guid parentId, string parentVersion, IInstalledLibrary installedLibrary, ProviderType providerType = ProviderType.Default);
        //Task<OASISResult<T1>> AddLibraryAsync(Guid avatarId, T1 parent, IInstalledLibrary installedLibrary, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<T1>> AddLibraryAsync(Guid avatarId, Guid parentId, string parentVersion, InstalledLibrary installedLibrary, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<T1>> AddLibraryAsync(Guid avatarId, T1 parent, InstalledLibrary installedLibrary, ProviderType providerType = ProviderType.Default);

        OASISResult<T1> AddOAPPTemplate(Guid avatarId, Guid parentId, int parentVersionSequence, Guid templateId, int subTemplateVersionSequence, ProviderType providerType = ProviderType.Default);
        OASISResult<T1> AddOAPPTemplate(Guid avatarId, Guid parentId, int parentVersionSequence, IInstalledOAPPTemplate installedOAPPTemplate, ProviderType providerType = ProviderType.Default);
        OASISResult<T1> AddOAPPTemplate(Guid avatarId, Guid parentId, string parentVersion, Guid templateId, string subTemplateVersion, ProviderType providerType = ProviderType.Default);
        OASISResult<T1> AddOAPPTemplate(Guid avatarId, Guid parentId, string parentVersion, IInstalledOAPPTemplate installedOAPPTemplate, ProviderType providerType = ProviderType.Default);
        OASISResult<T1> AddOAPPTemplate(Guid avatarId, T1 parent, IInstalledOAPPTemplate installedOAPPTemplate, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<T1>> AddOAPPTemplateAsync(Guid avatarId, Guid parentId, int parentVersionSequence, Guid templateId, int subTemplateVersionSequence, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<T1>> AddOAPPTemplateAsync(Guid avatarId, Guid parentId, int parentVersionSequence, IInstalledOAPPTemplate installedOAPPTemplate, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<T1>> AddOAPPTemplateAsync(Guid avatarId, Guid parentId, string parentVersion, Guid templateId, string subTemplateVersion, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<T1>> AddOAPPTemplateAsync(Guid avatarId, Guid parentId, string parentVersion, IInstalledOAPPTemplate installedOAPPTemplate, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<T1>> AddOAPPTemplateAsync(Guid avatarId, T1 parent, IInstalledOAPPTemplate installedOAPPTemplate, ProviderType providerType = ProviderType.Default);
        OASISResult<T1> AddRuntime(Guid avatarId, Guid parentId, int parentVersionSequence, Guid templateId, int libraryVersionSequence, ProviderType providerType = ProviderType.Default);
        OASISResult<T1> AddRuntime(Guid avatarId, Guid parentId, int parentVersionSequence, IInstalledRuntime installedRuntime, ProviderType providerType = ProviderType.Default);
        OASISResult<T1> AddRuntime(Guid avatarId, Guid parentId, string parentVersion, Guid templateId, string libraryVersion, ProviderType providerType = ProviderType.Default);
        OASISResult<T1> AddRuntime(Guid avatarId, Guid parentId, string parentVersion, IInstalledRuntime installedRuntime, ProviderType providerType = ProviderType.Default);
        OASISResult<T1> AddRuntime(Guid avatarId, T1 parent, IInstalledRuntime installedRuntime, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<T1>> AddRuntimeAsync(Guid avatarId, Guid parentId, int parentVersionSequence, Guid templateId, int libraryVersionSequence, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<T1>> AddRuntimeAsync(Guid avatarId, Guid parentId, int parentVersionSequence, IInstalledRuntime installedRuntime, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<T1>> AddRuntimeAsync(Guid avatarId, Guid parentId, string parentVersion, Guid templateId, string libraryVersion, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<T1>> AddRuntimeAsync(Guid avatarId, Guid parentId, string parentVersion, IInstalledRuntime installedRuntime, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<T1>> AddRuntimeAsync(Guid avatarId, T1 parent, IInstalledRuntime installedRuntime, ProviderType providerType = ProviderType.Default);
        OASISResult<T1> BeginPublish(Guid avatarId, string fullPathToSource, string fullPathToPublishTo, string launchTarget, bool edit, ProviderType providerType);
        Task<OASISResult<T1>> BeginPublishAsync(Guid avatarId, string fullPathToSource, string fullPathToPublishTo, string launchTarget, bool edit, ProviderType providerType);
        OASISResult<T1> Create(Guid avatarId, string name, string description, object holonSubType, string fullPathToSourceFolder, Dictionary<string, object> metaData = null, T1 newHolon = default, T4 STARNETDNA = default, bool checkIfSourcePathExists = true, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<T1>> CreateAsync(Guid avatarId, string name, string description, object holonSubType, string fullPathToSourceFolder, Dictionary<string, object> metaData = null, T1 newHolon = default, T4 STARNETDNA = default, bool checkIfSourcePathExists = true, ProviderType providerType = ProviderType.Default);
        OASISResult<T1> Deactivate(Guid avatarId, Guid STARNETHolonId, int version, ProviderType providerType = ProviderType.Default);
        OASISResult<T1> Deactivate(Guid avatarId, T1 holon, ProviderType providerType = ProviderType.Default);
        OASISResult<T1> Deactivate(Guid avatarId, T4 STARNETDNA, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<T1>> DeactivateAsync(Guid avatarId, Guid STARNETHolonId, int version, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<T1>> DeactivateAsync(Guid avatarId, T1 holon, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<T1>> DeactivateAsync(Guid avatarId, T4 STARNETDNA, ProviderType providerType = ProviderType.Default);
        OASISResult<T1> Delete(Guid avatarId, Guid id, int version, bool softDelete = true, bool deleteDownload = true, bool deleteInstall = true, ProviderType providerType = ProviderType.Default);
        OASISResult<T1> Delete(Guid avatarId, ISTARNETHolon oappSystemHolon, int version, bool softDelete = true, bool deleteDownload = true, bool deleteInstall = true, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<T1>> DeleteAsync(Guid avatarId, Guid id, int version, bool softDelete = true, bool deleteDownload = true, bool deleteInstall = true, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<T1>> DeleteAsync(Guid avatarId, ISTARNETHolon oappSystemHolon, int version, bool softDelete = true, bool deleteDownload = true, bool deleteInstall = true, ProviderType providerType = ProviderType.Default);
        OASISResult<T2> Download(Guid avatarId, Guid STARNETHolonId, int version, string fullDownloadPath = "", bool reInstall = false, ProviderType providerType = ProviderType.Default);
        OASISResult<T2> Download(Guid avatarId, Guid STARNETHolonId, string version, string fullDownloadPath = "", bool reInstall = false, ProviderType providerType = ProviderType.Default);
        OASISResult<T2> Download(Guid avatarId, string STARNETHolonName, int version, string fullDownloadPath = "", bool reInstall = false, ProviderType providerType = ProviderType.Default);
        OASISResult<T2> Download(Guid avatarId, string STARNETHolonName, string version, string fullDownloadPath = "", bool reInstall = false, ProviderType providerType = ProviderType.Default);
        OASISResult<T2> Download(Guid avatarId, T1 holon, string fullDownloadPath, bool reInstall = false, ProviderType providerType = ProviderType.Default);
        OASISResult<T3> DownloadAndInstall(Guid avatarId, Guid STARNETHolonId, int version, string fullInstallPath, string fullDownloadPath = "", bool createSTARNETHolonDirectory = true, bool reInstall = false, ProviderType providerType = ProviderType.Default);
        OASISResult<T3> DownloadAndInstall(Guid avatarId, Guid STARNETHolonId, string version, string fullInstallPath, string fullDownloadPath = "", bool createSTARNETHolonDirectory = true, bool reInstall = false, ProviderType providerType = ProviderType.Default);
        OASISResult<T3> DownloadAndInstall(Guid avatarId, string STARNETHolonName, int version, string fullInstallPath, string fullDownloadPath = "", bool createSTARNETHolonDirectory = true, bool reInstall = false, ProviderType providerType = ProviderType.Default);
        OASISResult<T3> DownloadAndInstall(Guid avatarId, string STARNETHolonName, string version, string fullInstallPath, string fullDownloadPath = "", bool createSTARNETHolonDirectory = true, bool reInstall = false, ProviderType providerType = ProviderType.Default);
        OASISResult<T3> DownloadAndInstall(Guid avatarId, T1 holon, string fullInstallPath, string fullDownloadPath = "", bool createSTARNETHolonDirectory = true, bool reInstall = false, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<T3>> DownloadAndInstallAsync(Guid avatarId, Guid STARNETHolonId, int version, string fullInstallPath, string fullDownloadPath = "", bool createSTARNETHolonDirectory = true, bool reInstall = false, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<T3>> DownloadAndInstallAsync(Guid avatarId, Guid STARNETHolonId, string version, string fullInstallPath, string fullDownloadPath = "", bool createSTARNETHolonDirectory = true, bool reInstall = false, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<T3>> DownloadAndInstallAsync(Guid avatarId, string STARNETHolonName, int version, string fullInstallPath, string fullDownloadPath = "", bool createSTARNETHolonDirectory = true, bool reInstall = false, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<T3>> DownloadAndInstallAsync(Guid avatarId, string STARNETHolonName, string version, string fullInstallPath, string fullDownloadPath = "", bool createSTARNETHolonDirectory = true, bool reInstall = false, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<T3>> DownloadAndInstallAsync(Guid avatarId, T1 holon, string fullInstallPath, string fullDownloadPath = "", bool createSTARNETHolonDirectory = true, bool reInstall = false, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<T2>> DownloadAsync(Guid avatarId, Guid STARNETHolonId, int version, string fullDownloadPath = "", bool reInstall = false, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<T2>> DownloadAsync(Guid avatarId, Guid STARNETHolonId, string version, string fullDownloadPath = "", bool reInstall = false, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<T2>> DownloadAsync(Guid avatarId, string STARNETHolonName, int version, string fullDownloadPath = "", bool reInstall = false, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<T2>> DownloadAsync(Guid avatarId, string STARNETHolonName, string version, string fullDownloadPath = "", bool reInstall = false, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<T2>> DownloadAsync(Guid avatarId, T1 holon, string fullDownloadPath, bool reInstall = false, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<T1>> EditAsync(Guid avatarId, T1 holon, T4 newSTARNETDNA, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<T1>> EditAsync(Guid id, T4 newSTARNETDNA, Guid avatarId, ProviderType providerType = ProviderType.Default);
        OASISResult<T1> FininalizePublish(Guid avatarId, T1 holon, bool edit, ProviderType providerType);
        Task<OASISResult<T1>> FininalizePublishAsync(Guid avatarId, T1 holon, bool edit, ProviderType providerType);
        OASISResult<bool> GenerateCompressedFile(string sourcePath, string destinationPath);
        OASISResult<T3> Install(Guid avatarId, string fullPathToPublishedOrDownloadedSTARNETHolonFile, string fullInstallPath, bool createSTARNETHolonDirectory = true, IDownloadedSTARNETHolon downloadedSTARNETHolon = null, bool reInstall = false, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<T3>> InstallAsync(Guid avatarId, string fullPathToPublishedOrDownloadedSTARNETHolonFile, string fullInstallPath, bool createSTARNETHolonDirectory = true, IDownloadedSTARNETHolon downloadedSTARNETHolon = null, bool reInstall = false, ProviderType providerType = ProviderType.Default);
        OASISResult<bool> IsInstalled(Guid avatarId, Guid STARNETHolonId, int versionSequence, ProviderType providerType = ProviderType.Default);
        OASISResult<bool> IsInstalled(Guid avatarId, Guid STARNETHolonId, string version, ProviderType providerType = ProviderType.Default);
        OASISResult<bool> IsInstalled(Guid avatarId, string name, int versionSequence, ProviderType providerType = ProviderType.Default);
        OASISResult<bool> IsInstalled(Guid avatarId, string name, string version, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<bool>> IsInstalledAsync(Guid avatarId, Guid STARNETHolonId, int versionSequence, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<bool>> IsInstalledAsync(Guid avatarId, Guid STARNETHolonId, string version, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<bool>> IsInstalledAsync(Guid avatarId, string name, int versionSequence, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<bool>> IsInstalledAsync(Guid avatarId, string name, string version, ProviderType providerType = ProviderType.Default);
        OASISResult<bool> IsPublished(Guid avatarId, Guid STARNETHolonId, int versionSequence, ProviderType providerType = ProviderType.Default);
        OASISResult<bool> IsPublished(Guid avatarId, Guid STARNETHolonId, string version, ProviderType providerType = ProviderType.Default);
        OASISResult<bool> IsPublished(Guid avatarId, string name, int versionSequence, ProviderType providerType = ProviderType.Default);
        OASISResult<bool> IsPublished(Guid avatarId, string name, string version, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<bool>> IsPublishedAsync(Guid avatarId, Guid STARNETHolonId, int versionSequence, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<bool>> IsPublishedAsync(Guid avatarId, Guid STARNETHolonId, string version, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<bool>> IsPublishedAsync(Guid avatarId, string name, int versionSequence, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<bool>> IsPublishedAsync(Guid avatarId, string name, string version, ProviderType providerType = ProviderType.Default);
        OASISResult<IEnumerable<T1>> ListDeactivated(Guid avatarId, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<IEnumerable<T1>>> ListDeactivatedAsync(Guid avatarId, ProviderType providerType = ProviderType.Default);
        OASISResult<IEnumerable<T3>> ListInstalled(Guid avatarId, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<IEnumerable<T3>>> ListInstalledAsync(Guid avatarId, ProviderType providerType = ProviderType.Default);
        OASISResult<IEnumerable<T3>> ListUninstalled(Guid avatarId, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<IEnumerable<T3>>> ListUninstalledAsync(Guid avatarId, ProviderType providerType = ProviderType.Default);
        OASISResult<IEnumerable<T1>> ListUnpublished(Guid avatarId, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<IEnumerable<T1>>> ListUnpublishedAsync(Guid avatarId, ProviderType providerType = ProviderType.Default);
        OASISResult<T1> Load(Guid avatarId, Guid id, int version = 0, ProviderType providerType = ProviderType.Default);
        OASISResult<IEnumerable<T1>> LoadAll(Guid avatarId, object holonSubType, bool loadAllTypes = true, bool showAllVersions = false, int version = 0, HolonType STARNETHolonType = HolonType.Default, string STARNETHolonTypeName = "Default", ProviderType providerType = ProviderType.Default);
        Task<OASISResult<IEnumerable<T1>>> LoadAllAsync(Guid avatarId, object holonSubType, bool loadAllTypes = true, bool showAllVersions = false, int version = 0, HolonType STARNETHolonType = HolonType.Default, string STARNETHolonTypeName = "Default", ProviderType providerType = ProviderType.Default);
        OASISResult<IEnumerable<T1>> LoadAllForAvatar(Guid avatarId, bool showAllVersions = false, int version = 0, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<IEnumerable<T1>>> LoadAllForAvatarAsync(Guid avatarId, bool showAllVersions = false, int version = 0, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<T1>> LoadAsync(Guid avatarId, Guid id, int version = 0, ProviderType providerType = ProviderType.Default);
        OASISResult<T1> LoadForHolon(Guid avatarId, Guid holonId, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<T1>> LoadForHolonAsync(Guid avatarId, Guid holonId, ProviderType providerType = ProviderType.Default);
        OASISResult<T1> LoadForPublishedFile(Guid avatarId, string publishedFilePath, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<T1>> LoadForPublishedFileAsync(Guid avatarId, string publishedFilePath, ProviderType providerType = ProviderType.Default);
        OASISResult<T1> LoadForSourceOrInstalledFolder(Guid avatarId, string sourceOrInstallPath, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<T1>> LoadForSourceOrInstalledFolderAsync(Guid avatarId, string sourceOrInstallPath, ProviderType providerType = ProviderType.Default);
        OASISResult<T3> LoadInstalled(Guid avatarId, Guid STARNETHolonId, bool active, int versionSequence, ProviderType providerType = ProviderType.Default);
        OASISResult<T3> LoadInstalled(Guid avatarId, Guid STARNETHolonId, int versionSequence, ProviderType providerType = ProviderType.Default);
        OASISResult<T3> LoadInstalled(Guid avatarId, Guid STARNETHolonId, string version, ProviderType providerType = ProviderType.Default);
        OASISResult<T3> LoadInstalled(Guid avatarId, Guid STARNETHolonId, string version, bool active, ProviderType providerType = ProviderType.Default);
        OASISResult<T3> LoadInstalled(Guid avatarId, string name, bool active, int versionSequence, ProviderType providerType = ProviderType.Default);
        OASISResult<T3> LoadInstalled(Guid avatarId, string name, int versionSequence, ProviderType providerType = ProviderType.Default);
        OASISResult<T3> LoadInstalled(Guid avatarId, string name, string version, ProviderType providerType = ProviderType.Default);
        OASISResult<T3> LoadInstalled(Guid avatarId, string STARNETHolonName, string version, bool active, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<T3>> LoadInstalledAsync(Guid avatarId, Guid STARNETHolonId, bool active, int versionSequence, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<T3>> LoadInstalledAsync(Guid avatarId, Guid STARNETHolonId, int versionSequence, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<T3>> LoadInstalledAsync(Guid avatarId, Guid STARNETHolonId, string version, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<T3>> LoadInstalledAsync(Guid avatarId, Guid STARNETHolonId, string version, bool active, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<T3>> LoadInstalledAsync(Guid avatarId, string name, bool active, int versionSequence, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<T3>> LoadInstalledAsync(Guid avatarId, string name, int versionSequence, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<T3>> LoadInstalledAsync(Guid avatarId, string name, string version, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<T3>> LoadInstalledAsync(Guid avatarId, string STARNETHolonName, string version, bool active, ProviderType providerType = ProviderType.Default);
        OASISResult<T1> LoadVersion(Guid id, string version, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<T1>> LoadVersionAsync(Guid id, string version, ProviderType providerType = ProviderType.Default);
        OASISResult<IEnumerable<T1>> LoadVersions(Guid id, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<IEnumerable<T1>>> LoadVersionsAsync(Guid id, ProviderType providerType = ProviderType.Default);
        OASISResult<T3> OpenSTARNETHolonFolder(Guid avatarId, Guid STARNETHolonId, int versionSequence = 0, ProviderType providerType = ProviderType.Default);
        OASISResult<T3> OpenSTARNETHolonFolder(Guid avatarId, Guid STARNETHolonId, string version, ProviderType providerType = ProviderType.Default);
        OASISResult<T3> OpenSTARNETHolonFolder(Guid avatarId, T3 holon);
        Task<OASISResult<T3>> OpenSTARNETHolonFolderAsync(Guid avatarId, Guid STARNETHolonId, int versionSequence = 0, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<T3>> OpenSTARNETHolonFolderAsync(Guid avatarId, Guid STARNETHolonId, string version, ProviderType providerType = ProviderType.Default);
        OASISResult<T1> Publish(Guid avatarId, string fullPathToSource, string launchTarget, string fullPathToPublishTo = "", bool edit = false, bool registerOnSTARNET = true, bool generateBinary = true, bool uploadToCloud = true, ProviderType providerType = ProviderType.Default, ProviderType binaryProviderType = ProviderType.IPFSOASIS, bool embedRuntimes = false, bool embedLibs = false, bool embedTemplates = false);
        Task<OASISResult<T1>> PublishAsync(Guid avatarId, string fullPathToSource, string launchTarget, string fullPathToPublishTo = "", bool edit = false, bool registerOnSTARNET = true, bool generateBinary = true, bool uploadToCloud = true, ProviderType providerType = ProviderType.Default, ProviderType binaryProviderType = ProviderType.IPFSOASIS, bool embedRuntimes = false, bool embedLibs = false, bool embedTemplates = false);
        OASISResult<T> ReadDNAFromPublishedFile<T>(string fullPathToPublishedFile);
        Task<OASISResult<T>> ReadDNAFromPublishedFileAsync<T>(string fullPathToPublishedFile);
        OASISResult<T> ReadDNAFromSourceOrInstallFolder<T>(string fullPathToSTARNETHolonFolder);
        Task<OASISResult<T>> ReadDNAFromSourceOrInstallFolderAsync<T>(string fullPathToSTARNETHolonFolder);
        OASISResult<T1> RemoveLibrary(Guid avatarId, Guid parentId, int parentVersionSequence, Guid templateId, int libraryVersionSequnce, ProviderType providerType = ProviderType.Default);
        OASISResult<T1> RemoveLibrary(Guid avatarId, Guid parentId, string parentVersion, Guid templateId, string libraryVersion, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<T1>> RemoveLibraryAsync(Guid avatarId, Guid parentId, int parentVersionSequence, Guid templateId, int libraryVersionSequence, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<T1>> RemoveLibraryAsync(Guid avatarId, Guid parentId, string parentVersion, Guid templateId, string libraryVersion, ProviderType providerType = ProviderType.Default);
        OASISResult<T1> RemoveOAPPTemplate(Guid avatarId, Guid parentId, int parentVersionSequence, Guid templateId, int subTemplateVersionSequnce, ProviderType providerType = ProviderType.Default);
        OASISResult<T1> RemoveOAPPTemplate(Guid avatarId, Guid parentId, string parentVersion, Guid templateId, string subTemplateVersion, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<T1>> RemoveOAPPTemplateAsync(Guid avatarId, Guid parentId, int parentVersionSequence, Guid templateId, int subTemplateVersionSequence, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<T1>> RemoveOAPPTemplateAsync(Guid avatarId, Guid parentId, string parentVersion, Guid templateId, string subTemplateVersion, ProviderType providerType = ProviderType.Default);
        OASISResult<T1> RemoveRuntime(Guid avatarId, Guid parentId, int parentVersionSequence, Guid templateId, int libraryVersionSequnce, ProviderType providerType = ProviderType.Default);
        OASISResult<T1> RemoveRuntime(Guid avatarId, Guid parentId, string parentVersion, Guid templateId, string libraryVersion, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<T1>> RemoveRuntimeAsync(Guid avatarId, Guid parentId, int parentVersionSequence, Guid templateId, int libraryVersionSequence, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<T1>> RemoveRuntimeAsync(Guid avatarId, Guid parentId, string parentVersion, Guid templateId, string libraryVersion, ProviderType providerType = ProviderType.Default);
        OASISResult<T1> Republish(Guid avatarId, Guid STARNETHolonId, int version, ProviderType providerType = ProviderType.Default);
        OASISResult<T1> Republish(Guid avatarId, T1 holon, ProviderType providerType = ProviderType.Default);
        OASISResult<T1> Republish(Guid avatarId, T4 STARNETDNA, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<T1>> RepublishAsync(Guid avatarId, Guid STARNETHolonId, int version, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<T1>> RepublishAsync(Guid avatarId, T1 holon, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<T1>> RepublishAsync(Guid avatarId, T4 STARNETDNA, ProviderType providerType = ProviderType.Default);
        OASISResult<IEnumerable<T1>> Search(Guid avatarId, string searchTerm, bool searchOnlyForCurrentAvatar = true, bool showAllVersions = false, int version = 0, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<IEnumerable<T>>> SearchAsync<T>(Guid avatarId, string searchTerm, bool searchOnlyForCurrentAvatar = true, bool showAllVersions = false, int version = 0, ProviderType providerType = ProviderType.Default) where T : ISTARNETHolon, new();
        OASISResult<T3> Uninstall(Guid avatarId, Guid STARNETHolonId, int versionSequence, ProviderType providerType = ProviderType.Default);
        OASISResult<T3> Uninstall(Guid avatarId, Guid STARNETHolonId, string version, ProviderType providerType = ProviderType.Default);
        OASISResult<T3> Uninstall(Guid avatarId, string STARNETHolonName, int versionSequence, ProviderType providerType = ProviderType.Default);
        OASISResult<T3> Uninstall(Guid avatarId, string STARNETHolonName, string version, ProviderType providerType = ProviderType.Default);
        OASISResult<T3> Uninstall(Guid avatarId, T3 installedSTARNETHolon, string errorMessage, ProviderType providerType);
        Task<OASISResult<T3>> UninstallAsync(Guid avatarId, Guid STARNETHolonId, int versionSequence, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<T3>> UninstallAsync(Guid avatarId, Guid STARNETHolonId, string version, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<T3>> UninstallAsync(Guid avatarId, string STARNETHolonName, int versionSequence, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<T3>> UninstallAsync(Guid avatarId, string STARNETHolonName, string version, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<T3>> UninstallAsync(Guid avatarId, T3 installedSTARNETHolon, string errorMessage, ProviderType providerType);
        OASISResult<T1> Unpublish(Guid avatarId, Guid STARNETHolonId, int version, ProviderType providerType = ProviderType.Default);
        OASISResult<T1> Unpublish(Guid avatarId, T1 holon, ProviderType providerType = ProviderType.Default);
        OASISResult<T1> Unpublish(Guid avatarId, T4 STARNETDNA, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<T1>> UnpublishAsync(Guid avatarId, Guid STARNETHolonId, int version, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<T1>> UnpublishAsync(Guid avatarId, T1 holon, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<T1>> UnpublishAsync(Guid avatarId, T4 STARNETDNA, ProviderType providerType = ProviderType.Default);
        OASISResult<T1> Update(Guid avatarId, T1 holon, bool updateDNAJSONFile = false, ProviderType providerType = ProviderType.Default);
        OASISResult<T3> Update(Guid avatarId, T3 holon, bool updateDNAJSONFile = false, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<T1>> UpdateAsync(Guid avatarId, T1 holon, bool updateDNAJSONFile = false, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<T3>> UpdateAsync(Guid avatarId, T3 holon, bool updateDNAJSONFile = false, ProviderType providerType = ProviderType.Default);
        OASISResult<T2> UpdateDownloadCounts(Guid avatarId, T2 downloadedSTARNETHolon, T4 STARNETDNA, OASISResult<T2> result, string errorMessage, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<T2>> UpdateDownloadCountsAsync(Guid avatarId, T2 downloadedSTARNETHolon, T4 STARNETDNA, OASISResult<T2> result, string errorMessage, ProviderType providerType = ProviderType.Default);
        OASISResult<T3> UpdateInstallCounts(Guid avatarId, T3 installedSTARNETHolon, T4 STARNETDNA, OASISResult<T3> result, string errorMessage, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<T3>> UpdateInstallCountsAsync(Guid avatarId, T3 installedSTARNETHolon, T4 STARNETDNA, OASISResult<T3> result, string errorMessage, ProviderType providerType = ProviderType.Default);
        OASISResult<T1> UpdateNumberOfVersionCounts(Guid avatarId, OASISResult<T1> result, string errorMessage, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<T1>> UpdateNumberOfVersionCountsAsync(Guid avatarId, OASISResult<T1> result, string errorMessage, ProviderType providerType = ProviderType.Default);
        OASISResult<bool> UploadToCloud(T4 STARNETDNA, string publishedSTARNETHolonFileName, bool registerOnSTARNET, ProviderType binaryProviderType);
        Task<OASISResult<bool>> UploadToCloudAsync(T4 STARNETDNA, string publishedSTARNETHolonFileName, bool registerOnSTARNET, ProviderType binaryProviderType);
        OASISResult<T1> UploadToOASIS(Guid avatarId, T4 STARNETDNA, string publishedPath, bool registerOnSTARNET, bool uploadToCloud, ProviderType binaryProviderType);
        Task<OASISResult<T1>> UploadToOASISAsync(Guid avatarId, T4 STARNETDNA, string publishedPath, bool registerOnSTARNET, bool uploadToCloud, ProviderType binaryProviderType);
        OASISResult<bool> ValidateVersion(string dnaVersion, string storedVersion, string fullPathToSTARNETHolonFolder, bool firstPublish, bool edit);
        OASISResult<bool> WriteDNA<T>(T STARNETDNA, string fullPathToSTARNETHolon);
        Task<OASISResult<bool>> WriteDNAAsync<T>(T STARNETDNA, string fullPathToSTARNETHolon);
    }
}