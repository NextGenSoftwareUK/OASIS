using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Text.Json;
using System.Diagnostics;
using System.IO.Compression;
using System.Threading.Tasks;
using Google.Cloud.Storage.V1;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using NextGenSoftware.CLI.Engine;
using NextGenSoftware.Utilities;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.ONode.Core.Interfaces;
using NextGenSoftware.OASIS.API.ONode.Core.Holons;
using NextGenSoftware.OASIS.API.ONODE.Core.Events;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.ONode.Core.Interfaces.Holons;

namespace NextGenSoftware.OASIS.API.ONode.Core.Managers
{
    public class OAPPTemplateManager : OAPPSystemManagerBase<OAPPTemplate, DownloadedOAPPTemplate, InstalledOAPPTemplate>
    {
        private int _progress = 0;
        private long _fileLength = 0;
        private const string GOOGLE_CLOUD_BUCKET_NAME = "oasis_oapptemplates";

        public OAPPTemplateManager(Guid avatarId, OASISDNA OASISDNA = null) : base(avatarId, OASISDNA, HolonType.OAPPTemplate, HolonType.InstalledOAPPTemplate, "OAPP Template", "OAPPTemplateId", "OAPPTemplateName", "OAPPTemplateType", "oapptemplate", "oasis_oapptemplates", "OAPPTemplateDNA.json" ) 
        {
            //Init();
        }
        public OAPPTemplateManager(IOASISStorageProvider OASISStorageProvider, Guid avatarId, OASISDNA OASISDNA = null) : base(OASISStorageProvider, avatarId, OASISDNA, HolonType.OAPPTemplate, HolonType.InstalledOAPPTemplate, "OAPP Template", "OAPPTemplateId", "OAPPTemplateName", "OAPPTemplateType", "oapptemplate", "oasis_oapptemplates", "OAPPTemplateDNA.json") 
        {
            //Init();
        }

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

        //public void Init()
        //{
        //    base.OAPPSystemHolonUIName = "OAPP Template";
        //    base.OAPPSystemHolonIdName = "OAPPTemplateId";
        //    base.OAPPSystemHolonNameName = "OAPPTemplateName";
        //    base.OAPPSystemHolonTypeName = "OAPPTemplateType";
        //    base.OAPPSystemHolonFileExtention = "oapptemplate";
        //    base.OAPPSystemHolonGoogleBucket = "oasis_oapptemplates";
        //    base.OAPPSystemHolonDNAFileName = "OAPPTemplateDNA.json";
        //    base.OAPPSystemHolonType = HolonType.OAPPTemplate;
        //    base.OAPPSystemInstalledHolonType = HolonType.InstalledOAPPTemplate;
        //}

        public async Task<OASISResult<IOAPPTemplate>> CreateOAPPTemplateAsync(string name, string description, OAPPTemplateType OAPPTemplateType, Guid avatarId, string fullPathToOAPPTemplate, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.CreateAsync(name, description, OAPPTemplateType, avatarId, fullPathToOAPPTemplate, providerType), new OASISResult<IOAPPTemplate>());
        }

