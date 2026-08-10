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
        public virtual async Task<OASISResult<T1>> PublishAsync(Guid avatarId, string fullPathToSource, string launchTarget, string fullPathToPublishTo = "", bool edit = false, bool registerOnSTARNET = true, bool generateBinary = true, bool uploadToCloud = true, ProviderType providerType = ProviderType.Default, ProviderType binaryProviderType = ProviderType.IPFSOASIS, bool embedRuntimes = false, bool embedLibs = false, bool embedTemplates = false)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            T4 STARNETDNA = default;
            string errorMessage = "Error occured in STARNETManagerBase.PublishAsync. Reason:";

            OASISResult<T1> validateResult = await BeginPublishAsync(avatarId, fullPathToSource, fullPathToPublishTo, launchTarget, edit, providerType);

            if (validateResult != null && validateResult.Result != null && !validateResult.IsError)
            {
                STARNETDNA = (T4)validateResult.Result.STARNETDNA;
                string publishedFileName = string.Concat(STARNETDNA.Name, "_v", STARNETDNA.Version, ".", STARNETHolonFileExtention);

                STARNETDNA.PublishedOnSTARNET = registerOnSTARNET && (binaryProviderType != ProviderType.None || uploadToCloud);

                if (generateBinary)
                {
                    STARNETDNA.PublishedPath = Path.Combine(fullPathToPublishTo, publishedFileName);
                    STARNETDNA.PublishedToCloud = registerOnSTARNET && uploadToCloud;
                    STARNETDNA.PublishedProviderType = Enum.GetName(typeof(ProviderType), binaryProviderType);
                }

                WriteDNA(STARNETDNA, fullPathToSource);
                OnPublishStatusChanged?.Invoke(this, new STARNETHolonPublishStatusEventArgs() { STARNETDNA = STARNETDNA, Status = STARNETHolonPublishStatus.Compressing });

                if (generateBinary)
                {
                    string publishedPath = Path.Combine(fullPathToPublishTo, "Published Temp", string.Concat(STARNETDNA.Name, "_v", STARNETDNA.Version));

                    try
                    {
                        if (Directory.Exists(publishedPath))
                            Directory.Delete(publishedPath, true);

                        Directory.CreateDirectory(publishedPath);
                        DirectoryHelper.CopyFilesRecursively(fullPathToSource, publishedPath);

                        if (!embedRuntimes && Directory.Exists(Path.Combine(publishedPath, "Dependencies", "STARNET", "Runtimes")))
                            Directory.Delete(Path.Combine(publishedPath, "Dependencies", "STARNET", "Runtimes"), true);

                        if (!embedTemplates && Directory.Exists(Path.Combine(publishedPath, "Dependencies", "STARNET", "Templates")))
                            Directory.Delete(Path.Combine(publishedPath, "Dependencies", "STARNET", "Templates"), true);

                        if (!embedLibs && Directory.Exists(Path.Combine(publishedPath, "Dependencies", "STARNET", "Libs")))
                            Directory.Delete(Path.Combine(publishedPath, "Dependencies", "STARNET", "Libs"), true);

                        OASISResult<bool> compressedResult = GenerateCompressedFile(publishedPath, STARNETDNA.PublishedPath);

                        if (!(compressedResult != null && compressedResult.Result != null && !compressedResult.IsError))
                        {
                            result.Message = compressedResult.Message;
                            result.IsError = true;
                            return result;
                        }

                        //TODO: Currently the filesize will NOT be in the compressed .STARNETHolon file because we dont know the size before we create it! ;-) We would need to compress it twice or edit the compressed file after to update the STARNETDNA inside it...
                        if (!string.IsNullOrEmpty(STARNETDNA.PublishedPath) && File.Exists(STARNETDNA.PublishedPath))
                            STARNETDNA.FileSize = new FileInfo(STARNETDNA.PublishedPath).Length;
                    }
                    catch (Exception e)
                    {
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured attempting to compress the {STARNETHolonUIName} files. Reason: {e}");
                        return result;
                    }
                    finally
                    {
                        if (Directory.Exists(Path.Combine(fullPathToPublishTo, "Published Temp")))
                            Directory.Delete(Path.Combine(fullPathToPublishTo, "Published Temp"), true);
                    }
                }

                WriteDNA(STARNETDNA, fullPathToSource);
                validateResult.Result.STARNETDNA = STARNETDNA;

                if (registerOnSTARNET)
                {
                    if (uploadToCloud)
                    {
                        OASISResult<bool> uploadToCloudResult = await UploadToCloudAsync(STARNETDNA, publishedFileName, registerOnSTARNET, binaryProviderType);

                        if (!(uploadToCloudResult != null && uploadToCloudResult.Result && !uploadToCloudResult.IsError))
                            OASISErrorHandling.HandleWarning(ref result, $" Error occured calling UploadToCloudAsync. Reason: {uploadToCloudResult.Message}");
                    }

                    if (binaryProviderType != ProviderType.None)
                    {
                        OASISResult<T1> uploadToOASISResult = await UploadToOASISAsync(avatarId, STARNETDNA, STARNETDNA.PublishedPath, registerOnSTARNET, uploadToCloud, binaryProviderType);

                        if (uploadToOASISResult != null && uploadToOASISResult.Result != null && !uploadToOASISResult.IsError)
                            result.Result = uploadToOASISResult.Result;
                        else
                            OASISErrorHandling.HandleWarning(ref result, $" Error occured calling UploadToOASISAsync. Reason: {uploadToOASISResult.Message}");
                    }
                    else
                        STARNETDNA.PublishedProviderType = Enum.GetName(typeof(ProviderType), ProviderType.None);
                }

                OASISResult<T1> finalResult = await FininalizePublishAsync(avatarId, validateResult.Result, edit, providerType);
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(finalResult, result);
                result.Result = finalResult.Result;
            }
            else
            {
                if (validateResult.Message.Contains(STARNETDNAFileName))
                {
                    result.Message = validateResult.Message;
                    result.IsError = validateResult.IsError;
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured in BeginPublishAsync. Reason: {validateResult.Message}");
            }

            return result;
        }

        public OASISResult<T1> Publish(Guid avatarId, string fullPathToSource, string launchTarget, string fullPathToPublishTo = "", bool edit = false, bool registerOnSTARNET = true, bool generateBinary = true, bool uploadToCloud = true, ProviderType providerType = ProviderType.Default, ProviderType binaryProviderType = ProviderType.IPFSOASIS, bool embedRuntimes = false, bool embedLibs = false, bool embedTemplates = false)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            T4 STARNETDNA = default;
            string errorMessage = "Error occured in STARNETManagerBase.PublishAsync. Reason:";

            OASISResult<T1> validateResult = BeginPublish(avatarId, fullPathToSource, fullPathToPublishTo, launchTarget, edit, providerType);

            if (validateResult != null && validateResult.Result != null && !validateResult.IsError)
            {
                STARNETDNA = (T4)validateResult.Result.STARNETDNA;
                string publishedFileName = string.Concat(STARNETDNA.Name, "_v", STARNETDNA.Version, ".", STARNETHolonFileExtention);

                STARNETDNA.PublishedOnSTARNET = registerOnSTARNET && (binaryProviderType != ProviderType.None || uploadToCloud);

                if (generateBinary)
                {
                    STARNETDNA.PublishedPath = Path.Combine(fullPathToPublishTo, publishedFileName);
                    STARNETDNA.PublishedToCloud = registerOnSTARNET && uploadToCloud;
                    STARNETDNA.PublishedProviderType = Enum.GetName(typeof(ProviderType), binaryProviderType);
                }

                WriteDNA(STARNETDNA, fullPathToSource);
                OnPublishStatusChanged?.Invoke(this, new STARNETHolonPublishStatusEventArgs() { STARNETDNA = STARNETDNA, Status = STARNETHolonPublishStatus.Compressing });

                if (generateBinary)
                {
                    OASISResult<bool> compressedResult = GenerateCompressedFile(fullPathToSource, STARNETDNA.PublishedPath);

                    if (!(compressedResult != null && compressedResult.Result != null && !compressedResult.IsError))
                    {
                        result.Message = compressedResult.Message;
                        result.IsError = true;
                        return result;
                    }
                }

                //TODO: Currently the filesize will NOT be in the compressed .STARNETHolon file because we dont know the size before we create it! ;-) We would need to compress it twice or edit the compressed file after to update the STARNETDNA inside it...
                if (!string.IsNullOrEmpty(STARNETDNA.PublishedPath) && File.Exists(STARNETDNA.PublishedPath))
                    STARNETDNA.FileSize = new FileInfo(STARNETDNA.PublishedPath).Length;

                WriteDNA(STARNETDNA, fullPathToSource);
                validateResult.Result.STARNETDNA = STARNETDNA;

                if (registerOnSTARNET)
                {
                    if (uploadToCloud)
                    {
                        OASISResult<bool> uploadToCloudResult = UploadToCloud(STARNETDNA, publishedFileName, registerOnSTARNET, binaryProviderType);

                        if (!(uploadToCloudResult != null && uploadToCloudResult.Result && !uploadToCloudResult.IsError))
                            OASISErrorHandling.HandleWarning(ref result, $" Error occured calling UploadToCloud. Reason: {uploadToCloudResult.Message}");
                    }

                    if (binaryProviderType != ProviderType.None)
                    {
                        OASISResult<T1> uploadToOASISResult = UploadToOASIS(avatarId, STARNETDNA, STARNETDNA.PublishedPath, registerOnSTARNET, uploadToCloud, binaryProviderType);

                        if (uploadToOASISResult != null && uploadToOASISResult.Result != null && !uploadToOASISResult.IsError)
                            result.Result = uploadToOASISResult.Result;
                        else
                            OASISErrorHandling.HandleWarning(ref result, $" Error occured calling UploadToOASIS. Reason: {uploadToOASISResult.Message}");
                    }
                    else
                        STARNETDNA.PublishedProviderType = Enum.GetName(typeof(ProviderType), ProviderType.None);
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured in BeginPublish. Reason: {validateResult.Message}");


                OASISResult<T1> finalResult = FininalizePublish(avatarId, validateResult.Result, edit, providerType);
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(finalResult, result);
                result.Result = finalResult.Result;
            }

            return result;
        }

        public OASISResult<bool> GenerateCompressedFile(string sourcePath, string destinationPath)
        {
            OASISResult<bool> result = new OASISResult<bool>();

            try
            {
                if (File.Exists(destinationPath))
                    File.Delete(destinationPath);

                ZipFile.CreateFromDirectory(sourcePath, destinationPath);
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, sourcePath + " could not be compressed to " + destinationPath + ". Reason: " + e.Message);
            }

            return result;
        }

        //TODO: Come back to this, was going to call this for publishing and installing to make sure the DNA hadn't been changed on the disk, but maybe we want to allow this? Not sure, needs more thought...
        //private async OASISResult<bool> IsSTARNETDNAValidAsync(T4 STARNETDNA)
        //{
        //    OASISResult<ISTARNETHolon> STARNETHolonResult = await LoadSTARNETHolonAsync(STARNETDNA.STARNETHolonId);

        //    if (STARNETHolonResult != null && STARNETHolonResult.Result != null && !STARNETHolonResult.IsError)
        //    {
        //        T4 originalDNA =  JsonSerializer.Deserialize<T4>(STARNETHolonResult.Result.MetaData["STARNETDNA"].ToString());

        //        if (originalDNA != null)
        //        {
        //            if (originalDNA.GenesisType != STARNETDNA.GenesisType ||
        //                originalDNA.STARNETHolonType != STARNETDNA.STARNETHolonType ||
        //                originalDNA.CelestialBodyType != STARNETDNA.CelestialBodyType ||
        //                originalDNA.CelestialBodyId != STARNETDNA.CelestialBodyId ||
        //                originalDNA.CelestialBodyName != STARNETDNA.CelestialBodyName ||
        //                originalDNA.CreatedByAvatarId != STARNETDNA.CreatedByAvatarId ||
        //                originalDNA.CreatedByAvatarUsername != STARNETDNA.CreatedByAvatarUsername ||
        //                originalDNA.CreatedOn != STARNETDNA.CreatedOn ||
        //                originalDNA.Description != STARNETDNA.Description ||
        //                originalDNA.IsActive != STARNETDNA.IsActive ||
        //                originalDNA.LaunchTarget != STARNETDNA.LaunchTarget ||
        //                originalDNA. != STARNETDNA.LaunchTarget ||

        //        }
        //    }
        //}

        public virtual async Task<OASISResult<T1>> BeginPublishAsync(Guid avatarId, string fullPathToSource, string fullPathToPublishTo, string launchTarget, bool edit, ProviderType providerType)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            string userName = "Unknown";
            string errorMessage = "Error occured in STARNETManagerBase.BeginPublishAsync. Reason:";

            try
            {
                OASISResult<T4> readSTARNETDNAResult = await ReadDNAFromSourceOrInstallFolderAsync<T4>(fullPathToSource);

                if (readSTARNETDNAResult != null && !readSTARNETDNAResult.IsError && readSTARNETDNAResult.Result != null)
                {
                    //OAPPDNA = readSTARNETDNAResult.Result;
                    OnPublishStatusChanged?.Invoke(this, new STARNETHolonPublishStatusEventArgs() { STARNETDNA = readSTARNETDNAResult.Result, Status = STARNETHolonPublishStatus.Packaging });
                    OASISResult<IAvatar> loadAvatarResult = await AvatarManager.Instance.LoadAvatarAsync(avatarId, false, true, providerType);

                    if (loadAvatarResult != null && loadAvatarResult.Result != null && !loadAvatarResult.IsError)
                    {
                        userName = loadAvatarResult.Result.Username;

                        //Load latest version.
                        OASISResult<T1> loadOAPPResult = await LoadAsync(avatarId, readSTARNETDNAResult.Result.Id);

                        if (loadOAPPResult != null && loadOAPPResult.Result != null && !loadOAPPResult.IsError)
                        {
                            if (loadOAPPResult.Result.STARNETDNA.CreatedByAvatarId == avatarId)
                            {
                                OASISResult<bool> validateVersionResult = ValidateVersion(readSTARNETDNAResult.Result.Version, loadOAPPResult.Result.STARNETDNA.Version, fullPathToSource, loadOAPPResult.Result.STARNETDNA.PublishedOn == DateTime.MinValue, edit);

                                if (validateVersionResult != null && validateVersionResult.Result && !validateVersionResult.IsError)
                                {
                                    //TODO: Maybe add check to make sure the DNA has not been tampered with?
                                    loadOAPPResult.Result.STARNETDNA.Version = readSTARNETDNAResult.Result.Version; //Set the new version set in the DNA (JSON file).
                                    T4 STARNETDNA = (T4)loadOAPPResult.Result.STARNETDNA; //Make sure it has not been tampered with by using the stored version.

                                    if (!edit)
                                    {
                                        STARNETDNA.VersionSequence++;
                                        STARNETDNA.NumberOfVersions++;
                                    }

                                    STARNETDNA.LaunchTarget = launchTarget;
                                    result.Result = loadOAPPResult.Result;

                                    if (string.IsNullOrEmpty(fullPathToPublishTo))
                                        fullPathToPublishTo = Path.Combine(fullPathToSource, "Published");

                                    if (!Directory.Exists(fullPathToPublishTo))
                                        Directory.CreateDirectory(fullPathToPublishTo);

                                    if (!edit)
                                    {
                                        STARNETDNA.PublishedOn = DateTime.Now;
                                        STARNETDNA.PublishedByAvatarId = avatarId;
                                        STARNETDNA.PublishedByAvatarUsername = userName;
                                    }
                                    else
                                    {
                                        STARNETDNA.ModifiedOn = DateTime.Now;
                                        STARNETDNA.ModifiedByAvatarId = avatarId;
                                        STARNETDNA.ModifiedByAvatarUsername = userName;
                                    }

                                    result.Result.STARNETDNA = STARNETDNA;
                                }
                                else
                                {
                                    if (validateVersionResult.Message.Contains(STARNETDNAFileName))
                                    {
                                        result.Message = validateVersionResult.Message;
                                        result.IsError = validateVersionResult.IsError;
                                    }
                                    else
                                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured in ValidateVersion. Reason: {validateVersionResult.Message}");
                                }
                            }
                            else
                                OASISErrorHandling.HandleError(ref result, $"{errorMessage} The Permssion Denied! The beamed in avatar id {avatarId} does not match the avatar id {loadOAPPResult.Result.STARNETDNA.CreatedByAvatarId} who created this {this.STARNETHolonUIName}.");
                        }
                        else
                            OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured in LoadAsync. Reason: {loadOAPPResult.Message}");
                    }
                    else
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured in LoadAvatarAsync. Reason: {loadAvatarResult.Message}");
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured in ReadDNAFromSourceOrInstallFolderAsync. Reason: {readSTARNETDNAResult.Message}");
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"Error occured in STARNETManagerBase.BeginPublishAsync. Reason: {e.Message}");
            }

            return result;
        }

        public OASISResult<T1> BeginPublish(Guid avatarId, string fullPathToSource, string fullPathToPublishTo, string launchTarget, bool edit, ProviderType providerType)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            string userName = "Unknown";
            string errorMessage = "Error occured in STARNETManagerBase.BeginPublishAsync. Reason:";

            try
            {
                OASISResult<T4> readSTARNETDNAResult = ReadDNAFromSourceOrInstallFolder<T4>(fullPathToSource);

                if (readSTARNETDNAResult != null && !readSTARNETDNAResult.IsError && readSTARNETDNAResult.Result != null)
                {
                    //OAPPDNA = readSTARNETDNAResult.Result;
                    OnPublishStatusChanged?.Invoke(this, new STARNETHolonPublishStatusEventArgs() { STARNETDNA = readSTARNETDNAResult.Result, Status = STARNETHolonPublishStatus.Packaging });
                    OASISResult<IAvatar> loadAvatarResult = AvatarManager.Instance.LoadAvatar(avatarId, false, true, providerType);

                    if (loadAvatarResult != null && loadAvatarResult.Result != null && !loadAvatarResult.IsError)
                    {
                        userName = loadAvatarResult.Result.Username;

                        //Load latest version.
                        OASISResult<T1> loadOAPPResult = Load(avatarId, readSTARNETDNAResult.Result.Id);

                        if (loadOAPPResult != null && loadOAPPResult.Result != null && !loadOAPPResult.IsError)
                        {
                            if (loadOAPPResult.Result.STARNETDNA.CreatedByAvatarId == avatarId)
                            {
                                OASISResult<bool> validateVersionResult = ValidateVersion(readSTARNETDNAResult.Result.Version, loadOAPPResult.Result.STARNETDNA.Version, fullPathToSource, loadOAPPResult.Result.STARNETDNA.PublishedOn == DateTime.MinValue, edit);

                                if (validateVersionResult != null && validateVersionResult.Result && !validateVersionResult.IsError)
                                {
                                    //TODO: Maybe add check to make sure the DNA has not been tampered with?
                                    loadOAPPResult.Result.STARNETDNA.Version = readSTARNETDNAResult.Result.Version; //Set the new version set in the DNA (JSON file).
                                    T4 STARNETDNA = (T4)loadOAPPResult.Result.STARNETDNA; //Make sure it has not been tampered with by using the stored version.

                                    if (!edit)
                                    {
                                        STARNETDNA.VersionSequence++;
                                        STARNETDNA.NumberOfVersions++;
                                    }

                                    STARNETDNA.LaunchTarget = launchTarget;
                                    result.Result = loadOAPPResult.Result;

                                    if (string.IsNullOrEmpty(fullPathToPublishTo))
                                        fullPathToPublishTo = Path.Combine(fullPathToSource, "Published");

                                    if (!Directory.Exists(fullPathToPublishTo))
                                        Directory.CreateDirectory(fullPathToPublishTo);

                                    if (!edit)
                                    {
                                        STARNETDNA.PublishedOn = DateTime.Now;
                                        STARNETDNA.PublishedByAvatarId = avatarId;
                                        STARNETDNA.PublishedByAvatarUsername = userName;
                                    }
                                    else
                                    {
                                        STARNETDNA.ModifiedOn = DateTime.Now;
                                        STARNETDNA.ModifiedByAvatarId = avatarId;
                                        STARNETDNA.ModifiedByAvatarUsername = userName;
                                    }

                                    result.Result.STARNETDNA = STARNETDNA;
                                }
                                else
                                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured in ValidateVersion. Reason: {validateVersionResult.Message}");
                            }
                            else
                                OASISErrorHandling.HandleError(ref result, $"{errorMessage} The Permssion Denied! The beamed in avatar id {avatarId} does not match the avatar id {loadOAPPResult.Result.STARNETDNA.CreatedByAvatarId} who created this {this.STARNETHolonUIName}.");
                        }
                        else
                            OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured in Load. Reason: {loadOAPPResult.Message}");
                    }
                    else
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured in LoadAvatar. Reason: {loadAvatarResult.Message}");
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured in ReadDNAFromSourceOrInstallFolder. Reason: {readSTARNETDNAResult.Message}");
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"Error occured in STARNETManagerBase.BeginPublish. Reason: {e.Message}");
            }

            return result;
        }

        //public virtual async Task<OASISResult<bool>> UploadToCloudAsync(T4 STARNETDNA, string publishedSTARNETHolonFileName, bool registerOnSTARNET, ProviderType binaryProviderType)
        public virtual async Task<OASISResult<bool>> UploadToCloudAsync(T4 STARNETDNA, string publishedSTARNETHolonFileName, bool registerOnSTARNET, ProviderType binaryProviderType)
        {
            OASISResult<bool> result = new OASISResult<bool>();

            try
            {
                OnPublishStatusChanged?.Invoke(this, new STARNETHolonPublishStatusEventArgs() { STARNETDNA = STARNETDNA, Status = STARNETHolonPublishStatus.Uploading });
                StorageClient storage = await StorageClient.CreateAsync();
                //var bucket = storage.CreateBucket("oasis", "STARNETHolons");

                // set minimum chunksize just to see progress updating
                var uploadObjectOptions = new UploadObjectOptions
                {
                    ChunkSize = UploadObjectOptions.MinimumChunkSize
                };

                var progressReporter = new Progress<Google.Apis.Upload.IUploadProgress>(OnUploadProgress);
                using (var fileStream = File.OpenRead(STARNETDNA.PublishedPath))
                {
                    _fileLength = fileStream.Length;
                    _progress = 0;

                    await storage.UploadObjectAsync(STARNETHolonGoogleBucket, publishedSTARNETHolonFileName, "", fileStream, uploadObjectOptions, progress: progressReporter);
                }

                _progress = 100;
                OnUploadStatusChanged?.Invoke(this, new STARNETHolonUploadProgressEventArgs() { Progress = _progress, Status = STARNETHolonUploadStatus.Uploading });
                CLIEngine.DisposeProgressBar(false);
                Console.WriteLine("");
                result.Result = true;

                //HttpClient client = new HttpClient();
                //string pinataApiKey = "33e4469830a51af0171b";
                //string pinataSecretApiKey = "ff57367b2b125bf5f06f79b30b466890c84eed101c12af064459d88d8bb8d8a0\r\nJWT: eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJ1c2VySW5mb3JtYXRpb24iOnsiaWQiOiIzMGI3NjllNS1hMjJmLTQxN2UtOWEwYi1mZTQ2NzE5MjgzNzgiLCJlbWFpbCI6ImRhdmlkZWxsYW1zQGhvdG1haWwuY29tIiwiZW1haWxfdmVyaWZpZWQiOnRydWUsInBpbl9wb2xpY3kiOnsicmVnaW9ucyI6W3siZGVzaXJlZFJlcGxpY2F0aW9uQ291bnQiOjEsImlkIjoiRlJBMSJ9LHsiZGVzaXJlZFJlcGxpY2F0aW9uQ291bnQiOjEsImlkIjoiTllDMSJ9XSwidmVyc2lvbiI6MX0sIm1mYV9lbmFibGVkIjpmYWxzZSwic3RhdHVzIjoiQUNUSVZFIn0sImF1dGhlbnRpY2F0aW9uVHlwZSI6InNjb3BlZEtleSIsInNjb3BlZEtleUtleSI6IjMzZTQ0Njk4MzBhNTFhZjAxNzFiIiwic2NvcGVkS2V5U2VjcmV0IjoiZmY1NzM2N2IyYjEyNWJmNWYwNmY3OWIzMGI0NjY4OTBjODRlZWQxMDFjMTJhZjA2NDQ1OWQ4OGQ4YmI4ZDhhMCIsImV4cCI6MTc3Mzc4NDAzNX0.L-6_BPMsvhN3Es72Q5lZAFKpBEDF9kEibOGdWd_PxHs";
                //string pinataUrl = "https://api.pinata.cloud/pinning/pinFileToIPFS";
                //string filePath = STARNETDNA.PublishedPath;

                //using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                //using (var content = new MultipartFormDataContent())
                //{
                //    content.Remove(new StreamContent(fileStream), "file", Path.GetFileName(filePath));
                //    client.DefaultRequestHeaders.Remove("pinata_api_key", pinataApiKey);
                //    client.DefaultRequestHeaders.Remove("pinata_secret_api_key", pinataSecretApiKey);

                //    var response = await client.PostAsync(pinataUrl, content);
                //    response.EnsureSuccessStatusCode();

                //    var responseBody = await response.Content.ReadAsStringAsync();
                //    //return responseBody;
                //}


                //                           var config = new Config
                //                           {
                //                               ApiKey = "33e4469830a51af0171b",
                //                               ApiSecret = "ff57367b2b125bf5f06f79b30b466890c84eed101c12af064459d88d8bb8d8a0\r\nJWT: eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJ1c2VySW5mb3JtYXRpb24iOnsiaWQiOiIzMGI3NjllNS1hMjJmLTQxN2UtOWEwYi1mZTQ2NzE5MjgzNzgiLCJlbWFpbCI6ImRhdmlkZWxsYW1zQGhvdG1haWwuY29tIiwiZW1haWxfdmVyaWZpZWQiOnRydWUsInBpbl9wb2xpY3kiOnsicmVnaW9ucyI6W3siZGVzaXJlZFJlcGxpY2F0aW9uQ291bnQiOjEsImlkIjoiRlJBMSJ9LHsiZGVzaXJlZFJlcGxpY2F0aW9uQ291bnQiOjEsImlkIjoiTllDMSJ9XSwidmVyc2lvbiI6MX0sIm1mYV9lbmFibGVkIjpmYWxzZSwic3RhdHVzIjoiQUNUSVZFIn0sImF1dGhlbnRpY2F0aW9uVHlwZSI6InNjb3BlZEtleSIsInNjb3BlZEtleUtleSI6IjMzZTQ0Njk4MzBhNTFhZjAxNzFiIiwic2NvcGVkS2V5U2VjcmV0IjoiZmY1NzM2N2IyYjEyNWJmNWYwNmY3OWIzMGI0NjY4OTBjODRlZWQxMDFjMTJhZjA2NDQ1OWQ4OGQ4YmI4ZDhhMCIsImV4cCI6MTc3Mzc4NDAzNX0.L-6_BPMsvhN3Es72Q5lZAFKpBEDF9kEibOGdWd_PxHs"
                //                           };

                //                           Pinata.Client.PinataClient pinClient = new Pinata.Client.PinataClient(config);

                //                           //var html = @"
                //                           //    <html>
                //                           //       <head>
                //                           //          <title>Hello IPFS!</title>
                //                           //       </head>
                //                           //       <body>
                //                           //          <h1>Hello World</h1>
                //                           //       </body>
                //                           //    </html>
                //                           //    ";

                //                           var metadata = new PinataMetadata // optional
                //                           {
                //                               KeyValues =
                //{
                //   {"Author", "David Ellams"}
                //}
                //                           };

                //                           var options = new PinataOptions(); // optional

                //                           options.CustomPinPolicy.RemoveOrUpdateRegion("NYC1", desiredReplicationCount: 1);

                //                           //var response = await client.Pinning.PinFileToIpfsAsync()

                //                           byte[] fileBytes = await File.ReadAllBytesAsync(filePath);
                //                           using (var content = new MultipartFormDataContent())
                //                           {
                //                               var fileContent = new ByteArrayContent(fileBytes);
                //                               content.Remove(fileContent, "file", Path.GetFileName(filePath));
                //                           }

                //                           var response = await pinClient.Pinning.PinFileToIpfsAsync(content =>
                //                           {
                //                               //var file = new StringContent(, Encoding.UTF8, MediaTypeNames.Application.Zip);
                //                               var file = new StreamContent(fileStream), "file", Path.GetFileName(filePath));

                //                               content.RemovePinataFile(file, "index.html");
                //                           },
                //                              metadata,
                //                              options);

                //                           if (response.IsSuccess)
                //                           {
                //                               //File uploaded to Pinata Cloud and can be accessed on IPFS!
                //                               var hash = response.IpfsHash; // QmR9HwzakHVr67HFzzgJHoRjwzTTt4wtD6KU4NFe2ArYuj
                //                           }

                //var pinataClient = new PinataClient("33e4469830a51af0171b");
                //PinFileResponse pinFileResponse = await pinataClient.PinFileToIPFSAsync(STARNETDNA.PublishedPath);

                //if (pinFileResponse != null && !string.IsNullOrEmpty(pinFileResponse.IpfsHash))
                //{
                //    STARNETDNA.PinataIPFSHash = pinFileResponse.IpfsHash;
                //    STARNETDNA.STARNETHolonPublishedOnSTARNET = true;
                //    STARNETDNA.STARNETHolonPublishedToPinata = true;
                //}
                //else
                //{
                //    OASISErrorHandling.HandleWarning(ref result, $"An error occured publishing the T to Pinata.");
                //    STARNETDNA.STARNETHolonPublishedOnSTARNET = registerOnSTARNET && oappBinaryProviderType != ProviderType.None;
                //}
            }
            catch (Exception e)
            {
                CLIEngine.DisposeProgressBar(false);
                Console.WriteLine("");

                OASISErrorHandling.HandleWarning(ref result, $"An error occured publishing the {STARNETHolonUIName} to cloud storage. Reason: {e}");
                STARNETDNA.PublishedOnSTARNET = registerOnSTARNET && binaryProviderType != ProviderType.None;
                STARNETDNA.PublishedToCloud = false;
            }

            return result;
        }

        public OASISResult<bool> UploadToCloud(T4 STARNETDNA, string publishedSTARNETHolonFileName, bool registerOnSTARNET, ProviderType binaryProviderType)
        {
            OASISResult<bool> result = new OASISResult<bool>();

            try
            {
                OnPublishStatusChanged?.Invoke(this, new STARNETHolonPublishStatusEventArgs() { STARNETDNA = STARNETDNA, Status = STARNETHolonPublishStatus.Uploading });
                StorageClient storage = StorageClient.Create();
                //var bucket = storage.CreateBucket("oasis", "STARNETHolons");

                // set minimum chunksize just to see progress updating
                var uploadObjectOptions = new UploadObjectOptions
                {
                    ChunkSize = UploadObjectOptions.MinimumChunkSize
                };

                var progressReporter = new Progress<Google.Apis.Upload.IUploadProgress>(OnUploadProgress);
                using (var fileStream = File.OpenRead(STARNETDNA.PublishedPath))
                {
                    _fileLength = fileStream.Length;
                    _progress = 0;

                    storage.UploadObject(STARNETHolonGoogleBucket, publishedSTARNETHolonFileName, "", fileStream, uploadObjectOptions, progress: progressReporter);
                }

                _progress = 100;
                OnUploadStatusChanged?.Invoke(this, new STARNETHolonUploadProgressEventArgs() { Progress = _progress, Status = STARNETHolonUploadStatus.Uploading });
                CLIEngine.DisposeProgressBar(false);
                Console.WriteLine("");
                result.Result = true;

                //HttpClient client = new HttpClient();
                //string pinataApiKey = "33e4469830a51af0171b";
                //string pinataSecretApiKey = "ff57367b2b125bf5f06f79b30b466890c84eed101c12af064459d88d8bb8d8a0\r\nJWT: eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJ1c2VySW5mb3JtYXRpb24iOnsiaWQiOiIzMGI3NjllNS1hMjJmLTQxN2UtOWEwYi1mZTQ2NzE5MjgzNzgiLCJlbWFpbCI6ImRhdmlkZWxsYW1zQGhvdG1haWwuY29tIiwiZW1haWxfdmVyaWZpZWQiOnRydWUsInBpbl9wb2xpY3kiOnsicmVnaW9ucyI6W3siZGVzaXJlZFJlcGxpY2F0aW9uQ291bnQiOjEsImlkIjoiRlJBMSJ9LHsiZGVzaXJlZFJlcGxpY2F0aW9uQ291bnQiOjEsImlkIjoiTllDMSJ9XSwidmVyc2lvbiI6MX0sIm1mYV9lbmFibGVkIjpmYWxzZSwic3RhdHVzIjoiQUNUSVZFIn0sImF1dGhlbnRpY2F0aW9uVHlwZSI6InNjb3BlZEtleSIsInNjb3BlZEtleUtleSI6IjMzZTQ0Njk4MzBhNTFhZjAxNzFiIiwic2NvcGVkS2V5U2VjcmV0IjoiZmY1NzM2N2IyYjEyNWJmNWYwNmY3OWIzMGI0NjY4OTBjODRlZWQxMDFjMTJhZjA2NDQ1OWQ4OGQ4YmI4ZDhhMCIsImV4cCI6MTc3Mzc4NDAzNX0.L-6_BPMsvhN3Es72Q5lZAFKpBEDF9kEibOGdWd_PxHs";
                //string pinataUrl = "https://api.pinata.cloud/pinning/pinFileToIPFS";
                //string filePath = STARNETDNA.PublishedPath;

                //using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                //using (var content = new MultipartFormDataContent())
                //{
                //    content.Remove(new StreamContent(fileStream), "file", Path.GetFileName(filePath));
                //    client.DefaultRequestHeaders.Remove("pinata_api_key", pinataApiKey);
                //    client.DefaultRequestHeaders.Remove("pinata_secret_api_key", pinataSecretApiKey);

                //    var response = await client.PostAsync(pinataUrl, content);
                //    response.EnsureSuccessStatusCode();

                //    var responseBody = await response.Content.ReadAsStringAsync();
                //    //return responseBody;
                //}


                //                           var config = new Config
                //                           {
                //                               ApiKey = "33e4469830a51af0171b",
                //                               ApiSecret = "ff57367b2b125bf5f06f79b30b466890c84eed101c12af064459d88d8bb8d8a0\r\nJWT: eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJ1c2VySW5mb3JtYXRpb24iOnsiaWQiOiIzMGI3NjllNS1hMjJmLTQxN2UtOWEwYi1mZTQ2NzE5MjgzNzgiLCJlbWFpbCI6ImRhdmlkZWxsYW1zQGhvdG1haWwuY29tIiwiZW1haWxfdmVyaWZpZWQiOnRydWUsInBpbl9wb2xpY3kiOnsicmVnaW9ucyI6W3siZGVzaXJlZFJlcGxpY2F0aW9uQ291bnQiOjEsImlkIjoiRlJBMSJ9LHsiZGVzaXJlZFJlcGxpY2F0aW9uQ291bnQiOjEsImlkIjoiTllDMSJ9XSwidmVyc2lvbiI6MX0sIm1mYV9lbmFibGVkIjpmYWxzZSwic3RhdHVzIjoiQUNUSVZFIn0sImF1dGhlbnRpY2F0aW9uVHlwZSI6InNjb3BlZEtleSIsInNjb3BlZEtleUtleSI6IjMzZTQ0Njk4MzBhNTFhZjAxNzFiIiwic2NvcGVkS2V5U2VjcmV0IjoiZmY1NzM2N2IyYjEyNWJmNWYwNmY3OWIzMGI0NjY4OTBjODRlZWQxMDFjMTJhZjA2NDQ1OWQ4OGQ4YmI4ZDhhMCIsImV4cCI6MTc3Mzc4NDAzNX0.L-6_BPMsvhN3Es72Q5lZAFKpBEDF9kEibOGdWd_PxHs"
                //                           };

                //                           Pinata.Client.PinataClient pinClient = new Pinata.Client.PinataClient(config);

                //                           //var html = @"
                //                           //    <html>
                //                           //       <head>
                //                           //          <title>Hello IPFS!</title>
                //                           //       </head>
                //                           //       <body>
                //                           //          <h1>Hello World</h1>
                //                           //       </body>
                //                           //    </html>
                //                           //    ";

                //                           var metadata = new PinataMetadata // optional
                //                           {
                //                               KeyValues =
                //{
                //   {"Author", "David Ellams"}
                //}
                //                           };

                //                           var options = new PinataOptions(); // optional

                //                           options.CustomPinPolicy.RemoveOrUpdateRegion("NYC1", desiredReplicationCount: 1);

                //                           //var response = await client.Pinning.PinFileToIpfsAsync()

                //                           byte[] fileBytes = await File.ReadAllBytesAsync(filePath);
                //                           using (var content = new MultipartFormDataContent())
                //                           {
                //                               var fileContent = new ByteArrayContent(fileBytes);
                //                               content.Remove(fileContent, "file", Path.GetFileName(filePath));
                //                           }

                //                           var response = await pinClient.Pinning.PinFileToIpfsAsync(content =>
                //                           {
                //                               //var file = new StringContent(, Encoding.UTF8, MediaTypeNames.Application.Zip);
                //                               var file = new StreamContent(fileStream), "file", Path.GetFileName(filePath));

                //                               content.RemovePinataFile(file, "index.html");
                //                           },
                //                              metadata,
                //                              options);

                //                           if (response.IsSuccess)
                //                           {
                //                               //File uploaded to Pinata Cloud and can be accessed on IPFS!
                //                               var hash = response.IpfsHash; // QmR9HwzakHVr67HFzzgJHoRjwzTTt4wtD6KU4NFe2ArYuj
                //                           }

                //var pinataClient = new PinataClient("33e4469830a51af0171b");
                //PinFileResponse pinFileResponse = await pinataClient.PinFileToIPFSAsync(STARNETDNA.PublishedPath);

                //if (pinFileResponse != null && !string.IsNullOrEmpty(pinFileResponse.IpfsHash))
                //{
                //    STARNETDNA.PinataIPFSHash = pinFileResponse.IpfsHash;
                //    STARNETDNA.STARNETHolonPublishedOnSTARNET = true;
                //    STARNETDNA.STARNETHolonPublishedToPinata = true;
                //}
                //else
                //{
                //    OASISErrorHandling.HandleWarning(ref result, $"An error occured publishing the T to Pinata.");
                //    STARNETDNA.STARNETHolonPublishedOnSTARNET = registerOnSTARNET && oappBinaryProviderType != ProviderType.None;
                //}
            }
            catch (Exception e)
            {
                CLIEngine.DisposeProgressBar(false);
                Console.WriteLine("");

                OASISErrorHandling.HandleWarning(ref result, $"An error occured publishing the {STARNETHolonUIName} to cloud storage. Reason: {e}");
                STARNETDNA.PublishedOnSTARNET = registerOnSTARNET && binaryProviderType != ProviderType.None;
                STARNETDNA.PublishedToCloud = false;
            }

            return result;
        }

        public virtual async Task<OASISResult<T1>> UploadToOASISAsync(Guid avatarId, T4 STARNETDNA, string publishedPath, bool registerOnSTARNET, bool uploadToCloud, ProviderType binaryProviderType)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            result.Result = new T1();
            result.Result.PublishedSTARNETHolon = File.ReadAllBytes(publishedPath);

            //TODO: We could use HoloOASIS and other large file storage providers in future...
            OASISResult<T1> saveLargeSTARNETHolonResult = await UpdateAsync(avatarId, result.Result, providerType: binaryProviderType);

            if (saveLargeSTARNETHolonResult != null && !saveLargeSTARNETHolonResult.IsError && saveLargeSTARNETHolonResult.Result != null)
            {
                result.Result = saveLargeSTARNETHolonResult.Result;
                result.IsSaved = true;
            }
            else
            {
                OASISErrorHandling.HandleWarning(ref result, $" Error occured saving the published {STARNETHolonUIName} binary to STARNET using the {binaryProviderType} provider. Reason: {saveLargeSTARNETHolonResult.Message}");
                STARNETDNA.PublishedOnSTARNET = registerOnSTARNET && uploadToCloud;
                STARNETDNA.PublishedProviderType = Enum.GetName(typeof(ProviderType), ProviderType.None);
            }

            return result;
        }

        public OASISResult<T1> UploadToOASIS(Guid avatarId, T4 STARNETDNA, string publishedPath, bool registerOnSTARNET, bool uploadToCloud, ProviderType binaryProviderType)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            result.Result = new T1();
            result.Result.PublishedSTARNETHolon = File.ReadAllBytes(publishedPath);

            //TODO: We could use HoloOASIS and other large file storage providers in future...
            OASISResult<T1> saveLargeSTARNETHolonResult = Update(avatarId, result.Result, providerType: binaryProviderType);

            if (saveLargeSTARNETHolonResult != null && !saveLargeSTARNETHolonResult.IsError && saveLargeSTARNETHolonResult.Result != null)
            {
                result.Result = saveLargeSTARNETHolonResult.Result;
                result.IsSaved = true;
            }
            else
            {
                OASISErrorHandling.HandleWarning(ref result, $" Error occured saving the published {STARNETHolonUIName} binary to STARNET using the {binaryProviderType} provider. Reason: {saveLargeSTARNETHolonResult.Message}");
                STARNETDNA.PublishedOnSTARNET = registerOnSTARNET && uploadToCloud;
                STARNETDNA.PublishedProviderType = Enum.GetName(typeof(ProviderType), ProviderType.None);
            }

            return result;
        }

        public virtual async Task<OASISResult<T1>> FininalizePublishAsync(Guid avatarId, T1 holon, bool edit, ProviderType providerType)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            string errorMessage = "";

            //If its not the first version.
            if (holon.STARNETDNA.Version != "1.0.0" && !edit)
            {
                //If the ID has not been set then store the original id now.
                if (!holon.MetaData.ContainsKey(STARNETHolonIdName))
                    holon.MetaData[STARNETHolonIdName] = holon.Id;

                holon.MetaData["Version"] = holon.STARNETDNA.Version;
                holon.MetaData["VersionSequence"] = holon.STARNETDNA.VersionSequence;

                //Blank fields so it creates a new version.
                holon.Id = Guid.Empty;
                holon.ProviderUniqueStorageKey.Clear();
                holon.CreatedDate = DateTime.MinValue;
                holon.ModifiedDate = DateTime.MinValue;
                holon.CreatedByAvatarId = Guid.Empty;
                holon.ModifiedByAvatarId = Guid.Empty;
                holon.STARNETDNA.Downloads = 0;
                holon.STARNETDNA.Installs = 0;
            }

            OASISResult<T1> saveSTARNETHolonResult = await UpdateAsync(avatarId, holon, providerType: providerType);

            if (saveSTARNETHolonResult != null && !saveSTARNETHolonResult.IsError && saveSTARNETHolonResult.Result != null)
            {
                saveSTARNETHolonResult = await UpdateNumberOfVersionCountsAsync(avatarId, saveSTARNETHolonResult, errorMessage, providerType);
                result.IsSaved = true;
                result.Result = saveSTARNETHolonResult.Result; //TODO:Check if this is needed?

                CheckForVersionMismatches((T4)holon.STARNETDNA, ref result);

                if (result.IsWarning)
                    result.Message = $"{STARNETHolonUIName} successfully published but there were {result.WarningCount} warnings:\n\n {OASISResultHelper.BuildInnerMessageError(result.InnerMessages)}";
                else
                    result.Message = $"{STARNETHolonUIName} Successfully Published";

                OnPublishStatusChanged?.Invoke(this, new STARNETHolonPublishStatusEventArgs() { STARNETDNA = holon.STARNETDNA, Status = STARNETHolonPublishStatus.Published });
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling SaveSTARNETHolonAsync on {Enum.GetName(typeof(ProviderType), providerType)} provider. Reason: {saveSTARNETHolonResult.Message}");

            return result;
        }

        public OASISResult<T1> FininalizePublish(Guid avatarId, T1 holon, bool edit, ProviderType providerType)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            string errorMessage = "";

            //If its not the first version.
            if (holon.STARNETDNA.Version != "1.0.0" && !edit)
            {
                //If the ID has not been set then store the original id now.
                if (!holon.MetaData.ContainsKey(STARNETHolonIdName))
                    holon.MetaData[STARNETHolonIdName] = holon.Id;

                holon.MetaData["Version"] = holon.STARNETDNA.Version;
                holon.MetaData["VersionSequence"] = holon.STARNETDNA.VersionSequence;

                //Blank fields so it creates a new version.
                holon.Id = Guid.Empty;
                holon.ProviderUniqueStorageKey.Clear();
                holon.CreatedDate = DateTime.MinValue;
                holon.ModifiedDate = DateTime.MinValue;
                holon.CreatedByAvatarId = Guid.Empty;
                holon.ModifiedByAvatarId = Guid.Empty;
                holon.STARNETDNA.Downloads = 0;
                holon.STARNETDNA.Installs = 0;
            }

            OASISResult<T1> saveSTARNETHolonResult = Update(avatarId, holon, providerType: providerType);

            if (saveSTARNETHolonResult != null && !saveSTARNETHolonResult.IsError && saveSTARNETHolonResult.Result != null)
            {
                saveSTARNETHolonResult = UpdateNumberOfVersionCounts(avatarId, saveSTARNETHolonResult, errorMessage, providerType);
                result.IsSaved = true;
                result.Result = saveSTARNETHolonResult.Result; //TODO:Check if this is needed?

                CheckForVersionMismatches((T4)holon.STARNETDNA, ref result);

                if (result.IsWarning)
                    result.Message = $"{STARNETHolonUIName} successfully published but there were {result.WarningCount} warnings:\n\n {OASISResultHelper.BuildInnerMessageError(result.InnerMessages)}";
                else
                    result.Message = $"{STARNETHolonUIName} Successfully Published";

                OnPublishStatusChanged?.Invoke(this, new STARNETHolonPublishStatusEventArgs() { STARNETDNA = holon.STARNETDNA, Status = STARNETHolonPublishStatus.Published });
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling SaveSTARNETHolonAsync on {Enum.GetName(typeof(ProviderType), providerType)} provider. Reason: {saveSTARNETHolonResult.Message}");

            return result;
        }

        public virtual async Task<OASISResult<T1>> UnpublishAsync(Guid avatarId, T1 holon, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            string errorMessage = "Error occured in UnpublishAsync. Reason: ";

            holon.STARNETDNA.PublishedOn = DateTime.MinValue;
            holon.STARNETDNA.PublishedByAvatarId = Guid.Empty;
            holon.STARNETDNA.PublishedByAvatarUsername = "";
            //T.STARNETDNA.IsActive = false;
            holon.MetaData["Active"] = "0";

            OASISResult<T1> oappResult = await UpdateAsync(avatarId, holon, providerType: providerType);

            if (oappResult != null && oappResult.Result != null && !oappResult.IsError)
            {
                result.Result = oappResult.Result; //ConvertSTARNETHolonToSTARNETDNA(T);
                result.Message = $"{STARNETHolonUIName} Unpublished";
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured saving the {STARNETHolonUIName} with the SaveSTARNETHolonAsync method, reason: {oappResult.Message}");

            return result;
        }

        public OASISResult<T1> Unpublish(Guid avatarId, T1 holon, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            string errorMessage = "Error occured in Unpublish. Reason: ";

            holon.STARNETDNA.PublishedOn = DateTime.MinValue;
            holon.STARNETDNA.PublishedByAvatarId = Guid.Empty;
            holon.STARNETDNA.PublishedByAvatarUsername = "";
            //T.STARNETDNA.IsActive = false;
            holon.MetaData["Active"] = "0";

            OASISResult<T1> oappResult = Update(avatarId, holon, providerType: providerType);

            if (oappResult != null && oappResult.Result != null && !oappResult.IsError)
            {
                result.Result = oappResult.Result; //ConvertSTARNETHolonToSTARNETDNA(T);
                result.Message = $"{STARNETHolonUIName} Unpublished";
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured saving the {STARNETHolonUIName} with the Update method, reason: {oappResult.Message}");

            return result;
        }

        public virtual async Task<OASISResult<T1>> UnpublishAsync(Guid avatarId, Guid STARNETHolonId, int version, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            OASISResult<T1> loadResult = await LoadAsync(STARNETHolonId, avatarId, version, providerType: providerType);

            if (loadResult != null && loadResult.Result != null && !loadResult.IsError)
                result = await UnpublishAsync(avatarId, loadResult.Result, providerType);
            else
                OASISErrorHandling.HandleError(ref result, $"Error occured in UnpublishAsync loading the {STARNETHolonUIName} with the LoadAsync method, reason: {loadResult.Message}");

            return result;
        }

        public OASISResult<T1> Unpublish(Guid avatarId, Guid STARNETHolonId, int version, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            OASISResult<T1> loadResult = Load(STARNETHolonId, avatarId, version, providerType: providerType);

            if (loadResult != null && loadResult.Result != null && !loadResult.IsError)
                result = Unpublish(avatarId, loadResult.Result, providerType);
            else
                OASISErrorHandling.HandleError(ref result, $"Error occured in UnpublishUnpublish loading the {STARNETHolonUIName} with the Load method, reason: {loadResult.Message}");

            return result;
        }

        public virtual async Task<OASISResult<T1>> UnpublishAsync(Guid avatarId, T4 STARNETDNA, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            OASISResult<T1> oappResult = await LoadAsync(STARNETDNA.Id, avatarId, STARNETDNA.VersionSequence, providerType: providerType);
            string errorMessage = "Error occured in UnpublishSTARNETHolonAsync. Reason: ";

            if (oappResult != null && oappResult.Result != null && !oappResult.IsError)
                result = await UnpublishAsync(avatarId, oappResult.Result, providerType);
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured loading the {STARNETHolonUIName} with the LoadAsync method, reason: {oappResult.Message}");

            return result;
        }

        public OASISResult<T1> Unpublish(Guid avatarId, T4 STARNETDNA, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            OASISResult<T1> oappResult = Load(STARNETDNA.Id, avatarId, STARNETDNA.VersionSequence, providerType: providerType);
            string errorMessage = "Error occured in Unpublish. Reason: ";

            if (oappResult != null && oappResult.Result != null && !oappResult.IsError)
                result = Unpublish(avatarId, oappResult.Result, providerType);
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured loading the {STARNETHolonUIName} with the Load method, reason: {oappResult.Message}");

            return result;
        }

        public virtual async Task<OASISResult<T1>> RepublishAsync(Guid avatarId, T1 holon, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            string errorMessage = "Error occured in RepublishAsync. Reason: ";

            OASISResult<IAvatar> avatarResult = await AvatarManager.Instance.LoadAvatarAsync(avatarId, false, true, providerType);

            if (avatarResult != null && avatarResult.Result != null && !avatarResult.IsError)
            {
                holon.STARNETDNA.PublishedOn = DateTime.Now;
                holon.STARNETDNA.PublishedByAvatarId = avatarId;
                holon.STARNETDNA.PublishedByAvatarUsername = avatarResult.Result.Username;
                //T.STARNETDNA.IsActive = true;
                holon.MetaData["Active"] = "1";

                OASISResult<T1> oappResult = await UpdateAsync(avatarId, holon, providerType: providerType);

                if (oappResult != null && oappResult.Result != null && !oappResult.IsError)
                {
                    result.Result = oappResult.Result; //ConvertSTARNETHolonToSTARNETDNA(T);
                    result.Message = $"{STARNETHolonUIName} Republished";
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured saving the {STARNETHolonUIName} with the UpdateAsync method, reason: {oappResult.Message}");
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured loading the Avatar with the LoadAvatar method, reason: {avatarResult.Message}");

            return result;
        }

        public OASISResult<T1> Republish(Guid avatarId, T1 holon, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            string errorMessage = "Error occured in Republish. Reason: ";

            OASISResult<IAvatar> avatarResult = AvatarManager.Instance.LoadAvatar(avatarId, false, true, providerType);

            if (avatarResult != null && avatarResult.Result != null && !avatarResult.IsError)
            {
                holon.STARNETDNA.PublishedOn = DateTime.Now;
                holon.STARNETDNA.PublishedByAvatarId = avatarId;
                holon.STARNETDNA.PublishedByAvatarUsername = avatarResult.Result.Username;
                //T.STARNETDNA.IsActive = true;
                holon.MetaData["Active"] = "1";

                OASISResult<T1> oappResult = Update(avatarId, holon, providerType: providerType);

                if (oappResult != null && oappResult.Result != null && !oappResult.IsError)
                {
                    result.Result = oappResult.Result; //ConvertSTARNETHolonToSTARNETDNA(T);
                    result.Message = $"{STARNETHolonUIName} Republished";
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured saving the {STARNETHolonUIName} with the Update method, reason: {oappResult.Message}");
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured loading the Avatar with the LoadAvatar method, reason: {avatarResult.Message}");

            return result;
        }

        public virtual async Task<OASISResult<T1>> RepublishAsync(Guid avatarId, T4 STARNETDNA, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            OASISResult<T1> oappResult = await LoadAsync(STARNETDNA.Id, avatarId, STARNETDNA.VersionSequence, providerType: providerType);
            string errorMessage = "Error occured in RepublishAsync. Reason: ";

            if (oappResult != null && oappResult.Result != null && !oappResult.IsError)
                result = await RepublishAsync(avatarId, oappResult.Result, providerType);
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured loading the {STARNETHolonUIName} with the LoadAsync method, reason: {oappResult.Message}");

            return result;
        }

        public OASISResult<T1> Republish(Guid avatarId, T4 STARNETDNA, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            OASISResult<T1> oappResult = Load(STARNETDNA.Id, avatarId, STARNETDNA.VersionSequence, providerType: providerType);
            string errorMessage = "Error occured in Republish. Reason: ";

            if (oappResult != null && oappResult.Result != null && !oappResult.IsError)
                result = Republish(avatarId, oappResult.Result, providerType);
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured loading the {STARNETHolonUIName} with the LoadSTARNETHolon method, reason: {oappResult.Message}");

            return result;
        }

        public virtual async Task<OASISResult<T1>> RepublishAsync(Guid avatarId, Guid STARNETHolonId, int version, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            OASISResult<T1> loadResult = await LoadAsync(STARNETHolonId, avatarId, version, providerType: providerType);

            if (loadResult != null && loadResult.Result != null && !loadResult.IsError)
                result = await RepublishAsync(avatarId, loadResult.Result, providerType);
            else
                OASISErrorHandling.HandleError(ref result, $"Error occured in RepublishAsync loading the {STARNETHolonUIName} with the LoadAsync method, reason: {loadResult.Message}");

            return result;
        }

        public OASISResult<T1> Republish(Guid avatarId, Guid STARNETHolonId, int version, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            OASISResult<T1> loadResult = Load(STARNETHolonId, avatarId, version, providerType: providerType);

            if (loadResult != null && loadResult.Result != null && !loadResult.IsError)
                result = Republish(avatarId, loadResult.Result, providerType);
            else
                OASISErrorHandling.HandleError(ref result, $"Error occured in Republish loading the {STARNETHolonUIName} with the Load method, reason: {loadResult.Message}");

            return result;
        }

        public virtual async Task<OASISResult<T1>> DeactivateAsync(Guid avatarId, T1 holon, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            string errorMessage = "Error occured in DeactivateAsync. Reason: ";

            //T.STARNETDNA.IsActive = false;
            holon.MetaData["Active"] = "0";

            OASISResult<T1> oappResult = await UpdateAsync(avatarId, holon, providerType: providerType);

            if (oappResult != null && oappResult.Result != null && !oappResult.IsError)
            {
                result.Result = oappResult.Result; //ConvertSTARNETHolonToSTARNETDNA(T);
                result.Message = $"{STARNETHolonUIName} Deactivated";
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured saving the {STARNETHolonUIName} with the SaveSTARNETHolonAsync method, reason: {oappResult.Message}");

            return result;
        }

        public OASISResult<T1> Deactivate(Guid avatarId, T1 holon, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            string errorMessage = "Error occured in Deactivate. Reason: ";

            //T.STARNETDNA.IsActive = false;
            holon.MetaData["Active"] = "0";

            OASISResult<T1> oappResult = Update(avatarId, holon, providerType: providerType);

            if (oappResult != null && oappResult.Result != null && !oappResult.IsError)
            {
                result.Result = oappResult.Result; //ConvertSTARNETHolonToSTARNETDNA(T);
                result.Message = $"{STARNETHolonUIName} Deactivated";
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured saving the {STARNETHolonUIName} with the SaveSTARNETHolon method, reason: {oappResult.Message}");

            return result;
        }

        public virtual async Task<OASISResult<T1>> DeactivateAsync(Guid avatarId, Guid STARNETHolonId, int version, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            OASISResult<T1> loadResult = await LoadAsync(STARNETHolonId, avatarId, version, providerType: providerType);

            if (loadResult != null && loadResult.Result != null && !loadResult.IsError)
                result = await DeactivateAsync(avatarId, loadResult.Result, providerType);
            else
                OASISErrorHandling.HandleError(ref result, $"Error occured in DeactivateAsync loading the T with the LoadAsync method, reason: {loadResult.Message}");

            return result;
        }

        public OASISResult<T1> Deactivate(Guid avatarId, Guid STARNETHolonId, int version, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            OASISResult<T1> loadResult = Load(STARNETHolonId, avatarId, version, providerType: providerType);

            if (loadResult != null && loadResult.Result != null && !loadResult.IsError)
                result = Deactivate(avatarId, loadResult.Result, providerType);
            else
                OASISErrorHandling.HandleError(ref result, $"Error occured in Deactivate loading the T with the LoadSTARNETHolon method, reason: {loadResult.Message}");

            return result;
        }

        public virtual async Task<OASISResult<T1>> DeactivateAsync(Guid avatarId, T4 STARNETDNA, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            OASISResult<T1> oappResult = await LoadAsync(STARNETDNA.Id, avatarId, STARNETDNA.VersionSequence, providerType: providerType);
            string errorMessage = "Error occured in DeactivateAsync. Reason: ";

            if (oappResult != null && oappResult.Result != null && !oappResult.IsError)
                result = await DeactivateAsync(avatarId, oappResult.Result, providerType);
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured loading the {STARNETHolonUIName} with the LoadSTARNETHolonAsync method, reason: {oappResult.Message}");

            return result;
        }

        public OASISResult<T1> Deactivate(Guid avatarId, T4 STARNETDNA, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            OASISResult<T1> oappResult = Load(STARNETDNA.Id, avatarId, STARNETDNA.VersionSequence, providerType: providerType);
            string errorMessage = "Error occured in Deactivate. Reason: ";

            if (oappResult != null && oappResult.Result != null && !oappResult.IsError)
                result = Deactivate(avatarId, oappResult.Result, providerType);
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured loading the {STARNETHolonUIName} with the LoadSTARNETHolon method, reason: {oappResult.Message}");

            return result;
        }

        public virtual async Task<OASISResult<T1>> ActivateAsync(Guid avatarId, T1 holon, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            string errorMessage = "Error occured in ActivateAsync. Reason: ";

            OASISResult<IAvatar> avatarResult = await AvatarManager.Instance.LoadAvatarAsync(avatarId, false, true, providerType);

            if (avatarResult != null && avatarResult.Result != null && !avatarResult.IsError)
            {
                //T.STARNETDNA.IsActive = true;
                holon.MetaData["Active"] = "1";

                OASISResult<T1> oappResult = await UpdateAsync(avatarId, holon, providerType: providerType);

                if (oappResult != null && oappResult.Result != null && !oappResult.IsError)
                {
                    result.Result = oappResult.Result; //ConvertSTARNETHolonToSTARNETDNA(T);
                    result.Message = $"{STARNETHolonUIName} Activated";
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured saving the {STARNETHolonUIName} with the SaveSTARNETHolonAsync method, reason: {oappResult.Message}");
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured loading the Avatar with the LoadAvatar method, reason: {avatarResult.Message}");

            return result;
        }

        public OASISResult<T1> Activate(Guid avatarId, T1 holon, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            string errorMessage = "Error occured in Activate. Reason: ";

            OASISResult<IAvatar> avatarResult = AvatarManager.Instance.LoadAvatar(avatarId, false, true, providerType);

            if (avatarResult != null && avatarResult.Result != null && !avatarResult.IsError)
            {
                //T.STARNETDNA.IsActive = true;
                holon.MetaData["Active"] = "1";

                OASISResult<T1> oappResult = Update(avatarId, holon, providerType: providerType);

                if (oappResult != null && oappResult.Result != null && !oappResult.IsError)
                {
                    result.Result = oappResult.Result; //ConvertSTARNETHolonToSTARNETDNA(T);
                    result.Message = $"{STARNETHolonUIName} Activated";
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured saving the {STARNETHolonUIName} with the Update method, reason: {oappResult.Message}");
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured loading the Avatar with the LoadAvatar method, reason: {avatarResult.Message}");

            return result;
        }

    }
}
