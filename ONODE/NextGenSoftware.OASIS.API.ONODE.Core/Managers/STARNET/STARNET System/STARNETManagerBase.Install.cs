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
    public abstract partial class STARNETManagerBase<T1, T2, T3, T4> where T1 : ISTARNETHolon, new()
        where T2 : IDownloadedSTARNETHolon, new()
        where T3 : IInstalledSTARNETHolon, new()
        where T4 : ISTARNETDNA, new()
    {
        public virtual async Task<OASISResult<T3>> DownloadAndInstallAsync(Guid avatarId, T1 holon, string fullInstallPath, string fullDownloadPath = "", bool createSTARNETHolonDirectory = true, bool reInstall = false, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T3> result = new OASISResult<T3>();
            string errorMessage = "Error occured in STARNETManagerBase.DownloadAndInstallAsync. Reason: ";
            bool isFullDownloadPathTemp = false;

            try
            {
                if (string.IsNullOrEmpty(fullDownloadPath))
                {
                    string tempPath = Path.GetTempPath();
                    fullDownloadPath = Path.Combine(tempPath, string.Concat(holon.Name, ".", STARNETHolonFileExtention));
                    isFullDownloadPathTemp = true;
                }

                if (File.Exists(fullDownloadPath))
                    File.Delete(fullDownloadPath);

                if (holon.PublishedSTARNETHolon != null)
                {
                    await File.WriteAllBytesAsync(fullDownloadPath, holon.PublishedSTARNETHolon);
                    result = await InstallAsync(avatarId, fullDownloadPath, fullInstallPath, createSTARNETHolonDirectory, null, reInstall, providerType);
                }
                else
                {
                    OASISResult<T2> downloadResult = await DownloadAsync(avatarId, holon, fullDownloadPath, reInstall, providerType);

                    if (!fullDownloadPath.Contains(string.Concat(".", STARNETHolonFileExtention)))
                        fullDownloadPath = Path.Combine(fullDownloadPath, string.Concat(holon.Name, "_v", holon.STARNETDNA.Version, ".", STARNETHolonFileExtention));

                    if (downloadResult != null && downloadResult.Result != null && !downloadResult.IsError)
                        result = await InstallAsync(avatarId, fullDownloadPath, fullInstallPath, createSTARNETHolonDirectory, downloadResult.Result, reInstall, providerType);
                    else
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured downloading the {STARNETHolonUIName} with the DownloadSTARNETHolonAsync method, reason: {downloadResult.Message}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} {ex}");
            }
            finally
            {
                if (isFullDownloadPathTemp && Directory.Exists(fullDownloadPath))
                    Directory.Delete(fullDownloadPath);
            }

            if (result.IsError)
                OnInstallStatusChanged?.Invoke(this, new STARNETHolonInstallStatusEventArgs() { STARNETDNA = holon.STARNETDNA, Status = STARNETHolonInstallStatus.Error, ErrorMessage = result.Message });

            return result;
        }

        //copied.
        public OASISResult<T3> DownloadAndInstall(Guid avatarId, T1 holon, string fullInstallPath, string fullDownloadPath = "", bool createSTARNETHolonDirectory = true, bool reInstall = false, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T3> result = new OASISResult<T3>();
            string errorMessage = "Error occured in STARNETManagerBase.DownloadAndInstall. Reason: ";
            bool isFullDownloadPathTemp = false;

            try
            {
                if (string.IsNullOrEmpty(fullDownloadPath))
                {
                    string tempPath = Path.GetTempPath();
                    fullDownloadPath = Path.Combine(tempPath, string.Concat(holon.Name, ".", STARNETHolonFileExtention));
                    isFullDownloadPathTemp = true;
                }

                if (File.Exists(fullDownloadPath))
                    File.Delete(fullDownloadPath);

                if (holon.PublishedSTARNETHolon != null)
                {
                    File.WriteAllBytes(fullDownloadPath, holon.PublishedSTARNETHolon);
                    result = Install(avatarId, fullDownloadPath, fullInstallPath, createSTARNETHolonDirectory, null, reInstall, providerType);
                }
                else
                {
                    OASISResult<T2> downloadResult = Download(avatarId, holon, fullDownloadPath, reInstall, providerType);

                    if (!fullDownloadPath.Contains(string.Concat(".", STARNETHolonFileExtention)))
                        fullDownloadPath = Path.Combine(fullDownloadPath, string.Concat(holon.Name, "_v", holon.STARNETDNA.Version, holon.Name, ".", STARNETHolonFileExtention));

                    if (downloadResult != null && downloadResult.Result != null && !downloadResult.IsError)
                        result = Install(avatarId, fullDownloadPath, fullInstallPath, createSTARNETHolonDirectory, downloadResult.Result, reInstall, providerType);
                    else
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured downloading the {STARNETHolonUIName} with the DownloadSTARNETHolonAsync method, reason: {downloadResult.Message}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} {ex}");
            }
            finally
            {
                if (isFullDownloadPathTemp && Directory.Exists(fullDownloadPath))
                    Directory.Delete(fullDownloadPath);
            }

            if (result.IsError)
                OnInstallStatusChanged?.Invoke(this, new STARNETHolonInstallStatusEventArgs() { STARNETDNA = holon.STARNETDNA, Status = STARNETHolonInstallStatus.Error, ErrorMessage = result.Message });

            return result;
        }

        public virtual async Task<OASISResult<T3>> DownloadAndInstallAsync(Guid avatarId, Guid STARNETHolonId, int version, string fullInstallPath, string fullDownloadPath = "", bool createSTARNETHolonDirectory = true, bool reInstall = false, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T3> result = new OASISResult<T3>();
            OASISResult<T1> STARNETHolonResult = await Data.LoadHolonByMetaDataAsync<T1>(new Dictionary<string, string>()
            {
                { STARNETHolonIdName, STARNETHolonId.ToString() },
                { "VersionSequence", version.ToString() } ,
                { "Active", "1" } //TODO: Not sure if we need this?
            }, metaKeyValuePairMatchMode: MetaKeyValuePairMatchMode.All, providerType: providerType);

            if (STARNETHolonResult != null && !STARNETHolonResult.IsError && STARNETHolonResult.Result != null)
                result = await DownloadAndInstallAsync(avatarId, STARNETHolonResult.Result, fullInstallPath, fullDownloadPath, createSTARNETHolonDirectory, reInstall, providerType);
            else
            {
                OASISErrorHandling.HandleError(ref result, $"Error occured in STARNETManagerBase.DownloadAndInstallAsync loading the {STARNETHolonUIName} with the LoadAsync method, reason: {OASISErrorHandling.ProcessMessage(result, $"No result found for id {STARNETHolonId.ToString()} and version {version}.")}");
                OnInstallStatusChanged?.Invoke(this, new STARNETHolonInstallStatusEventArgs() { Status = STARNETHolonInstallStatus.Error, ErrorMessage = result.Message });
            }

            return result;
        }

        //copied.
        public OASISResult<T3> DownloadAndInstall(Guid avatarId, Guid STARNETHolonId, int version, string fullInstallPath, string fullDownloadPath = "", bool createSTARNETHolonDirectory = true, bool reInstall = false, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T3> result = new OASISResult<T3>();
            OASISResult<T1> STARNETHolonResult = Data.LoadHolonByMetaData<T1>(new Dictionary<string, string>()
            {
                { STARNETHolonIdName, STARNETHolonId.ToString() },
                { "VersionSequence", version.ToString() } ,
                { "Active", "1" } //TODO: Not sure if we need this?
            }, metaKeyValuePairMatchMode: MetaKeyValuePairMatchMode.All, providerType: providerType);


            if (STARNETHolonResult != null && !STARNETHolonResult.IsError && STARNETHolonResult.Result != null)
                result = DownloadAndInstall(avatarId, STARNETHolonResult.Result, fullInstallPath, fullDownloadPath, createSTARNETHolonDirectory, reInstall, providerType);
            else
            {
                OASISErrorHandling.HandleError(ref result, $"Error occured in STARNETManagerBase.DownloadAndInstall loading the {STARNETHolonUIName} with the LoadAsync method, reason: {OASISErrorHandling.ProcessMessage(result, $"No result found for id {STARNETHolonId.ToString()} and version {version}.")}");
                OnInstallStatusChanged?.Invoke(this, new STARNETHolonInstallStatusEventArgs() { Status = STARNETHolonInstallStatus.Error, ErrorMessage = result.Message });
            }

            return result;
        }

        public virtual async Task<OASISResult<T3>> DownloadAndInstallAsync(Guid avatarId, Guid STARNETHolonId, string version, string fullInstallPath, string fullDownloadPath = "", bool createSTARNETHolonDirectory = true, bool reInstall = false, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T3> result = new OASISResult<T3>();
            OASISResult<T1> STARNETHolonResult = await Data.LoadHolonByMetaDataAsync<T1>(new Dictionary<string, string>()
            {
                { STARNETHolonIdName, STARNETHolonId.ToString() },
                { "Version", version } ,
                { "Active", "1" } //TODO: Not sure if we need this?
            }, metaKeyValuePairMatchMode: MetaKeyValuePairMatchMode.All, providerType: providerType);

            if (STARNETHolonResult != null && !STARNETHolonResult.IsError && STARNETHolonResult.Result != null)
                result = await DownloadAndInstallAsync(avatarId, STARNETHolonResult.Result, fullInstallPath, fullDownloadPath, createSTARNETHolonDirectory, reInstall, providerType);
            else
            {
                OASISErrorHandling.HandleError(ref result, $"Error occured in STARNETManagerBase.DownloadAndInstallAsync loading the {STARNETHolonUIName} with the LoadHolonByMetaDataAsync method, reason: {OASISErrorHandling.ProcessMessage(result, $"No result found for id {STARNETHolonId.ToString()} and version {version}")}");
                OnInstallStatusChanged?.Invoke(this, new STARNETHolonInstallStatusEventArgs() { Status = STARNETHolonInstallStatus.Error, ErrorMessage = result.Message });
            }

            return result;
        }

        public virtual OASISResult<T3> DownloadAndInstall(Guid avatarId, Guid STARNETHolonId, string version, string fullInstallPath, string fullDownloadPath = "", bool createSTARNETHolonDirectory = true, bool reInstall = false, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T3> result = new OASISResult<T3>();
            OASISResult<T1> STARNETHolonResult = Data.LoadHolonByMetaData<T1>(new Dictionary<string, string>()
            {
                { STARNETHolonIdName, STARNETHolonId.ToString() },
                { "Version", version } ,
                { "Active", "1" } //TODO: Not sure if we need this?
            }, metaKeyValuePairMatchMode: MetaKeyValuePairMatchMode.All, providerType: providerType);

            if (STARNETHolonResult != null && !STARNETHolonResult.IsError && STARNETHolonResult.Result != null)
                result = DownloadAndInstall(avatarId, STARNETHolonResult.Result, fullInstallPath, fullDownloadPath, createSTARNETHolonDirectory, reInstall, providerType);
            else
            {
                OASISErrorHandling.HandleError(ref result, $"Error occured in STARNETManagerBase.DownloadAndInstall loading the {STARNETHolonUIName} with the LoadHolonByMetaData method, reason: {OASISErrorHandling.ProcessMessage(result, $"No result found for id {STARNETHolonId.ToString()} and version {version}")}");
                OnInstallStatusChanged?.Invoke(this, new STARNETHolonInstallStatusEventArgs() { Status = STARNETHolonInstallStatus.Error, ErrorMessage = result.Message });
            }

            return result;
        }

        public virtual async Task<OASISResult<T3>> DownloadAndInstallAsync(Guid avatarId, string STARNETHolonName, int version, string fullInstallPath, string fullDownloadPath = "", bool createSTARNETHolonDirectory = true, bool reInstall = false, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T3> result = new OASISResult<T3>();
            OASISResult<T1> STARNETHolonResult = await Data.LoadHolonByMetaDataAsync<T1>(new Dictionary<string, string>()
            {
                { STARNETHolonNameName, STARNETHolonName },
                { "VersionSequence", version.ToString() } ,
                { "Active", "1" } //TODO: Not sure if we need this?
            }, metaKeyValuePairMatchMode: MetaKeyValuePairMatchMode.All, providerType: providerType);

            if (STARNETHolonResult != null && !STARNETHolonResult.IsError && STARNETHolonResult.Result != null)
                result = await DownloadAndInstallAsync(avatarId, STARNETHolonResult.Result, fullInstallPath, fullDownloadPath, createSTARNETHolonDirectory, reInstall, providerType);
            else
            {
                OASISErrorHandling.HandleError(ref result, $"Error occured in STARNETManagerBase.DownloadAndInstallAsync loading the {STARNETHolonUIName} with the LoadHolonByMetaDataAsync method, reason: {OASISErrorHandling.ProcessMessage(result, $"No result found for name {STARNETHolonName} and version {version}")}");
                OnInstallStatusChanged?.Invoke(this, new STARNETHolonInstallStatusEventArgs() { Status = STARNETHolonInstallStatus.Error, ErrorMessage = result.Message });
            }

            return result;
        }

        //copied.
        public OASISResult<T3> DownloadAndInstall(Guid avatarId, string STARNETHolonName, int version, string fullInstallPath, string fullDownloadPath = "", bool createSTARNETHolonDirectory = true, bool reInstall = false, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T3> result = new OASISResult<T3>();
            OASISResult<T1> STARNETHolonResult = Data.LoadHolonByMetaData<T1>(new Dictionary<string, string>()
            {
                { STARNETHolonNameName, STARNETHolonName },
                { "VersionSequence", version.ToString() } ,
                { "Active", "1" } //TODO: Not sure if we need this?
            }, metaKeyValuePairMatchMode: MetaKeyValuePairMatchMode.All, providerType: providerType);

            if (STARNETHolonResult != null && !STARNETHolonResult.IsError && STARNETHolonResult.Result != null)
                result = DownloadAndInstall(avatarId, STARNETHolonResult.Result, fullInstallPath, fullDownloadPath, createSTARNETHolonDirectory, reInstall, providerType);
            else
            {
                OASISErrorHandling.HandleError(ref result, $"Error occured in STARNETManagerBase.DownloadAndInstall loading the {STARNETHolonUIName} with the LoadHolonByMetaData method, reason: {OASISErrorHandling.ProcessMessage(result, $"No result found for name {STARNETHolonName} and version {version}")}");
                OnInstallStatusChanged?.Invoke(this, new STARNETHolonInstallStatusEventArgs() { Status = STARNETHolonInstallStatus.Error, ErrorMessage = result.Message });
            }

            return result;
        }

        public virtual async Task<OASISResult<T3>> DownloadAndInstallAsync(Guid avatarId, string STARNETHolonName, string version, string fullInstallPath, string fullDownloadPath = "", bool createSTARNETHolonDirectory = true, bool reInstall = false, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T3> result = new OASISResult<T3>();
            OASISResult<T1> STARNETHolonResult = await Data.LoadHolonByMetaDataAsync<T1>(new Dictionary<string, string>()
            {
                { STARNETHolonNameName, STARNETHolonName },
                { "Version", version } ,
                { "Active", "1" } //TODO: Not sure if we need this?
            }, metaKeyValuePairMatchMode: MetaKeyValuePairMatchMode.All, providerType: providerType);

            if (STARNETHolonResult != null && !STARNETHolonResult.IsError && STARNETHolonResult.Result != null)
                result = await DownloadAndInstallAsync(avatarId, STARNETHolonResult.Result, fullInstallPath, fullDownloadPath, createSTARNETHolonDirectory, reInstall, providerType);
            else
            {
                OASISErrorHandling.HandleError(ref result, $"Error occured in STARNETManagerBase.DownloadAndInstallAsync loading the {STARNETHolonUIName} with the LoadHolonByMetaDataAsync method, reason: {OASISErrorHandling.ProcessMessage(result, $"No result found for name {STARNETHolonName} and version {version}")}");
                OnInstallStatusChanged?.Invoke(this, new STARNETHolonInstallStatusEventArgs() { Status = STARNETHolonInstallStatus.Error, ErrorMessage = result.Message });
            }

            return result;
        }

        public virtual OASISResult<T3> DownloadAndInstall(Guid avatarId, string STARNETHolonName, string version, string fullInstallPath, string fullDownloadPath = "", bool createSTARNETHolonDirectory = true, bool reInstall = false, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T3> result = new OASISResult<T3>();
            OASISResult<T1> STARNETHolonResult = Data.LoadHolonByMetaData<T1>(new Dictionary<string, string>()
            {
                { STARNETHolonNameName, STARNETHolonName },
                { "Version", version } ,
                { "Active", "1" } //TODO: Not sure if we need this?
            }, metaKeyValuePairMatchMode: MetaKeyValuePairMatchMode.All, providerType: providerType);

            if (STARNETHolonResult != null && !STARNETHolonResult.IsError && STARNETHolonResult.Result != null)
                result = DownloadAndInstall(avatarId, STARNETHolonResult.Result, fullInstallPath, fullDownloadPath, createSTARNETHolonDirectory, reInstall, providerType);
            else
            {
                OASISErrorHandling.HandleError(ref result, $"Error occured in STARNETManagerBase.DownloadAndInstall loading the {STARNETHolonUIName} with the LoadHolonByMetaData method, reason: {OASISErrorHandling.ProcessMessage(result, $"No result found for name {STARNETHolonName} and version {version}")}");
                OnInstallStatusChanged?.Invoke(this, new STARNETHolonInstallStatusEventArgs() { Status = STARNETHolonInstallStatus.Error, ErrorMessage = result.Message });
            }

            return result;
        }

        public virtual async Task<OASISResult<T3>> InstallAsync(Guid avatarId, string fullPathToPublishedOrDownloadedSTARNETHolonFile, string fullInstallPath, bool createSTARNETHolonDirectory = true, IDownloadedSTARNETHolon downloadedSTARNETHolon = null, bool reInstall = false, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T3> result = new OASISResult<T3>();
            string errorMessage = "Error occured in STARNETManagerBase.InstallAsync. Reason: ";
            T4 STARNETDNA = default;
            T1 STARNETHolon = default;
            string tempPath = "";
            T3 installedSTARNETHolon = default;
            int totalInstalls = 0;

            try
            {
                tempPath = Path.GetTempPath();
                tempPath = Path.Combine(tempPath, $"{STARNETHolonUIName}");

                if (Directory.Exists(tempPath))
                    Directory.Delete(tempPath, true);

                //Unzip
                OnInstallStatusChanged?.Invoke(this, new STARNETHolonInstallStatusEventArgs() { Status = STARNETHolonInstallStatus.Decompressing });
                ZipFile.ExtractToDirectory(fullPathToPublishedOrDownloadedSTARNETHolonFile, tempPath, Encoding.Default, true);
                OASISResult<T4> STARNETDNAResult = await ReadDNAFromSourceOrInstallFolderAsync<T4>(tempPath);

                if (STARNETDNAResult != null && STARNETDNAResult.Result != null && !STARNETDNAResult.IsError)
                {
                    //Load the T from the OASIS to make sure the STARNETDNA is valid (and has not been tampered with).

                    //TODO: Check if this works ok? What if they tamper with the VersionSequence in the DNA file?!
                    OASISResult<T1> STARNETHolonLoadResult = await LoadAsync(avatarId, STARNETDNAResult.Result.Id, STARNETDNAResult.Result.VersionSequence, providerType: providerType);

                    if (STARNETHolonLoadResult != null && STARNETHolonLoadResult.Result != null && !STARNETHolonLoadResult.IsError)
                    {
                        //TODO: Not sure if we want to add a check here to compare the STARNETDNA in the T dir with the one stored in the OASIS?
                        STARNETDNA = (T4)STARNETHolonLoadResult.Result.STARNETDNA;
                        STARNETHolon = STARNETHolonLoadResult.Result;

                        if (!Directory.Exists(fullInstallPath))
                            Directory.CreateDirectory(fullInstallPath);

                        if (createSTARNETHolonDirectory)
                            fullInstallPath = Path.Combine(fullInstallPath, string.Concat(STARNETDNAResult.Result.Name, "_v", STARNETDNAResult.Result.Version));

                        if (Directory.Exists(fullInstallPath))
                            Directory.Delete(fullInstallPath, true);

                        OnInstallStatusChanged?.Invoke(this, new STARNETHolonInstallStatusEventArgs() { STARNETDNA = STARNETDNAResult.Result, Status = STARNETHolonInstallStatus.Installing });

                        //if (!Directory.Exists(fullInstallPath))
                        //    Directory.CreateDirectory(fullInstallPath);

                        Directory.Move(tempPath, fullInstallPath);

                        OASISResult<IAvatar> avatarResult = await AvatarManager.Instance.LoadAvatarAsync(avatarId, false, true, providerType);

                        if (avatarResult != null && !avatarResult.IsError && avatarResult.Result != null)
                        {
                            if (downloadedSTARNETHolon == null)
                            {
                                //OASISResult<DownloadedSTARNETHolon> downloadedSTARNETHolonResult = await Data.LoadHolonsByMetaDataAsync<DownloadedSTARNETHolon>("STARNETHolonId", STARNETDNAResult.Result.Id.ToString(), false, false, 0, true, 0, false, HolonType.All, providerType);
                                OASISResult<IEnumerable<T2>> downloadedSTARNETHolonResult = await Data.LoadHolonsByMetaDataAsync<T2>(STARNETHolonIdName, STARNETDNAResult.Result.Id.ToString(), HolonType.All, true, true, 0, true, false, 0, HolonType.All, 0, providerType);

                                if (downloadedSTARNETHolonResult != null && !downloadedSTARNETHolonResult.IsError && downloadedSTARNETHolonResult.Result != null)
                                    downloadedSTARNETHolon = downloadedSTARNETHolonResult.Result.FirstOrDefault();
                                else
                                    OASISErrorHandling.HandleWarning(ref result, $"The {STARNETHolonUIName} was installed but the DownloadedSTARNETHolon could not be found. Reason: {downloadedSTARNETHolonResult.Message}");
                            }

                            if (!reInstall)
                            {
                                //If it's a re-install then it doesnt count as an install so we dont need to update the counts.
                                STARNETDNA.Installs++;

                                installedSTARNETHolon = new T3()
                                {
                                    //ParentHolonId = STARNETHolonLoadResult.Result.Id,
                                    ParentSTARNETHolonId = STARNETDNA.Id,
                                    Name = string.Concat(STARNETDNA.Name, " Installed Holon"),
                                    Description = string.Concat(STARNETDNA.Description, " Installed Holon"),
                                    //STARNETHolonId = STARNETDNAResult.Result.STARNETHolonId,
                                    STARNETDNA = STARNETDNA,
                                    InstalledBy = avatarId,
                                    InstalledByAvatarUsername = avatarResult.Result.Username,
                                    InstalledOn = DateTime.Now,
                                    InstalledPath = fullInstallPath,
                                    //DownloadedSTARNETHolon = downloadedSTARNETHolon,
                                    DownloadedBy = downloadedSTARNETHolon.DownloadedBy,
                                    DownloadedByAvatarUsername = downloadedSTARNETHolon.DownloadedByAvatarUsername,
                                    DownloadedOn = downloadedSTARNETHolon.DownloadedOn,
                                    DownloadedPath = downloadedSTARNETHolon.DownloadedPath,
                                    DownloadedSTARNETHolonId = downloadedSTARNETHolon.Id,
                                    Active = "1",
                                    MetaData = STARNETHolon.MetaData
                                    //STARNETHolonVersion = STARNETDNA.Version
                                };

                                installedSTARNETHolon.MetaData["Version"] = STARNETDNA.Version;
                                installedSTARNETHolon.MetaData["VersionSequence"] = STARNETDNA.VersionSequence;
                                installedSTARNETHolon.MetaData[STARNETHolonIdName] = STARNETDNA.Id;

                                await UpdateInstallCountsAsync(avatarId, installedSTARNETHolon, STARNETDNA, result, errorMessage, providerType);
                            }
                            else
                            {
                                OASISResult<T3> installedSTARNETHolonResult = await LoadInstalledAsync(avatarId, STARNETDNAResult.Result.Id, STARNETDNAResult.Result.Version, false, providerType);

                                if (installedSTARNETHolonResult != null && installedSTARNETHolonResult.Result != null && !installedSTARNETHolonResult.IsError)
                                {
                                    installedSTARNETHolon = installedSTARNETHolonResult.Result;
                                    installedSTARNETHolon.Active = "1";
                                    installedSTARNETHolon.UninstalledBy = Guid.Empty;
                                    installedSTARNETHolon.UninstalledByAvatarUsername = "";
                                    installedSTARNETHolon.UninstalledOn = DateTime.MinValue;
                                    installedSTARNETHolon.InstalledBy = avatarId;
                                    installedSTARNETHolon.InstalledByAvatarUsername = avatarResult.Result.Username;
                                    installedSTARNETHolon.InstalledOn = DateTime.Now;
                                    installedSTARNETHolon.InstalledPath = fullInstallPath;
                                    installedSTARNETHolon.DownloadedBy = downloadedSTARNETHolon.DownloadedBy;
                                    installedSTARNETHolon.DownloadedByAvatarUsername = downloadedSTARNETHolon.DownloadedByAvatarUsername;
                                    installedSTARNETHolon.DownloadedOn = downloadedSTARNETHolon.DownloadedOn;
                                    installedSTARNETHolon.DownloadedPath = downloadedSTARNETHolon.DownloadedPath;
                                    installedSTARNETHolon.MetaData = STARNETHolon.MetaData;
                                }
                                else
                                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured re-installing {STARNETHolonUIName} calling LoadAsync. Reason: {installedSTARNETHolonResult.Message}");
                            }

                            if (!result.IsError)
                            {
                                OASISResult<T3> saveResult = await UpdateAsync(avatarId, installedSTARNETHolon, providerType: providerType);

                                if (saveResult != null && saveResult.Result != null && !saveResult.IsError)
                                {
                                    STARNETHolonLoadResult.Result.STARNETDNA = STARNETDNA;
                                    OASISResult<T1> oappSaveResult = await UpdateAsync(avatarId, STARNETHolonLoadResult.Result, providerType: providerType);

                                    if (oappSaveResult != null && !oappSaveResult.IsError && oappSaveResult.Result != null)
                                    {
                                        CheckForVersionMismatches(STARNETDNAResult.Result, ref result);

                                        OnInstallStatusChanged?.Invoke(this, new STARNETHolonInstallStatusEventArgs() { STARNETDNA = STARNETDNAResult.Result, Status = STARNETHolonInstallStatus.InstallingDependencies });
                                        result = await InstallDependenciesAsync(avatarId, STARNETHolon, fullInstallPath, errorMessage, result, providerType);

                                        if (result.InnerMessages.Count > 0)
                                            result.Message = $"{STARNETHolonUIName} successfully installed but there were {result.WarningCount} warnings:\n\n {OASISResultHelper.BuildInnerMessageError(result.InnerMessages)}";
                                        else
                                            result.Message = $"{STARNETHolonUIName} Successfully Installed";

                                        result.Result = installedSTARNETHolon;
                                        OnInstallStatusChanged?.Invoke(this, new STARNETHolonInstallStatusEventArgs() { STARNETDNA = STARNETDNAResult.Result, Status = STARNETHolonInstallStatus.Installed });
                                    }
                                    else
                                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling UpdateAsync method. Reason: {oappSaveResult.Message}");
                                }
                                else
                                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling UpdateAsync method. Reason: {saveResult.Message}");
                            }
                            else
                                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadAvatarAsync method. Reason: {avatarResult.Message}");
                        }
                    }
                    else
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadAsync method. Reason: {STARNETHolonLoadResult.Message}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} {ex}");
            }
            finally
            {
                if (Directory.Exists(tempPath))
                    Directory.Delete(tempPath, true);
            }

            if (result.IsError)
                OnInstallStatusChanged?.Invoke(this, new STARNETHolonInstallStatusEventArgs() { STARNETDNA = STARNETDNA, Status = STARNETHolonInstallStatus.Error, ErrorMessage = result.Message });

            return result;
        }

        //Copied from async
        public OASISResult<T3> Install(Guid avatarId, string fullPathToPublishedOrDownloadedSTARNETHolonFile, string fullInstallPath, bool createSTARNETHolonDirectory = true, IDownloadedSTARNETHolon downloadedSTARNETHolon = null, bool reInstall = false, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T3> result = new OASISResult<T3>();
            string errorMessage = "Error occured in STARNETManagerBase.Install. Reason: ";
            T4 STARNETDNA = default;
            T1 STARNETHolon = default;
            string tempPath = "";
            T3 installedSTARNETHolon = default;
            int totalInstalls = 0;

            try
            {
                tempPath = Path.GetTempPath();
                tempPath = Path.Combine(tempPath, $"{STARNETHolonUIName}");

                if (Directory.Exists(tempPath))
                    Directory.Delete(tempPath, true);

                //Unzip
                OnInstallStatusChanged?.Invoke(this, new STARNETHolonInstallStatusEventArgs() { Status = STARNETHolonInstallStatus.Decompressing });
                ZipFile.ExtractToDirectory(fullPathToPublishedOrDownloadedSTARNETHolonFile, tempPath, Encoding.Default, true);
                OASISResult<T4> STARNETDNAResult = ReadDNAFromSourceOrInstallFolder<T4>(tempPath);

                if (STARNETDNAResult != null && STARNETDNAResult.Result != null && !STARNETDNAResult.IsError)
                {
                    //Load the T from the OASIS to make sure the STARNETDNA is valid (and has not been tampered with).

                    //TODO: Check if this works ok? What if they tamper with the VersionSequence in the DNA file?!
                    OASISResult<T1> STARNETHolonLoadResult = Load(avatarId, STARNETDNAResult.Result.Id, STARNETDNAResult.Result.VersionSequence, providerType: providerType);
                    //OASISResult<ISTARNETHolon> STARNETHolonLoadResult = await LoadSTARNETHolonAsync(STARNETDNAResult.Result.Id, false, 0, providerType);

                    if (STARNETHolonLoadResult != null && STARNETHolonLoadResult.Result != null && !STARNETHolonLoadResult.IsError)
                    {
                        //TODO: Not sure if we want to add a check here to compare the STARNETDNA in the T dir with the one stored in the OASIS?
                        STARNETDNA = (T4)STARNETHolonLoadResult.Result.STARNETDNA;
                        STARNETHolon = STARNETHolonLoadResult.Result;

                        if (createSTARNETHolonDirectory)
                            fullInstallPath = Path.Combine(fullInstallPath, string.Concat(STARNETDNAResult.Result.Name, "_v", STARNETDNAResult.Result.Version));

                        if (Directory.Exists(fullInstallPath))
                            Directory.Delete(fullInstallPath, true);

                        //Directory.CreateDirectory(fullInstallPath);
                        Directory.Move(tempPath, fullInstallPath);
                        //Directory.Delete(tempPath);

                        OnInstallStatusChanged?.Invoke(this, new STARNETHolonInstallStatusEventArgs() { STARNETDNA = STARNETDNAResult.Result, Status = STARNETHolonInstallStatus.Installing });
                        OASISResult<IAvatar> avatarResult = AvatarManager.Instance.LoadAvatar(avatarId, false, true, providerType);

                        if (avatarResult != null && !avatarResult.IsError && avatarResult.Result != null)
                        {
                            if (downloadedSTARNETHolon == null)
                            {
                                //OASISResult<DownloadedSTARNETHolon> downloadedSTARNETHolonResult = await Data.LoadHolonsByMetaDataAsync<DownloadedSTARNETHolon>("STARNETHolonId", STARNETDNAResult.Result.Id.ToString(), false, false, 0, true, 0, false, HolonType.All, providerType);
                                OASISResult<IEnumerable<T2>> downloadedSTARNETHolonResult = Data.LoadHolonsByMetaData<T2>(STARNETHolonIdName, STARNETDNAResult.Result.Id.ToString(), HolonType.All, true, true, 0, true, false, 0, HolonType.All, 0, providerType);

                                if (downloadedSTARNETHolonResult != null && !downloadedSTARNETHolonResult.IsError && downloadedSTARNETHolonResult.Result != null)
                                    downloadedSTARNETHolon = downloadedSTARNETHolonResult.Result.FirstOrDefault();
                                else
                                    OASISErrorHandling.HandleWarning(ref result, $"The {STARNETHolonUIName} was installed but the DownloadedSTARNETHolon could not be found. Reason: {downloadedSTARNETHolonResult.Message}");
                            }

                            if (!reInstall)
                            {
                                //If it's a re-install then it doesnt count as an install so we dont need to update the counts.
                                STARNETDNA.Installs++;

                                installedSTARNETHolon = new T3()
                                {
                                    //ParentHolonId = STARNETHolonLoadResult.Result.Id,
                                    ParentSTARNETHolonId = STARNETDNA.Id,
                                    Name = string.Concat(STARNETDNA.Name, " Installed Holon"),
                                    Description = string.Concat(STARNETDNA.Description, " Installed Holon"),
                                    //STARNETHolonId = STARNETDNAResult.Result.STARNETHolonId,
                                    STARNETDNA = STARNETDNA,
                                    InstalledBy = avatarId,
                                    InstalledByAvatarUsername = avatarResult.Result.Username,
                                    InstalledOn = DateTime.Now,
                                    InstalledPath = fullInstallPath,
                                    //DownloadedSTARNETHolon = downloadedSTARNETHolon,
                                    DownloadedBy = downloadedSTARNETHolon.DownloadedBy,
                                    DownloadedByAvatarUsername = downloadedSTARNETHolon.DownloadedByAvatarUsername,
                                    DownloadedOn = downloadedSTARNETHolon.DownloadedOn,
                                    DownloadedPath = downloadedSTARNETHolon.DownloadedPath,
                                    DownloadedSTARNETHolonId = downloadedSTARNETHolon.Id,
                                    Active = "1",
                                    MetaData = STARNETHolon.MetaData
                                    //STARNETHolonVersion = STARNETDNA.Version
                                };

                                installedSTARNETHolon.MetaData["Version"] = STARNETDNA.Version;
                                installedSTARNETHolon.MetaData["VersionSequence"] = STARNETDNA.VersionSequence;
                                installedSTARNETHolon.MetaData[STARNETHolonIdName] = STARNETDNA.Id;

                                UpdateInstallCounts(avatarId, installedSTARNETHolon, STARNETDNA, result, errorMessage, providerType);
                            }
                            else
                            {
                                OASISResult<T3> installedSTARNETHolonResult = LoadInstalled(avatarId, STARNETDNAResult.Result.Id, STARNETDNAResult.Result.Version, false, providerType);

                                if (installedSTARNETHolonResult != null && installedSTARNETHolonResult.Result != null && !installedSTARNETHolonResult.IsError)
                                {
                                    installedSTARNETHolon = installedSTARNETHolonResult.Result;
                                    installedSTARNETHolon.Active = "1";
                                    installedSTARNETHolon.UninstalledBy = Guid.Empty;
                                    installedSTARNETHolon.UninstalledByAvatarUsername = "";
                                    installedSTARNETHolon.UninstalledOn = DateTime.MinValue;
                                    installedSTARNETHolon.InstalledBy = avatarId;
                                    installedSTARNETHolon.InstalledByAvatarUsername = avatarResult.Result.Username;
                                    installedSTARNETHolon.InstalledOn = DateTime.Now;
                                    installedSTARNETHolon.InstalledPath = fullInstallPath;
                                    installedSTARNETHolon.DownloadedBy = downloadedSTARNETHolon.DownloadedBy;
                                    installedSTARNETHolon.DownloadedByAvatarUsername = downloadedSTARNETHolon.DownloadedByAvatarUsername;
                                    installedSTARNETHolon.DownloadedOn = downloadedSTARNETHolon.DownloadedOn;
                                    installedSTARNETHolon.DownloadedPath = downloadedSTARNETHolon.DownloadedPath;
                                    installedSTARNETHolon.MetaData = STARNETHolon.MetaData;
                                }
                                else
                                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured re-installing {STARNETHolonUIName} calling LoadAsync. Reason: {installedSTARNETHolonResult.Message}");
                            }

                            if (!result.IsError)
                            {
                                OASISResult<T3> saveResult = Update(avatarId, installedSTARNETHolon, providerType: providerType);

                                if (saveResult != null && saveResult.Result != null && !saveResult.IsError)
                                {
                                    //result.Result = installedSTARNETHolon;
                                    //result.Result.DownloadedSTARNETHolon = downloadedSTARNETHolon;
                                    STARNETHolonLoadResult.Result.STARNETDNA = STARNETDNA;

                                    OASISResult<T1> oappSaveResult = Update(avatarId, STARNETHolonLoadResult.Result, providerType: providerType);

                                    if (oappSaveResult != null && !oappSaveResult.IsError && oappSaveResult.Result != null)
                                    {
                                        CheckForVersionMismatches(STARNETDNAResult.Result, ref result);

                                        OnInstallStatusChanged?.Invoke(this, new STARNETHolonInstallStatusEventArgs() { STARNETDNA = STARNETDNAResult.Result, Status = STARNETHolonInstallStatus.InstallingDependencies });
                                        result = InstallDependencies(avatarId, STARNETHolon, fullInstallPath, errorMessage, result, providerType);

                                        if (result.InnerMessages.Count > 0)
                                            result.Message = $"{STARNETHolonUIName} successfully installed but there were {result.WarningCount} warnings:\n\n {OASISResultHelper.BuildInnerMessageError(result.InnerMessages)}";
                                        else
                                            result.Message = $"{STARNETHolonUIName} Successfully Installed";

                                        result.Result = installedSTARNETHolon;
                                        OnInstallStatusChanged?.Invoke(this, new STARNETHolonInstallStatusEventArgs() { STARNETDNA = STARNETDNAResult.Result, Status = STARNETHolonInstallStatus.Installed });
                                    }
                                    else
                                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling UpdateAsync method. Reason: {oappSaveResult.Message}");
                                }
                                else
                                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling UpdateAsync method. Reason: {saveResult.Message}");
                            }
                            else
                                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadAvatarAsync method. Reason: {avatarResult.Message}");
                        }
                    }
                    else
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadSTARNETHolonAsync method. Reason: {STARNETHolonLoadResult.Message}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} {ex}");
            }
            finally
            {
                if (Directory.Exists(tempPath))
                    Directory.Delete(tempPath, true);
            }

            if (result.IsError)
                OnInstallStatusChanged?.Invoke(this, new STARNETHolonInstallStatusEventArgs() { STARNETDNA = STARNETDNA, Status = STARNETHolonInstallStatus.Error, ErrorMessage = result.Message });

            return result;
        }

        public virtual async Task<OASISResult<T3>> UninstallAsync(Guid avatarId, T3 installedSTARNETHolon, string errorMessage, ProviderType providerType)
        {
            OASISResult<T3> result = new OASISResult<T3>();

            try
            {
                Directory.Delete(installedSTARNETHolon.InstalledPath, true);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} Error occured attempting to delete the {STARNETHolonUIName} folder ({installedSTARNETHolon.InstalledPath}) Reason: {ex.Message}");
            }

            OASISResult<IAvatar> avatarResult = await AvatarManager.Instance.LoadAvatarAsync(avatarId, false, true, providerType, 0);

            if (avatarResult != null && avatarResult.Result != null && !avatarResult.IsError)
            {
                installedSTARNETHolon.UninstalledBy = avatarId;
                installedSTARNETHolon.UninstalledOn = DateTime.Now;
                installedSTARNETHolon.UninstalledByAvatarUsername = avatarResult.Result.Username;
                installedSTARNETHolon.Active = "0";

                OASISResult<T3> saveIntalledSTARNETHolonResult = await installedSTARNETHolon.SaveAsync<T3>();

                if (saveIntalledSTARNETHolonResult != null && !saveIntalledSTARNETHolonResult.IsError && saveIntalledSTARNETHolonResult.Result != null)
                {
                    result.Message = $"{STARNETHolonUIName} Uninstalled";
                    result.Result = saveIntalledSTARNETHolonResult.Result;
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling UpdateAsync. Reason: {saveIntalledSTARNETHolonResult.Message}");
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadAvatarAsync. Reason: {avatarResult.Message}");

            return result;
        }

        //copied.
        public OASISResult<T3> Uninstall(Guid avatarId, T3 installedSTARNETHolon, string errorMessage, ProviderType providerType)
        {
            OASISResult<T3> result = new OASISResult<T3>();

            try
            {
                Directory.Delete(installedSTARNETHolon.InstalledPath, true);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} Error occured attempting to delete the {STARNETHolonUIName} folder ({installedSTARNETHolon.InstalledPath}) Reason: {ex.Message}");
            }

            //if (!result.IsError)
            //{
            OASISResult<IAvatar> avatarResult = AvatarManager.Instance.LoadAvatar(avatarId, false, true, providerType, 0);

            if (avatarResult != null && avatarResult.Result != null && !avatarResult.IsError)
            {
                installedSTARNETHolon.UninstalledBy = avatarId;
                installedSTARNETHolon.UninstalledOn = DateTime.Now;
                installedSTARNETHolon.UninstalledByAvatarUsername = avatarResult.Result.Username;
                installedSTARNETHolon.Active = "0";

                OASISResult<T3> saveIntalledSTARNETHolonResult = installedSTARNETHolon.Save<T3>();

                if (saveIntalledSTARNETHolonResult != null && !saveIntalledSTARNETHolonResult.IsError && saveIntalledSTARNETHolonResult.Result != null)
                {
                    result.Message = $"{STARNETHolonUIName} Uninstalled";
                    result.Result = saveIntalledSTARNETHolonResult.Result;
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling UpdateAsync. Reason: {saveIntalledSTARNETHolonResult.Message}");
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadAvatarAsync. Reason: {avatarResult.Message}");
            //}

            return result;
        }

        public virtual async Task<OASISResult<T3>> UninstallAsync(Guid avatarId, Guid STARNETHolonId, int versionSequence, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T3> result = new OASISResult<T3>();
            string errorMessage = "Error occured in STARNETManagerBase.UninstallAsync. Reason: ";

            OASISResult<T3> loadResult = await Data.LoadHolonByMetaDataAsync<T3>(new Dictionary<string, string>()
            {
                { STARNETHolonIdName, STARNETHolonId.ToString() },
                { "VersionSequene", versionSequence.ToString() }

            }, MetaKeyValuePairMatchMode.All, STARNETHolonInstalledHolonType, true, true, 0, true, 0, false, HolonType.All, providerType);

            if (loadResult != null && loadResult.Result != null && !loadResult.IsError)
                result = await UninstallAsync(avatarId, loadResult.Result, errorMessage, providerType);
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonByMetaDataAsync. Reason: {loadResult.Message}");

            return result;
        }

        public OASISResult<T3> Uninstall(Guid avatarId, Guid STARNETHolonId, int versionSequence, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T3> result = new OASISResult<T3>();
            string errorMessage = "Error occured in STARNETManagerBase.UninstallAsync. Reason: ";

            OASISResult<T3> loadResult = Data.LoadHolonByMetaData<T3>(new Dictionary<string, string>()
            {
                { STARNETHolonIdName, STARNETHolonId.ToString() },
                { "VersionSequene", versionSequence.ToString() }

            }, MetaKeyValuePairMatchMode.All, STARNETHolonInstalledHolonType, true, true, 0, true, false, HolonType.All);

            if (loadResult != null && loadResult.Result != null && !loadResult.IsError)
                result = Uninstall(avatarId, loadResult.Result, errorMessage, providerType);
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonByMetaData. Reason: {loadResult.Message}");

            return result;
        }

        public virtual async Task<OASISResult<T3>> UninstallAsync(Guid avatarId, Guid STARNETHolonId, string version, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T3> result = new OASISResult<T3>();
            string errorMessage = "Error occured in STARNETManagerBase.UninstallAsync. Reason: ";

            OASISResult<T3> loadResult = await Data.LoadHolonByMetaDataAsync<T3>(new Dictionary<string, string>()
            {
                { STARNETHolonIdName, STARNETHolonId.ToString() },
                { "Version", version }

            }, MetaKeyValuePairMatchMode.All, STARNETHolonInstalledHolonType, true, true, 0, true, 0, false, HolonType.All, providerType);

            if (loadResult != null && loadResult.Result != null && !loadResult.IsError)
                result = await UninstallAsync(avatarId, loadResult.Result, errorMessage, providerType);
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonByMetaDataAsync. Reason: {loadResult.Message}");

            return result;
        }

        public OASISResult<T3> Uninstall(Guid avatarId, Guid STARNETHolonId, string version, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T3> result = new OASISResult<T3>();
            string errorMessage = "Error occured in STARNETManagerBase.Uninstall. Reason: ";

            OASISResult<T3> loadResult = Data.LoadHolonByMetaData<T3>(new Dictionary<string, string>()
            {
                { STARNETHolonIdName, STARNETHolonId.ToString() },
                { "Version", version }

            }, MetaKeyValuePairMatchMode.All, STARNETHolonInstalledHolonType, true, true, 0, true, false, HolonType.All);

            if (loadResult != null && loadResult.Result != null && !loadResult.IsError)
                result = Uninstall(avatarId, loadResult.Result, errorMessage, providerType);
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonByMetaData. Reason: {loadResult.Message}");

            return result;
        }

        public virtual async Task<OASISResult<T3>> UninstallAsync(Guid avatarId, string STARNETHolonName, int versionSequence, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T3> result = new OASISResult<T3>();
            string errorMessage = "Error occured in STARNETManagerBase.UninstallAsync. Reason: ";

            OASISResult<T3> loadResult = await Data.LoadHolonByMetaDataAsync<T3>(new Dictionary<string, string>()
            {
                { STARNETHolonNameName, STARNETHolonName },
                { "VersionSequene", versionSequence.ToString() }

            }, MetaKeyValuePairMatchMode.All, STARNETHolonInstalledHolonType, true, true, 0, true, 0, false, HolonType.All, providerType);

            if (loadResult != null && loadResult.Result != null && !loadResult.IsError)
                result = await UninstallAsync(avatarId, loadResult.Result, errorMessage, providerType);
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonByMetaDataAsync. Reason: {loadResult.Message}");

            return result;
        }

        public OASISResult<T3> Uninstall(Guid avatarId, string STARNETHolonName, int versionSequence, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T3> result = new OASISResult<T3>();
            string errorMessage = "Error occured in STARNETManagerBase.Uninstall. Reason: ";

            OASISResult<T3> loadResult = Data.LoadHolonByMetaData<T3>(new Dictionary<string, string>()
            {
                { STARNETHolonNameName, STARNETHolonName},
                { "VersionSequene", versionSequence.ToString() }

            }, MetaKeyValuePairMatchMode.All, STARNETHolonInstalledHolonType, true, true, 0, true, false, HolonType.All);

            if (loadResult != null && loadResult.Result != null && !loadResult.IsError)
                result = Uninstall(avatarId, loadResult.Result, errorMessage, providerType);
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonByMetaData. Reason: {loadResult.Message}");

            return result;
        }

        public virtual async Task<OASISResult<T3>> UninstallAsync(Guid avatarId, string STARNETHolonName, string version, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T3> result = new OASISResult<T3>();
            string errorMessage = "Error occured in STARNETManagerBase.UninstallAsync. Reason: ";

            OASISResult<T3> loadResult = await Data.LoadHolonByMetaDataAsync<T3>(new Dictionary<string, string>()
            {
                { STARNETHolonNameName, STARNETHolonName },
                { "Version", version }

            }, MetaKeyValuePairMatchMode.All, STARNETHolonInstalledHolonType, true, true, 0, true, 0, false, HolonType.All, providerType);

            if (loadResult != null && loadResult.Result != null && !loadResult.IsError)
                result = await UninstallAsync(avatarId, loadResult.Result, errorMessage, providerType);
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonByMetaDataAsync. Reason: {loadResult.Message}");

            return result;
        }

        public OASISResult<T3> Uninstall(Guid avatarId, string STARNETHolonName, string version, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T3> result = new OASISResult<T3>();
            string errorMessage = "Error occured in STARNETManagerBase.Uninstall. Reason: ";

            OASISResult<T3> loadResult = Data.LoadHolonByMetaData<T3>(new Dictionary<string, string>()
            {
                { STARNETHolonNameName, STARNETHolonName},
                { "Version", version }

            }, MetaKeyValuePairMatchMode.All, STARNETHolonInstalledHolonType, true, true, 0, true, false, HolonType.All);

            if (loadResult != null && loadResult.Result != null && !loadResult.IsError)
                result = Uninstall(avatarId, loadResult.Result, errorMessage, providerType);
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonByMetaData. Reason: {loadResult.Message}");

            return result;
        }

    }
}