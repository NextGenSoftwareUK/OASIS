﻿using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.ONODE.Core.Holons;
using NextGenSoftware.OASIS.API.ONODE.Core.Managers.Base;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Holons;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Objects.STARNET;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.Utilities;
using System.IO;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Managers
{
    public abstract class OAPPManagerBase<T1, T2, T3, T4> : STARNETManagerBase<T1, T2, T3, T4>, IOAPPManagerBase<T1, T2, T3, T4> 
        where T1 : IOAPPBase, new()
        where T2 : IDownloadedSTARNETHolon, new()
        where T3 : IInstalledSTARNETHolon, new()
        where T4 : ISTARNETDNA, new()
    {
        public OAPPManagerBase(Guid avatarId, OASISDNA OASISDNA = null) : base(avatarId, OASISDNA) { }
        public OAPPManagerBase(IOASISStorageProvider OASISStorageProvider, Guid avatarId, OASISDNA OASISDNA = null) : base(OASISStorageProvider, avatarId, OASISDNA) { }
        public OAPPManagerBase(Guid avatarId, OASISDNA OASISDNA = null, Type STARHolonSubType = null, HolonType STARHolonType = HolonType.STARNETHolon, HolonType STARInstalledHolonType = HolonType.InstalledSTARNETHolon, string STARHolonUIName = "OAPP System Holon", string STARHolonIdName = "STARHolonId", string STARHolonNameName = "STARHolonName", string STARHolonTypeName = "STARHolonType", string STARHolonFileExtention = "oappsystemholon", string STARHolonGoogleBucket = "oasis_oappsystemholons", string STARHolonDNAFileName = "STARHolonDNA.json", string STARHolonDNAJSONName = "STARHolonDNAJSON") : base(avatarId, OASISDNA, STARHolonSubType, STARHolonType, STARInstalledHolonType, STARHolonUIName, STARHolonIdName, STARHolonNameName, STARHolonTypeName, STARHolonFileExtention, STARHolonGoogleBucket, STARHolonDNAFileName, STARHolonDNAJSONName) { }
        public OAPPManagerBase(IOASISStorageProvider OASISStorageProvider, Guid avatarId, OASISDNA OASISDNA = null, Type STARHolonSubType = null, HolonType STARHolonType = HolonType.STARNETHolon, HolonType STARInstalledHolonType = HolonType.InstalledSTARNETHolon, string STARHolonUIName = "OAPP System Holon", string STARHolonIdName = "STARHolonId", string STARHolonNameName = "STARHolonName", string STARHolonTypeName = "STARHolonType", string STARHolonFileExtention = "oappsystemholon", string STARHolonGoogleBucket = "oasis_oappsystemholons", string STARHolonDNAFileName = "STARHolonDNA.json", string STARHolonDNAJSONName = "STARHolonDNAJSON") : base(OASISStorageProvider, avatarId, OASISDNA, STARHolonSubType, STARHolonType, STARInstalledHolonType, STARHolonUIName, STARHolonIdName, STARHolonNameName, STARHolonTypeName, STARHolonFileExtention, STARHolonGoogleBucket, STARHolonDNAFileName, STARHolonDNAJSONName) { }


        //public async Task<OASISResult<T1>> AddRuntimeAsync(Guid avatarId, Guid parentId, int templateVersionSequence, Guid runtimeId, int runtimeVersionSequence, ProviderType providerType = ProviderType.Default)
        //{
        //    OASISResult<T1> result = new OASISResult<T1>();
        //    string errorMessage = "Error occured in OAPPManagerBase.AddRuntimeAsync. Reason:";

        //    try
        //    {
        //        OASISResult<T1> parentResult = await LoadAsync(avatarId, parentId, templateVersionSequence, providerType: providerType);

        //        if (parentResult != null && parentResult.Result != null && !parentResult.IsError)
        //        {
        //            OASISResult<Runtime> runtimeResult = await Data.LoadHolonByMetaDataAsync<Runtime>(new Dictionary<string, string>()
        //            {
        //                { "RuntimeId", runtimeId.ToString() },
        //                { "VersionSequence", runtimeVersionSequence.ToString() }

        //            }, MetaKeyValuePairMatchMode.All, HolonType.Runtime, providerType: providerType);

        //            if (runtimeResult != null && runtimeResult.Result != null && !runtimeResult.IsError)
        //            {
        //                //List<STARNETHolonMetaData> metaData = new List<STARNETHolonMetaData>();

        //                //if (parentResult.Result.STARNETDNA.MetaData != null && parentResult.Result.STARNETDNA.MetaData.ContainsKey("RuntimesMetaData") && parentResult.Result.STARNETDNA.MetaData["RuntimesMetaData"] != null)
        //                //    metaData = parentResult.Result.STARNETDNA.MetaData["RuntimesMetaData"] as List<STARNETHolonMetaData>;

        //                //metaData.Add(new STARNETHolonMetaData() 
        //                //{ 
        //                //    HolonId = runtimeResult.Result.Id, 
        //                //    STARNETHolonId = runtimeResult.Result.STARNETDNA.Id,
        //                //    Name = runtimeResult.Result.Name, 
        //                //    VersionSequence = runtimeResult.Result.STARNETDNA.VersionSequence,
        //                //    Version = runtimeResult.Result.STARNETDNA.Version
        //                //});

        //                parentResult.Result.Runtimes.Add((IRuntime)runtimeResult.Result);
        //                parentResult.Result.RuntimesMetaData.Add((ISTARNETHolonMetaData)new STARNETHolonMetaData()
        //                {
        //                    HolonId = runtimeResult.Result.Id,
        //                    STARNETHolonId = runtimeResult.Result.STARNETDNA.Id,
        //                    Name = runtimeResult.Result.Name,
        //                    VersionSequence = runtimeResult.Result.STARNETDNA.VersionSequence,
        //                    Version = runtimeResult.Result.STARNETDNA.Version
        //                });

        //                //parentResult.Result.RuntimeIds.Add(runtimeResult.Result.Id.ToString());
        //                parentResult.Result.STARNETDNA.MetaData["Runtimes"] = parentResult.Result.Runtimes;
        //                //parentResult.Result.STARNETDNA.MetaData["RuntimeIds"] = parentResult.Result.RuntimeIds;
        //                parentResult.Result.STARNETDNA.MetaData["RuntimesMetaData"] = parentResult.Result.RuntimesMetaData;

        //                result = await UpdateAsync(avatarId, parentResult.Result, result, errorMessage, providerType);
        //            }
        //            else
        //                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the runtime with Data.LoadHolonByMetaDataAsync. Reason: {runtimeResult.Message}");
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

        //public OASISResult<T1> AddRuntime(Guid avatarId, Guid parentId, int templateVersionSequence, Guid runtimeId, int runtimeVersionSequnce, ProviderType providerType = ProviderType.Default)
        //{
        //    OASISResult<T1> result = new OASISResult<T1>();
        //    string errorMessage = "Error occured in OAPPManagerBase.AddRuntimeAsync. Reason:";

        //    try
        //    {
        //        OASISResult<T1> parentResult = Load(avatarId, parentId, templateVersionSequence, providerType: providerType);

        //        if (parentResult != null && parentResult.Result != null && !parentResult.IsError)
        //        {
        //            OASISResult<Runtime> runtimeResult = Data.LoadHolonByMetaData<Runtime>(new Dictionary<string, string>()
        //            {
        //                { "RuntimeId", runtimeId.ToString() },
        //                { "VersionSequence", runtimeVersionSequnce.ToString() }

        //            }, MetaKeyValuePairMatchMode.All, HolonType.Runtime, providerType: providerType);

        //            if (runtimeResult != null && runtimeResult.Result != null && !runtimeResult.IsError)
        //            {
        //                parentResult.Result.Runtimes.Add((IRuntime)runtimeResult.Result);
        //                parentResult.Result.RuntimesMetaData.Add((ISTARNETHolonMetaData)new STARNETHolonMetaData()
        //                {
        //                    HolonId = runtimeResult.Result.Id,
        //                    STARNETHolonId = runtimeResult.Result.STARNETDNA.Id,
        //                    Name = runtimeResult.Result.Name,
        //                    VersionSequence = runtimeResult.Result.STARNETDNA.VersionSequence,
        //                    Version = runtimeResult.Result.STARNETDNA.Version
        //                });

        //                parentResult.Result.STARNETDNA.MetaData["Runtimes"] = parentResult.Result.Runtimes;
        //                parentResult.Result.STARNETDNA.MetaData["RuntimesMetaData"] = parentResult.Result.RuntimesMetaData;

        //                result = Update(avatarId, parentResult.Result, result, errorMessage, providerType);
        //            }
        //            else
        //                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the runtime with Data.LoadHolonByMetaData. Reason: {runtimeResult.Message}");
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

        ////public async Task<OASISResult<T1>> AddRuntimeAsync(Guid avatarId, Guid parentId, string templateVersion, Guid runtimeId, string runtimeVersion, ProviderType providerType = ProviderType.Default)
        //public async Task<OASISResult<T1>> AddRuntimeAsync(Guid avatarId, Guid parentId, string templateVersion, IInstalledRuntime installedRuntime, ProviderType providerType = ProviderType.Default)
        //{
        //    OASISResult<T1> result = new OASISResult<T1>();
        //    string errorMessage = "Error occured in OAPPManagerBase.AddRuntimeAsync. Reason:";

        //    try
        //    {
        //        OASISResult<T1> parentResult = await Data.LoadHolonByMetaDataAsync<T1>(new Dictionary<string, string>()
        //        {
        //            { STARNETHolonIdName, parentId.ToString() },
        //            { "Version", templateVersion }

        //        }, MetaKeyValuePairMatchMode.All, STARNETHolonType, providerType: providerType);

        //        if (parentResult != null && parentResult.Result != null && !parentResult.IsError)
        //        {
        //            OASISResult<Runtime> runtimeResult = await Data.LoadHolonByMetaDataAsync<Runtime>(new Dictionary<string, string>()
        //            {
        //                { "RuntimeId", installedRuntime.STARNETDNA.Id.ToString() },
        //                { "Version", installedRuntime.STARNETDNA.Version }

        //            }, MetaKeyValuePairMatchMode.All, HolonType.Runtime, providerType: providerType);

        //            if (runtimeResult != null && runtimeResult.Result != null && !runtimeResult.IsError)
        //            {
        //                parentResult.Result.Runtimes.Add((IRuntime)runtimeResult.Result);
        //                parentResult.Result.RuntimesMetaData.Add((ISTARNETHolonMetaData)new STARNETHolonMetaData()
        //                {
        //                    HolonId = runtimeResult.Result.Id,
        //                    STARNETHolonId = runtimeResult.Result.STARNETDNA.Id,
        //                    Name = runtimeResult.Result.Name,
        //                    VersionSequence = runtimeResult.Result.STARNETDNA.VersionSequence,
        //                    Version = runtimeResult.Result.STARNETDNA.Version
        //                });

        //                parentResult.Result.STARNETDNA.MetaData["Runtimes"] = parentResult.Result.Runtimes;
        //                parentResult.Result.STARNETDNA.MetaData["RuntimesMetaData"] = parentResult.Result.RuntimesMetaData;
        //                result = await UpdateAsync(avatarId, parentResult.Result, result, errorMessage, providerType);

        //                if (result != null && result.Result != null && !result.IsError)
        //                    DirectoryHelper.CopyFilesRecursively(installedRuntime.InstalledPath, Path.Combine(parentResult.Result.STARNETDNA.SourcePath, "Dependencies", "STARNET", "Runtimes"));
        //            }
        //            else
        //                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the runtime with Data.LoadHolonByMetaDataAsync. Reason: {runtimeResult.Message}");
        //        }
        //        else
        //            OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the {STARNETHolonUIName} with OAPPManagerBase.LoadHolonByMetaDataAsync. Reason: {parentResult.Message}");
        //    }
        //    catch (Exception ex)
        //    {
        //        OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured. Reason: {ex}");
        //    }

        //    return result;
        //}

        //public OASISResult<T1> AddRuntime(Guid avatarId, Guid parentId, string templateVersion, Guid runtimeId, string runtimeVersion, ProviderType providerType = ProviderType.Default)
        //{
        //    OASISResult<T1> result = new OASISResult<T1>();
        //    string errorMessage = "Error occured in OAPPManagerBase.AddRuntimeAsync. Reason:";

        //    try
        //    {
        //        OASISResult<T1> parentResult = Data.LoadHolonByMetaData<T1>(new Dictionary<string, string>()
        //        {
        //            { STARNETHolonIdName, parentId.ToString() },
        //            { "Version", templateVersion }

        //        }, MetaKeyValuePairMatchMode.All, STARNETHolonType, providerType: providerType);

        //        if (parentResult != null && parentResult.Result != null && !parentResult.IsError)
        //        {
        //            OASISResult<Runtime> runtimeResult = Data.LoadHolonByMetaData<Runtime>(new Dictionary<string, string>()
        //            {
        //                { "RuntimeId", runtimeId.ToString() },
        //                { "Version", runtimeVersion }

        //            }, MetaKeyValuePairMatchMode.All, HolonType.Runtime, providerType: providerType);

        //            if (runtimeResult != null && runtimeResult.Result != null && !runtimeResult.IsError)
        //            {
        //                parentResult.Result.Runtimes.Add((IRuntime)runtimeResult.Result);
        //                parentResult.Result.RuntimesMetaData.Add((ISTARNETHolonMetaData)new STARNETHolonMetaData()
        //                {
        //                    HolonId = runtimeResult.Result.Id,
        //                    STARNETHolonId = runtimeResult.Result.STARNETDNA.Id,
        //                    Name = runtimeResult.Result.Name,
        //                    VersionSequence = runtimeResult.Result.STARNETDNA.VersionSequence,
        //                    Version = runtimeResult.Result.STARNETDNA.Version
        //                });

        //                parentResult.Result.STARNETDNA.MetaData["Runtimes"] = parentResult.Result.Runtimes;
        //                parentResult.Result.STARNETDNA.MetaData["RuntimesMetaData"] = parentResult.Result.RuntimesMetaData;
        //                result = await UpdateAsync(avatarId, parentResult.Result, result, errorMessage, providerType);

        //                if (result != null && result.Result != null && !result.IsError)
        //                    DirectoryHelper.CopyFilesRecursively(installedRuntime.InstalledPath, Path.Combine(parentResult.Result.STARNETDNA.SourcePath, "Dependencies", "STARNET", "Runtimes"));
        //            }
        //            else
        //                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the runtime with Data.LoadHolonByMetaData. Reason: {runtimeResult.Message}");
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


        //public async Task<OASISResult<T1>> RemoveRuntimeAsync(Guid avatarId, Guid parentId, int templateVersion, Guid runtimeId, int runtimeVersion, ProviderType providerType = ProviderType.Default)
        //{
        //    OASISResult<T1> result = new OASISResult<T1>();
        //    string errorMessage = "Error occured in OAPPManagerBase.RemoveRuntimeAsync. Reason:";

        //    try
        //    {
        //        OASISResult<T1> parentResult = await LoadAsync(avatarId, parentId, templateVersion, providerType: providerType);

        //        if (parentResult != null && parentResult.Result != null && !parentResult.IsError)
        //        {
        //            OASISResult<Runtime> runtimeResult = await Data.LoadHolonByMetaDataAsync<Runtime>(new Dictionary<string, string>()
        //            {
        //                { "RuntimeId", runtimeId.ToString() },
        //                { "VersionSequence", runtimeVersion.ToString() }

        //            }, MetaKeyValuePairMatchMode.All, HolonType.Runtime, providerType: providerType);

        //            if (runtimeResult != null && runtimeResult.Result != null && !runtimeResult.IsError)
        //            {
        //                parentResult.Result.Runtimes.Remove((IRuntime)runtimeResult.Result);
        //                ISTARNETHolonMetaData metaData = parentResult.Result.RuntimesMetaData.FirstOrDefault(x => x.HolonId == runtimeResult.Result.Id);

        //                if (metaData != null)
        //                    parentResult.Result.RuntimesMetaData.Remove(metaData);

        //                parentResult.Result.STARNETDNA.MetaData["Runtimes"] = parentResult.Result.Runtimes;
        //                parentResult.Result.STARNETDNA.MetaData["RuntimesMetaData"] = parentResult.Result.RuntimesMetaData;

        //                result = await UpdateAsync(avatarId, parentResult.Result, result, errorMessage, providerType);

        //                if (result != null && result.Result != null && !result.IsError)
        //                {
        //                    string path = Path.Combine(parentResult.Result.STARNETDNA.SourcePath, "Dependencies", "STARNET", "Runtimes", string.Concat(runtimeResult.Result.STARNETDNA.Name, "_v", runtimeResult.Result.STARNETDNA.Version));

        //                    if (Directory.Exists(path))
        //                        Directory.Exists(path);
        //                }
        //            }
        //            else
        //                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the runtime with Data.LoadHolonByMetaDataAsync. Reason: {runtimeResult.Message}");
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

        //public OASISResult<T1> RemoveRuntime(Guid avatarId, Guid parentId, int templateVersionSequence, Guid runtimeId, int runtimeVersionSequnce, ProviderType providerType = ProviderType.Default)
        //{
        //    OASISResult<T1> result = new OASISResult<T1>();
        //    string errorMessage = "Error occured in OAPPManagerBase.RemoveRuntimeAsync. Reason:";

        //    try
        //    {
        //        OASISResult<T1> parentResult = Load(avatarId, parentId, templateVersionSequence, providerType: providerType);

        //        if (parentResult != null && parentResult.Result != null && !parentResult.IsError)
        //        {
        //            OASISResult<Runtime> runtimeResult = Data.LoadHolonByMetaData<Runtime>(new Dictionary<string, string>()
        //            {
        //                { "RuntimeId", runtimeId.ToString() },
        //                { "VersionSequence", runtimeVersionSequnce.ToString() }

        //            }, MetaKeyValuePairMatchMode.All, HolonType.Runtime, providerType: providerType);

        //            if (runtimeResult != null && runtimeResult.Result != null && !runtimeResult.IsError)
        //            {
        //                parentResult.Result.Runtimes.Remove((IRuntime)runtimeResult.Result);
        //                ISTARNETHolonMetaData metaData = parentResult.Result.RuntimesMetaData.FirstOrDefault(x => x.HolonId == runtimeResult.Result.Id);

        //                if (metaData != null)
        //                    parentResult.Result.RuntimesMetaData.Remove(metaData);

        //                parentResult.Result.STARNETDNA.MetaData["Runtimes"] = parentResult.Result.Runtimes;
        //                parentResult.Result.STARNETDNA.MetaData["RuntimesMetaData"] = parentResult.Result.RuntimesMetaData;
        //                result = Update(avatarId, parentResult.Result, result, errorMessage, providerType);

        //                string path = Path.Combine(parentResult.Result.STARNETDNA.SourcePath, "Dependencies", "STARNET", "Runtimes", string.Concat(runtimeResult.Result.STARNETDNA.Name, "_v", runtimeResult.Result.STARNETDNA.Version));

        //                if (Directory.Exists(path))
        //                    Directory.Exists(path);
        //            }
        //            else
        //                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the runtime with Data.LoadHolonByMetaData. Reason: {runtimeResult.Message}");
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

        //public async Task<OASISResult<T1>> RemoveRuntimeAsync(Guid avatarId, Guid parentId, string templateVersion, Guid runtimeId, string runtimeVersion, ProviderType providerType = ProviderType.Default)
        //{
        //    OASISResult<T1> result = new OASISResult<T1>();
        //    string errorMessage = "Error occured in OAPPManagerBase.RemoveRuntimeAsync. Reason:";

        //    try
        //    {
        //        OASISResult<T1> parentResult = await Data.LoadHolonByMetaDataAsync<T1>(new Dictionary<string, string>()
        //        {
        //            { STARNETHolonIdName , parentId.ToString() },
        //            { "Version", templateVersion }

        //        }, MetaKeyValuePairMatchMode.All, STARNETHolonType, providerType: providerType);

        //        if (parentResult != null && parentResult.Result != null && !parentResult.IsError)
        //        {
        //            OASISResult<Runtime> runtimeResult = await Data.LoadHolonByMetaDataAsync<Runtime>(new Dictionary<string, string>()
        //            {
        //                { "RuntimeId", runtimeId.ToString() },
        //                { "Version", runtimeVersion }

        //            }, MetaKeyValuePairMatchMode.All, HolonType.Runtime, providerType: providerType);

        //            if (runtimeResult != null && runtimeResult.Result != null && !runtimeResult.IsError)
        //            {
        //                parentResult.Result.Runtimes.Remove((IRuntime)runtimeResult.Result);
        //                ISTARNETHolonMetaData metaData = parentResult.Result.RuntimesMetaData.FirstOrDefault(x => x.HolonId == runtimeResult.Result.Id);

        //                if (metaData != null)
        //                    parentResult.Result.RuntimesMetaData.Remove(metaData);

        //                parentResult.Result.STARNETDNA.MetaData["Runtimes"] = parentResult.Result.Runtimes;
        //                parentResult.Result.STARNETDNA.MetaData["RuntimesMetaData"] = parentResult.Result.RuntimesMetaData;

        //                result = await UpdateAsync(avatarId, parentResult.Result, result, errorMessage, providerType);

        //                string path = Path.Combine(parentResult.Result.STARNETDNA.SourcePath, "Dependencies", "STARNET", "Runtimes", string.Concat(runtimeResult.Result.STARNETDNA.Name, "_v", runtimeResult.Result.STARNETDNA.Version));

        //                if (Directory.Exists(path))
        //                    Directory.Exists(path);
        //            }
        //            else
        //                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the runtime with Data.LoadHolonByMetaDataAsync. Reason: {runtimeResult.Message}");
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

        //public OASISResult<T1> RemoveRuntime(Guid avatarId, Guid parentId, string templateVersion, Guid runtimeId, string runtimeVersion, ProviderType providerType = ProviderType.Default)
        //{
        //    OASISResult<T1> result = new OASISResult<T1>();
        //    string errorMessage = "Error occured in OAPPManagerBase.RemoveRuntimeAsync. Reason:";

        //    try
        //    {
        //        OASISResult<T1> parentResult = Data.LoadHolonByMetaData<T1>(new Dictionary<string, string>()
        //        {
        //            { STARNETHolonIdName , parentId.ToString() },
        //            { "Version", templateVersion }

        //        }, MetaKeyValuePairMatchMode.All, STARNETHolonType, providerType: providerType);

        //        if (parentResult != null && parentResult.Result != null && !parentResult.IsError)
        //        {
        //            OASISResult<Runtime> runtimeResult = Data.LoadHolonByMetaData<Runtime>(new Dictionary<string, string>()
        //            {
        //                { "RuntimeId", runtimeId.ToString() },
        //                { "Version", runtimeVersion }

        //            }, MetaKeyValuePairMatchMode.All, HolonType.Runtime, providerType: providerType);

        //            if (runtimeResult != null && runtimeResult.Result != null && !runtimeResult.IsError)
        //            {
        //                parentResult.Result.Runtimes.Remove((IRuntime)runtimeResult.Result);
        //                ISTARNETHolonMetaData metaData = parentResult.Result.RuntimesMetaData.FirstOrDefault(x => x.HolonId == runtimeResult.Result.Id);

        //                if (metaData != null)
        //                    parentResult.Result.RuntimesMetaData.Remove(metaData);

        //                parentResult.Result.STARNETDNA.MetaData["Runtimes"] = parentResult.Result.Runtimes;
        //                parentResult.Result.STARNETDNA.MetaData["RuntimesMetaData"] = parentResult.Result.RuntimesMetaData;

        //                result = Update(avatarId, parentResult.Result, result, errorMessage, providerType);

        //                string path = Path.Combine(parentResult.Result.STARNETDNA.SourcePath, "Dependencies", "STARNET", "Runtimes", string.Concat(runtimeResult.Result.STARNETDNA.Name, "_v", runtimeResult.Result.STARNETDNA.Version));

        //                if (Directory.Exists(path))
        //                    Directory.Exists(path);
        //            }
        //            else
        //                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the runtime with Data.LoadHolonByMetaData. Reason: {runtimeResult.Message}");
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


        public async Task<OASISResult<T1>> AddRuntimeAsync(Guid avatarId, T1 parent, IInstalledRuntime installedRuntime, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            string errorMessage = "Error occured in OAPPManagerBase.AddRuntimeAsync. Reason:";

            try
            {
                OASISResult<Runtime> runtimeResult = await Data.LoadHolonByMetaDataAsync<Runtime>(new Dictionary<string, string>()
                {
                    { "RuntimeId", installedRuntime.STARNETDNA.Id.ToString() },
                    { "Version", installedRuntime.STARNETDNA.Version }

                }, MetaKeyValuePairMatchMode.All, HolonType.Runtime, providerType: providerType);

                if (runtimeResult != null && runtimeResult.Result != null && !runtimeResult.IsError)
                {
                    parent.Runtimes.Add(runtimeResult.Result);
                    parent.RuntimesMetaData.Add(new STARNETHolonMetaData()
                    {
                        HolonId = runtimeResult.Result.Id,
                        STARNETHolonId = runtimeResult.Result.STARNETDNA.Id,
                        Name = runtimeResult.Result.Name,
                        VersionSequence = runtimeResult.Result.STARNETDNA.VersionSequence,
                        Version = runtimeResult.Result.STARNETDNA.Version
                    });

                    parent.STARNETDNA.MetaData["Runtimes"] = parent.Runtimes;
                    parent.STARNETDNA.MetaData["RuntimesMetaData"] = parent.RuntimesMetaData;
                    result = Update(avatarId, parent, result, errorMessage, providerType);

                    if (result != null && result.Result != null && !result.IsError)
                        DirectoryHelper.CopyFilesRecursively(installedRuntime.InstalledPath, Path.Combine(parent.STARNETDNA.SourcePath, "Dependencies", "STARNET", "Templates"));
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the Runtime with Data.LoadHolonByMetaDataAsync. Reason: {runtimeResult.Message}");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured. Reason: {ex}");
            }

            return result;
        }

        public OASISResult<T1> AddRuntime(Guid avatarId, T1 parent, IInstalledRuntime installedRuntime, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            string errorMessage = "Error occured in OAPPManagerBase.AddRuntime. Reason:";

            try
            {
                OASISResult<Runtime> runtimeResult = Data.LoadHolonByMetaData<Runtime>(new Dictionary<string, string>()
                {
                    { "RuntimeId", installedRuntime.STARNETDNA.Id.ToString() },
                    { "Version", installedRuntime.STARNETDNA.Version }

                }, MetaKeyValuePairMatchMode.All, HolonType.Runtime, providerType: providerType);

                if (runtimeResult != null && runtimeResult.Result != null && !runtimeResult.IsError)
                {
                    parent.Runtimes.Add(runtimeResult.Result);
                    parent.RuntimesMetaData.Add(new STARNETHolonMetaData()
                    {
                        HolonId = runtimeResult.Result.Id,
                        STARNETHolonId = runtimeResult.Result.STARNETDNA.Id,
                        Name = runtimeResult.Result.Name,
                        VersionSequence = runtimeResult.Result.STARNETDNA.VersionSequence,
                        Version = runtimeResult.Result.STARNETDNA.Version
                    });

                    parent.STARNETDNA.MetaData["Runtimes"] = parent.Runtimes;
                    parent.STARNETDNA.MetaData["RuntimesMetaData"] = parent.RuntimesMetaData;
                    result = Update(avatarId, parent, result, errorMessage, providerType);

                    if (result != null && result.Result != null && !result.IsError)
                        DirectoryHelper.CopyFilesRecursively(installedRuntime.InstalledPath, Path.Combine(parent.STARNETDNA.SourcePath, "Dependencies", "STARNET", "Templates"));
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the Runtime with Data.LoadHolonByMetaData. Reason: {runtimeResult.Message}");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured. Reason: {ex}");
            }

            return result;
        }

        public async Task<OASISResult<T1>> AddRuntimeAsync(Guid avatarId, Guid parentId, string parentVersion, IInstalledRuntime installedRuntime, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            string errorMessage = "Error occured in OAPPManagerBase.AddRuntimeAsync. Reason:";

            try
            {
                OASISResult<T1> parentResult = await Data.LoadHolonByMetaDataAsync<T1>(new Dictionary<string, string>()
                {
                    { STARNETHolonIdName, parentId.ToString() },
                    { "Version", parentVersion }

                }, MetaKeyValuePairMatchMode.All, STARNETHolonType, providerType: providerType);

                if (parentResult != null && parentResult.Result != null && !parentResult.IsError)
                    return await AddRuntimeAsync(avatarId, parentResult.Result, installedRuntime, providerType);
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the {STARNETHolonUIName}  with OAPPManagerBase.LoadHolonByMetaDataAsync. Reason: {parentResult.Message}");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured. Reason: {ex}");
            }

            return result;
        }

        public OASISResult<T1> AddRuntime(Guid avatarId, Guid parentId, string parentVersion, IInstalledRuntime installedRuntime, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            string errorMessage = "Error occured in OAPPManagerBase.AddRuntime. Reason:";

            try
            {
                OASISResult<T1> parentResult = Data.LoadHolonByMetaData<T1>(new Dictionary<string, string>()
                {
                    { STARNETHolonIdName, parentId.ToString() },
                    { "Version", parentVersion }

                }, MetaKeyValuePairMatchMode.All, STARNETHolonType, providerType: providerType);

                if (parentResult != null && parentResult.Result != null && !parentResult.IsError)
                    return AddRuntime(avatarId, parentResult.Result, installedRuntime, providerType);
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the {STARNETHolonUIName}  with OAPPManagerBase.LoadHolonByMetaData. Reason: {parentResult.Message}");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured. Reason: {ex}");
            }

            return result;
        }

        public async Task<OASISResult<T1>> AddRuntimeAsync(Guid avatarId, Guid parentId, int parentVersionSequence, IInstalledRuntime installedRuntime, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            string errorMessage = "Error occured in OAPPManagerBase.AddRuntimeAsync. Reason:";

            try
            {
                OASISResult<T1> parentResult = await Data.LoadHolonByMetaDataAsync<T1>(new Dictionary<string, string>()
                {
                    { STARNETHolonIdName, parentId.ToString() },
                    { "VersionSequence", parentVersionSequence.ToString() }

                }, MetaKeyValuePairMatchMode.All, STARNETHolonType, providerType: providerType);

                if (parentResult != null && parentResult.Result != null && !parentResult.IsError)
                    return await AddRuntimeAsync(avatarId, parentResult.Result, installedRuntime, providerType);
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the {STARNETHolonUIName}  with OAPPManagerBase.LoadHolonByMetaDataAsync. Reason: {parentResult.Message}");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured. Reason: {ex}");
            }

            return result;
        }

        public OASISResult<T1> AddRuntime(Guid avatarId, Guid parentId, int parentVersionSequence, IInstalledRuntime installedRuntime, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            string errorMessage = "Error occured in OAPPManagerBase.AddRuntime. Reason:";

            try
            {
                OASISResult<T1> parentResult = Data.LoadHolonByMetaData<T1>(new Dictionary<string, string>()
                {
                    { STARNETHolonIdName, parentId.ToString() },
                    { "VersionSequence", parentVersionSequence.ToString() }

                }, MetaKeyValuePairMatchMode.All, STARNETHolonType, providerType: providerType);

                if (parentResult != null && parentResult.Result != null && !parentResult.IsError)
                    return AddRuntime(avatarId, parentResult.Result, installedRuntime, providerType);
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the {STARNETHolonUIName}  with OAPPManagerBase.LoadHolonByMetaData. Reason: {parentResult.Message}");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured. Reason: {ex}");
            }

            return result;
        }

        public async Task<OASISResult<T1>> AddRuntimeAsync(Guid avatarId, Guid parentId, int parentVersionSequence, Guid templateId, int libraryVersionSequence, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            string errorMessage = "Error occured in OAPPManagerBase.AddRuntimeAsync. Reason:";

            OASISResult<InstalledRuntime> installedLibResult = await Data.LoadHolonByMetaDataAsync<InstalledRuntime>(new Dictionary<string, string>()
            {
                { "RuntimeId", templateId.ToString() },
                { "VersionSequence", libraryVersionSequence.ToString() }

            }, MetaKeyValuePairMatchMode.All, HolonType.InstalledRuntime, providerType: providerType);

            if (installedLibResult != null && installedLibResult.Result != null && !installedLibResult.IsError)
                result = await AddRuntimeAsync(avatarId, parentId, parentVersionSequence, (IInstalledRuntime)installedLibResult.Result, providerType);
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the Installed Runtime with Data.LoadHolonByMetaDataAsync. Reason: {installedLibResult.Message}");

            return result;
        }

        public OASISResult<T1> AddRuntime(Guid avatarId, Guid parentId, int parentVersionSequence, Guid templateId, int libraryVersionSequence, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            string errorMessage = "Error occured in OAPPManagerBase.AddRuntime. Reason:";

            OASISResult<InstalledRuntime> installedLibResult = Data.LoadHolonByMetaData<InstalledRuntime>(new Dictionary<string, string>()
            {
                { "RuntimeId", templateId.ToString() },
                { "VersionSequence", libraryVersionSequence.ToString() }

            }, MetaKeyValuePairMatchMode.All, HolonType.InstalledRuntime, providerType: providerType);

            if (installedLibResult != null && installedLibResult.Result != null && !installedLibResult.IsError)
                result = AddRuntime(avatarId, parentId, parentVersionSequence, (IInstalledRuntime)installedLibResult.Result, providerType);
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the Installed Runtime with Data.LoadHolonByMetaData. Reason: {installedLibResult.Message}");

            return result;
        }

        public async Task<OASISResult<T1>> AddRuntimeAsync(Guid avatarId, Guid parentId, string parentVersion, Guid templateId, string libraryVersion, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            string errorMessage = "Error occured in OAPPManagerBase.AddRuntimeAsync. Reason:";

            OASISResult<InstalledRuntime> installedLibResult = await Data.LoadHolonByMetaDataAsync<InstalledRuntime>(new Dictionary<string, string>()
            {
                { "RuntimeId", templateId.ToString() },
                { "Version", libraryVersion }

            }, MetaKeyValuePairMatchMode.All, HolonType.InstalledRuntime, providerType: providerType);

            if (installedLibResult != null && installedLibResult.Result != null && !installedLibResult.IsError)
                result = await AddRuntimeAsync(avatarId, parentId, parentVersion, (IInstalledRuntime)installedLibResult.Result, providerType);
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the Installed Runtime with Data.LoadHolonByMetaDataAsync. Reason: {installedLibResult.Message}");

            return result;
        }

        public OASISResult<T1> AddRuntime(Guid avatarId, Guid parentId, string parentVersion, Guid templateId, string libraryVersion, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            string errorMessage = "Error occured in OAPPManagerBase.AddRuntime. Reason:";

            OASISResult<InstalledRuntime> installedLibResult = Data.LoadHolonByMetaData<InstalledRuntime>(new Dictionary<string, string>()
            {
                { "RuntimeId", templateId.ToString() },
                { "Version", libraryVersion }

            }, MetaKeyValuePairMatchMode.All, HolonType.InstalledRuntime, providerType: providerType);

            if (installedLibResult != null && installedLibResult.Result != null && !installedLibResult.IsError)
                result = AddRuntime(avatarId, parentId, parentVersion, (IInstalledRuntime)installedLibResult.Result, providerType);
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the Installed Runtime with Data.LoadHolonByMetaData. Reason: {installedLibResult.Message}");

            return result;
        }


        public async Task<OASISResult<T1>> RemoveRuntimeAsync(Guid avatarId, Guid parentId, int parentVersionSequence, Guid templateId, int libraryVersionSequence, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            string errorMessage = "Error occured in OAPPManagerBase.RemoveRuntimeAsync. Reason:";

            try
            {
                OASISResult<T1> parentResult = await LoadAsync(avatarId, parentId, parentVersionSequence, providerType: providerType);

                if (parentResult != null && parentResult.Result != null && !parentResult.IsError)
                {
                    OASISResult<Runtime> runtimeResult = await Data.LoadHolonByMetaDataAsync<Runtime>(new Dictionary<string, string>()
                    {
                        { "RuntimeId", templateId.ToString() },
                        { "VersionSequence", libraryVersionSequence.ToString() }

                    }, MetaKeyValuePairMatchMode.All, HolonType.Runtime, providerType: providerType);

                    if (runtimeResult != null && runtimeResult.Result != null && !runtimeResult.IsError)
                    {
                        parentResult.Result.Runtimes.Remove((IRuntime)runtimeResult.Result);
                        ISTARNETHolonMetaData metaData = parentResult.Result.RuntimesMetaData.FirstOrDefault(x => x.HolonId == runtimeResult.Result.Id);

                        if (metaData != null)
                            parentResult.Result.RuntimesMetaData.Remove(metaData);

                        parentResult.Result.STARNETDNA.MetaData["Runtimes"] = parentResult.Result.Runtimes;
                        parentResult.Result.STARNETDNA.MetaData["RuntimesMetaData"] = parentResult.Result.RuntimesMetaData;
                        result = await UpdateAsync(avatarId, parentResult.Result, result, errorMessage, providerType);

                        string path = Path.Combine(parentResult.Result.STARNETDNA.SourcePath, "Dependencies", "STARNET", "Templates", string.Concat(runtimeResult.Result.STARNETDNA.Name, "_v", runtimeResult.Result.STARNETDNA.Version));

                        if (Directory.Exists(path))
                            Directory.Exists(path);
                    }
                    else
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the Runtime with Data.LoadHolonByMetaDataAsync. Reason: {runtimeResult.Message}");
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the {STARNETHolonUIName}  with OAPPManagerBase.LoadHolonByMetaDataAsync. Reason: {parentResult.Message}");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured. Reason: {ex}");
            }

            return result;
        }

        public OASISResult<T1> RemoveRuntime(Guid avatarId, Guid parentId, int parentVersionSequence, Guid templateId, int libraryVersionSequnce, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            string errorMessage = "Error occured in OAPPManagerBase.RemoveRuntimeAsync. Reason:";

            try
            {
                OASISResult<T1> parentResult = Load(avatarId, parentId, parentVersionSequence, providerType: providerType);

                if (parentResult != null && parentResult.Result != null && !parentResult.IsError)
                {
                    OASISResult<Runtime> runtimeResult = Data.LoadHolonByMetaData<Runtime>(new Dictionary<string, string>()
                    {
                        { "RuntimeId", templateId.ToString() },
                        { "VersionSequence", libraryVersionSequnce.ToString() }

                    }, MetaKeyValuePairMatchMode.All, HolonType.Runtime, providerType: providerType);

                    if (runtimeResult != null && runtimeResult.Result != null && !runtimeResult.IsError)
                    {
                        parentResult.Result.Runtimes.Remove((IRuntime)runtimeResult.Result);
                        ISTARNETHolonMetaData metaData = parentResult.Result.RuntimesMetaData.FirstOrDefault(x => x.HolonId == runtimeResult.Result.Id);

                        if (metaData != null)
                            parentResult.Result.RuntimesMetaData.Remove(metaData);

                        parentResult.Result.STARNETDNA.MetaData["Runtimes"] = parentResult.Result.Runtimes;
                        parentResult.Result.STARNETDNA.MetaData["RuntimesMetaData"] = parentResult.Result.RuntimesMetaData;
                        result = Update(avatarId, parentResult.Result, result, errorMessage, providerType);

                        string path = Path.Combine(parentResult.Result.STARNETDNA.SourcePath, "Dependencies", "STARNET", "Templates", string.Concat(runtimeResult.Result.STARNETDNA.Name, "_v", runtimeResult.Result.STARNETDNA.Version));

                        if (Directory.Exists(path))
                            Directory.Exists(path);
                    }
                    else
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the Runtime with Data.LoadHolonByMetaData. Reason: {runtimeResult.Message}");
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the {STARNETHolonUIName}  with OAPPManagerBase.LoadHolonByMetaData. Reason: {parentResult.Message}");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured. Reason: {ex}");
            }

            return result;
        }

        public async Task<OASISResult<T1>> RemoveRuntimeAsync(Guid avatarId, Guid parentId, string parentVersion, Guid templateId, string libraryVersion, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            string errorMessage = "Error occured in OAPPManagerBase.RemoveRuntimeAsync. Reason:";

            try
            {
                OASISResult<T1> parentResult = await Data.LoadHolonByMetaDataAsync<T1>(new Dictionary<string, string>()
                {
                    { STARNETHolonIdName, parentId.ToString() },
                    { "Version", parentVersion }

                }, MetaKeyValuePairMatchMode.All, STARNETHolonType, providerType: providerType);

                if (parentResult != null && parentResult.Result != null && !parentResult.IsError)
                {
                    OASISResult<Runtime> runtimeResult = await Data.LoadHolonByMetaDataAsync<Runtime>(new Dictionary<string, string>()
                    {
                        { "RuntimeId", templateId.ToString() },
                        { "Version", libraryVersion }

                    }, MetaKeyValuePairMatchMode.All, HolonType.Runtime, providerType: providerType);

                    if (runtimeResult != null && runtimeResult.Result != null && !runtimeResult.IsError)
                    {
                        parentResult.Result.Runtimes.Remove((IRuntime)runtimeResult.Result);
                        ISTARNETHolonMetaData metaData = parentResult.Result.RuntimesMetaData.FirstOrDefault(x => x.HolonId == runtimeResult.Result.Id);

                        if (metaData != null)
                            parentResult.Result.RuntimesMetaData.Remove(metaData);

                        parentResult.Result.STARNETDNA.MetaData["Runtimes"] = parentResult.Result.Runtimes;
                        parentResult.Result.STARNETDNA.MetaData["RuntimesMetaData"] = parentResult.Result.RuntimesMetaData;
                        result = await UpdateAsync(avatarId, parentResult.Result, result, errorMessage, providerType);

                        string path = Path.Combine(parentResult.Result.STARNETDNA.SourcePath, "Dependencies", "STARNET", "Templates", string.Concat(runtimeResult.Result.STARNETDNA.Name, "_v", runtimeResult.Result.STARNETDNA.Version));

                        if (Directory.Exists(path))
                            Directory.Exists(path);
                    }
                    else
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the Runtime with Data.LoadHolonByMetaDataAsync. Reason: {runtimeResult.Message}");
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the {STARNETHolonUIName}  with OAPPManagerBase.LoadHolonByMetaDataAsync. Reason: {parentResult.Message}");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured. Reason: {ex}");
            }

            return result;
        }

        public OASISResult<T1> RemoveRuntime(Guid avatarId, Guid parentId, string parentVersion, Guid templateId, string libraryVersion, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            string errorMessage = "Error occured in OAPPManagerBase.RemoveRuntimeAsync. Reason:";

            try
            {
                OASISResult<T1> parentResult = Data.LoadHolonByMetaData<T1>(new Dictionary<string, string>()
                {
                    { STARNETHolonIdName, templateId.ToString() },
                    { "Version", parentVersion }

                }, MetaKeyValuePairMatchMode.All, STARNETHolonType, providerType: providerType);

                if (parentResult != null && parentResult.Result != null && !parentResult.IsError)
                {
                    OASISResult<Runtime> runtimeResult = Data.LoadHolonByMetaData<Runtime>(new Dictionary<string, string>()
                    {
                        { "RuntimeId", templateId.ToString() },
                        { "Version", libraryVersion.ToString() }

                    }, MetaKeyValuePairMatchMode.All, HolonType.Runtime, providerType: providerType);

                    if (runtimeResult != null && runtimeResult.Result != null && !runtimeResult.IsError)
                    {
                        parentResult.Result.Runtimes.Remove((IRuntime)runtimeResult.Result);
                        ISTARNETHolonMetaData metaData = parentResult.Result.RuntimesMetaData.FirstOrDefault(x => x.HolonId == runtimeResult.Result.Id);

                        if (metaData != null)
                            parentResult.Result.RuntimesMetaData.Remove(metaData);

                        parentResult.Result.STARNETDNA.MetaData["Runtimes"] = parentResult.Result.Runtimes;
                        parentResult.Result.STARNETDNA.MetaData["RuntimesMetaData"] = parentResult.Result.RuntimesMetaData;
                        result = Update(avatarId, parentResult.Result, result, errorMessage, providerType);

                        string path = Path.Combine(parentResult.Result.STARNETDNA.SourcePath, "Dependencies", "STARNET", "Templates", string.Concat(runtimeResult.Result.STARNETDNA.Name, "_v", runtimeResult.Result.STARNETDNA.Version));

                        if (Directory.Exists(path))
                            Directory.Exists(path);
                    }
                    else
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the Runtime with Data.LoadHolonByMetaData. Reason: {runtimeResult.Message}");
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the {STARNETHolonUIName} with OAPPManagerBase.LoadHolonByMetaData. Reason: {parentResult.Message}");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured. Reason: {ex}");
            }

            return result;
        }




        //public async Task<OASISResult<T1>> AddLibraryAsync(Guid avatarId, Guid parentId, int templateVersion, Guid libraryId, int libraryVersion, ProviderType providerType = ProviderType.Default)
        //{
        //    OASISResult<T1> result = new OASISResult<T1>();
        //    string errorMessage = "Error occured in OAPPManagerBase.AddLibraryAsync. Reason:";

        //    try
        //    {
        //        OASISResult<T1> parentResult = await LoadAsync(avatarId, parentId, templateVersion, providerType: providerType);

        //        if (parentResult != null && parentResult.Result != null && !parentResult.IsError)
        //        {
        //            OASISResult<Library> libraryResult = await Data.LoadHolonByMetaDataAsync<Library>(new Dictionary<string, string>()
        //            {
        //                { "LibraryId", libraryId.ToString() },
        //                { "VersionSequence", libraryVersion.ToString() }

        //            }, MetaKeyValuePairMatchMode.All, HolonType.Library, providerType: providerType);

        //            if (libraryResult != null && libraryResult.Result != null && !libraryResult.IsError)
        //            {
        //                parentResult.Result.Libraries.Add((ILibrary)libraryResult.Result);
        //                parentResult.Result.LibrariesMetaData.Add((ISTARNETHolonMetaData)new STARNETHolonMetaData()
        //                {
        //                    HolonId = libraryResult.Result.Id,
        //                    STARNETHolonId = libraryResult.Result.STARNETDNA.Id,
        //                    Name = libraryResult.Result.Name,
        //                    VersionSequence = libraryResult.Result.STARNETDNA.VersionSequence,
        //                    Version = libraryResult.Result.STARNETDNA.Version
        //                });

        //                parentResult.Result.STARNETDNA.MetaData["Libs"] = parentResult.Result.Libraries;
        //                parentResult.Result.STARNETDNA.MetaData["LibsMetaData"] = parentResult.Result.LibrariesMetaData;

        //                result = await UpdateAsync(avatarId, parentResult.Result, result, errorMessage, providerType);

        //                if (result != null && result.Result != null && !result.IsError)
        //                    DirectoryHelper.CopyFilesRecursively(installedLib.InstalledPath, Path.Combine(parentResult.Result.STARNETDNA.SourcePath, "Dependencies", "STARNET", "Libs"));
        //            }
        //            else
        //                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the library with Data.LoadHolonByMetaDataAsync. Reason: {libraryResult.Message}");
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

        //public OASISResult<T1> AddLibrary(Guid avatarId, Guid parentId, int templateVersionSequence, Guid libraryId, int libraryVersionSequnce, ProviderType providerType = ProviderType.Default)
        //{
        //    OASISResult<T1> result = new OASISResult<T1>();
        //    string errorMessage = "Error occured in OAPPManagerBase.AddLibraryAsync. Reason:";

        //    try
        //    {
        //        OASISResult<T1> parentResult = Load(avatarId, parentId, templateVersionSequence, providerType: providerType);

        //        if (parentResult != null && parentResult.Result != null && !parentResult.IsError)
        //        {
        //            OASISResult<Library> libraryResult = Data.LoadHolonByMetaData<Library>(new Dictionary<string, string>()
        //            {
        //                { "LibraryId", libraryId.ToString() },
        //                { "VersionSequence", libraryVersionSequnce.ToString() }

        //            }, MetaKeyValuePairMatchMode.All, HolonType.Library, providerType: providerType);

        //            if (libraryResult != null && libraryResult.Result != null && !libraryResult.IsError)
        //            {
        //                parentResult.Result.Libraries.Add((ILibrary)libraryResult.Result);
        //                parentResult.Result.LibrariesMetaData.Add((ISTARNETHolonMetaData)new STARNETHolonMetaData()
        //                {
        //                    HolonId = libraryResult.Result.Id,
        //                    STARNETHolonId = libraryResult.Result.STARNETDNA.Id,
        //                    Name = libraryResult.Result.Name,
        //                    VersionSequence = libraryResult.Result.STARNETDNA.VersionSequence,
        //                    Version = libraryResult.Result.STARNETDNA.Version
        //                });

        //                parentResult.Result.STARNETDNA.MetaData["Libs"] = parentResult.Result.Libraries;
        //                parentResult.Result.STARNETDNA.MetaData["LibsMetaData"] = parentResult.Result.LibrariesMetaData;

        //                result = Update(avatarId, parentResult.Result, result, errorMessage, providerType);

        //                if (result != null && result.Result != null && !result.IsError)
        //                    DirectoryHelper.CopyFilesRecursively(installedLib.InstalledPath, Path.Combine(parentResult.Result.STARNETDNA.SourcePath, "Dependencies", "STARNET", "Libs"));
        //            }
        //            else
        //                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the library with Data.LoadHolonByMetaData. Reason: {libraryResult.Message}");
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

        //public async Task<OASISResult<T1>> AddLibraryAsync(Guid avatarId, Guid parentId, string templateVersion, Guid libraryId, string libraryVersion, ProviderType providerType = ProviderType.Default)
        //{
        //    OASISResult<T1> result = new OASISResult<T1>();
        //    string errorMessage = "Error occured in OAPPManagerBase.AddLibraryAsync. Reason:";

        //    try
        //    {
        //        OASISResult<T1> parentResult = await Data.LoadHolonByMetaDataAsync<T1>(new Dictionary<string, string>()
        //        {
        //            { STARNETHolonIdName, parentId.ToString() },
        //            { "Version", templateVersion }

        //        }, MetaKeyValuePairMatchMode.All, STARNETHolonType, providerType: providerType);

        //        if (parentResult != null && parentResult.Result != null && !parentResult.IsError)
        //        {
        //            OASISResult<Library> libraryResult = await Data.LoadHolonByMetaDataAsync<Library>(new Dictionary<string, string>()
        //            {
        //                { "LibraryId", libraryId.ToString() },
        //                { "Version", libraryVersion.ToString() }

        //            }, MetaKeyValuePairMatchMode.All, HolonType.Library, providerType: providerType);

        //            if (libraryResult != null && libraryResult.Result != null && !libraryResult.IsError)
        //            {
        //                parentResult.Result.Libraries.Add((ILibrary)libraryResult.Result);
        //                parentResult.Result.LibrariesMetaData.Add((ISTARNETHolonMetaData)new STARNETHolonMetaData()
        //                {
        //                    HolonId = libraryResult.Result.Id,
        //                    STARNETHolonId = libraryResult.Result.STARNETDNA.Id,
        //                    Name = libraryResult.Result.Name,
        //                    VersionSequence = libraryResult.Result.STARNETDNA.VersionSequence,
        //                    Version = libraryResult.Result.STARNETDNA.Version
        //                });

        //                parentResult.Result.STARNETDNA.MetaData["Libs"] = parentResult.Result.Libraries;
        //                parentResult.Result.STARNETDNA.MetaData["LibsMetaData"] = parentResult.Result.LibrariesMetaData;

        //                result = await UpdateAsync(avatarId, parentResult.Result, result, errorMessage, providerType);

        //                if (result != null && result.Result != null && !result.IsError)
        //                    DirectoryHelper.CopyFilesRecursively(installedLib.InstalledPath, Path.Combine(parentResult.Result.STARNETDNA.SourcePath, "Dependencies", "STARNET", "Libs"));
        //            }
        //            else
        //                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the library with Data.LoadHolonByMetaDataAsync. Reason: {libraryResult.Message}");
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

        //public OASISResult<T1> AddLibrary(Guid avatarId, Guid parentId, string templateVersion, Guid libraryId, string libraryVersion, ProviderType providerType = ProviderType.Default)
        //{
        //    OASISResult<T1> result = new OASISResult<T1>();
        //    string errorMessage = "Error occured in OAPPManagerBase.AddLibraryAsync. Reason:";

        //    try
        //    {
        //        OASISResult<T1> parentResult = Data.LoadHolonByMetaData<T1>(new Dictionary<string, string>()
        //        {
        //            { STARNETHolonIdName, parentId.ToString() },
        //            { "Version", templateVersion }

        //        }, MetaKeyValuePairMatchMode.All, STARNETHolonType, providerType: providerType);

        //        if (parentResult != null && parentResult.Result != null && !parentResult.IsError)
        //        {
        //            OASISResult<Library> libraryResult = Data.LoadHolonByMetaData<Library>(new Dictionary<string, string>()
        //            {
        //                { "LibraryId", libraryId.ToString() },
        //                { "Version", libraryVersion }

        //            }, MetaKeyValuePairMatchMode.All, HolonType.Library, providerType: providerType);

        //            if (libraryResult != null && libraryResult.Result != null && !libraryResult.IsError)
        //            {
        //                parentResult.Result.Libraries.Add((ILibrary)libraryResult.Result);
        //                parentResult.Result.LibrariesMetaData.Add((ISTARNETHolonMetaData)new STARNETHolonMetaData()
        //                {
        //                    HolonId = libraryResult.Result.Id,
        //                    STARNETHolonId = libraryResult.Result.STARNETDNA.Id,
        //                    Name = libraryResult.Result.Name,
        //                    VersionSequence = libraryResult.Result.STARNETDNA.VersionSequence,
        //                    Version = libraryResult.Result.STARNETDNA.Version
        //                });

        //                parentResult.Result.STARNETDNA.MetaData["Libs"] = parentResult.Result.Libraries;
        //                parentResult.Result.STARNETDNA.MetaData["LibsMetaData"] = parentResult.Result.LibrariesMetaData;

        //                result = Update(avatarId, parentResult.Result, result, errorMessage, providerType);

        //                if (result != null && result.Result != null && !result.IsError)
        //                    DirectoryHelper.CopyFilesRecursively(installedLib.InstalledPath, Path.Combine(parentResult.Result.STARNETDNA.SourcePath, "Dependencies", "STARNET", "Libs"));
        //            }
        //            else
        //                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the library with Data.LoadHolonByMetaData. Reason: {libraryResult.Message}");
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


        //public async Task<OASISResult<T1>> RemoveLibraryAsync(Guid avatarId, Guid parentId, int templateVersionSequence, Guid libraryId, int libraryVersionSequence, ProviderType providerType = ProviderType.Default)
        //{
        //    OASISResult<T1> result = new OASISResult<T1>();
        //    string errorMessage = "Error occured in OAPPManagerBase.RemoveLibraryAsync. Reason:";

        //    try
        //    {
        //        OASISResult<T1> parentResult = await LoadAsync(avatarId, parentId, templateVersionSequence, providerType: providerType);

        //        if (parentResult != null && parentResult.Result != null && !parentResult.IsError)
        //        {
        //            OASISResult<Library> libraryResult = await Data.LoadHolonByMetaDataAsync<Library>(new Dictionary<string, string>()
        //            {
        //                { "LibraryId", libraryId.ToString() },
        //                { "VersionSequence", libraryVersionSequence.ToString() }

        //            }, MetaKeyValuePairMatchMode.All, HolonType.Library, providerType: providerType);

        //            if (libraryResult != null && libraryResult.Result != null && !libraryResult.IsError)
        //            {
        //                parentResult.Result.Libraries.Remove((ILibrary)libraryResult.Result);
        //                ISTARNETHolonMetaData metaData = parentResult.Result.LibrariesMetaData.FirstOrDefault(x => x.HolonId == libraryResult.Result.Id);

        //                if (metaData != null)
        //                    parentResult.Result.LibrariesMetaData.Remove(metaData);

        //                parentResult.Result.STARNETDNA.MetaData["Libs"] = parentResult.Result.Libraries;
        //                parentResult.Result.STARNETDNA.MetaData["LibsMetaData"] = parentResult.Result.LibrariesMetaData;

        //                result = await UpdateAsync(avatarId, parentResult.Result, result, errorMessage, providerType);
        //                string path = Path.Combine(parentResult.Result.STARNETDNA.SourcePath, "Dependencies", "STARNET", "Libs", string.Concat(libraryResult.Result.STARNETDNA.Name, "_v", libraryResult.Result.STARNETDNA.Version));

        //                if (Directory.Exists(path))
        //                    Directory.Exists(path);
        //            }
        //            else
        //                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the library with Data.LoadHolonByMetaDataAsync. Reason: {libraryResult.Message}");
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

        //public OASISResult<T1> RemoveLibrary(Guid avatarId, Guid parentId, int templateVersionSequence, Guid libraryId, int libraryVersionSequnce, ProviderType providerType = ProviderType.Default)
        //{
        //    OASISResult<T1> result = new OASISResult<T1>();
        //    string errorMessage = "Error occured in OAPPManagerBase.RemoveLibraryAsync. Reason:";

        //    try
        //    {
        //        OASISResult<T1> parentResult = Load(avatarId, parentId, templateVersionSequence, providerType: providerType);

        //        if (parentResult != null && parentResult.Result != null && !parentResult.IsError)
        //        {
        //            OASISResult<Library> libraryResult = Data.LoadHolonByMetaData<Library>(new Dictionary<string, string>()
        //            {
        //                { "LibraryId", libraryId.ToString() },
        //                { "VersionSequence", libraryVersionSequnce.ToString() }

        //            }, MetaKeyValuePairMatchMode.All, HolonType.Library, providerType: providerType);

        //            if (libraryResult != null && libraryResult.Result != null && !libraryResult.IsError)
        //            {
        //                parentResult.Result.Libraries.Remove((ILibrary)libraryResult.Result);
        //                ISTARNETHolonMetaData metaData = parentResult.Result.LibrariesMetaData.FirstOrDefault(x => x.HolonId == libraryResult.Result.Id);

        //                if (metaData != null)
        //                    parentResult.Result.LibrariesMetaData.Remove(metaData);

        //                parentResult.Result.STARNETDNA.MetaData["Libs"] = parentResult.Result.Libraries;
        //                parentResult.Result.STARNETDNA.MetaData["LibsMetaData"] = parentResult.Result.LibrariesMetaData;
        //                result = Update(avatarId, parentResult.Result, result, errorMessage, providerType);
        //                string path = Path.Combine(parentResult.Result.STARNETDNA.SourcePath, "Dependencies", "STARNET", "Libs", string.Concat(libraryResult.Result.STARNETDNA.Name, "_v", libraryResult.Result.STARNETDNA.Version));

        //                if (Directory.Exists(path))
        //                    Directory.Exists(path);
        //            }
        //            else
        //                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the library with Data.LoadHolonByMetaData. Reason: {libraryResult.Message}");
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

        //public async Task<OASISResult<T1>> RemoveLibraryAsync(Guid avatarId, Guid parentId, string templateVersion, Guid libraryId, string libraryVersion, ProviderType providerType = ProviderType.Default)
        //{
        //    OASISResult<T1> result = new OASISResult<T1>();
        //    string errorMessage = "Error occured in OAPPManagerBase.RemoveLibraryAsync. Reason:";

        //    try
        //    {
        //        OASISResult<T1> parentResult = await Data.LoadHolonByMetaDataAsync<T1>(new Dictionary<string, string>()
        //        {
        //            { STARNETHolonIdName, parentId.ToString() },
        //            { "Version", templateVersion }

        //        }, MetaKeyValuePairMatchMode.All, STARNETHolonType, providerType: providerType);

        //        if (parentResult != null && parentResult.Result != null && !parentResult.IsError)
        //        {
        //            OASISResult<Library> libraryResult = await Data.LoadHolonByMetaDataAsync<Library>(new Dictionary<string, string>()
        //            {
        //                { "LibraryId", libraryId.ToString() },
        //                { "Version", libraryVersion }

        //            }, MetaKeyValuePairMatchMode.All, HolonType.Library, providerType: providerType);

        //            if (libraryResult != null && libraryResult.Result != null && !libraryResult.IsError)
        //            {
        //                parentResult.Result.Libraries.Remove((ILibrary)libraryResult.Result);
        //                ISTARNETHolonMetaData metaData = parentResult.Result.LibrariesMetaData.FirstOrDefault(x => x.HolonId == libraryResult.Result.Id);

        //                if (metaData != null)
        //                    parentResult.Result.LibrariesMetaData.Remove(metaData);

        //                parentResult.Result.STARNETDNA.MetaData["Libs"] = parentResult.Result.Libraries;
        //                parentResult.Result.STARNETDNA.MetaData["LibsMetaData"] = parentResult.Result.LibrariesMetaData;

        //                result = await UpdateAsync(avatarId, parentResult.Result, result, errorMessage, providerType);
        //                string path = Path.Combine(parentResult.Result.STARNETDNA.SourcePath, "Dependencies", "STARNET", "Libs", string.Concat(libraryResult.Result.STARNETDNA.Name, "_v", libraryResult.Result.STARNETDNA.Version));

        //                if (Directory.Exists(path))
        //                    Directory.Exists(path);
        //            }
        //            else
        //                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the library with Data.LoadHolonByMetaDataAsync. Reason: {libraryResult.Message}");
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

        //public OASISResult<T1> RemoveLibrary(Guid avatarId, Guid parentId, string templateVersion, Guid libraryId, string libraryVersion, ProviderType providerType = ProviderType.Default)
        //{
        //    OASISResult<T1> result = new OASISResult<T1>();
        //    string errorMessage = "Error occured in OAPPManagerBase.RemoveLibraryAsync. Reason:";

        //    try
        //    {
        //        OASISResult<T1> parentResult = Data.LoadHolonByMetaData<T1>(new Dictionary<string, string>()
        //        {
        //            { STARNETHolonIdName, libraryId.ToString() },
        //            { "Version", templateVersion }

        //        }, MetaKeyValuePairMatchMode.All, STARNETHolonType, providerType: providerType);

        //        if (parentResult != null && parentResult.Result != null && !parentResult.IsError)
        //        {
        //            OASISResult<Library> libraryResult = Data.LoadHolonByMetaData<Library>(new Dictionary<string, string>()
        //            {
        //                { "LibraryId", libraryId.ToString() },
        //                { "Version", libraryVersion.ToString() }

        //            }, MetaKeyValuePairMatchMode.All, HolonType.Library, providerType: providerType);

        //            if (libraryResult != null && libraryResult.Result != null && !libraryResult.IsError)
        //            {
        //                parentResult.Result.Libraries.Remove((ILibrary)libraryResult.Result);
        //                ISTARNETHolonMetaData metaData = parentResult.Result.LibrariesMetaData.FirstOrDefault(x => x.HolonId == libraryResult.Result.Id);

        //                if (metaData != null)
        //                    parentResult.Result.LibrariesMetaData.Remove(metaData);

        //                parentResult.Result.STARNETDNA.MetaData["Libs"] = parentResult.Result.Libraries;
        //                parentResult.Result.STARNETDNA.MetaData["LibsMetaData"] = parentResult.Result.LibrariesMetaData;

        //                result = Update(avatarId, parentResult.Result, result, errorMessage, providerType);
        //                string path = Path.Combine(parentResult.Result.STARNETDNA.SourcePath, "Dependencies", "STARNET", "Libs", string.Concat(libraryResult.Result.STARNETDNA.Name, "_v", libraryResult.Result.STARNETDNA.Version));

        //                if (Directory.Exists(path))
        //                    Directory.Exists(path);
        //            }
        //            else
        //                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the library with Data.LoadHolonByMetaData. Reason: {libraryResult.Message}");
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



        public async Task<OASISResult<T1>> AddLibraryAsync(Guid avatarId, T1 parent, IInstalledLibrary installedLibrary, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            string errorMessage = "Error occured in OAPPManagerBase.AddLibraryAsync. Reason:";

            try
            {
                OASISResult<Library> libResult = await Data.LoadHolonByMetaDataAsync<Library>(new Dictionary<string, string>()
                {
                    { "LibraryId", installedLibrary.STARNETDNA.Id.ToString() },
                    { "Version", installedLibrary.STARNETDNA.Version }

                }, MetaKeyValuePairMatchMode.All, HolonType.Library, providerType: providerType);

                if (libResult != null && libResult.Result != null && !libResult.IsError)
                {
                    parent.Libraries.Add(libResult.Result);
                    parent.LibrariesMetaData.Add(new STARNETHolonMetaData()
                    {
                        HolonId = libResult.Result.Id,
                        STARNETHolonId = libResult.Result.STARNETDNA.Id,
                        Name = libResult.Result.Name,
                        VersionSequence = libResult.Result.STARNETDNA.VersionSequence,
                        Version = libResult.Result.STARNETDNA.Version
                    });

                    parent.STARNETDNA.MetaData["Libraries"] = parent.Libraries;
                    parent.STARNETDNA.MetaData["LibrariesMetaData"] = parent.LibrariesMetaData;
                    result = Update(avatarId, parent, result, errorMessage, providerType);

                    if (result != null && result.Result != null && !result.IsError)
                        DirectoryHelper.CopyFilesRecursively(installedLibrary.InstalledPath, Path.Combine(parent.STARNETDNA.SourcePath, "Dependencies", "STARNET", "Templates"));
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the Library with Data.LoadHolonByMetaDataAsync. Reason: {libResult.Message}");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured. Reason: {ex}");
            }

            return result;
        }

        public OASISResult<T1> AddLibrary(Guid avatarId, T1 parent, IInstalledLibrary installedLibrary, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            string errorMessage = "Error occured in OAPPManagerBase.AddLibrary. Reason:";

            try
            {
                OASISResult<Library> libResult = Data.LoadHolonByMetaData<Library>(new Dictionary<string, string>()
                {
                    { "LibraryId", installedLibrary.STARNETDNA.Id.ToString() },
                    { "Version", installedLibrary.STARNETDNA.Version }

                }, MetaKeyValuePairMatchMode.All, HolonType.Library, providerType: providerType);

                if (libResult != null && libResult.Result != null && !libResult.IsError)
                {
                    parent.Libraries.Add(libResult.Result);
                    parent.LibrariesMetaData.Add(new STARNETHolonMetaData()
                    {
                        HolonId = libResult.Result.Id,
                        STARNETHolonId = libResult.Result.STARNETDNA.Id,
                        Name = libResult.Result.Name,
                        VersionSequence = libResult.Result.STARNETDNA.VersionSequence,
                        Version = libResult.Result.STARNETDNA.Version
                    });

                    parent.STARNETDNA.MetaData["Libraries"] = parent.Libraries;
                    parent.STARNETDNA.MetaData["LibrariesMetaData"] = parent.LibrariesMetaData;
                    result = Update(avatarId, parent, result, errorMessage, providerType);

                    if (result != null && result.Result != null && !result.IsError)
                        DirectoryHelper.CopyFilesRecursively(installedLibrary.InstalledPath, Path.Combine(parent.STARNETDNA.SourcePath, "Dependencies", "STARNET", "Templates"));
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the Library with Data.LoadHolonByMetaData. Reason: {libResult.Message}");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured. Reason: {ex}");
            }

            return result;
        }

        public async Task<OASISResult<T1>> AddLibraryAsync(Guid avatarId, Guid parentId, string parentVersion, IInstalledLibrary installedLibrary, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            string errorMessage = "Error occured in OAPPManagerBase.AddLibraryAsync. Reason:";

            try
            {
                OASISResult<T1> parentResult = await Data.LoadHolonByMetaDataAsync<T1>(new Dictionary<string, string>()
                {
                    { STARNETHolonIdName, parentId.ToString() },
                    { "Version", parentVersion }

                }, MetaKeyValuePairMatchMode.All, STARNETHolonType, providerType: providerType);

                if (parentResult != null && parentResult.Result != null && !parentResult.IsError)
                    return await AddLibraryAsync(avatarId, parentResult.Result, installedLibrary, providerType);
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the {STARNETHolonUIName}  with OAPPManagerBase.LoadHolonByMetaDataAsync. Reason: {parentResult.Message}");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured. Reason: {ex}");
            }

            return result;
        }

        public OASISResult<T1> AddLibrary(Guid avatarId, Guid parentId, string parentVersion, IInstalledLibrary installedLibrary, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            string errorMessage = "Error occured in OAPPManagerBase.AddLibrary. Reason:";

            try
            {
                OASISResult<T1> parentResult = Data.LoadHolonByMetaData<T1>(new Dictionary<string, string>()
                {
                    { STARNETHolonIdName, parentId.ToString() },
                    { "Version", parentVersion }

                }, MetaKeyValuePairMatchMode.All, STARNETHolonType, providerType: providerType);

                if (parentResult != null && parentResult.Result != null && !parentResult.IsError)
                    return AddLibrary(avatarId, parentResult.Result, installedLibrary, providerType);
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the {STARNETHolonUIName}  with OAPPManagerBase.LoadHolonByMetaData. Reason: {parentResult.Message}");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured. Reason: {ex}");
            }

            return result;
        }

        public async Task<OASISResult<T1>> AddLibraryAsync(Guid avatarId, Guid parentId, int parentVersionSequence, IInstalledLibrary installedLibrary, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            string errorMessage = "Error occured in OAPPManagerBase.AddLibraryAsync. Reason:";

            try
            {
                OASISResult<T1> parentResult = await Data.LoadHolonByMetaDataAsync<T1>(new Dictionary<string, string>()
                {
                    { STARNETHolonIdName, parentId.ToString() },
                    { "VersionSequence", parentVersionSequence.ToString() }

                }, MetaKeyValuePairMatchMode.All, STARNETHolonType, providerType: providerType);

                if (parentResult != null && parentResult.Result != null && !parentResult.IsError)
                    return await AddLibraryAsync(avatarId, parentResult.Result, installedLibrary, providerType);
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the {STARNETHolonUIName}  with OAPPManagerBase.LoadHolonByMetaDataAsync. Reason: {parentResult.Message}");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured. Reason: {ex}");
            }

            return result;
        }

        public OASISResult<T1> AddLibrary(Guid avatarId, Guid parentId, int parentVersionSequence, IInstalledLibrary installedLibrary, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            string errorMessage = "Error occured in OAPPManagerBase.AddLibrary. Reason:";

            try
            {
                OASISResult<T1> parentResult = Data.LoadHolonByMetaData<T1>(new Dictionary<string, string>()
                {
                    { STARNETHolonIdName, parentId.ToString() },
                    { "VersionSequence", parentVersionSequence.ToString() }

                }, MetaKeyValuePairMatchMode.All, STARNETHolonType, providerType: providerType);

                if (parentResult != null && parentResult.Result != null && !parentResult.IsError)
                    return AddLibrary(avatarId, parentResult.Result, installedLibrary, providerType);
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the {STARNETHolonUIName}  with OAPPManagerBase.LoadHolonByMetaData. Reason: {parentResult.Message}");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured. Reason: {ex}");
            }

            return result;
        }

        public async Task<OASISResult<T1>> AddLibraryAsync(Guid avatarId, Guid parentId, int parentVersionSequence, Guid templateId, int libraryVersionSequence, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            string errorMessage = "Error occured in OAPPManagerBase.AddLibraryAsync. Reason:";

            OASISResult<InstalledLibrary> installedLibResult = await Data.LoadHolonByMetaDataAsync<InstalledLibrary>(new Dictionary<string, string>()
            {
                { "LibraryId", templateId.ToString() },
                { "VersionSequence", libraryVersionSequence.ToString() }

            }, MetaKeyValuePairMatchMode.All, HolonType.InstalledLibrary, providerType: providerType);

            if (installedLibResult != null && installedLibResult.Result != null && !installedLibResult.IsError)
                result = await AddLibraryAsync(avatarId, parentId, parentVersionSequence, (IInstalledLibrary)installedLibResult.Result, providerType);
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the Installed Library with Data.LoadHolonByMetaDataAsync. Reason: {installedLibResult.Message}");

            return result;
        }

        public OASISResult<T1> AddLibrary(Guid avatarId, Guid parentId, int parentVersionSequence, Guid templateId, int libraryVersionSequence, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            string errorMessage = "Error occured in OAPPManagerBase.AddLibrary. Reason:";

            OASISResult<InstalledLibrary> installedLibResult = Data.LoadHolonByMetaData<InstalledLibrary>(new Dictionary<string, string>()
            {
                { "LibraryId", templateId.ToString() },
                { "VersionSequence", libraryVersionSequence.ToString() }

            }, MetaKeyValuePairMatchMode.All, HolonType.InstalledLibrary, providerType: providerType);

            if (installedLibResult != null && installedLibResult.Result != null && !installedLibResult.IsError)
                result = AddLibrary(avatarId, parentId, parentVersionSequence, (IInstalledLibrary)installedLibResult.Result, providerType);
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the Installed Library with Data.LoadHolonByMetaData. Reason: {installedLibResult.Message}");

            return result;
        }

        public async Task<OASISResult<T1>> AddLibraryAsync(Guid avatarId, Guid parentId, string parentVersion, Guid templateId, string libraryVersion, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            string errorMessage = "Error occured in OAPPManagerBase.AddLibraryAsync. Reason:";

            OASISResult<InstalledLibrary> installedLibResult = await Data.LoadHolonByMetaDataAsync<InstalledLibrary>(new Dictionary<string, string>()
            {
                { "LibraryId", templateId.ToString() },
                { "Version", libraryVersion }

            }, MetaKeyValuePairMatchMode.All, HolonType.InstalledLibrary, providerType: providerType);

            if (installedLibResult != null && installedLibResult.Result != null && !installedLibResult.IsError)
                result = await AddLibraryAsync(avatarId, parentId, parentVersion, (IInstalledLibrary)installedLibResult.Result, providerType);
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the Installed Library with Data.LoadHolonByMetaDataAsync. Reason: {installedLibResult.Message}");

            return result;
        }

        public OASISResult<T1> AddLibrary(Guid avatarId, Guid parentId, string parentVersion, Guid templateId, string libraryVersion, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            string errorMessage = "Error occured in OAPPManagerBase.AddLibrary. Reason:";

            OASISResult<InstalledLibrary> installedLibResult = Data.LoadHolonByMetaData<InstalledLibrary>(new Dictionary<string, string>()
            {
                { "LibraryId", templateId.ToString() },
                { "Version", libraryVersion }

            }, MetaKeyValuePairMatchMode.All, HolonType.InstalledLibrary, providerType: providerType);

            if (installedLibResult != null && installedLibResult.Result != null && !installedLibResult.IsError)
                result = AddLibrary(avatarId, parentId, parentVersion, (IInstalledLibrary)installedLibResult.Result, providerType);
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the Installed Library with Data.LoadHolonByMetaData. Reason: {installedLibResult.Message}");

            return result;
        }


        public async Task<OASISResult<T1>> RemoveLibraryAsync(Guid avatarId, Guid parentId, int parentVersionSequence, Guid templateId, int libraryVersionSequence, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            string errorMessage = "Error occured in OAPPManagerBase.RemoveLibraryAsync. Reason:";

            try
            {
                OASISResult<T1> parentResult = await LoadAsync(avatarId, parentId, parentVersionSequence, providerType: providerType);

                if (parentResult != null && parentResult.Result != null && !parentResult.IsError)
                {
                    OASISResult<Library> libResult = await Data.LoadHolonByMetaDataAsync<Library>(new Dictionary<string, string>()
                    {
                        { "LibraryId", templateId.ToString() },
                        { "VersionSequence", libraryVersionSequence.ToString() }

                    }, MetaKeyValuePairMatchMode.All, HolonType.Library, providerType: providerType);

                    if (libResult != null && libResult.Result != null && !libResult.IsError)
                    {
                        parentResult.Result.Libraries.Remove((ILibrary)libResult.Result);
                        ISTARNETHolonMetaData metaData = parentResult.Result.LibrariesMetaData.FirstOrDefault(x => x.HolonId == libResult.Result.Id);

                        if (metaData != null)
                            parentResult.Result.LibrariesMetaData.Remove(metaData);

                        parentResult.Result.STARNETDNA.MetaData["Libraries"] = parentResult.Result.Libraries;
                        parentResult.Result.STARNETDNA.MetaData["LibrariesMetaData"] = parentResult.Result.LibrariesMetaData;
                        result = await UpdateAsync(avatarId, parentResult.Result, result, errorMessage, providerType);

                        string path = Path.Combine(parentResult.Result.STARNETDNA.SourcePath, "Dependencies", "STARNET", "Templates", string.Concat(libResult.Result.STARNETDNA.Name, "_v", libResult.Result.STARNETDNA.Version));

                        if (Directory.Exists(path))
                            Directory.Exists(path);
                    }
                    else
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the Library with Data.LoadHolonByMetaDataAsync. Reason: {libResult.Message}");
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the {STARNETHolonUIName}  with OAPPManagerBase.LoadHolonByMetaDataAsync. Reason: {parentResult.Message}");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured. Reason: {ex}");
            }

            return result;
        }

        public OASISResult<T1> RemoveLibrary(Guid avatarId, Guid parentId, int parentVersionSequence, Guid templateId, int libraryVersionSequnce, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            string errorMessage = "Error occured in OAPPManagerBase.RemoveLibraryAsync. Reason:";

            try
            {
                OASISResult<T1> parentResult = Load(avatarId, parentId, parentVersionSequence, providerType: providerType);

                if (parentResult != null && parentResult.Result != null && !parentResult.IsError)
                {
                    OASISResult<Library> libResult = Data.LoadHolonByMetaData<Library>(new Dictionary<string, string>()
                    {
                        { "LibraryId", templateId.ToString() },
                        { "VersionSequence", libraryVersionSequnce.ToString() }

                    }, MetaKeyValuePairMatchMode.All, HolonType.Library, providerType: providerType);

                    if (libResult != null && libResult.Result != null && !libResult.IsError)
                    {
                        parentResult.Result.Libraries.Remove((ILibrary)libResult.Result);
                        ISTARNETHolonMetaData metaData = parentResult.Result.LibrariesMetaData.FirstOrDefault(x => x.HolonId == libResult.Result.Id);

                        if (metaData != null)
                            parentResult.Result.LibrariesMetaData.Remove(metaData);

                        parentResult.Result.STARNETDNA.MetaData["Libraries"] = parentResult.Result.Libraries;
                        parentResult.Result.STARNETDNA.MetaData["LibrariesMetaData"] = parentResult.Result.LibrariesMetaData;
                        result = Update(avatarId, parentResult.Result, result, errorMessage, providerType);

                        string path = Path.Combine(parentResult.Result.STARNETDNA.SourcePath, "Dependencies", "STARNET", "Templates", string.Concat(libResult.Result.STARNETDNA.Name, "_v", libResult.Result.STARNETDNA.Version));

                        if (Directory.Exists(path))
                            Directory.Exists(path);
                    }
                    else
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the Library with Data.LoadHolonByMetaData. Reason: {libResult.Message}");
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the {STARNETHolonUIName}  with OAPPManagerBase.LoadHolonByMetaData. Reason: {parentResult.Message}");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured. Reason: {ex}");
            }

            return result;
        }

        public async Task<OASISResult<T1>> RemoveLibraryAsync(Guid avatarId, Guid parentId, string parentVersion, Guid templateId, string libraryVersion, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            string errorMessage = "Error occured in OAPPManagerBase.RemoveLibraryAsync. Reason:";

            try
            {
                OASISResult<T1> parentResult = await Data.LoadHolonByMetaDataAsync<T1>(new Dictionary<string, string>()
                {
                    { STARNETHolonIdName, parentId.ToString() },
                    { "Version", parentVersion }

                }, MetaKeyValuePairMatchMode.All, STARNETHolonType, providerType: providerType);

                if (parentResult != null && parentResult.Result != null && !parentResult.IsError)
                {
                    OASISResult<Library> libResult = await Data.LoadHolonByMetaDataAsync<Library>(new Dictionary<string, string>()
                    {
                        { "LibraryId", templateId.ToString() },
                        { "Version", libraryVersion }

                    }, MetaKeyValuePairMatchMode.All, HolonType.Library, providerType: providerType);

                    if (libResult != null && libResult.Result != null && !libResult.IsError)
                    {
                        parentResult.Result.Libraries.Remove((ILibrary)libResult.Result);
                        ISTARNETHolonMetaData metaData = parentResult.Result.LibrariesMetaData.FirstOrDefault(x => x.HolonId == libResult.Result.Id);

                        if (metaData != null)
                            parentResult.Result.LibrariesMetaData.Remove(metaData);

                        parentResult.Result.STARNETDNA.MetaData["Libraries"] = parentResult.Result.Libraries;
                        parentResult.Result.STARNETDNA.MetaData["LibrariesMetaData"] = parentResult.Result.LibrariesMetaData;
                        result = await UpdateAsync(avatarId, parentResult.Result, result, errorMessage, providerType);

                        string path = Path.Combine(parentResult.Result.STARNETDNA.SourcePath, "Dependencies", "STARNET", "Templates", string.Concat(libResult.Result.STARNETDNA.Name, "_v", libResult.Result.STARNETDNA.Version));

                        if (Directory.Exists(path))
                            Directory.Exists(path);
                    }
                    else
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the Library with Data.LoadHolonByMetaDataAsync. Reason: {libResult.Message}");
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the {STARNETHolonUIName}  with OAPPManagerBase.LoadHolonByMetaDataAsync. Reason: {parentResult.Message}");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured. Reason: {ex}");
            }

            return result;
        }

        public OASISResult<T1> RemoveLibrary(Guid avatarId, Guid parentId, string parentVersion, Guid templateId, string libraryVersion, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            string errorMessage = "Error occured in OAPPManagerBase.RemoveLibraryAsync. Reason:";

            try
            {
                OASISResult<T1> parentResult = Data.LoadHolonByMetaData<T1>(new Dictionary<string, string>()
                {
                    { STARNETHolonIdName, templateId.ToString() },
                    { "Version", parentVersion }

                }, MetaKeyValuePairMatchMode.All, STARNETHolonType, providerType: providerType);

                if (parentResult != null && parentResult.Result != null && !parentResult.IsError)
                {
                    OASISResult<Library> libResult = Data.LoadHolonByMetaData<Library>(new Dictionary<string, string>()
                    {
                        { "LibraryId", templateId.ToString() },
                        { "Version", libraryVersion.ToString() }

                    }, MetaKeyValuePairMatchMode.All, HolonType.Library, providerType: providerType);

                    if (libResult != null && libResult.Result != null && !libResult.IsError)
                    {
                        parentResult.Result.Libraries.Remove((ILibrary)libResult.Result);
                        ISTARNETHolonMetaData metaData = parentResult.Result.LibrariesMetaData.FirstOrDefault(x => x.HolonId == libResult.Result.Id);

                        if (metaData != null)
                            parentResult.Result.LibrariesMetaData.Remove(metaData);

                        parentResult.Result.STARNETDNA.MetaData["Libraries"] = parentResult.Result.Libraries;
                        parentResult.Result.STARNETDNA.MetaData["LibrariesMetaData"] = parentResult.Result.LibrariesMetaData;
                        result = Update(avatarId, parentResult.Result, result, errorMessage, providerType);

                        string path = Path.Combine(parentResult.Result.STARNETDNA.SourcePath, "Dependencies", "STARNET", "Templates", string.Concat(libResult.Result.STARNETDNA.Name, "_v", libResult.Result.STARNETDNA.Version));

                        if (Directory.Exists(path))
                            Directory.Exists(path);
                    }
                    else
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the Library with Data.LoadHolonByMetaData. Reason: {libResult.Message}");
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the {STARNETHolonUIName} with OAPPManagerBase.LoadHolonByMetaData. Reason: {parentResult.Message}");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured. Reason: {ex}");
            }

            return result;
        }



        public async Task<OASISResult<T1>> AddOAPPTemplateAsync(Guid avatarId, T1 parent, IInstalledOAPPTemplate installedOAPPTemplate, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            string errorMessage = "Error occured in OAPPManagerBase.AddOAPPTemplateAsync. Reason:";

            try
            {
                OASISResult<OAPPTemplate> libResult = await Data.LoadHolonByMetaDataAsync<OAPPTemplate>(new Dictionary<string, string>()
                {
                    { "OAPPTemplateId", installedOAPPTemplate.STARNETDNA.Id.ToString() },
                    { "Version", installedOAPPTemplate.STARNETDNA.Version }

                }, MetaKeyValuePairMatchMode.All, HolonType.OAPPTemplate, providerType: providerType);

                if (libResult != null && libResult.Result != null && !libResult.IsError)
                {
                    parent.OAPPTemplates.Add(libResult.Result);
                    parent.OAPPTemplatesMetaData.Add(new STARNETHolonMetaData()
                    {
                        HolonId = libResult.Result.Id,
                        STARNETHolonId = libResult.Result.STARNETDNA.Id,
                        Name = libResult.Result.Name,
                        VersionSequence = libResult.Result.STARNETDNA.VersionSequence,
                        Version = libResult.Result.STARNETDNA.Version
                    });

                    parent.STARNETDNA.MetaData["OAPPTemplates"] = parent.OAPPTemplates;
                    parent.STARNETDNA.MetaData["OAPPTemplatesMetaData"] = parent.OAPPTemplatesMetaData;
                    result = Update(avatarId, parent, result, errorMessage, providerType);

                    if (result != null && result.Result != null && !result.IsError)
                        DirectoryHelper.CopyFilesRecursively(installedOAPPTemplate.InstalledPath, Path.Combine(parent.STARNETDNA.SourcePath, "Dependencies", "STARNET", "Templates"));
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the OAPP Template with Data.LoadHolonByMetaDataAsync. Reason: {libResult.Message}");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured. Reason: {ex}");
            }

            return result;
        }

        public OASISResult<T1> AddOAPPTemplate(Guid avatarId, T1 parent, IInstalledOAPPTemplate installedOAPPTemplate, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            string errorMessage = "Error occured in OAPPManagerBase.AddOAPPTemplate. Reason:";

            try
            {
                OASISResult<OAPPTemplate> libResult = Data.LoadHolonByMetaData<OAPPTemplate>(new Dictionary<string, string>()
                {
                    { "OAPPTemplateId", installedOAPPTemplate.STARNETDNA.Id.ToString() },
                    { "Version", installedOAPPTemplate.STARNETDNA.Version }

                }, MetaKeyValuePairMatchMode.All, HolonType.OAPPTemplate, providerType: providerType);

                if (libResult != null && libResult.Result != null && !libResult.IsError)
                {
                    parent.OAPPTemplates.Add(libResult.Result);
                    parent.OAPPTemplatesMetaData.Add(new STARNETHolonMetaData()
                    {
                        HolonId = libResult.Result.Id,
                        STARNETHolonId = libResult.Result.STARNETDNA.Id,
                        Name = libResult.Result.Name,
                        VersionSequence = libResult.Result.STARNETDNA.VersionSequence,
                        Version = libResult.Result.STARNETDNA.Version
                    });

                    parent.STARNETDNA.MetaData["OAPPTemplates"] = parent.OAPPTemplates;
                    parent.STARNETDNA.MetaData["OAPPTemplatesMetaData"] = parent.OAPPTemplatesMetaData;
                    result = Update(avatarId, parent, result, errorMessage, providerType);

                    if (result != null && result.Result != null && !result.IsError)
                        DirectoryHelper.CopyFilesRecursively(installedOAPPTemplate.InstalledPath, Path.Combine(parent.STARNETDNA.SourcePath, "Dependencies", "STARNET", "Templates"));
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the OAPP Template with Data.LoadHolonByMetaData. Reason: {libResult.Message}");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured. Reason: {ex}");
            }

            return result;
        }

        public async Task<OASISResult<T1>> AddOAPPTemplateAsync(Guid avatarId, Guid parentId, string parentVersion, IInstalledOAPPTemplate installedOAPPTemplate, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            string errorMessage = "Error occured in OAPPManagerBase.AddOAPPTemplateAsync. Reason:";

            try
            {
                OASISResult<T1> parentResult = await Data.LoadHolonByMetaDataAsync<T1>(new Dictionary<string, string>()
                {
                    { STARNETHolonIdName, parentId.ToString() },
                    { "Version", parentVersion }

                }, MetaKeyValuePairMatchMode.All, STARNETHolonType, providerType: providerType);

                if (parentResult != null && parentResult.Result != null && !parentResult.IsError)
                    return await AddOAPPTemplateAsync(avatarId, parentResult.Result, installedOAPPTemplate, providerType);
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the {STARNETHolonUIName}  with OAPPManagerBase.LoadHolonByMetaDataAsync. Reason: {parentResult.Message}");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured. Reason: {ex}");
            }

            return result;
        }

        public OASISResult<T1> AddOAPPTemplate(Guid avatarId, Guid parentId, string parentVersion, IInstalledOAPPTemplate installedOAPPTemplate, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            string errorMessage = "Error occured in OAPPManagerBase.AddOAPPTemplate. Reason:";

            try
            {
                OASISResult<T1> parentResult = Data.LoadHolonByMetaData<T1>(new Dictionary<string, string>()
                {
                    { STARNETHolonIdName, parentId.ToString() },
                    { "Version", parentVersion }

                }, MetaKeyValuePairMatchMode.All, STARNETHolonType, providerType: providerType);

                if (parentResult != null && parentResult.Result != null && !parentResult.IsError)
                    return AddOAPPTemplate(avatarId, parentResult.Result, installedOAPPTemplate, providerType);
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the {STARNETHolonUIName}  with OAPPManagerBase.LoadHolonByMetaData. Reason: {parentResult.Message}");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured. Reason: {ex}");
            }

            return result;
        }

        public async Task<OASISResult<T1>> AddOAPPTemplateAsync(Guid avatarId, Guid parentId, int parentVersionSequence, IInstalledOAPPTemplate installedOAPPTemplate, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            string errorMessage = "Error occured in OAPPManagerBase.AddOAPPTemplateAsync. Reason:";

            try
            {
                OASISResult<T1> parentResult = await Data.LoadHolonByMetaDataAsync<T1>(new Dictionary<string, string>()
                {
                    { STARNETHolonIdName, parentId.ToString() },
                    { "VersionSequence", parentVersionSequence.ToString() }

                }, MetaKeyValuePairMatchMode.All, STARNETHolonType, providerType: providerType);

                if (parentResult != null && parentResult.Result != null && !parentResult.IsError)
                    return await AddOAPPTemplateAsync(avatarId, parentResult.Result, installedOAPPTemplate, providerType);
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the {STARNETHolonUIName}  with OAPPManagerBase.LoadHolonByMetaDataAsync. Reason: {parentResult.Message}");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured. Reason: {ex}");
            }

            return result;
        }

        public OASISResult<T1> AddOAPPTemplate(Guid avatarId, Guid parentId, int parentVersionSequence, IInstalledOAPPTemplate installedOAPPTemplate, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            string errorMessage = "Error occured in OAPPManagerBase.AddOAPPTemplate. Reason:";

            try
            {
                OASISResult<T1> parentResult = Data.LoadHolonByMetaData<T1>(new Dictionary<string, string>()
                {
                    { STARNETHolonIdName, parentId.ToString() },
                    { "VersionSequence", parentVersionSequence.ToString() }

                }, MetaKeyValuePairMatchMode.All, STARNETHolonType, providerType: providerType);

                if (parentResult != null && parentResult.Result != null && !parentResult.IsError)
                    return AddOAPPTemplate(avatarId, parentResult.Result, installedOAPPTemplate, providerType);
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the {STARNETHolonUIName}  with OAPPManagerBase.LoadHolonByMetaData. Reason: {parentResult.Message}");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured. Reason: {ex}");
            }

            return result;
        }

        public async Task<OASISResult<T1>> AddOAPPTemplateAsync(Guid avatarId, Guid parentId, int parentVersionSequence, Guid templateId, int subTemplateVersionSequence, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            string errorMessage = "Error occured in OAPPManagerBase.AddOAPPTemplateAsync. Reason:";
           
            OASISResult<InstalledOAPPTemplate> installedTemplateResult = await Data.LoadHolonByMetaDataAsync<InstalledOAPPTemplate>(new Dictionary<string, string>()
            {
                { "OAPPTemplateId", templateId.ToString() },
                { "VersionSequence", subTemplateVersionSequence.ToString() }

            }, MetaKeyValuePairMatchMode.All, HolonType.InstalledOAPPTemplate, providerType: providerType);

            if (installedTemplateResult != null && installedTemplateResult.Result != null && !installedTemplateResult.IsError)
                result = await AddOAPPTemplateAsync(avatarId, parentId, parentVersionSequence, installedTemplateResult.Result, providerType);
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the Installed OAPP Template with Data.LoadHolonByMetaDataAsync. Reason: {installedTemplateResult.Message}");
                    
            return result;
        }

        public OASISResult<T1> AddOAPPTemplate(Guid avatarId, Guid parentId, int parentVersionSequence, Guid templateId, int subTemplateVersionSequence, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            string errorMessage = "Error occured in OAPPManagerBase.AddOAPPTemplate. Reason:";

            OASISResult<InstalledOAPPTemplate> installedTemplateResult = Data.LoadHolonByMetaData<InstalledOAPPTemplate>(new Dictionary<string, string>()
            {
                { "OAPPTemplateId", templateId.ToString() },
                { "VersionSequence", subTemplateVersionSequence.ToString() }

            }, MetaKeyValuePairMatchMode.All, HolonType.InstalledOAPPTemplate, providerType: providerType);

            if (installedTemplateResult != null && installedTemplateResult.Result != null && !installedTemplateResult.IsError)
                result = AddOAPPTemplate(avatarId, parentId, parentVersionSequence, installedTemplateResult.Result, providerType);
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the Installed OAPP Template with Data.LoadHolonByMetaData. Reason: {installedTemplateResult.Message}");

            return result;
        }

        public async Task<OASISResult<T1>> AddOAPPTemplateAsync(Guid avatarId, Guid parentId, string parentVersion, Guid templateId, string subTemplateVersion, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            string errorMessage = "Error occured in OAPPManagerBase.AddOAPPTemplateAsync. Reason:";

            OASISResult<InstalledOAPPTemplate> installedTemplateResult = await Data.LoadHolonByMetaDataAsync<InstalledOAPPTemplate>(new Dictionary<string, string>()
            {
                { "OAPPTemplateId", templateId.ToString() },
                { "Version", subTemplateVersion }

            }, MetaKeyValuePairMatchMode.All, HolonType.InstalledOAPPTemplate, providerType: providerType);

            if (installedTemplateResult != null && installedTemplateResult.Result != null && !installedTemplateResult.IsError)
                result = await AddOAPPTemplateAsync(avatarId, parentId, parentVersion, installedTemplateResult.Result, providerType);
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the Installed OAPP Template with Data.LoadHolonByMetaDataAsync. Reason: {installedTemplateResult.Message}");

            return result;
        }

        public OASISResult<T1> AddOAPPTemplate(Guid avatarId, Guid parentId, string parentVersion, Guid templateId, string subTemplateVersion, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            string errorMessage = "Error occured in OAPPManagerBase.AddOAPPTemplate. Reason:";

            OASISResult<InstalledOAPPTemplate> installedTemplateResult = Data.LoadHolonByMetaData<InstalledOAPPTemplate>(new Dictionary<string, string>()
            {
                { "OAPPTemplateId", templateId.ToString() },
                { "Version", subTemplateVersion }

            }, MetaKeyValuePairMatchMode.All, HolonType.InstalledOAPPTemplate, providerType: providerType);

            if (installedTemplateResult != null && installedTemplateResult.Result != null && !installedTemplateResult.IsError)
                result = AddOAPPTemplate(avatarId, parentId, parentVersion, installedTemplateResult.Result, providerType);
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the Installed OAPP Template with Data.LoadHolonByMetaData. Reason: {installedTemplateResult.Message}");

            return result;
        }


        public async Task<OASISResult<T1>> RemoveOAPPTemplateAsync(Guid avatarId, Guid parentId, int parentVersionSequence, Guid templateId, int subTemplateVersionSequence, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            string errorMessage = "Error occured in OAPPManagerBase.RemoveOAPPTemplateAsync. Reason:";

            try
            {
                OASISResult<T1> parentResult = await LoadAsync(avatarId, parentId, parentVersionSequence, providerType: providerType);

                if (parentResult != null && parentResult.Result != null && !parentResult.IsError)
                {
                    OASISResult<OAPPTemplate> libResult = await Data.LoadHolonByMetaDataAsync<OAPPTemplate>(new Dictionary<string, string>()
                    {
                        { "OAPPTemplateId", templateId.ToString() },
                        { "VersionSequence", subTemplateVersionSequence.ToString() }

                    }, MetaKeyValuePairMatchMode.All, HolonType.OAPPTemplate, providerType: providerType);

                    if (libResult != null && libResult.Result != null && !libResult.IsError)
                    {
                        parentResult.Result.OAPPTemplates.Remove((IOAPPTemplate)libResult.Result);
                        ISTARNETHolonMetaData metaData = parentResult.Result.OAPPTemplatesMetaData.FirstOrDefault(x => x.HolonId == libResult.Result.Id);

                        if (metaData != null)
                            parentResult.Result.OAPPTemplatesMetaData.Remove(metaData);

                        parentResult.Result.STARNETDNA.MetaData["OAPPTemplates"] = parentResult.Result.OAPPTemplates;
                        parentResult.Result.STARNETDNA.MetaData["OAPPTemplatesMetaData"] = parentResult.Result.OAPPTemplatesMetaData;
                        result = await UpdateAsync(avatarId, parentResult.Result, result, errorMessage, providerType);

                        string path = Path.Combine(parentResult.Result.STARNETDNA.SourcePath, "Dependencies", "STARNET", "Templates", string.Concat(libResult.Result.STARNETDNA.Name, "_v", libResult.Result.STARNETDNA.Version));

                        if (Directory.Exists(path))
                            Directory.Exists(path);
                    }
                    else
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the OAPP Template with Data.LoadHolonByMetaDataAsync. Reason: {libResult.Message}");
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the {STARNETHolonUIName}  with OAPPManagerBase.LoadHolonByMetaDataAsync. Reason: {parentResult.Message}");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured. Reason: {ex}");
            }

            return result;
        }

        public OASISResult<T1> RemoveOAPPTemplate(Guid avatarId, Guid parentId, int parentVersionSequence, Guid templateId, int subTemplateVersionSequnce, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            string errorMessage = "Error occured in OAPPManagerBase.RemoveOAPPTemplateAsync. Reason:";

            try
            {
                OASISResult<T1> parentResult = Load(avatarId, parentId, parentVersionSequence, providerType: providerType);

                if (parentResult != null && parentResult.Result != null && !parentResult.IsError)
                {
                    OASISResult<OAPPTemplate> libResult = Data.LoadHolonByMetaData<OAPPTemplate>(new Dictionary<string, string>()
                    {
                        { "OAPPTemplateId", templateId.ToString() },
                        { "VersionSequence", subTemplateVersionSequnce.ToString() }

                    }, MetaKeyValuePairMatchMode.All, HolonType.OAPPTemplate, providerType: providerType);

                    if (libResult != null && libResult.Result != null && !libResult.IsError)
                    {
                        parentResult.Result.OAPPTemplates.Remove((IOAPPTemplate)libResult.Result);
                        ISTARNETHolonMetaData metaData = parentResult.Result.OAPPTemplatesMetaData.FirstOrDefault(x => x.HolonId == libResult.Result.Id);

                        if (metaData != null)
                            parentResult.Result.OAPPTemplatesMetaData.Remove(metaData);

                        parentResult.Result.STARNETDNA.MetaData["OAPPTemplates"] = parentResult.Result.OAPPTemplates;
                        parentResult.Result.STARNETDNA.MetaData["OAPPTemplatesMetaData"] = parentResult.Result.OAPPTemplatesMetaData;
                        result = Update(avatarId, parentResult.Result, result, errorMessage, providerType);

                        string path = Path.Combine(parentResult.Result.STARNETDNA.SourcePath, "Dependencies", "STARNET", "Templates", string.Concat(libResult.Result.STARNETDNA.Name, "_v", libResult.Result.STARNETDNA.Version));

                        if (Directory.Exists(path))
                            Directory.Exists(path);
                    }
                    else
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the OAPP Template with Data.LoadHolonByMetaData. Reason: {libResult.Message}");
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the {STARNETHolonUIName}  with OAPPManagerBase.LoadHolonByMetaData. Reason: {parentResult.Message}");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured. Reason: {ex}");
            }

            return result;
        }

        public async Task<OASISResult<T1>> RemoveOAPPTemplateAsync(Guid avatarId, Guid parentId, string parentVersion, Guid templateId, string subTemplateVersion, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            string errorMessage = "Error occured in OAPPManagerBase.RemoveOAPPTemplateAsync. Reason:";

            try
            {
                OASISResult<T1> parentResult = await Data.LoadHolonByMetaDataAsync<T1>(new Dictionary<string, string>()
                {
                    { STARNETHolonIdName, parentId.ToString() },
                    { "Version", parentVersion }

                }, MetaKeyValuePairMatchMode.All, STARNETHolonType, providerType: providerType);

                if (parentResult != null && parentResult.Result != null && !parentResult.IsError)
                {
                    OASISResult<OAPPTemplate> libResult = await Data.LoadHolonByMetaDataAsync<OAPPTemplate>(new Dictionary<string, string>()
                    {
                        { "OAPPTemplateId", templateId.ToString() },
                        { "Version", subTemplateVersion }

                    }, MetaKeyValuePairMatchMode.All, HolonType.OAPPTemplate, providerType: providerType);

                    if (libResult != null && libResult.Result != null && !libResult.IsError)
                    {
                        parentResult.Result.OAPPTemplates.Remove((IOAPPTemplate)libResult.Result);
                        ISTARNETHolonMetaData metaData = parentResult.Result.OAPPTemplatesMetaData.FirstOrDefault(x => x.HolonId == libResult.Result.Id);

                        if (metaData != null)
                            parentResult.Result.OAPPTemplatesMetaData.Remove(metaData);

                        parentResult.Result.STARNETDNA.MetaData["OAPPTemplates"] = parentResult.Result.OAPPTemplates;
                        parentResult.Result.STARNETDNA.MetaData["OAPPTemplatesMetaData"] = parentResult.Result.OAPPTemplatesMetaData;
                        result = await UpdateAsync(avatarId, parentResult.Result, result, errorMessage, providerType);

                        string path = Path.Combine(parentResult.Result.STARNETDNA.SourcePath, "Dependencies", "STARNET", "Templates", string.Concat(libResult.Result.STARNETDNA.Name, "_v", libResult.Result.STARNETDNA.Version));

                        if (Directory.Exists(path))
                            Directory.Exists(path);
                    }
                    else
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the OAPP Template with Data.LoadHolonByMetaDataAsync. Reason: {libResult.Message}");
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the {STARNETHolonUIName}  with OAPPManagerBase.LoadHolonByMetaDataAsync. Reason: {parentResult.Message}");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured. Reason: {ex}");
            }

            return result;
        }

        public OASISResult<T1> RemoveOAPPTemplate(Guid avatarId, Guid parentId, string parentVersion, Guid templateId, string subTemplateVersion, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            string errorMessage = "Error occured in OAPPManagerBase.RemoveOAPPTemplateAsync. Reason:";

            try
            {
                OASISResult<T1> parentResult = Data.LoadHolonByMetaData<T1>(new Dictionary<string, string>()
                {
                    { STARNETHolonIdName, templateId.ToString() },
                    { "Version", parentVersion }

                }, MetaKeyValuePairMatchMode.All, STARNETHolonType, providerType: providerType);

                if (parentResult != null && parentResult.Result != null && !parentResult.IsError)
                {
                    OASISResult<OAPPTemplate> libResult = Data.LoadHolonByMetaData<OAPPTemplate>(new Dictionary<string, string>()
                    {
                        { "OAPPTemplateId", templateId.ToString() },
                        { "Version", subTemplateVersion.ToString() }

                    }, MetaKeyValuePairMatchMode.All, HolonType.OAPPTemplate, providerType: providerType);

                    if (libResult != null && libResult.Result != null && !libResult.IsError)
                    {
                        parentResult.Result.OAPPTemplates.Remove((IOAPPTemplate)libResult.Result);
                        ISTARNETHolonMetaData metaData = parentResult.Result.OAPPTemplatesMetaData.FirstOrDefault(x => x.HolonId == libResult.Result.Id);

                        if (metaData != null)
                            parentResult.Result.OAPPTemplatesMetaData.Remove(metaData);

                        parentResult.Result.STARNETDNA.MetaData["OAPPTemplates"] = parentResult.Result.OAPPTemplates;
                        parentResult.Result.STARNETDNA.MetaData["OAPPTemplatesMetaData"] = parentResult.Result.OAPPTemplatesMetaData;
                        result = Update(avatarId, parentResult.Result, result, errorMessage, providerType);

                        string path = Path.Combine(parentResult.Result.STARNETDNA.SourcePath, "Dependencies", "STARNET", "Templates", string.Concat(libResult.Result.STARNETDNA.Name, "_v", libResult.Result.STARNETDNA.Version));

                        if (Directory.Exists(path))
                            Directory.Exists(path);
                    }
                    else
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the OAPP Template with Data.LoadHolonByMetaData. Reason: {libResult.Message}");
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the {STARNETHolonUIName} with OAPPManagerBase.LoadHolonByMetaData. Reason: {parentResult.Message}");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured. Reason: {ex}");
            }

            return result;
        }










        //public async Task<OASISResult<T1>> AddRuntimeToT1Async(Guid avatarId, Guid parentId, Guid runtimeId, ProviderType providerType = ProviderType.Default)
        //{
        //    OASISResult<T1> result = new OASISResult<T1>();
        //    string errorMessage = "Error occured in OAPPManagerBase.AddRuntimeToT1Async. Reason:";

        //    try
        //    {
        //        OASISResult<T1> parentResult = await LoadAsync(avatarId, parentId, providerType: providerType);

        //        if (parentResult != null && parentResult.Result != null && !parentResult.IsError)
        //        {
        //            OASISResult<Runtime> runtimeResult = await Data.LoadHolonAsync<Runtime>(runtimeId, true, true, 0, true, false, HolonType.All, 0, providerType);

        //            if (runtimeResult != null && runtimeResult.Result != null && !runtimeResult.IsError)
        //            {
        //                parentResult.Result.Runtimes.Add(runtimeResult.Result);
        //                parentResult.Result.RuntimeIds.Add(runtimeResult.Result.Id.ToString());
        //                parentResult.Result.STARNETDNA.MetaData["RuntimeIds"] = parentResult.Result.RuntimeIds;
        //                result = await UpdateAsync(avatarId, parentResult.Result, result, errorMessage, providerType);
        //            }
        //            else
        //                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the runtime with Data.LoadHolonAsync. Reason: {runtimeResult.Message}");
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

        //public OASISResult<T1> AddRuntimeToT1(Guid avatarId, Guid parentId, Guid runtimeId, ProviderType providerType = ProviderType.Default)
        //{
        //    OASISResult<T1> result = new OASISResult<T1>();
        //    string errorMessage = "Error occured in OAPPManagerBase.AddRuntimeToT1. Reason:";

        //    try
        //    {
        //        OASISResult<T1> parentResult = Load(avatarId, parentId, providerType: providerType);

        //        if (parentResult != null && parentResult.Result != null && !parentResult.IsError)
        //        {
        //            OASISResult<Runtime> runtimeResult = Data.LoadHolon<Runtime>(runtimeId, true, true, 0, true, false, HolonType.All, 0, providerType);

        //            if (runtimeResult != null && runtimeResult.Result != null && !runtimeResult.IsError)
        //            {
        //                parentResult.Result.Runtimes.Add(runtimeResult.Result);
        //                parentResult.Result.RuntimeIds.Add(runtimeResult.Result.Id.ToString());
        //                parentResult.Result.STARNETDNA.MetaData["RuntimeIds"] = parentResult.Result.RuntimeIds;
        //                result = Update(avatarId, parentResult.Result, result, errorMessage, providerType);
        //            }
        //            else
        //                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the geo-hotspot with Data.LoadHolon. Reason: {runtimeResult.Message}");
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

        //public async Task<OASISResult<T1>> RemoveRuntimeFromT1Async(Guid avatarId, Guid parentId, Guid runtimeId, ProviderType providerType = ProviderType.Default)
        //{
        //    OASISResult<T1> result = new OASISResult<T1>();
        //    string errorMessage = "Error occured in OAPPManagerBase.RemoveRuntimeFromT1Async. Reason:";

        //    try
        //    {
        //        OASISResult<T1> parentResult = await LoadAsync(avatarId, parentId, providerType: providerType);

        //        if (parentResult != null && parentResult.Result != null && !parentResult.IsError)
        //        {
        //            IRuntime runtime = parentResult.Result.Runtimes.FirstOrDefault(x => x.Id == runtimeId);

        //            if (runtime != null)
        //            {
        //                parentResult.Result.Runtimes.Remove(runtime);
        //                parentResult.Result.RuntimeIds.Remove(runtime.ToString());
        //                parentResult.Result.STARNETDNA.MetaData["RuntimeIds"] = parentResult.Result.RuntimeIds;
        //                result = await UpdateAsync(avatarId, parentResult.Result, result, errorMessage, providerType);
        //            }
        //            else
        //                OASISErrorHandling.HandleError(ref result, $"{errorMessage} No Runtime could be found for the id {runtimeId}");
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

        //public OASISResult<T1> RemoveRuntimeFromT1(Guid avatarId, Guid parentId, Guid runtimeId, ProviderType providerType = ProviderType.Default)
        //{
        //    OASISResult<T1> result = new OASISResult<T1>();
        //    string errorMessage = "Error occured in OAPPManagerBase.RemoveRuntimeFromT1. Reason:";

        //    try
        //    {
        //        OASISResult<T1> parentResult = Load(avatarId, parentId, providerType: providerType);

        //        if (parentResult != null && parentResult.Result != null && !parentResult.IsError)
        //        {
        //            IRuntime runtime = parentResult.Result.Runtimes.FirstOrDefault(x => x.Id == runtimeId);

        //            if (runtime != null)
        //            {
        //                parentResult.Result.Runtimes.Remove(runtime);
        //                parentResult.Result.RuntimeIds.Remove(runtime.ToString());
        //                parentResult.Result.STARNETDNA.MetaData["RuntimeIds"] = parentResult.Result.RuntimeIds;
        //                result = Update(avatarId, parentResult.Result, result, errorMessage, providerType);
        //            }
        //            else
        //                OASISErrorHandling.HandleError(ref result, $"{errorMessage} No Runtime could be found for the id {runtimeId}");
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

        //public async Task<OASISResult<T1>> AddLibraryToT1Async(Guid avatarId, Guid parentId, Guid libraryId, ProviderType providerType = ProviderType.Default)
        //{
        //    OASISResult<T1> result = new OASISResult<T1>();
        //    string errorMessage = "Error occured in OAPPManagerBase.AddLibraryToT1Async. Reason:";

        //    try
        //    {
        //        OASISResult<T1> parentResult = await LoadAsync(avatarId, parentId, providerType: providerType);

        //        if (parentResult != null && parentResult.Result != null && !parentResult.IsError)
        //        {
        //            OASISResult<Library> libraryResult = await Data.LoadHolonAsync<Library>(libraryId, true, true, 0, true, false, HolonType.All, 0, providerType);

        //            if (libraryResult != null && libraryResult.Result != null && !libraryResult.IsError)
        //            {
        //                parentResult.Result.Libraries.Add(libraryResult.Result);
        //                parentResult.Result.LibraryIds.Add(libraryResult.Result.Id.ToString());
        //                parentResult.Result.STARNETDNA.MetaData["LibraryIds"] = parentResult.Result.LibraryIds;
        //                result = await UpdateAsync(avatarId, parentResult.Result, result, errorMessage, providerType);
        //            }
        //            else
        //                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the library with Data.LoadHolonAsync. Reason: {libraryResult.Message}");
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

        //public OASISResult<T1> AddLibraryToT1(Guid avatarId, Guid parentId, Guid libraryId, ProviderType providerType = ProviderType.Default)
        //{
        //    OASISResult<T1> result = new OASISResult<T1>();
        //    string errorMessage = "Error occured in OAPPManagerBase.AddLibraryToT1. Reason:";

        //    try
        //    {
        //        OASISResult<T1> parentResult = Load(avatarId, parentId, providerType: providerType);

        //        if (parentResult != null && parentResult.Result != null && !parentResult.IsError)
        //        {
        //            OASISResult<Library> libraryResult = Data.LoadHolon<Library>(libraryId, true, true, 0, true, false, HolonType.All, 0, providerType);

        //            if (libraryResult != null && libraryResult.Result != null && !libraryResult.IsError)
        //            {
        //                parentResult.Result.Libraries.Add(libraryResult.Result);
        //                parentResult.Result.LibraryIds.Add(libraryResult.Result.Id.ToString());
        //                parentResult.Result.STARNETDNA.MetaData["LibraryIds"] = parentResult.Result.LibraryIds;
        //                result = Update(avatarId, parentResult.Result, result, errorMessage, providerType);
        //            }
        //            else
        //                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the geo-hotspot with Data.LoadHolon. Reason: {libraryResult.Message}");
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

        //public async Task<OASISResult<T1>> RemoveLibraryFromT1Async(Guid avatarId, Guid parentId, Guid libraryId, ProviderType providerType = ProviderType.Default)
        //{
        //    OASISResult<T1> result = new OASISResult<T1>();
        //    string errorMessage = "Error occured in OAPPManagerBase.RemoveLibraryFromT1Async. Reason:";

        //    try
        //    {
        //        OASISResult<T1> parentResult = await LoadAsync(avatarId, parentId, providerType: providerType);

        //        if (parentResult != null && parentResult.Result != null && !parentResult.IsError)
        //        {
        //            ILibrary library = parentResult.Result.Libraries.FirstOrDefault(x => x.Id == libraryId);

        //            if (library != null)
        //            {
        //                parentResult.Result.Libraries.Remove(library);
        //                parentResult.Result.LibraryIds.Remove(library.ToString());
        //                parentResult.Result.STARNETDNA.MetaData["LibraryIds"] = parentResult.Result.LibraryIds;
        //                result = await UpdateAsync(avatarId, parentResult.Result, result, errorMessage, providerType);
        //            }
        //            else
        //                OASISErrorHandling.HandleError(ref result, $"{errorMessage} No Library could be found for the id {libraryId}");
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

        //public OASISResult<T1> RemoveLibraryFromT1(Guid avatarId, Guid parentId, Guid libraryId, ProviderType providerType = ProviderType.Default)
        //{
        //    OASISResult<T1> result = new OASISResult<T1>();
        //    string errorMessage = "Error occured in OAPPManagerBase.RemoveLibraryFromT1. Reason:";

        //    try
        //    {
        //        OASISResult<T1> parentResult = Load(avatarId, parentId, providerType: providerType);

        //        if (parentResult != null && parentResult.Result != null && !parentResult.IsError)
        //        {
        //            ILibrary library = parentResult.Result.Libraries.FirstOrDefault(x => x.Id == libraryId);

        //            if (library != null)
        //            {
        //                parentResult.Result.Libraries.Remove(library);
        //                parentResult.Result.LibraryIds.Remove(library.ToString());
        //                parentResult.Result.STARNETDNA.MetaData["LibraryIds"] = parentResult.Result.LibraryIds;
        //                result = Update(avatarId, parentResult.Result, result, errorMessage, providerType);
        //            }
        //            else
        //                OASISErrorHandling.HandleError(ref result, $"{errorMessage} No Library could be found for the id {libraryId}");
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

        private OASISResult<T1> Update(Guid avatarId, T1 quest, OASISResult<T1> result, string errorMessage, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> questResult = Update(avatarId, (T1)quest, providerType);
            OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(questResult, result);

            if (questResult != null && questResult.Result != null && !questResult.IsError)
                result.Result = (T1)questResult.Result;
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured saving the {STARNETHolonUIName} with OAPPManagerBase.Update. Reason: {questResult.Message}");

            return result;
        }

        private async Task<OASISResult<T1>> UpdateAsync(Guid avatarId, T1 quest, OASISResult<T1> result, string errorMessage, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> questResult = await UpdateAsync(avatarId, (T1)quest, providerType);
            OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(questResult, result);

            if (questResult != null && questResult.Result != null && !questResult.IsError)
                result.Result = (T1)questResult.Result;
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured saving the {STARNETHolonUIName} with OAPPManagerBase.Update. Reason: {questResult.Message}");

            return result;
        }
    }
}