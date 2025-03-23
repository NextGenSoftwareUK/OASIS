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


        //public async Task<OASISResult<IOAPPTemplateDNA>> CreateOAPPTemplateAsync(string name, string description, OAPPTemplateType OAPPTemplateType, Guid avatarId, string fullPathToOAPPTemplate, string fullPathToOASISRuntime, string fullpathToSTARODKRuntime, ProviderType providerType = ProviderType.Default)
        public async Task<OASISResult<IOAPPTemplateDNA>> CreateOAPPTemplateAsync(string name, string description, OAPPTemplateType OAPPTemplateType, Guid avatarId, string fullPathToOAPPTemplate, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IOAPPTemplateDNA> result = new OASISResult<IOAPPTemplateDNA>();
            string errorMessage = "Error occured in OAPPTemplateManager.CreateOAPPTemplateAsync, Reason:";

            try
            {
                //if (!Directory.Exists(fullPathToOASISRuntime))
                //{
                //    OASISErrorHandling.HandleError(ref result, $"{errorMessage} The fullPathToOASISRuntime param passed in was not found!");
                //    return result;
                //}

                //if (!Directory.Exists(fullpathToSTARODKRuntime))
                //{
                //    OASISErrorHandling.HandleError(ref result, $"{errorMessage} The fullpathToSTARODKRuntime param passed in was not found!");
                //    return result;
                //}

                OAPPTemplate OAPPTemplate = new OAPPTemplate()
                {
                    Id = Guid.NewGuid(),
                    Name = name,
                    Description = description
                };

                OAPPTemplate.MetaData["OAPPTemplateType"] = Enum.GetName(typeof(OAPPTemplateType), OAPPTemplateType);
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
                            //if (Directory.Exists(fullPathToOASISRuntime))
                            //{
                            //    DirectoryInfo directoryInfo = new DirectoryInfo(fullPathToOASISRuntime);

                            //    if (directoryInfo != null)
                            //    {
                            //        Directory.CreateDirectory(Path.Combine(fullPathToOAPPTemplate, directoryInfo.Name));
                            //        DirectoryHelper.CopyFilesRecursively(fullPathToOASISRuntime, Path.Combine(fullPathToOAPPTemplate, directoryInfo.Name));
                            //    }
                            //}
                            //else
                            //    OASISErrorHandling.HandleError(ref result, $"{errorMessage} The fullPathToOASISRuntime param passed in was not found!");

                            //directoryInfo = new DirectoryInfo(fullpathToSTARODKRuntime);

                            //if (directoryInfo != null)
                            //{
                            //    Directory.CreateDirectory(Path.Combine(fullPathToOAPPTemplate, directoryInfo.Name));
                            //    DirectoryHelper.CopyFilesRecursively(fullpathToSTARODKRuntime, Path.Combine(fullPathToOAPPTemplate, directoryInfo.Name));
                            //}

                            result.Result = OAPPTemplateDNA;
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
                        result.Result = OAPPTemplateDNA;
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

        public async Task<OASISResult<IEnumerable<IOAPPTemplate>>> LoadAllOAPPTemplatesAsync(OAPPTemplateType OAPPTemplateType = OAPPTemplateType.All, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<IOAPPTemplate>> result = new OASISResult<IEnumerable<IOAPPTemplate>>();
            OASISResult<IEnumerable<OAPPTemplate>> loadHolonsResult = null;

            if (OAPPTemplateType == OAPPTemplateType.All)
                loadHolonsResult = await Data.LoadAllHolonsAsync<OAPPTemplate>(HolonType.OAPPTemplate, true, true, 0, true, false, HolonType.All, 0, providerType);
            else
                loadHolonsResult = await Data.LoadHolonsForParentByMetaDataAsync<OAPPTemplate>("OAPPTemplateType", Enum.GetName(typeof(OAPPTemplateType), OAPPTemplateType));

            result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(loadHolonsResult, result);
            result.Result = loadHolonsResult.Result;
            return result;
        }

        public OASISResult<IEnumerable<IOAPPTemplate>> LoadAllOAPPTemplates(OAPPTemplateType OAPPTemplateType = OAPPTemplateType.All, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<IOAPPTemplate>> result = new OASISResult<IEnumerable<IOAPPTemplate>>();
            OASISResult<IEnumerable<OAPPTemplate>> loadHolonsResult = null;

            if (OAPPTemplateType == OAPPTemplateType.All)
                loadHolonsResult = Data.LoadAllHolons<OAPPTemplate>(HolonType.OAPPTemplate, true, true, 0, true, false, HolonType.All, 0, providerType);
            else
                loadHolonsResult = Data.LoadHolonsForParentByMetaData<OAPPTemplate>("OAPPTemplateType", Enum.GetName(typeof(OAPPTemplateType), OAPPTemplateType));

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

                        OAPPTemplateDNA.Versions++;

                        WriteOAPPTemplateDNA(OAPPTemplateDNA, fullPathToOAPPTemplate);
                        OnOAPPTemplatePublishStatusChanged?.Invoke(this, new OAPPTemplatePublishStatusEventArgs() { OAPPTemplateDNA = OAPPTemplateDNA, Status = Enums.OAPPTemplatePublishStatus.Compressing });

                        if (generateOAPPTemplateBinary)
                        {
                            if (File.Exists(OAPPTemplateDNA.OAPPTemplatePublishedPath))
                                File.Delete(OAPPTemplateDNA.OAPPTemplatePublishedPath);

                            tempPath = Path.GetTempPath();
                            tempPath = Path.Combine(tempPath, readOAPPTemplateDNAResult.Result.Name);

                            ZipFile.CreateFromDirectory(fullPathToOAPPTemplate, tempPath);
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
            finally
            {
                if (Directory.Exists(tempPath))
                    Directory.Delete(tempPath);
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

        public OASISResult<IOAPPTemplateDNA> PublishOAPPTemplate(string fullPathToOAPPTemplate, string launchTarget, Guid avatarId, string fullPathToPublishTo = "", bool registerOnSTARNET = true, bool generateOAPPTemplateBinary = true, bool uploadOAPPTemplateToCloud = false, ProviderType providerType = ProviderType.Default, ProviderType oappBinaryProviderType = ProviderType.IPFSOASIS)
        {
            OASISResult<IOAPPTemplateDNA> result = new OASISResult<IOAPPTemplateDNA>();
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

                        OAPPTemplateDNA.Versions++;

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
                    result.Message = "OAPP Template Unpublised";
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured saving the OAPP Template with the SaveOAPPTemplateAsync method, reason: {oappResult.Message}");
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured loading the OAPP Template with the LoadOAPPTemplateAsync method, reason: {oappResult.Message}");

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
                    result.Message = "OAPP Template Unpublised";
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured saving the OAPP Template with the SaveOAPPTemplate method, reason: {oappResult.Message}");
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured loading the OAPP Template with the LoadOAPPTemplate method, reason: {oappResult.Message}");

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
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured saving the OAPP Template with the SaveOAPPTemplateAsync method, reason: {oappResult.Message}");

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
                result.Message = "OAPP Template Unpublised";
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured saving the OAPP Template with the SaveOAPPTemplate method, reason: {oappResult.Message}");

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

        //public async Task<OASISResult<IInstalledOAPPTemplate>> InstallOAPPTemplateAsync(Guid avatarId, string fullPathToPublishedOAPPTemplateFile, string fullInstallPath, bool createOAPPTemplateDirectory = true, string OAPPName = "", ProviderType providerType = ProviderType.Default)
        public async Task<OASISResult<IInstalledOAPPTemplate>> InstallOAPPTemplateAsync(Guid avatarId, string fullPathToPublishedOAPPTemplateFile, string fullInstallPath, bool createOAPPTemplateDirectory = true, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IInstalledOAPPTemplate> result = new OASISResult<IInstalledOAPPTemplate>();
            string errorMessage = "Error occured in OAPPTemplateManager.InstallOAPPTemplateAsync. Reason: ";
            IOAPPTemplateDNA OAPPTemplateDNA = null;
            string tempPath = "";

            try
            {
                tempPath = Path.GetTempPath();
                tempPath = Path.Combine(tempPath, "OAPP Template");

                //Unzip
                OnOAPPTemplateInstallStatusChanged?.Invoke(this, new OAPPTemplateInstallStatusEventArgs() { Status = Enums.OAPPTemplateInstallStatus.Decompressing });
                ZipFile.ExtractToDirectory(fullPathToPublishedOAPPTemplateFile, tempPath, Encoding.Default, true);
                OASISResult<IOAPPTemplateDNA> OAPPTemplateDNAResult = await ReadOAPPTemplateDNAAsync(tempPath);

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

                        //Directory.CreateDirectory(fullInstallPath);
                        Directory.Move(tempPath, fullInstallPath);
                        //Directory.Delete(tempPath);

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
                                OAPPTemplateDNA.Downloads++;
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


        public async Task<OASISResult<IDownloadedOAPPTemplate>> DownloadOAPPTemplateAsync(Guid avatarId, IOAPPTemplate OAPPTemplate, string fullDownloadPath, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IDownloadedOAPPTemplate> result = new OASISResult<IDownloadedOAPPTemplate>();
            string errorMessage = "Error occured in OAPPTemplateManager.DownloadOAPPTemplateAsync. Reason: ";
            bool isFullDownloadPathTemp = false;

            try
            {
                //if (string.IsNullOrEmpty(fullDownloadPath))
                //{
                //    string tempPath = Path.GetTempPath();
                //    fullDownloadPath = Path.Combine(tempPath, string.Concat(OAPPTemplate.Name, ".oapptemplate"));
                //    isFullDownloadPathTemp = true;
                //}

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

                        string publishedOAPPTemplateFileName = string.Concat(OAPPTemplate.Name, ".oapptemplate");
                        OnOAPPTemplateInstallStatusChanged?.Invoke(this, new OAPPTemplateInstallStatusEventArgs() { OAPPTemplateDNA = OAPPTemplate.OAPPTemplateDNA, Status = Enums.OAPPTemplateInstallStatus.Downloading });
                        await storage.DownloadObjectAsync(GOOGLE_CLOUD_BUCKET_NAME, publishedOAPPTemplateFileName, fileStream, downloadObjectOptions, progress: progressReporter);

                        _progress = 100;
                        OnOAPPTemplateDownloadStatusChanged?.Invoke(this, new OAPPTemplateDownloadProgressEventArgs() { Progress = _progress, Status = Enums.OAPPTemplateDownloadStatus.Downloading });
                        CLIEngine.DisposeProgressBar(false);
                        Console.WriteLine("");
                        fileStream.Close();
                    }

                    result.Result = new DownloadedOAPPTemplate()
                    {
                        OAPPTemplateDNA = OAPPTemplate.OAPPTemplateDNA,
                        DownloadedBy = avatarId,
                        DownloadedOn = DateTime.Now,
                        DownloadedPath = fullDownloadPath
                    };
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
             //   OnOAPPTemplateInstallStatusChanged?.Invoke(this, new OAPPTemplateInstallStatusEventArgs() { OAPPTemplateDNA = OAPPTemplate.OAPPTemplateDNA, Status = Enums.OAPPTemplateInstallStatus.Error, ErrorMessage = result.Message });

            return result;
        }

        public async Task<OASISResult<IInstalledOAPPTemplate>> DownloadAndInstallOAPPTemplateAsync(Guid avatarId, IOAPPTemplate OAPPTemplate, string fullInstallPath,  string fullDownloadPath = "", bool createOAPPTemplateDirectory = true, ProviderType providerType = ProviderType.Default)
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
                    result = await InstallOAPPTemplateAsync(avatarId, fullDownloadPath, fullInstallPath, createOAPPTemplateDirectory, providerType);
                }
                else
                {
                    OASISResult<IDownloadedOAPPTemplate> downloadResult = await DownloadOAPPTemplateAsync(avatarId, OAPPTemplate, fullDownloadPath, providerType);

                    if (downloadResult != null && downloadResult.Result != null && !downloadResult.IsError)
                        result = await InstallOAPPTemplateAsync(avatarId, fullDownloadPath, fullInstallPath, createOAPPTemplateDirectory, providerType);
                    else
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured downloading the OAPP Template with the DownloadOAPPTemplateAsync method, reason: {downloadResult.Message}");

                    result.Result.DownloadedOAPPTemplate = downloadResult.Result;
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

        public async Task<OASISResult<IInstalledOAPPTemplate>> InstallOAPPTemplateAsync(Guid avatarId, Guid OAPPTemplateId, string fullInstallPath, string fullDownloadPath = "", bool createOAPPTemplateDirectory = true, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IInstalledOAPPTemplate> result = new OASISResult<IInstalledOAPPTemplate>();
            OASISResult<IOAPPTemplate> OAPPTemplateResult = await LoadOAPPTemplateAsync(OAPPTemplateId, providerType);

            if (OAPPTemplateResult != null && !OAPPTemplateResult.IsError && OAPPTemplateResult.Result != null)
                result = await InstallOAPPTemplateAsync(avatarId, OAPPTemplateResult.Result, fullInstallPath, fullDownloadPath, createOAPPTemplateDirectory, providerType);
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

        public OASISResult<IInstalledOAPPTemplate> OpenOAPPTemplateFolder(Guid avatarId, IInstalledOAPPTemplate OAPPTemplate)
        {
            OASISResult<IInstalledOAPPTemplate> result = new OASISResult<IInstalledOAPPTemplate>();
            string errorMessage = "An error occured in OAPPTemplateManager.OpenOAPPTemplateFolder. Reason:";

            if (OAPPTemplate != null)
            {
                try
                {
                    Process.Start("explorer.exe", OAPPTemplate.InstalledPath);
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

        public async Task<OASISResult<IInstalledOAPPTemplate>> OpenOAPPTemplateFolderAsync(Guid avatarId, Guid OAPPTemplateId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IInstalledOAPPTemplate> result = new OASISResult<IInstalledOAPPTemplate>();
            string errorMessage = "An error occured in OAPPTemplateManager.OpenOAPPTemplateFolderAsync. Reason:";
            result = await LoadInstalledOAPPTemplateAsync(avatarId, OAPPTemplateId);

            if (result != null && !result.IsError && result.Result != null)
                OpenOAPPTemplateFolder(avatarId, result.Result);
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

                    _progress = Convert.ToInt32(((double)progress.BytesDownloaded / (double)_fileLength) * 100);
                   // _progress = Convert.ToInt32(_fileLength / progress.BytesDownloaded);
                    OnOAPPTemplateDownloadStatusChanged?.Invoke(this, new OAPPTemplateDownloadProgressEventArgs() { Progress = _progress, Status = Enums.OAPPTemplateDownloadStatus.Downloading });
                    break;

                case Google.Apis.Download.DownloadStatus.Failed:
                    OnOAPPTemplateDownloadStatusChanged?.Invoke(this, new OAPPTemplateDownloadProgressEventArgs() { Progress = _progress, Status = Enums.OAPPTemplateDownloadStatus.Error, ErrorMessage = progress.Exception.ToString() });
                    break;
            }
        }
    }
}