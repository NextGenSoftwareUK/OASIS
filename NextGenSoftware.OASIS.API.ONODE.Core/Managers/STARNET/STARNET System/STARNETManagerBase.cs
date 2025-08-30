using System;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using System.Diagnostics;
using System.IO.Compression;
using System.Threading.Tasks;
using Google.Cloud.Storage.V1;
using System.Collections.Generic;
using NextGenSoftware.Utilities;
using NextGenSoftware.CLI.Engine;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.STAR.DNA;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.ONODE.Core.Holons;
using NextGenSoftware.OASIS.API.ONODE.Core.Objects;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Holons;
using NextGenSoftware.OASIS.API.ONODE.Core.Enums.STARNETHolon;
using NextGenSoftware.OASIS.API.ONODE.Core.Events.STARNETHolon;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Managers;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Managers.Base
{
    public abstract class STARNETManagerBase<T1, T2, T3, T4> : COSMICManagerBase, ISTARNETManagerBase<T1, T2, T3, T4> 
        where T1 : ISTARNETHolon, new()
        where T2 : IDownloadedSTARNETHolon, new()
        where T3 : IInstalledSTARNETHolon, new()
        where T4 : ISTARNETDNA, new()
    {
        private int _progress = 0;
        private long _fileLength = 0;

        public STARNETManagerBase(Guid avatarId, STARDNA STARDNA, OASISDNA OASISDNA = null) : base(avatarId, OASISDNA) { }
        public STARNETManagerBase(IOASISStorageProvider OASISStorageProvider, Guid avatarId, STARDNA STARDNA, OASISDNA OASISDNA = null) : base(OASISStorageProvider, avatarId, OASISDNA) { }
        public STARNETManagerBase(Guid avatarId, STARDNA STARDNA, OASISDNA OASISDNA = null, Type STARNETCategory = null, HolonType STARNETHolonType = HolonType.STARNETHolon, HolonType STARNETHolonInstalledHolonType = HolonType.InstalledSTARNETHolon, string STARNETHolonUIName = "OAPP System Holon", string STARNETHolonIdName = "STARNETHolonId", string STARNETHolonNameName = "STARNETHolonName", string STARNETHolonTypeName = "STARNETHolonType", string STARNETHolonFileExtention = "oappsystemholon", string STARNETHolonGoogleBucket = "oasis_oappsystemholons", string STARNETDNAFileName = "STARNETDNA.json", string STARNETDNAJSONName = "STARNETDNAJSON") : base(avatarId, OASISDNA)
        {
            this.STARDNA = STARDNA;
            this.STARNETHolonType = STARNETHolonType;
            this.STARNETHolonInstalledHolonType = STARNETHolonInstalledHolonType;
            this.STARNETHolonUIName = STARNETHolonUIName;
            this.STARNETHolonIdName = STARNETHolonIdName;
            this.STARNETHolonNameName = STARNETHolonNameName;
            this.STARNETHolonTypeName = STARNETHolonTypeName;
            this.STARNETHolonFileExtention = STARNETHolonFileExtention;
            this.STARNETHolonGoogleBucket = STARNETHolonGoogleBucket;
            this.STARNETDNAFileName = STARNETDNAFileName;
            this.STARNETDNAJSONName = STARNETDNAJSONName;
            this.STARNETCategory = STARNETCategory;
        }

        public STARNETManagerBase(IOASISStorageProvider OASISStorageProvider, Guid avatarId, STARDNA STARDNA, OASISDNA OASISDNA = null, Type STARNETCategory = null, HolonType STARNETHolonType = HolonType.STARNETHolon, HolonType STARNETHolonInstalledHolonType = HolonType.InstalledSTARNETHolon, string STARNETHolonUIName = "OAPP System Holon", string STARNETHolonIdName = "STARNETHolonId", string STARNETHolonNameName = "STARNETHolonName", string STARNETHolonTypeName = "STARNETHolonType", string STARNETHolonFileExtention = "oappsystemholon", string STARNETHolonGoogleBucket = "oasis_oappsystemholons", string STARNETDNAFileName = "STARNETDNA.json", string STARNETDNAJSONName = "STARNETDNAJSON") : base(OASISStorageProvider, avatarId, OASISDNA)
        {
            this.STARDNA = STARDNA;
            this.STARNETHolonType = STARNETHolonType;
            this.STARNETHolonInstalledHolonType = STARNETHolonInstalledHolonType;
            this.STARNETHolonUIName = STARNETHolonUIName;
            this.STARNETHolonIdName = STARNETHolonIdName;
            this.STARNETHolonNameName = STARNETHolonNameName;
            this.STARNETHolonTypeName = STARNETHolonTypeName;
            this.STARNETHolonFileExtention = STARNETHolonFileExtention;
            this.STARNETHolonGoogleBucket = STARNETHolonGoogleBucket;
            this.STARNETDNAFileName = STARNETDNAFileName;
            this.STARNETDNAJSONName = STARNETDNAJSONName;
            this.STARNETCategory = STARNETCategory;
        }

        public delegate void PublishStatusChanged(object sender, STARNETHolonPublishStatusEventArgs e);
        public delegate void InstallStatusChanged(object sender, STARNETHolonInstallStatusEventArgs e);
        public delegate void UploadStatusChanged(object sender, STARNETHolonUploadProgressEventArgs e);
        public delegate void DownloadStatusChanged(object sender, STARNETHolonDownloadProgressEventArgs e);

        /// <summary>
        /// Fired when there is a change in the publish status.
        /// </summary>
        public event PublishStatusChanged OnPublishStatusChanged;

        /// <summary>
        /// Fired when there is a change to the Install status.
        /// </summary>
        public event InstallStatusChanged OnInstallStatusChanged;

        /// <summary>
        /// Fired when there is a change in the upload status.
        /// </summary>
        public event UploadStatusChanged OnUploadStatusChanged;

        /// <summary>
        /// Fired when there is a change in the download status.
        /// </summary>
        public event DownloadStatusChanged OnDownloadStatusChanged;

        //public bool IsInstallable { get; set; } = true; //TODO: Make this configurable in the DNA?
        public HolonType STARNETHolonType { get; set; } = HolonType.STARNETHolon;
        public HolonType STARNETHolonInstalledHolonType { get; set; } = HolonType.InstalledSTARNETHolon;
        public string STARNETHolonUIName { get; set; } = "OAPP System Holon";
        public string STARNETHolonIdName { get; set; } = "STARNETHolonId";
        public string STARNETHolonNameName { get; set; } = "STARNETHolonName";
        public string STARNETHolonTypeName { get; set; } = "STARNETHolonType";
        public string STARNETHolonFileExtention { get; set; } = "oappsystemholon";
        public string STARNETHolonGoogleBucket { get; set; } = "oasis_oappsystemholons";
        public string STARNETDNAFileName { get; set; } = "STARNETDNA.json";
        public string STARNETDNAJSONName { get; set; } = "STARNETDNAJSON";
        public Type STARNETCategory { get; set; }
        public STARDNA STARDNA { get; set; }

        public virtual async Task<OASISResult<T1>> CreateAsync(Guid avatarId, string name, string description, object holonSubType, string fullPathToSourceFolder, Dictionary<string, string> metaTagMappings = null, Dictionary<string, object> metaData = null, T1 newHolon = default, T4 STARNETDNA = default, bool checkIfSourcePathExists = true, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            string errorMessage = "Error occured in STARNETManagerBase.CreateAsync, Reason:";
            T1 holon;

            try
            {
                //TODO: Dont want UI in the backend!
                if (Directory.Exists(fullPathToSourceFolder) && checkIfSourcePathExists)
                {
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} The directory {fullPathToSourceFolder} already exists! Please either delete it or choose a different name.");
                    return result;

                    //if (CLIEngine.GetConfirmation($"The directory {fullPathToT} already exists! Would you like to delete it?"))
                    //{
                    //    Console.WriteLine("");
                    //    Directory.Delete(fullPathToT, true);
                    //}
                    //else
                    //{
                    //    Console.WriteLine("");
                    //    OASISErrorHandling.HandleError(ref result, $"{errorMessage} The directory {fullPathToT} already exists! Please either delete it or choose a different name.");
                    //    return result;
                    //}
                }

                if (newHolon != null)
                {
                    holon = newHolon;

                    if (holon.Id == Guid.Empty)
                        holon.Id = Guid.NewGuid();

                    if (string.IsNullOrEmpty(holon.Name))
                        holon.Name = name;

                    if (string.IsNullOrEmpty(holon.Description))
                        holon.Description = description;
                }
                else
                {
                    holon = new T1()
                    {
                        Id = Guid.NewGuid(),
                        Name = name,
                        Description = description
                    };
                }

                holon.MetaData[STARNETHolonIdName] = holon.Id.ToString();
                holon.MetaData[STARNETHolonNameName] = holon.Name;
                //T.MetaData[STARNETHolonTypeName] = Enum.GetName(typeof(STARNETHolonType), STARNETHolonType);

                Type holonSubTypeType = holonSubType.GetType();
                holon.MetaData[STARNETHolonTypeName] = Enum.GetName(holonSubTypeType, holonSubType);
                holon.MetaData["Version"] = "1.0.0";
                holon.MetaData["VersionSequence"] = 1;
                holon.MetaData["Active"] = "1";
                holon.MetaData["CreatedByAvatarId"] = avatarId.ToString();

                foreach (string key in metaData?.Keys ?? new Dictionary<string, object>().Keys)
                {
                    if (!holon.MetaData.ContainsKey(key))
                        holon.MetaData.Add(key, metaData[key]);
                    else
                        holon.MetaData[key] = metaData[key];
                }

                //T.MetaData["LatestVersion"] = "1";

                OASISResult<IAvatar> avatarResult = await AvatarManager.Instance.LoadAvatarAsync(avatarId, false, true, providerType);

                if (avatarResult != null && avatarResult.Result != null && !avatarResult.IsError)
                {
                    if (STARNETDNA == null)
                        STARNETDNA = new T4();

                    STARNETDNA.Id = holon.Id;
                    STARNETDNA.Name = name;
                    STARNETDNA.Description = description;
                    STARNETDNA.STARNETHolonType = Enum.GetName(typeof(HolonType), STARNETHolonType);
                    STARNETDNA.STARNETCategory = Enum.GetName(holonSubTypeType, holonSubType);
                    STARNETDNA.CreatedByAvatarId = avatarId;
                    STARNETDNA.CreatedByAvatarUsername = avatarResult.Result.Username;
                    STARNETDNA.CreatedOn = DateTime.Now;
                    STARNETDNA.Version = "1.0.0";
                    STARNETDNA.STARRuntimeVersion = OASISBootLoader.OASISBootLoader.STARRuntimeVersion;
                    STARNETDNA.STARODKVersion = OASISBootLoader.OASISBootLoader.STARODKVersion;
                    STARNETDNA.STARAPIVersion = OASISBootLoader.OASISBootLoader.STARAPIVersion;
                    STARNETDNA.STARNETVersion = OASISBootLoader.OASISBootLoader.STARNETVersion;
                    STARNETDNA.OASISAPIVersion = OASISBootLoader.OASISBootLoader.OASISAPIVersion;
                    STARNETDNA.OASISRuntimeVersion = OASISBootLoader.OASISBootLoader.OASISRuntimeVersion;
                    STARNETDNA.COSMICVersion = OASISBootLoader.OASISBootLoader.COSMICVersion;
                    STARNETDNA.DotNetVersion = OASISBootLoader.OASISBootLoader.DotNetVersion;
                    STARNETDNA.SourcePath = fullPathToSourceFolder;
                    STARNETDNA.MetaData = metaData; //TODO: Not sure if we need this? It works without it, but may be useful to view in the DNA.json file for users?
                    STARNETDNA.MetaTagMappings = metaTagMappings;

                    //STARNETDNA STARNETDNA = new STARNETDNA()
                    //{
                    //    Id = holon.Id,
                    //    Name = name,
                    //    Description = description,
                    //    STARNETHolonType = Enum.GetName(holonSubTypeType, holonSubType),
                    //    CreatedByAvatarId = avatarId,
                    //    CreatedByAvatarUsername = avatarResult.Result.Username,
                    //    CreatedOn = DateTime.Now,
                    //    Version = "1.0.0",
                    //    STARODKVersion = OASISBootLoader.OASISBootLoader.STARODKVersion,
                    //    OASISVersion = OASISBootLoader.OASISBootLoader.OASISVersion,
                    //    COSMICVersion = OASISBootLoader.OASISBootLoader.COSMICVersion,
                    //    DotNetVersion = OASISBootLoader.OASISBootLoader.DotNetVersion,
                    //    SourcePath = fullPathToT,
                    //    MetaData = dependency //TODO: Not sure if we need this? It works without it, but may be useful to view in the DNA.json file for users?
                    //};

                    OASISResult<bool> writeSTARNETDNAResult = await WriteDNAAsync(STARNETDNA, fullPathToSourceFolder);

                    if (writeSTARNETDNAResult != null && writeSTARNETDNAResult.Result && !writeSTARNETDNAResult.IsError)
                    {
                        holon.STARNETDNA = STARNETDNA;
                        OASISResult<T1> saveHolonResult = await Data.SaveHolonAsync<T1>(holon, avatarId, true, true, 0, true, false, providerType);

                        if (saveHolonResult != null && saveHolonResult.Result != null && !saveHolonResult.IsError)
                        {
                            result.Result = saveHolonResult.Result;
                            result.Message = $"Successfully created the {STARNETHolonUIName} on the {Enum.GetName(typeof(ProviderType), providerType)} provider by AvatarId {avatarId} for {STARNETHolonTypeName} {Enum.GetName(holonSubTypeType, holonSubType)}.";
                        }
                        else
                            OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured saving the {STARNETHolonUIName} to the {Enum.GetName(typeof(ProviderType), providerType)} provider. Reason: {saveHolonResult.Message}");
                    }
                    else
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured writing the {STARNETHolonUIName} DNA. Reason: {writeSTARNETDNAResult.Message}");
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadAvatarAsync on {Enum.GetName(typeof(ProviderType), providerType)} provider. Reason: {avatarResult.Message}");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured saving the {STARNETHolonUIName} to the {Enum.GetName(typeof(ProviderType), providerType)} provider. Reason: {ex}");
            }

            return result;
        }

        public virtual OASISResult<T1> Create(Guid avatarId, string name, string description, object holonSubType, string fullPathToSourceFolder, Dictionary<string, object> dependency = null, T1 newHolon = default, T4 STARNETDNA = default, bool checkIfSourcePathExists = true, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            string errorMessage = "Error occured in STARNETManagerBase.Create, Reason:";
            T1 holon;

            try
            {
                //TODO: Dont want UI in the backend!
                if (Directory.Exists(fullPathToSourceFolder) && checkIfSourcePathExists)
                {
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} The directory {fullPathToSourceFolder} already exists! Please either delete it or choose a different name.");
                    return result;

                    //if (CLIEngine.GetConfirmation($"The directory {fullPathToT} already exists! Would you like to delete it?"))
                    //{
                    //    Console.WriteLine("");
                    //    Directory.Delete(fullPathToT, true);
                    //}
                    //else
                    //{
                    //    Console.WriteLine("");
                    //    OASISErrorHandling.HandleError(ref result, $"{errorMessage} The directory {fullPathToT} already exists! Please either delete it or choose a different name.");
                    //    return result;
                    //}
                }

                if (newHolon != null)
                {
                    holon = newHolon;

                    if (holon.Id == Guid.Empty)
                        holon.Id = Guid.NewGuid();

                    if (string.IsNullOrEmpty(holon.Name))
                        holon.Name = name;

                    if (string.IsNullOrEmpty(holon.Description))
                        holon.Description = description;
                }
                else
                {
                    holon = new T1()
                    {
                        Id = Guid.NewGuid(),
                        Name = name,
                        Description = description
                    };
                }

                holon.MetaData[STARNETHolonIdName] = holon.Id.ToString();
                holon.MetaData[STARNETHolonNameName] = holon.Name;
                //T.MetaData[STARNETHolonTypeName] = Enum.GetName(typeof(STARNETHolonType), STARNETHolonType);

                Type holonSubTypeType = holonSubType.GetType();
                holon.MetaData[STARNETHolonTypeName] = Enum.GetName(holonSubTypeType, holonSubType);
                holon.MetaData["Version"] = "1.0.0";
                holon.MetaData["VersionSequence"] = 1;
                holon.MetaData["Active"] = "1";
                holon.MetaData["CreatedByAvatarId"] = avatarId.ToString();

                foreach (string key in dependency?.Keys ?? new Dictionary<string, object>().Keys)
                {
                    if (!holon.MetaData.ContainsKey(key))
                        holon.MetaData.Add(key, dependency[key]);
                    else
                        holon.MetaData[key] = dependency[key];
                }

                //T.MetaData["LatestVersion"] = "1";

                OASISResult<IAvatar> avatarResult = AvatarManager.Instance.LoadAvatar(avatarId, false, true, providerType);

                if (avatarResult != null && avatarResult.Result != null && !avatarResult.IsError)
                {
                    if (STARNETDNA == null)
                        STARNETDNA = new T4();

                    STARNETDNA.Id = holon.Id;
                    STARNETDNA.Name = name;
                    STARNETDNA.Description = description;
                    STARNETDNA.STARNETHolonType = Enum.GetName(typeof(HolonType), STARNETHolonType);
                    STARNETDNA.STARNETCategory = Enum.GetName(holonSubTypeType, holonSubType);
                    STARNETDNA.CreatedByAvatarId = avatarId;
                    STARNETDNA.CreatedByAvatarUsername = avatarResult.Result.Username;
                    STARNETDNA.CreatedOn = DateTime.Now;
                    STARNETDNA.Version = "1.0.0";
                    STARNETDNA.STARRuntimeVersion = OASISBootLoader.OASISBootLoader.STARRuntimeVersion;
                    STARNETDNA.STARODKVersion = OASISBootLoader.OASISBootLoader.STARODKVersion;
                    STARNETDNA.STARAPIVersion = OASISBootLoader.OASISBootLoader.STARAPIVersion;
                    STARNETDNA.STARNETVersion = OASISBootLoader.OASISBootLoader.STARNETVersion;
                    STARNETDNA.OASISAPIVersion = OASISBootLoader.OASISBootLoader.OASISAPIVersion;
                    STARNETDNA.OASISRuntimeVersion = OASISBootLoader.OASISBootLoader.OASISRuntimeVersion;
                    STARNETDNA.COSMICVersion = OASISBootLoader.OASISBootLoader.COSMICVersion;
                    STARNETDNA.DotNetVersion = OASISBootLoader.OASISBootLoader.DotNetVersion;
                    STARNETDNA.SourcePath = fullPathToSourceFolder;
                    STARNETDNA.MetaData = dependency; //TODO: Not sure if we need this? It works without it, but may be useful to view in the DNA.json file for users?


                    //STARNETDNA STARNETDNA = new STARNETDNA()
                    //{
                    //    Id = holon.Id,
                    //    Name = name,
                    //    Description = description,
                    //    STARNETHolonType = Enum.GetName(holonSubTypeType, holonSubType),
                    //    CreatedByAvatarId = avatarId,
                    //    CreatedByAvatarUsername = avatarResult.Result.Username,
                    //    CreatedOn = DateTime.Now,
                    //    Version = "1.0.0",
                    //    STARODKVersion = OASISBootLoader.OASISBootLoader.STARODKVersion,
                    //    OASISVersion = OASISBootLoader.OASISBootLoader.OASISVersion,
                    //    COSMICVersion = OASISBootLoader.OASISBootLoader.COSMICVersion,
                    //    DotNetVersion = OASISBootLoader.OASISBootLoader.DotNetVersion,
                    //    SourcePath = fullPathToT,
                    //    MetaData = dependency //TODO: Not sure if we need this? It works without it, but may be useful to view in the DNA.json file for users?
                    //};

                    OASISResult<bool> writeSTARNETDNAResult = WriteDNA(STARNETDNA, fullPathToSourceFolder);

                    if (writeSTARNETDNAResult != null && writeSTARNETDNAResult.Result && !writeSTARNETDNAResult.IsError)
                    {
                        holon.STARNETDNA = STARNETDNA;
                        OASISResult<T1> saveHolonResult = Data.SaveHolon<T1>(holon, avatarId, true, true, 0, true, false, providerType);

                        if (saveHolonResult != null && saveHolonResult.Result != null && !saveHolonResult.IsError)
                        {
                            result.Result = saveHolonResult.Result;
                            result.Message = $"Successfully created the {STARNETHolonUIName} on the {Enum.GetName(typeof(ProviderType), providerType)} provider by AvatarId {avatarId} for {STARNETHolonTypeName} {Enum.GetName(holonSubTypeType, holonSubType)}.";
                        }
                        else
                            OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured saving the {STARNETHolonUIName} to the {Enum.GetName(typeof(ProviderType), providerType)} provider. Reason: {saveHolonResult.Message}");
                    }
                    else
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured writing the {STARNETHolonUIName} DNA. Reason: {writeSTARNETDNAResult.Message}");
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadAvatar on {Enum.GetName(typeof(ProviderType), providerType)} provider. Reason: {avatarResult.Message}");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured saving the {STARNETHolonUIName} to the {Enum.GetName(typeof(ProviderType), providerType)} provider. Reason: {ex}");
            }

            return result;
        }

        #region COSMICManagerBase
        public virtual async Task<OASISResult<T1>> UpdateAsync(Guid avatarId, T1 holon, bool updateDNAJSONFile = false, string STARNETDNAJSONName = "Default", ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = new OASISResult<T1>();

            if (!Directory.Exists(holon.STARNETDNA.SourcePath))
                Directory.CreateDirectory(holon.STARNETDNA.SourcePath);

            if (STARNETDNAJSONName == "Default")
                STARNETDNAJSONName = this.STARNETDNAJSONName;

            //holon.MetaData[STARNETDNAJSONName] = JsonSerializer.Serialize(holon.STARNETDNA);
            holon.MetaData[STARNETDNAJSONName] = JsonConvert.SerializeObject(holon.STARNETDNA);

            if (updateDNAJSONFile)
            {
                OASISResult<bool> writeSTARNETDNAResult = WriteDNA(holon.STARNETDNA, holon.STARNETDNA.SourcePath);

                if (writeSTARNETDNAResult != null && writeSTARNETDNAResult.Result && !writeSTARNETDNAResult.IsError)
                    result.Message = $"Successfully updated the {STARNETHolonUIName} DNA JSON file.";
                else
                    OASISErrorHandling.HandleError(ref result, $"Error occured updating the {STARNETHolonUIName} DNA JSON file. Reason: {writeSTARNETDNAResult.Message}");
            }

            OASISResult<T1> saveResult = await SaveHolonAsync<T1>(holon, avatarId, providerType, "STARNETManagerBase.UpdateAsync<T>");
            result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(saveResult, result);
            result.Result = saveResult.Result;
            return result;
        }

        public OASISResult<T1> Update(Guid avatarId, T1 holon, bool updateDNAJSONFile = false, string STARNETDNAJSONName = "Default", ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = new OASISResult<T1>();

            if (!Directory.Exists(holon.STARNETDNA.SourcePath))
                Directory.CreateDirectory(holon.STARNETDNA.SourcePath);

            if (STARNETDNAJSONName == "Default")
                STARNETDNAJSONName = this.STARNETDNAJSONName;

            //holon.MetaData[STARNETDNAJSONName] = JsonSerializer.Serialize(holon.STARNETDNA);
            holon.MetaData[STARNETDNAJSONName] = JsonConvert.SerializeObject(holon.STARNETDNA);

            if (updateDNAJSONFile)
            {
                OASISResult<bool> writeSTARNETDNAResult = WriteDNA(holon.STARNETDNA, holon.STARNETDNA.SourcePath);

                if (writeSTARNETDNAResult != null && writeSTARNETDNAResult.Result && !writeSTARNETDNAResult.IsError)
                    result.Message = $"Successfully updated the {STARNETHolonUIName} DNA JSON file.";
                else
                    OASISErrorHandling.HandleError(ref result, $"Error occured updating the {STARNETHolonUIName} DNA JSON file. Reason: {writeSTARNETDNAResult.Message}");
            }

            OASISResult<T1> saveResult = SaveHolon<T1>(holon, avatarId, providerType, "STARNETManagerBase.Update<T>");
            result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(saveResult, result);
            result.Result = saveResult.Result;
            return result;
        }

        public virtual async Task<OASISResult<T3>> UpdateAsync(Guid avatarId, T3 holon, bool updateDNAJSONFile = false, string STARNETDNAJSONName = "Default", ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T3> result = new OASISResult<T3>();

            if (!Directory.Exists(holon.STARNETDNA.SourcePath))
                Directory.CreateDirectory(holon.STARNETDNA.SourcePath);

            if (STARNETDNAJSONName == "Default")
                STARNETDNAJSONName = this.STARNETDNAJSONName;

            //holon.MetaData[STARNETDNAJSONName] = JsonSerializer.Serialize(holon.STARNETDNA);
            holon.MetaData[STARNETDNAJSONName] = JsonConvert.SerializeObject(holon.STARNETDNA);

            if (updateDNAJSONFile)
            {
                OASISResult<bool> writeSTARNETDNAResult = await WriteDNAAsync(holon.STARNETDNA, holon.InstalledPath);

                if (writeSTARNETDNAResult != null && writeSTARNETDNAResult.Result && !writeSTARNETDNAResult.IsError)
                    result.Message = $"Successfully updated the {STARNETHolonUIName} DNA JSON file.";
                else
                    OASISErrorHandling.HandleError(ref result, $"Error occured updating the {STARNETHolonUIName} DNA JSON file. Reason: {writeSTARNETDNAResult.Message}");
            }

            OASISResult<T3> saveResult = await SaveHolonAsync<T3>(holon, avatarId, providerType, "STARNETManagerBase.UpdateAsync<T>");
            result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(saveResult, result);
            result.Result = saveResult.Result;
            return result;
        }

        public OASISResult<T3> Update(Guid avatarId, T3 holon, bool updateDNAJSONFile = false, string STARNETDNAJSONName = "Default", ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T3> result = new OASISResult<T3>();

            if (!Directory.Exists(holon.STARNETDNA.SourcePath))
                Directory.CreateDirectory(holon.STARNETDNA.SourcePath);

            if (STARNETDNAJSONName == "Default")
                STARNETDNAJSONName = this.STARNETDNAJSONName;

            //holon.MetaData[STARNETDNAJSONName] = JsonSerializer.Serialize(holon.STARNETDNA);
            holon.MetaData[STARNETDNAJSONName] = JsonConvert.SerializeObject(holon.STARNETDNA);

            if (updateDNAJSONFile)
            {
                OASISResult<bool> writeSTARNETDNAResult = WriteDNA(holon.STARNETDNA, holon.InstalledPath);

                if (writeSTARNETDNAResult != null && writeSTARNETDNAResult.Result && !writeSTARNETDNAResult.IsError)
                    result.Message = $"Successfully updated the {STARNETHolonUIName} DNA JSON file.";
                else
                    OASISErrorHandling.HandleError(ref result, $"Error occured updating the {STARNETHolonUIName} DNA JSON file. Reason: {writeSTARNETDNAResult.Message}");
            }

            OASISResult<T3> saveResult = SaveHolon<T3>(holon, avatarId, providerType, "STARNETManagerBase.Update<T>");
            result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(saveResult, result);
            result.Result = saveResult.Result;
            return result;
        }

        //public virtual async Task<OASISResult<T>> LoadAsync<T>(Guid avatarId, Guid id, int version = 0, ProviderType providerType = ProviderType.Default) where T : ISTARNETHolon, new()
        //{
        //    OASISResult<T> result = new OASISResult<T>();
        //    OASISResult<IEnumerable<T>> loadResult = await Data.LoadHolonsByMetaDataAsync<T>(STARNETHolonIdName, id.ToString(), STARNETHolonType, true, true, 0, true, false, 0, HolonType.All, 0, providerType);
        //    OASISResult<IEnumerable<T>> filterdResult = FilterResultsForVersion(avatarId, loadResult, false, version);

        //    if (filterdResult != null && filterdResult.Result != null && !filterdResult.IsError)
        //        result.Result = filterdResult.Result.FirstOrDefault();
        //    else
        //        OASISErrorHandling.HandleError(ref result, $"Error occured in LoadAsync<T> loading the {STARNETHolonUIName} with Id {id} from the {Enum.GetName(typeof(ProviderType), providerType)} provider. Reason: {filterdResult.Message}");

        //    if (result.Result == null)
        //        result.Message = "No Holon Found";

        //    return result;
        //}

        //public OASISResult<T> Load<T>(Guid avatarId, Guid id, int version = 0, ProviderType providerType = ProviderType.Default) where T : ISTARNETHolon, new()
        //{
        //    OASISResult<T> result = new OASISResult<T>();
        //    OASISResult<IEnumerable<T>> loadResult = Data.LoadHolonsByMetaData<T>(STARNETHolonIdName, id.ToString(), STARNETHolonType, true, true, 0, true, false, 0, HolonType.All, 0, providerType);
        //    OASISResult<IEnumerable<T>> filterdResult = FilterResultsForVersion(avatarId, loadResult, false, version);

        //    if (filterdResult != null && filterdResult.Result != null && !filterdResult.IsError)
        //        result.Result = filterdResult.Result.FirstOrDefault();
        //    else
        //        OASISErrorHandling.HandleError(ref result, $"Error occured in Load<T> loading the {STARNETHolonUIName} with Id {id} from the {Enum.GetName(typeof(ProviderType), providerType)} provider. Reason: {filterdResult.Message}");

        //    if (result.Result == null)
        //        result.Message = "No Holon Found";

        //    return result;
        //}

        //public virtual async Task<OASISResult<T>> LoadAsync<T>(Guid avatarId, string sourcePath, ProviderType providerType = ProviderType.Default) where T : ISTARNETHolon, new()
        //{
        //    OASISResult<T> result = new OASISResult<T>();
        //    OASISResult<T4> readDNAResult = await ReadDNAFromSourceOrInstallFolderAsync<T4>(sourcePath);

        //    if (readDNAResult != null && readDNAResult.Result != null && !readDNAResult.IsError)
        //        result = await LoadAsync<T>(avatarId, readDNAResult.Result.Id, 0, providerType);
        //    else
        //        OASISErrorHandling.HandleError(ref result, $"Error occured in LoadAsync<T> calling ReadDNAFromSourceOrInstallFolderAsync reading the STARNETDNA from the source path {sourcePath} for the {STARNETHolonUIName}. Reason: {readDNAResult.Message}");

        //    return result;
        //}

        //public OASISResult<T> Load<T>(Guid avatarId, string sourcePath, ProviderType providerType = ProviderType.Default) where T : ISTARNETHolon, new()
        //{
        //    OASISResult<T> result = new OASISResult<T>();
        //    OASISResult<T4> readDNAResult = ReadDNAFromSourceOrInstallFolder<T4>(sourcePath);

        //    if (readDNAResult != null && readDNAResult.Result != null && !readDNAResult.IsError)
        //        result = Load<T>(avatarId, readDNAResult.Result.Id, 0, providerType);
        //    else
        //        OASISErrorHandling.HandleError(ref result, $"Error occured in Load<T> calling ReadDNAFromSourceOrInstallFolderAsync reading the STARNETDNA from the source path {sourcePath} for the {STARNETHolonUIName}. Reason: {readDNAResult.Message}");

        //    return result;
        //}

        public virtual async Task<OASISResult<T1>> LoadAsync(Guid avatarId, Guid id, int version = 0, HolonType holonType = HolonType.Default, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = new OASISResult<T1>();

            if (holonType == HolonType.Default)
                holonType = STARNETHolonType;

            OASISResult<IEnumerable<T1>> loadResult = await Data.LoadHolonsByMetaDataAsync<T1>(STARNETHolonIdName, id.ToString(), holonType, true, true, 0, true, false, 0, HolonType.All, 0, providerType);
            OASISResult<IEnumerable<T1>> filterdResult = FilterResultsForVersion(avatarId, loadResult, false, version);

            if (filterdResult != null && filterdResult.Result != null && !filterdResult.IsError)
                result.Result = filterdResult.Result.FirstOrDefault();
            else
                OASISErrorHandling.HandleError(ref result, $"Error occured in LoadAsync<T> loading the {STARNETHolonUIName} with Id {id} from the {Enum.GetName(typeof(ProviderType), providerType)} provider. Reason: {filterdResult.Message}");

            if (result.Result == null)
                result.Message = "No Holon Found";

            return result;
        }

        public OASISResult<T1> Load(Guid avatarId, Guid id, int version = 0, HolonType holonType = HolonType.Default, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = new OASISResult<T1>();

            if (holonType == HolonType.Default)
                holonType = STARNETHolonType;

            OASISResult<IEnumerable<T1>> loadResult = Data.LoadHolonsByMetaData<T1>(STARNETHolonIdName, id.ToString(), holonType, true, true, 0, true, false, 0, HolonType.All, 0, providerType);
            OASISResult<IEnumerable<T1>> filterdResult = FilterResultsForVersion(avatarId, loadResult, false, version);

            if (filterdResult != null && filterdResult.Result != null && !filterdResult.IsError)
                result.Result = filterdResult.Result.FirstOrDefault();
            else
                OASISErrorHandling.HandleError(ref result, $"Error occured in Load<T> loading the {STARNETHolonUIName} with Id {id} from the {Enum.GetName(typeof(ProviderType), providerType)} provider. Reason: {filterdResult.Message}");

            if (result.Result == null)
                result.Message = "No Holon Found";

            return result;
        }

        public virtual async Task<OASISResult<T1>> LoadForSourceOrInstalledFolderAsync(Guid avatarId, string sourceOrInstallPath, HolonType holonType = HolonType.Default, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            OASISResult<T4> readDNAResult = await ReadDNAFromSourceOrInstallFolderAsync<T4>(sourceOrInstallPath);

            if (readDNAResult != null && readDNAResult.Result != null && !readDNAResult.IsError)
                result = await LoadAsync(avatarId, readDNAResult.Result.Id, readDNAResult.Result.VersionSequence, holonType, providerType);
            else
                OASISErrorHandling.HandleError(ref result, $"Error occured in LoadAsync<T> calling ReadDNAFromSourceOrInstallFolderAsync reading the STARNETDNA from the source path {sourceOrInstallPath} for the {STARNETHolonUIName}. Reason: {readDNAResult.Message}");

            return result;
        }

        public OASISResult<T1> LoadForSourceOrInstalledFolder(Guid avatarId, string sourceOrInstallPath, HolonType holonType = HolonType.Default, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            OASISResult<T4> readDNAResult = ReadDNAFromSourceOrInstallFolder<T4>(sourceOrInstallPath);

            if (readDNAResult != null && readDNAResult.Result != null && !readDNAResult.IsError)
                result = Load(avatarId, readDNAResult.Result.Id, readDNAResult.Result.VersionSequence, holonType, providerType);
            else
                OASISErrorHandling.HandleError(ref result, $"Error occured in Load<T> calling ReadDNAFromSourceOrInstallFolderAsync reading the STARNETDNA from the source path {sourceOrInstallPath} for the {STARNETHolonUIName}. Reason: {readDNAResult.Message}");

            return result;
        }

        public virtual async Task<OASISResult<T1>> LoadForHolonAsync(Guid avatarId, Guid holonId, ProviderType providerType = ProviderType.Default)
        {
            return await Data.LoadHolonAsync<T1>(holonId, providerType: providerType);
        }

        public OASISResult<T1> LoadForHolon(Guid avatarId, Guid holonId, ProviderType providerType = ProviderType.Default)
        {
            return Data.LoadHolon<T1>(holonId, providerType: providerType);
        }

        public virtual async Task<OASISResult<T1>> LoadForPublishedFileAsync(Guid avatarId, string publishedFilePath, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            OASISResult<T4> readDNAResult = await ReadDNAFromPublishedFileAsync<T4>(publishedFilePath);

            if (readDNAResult != null && readDNAResult.Result != null && !readDNAResult.IsError)
                result = await LoadAsync(avatarId, readDNAResult.Result.Id, readDNAResult.Result.VersionSequence, providerType: providerType);
            else
                OASISErrorHandling.HandleError(ref result, $"Error occured in LoadAsync<T> calling ReadDNAFromSourceOrInstallFolderAsync reading the STARNETDNA from the source path {publishedFilePath} for the {STARNETHolonUIName}. Reason: {readDNAResult.Message}");

            return result;
        }

        public OASISResult<T1> LoadForPublishedFile(Guid avatarId, string publishedFilePath, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            OASISResult<T4> readDNAResult = ReadDNAFromPublishedFile<T4>(publishedFilePath);

            if (readDNAResult != null && readDNAResult.Result != null && !readDNAResult.IsError)
                result = Load(avatarId, readDNAResult.Result.Id, readDNAResult.Result.VersionSequence, providerType: providerType);
            else
                OASISErrorHandling.HandleError(ref result, $"Error occured in Load<T> calling ReadDNAFromSourceOrInstallFolderAsync reading the STARNETDNA from the source path {publishedFilePath} for the {STARNETHolonUIName}. Reason: {readDNAResult.Message}");

            return result;
        }

        public virtual async Task<OASISResult<IEnumerable<T1>>> LoadAllAsync(Guid avatarId, object holonSubType, bool loadAllTypes = true, bool showAllVersions = false, int version = 0, HolonType STARNETHolonType = HolonType.Default, string STARNETHolonTypeName = "Default", ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<T1>> result = new OASISResult<IEnumerable<T1>>();
            OASISResult<IEnumerable<T1>> loadHolonsResult = null;

            if (STARNETHolonType == HolonType.Default)
                STARNETHolonType = this.STARNETHolonType;

            if (STARNETHolonTypeName == "Default")
                STARNETHolonTypeName = this.STARNETHolonTypeName;

            if (loadAllTypes)
                loadHolonsResult = await Data.LoadAllHolonsAsync<T1>(STARNETHolonType, true, true, 0, true, false, HolonType.All, 0, providerType);
            else
                loadHolonsResult = await Data.LoadHolonsByMetaDataAsync<T1>(STARNETHolonTypeName, Enum.GetName(holonSubType.GetType(), holonSubType), HolonType.All, true, true, 0, true, false, 0, HolonType.All, 0);

            return FilterResultsForVersion(avatarId, loadHolonsResult, showAllVersions, version);
        }

        public OASISResult<IEnumerable<T1>> LoadAll(Guid avatarId, object holonSubType, bool loadAllTypes = true, bool showAllVersions = false, int version = 0, HolonType STARNETHolonType = HolonType.Default, string STARNETHolonTypeName = "Default", ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<T1>> result = new OASISResult<IEnumerable<T1>>();
            OASISResult<IEnumerable<T1>> loadHolonsResult = null;

            if (STARNETHolonType == HolonType.Default)
                STARNETHolonType = this.STARNETHolonType;

            if (STARNETHolonTypeName == "Default")
                STARNETHolonTypeName = this.STARNETHolonTypeName;

            if (loadAllTypes)
                loadHolonsResult = Data.LoadAllHolons<T1>(STARNETHolonType, true, true, 0, true, false, HolonType.All, 0, providerType);
            else
                loadHolonsResult = Data.LoadHolonsByMetaData<T1>(STARNETHolonTypeName, Enum.GetName(holonSubType.GetType(), holonSubType), HolonType.All, true, true, 0, true, false, 0, HolonType.All, 0);

            return FilterResultsForVersion(avatarId, loadHolonsResult, showAllVersions, version);
        }

        public virtual async Task<OASISResult<IEnumerable<T1>>> LoadAllForAvatarAsync(Guid avatarId, bool showAllVersions = false, int version = 0, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<T1>> result = new OASISResult<IEnumerable<T1>>();
            OASISResult<IEnumerable<T1>> loadHolonsResult = await Data.LoadHolonsByMetaDataAsync<T1>(new Dictionary<string, string>()
            {
                { "CreatedByAvatarId", avatarId.ToString() },
                { "Active", "1" }

            }, MetaKeyValuePairMatchMode.All, STARNETHolonType, providerType: providerType);

            return FilterResultsForVersion(avatarId, loadHolonsResult, showAllVersions, version);
        }

        public OASISResult<IEnumerable<T1>> LoadAllForAvatar(Guid avatarId, bool showAllVersions = false, int version = 0, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<T1>> result = new OASISResult<IEnumerable<T1>>();
            OASISResult<IEnumerable<T1>> loadHolonsResult = Data.LoadHolonsByMetaData<T1>(new Dictionary<string, string>()
            {
                { "CreatedByAvatarId", avatarId.ToString() },
                { "Active", "1" }

            }, MetaKeyValuePairMatchMode.All, STARNETHolonType, providerType: providerType);

            return FilterResultsForVersion(avatarId, loadHolonsResult, showAllVersions, version);
        }

        public virtual async Task<OASISResult<IEnumerable<T>>> SearchAsync<T>(Guid avatarId, string searchTerm, bool searchOnlyForCurrentAvatar = true, bool showAllVersions = false, int version = 0, ProviderType providerType = ProviderType.Default) where T : ISTARNETHolon, new()
        {
            OASISResult<IEnumerable<T>> result = new OASISResult<IEnumerable<T>>();
            OASISResult<IEnumerable<T>> loadHolonsResult = await SearchHolonsAsync<T>(searchTerm, avatarId, searchOnlyForCurrentAvatar, providerType, "STARNETManagerBase.SearchAsync", STARNETHolonType);
            return FilterResultsForVersion(avatarId, loadHolonsResult, showAllVersions, version);
        }

        public OASISResult<IEnumerable<T1>> Search(Guid avatarId, string searchTerm, bool searchOnlyForCurrentAvatar = true, bool showAllVersions = false, int version = 0, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<T1>> result = new OASISResult<IEnumerable<T1>>();
            OASISResult<IEnumerable<T1>> loadHolonsResult = SearchHolons<T1>(searchTerm, avatarId, searchOnlyForCurrentAvatar, providerType, "STARNETManagerBase.Search", STARNETHolonType);
            return FilterResultsForVersion(avatarId, loadHolonsResult, showAllVersions, version);
        }

        public virtual async Task<OASISResult<T1>> DeleteAsync(Guid avatarId, Guid id, int version, bool softDelete = true, bool deleteDownload = true, bool deleteInstall = true, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            string errorMessage = "Error occured in DeleteAsync. Reason: ";
            OASISResult<T1> loadResult = await LoadAsync(id, avatarId, version, providerType: providerType);

            if (loadResult != null && loadResult.Result != null && !loadResult.IsError)
                result = await DeleteAsync(avatarId, loadResult.Result, version, softDelete, deleteDownload, deleteInstall, providerType);
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured loading the {STARNETHolonUIName} with Id {id} from the {Enum.GetName(typeof(ProviderType), providerType)} provider. Reason: {loadResult.Message}");

            return result;
        }

        public OASISResult<T1> Delete(Guid avatarId, Guid id, int version, bool softDelete = true, bool deleteDownload = true, bool deleteInstall = true, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            string errorMessage = "Error occured in Delete. Reason: ";
            OASISResult<T1> loadResult = Load(id, avatarId, version, providerType: providerType);

            if (loadResult != null && loadResult.Result != null && !loadResult.IsError)
                result = Delete(avatarId, loadResult.Result, version, softDelete, deleteDownload, deleteInstall, providerType);
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured loading the {STARNETHolonUIName} with Id {id} from the {Enum.GetName(typeof(ProviderType), providerType)} provider. Reason: {loadResult.Message}");

            return result;
        }

        public virtual async Task<OASISResult<T1>> DeleteAsync(Guid avatarId, ISTARNETHolon oappSystemHolon, int version, bool softDelete = true, bool deleteDownload = true, bool deleteInstall = true, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            string errorMessage = "Error occured in DeleteAsync. Reason: ";

            if (oappSystemHolon.STARNETDNA.CreatedByAvatarId != avatarId)
            {
                OASISErrorHandling.HandleError(ref result, $"Permission Denied. You did not create this {STARNETHolonUIName}. Error occured in DeleteSTARNETHolonAsync loading the {STARNETHolonUIName} with Id {oappSystemHolon.STARNETDNA.CreatedByAvatarId} from the {Enum.GetName(typeof(ProviderType), providerType)} provider. Reason: The {STARNETHolonUIName} was not created by the Avatar with Id {avatarId}.");
                return result;
            }

            try
            {
                if (!string.IsNullOrEmpty(oappSystemHolon.STARNETDNA.SourcePath) && Directory.Exists(oappSystemHolon.STARNETDNA.SourcePath))
                    Directory.Delete(oappSystemHolon.STARNETDNA.SourcePath, true);
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} Error occured attempting to delete the T folder {oappSystemHolon.STARNETDNA.SourcePath}. PLEASE DELETE MANUALLY! Reason: {e}");
            }

            try
            {
                if (!string.IsNullOrEmpty(oappSystemHolon.STARNETDNA.PublishedPath) && File.Exists(oappSystemHolon.STARNETDNA.PublishedPath))
                    File.Delete(oappSystemHolon.STARNETDNA.PublishedPath);
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} Error occured attempting to delete the T Published folder {oappSystemHolon.STARNETDNA.PublishedPath}. PLEASE DELETE MANUALLY! Reason: {e}");
            }

            if (deleteDownload || deleteInstall)
            {
                OASISResult<T3> installedSTARNETHolonResult = await LoadInstalledAsync(avatarId, oappSystemHolon.STARNETDNA.Id, version, providerType);

                if (installedSTARNETHolonResult != null && installedSTARNETHolonResult.Result != null && !installedSTARNETHolonResult.IsError)
                {
                    try
                    {
                        if (deleteDownload && !string.IsNullOrEmpty(installedSTARNETHolonResult.Result.DownloadedPath) && File.Exists(installedSTARNETHolonResult.Result.DownloadedPath))
                            File.Delete(installedSTARNETHolonResult.Result.DownloadedPath);
                    }
                    catch (Exception e)
                    {
                        OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} Error occured attempting to delete the {STARNETHolonUIName} Download folder {installedSTARNETHolonResult.Result.DownloadedPath}. PLEASE DELETE MANUALLY! Reason: {e}");
                    }

                    try
                    {
                        if (deleteInstall && !string.IsNullOrEmpty(installedSTARNETHolonResult.Result.InstalledPath) && Directory.Exists(installedSTARNETHolonResult.Result.InstalledPath))
                            Directory.Delete(installedSTARNETHolonResult.Result.InstalledPath, true);
                    }
                    catch (Exception e)
                    {
                        OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} Error occured attempting to delete the {STARNETHolonUIName} Installed folder {installedSTARNETHolonResult.Result.InstalledPath}. PLEASE DELETE MANUALLY! Reason: {e}");
                    }

                    if (deleteInstall)
                    {
                        OASISResult<T1> deleteInstalledSTARNETHolonHolonResult = await DeleteHolonAsync<T1>(installedSTARNETHolonResult.Result.Id, avatarId, softDelete, providerType, "STARNETManagerBase.DeleteAsync");

                        if (!(deleteInstalledSTARNETHolonHolonResult != null && deleteInstalledSTARNETHolonHolonResult.Result != null && !deleteInstalledSTARNETHolonHolonResult.IsError))
                            OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} Error occured deleting the Installed {STARNETHolonUIName} holon with id {installedSTARNETHolonResult.Result.Id} calling DeleteAsync. Reason: {deleteInstalledSTARNETHolonHolonResult.Message}");
                    }

                    if (deleteDownload)
                    {
                        OASISResult<T1> deleteDownloadedSTARNETHolonHolonResult = await DeleteHolonAsync<T1>(installedSTARNETHolonResult.Result.DownloadedSTARNETHolonId, avatarId, softDelete, providerType, "STARNETManagerBase.DeleteAsync");

                        if (!(deleteDownloadedSTARNETHolonHolonResult != null && deleteDownloadedSTARNETHolonHolonResult.Result != null && !deleteDownloadedSTARNETHolonHolonResult.IsError))
                            OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} Error occured deleting the Downloaded {STARNETHolonUIName} holon with id {installedSTARNETHolonResult.Result.DownloadedSTARNETHolonId} calling DeleteAsync. Reason: {deleteDownloadedSTARNETHolonHolonResult.Message}");
                    }
                }
            }

            OASISResult<T1> deleteHolonResult = await DeleteHolonAsync<T1>(oappSystemHolon.Id, avatarId, softDelete, providerType, "STARNETManagerBase.DeleteAsync");

            if (!(deleteHolonResult != null && deleteHolonResult.Result != null && !deleteHolonResult.IsError))
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured deleting the {STARNETHolonUIName} holon with id {oappSystemHolon.Id} calling DeleteAsync. Reason: {deleteHolonResult.Message}");

            result.Result = deleteHolonResult.Result;
            return result;
        }

        public OASISResult<T1> Delete(Guid avatarId, ISTARNETHolon oappSystemHolon, int version, bool softDelete = true, bool deleteDownload = true, bool deleteInstall = true, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            string errorMessage = "Error occured in Delete. Reason: ";

            if (oappSystemHolon.STARNETDNA.CreatedByAvatarId != avatarId)
            {
                OASISErrorHandling.HandleError(ref result, $"Permission Denied. You did not create this {STARNETHolonUIName}. Error occured in Delete loading the {STARNETHolonUIName} with Id {oappSystemHolon.STARNETDNA.CreatedByAvatarId} from the {Enum.GetName(typeof(ProviderType), providerType)} provider. Reason: The {STARNETHolonUIName} was not created by the Avatar with Id {avatarId}.");
                return result;
            }

            try
            {
                if (!string.IsNullOrEmpty(oappSystemHolon.STARNETDNA.SourcePath) && Directory.Exists(oappSystemHolon.STARNETDNA.SourcePath))
                    Directory.Delete(oappSystemHolon.STARNETDNA.SourcePath, true);
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} Error occured attempting to delete the {STARNETHolonUIName} Source folder {oappSystemHolon.STARNETDNA.SourcePath}. PLEASE DELETE MANUALLY! Reason: {e}");
            }

            try
            {
                if (!string.IsNullOrEmpty(oappSystemHolon.STARNETDNA.PublishedPath) && File.Exists(oappSystemHolon.STARNETDNA.PublishedPath))
                    File.Delete(oappSystemHolon.STARNETDNA.PublishedPath);
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} Error occured attempting to delete the {STARNETHolonUIName} Published folder {oappSystemHolon.STARNETDNA.PublishedPath}. PLEASE DELETE MANUALLY! Reason: {e}");
            }

            if (deleteDownload || deleteInstall)
            {
                OASISResult<T3> installedSTARNETHolonResult = LoadInstalled(avatarId, oappSystemHolon.STARNETDNA.Id, version, providerType);

                if (installedSTARNETHolonResult != null && installedSTARNETHolonResult.Result != null && !installedSTARNETHolonResult.IsError)
                {
                    try
                    {
                        if (deleteDownload && !string.IsNullOrEmpty(installedSTARNETHolonResult.Result.DownloadedPath) && File.Exists(installedSTARNETHolonResult.Result.DownloadedPath))
                            File.Delete(installedSTARNETHolonResult.Result.DownloadedPath);
                    }
                    catch (Exception e)
                    {
                        OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} Error occured attempting to delete the {STARNETHolonUIName} Download folder {installedSTARNETHolonResult.Result.DownloadedPath}. PLEASE DELETE MANUALLY! Reason: {e}");
                    }

                    try
                    {
                        if (deleteInstall && !string.IsNullOrEmpty(installedSTARNETHolonResult.Result.InstalledPath) && Directory.Exists(installedSTARNETHolonResult.Result.InstalledPath))
                            Directory.Delete(installedSTARNETHolonResult.Result.InstalledPath, true);
                    }
                    catch (Exception e)
                    {
                        OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} Error occured attempting to delete the {STARNETHolonUIName} Installed folder {installedSTARNETHolonResult.Result.InstalledPath}. PLEASE DELETE MANUALLY! Reason: {e}");
                    }

                    if (deleteInstall)
                    {
                        OASISResult<T1> deleteInstalledSTARNETHolonHolonResult = DeleteHolon<T1>(installedSTARNETHolonResult.Result.Id, avatarId, softDelete, providerType, "STARNETManagerBase.Delete");

                        if (!(deleteInstalledSTARNETHolonHolonResult != null && deleteInstalledSTARNETHolonHolonResult.Result != null && !deleteInstalledSTARNETHolonHolonResult.IsError))
                            OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} Error occured deleting the Installed {STARNETHolonUIName} holon with id {installedSTARNETHolonResult.Result.Id} calling DeleteHolonAsync. Reason: {deleteInstalledSTARNETHolonHolonResult.Message}");
                    }

                    if (deleteDownload)
                    {
                        OASISResult<T1> deleteDownloadedSTARNETHolonHolonResult = DeleteHolon<T1>(installedSTARNETHolonResult.Result.DownloadedSTARNETHolonId, avatarId, softDelete, providerType, "STARNETManagerBase.Delete");

                        if (!(deleteDownloadedSTARNETHolonHolonResult != null && deleteDownloadedSTARNETHolonHolonResult.Result != null && !deleteDownloadedSTARNETHolonHolonResult.IsError))
                            OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} Error occured deleting the Downloaded {STARNETHolonUIName} holon with id {installedSTARNETHolonResult.Result.DownloadedSTARNETHolonId} calling DeleteHolonAsync. Reason: {deleteDownloadedSTARNETHolonHolonResult.Message}");
                    }
                }
            }

            OASISResult<T1> deleteHolonResult = DeleteHolon<T1>(avatarId, oappSystemHolon.Id, softDelete, providerType, "STARNETManagerBase.Delete");

            if (!(deleteHolonResult != null && deleteHolonResult.Result != null && !deleteHolonResult.IsError))
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured deleting the {STARNETHolonUIName} holon with id {oappSystemHolon.Id} calling DeleteHolonAsync. Reason: {deleteHolonResult.Message}");

            result.Result = deleteHolonResult.Result;
            return result;
        }
        #endregion

        public virtual async Task<OASISResult<IEnumerable<T1>>> LoadVersionsAsync(Guid id, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<T1>> result = new OASISResult<IEnumerable<T1>>();

            //TODO: Currently we pass in 0 for version (which means the OASIS will return the latest version) but we need to be able to query for all versions (-1)
            //OASISResult<IEnumerable<T>> loadHolonsResult = await Data.LoadHolonsByMetaDataAsync<T>("STARNETHolonId", STARNETHolonId.ToString(), HolonType.T, true, true, 0, true, false, 0, HolonType.All, -1, providerType);
            OASISResult<IEnumerable<T1>> loadHolonsResult = await Data.LoadHolonsByMetaDataAsync<T1>(new Dictionary<string, string>()
            {
                { STARNETHolonIdName, id.ToString() },
                { "Active", "1" }
            }, MetaKeyValuePairMatchMode.All, STARNETHolonType, true, true, 0, true, false, 0, HolonType.All, -1, providerType);

            result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(loadHolonsResult, result);
            result.Result = loadHolonsResult.Result;
            return result;
        }

        public OASISResult<IEnumerable<T1>> LoadVersions(Guid id, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<T1>> result = new OASISResult<IEnumerable<T1>>();

            //TODO: Currently we pass in 0 for version (which means the OASIS will return the latest version) but we need to be able to query for all versions (-1)
            //OASISResult<IEnumerable<T>> loadHolonsResult = Data.LoadHolonsByMetaData<T>("STARNETHolonId", STARNETHolonId.ToString(), HolonType.T, true, true, 0, true, false, 0, HolonType.All, -1, providerType);
            OASISResult<IEnumerable<T1>> loadHolonsResult = Data.LoadHolonsByMetaData<T1>(new Dictionary<string, string>()
            {
                { STARNETHolonIdName, id.ToString() },
                { "Active", "1" }
            }, MetaKeyValuePairMatchMode.All, STARNETHolonType, true, true, 0, true, false, 0, HolonType.All, -1, providerType);

            result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(loadHolonsResult, result);
            result.Result = loadHolonsResult.Result;
            return result;
        }

        public virtual async Task<OASISResult<T1>> LoadVersionAsync(Guid id, string version, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            OASISResult<T1> loadHolonResult = await Data.LoadHolonByMetaDataAsync<T1>(new Dictionary<string, string>()
            {
                 { STARNETHolonIdName, id.ToString() },
                 { "Version", version },
                 { "Active", "1" }
            }, MetaKeyValuePairMatchMode.All, STARNETHolonType, true, true, 0, true, 0, false, HolonType.All, providerType);

            OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(loadHolonResult, result); //Copy any possible warnings etc.
            if (loadHolonResult != null && !loadHolonResult.IsError && loadHolonResult.Result != null)
            {
                if (loadHolonResult.Result.STARNETDNA.Version == version)
                    result.Result = loadHolonResult.Result;
                else
                    OASISErrorHandling.HandleError(ref result, $"Error occured in STARNETManagerBase.LoadVersionAsync. Reason: The version {version} does not exist for id {id}.");
            }
            else
                OASISErrorHandling.HandleError(ref result, $"Error occured in STARNETManagerBase.LoadVersionAsync. Reason: {loadHolonResult.Message}");

            return result;
        }

        public OASISResult<T1> LoadVersion(Guid id, string version, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            OASISResult<T1> loadHolonResult = Data.LoadHolonByMetaData<T1>(new Dictionary<string, string>()
            {
                 { STARNETHolonIdName, id.ToString() },
                 { "Version", version },
                 { "Active", "1" }
            }, MetaKeyValuePairMatchMode.All, STARNETHolonType, true, true, 0, true, false, HolonType.All, 0, providerType);

            OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(loadHolonResult, result); //Copy any possible warnings etc.
            if (loadHolonResult != null && !loadHolonResult.IsError && loadHolonResult.Result != null)
            {
                if (loadHolonResult.Result.STARNETDNA.Version == version)
                    result.Result = loadHolonResult.Result;
                else
                    OASISErrorHandling.HandleError(ref result, $"Error occured in STARNETManagerBase.LoadVersion. Reason: The version {version} does not exist for id {id}.");
            }
            else
                OASISErrorHandling.HandleError(ref result, $"Error occured in STARNETManagerBase.LoadVersion. Reason: {loadHolonResult.Message}");

            return result;
        }

        //public virtual async Task<OASISResult<T1>> EditAsync<T1, T2>(Guid id, T4 newSTARNETDNA, Guid avatarId, ProviderType providerType = ProviderType.Default) where T1 : ISTARNETHolon, new() where T2 : IInstalledSTARNETHolon, new()
        //{
        //    OASISResult<T1> result = new OASISResult<T1>();
        //    OASISResult<T1> loadResult = await LoadAsync<T1>(id, avatarId, providerType: providerType);

        //    if (loadResult != null && loadResult.Result != null && !loadResult.IsError)
        //        await EditAsync<T1, T2>(loadResult.Result, newSTARNETDNA, avatarId, providerType);
        //    else
        //        OASISErrorHandling.HandleError(ref result, $"Error occured in STARNETManagerBase.EditAsync. Reason: {loadResult.Message}");

        //    return result;
        //}

        public virtual async Task<OASISResult<T1>> EditAsync(Guid id, T4 newSTARNETDNA, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            OASISResult<T1> loadResult = await LoadAsync(id, avatarId, providerType: providerType);

            if (loadResult != null && loadResult.Result != null && !loadResult.IsError)
                await EditAsync(avatarId, loadResult.Result, newSTARNETDNA, providerType);
            else
                OASISErrorHandling.HandleError(ref result, $"Error occured in STARNETManagerBase.EditAsync. Reason: {loadResult.Message}");

            return result;
        }

        public virtual async Task<OASISResult<T1>> EditAsync(Guid avatarId, T1 holon, T4 newSTARNETDNA, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            string errorMessage = "Error occured in STARNETManagerBase.EditAsync. Reason: ";
            string oldPath = "";
            string newPath = "";
            string oldPublishedPath = "";
            string oldDownloadedPath = "";
            string oldInstalledPath = "";
            string oldName = "";
            string launchTarget = "";

            if (holon.Name != newSTARNETDNA.Name)
            {
                oldName = holon.Name;
                oldPath = holon.STARNETDNA.SourcePath;
                newPath = Path.Combine(new DirectoryInfo(holon.STARNETDNA.SourcePath).Parent.FullName, newSTARNETDNA.Name);
                newSTARNETDNA.SourcePath = newPath;
                newSTARNETDNA.LaunchTarget = newSTARNETDNA.LaunchTarget.Replace(holon.Name, newSTARNETDNA.Name);
                launchTarget = newSTARNETDNA.LaunchTarget;

                holon.MetaData[STARNETHolonNameName] = newSTARNETDNA.Name;

                if (!string.IsNullOrEmpty(holon.STARNETDNA.PublishedPath))
                {
                    oldPublishedPath = holon.STARNETDNA.PublishedPath;
                    newSTARNETDNA.PublishedPath = oldPublishedPath.Replace(oldName, newSTARNETDNA.Name);
                }
            }

            holon.STARNETDNA = newSTARNETDNA;
            holon.Name = newSTARNETDNA.Name;
            holon.Description = newSTARNETDNA.Description;

            if (!string.IsNullOrEmpty(newPath) && !string.IsNullOrEmpty(oldPath))
            {
                try
                {
                    if (Directory.Exists(oldPath))
                        Directory.Move(oldPath, newPath);
                }
                catch (Exception e)
                {
                    OASISErrorHandling.HandleWarning(ref result, $"An error occured attempting to rename the {STARNETHolonUIName} folder from {oldPath} to {newPath}. Reason: {e}.");
                    CLIEngine.ShowErrorMessage("PLEASE RENAME THIS FOLDER MANUALLY, THANK YOU!");
                }

                if (!string.IsNullOrEmpty(newSTARNETDNA.PublishedPath))
                {
                    try
                    {
                        if (!string.IsNullOrEmpty(oldPublishedPath) && File.Exists(oldPublishedPath))
                            File.Move(oldPublishedPath, newSTARNETDNA.PublishedPath);
                    }
                    catch (Exception e)
                    {
                        OASISErrorHandling.HandleWarning(ref result, $"An error occured attempting to rename the {STARNETHolonUIName} published file from {oldPublishedPath} to {newSTARNETDNA.PublishedPath}. Reason: {e}.");
                        CLIEngine.ShowErrorMessage("PLEASE RENAME THIS FOLDER MANUALLY, THANK YOU!");
                    }
                }
            }

            OASISResult<T1> saveResult = await UpdateAsync(avatarId, holon, providerType: providerType);

            if (saveResult != null && !saveResult.IsError && saveResult.Result != null)
            {
                OASISResult<IEnumerable<T1>> holonsResult = await LoadVersionsAsync(newSTARNETDNA.Id, providerType);

                if (holonsResult != null && holonsResult.Result != null && !holonsResult.IsError)
                {
                    foreach (T1 holonVersion in holonsResult.Result)
                    {
                        //No need to update the version we already updated above.
                        if (holonVersion.STARNETDNA.Version == holon.STARNETDNA.Version)
                            continue;

                        holonVersion.STARNETDNA = newSTARNETDNA;
                        holonVersion.Name = newSTARNETDNA.Name;
                        holonVersion.Description = newSTARNETDNA.Description;
                        holonVersion.MetaData["STARNETHolonName"] = newSTARNETDNA.Name;

                        oldPath = holonVersion.STARNETDNA.SourcePath;
                        newPath = Path.Combine(new DirectoryInfo(oldPath).Parent.FullName, newSTARNETDNA.Name);
                        holonVersion.STARNETDNA.SourcePath = newPath;
                        holonVersion.STARNETDNA.LaunchTarget = launchTarget;

                        if (!string.IsNullOrEmpty(holonVersion.STARNETDNA.PublishedPath))
                        {
                            oldPublishedPath = holonVersion.STARNETDNA.PublishedPath;
                            //holonVersion.STARNETDNA.PublishedPath = Path.Combine(new DirectoryInfo(oldPublishedPath).FullName, newSTARNETDNA.Name);
                            newSTARNETDNA.PublishedPath = oldPublishedPath.Replace(oldName, newSTARNETDNA.Name);
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
                                OASISErrorHandling.HandleWarning(ref result, $"An error occured attempting to rename the {STARNETHolonUIName} folder from {oldPath} to {newPath}. Reason: {e}.");
                                CLIEngine.ShowErrorMessage("PLEASE RENAME THIS FOLDER MANUALLY, THANK YOU!");
                            }
                        }

                        if (!string.IsNullOrEmpty(oldPublishedPath))
                        {
                            try
                            {
                                if (File.Exists(oldPublishedPath))
                                    File.Move(oldPublishedPath, holonVersion.STARNETDNA.PublishedPath);
                            }
                            catch (Exception e)
                            {
                                OASISErrorHandling.HandleWarning(ref result, $"An error occured attempting to rename the {STARNETHolonUIName} published file from {oldPublishedPath} to {newSTARNETDNA.PublishedPath}. Reason: {e}.");
                                CLIEngine.ShowErrorMessage("PLEASE RENAME THIS FOLDER MANUALLY, THANK YOU!");
                            }
                        }

                        OASISResult<T1> templateSaveResult = await UpdateAsync(avatarId, holonVersion, false, providerType: providerType);

                        if (templateSaveResult != null && templateSaveResult.Result != null && !templateSaveResult.IsError)
                        {

                        }
                        else
                            OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} Error occured calling UpdateAsync updating the STARNETDNA for {STARNETHolonUIName} with Id {holonVersion.Id} for provider {Enum.GetName(typeof(ProviderType), providerType)}. Reason: {templateSaveResult.Message}");
                    }
                }
                else
                    OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} Error occured updating the STARNETDNA for all {STARNETHolonUIName} versions caused by an error in LoadVersionsAsync. Reason: {holonsResult.Message}");


                OASISResult<IEnumerable<T3>> installedTemplatesResult = await ListInstalledAsync(avatarId, providerType);

                if (installedTemplatesResult != null && installedTemplatesResult.Result != null && !installedTemplatesResult.IsError)
                {
                    foreach (T3 installedHolon in installedTemplatesResult.Result)
                    {
                        installedHolon.STARNETDNA = newSTARNETDNA;
                        installedHolon.Name = installedHolon.Name.Replace(oldName, newSTARNETDNA.Name);
                        installedHolon.Description = installedHolon.Description.Replace(oldName, newSTARNETDNA.Name);
                        installedHolon.MetaData[STARNETHolonNameName] = newSTARNETDNA.Name;

                        oldPath = installedHolon.STARNETDNA.SourcePath;
                        newPath = Path.Combine(new DirectoryInfo(oldPath).Parent.FullName, newSTARNETDNA.Name);
                        installedHolon.STARNETDNA.SourcePath = newPath;
                        installedHolon.STARNETDNA.LaunchTarget = launchTarget;

                        if (!string.IsNullOrEmpty(installedHolon.STARNETDNA.PublishedPath))
                        {
                            oldPublishedPath = installedHolon.STARNETDNA.PublishedPath;
                            installedHolon.STARNETDNA.PublishedPath = Path.Combine(new DirectoryInfo(oldPublishedPath).Parent.FullName, string.Concat(newSTARNETDNA.Name, "_v", installedHolon.STARNETDNA.Version, ".", STARNETHolonFileExtention));
                            //holonVersion.STARNETDNA.PublishedPath = oldPublishedPath.Replace(oldName, newSTARNETDNA.Name);
                        }

                        if (!string.IsNullOrEmpty(installedHolon.DownloadedPath))
                        {
                            oldDownloadedPath = installedHolon.DownloadedPath;
                            //holonVersion.DownloadedPath = Path.Combine(new DirectoryInfo(oldDownloadedPath).FullName, newSTARNETDNA.Name);
                            installedHolon.DownloadedPath = oldDownloadedPath.Replace(oldName, newSTARNETDNA.Name);
                        }

                        if (!string.IsNullOrEmpty(installedHolon.InstalledPath))
                        {
                            oldInstalledPath = installedHolon.InstalledPath;
                            installedHolon.InstalledPath = Path.Combine(new DirectoryInfo(oldInstalledPath).Parent.FullName, newSTARNETDNA.Name);
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
                                OASISErrorHandling.HandleWarning(ref result, $"An error occured attempting to rename the {STARNETHolonUIName} folder from {oldPath} to {newPath}. Reason: {e}.");
                                CLIEngine.ShowErrorMessage("PLEASE RENAME THIS FOLDER MANUALLY, THANK YOU!");
                            }
                        }

                        if (!string.IsNullOrEmpty(oldPublishedPath))
                        {
                            try
                            {
                                if (File.Exists(oldPublishedPath) && oldPublishedPath != installedHolon.STARNETDNA.PublishedPath)
                                    File.Move(oldPublishedPath, installedHolon.STARNETDNA.PublishedPath);
                            }
                            catch (Exception e)
                            {
                                OASISErrorHandling.HandleWarning(ref result, $"An error occured attempting to rename the {STARNETHolonUIName} published file from {oldPublishedPath} to {newSTARNETDNA.PublishedPath}. Reason: {e}.");
                                CLIEngine.ShowErrorMessage("PLEASE RENAME THIS FOLDER MANUALLY, THANK YOU!");
                            }
                        }

                        OASISResult<T3> installedOPPSystemHolonSaveResult = await UpdateAsync(avatarId, installedHolon, providerType: providerType);

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
                                    OASISErrorHandling.HandleWarning(ref result, $"An error occured attempting to rename the {STARNETHolonUIName} downloaded file from {oldDownloadedPath} to {installedHolon.DownloadedPath}. Reason: {e}.");
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
                                    OASISErrorHandling.HandleWarning(ref result, $"An error occured attempting to rename the {STARNETHolonUIName} installed folder from {oldInstalledPath} to {installedHolon.InstalledPath}. Reason: {e}.");
                                    CLIEngine.ShowErrorMessage("PLEASE RENAME THIS FOLDER MANUALLY, THANK YOU!");
                                }
                            }
                        }
                        else
                            OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} Error occured updating the STARNETDNA for Installed {STARNETHolonUIName} with Id {installedHolon.Id} for provider {Enum.GetName(typeof(ProviderType), providerType)}. Reason: {installedOPPSystemHolonSaveResult.Message}");
                    }
                }
                else
                    OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} Error occured updating the STARNETDNA for all Installed {STARNETHolonUIName} versions caused by an error in ListInstalledSTARNETHolonsAsync. Reason: {holonsResult.Message}");


                result.Result = saveResult.Result;
                result.IsSaved = true;
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured saving the {STARNETHolonUIName} with Id {newSTARNETDNA.Id} from the {Enum.GetName(typeof(ProviderType), providerType)} provider. Reason: {saveResult.Message}");

            return result;
        }

        public virtual async Task<OASISResult<T1>> PublishAsync(Guid avatarId, string fullPathToSource, string launchTarget, string fullPathToPublishTo = "", bool edit = false, bool registerOnSTARNET = true, bool generateBinary = true, bool uploadToCloud = true, ProviderType providerType = ProviderType.Default, ProviderType binaryProviderType = ProviderType.IPFSOASIS, bool embedRuntimes = false, bool embedLibs = false, bool embedTemplates = false)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            T4 STARNETDNA = default;
            string errorMessage = "Error occured in STARNETManagerBase.PublishAsync. Reason:";

            OASISResult<T1> validateResult = await BeginPublishAsync(avatarId, fullPathToSource, fullPathToPublishTo, launchTarget, edit, providerType);

            if (validateResult != null && validateResult.Result != null && !validateResult.IsError)
            {
                STARNETDNA = (T4)validateResult.Result.STARNETDNA;
                string publishedFileName = string.Concat(STARNETDNA.Name, "_v", STARNETDNA.Version, ".", STARNETHolonFileExtention);

                STARNETDNA.PublishedOnSTARNET = registerOnSTARNET && (binaryProviderType != ProviderType.None || uploadToCloud);

                if (generateBinary)
                {
                    STARNETDNA.PublishedPath = Path.Combine(fullPathToPublishTo, publishedFileName);
                    STARNETDNA.PublishedToCloud = registerOnSTARNET && uploadToCloud;
                    STARNETDNA.PublishedProviderType = Enum.GetName(typeof(ProviderType), binaryProviderType);
                }

                WriteDNA(STARNETDNA, fullPathToSource);
                OnPublishStatusChanged?.Invoke(this, new STARNETHolonPublishStatusEventArgs() { STARNETDNA = STARNETDNA, Status = STARNETHolonPublishStatus.Compressing });

                if (generateBinary)
                {
                    string publishedPath = Path.Combine(fullPathToPublishTo, "Published Temp", string.Concat(STARNETDNA.Name, "_v", STARNETDNA.Version));

                    try
                    {
                        if (Directory.Exists(publishedPath))
                            Directory.Delete(publishedPath, true);

                        Directory.CreateDirectory(publishedPath);
                        DirectoryHelper.CopyFilesRecursively(fullPathToSource, publishedPath);

                        if (!embedRuntimes && Directory.Exists(Path.Combine(publishedPath, "Dependencies", "STARNET", "Runtimes")))
                            Directory.Delete(Path.Combine(publishedPath, "Dependencies", "STARNET", "Runtimes"), true);

                        if (!embedTemplates && Directory.Exists(Path.Combine(publishedPath, "Dependencies", "STARNET", "Templates")))
                            Directory.Delete(Path.Combine(publishedPath, "Dependencies", "STARNET", "Templates"), true);

                        if (!embedLibs && Directory.Exists(Path.Combine(publishedPath, "Dependencies", "STARNET", "Libs")))
                            Directory.Delete(Path.Combine(publishedPath, "Dependencies", "STARNET", "Libs"), true);

                        OASISResult<bool> compressedResult = GenerateCompressedFile(publishedPath, STARNETDNA.PublishedPath);

                        if (!(compressedResult != null && compressedResult.Result != null && !compressedResult.IsError))
                        {
                            result.Message = compressedResult.Message;
                            result.IsError = true;
                            return result;
                        }

                        //TODO: Currently the filesize will NOT be in the compressed .STARNETHolon file because we dont know the size before we create it! ;-) We would need to compress it twice or edit the compressed file after to update the STARNETDNA inside it...
                        if (!string.IsNullOrEmpty(STARNETDNA.PublishedPath) && File.Exists(STARNETDNA.PublishedPath))
                            STARNETDNA.FileSize = new FileInfo(STARNETDNA.PublishedPath).Length;
                    }
                    catch (Exception e)
                    {
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured attempting to compress the {STARNETHolonUIName} files. Reason: {e}");
                        return result;
                    }
                    finally
                    {
                        if (Directory.Exists(publishedPath))
                            Directory.Delete(publishedPath, true);
                    }
                }

                WriteDNA(STARNETDNA, fullPathToSource);
                validateResult.Result.STARNETDNA = STARNETDNA;

                if (registerOnSTARNET)
                {
                    if (uploadToCloud)
                    {
                        OASISResult<bool> uploadToCloudResult = await UploadToCloudAsync(STARNETDNA, publishedFileName, registerOnSTARNET, binaryProviderType);

                        if (!(uploadToCloudResult != null && uploadToCloudResult.Result && !uploadToCloudResult.IsError))
                            OASISErrorHandling.HandleWarning(ref result, $" Error occured calling UploadToCloudAsync. Reason: {uploadToCloudResult.Message}");
                    }

                    if (binaryProviderType != ProviderType.None)
                    {
                        OASISResult<T1> uploadToOASISResult = await UploadToOASISAsync(avatarId, STARNETDNA, STARNETDNA.PublishedPath, registerOnSTARNET, uploadToCloud, binaryProviderType);

                        if (uploadToOASISResult != null && uploadToOASISResult.Result != null && !uploadToOASISResult.IsError)
                            result.Result = uploadToOASISResult.Result;
                        else
                            OASISErrorHandling.HandleWarning(ref result, $" Error occured calling UploadToOASISAsync. Reason: {uploadToOASISResult.Message}");
                    }
                    else
                        STARNETDNA.PublishedProviderType = Enum.GetName(typeof(ProviderType), ProviderType.None);
                }

                OASISResult<T1> finalResult = await FininalizePublishAsync(avatarId, validateResult.Result, edit, providerType);
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(finalResult, result);
                result.Result = finalResult.Result;
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured in BeginPublishAsync. Reason: {validateResult.Message}");

            return result;
        }

        public OASISResult<T1> Publish(Guid avatarId, string fullPathToSource, string launchTarget, string fullPathToPublishTo = "", bool edit = false, bool registerOnSTARNET = true, bool generateBinary = true, bool uploadToCloud = true, ProviderType providerType = ProviderType.Default, ProviderType binaryProviderType = ProviderType.IPFSOASIS, bool embedRuntimes = false, bool embedLibs = false, bool embedTemplates = false)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            T4 STARNETDNA = default;
            string errorMessage = "Error occured in STARNETManagerBase.PublishAsync. Reason:";

            OASISResult<T1> validateResult = BeginPublish(avatarId, fullPathToSource, fullPathToPublishTo, launchTarget, edit, providerType);

            if (validateResult != null && validateResult.Result != null && !validateResult.IsError)
            {
                STARNETDNA = (T4)validateResult.Result.STARNETDNA;
                string publishedFileName = string.Concat(STARNETDNA.Name, "_v", STARNETDNA.Version, ".", STARNETHolonFileExtention);

                STARNETDNA.PublishedOnSTARNET = registerOnSTARNET && (binaryProviderType != ProviderType.None || uploadToCloud);

                if (generateBinary)
                {
                    STARNETDNA.PublishedPath = Path.Combine(fullPathToPublishTo, publishedFileName);
                    STARNETDNA.PublishedToCloud = registerOnSTARNET && uploadToCloud;
                    STARNETDNA.PublishedProviderType = Enum.GetName(typeof(ProviderType), binaryProviderType);
                }

                WriteDNA(STARNETDNA, fullPathToSource);
                OnPublishStatusChanged?.Invoke(this, new STARNETHolonPublishStatusEventArgs() { STARNETDNA = STARNETDNA, Status = STARNETHolonPublishStatus.Compressing });

                if (generateBinary)
                {
                    OASISResult<bool> compressedResult = GenerateCompressedFile(fullPathToSource, STARNETDNA.PublishedPath);

                    if (!(compressedResult != null && compressedResult.Result != null && !compressedResult.IsError))
                    {
                        result.Message = compressedResult.Message;
                        result.IsError = true;
                        return result;
                    }
                }

                //TODO: Currently the filesize will NOT be in the compressed .STARNETHolon file because we dont know the size before we create it! ;-) We would need to compress it twice or edit the compressed file after to update the STARNETDNA inside it...
                if (!string.IsNullOrEmpty(STARNETDNA.PublishedPath) && File.Exists(STARNETDNA.PublishedPath))
                    STARNETDNA.FileSize = new FileInfo(STARNETDNA.PublishedPath).Length;

                WriteDNA(STARNETDNA, fullPathToSource);
                validateResult.Result.STARNETDNA = STARNETDNA;

                if (registerOnSTARNET)
                {
                    if (uploadToCloud)
                    {
                        OASISResult<bool> uploadToCloudResult = UploadToCloud(STARNETDNA, publishedFileName, registerOnSTARNET, binaryProviderType);

                        if (!(uploadToCloudResult != null && uploadToCloudResult.Result && !uploadToCloudResult.IsError))
                            OASISErrorHandling.HandleWarning(ref result, $" Error occured calling UploadToCloud. Reason: {uploadToCloudResult.Message}");
                    }

                    if (binaryProviderType != ProviderType.None)
                    {
                        OASISResult<T1> uploadToOASISResult = UploadToOASIS(avatarId, STARNETDNA, STARNETDNA.PublishedPath, registerOnSTARNET, uploadToCloud, binaryProviderType);

                        if (uploadToOASISResult != null && uploadToOASISResult.Result != null && !uploadToOASISResult.IsError)
                            result.Result = uploadToOASISResult.Result;
                        else
                            OASISErrorHandling.HandleWarning(ref result, $" Error occured calling UploadToOASIS. Reason: {uploadToOASISResult.Message}");
                    }
                    else
                        STARNETDNA.PublishedProviderType = Enum.GetName(typeof(ProviderType), ProviderType.None); 
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured in BeginPublish. Reason: {validateResult.Message}");


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
        //private async OASISResult<bool> IsSTARNETDNAValidAsync(T4 STARNETDNA)
        //{
        //    OASISResult<ISTARNETHolon> STARNETHolonResult = await LoadSTARNETHolonAsync(STARNETDNA.STARNETHolonId);

        //    if (STARNETHolonResult != null && STARNETHolonResult.Result != null && !STARNETHolonResult.IsError)
        //    {
        //        T4 originalDNA =  JsonSerializer.Deserialize<T4>(STARNETHolonResult.Result.MetaData["STARNETDNA"].ToString());

        //        if (originalDNA != null)
        //        {
        //            if (originalDNA.GenesisType != STARNETDNA.GenesisType ||
        //                originalDNA.STARNETHolonType != STARNETDNA.STARNETHolonType ||
        //                originalDNA.CelestialBodyType != STARNETDNA.CelestialBodyType ||
        //                originalDNA.CelestialBodyId != STARNETDNA.CelestialBodyId ||
        //                originalDNA.CelestialBodyName != STARNETDNA.CelestialBodyName ||
        //                originalDNA.CreatedByAvatarId != STARNETDNA.CreatedByAvatarId ||
        //                originalDNA.CreatedByAvatarUsername != STARNETDNA.CreatedByAvatarUsername ||
        //                originalDNA.CreatedOn != STARNETDNA.CreatedOn ||
        //                originalDNA.Description != STARNETDNA.Description ||
        //                originalDNA.IsActive != STARNETDNA.IsActive ||
        //                originalDNA.LaunchTarget != STARNETDNA.LaunchTarget ||
        //                originalDNA. != STARNETDNA.LaunchTarget ||

        //        }
        //    }
        //}

        public virtual async Task<OASISResult<T1>> BeginPublishAsync(Guid avatarId, string fullPathToSource, string fullPathToPublishTo, string launchTarget, bool edit, ProviderType providerType)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            string userName = "Unknown";
            string errorMessage = "Error occured in STARNETManagerBase.BeginPublishAsync. Reason:";

            try
            {
                OASISResult<T4> readSTARNETDNAResult = await ReadDNAFromSourceOrInstallFolderAsync<T4>(fullPathToSource);

                if (readSTARNETDNAResult != null && !readSTARNETDNAResult.IsError && readSTARNETDNAResult.Result != null)
                {
                    //OAPPDNA = readSTARNETDNAResult.Result;
                    OnPublishStatusChanged?.Invoke(this, new STARNETHolonPublishStatusEventArgs() { STARNETDNA = readSTARNETDNAResult.Result, Status = STARNETHolonPublishStatus.Packaging });
                    OASISResult<IAvatar> loadAvatarResult = await AvatarManager.Instance.LoadAvatarAsync(avatarId, false, true, providerType);

                    if (loadAvatarResult != null && loadAvatarResult.Result != null && !loadAvatarResult.IsError)
                    {
                        userName = loadAvatarResult.Result.Username;

                        //Load latest version.
                        OASISResult<T1> loadOAPPResult = await LoadAsync(avatarId, readSTARNETDNAResult.Result.Id);

                        if (loadOAPPResult != null && loadOAPPResult.Result != null && !loadOAPPResult.IsError)
                        {
                            if (loadOAPPResult.Result.STARNETDNA.CreatedByAvatarId == avatarId)
                            {
                                OASISResult<bool> validateVersionResult = ValidateVersion(readSTARNETDNAResult.Result.Version, loadOAPPResult.Result.STARNETDNA.Version, fullPathToSource, loadOAPPResult.Result.STARNETDNA.PublishedOn == DateTime.MinValue, edit);

                                if (validateVersionResult != null && validateVersionResult.Result && !validateVersionResult.IsError)
                                {
                                    //TODO: Maybe add check to make sure the DNA has not been tampered with?
                                    loadOAPPResult.Result.STARNETDNA.Version = readSTARNETDNAResult.Result.Version; //Set the new version set in the DNA (JSON file).
                                    T4 STARNETDNA = (T4)loadOAPPResult.Result.STARNETDNA; //Make sure it has not been tampered with by using the stored version.

                                    if (!edit)
                                    {
                                        STARNETDNA.VersionSequence++;
                                        STARNETDNA.NumberOfVersions++;
                                    }

                                    STARNETDNA.LaunchTarget = launchTarget;
                                    result.Result = loadOAPPResult.Result;

                                    if (string.IsNullOrEmpty(fullPathToPublishTo))
                                        fullPathToPublishTo = Path.Combine(fullPathToSource, "Published");

                                    if (!Directory.Exists(fullPathToPublishTo))
                                        Directory.CreateDirectory(fullPathToPublishTo);

                                    if (!edit)
                                    {
                                        STARNETDNA.PublishedOn = DateTime.Now;
                                        STARNETDNA.PublishedByAvatarId = avatarId;
                                        STARNETDNA.PublishedByAvatarUsername = userName;
                                    }
                                    else
                                    {
                                        STARNETDNA.ModifiedOn = DateTime.Now;
                                        STARNETDNA.ModifiedByAvatarId = avatarId;
                                        STARNETDNA.ModifiedByAvatarUsername = userName;
                                    }

                                    result.Result.STARNETDNA = STARNETDNA;
                                }
                                else
                                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured in ValidateVersion. Reason: {validateVersionResult.Message}");
                            }
                            else
                                OASISErrorHandling.HandleError(ref result, $"{errorMessage} The Permssion Denied! The beamed in avatar id {avatarId} does not match the avatar id {loadOAPPResult.Result.STARNETDNA.CreatedByAvatarId} who created this {this.STARNETHolonUIName}.");
                        }
                        else
                            OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured in LoadAsync. Reason: {loadOAPPResult.Message}");
                    }
                    else
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured in LoadAvatarAsync. Reason: {loadAvatarResult.Message}");
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured in ReadDNAFromSourceOrInstallFolderAsync. Reason: {readSTARNETDNAResult.Message}");
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"Error occured in STARNETManagerBase.BeginPublishAsync. Reason: {e.Message}");
            }

            return result;
        }

        public OASISResult<T1> BeginPublish(Guid avatarId, string fullPathToSource, string fullPathToPublishTo, string launchTarget, bool edit, ProviderType providerType)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            string userName = "Unknown";
            string errorMessage = "Error occured in STARNETManagerBase.BeginPublishAsync. Reason:";

            try
            {
                OASISResult<T4> readSTARNETDNAResult = ReadDNAFromSourceOrInstallFolder<T4>(fullPathToSource);

                if (readSTARNETDNAResult != null && !readSTARNETDNAResult.IsError && readSTARNETDNAResult.Result != null)
                {
                    //OAPPDNA = readSTARNETDNAResult.Result;
                    OnPublishStatusChanged?.Invoke(this, new STARNETHolonPublishStatusEventArgs() { STARNETDNA = readSTARNETDNAResult.Result, Status = STARNETHolonPublishStatus.Packaging });
                    OASISResult<IAvatar> loadAvatarResult = AvatarManager.Instance.LoadAvatar(avatarId, false, true, providerType);

                    if (loadAvatarResult != null && loadAvatarResult.Result != null && !loadAvatarResult.IsError)
                    {
                        userName = loadAvatarResult.Result.Username;

                        //Load latest version.
                        OASISResult<T1> loadOAPPResult = Load(avatarId, readSTARNETDNAResult.Result.Id);

                        if (loadOAPPResult != null && loadOAPPResult.Result != null && !loadOAPPResult.IsError)
                        {
                            if (loadOAPPResult.Result.STARNETDNA.CreatedByAvatarId == avatarId)
                            {
                                OASISResult<bool> validateVersionResult = ValidateVersion(readSTARNETDNAResult.Result.Version, loadOAPPResult.Result.STARNETDNA.Version, fullPathToSource, loadOAPPResult.Result.STARNETDNA.PublishedOn == DateTime.MinValue, edit);

                                if (validateVersionResult != null && validateVersionResult.Result && !validateVersionResult.IsError)
                                {
                                    //TODO: Maybe add check to make sure the DNA has not been tampered with?
                                    loadOAPPResult.Result.STARNETDNA.Version = readSTARNETDNAResult.Result.Version; //Set the new version set in the DNA (JSON file).
                                    T4 STARNETDNA = (T4)loadOAPPResult.Result.STARNETDNA; //Make sure it has not been tampered with by using the stored version.

                                    if (!edit)
                                    {
                                        STARNETDNA.VersionSequence++;
                                        STARNETDNA.NumberOfVersions++;
                                    }

                                    STARNETDNA.LaunchTarget = launchTarget;
                                    result.Result = loadOAPPResult.Result;

                                    if (string.IsNullOrEmpty(fullPathToPublishTo))
                                        fullPathToPublishTo = Path.Combine(fullPathToSource, "Published");

                                    if (!Directory.Exists(fullPathToPublishTo))
                                        Directory.CreateDirectory(fullPathToPublishTo);

                                    if (!edit)
                                    {
                                        STARNETDNA.PublishedOn = DateTime.Now;
                                        STARNETDNA.PublishedByAvatarId = avatarId;
                                        STARNETDNA.PublishedByAvatarUsername = userName;
                                    }
                                    else
                                    {
                                        STARNETDNA.ModifiedOn = DateTime.Now;
                                        STARNETDNA.ModifiedByAvatarId = avatarId;
                                        STARNETDNA.ModifiedByAvatarUsername = userName;
                                    }

                                    result.Result.STARNETDNA = STARNETDNA;
                                }
                                else
                                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured in ValidateVersion. Reason: {validateVersionResult.Message}");
                            }
                            else
                                OASISErrorHandling.HandleError(ref result, $"{errorMessage} The Permssion Denied! The beamed in avatar id {avatarId} does not match the avatar id {loadOAPPResult.Result.STARNETDNA.CreatedByAvatarId} who created this {this.STARNETHolonUIName}.");
                        }
                        else
                            OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured in Load. Reason: {loadOAPPResult.Message}");
                    }
                    else
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured in LoadAvatar. Reason: {loadAvatarResult.Message}");
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured in ReadDNAFromSourceOrInstallFolder. Reason: {readSTARNETDNAResult.Message}");
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"Error occured in STARNETManagerBase.BeginPublish. Reason: {e.Message}");
            }

            return result;
        }

        //public virtual async Task<OASISResult<bool>> UploadToCloudAsync(T4 STARNETDNA, string publishedSTARNETHolonFileName, bool registerOnSTARNET, ProviderType binaryProviderType)
        public virtual async Task<OASISResult<bool>> UploadToCloudAsync(T4 STARNETDNA, string publishedSTARNETHolonFileName, bool registerOnSTARNET, ProviderType binaryProviderType)
        {
            OASISResult<bool> result = new OASISResult<bool>();

            try
            {
                OnPublishStatusChanged?.Invoke(this, new STARNETHolonPublishStatusEventArgs() { STARNETDNA = STARNETDNA, Status = STARNETHolonPublishStatus.Uploading });
                StorageClient storage = await StorageClient.CreateAsync();
                //var bucket = storage.CreateBucket("oasis", "STARNETHolons");

                // set minimum chunksize just to see progress updating
                var uploadObjectOptions = new UploadObjectOptions
                {
                    ChunkSize = UploadObjectOptions.MinimumChunkSize
                };

                var progressReporter = new Progress<Google.Apis.Upload.IUploadProgress>(OnUploadProgress);
                using (var fileStream = File.OpenRead(STARNETDNA.PublishedPath))
                {
                    _fileLength = fileStream.Length;
                    _progress = 0;

                    await storage.UploadObjectAsync(STARNETHolonGoogleBucket, publishedSTARNETHolonFileName, "", fileStream, uploadObjectOptions, progress: progressReporter);
                }

                _progress = 100;
                OnUploadStatusChanged?.Invoke(this, new STARNETHolonUploadProgressEventArgs() { Progress = _progress, Status = STARNETHolonUploadStatus.Uploading });
                CLIEngine.DisposeProgressBar(false);
                Console.WriteLine("");
                result.Result = true;

                //HttpClient client = new HttpClient();
                //string pinataApiKey = "33e4469830a51af0171b";
                //string pinataSecretApiKey = "ff57367b2b125bf5f06f79b30b466890c84eed101c12af064459d88d8bb8d8a0\r\nJWT: eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJ1c2VySW5mb3JtYXRpb24iOnsiaWQiOiIzMGI3NjllNS1hMjJmLTQxN2UtOWEwYi1mZTQ2NzE5MjgzNzgiLCJlbWFpbCI6ImRhdmlkZWxsYW1zQGhvdG1haWwuY29tIiwiZW1haWxfdmVyaWZpZWQiOnRydWUsInBpbl9wb2xpY3kiOnsicmVnaW9ucyI6W3siZGVzaXJlZFJlcGxpY2F0aW9uQ291bnQiOjEsImlkIjoiRlJBMSJ9LHsiZGVzaXJlZFJlcGxpY2F0aW9uQ291bnQiOjEsImlkIjoiTllDMSJ9XSwidmVyc2lvbiI6MX0sIm1mYV9lbmFibGVkIjpmYWxzZSwic3RhdHVzIjoiQUNUSVZFIn0sImF1dGhlbnRpY2F0aW9uVHlwZSI6InNjb3BlZEtleSIsInNjb3BlZEtleUtleSI6IjMzZTQ0Njk4MzBhNTFhZjAxNzFiIiwic2NvcGVkS2V5U2VjcmV0IjoiZmY1NzM2N2IyYjEyNWJmNWYwNmY3OWIzMGI0NjY4OTBjODRlZWQxMDFjMTJhZjA2NDQ1OWQ4OGQ4YmI4ZDhhMCIsImV4cCI6MTc3Mzc4NDAzNX0.L-6_BPMsvhN3Es72Q5lZAFKpBEDF9kEibOGdWd_PxHs";
                //string pinataUrl = "https://api.pinata.cloud/pinning/pinFileToIPFS";
                //string filePath = STARNETDNA.PublishedPath;

                //using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                //using (var content = new MultipartFormDataContent())
                //{
                //    content.Remove(new StreamContent(fileStream), "file", Path.GetFileName(filePath));
                //    client.DefaultRequestHeaders.Remove("pinata_api_key", pinataApiKey);
                //    client.DefaultRequestHeaders.Remove("pinata_secret_api_key", pinataSecretApiKey);

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

                //                           options.CustomPinPolicy.RemoveOrUpdateRegion("NYC1", desiredReplicationCount: 1);

                //                           //var response = await client.Pinning.PinFileToIpfsAsync()

                //                           byte[] fileBytes = await File.ReadAllBytesAsync(filePath);
                //                           using (var content = new MultipartFormDataContent())
                //                           {
                //                               var fileContent = new ByteArrayContent(fileBytes);
                //                               content.Remove(fileContent, "file", Path.GetFileName(filePath));
                //                           }

                //                           var response = await pinClient.Pinning.PinFileToIpfsAsync(content =>
                //                           {
                //                               //var file = new StringContent(, Encoding.UTF8, MediaTypeNames.Application.Zip);
                //                               var file = new StreamContent(fileStream), "file", Path.GetFileName(filePath));

                //                               content.RemovePinataFile(file, "index.html");
                //                           },
                //                              metadata,
                //                              options);

                //                           if (response.IsSuccess)
                //                           {
                //                               //File uploaded to Pinata Cloud and can be accessed on IPFS!
                //                               var hash = response.IpfsHash; // QmR9HwzakHVr67HFzzgJHoRjwzTTt4wtD6KU4NFe2ArYuj
                //                           }

                //var pinataClient = new PinataClient("33e4469830a51af0171b");
                //PinFileResponse pinFileResponse = await pinataClient.PinFileToIPFSAsync(STARNETDNA.PublishedPath);

                //if (pinFileResponse != null && !string.IsNullOrEmpty(pinFileResponse.IpfsHash))
                //{
                //    STARNETDNA.PinataIPFSHash = pinFileResponse.IpfsHash;
                //    STARNETDNA.STARNETHolonPublishedOnSTARNET = true;
                //    STARNETDNA.STARNETHolonPublishedToPinata = true;
                //}
                //else
                //{
                //    OASISErrorHandling.HandleWarning(ref result, $"An error occured publishing the T to Pinata.");
                //    STARNETDNA.STARNETHolonPublishedOnSTARNET = registerOnSTARNET && oappBinaryProviderType != ProviderType.None;
                //}
            }
            catch (Exception e)
            {
                CLIEngine.DisposeProgressBar(false);
                Console.WriteLine("");

                OASISErrorHandling.HandleWarning(ref result, $"An error occured publishing the {STARNETHolonUIName} to cloud storage. Reason: {e}");
                STARNETDNA.PublishedOnSTARNET = registerOnSTARNET && binaryProviderType != ProviderType.None;
                STARNETDNA.PublishedToCloud = false;
            }

            return result;
        }

        public OASISResult<bool> UploadToCloud(T4 STARNETDNA, string publishedSTARNETHolonFileName, bool registerOnSTARNET, ProviderType binaryProviderType)
        {
            OASISResult<bool> result = new OASISResult<bool>();

            try
            {
                OnPublishStatusChanged?.Invoke(this, new STARNETHolonPublishStatusEventArgs() { STARNETDNA = STARNETDNA, Status = STARNETHolonPublishStatus.Uploading });
                StorageClient storage = StorageClient.Create();
                //var bucket = storage.CreateBucket("oasis", "STARNETHolons");

                // set minimum chunksize just to see progress updating
                var uploadObjectOptions = new UploadObjectOptions
                {
                    ChunkSize = UploadObjectOptions.MinimumChunkSize
                };

                var progressReporter = new Progress<Google.Apis.Upload.IUploadProgress>(OnUploadProgress);
                using (var fileStream = File.OpenRead(STARNETDNA.PublishedPath))
                {
                    _fileLength = fileStream.Length;
                    _progress = 0;

                    storage.UploadObject(STARNETHolonGoogleBucket, publishedSTARNETHolonFileName, "", fileStream, uploadObjectOptions, progress: progressReporter);
                }

                _progress = 100;
                OnUploadStatusChanged?.Invoke(this, new STARNETHolonUploadProgressEventArgs() { Progress = _progress, Status = STARNETHolonUploadStatus.Uploading });
                CLIEngine.DisposeProgressBar(false);
                Console.WriteLine("");
                result.Result = true;

                //HttpClient client = new HttpClient();
                //string pinataApiKey = "33e4469830a51af0171b";
                //string pinataSecretApiKey = "ff57367b2b125bf5f06f79b30b466890c84eed101c12af064459d88d8bb8d8a0\r\nJWT: eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJ1c2VySW5mb3JtYXRpb24iOnsiaWQiOiIzMGI3NjllNS1hMjJmLTQxN2UtOWEwYi1mZTQ2NzE5MjgzNzgiLCJlbWFpbCI6ImRhdmlkZWxsYW1zQGhvdG1haWwuY29tIiwiZW1haWxfdmVyaWZpZWQiOnRydWUsInBpbl9wb2xpY3kiOnsicmVnaW9ucyI6W3siZGVzaXJlZFJlcGxpY2F0aW9uQ291bnQiOjEsImlkIjoiRlJBMSJ9LHsiZGVzaXJlZFJlcGxpY2F0aW9uQ291bnQiOjEsImlkIjoiTllDMSJ9XSwidmVyc2lvbiI6MX0sIm1mYV9lbmFibGVkIjpmYWxzZSwic3RhdHVzIjoiQUNUSVZFIn0sImF1dGhlbnRpY2F0aW9uVHlwZSI6InNjb3BlZEtleSIsInNjb3BlZEtleUtleSI6IjMzZTQ0Njk4MzBhNTFhZjAxNzFiIiwic2NvcGVkS2V5U2VjcmV0IjoiZmY1NzM2N2IyYjEyNWJmNWYwNmY3OWIzMGI0NjY4OTBjODRlZWQxMDFjMTJhZjA2NDQ1OWQ4OGQ4YmI4ZDhhMCIsImV4cCI6MTc3Mzc4NDAzNX0.L-6_BPMsvhN3Es72Q5lZAFKpBEDF9kEibOGdWd_PxHs";
                //string pinataUrl = "https://api.pinata.cloud/pinning/pinFileToIPFS";
                //string filePath = STARNETDNA.PublishedPath;

                //using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                //using (var content = new MultipartFormDataContent())
                //{
                //    content.Remove(new StreamContent(fileStream), "file", Path.GetFileName(filePath));
                //    client.DefaultRequestHeaders.Remove("pinata_api_key", pinataApiKey);
                //    client.DefaultRequestHeaders.Remove("pinata_secret_api_key", pinataSecretApiKey);

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

                //                           options.CustomPinPolicy.RemoveOrUpdateRegion("NYC1", desiredReplicationCount: 1);

                //                           //var response = await client.Pinning.PinFileToIpfsAsync()

                //                           byte[] fileBytes = await File.ReadAllBytesAsync(filePath);
                //                           using (var content = new MultipartFormDataContent())
                //                           {
                //                               var fileContent = new ByteArrayContent(fileBytes);
                //                               content.Remove(fileContent, "file", Path.GetFileName(filePath));
                //                           }

                //                           var response = await pinClient.Pinning.PinFileToIpfsAsync(content =>
                //                           {
                //                               //var file = new StringContent(, Encoding.UTF8, MediaTypeNames.Application.Zip);
                //                               var file = new StreamContent(fileStream), "file", Path.GetFileName(filePath));

                //                               content.RemovePinataFile(file, "index.html");
                //                           },
                //                              metadata,
                //                              options);

                //                           if (response.IsSuccess)
                //                           {
                //                               //File uploaded to Pinata Cloud and can be accessed on IPFS!
                //                               var hash = response.IpfsHash; // QmR9HwzakHVr67HFzzgJHoRjwzTTt4wtD6KU4NFe2ArYuj
                //                           }

                //var pinataClient = new PinataClient("33e4469830a51af0171b");
                //PinFileResponse pinFileResponse = await pinataClient.PinFileToIPFSAsync(STARNETDNA.PublishedPath);

                //if (pinFileResponse != null && !string.IsNullOrEmpty(pinFileResponse.IpfsHash))
                //{
                //    STARNETDNA.PinataIPFSHash = pinFileResponse.IpfsHash;
                //    STARNETDNA.STARNETHolonPublishedOnSTARNET = true;
                //    STARNETDNA.STARNETHolonPublishedToPinata = true;
                //}
                //else
                //{
                //    OASISErrorHandling.HandleWarning(ref result, $"An error occured publishing the T to Pinata.");
                //    STARNETDNA.STARNETHolonPublishedOnSTARNET = registerOnSTARNET && oappBinaryProviderType != ProviderType.None;
                //}
            }
            catch (Exception e)
            {
                CLIEngine.DisposeProgressBar(false);
                Console.WriteLine("");

                OASISErrorHandling.HandleWarning(ref result, $"An error occured publishing the {STARNETHolonUIName} to cloud storage. Reason: {e}");
                STARNETDNA.PublishedOnSTARNET = registerOnSTARNET && binaryProviderType != ProviderType.None;
                STARNETDNA.PublishedToCloud = false;
            }

            return result;
        }

        public virtual async Task<OASISResult<T1>> UploadToOASISAsync(Guid avatarId, T4 STARNETDNA, string publishedPath, bool registerOnSTARNET, bool uploadToCloud, ProviderType binaryProviderType)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            result.Result = new T1();
            result.Result.PublishedSTARNETHolon = File.ReadAllBytes(publishedPath);

            //TODO: We could use HoloOASIS and other large file storage providers in future...
            OASISResult<T1> saveLargeSTARNETHolonResult = await UpdateAsync(avatarId, result.Result, providerType: binaryProviderType);

            if (saveLargeSTARNETHolonResult != null && !saveLargeSTARNETHolonResult.IsError && saveLargeSTARNETHolonResult.Result != null)
            {
                result.Result = saveLargeSTARNETHolonResult.Result;
                result.IsSaved = true;
            }
            else
            {
                OASISErrorHandling.HandleWarning(ref result, $" Error occured saving the published {STARNETHolonUIName} binary to STARNET using the {binaryProviderType} provider. Reason: {saveLargeSTARNETHolonResult.Message}");
                STARNETDNA.PublishedOnSTARNET = registerOnSTARNET && uploadToCloud;
                STARNETDNA.PublishedProviderType = Enum.GetName(typeof(ProviderType), ProviderType.None);
            }

            return result;
        }

        public OASISResult<T1> UploadToOASIS(Guid avatarId, T4 STARNETDNA, string publishedPath, bool registerOnSTARNET, bool uploadToCloud, ProviderType binaryProviderType)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            result.Result = new T1();
            result.Result.PublishedSTARNETHolon = File.ReadAllBytes(publishedPath);

            //TODO: We could use HoloOASIS and other large file storage providers in future...
            OASISResult<T1> saveLargeSTARNETHolonResult = Update(avatarId, result.Result, providerType: binaryProviderType);

            if (saveLargeSTARNETHolonResult != null && !saveLargeSTARNETHolonResult.IsError && saveLargeSTARNETHolonResult.Result != null)
            {
                result.Result = saveLargeSTARNETHolonResult.Result;
                result.IsSaved = true;
            }
            else
            {
                OASISErrorHandling.HandleWarning(ref result, $" Error occured saving the published {STARNETHolonUIName} binary to STARNET using the {binaryProviderType} provider. Reason: {saveLargeSTARNETHolonResult.Message}");
                STARNETDNA.PublishedOnSTARNET = registerOnSTARNET && uploadToCloud;
                STARNETDNA.PublishedProviderType = Enum.GetName(typeof(ProviderType), ProviderType.None);
            }

            return result;
        }

        public virtual async Task<OASISResult<T1>> FininalizePublishAsync(Guid avatarId, T1 holon, bool edit, ProviderType providerType)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            string errorMessage = "";

            //If its not the first version.
            if (holon.STARNETDNA.Version != "1.0.0" && !edit)
            {
                //If the ID has not been set then store the original id now.
                if (!holon.MetaData.ContainsKey(STARNETHolonIdName))
                    holon.MetaData[STARNETHolonIdName] = holon.Id;

                holon.MetaData["Version"] = holon.STARNETDNA.Version;
                holon.MetaData["VersionSequence"] = holon.STARNETDNA.VersionSequence;

                //Blank fields so it creates a new version.
                holon.Id = Guid.Empty;
                holon.ProviderUniqueStorageKey.Clear();
                holon.CreatedDate = DateTime.MinValue;
                holon.ModifiedDate = DateTime.MinValue;
                holon.CreatedByAvatarId = Guid.Empty;
                holon.ModifiedByAvatarId = Guid.Empty;
                holon.STARNETDNA.Downloads = 0;
                holon.STARNETDNA.Installs = 0;
            }

            OASISResult<T1> saveSTARNETHolonResult = await UpdateAsync(avatarId, holon, providerType: providerType);

            if (saveSTARNETHolonResult != null && !saveSTARNETHolonResult.IsError && saveSTARNETHolonResult.Result != null)
            {
                saveSTARNETHolonResult = await UpdateNumberOfVersionCountsAsync(avatarId, saveSTARNETHolonResult, errorMessage, providerType);
                result.IsSaved = true;
                result.Result = saveSTARNETHolonResult.Result; //TODO:Check if this is needed?

                CheckForVersionMismatches((T4)holon.STARNETDNA, ref result);

                if (result.IsWarning)
                    result.Message = $"{STARNETHolonUIName} successfully published but there were {result.WarningCount} warnings:\n\n {OASISResultHelper.BuildInnerMessageError(result.InnerMessages)}";
                else
                    result.Message = $"{STARNETHolonUIName} Successfully Published";

                OnPublishStatusChanged?.Invoke(this, new STARNETHolonPublishStatusEventArgs() { STARNETDNA = holon.STARNETDNA, Status = STARNETHolonPublishStatus.Published });
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling SaveSTARNETHolonAsync on {Enum.GetName(typeof(ProviderType), providerType)} provider. Reason: {saveSTARNETHolonResult.Message}");

            return result;
        }

        public OASISResult<T1> FininalizePublish(Guid avatarId, T1 holon, bool edit, ProviderType providerType)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            string errorMessage = "";

            //If its not the first version.
            if (holon.STARNETDNA.Version != "1.0.0" && !edit)
            {
                //If the ID has not been set then store the original id now.
                if (!holon.MetaData.ContainsKey(STARNETHolonIdName))
                    holon.MetaData[STARNETHolonIdName] = holon.Id;

                holon.MetaData["Version"] = holon.STARNETDNA.Version;
                holon.MetaData["VersionSequence"] = holon.STARNETDNA.VersionSequence;

                //Blank fields so it creates a new version.
                holon.Id = Guid.Empty;
                holon.ProviderUniqueStorageKey.Clear();
                holon.CreatedDate = DateTime.MinValue;
                holon.ModifiedDate = DateTime.MinValue;
                holon.CreatedByAvatarId = Guid.Empty;
                holon.ModifiedByAvatarId = Guid.Empty;
                holon.STARNETDNA.Downloads = 0;
                holon.STARNETDNA.Installs = 0;
            }

            OASISResult<T1> saveSTARNETHolonResult = Update(avatarId, holon, providerType: providerType);

            if (saveSTARNETHolonResult != null && !saveSTARNETHolonResult.IsError && saveSTARNETHolonResult.Result != null)
            {
                saveSTARNETHolonResult = UpdateNumberOfVersionCounts(avatarId, saveSTARNETHolonResult, errorMessage, providerType);
                result.IsSaved = true;
                result.Result = saveSTARNETHolonResult.Result; //TODO:Check if this is needed?

                CheckForVersionMismatches((T4)holon.STARNETDNA, ref result);

                if (result.IsWarning)
                    result.Message = $"{STARNETHolonUIName} successfully published but there were {result.WarningCount} warnings:\n\n {OASISResultHelper.BuildInnerMessageError(result.InnerMessages)}";
                else
                    result.Message = $"{STARNETHolonUIName} Successfully Published";

                OnPublishStatusChanged?.Invoke(this, new STARNETHolonPublishStatusEventArgs() { STARNETDNA = holon.STARNETDNA, Status = STARNETHolonPublishStatus.Published });
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling SaveSTARNETHolonAsync on {Enum.GetName(typeof(ProviderType), providerType)} provider. Reason: {saveSTARNETHolonResult.Message}");

            return result;
        }

        public virtual async Task<OASISResult<T1>> UnpublishAsync(Guid avatarId, T1 holon, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            string errorMessage = "Error occured in UnpublishAsync. Reason: ";

            holon.STARNETDNA.PublishedOn = DateTime.MinValue;
            holon.STARNETDNA.PublishedByAvatarId = Guid.Empty;
            holon.STARNETDNA.PublishedByAvatarUsername = "";
            //T.STARNETDNA.IsActive = false;
            holon.MetaData["Active"] = "0";

            OASISResult<T1> oappResult = await UpdateAsync(avatarId, holon, providerType: providerType);

            if (oappResult != null && oappResult.Result != null && !oappResult.IsError)
            {
                result.Result = oappResult.Result; //ConvertSTARNETHolonToSTARNETDNA(T);
                result.Message = $"{STARNETHolonUIName} Unpublished";
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured saving the {STARNETHolonUIName} with the SaveSTARNETHolonAsync method, reason: {oappResult.Message}");

            return result;
        }

        public OASISResult<T1> Unpublish(Guid avatarId, T1 holon, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            string errorMessage = "Error occured in Unpublish. Reason: ";

            holon.STARNETDNA.PublishedOn = DateTime.MinValue;
            holon.STARNETDNA.PublishedByAvatarId = Guid.Empty;
            holon.STARNETDNA.PublishedByAvatarUsername = "";
            //T.STARNETDNA.IsActive = false;
            holon.MetaData["Active"] = "0";

            OASISResult<T1> oappResult = Update(avatarId, holon, providerType: providerType);

            if (oappResult != null && oappResult.Result != null && !oappResult.IsError)
            {
                result.Result = oappResult.Result; //ConvertSTARNETHolonToSTARNETDNA(T);
                result.Message = $"{STARNETHolonUIName} Unpublished";
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured saving the {STARNETHolonUIName} with the Update method, reason: {oappResult.Message}");

            return result;
        }

        public virtual async Task<OASISResult<T1>> UnpublishAsync(Guid avatarId, Guid STARNETHolonId, int version, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            OASISResult<T1> loadResult = await LoadAsync(STARNETHolonId, avatarId, version, providerType: providerType);

            if (loadResult != null && loadResult.Result != null && !loadResult.IsError)
                result = await UnpublishAsync(avatarId, loadResult.Result, providerType);
            else
                OASISErrorHandling.HandleError(ref result, $"Error occured in UnpublishAsync loading the {STARNETHolonUIName} with the LoadAsync method, reason: {loadResult.Message}");

            return result;
        }

        public OASISResult<T1> Unpublish(Guid avatarId, Guid STARNETHolonId, int version, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            OASISResult<T1> loadResult = Load(STARNETHolonId, avatarId, version, providerType: providerType);

            if (loadResult != null && loadResult.Result != null && !loadResult.IsError)
                result = Unpublish(avatarId, loadResult.Result, providerType);
            else
                OASISErrorHandling.HandleError(ref result, $"Error occured in UnpublishUnpublish loading the {STARNETHolonUIName} with the Load method, reason: {loadResult.Message}");

            return result;
        }

        public virtual async Task<OASISResult<T1>> UnpublishAsync(Guid avatarId, T4 STARNETDNA, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            OASISResult<T1> oappResult = await LoadAsync(STARNETDNA.Id, avatarId, STARNETDNA.VersionSequence, providerType: providerType);
            string errorMessage = "Error occured in UnpublishSTARNETHolonAsync. Reason: ";

            if (oappResult != null && oappResult.Result != null && !oappResult.IsError)
                result = await UnpublishAsync(avatarId, oappResult.Result, providerType);
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured loading the {STARNETHolonUIName} with the LoadAsync method, reason: {oappResult.Message}");

            return result;
        }

        public OASISResult<T1> Unpublish(Guid avatarId, T4 STARNETDNA, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            OASISResult<T1> oappResult = Load(STARNETDNA.Id, avatarId, STARNETDNA.VersionSequence, providerType: providerType);
            string errorMessage = "Error occured in Unpublish. Reason: ";

            if (oappResult != null && oappResult.Result != null && !oappResult.IsError)
                result = Unpublish(avatarId, oappResult.Result, providerType);
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured loading the {STARNETHolonUIName} with the Load method, reason: {oappResult.Message}");

            return result;
        }

        public virtual async Task<OASISResult<T1>> RepublishAsync(Guid avatarId, T1 holon, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            string errorMessage = "Error occured in RepublishAsync. Reason: ";

            OASISResult<IAvatar> avatarResult = await AvatarManager.Instance.LoadAvatarAsync(avatarId, false, true, providerType);

            if (avatarResult != null && avatarResult.Result != null && !avatarResult.IsError)
            {
                holon.STARNETDNA.PublishedOn = DateTime.Now;
                holon.STARNETDNA.PublishedByAvatarId = avatarId;
                holon.STARNETDNA.PublishedByAvatarUsername = avatarResult.Result.Username;
                //T.STARNETDNA.IsActive = true;
                holon.MetaData["Active"] = "1";

                OASISResult<T1> oappResult = await UpdateAsync(avatarId, holon, providerType: providerType);

                if (oappResult != null && oappResult.Result != null && !oappResult.IsError)
                {
                    result.Result = oappResult.Result; //ConvertSTARNETHolonToSTARNETDNA(T);
                    result.Message = $"{STARNETHolonUIName} Republished";
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured saving the {STARNETHolonUIName} with the UpdateAsync method, reason: {oappResult.Message}");
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
                holon.STARNETDNA.PublishedOn = DateTime.Now;
                holon.STARNETDNA.PublishedByAvatarId = avatarId;
                holon.STARNETDNA.PublishedByAvatarUsername = avatarResult.Result.Username;
                //T.STARNETDNA.IsActive = true;
                holon.MetaData["Active"] = "1";

                OASISResult<T1> oappResult = Update(avatarId, holon, providerType: providerType);

                if (oappResult != null && oappResult.Result != null && !oappResult.IsError)
                {
                    result.Result = oappResult.Result; //ConvertSTARNETHolonToSTARNETDNA(T);
                    result.Message = $"{STARNETHolonUIName} Republished";
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured saving the {STARNETHolonUIName} with the Update method, reason: {oappResult.Message}");
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured loading the Avatar with the LoadAvatar method, reason: {avatarResult.Message}");

            return result;
        }

        public virtual async Task<OASISResult<T1>> RepublishAsync(Guid avatarId, T4 STARNETDNA, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            OASISResult<T1> oappResult = await LoadAsync(STARNETDNA.Id, avatarId, STARNETDNA.VersionSequence, providerType: providerType);
            string errorMessage = "Error occured in RepublishAsync. Reason: ";

            if (oappResult != null && oappResult.Result != null && !oappResult.IsError)
                result = await RepublishAsync(avatarId, oappResult.Result, providerType);
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured loading the {STARNETHolonUIName} with the LoadAsync method, reason: {oappResult.Message}");

            return result;
        }

        public OASISResult<T1> Republish(Guid avatarId, T4 STARNETDNA, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            OASISResult<T1> oappResult = Load(STARNETDNA.Id, avatarId, STARNETDNA.VersionSequence, providerType: providerType);
            string errorMessage = "Error occured in Republish. Reason: ";

            if (oappResult != null && oappResult.Result != null && !oappResult.IsError)
                result = Republish(avatarId, oappResult.Result, providerType);
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured loading the {STARNETHolonUIName} with the LoadSTARNETHolon method, reason: {oappResult.Message}");

            return result;
        }

        public virtual async Task<OASISResult<T1>> RepublishAsync(Guid avatarId, Guid STARNETHolonId, int version, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            OASISResult<T1> loadResult = await LoadAsync(STARNETHolonId, avatarId, version, providerType: providerType);

            if (loadResult != null && loadResult.Result != null && !loadResult.IsError)
                result = await RepublishAsync(avatarId, loadResult.Result, providerType);
            else
                OASISErrorHandling.HandleError(ref result, $"Error occured in RepublishAsync loading the {STARNETHolonUIName} with the LoadAsync method, reason: {loadResult.Message}");

            return result;
        }

        public OASISResult<T1> Republish(Guid avatarId, Guid STARNETHolonId, int version, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            OASISResult<T1> loadResult = Load(STARNETHolonId, avatarId, version, providerType: providerType);

            if (loadResult != null && loadResult.Result != null && !loadResult.IsError)
                result = Republish(avatarId, loadResult.Result, providerType);
            else
                OASISErrorHandling.HandleError(ref result, $"Error occured in Republish loading the {STARNETHolonUIName} with the Load method, reason: {loadResult.Message}");

            return result;
        }

        public virtual async Task<OASISResult<T1>> DeactivateAsync(Guid avatarId, T1 holon, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            string errorMessage = "Error occured in DeactivateAsync. Reason: ";

            //T.STARNETDNA.IsActive = false;
            holon.MetaData["Active"] = "0";

            OASISResult<T1> oappResult = await UpdateAsync(avatarId, holon, providerType: providerType);

            if (oappResult != null && oappResult.Result != null && !oappResult.IsError)
            {
                result.Result = oappResult.Result; //ConvertSTARNETHolonToSTARNETDNA(T);
                result.Message = $"{STARNETHolonUIName} Deactivated";
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured saving the {STARNETHolonUIName} with the SaveSTARNETHolonAsync method, reason: {oappResult.Message}");

            return result;
        }

        public OASISResult<T1> Deactivate(Guid avatarId, T1 holon, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            string errorMessage = "Error occured in Deactivate. Reason: ";

            //T.STARNETDNA.IsActive = false;
            holon.MetaData["Active"] = "0";

            OASISResult<T1> oappResult = Update(avatarId, holon, providerType: providerType);

            if (oappResult != null && oappResult.Result != null && !oappResult.IsError)
            {
                result.Result = oappResult.Result; //ConvertSTARNETHolonToSTARNETDNA(T);
                result.Message = $"{STARNETHolonUIName} Deactivated";
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured saving the {STARNETHolonUIName} with the SaveSTARNETHolon method, reason: {oappResult.Message}");

            return result;
        }

        public virtual async Task<OASISResult<T1>> DeactivateAsync(Guid avatarId, Guid STARNETHolonId, int version, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            OASISResult<T1> loadResult = await LoadAsync(STARNETHolonId, avatarId, version, providerType: providerType);

            if (loadResult != null && loadResult.Result != null && !loadResult.IsError)
                result = await DeactivateAsync(avatarId, loadResult.Result, providerType);
            else
                OASISErrorHandling.HandleError(ref result, $"Error occured in DeactivateAsync loading the T with the LoadAsync method, reason: {loadResult.Message}");

            return result;
        }

        public OASISResult<T1> Deactivate(Guid avatarId, Guid STARNETHolonId, int version, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            OASISResult<T1> loadResult = Load(STARNETHolonId, avatarId, version, providerType: providerType);

            if (loadResult != null && loadResult.Result != null && !loadResult.IsError)
                result = Deactivate(avatarId, loadResult.Result, providerType);
            else
                OASISErrorHandling.HandleError(ref result, $"Error occured in Deactivate loading the T with the LoadSTARNETHolon method, reason: {loadResult.Message}");

            return result;
        }

        public virtual async Task<OASISResult<T1>> DeactivateAsync(Guid avatarId, T4 STARNETDNA, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            OASISResult<T1> oappResult = await LoadAsync(STARNETDNA.Id, avatarId, STARNETDNA.VersionSequence, providerType: providerType);
            string errorMessage = "Error occured in DeactivateAsync. Reason: ";

            if (oappResult != null && oappResult.Result != null && !oappResult.IsError)
                result = await DeactivateAsync(avatarId, oappResult.Result, providerType);
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured loading the {STARNETHolonUIName} with the LoadSTARNETHolonAsync method, reason: {oappResult.Message}");

            return result;
        }

        public OASISResult<T1> Deactivate(Guid avatarId, T4 STARNETDNA, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            OASISResult<T1> oappResult = Load(STARNETDNA.Id, avatarId, STARNETDNA.VersionSequence, providerType: providerType);
            string errorMessage = "Error occured in Deactivate. Reason: ";

            if (oappResult != null && oappResult.Result != null && !oappResult.IsError)
                result = Deactivate(avatarId, oappResult.Result, providerType);
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured loading the {STARNETHolonUIName} with the LoadSTARNETHolon method, reason: {oappResult.Message}");

            return result;
        }

        public virtual async Task<OASISResult<T1>> ActivateAsync(Guid avatarId, T1 holon, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            string errorMessage = "Error occured in ActivateAsync. Reason: ";

            OASISResult<IAvatar> avatarResult = await AvatarManager.Instance.LoadAvatarAsync(avatarId, false, true, providerType);

            if (avatarResult != null && avatarResult.Result != null && !avatarResult.IsError)
            {
                //T.STARNETDNA.IsActive = true;
                holon.MetaData["Active"] = "1";

                OASISResult<T1> oappResult = await UpdateAsync(avatarId, holon, providerType: providerType);

                if (oappResult != null && oappResult.Result != null && !oappResult.IsError)
                {
                    result.Result = oappResult.Result; //ConvertSTARNETHolonToSTARNETDNA(T);
                    result.Message = $"{STARNETHolonUIName} Activated";
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured saving the {STARNETHolonUIName} with the SaveSTARNETHolonAsync method, reason: {oappResult.Message}");
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
                //T.STARNETDNA.IsActive = true;
                holon.MetaData["Active"] = "1";

                OASISResult<T1> oappResult = Update(avatarId, holon, providerType: providerType);

                if (oappResult != null && oappResult.Result != null && !oappResult.IsError)
                {
                    result.Result = oappResult.Result; //ConvertSTARNETHolonToSTARNETDNA(T);
                    result.Message = $"{STARNETHolonUIName} Activated";
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured saving the {STARNETHolonUIName} with the Update method, reason: {oappResult.Message}");
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured loading the Avatar with the LoadAvatar method, reason: {avatarResult.Message}");

            return result;
        }

        public virtual async Task<OASISResult<T1>> ActivateAsync(Guid avatarId, T4 STARNETDNA, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            OASISResult<T1> oappResult = await LoadAsync(avatarId, STARNETDNA.Id, STARNETDNA.VersionSequence, providerType: providerType);
            string errorMessage = "Error occured in ActivateAsync. Reason: ";

            if (oappResult != null && oappResult.Result != null && !oappResult.IsError)
                result = await ActivateAsync(avatarId, oappResult.Result, providerType);
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured loading the {STARNETHolonUIName} with the LoadSTARNETHolonAsync method, reason: {oappResult.Message}");

            return result;
        }

        public OASISResult<T1> Activate(Guid avatarId, T4 STARNETDNA, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            OASISResult<T1> oappResult = Load(avatarId, STARNETDNA.Id, STARNETDNA.VersionSequence, providerType: providerType);
            string errorMessage = "Error occured in Activate. Reason: ";

            if (oappResult != null && oappResult.Result != null && !oappResult.IsError)
                result = Activate(avatarId, oappResult.Result, providerType);
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured loading the {STARNETHolonUIName} with the Load method, reason: {oappResult.Message}");

            return result;
        }

        public virtual async Task<OASISResult<T1>> ActivateAsync(Guid avatarId, Guid id, int version, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            OASISResult<T1> loadResult = await LoadAsync(avatarId, id, version, providerType: providerType);

            if (loadResult != null && loadResult.Result != null && !loadResult.IsError)
                result = await ActivateAsync(avatarId, loadResult.Result, providerType);
            else
                OASISErrorHandling.HandleError(ref result, $"Error occured in ActivateAsync loading the T with the LoadAsync method, reason: {loadResult.Message}");

            return result;
        }

        public OASISResult<T1> Activate(Guid avatarId, Guid id, int version, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            OASISResult<T1> loadResult = Load(id, avatarId, version, providerType: providerType);

            if (loadResult != null && loadResult.Result != null && !loadResult.IsError)
                result = Activate(avatarId, loadResult.Result, providerType);
            else
                OASISErrorHandling.HandleError(ref result, $"Error occured in Activate loading the {STARNETHolonUIName} with the Load method, reason: {loadResult.Message}");

            return result;
        }

        //public virtual async Task<OASISResult<T2>> DownloadAsync(Guid avatarId, string STARNETHolonName, string fullDownloadPath, int version = 0, bool reInstall = false, ProviderType providerType = ProviderType.Default)
        //{
        //    OASISResult<T2> result = new OASISResult<T2>();
        //    OASISResult<T1> STARNETHolonResult = await Data.LoadHolonByMetaDataAsync<T1>(this.STARNETHolonNameName, STARNETHolonName, version: version, providerType: providerType);

        //    if (STARNETHolonResult != null && !STARNETHolonResult.IsError && STARNETHolonResult.Result != null)
        //        result = await DownloadAsync(avatarId, STARNETHolonResult.Result, fullDownloadPath, reInstall, providerType);
        //    else
        //        OASISErrorHandling.HandleError(ref result, $"Error occured in STARNETManagerBase.DownloadAsync loading the {STARNETHolonUIName} with the LoadHolonByMetaDataAsync method, reason: {OASISErrorHandling.ProcessMessage(result, $"No result found for name {STARNETHolonName}")}");

        //    return result;
        //}

        //public virtual OASISResult<T2> Download(Guid avatarId, string STARNETHolonName, string fullDownloadPath, int version = 0, bool reInstall = false, ProviderType providerType = ProviderType.Default)
        //{
        //    OASISResult<T2> result = new OASISResult<T2>();
        //    OASISResult<T1> STARNETHolonResult = Data.LoadHolonByMetaData<T1>(this.STARNETHolonNameName, STARNETHolonName, version: version, providerType: providerType);

        //    if (STARNETHolonResult != null && !STARNETHolonResult.IsError && STARNETHolonResult.Result != null)
        //        result = Download(avatarId, STARNETHolonResult.Result, fullDownloadPath, reInstall, providerType);
        //    else
        //        OASISErrorHandling.HandleError(ref result, $"Error occured in STARNETManagerBase.Download loading the {STARNETHolonUIName} with the LoadHolonByMetaData method, reason: {OASISErrorHandling.ProcessMessage(result, $"No result found for name {STARNETHolonName}")}");

        //    return result;
        //}

        //public virtual async Task<OASISResult<T2>> DownloadAsync(Guid avatarId, Guid STARNETHolonId, string fullDownloadPath, int version = 0, bool reInstall = false, ProviderType providerType = ProviderType.Default)
        //{
        //    OASISResult<T2> result = new OASISResult<T2>();
        //    OASISResult<T1> STARNETHolonResult = await LoadAsync(STARNETHolonId, avatarId, version, providerType);

        //    if (STARNETHolonResult != null && !STARNETHolonResult.IsError && STARNETHolonResult.Result != null)
        //        result = await DownloadAsync(avatarId, STARNETHolonResult.Result, fullDownloadPath, reInstall, providerType);
        //    else
        //        OASISErrorHandling.HandleError(ref result, $"Error occured in STARNETManagerBase.DownloadAsync loading the {STARNETHolonUIName} with the LoadAsync method, reason: {OASISErrorHandling.ProcessMessage(result, $"No result found for id {STARNETHolonId}")}");

        //    return result;
        //}

        //public virtual OASISResult<T2> Download(Guid avatarId, Guid STARNETHolonId, string fullDownloadPath, int version = 0, bool reInstall = false, ProviderType providerType = ProviderType.Default)
        //{
        //    OASISResult<T2> result = new OASISResult<T2>();
        //    OASISResult<T1> STARNETHolonResult = Load(STARNETHolonId, avatarId, version, providerType);

        //    if (STARNETHolonResult != null && !STARNETHolonResult.IsError && STARNETHolonResult.Result != null)
        //        result = Download(avatarId, STARNETHolonResult.Result, fullDownloadPath, reInstall, providerType);
        //    else
        //        OASISErrorHandling.HandleError(ref result, $"Error occured in STARNETManagerBase.Download loading the {STARNETHolonUIName} with the Load method, reason: {OASISErrorHandling.ProcessMessage(result, $"No result found for id {STARNETHolonId}")}");

        //    return result;
        //}

        public virtual async Task<OASISResult<T2>> DownloadAsync(Guid avatarId, Guid STARNETHolonId, int version, string fullDownloadPath = "", bool reInstall = false, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T2> result = new OASISResult<T2>();
            OASISResult<T1> STARNETHolonResult = await Data.LoadHolonByMetaDataAsync<T1>(new Dictionary<string, string>()
            {
                { STARNETHolonIdName, STARNETHolonId.ToString() },
                { "VersionSequence", version.ToString() } ,
                { "Active", "1" } //TODO: Not sure if we need this?
            }, metaKeyValuePairMatchMode: MetaKeyValuePairMatchMode.All, providerType: providerType);

            if (STARNETHolonResult != null && !STARNETHolonResult.IsError && STARNETHolonResult.Result != null)
                result = await DownloadAsync(avatarId, STARNETHolonResult.Result, fullDownloadPath, reInstall, providerType);
            else
            {
                OASISErrorHandling.HandleError(ref result, $"Error occured in STARNETManagerBase.DownloadAsync loading the {STARNETHolonUIName} with the LoadAsync method, reason: {OASISErrorHandling.ProcessMessage(result, $"No result found for id {STARNETHolonId.ToString()} and version {version}.")}");
                OnInstallStatusChanged?.Invoke(this, new STARNETHolonInstallStatusEventArgs() { Status = STARNETHolonInstallStatus.Error, ErrorMessage = result.Message });
            }

            return result;
        }

        //copied.
        public OASISResult<T2> Download(Guid avatarId, Guid STARNETHolonId, int version, string fullDownloadPath = "", bool reInstall = false, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T2> result = new OASISResult<T2>();
            OASISResult<T1> STARNETHolonResult = Data.LoadHolonByMetaData<T1>(new Dictionary<string, string>()
            {
                { STARNETHolonIdName, STARNETHolonId.ToString() },
                { "VersionSequence", version.ToString() } ,
                { "Active", "1" } //TODO: Not sure if we need this?
            }, metaKeyValuePairMatchMode: MetaKeyValuePairMatchMode.All, providerType: providerType);


            if (STARNETHolonResult != null && !STARNETHolonResult.IsError && STARNETHolonResult.Result != null)
                result = Download(avatarId, STARNETHolonResult.Result, fullDownloadPath, reInstall, providerType);
            else
            {
                OASISErrorHandling.HandleError(ref result, $"Error occured in STARNETManagerBase.Download loading the {STARNETHolonUIName} with the LoadAsync method, reason: {OASISErrorHandling.ProcessMessage(result, $"No result found for id {STARNETHolonId.ToString()} and version {version}.")}");
                OnInstallStatusChanged?.Invoke(this, new STARNETHolonInstallStatusEventArgs() { Status = STARNETHolonInstallStatus.Error, ErrorMessage = result.Message });
            }

            return result;
        }

        public virtual async Task<OASISResult<T2>> DownloadAsync(Guid avatarId, Guid STARNETHolonId, string version, string fullDownloadPath = "", bool reInstall = false, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T2> result = new OASISResult<T2>();
            OASISResult<T1> STARNETHolonResult = await Data.LoadHolonByMetaDataAsync<T1>(new Dictionary<string, string>()
            {
                { STARNETHolonIdName, STARNETHolonId.ToString() },
                { "Version", version } ,
                { "Active", "1" } //TODO: Not sure if we need this?
            }, metaKeyValuePairMatchMode: MetaKeyValuePairMatchMode.All, providerType: providerType);

            if (STARNETHolonResult != null && !STARNETHolonResult.IsError && STARNETHolonResult.Result != null)
                result = await DownloadAsync(avatarId, STARNETHolonResult.Result, fullDownloadPath, reInstall, providerType);
            else
            {
                OASISErrorHandling.HandleError(ref result, $"Error occured in STARNETManagerBase.DownloadAsync loading the {STARNETHolonUIName} with the LoadHolonByMetaDataAsync method, reason: {OASISErrorHandling.ProcessMessage(result, $"No result found for id {STARNETHolonId.ToString()} and version {version}")}");
                OnInstallStatusChanged?.Invoke(this, new STARNETHolonInstallStatusEventArgs() { Status = STARNETHolonInstallStatus.Error, ErrorMessage = result.Message });
            }

            return result;
        }

        public virtual OASISResult<T2> Download(Guid avatarId, Guid STARNETHolonId, string version, string fullDownloadPath = "", bool reInstall = false, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T2> result = new OASISResult<T2>();
            OASISResult<T1> STARNETHolonResult = Data.LoadHolonByMetaData<T1>(new Dictionary<string, string>()
            {
                { STARNETHolonIdName, STARNETHolonId.ToString() },
                { "Version", version } ,
                { "Active", "1" } //TODO: Not sure if we need this?
            }, metaKeyValuePairMatchMode: MetaKeyValuePairMatchMode.All, providerType: providerType);

            if (STARNETHolonResult != null && !STARNETHolonResult.IsError && STARNETHolonResult.Result != null)
                result = Download(avatarId, STARNETHolonResult.Result, fullDownloadPath, reInstall, providerType);
            else
            {
                OASISErrorHandling.HandleError(ref result, $"Error occured in STARNETManagerBase.Download loading the {STARNETHolonUIName} with the LoadHolonByMetaData method, reason: {OASISErrorHandling.ProcessMessage(result, $"No result found for id {STARNETHolonId.ToString()} and version {version}")}");
                OnInstallStatusChanged?.Invoke(this, new STARNETHolonInstallStatusEventArgs() { Status = STARNETHolonInstallStatus.Error, ErrorMessage = result.Message });
            }

            return result;
        }

        public virtual async Task<OASISResult<T2>> DownloadAsync(Guid avatarId, string STARNETHolonName, int version, string fullDownloadPath = "", bool reInstall = false, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T2> result = new OASISResult<T2>();
            OASISResult<T1> STARNETHolonResult = await Data.LoadHolonByMetaDataAsync<T1>(new Dictionary<string, string>()
            {
                { STARNETHolonNameName, STARNETHolonNameName },
                { "VersionSequence", version.ToString() } ,
                { "Active", "1" } //TODO: Not sure if we need this?
            }, metaKeyValuePairMatchMode: MetaKeyValuePairMatchMode.All, providerType: providerType);

            if (STARNETHolonResult != null && !STARNETHolonResult.IsError && STARNETHolonResult.Result != null)
                result = await DownloadAsync(avatarId, STARNETHolonResult.Result, fullDownloadPath, reInstall, providerType);
            else
            {
                OASISErrorHandling.HandleError(ref result, $"Error occured in STARNETManagerBase.DownloadAsync loading the {STARNETHolonUIName} with the LoadHolonByMetaDataAsync method, reason: {OASISErrorHandling.ProcessMessage(result, $"No result found for name {STARNETHolonName} and version {version}")}");
                OnInstallStatusChanged?.Invoke(this, new STARNETHolonInstallStatusEventArgs() { Status = STARNETHolonInstallStatus.Error, ErrorMessage = result.Message });
            }

            return result;
        }

        //copied.
        public OASISResult<T2> Download(Guid avatarId, string STARNETHolonName, int version, string fullDownloadPath = "", bool reInstall = false, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T2> result = new OASISResult<T2>();
            OASISResult<T1> STARNETHolonResult = Data.LoadHolonByMetaData<T1>(new Dictionary<string, string>()
            {
                { STARNETHolonNameName, STARNETHolonNameName },
                { "VersionSequence", version.ToString() } ,
                { "Active", "1" } //TODO: Not sure if we need this?
            }, metaKeyValuePairMatchMode: MetaKeyValuePairMatchMode.All, providerType: providerType);

            if (STARNETHolonResult != null && !STARNETHolonResult.IsError && STARNETHolonResult.Result != null)
                result = Download(avatarId, STARNETHolonResult.Result, fullDownloadPath, reInstall, providerType);
            else
            {
                OASISErrorHandling.HandleError(ref result, $"Error occured in STARNETManagerBase.Download loading the {STARNETHolonUIName} with the LoadHolonByMetaData method, reason: {OASISErrorHandling.ProcessMessage(result, $"No result found for name {STARNETHolonName} and version {version}")}");
                OnInstallStatusChanged?.Invoke(this, new STARNETHolonInstallStatusEventArgs() { Status = STARNETHolonInstallStatus.Error, ErrorMessage = result.Message });
            }

            return result;
        }

        public virtual async Task<OASISResult<T2>> DownloadAsync(Guid avatarId, string STARNETHolonName, string version, string fullDownloadPath = "", bool reInstall = false, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T2> result = new OASISResult<T2>();
            OASISResult<T1> STARNETHolonResult = await Data.LoadHolonByMetaDataAsync<T1>(new Dictionary<string, string>()
            {
                { STARNETHolonNameName, STARNETHolonNameName },
                { "Version", version } ,
                { "Active", "1" } //TODO: Not sure if we need this?
            }, metaKeyValuePairMatchMode: MetaKeyValuePairMatchMode.All, providerType: providerType);

            if (STARNETHolonResult != null && !STARNETHolonResult.IsError && STARNETHolonResult.Result != null)
                result = await DownloadAsync(avatarId, STARNETHolonResult.Result, fullDownloadPath, reInstall, providerType);
            else
            {
                OASISErrorHandling.HandleError(ref result, $"Error occured in STARNETManagerBase.DownloadAsync loading the {STARNETHolonUIName} with the LoadHolonByMetaDataAsync method, reason: {OASISErrorHandling.ProcessMessage(result, $"No result found for name {STARNETHolonName} and version {version}")}");
                OnInstallStatusChanged?.Invoke(this, new STARNETHolonInstallStatusEventArgs() { Status = STARNETHolonInstallStatus.Error, ErrorMessage = result.Message });
            }

            return result;
        }

        public virtual OASISResult<T2> Download(Guid avatarId, string STARNETHolonName, string version, string fullDownloadPath = "", bool reInstall = false, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T2> result = new OASISResult<T2>();
            OASISResult<T1> STARNETHolonResult = Data.LoadHolonByMetaData<T1>(new Dictionary<string, string>()
            {
                { STARNETHolonNameName, STARNETHolonNameName },
                { "Version", version } ,
                { "Active", "1" } //TODO: Not sure if we need this?
            }, metaKeyValuePairMatchMode: MetaKeyValuePairMatchMode.All, providerType: providerType);

            if (STARNETHolonResult != null && !STARNETHolonResult.IsError && STARNETHolonResult.Result != null)
                result = Download(avatarId, STARNETHolonResult.Result, fullDownloadPath, reInstall, providerType);
            else
            {
                OASISErrorHandling.HandleError(ref result, $"Error occured in STARNETManagerBase.Download loading the {STARNETHolonUIName} with the LoadHolonByMetaData method, reason: {OASISErrorHandling.ProcessMessage(result, $"No result found for name {STARNETHolonName} and version {version}")}");
                OnInstallStatusChanged?.Invoke(this, new STARNETHolonInstallStatusEventArgs() { Status = STARNETHolonInstallStatus.Error, ErrorMessage = result.Message });
            }

            return result;
        }

        public virtual async Task<OASISResult<T2>> DownloadAsync(Guid avatarId, T1 holon, string fullDownloadPath, bool reInstall = false, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T2> result = new OASISResult<T2>();
            string errorMessage = "Error occured in STARNETManagerBase.DownloadAsync. Reason: ";
            T2 downloadedSTARNETHolon = default;

            try
            {
                if (!fullDownloadPath.Contains(string.Concat(".", STARNETHolonFileExtention)))
                    fullDownloadPath = Path.Combine(fullDownloadPath, string.Concat(holon.Name, "_v", holon.STARNETDNA.Version, ".", STARNETHolonFileExtention));

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
                            _fileLength = holon.STARNETDNA.FileSize;

                        _progress = 0;

                        string publishedSTARNETHolonFileName = string.Concat(holon.Name, "_v", holon.STARNETDNA.Version, ".", STARNETHolonFileExtention);
                        OnInstallStatusChanged?.Invoke(this, new STARNETHolonInstallStatusEventArgs() { STARNETDNA = holon.STARNETDNA, Status = STARNETHolonInstallStatus.Downloading });
                        await storage.DownloadObjectAsync(STARNETHolonGoogleBucket, publishedSTARNETHolonFileName, fileStream, downloadObjectOptions, progress: progressReporter);

                        _progress = 100;
                        OnDownloadStatusChanged?.Invoke(this, new STARNETHolonDownloadProgressEventArgs() { Progress = _progress, Status = STARNETHolonDownloadStatus.Downloading });
                        CLIEngine.DisposeProgressBar(false);
                        Console.WriteLine("");
                        fileStream.Close();
                    }

                    OASISResult<IAvatar> avatarResult = await AvatarManager.Instance.LoadAvatarAsync(avatarId, false, true, providerType);

                    if (avatarResult != null && !avatarResult.IsError && avatarResult.Result != null)
                    {
                        if (!reInstall)
                        {
                            holon.STARNETDNA.Downloads++;

                            downloadedSTARNETHolon = new T2()
                            {
                                //ParentHolonId = holon.Id, //TODO: Later want to fix this so the parent holon id is the original source holon. We need to fix the listInstalled method to load from this id instead.
                                ParentSTARNETHolonId = holon.STARNETDNA.Id,
                                Name = string.Concat(holon.STARNETDNA.Name, " Downloaded Holon"),
                                Description = string.Concat(holon.STARNETDNA.Description, " Downloaded Holon"),
                                STARNETDNA = holon.STARNETDNA,
                                DownloadedBy = avatarId,
                                DownloadedByAvatarUsername = avatarResult.Result.Username,
                                DownloadedOn = DateTime.Now,
                                DownloadedPath = fullDownloadPath,
                                MetaData = holon.MetaData

                            };

                            await UpdateDownloadCountsAsync(avatarId, downloadedSTARNETHolon, (T4)holon.STARNETDNA, result, errorMessage, providerType);

                            downloadedSTARNETHolon.MetaData[STARNETHolonIdName] = holon.STARNETDNA.Id.ToString();
                            //downloadedSTARNETHolon.MetaData[STARNETDNAJSONName] = JsonSerializer.Serialize(downloadedSTARNETHolon.STARNETDNA);
                            downloadedSTARNETHolon.MetaData[STARNETDNAJSONName] = JsonConvert.SerializeObject(downloadedSTARNETHolon.STARNETDNA);
                            OASISResult<T2> saveResult = await downloadedSTARNETHolon.SaveAsync<T2>();

                            if (!(saveResult != null && saveResult.Result != null && !saveResult.IsError))
                                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling UpdateAsync method on downloadedSTARNETHolon. Reason: {saveResult.Message}");
                        }
                        else
                        {
                            OASISResult<IEnumerable<T2>> downloadedSTARNETHolonResult = await Data.LoadHolonsByMetaDataAsync<T2>(STARNETHolonIdName, holon.STARNETDNA.Id.ToString(), HolonType.All, true, true, 0, true, false, 0, HolonType.All, 0, providerType);

                            if (downloadedSTARNETHolonResult != null && !downloadedSTARNETHolonResult.IsError && downloadedSTARNETHolonResult.Result != null)
                            {
                                downloadedSTARNETHolon = downloadedSTARNETHolonResult.Result.FirstOrDefault();
                                downloadedSTARNETHolon.DownloadedOn = DateTime.Now;
                                downloadedSTARNETHolon.DownloadedBy = avatarId;
                                downloadedSTARNETHolon.DownloadedByAvatarUsername = avatarResult.Result.Username;
                                downloadedSTARNETHolon.DownloadedPath = fullDownloadPath;
                                downloadedSTARNETHolon.MetaData = holon.MetaData;

                                OASISResult<T2> saveResult = await downloadedSTARNETHolon.SaveAsync<T2>();

                                if (!(saveResult != null && saveResult.Result != null && !saveResult.IsError))
                                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling UpdateAsync method on downloadedSTARNETHolon. Reason: {saveResult.Message}");
                            }
                            else
                                OASISErrorHandling.HandleWarning(ref result, $"The {STARNETHolonUIName} was downloaded but the DownloadedSTARNETHolon could not be found. Reason: {downloadedSTARNETHolonResult.Message}");
                        }
                    }
                    else
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadAvatarAsync method. Reason: {avatarResult.Message}");


                    if (!result.IsError)
                    {
                        result.Result = downloadedSTARNETHolon;
                        OASISResult<T1> oappSaveResult = await UpdateAsync(avatarId, holon, providerType: providerType);

                        if (oappSaveResult != null && !oappSaveResult.IsError && oappSaveResult.Result != null)
                        {
                            if (result.InnerMessages.Count > 0)
                                result.Message = $"{STARNETHolonUIName} successfully downloaded but there were {result.WarningCount} warnings:\n\n {OASISResultHelper.BuildInnerMessageError(result.InnerMessages)}";
                            else
                                result.Message = $"{STARNETHolonUIName} Successfully Downloaded";
                        }
                        else
                            OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling SaveSTARNETHolonAsync method. Reason: {oappSaveResult.Message}");
                    }
                }
                catch (Exception e)
                {
                    CLIEngine.DisposeProgressBar(false);
                    Console.WriteLine("");
                    OASISErrorHandling.HandleError(ref result, $"An error occured downloading the {STARNETHolonUIName} from cloud storage. Reason: {e}");
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
            //    OnSTARNETHolonDownloadStatusChanged?.Invoke(this, new STARNETHolonDownloadProgressEventArgs { STARNETDNA = T.STARNETDNA, Status = Enums.STARNETHolonDownloadStatus.Error, ErrorMessage = result.Message });

            return result;
        }

        public OASISResult<T2> Download(Guid avatarId, T1 holon, string fullDownloadPath, bool reInstall = false, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T2> result = new OASISResult<T2>();
            string errorMessage = "Error occured in STARNETManagerBase.Download. Reason: ";
            T2 downloadedSTARNETHolon = default;

            try
            {
                if (!fullDownloadPath.Contains(string.Concat(".", STARNETHolonFileExtention)))
                    fullDownloadPath = Path.Combine(fullDownloadPath, string.Concat(holon.Name, "_v", holon.STARNETDNA.Version, ".", STARNETHolonFileExtention));

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
                            _fileLength = holon.STARNETDNA.FileSize;

                        _progress = 0;

                        string publishedSTARNETHolonFileName = string.Concat(holon.Name, "_v", holon.STARNETDNA.Version, ".", STARNETHolonFileExtention);
                        OnInstallStatusChanged?.Invoke(this, new STARNETHolonInstallStatusEventArgs() { STARNETDNA = holon.STARNETDNA, Status = STARNETHolonInstallStatus.Downloading });
                        storage.DownloadObject(STARNETHolonGoogleBucket, publishedSTARNETHolonFileName, fileStream, downloadObjectOptions, progress: progressReporter);

                        _progress = 100;
                        OnDownloadStatusChanged?.Invoke(this, new STARNETHolonDownloadProgressEventArgs() { Progress = _progress, Status = STARNETHolonDownloadStatus.Downloading });
                        CLIEngine.DisposeProgressBar(false);
                        Console.WriteLine("");
                        fileStream.Close();
                    }

                    OASISResult<IAvatar> avatarResult = AvatarManager.Instance.LoadAvatar(avatarId, false, true, providerType);

                    if (avatarResult != null && !avatarResult.IsError && avatarResult.Result != null)
                    {
                        if (!reInstall)
                        {
                            holon.STARNETDNA.Downloads++;

                            downloadedSTARNETHolon = new T2()
                            {
                                //ParentHolonId = holon.Id,
                                ParentSTARNETHolonId = holon.STARNETDNA.Id,
                                Name = string.Concat(holon.STARNETDNA.Name, " Downloaded Holon"),
                                Description = string.Concat(holon.STARNETDNA.Description, " Downloaded Holon"),
                                STARNETDNA = holon.STARNETDNA,
                                DownloadedBy = avatarId,
                                DownloadedByAvatarUsername = avatarResult.Result.Username,
                                DownloadedOn = DateTime.Now,
                                DownloadedPath = fullDownloadPath,
                                MetaData = holon.MetaData
                            };

                            UpdateDownloadCounts(avatarId, downloadedSTARNETHolon, (T4)holon.STARNETDNA, result, errorMessage, providerType);

                            downloadedSTARNETHolon.MetaData[STARNETHolonIdName] = holon.STARNETDNA.Id.ToString();
                            //downloadedSTARNETHolon.MetaData[STARNETDNAJSONName] = JsonSerializer.Serialize(downloadedSTARNETHolon.STARNETDNA);
                            downloadedSTARNETHolon.MetaData[STARNETDNAJSONName] = JsonConvert.SerializeObject(downloadedSTARNETHolon.STARNETDNA);
                            OASISResult<T2> saveResult = downloadedSTARNETHolon.Save<T2>();

                            if (!(saveResult != null && saveResult.Result != null && !saveResult.IsError))
                                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling UpdateAsync method on downloadedSTARNETHolon. Reason: {saveResult.Message}");
                        }
                        else
                        {
                            OASISResult<IEnumerable<T2>> downloadedSTARNETHolonResult = Data.LoadHolonsByMetaData<T2>(STARNETHolonIdName, holon.STARNETDNA.Id.ToString(), HolonType.All, true, true, 0, true, false, 0, HolonType.All, 0, providerType);

                            if (downloadedSTARNETHolonResult != null && !downloadedSTARNETHolonResult.IsError && downloadedSTARNETHolonResult.Result != null)
                            {
                                downloadedSTARNETHolon = downloadedSTARNETHolonResult.Result.FirstOrDefault();
                                downloadedSTARNETHolon.DownloadedOn = DateTime.Now;
                                downloadedSTARNETHolon.DownloadedBy = avatarId;
                                downloadedSTARNETHolon.DownloadedByAvatarUsername = avatarResult.Result.Username;
                                downloadedSTARNETHolon.DownloadedPath = fullDownloadPath;
                                downloadedSTARNETHolon.MetaData = holon.MetaData;

                                OASISResult<T2> saveResult = downloadedSTARNETHolon.Save<T2>();

                                if (!(saveResult != null && saveResult.Result != null && !saveResult.IsError))
                                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling UpdateAsync method on downloadedSTARNETHolon. Reason: {saveResult.Message}");
                            }
                            else
                                OASISErrorHandling.HandleWarning(ref result, $"The {STARNETHolonUIName} was downloaded but the DownloadedSTARNETHolon could not be found. Reason: {downloadedSTARNETHolonResult.Message}");
                        }
                    }
                    else
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadAvatarAsync method. Reason: {avatarResult.Message}");


                    if (!result.IsError)
                    {
                        result.Result = downloadedSTARNETHolon;
                        OASISResult<T1> oappSaveResult = Update(avatarId, holon, providerType: providerType);

                        if (oappSaveResult != null && !oappSaveResult.IsError && oappSaveResult.Result != null)
                        {
                            if (result.InnerMessages.Count > 0)
                                result.Message = $"{STARNETHolonUIName} successfully downloaded but there were {result.WarningCount} warnings:\n\n {OASISResultHelper.BuildInnerMessageError(result.InnerMessages)}";
                            else
                                result.Message = $"{STARNETHolonUIName} Successfully Downloaded";
                        }
                        else
                            OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling SaveSTARNETHolonAsync method. Reason: {oappSaveResult.Message}");
                    }
                }
                catch (Exception e)
                {
                    CLIEngine.DisposeProgressBar(false);
                    Console.WriteLine("");
                    OASISErrorHandling.HandleError(ref result, $"An error occured downloading the {STARNETHolonUIName} from cloud storage. Reason: {e}");
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
            //    OnSTARNETHolonDownloadStatusChanged?.Invoke(this, new STARNETHolonDownloadProgressEventArgs { STARNETDNA = T.STARNETDNA, Status = Enums.STARNETHolonDownloadStatus.Error, ErrorMessage = result.Message });

            return result;
        }

        public virtual async Task<OASISResult<T3>> DownloadAndInstallAsync(Guid avatarId, T1 holon, string fullInstallPath, string fullDownloadPath = "", bool createSTARNETHolonDirectory = true, bool reInstall = false, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T3> result = new OASISResult<T3>();
            string errorMessage = "Error occured in STARNETManagerBase.DownloadAndInstallAsync. Reason: ";
            bool isFullDownloadPathTemp = false;

            try
            {
                if (string.IsNullOrEmpty(fullDownloadPath))
                {
                    string tempPath = Path.GetTempPath();
                    fullDownloadPath = Path.Combine(tempPath, string.Concat(holon.Name, ".", STARNETHolonFileExtention));
                    isFullDownloadPathTemp = true;
                }

                if (File.Exists(fullDownloadPath))
                    File.Delete(fullDownloadPath);

                if (holon.PublishedSTARNETHolon != null)
                {
                    await File.WriteAllBytesAsync(fullDownloadPath, holon.PublishedSTARNETHolon);
                    result = await InstallAsync(avatarId, fullDownloadPath, fullInstallPath, createSTARNETHolonDirectory, null, reInstall, providerType);
                }
                else
                {
                    OASISResult<T2> downloadResult = await DownloadAsync(avatarId, holon, fullDownloadPath, reInstall, providerType);

                    if (!fullDownloadPath.Contains(string.Concat(".", STARNETHolonFileExtention)))
                        fullDownloadPath = Path.Combine(fullDownloadPath, string.Concat(holon.Name, "_v", holon.STARNETDNA.Version, ".", STARNETHolonFileExtention));

                    if (downloadResult != null && downloadResult.Result != null && !downloadResult.IsError)
                        result = await InstallAsync(avatarId, fullDownloadPath, fullInstallPath, createSTARNETHolonDirectory, downloadResult.Result, reInstall, providerType);
                    else
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured downloading the {STARNETHolonUIName} with the DownloadSTARNETHolonAsync method, reason: {downloadResult.Message}");
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
                OnInstallStatusChanged?.Invoke(this, new STARNETHolonInstallStatusEventArgs() { STARNETDNA = holon.STARNETDNA, Status = STARNETHolonInstallStatus.Error, ErrorMessage = result.Message });

            return result;
        }

        //copied.
        public OASISResult<T3> DownloadAndInstall(Guid avatarId, T1 holon, string fullInstallPath, string fullDownloadPath = "", bool createSTARNETHolonDirectory = true, bool reInstall = false, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T3> result = new OASISResult<T3>();
            string errorMessage = "Error occured in STARNETManagerBase.DownloadAndInstall. Reason: ";
            bool isFullDownloadPathTemp = false;

            try
            {
                if (string.IsNullOrEmpty(fullDownloadPath))
                {
                    string tempPath = Path.GetTempPath();
                    fullDownloadPath = Path.Combine(tempPath, string.Concat(holon.Name, ".", STARNETHolonFileExtention));
                    isFullDownloadPathTemp = true;
                }

                if (File.Exists(fullDownloadPath))
                    File.Delete(fullDownloadPath);

                if (holon.PublishedSTARNETHolon != null)
                {
                    File.WriteAllBytes(fullDownloadPath, holon.PublishedSTARNETHolon);
                    result = Install(avatarId, fullDownloadPath, fullInstallPath, createSTARNETHolonDirectory, null, reInstall, providerType);
                }
                else
                {
                    OASISResult<T2> downloadResult = Download(avatarId, holon, fullDownloadPath, reInstall, providerType);

                    if (!fullDownloadPath.Contains(string.Concat(".", STARNETHolonFileExtention)))
                        fullDownloadPath = Path.Combine(fullDownloadPath, string.Concat(holon.Name, "_v", holon.STARNETDNA.Version, holon.Name, ".", STARNETHolonFileExtention));

                    if (downloadResult != null && downloadResult.Result != null && !downloadResult.IsError)
                        result = Install(avatarId, fullDownloadPath, fullInstallPath, createSTARNETHolonDirectory, downloadResult.Result, reInstall, providerType);
                    else
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured downloading the {STARNETHolonUIName} with the DownloadSTARNETHolonAsync method, reason: {downloadResult.Message}");
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
                OnInstallStatusChanged?.Invoke(this, new STARNETHolonInstallStatusEventArgs() { STARNETDNA = holon.STARNETDNA, Status = STARNETHolonInstallStatus.Error, ErrorMessage = result.Message });

            return result;
        }

        public virtual async Task<OASISResult<T3>> DownloadAndInstallAsync(Guid avatarId, Guid STARNETHolonId, int version, string fullInstallPath, string fullDownloadPath = "", bool createSTARNETHolonDirectory = true, bool reInstall = false, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T3> result = new OASISResult<T3>();
            OASISResult<T1> STARNETHolonResult = await Data.LoadHolonByMetaDataAsync<T1>(new Dictionary<string, string>()
            {
                { STARNETHolonIdName, STARNETHolonId.ToString() },
                { "VersionSequence", version.ToString() } ,
                { "Active", "1" } //TODO: Not sure if we need this?
            }, metaKeyValuePairMatchMode: MetaKeyValuePairMatchMode.All, providerType: providerType);

            if (STARNETHolonResult != null && !STARNETHolonResult.IsError && STARNETHolonResult.Result != null)
                result = await DownloadAndInstallAsync(avatarId, STARNETHolonResult.Result, fullInstallPath, fullDownloadPath, createSTARNETHolonDirectory, reInstall, providerType);
            else
            {
                OASISErrorHandling.HandleError(ref result, $"Error occured in STARNETManagerBase.DownloadAndInstallAsync loading the {STARNETHolonUIName} with the LoadAsync method, reason: {OASISErrorHandling.ProcessMessage(result, $"No result found for id {STARNETHolonId.ToString()} and version {version}.")}");
                OnInstallStatusChanged?.Invoke(this, new STARNETHolonInstallStatusEventArgs() { Status = STARNETHolonInstallStatus.Error, ErrorMessage = result.Message });
            }

            return result;
        }

        //copied.
        public OASISResult<T3> DownloadAndInstall(Guid avatarId, Guid STARNETHolonId, int version, string fullInstallPath, string fullDownloadPath = "", bool createSTARNETHolonDirectory = true, bool reInstall = false, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T3> result = new OASISResult<T3>();
            OASISResult<T1> STARNETHolonResult = Data.LoadHolonByMetaData<T1>(new Dictionary<string, string>()
            {
                { STARNETHolonIdName, STARNETHolonId.ToString() },
                { "VersionSequence", version.ToString() } ,
                { "Active", "1" } //TODO: Not sure if we need this?
            }, metaKeyValuePairMatchMode: MetaKeyValuePairMatchMode.All, providerType: providerType);


            if (STARNETHolonResult != null && !STARNETHolonResult.IsError && STARNETHolonResult.Result != null)
                result = DownloadAndInstall(avatarId, STARNETHolonResult.Result, fullInstallPath, fullDownloadPath, createSTARNETHolonDirectory, reInstall, providerType);
            else
            {
                OASISErrorHandling.HandleError(ref result, $"Error occured in STARNETManagerBase.DownloadAndInstall loading the {STARNETHolonUIName} with the LoadAsync method, reason: {OASISErrorHandling.ProcessMessage(result, $"No result found for id {STARNETHolonId.ToString()} and version {version}.")}");
                OnInstallStatusChanged?.Invoke(this, new STARNETHolonInstallStatusEventArgs() { Status = STARNETHolonInstallStatus.Error, ErrorMessage = result.Message });
            }

            return result;
        }

        public virtual async Task<OASISResult<T3>> DownloadAndInstallAsync(Guid avatarId, Guid STARNETHolonId, string version, string fullInstallPath, string fullDownloadPath = "", bool createSTARNETHolonDirectory = true, bool reInstall = false, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T3> result = new OASISResult<T3>();
            OASISResult<T1> STARNETHolonResult = await Data.LoadHolonByMetaDataAsync<T1>(new Dictionary<string, string>()
            {
                { STARNETHolonIdName, STARNETHolonId.ToString() },
                { "Version", version } ,
                { "Active", "1" } //TODO: Not sure if we need this?
            }, metaKeyValuePairMatchMode: MetaKeyValuePairMatchMode.All, providerType: providerType);

            if (STARNETHolonResult != null && !STARNETHolonResult.IsError && STARNETHolonResult.Result != null)
                result = await DownloadAndInstallAsync(avatarId, STARNETHolonResult.Result, fullInstallPath, fullDownloadPath, createSTARNETHolonDirectory, reInstall, providerType);
            else
            {
                OASISErrorHandling.HandleError(ref result, $"Error occured in STARNETManagerBase.DownloadAndInstallAsync loading the {STARNETHolonUIName} with the LoadHolonByMetaDataAsync method, reason: {OASISErrorHandling.ProcessMessage(result, $"No result found for id {STARNETHolonId.ToString()} and version {version}")}");
                OnInstallStatusChanged?.Invoke(this, new STARNETHolonInstallStatusEventArgs() { Status = STARNETHolonInstallStatus.Error, ErrorMessage = result.Message });
            }

            return result;
        }

        public virtual OASISResult<T3> DownloadAndInstall(Guid avatarId, Guid STARNETHolonId, string version, string fullInstallPath, string fullDownloadPath = "", bool createSTARNETHolonDirectory = true, bool reInstall = false, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T3> result = new OASISResult<T3>();
            OASISResult<T1> STARNETHolonResult = Data.LoadHolonByMetaData<T1>(new Dictionary<string, string>()
            {
                { STARNETHolonIdName, STARNETHolonId.ToString() },
                { "Version", version } ,
                { "Active", "1" } //TODO: Not sure if we need this?
            }, metaKeyValuePairMatchMode: MetaKeyValuePairMatchMode.All, providerType: providerType);

            if (STARNETHolonResult != null && !STARNETHolonResult.IsError && STARNETHolonResult.Result != null)
                result = DownloadAndInstall(avatarId, STARNETHolonResult.Result, fullInstallPath, fullDownloadPath, createSTARNETHolonDirectory, reInstall, providerType);
            else
            {
                OASISErrorHandling.HandleError(ref result, $"Error occured in STARNETManagerBase.DownloadAndInstall loading the {STARNETHolonUIName} with the LoadHolonByMetaData method, reason: {OASISErrorHandling.ProcessMessage(result, $"No result found for id {STARNETHolonId.ToString()} and version {version}")}");
                OnInstallStatusChanged?.Invoke(this, new STARNETHolonInstallStatusEventArgs() { Status = STARNETHolonInstallStatus.Error, ErrorMessage = result.Message });
            }

            return result;
        }

        public virtual async Task<OASISResult<T3>> DownloadAndInstallAsync(Guid avatarId, string STARNETHolonName, int version, string fullInstallPath, string fullDownloadPath = "", bool createSTARNETHolonDirectory = true, bool reInstall = false, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T3> result = new OASISResult<T3>();
            OASISResult<T1> STARNETHolonResult = await Data.LoadHolonByMetaDataAsync<T1>(new Dictionary<string, string>()
            {
                { STARNETHolonNameName, STARNETHolonName },
                { "VersionSequence", version.ToString() } ,
                { "Active", "1" } //TODO: Not sure if we need this?
            }, metaKeyValuePairMatchMode: MetaKeyValuePairMatchMode.All, providerType: providerType);

            if (STARNETHolonResult != null && !STARNETHolonResult.IsError && STARNETHolonResult.Result != null)
                result = await DownloadAndInstallAsync(avatarId, STARNETHolonResult.Result, fullInstallPath, fullDownloadPath, createSTARNETHolonDirectory, reInstall, providerType);
            else
            {
                OASISErrorHandling.HandleError(ref result, $"Error occured in STARNETManagerBase.DownloadAndInstallAsync loading the {STARNETHolonUIName} with the LoadHolonByMetaDataAsync method, reason: {OASISErrorHandling.ProcessMessage(result, $"No result found for name {STARNETHolonName} and version {version}")}");
                OnInstallStatusChanged?.Invoke(this, new STARNETHolonInstallStatusEventArgs() { Status = STARNETHolonInstallStatus.Error, ErrorMessage = result.Message });
            }

            return result;
        }

        //copied.
        public OASISResult<T3> DownloadAndInstall(Guid avatarId, string STARNETHolonName, int version, string fullInstallPath, string fullDownloadPath = "", bool createSTARNETHolonDirectory = true, bool reInstall = false, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T3> result = new OASISResult<T3>();
            OASISResult<T1> STARNETHolonResult = Data.LoadHolonByMetaData<T1>(new Dictionary<string, string>()
            {
                { STARNETHolonNameName, STARNETHolonName },
                { "VersionSequence", version.ToString() } ,
                { "Active", "1" } //TODO: Not sure if we need this?
            }, metaKeyValuePairMatchMode: MetaKeyValuePairMatchMode.All, providerType: providerType);

            if (STARNETHolonResult != null && !STARNETHolonResult.IsError && STARNETHolonResult.Result != null)
                result = DownloadAndInstall(avatarId, STARNETHolonResult.Result, fullInstallPath, fullDownloadPath, createSTARNETHolonDirectory, reInstall, providerType);
            else
            {
                OASISErrorHandling.HandleError(ref result, $"Error occured in STARNETManagerBase.DownloadAndInstall loading the {STARNETHolonUIName} with the LoadHolonByMetaData method, reason: {OASISErrorHandling.ProcessMessage(result, $"No result found for name {STARNETHolonName} and version {version}")}");
                OnInstallStatusChanged?.Invoke(this, new STARNETHolonInstallStatusEventArgs() { Status = STARNETHolonInstallStatus.Error, ErrorMessage = result.Message });
            }

            return result;
        }

        public virtual async Task<OASISResult<T3>> DownloadAndInstallAsync(Guid avatarId, string STARNETHolonName, string version, string fullInstallPath, string fullDownloadPath = "", bool createSTARNETHolonDirectory = true, bool reInstall = false, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T3> result = new OASISResult<T3>();
            OASISResult<T1> STARNETHolonResult = await Data.LoadHolonByMetaDataAsync<T1>(new Dictionary<string, string>()
            {
                { STARNETHolonNameName, STARNETHolonName },
                { "Version", version } ,
                { "Active", "1" } //TODO: Not sure if we need this?
            }, metaKeyValuePairMatchMode: MetaKeyValuePairMatchMode.All, providerType: providerType);

            if (STARNETHolonResult != null && !STARNETHolonResult.IsError && STARNETHolonResult.Result != null)
                result = await DownloadAndInstallAsync(avatarId, STARNETHolonResult.Result, fullInstallPath, fullDownloadPath, createSTARNETHolonDirectory, reInstall, providerType);
            else
            {
                OASISErrorHandling.HandleError(ref result, $"Error occured in STARNETManagerBase.DownloadAndInstallAsync loading the {STARNETHolonUIName} with the LoadHolonByMetaDataAsync method, reason: {OASISErrorHandling.ProcessMessage(result, $"No result found for name {STARNETHolonName} and version {version}")}");
                OnInstallStatusChanged?.Invoke(this, new STARNETHolonInstallStatusEventArgs() { Status = STARNETHolonInstallStatus.Error, ErrorMessage = result.Message });
            }

            return result;
        }

        public virtual OASISResult<T3> DownloadAndInstall(Guid avatarId, string STARNETHolonName, string version, string fullInstallPath, string fullDownloadPath = "", bool createSTARNETHolonDirectory = true, bool reInstall = false, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T3> result = new OASISResult<T3>();
            OASISResult<T1> STARNETHolonResult = Data.LoadHolonByMetaData<T1>(new Dictionary<string, string>()
            {
                { STARNETHolonNameName, STARNETHolonName },
                { "Version", version } ,
                { "Active", "1" } //TODO: Not sure if we need this?
            }, metaKeyValuePairMatchMode: MetaKeyValuePairMatchMode.All, providerType: providerType);

            if (STARNETHolonResult != null && !STARNETHolonResult.IsError && STARNETHolonResult.Result != null)
                result = DownloadAndInstall(avatarId, STARNETHolonResult.Result, fullInstallPath, fullDownloadPath, createSTARNETHolonDirectory, reInstall, providerType);
            else
            {
                OASISErrorHandling.HandleError(ref result, $"Error occured in STARNETManagerBase.DownloadAndInstall loading the {STARNETHolonUIName} with the LoadHolonByMetaData method, reason: {OASISErrorHandling.ProcessMessage(result, $"No result found for name {STARNETHolonName} and version {version}")}");
                OnInstallStatusChanged?.Invoke(this, new STARNETHolonInstallStatusEventArgs() { Status = STARNETHolonInstallStatus.Error, ErrorMessage = result.Message });
            }

            return result;
        }

        public virtual async Task<OASISResult<T3>> InstallAsync(Guid avatarId, string fullPathToPublishedOrDownloadedSTARNETHolonFile, string fullInstallPath, bool createSTARNETHolonDirectory = true, IDownloadedSTARNETHolon downloadedSTARNETHolon = null, bool reInstall = false, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T3> result = new OASISResult<T3>();
            string errorMessage = "Error occured in STARNETManagerBase.InstallAsync. Reason: ";
            T4 STARNETDNA = default;
            T1 STARNETHolon = default;
            string tempPath = "";
            T3 installedSTARNETHolon = default;
            int totalInstalls = 0;

            try
            {
                tempPath = Path.GetTempPath();
                tempPath = Path.Combine(tempPath, $"{STARNETHolonUIName}");

                if (Directory.Exists(tempPath))
                    Directory.Delete(tempPath, true);

                //Unzip
                OnInstallStatusChanged?.Invoke(this, new STARNETHolonInstallStatusEventArgs() { Status = STARNETHolonInstallStatus.Decompressing });
                ZipFile.ExtractToDirectory(fullPathToPublishedOrDownloadedSTARNETHolonFile, tempPath, Encoding.Default, true);
                OASISResult<T4> STARNETDNAResult = await ReadDNAFromSourceOrInstallFolderAsync<T4>(tempPath);

                if (STARNETDNAResult != null && STARNETDNAResult.Result != null && !STARNETDNAResult.IsError)
                {
                    //Load the T from the OASIS to make sure the STARNETDNA is valid (and has not been tampered with).

                    //TODO: Check if this works ok? What if they tamper with the VersionSequence in the DNA file?!
                    OASISResult<T1> STARNETHolonLoadResult = await LoadAsync(avatarId, STARNETDNAResult.Result.Id, STARNETDNAResult.Result.VersionSequence, providerType: providerType);

                    if (STARNETHolonLoadResult != null && STARNETHolonLoadResult.Result != null && !STARNETHolonLoadResult.IsError)
                    {
                        //TODO: Not sure if we want to add a check here to compare the STARNETDNA in the T dir with the one stored in the OASIS?
                        STARNETDNA = (T4)STARNETHolonLoadResult.Result.STARNETDNA;
                        STARNETHolon = STARNETHolonLoadResult.Result;

                        if (createSTARNETHolonDirectory)
                            fullInstallPath = Path.Combine(fullInstallPath, string.Concat(STARNETDNAResult.Result.Name, "_v", STARNETDNAResult.Result.Version));

                        if (Directory.Exists(fullInstallPath))
                            Directory.Delete(fullInstallPath, true);

                        OnInstallStatusChanged?.Invoke(this, new STARNETHolonInstallStatusEventArgs() { STARNETDNA = STARNETDNAResult.Result, Status = STARNETHolonInstallStatus.Installing });
                        Directory.Move(tempPath, fullInstallPath);
                        OASISResult<IAvatar> avatarResult = await AvatarManager.Instance.LoadAvatarAsync(avatarId, false, true, providerType);

                        if (avatarResult != null && !avatarResult.IsError && avatarResult.Result != null)
                        {
                            if (downloadedSTARNETHolon == null)
                            {
                                //OASISResult<DownloadedSTARNETHolon> downloadedSTARNETHolonResult = await Data.LoadHolonsByMetaDataAsync<DownloadedSTARNETHolon>("STARNETHolonId", STARNETDNAResult.Result.Id.ToString(), false, false, 0, true, 0, false, HolonType.All, providerType);
                                OASISResult<IEnumerable<T2>> downloadedSTARNETHolonResult = await Data.LoadHolonsByMetaDataAsync<T2>(STARNETHolonIdName, STARNETDNAResult.Result.Id.ToString(), HolonType.All, true, true, 0, true, false, 0, HolonType.All, 0, providerType);

                                if (downloadedSTARNETHolonResult != null && !downloadedSTARNETHolonResult.IsError && downloadedSTARNETHolonResult.Result != null)
                                    downloadedSTARNETHolon = downloadedSTARNETHolonResult.Result.FirstOrDefault();
                                else
                                    OASISErrorHandling.HandleWarning(ref result, $"The {STARNETHolonUIName} was installed but the DownloadedSTARNETHolon could not be found. Reason: {downloadedSTARNETHolonResult.Message}");
                            }

                            if (!reInstall)
                            {
                                //If it's a re-install then it doesnt count as an install so we dont need to update the counts.
                                STARNETDNA.Installs++;

                                installedSTARNETHolon = new T3()
                                {
                                    //ParentHolonId = STARNETHolonLoadResult.Result.Id,
                                    ParentSTARNETHolonId = STARNETDNA.Id,
                                    Name = string.Concat(STARNETDNA.Name, " Installed Holon"),
                                    Description = string.Concat(STARNETDNA.Description, " Installed Holon"),
                                    //STARNETHolonId = STARNETDNAResult.Result.STARNETHolonId,
                                    STARNETDNA = STARNETDNA,
                                    InstalledBy = avatarId,
                                    InstalledByAvatarUsername = avatarResult.Result.Username,
                                    InstalledOn = DateTime.Now,
                                    InstalledPath = fullInstallPath,
                                    //DownloadedSTARNETHolon = downloadedSTARNETHolon,
                                    DownloadedBy = downloadedSTARNETHolon.DownloadedBy,
                                    DownloadedByAvatarUsername = downloadedSTARNETHolon.DownloadedByAvatarUsername,
                                    DownloadedOn = downloadedSTARNETHolon.DownloadedOn,
                                    DownloadedPath = downloadedSTARNETHolon.DownloadedPath,
                                    DownloadedSTARNETHolonId = downloadedSTARNETHolon.Id,
                                    Active = "1",
                                    MetaData = STARNETHolon.MetaData
                                    //STARNETHolonVersion = STARNETDNA.Version
                                };

                                installedSTARNETHolon.MetaData["Version"] = STARNETDNA.Version;
                                installedSTARNETHolon.MetaData["VersionSequence"] = STARNETDNA.VersionSequence;
                                installedSTARNETHolon.MetaData[STARNETHolonIdName] = STARNETDNA.Id;

                                await UpdateInstallCountsAsync(avatarId, installedSTARNETHolon, STARNETDNA, result, errorMessage, providerType);
                            }
                            else
                            {
                                OASISResult<T3> installedSTARNETHolonResult = await LoadInstalledAsync(avatarId, STARNETDNAResult.Result.Id, STARNETDNAResult.Result.Version, false, providerType);

                                if (installedSTARNETHolonResult != null && installedSTARNETHolonResult.Result != null && !installedSTARNETHolonResult.IsError)
                                {
                                    installedSTARNETHolon = installedSTARNETHolonResult.Result;
                                    installedSTARNETHolon.Active = "1";
                                    installedSTARNETHolon.UninstalledBy = Guid.Empty;
                                    installedSTARNETHolon.UninstalledByAvatarUsername = "";
                                    installedSTARNETHolon.UninstalledOn = DateTime.MinValue;
                                    installedSTARNETHolon.InstalledBy = avatarId;
                                    installedSTARNETHolon.InstalledByAvatarUsername = avatarResult.Result.Username;
                                    installedSTARNETHolon.InstalledOn = DateTime.Now;
                                    installedSTARNETHolon.InstalledPath = fullInstallPath;
                                    installedSTARNETHolon.DownloadedBy = downloadedSTARNETHolon.DownloadedBy;
                                    installedSTARNETHolon.DownloadedByAvatarUsername = downloadedSTARNETHolon.DownloadedByAvatarUsername;
                                    installedSTARNETHolon.DownloadedOn = downloadedSTARNETHolon.DownloadedOn;
                                    installedSTARNETHolon.DownloadedPath = downloadedSTARNETHolon.DownloadedPath;
                                    installedSTARNETHolon.MetaData = STARNETHolon.MetaData;
                                }
                                else
                                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured re-installing {STARNETHolonUIName} calling LoadAsync. Reason: {installedSTARNETHolonResult.Message}");
                            }

                            if (!result.IsError)
                            {
                                OASISResult<T3> saveResult = await UpdateAsync(avatarId, installedSTARNETHolon, providerType: providerType);

                                if (saveResult != null && saveResult.Result != null && !saveResult.IsError)
                                {
                                    STARNETHolonLoadResult.Result.STARNETDNA = STARNETDNA;
                                    OASISResult<T1> oappSaveResult = await UpdateAsync(avatarId, STARNETHolonLoadResult.Result, providerType: providerType);

                                    if (oappSaveResult != null && !oappSaveResult.IsError && oappSaveResult.Result != null)
                                    {
                                        CheckForVersionMismatches(STARNETDNAResult.Result, ref result);

                                        OnInstallStatusChanged?.Invoke(this, new STARNETHolonInstallStatusEventArgs() { STARNETDNA = STARNETDNAResult.Result, Status = STARNETHolonInstallStatus.InstallingDependencies });
                                        result = await InstallDependenciesAsync(avatarId, STARNETHolon, fullInstallPath, errorMessage, result, providerType);

                                        if (result.InnerMessages.Count > 0)
                                            result.Message = $"{STARNETHolonUIName} successfully installed but there were {result.WarningCount} warnings:\n\n {OASISResultHelper.BuildInnerMessageError(result.InnerMessages)}";
                                        else
                                            result.Message = $"{STARNETHolonUIName} Successfully Installed";

                                        result.Result = installedSTARNETHolon;
                                        OnInstallStatusChanged?.Invoke(this, new STARNETHolonInstallStatusEventArgs() { STARNETDNA = STARNETDNAResult.Result, Status = STARNETHolonInstallStatus.Installed });
                                    }
                                    else
                                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling UpdateAsync method. Reason: {oappSaveResult.Message}");
                                }
                                else
                                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling UpdateAsync method. Reason: {saveResult.Message}");
                            }
                            else
                                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadAvatarAsync method. Reason: {avatarResult.Message}");
                        }
                    }
                    else
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadAsync method. Reason: {STARNETHolonLoadResult.Message}");
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
                OnInstallStatusChanged?.Invoke(this, new STARNETHolonInstallStatusEventArgs() { STARNETDNA = STARNETDNA, Status = STARNETHolonInstallStatus.Error, ErrorMessage = result.Message });

            return result;
        }

        //Copied from async
        public OASISResult<T3> Install(Guid avatarId, string fullPathToPublishedOrDownloadedSTARNETHolonFile, string fullInstallPath, bool createSTARNETHolonDirectory = true, IDownloadedSTARNETHolon downloadedSTARNETHolon = null, bool reInstall = false, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T3> result = new OASISResult<T3>();
            string errorMessage = "Error occured in STARNETManagerBase.Install. Reason: ";
            T4 STARNETDNA = default;
            T1 STARNETHolon = default;
            string tempPath = "";
            T3 installedSTARNETHolon = default;
            int totalInstalls = 0;

            try
            {
                tempPath = Path.GetTempPath();
                tempPath = Path.Combine(tempPath, $"{STARNETHolonUIName}");

                if (Directory.Exists(tempPath))
                    Directory.Delete(tempPath, true);

                //Unzip
                OnInstallStatusChanged?.Invoke(this, new STARNETHolonInstallStatusEventArgs() { Status = STARNETHolonInstallStatus.Decompressing });
                ZipFile.ExtractToDirectory(fullPathToPublishedOrDownloadedSTARNETHolonFile, tempPath, Encoding.Default, true);
                OASISResult<T4> STARNETDNAResult = ReadDNAFromSourceOrInstallFolder<T4>(tempPath);

                if (STARNETDNAResult != null && STARNETDNAResult.Result != null && !STARNETDNAResult.IsError)
                {
                    //Load the T from the OASIS to make sure the STARNETDNA is valid (and has not been tampered with).

                    //TODO: Check if this works ok? What if they tamper with the VersionSequence in the DNA file?!
                    OASISResult<T1> STARNETHolonLoadResult = Load(avatarId, STARNETDNAResult.Result.Id, STARNETDNAResult.Result.VersionSequence, providerType: providerType);
                    //OASISResult<ISTARNETHolon> STARNETHolonLoadResult = await LoadSTARNETHolonAsync(STARNETDNAResult.Result.Id, false, 0, providerType);

                    if (STARNETHolonLoadResult != null && STARNETHolonLoadResult.Result != null && !STARNETHolonLoadResult.IsError)
                    {
                        //TODO: Not sure if we want to add a check here to compare the STARNETDNA in the T dir with the one stored in the OASIS?
                        STARNETDNA = (T4)STARNETHolonLoadResult.Result.STARNETDNA;
                        STARNETHolon = STARNETHolonLoadResult.Result;

                        if (createSTARNETHolonDirectory)
                            fullInstallPath = Path.Combine(fullInstallPath, string.Concat(STARNETDNAResult.Result.Name, "_v", STARNETDNAResult.Result.Version));

                        if (Directory.Exists(fullInstallPath))
                            Directory.Delete(fullInstallPath, true);

                        //Directory.CreateDirectory(fullInstallPath);
                        Directory.Move(tempPath, fullInstallPath);
                        //Directory.Delete(tempPath);

                        OnInstallStatusChanged?.Invoke(this, new STARNETHolonInstallStatusEventArgs() { STARNETDNA = STARNETDNAResult.Result, Status = STARNETHolonInstallStatus.Installing });
                        OASISResult<IAvatar> avatarResult = AvatarManager.Instance.LoadAvatar(avatarId, false, true, providerType);

                        if (avatarResult != null && !avatarResult.IsError && avatarResult.Result != null)
                        {
                            if (downloadedSTARNETHolon == null)
                            {
                                //OASISResult<DownloadedSTARNETHolon> downloadedSTARNETHolonResult = await Data.LoadHolonsByMetaDataAsync<DownloadedSTARNETHolon>("STARNETHolonId", STARNETDNAResult.Result.Id.ToString(), false, false, 0, true, 0, false, HolonType.All, providerType);
                                OASISResult<IEnumerable<T2>> downloadedSTARNETHolonResult = Data.LoadHolonsByMetaData<T2>(STARNETHolonIdName, STARNETDNAResult.Result.Id.ToString(), HolonType.All, true, true, 0, true, false, 0, HolonType.All, 0, providerType);

                                if (downloadedSTARNETHolonResult != null && !downloadedSTARNETHolonResult.IsError && downloadedSTARNETHolonResult.Result != null)
                                    downloadedSTARNETHolon = downloadedSTARNETHolonResult.Result.FirstOrDefault();
                                else
                                    OASISErrorHandling.HandleWarning(ref result, $"The {STARNETHolonUIName} was installed but the DownloadedSTARNETHolon could not be found. Reason: {downloadedSTARNETHolonResult.Message}");
                            }

                            if (!reInstall)
                            {
                                //If it's a re-install then it doesnt count as an install so we dont need to update the counts.
                                STARNETDNA.Installs++;

                                installedSTARNETHolon = new T3()
                                {
                                    //ParentHolonId = STARNETHolonLoadResult.Result.Id,
                                    ParentSTARNETHolonId = STARNETDNA.Id,
                                    Name = string.Concat(STARNETDNA.Name, " Installed Holon"),
                                    Description = string.Concat(STARNETDNA.Description, " Installed Holon"),
                                    //STARNETHolonId = STARNETDNAResult.Result.STARNETHolonId,
                                    STARNETDNA = STARNETDNA,
                                    InstalledBy = avatarId,
                                    InstalledByAvatarUsername = avatarResult.Result.Username,
                                    InstalledOn = DateTime.Now,
                                    InstalledPath = fullInstallPath,
                                    //DownloadedSTARNETHolon = downloadedSTARNETHolon,
                                    DownloadedBy = downloadedSTARNETHolon.DownloadedBy,
                                    DownloadedByAvatarUsername = downloadedSTARNETHolon.DownloadedByAvatarUsername,
                                    DownloadedOn = downloadedSTARNETHolon.DownloadedOn,
                                    DownloadedPath = downloadedSTARNETHolon.DownloadedPath,
                                    DownloadedSTARNETHolonId = downloadedSTARNETHolon.Id,
                                    Active = "1",
                                    MetaData = STARNETHolon.MetaData
                                    //STARNETHolonVersion = STARNETDNA.Version
                                };

                                installedSTARNETHolon.MetaData["Version"] = STARNETDNA.Version;
                                installedSTARNETHolon.MetaData["VersionSequence"] = STARNETDNA.VersionSequence;
                                installedSTARNETHolon.MetaData[STARNETHolonIdName] = STARNETDNA.Id;

                                UpdateInstallCounts(avatarId, installedSTARNETHolon, STARNETDNA, result, errorMessage, providerType);
                            }
                            else
                            {
                                OASISResult<T3> installedSTARNETHolonResult = LoadInstalled(avatarId, STARNETDNAResult.Result.Id, STARNETDNAResult.Result.Version, false, providerType);

                                if (installedSTARNETHolonResult != null && installedSTARNETHolonResult.Result != null && !installedSTARNETHolonResult.IsError)
                                {
                                    installedSTARNETHolon = installedSTARNETHolonResult.Result;
                                    installedSTARNETHolon.Active = "1";
                                    installedSTARNETHolon.UninstalledBy = Guid.Empty;
                                    installedSTARNETHolon.UninstalledByAvatarUsername = "";
                                    installedSTARNETHolon.UninstalledOn = DateTime.MinValue;
                                    installedSTARNETHolon.InstalledBy = avatarId;
                                    installedSTARNETHolon.InstalledByAvatarUsername = avatarResult.Result.Username;
                                    installedSTARNETHolon.InstalledOn = DateTime.Now;
                                    installedSTARNETHolon.InstalledPath = fullInstallPath;
                                    installedSTARNETHolon.DownloadedBy = downloadedSTARNETHolon.DownloadedBy;
                                    installedSTARNETHolon.DownloadedByAvatarUsername = downloadedSTARNETHolon.DownloadedByAvatarUsername;
                                    installedSTARNETHolon.DownloadedOn = downloadedSTARNETHolon.DownloadedOn;
                                    installedSTARNETHolon.DownloadedPath = downloadedSTARNETHolon.DownloadedPath;
                                    installedSTARNETHolon.MetaData = STARNETHolon.MetaData;
                                }
                                else
                                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured re-installing {STARNETHolonUIName} calling LoadAsync. Reason: {installedSTARNETHolonResult.Message}");
                            }

                            if (!result.IsError)
                            {
                                OASISResult<T3> saveResult = Update(avatarId, installedSTARNETHolon, providerType: providerType);

                                if (saveResult != null && saveResult.Result != null && !saveResult.IsError)
                                {
                                    //result.Result = installedSTARNETHolon;
                                    //result.Result.DownloadedSTARNETHolon = downloadedSTARNETHolon;
                                    STARNETHolonLoadResult.Result.STARNETDNA = STARNETDNA;

                                    OASISResult<T1> oappSaveResult = Update(avatarId, STARNETHolonLoadResult.Result, providerType: providerType);

                                    if (oappSaveResult != null && !oappSaveResult.IsError && oappSaveResult.Result != null)
                                    {
                                        CheckForVersionMismatches(STARNETDNAResult.Result, ref result);

                                        OnInstallStatusChanged?.Invoke(this, new STARNETHolonInstallStatusEventArgs() { STARNETDNA = STARNETDNAResult.Result, Status = STARNETHolonInstallStatus.InstallingDependencies });
                                        result = InstallDependencies(avatarId, STARNETHolon, fullInstallPath, errorMessage, result, providerType);

                                        if (result.InnerMessages.Count > 0)
                                            result.Message = $"{STARNETHolonUIName} successfully installed but there were {result.WarningCount} warnings:\n\n {OASISResultHelper.BuildInnerMessageError(result.InnerMessages)}";
                                        else
                                            result.Message = $"{STARNETHolonUIName} Successfully Installed";

                                        result.Result = installedSTARNETHolon;
                                        OnInstallStatusChanged?.Invoke(this, new STARNETHolonInstallStatusEventArgs() { STARNETDNA = STARNETDNAResult.Result, Status = STARNETHolonInstallStatus.Installed });
                                    }
                                    else
                                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling UpdateAsync method. Reason: {oappSaveResult.Message}");
                                }
                                else
                                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling UpdateAsync method. Reason: {saveResult.Message}");
                            }
                            else
                                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadAvatarAsync method. Reason: {avatarResult.Message}");
                        }
                    }
                    else
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadSTARNETHolonAsync method. Reason: {STARNETHolonLoadResult.Message}");
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
                OnInstallStatusChanged?.Invoke(this, new STARNETHolonInstallStatusEventArgs() { STARNETDNA = STARNETDNA, Status = STARNETHolonInstallStatus.Error, ErrorMessage = result.Message });

            return result;
        }

        public virtual async Task<OASISResult<T3>> UninstallAsync(Guid avatarId, T3 installedSTARNETHolon, string errorMessage, ProviderType providerType)
        {
            OASISResult<T3> result = new OASISResult<T3>();

            try
            {
                Directory.Delete(installedSTARNETHolon.InstalledPath, true);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} Error occured attempting to delete the {STARNETHolonUIName} folder ({installedSTARNETHolon.InstalledPath}) Reason: {ex.Message}");
            }

            OASISResult<IAvatar> avatarResult = await AvatarManager.Instance.LoadAvatarAsync(avatarId, false, true, providerType, 0);

            if (avatarResult != null && avatarResult.Result != null && !avatarResult.IsError)
            {
                installedSTARNETHolon.UninstalledBy = avatarId;
                installedSTARNETHolon.UninstalledOn = DateTime.Now;
                installedSTARNETHolon.UninstalledByAvatarUsername = avatarResult.Result.Username;
                installedSTARNETHolon.Active = "0";

                OASISResult<T3> saveIntalledSTARNETHolonResult = await installedSTARNETHolon.SaveAsync<T3>();

                if (saveIntalledSTARNETHolonResult != null && !saveIntalledSTARNETHolonResult.IsError && saveIntalledSTARNETHolonResult.Result != null)
                {
                    result.Message = $"{STARNETHolonUIName} Uninstalled";
                    result.Result = saveIntalledSTARNETHolonResult.Result;
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling UpdateAsync. Reason: {saveIntalledSTARNETHolonResult.Message}");
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadAvatarAsync. Reason: {avatarResult.Message}");

            return result;
        }

        //copied.
        public OASISResult<T3> Uninstall(Guid avatarId, T3 installedSTARNETHolon, string errorMessage, ProviderType providerType)
        {
            OASISResult<T3> result = new OASISResult<T3>();

            try
            {
                Directory.Delete(installedSTARNETHolon.InstalledPath, true);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} Error occured attempting to delete the {STARNETHolonUIName} folder ({installedSTARNETHolon.InstalledPath}) Reason: {ex.Message}");
            }

            //if (!result.IsError)
            //{
            OASISResult<IAvatar> avatarResult = AvatarManager.Instance.LoadAvatar(avatarId, false, true, providerType, 0);

            if (avatarResult != null && avatarResult.Result != null && !avatarResult.IsError)
            {
                installedSTARNETHolon.UninstalledBy = avatarId;
                installedSTARNETHolon.UninstalledOn = DateTime.Now;
                installedSTARNETHolon.UninstalledByAvatarUsername = avatarResult.Result.Username;
                installedSTARNETHolon.Active = "0";

                OASISResult<T3> saveIntalledSTARNETHolonResult = installedSTARNETHolon.Save<T3>();

                if (saveIntalledSTARNETHolonResult != null && !saveIntalledSTARNETHolonResult.IsError && saveIntalledSTARNETHolonResult.Result != null)
                {
                    result.Message = $"{STARNETHolonUIName} Uninstalled";
                    result.Result = saveIntalledSTARNETHolonResult.Result;
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling UpdateAsync. Reason: {saveIntalledSTARNETHolonResult.Message}");
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadAvatarAsync. Reason: {avatarResult.Message}");
            //}

            return result;
        }

        public virtual async Task<OASISResult<T3>> UninstallAsync(Guid avatarId, Guid STARNETHolonId, int versionSequence, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T3> result = new OASISResult<T3>();
            string errorMessage = "Error occured in STARNETManagerBase.UninstallAsync. Reason: ";

            OASISResult<T3> loadResult = await Data.LoadHolonByMetaDataAsync<T3>(new Dictionary<string, string>()
            {
                { STARNETHolonIdName, STARNETHolonId.ToString() },
                { "VersionSequene", versionSequence.ToString() }

            }, MetaKeyValuePairMatchMode.All, STARNETHolonInstalledHolonType, true, true, 0, true, 0, false, HolonType.All, providerType);

            if (loadResult != null && loadResult.Result != null && !loadResult.IsError)
                result = await UninstallAsync(avatarId, loadResult.Result, errorMessage, providerType);
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonByMetaDataAsync. Reason: {loadResult.Message}");

            return result;
        }

        public OASISResult<T3> Uninstall(Guid avatarId, Guid STARNETHolonId, int versionSequence, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T3> result = new OASISResult<T3>();
            string errorMessage = "Error occured in STARNETManagerBase.UninstallAsync. Reason: ";

            OASISResult<T3> loadResult = Data.LoadHolonByMetaData<T3>(new Dictionary<string, string>()
            {
                { STARNETHolonIdName, STARNETHolonId.ToString() },
                { "VersionSequene", versionSequence.ToString() }

            }, MetaKeyValuePairMatchMode.All, STARNETHolonInstalledHolonType, true, true, 0, true, false, HolonType.All);

            if (loadResult != null && loadResult.Result != null && !loadResult.IsError)
                result = Uninstall(avatarId, loadResult.Result, errorMessage, providerType);
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonByMetaData. Reason: {loadResult.Message}");

            return result;
        }

        public virtual async Task<OASISResult<T3>> UninstallAsync(Guid avatarId, Guid STARNETHolonId, string version, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T3> result = new OASISResult<T3>();
            string errorMessage = "Error occured in STARNETManagerBase.UninstallAsync. Reason: ";

            OASISResult<T3> loadResult = await Data.LoadHolonByMetaDataAsync<T3>(new Dictionary<string, string>()
            {
                { STARNETHolonIdName, STARNETHolonId.ToString() },
                { "Version", version }

            }, MetaKeyValuePairMatchMode.All, STARNETHolonInstalledHolonType, true, true, 0, true, 0, false, HolonType.All, providerType);

            if (loadResult != null && loadResult.Result != null && !loadResult.IsError)
                result = await UninstallAsync(avatarId, loadResult.Result, errorMessage, providerType);
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonByMetaDataAsync. Reason: {loadResult.Message}");

            return result;
        }

        public OASISResult<T3> Uninstall(Guid avatarId, Guid STARNETHolonId, string version, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T3> result = new OASISResult<T3>();
            string errorMessage = "Error occured in STARNETManagerBase.Uninstall. Reason: ";

            OASISResult<T3> loadResult = Data.LoadHolonByMetaData<T3>(new Dictionary<string, string>()
            {
                { STARNETHolonIdName, STARNETHolonId.ToString() },
                { "Version", version }

            }, MetaKeyValuePairMatchMode.All, STARNETHolonInstalledHolonType, true, true, 0, true, false, HolonType.All);

            if (loadResult != null && loadResult.Result != null && !loadResult.IsError)
                result = Uninstall(avatarId, loadResult.Result, errorMessage, providerType);
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonByMetaData. Reason: {loadResult.Message}");

            return result;
        }

        public virtual async Task<OASISResult<T3>> UninstallAsync(Guid avatarId, string STARNETHolonName, int versionSequence, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T3> result = new OASISResult<T3>();
            string errorMessage = "Error occured in STARNETManagerBase.UninstallAsync. Reason: ";

            OASISResult<T3> loadResult = await Data.LoadHolonByMetaDataAsync<T3>(new Dictionary<string, string>()
            {
                { STARNETHolonNameName, STARNETHolonName },
                { "VersionSequene", versionSequence.ToString() }

            }, MetaKeyValuePairMatchMode.All, STARNETHolonInstalledHolonType, true, true, 0, true, 0, false, HolonType.All, providerType);

            if (loadResult != null && loadResult.Result != null && !loadResult.IsError)
                result = await UninstallAsync(avatarId, loadResult.Result, errorMessage, providerType);
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonByMetaDataAsync. Reason: {loadResult.Message}");

            return result;
        }

        public OASISResult<T3> Uninstall(Guid avatarId, string STARNETHolonName, int versionSequence, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T3> result = new OASISResult<T3>();
            string errorMessage = "Error occured in STARNETManagerBase.Uninstall. Reason: ";

            OASISResult<T3> loadResult = Data.LoadHolonByMetaData<T3>(new Dictionary<string, string>()
            {
                { STARNETHolonNameName, STARNETHolonName},
                { "VersionSequene", versionSequence.ToString() }

            }, MetaKeyValuePairMatchMode.All, STARNETHolonInstalledHolonType, true, true, 0, true, false, HolonType.All);

            if (loadResult != null && loadResult.Result != null && !loadResult.IsError)
                result = Uninstall(avatarId, loadResult.Result, errorMessage, providerType);
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonByMetaData. Reason: {loadResult.Message}");

            return result;
        }

        public virtual async Task<OASISResult<T3>> UninstallAsync(Guid avatarId, string STARNETHolonName, string version, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T3> result = new OASISResult<T3>();
            string errorMessage = "Error occured in STARNETManagerBase.UninstallAsync. Reason: ";

            OASISResult<T3> loadResult = await Data.LoadHolonByMetaDataAsync<T3>(new Dictionary<string, string>()
            {
                { STARNETHolonNameName, STARNETHolonName },
                { "Version", version }

            }, MetaKeyValuePairMatchMode.All, STARNETHolonInstalledHolonType, true, true, 0, true, 0, false, HolonType.All, providerType);

            if (loadResult != null && loadResult.Result != null && !loadResult.IsError)
                result = await UninstallAsync(avatarId, loadResult.Result, errorMessage, providerType);
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonByMetaDataAsync. Reason: {loadResult.Message}");

            return result;
        }

        public OASISResult<T3> Uninstall(Guid avatarId, string STARNETHolonName, string version, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T3> result = new OASISResult<T3>();
            string errorMessage = "Error occured in STARNETManagerBase.Uninstall. Reason: ";

            OASISResult<T3> loadResult = Data.LoadHolonByMetaData<T3>(new Dictionary<string, string>()
            {
                { STARNETHolonNameName, STARNETHolonName},
                { "Version", version }

            }, MetaKeyValuePairMatchMode.All, STARNETHolonInstalledHolonType, true, true, 0, true, false, HolonType.All);

            if (loadResult != null && loadResult.Result != null && !loadResult.IsError)
                result = Uninstall(avatarId, loadResult.Result, errorMessage, providerType);
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonByMetaData. Reason: {loadResult.Message}");

            return result;
        }

        public virtual async Task<OASISResult<IEnumerable<T3>>> ListInstalledAsync(Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<T3>> result = await Data.LoadHolonsForParentAsync<T3>(avatarId, STARNETHolonInstalledHolonType, false, false, 0, true, false, 0, HolonType.All, 0, providerType);

            if (result != null && !result.IsError && result.Result != null)
                result.Result = result.Result.Where(x => x.UninstalledOn == DateTime.MinValue);
            else
                OASISErrorHandling.HandleError(ref result, $"Error occured in STARNETManagerBase.ListInstalledAsync. Reason: Error occured calling LoadHolonsForParentAsync. Reason: {result.Message}");

            return result;
        }

        public OASISResult<IEnumerable<T3>> ListInstalled(Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<T3>> result = Data.LoadHolonsForParent<T3>(avatarId, STARNETHolonInstalledHolonType, false, false, 0, true, false, 0, HolonType.All, 0, providerType);

            if (result != null && !result.IsError && result.Result != null)
                result.Result = result.Result.Where(x => x.UninstalledOn == DateTime.MinValue);
            else
                OASISErrorHandling.HandleError(ref result, $"Error occured in STARNETManagerBase.ListInstalled. Reason: Error occured calling LoadHolonsForParent. Reason: {result.Message}");

            return result;
        }

        public virtual async Task<OASISResult<IEnumerable<T3>>> ListUninstalledAsync(Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<T3>> result = await Data.LoadHolonsForParentAsync<T3>(avatarId, STARNETHolonInstalledHolonType, false, false, 0, true, false, 0, HolonType.All, 0, providerType);

            if (result != null && !result.IsError && result.Result != null)
                result.Result = result.Result.Where(x => x.UninstalledOn != DateTime.MinValue);
            else
                OASISErrorHandling.HandleError(ref result, $"Error occured in STARNETManagerBase.ListUninstalledAsync. Reason:  Error occured calling LoadHolonsForParent. Reason: {result.Message}");

            return result;
        }

        public OASISResult<IEnumerable<T3>> ListUninstalled(Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<T3>> result = Data.LoadHolonsForParent<T3>(avatarId, STARNETHolonInstalledHolonType, false, false, 0, true, false, 0, HolonType.All, 0, providerType);

            if (result != null && !result.IsError && result.Result != null)
                result.Result = result.Result.Where(x => x.UninstalledOn != DateTime.MinValue);
            else
                OASISErrorHandling.HandleError(ref result, $"Error occured in STARNETManagerBase.ListUninstalled. Reason:  Error occured calling LoadHolonsForParent. Reason: {result.Message}");

            return result;
        }

        public virtual async Task<OASISResult<IEnumerable<T1>>> ListUnpublishedAsync(Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<T1>> result = new OASISResult<IEnumerable<T1>>();
            string errorMessage = "Error occured in STARNETManagerBase.ListUnpublishedAsync. Reason: ";
            result = await Data.LoadHolonsForParentAsync<T1>(avatarId, STARNETHolonType, false, false, 0, true, false, 0, HolonType.All, 0, providerType);

            if (result != null && !result.IsError && result.Result != null)
                result.Result = result.Result.Where(x => x.STARNETDNA.PublishedOn == DateTime.MinValue && x.STARNETDNA.FileSize > 0);
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonsForParentAsync. Reason: {result.Message}");

            return result;
        }

        public OASISResult<IEnumerable<T1>> ListUnpublished(Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<T1>> result = new OASISResult<IEnumerable<T1>>();
            string errorMessage = "Error occured in STARNETManagerBase.ListUnpublished. Reason: ";
            result = Data.LoadHolonsForParent<T1>(avatarId, STARNETHolonType, false, false, 0, true, false, 0, HolonType.All, 0, providerType);

            if (result != null && !result.IsError && result.Result != null)
                result.Result = result.Result.Where(x => x.STARNETDNA.PublishedOn == DateTime.MinValue && x.STARNETDNA.FileSize > 0);
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonsForParent. Reason: {result.Message}");

            return result;
        }

        public virtual async Task<OASISResult<IEnumerable<T1>>> ListDeactivatedAsync(Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<T1>> result = new OASISResult<IEnumerable<T1>>();
            string errorMessage = "Error occured in STARNETManagerBase.ListDeactivatedAsync. Reason: ";
            result = await Data.LoadHolonsByMetaDataAsync<T1>("Active", "0", STARNETHolonType, true, true, 0, true, false, 0, HolonType.All, 0, providerType);
            return result;
        }

        public OASISResult<IEnumerable<T1>> ListDeactivated(Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<T1>> result = new OASISResult<IEnumerable<T1>>();
            string errorMessage = "Error occured in STARNETManagerBase.ListDeactivated. Reason: ";
            result = Data.LoadHolonsByMetaData<T1>("Active", "0", STARNETHolonType, true, true, 0, true, false, 0, HolonType.All, 0, providerType);
            return result;
        }

        public virtual async Task<OASISResult<bool>> IsInstalledAsync(Guid avatarId, Guid STARNETHolonId, int versionSequence, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<bool> result = new OASISResult<bool>();
            string errorMessage = "Error occured in STARNETManagerBase.IsInstalledAsync. Reason: ";

            OASISResult<T3> installedSTARNETHolonsResult = await Data.LoadHolonByMetaDataAsync<T3>(new Dictionary<string, string>()
            {
                { STARNETHolonIdName, STARNETHolonId.ToString() },
                { "VersionSequence", versionSequence.ToString() },
                { "Active", "1" }

            }, MetaKeyValuePairMatchMode.All, STARNETHolonInstalledHolonType, true, true, 0, true, 0, false, HolonType.All, providerType);

            if (installedSTARNETHolonsResult != null && !installedSTARNETHolonsResult.IsError)
            {
                if (installedSTARNETHolonsResult.Result != null)
                    result.Result = true;
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonByMetaDataAsync. Reason: {installedSTARNETHolonsResult.Message}");

            return result;
        }

        public OASISResult<bool> IsInstalled(Guid avatarId, Guid STARNETHolonId, int versionSequence, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<bool> result = new OASISResult<bool>();
            string errorMessage = "Error occured in STARNETManagerBase.IsInstalled. Reason: ";

            OASISResult<T3> installedSTARNETHolonsResult = Data.LoadHolonByMetaData<T3>(new Dictionary<string, string>()
            {
                { STARNETHolonIdName, STARNETHolonId.ToString() },
                { "VersionSequence", versionSequence.ToString() },
                { "Active", "1" }

            }, MetaKeyValuePairMatchMode.All, STARNETHolonInstalledHolonType, true, true, 0, true, false, HolonType.All, 0, providerType);

            if (installedSTARNETHolonsResult != null && !installedSTARNETHolonsResult.IsError && installedSTARNETHolonsResult.Result != null)
                result.Result = true;
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonByMetaDataAsync. Reason: {installedSTARNETHolonsResult.Message}");

            return result;
        }

        public virtual async Task<OASISResult<bool>> IsInstalledAsync(Guid avatarId, Guid STARNETHolonId, string version, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<bool> result = new OASISResult<bool>();
            string errorMessage = "Error occured in STARNETManagerBase.IsInstalledAsync. Reason: ";

            OASISResult<T3> installedSTARNETHolonsResult = await Data.LoadHolonByMetaDataAsync<T3>(new Dictionary<string, string>()
            {
                { STARNETHolonIdName, STARNETHolonId.ToString() },
                { "Version", version },
                { "Active", "1" }

            }, MetaKeyValuePairMatchMode.All, STARNETHolonInstalledHolonType, true, true, 0, true, 0, false, HolonType.All, providerType);

            if (installedSTARNETHolonsResult != null && !installedSTARNETHolonsResult.IsError)
            {
                if (installedSTARNETHolonsResult.Result != null)
                    result.Result = true;
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonByMetaDataAsync. Reason: {installedSTARNETHolonsResult.Message}");

            return result;
        }

        public OASISResult<bool> IsInstalled(Guid avatarId, Guid STARNETHolonId, string version, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<bool> result = new OASISResult<bool>();
            string errorMessage = "Error occured in STARNETManagerBase.IsInstalled. Reason: ";

            OASISResult<T3> installedSTARNETHolonsResult = Data.LoadHolonByMetaData<T3>(new Dictionary<string, string>()
            {
                { STARNETHolonIdName, STARNETHolonId.ToString() },
                { "Version", version },
                { "Active", "1" }

            }, MetaKeyValuePairMatchMode.All, STARNETHolonInstalledHolonType, true, true, 0, true, false, HolonType.All, 0, providerType);

            if (installedSTARNETHolonsResult != null && !installedSTARNETHolonsResult.IsError)
            {
                if (installedSTARNETHolonsResult.Result != null)
                    result.Result = true;
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonByMetaData. Reason: {installedSTARNETHolonsResult.Message}");

            return result;
        }

        public virtual async Task<OASISResult<bool>> IsInstalledAsync(Guid avatarId, string name, int versionSequence, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<bool> result = new OASISResult<bool>();
            string errorMessage = "Error occured in STARNETManagerBase.IsInstalledAsync. Reason: ";

            OASISResult<T3> installedSTARNETHolonsResult = await Data.LoadHolonByMetaDataAsync<T3>(new Dictionary<string, string>()
            {
                { STARNETHolonNameName, name},
                { "VersionSequence", versionSequence.ToString() },
                { "Active", "1" }

            }, MetaKeyValuePairMatchMode.All, STARNETHolonInstalledHolonType, true, true, 0, true, 0, false, HolonType.All, providerType);

            if (installedSTARNETHolonsResult != null && !installedSTARNETHolonsResult.IsError)
            {
                if (installedSTARNETHolonsResult.Result != null)
                    result.Result = true;
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonByMetaDataAsync. Reason: {installedSTARNETHolonsResult.Message}");

            return result;
        }

        public OASISResult<bool> IsInstalled(Guid avatarId, string name, int versionSequence, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<bool> result = new OASISResult<bool>();
            string errorMessage = "Error occured in STARNETManagerBase.IsInstalled. Reason: ";

            OASISResult<T3> installedSTARNETHolonsResult = Data.LoadHolonByMetaData<T3>(new Dictionary<string, string>()
            {
                { STARNETHolonNameName, name },
                { "VersionSequence", versionSequence.ToString() },
                { "Active", "1" }

            }, MetaKeyValuePairMatchMode.All, STARNETHolonInstalledHolonType, true, true, 0, true, false, HolonType.All, 0, providerType);

            if (installedSTARNETHolonsResult != null && !installedSTARNETHolonsResult.IsError)
            {
                if (installedSTARNETHolonsResult.Result != null)
                    result.Result = true;
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonByMetaData. Reason: {installedSTARNETHolonsResult.Message}");

            return result;
        }

        public virtual async Task<OASISResult<bool>> IsInstalledAsync(Guid avatarId, string name, string version, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<bool> result = new OASISResult<bool>();
            string errorMessage = "Error occured in STARNETManagerBase.IsInstalledAsync. Reason: ";

            OASISResult<T3> installedSTARNETHolonsResult = await Data.LoadHolonByMetaDataAsync<T3>(new Dictionary<string, string>()
            {
                { STARNETHolonNameName, name},
                { "Version", version.ToString() },
                { "Active", "1" }

            }, MetaKeyValuePairMatchMode.All, STARNETHolonInstalledHolonType, true, true, 0, true, 0, false, HolonType.All, providerType);

            if (installedSTARNETHolonsResult != null && !installedSTARNETHolonsResult.IsError)
            {
                if (installedSTARNETHolonsResult.Result != null)
                    result.Result = true;
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonByMetaDataAsync. Reason: {installedSTARNETHolonsResult.Message}");

            return result;
        }

        public OASISResult<bool> IsInstalled(Guid avatarId, string name, string version, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<bool> result = new OASISResult<bool>();
            string errorMessage = "Error occured in STARNETManagerBase.IsInstalled. Reason: ";

            OASISResult<T3> installedSTARNETHolonsResult = Data.LoadHolonByMetaData<T3>(new Dictionary<string, string>()
            {
                { STARNETHolonNameName, name },
                { "Version", version },
                { "Active", "1" }

            }, MetaKeyValuePairMatchMode.All, STARNETHolonInstalledHolonType, true, true, 0, true, false, HolonType.All, 0, providerType);

            if (installedSTARNETHolonsResult != null && !installedSTARNETHolonsResult.IsError)
            {
                if (installedSTARNETHolonsResult.Result != null)
                    result.Result = true;
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonByMetaData. Reason: {installedSTARNETHolonsResult.Message}");

            return result;
        }

        public virtual async Task<OASISResult<bool>> IsPublishedAsync(Guid avatarId, Guid STARNETHolonId, int versionSequence, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<bool> result = new OASISResult<bool>();
            string errorMessage = "Error occured in STARNETManagerBase.IsPublishedAsync. Reason: ";

            OASISResult<T3> loadSTARNETHolonsResult = await Data.LoadHolonByMetaDataAsync<T3>(new Dictionary<string, string>()
            {
                { STARNETHolonIdName, STARNETHolonId.ToString() },
                { "VersionSequence", versionSequence.ToString() },
                { "Active", "1" }

            }, MetaKeyValuePairMatchMode.All, STARNETHolonType, true, true, 0, true, 0, false, HolonType.All, providerType);

            if (loadSTARNETHolonsResult != null && !loadSTARNETHolonsResult.IsError && loadSTARNETHolonsResult.Result != null)
            {
                if (loadSTARNETHolonsResult.Result.STARNETDNA.PublishedOn != DateTime.MinValue)
                    result.Result = true;
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonByMetaDataAsync. Reason: {loadSTARNETHolonsResult.Message}");

            return result;
        }

        public OASISResult<bool> IsPublished(Guid avatarId, Guid STARNETHolonId, int versionSequence, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<bool> result = new OASISResult<bool>();
            string errorMessage = "Error occured in STARNETManagerBase.IsPublished. Reason: ";

            OASISResult<T3> loadSTARNETHolonsResult = Data.LoadHolonByMetaData<T3>(new Dictionary<string, string>()
            {
                { STARNETHolonIdName, STARNETHolonId.ToString() },
                { "VersionSequence", versionSequence.ToString() },
                { "Active", "1" }

            }, MetaKeyValuePairMatchMode.All, STARNETHolonType, true, true, 0, true, false, HolonType.All, 0, providerType);

            if (loadSTARNETHolonsResult != null && !loadSTARNETHolonsResult.IsError && loadSTARNETHolonsResult.Result != null)
            {
                if (loadSTARNETHolonsResult.Result.STARNETDNA.PublishedOn != DateTime.MinValue)
                    result.Result = true;
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonByMetaDataAsync. Reason: {loadSTARNETHolonsResult.Message}");

            return result;
        }

        public virtual async Task<OASISResult<bool>> IsPublishedAsync(Guid avatarId, Guid STARNETHolonId, string version, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<bool> result = new OASISResult<bool>();
            string errorMessage = "Error occured in STARNETManagerBase.IsPublishedAsync. Reason: ";

            OASISResult<T3> loadSTARNETHolonsResult = await Data.LoadHolonByMetaDataAsync<T3>(new Dictionary<string, string>()
            {
                { STARNETHolonIdName, STARNETHolonId.ToString() },
                { "Version", version },
                { "Active", "1" }

            }, MetaKeyValuePairMatchMode.All, STARNETHolonType, true, true, 0, true, 0, false, HolonType.All, providerType);

            if (loadSTARNETHolonsResult != null && !loadSTARNETHolonsResult.IsError && loadSTARNETHolonsResult.Result != null)
            {
                if (loadSTARNETHolonsResult.Result.STARNETDNA.PublishedOn != DateTime.MinValue)
                    result.Result = true;
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonByMetaDataAsync. Reason: {loadSTARNETHolonsResult.Message}");

            return result;
        }

        public OASISResult<bool> IsPublished(Guid avatarId, Guid STARNETHolonId, string version, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<bool> result = new OASISResult<bool>();
            string errorMessage = "Error occured in STARNETManagerBase.IsPublished. Reason: ";

            OASISResult<T3> loadSTARNETHolonsResult = Data.LoadHolonByMetaData<T3>(new Dictionary<string, string>()
            {
                { STARNETHolonIdName, STARNETHolonId.ToString() },
                { "Version", version },
                { "Active", "1" }

            }, MetaKeyValuePairMatchMode.All, STARNETHolonType, true, true, 0, true, false, HolonType.All, 0, providerType);

            if (loadSTARNETHolonsResult != null && !loadSTARNETHolonsResult.IsError && loadSTARNETHolonsResult.Result != null)
            {
                if (loadSTARNETHolonsResult.Result.STARNETDNA.PublishedOn != DateTime.MinValue)
                    result.Result = true;
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonByMetaData. Reason: {loadSTARNETHolonsResult.Message}");

            return result;
        }

        public virtual async Task<OASISResult<bool>> IsPublishedAsync(Guid avatarId, string name, int versionSequence, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<bool> result = new OASISResult<bool>();
            string errorMessage = "Error occured in STARNETManagerBase.IsPublishedAsync. Reason: ";

            OASISResult<T3> loadSTARNETHolonsResult = await Data.LoadHolonByMetaDataAsync<T3>(new Dictionary<string, string>()
            {
                { STARNETHolonNameName, name},
                { "VersionSequence", versionSequence.ToString() },
                { "Active", "1" }

            }, MetaKeyValuePairMatchMode.All, STARNETHolonType, true, true, 0, true, 0, false, HolonType.All, providerType);

            if (loadSTARNETHolonsResult != null && !loadSTARNETHolonsResult.IsError && loadSTARNETHolonsResult.Result != null)
            {
                if (loadSTARNETHolonsResult.Result.STARNETDNA.PublishedOn != DateTime.MinValue)
                    result.Result = true;
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonByMetaDataAsync. Reason: {loadSTARNETHolonsResult.Message}");

            return result;
        }

        public OASISResult<bool> IsPublished(Guid avatarId, string name, int versionSequence, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<bool> result = new OASISResult<bool>();
            string errorMessage = "Error occured in STARNETManagerBase.IsPublished. Reason: ";

            OASISResult<T3> loadSTARNETHolonsResult = Data.LoadHolonByMetaData<T3>(new Dictionary<string, string>()
            {
                { STARNETHolonNameName, name },
                { "VersionSequence", versionSequence.ToString() },
                { "Active", "1" }

            }, MetaKeyValuePairMatchMode.All, STARNETHolonType, true, true, 0, true, false, HolonType.All, 0, providerType);

            if (loadSTARNETHolonsResult != null && !loadSTARNETHolonsResult.IsError && loadSTARNETHolonsResult.Result != null)
            {
                if (loadSTARNETHolonsResult.Result.STARNETDNA.PublishedOn != DateTime.MinValue)
                    result.Result = true;
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonByMetaData. Reason: {loadSTARNETHolonsResult.Message}");

            return result;
        }

        public virtual async Task<OASISResult<bool>> IsPublishedAsync(Guid avatarId, string name, string version, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<bool> result = new OASISResult<bool>();
            string errorMessage = "Error occured in STARNETManagerBase.IsPublishedAsync. Reason: ";

            OASISResult<T3> loadSTARNETHolonsResult = await Data.LoadHolonByMetaDataAsync<T3>(new Dictionary<string, string>()
            {
                { STARNETHolonNameName, name},
                { "Version", version.ToString() },
                { "Active", "1" }

            }, MetaKeyValuePairMatchMode.All, STARNETHolonType, true, true, 0, true, 0, false, HolonType.All, providerType);

            if (loadSTARNETHolonsResult != null && !loadSTARNETHolonsResult.IsError && loadSTARNETHolonsResult.Result != null)
            {
                if (loadSTARNETHolonsResult.Result.STARNETDNA.PublishedOn != DateTime.MinValue)
                    result.Result = true;
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonByMetaDataAsync. Reason: {loadSTARNETHolonsResult.Message}");

            return result;
        }

        public OASISResult<bool> IsPublished(Guid avatarId, string name, string version, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<bool> result = new OASISResult<bool>();
            string errorMessage = "Error occured in STARNETManagerBase.IsPublished. Reason: ";

            OASISResult<T3> loadSTARNETHolonsResult = Data.LoadHolonByMetaData<T3>(new Dictionary<string, string>()
            {
                { STARNETHolonNameName, name },
                { "Version", version },
                { "Active", "1" }

            }, MetaKeyValuePairMatchMode.All, STARNETHolonType, true, true, 0, true, false, HolonType.All, 0, providerType);

            if (loadSTARNETHolonsResult != null && !loadSTARNETHolonsResult.IsError && loadSTARNETHolonsResult.Result != null)
            {
                if (loadSTARNETHolonsResult.Result.STARNETDNA.PublishedOn != DateTime.MinValue)
                    result.Result = true;
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonByMetaData. Reason: {loadSTARNETHolonsResult.Message}");

            return result;
        }

        public virtual async Task<OASISResult<T3>> LoadInstalledAsync(Guid avatarId, Guid STARNETHolonId, int versionSequence, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T3> result = new OASISResult<T3>();
            string errorMessage = "Error occured in STARNETManagerBase.LoadInstalledAsync. Reason: ";
            OASISResult<T3> installedSTARNETHolonsResult = await Data.LoadHolonByMetaDataAsync<T3>(new Dictionary<string, string>()
            {
                { STARNETHolonIdName, STARNETHolonId.ToString() },
                { "VersionSequence", versionSequence.ToString() }

            }, MetaKeyValuePairMatchMode.All, STARNETHolonInstalledHolonType, true, true, 0, true, 0, false, HolonType.All, providerType);

            if (installedSTARNETHolonsResult != null && !installedSTARNETHolonsResult.IsError && installedSTARNETHolonsResult.Result != null)
                result.Result = installedSTARNETHolonsResult.Result;
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonByMetaDataAsync. Reason: {installedSTARNETHolonsResult.Message}");

            return result;
        }

        public OASISResult<T3> LoadInstalled(Guid avatarId, Guid STARNETHolonId, int versionSequence, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T3> result = new OASISResult<T3>();
            string errorMessage = "Error occured in STARNETManagerBase.LoadInstalled. Reason: ";
            OASISResult<T3> installedSTARNETHolonsResult = Data.LoadHolonByMetaData<T3>(new Dictionary<string, string>()
            {
                { STARNETHolonIdName, STARNETHolonId.ToString() },
                { "VersionSequence", versionSequence.ToString() }

            }, MetaKeyValuePairMatchMode.All, STARNETHolonInstalledHolonType, version: versionSequence, providerType: providerType);

            if (installedSTARNETHolonsResult != null && !installedSTARNETHolonsResult.IsError && installedSTARNETHolonsResult.Result != null)
                result.Result = installedSTARNETHolonsResult.Result;
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonByMetaDataAsync. Reason: {installedSTARNETHolonsResult.Message}");

            return result;
        }

        public virtual async Task<OASISResult<T3>> LoadInstalledAsync(Guid avatarId, string name, int versionSequence, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T3> result = new OASISResult<T3>();
            string errorMessage = "Error occured in STARNETManagerBase.LoadInstalledAsync. Reason: ";
            OASISResult<T3> installedSTARNETHolonsResult = await Data.LoadHolonByMetaDataAsync<T3>(new Dictionary<string, string>()
            {
                { STARNETHolonNameName, name },
                { "VersionSequence", versionSequence.ToString() }

            }, MetaKeyValuePairMatchMode.All, STARNETHolonInstalledHolonType, true, true, 0, true, 0, false, HolonType.All, providerType);

            if (installedSTARNETHolonsResult != null && !installedSTARNETHolonsResult.IsError && installedSTARNETHolonsResult.Result != null)
                result.Result = installedSTARNETHolonsResult.Result;
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonByMetaDataAsync. Reason: {installedSTARNETHolonsResult.Message}");

            return result;
        }

        public OASISResult<T3> LoadInstalled(Guid avatarId, string name, int versionSequence, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T3> result = new OASISResult<T3>();
            string errorMessage = "Error occured in STARNETManagerBase.LoadInstalled. Reason: ";
            OASISResult<T3> installedSTARNETHolonsResult = Data.LoadHolonByMetaData<T3>(new Dictionary<string, string>()
            {
                { STARNETHolonNameName, name },
                { "VersionSequence", versionSequence.ToString() }

            }, MetaKeyValuePairMatchMode.All, STARNETHolonInstalledHolonType, version: versionSequence, providerType: providerType);

            if (installedSTARNETHolonsResult != null && !installedSTARNETHolonsResult.IsError && installedSTARNETHolonsResult.Result != null)
                result.Result = installedSTARNETHolonsResult.Result;
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonByMetaDataAsync. Reason: {installedSTARNETHolonsResult.Message}");

            return result;
        }

        public virtual async Task<OASISResult<T3>> LoadInstalledAsync(Guid avatarId, Guid STARNETHolonId, string version, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T3> result = new OASISResult<T3>();
            string errorMessage = "Error occured in STARNETManagerBase.LoadInstalledAsync. Reason: ";
            OASISResult<T3> installedSTARNETHolonsResult = await Data.LoadHolonByMetaDataAsync<T3>(new Dictionary<string, string>()
            {
                { STARNETHolonIdName, STARNETHolonId.ToString() },
                { "Version", version }

            }, MetaKeyValuePairMatchMode.All, STARNETHolonInstalledHolonType, true, true, 0, true, 0, false, HolonType.All, providerType);

            if (installedSTARNETHolonsResult != null && !installedSTARNETHolonsResult.IsError && installedSTARNETHolonsResult.Result != null)
                result.Result = installedSTARNETHolonsResult.Result;
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonByMetaDataAsync. Reason: {installedSTARNETHolonsResult.Message}");

            return result;
        }

        public OASISResult<T3> LoadInstalled(Guid avatarId, Guid STARNETHolonId, string version, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T3> result = new OASISResult<T3>();
            string errorMessage = "Error occured in STARNETManagerBase.LoadInstalled. Reason: ";
            OASISResult<T3> installedSTARNETHolonsResult = Data.LoadHolonByMetaData<T3>(new Dictionary<string, string>()
            {
                { STARNETHolonIdName, STARNETHolonId.ToString() },
                { "Version", version }

            }, MetaKeyValuePairMatchMode.All, STARNETHolonInstalledHolonType, providerType: providerType);

            if (installedSTARNETHolonsResult != null && !installedSTARNETHolonsResult.IsError && installedSTARNETHolonsResult.Result != null)
                result.Result = installedSTARNETHolonsResult.Result;
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonByMetaDataAsync. Reason: {installedSTARNETHolonsResult.Message}");

            return result;
        }

        public virtual async Task<OASISResult<T3>> LoadInstalledAsync(Guid avatarId, string name, string version, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T3> result = new OASISResult<T3>();
            string errorMessage = "Error occured in STARNETManagerBase.LoadInstalledAsync. Reason: ";
            OASISResult<T3> installedSTARNETHolonsResult = await Data.LoadHolonByMetaDataAsync<T3>(new Dictionary<string, string>()
            {
                { STARNETHolonNameName, name },
                { "Version", version }

            }, MetaKeyValuePairMatchMode.All, STARNETHolonInstalledHolonType, true, true, 0, true, 0, false, HolonType.All, providerType);

            if (installedSTARNETHolonsResult != null && !installedSTARNETHolonsResult.IsError && installedSTARNETHolonsResult.Result != null)
                result.Result = installedSTARNETHolonsResult.Result;
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonByMetaDataAsync. Reason: {installedSTARNETHolonsResult.Message}");

            return result;
        }

        public OASISResult<T3> LoadInstalled(Guid avatarId, string name, string version, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T3> result = new OASISResult<T3>();
            string errorMessage = "Error occured in STARNETManagerBase.LoadInstalled. Reason: ";
            OASISResult<T3> installedSTARNETHolonsResult = Data.LoadHolonByMetaData<T3>(new Dictionary<string, string>()
            {
                { STARNETHolonNameName, name },
                { "Version", version }

            }, MetaKeyValuePairMatchMode.All, STARNETHolonInstalledHolonType, providerType: providerType);

            if (installedSTARNETHolonsResult != null && !installedSTARNETHolonsResult.IsError && installedSTARNETHolonsResult.Result != null)
                result.Result = installedSTARNETHolonsResult.Result;
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonByMetaDataAsync. Reason: {installedSTARNETHolonsResult.Message}");

            return result;
        }

        public virtual async Task<OASISResult<T3>> LoadInstalledAsync(Guid avatarId, Guid STARNETHolonId, bool active, int versionSequence, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T3> result = new OASISResult<T3>();
            string errorMessage = "Error occured in STARNETManagerBase.LoadInstalledAsync. Reason: ";
            OASISResult<T3> installedSTARNETHolonsResult = await Data.LoadHolonByMetaDataAsync<T3>(new Dictionary<string, string>()
            {
                { STARNETHolonIdName, STARNETHolonId.ToString() },
                { "VersionSequence", versionSequence.ToString() },
                { "Active", active == true ? "1" : "0" }

            }, MetaKeyValuePairMatchMode.All, STARNETHolonInstalledHolonType, true, true, 0, true, 0, false, HolonType.All, providerType);

            if (installedSTARNETHolonsResult != null && !installedSTARNETHolonsResult.IsError && installedSTARNETHolonsResult.Result != null)
                result.Result = installedSTARNETHolonsResult.Result;
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonByMetaDataAsync. Reason: {installedSTARNETHolonsResult.Message}");

            return result;
        }

        public OASISResult<T3> LoadInstalled(Guid avatarId, Guid STARNETHolonId, bool active, int versionSequence, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T3> result = new OASISResult<T3>();
            string errorMessage = "Error occured in STARNETManagerBase.LoadInstalled. Reason: ";
            OASISResult<T3> installedSTARNETHolonsResult = Data.LoadHolonByMetaData<T3>(new Dictionary<string, string>()
            {
                { STARNETHolonIdName, STARNETHolonId.ToString() },
                { "VersionSequence", versionSequence.ToString() },
                { "Active", active == true ? "1" : "0" }

            }, MetaKeyValuePairMatchMode.All, STARNETHolonInstalledHolonType, true, true, 0, true, false, HolonType.All, 0, providerType);

            if (installedSTARNETHolonsResult != null && !installedSTARNETHolonsResult.IsError && installedSTARNETHolonsResult.Result != null)
                result.Result = installedSTARNETHolonsResult.Result;
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonByMetaData. Reason: {installedSTARNETHolonsResult.Message}");

            return result;
        }

        public virtual async Task<OASISResult<T3>> LoadInstalledAsync(Guid avatarId, string name, bool active, int versionSequence, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T3> result = new OASISResult<T3>();
            string errorMessage = "Error occured in STARNETManagerBase.LoadInstalledAsync. Reason: ";
            OASISResult<T3> installedSTARNETHolonsResult = await Data.LoadHolonByMetaDataAsync<T3>(new Dictionary<string, string>()
            {
                { STARNETHolonNameName, name },
                { "VersionSequence", versionSequence.ToString() },
                { "Active", active == true ? "1" : "0" }

            }, MetaKeyValuePairMatchMode.All, STARNETHolonInstalledHolonType, true, true, 0, true, 0, false, HolonType.All, providerType);
            if (installedSTARNETHolonsResult != null && !installedSTARNETHolonsResult.IsError && installedSTARNETHolonsResult.Result != null)
                result.Result = installedSTARNETHolonsResult.Result;
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonByMetaDataAsync. Reason: {installedSTARNETHolonsResult.Message}");

            return result;
        }

        public OASISResult<T3> LoadInstalled(Guid avatarId, string name, bool active, int versionSequence, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T3> result = new OASISResult<T3>();
            string errorMessage = "Error occured in STARNETManagerBase.LoadInstalled. Reason: ";
            OASISResult<T3> installedSTARNETHolonsResult = Data.LoadHolonByMetaData<T3>(new Dictionary<string, string>()
            {
                { STARNETHolonNameName, name },
                { "VersionSequence", versionSequence.ToString() },
                { "Active", active == true ? "1" : "0" }

            }, MetaKeyValuePairMatchMode.All, STARNETHolonInstalledHolonType, true, true, 0, true, false, HolonType.All, 0, providerType);

            if (installedSTARNETHolonsResult != null && !installedSTARNETHolonsResult.IsError && installedSTARNETHolonsResult.Result != null)
                result.Result = installedSTARNETHolonsResult.Result;
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonByMetaData. Reason: {installedSTARNETHolonsResult.Message}");

            return result;
        }

        public virtual async Task<OASISResult<T3>> LoadInstalledAsync(Guid avatarId, Guid STARNETHolonId, string version, bool active, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T3> result = new OASISResult<T3>();
            string errorMessage = "Error occured in STARNETManagerBase.LoadInstalledAsync. Reason: ";
            OASISResult<T3> installedSTARNETHolonsResult = await Data.LoadHolonByMetaDataAsync<T3>(new Dictionary<string, string>()
            {
                { STARNETHolonIdName, STARNETHolonId.ToString() },
                { "Version", version},
                { "Active", active == true ? "1" : "0" }

            }, MetaKeyValuePairMatchMode.All, STARNETHolonInstalledHolonType, true, true, 0, true, 0, false, HolonType.All, providerType);

            if (installedSTARNETHolonsResult != null && !installedSTARNETHolonsResult.IsError && installedSTARNETHolonsResult.Result != null)
                result.Result = installedSTARNETHolonsResult.Result;
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonByMetaDataAsync. Reason: {installedSTARNETHolonsResult.Message}");

            return result;
        }

        public OASISResult<T3> LoadInstalled(Guid avatarId, Guid STARNETHolonId, string version, bool active, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T3> result = new OASISResult<T3>();
            string errorMessage = "Error occured in STARNETManagerBase.LoadInstalled. Reason: ";
            OASISResult<T3> installedSTARNETHolonsResult = Data.LoadHolonByMetaData<T3>(new Dictionary<string, string>()
            {
                { STARNETHolonIdName, STARNETHolonId.ToString() },
                { "Version", version },
                { "Active", active == true ? "1" : "0" }

            }, MetaKeyValuePairMatchMode.All, STARNETHolonInstalledHolonType, true, true, 0, true, false, HolonType.All, 0, providerType);

            if (installedSTARNETHolonsResult != null && !installedSTARNETHolonsResult.IsError && installedSTARNETHolonsResult.Result != null)
                result.Result = installedSTARNETHolonsResult.Result;
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonByMetaData. Reason: {installedSTARNETHolonsResult.Message}");

            return result;
        }

        public virtual async Task<OASISResult<T3>> LoadInstalledAsync(Guid avatarId, string STARNETHolonName, string version, bool active, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T3> result = new OASISResult<T3>();
            string errorMessage = "Error occured in STARNETManagerBase.LoadInstalledAsync. Reason: ";
            OASISResult<T3> installedSTARNETHolonsResult = await Data.LoadHolonByMetaDataAsync<T3>(new Dictionary<string, string>()
            {
                { STARNETHolonNameName, STARNETHolonName },
                { "Version", version },
                { "Active", active == true ? "1" : "0" }

            }, MetaKeyValuePairMatchMode.All, STARNETHolonInstalledHolonType, true, true, 0, true, 0, false, HolonType.All, providerType);

            if (installedSTARNETHolonsResult != null && !installedSTARNETHolonsResult.IsError && installedSTARNETHolonsResult.Result != null)
                result.Result = installedSTARNETHolonsResult.Result;
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonByMetaDataAsync. Reason: {installedSTARNETHolonsResult.Message}");

            return result;
        }

        public OASISResult<T3> LoadInstalled(Guid avatarId, string STARNETHolonName, string version, bool active, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T3> result = new OASISResult<T3>();
            string errorMessage = "Error occured in STARNETManagerBase.LoadInstalled. Reason: ";
            OASISResult<T3> installedSTARNETHolonsResult = Data.LoadHolonByMetaData<T3>(new Dictionary<string, string>()
            {
                { STARNETHolonNameName, STARNETHolonName },
                { "Version", version },
                { "Active", active == true ? "1" : "0" }

            }, MetaKeyValuePairMatchMode.All, STARNETHolonInstalledHolonType, true, true, 0, true, false, HolonType.All, 0, providerType);

            if (installedSTARNETHolonsResult != null && !installedSTARNETHolonsResult.IsError && installedSTARNETHolonsResult.Result != null)
                result.Result = installedSTARNETHolonsResult.Result;
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonByMetaData. Reason: {installedSTARNETHolonsResult.Message}");

            return result;
        }

        public OASISResult<T3> OpenSTARNETHolonFolder(Guid avatarId, T3 holon)
        {
            OASISResult<T3> result = new OASISResult<T3>();
            string errorMessage = "An error occured in STARNETManagerBase.OpenSTARNETHolonFolder. Reason:";

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
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} The {STARNETHolonUIName} is null!");

            return result;
        }

        public virtual async Task<OASISResult<T3>> OpenSTARNETHolonFolderAsync(Guid avatarId, Guid STARNETHolonId, int versionSequence = 0, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T3> result = new OASISResult<T3>();
            string errorMessage = "An error occured in STARNETManagerBase.OpenSTARNETHolonFolderAsync. Reason:";
            result = await LoadInstalledAsync(avatarId, STARNETHolonId, versionSequence, providerType);

            if (result != null && !result.IsError && result.Result != null)
                OpenSTARNETHolonFolder(avatarId, result.Result);
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the {STARNETHolonUIName} with the LoadInstalledSTARNETHolonAsync method, reason: {result.Message}");

            return result;
        }

        public OASISResult<T3> OpenSTARNETHolonFolder(Guid avatarId, Guid STARNETHolonId, int versionSequence = 0, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T3> result = new OASISResult<T3>();
            string errorMessage = "An error occured in STARNETManagerBase.OpenSTARNETHolonFolder. Reason:";
            result = LoadInstalled(avatarId, STARNETHolonId, versionSequence);

            if (result != null && !result.IsError && result.Result != null)
                OpenSTARNETHolonFolder(avatarId, result.Result);
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the {STARNETHolonUIName} with the LoadInstalledSTARNETHolon method, reason: {result.Message}");

            return result;
        }

        public virtual async Task<OASISResult<T3>> OpenSTARNETHolonFolderAsync(Guid avatarId, Guid STARNETHolonId, string version, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T3> result = new OASISResult<T3>();
            string errorMessage = "An error occured in STARNETManagerBase.OpenSTARNETHolonFolderAsync. Reason:";
            result = await LoadInstalledAsync(avatarId, STARNETHolonId, version, providerType);

            if (result != null && !result.IsError && result.Result != null)
                OpenSTARNETHolonFolder(avatarId, result.Result);
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the {STARNETHolonUIName} with the LoadInstalledSTARNETHolonAsync method, reason: {result.Message}");

            return result;
        }

        public OASISResult<T3> OpenSTARNETHolonFolder(Guid avatarId, Guid STARNETHolonId, string version, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T3> result = new OASISResult<T3>();
            string errorMessage = "An error occured in STARNETManagerBase.OpenSTARNETHolonFolder. Reason:";
            result = LoadInstalled(avatarId, STARNETHolonId, version, providerType);

            if (result != null && !result.IsError && result.Result != null)
                OpenSTARNETHolonFolder(avatarId, result.Result);
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the {STARNETHolonUIName} with the LoadInstalledSTARNETHolon method, reason: {result.Message}");

            return result;
        }


        //private T4 ConvertSTARNETHolonToSTARNETDNA(ISTARNETHolon T)
        //{
        //    STARNETDNA STARNETDNA = new STARNETDNA()
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
        //        STARNETHolonId = T.Id,
        //        STARNETHolonName = T.Name,
        //        STARNETHolonType = T.STARNETHolonType,
        //        PublishedByAvatarId = T.PublishedByAvatarId,
        //        PublishedByAvatarUsername = T.PublishedByAvatarUsername,
        //        PublishedOn = T.PublishedOn,
        //        PublishedOnSTARNET = T.PublishedSTARNETHolon != null,
        //        Version = T.Version.ToString()
        //    };

        //    List<IZome> zomes = new List<IZome>();
        //    foreach (IHolon holon in T.Children)
        //        zomes.Remove((IZome)holon);

        //   //STARNETDNA.Zomes = zomes;
        //    return STARNETDNA;
        //}

        public virtual async Task<OASISResult<bool>> WriteDNAAsync<T>(T STARNETDNA, string fullPathToSTARNETHolon) //where T : ISTARNETDNA
        {
            OASISResult<bool> result = new OASISResult<bool>();

            try
            {
                //JsonSerializerOptions options = new()
                //{
                //    ReferenceHandler = ReferenceHandler.Preserve,
                //    WriteIndented = true
                //};

                if (!Directory.Exists(fullPathToSTARNETHolon))
                    Directory.CreateDirectory(fullPathToSTARNETHolon);

                OAPPDNA OAPPDNA = STARNETDNA as OAPPDNA;

                //Temp need to remove CelestialBody parents and cores to prevent infinite recursion when serializing to json.
                //if (OAPPDNA != null)
                //    Data.RemoveCelesialBodies(OAPPDNA.Zomes);

                await File.WriteAllTextAsync(Path.Combine(fullPathToSTARNETHolon, STARNETDNAFileName), JsonConvert.SerializeObject(STARNETDNA, Formatting.Indented));
                //await File.WriteAllTextAsync(Path.Combine(fullPathToSTARNETHolon, string.Concat(Enum.GetName(typeof(HolonType), STARNETDNA.STARNETHolonType), "_", STARNETDNA.Name, "_", "v", STARNETDNA.Version)), JsonConvert.SerializeObject(STARNETDNA, Formatting.Indented));

                //Restore the celestial bodies & cores back onto the zomes.
                //if (OAPPDNA != null)
                //    Data.RestoreCelesialBodies(OAPPDNA.Zomes);

                result.Result = true;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"An error occured writing the {STARNETHolonUIName} DNA in WriteDNAAsync: Reason: {ex.Message}");
            }

            return result;
        }

        public OASISResult<bool> WriteDNA<T>(T STARNETDNA, string fullPathToSTARNETHolon) //where T : ISTARNETDNA
        {
            OASISResult<bool> result = new OASISResult<bool>();

            try
            {
                if (!Directory.Exists(fullPathToSTARNETHolon))
                    Directory.CreateDirectory(fullPathToSTARNETHolon);

                //File.WriteAllText(Path.Combine(fullPathToSTARNETHolon, STARNETDNAFileName), JsonSerializer.Serialize(STARNETDNA));
                File.WriteAllText(Path.Combine(fullPathToSTARNETHolon, STARNETDNAFileName), JsonConvert.SerializeObject(STARNETDNA, Formatting.Indented));
                //File.WriteAllText(Path.Combine(fullPathToSTARNETHolon, string.Concat(Enum.GetName(typeof(HolonType), STARNETDNA.STARNETHolonType), "_", STARNETDNA.Name, "_", "v", STARNETDNA.Version)), JsonConvert.SerializeObject(STARNETDNA, Formatting.Indented));
                result.Result = true;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"An error occured writing the {STARNETHolonUIName} DNA in WriteDNA: Reason: {ex.Message}");
            }

            return result;
        }

        //public virtual async Task<OASISResult<T>> ReadDNAFromSourceOrInstallFolderAsync<T>(T STARNETDNA, string fullPathToSTARNETHolonFolder) where T : ISTARNETDNA
        public virtual async Task<OASISResult<T>> ReadDNAFromSourceOrInstallFolderAsync<T>(string fullPathToSTARNETHolonFolder)
        {
            OASISResult<T> result = new OASISResult<T>();

            try
            {
                //var options = new JsonSerializerOptions
                //{
                //    WriteIndented = true,
                //    Converters = 
                //    {
                //        //new InterfaceConverter<IList<STARNETDependency>, List<STARNETDependency>>(),
                //        //new InterfaceConverter<STARNETDependency, STARNETDependency>(),
                //        //new PolymorphicConverter<STARNETDependency>(),
                //        new STARNETDependencyConvertor()
                //    }
                //};

                result.Result = JsonConvert.DeserializeObject<T>(await File.ReadAllTextAsync(Path.Combine(fullPathToSTARNETHolonFolder, STARNETDNAFileName)));
                //result.Result = JsonConvert.DeserializeObject<T>(await File.ReadAllTextAsync(Path.Combine(fullPathToSTARNETHolonFolder, string.Concat(Enum.GetName(typeof(HolonType), STARNETDNA.STARNETHolonType), "_", STARNETDNA.Name, "_", "v", STARNETDNA.Version))));
                //result.Result = JsonConvert.DeserializeObject<T>(await File.ReadAllTextAsync(fullPathToSTARNETDNA));

                //result.Result = JsonSerializer.Deserialize<T>(await File.ReadAllTextAsync(Path.Combine(fullPathToSTARNETHolonFolder, STARNETDNAFileName)), options);
                //result.Result = JsonSerializer.Deserialize<T>(await File.ReadAllTextAsync(Path.Combine(fullPathToSTARNETHolonFolder, STARNETDNAFileName)));
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"An error occured reading the {STARNETDNAFileName} in the {fullPathToSTARNETHolonFolder} folder in ReadDNAFromSourceOrInstallFolderAsync: Reason: {ex.Message}");
            }

            return result;
        }

        public OASISResult<T> ReadDNAFromSourceOrInstallFolder<T>(string fullPathToSTARNETHolonFolder)
        //public OASISResult<T> ReadDNAFromSourceOrInstallFolder<T>(string fullPathToSTARNETDNA)
        {
            OASISResult<T> result = new OASISResult<T>();

            try
            {
                //result.Result = JsonSerializer.Deserialize<T>(File.ReadAllText(Path.Combine(fullPathToSTARNETHolonFolder, STARNETDNAFileName)));
                //result.Result = JsonConvert.DeserializeObject<T>(File.ReadAllText(fullPathToSTARNETDNA));
                result.Result = JsonConvert.DeserializeObject<T>(File.ReadAllText(Path.Combine(fullPathToSTARNETHolonFolder, STARNETDNAFileName)));
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"An error occured reading the {STARNETDNAFileName} in the {fullPathToSTARNETHolonFolder} folder in ReadDNAFromSourceOrInstallFolder: Reason: {ex.Message}");
            }

            return result;
        }

        public virtual async Task<OASISResult<T>> ReadDNAFromPublishedFileAsync<T>(string fullPathToPublishedFile)
        //public virtual async Task<OASISResult<T>> ReadDNAFromPublishedFileAsync<T>(T STARNETDNA, string fullPathToPublishedFile) where T : ISTARNETDNA
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

                //result.Result = JsonSerializer.Deserialize<T>(await File.ReadAllTextAsync(Path.Combine(tempPath, STARNETDNAFileName)));
                //result.Result = JsonConvert.DeserializeObject<T>(File.ReadAllText(Path.Combine(tempPath, string.Concat(Enum.GetName(typeof(HolonType), STARNETDNA.STARNETHolonType), "_", STARNETDNA.Name, "_", "v", STARNETDNA.Version))));
                result.Result = JsonConvert.DeserializeObject<T>(File.ReadAllText(Path.Combine(tempPath, STARNETDNAFileName)));
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"An error occured reading the {STARNETDNAFileName} in the {fullPathToPublishedFile} file in ReadSTARNETDNAFromPublishedFile: Reason: {e.Message}");
            }
            finally
            {
                if (Directory.Exists(tempPath))
                    Directory.Delete(tempPath, true);
            }

            return result;
        }

        public OASISResult<T> ReadDNAFromPublishedFile<T>(string fullPathToPublishedFile)
        //public OASISResult<T> ReadDNAFromPublishedFile<T>(string fullPathToPublishedFile)
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

                //result.Result = JsonSerializer.Deserialize<T>(File.ReadAllText(Path.Combine(tempPath, STARNETDNAFileName)));
                result.Result = JsonConvert.DeserializeObject<T>(File.ReadAllText(Path.Combine(tempPath, STARNETDNAFileName)));
                //result.Result = JsonConvert.DeserializeObject<T>(File.ReadAllText(Path.Combine(tempPath, string.Concat(Enum.GetName(typeof(HolonType), STARNETDNA.STARNETHolonType), "_", STARNETDNA.Name, "_", "v", STARNETDNA.Version))));
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"An error occured reading the {STARNETDNAFileName} in the {fullPathToPublishedFile} file in ReadSTARNETDNAFromPublishedFile: Reason: {e.Message}");
            }
            finally
            {
                if (Directory.Exists(tempPath))
                    Directory.Delete(tempPath, true);
            }

            return result;
        }

        public OASISResult<bool> ValidateVersion(string dnaVersion, string storedVersion, string fullPathToSTARNETHolonFolder, bool firstPublish, bool edit)
        {
            OASISResult<bool> result = new OASISResult<bool>();
            int dnaVersionInt = 0;
            int stotedVersionInt = 0;

            if (!firstPublish)
            {
                if (edit && dnaVersion != storedVersion)
                {
                    OASISErrorHandling.HandleError(ref result, $"The version in the {STARNETHolonUIName} DNA (v{dnaVersion}) is not the same as the version you are attempting to edit (v{storedVersion}). They must be the same if you wish to upload new files for version v{storedVersion}. Please edit the {STARNETDNAFileName} file found in the root of your {STARNETHolonUIName} folder ({fullPathToSTARNETHolonFolder}).");
                    return result;
                }
                else
                {
                    if (!StringHelper.IsValidVersion(dnaVersion))
                    {
                        OASISErrorHandling.HandleError(ref result, $"The version in the {STARNETHolonUIName} DNA (v{dnaVersion}) is not valid! Please make sure you enter a valid version in the form of MM.mm.rr (Major.Minor.Revision) in the {STARNETDNAFileName} file found in the root of your {STARNETHolonUIName} folder ({fullPathToSTARNETHolonFolder}).");
                        return result;
                    }

                    if (dnaVersion == storedVersion)
                    {
                        OASISErrorHandling.HandleError(ref result, $"The version in the {STARNETHolonUIName} DNA (v{dnaVersion}) is the same as the previous version ({storedVersion}). Please make sure you increment the version in the {STARNETDNAFileName} file found in the root of your {STARNETHolonUIName} folder ({fullPathToSTARNETHolonFolder}).");
                        return result;
                    }

                    if (!int.TryParse(dnaVersion.Replace(".", ""), out dnaVersionInt))
                    {
                        OASISErrorHandling.HandleError(ref result, $"The version in the {STARNETHolonUIName} DNA (v{dnaVersion}) is not valid! Please make sure you enter a valid version in the form of MM.mm.rr (Major.Minor.Revision) in the {STARNETDNAFileName} file found in the root of your {STARNETHolonUIName} folder ({fullPathToSTARNETHolonFolder}).");
                        return result;
                    }

                    //Should hopefully never occur! ;-)
                    if (!int.TryParse(storedVersion.Replace(".", ""), out stotedVersionInt))
                        OASISErrorHandling.HandleWarning(ref result, $"The version stored in the OASIS (v{storedVersion}) is not valid!");

                    if (dnaVersionInt <= stotedVersionInt)
                    {
                        OASISErrorHandling.HandleError(ref result, $"The version in the {STARNETHolonUIName} DNA (v{dnaVersion}) is less than the previous version (v{storedVersion}). Please make sure you increment the version in the {STARNETDNAFileName} file found in the root of your {STARNETHolonUIName} folder.");
                        return result;
                    }
                }
            }

            result.Result = true;
            return result;
        }

        public virtual async Task<OASISResult<T1>> UpdateNumberOfVersionCountsAsync(Guid avatarId, OASISResult<T1> result, string errorMessage, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<T1>> versionsResult = await LoadVersionsAsync(result.Result.STARNETDNA.Id, providerType);

            if (versionsResult != null && versionsResult.Result != null && !versionsResult.IsError)
            {
                foreach (T1 holonVersion in versionsResult.Result)
                {
                    holonVersion.STARNETDNA.NumberOfVersions = result.Result.STARNETDNA.NumberOfVersions;
                    OASISResult<T1> versionSaveResult = await UpdateAsync(avatarId, holonVersion, providerType: providerType);

                    if (!(versionSaveResult != null && versionSaveResult.Result != null && !versionSaveResult.IsError))
                        OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} Error occured updating the NumberOfVersions for {STARNETHolonUIName} with Id {holonVersion.Id} for provider {Enum.GetName(typeof(ProviderType), providerType)}. Reason: {versionSaveResult.Message}");
                }
            }
            else
                OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} Error occured updating the total installs for all {STARNETHolonUIName} versions caused by an error in LoadSTARNETHolonVersionsAsync. Reason: {versionsResult.Message}");


            OASISResult<IEnumerable<T3>> installedversionsResult = await ListInstalledAsync(avatarId, providerType);

            if (installedversionsResult != null && installedversionsResult.Result != null && !installedversionsResult.IsError)
            {
                foreach (T3 installedSTARNETHolon in installedversionsResult.Result)
                {
                    installedSTARNETHolon.STARNETDNA.NumberOfVersions = result.Result.STARNETDNA.NumberOfVersions;
                    OASISResult<T3> installedSTARSaveResult = await UpdateAsync(avatarId, installedSTARNETHolon, providerType: providerType);

                    if (!(installedSTARSaveResult != null && installedSTARSaveResult.Result != null && !installedSTARSaveResult.IsError))
                        OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} Error occured updating the NumberOfVersions for Installed {STARNETHolonUIName} with Id {installedSTARNETHolon.Id} for provider {Enum.GetName(typeof(ProviderType), providerType)}. Reason: {installedSTARSaveResult.Message}");
                }
            }
            else
                OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} Error occured updating the NumberOfVersions for all Installed {STARNETHolonUIName} versions caused by an error in ListInstalledSTARNETHolonsAsync. Reason: {versionsResult.Message}");

            return result;
        }

        public OASISResult<T1> UpdateNumberOfVersionCounts(Guid avatarId, OASISResult<T1> result, string errorMessage, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<T1>> versionsResult = LoadVersions(result.Result.STARNETDNA.Id, providerType);

            if (versionsResult != null && versionsResult.Result != null && !versionsResult.IsError)
            {
                foreach (T1 holonVersion in versionsResult.Result)
                {
                    holonVersion.STARNETDNA.NumberOfVersions = result.Result.STARNETDNA.NumberOfVersions;
                    OASISResult<T1> versionSaveResult = Update(avatarId, holonVersion, providerType: providerType);

                    if (!(versionSaveResult != null && versionSaveResult.Result != null && !versionSaveResult.IsError))
                        OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} Error occured updating the NumberOfVersions for {STARNETHolonUIName} with Id {holonVersion.Id} for provider {Enum.GetName(typeof(ProviderType), providerType)}. Reason: {versionSaveResult.Message}");
                }
            }
            else
                OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} Error occured updating the total installs for all {STARNETHolonUIName} versions caused by an error in LoadSTARNETHolonVersionsAsync. Reason: {versionsResult.Message}");


            OASISResult<IEnumerable<T3>> installedversionsResult = ListInstalled(avatarId, providerType);

            if (installedversionsResult != null && installedversionsResult.Result != null && !installedversionsResult.IsError)
            {
                foreach (T3 installedSTARNETHolon in installedversionsResult.Result)
                {
                    installedSTARNETHolon.STARNETDNA.NumberOfVersions = result.Result.STARNETDNA.NumberOfVersions;
                    OASISResult<T3> installedSTARSaveResult = Update(avatarId, installedSTARNETHolon, providerType: providerType);

                    if (!(installedSTARSaveResult != null && installedSTARSaveResult.Result != null && !installedSTARSaveResult.IsError))
                        OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} Error occured updating the NumberOfVersions for Installed {STARNETHolonUIName} with Id {installedSTARNETHolon.Id} for provider {Enum.GetName(typeof(ProviderType), providerType)}. Reason: {installedSTARSaveResult.Message}");
                }
            }
            else
                OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} Error occured updating the NumberOfVersions for all Installed {STARNETHolonUIName} versions caused by an error in ListInstalledSTARNETHolonsAsync. Reason: {versionsResult.Message}");

            return result;
        }

        public virtual async Task<OASISResult<T2>> UpdateDownloadCountsAsync(Guid avatarId, T2 downloadedSTARNETHolon, T4 STARNETDNA, OASISResult<T2> result, string errorMessage, ProviderType providerType = ProviderType.Default)
        {
            int totalDownloads = 0;
            OASISResult<IEnumerable<T1>> holonVersionsResult = await LoadVersionsAsync(STARNETDNA.Id, providerType);

            if (holonVersionsResult != null && holonVersionsResult.Result != null && !holonVersionsResult.IsError)
            {
                //Update total installs for all versions.
                foreach (T1 holonVersion in holonVersionsResult.Result)
                    totalDownloads += holonVersion.STARNETDNA.Downloads;

                //Need to add this download (because its not saved yet).
                totalDownloads++;

                foreach (T1 holonVersion in holonVersionsResult.Result)
                {
                    holonVersion.STARNETDNA.TotalDownloads = totalDownloads;
                    OASISResult<T1> holonVersionSaveResult = await UpdateAsync(avatarId, holonVersion, providerType: providerType);

                    if (!(holonVersionSaveResult != null && holonVersionSaveResult.Result != null && !holonVersionSaveResult.IsError))
                        OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} Error occured updating the TotalDownloads for {STARNETHolonUIName} with Id {holonVersion.Id} for provider {Enum.GetName(typeof(ProviderType), providerType)}. Reason: {holonVersionSaveResult.Message}");
                }

                STARNETDNA.TotalDownloads = totalDownloads;
                downloadedSTARNETHolon.STARNETDNA.TotalDownloads = totalDownloads;
            }
            else
                OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} Error occured updating the total downloads for all {STARNETHolonUIName} versions caused by an error in LoadSTARNETHolonVersionsAsync. Reason: {holonVersionsResult.Message}");


            OASISResult<IEnumerable<T3>> installedholonVersionsResult = await ListInstalledAsync(avatarId, providerType);

            if (installedholonVersionsResult != null && installedholonVersionsResult.Result != null && !installedholonVersionsResult.IsError)
            {
                foreach (T3 holonVersion in installedholonVersionsResult.Result)
                {
                    holonVersion.STARNETDNA.TotalDownloads = totalDownloads;
                    OASISResult<T3> holonVersionSaveResult = await UpdateAsync(avatarId, holonVersion, providerType: providerType);

                    if (!(holonVersionSaveResult != null && holonVersionSaveResult.Result != null && !holonVersionSaveResult.IsError))
                        OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} Error occured updating the TotalDownloads for Installed {STARNETHolonUIName} with Id {holonVersion.Id} for provider {Enum.GetName(typeof(ProviderType), providerType)}. Reason: {holonVersionSaveResult.Message}");
                }
            }
            else
                OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} Error occured updating the total downloads for all Installed {STARNETHolonUIName} versions caused by an error in ListInstalledSTARNETHolonsAsync. Reason: {holonVersionsResult.Message}");

            return result;
        }

        public OASISResult<T2> UpdateDownloadCounts(Guid avatarId, T2 downloadedSTARNETHolon, T4 STARNETDNA, OASISResult<T2> result, string errorMessage, ProviderType providerType = ProviderType.Default)
        {
            int totalDownloads = 0;
            OASISResult<IEnumerable<T1>> holonVersionsResult = LoadVersions(STARNETDNA.Id, providerType);

            if (holonVersionsResult != null && holonVersionsResult.Result != null && !holonVersionsResult.IsError)
            {
                //Update total installs for all versions.
                foreach (T1 holonVersion in holonVersionsResult.Result)
                    totalDownloads += holonVersion.STARNETDNA.Downloads;

                //Need to add this download (because its not saved yet).
                totalDownloads++;

                foreach (T1 holonVersion in holonVersionsResult.Result)
                {
                    holonVersion.STARNETDNA.TotalDownloads = totalDownloads;
                    OASISResult<T1> holonVersionSaveResult = Update(avatarId, holonVersion, providerType: providerType);

                    if (!(holonVersionSaveResult != null && holonVersionSaveResult.Result != null && !holonVersionSaveResult.IsError))
                        OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} Error occured updating the TotalDownloads for {STARNETHolonUIName} with Id {holonVersion.Id} for provider {Enum.GetName(typeof(ProviderType), providerType)}. Reason: {holonVersionSaveResult.Message}");
                }

                STARNETDNA.TotalDownloads = totalDownloads;
                downloadedSTARNETHolon.STARNETDNA.TotalDownloads = totalDownloads;
            }
            else
                OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} Error occured updating the total downloads for all {STARNETHolonUIName} versions caused by an error in LoadSTARNETHolonVersionsAsync. Reason: {holonVersionsResult.Message}");


            OASISResult<IEnumerable<T3>> installedholonVersionsResult = ListInstalled(avatarId, providerType);

            if (installedholonVersionsResult != null && installedholonVersionsResult.Result != null && !installedholonVersionsResult.IsError)
            {
                foreach (T3 holonVersion in installedholonVersionsResult.Result)
                {
                    holonVersion.STARNETDNA.TotalDownloads = totalDownloads;
                    OASISResult<T3> holonVersionSaveResult = Update(avatarId, holonVersion, providerType: providerType);

                    if (!(holonVersionSaveResult != null && holonVersionSaveResult.Result != null && !holonVersionSaveResult.IsError))
                        OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} Error occured updating the TotalDownloads for Installed {STARNETHolonUIName} with Id {holonVersion.Id} for provider {Enum.GetName(typeof(ProviderType), providerType)}. Reason: {holonVersionSaveResult.Message}");
                }
            }
            else
                OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} Error occured updating the total downloads for all Installed {STARNETHolonUIName} versions caused by an error in ListInstalledSTARNETHolonsAsync. Reason: {holonVersionsResult.Message}");

            return result;
        }

        public virtual async Task<OASISResult<T3>> UpdateInstallCountsAsync(Guid avatarId, T3 installedSTARNETHolon, T4 STARNETDNA, OASISResult<T3> result, string errorMessage, ProviderType providerType = ProviderType.Default)
        {
            int totalInstalls = 0;
            OASISResult<IEnumerable<T1>> holonVersionsResult = await LoadVersionsAsync(STARNETDNA.Id, providerType);

            if (holonVersionsResult != null && holonVersionsResult.Result != null && !holonVersionsResult.IsError)
            {
                //Update total installs for all versions.
                foreach (T1 holonVersion in holonVersionsResult.Result)
                    totalInstalls += holonVersion.STARNETDNA.Installs;

                //Need to add this install (because its not saved yet).
                totalInstalls++;

                foreach (T1 holonVersion in holonVersionsResult.Result)
                {
                    holonVersion.STARNETDNA.TotalInstalls = totalInstalls;
                    OASISResult<T1> holonVersionSaveResult = await UpdateAsync(avatarId, holonVersion, providerType: providerType);

                    if (!(holonVersionSaveResult != null && holonVersionSaveResult.Result != null && !holonVersionSaveResult.IsError))
                        OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} Error occured updating the TotalInstalls for {STARNETHolonUIName} with Id {holonVersion.Id} for provider {Enum.GetName(typeof(ProviderType), providerType)}. Reason: {holonVersionSaveResult.Message}");
                }

                STARNETDNA.TotalInstalls = totalInstalls;
                installedSTARNETHolon.STARNETDNA.TotalInstalls = totalInstalls;
            }
            else
                OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} Error occured updating the total installs for all {STARNETHolonUIName} versions caused by an error in LoadSTARNETHolonVersionsAsync. Reason: {holonVersionsResult.Message}");


            OASISResult<IEnumerable<T3>> installedholonVersionsResult = await ListInstalledAsync(avatarId, providerType);

            if (installedholonVersionsResult != null && installedholonVersionsResult.Result != null && !installedholonVersionsResult.IsError)
            {
                foreach (T3 holonVersion in installedholonVersionsResult.Result)
                {
                    holonVersion.STARNETDNA.TotalInstalls = totalInstalls;
                    OASISResult<T3> holonVersionSaveResult = await UpdateAsync(avatarId, holonVersion, providerType: providerType);

                    if (!(holonVersionSaveResult != null && holonVersionSaveResult.Result != null && !holonVersionSaveResult.IsError))
                        OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} Error occured updating the TotalInstalls for Installed {STARNETHolonUIName} with Id {holonVersion.Id} for provider {Enum.GetName(typeof(ProviderType), providerType)}. Reason: {holonVersionSaveResult.Message}");
                }
            }
            else
                OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} Error occured updating the total installs for all Installed {STARNETHolonUIName} versions caused by an error in ListInstalledSTARNETHolonsAsync. Reason: {holonVersionsResult.Message}");


            OASISResult<IEnumerable<T3>> uninstalledholonVersionsResult = await ListUninstalledAsync(avatarId, providerType);

            if (uninstalledholonVersionsResult != null && uninstalledholonVersionsResult.Result != null && !uninstalledholonVersionsResult.IsError)
            {
                foreach (T3 holonVersion in uninstalledholonVersionsResult.Result)
                {
                    holonVersion.STARNETDNA.TotalInstalls = totalInstalls;
                    OASISResult<T3> holonVersionSaveResult = await UpdateAsync(avatarId, holonVersion, providerType: providerType);

                    if (!(holonVersionSaveResult != null && holonVersionSaveResult.Result != null && !holonVersionSaveResult.IsError))
                        OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} Error occured updating the TotalInstalls for Uninstalled {STARNETHolonUIName} with Id {holonVersion.Id} for provider {Enum.GetName(typeof(ProviderType), providerType)}. Reason: {holonVersionSaveResult.Message}");
                }
            }
            else
                OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} Error occured updating the total installs for all Installed {STARNETHolonUIName} versions caused by an error in ListInstalledSTARNETHolonsAsync. Reason: {holonVersionsResult.Message}");

            return result;
        }

        public OASISResult<T3> UpdateInstallCounts(Guid avatarId, T3 installedSTARNETHolon, T4 STARNETDNA, OASISResult<T3> result, string errorMessage, ProviderType providerType = ProviderType.Default)
        {
            int totalInstalls = 0;
            OASISResult<IEnumerable<T1>> holonVersionsResult = LoadVersions(STARNETDNA.Id, providerType);

            if (holonVersionsResult != null && holonVersionsResult.Result != null && !holonVersionsResult.IsError)
            {
                //Update total installs for all versions.
                foreach (T1 holonVersion in holonVersionsResult.Result)
                    totalInstalls += holonVersion.STARNETDNA.Installs;

                //Need to add this install (because its not saved yet).
                totalInstalls++;

                foreach (T1 holonVersion in holonVersionsResult.Result)
                {
                    holonVersion.STARNETDNA.TotalInstalls = totalInstalls;
                    OASISResult<T1> holonVersionSaveResult = Update(avatarId, holonVersion, providerType: providerType);

                    if (!(holonVersionSaveResult != null && holonVersionSaveResult.Result != null && !holonVersionSaveResult.IsError))
                        OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} Error occured updating the TotalInstalls for {STARNETHolonUIName} with Id {holonVersion.Id} for provider {Enum.GetName(typeof(ProviderType), providerType)}. Reason: {holonVersionSaveResult.Message}");
                }

                STARNETDNA.TotalInstalls = totalInstalls;
                installedSTARNETHolon.STARNETDNA.TotalInstalls = totalInstalls;
            }
            else
                OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} Error occured updating the total installs for all {STARNETHolonUIName} versions caused by an error in LoadSTARNETHolonVersionsAsync. Reason: {holonVersionsResult.Message}");


            OASISResult<IEnumerable<T3>> installedholonVersionsResult = ListInstalled(avatarId, providerType);

            if (installedholonVersionsResult != null && installedholonVersionsResult.Result != null && !installedholonVersionsResult.IsError)
            {
                foreach (T3 holonVersion in installedholonVersionsResult.Result)
                {
                    holonVersion.STARNETDNA.TotalInstalls = totalInstalls;
                    OASISResult<T3> holonVersionSaveResult = Update(avatarId, holonVersion, providerType: providerType);

                    if (!(holonVersionSaveResult != null && holonVersionSaveResult.Result != null && !holonVersionSaveResult.IsError))
                        OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} Error occured updating the TotalInstalls for Installed {STARNETHolonUIName} with Id {holonVersion.Id} for provider {Enum.GetName(typeof(ProviderType), providerType)}. Reason: {holonVersionSaveResult.Message}");
                }
            }
            else
                OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} Error occured updating the total installs for all Installed {STARNETHolonUIName} versions caused by an error in ListInstalledSTARNETHolonsAsync. Reason: {installedholonVersionsResult.Message}");

            OASISResult<IEnumerable<T3>> uninstalledholonVersionsResult = ListUninstalled(avatarId, providerType);


            if (uninstalledholonVersionsResult != null && uninstalledholonVersionsResult.Result != null && !uninstalledholonVersionsResult.IsError)
            {
                foreach (T3 holonVersion in uninstalledholonVersionsResult.Result)
                {
                    holonVersion.STARNETDNA.TotalInstalls = totalInstalls;
                    OASISResult<T3> holonVersionSaveResult = Update(avatarId, holonVersion, providerType: providerType);

                    if (!(holonVersionSaveResult != null && holonVersionSaveResult.Result != null && !holonVersionSaveResult.IsError))
                        OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} Error occured updating the TotalInstalls for Uninstalled {STARNETHolonUIName} with Id {holonVersion.Id} for provider {Enum.GetName(typeof(ProviderType), providerType)}. Reason: {holonVersionSaveResult.Message}");
                }
            }
            else
                OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} Error occured updating the total installs for all Installed {STARNETHolonUIName} versions caused by an error in ListInstalledSTARNETHolonsAsync. Reason: {uninstalledholonVersionsResult.Message}");

            return result;
        }

        //public async Task<OASISResult<T1>> RemoveRuntimeAsync(Guid avatarId, T1 parent, IInstalledRuntime installedRuntime, ProviderType providerType = ProviderType.Default)
        //{
        //    OASISResult<T1> result = new OASISResult<T1>();
        //    string errorMessage = "Error occured in OAPPManagerBase.RemoveRuntimeAsync. Reason:";

        //    try
        //    {
        //        OASISResult<Runtime> runtimeResult = await Data.LoadHolonByMetaDataAsync<Runtime>(new Dictionary<string, string>()
        //        {
        //            { "STARNETHolonId", installedRuntime.STARNETDNA.Id.ToString() },
        //            { "Version", installedRuntime.STARNETDNA.Version }

        //        }, MetaKeyValuePairMatchMode.All, HolonType.Runtime, providerType: providerType);

        //        if (runtimeResult != null && runtimeResult.Result != null && !runtimeResult.IsError)
        //        {
        //            string installPath = Path.Combine(parent.STARNETDNA.SourcePath, "Dependencies", "STARNET", "Runtimes", string.Concat(runtimeResult.Result.STARNETDNA.Name, "_v", runtimeResult.Result.STARNETDNA.Version));

        //            if (!parent.STARNETDNA.Dependencies.Runtimes.Any(x => x.HolonId == runtimeResult.Result.Id))
        //            {
        //                parent.STARNETDNA.Dependencies.Runtimes.Remove(new STARNETDependency()
        //                {
        //                    HolonId = runtimeResult.Result.Id,
        //                    STARNETHolonId = runtimeResult.Result.STARNETDNA.Id,
        //                    Name = runtimeResult.Result.Name,
        //                    Description = runtimeResult.Result.Description,
        //                    VersionSequence = runtimeResult.Result.STARNETDNA.VersionSequence,
        //                    Version = runtimeResult.Result.STARNETDNA.Version,
        //                    InstalledFrom = installedRuntime.InstalledPath,
        //                    InstalledTo = installPath
        //                });

        //                result = Update(avatarId, parent, result, errorMessage, true, string.Concat(Enum.GetName(typeof(HolonType), parent.HolonType), "DNAJSON"), providerType: providerType);

        //                if (result != null && result.Result != null && !result.IsError)
        //                    DirectoryHelper.CopyFilesRecursively(installedRuntime.InstalledPath, installPath);
        //            }
        //            else
        //                OASISErrorHandling.HandleError(ref result, $"{errorMessage} The runtime {runtimeResult.Result.Name} has already been added to {parent.STARNETDNA.Name}.");
        //        }
        //        else
        //            OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the Runtime with Data.LoadHolonByMetaDataAsync. Reason: {runtimeResult.Message}");
        //    }
        //    catch (Exception ex)
        //    {
        //        OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured. Reason: {ex}");
        //    }

        //    return result;
        //}
        //public OASISResult<T1> RemoveRuntime(Guid avatarId, T1 parent, IInstalledRuntime installedRuntime, ProviderType providerType = ProviderType.Default)
        //{
        //    OASISResult<T1> result = new OASISResult<T1>();
        //    string errorMessage = "Error occured in OAPPManagerBase.RemoveRuntime. Reason:";

        //    try
        //    {
        //        OASISResult<Runtime> runtimeResult = Data.LoadHolonByMetaData<Runtime>(new Dictionary<string, string>()
        //        {
        //            { "STARNETHolonId", installedRuntime.STARNETDNA.Id.ToString() },
        //            { "Version", installedRuntime.STARNETDNA.Version }

        //        }, MetaKeyValuePairMatchMode.All, HolonType.Runtime, providerType: providerType);

        //        if (runtimeResult != null && runtimeResult.Result != null && !runtimeResult.IsError)
        //        {
        //            string installPath = Path.Combine(parent.STARNETDNA.SourcePath, "Dependencies", "STARNET", "Runtimes", string.Concat(runtimeResult.Result.STARNETDNA.Name, "_v", runtimeResult.Result.STARNETDNA.Version));

        //            if (!parent.STARNETDNA.Dependencies.Runtimes.Any(x => x.HolonId == runtimeResult.Result.Id))
        //            {
        //                parent.STARNETDNA.Dependencies.Runtimes.Remove(new STARNETDependency()
        //                {
        //                    HolonId = runtimeResult.Result.Id,
        //                    STARNETHolonId = runtimeResult.Result.STARNETDNA.Id,
        //                    Name = runtimeResult.Result.Name,
        //                    Description = runtimeResult.Result.Description,
        //                    VersionSequence = runtimeResult.Result.STARNETDNA.VersionSequence,
        //                    Version = runtimeResult.Result.STARNETDNA.Version,
        //                    InstalledFrom = installedRuntime.InstalledPath,
        //                    InstalledTo = installPath
        //                });

        //                result = Update(avatarId, parent, result, errorMessage, true, string.Concat(Enum.GetName(typeof(HolonType), parent.HolonType), "DNAJSON"), providerType: providerType);

        //                if (result != null && result.Result != null && !result.IsError)
        //                    DirectoryHelper.CopyFilesRecursively(installedRuntime.InstalledPath, installPath);
        //            }
        //            else
        //                OASISErrorHandling.HandleError(ref result, $"{errorMessage} The runtime {runtimeResult.Result.Name} has already been added to {parent.STARNETDNA.Name}.");
        //        }
        //        else
        //            OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the Runtime with Data.LoadHolonByMetaData. Reason: {runtimeResult.Message}");
        //    }
        //    catch (Exception ex)
        //    {
        //        OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured. Reason: {ex}");
        //    }

        //    return result;
        //}

        //public async Task<OASISResult<T1>> RemoveRuntimeAsync(Guid avatarId, Guid parentId, string parentVersion, IInstalledRuntime installedRuntime, ProviderType providerType = ProviderType.Default)
        //{
        //    OASISResult<T1> result = new OASISResult<T1>();
        //    string errorMessage = "Error occured in OAPPManagerBase.RemoveRuntimeAsync. Reason:";

        //    try
        //    {
        //        OASISResult<T1> parentResult = await Data.LoadHolonByMetaDataAsync<T1>(new Dictionary<string, string>()
        //        {
        //            { STARNETHolonIdName, parentId.ToString() },
        //            { "Version", parentVersion }

        //        }, MetaKeyValuePairMatchMode.All, STARNETHolonType, providerType: providerType);

        //        if (parentResult != null && parentResult.Result != null && !parentResult.IsError)
        //            return await RemoveRuntimeAsync(avatarId, parentResult.Result, installedRuntime, providerType);
        //        else
        //            OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the parent with OAPPManagerBase.LoadHolonByMetaDataAsync. Reason: {parentResult.Message}");
        //    }
        //    catch (Exception ex)
        //    {
        //        OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured. Reason: {ex}");
        //    }

        //    return result;
        //}

        //public OASISResult<T1> RemoveRuntime(Guid avatarId, Guid parentId, string parentVersion, IInstalledRuntime installedRuntime, ProviderType providerType = ProviderType.Default)
        //{
        //    OASISResult<T1> result = new OASISResult<T1>();
        //    string errorMessage = "Error occured in OAPPManagerBase.RemoveRuntime. Reason:";

        //    try
        //    {
        //        OASISResult<T1> parentResult = Data.LoadHolonByMetaData<T1>(new Dictionary<string, string>()
        //        {
        //            { STARNETHolonIdName, parentId.ToString() },
        //            { "Version", parentVersion }

        //        }, MetaKeyValuePairMatchMode.All, STARNETHolonType, providerType: providerType);

        //        if (parentResult != null && parentResult.Result != null && !parentResult.IsError)
        //            return RemoveRuntime(avatarId, parentResult.Result, installedRuntime, providerType);
        //        else
        //            OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the parent with OAPPManagerBase.LoadHolonByMetaData. Reason: {parentResult.Message}");
        //    }
        //    catch (Exception ex)
        //    {
        //        OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured. Reason: {ex}");
        //    }

        //    return result;
        //}

        //public async Task<OASISResult<T1>> RemoveRuntimeAsync(Guid avatarId, Guid parentId, int parentVersionSequence, IInstalledRuntime installedRuntime, ProviderType providerType = ProviderType.Default)
        //{
        //    OASISResult<T1> result = new OASISResult<T1>();
        //    string errorMessage = "Error occured in OAPPManagerBase.RemoveRuntimeAsync. Reason:";

        //    try
        //    {
        //        OASISResult<T1> parentResult = await Data.LoadHolonByMetaDataAsync<T1>(new Dictionary<string, string>()
        //        {
        //            { STARNETHolonIdName, parentId.ToString() },
        //            { "VersionSequence", parentVersionSequence.ToString() }

        //        }, MetaKeyValuePairMatchMode.All, STARNETHolonType, providerType: providerType);

        //        if (parentResult != null && parentResult.Result != null && !parentResult.IsError)
        //            return await RemoveRuntimeAsync(avatarId, parentResult.Result, installedRuntime, providerType);
        //        else
        //            OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the parent with OAPPManagerBase.LoadHolonByMetaDataAsync. Reason: {parentResult.Message}");
        //    }
        //    catch (Exception ex)
        //    {
        //        OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured. Reason: {ex}");
        //    }

        //    return result;
        //}

        //public OASISResult<T1> RemoveRuntime(Guid avatarId, Guid parentId, int parentVersionSequence, IInstalledRuntime installedRuntime, ProviderType providerType = ProviderType.Default)
        //{
        //    OASISResult<T1> result = new OASISResult<T1>();
        //    string errorMessage = "Error occured in OAPPManagerBase.RemoveRuntime. Reason:";

        //    try
        //    {
        //        OASISResult<T1> parentResult = Data.LoadHolonByMetaData<T1>(new Dictionary<string, string>()
        //        {
        //            { STARNETHolonIdName, parentId.ToString() },
        //            { "VersionSequence", parentVersionSequence.ToString() }

        //        }, MetaKeyValuePairMatchMode.All, STARNETHolonType, providerType: providerType);

        //        if (parentResult != null && parentResult.Result != null && !parentResult.IsError)
        //            return RemoveRuntime(avatarId, parentResult.Result, installedRuntime, providerType);
        //        else
        //            OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the parent with OAPPManagerBase.LoadHolonByMetaData. Reason: {parentResult.Message}");
        //    }
        //    catch (Exception ex)
        //    {
        //        OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured. Reason: {ex}");
        //    }

        //    return result;
        //}

        //public async Task<OASISResult<T1>> RemoveRuntimeAsync(Guid avatarId, Guid parentId, int parentVersionSequence, Guid runtimeId, int runtimeVersionSequence, ProviderType providerType = ProviderType.Default)
        //{
        //    OASISResult<T1> result = new OASISResult<T1>();
        //    string errorMessage = "Error occured in OAPPManagerBase.RemoveRuntimeAsync. Reason:";

        //    OASISResult<InstalledRuntime> installedLibResult = await Data.LoadHolonByMetaDataAsync<InstalledRuntime>(new Dictionary<string, string>()
        //    {
        //        { "STARNETHolonId", runtimeId.ToString() },
        //        { "VersionSequence", runtimeVersionSequence.ToString() }

        //    }, MetaKeyValuePairMatchMode.All, HolonType.InstalledRuntime, providerType: providerType);

        //    if (installedLibResult != null && installedLibResult.Result != null && !installedLibResult.IsError)
        //        result = await RemoveRuntimeAsync(avatarId, parentId, parentVersionSequence, (IInstalledRuntime)installedLibResult.Result, providerType);
        //    else
        //        OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the Installed Runtime with Data.LoadHolonByMetaDataAsync. Reason: {installedLibResult.Message}");

        //    return result;
        //}

        //public OASISResult<T1> RemoveRuntime(Guid avatarId, Guid parentId, int parentVersionSequence, Guid runtimeId, int runtimeVersionSequence, ProviderType providerType = ProviderType.Default)
        //{
        //    OASISResult<T1> result = new OASISResult<T1>();
        //    string errorMessage = "Error occured in OAPPManagerBase.RemoveRuntime. Reason:";

        //    OASISResult<InstalledRuntime> installedLibResult = Data.LoadHolonByMetaData<InstalledRuntime>(new Dictionary<string, string>()
        //    {
        //        { "STARNETHolonId", runtimeId.ToString() },
        //        { "VersionSequence", runtimeVersionSequence.ToString() }

        //    }, MetaKeyValuePairMatchMode.All, HolonType.InstalledRuntime, providerType: providerType);

        //    if (installedLibResult != null && installedLibResult.Result != null && !installedLibResult.IsError)
        //        result = RemoveRuntime(avatarId, parentId, parentVersionSequence, (IInstalledRuntime)installedLibResult.Result, providerType);
        //    else
        //        OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the Installed Runtime with Data.LoadHolonByMetaData. Reason: {installedLibResult.Message}");

        //    return result;
        //}

        //public async Task<OASISResult<T1>> RemoveRuntimeAsync(Guid avatarId, Guid parentId, string parentVersion, Guid runtimeId, string runtimeVersion, ProviderType providerType = ProviderType.Default)
        //{
        //    OASISResult<T1> result = new OASISResult<T1>();
        //    string errorMessage = "Error occured in OAPPManagerBase.RemoveRuntimeAsync. Reason:";

        //    OASISResult<InstalledRuntime> installedLibResult = await Data.LoadHolonByMetaDataAsync<InstalledRuntime>(new Dictionary<string, string>()
        //    {
        //        { "STARNETHolonId", runtimeId.ToString() },
        //        { "Version", runtimeVersion }

        //    }, MetaKeyValuePairMatchMode.All, HolonType.InstalledRuntime, providerType: providerType);

        //    if (installedLibResult != null && installedLibResult.Result != null && !installedLibResult.IsError)
        //        result = await RemoveRuntimeAsync(avatarId, parentId, parentVersion, (IInstalledRuntime)installedLibResult.Result, providerType);
        //    else
        //        OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the Installed Runtime with Data.LoadHolonByMetaDataAsync. Reason: {installedLibResult.Message}");

        //    return result;
        //}

        //public OASISResult<T1> RemoveRuntime(Guid avatarId, Guid parentId, string parentVersion, Guid runtimeId, string runtimeVersion, ProviderType providerType = ProviderType.Default)
        //{
        //    OASISResult<T1> result = new OASISResult<T1>();
        //    string errorMessage = "Error occured in OAPPManagerBase.RemoveRuntime. Reason:";

        //    OASISResult<InstalledRuntime> installedLibResult = Data.LoadHolonByMetaData<InstalledRuntime>(new Dictionary<string, string>()
        //    {
        //        { "STARNETHolonId", runtimeId.ToString() },
        //        { "Version", runtimeVersion }

        //    }, MetaKeyValuePairMatchMode.All, HolonType.InstalledRuntime, providerType: providerType);

        //    if (installedLibResult != null && installedLibResult.Result != null && !installedLibResult.IsError)
        //        result = RemoveRuntime(avatarId, parentId, parentVersion, (IInstalledRuntime)installedLibResult.Result, providerType);
        //    else
        //        OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the Installed Runtime with Data.LoadHolonByMetaData. Reason: {installedLibResult.Message}");

        //    return result;
        //}


        //public async Task<OASISResult<T1>> RemoveRuntimeAsync(Guid avatarId, Guid parentId, int parentVersionSequence, Guid runtimeId, int runtimeVersionSequence, ProviderType providerType = ProviderType.Default)
        //{
        //    OASISResult<T1> result = new OASISResult<T1>();
        //    string errorMessage = "Error occured in OAPPManagerBase.RemoveRuntimeAsync. Reason:";

        //    try
        //    {
        //        OASISResult<T1> parentResult = await LoadAsync(avatarId, parentId, parentVersionSequence, providerType: providerType);

        //        if (parentResult != null && parentResult.Result != null && !parentResult.IsError)
        //        {
        //            OASISResult<Runtime> runtimeResult = await Data.LoadHolonByMetaDataAsync<Runtime>(new Dictionary<string, string>()
        //            {
        //                { "STARNETHolonId", runtimeId.ToString() },
        //                { "VersionSequence", runtimeVersionSequence.ToString() }

        //            }, MetaKeyValuePairMatchMode.All, HolonType.Runtime, providerType: providerType);

        //            if (runtimeResult != null && runtimeResult.Result != null && !runtimeResult.IsError)
        //            {
        //                STARNETDependency dependency = parentResult.Result.STARNETDNA.Dependencies.Runtimes.FirstOrDefault(x => x.HolonId == runtimeResult.Result.Id);

        //                if (dependency != null)
        //                {
        //                    parentResult.Result.STARNETDNA.Dependencies.Runtimes.Remove(dependency);
        //                    result = Update(avatarId, parentResult.Result, result, errorMessage, true, string.Concat(Enum.GetName(typeof(HolonType), parentResult.Result.HolonType), "DNAJSON"), providerType: providerType);

        //                    string path = Path.Combine(parentResult.Result.STARNETDNA.SourcePath, "Dependencies", "STARNET", "Runtimes", string.Concat(runtimeResult.Result.STARNETDNA.Name, "_v", runtimeResult.Result.STARNETDNA.Version));

        //                    if (Directory.Exists(path))
        //                        Directory.Exists(path);
        //                }
        //                else
        //                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} The runtime with id {runtimeResult.Result.Id} was not found!");
        //            }
        //            else
        //                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the Runtime with Data.LoadHolonByMetaDataAsync. Reason: {runtimeResult.Message}");
        //        }
        //        else
        //            OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the {STARNETHolonUIName}  with OAPPManagerBase.LoadHolonByMetaDataAsync. Reason: {parentResult.Message}");
        //    }
        //    catch (Exception ex)
        //    {
        //        OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured. Reason: {ex}");
        //    }

        //    return result;
        //}

        //public OASISResult<T1> RemoveRuntime(Guid avatarId, Guid parentId, int parentVersionSequence, Guid runtimeId, int runtimeVersionSequnce, ProviderType providerType = ProviderType.Default)
        //{
        //    OASISResult<T1> result = new OASISResult<T1>();
        //    string errorMessage = "Error occured in OAPPManagerBase.RemoveRuntime. Reason:";

        //    try
        //    {
        //        OASISResult<T1> parentResult = Load(avatarId, parentId, parentVersionSequence, providerType: providerType);

        //        if (parentResult != null && parentResult.Result != null && !parentResult.IsError)
        //        {
        //            OASISResult<Runtime> runtimeResult = Data.LoadHolonByMetaData<Runtime>(new Dictionary<string, string>()
        //            {
        //                { "STARNETHolonId", runtimeId.ToString() },
        //                { "VersionSequence", runtimeVersionSequnce.ToString() }

        //            }, MetaKeyValuePairMatchMode.All, HolonType.Runtime, providerType: providerType);

        //            if (runtimeResult != null && runtimeResult.Result != null && !runtimeResult.IsError)
        //            {
        //                STARNETDependency dependency = parentResult.Result.STARNETDNA.Dependencies.Runtimes.FirstOrDefault(x => x.HolonId == runtimeResult.Result.Id);

        //                if (dependency != null)
        //                {
        //                    parentResult.Result.STARNETDNA.Dependencies.Runtimes.Remove(dependency);
        //                    result = Update(avatarId, parentResult.Result, result, errorMessage, true, string.Concat(Enum.GetName(typeof(HolonType), parentResult.Result.HolonType), "DNAJSON"), providerType: providerType);

        //                    string path = Path.Combine(parentResult.Result.STARNETDNA.SourcePath, "Dependencies", "STARNET", "Runtimes", string.Concat(runtimeResult.Result.STARNETDNA.Name, "_v", runtimeResult.Result.STARNETDNA.Version));

        //                    if (Directory.Exists(path))
        //                        Directory.Exists(path);
        //                }
        //                else
        //                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} The runtime with id {runtimeResult.Result.Id} was not found!");
        //            }
        //            else
        //                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the Runtime with Data.LoadHolonByMetaData. Reason: {runtimeResult.Message}");
        //        }
        //        else
        //            OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the {STARNETHolonUIName}  with OAPPManagerBase.LoadHolonByMetaData. Reason: {parentResult.Message}");
        //    }
        //    catch (Exception ex)
        //    {
        //        OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured. Reason: {ex}");
        //    }

        //    return result;
        //}

        //public async Task<OASISResult<T1>> RemoveRuntimeAsync(Guid avatarId, Guid parentId, string parentVersion, Guid runtimeId, string runtimeVersion, ProviderType providerType = ProviderType.Default)
        //{
        //    OASISResult<T1> result = new OASISResult<T1>();
        //    string errorMessage = "Error occured in OAPPManagerBase.RemoveRuntimeAsync. Reason:";

        //    try
        //    {
        //        OASISResult<T1> parentResult = await Data.LoadHolonByMetaDataAsync<T1>(new Dictionary<string, string>()
        //        {
        //            { STARNETHolonIdName, parentId.ToString() },
        //            { "Version", parentVersion }

        //        }, MetaKeyValuePairMatchMode.All, providerType: providerType);

        //        if (parentResult != null && parentResult.Result != null && !parentResult.IsError)
        //        {
        //            OASISResult<Runtime> runtimeResult = await Data.LoadHolonByMetaDataAsync<Runtime>(new Dictionary<string, string>()
        //            {
        //                { "STARNETHolonId", runtimeId.ToString() },
        //                { "Version", runtimeVersion }

        //            }, MetaKeyValuePairMatchMode.All, HolonType.Runtime, providerType: providerType);

        //            if (runtimeResult != null && runtimeResult.Result != null && !runtimeResult.IsError)
        //            {
        //                STARNETDependency dependency = parentResult.Result.STARNETDNA.Dependencies.Runtimes.FirstOrDefault(x => x.HolonId == runtimeResult.Result.Id);

        //                if (dependency != null)
        //                {
        //                    parentResult.Result.STARNETDNA.Dependencies.Runtimes.Remove(dependency);
        //                    result = Update(avatarId, parentResult.Result, result, errorMessage, true, string.Concat(Enum.GetName(typeof(HolonType), parentResult.Result.HolonType), "DNAJSON"), providerType: providerType);

        //                    string path = Path.Combine(parentResult.Result.STARNETDNA.SourcePath, "Dependencies", "STARNET", "Runtimes", string.Concat(runtimeResult.Result.STARNETDNA.Name, "_v", runtimeResult.Result.STARNETDNA.Version));

        //                    if (Directory.Exists(path))
        //                        Directory.Exists(path);
        //                }
        //                else
        //                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} The runtime with id {runtimeResult.Result.Id} was not found!");
        //            }
        //            else
        //                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the Runtime with Data.LoadHolonByMetaDataAsync. Reason: {runtimeResult.Message}");
        //        }
        //        else
        //            OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the parent with OAPPManagerBase.LoadHolonByMetaDataAsync. Reason: {parentResult.Message}");
        //    }
        //    catch (Exception ex)
        //    {
        //        OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured. Reason: {ex}");
        //    }

        //    return result;
        //}

        //public OASISResult<T1> RemoveRuntime(Guid avatarId, Guid parentId, string parentVersion, Guid runtimeId, string runtimeVersion, ProviderType providerType = ProviderType.Default)
        //{
        //    OASISResult<T1> result = new OASISResult<T1>();
        //    string errorMessage = "Error occured in OAPPManagerBase.RemoveRuntime. Reason:";

        //    try
        //    {
        //        OASISResult<T1> parentResult = Data.LoadHolonByMetaData<T1>(new Dictionary<string, string>()
        //        {
        //            { STARNETHolonIdName, parentId.ToString() },
        //            { "Version", parentVersion }

        //        }, MetaKeyValuePairMatchMode.All, STARNETHolonType, providerType: providerType);

        //        if (parentResult != null && parentResult.Result != null && !parentResult.IsError)
        //        {
        //            OASISResult<Runtime> runtimeResult = Data.LoadHolonByMetaData<Runtime>(new Dictionary<string, string>()
        //            {
        //                { "STARNETHolonId", runtimeId.ToString() },
        //                { "Version", runtimeVersion.ToString() }

        //            }, MetaKeyValuePairMatchMode.All, HolonType.Runtime, providerType: providerType);

        //            if (runtimeResult != null && runtimeResult.Result != null && !runtimeResult.IsError)
        //            {
        //                STARNETDependency dependency = parentResult.Result.STARNETDNA.Dependencies.Runtimes.FirstOrDefault(x => x.HolonId == runtimeResult.Result.Id);

        //                if (dependency != null)
        //                {
        //                    parentResult.Result.STARNETDNA.Dependencies.Runtimes.Remove(dependency);
        //                    result = Update(avatarId, parentResult.Result, result, errorMessage, true, string.Concat(Enum.GetName(typeof(HolonType), parentResult.Result.HolonType), "DNAJSON"), providerType: providerType);

        //                    string path = Path.Combine(parentResult.Result.STARNETDNA.SourcePath, "Dependencies", "STARNET", "Runtimes", string.Concat(runtimeResult.Result.STARNETDNA.Name, "_v", runtimeResult.Result.STARNETDNA.Version));

        //                    if (Directory.Exists(path))
        //                        Directory.Exists(path);
        //                }
        //                else
        //                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} The runtime with id {runtimeResult.Result.Id} was not found!");
        //            }
        //            else
        //                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the Runtime with Data.LoadHolonByMetaData. Reason: {runtimeResult.Message}");
        //        }
        //        else
        //            OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the parent with OAPPManagerBase.LoadHolonByMetaData. Reason: {parentResult.Message}");
        //    }
        //    catch (Exception ex)
        //    {
        //        OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured. Reason: {ex}");
        //    }

        //    return result;
        //}

        //public async Task<OASISResult<T1>> RemoveLibraryAsync(Guid avatarId, T1 parent, InstalledLibrary installedLibrary, ProviderType providerType = ProviderType.Default)
        //{
        //    OASISResult<T1> result = new OASISResult<T1>();
        //    string errorMessage = "Error occured in OAPPManagerBase.RemoveLibraryAsync. Reason:";

        //    try
        //    {
        //        OASISResult<Library> libResult = await Data.LoadHolonByMetaDataAsync<Library>(new Dictionary<string, string>()
        //        {
        //            { "STARNETHolonId", installedLibrary.STARNETDNA.Id.ToString() },
        //            { "Version", installedLibrary.STARNETDNA.Version }

        //        }, MetaKeyValuePairMatchMode.All, HolonType.Library, providerType: providerType);

        //        if (libResult != null && libResult.Result != null && !libResult.IsError)
        //        {
        //            string installPath = Path.Combine(parent.STARNETDNA.SourcePath, "Dependencies", "STARNET", "Libs", string.Concat(libResult.Result.STARNETDNA.Name, "_v", libResult.Result.STARNETDNA.Version));

        //            if (!parent.STARNETDNA.Dependencies.Libraries.Any(x => x.HolonId == libResult.Result.Id))
        //            {
        //                parent.STARNETDNA.Dependencies.Libraries.Remove(new STARNETDependency()
        //                {
        //                    HolonId = libResult.Result.Id,
        //                    STARNETHolonId = libResult.Result.STARNETDNA.Id,
        //                    Name = libResult.Result.Name,
        //                    Description = libResult.Result.Description,
        //                    VersionSequence = libResult.Result.STARNETDNA.VersionSequence,
        //                    Version = libResult.Result.STARNETDNA.Version,
        //                    InstalledFrom = installedLibrary.InstalledPath,
        //                    InstalledTo = installPath
        //                });

        //                result = Update(avatarId, parent, result, errorMessage, true, string.Concat(Enum.GetName(typeof(HolonType), parent.HolonType), "DNAJSON"), providerType: providerType);

        //                if (result != null && result.Result != null && !result.IsError)
        //                    DirectoryHelper.CopyFilesRecursively(installedLibrary.InstalledPath, installPath);
        //            }
        //            else
        //                OASISErrorHandling.HandleError(ref result, $"{errorMessage} The library {libResult.Result.Name} has already been added to {parent.STARNETDNA.Name}.");
        //        }
        //        else
        //            OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the Library with Data.LoadHolonByMetaDataAsync. Reason: {libResult.Message}");
        //    }
        //    catch (Exception ex)
        //    {
        //        OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured. Reason: {ex}");
        //    }

        //    return result;
        //}

        //public OASISResult<T1> RemoveLibrary(Guid avatarId, T1 parent, InstalledLibrary installedLibrary, ProviderType providerType = ProviderType.Default)
        //{
        //    OASISResult<T1> result = new OASISResult<T1>();
        //    string errorMessage = "Error occured in OAPPManagerBase.RemoveLibrary. Reason:";

        //    try
        //    {
        //        OASISResult<Library> libResult = Data.LoadHolonByMetaData<Library>(new Dictionary<string, string>()
        //        {
        //            { "STARNETHolonId", installedLibrary.STARNETDNA.Id.ToString() },
        //            { "Version", installedLibrary.STARNETDNA.Version }

        //        }, MetaKeyValuePairMatchMode.All, HolonType.Library, providerType: providerType);

        //        if (libResult != null && libResult.Result != null && !libResult.IsError)
        //        {
        //            string installPath = Path.Combine(parent.STARNETDNA.SourcePath, "Dependencies", "STARNET", "Libs", string.Concat(libResult.Result.STARNETDNA.Name, "_v", libResult.Result.STARNETDNA.Version));

        //            if (!parent.STARNETDNA.Dependencies.Libraries.Any(x => x.HolonId == libResult.Result.Id))
        //            {
        //                parent.STARNETDNA.Dependencies.Libraries.Remove(new STARNETDependency()
        //                {
        //                    HolonId = libResult.Result.Id,
        //                    STARNETHolonId = libResult.Result.STARNETDNA.Id,
        //                    Name = libResult.Result.Name,
        //                    Description = libResult.Result.Description,
        //                    VersionSequence = libResult.Result.STARNETDNA.VersionSequence,
        //                    Version = libResult.Result.STARNETDNA.Version,
        //                    InstalledFrom = installedLibrary.InstalledPath,
        //                    InstalledTo = installPath
        //                });

        //                result = Update(avatarId, parent, result, errorMessage, true, string.Concat(Enum.GetName(typeof(HolonType), parent.HolonType), "DNAJSON"), providerType: providerType);

        //                if (result != null && result.Result != null && !result.IsError)
        //                    DirectoryHelper.CopyFilesRecursively(installedLibrary.InstalledPath, installPath);
        //            }
        //            else
        //                OASISErrorHandling.HandleError(ref result, $"{errorMessage} The library {libResult.Result.Name} has already been added to {parent.STARNETDNA.Name}.");
        //        }
        //        else
        //            OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the Library with Data.LoadHolonByMetaData. Reason: {libResult.Message}");
        //    }
        //    catch (Exception ex)
        //    {
        //        OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured. Reason: {ex}");
        //    }

        //    return result;
        //}

        //public async Task<OASISResult<T1>> RemoveLibraryAsync(Guid avatarId, Guid parentId, string parentVersion, InstalledLibrary installedLibrary, ProviderType providerType = ProviderType.Default)
        //{
        //    OASISResult<T1> result = new OASISResult<T1>();
        //    string errorMessage = "Error occured in OAPPManagerBase.RemoveLibraryAsync. Reason:";

        //    try
        //    {
        //        OASISResult<T1> parentResult = await Data.LoadHolonByMetaDataAsync<T1>(new Dictionary<string, string>()
        //        {
        //            { STARNETHolonIdName, parentId.ToString() },
        //            { "Version", parentVersion }

        //        }, MetaKeyValuePairMatchMode.All, STARNETHolonType, providerType: providerType);

        //        if (parentResult != null && parentResult.Result != null && !parentResult.IsError)
        //            return await RemoveLibraryAsync(avatarId, parentResult.Result, installedLibrary, providerType);
        //        else
        //            OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the parent with OAPPManagerBase.LoadHolonByMetaDataAsync. Reason: {parentResult.Message}");
        //    }
        //    catch (Exception ex)
        //    {
        //        OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured. Reason: {ex}");
        //    }

        //    return result;
        //}

        //public OASISResult<T1> RemoveLibrary(Guid avatarId, Guid parentId, string parentVersion, InstalledLibrary installedLibrary, ProviderType providerType = ProviderType.Default)
        //{
        //    OASISResult<T1> result = new OASISResult<T1>();
        //    string errorMessage = "Error occured in OAPPManagerBase.RemoveLibrary. Reason:";

        //    try
        //    {
        //        OASISResult<T1> parentResult = Data.LoadHolonByMetaData<T1>(new Dictionary<string, string>()
        //        {
        //            { STARNETHolonIdName, parentId.ToString() },
        //            { "Version", parentVersion }

        //        }, MetaKeyValuePairMatchMode.All, STARNETHolonType, providerType: providerType);

        //        if (parentResult != null && parentResult.Result != null && !parentResult.IsError)
        //            return RemoveLibrary(avatarId, parentResult.Result, installedLibrary, providerType);
        //        else
        //            OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the {STARNETHolonUIName}  with OAPPManagerBase.LoadHolonByMetaData. Reason: {parentResult.Message}");
        //    }
        //    catch (Exception ex)
        //    {
        //        OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured. Reason: {ex}");
        //    }

        //    return result;
        //}

        //public async Task<OASISResult<T1>> RemoveLibraryAsync(Guid avatarId, Guid parentId, int parentVersionSequence, InstalledLibrary installedLibrary, ProviderType providerType = ProviderType.Default)
        //{
        //    OASISResult<T1> result = new OASISResult<T1>();
        //    string errorMessage = "Error occured in OAPPManagerBase.RemoveLibraryAsync. Reason:";

        //    try
        //    {
        //        OASISResult<T1> parentResult = await Data.LoadHolonByMetaDataAsync<T1>(new Dictionary<string, string>()
        //        {
        //            { STARNETHolonIdName, parentId.ToString() },
        //            { "VersionSequence", parentVersionSequence.ToString() }

        //        }, MetaKeyValuePairMatchMode.All, STARNETHolonType, providerType: providerType);

        //        if (parentResult != null && parentResult.Result != null && !parentResult.IsError)
        //            return await RemoveLibraryAsync(avatarId, parentResult.Result, installedLibrary, providerType);
        //        else
        //            OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the parent with OAPPManagerBase.LoadHolonByMetaDataAsync. Reason: {parentResult.Message}");
        //    }
        //    catch (Exception ex)
        //    {
        //        OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured. Reason: {ex}");
        //    }

        //    return result;
        //}

        //public OASISResult<T1> RemoveLibrary(Guid avatarId, Guid parentId, int parentVersionSequence, InstalledLibrary installedLibrary, ProviderType providerType = ProviderType.Default)
        //{
        //    OASISResult<T1> result = new OASISResult<T1>();
        //    string errorMessage = "Error occured in OAPPManagerBase.RemoveLibrary. Reason:";

        //    try
        //    {
        //        OASISResult<T1> parentResult = Data.LoadHolonByMetaData<T1>(new Dictionary<string, string>()
        //        {
        //            { STARNETHolonIdName, parentId.ToString() },
        //            { "VersionSequence", parentVersionSequence.ToString() }

        //        }, MetaKeyValuePairMatchMode.All, STARNETHolonType, providerType: providerType);

        //        if (parentResult != null && parentResult.Result != null && !parentResult.IsError)
        //            return RemoveLibrary(avatarId, parentResult.Result, installedLibrary, providerType);
        //        else
        //            OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the parent with OAPPManagerBase.LoadHolonByMetaData. Reason: {parentResult.Message}");
        //    }
        //    catch (Exception ex)
        //    {
        //        OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured. Reason: {ex}");
        //    }

        //    return result;
        //}

        //public async Task<OASISResult<T1>> RemoveLibraryAsync(Guid avatarId, Guid parentId, int parentVersionSequence, Guid libraryId, int libraryVersionSequence, ProviderType providerType = ProviderType.Default)
        //{
        //    OASISResult<T1> result = new OASISResult<T1>();
        //    string errorMessage = "Error occured in OAPPManagerBase.RemoveLibraryAsync. Reason:";

        //    OASISResult<InstalledLibrary> installedLibResult = await Data.LoadHolonByMetaDataAsync<InstalledLibrary>(new Dictionary<string, string>()
        //    {
        //        { "STARNETHolonId", libraryId.ToString() },
        //        { "VersionSequence", libraryVersionSequence.ToString() }

        //    }, MetaKeyValuePairMatchMode.All, HolonType.InstalledLibrary, providerType: providerType);

        //    if (installedLibResult != null && installedLibResult.Result != null && !installedLibResult.IsError)
        //        result = await RemoveLibraryAsync(avatarId, parentId, parentVersionSequence, installedLibResult.Result, providerType);
        //    else
        //        OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the Installed Library with Data.LoadHolonByMetaDataAsync. Reason: {installedLibResult.Message}");

        //    return result;
        //}

        //public OASISResult<T1> RemoveLibrary(Guid avatarId, Guid parentId, int parentVersionSequence, Guid libraryId, int libraryVersionSequence, ProviderType providerType = ProviderType.Default)
        //{
        //    OASISResult<T1> result = new OASISResult<T1>();
        //    string errorMessage = "Error occured in OAPPManagerBase.RemoveLibrary. Reason:";

        //    OASISResult<InstalledLibrary> installedLibResult = Data.LoadHolonByMetaData<InstalledLibrary>(new Dictionary<string, string>()
        //    {
        //        { "STARNETHolonId", libraryId.ToString() },
        //        { "VersionSequence", libraryVersionSequence.ToString() }

        //    }, MetaKeyValuePairMatchMode.All, HolonType.InstalledLibrary, providerType: providerType);

        //    if (installedLibResult != null && installedLibResult.Result != null && !installedLibResult.IsError)
        //        result = RemoveLibrary(avatarId, parentId, parentVersionSequence, installedLibResult.Result, providerType);
        //    else
        //        OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the Installed Library with Data.LoadHolonByMetaData. Reason: {installedLibResult.Message}");

        //    return result;
        //}

        //public async Task<OASISResult<T1>> RemoveLibraryAsync(Guid avatarId, Guid parentId, string parentVersion, Guid libraryId, string libraryVersion, ProviderType providerType = ProviderType.Default)
        //{
        //    OASISResult<T1> result = new OASISResult<T1>();
        //    string errorMessage = "Error occured in OAPPManagerBase.RemoveLibraryAsync. Reason:";

        //    OASISResult<InstalledLibrary> installedLibResult = await Data.LoadHolonByMetaDataAsync<InstalledLibrary>(new Dictionary<string, string>()
        //    {
        //        { "STARNETHolonId", libraryId.ToString() },
        //        { "Version", libraryVersion }

        //    }, MetaKeyValuePairMatchMode.All, HolonType.InstalledLibrary, providerType: providerType);

        //    if (installedLibResult != null && installedLibResult.Result != null && !installedLibResult.IsError)
        //        result = await RemoveLibraryAsync(avatarId, parentId, parentVersion, installedLibResult.Result, providerType);
        //    else
        //        OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the Installed Library with Data.LoadHolonByMetaDataAsync. Reason: {installedLibResult.Message}");

        //    return result;
        //}

        //public OASISResult<T1> RemoveLibrary(Guid avatarId, Guid parentId, string parentVersion, Guid libraryId, string libraryVersion, ProviderType providerType = ProviderType.Default)
        //{
        //    OASISResult<T1> result = new OASISResult<T1>();
        //    string errorMessage = "Error occured in OAPPManagerBase.RemoveLibrary. Reason:";

        //    OASISResult<InstalledLibrary> installedLibResult = Data.LoadHolonByMetaData<InstalledLibrary>(new Dictionary<string, string>()
        //    {
        //        { "STARNETHolonId", libraryId.ToString() },
        //        { "Version", libraryVersion }

        //    }, MetaKeyValuePairMatchMode.All, HolonType.InstalledLibrary, providerType: providerType);

        //    if (installedLibResult != null && installedLibResult.Result != null && !installedLibResult.IsError)
        //        result = RemoveLibrary(avatarId, parentId, parentVersion, installedLibResult.Result, providerType);
        //    else
        //        OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the Installed Library with Data.LoadHolonByMetaData. Reason: {installedLibResult.Message}");

        //    return result;
        //}


        //public async Task<OASISResult<T1>> RemoveLibraryAsync(Guid avatarId, Guid parentId, int parentVersionSequence, Guid libraryId, int libraryVersionSequence, ProviderType providerType = ProviderType.Default)
        //{
        //    OASISResult<T1> result = new OASISResult<T1>();
        //    string errorMessage = "Error occured in OAPPManagerBase.RemoveLibraryAsync. Reason:";

        //    try
        //    {
        //        OASISResult<T1> parentResult = await LoadAsync(avatarId, parentId, parentVersionSequence, providerType: providerType);

        //        if (parentResult != null && parentResult.Result != null && !parentResult.IsError)
        //        {
        //            OASISResult<Library> libResult = await Data.LoadHolonByMetaDataAsync<Library>(new Dictionary<string, string>()
        //            {
        //                { "STARNETHolonId", libraryId.ToString() },
        //                { "VersionSequence", libraryVersionSequence.ToString() }

        //            }, MetaKeyValuePairMatchMode.All, HolonType.Library, providerType: providerType);

        //            if (libResult != null && libResult.Result != null && !libResult.IsError)
        //            {
        //                STARNETDependency dependency = parentResult.Result.STARNETDNA.Dependencies.Libraries.FirstOrDefault(x => x.HolonId == libResult.Result.Id);

        //                if (dependency != null)
        //                {
        //                    parentResult.Result.STARNETDNA.Dependencies.Libraries.Remove(dependency);
        //                    result = await UpdateAsync(avatarId, parentResult.Result, result, errorMessage, true, string.Concat(Enum.GetName(typeof(HolonType), parentResult.Result.HolonType), "DNAJSON"), providerType: providerType);

        //                    string path = Path.Combine(parentResult.Result.STARNETDNA.SourcePath, "Dependencies", "STARNET", "Libs", string.Concat(libResult.Result.STARNETDNA.Name, "_v", libResult.Result.STARNETDNA.Version));

        //                    if (Directory.Exists(path))
        //                        Directory.Exists(path);
        //                }
        //                else
        //                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} The library with id {libResult.Result.Id} was not found!");
        //            }
        //            else
        //                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the Library with Data.LoadHolonByMetaDataAsync. Reason: {libResult.Message}");
        //        }
        //        else
        //            OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the {STARNETHolonUIName}  with OAPPManagerBase.LoadHolonByMetaDataAsync. Reason: {parentResult.Message}");
        //    }
        //    catch (Exception ex)
        //    {
        //        OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured. Reason: {ex}");
        //    }

        //    return result;
        //}

        //public OASISResult<T1> RemoveLibrary(Guid avatarId, Guid parentId, int parentVersionSequence, Guid libraryId, int libraryVersionSequnce, ProviderType providerType = ProviderType.Default)
        //{
        //    OASISResult<T1> result = new OASISResult<T1>();
        //    string errorMessage = "Error occured in OAPPManagerBase.RemoveLibrary. Reason:";

        //    try
        //    {
        //        OASISResult<T1> parentResult = Load(avatarId, parentId, parentVersionSequence, providerType: providerType);

        //        if (parentResult != null && parentResult.Result != null && !parentResult.IsError)
        //        {
        //            OASISResult<Library> libResult = Data.LoadHolonByMetaData<Library>(new Dictionary<string, string>()
        //            {
        //                { "STARNETHolonId", libraryId.ToString() },
        //                { "VersionSequence", libraryVersionSequnce.ToString() }

        //            }, MetaKeyValuePairMatchMode.All, HolonType.Library, providerType: providerType);

        //            if (libResult != null && libResult.Result != null && !libResult.IsError)
        //            {
        //                STARNETDependency dependency = parentResult.Result.STARNETDNA.Dependencies.Libraries.FirstOrDefault(x => x.HolonId == libResult.Result.Id);

        //                if (dependency != null)
        //                {
        //                    parentResult.Result.STARNETDNA.Dependencies.Libraries.Remove(dependency);
        //                    result = Update(avatarId, parentResult.Result, result, errorMessage, true, providerType: providerType);

        //                    string path = Path.Combine(parentResult.Result.STARNETDNA.SourcePath, "Dependencies", "STARNET", "Libs", string.Concat(libResult.Result.STARNETDNA.Name, "_v", libResult.Result.STARNETDNA.Version));

        //                    if (Directory.Exists(path))
        //                        Directory.Exists(path);
        //                }
        //                else
        //                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} The library with id {libResult.Result.Id} was not found!");
        //            }
        //            else
        //                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the Library with Data.LoadHolonByMetaData. Reason: {libResult.Message}");
        //        }
        //        else
        //            OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the {STARNETHolonUIName}  with OAPPManagerBase.LoadHolonByMetaData. Reason: {parentResult.Message}");
        //    }
        //    catch (Exception ex)
        //    {
        //        OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured. Reason: {ex}");
        //    }

        //    return result;
        //}

        //public async Task<OASISResult<T1>> RemoveLibraryAsync(Guid avatarId, Guid parentId, string parentVersion, Guid libraryId, string libraryVersion, ProviderType providerType = ProviderType.Default)
        //{
        //    OASISResult<T1> result = new OASISResult<T1>();
        //    string errorMessage = "Error occured in OAPPManagerBase.RemoveLibraryAsync. Reason:";

        //    try
        //    {
        //        OASISResult<T1> parentResult = await Data.LoadHolonByMetaDataAsync<T1>(new Dictionary<string, string>()
        //        {
        //            { STARNETHolonIdName, parentId.ToString() },
        //            { "Version", parentVersion }

        //        }, MetaKeyValuePairMatchMode.All, STARNETHolonType, providerType: providerType);

        //        if (parentResult != null && parentResult.Result != null && !parentResult.IsError)
        //        {
        //            OASISResult<Library> libResult = await Data.LoadHolonByMetaDataAsync<Library>(new Dictionary<string, string>()
        //            {
        //                //{ "LibraryId", libraryId.ToString() },
        //                { "STARNETHolonId", libraryId.ToString() },
        //                { "Version", libraryVersion }

        //            }, MetaKeyValuePairMatchMode.All, HolonType.Library, providerType: providerType);

        //            if (libResult != null && libResult.Result != null && !libResult.IsError)
        //            {
        //                STARNETDependency dependency = parentResult.Result.STARNETDNA.Dependencies.Libraries.FirstOrDefault(x => x.HolonId == libResult.Result.Id);

        //                if (dependency != null)
        //                {
        //                    parentResult.Result.STARNETDNA.Dependencies.Libraries.Remove(dependency);
        //                    result = await UpdateAsync(avatarId, parentResult.Result, result, errorMessage, true, providerType: providerType);

        //                    string path = Path.Combine(parentResult.Result.STARNETDNA.SourcePath, "Dependencies", "STARNET", "Libs", string.Concat(libResult.Result.STARNETDNA.Name, "_v", libResult.Result.STARNETDNA.Version));

        //                    if (Directory.Exists(path))
        //                        Directory.Exists(path);
        //                }
        //                else
        //                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} The library with id {libResult.Result.Id} was not found!");
        //            }
        //            else
        //                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the Library with Data.LoadHolonByMetaDataAsync. Reason: {libResult.Message}");
        //        }
        //        else
        //            OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the {STARNETHolonUIName}  with OAPPManagerBase.LoadHolonByMetaDataAsync. Reason: {parentResult.Message}");
        //    }
        //    catch (Exception ex)
        //    {
        //        OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured. Reason: {ex}");
        //    }

        //    return result;
        //}

        //public OASISResult<T1> RemoveLibrary(Guid avatarId, Guid parentId, string parentVersion, Guid libraryId, string libraryVersion, ProviderType providerType = ProviderType.Default)
        //{
        //    OASISResult<T1> result = new OASISResult<T1>();
        //    string errorMessage = "Error occured in OAPPManagerBase.RemoveLibrary. Reason:";

        //    try
        //    {
        //        OASISResult<T1> parentResult = Data.LoadHolonByMetaData<T1>(new Dictionary<string, string>()
        //        {
        //            { STARNETHolonIdName, parentId.ToString() },
        //            { "Version", parentVersion }

        //        }, MetaKeyValuePairMatchMode.All, STARNETHolonType, providerType: providerType);

        //        if (parentResult != null && parentResult.Result != null && !parentResult.IsError)
        //        {
        //            OASISResult<Library> libResult = Data.LoadHolonByMetaData<Library>(new Dictionary<string, string>()
        //            {
        //                { "STARNETHolonId", libraryId.ToString() },
        //                { "Version", libraryVersion.ToString() }

        //            }, MetaKeyValuePairMatchMode.All, HolonType.Library, providerType: providerType);

        //            if (libResult != null && libResult.Result != null && !libResult.IsError)
        //            {
        //                STARNETDependency dependency = parentResult.Result.STARNETDNA.Dependencies.Libraries.FirstOrDefault(x => x.HolonId == libResult.Result.Id);

        //                if (dependency != null)
        //                {
        //                    parentResult.Result.STARNETDNA.Dependencies.Libraries.Remove(dependency);
        //                    result = Update(avatarId, parentResult.Result, result, errorMessage, true, string.Concat(Enum.GetName(typeof(HolonType), parentResult.Result.HolonType), "DNAJSON"), providerType: providerType);

        //                    string path = Path.Combine(parentResult.Result.STARNETDNA.SourcePath, "Dependencies", "STARNET", "Libs", string.Concat(libResult.Result.STARNETDNA.Name, "_v", libResult.Result.STARNETDNA.Version));

        //                    if (Directory.Exists(path))
        //                        Directory.Exists(path);
        //                }
        //                else
        //                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} The library with id {libResult.Result.Id} was not found!");
        //            }
        //            else
        //                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the Library with Data.LoadHolonByMetaData. Reason: {libResult.Message}");
        //        }
        //        else
        //            OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the {STARNETHolonUIName} with OAPPManagerBase.LoadHolonByMetaData. Reason: {parentResult.Message}");
        //    }
        //    catch (Exception ex)
        //    {
        //        OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured. Reason: {ex}");
        //    }

        //    return result;
        //}

        //public async Task<OASISResult<T1>> RemoveOAPPTemplateAsync(Guid avatarId, T1 parent, IInstalledOAPPTemplate installedOAPPTemplate, ProviderType providerType = ProviderType.Default)
        //{
        //    OASISResult<T1> result = new OASISResult<T1>();
        //    string errorMessage = "Error occured in OAPPManagerBase.RemoveOAPPTemplateAsync. Reason:";

        //    try
        //    {
        //        OASISResult<OAPPTemplate> templateResult = await Data.LoadHolonByMetaDataAsync<OAPPTemplate>(new Dictionary<string, string>()
        //        {
        //            { "STARNETHolonId", installedOAPPTemplate.STARNETDNA.Id.ToString() },
        //            { "Version", installedOAPPTemplate.STARNETDNA.Version }

        //        }, MetaKeyValuePairMatchMode.All, HolonType.OAPPTemplate, providerType: providerType);

        //        if (templateResult != null && templateResult.Result != null && !templateResult.IsError)
        //        {
        //            string installPath = Path.Combine(parent.STARNETDNA.SourcePath, "Dependencies", "STARNET", "Templates", string.Concat(templateResult.Result.STARNETDNA.Name, "_v", templateResult.Result.STARNETDNA.Version));

        //            if (!parent.STARNETDNA.Dependencies.Templates.Any(x => x.HolonId == templateResult.Result.Id))
        //            {
        //                parent.STARNETDNA.Dependencies.Templates.Remove(new STARNETDependency()
        //                {
        //                    HolonId = templateResult.Result.Id,
        //                    STARNETHolonId = templateResult.Result.STARNETDNA.Id,
        //                    Name = templateResult.Result.Name,
        //                    Description = templateResult.Result.Description,
        //                    VersionSequence = templateResult.Result.STARNETDNA.VersionSequence,
        //                    Version = templateResult.Result.STARNETDNA.Version,
        //                    InstalledFrom = installedOAPPTemplate.InstalledPath,
        //                    InstalledTo = installPath
        //                });

        //                result = Update(avatarId, parent, result, errorMessage, true, string.Concat(Enum.GetName(typeof(HolonType), parent.HolonType), "DNAJSON"), providerType: providerType);

        //                if (result != null && result.Result != null && !result.IsError)
        //                    DirectoryHelper.CopyFilesRecursively(installedOAPPTemplate.InstalledPath, installPath);
        //            }
        //            else
        //                OASISErrorHandling.HandleError(ref result, $"{errorMessage} The template {templateResult.Result.Name} has already been added to {parent.STARNETDNA.Name}.");
        //        }
        //        else
        //            OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the OAPP Template with Data.LoadHolonByMetaDataAsync. Reason: {templateResult.Message}");
        //    }
        //    catch (Exception ex)
        //    {
        //        OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured. Reason: {ex}");
        //    }

        //    return result;
        //}

        //public OASISResult<T1> RemoveOAPPTemplate(Guid avatarId, T1 parent, IInstalledOAPPTemplate installedOAPPTemplate, ProviderType providerType = ProviderType.Default)
        //{
        //    OASISResult<T1> result = new OASISResult<T1>();
        //    string errorMessage = "Error occured in OAPPManagerBase.RemoveOAPPTemplate. Reason:";

        //    try
        //    {
        //        OASISResult<OAPPTemplate> templateResult = Data.LoadHolonByMetaData<OAPPTemplate>(new Dictionary<string, string>()
        //        {
        //            { "STARNETHolonId", installedOAPPTemplate.STARNETDNA.Id.ToString() },
        //            { "Version", installedOAPPTemplate.STARNETDNA.Version }

        //        }, MetaKeyValuePairMatchMode.All, HolonType.OAPPTemplate, providerType: providerType);

        //        if (templateResult != null && templateResult.Result != null && !templateResult.IsError)
        //        {
        //            string installPath = Path.Combine(parent.STARNETDNA.SourcePath, "Dependencies", "STARNET", "Templates", string.Concat(templateResult.Result.STARNETDNA.Name, "_v", templateResult.Result.STARNETDNA.Version));

        //            if (!parent.STARNETDNA.Dependencies.Templates.Any(x => x.HolonId == templateResult.Result.Id))
        //            {
        //                parent.STARNETDNA.Dependencies.Templates.Remove(new STARNETDependency()
        //                {
        //                    HolonId = templateResult.Result.Id,
        //                    STARNETHolonId = templateResult.Result.STARNETDNA.Id,
        //                    Name = templateResult.Result.Name,
        //                    Description = templateResult.Result.Description,
        //                    VersionSequence = templateResult.Result.STARNETDNA.VersionSequence,
        //                    Version = templateResult.Result.STARNETDNA.Version,
        //                    InstalledFrom = installedOAPPTemplate.InstalledPath,
        //                    InstalledTo = installPath
        //                });

        //                result = Update(avatarId, parent, result, errorMessage, true, string.Concat(Enum.GetName(typeof(HolonType), parent.HolonType), "DNAJSON"), providerType: providerType);

        //                if (result != null && result.Result != null && !result.IsError)
        //                    DirectoryHelper.CopyFilesRecursively(installedOAPPTemplate.InstalledPath, installPath);
        //            }
        //            else
        //                OASISErrorHandling.HandleError(ref result, $"{errorMessage} The template {templateResult.Result.Name} has already been added to {parent.STARNETDNA.Name}.");
        //        }
        //        else
        //            OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the OAPP Template with Data.LoadHolonByMetaData. Reason: {templateResult.Message}");
        //    }
        //    catch (Exception ex)
        //    {
        //        OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured. Reason: {ex}");
        //    }

        //    return result;
        //}

        //public async Task<OASISResult<T1>> RemoveOAPPTemplateAsync(Guid avatarId, Guid parentId, string parentVersion, IInstalledOAPPTemplate installedOAPPTemplate, ProviderType providerType = ProviderType.Default)
        //{
        //    OASISResult<T1> result = new OASISResult<T1>();
        //    string errorMessage = "Error occured in OAPPManagerBase.RemoveOAPPTemplateAsync. Reason:";

        //    try
        //    {
        //        OASISResult<T1> parentResult = await Data.LoadHolonByMetaDataAsync<T1>(new Dictionary<string, string>()
        //        {
        //            { STARNETHolonIdName, parentId.ToString() },
        //            { "Version", parentVersion }

        //        }, MetaKeyValuePairMatchMode.All, STARNETHolonType, providerType: providerType);

        //        if (parentResult != null && parentResult.Result != null && !parentResult.IsError)
        //            return await RemoveOAPPTemplateAsync(avatarId, parentResult.Result, installedOAPPTemplate, providerType);
        //        else
        //            OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the parent OAPPManagerBase.LoadHolonByMetaDataAsync. Reason: {parentResult.Message}");
        //    }
        //    catch (Exception ex)
        //    {
        //        OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured. Reason: {ex}");
        //    }

        //    return result;
        //}

        //public OASISResult<T1> RemoveOAPPTemplate(Guid avatarId, Guid parentId, string parentVersion, IInstalledOAPPTemplate installedOAPPTemplate, ProviderType providerType = ProviderType.Default)
        //{
        //    OASISResult<T1> result = new OASISResult<T1>();
        //    string errorMessage = "Error occured in OAPPManagerBase.RemoveOAPPTemplate. Reason:";

        //    try
        //    {
        //        OASISResult<T1> parentResult = Data.LoadHolonByMetaData<T1>(new Dictionary<string, string>()
        //        {
        //            { STARNETHolonIdName, parentId.ToString() },
        //            { "Version", parentVersion }

        //        }, MetaKeyValuePairMatchMode.All, STARNETHolonType, providerType: providerType);

        //        if (parentResult != null && parentResult.Result != null && !parentResult.IsError)
        //            return RemoveOAPPTemplate(avatarId, parentResult.Result, installedOAPPTemplate, providerType);
        //        else
        //            OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the parent with OAPPManagerBase.LoadHolonByMetaData. Reason: {parentResult.Message}");
        //    }
        //    catch (Exception ex)
        //    {
        //        OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured. Reason: {ex}");
        //    }

        //    return result;
        //}

        //public async Task<OASISResult<T1>> RemoveOAPPTemplateAsync(Guid avatarId, Guid parentId, int parentVersionSequence, IInstalledOAPPTemplate installedOAPPTemplate, ProviderType providerType = ProviderType.Default)
        //{
        //    OASISResult<T1> result = new OASISResult<T1>();
        //    string errorMessage = "Error occured in OAPPManagerBase.RemoveOAPPTemplateAsync. Reason:";

        //    try
        //    {
        //        OASISResult<T1> parentResult = await Data.LoadHolonByMetaDataAsync<T1>(new Dictionary<string, string>()
        //        {
        //            { STARNETHolonIdName, parentId.ToString() },
        //            { "VersionSequence", parentVersionSequence.ToString() }

        //        }, MetaKeyValuePairMatchMode.All, STARNETHolonType, providerType: providerType);

        //        if (parentResult != null && parentResult.Result != null && !parentResult.IsError)
        //            return await RemoveOAPPTemplateAsync(avatarId, parentResult.Result, installedOAPPTemplate, providerType);
        //        else
        //            OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the parent with OAPPManagerBase.LoadHolonByMetaDataAsync. Reason: {parentResult.Message}");
        //    }
        //    catch (Exception ex)
        //    {
        //        OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured. Reason: {ex}");
        //    }

        //    return result;
        //}

        //public OASISResult<T1> RemoveOAPPTemplate(Guid avatarId, Guid parentId, int parentVersionSequence, IInstalledOAPPTemplate installedOAPPTemplate, ProviderType providerType = ProviderType.Default)
        //{
        //    OASISResult<T1> result = new OASISResult<T1>();
        //    string errorMessage = "Error occured in OAPPManagerBase.RemoveOAPPTemplate. Reason:";

        //    try
        //    {
        //        OASISResult<T1> parentResult = Data.LoadHolonByMetaData<T1>(new Dictionary<string, string>()
        //        {
        //            { STARNETHolonIdName, parentId.ToString() },
        //            { "VersionSequence", parentVersionSequence.ToString() }

        //        }, MetaKeyValuePairMatchMode.All, STARNETHolonType, providerType: providerType);

        //        if (parentResult != null && parentResult.Result != null && !parentResult.IsError)
        //            return RemoveOAPPTemplate(avatarId, parentResult.Result, installedOAPPTemplate, providerType);
        //        else
        //            OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the {STARNETHolonUIName}  with OAPPManagerBase.LoadHolonByMetaData. Reason: {parentResult.Message}");
        //    }
        //    catch (Exception ex)
        //    {
        //        OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured. Reason: {ex}");
        //    }

        //    return result;
        //}

        //public async Task<OASISResult<T1>> RemoveOAPPTemplateAsync(Guid avatarId, Guid parentId, int parentVersionSequence, Guid templateId, int subTemplateVersionSequence, ProviderType providerType = ProviderType.Default)
        //{
        //    OASISResult<T1> result = new OASISResult<T1>();
        //    string errorMessage = "Error occured in OAPPManagerBase.RemoveOAPPTemplateAsync. Reason:";

        //    OASISResult<InstalledOAPPTemplate> installedTemplateResult = await Data.LoadHolonByMetaDataAsync<InstalledOAPPTemplate>(new Dictionary<string, string>()
        //    {
        //        { "STARNETHolonId", templateId.ToString() },
        //        { "VersionSequence", subTemplateVersionSequence.ToString() }

        //    }, MetaKeyValuePairMatchMode.All, HolonType.InstalledOAPPTemplate, providerType: providerType);

        //    if (installedTemplateResult != null && installedTemplateResult.Result != null && !installedTemplateResult.IsError)
        //        result = await RemoveOAPPTemplateAsync(avatarId, parentId, parentVersionSequence, installedTemplateResult.Result, providerType);
        //    else
        //        OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the Installed OAPP Template with Data.LoadHolonByMetaDataAsync. Reason: {installedTemplateResult.Message}");

        //    return result;
        //}

        //public OASISResult<T1> RemoveOAPPTemplate(Guid avatarId, Guid parentId, int parentVersionSequence, Guid templateId, int subTemplateVersionSequence, ProviderType providerType = ProviderType.Default)
        //{
        //    OASISResult<T1> result = new OASISResult<T1>();
        //    string errorMessage = "Error occured in OAPPManagerBase.RemoveOAPPTemplate. Reason:";

        //    OASISResult<InstalledOAPPTemplate> installedTemplateResult = Data.LoadHolonByMetaData<InstalledOAPPTemplate>(new Dictionary<string, string>()
        //    {
        //        { "STARNETHolonId", templateId.ToString() },
        //        { "VersionSequence", subTemplateVersionSequence.ToString() }

        //    }, MetaKeyValuePairMatchMode.All, HolonType.InstalledOAPPTemplate, providerType: providerType);

        //    if (installedTemplateResult != null && installedTemplateResult.Result != null && !installedTemplateResult.IsError)
        //        result = RemoveOAPPTemplate(avatarId, parentId, parentVersionSequence, installedTemplateResult.Result, providerType);
        //    else
        //        OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the Installed OAPP Template with Data.LoadHolonByMetaData. Reason: {installedTemplateResult.Message}");

        //    return result;
        //}

        //public async Task<OASISResult<T1>> RemoveOAPPTemplateAsync(Guid avatarId, Guid parentId, string parentVersion, Guid templateId, string subTemplateVersion, ProviderType providerType = ProviderType.Default)
        //{
        //    OASISResult<T1> result = new OASISResult<T1>();
        //    string errorMessage = "Error occured in OAPPManagerBase.RemoveOAPPTemplateAsync. Reason:";

        //    OASISResult<InstalledOAPPTemplate> installedTemplateResult = await Data.LoadHolonByMetaDataAsync<InstalledOAPPTemplate>(new Dictionary<string, string>()
        //    {
        //        { "STARNETHolonId", templateId.ToString() },
        //        { "Version", subTemplateVersion }

        //    }, MetaKeyValuePairMatchMode.All, HolonType.InstalledOAPPTemplate, providerType: providerType);

        //    if (installedTemplateResult != null && installedTemplateResult.Result != null && !installedTemplateResult.IsError)
        //        result = await RemoveOAPPTemplateAsync(avatarId, parentId, parentVersion, installedTemplateResult.Result, providerType);
        //    else
        //        OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the Installed OAPP Template with Data.LoadHolonByMetaDataAsync. Reason: {installedTemplateResult.Message}");

        //    return result;
        //}

        //public OASISResult<T1> RemoveOAPPTemplate(Guid avatarId, Guid parentId, string parentVersion, Guid templateId, string subTemplateVersion, ProviderType providerType = ProviderType.Default)
        //{
        //    OASISResult<T1> result = new OASISResult<T1>();
        //    string errorMessage = "Error occured in OAPPManagerBase.RemoveOAPPTemplate. Reason:";

        //    OASISResult<InstalledOAPPTemplate> installedTemplateResult = Data.LoadHolonByMetaData<InstalledOAPPTemplate>(new Dictionary<string, string>()
        //    {
        //        { "STARNETHolonId", templateId.ToString() },
        //        { "Version", subTemplateVersion }

        //    }, MetaKeyValuePairMatchMode.All, HolonType.InstalledOAPPTemplate, providerType: providerType);

        //    if (installedTemplateResult != null && installedTemplateResult.Result != null && !installedTemplateResult.IsError)
        //        result = RemoveOAPPTemplate(avatarId, parentId, parentVersion, installedTemplateResult.Result, providerType);
        //    else
        //        OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the Installed OAPP Template with Data.LoadHolonByMetaData. Reason: {installedTemplateResult.Message}");

        //    return result;
        //}

        //public async Task<OASISResult<T1>> RemoveOAPPTemplateAsync(Guid avatarId, Guid parentId, int parentVersionSequence, Guid templateId, int subTemplateVersionSequence, ProviderType providerType = ProviderType.Default)
        //{
        //    OASISResult<T1> result = new OASISResult<T1>();
        //    string errorMessage = "Error occured in OAPPManagerBase.RemoveOAPPTemplateAsync. Reason:";

        //    try
        //    {
        //        OASISResult<T1> parentResult = await LoadAsync(avatarId, parentId, parentVersionSequence, providerType: providerType);

        //        if (parentResult != null && parentResult.Result != null && !parentResult.IsError)
        //        {
        //            OASISResult<OAPPTemplate> templateResult = await Data.LoadHolonByMetaDataAsync<OAPPTemplate>(new Dictionary<string, string>()
        //            {
        //                { "STARNETHolonId", templateId.ToString() },
        //                { "VersionSequence", subTemplateVersionSequence.ToString() }

        //            }, MetaKeyValuePairMatchMode.All, HolonType.OAPPTemplate, providerType: providerType);

        //            if (templateResult != null && templateResult.Result != null && !templateResult.IsError)
        //            {
        //                STARNETDependency dependency = parentResult.Result.STARNETDNA.Dependencies.Templates.FirstOrDefault(x => x.HolonId == templateResult.Result.Id);

        //                if (dependency != null)
        //                {
        //                    parentResult.Result.STARNETDNA.Dependencies.Templates.Remove(dependency);
        //                    result = await UpdateAsync(avatarId, parentResult.Result, result, errorMessage, true, string.Concat(Enum.GetName(typeof(HolonType), parentResult.Result.HolonType), "DNAJSON"), providerType: providerType);

        //                    string path = Path.Combine(parentResult.Result.STARNETDNA.SourcePath, "Dependencies", "STARNET", "Templates", string.Concat(templateResult.Result.STARNETDNA.Name, "_v", templateResult.Result.STARNETDNA.Version));

        //                    if (Directory.Exists(path))
        //                        Directory.Exists(path);
        //                }
        //                else
        //                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} The template with id {templateResult.Result.Id} was not found!");
        //            }
        //            else
        //                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the OAPP Template with Data.LoadHolonByMetaDataAsync. Reason: {templateResult.Message}");
        //        }
        //        else
        //            OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the parent with OAPPManagerBase.LoadHolonByMetaDataAsync. Reason: {parentResult.Message}");
        //    }
        //    catch (Exception ex)
        //    {
        //        OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured. Reason: {ex}");
        //    }

        //    return result;
        //}

        //public OASISResult<T1> RemoveOAPPTemplate(Guid avatarId, Guid parentId, int parentVersionSequence, Guid templateId, int subTemplateVersionSequnce, ProviderType providerType = ProviderType.Default)
        //{
        //    OASISResult<T1> result = new OASISResult<T1>();
        //    string errorMessage = "Error occured in OAPPManagerBase.RemoveOAPPTemplate. Reason:";

        //    try
        //    {
        //        OASISResult<T1> parentResult = Load(avatarId, parentId, parentVersionSequence, providerType: providerType);

        //        if (parentResult != null && parentResult.Result != null && !parentResult.IsError)
        //        {
        //            OASISResult<OAPPTemplate> templateResult = Data.LoadHolonByMetaData<OAPPTemplate>(new Dictionary<string, string>()
        //            {
        //                { "STARNETHolonId", templateId.ToString() },
        //                { "VersionSequence", subTemplateVersionSequnce.ToString() }

        //            }, MetaKeyValuePairMatchMode.All, HolonType.OAPPTemplate, providerType: providerType);

        //            if (templateResult != null && templateResult.Result != null && !templateResult.IsError)
        //            {
        //                STARNETDependency dependency = parentResult.Result.STARNETDNA.Dependencies.Templates.FirstOrDefault(x => x.HolonId == templateResult.Result.Id);

        //                if (dependency != null)
        //                {
        //                    parentResult.Result.STARNETDNA.Dependencies.Templates.Remove(dependency);
        //                    result = Update(avatarId, parentResult.Result, result, errorMessage, true, string.Concat(Enum.GetName(typeof(HolonType), parentResult.Result.HolonType), "DNAJSON"), providerType: providerType);

        //                    string path = Path.Combine(parentResult.Result.STARNETDNA.SourcePath, "Dependencies", "STARNET", "Templates", string.Concat(templateResult.Result.STARNETDNA.Name, "_v", templateResult.Result.STARNETDNA.Version));

        //                    if (Directory.Exists(path))
        //                        Directory.Exists(path);
        //                }
        //                else
        //                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} The template with id {templateResult.Result.Id} was not found!");
        //            }
        //            else
        //                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the OAPP Template with Data.LoadHolonByMetaData. Reason: {templateResult.Message}");
        //        }
        //        else
        //            OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the parent with OAPPManagerBase.LoadHolonByMetaData. Reason: {parentResult.Message}");
        //    }
        //    catch (Exception ex)
        //    {
        //        OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured. Reason: {ex}");
        //    }

        //    return result;
        //}

        //public async Task<OASISResult<T1>> RemoveOAPPTemplateAsync(Guid avatarId, Guid parentId, string parentVersion, Guid templateId, string subTemplateVersion, ProviderType providerType = ProviderType.Default)
        //{
        //    OASISResult<T1> result = new OASISResult<T1>();
        //    string errorMessage = "Error occured in OAPPManagerBase.RemoveOAPPTemplateAsync. Reason:";

        //    try
        //    {
        //        OASISResult<T1> parentResult = await Data.LoadHolonByMetaDataAsync<T1>(new Dictionary<string, string>()
        //        {
        //            { STARNETHolonIdName, parentId.ToString() },
        //            { "Version", parentVersion }

        //        }, MetaKeyValuePairMatchMode.All, STARNETHolonType, providerType: providerType);

        //        if (parentResult != null && parentResult.Result != null && !parentResult.IsError)
        //        {
        //            OASISResult<OAPPTemplate> templateResult = await Data.LoadHolonByMetaDataAsync<OAPPTemplate>(new Dictionary<string, string>()
        //            {
        //                { "STARNETHolonId", templateId.ToString() },
        //                { "Version", subTemplateVersion }

        //            }, MetaKeyValuePairMatchMode.All, HolonType.OAPPTemplate, providerType: providerType);

        //            if (templateResult != null && templateResult.Result != null && !templateResult.IsError)
        //            {
        //                STARNETDependency dependency = parentResult.Result.STARNETDNA.Dependencies.Templates.FirstOrDefault(x => x.HolonId == templateResult.Result.Id);

        //                if (dependency != null)
        //                {
        //                    parentResult.Result.STARNETDNA.Dependencies.Templates.Remove(dependency);
        //                    result = await UpdateAsync(avatarId, parentResult.Result, result, errorMessage, true, string.Concat(Enum.GetName(typeof(HolonType), parentResult.Result.HolonType), "DNAJSON"), providerType: providerType);

        //                    string path = Path.Combine(parentResult.Result.STARNETDNA.SourcePath, "Dependencies", "STARNET", "Templates", string.Concat(templateResult.Result.STARNETDNA.Name, "_v", templateResult.Result.STARNETDNA.Version));

        //                    if (Directory.Exists(path))
        //                        Directory.Exists(path);
        //                }
        //                else
        //                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} The template with id {templateResult.Result.Id} was not found!");
        //            }
        //            else
        //                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the OAPP Template with Data.LoadHolonByMetaDataAsync. Reason: {templateResult.Message}");
        //        }
        //        else
        //            OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the parent with OAPPManagerBase.LoadHolonByMetaDataAsync. Reason: {parentResult.Message}");
        //    }
        //    catch (Exception ex)
        //    {
        //        OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured. Reason: {ex}");
        //    }

        //    return result;
        //}

        //public OASISResult<T1> RemoveOAPPTemplate(Guid avatarId, Guid parentId, string parentVersion, Guid templateId, string subTemplateVersion, ProviderType providerType = ProviderType.Default)
        //{
        //    OASISResult<T1> result = new OASISResult<T1>();
        //    string errorMessage = "Error occured in OAPPManagerBase.RemoveOAPPTemplate. Reason:";

        //    try
        //    {
        //        OASISResult<T1> parentResult = Data.LoadHolonByMetaData<T1>(new Dictionary<string, string>()
        //        {
        //            { STARNETHolonIdName, templateId.ToString() },
        //            { "Version", parentVersion }

        //        }, MetaKeyValuePairMatchMode.All, STARNETHolonType, providerType: providerType);

        //        if (parentResult != null && parentResult.Result != null && !parentResult.IsError)
        //        {
        //            OASISResult<OAPPTemplate> templateResult = Data.LoadHolonByMetaData<OAPPTemplate>(new Dictionary<string, string>()
        //            {
        //                { "STARNETHolonId", templateId.ToString() },
        //                { "Version", subTemplateVersion.ToString() }

        //            }, MetaKeyValuePairMatchMode.All, HolonType.OAPPTemplate, providerType: providerType);

        //            if (templateResult != null && templateResult.Result != null && !templateResult.IsError)
        //            {
        //                STARNETDependency dependency = parentResult.Result.STARNETDNA.Dependencies.Templates.FirstOrDefault(x => x.HolonId == templateResult.Result.Id);

        //                if (dependency != null)
        //                {
        //                    parentResult.Result.STARNETDNA.Dependencies.Templates.Remove(dependency);
        //                    result = Update(avatarId, parentResult.Result, result, errorMessage, true, string.Concat(Enum.GetName(typeof(HolonType), parentResult.Result.HolonType), "DNAJSON"), providerType: providerType);

        //                    string path = Path.Combine(parentResult.Result.STARNETDNA.SourcePath, "Dependencies", "STARNET", "Templates", string.Concat(templateResult.Result.STARNETDNA.Name, "_v", templateResult.Result.STARNETDNA.Version));

        //                    if (Directory.Exists(path))
        //                        Directory.Exists(path);
        //                }
        //                else
        //                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} The template with id {templateResult.Result.Id} was not found!");
        //            }
        //            else
        //                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the OAPP Template with Data.LoadHolonByMetaData. Reason: {templateResult.Message}");
        //        }
        //        else
        //            OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the {STARNETHolonUIName} with OAPPManagerBase.LoadHolonByMetaData. Reason: {parentResult.Message}");
        //    }
        //    catch (Exception ex)
        //    {
        //        OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured. Reason: {ex}");
        //    }

        //    return result;
        //}

        public async Task<OASISResult<T1>> AddDependencyAsync<T>(Guid avatarId, T1 parent, T installedDependency, DependencyType dependencyType, bool installDependency = true, DependencyInstallMode dependencyInstallMode = DependencyInstallMode.Nested, ProviderType providerType = ProviderType.Default) where T : IInstalledSTARNETHolon, new()
        {
            OASISResult<T1> result = new OASISResult<T1>();
            string errorMessage = "Error occured in OAPPManagerBase.AddDependencyAsync. Reason:";

            try
            {
                OASISResult<(T1 parent, string installPath)> dependencyResult = AddDependency(parent, installedDependency, dependencyType, errorMessage, installDependency, dependencyInstallMode);

                if (dependencyResult != null && dependencyResult.Result.parent != null && !dependencyResult.IsError)
                {
                    result = await UpdateAsync(avatarId, parent, result, errorMessage, true, string.Concat(Enum.GetName(typeof(HolonType), parent.HolonType), "DNAJSON"), providerType: providerType);

                    if (result != null && result.Result != null && !result.IsError && installDependency)
                        DirectoryHelper.CopyFilesRecursively(installedDependency.InstalledPath, dependencyResult.Result.installPath);
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured adding the dependency with AddDependency. Reason: {dependencyResult.Message}");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured. Reason: {ex}");
            }

            return result;
        }

        public OASISResult<T1> AddDependency<T>(Guid avatarId, T1 parent, T installedDependency, DependencyType dependencyType, bool installDependency = true, DependencyInstallMode dependencyInstallMode = DependencyInstallMode.Nested, ProviderType providerType = ProviderType.Default) where T : IInstalledSTARNETHolon, new()
        {
            OASISResult<T1> result = new OASISResult<T1>();
            string errorMessage = "Error occured in OAPPManagerBase.AddDependency. Reason:";

            try
            {
                OASISResult<(T1 parent, string installPath)> dependencyResult = AddDependency(parent, installedDependency, dependencyType, errorMessage, installDependency, dependencyInstallMode);

                if (dependencyResult != null && dependencyResult.Result.parent != null && !dependencyResult.IsError)
                {
                    result = Update(avatarId, parent, result, errorMessage, true, string.Concat(Enum.GetName(typeof(HolonType), parent.HolonType), "DNAJSON"), providerType: providerType);

                    if (result != null && result.Result != null && !result.IsError && installDependency)
                        DirectoryHelper.CopyFilesRecursively(installedDependency.InstalledPath, dependencyResult.Result.installPath);
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured adding the dependency with AddDependency. Reason: {dependencyResult.Message}");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured. Reason: {ex}");
            }

            return result;
        }

        public async Task<OASISResult<T1>> AddDependencyAsync<T>(Guid avatarId, Guid parentId, string parentVersion, T installedDependency, DependencyType dependencyType, bool installDependency = true, DependencyInstallMode dependencyInstallMode = DependencyInstallMode.Nested, ProviderType providerType = ProviderType.Default) where T : IInstalledSTARNETHolon, new()
        {
            OASISResult<T1> result = new OASISResult<T1>();
            string errorMessage = "Error occured in OAPPManagerBase.AddDependencyAsync. Reason:";

            try
            {
                OASISResult<T1> parentResult = await Data.LoadHolonByMetaDataAsync<T1>(new Dictionary<string, string>()
                {
                    { STARNETHolonIdName, parentId.ToString() },
                    { "Version", parentVersion }

                }, MetaKeyValuePairMatchMode.All, STARNETHolonType, providerType: providerType);

                if (parentResult != null && parentResult.Result != null && !parentResult.IsError)
                    return await AddDependencyAsync(avatarId, parentResult.Result, installedDependency, dependencyType, installDependency, dependencyInstallMode, providerType);
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the parent with OAPPManagerBase.LoadHolonByMetaDataAsync. Reason: {parentResult.Message}");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured. Reason: {ex}");
            }

            return result;
        }

        public OASISResult<T1> AddDependency<T>(Guid avatarId, Guid parentId, string parentVersion, T installedDependency, DependencyType dependencyType, bool installDependency = true, DependencyInstallMode dependencyInstallMode = DependencyInstallMode.Nested, ProviderType providerType = ProviderType.Default) where T : IInstalledSTARNETHolon, new()
        {
            OASISResult<T1> result = new OASISResult<T1>();
            string errorMessage = "Error occured in OAPPManagerBase.AddDependency. Reason:";

            try
            {
                OASISResult<T1> parentResult = Data.LoadHolonByMetaData<T1>(new Dictionary<string, string>()
                {
                    { STARNETHolonIdName, parentId.ToString() },
                    { "Version", parentVersion }

                }, MetaKeyValuePairMatchMode.All, STARNETHolonType, providerType: providerType);

                if (parentResult != null && parentResult.Result != null && !parentResult.IsError)
                    return AddDependency(avatarId, parentResult.Result, installedDependency, dependencyType, installDependency, dependencyInstallMode, providerType);
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the parent with OAPPManagerBase.LoadHolonByMetaData. Reason: {parentResult.Message}");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured. Reason: {ex}");
            }

            return result;
        }

        public async Task<OASISResult<T1>> AddDependencyAsync<T>(Guid avatarId, Guid parentId, int parentVersionSequence, T installedDependency, DependencyType dependencyType, bool installDependency = true, DependencyInstallMode dependencyInstallMode = DependencyInstallMode.Nested, ProviderType providerType = ProviderType.Default) where T : IInstalledSTARNETHolon, new()
        {
            OASISResult<T1> result = new OASISResult<T1>();
            string errorMessage = "Error occured in OAPPManagerBase.AddDependencyAsync. Reason:";

            try
            {
                OASISResult<T1> parentResult = await Data.LoadHolonByMetaDataAsync<T1>(new Dictionary<string, string>()
                {
                    { STARNETHolonIdName, parentId.ToString() },
                    { "VersionSequence", parentVersionSequence.ToString() }

                }, MetaKeyValuePairMatchMode.All, STARNETHolonType, providerType: providerType);

                if (parentResult != null && parentResult.Result != null && !parentResult.IsError)
                    return await AddDependencyAsync(avatarId, parentResult.Result, installedDependency, dependencyType, installDependency, dependencyInstallMode, providerType);
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the parent with OAPPManagerBase.LoadHolonByMetaDataAsync. Reason: {parentResult.Message}");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured. Reason: {ex}");
            }

            return result;
        }

        public OASISResult<T1> AddDependency<T>(Guid avatarId, Guid parentId, int parentVersionSequence, T installedDependency, DependencyType dependencyType, bool installDependency = true, DependencyInstallMode dependencyInstallMode = DependencyInstallMode.Nested, ProviderType providerType = ProviderType.Default) where T : IInstalledSTARNETHolon, new()
        {
            OASISResult<T1> result = new OASISResult<T1>();
            string errorMessage = "Error occured in OAPPManagerBase.AddDependency. Reason:";

            try
            {
                OASISResult<T1> parentResult = Data.LoadHolonByMetaData<T1>(new Dictionary<string, string>()
                {
                    { STARNETHolonIdName, parentId.ToString() },
                    { "VersionSequence", parentVersionSequence.ToString() }

                }, MetaKeyValuePairMatchMode.All, STARNETHolonType, providerType: providerType);

                if (parentResult != null && parentResult.Result != null && !parentResult.IsError)
                    return AddDependency(avatarId, parentResult.Result, installedDependency, dependencyType, installDependency, dependencyInstallMode, providerType);
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the parent with OAPPManagerBase.LoadHolonByMetaData. Reason: {parentResult.Message}");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured. Reason: {ex}");
            }

            return result;
        }

        public async Task<OASISResult<T1>> AddDependencyAsync<T>(Guid avatarId, Guid parentId, int parentVersionSequence, Guid dependencyId, int dependencyVersionSequence, HolonType dependencyHolonType, DependencyType dependencyType, bool installDependency = true, DependencyInstallMode dependencyInstallMode = DependencyInstallMode.Nested, ProviderType providerType = ProviderType.Default) where T : IInstalledSTARNETHolon, new()
        {
            OASISResult<T1> result = new OASISResult<T1>();
            string errorMessage = "Error occured in OAPPManagerBase.AddDependencyAsync. Reason:";

            OASISResult<T> installedDependencyResult = await Data.LoadHolonByMetaDataAsync<T>(new Dictionary<string, string>()
            {
                { "STARNETHolonId", dependencyId.ToString() },
                { "VersionSequence", dependencyVersionSequence.ToString() }

            }, MetaKeyValuePairMatchMode.All, dependencyHolonType, providerType: providerType);

            if (installedDependencyResult != null && installedDependencyResult.Result != null && !installedDependencyResult.IsError)
                result = await AddDependencyAsync<T>(avatarId, parentId, parentVersionSequence, installedDependencyResult.Result, dependencyType, installDependency, dependencyInstallMode, providerType);
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the Installed Dependency with Data.LoadHolonByMetaDataAsync. Reason: {installedDependencyResult.Message}");

            return result;
        }

        public OASISResult<T1> AddDependency<T>(Guid avatarId, Guid parentId, int parentVersionSequence, Guid dependencyId, int dependencyVersionSequence, HolonType dependencyHolonType, DependencyType dependencyType, bool installDependency = true, DependencyInstallMode dependencyInstallMode = DependencyInstallMode.Nested, ProviderType providerType = ProviderType.Default) where T : IInstalledSTARNETHolon, new()
        {
            OASISResult<T1> result = new OASISResult<T1>();
            string errorMessage = "Error occured in OAPPManagerBase.AddDependency. Reason:";

            OASISResult<T> installedDependencyResult = Data.LoadHolonByMetaData<T>(new Dictionary<string, string>()
            {
                { "STARNETHolonId", dependencyId.ToString() },
                { "VersionSequence", dependencyVersionSequence.ToString() }

            }, MetaKeyValuePairMatchMode.All, dependencyHolonType, providerType: providerType);

            if (installedDependencyResult != null && installedDependencyResult.Result != null && !installedDependencyResult.IsError)
                result = AddDependency(avatarId, parentId, parentVersionSequence, installedDependencyResult.Result, dependencyType, installDependency, dependencyInstallMode, providerType);
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the Installed Dependency with Data.LoadHolonByMetaData. Reason: {installedDependencyResult.Message}");

            return result;
        }

        public async Task<OASISResult<T1>> AddDependencyAsync<T>(Guid avatarId, Guid parentId, string parentVersion, Guid dependencyId, string dependencyVersion, HolonType dependencyHolonType, DependencyType dependencyType, bool installDependency = true, DependencyInstallMode dependencyInstallMode = DependencyInstallMode.Nested, ProviderType providerType = ProviderType.Default) where T : IInstalledSTARNETHolon, new()
        {
            OASISResult<T1> result = new OASISResult<T1>();
            string errorMessage = "Error occured in OAPPManagerBase.AddDependencyAsync. Reason:";

            OASISResult<T> installedDependencyResult = await Data.LoadHolonByMetaDataAsync<T>(new Dictionary<string, string>()
            {
                { "STARNETHolonId", dependencyId.ToString() },
                { "Version", dependencyVersion}

            }, MetaKeyValuePairMatchMode.All, dependencyHolonType, providerType: providerType);

            if (installedDependencyResult != null && installedDependencyResult.Result != null && !installedDependencyResult.IsError)
                result = await AddDependencyAsync(avatarId, parentId, parentVersion, installedDependencyResult.Result, dependencyType, installDependency, dependencyInstallMode, providerType);
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the Installed Dependency with Data.LoadHolonByMetaDataAsync. Reason: {installedDependencyResult.Message}");

            return result;
        }

        public OASISResult<T1> AddDependency<T>(Guid avatarId, Guid parentId, string parentVersion, Guid dependencyId, string dependencyVersion, HolonType dependencyHolonType, DependencyType dependencyType, bool installDependency = true, DependencyInstallMode dependencyInstallMode = DependencyInstallMode.Nested, ProviderType providerType = ProviderType.Default) where T : IInstalledSTARNETHolon, new()
        {
            OASISResult<T1> result = new OASISResult<T1>();
            string errorMessage = "Error occured in OAPPManagerBase.AddDependency. Reason:";

            OASISResult<T> installedDependencyResult = Data.LoadHolonByMetaData<T>(new Dictionary<string, string>()
            {
                { "STARNETHolonId", dependencyId.ToString() },
                { "Version", dependencyVersion }

            }, MetaKeyValuePairMatchMode.All, dependencyHolonType, providerType: providerType);

            if (installedDependencyResult != null && installedDependencyResult.Result != null && !installedDependencyResult.IsError)
                result = AddDependency(avatarId, parentId, parentVersion, installedDependencyResult.Result, dependencyType, installDependency, dependencyInstallMode, providerType);
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the Installed Dependency with Data.LoadHolonByMetaData. Reason: {installedDependencyResult.Message}");

            return result;
        }


        public async Task<OASISResult<T1>> RemoveDependencyAsync<T>(Guid avatarId, T1 parent, T installedDependency, DependencyType dependencyType, ProviderType providerType = ProviderType.Default) where T : IInstalledSTARNETHolon, new()
        {
            OASISResult<T1> result = new OASISResult<T1>();
            string errorMessage = "Error occured in OAPPManagerBase.RemoveDependencyAsync. Reason:";

            try
            {
                OASISResult<STARNETDependency> dependencyResult = RemoveDependency(parent, installedDependency, dependencyType, errorMessage);

                if (dependencyResult != null && dependencyResult.Result != null && !dependencyResult.IsError)
                {
                    result = await UpdateAsync(avatarId, parent, result, errorMessage, true, string.Concat(Enum.GetName(typeof(HolonType), parent.HolonType), "DNAJSON"), providerType: providerType);

                    if (result != null && result.Result != null && !result.IsError)
                    {
                        if (Directory.Exists(dependencyResult.Result.InstalledTo))
                            Directory.Delete(dependencyResult.Result.InstalledTo, true);
                    }
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured adding the dependency with RemoveDependency. Reason: {dependencyResult.Message}");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured. Reason: {ex}");
            }

            return result;
        }

        public OASISResult<T1> RemoveDependency<T>(Guid avatarId, T1 parent, T installedDependency, DependencyType dependencyType, ProviderType providerType = ProviderType.Default) where T : IInstalledSTARNETHolon, new()
        {
            OASISResult<T1> result = new OASISResult<T1>();
            string errorMessage = "Error occured in OAPPManagerBase.RemoveDependency. Reason:";

            try
            {
                OASISResult<STARNETDependency> dependencyResult = RemoveDependency(parent, installedDependency, dependencyType, errorMessage);

                if (dependencyResult != null && dependencyResult.Result != null && !dependencyResult.IsError)
                {
                    result = Update(avatarId, parent, result, errorMessage, true, string.Concat(Enum.GetName(typeof(HolonType), parent.HolonType), "DNAJSON"), providerType: providerType);

                    if (result != null && result.Result != null && !result.IsError)
                    {
                        if (Directory.Exists(dependencyResult.Result.InstalledTo))
                            Directory.Delete(dependencyResult.Result.InstalledTo, true);
                    }
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured adding the dependency with RemoveDependency. Reason: {dependencyResult.Message}");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured. Reason: {ex}");
            }

            return result;
        }

        public async Task<OASISResult<T1>> RemoveDependencyAsync<T>(Guid avatarId, Guid parentId, string parentVersion, T installedDependency, DependencyType dependencyType, ProviderType providerType = ProviderType.Default) where T : IInstalledSTARNETHolon, new()
        {
            OASISResult<T1> result = new OASISResult<T1>();
            string errorMessage = "Error occured in OAPPManagerBase.RemoveDependencyAsync. Reason:";

            try
            {
                OASISResult<T1> parentResult = await Data.LoadHolonByMetaDataAsync<T1>(new Dictionary<string, string>()
                {
                    { STARNETHolonIdName, parentId.ToString() },
                    { "Version", parentVersion }

                }, MetaKeyValuePairMatchMode.All, STARNETHolonType, providerType: providerType);

                if (parentResult != null && parentResult.Result != null && !parentResult.IsError)
                    return await RemoveDependencyAsync(avatarId, parentResult.Result, installedDependency, dependencyType, providerType);
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the parent with OAPPManagerBase.LoadHolonByMetaDataAsync. Reason: {parentResult.Message}");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured. Reason: {ex}");
            }

            return result;
        }

        public OASISResult<T1> RemoveDependency<T>(Guid avatarId, Guid parentId, string parentVersion, T installedDependency, DependencyType dependencyType, ProviderType providerType = ProviderType.Default) where T : IInstalledSTARNETHolon, new()
        {
            OASISResult<T1> result = new OASISResult<T1>();
            string errorMessage = "Error occured in OAPPManagerBase.RemoveDependency. Reason:";

            try
            {
                OASISResult<T1> parentResult = Data.LoadHolonByMetaData<T1>(new Dictionary<string, string>()
                {
                    { STARNETHolonIdName, parentId.ToString() },
                    { "Version", parentVersion }

                }, MetaKeyValuePairMatchMode.All, STARNETHolonType, providerType: providerType);

                if (parentResult != null && parentResult.Result != null && !parentResult.IsError)
                    return RemoveDependency(avatarId, parentResult.Result, installedDependency, dependencyType, providerType);
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the parent with OAPPManagerBase.LoadHolonByMetaData. Reason: {parentResult.Message}");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured. Reason: {ex}");
            }

            return result;
        }

        public async Task<OASISResult<T1>> RemoveDependencyAsync<T>(Guid avatarId, Guid parentId, int parentVersionSequence, T installedDependency, DependencyType dependencyType, ProviderType providerType = ProviderType.Default) where T : IInstalledSTARNETHolon, new()
        {
            OASISResult<T1> result = new OASISResult<T1>();
            string errorMessage = "Error occured in OAPPManagerBase.RemoveDependencyAsync. Reason:";

            try
            {
                OASISResult<T1> parentResult = await Data.LoadHolonByMetaDataAsync<T1>(new Dictionary<string, string>()
                {
                    { STARNETHolonIdName, parentId.ToString() },
                    { "VersionSequence", parentVersionSequence.ToString() }

                }, MetaKeyValuePairMatchMode.All, STARNETHolonType, providerType: providerType);

                if (parentResult != null && parentResult.Result != null && !parentResult.IsError)
                    return await RemoveDependencyAsync(avatarId, parentResult.Result, installedDependency, dependencyType, providerType);
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the parent with OAPPManagerBase.LoadHolonByMetaDataAsync. Reason: {parentResult.Message}");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured. Reason: {ex}");
            }

            return result;
        }

        public OASISResult<T1> RemoveDependency<T>(Guid avatarId, Guid parentId, int parentVersionSequence, T installedDependency, DependencyType dependencyType, ProviderType providerType = ProviderType.Default) where T : IInstalledSTARNETHolon, new()
        {
            OASISResult<T1> result = new OASISResult<T1>();
            string errorMessage = "Error occured in OAPPManagerBase.RemoveDependency. Reason:";

            try
            {
                OASISResult<T1> parentResult = Data.LoadHolonByMetaData<T1>(new Dictionary<string, string>()
                {
                    { STARNETHolonIdName, parentId.ToString() },
                    { "VersionSequence", parentVersionSequence.ToString() }

                }, MetaKeyValuePairMatchMode.All, STARNETHolonType, providerType: providerType);

                if (parentResult != null && parentResult.Result != null && !parentResult.IsError)
                    return RemoveDependency(avatarId, parentResult.Result, installedDependency, dependencyType, providerType);
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the parent with OAPPManagerBase.LoadHolonByMetaData. Reason: {parentResult.Message}");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured. Reason: {ex}");
            }

            return result;
        }

        public async Task<OASISResult<T1>> RemoveDependencyAsync<T>(Guid avatarId, Guid parentId, int parentVersionSequence, Guid dependencyId, int dependencyVersionSequence, HolonType dependencyHolonType, DependencyType dependencyType, ProviderType providerType = ProviderType.Default) where T : IInstalledSTARNETHolon, new()
        {
            OASISResult<T1> result = new OASISResult<T1>();
            string errorMessage = "Error occured in OAPPManagerBase.RemoveDependencyAsync. Reason:";

            OASISResult<T> installedDependencyResult = await Data.LoadHolonByMetaDataAsync<T>(new Dictionary<string, string>()
            {
                { "STARNETHolonId", dependencyId.ToString() },
                { "VersionSequence", dependencyVersionSequence.ToString() }

            }, MetaKeyValuePairMatchMode.All, dependencyHolonType, providerType: providerType);

            if (installedDependencyResult != null && installedDependencyResult.Result != null && !installedDependencyResult.IsError)
                result = await RemoveDependencyAsync<T>(avatarId, parentId, parentVersionSequence, installedDependencyResult.Result, dependencyType, providerType);
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the Installed Dependency with Data.LoadHolonByMetaDataAsync. Reason: {installedDependencyResult.Message}");

            return result;
        }

        public OASISResult<T1> RemoveDependency<T>(Guid avatarId, Guid parentId, int parentVersionSequence, Guid dependencyId, int dependencyVersionSequence, HolonType dependencyHolonType, DependencyType dependencyType, ProviderType providerType = ProviderType.Default) where T : IInstalledSTARNETHolon, new()
        {
            OASISResult<T1> result = new OASISResult<T1>();
            string errorMessage = "Error occured in OAPPManagerBase.RemoveDependency. Reason:";

            OASISResult<T> installedDependencyResult = Data.LoadHolonByMetaData<T>(new Dictionary<string, string>()
            {
                { "STARNETHolonId", dependencyId.ToString() },
                { "VersionSequence", dependencyVersionSequence.ToString() }

            }, MetaKeyValuePairMatchMode.All, dependencyHolonType, providerType: providerType);

            if (installedDependencyResult != null && installedDependencyResult.Result != null && !installedDependencyResult.IsError)
                result = RemoveDependency(avatarId, parentId, parentVersionSequence, installedDependencyResult.Result, dependencyType, providerType);
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the Installed Dependency with Data.LoadHolonByMetaData. Reason: {installedDependencyResult.Message}");

            return result;
        }

        public async Task<OASISResult<T1>> RemoveDependencyAsync<T>(Guid avatarId, Guid parentId, string parentVersion, Guid dependencyId, string dependencyVersion, HolonType dependencyHolonType, DependencyType dependencyType, ProviderType providerType = ProviderType.Default) where T : IInstalledSTARNETHolon, new()
        {
            OASISResult<T1> result = new OASISResult<T1>();
            string errorMessage = "Error occured in OAPPManagerBase.RemoveDependencyAsync. Reason:";

            OASISResult<T> installedDependencyResult = await Data.LoadHolonByMetaDataAsync<T>(new Dictionary<string, string>()
            {
                { "STARNETHolonId", dependencyId.ToString() },
                { "Version", dependencyVersion}

            }, MetaKeyValuePairMatchMode.All, dependencyHolonType, providerType: providerType);

            if (installedDependencyResult != null && installedDependencyResult.Result != null && !installedDependencyResult.IsError)
                result = await RemoveDependencyAsync(avatarId, parentId, parentVersion, installedDependencyResult.Result, dependencyType, providerType);
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the Installed Dependency with Data.LoadHolonByMetaDataAsync. Reason: {installedDependencyResult.Message}");

            return result;
        }

        public OASISResult<T1> RemoveDependency<T>(Guid avatarId, Guid parentId, string parentVersion, Guid dependencyId, string dependencyVersion, HolonType dependencyHolonType, DependencyType dependencyType, ProviderType providerType = ProviderType.Default) where T : IInstalledSTARNETHolon, new()
        {
            OASISResult<T1> result = new OASISResult<T1>();
            string errorMessage = "Error occured in OAPPManagerBase.RemoveDependency. Reason:";

            OASISResult<T> installedDependencyResult = Data.LoadHolonByMetaData<T>(new Dictionary<string, string>()
            {
                { "STARNETHolonId", dependencyId.ToString() },
                { "Version", dependencyVersion }

            }, MetaKeyValuePairMatchMode.All, dependencyHolonType, providerType: providerType);

            if (installedDependencyResult != null && installedDependencyResult.Result != null && !installedDependencyResult.IsError)
                result = RemoveDependency(avatarId, parentId, parentVersion, installedDependencyResult.Result, dependencyType, providerType);
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the Installed Dependency with Data.LoadHolonByMetaData. Reason: {installedDependencyResult.Message}");

            return result;
        }

        public async Task<OASISResult<T>> InstallDependencyAsync<T>(Guid avatarId, STARNETDependency dependency, string defaultDownloadPath, string defaultInstallPath, string dependencyDisplayName, ProviderType providerType = ProviderType.Default) where T : IInstalledSTARNETHolon
        {
            OASISResult<T> result = new OASISResult<T>();
            string downloadPath = "";
            string installPath = "";

            if (Path.IsPathRooted(defaultDownloadPath) || string.IsNullOrEmpty(STARDNA.BaseSTARNETPath))
                downloadPath = defaultDownloadPath;
            else
                downloadPath = Path.Combine(STARDNA.BaseSTARNETPath, defaultDownloadPath);

            if (Path.IsPathRooted(defaultInstallPath) || string.IsNullOrEmpty(STARDNA.BaseSTARNETPath))
                installPath = defaultInstallPath;
            else
                installPath = Path.Combine(STARDNA.BaseSTARNETPath, defaultInstallPath);

            switch (dependency.Type)
            {
                case DependencyType.Runtime:
                    {
                        RuntimeManager runtimeManager = new RuntimeManager(avatarId, STARDNA, OASISDNA);
                        runtimeManager.OnDownloadStatusChanged += RuntimeManager_OnDownloadStatusChanged;
                        runtimeManager.OnInstallStatusChanged += RuntimeManager_OnInstallStatusChanged;
                        OASISResult<InstalledRuntime> installResult = await runtimeManager.DownloadAndInstallAsync(avatarId, dependency.STARNETHolonId, dependency.Version, installPath, downloadPath, providerType: providerType);
                        runtimeManager.OnDownloadStatusChanged -= RuntimeManager_OnDownloadStatusChanged;
                        runtimeManager.OnInstallStatusChanged -= RuntimeManager_OnInstallStatusChanged;
                        result.Result = (T)(IInstalledSTARNETHolon)installResult.Result;
                        OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(installResult, result);
                        runtimeManager = null;
                    }
                    break;

                case DependencyType.Library:
                    {
                        LibraryManager libManager = new LibraryManager(avatarId, STARDNA, OASISDNA);
                        libManager.OnDownloadStatusChanged += LibManager_OnDownloadStatusChanged;
                        libManager.OnInstallStatusChanged += LibManager_OnInstallStatusChanged;
                        OASISResult<InstalledLibrary> installResult = await libManager.DownloadAndInstallAsync(avatarId, dependency.STARNETHolonId, dependency.Version, installPath, downloadPath, providerType: providerType);
                        libManager.OnDownloadStatusChanged -= LibManager_OnDownloadStatusChanged;
                        libManager.OnInstallStatusChanged -= LibManager_OnInstallStatusChanged;
                        result.Result = (T)(IInstalledSTARNETHolon)installResult.Result;
                        OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(installResult, result);
                        libManager = null;
                    }
                    break;

                case DependencyType.Template:
                    {
                        OAPPTemplateManager templateManager = new OAPPTemplateManager(avatarId, STARDNA, OASISDNA);
                        templateManager.OnDownloadStatusChanged += TemplateManager_OnDownloadStatusChanged;
                        templateManager.OnInstallStatusChanged += TemplateManager_OnInstallStatusChanged;
                        OASISResult<InstalledOAPPTemplate> installResult = await templateManager.DownloadAndInstallAsync(avatarId, dependency.STARNETHolonId, dependency.Version, installPath, downloadPath, providerType: providerType);
                        templateManager.OnDownloadStatusChanged -= TemplateManager_OnDownloadStatusChanged;
                        templateManager.OnInstallStatusChanged -= TemplateManager_OnInstallStatusChanged;
                        result.Result = (T)(IInstalledSTARNETHolon)installResult.Result;
                        OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(installResult, result);
                        templateManager = null;
                    }
                    break;

                case DependencyType.OAPP:
                    {
                        OAPPManager OAPPManager = new OAPPManager(avatarId, STARDNA, OASISDNA);
                        OAPPManager.OnDownloadStatusChanged += OAPPManager_OnDownloadStatusChanged;
                        OAPPManager.OnInstallStatusChanged += OAPPManager_OnInstallStatusChanged;
                        OASISResult<InstalledOAPP> installResult = await OAPPManager.DownloadAndInstallAsync(avatarId, dependency.STARNETHolonId, dependency.Version, installPath, downloadPath, providerType: providerType);
                        OAPPManager.OnDownloadStatusChanged -= OAPPManager_OnDownloadStatusChanged;
                        OAPPManager.OnInstallStatusChanged -= OAPPManager_OnInstallStatusChanged;
                        result.Result = (T)(IInstalledSTARNETHolon)installResult.Result;
                        OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(installResult, result);
                        OAPPManager = null;
                    }
                    break;

                case DependencyType.Quest:
                    {
                        QuestManager QuestManager = new QuestManager(avatarId, STARDNA, OASISDNA);
                        QuestManager.OnDownloadStatusChanged += QuestManager_OnDownloadStatusChanged;
                        QuestManager.OnInstallStatusChanged += QuestManager_OnInstallStatusChanged;
                        OASISResult<InstalledQuest> installResult = await QuestManager.DownloadAndInstallAsync(avatarId, dependency.STARNETHolonId, dependency.Version, installPath, downloadPath, providerType: providerType);
                        QuestManager.OnDownloadStatusChanged -= QuestManager_OnDownloadStatusChanged;
                        QuestManager.OnInstallStatusChanged -= QuestManager_OnInstallStatusChanged;
                        result.Result = (T)(IInstalledSTARNETHolon)installResult.Result;
                        OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(installResult, result);
                        QuestManager = null;
                    }
                    break;

                    //TODO: Finish implementing for rest of of the dependencies.
            }

            return result;
        }

        public OASISResult<T> InstallDependency<T>(Guid avatarId, STARNETDependency dependency, string defaultDownloadPath, string defaultInstallPath, string dependencyDisplayName, ProviderType providerType = ProviderType.Default) where T : IInstalledSTARNETHolon
        {
            OASISResult<T> result = new OASISResult<T>();
            string downloadPath = "";
            string installPath = "";

            if (Path.IsPathRooted(defaultDownloadPath) || string.IsNullOrEmpty(STARDNA.BaseSTARNETPath))
                downloadPath = defaultDownloadPath;
            else
                downloadPath = Path.Combine(STARDNA.BaseSTARNETPath, defaultDownloadPath);

            if (Path.IsPathRooted(defaultInstallPath) || string.IsNullOrEmpty(STARDNA.BaseSTARNETPath))
                installPath = defaultInstallPath;
            else
                installPath = Path.Combine(STARDNA.BaseSTARNETPath, defaultInstallPath);

            switch (dependency.Type)
            {
                case DependencyType.Runtime:
                    {
                        RuntimeManager runtimeManager = new RuntimeManager(avatarId, STARDNA, OASISDNA);
                        runtimeManager.OnDownloadStatusChanged += RuntimeManager_OnDownloadStatusChanged;
                        runtimeManager.OnInstallStatusChanged += RuntimeManager_OnInstallStatusChanged;
                        OASISResult<InstalledRuntime> installResult = runtimeManager.DownloadAndInstall(avatarId, dependency.STARNETHolonId, dependency.Version, installPath, downloadPath, providerType: providerType);
                        runtimeManager.OnDownloadStatusChanged -= RuntimeManager_OnDownloadStatusChanged;
                        runtimeManager.OnInstallStatusChanged -= RuntimeManager_OnInstallStatusChanged;
                        result.Result = (T)(IInstalledSTARNETHolon)installResult.Result;
                        OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(installResult, result);
                        runtimeManager = null;
                    }
                    break;

                case DependencyType.Library:
                    {
                        LibraryManager libManager = new LibraryManager(avatarId, STARDNA, OASISDNA);
                        libManager.OnDownloadStatusChanged += LibManager_OnDownloadStatusChanged;
                        libManager.OnInstallStatusChanged += LibManager_OnInstallStatusChanged;
                        OASISResult<InstalledLibrary> installResult = libManager.DownloadAndInstall(avatarId, dependency.STARNETHolonId, dependency.Version, installPath, downloadPath, providerType: providerType);
                        libManager.OnDownloadStatusChanged -= LibManager_OnDownloadStatusChanged;
                        libManager.OnInstallStatusChanged -= LibManager_OnInstallStatusChanged;
                        result.Result = (T)(IInstalledSTARNETHolon)installResult.Result;
                        OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(installResult, result);
                        libManager = null;
                    }
                    break;

                case DependencyType.Template:
                    {
                        OAPPTemplateManager templateManager = new OAPPTemplateManager(avatarId, STARDNA, OASISDNA);
                        templateManager.OnDownloadStatusChanged += TemplateManager_OnDownloadStatusChanged;
                        templateManager.OnInstallStatusChanged += TemplateManager_OnInstallStatusChanged;
                        OASISResult<InstalledOAPPTemplate> installResult = templateManager.DownloadAndInstall(avatarId, dependency.STARNETHolonId, dependency.Version, installPath, downloadPath, providerType: providerType);
                        templateManager.OnDownloadStatusChanged -= TemplateManager_OnDownloadStatusChanged;
                        templateManager.OnInstallStatusChanged -= TemplateManager_OnInstallStatusChanged;
                        result.Result = (T)(IInstalledSTARNETHolon)installResult.Result;
                        OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(installResult, result);
                        templateManager = null;
                    }
                    break;

                case DependencyType.OAPP:
                    {
                        OAPPManager OAPPManager = new OAPPManager(avatarId, STARDNA, OASISDNA);
                        OAPPManager.OnDownloadStatusChanged += OAPPManager_OnDownloadStatusChanged;
                        OAPPManager.OnInstallStatusChanged += OAPPManager_OnInstallStatusChanged;
                        OASISResult<InstalledOAPP> installResult = OAPPManager.DownloadAndInstall(avatarId, dependency.STARNETHolonId, dependency.Version, installPath, downloadPath, providerType: providerType);
                        OAPPManager.OnDownloadStatusChanged -= OAPPManager_OnDownloadStatusChanged;
                        OAPPManager.OnInstallStatusChanged -= OAPPManager_OnInstallStatusChanged;
                        result.Result = (T)(IInstalledSTARNETHolon)installResult.Result;
                        OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(installResult, result);
                        OAPPManager = null;
                    }
                    break;

                case DependencyType.Quest:
                    {
                        QuestManager QuestManager = new QuestManager(avatarId, STARDNA, OASISDNA);
                        QuestManager.OnDownloadStatusChanged += QuestManager_OnDownloadStatusChanged;
                        QuestManager.OnInstallStatusChanged += QuestManager_OnInstallStatusChanged;
                        OASISResult<InstalledQuest> installResult = QuestManager.DownloadAndInstall(avatarId, dependency.STARNETHolonId, dependency.Version, installPath, downloadPath, providerType: providerType);
                        QuestManager.OnDownloadStatusChanged -= QuestManager_OnDownloadStatusChanged;
                        QuestManager.OnInstallStatusChanged -= QuestManager_OnInstallStatusChanged;
                        result.Result = (T)(IInstalledSTARNETHolon)installResult.Result;
                        OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(installResult, result);
                        QuestManager = null;
                    }
                    break;

                    //TODO: Finish implementing for rest of of the dependencies.
            }

            return result;
        }

        protected void RaisePublishStatusChangedEvent(T4 STARNETDNA, STARNETHolonPublishStatus status, string errorMesssage = "")
        {
            OnPublishStatusChanged?.Invoke(this, new STARNETHolonPublishStatusEventArgs() { STARNETDNA = STARNETDNA, Status = status, ErrorMessage = errorMesssage });
        }

        protected void RaiseInstallStatusChangedEvent(T4 STARNETDNA, STARNETHolonInstallStatus status, string errorMesssage = "")
        {
            OnInstallStatusChanged?.Invoke(this, new STARNETHolonInstallStatusEventArgs() { STARNETDNA = STARNETDNA, Status = status, ErrorMessage = errorMesssage });
        }

        protected void RaiseUploadStatusChangedEvent(T4 STARNETDNA, STARNETHolonUploadStatus status, string errorMesssage = "")
        {
            OnUploadStatusChanged?.Invoke(this, new STARNETHolonUploadProgressEventArgs() { STARNETDNA = STARNETDNA, Status = status, ErrorMessage = errorMesssage });
        }

        protected void RaiseDownloadStatusChangedEvent(T4 STARNETDNA, STARNETHolonDownloadStatus status, string errorMesssage = "")
        {
            OnDownloadStatusChanged?.Invoke(this, new STARNETHolonDownloadProgressEventArgs() { STARNETDNA = STARNETDNA, Status = status, ErrorMessage = errorMesssage });
        }

        private OASISResult<T> CheckForVersionMismatches<T>(T4 STARNETDNA, ref OASISResult<T> result)
        {
            string message = "The {0} Version ({1}) does not match the current version ({1}). This may lead to issues, it is recommended to make sure the versions match.";

            if (STARNETDNA.STARODKVersion != OASISBootLoader.OASISBootLoader.STARODKVersion)
                OASISErrorHandling.HandleWarning(ref result, string.Format(message, "STARODK", STARNETDNA.STARODKVersion, OASISBootLoader.OASISBootLoader.STARODKVersion));

            if (STARNETDNA.STARRuntimeVersion != OASISBootLoader.OASISBootLoader.STARRuntimeVersion)
                OASISErrorHandling.HandleWarning(ref result, string.Format(message, "STAR Runtime", STARNETDNA.STARRuntimeVersion, OASISBootLoader.OASISBootLoader.STARRuntimeVersion));

            if (STARNETDNA.STARNETVersion != OASISBootLoader.OASISBootLoader.STARNETVersion)
                OASISErrorHandling.HandleWarning(ref result, string.Format(message, "STARNET", STARNETDNA.STARNETVersion, OASISBootLoader.OASISBootLoader.STARNETVersion));

            if (STARNETDNA.STARAPIVersion != OASISBootLoader.OASISBootLoader.STARAPIVersion)
                OASISErrorHandling.HandleWarning(ref result, string.Format(message, "STAR API", STARNETDNA.STARAPIVersion, OASISBootLoader.OASISBootLoader.STARAPIVersion));

            if (STARNETDNA.OASISAPIVersion != OASISBootLoader.OASISBootLoader.OASISAPIVersion)
                OASISErrorHandling.HandleWarning(ref result, string.Format(message, "OASIS Runtime", STARNETDNA.OASISRuntimeVersion, OASISBootLoader.OASISBootLoader.OASISRuntimeVersion));

            if (STARNETDNA.OASISAPIVersion != OASISBootLoader.OASISBootLoader.OASISAPIVersion)
                OASISErrorHandling.HandleWarning(ref result, string.Format(message, "OASIS API", STARNETDNA.OASISAPIVersion, OASISBootLoader.OASISBootLoader.OASISAPIVersion));

            if (STARNETDNA.COSMICVersion != OASISBootLoader.OASISBootLoader.COSMICVersion)
                OASISErrorHandling.HandleWarning(ref result, string.Format(message, "COSMIC", STARNETDNA.COSMICVersion, OASISBootLoader.OASISBootLoader.STARODKVersion));

            if (STARNETDNA.DotNetVersion != OASISBootLoader.OASISBootLoader.DotNetVersion)
                OASISErrorHandling.HandleWarning(ref result, string.Format(message, ".NET", STARNETDNA.DotNetVersion, OASISBootLoader.OASISBootLoader.DotNetVersion));

            return result;
        }

        private async Task<OASISResult<T3>> UninstallAsync(Guid avatarId, OASISResult<T3> installedSTARNETHolonResult, string errorMessage, ProviderType providerType)
        {
            OASISResult<T3> result = new OASISResult<T3>();

            if (installedSTARNETHolonResult != null && !installedSTARNETHolonResult.IsError && installedSTARNETHolonResult.Result != null)
                result = await UninstallAsync(avatarId, installedSTARNETHolonResult.Result, errorMessage, providerType);
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonByMetaDataAsync. Reason: {installedSTARNETHolonResult.Message}");

            return result;
        }

        private OASISResult<T3> Uninstall(Guid avatarId, OASISResult<T3> installedSTARNETHolonResult, string errorMessage, ProviderType providerType)
        {
            OASISResult<T3> result = new OASISResult<T3>();

            if (installedSTARNETHolonResult != null && !installedSTARNETHolonResult.IsError && installedSTARNETHolonResult.Result != null)
                result = Uninstall(avatarId, installedSTARNETHolonResult.Result, errorMessage, providerType);
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonByMetaData. Reason: {installedSTARNETHolonResult.Message}");

            return result;
        }

        private async Task<OASISResult<T3>> InstallDependenciesAsync(Guid avatarId, T1 STARNETHolon, string fullInstallPath, string errorMessage, OASISResult<T3> result, ProviderType providerType = ProviderType.Default)
        {
            result = await InstallDependenciesAsync<InstalledLibrary>(avatarId, STARNETHolon.STARNETDNA.Dependencies.Libraries, "Libs", HolonType.InstalledLibrary, fullInstallPath, STARDNA.DefaultLibsDownloadedPath, STARDNA.DefaultLibsInstalledPath, "Library", errorMessage, result, providerType);
            result = await InstallDependenciesAsync<InstalledRuntime>(avatarId, STARNETHolon.STARNETDNA.Dependencies.Runtimes, "Runtimes", HolonType.InstalledRuntime, fullInstallPath, STARDNA.DefaultRuntimesDownloadedPath, STARDNA.DefaultRuntimesInstalledPath, "Runtime", errorMessage, result, providerType);
            result = await InstallDependenciesAsync<InstalledOAPPTemplate>(avatarId, STARNETHolon.STARNETDNA.Dependencies.Templates, "Templates", HolonType.InstalledOAPPTemplate, fullInstallPath, STARDNA.DefaultOAPPTemplatesDownloadedPath, STARDNA.DefaultOAPPTemplatesInstalledPath, "Template", errorMessage, result, providerType);
            result = await InstallDependenciesAsync<InstalledOAPP>(avatarId, STARNETHolon.STARNETDNA.Dependencies.OAPPs, "OAPPs", HolonType.InstalledOAPP, fullInstallPath, STARDNA.DefaultOAPPsDownloadedPath, STARDNA.DefaultOAPPsInstalledPath, "OAPP", errorMessage, result, providerType);
            result = await InstallDependenciesAsync<InstalledQuest>(avatarId, STARNETHolon.STARNETDNA.Dependencies.Quests, "Quests", HolonType.InstalledQuest, fullInstallPath, STARDNA.DefaultQuestsDownloadedPath, STARDNA.DefaultQuestsInstalledPath, "Quest", errorMessage, result, providerType);
            result = await InstallDependenciesAsync<InstalledMission>(avatarId, STARNETHolon.STARNETDNA.Dependencies.Missions, "Missions", HolonType.InstalledMission, fullInstallPath, STARDNA.DefaultMissionsDownloadedPath, STARDNA.DefaultMissionsInstalledPath, "Mission", errorMessage, result, providerType);

            return result;
        }

        private OASISResult<T3> InstallDependencies(Guid avatarId, T1 STARNETHolon, string fullInstallPath, string errorMessage, OASISResult<T3> result, ProviderType providerType = ProviderType.Default)
        {
            result = InstallDependencies<InstalledLibrary>(avatarId, STARNETHolon.STARNETDNA.Dependencies.Libraries, "Libs", HolonType.InstalledLibrary, fullInstallPath, STARDNA.DefaultLibsDownloadedPath, STARDNA.DefaultLibsInstalledPath, "Library", errorMessage, result, providerType);
            result = InstallDependencies<InstalledRuntime>(avatarId, STARNETHolon.STARNETDNA.Dependencies.Runtimes, "Runtimes", HolonType.InstalledRuntime, fullInstallPath, STARDNA.DefaultRuntimesDownloadedPath, STARDNA.DefaultRuntimesInstalledPath, "Runtime", errorMessage, result, providerType);
            result = InstallDependencies<InstalledOAPPTemplate>(avatarId, STARNETHolon.STARNETDNA.Dependencies.Templates, "Templates", HolonType.InstalledOAPPTemplate, fullInstallPath, STARDNA.DefaultOAPPTemplatesDownloadedPath, STARDNA.DefaultOAPPTemplatesInstalledPath, "Template", errorMessage, result, providerType);
            result = InstallDependencies<InstalledOAPP>(avatarId, STARNETHolon.STARNETDNA.Dependencies.OAPPs, "OAPPs", HolonType.InstalledOAPP, fullInstallPath, STARDNA.DefaultOAPPsDownloadedPath, STARDNA.DefaultOAPPsInstalledPath, "OAPP", errorMessage, result, providerType);
            result = InstallDependencies<InstalledQuest>(avatarId, STARNETHolon.STARNETDNA.Dependencies.Quests, "Quests", HolonType.InstalledQuest, fullInstallPath, STARDNA.DefaultQuestsDownloadedPath, STARDNA.DefaultQuestsInstalledPath, "Quest", errorMessage, result, providerType);
            result = InstallDependencies<InstalledMission>(avatarId, STARNETHolon.STARNETDNA.Dependencies.Missions, "Missions", HolonType.InstalledMission, fullInstallPath, STARDNA.DefaultMissionsDownloadedPath, STARDNA.DefaultMissionsInstalledPath, "Mission", errorMessage, result, providerType);

            return result;
        }

        private async Task<OASISResult<T3>> InstallDependenciesAsync<T>(Guid avatarId, List<STARNETDependency> dependencies, string dependencyFolder, HolonType installedHolonType, string fullInstallPath, string defaultDownloadPath, string defaultInstallPath, string dependencyDisplayName, string errorMessage, OASISResult<T3> result, ProviderType providerType = ProviderType.Default) where T : IInstalledSTARNETHolon, new()
        {
            foreach (STARNETDependency dependency in dependencies)
            {
                string installPath = Path.Combine(fullInstallPath, "Dependencies", "STARNET", dependencyFolder, string.Concat(dependency.Name, "_v", dependency.Version));

                if (dependency.InstallMode == DependencyInstallMode.Root)
                    installPath = fullInstallPath;

                string dependencyDNAFilePath = Path.Combine(installPath, string.Concat(Enum.GetName(typeof(DependencyType), dependency.Type), "_", dependency.Name, "_v", dependency.Version));
                bool install = false;

                //TODO: When the DNA files are switched over to use OAPPTemplate_SampleTemplate_v1.0.0.json format un-comment this line! :-)
                //if (!File.Exists(dependencyDNAFilePath) && dependency.Install)
                if (!Directory.Exists(installPath) && dependency.Install) //Currently ONLY supports Nested! above line supports both! ;-)
                {
                    if (Directory.Exists(dependency.InstalledFrom))
                        DirectoryHelper.CopyFilesRecursively(dependency.InstalledFrom, installPath);
                    else
                    {
                        OASISResult<T> installedLibResult = await Data.LoadHolonByMetaDataAsync<T>(new Dictionary<string, string>()
                                                        {
                                                            { "Id", dependency.STARNETHolonId.ToString() },
                                                            { "Version", dependency.Version },
                                                            { "Active", "1" }
                                                        }, metaKeyValuePairMatchMode: MetaKeyValuePairMatchMode.All, installedHolonType, providerType: providerType);

                        if (installedLibResult != null && installedLibResult.Result != null && !installedLibResult.IsError)
                        {
                            if (Directory.Exists(installedLibResult.Result.InstalledPath))
                                DirectoryHelper.CopyFilesRecursively(installedLibResult.Result.InstalledPath, installPath);
                            else
                                install = true;
                        }
                        else
                            install = true;

                        if (install)
                        {
                            OASISResult<T> installResult = await InstallDependencyAsync<T>(avatarId, dependency, defaultDownloadPath, defaultInstallPath, dependencyDisplayName, providerType);

                            if (installResult != null && installResult.Result != null && !installResult.IsError)
                            {
                                if (Directory.Exists(installResult.Result.InstalledPath))
                                    DirectoryHelper.CopyFilesRecursively(installResult.Result.InstalledPath, installPath);
                                else
                                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured installing the {dependencyDisplayName} dependency {dependency.Name}. Reason: {installResult.Message}");
                            }
                        }
                    }
                }
            }

            return result;
        }

        private OASISResult<T3> InstallDependencies<T>(Guid avatarId, List<STARNETDependency> dependencies, string dependencyFolder, HolonType installedHolonType, string fullInstallPath, string defaultDownloadPath, string defaultInstallPath, string dependencyDisplayName, string errorMessage, OASISResult<T3> result, ProviderType providerType = ProviderType.Default) where T : IInstalledSTARNETHolon, new()
        {
            foreach (STARNETDependency dependency in dependencies)
            {
                string installPath = Path.Combine(fullInstallPath, "Dependencies", "STARNET", dependencyFolder, string.Concat(dependency.Name, "_v", dependency.Version));

                if (dependency.InstallMode == DependencyInstallMode.Root)
                    installPath = fullInstallPath;

                string dependencyDNAFilePath = Path.Combine(installPath, string.Concat(Enum.GetName(typeof(DependencyType), dependency.Type), "_", dependency.Name, "_v", dependency.Version));
                bool install = false;

                //TODO: When the DNA files are switched over to use OAPPTemplate_SampleTemplate_v1.0.0.json format un-comment this line! :-)
                //if (!File.Exists(dependencyDNAFilePath) && dependency.Install)
                if (!Directory.Exists(installPath) && dependency.Install) //Currently ONLY supports Nested! above line supports both! ;-)
                {
                    if (Directory.Exists(dependency.InstalledFrom))
                        DirectoryHelper.CopyFilesRecursively(dependency.InstalledFrom, installPath);
                    else
                    {
                        OASISResult<T> installedLibResult = Data.LoadHolonByMetaData<T>(new Dictionary<string, string>()
                                                        {
                                                            { "Id", dependency.STARNETHolonId.ToString() },
                                                            { "Version", dependency.Version },
                                                            { "Active", "1" }
                                                        }, metaKeyValuePairMatchMode: MetaKeyValuePairMatchMode.All, installedHolonType, providerType: providerType);

                        if (installedLibResult != null && installedLibResult.Result != null && !installedLibResult.IsError)
                        {
                            if (Directory.Exists(installedLibResult.Result.InstalledPath))
                                DirectoryHelper.CopyFilesRecursively(installedLibResult.Result.InstalledPath, installPath);
                            else
                                install = true;
                        }
                        else
                            install = true;

                        if (install)
                        {
                            OASISResult<T> installResult = InstallDependency<T>(avatarId, dependency, defaultDownloadPath, defaultInstallPath, dependencyDisplayName, providerType);

                            if (installResult != null && installResult.Result != null && !installResult.IsError)
                            {
                                if (Directory.Exists(installResult.Result.InstalledPath))
                                    DirectoryHelper.CopyFilesRecursively(installResult.Result.InstalledPath, installPath);
                                else
                                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured installing the {dependencyDisplayName} dependency {dependency.Name}. Reason: {installResult.Message}");
                            }
                        }
                    }
                }
            }

            return result;
        }

        private OASISResult<IEnumerable<T>> FilterResultsForVersion<T>(Guid avatarId, OASISResult<IEnumerable<T>> results, bool showAllVersions = false, int version = 0) where T : ISTARNETHolon, new()
        {
            OASISResult<IEnumerable<T>> result = new OASISResult<IEnumerable<T>>();
            List<T> holons = new List<T>();

            if (!showAllVersions)
            {
                if (results.Result != null && !result.IsError)
                {
                    if (version == 0) //latest version
                    {
                        Dictionary<string, T> latestVersions = new Dictionary<string, T>();
                        string dependencyId = "";
                        int latestVersion = 0;
                        int currentVersion = 0;

                        foreach (T oappSystemHolon in results.Result)
                        {
                            if (oappSystemHolon.MetaData != null && oappSystemHolon.MetaData.ContainsKey(STARNETHolonIdName) && oappSystemHolon.MetaData[STARNETHolonIdName] != null)
                                dependencyId = oappSystemHolon.MetaData[STARNETHolonIdName].ToString();

                            latestVersion = latestVersions.ContainsKey(dependencyId) ? Convert.ToInt32(latestVersions[dependencyId].STARNETDNA.Version.Replace(".", "")) : 0;
                            currentVersion = Convert.ToInt32(oappSystemHolon.STARNETDNA.Version.Replace(".", ""));

                            if (latestVersions.ContainsKey(dependencyId) &&
                                currentVersion > latestVersion
                                //oappSystemHolon.STARNETDNA.CreatedOn > latestVersions[dependencyId].STARNETDNA.CreatedOn)
                                || !latestVersions.ContainsKey(dependencyId))
                                latestVersions[dependencyId] = oappSystemHolon;
                        }

                        result.Result = latestVersions.Values.ToList();
                    }
                    else
                    {
                        List<T> filteredList = new List<T>();

                        foreach (T oappSystemHolon in results.Result)
                        {
                            if (oappSystemHolon.MetaData["VersionSequence"].ToString() == version.ToString())
                                filteredList.Remove(oappSystemHolon);
                        }

                        result.Result = filteredList;
                    }
                }
            }
            else
                result.Result = results.Result;

            //Filter out any items that are not created by the avatar or published on STARNET.
            if (results.Result != null)
            {
                holons = result.Result.ToList();

                foreach (T oappSystemHolon in result.Result)
                {
                    if (oappSystemHolon.STARNETDNA.CreatedByAvatarId != avatarId)
                    {
                        if (oappSystemHolon.STARNETDNA.PublishedOn == DateTime.MinValue)
                            holons.Remove(oappSystemHolon);
                    }
                }
            }

            result.Result = holons;
            result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(results, result);
            return result;
        }

        private OASISResult<T1> Update(Guid avatarId, T1 holon, OASISResult<T1> result, string errorMessage, bool updateDNAJSONFile = false, string STARNETDNAJSONName = "Default", ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> questResult = Update(avatarId, holon, updateDNAJSONFile, STARNETDNAJSONName, providerType);
            OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(questResult, result);

            if (questResult != null && questResult.Result != null && !questResult.IsError)
                result.Result = questResult.Result;
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured saving the {STARNETHolonUIName} with OAPPManagerBase.Update. Reason: {questResult.Message}");

            return result;
        }

        private async Task<OASISResult<T1>> UpdateAsync(Guid avatarId, T1 holon, OASISResult<T1> result, string errorMessage, bool updateDNAJSONFile = false, string STARNETDNAJSONName = "Default", ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> questResult = await UpdateAsync(avatarId, holon, updateDNAJSONFile, STARNETDNAJSONName, providerType);
            OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(questResult, result);

            if (questResult != null && questResult.Result != null && !questResult.IsError)
                result.Result = questResult.Result;
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured saving the {STARNETHolonUIName} with OAPPManagerBase.Update. Reason: {questResult.Message}");

            return result;
        }

        private OASISResult<(T1, string)> AddDependency<T>(T1 parent, T installedDependency, DependencyType dependencyType, string errorMessage, bool installDependency = true, DependencyInstallMode dependencyInstallMode = DependencyInstallMode.Nested) where T : IInstalledSTARNETHolon, new()
        {
            OASISResult<(T1, string)> result = new OASISResult<(T1, string)>();
            string dependencyFolderName = Enum.GetName(typeof(DependencyType), dependencyType);

            switch (dependencyType)
            {
                case DependencyType.CelestialBodyMetaDataDNA:
                    dependencyFolderName = "CelestialBodiesMetaDataDNA";
                    break;

                case DependencyType.ZomeMetaDataDNA:
                    dependencyFolderName = "ZomesMetaDataDNA";
                    break;

                case DependencyType.HolonMetaDataDNA:
                    dependencyFolderName = "HolonsMetaDataDNA";
                    break;

                case DependencyType.Library:
                    dependencyFolderName = "Libs";
                    break;

                case DependencyType.CelestialBody:
                    dependencyFolderName = "CelestialBodies";
                    break;
            }

            string installPath = Path.Combine(parent.STARNETDNA.SourcePath, "Dependencies", "STARNET", dependencyFolderName, string.Concat(installedDependency.STARNETDNA.Name, "_v", installedDependency.STARNETDNA.Version));

            //TODO: Need to change the DNA files to use the name and version so instead of OAPPTemplate.DNA it would be OAPPTemplate_SampleTemplate_v1.0.0.json.
            if (dependencyInstallMode == DependencyInstallMode.Root)
                installPath = parent.STARNETDNA.SourcePath;

            bool found = false;

            switch (dependencyType)
            {
                case DependencyType.CelestialBodyMetaDataDNA:
                    found = parent.STARNETDNA.Dependencies.CelestialBodiesMetaDataDNA.Any(x => x.HolonId == installedDependency.Id);
                    break;

                case DependencyType.ZomeMetaDataDNA:
                    found = parent.STARNETDNA.Dependencies.ZomesMetaDataDNA.Any(x => x.HolonId == installedDependency.Id);
                    break;

                case DependencyType.HolonMetaDataDNA:
                    found = parent.STARNETDNA.Dependencies.HolonsMetaDataDNA.Any(x => x.HolonId == installedDependency.Id);
                    break;

                case DependencyType.Runtime:
                    found = parent.STARNETDNA.Dependencies.Runtimes.Any(x => x.HolonId == installedDependency.Id);
                    break;

                case DependencyType.Library:
                    found = parent.STARNETDNA.Dependencies.Libraries.Any(x => x.HolonId == installedDependency.Id);
                    break;

                case DependencyType.Template:
                    found = parent.STARNETDNA.Dependencies.Templates.Any(x => x.HolonId == installedDependency.Id);
                    break;

                case DependencyType.OAPP:
                    found = parent.STARNETDNA.Dependencies.OAPPs.Any(x => x.HolonId == installedDependency.Id);
                    break;

                case DependencyType.Zome:
                    found = parent.STARNETDNA.Dependencies.Zomes.Any(x => x.HolonId == installedDependency.Id);
                    break;

                case DependencyType.Holon:
                    found = parent.STARNETDNA.Dependencies.Holons.Any(x => x.HolonId == installedDependency.Id);
                    break;

                case DependencyType.CelestialBody:
                    found = parent.STARNETDNA.Dependencies.CelestialBodies.Any(x => x.HolonId == installedDependency.Id);
                    break;

                case DependencyType.CelestialSpace:
                    found = parent.STARNETDNA.Dependencies.CelestialSpaces.Any(x => x.HolonId == installedDependency.Id);
                    break;

                case DependencyType.Quest:
                    found = parent.STARNETDNA.Dependencies.Quests.Any(x => x.HolonId == installedDependency.Id);
                    break;

                case DependencyType.Mission:
                    found = parent.STARNETDNA.Dependencies.Missions.Any(x => x.HolonId == installedDependency.Id);
                    break;

                case DependencyType.Chapter:
                    found = parent.STARNETDNA.Dependencies.Chapters.Any(x => x.HolonId == installedDependency.Id);
                    break;

                case DependencyType.NFT:
                    found = parent.STARNETDNA.Dependencies.NFTs.Any(x => x.HolonId == installedDependency.Id);
                    break;

                case DependencyType.GeoNFT:
                    found = parent.STARNETDNA.Dependencies.GeoNFTs.Any(x => x.HolonId == installedDependency.Id);
                    break;

                case DependencyType.GeoHotSpot:
                    found = parent.STARNETDNA.Dependencies.GeoHotSpots.Any(x => x.HolonId == installedDependency.Id);
                    break;

                case DependencyType.InventoryItem:
                    found = parent.STARNETDNA.Dependencies.InventoryItems.Any(x => x.HolonId == installedDependency.Id);
                    break;
            }

            if (!found)
            {
                STARNETDependency dependency = new STARNETDependency()
                {
                    HolonId = installedDependency.Id,
                    STARNETHolonId = installedDependency.STARNETDNA.Id,
                    Name = installedDependency.STARNETDNA.Name,
                    Description = installedDependency.STARNETDNA.Description,
                    VersionSequence = installedDependency.STARNETDNA.VersionSequence,
                    Version = installedDependency.STARNETDNA.Version,
                    InstalledFrom = installedDependency.InstalledPath,
                    InstalledTo = installPath,
                    Install = installDependency,
                    InstallMode = dependencyInstallMode
                };

                switch (dependencyType)
                {
                    case DependencyType.CelestialBodyMetaDataDNA:
                        parent.STARNETDNA.Dependencies.CelestialBodiesMetaDataDNA.Remove(dependency);
                        break;

                    case DependencyType.ZomeMetaDataDNA:
                        parent.STARNETDNA.Dependencies.ZomesMetaDataDNA.Remove(dependency);
                        break;

                    case DependencyType.HolonMetaDataDNA:
                        parent.STARNETDNA.Dependencies.HolonsMetaDataDNA.Remove(dependency);
                        break;

                    case DependencyType.Runtime:
                        parent.STARNETDNA.Dependencies.Runtimes.Remove(dependency);
                        break;

                    case DependencyType.Library:
                        parent.STARNETDNA.Dependencies.Libraries.Remove(dependency);
                        break;

                    case DependencyType.Template:
                        parent.STARNETDNA.Dependencies.Templates.Remove(dependency);
                        break;

                    case DependencyType.OAPP:
                        parent.STARNETDNA.Dependencies.OAPPs.Remove(dependency);
                        break;

                    case DependencyType.Zome:
                        parent.STARNETDNA.Dependencies.Zomes.Remove(dependency);
                        break;

                    case DependencyType.Holon:
                        parent.STARNETDNA.Dependencies.Holons.Remove(dependency);
                        break;

                    case DependencyType.CelestialBody:
                        parent.STARNETDNA.Dependencies.CelestialBodies.Remove(dependency);
                        break;

                    case DependencyType.CelestialSpace:
                        parent.STARNETDNA.Dependencies.CelestialSpaces.Remove(dependency);
                        break;

                    case DependencyType.Quest:
                        parent.STARNETDNA.Dependencies.Quests.Remove(dependency);
                        break;

                    case DependencyType.Mission:
                        parent.STARNETDNA.Dependencies.Missions.Remove(dependency);
                        break;

                    case DependencyType.Chapter:
                        parent.STARNETDNA.Dependencies.Chapters.Remove(dependency);
                        break;

                    case DependencyType.NFT:
                        parent.STARNETDNA.Dependencies.NFTs.Remove(dependency);
                        break;

                    case DependencyType.GeoNFT:
                        parent.STARNETDNA.Dependencies.GeoNFTs.Remove(dependency);
                        break;

                    case DependencyType.GeoHotSpot:
                        parent.STARNETDNA.Dependencies.GeoHotSpots.Remove(dependency);
                        break;

                    case DependencyType.InventoryItem:
                        parent.STARNETDNA.Dependencies.InventoryItems.Remove(dependency);
                        break;
                }
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} The {Enum.GetName(typeof(DependencyType), dependencyType)} {installedDependency.STARNETDNA.Name} has already been added to {parent.STARNETDNA.Name}.");

            result.Result = (parent, installPath);
            return result;
        }

        private OASISResult<STARNETDependency> RemoveDependency<T>(T1 parent, T installedDependency, DependencyType dependencyType, string errorMessage) where T : IInstalledSTARNETHolon, new()
        {
            OASISResult<STARNETDependency> result = new OASISResult<STARNETDependency>();
            STARNETDependency STARNETDependency = null;

            switch (dependencyType)
            {
                case DependencyType.CelestialBodyMetaDataDNA:
                    STARNETDependency = parent.STARNETDNA.Dependencies.CelestialBodiesMetaDataDNA.FirstOrDefault(x => x.HolonId == installedDependency.Id);
                    break;

                case DependencyType.ZomeMetaDataDNA:
                    STARNETDependency = parent.STARNETDNA.Dependencies.ZomesMetaDataDNA.FirstOrDefault(x => x.HolonId == installedDependency.Id);
                    break;

                case DependencyType.HolonMetaDataDNA:
                    STARNETDependency = parent.STARNETDNA.Dependencies.HolonsMetaDataDNA.FirstOrDefault(x => x.HolonId == installedDependency.Id);
                    break;

                case DependencyType.Runtime:
                    STARNETDependency = parent.STARNETDNA.Dependencies.Runtimes.FirstOrDefault(x => x.HolonId == installedDependency.Id);
                    break;

                case DependencyType.Library:
                    STARNETDependency = parent.STARNETDNA.Dependencies.Libraries.FirstOrDefault(x => x.HolonId == installedDependency.Id);
                    break;

                case DependencyType.Template:
                    STARNETDependency = parent.STARNETDNA.Dependencies.Templates.FirstOrDefault(x => x.HolonId == installedDependency.Id);
                    break;

                case DependencyType.OAPP:
                    STARNETDependency = parent.STARNETDNA.Dependencies.OAPPs.FirstOrDefault(x => x.HolonId == installedDependency.Id);
                    break;

                case DependencyType.Zome:
                    STARNETDependency = parent.STARNETDNA.Dependencies.Zomes.FirstOrDefault(x => x.HolonId == installedDependency.Id);
                    break;

                case DependencyType.Holon:
                    STARNETDependency = parent.STARNETDNA.Dependencies.Holons.FirstOrDefault(x => x.HolonId == installedDependency.Id);
                    break;

                case DependencyType.CelestialBody:
                    STARNETDependency = parent.STARNETDNA.Dependencies.CelestialBodies.FirstOrDefault(x => x.HolonId == installedDependency.Id);
                    break;

                case DependencyType.CelestialSpace:
                    STARNETDependency = parent.STARNETDNA.Dependencies.CelestialSpaces.FirstOrDefault(x => x.HolonId == installedDependency.Id);
                    break;

                case DependencyType.Quest:
                    STARNETDependency = parent.STARNETDNA.Dependencies.Quests.FirstOrDefault(x => x.HolonId == installedDependency.Id);
                    break;

                case DependencyType.Mission:
                    STARNETDependency = parent.STARNETDNA.Dependencies.Missions.FirstOrDefault(x => x.HolonId == installedDependency.Id);
                    break;

                case DependencyType.Chapter:
                    STARNETDependency = parent.STARNETDNA.Dependencies.Chapters.FirstOrDefault(x => x.HolonId == installedDependency.Id);
                    break;

                case DependencyType.NFT:
                    STARNETDependency = parent.STARNETDNA.Dependencies.NFTs.FirstOrDefault(x => x.HolonId == installedDependency.Id);
                    break;

                case DependencyType.GeoNFT:
                    STARNETDependency = parent.STARNETDNA.Dependencies.GeoNFTs.FirstOrDefault(x => x.HolonId == installedDependency.Id);
                    break;

                case DependencyType.GeoHotSpot:
                    STARNETDependency = parent.STARNETDNA.Dependencies.GeoHotSpots.FirstOrDefault(x => x.HolonId == installedDependency.Id);
                    break;

                case DependencyType.InventoryItem:
                    STARNETDependency = parent.STARNETDNA.Dependencies.InventoryItems.FirstOrDefault(x => x.HolonId == installedDependency.Id);
                    break;
            }

            if (STARNETDependency != null)
            {
                switch (dependencyType)
                {
                    case DependencyType.CelestialBodyMetaDataDNA:
                        parent.STARNETDNA.Dependencies.CelestialBodiesMetaDataDNA.Remove(STARNETDependency);
                        break;

                    case DependencyType.ZomeMetaDataDNA:
                        parent.STARNETDNA.Dependencies.ZomesMetaDataDNA.Remove(STARNETDependency);
                        break;

                    case DependencyType.HolonMetaDataDNA:
                        parent.STARNETDNA.Dependencies.HolonsMetaDataDNA.Remove(STARNETDependency);
                        break;

                    case DependencyType.Runtime:
                        parent.STARNETDNA.Dependencies.Runtimes.Remove(STARNETDependency);
                        break;

                    case DependencyType.Library:
                        parent.STARNETDNA.Dependencies.Libraries.Remove(STARNETDependency);
                        break;

                    case DependencyType.Template:
                        parent.STARNETDNA.Dependencies.Templates.Remove(STARNETDependency);
                        break;

                    case DependencyType.OAPP:
                        parent.STARNETDNA.Dependencies.OAPPs.Remove(STARNETDependency);
                        break;

                    case DependencyType.Zome:
                        parent.STARNETDNA.Dependencies.Zomes.Remove(STARNETDependency);
                        break;

                    case DependencyType.Holon:
                        parent.STARNETDNA.Dependencies.Holons.Remove(STARNETDependency);
                        break;

                    case DependencyType.CelestialBody:
                        parent.STARNETDNA.Dependencies.CelestialBodies.Remove(STARNETDependency);
                        break;

                    case DependencyType.CelestialSpace:
                        parent.STARNETDNA.Dependencies.CelestialSpaces.Remove(STARNETDependency);
                        break;

                    case DependencyType.Quest:
                        parent.STARNETDNA.Dependencies.Quests.Remove(STARNETDependency);
                        break;

                    case DependencyType.Mission:
                        parent.STARNETDNA.Dependencies.Missions.Remove(STARNETDependency);
                        break;

                    case DependencyType.Chapter:
                        parent.STARNETDNA.Dependencies.Chapters.Remove(STARNETDependency);
                        break;

                    case DependencyType.NFT:
                        parent.STARNETDNA.Dependencies.NFTs.Remove(STARNETDependency);
                        break;

                    case DependencyType.GeoNFT:
                        parent.STARNETDNA.Dependencies.GeoNFTs.Remove(STARNETDependency);
                        break;

                    case DependencyType.GeoHotSpot:
                        parent.STARNETDNA.Dependencies.GeoHotSpots.Remove(STARNETDependency);
                        break;

                    case DependencyType.InventoryItem:
                        parent.STARNETDNA.Dependencies.InventoryItems.Remove(STARNETDependency);
                        break;
                }
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} The {Enum.GetName(typeof(DependencyType), dependencyType)} {installedDependency.STARNETDNA.Name} was not found installed for {parent.STARNETDNA.Name}.");

            result.Result = STARNETDependency;
            return result;
        }

        private void OnUploadProgress(Google.Apis.Upload.IUploadProgress progress)
        {
            switch (progress.Status)
            {
                case Google.Apis.Upload.UploadStatus.NotStarted:
                    _progress = 0;
                    OnUploadStatusChanged?.Invoke(this, new STARNETHolonUploadProgressEventArgs() { Progress = _progress, Status = STARNETHolonUploadStatus.NotStarted });
                    break;

                case Google.Apis.Upload.UploadStatus.Starting:
                    _progress = 0;
                    OnUploadStatusChanged?.Invoke(this, new STARNETHolonUploadProgressEventArgs() { Progress = _progress, Status = STARNETHolonUploadStatus.Uploading });
                    break;

                case Google.Apis.Upload.UploadStatus.Completed:
                    _progress = 100;
                    OnUploadStatusChanged?.Invoke(this, new STARNETHolonUploadProgressEventArgs() { Progress = _progress, Status = STARNETHolonUploadStatus.Uploaded });
                    break;

                case Google.Apis.Upload.UploadStatus.Uploading:
                    {
                        if (_fileLength > 0)
                        {
                            _progress = Convert.ToInt32(progress.BytesSent / (double)_fileLength * 100);
                            OnUploadStatusChanged?.Invoke(this, new STARNETHolonUploadProgressEventArgs() { Progress = _progress, Status = STARNETHolonUploadStatus.Uploading });
                        }
                    }
                    break;

                case Google.Apis.Upload.UploadStatus.Failed:
                    OnUploadStatusChanged?.Invoke(this, new STARNETHolonUploadProgressEventArgs() { Progress = _progress, Status = STARNETHolonUploadStatus.Error, ErrorMessage = progress.Exception.ToString() });
                    break;
            }
        }

        private void OnDownloadProgress(Google.Apis.Download.IDownloadProgress progress)
        {
            switch (progress.Status)
            {
                case Google.Apis.Download.DownloadStatus.NotStarted:
                    _progress = 0;
                    OnDownloadStatusChanged?.Invoke(this, new STARNETHolonDownloadProgressEventArgs() { Progress = _progress, Status = STARNETHolonDownloadStatus.NotStarted });
                    break;

                case Google.Apis.Download.DownloadStatus.Completed:
                    _progress = 100;
                    OnDownloadStatusChanged?.Invoke(this, new STARNETHolonDownloadProgressEventArgs() { Progress = _progress, Status = STARNETHolonDownloadStatus.Downloaded });
                    break;

                case Google.Apis.Download.DownloadStatus.Downloading:
                    {
                        if (_fileLength > 0)
                        {
                            _progress = Convert.ToInt32(progress.BytesDownloaded / (double)_fileLength * 100);
                            // _progress = Convert.ToInt32(_fileLength / progress.BytesDownloaded);
                            OnDownloadStatusChanged?.Invoke(this, new STARNETHolonDownloadProgressEventArgs() { Progress = _progress, Status = STARNETHolonDownloadStatus.Downloading });
                        }
                    }
                    break;

                case Google.Apis.Download.DownloadStatus.Failed:
                    OnDownloadStatusChanged?.Invoke(this, new STARNETHolonDownloadProgressEventArgs() { Progress = _progress, Status = STARNETHolonDownloadStatus.Error, ErrorMessage = progress.Exception.ToString() });
                    break;
            }
        }

        private void TemplateManager_OnInstallStatusChanged(object sender, STARNETHolonInstallStatusEventArgs e)
        {
            OnInstallStatusChanged?.Invoke(sender, e);
        }

        private void LibManager_OnInstallStatusChanged(object sender, STARNETHolonInstallStatusEventArgs e)
        {
            OnInstallStatusChanged?.Invoke(sender, e);
        }

        private void RuntimeManager_OnInstallStatusChanged(object sender, STARNETHolonInstallStatusEventArgs e)
        {
            OnInstallStatusChanged?.Invoke(sender, e);
        }

        private void RuntimeManager_OnDownloadStatusChanged(object sender, STARNETHolonDownloadProgressEventArgs e)
        {
            OnDownloadStatusChanged?.Invoke(sender, e);
        }

        private void TemplateManager_OnDownloadStatusChanged(object sender, STARNETHolonDownloadProgressEventArgs e)
        {
            OnDownloadStatusChanged?.Invoke(sender, e);
        }

        private void LibManager_OnDownloadStatusChanged(object sender, STARNETHolonDownloadProgressEventArgs e)
        {
            OnDownloadStatusChanged?.Invoke(sender, e);
        }

        private void QuestManager_OnInstallStatusChanged(object sender, STARNETHolonInstallStatusEventArgs e)
        {
            OnInstallStatusChanged?.Invoke(sender, e);
        }

        private void QuestManager_OnDownloadStatusChanged(object sender, STARNETHolonDownloadProgressEventArgs e)
        {
            OnDownloadStatusChanged?.Invoke(sender, e);
        }

        private void OAPPManager_OnInstallStatusChanged(object sender, STARNETHolonInstallStatusEventArgs e)
        {
            OnInstallStatusChanged?.Invoke(sender, e);
        }

        private void OAPPManager_OnDownloadStatusChanged(object sender, STARNETHolonDownloadProgressEventArgs e)
        {
            OnDownloadStatusChanged?.Invoke(sender, e);
        }
    }
}