using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.ONode.Core.Interfaces;
using NextGenSoftware.OASIS.API.ONode.Core.Holons;
using NextGenSoftware.OASIS.API.ONODE.Core.Events;
using NextGenSoftware.OASIS.API.ONode.Core.Interfaces.Holons;
using NextGenSoftware.OASIS.API.Core.Managers;
using System.IO.Compression;
using Google.Cloud.Storage.V1;
using System.Text;
using System.Linq;
using System.Diagnostics;
using System.Text.Json.Serialization;


namespace NextGenSoftware.OASIS.API.ONode.Core.Managers
{
    public class OAPPTemplateManager : PublishManagerBase//, IOAPPTemplateManager
    {
        private int _progress = 0;
        private long _fileLength = 0;

        public OAPPTemplateManager(Guid avatarId, OASISDNA OASISDNA = null) : base(avatarId, OASISDNA) { }
        public OAPPTemplateManager(IOASISStorageProvider OASISStorageProvider, Guid avatarId, OASISDNA OASISDNA = null) : base(OASISStorageProvider, avatarId, OASISDNA) { }

        public delegate void OAPPTemplatePublishStatusChanged(object sender, OAPPTemplatePublishStatusEventArgs e);
        public delegate void OAPPTemplateInstallStatusChanged(object sender, OAPPTemplateInstallStatusEventArgs e);
        public delegate void OAPPTemplateUploadStatusChanged(object sender, OAPPTemplateUploadProgressEventArgs e);
        public delegate void OAPPTemplateDownloadStatusChanged(object sender, OAPPTemplateDownloadProgressEventArgs e);

        /// <summary>
        /// Fired when there is a change in the OAPP publish status.
        /// </summary>
        public event OAPPTemplatePublishStatusChanged OnOAPPTemplatePublishStatusChanged;

        /// <summary>
        /// Fired when there is a change to the OAPP Install status.
        /// </summary>
        public event OAPPTemplateInstallStatusChanged OnOAPPTemplateInstallStatusChanged;

        /// <summary>
        /// Fired when there is a change in the OAPP upload status.
        /// </summary>
        public event OAPPTemplateUploadStatusChanged OnOAPPTemplateUploadStatusChanged;

        /// <summary>
        /// Fired when there is a change in the OAPP download status.
        /// </summary>
        public event OAPPTemplateDownloadStatusChanged OnOAPPTemplateDownloadStatusChanged;


        public async Task<OASISResult<IOAPPTemplateDNA>> CreateOAPPTemplateAsync(string name, string description, OAPPTemplateType OAPPTemplateType, Guid avatarId, string fullPathToOAPPTemplate, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IOAPPTemplateDNA> result = new OASISResult<IOAPPTemplateDNA>();
            string errorMessage = "Error occured in OAPPTemplateManager.CreateOAPPTemplateAsync, Reason:";

            try
            {
                OAPPTemplate OAPPTemplate = new OAPPTemplate()
                {
                    Id = Guid.NewGuid(),
                    Name = name,
                    Description = description
                };

                OASISResult<IAvatar> avatarResult = await AvatarManager.Instance.LoadAvatarAsync(avatarId, false, true, providerType);

                if (avatarResult != null && avatarResult.Result != null && !avatarResult.IsError)
                {
                    OAPPTemplateDNA OAPPTemplateDNA = new OAPPTemplateDNA()
                    {
                        Id = OAPPTemplate.Id,
                        Name = name,
                        Description = description,
                        OAPPTemplateType = OAPPTemplateType,
                        CreatedByAvatarId = avatarId,
                        CreatedByAvatarUsername = avatarResult.Result.Username,
                        CreatedOn = DateTime.Now,
                        Version = "1.0.0",
                        STARODKVersion = OASISBootLoader.OASISBootLoader.STARODKVersion,
                        OASISVersion = OASISBootLoader.OASISBootLoader.OASISVersion,
                        COSMICVersion = OASISBootLoader.OASISBootLoader.COSMICVersion,
                        DotNetVersion = OASISBootLoader.OASISBootLoader.DotNetVersion
                    };

                    await WriteOAPPTemplateDNAAsync(OAPPTemplateDNA, fullPathToOAPPTemplate);

                    OAPPTemplate.OAPPTemplateDNA = OAPPTemplateDNA;
                    OASISResult<OAPPTemplate> saveHolonResult = await Data.SaveHolonAsync<OAPPTemplate>(OAPPTemplate, avatarId, true, true, 0, true, false, providerType);

                    if (saveHolonResult != null && saveHolonResult.Result != null && !saveHolonResult.IsError)
                        result.Message = $"Successfully created the OAPP Template on the {Enum.GetName(typeof(ProviderType), providerType)} provider by AvatarId {avatarId} for OAPPTemplateType {Enum.GetName(typeof(OAPPTemplateType), OAPPTemplateType)}.";
                    else
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured saving the OAPP Template to the {Enum.GetName(typeof(ProviderType), providerType)} provider. Reason: {saveHolonResult.Message}");
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadAvatarAsync on {Enum.GetName(typeof(ProviderType), providerType)} provider. Reason: {avatarResult.Message}");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured saving the OAPP Template to the {Enum.GetName(typeof(ProviderType), providerType)} provider. Reason: {ex}");
            }

            return result;
        }

        public OASISResult<IOAPPTemplateDNA> CreateOAPPTemplate(string name, string description, OAPPTemplateType OAPPTemplateType, Guid avatarId, string fullPathToOAPPTemplate, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IOAPPTemplateDNA> result = new OASISResult<IOAPPTemplateDNA>();
            string errorMessage = "Error occured in OAPPTemplateManager.CreateOAPPTemplate, Reason:";

            try
            {
                OAPPTemplate OAPPTemplate = new OAPPTemplate()
                {
                    Id = Guid.NewGuid(),
                    Name = name,
                    Description = description
                };

                OASISResult<IAvatar> avatarResult = AvatarManager.Instance.LoadAvatar(avatarId, false, true, providerType);

                if (avatarResult != null && avatarResult.Result != null && !avatarResult.IsError)
                {
                    OAPPTemplateDNA OAPPTemplateDNA = new OAPPTemplateDNA()
                    {
                        Id = OAPPTemplate.Id,
                        Name = name,
                        Description = description,
                        OAPPTemplateType = OAPPTemplateType,
                        CreatedByAvatarId = avatarId,
                        CreatedByAvatarUsername = avatarResult.Result.Username,
                        CreatedOn = DateTime.Now,
                        Version = "1.0.0",
                        STARODKVersion = OASISBootLoader.OASISBootLoader.STARODKVersion,
                        OASISVersion = OASISBootLoader.OASISBootLoader.OASISVersion,
                        COSMICVersion = OASISBootLoader.OASISBootLoader.COSMICVersion,
                        DotNetVersion = OASISBootLoader.OASISBootLoader.DotNetVersion
                    };

                    WriteOAPPTemplateDNA(OAPPTemplateDNA, fullPathToOAPPTemplate);

                    OAPPTemplate.OAPPTemplateDNA = OAPPTemplateDNA;
                    OASISResult<OAPPTemplate> saveHolonResult = Data.SaveHolon<OAPPTemplate>(OAPPTemplate, avatarId, true, true, 0, true, false, providerType);

                    if (saveHolonResult != null && saveHolonResult.Result != null && !saveHolonResult.IsError)
                        result.Message = $"Successfully created the OAPP Template on the {Enum.GetName(typeof(ProviderType), providerType)} provider by AvatarId {avatarId} for OAPPTemplateType {Enum.GetName(typeof(OAPPTemplateType), OAPPTemplateType)}.";
                    else
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured saving the OAPP Template to the {Enum.GetName(typeof(ProviderType), providerType)} provider. Reason: {saveHolonResult.Message}");
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadAvatarAsync on {Enum.GetName(typeof(ProviderType), providerType)} provider. Reason: {avatarResult.Message}");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured saving the OAPP Template to the {Enum.GetName(typeof(ProviderType), providerType)} provider. Reason: {ex}");
            }

            return result;
        }

        #region COSMICManagerBase
        public async Task<OASISResult<IOAPPTemplate>> SaveOAPPTemplateAsync(IOAPPTemplate oappTemplate, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IOAPPTemplate> result = new OASISResult<IOAPPTemplate>();

            //if (!Directory.Exists(oappTemplate.OAPPTemplatePath))
            //    Directory.CreateDirectory(oappTemplate.OAPPTemplatePath);

            if (!Directory.Exists(oappTemplate.OAPPTemplateDNA.OAPPTemplatePath))
                Directory.CreateDirectory(oappTemplate.OAPPTemplateDNA.OAPPTemplatePath);

            OASISResult<OAPPTemplate> saveResult = await SaveHolonAsync<OAPPTemplate>(oappTemplate, avatarId, providerType, "OAPPTemplateManager.SaveOAPPTemplateAsync");
            result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(saveResult, result);
            result.Result = saveResult.Result;
            return result;
        }

