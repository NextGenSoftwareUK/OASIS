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
using NextGenSoftware.CLI.Engine;
using PinataNET;
using PinataNET.Models;
using Pinata.Client;
using System.Net.Http;
using System.Net.Mime;
using SharpCompress.Common;
using NextGenSoftware.Utilities;
using System.Diagnostics.Eventing.Reader;
using Amazon.Runtime.Internal.Transform;

namespace NextGenSoftware.OASIS.API.ONode.Core.Managers
{
    public class OAPPTemplateManager : COSMICManagerBase//, IOAPPTemplateManager
    {
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


        public async Task<OASISResult<IOAPPTemplate>> CreateOAPPTemplateAsync(string name, string description, OAPPTemplateType OAPPTemplateType, Guid avatarId, string fullPathToOAPPTemplate, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IOAPPTemplate> result = new OASISResult<IOAPPTemplate>();
            string errorMessage = "Error occured in OAPPTemplateManager.CreateOAPPTemplateAsync, Reason:";

            try
            {
                if (Directory.Exists(fullPathToOAPPTemplate))
                {
                    if (CLIEngine.GetConfirmation($"The directory {fullPathToOAPPTemplate} already exists! Would you like to delete it?"))
                    {
                        Console.WriteLine("");
                        Directory.Delete(fullPathToOAPPTemplate, true);
                    }
                    else
                    {
                        Console.WriteLine("");
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} The directory {fullPathToOAPPTemplate} already exists! Please either delete it or choose a different name.");
                        return result;
                    }
                }

                OAPPTemplate OAPPTemplate = new OAPPTemplate()
                {
                    Id = Guid.NewGuid(),
                    Name = name,
                    Description = description
                };

                OAPPTemplate.MetaData["OAPPTemplateId"] = OAPPTemplate.Id.ToString();
                OAPPTemplate.MetaData["OAPPTemplateName"] = OAPPTemplate.Name;
                OAPPTemplate.MetaData["OAPPTemplateType"] = Enum.GetName(typeof(OAPPTemplateType), OAPPTemplateType);
                OAPPTemplate.MetaData["Version"] = "1.0.0";
                OAPPTemplate.MetaData["VersionSequence"] = 1;
                OAPPTemplate.MetaData["Active"] = "1";
                //OAPPTemplate.MetaData["LatestVersion"] = "1";

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
                        DotNetVersion = OASISBootLoader.OASISBootLoader.DotNetVersion,
                        OAPPTemplatePath = fullPathToOAPPTemplate
                    };

                    OASISResult<bool> writeOAPPTemplateDNAResult = await WriteOAPPTemplateDNAAsync(OAPPTemplateDNA, fullPathToOAPPTemplate);

                    if (writeOAPPTemplateDNAResult != null && writeOAPPTemplateDNAResult.Result && !writeOAPPTemplateDNAResult.IsError)
                    {
                        OAPPTemplate.OAPPTemplateDNA = OAPPTemplateDNA;
                        OASISResult<OAPPTemplate> saveHolonResult = await Data.SaveHolonAsync<OAPPTemplate>(OAPPTemplate, avatarId, true, true, 0, true, false, providerType);

                        if (saveHolonResult != null && saveHolonResult.Result != null && !saveHolonResult.IsError)
                        {
                            result.Result = saveHolonResult.Result;
                            result.Message = $"Successfully created the OAPP Template on the {Enum.GetName(typeof(ProviderType), providerType)} provider by AvatarId {avatarId} for OAPPTemplateType {Enum.GetName(typeof(OAPPTemplateType), OAPPTemplateType)}.";
                        }
                        else
                            OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured saving the OAPP Template to the {Enum.GetName(typeof(ProviderType), providerType)} provider. Reason: {saveHolonResult.Message}");
                    }
                    else
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured writing the OAPP Template DNA. Reason: {writeOAPPTemplateDNAResult.Message}");
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

        public OASISResult<IOAPPTemplate> CreateOAPPTemplate(string name, string description, OAPPTemplateType OAPPTemplateType, Guid avatarId, string fullPathToOAPPTemplate, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IOAPPTemplate> result = new OASISResult<IOAPPTemplate>();
            string errorMessage = "Error occured in OAPPTemplateManager.CreateOAPPTemplate, Reason:";

            try
            {
                if (Directory.Exists(fullPathToOAPPTemplate))
                {
                    if (CLIEngine.GetConfirmation($"{errorMessage} The directory {fullPathToOAPPTemplate} already exists! Would you like to delete it?"))
                        Directory.Delete(fullPathToOAPPTemplate);
                    else
                    {
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} The directory {fullPathToOAPPTemplate} already exists! Please either delete it or choose a different name.");
                        return result;
                    }
                }

                OAPPTemplate OAPPTemplate = new OAPPTemplate()
                {
                    Id = Guid.NewGuid(),
                    Name = name,
                    Description = description
                };

                OAPPTemplate.MetaData["OAPPTemplateType"] = Enum.GetName(typeof(OAPPTemplateType), OAPPTemplateType);
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
                        DotNetVersion = OASISBootLoader.OASISBootLoader.DotNetVersion,
                        OAPPTemplatePath = fullPathToOAPPTemplate
                    };

