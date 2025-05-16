using System;
using System.IO;
using System.Text;
using System.Linq;
using PinataNET;
using PinataNET.Models;
using Pinata.Client;
using System.Net.Http;
using System.Net.Mime;
using SharpCompress.Common;
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
using Nethereum.Web3.Accounts;

namespace NextGenSoftware.OASIS.API.ONode.Core.Managers
{
    public class OAPPTemplateManager : OAPPSystemManagerBase//, IOAPPTemplateManager
    {
        private bool _init = false;
        private int _progress = 0;
        private long _fileLength = 0;
        private const string GOOGLE_CLOUD_BUCKET_NAME = "oasis_oapptemplates";

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

        public void Init()
        {
            base.OAPPSystemHolonUIName = "OAPP Template";
            base.OAPPSystemHolonIdName = "OAPPTemplateId";
            base.OAPPSystemHolonNameName = "OAPPTemplateName";
            base.OAPPSystemHolonTypeName = "OAPPTemplateType";
            base.OAPPSystemHolonFileExtention = "oapptemplate";
            base.OAPPSystemHolonGoogleBucket = "oasis_oapptemplates";

            _init = true;
        }

        public async Task<OASISResult<IOAPPTemplate>> CreateOAPPTemplateAsync(string name, string description, OAPPTemplateType OAPPTemplateType, Guid avatarId, string fullPathToOAPPTemplate, ProviderType providerType = ProviderType.Default)
        {
            if (_init)
                Init(); 

            OASISResult<IOAPPTemplate> result = new OASISResult<IOAPPTemplate>();
            OASISResult<OAPPTemplate> createOAPPTemplateResult = await base.CreateAsync<OAPPTemplate>(name, description, OAPPTemplateType, avatarId, fullPathToOAPPTemplate, providerType);
            result.Result = createOAPPTemplateResult.Result;
            OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(createOAPPTemplateResult, result);
            return result;
        }

        public OASISResult<IOAPPTemplate> CreateOAPPTemplate(string name, string description, OAPPTemplateType OAPPTemplateType, Guid avatarId, string fullPathToOAPPTemplate, ProviderType providerType = ProviderType.Default)
        {
            if (_init)
                Init();

            OASISResult<IOAPPTemplate> result = new OASISResult<IOAPPTemplate>();
            OASISResult<OAPPTemplate> createOAPPTemplateResult = base.Create<OAPPTemplate>(name, description, OAPPTemplateType, avatarId, fullPathToOAPPTemplate, providerType);
            result.Result = createOAPPTemplateResult.Result;
            OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(createOAPPTemplateResult, result);
            return result;
        }

        #region COSMICManagerBase
        public async Task<OASISResult<IOAPPTemplate>> SaveOAPPTemplateAsync(IOAPPTemplate oappTemplate, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IOAPPTemplate> result = new OASISResult<IOAPPTemplate>();

            if (!Directory.Exists(oappTemplate.OAPPTemplateDNA.OAPPTemplatePath))
                Directory.CreateDirectory(oappTemplate.OAPPTemplateDNA.OAPPTemplatePath);

            oappTemplate.MetaData["OAPPTemplateDNAJSON"] = JsonSerializer.Serialize(oappTemplate.OAPPTemplateDNA);

            OASISResult<OAPPTemplate> saveResult = await SaveHolonAsync<OAPPTemplate>(oappTemplate, avatarId, providerType, "OAPPTemplateManager.SaveOAPPTemplateAsync");
            result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(saveResult, result);
            result.Result = saveResult.Result;
            return result;
        }

        public OASISResult<IOAPPTemplate> SaveOAPPTemplate(IOAPPTemplate oappTemplate, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IOAPPTemplate> result = new OASISResult<IOAPPTemplate>();

            if (!Directory.Exists(oappTemplate.OAPPTemplateDNA.OAPPTemplatePath))
                Directory.CreateDirectory(oappTemplate.OAPPTemplateDNA.OAPPTemplatePath);

            OASISResult<OAPPTemplate> saveResult = SaveHolon<OAPPTemplate>(oappTemplate, avatarId, providerType, "OAPPTemplateManager.SaveOAPPTemplate");
            result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(saveResult, result);
            result.Result = saveResult.Result;
            return result;
        }

        //public async Task<OASISResult<IOAPPTemplate>> LoadOAPPTemplateAsync(Guid OAPPTemplateId, bool showAllVersions = false, int version = 0, ProviderType providerType = ProviderType.Default)
        public async Task<OASISResult<IOAPPTemplate>> LoadOAPPTemplateAsync(Guid OAPPTemplateId, Guid avatarId, int version = 0, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IOAPPTemplate> result = new OASISResult<IOAPPTemplate>();
            //OASISResult<OAPPTemplate> loadResult = await LoadHolonAsync<OAPPTemplate>(OAPPTemplateId, providerType, "OAPPTemplateManager.LoadOAPPTemplateAsync");
            OASISResult<IEnumerable<OAPPTemplate>> loadResult = await Data.LoadHolonsByMetaDataAsync<OAPPTemplate>("OAPPTemplateId", OAPPTemplateId.ToString(), HolonType.OAPPTemplate, true, true, 0, true, false, 0, HolonType.All, 0, providerType);
            OASISResult<IEnumerable<IOAPPTemplate>> filterdResult = FilterResultsForVersion(avatarId, loadResult, false, version);

            if (filterdResult != null && filterdResult.Result != null && !filterdResult.IsError)
                result.Result = filterdResult.Result.FirstOrDefault();
            else
                OASISErrorHandling.HandleError(ref result, $"Error occured in LoadOAPPTemplateAsync loading the OAPP Template with Id {OAPPTemplateId} from the {Enum.GetName(typeof(ProviderType), providerType)} provider. Reason: {filterdResult.Message}");


            //OASISResult<OAPPTemplate> OAPPTemplatesResult = Data.LoadHolonByMetaData<OAPPTemplate>(new Dictionary<string, string>()
            //{
            //    { "OAPPTemplateId", OAPPTemplateId.ToString() },
            //    { "LatestVersion", "1" },
            //    { "Active", "1" }

            //}, MetaKeyValuePairMatchMode.All, HolonType.InstalledOAPPTemplate, true, true, 0, true, false, HolonType.All, 0, providerType);

            //if (OAPPTemplatesResult != null && !OAPPTemplatesResult.IsError && OAPPTemplatesResult.Result != null)
            //    result.Result = OAPPTemplatesResult.Result;
            //else
            //    OASISErrorHandling.HandleError(ref result, $"Error occured in LoadOAPPTemplateAsync loading the OAPP Template with Id {OAPPTemplateId} from the {Enum.GetName(typeof(ProviderType), providerType)} provider. Reason: {filterdResult.Message}");

            return result;
        }

        //public OASISResult<IOAPPTemplate> LoadOAPPTemplate(Guid OAPPTemplateId, bool showAllVersions = false, int version = 0, ProviderType providerType = ProviderType.Default)
        public OASISResult<IOAPPTemplate> LoadOAPPTemplate(Guid OAPPTemplateId, Guid avatarId, int version = 0, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IOAPPTemplate> result = new OASISResult<IOAPPTemplate>();
            //OASISResult<OAPPTemplate> loadResult = LoadHolon<OAPPTemplate>(OAPPTemplateId, providerType, "OAPPTemplateManager.LoadOAPPTemplate");

            OASISResult<IEnumerable<OAPPTemplate>> loadResult = Data.LoadHolonsByMetaData<OAPPTemplate>("OAPPTemplateId", OAPPTemplateId.ToString(), HolonType.OAPPTemplate, true, true, 0, true, false, 0, HolonType.All, 0, providerType);
            OASISResult<IEnumerable<IOAPPTemplate>> filterdResult = FilterResultsForVersion(avatarId, loadResult, false, version);

            if (filterdResult != null && filterdResult.Result != null && !filterdResult.IsError)
                result.Result = filterdResult.Result.FirstOrDefault();
            else
                OASISErrorHandling.HandleError(ref result, $"Error occured in LoadOAPPTemplate loading the OAPP Template with Id {OAPPTemplateId} from the {Enum.GetName(typeof(ProviderType), providerType)} provider. Reason: {filterdResult.Message}");

            return result;
        }

