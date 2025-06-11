using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.ONODE.Core.Managers.Base;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Holons;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Interfaces
{
    public interface ISTARManagerBase<T1, T2, T3>
        where T1 : ISTARHolon, new()
        where T2 : IDownloadedSTARHolon, new()
        where T3 : IInstalledSTARHolon, new()
    {
        string STARHolonDNAFileName { get; set; }
        string STARHolonDNAJSONName { get; set; }
        string STARHolonFileExtention { get; set; }
        string STARHolonGoogleBucket { get; set; }
        string STARHolonIdName { get; set; }
        string STARHolonNameName { get; set; }
        Type STARHolonSubType { get; set; }
        HolonType STARHolonType { get; set; }
        string STARHolonTypeName { get; set; }
        string STARHolonUIName { get; set; }
        HolonType STARInstalledHolonType { get; set; }

        event STARManagerBase<T1, T2, T3>.DownloadStatusChanged OnDownloadStatusChanged;
        event STARManagerBase<T1, T2, T3>.InstallStatusChanged OnInstallStatusChanged;
        event STARManagerBase<T1, T2, T3>.PublishStatusChanged OnPublishStatusChanged;
        event STARManagerBase<T1, T2, T3>.UploadStatusChanged OnUploadStatusChanged;

        OASISResult<T1> Activate(Guid avatarId, Guid id, int version, ProviderType providerType = ProviderType.Default);
        OASISResult<T1> Activate(Guid avatarId, ISTARHolonDNA STARHolonDNA, ProviderType providerType = ProviderType.Default);
        OASISResult<T1> Activate(Guid avatarId, T1 holon, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<T1>> ActivateAsync(Guid avatarId, Guid id, int version, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<T1>> ActivateAsync(Guid avatarId, ISTARHolonDNA STARHolonDNA, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<T1>> ActivateAsync(Guid avatarId, T1 holon, ProviderType providerType = ProviderType.Default);
        OASISResult<T1> BeginPublish(Guid avatarId, string fullPathToSource, string fullPathToPublishTo, string launchTarget, bool edit, ProviderType providerType);
        Task<OASISResult<T1>> BeginPublishAsync(Guid avatarId, string fullPathToSource, string fullPathToPublishTo, string launchTarget, bool edit, ProviderType providerType);
        OASISResult<T1> Create(Guid avatarId, string name, string description, object holonSubType, string fullPathToT, Dictionary<string, object> metaData = null, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<T1>> CreateAsync(Guid avatarId, string name, string description, object holonSubType, string fullPathToT, Dictionary<string, object> metaData = null, ProviderType providerType = ProviderType.Default);
        OASISResult<T1> Deactivate(Guid avatarId, Guid STARHolonId, int version, ProviderType providerType = ProviderType.Default);
        OASISResult<T1> Deactivate(Guid avatarId, ISTARHolonDNA STARHolonDNA, ProviderType providerType = ProviderType.Default);
        OASISResult<T1> Deactivate(Guid avatarId, T1 holon, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<T1>> DeactivateAsync(Guid avatarId, Guid STARHolonId, int version, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<T1>> DeactivateAsync(Guid avatarId, ISTARHolonDNA STARHolonDNA, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<T1>> DeactivateAsync(Guid avatarId, T1 holon, ProviderType providerType = ProviderType.Default);
        OASISResult<T1> Delete(Guid avatarId, Guid id, int version, bool softDelete = true, bool deleteDownload = true, bool deleteInstall = true, ProviderType providerType = ProviderType.Default);
        OASISResult<T1> Delete(Guid avatarId, ISTARHolon oappSystemHolon, int version, bool softDelete = true, bool deleteDownload = true, bool deleteInstall = true, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<T1>> DeleteAsync(Guid avatarId, Guid id, int version, bool softDelete = true, bool deleteDownload = true, bool deleteInstall = true, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<T1>> DeleteAsync(Guid avatarId, ISTARHolon oappSystemHolon, int version, bool softDelete = true, bool deleteDownload = true, bool deleteInstall = true, ProviderType providerType = ProviderType.Default);
        OASISResult<T2> Download(Guid avatarId, T1 holon, string fullDownloadPath, bool reInstall = false, ProviderType providerType = ProviderType.Default);
        OASISResult<T3> DownloadAndInstall(Guid avatarId, Guid STARHolonId, int version, string fullInstallPath, string fullDownloadPath = "", bool createSTARHolonDirectory = true, bool reInstall = false, ProviderType providerType = ProviderType.Default);
        OASISResult<T3> DownloadAndInstall(Guid avatarId, T1 holon, string fullInstallPath, string fullDownloadPath = "", bool createSTARHolonDirectory = true, bool reInstall = false, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<T3>> DownloadAndInstallAsync(Guid avatarId, Guid STARHolonId, int version, string fullInstallPath, string fullDownloadPath = "", bool createSTARHolonDirectory = true, bool reInstall = false, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<T3>> DownloadAndInstallAsync(Guid avatarId, T1 holon, string fullInstallPath, string fullDownloadPath = "", bool createSTARHolonDirectory = true, bool reInstall = false, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<T2>> DownloadAsync(Guid avatarId, T1 holon, string fullDownloadPath, bool reInstall = false, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<T1>> EditAsync(Guid id, ISTARHolonDNA newSTARHolonDNA, Guid avatarId, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<T1>> EditAsync(Guid avatarId, T1 holon, ISTARHolonDNA newSTARHolonDNA, ProviderType providerType = ProviderType.Default);
        OASISResult<T1> FininalizePublish(Guid avatarId, T1 holon, bool edit, ProviderType providerType);
        Task<OASISResult<T1>> FininalizePublishAsync(Guid avatarId, T1 holon, bool edit, ProviderType providerType);
        OASISResult<bool> GenerateCompressedFile(string sourcePath, string destinationPath);
        OASISResult<T3> Install(Guid avatarId, string fullPathToPublishedSTARHolonFile, string fullInstallPath, bool createSTARHolonDirectory = true, IDownloadedSTARHolon downloadedSTARHolon = null, bool reInstall = false, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<T3>> InstallAsync(Guid avatarId, string fullPathToPublishedSTARHolonFile, string fullInstallPath, bool createSTARHolonDirectory = true, IDownloadedSTARHolon downloadedSTARHolon = null, bool reInstall = false, ProviderType providerType = ProviderType.Default);
        OASISResult<bool> IsInstalled(Guid avatarId, Guid STARHolonId, int versionSequence, ProviderType providerType = ProviderType.Default);
        OASISResult<bool> IsInstalled(Guid avatarId, Guid STARHolonId, string version, ProviderType providerType = ProviderType.Default);
        OASISResult<bool> IsInstalled(Guid avatarId, string name, int versionSequence, ProviderType providerType = ProviderType.Default);
        OASISResult<bool> IsInstalled(Guid avatarId, string name, string version, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<bool>> IsInstalledAsync(Guid avatarId, Guid STARHolonId, int versionSequence, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<bool>> IsInstalledAsync(Guid avatarId, Guid STARHolonId, string version, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<bool>> IsInstalledAsync(Guid avatarId, string name, int versionSequence, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<bool>> IsInstalledAsync(Guid avatarId, string name, string version, ProviderType providerType = ProviderType.Default);
        OASISResult<IEnumerable<T1>> ListDeactivated(Guid avatarId, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<IEnumerable<T1>>> ListDeactivatedAsync(Guid avatarId, ProviderType providerType = ProviderType.Default);
        OASISResult<IEnumerable<T3>> ListInstalled(Guid avatarId, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<IEnumerable<T3>>> ListInstalledAsync(Guid avatarId, ProviderType providerType = ProviderType.Default);
        OASISResult<IEnumerable<T3>> ListUninstalled(Guid avatarId, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<IEnumerable<T3>>> ListUninstalledAsync(Guid avatarId, ProviderType providerType = ProviderType.Default);
        OASISResult<IEnumerable<T1>> ListUnpublished(Guid avatarId, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<IEnumerable<T1>>> ListUnpublishedAsync(Guid avatarId, ProviderType providerType = ProviderType.Default);
        OASISResult<T1> Load(Guid avatarId, Guid id, int version = 0, ProviderType providerType = ProviderType.Default);
        OASISResult<IEnumerable<T1>> LoadAll(Guid avatarId, object holonSubType, bool loadAllTypes = true, bool showAllVersions = false, int version = 0, HolonType STARHolonType = HolonType.Default, string STARHolonTypeName = "Default", ProviderType providerType = ProviderType.Default);
        Task<OASISResult<IEnumerable<T1>>> LoadAllAsync(Guid avatarId, object holonSubType, bool loadAllTypes = true, bool showAllVersions = false, int version = 0, HolonType STARHolonType = HolonType.Default, string STARHolonTypeName = "Default", ProviderType providerType = ProviderType.Default);
        OASISResult<IEnumerable<T1>> LoadAllForAvatar(Guid avatarId, bool showAllVersions = false, int version = 0, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<IEnumerable<T1>>> LoadAllForAvatarAsync(Guid avatarId, bool showAllVersions = false, int version = 0, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<T1>> LoadAsync(Guid avatarId, Guid id, int version = 0, ProviderType providerType = ProviderType.Default);
        OASISResult<T3> LoadInstalled(Guid avatarId, Guid STARHolonId, int versionSequence = 0, ProviderType providerType = ProviderType.Default);
        OASISResult<T3> LoadInstalled(Guid avatarId, Guid STARHolonId, bool active, int versionSequence = 0, ProviderType providerType = ProviderType.Default);
        OASISResult<T3> LoadInstalled(Guid avatarId, Guid STARHolonId, string version, ProviderType providerType = ProviderType.Default);
        OASISResult<T3> LoadInstalled(Guid avatarId, Guid STARHolonId, string version, bool active, ProviderType providerType = ProviderType.Default);
        OASISResult<T3> LoadInstalled(Guid avatarId, string name, int versionSequence = 0, ProviderType providerType = ProviderType.Default);
        OASISResult<T3> LoadInstalled(Guid avatarId, string name, bool active, int versionSequence = 0, ProviderType providerType = ProviderType.Default);
        OASISResult<T3> LoadInstalled(Guid avatarId, string name, string version, ProviderType providerType = ProviderType.Default);
        OASISResult<T3> LoadInstalled(Guid avatarId, string STARHolonName, string version, bool active, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<T3>> LoadInstalledAsync(Guid avatarId, Guid STARHolonId, int versionSequence = 0, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<T3>> LoadInstalledAsync(Guid avatarId, Guid STARHolonId, bool active, int versionSequence = 0, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<T3>> LoadInstalledAsync(Guid avatarId, Guid STARHolonId, string version, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<T3>> LoadInstalledAsync(Guid avatarId, Guid STARHolonId, string version, bool active, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<T3>> LoadInstalledAsync(Guid avatarId, string name, int versionSequence = 0, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<T3>> LoadInstalledAsync(Guid avatarId, string name, bool active, int versionSequence = 0, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<T3>> LoadInstalledAsync(Guid avatarId, string name, string version, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<T3>> LoadInstalledAsync(Guid avatarId, string STARHolonName, string version, bool active, ProviderType providerType = ProviderType.Default);
        OASISResult<T1> LoadVersion(Guid id, string version, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<T1>> LoadVersionAsync(Guid id, string version, ProviderType providerType = ProviderType.Default);
        OASISResult<IEnumerable<T1>> LoadVersions(Guid id, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<IEnumerable<T1>>> LoadVersionsAsync(Guid id, ProviderType providerType = ProviderType.Default);
        OASISResult<T3> OpenSTARHolonFolder(Guid avatarId, Guid STARHolonId, int versionSequence = 0, ProviderType providerType = ProviderType.Default);
        OASISResult<T3> OpenSTARHolonFolder(Guid avatarId, Guid STARHolonId, string version, ProviderType providerType = ProviderType.Default);
        OASISResult<T3> OpenSTARHolonFolder(Guid avatarId, T3 holon);
        Task<OASISResult<T3>> OpenSTARHolonFolderAsync(Guid avatarId, Guid STARHolonId, int versionSequence = 0, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<T3>> OpenSTARHolonFolderAsync(Guid avatarId, Guid STARHolonId, string version, ProviderType providerType = ProviderType.Default);
        OASISResult<T1> Publish(Guid avatarId, string fullPathToSource, string launchTarget, string fullPathToPublishTo = "", bool edit = false, bool registerOnSTARNET = true, bool generateBinary = true, bool uploadToCloud = false, ProviderType providerType = ProviderType.Default, ProviderType binaryProviderType = ProviderType.IPFSOASIS);
        Task<OASISResult<T1>> PublishAsync(Guid avatarId, string fullPathToSource, string launchTarget, string fullPathToPublishTo = "", bool edit = false, bool registerOnSTARNET = true, bool generateBinary = true, bool uploadToCloud = false, ProviderType providerType = ProviderType.Default, ProviderType binaryProviderType = ProviderType.IPFSOASIS);
        OASISResult<T> ReadDNAFromPublishedFile<T>(string fullPathToPublishedFile);
        Task<OASISResult<T>> ReadDNAFromPublishedFileAsync<T>(string fullPathToPublishedFile);
        OASISResult<T> ReadDNAFromSourceOrInstallFolder<T>(string fullPathToSTARHolonFolder);
        Task<OASISResult<T>> ReadDNAFromSourceOrInstallFolderAsync<T>(string fullPathToSTARHolonFolder);
        OASISResult<T1> Republish(Guid avatarId, Guid STARHolonId, int version, ProviderType providerType = ProviderType.Default);
        OASISResult<T1> Republish(Guid avatarId, ISTARHolonDNA STARHolonDNA, ProviderType providerType = ProviderType.Default);
        OASISResult<T1> Republish(Guid avatarId, T1 holon, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<T1>> RepublishAsync(Guid avatarId, Guid STARHolonId, int version, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<T1>> RepublishAsync(Guid avatarId, ISTARHolonDNA STARHolonDNA, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<T1>> RepublishAsync(Guid avatarId, T1 holon, ProviderType providerType = ProviderType.Default);
        OASISResult<T1> Save(Guid avatarId, T1 holon, ProviderType providerType = ProviderType.Default);
        OASISResult<T3> Save(Guid avatarId, T3 holon, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<T1>> SaveAsync(Guid avatarId, T1 holon, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<T3>> SaveAsync(Guid avatarId, T3 holon, ProviderType providerType = ProviderType.Default);
        OASISResult<IEnumerable<T1>> Search(Guid avatarId, string searchTerm, bool searchOnlyForCurrentAvatar = true, bool showAllVersions = false, int version = 0, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<IEnumerable<T1>>> SearchAsync(Guid avatarId, string searchTerm, bool searchOnlyForCurrentAvatar = true, bool showAllVersions = false, int version = 0, ProviderType providerType = ProviderType.Default);
        OASISResult<T3> Uninstall(Guid avatarId, Guid STARHolonId, int versionSequence, ProviderType providerType = ProviderType.Default);
        OASISResult<T3> Uninstall(Guid avatarId, Guid STARHolonId, string version, ProviderType providerType = ProviderType.Default);
        OASISResult<T3> Uninstall(Guid avatarId, string STARHolonName, int versionSequence, ProviderType providerType = ProviderType.Default);
        OASISResult<T3> Uninstall(Guid avatarId, string STARHolonName, string version, ProviderType providerType = ProviderType.Default);
        OASISResult<T3> Uninstall(Guid avatarId, T3 installedSTARHolon, string errorMessage, ProviderType providerType);
        Task<OASISResult<T3>> UninstallAsync(Guid avatarId, Guid STARHolonId, int versionSequence, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<T3>> UninstallAsync(Guid avatarId, Guid STARHolonId, string version, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<T3>> UninstallAsync(Guid avatarId, string STARHolonName, int versionSequence, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<T3>> UninstallAsync(Guid avatarId, string STARHolonName, string version, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<T3>> UninstallAsync(Guid avatarId, T3 installedSTARHolon, string errorMessage, ProviderType providerType);
        OASISResult<T1> Unpublish(Guid avatarId, Guid STARHolonId, int version, ProviderType providerType = ProviderType.Default);
        OASISResult<T1> Unpublish(Guid avatarId, ISTARHolonDNA STARHolonDNA, ProviderType providerType = ProviderType.Default);
        OASISResult<T1> Unpublish(Guid avatarId, T1 holon, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<T1>> UnpublishAsync(Guid avatarId, Guid STARHolonId, int version, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<T1>> UnpublishAsync(Guid avatarId, ISTARHolonDNA STARHolonDNA, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<T1>> UnpublishAsync(Guid avatarId, T1 holon, ProviderType providerType = ProviderType.Default);
        OASISResult<T2> UpdateDownloadCounts(Guid avatarId, T2 downloadedSTARHolon, ISTARHolonDNA STARHolonDNA, OASISResult<T2> result, string errorMessage, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<T2>> UpdateDownloadCountsAsync(Guid avatarId, T2 downloadedSTARHolon, ISTARHolonDNA STARHolonDNA, OASISResult<T2> result, string errorMessage, ProviderType providerType = ProviderType.Default);
        OASISResult<T3> UpdateInstallCounts(Guid avatarId, T3 installedSTARHolon, ISTARHolonDNA STARHolonDNA, OASISResult<T3> result, string errorMessage, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<T3>> UpdateInstallCountsAsync(Guid avatarId, T3 installedSTARHolon, ISTARHolonDNA STARHolonDNA, OASISResult<T3> result, string errorMessage, ProviderType providerType = ProviderType.Default);
        OASISResult<T1> UpdateNumberOfVersionCounts(Guid avatarId, OASISResult<T1> result, string errorMessage, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<T1>> UpdateNumberOfVersionCountsAsync(Guid avatarId, OASISResult<T1> result, string errorMessage, ProviderType providerType = ProviderType.Default);
        OASISResult<T> UploadToCloud<T>(ISTARHolonDNA STARHolonDNA, string publishedSTARHolonFileName, bool registerOnSTARNET, ProviderType binaryProviderType);
        Task<OASISResult<T>> UploadToCloudAsync<T>(ISTARHolonDNA STARHolonDNA, string publishedSTARHolonFileName, bool registerOnSTARNET, ProviderType binaryProviderType);
        OASISResult<T1> UploadToOASIS(Guid avatarId, ISTARHolonDNA STARHolonDNA, string publishedPath, bool registerOnSTARNET, bool uploadToCloud, ProviderType binaryProviderType);
        Task<OASISResult<T1>> UploadToOASISAsync(Guid avatarId, ISTARHolonDNA STARHolonDNA, string publishedPath, bool registerOnSTARNET, bool uploadToCloud, ProviderType binaryProviderType);
        OASISResult<bool> ValidateVersion(string dnaVersion, string storedVersion, string fullPathToSTARHolonFolder, bool firstPublish, bool edit);
        OASISResult<bool> WriteDNA<T>(T STARHolonDNA, string fullPathToSTARHolon);
        Task<OASISResult<bool>> WriteDNAAsync<T>(T STARHolonDNA, string fullPathToSTARHolon);
    }
}