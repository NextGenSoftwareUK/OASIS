using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces;
using NextGenSoftware.OASIS.API.ONODE.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Holons;
using System.Diagnostics;
using System.IO;
using NextGenSoftware.OASIS.API.Core.Managers;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Managers
{
    public class OAPPManager : OAPPSystemManagerBase<OAPP, DownloadedOAPP, InstalledOAPP>
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

        public async Task<OASISResult<IOAPP>> EditOAPPAsync(Guid OAPPId, IOAPPSystemHolonDNA newOAPPDNA, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.EditAsync(OAPPId, newOAPPDNA, avatarId, providerType));
        }

        public async Task<OASISResult<IOAPP>> EditOAPPAsync(IOAPP OAPP, IOAPPSystemHolonDNA newOAPPDNA, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.EditAsync(avatarId, (OAPP)OAPP, newOAPPDNA, providerType));
        }

        public async Task<OASISResult<IOAPP>> PublishOAPPAsync(Guid avatarId, string fullPathToSource, string launchTarget, string fullPathToPublishTo = "", bool edit = false, bool registerOnSTARNET = true, bool dotnetPublish = true, bool generateOAPPSource = true, bool uploadOAPPSourceToSTARNET = true, bool makeOAPPSourcePublic = false, bool generateOAPPBinary = true, bool generateOAPPSelfContainedBinary = false, bool generateOAPPSelfContainedFullBinary = false, bool uploadOAPPToCloud = false, bool uploadOAPPSelfContainedToCloud = false, bool uploadOAPPSelfContainedFullToCloud = false, ProviderType providerType = ProviderType.Default, ProviderType oappBinaryProviderType = ProviderType.IPFSOASIS, ProviderType oappSelfContainedBinaryProviderType = ProviderType.None, ProviderType oappSelfContainedFullBinaryProviderType = ProviderType.None)
        {
            OASISResult<IOAPP> result = new OASISResult<IOAPP>();
            string errorMessage = "Error occured in OAPPManager.PublishAsync. Reason: ";
            IOAPPDNA OAPPSystemHolonDNA = null;
            string tempPath = "";

            try
            {
                OASISResult<IOAPPDNA> readOAPPSystemHolonDNAResult = await ReadOAPPSystemHolonDNAFromSourceOrInstallFolderAsync<IOAPPDNA>(fullPathToSource);

                if (readOAPPSystemHolonDNAResult != null && !readOAPPSystemHolonDNAResult.IsError && readOAPPSystemHolonDNAResult.Result != null)
                {
                    OAPPSystemHolonDNA = readOAPPSystemHolonDNAResult.Result;
                    OnOAPPSystemHolonPublishStatusChanged?.Invoke(this, new OAPPSystemHolonPublishStatusEventArgs() { OAPPSystemHolonDNA = OAPPSystemHolonDNA, Status = Enums.OAPPSystemHolonPublishStatus.Packaging });
                    OASISResult<IAvatar> loadAvatarResult = await AvatarManager.Instance.LoadAvatarAsync(avatarId, false, true, providerType);

                    if (loadAvatarResult != null && loadAvatarResult.Result != null && !loadAvatarResult.IsError)
                    {
                        //Load latest version.
                        OASISResult<T1> loadOAPPSystemHolonResult = await LoadAsync(avatarId, OAPPSystemHolonDNA.Id);

                        if (loadOAPPSystemHolonResult != null && loadOAPPSystemHolonResult.Result != null && !loadOAPPSystemHolonResult.IsError)
                        {
                            if (loadOAPPSystemHolonResult.Result.OAPPSystemHolonDNA.CreatedByAvatarId == avatarId)
                            {
                                OASISResult<bool> validateVersionResult = ValidateVersion(OAPPSystemHolonDNA.Version, loadOAPPSystemHolonResult.Result.OAPPSystemHolonDNA.Version, fullPathToOAPPSystemHolon, OAPPSystemHolonDNA.PublishedOn == DateTime.MinValue, edit);

                                if (validateVersionResult != null && validateVersionResult.Result && !validateVersionResult.IsError)
                                {
                                    //TODO: Maybe add check to make sure the DNA has not been tampered with?
                                    loadOAPPSystemHolonResult.Result.OAPPSystemHolonDNA.Version = OAPPSystemHolonDNA.Version; //Set the new version set in the DNA (JSON file).
                                    OAPPSystemHolonDNA = loadOAPPSystemHolonResult.Result.OAPPSystemHolonDNA; //Make sure it has not been tampered with by using the stored version.

                                    if (!edit)
                                    {
                                        OAPPSystemHolonDNA.VersionSequence++;
                                        OAPPSystemHolonDNA.NumberOfVersions++;
                                    }

                                    OAPPSystemHolonDNA.LaunchTarget = launchTarget;
                                    string publishedOAPPSystemHolonFileName = string.Concat(OAPPSystemHolonDNA.Name, "_v", OAPPSystemHolonDNA.Version, ".", OAPPSystemHolonFileExtention);

                                    if (string.IsNullOrEmpty(fullPathToPublishTo))
                                        fullPathToPublishTo = Path.Combine(fullPathToSource, "Published");

                                    if (!Directory.Exists(fullPathToPublishTo))
                                        Directory.CreateDirectory(fullPathToPublishTo);

                                    if (!edit)
                                    {
                                        OAPPSystemHolonDNA.PublishedOn = DateTime.Now;
                                        OAPPSystemHolonDNA.PublishedByAvatarId = avatarId;
                                        OAPPSystemHolonDNA.PublishedByAvatarUsername = loadAvatarResult.Result.Username;
                                    }
                                    else
                                    {
                                        OAPPSystemHolonDNA.ModifiedOn = DateTime.Now;
                                        OAPPSystemHolonDNA.ModifiedByAvatarId = avatarId;
                                        OAPPSystemHolonDNA.ModifiedByAvatarUsername = loadAvatarResult.Result.Username;
                                    }

                                    OAPPSystemHolonDNA.PublishedOnSTARNET = registerOnSTARNET && (binaryProviderType != ProviderType.None || uploadToCloud);

                                    if (generateBinary)
                                    {
                                        OAPPSystemHolonDNA.PublishedPath = Path.Combine(fullPathToPublishTo, publishedOAPPSystemHolonFileName);
                                        OAPPSystemHolonDNA.PublishedToCloud = registerOnSTARNET && uploadToCloud;
                                        OAPPSystemHolonDNA.PublishedProviderType = binaryProviderType;
                                    }

                                    //if (generateSelfContainedBinary)
                                    //{
                                    //    OAPPDNA.SelfContainedPublishedPath = Path.Combine(fullPathToPublishTo, publishedOAPPSelfContainedFileName);
                                    //    OAPPDNA.SelfContainedPublishedToCloud = registerOnSTARNET && uploadOAPPSelfContainedToCloud;
                                    //    OAPPDNA.SelfContainedPublishedProviderType = oappSelfContainedBinaryProviderType;
                                    //}

                                    //if (generateSelfContainedFullBinary)
                                    //{
                                    //    OAPPDNA.SelfContainedFullPublishedPath = Path.Combine(fullPathToPublishTo, publishedOAPPSelfContainedFullFileName);
                                    //    OAPPDNA.SelfContainedFullPublishedToCloud = registerOnSTARNET && uploadOAPPSelfContainedFullToCloud;
                                    //    OAPPDNA.SelfContainedFullPublishedProviderType = oappSelfContainedFullBinaryProviderType;
                                    //}

                                    //if (generateSource)
                                    //{
                                    //    OAPPDNA.SourcePublishedPath = Path.Combine(fullPathToPublishTo, publishedOAPPSourceFileName);
                                    //    OAPPDNA.SourcePublishedOnSTARNET = registerOnSTARNET && uploadOAPPSourceToSTARNET;
                                    //    OAPPDNA.SourcePublicOnSTARNET = makeOAPPSourcePublic;
                                    //}

                                    WriteOAPPSystemHolonDNA(OAPPSystemHolonDNA, fullPathToSource);
                                    OnOAPPSystemHolonPublishStatusChanged?.Invoke(this, new OAPPSystemHolonPublishStatusEventArgs() { OAPPSystemHolonDNA = OAPPSystemHolonDNA, Status = Enums.OAPPSystemHolonPublishStatus.Compressing });

                                    if (generateBinary)
                                        GenerateCompressedFile(fullPathToSource, OAPPSystemHolonDNA.PublishedPath);

                                    //if (generateSelfContainedBinary)
                                    //{
                                    //    if (File.Exists(OAPPDNA.SelfContainedPublishedPath))
                                    //        File.Delete(OAPPDNA.SelfContainedPublishedPath);

                                    //    ZipFile.CreateFromDirectory(fullPathToSource, OAPPSystemHolonDNA.SelfContainedPublishedPath);
                                    //}

                                    //if (generateSelfContainedFullBinary)
                                    //{
                                    //    if (File.Exists(OAPPSystemHolonDNA.SelfContainedFullPublishedPath))
                                    //        File.Delete(OAPPSystemHolonDNA.SelfContainedFullPublishedPath);

                                    //    ZipFile.CreateFromDirectory(fullPathToSource, OAPPSystemHolonDNA.SelfContainedFullPublishedPath);
                                    //}

                                    //if (generateSource)
                                    //{
                                    //    tempPath = Path.Combine(Path.GetTempPath(), OAPPDNA.OAPPName);

                                    //    if (File.Exists(OAPPDNA.SourcePublishedPath))
                                    //        File.Delete(OAPPDNA.SourcePublishedPath);

                                    //    if (Directory.Exists(tempPath))
                                    //        Directory.Delete(tempPath, true);

                                    //    DirectoryHelper.CopyFilesRecursively(fullPathToSource, tempPath);
                                    //    Directory.Delete(Path.Combine(tempPath, "bin"), true);
                                    //    Directory.Delete(Path.Combine(tempPath, "obj"), true);

                                    //    ZipFile.CreateFromDirectory(tempPath, OAPPDNA.SourcePublishedPath);
                                    //}

                                    //TODO: Currently the filesize will NOT be in the compressed .OAPPSystemHolon file because we dont know the size before we create it! ;-) We would need to compress it twice or edit the compressed file after to update the OAPPSystemHolonDNA inside it...
                                    if (!string.IsNullOrEmpty(OAPPSystemHolonDNA.PublishedPath) && File.Exists(OAPPSystemHolonDNA.PublishedPath))
                                        OAPPSystemHolonDNA.FileSize = new FileInfo(OAPPSystemHolonDNA.PublishedPath).Length;

                                    WriteOAPPSystemHolonDNA(OAPPSystemHolonDNA, fullPathToSource);
                                    loadOAPPSystemHolonResult.Result.OAPPSystemHolonDNA = OAPPSystemHolonDNA;

                                    if (registerOnSTARNET)
                                    {
                                        if (uploadToCloud)
                                        {
                                            try
                                            {
                                                OnOAPPSystemHolonPublishStatusChanged?.Invoke(this, new OAPPSystemHolonPublishStatusEventArgs() { OAPPSystemHolonDNA = readOAPPSystemHolonDNAResult.Result, Status = Enums.OAPPSystemHolonPublishStatus.Uploading });
                                                StorageClient storage = await StorageClient.CreateAsync();
                                                //var bucket = storage.CreateBucket("oasis", "OAPPSystemHolons");

                                                // set minimum chunksize just to see progress updating
                                                var uploadObjectOptions = new UploadObjectOptions
                                                {
                                                    ChunkSize = UploadObjectOptions.MinimumChunkSize
                                                };

                                                var progressReporter = new Progress<Google.Apis.Upload.IUploadProgress>(OnUploadProgress);
                                                using (var fileStream = File.OpenRead(OAPPSystemHolonDNA.PublishedPath))
                                                {
                                                    _fileLength = fileStream.Length;
                                                    _progress = 0;

                                                    await storage.UploadObjectAsync(OAPPSystemHolonGoogleBucket, publishedOAPPSystemHolonFileName, "", fileStream, uploadObjectOptions, progress: progressReporter);
                                                }

                                                _progress = 100;
                                                OnOAPPSystemHolonUploadStatusChanged?.Invoke(this, new OAPPSystemHolonUploadProgressEventArgs() { Progress = _progress, Status = Enums.OAPPSystemHolonUploadStatus.Uploading });
                                                CLIEngine.DisposeProgressBar(false);
                                                Console.WriteLine("");

                                                //HttpClient client = new HttpClient();
                                                //string pinataApiKey = "33e4469830a51af0171b";
                                                //string pinataSecretApiKey = "ff57367b2b125bf5f06f79b30b466890c84eed101c12af064459d88d8bb8d8a0\r\nJWT: eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJ1c2VySW5mb3JtYXRpb24iOnsiaWQiOiIzMGI3NjllNS1hMjJmLTQxN2UtOWEwYi1mZTQ2NzE5MjgzNzgiLCJlbWFpbCI6ImRhdmlkZWxsYW1zQGhvdG1haWwuY29tIiwiZW1haWxfdmVyaWZpZWQiOnRydWUsInBpbl9wb2xpY3kiOnsicmVnaW9ucyI6W3siZGVzaXJlZFJlcGxpY2F0aW9uQ291bnQiOjEsImlkIjoiRlJBMSJ9LHsiZGVzaXJlZFJlcGxpY2F0aW9uQ291bnQiOjEsImlkIjoiTllDMSJ9XSwidmVyc2lvbiI6MX0sIm1mYV9lbmFibGVkIjpmYWxzZSwic3RhdHVzIjoiQUNUSVZFIn0sImF1dGhlbnRpY2F0aW9uVHlwZSI6InNjb3BlZEtleSIsInNjb3BlZEtleUtleSI6IjMzZTQ0Njk4MzBhNTFhZjAxNzFiIiwic2NvcGVkS2V5U2VjcmV0IjoiZmY1NzM2N2IyYjEyNWJmNWYwNmY3OWIzMGI0NjY4OTBjODRlZWQxMDFjMTJhZjA2NDQ1OWQ4OGQ4YmI4ZDhhMCIsImV4cCI6MTc3Mzc4NDAzNX0.L-6_BPMsvhN3Es72Q5lZAFKpBEDF9kEibOGdWd_PxHs";
                                                //string pinataUrl = "https://api.pinata.cloud/pinning/pinFileToIPFS";
                                                //string filePath = OAPPSystemHolonDNA.PublishedPath;

                                                //using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                                                //using (var content = new MultipartFormDataContent())
                                                //{
                                                //    content.Add(new StreamContent(fileStream), "file", Path.GetFileName(filePath));
                                                //    client.DefaultRequestHeaders.Add("pinata_api_key", pinataApiKey);
                                                //    client.DefaultRequestHeaders.Add("pinata_secret_api_key", pinataSecretApiKey);

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

                                                //                           options.CustomPinPolicy.AddOrUpdateRegion("NYC1", desiredReplicationCount: 1);

                                                //                           //var response = await client.Pinning.PinFileToIpfsAsync()

                                                //                           byte[] fileBytes = await File.ReadAllBytesAsync(filePath);
                                                //                           using (var content = new MultipartFormDataContent())
                                                //                           {
                                                //                               var fileContent = new ByteArrayContent(fileBytes);
                                                //                               content.Add(fileContent, "file", Path.GetFileName(filePath));
                                                //                           }

                                                //                           var response = await pinClient.Pinning.PinFileToIpfsAsync(content =>
                                                //                           {
                                                //                               //var file = new StringContent(, Encoding.UTF8, MediaTypeNames.Application.Zip);
                                                //                               var file = new StreamContent(fileStream), "file", Path.GetFileName(filePath));

                                                //                               content.AddPinataFile(file, "index.html");
                                                //                           },
                                                //                              metadata,
                                                //                              options);

                                                //                           if (response.IsSuccess)
                                                //                           {
                                                //                               //File uploaded to Pinata Cloud and can be accessed on IPFS!
                                                //                               var hash = response.IpfsHash; // QmR9HwzakHVr67HFzzgJHoRjwzTTt4wtD6KU4NFe2ArYuj
                                                //                           }

                                                //var pinataClient = new PinataClient("33e4469830a51af0171b");
                                                //PinFileResponse pinFileResponse = await pinataClient.PinFileToIPFSAsync(OAPPSystemHolonDNA.PublishedPath);

                                                //if (pinFileResponse != null && !string.IsNullOrEmpty(pinFileResponse.IpfsHash))
                                                //{
                                                //    OAPPSystemHolonDNA.PinataIPFSHash = pinFileResponse.IpfsHash;
                                                //    OAPPSystemHolonDNA.OAPPSystemHolonPublishedOnSTARNET = true;
                                                //    OAPPSystemHolonDNA.OAPPSystemHolonPublishedToPinata = true;
                                                //}
                                                //else
                                                //{
                                                //    OASISErrorHandling.HandleWarning(ref result, $"An error occured publishing the T to Pinata.");
                                                //    OAPPSystemHolonDNA.OAPPSystemHolonPublishedOnSTARNET = registerOnSTARNET && oappBinaryProviderType != ProviderType.None;
                                                //}
                                            }
                                            catch (Exception ex)
                                            {
                                                CLIEngine.DisposeProgressBar(false);
                                                Console.WriteLine("");

                                                OASISErrorHandling.HandleWarning(ref result, $"An error occured publishing the {OAPPSystemHolonUIName} to cloud storage. Reason: {ex}");
                                                OAPPSystemHolonDNA.PublishedOnSTARNET = registerOnSTARNET && binaryProviderType != ProviderType.None;
                                                OAPPSystemHolonDNA.PublishedToCloud = false;
                                            }
                                        }

                                        if (binaryProviderType != ProviderType.None)
                                        {
                                            loadOAPPSystemHolonResult.Result.PublishedOAPPSystemHolon = File.ReadAllBytes(OAPPSystemHolonDNA.PublishedPath);

                                            //TODO: We could use HoloOASIS and other large file storage providers in future...
                                            OASISResult<T1> saveLargeOAPPSystemHolonResult = await SaveAsync(avatarId, loadOAPPSystemHolonResult.Result, binaryProviderType);

                                            if (saveLargeOAPPSystemHolonResult != null && !saveLargeOAPPSystemHolonResult.IsError && saveLargeOAPPSystemHolonResult.Result != null)
                                            {
                                                result.Result = saveLargeOAPPSystemHolonResult.Result;
                                                result.IsSaved = true;
                                            }
                                            else
                                            {
                                                OASISErrorHandling.HandleWarning(ref result, $" Error occured saving the published {OAPPSystemHolonUIName} binary to STARNET using the {binaryProviderType} provider. Reason: {saveLargeOAPPSystemHolonResult.Message}");
                                                OAPPSystemHolonDNA.PublishedOnSTARNET = registerOnSTARNET && uploadToCloud;
                                                OAPPSystemHolonDNA.PublishedProviderType = ProviderType.None;
                                            }
                                        }
                                        else
                                            OAPPSystemHolonDNA.PublishedProviderType = ProviderType.None;
                                    }

                                    //If its not the first version.
                                    if (OAPPSystemHolonDNA.Version != "1.0.0" && !edit)
                                    {
                                        //If the ID has not been set then store the original id now.
                                        if (!loadOAPPSystemHolonResult.Result.MetaData.ContainsKey(OAPPSystemHolonIdName))
                                            loadOAPPSystemHolonResult.Result.MetaData[OAPPSystemHolonIdName] = loadOAPPSystemHolonResult.Result.Id;

                                        loadOAPPSystemHolonResult.Result.MetaData["Version"] = loadOAPPSystemHolonResult.Result.OAPPSystemHolonDNA.Version;
                                        loadOAPPSystemHolonResult.Result.MetaData["VersionSequence"] = loadOAPPSystemHolonResult.Result.OAPPSystemHolonDNA.VersionSequence;

                                        //Blank fields so it creates a new version.
                                        loadOAPPSystemHolonResult.Result.Id = Guid.Empty;
                                        loadOAPPSystemHolonResult.Result.ProviderUniqueStorageKey.Clear();
                                        loadOAPPSystemHolonResult.Result.CreatedDate = DateTime.MinValue;
                                        loadOAPPSystemHolonResult.Result.ModifiedDate = DateTime.MinValue;
                                        loadOAPPSystemHolonResult.Result.CreatedByAvatarId = Guid.Empty;
                                        loadOAPPSystemHolonResult.Result.ModifiedByAvatarId = Guid.Empty;
                                        loadOAPPSystemHolonResult.Result.OAPPSystemHolonDNA.Downloads = 0;
                                        loadOAPPSystemHolonResult.Result.OAPPSystemHolonDNA.Installs = 0;
                                    }

                                    OASISResult<T1> saveOAPPSystemHolonResult = await SaveAsync(avatarId, loadOAPPSystemHolonResult.Result, providerType);

                                    if (saveOAPPSystemHolonResult != null && !saveOAPPSystemHolonResult.IsError && saveOAPPSystemHolonResult.Result != null)
                                    {
                                        result = await UpdateNumberOfVersionCountsAsync(avatarId, saveOAPPSystemHolonResult, errorMessage, providerType);
                                        result.IsSaved = true;

                                        if (readOAPPSystemHolonDNAResult.Result.STARODKVersion != OASISBootLoader.OASISBootLoader.STARODKVersion)
                                            OASISErrorHandling.HandleWarning(ref result, $" The STAR ODK Version {readOAPPSystemHolonDNAResult.Result.STARODKVersion} does not match the current version {OASISBootLoader.OASISBootLoader.STARODKVersion}. This may lead to issues, it is recommended to make sure the versions match.");

                                        if (readOAPPSystemHolonDNAResult.Result.OASISVersion != OASISBootLoader.OASISBootLoader.OASISVersion)
                                            OASISErrorHandling.HandleWarning(ref result, $" The OASIS Version {readOAPPSystemHolonDNAResult.Result.OASISVersion} does not match the current version {OASISBootLoader.OASISBootLoader.OASISVersion}. This may lead to issues, it is recommended to make sure the versions match.");

                                        if (readOAPPSystemHolonDNAResult.Result.COSMICVersion != OASISBootLoader.OASISBootLoader.COSMICVersion)
                                            OASISErrorHandling.HandleWarning(ref result, $" The COSMIC Version {readOAPPSystemHolonDNAResult.Result.COSMICVersion} does not match the current version {OASISBootLoader.OASISBootLoader.COSMICVersion}. This may lead to issues, it is recommended to make sure the versions match.");

                                        if (result.IsWarning)
                                            result.Message = $"{OAPPSystemHolonUIName} successfully published but there were {result.WarningCount} warnings:\n\n {OASISResultHelper.BuildInnerMessageError(result.InnerMessages)}";
                                        else
                                            result.Message = "{OAPPSystemHolonUIName} Successfully Published";

                                        OnOAPPSystemHolonPublishStatusChanged?.Invoke(this, new OAPPSystemHolonPublishStatusEventArgs() { OAPPSystemHolonDNA = OAPPSystemHolonDNA, Status = Enums.OAPPSystemHolonPublishStatus.Published });
                                    }
                                    else
                                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling SaveOAPPSystemHolonAsync on {Enum.GetName(typeof(ProviderType), providerType)} provider. Reason: {saveOAPPSystemHolonResult.Message}");
                                }
                                else
                                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling ValidateResult. Reason: {validateVersionResult.Message}");
                            }
                            else
                                OASISErrorHandling.HandleError(ref result, $"Permission Denied! The {OAPPSystemHolonUIName} with id {OAPPSystemHolonDNA.Id} was created by a different avatar with id {OAPPSystemHolonDNA.CreatedByAvatarId}. The current avatar has an id of {avatarId}.");
                        }
                        else
                            OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadOAPPSystemHolonAsync on {Enum.GetName(typeof(ProviderType), providerType)} provider. Reason: {loadOAPPSystemHolonResult.Message}");
                    }
                    else
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadAvatarAsync on {Enum.GetName(typeof(ProviderType), providerType)} provider. Reason: {loadAvatarResult.Message}");
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling ReadOAPPSystemHolonDNAAsync on {Enum.GetName(typeof(ProviderType), providerType)} provider. Reason: {readOAPPSystemHolonDNAResult.Message}");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} {ex}");
            }
            finally
            {
                if (Directory.Exists(tempPath))
                    Directory.Delete(tempPath);
            }

            //if (result.IsError)
            //    OnOAPPSystemHolonPublishStatusChanged?.Invoke(this, new OAPPSystemHolonPublishStatusEventArgs() { OAPPSystemHolonDNA = OAPPSystemHolonDNA, Status = Enums.OAPPSystemHolonPublishStatus.Error, ErrorMessage = result.Message });

            return result;
        }


            //return ProcessResult(await base.PublishAsync(avatarId, fullPathToOAPP, launchTarget, fullPathToPublishTo, registerOnSTARNET, generateOAPPBinary, uploadOAPPToCloud, edit, providerType, oappBinaryProviderType, dotnetPublish));
            //OASISResult<OAPP> result = await base.PublishAsync(avatarId, fullPathToSource, launchTarget, fullPathToPublishTo, edit, registerOnSTARNET, generateOAPPBinary, uploadOAPPToCloud, providerType, oappBinaryProviderType);

            //if (result != null && !result.IsError && result.Result != null)
            //{
            //    if (dotnetPublish)
            //    {
            //        //TODO: Finish implementing this.
            //        //Process.Start("dotnet publish -c Release -r <RID> --self-contained");
            //        //Process.Start("dotnet publish -c Release -r win-x64 --self-contained");
            //        //string command = 

            //        OnOAPPPublishStatusChanged?.Invoke(this, new OAPPPublishStatusEventArgs() { OAPPDNA = OAPPDNA, Status = Enums.OAPPPublishStatus.DotNetPublishing });
            //        string dotnetPublishPath = Path.Combine(fullPathToSource, "dotnetPublished");
            //        Process.Start($"dotnet publish PROJECT {fullPathToSource} -c Release --self-contained -output {dotnetPublishPath}");
            //        fullPathToSource = dotnetPublishPath;

            //        //"bin\\Release\\net8.0\\";
            //    }


            //}
        }

        public OASISResult<IOAPP> PublishOAPP(Guid avatarId, string fullPathToOAPP, string launchTarget, string fullPathToPublishTo = "", bool registerOnSTARNET = true, bool dotnetPublish = true, bool generateOAPPSource = true, bool uploadOAPPSourceToSTARNET = true, bool makeOAPPSourcePublic = false, bool generateOAPPBinary = true, bool generateOAPPSelfContainedBinary = false, bool generateOAPPSelfContainedFullBinary = false, bool uploadOAPPToCloud = false, bool uploadOAPPSelfContainedToCloud = false, bool uploadOAPPSelfContainedFullToCloud = false, ProviderType providerType = ProviderType.Default, ProviderType oappBinaryProviderType = ProviderType.IPFSOASIS, ProviderType oappSelfContainedBinaryProviderType = ProviderType.None, ProviderType oappSelfContainedFullBinaryProviderType = ProviderType.None)
        {
            return ProcessResult(base.Publish(avatarId, fullPathToOAPP, launchTarget, fullPathToPublishTo, registerOnSTARNET, generateOAPPBinary, uploadOAPPToCloud, edit, providerType, oappBinaryProviderType, dotnetPublish));
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

        public async Task<OASISResult<IOAPP>> UnpublishOAPPAsync(Guid avatarId, IOAPPSystemHolonDNA OAPPDNA, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.UnpublishAsync(avatarId, OAPPDNA, providerType));
        }

        public OASISResult<IOAPP> UnpublishOAPP(Guid avatarId, IOAPPSystemHolonDNA OAPPDNA, ProviderType providerType = ProviderType.Default)
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

        public async Task<OASISResult<IOAPP>> RepublishOAPPAsync(Guid avatarId, IOAPPSystemHolonDNA OAPPDNA, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.RepublishAsync(avatarId, OAPPDNA, providerType));
        }

        public OASISResult<IOAPP> RepublishOAPP(Guid avatarId, IOAPPSystemHolonDNA OAPPDNA, ProviderType providerType = ProviderType.Default)
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

        public async Task<OASISResult<IOAPP>> DeactivateOAPPAsync(Guid avatarId, IOAPPSystemHolonDNA OAPPDNA, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.DeactivateAsync(avatarId, OAPPDNA, providerType));
        }

        public OASISResult<IOAPP> DeactivateOAPP(Guid avatarId, IOAPPSystemHolonDNA OAPPDNA, ProviderType providerType = ProviderType.Default)
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

        public async Task<OASISResult<IOAPP>> ActivateOAPPAsync(Guid avatarId, IOAPPSystemHolonDNA OAPPDNA, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.ActivateAsync(OAPPDNA, avatarId, providerType));
        }

        public OASISResult<IOAPP> ActivateOAPP(Guid avatarId, IOAPPSystemHolonDNA OAPPDNA, ProviderType providerType = ProviderType.Default)
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
            return ProcessResult(base.OpenOAPPSystemHolonFolder(avatarId, (InstalledOAPP)OAPP));
        }

        public async Task<OASISResult<IInstalledOAPP>> OpenOAPPFolderAsync(Guid avatarId, Guid OAPPId, int versionSequence, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.OpenOAPPSystemHolonFolderAsync(avatarId, OAPPId, versionSequence, providerType));
        }

        public OASISResult<IInstalledOAPP> OpenOAPPFolder(Guid avatarId, Guid OAPPId, int versionSequence, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.OpenOAPPSystemHolonFolder(avatarId, OAPPId, versionSequence, providerType));
        }

        public async Task<OASISResult<IInstalledOAPP>> OpenOAPPFolderAsync(Guid avatarId, Guid OAPPId, string version, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.OpenOAPPSystemHolonFolderAsync(avatarId, OAPPId, version, providerType));
        }

        public OASISResult<IInstalledOAPP> OpenOAPPFolder(Guid avatarId, Guid OAPPId, string version, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.OpenOAPPSystemHolonFolder(avatarId, OAPPId, version, providerType));
        }

        public async Task<OASISResult<bool>> WriteOAPPDNAAsync(IOAPPSystemHolonDNA OAPPDNA, string fullPathToOAPP)
        {
            return await base.WriteOAPPSystemHolonDNAAsync(OAPPDNA, fullPathToOAPP);
        }

        public OASISResult<bool> WriteOAPPDNA(IOAPPSystemHolonDNA OAPPDNA, string fullPathToOAPP)
        {
            return base.WriteOAPPSystemHolonDNA(OAPPDNA, fullPathToOAPP);
        }

        public async Task<OASISResult<IOAPPSystemHolonDNA>> ReadOAPPDNAFromSourceOrInstalledFolderAsync(string fullPathToOAPPFolder)
        {
            return await base.ReadOAPPSystemHolonDNAFromSourceOrInstallFolderAsync(fullPathToOAPPFolder);
        }

        public OASISResult<IOAPPSystemHolonDNA> ReadOAPPDNAFromSourceOrInstalledFolder(string fullPathToOAPPFolder)
        {
            return base.ReadOAPPSystemHolonDNAFromSourceOrInstallFolder(fullPathToOAPPFolder);
        }

        public async Task<OASISResult<IOAPPSystemHolonDNA>> ReadOAPPDNAFromPublishedOAPPFileAsync(string fullPathToOAPPFolder)
        {
            return await base.ReadOAPPSystemHolonDNAFromPublishedFileAsync(fullPathToOAPPFolder);
        }

        public OASISResult<IOAPPSystemHolonDNA> ReadOAPPDNAFromPublishedOAPPFile(string fullPathToOAPPFolder)
        {
            return base.ReadOAPPSystemHolonDNAFromPublishedFile(fullPathToOAPPFolder);
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