        public OASISResult<IOAPPTemplate> SaveOAPPTemplate(IOAPPTemplate oappTemplate, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IOAPPTemplate> result = new OASISResult<IOAPPTemplate>();

            //if (!Directory.Exists(oappTemplate.OAPPTemplatePath))
            //    Directory.CreateDirectory(oappTemplate.OAPPTemplatePath);

            if (!Directory.Exists(oappTemplate.OAPPTemplateDNA.OAPPTemplatePath))
                Directory.CreateDirectory(oappTemplate.OAPPTemplateDNA.OAPPTemplatePath);

            OASISResult<OAPPTemplate> saveResult = SaveHolon<OAPPTemplate>(oappTemplate, avatarId, providerType, "OAPPTemplateManager.SaveOAPPTemplate");
            result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(saveResult, result);
            result.Result = saveResult.Result;
            return result;
        }

        public async Task<OASISResult<IOAPPTemplate>> LoadOAPPTemplateAsync(Guid OAPPTemplateId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IOAPPTemplate> result = new OASISResult<IOAPPTemplate>();
            OASISResult<OAPPTemplate> loadResult = await LoadHolonAsync<OAPPTemplate>(OAPPTemplateId, providerType, "OAPPTemplateManager.LoadOAPPTemplateAsync");
            result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(loadResult, result);
            result.Result = loadResult.Result;
            return result;
        }

        public OASISResult<IOAPPTemplate> LoadOAPPTemplate(Guid OAPPTemplateId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IOAPPTemplate> result = new OASISResult<IOAPPTemplate>();
            OASISResult<OAPPTemplate> loadResult = LoadHolon<OAPPTemplate>(OAPPTemplateId, providerType, "OAPPTemplateManager.LoadOAPPTemplate");
            result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(loadResult, result);
            result.Result = loadResult.Result;
            return result;
        }

        public async Task<OASISResult<IEnumerable<IOAPPTemplate>>> LoadAllOAPPTemplatesAsync(ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<IOAPPTemplate>> result = new OASISResult<IEnumerable<IOAPPTemplate>>();
            OASISResult<IEnumerable<OAPPTemplate>> loadHolonsResult = await LoadAllHolonsAsync<OAPPTemplate>(providerType, "OAPPTemplateManager.LoadAllOAPPTemplatesAsync", HolonType.OAPPTemplate);
            result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(loadHolonsResult, result);
            result.Result = loadHolonsResult.Result;
            return result;
        }

        public OASISResult<IEnumerable<IOAPPTemplate>> LoadAllOAPPTemplates(ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<IOAPPTemplate>> result = new OASISResult<IEnumerable<IOAPPTemplate>>();
            OASISResult<IEnumerable<OAPPTemplate>> loadHolonsResult = LoadAllHolons<OAPPTemplate>(providerType, "OAPPTemplateManager.LoadAllOAPPTemplates", HolonType.OAPPTemplate);
            result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(loadHolonsResult, result);
            result.Result = loadHolonsResult.Result;
            return result;
        }

        public async Task<OASISResult<IEnumerable<IOAPPTemplate>>> SearchOAPPTemplatesAsync(string searchTerm, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<IOAPPTemplate>> result = new OASISResult<IEnumerable<IOAPPTemplate>>();
            OASISResult<IEnumerable<OAPPTemplate>> loadHolonsResult = await SearchHolonsAsync<OAPPTemplate>(searchTerm, providerType, "OAPPTemplateManager.SearchOAPPTemplatesAsync", HolonType.OAPPTemplate);
            result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(loadHolonsResult, result);
            result.Result = loadHolonsResult.Result;
            return result;
        }

        public OASISResult<IEnumerable<IOAPPTemplate>> SearchOAPPTemplates(string searchTerm, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<IOAPPTemplate>> result = new OASISResult<IEnumerable<IOAPPTemplate>>();
            OASISResult<IEnumerable<OAPPTemplate>> loadHolonsResult = SearchHolons<OAPPTemplate>(searchTerm, providerType, "OAPPTemplateManager.SearchOAPPTemplates", HolonType.OAPPTemplate);
            result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(loadHolonsResult, result);
            result.Result = loadHolonsResult.Result;
            return result;
        }

        public async Task<OASISResult<IEnumerable<IOAPPTemplate>>> LoadAllOAPPTemplatesForAvatarAsync(Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<IOAPPTemplate>> result = new OASISResult<IEnumerable<IOAPPTemplate>>();
            OASISResult<IEnumerable<OAPPTemplate>> loadHolonsResult = await LoadAllHolonsForAvatarAsync<OAPPTemplate>(avatarId, providerType, "OAPPTemplateManager.LoadAllOAPPTemplatesForAvatarAsync", HolonType.OAPPTemplate);
            result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(loadHolonsResult, result);
            result.Result = loadHolonsResult.Result;
            return result;
        }

        public OASISResult<IEnumerable<IOAPPTemplate>> LoadAllOAPPTemplatesForAvatar(Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<IOAPPTemplate>> result = new OASISResult<IEnumerable<IOAPPTemplate>>();
            OASISResult<IEnumerable<OAPPTemplate>> loadHolonsResult = LoadAllHolonsForAvatar<OAPPTemplate>(avatarId, providerType, "OAPPTemplateManager.LoadAllOAPPTemplatesForAvatarAsync", HolonType.OAPPTemplate);
            result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(loadHolonsResult, result);
            result.Result = loadHolonsResult.Result;
            return result;
        }


        public async Task<OASISResult<IOAPPTemplate>> DeleteOAPPTemplateAsync(Guid oappTemplateId, bool softDelete = true, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IOAPPTemplate> result = new OASISResult<IOAPPTemplate>();
            OASISResult<OAPPTemplate> loadHolonsResult = await DeleteHolonAsync<OAPPTemplate>(oappTemplateId, softDelete, providerType, "OAPPTemplateManager.DeleteOAPPTemplateAsync");
            result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(loadHolonsResult, result);
            result.Result = loadHolonsResult.Result;
            return result;
        }

        public OASISResult<IOAPPTemplate> DeleteOAPPTemplate(Guid oappTemplateId, bool softDelete = true, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IOAPPTemplate> result = new OASISResult<IOAPPTemplate>();
            OASISResult<OAPPTemplate> loadHolonsResult = DeleteHolon<OAPPTemplate>(oappTemplateId, softDelete, providerType, "OAPPTemplateManager.DeleteOAPPTemplate");
            result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(loadHolonsResult, result);
            result.Result = loadHolonsResult.Result;
            return result;
        }

        public async Task<OASISResult<IOAPPTemplate>> DeleteOAPPTemplateAsync(IOAPPTemplate oappTemplate, bool softDelete = true, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IOAPPTemplate> result = new OASISResult<IOAPPTemplate>();
            OASISResult<OAPPTemplate> loadHolonsResult = await DeleteHolonAsync<OAPPTemplate>(oappTemplate.Id, softDelete, providerType, "OAPPTemplateManager.DeleteOAPPTemplateAsync");
            result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(loadHolonsResult, result);
            result.Result = loadHolonsResult.Result;
            return result;
        }

        public OASISResult<IOAPPTemplate> DeleteOAPPTemplate(IOAPPTemplate oappTemplate, bool softDelete = true, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IOAPPTemplate> result = new OASISResult<IOAPPTemplate>();
            OASISResult<OAPPTemplate> loadHolonsResult = DeleteHolon<OAPPTemplate>(oappTemplate.Id, softDelete, providerType, "OAPPTemplateManager.DeleteOAPPTemplate");
            result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(loadHolonsResult, result);
            result.Result = loadHolonsResult.Result;
            return result;
        }
        #endregion

        public async Task<OASISResult<IEnumerable<IOAPPTemplate>>> SearchOAPPTemplatesAsync(string OAPPTemplateName, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<IOAPPTemplate>> result = new OASISResult<IEnumerable<IOAPPTemplate>>();

            LoadAllOAPPTemplates

            return result;
        }

