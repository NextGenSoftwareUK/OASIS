using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Google.Cloud.Storage.V1;
using Microsoft.Extensions.DependencyModel;
using Newtonsoft.Json;
using NextGenSoftware.CLI.Engine;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.ONODE.Core.Enums.STARNETHolon;
using NextGenSoftware.OASIS.API.ONODE.Core.Events.STARNETHolon;
using NextGenSoftware.OASIS.API.ONODE.Core.Holons;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Holons;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Managers;
using NextGenSoftware.OASIS.API.ONODE.Core.Objects;
using NextGenSoftware.OASIS.API.ONODE.Core.Objects.STARNET;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.STAR.DNA;
using NextGenSoftware.Utilities;
using NextGenSoftware.OASIS.API.ONODE.Core.Managers.Interop;
using NextGenSoftware.OASIS.API.ONODE.Core.Enums;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Managers.Base
{
    public abstract partial class STARNETManagerBase<T1, T2, T3, T4>
    {
        //public virtual async Task<OASISResult<T1>> CreateAsync(Guid avatarId, string name, string description, object holonSubType, string fullPathToSourceFolder, STARNETCreateOptions<T1, T4> createOptions = null)
        //{
        //    //return CreateAsync(avatarId, name, description, holonSubType, fullPathToSourceFolder, createOptions != null ? createOptions.ProviderType : ProviderType.Default, createOptions != null ? createOptions.MetaHolonTagMappings : null, createOptions != null ? createOptions.MetaTagMappings : null, createOptions != null ? createOptions.NewHolon : null, createOptions != null ? createOptions.STARNETDNA : null, createOptions != null ? createOptions.CheckIfSourcePathExists : true);
        //}

        //public virtual async Task<OASISResult<T1>> CreateAsync(Guid avatarId, string name, string description, object holonSubType, string fullPathToSourceFolder, List<MetaHolonTag> metaHolonTagMappings = null, Dictionary<string, string> metaTagMappings = null, Dictionary<string, object> metaData = null, T1 newHolon = default, T4 STARNETDNA = default, bool checkIfSourcePathExists = true, ProviderType providerType = ProviderType.Default)
        //public virtual async Task<OASISResult<T1>> CreateAsync(Guid avatarId, string name, string description, object holonSubType, string fullPathToSourceFolder, List<MetaHolonTag> metaHolonTagMappings = null, Dictionary<string, string> metaTagMappings = null, T1 newHolon = default, T4 STARNETDNA = default, bool checkIfSourcePathExists = true, ProviderType providerType = ProviderType.Default)
        public virtual async Task<OASISResult<T1>> CreateAsync(Guid avatarId, string name, string description, object holonSubType, string fullPathToSourceFolder, ISTARNETCreateOptions<T1, T4> createOptions = null, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            string errorMessage = "Error occured in STARNETManagerBase.CreateAsync, Reason:";
            T1 holon;
            T4 STARNETDNA;

            try
            {
                //TODO: Dont want UI in the backend!
                if (!string.IsNullOrWhiteSpace(fullPathToSourceFolder) && Directory.Exists(fullPathToSourceFolder) && createOptions != null && createOptions.CheckIfSourcePathExists)
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

                if (createOptions != null && createOptions.STARNETHolon != null)
                {
                    holon = createOptions.STARNETHolon;

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

                //foreach (string key in metaData?.Keys ?? new Dictionary<string, object>().Keys)
                //{
                //    if (!holon.MetaData.ContainsKey(key))
                //        holon.MetaData.Add(key, metaData[key]);
                //    else
                //        holon.MetaData[key] = metaData[key];
                //}


                OASISResult<IAvatar> avatarResult = await AvatarManager.Instance.LoadAvatarAsync(avatarId, false, true, providerType);

                if (avatarResult != null && avatarResult.Result != null && !avatarResult.IsError)
                {
                    if (createOptions != null && createOptions.STARNETDNA != null)
                        STARNETDNA = createOptions.STARNETDNA;
                    else
                        STARNETDNA = new T4();

                    STARNETDNA.Id = holon.Id;
                    STARNETDNA.Name = name;
                    STARNETDNA.Description = description;
                    STARNETDNA.STARNETHolonType = Enum.GetName(typeof(HolonType), STARNETHolonType);
                    STARNETDNA.STARNETCategory = Enum.GetName(holonSubTypeType, holonSubType);
                    
                    // Set STARNETSubCategory (Language) if provided in createOptions
                    if (createOptions != null && createOptions.CustomCreateParams != null && 
                        createOptions.CustomCreateParams.ContainsKey("STARNETSubCategory"))
                    {
                        var subCategory = createOptions.CustomCreateParams["STARNETSubCategory"];
                        if (subCategory != null)
                        {
                            // Use dynamic to access STARNETSubCategory since it may not be in ISTARNETDNA interface
                            dynamic dna = STARNETDNA;
                            dna.STARNETSubCategory = subCategory is Enum ? Enum.GetName(subCategory.GetType(), subCategory) : subCategory.ToString();
                        }
                    }
                    
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
                    STARNETDNA.SourcePath = fullPathToSourceFolder ?? string.Empty;
                    //STARNETDNA.MetaData = metaData; //TODO: Not sure if we need this? It works without it, but may be useful to view in the DNA.json file for users?
                    //STARNETDNA.MetaTagMappings.MetaHolonTags = createOptions != null ? createOptions.MetaTagMappings
                    //STARNETDNA.MetaTagMappings.MetaTags = metaTagMappings;
                    STARNETDNA.MetaTagMappings = createOptions != null ? createOptions.MetaTagMappings : new MetaTagMappings();

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

                    bool writeDnaOk;
                    if (string.IsNullOrWhiteSpace(fullPathToSourceFolder))
                    {
                        // No folder path (e.g. cross-game quest from API): skip disk DNA write; holon is saved to provider only.
                        writeDnaOk = true;
                    }
                    else
                    {
                        OASISResult<bool> writeSTARNETDNAResult = await WriteDNAAsync(STARNETDNA, fullPathToSourceFolder);
                        writeDnaOk = writeSTARNETDNAResult != null && writeSTARNETDNAResult.Result && !writeSTARNETDNAResult.IsError;
                        if (!writeDnaOk)
                            OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured writing the {STARNETHolonUIName} DNA. Reason: {writeSTARNETDNAResult?.Message}");
                    }

                    if (writeDnaOk)
                    {
                        holon.STARNETDNA = STARNETDNA;
                        OASISResult<T1> saveHolonResult = await Data.SaveHolonAsync<T1>(holon, avatarId, true, true, 0, true, false, providerType);

                        if (saveHolonResult != null && saveHolonResult.Result != null && !saveHolonResult.IsError)
                        {
                            result.Result = saveHolonResult.Result;
                            result.Message = $"Successfully created the {STARNETHolonUIName} on the {Enum.GetName(typeof(ProviderType), providerType)} provider by AvatarId {avatarId} for {STARNETHolonTypeName} {Enum.GetName(holonSubTypeType, holonSubType)}.";
                        }
                        else
                            OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured saving the {STARNETHolonUIName} to the {Enum.GetName(typeof(ProviderType), providerType)} provider. Reason: {saveHolonResult?.Message}");
                    }
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
                if (!string.IsNullOrWhiteSpace(fullPathToSourceFolder) && Directory.Exists(fullPathToSourceFolder) && checkIfSourcePathExists)
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
                    
                    // Set STARNETSubCategory (Language) if provided in dependency/MetaData
                    if (dependency != null && dependency.ContainsKey("STARNETSubCategory"))
                    {
                        var subCategory = dependency["STARNETSubCategory"];
                        if (subCategory != null)
                        {
                            // Use dynamic to access STARNETSubCategory since it may not be in ISTARNETDNA interface
                            dynamic dna = STARNETDNA;
                            dna.STARNETSubCategory = subCategory is Enum ? Enum.GetName(subCategory.GetType(), subCategory) : subCategory.ToString();
                        }
                    }
                    
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
                    STARNETDNA.SourcePath = fullPathToSourceFolder ?? string.Empty;
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

                    bool writeDnaOkSync;
                    if (string.IsNullOrWhiteSpace(fullPathToSourceFolder))
                    {
                        writeDnaOkSync = true;
                    }
                    else
                    {
                        OASISResult<bool> writeSTARNETDNAResult = WriteDNA(STARNETDNA, fullPathToSourceFolder);
                        writeDnaOkSync = writeSTARNETDNAResult != null && writeSTARNETDNAResult.Result && !writeSTARNETDNAResult.IsError;
                        if (!writeDnaOkSync)
                            OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured writing the {STARNETHolonUIName} DNA. Reason: {writeSTARNETDNAResult?.Message}");
                    }

                    if (writeDnaOkSync)
                    {
                        holon.STARNETDNA = STARNETDNA;
                        OASISResult<T1> saveHolonResult = Data.SaveHolon<T1>(holon, avatarId, true, true, 0, true, false, providerType);

                        if (saveHolonResult != null && saveHolonResult.Result != null && !saveHolonResult.IsError)
                        {
                            result.Result = saveHolonResult.Result;
                            result.Message = $"Successfully created the {STARNETHolonUIName} on the {Enum.GetName(typeof(ProviderType), providerType)} provider by AvatarId {avatarId} for {STARNETHolonTypeName} {Enum.GetName(holonSubTypeType, holonSubType)}.";
                        }
                        else
                            OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured saving the {STARNETHolonUIName} to the {Enum.GetName(typeof(ProviderType), providerType)} provider. Reason: {saveHolonResult?.Message}");
                    }
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

        public virtual async Task<OASISResult<T1>> UpdateAsync(Guid avatarId, T1 holon, bool updateDNAJSONFile = false, string STARNETDNAJSONName = "Default", ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = new OASISResult<T1>();

            if (!string.IsNullOrWhiteSpace(holon.STARNETDNA?.SourcePath))
            {
                if (!Directory.Exists(holon.STARNETDNA.SourcePath))
                    Directory.CreateDirectory(holon.STARNETDNA.SourcePath);
            }

            if (STARNETDNAJSONName == "Default")
                STARNETDNAJSONName = this.STARNETDNAJSONName;

            //holon.MetaData[STARNETDNAJSONName] = JsonSerializer.Serialize(holon.STARNETDNA);
            holon.MetaData[STARNETDNAJSONName] = JsonConvert.SerializeObject(holon.STARNETDNA);

            if (updateDNAJSONFile && !string.IsNullOrWhiteSpace(holon.STARNETDNA?.SourcePath))
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

            if (!string.IsNullOrWhiteSpace(holon.STARNETDNA?.SourcePath))
            {
                if (!Directory.Exists(holon.STARNETDNA.SourcePath))
                    Directory.CreateDirectory(holon.STARNETDNA.SourcePath);
            }

            if (STARNETDNAJSONName == "Default")
                STARNETDNAJSONName = this.STARNETDNAJSONName;

            //holon.MetaData[STARNETDNAJSONName] = JsonSerializer.Serialize(holon.STARNETDNA);
            holon.MetaData[STARNETDNAJSONName] = JsonConvert.SerializeObject(holon.STARNETDNA);

            if (updateDNAJSONFile && !string.IsNullOrWhiteSpace(holon.STARNETDNA?.SourcePath))
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

            if (!string.IsNullOrWhiteSpace(holon.STARNETDNA?.SourcePath))
            {
                if (!Directory.Exists(holon.STARNETDNA.SourcePath))
                    Directory.CreateDirectory(holon.STARNETDNA.SourcePath);
            }

            if (STARNETDNAJSONName == "Default")
                STARNETDNAJSONName = this.STARNETDNAJSONName;

            //holon.MetaData[STARNETDNAJSONName] = JsonSerializer.Serialize(holon.STARNETDNA);
            holon.MetaData[STARNETDNAJSONName] = JsonConvert.SerializeObject(holon.STARNETDNA);

            if (updateDNAJSONFile && !string.IsNullOrWhiteSpace(holon.InstalledPath))
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

            if (!string.IsNullOrWhiteSpace(holon.STARNETDNA?.SourcePath))
            {
                if (!Directory.Exists(holon.STARNETDNA.SourcePath))
                    Directory.CreateDirectory(holon.STARNETDNA.SourcePath);
            }

            if (STARNETDNAJSONName == "Default")
                STARNETDNAJSONName = this.STARNETDNAJSONName;

            //holon.MetaData[STARNETDNAJSONName] = JsonSerializer.Serialize(holon.STARNETDNA);
            holon.MetaData[STARNETDNAJSONName] = JsonConvert.SerializeObject(holon.STARNETDNA);

            if (updateDNAJSONFile && !string.IsNullOrWhiteSpace(holon.InstalledPath))
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

        public virtual async Task<OASISResult<IEnumerable<T>>> SearchAsync<T>(Guid avatarId, string searchTerm, Guid parentId = default, Dictionary<string, string> filterByMetaData = null, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode = MetaKeyValuePairMatchMode.All, bool searchOnlyForCurrentAvatar = true, bool showAllVersions = false, int version = 0, ProviderType providerType = ProviderType.Default) where T : ISTARNETHolon, new()
        {
            OASISResult<IEnumerable<T>> result = new OASISResult<IEnumerable<T>>();
            OASISResult<IEnumerable<T>> loadHolonsResult = await SearchHolonsAsync<T>(searchTerm, avatarId, parentId, filterByMetaData, metaKeyValuePairMatchMode, searchOnlyForCurrentAvatar, providerType, "STARNETManagerBase.SearchAsync", STARNETHolonType);
            return FilterResultsForVersion(avatarId, loadHolonsResult, showAllVersions, version);
        }

        public OASISResult<IEnumerable<T1>> Search(Guid avatarId, string searchTerm, Guid parentId = default, Dictionary<string, string> filterByMetaData = null, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode = MetaKeyValuePairMatchMode.All, bool searchOnlyForCurrentAvatar = true, bool showAllVersions = false, int version = 0, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<T1>> result = new OASISResult<IEnumerable<T1>>();
            OASISResult<IEnumerable<T1>> loadHolonsResult = SearchHolons<T1>(searchTerm, avatarId, parentId, filterByMetaData, metaKeyValuePairMatchMode, searchOnlyForCurrentAvatar, providerType, "STARNETManagerBase.Search", STARNETHolonType);
            return FilterResultsForVersion(avatarId, loadHolonsResult, showAllVersions, version);
        }

        public virtual async Task<OASISResult<T1>> DeleteAsync(Guid avatarId, Guid id, int version, bool softDelete = true, bool deleteDownload = true, bool deleteInstall = true, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            string errorMessage = "Error occured in DeleteAsync. Reason: ";
            OASISResult<T1> loadResult = await LoadAsync(avatarId, id, version, providerType: providerType);

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
            OASISResult<T1> loadResult = Load(avatarId, id, version, providerType: providerType);

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

                if (newSTARNETDNA.LaunchTarget != null)
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

    }
}
