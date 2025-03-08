using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Text.Json;
using System.Diagnostics;
using System.IO.Compression;
using System.Threading.Tasks;
using Google.Cloud.Storage.V1;
using NextGenSoftware.Utilities;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.ONode.Core.Holons;
using NextGenSoftware.OASIS.API.ONode.Core.Interfaces.Holons;
using NextGenSoftware.OASIS.API.ONODE.Core.Events;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Objects;
using NextGenSoftware.OASIS.API.ONODE.Core.Enums.Runtime;
using NextGenSoftware.OASIS.API.ONode.Core.Interfaces;
using static NextGenSoftware.OASIS.API.ONode.Core.Managers.OAPPTemplateManager;

namespace NextGenSoftware.OASIS.API.ONode.Core.Managers
{
    public class RuntimeManager : OASISManager
    {
        private int _progress = 0;
        private long _fileLength = 0;

        public RuntimeManager(IOASISStorageProvider OASISStorageProvider, Guid avatarId, OASISDNA OASISDNA = null) : base(OASISStorageProvider, avatarId, OASISDNA)
        {

        }

        public RuntimeManager(Guid avatarId, OASISDNA OASISDNA = null) : base(avatarId, OASISDNA)
        {

        }

        public delegate void RuntimePublishStatusChanged(object sender, RuntimePublishStatusEventArgs e);
        public delegate void RuntimeInstallStatusChanged(object sender, RuntimeInstallStatusEventArgs e);
        public delegate void RuntimeUploadStatusChanged(object sender, RuntimeUploadProgressEventArgs e);
        public delegate void RuntimeDownloadStatusChanged(object sender, RuntimeDownloadProgressEventArgs e);

        /// <summary>
        /// Fired when there is a change in the Runtime publish status.
        /// </summary>
        public event RuntimePublishStatusChanged OnRuntimePublishStatusChanged;

        /// <summary>
        /// Fired when there is a change to the Runtime Install status.
        /// </summary>
        public event RuntimeInstallStatusChanged OnRuntimeInstallStatusChanged;

        /// <summary>
        /// Fired when there is a change in the Runtime upload status.
        /// </summary>
        public event RuntimeUploadStatusChanged OnRuntimeUploadStatusChanged;

        /// <summary>
        /// Fired when there is a change in the Runtime download status.
        /// </summary>
        public event RuntimeDownloadStatusChanged OnRuntimeDownloadStatusChanged;