        /*
        #region PublishManagerBase
        public async Task<OASISResult<IOAPPTemplate>> PublishOAPPTemplateAsync(Guid OAPPTemplateId, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IOAPPTemplate> result = new OASISResult<IOAPPTemplate>();
            OASISResult<OAPPTemplate> saveResult = await PublishHolonAsync<OAPPTemplate>(OAPPTemplateId, avatarId, "OAPPTemplateManager.PublishOAPPTemplateAsync", providerType);
            result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(saveResult, result);
            result.Result = saveResult.Result;
            return result;
        }

        public OASISResult<IOAPPTemplate> PublishOAPPTemplate(Guid OAPPTemplateId, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IOAPPTemplate> result = new OASISResult<IOAPPTemplate>();
            OASISResult<OAPPTemplate> saveResult = PublishHolon<OAPPTemplate>(OAPPTemplateId, avatarId, "OAPPTemplateManager.PublishOAPPTemplate", providerType);
            result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(saveResult, result);
            result.Result = saveResult.Result;
            return result;
        }

        public async Task<OASISResult<IOAPPTemplate>> PublishOAPPTemplateAsync(IOAPPTemplate OAPPTemplate, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IOAPPTemplate> result = new OASISResult<IOAPPTemplate>();
            OASISResult<OAPPTemplate> saveResult = await PublishHolonAsync<OAPPTemplate>(OAPPTemplate, avatarId, "OAPPTemplateManager.PublishOAPPTemplateAsync", providerType);
            result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(saveResult, result);
            result.Result = saveResult.Result;
            return result;
        }

        public OASISResult<IOAPPTemplate> PublishOAPPTemplate(IOAPPTemplate OAPPTemplate, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IOAPPTemplate> result = new OASISResult<IOAPPTemplate>();
            OASISResult<OAPPTemplate> saveResult = PublishHolon<OAPPTemplate>(OAPPTemplate, avatarId, "OAPPTemplateManager.PublishOAPPTemplate", providerType);
            result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(saveResult, result);
            result.Result = saveResult.Result;
            return result;
        }

        public async Task<OASISResult<IOAPPTemplate>> UnpublishOAPPTemplateAsync(Guid OAPPTemplateId, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IOAPPTemplate> result = new OASISResult<IOAPPTemplate>();
            OASISResult<OAPPTemplate> saveResult = await UnpublishHolonAsync<OAPPTemplate>(OAPPTemplateId, avatarId, "OAPPTemplateManager.UnpublishOAPPTemplateAsync", providerType);
            result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(saveResult, result);
            result.Result = saveResult.Result;
            return result;
        }

        public OASISResult<IOAPPTemplate> UnpublishOAPPTemplate(Guid OAPPTemplateId, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IOAPPTemplate> result = new OASISResult<IOAPPTemplate>();
            OASISResult<OAPPTemplate> saveResult = UnpublishHolon<OAPPTemplate>(OAPPTemplateId, avatarId, "OAPPTemplateManager.UnpublishOAPPTemplate", providerType);
            result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(saveResult, result);
            result.Result = saveResult.Result;
            return result;
        }

        public async Task<OASISResult<IOAPPTemplate>> UnpublishOAPPTemplateAsync(IOAPPTemplate OAPPTemplate, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IOAPPTemplate> result = new OASISResult<IOAPPTemplate>();
            OASISResult<OAPPTemplate> saveResult = await UnpublishHolonAsync<OAPPTemplate>(OAPPTemplate, avatarId, "OAPPTemplateManager.UnpublishOAPPTemplateAsync", providerType);
            result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(saveResult, result);
            result.Result = saveResult.Result;
            return result;
        }

        public OASISResult<IOAPPTemplate> UnpublishOAPPTemplate(IOAPPTemplate OAPPTemplate, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IOAPPTemplate> result = new OASISResult<IOAPPTemplate>();
            OASISResult<OAPPTemplate> saveResult = UnpublishHolon<OAPPTemplate>(OAPPTemplate, avatarId, "OAPPTemplateManager.UnpublishOAPPTemplate", providerType);
            result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(saveResult, result);
            result.Result = saveResult.Result;
            return result;
        }
        #endregion*/

        public async Task<OASISResult<IOAPPTemplateDNA>> PublishOAPPTemplateTemplateAsync(string fullPathToOAPPTemplate, Guid avatarId, string fullPathToPublishTo = "", bool registerOnSTARNET = true, bool generateOAPPTemplateBinary = true, bool uploadOAPPTemplateToCloud = false, ProviderType providerType = ProviderType.Default, ProviderType oappBinaryProviderType = ProviderType.IPFSOASIS)
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

