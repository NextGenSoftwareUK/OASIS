using System;
using System.IO;
using System.Diagnostics;
using System.Threading.Tasks;
using NextGenSoftware.Utilities;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.ONODE.Core.Holons;
using NextGenSoftware.OASIS.API.ONODE.Core.Managers.Base;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Managers;
using NextGenSoftware.OASIS.API.ONODE.Core.Enums.STARNETHolon;
using NextGenSoftware.OASIS.API.ONODE.Core.Objects;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Managers
{
    public class OAPPManager : STARNETManagerBase<OAPP, DownloadedOAPP, InstalledOAPP, OAPPDNA>, IOAPPManager
    {
        public OAPPManager(Guid avatarId, OASISDNA OASISDNA = null) : base(avatarId,
             OASISDNA,
            typeof(OAPPType),
            HolonType.OAPP,
            HolonType.InstalledOAPP,
            "OAPP",
            "OAPPId",
            "OAPPName",
            "OAPPType",
            "oapp",
            "oasis_oapps",
            "OAPPDNA.json",
            "OAPPDNAJSON")
        { }

        public OAPPManager(IOASISStorageProvider OASISStorageProvider, Guid avatarId, OASISDNA OASISDNA = null, bool checkIfSourcePathExists = true) : base(OASISStorageProvider, avatarId,
            OASISDNA,
            typeof(OAPPType),
            HolonType.OAPP,
            HolonType.InstalledOAPP,
            "OAPP",
            "OAPPId",
            "OAPPName",
            "OAPPType",
            "oapp",
            "oasis_oapps",
            "OAPPDNA.json",
            "OAPPDNAJSON")
        { }

        public async Task<OASISResult<IOAPP>> PublishOAPPAsync(Guid avatarId, string fullPathToSource, string launchTarget, string fullPathToPublishTo = "", bool edit = false, bool registerOnSTARNET = true, bool dotnetPublish = true, bool generateSource = true, bool uploadSourceToSTARNET = true, bool makeSourcePublic = false, bool generateBinary = true, bool generateSelfContainedBinary = false, bool generateSelfContainedFullBinary = false, bool uploadToCloud = false, bool uploadSelfContainedToCloud = false, bool uploadSelfContainedFullToCloud = false, ProviderType providerType = ProviderType.Default, ProviderType binaryProviderType = ProviderType.IPFSOASIS, ProviderType selfContainedBinaryProviderType = ProviderType.None, ProviderType selfContainedFullBinaryProviderType = ProviderType.None, bool embedRuntimes = false, bool embedLibs = false, bool embedTemplates = false)
        {
            OASISResult<IOAPP> result = new OASISResult<IOAPP>();
            IOAPPDNA OAPPDNA = null;
            //ISTARNETDNA OAPPDNA = null;
            IOAPP OAPP = null;
            string originalFullPathToSource = fullPathToSource;
            string errorMessage = "Error occured in OAPPManager.PublishAsync. Reason:";

            OASISResult<OAPP> validateResult = await BeginPublishAsync(avatarId, fullPathToSource, launchTarget, fullPathToPublishTo, edit, providerType);

            if (validateResult != null && validateResult.Result != null && !validateResult.IsError)
            {
                OAPPDNA = (IOAPPDNA)validateResult.Result.STARNETDNA;
                //OAPPDNA = validateResult.Result.STARNETDNA;
                OAPP = validateResult.Result;

                string publishedFileName = string.Concat(OAPPDNA.Name, "_v", OAPPDNA.Version, ".", STARNETHolonFileExtention);
                string publishedSelfContainedFileName = string.Concat(OAPPDNA.Name, " (Self Contained)_v", OAPPDNA.Version, ".", STARNETHolonFileExtention);
                string publishedSelfContainedFullFileName = string.Concat(OAPPDNA.Name, " (Self Contained)_v", OAPPDNA.Version, ".", STARNETHolonFileExtention);
                string publishedSourceFileName = string.Concat(OAPPDNA.Name, "_v", OAPPDNA.Version, ".oappsource");

                OAPPDNA.PublishedOnSTARNET = registerOnSTARNET && (binaryProviderType != ProviderType.None || uploadToCloud || selfContainedBinaryProviderType != ProviderType.None || uploadSelfContainedToCloud || selfContainedFullBinaryProviderType != ProviderType.None || uploadSelfContainedFullToCloud);

                if (dotnetPublish || generateSelfContainedFullBinary)
                { 
                    OASISResult<(bool, string)> publishResult = PublishToDotNet(fullPathToSource, OAPPDNA, generateSelfContainedFullBinary);

                    if (publishResult != null && publishResult.Result.Item1 && !publishResult.IsError)
                        fullPathToSource = publishResult.Result.Item2;
                    else
                    {
                        result.Message = publishResult.Message;
                        result.IsError = true;
                        return result;
                    }
                }

                if (generateBinary)
                {
                    OAPPDNA.PublishedPath = Path.Combine(fullPathToPublishTo, publishedFileName);
                    OAPPDNA.PublishedToCloud = registerOnSTARNET && uploadToCloud;
                    OAPPDNA.PublishedProviderType = binaryProviderType;
                }

                if (generateSelfContainedBinary)
                {
                    OAPPDNA.MetaData["SelfContainedPublishedPath"] = Path.Combine(fullPathToPublishTo, publishedSelfContainedFileName);
                    OAPPDNA.MetaData["SelfContainedPublishedToCloud"] = registerOnSTARNET && uploadSelfContainedToCloud;
                    OAPPDNA.MetaData["SelfContainedPublishedProviderType"] = selfContainedBinaryProviderType;
                    OAPP.SelfContainedPublishedPath = Path.Combine(fullPathToPublishTo, publishedSelfContainedFileName);
                    OAPP.SelfContainedPublishedToCloud = registerOnSTARNET && uploadSelfContainedToCloud;
                    OAPP.SelfContainedPublishedProviderType = selfContainedBinaryProviderType;
                    OAPPDNA.SelfContainedPublishedPath = Path.Combine(fullPathToPublishTo, publishedSelfContainedFileName);
                    OAPPDNA.SelfContainedPublishedToCloud = registerOnSTARNET && uploadSelfContainedToCloud;
                    OAPPDNA.SelfContainedPublishedProviderType = selfContainedBinaryProviderType;
                }

                if (generateSelfContainedFullBinary)
                {
                    OAPPDNA.MetaData["SelfContainedFullPublishedPath"] = Path.Combine(fullPathToPublishTo, publishedSelfContainedFullFileName);
                    OAPPDNA.MetaData["SelfContainedFullPublishedToCloud"] = registerOnSTARNET && uploadSelfContainedFullToCloud;
                    OAPPDNA.MetaData["SelfContainedFullPublishedProviderType"] = selfContainedFullBinaryProviderType;
                    OAPP.SelfContainedFullPublishedPath = Path.Combine(fullPathToPublishTo, publishedSelfContainedFullFileName);
                    OAPP.SelfContainedFullPublishedToCloud = registerOnSTARNET && uploadSelfContainedFullToCloud;
                    OAPP.SelfContainedFullPublishedProviderType = selfContainedFullBinaryProviderType;
                    OAPPDNA.SelfContainedFullPublishedPath = Path.Combine(fullPathToPublishTo, publishedSelfContainedFullFileName);
                    OAPPDNA.SelfContainedFullPublishedToCloud = registerOnSTARNET && uploadSelfContainedFullToCloud;
                    OAPPDNA.SelfContainedFullPublishedProviderType = selfContainedFullBinaryProviderType;
                }

                if (generateSource)
                {
                    OAPPDNA.MetaData["SourcePublishedPath"] = Path.Combine(fullPathToPublishTo, publishedSourceFileName);
                    OAPPDNA.MetaData["SourcePublishedOnSTARNET"] = registerOnSTARNET && uploadSourceToSTARNET;
                    OAPPDNA.MetaData["SourcePublicOnSTARNET"] = makeSourcePublic;
                    OAPP.SourcePublishedPath = Path.Combine(fullPathToPublishTo, publishedSourceFileName);
                    OAPP.SourcePublishedOnSTARNET = registerOnSTARNET && uploadSourceToSTARNET;
                    OAPP.SourcePublicOnSTARNET = makeSourcePublic;
                    OAPPDNA.SourcePublishedPath = Path.Combine(fullPathToPublishTo, publishedSourceFileName);
                    OAPPDNA.SourcePublishedOnSTARNET = registerOnSTARNET && uploadSourceToSTARNET;
                    OAPPDNA.SourcePublicOnSTARNET = makeSourcePublic;
                }

                WriteDNA(OAPPDNA, fullPathToSource);
                RaisePublishStatusChangedEvent((OAPPDNA)OAPPDNA, STARNETHolonPublishStatus.Compressing);

                if (generateBinary)
                {
                    try
                    {
                        string publishedPath = Path.Combine(fullPathToPublishTo, "No Runtimes");

                        if (Directory.Exists(publishedPath))
                            Directory.Delete(publishedPath, true);

                        Directory.CreateDirectory(publishedPath);
                        DirectoryHelper.CopyFilesRecursively(fullPathToSource, publishedPath);
                        Directory.Delete(Path.Combine(publishedPath, "Dependencies", "STARNET", "Runtimes"), true);

                        if (!embedTemplates && Directory.Exists(Path.Combine(publishedPath, "Dependencies", "STARNET", "Templates")))
                            Directory.Delete(Path.Combine(publishedPath, "Dependencies", "STARNET", "Templates"), true);

                        if (!embedLibs && Directory.Exists(Path.Combine(publishedPath, "Dependencies", "STARNET", "Libs")))
                            Directory.Delete(Path.Combine(publishedPath, "Dependencies", "STARNET", "Libs"), true);

                        OASISResult<bool> compressedResult = GenerateCompressedFile(publishedPath, OAPPDNA.PublishedPath);

                        if (!(compressedResult != null && compressedResult.Result != null && !compressedResult.IsError))
                        {
                            result.Message = compressedResult.Message;
                            result.IsError = true;
                            return result;
                        }
                    }
                    catch (Exception e)
                    {
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured attempting to compress the {STARNETHolonUIName} files. Reason: {e}");
                        return result;
                    }
                    finally
                    {
                        //TODO: Put back in once finished testing! ;-)
                        //Directory.Delete(publishedPath, true);
                    }
                }

                if (generateSelfContainedBinary)
                {
                    try
                    {
                        string publishedPath = Path.Combine(fullPathToPublishTo, "Self Contained");

                        if (Directory.Exists(publishedPath))
                            Directory.Delete(publishedPath, true);

                        Directory.CreateDirectory(publishedPath);
                        DirectoryHelper.CopyFilesRecursively(fullPathToSource, publishedPath);

                        if (!Directory.Exists(Path.Combine(publishedPath, "Dependencies", "STARNET", "Runtimes")))
                            DirectoryHelper.CopyFilesRecursively(Path.Combine(originalFullPathToSource, "Runtimes"), publishedPath);

                        if (!embedTemplates && Directory.Exists(Path.Combine(publishedPath, "Dependencies", "STARNET", "Templates")))
                            Directory.Delete(Path.Combine(publishedPath, "Dependencies", "STARNET", "Templates"), true);

                        if (!embedLibs && Directory.Exists(Path.Combine(publishedPath, "Dependencies", "STARNET", "Libs")))
                            Directory.Delete(Path.Combine(publishedPath, "Dependencies", "STARNET", "Libs"), true);

                        OASISResult<bool> compressedResult = GenerateCompressedFile(publishedPath, OAPPDNA.SelfContainedPublishedPath);

                        if (!(compressedResult != null && compressedResult.Result != null && !compressedResult.IsError))
                        {
                            result.Message = compressedResult.Message;
                            result.IsError = true;
                            return result;
                        }
                    }
                    catch (Exception e)
                    {
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured attempting to compress the {STARNETHolonUIName} files. Reason: {e}");
                        return result;
                    }
                    finally
                    {
                        //TODO: Put back in once finished testing! ;-)
                        //Directory.Delete(publishedPath, true);
                    }
                }

                if (generateSelfContainedFullBinary)
                {
                    try
                    {
                        //if (!Directory.Exists(Path.Combine(fullPathToSource, "Runtimes")))
                        //    DirectoryHelper.CopyFilesRecursively(Path.Combine(originalFullPathToSource, "Runtimes"), fullPathToSource);

                        //OASISResult<bool> compressedResult = GenerateCompressedFile(fullPathToSource, OAPPDNA.SelfContainedFullPublishedPath);
                        ////OASISResult<bool> compressedResult = GenerateCompressedFile(fullPathToSource, OAPPDNA.MetaData["SelfContainedFullPublishedPath"].ToString());
                        ////OASISResult<bool> compressedResult = GenerateCompressedFile(fullPathToSource, OAPP.SelfContainedFullPublishedPath);

                        string publishedPath = Path.Combine(fullPathToPublishTo, "Self Contained Full");

                        if (Directory.Exists(publishedPath))
                            Directory.Delete(publishedPath, true);

                        Directory.CreateDirectory(publishedPath);
                        DirectoryHelper.CopyFilesRecursively(fullPathToSource, publishedPath);

                        if (!Directory.Exists(Path.Combine(publishedPath, "Dependencies", "STARNET", "Runtimes")))
                            DirectoryHelper.CopyFilesRecursively(Path.Combine(originalFullPathToSource, "Runtimes"), publishedPath);

                        if (!embedTemplates && Directory.Exists(Path.Combine(publishedPath, "Dependencies", "STARNET", "Templates")))
                            Directory.Delete(Path.Combine(publishedPath, "Dependencies", "STARNET", "Templates"), true);

                        if (!embedLibs && Directory.Exists(Path.Combine(publishedPath, "Dependencies", "STARNET", "Libs")))
                            Directory.Delete(Path.Combine(publishedPath, "Dependencies", "STARNET", "Libs"), true);

                        OASISResult<bool> compressedResult = GenerateCompressedFile(publishedPath, OAPPDNA.SelfContainedPublishedPath);

                        if (!(compressedResult != null && compressedResult.Result != null && !compressedResult.IsError))
                        {
                            result.Message = compressedResult.Message;
                            result.IsError = true;
                            return result;
                        }
                    }
                    catch (Exception e)
                    {
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured attempting to compress the {STARNETHolonUIName} files. Reason: {e}");
                        return result;
                    }
                    finally
                    {
                        //TODO: Put back in once finished testing! ;-)
                        //Directory.Delete(publishedPath, true);
                    }
                }

                if (generateSource)
                {
                    OASISResult<bool> generateSourceResult = GenerateSource(OAPPDNA, originalFullPathToSource, OAPP.SourcePublishedPath, fullPathToPublishTo);

                    if (!(generateSourceResult != null && generateSourceResult.Result != null && !generateSourceResult.IsError))
                        OASISErrorHandling.HandleWarning(ref result, $" Error occured calling GenerateSource. Reason: {generateSourceResult.Message}");
                }

                //TODO: Currently the filesize will NOT be in the compressed .STARNETHolon file because we dont know the size before we create it! ;-) We would need to compress it twice or edit the compressed file after to update the STARHolonDNA inside it...
                if (!string.IsNullOrEmpty(OAPPDNA.PublishedPath) && File.Exists(OAPPDNA.PublishedPath))
                    OAPPDNA.FileSize = new FileInfo(OAPPDNA.PublishedPath).Length;

                //if (!string.IsNullOrEmpty(OAPP.SelfContainedPublishedPath) && File.Exists(OAPP.SelfContainedPublishedPath))
                //    OAPP.SelfContainedFileSize = new FileInfo(OAPP.SelfContainedPublishedPath).Length;

                //if (!string.IsNullOrEmpty(OAPP.SelfContainedFullPublishedPath) && File.Exists(OAPP.SelfContainedFullPublishedPath))
                //    OAPP.SelfContainedFullFileSize = new FileInfo(OAPP.SelfContainedFullPublishedPath).Length;

                //if (!string.IsNullOrEmpty(OAPP.SourcePublishedPath) && File.Exists(OAPP.SourcePublishedPath))
                //    OAPP.SourceFileSize = new FileInfo(OAPP.SourcePublishedPath).Length;

                if (!string.IsNullOrEmpty(OAPPDNA.SelfContainedPublishedPath) && File.Exists(OAPPDNA.SelfContainedPublishedPath))
                    OAPPDNA.SelfContainedFileSize = new FileInfo(OAPPDNA.SelfContainedPublishedPath).Length;

                if (!string.IsNullOrEmpty(OAPPDNA.SelfContainedFullPublishedPath) && File.Exists(OAPPDNA.SelfContainedFullPublishedPath))
                    OAPPDNA.SelfContainedFullFileSize = new FileInfo(OAPPDNA.SelfContainedFullPublishedPath).Length;

                if (!string.IsNullOrEmpty(OAPPDNA.SourcePublishedPath) && File.Exists(OAPPDNA.SourcePublishedPath))
                    OAPPDNA.SourceFileSize = new FileInfo(OAPPDNA.SourcePublishedPath).Length;

                WriteDNA(OAPPDNA, fullPathToSource);
                validateResult.Result.STARNETDNA = OAPPDNA;

                if (registerOnSTARNET)
                {
                    if (uploadToCloud)
                    {
                        OASISResult<bool> uploadToCloudResult = await UploadToCloudAsync((OAPPDNA)OAPPDNA, publishedFileName, registerOnSTARNET, binaryProviderType);

                        if (!(uploadToCloudResult != null && uploadToCloudResult.Result && !uploadToCloudResult.IsError))
                            OASISErrorHandling.HandleWarning(ref result, $" Error occured calling UploadToCloudAsync. Reason: {uploadToCloudResult.Message}");
                    }

                    if (binaryProviderType != ProviderType.None)
                    {
                        OASISResult<OAPP> uploadToOASISResult = await UploadToOASISAsync(avatarId, (OAPPDNA)OAPPDNA, OAPPDNA.PublishedPath, registerOnSTARNET, uploadToCloud, binaryProviderType);

                        if (uploadToOASISResult != null && uploadToOASISResult.Result != null && !uploadToOASISResult.IsError)
                            result.Result = uploadToOASISResult.Result;
                        else
                            OASISErrorHandling.HandleWarning(ref result, $" Error occured calling UploadToOASISAsync. Reason: {uploadToOASISResult.Message}");
                    }
                    else
                        OAPPDNA.PublishedProviderType = ProviderType.None;

                    if (selfContainedBinaryProviderType != ProviderType.None)
                    {
                        //OASISResult<OAPP> uploadToOASISResult = await UploadToOASISAsync(avatarId, OAPPDNA, OAPPDNA.SelfContainedPublishedPath, registerOnSTARNET, uploadToCloud, selfContainedBinaryProviderType);
                        OASISResult<OAPP> uploadToOASISResult = await UploadToOASISAsync(avatarId, (OAPPDNA)OAPPDNA, OAPPDNA.MetaData["SelfContainedPublishedPath"].ToString(), registerOnSTARNET, uploadToCloud, selfContainedBinaryProviderType);

                        if (uploadToOASISResult != null && uploadToOASISResult.Result != null && !uploadToOASISResult.IsError)
                            result.Result = uploadToOASISResult.Result;
                        else
                            OASISErrorHandling.HandleWarning(ref result, $" Error occured calling UploadToOASISAsync. Reason: {uploadToOASISResult.Message}");
                    }
                    else
                        //OAPPDNA.SelfContainedPublishedProviderType = ProviderType.None;
                        OAPPDNA.MetaData["SelfContainedPublishedProviderType"] = ProviderType.None;

                    if (selfContainedFullBinaryProviderType != ProviderType.None)
                    {
                        //OASISResult<OAPP> uploadToOASISResult = await UploadToOASISAsync(avatarId, OAPPDNA, OAPPDNA.SelfContainedFullPublishedPath, registerOnSTARNET, uploadToCloud, selfContainedFullBinaryProviderType);
                        OASISResult<OAPP> uploadToOASISResult = await UploadToOASISAsync(avatarId, (OAPPDNA)OAPPDNA, OAPPDNA.MetaData["SelfContainedFullPublishedPath"].ToString(), registerOnSTARNET, uploadToCloud, selfContainedFullBinaryProviderType);

                        if (uploadToOASISResult != null && uploadToOASISResult.Result != null && !uploadToOASISResult.IsError)
                            result.Result = uploadToOASISResult.Result;
                        else
                            OASISErrorHandling.HandleWarning(ref result, $" Error occured calling UploadToOASISAsync. Reason: {uploadToOASISResult.Message}");
                    }
                    else
                        //OAPPDNA.SelfContainedFullPublishedProviderType = ProviderType.None;
                        OAPPDNA.MetaData["SelfContainedFullPublishedProviderType"] = ProviderType.None;
                }

                OASISResult<OAPP> finalResult = await FininalizePublishAsync(avatarId, validateResult.Result, edit, providerType);
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(finalResult, result);
                result.Result = finalResult.Result;
            }
            else
            {
                result.Result = validateResult.Result;
                result.Message = validateResult.Message;
                result.IsError = true;
            }

            return result;
        }


        public OASISResult<IOAPP> PublishOAPP(Guid avatarId, string fullPathToSource, string launchTarget, string fullPathToPublishTo = "", bool edit = false, bool registerOnSTARNET = true, bool dotnetPublish = true, bool generateSource = true, bool uploadSourceToSTARNET = true, bool makeSourcePublic = false, bool generateBinary = true, bool generateSelfContainedBinary = false, bool generateSelfContainedFullBinary = false, bool uploadToCloud = false, bool uploadSelfContainedToCloud = false, bool uploadSelfContainedFullToCloud = false, ProviderType providerType = ProviderType.Default, ProviderType binaryProviderType = ProviderType.IPFSOASIS, ProviderType selfContainedBinaryProviderType = ProviderType.None, ProviderType selfContainedFullBinaryProviderType = ProviderType.None, bool embedRuntimes = false, bool embedLibs = false, bool embedTemplates = false)
        {
            OASISResult<IOAPP> result = new OASISResult<IOAPP>();
            //IOAPPDNA OAPPDNA = null;
            ISTARNETDNA OAPPDNA = null;
            IOAPP OAPP = null;

            OASISResult<OAPP> validateResult = BeginPublish(avatarId, fullPathToSource, launchTarget, fullPathToPublishTo, edit, providerType);

            if (validateResult != null && validateResult.Result != null && !validateResult.IsError)
            {
                //OAPPDNA = (IOAPPDNA)validateResult.Result.STARNETDNA;
                OAPPDNA = validateResult.Result.STARNETDNA;
                string publishedFileName = string.Concat(OAPPDNA.Name, "_v", OAPPDNA.Version, ".", STARNETHolonFileExtention);
                string publishedSelfContainedFileName = string.Concat(OAPPDNA.Name, " (Self Contained)_v", OAPPDNA.Version, ".", STARNETHolonFileExtention);
                string publishedSelfContainedFullFileName = string.Concat(OAPPDNA.Name, " (Self Contained)_v", OAPPDNA.Version, ".", STARNETHolonFileExtention);
                string publishedSourceFileName = string.Concat(OAPPDNA.Name, "_v", OAPPDNA.Version, ".oappsource");

                OAPPDNA.PublishedOnSTARNET = registerOnSTARNET && (binaryProviderType != ProviderType.None || uploadToCloud || selfContainedBinaryProviderType != ProviderType.None || uploadSelfContainedToCloud || selfContainedFullBinaryProviderType != ProviderType.None || uploadSelfContainedFullToCloud);

                if (dotnetPublish || generateSelfContainedFullBinary)
                {
                    OASISResult<(bool, string)> publishResult = PublishToDotNet(fullPathToSource, OAPPDNA, generateSelfContainedFullBinary);

                    if (publishResult != null && publishResult.Result.Item1 && !publishResult.IsError)
                        fullPathToSource = publishResult.Result.Item2;
                    else
                    {
                        result.Message = publishResult.Message;
                        result.IsError = true;
                        return result;
                    }
                }

                if (generateBinary)
                {
                    OAPPDNA.PublishedPath = Path.Combine(fullPathToPublishTo, publishedFileName);
                    OAPPDNA.PublishedToCloud = registerOnSTARNET && uploadToCloud;
                    OAPPDNA.PublishedProviderType = binaryProviderType;
                }

                //if (generateSelfContainedBinary)
                //{
                //    OAPPDNA.SelfContainedPublishedPath = Path.Combine(fullPathToPublishTo, publishedSelfContainedFileName);
                //    OAPPDNA.SelfContainedPublishedToCloud = registerOnSTARNET && uploadSelfContainedToCloud;
                //    OAPPDNA.SelfContainedPublishedProviderType = selfContainedBinaryProviderType;
                //}

                //if (generateSelfContainedFullBinary)
                //{
                //    OAPPDNA.SelfContainedFullPublishedPath = Path.Combine(fullPathToPublishTo, publishedSelfContainedFullFileName);
                //    OAPPDNA.SelfContainedFullPublishedToCloud = registerOnSTARNET && uploadSelfContainedFullToCloud;
                //    OAPPDNA.SelfContainedFullPublishedProviderType = selfContainedFullBinaryProviderType;
                //}

                //if (generateSource)
                //{
                //    OAPPDNA.SourcePublishedPath = Path.Combine(fullPathToPublishTo, publishedSourceFileName);
                //    OAPPDNA.SourcePublishedOnSTARNET = registerOnSTARNET && uploadSourceToSTARNET;
                //    OAPPDNA.SourcePublicOnSTARNET = makeSourcePublic;
                //}

                if (generateSelfContainedBinary)
                {
                    OAPPDNA.MetaData["SelfContainedPublishedPath"] = Path.Combine(fullPathToPublishTo, publishedSelfContainedFileName);
                    OAPPDNA.MetaData["SelfContainedPublishedToCloud"] = registerOnSTARNET && uploadSelfContainedToCloud;
                    OAPPDNA.MetaData["SelfContainedPublishedProviderType"] = selfContainedBinaryProviderType;
                    OAPP.MetaData["SelfContainedPublishedPath"] = Path.Combine(fullPathToPublishTo, publishedSelfContainedFileName);
                    OAPP.MetaData["SelfContainedPublishedToCloud"] = registerOnSTARNET && uploadSelfContainedToCloud;
                    OAPP.MetaData["SelfContainedPublishedProviderType"] = selfContainedBinaryProviderType;
                }

                if (generateSelfContainedFullBinary)
                {
                    OAPPDNA.MetaData["SelfContainedFullPublishedPath"] = Path.Combine(fullPathToPublishTo, publishedSelfContainedFullFileName);
                    OAPPDNA.MetaData["SelfContainedFullPublishedToCloud"] = registerOnSTARNET && uploadSelfContainedFullToCloud;
                    OAPPDNA.MetaData["SelfContainedFullPublishedProviderType"] = selfContainedFullBinaryProviderType;
                    OAPP.MetaData["SelfContainedFullPublishedPath"] = Path.Combine(fullPathToPublishTo, publishedSelfContainedFullFileName);
                    OAPP.MetaData["SelfContainedFullPublishedToCloud"] = registerOnSTARNET && uploadSelfContainedFullToCloud;
                    OAPP.MetaData["SelfContainedFullPublishedProviderType"] = selfContainedFullBinaryProviderType;
                }

                if (generateSource)
                {
                    OAPPDNA.MetaData["SourcePublishedPath"] = Path.Combine(fullPathToPublishTo, publishedSourceFileName);
                    OAPPDNA.MetaData["SourcePublishedOnSTARNET"] = registerOnSTARNET && uploadSourceToSTARNET;
                    OAPPDNA.MetaData["SourcePublicOnSTARNET"] = makeSourcePublic;
                    OAPP.MetaData["SourcePublishedPath"] = Path.Combine(fullPathToPublishTo, publishedSourceFileName);
                    OAPP.MetaData["SourcePublishedOnSTARNET"] = registerOnSTARNET && uploadSourceToSTARNET;
                    OAPP.MetaData["SourcePublicOnSTARNET"] = makeSourcePublic;
                }

                WriteDNA(OAPPDNA, fullPathToSource);
                //OnOAPPPublishStatusChanged?.Invoke(this, new OAPPPublishStatusEventArgs() { OAPPDNA = OAPPDNA, Status = Enums.OAPPPublishStatus.Compressing });
                RaisePublishStatusChangedEvent((OAPPDNA)OAPPDNA, STARNETHolonPublishStatus.Compressing);

                if (generateBinary)
                {
                    OASISResult<bool> compressedResult = GenerateCompressedFile(fullPathToSource, OAPPDNA.PublishedPath);

                    if (!(compressedResult != null && compressedResult.Result != null && !compressedResult.IsError))
                    {
                        result.Message = compressedResult.Message;
                        result.IsError = true;
                        return result;
                    }
                }

                if (generateSelfContainedBinary)
                {
                    //OASISResult<bool> compressedResult = GenerateCompressedFile(fullPathToSource, OAPPDNA.SelfContainedPublishedPath);
                    OASISResult<bool> compressedResult = GenerateCompressedFile(fullPathToSource, OAPPDNA.MetaData["SelfContainedPublishedPath"].ToString());

                    if (!(compressedResult != null && compressedResult.Result != null && !compressedResult.IsError))
                    {
                        result.Message = compressedResult.Message;
                        result.IsError = true;
                        return result;
                    }
                }

                if (generateSelfContainedFullBinary)
                {
                    //OASISResult<bool> compressedResult = GenerateCompressedFile(fullPathToSource, OAPPDNA.SelfContainedFullPublishedPath);
                    OASISResult<bool> compressedResult = GenerateCompressedFile(fullPathToSource, OAPPDNA.MetaData["SelfContainedFullPublishedPath"].ToString());

                    if (!(compressedResult != null && compressedResult.Result != null && !compressedResult.IsError))
                    {
                        result.Message = compressedResult.Message;
                        result.IsError = true;
                        return result;
                    }
                }

                if (generateSource)
                {
                    OASISResult<bool> generateSourceResult = GenerateSource(OAPPDNA, fullPathToSource, OAPP.MetaData["SourcePublishedPath"].ToString(), fullPathToPublishTo);

                    if (!(generateSourceResult != null && generateSourceResult.Result != null && !generateSourceResult.IsError))
                        OASISErrorHandling.HandleWarning(ref result, $" Error occured calling GenerateSource. Reason: {generateSourceResult.Message}");
                }

                //TODO: Currently the filesize will NOT be in the compressed .STARNETHolon file because we dont know the size before we create it! ;-) We would need to compress it twice or edit the compressed file after to update the STARHolonDNA inside it...
                if (!string.IsNullOrEmpty(OAPPDNA.PublishedPath) && File.Exists(OAPPDNA.PublishedPath))
                    OAPPDNA.FileSize = new FileInfo(OAPPDNA.PublishedPath).Length;
                //OAPPDNA.FileSize = new FileInfo(OAPPDNA.PublishedPath).Length;

                WriteDNA(OAPPDNA, fullPathToSource);
                validateResult.Result.STARNETDNA = OAPPDNA;

                if (registerOnSTARNET)
                {
                    if (uploadToCloud)
                    {
                        OASISResult<bool> uploadToCloudResult = UploadToCloud((OAPPDNA)OAPPDNA, publishedFileName, registerOnSTARNET, binaryProviderType);

                        if (!(uploadToCloudResult != null && uploadToCloudResult.Result && !uploadToCloudResult.IsError))
                            OASISErrorHandling.HandleWarning(ref result, $" Error occured calling UploadToCloudAsync. Reason: {uploadToCloudResult.Message}");
                    }

                    if (binaryProviderType != ProviderType.None)
                    {
                        OASISResult<OAPP> uploadToOASISResult = UploadToOASIS(avatarId, (OAPPDNA)OAPPDNA, OAPPDNA.PublishedPath, registerOnSTARNET, uploadToCloud, binaryProviderType);

                        if (uploadToOASISResult != null && uploadToOASISResult.Result != null && !uploadToOASISResult.IsError)
                            result.Result = uploadToOASISResult.Result;
                        else
                            OASISErrorHandling.HandleWarning(ref result, $" Error occured calling UploadToOASISAsync. Reason: {uploadToOASISResult.Message}");
                    }
                    else
                        OAPPDNA.PublishedProviderType = ProviderType.None;

                    if (selfContainedBinaryProviderType != ProviderType.None)
                    {
                        //OASISResult<OAPP> uploadToOASISResult = await UploadToOASISAsync(avatarId, OAPPDNA, OAPPDNA.SelfContainedPublishedPath, registerOnSTARNET, uploadToCloud, selfContainedBinaryProviderType);
                        OASISResult<OAPP> uploadToOASISResult = UploadToOASIS(avatarId, (OAPPDNA)OAPPDNA, OAPPDNA.MetaData["SelfContainedPublishedPath"].ToString(), registerOnSTARNET, uploadToCloud, selfContainedBinaryProviderType);

                        if (uploadToOASISResult != null && uploadToOASISResult.Result != null && !uploadToOASISResult.IsError)
                            result.Result = uploadToOASISResult.Result;
                        else
                            OASISErrorHandling.HandleWarning(ref result, $" Error occured calling UploadToOASISAsync. Reason: {uploadToOASISResult.Message}");
                    }
                    else
                        //OAPPDNA.SelfContainedPublishedProviderType = ProviderType.None;
                        OAPPDNA.MetaData["SelfContainedPublishedProviderType"] = ProviderType.None;

                    if (selfContainedFullBinaryProviderType != ProviderType.None)
                    {
                        //OASISResult<OAPP> uploadToOASISResult = await UploadToOASISAsync(avatarId, OAPPDNA, OAPPDNA.SelfContainedFullPublishedPath, registerOnSTARNET, uploadToCloud, selfContainedFullBinaryProviderType);
                        OASISResult<OAPP> uploadToOASISResult = UploadToOASIS(avatarId, (OAPPDNA)OAPPDNA, OAPPDNA.MetaData["SelfContainedFullPublishedPath"].ToString(), registerOnSTARNET, uploadToCloud, selfContainedFullBinaryProviderType);

                        if (uploadToOASISResult != null && uploadToOASISResult.Result != null && !uploadToOASISResult.IsError)
                            result.Result = uploadToOASISResult.Result;
                        else
                            OASISErrorHandling.HandleWarning(ref result, $" Error occured calling UploadToOASISAsync. Reason: {uploadToOASISResult.Message}");
                    }
                    else
                        //OAPPDNA.SelfContainedFullPublishedProviderType = ProviderType.None;
                        OAPPDNA.MetaData["SelfContainedFullPublishedProviderType"] = ProviderType.None;
                }

                OASISResult<OAPP> finalResult = FininalizePublish(avatarId, validateResult.Result, edit, providerType);
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(finalResult, result);
                result.Result = finalResult.Result;
            }

            return result;
        }

        /*
        public async Task<OASISResult<IOAPP>> UnpublishOAPPAsync(Guid avatarId, IOAPP OAPP, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.UnpublishAsync(avatarId, (OAPP)OAPP, providerType));
        }

        public OASISResult<IOAPP> UnpublishOAPP(Guid avatarId, IOAPP OAPP, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.Unpublish(avatarId, (OAPP)OAPP, providerType));
        }

        public async Task<OASISResult<IOAPP>> UnpublishOAPPAsync(Guid avatarId, Guid OAPPId, int version, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.UnpublishAsync(avatarId, OAPPId, version, providerType));
        }

        public OASISResult<IOAPP> UnpublishOAPP(Guid avatarId, Guid OAPPId, int version, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.Unpublish(avatarId, OAPPId, version, providerType));
        }

        public async Task<OASISResult<IOAPP>> UnpublishOAPPAsync(Guid avatarId, ISTARNETHolonDNA OAPPDNA, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.UnpublishAsync(avatarId, OAPPDNA, providerType));
        }

        public OASISResult<IOAPP> UnpublishOAPP(Guid avatarId, ISTARNETHolonDNA OAPPDNA, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.Unpublish(avatarId, OAPPDNA, providerType));
        }

        public async Task<OASISResult<IOAPP>> RepublishOAPPAsync(Guid avatarId, IOAPP OAPP, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.RepublishAsync(avatarId, (OAPP)OAPP, providerType));
        }

        public OASISResult<IOAPP> RepublishOAPP(Guid avatarId, IOAPP OAPP, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.Republish(avatarId, (OAPP)OAPP, providerType));
        }

        public async Task<OASISResult<IOAPP>> RepublishOAPPAsync(Guid avatarId, ISTARNETHolonDNA OAPPDNA, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.RepublishAsync(avatarId, OAPPDNA, providerType));
        }

        public OASISResult<IOAPP> RepublishOAPP(Guid avatarId, ISTARNETHolonDNA OAPPDNA, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.Republish(avatarId, OAPPDNA, providerType));
        }

        public async Task<OASISResult<IOAPP>> RepublishOAPPAsync(Guid OAPPId, int version, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.RepublishAsync(avatarId, OAPPId, version, providerType));
        }

        public OASISResult<IOAPP> RepublishOAPP(Guid OAPPId, int version, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.Republish(avatarId, OAPPId, version, providerType));
        }

        public async Task<OASISResult<IOAPP>> DeactivateOAPPAsync(Guid avatarId, IOAPP OAPP, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.DeactivateAsync(avatarId, (OAPP)OAPP, providerType));
        }

        public OASISResult<IOAPP> DeactivateOAPP(Guid avatarId, IOAPP OAPP, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.Deactivate(avatarId, (OAPP)OAPP, providerType));
        }

        public async Task<OASISResult<IOAPP>> DeactivateOAPPAsync(Guid OAPPId, int version, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.DeactivateAsync(avatarId, OAPPId, version, providerType));
        }

        public OASISResult<IOAPP> DeactivateOAPP(Guid OAPPId, int version, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.Deactivate(avatarId, OAPPId, version, providerType));
        }

        public async Task<OASISResult<IOAPP>> DeactivateOAPPAsync(Guid avatarId, ISTARNETHolonDNA OAPPDNA, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.DeactivateAsync(avatarId, OAPPDNA, providerType));
        }

        public OASISResult<IOAPP> DeactivateOAPP(Guid avatarId, ISTARNETHolonDNA OAPPDNA, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.Deactivate(avatarId, OAPPDNA, providerType));
        }

        public async Task<OASISResult<IOAPP>> ActivateOAPPAsync(Guid avatarId, IOAPP OAPP, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.ActivateAsync(avatarId, (OAPP)OAPP, providerType));
        }

        public OASISResult<IOAPP> ActivateOAPP(Guid avatarId, IOAPP OAPP, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.Activate(avatarId, (OAPP)OAPP, providerType));
        }

        public async Task<OASISResult<IOAPP>> ActivateOAPPAsync(Guid avatarId, ISTARNETHolonDNA OAPPDNA, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.ActivateAsync(avatarId, OAPPDNA, providerType));
        }

        public OASISResult<IOAPP> ActivateOAPP(Guid avatarId, ISTARNETHolonDNA OAPPDNA, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.Activate(avatarId, OAPPDNA, providerType));
        }

        public async Task<OASISResult<IOAPP>> ActivateOAPPAsync(Guid avatarId, Guid OAPPId, int version, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.ActivateAsync(avatarId, OAPPId, version, providerType));
        }

        public OASISResult<IOAPP> ActivateOAPP(Guid avatarId, Guid OAPPId, int version, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.Activate(avatarId, OAPPId, version, providerType));
        }

        public async Task<OASISResult<IDownloadedOAPP>> DownloadOAPPAsync(Guid avatarId, IOAPP OAPP, string fullDownloadPath, bool reInstall = false, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.DownloadAsync(avatarId, (OAPP)OAPP, fullDownloadPath, reInstall, providerType));
        }

        public OASISResult<IDownloadedOAPP> DownloadOAPP(Guid avatarId, IOAPP OAPP, string fullDownloadPath, bool reInstall = false, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.Download(avatarId, (OAPP)OAPP, fullDownloadPath, reInstall, providerType));
        }

        public async Task<OASISResult<IInstalledOAPP>> DownloadAndInstallOAPPAsync(Guid avatarId, IOAPP OAPP, string fullInstallPath, string fullDownloadPath = "", bool createOAPPDirectory = true, bool reInstall = false, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.DownloadAndInstallAsync(avatarId, (OAPP)OAPP, fullInstallPath, fullDownloadPath, createOAPPDirectory, reInstall, providerType));
        }

        public OASISResult<IInstalledOAPP> DownloadAndInstallOAPP(Guid avatarId, IOAPP OAPP, string fullInstallPath, string fullDownloadPath = "", bool createOAPPDirectory = true, bool reInstall = false, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.DownloadAndInstall(avatarId, (OAPP)OAPP, fullInstallPath, fullDownloadPath, createOAPPDirectory, reInstall, providerType));
        }

        public async Task<OASISResult<IInstalledOAPP>> DownloadAndInstallOAPPAsync(Guid avatarId, Guid OAPPId, int version, string fullInstallPath, string fullDownloadPath = "", bool createOAPPDirectory = true, bool reInstall = false, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.DownloadAndInstallAsync(avatarId, OAPPId, version, fullInstallPath, fullDownloadPath, createOAPPDirectory, reInstall, providerType));
        }

        public async Task<OASISResult<IInstalledOAPP>> DownloadAndInstallOAPP(Guid avatarId, Guid OAPPId, int version, string fullInstallPath, string fullDownloadPath = "", bool createOAPPDirectory = true, bool reInstall = false, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.DownloadAndInstall(avatarId, OAPPId, version, fullInstallPath, fullDownloadPath, createOAPPDirectory, reInstall, providerType));
        }


        public async Task<OASISResult<IInstalledOAPP>> InstallOAPPAsync(Guid avatarId, string fullPathToPublishedOAPPFile, string fullInstallPath, bool createOAPPDirectory = true, IDownloadedOAPP downloadedOAPP = null, bool reInstall = false, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.InstallAsync(avatarId, fullPathToPublishedOAPPFile, fullInstallPath, createOAPPDirectory, downloadedOAPP, reInstall, providerType));
        }

        public OASISResult<IInstalledOAPP> InstallOAPP(Guid avatarId, string fullPathToPublishedOAPPFile, string fullInstallPath, bool createOAPPDirectory = true, IDownloadedOAPP downloadedOAPP = null, bool reInstall = false, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.Install(avatarId, fullPathToPublishedOAPPFile, fullInstallPath, createOAPPDirectory, downloadedOAPP, reInstall, providerType));
        }

        public async Task<OASISResult<IInstalledOAPP>> UninstallOAPPAsync(IInstalledOAPP installedOAPP, Guid avatarId, string errorMessage, ProviderType providerType)
        {
            return ProcessResult(await base.UninstallAsync(avatarId, (InstalledChapter)installedOAPP, errorMessage, providerType));
        }

        public OASISResult<IInstalledOAPP> UninstallOAPP(Guid avatarId, IInstalledOAPP installedOAPP, string errorMessage, ProviderType providerType)
        {
            return ProcessResult(base.Uninstall(avatarId, (InstalledChapter)installedOAPP, errorMessage, providerType));
        }

        public async Task<OASISResult<IInstalledOAPP>> UninstallOAPPAsync(Guid avatarId, Guid OAPPId, int versionSequence, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.UninstallAsync(avatarId, OAPPId, versionSequence, providerType));
        }

        public OASISResult<IInstalledOAPP> UninstallOAPP(Guid avatarId, Guid OAPPId, int versionSequence, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.Uninstall(avatarId, OAPPId, versionSequence, providerType));
        }

        public async Task<OASISResult<IInstalledOAPP>> UninstallOAPPAsync(Guid avatarId, Guid OAPPId, string version, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.UninstallAsync(avatarId, OAPPId, version, providerType));
        }

        public OASISResult<IInstalledOAPP> UninstallOAPP(Guid avatarId, Guid OAPPId, string version, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.Uninstall(avatarId, OAPPId, version, providerType));
        }

        public async Task<OASISResult<IInstalledOAPP>> UninstallOAPPAsync(Guid avatarId, string OAPPName, int versionSequence, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.UninstallAsync(avatarId, OAPPName, versionSequence, providerType));
        }

        public OASISResult<IInstalledOAPP> UninstallOAPP(Guid avatarId, string OAPPName, int versionSequence, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.Uninstall(avatarId, OAPPName, versionSequence, providerType));
        }

        public async Task<OASISResult<IInstalledOAPP>> UninstallOAPPAsync(Guid avatarId, string OAPPName, string version, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.UninstallAsync(avatarId, OAPPName, version, providerType));
        }

        public OASISResult<IInstalledOAPP> UninstallOAPP(Guid avatarId, string OAPPName, string version, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.Uninstall(avatarId, OAPPName, version, providerType));
        }

        public async Task<OASISResult<IEnumerable<IInstalledOAPP>>> ListInstalledOAPPsAsync(Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResults(await base.ListInstalledAsync(avatarId, providerType));
        }

        public OASISResult<IEnumerable<IInstalledOAPP>> ListInstalledOAPPs(Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResults(base.ListInstalled(avatarId, providerType));
        }

        public async Task<OASISResult<IEnumerable<IInstalledOAPP>>> ListUnInstalledOAPPsAsync(Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResults(await base.ListUninstalledAsync(avatarId, providerType));
        }

        public OASISResult<IEnumerable<IInstalledOAPP>> ListUnInstalledOAPPs(Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResults(base.ListUninstalled(avatarId, providerType));
        }

        public async Task<OASISResult<IEnumerable<IOAPP>>> ListUnpublishedOAPPsAsync(Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResults(await base.ListUnpublishedAsync(avatarId, providerType));
        }

        public OASISResult<IEnumerable<IOAPP>> ListUnpublishedOAPPs(Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResults(base.ListUnpublished(avatarId, providerType));
        }

        public async Task<OASISResult<IEnumerable<IOAPP>>> ListDeactivatedOAPPsAsync(Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResults(await base.ListDeactivatedAsync(avatarId, providerType));
        }

        public OASISResult<IEnumerable<IOAPP>> ListDeactivatedOAPPs(Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResults(base.ListDeactivated(avatarId, providerType));
        }

        public async Task<OASISResult<bool>> IsOAPPInstalledAsync(Guid avatarId, Guid OAPPId, int versionSequence, ProviderType providerType = ProviderType.Default)
        {
            return await base.IsInstalledAsync(avatarId, OAPPId, versionSequence, providerType);
        }

        public OASISResult<bool> IsOAPPInstalled(Guid avatarId, Guid OAPPId, int versionSequence, ProviderType providerType = ProviderType.Default)
        {
            return base.IsInstalled(avatarId, OAPPId, versionSequence, providerType);
        }

        public async Task<OASISResult<bool>> IsOAPPInstalledAsync(Guid avatarId, Guid OAPPId, string version, ProviderType providerType = ProviderType.Default)
        {
            return await base.IsInstalledAsync(avatarId, OAPPId, version, providerType);
        }

        public OASISResult<bool> IsOAPPInstalled(Guid avatarId, Guid OAPPId, string version, ProviderType providerType = ProviderType.Default)
        {
            return base.IsInstalled(avatarId, OAPPId, version, providerType);
        }

        public async Task<OASISResult<bool>> IsOAPPInstalledAsync(Guid avatarId, string OAPPName, int versionSequence, ProviderType providerType = ProviderType.Default)
        {
            return await base.IsInstalledAsync(avatarId, OAPPName, versionSequence, providerType);
        }

        public OASISResult<bool> IsOAPPInstalled(Guid avatarId, string OAPPName, int versionSequence, ProviderType providerType = ProviderType.Default)
        {
            return base.IsInstalled(avatarId, OAPPName, versionSequence, providerType);
        }

        public async Task<OASISResult<bool>> IsOAPPInstalledAsync(Guid avatarId, string OAPPName, string version, ProviderType providerType = ProviderType.Default)
        {
            return await base.IsInstalledAsync(avatarId, OAPPName, version, providerType);
        }

        public OASISResult<bool> IsOAPPInstalled(Guid avatarId, string OAPPName, string version, ProviderType providerType = ProviderType.Default)
        {
            return base.IsInstalled(avatarId, OAPPName, version, providerType);
        }

        public async Task<OASISResult<IInstalledOAPP>> LoadInstalledOAPPAsync(Guid avatarId, Guid OAPPId, int versionSequence = 0, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.LoadInstalledAsync(avatarId, OAPPId, versionSequence, providerType));
        }

        public OASISResult<IInstalledOAPP> LoadInstalledOAPP(Guid avatarId, Guid OAPPId, int versionSequence = 0, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.LoadInstalled(avatarId, OAPPId, versionSequence, providerType));
        }

        public async Task<OASISResult<IInstalledOAPP>> LoadInstalledOAPPAsync(Guid avatarId, string OAPPName, int versionSequence = 0, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.LoadInstalledAsync(avatarId, OAPPName, versionSequence, providerType));
        }

        public OASISResult<IInstalledOAPP> LoadInstalledOAPP(Guid avatarId, string OAPPName, int versionSequence = 0, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.LoadInstalled(avatarId, OAPPName, versionSequence, providerType));
        }

        public async Task<OASISResult<IInstalledOAPP>> LoadInstalledOAPPAsync(Guid avatarId, Guid OAPPId, string version, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.LoadInstalledAsync(avatarId, OAPPId, version, providerType));
        }

        public OASISResult<IInstalledOAPP> LoadInstalledOAPP(Guid avatarId, Guid OAPPId, string version, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.LoadInstalled(avatarId, OAPPId, version, providerType));
        }

        public async Task<OASISResult<IInstalledOAPP>> LoadInstalledOAPPAsync(Guid avatarId, string OAPPName, string version, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.LoadInstalledAsync(avatarId, OAPPName, version, providerType));
        }

        public OASISResult<IInstalledOAPP> LoadInstalledOAPP(Guid avatarId, string OAPPName, string version, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.LoadInstalled(avatarId, OAPPName, version, providerType));
        }

        public async Task<OASISResult<IInstalledOAPP>> LoadInstalledOAPPAsync(Guid avatarId, Guid OAPPId, bool active, int versionSequence = 0, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.LoadInstalledAsync(avatarId, OAPPId, active, versionSequence, providerType));
        }

        public OASISResult<IInstalledOAPP> LoadInstalledOAPP(Guid avatarId, Guid OAPPId, bool active, int versionSequence = 0, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.LoadInstalled(avatarId, OAPPId, active, versionSequence, providerType));
        }

        public async Task<OASISResult<IInstalledOAPP>> LoadInstalledOAPPAsync(Guid avatarId, string OAPPName, bool active, int versionSequence = 0, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.LoadInstalledAsync(avatarId, OAPPName, active, versionSequence, providerType));
        }

        public OASISResult<IInstalledOAPP> LoadInstalledOAPP(Guid avatarId, string OAPPName, bool active, int versionSequence = 0, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.LoadInstalled(avatarId, OAPPName, active, versionSequence, providerType));
        }

        public async Task<OASISResult<IInstalledOAPP>> LoadInstalledOAPPAsync(Guid avatarId, Guid OAPPId, string version, bool active, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.LoadInstalledAsync(avatarId, OAPPId, version, active, providerType));
        }

        public OASISResult<IInstalledOAPP> LoadInstalledOAPP(Guid avatarId, Guid OAPPId, string version, bool active, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.LoadInstalled(avatarId, OAPPId, version, active, providerType));
        }

        public async Task<OASISResult<IInstalledOAPP>> LoadInstalledOAPPAsync(Guid avatarId, string OAPPName, string version, bool active, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.LoadInstalledAsync(avatarId, OAPPName, version, active, providerType));
        }

        public OASISResult<IInstalledOAPP> LoadInstalledOAPP(Guid avatarId, string OAPPName, string version, bool active, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.LoadInstalled(avatarId, OAPPName, version, active, providerType));
        }

        public OASISResult<IInstalledOAPP> OpenOAPPFolder(Guid avatarId, IInstalledOAPP OAPP)
        {
            return ProcessResult(base.OpenSTARHolonFolder(avatarId, (InstalledChapter)OAPP));
        }

        public async Task<OASISResult<IInstalledOAPP>> OpenOAPPFolderAsync(Guid avatarId, Guid OAPPId, int versionSequence, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.OpenSTARHolonFolderAsync(avatarId, OAPPId, versionSequence, providerType));
        }

        public OASISResult<IInstalledOAPP> OpenOAPPFolder(Guid avatarId, Guid OAPPId, int versionSequence, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.OpenSTARHolonFolder(avatarId, OAPPId, versionSequence, providerType));
        }

        public async Task<OASISResult<IInstalledOAPP>> OpenOAPPFolderAsync(Guid avatarId, Guid OAPPId, string version, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.OpenSTARHolonFolderAsync(avatarId, OAPPId, version, providerType));
        }

        public OASISResult<IInstalledOAPP> OpenOAPPFolder(Guid avatarId, Guid OAPPId, string version, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.OpenSTARHolonFolder(avatarId, OAPPId, version, providerType));
        }

        //public async Task<OASISResult<bool>> WriteOAPPDNAAsync(ISTARNETHolonDNA OAPPDNA, string fullPathToOAPP)
        //{
        //    return await base.WritenDNAAsync(OAPPDNA, fullPathToOAPP);
        //}

        //public OASISResult<bool> WriteOAPPDNA(ISTARNETHolonDNA OAPPDNA, string fullPathToOAPP)
        //{
        //    return base.WriteSTARHolonDNA(OAPPDNA, fullPathToOAPP);
        //}

        //public async Task<OASISResult<ISTARNETHolonDNA>> ReadOAPPDNAFromSourceOrInstalledFolderAsync(string fullPathToOAPPFolder)
        //{
        //    return await base.ReadSTARHolonDNAFromSourceOrInstallFolderAsync(fullPathToOAPPFolder);
        //}

        //public OASISResult<ISTARNETHolonDNA> ReadOAPPDNAFromSourceOrInstalledFolder(string fullPathToOAPPFolder)
        //{
        //    return base.ReadSTARHolonDNAFromSourceOrInstallFolder(fullPathToOAPPFolder);
        //}

        //public async Task<OASISResult<ISTARNETHolonDNA>> ReadOAPPDNAFromPublishedOAPPFileAsync(string fullPathToOAPPFolder)
        //{
        //    return await base.ReadSTARHolonDNAFromPublishedFileAsync(fullPathToOAPPFolder);
        //}

        //public OASISResult<ISTARNETHolonDNA> ReadOAPPDNAFromPublishedOAPPFile(string fullPathToOAPPFolder)
        //{
        //    return base.ReadSTARHolonDNAFromPublishedFile(fullPathToOAPPFolder);
        //}
        */

        //private OASISResult<bool> PublishToDotNet(string fullPathToSource, IOAPPDNA OAPPDNA)
        private OASISResult<(bool, string)> PublishToDotNet(string fullPathToSource, ISTARNETDNA OAPPDNA, bool generateSelfContainedFullBinary = false)
        {
            OASISResult<(bool, string)> result = new OASISResult<(bool, string)>();

            try
            {
                string full = "";
                //TODO: Finish implementing this.
                //Process.Start("dotnet publish -c Release -r <RID> --self-contained");
                //Process.Start("dotnet publish -c Release -r win-x64 --self-contained");
                //string command = 

                //OnOAPPPublishStatusChanged?.Invoke(this, new OAPPPublishStatusEventArgs() { OAPPDNA = OAPPDNA, Status = Enums.OAPPPublishStatus.DotNetPublishing });
                RaisePublishStatusChangedEvent((OAPPDNA)OAPPDNA, STARNETHolonPublishStatus.DotNetPublishing);
                string dotnetPublishPath = Path.Combine(fullPathToSource, "dotnetPublished");

                if (generateSelfContainedFullBinary)
                    full = "--self-contained";

                Process.Start($"dotnet publish PROJECT {fullPathToSource} -c Release {full} -output {dotnetPublishPath}");
                fullPathToSource = dotnetPublishPath;
                result.Result = (true, fullPathToSource);
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"Error publishing OAPP '{OAPPDNA.Name}' to .NET. Reason: {e.Message}", e);
            }

            return result;
        }

        //private OASISResult<bool> GenerateSource(IOAPPDNA OAPPDNA, string fullPathToSource)
        private OASISResult<bool> GenerateSource(ISTARNETDNA OAPPDNA, string fullPathToSource, string fullPathToCompressedFile, string fullPathToPublishTo)
        {
            OASISResult<bool> result = new OASISResult<bool>();
            string tempPath = string.Empty;

            try
            {
                //tempPath = Path.Combine(Path.GetTempPath(), OAPPDNA.Name);
                string publishedPath = Path.Combine(fullPathToPublishTo, "Source Only");

                if (Directory.Exists(publishedPath))
                    Directory.Delete(publishedPath, true);

                Directory.CreateDirectory(publishedPath);
                DirectoryHelper.CopyFilesRecursively(fullPathToSource, publishedPath);

                if (Directory.Exists(Path.Combine(publishedPath, "bin")))
                    Directory.Delete(Path.Combine(publishedPath, "bin"), true);

                if (Directory.Exists(Path.Combine(publishedPath, "obj")))
                    Directory.Delete(Path.Combine(publishedPath, "obj"), true);

                if (Directory.Exists(Path.Combine(publishedPath, "Dependencies", "STARNET", "Runtimes")))
                    Directory.Delete(Path.Combine(publishedPath, "Dependencies", "STARNET", "Runtimes"), true);

                GenerateCompressedFile(publishedPath, fullPathToCompressedFile);
                Directory.Delete(publishedPath, true);
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"Error generating source for OAPP '{OAPPDNA.Name}'. Reason: {e.Message}", e);
            }
            finally
            {
                if (Directory.Exists(tempPath))
                    Directory.Delete(tempPath, true);
            }

            return result;
        }

        //private OASISResult<IEnumerable<IOAPP>> ProcessResults(OASISResult<IEnumerable<OAPP>> operationResult)
        //{
        //    OASISResult<IEnumerable<IOAPP>> result = new OASISResult<IEnumerable<IOAPP>>();

        //    if (operationResult != null && operationResult.Result != null && !operationResult.IsError && operationResult.Result.Count() > 0)
        //    {
        //        List<IOAPP> oappTemplates = new List<IOAPP>();

        //        foreach (IOAPP template in operationResult.Result)
        //            oappTemplates.Add(template);

        //        result.Result = oappTemplates;
        //    }

        //    OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(operationResult, result);
        //    return result;
        //}

        //private OASISResult<IEnumerable<IInstalledOAPP>> ProcessResults(OASISResult<IEnumerable<InstalledChapter>> operationResult)
        //{
        //    OASISResult<IEnumerable<IInstalledOAPP>> result = new OASISResult<IEnumerable<IInstalledOAPP>>();

        //    if (operationResult != null && operationResult.Result != null && !operationResult.IsError && operationResult.Result.Count() > 0)
        //    {
        //        List<IInstalledOAPP> oappTemplates = new List<IInstalledOAPP>();

        //        foreach (IInstalledOAPP template in operationResult.Result)
        //            oappTemplates.Add(template);

        //        result.Result = oappTemplates;
        //    }

        //    OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(operationResult, result);
        //    return result;
        //}

        private OASISResult<IOAPP> ProcessResult(OASISResult<OAPP> operationResult)
        {
            OASISResult<IOAPP> result = new OASISResult<IOAPP>();
            result.Result = operationResult.Result;
            OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(operationResult, result);
            return result;
        }

        //private OASISResult<IDownloadedOAPP> ProcessResult(OASISResult<DownloadedOAPP> operationResult)
        //{
        //    OASISResult<IDownloadedOAPP> result = new OASISResult<IDownloadedOAPP>();
        //    result.Result = operationResult.Result;
        //    OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(operationResult, result);
        //    return result;
        //}

        //private OASISResult<IInstalledOAPP> ProcessResult(OASISResult<InstalledChapter> operationResult)
        //{
        //    OASISResult<IInstalledOAPP> result = new OASISResult<IInstalledOAPP>();
        //    result.Result = operationResult.Result;
        //    OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(operationResult, result);
        //    return result;
        //}
    }
}