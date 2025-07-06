using System;
using System.IO;
using System.Diagnostics;
using System.Threading.Tasks;
using NextGenSoftware.Utilities;
using System.Collections.Generic;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.ONODE.Core.Events;
using NextGenSoftware.OASIS.API.ONODE.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.ONODE.Core.Managers.Base;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Managers;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Managers
{
    public class OAPPManager : STARNETManagerBase<OAPP, DownloadedOAPP, InstalledOAPP>, IOAPPManager
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

        public delegate void OAPPPublishStatusChanged(object sender, OAPPPublishStatusEventArgs e);
        public delegate void OAPPInstallStatusChanged(object sender, OAPPInstallStatusEventArgs e);
        public delegate void OAPPUploadStatusChanged(object sender, OAPPUploadProgressEventArgs e);
        public delegate void OAPPDownloadStatusChanged(object sender, OAPPDownloadProgressEventArgs e);

        /// <summary>
        /// Fired when there is a change in the OAPP publish status.
        /// </summary>
        public event OAPPPublishStatusChanged OnOAPPPublishStatusChanged;

        /// <summary>
        /// Fired when there is a change to the OAPP Install status.
        /// </summary>
        public event OAPPInstallStatusChanged OnOAPPInstallStatusChanged;

        /// <summary>
        /// Fired when there is a change in the OAPP upload status.
        /// </summary>
        public event OAPPUploadStatusChanged OnOAPPUploadStatusChanged;

        /// <summary>
        /// Fired when there is a change in the OAPP download status.
        /// </summary>
        public event OAPPDownloadStatusChanged OnOAPPDownloadStatusChanged;

        //public async Task<OASISResult<IOAPP>> CreateOAPPAsync(Guid avatarId, string name, string description, OAPPType OAPPType, OAPPTemplateType OAPPTemplateType, Guid OAPPTemplateId, int OAPPTemplateVersion, GenesisType genesisType, string fullPathToOAPP, ICelestialBody celestialBody = null, IEnumerable<IZome> zomes = null, bool checkIfSourcePathExists = true, ProviderType providerType = ProviderType.Default)
        //{
        //    return ProcessResult(await base.CreateAsync(avatarId, name, description, OAPPType, fullPathToOAPP, new Dictionary<string, object>()
        //    {
        //        { "OAPPTemplateType", OAPPTemplateType },
        //        { "OAPPTemplateId", OAPPTemplateId.ToString() },
        //        { "OAPPTemplateVersion", OAPPTemplateVersion.ToString() },
        //        { "GenesisType", genesisType.ToString() },
        //        { "CelestialBody", celestialBody },
        //        { "Zomes", zomes }
        //    },
        //    new OAPP()
        //    {
        //        OAPPType = OAPPType
        //    }, checkIfSourcePathExists,
        //    providerType));
        //}

        //public OASISResult<IOAPP> CreateOAPP(Guid avatarId, string name, string description, OAPPType OAPPType, OAPPTemplateType OAPPTemplateType, Guid OAPPId, GenesisType genesisType, string fullPathToOAPP, ICelestialBody celestialBody = null, IEnumerable<IZome> zomes = null, bool checkIfSourcePathExists = true, ProviderType providerType = ProviderType.Default)
        //{
        //    return ProcessResult(base.Create(avatarId, name, description, OAPPType, fullPathToOAPP, new Dictionary<string, object>()
        //    {
        //        { "OAPPTemplateType", OAPPTemplateType },
        //        { "OAPPId", OAPPId.ToString() },
        //        { "GenesisType", genesisType.ToString() },
        //        { "CelestialBody", celestialBody },
        //        { "Zomes", zomes }
        //    },
        //    new OAPP()
        //    {
        //        OAPPType = OAPPType
        //    }, checkIfSourcePathExists,
        //    providerType));
        //}

        /*
        #region COSMICManagerBase
        public async Task<OASISResult<IOAPP>> SaveOAPPAsync(Guid avatarId, IOAPP oappTemplate, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.SaveAsync(avatarId, (OAPP)oappTemplate, providerType));
        }

        public OASISResult<IOAPP> SaveOAPP(Guid avatarId, IOAPP oappTemplate, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.Save(avatarId, (OAPP)oappTemplate, providerType));
        }

        public async Task<OASISResult<IOAPP>> LoadOAPPAsync(Guid avatarId, Guid OAPPId, int version = 0, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.LoadAsync(avatarId, OAPPId, version, providerType));
        }

        public OASISResult<IOAPP> LoadOAPP(Guid avatarId, Guid OAPPId, int version = 0, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.Load(avatarId, OAPPId, version, providerType));
        }

        public async Task<OASISResult<IEnumerable<IOAPP>>> LoadAllOAPPsAsync(Guid avatarId, OAPPTemplateType OAPPTemplateType = OAPPTemplateType.All, bool showAllVersions = false, int version = 0, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResults(await base.LoadAllAsync(avatarId, OAPPTemplateType, OAPPTemplateType == OAPPTemplateType.All, showAllVersions, version, HolonType.OAPPTemplate, "OAPPTemplate", providerType));
        }

        public OASISResult<IEnumerable<IOAPP>> LoadAllOAPPs(Guid avatarId, OAPPTemplateType OAPPTemplateType = OAPPTemplateType.All, bool showAllVersions = false, int version = 0, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResults(base.LoadAll(avatarId, OAPPTemplateType, OAPPTemplateType == OAPPTemplateType.All, showAllVersions, version, HolonType.OAPPTemplate, "OAPPTemplate", providerType));
        }

        public async Task<OASISResult<IEnumerable<IOAPP>>> LoadAllOAPPsForAvatarAsync(Guid avatarId, bool showAllVersions = false, int version = 0, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResults(await base.LoadAllForAvatarAsync(avatarId, showAllVersions, version, providerType));
        }

        public OASISResult<IEnumerable<IOAPP>> LoadAllOAPPsForAvatar(Guid avatarId, bool showAllVersions = false, int version = 0, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResults(base.LoadAllForAvatar(avatarId, showAllVersions, version, providerType));
        }

        public async Task<OASISResult<IEnumerable<IOAPP>>> SearchOAPPsAsync(Guid avatarId, string searchTerm, bool searchOnlyForCurrentAvatar = true, bool showAllVersions = false, int version = 0, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResults(await base.SearchAsync(avatarId, searchTerm, searchOnlyForCurrentAvatar, showAllVersions, version, providerType));
        }

        public OASISResult<IEnumerable<IOAPP>> SearchOAPPs(Guid avatarId, string searchTerm, bool searchOnlyForCurrentAvatar = true, bool showAllVersions = false, int version = 0, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResults(base.Search(avatarId, searchTerm, searchOnlyForCurrentAvatar, showAllVersions, version, providerType));
        }

        public async Task<OASISResult<IOAPP>> DeleteOAPPAsync(Guid avatarId, Guid oappTemplateId, int version, bool softDelete = true, bool deleteDownload = true, bool deleteInstall = true, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.DeleteAsync(avatarId, oappTemplateId, version, softDelete, deleteDownload, deleteInstall, providerType));
        }

        public OASISResult<IOAPP> DeleteOAPP(Guid avatarId, Guid oappTemplateId, int version, bool softDelete = true, bool deleteDownload = true, bool deleteInstall = true, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.Delete(avatarId, oappTemplateId, version, softDelete, deleteDownload, deleteInstall, providerType));
        }

        public async Task<OASISResult<IOAPP>> DeleteOAPPAsync(Guid avatarId, IOAPP oappTemplate, int version, bool softDelete = true, bool deleteDownload = true, bool deleteInstall = true, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.DeleteAsync(avatarId, oappTemplate, version, softDelete, deleteDownload, deleteInstall, providerType));
        }

        public OASISResult<IOAPP> DeleteOAPP(Guid avatarId, IOAPP oappTemplate, int version, bool softDelete = true, bool deleteDownload = true, bool deleteInstall = true, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.Delete(avatarId, oappTemplate, version, softDelete, deleteDownload, deleteInstall, providerType));
        }
        #endregion

        public async Task<OASISResult<IEnumerable<IOAPP>>> LoadOAPPVersionsAsync(Guid OAPPId, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResults(await base.LoadVersionsAsync(OAPPId, providerType));
        }

        public OASISResult<IEnumerable<IOAPP>> LoadOAPPVersions(Guid OAPPId, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResults(base.LoadVersions(OAPPId, providerType));
        }

        public async Task<OASISResult<IOAPP>> LoadOAPPVersionAsync(Guid OAPPId, string version, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.LoadVersionAsync(OAPPId, version, providerType));
        }

        public OASISResult<IOAPP> LoadOAPPVersion(Guid OAPPId, string version, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.LoadVersion(OAPPId, version, providerType));
        }

        public async Task<OASISResult<IOAPP>> EditOAPPAsync(Guid OAPPId, ISTARNETHolonDNA newOAPPDNA, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.EditAsync(OAPPId, newOAPPDNA, avatarId, providerType));
        }

        public async Task<OASISResult<IOAPP>> EditOAPPAsync(IOAPP OAPP, ISTARNETHolonDNA newOAPPDNA, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.EditAsync(avatarId, (OAPP)OAPP, newOAPPDNA, providerType));
        }
        */

        public async Task<OASISResult<IOAPP>> PublishOAPPAsync(Guid avatarId, string fullPathToSource, string launchTarget, string fullPathToPublishTo = "", bool edit = false, bool registerOnSTARNET = true, bool dotnetPublish = true, bool generateSource = true, bool uploadSourceToSTARNET = true, bool makeSourcePublic = false, bool generateBinary = true, bool generateSelfContainedBinary = false, bool generateSelfContainedFullBinary = false, bool uploadToCloud = false, bool uploadSelfContainedToCloud = false, bool uploadSelfContainedFullToCloud = false, ProviderType providerType = ProviderType.Default, ProviderType binaryProviderType = ProviderType.IPFSOASIS, ProviderType selfContainedBinaryProviderType = ProviderType.None, ProviderType selfContainedFullBinaryProviderType = ProviderType.None)
        {
            OASISResult<IOAPP> result = new OASISResult<IOAPP>();
            IOAPPDNA OAPPDNA = null;

            OASISResult<OAPP> validateResult = await BeginPublishAsync(avatarId, fullPathToSource, launchTarget, fullPathToPublishTo, edit, providerType);

            if (validateResult != null && validateResult.Result != null && !validateResult.IsError)
            {
                OAPPDNA = (IOAPPDNA)validateResult.Result.STARNETDNA;
                string publishedFileName = string.Concat(OAPPDNA.Name, "_v", OAPPDNA.Version, ".", STARNETHolonFileExtention);
                string publishedSelfContainedFileName = string.Concat(OAPPDNA.Name, " (Self Contained)_v", OAPPDNA.Version, ".", STARNETHolonFileExtention);
                string publishedSelfContainedFullFileName = string.Concat(OAPPDNA.Name, " (Self Contained)_v", OAPPDNA.Version, ".", STARNETHolonFileExtention);
                string publishedSourceFileName = string.Concat(OAPPDNA.Name, "_v", OAPPDNA.Version, ".oappsource");

                OAPPDNA.PublishedOnSTARNET = registerOnSTARNET && (binaryProviderType != ProviderType.None || uploadToCloud || selfContainedBinaryProviderType != ProviderType.None || uploadSelfContainedToCloud || selfContainedFullBinaryProviderType != ProviderType.None || uploadSelfContainedFullToCloud);

                if (dotnetPublish)
                {
                    OASISResult<bool> publishResult = PublishToDotNet(fullPathToSource, OAPPDNA);

                    if (!(publishResult != null && publishResult.Result != null && !publishResult.IsError))
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
                    OAPPDNA.SelfContainedPublishedPath = Path.Combine(fullPathToPublishTo, publishedSelfContainedFileName);
                    OAPPDNA.SelfContainedPublishedToCloud = registerOnSTARNET && uploadSelfContainedToCloud;
                    OAPPDNA.SelfContainedPublishedProviderType = selfContainedBinaryProviderType;
                }

                if (generateSelfContainedFullBinary)
                {
                    OAPPDNA.SelfContainedFullPublishedPath = Path.Combine(fullPathToPublishTo, publishedSelfContainedFullFileName);
                    OAPPDNA.SelfContainedFullPublishedToCloud = registerOnSTARNET && uploadSelfContainedFullToCloud;
                    OAPPDNA.SelfContainedFullPublishedProviderType = selfContainedFullBinaryProviderType;
                }

                if (generateSource)
                {
                    OAPPDNA.SourcePublishedPath = Path.Combine(fullPathToPublishTo, publishedSourceFileName);
                    OAPPDNA.SourcePublishedOnSTARNET = registerOnSTARNET && uploadSourceToSTARNET;
                    OAPPDNA.SourcePublicOnSTARNET = makeSourcePublic;
                }

                WriteDNA(OAPPDNA, fullPathToSource);
                OnOAPPPublishStatusChanged?.Invoke(this, new OAPPPublishStatusEventArgs() { OAPPDNA = OAPPDNA, Status = Enums.OAPPPublishStatus.Compressing });

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
                    OASISResult<bool> compressedResult = GenerateCompressedFile(fullPathToSource, OAPPDNA.SelfContainedPublishedPath);

                    if (!(compressedResult != null && compressedResult.Result != null && !compressedResult.IsError))
                    {
                        result.Message = compressedResult.Message;
                        result.IsError = true;
                        return result;
                    }
                }

                if (generateSelfContainedFullBinary)
                {
                    OASISResult<bool> compressedResult = GenerateCompressedFile(fullPathToSource, OAPPDNA.SelfContainedFullPublishedPath);

                    if (!(compressedResult != null && compressedResult.Result != null && !compressedResult.IsError))
                    {
                        result.Message = compressedResult.Message;
                        result.IsError = true;
                        return result;
                    }
                }

                if (generateSource)
                {
                    OASISResult<bool> generateSourceResult = GenerateSource(OAPPDNA, fullPathToSource);

                    if (!(generateSourceResult != null && generateSourceResult.Result != null && !generateSourceResult.IsError))
                        OASISErrorHandling.HandleWarning(ref result, $" Error occured calling GenerateSource. Reason: {generateSourceResult.Message}");
                }

                //TODO: Currently the filesize will NOT be in the compressed .STARNETHolon file because we dont know the size before we create it! ;-) We would need to compress it twice or edit the compressed file after to update the STARHolonDNA inside it...
                if (!string.IsNullOrEmpty(OAPPDNA.PublishedPath) && File.Exists(OAPPDNA.PublishedPath))
                    OAPPDNA.FileSize = new FileInfo(OAPPDNA.PublishedPath).Length;

                WriteDNA(OAPPDNA, fullPathToSource);
                validateResult.Result.STARNETDNA = OAPPDNA;

                if (registerOnSTARNET)
                {
                    if (uploadToCloud)
                    {
                        OASISResult<bool> uploadToCloudResult = await UploadToCloudAsync(OAPPDNA, publishedFileName, registerOnSTARNET, binaryProviderType);

                        if (!(uploadToCloudResult != null && uploadToCloudResult.Result && !uploadToCloudResult.IsError))
                            OASISErrorHandling.HandleWarning(ref result, $" Error occured calling UploadToCloudAsync. Reason: {uploadToCloudResult.Message}");
                    }

                    if (binaryProviderType != ProviderType.None)
                    {
                        OASISResult<OAPP> uploadToOASISResult = await UploadToOASISAsync(avatarId, OAPPDNA, OAPPDNA.PublishedPath, registerOnSTARNET, uploadToCloud, binaryProviderType);

                        if (uploadToOASISResult != null && uploadToOASISResult.Result != null && !uploadToOASISResult.IsError)
                            result.Result = uploadToOASISResult.Result;
                        else
                            OASISErrorHandling.HandleWarning(ref result, $" Error occured calling UploadToOASISAsync. Reason: {uploadToOASISResult.Message}");
                    }
                    else
                        OAPPDNA.PublishedProviderType = ProviderType.None;

                    if (selfContainedBinaryProviderType != ProviderType.None)
                    {
                        OASISResult<OAPP> uploadToOASISResult = await UploadToOASISAsync(avatarId, OAPPDNA, OAPPDNA.SelfContainedPublishedPath, registerOnSTARNET, uploadToCloud, selfContainedBinaryProviderType);

                        if (uploadToOASISResult != null && uploadToOASISResult.Result != null && !uploadToOASISResult.IsError)
                            result.Result = uploadToOASISResult.Result;
                        else
                            OASISErrorHandling.HandleWarning(ref result, $" Error occured calling UploadToOASISAsync. Reason: {uploadToOASISResult.Message}");
                    }
                    else
                        OAPPDNA.SelfContainedPublishedProviderType = ProviderType.None;

                    if (selfContainedFullBinaryProviderType != ProviderType.None)
                    {
                        OASISResult<OAPP> uploadToOASISResult = await UploadToOASISAsync(avatarId, OAPPDNA, OAPPDNA.SelfContainedFullPublishedPath, registerOnSTARNET, uploadToCloud, selfContainedFullBinaryProviderType);

                        if (uploadToOASISResult != null && uploadToOASISResult.Result != null && !uploadToOASISResult.IsError)
                            result.Result = uploadToOASISResult.Result;
                        else
                            OASISErrorHandling.HandleWarning(ref result, $" Error occured calling UploadToOASISAsync. Reason: {uploadToOASISResult.Message}");
                    }
                    else
                        OAPPDNA.SelfContainedFullPublishedProviderType = ProviderType.None;
                }

                OASISResult<OAPP> finalResult = await FininalizePublishAsync(avatarId, validateResult.Result, edit, providerType);
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(finalResult, result);
                result.Result = finalResult.Result;
            }

            return result;
        }

        public OASISResult<IOAPP> PublishOAPP(Guid avatarId, string fullPathToSource, string launchTarget, string fullPathToPublishTo = "", bool edit = false, bool registerOnSTARNET = true, bool dotnetPublish = true, bool generateSource = true, bool uploadSourceToSTARNET = true, bool makeSourcePublic = false, bool generateBinary = true, bool generateSelfContainedBinary = false, bool generateSelfContainedFullBinary = false, bool uploadToCloud = false, bool uploadSelfContainedToCloud = false, bool uploadSelfContainedFullToCloud = false, ProviderType providerType = ProviderType.Default, ProviderType binaryProviderType = ProviderType.IPFSOASIS, ProviderType selfContainedBinaryProviderType = ProviderType.None, ProviderType selfContainedFullBinaryProviderType = ProviderType.None)
        {
            OASISResult<IOAPP> result = new OASISResult<IOAPP>();
            IOAPPDNA OAPPDNA = null;

            OASISResult<OAPP> validateResult = BeginPublish(avatarId, fullPathToSource, launchTarget, fullPathToPublishTo, edit, providerType);

            if (validateResult != null && validateResult.Result != null && !validateResult.IsError)
            {
                OAPPDNA = (IOAPPDNA)validateResult.Result.STARNETDNA;
                string publishedFileName = string.Concat(OAPPDNA.Name, "_v", OAPPDNA.Version, ".", STARNETHolonFileExtention);
                string publishedSelfContainedFileName = string.Concat(OAPPDNA.Name, " (Self Contained)_v", OAPPDNA.Version, ".", STARNETHolonFileExtention);
                string publishedSelfContainedFullFileName = string.Concat(OAPPDNA.Name, " (Self Contained)_v", OAPPDNA.Version, ".", STARNETHolonFileExtention);
                string publishedSourceFileName = string.Concat(OAPPDNA.Name, "_v", OAPPDNA.Version, ".oappsource");

                OAPPDNA.PublishedOnSTARNET = registerOnSTARNET && (binaryProviderType != ProviderType.None || uploadToCloud || selfContainedBinaryProviderType != ProviderType.None || uploadSelfContainedToCloud || selfContainedFullBinaryProviderType != ProviderType.None || uploadSelfContainedFullToCloud);

                if (dotnetPublish)
                {
                    OASISResult<bool> publishResult = PublishToDotNet(fullPathToSource, OAPPDNA);

                    if (!(publishResult != null && publishResult.Result != null && !publishResult.IsError))
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
                    OAPPDNA.SelfContainedPublishedPath = Path.Combine(fullPathToPublishTo, publishedSelfContainedFileName);
                    OAPPDNA.SelfContainedPublishedToCloud = registerOnSTARNET && uploadSelfContainedToCloud;
                    OAPPDNA.SelfContainedPublishedProviderType = selfContainedBinaryProviderType;
                }

                if (generateSelfContainedFullBinary)
                {
                    OAPPDNA.SelfContainedFullPublishedPath = Path.Combine(fullPathToPublishTo, publishedSelfContainedFullFileName);
                    OAPPDNA.SelfContainedFullPublishedToCloud = registerOnSTARNET && uploadSelfContainedFullToCloud;
                    OAPPDNA.SelfContainedFullPublishedProviderType = selfContainedFullBinaryProviderType;
                }

                if (generateSource)
                {
                    OAPPDNA.SourcePublishedPath = Path.Combine(fullPathToPublishTo, publishedSourceFileName);
                    OAPPDNA.SourcePublishedOnSTARNET = registerOnSTARNET && uploadSourceToSTARNET;
                    OAPPDNA.SourcePublicOnSTARNET = makeSourcePublic;
                }

                WriteDNA(OAPPDNA, fullPathToSource);
                OnOAPPPublishStatusChanged?.Invoke(this, new OAPPPublishStatusEventArgs() { OAPPDNA = OAPPDNA, Status = Enums.OAPPPublishStatus.Compressing });

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
                    OASISResult<bool> compressedResult = GenerateCompressedFile(fullPathToSource, OAPPDNA.SelfContainedPublishedPath);

                    if (!(compressedResult != null && compressedResult.Result != null && !compressedResult.IsError))
                    {
                        result.Message = compressedResult.Message;
                        result.IsError = true;
                        return result;
                    }
                }

                if (generateSelfContainedFullBinary)
                {
                    OASISResult<bool> compressedResult = GenerateCompressedFile(fullPathToSource, OAPPDNA.SelfContainedFullPublishedPath);

                    if (!(compressedResult != null && compressedResult.Result != null && !compressedResult.IsError))
                    {
                        result.Message = compressedResult.Message;
                        result.IsError = true;
                        return result;
                    }
                }

                if (generateSource)
                {
                    OASISResult<bool> generateSourceResult = GenerateSource(OAPPDNA, fullPathToSource);

                    if (!(generateSourceResult != null && generateSourceResult.Result != null && !generateSourceResult.IsError))
                        OASISErrorHandling.HandleWarning(ref result, $" Error occured calling GenerateSource. Reason: {generateSourceResult.Message}");
                }

                //TODO: Currently the filesize will NOT be in the compressed .STARNETHolon file because we dont know the size before we create it! ;-) We would need to compress it twice or edit the compressed file after to update the STARHolonDNA inside it...
                if (!string.IsNullOrEmpty(OAPPDNA.PublishedPath) && File.Exists(OAPPDNA.PublishedPath))
                    OAPPDNA.FileSize = new FileInfo(OAPPDNA.PublishedPath).Length;

                WriteDNA(OAPPDNA, fullPathToSource);
                validateResult.Result.STARNETDNA = OAPPDNA;

                if (registerOnSTARNET)
                {
                    if (uploadToCloud)
                    {
                        OASISResult<bool> uploadToCloudResult = UploadToCloud(OAPPDNA, publishedFileName, registerOnSTARNET, binaryProviderType);

                        if (!(uploadToCloudResult != null && uploadToCloudResult.Result && !uploadToCloudResult.IsError))
                            OASISErrorHandling.HandleWarning(ref result, $" Error occured calling UploadToCloud. Reason: {uploadToCloudResult.Message}");
                    }

                    if (binaryProviderType != ProviderType.None)
                    {
                        OASISResult<OAPP> uploadToOASISResult = UploadToOASIS(avatarId, OAPPDNA, OAPPDNA.PublishedPath, registerOnSTARNET, uploadToCloud, binaryProviderType);

                        if (uploadToOASISResult != null && uploadToOASISResult.Result != null && !uploadToOASISResult.IsError)
                            result.Result = uploadToOASISResult.Result;
                        else
                            OASISErrorHandling.HandleWarning(ref result, $" Error occured calling UploadToOASISAsync. Reason: {uploadToOASISResult.Message}");
                    }
                    else
                        OAPPDNA.PublishedProviderType = ProviderType.None;

                    if (selfContainedBinaryProviderType != ProviderType.None)
                    {
                        OASISResult<OAPP> uploadToOASISResult = UploadToOASIS(avatarId, OAPPDNA, OAPPDNA.SelfContainedPublishedPath, registerOnSTARNET, uploadToCloud, selfContainedBinaryProviderType);

                        if (uploadToOASISResult != null && uploadToOASISResult.Result != null && !uploadToOASISResult.IsError)
                            result.Result = uploadToOASISResult.Result;
                        else
                            OASISErrorHandling.HandleWarning(ref result, $" Error occured calling UploadToOASISAsync. Reason: {uploadToOASISResult.Message}");
                    }
                    else
                        OAPPDNA.SelfContainedPublishedProviderType = ProviderType.None;

                    if (selfContainedFullBinaryProviderType != ProviderType.None)
                    {
                        OASISResult<OAPP> uploadToOASISResult = UploadToOASIS(avatarId, OAPPDNA, OAPPDNA.SelfContainedFullPublishedPath, registerOnSTARNET, uploadToCloud, selfContainedFullBinaryProviderType);

                        if (uploadToOASISResult != null && uploadToOASISResult.Result != null && !uploadToOASISResult.IsError)
                            result.Result = uploadToOASISResult.Result;
                        else
                            OASISErrorHandling.HandleWarning(ref result, $" Error occured calling UploadToOASISAsync. Reason: {uploadToOASISResult.Message}");
                    }
                    else
                        OAPPDNA.SelfContainedFullPublishedProviderType = ProviderType.None;
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

        private OASISResult<bool> PublishToDotNet(string fullPathToSource, IOAPPDNA OAPPDNA)
        {
            OASISResult<bool> result = new OASISResult<bool>();

            try
            {
                //TODO: Finish implementing this.
                //Process.Start("dotnet publish -c Release -r <RID> --self-contained");
                //Process.Start("dotnet publish -c Release -r win-x64 --self-contained");
                //string command = 

                OnOAPPPublishStatusChanged?.Invoke(this, new OAPPPublishStatusEventArgs() { OAPPDNA = OAPPDNA, Status = Enums.OAPPPublishStatus.DotNetPublishing });
                string dotnetPublishPath = Path.Combine(fullPathToSource, "dotnetPublished");
                Process.Start($"dotnet publish PROJECT {fullPathToSource} -c Release --self-contained -output {dotnetPublishPath}");
                fullPathToSource = dotnetPublishPath;

                //"bin\\Release\\net8.0\\";
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"Error publishing OAPP '{OAPPDNA.Name}' to .NET. Reason: {e.Message}", e);
            }

            return result;
        }

        private OASISResult<bool> GenerateSource(IOAPPDNA OAPPDNA, string fullPathToSource)
        {
            OASISResult<bool> result = new OASISResult<bool>();
            string tempPath = string.Empty;

            try
            {
                tempPath = Path.Combine(Path.GetTempPath(), OAPPDNA.Name);

                if (Directory.Exists(tempPath))
                    Directory.Delete(tempPath, true);

                DirectoryHelper.CopyFilesRecursively(fullPathToSource, tempPath);
                Directory.Delete(Path.Combine(tempPath, "bin"), true);
                Directory.Delete(Path.Combine(tempPath, "obj"), true);

                GenerateCompressedFile(fullPathToSource, OAPPDNA.SourcePublishedPath);
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