        public OASISResult<IOAPPTemplate> CreateOAPPTemplate(string name, string description, OAPPTemplateType OAPPTemplateType, Guid avatarId, string fullPathToOAPPTemplate, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.Create(name, description, OAPPTemplateType, avatarId, fullPathToOAPPTemplate, providerType), new OASISResult<IOAPPTemplate>());
        }

        #region COSMICManagerBase
        public async Task<OASISResult<IOAPPTemplate>> SaveOAPPTemplateAsync(IOAPPTemplate oappTemplate, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.SaveAsync((OAPPTemplate)oappTemplate, avatarId, providerType), new OASISResult<IOAPPTemplate>());
        }

        public OASISResult<IOAPPTemplate> SaveOAPPTemplate(IOAPPTemplate oappTemplate, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.Save((OAPPTemplate)oappTemplate, avatarId, providerType), new OASISResult<IOAPPTemplate>());
        }

        public async Task<OASISResult<IOAPPTemplate>> LoadOAPPTemplateAsync(Guid OAPPTemplateId, Guid avatarId, int version = 0, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.LoadAsync(OAPPTemplateId, avatarId, version, providerType), new OASISResult<IOAPPTemplate>());
        }

        public OASISResult<IOAPPTemplate> LoadOAPPTemplate(Guid OAPPTemplateId, Guid avatarId, int version = 0, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.Load(OAPPTemplateId, avatarId, version, providerType), new OASISResult<IOAPPTemplate>());
        }

        public async Task<OASISResult<IEnumerable<IOAPPTemplate>>> LoadAllOAPPTemplatesAsync(Guid avatarId, OAPPTemplateType OAPPTemplateType = OAPPTemplateType.All, bool showAllVersions = false, int version = 0, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResults(await base.LoadAllAsync(avatarId, OAPPTemplateType, OAPPTemplateType == OAPPTemplateType.All, showAllVersions, version, providerType), new OASISResult<IEnumerable<IOAPPTemplate>>());
        }

        public OASISResult<IEnumerable<IOAPPTemplate>> LoadAllOAPPTemplates(Guid avatarId, OAPPTemplateType OAPPTemplateType = OAPPTemplateType.All, bool showAllVersions = false, int version = 0, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResults(base.LoadAll(avatarId, OAPPTemplateType, OAPPTemplateType == OAPPTemplateType.All, showAllVersions, version, providerType), new OASISResult<IEnumerable<IOAPPTemplate>>());
        }

        public async Task<OASISResult<IEnumerable<IOAPPTemplate>>> LoadAllOAPPTemplatesForAvatarAsync(Guid avatarId, bool showAllVersions = false, int version = 0, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResults(await base.LoadAllForAvatarAsync(avatarId, showAllVersions, version, providerType), new OASISResult<IEnumerable<IOAPPTemplate>>());
        }

        public OASISResult<IEnumerable<IOAPPTemplate>> LoadAllOAPPTemplatesForAvatar(Guid avatarId, bool showAllVersions = false, int version = 0, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResults(base.LoadAllForAvatar(avatarId, showAllVersions, version, providerType), new OASISResult<IEnumerable<IOAPPTemplate>>());
        }

        public async Task<OASISResult<IEnumerable<IOAPPTemplate>>> SearchOAPPTemplatesAsync(string searchTerm, Guid avatarId, bool searchOnlyForCurrentAvatar = true, bool showAllVersions = false, int version = 0, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResults(await base.SearchAsync(searchTerm, avatarId, HolonType.OAPPTemplate, searchOnlyForCurrentAvatar, showAllVersions, version, providerType), new OASISResult<IEnumerable<IOAPPTemplate>>());
        }

        public OASISResult<IEnumerable<IOAPPTemplate>> SearchOAPPTemplates(string searchTerm, Guid avatarId, bool searchOnlyForCurrentAvatar = true, bool showAllVersions = false, int version = 0, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResults(base.Search(searchTerm, avatarId, HolonType.OAPPTemplate, searchOnlyForCurrentAvatar, showAllVersions, version, providerType), new OASISResult<IEnumerable<IOAPPTemplate>>());
        }

        public async Task<OASISResult<IOAPPTemplate>> DeleteOAPPTemplateAsync(Guid oappTemplateId, Guid avatarId, int version, bool softDelete = true, bool deleteDownload = true, bool deleteInstall = true, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.DeleteAsync(oappTemplateId, avatarId, version, softDelete, deleteDownload, deleteInstall, providerType), new OASISResult<IOAPPTemplate>());
        }

        public OASISResult<IOAPPTemplate> DeleteOAPPTemplate(Guid oappTemplateId, Guid avatarId, int version, bool softDelete = true, bool deleteDownload = true, bool deleteInstall = true, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.Delete(oappTemplateId, avatarId, version, softDelete, deleteDownload, deleteInstall, providerType), new OASISResult<IOAPPTemplate>());
        }

        public async Task<OASISResult<IOAPPTemplate>> DeleteOAPPTemplateAsync(Guid avatarId, IOAPPTemplate oappTemplate, int version, bool softDelete = true, bool deleteDownload = true, bool deleteInstall = true, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.DeleteAsync(oappTemplate, avatarId, version, softDelete, deleteDownload, deleteInstall, providerType), new OASISResult<IOAPPTemplate>());
        }

        public OASISResult<IOAPPTemplate> DeleteOAPPTemplate(Guid avatarId, IOAPPTemplate oappTemplate, int version, bool softDelete = true, bool deleteDownload = true, bool deleteInstall = true, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.Delete(oappTemplate, avatarId, version, softDelete, deleteDownload, deleteInstall, providerType), new OASISResult<IOAPPTemplate>());
        }
        #endregion

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

        public async Task<OASISResult<IEnumerable<IOAPPTemplate>>> LoadOAPPTemplateVersionsAsync(Guid OAPPTemplateId, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResults(await base.LoadVersionsAsync(OAPPTemplateId, providerType), new OASISResult<IEnumerable<IOAPPTemplate>>());
        }

        public OASISResult<IEnumerable<IOAPPTemplate>> LoadOAPPTemplateVersions(Guid OAPPTemplateId, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResults(base.LoadVersions(OAPPTemplateId, providerType), new OASISResult<IEnumerable<IOAPPTemplate>>());
        }

        public async Task<OASISResult<IOAPPTemplate>> LoadOAPPTemplateVersionAsync(Guid OAPPTemplateId, string version, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.LoadVersionAsync(OAPPTemplateId, version, providerType), new OASISResult<IOAPPTemplate>());
        }

        public OASISResult<IOAPPTemplate> LoadOAPPTemplateVersion(Guid OAPPTemplateId, string version, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.LoadVersion(OAPPTemplateId, version, providerType), new OASISResult<IOAPPTemplate>());
        }

        //public async Task<OASISResult<IOAPPTemplate>> EditOAPPTemplateAsync(Guid OAPPTemplateId, IOAPPTemplateDNA newOAPPTemplateDNA, Guid avatarId, ProviderType providerType = ProviderType.Default)
        //{
        //    return ProcessResult(await base.EditAsync<OAPPTemplate, InstalledOAPPSystemHolon>(OAPPTemplateId, newOAPPTemplateDNA, avatarId, providerType), new OASISResult<IOAPPTemplate>());
        //}

        public async Task<OASISResult<IOAPPTemplate>> EditOAPPTemplateAsync(Guid OAPPTemplateId, IOAPPTemplateDNA newOAPPTemplateDNA, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.EditAsync(OAPPTemplateId, newOAPPTemplateDNA, avatarId, providerType), new OASISResult<IOAPPTemplate>());
        }

        public async Task<OASISResult<IOAPPTemplate>> EditOAPPTemplateAsync(IOAPPTemplate OAPPTemplate, IOAPPTemplateDNA newOAPPTemplateDNA, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.EditAsync((OAPPTemplate)OAPPTemplate, newOAPPTemplateDNA, avatarId, providerType), new OASISResult<IOAPPTemplate>());
        }

        public async Task<OASISResult<IOAPPTemplate>> PublishOAPPTemplateAsync(Guid avatarId, string fullPathToOAPPTemplate, string launchTarget, string fullPathToPublishTo = "", bool registerOnSTARNET = true, bool generateOAPPTemplateBinary = true, bool uploadOAPPTemplateToCloud = false, bool edit = false, ProviderType providerType = ProviderType.Default, ProviderType oappBinaryProviderType = ProviderType.IPFSOASIS)
        {
            return ProcessResult(await base.PublishAsync(avatarId, fullPathToOAPPTemplate, launchTarget, fullPathToPublishTo, registerOnSTARNET, generateOAPPTemplateBinary, uploadOAPPTemplateToCloud, edit, providerType), new OASISResult<IOAPPTemplate>());
        }

        public async Task<OASISResult<IOAPPTemplate>> PublishOAPPTemplate(Guid avatarId, string fullPathToOAPPTemplate, string launchTarget, string fullPathToPublishTo = "", bool registerOnSTARNET = true, bool generateOAPPTemplateBinary = true, bool uploadOAPPTemplateToCloud = false, bool edit = false, ProviderType providerType = ProviderType.Default, ProviderType oappBinaryProviderType = ProviderType.IPFSOASIS)
        {
            return ProcessResult(base.Publish(avatarId, fullPathToOAPPTemplate, launchTarget, fullPathToPublishTo, registerOnSTARNET, generateOAPPTemplateBinary, uploadOAPPTemplateToCloud, edit, providerType), new OASISResult<IOAPPTemplate>());
        }


        public async Task<OASISResult<IOAPPTemplate>> UnpublishOAPPTemplateAsync(IOAPPTemplate OAPPTemplate, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IOAPPTemplate> result = new OASISResult<IOAPPTemplate>();
            string errorMessage = "Error occured in UnpublishOAPPTemplateAsync. Reason: ";

            OAPPTemplate.OAPPTemplateDNA.PublishedOn = DateTime.MinValue;
            OAPPTemplate.OAPPTemplateDNA.PublishedByAvatarId = Guid.Empty;
            OAPPTemplate.OAPPTemplateDNA.PublishedByAvatarUsername = "";
            //OAPPTemplate.OAPPTemplateDNA.IsActive = false;
            OAPPTemplate.MetaData["Active"] = "0";

            OASISResult<IOAPPTemplate> oappResult = await SaveOAPPTemplateAsync(OAPPTemplate, avatarId, providerType);

            if (oappResult != null && oappResult.Result != null && !oappResult.IsError)
            { 
                result.Result = oappResult.Result; //ConvertOAPPTemplateToOAPPTemplateDNA(OAPPTemplate);
                result.Message = "OAPPTemplate Unpublished";
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured saving the OAPP Template with the SaveOAPPTemplateAsync method, reason: {oappResult.Message}");

            return result;
        }

        public OASISResult<IOAPPTemplate> UnpublishOAPPTemplate(IOAPPTemplate OAPPTemplate, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IOAPPTemplate> result = new OASISResult<IOAPPTemplate>();
            string errorMessage = "Error occured in UnpublishOAPPTemplate. Reason: ";

            OAPPTemplate.OAPPTemplateDNA.PublishedOn = DateTime.MinValue;
            OAPPTemplate.OAPPTemplateDNA.PublishedByAvatarId = Guid.Empty;
            OAPPTemplate.OAPPTemplateDNA.PublishedByAvatarUsername = "";
            //OAPPTemplate.OAPPTemplateDNA.IsActive = false;
            OAPPTemplate.MetaData["Active"] = "0";

            OASISResult<IOAPPTemplate> oappResult = SaveOAPPTemplate(OAPPTemplate, avatarId, providerType);

            if (oappResult != null && oappResult.Result != null && !oappResult.IsError)
            {
                result.Result = oappResult.Result; //ConvertOAPPTemplateToOAPPTemplateDNA(OAPPTemplate);
                result.Message = "OAPP Template Unpublished";
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured saving the OAPP Template with the SaveOAPPTemplate method, reason: {oappResult.Message}");

            return result;
        }

        public async Task<OASISResult<IOAPPTemplate>> UnpublishOAPPTemplateAsync(Guid OAPPTemplateId, int version, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IOAPPTemplate> result = new OASISResult<IOAPPTemplate>();
            OASISResult<IOAPPTemplate> loadResult = await LoadOAPPTemplateAsync(OAPPTemplateId, avatarId, version, providerType);

            if (loadResult != null && loadResult.Result != null && !loadResult.IsError)
                result = await UnpublishOAPPTemplateAsync(loadResult.Result, avatarId, providerType);
            else
                OASISErrorHandling.HandleError(ref result, $"Error occured in UnpublishOAPPTemplateAsync loading the OAPPTemplate with the LoadOAPPTemplateAsync method, reason: {loadResult.Message}");

            return result;
        }

        public OASISResult<IOAPPTemplate> UnpublishOAPPTemplate(Guid OAPPTemplateId, int version, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IOAPPTemplate> result = new OASISResult<IOAPPTemplate>();
            OASISResult<IOAPPTemplate> loadResult = LoadOAPPTemplate(OAPPTemplateId, avatarId, version, providerType);

            if (loadResult != null && loadResult.Result != null && !loadResult.IsError)
                result = UnpublishOAPPTemplate(loadResult.Result, avatarId, providerType);
            else
                OASISErrorHandling.HandleError(ref result, $"Error occured in UnpublishOAPPTemplate loading the OAPPTemplate with the LoadOAPPTemplate method, reason: {loadResult.Message}");

            return result;
        }

        public async Task<OASISResult<IOAPPTemplate>> UnpublishOAPPTemplateAsync(IOAPPTemplateDNA OAPPTemplateDNA, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IOAPPTemplate> result = new OASISResult<IOAPPTemplate>();
            OASISResult<IOAPPTemplate> oappResult = await LoadOAPPTemplateAsync(OAPPTemplateDNA.Id, avatarId, OAPPTemplateDNA.VersionSequence, providerType);
            string errorMessage = "Error occured in UnpublishOAPPTemplateAsync. Reason: ";

            if (oappResult != null && oappResult.Result != null && !oappResult.IsError)
                result = await UnpublishOAPPTemplateAsync(oappResult.Result, avatarId, providerType);
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured loading the OAPP Template with the LoadOAPPTemplateAsync method, reason: {oappResult.Message}");

            return result;
        }

        public OASISResult<IOAPPTemplate> UnpublishOAPPTemplate(IOAPPTemplateDNA OAPPTemplateDNA, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IOAPPTemplate> result = new OASISResult<IOAPPTemplate>();
            OASISResult<IOAPPTemplate> oappResult = LoadOAPPTemplate(OAPPTemplateDNA.Id, avatarId, OAPPTemplateDNA.VersionSequence, providerType);
            string errorMessage = "Error occured in UnpublishOAPPTemplate. Reason: ";

            if (oappResult != null && oappResult.Result != null && !oappResult.IsError)
                result = UnpublishOAPPTemplate(oappResult.Result, avatarId, providerType);
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured loading the OAPP Template with the LoadOAPPTemplate method, reason: {oappResult.Message}");

            return result;
        }

        public async Task<OASISResult<IOAPPTemplate>> RepublishOAPPTemplateAsync(IOAPPTemplate OAPPTemplate, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IOAPPTemplate> result = new OASISResult<IOAPPTemplate>();
            string errorMessage = "Error occured in RepublishOAPPTemplateAsync. Reason: ";

            OASISResult<IAvatar> avatarResult = await AvatarManager.Instance.LoadAvatarAsync(avatarId, false, true, providerType);

            if (avatarResult != null && avatarResult.Result != null && !avatarResult.IsError)
            {
                OAPPTemplate.OAPPTemplateDNA.PublishedOn = DateTime.Now;
                OAPPTemplate.OAPPTemplateDNA.PublishedByAvatarId = avatarId;
                OAPPTemplate.OAPPTemplateDNA.PublishedByAvatarUsername = avatarResult.Result.Username;
                //OAPPTemplate.OAPPTemplateDNA.IsActive = true;
                OAPPTemplate.MetaData["Active"] = "1";

                OASISResult<IOAPPTemplate> oappResult = await SaveOAPPTemplateAsync(OAPPTemplate, avatarId, providerType);

                if (oappResult != null && oappResult.Result != null && !oappResult.IsError)
                {
                    result.Result = oappResult.Result; //ConvertOAPPTemplateToOAPPTemplateDNA(OAPPTemplate);
                    result.Message = "OAPPTemplate Republished";
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured saving the OAPP Template with the SaveOAPPTemplateAsync method, reason: {oappResult.Message}");
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured loading the Avatar with the LoadAvatar method, reason: {avatarResult.Message}");

            return result;
        }

        public OASISResult<IOAPPTemplate> RepublishOAPPTemplate(IOAPPTemplate OAPPTemplate, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IOAPPTemplate> result = new OASISResult<IOAPPTemplate>();
            string errorMessage = "Error occured in RepublishOAPPTemplate. Reason: ";

            OASISResult<IAvatar> avatarResult = AvatarManager.Instance.LoadAvatar(avatarId, false, true, providerType);

            if (avatarResult != null && avatarResult.Result != null && !avatarResult.IsError)
            {
                OAPPTemplate.OAPPTemplateDNA.PublishedOn = DateTime.Now;
                OAPPTemplate.OAPPTemplateDNA.PublishedByAvatarId = avatarId;
                OAPPTemplate.OAPPTemplateDNA.PublishedByAvatarUsername = avatarResult.Result.Username;
                //OAPPTemplate.OAPPTemplateDNA.IsActive = true;
                OAPPTemplate.MetaData["Active"] = "1";

                OASISResult<IOAPPTemplate> oappResult = SaveOAPPTemplate(OAPPTemplate, avatarId, providerType);

                if (oappResult != null && oappResult.Result != null && !oappResult.IsError)
                {
                    result.Result = oappResult.Result; //ConvertOAPPTemplateToOAPPTemplateDNA(OAPPTemplate);
                    result.Message = "OAPP Template Republished";
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured saving the OAPP Template with the SaveOAPPTemplate method, reason: {oappResult.Message}");
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured loading the Avatar with the LoadAvatar method, reason: {avatarResult.Message}");

            return result;
        }

        public async Task<OASISResult<IOAPPTemplate>> RepublishOAPPTemplateAsync(IOAPPTemplateDNA OAPPTemplateDNA, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IOAPPTemplate> result = new OASISResult<IOAPPTemplate>();
            OASISResult<IOAPPTemplate> oappResult = await LoadOAPPTemplateAsync(OAPPTemplateDNA.Id, avatarId, OAPPTemplateDNA.VersionSequence, providerType);
            string errorMessage = "Error occured in RepublishOAPPTemplateAsync. Reason: ";

            if (oappResult != null && oappResult.Result != null && !oappResult.IsError)
                result = await RepublishOAPPTemplateAsync(oappResult.Result, avatarId, providerType);
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured loading the OAPP Template with the LoadOAPPTemplateAsync method, reason: {oappResult.Message}");

            return result;
        }

        public OASISResult<IOAPPTemplate> RepublishOAPPTemplate(IOAPPTemplateDNA OAPPTemplateDNA, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IOAPPTemplate> result = new OASISResult<IOAPPTemplate>();
            OASISResult<IOAPPTemplate> oappResult = LoadOAPPTemplate(OAPPTemplateDNA.Id, avatarId, OAPPTemplateDNA.VersionSequence, providerType);
            string errorMessage = "Error occured in RepublishOAPPTemplate. Reason: ";

            if (oappResult != null && oappResult.Result != null && !oappResult.IsError)
                result = RepublishOAPPTemplate(oappResult.Result, avatarId, providerType);
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured loading the OAPP Template with the LoadOAPPTemplate method, reason: {oappResult.Message}");

            return result;
        }

        public async Task<OASISResult<IOAPPTemplate>> RepublishOAPPTemplateAsync(Guid OAPPTemplateId, int version, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IOAPPTemplate> result = new OASISResult<IOAPPTemplate>();
            OASISResult<IOAPPTemplate> loadResult = await LoadOAPPTemplateAsync(OAPPTemplateId, avatarId, version, providerType);

            if (loadResult != null && loadResult.Result != null && !loadResult.IsError)
                result = await RepublishOAPPTemplateAsync(loadResult.Result, avatarId, providerType);
            else
                OASISErrorHandling.HandleError(ref result, $"Error occured in RepublishOAPPTemplateAsync loading the OAPPTemplate with the LoadOAPPTemplateAsync method, reason: {loadResult.Message}");

            return result;
        }

        public OASISResult<IOAPPTemplate> RepublishOAPPTemplate(Guid OAPPTemplateId, int version, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IOAPPTemplate> result = new OASISResult<IOAPPTemplate>();
            OASISResult<IOAPPTemplate> loadResult = LoadOAPPTemplate(OAPPTemplateId, avatarId, version, providerType);

            if (loadResult != null && loadResult.Result != null && !loadResult.IsError)
                result = RepublishOAPPTemplate(loadResult.Result, avatarId, providerType);
            else
                OASISErrorHandling.HandleError(ref result, $"Error occured in RepublishOAPPTemplate loading the OAPPTemplate with the LoadOAPPTemplate method, reason: {loadResult.Message}");

            return result;
        }

        public async Task<OASISResult<IOAPPTemplate>> DeactivateOAPPTemplateAsync(IOAPPTemplate OAPPTemplate, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IOAPPTemplate> result = new OASISResult<IOAPPTemplate>();
            string errorMessage = "Error occured in DeactivateOAPPTemplateAsync. Reason: ";

            //OAPPTemplate.OAPPTemplateDNA.IsActive = false;
            OAPPTemplate.MetaData["Active"] = "0";

            OASISResult<IOAPPTemplate> oappResult = await SaveOAPPTemplateAsync(OAPPTemplate, avatarId, providerType);

            if (oappResult != null && oappResult.Result != null && !oappResult.IsError)
            {
                result.Result = oappResult.Result; //ConvertOAPPTemplateToOAPPTemplateDNA(OAPPTemplate);
                result.Message = "OAPPTemplate Deactivateed";
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured saving the OAPP Template with the SaveOAPPTemplateAsync method, reason: {oappResult.Message}");

            return result;
        }

        public OASISResult<IOAPPTemplate> DeactivateOAPPTemplate(IOAPPTemplate OAPPTemplate, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IOAPPTemplate> result = new OASISResult<IOAPPTemplate>();
            string errorMessage = "Error occured in DeactivateOAPPTemplate. Reason: ";

            //OAPPTemplate.OAPPTemplateDNA.IsActive = false;
            OAPPTemplate.MetaData["Active"] = "0";

            OASISResult<IOAPPTemplate> oappResult = SaveOAPPTemplate(OAPPTemplate, avatarId, providerType);

            if (oappResult != null && oappResult.Result != null && !oappResult.IsError)
            {
                result.Result = oappResult.Result; //ConvertOAPPTemplateToOAPPTemplateDNA(OAPPTemplate);
                result.Message = "OAPP Template Deactivateed";
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured saving the OAPP Template with the SaveOAPPTemplate method, reason: {oappResult.Message}");

            return result;
        }

        public async Task<OASISResult<IOAPPTemplate>> DeactivateOAPPTemplateAsync(Guid OAPPTemplateId, int version, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IOAPPTemplate> result = new OASISResult<IOAPPTemplate>();
            OASISResult<IOAPPTemplate> loadResult = await LoadOAPPTemplateAsync(OAPPTemplateId, avatarId, version, providerType);

            if (loadResult != null && loadResult.Result != null && !loadResult.IsError)
                result = await DeactivateOAPPTemplateAsync(loadResult.Result, avatarId, providerType);
            else
                OASISErrorHandling.HandleError(ref result, $"Error occured in DeactivateOAPPTemplateAsync loading the OAPPTemplate with the LoadOAPPTemplateAsync method, reason: {loadResult.Message}");

            return result;
        }

        public OASISResult<IOAPPTemplate> DeactivateOAPPTemplate(Guid OAPPTemplateId, int version, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IOAPPTemplate> result = new OASISResult<IOAPPTemplate>();
            OASISResult<IOAPPTemplate> loadResult = LoadOAPPTemplate(OAPPTemplateId, avatarId, version, providerType);

            if (loadResult != null && loadResult.Result != null && !loadResult.IsError)
                result = DeactivateOAPPTemplate(loadResult.Result, avatarId, providerType);
            else
                OASISErrorHandling.HandleError(ref result, $"Error occured in DeactivateOAPPTemplate loading the OAPPTemplate with the LoadOAPPTemplate method, reason: {loadResult.Message}");

            return result;
        }

        public async Task<OASISResult<IOAPPTemplate>> DeactivateOAPPTemplateAsync(IOAPPTemplateDNA OAPPTemplateDNA, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IOAPPTemplate> result = new OASISResult<IOAPPTemplate>();
            OASISResult<IOAPPTemplate> oappResult = await LoadOAPPTemplateAsync(OAPPTemplateDNA.Id, avatarId, OAPPTemplateDNA.VersionSequence, providerType);
            string errorMessage = "Error occured in DeactivateOAPPTemplateAsync. Reason: ";

            if (oappResult != null && oappResult.Result != null && !oappResult.IsError)
                result = await DeactivateOAPPTemplateAsync(oappResult.Result, avatarId, providerType);
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured loading the OAPP Template with the LoadOAPPTemplateAsync method, reason: {oappResult.Message}");

            return result;
        }

        public OASISResult<IOAPPTemplate> DeactivateOAPPTemplate(IOAPPTemplateDNA OAPPTemplateDNA, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IOAPPTemplate> result = new OASISResult<IOAPPTemplate>();
            OASISResult<IOAPPTemplate> oappResult = LoadOAPPTemplate(OAPPTemplateDNA.Id, avatarId, OAPPTemplateDNA.VersionSequence, providerType);
            string errorMessage = "Error occured in DeactivateOAPPTemplate. Reason: ";

            if (oappResult != null && oappResult.Result != null && !oappResult.IsError)
                result = DeactivateOAPPTemplate(oappResult.Result, avatarId, providerType);
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured loading the OAPP Template with the LoadOAPPTemplate method, reason: {oappResult.Message}");

            return result;
        }

        public async Task<OASISResult<IOAPPTemplate>> ActivateOAPPTemplateAsync(IOAPPTemplate OAPPTemplate, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IOAPPTemplate> result = new OASISResult<IOAPPTemplate>();
            string errorMessage = "Error occured in ActivateOAPPTemplateAsync. Reason: ";

            OASISResult<IAvatar> avatarResult = await AvatarManager.Instance.LoadAvatarAsync(avatarId, false, true, providerType);

            if (avatarResult != null && avatarResult.Result != null && !avatarResult.IsError)
            {
                //OAPPTemplate.OAPPTemplateDNA.IsActive = true;
                OAPPTemplate.MetaData["Active"] = "1";

                OASISResult<IOAPPTemplate> oappResult = await SaveOAPPTemplateAsync(OAPPTemplate, avatarId, providerType);

                if (oappResult != null && oappResult.Result != null && !oappResult.IsError)
                {
                    result.Result = oappResult.Result; //ConvertOAPPTemplateToOAPPTemplateDNA(OAPPTemplate);
                    result.Message = "OAPPTemplate Activateed";
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured saving the OAPP Template with the SaveOAPPTemplateAsync method, reason: {oappResult.Message}");
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured loading the Avatar with the LoadAvatar method, reason: {avatarResult.Message}");

            return result;
        }

        public OASISResult<IOAPPTemplate> ActivateOAPPTemplate(IOAPPTemplate OAPPTemplate, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IOAPPTemplate> result = new OASISResult<IOAPPTemplate>();
            string errorMessage = "Error occured in ActivateOAPPTemplate. Reason: ";

            OASISResult<IAvatar> avatarResult = AvatarManager.Instance.LoadAvatar(avatarId, false, true, providerType);

            if (avatarResult != null && avatarResult.Result != null && !avatarResult.IsError)
            {
                //OAPPTemplate.OAPPTemplateDNA.IsActive = true;
                OAPPTemplate.MetaData["Active"] = "1";

                OASISResult<IOAPPTemplate> oappResult = SaveOAPPTemplate(OAPPTemplate, avatarId, providerType);

                if (oappResult != null && oappResult.Result != null && !oappResult.IsError)
                {
                    result.Result = oappResult.Result; //ConvertOAPPTemplateToOAPPTemplateDNA(OAPPTemplate);
                    result.Message = "OAPP Template Activateed";
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured saving the OAPP Template with the SaveOAPPTemplate method, reason: {oappResult.Message}");
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured loading the Avatar with the LoadAvatar method, reason: {avatarResult.Message}");

            return result;
        }

        public async Task<OASISResult<IOAPPTemplate>> ActivateOAPPTemplateAsync(IOAPPTemplateDNA OAPPTemplateDNA, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IOAPPTemplate> result = new OASISResult<IOAPPTemplate>();
            OASISResult<IOAPPTemplate> oappResult = await LoadOAPPTemplateAsync(OAPPTemplateDNA.Id, avatarId, OAPPTemplateDNA.VersionSequence, providerType);
            string errorMessage = "Error occured in ActivateOAPPTemplateAsync. Reason: ";

            if (oappResult != null && oappResult.Result != null && !oappResult.IsError)
                result = await ActivateOAPPTemplateAsync(oappResult.Result, avatarId, providerType);
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured loading the OAPP Template with the LoadOAPPTemplateAsync method, reason: {oappResult.Message}");

            return result;
        }

        public OASISResult<IOAPPTemplate> ActivateOAPPTemplate(IOAPPTemplateDNA OAPPTemplateDNA, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IOAPPTemplate> result = new OASISResult<IOAPPTemplate>();
            OASISResult<IOAPPTemplate> oappResult = LoadOAPPTemplate(OAPPTemplateDNA.Id, avatarId, OAPPTemplateDNA.VersionSequence, providerType);
            string errorMessage = "Error occured in ActivateOAPPTemplate. Reason: ";

            if (oappResult != null && oappResult.Result != null && !oappResult.IsError)
                result = ActivateOAPPTemplate(oappResult.Result, avatarId, providerType);
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured loading the OAPP Template with the LoadOAPPTemplate method, reason: {oappResult.Message}");

            return result;
        }

        public async Task<OASISResult<IOAPPTemplate>> ActivateOAPPTemplateAsync(Guid OAPPTemplateId, int version, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IOAPPTemplate> result = new OASISResult<IOAPPTemplate>();
            OASISResult<IOAPPTemplate> loadResult = await LoadOAPPTemplateAsync(OAPPTemplateId, avatarId, version, providerType);

            if (loadResult != null && loadResult.Result != null && !loadResult.IsError)
                result = await ActivateOAPPTemplateAsync(loadResult.Result, avatarId, providerType);
            else
                OASISErrorHandling.HandleError(ref result, $"Error occured in ActivateOAPPTemplateAsync loading the OAPPTemplate with the LoadOAPPTemplateAsync method, reason: {loadResult.Message}");

            return result;
        }

        public OASISResult<IOAPPTemplate> ActivateOAPPTemplate(Guid OAPPTemplateId, int version, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IOAPPTemplate> result = new OASISResult<IOAPPTemplate>();
            OASISResult<IOAPPTemplate> loadResult = LoadOAPPTemplate(OAPPTemplateId, avatarId, version, providerType);

            if (loadResult != null && loadResult.Result != null && !loadResult.IsError)
                result = ActivateOAPPTemplate(loadResult.Result, avatarId, providerType);
            else
                OASISErrorHandling.HandleError(ref result, $"Error occured in ActivateOAPPTemplate loading the OAPPTemplate with the LoadOAPPTemplate method, reason: {loadResult.Message}");

            return result;
        }

        public async Task<OASISResult<IDownloadedOAPPTemplate>> DownloadOAPPTemplateAsync(Guid avatarId, IOAPPTemplate OAPPTemplate, string fullDownloadPath, bool reInstall = false, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IDownloadedOAPPTemplate> result = new OASISResult<IDownloadedOAPPTemplate>();
            string errorMessage = "Error occured in OAPPTemplateManager.DownloadOAPPTemplateAsync. Reason: ";
            DownloadedOAPPTemplate downloadedOAPPTemplate = null;

            try
            {
                if (!fullDownloadPath.Contains(".oapptemplate"))
                    fullDownloadPath = Path.Combine(fullDownloadPath, string.Concat(OAPPTemplate.Name, "_v", OAPPTemplate.OAPPTemplateDNA.Version, ".oapptemplate"));

                if (File.Exists(fullDownloadPath))
                    File.Delete(fullDownloadPath);

                try
                {
                    StorageClient storage = await StorageClient.CreateAsync();

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
                            _fileLength = OAPPTemplate.OAPPTemplateDNA.OAPPTemplateFileSize;

                        _progress = 0;

                        string publishedOAPPTemplateFileName = string.Concat(OAPPTemplate.Name, "_v", OAPPTemplate.OAPPTemplateDNA.Version, ".oapptemplate");
                        OnOAPPTemplateInstallStatusChanged?.Invoke(this, new OAPPTemplateInstallStatusEventArgs() { OAPPTemplateDNA = OAPPTemplate.OAPPTemplateDNA, Status = Enums.OAPPTemplateInstallStatus.Downloading });
                        await storage.DownloadObjectAsync(GOOGLE_CLOUD_BUCKET_NAME, publishedOAPPTemplateFileName, fileStream, downloadObjectOptions, progress: progressReporter);

                        _progress = 100;
                        OnOAPPTemplateDownloadStatusChanged?.Invoke(this, new OAPPTemplateDownloadProgressEventArgs() { Progress = _progress, Status = Enums.OAPPTemplateDownloadStatus.Downloading });
                        CLIEngine.DisposeProgressBar(false);
                        Console.WriteLine("");
                        fileStream.Close();
                    }

                    OASISResult<IAvatar> avatarResult = await AvatarManager.Instance.LoadAvatarAsync(avatarId, false, true, providerType);

                    if (avatarResult != null && !avatarResult.IsError && avatarResult.Result != null)
                    {
                        if (!reInstall)
                        {
                            OAPPTemplate.OAPPTemplateDNA.Downloads++;

                            downloadedOAPPTemplate = new DownloadedOAPPTemplate()
                            {
                                Name = string.Concat(OAPPTemplate.OAPPTemplateDNA.Name, " Downloaded Holon"),
                                Description = string.Concat(OAPPTemplate.OAPPTemplateDNA.Description, " Downloaded Holon"),
                                OAPPTemplateDNA = OAPPTemplate.OAPPTemplateDNA,
                                DownloadedBy = avatarId,
                                DownloadedByAvatarUsername = avatarResult.Result.Username,
                                DownloadedOn = DateTime.Now,
                                DownloadedPath = fullDownloadPath
                            };

                            await UpdateDownloadCountsAsync(downloadedOAPPTemplate, OAPPTemplate.OAPPTemplateDNA, avatarId, result, errorMessage, providerType);

                            downloadedOAPPTemplate.MetaData["OAPPTemplateId"] = OAPPTemplate.OAPPTemplateDNA.Id.ToString();
                            downloadedOAPPTemplate.MetaData["OAPPTemplateDNAJSON"] = JsonSerializer.Serialize(downloadedOAPPTemplate.OAPPTemplateDNA);
                            OASISResult<DownloadedOAPPTemplate> saveResult = await downloadedOAPPTemplate.SaveAsync<DownloadedOAPPTemplate>();

                            if (!(saveResult != null && saveResult.Result != null && !saveResult.IsError))
                                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling SaveAsync method on downloadedOAPPTemplate. Reason: {saveResult.Message}");
                        }
                        else
                        {
                            OASISResult<IEnumerable<DownloadedOAPPTemplate>> downloadedOAPPTemplateResult = await Data.LoadHolonsByMetaDataAsync<DownloadedOAPPTemplate>("OAPPTemplateId", OAPPTemplate.OAPPTemplateDNA.Id.ToString(), HolonType.All, true, true, 0, true, false, 0, HolonType.All, 0, providerType);

                            if (downloadedOAPPTemplateResult != null && !downloadedOAPPTemplateResult.IsError && downloadedOAPPTemplateResult.Result != null)
                            {
                                downloadedOAPPTemplate = downloadedOAPPTemplateResult.Result.FirstOrDefault();
                                downloadedOAPPTemplate.DownloadedOn = DateTime.Now;
                                downloadedOAPPTemplate.DownloadedBy = avatarId;
                                downloadedOAPPTemplate.DownloadedByAvatarUsername = avatarResult.Result.Username;
                                downloadedOAPPTemplate.DownloadedPath = fullDownloadPath;

                                OASISResult<DownloadedOAPPTemplate> saveResult = await downloadedOAPPTemplate.SaveAsync<DownloadedOAPPTemplate>();

                                if (!(saveResult != null && saveResult.Result != null && !saveResult.IsError))
                                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling SaveAsync method on downloadedOAPPTemplate. Reason: {saveResult.Message}");
                            }
                            else
                                OASISErrorHandling.HandleWarning(ref result, $"The OAPP Template was downloaded but the DownloadedOAPPTemplate could not be found. Reason: {downloadedOAPPTemplateResult.Message}");
                        }
                    }
                    else
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadAvatarAsync method. Reason: {avatarResult.Message}");


                    if (!result.IsError)
                    {
                        result.Result = downloadedOAPPTemplate;
                        OASISResult<IOAPPTemplate> oappSaveResult = await SaveOAPPTemplateAsync(OAPPTemplate, avatarId, providerType);

                        if (oappSaveResult != null && !oappSaveResult.IsError && oappSaveResult.Result != null)
                        {
                            if (result.InnerMessages.Count > 0)
                                result.Message = $"OAPP Template successfully downloaded but there were {result.WarningCount} warnings:\n\n {OASISResultHelper.BuildInnerMessageError(result.InnerMessages)}";
                            else
                                result.Message = "OAPP Template Successfully Downloaded";
                        }
                        else
                            OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling SaveOAPPTemplateAsync method. Reason: {oappSaveResult.Message}");
                    }
                }
                catch (Exception e)
                {
                    CLIEngine.DisposeProgressBar(false);
                    Console.WriteLine("");
                    OASISErrorHandling.HandleError(ref result, $"An error occured downloading the OAPP Template from cloud storage. Reason: {e}");
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
            //    OnOAPPTemplateDownloadStatusChanged?.Invoke(this, new OAPPTemplateDownloadProgressEventArgs { OAPPTemplateDNA = OAPPTemplate.OAPPTemplateDNA, Status = Enums.OAPPTemplateDownloadStatus.Error, ErrorMessage = result.Message });

            return result;
        }

        public async Task<OASISResult<IInstalledOAPPTemplate>> DownloadAndInstallOAPPTemplateAsync(Guid avatarId, IOAPPTemplate OAPPTemplate, string fullInstallPath, string fullDownloadPath = "", bool createOAPPTemplateDirectory = true, bool reInstall = false, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IInstalledOAPPTemplate> result = new OASISResult<IInstalledOAPPTemplate>();
            string errorMessage = "Error occured in OAPPTemplateManager.DownloadAndInstallOAPPTemplateAsync. Reason: ";
            bool isFullDownloadPathTemp = false;

            try
            {
                if (string.IsNullOrEmpty(fullDownloadPath))
                {
                    string tempPath = Path.GetTempPath();
                    fullDownloadPath = Path.Combine(tempPath, string.Concat(OAPPTemplate.Name, ".oapptemplate"));
                    isFullDownloadPathTemp = true;
                }

                if (File.Exists(fullDownloadPath))
                    File.Delete(fullDownloadPath);

                if (OAPPTemplate.PublishedOAPPTemplate != null)
                {
                    await File.WriteAllBytesAsync(fullDownloadPath, OAPPTemplate.PublishedOAPPTemplate);
                    result = await InstallOAPPTemplateAsync(avatarId, fullDownloadPath, fullInstallPath, createOAPPTemplateDirectory, null, reInstall, providerType);
                }
                else
                {
                    OASISResult<IDownloadedOAPPTemplate> downloadResult = await DownloadOAPPTemplateAsync(avatarId, OAPPTemplate, fullDownloadPath, reInstall, providerType);

                    if (!fullDownloadPath.Contains(".oapptemplate"))
                        fullDownloadPath = Path.Combine(fullDownloadPath, string.Concat(OAPPTemplate.Name, "_v", OAPPTemplate.OAPPTemplateDNA.Version, ".oapptemplate"));

                    if (downloadResult != null && downloadResult.Result != null && !downloadResult.IsError)
                        result = await InstallOAPPTemplateAsync(avatarId, fullDownloadPath, fullInstallPath, createOAPPTemplateDirectory, downloadResult.Result, reInstall, providerType);
                    else
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured downloading the OAPP Template with the DownloadOAPPTemplateAsync method, reason: {downloadResult.Message}");
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
                OnOAPPTemplateInstallStatusChanged?.Invoke(this, new OAPPTemplateInstallStatusEventArgs() { OAPPTemplateDNA = OAPPTemplate.OAPPTemplateDNA, Status = Enums.OAPPTemplateInstallStatus.Error, ErrorMessage = result.Message });

            return result;
        }

        public async Task<OASISResult<IInstalledOAPPTemplate>> InstallOAPPTemplateAsync(Guid avatarId, string fullPathToPublishedOAPPTemplateFile, string fullInstallPath, bool createOAPPTemplateDirectory = true, IDownloadedOAPPTemplate downloadedOAPPTemplate = null, bool reInstall = false, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IInstalledOAPPTemplate> result = new OASISResult<IInstalledOAPPTemplate>();
            string errorMessage = "Error occured in OAPPTemplateManager.InstallOAPPTemplateAsync. Reason: ";
            IOAPPTemplateDNA OAPPTemplateDNA = null;
            string tempPath = "";
            InstalledOAPPTemplate installedOAPPTemplate = null;
            int totalInstalls = 0;

            try
            {
                tempPath = Path.GetTempPath();
                tempPath = Path.Combine(tempPath, "OAPP Template");

                if (Directory.Exists(tempPath))
                    Directory.Delete(tempPath, true);

                //Unzip
                OnOAPPTemplateInstallStatusChanged?.Invoke(this, new OAPPTemplateInstallStatusEventArgs() { Status = Enums.OAPPTemplateInstallStatus.Decompressing });
                ZipFile.ExtractToDirectory(fullPathToPublishedOAPPTemplateFile, tempPath, Encoding.Default, true);
                OASISResult<IOAPPTemplateDNA> OAPPTemplateDNAResult = await ReadOAPPTemplateDNAAsync(tempPath);

                if (OAPPTemplateDNAResult != null && OAPPTemplateDNAResult.Result != null && !OAPPTemplateDNAResult.IsError)
                {
                    //Load the OAPPTemplate from the OASIS to make sure the OAPPTemplateDNA is valid (and has not been tampered with).

                    //TODO: Check if this works ok? What if they tamper with the VersionSequence in the DNA file?!
                    OASISResult<IOAPPTemplate> oappTemplateLoadResult = await LoadOAPPTemplateAsync(OAPPTemplateDNAResult.Result.Id, avatarId, OAPPTemplateDNAResult.Result.VersionSequence, providerType);
                    //OASISResult<IOAPPTemplate> oappTemplateLoadResult = await LoadOAPPTemplateAsync(OAPPTemplateDNAResult.Result.Id, false, 0, providerType);

                    if (oappTemplateLoadResult != null && oappTemplateLoadResult.Result != null && !oappTemplateLoadResult.IsError)
                    {
                        //TODO: Not sure if we want to add a check here to compare the OAPPTemplateDNA in the OAPPTemplate dir with the one stored in the OASIS?
                        OAPPTemplateDNA = oappTemplateLoadResult.Result.OAPPTemplateDNA;

                        if (createOAPPTemplateDirectory)
                            fullInstallPath = Path.Combine(fullInstallPath, string.Concat(OAPPTemplateDNAResult.Result.Name, "_v", OAPPTemplateDNAResult.Result.Version));

                        if (Directory.Exists(fullInstallPath))
                            Directory.Delete(fullInstallPath, true);

                        //Directory.CreateDirectory(fullInstallPath);
                        Directory.Move(tempPath, fullInstallPath);
                        //Directory.Delete(tempPath);

                        OnOAPPTemplateInstallStatusChanged?.Invoke(this, new OAPPTemplateInstallStatusEventArgs() { OAPPTemplateDNA = OAPPTemplateDNAResult.Result, Status = Enums.OAPPTemplateInstallStatus.Installing });
                        OASISResult<IAvatar> avatarResult = await AvatarManager.Instance.LoadAvatarAsync(avatarId, false, true, providerType);

                        if (avatarResult != null && !avatarResult.IsError && avatarResult.Result != null)
                        {
                            if (downloadedOAPPTemplate == null)
                            {
                                //OASISResult<DownloadedOAPPTemplate> downloadedOAPPTemplateResult = await Data.LoadHolonsByMetaDataAsync<DownloadedOAPPTemplate>("OAPPTemplateId", OAPPTemplateDNAResult.Result.Id.ToString(), false, false, 0, true, 0, false, HolonType.All, providerType);
                                OASISResult<IEnumerable<DownloadedOAPPTemplate>> downloadedOAPPTemplateResult = await Data.LoadHolonsByMetaDataAsync<DownloadedOAPPTemplate>("OAPPTemplateId", OAPPTemplateDNAResult.Result.Id.ToString(), HolonType.All, true, true, 0, true, false, 0, HolonType.All, 0, providerType);

                                if (downloadedOAPPTemplateResult != null && !downloadedOAPPTemplateResult.IsError && downloadedOAPPTemplateResult.Result != null)
                                    downloadedOAPPTemplate = downloadedOAPPTemplateResult.Result.FirstOrDefault();
                                else
                                    OASISErrorHandling.HandleWarning(ref result, $"The OAPP Template was installed but the DownloadedOAPPTemplate could not be found. Reason: {downloadedOAPPTemplateResult.Message}");
                            }

                            if (!reInstall)
                            {
                                //If it's a re-install then it doesnt count as an install so we dont need to update the counts.
                                OAPPTemplateDNA.Installs++;

                                installedOAPPTemplate = new InstalledOAPPTemplate()
                                {
                                    Name = string.Concat(OAPPTemplateDNA.Name, " Installed Holon"),
                                    Description = string.Concat(OAPPTemplateDNA.Description, " Installed Holon"),
                                    //OAPPTemplateId = OAPPTemplateDNAResult.Result.OAPPTemplateId,
                                    OAPPTemplateDNA = OAPPTemplateDNA,
                                    InstalledBy = avatarId,
                                    InstalledByAvatarUsername = avatarResult.Result.Username,
                                    InstalledOn = DateTime.Now,
                                    InstalledPath = fullInstallPath,
                                    //DownloadedOAPPTemplate = downloadedOAPPTemplate,
                                    DownloadedBy = downloadedOAPPTemplate.DownloadedBy,
                                    DownloadedByAvatarUsername = downloadedOAPPTemplate.DownloadedByAvatarUsername,
                                    DownloadedOn = downloadedOAPPTemplate.DownloadedOn,
                                    DownloadedPath = downloadedOAPPTemplate.DownloadedPath,
                                    DownloadedOAPPTemplateHolonId = downloadedOAPPTemplate.Id,
                                    Active = "1",
                                    //OAPPTemplateVersion = OAPPTemplateDNA.Version
                                };

                                installedOAPPTemplate.MetaData["Version"] = OAPPTemplateDNA.Version;
                                installedOAPPTemplate.MetaData["VersionSequence"] = OAPPTemplateDNA.VersionSequence;
                                installedOAPPTemplate.MetaData["OAPPTemplateId"] = OAPPTemplateDNA.Id;
                                
                                await UpdateInstallCountsAsync(installedOAPPTemplate, OAPPTemplateDNA, avatarId, result, errorMessage, providerType);
                            }
                            else
                            {
                                OASISResult<IInstalledOAPPTemplate> installedOAPPTemplateResult = await LoadInstalledOAPPTemplateAsync(avatarId, OAPPTemplateDNAResult.Result.Id, OAPPTemplateDNAResult.Result.Version, false, providerType);

                                if (installedOAPPTemplateResult != null && installedOAPPTemplateResult.Result != null && !installedOAPPTemplateResult.IsError)
                                {
                                    installedOAPPTemplate = (InstalledOAPPTemplate)installedOAPPTemplateResult.Result;
                                    installedOAPPTemplate.Active = "1";
                                    installedOAPPTemplate.UninstalledBy = Guid.Empty;
                                    installedOAPPTemplate.UninstalledByAvatarUsername = "";
                                    installedOAPPTemplate.UninstalledOn = DateTime.MinValue;
                                    installedOAPPTemplate.InstalledBy = avatarId;
                                    installedOAPPTemplate.InstalledByAvatarUsername = avatarResult.Result.Username;
                                    installedOAPPTemplate.InstalledOn = DateTime.Now;
                                    installedOAPPTemplate.InstalledPath = fullInstallPath;
                                    installedOAPPTemplate.DownloadedBy = downloadedOAPPTemplate.DownloadedBy;
                                    installedOAPPTemplate.DownloadedByAvatarUsername = downloadedOAPPTemplate.DownloadedByAvatarUsername;
                                    installedOAPPTemplate.DownloadedOn = downloadedOAPPTemplate.DownloadedOn;
                                    installedOAPPTemplate.DownloadedPath = downloadedOAPPTemplate.DownloadedPath;
                                }
                                else
                                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured re-installing OAPP Template calling LoadInstalledOAPPTemplateAsync. Reason: {installedOAPPTemplateResult.Message}");
                            }

                            if (!result.IsError)
                            {
                                OASISResult<IOAPPTemplate> saveResult = await SaveOAPPTemplateAsync(installedOAPPTemplate, avatarId, providerType);

                                if (saveResult != null && saveResult.Result != null && !saveResult.IsError)
                                {
                                    //result.Result = installedOAPPTemplate;
                                    //result.Result.DownloadedOAPPTemplate = downloadedOAPPTemplate;
                                    oappTemplateLoadResult.Result.OAPPTemplateDNA = OAPPTemplateDNA;

                                    OASISResult<IOAPPTemplate> oappSaveResult = await SaveOAPPTemplateAsync(oappTemplateLoadResult.Result, avatarId, providerType);

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

                                        result.Result = installedOAPPTemplate;
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
                    }
                    else
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadOAPPTemplateAsync method. Reason: {oappTemplateLoadResult.Message}");
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
                OnOAPPTemplateInstallStatusChanged?.Invoke(this, new OAPPTemplateInstallStatusEventArgs() { OAPPTemplateDNA = OAPPTemplateDNA, Status = Enums.OAPPTemplateInstallStatus.Error, ErrorMessage = result.Message });

            return result;
        }

        public OASISResult<IInstalledOAPPTemplate> InstallOAPPTemplate(Guid avatarId, string fullPathToPublishedOAPPTemplateFile, string fullInstallPath, bool createOAPPTemplateDirectory = true, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IInstalledOAPPTemplate> result = new OASISResult<IInstalledOAPPTemplate>();
            string errorMessage = "Error occured in OAPPTemplateManager.InstallOAPPTemplate. Reason: ";
            IOAPPTemplateDNA OAPPTemplateDNA = null;

            try
            {
                OASISResult<IOAPPTemplateDNA> OAPPTemplateDNAResult = ReadOAPPTemplateDNA(fullPathToPublishedOAPPTemplateFile);

                if (OAPPTemplateDNAResult != null && OAPPTemplateDNAResult.Result != null && !OAPPTemplateDNAResult.IsError)
                {
                    //Load the OAPPTemplate from the OASIS to make sure the OAPPTemplateDNA is valid (and has not been tampered with).
                    OASISResult<IOAPPTemplate> oappResult = LoadOAPPTemplate(OAPPTemplateDNAResult.Result.Id, avatarId, OAPPTemplateDNAResult.Result.VersionSequence, providerType);

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
                                //OAPPTemplateDNA.Downloads++;
                                OAPPTemplateDNA.Installs++;
                                oappResult.Result.OAPPTemplateDNA = OAPPTemplateDNA;

                                OASISResult<IOAPPTemplate> oappSaveResult = SaveOAPPTemplate(oappResult.Result, avatarId, providerType);

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

        public OASISResult<IInstalledOAPPTemplate> InstallOAPPTemplate(Guid avatarId, IOAPPTemplate OAPPTemplate, string fullInstallPath, string fullDownloadPath = "", bool createOAPPTemplateDirectory = true, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IInstalledOAPPTemplate> result = new OASISResult<IInstalledOAPPTemplate>();
            string errorMessage = "Error occured in OAPPTemplateManager.InstallOAPPTemplate. Reason: ";

            try
            {
                string OAPPTemplatePath = Path.Combine("temp", OAPPTemplate.Name, ".oapptemplate");

                if (OAPPTemplate.PublishedOAPPTemplate != null)
                {
                    File.WriteAllBytes(OAPPTemplatePath, OAPPTemplate.PublishedOAPPTemplate);
                    result = InstallOAPPTemplate(avatarId, OAPPTemplatePath, fullInstallPath, createOAPPTemplateDirectory, providerType);
                }
                {
                    try
                    {
                        StorageClient storage = StorageClient.Create();

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
                        storage.DownloadObject(GOOGLE_CLOUD_BUCKET_NAME, string.Concat(OAPPTemplate.Name, ".oapptemplate"), fileStream, downloadObjectOptions, progress: progressReporter);

                        _progress = 100;
                        OnOAPPTemplateDownloadStatusChanged?.Invoke(this, new OAPPTemplateDownloadProgressEventArgs() { Progress = _progress, Status = Enums.OAPPTemplateDownloadStatus.Downloading });
                        CLIEngine.DisposeProgressBar(false);
                        Console.WriteLine("");

                        result = InstallOAPPTemplate(avatarId, OAPPTemplatePath, fullInstallPath, createOAPPTemplateDirectory, providerType);
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

        public async Task<OASISResult<IInstalledOAPPTemplate>> InstallOAPPTemplateAsync(Guid avatarId, Guid OAPPTemplateId, int version, string fullInstallPath, string fullDownloadPath = "", bool createOAPPTemplateDirectory = true, bool reInstall = false, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IInstalledOAPPTemplate> result = new OASISResult<IInstalledOAPPTemplate>();
            OASISResult<IOAPPTemplate> OAPPTemplateResult = await LoadOAPPTemplateAsync(OAPPTemplateId, avatarId, version, providerType);

            if (OAPPTemplateResult != null && !OAPPTemplateResult.IsError && OAPPTemplateResult.Result != null)
                result = await DownloadAndInstallOAPPTemplateAsync(avatarId, OAPPTemplateResult.Result, fullInstallPath, fullDownloadPath, createOAPPTemplateDirectory, reInstall, providerType);
            else
            {
                OASISErrorHandling.HandleError(ref result, $"Error occured in OAPPTemplateManager.InstallOAPPTemplateAsync loading the OAPP Template with the LoadOAPPTemplateAsync method, reason: {OASISErrorHandling.ProcessMessage(result, $"No result found for id {OAPPTemplateId.ToString()}")}");
                OnOAPPTemplateInstallStatusChanged?.Invoke(this, new OAPPTemplateInstallStatusEventArgs() { Status = Enums.OAPPTemplateInstallStatus.Error, ErrorMessage = result.Message });
            }

            return result;
        }

        public OASISResult<IInstalledOAPPTemplate> InstallOAPPTemplate(Guid avatarId, Guid OAPPTemplateId, int version, string fullInstallPath, string fullDownloadPath = "", bool createOAPPTemplateDirectory = true, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IInstalledOAPPTemplate> result = new OASISResult<IInstalledOAPPTemplate>();
            OASISResult<IOAPPTemplate> OAPPTemplateResult = LoadOAPPTemplate(OAPPTemplateId, avatarId, version, providerType);

            if (OAPPTemplateResult != null && !OAPPTemplateResult.IsError && OAPPTemplateResult.Result != null)
                result = InstallOAPPTemplate(avatarId, OAPPTemplateResult.Result, fullInstallPath, fullDownloadPath, createOAPPTemplateDirectory, providerType);
            else
            {
                OASISErrorHandling.HandleError(ref result, $"Error occured in OAPPTemplateManager.InstallOAPPTemplate loading the OAPP Template with the LoadOAPPTemplate method, reason: {result.Message}");
                OnOAPPTemplateInstallStatusChanged?.Invoke(this, new OAPPTemplateInstallStatusEventArgs() { Status = Enums.OAPPTemplateInstallStatus.Error, ErrorMessage = result.Message });
            }

            return result;
        }

        public async Task<OASISResult<IInstalledOAPPTemplate>> UninstallOAPPTemplateAsync(IInstalledOAPPTemplate installedOAPPTemplate, Guid avatarId, string errorMessage, ProviderType providerType)
        {
            OASISResult<IInstalledOAPPTemplate> result = new OASISResult<IInstalledOAPPTemplate>();

            try
            {
                Directory.Delete(installedOAPPTemplate.InstalledPath, true);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} Error occured attempting to delete the OAPP Template folder ({installedOAPPTemplate.InstalledPath}) Reason: {ex.Message}");
            }

            //if (!result.IsError)
            //{
                OASISResult<IAvatar> avatarResult = await AvatarManager.Instance.LoadAvatarAsync(avatarId, false, true, providerType, 0);

                if (avatarResult != null && avatarResult.Result != null && !avatarResult.IsError)
                {
                    installedOAPPTemplate.UninstalledBy = avatarId;
                    installedOAPPTemplate.UninstalledOn = DateTime.Now;
                    installedOAPPTemplate.UninstalledByAvatarUsername = avatarResult.Result.Username;
                    installedOAPPTemplate.Active = "0";

                    OASISResult<InstalledOAPPTemplate> saveIntalledOAPPTemplateResult = await installedOAPPTemplate.SaveAsync<InstalledOAPPTemplate>();

                    if (saveIntalledOAPPTemplateResult != null && !saveIntalledOAPPTemplateResult.IsError && saveIntalledOAPPTemplateResult.Result != null)
                    {
                        result.Message = "OAPP Template Uninstalled";
                        result.Result = saveIntalledOAPPTemplateResult.Result;
                    }
                    else
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling SaveAsync. Reason: {saveIntalledOAPPTemplateResult.Message}");
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadAvatarAsync. Reason: {avatarResult.Message}");
            //}

            return result;
        }

        public OASISResult<IInstalledOAPPTemplate> UninstallOAPPTemplate(IInstalledOAPPTemplate installedOAPPTemplate, Guid avatarId, string errorMessage, ProviderType providerType)
        {
            OASISResult<IInstalledOAPPTemplate> result = new OASISResult<IInstalledOAPPTemplate>();

            try
            {
                Directory.Delete(installedOAPPTemplate.InstalledPath, true);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured attempting to delete the OAPP Template folder ({installedOAPPTemplate.InstalledPath}) Reason: {ex.Message}");
            }

            if (!result.IsError)
            {
                OASISResult<IAvatar> avatarResult = AvatarManager.Instance.LoadAvatar(avatarId, false, true, providerType, 0);

                if (avatarResult != null && avatarResult.Result != null && !avatarResult.IsError)
                {
                    installedOAPPTemplate.UninstalledBy = avatarId;
                    installedOAPPTemplate.UninstalledOn = DateTime.Now;
                    installedOAPPTemplate.UninstalledByAvatarUsername = avatarResult.Result.Username;
                    installedOAPPTemplate.Active = "0";

                    OASISResult<InstalledOAPPTemplate> saveIntalledOAPPTemplateResult = installedOAPPTemplate.Save<InstalledOAPPTemplate>();

                    if (saveIntalledOAPPTemplateResult != null && !saveIntalledOAPPTemplateResult.IsError && saveIntalledOAPPTemplateResult.Result != null)
                    {
                        result.Message = "OAPP Template Uninstalled";
                        result.Result = saveIntalledOAPPTemplateResult.Result;
                    }
                    else
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling Save. Reason: {saveIntalledOAPPTemplateResult.Message}");
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadAvatar. Reason: {avatarResult.Message}");
            }

            return result;
        }

        public async Task<OASISResult<IInstalledOAPPTemplate>> UninstallOAPPTemplateAsync(Guid OAPPTemplateId, int versionSequence, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IInstalledOAPPTemplate> result = new OASISResult<IInstalledOAPPTemplate>();
            string errorMessage = "Error occured in OAPPTemplateManager.UninstallOAPPTemplateAsync. Reason: ";

            return await UninstallOAPPTemplateAsync(await Data.LoadHolonByMetaDataAsync<InstalledOAPPTemplate>(new Dictionary<string, string>()
            {
                { "OAPPTemplateId", OAPPTemplateId.ToString() },
                { "VersionSequene", versionSequence.ToString() }

            }, MetaKeyValuePairMatchMode.All, HolonType.InstalledOAPPTemplate, true, true, 0, true, 0, false, HolonType.All, providerType), avatarId, errorMessage, providerType);
        }

        public OASISResult<IInstalledOAPPTemplate> UninstallOAPPTemplate(Guid OAPPTemplateId, int versionSequence, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IInstalledOAPPTemplate> result = new OASISResult<IInstalledOAPPTemplate>();
            string errorMessage = "Error occured in OAPPTemplateManager.UninstallOAPPTemplate. Reason: ";

            return UninstallOAPPTemplate(Data.LoadHolonByMetaData<InstalledOAPPTemplate>(new Dictionary<string, string>()
            {
                { "OAPPTemplateId", OAPPTemplateId.ToString() },
                { "VersionSequene", versionSequence.ToString() }

            }, MetaKeyValuePairMatchMode.All, HolonType.InstalledOAPPTemplate, true, true, 0, true, false, HolonType.All, 0, providerType), avatarId, errorMessage, providerType);
        }

        public async Task<OASISResult<IInstalledOAPPTemplate>> UninstallOAPPTemplateAsync(Guid OAPPTemplateId, string version, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IInstalledOAPPTemplate> result = new OASISResult<IInstalledOAPPTemplate>();
            string errorMessage = "Error occured in OAPPTemplateManager.UninstallOAPPTemplateAsync. Reason: ";

            return await UninstallOAPPTemplateAsync(await Data.LoadHolonByMetaDataAsync<InstalledOAPPTemplate>(new Dictionary<string, string>()
            {
                { "OAPPTemplateId", OAPPTemplateId.ToString() },
                { "Version", version }

            }, MetaKeyValuePairMatchMode.All, HolonType.InstalledOAPPTemplate, true, true, 0, true, 0, false, HolonType.All, providerType), avatarId, errorMessage, providerType);
        }

        public OASISResult<IInstalledOAPPTemplate> UninstallOAPPTemplate(Guid OAPPTemplateId, string version, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IInstalledOAPPTemplate> result = new OASISResult<IInstalledOAPPTemplate>();
            string errorMessage = "Error occured in OAPPTemplateManager.UninstallOAPPTemplateAsync. Reason: ";

            return UninstallOAPPTemplate(Data.LoadHolonByMetaData<InstalledOAPPTemplate>(new Dictionary<string, string>()
            {
                { "OAPPTemplateId", OAPPTemplateId.ToString() },
                { "Version", version }

            }, MetaKeyValuePairMatchMode.All, HolonType.InstalledOAPPTemplate, true, true, 0, true, false, HolonType.All, 0, providerType), avatarId, errorMessage, providerType);
        }

        public async Task<OASISResult<IInstalledOAPPTemplate>> UninstallOAPPTemplateAsync(string OAPPTemplateName, int versionSequence, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IInstalledOAPPTemplate> result = new OASISResult<IInstalledOAPPTemplate>();
            string errorMessage = "Error occured in OAPPTemplateManager.UninstallOAPPTemplateAsync. Reason: ";

            return await UninstallOAPPTemplateAsync(await Data.LoadHolonByMetaDataAsync<InstalledOAPPTemplate>(new Dictionary<string, string>()
            {
                { "OAPPTemplateName", OAPPTemplateName },
                { "VersionSequence", versionSequence.ToString() }

            }, MetaKeyValuePairMatchMode.All, HolonType.InstalledOAPPTemplate, true, true, 0, true, 0, false, HolonType.All, providerType), avatarId, errorMessage, providerType);
        }

        public OASISResult<IInstalledOAPPTemplate> UninstallOAPPTemplate(string OAPPTemplateName, int versionSequence, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IInstalledOAPPTemplate> result = new OASISResult<IInstalledOAPPTemplate>();
            string errorMessage = "Error occured in OAPPTemplateManager.UninstallOAPPTemplate. Reason: ";

            return UninstallOAPPTemplate(Data.LoadHolonByMetaData<InstalledOAPPTemplate>(new Dictionary<string, string>()
            {
                { "OAPPTemplateName", OAPPTemplateName },
                { "VersionSequene", versionSequence.ToString() }

            }, MetaKeyValuePairMatchMode.All, HolonType.InstalledOAPPTemplate, true, true, 0, true, false, HolonType.All), avatarId, errorMessage, providerType);
        }

        public async Task<OASISResult<IInstalledOAPPTemplate>> UninstallOAPPTemplateAsync(string OAPPTemplateName, string version, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IInstalledOAPPTemplate> result = new OASISResult<IInstalledOAPPTemplate>();
            string errorMessage = "Error occured in OAPPTemplateManager.UninstallOAPPTemplate. Reason: ";

            return UninstallOAPPTemplate(await Data.LoadHolonByMetaDataAsync<InstalledOAPPTemplate>(new Dictionary<string, string>()
            {
                { "OAPPTemplateName", OAPPTemplateName },
                { "Version", version }

            }, MetaKeyValuePairMatchMode.All, HolonType.InstalledOAPPTemplate, true, true, 0, true, 0, false, HolonType.All, providerType), avatarId, errorMessage, providerType);
        }

        public OASISResult<IInstalledOAPPTemplate> UninstallOAPPTemplate(string OAPPTemplateName, string version, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IInstalledOAPPTemplate> result = new OASISResult<IInstalledOAPPTemplate>();
            string errorMessage = "Error occured in OAPPTemplateManager.UninstallOAPPTemplate. Reason: ";

            return UninstallOAPPTemplate(Data.LoadHolonByMetaData<InstalledOAPPTemplate>(new Dictionary<string, string>()
            {
                { "OAPPTemplateName", OAPPTemplateName },
                { "Version", version }

            }, MetaKeyValuePairMatchMode.All, HolonType.InstalledOAPPTemplate, true, true, 0, true, false, HolonType.All), avatarId, errorMessage, providerType);
        }

        public async Task<OASISResult<IEnumerable<IInstalledOAPPTemplate>>> ListInstalledOAPPTemplatesAsync(Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<IInstalledOAPPTemplate>> result = new OASISResult<IEnumerable<IInstalledOAPPTemplate>>();
            OASISResult<IEnumerable<InstalledOAPPTemplate>> installedOAPPTemplatesResult = await Data.LoadHolonsForParentAsync<InstalledOAPPTemplate>(avatarId, HolonType.InstalledOAPPTemplate, false, false, 0, true, false, 0, HolonType.All, 0, providerType);
            string errorMessage = "Error occured in OAPPTemplateManager.ListInstalledOAPPTemplatesAsync. Reason: ";

            if (installedOAPPTemplatesResult != null && !installedOAPPTemplatesResult.IsError && installedOAPPTemplatesResult.Result != null)
            {
                result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<IEnumerable<InstalledOAPPTemplate>, IEnumerable<IInstalledOAPPTemplate>>(installedOAPPTemplatesResult);
                result.Result = Mapper.Convert<InstalledOAPPTemplate, IInstalledOAPPTemplate>(installedOAPPTemplatesResult.Result.Where(x => x.UninstalledOn == DateTime.MinValue));
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
                result.Result = Mapper.Convert<InstalledOAPPTemplate, IInstalledOAPPTemplate>(installedOAPPTemplatesResult.Result.Where(x => x.UninstalledOn == DateTime.MinValue));
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonsForParent. Reason: {installedOAPPTemplatesResult.Message}");

            return result;
        }

        public async Task<OASISResult<IEnumerable<IInstalledOAPPTemplate>>> ListUnInstalledOAPPTemplatesAsync(Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<IInstalledOAPPTemplate>> result = new OASISResult<IEnumerable<IInstalledOAPPTemplate>>();
            OASISResult<IEnumerable<InstalledOAPPTemplate>> installedOAPPTemplatesResult = await Data.LoadHolonsForParentAsync<InstalledOAPPTemplate>(avatarId, HolonType.InstalledOAPPTemplate, false, false, 0, true, false, 0, HolonType.All, 0, providerType);
            string errorMessage = "Error occured in OAPPTemplateManager.ListUnInstalledOAPPTemplatesAsync. Reason: ";

            if (installedOAPPTemplatesResult != null && !installedOAPPTemplatesResult.IsError && installedOAPPTemplatesResult.Result != null)
            {
                result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<IEnumerable<InstalledOAPPTemplate>, IEnumerable<IInstalledOAPPTemplate>>(installedOAPPTemplatesResult);
                result.Result = Mapper.Convert<InstalledOAPPTemplate, IInstalledOAPPTemplate>(installedOAPPTemplatesResult.Result.Where(x => x.UninstalledOn != DateTime.MinValue));
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonsForParentAsync. Reason: {installedOAPPTemplatesResult.Message}");

            return result;
        }

        public OASISResult<IEnumerable<IInstalledOAPPTemplate>> ListUnInstalledOAPPTemplates(Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<IInstalledOAPPTemplate>> result = new OASISResult<IEnumerable<IInstalledOAPPTemplate>>();
            OASISResult<IEnumerable<InstalledOAPPTemplate>> installedOAPPTemplatesResult = Data.LoadHolonsForParent<InstalledOAPPTemplate>(avatarId, HolonType.InstalledOAPPTemplate, false, false, 0, true, false, 0, HolonType.All, 0, providerType);
            string errorMessage = "Error occured in OAPPTemplateManager.ListUnInstalledOAPPTemplates. Reason: ";

            if (installedOAPPTemplatesResult != null && !installedOAPPTemplatesResult.IsError && installedOAPPTemplatesResult.Result != null)
            {
                result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<IEnumerable<InstalledOAPPTemplate>, IEnumerable<IInstalledOAPPTemplate>>(installedOAPPTemplatesResult);
                result.Result = Mapper.Convert<InstalledOAPPTemplate, IInstalledOAPPTemplate>(installedOAPPTemplatesResult.Result.Where(x => x.UninstalledOn != DateTime.MinValue));
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonsForParent. Reason: {installedOAPPTemplatesResult.Message}");

            return result;
        }

        public async Task<OASISResult<IEnumerable<IOAPPTemplate>>> ListUnpublishedOAPPTemplatesAsync(Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<IOAPPTemplate>> result = new OASISResult<IEnumerable<IOAPPTemplate>>();
            OASISResult<IEnumerable<OAPPTemplate>> unpublishedOAPPTemplatesResult = await Data.LoadHolonsForParentAsync<OAPPTemplate>(avatarId, HolonType.OAPPTemplate, false, false, 0, true, false, 0, HolonType.All, 0, providerType);
            string errorMessage = "Error occured in OAPPTemplateManager.ListUnpublishedOAPPTemplatesAsync. Reason: ";

            if (unpublishedOAPPTemplatesResult != null && !unpublishedOAPPTemplatesResult.IsError && unpublishedOAPPTemplatesResult.Result != null)
            {
                result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<IEnumerable<OAPPTemplate>, IEnumerable<IOAPPTemplate>>(unpublishedOAPPTemplatesResult);
                result.Result = Mapper.Convert<OAPPTemplate, IOAPPTemplate>(unpublishedOAPPTemplatesResult.Result.Where(x => x.OAPPTemplateDNA.PublishedOn == DateTime.MinValue && x.OAPPTemplateDNA.OAPPTemplateFileSize > 0));
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonsForParentAsync. Reason: {unpublishedOAPPTemplatesResult.Message}");

            return result;
        }

        public OASISResult<IEnumerable<IOAPPTemplate>> ListUnpublishedOAPPTemplates(Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<IOAPPTemplate>> result = new OASISResult<IEnumerable<IOAPPTemplate>>();
            OASISResult<IEnumerable<OAPPTemplate>> unpublishedOAPPTemplatesResult = Data.LoadHolonsForParent<OAPPTemplate>(avatarId, HolonType.OAPPTemplate, false, false, 0, true, false, 0, HolonType.All, 0, providerType);
            string errorMessage = "Error occured in OAPPTemplateManager.ListUnpublishedOAPPTemplates. Reason: ";

            if (unpublishedOAPPTemplatesResult != null && !unpublishedOAPPTemplatesResult.IsError && unpublishedOAPPTemplatesResult.Result != null)
            {
                result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<IEnumerable<OAPPTemplate>, IEnumerable<IOAPPTemplate>>(unpublishedOAPPTemplatesResult);
                result.Result = Mapper.Convert<OAPPTemplate, IOAPPTemplate>(unpublishedOAPPTemplatesResult.Result.Where(x => x.OAPPTemplateDNA.PublishedOn == DateTime.MinValue && x.OAPPTemplateDNA.OAPPTemplateFileSize > 0));
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonsForParent. Reason: {unpublishedOAPPTemplatesResult.Message}");

            return result;
        }

        public async Task<OASISResult<IEnumerable<IOAPPTemplate>>> ListDeactivatedOAPPTemplatesAsync(Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<IOAPPTemplate>> result = new OASISResult<IEnumerable<IOAPPTemplate>>();
            //OASISResult<IEnumerable<OAPPTemplate>> deactivatedOAPPTemplatesResult = await Data.LoadHolonsForParentAsync<OAPPTemplate>(avatarId, HolonType.InstalledOAPPTemplate, false, false, 0, true, false, 0, HolonType.All, 0, providerType);
            string errorMessage = "Error occured in OAPPTemplateManager.ListDeactivatedOAPPTemplatesAsync. Reason: ";
            OASISResult<IEnumerable<OAPPTemplate>> deactivatedOAPPTemplatesResult = await Data.LoadHolonsByMetaDataAsync<OAPPTemplate>("Active", "0", HolonType.OAPPTemplate, true, true, 0, true, false, 0, HolonType.All, 0, providerType);

            if (deactivatedOAPPTemplatesResult != null && !deactivatedOAPPTemplatesResult.IsError && deactivatedOAPPTemplatesResult.Result != null)
            {
                result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<IEnumerable<OAPPTemplate>, IEnumerable<IOAPPTemplate>>(deactivatedOAPPTemplatesResult);
                result.Result = Mapper.Convert<OAPPTemplate, IOAPPTemplate>(deactivatedOAPPTemplatesResult.Result);
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonsForParentAsync. Reason: {deactivatedOAPPTemplatesResult.Message}");

            return result;
        }

        public OASISResult<IEnumerable<IOAPPTemplate>> ListDeactivatedOAPPTemplates(Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<IOAPPTemplate>> result = new OASISResult<IEnumerable<IOAPPTemplate>>();
            //OASISResult<IEnumerable<OAPPTemplate>> deactivatedOAPPTemplatesResult = Data.LoadHolonsForParent<OAPPTemplate>(avatarId, HolonType.OAPPTemplate, false, false, 0, true, false, 0, HolonType.All, 0, providerType);
            string errorMessage = "Error occured in OAPPTemplateManager.ListDeactivatedOAPPTemplates. Reason: ";
            OASISResult<IEnumerable<OAPPTemplate>> deactivatedOAPPTemplatesResult = Data.LoadHolonsByMetaData<OAPPTemplate>("Active", "0", HolonType.OAPPTemplate, true, true, 0, true, false, 0, HolonType.All, 0, providerType);

            if (deactivatedOAPPTemplatesResult != null && !deactivatedOAPPTemplatesResult.IsError && deactivatedOAPPTemplatesResult.Result != null)
            {
                result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<IEnumerable<OAPPTemplate>, IEnumerable<IOAPPTemplate>>(deactivatedOAPPTemplatesResult);
                //result.Result = Mapper.Convert<OAPPTemplate, IOAPPTemplate>(deactivatedOAPPTemplatesResult.Result.Where(x => x.OAPPTemplateDNA.IsActive != true));
                result.Result = Mapper.Convert<OAPPTemplate, IOAPPTemplate>(deactivatedOAPPTemplatesResult.Result);
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonsForParent. Reason: {deactivatedOAPPTemplatesResult.Message}");

            return result;
        }

        public async Task<OASISResult<bool>> IsOAPPTemplateInstalledAsync(Guid avatarId, Guid OAPPTemplateId, int versionSequence, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<bool> result = new OASISResult<bool>();
            string errorMessage = "Error occured in OAPPTemplateManager.IsOAPPTemplateInstalledAsync. Reason: ";

            OASISResult<InstalledOAPPTemplate> installedOAPPTemplatesResult = await Data.LoadHolonByMetaDataAsync<InstalledOAPPTemplate>(new Dictionary<string, string>()
            {
                { "OAPPTemplateId", OAPPTemplateId.ToString() },
                { "VersionSequence", versionSequence.ToString() },
                { "Active", "1" }

            }, MetaKeyValuePairMatchMode.All, HolonType.InstalledOAPPTemplate, true, true, 0, true, 0, false, HolonType.All, providerType);

            if (installedOAPPTemplatesResult != null && !installedOAPPTemplatesResult.IsError)
            {
                if (installedOAPPTemplatesResult.Result != null)
                    result.Result = true;
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonByMetaDataAsync. Reason: {installedOAPPTemplatesResult.Message}");

            return result;
        }

        public OASISResult<bool> IsOAPPTemplateInstalled(Guid avatarId, Guid OAPPTemplateId, int versionSequence, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<bool> result = new OASISResult<bool>();
            string errorMessage = "Error occured in OAPPTemplateManager.IsOAPPTemplateInstalledAsync. Reason: ";

            OASISResult<InstalledOAPPTemplate> installedOAPPTemplatesResult = Data.LoadHolonByMetaData<InstalledOAPPTemplate>(new Dictionary<string, string>()
            {
                { "OAPPTemplateId", OAPPTemplateId.ToString() },
                { "VersionSequence", versionSequence.ToString() },
                { "Active", "1" }

            }, MetaKeyValuePairMatchMode.All, HolonType.InstalledOAPPTemplate, true, true, 0, true, false, HolonType.All, 0, providerType);

            if (installedOAPPTemplatesResult != null && !installedOAPPTemplatesResult.IsError && installedOAPPTemplatesResult.Result != null)
                result.Result = true;
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonByMetaDataAsync. Reason: {installedOAPPTemplatesResult.Message}");

            return result;
        }

        public async Task<OASISResult<bool>> IsOAPPTemplateInstalledAsync(Guid avatarId, Guid OAPPTemplateId, string version, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<bool> result = new OASISResult<bool>();
            string errorMessage = "Error occured in OAPPTemplateManager.IsOAPPTemplateInstalledAsync. Reason: ";

            OASISResult<InstalledOAPPTemplate> installedOAPPTemplatesResult = await Data.LoadHolonByMetaDataAsync<InstalledOAPPTemplate>(new Dictionary<string, string>()
            {
                { "OAPPTemplateId", OAPPTemplateId.ToString() },
                { "Version", version },
                { "Active", "1" }

            }, MetaKeyValuePairMatchMode.All, HolonType.InstalledOAPPTemplate, true, true, 0, true, 0, false, HolonType.InstalledOAPPTemplate, providerType);

            if (installedOAPPTemplatesResult != null && !installedOAPPTemplatesResult.IsError)
            {
                if (installedOAPPTemplatesResult.Result != null)
                    result.Result = true;
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonByMetaDataAsync. Reason: {installedOAPPTemplatesResult.Message}");

            return result;
        }

        public OASISResult<bool> IsOAPPTemplateInstalled(Guid avatarId, Guid OAPPTemplateId, string version, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<bool> result = new OASISResult<bool>();
            string errorMessage = "Error occured in OAPPTemplateManager.IsOAPPTemplateInstalledAsync. Reason: ";

            OASISResult<InstalledOAPPTemplate> installedOAPPTemplatesResult = Data.LoadHolonByMetaData<InstalledOAPPTemplate>(new Dictionary<string, string>()
            {
                { "OAPPTemplateId", OAPPTemplateId.ToString() },
                { "Version", version },
                { "Active", "1" }

            }, MetaKeyValuePairMatchMode.All, HolonType.InstalledOAPPTemplate, true, true, 0, true, false, HolonType.All, 0, providerType);

            if (installedOAPPTemplatesResult != null && !installedOAPPTemplatesResult.IsError)
            {
                if (installedOAPPTemplatesResult.Result != null)
                    result.Result = true;
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonByMetaData. Reason: {installedOAPPTemplatesResult.Message}");

            return result;
        }

        public async Task<OASISResult<bool>> IsOAPPTemplateInstalledAsync(Guid avatarId, string OAPPTemplateName, int versionSequence, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<bool> result = new OASISResult<bool>();
            string errorMessage = "Error occured in OAPPTemplateManager.IsOAPPTemplateInstalledAsync. Reason: ";

            OASISResult<InstalledOAPPTemplate> installedOAPPTemplatesResult = await Data.LoadHolonByMetaDataAsync<InstalledOAPPTemplate>(new Dictionary<string, string>()
            {
                { "OAPPTemplateName", OAPPTemplateName},
                { "VersionSequence", versionSequence.ToString() },
                { "Active", "1" }

            }, MetaKeyValuePairMatchMode.All, HolonType.InstalledOAPPTemplate, true, true, 0, true, 0, false, HolonType.All, providerType);

            if (installedOAPPTemplatesResult != null && !installedOAPPTemplatesResult.IsError)
            {
                if (installedOAPPTemplatesResult.Result != null)
                    result.Result = true;
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonByMetaDataAsync. Reason: {installedOAPPTemplatesResult.Message}");

            return result;
        }

        public OASISResult<bool> IsOAPPTemplateInstalled(Guid avatarId, string OAPPTemplateName, int versionSequence, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<bool> result = new OASISResult<bool>();
            string errorMessage = "Error occured in OAPPTemplateManager.IsOAPPTemplateInstalled. Reason: ";

            OASISResult<InstalledOAPPTemplate> installedOAPPTemplatesResult = Data.LoadHolonByMetaData<InstalledOAPPTemplate>(new Dictionary<string, string>()
            {
                { "OAPPTemplateId", OAPPTemplateName.ToString() },
                { "VersionSequence", versionSequence.ToString() },
                { "Active", "1" }

            }, MetaKeyValuePairMatchMode.All, HolonType.InstalledOAPPTemplate, true, true, 0, true, false, HolonType.All, 0, providerType);

            if (installedOAPPTemplatesResult != null && !installedOAPPTemplatesResult.IsError)
            {
                if (installedOAPPTemplatesResult.Result != null)
                    result.Result = true;
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonByMetaData. Reason: {installedOAPPTemplatesResult.Message}");

            return result;
        }

        public async Task<OASISResult<bool>> IsOAPPTemplateInstalledAsync(Guid avatarId, string OAPPTemplateName, string version, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<bool> result = new OASISResult<bool>();
            string errorMessage = "Error occured in OAPPTemplateManager.IsOAPPTemplateInstalledAsync. Reason: ";

            OASISResult<InstalledOAPPTemplate> installedOAPPTemplatesResult = await Data.LoadHolonByMetaDataAsync<InstalledOAPPTemplate>(new Dictionary<string, string>()
            {
                { "OAPPTemplateName", OAPPTemplateName},
                { "Version", version.ToString() },
                { "Active", "1" }

            }, MetaKeyValuePairMatchMode.All, HolonType.InstalledOAPPTemplate, true, true, 0, true, 0, false, HolonType.All, providerType);

            if (installedOAPPTemplatesResult != null && !installedOAPPTemplatesResult.IsError)
            {
                if (installedOAPPTemplatesResult.Result != null)
                    result.Result = true;
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonByMetaDataAsync. Reason: {installedOAPPTemplatesResult.Message}");

            return result;
        }

        public OASISResult<bool> IsOAPPTemplateInstalled(Guid avatarId, string OAPPTemplateName, string version, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<bool> result = new OASISResult<bool>();
            string errorMessage = "Error occured in OAPPTemplateManager.IsOAPPTemplateInstalled. Reason: ";

            OASISResult<InstalledOAPPTemplate> installedOAPPTemplatesResult = Data.LoadHolonByMetaData<InstalledOAPPTemplate>(new Dictionary<string, string>()
            {
                { "OAPPTemplateId", OAPPTemplateName.ToString() },
                { "Version", version },
                { "Active", "1" }

            }, MetaKeyValuePairMatchMode.All, HolonType.InstalledOAPPTemplate, true, true, 0, true, false, HolonType.All, 0, providerType);

            if (installedOAPPTemplatesResult != null && !installedOAPPTemplatesResult.IsError)
            {
                if (installedOAPPTemplatesResult.Result != null)
                    result.Result = true;
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonByMetaData. Reason: {installedOAPPTemplatesResult.Message}");

            return result;
        }

        public async Task<OASISResult<IInstalledOAPPTemplate>> LoadInstalledOAPPTemplateAsync(Guid avatarId, Guid OAPPTemplateId, int versionSequence = 0, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IInstalledOAPPTemplate> result = new OASISResult<IInstalledOAPPTemplate>();
            string errorMessage = "Error occured in OAPPTemplateManager.LoadInstalledOAPPTemplateAsync. Reason: ";
            OASISResult<InstalledOAPPTemplate> installedOAPPTemplatesResult = await Data.LoadHolonByMetaDataAsync<InstalledOAPPTemplate>(new Dictionary<string, string>()
            {
                { "OAPPTemplateId", OAPPTemplateId.ToString() },
                { "VersionSequence", versionSequence.ToString() }

            }, MetaKeyValuePairMatchMode.All, HolonType.InstalledOAPPTemplate, true, true, 0, true, 0, false, HolonType.All, providerType);

            if (installedOAPPTemplatesResult != null && !installedOAPPTemplatesResult.IsError && installedOAPPTemplatesResult.Result != null)
                result.Result = installedOAPPTemplatesResult.Result;
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonByMetaDataAsync. Reason: {installedOAPPTemplatesResult.Message}");

            return result;
        }

        public OASISResult<IInstalledOAPPTemplate> LoadInstalledOAPPTemplate(Guid avatarId, Guid OAPPTemplateId, int versionSequence = 0, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IInstalledOAPPTemplate> result = new OASISResult<IInstalledOAPPTemplate>();
            string errorMessage = "Error occured in OAPPTemplateManager.LoadInstalledOAPPTemplateAsync. Reason: ";
            OASISResult<InstalledOAPPTemplate> installedOAPPTemplatesResult = Data.LoadHolonByMetaData<InstalledOAPPTemplate>(new Dictionary<string, string>()
            {
                { "OAPPTemplateId", OAPPTemplateId.ToString() },
                { "VersionSequence", versionSequence.ToString() }

            }, MetaKeyValuePairMatchMode.All, HolonType.OAPPTemplate, version: versionSequence, providerType: providerType);

            if (installedOAPPTemplatesResult != null && !installedOAPPTemplatesResult.IsError && installedOAPPTemplatesResult.Result != null)
                result.Result = installedOAPPTemplatesResult.Result;
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonByMetaDataAsync. Reason: {installedOAPPTemplatesResult.Message}");

            return result;
        }

        public async Task<OASISResult<IInstalledOAPPTemplate>> LoadInstalledOAPPTemplateAsync(Guid avatarId, string OAPPTemplateName, int versionSequence = 0, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IInstalledOAPPTemplate> result = new OASISResult<IInstalledOAPPTemplate>();
            string errorMessage = "Error occured in OAPPTemplateManager.LoadInstalledOAPPTemplateAsync. Reason: ";
            OASISResult<InstalledOAPPTemplate> installedOAPPTemplatesResult = await Data.LoadHolonByMetaDataAsync<InstalledOAPPTemplate>(new Dictionary<string, string>()
            {
                { "OAPPTemplateName", OAPPTemplateName },
                { "VersionSequence", versionSequence.ToString() }

            }, MetaKeyValuePairMatchMode.All, HolonType.InstalledOAPPTemplate, true, true, 0, true, 0, false, HolonType.All, providerType);

            if (installedOAPPTemplatesResult != null && !installedOAPPTemplatesResult.IsError && installedOAPPTemplatesResult.Result != null)
                result.Result = installedOAPPTemplatesResult.Result;
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonByMetaDataAsync. Reason: {installedOAPPTemplatesResult.Message}");

            return result;
        }

        public OASISResult<IInstalledOAPPTemplate> LoadInstalledOAPPTemplate(Guid avatarId, string OAPPTemplateName, int versionSequence = 0, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IInstalledOAPPTemplate> result = new OASISResult<IInstalledOAPPTemplate>();
            string errorMessage = "Error occured in OAPPTemplateManager.LoadInstalledOAPPTemplateAsync. Reason: ";
            OASISResult<InstalledOAPPTemplate> installedOAPPTemplatesResult = Data.LoadHolonByMetaData<InstalledOAPPTemplate>(new Dictionary<string, string>()
            {
                { "OAPPTemplateName", OAPPTemplateName },
                { "VersionSequence", versionSequence.ToString() }

            }, MetaKeyValuePairMatchMode.All, HolonType.OAPPTemplate, version: versionSequence, providerType: providerType);

            if (installedOAPPTemplatesResult != null && !installedOAPPTemplatesResult.IsError && installedOAPPTemplatesResult.Result != null)
                result.Result = installedOAPPTemplatesResult.Result;
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonByMetaDataAsync. Reason: {installedOAPPTemplatesResult.Message}");

            return result;
        }

        public async Task<OASISResult<IInstalledOAPPTemplate>> LoadInstalledOAPPTemplateAsync(Guid avatarId, Guid OAPPTemplateId, string version, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IInstalledOAPPTemplate> result = new OASISResult<IInstalledOAPPTemplate>();
            string errorMessage = "Error occured in OAPPTemplateManager.LoadInstalledOAPPTemplateAsync. Reason: ";
            OASISResult<InstalledOAPPTemplate> installedOAPPTemplatesResult = await Data.LoadHolonByMetaDataAsync<InstalledOAPPTemplate>(new Dictionary<string, string>()
            {
                { "OAPPTemplateId", OAPPTemplateId.ToString() },
                { "Version", version }

            }, MetaKeyValuePairMatchMode.All, HolonType.InstalledOAPPTemplate, true, true, 0, true, 0, false, HolonType.All, providerType);

            if (installedOAPPTemplatesResult != null && !installedOAPPTemplatesResult.IsError && installedOAPPTemplatesResult.Result != null)
                result.Result = installedOAPPTemplatesResult.Result;
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonByMetaDataAsync. Reason: {installedOAPPTemplatesResult.Message}");

            return result;
        }

        public OASISResult<IInstalledOAPPTemplate> LoadInstalledOAPPTemplate(Guid avatarId, Guid OAPPTemplateId, string version, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IInstalledOAPPTemplate> result = new OASISResult<IInstalledOAPPTemplate>();
            string errorMessage = "Error occured in OAPPTemplateManager.LoadInstalledOAPPTemplateAsync. Reason: ";
            OASISResult<InstalledOAPPTemplate> installedOAPPTemplatesResult = Data.LoadHolonByMetaData<InstalledOAPPTemplate>(new Dictionary<string, string>()
            {
                { "OAPPTemplateId", OAPPTemplateId.ToString() },
                { "Version", version }

            }, MetaKeyValuePairMatchMode.All, HolonType.OAPPTemplate, providerType: providerType);

            if (installedOAPPTemplatesResult != null && !installedOAPPTemplatesResult.IsError && installedOAPPTemplatesResult.Result != null)
                result.Result = installedOAPPTemplatesResult.Result;
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonByMetaDataAsync. Reason: {installedOAPPTemplatesResult.Message}");

            return result;
        }

        public async Task<OASISResult<IInstalledOAPPTemplate>> LoadInstalledOAPPTemplateAsync(Guid avatarId, string OAPPTemplateName, string version, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IInstalledOAPPTemplate> result = new OASISResult<IInstalledOAPPTemplate>();
            string errorMessage = "Error occured in OAPPTemplateManager.LoadInstalledOAPPTemplateAsync. Reason: ";
            OASISResult<InstalledOAPPTemplate> installedOAPPTemplatesResult = await Data.LoadHolonByMetaDataAsync<InstalledOAPPTemplate>(new Dictionary<string, string>()
            {
                { "OAPPTemplateName", OAPPTemplateName },
                { "Version", version }

            }, MetaKeyValuePairMatchMode.All, HolonType.InstalledOAPPTemplate, true, true, 0, true, 0, false, HolonType.All, providerType);

            if (installedOAPPTemplatesResult != null && !installedOAPPTemplatesResult.IsError && installedOAPPTemplatesResult.Result != null)
                result.Result = installedOAPPTemplatesResult.Result;
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonByMetaDataAsync. Reason: {installedOAPPTemplatesResult.Message}");

            return result;
        }

        public OASISResult<IInstalledOAPPTemplate> LoadInstalledOAPPTemplate(Guid avatarId, string OAPPTemplateName, string version, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IInstalledOAPPTemplate> result = new OASISResult<IInstalledOAPPTemplate>();
            string errorMessage = "Error occured in OAPPTemplateManager.LoadInstalledOAPPTemplateAsync. Reason: ";
            OASISResult<InstalledOAPPTemplate> installedOAPPTemplatesResult = Data.LoadHolonByMetaData<InstalledOAPPTemplate>(new Dictionary<string, string>()
            {
                { "OAPPTemplateName", OAPPTemplateName },
                { "Version", version }

            }, MetaKeyValuePairMatchMode.All, HolonType.OAPPTemplate, providerType: providerType);

            if (installedOAPPTemplatesResult != null && !installedOAPPTemplatesResult.IsError && installedOAPPTemplatesResult.Result != null)
                result.Result = installedOAPPTemplatesResult.Result;
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonByMetaDataAsync. Reason: {installedOAPPTemplatesResult.Message}");

            return result;
        }

        public async Task<OASISResult<IInstalledOAPPTemplate>> LoadInstalledOAPPTemplateAsync(Guid avatarId, Guid OAPPTemplateId, bool active, int versionSequence = 0, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IInstalledOAPPTemplate> result = new OASISResult<IInstalledOAPPTemplate>();
            string errorMessage = "Error occured in OAPPTemplateManager.LoadInstalledOAPPTemplateAsync. Reason: ";
            OASISResult<InstalledOAPPTemplate> installedOAPPTemplatesResult = await Data.LoadHolonByMetaDataAsync<InstalledOAPPTemplate>(new Dictionary<string, string>()
            {
                { "OAPPTemplateId", OAPPTemplateId.ToString() },
                { "VersionSequence", versionSequence.ToString() },
                { "Active", active == true ? "1" : "0" }

            }, MetaKeyValuePairMatchMode.All, HolonType.InstalledOAPPTemplate, true, true, 0, true, 0, false, HolonType.All, providerType);

            if (installedOAPPTemplatesResult != null && !installedOAPPTemplatesResult.IsError && installedOAPPTemplatesResult.Result != null)
                result.Result = installedOAPPTemplatesResult.Result;
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonByMetaDataAsync. Reason: {installedOAPPTemplatesResult.Message}");

            return result;
        }

        public OASISResult<IInstalledOAPPTemplate> LoadInstalledOAPPTemplate(Guid avatarId, Guid OAPPTemplateId, bool active, int versionSequence = 0, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IInstalledOAPPTemplate> result = new OASISResult<IInstalledOAPPTemplate>();
            string errorMessage = "Error occured in OAPPTemplateManager.LoadInstalledOAPPTemplate. Reason: ";
            OASISResult<InstalledOAPPTemplate> installedOAPPTemplatesResult = Data.LoadHolonByMetaData<InstalledOAPPTemplate>(new Dictionary<string, string>()
            {
                { "OAPPTemplateId", OAPPTemplateId.ToString() },
                { "VersionSequence", versionSequence.ToString() },
                { "Active", active == true ? "1" : "0" }

            }, MetaKeyValuePairMatchMode.All, HolonType.InstalledOAPPTemplate, true, true, 0, true, false, HolonType.All, 0, providerType);

            if (installedOAPPTemplatesResult != null && !installedOAPPTemplatesResult.IsError && installedOAPPTemplatesResult.Result != null)
                result.Result = installedOAPPTemplatesResult.Result;
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonByMetaData. Reason: {installedOAPPTemplatesResult.Message}");

            return result;
        }

        public async Task<OASISResult<IInstalledOAPPTemplate>> LoadInstalledOAPPTemplateAsync(Guid avatarId, string OAPPTemplateName, bool active, int versionSequence = 0, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IInstalledOAPPTemplate> result = new OASISResult<IInstalledOAPPTemplate>();
            string errorMessage = "Error occured in OAPPTemplateManager.LoadInstalledOAPPTemplateAsync. Reason: ";
            OASISResult<InstalledOAPPTemplate> installedOAPPTemplatesResult = await Data.LoadHolonByMetaDataAsync<InstalledOAPPTemplate>(new Dictionary<string, string>()
            {
                { "OAPPTemplateName", OAPPTemplateName },
                { "VersionSequence", versionSequence.ToString() },
                { "Active", active == true ? "1" : "0" }

            }, MetaKeyValuePairMatchMode.All, HolonType.InstalledOAPPTemplate, true, true, 0, true, 0, false, HolonType.All, providerType);
            if (installedOAPPTemplatesResult != null && !installedOAPPTemplatesResult.IsError && installedOAPPTemplatesResult.Result != null)
                result.Result = installedOAPPTemplatesResult.Result;
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonByMetaDataAsync. Reason: {installedOAPPTemplatesResult.Message}");

            return result;
        }

        public OASISResult<IInstalledOAPPTemplate> LoadInstalledOAPPTemplate(Guid avatarId, string OAPPTemplateName, bool active, int versionSequence = 0, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IInstalledOAPPTemplate> result = new OASISResult<IInstalledOAPPTemplate>();
            string errorMessage = "Error occured in OAPPTemplateManager.LoadInstalledOAPPTemplate. Reason: ";
            OASISResult<InstalledOAPPTemplate> installedOAPPTemplatesResult = Data.LoadHolonByMetaData<InstalledOAPPTemplate>(new Dictionary<string, string>()
            {
                { "OAPPTemplateId", OAPPTemplateName },
                { "VersionSequence", versionSequence.ToString() },
                { "Active", active == true ? "1" : "0" }

            }, MetaKeyValuePairMatchMode.All, HolonType.InstalledOAPPTemplate, true, true, 0, true, false, HolonType.All, 0, providerType);

            if (installedOAPPTemplatesResult != null && !installedOAPPTemplatesResult.IsError && installedOAPPTemplatesResult.Result != null)
                result.Result = installedOAPPTemplatesResult.Result;
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonByMetaData. Reason: {installedOAPPTemplatesResult.Message}");

            return result;
        }

        public async Task<OASISResult<IInstalledOAPPTemplate>> LoadInstalledOAPPTemplateAsync(Guid avatarId, Guid OAPPTemplateId, string version, bool active, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IInstalledOAPPTemplate> result = new OASISResult<IInstalledOAPPTemplate>();
            string errorMessage = "Error occured in OAPPTemplateManager.LoadInstalledOAPPTemplateAsync. Reason: ";
            OASISResult<InstalledOAPPTemplate> installedOAPPTemplatesResult = await Data.LoadHolonByMetaDataAsync<InstalledOAPPTemplate>(new Dictionary<string, string>()
            {
                { "OAPPTemplateId", OAPPTemplateId.ToString() },
                { "Version", version},
                { "Active", active == true ? "1" : "0" }

            }, MetaKeyValuePairMatchMode.All, HolonType.InstalledOAPPTemplate, true, true, 0, true, 0, false, HolonType.All, providerType);

            if (installedOAPPTemplatesResult != null && !installedOAPPTemplatesResult.IsError && installedOAPPTemplatesResult.Result != null)
                result.Result = installedOAPPTemplatesResult.Result;
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonByMetaDataAsync. Reason: {installedOAPPTemplatesResult.Message}");

            return result;
        }

        public OASISResult<IInstalledOAPPTemplate> LoadInstalledOAPPTemplate(Guid avatarId, Guid OAPPTemplateId, string version, bool active, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IInstalledOAPPTemplate> result = new OASISResult<IInstalledOAPPTemplate>();
            string errorMessage = "Error occured in OAPPTemplateManager.LoadInstalledOAPPTemplate. Reason: ";
            OASISResult<InstalledOAPPTemplate> installedOAPPTemplatesResult = Data.LoadHolonByMetaData<InstalledOAPPTemplate>(new Dictionary<string, string>()
            {
                { "OAPPTemplateId", OAPPTemplateId.ToString() },
                { "Version", version },
                 { "Active", active == true ? "1" : "0" }

            }, MetaKeyValuePairMatchMode.All, HolonType.InstalledOAPPTemplate, true, true, 0, true, false, HolonType.All, 0, providerType);

            if (installedOAPPTemplatesResult != null && !installedOAPPTemplatesResult.IsError && installedOAPPTemplatesResult.Result != null)
                result.Result = installedOAPPTemplatesResult.Result;
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonByMetaData. Reason: {installedOAPPTemplatesResult.Message}");

            return result;
        }

        public async Task<OASISResult<IInstalledOAPPTemplate>> LoadInstalledOAPPTemplateAsync(Guid avatarId, string OAPPTemplateName, string version, bool active, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IInstalledOAPPTemplate> result = new OASISResult<IInstalledOAPPTemplate>();
            string errorMessage = "Error occured in OAPPTemplateManager.LoadInstalledOAPPTemplateAsync. Reason: ";
            OASISResult<InstalledOAPPTemplate> installedOAPPTemplatesResult = await Data.LoadHolonByMetaDataAsync<InstalledOAPPTemplate>(new Dictionary<string, string>()
            {
                { "OAPPTemplateName", OAPPTemplateName },
                { "Version", version },
                { "Active", active == true ? "1" : "0" }

            }, MetaKeyValuePairMatchMode.All, HolonType.InstalledOAPPTemplate, true, true, 0, true, 0, false, HolonType.All, providerType);
            
            if (installedOAPPTemplatesResult != null && !installedOAPPTemplatesResult.IsError && installedOAPPTemplatesResult.Result != null)
                result.Result = installedOAPPTemplatesResult.Result;
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonByMetaDataAsync. Reason: {installedOAPPTemplatesResult.Message}");

            return result;
        }

        public OASISResult<IInstalledOAPPTemplate> LoadInstalledOAPPTemplate(Guid avatarId, string OAPPTemplateName, string version, bool active, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IInstalledOAPPTemplate> result = new OASISResult<IInstalledOAPPTemplate>();
            string errorMessage = "Error occured in OAPPTemplateManager.LoadInstalledOAPPTemplate. Reason: ";
            OASISResult<InstalledOAPPTemplate> installedOAPPTemplatesResult = Data.LoadHolonByMetaData<InstalledOAPPTemplate>(new Dictionary<string, string>()
            {
                { "OAPPTemplateName", OAPPTemplateName },
                { "Version", version },
                { "Active", active == true ? "1" : "0" }

            }, MetaKeyValuePairMatchMode.All, HolonType.InstalledOAPPTemplate, true, true, 0, true, false, HolonType.All, 0, providerType);

            if (installedOAPPTemplatesResult != null && !installedOAPPTemplatesResult.IsError && installedOAPPTemplatesResult.Result != null)
                result.Result = installedOAPPTemplatesResult.Result;
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonByMetaData. Reason: {installedOAPPTemplatesResult.Message}");

            return result;
        }

        public OASISResult<IInstalledOAPPTemplate> OpenOAPPTemplateFolder(Guid avatarId, IInstalledOAPPTemplate OAPPTemplate)
        {
            OASISResult<IInstalledOAPPTemplate> result = new OASISResult<IInstalledOAPPTemplate>();
            string errorMessage = "An error occured in OAPPTemplateManager.OpenOAPPTemplateFolder. Reason:";

            if (OAPPTemplate != null)
            {
                try
                {
                    if (!string.IsNullOrEmpty(OAPPTemplate.InstalledPath))
                        Process.Start("explorer.exe", OAPPTemplate.InstalledPath);

                    //else if (OAPPTemplate.DownloadedOAPPTemplate != null && !string.IsNullOrEmpty(OAPPTemplate.DownloadedOAPPTemplate.DownloadedPath))
                    else if (!string.IsNullOrEmpty(OAPPTemplate.DownloadedPath))
                        Process.Start("explorer.exe", new FileInfo(OAPPTemplate.DownloadedPath).DirectoryName);
                }
                catch (Exception e)
                {
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured attempting to open the folder {result.Result.InstalledPath}. Reason: {e}");
                }
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} The OAPP Template is null!");

            return result;
        }

        public async Task<OASISResult<IInstalledOAPPTemplate>> OpenOAPPTemplateFolderAsync(Guid avatarId, Guid OAPPTemplateId, int versionSequence, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IInstalledOAPPTemplate> result = new OASISResult<IInstalledOAPPTemplate>();
            string errorMessage = "An error occured in OAPPTemplateManager.OpenOAPPTemplateFolderAsync. Reason:";
            result = await LoadInstalledOAPPTemplateAsync(avatarId, OAPPTemplateId, versionSequence);

            if (result != null && !result.IsError && result.Result != null)
                OpenOAPPTemplateFolder(avatarId, result.Result);
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the OAPP Template with the LoadInstalledOAPPTemplateAsync method, reason: {result.Message}");

            return result;
        }

        public OASISResult<IInstalledOAPPTemplate> OpenOAPPTemplateFolder(Guid avatarId, Guid OAPPTemplateId, int versionSequence, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IInstalledOAPPTemplate> result = new OASISResult<IInstalledOAPPTemplate>();
            string errorMessage = "An error occured in OAPPTemplateManager.OpenOAPPTemplateFolder. Reason:";
            result = LoadInstalledOAPPTemplate(avatarId, OAPPTemplateId, versionSequence);

            if (result != null && !result.IsError && result.Result != null)
                OpenOAPPTemplateFolder(avatarId, result.Result);
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the OAPP Template with the LoadInstalledOAPPTemplate method, reason: {result.Message}");

            return result;
        }

        public async Task<OASISResult<IInstalledOAPPTemplate>> OpenOAPPTemplateFolderAsync(Guid avatarId, Guid OAPPTemplateId, string version, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IInstalledOAPPTemplate> result = new OASISResult<IInstalledOAPPTemplate>();
            string errorMessage = "An error occured in OAPPTemplateManager.OpenOAPPTemplateFolderAsync. Reason:";
            result = await LoadInstalledOAPPTemplateAsync(avatarId, OAPPTemplateId, version);

            if (result != null && !result.IsError && result.Result != null)
                OpenOAPPTemplateFolder(avatarId, result.Result);
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the OAPP Template with the LoadInstalledOAPPTemplateAsync method, reason: {result.Message}");

            return result;
        }

        public OASISResult<IInstalledOAPPTemplate> OpenOAPPTemplateFolder(Guid avatarId, Guid OAPPTemplateId, string version, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IInstalledOAPPTemplate> result = new OASISResult<IInstalledOAPPTemplate>();
            string errorMessage = "An error occured in OAPPTemplateManager.OpenOAPPTemplateFolder. Reason:";
            result = LoadInstalledOAPPTemplate(avatarId, OAPPTemplateId, version);

            if (result != null && !result.IsError && result.Result != null)
                OpenOAPPTemplateFolder(avatarId, result.Result);
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

                if (!Directory.Exists(fullPathToOAPPTemplate))
                    Directory.CreateDirectory(fullPathToOAPPTemplate);

                await File.WriteAllTextAsync(Path.Combine(fullPathToOAPPTemplate, "OAPPTemplateDNA.json"), JsonSerializer.Serialize((OAPPTemplateDNA)OAPPTemplateDNA, options));
                result.Result = true;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"An error occured writing the OAPP Template DNA in WriteOAPPTemplateDNAAsync: Reason: {ex.Message}");
            }

            return result;
        }

        public OASISResult<bool> WriteOAPPTemplateDNA(IOAPPTemplateDNA OAPPTemplateDNA, string fullPathToOAPPTemplate)
        {
            OASISResult<bool> result = new OASISResult<bool>();

            try
            {
                if (!Directory.Exists(fullPathToOAPPTemplate))
                    Directory.CreateDirectory(fullPathToOAPPTemplate);

                File.WriteAllText(Path.Combine(fullPathToOAPPTemplate, "OAPPTemplateDNA.json"), JsonSerializer.Serialize((OAPPTemplateDNA)OAPPTemplateDNA));
                result.Result = true;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"An error occured writing the OAPP Template DNA in WriteOAPPTemplateDNA: Reason: {ex.Message}");
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
                OASISErrorHandling.HandleError(ref result, $"An error occured reading the OAPP Template DNA in ReadOAPPTemplateDNAAsync: Reason: {ex.Message}");
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
                OASISErrorHandling.HandleError(ref result, $"An error occured reading the OAPP Template DNA in ReadOAPPTemplateDNA: Reason: {ex.Message}");
            }

            return result;
        }

        private async Task<OASISResult<IOAPPTemplate>> UpdateNumberOfVersionCountsAsync(OASISResult<IOAPPTemplate> result, Guid avatarId, string errorMessage, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<IOAPPTemplate>> templatesResult = await LoadOAPPTemplateVersionsAsync(result.Result.OAPPTemplateDNA.Id, providerType);

            if (templatesResult != null && templatesResult.Result != null && !templatesResult.IsError)
            {
                foreach (IOAPPTemplate template in templatesResult.Result)
                {
                    template.OAPPTemplateDNA.NumberOfVersions = result.Result.OAPPTemplateDNA.NumberOfVersions;
                    OASISResult<IOAPPTemplate> templateSaveResult = await SaveOAPPTemplateAsync(template, avatarId, providerType);

                    if (!(templateSaveResult != null && templateSaveResult.Result != null && !templateSaveResult.IsError))
                        OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} Error occured updating the NumberOfVersions for OAPP Template with Id {template.Id} for provider {Enum.GetName(typeof(ProviderType), providerType)}. Reason: {templateSaveResult.Message}");
                }

                //OAPPTemplate.OAPPTemplateDNA.NumberOfVersions = numberOfVersions;
                //installedOAPPTemplate.OAPPTemplateDNA.TotalInstalls = totalInstalls;
            }
            else
                OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} Error occured updating the total installs for all OAPP Template versions caused by an error in LoadOAPPTemplateVersionsAsync. Reason: {templatesResult.Message}");


            OASISResult<IEnumerable<IInstalledOAPPTemplate>> installedTemplatesResult = await ListInstalledOAPPTemplatesAsync(avatarId, providerType);

            if (installedTemplatesResult != null && installedTemplatesResult.Result != null && !installedTemplatesResult.IsError)
            {
                foreach (IInstalledOAPPTemplate template in installedTemplatesResult.Result)
                {
                    template.OAPPTemplateDNA.NumberOfVersions = result.Result.OAPPTemplateDNA.NumberOfVersions;
                    OASISResult<IOAPPTemplate> templateSaveResult = await SaveOAPPTemplateAsync(template, avatarId, providerType);

                    if (!(templateSaveResult != null && templateSaveResult.Result != null && !templateSaveResult.IsError))
                        OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} Error occured updating the NumberOfVersions for Installed OAPP Template with Id {template.Id} for provider {Enum.GetName(typeof(ProviderType), providerType)}. Reason: {templateSaveResult.Message}");
                }
            }
            else
                OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} Error occured updating the NumberOfVersions for all Installed OAPP Template versions caused by an error in ListInstalledOAPPTemplatesAsync. Reason: {templatesResult.Message}");

            return result;
        }

        private async Task<OASISResult<IDownloadedOAPPTemplate>> UpdateDownloadCountsAsync(DownloadedOAPPTemplate downloadedOAPPTemplate, IOAPPTemplateDNA OAPPTemplateDNA, Guid avatarId, OASISResult<IDownloadedOAPPTemplate> result, string errorMessage, ProviderType providerType = ProviderType.Default)
        {
            int totalDownloads = 0;
            OASISResult<IEnumerable<IOAPPTemplate>> templatesResult = await LoadOAPPTemplateVersionsAsync(OAPPTemplateDNA.Id, providerType);

            if (templatesResult != null && templatesResult.Result != null && !templatesResult.IsError)
            {
                //Update total installs for all versions.
                foreach (IOAPPTemplate template in templatesResult.Result)
                    totalDownloads += template.OAPPTemplateDNA.Installs;

                //Need to add this download (because its not saved yet).
                totalDownloads++;

                foreach (IOAPPTemplate template in templatesResult.Result)
                {
                    template.OAPPTemplateDNA.TotalDownloads = totalDownloads;
                    OASISResult<IOAPPTemplate> templateSaveResult = await SaveOAPPTemplateAsync(template, avatarId, providerType);

                    if (!(templateSaveResult != null && templateSaveResult.Result != null && !templateSaveResult.IsError))
                        OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} Error occured updating the TotalDownloads for OAPP Template with Id {template.Id} for provider {Enum.GetName(typeof(ProviderType), providerType)}. Reason: {templateSaveResult.Message}");
                }

                OAPPTemplateDNA.TotalDownloads = totalDownloads;
                downloadedOAPPTemplate.OAPPTemplateDNA.TotalDownloads = totalDownloads;
            }
            else
                OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} Error occured updating the total downloads for all OAPP Template versions caused by an error in LoadOAPPTemplateVersionsAsync. Reason: {templatesResult.Message}");


            OASISResult<IEnumerable<IInstalledOAPPTemplate>> installedTemplatesResult = await ListInstalledOAPPTemplatesAsync(avatarId, providerType);

            if (installedTemplatesResult != null && installedTemplatesResult.Result != null && !installedTemplatesResult.IsError)
            {
                foreach (IInstalledOAPPTemplate template in installedTemplatesResult.Result)
                {
                    template.OAPPTemplateDNA.TotalDownloads = totalDownloads;
                    OASISResult<IOAPPTemplate> templateSaveResult = await SaveOAPPTemplateAsync(template, avatarId, providerType);

                    if (!(templateSaveResult != null && templateSaveResult.Result != null && !templateSaveResult.IsError))
                        OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} Error occured updating the TotalDownloads for Installed OAPP Template with Id {template.Id} for provider {Enum.GetName(typeof(ProviderType), providerType)}. Reason: {templateSaveResult.Message}");
                }
            }
            else
                OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} Error occured updating the total downloads for all Installed OAPP Template versions caused by an error in ListInstalledOAPPTemplatesAsync. Reason: {templatesResult.Message}");

            return result;
        }

        private async Task<OASISResult<IInstalledOAPPTemplate>> UpdateInstallCountsAsync(InstalledOAPPTemplate installedOAPPTemplate, IOAPPTemplateDNA OAPPTemplateDNA, Guid avatarId, OASISResult<IInstalledOAPPTemplate> result, string errorMessage, ProviderType providerType = ProviderType.Default)
        {
            int totalInstalls = 0;
            OASISResult<IEnumerable<IOAPPTemplate>> templatesResult = await LoadOAPPTemplateVersionsAsync(OAPPTemplateDNA.Id, providerType);

            if (templatesResult != null && templatesResult.Result != null && !templatesResult.IsError)
            {
                //Update total installs for all versions.
                foreach (IOAPPTemplate template in templatesResult.Result)
                    totalInstalls += template.OAPPTemplateDNA.Installs;

                //Need to add this install (because its not saved yet).
                totalInstalls++;

                foreach (IOAPPTemplate template in templatesResult.Result)
                {
                    template.OAPPTemplateDNA.TotalInstalls = totalInstalls;
                    OASISResult<IOAPPTemplate> templateSaveResult = await SaveOAPPTemplateAsync(template, avatarId, providerType);

                    if (!(templateSaveResult != null && templateSaveResult.Result != null && !templateSaveResult.IsError))
                        OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} Error occured updating the TotalInstalls for OAPP Template with Id {template.Id} for provider {Enum.GetName(typeof(ProviderType), providerType)}. Reason: {templateSaveResult.Message}");
                }

                OAPPTemplateDNA.TotalInstalls = totalInstalls;
                installedOAPPTemplate.OAPPTemplateDNA.TotalInstalls = totalInstalls;
            }
            else
                OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} Error occured updating the total installs for all OAPP Template versions caused by an error in LoadOAPPTemplateVersionsAsync. Reason: {templatesResult.Message}");


            OASISResult<IEnumerable<IInstalledOAPPTemplate>> installedTemplatesResult = await ListInstalledOAPPTemplatesAsync(avatarId, providerType);

            if (installedTemplatesResult != null && installedTemplatesResult.Result != null && !installedTemplatesResult.IsError)
            {
                foreach (IInstalledOAPPTemplate template in installedTemplatesResult.Result)
                {
                    template.OAPPTemplateDNA.TotalInstalls = totalInstalls;
                    OASISResult<IOAPPTemplate> templateSaveResult = await SaveOAPPTemplateAsync(template, avatarId, providerType);

                    if (!(templateSaveResult != null && templateSaveResult.Result != null && !templateSaveResult.IsError))
                        OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} Error occured updating the TotalInstalls for Installed OAPP Template with Id {template.Id} for provider {Enum.GetName(typeof(ProviderType), providerType)}. Reason: {templateSaveResult.Message}");
                }
            }
            else
                OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} Error occured updating the total installs for all Installed OAPP Template versions caused by an error in ListInstalledOAPPTemplatesAsync. Reason: {templatesResult.Message}");

            return result;
        }

        private async Task<OASISResult<IInstalledOAPPTemplate>> UninstallOAPPTemplateAsync(OASISResult<InstalledOAPPTemplate> installedOAPPTemplateResult, Guid avatarId, string errorMessage, ProviderType providerType)
        {
            OASISResult<IInstalledOAPPTemplate> result = new OASISResult<IInstalledOAPPTemplate>();

            if (installedOAPPTemplateResult != null && !installedOAPPTemplateResult.IsError && installedOAPPTemplateResult.Result != null)
                result = await UninstallOAPPTemplateAsync(installedOAPPTemplateResult.Result, avatarId, errorMessage, providerType); 
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonByMetaDataAsync. Reason: {installedOAPPTemplateResult.Message}");

            return result;
        }

        private OASISResult<IInstalledOAPPTemplate> UninstallOAPPTemplate(OASISResult<InstalledOAPPTemplate> installedOAPPTemplateResult, Guid avatarId, string errorMessage, ProviderType providerType)
        {
            OASISResult<IInstalledOAPPTemplate> result = new OASISResult<IInstalledOAPPTemplate>();

            if (installedOAPPTemplateResult != null && !installedOAPPTemplateResult.IsError && installedOAPPTemplateResult.Result != null)
                result = UninstallOAPPTemplate(installedOAPPTemplateResult.Result, avatarId, errorMessage, providerType);
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonByMetaData. Reason: {installedOAPPTemplateResult.Message}");

            return result;
        }

        private OASISResult<IEnumerable<IOAPPTemplate>> FilterResultsForVersion(Guid avatarId, OASISResult<IEnumerable<OAPPTemplate>> results, bool showAllVersions = false, int version = 0)
        {
            OASISResult<IEnumerable<IOAPPTemplate>> result = new OASISResult<IEnumerable<IOAPPTemplate>>();
            List<IOAPPTemplate> templates = new List<IOAPPTemplate>();

            if (!showAllVersions)
            {
                if (results.Result != null && !result.IsError)
                {
                    if (version == 0) //latest version
                    {
                        Dictionary<string, IOAPPTemplate> latestVersions = new Dictionary<string, IOAPPTemplate>();
                        string metaDataId = "";
                        int latestVersion = 0;
                        int currentVersion = 0;

                        foreach (IOAPPTemplate oappTemplate in results.Result)
                        {
                            if (oappTemplate.MetaData != null && oappTemplate.MetaData.ContainsKey("OAPPTemplateId") && oappTemplate.MetaData["OAPPTemplateId"] != null)
                                metaDataId = oappTemplate.MetaData["OAPPTemplateId"].ToString();

                            latestVersion = latestVersions.ContainsKey(metaDataId) ? Convert.ToInt32(latestVersions[metaDataId].OAPPTemplateDNA.Version.Replace(".", "")) : 0;
                            currentVersion = Convert.ToInt32(oappTemplate.OAPPTemplateDNA.Version.Replace(".", ""));

                            if ((latestVersions.ContainsKey(metaDataId) &&
                                currentVersion > latestVersion)
                                //oappTemplate.OAPPTemplateDNA.CreatedOn > latestVersions[metaDataId].OAPPTemplateDNA.CreatedOn)
                                || !latestVersions.ContainsKey(metaDataId))
                                latestVersions[metaDataId] = oappTemplate;
                        }

                        result.Result = latestVersions.Values.ToList();
                    }
                    else
                    {
                        List<IOAPPTemplate> filteredList = new List<IOAPPTemplate>();

                        foreach (IOAPPTemplate oappTemplate in results.Result)
                        {
                            if (oappTemplate.MetaData["VersionSequence"].ToString() == version.ToString())
                                filteredList.Add(oappTemplate);
                        }

                        result.Result = filteredList;
                    }
                }
            }
            else
                result.Result = results.Result;

            //Filter out any templates that are not created by the avatar or published on STARNET.
            foreach (IOAPPTemplate oappTemplate in result.Result)
            {
                if (oappTemplate.OAPPTemplateDNA.CreatedByAvatarId == avatarId)
                    templates.Add(oappTemplate);
                
                else if (oappTemplate.OAPPTemplateDNA.PublishedOn != DateTime.MinValue)
                    templates.Add(oappTemplate);
            }

            result.Result = templates;
            result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(results, result);
            return result;
        }

        private OASISResult<bool> ValidateVersion(string dnaVersion, string storedVersion, string fullPathToOAPPTemplateFolder, bool firstPublish, bool edit)
        {
            OASISResult<bool> result = new OASISResult<bool>();
            int dnaVersionInt = 0;
            int stotedVersionInt = 0;

            if (!firstPublish)
            {
                if (edit && dnaVersion != storedVersion)
                {
                    OASISErrorHandling.HandleError(ref result, $"The version in the OAPPTemplateDNA ({dnaVersion}) is not the same as the version you are attempting to edit ({storedVersion}). They must be the same if you wish to upload new files for version {storedVersion}. Please edit the OAPPTemplateDNA.json file found in the root of your OAPPTemplate folder ({fullPathToOAPPTemplateFolder}).");
                    return result;
                }
                else
                {
                    if (!StringHelper.IsValidVersion(dnaVersion))
                    {
                        OASISErrorHandling.HandleError(ref result, $"The version in the OAPPTemplateDNA ({dnaVersion}) is not valid! Please make sure you enter a valid version in the form of MM.mm.rr (Major.Minor.Revision) in the OAPPTemplateDNA.json file found in the root of your OAPPTemplate folder ({fullPathToOAPPTemplateFolder}).");
                        return result;
                    }

                    if (dnaVersion == storedVersion)
                    {
                        OASISErrorHandling.HandleError(ref result, $"The version in the OAPPTemplateDNA ({dnaVersion}) is the same as the previous version ({storedVersion}). Please make sure you increment the version in the OAPPTemplateDNA.json file found in the root of your OAPPTemplate folder ({fullPathToOAPPTemplateFolder}).");
                        return result;
                    }

                    if (!int.TryParse(dnaVersion.Replace(".", ""), out dnaVersionInt))
                    {
                        OASISErrorHandling.HandleError(ref result, $"The version in the OAPPTemplateDNA ({dnaVersion}) is not valid! Please make sure you enter a valid version in the form of MM.mm.rr (Major.Minor.Revision) in the OAPPTemplateDNA.json file found in the root of your OAPPTemplate folder ({fullPathToOAPPTemplateFolder}).");
                        return result;
                    }

                    //Should hopefully never occur! ;-)
                    if (!int.TryParse(storedVersion.Replace(".", ""), out stotedVersionInt))
                        OASISErrorHandling.HandleWarning(ref result, $"The version stored in the OASIS ({storedVersion}) is not valid!");

                    if (dnaVersionInt <= stotedVersionInt)
                    {
                        OASISErrorHandling.HandleError(ref result, $"The version in the OAPPTemplateDNA ({dnaVersion}) is less than the previous version ({storedVersion}). Please make sure you increment the version in the OAPPTemplateDNA.json file found in the root of your OAPPTemplate folder.");
                        return result;
                    }
                }
            }

            result.Result = true;
            return result;
        }

        private OASISResult<IEnumerable<IOAPPTemplate>> ProcessResults(OASISResult<IEnumerable<OAPPTemplate>> loadResult, OASISResult<IEnumerable<IOAPPTemplate>> result)
        {
            if (loadResult != null && loadResult.Result != null && !loadResult.IsError && loadResult.Result.Count() > 0)
            {
                List<IOAPPTemplate> oappTemplates = new List<IOAPPTemplate>();

                foreach (IOAPPTemplate template in loadResult.Result)
                    oappTemplates.Add(template);

                result.Result = oappTemplates;
            }

            OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(loadResult, result);
            return result;
        }

        private OASISResult<IOAPPTemplate> ProcessResult(OASISResult<OAPPTemplate> loadResult, OASISResult<IOAPPTemplate> result)
        {
            result.Result = loadResult.Result;
            OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(loadResult, result);
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
                    {
                        if (_fileLength > 0)
                        {
                            _progress = Convert.ToInt32(((double)progress.BytesSent / (double)_fileLength) * 100);
                            OnOAPPTemplateUploadStatusChanged?.Invoke(this, new OAPPTemplateUploadProgressEventArgs() { Progress = _progress, Status = Enums.OAPPTemplateUploadStatus.Uploading });
                        }
                    }
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
                    {
                        if (_fileLength > 0)
                        {
                            _progress = Convert.ToInt32(((double)progress.BytesDownloaded / (double)_fileLength) * 100);
                            // _progress = Convert.ToInt32(_fileLength / progress.BytesDownloaded);
                            OnOAPPTemplateDownloadStatusChanged?.Invoke(this, new OAPPTemplateDownloadProgressEventArgs() { Progress = _progress, Status = Enums.OAPPTemplateDownloadStatus.Downloading });
                        }
                    }
                    break;

                case Google.Apis.Download.DownloadStatus.Failed:
                    OnOAPPTemplateDownloadStatusChanged?.Invoke(this, new OAPPTemplateDownloadProgressEventArgs() { Progress = _progress, Status = Enums.OAPPTemplateDownloadStatus.Error, ErrorMessage = progress.Exception.ToString() });
                    break;
            }
        }
    }
}