        private async Task<OASISResult<IRuntime>> DownloadAndInstallOASISRunTime(RuntimeType runtimeType, string version, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IRuntime> result = new OASISResult<IRuntime>();
            string errorMessage = "Error occured in OAPPManager.DownloadAndInstallOASISRunTime. Reason: ";

            try
            {
                string runtimePath = Path.Combine("temp", Enum.GetName(typeof(RuntimeType), runtimeType), version, ".oruntime");

                try
                {
                    StorageClient storage = await StorageClient.CreateAsync();

                    // set minimum chunksize just to see progress updating
                    var downloadObjectOptions = new DownloadObjectOptions
                    {
                        ChunkSize = UploadObjectOptions.MinimumChunkSize,
                    };

                    var progressReporter = new Progress<Google.Apis.Download.IDownloadProgress>(OnDownloadProgress);

                    using var fileStream = File.OpenWrite(runtimePath);
                    _fileLength = fileStream.Length;
                    _progress = 0;

                    OnRuntimeInstallStatusChanged?.Invoke(this, new RuntimeInstallStatusEventArgs() {  RunTimeType = runtimeType, Version = version, Status = Enums.RuntimeInstallStatus.Downloading });
                    await storage.DownloadObjectAsync("oasis_runtimes", string.Concat(Enum.GetName(typeof(RuntimeType), runtimeType), version, ".oruntime"), fileStream, downloadObjectOptions, progress: progressReporter);
                    result = await InstallRuntimeAsync(avatarId, runtimePath, fullInstallPath, createOAPPDirectory, providerType);
                }
                catch (Exception ex)
                {
                    OASISErrorHandling.HandleError(ref result, $"An error occured downloading the OAPP from cloud storage. Reason: {ex}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} {ex}");
            }

            if (result.IsError)
                OnRuntimeInstallStatusChanged?.Invoke(this, new RuntimeInstallStatusEventArgs() { RunTimeType = runtimeType, Version = version, Status = Enums.RuntimeInstallStatus.Error, ErrorMessage = result.Message });

            return result;
        }

        public async Task<OASISResult<IOAPPTemplateDNA>> PublishOAPPTemplateAsync(string fullPathToOAPPTemplate, string launchTarget, Guid avatarId, string fullPathToPublishTo = "", bool registerOnSTARNET = true, bool generateOAPPTemplateBinary = true, bool uploadOAPPTemplateToCloud = false, ProviderType providerType = ProviderType.Default, ProviderType oappBinaryProviderType = ProviderType.IPFSOASIS)
        {
            OASISResult<IOAPPTemplateDNA> result = new OASISResult<IOAPPTemplateDNA>();
            string errorMessage = "Error occured in OAPPTemplateManager.PublishOAPPTemplateAsync. Reason: ";
            IOAPPTemplateDNA OAPPTemplateDNA = null;
            string tempPath = "";

            try
            {
                OASISResult<IOAPPTemplateDNA> readOAPPTemplateDNAResult = await ReadOAPPTemplateDNAAsync(fullPathToOAPPTemplate);

                if (readOAPPTemplateDNAResult != null && !readOAPPTemplateDNAResult.IsError && readOAPPTemplateDNAResult.Result != null)
                {
                    OAPPTemplateDNA = readOAPPTemplateDNAResult.Result;
                    OnOAPPTemplatePublishStatusChanged?.Invoke(this, new OAPPTemplatePublishStatusEventArgs() { OAPPTemplateDNA = OAPPTemplateDNA, Status = Enums.OAPPTemplatePublishStatus.Packaging });
                    OASISResult<IAvatar> loadAvatarResult = await AvatarManager.Instance.LoadAvatarAsync(avatarId, false, true, providerType);

                    if (loadAvatarResult != null && loadAvatarResult.Result != null && !loadAvatarResult.IsError)
                    {
                        string publishedOAPPTemplateFileName = string.Concat(OAPPTemplateDNA.Name, ".oapptemplate");
                        //string publishedOAPPTemplateSelfContainedFileName = string.Concat(OAPPTemplateDNA.OAPPTemplateName, "(Self Contained).oapp");
                        //string publishedOAPPTemplateSelfContainedFullFileName = string.Concat(OAPPTemplateDNA.OAPPTemplateName, "(Self Contained Full).oapp");
                        //string publishedOAPPTemplateSourceFileName = string.Concat(OAPPTemplateDNA.OAPPTemplateName, ".oappsource");

                        if (string.IsNullOrEmpty(fullPathToPublishTo))
                        {
                            //Directory.CreateDirectory(Path.Combine(fullPathToOAPPTemplate, "Published"));
                            //fullPathToPublishTo = Path.Combine(fullPathToOAPPTemplate, "Published", publishedOAPPTemplateFileName);
                            fullPathToPublishTo = Path.Combine(fullPathToOAPPTemplate, "Published");
                        }

                        if (!Directory.Exists(fullPathToPublishTo))
                            Directory.CreateDirectory(fullPathToPublishTo);

                        //fullPathToPublishTo = Path.Combine(fullPathToPublishTo, publishedOAPPTemplateFileName);
                        //string fullPathToPublishToOAPPTemplateSource = Path.Combine(fullPathToPublishTo, publishedOAPPTemplateSourceFileName);

                        OAPPTemplateDNA.PublishedOn = DateTime.Now;
                        OAPPTemplateDNA.PublishedByAvatarId = avatarId;
                        OAPPTemplateDNA.PublishedByAvatarUsername = loadAvatarResult.Result.Username;
                        OAPPTemplateDNA.OAPPTemplatePublishedOnSTARNET = registerOnSTARNET && (oappBinaryProviderType != ProviderType.None || uploadOAPPTemplateToCloud);

                        if (generateOAPPTemplateBinary)
                        {
                            OAPPTemplateDNA.OAPPTemplatePublishedPath = Path.Combine(fullPathToPublishTo, publishedOAPPTemplateFileName);
                            OAPPTemplateDNA.OAPPTemplatePublishedToCloud = registerOnSTARNET && uploadOAPPTemplateToCloud;
                            OAPPTemplateDNA.OAPPTemplatePublishedProviderType = oappBinaryProviderType;
                        }
                        OAPPTemplateDNA.Versions++;

                        WriteOAPPTemplateDNA(OAPPTemplateDNA, fullPathToOAPPTemplate);
                        OnOAPPTemplatePublishStatusChanged?.Invoke(this, new OAPPTemplatePublishStatusEventArgs() { OAPPTemplateDNA = OAPPTemplateDNA, Status = Enums.OAPPTemplatePublishStatus.Compressing });

                        if (generateOAPPTemplateBinary)
                        {
                            //tempPath = Path.Combine(Path.GetTempPath(), publishedOAPPTemplateFileName);

                            //if (File.Exists(tempPath))
                            //    File.Delete(tempPath);

                            if (File.Exists(OAPPTemplateDNA.OAPPTemplatePublishedPath))
                                File.Delete(OAPPTemplateDNA.OAPPTemplatePublishedPath);

                            ZipFile.CreateFromDirectory(fullPathToOAPPTemplate, OAPPTemplateDNA.OAPPTemplatePublishedPath);
                            //ZipFile.CreateFromDirectory(fullPathToOAPPTemplate, tempPath);
                            //SharpCompress.Compressors.LZMA.LZipStream. (7Zip)
                            //SharpZipLib.

                            //TODO: Look into the most optimal compression...
                            //using (FileStream fs = File.OpenRead(tempPath))
                            //{
                            //    DeflateStream deflateStream = new DeflateStream(fs, CompressionLevel.SmallestSize, false);
                            //    GZipStream gZipStream = new GZipStream(fs, CompressionLevel.SmallestSize, false);

                            //    //deflateStream.Write
                            //}

                            File.Move(tempPath, readOAPPTemplateDNAResult.Result.OAPPTemplatePublishedPath);
                        }

                        //TODO: Currently the filesize will NOT be in the compressed .oapptemplate file because we dont know the size before we create it! ;-) We would need to compress it twice or edit the compressed file after to update the OAPPTemplateDNA inside it...
                        if (!string.IsNullOrEmpty(OAPPTemplateDNA.OAPPTemplatePublishedPath) && File.Exists(OAPPTemplateDNA.OAPPTemplatePublishedPath))
                            OAPPTemplateDNA.OAPPTemplateFileSize = new FileInfo(OAPPTemplateDNA.OAPPTemplatePublishedPath).Length;

                        WriteOAPPTemplateDNA(OAPPTemplateDNA, fullPathToOAPPTemplate);

                        OASISResult<IOAPPTemplate> loadOAPPTemplateResult = await LoadOAPPTemplateAsync(OAPPTemplateDNA.Id);

                        if (loadOAPPTemplateResult != null && loadOAPPTemplateResult.Result != null && !loadOAPPTemplateResult.IsError)
                        {
                            loadOAPPTemplateResult.Result.OAPPTemplateDNA = OAPPTemplateDNA;

                            if (registerOnSTARNET)
                            {
                                if (uploadOAPPTemplateToCloud)
                                {
                                    try
                                    {
                                        OnOAPPTemplatePublishStatusChanged?.Invoke(this, new OAPPTemplatePublishStatusEventArgs() { OAPPTemplateDNA = readOAPPTemplateDNAResult.Result, Status = Enums.OAPPTemplatePublishStatus.Uploading });
                                        StorageClient storage = await StorageClient.CreateAsync();
                                        //var bucket = storage.CreateBucket("oasis", "oapptemplates");

                                        // set minimum chunksize just to see progress updating
                                        var uploadObjectOptions = new UploadObjectOptions
                                        {
                                            ChunkSize = UploadObjectOptions.MinimumChunkSize
                                        };

                                        var progressReporter = new Progress<Google.Apis.Upload.IUploadProgress>(OnUploadProgress);
                                        using var fileStream = File.OpenRead(OAPPTemplateDNA.OAPPTemplatePublishedPath);
                                        _fileLength = fileStream.Length;
                                        _progress = 0;

                                        await storage.UploadObjectAsync("oasis_oapptemplates", publishedOAPPTemplateFileName, "oapptemplate", fileStream, uploadObjectOptions, progress: progressReporter);
                                    }
                                    catch (Exception ex)
                                    {
                                        OASISErrorHandling.HandleWarning(ref result, $"An error occured publishing the OAPPTemplate to cloud storage. Reason: {ex}");
                                        OAPPTemplateDNA.OAPPTemplatePublishedOnSTARNET = registerOnSTARNET && oappBinaryProviderType != ProviderType.None;
                                        OAPPTemplateDNA.OAPPTemplatePublishedToCloud = false;
                                    }
                                }

                                if (oappBinaryProviderType != ProviderType.None)
                                {
                                    loadOAPPTemplateResult.Result.PublishedOAPPTemplate = File.ReadAllBytes(OAPPTemplateDNA.OAPPTemplatePublishedPath);

                                    //TODO: We could use HoloOASIS and other large file storage providers in future...
                                    OASISResult<IOAPPTemplate> saveLargeOAPPTemplateResult = await SaveOAPPTemplateAsync(loadOAPPTemplateResult.Result, avatarId, oappBinaryProviderType);

                                    if (saveLargeOAPPTemplateResult != null && !saveLargeOAPPTemplateResult.IsError && saveLargeOAPPTemplateResult.Result != null)
                                    {
                                        result.Result = readOAPPTemplateDNAResult.Result;
                                        result.IsSaved = true;
                                    }
                                    else
                                    {
                                        OASISErrorHandling.HandleWarning(ref result, $" Error occured saving the published OAPPTemplate binary to STARNET using the {oappBinaryProviderType} provider. Reason: {saveLargeOAPPTemplateResult.Message}");
                                        OAPPTemplateDNA.OAPPTemplatePublishedOnSTARNET = registerOnSTARNET && uploadOAPPTemplateToCloud;
                                        OAPPTemplateDNA.OAPPTemplatePublishedProviderType = ProviderType.None;
                                    }
                                }
                            }

                            OASISResult<IOAPPTemplate> saveOAPPTemplateResult = await SaveOAPPTemplateAsync(loadOAPPTemplateResult.Result, avatarId, providerType);

                            if (saveOAPPTemplateResult != null && !saveOAPPTemplateResult.IsError && saveOAPPTemplateResult.Result != null)
                            {
                                result.Result = readOAPPTemplateDNAResult.Result;
                                result.IsSaved = true;

                                if (readOAPPTemplateDNAResult.Result.STARODKVersion != OASISBootLoader.OASISBootLoader.STARODKVersion)
                                    OASISErrorHandling.HandleWarning(ref result, $" The STAR ODK Version {readOAPPTemplateDNAResult.Result.STARODKVersion} does not match the current version {OASISBootLoader.OASISBootLoader.STARODKVersion}. This may lead to issues, it is recommended to make sure the versions match.");

                                if (readOAPPTemplateDNAResult.Result.OASISVersion != OASISBootLoader.OASISBootLoader.OASISVersion)
                                    OASISErrorHandling.HandleWarning(ref result, $" The OASIS Version {readOAPPTemplateDNAResult.Result.OASISVersion} does not match the current version {OASISBootLoader.OASISBootLoader.OASISVersion}. This may lead to issues, it is recommended to make sure the versions match.");

                                if (readOAPPTemplateDNAResult.Result.COSMICVersion != OASISBootLoader.OASISBootLoader.COSMICVersion)
                                    OASISErrorHandling.HandleWarning(ref result, $" The COSMIC Version {readOAPPTemplateDNAResult.Result.COSMICVersion} does not match the current version {OASISBootLoader.OASISBootLoader.COSMICVersion}. This may lead to issues, it is recommended to make sure the versions match.");

                                if (result.IsWarning)
                                    result.Message = $"OAPP Template successfully published but there were {result.WarningCount} warnings:\n\n {OASISResultHelper.BuildInnerMessageError(result.InnerMessages)}";
                                else
                                    result.Message = "OAPP Template Successfully Published";

                                OnOAPPTemplatePublishStatusChanged?.Invoke(this, new OAPPTemplatePublishStatusEventArgs() { OAPPTemplateDNA = OAPPTemplateDNA, Status = Enums.OAPPTemplatePublishStatus.Published });
                            }
                            else
                                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling SaveOAPPTemplateAsync on {Enum.GetName(typeof(ProviderType), providerType)} provider. Reason: {saveOAPPTemplateResult.Message}");
                        }
                        else
                            OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadOAPPTemplateAsync on {Enum.GetName(typeof(ProviderType), providerType)} provider. Reason: {loadOAPPTemplateResult.Message}");
                    }
                    else
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadAvatarAsync on {Enum.GetName(typeof(ProviderType), providerType)} provider. Reason: {loadAvatarResult.Message}");
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling ReadOAPPTemplateDNAAsync on {Enum.GetName(typeof(ProviderType), providerType)} provider. Reason: {readOAPPTemplateDNAResult.Message}");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} {ex}");
            }

            if (result.IsError)
                OnOAPPTemplatePublishStatusChanged?.Invoke(this, new OAPPTemplatePublishStatusEventArgs() { OAPPTemplateDNA = OAPPTemplateDNA, Status = Enums.OAPPTemplatePublishStatus.Error, ErrorMessage = result.Message });

            return result;
        }

        //TODO: Come back to this, was going to call this for publishing and installing to make sure the DNA hadn't been changed on the disk, but maybe we want to allow this? Not sure, needs more thought...
        //private async OASISResult<bool> IsOAPPTemplateDNAValidAsync(IOAPPTemplateDNA OAPPTemplateDNA)
        //{
        //    OASISResult<IOAPPTemplate> OAPPTemplateResult = await LoadOAPPTemplateAsync(OAPPTemplateDNA.OAPPTemplateId);

        //    if (OAPPTemplateResult != null && OAPPTemplateResult.Result != null && !OAPPTemplateResult.IsError)
        //    {
        //        IOAPPTemplateDNA originalDNA =  JsonSerializer.Deserialize<IOAPPTemplateDNA>(OAPPTemplateResult.Result.MetaData["OAPPTemplateDNA"].ToString());

        //        if (originalDNA != null)
        //        {
        //            if (originalDNA.GenesisType != OAPPTemplateDNA.GenesisType ||
        //                originalDNA.OAPPTemplateType != OAPPTemplateDNA.OAPPTemplateType ||
        //                originalDNA.CelestialBodyType != OAPPTemplateDNA.CelestialBodyType ||
        //                originalDNA.CelestialBodyId != OAPPTemplateDNA.CelestialBodyId ||
        //                originalDNA.CelestialBodyName != OAPPTemplateDNA.CelestialBodyName ||
        //                originalDNA.CreatedByAvatarId != OAPPTemplateDNA.CreatedByAvatarId ||
        //                originalDNA.CreatedByAvatarUsername != OAPPTemplateDNA.CreatedByAvatarUsername ||
        //                originalDNA.CreatedOn != OAPPTemplateDNA.CreatedOn ||
        //                originalDNA.Description != OAPPTemplateDNA.Description ||
        //                originalDNA.IsActive != OAPPTemplateDNA.IsActive ||
        //                originalDNA.LaunchTarget != OAPPTemplateDNA.LaunchTarget ||
        //                originalDNA. != OAPPTemplateDNA.LaunchTarget ||

        //        }
        //    }
        //}

        public OASISResult<IRuntimeDNA> PublishRuntime(string fullPathToRuntime, string launchTarget, Guid avatarId, bool dotnetPublish = true, string fullPathToPublishTo = "", bool registerOnSTARNET = true, bool generateRuntimeSource = true, bool uploadRuntimeSourceToSTARNET = true, bool makeRuntimeSourcePublic = false, bool generateRuntimeBinary = true, bool generateRuntimeSelfContainedBinary = false, bool generateRuntimeSelfContainedFullBinary = false, bool uploadRuntimeToCloud = false, bool uploadRuntimeSelfContainedToCloud = false, bool uploadRuntimeSelfContainedFullToCloud = false, ProviderType providerType = ProviderType.Default, ProviderType oappBinaryProviderType = ProviderType.IPFSOASIS, ProviderType oappSelfContainedBinaryProviderType = ProviderType.None, ProviderType oappSelfContainedFullBinaryProviderType = ProviderType.None)
        {
            OASISResult<IRuntimeDNA> result = new OASISResult<IRuntimeDNA>();
            string errorMessage = "Error occured in RuntimeManager.PublishRuntime. Reason: ";
            IRuntimeDNA RuntimeDNA = null;
            string tempPath = "";

            try
            {
                OASISResult<IRuntimeDNA> readRuntimeDNAResult = ReadRuntimeDNA(fullPathToRuntime);

                if (readRuntimeDNAResult != null && !readRuntimeDNAResult.IsError && readRuntimeDNAResult.Result != null)
                {
                    RuntimeDNA = readRuntimeDNAResult.Result;
                    OnRuntimePublishStatusChanged?.Invoke(this, new RuntimePublishStatusEventArgs() { RuntimeDNA = RuntimeDNA, Status = Enums.RuntimePublishStatus.Packaging });
                    OASISResult<IAvatar> loadAvatarResult = AvatarManager.Instance.LoadAvatar(avatarId, false, true, providerType);

                    if (loadAvatarResult != null && loadAvatarResult.Result != null && !loadAvatarResult.IsError)
                    {
                        string publishedRuntimeFileName = string.Concat(RuntimeDNA.Name, ".Runtime");
                        //string publishedRuntimeSelfContainedFileName = string.Concat(RuntimeDNA.RuntimeName, "(Self Contained).oapp");
                        //string publishedRuntimeSelfContainedFullFileName = string.Concat(RuntimeDNA.RuntimeName, "(Self Contained Full).oapp");
                        //string publishedRuntimeSourceFileName = string.Concat(RuntimeDNA.RuntimeName, ".oappsource");

                        if (string.IsNullOrEmpty(fullPathToPublishTo))
                        {
                            //Directory.CreateDirectory(Path.Combine(fullPathToRuntime, "Published"));
                            //fullPathToPublishTo = Path.Combine(fullPathToRuntime, "Published", publishedRuntimeFileName);
                            fullPathToPublishTo = Path.Combine(fullPathToRuntime, "Published");
                        }

                        if (!Directory.Exists(fullPathToPublishTo))
                            Directory.CreateDirectory(fullPathToPublishTo);

                        //fullPathToPublishTo = Path.Combine(fullPathToPublishTo, publishedRuntimeFileName);
                        //string fullPathToPublishToRuntimeSource = Path.Combine(fullPathToPublishTo, publishedRuntimeSourceFileName);

                        RuntimeDNA.PublishedOn = DateTime.Now;
                        RuntimeDNA.PublishedByAvatarId = avatarId;
                        RuntimeDNA.PublishedByAvatarUsername = loadAvatarResult.Result.Username;
                        RuntimeDNA.RuntimePublishedOnSTARNET = registerOnSTARNET && (oappBinaryProviderType != ProviderType.None || uploadRuntimeToCloud);

                        if (generateRuntimeBinary)
                        {
                            RuntimeDNA.RuntimePublishedPath = Path.Combine(fullPathToPublishTo, publishedRuntimeFileName);
                            RuntimeDNA.RuntimePublishedToCloud = registerOnSTARNET && uploadRuntimeToCloud;
                            RuntimeDNA.RuntimePublishedProviderType = oappBinaryProviderType;
                        }
                        RuntimeDNA.Versions++;

                        WriteRuntimeDNA(RuntimeDNA, fullPathToRuntime);
                        OnRuntimePublishStatusChanged?.Invoke(this, new RuntimePublishStatusEventArgs() { RuntimeDNA = RuntimeDNA, Status = Enums.RuntimePublishStatus.Compressing });

                        if (generateRuntimeBinary)
                        {
                            //tempPath = Path.Combine(Path.GetTempPath(), publishedRuntimeFileName);

                            //if (File.Exists(tempPath))
                            //    File.Delete(tempPath);

                            if (File.Exists(RuntimeDNA.RuntimePublishedPath))
                                File.Delete(RuntimeDNA.RuntimePublishedPath);

                            ZipFile.CreateFromDirectory(fullPathToRuntime, RuntimeDNA.RuntimePublishedPath);
                            //ZipFile.CreateFromDirectory(fullPathToRuntime, tempPath);
                            //SharpCompress.Compressors.LZMA.LZipStream. (7Zip)
                            //SharpZipLib.

                            //TODO: Look into the most optimal compression...
                            //using (FileStream fs = File.OpenRead(tempPath))
                            //{
                            //    DeflateStream deflateStream = new DeflateStream(fs, CompressionLevel.SmallestSize, false);
                            //    GZipStream gZipStream = new GZipStream(fs, CompressionLevel.SmallestSize, false);

                            //    //deflateStream.Write
                            //}

                            File.Move(tempPath, readRuntimeDNAResult.Result.RuntimePublishedPath);
                        }

                        //TODO: Currently the filesize will NOT be in the compressed .Runtime file because we dont know the size before we create it! ;-) We would need to compress it twice or edit the compressed file after to update the RuntimeDNA inside it...
                        if (!string.IsNullOrEmpty(RuntimeDNA.RuntimePublishedPath) && File.Exists(RuntimeDNA.RuntimePublishedPath))
                            RuntimeDNA.RuntimeFileSize = new FileInfo(RuntimeDNA.RuntimePublishedPath).Length;

                        WriteRuntimeDNA(RuntimeDNA, fullPathToRuntime);

                        OASISResult<IRuntime> loadRuntimeResult = LoadRuntime(RuntimeDNA.Id);

                        if (loadRuntimeResult != null && loadRuntimeResult.Result != null && !loadRuntimeResult.IsError)
                        {
                            loadRuntimeResult.Result.RuntimeDNA = RuntimeDNA;

                            if (registerOnSTARNET)
                            {
                                if (uploadRuntimeToCloud)
                                {
                                    try
                                    {
                                        OnRuntimePublishStatusChanged?.Invoke(this, new RuntimePublishStatusEventArgs() { RuntimeDNA = readRuntimeDNAResult.Result, Status = Enums.RuntimePublishStatus.Uploading });
                                        StorageClient storage = StorageClient.Create();
                                        //var bucket = storage.CreateBucket("oasis", "Runtimes");

                                        // set minimum chunksize just to see progress updating
                                        var uploadObjectOptions = new UploadObjectOptions
                                        {
                                            ChunkSize = UploadObjectOptions.MinimumChunkSize
                                        };

                                        var progressReporter = new Progress<Google.Apis.Upload.IUploadProgress>(OnUploadProgress);
                                        using var fileStream = File.OpenRead(RuntimeDNA.RuntimePublishedPath);
                                        _fileLength = fileStream.Length;
                                        _progress = 0;

                                        storage.UploadObject("oasis_Runtimes", publishedRuntimeFileName, "Runtime", fileStream, uploadObjectOptions, progress: progressReporter);
                                    }
                                    catch (Exception ex)
                                    {
                                        OASISErrorHandling.HandleWarning(ref result, $"An error occured publishing the Runtime to cloud storage. Reason: {ex}");
                                        RuntimeDNA.RuntimePublishedOnSTARNET = registerOnSTARNET && oappBinaryProviderType != ProviderType.None;
                                        RuntimeDNA.RuntimePublishedToCloud = false;
                                    }
                                }

                                if (oappBinaryProviderType != ProviderType.None)
                                {
                                    loadRuntimeResult.Result.PublishedRuntime = File.ReadAllBytes(RuntimeDNA.RuntimePublishedPath);

                                    //TODO: We could use HoloOASIS and other large file storage providers in future...
                                    OASISResult<IRuntime> saveLargeRuntimeResult = SaveRuntime(loadRuntimeResult.Result, avatarId, oappBinaryProviderType);

                                    if (saveLargeRuntimeResult != null && !saveLargeRuntimeResult.IsError && saveLargeRuntimeResult.Result != null)
                                    {
                                        result.Result = readRuntimeDNAResult.Result;
                                        result.IsSaved = true;
                                    }
                                    else
                                    {
                                        OASISErrorHandling.HandleWarning(ref result, $" Error occured saving the published Runtime binary to STARNET using the {oappBinaryProviderType} provider. Reason: {saveLargeRuntimeResult.Message}");
                                        RuntimeDNA.RuntimePublishedOnSTARNET = registerOnSTARNET && uploadRuntimeToCloud;
                                        RuntimeDNA.RuntimePublishedProviderType = ProviderType.None;
                                    }
                                }
                            }

                            OASISResult<IRuntime> saveRuntimeResult = SaveRuntime(loadRuntimeResult.Result, avatarId, providerType);

                            if (saveRuntimeResult != null && !saveRuntimeResult.IsError && saveRuntimeResult.Result != null)
                            {
                                result.Result = readRuntimeDNAResult.Result;
                                result.IsSaved = true;

                                if (readRuntimeDNAResult.Result.STARODKVersion != OASISBootLoader.OASISBootLoader.STARODKVersion)
                                    OASISErrorHandling.HandleWarning(ref result, $" The STAR ODK Version {readRuntimeDNAResult.Result.STARODKVersion} does not match the current version {OASISBootLoader.OASISBootLoader.STARODKVersion}. This may lead to issues, it is recommended to make sure the versions match.");

                                if (readRuntimeDNAResult.Result.OASISVersion != OASISBootLoader.OASISBootLoader.OASISVersion)
                                    OASISErrorHandling.HandleWarning(ref result, $" The OASIS Version {readRuntimeDNAResult.Result.OASISVersion} does not match the current version {OASISBootLoader.OASISBootLoader.OASISVersion}. This may lead to issues, it is recommended to make sure the versions match.");

                                if (readRuntimeDNAResult.Result.COSMICVersion != OASISBootLoader.OASISBootLoader.COSMICVersion)
                                    OASISErrorHandling.HandleWarning(ref result, $" The COSMIC Version {readRuntimeDNAResult.Result.COSMICVersion} does not match the current version {OASISBootLoader.OASISBootLoader.COSMICVersion}. This may lead to issues, it is recommended to make sure the versions match.");

                                if (result.IsWarning)
                                    result.Message = $"OAPP Template successfully published but there were {result.WarningCount} warnings:\n\n {OASISResultHelper.BuildInnerMessageError(result.InnerMessages)}";
                                else
                                    result.Message = "OAPP Template Successfully Published";

                                OnRuntimePublishStatusChanged?.Invoke(this, new RuntimePublishStatusEventArgs() { RuntimeDNA = RuntimeDNA, Status = Enums.RuntimePublishStatus.Published });
                            }
                            else
                                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling SaveRuntimeAsync on {Enum.GetName(typeof(ProviderType), providerType)} provider. Reason: {saveRuntimeResult.Message}");
                        }
                        else
                            OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadRuntimeAsync on {Enum.GetName(typeof(ProviderType), providerType)} provider. Reason: {loadRuntimeResult.Message}");
                    }
                    else
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadAvatarAsync on {Enum.GetName(typeof(ProviderType), providerType)} provider. Reason: {loadAvatarResult.Message}");
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling ReadRuntimeDNAAsync on {Enum.GetName(typeof(ProviderType), providerType)} provider. Reason: {readRuntimeDNAResult.Message}");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} {ex}");
            }

            if (result.IsError)
                OnRuntimePublishStatusChanged?.Invoke(this, new RuntimePublishStatusEventArgs() { RuntimeDNA = RuntimeDNA, Status = Enums.RuntimePublishStatus.Error, ErrorMessage = result.Message });

            return result;
        }

        ////TODO: Think will return IRuntimeDNA instead of IRuntime (get from metadata)
        //public async Task<OASISResult<IEnumerable<IRuntime>>> ListRuntimesCreatedByAvatarAsync(Guid avatarId, ProviderType providerType = ProviderType.Default)
        //{
        //    OASISResult<IEnumerable<IRuntime>> result = new OASISResult<IEnumerable<IRuntime>>();
        //    string errorMessage = "Error occured in RuntimeManager.ListRuntimesCreatedByAvatarAsync, Reason:";

        //    try
        //    {
        //        OASISResult<IEnumerable<Runtime>> Runtimes = await Data.LoadHolonsForParentAsync<Runtime>(avatarId, HolonType.Runtime, true, true, 0, true, false, 0, HolonType.All, 0, providerType);

        //        if (Runtimes != null && Runtimes.Result != null && !Runtimes.IsError)
        //        {
        //            result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<IEnumerable<Runtime>, IEnumerable<IRuntime>>(Runtimes);
        //            result.Result = [.. Runtimes.Result];
        //        }
        //        else
        //            OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling Data.LoadHolonsForParentAsync, reason: {Runtimes.Message}");
        //    }
        //    catch (Exception e)
        //    {
        //        OASISErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
        //    }

        //    return result;
        //}

        ////TODO: Think will return IRuntimeDNA instead of IRuntime (get from metadata)
        //public OASISResult<IEnumerable<IRuntime>> ListRuntimesCreatedByAvatar(Guid avatarId, ProviderType providerType = ProviderType.Default)
        //{
        //    OASISResult<IEnumerable<IRuntime>> result = new OASISResult<IEnumerable<IRuntime>>();
        //    string errorMessage = "Error occured in RuntimeManager.ListRuntimesCreatedByAvatar, Reason:";

        //    try
        //    {
        //        OASISResult<IEnumerable<Runtime>> Runtimes = Data.LoadHolonsForParent<Runtime>(avatarId, HolonType.Runtime, true, true, 0, true, false, 0, HolonType.All, 0, providerType);

        //        if (Runtimes != null && Runtimes.Result != null && !Runtimes.IsError)
        //        {
        //            result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<IEnumerable<Runtime>, IEnumerable<IRuntime>>(Runtimes);
        //            result.Result = [.. Runtimes.Result];
        //        }
        //        else
        //            OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling Data.LoadHolonsForParent, reason: {Runtimes.Message}");
        //    }
        //    catch (Exception e)
        //    {
        //        OASISErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
        //    }

        //    return result;
        //}

        ////TODO: Think will return IRuntimeDNA instead of IRuntime (get from metadata)
        //public async Task<OASISResult<IEnumerable<IRuntime>>> ListAllRuntimesAsync(ProviderType providerType = ProviderType.Default)
        //{
        //    OASISResult<IEnumerable<IRuntime>> result = new OASISResult<IEnumerable<IRuntime>>();
        //    string errorMessage = "Error occured in RuntimeManager.ListAllRuntimesAsync, Reason:";

        //    try
        //    {
        //        //result = DecodeNFTMetaData(await Data.LoadAllHolonsAsync(HolonType.NFT, true, true, 0, true, false, HolonType.All, 0, providerType), result, errorMessage);
        //        //OASISResult<IEnumerable<IHolon>> holons = await Data.LoadAllHolonsAsync(HolonType.Runtime, true, true, 0, true, false, HolonType.All, 0, providerType);
        //        OASISResult<IEnumerable<Runtime>> Runtimes = await Data.LoadAllHolonsAsync<Runtime>(HolonType.Runtime, true, true, 0, true, false, HolonType.All, 0, providerType);

        //        if (Runtimes != null && Runtimes.Result != null && !Runtimes.IsError)
        //        {
        //            result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<IEnumerable<Runtime>, IEnumerable<IRuntime>>(Runtimes);
        //            result.Result = [.. Runtimes.Result];
        //        }
        //        else
        //            OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling Data.LoadAllHolonsAsync, reason: {Runtimes.Message}");

        //        //if (holons != null && holons.Result != null && !holons.IsError)
        //        //{
        //        //    List<IRuntime> Runtimes = new List<IRuntime>();

        //        //    foreach (IHolon holon in holons.Result) 
        //        //    {
        //        //        IRuntime Runtime = Mapper<IHolon, Runtime>.MapBaseHolonProperties(holon);
        //        //        Runtime.GenesisType = holon.MetaData[]
        //        //    }
        //        //}
        //    }
        //    catch (Exception e)
        //    {
        //        OASISErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
        //    }

        //    return result;
        //}

        ////TODO: Think will return IRuntimeDNA instead of IRuntime (get from metadata)
        //public OASISResult<IEnumerable<IRuntime>> ListAllRuntimes(ProviderType providerType = ProviderType.Default)
        //{
        //    OASISResult<IEnumerable<IRuntime>> result = new OASISResult<IEnumerable<IRuntime>>();
        //    string errorMessage = "Error occured in RuntimeManager.ListAllRuntimes, Reason:";

        //    try
        //    {
        //        OASISResult<IEnumerable<Runtime>> Runtimes = Data.LoadAllHolons<Runtime>(HolonType.Runtime, true, true, 0, true, false, HolonType.All, 0, providerType);

        //        if (Runtimes != null && Runtimes.Result != null && !Runtimes.IsError)
        //        {
        //            result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<IEnumerable<Runtime>, IEnumerable<IRuntime>>(Runtimes);
        //            result.Result = [.. Runtimes.Result];


        //        }
        //        else
        //            OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling Data.LoadAllHolons, reason: {Runtimes.Message}");
        //    }
        //    catch (Exception e)
        //    {
        //        OASISErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
        //    }

        //    return result;
        //}

        ////TODO: Think will return IRuntimeDNA instead of IRuntime (get from metadata)
        //public async Task<OASISResult<IRuntime>> LoadRuntimeAsync(Guid RuntimeId, ProviderType providerType = ProviderType.Default)
        //{
        //    OASISResult<IRuntime> result = new OASISResult<IRuntime>();
        //    string errorMessage = "Error occured in RuntimeManager.LoadRuntimeAsync, Reason:";

        //    try
        //    {
        //        OASISResult<Runtime> Runtime = await Data.LoadHolonAsync<Runtime>(RuntimeId, true, true, 0, true, false, HolonType.All, 0, providerType);

        //        if (Runtime != null && Runtime.Result != null && !Runtime.IsError)
        //        {
        //            //RuntimeDNA RuntimeDNA = JsonSerializer.Deserialize<RuntimeDNA>(Runtime.Result.MetaData["RuntimeDNAJSON"].ToString());
        //            //Runtime.Result.RuntimeDNA = RuntimeDNA;

        //            result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<Runtime, IRuntime>(Runtime);

        //            //result.Result.RuntimeDNA = JsonSerializer.Deserialize<RuntimeDNA>(result.Result.MetaData["RuntimeDNAJSON"].ToString());
        //            result.Result = Runtime.Result;
        //        }
        //        else
        //            OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling Data.LoadHolonAsync, reason: {Runtime.Message}");
        //    }
        //    catch (Exception e)
        //    {
        //        OASISErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
        //    }

        //    return result;
        //}

        ////TODO: Think will return IRuntimeDNA instead of IRuntime (get from metadata)
        //public OASISResult<IRuntime> LoadRuntime(Guid RuntimeId, ProviderType providerType = ProviderType.Default)
        //{
        //    OASISResult<IRuntime> result = new OASISResult<IRuntime>();
        //    string errorMessage = "Error occured in RuntimeManager.LoadRuntime, Reason:";

        //    try
        //    {
        //        OASISResult<Runtime> Runtime = Data.LoadHolon<Runtime>(RuntimeId, true, true, 0, true, false, HolonType.All, 0, providerType);

        //        if (Runtime != null && Runtime.Result != null && !Runtime.IsError)
        //        {
        //            //RuntimeDNA RuntimeDNA = JsonSerializer.Deserialize<RuntimeDNA>(Runtime.Result.MetaData["RuntimeDNAJSON"].ToString());
        //            //Runtime.Result.RuntimeDNA = RuntimeDNA;

        //            result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<Runtime, IRuntime>(Runtime);
        //            result.Result = Runtime.Result;
        //        }
        //        else
        //            OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling Data.LoadHolon, reason: {Runtime.Message}");
        //    }
        //    catch (Exception e)
        //    {
        //        OASISErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
        //    }

        //    return result;
        //}

        //public async Task<OASISResult<IRuntime>> LoadRuntimeAsync(string fullPathToRuntime, ProviderType providerType = ProviderType.Default)
        //{
        //    OASISResult<IRuntime> result = new OASISResult<IRuntime>();
        //    string errorMessage = "Error occured in RuntimeManager.LoadRuntimeAsync, Reason:";

        //    try
        //    {
        //        OASISResult<IRuntimeDNA> readRuntimeDNAResult = await ReadRuntimeDNAAsync(fullPathToRuntime);

        //        if (readRuntimeDNAResult != null && readRuntimeDNAResult.Result != null && !readRuntimeDNAResult.IsError)
        //            result = await LoadRuntimeAsync(readRuntimeDNAResult.Result.RuntimeId, providerType);
        //        else
        //            OASISErrorHandling.HandleError(ref result, $"{errorMessage} Reading the RuntimeDNA with ReadRuntimeDNAAsync. Reason: {readRuntimeDNAResult.Message}");
        //    }
        //    catch (Exception e)
        //    {
        //        OASISErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
        //    }

        //    return result;
        //}

        //public OASISResult<IRuntime> LoadRuntime(string fullPathToRuntime, ProviderType providerType = ProviderType.Default)
        //{
        //    OASISResult<IRuntime> result = new OASISResult<IRuntime>();
        //    string errorMessage = "Error occured in RuntimeManager.LoadRuntime, Reason:";

        //    try
        //    {
        //        OASISResult<IRuntimeDNA> readRuntimeDNAResult = ReadRuntimeDNA(fullPathToRuntime);

        //        if (readRuntimeDNAResult != null && readRuntimeDNAResult.Result != null && !readRuntimeDNAResult.IsError)
        //            result = LoadRuntime(readRuntimeDNAResult.Result.RuntimeId, providerType);
        //        else
        //            OASISErrorHandling.HandleError(ref result, $"{errorMessage} Reading the RuntimeDNA with ReadRuntimeDNA. Reason: {readRuntimeDNAResult.Message}");
        //    }
        //    catch (Exception e)
        //    {
        //        OASISErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
        //    }

        //    return result;
        //}

        //public async Task<OASISResult<IRuntimeDNA>> CreateRuntimeAsync(string RuntimeName, string RuntimeDescription, RuntimeType RuntimeType, RuntimeTemplateType RuntimeTemplateType, Guid RuntimeTemplateId, GenesisType genesisType, Guid avatarId, string fullPathToRuntime, ICelestialBody celestialBody = null, IEnumerable<IZome> zomes = null, ProviderType providerType = ProviderType.Default)
        //{
        //    OASISResult<IRuntimeDNA> result = new OASISResult<IRuntimeDNA>();
        //    string errorMessage = "Error occured in RuntimeManager.CreateRuntimeAsync, Reason:";

        //    try
        //    {
        //        Runtime Runtime = new Runtime()
        //        {
        //            Id = Guid.NewGuid(),
        //            Name = RuntimeName,
        //            Description = RuntimeDescription,
        //        };

        //        OASISResult<IAvatar> avatarResult = await AvatarManager.Instance.LoadAvatarAsync(avatarId, false, true, providerType);

        //        if (avatarResult != null && avatarResult.Result != null && !avatarResult.IsError)
        //        {
        //            RuntimeDNA RuntimeDNA = new RuntimeDNA()
        //            {
        //                RuntimeId = Runtime.Id,
        //                RuntimeName = RuntimeName,
        //                Description = RuntimeDescription,
        //                //CelestialBody = celestialBody, //TODO: Temp
        //                CelestialBodyId = celestialBody != null ? celestialBody.Id : Guid.Empty,
        //                CelestialBodyName = celestialBody != null ? celestialBody.Name : "",
        //                CelestialBodyType = celestialBody != null ? celestialBody.HolonType : HolonType.None,
        //                //Zomes = zomes, //Can be either zomes of CelestialBody but not both (zomes are contained in CelestialBody) but if no CelestialBody is generated then this prop is used instead.
        //                RuntimeType = RuntimeType,
        //                RuntimeTemplateType = RuntimeTemplateType,
        //                RuntimeTemplateId = RuntimeTemplateId,
        //                GenesisType = genesisType,
        //                CreatedByAvatarId = avatarId,
        //                CreatedByAvatarUsername = avatarResult.Result.Username,
        //                CreatedOn = DateTime.Now,
        //                Version = "1.0.0",
        //                STARODKVersion = OASISBootLoader.OASISBootLoader.STARODKVersion,
        //                OASISVersion = OASISBootLoader.OASISBootLoader.OASISVersion,
        //                COSMICVersion = OASISBootLoader.OASISBootLoader.COSMICVersion,
        //                DotNetVersion = OASISBootLoader.OASISBootLoader.DotNetVersion
        //            };

        //            await WriteRuntimeDNAAsync(RuntimeDNA, fullPathToRuntime);

        //            Runtime.RuntimeDNA = RuntimeDNA;
        //            OASISResult<Runtime> saveHolonResult = await Data.SaveHolonAsync<Runtime>(Runtime, avatarId, true, true, 0, true, false, providerType);

        //            if (saveHolonResult != null && saveHolonResult.Result != null && !saveHolonResult.IsError)
        //            {
        //                if (celestialBody != null)
        //                {
        //                    OASISResult<ICelestialBody> celestialBodyResult = await celestialBody.SaveAsync(true, true, 0, true, false, providerType);

        //                    if (celestialBodyResult != null && celestialBodyResult.Result != null && !celestialBodyResult.IsError)
        //                    {
        //                        result.Result = RuntimeDNA;
        //                        result.Message = $"Successfully created the Runtime on the {Enum.GetName(typeof(ProviderType), providerType)} provider by AvatarId {avatarId} for RuntimeType {Enum.GetName(typeof(RuntimeType), RuntimeType)} and GenesisType {Enum.GetName(typeof(GenesisType), genesisType)}.";
        //                    }
        //                    else
        //                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured saving Runtime to the {Enum.GetName(typeof(ProviderType), providerType)} provider. Reason: {saveHolonResult.Message}");
        //                }
        //                else
        //                    result.Message = $"Successfully created the Runtime on the {Enum.GetName(typeof(ProviderType), providerType)} provider by AvatarId {avatarId} for RuntimeType {Enum.GetName(typeof(RuntimeType), RuntimeType)} and GenesisType {Enum.GetName(typeof(GenesisType), genesisType)}.";
        //            }
        //            else
        //                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured saving Runtime to the {Enum.GetName(typeof(ProviderType), providerType)} provider. Reason: {saveHolonResult.Message}");
        //        }
        //        else
        //            OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadAvatarAsync on {Enum.GetName(typeof(ProviderType), providerType)} provider. Reason: {avatarResult.Message}");
        //    }
        //    catch (Exception ex)
        //    {
        //        OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured saving Runtime to the {Enum.GetName(typeof(ProviderType), providerType)} provider. Reason: {ex}");
        //    }

        //    return result;
        //}
        //public OASISResult<IRuntimeDNA> CreateRuntime(string RuntimeName, string RuntimeDescription, RuntimeType RuntimeType, RuntimeTemplateType RuntimeTemplateType, Guid RuntimeTemplateId, GenesisType genesisType, Guid avatarId, string fullPathToRuntime, ICelestialBody celestialBody = null, IEnumerable<IZome> zomes = null, ProviderType providerType = ProviderType.Default)
        //{
        //    OASISResult<IRuntimeDNA> result = new OASISResult<IRuntimeDNA>();
        //    string errorMessage = "Error occured in RuntimeManager.CreateRuntime, Reason:";

        //    try
        //    {
        //        Runtime Runtime = new Runtime()
        //        {
        //            Id = Guid.NewGuid(),
        //            Name = RuntimeName,
        //            Description = RuntimeDescription,
        //        };

        //        if (zomes != null)
        //        {
        //            foreach (IZome zome in zomes)
        //                Runtime.Children.Add(zome);
        //        }

        //        OASISResult<IAvatar> avatarResult = AvatarManager.Instance.LoadAvatar(avatarId, false, true, providerType);

        //        if (avatarResult != null && avatarResult.Result != null && !avatarResult.IsError)
        //        {
        //            RuntimeDNA RuntimeDNA = new RuntimeDNA()
        //            {
        //                RuntimeId = Runtime.Id,
        //                RuntimeName = RuntimeName,
        //                Description = RuntimeDescription,
        //                //CelestialBody = celestialBody, //TODO: Temp
        //                CelestialBodyId = celestialBody != null ? celestialBody.Id : Guid.Empty,
        //                CelestialBodyName = celestialBody != null ? celestialBody.Name : "",
        //                CelestialBodyType = celestialBody != null ? celestialBody.HolonType : HolonType.None,
        //                //Zomes = zomes, //Can be either zomes of CelestialBody but not both (zomes are contained in CelestialBody) but if no CelestialBody is generated then this prop is used instead.
        //                RuntimeType = RuntimeType,
        //                RuntimeTemplateType = RuntimeTemplateType,
        //                RuntimeTemplateId = RuntimeTemplateId,
        //                GenesisType = genesisType,
        //                CreatedByAvatarId = avatarId,
        //                CreatedByAvatarUsername = avatarResult.Result.Username,
        //                CreatedOn = DateTime.Now,
        //                Version = "1.0.0"
        //            };

        //            WriteRuntimeDNA(RuntimeDNA, fullPathToRuntime);

        //            Runtime.MetaData["RuntimeDNAJSON"] = JsonSerializer.Serialize(RuntimeDNA); //Store the RuntimeDNA in the db so it can be verified later against the file OASISDNA when publishing, installing etc to make sure its not been tampered with.
        //            OASISResult<IHolon> saveHolonResult = Data.SaveHolon(Runtime, avatarId, true, true, 0, true, false, providerType);

        //            if (saveHolonResult != null && saveHolonResult.Result != null && !saveHolonResult.IsError)
        //            {
        //                if (celestialBody != null)
        //                {
        //                    OASISResult<ICelestialBody> celestialBodyResult = celestialBody.Save(true, true, 0, true, false, providerType);

        //                    if (celestialBodyResult != null && celestialBodyResult.Result != null && !celestialBodyResult.IsError)
        //                    {
        //                        result.Result = RuntimeDNA;
        //                        result.Message = $"Successfully created the Runtime on the {Enum.GetName(typeof(ProviderType), providerType)} provider by AvatarId {avatarId} for RuntimeType {Enum.GetName(typeof(RuntimeType), RuntimeType)} and GenesisType {Enum.GetName(typeof(GenesisType), genesisType)}.";
        //                    }
        //                    else
        //                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured saving Runtime to the {Enum.GetName(typeof(ProviderType), providerType)} provider. Reason: {saveHolonResult.Message}");
        //                }
        //                else
        //                    result.Message = $"Successfully created the Runtime on the {Enum.GetName(typeof(ProviderType), providerType)} provider by AvatarId {avatarId} for RuntimeType {Enum.GetName(typeof(RuntimeType), RuntimeType)} and GenesisType {Enum.GetName(typeof(GenesisType), genesisType)}.";
        //            }
        //            else
        //                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured saving Runtime to the {Enum.GetName(typeof(ProviderType), providerType)} provider. Reason: {saveHolonResult.Message}");
        //        }
        //        else
        //            OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadAvatar on {Enum.GetName(typeof(ProviderType), providerType)} provider. Reason: {avatarResult.Message}");
        //    }
        //    catch (Exception ex)
        //    {
        //        OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured saving Runtime to the {Enum.GetName(typeof(ProviderType), providerType)} provider. Reason: {ex}");
        //    }

        //    return result;
        //}

        ////TODO: Think will return IRuntimeDNA instead of IRuntime (get from metadata)
        //public async Task<OASISResult<IRuntime>> SaveRuntimeAsync(IRuntime Runtime, ProviderType providerType = ProviderType.Default)
        //{
        //    OASISResult<IRuntime> result = new OASISResult<IRuntime>();
        //    OASISResult<Runtime> saveResult = await Runtime.SaveAsync<Runtime>(true, true, 0, true, false, providerType);

        //    if (saveResult != null && !saveResult.IsError && saveResult.Result != null)
        //    {
        //        result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<Runtime, IRuntime>(saveResult);
        //        result.Result = saveResult.Result;
        //    }
        //    else
        //        OASISErrorHandling.HandleError(ref result, $"An error occured in RuntimeManager.SaveRuntimeAsync saving the Runtime. Reason: {saveResult.Message}");

        //    return result;
        //}

        ////TODO: Think will return IRuntimeDNA instead of IRuntime (get from metadata)
        //public OASISResult<IRuntime> SaveRuntime(IRuntime Runtime, ProviderType providerType = ProviderType.Default)
        //{
        //    OASISResult<IRuntime> result = new OASISResult<IRuntime>();
        //    OASISResult<Runtime> saveResult = Runtime.Save<Runtime>(true, true, 0, true, false, providerType);

        //    if (saveResult != null && !saveResult.IsError && saveResult.Result != null)
        //    {
        //        result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<Runtime, IRuntime>(saveResult);
        //        result.Result = saveResult.Result;
        //    }
        //    else
        //        OASISErrorHandling.HandleError(ref result, $"An error occured in RuntimeManager.SaveRuntime saving the Runtime. Reason: {saveResult.Message}");

        //    return result;
        //}

        //public async Task<OASISResult<IRuntime>> DeleteRuntimeAsync(Guid RuntimeId, ProviderType providerType = ProviderType.Default)
        //{
        //    OASISResult<IRuntime> result = await LoadRuntimeAsync(RuntimeId, providerType);

        //    if (result != null && !result.IsError && result.Result != null)
        //        result = await DeleteRuntimeAsync(result.Result);
        //    else
        //        OASISErrorHandling.HandleError(ref result, $"An error occured in RuntimeManager.DeleteRuntimeAsync loading the Runtime. Reason: {result.Message}");

        //    return result;
        //}

        //public async Task<OASISResult<IRuntime>> DeleteRuntime(Guid RuntimeId, ProviderType providerType = ProviderType.Default)
        //{
        //    OASISResult<IRuntime> result = LoadRuntime(RuntimeId, providerType);

        //    if (result != null && !result.IsError && result.Result != null)
        //        result = DeleteRuntime(result.Result);
        //    else
        //        OASISErrorHandling.HandleError(ref result, $"An error occured in RuntimeManager.DeleteRuntime loading the Runtime. Reason: {result.Message}");

        //    return result;
        //}

        //public async Task<OASISResult<IRuntime>> DeleteRuntimeAsync(IRuntime Runtime, ProviderType providerType = ProviderType.Default)
        //{
        //    OASISResult<IRuntime> result = new OASISResult<IRuntime>();
        //    OASISResult<IHolon> deleteResult = await Runtime.DeleteAsync(true, providerType);

        //    if (deleteResult != null && !deleteResult.IsError && deleteResult.Result != null)
        //    {
        //        result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<IHolon, IRuntime>(deleteResult);
        //        result.Result = (IRuntime)deleteResult.Result;
        //    }
        //    else
        //        OASISErrorHandling.HandleError(ref result, $"An error occured in RuntimeManager.DeleteRuntimeAsync deleting the Runtime. Reason: {deleteResult.Message}");

        //    return result;
        //}

        //public OASISResult<IRuntime> DeleteRuntime(IRuntime Runtime, ProviderType providerType = ProviderType.Default)
        //{
        //    OASISResult<IRuntime> result = new OASISResult<IRuntime>();
        //    OASISResult<IHolon> deleteResult = Runtime.Delete(true, providerType);

        //    if (deleteResult != null && !deleteResult.IsError && deleteResult.Result != null)
        //    {
        //        result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<IHolon, IRuntime>(deleteResult);
        //        result.Result = (IRuntime)deleteResult.Result;
        //    }
        //    else
        //        OASISErrorHandling.HandleError(ref result, $"An error occured in RuntimeManager.DeleteRuntime deleting the Runtime. Reason: {deleteResult.Message}");

        //    return result;
        //}

        //public async Task<OASISResult<IRuntimeDNA>> PublishRuntimeAsync(string fullPathToRuntime, string launchTarget, Guid avatarId, bool dotnetPublish = true, string fullPathToPublishTo = "", bool registerOnSTARNET = true, bool generateRuntimeSource = true, bool uploadRuntimeSourceToSTARNET = true, bool makeRuntimeSourcePublic = false, bool generateRuntimeBinary = true, bool generateRuntimeSelfContainedBinary = false, bool generateRuntimeSelfContainedFullBinary = false, bool uploadRuntimeToCloud = false, bool uploadRuntimeSelfContainedToCloud = false, bool uploadRuntimeSelfContainedFullToCloud = false, ProviderType providerType = ProviderType.Default, ProviderType RuntimeBinaryProviderType = ProviderType.IPFSOASIS, ProviderType RuntimeSelfContainedBinaryProviderType = ProviderType.None, ProviderType RuntimeSelfContainedFullBinaryProviderType = ProviderType.None)
        //{
        //    OASISResult<IRuntimeDNA> result = new OASISResult<IRuntimeDNA>();
        //    string errorMessage = "Error occured in RuntimeManager.PublishRuntimeAsync. Reason: ";
        //    IRuntimeDNA RuntimeDNA = null;
        //    string tempPath = "";

        //    try
        //    {
        //        OASISResult<IRuntimeDNA> readRuntimeDNAResult = await ReadRuntimeDNAAsync(fullPathToRuntime);

        //        if (readRuntimeDNAResult != null && !readRuntimeDNAResult.IsError && readRuntimeDNAResult.Result != null)
        //        {
        //            RuntimeDNA = readRuntimeDNAResult.Result;
        //            OnRuntimePublishStatusChanged?.Invoke(this, new RuntimePublishStatusEventArgs() { RuntimeDNA = RuntimeDNA, Status = Enums.RuntimePublishStatus.Packaging });
        //            OASISResult <IAvatar> loadAvatarResult = await AvatarManager.Instance.LoadAvatarAsync(avatarId, false, true, providerType);

        //            if (loadAvatarResult != null && loadAvatarResult.Result != null && !loadAvatarResult.IsError)
        //            {
        //                string publishedRuntimeFileName = string.Concat(RuntimeDNA.RuntimeName, ".Runtime");
        //                string publishedRuntimeSelfContainedFileName = string.Concat(RuntimeDNA.RuntimeName, "(Self Contained).Runtime");
        //                string publishedRuntimeSelfContainedFullFileName = string.Concat(RuntimeDNA.RuntimeName, "(Self Contained Full).Runtime");
        //                string publishedRuntimeSourceFileName = string.Concat(RuntimeDNA.RuntimeName, ".Runtimesource");

        //                if (dotnetPublish)
        //                {
        //                    //TODO: Finish implementing this.
        //                    //Process.Start("dotnet publish -c Release -r <RID> --self-contained");
        //                    //Process.Start("dotnet publish -c Release -r win-x64 --self-contained");
        //                    //string command = 

        //                    OnRuntimePublishStatusChanged?.Invoke(this, new RuntimePublishStatusEventArgs() { RuntimeDNA = RuntimeDNA, Status = Enums.RuntimePublishStatus.DotNetPublishing });
        //                    string dotnetPublishPath = Path.Combine(fullPathToRuntime, "dotnetPublished");
        //                    Process.Start($"dotnet publish PROJECT {fullPathToRuntime} -c Release --self-contained -output {dotnetPublishPath}");
        //                    fullPathToRuntime = dotnetPublishPath;

        //                    //"bin\\Release\\net8.0\\";
        //                }

        //                if (string.IsNullOrEmpty(fullPathToPublishTo))
        //                {
        //                    //Directory.CreateDirectory(Path.Combine(fullPathToRuntime, "Published"));
        //                    //fullPathToPublishTo = Path.Combine(fullPathToRuntime, "Published", publishedRuntimeFileName);
        //                    fullPathToPublishTo = Path.Combine(fullPathToRuntime, "Published");
        //                }

        //                if (!Directory.Exists(fullPathToPublishTo))
        //                    Directory.CreateDirectory(fullPathToPublishTo);

        //                //fullPathToPublishTo = Path.Combine(fullPathToPublishTo, publishedRuntimeFileName);
        //                //string fullPathToPublishToRuntimeSource = Path.Combine(fullPathToPublishTo, publishedRuntimeSourceFileName);

        //                RuntimeDNA.PublishedOn = DateTime.Now;
        //                RuntimeDNA.PublishedByAvatarId = avatarId;
        //                RuntimeDNA.PublishedByAvatarUsername = loadAvatarResult.Result.Username;
        //                RuntimeDNA.RuntimePublishedOnSTARNET = registerOnSTARNET && (RuntimeBinaryProviderType != ProviderType.None || uploadRuntimeToCloud || RuntimeSelfContainedBinaryProviderType != ProviderType.None || uploadRuntimeSelfContainedToCloud || RuntimeSelfContainedFullBinaryProviderType != ProviderType.None || uploadRuntimeSelfContainedFullToCloud);
        //                RuntimeDNA.LaunchTarget = launchTarget;

        //                if (generateRuntimeBinary)
        //                {
        //                    RuntimeDNA.RuntimePublishedPath = Path.Combine(fullPathToPublishTo, publishedRuntimeFileName);
        //                    RuntimeDNA.RuntimePublishedToCloud = registerOnSTARNET && uploadRuntimeToCloud;
        //                    RuntimeDNA.RuntimePublishedProviderType = RuntimeBinaryProviderType;
        //                }

        //                if (generateRuntimeSelfContainedBinary)
        //                {
        //                    RuntimeDNA.RuntimeSelfContainedPublishedPath = Path.Combine(fullPathToPublishTo, publishedRuntimeSelfContainedFileName);
        //                    RuntimeDNA.RuntimeSelfContainedPublishedToCloud = registerOnSTARNET && uploadRuntimeSelfContainedToCloud;
        //                    RuntimeDNA.RuntimeSelfContainedPublishedProviderType = RuntimeSelfContainedBinaryProviderType;
        //                }

        //                if (generateRuntimeSelfContainedFullBinary)
        //                {
        //                    RuntimeDNA.RuntimeSelfContainedFullPublishedPath = Path.Combine(fullPathToPublishTo, publishedRuntimeSelfContainedFullFileName);
        //                    RuntimeDNA.RuntimeSelfContainedFullPublishedToCloud = registerOnSTARNET && uploadRuntimeSelfContainedFullToCloud;
        //                    RuntimeDNA.RuntimeSelfContainedFullPublishedProviderType = RuntimeSelfContainedFullBinaryProviderType;
        //                }

        //                if (generateRuntimeSource)
        //                {
        //                    RuntimeDNA.RuntimeSourcePublishedPath = Path.Combine(fullPathToPublishTo, publishedRuntimeSourceFileName);
        //                    RuntimeDNA.RuntimeSourcePublishedOnSTARNET = registerOnSTARNET && uploadRuntimeSourceToSTARNET;
        //                    RuntimeDNA.RuntimeSourcePublicOnSTARNET = makeRuntimeSourcePublic;
        //                }

        //                RuntimeDNA.Versions++;

        //                WriteRuntimeDNA(RuntimeDNA, fullPathToRuntime);
        //                OnRuntimePublishStatusChanged?.Invoke(this, new RuntimePublishStatusEventArgs() { RuntimeDNA = RuntimeDNA, Status = Enums.RuntimePublishStatus.Compressing });

        //                if (generateRuntimeBinary)
        //                {
        //                    //tempPath = Path.Combine(Path.GetTempPath(), publishedRuntimeFileName);

        //                    //if (File.Exists(tempPath))
        //                    //    File.Delete(tempPath);

        //                    if (File.Exists(RuntimeDNA.RuntimePublishedPath))
        //                        File.Delete(RuntimeDNA.RuntimePublishedPath);

        //                    ZipFile.CreateFromDirectory(fullPathToRuntime, RuntimeDNA.RuntimePublishedPath);
        //                    //ZipFile.CreateFromDirectory(fullPathToRuntime, tempPath);
        //                    //SharpCompress.Compressors.LZMA.LZipStream. (7Zip)
        //                    //SharpZipLib.

        //                    //TODO: Look into the most optimal compression...
        //                    //using (FileStream fs = File.OpenRead(tempPath))
        //                    //{
        //                    //    DeflateStream deflateStream = new DeflateStream(fs, CompressionLevel.SmallestSize, false);
        //                    //    GZipStream gZipStream = new GZipStream(fs, CompressionLevel.SmallestSize, false);

        //                    //    //deflateStream.Write
        //                    //}

        //                    File.Move(tempPath, readRuntimeDNAResult.Result.RuntimePublishedPath);
        //                }

        //                if (generateRuntimeSelfContainedBinary)
        //                {
        //                   // tempPath = Path.Combine(Path.GetTempPath(), publishedRuntimeSelfContainedFileName);

        //                    //if (File.Exists(tempPath))
        //                    //    File.Delete(tempPath);

        //                    if (File.Exists(RuntimeDNA.RuntimeSelfContainedPublishedPath))
        //                        File.Delete(RuntimeDNA.RuntimeSelfContainedPublishedPath);

        //                    ZipFile.CreateFromDirectory(fullPathToRuntime, RuntimeDNA.RuntimeSelfContainedPublishedPath);
        //                    //ZipFile.CreateFromDirectory(fullPathToRuntime, tempPath);
        //                    //SharpCompress.Compressors.LZMA.LZipStream. (7Zip)
        //                    //SharpZipLib.

        //                    //TODO: Look into the most optimal compression...
        //                    //using (FileStream fs = File.OpenRead(tempPath))
        //                    //{
        //                    //    DeflateStream deflateStream = new DeflateStream(fs, CompressionLevel.SmallestSize, false);
        //                    //    GZipStream gZipStream = new GZipStream(fs, CompressionLevel.SmallestSize, false);

        //                    //    //deflateStream.Write
        //                    //}

        //                    File.Move(tempPath, readRuntimeDNAResult.Result.RuntimeSelfContainedPublishedPath);
        //                }

        //                if (generateRuntimeSelfContainedFullBinary)
        //                {
        //                    //tempPath = Path.Combine(Path.GetTempPath(), publishedRuntimeSelfContainedFullFileName);

        //                    //if (File.Exists(tempPath))
        //                    //    File.Delete(tempPath);

        //                    if (File.Exists(RuntimeDNA.RuntimeSelfContainedFullPublishedPath))
        //                        File.Delete(RuntimeDNA.RuntimeSelfContainedFullPublishedPath);

        //                    ZipFile.CreateFromDirectory(fullPathToRuntime, RuntimeDNA.RuntimeSelfContainedFullPublishedPath);
        //                    //ZipFile.CreateFromDirectory(fullPathToRuntime, tempPath);
        //                    //SharpCompress.Compressors.LZMA.LZipStream. (7Zip)
        //                    //SharpZipLib.

        //                    //TODO: Look into the most optimal compression...
        //                    //using (FileStream fs = File.OpenRead(tempPath))
        //                    //{
        //                    //    DeflateStream deflateStream = new DeflateStream(fs, CompressionLevel.SmallestSize, false);
        //                    //    GZipStream gZipStream = new GZipStream(fs, CompressionLevel.SmallestSize, false);

        //                    //    //deflateStream.Write
        //                    //}

        //                    //File.Move(tempPath, readRuntimeDNAResult.Result.RuntimeSelfContainedFullPublishedPath);
        //                }


        //                //TODO: Need to generate and move the Runtimesource file here.
        //                if (generateRuntimeSource)
        //                {
        //                    tempPath = Path.Combine(Path.GetTempPath(), RuntimeDNA.RuntimeName);

        //                    if (File.Exists(RuntimeDNA.RuntimeSourcePublishedPath))
        //                        File.Delete(RuntimeDNA.RuntimeSourcePublishedPath);

        //                    if (Directory.Exists(tempPath))
        //                        Directory.Delete(tempPath, true);

        //                    DirectoryHelper.CopyFilesRecursively(fullPathToRuntime, tempPath);
        //                    Directory.Delete(Path.Combine(tempPath, "bin"), true);
        //                    Directory.Delete(Path.Combine(tempPath, "obj"), true);

        //                    ZipFile.CreateFromDirectory(tempPath, RuntimeDNA.RuntimeSourcePublishedPath);
        //                }

        //                //TODO: Currently the filesize will NOT be in the compressed .Runtime file because we dont know the size before we create it! ;-) We would need to compress it twice or edit the compressed file after to update the RuntimeDNA inside it...
        //                if (!string.IsNullOrEmpty(RuntimeDNA.RuntimePublishedPath) && File.Exists(RuntimeDNA.RuntimePublishedPath))
        //                    RuntimeDNA.RuntimeFileSize = new FileInfo(RuntimeDNA.RuntimePublishedPath).Length;

        //                if (!string.IsNullOrEmpty(RuntimeDNA.RuntimeSelfContainedPublishedPath) && File.Exists(RuntimeDNA.RuntimeSelfContainedPublishedPath))
        //                    RuntimeDNA.RuntimeSelfContainedFileSize = new FileInfo(RuntimeDNA.RuntimeSelfContainedPublishedPath).Length;

        //                if (!string.IsNullOrEmpty(RuntimeDNA.RuntimeSelfContainedFullPublishedPath) && File.Exists(RuntimeDNA.RuntimeSelfContainedFullPublishedPath))
        //                    RuntimeDNA.RuntimeSelfContainedFullFileSize = new FileInfo(RuntimeDNA.RuntimeSelfContainedFullPublishedPath).Length;

        //                if (!string.IsNullOrEmpty(RuntimeDNA.RuntimeSourcePublishedPath) && File.Exists(RuntimeDNA.RuntimeSourcePublishedPath))
        //                    RuntimeDNA.RuntimeSourceFileSize = new FileInfo(RuntimeDNA.RuntimeSourcePublishedPath).Length;

        //                WriteRuntimeDNA(RuntimeDNA, fullPathToRuntime);

        //                OASISResult<IRuntime> loadRuntimeResult = await LoadRuntimeAsync(RuntimeDNA.RuntimeId);

        //                if (loadRuntimeResult != null && loadRuntimeResult.Result != null && !loadRuntimeResult.IsError)
        //                {
        //                    loadRuntimeResult.Result.RuntimeDNA = RuntimeDNA;

        //                    if (registerOnSTARNET)
        //                    {
        //                        if (uploadRuntimeSourceToSTARNET)
        //                            loadRuntimeResult.Result.PublishedRuntimeSource = File.ReadAllBytes(RuntimeDNA.RuntimeSourcePublishedPath);

        //                        if (uploadRuntimeToCloud)
        //                        {
        //                            try
        //                            {
        //                                OnRuntimePublishStatusChanged?.Invoke(this, new RuntimePublishStatusEventArgs() { RuntimeDNA = readRuntimeDNAResult.Result, Status = Enums.RuntimePublishStatus.Uploading });
        //                                StorageClient storage = await StorageClient.CreateAsync();
        //                                //var bucket = storage.CreateBucket("oasis", "Runtimes");

        //                                // set minimum chunksize just to see progress updating
        //                                var uploadObjectOptions = new UploadObjectOptions
        //                                {
        //                                    ChunkSize = UploadObjectOptions.MinimumChunkSize
        //                                };

        //                                var progressReporter = new Progress<Google.Apis.Upload.IUploadProgress>(OnUploadProgress);
        //                                using var fileStream = File.OpenRead(RuntimeDNA.RuntimePublishedPath);
        //                                _fileLength = fileStream.Length;
        //                                _progress = 0;

        //                                await storage.UploadObjectAsync("oasis_Runtimes", publishedRuntimeFileName, "Runtime", fileStream, uploadObjectOptions, progress: progressReporter);
        //                            }
        //                            catch (Exception ex)
        //                            {
        //                                OASISErrorHandling.HandleWarning(ref result, $"An error occured publishing the Runtime to cloud storage. Reason: {ex}");
        //                                RuntimeDNA.RuntimePublishedOnSTARNET = registerOnSTARNET && RuntimeBinaryProviderType != ProviderType.None;
        //                                RuntimeDNA.RuntimePublishedToCloud = false;
        //                            }
        //                        }

        //                        if (RuntimeBinaryProviderType != ProviderType.None)
        //                        {
        //                            //The smallest Runtime is around 250MB because of the 208MB runtimes.
        //                            loadRuntimeResult.Result.PublishedRuntime = File.ReadAllBytes(RuntimeDNA.RuntimePublishedPath);

        //                            //TODO: We could use HoloOASIS and other large file storage providers in future...
        //                            OASISResult<IRuntime> saveLargeRuntimeResult = await SaveRuntimeAsync(loadRuntimeResult.Result, RuntimeBinaryProviderType);

        //                            if (saveLargeRuntimeResult != null && !saveLargeRuntimeResult.IsError && saveLargeRuntimeResult.Result != null)
        //                            {
        //                                result.Result = readRuntimeDNAResult.Result;
        //                                result.IsSaved = true;
        //                            }
        //                            else
        //                            {
        //                                OASISErrorHandling.HandleWarning(ref result, $" Error occured saving the published Runtime binary to STARNET using the {RuntimeBinaryProviderType} provider. Reason: {saveLargeRuntimeResult.Message}");
        //                                RuntimeDNA.RuntimePublishedOnSTARNET = registerOnSTARNET && uploadRuntimeToCloud;
        //                                RuntimeDNA.RuntimePublishedProviderType = ProviderType.None;
        //                            }
        //                        }
        //                    }

        //                    OASISResult<IRuntime> saveRuntimeResult = await SaveRuntimeAsync(loadRuntimeResult.Result, providerType);

        //                    if (saveRuntimeResult != null && !saveRuntimeResult.IsError && saveRuntimeResult.Result != null)
        //                    {
        //                        result.Result = readRuntimeDNAResult.Result;
        //                        result.IsSaved = true;

        //                        if (readRuntimeDNAResult.Result.STARODKVersion != OASISBootLoader.OASISBootLoader.STARODKVersion)
        //                            OASISErrorHandling.HandleWarning(ref result, $" The STAR ODK Version {readRuntimeDNAResult.Result.STARODKVersion} does not match the current version {OASISBootLoader.OASISBootLoader.STARODKVersion}. This may lead to issues, it is recommended to make sure the versions match.");

        //                        if (readRuntimeDNAResult.Result.OASISVersion != OASISBootLoader.OASISBootLoader.OASISVersion)
        //                            OASISErrorHandling.HandleWarning(ref result, $" The OASIS Version {readRuntimeDNAResult.Result.OASISVersion} does not match the current version {OASISBootLoader.OASISBootLoader.OASISVersion}. This may lead to issues, it is recommended to make sure the versions match.");

        //                        if (readRuntimeDNAResult.Result.COSMICVersion != OASISBootLoader.OASISBootLoader.COSMICVersion)
        //                            OASISErrorHandling.HandleWarning(ref result, $" The COSMIC Version {readRuntimeDNAResult.Result.COSMICVersion} does not match the current version {OASISBootLoader.OASISBootLoader.COSMICVersion}. This may lead to issues, it is recommended to make sure the versions match.");

        //                        if (result.IsWarning)
        //                            result.Message = $"Runtime successfully published but there were {result.WarningCount} warnings:\n\n {OASISResultHelper.BuildInnerMessageError(result.InnerMessages)}";
        //                        else
        //                            result.Message = "Runtime Successfully Published";

        //                        OnRuntimePublishStatusChanged?.Invoke(this, new RuntimePublishStatusEventArgs() { RuntimeDNA = RuntimeDNA, Status = Enums.RuntimePublishStatus.Published });
        //                    }
        //                    else
        //                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling SaveRuntimeAsync on {Enum.GetName(typeof(ProviderType), providerType)} provider. Reason: {saveRuntimeResult.Message}");
        //                }  
        //                else
        //                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadRuntimeAsync on {Enum.GetName(typeof(ProviderType), providerType)} provider. Reason: {loadRuntimeResult.Message}");
        //            }
        //            else
        //                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadAvatarAsync on {Enum.GetName(typeof(ProviderType), providerType)} provider. Reason: {loadAvatarResult.Message}");
        //        }
        //        else
        //            OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling ReadRuntimeDNAAsync on {Enum.GetName(typeof(ProviderType), providerType)} provider. Reason: {readRuntimeDNAResult.Message}");
        //    }
        //    catch (Exception ex) 
        //    {
        //        OASISErrorHandling.HandleError(ref result, $"{errorMessage} {ex}");
        //    }

        //    if (result.IsError)
        //        OnRuntimePublishStatusChanged?.Invoke(this, new RuntimePublishStatusEventArgs() { RuntimeDNA = RuntimeDNA, Status = Enums.RuntimePublishStatus.Error, ErrorMessage = result.Message });

        //    return result;
        //}

        ////TODO: Come back to this, was going to call this for publishing and installing to make sure the DNA hadn't been changed on the disk, but maybe we want to allow this? Not sure, needs more thought...
        ////private async OASISResult<bool> IsRuntimeDNAValidAsync(IRuntimeDNA RuntimeDNA)
        ////{
        ////    OASISResult<IRuntime> RuntimeResult = await LoadRuntimeAsync(RuntimeDNA.RuntimeId);

        ////    if (RuntimeResult != null && RuntimeResult.Result != null && !RuntimeResult.IsError)
        ////    {
        ////        IRuntimeDNA originalDNA =  JsonSerializer.Deserialize<IRuntimeDNA>(RuntimeResult.Result.MetaData["RuntimeDNA"].ToString());

        ////        if (originalDNA != null)
        ////        {
        ////            if (originalDNA.GenesisType != RuntimeDNA.GenesisType ||
        ////                originalDNA.RuntimeType != RuntimeDNA.RuntimeType ||
        ////                originalDNA.CelestialBodyType != RuntimeDNA.CelestialBodyType ||
        ////                originalDNA.CelestialBodyId != RuntimeDNA.CelestialBodyId ||
        ////                originalDNA.CelestialBodyName != RuntimeDNA.CelestialBodyName ||
        ////                originalDNA.CreatedByAvatarId != RuntimeDNA.CreatedByAvatarId ||
        ////                originalDNA.CreatedByAvatarUsername != RuntimeDNA.CreatedByAvatarUsername ||
        ////                originalDNA.CreatedOn != RuntimeDNA.CreatedOn ||
        ////                originalDNA.Description != RuntimeDNA.Description ||
        ////                originalDNA.IsActive != RuntimeDNA.IsActive ||
        ////                originalDNA.LaunchTarget != RuntimeDNA.LaunchTarget ||
        ////                originalDNA. != RuntimeDNA.LaunchTarget ||

        ////        }
        ////    }
        ////}

        //public OASISResult<IRuntimeDNA> PublishRuntime(string fullPathToRuntime, string launchTarget, Guid avatarId, bool dotnetPublish = true, string fullPathToPublishTo = "", bool registerOnSTARNET = true, bool generateRuntimeSource = true, bool uploadRuntimeSourceToSTARNET = true, bool makeRuntimeSourcePublic = false, bool generateRuntimeBinary = true, bool generateRuntimeSelfContainedBinary = false, bool generateRuntimeSelfContainedFullBinary = false, bool uploadRuntimeToCloud = false, bool uploadRuntimeSelfContainedToCloud = false, bool uploadRuntimeSelfContainedFullToCloud = false, ProviderType providerType = ProviderType.Default, ProviderType RuntimeBinaryProviderType = ProviderType.IPFSOASIS, ProviderType RuntimeSelfContainedBinaryProviderType = ProviderType.None, ProviderType RuntimeSelfContainedFullBinaryProviderType = ProviderType.None)
        //{
        //    OASISResult<IRuntimeDNA> result = new OASISResult<IRuntimeDNA>();
        //    string errorMessage = "Error occured in RuntimeManager.PublishRuntimeAsync. Reason: ";
        //    IRuntimeDNA RuntimeDNA = null;

        //    try
        //    {
        //        OASISResult<IRuntimeDNA> readRuntimeDNAResult = ReadRuntimeDNA(fullPathToRuntime);

        //        if (readRuntimeDNAResult != null && !readRuntimeDNAResult.IsError && readRuntimeDNAResult.Result != null)
        //        {
        //            RuntimeDNA = readRuntimeDNAResult.Result;
        //            OnRuntimePublishStatusChanged?.Invoke(this, new RuntimePublishStatusEventArgs() { RuntimeDNA = RuntimeDNA, Status = Enums.RuntimePublishStatus.Packaging });
        //            OASISResult<IAvatar> loadAvatarResult = AvatarManager.Instance.LoadAvatar(avatarId, false, true, providerType);

        //            if (loadAvatarResult != null && loadAvatarResult.Result != null && !loadAvatarResult.IsError)
        //            {
        //                string publishedRuntimeFileName = string.Concat(RuntimeDNA.RuntimeName, ".Runtime");
        //                string publishedRuntimeSourceFileName = string.Concat(RuntimeDNA.RuntimeName, ".Runtimesource");
        //                string tempPath = Path.Combine(Path.GetTempPath(), publishedRuntimeFileName);

        //                if (dotnetPublish)
        //                {
        //                    //TODO: Finish implementing this.
        //                    //Process.Start("dotnet publish -c Release -r <RID> --self-contained");
        //                    //Process.Start("dotnet publish -c Release -r win-x64 --self-contained");
        //                    //string command = 

        //                    OnRuntimePublishStatusChanged?.Invoke(this, new RuntimePublishStatusEventArgs() { RuntimeDNA = RuntimeDNA, Status = Enums.RuntimePublishStatus.DotNetPublishing });
        //                    string dotnetPublishPath = Path.Combine(fullPathToRuntime, "dotnetPublished");
        //                    Process.Start($"dotnet publish PROJECT {fullPathToRuntime} -c Release --self-contained -output {dotnetPublishPath}");
        //                    fullPathToRuntime = dotnetPublishPath;

        //                    //"bin\\Release\\net8.0\\";
        //                }

        //                if (string.IsNullOrEmpty(fullPathToPublishTo))
        //                {
        //                    //Directory.CreateDirectory(Path.Combine(fullPathToRuntime, "Published"));
        //                    //fullPathToPublishTo = Path.Combine(fullPathToRuntime, "Published", publishedRuntimeFileName);
        //                    fullPathToPublishTo = Path.Combine(fullPathToRuntime, "Published");
        //                }

        //                if (!Directory.Exists(fullPathToPublishTo))
        //                    Directory.CreateDirectory(fullPathToPublishTo);

        //                fullPathToPublishTo = Path.Combine(fullPathToPublishTo, publishedRuntimeFileName);
        //                string fullPathToPublishToRuntimeSource = Path.Combine(fullPathToPublishTo, publishedRuntimeSourceFileName);

        //                RuntimeDNA.PublishedOn = DateTime.Now;
        //                RuntimeDNA.PublishedByAvatarId = avatarId;
        //                RuntimeDNA.PublishedByAvatarUsername = loadAvatarResult.Result.Username;
        //                RuntimeDNA.LaunchTarget = launchTarget;
        //                RuntimeDNA.RuntimePublishedPath = fullPathToPublishTo;
        //                RuntimeDNA.RuntimeSourcePublishedPath = fullPathToPublishToRuntimeSource;
        //                RuntimeDNA.RuntimeSourcePublishedOnSTARNET = registerOnSTARNET;
        //                RuntimeDNA.RuntimeSourcePublicOnSTARNET = makeRuntimeSourcePublic;
        //                RuntimeDNA.RuntimePublishedOnSTARNET = registerOnSTARNET && (RuntimeBinaryProviderType != ProviderType.None || uploadRuntimeToCloud);
        //                RuntimeDNA.RuntimePublishedToCloud = registerOnSTARNET && uploadRuntimeToCloud;
        //                RuntimeDNA.RuntimePublishedProviderType = RuntimeBinaryProviderType;
        //                RuntimeDNA.Versions++;

        //                WriteRuntimeDNA(RuntimeDNA, fullPathToRuntime);

        //                if (File.Exists(tempPath))
        //                    File.Delete(tempPath);

        //                OnRuntimePublishStatusChanged?.Invoke(this, new RuntimePublishStatusEventArgs() { RuntimeDNA = RuntimeDNA, Status = Enums.RuntimePublishStatus.Compressing });
        //                ZipFile.CreateFromDirectory(fullPathToRuntime, tempPath);
        //                //SharpCompress.Compressors.LZMA.LZipStream. (7Zip)
        //                //SharpZipLib.

        //                //TODO: Look into the most optimal compression...
        //                //using (FileStream fs = File.OpenRead(tempPath))
        //                //{
        //                //    DeflateStream deflateStream = new DeflateStream(fs, CompressionLevel.SmallestSize, false);
        //                //    GZipStream gZipStream = new GZipStream(fs, CompressionLevel.SmallestSize, false);

        //                //    //deflateStream.Write
        //                //}

        //                if (File.Exists(RuntimeDNA.RuntimePublishedPath))
        //                    File.Delete(RuntimeDNA.RuntimePublishedPath);

        //                if (File.Exists(RuntimeDNA.RuntimeSourcePublishedPath))
        //                    File.Delete(RuntimeDNA.RuntimeSourcePublishedPath);

        //                File.Move(tempPath, readRuntimeDNAResult.Result.RuntimePublishedPath);

        //                //TODO: Need to generate and move the Runtimesource file here.
        //                if (generateRuntimeSource)
        //                {
        //                    tempPath = Path.Combine(Path.GetTempPath(), RuntimeDNA.RuntimeName);
        //                    //stringtempPath = Path.Combine(Path.GetTempPath(), RuntimeDNA.RuntimeName);

        //                    if (Directory.Exists(tempPath))
        //                        Directory.Delete(tempPath, true);

        //                    DirectoryHelper.CopyFilesRecursively(fullPathToRuntime, Path.Combine(Path.GetTempPath(), RuntimeDNA.RuntimeName));
        //                    Directory.Delete(Path.Combine(tempPath, "bin"), true);
        //                    Directory.Delete(Path.Combine(tempPath, "obj"), true);

        //                    ZipFile.CreateFromDirectory(tempPath, RuntimeDNA.RuntimeSourcePublishedPath);
        //                }

        //                //TODO: Currently the filesize will NOT be in the compressed .Runtime file because we dont know the size before we create it! ;-) We would need to compress it twice or edit the compressed file after to update the RuntimeDNA inside it...
        //                if (!string.IsNullOrEmpty(RuntimeDNA.RuntimePublishedPath) && File.Exists(RuntimeDNA.RuntimePublishedPath))
        //                    RuntimeDNA.RuntimeFileSize = new FileInfo(RuntimeDNA.RuntimePublishedPath).Length;

        //                if (!string.IsNullOrEmpty(RuntimeDNA.RuntimeSourcePublishedPath) && File.Exists(RuntimeDNA.RuntimeSourcePublishedPath))
        //                    RuntimeDNA.RuntimeSourceFileSize = new FileInfo(RuntimeDNA.RuntimeSourcePublishedPath).Length;

        //                WriteRuntimeDNA(RuntimeDNA, fullPathToRuntime);

        //                OASISResult<IRuntime> loadRuntimeResult = LoadRuntime(RuntimeDNA.RuntimeId);

        //                if (loadRuntimeResult != null && loadRuntimeResult.Result != null && !loadRuntimeResult.IsError)
        //                {
        //                    loadRuntimeResult.Result.RuntimeDNA = RuntimeDNA;

        //                    if (registerOnSTARNET)
        //                    {
        //                        if (uploadRuntimeSourceToSTARNET)
        //                            loadRuntimeResult.Result.PublishedRuntimeSource = File.ReadAllBytes(RuntimeDNA.RuntimeSourcePublishedPath);

        //                        if (uploadRuntimeToCloud)
        //                        {
        //                            try
        //                            {
        //                                OnRuntimePublishStatusChanged?.Invoke(this, new RuntimePublishStatusEventArgs() { RuntimeDNA = readRuntimeDNAResult.Result, Status = Enums.RuntimePublishStatus.Uploading });
        //                                StorageClient storage = StorageClient.Create();
        //                                //var bucket = storage.CreateBucket("oasis", "Runtimes");

        //                                // set minimum chunksize just to see progress updating
        //                                var uploadObjectOptions = new UploadObjectOptions
        //                                {
        //                                    ChunkSize = UploadObjectOptions.MinimumChunkSize
        //                                };

        //                                var progressReporter = new Progress<Google.Apis.Upload.IUploadProgress>(OnUploadProgress);
        //                                using var fileStream = File.OpenRead(RuntimeDNA.RuntimePublishedPath);
        //                                _fileLength = fileStream.Length;
        //                                _progress = 0;

        //                                storage.UploadObject("oasis_Runtimes", publishedRuntimeFileName, "Runtime", fileStream, uploadObjectOptions, progress: progressReporter);
        //                            }
        //                            catch (Exception ex)
        //                            {
        //                                OASISErrorHandling.HandleWarning(ref result, $"An error occured publishing the Runtime to cloud storage. Reason: {ex}");
        //                                RuntimeDNA.RuntimePublishedOnSTARNET = registerOnSTARNET && RuntimeBinaryProviderType != ProviderType.None;
        //                                RuntimeDNA.RuntimePublishedToCloud = false;
        //                            }
        //                        }

        //                        if (RuntimeBinaryProviderType != ProviderType.None)
        //                        {
        //                            //The smallest Runtime is around 250MB because of the 208MB runtimes.
        //                            loadRuntimeResult.Result.PublishedRuntime = File.ReadAllBytes(RuntimeDNA.RuntimePublishedPath);

        //                            //TODO: We could use HoloOASIS and other large file storage providers in future...
        //                            OASISResult<IRuntime> saveLargeRuntimeResult = SaveRuntime(loadRuntimeResult.Result, RuntimeBinaryProviderType);

        //                            if (saveLargeRuntimeResult != null && !saveLargeRuntimeResult.IsError && saveLargeRuntimeResult.Result != null)
        //                            {
        //                                result.Result = readRuntimeDNAResult.Result;
        //                                result.IsSaved = true;
        //                            }
        //                            else
        //                            {
        //                                OASISErrorHandling.HandleWarning(ref result, $" Error occured saving the published Runtime binary to STARNET using the {RuntimeBinaryProviderType} provider. Reason: {saveLargeRuntimeResult.Message}");
        //                                RuntimeDNA.RuntimePublishedOnSTARNET = registerOnSTARNET && uploadRuntimeToCloud;
        //                                RuntimeDNA.RuntimePublishedProviderType = ProviderType.None;
        //                            }
        //                        }
        //                    }

        //                    OASISResult<IRuntime> saveRuntimeResult = SaveRuntime(loadRuntimeResult.Result, providerType);

        //                    if (saveRuntimeResult != null && !saveRuntimeResult.IsError && saveRuntimeResult.Result != null)
        //                    {
        //                        result.Result = readRuntimeDNAResult.Result;
        //                        result.IsSaved = true;

        //                        if (readRuntimeDNAResult.Result.STARODKVersion != OASISBootLoader.OASISBootLoader.STARODKVersion)
        //                            OASISErrorHandling.HandleWarning(ref result, $" The STAR ODK Version {readRuntimeDNAResult.Result.STARODKVersion} does not match the current version {OASISBootLoader.OASISBootLoader.STARODKVersion}. This may lead to issues, it is recommended to make sure the versions match.");

        //                        if (readRuntimeDNAResult.Result.OASISVersion != OASISBootLoader.OASISBootLoader.OASISVersion)
        //                            OASISErrorHandling.HandleWarning(ref result, $" The OASIS Version {readRuntimeDNAResult.Result.OASISVersion} does not match the current version {OASISBootLoader.OASISBootLoader.OASISVersion}. This may lead to issues, it is recommended to make sure the versions match.");

        //                        if (readRuntimeDNAResult.Result.COSMICVersion != OASISBootLoader.OASISBootLoader.COSMICVersion)
        //                            OASISErrorHandling.HandleWarning(ref result, $" The COSMIC Version {readRuntimeDNAResult.Result.COSMICVersion} does not match the current version {OASISBootLoader.OASISBootLoader.COSMICVersion}. This may lead to issues, it is recommended to make sure the versions match.");

        //                        if (result.IsWarning)
        //                            result.Message = $"Runtime successfully published but there were {result.WarningCount} warnings:\n\n {OASISResultHelper.BuildInnerMessageError(result.InnerMessages)}";
        //                        else
        //                            result.Message = "Runtime Successfully Published";

        //                        OnRuntimePublishStatusChanged?.Invoke(this, new RuntimePublishStatusEventArgs() { RuntimeDNA = RuntimeDNA, Status = Enums.RuntimePublishStatus.Published });
        //                    }
        //                    else
        //                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling SaveRuntimeAsync on {Enum.GetName(typeof(ProviderType), providerType)} provider. Reason: {saveRuntimeResult.Message}");
        //                }
        //                else
        //                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadRuntimeAsync on {Enum.GetName(typeof(ProviderType), providerType)} provider. Reason: {loadRuntimeResult.Message}");
        //            }
        //            else
        //                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadAvatarAsync on {Enum.GetName(typeof(ProviderType), providerType)} provider. Reason: {loadAvatarResult.Message}");
        //        }
        //        else
        //            OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling ReadRuntimeDNAAsync on {Enum.GetName(typeof(ProviderType), providerType)} provider. Reason: {readRuntimeDNAResult.Message}");
        //    }
        //    catch (Exception ex)
        //    {
        //        OASISErrorHandling.HandleError(ref result, $"{errorMessage} {ex}");
        //    }

        //    if (result.IsError)
        //        OnRuntimePublishStatusChanged?.Invoke(this, new RuntimePublishStatusEventArgs() { RuntimeDNA = RuntimeDNA, Status = Enums.RuntimePublishStatus.Error, ErrorMessage = result.Message });

        //    return result;
        //}

        //public async Task<OASISResult<IRuntimeDNA>> UnPublishRuntimeAsync(IRuntimeDNA RuntimeDNA, ProviderType providerType = ProviderType.Default)
        //{
        //    OASISResult<IRuntimeDNA> result = new OASISResult<IRuntimeDNA>(RuntimeDNA);
        //    OASISResult<IRuntime> RuntimeResult = await LoadRuntimeAsync(RuntimeDNA.RuntimeId, providerType);
        //    string errorMessage = "Error occured in UnPublishRuntimeAsync. Reason: ";

        //    if (RuntimeResult != null && RuntimeResult.Result != null && !RuntimeResult.IsError)
        //    {
        //        RuntimeResult.Result.RuntimeDNA.PublishedOn = DateTime.MinValue;
        //        RuntimeResult.Result.RuntimeDNA.PublishedByAvatarId = Guid.Empty;
        //        RuntimeResult.Result.RuntimeDNA.PublishedByAvatarUsername = "";

        //        RuntimeResult = await SaveRuntimeAsync(RuntimeResult.Result, providerType);

        //        if (RuntimeResult != null && RuntimeResult.Result != null && !RuntimeResult.IsError)
        //        {
        //            RuntimeDNA.PublishedOn = DateTime.MinValue;
        //            RuntimeDNA.PublishedByAvatarId = Guid.Empty;
        //            result.Message = "Runtime Unpublised";
        //        }
        //        else
        //            OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured saving the Runtime with the SaveRuntimeAsync method, reason: {RuntimeResult.Message}");
        //    }
        //    else
        //        OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured loading the Runtime with the LoadRuntimeAsync method, reason: {RuntimeResult.Message}");

        //    return result;
        //}

        //public OASISResult<IRuntimeDNA> UnPublishRuntime(IRuntimeDNA RuntimeDNA, ProviderType providerType = ProviderType.Default)
        //{
        //    OASISResult<IRuntimeDNA> result = new OASISResult<IRuntimeDNA>(RuntimeDNA);
        //    OASISResult<IRuntime> RuntimeResult = LoadRuntime(RuntimeDNA.RuntimeId, providerType);
        //    string errorMessage = "Error occured in UnPublishRuntime. Reason: ";

        //    if (RuntimeResult != null && RuntimeResult.Result != null && !RuntimeResult.IsError)
        //    {
        //        RuntimeResult.Result.RuntimeDNA.PublishedOn = DateTime.MinValue;
        //        RuntimeResult.Result.RuntimeDNA.PublishedByAvatarId = Guid.Empty;
        //        RuntimeResult.Result.RuntimeDNA.PublishedByAvatarUsername = "";

        //        RuntimeResult = SaveRuntime(RuntimeResult.Result, providerType);

        //        if (RuntimeResult != null && RuntimeResult.Result != null && !RuntimeResult.IsError)
        //        {
        //            RuntimeDNA.PublishedOn = DateTime.MinValue;
        //            RuntimeDNA.PublishedByAvatarId = Guid.Empty;
        //            result.Message = "Runtime Unpublised";
        //        }
        //        else
        //            OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured saving the Runtime with the SaveRuntime method, reason: {RuntimeResult.Message}");
        //    }
        //    else
        //        OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured loading the Runtime with the LoadRuntime method, reason: {RuntimeResult.Message}");

        //    return result;
        //}

        //public async Task<OASISResult<IRuntimeDNA>> UnPublishRuntimeAsync(IRuntime Runtime, ProviderType providerType = ProviderType.Default)
        //{
        //    OASISResult<IRuntimeDNA> result = new OASISResult<IRuntimeDNA>();
        //    string errorMessage = "Error occured in UnPublishRuntimeAsync. Reason: ";

        //    Runtime.RuntimeDNA.PublishedOn = DateTime.MinValue;
        //    Runtime.RuntimeDNA.PublishedByAvatarId = Guid.Empty;
        //    Runtime.RuntimeDNA.PublishedByAvatarUsername = "";

        //    OASISResult<IRuntime> RuntimeResult = await SaveRuntimeAsync(Runtime, providerType);

        //    if (RuntimeResult != null && RuntimeResult.Result != null && !RuntimeResult.IsError)
        //    {
        //        result.Result = Runtime.RuntimeDNA; //ConvertRuntimeToRuntimeDNA(Runtime);
        //        result.Message = "Runtime Unpublised";
        //    }
        //    else
        //        OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured saving the Runtime with the SaveRuntimeAsync method, reason: {RuntimeResult.Message}");

        //    return result;
        //}

        //public OASISResult<IRuntimeDNA> UnPublishRuntime(IRuntime Runtime, ProviderType providerType = ProviderType.Default)
        //{
        //    OASISResult<IRuntimeDNA> result = new OASISResult<IRuntimeDNA>();
        //    string errorMessage = "Error occured in UnPublishRuntime. Reason: ";

        //    Runtime.RuntimeDNA.PublishedOn = DateTime.MinValue;
        //    Runtime.RuntimeDNA.PublishedByAvatarId = Guid.Empty;
        //    Runtime.RuntimeDNA.PublishedByAvatarUsername = "";

        //    OASISResult<IRuntime> RuntimeResult = SaveRuntime(Runtime, providerType);

        //    if (RuntimeResult != null && RuntimeResult.Result != null && !RuntimeResult.IsError)
        //    {
        //        result.Result = Runtime.RuntimeDNA; //ConvertRuntimeToRuntimeDNA(Runtime);
        //        result.Message = "Runtime Unpublised";
        //    }
        //    else
        //        OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured saving the Runtime with the SaveRuntime method, reason: {RuntimeResult.Message}");

        //    return result;
        //}

        //public async Task<OASISResult<IRuntimeDNA>> UnPublishRuntimeAsync(Guid RuntimeId, ProviderType providerType = ProviderType.Default)
        //{
        //    OASISResult<IRuntimeDNA> result = new OASISResult<IRuntimeDNA>();
        //    OASISResult<IRuntime> loadResult = await LoadRuntimeAsync(RuntimeId, providerType);

        //    if (loadResult != null && loadResult.Result != null && !loadResult.IsError)
        //        result = await UnPublishRuntimeAsync(loadResult.Result, providerType);
        //    else
        //        OASISErrorHandling.HandleError(ref result, $"Error occured in UnPublishRuntimeAsync loading the Runtime with the LoadRuntimeAsync method, reason: {loadResult.Message}");

        //    return result;
        //}

        //public OASISResult<IRuntimeDNA> UnPublishRuntime(Guid RuntimeId, ProviderType providerType = ProviderType.Default)
        //{
        //    OASISResult<IRuntimeDNA> result = new OASISResult<IRuntimeDNA>();
        //    OASISResult<IRuntime> loadResult = LoadRuntime(RuntimeId, providerType);

        //    if (loadResult != null && loadResult.Result != null && !loadResult.IsError)
        //        result = UnPublishRuntime(loadResult.Result, providerType);
        //    else
        //        OASISErrorHandling.HandleError(ref result, $"Error occured in UnPublishRuntime loading the Runtime with the LoadRuntime method, reason: {loadResult.Message}");

        //    return result;
        //}

        //public async Task<OASISResult<IInstalledRuntime>> InstallRuntimeAsync(Guid avatarId, string fullPathToPublishedRuntimeFile, string fullInstallPath, bool createRuntimeDirectory = true, ProviderType providerType = ProviderType.Default)
        //{
        //    OASISResult<IInstalledRuntime> result = new OASISResult<IInstalledRuntime>();
        //    string errorMessage = "Error occured in RuntimeManager.InstallRuntimeAsync. Reason: ";
        //    IRuntimeDNA RuntimeDNA = null;

        //    try
        //    {
        //        OASISResult<IRuntimeDNA> RuntimeDNAResult = await ReadRuntimeDNAAsync(fullPathToPublishedRuntimeFile);

        //        if (RuntimeDNAResult != null && RuntimeDNAResult.Result != null && !RuntimeDNAResult.IsError)
        //        {
        //            //Load the Runtime from the OASIS to make sure the RuntimeDNA is valid (and has not been tampered with).
        //            OASISResult<IRuntime> RuntimeResult = await LoadRuntimeAsync(RuntimeDNAResult.Result.RuntimeId, providerType);

        //            if (RuntimeResult != null && RuntimeResult.Result != null && !RuntimeResult.IsError)
        //            {
        //                //TODO: Not sure if we want to add a check here to compare the RuntimeDNA in the Runtime dir with the one stored in the OASIS?
        //                RuntimeDNA = RuntimeResult.Result.RuntimeDNA;

        //                if (createRuntimeDirectory)
        //                    fullInstallPath = Path.Combine(fullInstallPath, RuntimeDNAResult.Result.RuntimeName);

        //                if (Directory.Exists(fullInstallPath))
        //                    Directory.Delete(fullInstallPath, true);

        //                Directory.CreateDirectory(fullInstallPath);

        //                OnRuntimeInstallStatusChanged?.Invoke(this, new RuntimeInstallStatusEventArgs() { RuntimeDNA = RuntimeDNAResult.Result, Status = Enums.RuntimeInstallStatus.Decompressing });
        //                ZipFile.ExtractToDirectory(fullPathToPublishedRuntimeFile, fullInstallPath, Encoding.Default, true);

        //                OnRuntimeInstallStatusChanged?.Invoke(this, new RuntimeInstallStatusEventArgs() { RuntimeDNA = RuntimeDNAResult.Result, Status = Enums.RuntimeInstallStatus.Installing });
        //                OASISResult<IAvatar> avatarResult = await AvatarManager.Instance.LoadAvatarAsync(avatarId, false, true, providerType);

        //                if (avatarResult != null && !avatarResult.IsError && avatarResult.Result != null)
        //                {
        //                    InstalledRuntime installedRuntime = new InstalledRuntime()
        //                    {
        //                        //RuntimeId = RuntimeDNAResult.Result.RuntimeId,
        //                        RuntimeDNA = RuntimeDNAResult.Result,
        //                        InstalledBy = avatarId,
        //                        InstalledByAvatarUsername = avatarResult.Result.Username,
        //                        InstalledOn = DateTime.Now,
        //                        InstalledPath = fullInstallPath
        //                    };

        //                    OASISResult<IHolon> saveResult = await installedRuntime.SaveAsync();

        //                    if (saveResult != null && saveResult.Result != null && !saveResult.IsError)
        //                    {
        //                        result.Result = installedRuntime;
        //                        RuntimeDNA.RuntimeDownloads++;
        //                        RuntimeResult.Result.RuntimeDNA = RuntimeDNA;

        //                        OASISResult<IRuntime> RuntimeSaveResult = await SaveRuntimeAsync(RuntimeResult.Result, providerType);

        //                        if (RuntimeSaveResult!= null && !RuntimeSaveResult.IsError && RuntimeSaveResult.Result != null)
        //                        {
        //                            if (RuntimeDNAResult.Result.STARODKVersion != OASISBootLoader.OASISBootLoader.STARODKVersion)
        //                                OASISErrorHandling.HandleWarning(ref result, $"The STAR ODK Version {RuntimeDNAResult.Result.STARODKVersion} does not match the current version {OASISBootLoader.OASISBootLoader.STARODKVersion}. This may lead to issues, it is recommended to make sure the versions match.");

        //                            if (RuntimeDNAResult.Result.OASISVersion != OASISBootLoader.OASISBootLoader.OASISVersion)
        //                                OASISErrorHandling.HandleWarning(ref result, $"The OASIS Version {RuntimeDNAResult.Result.OASISVersion} does not match the current version {OASISBootLoader.OASISBootLoader.OASISVersion}. This may lead to issues, it is recommended to make sure the versions match.");

        //                            if (RuntimeDNAResult.Result.COSMICVersion != OASISBootLoader.OASISBootLoader.COSMICVersion)
        //                                OASISErrorHandling.HandleWarning(ref result, $"The COSMIC Version {RuntimeDNAResult.Result.COSMICVersion} does not match the current version {OASISBootLoader.OASISBootLoader.COSMICVersion}. This may lead to issues, it is recommended to make sure the versions match.");

        //                            if (result.InnerMessages.Count > 0)
        //                                result.Message = $"Runtime successfully installed but there were {result.WarningCount} warnings:\n\n {OASISResultHelper.BuildInnerMessageError(result.InnerMessages)}";
        //                            else
        //                                result.Message = "Runtime Successfully Installed";

        //                            OnRuntimeInstallStatusChanged?.Invoke(this, new RuntimeInstallStatusEventArgs() { RuntimeDNA = RuntimeDNAResult.Result, Status = Enums.RuntimeInstallStatus.Installed });
        //                        }
        //                        else
        //                            OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling SaveRuntimeAsync method. Reason: {RuntimeSaveResult.Message}");
        //                    }
        //                    else
        //                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling SaveAsync method. Reason: {saveResult.Message}");
        //                }
        //                else
        //                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadAvatarAsync method. Reason: {avatarResult.Message}");
        //            }
        //            else
        //                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadRuntimeAsync method. Reason: {RuntimeResult.Message}");
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        OASISErrorHandling.HandleError(ref result, $"{errorMessage} {ex}");
        //    }

        //    if (result.IsError)
        //        OnRuntimeInstallStatusChanged?.Invoke(this, new RuntimeInstallStatusEventArgs() { RuntimeDNA = RuntimeDNA, Status = Enums.RuntimeInstallStatus.Error, ErrorMessage = result.Message });

        //    return result;
        //}

        //public OASISResult<IInstalledRuntime> InstallRuntime(Guid avatarId, string fullPathToPublishedRuntimeFile, string fullInstallPath, bool createRuntimeDirectory = true, ProviderType providerType = ProviderType.Default)
        //{
        //    OASISResult<IInstalledRuntime> result = new OASISResult<IInstalledRuntime>();
        //    string errorMessage = "Error occured in RuntimeManager.InstallRuntime. Reason: ";

        //    try
        //    {
        //        ZipFile.ExtractToDirectory(fullPathToPublishedRuntimeFile, fullInstallPath, Encoding.Default, true);
        //        OASISResult<IRuntimeDNA> RuntimeDNAResult = ReadRuntimeDNA(fullInstallPath);

        //        if (RuntimeDNAResult != null && RuntimeDNAResult.Result != null && !RuntimeDNAResult.IsError)
        //        {
        //            OASISResult<IAvatar> avatarResult = AvatarManager.Instance.LoadAvatar(avatarId, false, true, providerType);

        //            if (avatarResult != null && !avatarResult.IsError && avatarResult.Result != null)
        //            {
        //                InstalledRuntime installedRuntime = new InstalledRuntime()
        //                {
        //                    //RuntimeId = RuntimeDNAResult.Result.RuntimeId,
        //                    RuntimeDNA = RuntimeDNAResult.Result,
        //                    InstalledBy = avatarId,
        //                    InstalledByAvatarUsername = avatarResult.Result.Username,
        //                    InstalledOn = DateTime.Now,
        //                    InstalledPath = fullInstallPath
        //                };

        //                OASISResult<IHolon> saveResult = installedRuntime.Save();

        //                if (saveResult != null && saveResult.Result != null && !saveResult.IsError)
        //                {
        //                    result.Result = installedRuntime;

        //                    if (RuntimeDNAResult.Result.STARODKVersion != OASISBootLoader.OASISBootLoader.STARODKVersion)
        //                        OASISErrorHandling.HandleWarning(ref result, $"The STAR ODK Version {RuntimeDNAResult.Result.STARODKVersion} does not match the current version {OASISBootLoader.OASISBootLoader.STARODKVersion}. This may lead to issues, it is recommended to make sure the versions match.");

        //                    if (RuntimeDNAResult.Result.OASISVersion != OASISBootLoader.OASISBootLoader.OASISVersion)
        //                        OASISErrorHandling.HandleWarning(ref result, $"The OASIS Version {RuntimeDNAResult.Result.OASISVersion} does not match the current version {OASISBootLoader.OASISBootLoader.OASISVersion}. This may lead to issues, it is recommended to make sure the versions match.");

        //                    if (RuntimeDNAResult.Result.COSMICVersion != OASISBootLoader.OASISBootLoader.COSMICVersion)
        //                        OASISErrorHandling.HandleWarning(ref result, $"The COSMIC Version {RuntimeDNAResult.Result.COSMICVersion} does not match the current version {OASISBootLoader.OASISBootLoader.COSMICVersion}. This may lead to issues, it is recommended to make sure the versions match.");

        //                    if (result.InnerMessages.Count > 0)
        //                        result.Message = $"Runtime successfully installed but there were {result.WarningCount} warnings:\n\n {OASISResultHelper.BuildInnerMessageError(result.InnerMessages)}";
        //                    else
        //                        result.Message = "Runtime Successfully Installed";
        //                }
        //                else
        //                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling Save method. Reason: {saveResult.Message}");
        //            }
        //            else
        //                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadAvatar method. Reason: {avatarResult.Message}");
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        OASISErrorHandling.HandleError(ref result, $"{errorMessage} {ex}");
        //    }

        //    return result;
        //}

        //public async Task<OASISResult<IInstalledRuntime>> InstallRuntimeAsync(Guid avatarId, IRuntime Runtime, string fullInstallPath, bool createRuntimeDirectory = true, ProviderType providerType = ProviderType.Default)
        //{
        //    OASISResult<IInstalledRuntime> result = new OASISResult<IInstalledRuntime>();
        //    string errorMessage = "Error occured in RuntimeManager.InstallRuntimeAsync. Reason: ";

        //    try
        //    {
        //        string RuntimePath = Path.Combine("temp", Runtime.Name, ".Runtime");

        //        if (Runtime.PublishedRuntime != null)
        //        {
        //            await File.WriteAllBytesAsync(RuntimePath, Runtime.PublishedRuntime);
        //            result = await InstallRuntimeAsync(avatarId, RuntimePath, fullInstallPath, createRuntimeDirectory, providerType);
        //        }
        //        else
        //        {
        //            //OASISErrorHandling.HandleWarning(ref result, "The Runtime.PublishedRuntime property is null! Please make sure this Runtime was published to STARNET and try again.");
        //           // FileStream fileStream = null;

        //            try
        //            {
        //                StorageClient storage = await StorageClient.CreateAsync();

        //                // set minimum chunksize just to see progress updating
        //                var downloadObjectOptions = new DownloadObjectOptions
        //                {
        //                     ChunkSize = UploadObjectOptions.MinimumChunkSize,
        //                };

        //                var progressReporter = new Progress<Google.Apis.Download.IDownloadProgress>(OnDownloadProgress);

        //                using var fileStream = File.OpenWrite(RuntimePath);
        //                _fileLength = fileStream.Length;
        //                _progress = 0;

        //                OnRuntimeInstallStatusChanged?.Invoke(this, new RuntimeInstallStatusEventArgs() { RuntimeDNA = Runtime.RuntimeDNA, Status = Enums.RuntimeInstallStatus.Downloading });
        //                await storage.DownloadObjectAsync("oasis_Runtimes", string.Concat(Runtime.Name, ".Runtime"), fileStream, downloadObjectOptions, progress: progressReporter);
        //                result = await InstallRuntimeAsync(avatarId, RuntimePath, fullInstallPath, createRuntimeDirectory, providerType);
        //            }
        //            catch (Exception ex)
        //            {
        //                OASISErrorHandling.HandleError(ref result, $"An error occured downloading the Runtime from cloud storage. Reason: {ex}");
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        OASISErrorHandling.HandleError(ref result, $"{errorMessage} {ex}");
        //    }

        //    if (result.IsError)
        //        OnRuntimeInstallStatusChanged?.Invoke(this, new RuntimeInstallStatusEventArgs() { RuntimeDNA = Runtime.RuntimeDNA, Status = Enums.RuntimeInstallStatus.Error, ErrorMessage = result.Message });

        //    return result;
        //}

        //public OASISResult<IInstalledRuntime> InstallRuntime(Guid avatarId, IRuntime Runtime, string fullInstallPath, bool createRuntimeDirectory = true, ProviderType providerType = ProviderType.Default)
        //{
        //    OASISResult<IInstalledRuntime> result = new OASISResult<IInstalledRuntime>();
        //    string errorMessage = "Error occured in RuntimeManager.InstallRuntime. Reason: ";

        //    try
        //    {
        //        if (Runtime.PublishedRuntime != null)
        //        {
        //            string RuntimePath = Path.Combine("temp", Runtime.Name, ".Runtime");
        //            File.WriteAllBytes(RuntimePath, Runtime.PublishedRuntime);
        //            result = InstallRuntime(avatarId, RuntimePath, fullInstallPath, createRuntimeDirectory, providerType);
        //        }
        //        else
        //            OASISErrorHandling.HandleError(ref result, "The Runtime.PublishedRuntime property is null! Please make sure this Runtime was published to STARNET and try again.");
        //    }
        //    catch (Exception ex)
        //    {
        //        OASISErrorHandling.HandleError(ref result, $"{errorMessage} {ex}");
        //    }

        //    return result;
        //}

        //public async Task<OASISResult<IInstalledRuntime>> InstallRuntimeAsync(Guid avatarId, Guid RuntimeId, string fullInstallPath, bool createRuntimeDirectory = true, ProviderType providerType = ProviderType.Default)
        //{
        //    OASISResult<IInstalledRuntime> result = new OASISResult<IInstalledRuntime>();
        //    OASISResult<IRuntime> RuntimeResult = await LoadRuntimeAsync(RuntimeId, providerType);

        //    if (RuntimeResult != null && !RuntimeResult.IsError && RuntimeResult.Result != null)
        //        result = await InstallRuntimeAsync(avatarId, RuntimeResult.Result, fullInstallPath, createRuntimeDirectory, providerType);
        //    else
        //        OASISErrorHandling.HandleError(ref result, $"Error occured in RuntimeManager.InstallRuntimeAsync loading the Runtime with the LoadRuntimeAsync method, reason: {result.Message}");

        //    return result;
        //}

        //public OASISResult<IInstalledRuntime> InstallRuntime(Guid avatarId, Guid RuntimeId, string fullInstallPath, bool createRuntimeDirectory = true, ProviderType providerType = ProviderType.Default)
        //{
        //    OASISResult<IInstalledRuntime> result = new OASISResult<IInstalledRuntime>();
        //    OASISResult<IRuntime> RuntimeResult = LoadRuntime(RuntimeId, providerType);

        //    if (RuntimeResult != null && !RuntimeResult.IsError && RuntimeResult.Result != null)
        //        result = InstallRuntime(avatarId, RuntimeResult.Result, fullInstallPath, createRuntimeDirectory, providerType);
        //    else
        //        OASISErrorHandling.HandleError(ref result, $"Error occured in RuntimeManager.InstallRuntime loading the Runtime with the LoadRuntime method, reason: {result.Message}");

        //    return result;
        //}

        //public async Task<OASISResult<IRuntimeDNA>> UnInstallRuntimeAsync(IRuntimeDNA Runtime, Guid avatarId, ProviderType providerType = ProviderType.Default)
        //{
        //    return await UnInstallRuntimeAsync(Runtime.RuntimeId, avatarId, providerType);
        //}

        //public OASISResult<IRuntimeDNA> UnInstallRuntime(IRuntimeDNA Runtime, Guid avatarId, ProviderType providerType = ProviderType.Default)
        //{
        //    return UnInstallRuntime(Runtime.RuntimeId, avatarId, providerType);
        //}

        //public async Task<OASISResult<IRuntimeDNA>> UnInstallRuntimeAsync(Guid RuntimeId, Guid avatarId, ProviderType providerType = ProviderType.Default)
        //{
        //    OASISResult<IRuntimeDNA> result = new OASISResult<IRuntimeDNA>();
        //    OASISResult<IEnumerable<InstalledRuntime>> intalledRuntimeResult = await Data.LoadHolonsForParentAsync<InstalledRuntime>(avatarId, HolonType.InstalledRuntime, false, false, 0, true, false, 0, HolonType.All, 0, providerType);
        //    string errorMessage = "Error occured in RuntimeManager.UnInstallRuntimeAsync. Reason: ";

        //    if (intalledRuntimeResult != null && !intalledRuntimeResult.IsError && intalledRuntimeResult.Result != null)
        //    {
        //        InstalledRuntime installedRuntime = intalledRuntimeResult.Result.FirstOrDefault(x => x.RuntimeDNA.RuntimeId == RuntimeId);

        //        if (installedRuntime != null)
        //        {
        //            OASISResult<IHolon> holonResult = await installedRuntime.DeleteAsync(false, providerType);

        //            if (holonResult != null && !holonResult.IsError && holonResult.Result != null)
        //            {
        //                result.Message = "Runtime Uninstalled";
        //                result.Result = installedRuntime.RuntimeDNA;
        //            }
        //            else
        //                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling DeleteAsync. Reason: {holonResult.Message}");
        //        }
        //        else
        //            OASISErrorHandling.HandleError(ref result, $"{errorMessage} No installed Runtime was found for the Id {RuntimeId}.");
        //    }
        //    else
        //        OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonsForParentAsync. Reason: {intalledRuntimeResult.Message}");

        //    return result;
        //}

        //public OASISResult<IRuntimeDNA> UnInstallRuntime(Guid RuntimeId, Guid avatarId, ProviderType providerType = ProviderType.Default)
        //{
        //    OASISResult<IRuntimeDNA> result = new OASISResult<IRuntimeDNA>();
        //    OASISResult<IEnumerable<InstalledRuntime>> intalledRuntimeResult = Data.LoadHolonsForParent<InstalledRuntime>(avatarId, HolonType.InstalledRuntime, false, false, 0, true, false, 0, HolonType.All, 0, providerType);
        //    string errorMessage = "Error occured in RuntimeManager.UnInstallRuntime. Reason: ";

        //    if (intalledRuntimeResult != null && !intalledRuntimeResult.IsError && intalledRuntimeResult.Result != null)
        //    {
        //        InstalledRuntime installedRuntime = intalledRuntimeResult.Result.FirstOrDefault(x => x.RuntimeDNA.RuntimeId == RuntimeId);

        //        if (installedRuntime != null)
        //        {
        //            OASISResult<IHolon> holonResult = installedRuntime.Delete(false, providerType);

        //            if (holonResult != null && !holonResult.IsError && holonResult.Result != null)
        //            {
        //                result.Message = "Runtime Uninstalled";
        //                result.Result = installedRuntime.RuntimeDNA;
        //            }
        //            else
        //                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling Delete. Reason: {holonResult.Message}");
        //        }
        //        else
        //            OASISErrorHandling.HandleError(ref result, $"{errorMessage} No installed Runtime was found for the Id {RuntimeId}.");
        //    }
        //    else
        //        OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonsForParent. Reason: {intalledRuntimeResult.Message}");

        //    return result;
        //}

        //public async Task<OASISResult<IEnumerable<IInstalledRuntime>>> ListInstalledRuntimesAsync(Guid avatarId, ProviderType providerType = ProviderType.Default)
        //{
        //    OASISResult<IEnumerable<IInstalledRuntime>> result = new OASISResult<IEnumerable<IInstalledRuntime>>();
        //    OASISResult<IEnumerable<InstalledRuntime>> installedRuntimesResult = await Data.LoadHolonsForParentAsync<InstalledRuntime>(avatarId, HolonType.InstalledRuntime, false, false, 0, true, false, 0, HolonType.All, 0, providerType);
        //    string errorMessage = "Error occured in RuntimeManager.ListInstalledRuntimesAsync. Reason: ";

        //    if (installedRuntimesResult != null && !installedRuntimesResult.IsError && installedRuntimesResult.Result != null)
        //    {
        //        result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<IEnumerable<InstalledRuntime>, IEnumerable<IInstalledRuntime>>(installedRuntimesResult);
        //        result.Result = Mapper.Convert<InstalledRuntime, IInstalledRuntime>(installedRuntimesResult.Result);
        //    }
        //    else
        //        OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonsForParentAsync. Reason: {installedRuntimesResult.Message}");

        //    return result;
        //}

        //public OASISResult<IEnumerable<IInstalledRuntime>> ListInstalledRuntimes(Guid avatarId, ProviderType providerType = ProviderType.Default)
        //{
        //    OASISResult<IEnumerable<IInstalledRuntime>> result = new OASISResult<IEnumerable<IInstalledRuntime>>();
        //    OASISResult<IEnumerable<InstalledRuntime>> installedRuntimesResult = Data.LoadHolonsForParent<InstalledRuntime>(avatarId, HolonType.InstalledRuntime, false, false, 0, true, false, 0, HolonType.All, 0, providerType);
        //    string errorMessage = "Error occured in RuntimeManager.ListInstalledRuntimes. Reason: ";

        //    if (installedRuntimesResult != null && !installedRuntimesResult.IsError && installedRuntimesResult.Result != null)
        //    {
        //        result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<IEnumerable<InstalledRuntime>, IEnumerable<IInstalledRuntime>>(installedRuntimesResult);
        //        result.Result = Mapper.Convert<InstalledRuntime, IInstalledRuntime>(installedRuntimesResult.Result);
        //    }
        //    else
        //        OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonsForParent. Reason: {installedRuntimesResult.Message}");

        //    return result;
        //}

        //public async Task<OASISResult<bool>> IsRuntimeInstalledAsync(Guid avatarId, Guid RuntimeId, ProviderType providerType = ProviderType.Default)
        //{
        //    OASISResult<bool> result = new OASISResult<bool>();
        //    OASISResult<IEnumerable<InstalledRuntime>> installedRuntimesResult = await Data.LoadHolonsForParentAsync<InstalledRuntime>(avatarId, HolonType.InstalledRuntime, false, false, 0, true, false, 0, HolonType.All, 0, providerType);
        //    string errorMessage = "Error occured in RuntimeManager.IsRuntimeInstalledAsync. Reason: ";

        //    if (installedRuntimesResult != null && !installedRuntimesResult.IsError && installedRuntimesResult.Result != null)
        //        result.Result = installedRuntimesResult.Result.Any(x => x.RuntimeDNA.RuntimeId == RuntimeId);
        //    else
        //        OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonsForParentAsync. Reason: {installedRuntimesResult.Message}");

        //    return result;
        //}

        //public OASISResult<bool> IsRuntimeInstalled(Guid avatarId, Guid RuntimeId, ProviderType providerType = ProviderType.Default)
        //{
        //    OASISResult<bool> result = new OASISResult<bool>();
        //    OASISResult<IEnumerable<InstalledRuntime>> installedRuntimesResult = Data.LoadHolonsForParent<InstalledRuntime>(avatarId, HolonType.InstalledRuntime, false, false, 0, true, false, 0, HolonType.All, 0, providerType);
        //    string errorMessage = "Error occured in RuntimeManager.IsRuntimeInstalled. Reason: ";

        //    if (installedRuntimesResult != null && !installedRuntimesResult.IsError && installedRuntimesResult.Result != null)
        //        result.Result = installedRuntimesResult.Result.Any(x => x.RuntimeDNA.RuntimeId == RuntimeId);
        //    else
        //        OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonsForParent. Reason: {installedRuntimesResult.Message}");

        //    return result;
        //}

        //public async Task<OASISResult<bool>> IsRuntimeInstalledAsync(Guid avatarId, string RuntimeName, ProviderType providerType = ProviderType.Default)
        //{
        //    OASISResult<bool> result = new OASISResult<bool>();
        //    OASISResult<IEnumerable<InstalledRuntime>> installedRuntimesResult = await Data.LoadHolonsForParentAsync<InstalledRuntime>(avatarId, HolonType.InstalledRuntime, false, false, 0, true, false, 0, HolonType.All, 0, providerType);
        //    string errorMessage = "Error occured in RuntimeManager.IsRuntimeInstalledAsync. Reason: ";

        //    if (installedRuntimesResult != null && !installedRuntimesResult.IsError && installedRuntimesResult.Result != null)
        //        result.Result = installedRuntimesResult.Result.Any(x => x.RuntimeDNA.RuntimeName == RuntimeName);
        //    else
        //        OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonsForParentAsync. Reason: {installedRuntimesResult.Message}");

        //    return result;
        //}

        //public OASISResult<bool> IsRuntimeInstalled(Guid avatarId, string RuntimeName, ProviderType providerType = ProviderType.Default)
        //{
        //    OASISResult<bool> result = new OASISResult<bool>();
        //    string errorMessage = "Error occured in RuntimeManager.IsRuntimeInstalled. Reason: ";
        //    OASISResult<IEnumerable<InstalledRuntime>> installedRuntimesResult = Data.LoadHolonsForParent<InstalledRuntime>(avatarId, HolonType.InstalledRuntime, false, false, 0, true, false, 0, HolonType.All, 0, providerType);

        //    if (installedRuntimesResult != null && !installedRuntimesResult.IsError && installedRuntimesResult.Result != null)
        //        result.Result = installedRuntimesResult.Result.Any(x => x.RuntimeDNA.RuntimeName == RuntimeName);
        //    else
        //        OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonsForParent. Reason: {installedRuntimesResult.Message}");

        //    return result;
        //}

        //public async Task<OASISResult<IInstalledRuntime>> LoadInstalledRuntimeAsync(Guid avatarId, Guid RuntimeId, ProviderType providerType = ProviderType.Default)
        //{
        //    OASISResult<IInstalledRuntime> result = new OASISResult<IInstalledRuntime>();
        //    string errorMessage = "Error occured in RuntimeManager.LoadInstalledRuntimeAsync. Reason: ";
        //    OASISResult<IEnumerable<InstalledRuntime>> installedRuntimesResult = await Data.LoadHolonsForParentAsync<InstalledRuntime>(avatarId, HolonType.InstalledRuntime, false, false, 0, true, false, 0, HolonType.All, 0, providerType);

        //    if (installedRuntimesResult != null && !installedRuntimesResult.IsError && installedRuntimesResult.Result != null)
        //        result.Result = installedRuntimesResult.Result.FirstOrDefault(x => x.RuntimeDNA.RuntimeId == RuntimeId);
        //    else
        //        OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonsForParentAsync. Reason: {installedRuntimesResult.Message}");

        //    return result;
        //}

        //public OASISResult<IInstalledRuntime> LoadInstalledRuntime(Guid avatarId, Guid RuntimeId, ProviderType providerType = ProviderType.Default)
        //{
        //    OASISResult<IInstalledRuntime> result = new OASISResult<IInstalledRuntime>();
        //    string errorMessage = "Error occured in RuntimeManager.LoadInstalledRuntime. Reason: ";
        //    OASISResult<IEnumerable<InstalledRuntime>> installedRuntimesResult = Data.LoadHolonsForParent<InstalledRuntime>(avatarId, HolonType.InstalledRuntime, false, false, 0, true, false, 0, HolonType.All, 0, providerType);

        //    if (installedRuntimesResult != null && !installedRuntimesResult.IsError && installedRuntimesResult.Result != null)
        //        result.Result = installedRuntimesResult.Result.FirstOrDefault(x => x.RuntimeDNA.RuntimeId == RuntimeId);
        //    else
        //        OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonsForParent. Reason: {installedRuntimesResult.Message}");

        //    return result;
        //}

        //public async Task<OASISResult<IInstalledRuntime>> LoadInstalledRuntimeAsync(Guid avatarId, string RuntimeName, ProviderType providerType = ProviderType.Default)
        //{
        //    OASISResult<IInstalledRuntime> result = new OASISResult<IInstalledRuntime>();
        //    string errorMessage = "Error occured in RuntimeManager.LoadInstalledRuntimeAsync. Reason: ";
        //    OASISResult<IEnumerable<InstalledRuntime>> installedRuntimesResult = await Data.LoadHolonsForParentAsync<InstalledRuntime>(avatarId, HolonType.InstalledRuntime, false, false, 0, true, false, 0, HolonType.All, 0, providerType);

        //    if (installedRuntimesResult != null && !installedRuntimesResult.IsError && installedRuntimesResult.Result != null)
        //        result.Result = installedRuntimesResult.Result.FirstOrDefault(x => x.RuntimeDNA.RuntimeName == RuntimeName);
        //    else
        //        OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonsForParentAsync. Reason: {installedRuntimesResult.Message}");

        //    return result;
        //}

        //public OASISResult<IInstalledRuntime> LoadInstalledRuntime(Guid avatarId, string RuntimeName, ProviderType providerType = ProviderType.Default)
        //{
        //    OASISResult<IInstalledRuntime> result = new OASISResult<IInstalledRuntime>();
        //    string errorMessage = "Error occured in RuntimeManager.LoadInstalledRuntime. Reason: ";
        //    OASISResult<IEnumerable<InstalledRuntime>> installedRuntimesResult = Data.LoadHolonsForParent<InstalledRuntime>(avatarId, HolonType.InstalledRuntime, false, false, 0, true, false, 0, HolonType.All, 0, providerType);

        //    if (installedRuntimesResult != null && !installedRuntimesResult.IsError && installedRuntimesResult.Result != null)
        //        result.Result = installedRuntimesResult.Result.FirstOrDefault(x => x.RuntimeDNA.RuntimeName == RuntimeName);
        //    else
        //        OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonsForParent. Reason: {installedRuntimesResult.Message}");

        //    return result;
        //}

        //public async Task<OASISResult<IInstalledRuntime>> LaunchRuntimeAsync(Guid avatarId, Guid RuntimeId, ProviderType providerType = ProviderType.Default)
        //{
        //    OASISResult<IInstalledRuntime> result = new OASISResult<IInstalledRuntime>();
        //    result = await LoadInstalledRuntimeAsync(avatarId, RuntimeId);

        //    if (result != null && !result.IsError && result.Result != null)
        //    {
        //        //Process.Start("explorer.exe", Path.Combine(result.Result.InstalledPath, result.Result.RuntimeDNA.LaunchTarget));
        //        Process.Start("dotnet.exe", Path.Combine(result.Result.InstalledPath, result.Result.RuntimeDNA.LaunchTarget));
        //    }
        //    else
        //        OASISErrorHandling.HandleError(ref result, $"Error occured in RuntimeManager.LaunchRuntimeAsync loading the Runtime with the LoadInstalledRuntimeAsync method, reason: {result.Message}");

        //    return result;
        //}

        //public OASISResult<IInstalledRuntime> LaunchRuntime(Guid avatarId, Guid RuntimeId, ProviderType providerType = ProviderType.Default)
        //{
        //    OASISResult<IInstalledRuntime> result = new OASISResult<IInstalledRuntime>();
        //    result = LoadInstalledRuntime(avatarId, RuntimeId);

        //    if (result != null && !result.IsError && result.Result != null)
        //    {
        //        //Process.Start("explorer.exe", Path.Combine(result.Result.InstalledPath, result.Result.RuntimeDNA.LaunchTarget));
        //        Process.Start("dotnet.exe", Path.Combine(result.Result.InstalledPath, result.Result.RuntimeDNA.LaunchTarget));
        //    }
        //    else
        //        OASISErrorHandling.HandleError(ref result, $"Error occured in RuntimeManager.LaunchRuntime loading the Runtime with the LoadInstalledRuntime method, reason: {result.Message}");

        //    return result;
        //}


        ////private IRuntimeDNA ConvertRuntimeToRuntimeDNA(IRuntime Runtime)
        ////{
        ////    RuntimeDNA RuntimeDNA = new RuntimeDNA()
        ////    {
        ////        CelestialBodyId = Runtime.CelestialBodyId,
        ////        //CelestialBody = Runtime.CelestialBody,
        ////        CelestialBodyName = Runtime.CelestialBody != null ? Runtime.CelestialBody.Name : "",
        ////        CelestialBodyType = Runtime.CelestialBody != null ? Runtime.CelestialBody.HolonType : HolonType.None,
        ////        CreatedByAvatarId = Runtime.CreatedByAvatarId,
        ////        CreatedByAvatarUsername = Runtime.CreatedByAvatarUsername,
        ////        CreatedOn = Runtime.CreatedDate,
        ////        Description = Runtime.Description,
        ////        GenesisType = Runtime.GenesisType,
        ////        RuntimeId = Runtime.Id,
        ////        RuntimeName = Runtime.Name,
        ////        RuntimeType = Runtime.RuntimeType,
        ////        PublishedByAvatarId = Runtime.PublishedByAvatarId,
        ////        PublishedByAvatarUsername = Runtime.PublishedByAvatarUsername,
        ////        PublishedOn = Runtime.PublishedOn,
        ////        PublishedOnSTARNET = Runtime.PublishedRuntime != null,
        ////        Version = Runtime.Version.ToString()
        ////    };

        ////    List<IZome> zomes = new List<IZome>();
        ////    foreach (IHolon holon in Runtime.Children)
        ////        zomes.Add((IZome)holon);

        ////   //RuntimeDNA.Zomes = zomes;
        ////    return RuntimeDNA;
        ////}

        //public async Task<OASISResult<bool>> WriteRuntimeDNAAsync(IRuntimeDNA RuntimeDNA, string fullPathToRuntime)
        //{
        //    OASISResult<bool> result = new OASISResult<bool>();

        //    try
        //    {
        //        JsonSerializerOptions options = new()
        //        {
        //            ReferenceHandler = ReferenceHandler.Preserve,
        //            WriteIndented = true
        //        };

        //        if (!Directory.Exists(fullPathToRuntime))
        //            Directory.CreateDirectory(fullPathToRuntime);

        //        await File.WriteAllTextAsync(Path.Combine(fullPathToRuntime, "RuntimeDNA.json"), JsonSerializer.Serialize((RuntimeDNA)RuntimeDNA, options));
        //        result.Result = true;
        //    }
        //    catch (Exception ex)
        //    {
        //        OASISErrorHandling.HandleError(ref result, $"An error occured writing the RuntimeDNA in WriteRuntimeDNAAsync: Reason: {ex.Message}");
        //    }

        //    return result;
        //}

        //public OASISResult<bool> WriteRuntimeDNA(IRuntimeDNA RuntimeDNA, string fullPathToRuntime)
        //{
        //    OASISResult<bool> result = new OASISResult<bool>();

        //    try
        //    {
        //        if (!Directory.Exists(fullPathToRuntime))
        //            Directory.CreateDirectory(fullPathToRuntime);

        //        File.WriteAllText(Path.Combine(fullPathToRuntime, "RuntimeDNA.json"), JsonSerializer.Serialize((RuntimeDNA)RuntimeDNA));
        //        result.Result = true;
        //    }
        //    catch (Exception ex)
        //    {
        //        OASISErrorHandling.HandleError(ref result, $"An error occured writing the RuntimeDNA in WriteRuntimeDNA: Reason: {ex.Message}");
        //    }

        //    return result;
        //}

        //public async Task<OASISResult<IRuntimeDNA>> ReadRuntimeDNAAsync(string fullPathToRuntime)
        //{
        //    OASISResult<IRuntimeDNA> result = new OASISResult<IRuntimeDNA>();

        //    try
        //    {
        //        result.Result = JsonSerializer.Deserialize<RuntimeDNA>(await File.ReadAllTextAsync(Path.Combine(fullPathToRuntime, "RuntimeDNA.json")));
        //    }
        //    catch (Exception ex)
        //    {
        //        OASISErrorHandling.HandleError(ref result, $"An error occured reading the RuntimeDNA in ReadRuntimeDNAAsync: Reason: {ex.Message}");
        //    }

        //    return result;
        //}

        //public OASISResult<IRuntimeDNA> ReadRuntimeDNA(string fullPathToRuntime)
        //{
        //    OASISResult<IRuntimeDNA> result = new OASISResult<IRuntimeDNA>();

        //    try
        //    {
        //        result.Result = JsonSerializer.Deserialize<RuntimeDNA>(File.ReadAllText(Path.Combine(fullPathToRuntime, "RuntimeDNA.json")));
        //    }
        //    catch (Exception ex)
        //    {
        //        OASISErrorHandling.HandleError(ref result, $"An error occured reading the RuntimeDNA in ReadRuntimeDNA: Reason: {ex.Message}");
        //    }

        //    return result;
        //}

        //public async Task<OASISResult<IRuntime>> AddMissionToRuntimeAsync(Guid RuntimeId, IMission mission)
        //{
        //    OASISResult<IRuntime> result = new OASISResult<IRuntime>();
        //    OASISResult<IRuntime> loadResult = await LoadRuntimeAsync(RuntimeId);

        //    if (loadResult != null && loadResult.Result != null && !loadResult.IsError)
        //    {
        //        loadResult.Result.Missions.Add(mission);
        //        result = await SaveRuntimeAsync(loadResult.Result);
        //    }
        //    else
        //        OASISErrorHandling.HandleError(ref result, $"An error occured in RuntimeManager.AddMissionToRuntimeAsync calling LoadRuntimeAsync. Reason: {loadResult.Message}");

        //    return result;
        //}

        //public OASISResult<IRuntime> AddMissionToRuntime(Guid RuntimeId, IMission mission)
        //{
        //    OASISResult<IRuntime> result = new OASISResult<IRuntime>();
        //    OASISResult<IRuntime> loadResult = LoadRuntime(RuntimeId);

        //    if (loadResult != null && loadResult.Result != null && !loadResult.IsError)
        //    {
        //        loadResult.Result.Missions.Add(mission);
        //        result = SaveRuntime(loadResult.Result);
        //    }
        //    else
        //        OASISErrorHandling.HandleError(ref result, $"An error occured in RuntimeManager.AddMissionToRuntime calling LoadRuntime. Reason: {loadResult.Message}");

        //    return result;
        //}

        //public async Task<OASISResult<IRuntime>> RemoveMissionFromRuntimeAsync(Guid RuntimeId, IMission mission)
        //{
        //    OASISResult<IRuntime> result = new OASISResult<IRuntime>();
        //    OASISResult<IRuntime> loadResult = await LoadRuntimeAsync(RuntimeId);

        //    if (loadResult != null && loadResult.Result != null && !loadResult.IsError)
        //    {
        //        loadResult.Result.Missions.Remove(mission);
        //        result = await SaveRuntimeAsync(loadResult.Result);
        //    }
        //    else
        //        OASISErrorHandling.HandleError(ref result, $"An error occured in RuntimeManager.RemoveMissionFromRuntimeAsync calling LoadRuntimeAsync. Reason: {loadResult.Message}");

        //    return result;
        //}

        //public OASISResult<IRuntime> RemoveMissionFromRuntime(Guid RuntimeId, IMission mission)
        //{
        //    OASISResult<IRuntime> result = new OASISResult<IRuntime>();
        //    OASISResult<IRuntime> loadResult = LoadRuntime(RuntimeId);

        //    if (loadResult != null && loadResult.Result != null && !loadResult.IsError)
        //    {
        //        loadResult.Result.Missions.Remove(mission);
        //        result = SaveRuntime(loadResult.Result);
        //    }
        //    else
        //        OASISErrorHandling.HandleError(ref result, $"An error occured in RuntimeManager.RemoveMissionFromRuntime calling LoadRuntime. Reason: {loadResult.Message}");

        //    return result;
        //}

        //public async Task<OASISResult<IRuntime>> RemoveMissionFromRuntimeAsync(Guid RuntimeId, Guid missionId)
        //{
        //    OASISResult<IRuntime> result = new OASISResult<IRuntime>();
        //    OASISResult<IRuntime> loadResult = await LoadRuntimeAsync(RuntimeId);
        //    string errorMessage = "An error occured in RuntimeManager.RemoveMissionFromRuntimeAsync. Reason:";

        //    if (loadResult != null && loadResult.Result != null && !loadResult.IsError)
        //    {
        //        IMission mission = loadResult.Result.Missions.FirstOrDefault(x => x.Id == missionId);

        //        if (mission != null)
        //        {
        //            loadResult.Result.Missions.Remove(mission);
        //            result = await SaveRuntimeAsync(loadResult.Result);
        //        }
        //        else
        //            OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error calling LoadRuntimeAsync. Reason: {loadResult.Message}");
        //    }
        //    else
        //        OASISErrorHandling.HandleError(ref result, $"{errorMessage} No mission could be found for the missionId {missionId}");

        //    return result;
        //}

        //public OASISResult<IRuntime> RemoveMissionFromRuntime(Guid RuntimeId, Guid missionId)
        //{
        //    OASISResult<IRuntime> result = new OASISResult<IRuntime>();
        //    OASISResult<IRuntime> loadResult = LoadRuntime(RuntimeId);
        //    string errorMessage = "An error occured in RuntimeManager.RemoveMissionFromRuntime. Reason:";

        //    if (loadResult != null && loadResult.Result != null && !loadResult.IsError)
        //    {
        //        IMission mission = loadResult.Result.Missions.FirstOrDefault(x => x.Id == missionId);

        //        if (mission != null)
        //        {
        //            loadResult.Result.Missions.Remove(mission);
        //            result = SaveRuntime(loadResult.Result);
        //        }
        //        else
        //            OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error calling LoadRuntime. Reason: {loadResult.Message}");
        //    }
        //    else
        //        OASISErrorHandling.HandleError(ref result, $"{errorMessage} No mission could be found for the missionId {missionId}");

        //    return result;
        //}

        //public async Task<OASISResult<IList<IMission>>> GetRuntimeMissionsAsync(Guid RuntimeId)
        //{
        //    OASISResult<IList<IMission>> result = new OASISResult<IList<IMission>>();
        //    OASISResult<IRuntime> loadResult = await LoadRuntimeAsync(RuntimeId);

        //    if (loadResult != null && loadResult.Result != null && !loadResult.IsError)
        //    {
        //        result.Result = loadResult.Result.Missions;
        //        result.IsLoaded = true;
        //        result.Message = "Missions Loaded Successfully";
        //    }
        //    else
        //        OASISErrorHandling.HandleError(ref result, $"An error occured in RuntimeManager.GetRuntimeMissions calling LoadRuntimeAsync. Reason: {loadResult.Message}");

        //    return result;
        //}

        //public OASISResult<IList<IMission>> GetRuntimeMissions(Guid RuntimeId)
        //{
        //    OASISResult<IList<IMission>> result = new OASISResult<IList<IMission>>();
        //    OASISResult<IRuntime> loadResult = LoadRuntime(RuntimeId);

        //    if (loadResult != null && loadResult.Result != null && !loadResult.IsError)
        //    {
        //        result.Result = loadResult.Result.Missions;
        //        result.IsLoaded = true;
        //        result.Message = "Missions Loaded Successfully";
        //    }
        //    else
        //        OASISErrorHandling.HandleError(ref result, $"An error occured in RuntimeManager.GetRuntimeMissions calling LoadRuntime. Reason: {loadResult.Message}");

        //    return result;
        //}

        private void OnUploadProgress(Google.Apis.Upload.IUploadProgress progress)
        {
            switch (progress.Status)
            {
                case Google.Apis.Upload.UploadStatus.NotStarted:
                    _progress = 0;
                    OnRuntimeUploadStatusChanged?.Invoke(this, new RuntimeUploadProgressEventArgs() { Progress = _progress, Status = Enums.RuntimeUploadStatus.NotStarted });
                    break;

                case Google.Apis.Upload.UploadStatus.Starting:
                    _progress = 0;
                    OnRuntimeUploadStatusChanged?.Invoke(this, new RuntimeUploadProgressEventArgs() { Progress = _progress, Status = Enums.RuntimeUploadStatus.Uploading });
                    break;

                case Google.Apis.Upload.UploadStatus.Completed:
                    _progress = 100;
                    OnRuntimeUploadStatusChanged?.Invoke(this, new RuntimeUploadProgressEventArgs() { Progress = _progress, Status = Enums.RuntimeUploadStatus.Uploaded });
                    break;

                case Google.Apis.Upload.UploadStatus.Uploading:
                    _progress = Convert.ToInt32(((double)progress.BytesSent / (double)_fileLength) * 100);
                    OnRuntimeUploadStatusChanged?.Invoke(this, new RuntimeUploadProgressEventArgs() { Progress = _progress, Status = Enums.RuntimeUploadStatus.Uploading });
                    break;

                case Google.Apis.Upload.UploadStatus.Failed:
                    OnRuntimeUploadStatusChanged?.Invoke(this, new RuntimeUploadProgressEventArgs() { Progress = _progress, Status = Enums.RuntimeUploadStatus.Error, ErrorMessage = progress.Exception.ToString() });
                    break;
            }
        }

        private void OnDownloadProgress(Google.Apis.Download.IDownloadProgress progress)
        {
            switch (progress.Status)
            {
                case Google.Apis.Download.DownloadStatus.NotStarted:
                    _progress = 0;
                    OnRuntimeDownloadStatusChanged?.Invoke(this, new RuntimeDownloadProgressEventArgs() { Progress = _progress, Status = Enums.RuntimeDownloadStatus.NotStarted });
                    break;

                case Google.Apis.Download.DownloadStatus.Completed:
                    _progress = 100;
                    OnRuntimeDownloadStatusChanged?.Invoke(this, new RuntimeDownloadProgressEventArgs() { Progress = _progress, Status = Enums.RuntimeDownloadStatus.Downloaded });
                    break;

                case Google.Apis.Download.DownloadStatus.Downloading:
                    _progress = Convert.ToInt32((progress.BytesDownloaded / _fileLength) * 100);
                    OnRuntimeDownloadStatusChanged?.Invoke(this, new RuntimeDownloadProgressEventArgs() { Progress = _progress, Status = Enums.RuntimeDownloadStatus.Downloading });
                    break;

                case Google.Apis.Download.DownloadStatus.Failed:
                    OnRuntimeDownloadStatusChanged?.Invoke(this, new RuntimeDownloadProgressEventArgs() { Progress = _progress, Status = Enums.RuntimeDownloadStatus.Error, ErrorMessage = progress.Exception.ToString()});
                    break;
            }
        }
    }
}