        public async Task<OASISResult<IEnumerable<IOAPPTemplate>>> LoadAllOAPPTemplatesAsync(Guid avatarId, OAPPTemplateType OAPPTemplateType = OAPPTemplateType.All, bool showAllVersions = false, int version = 0, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<IOAPPTemplate>> result = new OASISResult<IEnumerable<IOAPPTemplate>>();
            OASISResult<IEnumerable<OAPPTemplate>> loadHolonsResult = null;

            if (OAPPTemplateType == OAPPTemplateType.All)
                loadHolonsResult = await Data.LoadAllHolonsAsync<OAPPTemplate>(HolonType.OAPPTemplate, true, true, 0, true, false, HolonType.All, 0, providerType);
            else
                //TODO: Need to upgrade HolonManager to be able to query for multiple metadata keys at once as well as retreive the latest version.
                loadHolonsResult = await Data.LoadHolonsByMetaDataAsync<OAPPTemplate>("OAPPTemplateType", Enum.GetName(typeof(OAPPTemplateType), OAPPTemplateType), HolonType.All, true, true, 0, true, false, 0, HolonType.All, 0);

            return FilterResultsForVersion(avatarId, loadHolonsResult, showAllVersions, version);
        }

        public OASISResult<IEnumerable<IOAPPTemplate>> LoadAllOAPPTemplates(Guid avatarId, OAPPTemplateType OAPPTemplateType = OAPPTemplateType.All, bool showAllVersions = false, int version = 0, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<IOAPPTemplate>> result = new OASISResult<IEnumerable<IOAPPTemplate>>();
            OASISResult<IEnumerable<OAPPTemplate>> loadHolonsResult = null;

            if (OAPPTemplateType == OAPPTemplateType.All)
                loadHolonsResult = Data.LoadAllHolons<OAPPTemplate>(HolonType.OAPPTemplate, true, true, 0, true, false, HolonType.All, 0, providerType);
            else
                loadHolonsResult = Data.LoadHolonsByMetaData<OAPPTemplate>("OAPPTemplateType", Enum.GetName(typeof(OAPPTemplateType), OAPPTemplateType));

            return FilterResultsForVersion(avatarId, loadHolonsResult, showAllVersions, version);
        }

        public async Task<OASISResult<IEnumerable<IOAPPTemplate>>> LoadAllOAPPTemplatesForAvatarAsync(Guid avatarId, bool showAllVersions = false, int version = 0, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<IOAPPTemplate>> result = new OASISResult<IEnumerable<IOAPPTemplate>>();
            //OASISResult<IEnumerable<OAPPTemplate>> loadHolonsResult = await LoadAllHolonsForAvatarAsync<OAPPTemplate>(avatarId, providerType, "OAPPTemplateManager.LoadAllOAPPTemplatesForAvatarAsync", HolonType.OAPPTemplate);
            //OASISResult<IEnumerable<OAPPTemplate>> loadHolonsResult = await Data.LoadHolonsByMetaDataAsync<OAPPTemplate>("Active", "1", HolonType.OAPPTemplate, false, false, 0, true, false, 0, HolonType.All, 0, providerType);
            OASISResult<IEnumerable<OAPPTemplate>> loadHolonsResult = await Data.LoadHolonsByMetaDataAsync<OAPPTemplate>(new Dictionary<string, string>()
            {
                { "CreatedByAvatarId", avatarId.ToString() },
                { "Active", "1" }

            }, MetaKeyValuePairMatchMode.All, HolonType.OAPPTemplate, providerType: providerType);

            return FilterResultsForVersion(avatarId, loadHolonsResult, showAllVersions, version);
        }

        public OASISResult<IEnumerable<IOAPPTemplate>> LoadAllOAPPTemplatesForAvatar(Guid avatarId, bool showAllVersions = false, int version = 0, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<IOAPPTemplate>> result = new OASISResult<IEnumerable<IOAPPTemplate>>();
            //OASISResult<IEnumerable<OAPPTemplate>> loadHolonsResult = LoadAllHolonsForAvatar<OAPPTemplate>(avatarId, providerType, "OAPPTemplateManager.LoadAllOAPPTemplatesForAvatarAsync", HolonType.OAPPTemplate);
            //OASISResult<IEnumerable<OAPPTemplate>> loadHolonsResult = Data.LoadHolonsByMetaData<OAPPTemplate>("Active", "1", HolonType.OAPPTemplate, false, false, 0, true, false, 0, HolonType.All, 0, providerType);

            OASISResult<IEnumerable<OAPPTemplate>> loadHolonsResult = Data.LoadHolonsByMetaData<OAPPTemplate>(new Dictionary<string, string>()
            {
                { "CreatedByAvatarId", avatarId.ToString() },
                { "Active", "1" }

            }, MetaKeyValuePairMatchMode.All, HolonType.OAPPTemplate, providerType: providerType);

            return FilterResultsForVersion(avatarId, loadHolonsResult, showAllVersions, version);
        }

        public async Task<OASISResult<IEnumerable<IOAPPTemplate>>> SearchOAPPTemplatesAsync(string searchTerm, Guid avatarId, bool searchOnlyForCurrentAvatar = true, bool showAllVersions = false, int version = 0, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<IOAPPTemplate>> result = new OASISResult<IEnumerable<IOAPPTemplate>>();
            OASISResult<IEnumerable<OAPPTemplate>> loadHolonsResult = await SearchHolonsAsync<OAPPTemplate>(searchTerm, avatarId, searchOnlyForCurrentAvatar, providerType, "OAPPTemplateManager.SearchOAPPTemplatesAsync", HolonType.OAPPTemplate);
            return FilterResultsForVersion(avatarId, loadHolonsResult, showAllVersions, version);
        }

        public OASISResult<IEnumerable<IOAPPTemplate>> SearchOAPPTemplates(string searchTerm, Guid avatarId, bool searchOnlyForCurrentAvatar = true, bool showAllVersions = false, int version = 0, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<IOAPPTemplate>> result = new OASISResult<IEnumerable<IOAPPTemplate>>();
            OASISResult<IEnumerable<OAPPTemplate>> loadHolonsResult = SearchHolons<OAPPTemplate>(searchTerm, avatarId, searchOnlyForCurrentAvatar, providerType, "OAPPTemplateManager.SearchOAPPTemplates", HolonType.OAPPTemplate);
            return FilterResultsForVersion(avatarId, loadHolonsResult, showAllVersions, version);
        }

