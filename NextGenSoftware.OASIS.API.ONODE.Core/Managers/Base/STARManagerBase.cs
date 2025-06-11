using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Diagnostics;
using System.IO.Compression;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Google.Cloud.Storage.V1;
using NextGenSoftware.Utilities;
using NextGenSoftware.CLI.Engine;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.ONODE.Core.Objects;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces;
using NextGenSoftware.OASIS.API.ONODE.Core.Enums.STARHolon;
using NextGenSoftware.OASIS.API.ONODE.Core.Events.STARHolon;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Holons;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Managers.Base
{
    public class STARManagerBase<T1, T2, T3> : PublishManagerBase, ISTARManagerBase<T1, T2, T3> where T1 : ISTARHolon, new()
        where T2 : IDownloadedSTARHolon, new()
        where T3 : IInstalledSTARHolon, new()
    {
        private int _progress = 0;
        private long _fileLength = 0;

        public STARManagerBase(Guid avatarId, OASISDNA OASISDNA = null) : base(avatarId, OASISDNA) { }

        public STARManagerBase(IOASISStorageProvider OASISStorageProvider, Guid avatarId, Type STARHolonSubType, OASISDNA OASISDNA = null) : base(OASISStorageProvider, avatarId, OASISDNA) { }

        public STARManagerBase(Guid avatarId, Type STARHolonSubType, OASISDNA OASISDNA = null, HolonType STARHolonType = HolonType.STARHolon, HolonType STARInstalledHolonType = HolonType.InstalledSTARHolon, string STARHolonUIName = "OAPP System Holon", string STARHolonIdName = "STARHolonId", string STARHolonNameName = "STARHolonName", string STARHolonTypeName = "STARHolonType", string STARHolonFileExtention = "oappsystemholon", string STARHolonGoogleBucket = "oasis_oappsystemholons", string STARHolonDNAFileName = "STARHolonDNA.json", string STARHolonDNAJSONName = "STARHolonDNAJSON") : base(avatarId, OASISDNA)
        {
            this.STARHolonType = STARHolonType;
            this.STARInstalledHolonType = STARInstalledHolonType;
            this.STARHolonUIName = STARHolonUIName;
            this.STARHolonIdName = STARHolonIdName;
            this.STARHolonNameName = STARHolonNameName;
            this.STARHolonTypeName = STARHolonTypeName;
            this.STARHolonFileExtention = STARHolonFileExtention;
            this.STARHolonGoogleBucket = STARHolonGoogleBucket;
            this.STARHolonDNAFileName = STARHolonDNAFileName;
            this.STARHolonDNAJSONName = STARHolonDNAJSONName;
            this.STARHolonSubType = STARHolonSubType;
        }

        public STARManagerBase(IOASISStorageProvider OASISStorageProvider, Guid avatarId, Type STARHolonSubType, OASISDNA OASISDNA = null, HolonType STARHolonType = HolonType.STARHolon, HolonType STARInstalledHolonType = HolonType.InstalledSTARHolon, string STARHolonUIName = "OAPP System Holon", string STARHolonIdName = "STARHolonId", string STARHolonNameName = "STARHolonName", string STARHolonTypeName = "STARHolonType", string STARHolonFileExtention = "oappsystemholon", string STARHolonGoogleBucket = "oasis_oappsystemholons", string STARHolonDNAFileName = "STARHolonDNA.json", string STARHolonDNAJSONName = "STARHolonDNAJSON") : base(OASISStorageProvider, avatarId, OASISDNA)
        {
            this.STARHolonType = STARHolonType;
            this.STARInstalledHolonType = STARInstalledHolonType;
            this.STARHolonUIName = STARHolonUIName;
            this.STARHolonIdName = STARHolonIdName;
            this.STARHolonNameName = STARHolonNameName;
            this.STARHolonTypeName = STARHolonTypeName;
            this.STARHolonFileExtention = STARHolonFileExtention;
            this.STARHolonGoogleBucket = STARHolonGoogleBucket;
            this.STARHolonDNAFileName = STARHolonDNAFileName;
            this.STARHolonDNAJSONName = STARHolonDNAJSONName;
            this.STARHolonSubType = STARHolonSubType;
        }

        public delegate void PublishStatusChanged(object sender, STARHolonPublishStatusEventArgs e);
        public delegate void InstallStatusChanged(object sender, STARHolonInstallStatusEventArgs e);
        public delegate void UploadStatusChanged(object sender, STARHolonUploadProgressEventArgs e);
        public delegate void DownloadStatusChanged(object sender, STARHolonDownloadProgressEventArgs e);

        /// <summary>
        /// Fired when there is a change in the OAPP publish status.
        /// </summary>
        public event PublishStatusChanged OnPublishStatusChanged;

        /// <summary>
        /// Fired when there is a change to the OAPP Install status.
        /// </summary>
        public event InstallStatusChanged OnInstallStatusChanged;

        /// <summary>
        /// Fired when there is a change in the OAPP upload status.
        /// </summary>
        public event UploadStatusChanged OnUploadStatusChanged;

        /// <summary>
        /// Fired when there is a change in the OAPP download status.
        /// </summary>
        public event DownloadStatusChanged OnDownloadStatusChanged;

        public HolonType STARHolonType { get; set; } = HolonType.STARHolon;
        public HolonType STARInstalledHolonType { get; set; } = HolonType.InstalledSTARHolon;
        public string STARHolonUIName { get; set; } = "OAPP System Holon";
        public string STARHolonIdName { get; set; } = "STARHolonId";
        public string STARHolonNameName { get; set; } = "STARHolonName";
        public string STARHolonTypeName { get; set; } = "STARHolonType";
        public string STARHolonFileExtention { get; set; } = "oappsystemholon";
        public string STARHolonGoogleBucket { get; set; } = "oasis_oappsystemholons";
        public string STARHolonDNAFileName { get; set; } = "STARHolonDNA.json";
        public string STARHolonDNAJSONName { get; set; } = "STARHolonDNAJSON";
        public Type STARHolonSubType { get; set; }

        public virtual async Task<OASISResult<T1>> CreateAsync(Guid avatarId, string name, string description, object holonSubType, string fullPathToT, Dictionary<string, object> metaData = null, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            string errorMessage = "Error occured in STARManagerBase.CreateAsync, Reason:";

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

                T1 holon = new T1()
                {
                    Id = Guid.NewGuid(),
                    Name = name,
                    Description = description
                };

                holon.MetaData[STARHolonIdName] = holon.Id.ToString();
                holon.MetaData[STARHolonNameName] = holon.Name;
                //T.MetaData[STARHolonTypeName] = Enum.GetName(typeof(STARHolonType), STARHolonType);

                Type holonSubTypeType = holonSubType.GetType();
                holon.MetaData[STARHolonTypeName] = Enum.GetName(holonSubTypeType, holonSubType);
                holon.MetaData["Version"] = "1.0.0";
                holon.MetaData["VersionSequence"] = 1;
                holon.MetaData["Active"] = "1";
                holon.MetaData["CreatedByAvatarId"] = avatarId.ToString();

                //T.MetaData["LatestVersion"] = "1";

                OASISResult<IAvatar> avatarResult = await AvatarManager.Instance.LoadAvatarAsync(avatarId, false, true, providerType);

                if (avatarResult != null && avatarResult.Result != null && !avatarResult.IsError)
                {
                    STARHolonDNA STARHolonDNA = new STARHolonDNA()
                    {
                        Id = holon.Id,
                        Name = name,
                        Description = description,
                        STARHolonType = Enum.GetName(holonSubTypeType, holonSubType),
                        CreatedByAvatarId = avatarId,
                        CreatedByAvatarUsername = avatarResult.Result.Username,
                        CreatedOn = DateTime.Now,
                        Version = "1.0.0",
                        STARODKVersion = OASISBootLoader.OASISBootLoader.STARODKVersion,
                        OASISVersion = OASISBootLoader.OASISBootLoader.OASISVersion,
                        COSMICVersion = OASISBootLoader.OASISBootLoader.COSMICVersion,
                        DotNetVersion = OASISBootLoader.OASISBootLoader.DotNetVersion,
                        SourcePath = fullPathToT,
                        MetaData = metaData
                    };

                    OASISResult<bool> writeSTARHolonDNAResult = await WriteDNAAsync(STARHolonDNA, fullPathToT);

                    if (writeSTARHolonDNAResult != null && writeSTARHolonDNAResult.Result && !writeSTARHolonDNAResult.IsError)
                    {
                        holon.STARHolonDNA = STARHolonDNA;
                        OASISResult<T1> saveHolonResult = await Data.SaveHolonAsync<T1>(holon, avatarId, true, true, 0, true, false, providerType);

                        if (saveHolonResult != null && saveHolonResult.Result != null && !saveHolonResult.IsError)
                        {
                            result.Result = saveHolonResult.Result;
                            result.Message = $"Successfully created the {STARHolonUIName} on the {Enum.GetName(typeof(ProviderType), providerType)} provider by AvatarId {avatarId} for {STARHolonTypeName} {Enum.GetName(holonSubTypeType, holonSubType)}.";
                        }
                        else
                            OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured saving the {STARHolonUIName} to the {Enum.GetName(typeof(ProviderType), providerType)} provider. Reason: {saveHolonResult.Message}");
                    }
                    else
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured writing the {STARHolonUIName} DNA. Reason: {writeSTARHolonDNAResult.Message}");
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadAvatarAsync on {Enum.GetName(typeof(ProviderType), providerType)} provider. Reason: {avatarResult.Message}");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured saving the {STARHolonUIName} to the {Enum.GetName(typeof(ProviderType), providerType)} provider. Reason: {ex}");
            }

            return result;
        }

        public virtual OASISResult<T1> Create(Guid avatarId, string name, string description, object holonSubType, string fullPathToT, Dictionary<string, object> metaData = null, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            string errorMessage = "Error occured in STARManagerBase.Create, Reason:";

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

                T1 holon = new T1()
                {
                    Id = Guid.NewGuid(),
                    Name = name,
                    Description = description
                };

                holon.MetaData[STARHolonIdName] = holon.Id.ToString();
                holon.MetaData[STARHolonNameName] = holon.Name;
                //T.MetaData[STARHolonTypeName] = Enum.GetName(typeof(STARHolonType), STARHolonType);

                Type holonSubTypeType = holonSubType.GetType();
                holon.MetaData[STARHolonTypeName] = Enum.GetName(holonSubTypeType, holonSubType);
                holon.MetaData["Version"] = "1.0.0";
                holon.MetaData["VersionSequence"] = 1;
                holon.MetaData["Active"] = "1";
                holon.MetaData["CreatedByAvatarId"] = avatarId.ToString();

                //T.MetaData["LatestVersion"] = "1";

                OASISResult<IAvatar> avatarResult = AvatarManager.Instance.LoadAvatar(avatarId, false, true, providerType);

                if (avatarResult != null && avatarResult.Result != null && !avatarResult.IsError)
                {
                    STARHolonDNA STARHolonDNA = new STARHolonDNA()
                    {
                        Id = holon.Id,
                        Name = name,
                        Description = description,
                        //STARHolonType = STARHolonType,
                        CreatedByAvatarId = avatarId,
                        CreatedByAvatarUsername = avatarResult.Result.Username,
                        CreatedOn = DateTime.Now,
                        Version = "1.0.0",
                        STARODKVersion = OASISBootLoader.OASISBootLoader.STARODKVersion,
                        OASISVersion = OASISBootLoader.OASISBootLoader.OASISVersion,
                        COSMICVersion = OASISBootLoader.OASISBootLoader.COSMICVersion,
                        DotNetVersion = OASISBootLoader.OASISBootLoader.DotNetVersion,
                        SourcePath = fullPathToT,
                        MetaData = metaData
                    };

                    OASISResult<bool> writeSTARHolonDNAResult = WriteDNA(STARHolonDNA, fullPathToT);

                    if (writeSTARHolonDNAResult != null && writeSTARHolonDNAResult.Result && !writeSTARHolonDNAResult.IsError)
                    {
                        holon.STARHolonDNA = STARHolonDNA;
                        OASISResult<T1> saveHolonResult = Data.SaveHolon<T1>(holon, avatarId, true, true, 0, true, false, providerType);

                        if (saveHolonResult != null && saveHolonResult.Result != null && !saveHolonResult.IsError)
                        {
                            result.Result = saveHolonResult.Result;
                            result.Message = $"Successfully created the {STARHolonUIName} on the {Enum.GetName(typeof(ProviderType), providerType)} provider by AvatarId {avatarId} for {STARHolonTypeName} {Enum.GetName(holonSubTypeType, holonSubType)}.";
                        }
                        else
                            OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured saving the {STARHolonUIName} to the {Enum.GetName(typeof(ProviderType), providerType)} provider. Reason: {saveHolonResult.Message}");
                    }
                    else
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured writing the {STARHolonUIName} DNA. Reason: {writeSTARHolonDNAResult.Message}");
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadAvatar on {Enum.GetName(typeof(ProviderType), providerType)} provider. Reason: {avatarResult.Message}");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured saving the {STARHolonUIName} to the {Enum.GetName(typeof(ProviderType), providerType)} provider. Reason: {ex}");
            }

            return result;
        }

        #region COSMICManagerBase
        public async Task<OASISResult<T1>> SaveAsync(Guid avatarId, T1 holon, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = new OASISResult<T1>();

            if (!Directory.Exists(holon.STARHolonDNA.SourcePath))
                Directory.CreateDirectory(holon.STARHolonDNA.SourcePath);

            holon.MetaData[STARHolonDNAJSONName] = JsonSerializer.Serialize(holon.STARHolonDNA);

            OASISResult<T1> saveResult = await SaveHolonAsync<T1>(holon, avatarId, providerType, "STARManagerBase.SaveAsync<T>");
            result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(saveResult, result);
            result.Result = saveResult.Result;
            return result;
        }

        public OASISResult<T1> Save(Guid avatarId, T1 holon, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = new OASISResult<T1>();

            if (!Directory.Exists(holon.STARHolonDNA.SourcePath))
                Directory.CreateDirectory(holon.STARHolonDNA.SourcePath);

            holon.MetaData[STARHolonDNAJSONName] = JsonSerializer.Serialize(holon.STARHolonDNA);

            OASISResult<T1> saveResult = SaveHolon<T1>(holon, avatarId, providerType, "STARManagerBase.Save<T>");
            result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(saveResult, result);
            result.Result = saveResult.Result;
            return result;
        }

        public async Task<OASISResult<T3>> SaveAsync(Guid avatarId, T3 holon, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T3> result = new OASISResult<T3>();

            if (!Directory.Exists(holon.STARHolonDNA.SourcePath))
                Directory.CreateDirectory(holon.STARHolonDNA.SourcePath);

            holon.MetaData[STARHolonDNAJSONName] = JsonSerializer.Serialize(holon.STARHolonDNA);

            OASISResult<T3> saveResult = await SaveHolonAsync<T3>(holon, avatarId, providerType, "STARManagerBase.SaveAsync<T>");
            result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(saveResult, result);
            result.Result = saveResult.Result;
            return result;
        }

        public OASISResult<T3> Save(Guid avatarId, T3 holon, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T3> result = new OASISResult<T3>();

            if (!Directory.Exists(holon.STARHolonDNA.SourcePath))
                Directory.CreateDirectory(holon.STARHolonDNA.SourcePath);

            holon.MetaData[STARHolonDNAJSONName] = JsonSerializer.Serialize(holon.STARHolonDNA);

            OASISResult<T3> saveResult = SaveHolon<T3>(holon, avatarId, providerType, "STARManagerBase.Save<T>");
            result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(saveResult, result);
            result.Result = saveResult.Result;
            return result;
        }

        public async Task<OASISResult<T1>> LoadAsync(Guid avatarId, Guid id, int version = 0, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            OASISResult<IEnumerable<T1>> loadResult = await Data.LoadHolonsByMetaDataAsync<T1>(STARHolonIdName, id.ToString(), STARHolonType, true, true, 0, true, false, 0, HolonType.All, 0, providerType);
            OASISResult<IEnumerable<T1>> filterdResult = FilterResultsForVersion(avatarId, loadResult, false, version);

            if (filterdResult != null && filterdResult.Result != null && !filterdResult.IsError)
                result.Result = filterdResult.Result.FirstOrDefault();
            else
                OASISErrorHandling.HandleError(ref result, $"Error occured in LoadAsync<T> loading the {STARHolonUIName} with Id {id} from the {Enum.GetName(typeof(ProviderType), providerType)} provider. Reason: {filterdResult.Message}");

            return result;
        }

        public OASISResult<T1> Load(Guid avatarId, Guid id, int version = 0, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            OASISResult<IEnumerable<T1>> loadResult = Data.LoadHolonsByMetaData<T1>(STARHolonIdName, id.ToString(), STARHolonType, true, true, 0, true, false, 0, HolonType.All, 0, providerType);
            OASISResult<IEnumerable<T1>> filterdResult = FilterResultsForVersion(avatarId, loadResult, false, version);

            if (filterdResult != null && filterdResult.Result != null && !filterdResult.IsError)
                result.Result = filterdResult.Result.FirstOrDefault();
            else
                OASISErrorHandling.HandleError(ref result, $"Error occured in Load<T> loading the {STARHolonUIName} with Id {id} from the {Enum.GetName(typeof(ProviderType), providerType)} provider. Reason: {filterdResult.Message}");

            return result;
        }

        public async Task<OASISResult<IEnumerable<T1>>> LoadAllAsync(Guid avatarId, object holonSubType, bool loadAllTypes = true, bool showAllVersions = false, int version = 0, HolonType STARHolonType = HolonType.Default, string STARHolonTypeName = "Default", ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<T1>> result = new OASISResult<IEnumerable<T1>>();
            OASISResult<IEnumerable<T1>> loadHolonsResult = null;

            if (STARHolonType == HolonType.Default)
                STARHolonType = this.STARHolonType;

            if (STARHolonTypeName == "Default")
                STARHolonTypeName = this.STARHolonTypeName;

            if (loadAllTypes)
                loadHolonsResult = await Data.LoadAllHolonsAsync<T1>(STARHolonType, true, true, 0, true, false, HolonType.All, 0, providerType);
            else
                loadHolonsResult = await Data.LoadHolonsByMetaDataAsync<T1>(STARHolonTypeName, Enum.GetName(holonSubType.GetType(), holonSubType), HolonType.All, true, true, 0, true, false, 0, HolonType.All, 0);

            return FilterResultsForVersion(avatarId, loadHolonsResult, showAllVersions, version);
        }

        public OASISResult<IEnumerable<T1>> LoadAll(Guid avatarId, object holonSubType, bool loadAllTypes = true, bool showAllVersions = false, int version = 0, HolonType STARHolonType = HolonType.Default, string STARHolonTypeName = "Default", ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<T1>> result = new OASISResult<IEnumerable<T1>>();
            OASISResult<IEnumerable<T1>> loadHolonsResult = null;

            if (STARHolonType == HolonType.Default)
                STARHolonType = this.STARHolonType;

            if (STARHolonTypeName == "Default")
                STARHolonTypeName = this.STARHolonTypeName;

            if (loadAllTypes)
                loadHolonsResult = Data.LoadAllHolons<T1>(STARHolonType, true, true, 0, true, false, HolonType.All, 0, providerType);
            else
                loadHolonsResult = Data.LoadHolonsByMetaData<T1>(STARHolonTypeName, Enum.GetName(holonSubType.GetType(), holonSubType), HolonType.All, true, true, 0, true, false, 0, HolonType.All, 0);

            return FilterResultsForVersion(avatarId, loadHolonsResult, showAllVersions, version);
        }

        public async Task<OASISResult<IEnumerable<T1>>> LoadAllForAvatarAsync(Guid avatarId, bool showAllVersions = false, int version = 0, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<T1>> result = new OASISResult<IEnumerable<T1>>();
            OASISResult<IEnumerable<T1>> loadHolonsResult = await Data.LoadHolonsByMetaDataAsync<T1>(new Dictionary<string, string>()
            {
                { "CreatedByAvatarId", avatarId.ToString() },
                { "Active", "1" }

            }, MetaKeyValuePairMatchMode.All, STARHolonType, providerType: providerType);

            return FilterResultsForVersion(avatarId, loadHolonsResult, showAllVersions, version);
        }

        public OASISResult<IEnumerable<T1>> LoadAllForAvatar(Guid avatarId, bool showAllVersions = false, int version = 0, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<T1>> result = new OASISResult<IEnumerable<T1>>();
            OASISResult<IEnumerable<T1>> loadHolonsResult = Data.LoadHolonsByMetaData<T1>(new Dictionary<string, string>()
            {
                { "CreatedByAvatarId", avatarId.ToString() },
                { "Active", "1" }

            }, MetaKeyValuePairMatchMode.All, STARHolonType, providerType: providerType);

            return FilterResultsForVersion(avatarId, loadHolonsResult, showAllVersions, version);
        }

        public async Task<OASISResult<IEnumerable<T1>>> SearchAsync(Guid avatarId, string searchTerm, bool searchOnlyForCurrentAvatar = true, bool showAllVersions = false, int version = 0, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<T1>> result = new OASISResult<IEnumerable<T1>>();
            OASISResult<IEnumerable<T1>> loadHolonsResult = await SearchHolonsAsync<T1>(searchTerm, avatarId, searchOnlyForCurrentAvatar, providerType, "STARManagerBase.SearchAsync", STARHolonType);
            return FilterResultsForVersion(avatarId, loadHolonsResult, showAllVersions, version);
        }

        public OASISResult<IEnumerable<T1>> Search(Guid avatarId, string searchTerm, bool searchOnlyForCurrentAvatar = true, bool showAllVersions = false, int version = 0, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<T1>> result = new OASISResult<IEnumerable<T1>>();
            OASISResult<IEnumerable<T1>> loadHolonsResult = SearchHolons<T1>(searchTerm, avatarId, searchOnlyForCurrentAvatar, providerType, "STARManagerBase.Search", STARHolonType);
            return FilterResultsForVersion(avatarId, loadHolonsResult, showAllVersions, version);
        }

        public async Task<OASISResult<T1>> DeleteAsync(Guid avatarId, Guid id, int version, bool softDelete = true, bool deleteDownload = true, bool deleteInstall = true, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            string errorMessage = "Error occured in DeleteAsync. Reason: ";
            OASISResult<T1> loadResult = await LoadAsync(id, avatarId, version, providerType);

            if (loadResult != null && loadResult.Result != null && !loadResult.IsError)
                result = await DeleteAsync(avatarId, loadResult.Result, version, softDelete, deleteDownload, deleteInstall, providerType);
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured loading the {STARHolonUIName} with Id {id} from the {Enum.GetName(typeof(ProviderType), providerType)} provider. Reason: {loadResult.Message}");

            return result;
        }

        public OASISResult<T1> Delete(Guid avatarId, Guid id, int version, bool softDelete = true, bool deleteDownload = true, bool deleteInstall = true, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            string errorMessage = "Error occured in Delete. Reason: ";
            OASISResult<T1> loadResult = Load(id, avatarId, version, providerType);

            if (loadResult != null && loadResult.Result != null && !loadResult.IsError)
                result = Delete(avatarId, loadResult.Result, version, softDelete, deleteDownload, deleteInstall, providerType);
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured loading the {STARHolonUIName} with Id {id} from the {Enum.GetName(typeof(ProviderType), providerType)} provider. Reason: {loadResult.Message}");

            return result;
        }

        public async Task<OASISResult<T1>> DeleteAsync(Guid avatarId, ISTARHolon oappSystemHolon, int version, bool softDelete = true, bool deleteDownload = true, bool deleteInstall = true, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            string errorMessage = "Error occured in DeleteAsync. Reason: ";

            if (oappSystemHolon.STARHolonDNA.CreatedByAvatarId != avatarId)
            {
                OASISErrorHandling.HandleError(ref result, $"Permission Denied. You did not create this {STARHolonUIName}. Error occured in DeleteSTARHolonAsync loading the {STARHolonUIName} with Id {oappSystemHolon.STARHolonDNA.CreatedByAvatarId} from the {Enum.GetName(typeof(ProviderType), providerType)} provider. Reason: The {STARHolonUIName} was not created by the Avatar with Id {avatarId}.");
                return result;
            }

            try
            {
                if (!string.IsNullOrEmpty(oappSystemHolon.STARHolonDNA.SourcePath) && Directory.Exists(oappSystemHolon.STARHolonDNA.SourcePath))
                    Directory.Delete(oappSystemHolon.STARHolonDNA.SourcePath, true);
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} Error occured attempting to delete the T folder {oappSystemHolon.STARHolonDNA.SourcePath}. PLEASE DELETE MANUALLY! Reason: {e}");
            }

            try
            {
                if (!string.IsNullOrEmpty(oappSystemHolon.STARHolonDNA.PublishedPath) && File.Exists(oappSystemHolon.STARHolonDNA.PublishedPath))
                    File.Delete(oappSystemHolon.STARHolonDNA.PublishedPath);
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} Error occured attempting to delete the T Published folder {oappSystemHolon.STARHolonDNA.PublishedPath}. PLEASE DELETE MANUALLY! Reason: {e}");
            }

            if (deleteDownload || deleteInstall)
            {
                OASISResult<T3> installedSTARHolonResult = await LoadInstalledAsync(avatarId, oappSystemHolon.STARHolonDNA.Id, version, providerType);

                if (installedSTARHolonResult != null && installedSTARHolonResult.Result != null && !installedSTARHolonResult.IsError)
                {
                    try
                    {
                        if (deleteDownload && !string.IsNullOrEmpty(installedSTARHolonResult.Result.DownloadedPath) && File.Exists(installedSTARHolonResult.Result.DownloadedPath))
                            File.Delete(installedSTARHolonResult.Result.DownloadedPath);
                    }
                    catch (Exception e)
                    {
                        OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} Error occured attempting to delete the {STARHolonUIName} Download folder {installedSTARHolonResult.Result.DownloadedPath}. PLEASE DELETE MANUALLY! Reason: {e}");
                    }

                    try
                    {
                        if (deleteInstall && !string.IsNullOrEmpty(installedSTARHolonResult.Result.InstalledPath) && Directory.Exists(installedSTARHolonResult.Result.InstalledPath))
                            Directory.Delete(installedSTARHolonResult.Result.InstalledPath, true);
                    }
                    catch (Exception e)
                    {
                        OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} Error occured attempting to delete the {STARHolonUIName} Installed folder {installedSTARHolonResult.Result.InstalledPath}. PLEASE DELETE MANUALLY! Reason: {e}");
                    }

                    if (deleteInstall)
                    {
                        OASISResult<T1> deleteInstalledSTARHolonHolonResult = await DeleteHolonAsync<T1>(installedSTARHolonResult.Result.Id, avatarId, softDelete, providerType, "STARManagerBase.DeleteAsync");

                        if (!(deleteInstalledSTARHolonHolonResult != null && deleteInstalledSTARHolonHolonResult.Result != null && !deleteInstalledSTARHolonHolonResult.IsError))
                            OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} Error occured deleting the Installed {STARHolonUIName} holon with id {installedSTARHolonResult.Result.Id} calling DeleteAsync. Reason: {deleteInstalledSTARHolonHolonResult.Message}");
                    }

                    if (deleteDownload)
                    {
                        OASISResult<T1> deleteDownloadedSTARHolonHolonResult = await DeleteHolonAsync<T1>(installedSTARHolonResult.Result.DownloadedSTARHolonId, avatarId, softDelete, providerType, "STARManagerBase.DeleteAsync");

                        if (!(deleteDownloadedSTARHolonHolonResult != null && deleteDownloadedSTARHolonHolonResult.Result != null && !deleteDownloadedSTARHolonHolonResult.IsError))
                            OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} Error occured deleting the Downloaded {STARHolonUIName} holon with id {installedSTARHolonResult.Result.DownloadedSTARHolonId} calling DeleteAsync. Reason: {deleteDownloadedSTARHolonHolonResult.Message}");
                    }
                }
            }

            OASISResult<T1> deleteHolonResult = await DeleteHolonAsync<T1>(oappSystemHolon.Id, avatarId, softDelete, providerType, "STARManagerBase.DeleteAsync");

            if (!(deleteHolonResult != null && deleteHolonResult.Result != null && !deleteHolonResult.IsError))
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured deleting the {STARHolonUIName} holon with id {oappSystemHolon.Id} calling DeleteAsync. Reason: {deleteHolonResult.Message}");

            result.Result = deleteHolonResult.Result;
            return result;
        }

        public OASISResult<T1> Delete(Guid avatarId, ISTARHolon oappSystemHolon, int version, bool softDelete = true, bool deleteDownload = true, bool deleteInstall = true, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            string errorMessage = "Error occured in Delete. Reason: ";

            if (oappSystemHolon.STARHolonDNA.CreatedByAvatarId != avatarId)
            {
                OASISErrorHandling.HandleError(ref result, $"Permission Denied. You did not create this {STARHolonUIName}. Error occured in Delete loading the {STARHolonUIName} with Id {oappSystemHolon.STARHolonDNA.CreatedByAvatarId} from the {Enum.GetName(typeof(ProviderType), providerType)} provider. Reason: The {STARHolonUIName} was not created by the Avatar with Id {avatarId}.");
                return result;
            }

            try
            {
                if (!string.IsNullOrEmpty(oappSystemHolon.STARHolonDNA.SourcePath) && Directory.Exists(oappSystemHolon.STARHolonDNA.SourcePath))
                    Directory.Delete(oappSystemHolon.STARHolonDNA.SourcePath, true);
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} Error occured attempting to delete the {STARHolonUIName} Source folder {oappSystemHolon.STARHolonDNA.SourcePath}. PLEASE DELETE MANUALLY! Reason: {e}");
            }

            try
            {
                if (!string.IsNullOrEmpty(oappSystemHolon.STARHolonDNA.PublishedPath) && File.Exists(oappSystemHolon.STARHolonDNA.PublishedPath))
                    File.Delete(oappSystemHolon.STARHolonDNA.PublishedPath);
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} Error occured attempting to delete the {STARHolonUIName} Published folder {oappSystemHolon.STARHolonDNA.PublishedPath}. PLEASE DELETE MANUALLY! Reason: {e}");
            }

            if (deleteDownload || deleteInstall)
            {
                OASISResult<T3> installedSTARHolonResult = LoadInstalled(avatarId, oappSystemHolon.STARHolonDNA.Id, version, providerType);

                if (installedSTARHolonResult != null && installedSTARHolonResult.Result != null && !installedSTARHolonResult.IsError)
                {
                    try
                    {
                        if (deleteDownload && !string.IsNullOrEmpty(installedSTARHolonResult.Result.DownloadedPath) && File.Exists(installedSTARHolonResult.Result.DownloadedPath))
                            File.Delete(installedSTARHolonResult.Result.DownloadedPath);
                    }
                    catch (Exception e)
                    {
                        OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} Error occured attempting to delete the {STARHolonUIName} Download folder {installedSTARHolonResult.Result.DownloadedPath}. PLEASE DELETE MANUALLY! Reason: {e}");
                    }

                    try
                    {
                        if (deleteInstall && !string.IsNullOrEmpty(installedSTARHolonResult.Result.InstalledPath) && Directory.Exists(installedSTARHolonResult.Result.InstalledPath))
                            Directory.Delete(installedSTARHolonResult.Result.InstalledPath, true);
                    }
                    catch (Exception e)
                    {
                        OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} Error occured attempting to delete the {STARHolonUIName} Installed folder {installedSTARHolonResult.Result.InstalledPath}. PLEASE DELETE MANUALLY! Reason: {e}");
                    }

                    if (deleteInstall)
                    {
                        OASISResult<T1> deleteInstalledSTARHolonHolonResult = DeleteHolon<T1>(installedSTARHolonResult.Result.Id, avatarId, softDelete, providerType, "STARManagerBase.Delete");

                        if (!(deleteInstalledSTARHolonHolonResult != null && deleteInstalledSTARHolonHolonResult.Result != null && !deleteInstalledSTARHolonHolonResult.IsError))
                            OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} Error occured deleting the Installed {STARHolonUIName} holon with id {installedSTARHolonResult.Result.Id} calling DeleteHolonAsync. Reason: {deleteInstalledSTARHolonHolonResult.Message}");
                    }

                    if (deleteDownload)
                    {
                        OASISResult<T1> deleteDownloadedSTARHolonHolonResult = DeleteHolon<T1>(installedSTARHolonResult.Result.DownloadedSTARHolonId, avatarId, softDelete, providerType, "STARManagerBase.Delete");

                        if (!(deleteDownloadedSTARHolonHolonResult != null && deleteDownloadedSTARHolonHolonResult.Result != null && !deleteDownloadedSTARHolonHolonResult.IsError))
                            OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} Error occured deleting the Downloaded {STARHolonUIName} holon with id {installedSTARHolonResult.Result.DownloadedSTARHolonId} calling DeleteHolonAsync. Reason: {deleteDownloadedSTARHolonHolonResult.Message}");
                    }
                }
            }

            OASISResult<T1> deleteHolonResult = DeleteHolon<T1>(avatarId, oappSystemHolon.Id, softDelete, providerType, "STARManagerBase.Delete");

            if (!(deleteHolonResult != null && deleteHolonResult.Result != null && !deleteHolonResult.IsError))
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured deleting the {STARHolonUIName} holon with id {oappSystemHolon.Id} calling DeleteHolonAsync. Reason: {deleteHolonResult.Message}");

            result.Result = deleteHolonResult.Result;
            return result;
        }
        #endregion

        /*
        #region PublishManagerBase
        public async Task<OASISResult<ISTARHolon>> PublishSTARHolonAsync(Guid STARHolonId, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<ISTARHolon> result = new OASISResult<ISTARHolon>();
            OASISResult<T> saveResult = await PublishHolonAsync<T>(STARHolonId, avatarId, "STARManagerBase.PublishSTARHolonAsync", providerType);
            result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(saveResult, result);
            result.Result = saveResult.Result;
            return result;
        }

        public OASISResult<ISTARHolon> PublishSTARHolon(Guid STARHolonId, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<ISTARHolon> result = new OASISResult<ISTARHolon>();
            OASISResult<T> saveResult = PublishHolon<T>(STARHolonId, avatarId, "STARManagerBase.PublishSTARHolon", providerType);
            result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(saveResult, result);
            result.Result = saveResult.Result;
            return result;
        }

        public async Task<OASISResult<ISTARHolon>> PublishSTARHolonAsync(ISTARHolon T, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<ISTARHolon> result = new OASISResult<ISTARHolon>();
            OASISResult<T> saveResult = await PublishHolonAsync<T>(T, avatarId, "STARManagerBase.PublishSTARHolonAsync", providerType);
            result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(saveResult, result);
            result.Result = saveResult.Result;
            return result;
        }

        public OASISResult<ISTARHolon> PublishSTARHolon(ISTARHolon T, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<ISTARHolon> result = new OASISResult<ISTARHolon>();
            OASISResult<T> saveResult = PublishHolon<T>(T, avatarId, "STARManagerBase.PublishSTARHolon", providerType);
            result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(saveResult, result);
            result.Result = saveResult.Result;
            return result;
        }

        public async Task<OASISResult<ISTARHolon>> UnpublishSTARHolonAsync(Guid STARHolonId, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<ISTARHolon> result = new OASISResult<ISTARHolon>();
            OASISResult<T> saveResult = await UnpublishHolonAsync<T>(STARHolonId, avatarId, "STARManagerBase.UnpublishSTARHolonAsync", providerType);
            result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(saveResult, result);
            result.Result = saveResult.Result;
            return result;
        }

        public OASISResult<ISTARHolon> UnpublishSTARHolon(Guid STARHolonId, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<ISTARHolon> result = new OASISResult<ISTARHolon>();
            OASISResult<T> saveResult = UnpublishHolon<T>(STARHolonId, avatarId, "STARManagerBase.UnpublishSTARHolon", providerType);
            result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(saveResult, result);
            result.Result = saveResult.Result;
            return result;
        }

        public async Task<OASISResult<ISTARHolon>> UnpublishSTARHolonAsync(ISTARHolon T, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<ISTARHolon> result = new OASISResult<ISTARHolon>();
            OASISResult<T> saveResult = await UnpublishHolonAsync<T>(T, avatarId, "STARManagerBase.UnpublishSTARHolonAsync", providerType);
            result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(saveResult, result);
            result.Result = saveResult.Result;
            return result;
        }

        public OASISResult<ISTARHolon> UnpublishSTARHolon(ISTARHolon T, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<ISTARHolon> result = new OASISResult<ISTARHolon>();
            OASISResult<T> saveResult = UnpublishHolon<T>(T, avatarId, "STARManagerBase.UnpublishSTARHolon", providerType);
            result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(saveResult, result);
            result.Result = saveResult.Result;
            return result;
        }
        #endregion*/

        public async Task<OASISResult<IEnumerable<T1>>> LoadVersionsAsync(Guid id, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<T1>> result = new OASISResult<IEnumerable<T1>>();

            //TODO: Currently we pass in 0 for version (which means the OASIS will return the latest version) but we need to be able to query for all versions (-1)
            //OASISResult<IEnumerable<T>> loadHolonsResult = await Data.LoadHolonsByMetaDataAsync<T>("STARHolonId", STARHolonId.ToString(), HolonType.T, true, true, 0, true, false, 0, HolonType.All, -1, providerType);
            OASISResult<IEnumerable<T1>> loadHolonsResult = await Data.LoadHolonsByMetaDataAsync<T1>(new Dictionary<string, string>()
            {
                { STARHolonIdName, id.ToString() },
                { "Active", "1" }
            }, MetaKeyValuePairMatchMode.All, STARHolonType, true, true, 0, true, false, 0, HolonType.All, -1, providerType);

            result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(loadHolonsResult, result);
            result.Result = loadHolonsResult.Result;
            return result;
        }

        public OASISResult<IEnumerable<T1>> LoadVersions(Guid id, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<T1>> result = new OASISResult<IEnumerable<T1>>();

            //TODO: Currently we pass in 0 for version (which means the OASIS will return the latest version) but we need to be able to query for all versions (-1)
            //OASISResult<IEnumerable<T>> loadHolonsResult = Data.LoadHolonsByMetaData<T>("STARHolonId", STARHolonId.ToString(), HolonType.T, true, true, 0, true, false, 0, HolonType.All, -1, providerType);
            OASISResult<IEnumerable<T1>> loadHolonsResult = Data.LoadHolonsByMetaData<T1>(new Dictionary<string, string>()
            {
                { STARHolonIdName, id.ToString() },
                { "Active", "1" }
            }, MetaKeyValuePairMatchMode.All, STARHolonType, true, true, 0, true, false, 0, HolonType.All, -1, providerType);

            result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(loadHolonsResult, result);
            result.Result = loadHolonsResult.Result;
            return result;
        }

        public async Task<OASISResult<T1>> LoadVersionAsync(Guid id, string version, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            OASISResult<T1> loadHolonResult = await Data.LoadHolonByMetaDataAsync<T1>(new Dictionary<string, string>()
            {
                 { STARHolonIdName, id.ToString() },
                 { "Version", version },
                 { "Active", "1" }
            }, MetaKeyValuePairMatchMode.All, STARHolonType, true, true, 0, true, 0, false, HolonType.All, providerType);

            OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(loadHolonResult, result); //Copy any possible warnings etc.
            if (loadHolonResult != null && !loadHolonResult.IsError && loadHolonResult.Result != null)
            {
                if (loadHolonResult.Result.STARHolonDNA.Version == version)
                    result.Result = loadHolonResult.Result;
                else
                    OASISErrorHandling.HandleError(ref result, $"Error occured in STARManagerBase.LoadVersionAsync. Reason: The version {version} does not exist for id {id}.");
            }
            else
                OASISErrorHandling.HandleError(ref result, $"Error occured in STARManagerBase.LoadVersionAsync. Reason: {loadHolonResult.Message}");

            return result;
        }

        public OASISResult<T1> LoadVersion(Guid id, string version, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            OASISResult<T1> loadHolonResult = Data.LoadHolonByMetaData<T1>(new Dictionary<string, string>()
            {
                 { STARHolonIdName, id.ToString() },
                 { "Version", version },
                 { "Active", "1" }
            }, MetaKeyValuePairMatchMode.All, STARHolonType, true, true, 0, true, false, HolonType.All, 0, providerType);

            OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(loadHolonResult, result); //Copy any possible warnings etc.
            if (loadHolonResult != null && !loadHolonResult.IsError && loadHolonResult.Result != null)
            {
                if (loadHolonResult.Result.STARHolonDNA.Version == version)
                    result.Result = loadHolonResult.Result;
                else
                    OASISErrorHandling.HandleError(ref result, $"Error occured in STARManagerBase.LoadVersion. Reason: The version {version} does not exist for id {id}.");
            }
            else
                OASISErrorHandling.HandleError(ref result, $"Error occured in STARManagerBase.LoadVersion. Reason: {loadHolonResult.Message}");

            return result;
        }

        //public async Task<OASISResult<T1>> EditAsync<T1, T2>(Guid id, ISTARHolonDNA newSTARHolonDNA, Guid avatarId, ProviderType providerType = ProviderType.Default) where T1 : ISTARHolon, new() where T2 : IInstalledSTARHolon, new()
        //{
        //    OASISResult<T1> result = new OASISResult<T1>();
        //    OASISResult<T1> loadResult = await LoadAsync<T1>(id, avatarId, providerType: providerType);

        //    if (loadResult != null && loadResult.Result != null && !loadResult.IsError)
        //        await EditAsync<T1, T2>(loadResult.Result, newSTARHolonDNA, avatarId, providerType);
        //    else
        //        OASISErrorHandling.HandleError(ref result, $"Error occured in STARManagerBase.EditAsync. Reason: {loadResult.Message}");

        //    return result;
        //}

        public async Task<OASISResult<T1>> EditAsync(Guid id, ISTARHolonDNA newSTARHolonDNA, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            OASISResult<T1> loadResult = await LoadAsync(id, avatarId, providerType: providerType);

            if (loadResult != null && loadResult.Result != null && !loadResult.IsError)
                await EditAsync(avatarId, loadResult.Result, newSTARHolonDNA, providerType);
            else
                OASISErrorHandling.HandleError(ref result, $"Error occured in STARManagerBase.EditAsync. Reason: {loadResult.Message}");

            return result;
        }

        public async Task<OASISResult<T1>> EditAsync(Guid avatarId, T1 holon, ISTARHolonDNA newSTARHolonDNA, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            string errorMessage = "Error occured in STARManagerBase.EditAsync. Reason: ";
            string oldPath = "";
            string newPath = "";
            string oldPublishedPath = "";
            string oldDownloadedPath = "";
            string oldInstalledPath = "";
            string oldName = "";
            string launchTarget = "";

            if (holon.Name != newSTARHolonDNA.Name)
            {
                oldName = holon.Name;
                oldPath = holon.STARHolonDNA.SourcePath;
                newPath = Path.Combine(new DirectoryInfo(holon.STARHolonDNA.SourcePath).Parent.FullName, newSTARHolonDNA.Name);
                newSTARHolonDNA.SourcePath = newPath;
                newSTARHolonDNA.LaunchTarget = newSTARHolonDNA.LaunchTarget.Replace(holon.Name, newSTARHolonDNA.Name);
                launchTarget = newSTARHolonDNA.LaunchTarget;

                holon.MetaData[STARHolonNameName] = newSTARHolonDNA.Name;

                if (!string.IsNullOrEmpty(holon.STARHolonDNA.PublishedPath))
                {
                    oldPublishedPath = holon.STARHolonDNA.PublishedPath;
                    newSTARHolonDNA.PublishedPath = oldPublishedPath.Replace(oldName, newSTARHolonDNA.Name);
                }
            }

            holon.STARHolonDNA = newSTARHolonDNA;
            holon.Name = newSTARHolonDNA.Name;
            holon.Description = newSTARHolonDNA.Description;

            if (!string.IsNullOrEmpty(newPath) && !string.IsNullOrEmpty(oldPath))
            {
                try
                {
                    if (Directory.Exists(oldPath))
                        Directory.Move(oldPath, newPath);
                }
                catch (Exception e)
                {
                    OASISErrorHandling.HandleWarning(ref result, $"An error occured attempting to rename the {STARHolonUIName} folder from {oldPath} to {newPath}. Reason: {e}.");
                    CLIEngine.ShowErrorMessage("PLEASE RENAME THIS FOLDER MANUALLY, THANK YOU!");
                }

                if (!string.IsNullOrEmpty(newSTARHolonDNA.PublishedPath))
                {
                    try
                    {
                        if (!string.IsNullOrEmpty(oldPublishedPath) && File.Exists(oldPublishedPath))
                            File.Move(oldPublishedPath, newSTARHolonDNA.PublishedPath);
                    }
                    catch (Exception e)
                    {
                        OASISErrorHandling.HandleWarning(ref result, $"An error occured attempting to rename the {STARHolonUIName} published file from {oldPublishedPath} to {newSTARHolonDNA.PublishedPath}. Reason: {e}.");
                        CLIEngine.ShowErrorMessage("PLEASE RENAME THIS FOLDER MANUALLY, THANK YOU!");
                    }
                }
            }

            OASISResult<T1> saveResult = await SaveAsync(avatarId, holon, providerType: providerType);

            if (saveResult != null && !saveResult.IsError && saveResult.Result != null)
            {
                OASISResult<IEnumerable<T1>> holonsResult = await LoadVersionsAsync(newSTARHolonDNA.Id, providerType);

                if (holonsResult != null && holonsResult.Result != null && !holonsResult.IsError)
                {
                    foreach (T1 holonVersion in holonsResult.Result)
                    {
                        //No need to update the version we already updated above.
                        if (holonVersion.STARHolonDNA.Version == holon.STARHolonDNA.Version)
                            continue;

                        holonVersion.STARHolonDNA = newSTARHolonDNA;
                        holonVersion.Name = newSTARHolonDNA.Name;
                        holonVersion.Description = newSTARHolonDNA.Description;
                        holonVersion.MetaData["STARHolonName"] = newSTARHolonDNA.Name;

                        oldPath = holonVersion.STARHolonDNA.SourcePath;
                        newPath = Path.Combine(new DirectoryInfo(oldPath).Parent.FullName, newSTARHolonDNA.Name);
                        holonVersion.STARHolonDNA.SourcePath = newPath;
                        holonVersion.STARHolonDNA.LaunchTarget = launchTarget;

                        if (!string.IsNullOrEmpty(holonVersion.STARHolonDNA.PublishedPath))
                        {
                            oldPublishedPath = holonVersion.STARHolonDNA.PublishedPath;
                            //holonVersion.STARHolonDNA.PublishedPath = Path.Combine(new DirectoryInfo(oldPublishedPath).FullName, newSTARHolonDNA.Name);
                            newSTARHolonDNA.PublishedPath = oldPublishedPath.Replace(oldName, newSTARHolonDNA.Name);
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
                                OASISErrorHandling.HandleWarning(ref result, $"An error occured attempting to rename the {STARHolonUIName} folder from {oldPath} to {newPath}. Reason: {e}.");
                                CLIEngine.ShowErrorMessage("PLEASE RENAME THIS FOLDER MANUALLY, THANK YOU!");
                            }
                        }

                        if (!string.IsNullOrEmpty(oldPublishedPath))
                        {
                            try
                            {
                                if (File.Exists(oldPublishedPath))
                                    File.Move(oldPublishedPath, holonVersion.STARHolonDNA.PublishedPath);
                            }
                            catch (Exception e)
                            {
                                OASISErrorHandling.HandleWarning(ref result, $"An error occured attempting to rename the {STARHolonUIName} published file from {oldPublishedPath} to {newSTARHolonDNA.PublishedPath}. Reason: {e}.");
                                CLIEngine.ShowErrorMessage("PLEASE RENAME THIS FOLDER MANUALLY, THANK YOU!");
                            }
                        }

                        OASISResult<T1> templateSaveResult = await SaveAsync(avatarId, holonVersion, providerType);

                        if (templateSaveResult != null && templateSaveResult.Result != null && !templateSaveResult.IsError)
                        {

                        }
                        else
                            OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} Error occured calling SaveAsync updating the STARHolonDNA for {STARHolonUIName} with Id {holonVersion.Id} for provider {Enum.GetName(typeof(ProviderType), providerType)}. Reason: {templateSaveResult.Message}");
                    }
                }
                else
                    OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} Error occured updating the STARHolonDNA for all {STARHolonUIName} versions caused by an error in LoadVersionsAsync. Reason: {holonsResult.Message}");


                OASISResult<IEnumerable<T3>> installedTemplatesResult = await ListInstalledAsync(avatarId, providerType);

                if (installedTemplatesResult != null && installedTemplatesResult.Result != null && !installedTemplatesResult.IsError)
                {
                    foreach (T3 installedHolon in installedTemplatesResult.Result)
                    {
                        installedHolon.STARHolonDNA = newSTARHolonDNA;
                        installedHolon.Name = installedHolon.Name.Replace(oldName, newSTARHolonDNA.Name);
                        installedHolon.Description = installedHolon.Description.Replace(oldName, newSTARHolonDNA.Name);
                        installedHolon.MetaData[STARHolonNameName] = newSTARHolonDNA.Name;

                        oldPath = installedHolon.STARHolonDNA.SourcePath;
                        newPath = Path.Combine(new DirectoryInfo(oldPath).Parent.FullName, newSTARHolonDNA.Name);
                        installedHolon.STARHolonDNA.SourcePath = newPath;
                        installedHolon.STARHolonDNA.LaunchTarget = launchTarget;

                        if (!string.IsNullOrEmpty(installedHolon.STARHolonDNA.PublishedPath))
                        {
                            oldPublishedPath = installedHolon.STARHolonDNA.PublishedPath;
                            installedHolon.STARHolonDNA.PublishedPath = Path.Combine(new DirectoryInfo(oldPublishedPath).Parent.FullName, string.Concat(newSTARHolonDNA.Name, "_v", installedHolon.STARHolonDNA.Version, ".", STARHolonFileExtention));
                            //holonVersion.STARHolonDNA.PublishedPath = oldPublishedPath.Replace(oldName, newSTARHolonDNA.Name);
                        }

                        if (!string.IsNullOrEmpty(installedHolon.DownloadedPath))
                        {
                            oldDownloadedPath = installedHolon.DownloadedPath;
                            //holonVersion.DownloadedPath = Path.Combine(new DirectoryInfo(oldDownloadedPath).FullName, newSTARHolonDNA.Name);
                            installedHolon.DownloadedPath = oldDownloadedPath.Replace(oldName, newSTARHolonDNA.Name);
                        }

                        if (!string.IsNullOrEmpty(installedHolon.InstalledPath))
                        {
                            oldInstalledPath = installedHolon.InstalledPath;
                            installedHolon.InstalledPath = Path.Combine(new DirectoryInfo(oldInstalledPath).Parent.FullName, newSTARHolonDNA.Name);
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
                                OASISErrorHandling.HandleWarning(ref result, $"An error occured attempting to rename the {STARHolonUIName} folder from {oldPath} to {newPath}. Reason: {e}.");
                                CLIEngine.ShowErrorMessage("PLEASE RENAME THIS FOLDER MANUALLY, THANK YOU!");
                            }
                        }

                        if (!string.IsNullOrEmpty(oldPublishedPath))
                        {
                            try
                            {
                                if (File.Exists(oldPublishedPath) && oldPublishedPath != installedHolon.STARHolonDNA.PublishedPath)
                                    File.Move(oldPublishedPath, installedHolon.STARHolonDNA.PublishedPath);
                            }
                            catch (Exception e)
                            {
                                OASISErrorHandling.HandleWarning(ref result, $"An error occured attempting to rename the {STARHolonUIName} published file from {oldPublishedPath} to {newSTARHolonDNA.PublishedPath}. Reason: {e}.");
                                CLIEngine.ShowErrorMessage("PLEASE RENAME THIS FOLDER MANUALLY, THANK YOU!");
                            }
                        }

                        OASISResult<T3> installedOPPSystemHolonSaveResult = await SaveAsync(avatarId, installedHolon, providerType);

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
                                    OASISErrorHandling.HandleWarning(ref result, $"An error occured attempting to rename the {STARHolonUIName} downloaded file from {oldDownloadedPath} to {installedHolon.DownloadedPath}. Reason: {e}.");
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
                                    OASISErrorHandling.HandleWarning(ref result, $"An error occured attempting to rename the {STARHolonUIName} installed folder from {oldInstalledPath} to {installedHolon.InstalledPath}. Reason: {e}.");
                                    CLIEngine.ShowErrorMessage("PLEASE RENAME THIS FOLDER MANUALLY, THANK YOU!");
                                }
                            }
                        }
                        else
                            OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} Error occured updating the STARHolonDNA for Installed {STARHolonUIName} with Id {installedHolon.Id} for provider {Enum.GetName(typeof(ProviderType), providerType)}. Reason: {installedOPPSystemHolonSaveResult.Message}");
                    }
                }
                else
                    OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} Error occured updating the STARHolonDNA for all Installed {STARHolonUIName} versions caused by an error in ListInstalledSTARHolonsAsync. Reason: {holonsResult.Message}");


                result.Result = saveResult.Result;
                result.IsSaved = true;
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured saving the {STARHolonUIName} with Id {newSTARHolonDNA.Id} from the {Enum.GetName(typeof(ProviderType), providerType)} provider. Reason: {saveResult.Message}");

            return result;
        }

        public async Task<OASISResult<T1>> PublishAsync(Guid avatarId, string fullPathToSource, string launchTarget, string fullPathToPublishTo = "", bool edit = false, bool registerOnSTARNET = true, bool generateBinary = true, bool uploadToCloud = false, ProviderType providerType = ProviderType.Default, ProviderType binaryProviderType = ProviderType.IPFSOASIS)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            ISTARHolonDNA STARHolonDNA = null;

            OASISResult<T1> validateResult = await BeginPublishAsync(avatarId, fullPathToSource, launchTarget, fullPathToPublishTo, edit, providerType);

            if (validateResult != null && validateResult.Result != null && !validateResult.IsError)
            {
                STARHolonDNA = validateResult.Result.STARHolonDNA;
                string publishedFileName = string.Concat(STARHolonDNA.Name, "_v", STARHolonDNA.Version, ".", STARHolonFileExtention);

                STARHolonDNA.PublishedOnSTARNET = registerOnSTARNET && (binaryProviderType != ProviderType.None || uploadToCloud);

                if (generateBinary)
                {
                    STARHolonDNA.PublishedPath = Path.Combine(fullPathToPublishTo, publishedFileName);
                    STARHolonDNA.PublishedToCloud = registerOnSTARNET && uploadToCloud;
                    STARHolonDNA.PublishedProviderType = binaryProviderType;
                }

                WriteDNA(STARHolonDNA, fullPathToSource);
                OnPublishStatusChanged?.Invoke(this, new STARHolonPublishStatusEventArgs() { STARHolonDNA = STARHolonDNA, Status = STARHolonPublishStatus.Compressing });

                if (generateBinary)
                {
                    OASISResult<bool> compressedResult = GenerateCompressedFile(fullPathToSource, STARHolonDNA.PublishedPath);

                    if (!(compressedResult != null && compressedResult.Result != null && !compressedResult.IsError))
                    {
                        result.Message = compressedResult.Message;
                        result.IsError = true;
                        return result;
                    }
                }

                //TODO: Currently the filesize will NOT be in the compressed .STARHolon file because we dont know the size before we create it! ;-) We would need to compress it twice or edit the compressed file after to update the STARHolonDNA inside it...
                if (!string.IsNullOrEmpty(STARHolonDNA.PublishedPath) && File.Exists(STARHolonDNA.PublishedPath))
                    STARHolonDNA.FileSize = new FileInfo(STARHolonDNA.PublishedPath).Length;

                WriteDNA(STARHolonDNA, fullPathToSource);
                validateResult.Result.STARHolonDNA = STARHolonDNA;

                if (registerOnSTARNET)
                {
                    if (uploadToCloud)
                        result = await UploadToCloudAsync<T1>(STARHolonDNA, publishedFileName, registerOnSTARNET, binaryProviderType);

                    if (binaryProviderType != ProviderType.None)
                    {
                        OASISResult<T1> uploadToOASISResult = await UploadToOASISAsync(avatarId, STARHolonDNA, STARHolonDNA.PublishedPath, registerOnSTARNET, uploadToCloud, binaryProviderType);

                        if (uploadToOASISResult != null && uploadToOASISResult.Result != null && !uploadToOASISResult.IsError)
                            result.Result = uploadToOASISResult.Result;
                        else
                            OASISErrorHandling.HandleWarning(ref result, $" Error occured calling UploadToOASISAsync. Reason: {uploadToOASISResult.Message}");
                    }
                    else
                        STARHolonDNA.PublishedProviderType = ProviderType.None;
                }

                OASISResult<T1> finalResult = await FininalizePublishAsync(avatarId, validateResult.Result, edit, providerType);
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(finalResult, result);
                result.Result = finalResult.Result;
            }

            return result;
        }

        public OASISResult<T1> Publish(Guid avatarId, string fullPathToSource, string launchTarget, string fullPathToPublishTo = "", bool edit = false, bool registerOnSTARNET = true, bool generateBinary = true, bool uploadToCloud = false, ProviderType providerType = ProviderType.Default, ProviderType binaryProviderType = ProviderType.IPFSOASIS)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            ISTARHolonDNA STARHolonDNA = null;

            OASISResult<T1> validateResult = BeginPublish(avatarId, fullPathToSource, launchTarget, fullPathToPublishTo, edit, providerType);

            if (validateResult != null && validateResult.Result != null && !validateResult.IsError)
            {
                STARHolonDNA = validateResult.Result.STARHolonDNA;
                string publishedFileName = string.Concat(STARHolonDNA.Name, "_v", STARHolonDNA.Version, ".", STARHolonFileExtention);

                STARHolonDNA.PublishedOnSTARNET = registerOnSTARNET && (binaryProviderType != ProviderType.None || uploadToCloud);

                if (generateBinary)
                {
                    STARHolonDNA.PublishedPath = Path.Combine(fullPathToPublishTo, publishedFileName);
                    STARHolonDNA.PublishedToCloud = registerOnSTARNET && uploadToCloud;
                    STARHolonDNA.PublishedProviderType = binaryProviderType;
                }

                WriteDNA(STARHolonDNA, fullPathToSource);
                OnPublishStatusChanged?.Invoke(this, new STARHolonPublishStatusEventArgs() { STARHolonDNA = STARHolonDNA, Status = STARHolonPublishStatus.Compressing });

                if (generateBinary)
                {
                    OASISResult<bool> compressedResult = GenerateCompressedFile(fullPathToSource, STARHolonDNA.PublishedPath);

                    if (!(compressedResult != null && compressedResult.Result != null && !compressedResult.IsError))
                    {
                        result.Message = compressedResult.Message;
                        result.IsError = true;
                        return result;
                    }
                }

                //TODO: Currently the filesize will NOT be in the compressed .STARHolon file because we dont know the size before we create it! ;-) We would need to compress it twice or edit the compressed file after to update the STARHolonDNA inside it...
                if (!string.IsNullOrEmpty(STARHolonDNA.PublishedPath) && File.Exists(STARHolonDNA.PublishedPath))
                    STARHolonDNA.FileSize = new FileInfo(STARHolonDNA.PublishedPath).Length;

                WriteDNA(STARHolonDNA, fullPathToSource);
                validateResult.Result.STARHolonDNA = STARHolonDNA;

                if (registerOnSTARNET)
                {
                    if (uploadToCloud)
                        result = UploadToCloud<T1>(STARHolonDNA, publishedFileName, registerOnSTARNET, binaryProviderType);

                    if (binaryProviderType != ProviderType.None)
                    {
                        OASISResult<T1> uploadToOASISResult = UploadToOASIS(avatarId, STARHolonDNA, STARHolonDNA.PublishedPath, registerOnSTARNET, uploadToCloud, binaryProviderType);

                        if (uploadToOASISResult != null && uploadToOASISResult.Result != null && !uploadToOASISResult.IsError)
                            result.Result = uploadToOASISResult.Result;
                        else
                            OASISErrorHandling.HandleWarning(ref result, $" Error occured calling UploadToOASIS. Reason: {uploadToOASISResult.Message}");
                    }
                    else
                        STARHolonDNA.PublishedProviderType = ProviderType.None;
                }

                OASISResult<T1> finalResult = FininalizePublish(avatarId, validateResult.Result, edit, providerType);
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(finalResult, result);
                result.Result = finalResult.Result;
            }

            return result;
        }

        public OASISResult<bool> GenerateCompressedFile(string sourcePath, string destinationPath)
        {
            OASISResult<bool> result = new OASISResult<bool>();

            try
            {
                if (File.Exists(destinationPath))
                    File.Delete(destinationPath);

                ZipFile.CreateFromDirectory(sourcePath, destinationPath);
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, sourcePath + " could not be compressed to " + destinationPath + ". Reason: " + e.Message);
            }

            return result;
        }

        //TODO: Come back to this, was going to call this for publishing and installing to make sure the DNA hadn't been changed on the disk, but maybe we want to allow this? Not sure, needs more thought...
        //private async OASISResult<bool> IsSTARHolonDNAValidAsync(ISTARHolonDNA STARHolonDNA)
        //{
        //    OASISResult<ISTARHolon> STARHolonResult = await LoadSTARHolonAsync(STARHolonDNA.STARHolonId);

        //    if (STARHolonResult != null && STARHolonResult.Result != null && !STARHolonResult.IsError)
        //    {
        //        ISTARHolonDNA originalDNA =  JsonSerializer.Deserialize<ISTARHolonDNA>(STARHolonResult.Result.MetaData["STARHolonDNA"].ToString());

        //        if (originalDNA != null)
        //        {
        //            if (originalDNA.GenesisType != STARHolonDNA.GenesisType ||
        //                originalDNA.STARHolonType != STARHolonDNA.STARHolonType ||
        //                originalDNA.CelestialBodyType != STARHolonDNA.CelestialBodyType ||
        //                originalDNA.CelestialBodyId != STARHolonDNA.CelestialBodyId ||
        //                originalDNA.CelestialBodyName != STARHolonDNA.CelestialBodyName ||
        //                originalDNA.CreatedByAvatarId != STARHolonDNA.CreatedByAvatarId ||
        //                originalDNA.CreatedByAvatarUsername != STARHolonDNA.CreatedByAvatarUsername ||
        //                originalDNA.CreatedOn != STARHolonDNA.CreatedOn ||
        //                originalDNA.Description != STARHolonDNA.Description ||
        //                originalDNA.IsActive != STARHolonDNA.IsActive ||
        //                originalDNA.LaunchTarget != STARHolonDNA.LaunchTarget ||
        //                originalDNA. != STARHolonDNA.LaunchTarget ||

        //        }
        //    }
        //}

        public async Task<OASISResult<T1>> BeginPublishAsync(Guid avatarId, string fullPathToSource, string fullPathToPublishTo, string launchTarget, bool edit, ProviderType providerType)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            string userName = "Unknown";

            try
            {
                OASISResult<IOAPPDNA> readSTARHolonDNAResult = await ReadDNAFromSourceOrInstallFolderAsync<IOAPPDNA>(fullPathToSource);

                if (readSTARHolonDNAResult != null && !readSTARHolonDNAResult.IsError && readSTARHolonDNAResult.Result != null)
                {
                    //OAPPDNA = readSTARHolonDNAResult.Result;
                    OnPublishStatusChanged?.Invoke(this, new STARHolonPublishStatusEventArgs() { STARHolonDNA = readSTARHolonDNAResult.Result, Status = STARHolonPublishStatus.Packaging });
                    OASISResult<IAvatar> loadAvatarResult = await AvatarManager.Instance.LoadAvatarAsync(avatarId, false, true, providerType);

                    if (loadAvatarResult != null && loadAvatarResult.Result != null && !loadAvatarResult.IsError)
                    {
                        userName = loadAvatarResult.Result.Username;

                        //Load latest version.
                        OASISResult<T1> loadOAPPResult = await LoadAsync(avatarId, readSTARHolonDNAResult.Result.Id);

                        if (loadOAPPResult != null && loadOAPPResult.Result != null && !loadOAPPResult.IsError)
                        {
                            if (loadOAPPResult.Result.STARHolonDNA.CreatedByAvatarId == avatarId)
                            {
                                OASISResult<bool> validateVersionResult = ValidateVersion(readSTARHolonDNAResult.Result.Version, loadOAPPResult.Result.STARHolonDNA.Version, fullPathToSource, loadOAPPResult.Result.STARHolonDNA.PublishedOn == DateTime.MinValue, edit);

                                if (validateVersionResult != null && validateVersionResult.Result && !validateVersionResult.IsError)
                                {
                                    //TODO: Maybe add check to make sure the DNA has not been tampered with?
                                    loadOAPPResult.Result.STARHolonDNA.Version = readSTARHolonDNAResult.Result.Version; //Set the new version set in the DNA (JSON file).
                                    IOAPPDNA OAPPDNA = (IOAPPDNA)loadOAPPResult.Result.STARHolonDNA; //Make sure it has not been tampered with by using the stored version.

                                    if (!edit)
                                    {
                                        OAPPDNA.VersionSequence++;
                                        OAPPDNA.NumberOfVersions++;
                                    }

                                    OAPPDNA.LaunchTarget = launchTarget;
                                    result.Result = loadOAPPResult.Result;

                                    if (string.IsNullOrEmpty(fullPathToPublishTo))
                                        fullPathToPublishTo = Path.Combine(fullPathToSource, "Published");

                                    if (!Directory.Exists(fullPathToPublishTo))
                                        Directory.CreateDirectory(fullPathToPublishTo);

                                    if (!edit)
                                    {
                                        OAPPDNA.PublishedOn = DateTime.Now;
                                        OAPPDNA.PublishedByAvatarId = avatarId;
                                        OAPPDNA.PublishedByAvatarUsername = userName;
                                    }
                                    else
                                    {
                                        OAPPDNA.ModifiedOn = DateTime.Now;
                                        OAPPDNA.ModifiedByAvatarId = avatarId;
                                        OAPPDNA.ModifiedByAvatarUsername = userName;
                                    }

                                    result.Result.STARHolonDNA = OAPPDNA;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"Error occured in STARManagerBase.BeginPublishAsync. Reason: {e.Message}");
            }

            return result;
        }

        public OASISResult<T1> BeginPublish(Guid avatarId, string fullPathToSource, string fullPathToPublishTo, string launchTarget, bool edit, ProviderType providerType)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            string userName = "Unknown";

            try
            {
                OASISResult<IOAPPDNA> readSTARHolonDNAResult = ReadDNAFromSourceOrInstallFolder<IOAPPDNA>(fullPathToSource);

                if (readSTARHolonDNAResult != null && !readSTARHolonDNAResult.IsError && readSTARHolonDNAResult.Result != null)
                {
                    //OAPPDNA = readSTARHolonDNAResult.Result;
                    OnPublishStatusChanged?.Invoke(this, new STARHolonPublishStatusEventArgs() { STARHolonDNA = readSTARHolonDNAResult.Result, Status = STARHolonPublishStatus.Packaging });
                    OASISResult<IAvatar> loadAvatarResult = AvatarManager.Instance.LoadAvatar(avatarId, false, true, providerType);

                    if (loadAvatarResult != null && loadAvatarResult.Result != null && !loadAvatarResult.IsError)
                    {
                        userName = loadAvatarResult.Result.Username;

                        //Load latest version.
                        OASISResult<T1> loadOAPPResult = Load(avatarId, readSTARHolonDNAResult.Result.Id);

                        if (loadOAPPResult != null && loadOAPPResult.Result != null && !loadOAPPResult.IsError)
                        {
                            if (loadOAPPResult.Result.STARHolonDNA.CreatedByAvatarId == avatarId)
                            {
                                OASISResult<bool> validateVersionResult = ValidateVersion(readSTARHolonDNAResult.Result.Version, loadOAPPResult.Result.STARHolonDNA.Version, fullPathToSource, loadOAPPResult.Result.STARHolonDNA.PublishedOn == DateTime.MinValue, edit);

                                if (validateVersionResult != null && validateVersionResult.Result && !validateVersionResult.IsError)
                                {
                                    //TODO: Maybe add check to make sure the DNA has not been tampered with?
                                    loadOAPPResult.Result.STARHolonDNA.Version = readSTARHolonDNAResult.Result.Version; //Set the new version set in the DNA (JSON file).
                                    IOAPPDNA OAPPDNA = (IOAPPDNA)loadOAPPResult.Result.STARHolonDNA; //Make sure it has not been tampered with by using the stored version.

                                    if (!edit)
                                    {
                                        OAPPDNA.VersionSequence++;
                                        OAPPDNA.NumberOfVersions++;
                                    }

                                    OAPPDNA.LaunchTarget = launchTarget;
                                    result.Result = loadOAPPResult.Result;

                                    if (string.IsNullOrEmpty(fullPathToPublishTo))
                                        fullPathToPublishTo = Path.Combine(fullPathToSource, "Published");

                                    if (!Directory.Exists(fullPathToPublishTo))
                                        Directory.CreateDirectory(fullPathToPublishTo);

                                    if (!edit)
                                    {
                                        OAPPDNA.PublishedOn = DateTime.Now;
                                        OAPPDNA.PublishedByAvatarId = avatarId;
                                        OAPPDNA.PublishedByAvatarUsername = userName;
                                    }
                                    else
                                    {
                                        OAPPDNA.ModifiedOn = DateTime.Now;
                                        OAPPDNA.ModifiedByAvatarId = avatarId;
                                        OAPPDNA.ModifiedByAvatarUsername = userName;
                                    }

                                    result.Result.STARHolonDNA = OAPPDNA;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"Error occured in STARManagerBase.BeginPublish. Reason: {e.Message}");
            }

            return result;
        }

        public async Task<OASISResult<T>> UploadToCloudAsync<T>(ISTARHolonDNA STARHolonDNA, string publishedSTARHolonFileName, bool registerOnSTARNET, ProviderType binaryProviderType)
        {
            OASISResult<T> result = new OASISResult<T>();

            try
            {
                OnPublishStatusChanged?.Invoke(this, new STARHolonPublishStatusEventArgs() { STARHolonDNA = STARHolonDNA, Status = STARHolonPublishStatus.Uploading });
                StorageClient storage = await StorageClient.CreateAsync();
                //var bucket = storage.CreateBucket("oasis", "STARHolons");

                // set minimum chunksize just to see progress updating
                var uploadObjectOptions = new UploadObjectOptions
                {
                    ChunkSize = UploadObjectOptions.MinimumChunkSize
                };

                var progressReporter = new Progress<Google.Apis.Upload.IUploadProgress>(OnUploadProgress);
                using (var fileStream = File.OpenRead(STARHolonDNA.PublishedPath))
                {
                    _fileLength = fileStream.Length;
                    _progress = 0;

                    await storage.UploadObjectAsync(STARHolonGoogleBucket, publishedSTARHolonFileName, "", fileStream, uploadObjectOptions, progress: progressReporter);
                }

                _progress = 100;
                OnUploadStatusChanged?.Invoke(this, new STARHolonUploadProgressEventArgs() { Progress = _progress, Status = STARHolonUploadStatus.Uploading });
                CLIEngine.DisposeProgressBar(false);
                Console.WriteLine("");

                //HttpClient client = new HttpClient();
                //string pinataApiKey = "33e4469830a51af0171b";
                //string pinataSecretApiKey = "ff57367b2b125bf5f06f79b30b466890c84eed101c12af064459d88d8bb8d8a0\r\nJWT: eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJ1c2VySW5mb3JtYXRpb24iOnsiaWQiOiIzMGI3NjllNS1hMjJmLTQxN2UtOWEwYi1mZTQ2NzE5MjgzNzgiLCJlbWFpbCI6ImRhdmlkZWxsYW1zQGhvdG1haWwuY29tIiwiZW1haWxfdmVyaWZpZWQiOnRydWUsInBpbl9wb2xpY3kiOnsicmVnaW9ucyI6W3siZGVzaXJlZFJlcGxpY2F0aW9uQ291bnQiOjEsImlkIjoiRlJBMSJ9LHsiZGVzaXJlZFJlcGxpY2F0aW9uQ291bnQiOjEsImlkIjoiTllDMSJ9XSwidmVyc2lvbiI6MX0sIm1mYV9lbmFibGVkIjpmYWxzZSwic3RhdHVzIjoiQUNUSVZFIn0sImF1dGhlbnRpY2F0aW9uVHlwZSI6InNjb3BlZEtleSIsInNjb3BlZEtleUtleSI6IjMzZTQ0Njk4MzBhNTFhZjAxNzFiIiwic2NvcGVkS2V5U2VjcmV0IjoiZmY1NzM2N2IyYjEyNWJmNWYwNmY3OWIzMGI0NjY4OTBjODRlZWQxMDFjMTJhZjA2NDQ1OWQ4OGQ4YmI4ZDhhMCIsImV4cCI6MTc3Mzc4NDAzNX0.L-6_BPMsvhN3Es72Q5lZAFKpBEDF9kEibOGdWd_PxHs";
                //string pinataUrl = "https://api.pinata.cloud/pinning/pinFileToIPFS";
                //string filePath = STARHolonDNA.PublishedPath;

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
                //PinFileResponse pinFileResponse = await pinataClient.PinFileToIPFSAsync(STARHolonDNA.PublishedPath);

                //if (pinFileResponse != null && !string.IsNullOrEmpty(pinFileResponse.IpfsHash))
                //{
                //    STARHolonDNA.PinataIPFSHash = pinFileResponse.IpfsHash;
                //    STARHolonDNA.STARHolonPublishedOnSTARNET = true;
                //    STARHolonDNA.STARHolonPublishedToPinata = true;
                //}
                //else
                //{
                //    OASISErrorHandling.HandleWarning(ref result, $"An error occured publishing the T to Pinata.");
                //    STARHolonDNA.STARHolonPublishedOnSTARNET = registerOnSTARNET && oappBinaryProviderType != ProviderType.None;
                //}
            }
            catch (Exception e)
            {
                CLIEngine.DisposeProgressBar(false);
                Console.WriteLine("");

                OASISErrorHandling.HandleWarning(ref result, $"An error occured publishing the {STARHolonUIName} to cloud storage. Reason: {e}");
                STARHolonDNA.PublishedOnSTARNET = registerOnSTARNET && binaryProviderType != ProviderType.None;
                STARHolonDNA.PublishedToCloud = false;
            }

            return result;
        }

        public OASISResult<T> UploadToCloud<T>(ISTARHolonDNA STARHolonDNA, string publishedSTARHolonFileName, bool registerOnSTARNET, ProviderType binaryProviderType)
        {
            OASISResult<T> result = new OASISResult<T>();

            try
            {
                OnPublishStatusChanged?.Invoke(this, new STARHolonPublishStatusEventArgs() { STARHolonDNA = STARHolonDNA, Status = STARHolonPublishStatus.Uploading });
                StorageClient storage = StorageClient.Create();
                //var bucket = storage.CreateBucket("oasis", "STARHolons");

                // set minimum chunksize just to see progress updating
                var uploadObjectOptions = new UploadObjectOptions
                {
                    ChunkSize = UploadObjectOptions.MinimumChunkSize
                };

                var progressReporter = new Progress<Google.Apis.Upload.IUploadProgress>(OnUploadProgress);
                using (var fileStream = File.OpenRead(STARHolonDNA.PublishedPath))
                {
                    _fileLength = fileStream.Length;
                    _progress = 0;

                    storage.UploadObject(STARHolonGoogleBucket, publishedSTARHolonFileName, "", fileStream, uploadObjectOptions, progress: progressReporter);
                }

                _progress = 100;
                OnUploadStatusChanged?.Invoke(this, new STARHolonUploadProgressEventArgs() { Progress = _progress, Status = STARHolonUploadStatus.Uploading });
                CLIEngine.DisposeProgressBar(false);
                Console.WriteLine("");

                //HttpClient client = new HttpClient();
                //string pinataApiKey = "33e4469830a51af0171b";
                //string pinataSecretApiKey = "ff57367b2b125bf5f06f79b30b466890c84eed101c12af064459d88d8bb8d8a0\r\nJWT: eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJ1c2VySW5mb3JtYXRpb24iOnsiaWQiOiIzMGI3NjllNS1hMjJmLTQxN2UtOWEwYi1mZTQ2NzE5MjgzNzgiLCJlbWFpbCI6ImRhdmlkZWxsYW1zQGhvdG1haWwuY29tIiwiZW1haWxfdmVyaWZpZWQiOnRydWUsInBpbl9wb2xpY3kiOnsicmVnaW9ucyI6W3siZGVzaXJlZFJlcGxpY2F0aW9uQ291bnQiOjEsImlkIjoiRlJBMSJ9LHsiZGVzaXJlZFJlcGxpY2F0aW9uQ291bnQiOjEsImlkIjoiTllDMSJ9XSwidmVyc2lvbiI6MX0sIm1mYV9lbmFibGVkIjpmYWxzZSwic3RhdHVzIjoiQUNUSVZFIn0sImF1dGhlbnRpY2F0aW9uVHlwZSI6InNjb3BlZEtleSIsInNjb3BlZEtleUtleSI6IjMzZTQ0Njk4MzBhNTFhZjAxNzFiIiwic2NvcGVkS2V5U2VjcmV0IjoiZmY1NzM2N2IyYjEyNWJmNWYwNmY3OWIzMGI0NjY4OTBjODRlZWQxMDFjMTJhZjA2NDQ1OWQ4OGQ4YmI4ZDhhMCIsImV4cCI6MTc3Mzc4NDAzNX0.L-6_BPMsvhN3Es72Q5lZAFKpBEDF9kEibOGdWd_PxHs";
                //string pinataUrl = "https://api.pinata.cloud/pinning/pinFileToIPFS";
                //string filePath = STARHolonDNA.PublishedPath;

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
                //PinFileResponse pinFileResponse = await pinataClient.PinFileToIPFSAsync(STARHolonDNA.PublishedPath);

                //if (pinFileResponse != null && !string.IsNullOrEmpty(pinFileResponse.IpfsHash))
                //{
                //    STARHolonDNA.PinataIPFSHash = pinFileResponse.IpfsHash;
                //    STARHolonDNA.STARHolonPublishedOnSTARNET = true;
                //    STARHolonDNA.STARHolonPublishedToPinata = true;
                //}
                //else
                //{
                //    OASISErrorHandling.HandleWarning(ref result, $"An error occured publishing the T to Pinata.");
                //    STARHolonDNA.STARHolonPublishedOnSTARNET = registerOnSTARNET && oappBinaryProviderType != ProviderType.None;
                //}
            }
            catch (Exception e)
            {
                CLIEngine.DisposeProgressBar(false);
                Console.WriteLine("");

                OASISErrorHandling.HandleWarning(ref result, $"An error occured publishing the {STARHolonUIName} to cloud storage. Reason: {e}");
                STARHolonDNA.PublishedOnSTARNET = registerOnSTARNET && binaryProviderType != ProviderType.None;
                STARHolonDNA.PublishedToCloud = false;
            }

            return result;
        }

        public async Task<OASISResult<T1>> UploadToOASISAsync(Guid avatarId, ISTARHolonDNA STARHolonDNA, string publishedPath, bool registerOnSTARNET, bool uploadToCloud, ProviderType binaryProviderType)
        {
            OASISResult<T1> result = new OASISResult<T1>();

            result.Result.PublishedSTARHolon = File.ReadAllBytes(publishedPath);

            //TODO: We could use HoloOASIS and other large file storage providers in future...
            OASISResult<T1> saveLargeSTARHolonResult = await SaveAsync(avatarId, result.Result, binaryProviderType);

            if (saveLargeSTARHolonResult != null && !saveLargeSTARHolonResult.IsError && saveLargeSTARHolonResult.Result != null)
            {
                result.Result = saveLargeSTARHolonResult.Result;
                result.IsSaved = true;
            }
            else
            {
                OASISErrorHandling.HandleWarning(ref result, $" Error occured saving the published {STARHolonUIName} binary to STARNET using the {binaryProviderType} provider. Reason: {saveLargeSTARHolonResult.Message}");
                STARHolonDNA.PublishedOnSTARNET = registerOnSTARNET && uploadToCloud;
                STARHolonDNA.PublishedProviderType = ProviderType.None;
            }

            return result;
        }

        public OASISResult<T1> UploadToOASIS(Guid avatarId, ISTARHolonDNA STARHolonDNA, string publishedPath, bool registerOnSTARNET, bool uploadToCloud, ProviderType binaryProviderType)
        {
            OASISResult<T1> result = new OASISResult<T1>();

            result.Result.PublishedSTARHolon = File.ReadAllBytes(publishedPath);

            //TODO: We could use HoloOASIS and other large file storage providers in future...
            OASISResult<T1> saveLargeSTARHolonResult = Save(avatarId, result.Result, binaryProviderType);

            if (saveLargeSTARHolonResult != null && !saveLargeSTARHolonResult.IsError && saveLargeSTARHolonResult.Result != null)
            {
                result.Result = saveLargeSTARHolonResult.Result;
                result.IsSaved = true;
            }
            else
            {
                OASISErrorHandling.HandleWarning(ref result, $" Error occured saving the published {STARHolonUIName} binary to STARNET using the {binaryProviderType} provider. Reason: {saveLargeSTARHolonResult.Message}");
                STARHolonDNA.PublishedOnSTARNET = registerOnSTARNET && uploadToCloud;
                STARHolonDNA.PublishedProviderType = ProviderType.None;
            }

            return result;
        }

        public async Task<OASISResult<T1>> FininalizePublishAsync(Guid avatarId, T1 holon, bool edit, ProviderType providerType)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            string errorMessage = "";

            //If its not the first version.
            if (holon.STARHolonDNA.Version != "1.0.0" && !edit)
            {
                //If the ID has not been set then store the original id now.
                if (!holon.MetaData.ContainsKey(STARHolonIdName))
                    holon.MetaData[STARHolonIdName] = holon.Id;

                holon.MetaData["Version"] = holon.STARHolonDNA.Version;
                holon.MetaData["VersionSequence"] = holon.STARHolonDNA.VersionSequence;

                //Blank fields so it creates a new version.
                holon.Id = Guid.Empty;
                holon.ProviderUniqueStorageKey.Clear();
                holon.CreatedDate = DateTime.MinValue;
                holon.ModifiedDate = DateTime.MinValue;
                holon.CreatedByAvatarId = Guid.Empty;
                holon.ModifiedByAvatarId = Guid.Empty;
                holon.STARHolonDNA.Downloads = 0;
                holon.STARHolonDNA.Installs = 0;
            }

            OASISResult<T1> saveSTARHolonResult = await SaveAsync(avatarId, holon, providerType);

            if (saveSTARHolonResult != null && !saveSTARHolonResult.IsError && saveSTARHolonResult.Result != null)
            {
                saveSTARHolonResult = await UpdateNumberOfVersionCountsAsync(avatarId, saveSTARHolonResult, errorMessage, providerType);
                result.IsSaved = true;
                result.Result = saveSTARHolonResult.Result; //TODO:Check if this is needed?

                if (holon.STARHolonDNA.STARODKVersion != OASISBootLoader.OASISBootLoader.STARODKVersion)
                    OASISErrorHandling.HandleWarning(ref result, $" The STAR ODK Version {holon.STARHolonDNA.STARODKVersion} does not match the current version {OASISBootLoader.OASISBootLoader.STARODKVersion}. This may lead to issues, it is recommended to make sure the versions match.");

                if (holon.STARHolonDNA.OASISVersion != OASISBootLoader.OASISBootLoader.OASISVersion)
                    OASISErrorHandling.HandleWarning(ref result, $" The OASIS Version {holon.STARHolonDNA.OASISVersion} does not match the current version {OASISBootLoader.OASISBootLoader.OASISVersion}. This may lead to issues, it is recommended to make sure the versions match.");

                if (holon.STARHolonDNA.COSMICVersion != OASISBootLoader.OASISBootLoader.COSMICVersion)
                    OASISErrorHandling.HandleWarning(ref result, $" The COSMIC Version {holon.STARHolonDNA.COSMICVersion} does not match the current version {OASISBootLoader.OASISBootLoader.COSMICVersion}. This may lead to issues, it is recommended to make sure the versions match.");

                if (result.IsWarning)
                    result.Message = $"{STARHolonUIName} successfully published but there were {result.WarningCount} warnings:\n\n {OASISResultHelper.BuildInnerMessageError(result.InnerMessages)}";
                else
                    result.Message = $"{STARHolonUIName} Successfully Published";

                OnPublishStatusChanged?.Invoke(this, new STARHolonPublishStatusEventArgs() { STARHolonDNA = holon.STARHolonDNA, Status = STARHolonPublishStatus.Published });
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling SaveSTARHolonAsync on {Enum.GetName(typeof(ProviderType), providerType)} provider. Reason: {saveSTARHolonResult.Message}");

            return result;
        }

        public OASISResult<T1> FininalizePublish(Guid avatarId, T1 holon, bool edit, ProviderType providerType)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            string errorMessage = "";

            //If its not the first version.
            if (holon.STARHolonDNA.Version != "1.0.0" && !edit)
            {
                //If the ID has not been set then store the original id now.
                if (!holon.MetaData.ContainsKey(STARHolonIdName))
                    holon.MetaData[STARHolonIdName] = holon.Id;

                holon.MetaData["Version"] = holon.STARHolonDNA.Version;
                holon.MetaData["VersionSequence"] = holon.STARHolonDNA.VersionSequence;

                //Blank fields so it creates a new version.
                holon.Id = Guid.Empty;
                holon.ProviderUniqueStorageKey.Clear();
                holon.CreatedDate = DateTime.MinValue;
                holon.ModifiedDate = DateTime.MinValue;
                holon.CreatedByAvatarId = Guid.Empty;
                holon.ModifiedByAvatarId = Guid.Empty;
                holon.STARHolonDNA.Downloads = 0;
                holon.STARHolonDNA.Installs = 0;
            }

            OASISResult<T1> saveSTARHolonResult = Save(avatarId, holon, providerType);

            if (saveSTARHolonResult != null && !saveSTARHolonResult.IsError && saveSTARHolonResult.Result != null)
            {
                saveSTARHolonResult = UpdateNumberOfVersionCounts(avatarId, saveSTARHolonResult, errorMessage, providerType);
                result.IsSaved = true;
                result.Result = saveSTARHolonResult.Result; //TODO:Check if this is needed?

                if (holon.STARHolonDNA.STARODKVersion != OASISBootLoader.OASISBootLoader.STARODKVersion)
                    OASISErrorHandling.HandleWarning(ref result, $" The STAR ODK Version {holon.STARHolonDNA.STARODKVersion} does not match the current version {OASISBootLoader.OASISBootLoader.STARODKVersion}. This may lead to issues, it is recommended to make sure the versions match.");

                if (holon.STARHolonDNA.OASISVersion != OASISBootLoader.OASISBootLoader.OASISVersion)
                    OASISErrorHandling.HandleWarning(ref result, $" The OASIS Version {holon.STARHolonDNA.OASISVersion} does not match the current version {OASISBootLoader.OASISBootLoader.OASISVersion}. This may lead to issues, it is recommended to make sure the versions match.");

                if (holon.STARHolonDNA.COSMICVersion != OASISBootLoader.OASISBootLoader.COSMICVersion)
                    OASISErrorHandling.HandleWarning(ref result, $" The COSMIC Version {holon.STARHolonDNA.COSMICVersion} does not match the current version {OASISBootLoader.OASISBootLoader.COSMICVersion}. This may lead to issues, it is recommended to make sure the versions match.");

                if (result.IsWarning)
                    result.Message = $"{STARHolonUIName} successfully published but there were {result.WarningCount} warnings:\n\n {OASISResultHelper.BuildInnerMessageError(result.InnerMessages)}";
                else
                    result.Message = $"{STARHolonUIName} Successfully Published";

                OnPublishStatusChanged?.Invoke(this, new STARHolonPublishStatusEventArgs() { STARHolonDNA = holon.STARHolonDNA, Status = STARHolonPublishStatus.Published });
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling SaveSTARHolonAsync on {Enum.GetName(typeof(ProviderType), providerType)} provider. Reason: {saveSTARHolonResult.Message}");

            return result;
        }

        public async Task<OASISResult<T1>> UnpublishAsync(Guid avatarId, T1 holon, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            string errorMessage = "Error occured in UnpublishAsync. Reason: ";

            holon.STARHolonDNA.PublishedOn = DateTime.MinValue;
            holon.STARHolonDNA.PublishedByAvatarId = Guid.Empty;
            holon.STARHolonDNA.PublishedByAvatarUsername = "";
            //T.STARHolonDNA.IsActive = false;
            holon.MetaData["Active"] = "0";

            OASISResult<T1> oappResult = await SaveAsync(avatarId, holon, providerType);

            if (oappResult != null && oappResult.Result != null && !oappResult.IsError)
            {
                result.Result = oappResult.Result; //ConvertSTARHolonToSTARHolonDNA(T);
                result.Message = $"{STARHolonUIName} Unpublished";
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured saving the {STARHolonUIName} with the SaveSTARHolonAsync method, reason: {oappResult.Message}");

            return result;
        }

        public OASISResult<T1> Unpublish(Guid avatarId, T1 holon, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            string errorMessage = "Error occured in Unpublish. Reason: ";

            holon.STARHolonDNA.PublishedOn = DateTime.MinValue;
            holon.STARHolonDNA.PublishedByAvatarId = Guid.Empty;
            holon.STARHolonDNA.PublishedByAvatarUsername = "";
            //T.STARHolonDNA.IsActive = false;
            holon.MetaData["Active"] = "0";

            OASISResult<T1> oappResult = Save(avatarId, holon, providerType);

            if (oappResult != null && oappResult.Result != null && !oappResult.IsError)
            {
                result.Result = oappResult.Result; //ConvertSTARHolonToSTARHolonDNA(T);
                result.Message = $"{STARHolonUIName} Unpublished";
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured saving the {STARHolonUIName} with the Save method, reason: {oappResult.Message}");

            return result;
        }

        public async Task<OASISResult<T1>> UnpublishAsync(Guid avatarId, Guid STARHolonId, int version, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            OASISResult<T1> loadResult = await LoadAsync(STARHolonId, avatarId, version, providerType);

            if (loadResult != null && loadResult.Result != null && !loadResult.IsError)
                result = await UnpublishAsync(avatarId, loadResult.Result, providerType);
            else
                OASISErrorHandling.HandleError(ref result, $"Error occured in UnpublishAsync loading the {STARHolonUIName} with the LoadAsync method, reason: {loadResult.Message}");

            return result;
        }

        public OASISResult<T1> Unpublish(Guid avatarId, Guid STARHolonId, int version, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            OASISResult<T1> loadResult = Load(STARHolonId, avatarId, version, providerType);

            if (loadResult != null && loadResult.Result != null && !loadResult.IsError)
                result = Unpublish(avatarId, loadResult.Result, providerType);
            else
                OASISErrorHandling.HandleError(ref result, $"Error occured in UnpublishUnpublish loading the {STARHolonUIName} with the Load method, reason: {loadResult.Message}");

            return result;
        }

        public async Task<OASISResult<T1>> UnpublishAsync(Guid avatarId, ISTARHolonDNA STARHolonDNA, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            OASISResult<T1> oappResult = await LoadAsync(STARHolonDNA.Id, avatarId, STARHolonDNA.VersionSequence, providerType);
            string errorMessage = "Error occured in UnpublishSTARHolonAsync. Reason: ";

            if (oappResult != null && oappResult.Result != null && !oappResult.IsError)
                result = await UnpublishAsync(avatarId, oappResult.Result, providerType);
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured loading the {STARHolonUIName} with the LoadAsync method, reason: {oappResult.Message}");

            return result;
        }

        public OASISResult<T1> Unpublish(Guid avatarId, ISTARHolonDNA STARHolonDNA, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            OASISResult<T1> oappResult = Load(STARHolonDNA.Id, avatarId, STARHolonDNA.VersionSequence, providerType);
            string errorMessage = "Error occured in Unpublish. Reason: ";

            if (oappResult != null && oappResult.Result != null && !oappResult.IsError)
                result = Unpublish(avatarId, oappResult.Result, providerType);
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured loading the {STARHolonUIName} with the Load method, reason: {oappResult.Message}");

            return result;
        }

        public async Task<OASISResult<T1>> RepublishAsync(Guid avatarId, T1 holon, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            string errorMessage = "Error occured in RepublishAsync. Reason: ";

            OASISResult<IAvatar> avatarResult = await AvatarManager.Instance.LoadAvatarAsync(avatarId, false, true, providerType);

            if (avatarResult != null && avatarResult.Result != null && !avatarResult.IsError)
            {
                holon.STARHolonDNA.PublishedOn = DateTime.Now;
                holon.STARHolonDNA.PublishedByAvatarId = avatarId;
                holon.STARHolonDNA.PublishedByAvatarUsername = avatarResult.Result.Username;
                //T.STARHolonDNA.IsActive = true;
                holon.MetaData["Active"] = "1";

                OASISResult<T1> oappResult = await SaveAsync(avatarId, holon, providerType);

                if (oappResult != null && oappResult.Result != null && !oappResult.IsError)
                {
                    result.Result = oappResult.Result; //ConvertSTARHolonToSTARHolonDNA(T);
                    result.Message = $"{STARHolonUIName} Republished";
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured saving the {STARHolonUIName} with the SaveAsync method, reason: {oappResult.Message}");
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured loading the Avatar with the LoadAvatar method, reason: {avatarResult.Message}");

            return result;
        }

        public OASISResult<T1> Republish(Guid avatarId, T1 holon, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            string errorMessage = "Error occured in Republish. Reason: ";

            OASISResult<IAvatar> avatarResult = AvatarManager.Instance.LoadAvatar(avatarId, false, true, providerType);

            if (avatarResult != null && avatarResult.Result != null && !avatarResult.IsError)
            {
                holon.STARHolonDNA.PublishedOn = DateTime.Now;
                holon.STARHolonDNA.PublishedByAvatarId = avatarId;
                holon.STARHolonDNA.PublishedByAvatarUsername = avatarResult.Result.Username;
                //T.STARHolonDNA.IsActive = true;
                holon.MetaData["Active"] = "1";

                OASISResult<T1> oappResult = Save(avatarId, holon, providerType);

                if (oappResult != null && oappResult.Result != null && !oappResult.IsError)
                {
                    result.Result = oappResult.Result; //ConvertSTARHolonToSTARHolonDNA(T);
                    result.Message = $"{STARHolonUIName} Republished";
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured saving the {STARHolonUIName} with the Save method, reason: {oappResult.Message}");
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured loading the Avatar with the LoadAvatar method, reason: {avatarResult.Message}");

            return result;
        }

        public async Task<OASISResult<T1>> RepublishAsync(Guid avatarId, ISTARHolonDNA STARHolonDNA, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            OASISResult<T1> oappResult = await LoadAsync(STARHolonDNA.Id, avatarId, STARHolonDNA.VersionSequence, providerType);
            string errorMessage = "Error occured in RepublishAsync. Reason: ";

            if (oappResult != null && oappResult.Result != null && !oappResult.IsError)
                result = await RepublishAsync(avatarId, oappResult.Result, providerType);
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured loading the {STARHolonUIName} with the LoadAsync method, reason: {oappResult.Message}");

            return result;
        }

        public OASISResult<T1> Republish(Guid avatarId, ISTARHolonDNA STARHolonDNA, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            OASISResult<T1> oappResult = Load(STARHolonDNA.Id, avatarId, STARHolonDNA.VersionSequence, providerType);
            string errorMessage = "Error occured in Republish. Reason: ";

            if (oappResult != null && oappResult.Result != null && !oappResult.IsError)
                result = Republish(avatarId, oappResult.Result, providerType);
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured loading the {STARHolonUIName} with the LoadSTARHolon method, reason: {oappResult.Message}");

            return result;
        }

        public async Task<OASISResult<T1>> RepublishAsync(Guid avatarId, Guid STARHolonId, int version, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            OASISResult<T1> loadResult = await LoadAsync(STARHolonId, avatarId, version, providerType);

            if (loadResult != null && loadResult.Result != null && !loadResult.IsError)
                result = await RepublishAsync(avatarId, loadResult.Result, providerType);
            else
                OASISErrorHandling.HandleError(ref result, $"Error occured in RepublishAsync loading the {STARHolonUIName} with the LoadAsync method, reason: {loadResult.Message}");

            return result;
        }

        public OASISResult<T1> Republish(Guid avatarId, Guid STARHolonId, int version, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            OASISResult<T1> loadResult = Load(STARHolonId, avatarId, version, providerType);

            if (loadResult != null && loadResult.Result != null && !loadResult.IsError)
                result = Republish(avatarId, loadResult.Result, providerType);
            else
                OASISErrorHandling.HandleError(ref result, $"Error occured in Republish loading the {STARHolonUIName} with the Load method, reason: {loadResult.Message}");

            return result;
        }

        public async Task<OASISResult<T1>> DeactivateAsync(Guid avatarId, T1 holon, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            string errorMessage = "Error occured in DeactivateAsync. Reason: ";

            //T.STARHolonDNA.IsActive = false;
            holon.MetaData["Active"] = "0";

            OASISResult<T1> oappResult = await SaveAsync(avatarId, holon, providerType);

            if (oappResult != null && oappResult.Result != null && !oappResult.IsError)
            {
                result.Result = oappResult.Result; //ConvertSTARHolonToSTARHolonDNA(T);
                result.Message = $"{STARHolonUIName} Deactivated";
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured saving the {STARHolonUIName} with the SaveSTARHolonAsync method, reason: {oappResult.Message}");

            return result;
        }

        public OASISResult<T1> Deactivate(Guid avatarId, T1 holon, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            string errorMessage = "Error occured in Deactivate. Reason: ";

            //T.STARHolonDNA.IsActive = false;
            holon.MetaData["Active"] = "0";

            OASISResult<T1> oappResult = Save(avatarId, holon, providerType);

            if (oappResult != null && oappResult.Result != null && !oappResult.IsError)
            {
                result.Result = oappResult.Result; //ConvertSTARHolonToSTARHolonDNA(T);
                result.Message = $"{STARHolonUIName} Deactivated";
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured saving the {STARHolonUIName} with the SaveSTARHolon method, reason: {oappResult.Message}");

            return result;
        }

        public async Task<OASISResult<T1>> DeactivateAsync(Guid avatarId, Guid STARHolonId, int version, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            OASISResult<T1> loadResult = await LoadAsync(STARHolonId, avatarId, version, providerType);

            if (loadResult != null && loadResult.Result != null && !loadResult.IsError)
                result = await DeactivateAsync(avatarId, loadResult.Result, providerType);
            else
                OASISErrorHandling.HandleError(ref result, $"Error occured in DeactivateAsync loading the T with the LoadAsync method, reason: {loadResult.Message}");

            return result;
        }

        public OASISResult<T1> Deactivate(Guid avatarId, Guid STARHolonId, int version, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            OASISResult<T1> loadResult = Load(STARHolonId, avatarId, version, providerType);

            if (loadResult != null && loadResult.Result != null && !loadResult.IsError)
                result = Deactivate(avatarId, loadResult.Result, providerType);
            else
                OASISErrorHandling.HandleError(ref result, $"Error occured in Deactivate loading the T with the LoadSTARHolon method, reason: {loadResult.Message}");

            return result;
        }

        public async Task<OASISResult<T1>> DeactivateAsync(Guid avatarId, ISTARHolonDNA STARHolonDNA, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            OASISResult<T1> oappResult = await LoadAsync(STARHolonDNA.Id, avatarId, STARHolonDNA.VersionSequence, providerType);
            string errorMessage = "Error occured in DeactivateAsync. Reason: ";

            if (oappResult != null && oappResult.Result != null && !oappResult.IsError)
                result = await DeactivateAsync(avatarId, oappResult.Result, providerType);
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured loading the {STARHolonUIName} with the LoadSTARHolonAsync method, reason: {oappResult.Message}");

            return result;
        }

        public OASISResult<T1> Deactivate(Guid avatarId, ISTARHolonDNA STARHolonDNA, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            OASISResult<T1> oappResult = Load(STARHolonDNA.Id, avatarId, STARHolonDNA.VersionSequence, providerType);
            string errorMessage = "Error occured in Deactivate. Reason: ";

            if (oappResult != null && oappResult.Result != null && !oappResult.IsError)
                result = Deactivate(avatarId, oappResult.Result, providerType);
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured loading the {STARHolonUIName} with the LoadSTARHolon method, reason: {oappResult.Message}");

            return result;
        }

        public async Task<OASISResult<T1>> ActivateAsync(Guid avatarId, T1 holon, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            string errorMessage = "Error occured in ActivateAsync. Reason: ";

            OASISResult<IAvatar> avatarResult = await AvatarManager.Instance.LoadAvatarAsync(avatarId, false, true, providerType);

            if (avatarResult != null && avatarResult.Result != null && !avatarResult.IsError)
            {
                //T.STARHolonDNA.IsActive = true;
                holon.MetaData["Active"] = "1";

                OASISResult<T1> oappResult = await SaveAsync(avatarId, holon, providerType);

                if (oappResult != null && oappResult.Result != null && !oappResult.IsError)
                {
                    result.Result = oappResult.Result; //ConvertSTARHolonToSTARHolonDNA(T);
                    result.Message = $"{STARHolonUIName} Activated";
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured saving the {STARHolonUIName} with the SaveSTARHolonAsync method, reason: {oappResult.Message}");
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured loading the Avatar with the LoadAvatar method, reason: {avatarResult.Message}");

            return result;
        }

        public OASISResult<T1> Activate(Guid avatarId, T1 holon, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            string errorMessage = "Error occured in Activate. Reason: ";

            OASISResult<IAvatar> avatarResult = AvatarManager.Instance.LoadAvatar(avatarId, false, true, providerType);

            if (avatarResult != null && avatarResult.Result != null && !avatarResult.IsError)
            {
                //T.STARHolonDNA.IsActive = true;
                holon.MetaData["Active"] = "1";

                OASISResult<T1> oappResult = Save(avatarId, holon, providerType);

                if (oappResult != null && oappResult.Result != null && !oappResult.IsError)
                {
                    result.Result = oappResult.Result; //ConvertSTARHolonToSTARHolonDNA(T);
                    result.Message = $"{STARHolonUIName} Activated";
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured saving the {STARHolonUIName} with the Save method, reason: {oappResult.Message}");
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured loading the Avatar with the LoadAvatar method, reason: {avatarResult.Message}");

            return result;
        }

        public async Task<OASISResult<T1>> ActivateAsync(Guid avatarId, ISTARHolonDNA STARHolonDNA, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            OASISResult<T1> oappResult = await LoadAsync(avatarId, STARHolonDNA.Id, STARHolonDNA.VersionSequence, providerType);
            string errorMessage = "Error occured in ActivateAsync. Reason: ";

            if (oappResult != null && oappResult.Result != null && !oappResult.IsError)
                result = await ActivateAsync(avatarId, oappResult.Result, providerType);
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured loading the {STARHolonUIName} with the LoadSTARHolonAsync method, reason: {oappResult.Message}");

            return result;
        }

        public OASISResult<T1> Activate(Guid avatarId, ISTARHolonDNA STARHolonDNA, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            OASISResult<T1> oappResult = Load(avatarId, STARHolonDNA.Id, STARHolonDNA.VersionSequence, providerType);
            string errorMessage = "Error occured in Activate. Reason: ";

            if (oappResult != null && oappResult.Result != null && !oappResult.IsError)
                result = Activate(avatarId, oappResult.Result, providerType);
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured loading the {STARHolonUIName} with the Load method, reason: {oappResult.Message}");

            return result;
        }

        public async Task<OASISResult<T1>> ActivateAsync(Guid avatarId, Guid id, int version, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            OASISResult<T1> loadResult = await LoadAsync(avatarId, id, version, providerType);

            if (loadResult != null && loadResult.Result != null && !loadResult.IsError)
                result = await ActivateAsync(avatarId, loadResult.Result, providerType);
            else
                OASISErrorHandling.HandleError(ref result, $"Error occured in ActivateAsync loading the T with the LoadAsync method, reason: {loadResult.Message}");

            return result;
        }

        public OASISResult<T1> Activate(Guid avatarId, Guid id, int version, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            OASISResult<T1> loadResult = Load(id, avatarId, version, providerType);

            if (loadResult != null && loadResult.Result != null && !loadResult.IsError)
                result = Activate(avatarId, loadResult.Result, providerType);
            else
                OASISErrorHandling.HandleError(ref result, $"Error occured in Activate loading the {STARHolonUIName} with the Load method, reason: {loadResult.Message}");

            return result;
        }

        public async Task<OASISResult<T2>> DownloadAsync(Guid avatarId, T1 holon, string fullDownloadPath, bool reInstall = false, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T2> result = new OASISResult<T2>();
            string errorMessage = "Error occured in STARManagerBase.DownloadAsync. Reason: ";
            T2 downloadedSTARHolon = default;

            try
            {
                if (!fullDownloadPath.Contains(string.Concat(".", STARHolonFileExtention)))
                    fullDownloadPath = Path.Combine(fullDownloadPath, string.Concat(holon.Name, "_v", holon.STARHolonDNA.Version, ".", STARHolonFileExtention));

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
                            _fileLength = holon.STARHolonDNA.FileSize;

                        _progress = 0;

                        string publishedSTARHolonFileName = string.Concat(holon.Name, "_v", holon.STARHolonDNA.Version, ".", STARHolonFileExtention);
                        OnInstallStatusChanged?.Invoke(this, new STARHolonInstallStatusEventArgs() { STARHolonDNA = holon.STARHolonDNA, Status = STARHolonInstallStatus.Downloading });
                        await storage.DownloadObjectAsync(STARHolonGoogleBucket, publishedSTARHolonFileName, fileStream, downloadObjectOptions, progress: progressReporter);

                        _progress = 100;
                        OnDownloadStatusChanged?.Invoke(this, new STARHolonDownloadProgressEventArgs() { Progress = _progress, Status = STARHolonDownloadStatus.Downloading });
                        CLIEngine.DisposeProgressBar(false);
                        Console.WriteLine("");
                        fileStream.Close();
                    }

                    OASISResult<IAvatar> avatarResult = await AvatarManager.Instance.LoadAvatarAsync(avatarId, false, true, providerType);

                    if (avatarResult != null && !avatarResult.IsError && avatarResult.Result != null)
                    {
                        if (!reInstall)
                        {
                            holon.STARHolonDNA.Downloads++;

                            downloadedSTARHolon = new T2()
                            {
                                Name = string.Concat(holon.STARHolonDNA.Name, " Downloaded Holon"),
                                Description = string.Concat(holon.STARHolonDNA.Description, " Downloaded Holon"),
                                STARHolonDNA = holon.STARHolonDNA,
                                DownloadedBy = avatarId,
                                DownloadedByAvatarUsername = avatarResult.Result.Username,
                                DownloadedOn = DateTime.Now,
                                DownloadedPath = fullDownloadPath
                            };

                            await UpdateDownloadCountsAsync(avatarId, downloadedSTARHolon, holon.STARHolonDNA, result, errorMessage, providerType);

                            downloadedSTARHolon.MetaData[STARHolonIdName] = holon.STARHolonDNA.Id.ToString();
                            downloadedSTARHolon.MetaData[STARHolonDNAJSONName] = JsonSerializer.Serialize(downloadedSTARHolon.STARHolonDNA);
                            OASISResult<T2> saveResult = await downloadedSTARHolon.SaveAsync<T2>();

                            if (!(saveResult != null && saveResult.Result != null && !saveResult.IsError))
                                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling SaveAsync method on downloadedSTARHolon. Reason: {saveResult.Message}");
                        }
                        else
                        {
                            OASISResult<IEnumerable<T2>> downloadedSTARHolonResult = await Data.LoadHolonsByMetaDataAsync<T2>(STARHolonIdName, holon.STARHolonDNA.Id.ToString(), HolonType.All, true, true, 0, true, false, 0, HolonType.All, 0, providerType);

                            if (downloadedSTARHolonResult != null && !downloadedSTARHolonResult.IsError && downloadedSTARHolonResult.Result != null)
                            {
                                downloadedSTARHolon = downloadedSTARHolonResult.Result.FirstOrDefault();
                                downloadedSTARHolon.DownloadedOn = DateTime.Now;
                                downloadedSTARHolon.DownloadedBy = avatarId;
                                downloadedSTARHolon.DownloadedByAvatarUsername = avatarResult.Result.Username;
                                downloadedSTARHolon.DownloadedPath = fullDownloadPath;

                                OASISResult<T2> saveResult = await downloadedSTARHolon.SaveAsync<T2>();

                                if (!(saveResult != null && saveResult.Result != null && !saveResult.IsError))
                                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling SaveAsync method on downloadedSTARHolon. Reason: {saveResult.Message}");
                            }
                            else
                                OASISErrorHandling.HandleWarning(ref result, $"The {STARHolonUIName} was downloaded but the DownloadedSTARHolon could not be found. Reason: {downloadedSTARHolonResult.Message}");
                        }
                    }
                    else
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadAvatarAsync method. Reason: {avatarResult.Message}");


                    if (!result.IsError)
                    {
                        result.Result = downloadedSTARHolon;
                        OASISResult<T1> oappSaveResult = await SaveAsync(avatarId, holon, providerType);

                        if (oappSaveResult != null && !oappSaveResult.IsError && oappSaveResult.Result != null)
                        {
                            if (result.InnerMessages.Count > 0)
                                result.Message = $"{STARHolonUIName} successfully downloaded but there were {result.WarningCount} warnings:\n\n {OASISResultHelper.BuildInnerMessageError(result.InnerMessages)}";
                            else
                                result.Message = $"{STARHolonUIName} Successfully Downloaded";
                        }
                        else
                            OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling SaveSTARHolonAsync method. Reason: {oappSaveResult.Message}");
                    }
                }
                catch (Exception e)
                {
                    CLIEngine.DisposeProgressBar(false);
                    Console.WriteLine("");
                    OASISErrorHandling.HandleError(ref result, $"An error occured downloading the {STARHolonUIName} from cloud storage. Reason: {e}");
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
            //    OnSTARHolonDownloadStatusChanged?.Invoke(this, new STARHolonDownloadProgressEventArgs { STARHolonDNA = T.STARHolonDNA, Status = Enums.STARHolonDownloadStatus.Error, ErrorMessage = result.Message });

            return result;
        }

        public OASISResult<T2> Download(Guid avatarId, T1 holon, string fullDownloadPath, bool reInstall = false, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T2> result = new OASISResult<T2>();
            string errorMessage = "Error occured in STARManagerBase.Download. Reason: ";
            T2 downloadedSTARHolon = default;

            try
            {
                if (!fullDownloadPath.Contains(string.Concat(".", STARHolonFileExtention)))
                    fullDownloadPath = Path.Combine(fullDownloadPath, string.Concat(holon.Name, "_v", holon.STARHolonDNA.Version, ".", STARHolonFileExtention));

                if (File.Exists(fullDownloadPath))
                    File.Delete(fullDownloadPath);

                try
                {
                    StorageClient storage = StorageClient.Create();

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
                            _fileLength = holon.STARHolonDNA.FileSize;

                        _progress = 0;

                        string publishedSTARHolonFileName = string.Concat(holon.Name, "_v", holon.STARHolonDNA.Version, ".", STARHolonFileExtention);
                        OnInstallStatusChanged?.Invoke(this, new STARHolonInstallStatusEventArgs() { STARHolonDNA = holon.STARHolonDNA, Status = STARHolonInstallStatus.Downloading });
                        storage.DownloadObject(STARHolonGoogleBucket, publishedSTARHolonFileName, fileStream, downloadObjectOptions, progress: progressReporter);

                        _progress = 100;
                        OnDownloadStatusChanged?.Invoke(this, new STARHolonDownloadProgressEventArgs() { Progress = _progress, Status = STARHolonDownloadStatus.Downloading });
                        CLIEngine.DisposeProgressBar(false);
                        Console.WriteLine("");
                        fileStream.Close();
                    }

                    OASISResult<IAvatar> avatarResult = AvatarManager.Instance.LoadAvatar(avatarId, false, true, providerType);

                    if (avatarResult != null && !avatarResult.IsError && avatarResult.Result != null)
                    {
                        if (!reInstall)
                        {
                            holon.STARHolonDNA.Downloads++;

                            downloadedSTARHolon = new T2()
                            {
                                Name = string.Concat(holon.STARHolonDNA.Name, " Downloaded Holon"),
                                Description = string.Concat(holon.STARHolonDNA.Description, " Downloaded Holon"),
                                STARHolonDNA = holon.STARHolonDNA,
                                DownloadedBy = avatarId,
                                DownloadedByAvatarUsername = avatarResult.Result.Username,
                                DownloadedOn = DateTime.Now,
                                DownloadedPath = fullDownloadPath
                            };

                            UpdateDownloadCounts(avatarId, downloadedSTARHolon, holon.STARHolonDNA, result, errorMessage, providerType);

                            downloadedSTARHolon.MetaData[STARHolonIdName] = holon.STARHolonDNA.Id.ToString();
                            downloadedSTARHolon.MetaData[STARHolonDNAJSONName] = JsonSerializer.Serialize(downloadedSTARHolon.STARHolonDNA);
                            OASISResult<T2> saveResult = downloadedSTARHolon.Save<T2>();

                            if (!(saveResult != null && saveResult.Result != null && !saveResult.IsError))
                                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling SaveAsync method on downloadedSTARHolon. Reason: {saveResult.Message}");
                        }
                        else
                        {
                            OASISResult<IEnumerable<T2>> downloadedSTARHolonResult = Data.LoadHolonsByMetaData<T2>(STARHolonIdName, holon.STARHolonDNA.Id.ToString(), HolonType.All, true, true, 0, true, false, 0, HolonType.All, 0, providerType);

                            if (downloadedSTARHolonResult != null && !downloadedSTARHolonResult.IsError && downloadedSTARHolonResult.Result != null)
                            {
                                downloadedSTARHolon = downloadedSTARHolonResult.Result.FirstOrDefault();
                                downloadedSTARHolon.DownloadedOn = DateTime.Now;
                                downloadedSTARHolon.DownloadedBy = avatarId;
                                downloadedSTARHolon.DownloadedByAvatarUsername = avatarResult.Result.Username;
                                downloadedSTARHolon.DownloadedPath = fullDownloadPath;

                                OASISResult<T2> saveResult = downloadedSTARHolon.Save<T2>();

                                if (!(saveResult != null && saveResult.Result != null && !saveResult.IsError))
                                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling SaveAsync method on downloadedSTARHolon. Reason: {saveResult.Message}");
                            }
                            else
                                OASISErrorHandling.HandleWarning(ref result, $"The {STARHolonUIName} was downloaded but the DownloadedSTARHolon could not be found. Reason: {downloadedSTARHolonResult.Message}");
                        }
                    }
                    else
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadAvatarAsync method. Reason: {avatarResult.Message}");


                    if (!result.IsError)
                    {
                        result.Result = downloadedSTARHolon;
                        OASISResult<T1> oappSaveResult = Save(avatarId, holon, providerType);

                        if (oappSaveResult != null && !oappSaveResult.IsError && oappSaveResult.Result != null)
                        {
                            if (result.InnerMessages.Count > 0)
                                result.Message = $"{STARHolonUIName} successfully downloaded but there were {result.WarningCount} warnings:\n\n {OASISResultHelper.BuildInnerMessageError(result.InnerMessages)}";
                            else
                                result.Message = $"{STARHolonUIName} Successfully Downloaded";
                        }
                        else
                            OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling SaveSTARHolonAsync method. Reason: {oappSaveResult.Message}");
                    }
                }
                catch (Exception e)
                {
                    CLIEngine.DisposeProgressBar(false);
                    Console.WriteLine("");
                    OASISErrorHandling.HandleError(ref result, $"An error occured downloading the {STARHolonUIName} from cloud storage. Reason: {e}");
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
            //    OnSTARHolonDownloadStatusChanged?.Invoke(this, new STARHolonDownloadProgressEventArgs { STARHolonDNA = T.STARHolonDNA, Status = Enums.STARHolonDownloadStatus.Error, ErrorMessage = result.Message });

            return result;
        }

        public async Task<OASISResult<T3>> DownloadAndInstallAsync(Guid avatarId, T1 holon, string fullInstallPath, string fullDownloadPath = "", bool createSTARHolonDirectory = true, bool reInstall = false, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T3> result = new OASISResult<T3>();
            string errorMessage = "Error occured in STARManagerBase.DownloadAndInstallAsync. Reason: ";
            bool isFullDownloadPathTemp = false;

            try
            {
                if (string.IsNullOrEmpty(fullDownloadPath))
                {
                    string tempPath = Path.GetTempPath();
                    fullDownloadPath = Path.Combine(tempPath, string.Concat(holon.Name, ".", STARHolonFileExtention));
                    isFullDownloadPathTemp = true;
                }

                if (File.Exists(fullDownloadPath))
                    File.Delete(fullDownloadPath);

                if (holon.PublishedSTARHolon != null)
                {
                    await File.WriteAllBytesAsync(fullDownloadPath, holon.PublishedSTARHolon);
                    result = await InstallAsync(avatarId, fullDownloadPath, fullInstallPath, createSTARHolonDirectory, null, reInstall, providerType);
                }
                else
                {
                    OASISResult<T2> downloadResult = await DownloadAsync(avatarId, holon, fullDownloadPath, reInstall, providerType);

                    if (!fullDownloadPath.Contains(string.Concat(".", STARHolonFileExtention)))
                        fullDownloadPath = Path.Combine(fullDownloadPath, string.Concat(holon.Name, "_v", holon.STARHolonDNA.Version, ".", STARHolonFileExtention));

                    if (downloadResult != null && downloadResult.Result != null && !downloadResult.IsError)
                        result = await InstallAsync(avatarId, fullDownloadPath, fullInstallPath, createSTARHolonDirectory, downloadResult.Result, reInstall, providerType);
                    else
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured downloading the {STARHolonUIName} with the DownloadSTARHolonAsync method, reason: {downloadResult.Message}");
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
                OnInstallStatusChanged?.Invoke(this, new STARHolonInstallStatusEventArgs() { STARHolonDNA = holon.STARHolonDNA, Status = STARHolonInstallStatus.Error, ErrorMessage = result.Message });

            return result;
        }

        //copied.
        public OASISResult<T3> DownloadAndInstall(Guid avatarId, T1 holon, string fullInstallPath, string fullDownloadPath = "", bool createSTARHolonDirectory = true, bool reInstall = false, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T3> result = new OASISResult<T3>();
            string errorMessage = "Error occured in STARManagerBase.DownloadAndInstall. Reason: ";
            bool isFullDownloadPathTemp = false;

            try
            {
                if (string.IsNullOrEmpty(fullDownloadPath))
                {
                    string tempPath = Path.GetTempPath();
                    fullDownloadPath = Path.Combine(tempPath, string.Concat(holon.Name, ".", STARHolonFileExtention));
                    isFullDownloadPathTemp = true;
                }

                if (File.Exists(fullDownloadPath))
                    File.Delete(fullDownloadPath);

                if (holon.PublishedSTARHolon != null)
                {
                    File.WriteAllBytes(fullDownloadPath, holon.PublishedSTARHolon);
                    result = Install(avatarId, fullDownloadPath, fullInstallPath, createSTARHolonDirectory, null, reInstall, providerType);
                }
                else
                {
                    OASISResult<T2> downloadResult = Download(avatarId, holon, fullDownloadPath, reInstall, providerType);

                    if (!fullDownloadPath.Contains(string.Concat(".", STARHolonFileExtention)))
                        fullDownloadPath = Path.Combine(fullDownloadPath, string.Concat(holon.Name, "_v", holon.STARHolonDNA.Version, holon.Name, ".", STARHolonFileExtention));

                    if (downloadResult != null && downloadResult.Result != null && !downloadResult.IsError)
                        result = Install(avatarId, fullDownloadPath, fullInstallPath, createSTARHolonDirectory, downloadResult.Result, reInstall, providerType);
                    else
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured downloading the {STARHolonUIName} with the DownloadSTARHolonAsync method, reason: {downloadResult.Message}");
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
                OnInstallStatusChanged?.Invoke(this, new STARHolonInstallStatusEventArgs() { STARHolonDNA = holon.STARHolonDNA, Status = STARHolonInstallStatus.Error, ErrorMessage = result.Message });

            return result;
        }

        public async Task<OASISResult<T3>> DownloadAndInstallAsync(Guid avatarId, Guid STARHolonId, int version, string fullInstallPath, string fullDownloadPath = "", bool createSTARHolonDirectory = true, bool reInstall = false, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T3> result = new OASISResult<T3>();
            OASISResult<T1> STARHolonResult = await LoadAsync(STARHolonId, avatarId, version, providerType);

            if (STARHolonResult != null && !STARHolonResult.IsError && STARHolonResult.Result != null)
                result = await DownloadAndInstallAsync(avatarId, STARHolonResult.Result, fullInstallPath, fullDownloadPath, createSTARHolonDirectory, reInstall, providerType);
            else
            {
                OASISErrorHandling.HandleError(ref result, $"Error occured in STARManagerBase.InstallAsync loading the {STARHolonUIName} with the LoadAsync method, reason: {OASISErrorHandling.ProcessMessage(result, $"No result found for id {STARHolonId.ToString()}")}");
                OnInstallStatusChanged?.Invoke(this, new STARHolonInstallStatusEventArgs() { Status = STARHolonInstallStatus.Error, ErrorMessage = result.Message });
            }

            return result;
        }

        //copied.
        public OASISResult<T3> DownloadAndInstall(Guid avatarId, Guid STARHolonId, int version, string fullInstallPath, string fullDownloadPath = "", bool createSTARHolonDirectory = true, bool reInstall = false, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T3> result = new OASISResult<T3>();
            OASISResult<T1> STARHolonResult = Load(STARHolonId, avatarId, version, providerType);

            if (STARHolonResult != null && !STARHolonResult.IsError && STARHolonResult.Result != null)
                result = DownloadAndInstall(avatarId, STARHolonResult.Result, fullInstallPath, fullDownloadPath, createSTARHolonDirectory, reInstall, providerType);
            else
            {
                OASISErrorHandling.HandleError(ref result, $"Error occured in STARManagerBase.Install loading the {STARHolonUIName} with the LoadAsync method, reason: {OASISErrorHandling.ProcessMessage(result, $"No result found for id {STARHolonId.ToString()}")}");
                OnInstallStatusChanged?.Invoke(this, new STARHolonInstallStatusEventArgs() { Status = STARHolonInstallStatus.Error, ErrorMessage = result.Message });
            }

            return result;
        }

        public async Task<OASISResult<T3>> InstallAsync(Guid avatarId, string fullPathToPublishedSTARHolonFile, string fullInstallPath, bool createSTARHolonDirectory = true, IDownloadedSTARHolon downloadedSTARHolon = null, bool reInstall = false, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T3> result = new OASISResult<T3>();
            string errorMessage = "Error occured in STARManagerBase.InstallAsync. Reason: ";
            ISTARHolonDNA STARHolonDNA = null;
            string tempPath = "";
            T3 installedSTARHolon = default;
            int totalInstalls = 0;

            try
            {
                tempPath = Path.GetTempPath();
                tempPath = Path.Combine(tempPath, $"{STARHolonUIName}");

                if (Directory.Exists(tempPath))
                    Directory.Delete(tempPath, true);

                //Unzip
                OnInstallStatusChanged?.Invoke(this, new STARHolonInstallStatusEventArgs() { Status = STARHolonInstallStatus.Decompressing });
                ZipFile.ExtractToDirectory(fullPathToPublishedSTARHolonFile, tempPath, Encoding.Default, true);
                OASISResult<ISTARHolonDNA> STARHolonDNAResult = await ReadDNAFromSourceOrInstallFolderAsync<ISTARHolonDNA>(tempPath);

                if (STARHolonDNAResult != null && STARHolonDNAResult.Result != null && !STARHolonDNAResult.IsError)
                {
                    //Load the T from the OASIS to make sure the STARHolonDNA is valid (and has not been tampered with).

                    //TODO: Check if this works ok? What if they tamper with the VersionSequence in the DNA file?!
                    OASISResult<T1> STARHolonLoadResult = await LoadAsync(avatarId, STARHolonDNAResult.Result.Id, STARHolonDNAResult.Result.VersionSequence, providerType);
                    //OASISResult<ISTARHolon> STARHolonLoadResult = await LoadSTARHolonAsync(STARHolonDNAResult.Result.Id, false, 0, providerType);

                    if (STARHolonLoadResult != null && STARHolonLoadResult.Result != null && !STARHolonLoadResult.IsError)
                    {
                        //TODO: Not sure if we want to add a check here to compare the STARHolonDNA in the T dir with the one stored in the OASIS?
                        STARHolonDNA = STARHolonLoadResult.Result.STARHolonDNA;

                        if (createSTARHolonDirectory)
                            fullInstallPath = Path.Combine(fullInstallPath, string.Concat(STARHolonDNAResult.Result.Name, "_v", STARHolonDNAResult.Result.Version));

                        if (Directory.Exists(fullInstallPath))
                            Directory.Delete(fullInstallPath, true);

                        //Directory.CreateDirectory(fullInstallPath);
                        Directory.Move(tempPath, fullInstallPath);
                        //Directory.Delete(tempPath);

                        OnInstallStatusChanged?.Invoke(this, new STARHolonInstallStatusEventArgs() { STARHolonDNA = STARHolonDNAResult.Result, Status = STARHolonInstallStatus.Installing });
                        OASISResult<IAvatar> avatarResult = await AvatarManager.Instance.LoadAvatarAsync(avatarId, false, true, providerType);

                        if (avatarResult != null && !avatarResult.IsError && avatarResult.Result != null)
                        {
                            if (downloadedSTARHolon == null)
                            {
                                //OASISResult<DownloadedSTARHolon> downloadedSTARHolonResult = await Data.LoadHolonsByMetaDataAsync<DownloadedSTARHolon>("STARHolonId", STARHolonDNAResult.Result.Id.ToString(), false, false, 0, true, 0, false, HolonType.All, providerType);
                                OASISResult<IEnumerable<T2>> downloadedSTARHolonResult = await Data.LoadHolonsByMetaDataAsync<T2>(STARHolonIdName, STARHolonDNAResult.Result.Id.ToString(), HolonType.All, true, true, 0, true, false, 0, HolonType.All, 0, providerType);

                                if (downloadedSTARHolonResult != null && !downloadedSTARHolonResult.IsError && downloadedSTARHolonResult.Result != null)
                                    downloadedSTARHolon = downloadedSTARHolonResult.Result.FirstOrDefault();
                                else
                                    OASISErrorHandling.HandleWarning(ref result, $"The {STARHolonUIName} was installed but the DownloadedSTARHolon could not be found. Reason: {downloadedSTARHolonResult.Message}");
                            }

                            if (!reInstall)
                            {
                                //If it's a re-install then it doesnt count as an install so we dont need to update the counts.
                                STARHolonDNA.Installs++;

                                installedSTARHolon = new T3()
                                {
                                    Name = string.Concat(STARHolonDNA.Name, " Installed Holon"),
                                    Description = string.Concat(STARHolonDNA.Description, " Installed Holon"),
                                    //STARHolonId = STARHolonDNAResult.Result.STARHolonId,
                                    STARHolonDNA = STARHolonDNA,
                                    InstalledBy = avatarId,
                                    InstalledByAvatarUsername = avatarResult.Result.Username,
                                    InstalledOn = DateTime.Now,
                                    InstalledPath = fullInstallPath,
                                    //DownloadedSTARHolon = downloadedSTARHolon,
                                    DownloadedBy = downloadedSTARHolon.DownloadedBy,
                                    DownloadedByAvatarUsername = downloadedSTARHolon.DownloadedByAvatarUsername,
                                    DownloadedOn = downloadedSTARHolon.DownloadedOn,
                                    DownloadedPath = downloadedSTARHolon.DownloadedPath,
                                    DownloadedSTARHolonId = downloadedSTARHolon.Id,
                                    Active = "1",
                                    //STARHolonVersion = STARHolonDNA.Version
                                };

                                installedSTARHolon.MetaData["Version"] = STARHolonDNA.Version;
                                installedSTARHolon.MetaData["VersionSequence"] = STARHolonDNA.VersionSequence;
                                installedSTARHolon.MetaData[STARHolonIdName] = STARHolonDNA.Id;

                                await UpdateInstallCountsAsync(avatarId, installedSTARHolon, STARHolonDNA, result, errorMessage, providerType);
                            }
                            else
                            {
                                OASISResult<T3> installedSTARHolonResult = await LoadInstalledAsync(avatarId, STARHolonDNAResult.Result.Id, STARHolonDNAResult.Result.Version, false, providerType);

                                if (installedSTARHolonResult != null && installedSTARHolonResult.Result != null && !installedSTARHolonResult.IsError)
                                {
                                    installedSTARHolon = installedSTARHolonResult.Result;
                                    installedSTARHolon.Active = "1";
                                    installedSTARHolon.UninstalledBy = Guid.Empty;
                                    installedSTARHolon.UninstalledByAvatarUsername = "";
                                    installedSTARHolon.UninstalledOn = DateTime.MinValue;
                                    installedSTARHolon.InstalledBy = avatarId;
                                    installedSTARHolon.InstalledByAvatarUsername = avatarResult.Result.Username;
                                    installedSTARHolon.InstalledOn = DateTime.Now;
                                    installedSTARHolon.InstalledPath = fullInstallPath;
                                    installedSTARHolon.DownloadedBy = downloadedSTARHolon.DownloadedBy;
                                    installedSTARHolon.DownloadedByAvatarUsername = downloadedSTARHolon.DownloadedByAvatarUsername;
                                    installedSTARHolon.DownloadedOn = downloadedSTARHolon.DownloadedOn;
                                    installedSTARHolon.DownloadedPath = downloadedSTARHolon.DownloadedPath;
                                }
                                else
                                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured re-installing {STARHolonUIName} calling LoadAsync. Reason: {installedSTARHolonResult.Message}");
                            }

                            if (!result.IsError)
                            {
                                OASISResult<T3> saveResult = await SaveAsync(avatarId, installedSTARHolon, providerType);

                                if (saveResult != null && saveResult.Result != null && !saveResult.IsError)
                                {
                                    //result.Result = installedSTARHolon;
                                    //result.Result.DownloadedSTARHolon = downloadedSTARHolon;
                                    STARHolonLoadResult.Result.STARHolonDNA = STARHolonDNA;

                                    OASISResult<T1> oappSaveResult = await SaveAsync(avatarId, STARHolonLoadResult.Result, providerType);

                                    if (oappSaveResult != null && !oappSaveResult.IsError && oappSaveResult.Result != null)
                                    {
                                        if (STARHolonDNAResult.Result.STARODKVersion != OASISBootLoader.OASISBootLoader.STARODKVersion)
                                            OASISErrorHandling.HandleWarning(ref result, $"The STAR ODK Version {STARHolonDNAResult.Result.STARODKVersion} does not match the current version {OASISBootLoader.OASISBootLoader.STARODKVersion}. This may lead to issues, it is recommended to make sure the versions match.");

                                        if (STARHolonDNAResult.Result.OASISVersion != OASISBootLoader.OASISBootLoader.OASISVersion)
                                            OASISErrorHandling.HandleWarning(ref result, $"The OASIS Version {STARHolonDNAResult.Result.OASISVersion} does not match the current version {OASISBootLoader.OASISBootLoader.OASISVersion}. This may lead to issues, it is recommended to make sure the versions match.");

                                        if (STARHolonDNAResult.Result.COSMICVersion != OASISBootLoader.OASISBootLoader.COSMICVersion)
                                            OASISErrorHandling.HandleWarning(ref result, $"The COSMIC Version {STARHolonDNAResult.Result.COSMICVersion} does not match the current version {OASISBootLoader.OASISBootLoader.COSMICVersion}. This may lead to issues, it is recommended to make sure the versions match.");

                                        if (result.InnerMessages.Count > 0)
                                            result.Message = $"{STARHolonUIName} successfully installed but there were {result.WarningCount} warnings:\n\n {OASISResultHelper.BuildInnerMessageError(result.InnerMessages)}";
                                        else
                                            result.Message = $"{STARHolonUIName} Successfully Installed";

                                        result.Result = installedSTARHolon;
                                        OnInstallStatusChanged?.Invoke(this, new STARHolonInstallStatusEventArgs() { STARHolonDNA = STARHolonDNAResult.Result, Status = STARHolonInstallStatus.Installed });
                                    }
                                    else
                                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling SaveAsync method. Reason: {oappSaveResult.Message}");
                                }
                                else
                                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling SaveAsync method. Reason: {saveResult.Message}");
                            }
                            else
                                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadAvatarAsync method. Reason: {avatarResult.Message}");
                        }
                    }
                    else
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadSTARHolonAsync method. Reason: {STARHolonLoadResult.Message}");
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
                OnInstallStatusChanged?.Invoke(this, new STARHolonInstallStatusEventArgs() { STARHolonDNA = STARHolonDNA, Status = STARHolonInstallStatus.Error, ErrorMessage = result.Message });

            return result;
        }

        //Copied from async
        public OASISResult<T3> Install(Guid avatarId, string fullPathToPublishedSTARHolonFile, string fullInstallPath, bool createSTARHolonDirectory = true, IDownloadedSTARHolon downloadedSTARHolon = null, bool reInstall = false, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T3> result = new OASISResult<T3>();
            string errorMessage = "Error occured in STARManagerBase.Install. Reason: ";
            ISTARHolonDNA STARHolonDNA = null;
            string tempPath = "";
            T3 installedSTARHolon = default;
            int totalInstalls = 0;

            try
            {
                tempPath = Path.GetTempPath();
                tempPath = Path.Combine(tempPath, $"{STARHolonUIName}");

                if (Directory.Exists(tempPath))
                    Directory.Delete(tempPath, true);

                //Unzip
                OnInstallStatusChanged?.Invoke(this, new STARHolonInstallStatusEventArgs() { Status = STARHolonInstallStatus.Decompressing });
                ZipFile.ExtractToDirectory(fullPathToPublishedSTARHolonFile, tempPath, Encoding.Default, true);
                OASISResult<ISTARHolonDNA> STARHolonDNAResult = ReadDNAFromSourceOrInstallFolder<ISTARHolonDNA>(tempPath);

                if (STARHolonDNAResult != null && STARHolonDNAResult.Result != null && !STARHolonDNAResult.IsError)
                {
                    //Load the T from the OASIS to make sure the STARHolonDNA is valid (and has not been tampered with).

                    //TODO: Check if this works ok? What if they tamper with the VersionSequence in the DNA file?!
                    OASISResult<T1> STARHolonLoadResult = Load(avatarId, STARHolonDNAResult.Result.Id, STARHolonDNAResult.Result.VersionSequence, providerType);
                    //OASISResult<ISTARHolon> STARHolonLoadResult = await LoadSTARHolonAsync(STARHolonDNAResult.Result.Id, false, 0, providerType);

                    if (STARHolonLoadResult != null && STARHolonLoadResult.Result != null && !STARHolonLoadResult.IsError)
                    {
                        //TODO: Not sure if we want to add a check here to compare the STARHolonDNA in the T dir with the one stored in the OASIS?
                        STARHolonDNA = STARHolonLoadResult.Result.STARHolonDNA;

                        if (createSTARHolonDirectory)
                            fullInstallPath = Path.Combine(fullInstallPath, string.Concat(STARHolonDNAResult.Result.Name, "_v", STARHolonDNAResult.Result.Version));

                        if (Directory.Exists(fullInstallPath))
                            Directory.Delete(fullInstallPath, true);

                        //Directory.CreateDirectory(fullInstallPath);
                        Directory.Move(tempPath, fullInstallPath);
                        //Directory.Delete(tempPath);

                        OnInstallStatusChanged?.Invoke(this, new STARHolonInstallStatusEventArgs() { STARHolonDNA = STARHolonDNAResult.Result, Status = STARHolonInstallStatus.Installing });
                        OASISResult<IAvatar> avatarResult = AvatarManager.Instance.LoadAvatar(avatarId, false, true, providerType);

                        if (avatarResult != null && !avatarResult.IsError && avatarResult.Result != null)
                        {
                            if (downloadedSTARHolon == null)
                            {
                                //OASISResult<DownloadedSTARHolon> downloadedSTARHolonResult = await Data.LoadHolonsByMetaDataAsync<DownloadedSTARHolon>("STARHolonId", STARHolonDNAResult.Result.Id.ToString(), false, false, 0, true, 0, false, HolonType.All, providerType);
                                OASISResult<IEnumerable<T2>> downloadedSTARHolonResult = Data.LoadHolonsByMetaData<T2>(STARHolonIdName, STARHolonDNAResult.Result.Id.ToString(), HolonType.All, true, true, 0, true, false, 0, HolonType.All, 0, providerType);

                                if (downloadedSTARHolonResult != null && !downloadedSTARHolonResult.IsError && downloadedSTARHolonResult.Result != null)
                                    downloadedSTARHolon = downloadedSTARHolonResult.Result.FirstOrDefault();
                                else
                                    OASISErrorHandling.HandleWarning(ref result, $"The {STARHolonUIName} was installed but the DownloadedSTARHolon could not be found. Reason: {downloadedSTARHolonResult.Message}");
                            }

                            if (!reInstall)
                            {
                                //If it's a re-install then it doesnt count as an install so we dont need to update the counts.
                                STARHolonDNA.Installs++;

                                installedSTARHolon = new T3()
                                {
                                    Name = string.Concat(STARHolonDNA.Name, " Installed Holon"),
                                    Description = string.Concat(STARHolonDNA.Description, " Installed Holon"),
                                    //STARHolonId = STARHolonDNAResult.Result.STARHolonId,
                                    STARHolonDNA = STARHolonDNA,
                                    InstalledBy = avatarId,
                                    InstalledByAvatarUsername = avatarResult.Result.Username,
                                    InstalledOn = DateTime.Now,
                                    InstalledPath = fullInstallPath,
                                    //DownloadedSTARHolon = downloadedSTARHolon,
                                    DownloadedBy = downloadedSTARHolon.DownloadedBy,
                                    DownloadedByAvatarUsername = downloadedSTARHolon.DownloadedByAvatarUsername,
                                    DownloadedOn = downloadedSTARHolon.DownloadedOn,
                                    DownloadedPath = downloadedSTARHolon.DownloadedPath,
                                    DownloadedSTARHolonId = downloadedSTARHolon.Id,
                                    Active = "1",
                                    //STARHolonVersion = STARHolonDNA.Version
                                };

                                installedSTARHolon.MetaData["Version"] = STARHolonDNA.Version;
                                installedSTARHolon.MetaData["VersionSequence"] = STARHolonDNA.VersionSequence;
                                installedSTARHolon.MetaData[STARHolonIdName] = STARHolonDNA.Id;

                                UpdateInstallCounts(avatarId, installedSTARHolon, STARHolonDNA, result, errorMessage, providerType);
                            }
                            else
                            {
                                OASISResult<T3> installedSTARHolonResult = LoadInstalled(avatarId, STARHolonDNAResult.Result.Id, STARHolonDNAResult.Result.Version, false, providerType);

                                if (installedSTARHolonResult != null && installedSTARHolonResult.Result != null && !installedSTARHolonResult.IsError)
                                {
                                    installedSTARHolon = installedSTARHolonResult.Result;
                                    installedSTARHolon.Active = "1";
                                    installedSTARHolon.UninstalledBy = Guid.Empty;
                                    installedSTARHolon.UninstalledByAvatarUsername = "";
                                    installedSTARHolon.UninstalledOn = DateTime.MinValue;
                                    installedSTARHolon.InstalledBy = avatarId;
                                    installedSTARHolon.InstalledByAvatarUsername = avatarResult.Result.Username;
                                    installedSTARHolon.InstalledOn = DateTime.Now;
                                    installedSTARHolon.InstalledPath = fullInstallPath;
                                    installedSTARHolon.DownloadedBy = downloadedSTARHolon.DownloadedBy;
                                    installedSTARHolon.DownloadedByAvatarUsername = downloadedSTARHolon.DownloadedByAvatarUsername;
                                    installedSTARHolon.DownloadedOn = downloadedSTARHolon.DownloadedOn;
                                    installedSTARHolon.DownloadedPath = downloadedSTARHolon.DownloadedPath;
                                }
                                else
                                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured re-installing {STARHolonUIName} calling LoadAsync. Reason: {installedSTARHolonResult.Message}");
                            }

                            if (!result.IsError)
                            {
                                OASISResult<T3> saveResult = Save(avatarId, installedSTARHolon, providerType);

                                if (saveResult != null && saveResult.Result != null && !saveResult.IsError)
                                {
                                    //result.Result = installedSTARHolon;
                                    //result.Result.DownloadedSTARHolon = downloadedSTARHolon;
                                    STARHolonLoadResult.Result.STARHolonDNA = STARHolonDNA;

                                    OASISResult<T1> oappSaveResult = Save(avatarId, STARHolonLoadResult.Result, providerType);

                                    if (oappSaveResult != null && !oappSaveResult.IsError && oappSaveResult.Result != null)
                                    {
                                        if (STARHolonDNAResult.Result.STARODKVersion != OASISBootLoader.OASISBootLoader.STARODKVersion)
                                            OASISErrorHandling.HandleWarning(ref result, $"The STAR ODK Version {STARHolonDNAResult.Result.STARODKVersion} does not match the current version {OASISBootLoader.OASISBootLoader.STARODKVersion}. This may lead to issues, it is recommended to make sure the versions match.");

                                        if (STARHolonDNAResult.Result.OASISVersion != OASISBootLoader.OASISBootLoader.OASISVersion)
                                            OASISErrorHandling.HandleWarning(ref result, $"The OASIS Version {STARHolonDNAResult.Result.OASISVersion} does not match the current version {OASISBootLoader.OASISBootLoader.OASISVersion}. This may lead to issues, it is recommended to make sure the versions match.");

                                        if (STARHolonDNAResult.Result.COSMICVersion != OASISBootLoader.OASISBootLoader.COSMICVersion)
                                            OASISErrorHandling.HandleWarning(ref result, $"The COSMIC Version {STARHolonDNAResult.Result.COSMICVersion} does not match the current version {OASISBootLoader.OASISBootLoader.COSMICVersion}. This may lead to issues, it is recommended to make sure the versions match.");

                                        if (result.InnerMessages.Count > 0)
                                            result.Message = $"{STARHolonUIName} successfully installed but there were {result.WarningCount} warnings:\n\n {OASISResultHelper.BuildInnerMessageError(result.InnerMessages)}";
                                        else
                                            result.Message = $"{STARHolonUIName} Successfully Installed";

                                        result.Result = installedSTARHolon;
                                        OnInstallStatusChanged?.Invoke(this, new STARHolonInstallStatusEventArgs() { STARHolonDNA = STARHolonDNAResult.Result, Status = STARHolonInstallStatus.Installed });
                                    }
                                    else
                                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling SaveAsync method. Reason: {oappSaveResult.Message}");
                                }
                                else
                                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling SaveAsync method. Reason: {saveResult.Message}");
                            }
                            else
                                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadAvatarAsync method. Reason: {avatarResult.Message}");
                        }
                    }
                    else
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadSTARHolonAsync method. Reason: {STARHolonLoadResult.Message}");
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
                OnInstallStatusChanged?.Invoke(this, new STARHolonInstallStatusEventArgs() { STARHolonDNA = STARHolonDNA, Status = STARHolonInstallStatus.Error, ErrorMessage = result.Message });

            return result;
        }

        //public OASISResult<T3> Install(Guid avatarId, T1 holon, string fullInstallPath, string fullDownloadPath = "", bool createSTARHolonDirectory = true, ProviderType providerType = ProviderType.Default)
        //{
        //    OASISResult<T3> result = new OASISResult<T3>();
        //    string errorMessage = "Error occured in STARManagerBase.Install. Reason: ";

        //    try
        //    {
        //        string SourcePath = Path.Combine("temp", holon.Name, ".", STARHolonFileExtention);

        //        if (holon.PublishedSTARHolon != null)
        //        {
        //            File.WriteAllBytes(SourcePath, holon.PublishedSTARHolon);
        //            result = Install(avatarId, holon, SourcePath, fullInstallPath, createSTARHolonDirectory, providerType);
        //        }
        //        {
        //            try
        //            {
        //                StorageClient storage = StorageClient.Create();

        //                // set minimum chunksize just to see progress updating
        //                var downloadObjectOptions = new DownloadObjectOptions
        //                {
        //                    ChunkSize = UploadObjectOptions.MinimumChunkSize,
        //                };

        //                var progressReporter = new Progress<Google.Apis.Download.IDownloadProgress>(OnDownloadProgress);

        //                using var fileStream = File.OpenWrite(SourcePath);
        //                _fileLength = fileStream.Length;
        //                _progress = 0;

        //                OnSTARHolonInstallStatusChanged?.Invoke(this, new STARHolonInstallStatusEventArgs() { STARHolonDNA = holon.STARHolonDNA, Status = Enums.STARHolonInstallStatus.Downloading });
        //                storage.DownloadObject(STARHolonGoogleBucket, string.Concat(holon.Name, ".", STARHolonFileExtention), fileStream, downloadObjectOptions, progress: progressReporter);

        //                _progress = 100;
        //                OnSTARHolonDownloadStatusChanged?.Invoke(this, new STARHolonDownloadProgressEventArgs() { Progress = _progress, Status = Enums.STARHolonDownloadStatus.Downloading });
        //                CLIEngine.DisposeProgressBar(false);
        //                Console.WriteLine("");

        //                result = Install(avatarId, holon, SourcePath, fullInstallPath, createSTARHolonDirectory, providerType);
        //            }
        //            catch (Exception ex)
        //            {
        //                OASISErrorHandling.HandleError(ref result, $"An error occured downloading the {STARHolonUIName} from cloud storage. Reason: {ex}");
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        OASISErrorHandling.HandleError(ref result, $"{errorMessage} {ex}");
        //    }

        //    if (result.IsError)
        //        OnSTARHolonInstallStatusChanged?.Invoke(this, new STARHolonInstallStatusEventArgs() { STARHolonDNA = holon.STARHolonDNA, Status = Enums.STARHolonInstallStatus.Error, ErrorMessage = result.Message });

        //    return result;
        //}

        public async Task<OASISResult<T3>> UninstallAsync(Guid avatarId, T3 installedSTARHolon, string errorMessage, ProviderType providerType)
        {
            OASISResult<T3> result = new OASISResult<T3>();

            try
            {
                Directory.Delete(installedSTARHolon.InstalledPath, true);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} Error occured attempting to delete the {STARHolonUIName} folder ({installedSTARHolon.InstalledPath}) Reason: {ex.Message}");
            }

            //if (!result.IsError)
            //{
            OASISResult<IAvatar> avatarResult = await AvatarManager.Instance.LoadAvatarAsync(avatarId, false, true, providerType, 0);

            if (avatarResult != null && avatarResult.Result != null && !avatarResult.IsError)
            {
                installedSTARHolon.UninstalledBy = avatarId;
                installedSTARHolon.UninstalledOn = DateTime.Now;
                installedSTARHolon.UninstalledByAvatarUsername = avatarResult.Result.Username;
                installedSTARHolon.Active = "0";

                OASISResult<T3> saveIntalledSTARHolonResult = await installedSTARHolon.SaveAsync<T3>();

                if (saveIntalledSTARHolonResult != null && !saveIntalledSTARHolonResult.IsError && saveIntalledSTARHolonResult.Result != null)
                {
                    result.Message = $"{STARHolonUIName} Uninstalled";
                    result.Result = saveIntalledSTARHolonResult.Result;
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling SaveAsync. Reason: {saveIntalledSTARHolonResult.Message}");
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadAvatarAsync. Reason: {avatarResult.Message}");
            //}

            return result;
        }

        //copied.
        public OASISResult<T3> Uninstall(Guid avatarId, T3 installedSTARHolon, string errorMessage, ProviderType providerType)
        {
            OASISResult<T3> result = new OASISResult<T3>();

            try
            {
                Directory.Delete(installedSTARHolon.InstalledPath, true);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} Error occured attempting to delete the {STARHolonUIName} folder ({installedSTARHolon.InstalledPath}) Reason: {ex.Message}");
            }

            //if (!result.IsError)
            //{
            OASISResult<IAvatar> avatarResult = AvatarManager.Instance.LoadAvatar(avatarId, false, true, providerType, 0);

            if (avatarResult != null && avatarResult.Result != null && !avatarResult.IsError)
            {
                installedSTARHolon.UninstalledBy = avatarId;
                installedSTARHolon.UninstalledOn = DateTime.Now;
                installedSTARHolon.UninstalledByAvatarUsername = avatarResult.Result.Username;
                installedSTARHolon.Active = "0";

                OASISResult<T3> saveIntalledSTARHolonResult = installedSTARHolon.Save<T3>();

                if (saveIntalledSTARHolonResult != null && !saveIntalledSTARHolonResult.IsError && saveIntalledSTARHolonResult.Result != null)
                {
                    result.Message = $"{STARHolonUIName} Uninstalled";
                    result.Result = saveIntalledSTARHolonResult.Result;
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling SaveAsync. Reason: {saveIntalledSTARHolonResult.Message}");
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadAvatarAsync. Reason: {avatarResult.Message}");
            //}

            return result;
        }

        public async Task<OASISResult<T3>> UninstallAsync(Guid avatarId, Guid STARHolonId, int versionSequence, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T3> result = new OASISResult<T3>();
            string errorMessage = "Error occured in STARManagerBase.UninstallAsync. Reason: ";

            OASISResult<T3> loadResult = await Data.LoadHolonByMetaDataAsync<T3>(new Dictionary<string, string>()
            {
                { STARHolonIdName, STARHolonId.ToString() },
                { "VersionSequene", versionSequence.ToString() }

            }, MetaKeyValuePairMatchMode.All, STARInstalledHolonType, true, true, 0, true, 0, false, HolonType.All, providerType);

            if (loadResult != null && loadResult.Result != null && !loadResult.IsError)
                result = await UninstallAsync(avatarId, loadResult.Result, errorMessage, providerType);
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonByMetaDataAsync. Reason: {loadResult.Message}");

            return result;
        }

        public OASISResult<T3> Uninstall(Guid avatarId, Guid STARHolonId, int versionSequence, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T3> result = new OASISResult<T3>();
            string errorMessage = "Error occured in STARManagerBase.UninstallAsync. Reason: ";

            OASISResult<T3> loadResult = Data.LoadHolonByMetaData<T3>(new Dictionary<string, string>()
            {
                { STARHolonIdName, STARHolonId.ToString() },
                { "VersionSequene", versionSequence.ToString() }

            }, MetaKeyValuePairMatchMode.All, STARInstalledHolonType, true, true, 0, true, false, HolonType.All);

            if (loadResult != null && loadResult.Result != null && !loadResult.IsError)
                result = Uninstall(avatarId, loadResult.Result, errorMessage, providerType);
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonByMetaData. Reason: {loadResult.Message}");

            return result;
        }

        public async Task<OASISResult<T3>> UninstallAsync(Guid avatarId, Guid STARHolonId, string version, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T3> result = new OASISResult<T3>();
            string errorMessage = "Error occured in STARManagerBase.UninstallAsync. Reason: ";

            OASISResult<T3> loadResult = await Data.LoadHolonByMetaDataAsync<T3>(new Dictionary<string, string>()
            {
                { STARHolonIdName, STARHolonId.ToString() },
                { "Version", version }

            }, MetaKeyValuePairMatchMode.All, STARInstalledHolonType, true, true, 0, true, 0, false, HolonType.All, providerType);

            if (loadResult != null && loadResult.Result != null && !loadResult.IsError)
                result = await UninstallAsync(avatarId, loadResult.Result, errorMessage, providerType);
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonByMetaDataAsync. Reason: {loadResult.Message}");

            return result;
        }

        public OASISResult<T3> Uninstall(Guid avatarId, Guid STARHolonId, string version, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T3> result = new OASISResult<T3>();
            string errorMessage = "Error occured in STARManagerBase.Uninstall. Reason: ";

            OASISResult<T3> loadResult = Data.LoadHolonByMetaData<T3>(new Dictionary<string, string>()
            {
                { STARHolonIdName, STARHolonId.ToString() },
                { "Version", version }

            }, MetaKeyValuePairMatchMode.All, STARInstalledHolonType, true, true, 0, true, false, HolonType.All);

            if (loadResult != null && loadResult.Result != null && !loadResult.IsError)
                result = Uninstall(avatarId, loadResult.Result, errorMessage, providerType);
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonByMetaData. Reason: {loadResult.Message}");

            return result;
        }

        public async Task<OASISResult<T3>> UninstallAsync(Guid avatarId, string STARHolonName, int versionSequence, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T3> result = new OASISResult<T3>();
            string errorMessage = "Error occured in STARManagerBase.UninstallAsync. Reason: ";

            OASISResult<T3> loadResult = await Data.LoadHolonByMetaDataAsync<T3>(new Dictionary<string, string>()
            {
                { STARHolonNameName, STARHolonName },
                { "VersionSequene", versionSequence.ToString() }

            }, MetaKeyValuePairMatchMode.All, STARInstalledHolonType, true, true, 0, true, 0, false, HolonType.All, providerType);

            if (loadResult != null && loadResult.Result != null && !loadResult.IsError)
                result = await UninstallAsync(avatarId, loadResult.Result, errorMessage, providerType);
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonByMetaDataAsync. Reason: {loadResult.Message}");

            return result;
        }

        public OASISResult<T3> Uninstall(Guid avatarId, string STARHolonName, int versionSequence, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T3> result = new OASISResult<T3>();
            string errorMessage = "Error occured in STARManagerBase.Uninstall. Reason: ";

            OASISResult<T3> loadResult = Data.LoadHolonByMetaData<T3>(new Dictionary<string, string>()
            {
                { STARHolonNameName, STARHolonName},
                { "VersionSequene", versionSequence.ToString() }

            }, MetaKeyValuePairMatchMode.All, STARInstalledHolonType, true, true, 0, true, false, HolonType.All);

            if (loadResult != null && loadResult.Result != null && !loadResult.IsError)
                result = Uninstall(avatarId, loadResult.Result, errorMessage, providerType);
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonByMetaData. Reason: {loadResult.Message}");

            return result;
        }

        public async Task<OASISResult<T3>> UninstallAsync(Guid avatarId, string STARHolonName, string version, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T3> result = new OASISResult<T3>();
            string errorMessage = "Error occured in STARManagerBase.UninstallAsync. Reason: ";

            OASISResult<T3> loadResult = await Data.LoadHolonByMetaDataAsync<T3>(new Dictionary<string, string>()
            {
                { STARHolonNameName, STARHolonName },
                { "Version", version }

            }, MetaKeyValuePairMatchMode.All, STARInstalledHolonType, true, true, 0, true, 0, false, HolonType.All, providerType);

            if (loadResult != null && loadResult.Result != null && !loadResult.IsError)
                result = await UninstallAsync(avatarId, loadResult.Result, errorMessage, providerType);
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonByMetaDataAsync. Reason: {loadResult.Message}");

            return result;
        }

        public OASISResult<T3> Uninstall(Guid avatarId, string STARHolonName, string version, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T3> result = new OASISResult<T3>();
            string errorMessage = "Error occured in STARManagerBase.Uninstall. Reason: ";

            OASISResult<T3> loadResult = Data.LoadHolonByMetaData<T3>(new Dictionary<string, string>()
            {
                { STARHolonNameName, STARHolonName},
                { "Version", version }

            }, MetaKeyValuePairMatchMode.All, STARInstalledHolonType, true, true, 0, true, false, HolonType.All);

            if (loadResult != null && loadResult.Result != null && !loadResult.IsError)
                result = Uninstall(avatarId, loadResult.Result, errorMessage, providerType);
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonByMetaData. Reason: {loadResult.Message}");

            return result;
        }

        public async Task<OASISResult<IEnumerable<T3>>> ListInstalledAsync(Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<T3>> result = await Data.LoadHolonsForParentAsync<T3>(avatarId, STARInstalledHolonType, false, false, 0, true, false, 0, HolonType.All, 0, providerType);

            if (result != null && !result.IsError && result.Result != null)
                result.Result = result.Result.Where(x => x.UninstalledOn == DateTime.MinValue);
            else
                OASISErrorHandling.HandleError(ref result, $"Error occured in STARManagerBase.ListInstalledAsync. Reason: Error occured calling LoadHolonsForParentAsync. Reason: {result.Message}");

            return result;
        }

        public OASISResult<IEnumerable<T3>> ListInstalled(Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<T3>> result = Data.LoadHolonsForParent<T3>(avatarId, STARInstalledHolonType, false, false, 0, true, false, 0, HolonType.All, 0, providerType);

            if (result != null && !result.IsError && result.Result != null)
                result.Result = result.Result.Where(x => x.UninstalledOn == DateTime.MinValue);
            else
                OASISErrorHandling.HandleError(ref result, $"Error occured in STARManagerBase.ListInstalled. Reason: Error occured calling LoadHolonsForParent. Reason: {result.Message}");

            return result;
        }

        public async Task<OASISResult<IEnumerable<T3>>> ListUninstalledAsync(Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<T3>> result = await Data.LoadHolonsForParentAsync<T3>(avatarId, STARInstalledHolonType, false, false, 0, true, false, 0, HolonType.All, 0, providerType);

            if (result != null && !result.IsError && result.Result != null)
                result.Result = result.Result.Where(x => x.UninstalledOn != DateTime.MinValue);
            else
                OASISErrorHandling.HandleError(ref result, $"Error occured in STARManagerBase.ListUninstalledAsync. Reason:  Error occured calling LoadHolonsForParent. Reason: {result.Message}");

            return result;
        }

        public OASISResult<IEnumerable<T3>> ListUninstalled(Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<T3>> result = Data.LoadHolonsForParent<T3>(avatarId, STARInstalledHolonType, false, false, 0, true, false, 0, HolonType.All, 0, providerType);

            if (result != null && !result.IsError && result.Result != null)
                result.Result = result.Result.Where(x => x.UninstalledOn != DateTime.MinValue);
            else
                OASISErrorHandling.HandleError(ref result, $"Error occured in STARManagerBase.ListUninstalled. Reason:  Error occured calling LoadHolonsForParent. Reason: {result.Message}");

            return result;
        }

        public async Task<OASISResult<IEnumerable<T1>>> ListUnpublishedAsync(Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<T1>> result = new OASISResult<IEnumerable<T1>>();
            string errorMessage = "Error occured in STARManagerBase.ListUnpublishedAsync. Reason: ";
            result = await Data.LoadHolonsForParentAsync<T1>(avatarId, STARHolonType, false, false, 0, true, false, 0, HolonType.All, 0, providerType);

            if (result != null && !result.IsError && result.Result != null)
                result.Result = result.Result.Where(x => x.STARHolonDNA.PublishedOn == DateTime.MinValue && x.STARHolonDNA.FileSize > 0);
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonsForParentAsync. Reason: {result.Message}");

            return result;
        }

        public OASISResult<IEnumerable<T1>> ListUnpublished(Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<T1>> result = new OASISResult<IEnumerable<T1>>();
            string errorMessage = "Error occured in STARManagerBase.ListUnpublished. Reason: ";
            result = Data.LoadHolonsForParent<T1>(avatarId, STARHolonType, false, false, 0, true, false, 0, HolonType.All, 0, providerType);

            if (result != null && !result.IsError && result.Result != null)
                result.Result = result.Result.Where(x => x.STARHolonDNA.PublishedOn == DateTime.MinValue && x.STARHolonDNA.FileSize > 0);
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonsForParent. Reason: {result.Message}");

            return result;
        }

        public async Task<OASISResult<IEnumerable<T1>>> ListDeactivatedAsync(Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<T1>> result = new OASISResult<IEnumerable<T1>>();
            string errorMessage = "Error occured in STARManagerBase.ListDeactivatedAsync. Reason: ";
            result = await Data.LoadHolonsByMetaDataAsync<T1>("Active", "0", STARHolonType, true, true, 0, true, false, 0, HolonType.All, 0, providerType);
            return result;
        }

        public OASISResult<IEnumerable<T1>> ListDeactivated(Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<T1>> result = new OASISResult<IEnumerable<T1>>();
            string errorMessage = "Error occured in STARManagerBase.ListDeactivated. Reason: ";
            result = Data.LoadHolonsByMetaData<T1>("Active", "0", STARHolonType, true, true, 0, true, false, 0, HolonType.All, 0, providerType);
            return result;
        }

        public async Task<OASISResult<bool>> IsInstalledAsync(Guid avatarId, Guid STARHolonId, int versionSequence, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<bool> result = new OASISResult<bool>();
            string errorMessage = "Error occured in STARManagerBase.IsInstalledAsync. Reason: ";

            OASISResult<T3> installedSTARHolonsResult = await Data.LoadHolonByMetaDataAsync<T3>(new Dictionary<string, string>()
            {
                { STARHolonIdName, STARHolonId.ToString() },
                { "VersionSequence", versionSequence.ToString() },
                { "Active", "1" }

            }, MetaKeyValuePairMatchMode.All, STARInstalledHolonType, true, true, 0, true, 0, false, HolonType.All, providerType);

            if (installedSTARHolonsResult != null && !installedSTARHolonsResult.IsError)
            {
                if (installedSTARHolonsResult.Result != null)
                    result.Result = true;
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonByMetaDataAsync. Reason: {installedSTARHolonsResult.Message}");

            return result;
        }

        public OASISResult<bool> IsInstalled(Guid avatarId, Guid STARHolonId, int versionSequence, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<bool> result = new OASISResult<bool>();
            string errorMessage = "Error occured in STARManagerBase.IsInstalled. Reason: ";

            OASISResult<T3> installedSTARHolonsResult = Data.LoadHolonByMetaData<T3>(new Dictionary<string, string>()
            {
                { STARHolonIdName, STARHolonId.ToString() },
                { "VersionSequence", versionSequence.ToString() },
                { "Active", "1" }

            }, MetaKeyValuePairMatchMode.All, STARInstalledHolonType, true, true, 0, true, false, HolonType.All, 0, providerType);

            if (installedSTARHolonsResult != null && !installedSTARHolonsResult.IsError && installedSTARHolonsResult.Result != null)
                result.Result = true;
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonByMetaDataAsync. Reason: {installedSTARHolonsResult.Message}");

            return result;
        }

        public async Task<OASISResult<bool>> IsInstalledAsync(Guid avatarId, Guid STARHolonId, string version, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<bool> result = new OASISResult<bool>();
            string errorMessage = "Error occured in STARManagerBase.IsInstalledAsync. Reason: ";

            OASISResult<T3> installedSTARHolonsResult = await Data.LoadHolonByMetaDataAsync<T3>(new Dictionary<string, string>()
            {
                { STARHolonIdName, STARHolonId.ToString() },
                { "Version", version },
                { "Active", "1" }

            }, MetaKeyValuePairMatchMode.All, STARInstalledHolonType, true, true, 0, true, 0, false, HolonType.All, providerType);

            if (installedSTARHolonsResult != null && !installedSTARHolonsResult.IsError)
            {
                if (installedSTARHolonsResult.Result != null)
                    result.Result = true;
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonByMetaDataAsync. Reason: {installedSTARHolonsResult.Message}");

            return result;
        }

        public OASISResult<bool> IsInstalled(Guid avatarId, Guid STARHolonId, string version, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<bool> result = new OASISResult<bool>();
            string errorMessage = "Error occured in STARManagerBase.IsInstalled. Reason: ";

            OASISResult<T3> installedSTARHolonsResult = Data.LoadHolonByMetaData<T3>(new Dictionary<string, string>()
            {
                { STARHolonIdName, STARHolonId.ToString() },
                { "Version", version },
                { "Active", "1" }

            }, MetaKeyValuePairMatchMode.All, STARInstalledHolonType, true, true, 0, true, false, HolonType.All, 0, providerType);

            if (installedSTARHolonsResult != null && !installedSTARHolonsResult.IsError)
            {
                if (installedSTARHolonsResult.Result != null)
                    result.Result = true;
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonByMetaData. Reason: {installedSTARHolonsResult.Message}");

            return result;
        }

        public async Task<OASISResult<bool>> IsInstalledAsync(Guid avatarId, string name, int versionSequence, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<bool> result = new OASISResult<bool>();
            string errorMessage = "Error occured in STARManagerBase.IsInstalledAsync. Reason: ";

            OASISResult<T3> installedSTARHolonsResult = await Data.LoadHolonByMetaDataAsync<T3>(new Dictionary<string, string>()
            {
                { STARHolonNameName, name},
                { "VersionSequence", versionSequence.ToString() },
                { "Active", "1" }

            }, MetaKeyValuePairMatchMode.All, STARInstalledHolonType, true, true, 0, true, 0, false, HolonType.All, providerType);

            if (installedSTARHolonsResult != null && !installedSTARHolonsResult.IsError)
            {
                if (installedSTARHolonsResult.Result != null)
                    result.Result = true;
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonByMetaDataAsync. Reason: {installedSTARHolonsResult.Message}");

            return result;
        }

        public OASISResult<bool> IsInstalled(Guid avatarId, string name, int versionSequence, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<bool> result = new OASISResult<bool>();
            string errorMessage = "Error occured in STARManagerBase.IsInstalled. Reason: ";

            OASISResult<T3> installedSTARHolonsResult = Data.LoadHolonByMetaData<T3>(new Dictionary<string, string>()
            {
                { STARHolonNameName, name },
                { "VersionSequence", versionSequence.ToString() },
                { "Active", "1" }

            }, MetaKeyValuePairMatchMode.All, STARInstalledHolonType, true, true, 0, true, false, HolonType.All, 0, providerType);

            if (installedSTARHolonsResult != null && !installedSTARHolonsResult.IsError)
            {
                if (installedSTARHolonsResult.Result != null)
                    result.Result = true;
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonByMetaData. Reason: {installedSTARHolonsResult.Message}");

            return result;
        }

        public async Task<OASISResult<bool>> IsInstalledAsync(Guid avatarId, string name, string version, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<bool> result = new OASISResult<bool>();
            string errorMessage = "Error occured in STARManagerBase.IsInstalledAsync. Reason: ";

            OASISResult<T3> installedSTARHolonsResult = await Data.LoadHolonByMetaDataAsync<T3>(new Dictionary<string, string>()
            {
                { STARHolonNameName, name},
                { "Version", version.ToString() },
                { "Active", "1" }

            }, MetaKeyValuePairMatchMode.All, STARInstalledHolonType, true, true, 0, true, 0, false, HolonType.All, providerType);

            if (installedSTARHolonsResult != null && !installedSTARHolonsResult.IsError)
            {
                if (installedSTARHolonsResult.Result != null)
                    result.Result = true;
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonByMetaDataAsync. Reason: {installedSTARHolonsResult.Message}");

            return result;
        }

        public OASISResult<bool> IsInstalled(Guid avatarId, string name, string version, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<bool> result = new OASISResult<bool>();
            string errorMessage = "Error occured in STARManagerBase.IsInstalled. Reason: ";

            OASISResult<T3> installedSTARHolonsResult = Data.LoadHolonByMetaData<T3>(new Dictionary<string, string>()
            {
                { STARHolonNameName, name },
                { "Version", version },
                { "Active", "1" }

            }, MetaKeyValuePairMatchMode.All, STARInstalledHolonType, true, true, 0, true, false, HolonType.All, 0, providerType);

            if (installedSTARHolonsResult != null && !installedSTARHolonsResult.IsError)
            {
                if (installedSTARHolonsResult.Result != null)
                    result.Result = true;
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonByMetaData. Reason: {installedSTARHolonsResult.Message}");

            return result;
        }

        public async Task<OASISResult<T3>> LoadInstalledAsync(Guid avatarId, Guid STARHolonId, int versionSequence = 0, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T3> result = new OASISResult<T3>();
            string errorMessage = "Error occured in STARManagerBase.LoadInstalledAsync. Reason: ";
            OASISResult<T3> installedSTARHolonsResult = await Data.LoadHolonByMetaDataAsync<T3>(new Dictionary<string, string>()
            {
                { STARHolonIdName, STARHolonId.ToString() },
                { "VersionSequence", versionSequence.ToString() }

            }, MetaKeyValuePairMatchMode.All, STARInstalledHolonType, true, true, 0, true, 0, false, HolonType.All, providerType);

            if (installedSTARHolonsResult != null && !installedSTARHolonsResult.IsError && installedSTARHolonsResult.Result != null)
                result.Result = installedSTARHolonsResult.Result;
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonByMetaDataAsync. Reason: {installedSTARHolonsResult.Message}");

            return result;
        }

        public OASISResult<T3> LoadInstalled(Guid avatarId, Guid STARHolonId, int versionSequence = 0, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T3> result = new OASISResult<T3>();
            string errorMessage = "Error occured in STARManagerBase.LoadInstalled. Reason: ";
            OASISResult<T3> installedSTARHolonsResult = Data.LoadHolonByMetaData<T3>(new Dictionary<string, string>()
            {
                { STARHolonIdName, STARHolonId.ToString() },
                { "VersionSequence", versionSequence.ToString() }

            }, MetaKeyValuePairMatchMode.All, STARInstalledHolonType, version: versionSequence, providerType: providerType);

            if (installedSTARHolonsResult != null && !installedSTARHolonsResult.IsError && installedSTARHolonsResult.Result != null)
                result.Result = installedSTARHolonsResult.Result;
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonByMetaDataAsync. Reason: {installedSTARHolonsResult.Message}");

            return result;
        }

        public async Task<OASISResult<T3>> LoadInstalledAsync(Guid avatarId, string name, int versionSequence = 0, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T3> result = new OASISResult<T3>();
            string errorMessage = "Error occured in STARManagerBase.LoadInstalledAsync. Reason: ";
            OASISResult<T3> installedSTARHolonsResult = await Data.LoadHolonByMetaDataAsync<T3>(new Dictionary<string, string>()
            {
                { STARHolonNameName, name },
                { "VersionSequence", versionSequence.ToString() }

            }, MetaKeyValuePairMatchMode.All, STARInstalledHolonType, true, true, 0, true, 0, false, HolonType.All, providerType);

            if (installedSTARHolonsResult != null && !installedSTARHolonsResult.IsError && installedSTARHolonsResult.Result != null)
                result.Result = installedSTARHolonsResult.Result;
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonByMetaDataAsync. Reason: {installedSTARHolonsResult.Message}");

            return result;
        }

        public OASISResult<T3> LoadInstalled(Guid avatarId, string name, int versionSequence = 0, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T3> result = new OASISResult<T3>();
            string errorMessage = "Error occured in STARManagerBase.LoadInstalled. Reason: ";
            OASISResult<T3> installedSTARHolonsResult = Data.LoadHolonByMetaData<T3>(new Dictionary<string, string>()
            {
                { STARHolonNameName, name },
                { "VersionSequence", versionSequence.ToString() }

            }, MetaKeyValuePairMatchMode.All, STARInstalledHolonType, version: versionSequence, providerType: providerType);

            if (installedSTARHolonsResult != null && !installedSTARHolonsResult.IsError && installedSTARHolonsResult.Result != null)
                result.Result = installedSTARHolonsResult.Result;
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonByMetaDataAsync. Reason: {installedSTARHolonsResult.Message}");

            return result;
        }

        public async Task<OASISResult<T3>> LoadInstalledAsync(Guid avatarId, Guid STARHolonId, string version, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T3> result = new OASISResult<T3>();
            string errorMessage = "Error occured in STARManagerBase.LoadInstalledAsync. Reason: ";
            OASISResult<T3> installedSTARHolonsResult = await Data.LoadHolonByMetaDataAsync<T3>(new Dictionary<string, string>()
            {
                { STARHolonIdName, STARHolonId.ToString() },
                { "Version", version }

            }, MetaKeyValuePairMatchMode.All, STARInstalledHolonType, true, true, 0, true, 0, false, HolonType.All, providerType);

            if (installedSTARHolonsResult != null && !installedSTARHolonsResult.IsError && installedSTARHolonsResult.Result != null)
                result.Result = installedSTARHolonsResult.Result;
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonByMetaDataAsync. Reason: {installedSTARHolonsResult.Message}");

            return result;
        }

        public OASISResult<T3> LoadInstalled(Guid avatarId, Guid STARHolonId, string version, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T3> result = new OASISResult<T3>();
            string errorMessage = "Error occured in STARManagerBase.LoadInstalled. Reason: ";
            OASISResult<T3> installedSTARHolonsResult = Data.LoadHolonByMetaData<T3>(new Dictionary<string, string>()
            {
                { STARHolonIdName, STARHolonId.ToString() },
                { "Version", version }

            }, MetaKeyValuePairMatchMode.All, STARInstalledHolonType, providerType: providerType);

            if (installedSTARHolonsResult != null && !installedSTARHolonsResult.IsError && installedSTARHolonsResult.Result != null)
                result.Result = installedSTARHolonsResult.Result;
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonByMetaDataAsync. Reason: {installedSTARHolonsResult.Message}");

            return result;
        }

        public async Task<OASISResult<T3>> LoadInstalledAsync(Guid avatarId, string name, string version, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T3> result = new OASISResult<T3>();
            string errorMessage = "Error occured in STARManagerBase.LoadInstalledAsync. Reason: ";
            OASISResult<T3> installedSTARHolonsResult = await Data.LoadHolonByMetaDataAsync<T3>(new Dictionary<string, string>()
            {
                { STARHolonNameName, name },
                { "Version", version }

            }, MetaKeyValuePairMatchMode.All, STARInstalledHolonType, true, true, 0, true, 0, false, HolonType.All, providerType);

            if (installedSTARHolonsResult != null && !installedSTARHolonsResult.IsError && installedSTARHolonsResult.Result != null)
                result.Result = installedSTARHolonsResult.Result;
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonByMetaDataAsync. Reason: {installedSTARHolonsResult.Message}");

            return result;
        }

        public OASISResult<T3> LoadInstalled(Guid avatarId, string name, string version, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T3> result = new OASISResult<T3>();
            string errorMessage = "Error occured in STARManagerBase.LoadInstalled. Reason: ";
            OASISResult<T3> installedSTARHolonsResult = Data.LoadHolonByMetaData<T3>(new Dictionary<string, string>()
            {
                { STARHolonNameName, name },
                { "Version", version }

            }, MetaKeyValuePairMatchMode.All, STARInstalledHolonType, providerType: providerType);

            if (installedSTARHolonsResult != null && !installedSTARHolonsResult.IsError && installedSTARHolonsResult.Result != null)
                result.Result = installedSTARHolonsResult.Result;
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonByMetaDataAsync. Reason: {installedSTARHolonsResult.Message}");

            return result;
        }

        public async Task<OASISResult<T3>> LoadInstalledAsync(Guid avatarId, Guid STARHolonId, bool active, int versionSequence = 0, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T3> result = new OASISResult<T3>();
            string errorMessage = "Error occured in STARManagerBase.LoadInstalledAsync. Reason: ";
            OASISResult<T3> installedSTARHolonsResult = await Data.LoadHolonByMetaDataAsync<T3>(new Dictionary<string, string>()
            {
                { STARHolonIdName, STARHolonId.ToString() },
                { "VersionSequence", versionSequence.ToString() },
                { "Active", active == true ? "1" : "0" }

            }, MetaKeyValuePairMatchMode.All, STARInstalledHolonType, true, true, 0, true, 0, false, HolonType.All, providerType);

            if (installedSTARHolonsResult != null && !installedSTARHolonsResult.IsError && installedSTARHolonsResult.Result != null)
                result.Result = installedSTARHolonsResult.Result;
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonByMetaDataAsync. Reason: {installedSTARHolonsResult.Message}");

            return result;
        }

        public OASISResult<T3> LoadInstalled(Guid avatarId, Guid STARHolonId, bool active, int versionSequence = 0, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T3> result = new OASISResult<T3>();
            string errorMessage = "Error occured in STARManagerBase.LoadInstalled. Reason: ";
            OASISResult<T3> installedSTARHolonsResult = Data.LoadHolonByMetaData<T3>(new Dictionary<string, string>()
            {
                { STARHolonIdName, STARHolonId.ToString() },
                { "VersionSequence", versionSequence.ToString() },
                { "Active", active == true ? "1" : "0" }

            }, MetaKeyValuePairMatchMode.All, STARInstalledHolonType, true, true, 0, true, false, HolonType.All, 0, providerType);

            if (installedSTARHolonsResult != null && !installedSTARHolonsResult.IsError && installedSTARHolonsResult.Result != null)
                result.Result = installedSTARHolonsResult.Result;
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonByMetaData. Reason: {installedSTARHolonsResult.Message}");

            return result;
        }

        public async Task<OASISResult<T3>> LoadInstalledAsync(Guid avatarId, string name, bool active, int versionSequence = 0, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T3> result = new OASISResult<T3>();
            string errorMessage = "Error occured in STARManagerBase.LoadInstalledAsync. Reason: ";
            OASISResult<T3> installedSTARHolonsResult = await Data.LoadHolonByMetaDataAsync<T3>(new Dictionary<string, string>()
            {
                { STARHolonNameName, name },
                { "VersionSequence", versionSequence.ToString() },
                { "Active", active == true ? "1" : "0" }

            }, MetaKeyValuePairMatchMode.All, STARInstalledHolonType, true, true, 0, true, 0, false, HolonType.All, providerType);
            if (installedSTARHolonsResult != null && !installedSTARHolonsResult.IsError && installedSTARHolonsResult.Result != null)
                result.Result = installedSTARHolonsResult.Result;
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonByMetaDataAsync. Reason: {installedSTARHolonsResult.Message}");

            return result;
        }

        public OASISResult<T3> LoadInstalled(Guid avatarId, string name, bool active, int versionSequence = 0, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T3> result = new OASISResult<T3>();
            string errorMessage = "Error occured in STARManagerBase.LoadInstalled. Reason: ";
            OASISResult<T3> installedSTARHolonsResult = Data.LoadHolonByMetaData<T3>(new Dictionary<string, string>()
            {
                { STARHolonNameName, name },
                { "VersionSequence", versionSequence.ToString() },
                { "Active", active == true ? "1" : "0" }

            }, MetaKeyValuePairMatchMode.All, STARInstalledHolonType, true, true, 0, true, false, HolonType.All, 0, providerType);

            if (installedSTARHolonsResult != null && !installedSTARHolonsResult.IsError && installedSTARHolonsResult.Result != null)
                result.Result = installedSTARHolonsResult.Result;
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonByMetaData. Reason: {installedSTARHolonsResult.Message}");

            return result;
        }

        public async Task<OASISResult<T3>> LoadInstalledAsync(Guid avatarId, Guid STARHolonId, string version, bool active, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T3> result = new OASISResult<T3>();
            string errorMessage = "Error occured in STARManagerBase.LoadInstalledAsync. Reason: ";
            OASISResult<T3> installedSTARHolonsResult = await Data.LoadHolonByMetaDataAsync<T3>(new Dictionary<string, string>()
            {
                { STARHolonIdName, STARHolonId.ToString() },
                { "Version", version},
                { "Active", active == true ? "1" : "0" }

            }, MetaKeyValuePairMatchMode.All, STARInstalledHolonType, true, true, 0, true, 0, false, HolonType.All, providerType);

            if (installedSTARHolonsResult != null && !installedSTARHolonsResult.IsError && installedSTARHolonsResult.Result != null)
                result.Result = installedSTARHolonsResult.Result;
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonByMetaDataAsync. Reason: {installedSTARHolonsResult.Message}");

            return result;
        }

        public OASISResult<T3> LoadInstalled(Guid avatarId, Guid STARHolonId, string version, bool active, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T3> result = new OASISResult<T3>();
            string errorMessage = "Error occured in STARManagerBase.LoadInstalled. Reason: ";
            OASISResult<T3> installedSTARHolonsResult = Data.LoadHolonByMetaData<T3>(new Dictionary<string, string>()
            {
                { STARHolonIdName, STARHolonId.ToString() },
                { "Version", version },
                { "Active", active == true ? "1" : "0" }

            }, MetaKeyValuePairMatchMode.All, STARInstalledHolonType, true, true, 0, true, false, HolonType.All, 0, providerType);

            if (installedSTARHolonsResult != null && !installedSTARHolonsResult.IsError && installedSTARHolonsResult.Result != null)
                result.Result = installedSTARHolonsResult.Result;
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonByMetaData. Reason: {installedSTARHolonsResult.Message}");

            return result;
        }

        public async Task<OASISResult<T3>> LoadInstalledAsync(Guid avatarId, string STARHolonName, string version, bool active, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T3> result = new OASISResult<T3>();
            string errorMessage = "Error occured in STARManagerBase.LoadInstalledAsync. Reason: ";
            OASISResult<T3> installedSTARHolonsResult = await Data.LoadHolonByMetaDataAsync<T3>(new Dictionary<string, string>()
            {
                { STARHolonNameName, STARHolonName },
                { "Version", version },
                { "Active", active == true ? "1" : "0" }

            }, MetaKeyValuePairMatchMode.All, STARInstalledHolonType, true, true, 0, true, 0, false, HolonType.All, providerType);

            if (installedSTARHolonsResult != null && !installedSTARHolonsResult.IsError && installedSTARHolonsResult.Result != null)
                result.Result = installedSTARHolonsResult.Result;
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonByMetaDataAsync. Reason: {installedSTARHolonsResult.Message}");

            return result;
        }

        public OASISResult<T3> LoadInstalled(Guid avatarId, string STARHolonName, string version, bool active, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T3> result = new OASISResult<T3>();
            string errorMessage = "Error occured in STARManagerBase.LoadInstalled. Reason: ";
            OASISResult<T3> installedSTARHolonsResult = Data.LoadHolonByMetaData<T3>(new Dictionary<string, string>()
            {
                { STARHolonNameName, STARHolonName },
                { "Version", version },
                { "Active", active == true ? "1" : "0" }

            }, MetaKeyValuePairMatchMode.All, STARInstalledHolonType, true, true, 0, true, false, HolonType.All, 0, providerType);

            if (installedSTARHolonsResult != null && !installedSTARHolonsResult.IsError && installedSTARHolonsResult.Result != null)
                result.Result = installedSTARHolonsResult.Result;
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonByMetaData. Reason: {installedSTARHolonsResult.Message}");

            return result;
        }

        public OASISResult<T3> OpenSTARHolonFolder(Guid avatarId, T3 holon)
        {
            OASISResult<T3> result = new OASISResult<T3>();
            string errorMessage = "An error occured in STARManagerBase.OpenSTARHolonFolder. Reason:";

            if (holon != null)
            {
                try
                {
                    if (!string.IsNullOrEmpty(holon.InstalledPath))
                        Process.Start("explorer.exe", holon.InstalledPath);

                    else if (!string.IsNullOrEmpty(holon.DownloadedPath))
                        Process.Start("explorer.exe", new FileInfo(holon.DownloadedPath).DirectoryName);
                }
                catch (Exception e)
                {
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured attempting to open the folder {result.Result.InstalledPath}. Reason: {e}");
                }
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} The {STARHolonUIName} is null!");

            return result;
        }

        public async Task<OASISResult<T3>> OpenSTARHolonFolderAsync(Guid avatarId, Guid STARHolonId, int versionSequence = 0, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T3> result = new OASISResult<T3>();
            string errorMessage = "An error occured in STARManagerBase.OpenSTARHolonFolderAsync. Reason:";
            result = await LoadInstalledAsync(avatarId, STARHolonId, versionSequence, providerType);

            if (result != null && !result.IsError && result.Result != null)
                OpenSTARHolonFolder(avatarId, result.Result);
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the {STARHolonUIName} with the LoadInstalledSTARHolonAsync method, reason: {result.Message}");

            return result;
        }

        public OASISResult<T3> OpenSTARHolonFolder(Guid avatarId, Guid STARHolonId, int versionSequence = 0, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T3> result = new OASISResult<T3>();
            string errorMessage = "An error occured in STARManagerBase.OpenSTARHolonFolder. Reason:";
            result = LoadInstalled(avatarId, STARHolonId, versionSequence);

            if (result != null && !result.IsError && result.Result != null)
                OpenSTARHolonFolder(avatarId, result.Result);
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the {STARHolonUIName} with the LoadInstalledSTARHolon method, reason: {result.Message}");

            return result;
        }

        public async Task<OASISResult<T3>> OpenSTARHolonFolderAsync(Guid avatarId, Guid STARHolonId, string version, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T3> result = new OASISResult<T3>();
            string errorMessage = "An error occured in STARManagerBase.OpenSTARHolonFolderAsync. Reason:";
            result = await LoadInstalledAsync(avatarId, STARHolonId, version, providerType);

            if (result != null && !result.IsError && result.Result != null)
                OpenSTARHolonFolder(avatarId, result.Result);
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the {STARHolonUIName} with the LoadInstalledSTARHolonAsync method, reason: {result.Message}");

            return result;
        }

        public OASISResult<T3> OpenSTARHolonFolder(Guid avatarId, Guid STARHolonId, string version, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T3> result = new OASISResult<T3>();
            string errorMessage = "An error occured in STARManagerBase.OpenSTARHolonFolder. Reason:";
            result = LoadInstalled(avatarId, STARHolonId, version, providerType);

            if (result != null && !result.IsError && result.Result != null)
                OpenSTARHolonFolder(avatarId, result.Result);
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the {STARHolonUIName} with the LoadInstalledSTARHolon method, reason: {result.Message}");

            return result;
        }


        //private ISTARHolonDNA ConvertSTARHolonToSTARHolonDNA(ISTARHolon T)
        //{
        //    STARHolonDNA STARHolonDNA = new STARHolonDNA()
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
        //        STARHolonId = T.Id,
        //        STARHolonName = T.Name,
        //        STARHolonType = T.STARHolonType,
        //        PublishedByAvatarId = T.PublishedByAvatarId,
        //        PublishedByAvatarUsername = T.PublishedByAvatarUsername,
        //        PublishedOn = T.PublishedOn,
        //        PublishedOnSTARNET = T.PublishedSTARHolon != null,
        //        Version = T.Version.ToString()
        //    };

        //    List<IZome> zomes = new List<IZome>();
        //    foreach (IHolon holon in T.Children)
        //        zomes.Add((IZome)holon);

        //   //STARHolonDNA.Zomes = zomes;
        //    return STARHolonDNA;
        //}

        public async Task<OASISResult<bool>> WriteDNAAsync<T>(T STARHolonDNA, string fullPathToSTARHolon)
        {
            OASISResult<bool> result = new OASISResult<bool>();

            try
            {
                JsonSerializerOptions options = new()
                {
                    ReferenceHandler = ReferenceHandler.Preserve,
                    WriteIndented = true
                };

                if (!Directory.Exists(fullPathToSTARHolon))
                    Directory.CreateDirectory(fullPathToSTARHolon);

                await File.WriteAllTextAsync(Path.Combine(fullPathToSTARHolon, STARHolonDNAFileName), JsonSerializer.Serialize(STARHolonDNA, options));
                result.Result = true;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"An error occured writing the {STARHolonUIName} DNA in WriteDNAAsync: Reason: {ex.Message}");
            }

            return result;
        }

        public OASISResult<bool> WriteDNA<T>(T STARHolonDNA, string fullPathToSTARHolon)
        {
            OASISResult<bool> result = new OASISResult<bool>();

            try
            {
                if (!Directory.Exists(fullPathToSTARHolon))
                    Directory.CreateDirectory(fullPathToSTARHolon);

                File.WriteAllText(Path.Combine(fullPathToSTARHolon, STARHolonDNAFileName), JsonSerializer.Serialize(STARHolonDNA));
                result.Result = true;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"An error occured writing the {STARHolonUIName} DNA in WriteDNA: Reason: {ex.Message}");
            }

            return result;
        }

        //public async Task<OASISResult<ISTARHolonDNA>> ReadDNAFromSourceOrInstallFolderAsync(string fullPathToSTARHolonFolder)
        //{
        //    OASISResult<ISTARHolonDNA> result = new OASISResult<ISTARHolonDNA>();

        //    try
        //    {
        //        result.Result = JsonSerializer.Deserialize<STARHolonDNA>(await File.ReadAllTextAsync(Path.Combine(fullPathToSTARHolonFolder, STARHolonDNAFileName)));
        //    }
        //    catch (Exception ex)
        //    {
        //        OASISErrorHandling.HandleError(ref result, $"An error occured reading the {STARHolonDNAFileName} in the {fullPathToSTARHolonFolder} folder in ReadDNAFromSourceOrInstallFolderAsync: Reason: {ex.Message}");
        //    }

        //    return result;
        //}

        //public OASISResult<ISTARHolonDNA> ReadDNAFromSourceOrInstallFolder(string fullPathToSTARHolonFolder)
        //{
        //    OASISResult<ISTARHolonDNA> result = new OASISResult<ISTARHolonDNA>();

        //    try
        //    {
        //        result.Result = JsonSerializer.Deserialize<STARHolonDNA>(File.ReadAllText(Path.Combine(fullPathToSTARHolonFolder, STARHolonDNAFileName)));
        //    }
        //    catch (Exception ex)
        //    {
        //        OASISErrorHandling.HandleError(ref result, $"An error occured reading the {STARHolonDNAFileName} in the {fullPathToSTARHolonFolder} folder in ReadDNAFromSourceOrInstallFolder: Reason: {ex.Message}");
        //    }

        //    return result;
        //}

        //public async Task<OASISResult<ISTARHolonDNA>> ReadSTARHolonDNAFromPublishedFileAsync(string fullPathToPublishedFile)
        //{
        //    OASISResult<ISTARHolonDNA> result = new OASISResult<ISTARHolonDNA>();
        //    string tempPath = "";

        //    try
        //    {
        //        tempPath = Path.GetTempPath();
        //        tempPath = Path.Combine(tempPath, "tmp_oapp_system_holon");

        //        if (Directory.Exists(tempPath))
        //            Directory.Delete(tempPath, true);

        //        ZipFile.ExtractToDirectory(fullPathToPublishedFile, tempPath, Encoding.Default, true);

        //        result.Result = JsonSerializer.Deserialize<STARHolonDNA>(await File.ReadAllTextAsync(Path.Combine(tempPath, STARHolonDNAFileName)));
        //    }
        //    catch (Exception e)
        //    {
        //        OASISErrorHandling.HandleError(ref result, $"An error occured reading the {STARHolonDNAFileName} in the {fullPathToPublishedFile} file in ReadSTARHolonDNAFromPublishedFile: Reason: {e.Message}");
        //    }
        //    finally
        //    {
        //        if (Directory.Exists(tempPath))
        //            Directory.Delete(tempPath, true);
        //    }

        //    return result;
        //}

        //public OASISResult<ISTARHolonDNA> ReadSTARHolonDNAFromPublishedFile(string fullPathToPublishedFile)
        //{
        //    OASISResult<ISTARHolonDNA> result = new OASISResult<ISTARHolonDNA>();
        //    string tempPath = "";

        //    try
        //    {
        //        tempPath = Path.GetTempPath();
        //        tempPath = Path.Combine(tempPath, "tmp_oapp_system_holon");

        //        if (Directory.Exists(tempPath))
        //            Directory.Delete(tempPath, true);

        //        ZipFile.ExtractToDirectory(fullPathToPublishedFile, tempPath, Encoding.Default, true);

        //        result.Result = JsonSerializer.Deserialize<STARHolonDNA>(File.ReadAllText(Path.Combine(tempPath, STARHolonDNAFileName)));
        //    }
        //    catch (Exception e)
        //    {
        //        OASISErrorHandling.HandleError(ref result, $"An error occured reading the {STARHolonDNAFileName} in the {fullPathToPublishedFile} file in ReadSTARHolonDNAFromPublishedFile: Reason: {e.Message}");
        //    }
        //    finally
        //    {
        //        if (Directory.Exists(tempPath))
        //            Directory.Delete(tempPath, true);
        //    }

        //    return result;
        //}

        public async Task<OASISResult<T>> ReadDNAFromSourceOrInstallFolderAsync<T>(string fullPathToSTARHolonFolder)
        {
            OASISResult<T> result = new OASISResult<T>();

            try
            {
                result.Result = JsonSerializer.Deserialize<T>(await File.ReadAllTextAsync(Path.Combine(fullPathToSTARHolonFolder, STARHolonDNAFileName)));
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"An error occured reading the {STARHolonDNAFileName} in the {fullPathToSTARHolonFolder} folder in ReadDNAFromSourceOrInstallFolderAsync: Reason: {ex.Message}");
            }

            return result;
        }

        public OASISResult<T> ReadDNAFromSourceOrInstallFolder<T>(string fullPathToSTARHolonFolder)
        {
            OASISResult<T> result = new OASISResult<T>();

            try
            {
                result.Result = JsonSerializer.Deserialize<T>(File.ReadAllText(Path.Combine(fullPathToSTARHolonFolder, STARHolonDNAFileName)));
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"An error occured reading the {STARHolonDNAFileName} in the {fullPathToSTARHolonFolder} folder in ReadDNAFromSourceOrInstallFolder: Reason: {ex.Message}");
            }

            return result;
        }

        public async Task<OASISResult<T>> ReadDNAFromPublishedFileAsync<T>(string fullPathToPublishedFile)
        {
            OASISResult<T> result = new OASISResult<T>();
            string tempPath = "";

            try
            {
                tempPath = Path.GetTempPath();
                tempPath = Path.Combine(tempPath, "tmp_oapp_system_holon");

                if (Directory.Exists(tempPath))
                    Directory.Delete(tempPath, true);

                ZipFile.ExtractToDirectory(fullPathToPublishedFile, tempPath, Encoding.Default, true);

                result.Result = JsonSerializer.Deserialize<T>(await File.ReadAllTextAsync(Path.Combine(tempPath, STARHolonDNAFileName)));
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"An error occured reading the {STARHolonDNAFileName} in the {fullPathToPublishedFile} file in ReadSTARHolonDNAFromPublishedFile: Reason: {e.Message}");
            }
            finally
            {
                if (Directory.Exists(tempPath))
                    Directory.Delete(tempPath, true);
            }

            return result;
        }

        public OASISResult<T> ReadDNAFromPublishedFile<T>(string fullPathToPublishedFile)
        {
            OASISResult<T> result = new OASISResult<T>();
            string tempPath = "";

            try
            {
                tempPath = Path.GetTempPath();
                tempPath = Path.Combine(tempPath, "tmp_oapp_system_holon");

                if (Directory.Exists(tempPath))
                    Directory.Delete(tempPath, true);

                ZipFile.ExtractToDirectory(fullPathToPublishedFile, tempPath, Encoding.Default, true);

                result.Result = JsonSerializer.Deserialize<T>(File.ReadAllText(Path.Combine(tempPath, STARHolonDNAFileName)));
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"An error occured reading the {STARHolonDNAFileName} in the {fullPathToPublishedFile} file in ReadSTARHolonDNAFromPublishedFile: Reason: {e.Message}");
            }
            finally
            {
                if (Directory.Exists(tempPath))
                    Directory.Delete(tempPath, true);
            }

            return result;
        }

        public OASISResult<bool> ValidateVersion(string dnaVersion, string storedVersion, string fullPathToSTARHolonFolder, bool firstPublish, bool edit)
        {
            OASISResult<bool> result = new OASISResult<bool>();
            int dnaVersionInt = 0;
            int stotedVersionInt = 0;

            if (!firstPublish)
            {
                if (edit && dnaVersion != storedVersion)
                {
                    OASISErrorHandling.HandleError(ref result, $"The version in the {STARHolonUIName} DNA (v{dnaVersion}) is not the same as the version you are attempting to edit (v{storedVersion}). They must be the same if you wish to upload new files for version v{storedVersion}. Please edit the {STARHolonDNAFileName} file found in the root of your {STARHolonUIName} folder ({fullPathToSTARHolonFolder}).");
                    return result;
                }
                else
                {
                    if (!StringHelper.IsValidVersion(dnaVersion))
                    {
                        OASISErrorHandling.HandleError(ref result, $"The version in the {STARHolonUIName} DNA (v{dnaVersion}) is not valid! Please make sure you enter a valid version in the form of MM.mm.rr (Major.Minor.Revision) in the {STARHolonDNAFileName} file found in the root of your {STARHolonUIName} folder ({fullPathToSTARHolonFolder}).");
                        return result;
                    }

                    if (dnaVersion == storedVersion)
                    {
                        OASISErrorHandling.HandleError(ref result, $"The version in the {STARHolonUIName} DNA (v{dnaVersion}) is the same as the previous version ({storedVersion}). Please make sure you increment the version in the {STARHolonDNAFileName} file found in the root of your {STARHolonUIName} folder ({fullPathToSTARHolonFolder}).");
                        return result;
                    }

                    if (!int.TryParse(dnaVersion.Replace(".", ""), out dnaVersionInt))
                    {
                        OASISErrorHandling.HandleError(ref result, $"The version in the {STARHolonUIName} DNA (v{dnaVersion}) is not valid! Please make sure you enter a valid version in the form of MM.mm.rr (Major.Minor.Revision) in the {STARHolonDNAFileName} file found in the root of your {STARHolonUIName} folder ({fullPathToSTARHolonFolder}).");
                        return result;
                    }

                    //Should hopefully never occur! ;-)
                    if (!int.TryParse(storedVersion.Replace(".", ""), out stotedVersionInt))
                        OASISErrorHandling.HandleWarning(ref result, $"The version stored in the OASIS (v{storedVersion}) is not valid!");

                    if (dnaVersionInt <= stotedVersionInt)
                    {
                        OASISErrorHandling.HandleError(ref result, $"The version in the {STARHolonUIName} DNA (v{dnaVersion}) is less than the previous version (v{storedVersion}). Please make sure you increment the version in the {STARHolonDNAFileName} file found in the root of your {STARHolonUIName} folder.");
                        return result;
                    }
                }
            }

            result.Result = true;
            return result;
        }

        public async Task<OASISResult<T1>> UpdateNumberOfVersionCountsAsync(Guid avatarId, OASISResult<T1> result, string errorMessage, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<T1>> versionsResult = await LoadVersionsAsync(result.Result.STARHolonDNA.Id, providerType);

            if (versionsResult != null && versionsResult.Result != null && !versionsResult.IsError)
            {
                foreach (T1 holonVersion in versionsResult.Result)
                {
                    holonVersion.STARHolonDNA.NumberOfVersions = result.Result.STARHolonDNA.NumberOfVersions;
                    OASISResult<T1> versionSaveResult = await SaveAsync(avatarId, holonVersion, providerType);

                    if (!(versionSaveResult != null && versionSaveResult.Result != null && !versionSaveResult.IsError))
                        OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} Error occured updating the NumberOfVersions for {STARHolonUIName} with Id {holonVersion.Id} for provider {Enum.GetName(typeof(ProviderType), providerType)}. Reason: {versionSaveResult.Message}");
                }
            }
            else
                OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} Error occured updating the total installs for all {STARHolonUIName} versions caused by an error in LoadSTARHolonVersionsAsync. Reason: {versionsResult.Message}");


            OASISResult<IEnumerable<T3>> installedversionsResult = await ListInstalledAsync(avatarId, providerType);

            if (installedversionsResult != null && installedversionsResult.Result != null && !installedversionsResult.IsError)
            {
                foreach (T3 installedSTARHolon in installedversionsResult.Result)
                {
                    installedSTARHolon.STARHolonDNA.NumberOfVersions = result.Result.STARHolonDNA.NumberOfVersions;
                    OASISResult<T3> installedSTARSaveResult = await SaveAsync(avatarId, installedSTARHolon, providerType);

                    if (!(installedSTARSaveResult != null && installedSTARSaveResult.Result != null && !installedSTARSaveResult.IsError))
                        OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} Error occured updating the NumberOfVersions for Installed {STARHolonUIName} with Id {installedSTARHolon.Id} for provider {Enum.GetName(typeof(ProviderType), providerType)}. Reason: {installedSTARSaveResult.Message}");
                }
            }
            else
                OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} Error occured updating the NumberOfVersions for all Installed {STARHolonUIName} versions caused by an error in ListInstalledSTARHolonsAsync. Reason: {versionsResult.Message}");

            return result;
        }

        public OASISResult<T1> UpdateNumberOfVersionCounts(Guid avatarId, OASISResult<T1> result, string errorMessage, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<T1>> versionsResult = LoadVersions(result.Result.STARHolonDNA.Id, providerType);

            if (versionsResult != null && versionsResult.Result != null && !versionsResult.IsError)
            {
                foreach (T1 holonVersion in versionsResult.Result)
                {
                    holonVersion.STARHolonDNA.NumberOfVersions = result.Result.STARHolonDNA.NumberOfVersions;
                    OASISResult<T1> versionSaveResult = Save(avatarId, holonVersion, providerType);

                    if (!(versionSaveResult != null && versionSaveResult.Result != null && !versionSaveResult.IsError))
                        OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} Error occured updating the NumberOfVersions for {STARHolonUIName} with Id {holonVersion.Id} for provider {Enum.GetName(typeof(ProviderType), providerType)}. Reason: {versionSaveResult.Message}");
                }
            }
            else
                OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} Error occured updating the total installs for all {STARHolonUIName} versions caused by an error in LoadSTARHolonVersionsAsync. Reason: {versionsResult.Message}");


            OASISResult<IEnumerable<T3>> installedversionsResult = ListInstalled(avatarId, providerType);

            if (installedversionsResult != null && installedversionsResult.Result != null && !installedversionsResult.IsError)
            {
                foreach (T3 installedSTARHolon in installedversionsResult.Result)
                {
                    installedSTARHolon.STARHolonDNA.NumberOfVersions = result.Result.STARHolonDNA.NumberOfVersions;
                    OASISResult<T3> installedSTARSaveResult = Save(avatarId, installedSTARHolon, providerType);

                    if (!(installedSTARSaveResult != null && installedSTARSaveResult.Result != null && !installedSTARSaveResult.IsError))
                        OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} Error occured updating the NumberOfVersions for Installed {STARHolonUIName} with Id {installedSTARHolon.Id} for provider {Enum.GetName(typeof(ProviderType), providerType)}. Reason: {installedSTARSaveResult.Message}");
                }
            }
            else
                OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} Error occured updating the NumberOfVersions for all Installed {STARHolonUIName} versions caused by an error in ListInstalledSTARHolonsAsync. Reason: {versionsResult.Message}");

            return result;
        }

        public async Task<OASISResult<T2>> UpdateDownloadCountsAsync(Guid avatarId, T2 downloadedSTARHolon, ISTARHolonDNA STARHolonDNA, OASISResult<T2> result, string errorMessage, ProviderType providerType = ProviderType.Default)
        {
            int totalDownloads = 0;
            OASISResult<IEnumerable<T1>> holonVersionsResult = await LoadVersionsAsync(STARHolonDNA.Id, providerType);

            if (holonVersionsResult != null && holonVersionsResult.Result != null && !holonVersionsResult.IsError)
            {
                //Update total installs for all versions.
                foreach (T1 holonVersion in holonVersionsResult.Result)
                    totalDownloads += holonVersion.STARHolonDNA.Downloads;

                //Need to add this download (because its not saved yet).
                totalDownloads++;

                foreach (T1 holonVersion in holonVersionsResult.Result)
                {
                    holonVersion.STARHolonDNA.TotalDownloads = totalDownloads;
                    OASISResult<T1> holonVersionSaveResult = await SaveAsync(avatarId, holonVersion, providerType);

                    if (!(holonVersionSaveResult != null && holonVersionSaveResult.Result != null && !holonVersionSaveResult.IsError))
                        OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} Error occured updating the TotalDownloads for {STARHolonUIName} with Id {holonVersion.Id} for provider {Enum.GetName(typeof(ProviderType), providerType)}. Reason: {holonVersionSaveResult.Message}");
                }

                STARHolonDNA.TotalDownloads = totalDownloads;
                downloadedSTARHolon.STARHolonDNA.TotalDownloads = totalDownloads;
            }
            else
                OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} Error occured updating the total downloads for all {STARHolonUIName} versions caused by an error in LoadSTARHolonVersionsAsync. Reason: {holonVersionsResult.Message}");


            OASISResult<IEnumerable<T3>> installedholonVersionsResult = await ListInstalledAsync(avatarId, providerType);

            if (installedholonVersionsResult != null && installedholonVersionsResult.Result != null && !installedholonVersionsResult.IsError)
            {
                foreach (T3 holonVersion in installedholonVersionsResult.Result)
                {
                    holonVersion.STARHolonDNA.TotalDownloads = totalDownloads;
                    OASISResult<T3> holonVersionSaveResult = await SaveAsync(avatarId, holonVersion, providerType);

                    if (!(holonVersionSaveResult != null && holonVersionSaveResult.Result != null && !holonVersionSaveResult.IsError))
                        OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} Error occured updating the TotalDownloads for Installed {STARHolonUIName} with Id {holonVersion.Id} for provider {Enum.GetName(typeof(ProviderType), providerType)}. Reason: {holonVersionSaveResult.Message}");
                }
            }
            else
                OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} Error occured updating the total downloads for all Installed {STARHolonUIName} versions caused by an error in ListInstalledSTARHolonsAsync. Reason: {holonVersionsResult.Message}");

            return result;
        }

        public OASISResult<T2> UpdateDownloadCounts(Guid avatarId, T2 downloadedSTARHolon, ISTARHolonDNA STARHolonDNA, OASISResult<T2> result, string errorMessage, ProviderType providerType = ProviderType.Default)
        {
            int totalDownloads = 0;
            OASISResult<IEnumerable<T1>> holonVersionsResult = LoadVersions(STARHolonDNA.Id, providerType);

            if (holonVersionsResult != null && holonVersionsResult.Result != null && !holonVersionsResult.IsError)
            {
                //Update total installs for all versions.
                foreach (T1 holonVersion in holonVersionsResult.Result)
                    totalDownloads += holonVersion.STARHolonDNA.Downloads;

                //Need to add this download (because its not saved yet).
                totalDownloads++;

                foreach (T1 holonVersion in holonVersionsResult.Result)
                {
                    holonVersion.STARHolonDNA.TotalDownloads = totalDownloads;
                    OASISResult<T1> holonVersionSaveResult = Save(avatarId, holonVersion, providerType);

                    if (!(holonVersionSaveResult != null && holonVersionSaveResult.Result != null && !holonVersionSaveResult.IsError))
                        OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} Error occured updating the TotalDownloads for {STARHolonUIName} with Id {holonVersion.Id} for provider {Enum.GetName(typeof(ProviderType), providerType)}. Reason: {holonVersionSaveResult.Message}");
                }

                STARHolonDNA.TotalDownloads = totalDownloads;
                downloadedSTARHolon.STARHolonDNA.TotalDownloads = totalDownloads;
            }
            else
                OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} Error occured updating the total downloads for all {STARHolonUIName} versions caused by an error in LoadSTARHolonVersionsAsync. Reason: {holonVersionsResult.Message}");


            OASISResult<IEnumerable<T3>> installedholonVersionsResult = ListInstalled(avatarId, providerType);

            if (installedholonVersionsResult != null && installedholonVersionsResult.Result != null && !installedholonVersionsResult.IsError)
            {
                foreach (T3 holonVersion in installedholonVersionsResult.Result)
                {
                    holonVersion.STARHolonDNA.TotalDownloads = totalDownloads;
                    OASISResult<T3> holonVersionSaveResult = Save(avatarId, holonVersion, providerType);

                    if (!(holonVersionSaveResult != null && holonVersionSaveResult.Result != null && !holonVersionSaveResult.IsError))
                        OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} Error occured updating the TotalDownloads for Installed {STARHolonUIName} with Id {holonVersion.Id} for provider {Enum.GetName(typeof(ProviderType), providerType)}. Reason: {holonVersionSaveResult.Message}");
                }
            }
            else
                OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} Error occured updating the total downloads for all Installed {STARHolonUIName} versions caused by an error in ListInstalledSTARHolonsAsync. Reason: {holonVersionsResult.Message}");

            return result;
        }

        public async Task<OASISResult<T3>> UpdateInstallCountsAsync(Guid avatarId, T3 installedSTARHolon, ISTARHolonDNA STARHolonDNA, OASISResult<T3> result, string errorMessage, ProviderType providerType = ProviderType.Default)
        {
            int totalInstalls = 0;
            OASISResult<IEnumerable<T1>> holonVersionsResult = await LoadVersionsAsync(STARHolonDNA.Id, providerType);

            if (holonVersionsResult != null && holonVersionsResult.Result != null && !holonVersionsResult.IsError)
            {
                //Update total installs for all versions.
                foreach (T1 holonVersion in holonVersionsResult.Result)
                    totalInstalls += holonVersion.STARHolonDNA.Installs;

                //Need to add this install (because its not saved yet).
                totalInstalls++;

                foreach (T1 holonVersion in holonVersionsResult.Result)
                {
                    holonVersion.STARHolonDNA.TotalInstalls = totalInstalls;
                    OASISResult<T1> holonVersionSaveResult = await SaveAsync(avatarId, holonVersion, providerType);

                    if (!(holonVersionSaveResult != null && holonVersionSaveResult.Result != null && !holonVersionSaveResult.IsError))
                        OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} Error occured updating the TotalInstalls for {STARHolonUIName} with Id {holonVersion.Id} for provider {Enum.GetName(typeof(ProviderType), providerType)}. Reason: {holonVersionSaveResult.Message}");
                }

                STARHolonDNA.TotalInstalls = totalInstalls;
                installedSTARHolon.STARHolonDNA.TotalInstalls = totalInstalls;
            }
            else
                OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} Error occured updating the total installs for all {STARHolonUIName} versions caused by an error in LoadSTARHolonVersionsAsync. Reason: {holonVersionsResult.Message}");


            OASISResult<IEnumerable<T3>> installedholonVersionsResult = await ListInstalledAsync(avatarId, providerType);

            if (installedholonVersionsResult != null && installedholonVersionsResult.Result != null && !installedholonVersionsResult.IsError)
            {
                foreach (T3 holonVersion in installedholonVersionsResult.Result)
                {
                    holonVersion.STARHolonDNA.TotalInstalls = totalInstalls;
                    OASISResult<T3> holonVersionSaveResult = await SaveAsync(avatarId, holonVersion, providerType);

                    if (!(holonVersionSaveResult != null && holonVersionSaveResult.Result != null && !holonVersionSaveResult.IsError))
                        OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} Error occured updating the TotalInstalls for Installed {STARHolonUIName} with Id {holonVersion.Id} for provider {Enum.GetName(typeof(ProviderType), providerType)}. Reason: {holonVersionSaveResult.Message}");
                }
            }
            else
                OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} Error occured updating the total installs for all Installed {STARHolonUIName} versions caused by an error in ListInstalledSTARHolonsAsync. Reason: {holonVersionsResult.Message}");


            OASISResult<IEnumerable<T3>> uninstalledholonVersionsResult = await ListUninstalledAsync(avatarId, providerType);

            if (uninstalledholonVersionsResult != null && uninstalledholonVersionsResult.Result != null && !uninstalledholonVersionsResult.IsError)
            {
                foreach (T3 holonVersion in uninstalledholonVersionsResult.Result)
                {
                    holonVersion.STARHolonDNA.TotalInstalls = totalInstalls;
                    OASISResult<T3> holonVersionSaveResult = await SaveAsync(avatarId, holonVersion, providerType);

                    if (!(holonVersionSaveResult != null && holonVersionSaveResult.Result != null && !holonVersionSaveResult.IsError))
                        OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} Error occured updating the TotalInstalls for Uninstalled {STARHolonUIName} with Id {holonVersion.Id} for provider {Enum.GetName(typeof(ProviderType), providerType)}. Reason: {holonVersionSaveResult.Message}");
                }
            }
            else
                OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} Error occured updating the total installs for all Installed {STARHolonUIName} versions caused by an error in ListInstalledSTARHolonsAsync. Reason: {holonVersionsResult.Message}");

            return result;
        }

        public OASISResult<T3> UpdateInstallCounts(Guid avatarId, T3 installedSTARHolon, ISTARHolonDNA STARHolonDNA, OASISResult<T3> result, string errorMessage, ProviderType providerType = ProviderType.Default)
        {
            int totalInstalls = 0;
            OASISResult<IEnumerable<T1>> holonVersionsResult = LoadVersions(STARHolonDNA.Id, providerType);

            if (holonVersionsResult != null && holonVersionsResult.Result != null && !holonVersionsResult.IsError)
            {
                //Update total installs for all versions.
                foreach (T1 holonVersion in holonVersionsResult.Result)
                    totalInstalls += holonVersion.STARHolonDNA.Installs;

                //Need to add this install (because its not saved yet).
                totalInstalls++;

                foreach (T1 holonVersion in holonVersionsResult.Result)
                {
                    holonVersion.STARHolonDNA.TotalInstalls = totalInstalls;
                    OASISResult<T1> holonVersionSaveResult = Save(avatarId, holonVersion, providerType);

                    if (!(holonVersionSaveResult != null && holonVersionSaveResult.Result != null && !holonVersionSaveResult.IsError))
                        OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} Error occured updating the TotalInstalls for {STARHolonUIName} with Id {holonVersion.Id} for provider {Enum.GetName(typeof(ProviderType), providerType)}. Reason: {holonVersionSaveResult.Message}");
                }

                STARHolonDNA.TotalInstalls = totalInstalls;
                installedSTARHolon.STARHolonDNA.TotalInstalls = totalInstalls;
            }
            else
                OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} Error occured updating the total installs for all {STARHolonUIName} versions caused by an error in LoadSTARHolonVersionsAsync. Reason: {holonVersionsResult.Message}");


            OASISResult<IEnumerable<T3>> installedholonVersionsResult = ListInstalled(avatarId, providerType);

            if (installedholonVersionsResult != null && installedholonVersionsResult.Result != null && !installedholonVersionsResult.IsError)
            {
                foreach (T3 holonVersion in installedholonVersionsResult.Result)
                {
                    holonVersion.STARHolonDNA.TotalInstalls = totalInstalls;
                    OASISResult<T3> holonVersionSaveResult = Save(avatarId, holonVersion, providerType);

                    if (!(holonVersionSaveResult != null && holonVersionSaveResult.Result != null && !holonVersionSaveResult.IsError))
                        OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} Error occured updating the TotalInstalls for Installed {STARHolonUIName} with Id {holonVersion.Id} for provider {Enum.GetName(typeof(ProviderType), providerType)}. Reason: {holonVersionSaveResult.Message}");
                }
            }
            else
                OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} Error occured updating the total installs for all Installed {STARHolonUIName} versions caused by an error in ListInstalledSTARHolonsAsync. Reason: {installedholonVersionsResult.Message}");

            OASISResult<IEnumerable<T3>> uninstalledholonVersionsResult = ListUninstalled(avatarId, providerType);


            if (uninstalledholonVersionsResult != null && uninstalledholonVersionsResult.Result != null && !uninstalledholonVersionsResult.IsError)
            {
                foreach (T3 holonVersion in uninstalledholonVersionsResult.Result)
                {
                    holonVersion.STARHolonDNA.TotalInstalls = totalInstalls;
                    OASISResult<T3> holonVersionSaveResult = Save(avatarId, holonVersion, providerType);

                    if (!(holonVersionSaveResult != null && holonVersionSaveResult.Result != null && !holonVersionSaveResult.IsError))
                        OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} Error occured updating the TotalInstalls for Uninstalled {STARHolonUIName} with Id {holonVersion.Id} for provider {Enum.GetName(typeof(ProviderType), providerType)}. Reason: {holonVersionSaveResult.Message}");
                }
            }
            else
                OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} Error occured updating the total installs for all Installed {STARHolonUIName} versions caused by an error in ListInstalledSTARHolonsAsync. Reason: {uninstalledholonVersionsResult.Message}");

            return result;
        }

        private async Task<OASISResult<T3>> UninstallAsync(Guid avatarId, OASISResult<T3> installedSTARHolonResult, string errorMessage, ProviderType providerType)
        {
            OASISResult<T3> result = new OASISResult<T3>();

            if (installedSTARHolonResult != null && !installedSTARHolonResult.IsError && installedSTARHolonResult.Result != null)
                result = await UninstallAsync(avatarId, installedSTARHolonResult.Result, errorMessage, providerType);
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonByMetaDataAsync. Reason: {installedSTARHolonResult.Message}");

            return result;
        }

        private OASISResult<T3> Uninstall(Guid avatarId, OASISResult<T3> installedSTARHolonResult, string errorMessage, ProviderType providerType)
        {
            OASISResult<T3> result = new OASISResult<T3>();

            if (installedSTARHolonResult != null && !installedSTARHolonResult.IsError && installedSTARHolonResult.Result != null)
                result = Uninstall(avatarId, installedSTARHolonResult.Result, errorMessage, providerType);
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonByMetaData. Reason: {installedSTARHolonResult.Message}");

            return result;
        }

        private OASISResult<IEnumerable<T1>> FilterResultsForVersion(Guid avatarId, OASISResult<IEnumerable<T1>> results, bool showAllVersions = false, int version = 0)
        {
            OASISResult<IEnumerable<T1>> result = new OASISResult<IEnumerable<T1>>();
            List<T1> templates = new List<T1>();

            if (!showAllVersions)
            {
                if (results.Result != null && !result.IsError)
                {
                    if (version == 0) //latest version
                    {
                        Dictionary<string, T1> latestVersions = new Dictionary<string, T1>();
                        string metaDataId = "";
                        int latestVersion = 0;
                        int currentVersion = 0;

                        foreach (T1 oappSystemHolon in results.Result)
                        {
                            if (oappSystemHolon.MetaData != null && oappSystemHolon.MetaData.ContainsKey(STARHolonIdName) && oappSystemHolon.MetaData[STARHolonIdName] != null)
                                metaDataId = oappSystemHolon.MetaData[STARHolonIdName].ToString();

                            latestVersion = latestVersions.ContainsKey(metaDataId) ? Convert.ToInt32(latestVersions[metaDataId].STARHolonDNA.Version.Replace(".", "")) : 0;
                            currentVersion = Convert.ToInt32(oappSystemHolon.STARHolonDNA.Version.Replace(".", ""));

                            if (latestVersions.ContainsKey(metaDataId) &&
                                currentVersion > latestVersion
                                //oappSystemHolon.STARHolonDNA.CreatedOn > latestVersions[metaDataId].STARHolonDNA.CreatedOn)
                                || !latestVersions.ContainsKey(metaDataId))
                                latestVersions[metaDataId] = oappSystemHolon;
                        }

                        result.Result = latestVersions.Values.ToList();
                    }
                    else
                    {
                        List<T1> filteredList = new List<T1>();

                        foreach (T1 oappSystemHolon in results.Result)
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
            foreach (T1 oappSystemHolon in result.Result)
            {
                if (oappSystemHolon.STARHolonDNA.CreatedByAvatarId == avatarId)
                    templates.Add(oappSystemHolon);

                else if (oappSystemHolon.STARHolonDNA.PublishedOn != DateTime.MinValue)
                    templates.Add(oappSystemHolon);
            }

            result.Result = templates;
            result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(results, result);
            return result;
        }

        private void OnUploadProgress(Google.Apis.Upload.IUploadProgress progress)
        {
            switch (progress.Status)
            {
                case Google.Apis.Upload.UploadStatus.NotStarted:
                    _progress = 0;
                    OnUploadStatusChanged?.Invoke(this, new STARHolonUploadProgressEventArgs() { Progress = _progress, Status = STARHolonUploadStatus.NotStarted });
                    break;

                case Google.Apis.Upload.UploadStatus.Starting:
                    _progress = 0;
                    OnUploadStatusChanged?.Invoke(this, new STARHolonUploadProgressEventArgs() { Progress = _progress, Status = STARHolonUploadStatus.Uploading });
                    break;

                case Google.Apis.Upload.UploadStatus.Completed:
                    _progress = 100;
                    OnUploadStatusChanged?.Invoke(this, new STARHolonUploadProgressEventArgs() { Progress = _progress, Status = STARHolonUploadStatus.Uploaded });
                    break;

                case Google.Apis.Upload.UploadStatus.Uploading:
                    {
                        if (_fileLength > 0)
                        {
                            _progress = Convert.ToInt32(progress.BytesSent / (double)_fileLength * 100);
                            OnUploadStatusChanged?.Invoke(this, new STARHolonUploadProgressEventArgs() { Progress = _progress, Status = STARHolonUploadStatus.Uploading });
                        }
                    }
                    break;

                case Google.Apis.Upload.UploadStatus.Failed:
                    OnUploadStatusChanged?.Invoke(this, new STARHolonUploadProgressEventArgs() { Progress = _progress, Status = STARHolonUploadStatus.Error, ErrorMessage = progress.Exception.ToString() });
                    break;
            }
        }

        private void OnDownloadProgress(Google.Apis.Download.IDownloadProgress progress)
        {
            switch (progress.Status)
            {
                case Google.Apis.Download.DownloadStatus.NotStarted:
                    _progress = 0;
                    OnDownloadStatusChanged?.Invoke(this, new STARHolonDownloadProgressEventArgs() { Progress = _progress, Status = STARHolonDownloadStatus.NotStarted });
                    break;

                case Google.Apis.Download.DownloadStatus.Completed:
                    _progress = 100;
                    OnDownloadStatusChanged?.Invoke(this, new STARHolonDownloadProgressEventArgs() { Progress = _progress, Status = STARHolonDownloadStatus.Downloaded });
                    break;

                case Google.Apis.Download.DownloadStatus.Downloading:
                    {
                        if (_fileLength > 0)
                        {
                            _progress = Convert.ToInt32(progress.BytesDownloaded / (double)_fileLength * 100);
                            // _progress = Convert.ToInt32(_fileLength / progress.BytesDownloaded);
                            OnDownloadStatusChanged?.Invoke(this, new STARHolonDownloadProgressEventArgs() { Progress = _progress, Status = STARHolonDownloadStatus.Downloading });
                        }
                    }
                    break;

                case Google.Apis.Download.DownloadStatus.Failed:
                    OnDownloadStatusChanged?.Invoke(this, new STARHolonDownloadProgressEventArgs() { Progress = _progress, Status = STARHolonDownloadStatus.Error, ErrorMessage = progress.Exception.ToString() });
                    break;
            }
        }
    }
}