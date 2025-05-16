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
    public class OAPPSystemManagerBase : PublishManagerBase //: COSMICManagerBase//, IOAPPSystemManagerBase
    {
        private int _progress = 0;
        private long _fileLength = 0;
        //private const string GOOGLE_CLOUD_BUCKET_NAME = "oasis_OAPPSystemHolons";

        public OAPPSystemManagerBase(Guid avatarId, OASISDNA OASISDNA = null) : base(avatarId, OASISDNA) { }
        public OAPPSystemManagerBase(IOASISStorageProvider OASISStorageProvider, Guid avatarId, OASISDNA OASISDNA = null) : base(OASISStorageProvider, avatarId, OASISDNA) { }

        public delegate void OAPPSystemHolonPublishStatusChanged(object sender, OAPPSystemHolonPublishStatusEventArgs e);
        public delegate void OAPPSystemHolonInstallStatusChanged(object sender, OAPPSystemHolonInstallStatusEventArgs e);
        public delegate void OAPPSystemHolonUploadStatusChanged(object sender, OAPPSystemHolonUploadProgressEventArgs e);
        public delegate void OAPPSystemHolonDownloadStatusChanged(object sender, OAPPSystemHolonDownloadStatusChanged e);

        /// <summary>
        /// Fired when there is a change in the OAPP publish status.
        /// </summary>
        public event OAPPSystemHolonPublishStatusChanged OnOAPPSystemHolonPublishStatusChanged;

        /// <summary>
        /// Fired when there is a change to the OAPP Install status.
        /// </summary>
        public event OAPPSystemHolonInstallStatusChanged OnOAPPSystemHolonInstallStatusChanged;

        /// <summary>
        /// Fired when there is a change in the OAPP upload status.
        /// </summary>
        public event OAPPSystemHolonUploadStatusChanged OnOAPPSystemHolonUploadStatusChanged;

        /// <summary>
        /// Fired when there is a change in the OAPP download status.
        /// </summary>
        public event OAPPSystemHolonDownloadStatusChanged OnOAPPSystemHolonDownloadStatusChanged;

        public HolonType HolonType { get; set; } = HolonType.OAPPSystemHolon;
        public string OAPPSystemHolonUIName { get; set; } = "{OAPPSystemHolonUIName}";
        public string OAPPSystemHolonIdName { get; set; } = "OAPPSystemHolonId";
        public string OAPPSystemHolonNameName { get; set; } = "OAPPSystemHolonName";
        public string OAPPSystemHolonTypeName { get; set; } = "OAPPSystemHolonType";
        public string OAPPSystemHolonFileExtention { get; set; } = "OAPPSystemHolon";
        public string OAPPSystemHolonGoogleBucket { get; set; } = "oasis_OAPPSystemHolons";

        public virtual async Task<OASISResult<T>> CreateAsync<T>(string name, string description, object holonSubType, Guid avatarId, string fullPathToT, ProviderType providerType = ProviderType.Default) where T : IOAPPSystemHolon, new()
        {
            OASISResult<T> result = new OASISResult<T>();
            string errorMessage = "Error occured in OAPPSystemManagerBase.CreateAsync, Reason:";

            try
            {
                if (Directory.Exists(fullPathToT))
                {
                    if (CLIEngine.GetConfirmation($"The directory {fullPathToT} already exists! Would you like to delete it?"))
                    {
                        Console.WriteLine("");
                        Directory.Delete(fullPathToT, true);
                    }
                    else
                    {
                        Console.WriteLine("");
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} The directory {fullPathToT} already exists! Please either delete it or choose a different name.");
                        return result;
                    }
                }

                T holon = new T()
                {
                    Id = Guid.NewGuid(),
                    Name = name,
                    Description = description
                };

                holon.MetaData[OAPPSystemHolonIdName] = holon.Id.ToString();
                holon.MetaData[OAPPSystemHolonNameName] = holon.Name;
                //T.MetaData[OAPPSystemHolonTypeName] = Enum.GetName(typeof(OAPPSystemHolonType), OAPPSystemHolonType);

                Type holonSubTypeType = holonSubType.GetType();
                holon.MetaData[OAPPSystemHolonTypeName] = Enum.GetName(holonSubTypeType, holonSubType);
                holon.MetaData["Version"] = "1.0.0";
                holon.MetaData["VersionSequence"] = 1;
                holon.MetaData["Active"] = "1";
                holon.MetaData["CreatedByAvatarId"] = avatarId.ToString();

                //T.MetaData["LatestVersion"] = "1";

                OASISResult<IAvatar> avatarResult = await AvatarManager.Instance.LoadAvatarAsync(avatarId, false, true, providerType);

                if (avatarResult != null && avatarResult.Result != null && !avatarResult.IsError)
                {
                    OAPPSystemHolonDNA OAPPSystemHolonDNA = new OAPPSystemHolonDNA()
                    {
                        Id = holon.Id,
                        Name = name,
                        Description = description,
                        //OAPPSystemHolonType = OAPPSystemHolonType,
                        CreatedByAvatarId = avatarId,
                        CreatedByAvatarUsername = avatarResult.Result.Username,
                        CreatedOn = DateTime.Now,
                        Version = "1.0.0",
                        STARODKVersion = OASISBootLoader.OASISBootLoader.STARODKVersion,
                        OASISVersion = OASISBootLoader.OASISBootLoader.OASISVersion,
                        COSMICVersion = OASISBootLoader.OASISBootLoader.COSMICVersion,
                        DotNetVersion = OASISBootLoader.OASISBootLoader.DotNetVersion,
                        SourcePath = fullPathToT
                    };

                    OASISResult<bool> writeOAPPSystemHolonDNAResult = await WriteOAPPSystemHolonDNAAsync(OAPPSystemHolonDNA, fullPathToT);

                    if (writeOAPPSystemHolonDNAResult != null && writeOAPPSystemHolonDNAResult.Result && !writeOAPPSystemHolonDNAResult.IsError)
                    {
                        holon.OAPPSystemHolonDNA = OAPPSystemHolonDNA;
                        OASISResult<T> saveHolonResult = await Data.SaveHolonAsync<T>(holon, avatarId, true, true, 0, true, false, providerType);

                        if (saveHolonResult != null && saveHolonResult.Result != null && !saveHolonResult.IsError)
                        {
                            result.Result = saveHolonResult.Result;
                            result.Message = $"Successfully created the {OAPPSystemHolonUIName} on the {Enum.GetName(typeof(ProviderType), providerType)} provider by AvatarId {avatarId} for {OAPPSystemHolonTypeName} {Enum.GetName(holonSubTypeType, holonSubType)}.";
                        }
                        else
                            OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured saving the {OAPPSystemHolonUIName} to the {Enum.GetName(typeof(ProviderType), providerType)} provider. Reason: {saveHolonResult.Message}");
                    }
                    else
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured writing the {OAPPSystemHolonUIName} DNA. Reason: {writeOAPPSystemHolonDNAResult.Message}");
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadAvatarAsync on {Enum.GetName(typeof(ProviderType), providerType)} provider. Reason: {avatarResult.Message}");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured saving the {OAPPSystemHolonUIName} to the {Enum.GetName(typeof(ProviderType), providerType)} provider. Reason: {ex}");
            }

            return result;
        }

        public virtual OASISResult<T> Create<T>(string name, string description, object holonSubType, Guid avatarId, string fullPathToT, ProviderType providerType = ProviderType.Default) where T : IOAPPSystemHolon, new()
        {
            OASISResult<T> result = new OASISResult<T>();
            string errorMessage = "Error occured in OAPPSystemManagerBase.Create, Reason:";

            try
            {
                if (Directory.Exists(fullPathToT))
                {
                    if (CLIEngine.GetConfirmation($"The directory {fullPathToT} already exists! Would you like to delete it?"))
                    {
                        Console.WriteLine("");
                        Directory.Delete(fullPathToT, true);
                    }
                    else
                    {
                        Console.WriteLine("");
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} The directory {fullPathToT} already exists! Please either delete it or choose a different name.");
                        return result;
                    }
                }

                T holon = new T()
                {
                    Id = Guid.NewGuid(),
                    Name = name,
                    Description = description
                };

                holon.MetaData[OAPPSystemHolonIdName] = holon.Id.ToString();
                holon.MetaData[OAPPSystemHolonNameName] = holon.Name;
                //T.MetaData[OAPPSystemHolonTypeName] = Enum.GetName(typeof(OAPPSystemHolonType), OAPPSystemHolonType);

                Type holonSubTypeType = holonSubType.GetType();
                holon.MetaData[OAPPSystemHolonTypeName] = Enum.GetName(holonSubTypeType, holonSubType);
                holon.MetaData["Version"] = "1.0.0";
                holon.MetaData["VersionSequence"] = 1;
                holon.MetaData["Active"] = "1";
                holon.MetaData["CreatedByAvatarId"] = avatarId.ToString();

                //T.MetaData["LatestVersion"] = "1";

                OASISResult<IAvatar> avatarResult = AvatarManager.Instance.LoadAvatar(avatarId, false, true, providerType);

                if (avatarResult != null && avatarResult.Result != null && !avatarResult.IsError)
                {
                    OAPPSystemHolonDNA OAPPSystemHolonDNA = new OAPPSystemHolonDNA()
                    {
                        Id = holon.Id,
                        Name = name,
                        Description = description,
                        //OAPPSystemHolonType = OAPPSystemHolonType,
                        CreatedByAvatarId = avatarId,
                        CreatedByAvatarUsername = avatarResult.Result.Username,
                        CreatedOn = DateTime.Now,
                        Version = "1.0.0",
                        STARODKVersion = OASISBootLoader.OASISBootLoader.STARODKVersion,
                        OASISVersion = OASISBootLoader.OASISBootLoader.OASISVersion,
                        COSMICVersion = OASISBootLoader.OASISBootLoader.COSMICVersion,
                        DotNetVersion = OASISBootLoader.OASISBootLoader.DotNetVersion,
                        SourcePath = fullPathToT
                    };

                    OASISResult<bool> writeOAPPSystemHolonDNAResult = WriteOAPPSystemHolonDNA(OAPPSystemHolonDNA, fullPathToT);

                    if (writeOAPPSystemHolonDNAResult != null && writeOAPPSystemHolonDNAResult.Result && !writeOAPPSystemHolonDNAResult.IsError)
                    {
                        holon.OAPPSystemHolonDNA = OAPPSystemHolonDNA;
                        OASISResult<T> saveHolonResult = Data.SaveHolon<T>(holon, avatarId, true, true, 0, true, false, providerType);

                        if (saveHolonResult != null && saveHolonResult.Result != null && !saveHolonResult.IsError)
                        {
                            result.Result = saveHolonResult.Result;
                            result.Message = $"Successfully created the {OAPPSystemHolonUIName} on the {Enum.GetName(typeof(ProviderType), providerType)} provider by AvatarId {avatarId} for {OAPPSystemHolonTypeName} {Enum.GetName(holonSubTypeType, holonSubType)}.";
                        }
                        else
                            OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured saving the {OAPPSystemHolonUIName} to the {Enum.GetName(typeof(ProviderType), providerType)} provider. Reason: {saveHolonResult.Message}");
                    }
                    else
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured writing the {OAPPSystemHolonUIName} DNA. Reason: {writeOAPPSystemHolonDNAResult.Message}");
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadAvatar on {Enum.GetName(typeof(ProviderType), providerType)} provider. Reason: {avatarResult.Message}");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured saving the {OAPPSystemHolonUIName} to the {Enum.GetName(typeof(ProviderType), providerType)} provider. Reason: {ex}");
            }

            return result;
        }
        
        #region COSMICManagerBase
        public async Task<OASISResult<T>> SaveAsync<T>(T holon, Guid avatarId, ProviderType providerType = ProviderType.Default) where T : IOAPPSystemHolon, new()
        {
            OASISResult<T> result = new OASISResult<T>();

            if (!Directory.Exists(holon.OAPPSystemHolonDNA.SourcePath))
                Directory.CreateDirectory(holon.OAPPSystemHolonDNA.SourcePath);

            holon.MetaData["OAPPSystemHolonDNAJSON"] = JsonSerializer.Serialize(holon.OAPPSystemHolonDNA);

            OASISResult<T> saveResult = await SaveHolonAsync<T>(holon, avatarId, providerType, "OAPPSystemManagerBase.SaveAsync<T>");
            result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(saveResult, result);
            result.Result = saveResult.Result;
            return result;
        }

        public OASISResult<T> Save<T>(T holon, Guid avatarId, ProviderType providerType = ProviderType.Default) where T : IOAPPSystemHolon, new()
        {
            OASISResult<T> result = new OASISResult<T>();

            if (!Directory.Exists(holon.OAPPSystemHolonDNA.SourcePath))
                Directory.CreateDirectory(holon.OAPPSystemHolonDNA.SourcePath);

            holon.MetaData["OAPPSystemHolonDNAJSON"] = JsonSerializer.Serialize(holon.OAPPSystemHolonDNA);

            OASISResult<T> saveResult = SaveHolon<T>(holon, avatarId, providerType, "OAPPSystemManagerBase.Save<T>");
            result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(saveResult, result);
            result.Result = saveResult.Result;
            return result;
        }

        //public async Task<OASISResult<IInstalledOAPPSystemHolon>> SaveAsync(IInstalledOAPPSystemHolon holon, Guid avatarId, ProviderType providerType = ProviderType.Default)
        //{
        //    OASISResult<IInstalledOAPPSystemHolon> result = new OASISResult<IInstalledOAPPSystemHolon>();

        //    if (!Directory.Exists(holon.OAPPSystemHolonDNA.SourcePath))
        //        Directory.CreateDirectory(holon.OAPPSystemHolonDNA.SourcePath);

        //    holon.MetaData["OAPPSystemHolonDNAJSON"] = JsonSerializer.Serialize(holon.OAPPSystemHolonDNA);

        //    //OASISResult<T> saveResult = await SaveHolonAsync<T>(holon, avatarId, providerType, "OAPPSystemManagerBase.SaveAsync<T>");
        //    OASISResult<IHolon> saveResult = await holon.SaveAsync(providerType: providerType);
        //    result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(saveResult, result);
        //    result.Result = saveResult.Result;
        //    return result;
        //}

        //public OASISResult<T> Save<T>(T holon, Guid avatarId, ProviderType providerType = ProviderType.Default) where T : IOAPPSystemHolon, new()
        //{
        //    OASISResult<T> result = new OASISResult<T>();

        //    if (!Directory.Exists(holon.OAPPSystemHolonDNA.SourcePath))
        //        Directory.CreateDirectory(holon.OAPPSystemHolonDNA.SourcePath);

        //    holon.MetaData["OAPPSystemHolonDNAJSON"] = JsonSerializer.Serialize(holon.OAPPSystemHolonDNA);

        //    OASISResult<T> saveResult = SaveHolon<T>(holon, avatarId, providerType, "OAPPSystemManagerBase.Save<T>");
        //    result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(saveResult, result);
        //    result.Result = saveResult.Result;
        //    return result;
        //}

        public async Task<OASISResult<T>> LoadAsync<T>(Guid id, Guid avatarId, HolonType holonType, int version = 0, ProviderType providerType = ProviderType.Default) where T : IOAPPSystemHolon, new()
        {
            OASISResult<T> result = new OASISResult<T>();
            OASISResult<IEnumerable<T>> loadResult = await Data.LoadHolonsByMetaDataAsync<T>(OAPPSystemHolonIdName, id.ToString(), holonType, true, true, 0, true, false, 0, HolonType.All, 0, providerType);
            OASISResult<IEnumerable<T>> filterdResult = FilterResultsForVersion(avatarId, loadResult, false, version);

            if (filterdResult != null && filterdResult.Result != null && !filterdResult.IsError)
                result.Result = filterdResult.Result.FirstOrDefault();
            else
                OASISErrorHandling.HandleError(ref result, $"Error occured in LoadAsync<T> loading the {OAPPSystemHolonUIName} with Id {id} from the {Enum.GetName(typeof(ProviderType), providerType)} provider. Reason: {filterdResult.Message}");

            return result;
        }

        public OASISResult<T> Load<T>(Guid id, Guid avatarId, HolonType holonType, int version = 0, ProviderType providerType = ProviderType.Default) where T : IOAPPSystemHolon, new()
        {
            OASISResult<T> result = new OASISResult<T>();
            OASISResult<IEnumerable<T>> loadResult = Data.LoadHolonsByMetaData<T>(OAPPSystemHolonIdName, id.ToString(), holonType, true, true, 0, true, false, 0, HolonType.All, 0, providerType);
            OASISResult<IEnumerable<T>> filterdResult = FilterResultsForVersion(avatarId, loadResult, false, version);

            if (filterdResult != null && filterdResult.Result != null && !filterdResult.IsError)
                result.Result = filterdResult.Result.FirstOrDefault();
            else
                OASISErrorHandling.HandleError(ref result, $"Error occured in Load<T> loading the {OAPPSystemHolonUIName} with Id {id} from the {Enum.GetName(typeof(ProviderType), providerType)} provider. Reason: {filterdResult.Message}");

            return result;
        }

        //public async Task<OASISResult<IEnumerable<T>>> LoadAllAsync<T>(Guid avatarId, HolonType holonType, OAPPSystemHolonType OAPPSystemHolonType = OAPPSystemHolonType.All, bool showAllVersions = false, int version = 0, ProviderType providerType = ProviderType.Default)
        public async Task<OASISResult<IEnumerable<T>>> LoadAllAsync<T>(Guid avatarId, HolonType holonType, object itemType, bool loadAllTTypes = true, bool showAllVersions = false, int version = 0, ProviderType providerType = ProviderType.Default) where T : IOAPPSystemHolon, new()
        {
            OASISResult<IEnumerable<T>> result = new OASISResult<IEnumerable<T>>();
            OASISResult<IEnumerable<T>> loadHolonsResult = null;

            //if (OAPPSystemHolonType == OAPPSystemHolonType.All)
            if (loadAllTTypes)
                loadHolonsResult = await Data.LoadAllHolonsAsync<T>(holonType, true, true, 0, true, false, HolonType.All, 0, providerType);
            else
                loadHolonsResult = await Data.LoadHolonsByMetaDataAsync<T>(OAPPSystemHolonTypeName, Enum.GetName(itemType.GetType(), itemType), HolonType.All, true, true, 0, true, false, 0, HolonType.All, 0);

            return FilterResultsForVersion(avatarId, loadHolonsResult, showAllVersions, version);
        }

        public OASISResult<IEnumerable<T>> LoadAll<T>(Guid avatarId, HolonType holonType, object itemType, bool loadAllTTypes = true, bool showAllVersions = false, int version = 0, ProviderType providerType = ProviderType.Default) where T : IOAPPSystemHolon, new()
        {
            OASISResult<IEnumerable<T>> result = new OASISResult<IEnumerable<T>>();
            OASISResult<IEnumerable<T>> loadHolonsResult = null;

            //if (OAPPSystemHolonType == OAPPSystemHolonType.All)
            if (loadAllTTypes)
                loadHolonsResult = Data.LoadAllHolons<T>(holonType, true, true, 0, true, false, HolonType.All, 0, providerType);
            else
                loadHolonsResult = Data.LoadHolonsByMetaData<T>(OAPPSystemHolonTypeName, Enum.GetName(itemType.GetType(), itemType), HolonType.All, true, true, 0, true, false, 0, HolonType.All, 0);

            return FilterResultsForVersion(avatarId, loadHolonsResult, showAllVersions, version);
        }

        public async Task<OASISResult<IEnumerable<T>>> LoadAllForAvatarAsync<T>(Guid avatarId, HolonType holonType, bool showAllVersions = false, int version = 0, ProviderType providerType = ProviderType.Default) where T : IOAPPSystemHolon, new()
        {
            OASISResult<IEnumerable<T>> result = new OASISResult<IEnumerable<T>>();
            OASISResult<IEnumerable<T>> loadHolonsResult = await Data.LoadHolonsByMetaDataAsync<T>(new Dictionary<string, string>()
            {
                { "CreatedByAvatarId", avatarId.ToString() },
                { "Active", "1" }

            }, MetaKeyValuePairMatchMode.All, holonType, providerType: providerType);

            return FilterResultsForVersion(avatarId, loadHolonsResult, showAllVersions, version);
        }

        public async Task<OASISResult<IEnumerable<T>>> LoadAllForAvatar<T>(Guid avatarId, HolonType holonType, bool showAllVersions = false, int version = 0, ProviderType providerType = ProviderType.Default) where T : IOAPPSystemHolon, new()
        {
            OASISResult<IEnumerable<T>> result = new OASISResult<IEnumerable<T>>();
            OASISResult<IEnumerable<T>> loadHolonsResult = await Data.LoadHolonsByMetaDataAsync<T>(new Dictionary<string, string>()
            {
                { "CreatedByAvatarId", avatarId.ToString() },
                { "Active", "1" }

            }, MetaKeyValuePairMatchMode.All, holonType, providerType: providerType);

            return FilterResultsForVersion(avatarId, loadHolonsResult, showAllVersions, version);
        }

        public async Task<OASISResult<IEnumerable<T>>> SearchAsync<T>(string searchTerm, HolonType holonType, Guid avatarId, bool searchOnlyForCurrentAvatar = true, bool showAllVersions = false, int version = 0, ProviderType providerType = ProviderType.Default) where T : IOAPPSystemHolon, new()
        {
            OASISResult<IEnumerable<T>> result = new OASISResult<IEnumerable<T>>();
            OASISResult<IEnumerable<T>> loadHolonsResult = await SearchHolonsAsync<T>(searchTerm, avatarId, searchOnlyForCurrentAvatar, providerType, "OAPPSystemManagerBase.SearchAsync", holonType);
            return FilterResultsForVersion(avatarId, loadHolonsResult, showAllVersions, version);
        }

        public OASISResult<IEnumerable<T>> Search<T>(string searchTerm, Guid avatarId, HolonType holonType, bool searchOnlyForCurrentAvatar = true, bool showAllVersions = false, int version = 0, ProviderType providerType = ProviderType.Default) where T : IOAPPSystemHolon, new()
        {
            OASISResult<IEnumerable<T>> result = new OASISResult<IEnumerable<T>>();
            OASISResult<IEnumerable<T>> loadHolonsResult = SearchHolons<T>(searchTerm, avatarId, searchOnlyForCurrentAvatar, providerType, "OAPPSystemManagerBase.Search", holonType);
            return FilterResultsForVersion(avatarId, loadHolonsResult, showAllVersions, version);
        }

        public async Task<OASISResult<T>> DeleteAsync<T>(Guid id, Guid avatarId, HolonType holonType, int version, bool softDelete = true, bool deleteDownload = true, bool deleteInstall = true, ProviderType providerType = ProviderType.Default) where T : IOAPPSystemHolon, new()
        {
            OASISResult<T> result = new OASISResult<T>();
            string errorMessage = "Error occured in DeleteAsync. Reason: ";
            OASISResult<T> loadResult = await LoadAsync<T>(id, avatarId, holonType, version, providerType);

            if (loadResult != null && loadResult.Result != null && !loadResult.IsError)
                result = await DeleteAsync<T>(loadResult.Result, avatarId, version, softDelete, deleteDownload, deleteInstall, providerType);
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured loading the {OAPPSystemHolonUIName} with Id {id} from the {Enum.GetName(typeof(ProviderType), providerType)} provider. Reason: {loadResult.Message}");
 
            return result;
        }

        public OASISResult<T> Delete<T>(Guid id, Guid avatarId, HolonType holonType, int version, bool softDelete = true, bool deleteDownload = true, bool deleteInstall = true, ProviderType providerType = ProviderType.Default) where T : IOAPPSystemHolon, new()
        {
            OASISResult<T> result = new OASISResult<T>();
            string errorMessage = "Error occured in Delete. Reason: ";
            OASISResult<T> loadResult = Load<T>(id, avatarId, holonType, version, providerType);

            if (loadResult != null && loadResult.Result != null && !loadResult.IsError)
                result = Delete<T>(loadResult.Result, avatarId, version, softDelete, deleteDownload, deleteInstall, providerType);
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured loading the {OAPPSystemHolonUIName} with Id {id} from the {Enum.GetName(typeof(ProviderType), providerType)} provider. Reason: {loadResult.Message}");

            return result;
        }

        public async Task<OASISResult<T>> DeleteAsync<T>(IOAPPSystemHolon oappSystemHolon, Guid avatarId, int version, bool softDelete = true, bool deleteDownload = true, bool deleteInstall = true, ProviderType providerType = ProviderType.Default) where T : IOAPPSystemHolon, new()
        {
            OASISResult<T> result = new OASISResult<T>();
            string errorMessage = "Error occured in DeleteOAPPSystemHolonAsync. Reason: ";

            if (oappSystemHolon.OAPPSystemHolonDNA.CreatedByAvatarId != avatarId)
            {
                OASISErrorHandling.HandleError(ref result, $"Permission Denied. You did not create this {OAPPSystemHolonUIName}. Error occured in DeleteOAPPSystemHolonAsync loading the {OAPPSystemHolonUIName} with Id {oappSystemHolon.OAPPSystemHolonDNA.CreatedByAvatarId} from the {Enum.GetName(typeof(ProviderType), providerType)} provider. Reason: The {OAPPSystemHolonUIName} was not created by the Avatar with Id {avatarId}.");
                return result;
            }

            try
            {
                if (!string.IsNullOrEmpty(oappSystemHolon.OAPPSystemHolonDNA.SourcePath) && Directory.Exists(oappSystemHolon.OAPPSystemHolonDNA.SourcePath))
                    Directory.Delete(oappSystemHolon.OAPPSystemHolonDNA.SourcePath, true);
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} Error occured attempting to delete the T folder {oappSystemHolon.OAPPSystemHolonDNA.SourcePath}. PLEASE DELETE MANUALLY! Reason: {e}");
            }

            try
            {
                if (!string.IsNullOrEmpty(oappSystemHolon.OAPPSystemHolonDNA.PublishedPath) && File.Exists(oappSystemHolon.OAPPSystemHolonDNA.PublishedPath))
                    File.Delete(oappSystemHolon.OAPPSystemHolonDNA.PublishedPath);
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} Error occured attempting to delete the T Published folder {oappSystemHolon.OAPPSystemHolonDNA.PublishedPath}. PLEASE DELETE MANUALLY! Reason: {e}");
            }

            if (deleteDownload || deleteInstall)
            {
                OASISResult<IInstalledOAPPSystemHolon> installedOAPPSystemHolonResult = await LoadInstalledOAPPSystemHolonAsync(avatarId, oappSystemHolon.OAPPSystemHolonDNA.Id, version, providerType);

                if (installedOAPPSystemHolonResult != null && installedOAPPSystemHolonResult.Result != null && !installedOAPPSystemHolonResult.IsError)
                {
                    try
                    {
                        if (deleteDownload && !string.IsNullOrEmpty(installedOAPPSystemHolonResult.Result.DownloadedPath) && File.Exists(installedOAPPSystemHolonResult.Result.DownloadedPath))
                            File.Delete(installedOAPPSystemHolonResult.Result.DownloadedPath);
                    }
                    catch (Exception e)
                    {
                        OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} Error occured attempting to delete the T Download folder {installedOAPPSystemHolonResult.Result.DownloadedPath}. PLEASE DELETE MANUALLY! Reason: {e}");
                    }

                    try
                    {
                        if (deleteInstall && !string.IsNullOrEmpty(installedOAPPSystemHolonResult.Result.InstalledPath) && Directory.Exists(installedOAPPSystemHolonResult.Result.InstalledPath))
                            Directory.Delete(installedOAPPSystemHolonResult.Result.InstalledPath, true);
                    }
                    catch (Exception e)
                    {
                        OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} Error occured attempting to delete the T Installed folder {installedOAPPSystemHolonResult.Result.InstalledPath}. PLEASE DELETE MANUALLY! Reason: {e}");
                    }

                    if (deleteInstall)
                    {
                        OASISResult<T> deleteInstalledOAPPSystemHolonHolonResult = await DeleteHolonAsync<T>(installedOAPPSystemHolonResult.Result.Id, avatarId, softDelete, providerType, "OAPPSystemManagerBase.DeleteOAPPSystemHolonAsync");

                        if (!(deleteInstalledOAPPSystemHolonHolonResult != null && deleteInstalledOAPPSystemHolonHolonResult.Result != null && !deleteInstalledOAPPSystemHolonHolonResult.IsError))
                            OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} Error occured deleting the Installed T holon with id {installedOAPPSystemHolonResult.Result.Id} calling DeleteHolonAsync. Reason: {deleteInstalledOAPPSystemHolonHolonResult.Message}");
                    }

                    if (deleteDownload)
                    {
                        OASISResult<T> deleteDownloadedOAPPSystemHolonHolonResult = await DeleteHolonAsync<T>(installedOAPPSystemHolonResult.Result.DownloadedOAPPSystemHolonHolonId, avatarId, softDelete, providerType, "OAPPSystemManagerBase.DeleteOAPPSystemHolonAsync");

                        if (!(deleteDownloadedOAPPSystemHolonHolonResult != null && deleteDownloadedOAPPSystemHolonHolonResult.Result != null && !deleteDownloadedOAPPSystemHolonHolonResult.IsError))
                            OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} Error occured deleting the Downloaded T holon with id {installedOAPPSystemHolonResult.Result.DownloadedOAPPSystemHolonHolonId} calling DeleteHolonAsync. Reason: {deleteDownloadedOAPPSystemHolonHolonResult.Message}");
                    }
                }
            }

            OASISResult<T> deleteHolonResult = await DeleteHolonAsync<T>(oappSystemHolon.Id, avatarId, softDelete, providerType, "OAPPSystemManagerBase.DeleteOAPPSystemHolonAsync");

            if (!(deleteHolonResult != null && deleteHolonResult.Result != null && !deleteHolonResult.IsError))
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured deleting the T holon with id {oappSystemHolon.Id} calling DeleteHolonAsync. Reason: {deleteHolonResult.Message}");

            result.Result = deleteHolonResult.Result;
            return result;
        }

        public OASISResult<T> Delete<T>(IOAPPSystemHolon oappSystemHolon, Guid avatarId, int version, bool softDelete = true, bool deleteDownload = true, bool deleteInstall = true, ProviderType providerType = ProviderType.Default) where T : IOAPPSystemHolon, new()
        {
            OASISResult<T> result = new OASISResult<T>();
            string errorMessage = "Error occured in DeleteOAPPSystemHolon. Reason: ";

            if (oappSystemHolon.OAPPSystemHolonDNA.CreatedByAvatarId != avatarId)
            {
                OASISErrorHandling.HandleError(ref result, $"Permission Denied. You did not create this {OAPPSystemHolonUIName}. Error occured in DeleteOAPPSystemHolonAsync loading the {OAPPSystemHolonUIName} with Id {oappSystemHolon.OAPPSystemHolonDNA.CreatedByAvatarId} from the {Enum.GetName(typeof(ProviderType), providerType)} provider. Reason: The {OAPPSystemHolonUIName} was not created by the Avatar with Id {avatarId}.");
                return result;
            }

            try
            {
                if (!string.IsNullOrEmpty(oappSystemHolon.OAPPSystemHolonDNA.SourcePath) && Directory.Exists(oappSystemHolon.OAPPSystemHolonDNA.SourcePath))
                    Directory.Delete(oappSystemHolon.OAPPSystemHolonDNA.SourcePath, true);
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} Error occured attempting to delete the {OAPPSystemHolonUIName} Source folder {oappSystemHolon.OAPPSystemHolonDNA.SourcePath}. PLEASE DELETE MANUALLY! Reason: {e}");
            }

            try
            {
                if (!string.IsNullOrEmpty(oappSystemHolon.OAPPSystemHolonDNA.PublishedPath) && File.Exists(oappSystemHolon.OAPPSystemHolonDNA.PublishedPath))
                    File.Delete(oappSystemHolon.OAPPSystemHolonDNA.PublishedPath);
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} Error occured attempting to delete the {OAPPSystemHolonUIName} Published folder {oappSystemHolon.OAPPSystemHolonDNA.PublishedPath}. PLEASE DELETE MANUALLY! Reason: {e}");
            }

            if (deleteDownload || deleteInstall)
            {
                OASISResult<IInstalledOAPPSystemHolon> installedOAPPSystemHolonResult = LoadInstalledOAPPSystemHolon(avatarId, oappSystemHolon.OAPPSystemHolonDNA.Id, version, providerType);

                if (installedOAPPSystemHolonResult != null && installedOAPPSystemHolonResult.Result != null && !installedOAPPSystemHolonResult.IsError)
                {
                    try
                    {
                        if (deleteDownload && !string.IsNullOrEmpty(installedOAPPSystemHolonResult.Result.DownloadedPath) && File.Exists(installedOAPPSystemHolonResult.Result.DownloadedPath))
                            File.Delete(installedOAPPSystemHolonResult.Result.DownloadedPath);
                    }
                    catch (Exception e)
                    {
                        OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} Error occured attempting to delete the {OAPPSystemHolonUIName} Download folder {installedOAPPSystemHolonResult.Result.DownloadedPath}. PLEASE DELETE MANUALLY! Reason: {e}");
                    }

                    try
                    {
                        if (deleteInstall && !string.IsNullOrEmpty(installedOAPPSystemHolonResult.Result.InstalledPath) && Directory.Exists(installedOAPPSystemHolonResult.Result.InstalledPath))
                            Directory.Delete(installedOAPPSystemHolonResult.Result.InstalledPath, true);
                    }
                    catch (Exception e)
                    {
                        OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} Error occured attempting to delete the {OAPPSystemHolonUIName} Installed folder {installedOAPPSystemHolonResult.Result.InstalledPath}. PLEASE DELETE MANUALLY! Reason: {e}");
                    }

                    if (deleteInstall)
                    {
                        OASISResult<T> deleteInstalledOAPPSystemHolonHolonResult = DeleteHolon<T>(installedOAPPSystemHolonResult.Result.Id, avatarId, softDelete, providerType, "OAPPSystemManagerBase.DeleteOAPPSystemHolonAsync");

                        if (!(deleteInstalledOAPPSystemHolonHolonResult != null && deleteInstalledOAPPSystemHolonHolonResult.Result != null && !deleteInstalledOAPPSystemHolonHolonResult.IsError))
                            OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} Error occured deleting the Installed {OAPPSystemHolonUIName} holon with id {installedOAPPSystemHolonResult.Result.Id} calling DeleteHolonAsync. Reason: {deleteInstalledOAPPSystemHolonHolonResult.Message}");
                    }

                    if (deleteDownload)
                    {
                        OASISResult<T> deleteDownloadedOAPPSystemHolonHolonResult = DeleteHolon<T>(installedOAPPSystemHolonResult.Result.DownloadedOAPPSystemHolonHolonId, avatarId, softDelete, providerType, "OAPPSystemManagerBase.DeleteOAPPSystemHolonAsync");

                        if (!(deleteDownloadedOAPPSystemHolonHolonResult != null && deleteDownloadedOAPPSystemHolonHolonResult.Result != null && !deleteDownloadedOAPPSystemHolonHolonResult.IsError))
                            OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} Error occured deleting the Downloaded {OAPPSystemHolonUIName} holon with id {installedOAPPSystemHolonResult.Result.DownloadedOAPPSystemHolonHolonId} calling DeleteHolonAsync. Reason: {deleteDownloadedOAPPSystemHolonHolonResult.Message}");
                    }
                }
            }

            OASISResult<T> deleteHolonResult = DeleteHolon<T>(oappSystemHolon.Id, avatarId, softDelete, providerType, "OAPPSystemManagerBase.DeleteOAPPSystemHolonAsync");

            if (!(deleteHolonResult != null && deleteHolonResult.Result != null && !deleteHolonResult.IsError))
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured deleting the T holon with id {oappSystemHolon.Id} calling DeleteHolonAsync. Reason: {deleteHolonResult.Message}");

            result.Result = deleteHolonResult.Result;
            return result;
        }
        #endregion

        /*
        #region PublishManagerBase
        public async Task<OASISResult<IOAPPSystemHolon>> PublishOAPPSystemHolonAsync(Guid OAPPSystemHolonId, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IOAPPSystemHolon> result = new OASISResult<IOAPPSystemHolon>();
            OASISResult<T> saveResult = await PublishHolonAsync<T>(OAPPSystemHolonId, avatarId, "OAPPSystemManagerBase.PublishOAPPSystemHolonAsync", providerType);
            result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(saveResult, result);
            result.Result = saveResult.Result;
            return result;
        }

        public OASISResult<IOAPPSystemHolon> PublishOAPPSystemHolon(Guid OAPPSystemHolonId, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IOAPPSystemHolon> result = new OASISResult<IOAPPSystemHolon>();
            OASISResult<T> saveResult = PublishHolon<T>(OAPPSystemHolonId, avatarId, "OAPPSystemManagerBase.PublishOAPPSystemHolon", providerType);
            result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(saveResult, result);
            result.Result = saveResult.Result;
            return result;
        }

        public async Task<OASISResult<IOAPPSystemHolon>> PublishOAPPSystemHolonAsync(IOAPPSystemHolon T, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IOAPPSystemHolon> result = new OASISResult<IOAPPSystemHolon>();
            OASISResult<T> saveResult = await PublishHolonAsync<T>(T, avatarId, "OAPPSystemManagerBase.PublishOAPPSystemHolonAsync", providerType);
            result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(saveResult, result);
            result.Result = saveResult.Result;
            return result;
        }

        public OASISResult<IOAPPSystemHolon> PublishOAPPSystemHolon(IOAPPSystemHolon T, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IOAPPSystemHolon> result = new OASISResult<IOAPPSystemHolon>();
            OASISResult<T> saveResult = PublishHolon<T>(T, avatarId, "OAPPSystemManagerBase.PublishOAPPSystemHolon", providerType);
            result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(saveResult, result);
            result.Result = saveResult.Result;
            return result;
        }

        public async Task<OASISResult<IOAPPSystemHolon>> UnpublishOAPPSystemHolonAsync(Guid OAPPSystemHolonId, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IOAPPSystemHolon> result = new OASISResult<IOAPPSystemHolon>();
            OASISResult<T> saveResult = await UnpublishHolonAsync<T>(OAPPSystemHolonId, avatarId, "OAPPSystemManagerBase.UnpublishOAPPSystemHolonAsync", providerType);
            result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(saveResult, result);
            result.Result = saveResult.Result;
            return result;
        }

        public OASISResult<IOAPPSystemHolon> UnpublishOAPPSystemHolon(Guid OAPPSystemHolonId, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IOAPPSystemHolon> result = new OASISResult<IOAPPSystemHolon>();
            OASISResult<T> saveResult = UnpublishHolon<T>(OAPPSystemHolonId, avatarId, "OAPPSystemManagerBase.UnpublishOAPPSystemHolon", providerType);
            result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(saveResult, result);
            result.Result = saveResult.Result;
            return result;
        }

        public async Task<OASISResult<IOAPPSystemHolon>> UnpublishOAPPSystemHolonAsync(IOAPPSystemHolon T, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IOAPPSystemHolon> result = new OASISResult<IOAPPSystemHolon>();
            OASISResult<T> saveResult = await UnpublishHolonAsync<T>(T, avatarId, "OAPPSystemManagerBase.UnpublishOAPPSystemHolonAsync", providerType);
            result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(saveResult, result);
            result.Result = saveResult.Result;
            return result;
        }

        public OASISResult<IOAPPSystemHolon> UnpublishOAPPSystemHolon(IOAPPSystemHolon T, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IOAPPSystemHolon> result = new OASISResult<IOAPPSystemHolon>();
            OASISResult<T> saveResult = UnpublishHolon<T>(T, avatarId, "OAPPSystemManagerBase.UnpublishOAPPSystemHolon", providerType);
            result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(saveResult, result);
            result.Result = saveResult.Result;
            return result;
        }
        #endregion*/

        public async Task<OASISResult<IEnumerable<T>>> LoadVersionsAsync<T>(Guid id, HolonType holonType, ProviderType providerType = ProviderType.Default) where T : IOAPPSystemHolon, new()
        {
            OASISResult<IEnumerable<T>> result = new OASISResult<IEnumerable<T>>();

            //TODO: Currently we pass in 0 for version (which means the OASIS will return the latest version) but we need to be able to query for all versions (-1)
            //OASISResult<IEnumerable<T>> loadHolonsResult = await Data.LoadHolonsByMetaDataAsync<T>("OAPPSystemHolonId", OAPPSystemHolonId.ToString(), HolonType.T, true, true, 0, true, false, 0, HolonType.All, -1, providerType);
            OASISResult<IEnumerable<T>> loadHolonsResult = await Data.LoadHolonsByMetaDataAsync<T>(new Dictionary<string, string>() 
            { 
                { "OAPPSystemHolonId", id.ToString() },
                { "Active", "1" }
            }, MetaKeyValuePairMatchMode.All, holonType, true, true, 0, true, false, 0, HolonType.All, -1, providerType);

            result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(loadHolonsResult, result);
            result.Result = loadHolonsResult.Result;
            return result;
        }

        public OASISResult<IEnumerable<T>> LoadVersions<T>(Guid id, HolonType holonType, ProviderType providerType = ProviderType.Default) where T : IOAPPSystemHolon, new()
        {
            OASISResult<IEnumerable<T>> result = new OASISResult<IEnumerable<T>>();

            //TODO: Currently we pass in 0 for version (which means the OASIS will return the latest version) but we need to be able to query for all versions (-1)
            //OASISResult<IEnumerable<T>> loadHolonsResult = Data.LoadHolonsByMetaData<T>("OAPPSystemHolonId", OAPPSystemHolonId.ToString(), HolonType.T, true, true, 0, true, false, 0, HolonType.All, -1, providerType);
            OASISResult<IEnumerable<T>> loadHolonsResult = Data.LoadHolonsByMetaData<T>(new Dictionary<string, string>()
            {
                { "OAPPSystemHolonId", id.ToString() },
                { "Active", "1" }
            }, MetaKeyValuePairMatchMode.All, holonType, true, true, 0, true, false, 0, HolonType.All, -1, providerType);

            result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(loadHolonsResult, result);
            result.Result = loadHolonsResult.Result;
            return result;
        }

        public async Task<OASISResult<T>> LoadVersionAsync<T>(Guid id, HolonType holonType, string version, ProviderType providerType = ProviderType.Default) where T : IOAPPSystemHolon, new()
        {
            OASISResult<T> result = new OASISResult<T>();
            OASISResult<T> loadHolonResult = await Data.LoadHolonByMetaDataAsync<T>(new Dictionary<string, string>()
            {
                 { "OAPPSystemHolonId", id.ToString() },
                 { "Version", version },
                 { "Active", "1" }
            }, MetaKeyValuePairMatchMode.All, holonType, true, true, 0, true, 0, false, HolonType.All, providerType);

            OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(loadHolonResult, result); //Copy any possible warnings etc.
            if (loadHolonResult != null && !loadHolonResult.IsError && loadHolonResult.Result != null)
            {
                if (loadHolonResult.Result.OAPPSystemHolonDNA.Version == version)
                    result.Result = loadHolonResult.Result;
                else
                    OASISErrorHandling.HandleError(ref result, $"Error occured in OAPPSystemManagerBase.LoadVersionAsync. Reason: The version {version} does not exist for id {id}.");
            }
            else
                OASISErrorHandling.HandleError(ref result, $"Error occured in OAPPSystemManagerBase.LoadVersionAsync. Reason: {loadHolonResult.Message}");

            return result;
        }

        public OASISResult<T> LoadVersion<T>(Guid id, HolonType holonType, string version, ProviderType providerType = ProviderType.Default) where T : IOAPPSystemHolon, new()
        {
            OASISResult<T> result = new OASISResult<T>();
            OASISResult<T> loadHolonResult = Data.LoadHolonByMetaData<T>(new Dictionary<string, string>()
            {
                 { "OAPPSystemHolonId", id.ToString() },
                 { "Version", version },
                 { "Active", "1" }
            }, MetaKeyValuePairMatchMode.All, holonType, true, true, 0, true, false, HolonType.All, 0, providerType);

            OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(loadHolonResult, result); //Copy any possible warnings etc.
            if (loadHolonResult != null && !loadHolonResult.IsError && loadHolonResult.Result != null)
            {
                if (loadHolonResult.Result.OAPPSystemHolonDNA.Version == version)
                    result.Result = loadHolonResult.Result;
                else
                    OASISErrorHandling.HandleError(ref result, $"Error occured in OAPPSystemManagerBase.LoadVersion. Reason: The version {version} does not exist for id {id}.");
            }
            else
                OASISErrorHandling.HandleError(ref result, $"Error occured in OAPPSystemManagerBase.LoadVersion. Reason: {loadHolonResult.Message}");

            return result;
        }

        public async Task<OASISResult<T>> EditAsync<T>(Guid id, HolonType holonType, IOAPPSystemHolonDNA newOAPPSystemHolonDNA, Guid avatarId, ProviderType providerType = ProviderType.Default) where T : IOAPPSystemHolon, new()
        {
            OASISResult<T> result = new OASISResult<T>();
            OASISResult<T> loadResult = await LoadAsync<T>(id, avatarId, holonType, providerType: providerType);

            if (loadResult != null && loadResult.Result != null && !loadResult.IsError)
                await EditAsync<T>(loadResult.Result, newOAPPSystemHolonDNA, avatarId, providerType);
            else
                OASISErrorHandling.HandleError(ref result, $"Error occured in OAPPSystemManagerBase.EditAsync. Reason: {loadResult.Message}");

            return result;
        }

        public async Task<OASISResult<T>> EditAsync<T>(T holon, IOAPPSystemHolonDNA newOAPPSystemHolonDNA, Guid avatarId, ProviderType providerType = ProviderType.Default) where T : IOAPPSystemHolon, new()
        {
            OASISResult<T> result = new OASISResult<T>();
            string errorMessage = "Error occured in OAPPSystemManagerBase.EditAsync. Reason: ";
            string oldPath = "";
            string newPath = "";
            string oldPublishedPath = "";
            string oldDownloadedPath = "";
            string oldInstalledPath = "";
            string oldName = "";
            string launchTarget = "";

            if (holon.Name != newOAPPSystemHolonDNA.Name)
            {
                oldName = holon.Name;
                oldPath = holon.OAPPSystemHolonDNA.SourcePath;
                newPath = Path.Combine(new DirectoryInfo(holon.OAPPSystemHolonDNA.SourcePath).Parent.FullName, newOAPPSystemHolonDNA.Name);
                newOAPPSystemHolonDNA.SourcePath = newPath;
                newOAPPSystemHolonDNA.LaunchTarget = newOAPPSystemHolonDNA.LaunchTarget.Replace(holon.Name, newOAPPSystemHolonDNA.Name);
                launchTarget = newOAPPSystemHolonDNA.LaunchTarget;

                holon.MetaData[OAPPSystemHolonNameName] = newOAPPSystemHolonDNA.Name;

                if (!string.IsNullOrEmpty(holon.OAPPSystemHolonDNA.PublishedPath))
                {
                    oldPublishedPath = holon.OAPPSystemHolonDNA.PublishedPath;
                    newOAPPSystemHolonDNA.PublishedPath = oldPublishedPath.Replace(oldName, newOAPPSystemHolonDNA.Name);
                }
            }

            holon.OAPPSystemHolonDNA = newOAPPSystemHolonDNA;
            holon.Name = newOAPPSystemHolonDNA.Name;
            holon.Description = newOAPPSystemHolonDNA.Description;

            if (!string.IsNullOrEmpty(newPath) && !string.IsNullOrEmpty(oldPath))
            {
                try
                {
                    if (Directory.Exists(oldPath))
                        Directory.Move(oldPath, newPath);
                }
                catch (Exception e)
                {
                    OASISErrorHandling.HandleWarning(ref result, $"An error occured attempting to rename the {OAPPSystemHolonUIName} folder from {oldPath} to {newPath}. Reason: {e}.");
                    CLIEngine.ShowErrorMessage("PLEASE RENAME THIS FOLDER MANUALLY, THANK YOU!");
                }

                if (!string.IsNullOrEmpty(newOAPPSystemHolonDNA.PublishedPath))
                {
                    try
                    {
                        if (!string.IsNullOrEmpty(oldPublishedPath) && File.Exists(oldPublishedPath))
                            File.Move(oldPublishedPath, newOAPPSystemHolonDNA.PublishedPath);
                    }
                    catch (Exception e)
                    {
                        OASISErrorHandling.HandleWarning(ref result, $"An error occured attempting to rename the {OAPPSystemHolonUIName} published file from {oldPublishedPath} to {newOAPPSystemHolonDNA.PublishedPath}. Reason: {e}.");
                        CLIEngine.ShowErrorMessage("PLEASE RENAME THIS FOLDER MANUALLY, THANK YOU!");
                    }
                }
            }

            OASISResult<T> saveResult = await SaveAsync<T>(holon, avatarId, providerType: providerType);

            if (saveResult != null && !saveResult.IsError && saveResult.Result != null)
            {
                OASISResult<IEnumerable<T>> holonsResult = await LoadVersionsAsync<T>(newOAPPSystemHolonDNA.Id, holon.HolonType, providerType);

                if (holonsResult != null && holonsResult.Result != null && !holonsResult.IsError)
                {
                    foreach (T holonVersion in holonsResult.Result)
                    {
                        //No need to update the version we already updated above.
                        if (holonVersion.OAPPSystemHolonDNA.Version == holon.OAPPSystemHolonDNA.Version)
                            continue;

                        holonVersion.OAPPSystemHolonDNA = newOAPPSystemHolonDNA;
                        holonVersion.Name = newOAPPSystemHolonDNA.Name;
                        holonVersion.Description = newOAPPSystemHolonDNA.Description;
                        holonVersion.MetaData["OAPPSystemHolonName"] = newOAPPSystemHolonDNA.Name;

                        oldPath = holonVersion.OAPPSystemHolonDNA.SourcePath;
                        newPath = Path.Combine(new DirectoryInfo(oldPath).Parent.FullName, newOAPPSystemHolonDNA.Name);
                        holonVersion.OAPPSystemHolonDNA.SourcePath = newPath;
                        holonVersion.OAPPSystemHolonDNA.LaunchTarget = launchTarget;

                        if (!string.IsNullOrEmpty(holonVersion.OAPPSystemHolonDNA.PublishedPath))
                        {
                            oldPublishedPath = holonVersion.OAPPSystemHolonDNA.PublishedPath;
                            //holonVersion.OAPPSystemHolonDNA.PublishedPath = Path.Combine(new DirectoryInfo(oldPublishedPath).FullName, newOAPPSystemHolonDNA.Name);
                            newOAPPSystemHolonDNA.PublishedPath = oldPublishedPath.Replace(oldName, newOAPPSystemHolonDNA.Name);
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
                                OASISErrorHandling.HandleWarning(ref result, $"An error occured attempting to rename the {OAPPSystemHolonUIName} folder from {oldPath} to {newPath}. Reason: {e}.");
                                CLIEngine.ShowErrorMessage("PLEASE RENAME THIS FOLDER MANUALLY, THANK YOU!");
                            }
                        }

                        if (!string.IsNullOrEmpty(oldPublishedPath))
                        {
                            try
                            {
                                if (File.Exists(oldPublishedPath))
                                    File.Move(oldPublishedPath, holonVersion.OAPPSystemHolonDNA.PublishedPath);
                            }
                            catch (Exception e)
                            {
                                OASISErrorHandling.HandleWarning(ref result, $"An error occured attempting to rename the {OAPPSystemHolonUIName} published file from {oldPublishedPath} to {newOAPPSystemHolonDNA.PublishedPath}. Reason: {e}.");
                                CLIEngine.ShowErrorMessage("PLEASE RENAME THIS FOLDER MANUALLY, THANK YOU!");
                            }
                        }

                        OASISResult<T> templateSaveResult = await SaveAsync<T>(holonVersion, avatarId, providerType);

                        if (templateSaveResult != null && templateSaveResult.Result != null && !templateSaveResult.IsError)
                        {
                           
                        }
                        else
                            OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} Error occured calling SaveAsync updating the OAPPSystemHolonDNA for {OAPPSystemHolonUIName} with Id {holonVersion.Id} for provider {Enum.GetName(typeof(ProviderType), providerType)}. Reason: {templateSaveResult.Message}");
                    }
                }
                else
                    OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} Error occured updating the OAPPSystemHolonDNA for all {OAPPSystemHolonUIName} versions caused by an error in LoadVersionsAsync. Reason: {holonsResult.Message}");


                OASISResult<IEnumerable<IInstalledOAPPSystemHolon>> installedTemplatesResult = await ListInstalledAsync(avatarId, providerType);

                if (installedTemplatesResult != null && installedTemplatesResult.Result != null && !installedTemplatesResult.IsError)
                {
                    foreach (IInstalledOAPPSystemHolon installedHolon in installedTemplatesResult.Result)
                    {
                        installedHolon.OAPPSystemHolonDNA = newOAPPSystemHolonDNA;
                        installedHolon.Name = installedHolon.Name.Replace(oldName, newOAPPSystemHolonDNA.Name);
                        installedHolon.Description = installedHolon.Description.Replace(oldName, newOAPPSystemHolonDNA.Name);
                        installedHolon.MetaData[OAPPSystemHolonNameName] = newOAPPSystemHolonDNA.Name;

                        oldPath = installedHolon.OAPPSystemHolonDNA.SourcePath;
                        newPath = Path.Combine(new DirectoryInfo(oldPath).Parent.FullName, newOAPPSystemHolonDNA.Name);
                        installedHolon.OAPPSystemHolonDNA.SourcePath = newPath;
                        installedHolon.OAPPSystemHolonDNA.LaunchTarget = launchTarget;

                        if (!string.IsNullOrEmpty(installedHolon.OAPPSystemHolonDNA.PublishedPath))
                        {
                            oldPublishedPath = installedHolon.OAPPSystemHolonDNA.PublishedPath;
                            installedHolon.OAPPSystemHolonDNA.PublishedPath = Path.Combine(new DirectoryInfo(oldPublishedPath).Parent.FullName, string.Concat(newOAPPSystemHolonDNA.Name, "_v", installedHolon.OAPPSystemHolonDNA.Version, ".", OAPPSystemHolonFileExtention));
                            //holonVersion.OAPPSystemHolonDNA.PublishedPath = oldPublishedPath.Replace(oldName, newOAPPSystemHolonDNA.Name);
                        }

                        if (!string.IsNullOrEmpty(installedHolon.DownloadedPath))
                        {
                            oldDownloadedPath = installedHolon.DownloadedPath;
                            //holonVersion.DownloadedPath = Path.Combine(new DirectoryInfo(oldDownloadedPath).FullName, newOAPPSystemHolonDNA.Name);
                            installedHolon.DownloadedPath = oldDownloadedPath.Replace(oldName, newOAPPSystemHolonDNA.Name);
                        }

                        if (!string.IsNullOrEmpty(installedHolon.InstalledPath))
                        {
                            oldInstalledPath = installedHolon.InstalledPath;
                            installedHolon.InstalledPath = Path.Combine(new DirectoryInfo(oldInstalledPath).Parent.FullName, newOAPPSystemHolonDNA.Name);
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
                                OASISErrorHandling.HandleWarning(ref result, $"An error occured attempting to rename the {OAPPSystemHolonUIName} folder from {oldPath} to {newPath}. Reason: {e}.");
                                CLIEngine.ShowErrorMessage("PLEASE RENAME THIS FOLDER MANUALLY, THANK YOU!");
                            }
                        }

                        if (!string.IsNullOrEmpty(oldPublishedPath))
                        {
                            try
                            {
                                if (File.Exists(oldPublishedPath) && oldPublishedPath != installedHolon.OAPPSystemHolonDNA.PublishedPath)
                                    File.Move(oldPublishedPath, installedHolon.OAPPSystemHolonDNA.PublishedPath);
                            }
                            catch (Exception e)
                            {
                                OASISErrorHandling.HandleWarning(ref result, $"An error occured attempting to rename the {OAPPSystemHolonUIName} published file from {oldPublishedPath} to {newOAPPSystemHolonDNA.PublishedPath}. Reason: {e}.");
                                CLIEngine.ShowErrorMessage("PLEASE RENAME THIS FOLDER MANUALLY, THANK YOU!");
                            }
                        }

                        OASISResult<InstalledOAPPSystemHolon> installedOPPSystemHolonSaveResult = await SaveAsync((InstalledOAPPSystemHolon)installedHolon, avatarId, providerType);

                        if (installedOPPSystemHolonSaveResult != null && installedOPPSystemHolonSaveResult.Result != null && !installedOPPSystemHolonSaveResult.IsError)
                        {
                            if (!string.IsNullOrEmpty(oldDownloadedPath))
                            {
                                try
                                {
                                    if (File.Exists(oldDownloadedPath))
                                        File.Move(oldDownloadedPath, installedHolon.DownloadedPath);
                                }
                                catch (Exception e)
                                {
                                    OASISErrorHandling.HandleWarning(ref result, $"An error occured attempting to rename the {OAPPSystemHolonUIName} downloaded file from {oldDownloadedPath} to {holonVersion.DownloadedPath}. Reason: {e}.");
                                    CLIEngine.ShowErrorMessage("PLEASE RENAME THIS FOLDER MANUALLY, THANK YOU!");
                                }
                            }

                            if (!string.IsNullOrEmpty(oldInstalledPath))
                            {
                                try
                                {
                                    if (Directory.Exists(oldInstalledPath))
                                        Directory.Move(oldInstalledPath, installedHolon.InstalledPath);
                                }
                                catch (Exception e)
                                {
                                    OASISErrorHandling.HandleWarning(ref result, $"An error occured attempting to rename the {OAPPSystemHolonUIName} installed folder from {oldInstalledPath} to {holonVersion.InstalledPath}. Reason: {e}.");
                                    CLIEngine.ShowErrorMessage("PLEASE RENAME THIS FOLDER MANUALLY, THANK YOU!");
                                }
                            }
                        }
                        else
                            OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} Error occured updating the OAPPSystemHolonDNA for Installed {OAPPSystemHolonUIName} with Id {holonVersion.Id} for provider {Enum.GetName(typeof(ProviderType), providerType)}. Reason: {templateSaveResult.Message}");
                    }
                }
                else
                    OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} Error occured updating the OAPPSystemHolonDNA for all Installed {OAPPSystemHolonUIName} versions caused by an error in ListInstalledOAPPSystemHolonsAsync. Reason: {holonsResult.Message}");


                result.Result = saveResult.Result;
                result.IsSaved = true;
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured saving the {OAPPSystemHolonUIName} with Id {newOAPPSystemHolonDNA.Id} from the {Enum.GetName(typeof(ProviderType), providerType)} provider. Reason: {saveResult.Message}");

            return result;
        }

        //private async Task UpdateOAPPSystemHolonAsync(IOAPPSystemHolon template, IOAPPSystemHolonDNA newOAPPSystemHolonDNA, Guid avatarId, OASISResult<IOAPPSystemHolon> result, string errorMessage, ProviderType providerType)
        //{
        //    string oldPath = "";
        //    string newPath = "";
        //    string oldPublishedPath = "";

        //    template.OAPPSystemHolonDNA = newOAPPSystemHolonDNA;
        //    template.Name = newOAPPSystemHolonDNA.Name;
        //    template.Description = newOAPPSystemHolonDNA.Description;

        //    oldPath = template.OAPPSystemHolonDNA.SourcePath;
        //    newPath = Path.Combine(new DirectoryInfo(oldPath).Parent.FullName, newOAPPSystemHolonDNA.Name);
        //    template.OAPPSystemHolonDNA.SourcePath = newPath;

        //    if (!string.IsNullOrEmpty(template.OAPPSystemHolonDNA.PublishedPath))
        //    {
        //        oldPublishedPath = template.OAPPSystemHolonDNA.PublishedPath;
        //        template.OAPPSystemHolonDNA.PublishedPath = Path.Combine(new DirectoryInfo(oldPublishedPath).FullName, newOAPPSystemHolonDNA.Name);
        //    }

        //    OASISResult<IOAPPSystemHolon> templateSaveResult = await SaveOAPPSystemHolonAsync(template, avatarId, providerType);

        //    if (templateSaveResult != null && templateSaveResult.Result != null && !templateSaveResult.IsError)
        //    {
        //        if (!string.IsNullOrEmpty(newPath))
        //        {
        //            try
        //            {
        //                if (Directory.Exists(oldPath))
        //                    Directory.Move(oldPath, newPath);
        //            }
        //            catch (Exception e)
        //            {
        //                OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} An error occured attempting to rename the {OAPPSystemHolonUIName} folder from {oldPath} to {newPath}. Reason: {e}.");
        //                CLIEngine.ShowErrorMessage("PLEASE RENAME THIS FOLDER MANUALLY, THANK YOU!");
        //            }
        //        }

        //        if (!string.IsNullOrEmpty(oldPublishedPath))
        //        {
        //            try
        //            {
        //                if (File.Exists(oldPublishedPath))
        //                    File.Move(oldPublishedPath, template.OAPPSystemHolonDNA.PublishedPath);
        //            }
        //            catch (Exception e)
        //            {
        //                OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} An error occured attempting to rename the {OAPPSystemHolonUIName} published file from {oldPublishedPath} to {newOAPPSystemHolonDNA.PublishedPath}. Reason: {e}.");
        //                CLIEngine.ShowErrorMessage("PLEASE RENAME THIS FOLDER MANUALLY, THANK YOU!");
        //            }
        //        }
        //    }
        //    else
        //        OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} Error occured calling SaveOAPPSystemHolonAsync updating the OAPPSystemHolonDNA for {OAPPSystemHolonUIName} with Id {template.Id} for provider {Enum.GetName(typeof(ProviderType), providerType)}. Reason: {templateSaveResult.Message}");
        //}

        //public async Task<OASISResult<IOAPPSystemHolonDNA>> PublishOAPPSystemHolonAsync(string fullPathToOAPPSystemHolon, string launchTarget, Guid avatarId, string fullPathToPublishTo = "", bool registerOnSTARNET = true, bool generateOAPPSystemHolonBinary = true, bool uploadOAPPSystemHolonToCloud = false, ProviderType providerType = ProviderType.Default, ProviderType oappBinaryProviderType = ProviderType.IPFSOASIS)
        public async Task<OASISResult<T>> PublishAsync<T>(Guid avatarId, HolonType holonType, string fullPathToOAPPSystemHolon, string launchTarget, string fullPathToPublishTo = "", bool registerOnSTARNET = true, bool generateOAPPSystemHolonBinary = true, bool uploadOAPPSystemHolonToCloud = false, bool edit = false, ProviderType providerType = ProviderType.Default, ProviderType oappBinaryProviderType = ProviderType.IPFSOASIS) where T : IOAPPSystemHolon, new()
        {
            OASISResult<T> result = new OASISResult<T>();
            string errorMessage = "Error occured in OAPPSystemManagerBase.PublishAsync. Reason: ";
            IOAPPSystemHolonDNA OAPPSystemHolonDNA = null;
            string tempPath = "";

            try
            {
                OASISResult<IOAPPSystemHolonDNA> readOAPPSystemHolonDNAResult = await ReadOAPPSystemHolonDNAAsync(fullPathToOAPPSystemHolon);

                if (readOAPPSystemHolonDNAResult != null && !readOAPPSystemHolonDNAResult.IsError && readOAPPSystemHolonDNAResult.Result != null)
                {
                    OAPPSystemHolonDNA = readOAPPSystemHolonDNAResult.Result;
                    OnOAPPSystemHolonPublishStatusChanged?.Invoke(this, new OAPPSystemHolonPublishStatusEventArgs() { OAPPSystemHolonDNA = OAPPSystemHolonDNA, Status = Enums.OAPPSystemHolonPublishStatus.Packaging });
                    OASISResult<IAvatar> loadAvatarResult = await AvatarManager.Instance.LoadAvatarAsync(avatarId, false, true, providerType);

                    if (loadAvatarResult != null && loadAvatarResult.Result != null && !loadAvatarResult.IsError)
                    {
                        //Load latest version.
                        OASISResult<T> loadOAPPSystemHolonResult = await LoadAsync<T>(OAPPSystemHolonDNA.Id, avatarId, holonType);

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
                                        fullPathToPublishTo = Path.Combine(fullPathToOAPPSystemHolon, "Published");

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

                                    OAPPSystemHolonDNA.PublishedOnSTARNET = registerOnSTARNET && (oappBinaryProviderType != ProviderType.None || uploadOAPPSystemHolonToCloud);

                                    if (generateOAPPSystemHolonBinary)
                                    {
                                        OAPPSystemHolonDNA.PublishedPath = Path.Combine(fullPathToPublishTo, publishedOAPPSystemHolonFileName);
                                        OAPPSystemHolonDNA.PublishedToCloud = registerOnSTARNET && uploadOAPPSystemHolonToCloud;
                                        OAPPSystemHolonDNA.PublishedProviderType = oappBinaryProviderType;
                                    }

                                    WriteOAPPSystemHolonDNA(OAPPSystemHolonDNA, fullPathToOAPPSystemHolon);
                                    OnOAPPSystemHolonPublishStatusChanged?.Invoke(this, new OAPPSystemHolonPublishStatusEventArgs() { OAPPSystemHolonDNA = OAPPSystemHolonDNA, Status = Enums.OAPPSystemHolonPublishStatus.Compressing });

                                    if (generateOAPPSystemHolonBinary)
                                    {
                                        if (File.Exists(OAPPSystemHolonDNA.PublishedPath))
                                            File.Delete(OAPPSystemHolonDNA.PublishedPath);

                                        ZipFile.CreateFromDirectory(fullPathToOAPPSystemHolon, OAPPSystemHolonDNA.PublishedPath);
                                    }

                                    //TODO: Currently the filesize will NOT be in the compressed .OAPPSystemHolon file because we dont know the size before we create it! ;-) We would need to compress it twice or edit the compressed file after to update the OAPPSystemHolonDNA inside it...
                                    if (!string.IsNullOrEmpty(OAPPSystemHolonDNA.PublishedPath) && File.Exists(OAPPSystemHolonDNA.PublishedPath))
                                        OAPPSystemHolonDNA.FileSize = new FileInfo(OAPPSystemHolonDNA.PublishedPath).Length;

                                    WriteOAPPSystemHolonDNA(OAPPSystemHolonDNA, fullPathToOAPPSystemHolon);
                                    loadOAPPSystemHolonResult.Result.OAPPSystemHolonDNA = OAPPSystemHolonDNA;

                                    if (registerOnSTARNET)
                                    {
                                        if (uploadOAPPSystemHolonToCloud)
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
                                                OAPPSystemHolonDNA.PublishedOnSTARNET = registerOnSTARNET && oappBinaryProviderType != ProviderType.None;
                                                OAPPSystemHolonDNA.PublishedToCloud = false;
                                            }
                                        }

                                        if (oappBinaryProviderType != ProviderType.None)
                                        {
                                            loadOAPPSystemHolonResult.Result.PublishedOAPPSystemHolon = File.ReadAllBytes(OAPPSystemHolonDNA.PublishedPath);

                                            //TODO: We could use HoloOASIS and other large file storage providers in future...
                                            OASISResult<T> saveLargeOAPPSystemHolonResult = await SaveAsync<T>(loadOAPPSystemHolonResult.Result, avatarId, oappBinaryProviderType);

                                            if (saveLargeOAPPSystemHolonResult != null && !saveLargeOAPPSystemHolonResult.IsError && saveLargeOAPPSystemHolonResult.Result != null)
                                            {
                                                result.Result = saveLargeOAPPSystemHolonResult.Result;
                                                result.IsSaved = true;
                                            }
                                            else
                                            {
                                                OASISErrorHandling.HandleWarning(ref result, $" Error occured saving the published {OAPPSystemHolonUIName} binary to STARNET using the {oappBinaryProviderType} provider. Reason: {saveLargeOAPPSystemHolonResult.Message}");
                                                OAPPSystemHolonDNA.PublishedOnSTARNET = registerOnSTARNET && uploadOAPPSystemHolonToCloud;
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

                                    OASISResult<T> saveOAPPSystemHolonResult = await SaveAsync<T>(loadOAPPSystemHolonResult.Result, avatarId, providerType);

                                    if (saveOAPPSystemHolonResult != null && !saveOAPPSystemHolonResult.IsError && saveOAPPSystemHolonResult.Result != null)
                                    {
                                        result = await UpdateNumberOfVersionCountsAsync(saveOAPPSystemHolonResult, avatarId, holonType, errorMessage, providerType);
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

        //TODO: Come back to this, was going to call this for publishing and installing to make sure the DNA hadn't been changed on the disk, but maybe we want to allow this? Not sure, needs more thought...
        //private async OASISResult<bool> IsOAPPSystemHolonDNAValidAsync(IOAPPSystemHolonDNA OAPPSystemHolonDNA)
        //{
        //    OASISResult<IOAPPSystemHolon> OAPPSystemHolonResult = await LoadOAPPSystemHolonAsync(OAPPSystemHolonDNA.OAPPSystemHolonId);

        //    if (OAPPSystemHolonResult != null && OAPPSystemHolonResult.Result != null && !OAPPSystemHolonResult.IsError)
        //    {
        //        IOAPPSystemHolonDNA originalDNA =  JsonSerializer.Deserialize<IOAPPSystemHolonDNA>(OAPPSystemHolonResult.Result.MetaData["OAPPSystemHolonDNA"].ToString());

        //        if (originalDNA != null)
        //        {
        //            if (originalDNA.GenesisType != OAPPSystemHolonDNA.GenesisType ||
        //                originalDNA.OAPPSystemHolonType != OAPPSystemHolonDNA.OAPPSystemHolonType ||
        //                originalDNA.CelestialBodyType != OAPPSystemHolonDNA.CelestialBodyType ||
        //                originalDNA.CelestialBodyId != OAPPSystemHolonDNA.CelestialBodyId ||
        //                originalDNA.CelestialBodyName != OAPPSystemHolonDNA.CelestialBodyName ||
        //                originalDNA.CreatedByAvatarId != OAPPSystemHolonDNA.CreatedByAvatarId ||
        //                originalDNA.CreatedByAvatarUsername != OAPPSystemHolonDNA.CreatedByAvatarUsername ||
        //                originalDNA.CreatedOn != OAPPSystemHolonDNA.CreatedOn ||
        //                originalDNA.Description != OAPPSystemHolonDNA.Description ||
        //                originalDNA.IsActive != OAPPSystemHolonDNA.IsActive ||
        //                originalDNA.LaunchTarget != OAPPSystemHolonDNA.LaunchTarget ||
        //                originalDNA. != OAPPSystemHolonDNA.LaunchTarget ||

        //        }
        //    }
        //}

        public OASISResult<T> PublishOAPPSystemHolon<T>(Guid avatarId, HolonType holonType, string fullPathToOAPPSystemHolon, string launchTarget, string fullPathToPublishTo = "", bool registerOnSTARNET = true, bool generateOAPPSystemHolonBinary = true, bool uploadOAPPSystemHolonToCloud = false, bool edit = false, ProviderType providerType = ProviderType.Default, ProviderType oappBinaryProviderType = ProviderType.IPFSOASIS) where T : IOAPPSystemHolon, new()
        {
            OASISResult<T> result = new OASISResult<T>();
            string errorMessage = "Error occured in OAPPSystemManagerBase.Publish. Reason: ";
            IOAPPSystemHolonDNA OAPPSystemHolonDNA = null;
            string tempPath = "";

            try
            {
                OASISResult<IOAPPSystemHolonDNA> readOAPPSystemHolonDNAResult = ReadOAPPSystemHolonDNA(fullPathToOAPPSystemHolon);

                if (readOAPPSystemHolonDNAResult != null && !readOAPPSystemHolonDNAResult.IsError && readOAPPSystemHolonDNAResult.Result != null)
                {
                    OAPPSystemHolonDNA = readOAPPSystemHolonDNAResult.Result;
                    OnOAPPSystemHolonPublishStatusChanged?.Invoke(this, new OAPPSystemHolonPublishStatusEventArgs() { OAPPSystemHolonDNA = OAPPSystemHolonDNA, Status = Enums.OAPPSystemHolonPublishStatus.Packaging });
                    OASISResult<IAvatar> loadAvatarResult = AvatarManager.Instance.LoadAvatar(avatarId, false, true, providerType);

                    if (loadAvatarResult != null && loadAvatarResult.Result != null && !loadAvatarResult.IsError)
                    {
                        //Load latest version.
                        OASISResult<T> loadOAPPSystemHolonResult = Load<T>(OAPPSystemHolonDNA.Id, avatarId, holonType);

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
                                        fullPathToPublishTo = Path.Combine(fullPathToOAPPSystemHolon, "Published");

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

                                    OAPPSystemHolonDNA.PublishedOnSTARNET = registerOnSTARNET && (oappBinaryProviderType != ProviderType.None || uploadOAPPSystemHolonToCloud);

                                    if (generateOAPPSystemHolonBinary)
                                    {
                                        OAPPSystemHolonDNA.PublishedPath = Path.Combine(fullPathToPublishTo, publishedOAPPSystemHolonFileName);
                                        OAPPSystemHolonDNA.PublishedToCloud = registerOnSTARNET && uploadOAPPSystemHolonToCloud;
                                        OAPPSystemHolonDNA.PublishedProviderType = oappBinaryProviderType;
                                    }

                                    WriteOAPPSystemHolonDNA(OAPPSystemHolonDNA, fullPathToOAPPSystemHolon);
                                    OnOAPPSystemHolonPublishStatusChanged?.Invoke(this, new OAPPSystemHolonPublishStatusEventArgs() { OAPPSystemHolonDNA = OAPPSystemHolonDNA, Status = Enums.OAPPSystemHolonPublishStatus.Compressing });

                                    if (generateOAPPSystemHolonBinary)
                                    {
                                        if (File.Exists(OAPPSystemHolonDNA.PublishedPath))
                                            File.Delete(OAPPSystemHolonDNA.PublishedPath);

                                        ZipFile.CreateFromDirectory(fullPathToOAPPSystemHolon, OAPPSystemHolonDNA.PublishedPath);
                                    }

                                    //TODO: Currently the filesize will NOT be in the compressed .OAPPSystemHolon file because we dont know the size before we create it! ;-) We would need to compress it twice or edit the compressed file after to update the OAPPSystemHolonDNA inside it...
                                    if (!string.IsNullOrEmpty(OAPPSystemHolonDNA.PublishedPath) && File.Exists(OAPPSystemHolonDNA.PublishedPath))
                                        OAPPSystemHolonDNA.FileSize = new FileInfo(OAPPSystemHolonDNA.PublishedPath).Length;

                                    WriteOAPPSystemHolonDNA(OAPPSystemHolonDNA, fullPathToOAPPSystemHolon);
                                    loadOAPPSystemHolonResult.Result.OAPPSystemHolonDNA = OAPPSystemHolonDNA;

                                    if (registerOnSTARNET)
                                    {
                                        if (uploadOAPPSystemHolonToCloud)
                                        {
                                            try
                                            {
                                                OnOAPPSystemHolonPublishStatusChanged?.Invoke(this, new OAPPSystemHolonPublishStatusEventArgs() { OAPPSystemHolonDNA = readOAPPSystemHolonDNAResult.Result, Status = Enums.OAPPSystemHolonPublishStatus.Uploading });
                                                StorageClient storage = StorageClient.Create();
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

                                                    storage.UploadObject(OAPPSystemHolonGoogleBucket, publishedOAPPSystemHolonFileName, "", fileStream, uploadObjectOptions, progress: progressReporter);
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
                                                OAPPSystemHolonDNA.PublishedOnSTARNET = registerOnSTARNET && oappBinaryProviderType != ProviderType.None;
                                                OAPPSystemHolonDNA.PublishedToCloud = false;
                                            }
                                        }

                                        if (oappBinaryProviderType != ProviderType.None)
                                        {
                                            loadOAPPSystemHolonResult.Result.PublishedOAPPSystemHolon = File.ReadAllBytes(OAPPSystemHolonDNA.PublishedPath);

                                            //TODO: We could use HoloOASIS and other large file storage providers in future...
                                            OASISResult<T> saveLargeOAPPSystemHolonResult = Save(loadOAPPSystemHolonResult.Result, avatarId, oappBinaryProviderType);

                                            if (saveLargeOAPPSystemHolonResult != null && !saveLargeOAPPSystemHolonResult.IsError && saveLargeOAPPSystemHolonResult.Result != null)
                                            {
                                                result.Result = saveLargeOAPPSystemHolonResult.Result;
                                                result.IsSaved = true;
                                            }
                                            else
                                            {
                                                OASISErrorHandling.HandleWarning(ref result, $" Error occured saving the published {OAPPSystemHolonUIName} binary to STARNET using the {oappBinaryProviderType} provider. Reason: {saveLargeOAPPSystemHolonResult.Message}");
                                                OAPPSystemHolonDNA.PublishedOnSTARNET = registerOnSTARNET && uploadOAPPSystemHolonToCloud;
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

                                    OASISResult<T> saveOAPPSystemHolonResult = Save(loadOAPPSystemHolonResult.Result, avatarId, providerType);

                                    if (saveOAPPSystemHolonResult != null && !saveOAPPSystemHolonResult.IsError && saveOAPPSystemHolonResult.Result != null)
                                    {
                                        result = UpdateNumberOfVersionCountsAsync(saveOAPPSystemHolonResult, avatarId, errorMessage, providerType);
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

        public async Task<OASISResult<IOAPPSystemHolon>> UnpublishOAPPSystemHolonAsync(IOAPPSystemHolon T, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IOAPPSystemHolon> result = new OASISResult<IOAPPSystemHolon>();
            string errorMessage = "Error occured in UnpublishOAPPSystemHolonAsync. Reason: ";

            T.OAPPSystemHolonDNA.PublishedOn = DateTime.MinValue;
            T.OAPPSystemHolonDNA.PublishedByAvatarId = Guid.Empty;
            T.OAPPSystemHolonDNA.PublishedByAvatarUsername = "";
            //T.OAPPSystemHolonDNA.IsActive = false;
            T.MetaData["Active"] = "0";

            OASISResult<IOAPPSystemHolon> oappResult = await SaveOAPPSystemHolonAsync(T, avatarId, providerType);

            if (oappResult != null && oappResult.Result != null && !oappResult.IsError)
            { 
                result.Result = oappResult.Result; //ConvertOAPPSystemHolonToOAPPSystemHolonDNA(T);
                result.Message = "T Unpublished";
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured saving the {OAPPSystemHolonUIName} with the SaveOAPPSystemHolonAsync method, reason: {oappResult.Message}");

            return result;
        }

        public OASISResult<IOAPPSystemHolon> UnpublishOAPPSystemHolon(IOAPPSystemHolon T, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IOAPPSystemHolon> result = new OASISResult<IOAPPSystemHolon>();
            string errorMessage = "Error occured in UnpublishOAPPSystemHolon. Reason: ";

            T.OAPPSystemHolonDNA.PublishedOn = DateTime.MinValue;
            T.OAPPSystemHolonDNA.PublishedByAvatarId = Guid.Empty;
            T.OAPPSystemHolonDNA.PublishedByAvatarUsername = "";
            //T.OAPPSystemHolonDNA.IsActive = false;
            T.MetaData["Active"] = "0";

            OASISResult<IOAPPSystemHolon> oappResult = SaveOAPPSystemHolon(T, avatarId, providerType);

            if (oappResult != null && oappResult.Result != null && !oappResult.IsError)
            {
                result.Result = oappResult.Result; //ConvertOAPPSystemHolonToOAPPSystemHolonDNA(T);
                result.Message = "{OAPPSystemHolonUIName} Unpublished";
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured saving the {OAPPSystemHolonUIName} with the SaveOAPPSystemHolon method, reason: {oappResult.Message}");

            return result;
        }

        public async Task<OASISResult<IOAPPSystemHolon>> UnpublishOAPPSystemHolonAsync(Guid OAPPSystemHolonId, int version, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IOAPPSystemHolon> result = new OASISResult<IOAPPSystemHolon>();
            OASISResult<IOAPPSystemHolon> loadResult = await LoadOAPPSystemHolonAsync(OAPPSystemHolonId, avatarId, version, providerType);

            if (loadResult != null && loadResult.Result != null && !loadResult.IsError)
                result = await UnpublishOAPPSystemHolonAsync(loadResult.Result, avatarId, providerType);
            else
                OASISErrorHandling.HandleError(ref result, $"Error occured in UnpublishOAPPSystemHolonAsync loading the T with the LoadOAPPSystemHolonAsync method, reason: {loadResult.Message}");

            return result;
        }

        public OASISResult<IOAPPSystemHolon> UnpublishOAPPSystemHolon(Guid OAPPSystemHolonId, int version, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IOAPPSystemHolon> result = new OASISResult<IOAPPSystemHolon>();
            OASISResult<IOAPPSystemHolon> loadResult = LoadOAPPSystemHolon(OAPPSystemHolonId, avatarId, version, providerType);

            if (loadResult != null && loadResult.Result != null && !loadResult.IsError)
                result = UnpublishOAPPSystemHolon(loadResult.Result, avatarId, providerType);
            else
                OASISErrorHandling.HandleError(ref result, $"Error occured in UnpublishOAPPSystemHolon loading the T with the LoadOAPPSystemHolon method, reason: {loadResult.Message}");

            return result;
        }

        public async Task<OASISResult<IOAPPSystemHolon>> UnpublishOAPPSystemHolonAsync(IOAPPSystemHolonDNA OAPPSystemHolonDNA, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IOAPPSystemHolon> result = new OASISResult<IOAPPSystemHolon>();
            OASISResult<IOAPPSystemHolon> oappResult = await LoadOAPPSystemHolonAsync(OAPPSystemHolonDNA.Id, avatarId, OAPPSystemHolonDNA.VersionSequence, providerType);
            string errorMessage = "Error occured in UnpublishOAPPSystemHolonAsync. Reason: ";

            if (oappResult != null && oappResult.Result != null && !oappResult.IsError)
                result = await UnpublishOAPPSystemHolonAsync(oappResult.Result, avatarId, providerType);
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured loading the {OAPPSystemHolonUIName} with the LoadOAPPSystemHolonAsync method, reason: {oappResult.Message}");

            return result;
        }

        public OASISResult<IOAPPSystemHolon> UnpublishOAPPSystemHolon(IOAPPSystemHolonDNA OAPPSystemHolonDNA, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IOAPPSystemHolon> result = new OASISResult<IOAPPSystemHolon>();
            OASISResult<IOAPPSystemHolon> oappResult = LoadOAPPSystemHolon(OAPPSystemHolonDNA.Id, avatarId, OAPPSystemHolonDNA.VersionSequence, providerType);
            string errorMessage = "Error occured in UnpublishOAPPSystemHolon. Reason: ";

            if (oappResult != null && oappResult.Result != null && !oappResult.IsError)
                result = UnpublishOAPPSystemHolon(oappResult.Result, avatarId, providerType);
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured loading the {OAPPSystemHolonUIName} with the LoadOAPPSystemHolon method, reason: {oappResult.Message}");

            return result;
        }

        public async Task<OASISResult<IOAPPSystemHolon>> RepublishOAPPSystemHolonAsync(IOAPPSystemHolon T, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IOAPPSystemHolon> result = new OASISResult<IOAPPSystemHolon>();
            string errorMessage = "Error occured in RepublishOAPPSystemHolonAsync. Reason: ";

            OASISResult<IAvatar> avatarResult = await AvatarManager.Instance.LoadAvatarAsync(avatarId, false, true, providerType);

            if (avatarResult != null && avatarResult.Result != null && !avatarResult.IsError)
            {
                T.OAPPSystemHolonDNA.PublishedOn = DateTime.Now;
                T.OAPPSystemHolonDNA.PublishedByAvatarId = avatarId;
                T.OAPPSystemHolonDNA.PublishedByAvatarUsername = avatarResult.Result.Username;
                //T.OAPPSystemHolonDNA.IsActive = true;
                T.MetaData["Active"] = "1";

                OASISResult<IOAPPSystemHolon> oappResult = await SaveOAPPSystemHolonAsync(T, avatarId, providerType);

                if (oappResult != null && oappResult.Result != null && !oappResult.IsError)
                {
                    result.Result = oappResult.Result; //ConvertOAPPSystemHolonToOAPPSystemHolonDNA(T);
                    result.Message = "T Republished";
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured saving the {OAPPSystemHolonUIName} with the SaveOAPPSystemHolonAsync method, reason: {oappResult.Message}");
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured loading the Avatar with the LoadAvatar method, reason: {avatarResult.Message}");

            return result;
        }

        public OASISResult<IOAPPSystemHolon> RepublishOAPPSystemHolon(IOAPPSystemHolon T, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IOAPPSystemHolon> result = new OASISResult<IOAPPSystemHolon>();
            string errorMessage = "Error occured in RepublishOAPPSystemHolon. Reason: ";

            OASISResult<IAvatar> avatarResult = AvatarManager.Instance.LoadAvatar(avatarId, false, true, providerType);

            if (avatarResult != null && avatarResult.Result != null && !avatarResult.IsError)
            {
                T.OAPPSystemHolonDNA.PublishedOn = DateTime.Now;
                T.OAPPSystemHolonDNA.PublishedByAvatarId = avatarId;
                T.OAPPSystemHolonDNA.PublishedByAvatarUsername = avatarResult.Result.Username;
                //T.OAPPSystemHolonDNA.IsActive = true;
                T.MetaData["Active"] = "1";

                OASISResult<IOAPPSystemHolon> oappResult = SaveOAPPSystemHolon(T, avatarId, providerType);

                if (oappResult != null && oappResult.Result != null && !oappResult.IsError)
                {
                    result.Result = oappResult.Result; //ConvertOAPPSystemHolonToOAPPSystemHolonDNA(T);
                    result.Message = "{OAPPSystemHolonUIName} Republished";
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured saving the {OAPPSystemHolonUIName} with the SaveOAPPSystemHolon method, reason: {oappResult.Message}");
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured loading the Avatar with the LoadAvatar method, reason: {avatarResult.Message}");

            return result;
        }

        public async Task<OASISResult<IOAPPSystemHolon>> RepublishOAPPSystemHolonAsync(IOAPPSystemHolonDNA OAPPSystemHolonDNA, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IOAPPSystemHolon> result = new OASISResult<IOAPPSystemHolon>();
            OASISResult<IOAPPSystemHolon> oappResult = await LoadOAPPSystemHolonAsync(OAPPSystemHolonDNA.Id, avatarId, OAPPSystemHolonDNA.VersionSequence, providerType);
            string errorMessage = "Error occured in RepublishOAPPSystemHolonAsync. Reason: ";

            if (oappResult != null && oappResult.Result != null && !oappResult.IsError)
                result = await RepublishOAPPSystemHolonAsync(oappResult.Result, avatarId, providerType);
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured loading the {OAPPSystemHolonUIName} with the LoadOAPPSystemHolonAsync method, reason: {oappResult.Message}");

            return result;
        }

        public OASISResult<IOAPPSystemHolon> RepublishOAPPSystemHolon(IOAPPSystemHolonDNA OAPPSystemHolonDNA, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IOAPPSystemHolon> result = new OASISResult<IOAPPSystemHolon>();
            OASISResult<IOAPPSystemHolon> oappResult = LoadOAPPSystemHolon(OAPPSystemHolonDNA.Id, avatarId, OAPPSystemHolonDNA.VersionSequence, providerType);
            string errorMessage = "Error occured in RepublishOAPPSystemHolon. Reason: ";

            if (oappResult != null && oappResult.Result != null && !oappResult.IsError)
                result = RepublishOAPPSystemHolon(oappResult.Result, avatarId, providerType);
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured loading the {OAPPSystemHolonUIName} with the LoadOAPPSystemHolon method, reason: {oappResult.Message}");

            return result;
        }

        public async Task<OASISResult<IOAPPSystemHolon>> RepublishOAPPSystemHolonAsync(Guid OAPPSystemHolonId, int version, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IOAPPSystemHolon> result = new OASISResult<IOAPPSystemHolon>();
            OASISResult<IOAPPSystemHolon> loadResult = await LoadOAPPSystemHolonAsync(OAPPSystemHolonId, avatarId, version, providerType);

            if (loadResult != null && loadResult.Result != null && !loadResult.IsError)
                result = await RepublishOAPPSystemHolonAsync(loadResult.Result, avatarId, providerType);
            else
                OASISErrorHandling.HandleError(ref result, $"Error occured in RepublishOAPPSystemHolonAsync loading the T with the LoadOAPPSystemHolonAsync method, reason: {loadResult.Message}");

            return result;
        }

        public OASISResult<IOAPPSystemHolon> RepublishOAPPSystemHolon(Guid OAPPSystemHolonId, int version, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IOAPPSystemHolon> result = new OASISResult<IOAPPSystemHolon>();
            OASISResult<IOAPPSystemHolon> loadResult = LoadOAPPSystemHolon(OAPPSystemHolonId, avatarId, version, providerType);

            if (loadResult != null && loadResult.Result != null && !loadResult.IsError)
                result = RepublishOAPPSystemHolon(loadResult.Result, avatarId, providerType);
            else
                OASISErrorHandling.HandleError(ref result, $"Error occured in RepublishOAPPSystemHolon loading the T with the LoadOAPPSystemHolon method, reason: {loadResult.Message}");

            return result;
        }

        public async Task<OASISResult<IOAPPSystemHolon>> DeactivateOAPPSystemHolonAsync(IOAPPSystemHolon T, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IOAPPSystemHolon> result = new OASISResult<IOAPPSystemHolon>();
            string errorMessage = "Error occured in DeactivateOAPPSystemHolonAsync. Reason: ";

            //T.OAPPSystemHolonDNA.IsActive = false;
            T.MetaData["Active"] = "0";

            OASISResult<IOAPPSystemHolon> oappResult = await SaveOAPPSystemHolonAsync(T, avatarId, providerType);

            if (oappResult != null && oappResult.Result != null && !oappResult.IsError)
            {
                result.Result = oappResult.Result; //ConvertOAPPSystemHolonToOAPPSystemHolonDNA(T);
                result.Message = "T Deactivateed";
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured saving the {OAPPSystemHolonUIName} with the SaveOAPPSystemHolonAsync method, reason: {oappResult.Message}");

            return result;
        }

        public OASISResult<IOAPPSystemHolon> DeactivateOAPPSystemHolon(IOAPPSystemHolon T, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IOAPPSystemHolon> result = new OASISResult<IOAPPSystemHolon>();
            string errorMessage = "Error occured in DeactivateOAPPSystemHolon. Reason: ";

            //T.OAPPSystemHolonDNA.IsActive = false;
            T.MetaData["Active"] = "0";

            OASISResult<IOAPPSystemHolon> oappResult = SaveOAPPSystemHolon(T, avatarId, providerType);

            if (oappResult != null && oappResult.Result != null && !oappResult.IsError)
            {
                result.Result = oappResult.Result; //ConvertOAPPSystemHolonToOAPPSystemHolonDNA(T);
                result.Message = "{OAPPSystemHolonUIName} Deactivateed";
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured saving the {OAPPSystemHolonUIName} with the SaveOAPPSystemHolon method, reason: {oappResult.Message}");

            return result;
        }

        public async Task<OASISResult<IOAPPSystemHolon>> DeactivateOAPPSystemHolonAsync(Guid OAPPSystemHolonId, int version, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IOAPPSystemHolon> result = new OASISResult<IOAPPSystemHolon>();
            OASISResult<IOAPPSystemHolon> loadResult = await LoadOAPPSystemHolonAsync(OAPPSystemHolonId, avatarId, version, providerType);

            if (loadResult != null && loadResult.Result != null && !loadResult.IsError)
                result = await DeactivateOAPPSystemHolonAsync(loadResult.Result, avatarId, providerType);
            else
                OASISErrorHandling.HandleError(ref result, $"Error occured in DeactivateOAPPSystemHolonAsync loading the T with the LoadOAPPSystemHolonAsync method, reason: {loadResult.Message}");

            return result;
        }

        public OASISResult<IOAPPSystemHolon> DeactivateOAPPSystemHolon(Guid OAPPSystemHolonId, int version, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IOAPPSystemHolon> result = new OASISResult<IOAPPSystemHolon>();
            OASISResult<IOAPPSystemHolon> loadResult = LoadOAPPSystemHolon(OAPPSystemHolonId, avatarId, version, providerType);

            if (loadResult != null && loadResult.Result != null && !loadResult.IsError)
                result = DeactivateOAPPSystemHolon(loadResult.Result, avatarId, providerType);
            else
                OASISErrorHandling.HandleError(ref result, $"Error occured in DeactivateOAPPSystemHolon loading the T with the LoadOAPPSystemHolon method, reason: {loadResult.Message}");

            return result;
        }

        public async Task<OASISResult<IOAPPSystemHolon>> DeactivateOAPPSystemHolonAsync(IOAPPSystemHolonDNA OAPPSystemHolonDNA, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IOAPPSystemHolon> result = new OASISResult<IOAPPSystemHolon>();
            OASISResult<IOAPPSystemHolon> oappResult = await LoadOAPPSystemHolonAsync(OAPPSystemHolonDNA.Id, avatarId, OAPPSystemHolonDNA.VersionSequence, providerType);
            string errorMessage = "Error occured in DeactivateOAPPSystemHolonAsync. Reason: ";

            if (oappResult != null && oappResult.Result != null && !oappResult.IsError)
                result = await DeactivateOAPPSystemHolonAsync(oappResult.Result, avatarId, providerType);
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured loading the {OAPPSystemHolonUIName} with the LoadOAPPSystemHolonAsync method, reason: {oappResult.Message}");

            return result;
        }

        public OASISResult<IOAPPSystemHolon> DeactivateOAPPSystemHolon(IOAPPSystemHolonDNA OAPPSystemHolonDNA, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IOAPPSystemHolon> result = new OASISResult<IOAPPSystemHolon>();
            OASISResult<IOAPPSystemHolon> oappResult = LoadOAPPSystemHolon(OAPPSystemHolonDNA.Id, avatarId, OAPPSystemHolonDNA.VersionSequence, providerType);
            string errorMessage = "Error occured in DeactivateOAPPSystemHolon. Reason: ";

            if (oappResult != null && oappResult.Result != null && !oappResult.IsError)
                result = DeactivateOAPPSystemHolon(oappResult.Result, avatarId, providerType);
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured loading the {OAPPSystemHolonUIName} with the LoadOAPPSystemHolon method, reason: {oappResult.Message}");

            return result;
        }

        public async Task<OASISResult<IOAPPSystemHolon>> ActivateOAPPSystemHolonAsync(IOAPPSystemHolon T, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IOAPPSystemHolon> result = new OASISResult<IOAPPSystemHolon>();
            string errorMessage = "Error occured in ActivateOAPPSystemHolonAsync. Reason: ";

            OASISResult<IAvatar> avatarResult = await AvatarManager.Instance.LoadAvatarAsync(avatarId, false, true, providerType);

            if (avatarResult != null && avatarResult.Result != null && !avatarResult.IsError)
            {
                //T.OAPPSystemHolonDNA.IsActive = true;
                T.MetaData["Active"] = "1";

                OASISResult<IOAPPSystemHolon> oappResult = await SaveOAPPSystemHolonAsync(T, avatarId, providerType);

                if (oappResult != null && oappResult.Result != null && !oappResult.IsError)
                {
                    result.Result = oappResult.Result; //ConvertOAPPSystemHolonToOAPPSystemHolonDNA(T);
                    result.Message = "T Activateed";
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured saving the {OAPPSystemHolonUIName} with the SaveOAPPSystemHolonAsync method, reason: {oappResult.Message}");
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured loading the Avatar with the LoadAvatar method, reason: {avatarResult.Message}");

            return result;
        }

        public OASISResult<IOAPPSystemHolon> ActivateOAPPSystemHolon(IOAPPSystemHolon T, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IOAPPSystemHolon> result = new OASISResult<IOAPPSystemHolon>();
            string errorMessage = "Error occured in ActivateOAPPSystemHolon. Reason: ";

            OASISResult<IAvatar> avatarResult = AvatarManager.Instance.LoadAvatar(avatarId, false, true, providerType);

            if (avatarResult != null && avatarResult.Result != null && !avatarResult.IsError)
            {
                //T.OAPPSystemHolonDNA.IsActive = true;
                T.MetaData["Active"] = "1";

                OASISResult<IOAPPSystemHolon> oappResult = SaveOAPPSystemHolon(T, avatarId, providerType);

                if (oappResult != null && oappResult.Result != null && !oappResult.IsError)
                {
                    result.Result = oappResult.Result; //ConvertOAPPSystemHolonToOAPPSystemHolonDNA(T);
                    result.Message = "{OAPPSystemHolonUIName} Activateed";
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured saving the {OAPPSystemHolonUIName} with the SaveOAPPSystemHolon method, reason: {oappResult.Message}");
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured loading the Avatar with the LoadAvatar method, reason: {avatarResult.Message}");

            return result;
        }

        public async Task<OASISResult<IOAPPSystemHolon>> ActivateOAPPSystemHolonAsync(IOAPPSystemHolonDNA OAPPSystemHolonDNA, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IOAPPSystemHolon> result = new OASISResult<IOAPPSystemHolon>();
            OASISResult<IOAPPSystemHolon> oappResult = await LoadOAPPSystemHolonAsync(OAPPSystemHolonDNA.Id, avatarId, OAPPSystemHolonDNA.VersionSequence, providerType);
            string errorMessage = "Error occured in ActivateOAPPSystemHolonAsync. Reason: ";

            if (oappResult != null && oappResult.Result != null && !oappResult.IsError)
                result = await ActivateOAPPSystemHolonAsync(oappResult.Result, avatarId, providerType);
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured loading the {OAPPSystemHolonUIName} with the LoadOAPPSystemHolonAsync method, reason: {oappResult.Message}");

            return result;
        }

        public OASISResult<IOAPPSystemHolon> ActivateOAPPSystemHolon(IOAPPSystemHolonDNA OAPPSystemHolonDNA, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IOAPPSystemHolon> result = new OASISResult<IOAPPSystemHolon>();
            OASISResult<IOAPPSystemHolon> oappResult = LoadOAPPSystemHolon(OAPPSystemHolonDNA.Id, avatarId, OAPPSystemHolonDNA.VersionSequence, providerType);
            string errorMessage = "Error occured in ActivateOAPPSystemHolon. Reason: ";

            if (oappResult != null && oappResult.Result != null && !oappResult.IsError)
                result = ActivateOAPPSystemHolon(oappResult.Result, avatarId, providerType);
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured loading the {OAPPSystemHolonUIName} with the LoadOAPPSystemHolon method, reason: {oappResult.Message}");

            return result;
        }

        public async Task<OASISResult<IOAPPSystemHolon>> ActivateOAPPSystemHolonAsync(Guid OAPPSystemHolonId, int version, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IOAPPSystemHolon> result = new OASISResult<IOAPPSystemHolon>();
            OASISResult<IOAPPSystemHolon> loadResult = await LoadOAPPSystemHolonAsync(OAPPSystemHolonId, avatarId, version, providerType);

            if (loadResult != null && loadResult.Result != null && !loadResult.IsError)
                result = await ActivateOAPPSystemHolonAsync(loadResult.Result, avatarId, providerType);
            else
                OASISErrorHandling.HandleError(ref result, $"Error occured in ActivateOAPPSystemHolonAsync loading the T with the LoadOAPPSystemHolonAsync method, reason: {loadResult.Message}");

            return result;
        }

        public OASISResult<IOAPPSystemHolon> ActivateOAPPSystemHolon(Guid OAPPSystemHolonId, int version, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IOAPPSystemHolon> result = new OASISResult<IOAPPSystemHolon>();
            OASISResult<IOAPPSystemHolon> loadResult = LoadOAPPSystemHolon(OAPPSystemHolonId, avatarId, version, providerType);

            if (loadResult != null && loadResult.Result != null && !loadResult.IsError)
                result = ActivateOAPPSystemHolon(loadResult.Result, avatarId, providerType);
            else
                OASISErrorHandling.HandleError(ref result, $"Error occured in ActivateOAPPSystemHolon loading the T with the LoadOAPPSystemHolon method, reason: {loadResult.Message}");

            return result;
        }

        public async Task<OASISResult<IDownloadedOAPPSystemHolon>> DownloadOAPPSystemHolonAsync(Guid avatarId, IOAPPSystemHolon T, string fullDownloadPath, bool reInstall = false, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IDownloadedOAPPSystemHolon> result = new OASISResult<IDownloadedOAPPSystemHolon>();
            string errorMessage = "Error occured in OAPPSystemManagerBase.DownloadOAPPSystemHolonAsync. Reason: ";
            DownloadedOAPPSystemHolon downloadedOAPPSystemHolon = null;

            try
            {
                if (!fullDownloadPath.Contains(".OAPPSystemHolon"))
                    fullDownloadPath = Path.Combine(fullDownloadPath, string.Concat(T.Name, "_v", T.OAPPSystemHolonDNA.Version, ".OAPPSystemHolon"));

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
                            _fileLength = T.OAPPSystemHolonDNA.OAPPSystemHolonFileSize;

                        _progress = 0;

                        string publishedOAPPSystemHolonFileName = string.Concat(T.Name, "_v", T.OAPPSystemHolonDNA.Version, ".OAPPSystemHolon");
                        OnOAPPSystemHolonInstallStatusChanged?.Invoke(this, new OAPPSystemHolonInstallStatusEventArgs() { OAPPSystemHolonDNA = T.OAPPSystemHolonDNA, Status = Enums.OAPPSystemHolonInstallStatus.Downloading });
                        await storage.DownloadObjectAsync(GOOGLE_CLOUD_BUCKET_NAME, publishedOAPPSystemHolonFileName, fileStream, downloadObjectOptions, progress: progressReporter);

                        _progress = 100;
                        OnOAPPSystemHolonDownloadStatusChanged?.Invoke(this, new OAPPSystemHolonDownloadProgressEventArgs() { Progress = _progress, Status = Enums.OAPPSystemHolonDownloadStatus.Downloading });
                        CLIEngine.DisposeProgressBar(false);
                        Console.WriteLine("");
                        fileStream.Close();
                    }

                    OASISResult<IAvatar> avatarResult = await AvatarManager.Instance.LoadAvatarAsync(avatarId, false, true, providerType);

                    if (avatarResult != null && !avatarResult.IsError && avatarResult.Result != null)
                    {
                        if (!reInstall)
                        {
                            T.OAPPSystemHolonDNA.Downloads++;

                            downloadedOAPPSystemHolon = new DownloadedOAPPSystemHolon()
                            {
                                Name = string.Concat(T.OAPPSystemHolonDNA.Name, " Downloaded Holon"),
                                Description = string.Concat(T.OAPPSystemHolonDNA.Description, " Downloaded Holon"),
                                OAPPSystemHolonDNA = T.OAPPSystemHolonDNA,
                                DownloadedBy = avatarId,
                                DownloadedByAvatarUsername = avatarResult.Result.Username,
                                DownloadedOn = DateTime.Now,
                                DownloadedPath = fullDownloadPath
                            };

                            await UpdateDownloadCountsAsync(downloadedOAPPSystemHolon, T.OAPPSystemHolonDNA, avatarId, result, errorMessage, providerType);

                            downloadedOAPPSystemHolon.MetaData["OAPPSystemHolonId"] = T.OAPPSystemHolonDNA.Id.ToString();
                            downloadedOAPPSystemHolon.MetaData["OAPPSystemHolonDNAJSON"] = JsonSerializer.Serialize(downloadedOAPPSystemHolon.OAPPSystemHolonDNA);
                            OASISResult<DownloadedOAPPSystemHolon> saveResult = await downloadedOAPPSystemHolon.SaveAsync<DownloadedOAPPSystemHolon>();

                            if (!(saveResult != null && saveResult.Result != null && !saveResult.IsError))
                                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling SaveAsync method on downloadedOAPPSystemHolon. Reason: {saveResult.Message}");
                        }
                        else
                        {
                            OASISResult<IEnumerable<DownloadedOAPPSystemHolon>> downloadedOAPPSystemHolonResult = await Data.LoadHolonsByMetaDataAsync<DownloadedOAPPSystemHolon>("OAPPSystemHolonId", T.OAPPSystemHolonDNA.Id.ToString(), HolonType.All, true, true, 0, true, false, 0, HolonType.All, 0, providerType);

                            if (downloadedOAPPSystemHolonResult != null && !downloadedOAPPSystemHolonResult.IsError && downloadedOAPPSystemHolonResult.Result != null)
                            {
                                downloadedOAPPSystemHolon = downloadedOAPPSystemHolonResult.Result.FirstOrDefault();
                                downloadedOAPPSystemHolon.DownloadedOn = DateTime.Now;
                                downloadedOAPPSystemHolon.DownloadedBy = avatarId;
                                downloadedOAPPSystemHolon.DownloadedByAvatarUsername = avatarResult.Result.Username;
                                downloadedOAPPSystemHolon.DownloadedPath = fullDownloadPath;

                                OASISResult<DownloadedOAPPSystemHolon> saveResult = await downloadedOAPPSystemHolon.SaveAsync<DownloadedOAPPSystemHolon>();

                                if (!(saveResult != null && saveResult.Result != null && !saveResult.IsError))
                                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling SaveAsync method on downloadedOAPPSystemHolon. Reason: {saveResult.Message}");
                            }
                            else
                                OASISErrorHandling.HandleWarning(ref result, $"The {OAPPSystemHolonUIName} was downloaded but the DownloadedOAPPSystemHolon could not be found. Reason: {downloadedOAPPSystemHolonResult.Message}");
                        }
                    }
                    else
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadAvatarAsync method. Reason: {avatarResult.Message}");


                    if (!result.IsError)
                    {
                        result.Result = downloadedOAPPSystemHolon;
                        OASISResult<IOAPPSystemHolon> oappSaveResult = await SaveOAPPSystemHolonAsync(T, avatarId, providerType);

                        if (oappSaveResult != null && !oappSaveResult.IsError && oappSaveResult.Result != null)
                        {
                            if (result.InnerMessages.Count > 0)
                                result.Message = $"{OAPPSystemHolonUIName} successfully downloaded but there were {result.WarningCount} warnings:\n\n {OASISResultHelper.BuildInnerMessageError(result.InnerMessages)}";
                            else
                                result.Message = "{OAPPSystemHolonUIName} Successfully Downloaded";
                        }
                        else
                            OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling SaveOAPPSystemHolonAsync method. Reason: {oappSaveResult.Message}");
                    }
                }
                catch (Exception e)
                {
                    CLIEngine.DisposeProgressBar(false);
                    Console.WriteLine("");
                    OASISErrorHandling.HandleError(ref result, $"An error occured downloading the {OAPPSystemHolonUIName} from cloud storage. Reason: {e}");
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
            //    OnOAPPSystemHolonDownloadStatusChanged?.Invoke(this, new OAPPSystemHolonDownloadProgressEventArgs { OAPPSystemHolonDNA = T.OAPPSystemHolonDNA, Status = Enums.OAPPSystemHolonDownloadStatus.Error, ErrorMessage = result.Message });

            return result;
        }

        public async Task<OASISResult<IInstalledOAPPSystemHolon>> DownloadAndInstallOAPPSystemHolonAsync(Guid avatarId, IOAPPSystemHolon T, string fullInstallPath, string fullDownloadPath = "", bool createOAPPSystemHolonDirectory = true, bool reInstall = false, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IInstalledOAPPSystemHolon> result = new OASISResult<IInstalledOAPPSystemHolon>();
            string errorMessage = "Error occured in OAPPSystemManagerBase.DownloadAndInstallOAPPSystemHolonAsync. Reason: ";
            bool isFullDownloadPathTemp = false;

            try
            {
                if (string.IsNullOrEmpty(fullDownloadPath))
                {
                    string tempPath = Path.GetTempPath();
                    fullDownloadPath = Path.Combine(tempPath, string.Concat(T.Name, ".OAPPSystemHolon"));
                    isFullDownloadPathTemp = true;
                }

                if (File.Exists(fullDownloadPath))
                    File.Delete(fullDownloadPath);

                if (T.PublishedOAPPSystemHolon != null)
                {
                    await File.WriteAllBytesAsync(fullDownloadPath, T.PublishedOAPPSystemHolon);
                    result = await InstallOAPPSystemHolonAsync(avatarId, fullDownloadPath, fullInstallPath, createOAPPSystemHolonDirectory, null, reInstall, providerType);
                }
                else
                {
                    OASISResult<IDownloadedOAPPSystemHolon> downloadResult = await DownloadOAPPSystemHolonAsync(avatarId, T, fullDownloadPath, reInstall, providerType);

                    if (!fullDownloadPath.Contains(".OAPPSystemHolon"))
                        fullDownloadPath = Path.Combine(fullDownloadPath, string.Concat(T.Name, "_v", T.OAPPSystemHolonDNA.Version, ".OAPPSystemHolon"));

                    if (downloadResult != null && downloadResult.Result != null && !downloadResult.IsError)
                        result = await InstallOAPPSystemHolonAsync(avatarId, fullDownloadPath, fullInstallPath, createOAPPSystemHolonDirectory, downloadResult.Result, reInstall, providerType);
                    else
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured downloading the {OAPPSystemHolonUIName} with the DownloadOAPPSystemHolonAsync method, reason: {downloadResult.Message}");
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
                OnOAPPSystemHolonInstallStatusChanged?.Invoke(this, new OAPPSystemHolonInstallStatusEventArgs() { OAPPSystemHolonDNA = T.OAPPSystemHolonDNA, Status = Enums.OAPPSystemHolonInstallStatus.Error, ErrorMessage = result.Message });

            return result;
        }

        public async Task<OASISResult<IInstalledOAPPSystemHolon>> InstallOAPPSystemHolonAsync(Guid avatarId, string fullPathToPublishedOAPPSystemHolonFile, string fullInstallPath, bool createOAPPSystemHolonDirectory = true, IDownloadedOAPPSystemHolon downloadedOAPPSystemHolon = null, bool reInstall = false, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IInstalledOAPPSystemHolon> result = new OASISResult<IInstalledOAPPSystemHolon>();
            string errorMessage = "Error occured in OAPPSystemManagerBase.InstallOAPPSystemHolonAsync. Reason: ";
            IOAPPSystemHolonDNA OAPPSystemHolonDNA = null;
            string tempPath = "";
            InstalledOAPPSystemHolon installedOAPPSystemHolon = null;
            int totalInstalls = 0;

            try
            {
                tempPath = Path.GetTempPath();
                tempPath = Path.Combine(tempPath, "{OAPPSystemHolonUIName}");

                if (Directory.Exists(tempPath))
                    Directory.Delete(tempPath, true);

                //Unzip
                OnOAPPSystemHolonInstallStatusChanged?.Invoke(this, new OAPPSystemHolonInstallStatusEventArgs() { Status = Enums.OAPPSystemHolonInstallStatus.Decompressing });
                ZipFile.ExtractToDirectory(fullPathToPublishedOAPPSystemHolonFile, tempPath, Encoding.Default, true);
                OASISResult<IOAPPSystemHolonDNA> OAPPSystemHolonDNAResult = await ReadOAPPSystemHolonDNAAsync(tempPath);

                if (OAPPSystemHolonDNAResult != null && OAPPSystemHolonDNAResult.Result != null && !OAPPSystemHolonDNAResult.IsError)
                {
                    //Load the T from the OASIS to make sure the OAPPSystemHolonDNA is valid (and has not been tampered with).

                    //TODO: Check if this works ok? What if they tamper with the VersionSequence in the DNA file?!
                    OASISResult<IOAPPSystemHolon> OAPPSystemHolonLoadResult = await LoadOAPPSystemHolonAsync(OAPPSystemHolonDNAResult.Result.Id, avatarId, OAPPSystemHolonDNAResult.Result.VersionSequence, providerType);
                    //OASISResult<IOAPPSystemHolon> OAPPSystemHolonLoadResult = await LoadOAPPSystemHolonAsync(OAPPSystemHolonDNAResult.Result.Id, false, 0, providerType);

                    if (OAPPSystemHolonLoadResult != null && OAPPSystemHolonLoadResult.Result != null && !OAPPSystemHolonLoadResult.IsError)
                    {
                        //TODO: Not sure if we want to add a check here to compare the OAPPSystemHolonDNA in the T dir with the one stored in the OASIS?
                        OAPPSystemHolonDNA = OAPPSystemHolonLoadResult.Result.OAPPSystemHolonDNA;

                        if (createOAPPSystemHolonDirectory)
                            fullInstallPath = Path.Combine(fullInstallPath, string.Concat(OAPPSystemHolonDNAResult.Result.Name, "_v", OAPPSystemHolonDNAResult.Result.Version));

                        if (Directory.Exists(fullInstallPath))
                            Directory.Delete(fullInstallPath, true);

                        //Directory.CreateDirectory(fullInstallPath);
                        Directory.Move(tempPath, fullInstallPath);
                        //Directory.Delete(tempPath);

                        OnOAPPSystemHolonInstallStatusChanged?.Invoke(this, new OAPPSystemHolonInstallStatusEventArgs() { OAPPSystemHolonDNA = OAPPSystemHolonDNAResult.Result, Status = Enums.OAPPSystemHolonInstallStatus.Installing });
                        OASISResult<IAvatar> avatarResult = await AvatarManager.Instance.LoadAvatarAsync(avatarId, false, true, providerType);

                        if (avatarResult != null && !avatarResult.IsError && avatarResult.Result != null)
                        {
                            if (downloadedOAPPSystemHolon == null)
                            {
                                //OASISResult<DownloadedOAPPSystemHolon> downloadedOAPPSystemHolonResult = await Data.LoadHolonsByMetaDataAsync<DownloadedOAPPSystemHolon>("OAPPSystemHolonId", OAPPSystemHolonDNAResult.Result.Id.ToString(), false, false, 0, true, 0, false, HolonType.All, providerType);
                                OASISResult<IEnumerable<DownloadedOAPPSystemHolon>> downloadedOAPPSystemHolonResult = await Data.LoadHolonsByMetaDataAsync<DownloadedOAPPSystemHolon>("OAPPSystemHolonId", OAPPSystemHolonDNAResult.Result.Id.ToString(), HolonType.All, true, true, 0, true, false, 0, HolonType.All, 0, providerType);

                                if (downloadedOAPPSystemHolonResult != null && !downloadedOAPPSystemHolonResult.IsError && downloadedOAPPSystemHolonResult.Result != null)
                                    downloadedOAPPSystemHolon = downloadedOAPPSystemHolonResult.Result.FirstOrDefault();
                                else
                                    OASISErrorHandling.HandleWarning(ref result, $"The {OAPPSystemHolonUIName} was installed but the DownloadedOAPPSystemHolon could not be found. Reason: {downloadedOAPPSystemHolonResult.Message}");
                            }

                            if (!reInstall)
                            {
                                //If it's a re-install then it doesnt count as an install so we dont need to update the counts.
                                OAPPSystemHolonDNA.Installs++;

                                installedOAPPSystemHolon = new InstalledOAPPSystemHolon()
                                {
                                    Name = string.Concat(OAPPSystemHolonDNA.Name, " Installed Holon"),
                                    Description = string.Concat(OAPPSystemHolonDNA.Description, " Installed Holon"),
                                    //OAPPSystemHolonId = OAPPSystemHolonDNAResult.Result.OAPPSystemHolonId,
                                    OAPPSystemHolonDNA = OAPPSystemHolonDNA,
                                    InstalledBy = avatarId,
                                    InstalledByAvatarUsername = avatarResult.Result.Username,
                                    InstalledOn = DateTime.Now,
                                    InstalledPath = fullInstallPath,
                                    //DownloadedOAPPSystemHolon = downloadedOAPPSystemHolon,
                                    DownloadedBy = downloadedOAPPSystemHolon.DownloadedBy,
                                    DownloadedByAvatarUsername = downloadedOAPPSystemHolon.DownloadedByAvatarUsername,
                                    DownloadedOn = downloadedOAPPSystemHolon.DownloadedOn,
                                    DownloadedPath = downloadedOAPPSystemHolon.DownloadedPath,
                                    DownloadedOAPPSystemHolonHolonId = downloadedOAPPSystemHolon.Id,
                                    Active = "1",
                                    //OAPPSystemHolonVersion = OAPPSystemHolonDNA.Version
                                };

                                installedOAPPSystemHolon.MetaData["Version"] = OAPPSystemHolonDNA.Version;
                                installedOAPPSystemHolon.MetaData["VersionSequence"] = OAPPSystemHolonDNA.VersionSequence;
                                installedOAPPSystemHolon.MetaData["OAPPSystemHolonId"] = OAPPSystemHolonDNA.Id;
                                
                                await UpdateInstallCountsAsync(installedOAPPSystemHolon, OAPPSystemHolonDNA, avatarId, result, errorMessage, providerType);
                            }
                            else
                            {
                                OASISResult<IInstalledOAPPSystemHolon> installedOAPPSystemHolonResult = await LoadInstalledOAPPSystemHolonAsync(avatarId, OAPPSystemHolonDNAResult.Result.Id, OAPPSystemHolonDNAResult.Result.Version, false, providerType);

                                if (installedOAPPSystemHolonResult != null && installedOAPPSystemHolonResult.Result != null && !installedOAPPSystemHolonResult.IsError)
                                {
                                    installedOAPPSystemHolon = (InstalledOAPPSystemHolon)installedOAPPSystemHolonResult.Result;
                                    installedOAPPSystemHolon.Active = "1";
                                    installedOAPPSystemHolon.UninstalledBy = Guid.Empty;
                                    installedOAPPSystemHolon.UninstalledByAvatarUsername = "";
                                    installedOAPPSystemHolon.UninstalledOn = DateTime.MinValue;
                                    installedOAPPSystemHolon.InstalledBy = avatarId;
                                    installedOAPPSystemHolon.InstalledByAvatarUsername = avatarResult.Result.Username;
                                    installedOAPPSystemHolon.InstalledOn = DateTime.Now;
                                    installedOAPPSystemHolon.InstalledPath = fullInstallPath;
                                    installedOAPPSystemHolon.DownloadedBy = downloadedOAPPSystemHolon.DownloadedBy;
                                    installedOAPPSystemHolon.DownloadedByAvatarUsername = downloadedOAPPSystemHolon.DownloadedByAvatarUsername;
                                    installedOAPPSystemHolon.DownloadedOn = downloadedOAPPSystemHolon.DownloadedOn;
                                    installedOAPPSystemHolon.DownloadedPath = downloadedOAPPSystemHolon.DownloadedPath;
                                }
                                else
                                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured re-installing {OAPPSystemHolonUIName} calling LoadInstalledOAPPSystemHolonAsync. Reason: {installedOAPPSystemHolonResult.Message}");
                            }

                            if (!result.IsError)
                            {
                                OASISResult<IOAPPSystemHolon> saveResult = await SaveOAPPSystemHolonAsync(installedOAPPSystemHolon, avatarId, providerType);

                                if (saveResult != null && saveResult.Result != null && !saveResult.IsError)
                                {
                                    //result.Result = installedOAPPSystemHolon;
                                    //result.Result.DownloadedOAPPSystemHolon = downloadedOAPPSystemHolon;
                                    OAPPSystemHolonLoadResult.Result.OAPPSystemHolonDNA = OAPPSystemHolonDNA;

                                    OASISResult<IOAPPSystemHolon> oappSaveResult = await SaveOAPPSystemHolonAsync(OAPPSystemHolonLoadResult.Result, avatarId, providerType);

                                    if (oappSaveResult != null && !oappSaveResult.IsError && oappSaveResult.Result != null)
                                    {
                                        if (OAPPSystemHolonDNAResult.Result.STARODKVersion != OASISBootLoader.OASISBootLoader.STARODKVersion)
                                            OASISErrorHandling.HandleWarning(ref result, $"The STAR ODK Version {OAPPSystemHolonDNAResult.Result.STARODKVersion} does not match the current version {OASISBootLoader.OASISBootLoader.STARODKVersion}. This may lead to issues, it is recommended to make sure the versions match.");

                                        if (OAPPSystemHolonDNAResult.Result.OASISVersion != OASISBootLoader.OASISBootLoader.OASISVersion)
                                            OASISErrorHandling.HandleWarning(ref result, $"The OASIS Version {OAPPSystemHolonDNAResult.Result.OASISVersion} does not match the current version {OASISBootLoader.OASISBootLoader.OASISVersion}. This may lead to issues, it is recommended to make sure the versions match.");

                                        if (OAPPSystemHolonDNAResult.Result.COSMICVersion != OASISBootLoader.OASISBootLoader.COSMICVersion)
                                            OASISErrorHandling.HandleWarning(ref result, $"The COSMIC Version {OAPPSystemHolonDNAResult.Result.COSMICVersion} does not match the current version {OASISBootLoader.OASISBootLoader.COSMICVersion}. This may lead to issues, it is recommended to make sure the versions match.");

                                        if (result.InnerMessages.Count > 0)
                                            result.Message = $"{OAPPSystemHolonUIName} successfully installed but there were {result.WarningCount} warnings:\n\n {OASISResultHelper.BuildInnerMessageError(result.InnerMessages)}";
                                        else
                                            result.Message = "{OAPPSystemHolonUIName} Successfully Installed";

                                        result.Result = installedOAPPSystemHolon;
                                        OnOAPPSystemHolonInstallStatusChanged?.Invoke(this, new OAPPSystemHolonInstallStatusEventArgs() { OAPPSystemHolonDNA = OAPPSystemHolonDNAResult.Result, Status = Enums.OAPPSystemHolonInstallStatus.Installed });
                                    }
                                    else
                                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling SaveOAPPSystemHolonAsync method. Reason: {oappSaveResult.Message}");
                                }
                                else
                                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling SaveAsync method. Reason: {saveResult.Message}");
                            }
                            else
                                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadAvatarAsync method. Reason: {avatarResult.Message}");
                        }
                    }
                    else
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadOAPPSystemHolonAsync method. Reason: {OAPPSystemHolonLoadResult.Message}");
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
                OnOAPPSystemHolonInstallStatusChanged?.Invoke(this, new OAPPSystemHolonInstallStatusEventArgs() { OAPPSystemHolonDNA = OAPPSystemHolonDNA, Status = Enums.OAPPSystemHolonInstallStatus.Error, ErrorMessage = result.Message });

            return result;
        }

        public OASISResult<IInstalledOAPPSystemHolon> InstallOAPPSystemHolon(Guid avatarId, string fullPathToPublishedOAPPSystemHolonFile, string fullInstallPath, bool createOAPPSystemHolonDirectory = true, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IInstalledOAPPSystemHolon> result = new OASISResult<IInstalledOAPPSystemHolon>();
            string errorMessage = "Error occured in OAPPSystemManagerBase.InstallOAPPSystemHolon. Reason: ";
            IOAPPSystemHolonDNA OAPPSystemHolonDNA = null;

            try
            {
                OASISResult<IOAPPSystemHolonDNA> OAPPSystemHolonDNAResult = ReadOAPPSystemHolonDNA(fullPathToPublishedOAPPSystemHolonFile);

                if (OAPPSystemHolonDNAResult != null && OAPPSystemHolonDNAResult.Result != null && !OAPPSystemHolonDNAResult.IsError)
                {
                    //Load the T from the OASIS to make sure the OAPPSystemHolonDNA is valid (and has not been tampered with).
                    OASISResult<IOAPPSystemHolon> oappResult = LoadOAPPSystemHolon(OAPPSystemHolonDNAResult.Result.Id, avatarId, OAPPSystemHolonDNAResult.Result.VersionSequence, providerType);

                    if (oappResult != null && oappResult.Result != null && !oappResult.IsError)
                    {
                        //TODO: Not sure if we want to add a check here to compare the OAPPSystemHolonDNA in the T dir with the one stored in the OASIS?
                        OAPPSystemHolonDNA = oappResult.Result.OAPPSystemHolonDNA;

                        if (createOAPPSystemHolonDirectory)
                            fullInstallPath = Path.Combine(fullInstallPath, OAPPSystemHolonDNAResult.Result.Name);

                        if (Directory.Exists(fullInstallPath))
                            Directory.Delete(fullInstallPath, true);

                        Directory.CreateDirectory(fullInstallPath);

                        OnOAPPSystemHolonInstallStatusChanged?.Invoke(this, new OAPPSystemHolonInstallStatusEventArgs() { OAPPSystemHolonDNA = OAPPSystemHolonDNAResult.Result, Status = Enums.OAPPSystemHolonInstallStatus.Decompressing });
                        ZipFile.ExtractToDirectory(fullPathToPublishedOAPPSystemHolonFile, fullInstallPath, Encoding.Default, true);

                        OnOAPPSystemHolonInstallStatusChanged?.Invoke(this, new OAPPSystemHolonInstallStatusEventArgs() { OAPPSystemHolonDNA = OAPPSystemHolonDNAResult.Result, Status = Enums.OAPPSystemHolonInstallStatus.Installing });
                        OASISResult<IAvatar> avatarResult = AvatarManager.Instance.LoadAvatar(avatarId, false, true, providerType);

                        if (avatarResult != null && !avatarResult.IsError && avatarResult.Result != null)
                        {
                            InstalledOAPPSystemHolon installedOAPPSystemHolon = new InstalledOAPPSystemHolon()
                            {
                                //OAPPSystemHolonId = OAPPSystemHolonDNAResult.Result.OAPPSystemHolonId,
                                OAPPSystemHolonDNA = OAPPSystemHolonDNAResult.Result,
                                InstalledBy = avatarId,
                                InstalledByAvatarUsername = avatarResult.Result.Username,
                                InstalledOn = DateTime.Now,
                                InstalledPath = fullInstallPath
                            };

                            OASISResult<IHolon> saveResult = installedOAPPSystemHolon.Save();

                            if (saveResult != null && saveResult.Result != null && !saveResult.IsError)
                            {
                                result.Result = installedOAPPSystemHolon;
                                //OAPPSystemHolonDNA.Downloads++;
                                OAPPSystemHolonDNA.Installs++;
                                oappResult.Result.OAPPSystemHolonDNA = OAPPSystemHolonDNA;

                                OASISResult<IOAPPSystemHolon> oappSaveResult = SaveOAPPSystemHolon(oappResult.Result, avatarId, providerType);

                                if (oappSaveResult != null && !oappSaveResult.IsError && oappSaveResult.Result != null)
                                {
                                    if (OAPPSystemHolonDNAResult.Result.STARODKVersion != OASISBootLoader.OASISBootLoader.STARODKVersion)
                                        OASISErrorHandling.HandleWarning(ref result, $"The STAR ODK Version {OAPPSystemHolonDNAResult.Result.STARODKVersion} does not match the current version {OASISBootLoader.OASISBootLoader.STARODKVersion}. This may lead to issues, it is recommended to make sure the versions match.");

                                    if (OAPPSystemHolonDNAResult.Result.OASISVersion != OASISBootLoader.OASISBootLoader.OASISVersion)
                                        OASISErrorHandling.HandleWarning(ref result, $"The OASIS Version {OAPPSystemHolonDNAResult.Result.OASISVersion} does not match the current version {OASISBootLoader.OASISBootLoader.OASISVersion}. This may lead to issues, it is recommended to make sure the versions match.");

                                    if (OAPPSystemHolonDNAResult.Result.COSMICVersion != OASISBootLoader.OASISBootLoader.COSMICVersion)
                                        OASISErrorHandling.HandleWarning(ref result, $"The COSMIC Version {OAPPSystemHolonDNAResult.Result.COSMICVersion} does not match the current version {OASISBootLoader.OASISBootLoader.COSMICVersion}. This may lead to issues, it is recommended to make sure the versions match.");

                                    if (result.InnerMessages.Count > 0)
                                        result.Message = $"{OAPPSystemHolonUIName} successfully installed but there were {result.WarningCount} warnings:\n\n {OASISResultHelper.BuildInnerMessageError(result.InnerMessages)}";
                                    else
                                        result.Message = "{OAPPSystemHolonUIName} Successfully Installed";

                                    OnOAPPSystemHolonInstallStatusChanged?.Invoke(this, new OAPPSystemHolonInstallStatusEventArgs() { OAPPSystemHolonDNA = OAPPSystemHolonDNAResult.Result, Status = Enums.OAPPSystemHolonInstallStatus.Installed });
                                }
                                else
                                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling SaveOAPPSystemHolonAsync method. Reason: {oappSaveResult.Message}");
                            }
                            else
                                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling SaveAsync method. Reason: {saveResult.Message}");
                        }
                        else
                            OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadAvatarAsync method. Reason: {avatarResult.Message}");
                    }
                    else
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadOAPPSystemHolonAsync method. Reason: {oappResult.Message}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} {ex}");
            }

            if (result.IsError)
                OnOAPPSystemHolonInstallStatusChanged?.Invoke(this, new OAPPSystemHolonInstallStatusEventArgs() { OAPPSystemHolonDNA = OAPPSystemHolonDNA, Status = Enums.OAPPSystemHolonInstallStatus.Error, ErrorMessage = result.Message });

            return result;
        }

        public OASISResult<IInstalledOAPPSystemHolon> InstallOAPPSystemHolon(Guid avatarId, IOAPPSystemHolon T, string fullInstallPath, string fullDownloadPath = "", bool createOAPPSystemHolonDirectory = true, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IInstalledOAPPSystemHolon> result = new OASISResult<IInstalledOAPPSystemHolon>();
            string errorMessage = "Error occured in OAPPSystemManagerBase.InstallOAPPSystemHolon. Reason: ";

            try
            {
                string SourcePath = Path.Combine("temp", T.Name, ".OAPPSystemHolon");

                if (T.PublishedOAPPSystemHolon != null)
                {
                    File.WriteAllBytes(SourcePath, T.PublishedOAPPSystemHolon);
                    result = InstallOAPPSystemHolon(avatarId, SourcePath, fullInstallPath, createOAPPSystemHolonDirectory, providerType);
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

                        using var fileStream = File.OpenWrite(SourcePath);
                        _fileLength = fileStream.Length;
                        _progress = 0;

                        OnOAPPSystemHolonInstallStatusChanged?.Invoke(this, new OAPPSystemHolonInstallStatusEventArgs() { OAPPSystemHolonDNA = T.OAPPSystemHolonDNA, Status = Enums.OAPPSystemHolonInstallStatus.Downloading });
                        storage.DownloadObject(GOOGLE_CLOUD_BUCKET_NAME, string.Concat(T.Name, ".OAPPSystemHolon"), fileStream, downloadObjectOptions, progress: progressReporter);

                        _progress = 100;
                        OnOAPPSystemHolonDownloadStatusChanged?.Invoke(this, new OAPPSystemHolonDownloadProgressEventArgs() { Progress = _progress, Status = Enums.OAPPSystemHolonDownloadStatus.Downloading });
                        CLIEngine.DisposeProgressBar(false);
                        Console.WriteLine("");

                        result = InstallOAPPSystemHolon(avatarId, SourcePath, fullInstallPath, createOAPPSystemHolonDirectory, providerType);
                    }
                    catch (Exception ex)
                    {
                        OASISErrorHandling.HandleError(ref result, $"An error occured downloading the {OAPPSystemHolonUIName} from cloud storage. Reason: {ex}");
                    }
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} {ex}");
            }

            if (result.IsError)
                OnOAPPSystemHolonInstallStatusChanged?.Invoke(this, new OAPPSystemHolonInstallStatusEventArgs() { OAPPSystemHolonDNA = T.OAPPSystemHolonDNA, Status = Enums.OAPPSystemHolonInstallStatus.Error, ErrorMessage = result.Message });

            return result;
        }

        public async Task<OASISResult<IInstalledOAPPSystemHolon>> InstallOAPPSystemHolonAsync(Guid avatarId, Guid OAPPSystemHolonId, int version, string fullInstallPath, string fullDownloadPath = "", bool createOAPPSystemHolonDirectory = true, bool reInstall = false, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IInstalledOAPPSystemHolon> result = new OASISResult<IInstalledOAPPSystemHolon>();
            OASISResult<IOAPPSystemHolon> OAPPSystemHolonResult = await LoadOAPPSystemHolonAsync(OAPPSystemHolonId, avatarId, version, providerType);

            if (OAPPSystemHolonResult != null && !OAPPSystemHolonResult.IsError && OAPPSystemHolonResult.Result != null)
                result = await DownloadAndInstallOAPPSystemHolonAsync(avatarId, OAPPSystemHolonResult.Result, fullInstallPath, fullDownloadPath, createOAPPSystemHolonDirectory, reInstall, providerType);
            else
            {
                OASISErrorHandling.HandleError(ref result, $"Error occured in OAPPSystemManagerBase.InstallOAPPSystemHolonAsync loading the {OAPPSystemHolonUIName} with the LoadOAPPSystemHolonAsync method, reason: {OASISErrorHandling.ProcessMessage(result, $"No result found for id {OAPPSystemHolonId.ToString()}")}");
                OnOAPPSystemHolonInstallStatusChanged?.Invoke(this, new OAPPSystemHolonInstallStatusEventArgs() { Status = Enums.OAPPSystemHolonInstallStatus.Error, ErrorMessage = result.Message });
            }

            return result;
        }

        public OASISResult<IInstalledOAPPSystemHolon> InstallOAPPSystemHolon(Guid avatarId, Guid OAPPSystemHolonId, int version, string fullInstallPath, string fullDownloadPath = "", bool createOAPPSystemHolonDirectory = true, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IInstalledOAPPSystemHolon> result = new OASISResult<IInstalledOAPPSystemHolon>();
            OASISResult<IOAPPSystemHolon> OAPPSystemHolonResult = LoadOAPPSystemHolon(OAPPSystemHolonId, avatarId, version, providerType);

            if (OAPPSystemHolonResult != null && !OAPPSystemHolonResult.IsError && OAPPSystemHolonResult.Result != null)
                result = InstallOAPPSystemHolon(avatarId, OAPPSystemHolonResult.Result, fullInstallPath, fullDownloadPath, createOAPPSystemHolonDirectory, providerType);
            else
            {
                OASISErrorHandling.HandleError(ref result, $"Error occured in OAPPSystemManagerBase.InstallOAPPSystemHolon loading the {OAPPSystemHolonUIName} with the LoadOAPPSystemHolon method, reason: {result.Message}");
                OnOAPPSystemHolonInstallStatusChanged?.Invoke(this, new OAPPSystemHolonInstallStatusEventArgs() { Status = Enums.OAPPSystemHolonInstallStatus.Error, ErrorMessage = result.Message });
            }

            return result;
        }

        public async Task<OASISResult<IInstalledOAPPSystemHolon>> UninstallOAPPSystemHolonAsync(IInstalledOAPPSystemHolon installedOAPPSystemHolon, Guid avatarId, string errorMessage, ProviderType providerType)
        {
            OASISResult<IInstalledOAPPSystemHolon> result = new OASISResult<IInstalledOAPPSystemHolon>();

            try
            {
                Directory.Delete(installedOAPPSystemHolon.InstalledPath, true);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} Error occured attempting to delete the {OAPPSystemHolonUIName} folder ({installedOAPPSystemHolon.InstalledPath}) Reason: {ex.Message}");
            }

            //if (!result.IsError)
            //{
                OASISResult<IAvatar> avatarResult = await AvatarManager.Instance.LoadAvatarAsync(avatarId, false, true, providerType, 0);

                if (avatarResult != null && avatarResult.Result != null && !avatarResult.IsError)
                {
                    installedOAPPSystemHolon.UninstalledBy = avatarId;
                    installedOAPPSystemHolon.UninstalledOn = DateTime.Now;
                    installedOAPPSystemHolon.UninstalledByAvatarUsername = avatarResult.Result.Username;
                    installedOAPPSystemHolon.Active = "0";

                    OASISResult<InstalledOAPPSystemHolon> saveIntalledOAPPSystemHolonResult = await installedOAPPSystemHolon.SaveAsync<InstalledOAPPSystemHolon>();

                    if (saveIntalledOAPPSystemHolonResult != null && !saveIntalledOAPPSystemHolonResult.IsError && saveIntalledOAPPSystemHolonResult.Result != null)
                    {
                        result.Message = "{OAPPSystemHolonUIName} Uninstalled";
                        result.Result = saveIntalledOAPPSystemHolonResult.Result;
                    }
                    else
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling SaveAsync. Reason: {saveIntalledOAPPSystemHolonResult.Message}");
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadAvatarAsync. Reason: {avatarResult.Message}");
            //}

            return result;
        }

        public OASISResult<IInstalledOAPPSystemHolon> UninstallOAPPSystemHolon(IInstalledOAPPSystemHolon installedOAPPSystemHolon, Guid avatarId, string errorMessage, ProviderType providerType)
        {
            OASISResult<IInstalledOAPPSystemHolon> result = new OASISResult<IInstalledOAPPSystemHolon>();

            try
            {
                Directory.Delete(installedOAPPSystemHolon.InstalledPath, true);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured attempting to delete the {OAPPSystemHolonUIName} folder ({installedOAPPSystemHolon.InstalledPath}) Reason: {ex.Message}");
            }

            if (!result.IsError)
            {
                OASISResult<IAvatar> avatarResult = AvatarManager.Instance.LoadAvatar(avatarId, false, true, providerType, 0);

                if (avatarResult != null && avatarResult.Result != null && !avatarResult.IsError)
                {
                    installedOAPPSystemHolon.UninstalledBy = avatarId;
                    installedOAPPSystemHolon.UninstalledOn = DateTime.Now;
                    installedOAPPSystemHolon.UninstalledByAvatarUsername = avatarResult.Result.Username;
                    installedOAPPSystemHolon.Active = "0";

                    OASISResult<InstalledOAPPSystemHolon> saveIntalledOAPPSystemHolonResult = installedOAPPSystemHolon.Save<InstalledOAPPSystemHolon>();

                    if (saveIntalledOAPPSystemHolonResult != null && !saveIntalledOAPPSystemHolonResult.IsError && saveIntalledOAPPSystemHolonResult.Result != null)
                    {
                        result.Message = "{OAPPSystemHolonUIName} Uninstalled";
                        result.Result = saveIntalledOAPPSystemHolonResult.Result;
                    }
                    else
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling Save. Reason: {saveIntalledOAPPSystemHolonResult.Message}");
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadAvatar. Reason: {avatarResult.Message}");
            }

            return result;
        }

        public async Task<OASISResult<IInstalledOAPPSystemHolon>> UninstallOAPPSystemHolonAsync(Guid OAPPSystemHolonId, int versionSequence, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IInstalledOAPPSystemHolon> result = new OASISResult<IInstalledOAPPSystemHolon>();
            string errorMessage = "Error occured in OAPPSystemManagerBase.UninstallOAPPSystemHolonAsync. Reason: ";

            return await UninstallOAPPSystemHolonAsync(await Data.LoadHolonByMetaDataAsync<InstalledOAPPSystemHolon>(new Dictionary<string, string>()
            {
                { "OAPPSystemHolonId", OAPPSystemHolonId.ToString() },
                { "VersionSequene", versionSequence.ToString() }

            }, MetaKeyValuePairMatchMode.All, HolonType.InstalledOAPPSystemHolon, true, true, 0, true, 0, false, HolonType.All, providerType), avatarId, errorMessage, providerType);
        }

        public OASISResult<IInstalledOAPPSystemHolon> UninstallOAPPSystemHolon(Guid OAPPSystemHolonId, int versionSequence, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IInstalledOAPPSystemHolon> result = new OASISResult<IInstalledOAPPSystemHolon>();
            string errorMessage = "Error occured in OAPPSystemManagerBase.UninstallOAPPSystemHolon. Reason: ";

            return UninstallOAPPSystemHolon(Data.LoadHolonByMetaData<InstalledOAPPSystemHolon>(new Dictionary<string, string>()
            {
                { "OAPPSystemHolonId", OAPPSystemHolonId.ToString() },
                { "VersionSequene", versionSequence.ToString() }

            }, MetaKeyValuePairMatchMode.All, HolonType.InstalledOAPPSystemHolon, true, true, 0, true, false, HolonType.All, 0, providerType), avatarId, errorMessage, providerType);
        }

        public async Task<OASISResult<IInstalledOAPPSystemHolon>> UninstallOAPPSystemHolonAsync(Guid OAPPSystemHolonId, string version, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IInstalledOAPPSystemHolon> result = new OASISResult<IInstalledOAPPSystemHolon>();
            string errorMessage = "Error occured in OAPPSystemManagerBase.UninstallOAPPSystemHolonAsync. Reason: ";

            return await UninstallOAPPSystemHolonAsync(await Data.LoadHolonByMetaDataAsync<InstalledOAPPSystemHolon>(new Dictionary<string, string>()
            {
                { "OAPPSystemHolonId", OAPPSystemHolonId.ToString() },
                { "Version", version }

            }, MetaKeyValuePairMatchMode.All, HolonType.InstalledOAPPSystemHolon, true, true, 0, true, 0, false, HolonType.All, providerType), avatarId, errorMessage, providerType);
        }

        public OASISResult<IInstalledOAPPSystemHolon> UninstallOAPPSystemHolon(Guid OAPPSystemHolonId, string version, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IInstalledOAPPSystemHolon> result = new OASISResult<IInstalledOAPPSystemHolon>();
            string errorMessage = "Error occured in OAPPSystemManagerBase.UninstallOAPPSystemHolonAsync. Reason: ";

            return UninstallOAPPSystemHolon(Data.LoadHolonByMetaData<InstalledOAPPSystemHolon>(new Dictionary<string, string>()
            {
                { "OAPPSystemHolonId", OAPPSystemHolonId.ToString() },
                { "Version", version }

            }, MetaKeyValuePairMatchMode.All, HolonType.InstalledOAPPSystemHolon, true, true, 0, true, false, HolonType.All, 0, providerType), avatarId, errorMessage, providerType);
        }

        public async Task<OASISResult<IInstalledOAPPSystemHolon>> UninstallOAPPSystemHolonAsync(string OAPPSystemHolonName, int versionSequence, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IInstalledOAPPSystemHolon> result = new OASISResult<IInstalledOAPPSystemHolon>();
            string errorMessage = "Error occured in OAPPSystemManagerBase.UninstallOAPPSystemHolonAsync. Reason: ";

            return await UninstallOAPPSystemHolonAsync(await Data.LoadHolonByMetaDataAsync<InstalledOAPPSystemHolon>(new Dictionary<string, string>()
            {
                { "OAPPSystemHolonName", OAPPSystemHolonName },
                { "VersionSequence", versionSequence.ToString() }

            }, MetaKeyValuePairMatchMode.All, HolonType.InstalledOAPPSystemHolon, true, true, 0, true, 0, false, HolonType.All, providerType), avatarId, errorMessage, providerType);
        }

        public OASISResult<IInstalledOAPPSystemHolon> UninstallOAPPSystemHolon(string OAPPSystemHolonName, int versionSequence, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IInstalledOAPPSystemHolon> result = new OASISResult<IInstalledOAPPSystemHolon>();
            string errorMessage = "Error occured in OAPPSystemManagerBase.UninstallOAPPSystemHolon. Reason: ";

            return UninstallOAPPSystemHolon(Data.LoadHolonByMetaData<InstalledOAPPSystemHolon>(new Dictionary<string, string>()
            {
                { "OAPPSystemHolonName", OAPPSystemHolonName },
                { "VersionSequene", versionSequence.ToString() }

            }, MetaKeyValuePairMatchMode.All, HolonType.InstalledOAPPSystemHolon, true, true, 0, true, false, HolonType.All), avatarId, errorMessage, providerType);
        }

        public async Task<OASISResult<IInstalledOAPPSystemHolon>> UninstallOAPPSystemHolonAsync(string OAPPSystemHolonName, string version, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IInstalledOAPPSystemHolon> result = new OASISResult<IInstalledOAPPSystemHolon>();
            string errorMessage = "Error occured in OAPPSystemManagerBase.UninstallOAPPSystemHolon. Reason: ";

            return UninstallOAPPSystemHolon(await Data.LoadHolonByMetaDataAsync<InstalledOAPPSystemHolon>(new Dictionary<string, string>()
            {
                { "OAPPSystemHolonName", OAPPSystemHolonName },
                { "Version", version }

            }, MetaKeyValuePairMatchMode.All, HolonType.InstalledOAPPSystemHolon, true, true, 0, true, 0, false, HolonType.All, providerType), avatarId, errorMessage, providerType);
        }

        public OASISResult<IInstalledOAPPSystemHolon> UninstallOAPPSystemHolon(string OAPPSystemHolonName, string version, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IInstalledOAPPSystemHolon> result = new OASISResult<IInstalledOAPPSystemHolon>();
            string errorMessage = "Error occured in OAPPSystemManagerBase.UninstallOAPPSystemHolon. Reason: ";

            return UninstallOAPPSystemHolon(Data.LoadHolonByMetaData<InstalledOAPPSystemHolon>(new Dictionary<string, string>()
            {
                { "OAPPSystemHolonName", OAPPSystemHolonName },
                { "Version", version }

            }, MetaKeyValuePairMatchMode.All, HolonType.InstalledOAPPSystemHolon, true, true, 0, true, false, HolonType.All), avatarId, errorMessage, providerType);
        }

        public async Task<OASISResult<IEnumerable<IInstalledOAPPSystemHolon>>> ListInstalledOAPPSystemHolonsAsync(Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<IInstalledOAPPSystemHolon>> result = new OASISResult<IEnumerable<IInstalledOAPPSystemHolon>>();
            OASISResult<IEnumerable<InstalledOAPPSystemHolon>> installedOAPPSystemHolonsResult = await Data.LoadHolonsForParentAsync<InstalledOAPPSystemHolon>(avatarId, HolonType.InstalledOAPPSystemHolon, false, false, 0, true, false, 0, HolonType.All, 0, providerType);
            string errorMessage = "Error occured in OAPPSystemManagerBase.ListInstalledOAPPSystemHolonsAsync. Reason: ";

            if (installedOAPPSystemHolonsResult != null && !installedOAPPSystemHolonsResult.IsError && installedOAPPSystemHolonsResult.Result != null)
            {
                result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<IEnumerable<InstalledOAPPSystemHolon>, IEnumerable<IInstalledOAPPSystemHolon>>(installedOAPPSystemHolonsResult);
                result.Result = Mapper.Convert<InstalledOAPPSystemHolon, IInstalledOAPPSystemHolon>(installedOAPPSystemHolonsResult.Result.Where(x => x.UninstalledOn == DateTime.MinValue));
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonsForParentAsync. Reason: {installedOAPPSystemHolonsResult.Message}");

            return result;
        }

        public OASISResult<IEnumerable<IInstalledOAPPSystemHolon>> ListInstalledOAPPSystemHolons(Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<IInstalledOAPPSystemHolon>> result = new OASISResult<IEnumerable<IInstalledOAPPSystemHolon>>();
            OASISResult<IEnumerable<InstalledOAPPSystemHolon>> installedOAPPSystemHolonsResult = Data.LoadHolonsForParent<InstalledOAPPSystemHolon>(avatarId, HolonType.InstalledOAPPSystemHolon, false, false, 0, true, false, 0, HolonType.All, 0, providerType);
            string errorMessage = "Error occured in OAPPSystemManagerBase.ListInstalledOAPPSystemHolons. Reason: ";

            if (installedOAPPSystemHolonsResult != null && !installedOAPPSystemHolonsResult.IsError && installedOAPPSystemHolonsResult.Result != null)
            {
                result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<IEnumerable<InstalledOAPPSystemHolon>, IEnumerable<IInstalledOAPPSystemHolon>>(installedOAPPSystemHolonsResult);
                result.Result = Mapper.Convert<InstalledOAPPSystemHolon, IInstalledOAPPSystemHolon>(installedOAPPSystemHolonsResult.Result.Where(x => x.UninstalledOn == DateTime.MinValue));
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonsForParent. Reason: {installedOAPPSystemHolonsResult.Message}");

            return result;
        }

        public async Task<OASISResult<IEnumerable<IInstalledOAPPSystemHolon>>> ListUnInstalledOAPPSystemHolonsAsync(Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<IInstalledOAPPSystemHolon>> result = new OASISResult<IEnumerable<IInstalledOAPPSystemHolon>>();
            OASISResult<IEnumerable<InstalledOAPPSystemHolon>> installedOAPPSystemHolonsResult = await Data.LoadHolonsForParentAsync<InstalledOAPPSystemHolon>(avatarId, HolonType.InstalledOAPPSystemHolon, false, false, 0, true, false, 0, HolonType.All, 0, providerType);
            string errorMessage = "Error occured in OAPPSystemManagerBase.ListUnInstalledOAPPSystemHolonsAsync. Reason: ";

            if (installedOAPPSystemHolonsResult != null && !installedOAPPSystemHolonsResult.IsError && installedOAPPSystemHolonsResult.Result != null)
            {
                result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<IEnumerable<InstalledOAPPSystemHolon>, IEnumerable<IInstalledOAPPSystemHolon>>(installedOAPPSystemHolonsResult);
                result.Result = Mapper.Convert<InstalledOAPPSystemHolon, IInstalledOAPPSystemHolon>(installedOAPPSystemHolonsResult.Result.Where(x => x.UninstalledOn != DateTime.MinValue));
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonsForParentAsync. Reason: {installedOAPPSystemHolonsResult.Message}");

            return result;
        }

        public OASISResult<IEnumerable<IInstalledOAPPSystemHolon>> ListUnInstalledOAPPSystemHolons(Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<IInstalledOAPPSystemHolon>> result = new OASISResult<IEnumerable<IInstalledOAPPSystemHolon>>();
            OASISResult<IEnumerable<InstalledOAPPSystemHolon>> installedOAPPSystemHolonsResult = Data.LoadHolonsForParent<InstalledOAPPSystemHolon>(avatarId, HolonType.InstalledOAPPSystemHolon, false, false, 0, true, false, 0, HolonType.All, 0, providerType);
            string errorMessage = "Error occured in OAPPSystemManagerBase.ListUnInstalledOAPPSystemHolons. Reason: ";

            if (installedOAPPSystemHolonsResult != null && !installedOAPPSystemHolonsResult.IsError && installedOAPPSystemHolonsResult.Result != null)
            {
                result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<IEnumerable<InstalledOAPPSystemHolon>, IEnumerable<IInstalledOAPPSystemHolon>>(installedOAPPSystemHolonsResult);
                result.Result = Mapper.Convert<InstalledOAPPSystemHolon, IInstalledOAPPSystemHolon>(installedOAPPSystemHolonsResult.Result.Where(x => x.UninstalledOn != DateTime.MinValue));
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonsForParent. Reason: {installedOAPPSystemHolonsResult.Message}");

            return result;
        }

        public async Task<OASISResult<IEnumerable<IOAPPSystemHolon>>> ListUnpublishedOAPPSystemHolonsAsync(Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<IOAPPSystemHolon>> result = new OASISResult<IEnumerable<IOAPPSystemHolon>>();
            OASISResult<IEnumerable<T>> unpublishedOAPPSystemHolonsResult = await Data.LoadHolonsForParentAsync<T>(avatarId, HolonType.T, false, false, 0, true, false, 0, HolonType.All, 0, providerType);
            string errorMessage = "Error occured in OAPPSystemManagerBase.ListUnpublishedOAPPSystemHolonsAsync. Reason: ";

            if (unpublishedOAPPSystemHolonsResult != null && !unpublishedOAPPSystemHolonsResult.IsError && unpublishedOAPPSystemHolonsResult.Result != null)
            {
                result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<IEnumerable<T>, IEnumerable<IOAPPSystemHolon>>(unpublishedOAPPSystemHolonsResult);
                result.Result = Mapper.Convert<T, IOAPPSystemHolon>(unpublishedOAPPSystemHolonsResult.Result.Where(x => x.OAPPSystemHolonDNA.PublishedOn == DateTime.MinValue && x.OAPPSystemHolonDNA.OAPPSystemHolonFileSize > 0));
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonsForParentAsync. Reason: {unpublishedOAPPSystemHolonsResult.Message}");

            return result;
        }

        public OASISResult<IEnumerable<IOAPPSystemHolon>> ListUnpublishedOAPPSystemHolons(Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<IOAPPSystemHolon>> result = new OASISResult<IEnumerable<IOAPPSystemHolon>>();
            OASISResult<IEnumerable<T>> unpublishedOAPPSystemHolonsResult = Data.LoadHolonsForParent<T>(avatarId, HolonType.T, false, false, 0, true, false, 0, HolonType.All, 0, providerType);
            string errorMessage = "Error occured in OAPPSystemManagerBase.ListUnpublishedOAPPSystemHolons. Reason: ";

            if (unpublishedOAPPSystemHolonsResult != null && !unpublishedOAPPSystemHolonsResult.IsError && unpublishedOAPPSystemHolonsResult.Result != null)
            {
                result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<IEnumerable<T>, IEnumerable<IOAPPSystemHolon>>(unpublishedOAPPSystemHolonsResult);
                result.Result = Mapper.Convert<T, IOAPPSystemHolon>(unpublishedOAPPSystemHolonsResult.Result.Where(x => x.OAPPSystemHolonDNA.PublishedOn == DateTime.MinValue && x.OAPPSystemHolonDNA.OAPPSystemHolonFileSize > 0));
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonsForParent. Reason: {unpublishedOAPPSystemHolonsResult.Message}");

            return result;
        }

        public async Task<OASISResult<IEnumerable<IOAPPSystemHolon>>> ListDeactivatedOAPPSystemHolonsAsync(Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<IOAPPSystemHolon>> result = new OASISResult<IEnumerable<IOAPPSystemHolon>>();
            //OASISResult<IEnumerable<T>> deactivatedOAPPSystemHolonsResult = await Data.LoadHolonsForParentAsync<T>(avatarId, HolonType.InstalledOAPPSystemHolon, false, false, 0, true, false, 0, HolonType.All, 0, providerType);
            string errorMessage = "Error occured in OAPPSystemManagerBase.ListDeactivatedOAPPSystemHolonsAsync. Reason: ";
            OASISResult<IEnumerable<T>> deactivatedOAPPSystemHolonsResult = await Data.LoadHolonsByMetaDataAsync<T>("Active", "0", HolonType.T, true, true, 0, true, false, 0, HolonType.All, 0, providerType);

            if (deactivatedOAPPSystemHolonsResult != null && !deactivatedOAPPSystemHolonsResult.IsError && deactivatedOAPPSystemHolonsResult.Result != null)
            {
                result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<IEnumerable<T>, IEnumerable<IOAPPSystemHolon>>(deactivatedOAPPSystemHolonsResult);
                result.Result = Mapper.Convert<T, IOAPPSystemHolon>(deactivatedOAPPSystemHolonsResult.Result);
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonsForParentAsync. Reason: {deactivatedOAPPSystemHolonsResult.Message}");

            return result;
        }

        public OASISResult<IEnumerable<IOAPPSystemHolon>> ListDeactivatedOAPPSystemHolons(Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<IOAPPSystemHolon>> result = new OASISResult<IEnumerable<IOAPPSystemHolon>>();
            //OASISResult<IEnumerable<T>> deactivatedOAPPSystemHolonsResult = Data.LoadHolonsForParent<T>(avatarId, HolonType.T, false, false, 0, true, false, 0, HolonType.All, 0, providerType);
            string errorMessage = "Error occured in OAPPSystemManagerBase.ListDeactivatedOAPPSystemHolons. Reason: ";
            OASISResult<IEnumerable<T>> deactivatedOAPPSystemHolonsResult = Data.LoadHolonsByMetaData<T>("Active", "0", HolonType.T, true, true, 0, true, false, 0, HolonType.All, 0, providerType);

            if (deactivatedOAPPSystemHolonsResult != null && !deactivatedOAPPSystemHolonsResult.IsError && deactivatedOAPPSystemHolonsResult.Result != null)
            {
                result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<IEnumerable<T>, IEnumerable<IOAPPSystemHolon>>(deactivatedOAPPSystemHolonsResult);
                //result.Result = Mapper.Convert<T, IOAPPSystemHolon>(deactivatedOAPPSystemHolonsResult.Result.Where(x => x.OAPPSystemHolonDNA.IsActive != true));
                result.Result = Mapper.Convert<T, IOAPPSystemHolon>(deactivatedOAPPSystemHolonsResult.Result);
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonsForParent. Reason: {deactivatedOAPPSystemHolonsResult.Message}");

            return result;
        }

        public async Task<OASISResult<bool>> IsOAPPSystemHolonInstalledAsync(Guid avatarId, Guid OAPPSystemHolonId, int versionSequence, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<bool> result = new OASISResult<bool>();
            string errorMessage = "Error occured in OAPPSystemManagerBase.IsOAPPSystemHolonInstalledAsync. Reason: ";

            OASISResult<InstalledOAPPSystemHolon> installedOAPPSystemHolonsResult = await Data.LoadHolonByMetaDataAsync<InstalledOAPPSystemHolon>(new Dictionary<string, string>()
            {
                { "OAPPSystemHolonId", OAPPSystemHolonId.ToString() },
                { "VersionSequence", versionSequence.ToString() },
                { "Active", "1" }

            }, MetaKeyValuePairMatchMode.All, HolonType.InstalledOAPPSystemHolon, true, true, 0, true, 0, false, HolonType.All, providerType);

            if (installedOAPPSystemHolonsResult != null && !installedOAPPSystemHolonsResult.IsError)
            {
                if (installedOAPPSystemHolonsResult.Result != null)
                    result.Result = true;
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonByMetaDataAsync. Reason: {installedOAPPSystemHolonsResult.Message}");

            return result;
        }

        public OASISResult<bool> IsOAPPSystemHolonInstalled(Guid avatarId, Guid OAPPSystemHolonId, int versionSequence, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<bool> result = new OASISResult<bool>();
            string errorMessage = "Error occured in OAPPSystemManagerBase.IsOAPPSystemHolonInstalledAsync. Reason: ";

            OASISResult<InstalledOAPPSystemHolon> installedOAPPSystemHolonsResult = Data.LoadHolonByMetaData<InstalledOAPPSystemHolon>(new Dictionary<string, string>()
            {
                { "OAPPSystemHolonId", OAPPSystemHolonId.ToString() },
                { "VersionSequence", versionSequence.ToString() },
                { "Active", "1" }

            }, MetaKeyValuePairMatchMode.All, HolonType.InstalledOAPPSystemHolon, true, true, 0, true, false, HolonType.All, 0, providerType);

            if (installedOAPPSystemHolonsResult != null && !installedOAPPSystemHolonsResult.IsError && installedOAPPSystemHolonsResult.Result != null)
                result.Result = true;
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonByMetaDataAsync. Reason: {installedOAPPSystemHolonsResult.Message}");

            return result;
        }

        public async Task<OASISResult<bool>> IsOAPPSystemHolonInstalledAsync(Guid avatarId, Guid OAPPSystemHolonId, string version, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<bool> result = new OASISResult<bool>();
            string errorMessage = "Error occured in OAPPSystemManagerBase.IsOAPPSystemHolonInstalledAsync. Reason: ";

            OASISResult<InstalledOAPPSystemHolon> installedOAPPSystemHolonsResult = await Data.LoadHolonByMetaDataAsync<InstalledOAPPSystemHolon>(new Dictionary<string, string>()
            {
                { "OAPPSystemHolonId", OAPPSystemHolonId.ToString() },
                { "Version", version },
                { "Active", "1" }

            }, MetaKeyValuePairMatchMode.All, HolonType.InstalledOAPPSystemHolon, true, true, 0, true, 0, false, HolonType.InstalledOAPPSystemHolon, providerType);

            if (installedOAPPSystemHolonsResult != null && !installedOAPPSystemHolonsResult.IsError)
            {
                if (installedOAPPSystemHolonsResult.Result != null)
                    result.Result = true;
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonByMetaDataAsync. Reason: {installedOAPPSystemHolonsResult.Message}");

            return result;
        }

        public OASISResult<bool> IsOAPPSystemHolonInstalled(Guid avatarId, Guid OAPPSystemHolonId, string version, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<bool> result = new OASISResult<bool>();
            string errorMessage = "Error occured in OAPPSystemManagerBase.IsOAPPSystemHolonInstalledAsync. Reason: ";

            OASISResult<InstalledOAPPSystemHolon> installedOAPPSystemHolonsResult = Data.LoadHolonByMetaData<InstalledOAPPSystemHolon>(new Dictionary<string, string>()
            {
                { "OAPPSystemHolonId", OAPPSystemHolonId.ToString() },
                { "Version", version },
                { "Active", "1" }

            }, MetaKeyValuePairMatchMode.All, HolonType.InstalledOAPPSystemHolon, true, true, 0, true, false, HolonType.All, 0, providerType);

            if (installedOAPPSystemHolonsResult != null && !installedOAPPSystemHolonsResult.IsError)
            {
                if (installedOAPPSystemHolonsResult.Result != null)
                    result.Result = true;
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonByMetaData. Reason: {installedOAPPSystemHolonsResult.Message}");

            return result;
        }

        public async Task<OASISResult<bool>> IsOAPPSystemHolonInstalledAsync(Guid avatarId, string OAPPSystemHolonName, int versionSequence, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<bool> result = new OASISResult<bool>();
            string errorMessage = "Error occured in OAPPSystemManagerBase.IsOAPPSystemHolonInstalledAsync. Reason: ";

            OASISResult<InstalledOAPPSystemHolon> installedOAPPSystemHolonsResult = await Data.LoadHolonByMetaDataAsync<InstalledOAPPSystemHolon>(new Dictionary<string, string>()
            {
                { "OAPPSystemHolonName", OAPPSystemHolonName},
                { "VersionSequence", versionSequence.ToString() },
                { "Active", "1" }

            }, MetaKeyValuePairMatchMode.All, HolonType.InstalledOAPPSystemHolon, true, true, 0, true, 0, false, HolonType.All, providerType);

            if (installedOAPPSystemHolonsResult != null && !installedOAPPSystemHolonsResult.IsError)
            {
                if (installedOAPPSystemHolonsResult.Result != null)
                    result.Result = true;
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonByMetaDataAsync. Reason: {installedOAPPSystemHolonsResult.Message}");

            return result;
        }

        public OASISResult<bool> IsOAPPSystemHolonInstalled(Guid avatarId, string OAPPSystemHolonName, int versionSequence, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<bool> result = new OASISResult<bool>();
            string errorMessage = "Error occured in OAPPSystemManagerBase.IsOAPPSystemHolonInstalled. Reason: ";

            OASISResult<InstalledOAPPSystemHolon> installedOAPPSystemHolonsResult = Data.LoadHolonByMetaData<InstalledOAPPSystemHolon>(new Dictionary<string, string>()
            {
                { "OAPPSystemHolonId", OAPPSystemHolonName.ToString() },
                { "VersionSequence", versionSequence.ToString() },
                { "Active", "1" }

            }, MetaKeyValuePairMatchMode.All, HolonType.InstalledOAPPSystemHolon, true, true, 0, true, false, HolonType.All, 0, providerType);

            if (installedOAPPSystemHolonsResult != null && !installedOAPPSystemHolonsResult.IsError)
            {
                if (installedOAPPSystemHolonsResult.Result != null)
                    result.Result = true;
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonByMetaData. Reason: {installedOAPPSystemHolonsResult.Message}");

            return result;
        }

        public async Task<OASISResult<bool>> IsOAPPSystemHolonInstalledAsync(Guid avatarId, string OAPPSystemHolonName, string version, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<bool> result = new OASISResult<bool>();
            string errorMessage = "Error occured in OAPPSystemManagerBase.IsOAPPSystemHolonInstalledAsync. Reason: ";

            OASISResult<InstalledOAPPSystemHolon> installedOAPPSystemHolonsResult = await Data.LoadHolonByMetaDataAsync<InstalledOAPPSystemHolon>(new Dictionary<string, string>()
            {
                { "OAPPSystemHolonName", OAPPSystemHolonName},
                { "Version", version.ToString() },
                { "Active", "1" }

            }, MetaKeyValuePairMatchMode.All, HolonType.InstalledOAPPSystemHolon, true, true, 0, true, 0, false, HolonType.All, providerType);

            if (installedOAPPSystemHolonsResult != null && !installedOAPPSystemHolonsResult.IsError)
            {
                if (installedOAPPSystemHolonsResult.Result != null)
                    result.Result = true;
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonByMetaDataAsync. Reason: {installedOAPPSystemHolonsResult.Message}");

            return result;
        }

        public OASISResult<bool> IsOAPPSystemHolonInstalled(Guid avatarId, string OAPPSystemHolonName, string version, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<bool> result = new OASISResult<bool>();
            string errorMessage = "Error occured in OAPPSystemManagerBase.IsOAPPSystemHolonInstalled. Reason: ";

            OASISResult<InstalledOAPPSystemHolon> installedOAPPSystemHolonsResult = Data.LoadHolonByMetaData<InstalledOAPPSystemHolon>(new Dictionary<string, string>()
            {
                { "OAPPSystemHolonId", OAPPSystemHolonName.ToString() },
                { "Version", version },
                { "Active", "1" }

            }, MetaKeyValuePairMatchMode.All, HolonType.InstalledOAPPSystemHolon, true, true, 0, true, false, HolonType.All, 0, providerType);

            if (installedOAPPSystemHolonsResult != null && !installedOAPPSystemHolonsResult.IsError)
            {
                if (installedOAPPSystemHolonsResult.Result != null)
                    result.Result = true;
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonByMetaData. Reason: {installedOAPPSystemHolonsResult.Message}");

            return result;
        }

        public async Task<OASISResult<IInstalledOAPPSystemHolon>> LoadInstalledOAPPSystemHolonAsync(Guid avatarId, Guid OAPPSystemHolonId, int versionSequence = 0, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IInstalledOAPPSystemHolon> result = new OASISResult<IInstalledOAPPSystemHolon>();
            string errorMessage = "Error occured in OAPPSystemManagerBase.LoadInstalledOAPPSystemHolonAsync. Reason: ";
            OASISResult<InstalledOAPPSystemHolon> installedOAPPSystemHolonsResult = await Data.LoadHolonByMetaDataAsync<InstalledOAPPSystemHolon>(new Dictionary<string, string>()
            {
                { "OAPPSystemHolonId", OAPPSystemHolonId.ToString() },
                { "VersionSequence", versionSequence.ToString() }

            }, MetaKeyValuePairMatchMode.All, HolonType.InstalledOAPPSystemHolon, true, true, 0, true, 0, false, HolonType.All, providerType);

            if (installedOAPPSystemHolonsResult != null && !installedOAPPSystemHolonsResult.IsError && installedOAPPSystemHolonsResult.Result != null)
                result.Result = installedOAPPSystemHolonsResult.Result;
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonByMetaDataAsync. Reason: {installedOAPPSystemHolonsResult.Message}");

            return result;
        }

        public OASISResult<IInstalledOAPPSystemHolon> LoadInstalledOAPPSystemHolon(Guid avatarId, Guid OAPPSystemHolonId, int versionSequence = 0, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IInstalledOAPPSystemHolon> result = new OASISResult<IInstalledOAPPSystemHolon>();
            string errorMessage = "Error occured in OAPPSystemManagerBase.LoadInstalledOAPPSystemHolonAsync. Reason: ";
            OASISResult<InstalledOAPPSystemHolon> installedOAPPSystemHolonsResult = Data.LoadHolonByMetaData<InstalledOAPPSystemHolon>(new Dictionary<string, string>()
            {
                { "OAPPSystemHolonId", OAPPSystemHolonId.ToString() },
                { "VersionSequence", versionSequence.ToString() }

            }, MetaKeyValuePairMatchMode.All, HolonType.T, version: versionSequence, providerType: providerType);

            if (installedOAPPSystemHolonsResult != null && !installedOAPPSystemHolonsResult.IsError && installedOAPPSystemHolonsResult.Result != null)
                result.Result = installedOAPPSystemHolonsResult.Result;
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonByMetaDataAsync. Reason: {installedOAPPSystemHolonsResult.Message}");

            return result;
        }

        public async Task<OASISResult<IInstalledOAPPSystemHolon>> LoadInstalledOAPPSystemHolonAsync(Guid avatarId, string OAPPSystemHolonName, int versionSequence = 0, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IInstalledOAPPSystemHolon> result = new OASISResult<IInstalledOAPPSystemHolon>();
            string errorMessage = "Error occured in OAPPSystemManagerBase.LoadInstalledOAPPSystemHolonAsync. Reason: ";
            OASISResult<InstalledOAPPSystemHolon> installedOAPPSystemHolonsResult = await Data.LoadHolonByMetaDataAsync<InstalledOAPPSystemHolon>(new Dictionary<string, string>()
            {
                { "OAPPSystemHolonName", OAPPSystemHolonName },
                { "VersionSequence", versionSequence.ToString() }

            }, MetaKeyValuePairMatchMode.All, HolonType.InstalledOAPPSystemHolon, true, true, 0, true, 0, false, HolonType.All, providerType);

            if (installedOAPPSystemHolonsResult != null && !installedOAPPSystemHolonsResult.IsError && installedOAPPSystemHolonsResult.Result != null)
                result.Result = installedOAPPSystemHolonsResult.Result;
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonByMetaDataAsync. Reason: {installedOAPPSystemHolonsResult.Message}");

            return result;
        }

        public OASISResult<IInstalledOAPPSystemHolon> LoadInstalledOAPPSystemHolon(Guid avatarId, string OAPPSystemHolonName, int versionSequence = 0, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IInstalledOAPPSystemHolon> result = new OASISResult<IInstalledOAPPSystemHolon>();
            string errorMessage = "Error occured in OAPPSystemManagerBase.LoadInstalledOAPPSystemHolonAsync. Reason: ";
            OASISResult<InstalledOAPPSystemHolon> installedOAPPSystemHolonsResult = Data.LoadHolonByMetaData<InstalledOAPPSystemHolon>(new Dictionary<string, string>()
            {
                { "OAPPSystemHolonName", OAPPSystemHolonName },
                { "VersionSequence", versionSequence.ToString() }

            }, MetaKeyValuePairMatchMode.All, HolonType.T, version: versionSequence, providerType: providerType);

            if (installedOAPPSystemHolonsResult != null && !installedOAPPSystemHolonsResult.IsError && installedOAPPSystemHolonsResult.Result != null)
                result.Result = installedOAPPSystemHolonsResult.Result;
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonByMetaDataAsync. Reason: {installedOAPPSystemHolonsResult.Message}");

            return result;
        }

        public async Task<OASISResult<IInstalledOAPPSystemHolon>> LoadInstalledOAPPSystemHolonAsync(Guid avatarId, Guid OAPPSystemHolonId, string version, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IInstalledOAPPSystemHolon> result = new OASISResult<IInstalledOAPPSystemHolon>();
            string errorMessage = "Error occured in OAPPSystemManagerBase.LoadInstalledOAPPSystemHolonAsync. Reason: ";
            OASISResult<InstalledOAPPSystemHolon> installedOAPPSystemHolonsResult = await Data.LoadHolonByMetaDataAsync<InstalledOAPPSystemHolon>(new Dictionary<string, string>()
            {
                { "OAPPSystemHolonId", OAPPSystemHolonId.ToString() },
                { "Version", version }

            }, MetaKeyValuePairMatchMode.All, HolonType.InstalledOAPPSystemHolon, true, true, 0, true, 0, false, HolonType.All, providerType);

            if (installedOAPPSystemHolonsResult != null && !installedOAPPSystemHolonsResult.IsError && installedOAPPSystemHolonsResult.Result != null)
                result.Result = installedOAPPSystemHolonsResult.Result;
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonByMetaDataAsync. Reason: {installedOAPPSystemHolonsResult.Message}");

            return result;
        }

        public OASISResult<IInstalledOAPPSystemHolon> LoadInstalledOAPPSystemHolon(Guid avatarId, Guid OAPPSystemHolonId, string version, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IInstalledOAPPSystemHolon> result = new OASISResult<IInstalledOAPPSystemHolon>();
            string errorMessage = "Error occured in OAPPSystemManagerBase.LoadInstalledOAPPSystemHolonAsync. Reason: ";
            OASISResult<InstalledOAPPSystemHolon> installedOAPPSystemHolonsResult = Data.LoadHolonByMetaData<InstalledOAPPSystemHolon>(new Dictionary<string, string>()
            {
                { "OAPPSystemHolonId", OAPPSystemHolonId.ToString() },
                { "Version", version }

            }, MetaKeyValuePairMatchMode.All, HolonType.T, providerType: providerType);

            if (installedOAPPSystemHolonsResult != null && !installedOAPPSystemHolonsResult.IsError && installedOAPPSystemHolonsResult.Result != null)
                result.Result = installedOAPPSystemHolonsResult.Result;
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonByMetaDataAsync. Reason: {installedOAPPSystemHolonsResult.Message}");

            return result;
        }

        public async Task<OASISResult<IInstalledOAPPSystemHolon>> LoadInstalledOAPPSystemHolonAsync(Guid avatarId, string OAPPSystemHolonName, string version, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IInstalledOAPPSystemHolon> result = new OASISResult<IInstalledOAPPSystemHolon>();
            string errorMessage = "Error occured in OAPPSystemManagerBase.LoadInstalledOAPPSystemHolonAsync. Reason: ";
            OASISResult<InstalledOAPPSystemHolon> installedOAPPSystemHolonsResult = await Data.LoadHolonByMetaDataAsync<InstalledOAPPSystemHolon>(new Dictionary<string, string>()
            {
                { "OAPPSystemHolonName", OAPPSystemHolonName },
                { "Version", version }

            }, MetaKeyValuePairMatchMode.All, HolonType.InstalledOAPPSystemHolon, true, true, 0, true, 0, false, HolonType.All, providerType);

            if (installedOAPPSystemHolonsResult != null && !installedOAPPSystemHolonsResult.IsError && installedOAPPSystemHolonsResult.Result != null)
                result.Result = installedOAPPSystemHolonsResult.Result;
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonByMetaDataAsync. Reason: {installedOAPPSystemHolonsResult.Message}");

            return result;
        }

        public OASISResult<IInstalledOAPPSystemHolon> LoadInstalledOAPPSystemHolon(Guid avatarId, string OAPPSystemHolonName, string version, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IInstalledOAPPSystemHolon> result = new OASISResult<IInstalledOAPPSystemHolon>();
            string errorMessage = "Error occured in OAPPSystemManagerBase.LoadInstalledOAPPSystemHolonAsync. Reason: ";
            OASISResult<InstalledOAPPSystemHolon> installedOAPPSystemHolonsResult = Data.LoadHolonByMetaData<InstalledOAPPSystemHolon>(new Dictionary<string, string>()
            {
                { "OAPPSystemHolonName", OAPPSystemHolonName },
                { "Version", version }

            }, MetaKeyValuePairMatchMode.All, HolonType.T, providerType: providerType);

            if (installedOAPPSystemHolonsResult != null && !installedOAPPSystemHolonsResult.IsError && installedOAPPSystemHolonsResult.Result != null)
                result.Result = installedOAPPSystemHolonsResult.Result;
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonByMetaDataAsync. Reason: {installedOAPPSystemHolonsResult.Message}");

            return result;
        }

        public async Task<OASISResult<IInstalledOAPPSystemHolon>> LoadInstalledOAPPSystemHolonAsync(Guid avatarId, Guid OAPPSystemHolonId, bool active, int versionSequence = 0, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IInstalledOAPPSystemHolon> result = new OASISResult<IInstalledOAPPSystemHolon>();
            string errorMessage = "Error occured in OAPPSystemManagerBase.LoadInstalledOAPPSystemHolonAsync. Reason: ";
            OASISResult<InstalledOAPPSystemHolon> installedOAPPSystemHolonsResult = await Data.LoadHolonByMetaDataAsync<InstalledOAPPSystemHolon>(new Dictionary<string, string>()
            {
                { "OAPPSystemHolonId", OAPPSystemHolonId.ToString() },
                { "VersionSequence", versionSequence.ToString() },
                { "Active", active == true ? "1" : "0" }

            }, MetaKeyValuePairMatchMode.All, HolonType.InstalledOAPPSystemHolon, true, true, 0, true, 0, false, HolonType.All, providerType);

            if (installedOAPPSystemHolonsResult != null && !installedOAPPSystemHolonsResult.IsError && installedOAPPSystemHolonsResult.Result != null)
                result.Result = installedOAPPSystemHolonsResult.Result;
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonByMetaDataAsync. Reason: {installedOAPPSystemHolonsResult.Message}");

            return result;
        }

        public OASISResult<IInstalledOAPPSystemHolon> LoadInstalledOAPPSystemHolon(Guid avatarId, Guid OAPPSystemHolonId, bool active, int versionSequence = 0, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IInstalledOAPPSystemHolon> result = new OASISResult<IInstalledOAPPSystemHolon>();
            string errorMessage = "Error occured in OAPPSystemManagerBase.LoadInstalledOAPPSystemHolon. Reason: ";
            OASISResult<InstalledOAPPSystemHolon> installedOAPPSystemHolonsResult = Data.LoadHolonByMetaData<InstalledOAPPSystemHolon>(new Dictionary<string, string>()
            {
                { "OAPPSystemHolonId", OAPPSystemHolonId.ToString() },
                { "VersionSequence", versionSequence.ToString() },
                { "Active", active == true ? "1" : "0" }

            }, MetaKeyValuePairMatchMode.All, HolonType.InstalledOAPPSystemHolon, true, true, 0, true, false, HolonType.All, 0, providerType);

            if (installedOAPPSystemHolonsResult != null && !installedOAPPSystemHolonsResult.IsError && installedOAPPSystemHolonsResult.Result != null)
                result.Result = installedOAPPSystemHolonsResult.Result;
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonByMetaData. Reason: {installedOAPPSystemHolonsResult.Message}");

            return result;
        }

        public async Task<OASISResult<IInstalledOAPPSystemHolon>> LoadInstalledOAPPSystemHolonAsync(Guid avatarId, string OAPPSystemHolonName, bool active, int versionSequence = 0, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IInstalledOAPPSystemHolon> result = new OASISResult<IInstalledOAPPSystemHolon>();
            string errorMessage = "Error occured in OAPPSystemManagerBase.LoadInstalledOAPPSystemHolonAsync. Reason: ";
            OASISResult<InstalledOAPPSystemHolon> installedOAPPSystemHolonsResult = await Data.LoadHolonByMetaDataAsync<InstalledOAPPSystemHolon>(new Dictionary<string, string>()
            {
                { "OAPPSystemHolonName", OAPPSystemHolonName },
                { "VersionSequence", versionSequence.ToString() },
                { "Active", active == true ? "1" : "0" }

            }, MetaKeyValuePairMatchMode.All, HolonType.InstalledOAPPSystemHolon, true, true, 0, true, 0, false, HolonType.All, providerType);
            if (installedOAPPSystemHolonsResult != null && !installedOAPPSystemHolonsResult.IsError && installedOAPPSystemHolonsResult.Result != null)
                result.Result = installedOAPPSystemHolonsResult.Result;
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonByMetaDataAsync. Reason: {installedOAPPSystemHolonsResult.Message}");

            return result;
        }

        public OASISResult<IInstalledOAPPSystemHolon> LoadInstalledOAPPSystemHolon(Guid avatarId, string OAPPSystemHolonName, bool active, int versionSequence = 0, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IInstalledOAPPSystemHolon> result = new OASISResult<IInstalledOAPPSystemHolon>();
            string errorMessage = "Error occured in OAPPSystemManagerBase.LoadInstalledOAPPSystemHolon. Reason: ";
            OASISResult<InstalledOAPPSystemHolon> installedOAPPSystemHolonsResult = Data.LoadHolonByMetaData<InstalledOAPPSystemHolon>(new Dictionary<string, string>()
            {
                { "OAPPSystemHolonId", OAPPSystemHolonName },
                { "VersionSequence", versionSequence.ToString() },
                { "Active", active == true ? "1" : "0" }

            }, MetaKeyValuePairMatchMode.All, HolonType.InstalledOAPPSystemHolon, true, true, 0, true, false, HolonType.All, 0, providerType);

            if (installedOAPPSystemHolonsResult != null && !installedOAPPSystemHolonsResult.IsError && installedOAPPSystemHolonsResult.Result != null)
                result.Result = installedOAPPSystemHolonsResult.Result;
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonByMetaData. Reason: {installedOAPPSystemHolonsResult.Message}");

            return result;
        }

        public async Task<OASISResult<IInstalledOAPPSystemHolon>> LoadInstalledOAPPSystemHolonAsync(Guid avatarId, Guid OAPPSystemHolonId, string version, bool active, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IInstalledOAPPSystemHolon> result = new OASISResult<IInstalledOAPPSystemHolon>();
            string errorMessage = "Error occured in OAPPSystemManagerBase.LoadInstalledOAPPSystemHolonAsync. Reason: ";
            OASISResult<InstalledOAPPSystemHolon> installedOAPPSystemHolonsResult = await Data.LoadHolonByMetaDataAsync<InstalledOAPPSystemHolon>(new Dictionary<string, string>()
            {
                { "OAPPSystemHolonId", OAPPSystemHolonId.ToString() },
                { "Version", version},
                { "Active", active == true ? "1" : "0" }

            }, MetaKeyValuePairMatchMode.All, HolonType.InstalledOAPPSystemHolon, true, true, 0, true, 0, false, HolonType.All, providerType);

            if (installedOAPPSystemHolonsResult != null && !installedOAPPSystemHolonsResult.IsError && installedOAPPSystemHolonsResult.Result != null)
                result.Result = installedOAPPSystemHolonsResult.Result;
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonByMetaDataAsync. Reason: {installedOAPPSystemHolonsResult.Message}");

            return result;
        }

        public OASISResult<IInstalledOAPPSystemHolon> LoadInstalledOAPPSystemHolon(Guid avatarId, Guid OAPPSystemHolonId, string version, bool active, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IInstalledOAPPSystemHolon> result = new OASISResult<IInstalledOAPPSystemHolon>();
            string errorMessage = "Error occured in OAPPSystemManagerBase.LoadInstalledOAPPSystemHolon. Reason: ";
            OASISResult<InstalledOAPPSystemHolon> installedOAPPSystemHolonsResult = Data.LoadHolonByMetaData<InstalledOAPPSystemHolon>(new Dictionary<string, string>()
            {
                { "OAPPSystemHolonId", OAPPSystemHolonId.ToString() },
                { "Version", version },
                 { "Active", active == true ? "1" : "0" }

            }, MetaKeyValuePairMatchMode.All, HolonType.InstalledOAPPSystemHolon, true, true, 0, true, false, HolonType.All, 0, providerType);

            if (installedOAPPSystemHolonsResult != null && !installedOAPPSystemHolonsResult.IsError && installedOAPPSystemHolonsResult.Result != null)
                result.Result = installedOAPPSystemHolonsResult.Result;
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonByMetaData. Reason: {installedOAPPSystemHolonsResult.Message}");

            return result;
        }

        public async Task<OASISResult<IInstalledOAPPSystemHolon>> LoadInstalledOAPPSystemHolonAsync(Guid avatarId, string OAPPSystemHolonName, string version, bool active, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IInstalledOAPPSystemHolon> result = new OASISResult<IInstalledOAPPSystemHolon>();
            string errorMessage = "Error occured in OAPPSystemManagerBase.LoadInstalledOAPPSystemHolonAsync. Reason: ";
            OASISResult<InstalledOAPPSystemHolon> installedOAPPSystemHolonsResult = await Data.LoadHolonByMetaDataAsync<InstalledOAPPSystemHolon>(new Dictionary<string, string>()
            {
                { "OAPPSystemHolonName", OAPPSystemHolonName },
                { "Version", version },
                { "Active", active == true ? "1" : "0" }

            }, MetaKeyValuePairMatchMode.All, HolonType.InstalledOAPPSystemHolon, true, true, 0, true, 0, false, HolonType.All, providerType);
            
            if (installedOAPPSystemHolonsResult != null && !installedOAPPSystemHolonsResult.IsError && installedOAPPSystemHolonsResult.Result != null)
                result.Result = installedOAPPSystemHolonsResult.Result;
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonByMetaDataAsync. Reason: {installedOAPPSystemHolonsResult.Message}");

            return result;
        }

        public OASISResult<IInstalledOAPPSystemHolon> LoadInstalledOAPPSystemHolon(Guid avatarId, string OAPPSystemHolonName, string version, bool active, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IInstalledOAPPSystemHolon> result = new OASISResult<IInstalledOAPPSystemHolon>();
            string errorMessage = "Error occured in OAPPSystemManagerBase.LoadInstalledOAPPSystemHolon. Reason: ";
            OASISResult<InstalledOAPPSystemHolon> installedOAPPSystemHolonsResult = Data.LoadHolonByMetaData<InstalledOAPPSystemHolon>(new Dictionary<string, string>()
            {
                { "OAPPSystemHolonName", OAPPSystemHolonName },
                { "Version", version },
                { "Active", active == true ? "1" : "0" }

            }, MetaKeyValuePairMatchMode.All, HolonType.InstalledOAPPSystemHolon, true, true, 0, true, false, HolonType.All, 0, providerType);

            if (installedOAPPSystemHolonsResult != null && !installedOAPPSystemHolonsResult.IsError && installedOAPPSystemHolonsResult.Result != null)
                result.Result = installedOAPPSystemHolonsResult.Result;
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonByMetaData. Reason: {installedOAPPSystemHolonsResult.Message}");

            return result;
        }

        public OASISResult<IInstalledOAPPSystemHolon> OpenOAPPSystemHolonFolder(Guid avatarId, IInstalledOAPPSystemHolon T)
        {
            OASISResult<IInstalledOAPPSystemHolon> result = new OASISResult<IInstalledOAPPSystemHolon>();
            string errorMessage = "An error occured in OAPPSystemManagerBase.OpenOAPPSystemHolonFolder. Reason:";

            if (T != null)
            {
                try
                {
                    if (!string.IsNullOrEmpty(T.InstalledPath))
                        Process.Start("explorer.exe", T.InstalledPath);

                    //else if (T.DownloadedOAPPSystemHolon != null && !string.IsNullOrEmpty(T.DownloadedOAPPSystemHolon.DownloadedPath))
                    else if (!string.IsNullOrEmpty(T.DownloadedPath))
                        Process.Start("explorer.exe", new FileInfo(T.DownloadedPath).DirectoryName);
                }
                catch (Exception e)
                {
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured attempting to open the folder {result.Result.InstalledPath}. Reason: {e}");
                }
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} The {OAPPSystemHolonUIName} is null!");

            return result;
        }

        public async Task<OASISResult<IInstalledOAPPSystemHolon>> OpenOAPPSystemHolonFolderAsync(Guid avatarId, Guid OAPPSystemHolonId, int versionSequence, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IInstalledOAPPSystemHolon> result = new OASISResult<IInstalledOAPPSystemHolon>();
            string errorMessage = "An error occured in OAPPSystemManagerBase.OpenOAPPSystemHolonFolderAsync. Reason:";
            result = await LoadInstalledOAPPSystemHolonAsync(avatarId, OAPPSystemHolonId, versionSequence);

            if (result != null && !result.IsError && result.Result != null)
                OpenOAPPSystemHolonFolder(avatarId, result.Result);
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the {OAPPSystemHolonUIName} with the LoadInstalledOAPPSystemHolonAsync method, reason: {result.Message}");

            return result;
        }

        public OASISResult<IInstalledOAPPSystemHolon> OpenOAPPSystemHolonFolder(Guid avatarId, Guid OAPPSystemHolonId, int versionSequence, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IInstalledOAPPSystemHolon> result = new OASISResult<IInstalledOAPPSystemHolon>();
            string errorMessage = "An error occured in OAPPSystemManagerBase.OpenOAPPSystemHolonFolder. Reason:";
            result = LoadInstalledOAPPSystemHolon(avatarId, OAPPSystemHolonId, versionSequence);

            if (result != null && !result.IsError && result.Result != null)
                OpenOAPPSystemHolonFolder(avatarId, result.Result);
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the {OAPPSystemHolonUIName} with the LoadInstalledOAPPSystemHolon method, reason: {result.Message}");

            return result;
        }

        public async Task<OASISResult<IInstalledOAPPSystemHolon>> OpenOAPPSystemHolonFolderAsync(Guid avatarId, Guid OAPPSystemHolonId, string version, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IInstalledOAPPSystemHolon> result = new OASISResult<IInstalledOAPPSystemHolon>();
            string errorMessage = "An error occured in OAPPSystemManagerBase.OpenOAPPSystemHolonFolderAsync. Reason:";
            result = await LoadInstalledOAPPSystemHolonAsync(avatarId, OAPPSystemHolonId, version);

            if (result != null && !result.IsError && result.Result != null)
                OpenOAPPSystemHolonFolder(avatarId, result.Result);
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the {OAPPSystemHolonUIName} with the LoadInstalledOAPPSystemHolonAsync method, reason: {result.Message}");

            return result;
        }

        public OASISResult<IInstalledOAPPSystemHolon> OpenOAPPSystemHolonFolder(Guid avatarId, Guid OAPPSystemHolonId, string version, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IInstalledOAPPSystemHolon> result = new OASISResult<IInstalledOAPPSystemHolon>();
            string errorMessage = "An error occured in OAPPSystemManagerBase.OpenOAPPSystemHolonFolder. Reason:";
            result = LoadInstalledOAPPSystemHolon(avatarId, OAPPSystemHolonId, version);

            if (result != null && !result.IsError && result.Result != null)
                OpenOAPPSystemHolonFolder(avatarId, result.Result);
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the {OAPPSystemHolonUIName} with the LoadInstalledOAPPSystemHolon method, reason: {result.Message}");

            return result;
        }


        //private IOAPPSystemHolonDNA ConvertOAPPSystemHolonToOAPPSystemHolonDNA(IOAPPSystemHolon T)
        //{
        //    OAPPSystemHolonDNA OAPPSystemHolonDNA = new OAPPSystemHolonDNA()
        //    {
        //        CelestialBodyId = T.CelestialBodyId,
        //        //CelestialBody = T.CelestialBody,
        //        CelestialBodyName = T.CelestialBody != null ? T.CelestialBody.Name : "",
        //        CelestialBodyType = T.CelestialBody != null ? T.CelestialBody.HolonType : HolonType.None,
        //        CreatedByAvatarId = T.CreatedByAvatarId,
        //        CreatedByAvatarUsername = T.CreatedByAvatarUsername,
        //        CreatedOn = T.CreatedDate,
        //        Description = T.Description,
        //        GenesisType = T.GenesisType,
        //        OAPPSystemHolonId = T.Id,
        //        OAPPSystemHolonName = T.Name,
        //        OAPPSystemHolonType = T.OAPPSystemHolonType,
        //        PublishedByAvatarId = T.PublishedByAvatarId,
        //        PublishedByAvatarUsername = T.PublishedByAvatarUsername,
        //        PublishedOn = T.PublishedOn,
        //        PublishedOnSTARNET = T.PublishedOAPPSystemHolon != null,
        //        Version = T.Version.ToString()
        //    };

        //    List<IZome> zomes = new List<IZome>();
        //    foreach (IHolon holon in T.Children)
        //        zomes.Add((IZome)holon);

        //   //OAPPSystemHolonDNA.Zomes = zomes;
        //    return OAPPSystemHolonDNA;
        //}

        public async Task<OASISResult<bool>> WriteOAPPSystemHolonDNAAsync(IOAPPSystemHolonDNA OAPPSystemHolonDNA, string fullPathToOAPPSystemHolon)
        {
            OASISResult<bool> result = new OASISResult<bool>();

            try
            {
                JsonSerializerOptions options = new()
                {
                    ReferenceHandler = ReferenceHandler.Preserve,
                    WriteIndented = true
                };

                if (!Directory.Exists(fullPathToOAPPSystemHolon))
                    Directory.CreateDirectory(fullPathToOAPPSystemHolon);

                await File.WriteAllTextAsync(Path.Combine(fullPathToOAPPSystemHolon, "OAPPSystemHolonDNA.json"), JsonSerializer.Serialize((OAPPSystemHolonDNA)OAPPSystemHolonDNA, options));
                result.Result = true;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"An error occured writing the {OAPPSystemHolonUIName} DNA in WriteOAPPSystemHolonDNAAsync: Reason: {ex.Message}");
            }

            return result;
        }

        public OASISResult<bool> WriteOAPPSystemHolonDNA(IOAPPSystemHolonDNA OAPPSystemHolonDNA, string fullPathToOAPPSystemHolon)
        {
            OASISResult<bool> result = new OASISResult<bool>();

            try
            {
                if (!Directory.Exists(fullPathToOAPPSystemHolon))
                    Directory.CreateDirectory(fullPathToOAPPSystemHolon);

                File.WriteAllText(Path.Combine(fullPathToOAPPSystemHolon, "OAPPSystemHolonDNA.json"), JsonSerializer.Serialize((OAPPSystemHolonDNA)OAPPSystemHolonDNA));
                result.Result = true;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"An error occured writing the {OAPPSystemHolonUIName} DNA in WriteOAPPSystemHolonDNA: Reason: {ex.Message}");
            }

            return result;
        }

        public async Task<OASISResult<IOAPPSystemHolonDNA>> ReadOAPPSystemHolonDNAAsync(string fullPathToOAPPSystemHolon)
        {
            OASISResult<IOAPPSystemHolonDNA> result = new OASISResult<IOAPPSystemHolonDNA>();

            try
            {
                result.Result = JsonSerializer.Deserialize<OAPPSystemHolonDNA>(await File.ReadAllTextAsync(Path.Combine(fullPathToOAPPSystemHolon, "OAPPSystemHolonDNA.json")));
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"An error occured reading the {OAPPSystemHolonUIName} DNA in ReadOAPPSystemHolonDNAAsync: Reason: {ex.Message}");
            }

            return result;
        }

        public OASISResult<IOAPPSystemHolonDNA> ReadOAPPSystemHolonDNA(string fullPathToOAPPSystemHolon)
        {
            OASISResult<IOAPPSystemHolonDNA> result = new OASISResult<IOAPPSystemHolonDNA>();

            try
            {
                result.Result = JsonSerializer.Deserialize<OAPPSystemHolonDNA>(File.ReadAllText(Path.Combine(fullPathToOAPPSystemHolon, "OAPPSystemHolonDNA.json")));
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"An error occured reading the {OAPPSystemHolonUIName} DNA in ReadOAPPSystemHolonDNA: Reason: {ex.Message}");
            }

            return result;
        }

        private async Task<OASISResult<T>> UpdateNumberOfVersionCountsAsync<T>(OASISResult<T> result, Guid avatarId, HolonType holonType, string errorMessage, ProviderType providerType = ProviderType.Default) where T : IOAPPSystemHolon, new()
        {
            OASISResult<IEnumerable<T>> versionsResult = await LoadVersionsAsync<T>(result.Result.OAPPSystemHolonDNA.Id, holonType, providerType);

            if (versionsResult != null && versionsResult.Result != null && !versionsResult.IsError)
            {
                foreach (T holonVersion in versionsResult.Result)
                {
                    holonVersion.OAPPSystemHolonDNA.NumberOfVersions = result.Result.OAPPSystemHolonDNA.NumberOfVersions;
                    OASISResult<T> versionSaveResult = await SaveAsync<T>(holonVersion, avatarId, providerType);

                    if (!(versionSaveResult != null && versionSaveResult.Result != null && !versionSaveResult.IsError))
                        OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} Error occured updating the NumberOfVersions for {OAPPSystemHolonUIName} with Id {holonVersion.Id} for provider {Enum.GetName(typeof(ProviderType), providerType)}. Reason: {versionSaveResult.Message}");
                }
            }
            else
                OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} Error occured updating the total installs for all {OAPPSystemHolonUIName} versions caused by an error in LoadOAPPSystemHolonVersionsAsync. Reason: {versionsResult.Message}");


            OASISResult<IEnumerable<IInstalledOAPPSystemHolon>> installedversionsResult = await ListInstalledOAPPSystemHolonsAsync(avatarId, providerType);

            if (installedversionsResult != null && installedversionsResult.Result != null && !installedversionsResult.IsError)
            {
                foreach (IInstalledOAPPSystemHolon installedOAPPSystemHolon in installedversionsResult.Result)
                {
                    installedOAPPSystemHolon.OAPPSystemHolonDNA.NumberOfVersions = result.Result.OAPPSystemHolonDNA.NumberOfVersions;
                    OASISResult<InstalledOAPPSystemHolon> installedOAPPSystemSaveResult = await SaveAsync((InstalledOAPPSystemHolon)installedOAPPSystemHolon, avatarId, providerType);

                    if (!(installedOAPPSystemSaveResult != null && installedOAPPSystemSaveResult.Result != null && !installedOAPPSystemSaveResult.IsError))
                        OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} Error occured updating the NumberOfVersions for Installed {OAPPSystemHolonUIName} with Id {installedOAPPSystemHolon.Id} for provider {Enum.GetName(typeof(ProviderType), providerType)}. Reason: {installedOAPPSystemSaveResult.Message}");
                }
            }
            else
                OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} Error occured updating the NumberOfVersions for all Installed {OAPPSystemHolonUIName} versions caused by an error in ListInstalledOAPPSystemHolonsAsync. Reason: {versionsResult.Message}");

            return result;
        }

        private OASISResult<T> UpdateNumberOfVersionCounts<T>(OASISResult<T> result, Guid avatarId, HolonType holonType, string errorMessage, ProviderType providerType = ProviderType.Default) where T : IOAPPSystemHolon, new()
        {
            OASISResult<IEnumerable<T>> versionsResult = LoadVersions<T>(result.Result.OAPPSystemHolonDNA.Id, holonType, providerType);

            if (versionsResult != null && versionsResult.Result != null && !versionsResult.IsError)
            {
                foreach (T holonVersion in versionsResult.Result)
                {
                    holonVersion.OAPPSystemHolonDNA.NumberOfVersions = result.Result.OAPPSystemHolonDNA.NumberOfVersions;
                    OASISResult<T> versionSaveResult = Save(holonVersion, avatarId, providerType);

                    if (!(versionSaveResult != null && versionSaveResult.Result != null && !versionSaveResult.IsError))
                        OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} Error occured updating the NumberOfVersions for {OAPPSystemHolonUIName} with Id {holonVersion.Id} for provider {Enum.GetName(typeof(ProviderType), providerType)}. Reason: {versionSaveResult.Message}");
                }
            }
            else
                OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} Error occured updating the total installs for all {OAPPSystemHolonUIName} versions caused by an error in LoadOAPPSystemHolonVersionsAsync. Reason: {versionsResult.Message}");


            OASISResult<IEnumerable<IInstalledOAPPSystemHolon>> installedversionsResult = ListInstalledOAPPSystemHolons(avatarId, providerType);

            if (installedversionsResult != null && installedversionsResult.Result != null && !installedversionsResult.IsError)
            {
                foreach (IInstalledOAPPSystemHolon installedOAPPSystemHolon in installedversionsResult.Result)
                {
                    installedOAPPSystemHolon.OAPPSystemHolonDNA.NumberOfVersions = result.Result.OAPPSystemHolonDNA.NumberOfVersions;
                    OASISResult<InstalledOAPPSystemHolon> installedOAPPSystemSaveResult = Save((InstalledOAPPSystemHolon)installedOAPPSystemHolon, avatarId, providerType);

                    if (!(installedOAPPSystemSaveResult != null && installedOAPPSystemSaveResult.Result != null && !installedOAPPSystemSaveResult.IsError))
                        OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} Error occured updating the NumberOfVersions for Installed {OAPPSystemHolonUIName} with Id {installedOAPPSystemHolon.Id} for provider {Enum.GetName(typeof(ProviderType), providerType)}. Reason: {installedOAPPSystemSaveResult.Message}");
                }
            }
            else
                OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} Error occured updating the NumberOfVersions for all Installed {OAPPSystemHolonUIName} versions caused by an error in ListInstalledOAPPSystemHolonsAsync. Reason: {versionsResult.Message}");

            return result;
        }

        private async Task<OASISResult<IDownloadedOAPPSystemHolon>> UpdateDownloadCountsAsync<T>(HolonType holonType, DownloadedOAPPSystemHolon downloadedOAPPSystemHolon, IOAPPSystemHolonDNA OAPPSystemHolonDNA, Guid avatarId, OASISResult<IDownloadedOAPPSystemHolon> result, string errorMessage, ProviderType providerType = ProviderType.Default) where T : IOAPPSystemHolon, new()
        {
            int totalDownloads = 0;
            OASISResult<IEnumerable<T>> holonVersionsResult = await LoadVersionsAsync<T>(OAPPSystemHolonDNA.Id, holonType, providerType);

            if (holonVersionsResult != null && holonVersionsResult.Result != null && !holonVersionsResult.IsError)
            {
                //Update total installs for all versions.
                foreach (T holonVersion in holonVersionsResult.Result)
                    totalDownloads += holonVersion.OAPPSystemHolonDNA.Installs;

                //Need to add this download (because its not saved yet).
                totalDownloads++;

                foreach (T holonVersion in holonVersionsResult.Result)
                {
                    holonVersion.OAPPSystemHolonDNA.TotalDownloads = totalDownloads;
                    OASISResult<T> holonVersionSaveResult = await SaveAsync(holonVersion, avatarId, providerType);

                    if (!(holonVersionSaveResult != null && holonVersionSaveResult.Result != null && !holonVersionSaveResult.IsError))
                        OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} Error occured updating the TotalDownloads for {OAPPSystemHolonUIName} with Id {holonVersion.Id} for provider {Enum.GetName(typeof(ProviderType), providerType)}. Reason: {holonVersionSaveResult.Message}");
                }

                OAPPSystemHolonDNA.TotalDownloads = totalDownloads;
                downloadedOAPPSystemHolon.OAPPSystemHolonDNA.TotalDownloads = totalDownloads;
            }
            else
                OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} Error occured updating the total downloads for all {OAPPSystemHolonUIName} versions caused by an error in LoadOAPPSystemHolonVersionsAsync. Reason: {holonVersionsResult.Message}");


            OASISResult<IEnumerable<IInstalledOAPPSystemHolon>> installedholonVersionsResult = await ListInstalledOAPPSystemHolonsAsync(avatarId, providerType);

            if (installedholonVersionsResult != null && installedholonVersionsResult.Result != null && !installedholonVersionsResult.IsError)
            {
                foreach (IInstalledOAPPSystemHolon holonVersion in installedholonVersionsResult.Result)
                {
                    holonVersion.OAPPSystemHolonDNA.TotalDownloads = totalDownloads;
                    OASISResult<InstalledOAPPSystemHolon> holonVersionSaveResult = await SaveAsync((InstalledOAPPSystemHolon)holonVersion, avatarId, providerType);

                    if (!(holonVersionSaveResult != null && holonVersionSaveResult.Result != null && !holonVersionSaveResult.IsError))
                        OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} Error occured updating the TotalDownloads for Installed {OAPPSystemHolonUIName} with Id {holonVersion.Id} for provider {Enum.GetName(typeof(ProviderType), providerType)}. Reason: {holonVersionSaveResult.Message}");
                }
            }
            else
                OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} Error occured updating the total downloads for all Installed {OAPPSystemHolonUIName} versions caused by an error in ListInstalledOAPPSystemHolonsAsync. Reason: {holonVersionsResult.Message}");

            return result;
        }

        private OASISResult<IDownloadedOAPPSystemHolon> UpdateDownloadCounts<T>(HolonType holonType, DownloadedOAPPSystemHolon downloadedOAPPSystemHolon, IOAPPSystemHolonDNA OAPPSystemHolonDNA, Guid avatarId, OASISResult<IDownloadedOAPPSystemHolon> result, string errorMessage, ProviderType providerType = ProviderType.Default) where T : IOAPPSystemHolon, new()
        {
            int totalDownloads = 0;
            OASISResult<IEnumerable<T>> holonVersionsResult = LoadVersions<T>(OAPPSystemHolonDNA.Id, holonType, providerType);

            if (holonVersionsResult != null && holonVersionsResult.Result != null && !holonVersionsResult.IsError)
            {
                //Update total installs for all versions.
                foreach (T holonVersion in holonVersionsResult.Result)
                    totalDownloads += holonVersion.OAPPSystemHolonDNA.Installs;

                //Need to add this download (because its not saved yet).
                totalDownloads++;

                foreach (T holonVersion in holonVersionsResult.Result)
                {
                    holonVersion.OAPPSystemHolonDNA.TotalDownloads = totalDownloads;
                    OASISResult<T> holonVersionSaveResult = Save(holonVersion, avatarId, providerType);

                    if (!(holonVersionSaveResult != null && holonVersionSaveResult.Result != null && !holonVersionSaveResult.IsError))
                        OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} Error occured updating the TotalDownloads for {OAPPSystemHolonUIName} with Id {holonVersion.Id} for provider {Enum.GetName(typeof(ProviderType), providerType)}. Reason: {holonVersionSaveResult.Message}");
                }

                OAPPSystemHolonDNA.TotalDownloads = totalDownloads;
                downloadedOAPPSystemHolon.OAPPSystemHolonDNA.TotalDownloads = totalDownloads;
            }
            else
                OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} Error occured updating the total downloads for all {OAPPSystemHolonUIName} versions caused by an error in LoadOAPPSystemHolonVersionsAsync. Reason: {holonVersionsResult.Message}");


            OASISResult<IEnumerable<IInstalledOAPPSystemHolon>> installedholonVersionsResult = ListInstalledOAPPSystemHolons(avatarId, providerType);

            if (installedholonVersionsResult != null && installedholonVersionsResult.Result != null && !installedholonVersionsResult.IsError)
            {
                foreach (IInstalledOAPPSystemHolon holonVersion in installedholonVersionsResult.Result)
                {
                    holonVersion.OAPPSystemHolonDNA.TotalDownloads = totalDownloads;
                    OASISResult<InstalledOAPPSystemHolon> holonVersionSaveResult = Save((InstalledOAPPSystemHolon)holonVersion, avatarId, providerType);

                    if (!(holonVersionSaveResult != null && holonVersionSaveResult.Result != null && !holonVersionSaveResult.IsError))
                        OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} Error occured updating the TotalDownloads for Installed {OAPPSystemHolonUIName} with Id {holonVersion.Id} for provider {Enum.GetName(typeof(ProviderType), providerType)}. Reason: {holonVersionSaveResult.Message}");
                }
            }
            else
                OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} Error occured updating the total downloads for all Installed {OAPPSystemHolonUIName} versions caused by an error in ListInstalledOAPPSystemHolonsAsync. Reason: {holonVersionsResult.Message}");

            return result;
        }

        private async Task<OASISResult<IInstalledOAPPSystemHolon>> UpdateInstallCountsAsync<T>(HolonType holonType, InstalledOAPPSystemHolon installedOAPPSystemHolon, IOAPPSystemHolonDNA OAPPSystemHolonDNA, Guid avatarId, OASISResult<IInstalledOAPPSystemHolon> result, string errorMessage, ProviderType providerType = ProviderType.Default) where T: IOAPPSystemHolon, new()
        {
            int totalInstalls = 0;
            OASISResult<IEnumerable<T>> holonVersionsResult = await LoadVersionsAsync<T>(OAPPSystemHolonDNA.Id, holonType, providerType);

            if (holonVersionsResult != null && holonVersionsResult.Result != null && !holonVersionsResult.IsError)
            {
                //Update total installs for all versions.
                foreach (T holonVersion in holonVersionsResult.Result)
                    totalInstalls += holonVersion.OAPPSystemHolonDNA.Installs;

                //Need to add this install (because its not saved yet).
                totalInstalls++;

                foreach (T holonVersion in holonVersionsResult.Result)
                {
                    holonVersion.OAPPSystemHolonDNA.TotalInstalls = totalInstalls;
                    OASISResult<T> holonVersionSaveResult = await SaveAsync(holonVersion, avatarId, providerType);

                    if (!(holonVersionSaveResult != null && holonVersionSaveResult.Result != null && !holonVersionSaveResult.IsError))
                        OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} Error occured updating the TotalInstalls for {OAPPSystemHolonUIName} with Id {holonVersion.Id} for provider {Enum.GetName(typeof(ProviderType), providerType)}. Reason: {holonVersionSaveResult.Message}");
                }

                OAPPSystemHolonDNA.TotalInstalls = totalInstalls;
                installedOAPPSystemHolon.OAPPSystemHolonDNA.TotalInstalls = totalInstalls;
            }
            else
                OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} Error occured updating the total installs for all {OAPPSystemHolonUIName} versions caused by an error in LoadOAPPSystemHolonVersionsAsync. Reason: {holonVersionsResult.Message}");


            OASISResult<IEnumerable<IInstalledOAPPSystemHolon>> installedholonVersionsResult = await ListInstalledOAPPSystemHolonsAsync(avatarId, providerType);

            if (installedholonVersionsResult != null && installedholonVersionsResult.Result != null && !installedholonVersionsResult.IsError)
            {
                foreach (IInstalledOAPPSystemHolon holonVersion in installedholonVersionsResult.Result)
                {
                    holonVersion.OAPPSystemHolonDNA.TotalInstalls = totalInstalls;
                    OASISResult<InstalledOAPPSystemHolon> holonVersionSaveResult = await SaveAsync<InstalledOAPPSystemHolon>((InstalledOAPPSystemHolon)holonVersion, avatarId, providerType);

                    if (!(holonVersionSaveResult != null && holonVersionSaveResult.Result != null && !holonVersionSaveResult.IsError))
                        OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} Error occured updating the TotalInstalls for Installed {OAPPSystemHolonUIName} with Id {holonVersion.Id} for provider {Enum.GetName(typeof(ProviderType), providerType)}. Reason: {holonVersionSaveResult.Message}");
                }
            }
            else
                OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} Error occured updating the total installs for all Installed {OAPPSystemHolonUIName} versions caused by an error in ListInstalledOAPPSystemHolonsAsync. Reason: {holonVersionsResult.Message}");

            return result;
        }

        private OASISResult<IInstalledOAPPSystemHolon> UpdateInstallCounts<T>(HolonType holonType, InstalledOAPPSystemHolon installedOAPPSystemHolon, IOAPPSystemHolonDNA OAPPSystemHolonDNA, Guid avatarId, OASISResult<IInstalledOAPPSystemHolon> result, string errorMessage, ProviderType providerType = ProviderType.Default) where T : IOAPPSystemHolon, new()
        {
            int totalInstalls = 0;
            OASISResult<IEnumerable<T>> holonVersionsResult = LoadVersions<T>(OAPPSystemHolonDNA.Id, holonType, providerType);

            if (holonVersionsResult != null && holonVersionsResult.Result != null && !holonVersionsResult.IsError)
            {
                //Update total installs for all versions.
                foreach (T holonVersion in holonVersionsResult.Result)
                    totalInstalls += holonVersion.OAPPSystemHolonDNA.Installs;

                //Need to add this install (because its not saved yet).
                totalInstalls++;

                foreach (T holonVersion in holonVersionsResult.Result)
                {
                    holonVersion.OAPPSystemHolonDNA.TotalInstalls = totalInstalls;
                    OASISResult<T> holonVersionSaveResult = Save(holonVersion, avatarId, providerType);

                    if (!(holonVersionSaveResult != null && holonVersionSaveResult.Result != null && !holonVersionSaveResult.IsError))
                        OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} Error occured updating the TotalInstalls for {OAPPSystemHolonUIName} with Id {holonVersion.Id} for provider {Enum.GetName(typeof(ProviderType), providerType)}. Reason: {holonVersionSaveResult.Message}");
                }

                OAPPSystemHolonDNA.TotalInstalls = totalInstalls;
                installedOAPPSystemHolon.OAPPSystemHolonDNA.TotalInstalls = totalInstalls;
            }
            else
                OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} Error occured updating the total installs for all {OAPPSystemHolonUIName} versions caused by an error in LoadOAPPSystemHolonVersionsAsync. Reason: {holonVersionsResult.Message}");


            OASISResult<IEnumerable<IInstalledOAPPSystemHolon>> installedholonVersionsResult = ListInstalledOAPPSystemHolons(avatarId, providerType);

            if (installedholonVersionsResult != null && installedholonVersionsResult.Result != null && !installedholonVersionsResult.IsError)
            {
                foreach (IInstalledOAPPSystemHolon holonVersion in installedholonVersionsResult.Result)
                {
                    holonVersion.OAPPSystemHolonDNA.TotalInstalls = totalInstalls;
                    OASISResult<InstalledOAPPSystemHolon> holonVersionSaveResult = Save((InstalledOAPPSystemHolon)holonVersion, avatarId, providerType);

                    if (!(holonVersionSaveResult != null && holonVersionSaveResult.Result != null && !holonVersionSaveResult.IsError))
                        OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} Error occured updating the TotalInstalls for Installed {OAPPSystemHolonUIName} with Id {holonVersion.Id} for provider {Enum.GetName(typeof(ProviderType), providerType)}. Reason: {holonVersionSaveResult.Message}");
                }
            }
            else
                OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} Error occured updating the total installs for all Installed {OAPPSystemHolonUIName} versions caused by an error in ListInstalledOAPPSystemHolonsAsync. Reason: {holonVersionsResult.Message}");

            return result;
        }

        private async Task<OASISResult<IInstalledOAPPSystemHolon>> UninstallOAPPSystemHolonAsync(OASISResult<InstalledOAPPSystemHolon> installedOAPPSystemHolonResult, Guid avatarId, string errorMessage, ProviderType providerType)
        {
            OASISResult<IInstalledOAPPSystemHolon> result = new OASISResult<IInstalledOAPPSystemHolon>();

            if (installedOAPPSystemHolonResult != null && !installedOAPPSystemHolonResult.IsError && installedOAPPSystemHolonResult.Result != null)
                result = await UninstallOAPPSystemHolonAsync(installedOAPPSystemHolonResult.Result, avatarId, errorMessage, providerType); 
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonByMetaDataAsync. Reason: {installedOAPPSystemHolonResult.Message}");

            return result;
        }

        private OASISResult<IInstalledOAPPSystemHolon> UninstallOAPPSystemHolon(OASISResult<InstalledOAPPSystemHolon> installedOAPPSystemHolonResult, Guid avatarId, string errorMessage, ProviderType providerType)
        {
            OASISResult<IInstalledOAPPSystemHolon> result = new OASISResult<IInstalledOAPPSystemHolon>();

            if (installedOAPPSystemHolonResult != null && !installedOAPPSystemHolonResult.IsError && installedOAPPSystemHolonResult.Result != null)
                result = UninstallOAPPSystemHolon(installedOAPPSystemHolonResult.Result, avatarId, errorMessage, providerType);
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonByMetaData. Reason: {installedOAPPSystemHolonResult.Message}");

            return result;
        }

        private OASISResult<IEnumerable<T>> FilterResultsForVersion<T>(Guid avatarId, OASISResult<IEnumerable<T>> results, bool showAllVersions = false, int version = 0)
        {
            OASISResult<IEnumerable<IOAPPSystemHolon>> result = new OASISResult<IEnumerable<IOAPPSystemHolon>>();
            List<IOAPPSystemHolon> templates = new List<IOAPPSystemHolon>();

            if (!showAllVersions)
            {
                if (results.Result != null && !result.IsError)
                {
                    if (version == 0) //latest version
                    {
                        Dictionary<string, IOAPPSystemHolon> latestVersions = new Dictionary<string, IOAPPSystemHolon>();
                        string metaDataId = "";
                        int latestVersion = 0;
                        int currentVersion = 0;

                        foreach (IOAPPSystemHolon oappSystemHolon in results.Result)
                        {
                            if (oappSystemHolon.MetaData != null && oappSystemHolon.MetaData.ContainsKey("OAPPSystemHolonId") && oappSystemHolon.MetaData["OAPPSystemHolonId"] != null)
                                metaDataId = oappSystemHolon.MetaData["OAPPSystemHolonId"].ToString();

                            latestVersion = latestVersions.ContainsKey(metaDataId) ? Convert.ToInt32(latestVersions[metaDataId].OAPPSystemHolonDNA.Version.Replace(".", "")) : 0;
                            currentVersion = Convert.ToInt32(oappSystemHolon.OAPPSystemHolonDNA.Version.Replace(".", ""));

                            if ((latestVersions.ContainsKey(metaDataId) &&
                                currentVersion > latestVersion)
                                //oappSystemHolon.OAPPSystemHolonDNA.CreatedOn > latestVersions[metaDataId].OAPPSystemHolonDNA.CreatedOn)
                                || !latestVersions.ContainsKey(metaDataId))
                                latestVersions[metaDataId] = oappSystemHolon;
                        }

                        result.Result = latestVersions.Values.ToList();
                    }
                    else
                    {
                        List<IOAPPSystemHolon> filteredList = new List<IOAPPSystemHolon>();

                        foreach (IOAPPSystemHolon oappSystemHolon in results.Result)
                        {
                            if (oappSystemHolon.MetaData["VersionSequence"].ToString() == version.ToString())
                                filteredList.Add(oappSystemHolon);
                        }

                        result.Result = filteredList;
                    }
                }
            }
            else
                result.Result = results.Result;

            //Filter out any templates that are not created by the avatar or published on STARNET.
            foreach (IOAPPSystemHolon oappSystemHolon in result.Result)
            {
                if (oappSystemHolon.OAPPSystemHolonDNA.CreatedByAvatarId == avatarId)
                    templates.Add(oappSystemHolon);
                
                else if (oappSystemHolon.OAPPSystemHolonDNA.PublishedOn != DateTime.MinValue)
                    templates.Add(oappSystemHolon);
            }

            result.Result = templates;
            result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(results, result);
            return result;
        }

        private OASISResult<bool> ValidateVersion(string dnaVersion, string storedVersion, string fullPathToOAPPSystemHolonFolder, bool firstPublish, bool edit)
        {
            OASISResult<bool> result = new OASISResult<bool>();
            int dnaVersionInt = 0;
            int stotedVersionInt = 0;

            if (!firstPublish)
            {
                if (edit && dnaVersion != storedVersion)
                {
                    OASISErrorHandling.HandleError(ref result, $"The version in the OAPPSystemHolonDNA ({dnaVersion}) is not the same as the version you are attempting to edit ({storedVersion}). They must be the same if you wish to upload new files for version {storedVersion}. Please edit the OAPPSystemHolonDNA.json file found in the root of your T folder ({fullPathToOAPPSystemHolonFolder}).");
                    return result;
                } 
                else
                {
                    if (!StringHelper.IsValidVersion(dnaVersion))
                    {
                        OASISErrorHandling.HandleError(ref result, $"The version in the OAPPSystemHolonDNA ({dnaVersion}) is not valid! Please make sure you enter a valid version in the form of MM.mm.rr (Major.Minor.Revision) in the OAPPSystemHolonDNA.json file found in the root of your T folder ({fullPathToOAPPSystemHolonFolder}).");
                        return result;
                    }

                    if (dnaVersion == storedVersion)
                    {
                        OASISErrorHandling.HandleError(ref result, $"The version in the OAPPSystemHolonDNA ({dnaVersion}) is the same as the previous version ({storedVersion}). Please make sure you increment the version in the OAPPSystemHolonDNA.json file found in the root of your T folder ({fullPathToOAPPSystemHolonFolder}).");
                        return result;
                    }

                    if (!int.TryParse(dnaVersion.Replace(".", ""), out dnaVersionInt))
                    {
                        OASISErrorHandling.HandleError(ref result, $"The version in the OAPPSystemHolonDNA ({dnaVersion}) is not valid! Please make sure you enter a valid version in the form of MM.mm.rr (Major.Minor.Revision) in the OAPPSystemHolonDNA.json file found in the root of your T folder ({fullPathToOAPPSystemHolonFolder}).");
                        return result;
                    }

                    //Should hopefully never occur! ;-)
                    if (!int.TryParse(storedVersion.Replace(".", ""), out stotedVersionInt))
                        OASISErrorHandling.HandleWarning(ref result, $"The version stored in the OASIS ({storedVersion}) is not valid!");

                    if (dnaVersionInt <= stotedVersionInt)
                    {
                        OASISErrorHandling.HandleError(ref result, $"The version in the OAPPSystemHolonDNA ({dnaVersion}) is less than the previous version ({storedVersion}). Please make sure you increment the version in the OAPPSystemHolonDNA.json file found in the root of your T folder.");
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
                    OnOAPPSystemHolonUploadStatusChanged?.Invoke(this, new OAPPSystemHolonUploadProgressEventArgs() { Progress = _progress, Status = Enums.OAPPSystemHolonUploadStatus.NotStarted });
                    break;

                case Google.Apis.Upload.UploadStatus.Starting:
                    _progress = 0;
                    OnOAPPSystemHolonUploadStatusChanged?.Invoke(this, new OAPPSystemHolonUploadProgressEventArgs() { Progress = _progress, Status = Enums.OAPPSystemHolonUploadStatus.Uploading });
                    break;

                case Google.Apis.Upload.UploadStatus.Completed:
                    _progress = 100;
                    OnOAPPSystemHolonUploadStatusChanged?.Invoke(this, new OAPPSystemHolonUploadProgressEventArgs() { Progress = _progress, Status = Enums.OAPPSystemHolonUploadStatus.Uploaded });
                    break;

                case Google.Apis.Upload.UploadStatus.Uploading:
                    {
                        if (_fileLength > 0)
                        {
                            _progress = Convert.ToInt32(((double)progress.BytesSent / (double)_fileLength) * 100);
                            OnOAPPSystemHolonUploadStatusChanged?.Invoke(this, new OAPPSystemHolonUploadProgressEventArgs() { Progress = _progress, Status = Enums.OAPPSystemHolonUploadStatus.Uploading });
                        }
                    }
                    break;

                case Google.Apis.Upload.UploadStatus.Failed:
                    OnOAPPSystemHolonUploadStatusChanged?.Invoke(this, new OAPPSystemHolonUploadProgressEventArgs() { Progress = _progress, Status = Enums.OAPPSystemHolonUploadStatus.Error, ErrorMessage = progress.Exception.ToString() });
                    break;
            }
        }

        private void OnDownloadProgress(Google.Apis.Download.IDownloadProgress progress)
        {
            switch (progress.Status)
            {
                case Google.Apis.Download.DownloadStatus.NotStarted:
                    _progress = 0;
                    OnOAPPSystemHolonDownloadStatusChanged?.Invoke(this, new OAPPSystemHolonDownloadProgressEventArgs() { Progress = _progress, Status = Enums.OAPPSystemHolonDownloadStatus.NotStarted });
                    break;

                case Google.Apis.Download.DownloadStatus.Completed:
                    _progress = 100;
                    OnOAPPSystemHolonDownloadStatusChanged?.Invoke(this, new OAPPSystemHolonDownloadProgressEventArgs() { Progress = _progress, Status = Enums.OAPPSystemHolonDownloadStatus.Downloaded });
                    break;

                case Google.Apis.Download.DownloadStatus.Downloading:
                    {
                        if (_fileLength > 0)
                        {
                            _progress = Convert.ToInt32(((double)progress.BytesDownloaded / (double)_fileLength) * 100);
                            // _progress = Convert.ToInt32(_fileLength / progress.BytesDownloaded);
                            OnOAPPSystemHolonDownloadStatusChanged?.Invoke(this, new OAPPSystemHolonDownloadProgressEventArgs() { Progress = _progress, Status = Enums.OAPPSystemHolonDownloadStatus.Downloading });
                        }
                    }
                    break;

                case Google.Apis.Download.DownloadStatus.Failed:
                    OnOAPPSystemHolonDownloadStatusChanged?.Invoke(this, new OAPPSystemHolonDownloadProgressEventArgs() { Progress = _progress, Status = Enums.OAPPSystemHolonDownloadStatus.Error, ErrorMessage = progress.Exception.ToString() });
                    break;
            }
        }
    }
}