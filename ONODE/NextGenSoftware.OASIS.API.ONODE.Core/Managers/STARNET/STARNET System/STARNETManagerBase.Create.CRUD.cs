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
    }
}
