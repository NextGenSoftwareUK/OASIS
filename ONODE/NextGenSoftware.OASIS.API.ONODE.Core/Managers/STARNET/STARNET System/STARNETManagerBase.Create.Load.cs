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
        public virtual async Task<OASISResult<T1>> LoadAsync(Guid avatarId, Guid id, int version = 0, HolonType holonType = HolonType.Default, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = new OASISResult<T1>();

            if (holonType == HolonType.Default)
                holonType = STARNETHolonType;

            OASISResult<IEnumerable<T1>> loadResult = await Data.LoadHolonsByMetaDataAsync<T1>(STARNETHolonIdName, id.ToString(), holonType, true, true, 0, true, false, 0, HolonType.All, 0, providerType);
            OASISResult<IEnumerable<T1>> filterdResult = FilterResultsForVersion(avatarId, loadResult, false, version);

            if (filterdResult != null && filterdResult.Result != null && !filterdResult.IsError)
                result.Result = filterdResult.Result.FirstOrDefault();
            else
                OASISErrorHandling.HandleError(ref result, $"Error occured in LoadAsync<T> loading the {STARNETHolonUIName} with Id {id} from the {Enum.GetName(typeof(ProviderType), providerType)} provider. Reason: {filterdResult.Message}");

            if (result.Result == null)
                result.Message = "No Holon Found";

            return result;
        }

        public OASISResult<T1> Load(Guid avatarId, Guid id, int version = 0, HolonType holonType = HolonType.Default, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = new OASISResult<T1>();

            if (holonType == HolonType.Default)
                holonType = STARNETHolonType;

            OASISResult<IEnumerable<T1>> loadResult = Data.LoadHolonsByMetaData<T1>(STARNETHolonIdName, id.ToString(), holonType, true, true, 0, true, false, 0, HolonType.All, 0, providerType);
            OASISResult<IEnumerable<T1>> filterdResult = FilterResultsForVersion(avatarId, loadResult, false, version);

            if (filterdResult != null && filterdResult.Result != null && !filterdResult.IsError)
                result.Result = filterdResult.Result.FirstOrDefault();
            else
                OASISErrorHandling.HandleError(ref result, $"Error occured in Load<T> loading the {STARNETHolonUIName} with Id {id} from the {Enum.GetName(typeof(ProviderType), providerType)} provider. Reason: {filterdResult.Message}");

            if (result.Result == null)
                result.Message = "No Holon Found";

            return result;
        }

        public virtual async Task<OASISResult<T1>> LoadForSourceOrInstalledFolderAsync(Guid avatarId, string sourceOrInstallPath, HolonType holonType = HolonType.Default, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            OASISResult<T4> readDNAResult = await ReadDNAFromSourceOrInstallFolderAsync<T4>(sourceOrInstallPath);

            if (readDNAResult != null && readDNAResult.Result != null && !readDNAResult.IsError)
                result = await LoadAsync(avatarId, readDNAResult.Result.Id, readDNAResult.Result.VersionSequence, holonType, providerType);
            else
                OASISErrorHandling.HandleError(ref result, $"Error occured in LoadAsync<T> calling ReadDNAFromSourceOrInstallFolderAsync reading the STARNETDNA from the source path {sourceOrInstallPath} for the {STARNETHolonUIName}. Reason: {readDNAResult.Message}");

            return result;
        }

        public OASISResult<T1> LoadForSourceOrInstalledFolder(Guid avatarId, string sourceOrInstallPath, HolonType holonType = HolonType.Default, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            OASISResult<T4> readDNAResult = ReadDNAFromSourceOrInstallFolder<T4>(sourceOrInstallPath);

            if (readDNAResult != null && readDNAResult.Result != null && !readDNAResult.IsError)
                result = Load(avatarId, readDNAResult.Result.Id, readDNAResult.Result.VersionSequence, holonType, providerType);
            else
                OASISErrorHandling.HandleError(ref result, $"Error occured in Load<T> calling ReadDNAFromSourceOrInstallFolderAsync reading the STARNETDNA from the source path {sourceOrInstallPath} for the {STARNETHolonUIName}. Reason: {readDNAResult.Message}");

            return result;
        }

        public virtual async Task<OASISResult<T1>> LoadForHolonAsync(Guid avatarId, Guid holonId, ProviderType providerType = ProviderType.Default)
        {
            return await Data.LoadHolonAsync<T1>(holonId, providerType: providerType);
        }

        public OASISResult<T1> LoadForHolon(Guid avatarId, Guid holonId, ProviderType providerType = ProviderType.Default)
        {
            return Data.LoadHolon<T1>(holonId, providerType: providerType);
        }

        public virtual async Task<OASISResult<T1>> LoadForPublishedFileAsync(Guid avatarId, string publishedFilePath, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            OASISResult<T4> readDNAResult = await ReadDNAFromPublishedFileAsync<T4>(publishedFilePath);

            if (readDNAResult != null && readDNAResult.Result != null && !readDNAResult.IsError)
                result = await LoadAsync(avatarId, readDNAResult.Result.Id, readDNAResult.Result.VersionSequence, providerType: providerType);
            else
                OASISErrorHandling.HandleError(ref result, $"Error occured in LoadAsync<T> calling ReadDNAFromSourceOrInstallFolderAsync reading the STARNETDNA from the source path {publishedFilePath} for the {STARNETHolonUIName}. Reason: {readDNAResult.Message}");

            return result;
        }

        public OASISResult<T1> LoadForPublishedFile(Guid avatarId, string publishedFilePath, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            OASISResult<T4> readDNAResult = ReadDNAFromPublishedFile<T4>(publishedFilePath);

            if (readDNAResult != null && readDNAResult.Result != null && !readDNAResult.IsError)
                result = Load(avatarId, readDNAResult.Result.Id, readDNAResult.Result.VersionSequence, providerType: providerType);
            else
                OASISErrorHandling.HandleError(ref result, $"Error occured in Load<T> calling ReadDNAFromSourceOrInstallFolderAsync reading the STARNETDNA from the source path {publishedFilePath} for the {STARNETHolonUIName}. Reason: {readDNAResult.Message}");

            return result;
        }

        public virtual async Task<OASISResult<IEnumerable<T1>>> LoadAllAsync(Guid avatarId, object holonSubType, bool loadAllTypes = true, bool showAllVersions = false, int version = 0, HolonType STARNETHolonType = HolonType.Default, string STARNETHolonTypeName = "Default", ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<T1>> result = new OASISResult<IEnumerable<T1>>();
            OASISResult<IEnumerable<T1>> loadHolonsResult = null;

            if (STARNETHolonType == HolonType.Default)
                STARNETHolonType = this.STARNETHolonType;

            if (STARNETHolonTypeName == "Default")
                STARNETHolonTypeName = this.STARNETHolonTypeName;

            if (loadAllTypes)
                loadHolonsResult = await Data.LoadAllHolonsAsync<T1>(STARNETHolonType, true, true, 0, true, false, HolonType.All, 0, providerType);
            else
                loadHolonsResult = await Data.LoadHolonsByMetaDataAsync<T1>(STARNETHolonTypeName, Enum.GetName(holonSubType.GetType(), holonSubType), HolonType.All, true, true, 0, true, false, 0, HolonType.All, 0);

            return FilterResultsForVersion(avatarId, loadHolonsResult, showAllVersions, version);
        }

        public OASISResult<IEnumerable<T1>> LoadAll(Guid avatarId, object holonSubType, bool loadAllTypes = true, bool showAllVersions = false, int version = 0, HolonType STARNETHolonType = HolonType.Default, string STARNETHolonTypeName = "Default", ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<T1>> result = new OASISResult<IEnumerable<T1>>();
            OASISResult<IEnumerable<T1>> loadHolonsResult = null;

            if (STARNETHolonType == HolonType.Default)
                STARNETHolonType = this.STARNETHolonType;

            if (STARNETHolonTypeName == "Default")
                STARNETHolonTypeName = this.STARNETHolonTypeName;

            if (loadAllTypes)
                loadHolonsResult = Data.LoadAllHolons<T1>(STARNETHolonType, true, true, 0, true, false, HolonType.All, 0, providerType);
            else
                loadHolonsResult = Data.LoadHolonsByMetaData<T1>(STARNETHolonTypeName, Enum.GetName(holonSubType.GetType(), holonSubType), HolonType.All, true, true, 0, true, false, 0, HolonType.All, 0);

            return FilterResultsForVersion(avatarId, loadHolonsResult, showAllVersions, version);
        }

        public virtual async Task<OASISResult<IEnumerable<T1>>> LoadAllForAvatarAsync(Guid avatarId, bool showAllVersions = false, int version = 0, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<T1>> result = new OASISResult<IEnumerable<T1>>();
            OASISResult<IEnumerable<T1>> loadHolonsResult = await Data.LoadHolonsByMetaDataAsync<T1>(new Dictionary<string, string>()
            {
                { "CreatedByAvatarId", avatarId.ToString() },
                { "Active", "1" }

            }, MetaKeyValuePairMatchMode.All, STARNETHolonType, providerType: providerType);

            return FilterResultsForVersion(avatarId, loadHolonsResult, showAllVersions, version);
        }

        public OASISResult<IEnumerable<T1>> LoadAllForAvatar(Guid avatarId, bool showAllVersions = false, int version = 0, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<T1>> result = new OASISResult<IEnumerable<T1>>();
            OASISResult<IEnumerable<T1>> loadHolonsResult = Data.LoadHolonsByMetaData<T1>(new Dictionary<string, string>()
            {
                { "CreatedByAvatarId", avatarId.ToString() },
                { "Active", "1" }

            }, MetaKeyValuePairMatchMode.All, STARNETHolonType, providerType: providerType);

            return FilterResultsForVersion(avatarId, loadHolonsResult, showAllVersions, version);
        }

        public virtual async Task<OASISResult<IEnumerable<T>>> SearchAsync<T>(Guid avatarId, string searchTerm, Guid parentId = default, Dictionary<string, string> filterByMetaData = null, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode = MetaKeyValuePairMatchMode.All, bool searchOnlyForCurrentAvatar = true, bool showAllVersions = false, int version = 0, ProviderType providerType = ProviderType.Default) where T : ISTARNETHolon, new()
        {
            OASISResult<IEnumerable<T>> result = new OASISResult<IEnumerable<T>>();
            OASISResult<IEnumerable<T>> loadHolonsResult = await SearchHolonsAsync<T>(searchTerm, avatarId, parentId, filterByMetaData, metaKeyValuePairMatchMode, searchOnlyForCurrentAvatar, providerType, "STARNETManagerBase.SearchAsync", STARNETHolonType);
            return FilterResultsForVersion(avatarId, loadHolonsResult, showAllVersions, version);
        }

        public OASISResult<IEnumerable<T1>> Search(Guid avatarId, string searchTerm, Guid parentId = default, Dictionary<string, string> filterByMetaData = null, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode = MetaKeyValuePairMatchMode.All, bool searchOnlyForCurrentAvatar = true, bool showAllVersions = false, int version = 0, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<T1>> result = new OASISResult<IEnumerable<T1>>();
            OASISResult<IEnumerable<T1>> loadHolonsResult = SearchHolons<T1>(searchTerm, avatarId, parentId, filterByMetaData, metaKeyValuePairMatchMode, searchOnlyForCurrentAvatar, providerType, "STARNETManagerBase.Search", STARNETHolonType);
            return FilterResultsForVersion(avatarId, loadHolonsResult, showAllVersions, version);
        }

        public virtual async Task<OASISResult<T1>> DeleteAsync(Guid avatarId, Guid id, int version, bool softDelete = true, bool deleteDownload = true, bool deleteInstall = true, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            string errorMessage = "Error occured in DeleteAsync. Reason: ";
            OASISResult<T1> loadResult = await LoadAsync(avatarId, id, version, providerType: providerType);

            if (loadResult != null && loadResult.Result != null && !loadResult.IsError)
                result = await DeleteAsync(avatarId, loadResult.Result, version, softDelete, deleteDownload, deleteInstall, providerType);
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured loading the {STARNETHolonUIName} with Id {id} from the {Enum.GetName(typeof(ProviderType), providerType)} provider. Reason: {loadResult.Message}");

            return result;
        }

        public OASISResult<T1> Delete(Guid avatarId, Guid id, int version, bool softDelete = true, bool deleteDownload = true, bool deleteInstall = true, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            string errorMessage = "Error occured in Delete. Reason: ";
            OASISResult<T1> loadResult = Load(avatarId, id, version, providerType: providerType);

            if (loadResult != null && loadResult.Result != null && !loadResult.IsError)
                result = Delete(avatarId, loadResult.Result, version, softDelete, deleteDownload, deleteInstall, providerType);
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured loading the {STARNETHolonUIName} with Id {id} from the {Enum.GetName(typeof(ProviderType), providerType)} provider. Reason: {loadResult.Message}");

            return result;
        }

        public virtual async Task<OASISResult<T1>> DeleteAsync(Guid avatarId, ISTARNETHolon oappSystemHolon, int version, bool softDelete = true, bool deleteDownload = true, bool deleteInstall = true, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            string errorMessage = "Error occured in DeleteAsync. Reason: ";

            if (oappSystemHolon.STARNETDNA.CreatedByAvatarId != avatarId)
            {
                OASISErrorHandling.HandleError(ref result, $"Permission Denied. You did not create this {STARNETHolonUIName}. Error occured in DeleteSTARNETHolonAsync loading the {STARNETHolonUIName} with Id {oappSystemHolon.STARNETDNA.CreatedByAvatarId} from the {Enum.GetName(typeof(ProviderType), providerType)} provider. Reason: The {STARNETHolonUIName} was not created by the Avatar with Id {avatarId}.");
                return result;
            }

            try
            {
                if (!string.IsNullOrEmpty(oappSystemHolon.STARNETDNA.SourcePath) && Directory.Exists(oappSystemHolon.STARNETDNA.SourcePath))
                    Directory.Delete(oappSystemHolon.STARNETDNA.SourcePath, true);
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} Error occured attempting to delete the T folder {oappSystemHolon.STARNETDNA.SourcePath}. PLEASE DELETE MANUALLY! Reason: {e}");
            }

            try
            {
                if (!string.IsNullOrEmpty(oappSystemHolon.STARNETDNA.PublishedPath) && File.Exists(oappSystemHolon.STARNETDNA.PublishedPath))
                    File.Delete(oappSystemHolon.STARNETDNA.PublishedPath);
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} Error occured attempting to delete the T Published folder {oappSystemHolon.STARNETDNA.PublishedPath}. PLEASE DELETE MANUALLY! Reason: {e}");
            }

            if (deleteDownload || deleteInstall)
            {
                OASISResult<T3> installedSTARNETHolonResult = await LoadInstalledAsync(avatarId, oappSystemHolon.STARNETDNA.Id, version, providerType);

                if (installedSTARNETHolonResult != null && installedSTARNETHolonResult.Result != null && !installedSTARNETHolonResult.IsError)
                {
                    try
                    {
                        if (deleteDownload && !string.IsNullOrEmpty(installedSTARNETHolonResult.Result.DownloadedPath) && File.Exists(installedSTARNETHolonResult.Result.DownloadedPath))
                            File.Delete(installedSTARNETHolonResult.Result.DownloadedPath);
                    }
                    catch (Exception e)
                    {
                        OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} Error occured attempting to delete the {STARNETHolonUIName} Download folder {installedSTARNETHolonResult.Result.DownloadedPath}. PLEASE DELETE MANUALLY! Reason: {e}");
                    }

                    try
                    {
                        if (deleteInstall && !string.IsNullOrEmpty(installedSTARNETHolonResult.Result.InstalledPath) && Directory.Exists(installedSTARNETHolonResult.Result.InstalledPath))
                            Directory.Delete(installedSTARNETHolonResult.Result.InstalledPath, true);
                    }
                    catch (Exception e)
                    {
                        OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} Error occured attempting to delete the {STARNETHolonUIName} Installed folder {installedSTARNETHolonResult.Result.InstalledPath}. PLEASE DELETE MANUALLY! Reason: {e}");
                    }

                    if (deleteInstall)
                    {
                        OASISResult<T1> deleteInstalledSTARNETHolonHolonResult = await DeleteHolonAsync<T1>(installedSTARNETHolonResult.Result.Id, avatarId, softDelete, providerType, "STARNETManagerBase.DeleteAsync");

                        if (!(deleteInstalledSTARNETHolonHolonResult != null && deleteInstalledSTARNETHolonHolonResult.Result != null && !deleteInstalledSTARNETHolonHolonResult.IsError))
                            OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} Error occured deleting the Installed {STARNETHolonUIName} holon with id {installedSTARNETHolonResult.Result.Id} calling DeleteAsync. Reason: {deleteInstalledSTARNETHolonHolonResult.Message}");
                    }

                    if (deleteDownload)
                    {
                        OASISResult<T1> deleteDownloadedSTARNETHolonHolonResult = await DeleteHolonAsync<T1>(installedSTARNETHolonResult.Result.DownloadedSTARNETHolonId, avatarId, softDelete, providerType, "STARNETManagerBase.DeleteAsync");

                        if (!(deleteDownloadedSTARNETHolonHolonResult != null && deleteDownloadedSTARNETHolonHolonResult.Result != null && !deleteDownloadedSTARNETHolonHolonResult.IsError))
                            OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} Error occured deleting the Downloaded {STARNETHolonUIName} holon with id {installedSTARNETHolonResult.Result.DownloadedSTARNETHolonId} calling DeleteAsync. Reason: {deleteDownloadedSTARNETHolonHolonResult.Message}");
                    }
                }
            }

            OASISResult<T1> deleteHolonResult = await DeleteHolonAsync<T1>(oappSystemHolon.Id, avatarId, softDelete, providerType, "STARNETManagerBase.DeleteAsync");

            if (!(deleteHolonResult != null && deleteHolonResult.Result != null && !deleteHolonResult.IsError))
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured deleting the {STARNETHolonUIName} holon with id {oappSystemHolon.Id} calling DeleteAsync. Reason: {deleteHolonResult.Message}");

            result.Result = deleteHolonResult.Result;
            return result;
        }

        public OASISResult<T1> Delete(Guid avatarId, ISTARNETHolon oappSystemHolon, int version, bool softDelete = true, bool deleteDownload = true, bool deleteInstall = true, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            string errorMessage = "Error occured in Delete. Reason: ";

            if (oappSystemHolon.STARNETDNA.CreatedByAvatarId != avatarId)
            {
                OASISErrorHandling.HandleError(ref result, $"Permission Denied. You did not create this {STARNETHolonUIName}. Error occured in Delete loading the {STARNETHolonUIName} with Id {oappSystemHolon.STARNETDNA.CreatedByAvatarId} from the {Enum.GetName(typeof(ProviderType), providerType)} provider. Reason: The {STARNETHolonUIName} was not created by the Avatar with Id {avatarId}.");
                return result;
            }

            try
            {
                if (!string.IsNullOrEmpty(oappSystemHolon.STARNETDNA.SourcePath) && Directory.Exists(oappSystemHolon.STARNETDNA.SourcePath))
                    Directory.Delete(oappSystemHolon.STARNETDNA.SourcePath, true);
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} Error occured attempting to delete the {STARNETHolonUIName} Source folder {oappSystemHolon.STARNETDNA.SourcePath}. PLEASE DELETE MANUALLY! Reason: {e}");
            }

            try
            {
                if (!string.IsNullOrEmpty(oappSystemHolon.STARNETDNA.PublishedPath) && File.Exists(oappSystemHolon.STARNETDNA.PublishedPath))
                    File.Delete(oappSystemHolon.STARNETDNA.PublishedPath);
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} Error occured attempting to delete the {STARNETHolonUIName} Published folder {oappSystemHolon.STARNETDNA.PublishedPath}. PLEASE DELETE MANUALLY! Reason: {e}");
            }

            if (deleteDownload || deleteInstall)
            {
                OASISResult<T3> installedSTARNETHolonResult = LoadInstalled(avatarId, oappSystemHolon.STARNETDNA.Id, version, providerType);

                if (installedSTARNETHolonResult != null && installedSTARNETHolonResult.Result != null && !installedSTARNETHolonResult.IsError)
                {
                    try
                    {
                        if (deleteDownload && !string.IsNullOrEmpty(installedSTARNETHolonResult.Result.DownloadedPath) && File.Exists(installedSTARNETHolonResult.Result.DownloadedPath))
                            File.Delete(installedSTARNETHolonResult.Result.DownloadedPath);
                    }
                    catch (Exception e)
                    {
                        OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} Error occured attempting to delete the {STARNETHolonUIName} Download folder {installedSTARNETHolonResult.Result.DownloadedPath}. PLEASE DELETE MANUALLY! Reason: {e}");
                    }

                    try
                    {
                        if (deleteInstall && !string.IsNullOrEmpty(installedSTARNETHolonResult.Result.InstalledPath) && Directory.Exists(installedSTARNETHolonResult.Result.InstalledPath))
                            Directory.Delete(installedSTARNETHolonResult.Result.InstalledPath, true);
                    }
                    catch (Exception e)
                    {
                        OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} Error occured attempting to delete the {STARNETHolonUIName} Installed folder {installedSTARNETHolonResult.Result.InstalledPath}. PLEASE DELETE MANUALLY! Reason: {e}");
                    }

                    if (deleteInstall)
                    {
                        OASISResult<T1> deleteInstalledSTARNETHolonHolonResult = DeleteHolon<T1>(installedSTARNETHolonResult.Result.Id, avatarId, softDelete, providerType, "STARNETManagerBase.Delete");

                        if (!(deleteInstalledSTARNETHolonHolonResult != null && deleteInstalledSTARNETHolonHolonResult.Result != null && !deleteInstalledSTARNETHolonHolonResult.IsError))
                            OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} Error occured deleting the Installed {STARNETHolonUIName} holon with id {installedSTARNETHolonResult.Result.Id} calling DeleteHolonAsync. Reason: {deleteInstalledSTARNETHolonHolonResult.Message}");
                    }

                    if (deleteDownload)
                    {
                        OASISResult<T1> deleteDownloadedSTARNETHolonHolonResult = DeleteHolon<T1>(installedSTARNETHolonResult.Result.DownloadedSTARNETHolonId, avatarId, softDelete, providerType, "STARNETManagerBase.Delete");

                        if (!(deleteDownloadedSTARNETHolonHolonResult != null && deleteDownloadedSTARNETHolonHolonResult.Result != null && !deleteDownloadedSTARNETHolonHolonResult.IsError))
                            OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} Error occured deleting the Downloaded {STARNETHolonUIName} holon with id {installedSTARNETHolonResult.Result.DownloadedSTARNETHolonId} calling DeleteHolonAsync. Reason: {deleteDownloadedSTARNETHolonHolonResult.Message}");
                    }
                }
            }

            OASISResult<T1> deleteHolonResult = DeleteHolon<T1>(avatarId, oappSystemHolon.Id, softDelete, providerType, "STARNETManagerBase.Delete");

            if (!(deleteHolonResult != null && deleteHolonResult.Result != null && !deleteHolonResult.IsError))
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured deleting the {STARNETHolonUIName} holon with id {oappSystemHolon.Id} calling DeleteHolonAsync. Reason: {deleteHolonResult.Message}");

            result.Result = deleteHolonResult.Result;
            return result;
        }
    }
}
