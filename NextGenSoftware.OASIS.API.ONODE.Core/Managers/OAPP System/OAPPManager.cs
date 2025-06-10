using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NextGenSoftware.Utilities;
using System.Collections.Generic;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.ONODE.Core.Events;
using NextGenSoftware.OASIS.API.ONODE.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Holons;
using System.Diagnostics;
using NextGenSoftware.OASIS.API.ONODE.Core.Managers.Base;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Managers
{
    public class OAPPManager : STARManagerBase<OAPP, DownloadedOAPP, InstalledOAPP>
    {
        public OAPPManager(Guid avatarId, OASISDNA OASISDNA = null) : base(avatarId, OASISDNA,
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

        public OAPPManager(IOASISStorageProvider OASISStorageProvider, Guid avatarId, OASISDNA OASISDNA = null) : base(OASISStorageProvider, avatarId, OASISDNA,
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

        public async Task<OASISResult<IOAPP>> CreateOAPPAsync(Guid avatarId, string name, string description, OAPPType OAPPType, OAPPTemplateType OAPPTemplateType, Guid OAPPId, GenesisType genesisType, string fullPathToOAPP, ICelestialBody celestialBody = null, IEnumerable<IZome> zomes = null, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.CreateAsync(avatarId, name, description, OAPPType, fullPathToOAPP, new Dictionary<string, object>()
            { 
                { "OAPPTemplateType", OAPPTemplateType },
                { "OAPPId", OAPPId.ToString() },
                { "GenesisType", genesisType.ToString() },
                { "CelestialBody", celestialBody },
                { "Zomes", zomes }
            }, 
            providerType));
        }

        public OASISResult<IOAPP> CreateOAPP(Guid avatarId, string name, string description, OAPPType OAPPType, OAPPTemplateType OAPPTemplateType, Guid OAPPId, GenesisType genesisType, string fullPathToOAPP, ICelestialBody celestialBody = null, IEnumerable<IZome> zomes = null, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.Create(avatarId, name, description, OAPPType, fullPathToOAPP, new Dictionary<string, object>()
            {
                { "OAPPTemplateType", OAPPTemplateType },
                { "OAPPId", OAPPId.ToString() },
                { "GenesisType", genesisType.ToString() },
                { "CelestialBody", celestialBody },
                { "Zomes", zomes }
            },
            providerType));
        }

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
            return ProcessResults(await base.SearchAsync(avatarId, searchTerm, HolonType.OAPP, searchOnlyForCurrentAvatar, showAllVersions, version, providerType));
        }

        public OASISResult<IEnumerable<IOAPP>> SearchOAPPs(Guid avatarId, string searchTerm, bool searchOnlyForCurrentAvatar = true, bool showAllVersions = false, int version = 0, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResults(base.Search(avatarId, searchTerm, HolonType.OAPP, searchOnlyForCurrentAvatar, showAllVersions, version, providerType));
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

        public async Task<OASISResult<IOAPP>> EditOAPPAsync(Guid OAPPId, ISTARHolonDNA newOAPPDNA, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.EditAsync(OAPPId, newOAPPDNA, avatarId, providerType));
        }

        public async Task<OASISResult<IOAPP>> EditOAPPAsync(IOAPP OAPP, ISTARHolonDNA newOAPPDNA, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.EditAsync(avatarId, (OAPP)OAPP, newOAPPDNA, providerType));
        }

        //public async Task<OASISResult<IOAPP>> PublishOAPPAsync(Guid avatarId, string fullPathToSource, string launchTarget, string fullPathToPublishTo = "", bool edit = false, bool registerOnSTARNET = true, bool dotnetPublish = true, bool generateSource = true, bool uploadSourceToSTARNET = true, bool makeSourcePublic = false, bool generateBinary = true, bool generateSelfContainedBinary = false, bool generateSelfContainedFullBinary = false, bool uploadToCloud = false, bool uploadSelfContainedToCloud = false, bool uploadSelfContainedFullToCloud = false, ProviderType providerType = ProviderType.Default, ProviderType binaryProviderType = ProviderType.IPFSOASIS, ProviderType selfContainedBinaryProviderType = ProviderType.None, ProviderType selfContainedFullBinaryProviderType = ProviderType.None)
        //{
        //    OASISResult<IOAPP> result = new OASISResult<IOAPP>();
        //    string errorMessage = "Error occured in OAPPManager.PublishAsync. Reason: ";
        //    IOAPPDNA OAPPDNA = null;
        //    string tempPath = "";

        //    try
        //    {
        //        OASISResult<IOAPPDNA> readSTARHolonDNAResult = await ReadDNAFromSourceOrInstallFolderAsync<IOAPPDNA>(fullPathToSource);

        //        if (readSTARHolonDNAResult != null && !readSTARHolonDNAResult.IsError && readSTARHolonDNAResult.Result != null)
        //        {
        //            OAPPDNA = readSTARHolonDNAResult.Result;
        //            OnOAPPPublishStatusChanged?.Invoke(this, new OAPPPublishStatusEventArgs() { OAPPDNA = OAPPDNA, Status = Enums.OAPPPublishStatus.Packaging });
        //            OASISResult<IAvatar> loadAvatarResult = await AvatarManager.Instance.LoadAvatarAsync(avatarId, false, true, providerType);

        //            if (loadAvatarResult != null && loadAvatarResult.Result != null && !loadAvatarResult.IsError)
        //            {
        //                //Load latest version.
        //                OASISResult<OAPP> loadOAPPResult = await LoadAsync(avatarId, OAPPDNA.Id);

        //                if (loadOAPPResult != null && loadOAPPResult.Result != null && !loadOAPPResult.IsError)
        //                {
        //                    if (loadOAPPResult.Result.STARHolonDNA.CreatedByAvatarId == avatarId)
        //                    {
        //                        OASISResult<bool> validateVersionResult = ValidateVersion(OAPPDNA.Version, loadOAPPResult.Result.STARHolonDNA.Version, fullPathToSource, OAPPDNA.PublishedOn == DateTime.MinValue, edit);

        //                        if (validateVersionResult != null && validateVersionResult.Result && !validateVersionResult.IsError)
        //                        {
        //                            //TODO: Maybe add check to make sure the DNA has not been tampered with?
        //                            loadOAPPResult.Result.STARHolonDNA.Version = OAPPDNA.Version; //Set the new version set in the DNA (JSON file).
        //                            OAPPDNA = (IOAPPDNA)loadOAPPResult.Result.STARHolonDNA; //Make sure it has not been tampered with by using the stored version.

        //                            if (!edit)
        //                            {
        //                                OAPPDNA.VersionSequence++;
        //                                OAPPDNA.NumberOfVersions++;
        //                            }

        //                            OAPPDNA.LaunchTarget = launchTarget;

        //                            string publishedFileName = string.Concat(OAPPDNA.Name, "_v", OAPPDNA.Version, ".", STARHolonFileExtention);
        //                            string publishedSelfContainedFileName = string.Concat(OAPPDNA.Name, " (Self Contained)_v", OAPPDNA.Version, ".", STARHolonFileExtention);
        //                            string publishedSelfContainedFullFileName = string.Concat(OAPPDNA.Name, " (Self Contained)_v", OAPPDNA.Version, ".", STARHolonFileExtention);
        //                            string publishedSourceFileName = string.Concat(OAPPDNA.Name, "_v", OAPPDNA.Version, ".oappsource");

        //                            if (string.IsNullOrEmpty(fullPathToPublishTo))
        //                                fullPathToPublishTo = Path.Combine(fullPathToSource, "Published");

        //                            if (!Directory.Exists(fullPathToPublishTo))
        //                                Directory.CreateDirectory(fullPathToPublishTo);

        //                            if (!edit)
        //                            {
        //                                OAPPDNA.PublishedOn = DateTime.Now;
        //                                OAPPDNA.PublishedByAvatarId = avatarId;
        //                                OAPPDNA.PublishedByAvatarUsername = loadAvatarResult.Result.Username;
        //                            }
        //                            else
        //                            {
        //                                OAPPDNA.ModifiedOn = DateTime.Now;
        //                                OAPPDNA.ModifiedByAvatarId = avatarId;
        //                                OAPPDNA.ModifiedByAvatarUsername = loadAvatarResult.Result.Username;
        //                            }

        //                            OAPPDNA.PublishedOnSTARNET = registerOnSTARNET && (binaryProviderType != ProviderType.None || uploadToCloud);

        //                            if (generateBinary)
        //                            {
        //                                OAPPDNA.PublishedPath = Path.Combine(fullPathToPublishTo, publishedFileName);
        //                                OAPPDNA.PublishedToCloud = registerOnSTARNET && uploadToCloud;
        //                                OAPPDNA.PublishedProviderType = binaryProviderType;
        //                            }

        //                            if (generateSelfContainedBinary)
        //                            {
        //                                OAPPDNA.SelfContainedPublishedPath = Path.Combine(fullPathToPublishTo, publishedSelfContainedFileName);
        //                                OAPPDNA.SelfContainedPublishedToCloud = registerOnSTARNET && uploadSelfContainedToCloud;
        //                                OAPPDNA.SelfContainedPublishedProviderType = selfContainedBinaryProviderType;
        //                            }

        //                            if (generateSelfContainedFullBinary)
        //                            {
        //                                OAPPDNA.SelfContainedFullPublishedPath = Path.Combine(fullPathToPublishTo, publishedSelfContainedFullFileName);
        //                                OAPPDNA.SelfContainedFullPublishedToCloud = registerOnSTARNET && uploadSelfContainedFullToCloud;
        //                                OAPPDNA.SelfContainedFullPublishedProviderType = selfContainedFullBinaryProviderType;
        //                            }

        //                            if (generateSource)
        //                            {
        //                                OAPPDNA.SourcePublishedPath = Path.Combine(fullPathToPublishTo, publishedSourceFileName);
        //                                OAPPDNA.SourcePublishedOnSTARNET = registerOnSTARNET && uploadSourceToSTARNET;
        //                                OAPPDNA.SourcePublicOnSTARNET = makeSourcePublic;
        //                            }

        //                            WriteDNA(OAPPDNA, fullPathToSource);
        //                            OnOAPPPublishStatusChanged?.Invoke(this, new OAPPPublishStatusEventArgs() { OAPPDNA = OAPPDNA, Status = Enums.OAPPPublishStatus.Compressing });

        //                            if (generateBinary)
        //                                GenerateCompressedFile(fullPathToSource, OAPPDNA.PublishedPath);

        //                            if (generateSelfContainedBinary)
        //                                GenerateCompressedFile(fullPathToSource, OAPPDNA.SelfContainedPublishedPath);

        //                            if (generateSelfContainedFullBinary)
        //                                GenerateCompressedFile(fullPathToSource, OAPPDNA.SelfContainedFullPublishedPath);

        //                            if (generateSource)
        //                            {
        //                                tempPath = Path.Combine(Path.GetTempPath(), OAPPDNA.Name);

        //                                if (Directory.Exists(tempPath))
        //                                    Directory.Delete(tempPath, true);

        //                                DirectoryHelper.CopyFilesRecursively(fullPathToSource, tempPath);
        //                                Directory.Delete(Path.Combine(tempPath, "bin"), true);
        //                                Directory.Delete(Path.Combine(tempPath, "obj"), true);

        //                                GenerateCompressedFile(fullPathToSource, OAPPDNA.SourcePublishedPath);
        //                            }

        //                            //TODO: Currently the filesize will NOT be in the compressed .STARHolon file because we dont know the size before we create it! ;-) We would need to compress it twice or edit the compressed file after to update the STARHolonDNA inside it...
        //                            if (!string.IsNullOrEmpty(OAPPDNA.PublishedPath) && File.Exists(OAPPDNA.PublishedPath))
        //                                OAPPDNA.FileSize = new FileInfo(OAPPDNA.PublishedPath).Length;

        //                            WriteDNA(OAPPDNA, fullPathToSource);
        //                            loadOAPPResult.Result.STARHolonDNA = OAPPDNA;

        //                            if (registerOnSTARNET)
        //                            {
        //                                if (uploadToCloud)
        //                                    result = await UploadToCloudAsync<IOAPP>(OAPPDNA, publishedFileName, registerOnSTARNET, binaryProviderType);

        //                                if (binaryProviderType != ProviderType.None)
        //                                {
        //                                    result.Result.PublishedSTARHolon = File.ReadAllBytes(OAPPDNA.PublishedPath);

        //                                    //TODO: We could use HoloOASIS and other large file storage providers in future...
        //                                    OASISResult<OAPP> saveLargeSTARHolonResult = await SaveAsync(avatarId, (OAPP)result.Result, binaryProviderType);

        //                                    if (saveLargeSTARHolonResult != null && !saveLargeSTARHolonResult.IsError && saveLargeSTARHolonResult.Result != null)
        //                                    {
        //                                        result.Result = saveLargeSTARHolonResult.Result;
        //                                        result.IsSaved = true;
        //                                    }
        //                                    else
        //                                    {
        //                                        OASISErrorHandling.HandleWarning(ref result, $" Error occured saving the published {STARHolonUIName} binary to STARNET using the {binaryProviderType} provider. Reason: {saveLargeSTARHolonResult.Message}");
        //                                        OAPPDNA.PublishedOnSTARNET = registerOnSTARNET && uploadToCloud;
        //                                        OAPPDNA.PublishedProviderType = ProviderType.None;
        //                                    }
        //                                }
        //                                else
        //                                    OAPPDNA.PublishedProviderType = ProviderType.None;
        //                            }

        //                            //If its not the first version.
        //                            if (OAPPDNA.Version != "1.0.0" && !edit)
        //                            {
        //                                //If the ID has not been set then store the original id now.
        //                                if (!loadOAPPResult.Result.MetaData.ContainsKey(STARHolonIdName))
        //                                    loadOAPPResult.Result.MetaData[STARHolonIdName] = loadOAPPResult.Result.Id;

        //                                loadOAPPResult.Result.MetaData["Version"] = loadOAPPResult.Result.STARHolonDNA.Version;
        //                                loadOAPPResult.Result.MetaData["VersionSequence"] = loadOAPPResult.Result.STARHolonDNA.VersionSequence;

        //                                //Blank fields so it creates a new version.
        //                                loadOAPPResult.Result.Id = Guid.Empty;
        //                                loadOAPPResult.Result.ProviderUniqueStorageKey.Clear();
        //                                loadOAPPResult.Result.CreatedDate = DateTime.MinValue;
        //                                loadOAPPResult.Result.ModifiedDate = DateTime.MinValue;
        //                                loadOAPPResult.Result.CreatedByAvatarId = Guid.Empty;
        //                                loadOAPPResult.Result.ModifiedByAvatarId = Guid.Empty;
        //                                loadOAPPResult.Result.STARHolonDNA.Downloads = 0;
        //                                loadOAPPResult.Result.STARHolonDNA.Installs = 0;
        //                            }

        //                            OASISResult<OAPP> saveSTARHolonResult = await SaveAsync(avatarId, loadOAPPResult.Result, providerType);

        //                            if (saveSTARHolonResult != null && !saveSTARHolonResult.IsError && saveSTARHolonResult.Result != null)
        //                            {
        //                                saveSTARHolonResult = await UpdateNumberOfVersionCountsAsync(avatarId, saveSTARHolonResult, errorMessage, providerType);
        //                                result.IsSaved = true;
        //                                result.Result = saveSTARHolonResult.Result; //TODO:Check if this is needed?

        //                                if (readSTARHolonDNAResult.Result.STARODKVersion != OASISBootLoader.OASISBootLoader.STARODKVersion)
        //                                    OASISErrorHandling.HandleWarning(ref result, $" The STAR ODK Version {readSTARHolonDNAResult.Result.STARODKVersion} does not match the current version {OASISBootLoader.OASISBootLoader.STARODKVersion}. This may lead to issues, it is recommended to make sure the versions match.");

        //                                if (readSTARHolonDNAResult.Result.OASISVersion != OASISBootLoader.OASISBootLoader.OASISVersion)
        //                                    OASISErrorHandling.HandleWarning(ref result, $" The OASIS Version {readSTARHolonDNAResult.Result.OASISVersion} does not match the current version {OASISBootLoader.OASISBootLoader.OASISVersion}. This may lead to issues, it is recommended to make sure the versions match.");

        //                                if (readSTARHolonDNAResult.Result.COSMICVersion != OASISBootLoader.OASISBootLoader.COSMICVersion)
        //                                    OASISErrorHandling.HandleWarning(ref result, $" The COSMIC Version {readSTARHolonDNAResult.Result.COSMICVersion} does not match the current version {OASISBootLoader.OASISBootLoader.COSMICVersion}. This may lead to issues, it is recommended to make sure the versions match.");

        //                                if (result.IsWarning)
        //                                    result.Message = $"{STARHolonUIName} successfully published but there were {result.WarningCount} warnings:\n\n {OASISResultHelper.BuildInnerMessageError(result.InnerMessages)}";
        //                                else
        //                                    result.Message = "{STARHolonUIName} Successfully Published";

        //                                OnOAPPPublishStatusChanged?.Invoke(this, new OAPPPublishStatusEventArgs() { OAPPDNA = OAPPDNA, Status = Enums.OAPPPublishStatus.Published });
        //                            }
        //                            else
        //                                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling SaveSTARHolonAsync on {Enum.GetName(typeof(ProviderType), providerType)} provider. Reason: {saveSTARHolonResult.Message}");
        //                        }
        //                        else
        //                            OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling ValidateResult. Reason: {validateVersionResult.Message}");
        //                    }
        //                    else
        //                        OASISErrorHandling.HandleError(ref result, $"Permission Denied! The {STARHolonUIName} with id {OAPPDNA.Id} was created by a different avatar with id {OAPPDNA.CreatedByAvatarId}. The current avatar has an id of {avatarId}.");
        //                }
        //                else
        //                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadSTARHolonAsync on {Enum.GetName(typeof(ProviderType), providerType)} provider. Reason: {loadOAPPResult.Message}");
        //            }
        //            else
        //                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadAvatarAsync on {Enum.GetName(typeof(ProviderType), providerType)} provider. Reason: {loadAvatarResult.Message}");
        //        }
        //        else
        //            OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling ReadSTARHolonDNAAsync on {Enum.GetName(typeof(ProviderType), providerType)} provider. Reason: {readSTARHolonDNAResult.Message}");
        //    }
        //    catch (Exception ex)
        //    {
        //        OASISErrorHandling.HandleError(ref result, $"{errorMessage} {ex}");
        //    }
        //    finally
        //    {
        //        if (Directory.Exists(tempPath))
        //            Directory.Delete(tempPath);
        //    }

        //    //if (result.IsError)
        //    //    OnSTARHolonPublishStatusChanged?.Invoke(this, new STARHolonPublishStatusEventArgs() { STARHolonDNA = STARHolonDNA, Status = Enums.STARHolonPublishStatus.Error, ErrorMessage = result.Message });

        //    return result;
        //}

        public async Task<OASISResult<IOAPP>> PublishOAPPAsync(Guid avatarId, string fullPathToSource, string launchTarget, string fullPathToPublishTo = "", bool edit = false, bool registerOnSTARNET = true, bool dotnetPublish = true, bool generateSource = true, bool uploadSourceToSTARNET = true, bool makeSourcePublic = false, bool generateBinary = true, bool generateSelfContainedBinary = false, bool generateSelfContainedFullBinary = false, bool uploadToCloud = false, bool uploadSelfContainedToCloud = false, bool uploadSelfContainedFullToCloud = false, ProviderType providerType = ProviderType.Default, ProviderType binaryProviderType = ProviderType.IPFSOASIS, ProviderType selfContainedBinaryProviderType = ProviderType.None, ProviderType selfContainedFullBinaryProviderType = ProviderType.None)
        {
            OASISResult<IOAPP> result = new OASISResult<IOAPP>();
            IOAPPDNA OAPPDNA = null;

            OASISResult<OAPP> validateResult = await BeginPublishAsync(avatarId, fullPathToSource, launchTarget, fullPathToPublishTo, edit, providerType);

            if (validateResult != null && validateResult.Result != null && !validateResult.IsError)
            {
                OAPPDNA = (IOAPPDNA)validateResult.Result.STARHolonDNA;
                string publishedFileName = string.Concat(OAPPDNA.Name, "_v", OAPPDNA.Version, ".", STARHolonFileExtention);
                string publishedSelfContainedFileName = string.Concat(OAPPDNA.Name, " (Self Contained)_v", OAPPDNA.Version, ".", STARHolonFileExtention);
                string publishedSelfContainedFullFileName = string.Concat(OAPPDNA.Name, " (Self Contained)_v", OAPPDNA.Version, ".", STARHolonFileExtention);
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

                //TODO: Currently the filesize will NOT be in the compressed .STARHolon file because we dont know the size before we create it! ;-) We would need to compress it twice or edit the compressed file after to update the STARHolonDNA inside it...
                if (!string.IsNullOrEmpty(OAPPDNA.PublishedPath) && File.Exists(OAPPDNA.PublishedPath))
                    OAPPDNA.FileSize = new FileInfo(OAPPDNA.PublishedPath).Length;

                WriteDNA(OAPPDNA, fullPathToSource);
                validateResult.Result.STARHolonDNA = OAPPDNA;

                if (registerOnSTARNET)
                {
                    if (uploadToCloud)
                        result = await UploadToCloudAsync<IOAPP>(OAPPDNA, publishedFileName, registerOnSTARNET, binaryProviderType);

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
                OAPPDNA = (IOAPPDNA)validateResult.Result.STARHolonDNA;
                string publishedFileName = string.Concat(OAPPDNA.Name, "_v", OAPPDNA.Version, ".", STARHolonFileExtention);
                string publishedSelfContainedFileName = string.Concat(OAPPDNA.Name, " (Self Contained)_v", OAPPDNA.Version, ".", STARHolonFileExtention);
                string publishedSelfContainedFullFileName = string.Concat(OAPPDNA.Name, " (Self Contained)_v", OAPPDNA.Version, ".", STARHolonFileExtention);
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

                //TODO: Currently the filesize will NOT be in the compressed .STARHolon file because we dont know the size before we create it! ;-) We would need to compress it twice or edit the compressed file after to update the STARHolonDNA inside it...
                if (!string.IsNullOrEmpty(OAPPDNA.PublishedPath) && File.Exists(OAPPDNA.PublishedPath))
                    OAPPDNA.FileSize = new FileInfo(OAPPDNA.PublishedPath).Length;

                WriteDNA(OAPPDNA, fullPathToSource);
                validateResult.Result.STARHolonDNA = OAPPDNA;

                if (registerOnSTARNET)
                {
                    if (uploadToCloud)
                        result = UploadToCloud<IOAPP>(OAPPDNA, publishedFileName, registerOnSTARNET, binaryProviderType);

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

        public async Task<OASISResult<IOAPP>> UnpublishOAPPAsync(Guid avatarId, ISTARHolonDNA OAPPDNA, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.UnpublishAsync(avatarId, OAPPDNA, providerType));
        }

        public OASISResult<IOAPP> UnpublishOAPP(Guid avatarId, ISTARHolonDNA OAPPDNA, ProviderType providerType = ProviderType.Default)
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

        public async Task<OASISResult<IOAPP>> RepublishOAPPAsync(Guid avatarId, ISTARHolonDNA OAPPDNA, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.RepublishAsync(avatarId, OAPPDNA, providerType));
        }

        public OASISResult<IOAPP> RepublishOAPP(Guid avatarId, ISTARHolonDNA OAPPDNA, ProviderType providerType = ProviderType.Default)
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

        public async Task<OASISResult<IOAPP>> DeactivateOAPPAsync(Guid avatarId, ISTARHolonDNA OAPPDNA, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.DeactivateAsync(avatarId, OAPPDNA, providerType));
        }

        public OASISResult<IOAPP> DeactivateOAPP(Guid avatarId, ISTARHolonDNA OAPPDNA, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.Deactivate(avatarId, OAPPDNA, providerType));
        }

        public async Task<OASISResult<IOAPP>> ActivateOAPPAsync(Guid avatarId, IOAPP OAPP, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.ActivateAsync((OAPP)OAPP, avatarId, providerType));
        }

        public OASISResult<IOAPP> ActivateOAPP(Guid avatarId, IOAPP OAPP, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.Activate((OAPP)OAPP, avatarId, providerType));
        }

        public async Task<OASISResult<IOAPP>> ActivateOAPPAsync(Guid avatarId, ISTARHolonDNA OAPPDNA, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.ActivateAsync(OAPPDNA, avatarId, providerType));
        }

        public OASISResult<IOAPP> ActivateOAPP(Guid avatarId, ISTARHolonDNA OAPPDNA, ProviderType providerType = ProviderType.Default)
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
            return ProcessResult(await base.UninstallAsync(avatarId, (InstalledOAPP)installedOAPP, errorMessage, providerType));
        }

        public OASISResult<IInstalledOAPP> UninstallOAPP(Guid avatarId, IInstalledOAPP installedOAPP, string errorMessage, ProviderType providerType)
        {
            return ProcessResult(base.Uninstall(avatarId, (InstalledOAPP)installedOAPP, errorMessage, providerType));
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
            return ProcessResult(base.OpenSTARHolonFolder(avatarId, (InstalledOAPP)OAPP));
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

        //public async Task<OASISResult<bool>> WriteOAPPDNAAsync(ISTARHolonDNA OAPPDNA, string fullPathToOAPP)
        //{
        //    return await base.WritenDNAAsync(OAPPDNA, fullPathToOAPP);
        //}

        //public OASISResult<bool> WriteOAPPDNA(ISTARHolonDNA OAPPDNA, string fullPathToOAPP)
        //{
        //    return base.WriteSTARHolonDNA(OAPPDNA, fullPathToOAPP);
        //}

        //public async Task<OASISResult<ISTARHolonDNA>> ReadOAPPDNAFromSourceOrInstalledFolderAsync(string fullPathToOAPPFolder)
        //{
        //    return await base.ReadSTARHolonDNAFromSourceOrInstallFolderAsync(fullPathToOAPPFolder);
        //}

        //public OASISResult<ISTARHolonDNA> ReadOAPPDNAFromSourceOrInstalledFolder(string fullPathToOAPPFolder)
        //{
        //    return base.ReadSTARHolonDNAFromSourceOrInstallFolder(fullPathToOAPPFolder);
        //}

        //public async Task<OASISResult<ISTARHolonDNA>> ReadOAPPDNAFromPublishedOAPPFileAsync(string fullPathToOAPPFolder)
        //{
        //    return await base.ReadSTARHolonDNAFromPublishedFileAsync(fullPathToOAPPFolder);
        //}

        //public OASISResult<ISTARHolonDNA> ReadOAPPDNAFromPublishedOAPPFile(string fullPathToOAPPFolder)
        //{
        //    return base.ReadSTARHolonDNAFromPublishedFile(fullPathToOAPPFolder);
        //}

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

        private OASISResult<IEnumerable<IOAPP>> ProcessResults(OASISResult<IEnumerable<OAPP>> operationResult)
        {
            OASISResult<IEnumerable<IOAPP>> result = new OASISResult<IEnumerable<IOAPP>>();

            if (operationResult != null && operationResult.Result != null && !operationResult.IsError && operationResult.Result.Count() > 0)
            {
                List<IOAPP> oappTemplates = new List<IOAPP>();

                foreach (IOAPP template in operationResult.Result)
                    oappTemplates.Add(template);

                result.Result = oappTemplates;
            }

            OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(operationResult, result);
            return result;
        }

        private OASISResult<IEnumerable<IInstalledOAPP>> ProcessResults(OASISResult<IEnumerable<InstalledOAPP>> operationResult)
        {
            OASISResult<IEnumerable<IInstalledOAPP>> result = new OASISResult<IEnumerable<IInstalledOAPP>>();

            if (operationResult != null && operationResult.Result != null && !operationResult.IsError && operationResult.Result.Count() > 0)
            {
                List<IInstalledOAPP> oappTemplates = new List<IInstalledOAPP>();

                foreach (IInstalledOAPP template in operationResult.Result)
                    oappTemplates.Add(template);

                result.Result = oappTemplates;
            }

            OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(operationResult, result);
            return result;
        }

        private OASISResult<IOAPP> ProcessResult(OASISResult<OAPP> operationResult)
        {
            OASISResult<IOAPP> result = new OASISResult<IOAPP>();
            result.Result = operationResult.Result;
            OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(operationResult, result);
            return result;
        }

        private OASISResult<IDownloadedOAPP> ProcessResult(OASISResult<DownloadedOAPP> operationResult)
        {
            OASISResult<IDownloadedOAPP> result = new OASISResult<IDownloadedOAPP>();
            result.Result = operationResult.Result;
            OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(operationResult, result);
            return result;
        }

        private OASISResult<IInstalledOAPP> ProcessResult(OASISResult<InstalledOAPP> operationResult)
        {
            OASISResult<IInstalledOAPP> result = new OASISResult<IInstalledOAPP>();
            result.Result = operationResult.Result;
            OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(operationResult, result);
            return result;
        }
    }
}