        public OASISResult<IOAPPTemplateDNA> PublishOAPPTemplate(string fullPathToOAPPTemplate, string launchTarget, Guid avatarId, bool dotnetPublish = true, string fullPathToPublishTo = "", bool registerOnSTARNET = true, bool generateOAPPTemplateSource = true, bool uploadOAPPTemplateSourceToSTARNET = true, bool makeOAPPTemplateSourcePublic = false, bool generateOAPPTemplateBinary = true, bool generateOAPPTemplateSelfContainedBinary = false, bool generateOAPPTemplateSelfContainedFullBinary = false, bool uploadOAPPTemplateToCloud = false, bool uploadOAPPTemplateSelfContainedToCloud = false, bool uploadOAPPTemplateSelfContainedFullToCloud = false, ProviderType providerType = ProviderType.Default, ProviderType oappBinaryProviderType = ProviderType.IPFSOASIS, ProviderType oappSelfContainedBinaryProviderType = ProviderType.None, ProviderType oappSelfContainedFullBinaryProviderType = ProviderType.None)
        {
            OASISResult<IOAPPTemplateDNA> result = new OASISResult<IOAPPTemplateDNA>();
            string errorMessage = "Error occured in OAPPTemplateManager.PublishOAPPTemplateAsync. Reason: ";
            IOAPPTemplateDNA OAPPTemplateDNA = null;
            string tempPath = "";

            try
            {
                OASISResult<IOAPPTemplateDNA> readOAPPTemplateDNAResult = ReadOAPPTemplateDNA(fullPathToOAPPTemplate);

                if (readOAPPTemplateDNAResult != null && !readOAPPTemplateDNAResult.IsError && readOAPPTemplateDNAResult.Result != null)
                {
                    OAPPTemplateDNA = readOAPPTemplateDNAResult.Result;
                    OnOAPPTemplatePublishStatusChanged?.Invoke(this, new OAPPTemplatePublishStatusEventArgs() { OAPPTemplateDNA = OAPPTemplateDNA, Status = Enums.OAPPTemplatePublishStatus.Packaging });
                    OASISResult<IAvatar> loadAvatarResult = AvatarManager.Instance.LoadAvatar(avatarId, false, true, providerType);

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

                        OASISResult<IOAPPTemplate> loadOAPPTemplateResult = LoadOAPPTemplate(OAPPTemplateDNA.Id);

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
                                        StorageClient storage = StorageClient.Create();
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

                                        storage.UploadObject("oasis_oapptemplates", publishedOAPPTemplateFileName, "oapptemplate", fileStream, uploadObjectOptions, progress: progressReporter);
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
                                    OASISResult<IOAPPTemplate> saveLargeOAPPTemplateResult = SaveOAPPTemplate(loadOAPPTemplateResult.Result, avatarId, oappBinaryProviderType);

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

                            OASISResult<IOAPPTemplate> saveOAPPTemplateResult = SaveOAPPTemplate(loadOAPPTemplateResult.Result, avatarId, providerType);

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

        public async Task<OASISResult<IOAPPTemplateDNA>> UnPublishOAPPTemplateAsync(IOAPPTemplateDNA OAPPTemplateDNA, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IOAPPTemplateDNA> result = new OASISResult<IOAPPTemplateDNA>(OAPPTemplateDNA);
            OASISResult<IOAPPTemplate> oappResult = await LoadOAPPTemplateAsync(OAPPTemplateDNA.Id, providerType);
            string errorMessage = "Error occured in UnPublishOAPPTemplateAsync. Reason: ";

            if (oappResult != null && oappResult.Result != null && !oappResult.IsError)
            {
                oappResult.Result.OAPPTemplateDNA.PublishedOn = DateTime.MinValue;
                oappResult.Result.OAPPTemplateDNA.PublishedByAvatarId = Guid.Empty;
                oappResult.Result.OAPPTemplateDNA.PublishedByAvatarUsername = "";

                oappResult = await SaveOAPPTemplateAsync(oappResult.Result, avatarId, providerType);

                if (oappResult != null && oappResult.Result != null && !oappResult.IsError)
                {
                    OAPPTemplateDNA.PublishedOn = DateTime.MinValue;
                    OAPPTemplateDNA.PublishedByAvatarId = Guid.Empty;
                    result.Message = "OAPPTemplate Unpublised";
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured saving the OAPPTemplate with the SaveOAPPTemplateAsync method, reason: {oappResult.Message}");
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured loading the OAPPTemplate with the LoadOAPPTemplateAsync method, reason: {oappResult.Message}");

            return result;
        }

        public OASISResult<IOAPPTemplateDNA> UnPublishOAPPTemplate(IOAPPTemplateDNA OAPPTemplateDNA, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IOAPPTemplateDNA> result = new OASISResult<IOAPPTemplateDNA>(OAPPTemplateDNA);
            OASISResult<IOAPPTemplate> oappResult = LoadOAPPTemplate(OAPPTemplateDNA.Id, providerType);
            string errorMessage = "Error occured in UnPublishOAPPTemplate. Reason: ";

            if (oappResult != null && oappResult.Result != null && !oappResult.IsError)
            {
                oappResult.Result.OAPPTemplateDNA.PublishedOn = DateTime.MinValue;
                oappResult.Result.OAPPTemplateDNA.PublishedByAvatarId = Guid.Empty;
                oappResult.Result.OAPPTemplateDNA.PublishedByAvatarUsername = "";

                oappResult = SaveOAPPTemplate(oappResult.Result, avatarId, providerType);

                if (oappResult != null && oappResult.Result != null && !oappResult.IsError)
                {
                    OAPPTemplateDNA.PublishedOn = DateTime.MinValue;
                    OAPPTemplateDNA.PublishedByAvatarId = Guid.Empty;
                    result.Message = "OAPPTemplate Unpublised";
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured saving the OAPPTemplate with the SaveOAPPTemplate method, reason: {oappResult.Message}");
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured loading the OAPPTemplate with the LoadOAPPTemplate method, reason: {oappResult.Message}");

            return result;
        }

        public async Task<OASISResult<IOAPPTemplateDNA>> UnPublishOAPPTemplateAsync(IOAPPTemplate OAPPTemplate, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IOAPPTemplateDNA> result = new OASISResult<IOAPPTemplateDNA>();
            string errorMessage = "Error occured in UnPublishOAPPTemplateAsync. Reason: ";

            OAPPTemplate.OAPPTemplateDNA.PublishedOn = DateTime.MinValue;
            OAPPTemplate.OAPPTemplateDNA.PublishedByAvatarId = Guid.Empty;
            OAPPTemplate.OAPPTemplateDNA.PublishedByAvatarUsername = "";

            OASISResult<IOAPPTemplate> oappResult = await SaveOAPPTemplateAsync(OAPPTemplate, avatarId, providerType);

            if (oappResult != null && oappResult.Result != null && !oappResult.IsError)
            {
                result.Result = OAPPTemplate.OAPPTemplateDNA; //ConvertOAPPTemplateToOAPPTemplateDNA(OAPPTemplate);
                result.Message = "OAPPTemplate Unpublised";
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured saving the OAPPTemplate with the SaveOAPPTemplateAsync method, reason: {oappResult.Message}");

            return result;
        }

        public OASISResult<IOAPPTemplateDNA> UnPublishOAPPTemplate(IOAPPTemplate OAPPTemplate, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IOAPPTemplateDNA> result = new OASISResult<IOAPPTemplateDNA>();
            string errorMessage = "Error occured in UnPublishOAPPTemplate. Reason: ";

            OAPPTemplate.OAPPTemplateDNA.PublishedOn = DateTime.MinValue;
            OAPPTemplate.OAPPTemplateDNA.PublishedByAvatarId = Guid.Empty;
            OAPPTemplate.OAPPTemplateDNA.PublishedByAvatarUsername = "";

            OASISResult<IOAPPTemplate> oappResult = SaveOAPPTemplate(OAPPTemplate, avatarId, providerType);

            if (oappResult != null && oappResult.Result != null && !oappResult.IsError)
            {
                result.Result = OAPPTemplate.OAPPTemplateDNA; //ConvertOAPPTemplateToOAPPTemplateDNA(OAPPTemplate);
                result.Message = "OAPPTemplate Unpublised";
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured saving the OAPPTemplate with the SaveOAPPTemplate method, reason: {oappResult.Message}");

            return result;
        }

        public async Task<OASISResult<IOAPPTemplateDNA>> UnPublishOAPPTemplateAsync(Guid OAPPTemplateId, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IOAPPTemplateDNA> result = new OASISResult<IOAPPTemplateDNA>();
            OASISResult<IOAPPTemplate> loadResult = await LoadOAPPTemplateAsync(OAPPTemplateId, providerType);

            if (loadResult != null && loadResult.Result != null && !loadResult.IsError)
                result = await UnPublishOAPPTemplateAsync(loadResult.Result, avatarId, providerType);
            else
                OASISErrorHandling.HandleError(ref result, $"Error occured in UnPublishOAPPTemplateAsync loading the OAPPTemplate with the LoadOAPPTemplateAsync method, reason: {loadResult.Message}");

            return result;
        }

        public OASISResult<IOAPPTemplateDNA> UnPublishOAPPTemplate(Guid OAPPTemplateId, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IOAPPTemplateDNA> result = new OASISResult<IOAPPTemplateDNA>();
            OASISResult<IOAPPTemplate> loadResult = LoadOAPPTemplate(OAPPTemplateId, providerType);

            if (loadResult != null && loadResult.Result != null && !loadResult.IsError)
                result = UnPublishOAPPTemplate(loadResult.Result, avatarId, providerType);
            else
                OASISErrorHandling.HandleError(ref result, $"Error occured in UnPublishOAPPTemplate loading the OAPPTemplate with the LoadOAPPTemplate method, reason: {loadResult.Message}");

            return result;
        }

        public async Task<OASISResult<IInstalledOAPPTemplate>> InstallOAPPTemplateAsync(Guid avatarId, string fullPathToPublishedOAPPTemplateFile, string fullInstallPath, bool createOAPPTemplateDirectory = true, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IInstalledOAPPTemplate> result = new OASISResult<IInstalledOAPPTemplate>();
            string errorMessage = "Error occured in OAPPTemplateManager.InstallOAPPTemplateAsync. Reason: ";
            IOAPPTemplateDNA OAPPTemplateDNA = null;

            try
            {
                OASISResult<IOAPPTemplateDNA> OAPPTemplateDNAResult = await ReadOAPPTemplateDNAAsync(fullPathToPublishedOAPPTemplateFile);

                if (OAPPTemplateDNAResult != null && OAPPTemplateDNAResult.Result != null && !OAPPTemplateDNAResult.IsError)
                {
                    //Load the OAPPTemplate from the OASIS to make sure the OAPPTemplateDNA is valid (and has not been tampered with).
                    OASISResult<IOAPPTemplate> oappResult = await LoadOAPPTemplateAsync(OAPPTemplateDNAResult.Result.Id, providerType);

                    if (oappResult != null && oappResult.Result != null && !oappResult.IsError)
                    {
                        //TODO: Not sure if we want to add a check here to compare the OAPPTemplateDNA in the OAPPTemplate dir with the one stored in the OASIS?
                        OAPPTemplateDNA = oappResult.Result.OAPPTemplateDNA;

                        if (createOAPPTemplateDirectory)
                            fullInstallPath = Path.Combine(fullInstallPath, OAPPTemplateDNAResult.Result.Name);

                        if (Directory.Exists(fullInstallPath))
                            Directory.Delete(fullInstallPath, true);

                        Directory.CreateDirectory(fullInstallPath);

                        OnOAPPTemplateInstallStatusChanged?.Invoke(this, new OAPPTemplateInstallStatusEventArgs() { OAPPTemplateDNA = OAPPTemplateDNAResult.Result, Status = Enums.OAPPTemplateInstallStatus.Decompressing });
                        ZipFile.ExtractToDirectory(fullPathToPublishedOAPPTemplateFile, fullInstallPath, Encoding.Default, true);

                        OnOAPPTemplateInstallStatusChanged?.Invoke(this, new OAPPTemplateInstallStatusEventArgs() { OAPPTemplateDNA = OAPPTemplateDNAResult.Result, Status = Enums.OAPPTemplateInstallStatus.Installing });
                        OASISResult<IAvatar> avatarResult = await AvatarManager.Instance.LoadAvatarAsync(avatarId, false, true, providerType);

                        if (avatarResult != null && !avatarResult.IsError && avatarResult.Result != null)
                        {
                            InstalledOAPPTemplate installedOAPPTemplate = new InstalledOAPPTemplate()
                            {
                                //OAPPTemplateId = OAPPTemplateDNAResult.Result.OAPPTemplateId,
                                OAPPTemplateDNA = OAPPTemplateDNAResult.Result,
                                InstalledBy = avatarId,
                                InstalledByAvatarUsername = avatarResult.Result.Username,
                                InstalledOn = DateTime.Now,
                                InstalledPath = fullInstallPath
                            };

                            OASISResult<IHolon> saveResult = await installedOAPPTemplate.SaveAsync();

                            if (saveResult != null && saveResult.Result != null && !saveResult.IsError)
                            {
                                result.Result = installedOAPPTemplate;
                                OAPPTemplateDNA.Downloads++;
                                oappResult.Result.OAPPTemplateDNA = OAPPTemplateDNA;

                                OASISResult<IOAPPTemplate> oappSaveResult = await SaveOAPPTemplateAsync(oappResult.Result, avatarId, providerType);

                                if (oappSaveResult != null && !oappSaveResult.IsError && oappSaveResult.Result != null)
                                {
                                    if (OAPPTemplateDNAResult.Result.STARODKVersion != OASISBootLoader.OASISBootLoader.STARODKVersion)
                                        OASISErrorHandling.HandleWarning(ref result, $"The STAR ODK Version {OAPPTemplateDNAResult.Result.STARODKVersion} does not match the current version {OASISBootLoader.OASISBootLoader.STARODKVersion}. This may lead to issues, it is recommended to make sure the versions match.");

                                    if (OAPPTemplateDNAResult.Result.OASISVersion != OASISBootLoader.OASISBootLoader.OASISVersion)
                                        OASISErrorHandling.HandleWarning(ref result, $"The OASIS Version {OAPPTemplateDNAResult.Result.OASISVersion} does not match the current version {OASISBootLoader.OASISBootLoader.OASISVersion}. This may lead to issues, it is recommended to make sure the versions match.");

                                    if (OAPPTemplateDNAResult.Result.COSMICVersion != OASISBootLoader.OASISBootLoader.COSMICVersion)
                                        OASISErrorHandling.HandleWarning(ref result, $"The COSMIC Version {OAPPTemplateDNAResult.Result.COSMICVersion} does not match the current version {OASISBootLoader.OASISBootLoader.COSMICVersion}. This may lead to issues, it is recommended to make sure the versions match.");

                                    if (result.InnerMessages.Count > 0)
                                        result.Message = $"OAPP Template successfully installed but there were {result.WarningCount} warnings:\n\n {OASISResultHelper.BuildInnerMessageError(result.InnerMessages)}";
                                    else
                                        result.Message = "OAPP Template Successfully Installed";

                                    OnOAPPTemplateInstallStatusChanged?.Invoke(this, new OAPPTemplateInstallStatusEventArgs() { OAPPTemplateDNA = OAPPTemplateDNAResult.Result, Status = Enums.OAPPTemplateInstallStatus.Installed });
                                }
                                else
                                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling SaveOAPPTemplateAsync method. Reason: {oappSaveResult.Message}");
                            }
                            else
                                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling SaveAsync method. Reason: {saveResult.Message}");
                        }
                        else
                            OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadAvatarAsync method. Reason: {avatarResult.Message}");
                    }
                    else
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadOAPPTemplateAsync method. Reason: {oappResult.Message}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} {ex}");
            }

            if (result.IsError)
                OnOAPPTemplateInstallStatusChanged?.Invoke(this, new OAPPTemplateInstallStatusEventArgs() { OAPPTemplateDNA = OAPPTemplateDNA, Status = Enums.OAPPTemplateInstallStatus.Error, ErrorMessage = result.Message });

            return result;
        }

        public OASISResult<IInstalledOAPPTemplate> InstallOAPPTemplate(Guid avatarId, string fullPathToPublishedOAPPTemplateFile, string fullInstallPath, bool createOAPPTemplateDirectory = true, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IInstalledOAPPTemplate> result = new OASISResult<IInstalledOAPPTemplate>();
            string errorMessage = "Error occured in OAPPTemplateManager.InstallOAPPTemplate. Reason: ";

            try
            {
                ZipFile.ExtractToDirectory(fullPathToPublishedOAPPTemplateFile, fullInstallPath, Encoding.Default, true);
                OASISResult<IOAPPTemplateDNA> OAPPTemplateDNAResult = ReadOAPPTemplateDNA(fullInstallPath);

                if (OAPPTemplateDNAResult != null && OAPPTemplateDNAResult.Result != null && !OAPPTemplateDNAResult.IsError)
                {
                    OASISResult<IAvatar> avatarResult = AvatarManager.Instance.LoadAvatar(avatarId, false, true, providerType);

                    if (avatarResult != null && !avatarResult.IsError && avatarResult.Result != null)
                    {
                        InstalledOAPPTemplate installedOAPPTemplate = new InstalledOAPPTemplate()
                        {
                            //OAPPTemplateId = OAPPTemplateDNAResult.Result.OAPPTemplateId,
                            OAPPTemplateDNA = OAPPTemplateDNAResult.Result,
                            InstalledBy = avatarId,
                            InstalledByAvatarUsername = avatarResult.Result.Username,
                            InstalledOn = DateTime.Now,
                            InstalledPath = fullInstallPath
                        };

                        OASISResult<IHolon> saveResult = installedOAPPTemplate.Save();

                        if (saveResult != null && saveResult.Result != null && !saveResult.IsError)
                        {
                            result.Result = installedOAPPTemplate;

                            if (OAPPTemplateDNAResult.Result.STARODKVersion != OASISBootLoader.OASISBootLoader.STARODKVersion)
                                OASISErrorHandling.HandleWarning(ref result, $"The STAR ODK Version {OAPPTemplateDNAResult.Result.STARODKVersion} does not match the current version {OASISBootLoader.OASISBootLoader.STARODKVersion}. This may lead to issues, it is recommended to make sure the versions match.");

                            if (OAPPTemplateDNAResult.Result.OASISVersion != OASISBootLoader.OASISBootLoader.OASISVersion)
                                OASISErrorHandling.HandleWarning(ref result, $"The OASIS Version {OAPPTemplateDNAResult.Result.OASISVersion} does not match the current version {OASISBootLoader.OASISBootLoader.OASISVersion}. This may lead to issues, it is recommended to make sure the versions match.");

                            if (OAPPTemplateDNAResult.Result.COSMICVersion != OASISBootLoader.OASISBootLoader.COSMICVersion)
                                OASISErrorHandling.HandleWarning(ref result, $"The COSMIC Version {OAPPTemplateDNAResult.Result.COSMICVersion} does not match the current version {OASISBootLoader.OASISBootLoader.COSMICVersion}. This may lead to issues, it is recommended to make sure the versions match.");

                            if (result.InnerMessages.Count > 0)
                                result.Message = $"OAPP Template successfully installed but there were {result.WarningCount} warnings:\n\n {OASISResultHelper.BuildInnerMessageError(result.InnerMessages)}";
                            else
                                result.Message = "OAPP Template Successfully Installed";
                        }
                        else
                            OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling Save method. Reason: {saveResult.Message}");
                    }
                    else
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadAvatar method. Reason: {avatarResult.Message}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} {ex}");
            }

            return result;
        }

        public async Task<OASISResult<IInstalledOAPPTemplate>> InstallOAPPTemplateAsync(Guid avatarId, IOAPPTemplate OAPPTemplate, string fullInstallPath, bool createOAPPTemplateDirectory = true, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IInstalledOAPPTemplate> result = new OASISResult<IInstalledOAPPTemplate>();
            string errorMessage = "Error occured in OAPPTemplateManager.InstallOAPPTemplateAsync. Reason: ";

            try
            {
                string OAPPTemplatePath = Path.Combine("temp", OAPPTemplate.Name, ".oapp");

                if (OAPPTemplate.PublishedOAPPTemplate != null)
                {
                    await File.WriteAllBytesAsync(OAPPTemplatePath, OAPPTemplate.PublishedOAPPTemplate);
                    result = await InstallOAPPTemplateAsync(avatarId, OAPPTemplatePath, fullInstallPath, createOAPPTemplateDirectory, providerType);
                }
                else
                {
                    //OASISErrorHandling.HandleWarning(ref result, "The OAPPTemplate.PublishedOAPPTemplate property is null! Please make sure this OAPPTemplate was published to STARNET and try again.");
                    // FileStream fileStream = null;

                    try
                    {
                        StorageClient storage = await StorageClient.CreateAsync();

                        // set minimum chunksize just to see progress updating
                        var downloadObjectOptions = new DownloadObjectOptions
                        {
                            ChunkSize = UploadObjectOptions.MinimumChunkSize,
                        };

                        var progressReporter = new Progress<Google.Apis.Download.IDownloadProgress>(OnDownloadProgress);

                        using var fileStream = File.OpenWrite(OAPPTemplatePath);
                        _fileLength = fileStream.Length;
                        _progress = 0;

                        OnOAPPTemplateInstallStatusChanged?.Invoke(this, new OAPPTemplateInstallStatusEventArgs() { OAPPTemplateDNA = OAPPTemplate.OAPPTemplateDNA, Status = Enums.OAPPTemplateInstallStatus.Downloading });
                        await storage.DownloadObjectAsync("oasis_oapptemplates", string.Concat(OAPPTemplate.Name, ".oapptemplates"), fileStream, downloadObjectOptions, progress: progressReporter);
                        result = await InstallOAPPTemplateAsync(avatarId, OAPPTemplatePath, fullInstallPath, createOAPPTemplateDirectory, providerType);
                    }
                    catch (Exception ex)
                    {
                        OASISErrorHandling.HandleError(ref result, $"An error occured downloading the OAPP Template from cloud storage. Reason: {ex}");
                    }
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} {ex}");
            }

            if (result.IsError)
                OnOAPPTemplateInstallStatusChanged?.Invoke(this, new OAPPTemplateInstallStatusEventArgs() { OAPPTemplateDNA = OAPPTemplate.OAPPTemplateDNA, Status = Enums.OAPPTemplateInstallStatus.Error, ErrorMessage = result.Message });

            return result;
        }

        public OASISResult<IInstalledOAPPTemplate> InstallOAPPTemplate(Guid avatarId, IOAPPTemplate OAPPTemplate, string fullInstallPath, bool createOAPPTemplateDirectory = true, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IInstalledOAPPTemplate> result = new OASISResult<IInstalledOAPPTemplate>();
            string errorMessage = "Error occured in OAPPTemplateManager.InstallOAPPTemplate. Reason: ";

            try
            {
                if (OAPPTemplate.PublishedOAPPTemplate != null)
                {
                    string OAPPTemplatePath = Path.Combine("temp", OAPPTemplate.Name, ".oapptemplate");
                    File.WriteAllBytes(OAPPTemplatePath, OAPPTemplate.PublishedOAPPTemplate);
                    result = InstallOAPPTemplate(avatarId, OAPPTemplatePath, fullInstallPath, createOAPPTemplateDirectory, providerType);
                }
                else
                    OASISErrorHandling.HandleError(ref result, "The OAPPTemplate.PublishedOAPPTemplate property is null! Please make sure this OAPPTemplate was published to STARNET and try again.");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} {ex}");
            }

            return result;
        }

        public async Task<OASISResult<IInstalledOAPPTemplate>> InstallOAPPTemplateAsync(Guid avatarId, Guid OAPPTemplateId, string fullInstallPath, bool createOAPPTemplateDirectory = true, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IInstalledOAPPTemplate> result = new OASISResult<IInstalledOAPPTemplate>();
            OASISResult<IOAPPTemplate> OAPPTemplateResult = await LoadOAPPTemplateAsync(OAPPTemplateId, providerType);

            if (OAPPTemplateResult != null && !OAPPTemplateResult.IsError && OAPPTemplateResult.Result != null)
                result = await InstallOAPPTemplateAsync(avatarId, OAPPTemplateResult.Result, fullInstallPath, createOAPPTemplateDirectory, providerType);
            else
                OASISErrorHandling.HandleError(ref result, $"Error occured in OAPPTemplateManager.InstallOAPPTemplateAsync loading the OAPP Template with the LoadOAPPTemplateAsync method, reason: {result.Message}");

            return result;
        }

        public OASISResult<IInstalledOAPPTemplate> InstallOAPPTemplate(Guid avatarId, Guid OAPPTemplateId, string fullInstallPath, bool createOAPPTemplateDirectory = true, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IInstalledOAPPTemplate> result = new OASISResult<IInstalledOAPPTemplate>();
            OASISResult<IOAPPTemplate> OAPPTemplateResult = LoadOAPPTemplate(OAPPTemplateId, providerType);

            if (OAPPTemplateResult != null && !OAPPTemplateResult.IsError && OAPPTemplateResult.Result != null)
                result = InstallOAPPTemplate(avatarId, OAPPTemplateResult.Result, fullInstallPath, createOAPPTemplateDirectory, providerType);
            else
                OASISErrorHandling.HandleError(ref result, $"Error occured in OAPPTemplateManager.InstallOAPPTemplate loading the OAPP Template with the LoadOAPPTemplate method, reason: {result.Message}");

            return result;
        }

        public async Task<OASISResult<IOAPPTemplateDNA>> UnInstallOAPPTemplateAsync(IOAPPTemplateDNA OAPPTemplate, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            return await UnInstallOAPPTemplateAsync(OAPPTemplate.Id, avatarId, providerType);
        }

        public OASISResult<IOAPPTemplateDNA> UnInstallOAPPTemplate(IOAPPTemplateDNA OAPPTemplate, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            return UnInstallOAPPTemplate(OAPPTemplate.Id, avatarId, providerType);
        }

        public async Task<OASISResult<IOAPPTemplateDNA>> UnInstallOAPPTemplateAsync(Guid OAPPTemplateId, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IOAPPTemplateDNA> result = new OASISResult<IOAPPTemplateDNA>();
            OASISResult<IEnumerable<InstalledOAPPTemplate>> intalledOAPPTemplateResult = await Data.LoadHolonsForParentAsync<InstalledOAPPTemplate>(avatarId, HolonType.InstalledOAPPTemplate, false, false, 0, true, false, 0, HolonType.All, 0, providerType);
            string errorMessage = "Error occured in OAPPTemplateManager.UnInstallOAPPTemplateAsync. Reason: ";

            if (intalledOAPPTemplateResult != null && !intalledOAPPTemplateResult.IsError && intalledOAPPTemplateResult.Result != null)
            {
                InstalledOAPPTemplate installedOAPPTemplate = intalledOAPPTemplateResult.Result.FirstOrDefault(x => x.OAPPTemplateDNA.Id == OAPPTemplateId);

                if (installedOAPPTemplate != null)
                {
                    OASISResult<IHolon> holonResult = await installedOAPPTemplate.DeleteAsync(false, providerType);

                    if (holonResult != null && !holonResult.IsError && holonResult.Result != null)
                    {
                        result.Message = "OAPP Template Uninstalled";
                        result.Result = installedOAPPTemplate.OAPPTemplateDNA;
                    }
                    else
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling DeleteAsync. Reason: {holonResult.Message}");
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} No installed OAPPTemplate was found for the Id {OAPPTemplateId}.");
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonsForParentAsync. Reason: {intalledOAPPTemplateResult.Message}");

            return result;
        }

        public OASISResult<IOAPPTemplateDNA> UnInstallOAPPTemplate(Guid OAPPTemplateId, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IOAPPTemplateDNA> result = new OASISResult<IOAPPTemplateDNA>();
            OASISResult<IEnumerable<InstalledOAPPTemplate>> intalledOAPPTemplateResult = Data.LoadHolonsForParent<InstalledOAPPTemplate>(avatarId, HolonType.InstalledOAPPTemplate, false, false, 0, true, false, 0, HolonType.All, 0, providerType);
            string errorMessage = "Error occured in OAPPTemplateManager.UnInstallOAPPTemplate. Reason: ";

            if (intalledOAPPTemplateResult != null && !intalledOAPPTemplateResult.IsError && intalledOAPPTemplateResult.Result != null)
            {
                InstalledOAPPTemplate installedOAPPTemplate = intalledOAPPTemplateResult.Result.FirstOrDefault(x => x.OAPPTemplateDNA.Id == OAPPTemplateId);

                if (installedOAPPTemplate != null)
                {
                    OASISResult<IHolon> holonResult = installedOAPPTemplate.Delete(false, providerType);

                    if (holonResult != null && !holonResult.IsError && holonResult.Result != null)
                    {
                        result.Message = "OAPP Template Uninstalled";
                        result.Result = installedOAPPTemplate.OAPPTemplateDNA;
                    }
                    else
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling Delete. Reason: {holonResult.Message}");
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} No installed OAPPTemplate was found for the Id {OAPPTemplateId}.");
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonsForParent. Reason: {intalledOAPPTemplateResult.Message}");

            return result;
        }

        public async Task<OASISResult<IEnumerable<IInstalledOAPPTemplate>>> ListInstalledOAPPTemplatesAsync(Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<IInstalledOAPPTemplate>> result = new OASISResult<IEnumerable<IInstalledOAPPTemplate>>();
            OASISResult<IEnumerable<InstalledOAPPTemplate>> installedOAPPTemplatesResult = await Data.LoadHolonsForParentAsync<InstalledOAPPTemplate>(avatarId, HolonType.InstalledOAPPTemplate, false, false, 0, true, false, 0, HolonType.All, 0, providerType);
            string errorMessage = "Error occured in OAPPTemplateManager.ListInstalledOAPPTemplatesAsync. Reason: ";

            if (installedOAPPTemplatesResult != null && !installedOAPPTemplatesResult.IsError && installedOAPPTemplatesResult.Result != null)
            {
                result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<IEnumerable<InstalledOAPPTemplate>, IEnumerable<IInstalledOAPPTemplate>>(installedOAPPTemplatesResult);
                result.Result = Mapper.Convert<InstalledOAPPTemplate, IInstalledOAPPTemplate>(installedOAPPTemplatesResult.Result);
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonsForParentAsync. Reason: {installedOAPPTemplatesResult.Message}");

            return result;
        }

        public OASISResult<IEnumerable<IInstalledOAPPTemplate>> ListInstalledOAPPTemplates(Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<IInstalledOAPPTemplate>> result = new OASISResult<IEnumerable<IInstalledOAPPTemplate>>();
            OASISResult<IEnumerable<InstalledOAPPTemplate>> installedOAPPTemplatesResult = Data.LoadHolonsForParent<InstalledOAPPTemplate>(avatarId, HolonType.InstalledOAPPTemplate, false, false, 0, true, false, 0, HolonType.All, 0, providerType);
            string errorMessage = "Error occured in OAPPTemplateManager.ListInstalledOAPPTemplates. Reason: ";

            if (installedOAPPTemplatesResult != null && !installedOAPPTemplatesResult.IsError && installedOAPPTemplatesResult.Result != null)
            {
                result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<IEnumerable<InstalledOAPPTemplate>, IEnumerable<IInstalledOAPPTemplate>>(installedOAPPTemplatesResult);
                result.Result = Mapper.Convert<InstalledOAPPTemplate, IInstalledOAPPTemplate>(installedOAPPTemplatesResult.Result);
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonsForParent. Reason: {installedOAPPTemplatesResult.Message}");

            return result;
        }

        public async Task<OASISResult<bool>> IsOAPPTemplateInstalledAsync(Guid avatarId, Guid OAPPTemplateId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<bool> result = new OASISResult<bool>();
            OASISResult<IEnumerable<InstalledOAPPTemplate>> installedOAPPTemplatesResult = await Data.LoadHolonsForParentAsync<InstalledOAPPTemplate>(avatarId, HolonType.InstalledOAPPTemplate, false, false, 0, true, false, 0, HolonType.All, 0, providerType);
            string errorMessage = "Error occured in OAPPTemplateManager.IsOAPPTemplateInstalledAsync. Reason: ";

            if (installedOAPPTemplatesResult != null && !installedOAPPTemplatesResult.IsError && installedOAPPTemplatesResult.Result != null)
                result.Result = installedOAPPTemplatesResult.Result.Any(x => x.OAPPTemplateDNA.Id == OAPPTemplateId);
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonsForParentAsync. Reason: {installedOAPPTemplatesResult.Message}");

            return result;
        }

        public OASISResult<bool> IsOAPPTemplateInstalled(Guid avatarId, Guid OAPPTemplateId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<bool> result = new OASISResult<bool>();
            OASISResult<IEnumerable<InstalledOAPPTemplate>> installedOAPPTemplatesResult = Data.LoadHolonsForParent<InstalledOAPPTemplate>(avatarId, HolonType.InstalledOAPPTemplate, false, false, 0, true, false, 0, HolonType.All, 0, providerType);
            string errorMessage = "Error occured in OAPPTemplateManager.IsOAPPTemplateInstalled. Reason: ";

            if (installedOAPPTemplatesResult != null && !installedOAPPTemplatesResult.IsError && installedOAPPTemplatesResult.Result != null)
                result.Result = installedOAPPTemplatesResult.Result.Any(x => x.OAPPTemplateDNA.Id == OAPPTemplateId);
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonsForParent. Reason: {installedOAPPTemplatesResult.Message}");

            return result;
        }

        public async Task<OASISResult<bool>> IsOAPPTemplateInstalledAsync(Guid avatarId, string OAPPTemplateName, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<bool> result = new OASISResult<bool>();
            OASISResult<IEnumerable<InstalledOAPPTemplate>> installedOAPPTemplatesResult = await Data.LoadHolonsForParentAsync<InstalledOAPPTemplate>(avatarId, HolonType.InstalledOAPPTemplate, false, false, 0, true, false, 0, HolonType.All, 0, providerType);
            string errorMessage = "Error occured in OAPPTemplateManager.IsOAPPTemplateInstalledAsync. Reason: ";

            if (installedOAPPTemplatesResult != null && !installedOAPPTemplatesResult.IsError && installedOAPPTemplatesResult.Result != null)
                result.Result = installedOAPPTemplatesResult.Result.Any(x => x.OAPPTemplateDNA.Name == OAPPTemplateName);
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonsForParentAsync. Reason: {installedOAPPTemplatesResult.Message}");

            return result;
        }

        public OASISResult<bool> IsOAPPTemplateInstalled(Guid avatarId, string OAPPTemplateName, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<bool> result = new OASISResult<bool>();
            string errorMessage = "Error occured in OAPPTemplateManager.IsOAPPTemplateInstalled. Reason: ";
            OASISResult<IEnumerable<InstalledOAPPTemplate>> installedOAPPTemplatesResult = Data.LoadHolonsForParent<InstalledOAPPTemplate>(avatarId, HolonType.InstalledOAPPTemplate, false, false, 0, true, false, 0, HolonType.All, 0, providerType);

            if (installedOAPPTemplatesResult != null && !installedOAPPTemplatesResult.IsError && installedOAPPTemplatesResult.Result != null)
                result.Result = installedOAPPTemplatesResult.Result.Any(x => x.OAPPTemplateDNA.Name == OAPPTemplateName);
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonsForParent. Reason: {installedOAPPTemplatesResult.Message}");

            return result;
        }

        public async Task<OASISResult<IInstalledOAPPTemplate>> LoadInstalledOAPPTemplateAsync(Guid avatarId, Guid OAPPTemplateId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IInstalledOAPPTemplate> result = new OASISResult<IInstalledOAPPTemplate>();
            string errorMessage = "Error occured in OAPPTemplateManager.LoadInstalledOAPPTemplateAsync. Reason: ";
            OASISResult<IEnumerable<InstalledOAPPTemplate>> installedOAPPTemplatesResult = await Data.LoadHolonsForParentAsync<InstalledOAPPTemplate>(avatarId, HolonType.InstalledOAPPTemplate, false, false, 0, true, false, 0, HolonType.All, 0, providerType);

            if (installedOAPPTemplatesResult != null && !installedOAPPTemplatesResult.IsError && installedOAPPTemplatesResult.Result != null)
                result.Result = installedOAPPTemplatesResult.Result.FirstOrDefault(x => x.OAPPTemplateDNA.Id == OAPPTemplateId);
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonsForParentAsync. Reason: {installedOAPPTemplatesResult.Message}");

            return result;
        }

        public OASISResult<IInstalledOAPPTemplate> LoadInstalledOAPPTemplate(Guid avatarId, Guid OAPPTemplateId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IInstalledOAPPTemplate> result = new OASISResult<IInstalledOAPPTemplate>();
            string errorMessage = "Error occured in OAPPTemplateManager.LoadInstalledOAPPTemplate. Reason: ";
            OASISResult<IEnumerable<InstalledOAPPTemplate>> installedOAPPTemplatesResult = Data.LoadHolonsForParent<InstalledOAPPTemplate>(avatarId, HolonType.InstalledOAPPTemplate, false, false, 0, true, false, 0, HolonType.All, 0, providerType);

            if (installedOAPPTemplatesResult != null && !installedOAPPTemplatesResult.IsError && installedOAPPTemplatesResult.Result != null)
                result.Result = installedOAPPTemplatesResult.Result.FirstOrDefault(x => x.OAPPTemplateDNA.Id == OAPPTemplateId);
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonsForParent. Reason: {installedOAPPTemplatesResult.Message}");

            return result;
        }

        public async Task<OASISResult<IInstalledOAPPTemplate>> LoadInstalledOAPPTemplateAsync(Guid avatarId, string OAPPTemplateName, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IInstalledOAPPTemplate> result = new OASISResult<IInstalledOAPPTemplate>();
            string errorMessage = "Error occured in OAPPTemplateManager.LoadInstalledOAPPTemplateAsync. Reason: ";
            OASISResult<IEnumerable<InstalledOAPPTemplate>> installedOAPPTemplatesResult = await Data.LoadHolonsForParentAsync<InstalledOAPPTemplate>(avatarId, HolonType.InstalledOAPPTemplate, false, false, 0, true, false, 0, HolonType.All, 0, providerType);

            if (installedOAPPTemplatesResult != null && !installedOAPPTemplatesResult.IsError && installedOAPPTemplatesResult.Result != null)
                result.Result = installedOAPPTemplatesResult.Result.FirstOrDefault(x => x.OAPPTemplateDNA.Name == OAPPTemplateName);
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonsForParentAsync. Reason: {installedOAPPTemplatesResult.Message}");

            return result;
        }

        public OASISResult<IInstalledOAPPTemplate> LoadInstalledOAPPTemplate(Guid avatarId, string OAPPTemplateName, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IInstalledOAPPTemplate> result = new OASISResult<IInstalledOAPPTemplate>();
            string errorMessage = "Error occured in OAPPTemplateManager.LoadInstalledOAPPTemplate. Reason: ";
            OASISResult<IEnumerable<InstalledOAPPTemplate>> installedOAPPTemplatesResult = Data.LoadHolonsForParent<InstalledOAPPTemplate>(avatarId, HolonType.InstalledOAPPTemplate, false, false, 0, true, false, 0, HolonType.All, 0, providerType);

            if (installedOAPPTemplatesResult != null && !installedOAPPTemplatesResult.IsError && installedOAPPTemplatesResult.Result != null)
                result.Result = installedOAPPTemplatesResult.Result.FirstOrDefault(x => x.OAPPTemplateDNA.Name == OAPPTemplateName);
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonsForParent. Reason: {installedOAPPTemplatesResult.Message}");

            return result;
        }

        public OASISResult<IInstalledOAPPTemplate> OpenOAPPTemplateFolder(Guid avatarId, IInstalledOAPPTemplate OAPPTemplate, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IInstalledOAPPTemplate> result = new OASISResult<IInstalledOAPPTemplate>();
            string errorMessage = "An error occured in OAPPTemplateManager.OpenOAPPTemplateFolder. Reason:";

            if (OAPPTemplate != null)
            {
                try
                {
                    Process.Start("explorer.exe", result.Result.InstalledPath);
                }
                catch (Exception e)
                {
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured attempting to open the folder {result.Result.InstalledPath}. Reason: {e}");
                }
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} The OAPPTemplate is null!");

            return result;
        }

        public async Task<OASISResult<IInstalledOAPPTemplate>> OpenOAPPTemplateFolderAsync(Guid avatarId, Guid OAPPTemplateId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IInstalledOAPPTemplate> result = new OASISResult<IInstalledOAPPTemplate>();
            string errorMessage = "An error occured in OAPPTemplateManager.OpenOAPPTemplateFolderAsync. Reason:";
            result = await LoadInstalledOAPPTemplateAsync(avatarId, OAPPTemplateId);

            if (result != null && !result.IsError && result.Result != null)
                OpenOAPPTemplateFolder(avatarId, result.Result, providerType);
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the OAPP Template with the LoadInstalledOAPPTemplateAsync method, reason: {result.Message}");

            return result;
        }

        public OASISResult<IInstalledOAPPTemplate> OpenOAPPTemplateFolder(Guid avatarId, Guid OAPPTemplateId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IInstalledOAPPTemplate> result = new OASISResult<IInstalledOAPPTemplate>();
            string errorMessage = "An error occured in OAPPTemplateManager.OpenOAPPTemplateFolder. Reason:";
            result = LoadInstalledOAPPTemplate(avatarId, OAPPTemplateId);

            if (result != null && !result.IsError && result.Result != null)
                OpenOAPPTemplateFolder(avatarId, result.Result, providerType);
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the OAPP Template with the LoadInstalledOAPPTemplate method, reason: {result.Message}");

            return result;
        }


        //private IOAPPTemplateDNA ConvertOAPPTemplateToOAPPTemplateDNA(IOAPPTemplate OAPPTemplate)
        //{
        //    OAPPTemplateDNA OAPPTemplateDNA = new OAPPTemplateDNA()
        //    {
        //        CelestialBodyId = OAPPTemplate.CelestialBodyId,
        //        //CelestialBody = OAPPTemplate.CelestialBody,
        //        CelestialBodyName = OAPPTemplate.CelestialBody != null ? OAPPTemplate.CelestialBody.Name : "",
        //        CelestialBodyType = OAPPTemplate.CelestialBody != null ? OAPPTemplate.CelestialBody.HolonType : HolonType.None,
        //        CreatedByAvatarId = OAPPTemplate.CreatedByAvatarId,
        //        CreatedByAvatarUsername = OAPPTemplate.CreatedByAvatarUsername,
        //        CreatedOn = OAPPTemplate.CreatedDate,
        //        Description = OAPPTemplate.Description,
        //        GenesisType = OAPPTemplate.GenesisType,
        //        OAPPTemplateId = OAPPTemplate.Id,
        //        OAPPTemplateName = OAPPTemplate.Name,
        //        OAPPTemplateType = OAPPTemplate.OAPPTemplateType,
        //        PublishedByAvatarId = OAPPTemplate.PublishedByAvatarId,
        //        PublishedByAvatarUsername = OAPPTemplate.PublishedByAvatarUsername,
        //        PublishedOn = OAPPTemplate.PublishedOn,
        //        PublishedOnSTARNET = OAPPTemplate.PublishedOAPPTemplate != null,
        //        Version = OAPPTemplate.Version.ToString()
        //    };

        //    List<IZome> zomes = new List<IZome>();
        //    foreach (IHolon holon in OAPPTemplate.Children)
        //        zomes.Add((IZome)holon);

        //   //OAPPTemplateDNA.Zomes = zomes;
        //    return OAPPTemplateDNA;
        //}

        public async Task<OASISResult<bool>> WriteOAPPTemplateDNAAsync(IOAPPTemplateDNA OAPPTemplateDNA, string fullPathToOAPPTemplate)
        {
            OASISResult<bool> result = new OASISResult<bool>();

            try
            {
                JsonSerializerOptions options = new()
                {
                    ReferenceHandler = ReferenceHandler.Preserve,
                    WriteIndented = true
                };

                await File.WriteAllTextAsync(Path.Combine(fullPathToOAPPTemplate, "OAPPTemplateDNA.json"), JsonSerializer.Serialize((OAPPTemplateDNA)OAPPTemplateDNA, options));
                result.Result = true;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"An error occured writing the OAPPTemplateDNA in WriteOAPPTemplateDNAAsync: Reason: {ex.Message}");
            }

            return result;
        }

        public OASISResult<bool> WriteOAPPTemplateDNA(IOAPPTemplateDNA OAPPTemplateDNA, string fullPathToOAPPTemplate)
        {
            OASISResult<bool> result = new OASISResult<bool>();

            try
            {
                File.WriteAllText(Path.Combine(fullPathToOAPPTemplate, "OAPPTemplateDNA.json"), JsonSerializer.Serialize((OAPPTemplateDNA)OAPPTemplateDNA));
                result.Result = true;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"An error occured writing the OAPPTemplateDNA in WriteOAPPTemplateDNA: Reason: {ex.Message}");
            }

            return result;
        }

        public async Task<OASISResult<IOAPPTemplateDNA>> ReadOAPPTemplateDNAAsync(string fullPathToOAPPTemplate)
        {
            OASISResult<IOAPPTemplateDNA> result = new OASISResult<IOAPPTemplateDNA>();

            try
            {
                result.Result = JsonSerializer.Deserialize<OAPPTemplateDNA>(await File.ReadAllTextAsync(Path.Combine(fullPathToOAPPTemplate, "OAPPTemplateDNA.json")));
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"An error occured reading the OAPPTemplateDNA in ReadOAPPTemplateDNAAsync: Reason: {ex.Message}");
            }

            return result;
        }

        public OASISResult<IOAPPTemplateDNA> ReadOAPPTemplateDNA(string fullPathToOAPPTemplate)
        {
            OASISResult<IOAPPTemplateDNA> result = new OASISResult<IOAPPTemplateDNA>();

            try
            {
                result.Result = JsonSerializer.Deserialize<OAPPTemplateDNA>(File.ReadAllText(Path.Combine(fullPathToOAPPTemplate, "OAPPTemplateDNA.json")));
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"An error occured reading the OAPPTemplateDNA in ReadOAPPTemplateDNA: Reason: {ex.Message}");
            }

            return result;
        }

        private void OnUploadProgress(Google.Apis.Upload.IUploadProgress progress)
        {
            switch (progress.Status)
            {
                case Google.Apis.Upload.UploadStatus.NotStarted:
                    _progress = 0;
                    OnOAPPTemplateUploadStatusChanged?.Invoke(this, new OAPPTemplateUploadProgressEventArgs() { Progress = _progress, Status = Enums.OAPPTemplateUploadStatus.NotStarted });
                    break;

                case Google.Apis.Upload.UploadStatus.Starting:
                    _progress = 0;
                    OnOAPPTemplateUploadStatusChanged?.Invoke(this, new OAPPTemplateUploadProgressEventArgs() { Progress = _progress, Status = Enums.OAPPTemplateUploadStatus.Uploading });
                    break;

                case Google.Apis.Upload.UploadStatus.Completed:
                    _progress = 100;
                    OnOAPPTemplateUploadStatusChanged?.Invoke(this, new OAPPTemplateUploadProgressEventArgs() { Progress = _progress, Status = Enums.OAPPTemplateUploadStatus.Uploaded });
                    break;

                case Google.Apis.Upload.UploadStatus.Uploading:
                    _progress = Convert.ToInt32(((double)progress.BytesSent / (double)_fileLength) * 100);
                    OnOAPPTemplateUploadStatusChanged?.Invoke(this, new OAPPTemplateUploadProgressEventArgs() { Progress = _progress, Status = Enums.OAPPTemplateUploadStatus.Uploading });
                    break;

                case Google.Apis.Upload.UploadStatus.Failed:
                    OnOAPPTemplateUploadStatusChanged?.Invoke(this, new OAPPTemplateUploadProgressEventArgs() { Progress = _progress, Status = Enums.OAPPTemplateUploadStatus.Error, ErrorMessage = progress.Exception.ToString() });
                    break;
            }
        }

        private void OnDownloadProgress(Google.Apis.Download.IDownloadProgress progress)
        {
            switch (progress.Status)
            {
                case Google.Apis.Download.DownloadStatus.NotStarted:
                    _progress = 0;
                    OnOAPPTemplateDownloadStatusChanged?.Invoke(this, new OAPPTemplateDownloadProgressEventArgs() { Progress = _progress, Status = Enums.OAPPTemplateDownloadStatus.NotStarted });
                    break;

                case Google.Apis.Download.DownloadStatus.Completed:
                    _progress = 100;
                    OnOAPPTemplateDownloadStatusChanged?.Invoke(this, new OAPPTemplateDownloadProgressEventArgs() { Progress = _progress, Status = Enums.OAPPTemplateDownloadStatus.Downloaded });
                    break;

                case Google.Apis.Download.DownloadStatus.Downloading:
                    _progress = Convert.ToInt32((progress.BytesDownloaded / _fileLength) * 100);
                    OnOAPPTemplateDownloadStatusChanged?.Invoke(this, new OAPPTemplateDownloadProgressEventArgs() { Progress = _progress, Status = Enums.OAPPTemplateDownloadStatus.Downloading });
                    break;

                case Google.Apis.Download.DownloadStatus.Failed:
                    OnOAPPTemplateDownloadStatusChanged?.Invoke(this, new OAPPTemplateDownloadProgressEventArgs() { Progress = _progress, Status = Enums.OAPPTemplateDownloadStatus.Error, ErrorMessage = progress.Exception.ToString() });
                    break;
            }
        }
    }
}