        public async Task<OASISResult<IOAPPTemplate>> DeleteOAPPTemplateAsync(Guid avatarId, Guid oappTemplateId, int version, bool softDelete = true, bool deleteDownload = true, bool deleteInstall = true, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IOAPPTemplate> result = new OASISResult<IOAPPTemplate>();
            string errorMessage = "Error occured in DeleteOAPPTemplateAsync. Reason: ";
            OASISResult<IOAPPTemplate> oappTemplateResult = await LoadOAPPTemplateAsync(avatarId, oappTemplateId, version, providerType);

            if (oappTemplateResult != null && oappTemplateResult.Result != null && !oappTemplateResult.IsError)
            {
                result = await DeleteOAPPTemplateAsync(avatarId, oappTemplateResult.Result, version, softDelete, deleteDownload, deleteInstall, providerType);
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured loading the OAPP Template with Id {oappTemplateId} from the {Enum.GetName(typeof(ProviderType), providerType)} provider. Reason: {oappTemplateResult.Message}");
 
            return result;
        }

        public OASISResult<IOAPPTemplate> DeleteOAPPTemplate(Guid avatarId, Guid oappTemplateId, bool softDelete = true, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IOAPPTemplate> result = new OASISResult<IOAPPTemplate>();
            OASISResult<IOAPPTemplate> oappTemplateResult = LoadOAPPTemplate(avatarId, oappTemplateId, providerType: providerType);

            if (oappTemplateResult != null && oappTemplateResult.Result != null && !oappTemplateResult.IsError)
            {
                if (oappTemplateResult.Result.OAPPTemplateDNA.CreatedByAvatarId != avatarId)
                {
                    OASISErrorHandling.HandleError(ref result, $"Permission Denied. You did not create this OAPP Template. Error occured in DeleteOAPPTemplateAsync loading the OAPP Template with Id {oappTemplateId} from the {Enum.GetName(typeof(ProviderType), providerType)} provider. Reason: The OAPP Template was not created by the Avatar with Id {avatarId}.");
                    return result;
                }

                if (Directory.Exists(oappTemplateResult.Result.OAPPTemplateDNA.OAPPTemplatePath))
                    Directory.Delete(oappTemplateResult.Result.OAPPTemplateDNA.OAPPTemplatePath, true);

                if (Directory.Exists(oappTemplateResult.Result.OAPPTemplateDNA.OAPPTemplatePublishedPath))
                    Directory.Delete(oappTemplateResult.Result.OAPPTemplateDNA.OAPPTemplatePublishedPath, true);
            }
            else
                OASISErrorHandling.HandleError(ref result, $"Error occured in DeleteOAPPTemplate loading the OAPP Template with Id {oappTemplateId} from the {Enum.GetName(typeof(ProviderType), providerType)} provider. Reason: {oappTemplateResult.Message}");

            OASISResult<OAPPTemplate> loadHolonsResult = DeleteHolon<OAPPTemplate>(oappTemplateId, avatarId, softDelete, providerType, "OAPPTemplateManager.DeleteOAPPTemplate");
            result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(loadHolonsResult, result);
            result.Result = loadHolonsResult.Result;
            return result;
        }

        public async Task<OASISResult<IOAPPTemplate>> DeleteOAPPTemplateAsync(Guid avatarId, IOAPPTemplate oappTemplate, int version, bool softDelete = true, bool deleteDownload = true, bool deleteInstall = true, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IOAPPTemplate> result = new OASISResult<IOAPPTemplate>();
            string errorMessage = "Error occured in DeleteOAPPTemplateAsync. Reason: ";

            if (oappTemplate.OAPPTemplateDNA.CreatedByAvatarId != avatarId)
            {
                OASISErrorHandling.HandleError(ref result, $"Permission Denied. You did not create this OAPP Template. Error occured in DeleteOAPPTemplateAsync loading the OAPP Template with Id {oappTemplate.OAPPTemplateDNA.CreatedByAvatarId} from the {Enum.GetName(typeof(ProviderType), providerType)} provider. Reason: The OAPP Template was not created by the Avatar with Id {avatarId}.");
                return result;
            }

            try
            {
                if (!string.IsNullOrEmpty(oappTemplate.OAPPTemplateDNA.OAPPTemplatePath) && Directory.Exists(oappTemplate.OAPPTemplateDNA.OAPPTemplatePath))
                    Directory.Delete(oappTemplate.OAPPTemplateDNA.OAPPTemplatePath, true);
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} Error occured attempting to delete the OAPPTemplate folder {oappTemplate.OAPPTemplateDNA.OAPPTemplatePath}. PLEASE DELETE MANUALLY! Reason: {e}");
            }

            try
            {
                if (!string.IsNullOrEmpty(oappTemplate.OAPPTemplateDNA.OAPPTemplatePublishedPath) && File.Exists(oappTemplate.OAPPTemplateDNA.OAPPTemplatePublishedPath))
                    File.Delete(oappTemplate.OAPPTemplateDNA.OAPPTemplatePublishedPath);
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} Error occured attempting to delete the OAPPTemplate Published folder {oappTemplate.OAPPTemplateDNA.OAPPTemplatePublishedPath}. PLEASE DELETE MANUALLY! Reason: {e}");
            }

            if (deleteDownload || deleteInstall)
            {
                OASISResult<IInstalledOAPPTemplate> installedOAPPTemplateResult = await LoadInstalledOAPPTemplateAsync(avatarId, oappTemplate.OAPPTemplateDNA.Id, version, providerType);

                if (installedOAPPTemplateResult != null && installedOAPPTemplateResult.Result != null && !installedOAPPTemplateResult.IsError)
                {
                    try
                    {
                        if (deleteDownload && !string.IsNullOrEmpty(installedOAPPTemplateResult.Result.DownloadedPath) && File.Exists(installedOAPPTemplateResult.Result.DownloadedPath))
                            File.Delete(installedOAPPTemplateResult.Result.DownloadedPath);
                    }
                    catch (Exception e)
                    {
                        OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} Error occured attempting to delete the OAPPTemplate Download folder {installedOAPPTemplateResult.Result.DownloadedPath}. PLEASE DELETE MANUALLY! Reason: {e}");
                    }

                    try
                    {
                        if (deleteInstall && !string.IsNullOrEmpty(installedOAPPTemplateResult.Result.InstalledPath) && Directory.Exists(installedOAPPTemplateResult.Result.InstalledPath))
                            Directory.Delete(installedOAPPTemplateResult.Result.InstalledPath, true);
                    }
                    catch (Exception e)
                    {
                        OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} Error occured attempting to delete the OAPPTemplate Installed folder {installedOAPPTemplateResult.Result.InstalledPath}. PLEASE DELETE MANUALLY! Reason: {e}");
                    }

                    if (deleteInstall)
                    {
                        OASISResult<OAPPTemplate> deleteInstalledOAPPTemplateHolonResult = await DeleteHolonAsync<OAPPTemplate>(installedOAPPTemplateResult.Result.Id, avatarId, softDelete, providerType, "OAPPTemplateManager.DeleteOAPPTemplateAsync");

                        if (!(deleteInstalledOAPPTemplateHolonResult != null && deleteInstalledOAPPTemplateHolonResult.Result != null && !deleteInstalledOAPPTemplateHolonResult.IsError))
                            OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} Error occured deleting the Installed OAPPTemplate holon with id {installedOAPPTemplateResult.Result.Id} calling DeleteHolonAsync. Reason: {deleteInstalledOAPPTemplateHolonResult.Message}");
                    }

