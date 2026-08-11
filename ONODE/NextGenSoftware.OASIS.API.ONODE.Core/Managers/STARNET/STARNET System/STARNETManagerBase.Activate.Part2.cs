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

        public OASISResult<T2> Download(Guid avatarId, T1 holon, string fullDownloadPath, bool reInstall = false, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T2> result = new OASISResult<T2>();
            string errorMessage = "Error occured in STARNETManagerBase.Download. Reason: ";
            T2 downloadedSTARNETHolon = default;

            try
            {
                if (!fullDownloadPath.Contains(string.Concat(".", STARNETHolonFileExtention)))
                    fullDownloadPath = Path.Combine(fullDownloadPath, string.Concat(holon.Name, "_v", holon.STARNETDNA.Version, ".", STARNETHolonFileExtention));

                if (File.Exists(fullDownloadPath))
                    File.Delete(fullDownloadPath);

                try
                {
                    StorageClient storage = StorageClient.Create();

                    // set minimum chunksize just to see progress updating
                    var downloadObjectOptions = new DownloadObjectOptions
                    {
                        ChunkSize = UploadObjectOptions.MinimumChunkSize,
                    };

                    var progressReporter = new Progress<Google.Apis.Download.IDownloadProgress>(OnDownloadProgress);

                    using (var fileStream = File.OpenWrite(fullDownloadPath))
                    {
                        _fileLength = fileStream.Length;

                        if (_fileLength == 0)
                            _fileLength = holon.STARNETDNA.FileSize;

                        _progress = 0;

                        string publishedSTARNETHolonFileName = string.Concat(holon.Name, "_v", holon.STARNETDNA.Version, ".", STARNETHolonFileExtention);
                        OnInstallStatusChanged?.Invoke(this, new STARNETHolonInstallStatusEventArgs() { STARNETDNA = holon.STARNETDNA, Status = STARNETHolonInstallStatus.Downloading });
                        storage.DownloadObject(STARNETHolonGoogleBucket, publishedSTARNETHolonFileName, fileStream, downloadObjectOptions, progress: progressReporter);

                        _progress = 100;
                        OnDownloadStatusChanged?.Invoke(this, new STARNETHolonDownloadProgressEventArgs() { Progress = _progress, Status = STARNETHolonDownloadStatus.Downloading });
                        CLIEngine.DisposeProgressBar(false);
                        Console.WriteLine("");
                        fileStream.Close();
                    }

                    OASISResult<IAvatar> avatarResult = AvatarManager.Instance.LoadAvatar(avatarId, false, true, providerType);

                    if (avatarResult != null && !avatarResult.IsError && avatarResult.Result != null)
                    {
                        if (!reInstall)
                        {
                            holon.STARNETDNA.Downloads++;

                            downloadedSTARNETHolon = new T2()
                            {
                                //ParentHolonId = holon.Id,
                                ParentSTARNETHolonId = holon.STARNETDNA.Id,
                                Name = string.Concat(holon.STARNETDNA.Name, " Downloaded Holon"),
                                Description = string.Concat(holon.STARNETDNA.Description, " Downloaded Holon"),
                                STARNETDNA = holon.STARNETDNA,
                                DownloadedBy = avatarId,
                                DownloadedByAvatarUsername = avatarResult.Result.Username,
                                DownloadedOn = DateTime.Now,
                                DownloadedPath = fullDownloadPath,
                                MetaData = holon.MetaData
                            };

                            UpdateDownloadCounts(avatarId, downloadedSTARNETHolon, (T4)holon.STARNETDNA, result, errorMessage, providerType);

                            downloadedSTARNETHolon.MetaData[STARNETHolonIdName] = holon.STARNETDNA.Id.ToString();
                            //downloadedSTARNETHolon.MetaData[STARNETDNAJSONName] = JsonSerializer.Serialize(downloadedSTARNETHolon.STARNETDNA);
                            downloadedSTARNETHolon.MetaData[STARNETDNAJSONName] = JsonConvert.SerializeObject(downloadedSTARNETHolon.STARNETDNA);
                            OASISResult<T2> saveResult = downloadedSTARNETHolon.Save<T2>();

                            if (!(saveResult != null && saveResult.Result != null && !saveResult.IsError))
                                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling UpdateAsync method on downloadedSTARNETHolon. Reason: {saveResult.Message}");
                        }
                        else
                        {
                            OASISResult<IEnumerable<T2>> downloadedSTARNETHolonResult = Data.LoadHolonsByMetaData<T2>(STARNETHolonIdName, holon.STARNETDNA.Id.ToString(), HolonType.All, true, true, 0, true, false, 0, HolonType.All, 0, providerType);

                            if (downloadedSTARNETHolonResult != null && !downloadedSTARNETHolonResult.IsError && downloadedSTARNETHolonResult.Result != null)
                            {
                                downloadedSTARNETHolon = downloadedSTARNETHolonResult.Result.FirstOrDefault();
                                downloadedSTARNETHolon.DownloadedOn = DateTime.Now;
                                downloadedSTARNETHolon.DownloadedBy = avatarId;
                                downloadedSTARNETHolon.DownloadedByAvatarUsername = avatarResult.Result.Username;
                                downloadedSTARNETHolon.DownloadedPath = fullDownloadPath;
                                downloadedSTARNETHolon.MetaData = holon.MetaData;

                                OASISResult<T2> saveResult = downloadedSTARNETHolon.Save<T2>();

                                if (!(saveResult != null && saveResult.Result != null && !saveResult.IsError))
                                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling UpdateAsync method on downloadedSTARNETHolon. Reason: {saveResult.Message}");
                            }
                            else
                                OASISErrorHandling.HandleWarning(ref result, $"The {STARNETHolonUIName} was downloaded but the DownloadedSTARNETHolon could not be found. Reason: {downloadedSTARNETHolonResult.Message}");
                        }
                    }
                    else
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadAvatarAsync method. Reason: {avatarResult.Message}");


                    if (!result.IsError)
                    {
                        result.Result = downloadedSTARNETHolon;
                        OASISResult<T1> oappSaveResult = Update(avatarId, holon, providerType: providerType);

                        if (oappSaveResult != null && !oappSaveResult.IsError && oappSaveResult.Result != null)
                        {
                            if (result.InnerMessages.Count > 0)
                                result.Message = $"{STARNETHolonUIName} successfully downloaded but there were {result.WarningCount} warnings:\n\n {OASISResultHelper.BuildInnerMessageError(result.InnerMessages)}";
                            else
                                result.Message = $"{STARNETHolonUIName} Successfully Downloaded";
                        }
                        else
                            OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling SaveSTARNETHolonAsync method. Reason: {oappSaveResult.Message}");
                    }
                }
                catch (Exception e)
                {
                    CLIEngine.DisposeProgressBar(false);
                    Console.WriteLine("");
                    OASISErrorHandling.HandleError(ref result, $"An error occured downloading the {STARNETHolonUIName} from cloud storage. Reason: {e}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} {ex}");
            }
            //finally
            //{
            //    if (isFullDownloadPathTemp && Directory.Exists(fullDownloadPath))
            //        Directory.Delete(fullDownloadPath);
            //}

            //if (result.IsError)
            //    OnSTARNETHolonDownloadStatusChanged?.Invoke(this, new STARNETHolonDownloadProgressEventArgs { STARNETDNA = T.STARNETDNA, Status = Enums.STARNETHolonDownloadStatus.Error, ErrorMessage = result.Message });

            return result;
        }

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
    }
}