                    WriteOAPPTemplateDNA(OAPPTemplateDNA, fullPathToOAPPTemplate);

                    OAPPTemplate.OAPPTemplateDNA = OAPPTemplateDNA;
                    OASISResult<OAPPTemplate> saveHolonResult = Data.SaveHolon<OAPPTemplate>(OAPPTemplate, avatarId, true, true, 0, true, false, providerType);

                    if (saveHolonResult != null && saveHolonResult.Result != null && !saveHolonResult.IsError)
                    {
                        result.Result = saveHolonResult.Result;
                        result.Message = $"Successfully created the OAPP Template on the {Enum.GetName(typeof(ProviderType), providerType)} provider by AvatarId {avatarId} for OAPPTemplateType {Enum.GetName(typeof(OAPPTemplateType), OAPPTemplateType)}.";
                    }
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

        public async Task<OASISResult<IOAPPTemplate>> LoadOAPPTemplateAsync(Guid OAPPTemplateId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IOAPPTemplate> result = new OASISResult<IOAPPTemplate>();
            //OASISResult<OAPPTemplate> loadResult = await LoadHolonAsync<OAPPTemplate>(OAPPTemplateId, providerType, "OAPPTemplateManager.LoadOAPPTemplateAsync");
            OASISResult<IEnumerable<OAPPTemplate>> loadResult = await Data.LoadHolonsByMetaDataAsync<OAPPTemplate>("OAPPTemplateId", OAPPTemplateId.ToString(), HolonType.OAPPTemplate, true, true, 0, true, false, 0, HolonType.All, 0, providerType);
            OASISResult<IEnumerable<IOAPPTemplate>> filterdResult = FilterResults(loadResult, 0);

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

        public OASISResult<IOAPPTemplate> LoadOAPPTemplate(Guid OAPPTemplateId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IOAPPTemplate> result = new OASISResult<IOAPPTemplate>();
            //OASISResult<OAPPTemplate> loadResult = LoadHolon<OAPPTemplate>(OAPPTemplateId, providerType, "OAPPTemplateManager.LoadOAPPTemplate");

            OASISResult<IEnumerable<OAPPTemplate>> loadResult = Data.LoadHolonsByMetaData<OAPPTemplate>("OAPPTemplateId", OAPPTemplateId.ToString(), HolonType.OAPPTemplate, true, true, 0, true, false, 0, HolonType.All, 0, providerType);
            OASISResult<IEnumerable<IOAPPTemplate>> filterdResult = FilterResults(loadResult, 0);

            if (filterdResult != null && filterdResult.Result != null && !filterdResult.IsError)
                result.Result = filterdResult.Result.FirstOrDefault();
            else
                OASISErrorHandling.HandleError(ref result, $"Error occured in LoadOAPPTemplate loading the OAPP Template with Id {OAPPTemplateId} from the {Enum.GetName(typeof(ProviderType), providerType)} provider. Reason: {filterdResult.Message}");

            return result;
        }

        public async Task<OASISResult<IEnumerable<IOAPPTemplate>>> LoadAllOAPPTemplatesAsync(OAPPTemplateType OAPPTemplateType = OAPPTemplateType.All, int version = 0, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<IOAPPTemplate>> result = new OASISResult<IEnumerable<IOAPPTemplate>>();
            OASISResult<IEnumerable<OAPPTemplate>> loadHolonsResult = null;

            if (OAPPTemplateType == OAPPTemplateType.All)
                loadHolonsResult = await Data.LoadAllHolonsAsync<OAPPTemplate>(HolonType.OAPPTemplate, true, true, 0, true, false, HolonType.All, 0, providerType);
            else
                //TODO: Need to upgrade HolonManager to be able to query for multiple metadata keys at once as well as retreive the latest version.
                loadHolonsResult = await Data.LoadHolonsByMetaDataAsync<OAPPTemplate>("OAPPTemplateType", Enum.GetName(typeof(OAPPTemplateType), OAPPTemplateType), HolonType.All, true, true, 0, true, false, 0, HolonType.All, 0);

            return FilterResults(loadHolonsResult, version);
        }

        public OASISResult<IEnumerable<IOAPPTemplate>> LoadAllOAPPTemplates(OAPPTemplateType OAPPTemplateType = OAPPTemplateType.All, int version = 0, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<IOAPPTemplate>> result = new OASISResult<IEnumerable<IOAPPTemplate>>();
            OASISResult<IEnumerable<OAPPTemplate>> loadHolonsResult = null;

            if (OAPPTemplateType == OAPPTemplateType.All)
                loadHolonsResult = Data.LoadAllHolons<OAPPTemplate>(HolonType.OAPPTemplate, true, true, 0, true, false, HolonType.All, 0, providerType);
            else
                loadHolonsResult = Data.LoadHolonsByMetaData<OAPPTemplate>("OAPPTemplateType", Enum.GetName(typeof(OAPPTemplateType), OAPPTemplateType));

            return FilterResults(loadHolonsResult, version);
        }

        public async Task<OASISResult<IEnumerable<IOAPPTemplate>>> LoadAllOAPPTemplatesForAvatarAsync(Guid avatarId, int version = 0, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<IOAPPTemplate>> result = new OASISResult<IEnumerable<IOAPPTemplate>>();
            OASISResult<IEnumerable<OAPPTemplate>> loadHolonsResult = await LoadAllHolonsForAvatarAsync<OAPPTemplate>(avatarId, providerType, "OAPPTemplateManager.LoadAllOAPPTemplatesForAvatarAsync", HolonType.OAPPTemplate);
            return FilterResults(loadHolonsResult, version);
        }

        public OASISResult<IEnumerable<IOAPPTemplate>> LoadAllOAPPTemplatesForAvatar(Guid avatarId, int version = 0, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<IOAPPTemplate>> result = new OASISResult<IEnumerable<IOAPPTemplate>>();
            OASISResult<IEnumerable<OAPPTemplate>> loadHolonsResult = LoadAllHolonsForAvatar<OAPPTemplate>(avatarId, providerType, "OAPPTemplateManager.LoadAllOAPPTemplatesForAvatarAsync", HolonType.OAPPTemplate);
            return FilterResults(loadHolonsResult, version);
        }

        public async Task<OASISResult<IEnumerable<IOAPPTemplate>>> SearchOAPPTemplatesAsync(string searchTerm, int version = 0, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<IOAPPTemplate>> result = new OASISResult<IEnumerable<IOAPPTemplate>>();
            OASISResult<IEnumerable<OAPPTemplate>> loadHolonsResult = await SearchHolonsAsync<OAPPTemplate>(searchTerm, providerType, "OAPPTemplateManager.SearchOAPPTemplatesAsync", HolonType.OAPPTemplate);
            return FilterResults(loadHolonsResult, version);
        }

        public OASISResult<IEnumerable<IOAPPTemplate>> SearchOAPPTemplates(string searchTerm, int version = 0, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<IOAPPTemplate>> result = new OASISResult<IEnumerable<IOAPPTemplate>>();
            OASISResult<IEnumerable<OAPPTemplate>> loadHolonsResult = SearchHolons<OAPPTemplate>(searchTerm, providerType, "OAPPTemplateManager.SearchOAPPTemplates", HolonType.OAPPTemplate);
            return FilterResults(loadHolonsResult, version);
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

        //public async Task<OASISResult<IOAPPTemplateDNA>> PublishOAPPTemplateAsync(string fullPathToOAPPTemplate, string launchTarget, Guid avatarId, string fullPathToPublishTo = "", bool registerOnSTARNET = true, bool generateOAPPTemplateBinary = true, bool uploadOAPPTemplateToCloud = false, ProviderType providerType = ProviderType.Default, ProviderType oappBinaryProviderType = ProviderType.IPFSOASIS)
        public async Task<OASISResult<IOAPPTemplate>> PublishOAPPTemplateAsync(string fullPathToOAPPTemplate, string launchTarget, Guid avatarId, string fullPathToPublishTo = "", bool registerOnSTARNET = true, bool generateOAPPTemplateBinary = true, bool uploadOAPPTemplateToCloud = false, ProviderType providerType = ProviderType.Default, ProviderType oappBinaryProviderType = ProviderType.IPFSOASIS)
        {
            OASISResult<IOAPPTemplate> result = new OASISResult<IOAPPTemplate>();
            //OASISResult<IOAPPTemplateDNA> result = new OASISResult<IOAPPTemplateDNA>();
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
                        OASISResult<IOAPPTemplate> loadOAPPTemplateResult = await LoadOAPPTemplateAsync(OAPPTemplateDNA.Id);

                        if (loadOAPPTemplateResult != null && loadOAPPTemplateResult.Result != null && !loadOAPPTemplateResult.IsError)
                        {                     
                            OASISResult<bool> validateVersionResult = ValidateVersion(OAPPTemplateDNA.Version, loadOAPPTemplateResult.Result.OAPPTemplateDNA.Version, fullPathToOAPPTemplate, OAPPTemplateDNA.PublishedOn == DateTime.MinValue);

                            if (validateVersionResult != null && validateVersionResult.Result && !validateVersionResult.IsError)
                            {
                                //TODO: Maybe add check to make sure the DNA has not been tampered with?
                                loadOAPPTemplateResult.Result.OAPPTemplateDNA.Version = OAPPTemplateDNA.Version; //Set the new version set in the DNA (JSON file).
                                OAPPTemplateDNA = loadOAPPTemplateResult.Result.OAPPTemplateDNA; //Make sure it has not been tampered with by using the stored version.
                                OAPPTemplateDNA.VersionSequence++;
                                OAPPTemplateDNA.NumberOfVersions++;

                                string publishedOAPPTemplateFileName = string.Concat(OAPPTemplateDNA.Name, "_v", OAPPTemplateDNA.Version, ".oapptemplate");

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
                                if (OAPPTemplateDNA.Version != "1.0.0")
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
                                    OASISResult<IEnumerable<IOAPPTemplate>> templatesResult = await LoadOAPPTemplateVersionsAsync(OAPPTemplateDNA.Id, providerType);

                                    if (templatesResult != null && templatesResult.Result != null && !templatesResult.IsError)
                                    {
                                        //Update all versions with the total number of versions.
                                        foreach (IOAPPTemplate template in templatesResult.Result)
                                        {
                                            template.OAPPTemplateDNA.NumberOfVersions = OAPPTemplateDNA.NumberOfVersions;
                                            OASISResult<IOAPPTemplate> templateSaveResult = await SaveOAPPTemplateAsync(template, avatarId, providerType);

                                            if (!(templateSaveResult != null && templateSaveResult.Result != null && !templateSaveResult.IsError))
                                                OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} Error occured updating the NumberOfVersions for OAPP Template with Id {template.Id} for provider {Enum.GetName(typeof(ProviderType), providerType)}. Reason: {templateSaveResult.Message}");
                                        }
                                    }

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
                                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling ValidateResult. Reason: {validateVersionResult.Message}");
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

        public async Task<OASISResult<IOAPPTemplate>> UnPublishOAPPTemplateAsync(IOAPPTemplateDNA OAPPTemplateDNA, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IOAPPTemplate> result = new OASISResult<IOAPPTemplate>();
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
                    result.Message = "OAPP Template Unpublised";
                    result.Result = oappResult.Result;
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured saving the OAPP Template with the SaveOAPPTemplateAsync method, reason: {oappResult.Message}");
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured loading the OAPP Template with the LoadOAPPTemplateAsync method, reason: {oappResult.Message}");

            return result;
        }

        public OASISResult<IOAPPTemplate> UnPublishOAPPTemplate(IOAPPTemplateDNA OAPPTemplateDNA, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IOAPPTemplate> result = new OASISResult<IOAPPTemplate>();
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
                    result.Message = "OAPP Template Unpublised";
                    result.Result = oappResult.Result;
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured saving the OAPP Template with the SaveOAPPTemplate method, reason: {oappResult.Message}");
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured loading the OAPP Template with the LoadOAPPTemplate method, reason: {oappResult.Message}");

            return result;
        }

        public async Task<OASISResult<IOAPPTemplate>> UnPublishOAPPTemplateAsync(IOAPPTemplate OAPPTemplate, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IOAPPTemplate> result = new OASISResult<IOAPPTemplate>();
            string errorMessage = "Error occured in UnPublishOAPPTemplateAsync. Reason: ";

            OAPPTemplate.OAPPTemplateDNA.PublishedOn = DateTime.MinValue;
            OAPPTemplate.OAPPTemplateDNA.PublishedByAvatarId = Guid.Empty;
            OAPPTemplate.OAPPTemplateDNA.PublishedByAvatarUsername = "";

            OASISResult<IOAPPTemplate> oappResult = await SaveOAPPTemplateAsync(OAPPTemplate, avatarId, providerType);

            if (oappResult != null && oappResult.Result != null && !oappResult.IsError)
            { 
                result.Result = oappResult.Result; //ConvertOAPPTemplateToOAPPTemplateDNA(OAPPTemplate);
                result.Message = "OAPPTemplate Unpublised";
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured saving the OAPP Template with the SaveOAPPTemplateAsync method, reason: {oappResult.Message}");

            return result;
        }

        public OASISResult<IOAPPTemplate> UnPublishOAPPTemplate(IOAPPTemplate OAPPTemplate, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IOAPPTemplate> result = new OASISResult<IOAPPTemplate>();
            string errorMessage = "Error occured in UnPublishOAPPTemplate. Reason: ";

            OAPPTemplate.OAPPTemplateDNA.PublishedOn = DateTime.MinValue;
            OAPPTemplate.OAPPTemplateDNA.PublishedByAvatarId = Guid.Empty;
            OAPPTemplate.OAPPTemplateDNA.PublishedByAvatarUsername = "";

            OASISResult<IOAPPTemplate> oappResult = SaveOAPPTemplate(OAPPTemplate, avatarId, providerType);

            if (oappResult != null && oappResult.Result != null && !oappResult.IsError)
            {
                result.Result = oappResult.Result; //ConvertOAPPTemplateToOAPPTemplateDNA(OAPPTemplate);
                result.Message = "OAPP Template Unpublised";
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured saving the OAPP Template with the SaveOAPPTemplate method, reason: {oappResult.Message}");

            return result;
        }

        public async Task<OASISResult<IOAPPTemplate>> UnPublishOAPPTemplateAsync(Guid OAPPTemplateId, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IOAPPTemplate> result = new OASISResult<IOAPPTemplate>();
            OASISResult<IOAPPTemplate> loadResult = await LoadOAPPTemplateAsync(OAPPTemplateId, providerType);

            if (loadResult != null && loadResult.Result != null && !loadResult.IsError)
                result = await UnPublishOAPPTemplateAsync(loadResult.Result, avatarId, providerType);
            else
                OASISErrorHandling.HandleError(ref result, $"Error occured in UnPublishOAPPTemplateAsync loading the OAPPTemplate with the LoadOAPPTemplateAsync method, reason: {loadResult.Message}");

            return result;
        }

        public OASISResult<IOAPPTemplate> UnPublishOAPPTemplate(Guid OAPPTemplateId, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IOAPPTemplate> result = new OASISResult<IOAPPTemplate>();
            OASISResult<IOAPPTemplate> loadResult = LoadOAPPTemplate(OAPPTemplateId, providerType);

            if (loadResult != null && loadResult.Result != null && !loadResult.IsError)
                result = UnPublishOAPPTemplate(loadResult.Result, avatarId, providerType);
            else
                OASISErrorHandling.HandleError(ref result, $"Error occured in UnPublishOAPPTemplate loading the OAPPTemplate with the LoadOAPPTemplate method, reason: {loadResult.Message}");

            return result;
        }

        public async Task<OASISResult<IDownloadedOAPPTemplate>> DownloadOAPPTemplateAsync(Guid avatarId, IOAPPTemplate OAPPTemplate, string fullDownloadPath, bool reInstall = false, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IDownloadedOAPPTemplate> result = new OASISResult<IDownloadedOAPPTemplate>();
            string errorMessage = "Error occured in OAPPTemplateManager.DownloadOAPPTemplateAsync. Reason: ";
            DownloadedOAPPTemplate downloadedOAPPTemplate = null;

            try
            {
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

                    if (!reInstall)
                    {
                        OASISResult<IAvatar> avatarResult = await AvatarManager.Instance.LoadAvatarAsync(avatarId, false, true, providerType);

                        if (avatarResult != null && !avatarResult.IsError && avatarResult.Result != null)
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
                            OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadAvatarAsync method. Reason: {avatarResult.Message}");
                    }

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
                catch (Exception ex)
                {
                    CLIEngine.DisposeProgressBar(false);
                    Console.WriteLine("");
                    OASISErrorHandling.HandleError(ref result, $"An error occured downloading the OAPP Template from cloud storage. Reason: {ex}");
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
                    OASISResult<IOAPPTemplate> oappTemplateLoadResult = await LoadOAPPTemplateAsync(OAPPTemplateDNAResult.Result.Id, providerType);

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

                                if (downloadedOAPPTemplateResult != null && !downloadedOAPPTemplateResult.IsError && downloadedOAPPTemplateResult.Result != null && downloadedOAPPTemplateResult.Result.Count() == 1)
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
                                    DownloadedOAPPTemplateId = downloadedOAPPTemplate.Id,
                                    Active = "1"
                                    //OAPPTemplateVersion = OAPPTemplateDNA.Version
                                };

                                installedOAPPTemplate.MetaData["Version"] = OAPPTemplateDNA.Version;
                                installedOAPPTemplate.MetaData["OAPPTemplateId"] = OAPPTemplateDNA.Id;
                                
                                await UpdateInstallCountsAsync(installedOAPPTemplate, OAPPTemplateDNA, avatarId, result, errorMessage, providerType);
                            }
                            else
                            {
                                OASISResult<IInstalledOAPPTemplate> installedOAPPTemplateResult = await LoadInstalledOAPPTemplateAsync(avatarId, OAPPTemplateDNAResult.Result.Id, OAPPTemplateDNAResult.Result.VersionSequence, providerType);

                                if (installedOAPPTemplateResult != null && installedOAPPTemplateResult.Result != null && !installedOAPPTemplateResult.IsError)
                                {
                                    installedOAPPTemplate = (InstalledOAPPTemplate)installedOAPPTemplateResult.Result;
                                    installedOAPPTemplate.Active = "1";
                                    installedOAPPTemplate.UninstalledBy = Guid.Empty;
                                    installedOAPPTemplate.UninstalledByAvatarUsername = "";
                                    installedOAPPTemplate.UninstalledOn = DateTime.MinValue;
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
                    OASISResult<IOAPPTemplate> oappResult = LoadOAPPTemplate(OAPPTemplateDNAResult.Result.Id, providerType);

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

        public async Task<OASISResult<IInstalledOAPPTemplate>> InstallOAPPTemplateAsync(Guid avatarId, Guid OAPPTemplateId, string fullInstallPath, string fullDownloadPath = "", bool createOAPPTemplateDirectory = true, bool reInstall = false, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IInstalledOAPPTemplate> result = new OASISResult<IInstalledOAPPTemplate>();
            OASISResult<IOAPPTemplate> OAPPTemplateResult = await LoadOAPPTemplateAsync(OAPPTemplateId, providerType);

            if (OAPPTemplateResult != null && !OAPPTemplateResult.IsError && OAPPTemplateResult.Result != null)
                result = await DownloadAndInstallOAPPTemplateAsync(avatarId, OAPPTemplateResult.Result, fullInstallPath, fullDownloadPath, createOAPPTemplateDirectory, reInstall, providerType);
            else
            {
                OASISErrorHandling.HandleError(ref result, $"Error occured in OAPPTemplateManager.InstallOAPPTemplateAsync loading the OAPP Template with the LoadOAPPTemplateAsync method, reason: {result.Message}");
                OnOAPPTemplateInstallStatusChanged?.Invoke(this, new OAPPTemplateInstallStatusEventArgs() { Status = Enums.OAPPTemplateInstallStatus.Error, ErrorMessage = result.Message });
            }

            return result;
        }

        public OASISResult<IInstalledOAPPTemplate> InstallOAPPTemplate(Guid avatarId, Guid OAPPTemplateId, string fullInstallPath, string fullDownloadPath = "", bool createOAPPTemplateDirectory = true, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IInstalledOAPPTemplate> result = new OASISResult<IInstalledOAPPTemplate>();
            OASISResult<IOAPPTemplate> OAPPTemplateResult = LoadOAPPTemplate(OAPPTemplateId, providerType);

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
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured attempting to delete the OAPP Template folder ({installedOAPPTemplate.InstalledPath}) Reason: {ex.Message}");
            }

            if (!result.IsError)
            {
                OASISResult<IAvatar> avatarResult = await AvatarManager.Instance.LoadAvatarAsync(avatarId, false, true, providerType, 0);

                if (avatarResult != null && avatarResult.Result != null && !avatarResult.IsError)
                {
                    installedOAPPTemplate.UninstalledBy = avatarId;
                    installedOAPPTemplate.UninstalledOn = DateTime.Now;
                    installedOAPPTemplate.UninstalledByAvatarUsername = avatarResult.Result.Username;
                    installedOAPPTemplate.MetaData["Active"] = "0";

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
            }

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
                    installedOAPPTemplate.MetaData["Active"] = "0";

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

        public async Task<OASISResult<IInstalledOAPPTemplate>> UnInstallOAPPTemplateAsync(Guid OAPPTemplateId, int versionSequence, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IInstalledOAPPTemplate> result = new OASISResult<IInstalledOAPPTemplate>();
            string errorMessage = "Error occured in OAPPTemplateManager.UnInstallOAPPTemplateAsync. Reason: ";

            return await UninstallOAPPTemplateAsync(await Data.LoadHolonByMetaDataAsync<InstalledOAPPTemplate>(new Dictionary<string, string>()
            {
                { "OAPPTemplateId", OAPPTemplateId.ToString() },
                { "VersionSequene", versionSequence.ToString() }

            }, MetaKeyValuePairMatchMode.All, HolonType.InstalledOAPPTemplate, true, true, 0, true, 0, false, HolonType.All, providerType), avatarId, errorMessage, providerType);
        }

        public OASISResult<IInstalledOAPPTemplate> UnInstallOAPPTemplate(Guid OAPPTemplateId, int versionSequence, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IInstalledOAPPTemplate> result = new OASISResult<IInstalledOAPPTemplate>();
            string errorMessage = "Error occured in OAPPTemplateManager.UnInstallOAPPTemplate. Reason: ";

            return UninstallOAPPTemplate(Data.LoadHolonByMetaData<InstalledOAPPTemplate>(new Dictionary<string, string>()
            {
                { "OAPPTemplateId", OAPPTemplateId.ToString() },
                { "VersionSequene", versionSequence.ToString() }

            }, MetaKeyValuePairMatchMode.All, HolonType.InstalledOAPPTemplate, true, true, 0, true, false, HolonType.All, 0, providerType), avatarId, errorMessage, providerType);
        }

        public async Task<OASISResult<IInstalledOAPPTemplate>> UnInstallOAPPTemplateAsync(Guid OAPPTemplateId, string version, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IInstalledOAPPTemplate> result = new OASISResult<IInstalledOAPPTemplate>();
            string errorMessage = "Error occured in OAPPTemplateManager.UnInstallOAPPTemplateAsync. Reason: ";

            return await UninstallOAPPTemplateAsync(await Data.LoadHolonByMetaDataAsync<InstalledOAPPTemplate>(new Dictionary<string, string>()
            {
                { "OAPPTemplateId", OAPPTemplateId.ToString() },
                { "Version", version }

            }, MetaKeyValuePairMatchMode.All, HolonType.InstalledOAPPTemplate, true, true, 0, true, 0, false, HolonType.All, providerType), avatarId, errorMessage, providerType);
        }

        public OASISResult<IInstalledOAPPTemplate> UnInstallOAPPTemplate(Guid OAPPTemplateId, string version, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IInstalledOAPPTemplate> result = new OASISResult<IInstalledOAPPTemplate>();
            string errorMessage = "Error occured in OAPPTemplateManager.UnInstallOAPPTemplateAsync. Reason: ";

            return UninstallOAPPTemplate(Data.LoadHolonByMetaData<InstalledOAPPTemplate>(new Dictionary<string, string>()
            {
                { "OAPPTemplateId", OAPPTemplateId.ToString() },
                { "Version", version }

            }, MetaKeyValuePairMatchMode.All, HolonType.InstalledOAPPTemplate, true, true, 0, true, false, HolonType.All, 0, providerType), avatarId, errorMessage, providerType);
        }

        public async Task<OASISResult<IInstalledOAPPTemplate>> UnInstallOAPPTemplateAsync(string OAPPTemplateName, int versionSequence, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IInstalledOAPPTemplate> result = new OASISResult<IInstalledOAPPTemplate>();
            string errorMessage = "Error occured in OAPPTemplateManager.UnInstallOAPPTemplateAsync. Reason: ";

            return await UninstallOAPPTemplateAsync(await Data.LoadHolonByMetaDataAsync<InstalledOAPPTemplate>(new Dictionary<string, string>()
            {
                { "OAPPTemplateName", OAPPTemplateName },
                { "VersionSequence", versionSequence.ToString() }

            }, MetaKeyValuePairMatchMode.All, HolonType.InstalledOAPPTemplate, true, true, 0, true, 0, false, HolonType.All, providerType), avatarId, errorMessage, providerType);
        }

        public OASISResult<IInstalledOAPPTemplate> UnInstallOAPPTemplate(string OAPPTemplateName, int versionSequence, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IInstalledOAPPTemplate> result = new OASISResult<IInstalledOAPPTemplate>();
            string errorMessage = "Error occured in OAPPTemplateManager.UnInstallOAPPTemplate. Reason: ";

            return UninstallOAPPTemplate(Data.LoadHolonByMetaData<InstalledOAPPTemplate>(new Dictionary<string, string>()
            {
                { "OAPPTemplateName", OAPPTemplateName },
                { "VersionSequene", versionSequence.ToString() }

            }, MetaKeyValuePairMatchMode.All, HolonType.InstalledOAPPTemplate, true, true, 0, true, false, HolonType.All), avatarId, errorMessage, providerType);
        }

        public async Task<OASISResult<IInstalledOAPPTemplate>> UnInstallOAPPTemplateAsync(string OAPPTemplateName, string version, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IInstalledOAPPTemplate> result = new OASISResult<IInstalledOAPPTemplate>();
            string errorMessage = "Error occured in OAPPTemplateManager.UnInstallOAPPTemplate. Reason: ";

            return UninstallOAPPTemplate(await Data.LoadHolonByMetaDataAsync<InstalledOAPPTemplate>(new Dictionary<string, string>()
            {
                { "OAPPTemplateName", OAPPTemplateName },
                { "Version", version }

            }, MetaKeyValuePairMatchMode.All, HolonType.InstalledOAPPTemplate, true, true, 0, true, 0, false, HolonType.All, providerType), avatarId, errorMessage, providerType);
        }

        public OASISResult<IInstalledOAPPTemplate> UnInstallOAPPTemplate(string OAPPTemplateName, string version, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IInstalledOAPPTemplate> result = new OASISResult<IInstalledOAPPTemplate>();
            string errorMessage = "Error occured in OAPPTemplateManager.UnInstallOAPPTemplate. Reason: ";

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

        public async Task<OASISResult<bool>> IsOAPPTemplateInstalledAsync(Guid avatarId, Guid OAPPTemplateId, int versionSequence, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<bool> result = new OASISResult<bool>();
            string errorMessage = "Error occured in OAPPTemplateManager.IsOAPPTemplateInstalledAsync. Reason: ";

            OASISResult<InstalledOAPPTemplate> installedOAPPTemplatesResult = await Data.LoadHolonByMetaDataAsync<InstalledOAPPTemplate>(new Dictionary<string, string>()
            {
                { "OAPPTemplateId", OAPPTemplateId.ToString() },
                { "VersionSequene", versionSequence.ToString() },
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
                { "VersionSequene", versionSequence.ToString() },
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
                { "Version", version.ToString() },
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
                { "Version", versionSequence.ToString() },
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
                { "VersionSequene", versionSequence.ToString() },
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
                { "VersionSequene", versionSequence.ToString() },
                { "Active", "1" }

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
            string errorMessage = "Error occured in OAPPTemplateManager.LoadInstalledOAPPTemplate. Reason: ";
            OASISResult<InstalledOAPPTemplate> installedOAPPTemplatesResult = Data.LoadHolonByMetaData<InstalledOAPPTemplate>(new Dictionary<string, string>()
            {
                { "OAPPTemplateId", OAPPTemplateId.ToString() },
                { "VersionSequene", versionSequence.ToString() },
                { "Active", "1" }

            }, MetaKeyValuePairMatchMode.All, HolonType.InstalledOAPPTemplate, true, true, 0, true, false, HolonType.All, 0, providerType);

            if (installedOAPPTemplatesResult != null && !installedOAPPTemplatesResult.IsError && installedOAPPTemplatesResult.Result != null)
                result.Result = installedOAPPTemplatesResult.Result;
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonByMetaData. Reason: {installedOAPPTemplatesResult.Message}");

            return result;
        }

        public async Task<OASISResult<IInstalledOAPPTemplate>> LoadInstalledOAPPTemplateAsync(Guid avatarId, string OAPPTemplateName, int versionSequence = 0, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IInstalledOAPPTemplate> result = new OASISResult<IInstalledOAPPTemplate>();
            string errorMessage = "Error occured in OAPPTemplateManager.LoadInstalledOAPPTemplateAsync. Reason: ";
            OASISResult<InstalledOAPPTemplate> installedOAPPTemplatesResult = await Data.LoadHolonByMetaDataAsync<InstalledOAPPTemplate>(new Dictionary<string, string>()
            {
                { "OAPPTemplateName", OAPPTemplateName },
                { "VersionSequene", versionSequence.ToString() },
                { "Active", "1" }

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
            string errorMessage = "Error occured in OAPPTemplateManager.LoadInstalledOAPPTemplate. Reason: ";
            OASISResult<InstalledOAPPTemplate> installedOAPPTemplatesResult = Data.LoadHolonByMetaData<InstalledOAPPTemplate>(new Dictionary<string, string>()
            {
                { "OAPPTemplateId", OAPPTemplateName },
                { "VersionSequene", versionSequence.ToString() },
                { "Active", "1" }

            }, MetaKeyValuePairMatchMode.All, HolonType.InstalledOAPPTemplate, true, true, 0, true, false, HolonType.All, 0, providerType);

            if (installedOAPPTemplatesResult != null && !installedOAPPTemplatesResult.IsError && installedOAPPTemplatesResult.Result != null)
                result.Result = installedOAPPTemplatesResult.Result;
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonByMetaData. Reason: {installedOAPPTemplatesResult.Message}");

            return result;
        }

        public async Task<OASISResult<IInstalledOAPPTemplate>> LoadInstalledOAPPTemplateAsync(Guid avatarId, Guid OAPPTemplateId, string version, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IInstalledOAPPTemplate> result = new OASISResult<IInstalledOAPPTemplate>();
            string errorMessage = "Error occured in OAPPTemplateManager.LoadInstalledOAPPTemplateAsync. Reason: ";
            OASISResult<InstalledOAPPTemplate> installedOAPPTemplatesResult = await Data.LoadHolonByMetaDataAsync<InstalledOAPPTemplate>(new Dictionary<string, string>()
            {
                { "OAPPTemplateId", OAPPTemplateId.ToString() },
                { "Version", version},
                { "Active", "1" }

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
            string errorMessage = "Error occured in OAPPTemplateManager.LoadInstalledOAPPTemplate. Reason: ";
            OASISResult<InstalledOAPPTemplate> installedOAPPTemplatesResult = Data.LoadHolonByMetaData<InstalledOAPPTemplate>(new Dictionary<string, string>()
            {
                { "OAPPTemplateId", OAPPTemplateId.ToString() },
                { "Version", version },
                { "Active", "1" }

            }, MetaKeyValuePairMatchMode.All, HolonType.InstalledOAPPTemplate, true, true, 0, true, false, HolonType.All, 0, providerType);

            if (installedOAPPTemplatesResult != null && !installedOAPPTemplatesResult.IsError && installedOAPPTemplatesResult.Result != null)
                result.Result = installedOAPPTemplatesResult.Result;
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonByMetaData. Reason: {installedOAPPTemplatesResult.Message}");

            return result;
        }

        public async Task<OASISResult<IInstalledOAPPTemplate>> LoadInstalledOAPPTemplateAsync(Guid avatarId, string OAPPTemplateName, string version, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IInstalledOAPPTemplate> result = new OASISResult<IInstalledOAPPTemplate>();
            string errorMessage = "Error occured in OAPPTemplateManager.LoadInstalledOAPPTemplateAsync. Reason: ";
            OASISResult<InstalledOAPPTemplate> installedOAPPTemplatesResult = await Data.LoadHolonByMetaDataAsync<InstalledOAPPTemplate>(new Dictionary<string, string>()
            {
                { "OAPPTemplateName", OAPPTemplateName },
                { "Version", version },
                { "Active", "1" }

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
            string errorMessage = "Error occured in OAPPTemplateManager.LoadInstalledOAPPTemplate. Reason: ";
            OASISResult<InstalledOAPPTemplate> installedOAPPTemplatesResult = Data.LoadHolonByMetaData<InstalledOAPPTemplate>(new Dictionary<string, string>()
            {
                { "OAPPTemplateId", OAPPTemplateName },
                { "Version", version },
                { "Active", "1" }

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
                    template.OAPPTemplateDNA.TotalInstalls = totalDownloads;
                    OASISResult<IOAPPTemplate> templateSaveResult = await SaveOAPPTemplateAsync(template, avatarId, providerType);

                    if (!(templateSaveResult != null && templateSaveResult.Result != null && !templateSaveResult.IsError))
                        OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} Error occured updating the TotalDownloads for OAPP Template with Id {template.Id} for provider {Enum.GetName(typeof(ProviderType), providerType)}. Reason: {templateSaveResult.Message}");
                }

                OAPPTemplateDNA.TotalDownloads = totalDownloads;
                downloadedOAPPTemplate.OAPPTemplateDNA.TotalInstalls = totalDownloads;
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

        private OASISResult<IEnumerable<IOAPPTemplate>> FilterResults(OASISResult<IEnumerable<OAPPTemplate>> results, int version)
        {
            OASISResult<IEnumerable<IOAPPTemplate>> result = new OASISResult<IEnumerable<IOAPPTemplate>>();

            //0 means we want the latest version.
            if (results.Result != null && !result.IsError)
            {
                if (version == 0)
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

            result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(results, result);
            return result;
        }

        private OASISResult<bool> ValidateVersion(string dnaVersion, string storedVersion, string fullPathToOAPPTemplateFolder, bool firstPublish)
        {
            OASISResult<bool> result = new OASISResult<bool>();
            int dnaVersionInt = 0;
            int stotedVersionInt = 0;

            if (!firstPublish)
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