                    if (deleteDownload)
                    {
                        OASISResult<OAPPTemplate> deleteDownloadedOAPPTemplateHolonResult = await DeleteHolonAsync<OAPPTemplate>(installedOAPPTemplateResult.Result.DownloadedOAPPTemplateHolonId, avatarId, softDelete, providerType, "OAPPTemplateManager.DeleteOAPPTemplateAsync");

                        if (!(deleteDownloadedOAPPTemplateHolonResult != null && deleteDownloadedOAPPTemplateHolonResult.Result != null && !deleteDownloadedOAPPTemplateHolonResult.IsError))
                            OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} Error occured deleting the Downloaded OAPPTemplate holon with id {installedOAPPTemplateResult.Result.DownloadedOAPPTemplateHolonId} calling DeleteHolonAsync. Reason: {deleteDownloadedOAPPTemplateHolonResult.Message}");
                    }
                }
            }

            OASISResult<OAPPTemplate> deleteHolonResult = await DeleteHolonAsync<OAPPTemplate>(oappTemplate.Id, avatarId, softDelete, providerType, "OAPPTemplateManager.DeleteOAPPTemplateAsync");

            if (!(deleteHolonResult != null && deleteHolonResult.Result != null && !deleteHolonResult.IsError))
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured deleting the OAPPTemplate holon with id {oappTemplate.Id} calling DeleteHolonAsync. Reason: {deleteHolonResult.Message}");

            result.Result = deleteHolonResult.Result;
            return result;
        }

        public OASISResult<IOAPPTemplate> DeleteOAPPTemplate(Guid avatarId, IOAPPTemplate oappTemplate, bool softDelete = true, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IOAPPTemplate> result = new OASISResult<IOAPPTemplate>();

            if (oappTemplate.OAPPTemplateDNA.CreatedByAvatarId != avatarId)
            {
                OASISErrorHandling.HandleError(ref result, $"Permission Denied. You did not create this OAPP Template. Error occured in DeleteOAPPTemplateAsync loading the OAPP Template with Id {oappTemplate.OAPPTemplateDNA.Id} from the {Enum.GetName(typeof(ProviderType), providerType)} provider. Reason: The OAPP Template was not created by the Avatar with Id {avatarId}.");
                return result;
            }

            if (Directory.Exists(oappTemplate.OAPPTemplateDNA.OAPPTemplatePath))
                Directory.Delete(oappTemplate.OAPPTemplateDNA.OAPPTemplatePath, true);

            if (Directory.Exists(oappTemplate.OAPPTemplateDNA.OAPPTemplatePublishedPath))
                Directory.Delete(oappTemplate.OAPPTemplateDNA.OAPPTemplatePublishedPath, true);

            OASISResult<OAPPTemplate> loadHolonsResult = DeleteHolon<OAPPTemplate>(oappTemplate.Id, avatarId, softDelete, providerType, "OAPPTemplateManager.DeleteOAPPTemplate");
            result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(loadHolonsResult, result);
            result.Result = loadHolonsResult.Result;
            return result;
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
            OASISResult<IEnumerable<IOAPPTemplate>> result = new OASISResult<IEnumerable<IOAPPTemplate>>();

            //TODO: Currently we pass in 0 for version (which means the OASIS will return the latest version) but we need to be able to query for all versions (-1)
            //OASISResult<IEnumerable<OAPPTemplate>> loadHolonsResult = await Data.LoadHolonsByMetaDataAsync<OAPPTemplate>("OAPPTemplateId", OAPPTemplateId.ToString(), HolonType.OAPPTemplate, true, true, 0, true, false, 0, HolonType.All, -1, providerType);
            OASISResult<IEnumerable<OAPPTemplate>> loadHolonsResult = await Data.LoadHolonsByMetaDataAsync<OAPPTemplate>(new Dictionary<string, string>() 
            { 
                { "OAPPTemplateId", OAPPTemplateId.ToString() },
                { "Active", "1" }
            }, MetaKeyValuePairMatchMode.All, HolonType.OAPPTemplate, true, true, 0, true, false, 0, HolonType.All, -1, providerType);

            result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(loadHolonsResult, result);
            result.Result = loadHolonsResult.Result;
            return result;
        }

        public OASISResult<IEnumerable<IOAPPTemplate>> LoadOAPPTemplateVersions(Guid OAPPTemplateId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<IOAPPTemplate>> result = new OASISResult<IEnumerable<IOAPPTemplate>>();

            //TODO: Currently we pass in 0 for version (which means the OASIS will return the latest version) but we need to be able to query for all versions (-1)
            //OASISResult<IEnumerable<OAPPTemplate>> loadHolonsResult = Data.LoadHolonsByMetaData<OAPPTemplate>("OAPPTemplateId", OAPPTemplateId.ToString(), HolonType.OAPPTemplate, true, true, 0, true, false, 0, HolonType.All, -1, providerType);
            OASISResult<IEnumerable<OAPPTemplate>> loadHolonsResult = Data.LoadHolonsByMetaData<OAPPTemplate>(new Dictionary<string, string>()
            {
                { "OAPPTemplateId", OAPPTemplateId.ToString() },
                { "Active", "1" }
            }, MetaKeyValuePairMatchMode.All, HolonType.OAPPTemplate, true, true, 0, true, false, 0, HolonType.All, -1, providerType);

            result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(loadHolonsResult, result);
            result.Result = loadHolonsResult.Result;
            return result;
        }

        public async Task<OASISResult<IOAPPTemplate>> LoadOAPPTemplateVersionAsync(Guid OAPPTemplateId, string version, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IOAPPTemplate> result = new OASISResult<IOAPPTemplate>();
            OASISResult<OAPPTemplate> loadHolonResult = await Data.LoadHolonByMetaDataAsync<OAPPTemplate>(new Dictionary<string, string>()
            {
                 { "OAPPTemplateId", OAPPTemplateId.ToString() },
                 { "Version", version },
                 { "Active", "1" }
            }, MetaKeyValuePairMatchMode.All, HolonType.OAPPTemplate, true, true, 0, true, 0, false, HolonType.All, providerType);

            OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(loadHolonResult, result); //Copy any possible warnings etc.
            if (loadHolonResult != null && !loadHolonResult.IsError && loadHolonResult.Result != null)
            {
                if (loadHolonResult.Result.OAPPTemplateDNA.Version == version)
                    result.Result = loadHolonResult.Result;
                else
                    OASISErrorHandling.HandleError(ref result, $"Error occured in OAPPTemplateManager.LoadOAPPTemplateVersion. Reason: The version {version} does not exist for OAPPTemplateId {OAPPTemplateId}.");
            }
            else
                OASISErrorHandling.HandleError(ref result, $"Error occured in OAPPTemplateManager.LoadOAPPTemplateVersion. Reason: {loadHolonResult.Message}");

            return result;
        }

        public OASISResult<IOAPPTemplate> LoadOAPPTemplateVersion(Guid OAPPTemplateId, string version, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IOAPPTemplate> result = new OASISResult<IOAPPTemplate>();
            OASISResult<OAPPTemplate> loadHolonResult = Data.LoadHolonByMetaData<OAPPTemplate>(new Dictionary<string, string>()
            {
                 { "OAPPTemplateId", OAPPTemplateId.ToString() },
                 { "Version", version },
                 { "Active", "1" }
            }, MetaKeyValuePairMatchMode.All, HolonType.OAPPTemplate, true, true, 0, true, false, HolonType.All, 0, providerType);

            OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(loadHolonResult, result); //Copy any possible warnings etc.
            if (loadHolonResult != null && !loadHolonResult.IsError && loadHolonResult.Result != null)
            {
                if (loadHolonResult.Result.OAPPTemplateDNA.Version == version)
                    result.Result = loadHolonResult.Result;
                else
                    OASISErrorHandling.HandleError(ref result, $"Error occured in OAPPTemplateManager.LoadOAPPTemplateVersion. Reason: The version {version} does not exist for OAPPTemplateId {OAPPTemplateId}.");
            }
            else
                OASISErrorHandling.HandleError(ref result, $"Error occured in OAPPTemplateManager.LoadOAPPTemplateVersion. Reason: {loadHolonResult.Message}");

            return result;
        }

        public async Task<OASISResult<IOAPPTemplate>> EditOAPPTemplateAsync(Guid OAPPTemplateId, IOAPPTemplateDNA newOAPPTemplateDNA, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IOAPPTemplate> result = new OASISResult<IOAPPTemplate>();
            OASISResult<IOAPPTemplate> oappTemplateResult = await LoadOAPPTemplateAsync(OAPPTemplateId, avatarId, providerType: providerType);

            if (oappTemplateResult != null && oappTemplateResult.Result != null && !oappTemplateResult.IsError)
                await EditOAPPTemplateAsync(oappTemplateResult.Result, newOAPPTemplateDNA, avatarId, providerType);
            else
                OASISErrorHandling.HandleError(ref result, $"Error occured in OAPPTemplateManager.EditOAPPTemplateAsync. Reason: {oappTemplateResult.Message}");

            return result;
        }

        public async Task<OASISResult<IOAPPTemplate>> EditOAPPTemplateAsync(IOAPPTemplate OAPPTemplate, IOAPPTemplateDNA newOAPPTemplateDNA, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IOAPPTemplate> result = new OASISResult<IOAPPTemplate>();
            string errorMessage = "Error occured in OAPPTemplateManager.EditOAPPTemplateAsync. Reason: ";
            string oldPath = "";
            string newPath = "";
            string oldPublishedPath = "";
            string oldDownloadedPath = "";
            string oldInstalledPath = "";
            string oldName = "";
            string launchTarget = "";

            if (OAPPTemplate.Name != newOAPPTemplateDNA.Name)
            {
                oldName = OAPPTemplate.Name;
                oldPath = OAPPTemplate.OAPPTemplateDNA.OAPPTemplatePath;
                newPath = Path.Combine(new DirectoryInfo(OAPPTemplate.OAPPTemplateDNA.OAPPTemplatePath).Parent.FullName, newOAPPTemplateDNA.Name);
                newOAPPTemplateDNA.OAPPTemplatePath = newPath;
                newOAPPTemplateDNA.LaunchTarget = newOAPPTemplateDNA.LaunchTarget.Replace(OAPPTemplate.Name, newOAPPTemplateDNA.Name);
                launchTarget = newOAPPTemplateDNA.LaunchTarget;

                OAPPTemplate.MetaData["OAPPTemplateName"] = newOAPPTemplateDNA.Name;

                if (!string.IsNullOrEmpty(OAPPTemplate.OAPPTemplateDNA.OAPPTemplatePublishedPath))
                {
                    oldPublishedPath = OAPPTemplate.OAPPTemplateDNA.OAPPTemplatePublishedPath;
                    //newOAPPTemplateDNA.OAPPTemplatePublishedPath = Path.Combine(new DirectoryInfo(oldPublishedPath).Parent.FullName, newOAPPTemplateDNA.Name);
                    newOAPPTemplateDNA.OAPPTemplatePublishedPath = oldPublishedPath.Replace(oldName, newOAPPTemplateDNA.Name);
                }
            }

            OAPPTemplate.OAPPTemplateDNA = newOAPPTemplateDNA;
            OAPPTemplate.Name = newOAPPTemplateDNA.Name;
            OAPPTemplate.Description = newOAPPTemplateDNA.Description;

            if (!string.IsNullOrEmpty(newPath) && !string.IsNullOrEmpty(oldPath))
            {
                try
                {
                    if (Directory.Exists(oldPath))
                        Directory.Move(oldPath, newPath);
                }
                catch (Exception e)
                {
                    OASISErrorHandling.HandleWarning(ref result, $"An error occured attempting to rename the OAPP Template folder from {oldPath} to {newPath}. Reason: {e}.");
                    CLIEngine.ShowErrorMessage("PLEASE RENAME THIS FOLDER MANUALLY, THANK YOU!");
                }

                if (!string.IsNullOrEmpty(newOAPPTemplateDNA.OAPPTemplatePublishedPath))
                {
                    try
                    {
                        if (!string.IsNullOrEmpty(oldPublishedPath) && File.Exists(oldPublishedPath))
                            File.Move(oldPublishedPath, newOAPPTemplateDNA.OAPPTemplatePublishedPath);
                    }
                    catch (Exception e)
                    {
                        OASISErrorHandling.HandleWarning(ref result, $"An error occured attempting to rename the OAPP Template published file from {oldPublishedPath} to {newOAPPTemplateDNA.OAPPTemplatePublishedPath}. Reason: {e}.");
                        CLIEngine.ShowErrorMessage("PLEASE RENAME THIS FOLDER MANUALLY, THANK YOU!");
                    }
                }
            }

            OASISResult<IOAPPTemplate> saveResult = await SaveOAPPTemplateAsync(OAPPTemplate, avatarId, providerType: providerType);

            if (saveResult != null && !saveResult.IsError && saveResult.Result != null)
            {
                OASISResult<IEnumerable<IOAPPTemplate>> templatesResult = await LoadOAPPTemplateVersionsAsync(newOAPPTemplateDNA.Id, providerType);

                if (templatesResult != null && templatesResult.Result != null && !templatesResult.IsError)
                {
                    foreach (IOAPPTemplate template in templatesResult.Result)
                    {
                        //No need to update the version we already updated above.
                        if (template.OAPPTemplateDNA.Version == OAPPTemplate.OAPPTemplateDNA.Version)
                            continue;

                        template.OAPPTemplateDNA = newOAPPTemplateDNA;
                        template.Name = newOAPPTemplateDNA.Name;
                        template.Description = newOAPPTemplateDNA.Description;
                        template.MetaData["OAPPTemplateName"] = newOAPPTemplateDNA.Name;

                        oldPath = template.OAPPTemplateDNA.OAPPTemplatePath;
                        newPath = Path.Combine(new DirectoryInfo(oldPath).Parent.FullName, newOAPPTemplateDNA.Name);
                        template.OAPPTemplateDNA.OAPPTemplatePath = newPath;
                        template.OAPPTemplateDNA.LaunchTarget = launchTarget;

                        if (!string.IsNullOrEmpty(template.OAPPTemplateDNA.OAPPTemplatePublishedPath))
                        {
                            oldPublishedPath = template.OAPPTemplateDNA.OAPPTemplatePublishedPath;
                            //template.OAPPTemplateDNA.OAPPTemplatePublishedPath = Path.Combine(new DirectoryInfo(oldPublishedPath).FullName, newOAPPTemplateDNA.Name);
                            newOAPPTemplateDNA.OAPPTemplatePublishedPath = oldPublishedPath.Replace(oldName, newOAPPTemplateDNA.Name);
                        }

                        if (!string.IsNullOrEmpty(newPath))
                        {
                            try
                            {
                                if (Directory.Exists(oldPath))
                                    Directory.Move(oldPath, newPath);
                            }
                            catch (Exception e)
                            {
                                OASISErrorHandling.HandleWarning(ref result, $"An error occured attempting to rename the OAPP Template folder from {oldPath} to {newPath}. Reason: {e}.");
                                CLIEngine.ShowErrorMessage("PLEASE RENAME THIS FOLDER MANUALLY, THANK YOU!");
                            }
                        }

                        if (!string.IsNullOrEmpty(oldPublishedPath))
                        {
                            try
                            {
                                if (File.Exists(oldPublishedPath))
                                    File.Move(oldPublishedPath, template.OAPPTemplateDNA.OAPPTemplatePublishedPath);
                            }
                            catch (Exception e)
                            {
                                OASISErrorHandling.HandleWarning(ref result, $"An error occured attempting to rename the OAPP Template published file from {oldPublishedPath} to {newOAPPTemplateDNA.OAPPTemplatePublishedPath}. Reason: {e}.");
                                CLIEngine.ShowErrorMessage("PLEASE RENAME THIS FOLDER MANUALLY, THANK YOU!");
                            }
                        }

                        OASISResult<IOAPPTemplate> templateSaveResult = await SaveOAPPTemplateAsync(template, avatarId, providerType);

                        if (templateSaveResult != null && templateSaveResult.Result != null && !templateSaveResult.IsError)
                        {
                           
                        }
                        else
                            OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} Error occured calling SaveOAPPTemplateAsync updating the OAPPTemplateDNA for OAPP Template with Id {template.Id} for provider {Enum.GetName(typeof(ProviderType), providerType)}. Reason: {templateSaveResult.Message}");
                    }
                }
                else
                    OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} Error occured updating the OAPPTemplateDNA for all OAPP Template versions caused by an error in LoadOAPPTemplateVersionsAsync. Reason: {templatesResult.Message}");


                OASISResult<IEnumerable<IInstalledOAPPTemplate>> installedTemplatesResult = await ListInstalledOAPPTemplatesAsync(avatarId, providerType);

                if (installedTemplatesResult != null && installedTemplatesResult.Result != null && !installedTemplatesResult.IsError)
                {
                    foreach (IInstalledOAPPTemplate template in installedTemplatesResult.Result)
                    {
                        template.OAPPTemplateDNA = newOAPPTemplateDNA;
                        template.Name = template.Name.Replace(oldName, newOAPPTemplateDNA.Name);
                        template.Description = template.Description.Replace(oldName, newOAPPTemplateDNA.Name);
                        template.MetaData["OAPPTemplateName"] = newOAPPTemplateDNA.Name;

                        oldPath = template.OAPPTemplateDNA.OAPPTemplatePath;
                        newPath = Path.Combine(new DirectoryInfo(oldPath).Parent.FullName, newOAPPTemplateDNA.Name);
                        template.OAPPTemplateDNA.OAPPTemplatePath = newPath;
                        template.OAPPTemplateDNA.LaunchTarget = launchTarget;

                        if (!string.IsNullOrEmpty(template.OAPPTemplateDNA.OAPPTemplatePublishedPath))
                        {
                            oldPublishedPath = template.OAPPTemplateDNA.OAPPTemplatePublishedPath;
                            template.OAPPTemplateDNA.OAPPTemplatePublishedPath = Path.Combine(new DirectoryInfo(oldPublishedPath).Parent.FullName, string.Concat(newOAPPTemplateDNA.Name, "_v", template.OAPPTemplateDNA.Version, ".oapptemplate"));
                            //template.OAPPTemplateDNA.OAPPTemplatePublishedPath = oldPublishedPath.Replace(oldName, newOAPPTemplateDNA.Name);
                        }

                        if (!string.IsNullOrEmpty(template.DownloadedPath))
                        {
                            oldDownloadedPath = template.DownloadedPath;
                            //template.DownloadedPath = Path.Combine(new DirectoryInfo(oldDownloadedPath).FullName, newOAPPTemplateDNA.Name);
                            template.DownloadedPath = oldDownloadedPath.Replace(oldName, newOAPPTemplateDNA.Name);
                        }

                        if (!string.IsNullOrEmpty(template.InstalledPath))
                        {
                            oldInstalledPath = template.InstalledPath;
                            template.InstalledPath = Path.Combine(new DirectoryInfo(oldInstalledPath).Parent.FullName, newOAPPTemplateDNA.Name);
                        }

                        if (!string.IsNullOrEmpty(newPath))
                        {
                            try
                            {
                                if (Directory.Exists(oldPath) && oldPath != newPath)
                                    Directory.Move(oldPath, newPath);
                            }
                            catch (Exception e)
                            {
                                OASISErrorHandling.HandleWarning(ref result, $"An error occured attempting to rename the OAPP Template folder from {oldPath} to {newPath}. Reason: {e}.");
                                CLIEngine.ShowErrorMessage("PLEASE RENAME THIS FOLDER MANUALLY, THANK YOU!");
                            }
                        }

                        if (!string.IsNullOrEmpty(oldPublishedPath))
                        {
                            try
                            {
                                if (File.Exists(oldPublishedPath) && oldPublishedPath != template.OAPPTemplateDNA.OAPPTemplatePublishedPath)
                                    File.Move(oldPublishedPath, template.OAPPTemplateDNA.OAPPTemplatePublishedPath);
                            }
                            catch (Exception e)
                            {
                                OASISErrorHandling.HandleWarning(ref result, $"An error occured attempting to rename the OAPP Template published file from {oldPublishedPath} to {newOAPPTemplateDNA.OAPPTemplatePublishedPath}. Reason: {e}.");
                                CLIEngine.ShowErrorMessage("PLEASE RENAME THIS FOLDER MANUALLY, THANK YOU!");
                            }
                        }

                        OASISResult<IOAPPTemplate> templateSaveResult = await SaveOAPPTemplateAsync(template, avatarId, providerType);

                        if (templateSaveResult != null && templateSaveResult.Result != null && !templateSaveResult.IsError)
                        {
                            if (!string.IsNullOrEmpty(oldDownloadedPath))
                            {
                                try
                                {
                                    if (File.Exists(oldDownloadedPath))
                                        File.Move(oldDownloadedPath, template.DownloadedPath);
                                }
                                catch (Exception e)
                                {
                                    OASISErrorHandling.HandleWarning(ref result, $"An error occured attempting to rename the OAPP Template downloaded file from {oldDownloadedPath} to {template.DownloadedPath}. Reason: {e}.");
                                    CLIEngine.ShowErrorMessage("PLEASE RENAME THIS FOLDER MANUALLY, THANK YOU!");
                                }
                            }

                            if (!string.IsNullOrEmpty(oldInstalledPath))
                            {
                                try
                                {
                                    if (Directory.Exists(oldInstalledPath))
                                        Directory.Move(oldInstalledPath, template.InstalledPath);
                                }
                                catch (Exception e)
                                {
                                    OASISErrorHandling.HandleWarning(ref result, $"An error occured attempting to rename the OAPP Template installed folder from {oldInstalledPath} to {template.InstalledPath}. Reason: {e}.");
                                    CLIEngine.ShowErrorMessage("PLEASE RENAME THIS FOLDER MANUALLY, THANK YOU!");
                                }
                            }
                        }
                        else
                            OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} Error occured updating the OAPPTemplateDNA for Installed OAPP Template with Id {template.Id} for provider {Enum.GetName(typeof(ProviderType), providerType)}. Reason: {templateSaveResult.Message}");
                    }
                }
                else
                    OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} Error occured updating the OAPPTemplateDNA for all Installed OAPP Template versions caused by an error in ListInstalledOAPPTemplatesAsync. Reason: {templatesResult.Message}");


                result.Result = saveResult.Result;
                result.IsSaved = true;
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured saving the OAPP Template with Id {newOAPPTemplateDNA.Id} from the {Enum.GetName(typeof(ProviderType), providerType)} provider. Reason: {saveResult.Message}");

            return result;
        }

        private async Task UpdateOAPPTemplateAsync(IOAPPTemplate template, IOAPPTemplateDNA newOAPPTemplateDNA, Guid avatarId, OASISResult<IOAPPTemplate> result, string errorMessage, ProviderType providerType)
        {
            string oldPath = "";
            string newPath = "";
            string oldPublishedPath = "";

            template.OAPPTemplateDNA = newOAPPTemplateDNA;
            template.Name = newOAPPTemplateDNA.Name;
            template.Description = newOAPPTemplateDNA.Description;

            oldPath = template.OAPPTemplateDNA.OAPPTemplatePath;
            newPath = Path.Combine(new DirectoryInfo(oldPath).Parent.FullName, newOAPPTemplateDNA.Name);
            template.OAPPTemplateDNA.OAPPTemplatePath = newPath;

            if (!string.IsNullOrEmpty(template.OAPPTemplateDNA.OAPPTemplatePublishedPath))
            {
                oldPublishedPath = template.OAPPTemplateDNA.OAPPTemplatePublishedPath;
                template.OAPPTemplateDNA.OAPPTemplatePublishedPath = Path.Combine(new DirectoryInfo(oldPublishedPath).FullName, newOAPPTemplateDNA.Name);
            }

            OASISResult<IOAPPTemplate> templateSaveResult = await SaveOAPPTemplateAsync(template, avatarId, providerType);

            if (templateSaveResult != null && templateSaveResult.Result != null && !templateSaveResult.IsError)
            {
                if (!string.IsNullOrEmpty(newPath))
                {
                    try
                    {
                        if (Directory.Exists(oldPath))
                            Directory.Move(oldPath, newPath);
                    }
                    catch (Exception e)
                    {
                        OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} An error occured attempting to rename the OAPP Template folder from {oldPath} to {newPath}. Reason: {e}.");
                        CLIEngine.ShowErrorMessage("PLEASE RENAME THIS FOLDER MANUALLY, THANK YOU!");
                    }
                }

                if (!string.IsNullOrEmpty(oldPublishedPath))
                {
                    try
                    {
                        if (File.Exists(oldPublishedPath))
                            File.Move(oldPublishedPath, template.OAPPTemplateDNA.OAPPTemplatePublishedPath);
                    }
                    catch (Exception e)
                    {
                        OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} An error occured attempting to rename the OAPP Template published file from {oldPublishedPath} to {newOAPPTemplateDNA.OAPPTemplatePublishedPath}. Reason: {e}.");
                        CLIEngine.ShowErrorMessage("PLEASE RENAME THIS FOLDER MANUALLY, THANK YOU!");
                    }
                }
            }
            else
                OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} Error occured calling SaveOAPPTemplateAsync updating the OAPPTemplateDNA for OAPP Template with Id {template.Id} for provider {Enum.GetName(typeof(ProviderType), providerType)}. Reason: {templateSaveResult.Message}");
        }

        //public async Task<OASISResult<IOAPPTemplateDNA>> PublishOAPPTemplateAsync(string fullPathToOAPPTemplate, string launchTarget, Guid avatarId, string fullPathToPublishTo = "", bool registerOnSTARNET = true, bool generateOAPPTemplateBinary = true, bool uploadOAPPTemplateToCloud = false, ProviderType providerType = ProviderType.Default, ProviderType oappBinaryProviderType = ProviderType.IPFSOASIS)
        public async Task<OASISResult<IOAPPTemplate>> PublishOAPPTemplateAsync(Guid avatarId, string fullPathToOAPPTemplate, string launchTarget, string fullPathToPublishTo = "", bool registerOnSTARNET = true, bool generateOAPPTemplateBinary = true, bool uploadOAPPTemplateToCloud = false, bool edit = false, ProviderType providerType = ProviderType.Default, ProviderType oappBinaryProviderType = ProviderType.IPFSOASIS)
        {
            OASISResult<IOAPPTemplate> result = new OASISResult<IOAPPTemplate>();
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
                        //Load latest version.
                        OASISResult<IOAPPTemplate> loadOAPPTemplateResult = await LoadOAPPTemplateAsync(OAPPTemplateDNA.Id, avatarId);

                        if (loadOAPPTemplateResult != null && loadOAPPTemplateResult.Result != null && !loadOAPPTemplateResult.IsError)
                        {
                            if (loadOAPPTemplateResult.Result.OAPPTemplateDNA.CreatedByAvatarId == avatarId)
                            {
                                OASISResult<bool> validateVersionResult = ValidateVersion(OAPPTemplateDNA.Version, loadOAPPTemplateResult.Result.OAPPTemplateDNA.Version, fullPathToOAPPTemplate, OAPPTemplateDNA.PublishedOn == DateTime.MinValue, edit);

                                if (validateVersionResult != null && validateVersionResult.Result && !validateVersionResult.IsError)
                                {
                                    //TODO: Maybe add check to make sure the DNA has not been tampered with?
                                    loadOAPPTemplateResult.Result.OAPPTemplateDNA.Version = OAPPTemplateDNA.Version; //Set the new version set in the DNA (JSON file).
                                    OAPPTemplateDNA = loadOAPPTemplateResult.Result.OAPPTemplateDNA; //Make sure it has not been tampered with by using the stored version.

                                    if (!edit)
                                    {
                                        OAPPTemplateDNA.VersionSequence++;
                                        OAPPTemplateDNA.NumberOfVersions++;
                                    }

                                    OAPPTemplateDNA.LaunchTarget = launchTarget;

                                    string publishedOAPPTemplateFileName = string.Concat(OAPPTemplateDNA.Name, "_v", OAPPTemplateDNA.Version, ".oapptemplate");

                                    if (string.IsNullOrEmpty(fullPathToPublishTo))
                                        fullPathToPublishTo = Path.Combine(fullPathToOAPPTemplate, "Published");

                                    if (!Directory.Exists(fullPathToPublishTo))
                                        Directory.CreateDirectory(fullPathToPublishTo);

                                    if (!edit)
                                    {
                                        OAPPTemplateDNA.PublishedOn = DateTime.Now;
                                        OAPPTemplateDNA.PublishedByAvatarId = avatarId;
                                        OAPPTemplateDNA.PublishedByAvatarUsername = loadAvatarResult.Result.Username;
                                    }
                                    else
                                    {
                                        OAPPTemplateDNA.ModifiedOn = DateTime.Now;
                                        OAPPTemplateDNA.ModifiedByAvatarId = avatarId;
                                        OAPPTemplateDNA.ModifiedByAvatarUsername = loadAvatarResult.Result.Username;
                                    }

                                    OAPPTemplateDNA.OAPPTemplatePublishedOnSTARNET = registerOnSTARNET && (oappBinaryProviderType != ProviderType.None || uploadOAPPTemplateToCloud);

                                    if (generateOAPPTemplateBinary)
                                    {
                                        OAPPTemplateDNA.OAPPTemplatePublishedPath = Path.Combine(fullPathToPublishTo, publishedOAPPTemplateFileName);
                                        OAPPTemplateDNA.OAPPTemplatePublishedToCloud = registerOnSTARNET && uploadOAPPTemplateToCloud;
                                        OAPPTemplateDNA.OAPPTemplatePublishedProviderType = oappBinaryProviderType;
                                    }

                                    WriteOAPPTemplateDNA(OAPPTemplateDNA, fullPathToOAPPTemplate);
                                    OnOAPPTemplatePublishStatusChanged?.Invoke(this, new OAPPTemplatePublishStatusEventArgs() { OAPPTemplateDNA = OAPPTemplateDNA, Status = Enums.OAPPTemplatePublishStatus.Compressing });

                                    if (generateOAPPTemplateBinary)
                                    {
                                        if (File.Exists(OAPPTemplateDNA.OAPPTemplatePublishedPath))
                                            File.Delete(OAPPTemplateDNA.OAPPTemplatePublishedPath);

                                        ZipFile.CreateFromDirectory(fullPathToOAPPTemplate, OAPPTemplateDNA.OAPPTemplatePublishedPath);

                                        //tempPath = Path.GetTempPath();
                                        //tempPath = Path.Combine(tempPath, readOAPPTemplateDNAResult.Result.Name);

                                        //if (Directory.Exists(tempPath))
                                        //    Directory.Delete(tempPath, true);

                                        //ZipFile.CreateFromDirectory(fullPathToOAPPTemplate, tempPath);
                                        //File.Move(tempPath, OAPPTemplateDNA.OAPPTemplatePublishedPath);
                                    }

                                    //TODO: Currently the filesize will NOT be in the compressed .oapptemplate file because we dont know the size before we create it! ;-) We would need to compress it twice or edit the compressed file after to update the OAPPTemplateDNA inside it...
                                    if (!string.IsNullOrEmpty(OAPPTemplateDNA.OAPPTemplatePublishedPath) && File.Exists(OAPPTemplateDNA.OAPPTemplatePublishedPath))
                                        OAPPTemplateDNA.OAPPTemplateFileSize = new FileInfo(OAPPTemplateDNA.OAPPTemplatePublishedPath).Length;

                                    WriteOAPPTemplateDNA(OAPPTemplateDNA, fullPathToOAPPTemplate);
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
                                                using (var fileStream = File.OpenRead(OAPPTemplateDNA.OAPPTemplatePublishedPath))
                                                {
                                                    _fileLength = fileStream.Length;
                                                    _progress = 0;

                                                    await storage.UploadObjectAsync(GOOGLE_CLOUD_BUCKET_NAME, publishedOAPPTemplateFileName, "", fileStream, uploadObjectOptions, progress: progressReporter);
                                                }

                                                _progress = 100;
                                                OnOAPPTemplateUploadStatusChanged?.Invoke(this, new OAPPTemplateUploadProgressEventArgs() { Progress = _progress, Status = Enums.OAPPTemplateUploadStatus.Uploading });
                                                CLIEngine.DisposeProgressBar(false);
                                                Console.WriteLine("");

                                                //HttpClient client = new HttpClient();
                                                //string pinataApiKey = "33e4469830a51af0171b";
                                                //string pinataSecretApiKey = "ff57367b2b125bf5f06f79b30b466890c84eed101c12af064459d88d8bb8d8a0\r\nJWT: eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJ1c2VySW5mb3JtYXRpb24iOnsiaWQiOiIzMGI3NjllNS1hMjJmLTQxN2UtOWEwYi1mZTQ2NzE5MjgzNzgiLCJlbWFpbCI6ImRhdmlkZWxsYW1zQGhvdG1haWwuY29tIiwiZW1haWxfdmVyaWZpZWQiOnRydWUsInBpbl9wb2xpY3kiOnsicmVnaW9ucyI6W3siZGVzaXJlZFJlcGxpY2F0aW9uQ291bnQiOjEsImlkIjoiRlJBMSJ9LHsiZGVzaXJlZFJlcGxpY2F0aW9uQ291bnQiOjEsImlkIjoiTllDMSJ9XSwidmVyc2lvbiI6MX0sIm1mYV9lbmFibGVkIjpmYWxzZSwic3RhdHVzIjoiQUNUSVZFIn0sImF1dGhlbnRpY2F0aW9uVHlwZSI6InNjb3BlZEtleSIsInNjb3BlZEtleUtleSI6IjMzZTQ0Njk4MzBhNTFhZjAxNzFiIiwic2NvcGVkS2V5U2VjcmV0IjoiZmY1NzM2N2IyYjEyNWJmNWYwNmY3OWIzMGI0NjY4OTBjODRlZWQxMDFjMTJhZjA2NDQ1OWQ4OGQ4YmI4ZDhhMCIsImV4cCI6MTc3Mzc4NDAzNX0.L-6_BPMsvhN3Es72Q5lZAFKpBEDF9kEibOGdWd_PxHs";
                                                //string pinataUrl = "https://api.pinata.cloud/pinning/pinFileToIPFS";
                                                //string filePath = OAPPTemplateDNA.OAPPTemplatePublishedPath;

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
                                                //PinFileResponse pinFileResponse = await pinataClient.PinFileToIPFSAsync(OAPPTemplateDNA.OAPPTemplatePublishedPath);

                                                //if (pinFileResponse != null && !string.IsNullOrEmpty(pinFileResponse.IpfsHash))
                                                //{
                                                //    OAPPTemplateDNA.PinataIPFSHash = pinFileResponse.IpfsHash;
                                                //    OAPPTemplateDNA.OAPPTemplatePublishedOnSTARNET = true;
                                                //    OAPPTemplateDNA.OAPPTemplatePublishedToPinata = true;
                                                //}
                                                //else
                                                //{
                                                //    OASISErrorHandling.HandleWarning(ref result, $"An error occured publishing the OAPPTemplate to Pinata.");
                                                //    OAPPTemplateDNA.OAPPTemplatePublishedOnSTARNET = registerOnSTARNET && oappBinaryProviderType != ProviderType.None;
                                                //}
                                            }
                                            catch (Exception ex)
                                            {
                                                CLIEngine.DisposeProgressBar(false);
                                                Console.WriteLine("");

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
                                                result.Result = saveLargeOAPPTemplateResult.Result;
                                                result.IsSaved = true;
                                            }
                                            else
                                            {
                                                OASISErrorHandling.HandleWarning(ref result, $" Error occured saving the published OAPPTemplate binary to STARNET using the {oappBinaryProviderType} provider. Reason: {saveLargeOAPPTemplateResult.Message}");
                                                OAPPTemplateDNA.OAPPTemplatePublishedOnSTARNET = registerOnSTARNET && uploadOAPPTemplateToCloud;
                                                OAPPTemplateDNA.OAPPTemplatePublishedProviderType = ProviderType.None;
                                            }
                                        }
                                        else
                                            OAPPTemplateDNA.OAPPTemplatePublishedProviderType = ProviderType.None;
                                    }

                                    //If its not the first version.
                                    if (OAPPTemplateDNA.Version != "1.0.0" && !edit)
                                    {
                                        //If the ID has not been set then store the original id now.
                                        if (!loadOAPPTemplateResult.Result.MetaData.ContainsKey("OAPPTemplateId"))
                                            loadOAPPTemplateResult.Result.MetaData["OAPPTemplateId"] = loadOAPPTemplateResult.Result.Id;

                                        loadOAPPTemplateResult.Result.MetaData["Version"] = loadOAPPTemplateResult.Result.OAPPTemplateDNA.Version;
                                        loadOAPPTemplateResult.Result.MetaData["VersionSequence"] = loadOAPPTemplateResult.Result.OAPPTemplateDNA.VersionSequence;

                                        //Blank fields so it creates a new version.
                                        loadOAPPTemplateResult.Result.Id = Guid.Empty;
                                        loadOAPPTemplateResult.Result.ProviderUniqueStorageKey.Clear();
                                        loadOAPPTemplateResult.Result.CreatedDate = DateTime.MinValue;
                                        loadOAPPTemplateResult.Result.ModifiedDate = DateTime.MinValue;
                                        loadOAPPTemplateResult.Result.CreatedByAvatarId = Guid.Empty;
                                        loadOAPPTemplateResult.Result.ModifiedByAvatarId = Guid.Empty;
                                        loadOAPPTemplateResult.Result.OAPPTemplateDNA.Downloads = 0;
                                        loadOAPPTemplateResult.Result.OAPPTemplateDNA.Installs = 0;
                                    }

                                    OASISResult<IOAPPTemplate> saveOAPPTemplateResult = await SaveOAPPTemplateAsync(loadOAPPTemplateResult.Result, avatarId, providerType);

                                    if (saveOAPPTemplateResult != null && !saveOAPPTemplateResult.IsError && saveOAPPTemplateResult.Result != null)
                                    {
                                        result = await UpdateNumberOfVersionCountsAsync(saveOAPPTemplateResult, avatarId, errorMessage, providerType);

                                        //OASISResult<IEnumerable<IOAPPTemplate>> templatesResult = await LoadOAPPTemplateVersionsAsync(OAPPTemplateDNA.Id, providerType);

                                        //if (templatesResult != null && templatesResult.Result != null && !templatesResult.IsError)
                                        //{
                                        //    //Update all versions with the total number of versions.
                                        //    foreach (IOAPPTemplate template in templatesResult.Result)
                                        //    {
                                        //        template.OAPPTemplateDNA.NumberOfVersions = OAPPTemplateDNA.NumberOfVersions;
                                        //        OASISResult<IOAPPTemplate> templateSaveResult = await SaveOAPPTemplateAsync(template, avatarId, providerType);

                                        //        if (!(templateSaveResult != null && templateSaveResult.Result != null && !templateSaveResult.IsError))
                                        //            OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} Error occured updating the NumberOfVersions for OAPP Template with Id {template.Id} for provider {Enum.GetName(typeof(ProviderType), providerType)}. Reason: {templateSaveResult.Message}");
                                        //    }
                                        //}

                                        //result.Result = saveOAPPTemplateResult.Result;
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
                                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling ValidateResult. Reason: {validateVersionResult.Message}");
                            }
                            else
                                OASISErrorHandling.HandleError(ref result, $"Permission Denied! The OAPP Template with id {OAPPTemplateDNA.Id} was created by a different avatar with id {OAPPTemplateDNA.CreatedByAvatarId}. The current avatar has an id of {avatarId}.");
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
            finally
            {
                if (Directory.Exists(tempPath))
                    Directory.Delete(tempPath);
            }

            //if (result.IsError)
            //    OnOAPPTemplatePublishStatusChanged?.Invoke(this, new OAPPTemplatePublishStatusEventArgs() { OAPPTemplateDNA = OAPPTemplateDNA, Status = Enums.OAPPTemplatePublishStatus.Error, ErrorMessage = result.Message });

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

        public OASISResult<IOAPPTemplate> PublishOAPPTemplate(string fullPathToOAPPTemplate, string launchTarget, Guid avatarId, string fullPathToPublishTo = "", bool registerOnSTARNET = true, bool generateOAPPTemplateBinary = true, bool uploadOAPPTemplateToCloud = false, ProviderType providerType = ProviderType.Default, ProviderType oappBinaryProviderType = ProviderType.IPFSOASIS)
        {
            OASISResult<IOAPPTemplate> result = new OASISResult<IOAPPTemplate>();
            string errorMessage = "Error occured in OAPPTemplateManager.PublishOAPPTemplate. Reason: ";
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

                        if (string.IsNullOrEmpty(fullPathToPublishTo))
                            fullPathToPublishTo = Path.Combine(fullPathToOAPPTemplate, "Published");

                        if (!Directory.Exists(fullPathToPublishTo))
                            Directory.CreateDirectory(fullPathToPublishTo);

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

                        OAPPTemplateDNA.NumberOfVersions++;
                        OAPPTemplateDNA.VersionSequence++;

                        WriteOAPPTemplateDNA(OAPPTemplateDNA, fullPathToOAPPTemplate);
                        OnOAPPTemplatePublishStatusChanged?.Invoke(this, new OAPPTemplatePublishStatusEventArgs() { OAPPTemplateDNA = OAPPTemplateDNA, Status = Enums.OAPPTemplatePublishStatus.Compressing });

                        if (generateOAPPTemplateBinary)
                        {
                            if (File.Exists(OAPPTemplateDNA.OAPPTemplatePublishedPath))
                                File.Delete(OAPPTemplateDNA.OAPPTemplatePublishedPath);

                            ZipFile.CreateFromDirectory(fullPathToOAPPTemplate, OAPPTemplateDNA.OAPPTemplatePublishedPath);
                            File.Move(tempPath, readOAPPTemplateDNAResult.Result.OAPPTemplatePublishedPath);
                        }

                        //TODO: Currently the filesize will NOT be in the compressed .oapptemplate file because we dont know the size before we create it! ;-) We would need to compress it twice or edit the compressed file after to update the OAPPTemplateDNA inside it...
                        if (!string.IsNullOrEmpty(OAPPTemplateDNA.OAPPTemplatePublishedPath) && File.Exists(OAPPTemplateDNA.OAPPTemplatePublishedPath))
                            OAPPTemplateDNA.OAPPTemplateFileSize = new FileInfo(OAPPTemplateDNA.OAPPTemplatePublishedPath).Length;

                        WriteOAPPTemplateDNA(OAPPTemplateDNA, fullPathToOAPPTemplate);

                        OASISResult<IOAPPTemplate> loadOAPPTemplateResult = LoadOAPPTemplate(avatarId, OAPPTemplateDNA.Id);

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

                                        // set minimum chunksize just to see progress updating
                                        var uploadObjectOptions = new UploadObjectOptions
                                        {
                                            ChunkSize = UploadObjectOptions.MinimumChunkSize
                                        };

                                        var progressReporter = new Progress<Google.Apis.Upload.IUploadProgress>(OnUploadProgress);
                                        using var fileStream = File.OpenRead(OAPPTemplateDNA.OAPPTemplatePublishedPath);
                                        _fileLength = fileStream.Length;
                                        _progress = 0;

                                        storage.UploadObject(GOOGLE_CLOUD_BUCKET_NAME, publishedOAPPTemplateFileName, "oapptemplate", fileStream, uploadObjectOptions, progress: progressReporter);
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
                                        result.Result = saveLargeOAPPTemplateResult.Result;
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
                                result.Result = saveOAPPTemplateResult.Result;
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